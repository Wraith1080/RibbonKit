using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class SplitChromeRenderingTests
{
    [Theory]
    [InlineData(120)]
    [InlineData(192)]
    public void Joined_edges_are_square_and_have_exactly_one_separator_pixel(int dpi) => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonOffice2024Renderer();
        var theme = RibbonThemes.Office2024Light;
        foreach (bool vertical in new[] { false, true })
        foreach (bool arrowActive in new[] { false, true })
        foreach (bool pressed in new[] { false, true })
        foreach (bool check in new[] { false, true })
        {
            using var split = new RibbonSplitButtonItem { Layout = vertical ? RibbonSplitButtonLayout.Vertical : RibbonSplitButtonLayout.Horizontal, Checked = check };
            var bounds = new Rectangle(4, 4, RibbonLayoutEngine.Scale(90, dpi), RibbonLayoutEngine.Scale(100, dpi));
            var (primary, arrow) = RibbonSplitLayout.Calculate(bounds, split, RibbonItemSize.Large, false, dpi);
            using var bitmap = new Bitmap(bounds.Right + 4, bounds.Bottom + 4);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(theme.Surface);
            var context = new RibbonPaintContext(graphics, primary, theme, SystemFonts.DefaultFont, dpi,
                Hot: !arrowActive, Pressed: !arrowActive && pressed);
            renderer.DrawSplitItem(context, context with { Bounds = arrow, Hot = arrowActive, Pressed = arrowActive && pressed }, split, RibbonItemSize.Large, false);
            int fill = (pressed ? theme.Pressed.Start : theme.Hover.Start).ToArgb();
            int primaryFill = arrowActive ? (check ? theme.Checked.Start : theme.Surface).ToArgb() : fill;
            int arrowFill = arrowActive ? fill : theme.Surface.ToArgb();
            // Include points immediately inside both outer edges. Rounded inner
            // corners and the old half inset leave visible holes at these points.
            var points = vertical ? new[] { bounds.Left + 2, bounds.Left + bounds.Width / 2, bounds.Right - 3 }
                : new[] { bounds.Top + 2, bounds.Top + bounds.Height / 3, bounds.Bottom - 3 };
            foreach (int point in points)
            {
                for (int offset = -2; offset <= 2; offset++)
                {
                    int expected = offset < 0 ? primaryFill : offset == 0 ? theme.Border.ToArgb() : arrowFill;
                    var pixel = vertical ? bitmap.GetPixel(point, arrow.Top + offset) : bitmap.GetPixel(arrow.Left + offset, point);
                    Assert.Equal(expected, pixel.ToArgb());
                }
            }
            // Both top/bottom or left/right outer corners still share the same
            // rounded contour; the joined edge is the only square edge.
            int edgeY = vertical ? (arrowActive ? bounds.Bottom - 2 : bounds.Top + 1) : bounds.Top + 1;
            int edgeX = vertical ? bounds.Left + 1 : (arrowActive ? bounds.Right - 2 : bounds.Left + 1);
            Assert.NotEqual(fill, bitmap.GetPixel(edgeX, edgeY).ToArgb());
        }
    });
}
