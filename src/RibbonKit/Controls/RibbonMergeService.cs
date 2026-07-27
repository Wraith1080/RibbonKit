using System.Windows;
using System.Windows.Data;

namespace RibbonKit.Controls;

/// <summary>
/// Owns tab-merging bookkeeping for one <see cref="Ribbon"/>: which sources are merged, what each
/// contributed (whole tabs, and groups injected into the host's own tabs), and where that content
/// belongs.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ordering.</b> Index arithmetic at merge time is not stable across repeated merge/unmerge
/// cycles, so every tab in the strip — and every group in a target tab — is given a sort key and
/// inserts land at the first position whose key is greater. Host-declared content shares the key
/// <c>(0, -1)</c>; merged content's key is <c>(order, sequence)</c> where <c>sequence</c> is
/// assigned the FIRST time a source merges into this ribbon and reused forever after. That gives
/// three properties: sources with the same order keep their first-merge relative order, a source
/// that unmerges and re-merges returns to the same slot, and a negative order sorts before the
/// host's own content (which is why the host sequence is -1 rather than 0).
/// </para>
/// <para>
/// <b>Unmerge is removal by reference</b>, never by remembered index — so two sources contributing
/// into the same host tab can unmerge in either order and the host's own groups close back up in
/// their original relative order automatically.
/// </para>
/// <para>
/// <b>Customization.</b> Merged tabs, merged groups and their command controls all carry the
/// read-only attached flag <see cref="Ribbon.IsMergedProperty"/>, which
/// <see cref="RibbonCustomizationSerializer"/> and <see cref="RibbonCustomizePage"/> use to skip
/// them. Quick-access proxies created for merged commands additionally carry
/// <see cref="Ribbon.MergeSourceProperty"/> so they can be parked (disabled, not deleted) while
/// their source is away and revived when it returns — see docs/06-MERGE-AND-MODAL-PLAN.md §5.2/§5.4.
/// </para>
/// </remarks>
internal sealed class RibbonMergeService
{
    // Sort sequence for host-declared content. Lower than any merged sequence (which starts at 0),
    // so an order-0 source appends AFTER the host's content while a negative order lands before it.
    private const int HostSequence = -1;

    private readonly Ribbon _ribbon;
    private readonly List<MergeRecord> _records = new();

    // First-merge sequence per source, kept for the ribbon's lifetime so re-merging is stable.
    private readonly Dictionary<RibbonMergeSource, int> _sequence = new();

    // Sort key per contributed group, so a later contribution into the same host tab can be
    // positioned against it. Entries live only while the group is merged.
    private readonly Dictionary<RibbonGroup, (int Order, int Sequence)> _groupKeys = new();

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
    /// Inserts <paramref name="source"/>'s tabs into the strip and its group contributions into
    /// their target tabs. Returns <see langword="false"/> when the source is already merged here.
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
            MergeTabs(source, sequence, record);
            MergeGroupContributions(source, sequence, record);

            _records.Add(record);
            source.SetMergedInto(_ribbon);

            // Revive any quick-access proxies parked when this source last left.
            SetProxiesEnabled(source, enabled: true);
        }
        finally
        {
            _applying = false;
        }

        _ribbon.OnMergeChanged();
        return true;
    }

    /// <summary>
    /// Removes everything <paramref name="source"/> contributed. Returns <see langword="false"/>
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
            // Groups first: pulling a tab out while its groups are still tracked would leave
            // stale _groupKeys entries behind.
            foreach ((RibbonTab hostTab, RibbonGroup group) in record.Groups)
            {
                hostTab.Groups.Remove(group);
                _groupKeys.Remove(group);
                Unmark(group);
            }

            foreach (RibbonTab tab in record.Tabs)
            {
                // Removing the modal tab ends modal mode — Ribbon.OnTabsCollectionChanged routes
                // that through RibbonModalScope, so nothing extra is needed here.
                _ribbon.Tabs.Remove(tab);

                if (record.BoundDataContext.Contains(tab))
                {
                    BindingOperations.ClearBinding(tab, FrameworkElement.DataContextProperty);
                }

                foreach (RibbonGroup group in tab.Groups)
                {
                    Unmark(group);
                }

                Ribbon.SetIsMergedInternal(tab, false);
                Ribbon.SetMergeSourceInternal(tab, null);
            }

            // Park, don't delete: a proxy whose command has stepped out greys like Office rather
            // than vanishing from the user's quick access toolbar. Its MergeSource marker stays,
            // so the serializer keeps skipping it and Merge can revive it later.
            SetProxiesEnabled(source, enabled: false);

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
    /// would strand merged content with stale records. Unmerging first and re-merging after is both
    /// simpler and more predictable than re-asserting positions afterwards
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
    /// retires the source once nothing of it is left.
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

        foreach (RibbonGroup group in tab.Groups)
        {
            Unmark(group);
        }

        Ribbon.SetIsMergedInternal(tab, false);
        Ribbon.SetMergeSourceInternal(tab, null);

        if (record.Tabs.Count == 0 && record.Groups.Count == 0)
        {
            _records.Remove(record);
            record.Source.SetMergedInto(null);
        }
    }

    private void MergeTabs(RibbonMergeSource source, int sequence, MergeRecord record)
    {
        foreach (RibbonTab tab in source.Tabs)
        {
            if (_ribbon.Tabs.Contains(tab))
            {
                continue; // Defensive: the same tab declared in two sources.
            }

            Ribbon.SetIsMergedInternal(tab, true);
            Ribbon.SetMergeSourceInternal(tab, source);

            foreach (RibbonGroup group in tab.Groups)
            {
                Mark(group, source);
            }

            // Bind (not assign) so a child view model swapped later still reaches the tab, and
            // only when the app hasn't pinned a DataContext on the tab itself.
            if (tab.ReadLocalValue(FrameworkElement.DataContextProperty) == DependencyProperty.UnsetValue)
            {
                tab.SetBinding(
                    FrameworkElement.DataContextProperty,
                    new Binding(nameof(FrameworkElement.DataContext)) { Source = source });
                record.BoundDataContext.Add(tab);
            }

            _ribbon.Tabs.Insert(FindTabInsertIndex(source.Order, sequence), tab);
            record.Tabs.Add(tab);
        }
    }

    private void MergeGroupContributions(RibbonMergeSource source, int sequence, MergeRecord record)
    {
        foreach (RibbonGroupContribution contribution in source.Groups)
        {
            if (contribution.Group is not { } group || contribution.TargetTabId is not { } targetId)
            {
                continue;
            }

            // A source shouldn't break a host that doesn't happen to have the tab it hoped for.
            RibbonTab? hostTab = _ribbon.Tabs.FirstOrDefault(
                tab => !Ribbon.GetIsMerged(tab) && Ribbon.GetCommandId(tab) == targetId);

            if (hostTab is null || hostTab.Groups.Contains(group))
            {
                continue;
            }

            Mark(group, source);
            _groupKeys[group] = (contribution.Order, sequence);
            hostTab.Groups.Insert(FindGroupInsertIndex(hostTab, contribution.Order, sequence), group);
            record.Groups.Add((hostTab, group));
        }
    }

    // Flags a group and its command controls as merged, so customization skips them and any
    // quick-access proxy of theirs can be recognised later. Group content is walked with the same
    // catalog the customization pages use, so "what counts as a command" stays in one place.
    private static void Mark(RibbonGroup group, RibbonMergeSource source)
    {
        Ribbon.SetIsMergedInternal(group, true);
        Ribbon.SetMergeSourceInternal(group, source);

        foreach (FrameworkElement control in RibbonCommandCatalog.CollectControls(group))
        {
            Ribbon.SetIsMergedInternal(control, true);
            Ribbon.SetMergeSourceInternal(control, source);
        }
    }

    private static void Unmark(RibbonGroup group)
    {
        Ribbon.SetIsMergedInternal(group, false);
        Ribbon.SetMergeSourceInternal(group, null);

        foreach (FrameworkElement control in RibbonCommandCatalog.CollectControls(group))
        {
            Ribbon.SetIsMergedInternal(control, false);
            Ribbon.SetMergeSourceInternal(control, null);
        }
    }

    // Quick-access proxies are tagged with their source's provenance when created (see
    // Ribbon.AddToQuickAccess), so parking and reviving is a flag sweep rather than a tree walk —
    // which matters because an unmerged tab is no longer in any tree to walk.
    private void SetProxiesEnabled(RibbonMergeSource source, bool enabled)
    {
        foreach (object item in _ribbon.QuickAccessItems)
        {
            if (item is FrameworkElement element
                && ReferenceEquals(Ribbon.GetMergeSource(element), source))
            {
                element.IsEnabled = enabled;
            }
        }
    }

    // First position whose sort key is greater than the incoming content's; append when none is.
    private int FindTabInsertIndex(int order, int sequence)
    {
        for (int i = 0; i < _ribbon.Tabs.Count; i++)
        {
            if (IsGreater(TabKey(_ribbon.Tabs[i]), order, sequence))
            {
                return i;
            }
        }

        return _ribbon.Tabs.Count;
    }

    private int FindGroupInsertIndex(RibbonTab hostTab, int order, int sequence)
    {
        for (int i = 0; i < hostTab.Groups.Count; i++)
        {
            if (IsGreater(GroupKey(hostTab.Groups[i]), order, sequence))
            {
                return i;
            }
        }

        return hostTab.Groups.Count;
    }

    private static bool IsGreater((int Order, int Sequence) key, int order, int sequence) =>
        key.Order > order || (key.Order == order && key.Sequence > sequence);

    private (int Order, int Sequence) TabKey(RibbonTab tab) =>
        Ribbon.GetMergeSource(tab) is { } source && _sequence.TryGetValue(source, out int sequence)
            ? (source.Order, sequence)
            : (0, HostSequence);

    private (int Order, int Sequence) GroupKey(RibbonGroup group) =>
        _groupKeys.TryGetValue(group, out (int Order, int Sequence) key) ? key : (0, HostSequence);

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

        /// <summary>Groups injected into host tabs, paired with the tab that received them.</summary>
        internal List<(RibbonTab HostTab, RibbonGroup Group)> Groups { get; } = new();

        /// <summary>Tabs whose DataContext this merge bound (and must therefore unbind).</summary>
        internal List<RibbonTab> BoundDataContext { get; } = new();
    }
}
