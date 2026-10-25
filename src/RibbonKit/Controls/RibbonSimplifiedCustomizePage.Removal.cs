namespace RibbonKit.Controls;

public partial class RibbonSimplifiedCustomizePage
{
    // Keep removals separate from live collections so Cancel, export and imported drafts agree.
    private readonly HashSet<RibbonTab> _removedTabs = new();
    private readonly HashSet<(RibbonTab Tab, RibbonGroup Group)> _removedGroups = new();

    private static bool SameStructure(System.Windows.FrameworkElement a, System.Windows.FrameworkElement b) =>
        a == b || Controls.Ribbon.GetCommandId(a) is { Length: > 0 } id && Controls.Ribbon.GetCommandId(b) == id;

    private bool IsRemovedTab(RibbonTab tab) => Controls.Ribbon.GetIsCustom(tab)
        && _removedTabs.Any(removed => SameStructure(removed, tab));

    private bool IsRemovedGroup(RibbonTab tab, RibbonGroup group) => Controls.Ribbon.GetIsCustom(group)
        && _removedGroups.Any(removed => SameStructure(removed.Tab, tab) && SameStructure(removed.Group, group));

    private bool CanRemoveGroup(RibbonGroup group) => IsEnabled && _tab is not null
        && Controls.Ribbon.GetIsCustom(group) && !Controls.Ribbon.GetIsMerged(group)
        && Controls.Ribbon.GetMergeSource(group) is null && DraftGroups(_tab).Contains(group);

    private void RemoveCustomGroup(RibbonGroup group)
    {
        if (!CanRemoveGroup(group) || _tab is not { } tab) return;
        var sources = DraftSources(tab);
        var choices = Choices().Where(choice => Controls.Ribbon.ResolveSimplifiedChoice(sources, choice)?.Group != group).ToList();
        _removedGroups.Add((tab, group));
        _drafts[tab] = choices;
        _group = null;
        // Clear the removed header/command selection before rebuilding the list.
        if (_current is not null) _current.SelectedItem = null;
        RefreshGroups(); RefreshLists();
    }

    private void RemoveCustomTab()
    {
        if (!IsEnabled || _tab is not { } tab || !Controls.Ribbon.GetIsCustom(tab) || !DraftTabs().Contains(tab)) return;
        _removedTabs.Add(tab);
        _tab = null; _group = null;
        if (_current is not null) _current.SelectedItem = null;
        RefreshTabs();
    }

    private void ApplyRemovals(Ribbon ribbon)
    {
        foreach (var tab in ribbon.Tabs.Where(RibbonCustomizePage.IsCustomizableTab).ToArray())
        {
            if (IsRemovedTab(tab)) { ribbon.Tabs.Remove(tab); continue; }
            foreach (var group in tab.Groups.Where(group => !Controls.Ribbon.GetIsMerged(group)
                && Controls.Ribbon.GetMergeSource(group) is null && IsRemovedGroup(tab, group)).ToArray())
                tab.Groups.Remove(group);
        }
    }

    private static int DraftInsertionIndex<T>(IList<T> live, List<T> draft, T item)
    {
        int position = draft.IndexOf(item);
        for (int i = position - 1; i >= 0; i--)
            if (live.IndexOf(draft[i]) is >= 0 and var previous) return previous + 1;
        for (int i = position + 1; i < draft.Count; i++)
            if (live.IndexOf(draft[i]) is >= 0 and var next) return next;
        return live.Count;
    }
}
