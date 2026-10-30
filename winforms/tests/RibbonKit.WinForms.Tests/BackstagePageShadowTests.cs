using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstagePageShadowTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Material_page_uses_crisp_borders_without_shadow_coverage_or_caption_bleed(bool transparent) => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonOffice2024Renderer();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var theme = RibbonThemes.Office2024Light with { BackstageNavigationBackground = transparent ? Color.Transparent : Color.White, BodyShadowColor = Color.Magenta };
            var size = new Size(S(360), S(180)); var bounds = new Rectangle(S(10), S(10), size.Width, size.Height);
            using var image = new Bitmap(bounds.Right + S(10), bounds.Bottom + S(10)); using var graphics = Graphics.FromImage(image);
            using var buffer = new RibbonBackdropBuffer();
            buffer.Paint(graphics, image.Size, image.Height, Color.Transparent, canvas => renderer.DrawBackstageBackground(
                new(canvas, bounds, theme, SystemFonts.MessageBoxFont!, dpi) { CompositedText = transparent, WindowBackdrop = true }, S(160)));
            int edge = bounds.Left + S(160), top = bounds.Top;
            Assert.Equal(0, renderer.GetBackstagePageShadowInset(theme, dpi));
            var outside = buffer.Image.GetPixel(edge - 1, top + S(30));
            if (transparent) Assert.Equal(0, outside.A);
            else Assert.Equal(Color.White.ToArgb(), outside.ToArgb());
            Assert.Equal(theme.Border.ToArgb(), buffer.Image.GetPixel(edge, top + S(30)).ToArgb());
            Assert.Equal(theme.Border.ToArgb(), buffer.Image.GetPixel(edge + S(30), top).ToArgb());
            Assert.Equal(Color.White.ToArgb(), buffer.Image.GetPixel(edge + S(30), top + 1).ToArgb());
            Assert.Equal(0, buffer.Image.GetPixel(edge + S(30), bounds.Top - 1).A); // Caption is outside the painted surface.
            Assert.Equal(0, buffer.Image.GetPixel(bounds.Right, top + S(30)).A); // No right/bottom bleed.
            Assert.Equal(0, buffer.Image.GetPixel(edge + S(30), bounds.Bottom).A);
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Solid_layout_reserves_no_shadow_band_regardless_of_shadow_colour(bool office) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer(); ribbon.Renderer = renderer;
        ribbon.Backstage.NavigationWidth = 220;
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (bool enabled in new[] { false, true })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            ribbon.Theme = ribbon.Theme with { BodyShadowColor = enabled ? Color.FromArgb(24, Color.Black) : Color.Transparent };
            var size = new Size(S(1000), S(500)); var layout = RibbonBackstageLayout.Build(size, ribbon.Font, dpi, ribbon.Backstage);
            int inset = 0;
            Assert.Equal(inset, layout.Page.Top); Assert.Equal(S(220), layout.Page.Left);
            Assert.Equal(inset, layout.Page.Left - layout.Navigation.Right);
            Assert.Equal(size.Width, layout.Page.Right); Assert.Equal(size.Height, layout.Page.Bottom);
            var narrow = RibbonBackstageLayout.Build(new(8, 5), ribbon.Font, dpi, ribbon.Backstage);
            Assert.True(narrow.Page.Width >= 0 && narrow.Page.Height > 0 && narrow.Navigation.Width >= 0);
        }
    });
}
