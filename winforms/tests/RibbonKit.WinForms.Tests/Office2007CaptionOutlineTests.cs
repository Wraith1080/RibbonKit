using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2007CaptionOutlineTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Caption_outlines_are_one_device_pixel_on_hover_press_and_absent_at_idle(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new RoundedPainter(); using var image = new Bitmap(S(70), S(50)); using var g = Graphics.FromImage(image);
        var t = RibbonThemes.Office2007Blue; var a = t.CaptionButtonAppearance! with { GlyphSize = 0 }; t = t with { CaptionButtonAppearance = a };
        var target = new Rectangle(S(4), S(4), S(58), S(38)); var metrics = new RibbonCaptionButtonMetrics(t, dpi); var paint = metrics.StateBounds(target);
        foreach (bool plane in new[] { false, true }) foreach (int hit in new[] { 8, 9, 20 }) foreach (int state in new[] { 0, 1, 2, 3 })
        {
            g.Clear(Color.Transparent); var c = new RibbonPaintContext(g, target, t, Control.DefaultFont, dpi, Hot: state is 1 or 2, Pressed: state is 2 or 3) { CompositedText = plane };
            RibbonCaptionButtonRenderer.Draw(painter, c, hit, false, plane, _ => { });
            if (state is 0 or 3) { Assert.All(Pixels(image), pixel => Assert.Equal(0, pixel.A)); continue; }
            var chrome = hit == 20 ? a.CloseStateChrome! : a.StateChrome!; var border = state == 2 ? chrome.PressedBorderColor : chrome.HoverBorderColor;
            Assert.Equal(border.ToArgb(), image.GetPixel(paint.Left + paint.Width / 2, paint.Top).ToArgb());
            var fill = hit == 20 ? state == 2 ? a.ClosePressedBackground!.Value : a.CloseHoverBackground!.Value : state == 2 ? a.PressedBackground!.Value : a.HoverBackground!.Value;
            Assert.Equal(RibbonFillColors.At(fill, 1d / (paint.Height - 1)).ToArgb(), image.GetPixel(paint.Left + paint.Width / 2, paint.Top + 1).ToArgb());
            Assert.Equal(0, image.GetPixel(paint.Left + paint.Width / 2, paint.Top - 1).A);
            for (int y = 0; y < S(5); y++) for (int x = 0; x < S(5); x++)
            {
                var alpha = image.GetPixel(paint.Left + x, paint.Top + y).A;
                Assert.Equal(alpha, image.GetPixel(paint.Right - 1 - x, paint.Top + y).A); Assert.Equal(alpha, image.GetPixel(paint.Left + x, paint.Bottom - 1 - y).A); Assert.Equal(alpha, image.GetPixel(paint.Right - 1 - x, paint.Bottom - 1 - y).A);
            }
            var hidden = chrome with { HoverBorderColor = Color.Transparent, PressedBorderColor = Color.Transparent };
            var reset = t with { CaptionButtonAppearance = hit == 20 ? a with { CloseStateChrome = hidden } : a with { StateChrome = hidden } };
            g.Clear(Color.Transparent); RibbonCaptionButtonRenderer.Draw(painter, c with { Theme = reset }, hit, false, plane, _ => { });
            Assert.Equal(fill.Start.ToArgb(), image.GetPixel(paint.Left + paint.Width / 2, paint.Top).ToArgb());
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Restore_underlay_and_caller_clips_survive_bordered_caption_states(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new RoundedPainter(); using var image = new Bitmap(S(100), S(70)); using var g = Graphics.FromImage(image);
        var t = RibbonThemes.Office2007Blue; var a = t.CaptionButtonAppearance! with { GlyphSize = 24, GlyphStrokeWidth = 1, RestoreOffset = 6 }; t = t with { CaptionButtonAppearance = a };
        var target = new Rectangle(S(5), S(5), S(80), S(56)); var glyph = new RibbonCaptionButtonMetrics(t, dpi).GlyphBounds(target); var point = new Point(glyph.Left + S(3), glyph.Top + S(10));
        foreach (bool plane in new[] { false, true }) foreach (bool hot in new[] { false, true }) foreach (bool pressed in new[] { false, true })
        {
            var background = plane ? Color.Transparent : Color.MidnightBlue; var c = new RibbonPaintContext(g, target, t, Control.DefaultFont, dpi, Hot: hot, Pressed: pressed) { CompositedText = plane };
            g.Clear(background); RibbonCaptionButtonRenderer.Draw(painter, c, 9, true, plane, bounds => { using var brush = new SolidBrush(background); g.FillRectangle(brush, bounds); }); var restorePixel = image.GetPixel(point.X, point.Y);
            g.Clear(background); RibbonCaptionButtonRenderer.Draw(painter, c with { Theme = t with { CaptionButtonAppearance = a with { GlyphSize = 0 } } }, 9, false, plane, _ => { }); Assert.Equal(restorePixel, image.GetPixel(point.X, point.Y));
            g.Clear(background); int untouched = image.GetPixel(target.Left, target.Top).ToArgb(); var clip = Rectangle.Inflate(target, -S(4), -S(4)); g.SetClip(clip); RibbonCaptionButtonRenderer.Draw(painter, c, 20, false, plane, _ => { });
            Assert.Equal(untouched, image.GetPixel(target.Left, target.Top).ToArgb()); g.ResetClip();
        }
    });

    [Fact]
    public void Preset_keeps_idle_empty_and_existing_independent_state_resets()
    {
        var a = RibbonThemes.Office2007Blue.CaptionButtonAppearance!;
        Assert.True(a.StateChrome!.IdleBorderColor.IsEmpty); Assert.True(a.CloseStateChrome!.IdleBorderColor.IsEmpty);
        Assert.NotEqual(a.StateChrome.HoverBorderColor, a.CloseStateChrome.HoverBorderColor);
        Assert.NotNull((a with { StateChrome = null }).CloseStateChrome); Assert.NotNull((a with { CloseStateChrome = null }).StateChrome);
    }
    private static IEnumerable<Color> Pixels(Bitmap image) { for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) yield return image.GetPixel(x, y); }
}
