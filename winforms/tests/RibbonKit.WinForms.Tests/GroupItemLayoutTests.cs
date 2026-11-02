using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GroupItemLayoutTests
{
    [Theory]
    [InlineData(96, false)] [InlineData(120, false)] [InlineData(144, false)] [InlineData(192, false)]
    [InlineData(96, true)] [InlineData(120, true)] [InlineData(144, true)] [InlineData(192, true)]
    public void Explicit_rows_flow_independently_and_remain_centered_at_each_density_and_reduction(int dpi, bool threeRows) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); ribbon.Tabs.Add(tab);
        var group = new RibbonGroup { Layout = threeRows ? RibbonGroupLayout.ThreeRows : RibbonGroupLayout.TwoRows }; tab.Groups.Add(group);
        var topA = Command("Wide top command", RibbonItemRow.First); var bottomA = Command("Second", RibbonItemRow.Second);
        var topB = Command("B", RibbonItemRow.First, RibbonItemSize.Small); var bottomB = new RibbonTextBoxItem { Text = "Value", Row = RibbonItemRow.Second };
        var bottomC = Command("C", threeRows ? RibbonItemRow.Third : RibbonItemRow.Second, RibbonItemSize.Small);
        group.Items.AddRange(topA, bottomA, topB, bottomB, bottomC);
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int state in new[] { 0, 1, 2 })
        {
            ribbon.Density = density; ribbon.Engine.Clear();
            var measured = ribbon.Engine.Measure(group, state, ribbon.Font, dpi, ribbon.ClassicLayoutTheme);
            Rectangle B(RibbonItem item) => measured.Items.Single(i => i.Item == item).Bounds;
            Assert.Equal(B(topA).Left, B(bottomA).Left);
            Assert.Equal(B(topA).Top, B(topB).Top); Assert.Equal(B(bottomA).Top, B(bottomB).Top);
            if (threeRows) { Assert.True(B(bottomC).Top > B(bottomB).Bottom); Assert.Equal(B(topA).Left, B(bottomC).Left); }
            else Assert.Equal(B(bottomB).Top, B(bottomC).Top);
            Assert.True(B(topA).Bottom < B(bottomA).Top); Assert.True(B(topA).Right < B(topB).Left);
            Assert.True(B(bottomA).Right < B(bottomB).Left); if (!threeRows) Assert.True(B(bottomB).Right < B(bottomC).Left);
            CheckCentered(ribbon, measured, dpi);
            Assert.All(measured.Items, a => Assert.DoesNotContain(measured.Items, b => a != b && a.Bounds.IntersectsWith(b.Bounds)));
        }
    });

    [Theory]
    [InlineData(2)] [InlineData(4)] [InlineData(5)]
    public void Only_a_two_row_non_large_column_group_is_automatically_centered(int count) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        for (int i = 0; i < count; i++) group.Items.Add(Command("Item " + i, RibbonItemRow.First));
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.Density = density; ribbon.Engine.Clear();
            var measured = ribbon.Engine.Measure(group, 0, ribbon.Font, dpi, ribbon.ClassicLayoutTheme);
            if (count == 2) CheckCentered(ribbon, measured, dpi);
            else Assert.Equal(RibbonLayoutEngine.Scale(RibbonLayoutEngine.CommandTopInset, dpi), measured.Items.Min(i => i.Bounds.Top));
        }
        // A full-height item preserves the normal top-aligned columns.
        group.Items.Add(new RibbonButtonItem()); ribbon.Engine.Clear();
        var mixed = ribbon.Engine.Measure(group, 0, ribbon.Font, 96, ribbon.ClassicLayoutTheme);
        Assert.Equal(RibbonLayoutEngine.CommandTopInset, mixed.Items[0].Bounds.Top);
    });

    [Fact]
    public void Large_commands_and_separators_end_both_row_flows_and_new_rows_start_together() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup { Layout = RibbonGroupLayout.TwoRows };
        tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var a = Command("Top before", RibbonItemRow.First); var b = Command("A much wider bottom before", RibbonItemRow.Second);
        var separator = new RibbonMenuSeparatorItem(); var c = Command("Top after", RibbonItemRow.First); var d = Command("Bottom after", RibbonItemRow.Second);
        var large = new RibbonSplitButtonItem { Layout = RibbonSplitButtonLayout.Vertical }; var e = Command("Final", RibbonItemRow.Second);
        group.Items.AddRange(a, b, separator, c, d, large, e);
        var measured = ribbon.Engine.Measure(group, 0, ribbon.Font, 96, ribbon.ClassicLayoutTheme);
        Rectangle B(RibbonItem item) => measured.Items.Single(i => i.Item == item).Bounds;
        Assert.True(B(separator).Left > Math.Max(B(a).Right, B(b).Right)); Assert.True(B(c).Left > B(separator).Right);
        Assert.Equal(B(c).Left, B(d).Left); Assert.True(B(large).Left > Math.Max(B(c).Right, B(d).Right)); Assert.True(B(e).Left > B(large).Right);
        Assert.Equal(B(large).Top, B(separator).Top); Assert.Equal(B(large).Height, B(separator).Height);
        Assert.True(B(a).Top > B(large).Top);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Group_flyouts_keep_the_same_layout_and_separator_is_decorative(bool twoRows) => FoundationTests.Sta(() =>
    {
        using var renderer = new SeparatorRenderer(); using var form = new Form { Width = 950, Height = 400 };
        var ribbon = new RibbonControl { Renderer = renderer }; form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup { CanResize = false, Layout = twoRows ? RibbonGroupLayout.TwoRows : RibbonGroupLayout.Columns };
        tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        group.Items.AddRange(Command("Top", RibbonItemRow.First), Command("Second", RibbonItemRow.Second), new RibbonMenuSeparatorItem { KeyTip = "S" }, new RibbonTextBoxItem { Text = "Field", Row = RibbonItemRow.Second });
        form.Show(); Application.DoEvents();
        var source = ribbon.Engine.Measure(group, 0, ribbon.Font, ribbon.DeviceDpi, ribbon.ClassicLayoutTheme);
        using var popup = new RibbonPopupSurface(ribbon, group, new Size(1200, 800));
        form.Controls.Add(popup); popup.FocusFirst();
        Point? offset = null;
        foreach (var entry in source.Items)
        {
            var target = popup.Targets.Single(t => t.Element == entry.Item);
            Assert.Equal(entry.Bounds.Size, target.Bounds.Size);
            var difference = new Point(target.Bounds.X - entry.Bounds.X, target.Bounds.Y - entry.Bounds.Y);
            offset ??= difference; Assert.Equal(offset.Value, difference);
            if (entry.Item is RibbonMenuSeparatorItem) Assert.False(target.Enabled);
        }
        renderer.GroupSeparators = renderer.MenuSeparators = 0;
        using var rootPixels = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(rootPixels, ribbon.ClientRectangle);
        using var popupPixels = new Bitmap(popup.Width, popup.Height); popup.DrawToBitmap(popupPixels, popup.ClientRectangle);
        Assert.Equal(2, renderer.GroupSeparators); Assert.Equal(0, renderer.MenuSeparators);
        var menu = new RibbonDropDownItem(); menu.Items.Add(new RibbonMenuSeparatorItem()); group.Items.Add(menu);
        using var menuPopup = new RibbonPopupSurface(ribbon, menu, new Size(1200, 800));
        using var menuPixels = new Bitmap(menuPopup.Width, menuPopup.Height); menuPopup.DrawToBitmap(menuPixels, menuPopup.ClientRectangle);
        Assert.Equal(1, renderer.MenuSeparators);
        Assert.DoesNotContain(ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.QuickAccess), c => c.Source is RibbonMenuSeparatorItem);
    });

    [Fact]
    public void Row_changes_invalidate_geometry_without_replacing_models_and_invalid_enum_values_are_rejected() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 900 }; var tab = new RibbonTab(); var group = new RibbonGroup { Layout = RibbonGroupLayout.TwoRows };
        var command = Command("Move", RibbonItemRow.First); group.Items.Add(command); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        int before = ribbon.Snapshot.Targets.Single(t => t.Element == command).Bounds.Top;
        command.Row = RibbonItemRow.Second;
        Assert.True(ribbon.Snapshot.Targets.Single(t => t.Element == command).Bounds.Top > before);
        int second = ribbon.Snapshot.Targets.Single(t => t.Element == command).Bounds.Top;
        command.Row = RibbonItemRow.Third; Assert.Equal(second, ribbon.Snapshot.Targets.Single(t => t.Element == command).Bounds.Top);
        group.Layout = RibbonGroupLayout.ThreeRows; Assert.True(ribbon.Snapshot.Targets.Single(t => t.Element == command).Bounds.Top > second);
        group.Layout = RibbonGroupLayout.Columns;
        Assert.True(ribbon.Snapshot.Targets.Single(t => t.Element == command).Bounds.Top < before);
        Assert.Throws<ArgumentOutOfRangeException>(() => command.Row = (RibbonItemRow)20);
        Assert.Throws<ArgumentOutOfRangeException>(() => group.Layout = (RibbonGroupLayout)20);
    });

    private static RibbonButtonItem Command(string text, RibbonItemRow row, RibbonItemSize size = RibbonItemSize.Medium) => new() { Text = text, Row = row, SizePolicy = RibbonSizePolicy.Fixed(size) };
    private static void CheckCentered(RibbonControl ribbon, MeasuredGroup group, int dpi)
    {
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        int fontLogical = (int)Math.Ceiling(ribbon.Font.Height * 96d / dpi);
        int areaTop = S(RibbonLayoutEngine.CommandTopInset), areaHeight = S(ribbon.ClassicLayoutTheme.BodyHeight - Math.Max(20, fontLogical + 6) - RibbonLayoutEngine.CommandVerticalInset);
        int above = group.Items.Min(i => i.Bounds.Top) - areaTop;
        int below = areaTop + areaHeight - group.Items.Max(i => i.Bounds.Bottom);
        Assert.True(Math.Abs(above - below) <= 1, $"Unequal top/bottom clearance: {above}/{below}");
    }
    private sealed class SeparatorRenderer : RibbonRenderer
    {
        internal int GroupSeparators, MenuSeparators;
        public override void DrawGroupSeparator(RibbonPaintContext context, RibbonMenuSeparatorItem item) { GroupSeparators++; base.DrawGroupSeparator(context, item); }
        public override void DrawMenuSeparator(RibbonPaintContext context, RibbonMenuSeparatorItem item) { MenuSeparators++; base.DrawMenuSeparator(context, item); }
    }
}
