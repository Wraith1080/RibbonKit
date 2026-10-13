using System.Windows;
using System.Windows.Controls.Primitives;
using RibbonKit.Localization;

namespace RibbonKit.Controls;

public partial class RibbonSimplifiedCustomizePage
{
    // Detached structures and source references keep Cancel free of live mutations/proxy bindings.
    private readonly List<(RibbonTab Tab, RibbonTab? Anchor)> _newTabs = new();
    private readonly Dictionary<RibbonTab, List<(RibbonGroup Group, RibbonGroup? Anchor)>> _newGroups = new();
    private readonly Dictionary<FrameworkElement, string> _renames = new();
    private readonly Dictionary<RibbonGroup, List<FrameworkElement>> _memberships = new();
    private Selector? _groups;
    private RibbonGroup? _group;
    private bool HasDraft => _drafts.Count > 0 || _newTabs.Count > 0 || _newGroups.Count > 0 || _renames.Count > 0 || _memberships.Count > 0;

    private string DraftHeader(FrameworkElement element) => _renames.TryGetValue(element, out string? name) ? name : element switch
    {
        RibbonTab tab => tab.Header?.ToString() ?? string.Empty,
        RibbonGroup group => group.Header?.ToString() ?? string.Empty,
        _ => string.Empty,
    };

    private List<RibbonTab> DraftTabs()
    {
        var tabs = Ribbon?.Tabs.Where(RibbonCustomizePage.IsCustomizableTab).ToList() ?? new();
        if (Ribbon is null) return tabs;
        foreach (var pair in _newTabs)
            tabs.Insert(pair.Anchor is not null && tabs.IndexOf(pair.Anchor) is >= 0 and var index ? index + 1 : tabs.Count, pair.Tab);
        return tabs;
    }

    private List<RibbonGroup> DraftGroups(RibbonTab tab)
    {
        var groups = tab.Groups.Where(group => !Controls.Ribbon.GetIsMerged(group)).ToList();
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
        entries.AddRange(groups.Select(group => new SimplifiedGroupChoice(group, _renames.GetValueOrDefault(group))));
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

    private List<(SimplifiedCommandSource Source, string Path)> AvailableSources()
    {
        var result = new List<(SimplifiedCommandSource Source, string Path)>();
        if (Ribbon is null) return result;
        // Prefer original declarations, keeping custom copies out of command discovery.
        foreach (var tab in Ribbon.Tabs.Where(RibbonCustomizePage.IsCustomizableTab).OrderBy(Controls.Ribbon.GetIsCustom))
        foreach (var source in Controls.Ribbon.SimplifiedSources(tab, true))
            if (!result.Any(pair => Controls.Ribbon.SameSimplifiedCommand(pair.Source.Control, source.Control)))
                result.Add((source, DraftHeader(tab) + " › " + DraftHeader(source.Group)));
        return result;
    }

    private List<SimplifiedCommandSource> DraftSources()
    {
        if (_tab is null) return new();
        var groups = DraftGroups(_tab);
        var sources = Controls.Ribbon.SimplifiedSources(_tab, true).Where(entry => groups.Contains(entry.Group)).ToList();
        foreach (var group in groups)
            if (_memberships.TryGetValue(group, out var members))
                foreach (var source in members)
                    if (!sources.Any(entry => entry.Group == group && Controls.Ribbon.SameSimplifiedCommand(entry.Control, source)))
                        sources.Add(new SimplifiedCommandSource(source, group, source));
        return sources;
    }

    private RibbonGroup? TargetGroup(FrameworkElement source)
    {
        var sources = DraftSources();
        if (_group is null) return sources.FirstOrDefault(entry => Controls.Ribbon.SameSimplifiedCommand(entry.Control, source))?.Group;
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
        var group = RibbonCustomizePage.CreateCustomGroup(); tab.Groups.Add(group);
        _newTabs.Add((tab, _tab)); _tab = tab; _group = group;
        RefreshTabs();
    }

    private void NewGroup()
    {
        if (!IsEnabled || _tab is null) return;
        var group = RibbonCustomizePage.CreateCustomGroup();
        if (!_newGroups.TryGetValue(_tab, out var groups)) _newGroups[_tab] = groups = new();
        groups.Add((group, _group)); _group = group;
        RefreshGroups(); RefreshLists();
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
        if (dialog.ShowDialog() == true && dialog.ItemName is { } name) RenameDraft(target, name);
    }

    internal void RenameDraft(FrameworkElement target, string name)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(name) || !DraftTabs().Any(tab => tab == target || (target is RibbonGroup group && DraftGroups(tab).Contains(group)))) return;
        _renames[target] = name.Trim(); RefreshTabs();
    }

    private void ApplyStructure(Ribbon ribbon)
    {
        var available = AvailableSources().Select(pair => pair.Source.Control).ToList();
        foreach (var pair in _newTabs)
            ribbon.Tabs.Insert(pair.Anchor is not null && ribbon.Tabs.IndexOf(pair.Anchor) is >= 0 and var index ? index + 1 : ribbon.Tabs.Count, pair.Tab);
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
        _drafts.Clear(); _newTabs.Clear(); _newGroups.Clear(); _renames.Clear(); _memberships.Clear();
    }
}
