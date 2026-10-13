using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace RibbonKit.Controls;

internal sealed record SimplifiedTabChoice(RibbonTab Tab);

internal sealed class SimplifiedCustomizeEntry : INotifyPropertyChanged
{
    private bool _showLabel;
    private readonly Action<bool>? _labelChanged;
    internal SimplifiedCustomizeEntry(SimplifiedCommandSource source, RibbonControlSize size, Action<bool>? labelChanged = null)
    {
        Control = source.Control;
        GroupName = source.Group.Header?.ToString() ?? string.Empty;
        var description = RibbonCommandCatalog.Describe(source.Declaration);
        DisplayName = description.DisplayName;
        Icon = description.Icon;
        _showLabel = size == RibbonControlSize.Medium;
        _labelChanged = labelChanged;
    }
    public FrameworkElement Control { get; }
    public string GroupName { get; }
    public string DisplayName { get; }
    public ImageSource? Icon { get; }
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
/// Eligible only in Simplified mode; QAT and classic layout remain independent.
/// </summary>
[TemplatePart(Name = "PART_TabSelector", Type = typeof(Selector))]
[TemplatePart(Name = "PART_AvailableList", Type = typeof(ListBox))]
[TemplatePart(Name = "PART_CurrentList", Type = typeof(ListBox))]
[TemplatePart(Name = "PART_AddButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_RemoveButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_UpButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_DownButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_ResetButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_ResetAllButton", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_ApplyButton", Type = typeof(ButtonBase))]
public class RibbonSimplifiedCustomizePage : Control, IRibbonFillPage
{
    /// <summary>Identifies the <see cref="Ribbon"/> dependency property.</summary>
    public static readonly DependencyProperty RibbonProperty = DependencyProperty.Register(
        nameof(Ribbon), typeof(Ribbon), typeof(RibbonSimplifiedCustomizePage),
        new FrameworkPropertyMetadata(null, (d, _) => ((RibbonSimplifiedCustomizePage)d).ChangeRibbon()));

    private static readonly DependencyProperty OwnerLayoutModeProperty = DependencyProperty.Register(
        "OwnerLayoutMode", typeof(RibbonLayoutMode), typeof(RibbonSimplifiedCustomizePage),
        new PropertyMetadata(RibbonLayoutMode.Classic, (d, _) => d.CoerceValue(IsEnabledProperty)));
    private readonly Dictionary<RibbonTab, List<SimplifiedCommandChoice>?> _drafts = new();
    private readonly List<(ButtonBase Button, RoutedEventHandler Handler)> _handlers = new();
    private Selector? _tabs;
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
            (d, value) => (bool)value && ((RibbonSimplifiedCustomizePage)d).Ribbon?.LayoutMode == RibbonLayoutMode.Simplified));
    }

    /// <summary>Initializes a draft editor; loading or retemplating preserves unfinished edits.</summary>
    public RibbonSimplifiedCustomizePage()
    {
        Loaded += (_, _) => { ObserveRibbon(); RefreshTabs(); };
        Unloaded += (_, _) => StopObserving();
    }

    /// <summary>The ribbon whose simplified selections this page edits.</summary>
    public Ribbon? Ribbon
    {
        get => (Ribbon?)GetValue(RibbonProperty);
        set => SetValue(RibbonProperty, value);
    }

    /// <summary>Commits edited tabs, ordering, labels and resets. Hosts can persist the ribbon after this call.</summary>
    public void ApplyChanges()
    {
        if (Ribbon is not { } ribbon) return;
        bool changed = _drafts.Count > 0;
        foreach (var pair in _drafts) ribbon.SetSimplifiedChoices(pair.Key, pair.Value, false);
        _drafts.Clear();
        if (changed) ribbon.NotifySimplifiedCustomizationChanged();
        RefreshTabs();
    }

    /// <summary>Discards uncommitted edits and reloads the current saved selections.</summary>
    public void CancelChanges()
    {
        _drafts.Clear();
        RefreshTabs();
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        foreach (var pair in _handlers) pair.Button.Click -= pair.Handler;
        _handlers.Clear();
        if (_tabs is not null) _tabs.SelectionChanged -= OnTabChanged;
        if (_available is not null) _available.SelectionChanged -= OnSelectionChanged;
        if (_current is not null) _current.SelectionChanged -= OnSelectionChanged;
        base.OnApplyTemplate();
        _tabs = GetTemplateChild("PART_TabSelector") as Selector;
        _available = GetTemplateChild("PART_AvailableList") as ListBox;
        _current = GetTemplateChild("PART_CurrentList") as ListBox;
        if (_tabs is not null) _tabs.SelectionChanged += OnTabChanged;
        if (_available is not null) _available.SelectionChanged += OnSelectionChanged;
        if (_current is not null) _current.SelectionChanged += OnSelectionChanged;
        Hook("PART_AddButton", (_, _) => Add());
        Hook("PART_RemoveButton", (_, _) => Remove());
        Hook("PART_UpButton", (_, _) => Move(-1));
        Hook("PART_DownButton", (_, _) => Move(1));
        Hook("PART_ResetButton", (_, _) => Reset(false));
        Hook("PART_ResetAllButton", (_, _) => Reset(true));
        Hook("PART_ApplyButton", (_, _) => ApplyChanges());
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
        _drafts.Clear(); _tab = null;
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

    private void OnSavedChanges(object? sender, EventArgs e) => RefreshTabs();
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();
    private void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing) return;
        _tab = (_tabs?.SelectedItem as SimplifiedTabChoice)?.Tab;
        RefreshLists();
    }

    private void RefreshTabs()
    {
        if (_tabs is null) return;
        _refreshing = true;
        var tabs = Ribbon?.Tabs.Where(RibbonCustomizePage.IsCustomizableTab).ToList() ?? new();
        var entries = tabs.Select(tab => new SimplifiedTabChoice(tab)).ToList();
        _tabs.ItemsSource = entries;
        _tab = tabs.Contains(_tab!) ? _tab : tabs.FirstOrDefault(tab => ReferenceEquals(tab, Ribbon?.SelectedTab)) ?? tabs.FirstOrDefault();
        _tabs.SelectedItem = entries.FirstOrDefault(entry => ReferenceEquals(entry.Tab, _tab));
        _refreshing = false;
        RefreshLists();
    }

    private List<SimplifiedCommandChoice> Choices() => Ribbon is { } ribbon && _tab is { } tab
        ? _drafts.TryGetValue(tab, out var draft) ? draft?.ToList() ?? ribbon.GetSimplifiedDefaults(tab) : ribbon.GetSimplifiedChoices(tab)
        : new();

    private void RefreshLists(FrameworkElement? selected = null)
    {
        if (_available is null || _current is null) return;
        var sources = _tab is { } tab ? Controls.Ribbon.SimplifiedSources(tab, true) : new();
        var available = new ListCollectionView(sources.Select(source => new SimplifiedCustomizeEntry(source, Controls.Ribbon.GetSimplifiedSize(source.Control))).ToList());
        available.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SimplifiedCustomizeEntry.GroupName)));
        _available.ItemsSource = available;
        var entries = new ObservableCollection<SimplifiedCustomizeEntry>();
        foreach (var choice in Choices())
        {
            var source = sources.FirstOrDefault(entry => Controls.Ribbon.SameSimplifiedCommand(entry.Control, choice.Source));
            if (source is null) continue;
            entries.Add(new SimplifiedCustomizeEntry(source, choice.Size, show => ChangeLabel(source.Control, show)));
        }
        _current.ItemsSource = entries;
        _current.SelectedItem = entries.FirstOrDefault(entry => ReferenceEquals(entry.Control, selected));
        UpdateButtons();
    }

    private void SaveDraft(List<SimplifiedCommandChoice> choices)
    {
        if (_tab is { } tab) _drafts[tab] = choices;
        UpdateButtons();
    }

    private void ChangeLabel(FrameworkElement source, bool show)
    {
        var choices = Choices();
        int index = choices.FindIndex(choice => ReferenceEquals(choice.Source, source));
        if (index < 0) return;
        choices[index] = new SimplifiedCommandChoice(source, show ? RibbonControlSize.Medium : RibbonControlSize.Small);
        SaveDraft(choices);
    }

    private void Add()
    {
        if (!IsEnabled || _available?.SelectedItem is not SimplifiedCustomizeEntry entry) return;
        var choices = Choices();
        if (choices.Any(choice => Controls.Ribbon.SameSimplifiedCommand(choice.Source, entry.Control))) return;
        choices.Add(new SimplifiedCommandChoice(entry.Control, Controls.Ribbon.GetSimplifiedSize(entry.Control)));
        SaveDraft(choices); RefreshLists(entry.Control);
    }

    private void Remove()
    {
        if (!IsEnabled || _current?.SelectedItem is not SimplifiedCustomizeEntry entry) return;
        var choices = Choices(); choices.RemoveAll(choice => ReferenceEquals(choice.Source, entry.Control));
        SaveDraft(choices); RefreshLists();
    }

    private void Move(int delta)
    {
        if (!IsEnabled || _current?.SelectedItem is not SimplifiedCustomizeEntry entry) return;
        var choices = Choices();
        int index = choices.FindIndex(choice => ReferenceEquals(choice.Source, entry.Control));
        int target = index + delta;
        if (index < 0 || target < 0 || target >= choices.Count) return;
        (choices[index], choices[target]) = (choices[target], choices[index]);
        SaveDraft(choices); RefreshLists(entry.Control);
    }

    private void Reset(bool all)
    {
        if (!IsEnabled || Ribbon is null) return;
        foreach (var tab in Ribbon.Tabs.Where(tab => RibbonCustomizePage.IsCustomizableTab(tab) && (all || ReferenceEquals(tab, _tab))))
            _drafts[tab] = null;
        RefreshLists();
    }

    private void UpdateButtons()
    {
        var choices = Choices();
        int index = _current?.SelectedItem is SimplifiedCustomizeEntry entry ? choices.FindIndex(choice => ReferenceEquals(choice.Source, entry.Control)) : -1;
        Enable("PART_AddButton", _available?.SelectedItem is SimplifiedCustomizeEntry available && !choices.Any(choice => Controls.Ribbon.SameSimplifiedCommand(choice.Source, available.Control)));
        Enable("PART_RemoveButton", index >= 0);
        Enable("PART_UpButton", index > 0);
        Enable("PART_DownButton", index >= 0 && index < choices.Count - 1);
        Enable("PART_ResetButton", _tab is not null);
        Enable("PART_ResetAllButton", Ribbon?.Tabs.Any(RibbonCustomizePage.IsCustomizableTab) == true);
        Enable("PART_ApplyButton", _drafts.Count > 0);
    }

    private void Enable(string name, bool enabled) => (GetTemplateChild(name) as ButtonBase)?.SetCurrentValue(IsEnabledProperty, enabled);
}
