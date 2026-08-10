using System.Windows;
using RibbonKit.Controls;
using RibbonKit.Layout;
using Xunit;

namespace RibbonKit.Tests;

public class RibbonGroupsPanelTests
{
    [Fact]
    public void Highest_explicit_priority_reduces_first() => Sta.Run(() =>
    {
        var left = Group(100, 50, priority: null);
        var middle = Group(100, 50, priority: 5);
        var right = Group(100, 50, priority: 10);

        Measure(250, left, middle, right);

        Assert.Equal(RibbonGroupSizeState.Large, left.SizeState);
        Assert.Equal(RibbonGroupSizeState.Large, middle.SizeState);
        Assert.Equal(RibbonGroupSizeState.Collapsed, right.SizeState);
    });

    [Fact]
    public void Explicit_priority_ties_reduce_rightmost_first() => Sta.Run(() =>
    {
        var left = Group(100, 50, priority: 10);
        var right = Group(100, 50, priority: 10);

        Measure(150, left, right);

        Assert.Equal(RibbonGroupSizeState.Large, left.SizeState);
        Assert.Equal(RibbonGroupSizeState.Collapsed, right.SizeState);
    });

    [Fact]
    public void Unprioritized_groups_reduce_largest_then_rightmost() => Sta.Run(() =>
    {
        var left = Group(120, 60, priority: null);
        var middle = Group(100, 50, priority: null);
        var right = Group(120, 60, priority: null);

        Measure(280, left, middle, right);

        Assert.Equal(RibbonGroupSizeState.Large, left.SizeState);
        Assert.Equal(RibbonGroupSizeState.Large, middle.SizeState);
        Assert.Equal(RibbonGroupSizeState.Collapsed, right.SizeState);
    });

    [Fact]
    public void Non_resizable_group_stays_large_and_is_excluded_from_order() => Sta.Run(() =>
    {
        var flexible = Group(100, 50, priority: null);
        var fixedGroup = Group(200, 20, priority: 100);
        fixedGroup.CanResize = false;

        Measure(250, flexible, fixedGroup);

        Assert.Equal(RibbonGroupSizeState.Collapsed, flexible.SizeState);
        Assert.Equal(RibbonGroupSizeState.Large, fixedGroup.SizeState);
    });

    [Theory]
    [InlineData(110, RibbonGroupSizeState.Medium)]
    [InlineData(90, RibbonGroupSizeState.Small)]
    [InlineData(70, RibbonGroupSizeState.Collapsed)]
    public void Resize_then_collapse_walks_the_full_state_map(
        double availableWidth,
        RibbonGroupSizeState expected) => Sta.Run(() =>
    {
        var group = new MeasuredGroup(new Dictionary<RibbonGroupSizeState, double>
        {
            [RibbonGroupSizeState.Large] = 120,
            [RibbonGroupSizeState.Medium] = 100,
            [RibbonGroupSizeState.Small] = 80,
            [RibbonGroupSizeState.Collapsed] = 40,
        })
        {
            ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse,
        };

        Measure(availableWidth, group);

        Assert.Equal(expected, group.SizeState);
    });

    [Fact]
    public void Invalidating_the_cache_reprobes_runtime_width_changes() => Sta.Run(() =>
    {
        var group = Group(100, 50, priority: null);
        var panel = new RibbonGroupsPanel();
        panel.Children.Add(group);
        panel.Measure(new Size(90, 100));
        Assert.Equal(RibbonGroupSizeState.Collapsed, group.SizeState);

        group.SetWidth(RibbonGroupSizeState.Large, 80);
        panel.InvalidateStateCache();
        panel.Measure(new Size(90, 100));

        Assert.Equal(RibbonGroupSizeState.Large, group.SizeState);
    });

    private static MeasuredGroup Group(double large, double collapsed, int? priority) => new(
        new Dictionary<RibbonGroupSizeState, double>
        {
            [RibbonGroupSizeState.Large] = large,
            [RibbonGroupSizeState.Collapsed] = collapsed,
        })
    {
        ReductionMode = RibbonGroupReductionMode.Collapse,
        ReductionPriority = priority,
    };

    private static void Measure(double availableWidth, params MeasuredGroup[] groups)
    {
        var panel = new RibbonGroupsPanel();
        foreach (MeasuredGroup group in groups)
        {
            panel.Children.Add(group);
        }

        panel.Measure(new Size(availableWidth, 100));
    }

    private sealed class MeasuredGroup : RibbonGroup
    {
        private readonly Dictionary<RibbonGroupSizeState, double> _widths;

        public MeasuredGroup(Dictionary<RibbonGroupSizeState, double> widths)
        {
            _widths = widths;
        }

        public void SetWidth(RibbonGroupSizeState state, double width)
        {
            _widths[state] = width;
            InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size constraint) =>
            new(_widths[SizeState], 24);
    }
}
