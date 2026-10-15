using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using RibbonKit.Localization;

namespace RibbonKit.Controls;

public partial class RibbonSimplifiedCustomizePage
{
    // Detached structures and source references keep Cancel free of live mutations/proxy bindings.
    // Creation and moves share chronological placements: later creation anchors to the
    // current draft position, rather than replaying all moves after creating every tab.
    private readonly List<(RibbonTab Tab, RibbonTab? Anchor, bool After, bool Create)> _tabPlacements = new();
    private readonly Dictionary<RibbonTab, List<(RibbonGroup Group, RibbonGroup? Anchor)>> _newGroups = new();
    private readonly Dictionary<FrameworkElement, string> _renames = new();
    private readonly Dictionary<RibbonGroup, (ImageSource? Icon, RibbonGroupLayout Layout)> _groupEdits = new();
    private readonly RibbonGroup _qatSourceGroup = new();
    private readonly Dictionary<RibbonGroup, List<FrameworkElement>> _memberships = new();
    private Selector? _groups;
    private RibbonGroup? _group;
    private bool HasDraft => _drafts.Count > 0 || _tabPlacements.Count > 0 || _newGroups.Count > 0 || _renames.Count > 0 || _groupEdits.Count > 0 || _memberships.Count > 0 || _pendingLayout is not null || _pendingQat is not null;

    private ImageSource? DraftIcon(RibbonGroup group) => _groupEdits.TryGetValue(group, out var edit) ? edit.Icon : group.Icon;
    private RibbonGroupLayout DraftLayout(RibbonGroup group) => _groupEdits.TryGetValue(group, out var edit) ? edit.Layout : group.Layout;
    private List<RibbonTab> SourceTabs() => Ribbon?.Tabs.Where(tab => RibbonCustomizePage.IsCustomizableTab(tab) && !Controls.Ribbon.GetIsCustom(tab)).ToList() ?? new();

    private string DraftHeader(FrameworkElement element) => ChoiceName(element) ?? (element switch
    {
        RibbonTab tab => tab.Header?.ToString() ?? string.Empty,
        RibbonGroup group => group.Header?.ToString() ?? string.Empty,
        _ => string.Empty,
    });

    private List<RibbonTab> DraftTabs()
    {
        var tabs = _layoutTabs?.ToList() ?? Ribbon?.Tabs.Where(RibbonCustomizePage.IsCustomizableTab).ToList() ?? new();
        if (Ribbon is null) return tabs;
        foreach (var placement in _tabPlacements)
        {
            var moving = ResolveTab(tabs, placement.Tab) ?? (placement.Create ? placement.Tab : null);
            var anchor = ResolveTab(tabs, placement.Anchor);
            if (moving is null || (!placement.Create && (anchor is null || !Controls.Ribbon.GetIsCustom(moving))) || moving == anchor) continue;
            tabs.Remove(moving);
            tabs.Insert(anchor is not null ? tabs.IndexOf(anchor) + (placement.After ? 1 : 0) : tabs.Count, moving);
        }
        return tabs;
    }

    private static RibbonTab? ResolveTab(IEnumerable<RibbonTab> tabs, RibbonTab? tab) => tab is null ? null
        : tabs.FirstOrDefault(candidate => candidate == tab) ?? tabs.FirstOrDefault(candidate =>
            Controls.Ribbon.GetCommandId(tab) is { Length: > 0 } id && Controls.Ribbon.GetCommandId(candidate) == id);

    internal bool StageTabMove(RibbonTab tab, RibbonTab target, bool after)
    {
        if (!IsEnabled || Ribbon is null) return false;
        var before = DraftTabs();
        var moving = ResolveTab(before, tab); var anchor = ResolveTab(before, target);
        if (moving is null || anchor is null || moving == anchor || !Controls.Ribbon.GetIsCustom(moving)) return false;
        var reordered = before.ToList(); reordered.Remove(moving);
        reordered.Insert(reordered.IndexOf(anchor) + (after ? 1 : 0), moving);
        if (before.SequenceEqual(reordered)) return false;
        _tabPlacements.Add((moving, anchor, after, false));
        RefreshTabs(); return true;
    }

    private void MoveTab()
    {
        if (!IsEnabled || Ribbon is null || _tab is not { } moving || !Controls.Ribbon.GetIsCustom(moving)) return;
        var tabs = DraftTabs(); var targets = tabs.Where(tab => tab != moving).ToList();
        if (targets.Count == 0) return;
        var next = tabs.Skip(tabs.IndexOf(moving) + 1).FirstOrDefault();
        var dialog = new RibbonCustomizeMoveTabDialog
        {
            Owner = Window.GetWindow(this), FlowDirection = FlowDirection,
            Targets = targets.Select(tab => new SimplifiedTabChoice(tab, ChoiceName(tab))).ToList(),
            Target = next ?? targets.Last(), After = next is null,
        };
        Controls.Ribbon.SetDensity(dialog, Ribbon.Density);
        if (Ribbon.Density == RibbonDensity.Touch) dialog.Height = 280;
        if (dialog.ShowDialog() == true && dialog.Target is { } target) StageTabMove(moving, target, dialog.After);
    }

    private List<RibbonGroup> DraftGroups(RibbonTab tab)
    {
        var groups = _layoutGroups.TryGetValue(tab, out var imported) ? imported.ToList() : tab.Groups.Where(group => !Controls.Ribbon.GetIsMerged(group)).ToList();
        if (_newGroups.TryGetValue(tab, out var additions))
            foreach (var pair in additions)
                groups.Insert(pair.Anchor is not null && groups.IndexOf(pair.Anchor) is >= 0 and var index ? index + 1 : groups.Count, pair.Group);
        return groups;
    }

    private void RefreshGroups()
    {
        var groups = _tab is null ? new List<RibbonGroup>() : DraftGroups(_tab);
        if (_group is not null && !groups.Contains(_group)) _group = null;
        if (_groups is null) return;
        var entries = new List<SimplifiedGroupChoice> { new(null, null) };
        entries.AddRange(groups.Select(group => new SimplifiedGroupChoice(group, ChoiceName(group))));
        bool wasRefreshing = _refreshing; _refreshing = true;
        _groups.ItemsSource = entries;
        _groups.SelectedItem = entries.First(entry => entry.Group == _group);
        _refreshing = wasRefreshing;
    }

    private void OnGroupChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_refreshing) return;
        _group = (_groups?.SelectedItem as SimplifiedGroupChoice)?.Group;
        RefreshLists();
    }

    private List<(SimplifiedCommandSource Source, string Path)> AvailableSources(RibbonTab? filter = null)
    {
        var result = new List<(SimplifiedCommandSource Source, string Path)>();
        if (Ribbon is null) return result;
        // Source discovery describes authored content, independently of destination drafts.
        var authored = SourceTabs().SelectMany(tab => Controls.Ribbon.SimplifiedSources(tab, true)
            .Where(source => !Controls.Ribbon.GetIsCustom(source.Group) && ReferenceEquals(source.Control, source.Declaration))).ToList();
        if (_sourceQatOnly && _sourceTabs is not null)
        {
            foreach (var declared in Ribbon.DeclaredQuickAccessCommands)
            {
                var source = Controls.Ribbon.OriginalSimplifiedSource(declared);
                if (source is not (RibbonButton or RibbonToggleButton or RibbonDropDownButton or RibbonComboBox or RibbonGallery)
                    || Controls.Ribbon.GetIsMerged(source) || Controls.Ribbon.GetMergeSource(source) is not null
                    || authored.Any(entry => Controls.Ribbon.SameSimplifiedCommand(entry.Control, source))
                    || result.Any(pair => Controls.Ribbon.SameSimplifiedCommand(pair.Source.Control, source))) continue;
                result.Add((new SimplifiedCommandSource(source, _qatSourceGroup, source), RibbonLocalization.GetString(RibbonString.CommandsNotInRibbon)));
            }
            return result;
        }
        foreach (var tab in SourceTabs().Where(tab => filter is null || tab == filter))
        foreach (var source in Controls.Ribbon.SimplifiedSources(tab, true).Where(source => authored.Contains(source)))
            if (!result.Any(pair => Controls.Ribbon.SameSimplifiedCommand(pair.Source.Control, source.Control)))
                result.Add((source, filter is null ? tab.Header + " › " + source.Group.Header : source.Group.Header?.ToString() ?? string.Empty));
        return result;
    }

    private List<SimplifiedCommandSource> DraftSources() => _tab is null ? new() : DraftSources(_tab);
    private List<SimplifiedCommandSource> DraftSources(RibbonTab tab)
    {
        var groups = DraftGroups(tab);
        var sources = Controls.Ribbon.SimplifiedSources(tab, true).Where(entry => groups.Contains(entry.Group)).ToList();
        foreach (var group in groups)
        {
            if (_layoutSources.TryGetValue(group, out var imported)) sources.AddRange(imported);
            if (_memberships.TryGetValue(group, out var members))
                foreach (var source in members)
                    if (!sources.Any(entry => entry.Group == group && Controls.Ribbon.SameSimplifiedCommand(entry.Control, source)))
                        sources.Add(new SimplifiedCommandSource(source, group, source));
        }
        return sources;
    }

    private RibbonGroup? TargetGroup(FrameworkElement source)
    {
        var sources = DraftSources();
        if (_group is null)
        {
            var matches = sources.Where(entry => Controls.Ribbon.SameSimplifiedCommand(entry.Control, source)).Select(entry => entry.Group).Distinct().ToList();
            return matches.Count == 1 ? matches[0] : null;
        }
        return Controls.Ribbon.GetIsCustom(_group) || sources.Any(entry => entry.Group == _group && Controls.Ribbon.SameSimplifiedCommand(entry.Control, source))
            ? _group : null;
    }

    private static bool SameChoice(SimplifiedCommandChoice choice, FrameworkElement source, RibbonGroup group) =>
        (choice.Group is null || choice.Group == group) && Controls.Ribbon.SameSimplifiedCommand(choice.Source, source);

    private void StageMembership(RibbonGroup group, FrameworkElement source)
    {
        if (!Controls.Ribbon.GetIsCustom(group) || DraftSources().Any(entry => entry.Group == group && Controls.Ribbon.SameSimplifiedCommand(entry.Control, source))) return;
        if (!_memberships.TryGetValue(group, out var members)) _memberships[group] = members = new();
        members.Add(source);
    }

    private void NewTab()
    {
        if (!IsEnabled || Ribbon is null) return;
        var tab = new RibbonTab { Header = RibbonLocalization.GetString(RibbonString.NewTab) };
        Controls.Ribbon.SetIsCustom(tab, true); Controls.Ribbon.SetCommandId(tab, RibbonCustomizePage.NewCustomId());
        var group = RibbonCustomizePage.CreateCustomGroup(Ribbon); tab.Groups.Add(group);
        _tabPlacements.Add((tab, _tab, true, true)); _tab = tab; _group = group;
        RefreshTabs();
    }

    private void NewGroup()
    {
        if (!IsEnabled || _tab is null || Ribbon is null) return;
        var group = RibbonCustomizePage.CreateCustomGroup(Ribbon);
        if (!_newGroups.TryGetValue(_tab, out var groups)) _newGroups[_tab] = groups = new();
        groups.Add((group, _group)); _group = group;
        RefreshGroups(); RefreshLists(selectHeader: group);
    }

    private void Rename(FrameworkElement? target)
    {
        if (!IsEnabled || target is null) return;
        var dialog = new RibbonCustomizeEditDialog
        {
            Owner = Window.GetWindow(this), ItemName = DraftHeader(target),
            Title = RibbonLocalization.GetString(target is RibbonTab ? RibbonString.EditTab : RibbonString.EditGroup),
            FlowDirection = FlowDirection,
        };
        if (target is RibbonGroup group && Controls.Ribbon.GetIsCustom(group) && Ribbon is { } ribbon)
        {
            dialog.CanEditIcon = true; dialog.CanEditLayout = true;
            dialog.IconChoices = RibbonCommandCatalog.CollectIcons(ribbon);
            dialog.SelectedIcon = DraftIcon(group); dialog.SelectedLayout = DraftLayout(group);
        }
        if (dialog.ShowDialog() == true) ApplyEditDraft(target, dialog);
    }

    internal void ApplyEditDraft(FrameworkElement target, RibbonCustomizeEditDialog dialog)
    {
        if (!IsEnabled || !DraftTabs().Any(tab => tab == target || target is RibbonGroup group && DraftGroups(tab).Contains(group))) return;
        if (target is RibbonGroup custom && Controls.Ribbon.GetIsCustom(custom) && dialog.CanEditIcon && dialog.CanEditLayout)
            _groupEdits[custom] = (dialog.SelectedIcon, dialog.SelectedLayout);
        if (!string.IsNullOrWhiteSpace(dialog.ItemName)) _renames[target] = dialog.ItemName.Trim();
        RefreshTabs();
    }

    internal void RenameDraft(FrameworkElement target, string name)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(name) || !DraftTabs().Any(tab => tab == target || (target is RibbonGroup group && DraftGroups(tab).Contains(group)))) return;
        _renames[target] = name.Trim(); RefreshTabs();
    }

    private void ApplyStructure(Ribbon ribbon)
    {
        var available = ribbon.Tabs.Where(RibbonCustomizePage.IsCustomizableTab)
            .SelectMany(tab => Controls.Ribbon.SimplifiedSources(tab, true)).Select(entry => entry.Control)
            .Concat(ribbon.DeclaredQuickAccessCommands).ToList();
        var selectedTab = ribbon.SelectedTab;
        foreach (var placement in _tabPlacements)
        {
            var eligible = ribbon.Tabs.Where(RibbonCustomizePage.IsCustomizableTab).ToList();
            var anchor = ResolveTab(eligible, placement.Anchor);
            if (placement.Create)
            {
                if (!ribbon.Tabs.Contains(placement.Tab))
                    ribbon.Tabs.Insert(anchor is not null ? ribbon.Tabs.IndexOf(anchor) + (placement.After ? 1 : 0) : ribbon.Tabs.Count, placement.Tab);
            }
            else if (ResolveTab(eligible, placement.Tab) is { } moving && Controls.Ribbon.GetIsCustom(moving) && anchor is not null && moving != anchor)
            {
                int from = ribbon.Tabs.IndexOf(moving), to = ribbon.Tabs.IndexOf(anchor) + (placement.After ? 1 : 0);
                if (from < to) to--;
                if (from != to) ribbon.Tabs.Move(from, to);
            }
        }
        // The tab strip can reset selection when an item moves; retain activation.
        if (selectedTab is not null && ribbon.Tabs.Contains(selectedTab) && selectedTab.Visibility == Visibility.Visible)
            ribbon.SelectedTab = selectedTab;
        foreach (var addition in _newGroups.Where(pair => ribbon.Tabs.Contains(pair.Key) && RibbonCustomizePage.IsCustomizableTab(pair.Key)))
            foreach (var pair in addition.Value)
                addition.Key.Groups.Insert(pair.Anchor is not null && addition.Key.Groups.IndexOf(pair.Anchor) is >= 0 and var index ? index + 1 : addition.Key.Groups.Count, pair.Group);
        var live = ribbon.Tabs.Where(RibbonCustomizePage.IsCustomizableTab).SelectMany(tab => tab.Groups.Where(group => !Controls.Ribbon.GetIsMerged(group))).ToHashSet();
        foreach (var pair in _renames)
            switch (pair.Key)
            {
                case RibbonTab tab when ribbon.Tabs.Contains(tab) && RibbonCustomizePage.IsCustomizableTab(tab): tab.Header = pair.Value; break;
                case RibbonGroup group when live.Contains(group): group.Header = pair.Value; break;
            }
        foreach (var pair in _groupEdits.Where(pair => live.Contains(pair.Key)))
        { pair.Key.Icon = pair.Value.Icon; pair.Key.Layout = pair.Value.Layout; }
        foreach (var pair in _memberships.Where(pair => live.Contains(pair.Key)))
            foreach (var staged in pair.Value)
            {
                // Resolve again on Apply: the host may have replaced/removed a command while the draft was open.
                var source = available.FirstOrDefault(command => Controls.Ribbon.SameSimplifiedCommand(command, staged));
                if (source is null) continue;
                if (!RibbonGroupsHost.Commands(pair.Key).Any(command => Controls.Ribbon.SameSimplifiedCommand(Controls.Ribbon.OriginalSimplifiedSource(command), source)))
                    pair.Key.Items.Add(ribbon.CreateCommandProxy(source, pair.Key.Layout == RibbonGroupLayout.Large ? RibbonControlSize.Large : RibbonControlSize.Medium));
            }
    }

    private void ClearDraft()
    {
        _drafts.Clear(); _tabPlacements.Clear(); _newGroups.Clear(); _renames.Clear(); _groupEdits.Clear(); _memberships.Clear();
        ClearPersistenceDraft();
    }
}
