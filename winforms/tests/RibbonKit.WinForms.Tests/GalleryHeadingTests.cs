using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GalleryHeadingTests
{
    [Fact]
    public void Gallery_heading_text_is_bold_while_only_Office2024_paints_a_rounded_background() => FoundationTests.Sta(() =>
    {
        using var office = new Office(); using var neutral = new Neutral();
        foreach (var (renderer, record) in new (RibbonRenderer, Record)[] { (office, office.Record), (neutral, neutral.Record) })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int logicalWidth in new[] { 199, 200 })
        foreach (int points in new[] { 9, 18 })
        foreach (var (fill, foreground) in new[] { (Color.FromArgb(240, 240, 240), Color.FromArgb(32, 31, 30)), (Color.FromArgb(48, 52, 60), Color.FromArgb(245, 238, 220)) })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            var theme = RibbonThemes.Office2024Light with { GalleryHeadingBackground = new(fill), GalleryHeadingForeground = foreground };
            using var font = new Font("Segoe UI", points * dpi / 96f);
            var bounds = new Rectangle(0, 0, S(logicalWidth), font.Height + S(12));
            using var normal = Draw(false); using var states = Draw(true);
            bool styled = ReferenceEquals(renderer, office);
            Assert.Equal((styled ? fill : theme.Surface).ToArgb(), normal.GetPixel(bounds.Width / 2, bounds.Height - S(3)).ToArgb());
            Assert.Equal((styled ? foreground : theme.SecondaryText).ToArgb(), record.Color.ToArgb()); Assert.True(record.Bold);
            Assert.False(record.Hot || record.Pressed || record.Focused);
            Assert.Equal(theme.Surface.ToArgb(), normal.GetPixel(0, 0).ToArgb());
            Assert.True(record.TextBounds.Height >= record.FontHeight); Assert.True(record.TextBounds.Left >= S(6));
            Assert.Equal(Pixels(normal), Pixels(states));
            Bitmap Draw(bool flags)
            {
                var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(theme.Surface);
                renderer.DrawGalleryHeading(new(graphics, bounds, theme, font, dpi, flags, flags, flags), "Document"); return bitmap;
            }
        }
        static int[] Pixels(Bitmap bitmap) => Enumerable.Range(0, bitmap.Height).SelectMany(y => Enumerable.Range(0, bitmap.Width).Select(x => bitmap.GetPixel(x, y).ToArgb())).ToArray();
    });
    private sealed class Record { internal Color Color; internal bool Bold, Hot, Pressed, Focused; internal int FontHeight; internal Rectangle TextBounds; }
    private sealed class Office : RibbonOffice2024Renderer
    {
        internal Record Record = new();
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { Capture(Record, c, color); base.DrawText(c, text, color, flags); }
    }
    private sealed class Neutral : RibbonRenderer
    {
        internal Record Record = new();
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { Capture(Record, c, color); base.DrawText(c, text, color, flags); }
    }
    private static void Capture(Record record, RibbonPaintContext context, Color color)
    {
        record.Color = color; record.Bold = context.Font.Bold; record.FontHeight = context.Font.Height; record.TextBounds = context.Bounds;
        record.Hot = context.Hot; record.Pressed = context.Pressed; record.Focused = context.Focused;
    }
}
