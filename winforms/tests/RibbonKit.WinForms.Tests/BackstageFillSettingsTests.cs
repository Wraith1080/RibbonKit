using System.Drawing.Imaging;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageFillSettingsTests
{
    [Theory]
    [InlineData(RibbonBackstageStyle.Classic, false, false)] [InlineData(RibbonBackstageStyle.Modern, false, false)]
    [InlineData(RibbonBackstageStyle.Classic, false, true)] [InlineData(RibbonBackstageStyle.Modern, false, true)]
    [InlineData(RibbonBackstageStyle.Classic, true, false)] [InlineData(RibbonBackstageStyle.Modern, true, false)]
    [InlineData(RibbonBackstageStyle.Classic, true, true)] [InlineData(RibbonBackstageStyle.Modern, true, true)]
    public void Authored_navigation_fills_reach_material_rows_with_their_alpha(RibbonBackstageStyle style, bool fullWidth, bool composited) => FoundationTests.Sta(() =>
    {
        var hover = Color.FromArgb(128, 210, 30, 110); var selected = Color.FromArgb(192, 30, 110, 210);
        var theme = RibbonThemes.Office2024Light with { BackstageStyle = style, BackstageFullWidthNavigation = fullWidth,
            BackstageNavigationHover = new(hover), BackstageNavigationSelected = new(selected),
            BackdropHover = new(Color.Lime), BackdropPressed = new(Color.Yellow), BackstageSelectionIndicatorVisible = false };
        using var painter = new RibbonOffice2024Renderer(); using var item = new RibbonButtonItem { Text = "Entry" };
        foreach (int state in new[] { 0, 1, 2 })
        {
            using var image = new Bitmap(240, 52); using var graphics = Graphics.FromImage(image); graphics.Clear(Color.White);
            var context = new RibbonPaintContext(graphics, new(0, 0, 240, 52), theme, Control.DefaultFont, 120, Hot: state == 0, Pressed: state == 2)
                { WindowBackdrop = true, CompositedText = composited };
            painter.DrawBackstageEntry(context, item, state == 1);
            using var expected = new Bitmap(1, 1); using var reference = Graphics.FromImage(expected);
            reference.Clear(theme.BackstageNavigationColor); using var fill = new SolidBrush(state == 0 ? hover : selected); reference.FillRectangle(fill, 0, 0, 1, 1);
            Assert.Equal(expected.GetPixel(0, 0).ToArgb(), image.GetPixel(210, 26).ToArgb());
        }
    });

    [Fact]
    public void Default_navigation_paint_matches_saved_state_frames() => FoundationTests.Sta(() =>
    {
        foreach (bool classic in new[] { false, true })
        foreach (bool fullWidth in new[] { false, true })
        foreach (bool accent in new[] { false, true })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 950, 650), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage,
                Theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { BackstageFullWidthNavigation = fullWidth, BackstageNavigationUsesAccent = accent } };
            ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
            ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info" }); ribbon.Backstage.NavigationBackgroundOpacity = .65f;
            host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true;
            var surface = ribbon.BackstageSurface!; var theme = surface.PaintTheme;
            int dpi = ribbon.DeviceDpi; int S(int n) => (int)Math.Round(n * dpi / 96d);
            using var image = new Bitmap(S(240), S(240)); using var graphics = Graphics.FromImage(image); graphics.Clear(Color.White);
            using var item = new RibbonButtonItem { Text = "Entry" };
            using var painter = new RibbonOffice2024Renderer();
            for (int state = 0; state < 4; state++)
                painter.DrawBackstageEntry(new(graphics, new(0, S(state * 60), S(240), S(52)), theme, ribbon.Font, dpi, Hot: state == 1, Pressed: state == 3)
                    { WindowBackdrop = ribbon.HasWindowBackdrop, CompositedText = ribbon.HasWindowBackdrop }, item, state == 2);
            var geometry = $"dpi={dpi};effective={host.EffectiveBackdrop};nav={surface.NavigationBounds};page={surface.PageArea.Bounds}";
            string name = $"navigation-{classic}-{fullWidth}-{accent}-{backdrop}";
            using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); var png = stream.ToArray();
            if (Environment.GetEnvironmentVariable("RIBBONKIT_BACKSTAGE_EFFECT_CAPTURE") is { Length: > 0 } output)
            { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), png); File.WriteAllText(Path.Combine(output, name + ".txt"), geometry); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_BACKSTAGE_EFFECT_BASELINE") is { Length: > 0 } baseline)
            { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), geometry); }
        }
    });
}
