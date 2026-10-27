// Adapted from WPF v1.1.0 cf1b942; MIT, RibbonKit contributors.
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public class ReductionAlgorithmTests
{
    [Fact]
    public void Default_order_respects_exact_fit_and_exhausts_rightmost_groups_first()
    {
        var widths = new[] { new double[] { 100, 80, 60 }, new double[] { 100, 80, 60 } };
        Assert.Equal(new[] { 0, 0 }, ReductionAlgorithm.ComputeStates(200, widths));
        Assert.Equal(new[] { 0, 1 }, ReductionAlgorithm.ComputeStates(190, widths));
        Assert.Equal(new[] { 1, 2 }, ReductionAlgorithm.ComputeStates(145, widths));
        Assert.Equal(new[] { 2, 2 }, ReductionAlgorithm.ComputeStates(50, widths));
    }

    [Fact]
    public void Custom_order_prioritizes_groups_and_respects_fixed_or_omitted_groups()
    {
        var widths = new[] { new double[] { 100, 80, 60 }, new double[] { 100, 80, 60 } };
        Assert.Equal(new[] { 1, 0 }, ReductionAlgorithm.ComputeStates(190, widths, [0, 1]));
        Assert.Equal(new[] { 2, 1 }, ReductionAlgorithm.ComputeStates(140, widths, [0, 1]));
        Assert.Equal(new[] { 0, 2 }, ReductionAlgorithm.ComputeStates(50, widths, [1]));
        widths[1] = [100];
        Assert.Equal(new[] { 2, 0 }, ReductionAlgorithm.ComputeStates(160, widths));
    }

    [Fact]
    public void Steps_that_widen_or_do_not_meaningfully_shrink_a_group_are_skipped()
    {
        Assert.Equal(new[] { 2 }, ReductionAlgorithm.ComputeStates(60, [[100, 120, 50]], [0]));
        Assert.Equal(new[] { 3 }, ReductionAlgorithm.ComputeStates(90, [[100, 100, 100 - 1e-14, 70]]));
    }

    [Fact]
    public void Fractional_exact_fit_ignores_binary_rounding_noise() =>
        Assert.Equal(new[] { 0, 0 }, ReductionAlgorithm.ComputeStates(0.3, [[0.1, 0.05], [0.2, 0.1]]));

    [Fact]
    public void Empty_input_returns_empty_result() =>
        Assert.Empty(ReductionAlgorithm.ComputeStates(100, []));

    [Fact]
    public void Invalid_tables_widths_and_order_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => ReductionAlgorithm.ComputeStates(100, null!));
        Assert.Throws<ArgumentException>(() => ReductionAlgorithm.ComputeStates(100, [[]]));
        Assert.Throws<ArgumentOutOfRangeException>(() => ReductionAlgorithm.ComputeStates(-1, [[100, 50]]));
        Assert.Throws<ArgumentOutOfRangeException>(() => ReductionAlgorithm.ComputeStates(100, [[100, double.NaN]]));
        Assert.Throws<ArgumentException>(() => ReductionAlgorithm.ComputeStates(50, [[100, 50]], [1]));
    }
}
