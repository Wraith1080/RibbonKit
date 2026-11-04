using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class LargeCommandCenteringTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Roomier_large_shared_bands_align_controls_and_caption_lengths(bool office) => FoundationTests.Sta(() =>
    {
        var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
        var alignments = new Dictionary<(RibbonDensity, int, int), (int Icon, int Caption, int Arrow)>();
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        foreach (string text in new[] { "Paste", "Keep source formatting" })
        {
            using var font = new Font("Segoe UI", points * dpi / 96f); using var ribbon = new RibbonControl { Density = density, Font = font };
            var group = new RibbonGroup(); var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            RibbonItem[] items = [new RibbonButtonItem(), new RibbonDropDownItem(), new RibbonSplitButtonItem(), new RibbonSplitButtonItem { Layout = RibbonSplitButtonLayout.Vertical }];
            foreach (var item in items) { item.Text = text; item.Icon = RibbonStockIcons.Create("open"); item.SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Large); group.Items.Add(item); }
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            foreach (var item in items)
            {
                var bounds = new Rectangle(S(4), S(4), S(104), S(180));
                byte[] Draw(RibbonTheme theme)
                {
                    ribbon.Theme = theme; record.Clear(); using var image = new Bitmap(S(112), S(188)); using var graphics = Graphics.FromImage(image); graphics.Clear(Color.White);
                    var c = new RibbonPaintContext(graphics, bounds, theme, font, dpi) { Density = density, Hot = true };
                    if (item is RibbonSplitButtonItem split)
                    {
                        var parts = RibbonSplitLayout.Calculate(bounds, split, RibbonItemSize.Large, false, dpi, font); record.Primary = parts.Primary; record.DropDown = parts.DropDown;
                        painter.DrawSplitItem(c with { Bounds = parts.Primary }, c with { Bounds = parts.DropDown }, split, RibbonItemSize.Large, false);
                    }
                    else painter.DrawItem(c, item, RibbonItemSize.Large, false);
                    if (Environment.GetEnvironmentVariable("RIBBONKIT_LARGE_CENTER_CAPTURE") is { Length: > 0 } output)
                    { Directory.CreateDirectory(output); image.Save(Path.Combine(output, $"large-{office}-{density}-{dpi}-{points}-{Array.IndexOf(items, item)}-{(text == "Paste" ? "short" : "wrapped")}-{(theme.CommandContentVerticalPadding is null ? "default" : "roomier")}.png")); }
                    using var stream = new MemoryStream(); image.Save(stream, System.Drawing.Imaging.ImageFormat.Png); return stream.ToArray();
                }
                var before = Draw(RibbonThemes.Office2024Light); _ = Draw(RibbonThemes.Office2024Light with { CommandContentHorizontalPadding = 12, CommandContentVerticalPadding = 16 });
                Assert.NotEmpty(record.Lines); var textBounds = record.Lines.Aggregate(Rectangle.Union);
                bool vertical = item is RibbonSplitButtonItem { Layout: RibbonSplitButtonLayout.Vertical };
                int reservedCaption = font.Height + RibbonCaptionLayout.LineAdvance(font, dpi);
                int arrowTop = record.Lines[0].Top + reservedCaption + S(4);
                var alignment = (record.Image.Top, record.Lines[0].Top, arrowTop);
                var key = (density, dpi, points);
                if (alignments.TryGetValue(key, out var expected)) Assert.Equal(expected, alignment); else alignments[key] = alignment;
                Assert.InRange(Math.Abs(record.Image.Top + arrowTop + S(12) - bounds.Top - bounds.Bottom), 0, 1);
                Assert.Equal(S(density == RibbonDensity.Touch ? 8 : 5), record.Lines[0].Top - record.Image.Bottom);
                if (vertical)
                {
                    Assert.Equal(arrowTop, record.Arrow.Top); Assert.True(record.Primary.Contains(record.Image));
                    Assert.All(record.Lines, line => Assert.True(record.DropDown.Contains(line))); Assert.True(record.DropDown.Contains(record.Arrow));
                    Assert.Equal(bounds, Rectangle.Union(record.Primary, record.DropDown)); Assert.False(record.Primary.IntersectsWith(record.DropDown));
                }
                Assert.Equal(before, Draw(RibbonThemes.Office2024Light));
            }
        }
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Native_root_and_group_targets_keep_separate_large_primary_and_dropdown_actions(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1500, Height = 800 }; using var ribbon = new RibbonControl { Density = density, AnimationLevel = RibbonAnimationLevel.None };
        form.Controls.Add(ribbon); var group = new RibbonGroup { CanResize = false }; var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        group.Items.Add(new RibbonButtonItem { SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Small) });
        var split = new RibbonSplitButtonItem { Text = "Keep source formatting", Icon = RibbonStockIcons.Create("open"), Layout = RibbonSplitButtonLayout.Vertical,
            SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Large), CheckOnClick = true, KeyTip = "A", DropDownKeyTip = "B" }; group.Items.Add(split);
        int clicks = 0, openings = 0; split.Click += (_, _) => { clicks++; Assert.True(split.Checked); }; split.DropDownOpening += (_, e) => { openings++; e.Cancel = true; };
        ribbon.Theme = RibbonThemes.Office2024Light with { CommandContentVerticalPadding = 16 }; form.Show(); Application.DoEvents();
        foreach (bool flyout in new[] { false, true })
        {
            using var popup = flyout ? new RibbonPopupSurface(ribbon, group, new(1200, 650)) : null;
            var targets = popup?.Targets ?? ribbon.Snapshot.Targets; var main = targets.Single(t => t.Element == split && !t.DropDownPart); var arrow = targets.Single(t => t.Element == split && t.DropDownPart);
            Assert.Equal(main.Bounds.Bottom, arrow.Bounds.Top); Assert.Equal("A", main.PreferredKeyTip); Assert.Equal("B", arrow.PreferredKeyTip);
            Control surface = (Control?)popup ?? ribbon; surface.CreateControl();
            var node = (popup?.AutomationRoot ?? ribbon.AutomationRoot).Node(split); var bounds = node.BoundingRectangle;
            var screen = surface.RectangleToScreen(main.VisibleBounds); Assert.Equal(screen.Top, bounds.Top); Assert.Equal(screen.Height, bounds.Height);
            split.Checked = false; ribbon.Activate(main, popup); Assert.Equal(flyout ? 2 : 1, clicks); Assert.True(split.Checked);
            ribbon.Activate(arrow, popup); Assert.Equal(flyout ? 2 : 1, openings); Assert.False(split.IsDropDownOpen);
        }
        form.Close();
    });

    [Fact]
    public void Centering_is_opt_in_and_constrained_rectangles_keep_the_legacy_split() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var split = new RibbonSplitButtonItem { Text = "Keep source formatting", Icon = RibbonStockIcons.Create("open"), Layout = RibbonSplitButtonLayout.Vertical }; group.Items.Add(split);
        var bounds = new Rectangle(0, 0, 25, 20); var before = RibbonSplitLayout.Calculate(bounds, split, RibbonItemSize.Large, false, ribbon.DeviceDpi);
        foreach (int? padding in new int?[] { null, 0, 3, 16 })
        {
            ribbon.Theme = ribbon.Theme with { CommandContentHorizontalPadding = 12, CommandContentVerticalPadding = padding };
            Assert.Equal(before, RibbonSplitLayout.Calculate(bounds, split, RibbonItemSize.Large, false, ribbon.DeviceDpi));
            Assert.False(RibbonLargeCommandLayout.TryCalculate(bounds, ribbon.Font, ribbon.DeviceDpi, ribbon.Density, ribbon.Theme, true, true, out _));
        }
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Unused_image_and_arrow_bands_keep_the_same_caption_and_split_boundary(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        {
            using var font = new Font("Segoe UI", points * dpi / 96f); int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var bounds = new Rectangle(S(4), S(4), S(104), S(180)); var theme = RibbonThemes.Office2024Light with { CommandContentVerticalPadding = 16 };
            Assert.True(RibbonLargeCommandLayout.TryCalculate(bounds, font, dpi, density, theme, true, true, out var reference));
            foreach (bool vertical in new[] { false, true })
            foreach (bool image in new[] { false, true })
            {
                Assert.True(RibbonLargeCommandLayout.TryCalculate(bounds, font, dpi, density, theme, vertical, image, out var actual));
                Assert.Equal(reference.Caption, actual.Caption); Assert.Equal(reference.PrimaryHeight, actual.PrimaryHeight);
                Assert.Equal(image, !actual.Icon.IsEmpty); Assert.Equal(vertical, !actual.Arrow.IsEmpty);
            }
        }
    });

    private sealed class Record
    {
        internal Rectangle Image, Arrow, Primary, DropDown; internal readonly List<Rectangle> Lines = [];
        internal void Clear() { Image = Arrow = Primary = DropDown = Rectangle.Empty; Lines.Clear(); }
    }
    private sealed class Flat(Record r) : RibbonRenderer
    {
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { r.Image = c.Bounds; base.DrawItemImage(c, item, size); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { r.Lines.Add(c.Bounds); base.DrawText(c, text, color, flags); }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { r.Arrow = c.Bounds; base.DrawDropDownArrow(c, color); }
    }
    private sealed class Office(Record r) : RibbonOffice2024Renderer
    {
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { r.Image = c.Bounds; base.DrawItemImage(c, item, size); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { r.Lines.Add(c.Bounds); base.DrawText(c, text, color, flags); }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { r.Arrow = c.Bounds; base.DrawDropDownArrow(c, color); }
    }
}
