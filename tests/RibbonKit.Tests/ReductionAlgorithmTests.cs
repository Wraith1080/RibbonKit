using RibbonKit.Layout;
using Xunit;

namespace RibbonKit.Tests;

public class ReductionAlgorithmTests
{
    [Fact]
    public void All_groups_stay_large_when_width_is_sufficient()
    {
        var widths = new[]
        {
            new double[] { 100, 80, 60 },
            new double[] { 100, 80, 60 },
        };

        int[] states = ReductionAlgorithm.ComputeStates(500, widths);

        Assert.Equal(new[] { 0, 0 }, states);
    }

    [Fact]
    public void Default_order_shrinks_rightmost_group_first()
    {
        var widths = new[]
        {
            new double[] { 100, 80, 60 },
            new double[] { 100, 80, 60 },
        };

        // 200 doesn't fit in 190; one step down of the RIGHT group (index 1) does.
        int[] states = ReductionAlgorithm.ComputeStates(190, widths);

        Assert.Equal(new[] { 0, 1 }, states);
    }

    [Fact]
    public void Default_order_walks_left_after_rightmost_is_exhausted()
    {
        var widths = new[]
        {
            new double[] { 100, 80, 60 },
            new double[] { 100, 80, 60 },
        };

        // Right group fully reduced (60) + left group one step (80) = 140 fits in 145.
        int[] states = ReductionAlgorithm.ComputeStates(145, widths);

        Assert.Equal(new[] { 1, 2 }, states);
    }

    [Fact]
    public void All_groups_fully_reduce_when_width_is_too_small_for_anything()
    {
        var widths = new[]
        {
            new double[] { 100, 80, 60 },
            new double[] { 100, 80, 60 },
        };

        int[] states = ReductionAlgorithm.ComputeStates(50, widths);

        Assert.Equal(new[] { 2, 2 }, states);
    }

    [Fact]
    public void Non_adaptive_children_are_skipped_over()
    {
        var widths = new[]
        {
            new double[] { 100, 80, 60 },
            new double[] { 100 },          // non-adaptive: single state
        };

        // Needs 40 saved; right child can't shrink, so the left group reduces fully.
        int[] states = ReductionAlgorithm.ComputeStates(160, widths);

        Assert.Equal(new[] { 2, 0 }, states);
    }

    [Fact]
    public void Infinite_width_keeps_everything_large()
    {
        var widths = new[] { new double[] { 100, 80, 60 } };

        int[] states = ReductionAlgorithm.ComputeStates(double.PositiveInfinity, widths);

        Assert.Equal(new[] { 0 }, states);
    }

    [Fact]
    public void Empty_input_returns_empty_result()
    {
        int[] states = ReductionAlgorithm.ComputeStates(100, Array.Empty<double[]>());

        Assert.Empty(states);
    }

    [Fact]
    public void Custom_reduction_order_is_respected()
    {
        var widths = new[]
        {
            new double[] { 100, 80, 60 },
            new double[] { 100, 80, 60 },
        };

        // Same scenario as the rightmost-first test, but the order says index 0 first.
        int[] states = ReductionAlgorithm.ComputeStates(190, widths, new[] { 0, 1 });

        Assert.Equal(new[] { 1, 0 }, states);
    }

    [Fact]
    public void Each_group_in_the_order_is_exhausted_before_the_next_shrinks()
    {
        var widths = new[]
        {
            new double[] { 100, 80, 60 },
            new double[] { 100, 80, 60 },
        };

        // Order [0, 1]: group 0 must reach its smallest (60) before group 1 shrinks.
        int[] states = ReductionAlgorithm.ComputeStates(140, widths, new[] { 0, 1 });

        Assert.Equal(new[] { 2, 1 }, states);
    }

    [Fact]
    public void Groups_omitted_from_the_order_are_never_reduced()
    {
        var widths = new[]
        {
            new double[] { 100, 80, 60 },
            new double[] { 100, 80, 60 },
        };

        // Only group 1 may shrink; even at an impossible width, group 0 stays large.
        int[] states = ReductionAlgorithm.ComputeStates(50, widths, new[] { 1 });

        Assert.Equal(new[] { 0, 2 }, states);
    }

    [Fact]
    public void Steps_that_would_widen_the_group_are_skipped()
    {
        // Medium (120) is WIDER than Large (100) — e.g. three large buttons whose
        // icon+label medium row is wider than the large stack. The algorithm must jump
        // straight from Large to Small instead of cascading through Medium.
        var widths = new[]
        {
            new double[] { 100, 120, 50 },
        };

        int[] states = ReductionAlgorithm.ComputeStates(60, widths, new[] { 0 });

        Assert.Equal(new[] { 2 }, states);
    }

    [Fact]
    public void Invalid_order_index_throws()
    {
        var widths = new[] { new double[] { 100, 80, 60 } };

        Assert.Throws<ArgumentException>(
            () => ReductionAlgorithm.ComputeStates(50, widths, new[] { 3 }));
    }
}
