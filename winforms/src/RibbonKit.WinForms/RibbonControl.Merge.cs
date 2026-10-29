using System.ComponentModel;

namespace RibbonKit.WinForms;

public partial class RibbonControl
{
    private RibbonMergeService? _merges;
    internal RibbonMergeService Merges => _merges ??= new(this);
    /// <summary>Projects a borrowed source into this host. Returns false when it is already merged here. The application retains ownership and drives activation.</summary>
    public bool Merge(RibbonMergeSource source)
    {
        ObjectDisposedException.ThrowIf(_disposing || IsDisposed, this);
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsMerged && source.Visible && source.Tabs.Any(t => t.Visible) && CanAnimateHeaderArrival)
        {
            bool merged = false; Motion.Change(false, () => merged = Merges.Merge(source), headers: true); return merged;
        }
        return Merges.Merge(source);
    }
    /// <summary>Removes this source's presentation by identity. Models remain owned by the source; QAT references and native presenters are parked for reuse.</summary>
    public bool Unmerge(RibbonMergeSource source) => Merges.Unmerge(source);
    /// <summary>Gets a snapshot of authored and currently merged tabs in presentation order. Tabs remains the host's authored collection.</summary>
    [Browsable(false)]
    public IReadOnlyList<RibbonTab> GetPresentedTabs() => ModalTab is { } modal ? UnfilteredTabs.Where(t => ReferenceEquals(t, modal)).ToArray() : UnfilteredTabs.Where(t => CommandCatalog?.Structure.Hidden(t) != true).ToArray();
    /// <summary>Gets a snapshot of a presented tab's authored and contributed groups. The tab's Groups collection retains authored ownership.</summary>
    public IReadOnlyList<RibbonGroup> GetPresentedGroups(RibbonTab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        if (!GetPresentedTabs().Contains(tab)) return [];
        return _merges?.Groups(tab) ?? tab.Groups.ToArray();
    }
    /// <summary>Gets the source owning a transient model, including while it is unmerged, or null for ordinary authored models. Future structural persistence must exclude these models and their QAT references.</summary>
    public static RibbonMergeSource? GetMergeSource(RibbonElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        for (RibbonElement? node = element; node is not null; node = node.Parent as RibbonElement)
            if (node is RibbonMergeSource source) return source;
        if (RibbonQuickAccess.Source(element) is { } borrowed && !ReferenceEquals(borrowed, element))
            return borrowed.IsDisposed ? element.MergeProvenance : GetMergeSource(borrowed);
        return null;
    }
    internal IEnumerable<RibbonElement> RetainedElements => AllElements.Concat(_merges?.RetainedElements ?? []).Concat(CommandCatalog.Structure.Retained).Distinct();
}
