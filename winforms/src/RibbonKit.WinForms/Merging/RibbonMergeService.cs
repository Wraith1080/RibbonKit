namespace RibbonKit.WinForms;

// Projection is kept outside adaptive layout. Authored collections retain their
// single logical owner, bindings, IDs and disposal responsibility throughout.
internal sealed class RibbonMergeService(RibbonControl ribbon) : IDisposable
{
    private readonly Dictionary<RibbonMergeSource, long> _known = [];
    private readonly HashSet<RibbonMergeSource> _active = [];
    private long _nextSequence;
    internal IEnumerable<RibbonElement> ActiveElements => _active.SelectMany(s => s.Tree());
    internal IEnumerable<RibbonElement> RetainedElements => _known.Keys.SelectMany(s => s.Tree());
    internal IReadOnlyList<RibbonTab> Tabs() =>
        ribbon.Tabs.Select(t => (Tab: t, Order: 0, Sequence: -1L))
            .Concat(_active.Where(s => s.Visible).SelectMany(s => s.Tabs.Where(t => !t.IsDisposed).Select(t => (Tab: t, Order: s.Order, Sequence: _known[s]))))
            .OrderBy(t => t.Order).ThenBy(t => t.Sequence).Select(t => t.Tab).ToArray();
    internal IReadOnlyList<RibbonGroup> Groups(RibbonTab tab) =>
        tab.Groups.Select(g => (Group: g, Order: 0, Sequence: -1L))
            .Concat(!ribbon.Tabs.Contains(tab) ? [] : _active.Where(s => s.Visible).SelectMany(s => s.Groups
                .Where(c => c.Visible && !c.IsDisposed && !c.Group.IsDisposed && ReferenceEquals(c.TargetTab, tab))
                .Select(c => (Group: c.Group, Order: c.Order, Sequence: _known[s]))))
            .OrderBy(g => g.Order).ThenBy(g => g.Sequence).Select(g => g.Group).ToArray();
    internal bool Merge(RibbonMergeSource source)
    {
        ribbon.VerifyAccess(); ArgumentNullException.ThrowIfNull(source); ObjectDisposedException.ThrowIf(ribbon.IsDisposed, ribbon);
        ObjectDisposedException.ThrowIf(source.IsDisposed, source);
        if (_active.Contains(source)) return false;
        if (source.MergedInto is not null) throw new InvalidOperationException("Unmerge the source before attaching it to another ribbon.");
        // Validate every ID before changing either host or retaining native state.
        ribbon.ValidateAddition(source, null);
        if (source.RetainedBy is { } previous && !ReferenceEquals(previous, ribbon)) previous.Merges.Forget(source);
        if (!_known.ContainsKey(source)) _known.Add(source, _nextSequence++);
        source.RetainedBy = ribbon; _active.Add(source); source.Parent = ribbon;
        ribbon.ModelChanged(); source.NotifyMergeChanged();
        return true;
    }
    internal bool Unmerge(RibbonMergeSource source)
    {
        ribbon.VerifyAccess(); ArgumentNullException.ThrowIfNull(source);
        if (!_active.Contains(source)) return false;
        int index = Tabs().ToList().IndexOf(ribbon.SelectedTab!);
        // Publish removal before closing/cancelling native presentation. Application
        // lifecycle handlers can dispose the source without recursively unmerging it.
        _active.Remove(source); source.Parent = null;
        if (ribbon.SelectedTab is { } selected && ReferenceEquals(RibbonControl.GetMergeSource(selected), source)) ribbon.RemovingTab(selected, index);
        ribbon.Popups.CloseAll(false); ribbon.Editors.CloseLists();
        foreach (var gallery in source.Tree().OfType<RibbonGalleryItem>().ToArray()) gallery.CancelPreview();
        ribbon.ModelChanged(); if (!source.IsDisposed) source.NotifyMergeChanged();
        return true;
    }
    internal void Forget(RibbonMergeSource source)
    {
        Unmerge(source); _known.Remove(source);
        if (ReferenceEquals(source.RetainedBy, ribbon)) source.RetainedBy = null;
        ribbon.ModelChanged();
    }
    public void Dispose()
    {
        foreach (var source in _known.Keys.ToArray())
        {
            bool active = _active.Remove(source);
            if (active) source.Parent = null;
            source.RetainedBy = null;
            if (active) source.NotifyMergeChanged();
        }
        _known.Clear();
    }
}
