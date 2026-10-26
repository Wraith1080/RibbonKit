using RibbonKit.WinForms.Layout;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace RibbonKit.WinForms.Tests;

public sealed class AlignmentAndLifecycleTests
{
    [Theory]
    [InlineData(96, 9)]
    [InlineData(120, 9)]
    [InlineData(144, 12)]
    [InlineData(192, 18)]
    public void Neighbouring_rows_share_column_width_icon_slot_and_text_baseline(int dpi, int fontPoints) => FoundationTests.Sta(() =>
    {
        using var font = new Font("Segoe UI", fontPoints);
        using var image = new Bitmap(16, 16);
        var group = new RibbonGroup();
        group.Items.Add(new RibbonButtonItem { Text = "Short", Image = image, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) });
        group.Items.Add(new RibbonButtonItem { Text = "A longer neighbouring label", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) });
        group.Items.Add(new RibbonButtonItem { Text = "Third", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) });
        var measure = new RibbonLayoutEngine().Measure(group, 0, font, dpi, new RibbonTheme { BodyHeight = 300 });
        Assert.Single(measure.Items.Select(i => i.Bounds.Width).Distinct());
        Assert.All(measure.Items, i => Assert.True(i.ReserveIcon));
        var contents = measure.Items.Select(i => RibbonContentLayout.Calculate(i.Bounds, i.Size, false, i.ReserveIcon, dpi, font)).ToArray();
        Assert.Single(contents.Select(c => c.Text.Left).Distinct());
        Assert.Single(contents.Select(c => c.Icon.Left).Distinct());
        Assert.Single(contents.Select((c, i) => c.Text.Top - measure.Items[i].Bounds.Top).Distinct());
        Assert.All(contents, c => Assert.True(c.Text.Height >= font.Height));
        Assert.All(contents, c => Assert.True(c.Flags.HasFlag(TextFormatFlags.NoPadding)));
        for (int i = 0; i < contents.Length; i++)
        {
            int labelWidth = TextRenderer.MeasureText(group.Items[i].Text, font, new Size(1000, 1000), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
            // Width caps intentionally ellipsize exceptionally long labels.
            if (labelWidth + RibbonLayoutEngine.Scale(38, dpi) <= RibbonLayoutEngine.Scale(180, dpi))
                Assert.True(contents[i].Text.Width >= labelWidth);
        }
    });
    [Fact]
    public void Large_single_and_wrapped_labels_start_on_the_same_line() => FoundationTests.Sta(() =>
    {
        using var font = new Font("Segoe UI", 9);
        var a = RibbonContentLayout.Calculate(new(0, 0, 80, 100), RibbonItemSize.Large, false, true, 120, font);
        var b = RibbonContentLayout.Calculate(new(80, 0, 130, 100), RibbonItemSize.Large, false, true, 120, font);
        Assert.Equal(a.Icon.Top, b.Icon.Top);
        Assert.Equal(a.Text.Top, b.Text.Top);
        Assert.False(a.Flags.HasFlag(TextFormatFlags.VerticalCenter));
    });
    [Fact]
    public void Detached_trees_reject_duplicate_ids_and_dispose_children_cleanly()
    {
        using var group = new RibbonGroup { Id = "group" };
        var first = new RibbonButtonItem { Id = "first" };
        group.Items.Add(first);
        Assert.Throws<ArgumentException>(() => group.Items.Add(new RibbonButtonItem { Id = "first" }));
        Assert.Throws<ArgumentException>(() => first.Id = "group");
        first.Dispose();
        Assert.Empty(group.Items);
    }
    [Fact]
    public void Removing_or_hiding_selected_tab_uses_next_then_previous() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl();
        var a = new RibbonTab();
        var b = new RibbonTab();
        var c = new RibbonTab();
        ribbon.Tabs.AddRange(a, b, c);
        ribbon.SelectedTab = b;
        ribbon.Tabs.Remove(b);
        Assert.Same(c, ribbon.SelectedTab);
        c.Visible = false;
        Assert.Same(a, ribbon.SelectedTab);
        a.Enabled = false;
        Assert.Null(ribbon.SelectedTab);
    });
    [Fact]
    public void Large_keytip_levels_terminate_and_remain_prefix_free()
    {
        var input = Enumerable.Range(0, 80).Select(i => ("Command", i < 36 ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[i].ToString() : "")).ToArray();
        var result = KeyTipResolver.Resolve(input);
        Assert.Equal(80, result.Distinct().Count());
        foreach (var a in result)
            foreach (var b in result)
                if (a != b)
                    Assert.False(b.StartsWith(a, StringComparison.Ordinal));
    }
}
