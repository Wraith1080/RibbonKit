using Xunit;

namespace RibbonKit.WinForms.Tests;

// Offscreen alpha checks; no app launch or focus/input changes.
public sealed class BackdropTransparencyTests
{
    [Fact]
    public void Backstage_opacity_is_opt_in_finite_and_not_a_page_or_control_opacity() => FoundationTests.Sta(() =>
    {
        using var backstage = new RibbonBackstage(); int changes = 0;
        Assert.Equal(1, backstage.NavigationBackgroundOpacity);
        backstage.NavigationBackgroundOpacityChanged += (_, _) => changes++;
        backstage.NavigationBackgroundOpacity = .65f; backstage.NavigationBackgroundOpacity = .65f;
        Assert.Equal(1, changes);
        foreach (float value in new[] { -.1f, 1.1f, float.NaN, float.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => backstage.NavigationBackgroundOpacity = value);
        using var ribbon = new RibbonControl();
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = ribbon.Theme with { QuickAccessBackdropOpacity = float.NaN });
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = ribbon.Theme with { QuickAccessBackdropOpacity = 1.1f });
    });

    [Theory] [InlineData(true, false, 153)] [InlineData(false, false, 255)] [InlineData(true, true, 255)]
    public void Qat_only_tints_backdrop_backgrounds_and_keeps_vector_ink_opaque(bool composited, bool menu, int expectedAlpha) => FoundationTests.Sta(() =>
    {
        using var item = new RibbonButtonItem { Text = "Bold", Icon = new SolidIcon() };
        using var entry = new RibbonQuickAccessReference { Source = item };
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup();
        group.Items.Add(item); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.QuickAccessItems.Add(entry);
        using var renderer = new RibbonRenderer(); using var buffer = new RibbonBackdropBuffer();
        using var output = new Bitmap(60, 44); using var graphics = Graphics.FromImage(output);
        buffer.Paint(graphics, output.Size, output.Height, Color.Transparent, canvas => renderer.DrawQuickAccessItem(
            new(canvas, new(Point.Empty, output.Size), RibbonThemes.Office2024Light, SystemFonts.MessageBoxFont!, 96, Hot: true) { CompositedText = composited }, entry, menu));
        Assert.InRange(buffer.Image.GetPixel(8, 8).A, expectedAlpha - 1, expectedAlpha + 1);
        int ink = 0;
        for (int y = 0; y < output.Height; y++) for (int x = 0; x < output.Width; x++)
        { var pixel = buffer.Image.GetPixel(x, y); if (pixel.A == 255 && pixel.R < 40) ink++; }
        Assert.True(ink > 32, "QAT artwork must retain opaque ink.");
    });
    private sealed class SolidIcon : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        { using var fill = new SolidBrush(context.Foreground); graphics.FillRectangle(fill, bounds); }
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Entries_replace_the_rail_tint_without_compounding_alpha_in_motion(bool back) => FoundationTests.Sta(() =>
    {
        using var item = new RibbonButtonItem { Text = "Info" };
        using var renderer = new RibbonRenderer(); using var buffer = new RibbonBackdropBuffer();
        using var output = new Bitmap(250, 110); using var graphics = Graphics.FromImage(output);
        var theme = RibbonThemes.Office2024Light with { BackstageNavigationBackground = Color.FromArgb(120, 24, 72, 142) };
        buffer.Paint(graphics, output.Size, output.Height, Color.Transparent, canvas =>
        {
            var context = new RibbonPaintContext(canvas, new(Point.Empty, output.Size), theme, SystemFonts.MessageBoxFont!, 96) { CompositedText = true };
            renderer.DrawBackstageBackground(context, output.Width);
            renderer.DrawBackstageEntry(context with { Bounds = new(10, 20, 230, 52) }, back ? null : item, false);
        });
        Assert.InRange(buffer.Image.GetPixel(20, 25).A, 119, 121);
        Assert.InRange(buffer.Image.GetPixel(20, 5).A, 119, 121);
    });

    [Theory] [InlineData(0)] [InlineData(120)] [InlineData(255)]
    public void Backstage_rail_has_authored_alpha_and_page_stays_opaque(int alpha) => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonRenderer(); using var buffer = new RibbonBackdropBuffer();
        using var output = new Bitmap(300, 180); using var graphics = Graphics.FromImage(output);
        var theme = RibbonThemes.Office2024Light with { BackstageNavigationBackground = Color.FromArgb(alpha, 245, 244, 243) };
        buffer.Paint(graphics, output.Size, output.Height, Color.Transparent, canvas => renderer.DrawBackstageBackground(
            new(canvas, new(Point.Empty, output.Size), theme, SystemFonts.MessageBoxFont!, 96) { CompositedText = true }, 140));
        Assert.InRange(buffer.Image.GetPixel(20, 20).A, Math.Max(0, alpha - 1), Math.Min(255, alpha + 1));
        Assert.Equal(Color.White.ToArgb(), buffer.Image.GetPixel(200, 20).ToArgb());
    });

    [Theory] [InlineData(0)] [InlineData(70)]
    public void Backstage_entry_text_keeps_coverage_at_its_actual_navigation_row(int top) => FoundationTests.Sta(() =>
    {
        using var item = new RibbonButtonItem { Text = "Information" };
        using var font = new Font("Segoe UI", 14);
        using var renderer = new RibbonRenderer(); using var buffer = new RibbonBackdropBuffer();
        using var output = new Bitmap(250, top + 52); using var graphics = Graphics.FromImage(output);
        var theme = RibbonThemes.Office2024Light with { BackstageNavigationBackground = Color.FromArgb(120, 245, 244, 243) };
        buffer.Paint(graphics, output.Size, output.Height, Color.Transparent, canvas => renderer.DrawBackstageEntry(
            new(canvas, new(0, top, 250, 52), theme, font, 96) { CompositedText = true }, item, false));
        Assert.InRange(buffer.Image.GetPixel(240, top + 5).A, 119, 121);
        int ink = 0;
        for (int y = top + 6; y < top + 46; y++) for (int x = 44; x < 220; x++)
        { var pixel = buffer.Image.GetPixel(x, y); if (pixel.A > 240 && pixel.R < 80) ink++; }
        Assert.True(ink > 40, "Navigation text must retain opaque glyph coverage.");
    });
}
