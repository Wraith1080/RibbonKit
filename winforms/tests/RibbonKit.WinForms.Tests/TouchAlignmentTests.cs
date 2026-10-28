using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class TouchAlignmentTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Touch_commands_keep_close_regular_captions_larger_split_primary_and_lower_menu_content(bool office) => FoundationTests.Sta(() =>
    {
        using var probe = new Control(); using var image = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(image)) { using var pen = new Pen(Color.SteelBlue, 2); g.DrawRectangle(pen, 5, 3, 22, 26); }
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        foreach (bool pressed in new[] { false, true })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            using var font = new Font("Segoe UI", points * dpi / (float)probe.DeviceDpi);
            using var host = new RibbonControl { Density = RibbonDensity.Touch, Font = font };
            var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); host.Tabs.Add(tab);
            RibbonItem[] items = [new RibbonButtonItem { Text = "Copy" }, new RibbonToggleItem { Text = "Keep source formatting" },
                new RibbonDropDownItem { Text = "Text color" }, new RibbonSplitButtonItem { Text = "Paste", Layout = RibbonSplitButtonLayout.Vertical },
                new RibbonSplitButtonItem { Text = "Keep source formatting", Layout = RibbonSplitButtonLayout.Vertical }, new RibbonSplitButtonItem { Text = "Track changes" }];
            foreach (var item in items) { item.Image = image; group.Items.Add(item); }
            var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Neutral(record);
            int row = Math.Max(45, (int)Math.Ceiling(font.Height * 96d / dpi) + 8);
            using var bitmap = new Bitmap(S(6 * 112 + 8), S(row * 3 + 16)); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(host.Theme.Surface);
            var imageTops = new List<int>(); var captionTops = new List<int>();
            for (int i = 0; i < items.Length; i++)
            {
                record.Lines.Clear(); record.Arrow = Rectangle.Empty;
                var bounds = new Rectangle(S(8 + i * 112), S(8), S(104), S(row * 3 - 1));
                var context = new RibbonPaintContext(graphics, bounds, host.Theme, font, dpi, true, pressed, true) { Density = host.Density };
                if (items[i] is RibbonSplitButtonItem split)
                {
                    var parts = RibbonSplitLayout.Calculate(bounds, split, RibbonItemSize.Large, false, dpi, font);
                    Assert.Equal(bounds, Rectangle.Union(parts.Primary, parts.DropDown)); Assert.False(parts.Primary.IntersectsWith(parts.DropDown));
                    painter.DrawSplitItem(context with { Bounds = parts.Primary, Hot = false, Pressed = false, Focused = false }, context with { Bounds = parts.DropDown }, split, RibbonItemSize.Large, false);
                    if (split.Layout == RibbonSplitButtonLayout.Vertical)
                    {
                        Assert.True(parts.Primary.Contains(record.Image));
                        Assert.All(record.Lines, line => Assert.True(parts.DropDown.Contains(line)));
                        Assert.True(parts.DropDown.Contains(record.Arrow)); Assert.True(record.Arrow.Top > record.Lines[^1].Bottom);
                        if (points == 9) Assert.True(parts.Primary.Height > S(68));
                        int envelope = font.Height + (record.Lines.Count - 1) * RibbonCaptionLayout.LineAdvance(font, dpi) + S(16);
                        Assert.InRange(Math.Abs(record.Lines[0].Top + envelope / 2d - (parts.DropDown.Top + parts.DropDown.Bottom) / 2d), 0, 1);
                        Assert.InRange(Math.Abs((record.Image.Top + record.Image.Bottom) / 2d - (parts.Primary.Top + parts.Primary.Bottom) / 2d), 0, 1);
                        Assert.InRange(Math.Abs((record.Arrow.Left + record.Arrow.Right) / 2d - (parts.DropDown.Left + parts.DropDown.Right) / 2d), 0, 1);
                        var ink = Rectangle.Empty;
                        int center = (record.Arrow.Left + record.Arrow.Right) / 2;
                        var background = bitmap.GetPixel(center - S(6), record.Arrow.Top);
                        for (int y = record.Arrow.Top; y < record.Arrow.Bottom; y++) for (int x = center - S(6); x <= center + S(6); x++)
                            if (bitmap.GetPixel(x, y) != background) ink = ink.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(ink, new(x, y, 1, 1));
                        Assert.InRange(ink.Width, S(8) - 1, S(8) + 1); Assert.InRange(ink.Height, S(6) - 1, S(6) + 1);
                        Assert.InRange(Math.Abs((ink.Top + ink.Bottom) / 2d - (record.Arrow.Top + record.Arrow.Bottom) / 2d), 0, 1);
                    }
                }
                else painter.DrawItem(context, items[i], RibbonItemSize.Large, false);
                imageTops.Add(record.Image.Top);
                if (items[i] is not RibbonSplitButtonItem { Layout: RibbonSplitButtonLayout.Vertical })
                {
                    captionTops.Add(record.Lines[0].Top); Assert.Equal(S(8), record.Lines[0].Top - record.Image.Bottom);
                }
                Assert.All(record.Lines, line => Assert.True(bounds.Contains(line)));
            }
            Assert.Single(imageTops.Distinct()); Assert.Single(captionTops.Distinct());
            if (Environment.GetEnvironmentVariable("RIBBONKIT_NATIVE_CAPTURE_DIR") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); bitmap.Save(Path.Combine(capture, $"touch-alignment-{(office ? "office" : "neutral")}-{dpi}-{points}-{(pressed ? "pressed" : "hover")}.png")); }
        }
    });

    private sealed class Record { internal Rectangle Image, Arrow; internal readonly List<Rectangle> Lines = []; }
    private sealed class Neutral(Record record) : RibbonRenderer
    {
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { record.Image = c.Bounds; base.DrawItemImage(c, item, size); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Lines.Add(c.Bounds); base.DrawText(c, text, color, flags); }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawDropDownArrow(c, color); }
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { record.Image = c.Bounds; base.DrawItemImage(c, item, size); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Lines.Add(c.Bounds); base.DrawText(c, text, color, flags); }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawDropDownArrow(c, color); }
    }
}
