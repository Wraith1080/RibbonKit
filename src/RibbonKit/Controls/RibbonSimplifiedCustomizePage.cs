using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace RibbonKit.Controls;

internal sealed class SimplifiedTabChoice : DependencyObject
{
    private static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register(
        nameof(DisplayName), typeof(string), typeof(SimplifiedTabChoice), new PropertyMetadata(string.Empty));
    internal SimplifiedTabChoice(RibbonTab? tab, string? draftName, bool commandsNotInRibbon = false)
    {
        Tab = tab;
        CommandsNotInRibbon = commandsNotInRibbon;
        if (draftName is not null) SetValue(DisplayNameProperty, draftName);
        else BindingOperations.SetBinding(this, DisplayNameProperty, tab is null
            ? new Binding(commandsNotInRibbon ? "[CommandsNotInRibbon]" : "[AllTabs]") { Source = RibbonKit.Localization.RibbonLocalizationBindingSource.Instance }
            : new Binding(nameof(tab.Header)) { Source = tab });
    }
    public RibbonTab? Tab { get; }
    internal bool CommandsNotInRibbon { get; }
    public string DisplayName => (string?)GetValue(DisplayNameProperty) ?? string.Empty;
}

internal sealed class SimplifiedGroupChoice : DependencyObject
{
    private static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register(
        nameof(DisplayName), typeof(string), typeof(SimplifiedGroupChoice), new PropertyMetadata(string.Empty));
    internal SimplifiedGroupChoice(RibbonGroup? group, string? draftName)
    {
        Group = group;
        if (draftName is not null) SetValue(DisplayNameProperty, draftName);
        else BindingOperations.SetBinding(this, DisplayNameProperty, group is null
            ? new Binding("[AllGroups]") { Source = RibbonKit.Localization.RibbonLocalizationBindingSource.Instance }
            : new Binding(nameof(group.Header)) { Source = group });
    }
    public RibbonGroup? Group { get; }
    public string DisplayName => (string?)GetValue(DisplayNameProperty) ?? string.Empty;
    public bool IsGroupHeader => true;
    public ImageSource? Icon => null;
    public string GroupName => DisplayName;
}

internal sealed class SimplifiedCustomizeEntry : INotifyPropertyChanged
{
    private bool _showLabel;
    private readonly Action<bool>? _labelChanged;
    internal SimplifiedCustomizeEntry(SimplifiedCommandSource source, RibbonControlSize size, Action<bool>? labelChanged = null, string? groupName = null, string? displayName = null)
    {
        Control = source.Control;
        Group = source.Group;
        GroupName = groupName ?? source.Group.Header?.ToString() ?? string.Empty;
        var description = RibbonCommandCatalog.Describe(source.Declaration);
        DisplayName = displayName ?? description.DisplayName;
        Icon = description.Icon;
        _showLabel = size == RibbonControlSize.Medium;
        _labelChanged = labelChanged;
    }
    public FrameworkElement Control { get; }
    internal RibbonGroup Group { get; }
    public string GroupName { get; }
    public string DisplayName { get; }
    public ImageSource? Icon { get; }
    public bool IsGroupHeader => false;
    public bool ShowLabel
    {
        get => _showLabel;
        set
        {
            if (_showLabel == value) return;
            _showLabel = value;
            _labelChanged?.Invoke(value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowLabel)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// A lookless, tab-based simplified ribbon editor. Changes stay in a draft until
/// <see cref="ApplyChanges"/>; <see cref="CancelChanges"/> discards that draft.
/// <see cref="RibbonOptionsDialog"/> commits it on OK and cancels it on other close paths.
/// Eligible only in Simplified mode. New/renamed tabs and groups and custom-group
/// membership are shared with Classic; simplified order, labels and QAT stay independent.
/// </summary>
[TemplatePart(Name = "PART_TabSelector", Type = typeof(Selector))]
[TemplatePart(Name = "PART_SourceTabSelector", Type = typeof(Selector))]
[TemplatePart(Name = "PART_NewButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_EditButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_ResetMenuButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_ImportButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_ExportButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_GroupSelector", Type = typeof(Selector))]
[TemplatePart(Name = "PART_NewTabButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_NewGroupButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_RenameTabButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_RenameGroupButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_AvailableList", Type = typeof(ListBox))]
[TemplatePart(Name = "PART_CurrentList", Type = typeof(ListBox))]
[TemplatePart(Name = "PART_AddButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_RemoveButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_UpButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_DownButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_ResetButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_ResetAllButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_ApplyButton", Type = typeof(ButtonBase))]
[System.ComponentModel.ToolboxItem(false)]
[System.ComponentModel.DesignTimeVisible(false)]
public partial class RibbonSimplifiedCustomizePage : Control, IRibbonFillPage
{
    /// <summary>Identifies the <see cref="Ribbon"/> dependency property.</summary>
    public static readonly DependencyProperty RibbonProperty = DependencyProperty.Register(
        nameof(Ribbon), typeof(Ribbon), typeof(RibbonSimplifiedCustomizePage),
        new FrameworkPropertyMetadata(null, (d, _) => ((RibbonSimplifiedCustomizePage)d).ChangeRibbon()));

    private static readonly DependencyProperty OwnerCustomizationEnabledProperty = DependencyProperty.Register(
        "OwnerCustomizationEnabled", typeof(bool), typeof(RibbonSimplifiedCustomizePage),
        new PropertyMetadata(true, (d, _) => d.CoerceValue(IsEnabledProperty)));

    private static readonly DependencyProperty OwnerLayoutModeProperty = DependencyProperty.Register(
        "OwnerLayoutMode", typeof(RibbonLayoutMode), typeof(RibbonSimplifiedCustomizePage),
        new PropertyMetadata(RibbonLayoutMode.Classic, (d, _) => d.CoerceValue(IsEnabledProperty)));
    private readonly Dictionary<RibbonTab, List<SimplifiedCommandChoice>?> _drafts = new();
    private readonly List<(ButtonBase Button, RoutedEventHandler Handler)> _handlers = new();
    private Selector? _tabs;
    private Selector? _sourceTabs;
    private RibbonTab? _sourceTab;
    private bool _sourceQatOnly;
    private bool _sourceInitialized;
    private ListBox? _available;
    private ListBox? _current;
    private Ribbon? _observed;
    private RibbonTab? _tab;
    private bool _refreshing;

    static RibbonSimplifiedCustomizePage()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonSimplifiedCustomizePage),
            new FrameworkPropertyMetadata(typeof(RibbonSimplifiedCustomizePage)));
        IsEnabledProperty.OverrideMetadata(typeof(RibbonSimplifiedCustomizePage), new UIPropertyMetadata(true, null,
            (d, value) => (bool)value && ((RibbonSimplifiedCustomizePage)d).Ribbon is { CanUserCustomizeRibbon: true, IsSimplifiedRibbonEnabled: true, LayoutMode: RibbonLayoutMode.Simplified }));
    }

    /// <summary>Initializes a draft editor; loading or retemplating preserves unfinished edits.</summary>
    public RibbonSimplifiedCustomizePage()
    {
        Loaded += (_, _) => { RebasePending(); ObserveRibbon(); RefreshTabs(); };
        Unloaded += (_, _) => { StopObserving(); CloseActions(); ClearInsertion(); };
    }

    /// <summary>The ribbon whose simplified selections this page edits.</summary>
    public Ribbon? Ribbon
    {
        get => (Ribbon?)GetValue(RibbonProperty);
        set => SetValue(RibbonProperty, value);
    }

    /// <summary>Commits structure, simplified selections, labels, scoped resets and imported customization. Hosts can persist the ribbon after this call.</summary>
    public void ApplyChanges()
    {
        if (Ribbon is not { CanUserCustomizeRibbon: true, IsSimplifiedRibbonEnabled: true } ribbon) return;
        RebasePending();
        bool changed = HasDraft;
        bool fullLayout = _pendingLayout is not null;
        if (fullLayout)
            RibbonCustomizationSerializer.ApplyUserLayout(ribbon, RibbonCustomizationSerializer.WriteLayout(BuildDraftLayout()));
        else
        {
            ApplyStructure(ribbon);
            foreach (var pair in _drafts) ribbon.SetSimplifiedChoices(pair.Key, pair.Value, false);
            if (_pendingQat is not null) RibbonCustomizationSerializer.ApplyQuickAccessLayout(ribbon, _pendingQat, userInitiated: true);
        }
        ClearDraft();
        if (changed && !fullLayout) ribbon.NotifySimplifiedCustomizationChanged();
        RefreshTabs();
    }

    /// <summary>Discards uncommitted edits and reloads the current saved selections.</summary>
    public void CancelChanges()
    {
        ClearDraft();
        RefreshTabs();
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        foreach (var pair in _handlers) pair.Button.Click -= pair.Handler;
        _handlers.Clear();
        if (_tabs is not null) _tabs.SelectionChanged -= OnTabChanged;
        if (_sourceTabs is not null) _sourceTabs.SelectionChanged -= OnSourceTabChanged;
        if (_groups is not null) _groups.SelectionChanged -= OnGroupChanged;
        if (_available is not null) _available.SelectionChanged -= OnSelectionChanged;
        if (_current is not null) _current.SelectionChanged -= OnSelectionChanged;
        base.OnApplyTemplate();
        _tabs = GetTemplateChild("PART_TabSelector") as Selector;
        _sourceTabs = GetTemplateChild("PART_SourceTabSelector") as Selector;
        _groups = GetTemplateChild("PART_GroupSelector") as Selector;
        _available = GetTemplateChild("PART_AvailableList") as ListBox;
        _current = GetTemplateChild("PART_CurrentList") as ListBox;
        if (_tabs is not null) _tabs.SelectionChanged += OnTabChanged;
        if (_sourceTabs is not null) _sourceTabs.SelectionChanged += OnSourceTabChanged;
        if (_groups is not null) _groups.SelectionChanged += OnGroupChanged;
        if (_available is not null) _available.SelectionChanged += OnSelectionChanged;
        if (_current is not null) _current.SelectionChanged += OnSelectionChanged;
        Hook("PART_AddButton", (_, _) => Add());
        Hook("PART_RemoveButton", (_, _) => Remove());
        Hook("PART_UpButton", (_, _) => Move(-1));
        Hook("PART_DownButton", (_, _) => Move(1));
        Hook("PART_ResetButton", (_, _) => Reset(false));
        Hook("PART_ResetAllButton", (_, _) => Reset(true));
        Hook("PART_ApplyButton", (_, _) => ApplyChanges());
        Hook("PART_NewTabButton", (_, _) => NewTab());
        Hook("PART_NewGroupButton", (_, _) => NewGroup());
        Hook("PART_RenameTabButton", (_, _) => Rename(_tab));
        Hook("PART_RenameGroupButton", (_, _) => Rename(_group));
        Hook("PART_NewButton", (_, _) => OpenActions("PART_NewButton", CustomizeMenu.New));
        Hook("PART_EditButton", (_, _) => OpenActions("PART_EditButton", CustomizeMenu.Edit));
        Hook("PART_ResetMenuButton", (_, _) => OpenActions("PART_ResetMenuButton", CustomizeMenu.Reset));
        Hook("PART_ImportButton", (_, _) => OnImport());
        Hook("PART_ExportButton", (_, _) => OnExport());
        HookReordering();
        RefreshTabs();
    }

    private void Hook(string name, RoutedEventHandler handler)
    {
        if (GetTemplateChild(name) is not ButtonBase button) return;
        button.Click += handler;
        _handlers.Add((button, handler));
    }

    private void ChangeRibbon()
    {
        StopObserving();
        ClearDraft(); _tab = null; _group = null; _sourceTab = null; _sourceQatOnly = false; _sourceInitialized = false;
        Controls.Ribbon.ObserveUserPermission(this, OwnerCustomizationEnabledProperty, Ribbon, nameof(Controls.Ribbon.CanUserCustomizeRibbon));
        if (Ribbon is null) ClearValue(OwnerLayoutModeProperty);
        else SetBinding(OwnerLayoutModeProperty, new Binding(nameof(Controls.Ribbon.LayoutMode)) { Source = Ribbon });
        CoerceValue(IsEnabledProperty);
        if (IsLoaded) ObserveRibbon();
        RefreshTabs();
    }

    private void ObserveRibbon()
    {
        if (ReferenceEquals(_observed, Ribbon)) return;
        StopObserving(); _observed = Ribbon;
        if (_observed is not null)
            WeakEventManager<Ribbon, EventArgs>.AddHandler(_observed, nameof(Controls.Ribbon.SimplifiedCustomizationChanged), OnSavedChanges);
    }

    private void StopObserving()
    {
        if (_observed is not null)
            WeakEventManager<Ribbon, EventArgs>.RemoveHandler(_observed, nameof(Controls.Ribbon.SimplifiedCustomizationChanged), OnSavedChanges);
        _observed = null;
    }

    private void OnSavedChanges(object? sender, EventArgs e) { RebasePending(); RefreshTabs(); }
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing) return;
        if (ReferenceEquals(sender, _current) && _groups is null)
            _group = _current.SelectedItem switch
            {
                SimplifiedGroupChoice header => header.Group,
                SimplifiedCustomizeEntry command => command.Group,
                _ => null,
            };
        UpdateButtons();
    }
    private void OnSourceTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing) return;
        _sourceTab = (_sourceTabs?.SelectedItem as SimplifiedTabChoice)?.Tab;
        _sourceQatOnly = (_sourceTabs?.SelectedItem as SimplifiedTabChoice)?.CommandsNotInRibbon == true;
        RefreshLists();
    }
    private void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing) return;
        _tab = (_tabs?.SelectedItem as SimplifiedTabChoice)?.Tab;
        _group = null;
        RefreshGroups();
        RefreshLists();
    }

    private void RefreshTabs()
    {
        if (_tabs is null) return;
        _refreshing = true;
        var tabs = DraftTabs();
        var entries = tabs.Select(tab => new SimplifiedTabChoice(tab, ChoiceName(tab))).ToList();
        _tabs.ItemsSource = entries;
        _tab = tabs.Contains(_tab!) ? _tab : tabs.FirstOrDefault(tab => _tab is not null && Controls.Ribbon.GetCommandId(_tab) is { Length: > 0 } id && Controls.Ribbon.GetCommandId(tab) == id)
            ?? tabs.FirstOrDefault(tab => ReferenceEquals(tab, Ribbon?.SelectedTab)) ?? tabs.FirstOrDefault();
        _tabs.SelectedItem = entries.FirstOrDefault(entry => ReferenceEquals(entry.Tab, _tab));
        var sourceTabs = SourceTabs();
        if (!_sourceInitialized) { _sourceTab = sourceTabs.Contains(_tab!) ? _tab : sourceTabs.FirstOrDefault(); _sourceInitialized = true; }
        if (_sourceTab is not null && !sourceTabs.Contains(_sourceTab))
            _sourceTab = sourceTabs.FirstOrDefault(tab => Controls.Ribbon.GetCommandId(_sourceTab) is { Length: > 0 } id && Controls.Ribbon.GetCommandId(tab) == id) ?? sourceTabs.FirstOrDefault();
        if (_sourceTabs is not null)
        {
            var sourceEntries = new List<SimplifiedTabChoice> { new(null, null) };
            sourceEntries.AddRange(sourceTabs.Select(tab => new SimplifiedTabChoice(tab, null)));
            sourceEntries.Add(new(null, null, commandsNotInRibbon: true));
            _sourceTabs.ItemsSource = sourceEntries;
            _sourceTabs.SelectedItem = sourceEntries.FirstOrDefault(entry => entry.Tab == _sourceTab && entry.CommandsNotInRibbon == _sourceQatOnly);
        }
        _refreshing = false;
        RefreshGroups();
        RefreshLists();
    }

    private List<SimplifiedCommandChoice> Choices() => Ribbon is { } ribbon && _tab is { } tab
        ? _drafts.TryGetValue(tab, out var draft) ? draft?.ToList() ?? DraftDefaults(tab)
            : _layoutChoices.TryGetValue(tab, out var imported) ? imported.ToList()
            : _pendingLayout is not null ? DraftDefaults(tab) : ribbon.GetSimplifiedChoices(tab)
        : new();

    private void RefreshLists(FrameworkElement? selected = null, RibbonGroup? selectedGroup = null, RibbonGroup? selectHeader = null)
    {
        if (_available is null || _current is null) return;
        var previous = _current.SelectedItem;
        if (selectHeader is null)
        {
            selected ??= (previous as SimplifiedCustomizeEntry)?.Control;
            selectedGroup ??= (previous as SimplifiedCustomizeEntry)?.Group;
        }
        var selectedHeader = selectHeader ?? (previous as SimplifiedGroupChoice)?.Group ?? _group;
        _refreshing = true;
        var sources = DraftSources();
        var groups = _tab is null ? new List<RibbonGroup>() : DraftGroups(_tab);
        RibbonGroup? CurrentGroup(RibbonGroup? previousGroup) => previousGroup is null ? null : groups.FirstOrDefault(group => group == previousGroup)
            ?? groups.FirstOrDefault(group => Controls.Ribbon.GetCommandId(previousGroup) is { Length: > 0 } id && Controls.Ribbon.GetCommandId(group) == id);
        selectedGroup = CurrentGroup(selectedGroup);
        selectedHeader = CurrentGroup(selectedHeader);
        var previousAvailable = (_available.SelectedItem as SimplifiedCustomizeEntry)?.Control;
        var availableEntries = AvailableSources(_sourceTabs is not null ? _sourceTab : null).Select(pair => new SimplifiedCustomizeEntry(pair.Source,
            Controls.Ribbon.GetSimplifiedSize(pair.Source.Control), groupName: pair.Path, displayName: CommandName(pair.Source))).ToList();
        var available = new ListCollectionView(availableEntries);
        available.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SimplifiedCustomizeEntry.GroupName)));
        _available.ItemsSource = available;
        _available.SelectedItem = availableEntries.FirstOrDefault(entry => previousAvailable is not null && Controls.Ribbon.SameSimplifiedCommand(entry.Control, previousAvailable));
        var entries = new ObservableCollection<object>();
        RibbonGroup? lastGroup = null;
        var shownGroups = new HashSet<RibbonGroup>();
        foreach (var choice in Choices())
        {
            var source = Controls.Ribbon.ResolveSimplifiedChoice(sources, choice);
            if (source is null) continue;
            if (_groups is not null && _group is not null && source.Group != _group) continue;
            if (_groups is null && lastGroup != source.Group)
            {
                entries.Add(new SimplifiedGroupChoice(source.Group, ChoiceName(source.Group)));
                shownGroups.Add(source.Group); lastGroup = source.Group;
            }
            entries.Add(new SimplifiedCustomizeEntry(source, choice.Size, show => ChangeLabel(source.Control, source.Group, show), DraftHeader(source.Group), CommandName(source)));
        }
        if (_groups is null && _tab is not null)
            foreach (var group in DraftGroups(_tab).Where(group => !shownGroups.Contains(group)))
                entries.Add(new SimplifiedGroupChoice(group, ChoiceName(group)));
        _current.ItemsSource = entries;
        _current.SelectedItem = entries.OfType<SimplifiedCustomizeEntry>().FirstOrDefault(entry => selected is not null && Controls.Ribbon.SameSimplifiedCommand(entry.Control, selected) && (selectedGroup is null || entry.Group == selectedGroup))
            ?? (object?)entries.OfType<SimplifiedGroupChoice>().FirstOrDefault(entry => entry.Group == selectedHeader);
        _refreshing = false;
        if (_groups is null) _group = _current.SelectedItem switch
        {
            SimplifiedCustomizeEntry command => command.Group,
            SimplifiedGroupChoice header => header.Group,
            _ => null,
        };
        UpdateButtons();
    }

    private void SaveDraft(List<SimplifiedCommandChoice> choices)
    {
        if (_tab is { } tab) _drafts[tab] = choices;
        UpdateButtons();
    }

    private void ChangeLabel(FrameworkElement source, RibbonGroup group, bool show)
    {
        var choices = Choices();
        int index = choices.FindIndex(choice => SameChoice(choice, source, group));
        if (index < 0) return;
        choices[index] = new SimplifiedCommandChoice(source, show ? RibbonControlSize.Medium : RibbonControlSize.Small, group);
        SaveDraft(choices);
    }

    private void Add()
    {
        if (!IsEnabled || _available?.SelectedItem is not SimplifiedCustomizeEntry entry) return;
        if (TargetGroup(entry.Control) is not { } group) return;
        var choices = Choices();
        if (choices.Any(choice => SameChoice(choice, entry.Control, group))) return;
        StageMembership(group, entry.Control);
        choices.Add(new SimplifiedCommandChoice(entry.Control, Controls.Ribbon.GetSimplifiedSize(entry.Control), group));
        SaveDraft(choices); RefreshLists(entry.Control, group);
    }

    private void Remove()
    {
        if (!IsEnabled || _current?.SelectedItem is not SimplifiedCustomizeEntry entry) return;
        var choices = Choices(); choices.RemoveAll(choice => SameChoice(choice, entry.Control, entry.Group));
        SaveDraft(choices); RefreshLists();
    }

    private void Move(int delta)
    {
        if (!IsEnabled || _current?.SelectedItem is not SimplifiedCustomizeEntry entry) return;
        var choices = Choices();
        var visible = _current.Items.OfType<SimplifiedCustomizeEntry>().ToList();
        int visibleIndex = visible.IndexOf(entry), visibleTarget = visibleIndex + delta;
        if (visibleIndex < 0 || visibleTarget < 0 || visibleTarget >= visible.Count) return;
        int index = choices.FindIndex(choice => SameChoice(choice, entry.Control, entry.Group));
        var neighbour = visible[visibleTarget];
        int target = choices.FindIndex(choice => SameChoice(choice, neighbour.Control, neighbour.Group));
        if (index < 0 || target < 0) return;
        (choices[index], choices[target]) = (choices[target], choices[index]);
        SaveDraft(choices); RefreshLists(entry.Control, entry.Group); RevealSelection();
    }

    private void Reset(bool all)
    {
        if (!IsEnabled || Ribbon is null) return;
        foreach (var tab in DraftTabs().Where(tab => all || ReferenceEquals(tab, _tab)))
            _drafts[tab] = null;
        RefreshLists();
    }

    private void UpdateButtons()
    {
        var choices = Choices();
        var commands = _current?.Items.OfType<SimplifiedCustomizeEntry>().ToList() ?? new();
        int index = _current?.SelectedItem is SimplifiedCustomizeEntry selected ? commands.IndexOf(selected) : -1;
        Enable("PART_AddButton", _available?.SelectedItem is SimplifiedCustomizeEntry available && TargetGroup(available.Control) is { } group
            && !choices.Any(choice => SameChoice(choice, available.Control, group)));
        Enable("PART_RemoveButton", index >= 0);
        Enable("PART_UpButton", index > 0);
        Enable("PART_DownButton", index >= 0 && index < commands.Count - 1);
        Enable("PART_ResetButton", _tab is not null);
        Enable("PART_ResetAllButton", Ribbon?.Tabs.Any(RibbonCustomizePage.IsCustomizableTab) == true);
        Enable("PART_ApplyButton", HasDraft);
        Enable("PART_NewTabButton", Ribbon is not null);
        Enable("PART_NewGroupButton", _tab is not null);
        Enable("PART_RenameTabButton", _tab is not null);
        Enable("PART_RenameGroupButton", _group is not null);
        Enable("PART_NewButton", Ribbon is not null);
        Enable("PART_EditButton", _tab is not null);
        Enable("PART_ResetMenuButton", Ribbon is not null);
        Enable("PART_ImportButton", Ribbon is not null);
        Enable("PART_ExportButton", Ribbon is not null);
    }

    private void Enable(string name, bool enabled) => (GetTemplateChild(name) as ButtonBase)?.SetCurrentValue(IsEnabledProperty, enabled);
}
