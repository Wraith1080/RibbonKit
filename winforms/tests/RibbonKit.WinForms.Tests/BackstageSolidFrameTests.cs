using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageSolidFrameTests
{
    [Theory] [InlineData(RibbonWindowBackdrop.Mica)] [InlineData(RibbonWindowBackdrop.Acrylic)]
    public void Material_rail_opacity_changes_keep_one_native_page_layer_during_opening_and_closing(RibbonWindowBackdrop material) => FoundationTests.Sta(() =>
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) || SystemInformation.HighContrast) return;
        using var host = new RibbonForm { Bounds = new(180, 120, 950, 650) };
        var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        using var page = new Panel(); var field = new TextBox { Text = "Retained native text" }; page.Controls.Add(field);
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); host.Backdrop = material;
        Assert.Equal(material, host.EffectiveBackdrop); ribbon.Motion.SystemPolicyOverride = true;
        nint fieldHandle = field.Handle;
        foreach (float opacity in new[] { 1f, 0f, 1f, .65f })
        {
            ribbon.Backstage.NavigationBackgroundOpacity = opacity; ribbon.IsBackstageOpen = true;
            var surface = ribbon.BackstageSurface!; var pageHandle = surface.PageArea.Handle;
            Assert.False(RibbonDrawingHold.HasOpacity(surface.Handle)); Assert.True(RibbonDrawingHold.HasOpacity(pageHandle));
            ribbon.BackstageOpeningMotion!.SetProgress(.5);
            Assert.InRange(surface.MotionAlpha, 127, 128);
            ribbon.SettleMotion();
            ribbon.Backstage.NavigationBackgroundOpacity = 1 - opacity;
            Assert.Equal(pageHandle, surface.PageArea.Handle); Assert.False(RibbonDrawingHold.HasOpacity(surface.Handle));
            Assert.True(RibbonDrawingHold.HasOpacity(pageHandle));
            ribbon.IsBackstageOpen = false; ribbon.BackstageOpeningMotion!.SetProgress(.5);
            Assert.InRange(surface.MotionAlpha, 127, 128); Assert.Equal(fieldHandle, field.Handle);
            Assert.Equal("Retained native text", field.Text); ribbon.SettleMotion();
            Assert.False(RibbonDrawingHold.HasOpacity(pageHandle));
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Solid_and_material_pages_share_flush_borders_and_only_the_top_left_corner_is_rounded(int dpi) => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonOffice2024Renderer();
        foreach (int extra in new[] { 0, 1 })
        foreach (bool material in new[] { false, true })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var theme = RibbonThemes.Office2024Light; var bounds = new Rectangle(4, 4, S(360) + extra, S(180) + extra);
            int nav = S(160), x = bounds.Left + nav, y = bounds.Top;
            var page = new Rectangle(x, y, bounds.Width - nav, bounds.Height);
            int radius = RibbonBackstageLayout.PageCornerRadius(renderer, theme, dpi, page.Size);
            int stroke = RibbonBackstageLayout.PageBorderWidth(renderer, theme, dpi, page.Size);
            using var image = new Bitmap(bounds.Right + 4, bounds.Bottom + 4); using var graphics = Graphics.FromImage(image); graphics.Clear(Color.Magenta);
            renderer.DrawBackstageBackground(new(graphics, bounds, theme, SystemFonts.MessageBoxFont!, dpi) { WindowBackdrop = material }, nav);
            Assert.Equal(1, stroke);
            Assert.Equal(theme.BackstageNavigationBackground.ToArgb(), image.GetPixel(x, y).ToArgb());
            Assert.Equal(theme.Border.ToArgb(), image.GetPixel(page.Right - 1, y).ToArgb());
            Assert.Equal(theme.Border.ToArgb(), image.GetPixel(x, page.Bottom - 1).ToArgb());
            Assert.Equal(theme.Surface.ToArgb(), image.GetPixel(page.Right - 1, page.Bottom - 1).ToArgb());
            for (int px = x + radius; px < page.Right; px++)
            {
                Assert.Equal(theme.Border.ToArgb(), image.GetPixel(px, y).ToArgb());
                Assert.Equal(theme.Surface.ToArgb(), image.GetPixel(px, y + stroke).ToArgb());
            }
            Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(x + radius, y - 1).ToArgb());
            Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(page.Right, y + radius).ToArgb());
            Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(x + radius, page.Bottom).ToArgb());

            int innerRadius = radius - stroke; var inner = new Size(page.Width - stroke, page.Height - stroke);
            using var clip = RoundedPainter.CreateOpaqueRegion(new(inner.Width + innerRadius, inner.Height + innerRadius), innerRadius);
            clip.Intersect(new Rectangle(Point.Empty, inner));
            Assert.False(clip.IsVisible(0, 0)); Assert.True(clip.IsVisible(inner.Width - 1, 0));
            Assert.True(clip.IsVisible(0, inner.Height - 1)); Assert.True(clip.IsVisible(inner.Width - 1, inner.Height - 1));
            for (int py = 0; py < radius + 2; py++) for (int px = 0; px < radius + 2; px++)
                if (clip.IsVisible(px, py)) Assert.Equal(theme.Surface.ToArgb(), image.GetPixel(x + stroke + px, y + stroke + py).ToArgb());
            string directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "artifacts", "solid-backstage-frame-20261030", "raster"));
            Directory.CreateDirectory(directory); image.Save(Path.Combine(directory, $"border-{material}-{dpi}-{extra}.png"));
        }
    });

    [Fact]
    public void Open_page_keeps_the_same_border_clip_across_material_changes_without_recreating_native_fields() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(180, 120, 950, 650) };
        var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage }; ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        using var page = new Panel(); var field = new TextBox { Text = "Retained document", Bounds = new(30, 30, 300, 30) }; page.Controls.Add(field);
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true;
        var area = ribbon.BackstageSurface!.PageArea; nint handle = field.Handle; var parent = field.Parent;
        Assert.Equal(1, area.Top); Assert.NotNull(area.Region); Assert.False(area.Region.IsVisible(0, 0));
        foreach (var material in new[] { RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic, RibbonWindowBackdrop.None })
        {
            host.Backdrop = material;
            Assert.Equal(1, area.Top); Assert.NotNull(area.Region); Assert.False(area.Region.IsVisible(0, 0));
            Assert.Equal(handle, field.Handle); Assert.Same(parent, field.Parent); Assert.Equal("Retained document", field.Text);
        }
    });
}
