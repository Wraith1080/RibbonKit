using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class TouchSplitWidthTests
{
    [Fact]
    public void Horizontal_touch_splits_reserve_a_full_arrow_target_and_larger_primary_without_losing_caption_space() => FoundationTests.Sta(() =>
    {
        using var probe = new Control();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        foreach (int maximum in new[] { 60, 108, 160 })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            using var font = new Font("Segoe UI", points * dpi / (float)probe.DeviceDpi);
            using var host = new RibbonControl { Font = font };
            host.Theme = host.Theme with { LargeButtonMaxWidth = maximum };
            var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); host.Tabs.Add(tab);
            RibbonSplitButtonItem[] splits = [new() { Text = "Paste" }, new() { Text = "Keep the original document source formatting" },
                new() { Text = "Medium", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) },
                new() { Text = "Small", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Small) },
                new() { Text = "Paste", Layout = RibbonSplitButtonLayout.Vertical },
                new() { Text = "Reduced", Layout = RibbonSplitButtonLayout.Vertical, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) }];
            group.Items.AddRange(splits);
            Rectangle[] compact = host.Engine.Measure(group, 0, font, dpi, host.Theme).Items.Select(i => i.Bounds).ToArray();
            host.Density = RibbonDensity.Touch;
            var touch = host.Engine.Measure(group, 0, font, dpi, host.Theme);
            foreach (var entry in touch.Items)
            {
                var split = (RibbonSplitButtonItem)entry.Item;
                var parts = RibbonSplitLayout.Calculate(entry.Bounds, split, entry.Size, false, dpi, font);
                Assert.Equal(entry.Bounds, Rectangle.Union(parts.Primary, parts.DropDown));
                Assert.False(parts.Primary.IntersectsWith(parts.DropDown));
                if (RibbonSplitLayout.IsVertical(split, entry.Size, false))
                {
                    Assert.Equal(entry.Bounds.Width, parts.DropDown.Width);
                    continue;
                }
                Assert.Equal(S(44), parts.DropDown.Width);
                Assert.True(parts.Primary.Width >= S(entry.Size == RibbonItemSize.Large ? 72 : 56));
                Assert.True(parts.Primary.Width > parts.DropDown.Width);
                if (entry.Size == RibbonItemSize.Large)
                {
                    // The old Large Touch width still describes the vertical variant.
                    split.Layout = RibbonSplitButtonLayout.Vertical;
                    var oldWidth = host.Engine.Measure(group, 0, font, dpi, host.Theme).Items.Single(i => i.Item == split).Bounds.Width;
                    Assert.True(parts.Primary.Width >= oldWidth - S(28));
                    split.Layout = RibbonSplitButtonLayout.Horizontal;
                }
            }
            var menu = RibbonSplitLayout.Calculate(new(0, 0, S(280), S(44)), splits[0], RibbonItemSize.Medium, true, dpi);
            Assert.Equal(S(28), menu.DropDown.Width);
            var qat = new RibbonQuickAccessDropDownReference { Source = splits[0] }; host.QuickAccessItems.Add(qat);
            var target = new RibbonTarget(TargetKind.QuickAccess, qat, new(0, 0, S(64), S(44)), new(0, 0, S(64), S(44)));
            Assert.Equal(S(20), RibbonSplitLayout.Expand(target, dpi, false).Last().Bounds.Width);
            host.Density = RibbonDensity.Compact;
            Assert.Equal(compact, host.Engine.Measure(group, 0, font, dpi, host.Theme).Items.Select(i => i.Bounds).ToArray());
        }
    });
}
