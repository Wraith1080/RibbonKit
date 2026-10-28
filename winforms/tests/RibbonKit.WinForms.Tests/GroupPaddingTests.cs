using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GroupPaddingTests
{
    [Fact]
    public void Expanded_groups_have_equal_clearance_inside_the_divider_lane_without_a_trailing_column_gap() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        group.Items.AddRange(new RibbonButtonItem { Text = "Print preview" }, new RibbonButtonItem { Text = "Small", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Small) },
            new RibbonButtonItem { Text = "Medium", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) });
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (string caption in new[] { "", "Modal preview", "A deliberately wide group caption that requires a centered content block" })
        foreach (int state in new[] { 0, 1, 2 })
        {
            ribbon.Density = density; group.Text = caption; ribbon.Engine.Clear();
            var measured = ribbon.Engine.Measure(group, state, ribbon.Font, dpi, ribbon.LayoutTheme);
            int left = measured.Items.Min(i => i.Bounds.Left), right = measured.Width - RibbonLayoutEngine.GroupDividerWidth - measured.Items.Max(i => i.Bounds.Right);
            Assert.Equal(left, right); Assert.True(left >= RibbonLayoutEngine.Scale(RibbonLayoutEngine.GroupSideInset, dpi));
            if (caption.Length == 0) Assert.Equal(RibbonLayoutEngine.Scale(RibbonLayoutEngine.GroupSideInset, dpi), right);
        }
    });

    [Fact]
    public void Collapsed_openers_use_identical_left_and_right_padding_from_the_adjacent_divider_edges() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 210 }; var tab = new RibbonTab(); ribbon.Tabs.Add(tab);
        for (int g = 0; g < 3; g++)
        {
            var group = new RibbonGroup { Text = "Editing " + g, ReductionMode = RibbonGroupReductionMode.Collapse };
            group.Items.AddRange(Enumerable.Range(0, 4).Select(i => new RibbonButtonItem { Text = "Large command " + i }).ToArray()); tab.Groups.Add(group);
        }
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            ribbon.Density = density;
            foreach (var layout in new[] { ribbon.Snapshot, ribbon.Engine.BuildBody(ribbon, tab, ribbon.Width, 0) })
            {
                Assert.Contains(layout.Groups, g => g.Collapsed);
                foreach (var slot in layout.Groups.Where(g => g.Collapsed))
                {
                    Assert.Equal(slot.Bounds.Left - slot.Frame.Left, slot.Frame.Right - RibbonLayoutEngine.GroupDividerWidth - slot.Bounds.Right);
                    Assert.Equal(RibbonLayoutEngine.Scale(RibbonLayoutEngine.CollapsedGroupSideInset, ribbon.DeviceDpi), slot.Bounds.Left - slot.Frame.Left);
                    Assert.Equal(slot.Bounds, layout.Targets.Single(t => t.Element == slot.Group).Bounds);
                }
            }
        }
    });
}
