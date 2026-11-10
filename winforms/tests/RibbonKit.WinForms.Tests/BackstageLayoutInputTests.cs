using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageLayoutInputTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Measured_inputs_retain_revealed_footer_geometry_after_live_models_and_font_are_disposed(int dpi) => FoundationTests.Sta(() =>
    {
        foreach (var style in Enum.GetValues<RibbonBackstageStyle>())
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            using var ribbon = new RibbonControl { Density = density, Theme = RibbonThemes.Office2024Light with { BackstageStyle = style, BackstagePagePadding = 17 } };
            ribbon.Backstage.MaximumNavigationWidth = 900;
            ribbon.Backstage.Items.AddRange(new RibbonBackstagePageItem { Text = "Info" }, new RibbonMenuSeparatorItem());
            var hidden = new RibbonBackstagePageItem { Text = new string('W', 35), Visible = false };
            ribbon.Backstage.FooterItems.AddRange(new RibbonMenuSeparatorItem(), hidden);
            using var font = new Font("Segoe UI", 9 * dpi / 96f);
            var family = RibbonPresentationResolver.Backstage(ribbon.Theme);
            var metrics = family.ResolveMetrics(ribbon.Theme, density);
            var size = new Size(RibbonLayoutEngine.Scale(1200, dpi), RibbonLayoutEngine.Scale(700, dpi));
            var withoutReveal = RibbonBackstageLayout.Build(size, font, dpi, ribbon.Backstage);
            var input = RibbonBackstageLayoutMeasurement.Measure(font, dpi, ribbon.Backstage, hidden, 37, metrics, shadow: 6);
            var measured = family.Layout.Build(size, input);
            Assert.True(measured.Page.Left > withoutReveal.Page.Left);
            Assert.Equal(6, measured.Page.Top); Assert.Equal(37 + RibbonLayoutEngine.Scale(20, dpi), measured.Back.Top);
            Assert.False(measured.FooterInline);
            Assert.Equal(measured.Page.Left + 1 + RibbonLayoutEngine.Scale(17, dpi), measured.PageContentBounds(1).Left);
            Assert.Equal(measured.RowHeight + measured.SeparatorHeight +
                (style == RibbonBackstageStyle.Classic ? RibbonLayoutEngine.Scale(14, dpi) : measured.Inset), measured.Footer.Height);
            var shortSize = new Size(size.Width, measured.Navigation.Top + measured.RowHeight);
            var shortLayout = family.Layout.Build(shortSize, input);
            Assert.True(shortLayout.FooterInline);
            Assert.Equal(2 * measured.RowHeight + 2 * measured.SeparatorHeight, shortLayout.NavigationContentHeight);

            ribbon.Theme = ribbon.Theme with { BackstageNavigationPadding = 100, BackstageMinimumRowHeight = 90, BackstagePagePadding = 100 };
            ribbon.Backstage.NavigationWidth = 80; ribbon.Density = density == RibbonDensity.Touch ? RibbonDensity.Compact : RibbonDensity.Touch;
            ribbon.Dispose(); font.Dispose();
            Assert.Equal(measured, family.Layout.Build(size, input));
            Assert.Equal(shortLayout, family.Layout.Build(shortSize, input));
        }
    });

    [Theory] [InlineData(RibbonBackstageStyle.Modern)] [InlineData(RibbonBackstageStyle.Classic)]
    public void Narrow_rail_footer_clearance_keeps_family_fallback_and_explicit_override_independent(RibbonBackstageStyle style) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2024Light with { BackstageStyle = style, BackstageNavigationPadding = 100 } };
        ribbon.Backstage.NavigationWidth = 80;
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info" });
        ribbon.Backstage.FooterItems.Add(new RibbonButtonItem { Text = "Options" });
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var size = new Size(S(140), S(500));
            ribbon.Theme = ribbon.Theme with { BackstageFooterBottomPadding = null };
            var fallback = RibbonBackstageLayout.Build(size, ribbon.Font, dpi, ribbon.Backstage);
            Assert.Equal(style == RibbonBackstageStyle.Modern ? fallback.Inset : S(14), Clearance(fallback));
            Assert.True(fallback.Inset < S(100));
            ribbon.Theme = ribbon.Theme with { BackstageFooterBottomPadding = 0 };
            Assert.Equal(0, Clearance(RibbonBackstageLayout.Build(size, ribbon.Font, dpi, ribbon.Backstage)));
            ribbon.Theme = ribbon.Theme with { BackstageFooterBottomPadding = null };
            Assert.Equal(fallback, RibbonBackstageLayout.Build(size, ribbon.Font, dpi, ribbon.Backstage));
        }
        int Clearance(RibbonBackstageLayout layout) => layout.Footer.Bottom - layout.Arrange(ribbon.Backstage).Last().Bounds.Bottom;
    });

    [Theory] [InlineData(RibbonBackstageStyle.Modern)] [InlineData(RibbonBackstageStyle.Classic)]
    public void Empty_caption_still_reserves_icon_and_text_space_but_no_entries_do_not(RibbonBackstageStyle style) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2024Light with
        { BackstageStyle = style, BackstageAutoMinimumNavigationWidth = 1, BackstageNavigationPadding = 0, BackstageNavigationIconInset = 100 } };
        ribbon.Backstage.MinimumNavigationWidth = 80;
        var empty = RibbonBackstageLayout.Build(new(1000, 500), ribbon.Font, 96, ribbon.Backstage);
        Assert.Equal(80, empty.Page.Left);
        var page = new RibbonBackstagePageItem(); ribbon.Backstage.Items.Add(page);
        Assert.True(RibbonBackstageLayout.Build(new(1000, 500), ribbon.Font, 96, ribbon.Backstage).Page.Left > empty.Page.Left);
        page.Visible = false;
        Assert.Equal(empty, RibbonBackstageLayout.Build(new(1000, 500), ribbon.Font, 96, ribbon.Backstage));
    });

    [Theory] [InlineData(RibbonBackstageStyle.Modern)] [InlineData(RibbonBackstageStyle.Classic)]
    public void Material_layout_uses_current_application_shadow_hook_and_keeps_tiny_host_clamps(RibbonBackstageStyle style) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Backdrop = RibbonWindowBackdrop.Mica, ClientSize = new(900, 560) };
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2024Light with { BackstageStyle = style } };
        using var painter = new ShadowRenderer(); ribbon.Renderer = painter;
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        Assert.True(ribbon.HasWindowBackdrop);
        var layout = RibbonBackstageLayout.Build(new(900, 560), ribbon.Font, ribbon.DeviceDpi, ribbon.Backstage);
        Assert.Equal(7, layout.Page.Top); Assert.Equal(layout.Page.Left - 7, layout.Navigation.Right);
        painter.Shadow = int.MaxValue; ribbon.Renderer = painter;
        var tiny = RibbonBackstageLayout.Build(new(8, 5), ribbon.Font, ribbon.DeviceDpi, ribbon.Backstage);
        Assert.Equal(4, tiny.Page.Top); Assert.Equal(1, tiny.Page.Height); Assert.True(tiny.Navigation.Width >= 0);
        painter.Shadow = 0; ribbon.Renderer = painter;
        Assert.Equal(0, RibbonBackstageLayout.Build(new(900, 560), ribbon.Font, ribbon.DeviceDpi, ribbon.Backstage).Page.Top);
    });

    private sealed class ShadowRenderer : RibbonOffice2024Renderer
    {
        internal int Shadow = 7;
        public override int GetBackstagePageShadowInset(RibbonTheme theme, int deviceDpi) => Shadow;
    }
}
