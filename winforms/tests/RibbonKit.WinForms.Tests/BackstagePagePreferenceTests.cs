using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstagePagePreferenceTests
{
    [Fact]
    public void Default_page_space_layouts_can_be_compared_across_the_change() => FoundationTests.Sta(() =>
    {
        var output = Environment.GetEnvironmentVariable("RIBBONKIT_A10_CAPTURE");
        if (!string.IsNullOrEmpty(output)) Directory.CreateDirectory(output);
        var lines = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            using var ribbon = new RibbonControl { BuiltInTheme = family, Density = density };
            ribbon.Backstage.MaximumNavigationWidth = 1000;
            ribbon.Backstage.Items.AddRange(new RibbonBackstagePageItem { Text = "Document information and publication settings" }, new RibbonMenuSeparatorItem(), new RibbonButtonItem { Text = "Save" });
            ribbon.Backstage.FooterItems.Add(new RibbonButtonItem { Text = "Options" });
            using var font = new Font("Segoe UI", 9 * dpi / 96f);
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            foreach (int width in new[] { 160, 640, 1000 })
            foreach (int height in new[] { 120, 500 })
            foreach (int? navigation in new int?[] { null, 320, 1000 })
            {
                ribbon.Backstage.NavigationWidth = navigation;
                var layout = RibbonBackstageLayout.Build(new(S(width), S(height)), font, dpi, ribbon.Backstage, captionHeight: family == RibbonBuiltInTheme.Office2019Light ? S(32) : 0);
                lines.Add($"{family}/{density}/{dpi}/{width}/{height}/{navigation}: {layout}; " + string.Join("; ", layout.Arrange(ribbon.Backstage).Select(slot => $"{slot.Item.Text}:{slot.Bounds}/{slot.Clip}/{slot.Footer}")));
                ribbon.Theme = ribbon.Theme with { BackstagePreferredMinimumPageWidth = 240 };
                Assert.Equal(layout, RibbonBackstageLayout.Build(new(S(width), S(height)), font, dpi, ribbon.Backstage, captionHeight: family == RibbonBuiltInTheme.Office2019Light ? S(32) : 0));
                ribbon.Theme = ribbon.Theme with { BackstagePreferredMinimumPageWidth = null };
            }
        }
        if (!string.IsNullOrEmpty(output)) File.WriteAllLines(Path.Combine(output, "default-layouts.txt"), lines);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Page_preference_caps_explicit_and_auto_widths_without_rewriting_constraints(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            using var ribbon = new RibbonControl { BuiltInTheme = family, Density = density };
            var page = new RibbonBackstagePageItem { Text = new string('W', 250) }; ribbon.Backstage.Items.Add(page);
            ribbon.Backstage.MinimumNavigationWidth = 80; ribbon.Backstage.MaximumNavigationWidth = 1000;
            using var font = new Font("Segoe UI", 9 * dpi / 96f);
            foreach (var (preference, expectedRail) in new[] { (0, 1000), (160, 840), (240, 760), (360, 640), (600, 500), (1024, 500) })
            {
                ribbon.Theme = ribbon.Theme with { BackstagePreferredMinimumPageWidth = preference };
                foreach (int? width in new int?[] { null, 1000, int.MaxValue })
                {
                    ribbon.Backstage.NavigationWidth = width;
                    var layout = RibbonBackstageLayout.Build(new(S(1000), S(500)), font, dpi, ribbon.Backstage);
                    Assert.Equal(S(expectedRail), layout.Page.Left); Assert.Equal(S(1000) - S(expectedRail), layout.Page.Width);
                    Assert.Equal(width, ribbon.Backstage.NavigationWidth); Assert.Equal(80, ribbon.Backstage.MinimumNavigationWidth); Assert.Equal(1000, ribbon.Backstage.MaximumNavigationWidth);
                    Assert.True(layout.Back.Width >= 0); Assert.True(layout.Navigation.Width >= 0);
                }
            }
            ribbon.Theme = ribbon.Theme with { BackstagePreferredMinimumPageWidth = 360 };
            ribbon.Backstage.MinimumNavigationWidth = 800; ribbon.Backstage.MaximumNavigationWidth = 600; ribbon.Backstage.NavigationWidth = 300;
            Assert.Equal(S(640), RibbonBackstageLayout.Build(new(S(1000), S(500)), font, dpi, ribbon.Backstage).Page.Left);
            Assert.Equal(800, ribbon.Backstage.MinimumNavigationWidth); Assert.Equal(600, ribbon.Backstage.MaximumNavigationWidth); Assert.Equal(300, ribbon.Backstage.NavigationWidth);
            ribbon.Theme = ribbon.Theme with { BackstagePreferredMinimumPageWidth = 1024 };
            foreach (int width in new[] { 0, 1, S(79), S(160), S(641) })
            {
                var layout = RibbonBackstageLayout.Build(new(width, S(85)), font, dpi, ribbon.Backstage);
                Assert.Equal(width / 2, layout.Page.Left); Assert.Equal(width - width / 2, layout.Page.Width);
                Assert.InRange(layout.Back.Right, 0, width); Assert.True(layout.Navigation.Width >= 0);
            }
            var profile = RibbonPresentationResolver.Backstage(ribbon.Theme);
            var input = RibbonBackstageLayoutMeasurement.Measure(font, dpi, ribbon.Backstage, null, 0, profile.ResolveMetrics(ribbon.Theme, density), 0);
            var frozen = profile.Layout.Build(new(S(1000), S(500)), input);
            ribbon.Theme = ribbon.Theme with { BackstagePreferredMinimumPageWidth = 0 }; font.Dispose(); ribbon.Dispose();
            Assert.Equal(1024, input.Metrics.MinimumPageWidth); Assert.Equal(frozen, profile.Layout.Build(new(S(1000), S(500)), input));
        }
    });

    [Fact]
    public void Preference_validation_selection_and_reset_are_atomic_and_per_ribbon() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; using var other = new RibbonControl();
        var initial = ribbon.Theme; var builtIn = ribbon.BuiltInTheme;
        foreach (int invalid in new[] { -1, 1025, int.MinValue, int.MaxValue })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = initial with { BackstagePreferredMinimumPageWidth = invalid });
            Assert.Same(initial, ribbon.Theme); Assert.Equal(builtIn, ribbon.BuiltInTheme);
        }
        var application = initial with { BackstagePreferredMinimumPageWidth = 360, BackstagePagePadding = 12, CaptionButtonAppearance = new() { GlyphSize = 18 } };
        ribbon.Theme = application; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; ribbon.BuiltInTheme = null; Assert.Same(application, ribbon.Theme);
        ribbon.Theme = application with { BackstagePreferredMinimumPageWidth = null };
        Assert.Equal(12, ribbon.Theme.BackstagePagePadding); Assert.Equal(18, ribbon.Theme.CaptionButtonAppearance!.GlyphSize); Assert.Null(other.Theme.BackstagePreferredMinimumPageWidth);
    });

    [Theory] [InlineData(RibbonBuiltInTheme.Office2019Light)] [InlineData(RibbonBuiltInTheme.Office2024Light)]
    public void Live_page_resizing_preserves_native_edit_state_focus_and_pending_validation(RibbonBuiltInTheme family) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { ClientSize = new(1000, 600) };
        using var ribbon = new RibbonControl { BuiltInTheme = family, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.None };
        using var field = new TextBox { Text = "Pending native page draft" }; var page = new RibbonBackstagePageItem { Text = "Info", Content = field };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); ribbon.Backstage.Items.Add(page);
        ribbon.Backstage.MinimumNavigationWidth = 80; ribbon.Backstage.MaximumNavigationWidth = 1000; ribbon.Backstage.NavigationWidth = 1000;
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true; field.Focus(); field.Select(2, 5);
        var handle = field.Handle; var parent = field.Parent; int validations = 0;
        field.Validating += (_, args) => { validations++; args.Cancel = true; };
        foreach (int? preference in new int?[] { 160, 360, 600, 1024, null })
        {
            ribbon.Theme = ribbon.Theme with { BackstagePreferredMinimumPageWidth = preference };
            Assert.True(ribbon.IsBackstageOpen); Assert.Same(page, ribbon.Backstage.SelectedPage); Assert.Same(parent, field.Parent); Assert.Equal(handle, field.Handle);
            Assert.Equal("Pending native page draft", field.Text); Assert.Equal(2, field.SelectionStart); Assert.Equal(5, field.SelectionLength); Assert.True(field.Focused); Assert.Equal(0, validations);
            var surface = ribbon.BackstageSurface!; var layout = RibbonBackstageLayout.Build(surface.ClientSize, ribbon.Font, ribbon.DeviceDpi, ribbon.Backstage, captionHeight: surface.CaptionHeight);
            Assert.Equal(layout.PageContentBounds(RibbonBackstageLayout.PageBorderWidth(ribbon.Painter, ribbon.Theme, ribbon.DeviceDpi, surface.ClientSize)), surface.PageArea.Bounds);
        }
        form.ClientSize = new(200, 140);
        Assert.Equal(1000, ribbon.Backstage.NavigationWidth); Assert.Equal(handle, field.Handle); Assert.Same(parent, field.Parent); Assert.Equal(0, validations);
    });
}
