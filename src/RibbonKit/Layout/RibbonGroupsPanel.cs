using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Controls;

namespace RibbonKit.Layout;

/// <summary>
/// Arranges <see cref="RibbonGroup"/> children horizontally and drives the adaptive
/// sizing engine: when the available width is insufficient, groups are reduced — by
/// default collapsing to a single flyout button like modern Office (see
/// <see cref="RibbonGroup.ReductionMode"/>).
/// </summary>
/// <remarks>
/// <para>
/// Reduction order: groups with an explicit <see cref="RibbonGroup.ReductionPriority"/>
/// reduce first (highest value first, each fully exhausted before the next). Groups
/// without a priority then reduce largest-first (ties: rightmost first). Groups with
/// <see cref="RibbonGroup.CanResize"/> set to <see langword="false"/> are never reduced.
/// The decision logic lives in <see cref="ReductionAlgorithm"/> so it stays unit-testable.
/// </para>
/// <para>
/// Design note: learning how wide each group is at each size state requires probing the
/// live controls, which mutates their size properties and invalidates large parts of the
/// visual tree. Doing that on every measure pass causes layout storms (UI freezes and
/// flickering during window resize), so the probe runs ONCE and its result is cached.
/// Every subsequent pass is pure arithmetic over the cached widths; control sizes only
/// actually change at the moment a group crosses a reduction threshold. The cache is
/// rebuilt when children are added/removed or when a group's content or layout policy
/// changes (see <see cref="InvalidateStateCache"/>).
/// </para>
/// </remarks>
public class RibbonGroupsPanel : Panel
{
    private double[][]? _stateWidths;
    private RibbonGroupSizeState[][]? _stateMaps;
    private bool _isMeasuring;
    private RibbonScrollContentHost? _scrollHost;

    /// <summary>Initializes the panel and caches the enclosing scroller once the tree is connected.</summary>
    public RibbonGroupsPanel()
    {
        // The visual parent chain isn't reliably walkable during an items-host panel's MeasureOverride,
        // so we resolve the scroller at Loaded (tree fully connected) and cache it. See ReportContentWidth.
        Loaded += (_, _) => _scrollHost ??= FindScrollHost();
    }

    /// <summary>
    /// Discards the cached per-state group widths so the next measure pass re-probes.
    /// Called automatically when children, group content, or group layout policy
    /// change; call it manually after other runtime changes that affect control sizes
    /// (e.g. font changes).
    /// </summary>
    public void InvalidateStateCache()
    {
        _stateWidths = null;
        _stateMaps = null;
        InvalidateMeasure();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        int count = InternalChildren.Count;
        if (count == 0)
        {
            return new Size(0, 0);
        }

        _isMeasuring = true;
        try
        {
            var probeSize = new Size(double.PositiveInfinity, availableSize.Height);

            if (_stateWidths is null || _stateMaps is null || _stateWidths.Length != count)
            {
                ProbeStateWidths(probeSize);
            }

            // Reduce toward the available width. When we sit inside a RibbonScrollContentHost with
            // ConstrainChildWidth set (the groups row), the host measures us AT the viewport width, so
            // availableSize.Width is exactly the visible area: groups reduce to fit the viewport first,
            // and only the leftover overflow scrolls. See RibbonScrollContentHost.MeasureOverride.
            int[] order = BuildReductionOrder(_stateWidths!);
            int[] states = ReductionAlgorithm.ComputeStates(availableSize.Width, _stateWidths!, order);

            double totalWidth = 0;
            double maxHeight = 0;
            for (int i = 0; i < count; i++)
            {
                UIElement child = InternalChildren[i];
                if (child is RibbonGroup group && group.CanResize)
                {
                    RibbonGroupSizeState desired = _stateMaps![i][states[i]];
                    if (group.SizeState != desired)
                    {
                        // Threshold crossing: this is the only moment control sizes change.
                        group.SetSizeState(desired);
                    }
                }

                child.Measure(probeSize);
                totalWidth += child.DesiredSize.Width;
                maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
            }

            // Report the TRUE (unclamped) content width to the scroller. WPF clamps this method's
            // returned DesiredSize to availableSize, so once the row is fully reduced but still wider
            // than the viewport, the host can't see the overflow from our DesiredSize alone — it reads
            // the reported width instead to decide whether the chevrons should appear.
            (_scrollHost ??= FindScrollHost())?.ReportContentWidth(totalWidth);

            return new Size(totalWidth, maxHeight);
        }
        finally
        {
            _isMeasuring = false;
        }
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        double x = 0;
        foreach (UIElement child in InternalChildren)
        {
            double width = child.DesiredSize.Width;
            child.Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width;
        }

        return finalSize;
    }

    /// <summary>Walks up to the enclosing <see cref="RibbonScrollContentHost"/> (the groups scroller), or null.</summary>
    private RibbonScrollContentHost? FindScrollHost()
    {
        DependencyObject? parent = VisualTreeHelper.GetParent(this);
        while (parent != null)
        {
            if (parent is RibbonScrollContentHost host)
            {
                return host;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }

    /// <inheritdoc />
    protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
    {
        base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        _stateWidths = null;
        _stateMaps = null;
    }

    /// <summary>
    /// Ignores child desired-size changes caused by the panel's own probing and
    /// threshold switching during <see cref="MeasureOverride"/>. Without this guard
    /// each change would re-invalidate the panel and create a layout feedback loop.
    /// </summary>
    protected override void OnChildDesiredSizeChanged(UIElement child)
    {
        if (_isMeasuring)
        {
            return;
        }

        base.OnChildDesiredSizeChanged(child);
    }

    /// <summary>
    /// One-time probe: measures every resizable group at each state of its reduction
    /// sequence to build the width table the algorithm works from. Non-resizable groups
    /// and non-group children get a single-entry table (they can never shrink).
    /// </summary>
    private void ProbeStateWidths(Size probeSize)
    {
        int count = InternalChildren.Count;
        var widths = new double[count][];
        var maps = new RibbonGroupSizeState[count][];

        for (int i = 0; i < count; i++)
        {
            UIElement child = InternalChildren[i];
            if (child is RibbonGroup group && group.CanResize)
            {
                maps[i] = GetStateSequence(group.ReductionMode);
                widths[i] = new double[maps[i].Length];
                for (int s = 0; s < maps[i].Length; s++)
                {
                    group.SetSizeState(maps[i][s]);
                    group.Measure(probeSize);
                    widths[i][s] = group.DesiredSize.Width;
                }
            }
            else
            {
                if (child is RibbonGroup fixedGroup)
                {
                    fixedGroup.SetSizeState(RibbonGroupSizeState.Large);
                }

                child.Measure(probeSize);
                widths[i] = new[] { child.DesiredSize.Width };
                maps[i] = new[] { RibbonGroupSizeState.Large };
            }
        }

        _stateWidths = widths;
        _stateMaps = maps;
    }

    /// <summary>Maps a reduction mode to the sequence of states the group steps through.</summary>
    private static RibbonGroupSizeState[] GetStateSequence(RibbonGroupReductionMode mode) => mode switch
    {
        RibbonGroupReductionMode.ResizeThenCollapse => new[]
        {
            RibbonGroupSizeState.Large,
            RibbonGroupSizeState.Medium,
            RibbonGroupSizeState.Small,
            RibbonGroupSizeState.Collapsed,
        },
        RibbonGroupReductionMode.Resize => new[]
        {
            RibbonGroupSizeState.Large,
            RibbonGroupSizeState.Medium,
            RibbonGroupSizeState.Small,
        },
        _ => new[]
        {
            RibbonGroupSizeState.Large,
            RibbonGroupSizeState.Collapsed,
        },
    };

    /// <summary>
    /// Builds the reduction order: explicitly prioritized groups first (highest
    /// <see cref="RibbonGroup.ReductionPriority"/> first, ties rightmost-first), then
    /// unprioritized groups largest-first (ties rightmost-first). Non-resizable groups
    /// and non-group children are excluded.
    /// </summary>
    private int[] BuildReductionOrder(double[][] stateWidths)
    {
        var prioritized = new List<(int Index, int Priority)>();
        var unprioritized = new List<(int Index, double LargeWidth)>();

        for (int i = 0; i < InternalChildren.Count; i++)
        {
            if (InternalChildren[i] is not RibbonGroup group || !group.CanResize)
            {
                continue;
            }

            if (group.ReductionPriority is int priority)
            {
                prioritized.Add((i, priority));
            }
            else
            {
                unprioritized.Add((i, stateWidths[i][0]));
            }
        }

        return prioritized
            .OrderByDescending(g => g.Priority)
            .ThenByDescending(g => g.Index)
            .Select(g => g.Index)
            .Concat(unprioritized
                .OrderByDescending(g => g.LargeWidth)
                .ThenByDescending(g => g.Index)
                .Select(g => g.Index))
            .ToArray();
    }
}
