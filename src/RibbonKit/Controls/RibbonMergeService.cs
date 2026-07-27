using System.Windows;
using System.Windows.Data;

namespace RibbonKit.Controls;

/// <summary>
/// Owns tab-merging bookkeeping for one <see cref="Ribbon"/>: which sources are merged, which tabs
/// each contributed, and where a source's tabs belong in the strip.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ordering.</b> Index arithmetic at merge time is not stable across repeated merge/unmerge
/// cycles, so every tab in the strip is given a sort key instead and inserts land at the first
/// position whose key is greater. Host-declared tabs share the key <c>(0, -1)</c>; a merged tab's
/// key is <c>(source.Order, sequence)</c> where <c>sequence</c> is assigned the FIRST time a source
/// merges into this ribbon and reused forever after. That gives three properties the plan asked
/// for: sources with the same <c>Order</c> keep their first-merge relative order, a source that
/// unmerges and re-merges returns to the same slot, and a negative <c>Order</c> sorts before the
/// host's own tabs (which is why host tabs use -1 rather than 0 as their sequence).
/// </para>
/// <para>
/// <b>Customization.</b> Merged tabs carry the read-only attached flag
/// <see cref="Ribbon.IsMergedProperty"/>, which <see cref="RibbonCustomizationSerializer"/> and
/// <see cref="RibbonCustomizePage"/> use to skip them entirely — see
/// docs/06-MERGE-AND-MODAL-PLAN.md §5.2.
/// </para>
/// </remarks>
internal sealed class RibbonMergeService
{
    // Sequence for host-declared tabs. Lower than any merged tab's sequence (which starts at 0),
    // so an Order-0 source appends AFTER the host's tabs while a negative Order lands before them.
    private const int HostSequence = -1;

    private readonly Ribbon _ribbon;
    private readonly List<MergeRecord> _records = new();

    // First-merge sequence per source, kept for the ribbon's lifetime so re-merging is stable.
    private readonly Dictionary<RibbonMergeSource, int> _sequence = new();
    private int _nextSequence;

    // Set while this service is mutating Ribbon.Tabs, so its own collection notifications don't
    // feed back in as "someone removed a merged tab behind our back".
    private bool _applying;

    internal RibbonMergeService(Ribbon ribbon) => _ribbon = ribbon;

    /// <summary>The sources currently merged into this ribbon, in merge order.</summary>
    internal IReadOnlyList<RibbonMergeSource> Sources =>
        _records.Select(record => record.Source).ToList();

    /// <summary>Whether <paramref name="source"/> is currently merged into this ribbon.</summary>
    internal bool IsMerged(RibbonMergeSource source) =>
        _records.Any(record => ReferenceEquals(record.Source, source));

    /// <summary>
    /// Inserts every tab of <paramref name="source"/> into the host strip. Returns
    /// <see langword="false"/> when the source is already merged here.
    /// </summary>
    internal bool Merge(RibbonMergeSource source)
    {
        if (IsMerged(source))
        {
            return false;
        }

        if (source.MergedInto is { } other && !ReferenceEquals(other, _ribbon))
        {
            throw new InvalidOperationException(
                "The merge source is already merged into a different ribbon. Unmerge it first.");
        }

        if (!_sequence.TryGetValue(source, out int sequence))
        {
            sequence = _nextSequence++;
            _sequence[source] = sequence;
        }

        var record = new MergeRecord(source, sequence);

        _applying = true;
        try
        {
            foreach (RibbonTab tab in source.Tabs)
            {
                if (_ribbon.Tabs.Contains(tab))
                {
                    continue; // Defensive: the same tab declared in two sources.
                }

                Ribbon.SetIsMergedInternal(tab, true);
                Ribbon.SetMergeSourceInternal(tab, source);

                // Bind (not assign) so a child view model swapped later still reaches the tab.
                // Only when the app hasn't pinned a DataContext on the tab itself.
                if (tab.ReadLocalValue(FrameworkElement.DataContextProperty) == DependencyProperty.UnsetValue)
                {
                    tab.SetBinding(
                        FrameworkElement.DataContextProperty,
                        new Binding(nameof(FrameworkElement.DataContext)) { Source = source });
                    record.BoundDataContext.Add(tab);
                }

                _ribbon.Tabs.Insert(FindInsertIndex(source.Order, sequence), tab);
                record.Tabs.Add(tab);
            }

            _records.Add(record);
            source.SetMergedInto(_ribbon);
        }
        finally
        {
            _applying = false;
        }

        _ribbon.OnMergeChanged();
        return true;
    }

    /// <summary>
    /// Removes every tab <paramref name="source"/> contributed. Returns <see langword="false"/>
    /// when the source is not merged here.
    /// </summary>
    internal bool Unmerge(RibbonMergeSource source)
    {
        MergeRecord? record = _records.FirstOrDefault(r => ReferenceEquals(r.Source, source));
        if (record is null)
        {
            return false;
        }

        _applying = true;
        try
        {
            foreach (RibbonTab tab in record.Tabs)
            {
                // Removing the modal tab ends modal mode — Ribbon.OnTabsCollectionChanged routes
                // that through RibbonModalScope, so nothing extra is needed here.
                _ribbon.Tabs.Remove(tab);

                if (record.BoundDataContext.Contains(tab))
                {
                    BindingOperations.ClearBinding(tab, FrameworkElement.DataContextProperty);
                }

                Ribbon.SetIsMergedInternal(tab, false);
                Ribbon.SetMergeSourceInternal(tab, null);
            }

            _records.Remove(record);
            source.SetMergedInto(null);
        }
        finally
        {
            _applying = false;
        }

        _ribbon.OnMergeChanged();
        return true;
    }

    /// <summary>
    /// Unmerges every source and returns them in merge order, for a caller that is about to
    /// rebuild <see cref="Ribbon.Tabs"/> wholesale. Pair with <see cref="Remerge"/>.
    /// </summary>
    /// <remarks>
    /// <c>RibbonCustomizationSerializer.Apply</c> clears and re-adds the whole collection, which
    /// would strand merged tabs at the end with stale records. Unmerging first and re-merging after
    /// is both simpler and more predictable than trying to re-assert positions afterwards
    /// (docs/06-MERGE-AND-MODAL-PLAN.md §5.3), and customization application is a rare,
    /// user-initiated operation, so the extra churn costs nothing.
    /// </remarks>
    internal List<RibbonMergeSource> UnmergeAll()
    {
        var sources = _records.Select(record => record.Source).ToList();
        foreach (RibbonMergeSource source in sources)
        {
            Unmerge(source);
        }

        return sources;
    }

    /// <summary>Re-merges sources previously taken out by <see cref="UnmergeAll"/>.</summary>
    internal void Remerge(List<RibbonMergeSource> sources)
    {
        foreach (RibbonMergeSource source in sources)
        {
            Merge(source);
        }
    }

    /// <summary>
    /// A tab left <see cref="Ribbon.Tabs"/> by some other path (the customize page deleting it, an
    /// app mutating the collection). Drops it from its record so unmerge doesn't chase a ghost, and
    /// retires the source once it has no tabs left.
    /// </summary>
    internal void OnTabRemoved(RibbonTab tab)
    {
        if (_applying || !Ribbon.GetIsMerged(tab))
        {
            return;
        }

        MergeRecord? record = _records.FirstOrDefault(r => r.Tabs.Contains(tab));
        if (record is null)
        {
            return;
        }

        record.Tabs.Remove(tab);
        record.BoundDataContext.Remove(tab);
        BindingOperations.ClearBinding(tab, FrameworkElement.DataContextProperty);
        Ribbon.SetIsMergedInternal(tab, false);
        Ribbon.SetMergeSourceInternal(tab, null);

        if (record.Tabs.Count == 0)
        {
            _records.Remove(record);
            record.Source.SetMergedInto(null);
        }
    }

    // First position whose sort key is greater than the incoming tab's; append when none is.
    private int FindInsertIndex(int order, int sequence)
    {
        for (int i = 0; i < _ribbon.Tabs.Count; i++)
        {
            (int Order, int Sequence) key = KeyOf(_ribbon.Tabs[i]);
            if (key.Order > order || (key.Order == order && key.Sequence > sequence))
            {
                return i;
            }
        }

        return _ribbon.Tabs.Count;
    }

    private (int Order, int Sequence) KeyOf(RibbonTab tab)
    {
        if (Ribbon.GetMergeSource(tab) is { } source
            && _sequence.TryGetValue(source, out int sequence))
        {
            return (source.Order, sequence);
        }

        return (0, HostSequence);
    }

    private sealed class MergeRecord
    {
        internal MergeRecord(RibbonMergeSource source, int sequence)
        {
            Source = source;
            Sequence = sequence;
        }

        internal RibbonMergeSource Source { get; }

        internal int Sequence { get; }

        /// <summary>The tabs actually inserted, in insertion order.</summary>
        internal List<RibbonTab> Tabs { get; } = new();

        /// <summary>Tabs whose DataContext this merge bound (and must therefore unbind).</summary>
        internal List<RibbonTab> BoundDataContext { get; } = new();
    }
}
