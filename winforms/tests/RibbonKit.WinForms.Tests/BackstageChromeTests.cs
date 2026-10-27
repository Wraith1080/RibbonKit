using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageChromeTests
{
    [Fact]
    public void Selected_marker_has_symmetric_rounded_ends_at_odd_even_bounds_and_representative_dpi() => FoundationTests.Sta(() =>
    {
        using var office = new RibbonOffice2024Renderer(); using var neutral = new RibbonRenderer();
        using var page = new RibbonBackstagePageItem { Text = "Info" };
        var theme = RibbonThemes.Office2024Light;
        foreach (var renderer in new[] { neutral, office })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int logicalHeight in new[] { 42, 43 })
        foreach (var (hot, pressed) in new[] { (false, false), (true, false), (true, true) })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            var bounds = new Rectangle(8, 8, S(200), S(logicalHeight));
            using var bitmap = new Bitmap(bounds.Right + 8, bounds.Bottom + 8); using var graphics = Graphics.FromImage(bitmap);
            renderer.DrawBackstageEntry(new(graphics, bounds, theme, SystemFonts.DefaultFont, dpi, hot, pressed), page, true);
            var marker = new Rectangle(bounds.Left + S(2), bounds.Top + S(10), S(3), bounds.Height - S(20));
            Assert.NotEqual(theme.Accent.ToArgb(), bitmap.GetPixel(marker.Left, marker.Top).ToArgb());
            Assert.Equal(theme.Accent.ToArgb(), bitmap.GetPixel(marker.Left, marker.Top + marker.Height / 2).ToArgb());
            for (int y = 0; y < marker.Height; y++)
            for (int x = 0; x < marker.Width; x++)
            {
                int pixel = bitmap.GetPixel(marker.Left + x, marker.Top + y).ToArgb();
                Assert.Equal(pixel, bitmap.GetPixel(marker.Right - 1 - x, marker.Top + y).ToArgb());
                Assert.Equal(pixel, bitmap.GetPixel(marker.Left + x, marker.Bottom - 1 - y).ToArgb());
            }
        }
    });

    [Fact]
    public void Back_hover_press_and_focus_follow_the_circle_without_square_feedback() => FoundationTests.Sta(() =>
    {
        using var office = new RibbonOffice2024Renderer(); using var neutral = new RibbonRenderer(); var theme = RibbonThemes.Office2024Light;
        string directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "TestResults", "visual", "m4c-backstage-chrome"));
        Directory.CreateDirectory(directory);
        foreach (var renderer in new[] { neutral, office })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int logicalSize in new[] { 36, 37 })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            int size = S(logicalSize), diameter = size - 2 * S(2), offset = (size - diameter) / 2;
            var bounds = new Rectangle(8, 8, size, size);
            using var montage = new Bitmap((size + 16) * 4, size + 16); using var output = Graphics.FromImage(montage); output.Clear(theme.BackstageNavigationBackground);
            int column = 0;
            foreach (var (hot, pressed, focus) in new[] { (false, false, false), (true, false, false), (true, true, false), (false, false, true) })
            {
                using var bitmap = new Bitmap(size + 16, size + 16); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(theme.BackstageNavigationBackground);
                renderer.DrawBackstageEntry(new(graphics, bounds, theme, SystemFonts.DefaultFont, dpi, hot, pressed, focus), null, false);
                var circle = new Rectangle(bounds.Left + offset, bounds.Top + offset, diameter, diameter);
                // Inspect each corner and the complete circular edge band; the arrow is inside it.
                for (int y = 0; y < diameter; y++)
                for (int x = 0; x < diameter; x++)
                {
                    if (Math.Min(x, diameter - 1 - x) > S(4) && Math.Min(y, diameter - 1 - y) > S(4)) continue;
                    int pixel = bitmap.GetPixel(circle.Left + x, circle.Top + y).ToArgb();
                    Assert.Equal(pixel, bitmap.GetPixel(circle.Right - 1 - x, circle.Top + y).ToArgb());
                    Assert.Equal(pixel, bitmap.GetPixel(circle.Left + x, circle.Bottom - 1 - y).ToArgb());
                }
                Assert.Equal(theme.BackstageNavigationBackground.ToArgb(), bitmap.GetPixel(bounds.Left + S(3), bounds.Top + S(3)).ToArgb());
                Assert.Equal((focus ? theme.Accent : theme.Foreground).ToArgb(), bitmap.GetPixel(circle.Left + diameter / 2, circle.Top + 1).ToArgb());
                if (hot || pressed)
                    Assert.Equal((pressed ? theme.Pressed.Start : theme.Hover.Start).ToArgb(), bitmap.GetPixel(circle.Left + diameter / 2, circle.Top + S(5)).ToArgb());
                output.DrawImageUnscaled(bitmap, column++ * (size + 16), 0);
            }
            montage.Save(Path.Combine(directory, $"{(renderer == office ? "office" : "neutral")}-{dpi}dpi-{logicalSize}.png"));
        }
    });
}
