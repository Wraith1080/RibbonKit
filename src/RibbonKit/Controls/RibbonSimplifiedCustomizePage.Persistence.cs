using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RibbonKit.Localization;
using static RibbonKit.Controls.RibbonCustomizationSerializer;

namespace RibbonKit.Controls;

public partial class RibbonSimplifiedCustomizePage
{
    /// <summary>Identifies the host-authored <see cref="ResetLayout"/> baseline.</summary>
    public static readonly DependencyProperty ResetLayoutProperty = DependencyProperty.Register(
        nameof(ResetLayout), typeof(string), typeof(RibbonSimplifiedCustomizePage), new PropertyMetadata(null));

    /// <summary>
    /// A startup customization capture used by Reset QAT and Reset All Customizations.
    /// Resets remain drafts until Apply/OK. Null disables those reset scopes.
    /// </summary>
    public string? ResetLayout
    {
        get => (string?)GetValue(ResetLayoutProperty);
        set => SetValue(ResetLayoutProperty, value);
    }

    private RibbonLayoutDto? _pendingLayout;
    private RibbonLayoutDto? _pendingRibbonBase;
    private RibbonLayoutDto? _pendingQat;
    private RibbonLayoutDto? _pendingQatBase;
    private List<RibbonTab>? _layoutTabs;
    private readonly Dictionary<RibbonTab, List<RibbonGroup>> _layoutGroups = new();
    private readonly Dictionary<FrameworkElement, string> _layoutNames = new();
    private readonly Dictionary<RibbonGroup, List<SimplifiedCommandSource>> _layoutSources = new();
    private readonly Dictionary<RibbonTab, List<SimplifiedCommandChoice>> _layoutChoices = new();
    private readonly Dictionary<(FrameworkElement Source, RibbonGroup Group), string> _layoutCommandNames = new();

    private string? ChoiceName(FrameworkElement element) => _renames.GetValueOrDefault(element) ?? _layoutNames.GetValueOrDefault(element);
    private string? CommandName(SimplifiedCommandSource source) => _layoutCommandNames.GetValueOrDefault((source.Control, source.Group));
    private List<SimplifiedCommandChoice> DraftDefaults(RibbonTab tab) => DraftSources(tab)
        .Where(source => Controls.Ribbon.GetSimplifiedPlacement(source.Declaration) == RibbonSimplifiedPlacement.Inline
            && RibbonGroupsHost.Commands(source.Group).Contains(source.Declaration))
        .Select(source => new SimplifiedCommandChoice(source.Control, Controls.Ribbon.GetSimplifiedSize(source.Declaration), source.Group)).ToList();

    private void AddResetUtilities(ContextMenu menu, Func<RibbonString, Action, bool, MenuItem> add)
    {
        menu.Items.Add(new Separator());
        bool baseline = TryReadLayout(ResetLayout, out _);
        add(RibbonString.ResetQuickAccessToolbar, ResetQatDraft, baseline);
        add(RibbonString.ResetAllCustomizations, () => ImportDraft(ResetLayout), baseline);
    }

    private void ResetQatDraft()
    {
        if (!IsEnabled || Ribbon is null || !TryReadLayout(ResetLayout, out var baseline)) return;
        _pendingQatBase = CaptureLayout(Ribbon); _pendingQat = baseline; UpdateButtons();
    }

    internal bool ImportDraft(string? json)
    {
        if (!IsEnabled || Ribbon is null || !TryReadLayout(json, out var layout)) return false;
        var previousTabId = _tab is null ? null : Controls.Ribbon.GetCommandId(_tab);
        ClearDraft();
        _pendingRibbonBase = CaptureLayout(Ribbon); _pendingQatBase = CaptureLayout(Ribbon);
        _pendingLayout = layout; _pendingQat = layout;
        ProjectLayout(layout!);
        _tab = _layoutTabs!.FirstOrDefault(tab => Controls.Ribbon.GetCommandId(tab) == previousTabId);
        _group = null; RefreshTabs(); return true;
    }

    private void ProjectLayout(RibbonLayoutDto layout)
    {
        if (Ribbon is not { } ribbon) return;
        BuildIdentity(ribbon, out var byId, out _, out var icons);
        var eligible = ribbon.Tabs.Where(RibbonCustomizePage.IsCustomizableTab).SelectMany(tab => Controls.Ribbon.SimplifiedSources(tab, true))
            .Select(source => source.Control).Concat(ribbon.DeclaredQuickAccessCommands).ToHashSet();
        _layoutTabs = new();
        var liveTabs = ribbon.Tabs.Where(RibbonCustomizePage.IsCustomizableTab).ToList();
        foreach (var saved in layout.Tabs)
        {
            RibbonTab? tab = saved.IsCustom ? new RibbonTab() : liveTabs.FirstOrDefault(tab => !Controls.Ribbon.GetIsCustom(tab) && Controls.Ribbon.GetCommandId(tab) == saved.Id);
            if (tab is null || _layoutTabs.Contains(tab)) continue;
            if (saved.IsCustom)
            {
                Controls.Ribbon.SetIsCustom(tab, true); Controls.Ribbon.SetCommandId(tab, saved.Id ?? RibbonCustomizePage.NewCustomId());
                saved.Id ??= Controls.Ribbon.GetCommandId(tab);
                tab.Header = saved.Header ?? RibbonLocalization.GetString(RibbonString.NewTab);
            }
            if (saved.Header is not null) _layoutNames[tab] = saved.Header;
            _layoutTabs.Add(tab);
            var groups = new List<RibbonGroup>();
            foreach (var groupData in saved.Groups)
            {
                RibbonGroup? group = groupData.IsCustom ? RibbonCustomizePage.CreateCustomGroup(ribbon)
                    : tab.Groups.FirstOrDefault(group => !Controls.Ribbon.GetIsCustom(group) && Controls.Ribbon.GetCommandId(group) == groupData.Id);
                if (group is null || groups.Contains(group)) continue;
                if (groupData.IsCustom)
                {
                    if (groupData.Id is not null) Controls.Ribbon.SetCommandId(group, groupData.Id);
                    groupData.Id ??= Controls.Ribbon.GetCommandId(group);
                    group.Header = groupData.Header ?? RibbonLocalization.GetString(RibbonString.NewGroup);
                    group.Layout = groupData.Layout == RibbonGroupLayout.Default ? RibbonGroupLayout.Stacked : groupData.Layout;
                    group.Icon = groupData.IconCommandId is not null ? icons.GetValueOrDefault(groupData.IconCommandId) : null;
                    var commands = new List<SimplifiedCommandSource>();
                    var knownCommands = new List<CommandDto>();
                    foreach (var command in groupData.Commands ?? new())
                    {
                        if (command.SourceId is null || !byId.TryGetValue(command.SourceId, out var source) || source is not (RibbonButton or RibbonToggleButton or RibbonSplitButton or RibbonDropDownButton or RibbonComboBox or RibbonGallery)) continue;
                        source = Controls.Ribbon.OriginalSimplifiedSource(source);
                        if (!eligible.Contains(source) || Controls.Ribbon.GetIsMerged(source) || Controls.Ribbon.GetMergeSource(source) is not null) continue;
                        if (commands.Any(entry => Controls.Ribbon.SameSimplifiedCommand(entry.Control, source))) continue;
                        commands.Add(new SimplifiedCommandSource(source, group, source));
                        knownCommands.Add(command);
                        if (command.Header is not null) _layoutCommandNames[(source, group)] = command.Header;
                    }
                    _layoutSources[group] = commands;
                    groupData.Commands = knownCommands;
                }
                if (groupData.Header is not null) _layoutNames[group] = groupData.Header;
                groups.Add(group);
            }
            groups.AddRange(tab.Groups.Where(group => !Controls.Ribbon.GetIsCustom(group) && !Controls.Ribbon.GetIsMerged(group) && !groups.Contains(group)));
            _layoutGroups[tab] = groups;
        }
        _layoutTabs.AddRange(liveTabs.Where(tab => !Controls.Ribbon.GetIsCustom(tab) && !_layoutTabs.Contains(tab)));
        if (layout.Simplified is { Version: 1 or 2 } simplified)
        {
            foreach (var saved in simplified.Tabs)
            {
                var tab = _layoutTabs.FirstOrDefault(tab => Controls.Ribbon.GetCommandId(tab) == saved.TabId);
                if (tab is null || _layoutChoices.ContainsKey(tab)) continue;
                var sources = DraftSources(tab);
                var choices = new List<SimplifiedCommandChoice>();
                foreach (var command in saved.Commands)
                {
                    if (command.SourceId is not { Length: > 0 }) continue;
                    var source = sources.FirstOrDefault(source => Controls.Ribbon.GetCommandId(source.Control) == command.SourceId
                        && (simplified.Version == 1 || command.GroupId is null || Controls.Ribbon.GetCommandId(source.Group) == command.GroupId));
                    if (source is not null && !choices.Any(choice => SameChoice(choice, source.Control, source.Group)))
                        choices.Add(new SimplifiedCommandChoice(source.Control, command.Size, source.Group));
                }
                _layoutChoices[tab] = choices;
            }
        }
        else
        {
            // Older files preserve optional selections, including recreated custom group identities.
            foreach (var pair in ribbon.SimplifiedOverrides)
            {
                var tab = _layoutTabs.FirstOrDefault(tab => tab == pair.Key || Controls.Ribbon.GetCommandId(tab) == Controls.Ribbon.GetCommandId(pair.Key));
                if (tab is null) continue;
                var sources = DraftSources(tab);
                _layoutChoices[tab] = pair.Value.Select(choice => (choice, source: sources.FirstOrDefault(source => Controls.Ribbon.SameSimplifiedCommand(source.Control, choice.Source)
                    && (source.Group == choice.Group || choice.Group is not null && Controls.Ribbon.GetCommandId(source.Group) == Controls.Ribbon.GetCommandId(choice.Group)))))
                    .Where(pair => pair.source is not null).Select(pair => new SimplifiedCommandChoice(pair.source!.Control, pair.choice.Size, pair.source.Group)).ToList();
            }
        }
    }

    private bool RebasePending()
    {
        if (Ribbon is null || _pendingLayout is null && _pendingQat is null) return false;
        bool changed = false;
        var live = CaptureLayout(Ribbon);
        if (_pendingRibbonBase is not null && !SameRibbonScope(live, _pendingRibbonBase))
        {
            // A later edit in the live Classic/Simplified surface supersedes the older imported scope.
            _pendingLayout = null; _pendingRibbonBase = null; ClearLayoutProjection();
            _drafts.Clear(); _tabPlacements.Clear(); _newGroups.Clear(); _renames.Clear(); _groupEdits.Clear(); _memberships.Clear();
            _removedTabs.Clear(); _removedGroups.Clear();
            changed = true;
        }
        if (_pendingQatBase is not null && !SameQatScope(live, _pendingQatBase))
        { _pendingQat = null; _pendingQatBase = null; changed = true; }
        return changed;
    }
    private static bool SameRibbonScope(RibbonLayoutDto a, RibbonLayoutDto b) =>
        JsonSerializer.Serialize(new { a.Tabs, a.Simplified }) == JsonSerializer.Serialize(new { b.Tabs, b.Simplified });
    private static bool SameQatScope(RibbonLayoutDto a, RibbonLayoutDto b) =>
        JsonSerializer.Serialize(new { a.QuickAccess, a.QuickAccessPosition, a.IsQuickAccessVisible }) == JsonSerializer.Serialize(new { b.QuickAccess, b.QuickAccessPosition, b.IsQuickAccessVisible });

    internal string ExportDraft()
    {
        if (RebasePending()) RefreshTabs();
        return Ribbon is null ? string.Empty : WriteLayout(BuildDraftLayout());
    }

    private RibbonLayoutDto BuildDraftLayout()
    {
        var live = CaptureLayout(Ribbon!);
        // Deserialize a copy so exporting repeatedly cannot edit either pending baseline.
        TryReadLayout(WriteLayout(_pendingLayout ?? live), out var layout);
        layout ??= live;
        layout.QuickAccess = (_pendingQat ?? live).QuickAccess;
        layout.QuickAccessPosition = (_pendingQat ?? live).QuickAccessPosition;
        layout.IsQuickAccessVisible = (_pendingQat ?? live).IsQuickAccessVisible;
        BuildIdentity(Ribbon!, out _, out var ids, out var icons);
        var tabs = new List<TabDto>();
        foreach (var tab in DraftTabs())
        {
            string? id = Controls.Ribbon.GetCommandId(tab);
            if (id is null && !Controls.Ribbon.GetIsCustom(tab)) continue;
            var data = layout.Tabs.FirstOrDefault(data => data.Id == id) ?? new TabDto
                { Id = id, IsCustom = Controls.Ribbon.GetIsCustom(tab), Visible = Ribbon!.GetAuthoredVisibility(tab) == Visibility.Visible };
            data.Header = DraftHeader(tab);
            var groups = new List<GroupDto>();
            foreach (var group in DraftGroups(tab))
            {
                string? groupId = Controls.Ribbon.GetCommandId(group);
                if (groupId is null && !Controls.Ribbon.GetIsCustom(group)) continue;
                var groupData = data.Groups.FirstOrDefault(data => data.Id == groupId) ?? new GroupDto
                    { Id = groupId, IsCustom = Controls.Ribbon.GetIsCustom(group), Layout = group.Layout };
                groupData.Header = DraftHeader(group);
                if (groupData.IsCustom)
                {
                    groupData.IconCommandId = FindIconId(DraftIcon(group), icons);
                    groupData.Layout = DraftLayout(group);
                    groupData.Commands ??= new();
                    if (_groupEdits.TryGetValue(group, out var edit) && edit.Layout != group.Layout)
                        foreach (var command in groupData.Commands)
                            command.Size = groupData.Layout == RibbonGroupLayout.Large ? RibbonControlSize.Large
                                : command.Size == RibbonControlSize.Large ? RibbonControlSize.Medium : command.Size;
                    if (_memberships.TryGetValue(group, out var members))
                        foreach (var source in members)
                        {
                            string? sourceId = ids.GetValueOrDefault(source) ?? Controls.Ribbon.GetCommandId(source);
                            if (sourceId is null || groupData.Commands.Any(command => command.SourceId == sourceId)) continue;
                            groupData.Commands.Add(new CommandDto { SourceId = sourceId, Header = RibbonCommandCatalog.Describe(source).DisplayName,
                                Size = DraftLayout(group) == RibbonGroupLayout.Large ? RibbonControlSize.Large : RibbonControlSize.Medium });
                        }
                }
                groups.Add(groupData);
            }
            data.Groups = groups; tabs.Add(data);
        }
        layout.Tabs = tabs;
        if (_pendingLayout is not null)
        {
            layout.Simplified = new SimplifiedDto();
            foreach (var pair in _layoutChoices) WriteChoices(layout.Simplified, pair.Key, pair.Value);
        }
        layout.Simplified ??= new SimplifiedDto();
        foreach (var pair in _drafts) WriteChoices(layout.Simplified, pair.Key, pair.Value);
        var retainedTabs = DraftTabs();
        layout.Simplified.Tabs.RemoveAll(data => !retainedTabs.Any(tab => Controls.Ribbon.GetCommandId(tab) == data.TabId));
        return layout;
    }
    private void WriteChoices(SimplifiedDto simplified, RibbonTab tab, List<SimplifiedCommandChoice>? choices)
    {
        string? id = Controls.Ribbon.GetCommandId(tab);
        if (id is null) return;
        simplified.Tabs.RemoveAll(data => data.TabId == id);
        if (choices is null) return;
        var sources = DraftSources(tab);
        var commands = choices.Select(choice => (choice, source: Controls.Ribbon.ResolveSimplifiedChoice(sources, choice)))
            .Where(pair => pair.source is not null && Controls.Ribbon.GetCommandId(pair.source.Control) is { Length: > 0 })
            .Select(pair => new SimplifiedCommandDto { SourceId = Controls.Ribbon.GetCommandId(pair.source!.Control), GroupId = Controls.Ribbon.GetCommandId(pair.source.Group), Size = pair.choice.Size }).ToList();
        simplified.Tabs.Add(new SimplifiedTabDto { TabId = id, Commands = commands });
    }

    private void ClearLayoutProjection()
    {
        _layoutTabs = null; _layoutGroups.Clear(); _layoutNames.Clear(); _layoutSources.Clear(); _layoutChoices.Clear(); _layoutCommandNames.Clear();
    }
    private void ClearPersistenceDraft()
    {
        _pendingLayout = null; _pendingRibbonBase = null; _pendingQat = null; _pendingQatBase = null; ClearLayoutProjection();
    }
    private void OnImport()
    {
        if (!IsEnabled || Ribbon is null) return;
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = RibbonLocalization.GetString(RibbonString.ImportRibbonCustomization), Filter = RibbonLocalization.GetString(RibbonString.RibbonLayoutFileFilter),
            DefaultExt = ".json", CheckFileExists = true,
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try { if (!ImportDraft(File.ReadAllText(dialog.FileName))) ShowFileError(RibbonLocalization.GetString(RibbonString.InvalidRibbonLayout)); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { ShowFileError(RibbonLocalization.GetString(RibbonString.CouldNotReadFile) + "\n\n" + ex.Message); }
    }
    private void OnExport()
    {
        if (!IsEnabled || Ribbon is null) return;
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = RibbonLocalization.GetString(RibbonString.ExportRibbonCustomization), Filter = RibbonLocalization.GetString(RibbonString.RibbonLayoutFileFilter),
            DefaultExt = ".json", FileName = "ribbon-customization.json", AddExtension = true, OverwritePrompt = true,
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try { File.WriteAllText(dialog.FileName, ExportDraft()); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { ShowFileError(RibbonLocalization.GetString(RibbonString.CouldNotSaveFile) + "\n\n" + ex.Message); }
    }
    private void ShowFileError(string message) => MessageBox.Show(Window.GetWindow(this), message,
        RibbonLocalization.GetString(RibbonString.RibbonCustomization), MessageBoxButton.OK, MessageBoxImage.Warning);
}
