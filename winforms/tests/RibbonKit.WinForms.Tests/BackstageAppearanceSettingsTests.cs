using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageAppearanceSettingsTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Marker_geometry_ink_visibility_and_short_row_clips_are_independent(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool classic in new[] { false, true })
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            var settings = new RibbonBackstageAppearanceSettings { MarkerLeftInset = 11, MarkerWidth = 6, MarkerTopInset = 7, MarkerBottomInset = 9, MarkerColor = Color.Magenta };
            var theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { BackstageAppearance = settings,
                BackstageSelectionIndicatorVisible = true, NavigationMarkerCornerRadius = 0, BackstageNavigationSelected = new(Color.Transparent) };
            using var image = new Bitmap(S(250), S(80)); using var graphics = Graphics.FromImage(image); using var item = new RibbonButtonItem();
            var bounds = new Rectangle(S(5), S(4), S(220), S(52));
            var context = new RibbonPaintContext(graphics, bounds, theme, Control.DefaultFont, dpi);
            painter.DrawBackstageEntry(context, item, true);
            var expected = new Rectangle(bounds.Left + S(11), bounds.Top + S(7), S(6), bounds.Height - S(16));
            Assert.Equal(expected, InkBounds(image, Color.Magenta));
            painter.DrawBackstageEntry(context with { Theme = theme with { BackstageSelectionIndicatorVisible = false } }, item, true);
            Assert.True(InkBounds(image, Color.Magenta).IsEmpty);
            painter.DrawBackstageEntry(context with { Theme = theme with { BackstageAppearance = settings with { MarkerWidth = 0 } } }, item, true);
            Assert.True(InkBounds(image, Color.Magenta).IsEmpty);
            var alpha = Color.FromArgb(128, 255, 0, 255);
            painter.DrawBackstageEntry(context with { Theme = theme with { BackstageAppearance = settings with { MarkerColor = alpha } } }, item, true);
            Assert.Equal(Blend(theme.BackstageNavigationColor, alpha), image.GetPixel(expected.Left, expected.Top));
            graphics.Clear(Color.Beige);
            var tiny = new Rectangle(3, 4, 7, 5);
            painter.DrawBackstageEntry(context with { Bounds = tiny, Theme = theme with { BackstageAppearance = settings with { MarkerLeftInset = 5, MarkerTopInset = 1, MarkerBottomInset = 0, MarkerWidth = 1024 } } }, item, true);
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
                if (!tiny.Contains(x, y)) Assert.Equal(Color.Beige.ToArgb(), image.GetPixel(x, y).ToArgb());
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Navigation_shape_border_and_alpha_follow_authored_values_in_all_states(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool classic in new[] { false, true })
        foreach (bool office in new[] { false, true })
        foreach (bool fullWidth in new[] { false, true })
        foreach (bool material in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            var border = Color.FromArgb(160, 200, 20, 120);
            var settings = new RibbonBackstageAppearanceSettings { NavigationCornerRadius = 0, NavigationBorderWidth = 3, NavigationBorderColor = border };
            var theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { BackstageAppearance = settings, BackstageFullWidthNavigation = fullWidth,
                BackstageSelectionIndicatorVisible = false, BackstageNavigationHover = new(Color.LightBlue), BackstageNavigationSelected = new(Color.LightGreen) };
            using var image = new Bitmap(S(250), S(70)); using var graphics = Graphics.FromImage(image); using var item = new RibbonButtonItem();
            var bounds = new Rectangle(S(5), S(4), S(220), S(52)); int inset = fullWidth ? 0 : 1;
            for (int state = 0; state < 3; state++)
            {
                var context = new RibbonPaintContext(graphics, bounds, theme, Control.DefaultFont, dpi, Hot: state == 0, Pressed: state == 2) { WindowBackdrop = material, CompositedText = material };
                painter.DrawBackstageEntry(context, item, state == 1);
                Assert.Equal(Blend(theme.BackstageNavigationColor, border), image.GetPixel(bounds.Left + inset, bounds.Top + bounds.Height / 2));
                Assert.Equal((state == 0 ? Color.LightBlue : Color.LightGreen).ToArgb(), image.GetPixel(bounds.Left + inset + S(3), bounds.Top + bounds.Height / 2).ToArgb());
                painter.DrawBackstageEntry(context with { Theme = theme with { BackstageAppearance = settings with { NavigationCornerRadius = 12 } } }, item, state == 1);
                Assert.Equal(theme.BackstageNavigationColor.ToArgb(), image.GetPixel(bounds.Left + inset, bounds.Top + inset).ToArgb());
                painter.DrawBackstageEntry(context with { Theme = theme with { BackstageAppearance = settings with { NavigationBorderWidth = 0 } } }, item, state == 1);
                Assert.Equal((state == 0 ? Color.LightBlue : Color.LightGreen).ToArgb(), image.GetPixel(bounds.Left + inset, bounds.Top + bounds.Height / 2).ToArgb());
            }
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Back_circle_and_arrow_can_change_independently_without_changing_the_target(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new RibbonOffice2024Renderer();
        var settings = new RibbonBackstageAppearanceSettings { BackVisualInset = 7, BackOutlineWidth = 0, BackGlyphScale = 0 };
        var theme = RibbonThemes.Office2024Light with { BackstageAppearance = settings, BackstageNavigationForeground = Color.DarkRed, Hover = new(Color.Orange) };
        using var image = new Bitmap(S(80), S(80)); using var graphics = Graphics.FromImage(image);
        var bounds = new Rectangle(S(8), S(8), S(44), S(44));
        var context = new RibbonPaintContext(graphics, bounds, theme, Control.DefaultFont, dpi, Hot: true);
        painter.DrawBackstageEntry(context, null, false);
        Assert.Equal(Color.Orange.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2).ToArgb());
        Assert.Equal(theme.BackstageNavigationColor.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + S(3)).ToArgb());
        Assert.True(InkBounds(image, Color.DarkRed).IsEmpty);
        painter.DrawBackstageEntry(context with { Theme = theme with { BackstageAppearance = settings with { BackOutlineWidth = 3 } } }, null, false);
        Assert.False(InkBounds(image, Color.DarkRed).IsEmpty);
        var glyph = context with { Hot = false, Theme = theme with { BackstageAppearance = settings with { BackGlyphScale = 1, BackGlyphStrokeWidth = 2 } } };
        painter.DrawBackstageEntry(glyph, null, false); var normal = DifferentBounds(image, theme.BackstageNavigationColor, bounds);
        Assert.False(normal.IsEmpty); Assert.True(bounds.Contains(normal));
        painter.DrawBackstageEntry(glyph with { Theme = theme with { BackstageAppearance = settings with { BackGlyphScale = 2, BackGlyphStrokeWidth = 2 } } }, null, false);
        var larger = DifferentBounds(image, theme.BackstageNavigationColor, bounds); Assert.True(larger.Width > normal.Width && larger.Height > normal.Height);
        painter.DrawBackstageEntry(glyph with { Theme = theme with { BackstageAppearance = settings with { BackGlyphScale = 2, BackGlyphStrokeWidth = 0 } } }, null, false);
        Assert.True(DifferentBounds(image, theme.BackstageNavigationColor, bounds).IsEmpty);
        using var ribbon = new RibbonControl { Theme = theme }; var before = RibbonBackstageLayout.Build(new(900, 600), ribbon.Font, dpi, ribbon.Backstage);
        ribbon.Theme = theme with { BackstageAppearance = settings with { BackVisualInset = 1024, BackGlyphScale = 8, BackOutlineWidth = 32 } };
        Assert.Equal(before, RibbonBackstageLayout.Build(new(900, 600), ribbon.Font, dpi, ribbon.Backstage));
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Spacing_uses_shared_row_footer_auto_width_and_text_paint_metrics(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (var style in Enum.GetValues<RibbonBackstageStyle>())
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            var original = RibbonThemes.Office2024Light with { BackstageStyle = style, BackstageAutoMinimumNavigationWidth = 1 };
            using var painter = new HookRenderer(); using var ribbon = new RibbonControl { Theme = original, Density = density, Renderer = painter };
            ribbon.Backstage.MinimumNavigationWidth = 80; ribbon.Backstage.MaximumNavigationWidth = 900;
            var page = new RibbonBackstagePageItem { Text = new string('W', 25) }; ribbon.Backstage.Items.AddRange(page, new RibbonMenuSeparatorItem());
            ribbon.Backstage.FooterItems.AddRange(new RibbonMenuSeparatorItem(), new RibbonButtonItem { Text = "Options" });
            using var font = new Font("Segoe UI", 9 * dpi / 96f);
            var before = RibbonBackstageLayout.Build(new(S(1500), S(900)), font, dpi, ribbon.Backstage);
            ribbon.Theme = original with { BackstageRowTextVerticalPadding = 50, BackstageSeparatorHeight = 17, BackstageTrailingPadding = 31 };
            var layout = RibbonBackstageLayout.Build(new(S(1500), S(900)), font, dpi, ribbon.Backstage);
            Assert.Equal(Math.Max(S(density == RibbonDensity.Touch ? 52 : 42), font.Height + S(50)), layout.RowHeight);
            Assert.Equal(S(17), layout.SeparatorHeight); Assert.True(layout.Page.Left > before.Page.Left);
            Assert.Equal(layout.RowHeight + S(17) + (style == RibbonBackstageStyle.Classic ? S(14) : layout.Inset), layout.Footer.Height);
            var shortLayout = RibbonBackstageLayout.Build(new(S(1500), layout.Navigation.Top + layout.RowHeight), font, dpi, ribbon.Backstage); Assert.True(shortLayout.FooterInline);
            using var image = new Bitmap(S(400), S(90)); using var graphics = Graphics.FromImage(image);
            var bounds = new Rectangle(0, 0, S(390), S(80));
            painter.DrawBackstageEntry(new(graphics, bounds, ribbon.Theme, font, dpi), page, false);
            Assert.Equal(bounds.Right - S(31), painter.TextBounds.Right);
            ribbon.Theme = ribbon.Theme with { BackstageSeparatorHeight = 0 }; Assert.Equal(0, RibbonBackstageLayout.Build(new(S(1500), S(900)), font, dpi, ribbon.Backstage).SeparatorHeight);
            ribbon.Theme = original; Assert.Equal(before, RibbonBackstageLayout.Build(new(S(1500), S(900)), font, dpi, ribbon.Backstage));
        }
    });

    [Fact]
    public void Authored_chrome_keeps_public_renderer_text_focus_and_reset_precedence() => FoundationTests.Sta(() =>
    {
        using var painter = new HookRenderer(); using var ribbon = new RibbonControl(); using var other = new RibbonControl();
        var settings = new RibbonBackstageAppearanceSettings { MarkerColor = Color.Purple, NavigationCornerRadius = 8, NavigationBorderColor = Color.Red, NavigationBorderWidth = 3 };
        var application = ribbon.Theme with { BackstageAppearance = settings, BackstageTrailingPadding = 25 }; ribbon.Theme = application;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = null;
        Assert.Same(application, ribbon.Theme); Assert.Null(other.Theme.BackstageAppearance);
        using var image = new Bitmap(240, 60); using var graphics = Graphics.FromImage(image); using var item = new RibbonButtonItem { Text = "Info" };
        painter.OverrideChrome = true;
        painter.DrawBackstageEntry(new(graphics, new(0, 0, 240, 52), ribbon.Theme, ribbon.Font, 120, Hot: true, Focused: true), item, false);
        Assert.Equal(1, painter.ChromeCalls); Assert.Equal(1, painter.FocusCalls); Assert.True(painter.TextBounds.Width > 0);
        Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(200, 26).ToArgb());
        ribbon.Theme = application with { BackstageAppearance = settings with { NavigationCornerRadius = null } };
        Assert.Null(ribbon.Theme.BackstageAppearance!.NavigationCornerRadius); Assert.Equal(Color.Red, ribbon.Theme.BackstageAppearance.NavigationBorderColor); Assert.Equal(8, settings.NavigationCornerRadius);
        ribbon.Theme = application with { BackstageAppearance = null }; Assert.Equal(25, ribbon.Theme.BackstageTrailingPadding);
    });

    [Theory]
    [InlineData("MarkerLeftInset")] [InlineData("MarkerWidth")] [InlineData("MarkerTopInset")] [InlineData("MarkerBottomInset")]
    [InlineData("BackVisualInset")] [InlineData("NavigationCornerRadius")] [InlineData("NavigationBorderWidth")]
    [InlineData("BackOutlineWidth")] [InlineData("BackGlyphScale")] [InlineData("BackGlyphStrokeWidth")]
    [InlineData("BackstageRowTextVerticalPadding")] [InlineData("BackstageSeparatorHeight")] [InlineData("BackstageTrailingPadding")]
    public void Invalid_values_do_not_mutate_application_theme_or_builtin_selection(string member) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme;
        bool direct = member.StartsWith("Backstage", StringComparison.Ordinal);
        var property = (direct ? typeof(RibbonTheme) : typeof(RibbonBackstageAppearanceSettings)).GetProperty(member)!;
        int maximum = member == "NavigationCornerRadius" ? 64 : member == "NavigationBorderWidth" || member == "BackOutlineWidth" || member == "BackGlyphStrokeWidth" ? 32 : member == "BackGlyphScale" ? 8 : 1024;
        object[] invalid = property.PropertyType == typeof(double?) ? [double.NaN, double.PositiveInfinity, double.NegativeInfinity, -.01, maximum + .01] : [-1, maximum + 1];
        foreach (var value in invalid)
        {
            var settings = new RibbonBackstageAppearanceSettings(); var candidate = original with { BackstageAppearance = settings };
            property.SetValue(direct ? candidate : settings, value);
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = candidate);
            Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme);
        }
    });

    private static Color Blend(Color background, Color ink)
    {
        using var image = new Bitmap(1, 1); using var graphics = Graphics.FromImage(image); graphics.Clear(background);
        using var brush = new SolidBrush(ink); graphics.FillRectangle(brush, 0, 0, 1, 1); return image.GetPixel(0, 0);
    }
    private static Rectangle InkBounds(Bitmap image, Color color) => PixelBounds(image, new(Point.Empty, image.Size), value => value.ToArgb() == color.ToArgb());
    private static Rectangle DifferentBounds(Bitmap image, Color color, Rectangle bounds) => PixelBounds(image, bounds, value => value.ToArgb() != color.ToArgb());
    private static Rectangle PixelBounds(Bitmap image, Rectangle bounds, Func<Color, bool> match)
    {
        int left = bounds.Right, top = bounds.Bottom, right = bounds.Left - 1, bottom = bounds.Top - 1;
        for (int y = bounds.Top; y < bounds.Bottom; y++) for (int x = bounds.Left; x < bounds.Right; x++)
            if (match(image.GetPixel(x, y))) { left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y); }
        return right < left ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }
    private sealed class HookRenderer : RibbonOffice2024Renderer
    {
        internal bool OverrideChrome;
        internal int ChromeCalls, FocusCalls;
        internal Rectangle TextBounds;
        protected override void DrawChrome(RibbonPaintContext context, RibbonFill fill, Color border)
        {
            ChromeCalls++;
            if (OverrideChrome) { using var brush = new SolidBrush(Color.Lime); context.Graphics.FillRectangle(brush, context.Bounds); }
            else base.DrawChrome(context, fill, border);
        }
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) => TextBounds = context.Bounds;
        public override void DrawFocus(RibbonPaintContext context) => FocusCalls++;
    }

    [Fact]
    public void Default_backstage_decoration_and_spacing_match_saved_frames() => FoundationTests.Sta(() =>
    {
        foreach (bool classic in new[] { false, true })
        foreach (bool office in new[] { false, true })
        foreach (bool fullWidth in new[] { false, true })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { BackstageFullWidthNavigation = fullWidth };
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var ribbon = new RibbonControl { Theme = theme, Renderer = painter };
            ribbon.Backstage.Items.AddRange(new RibbonBackstagePageItem { Text = "Info" }, new RibbonMenuSeparatorItem(), new RibbonBackstagePageItem { Text = "Print" });
            ribbon.Backstage.FooterItems.Add(new RibbonButtonItem { Text = "Options" });
            using var font = new Font("Segoe UI", 9 * dpi / 96f);
            using var image = new Bitmap(S(280), S(390)); using var graphics = Graphics.FromImage(image); graphics.Clear(Color.White);
            using var item = new RibbonButtonItem { Text = "Entry" };
            for (int state = 0; state < 6; state++)
            {
                var context = new RibbonPaintContext(graphics, new(S(4), S(state * 60 + 4), S(250), S(52)), theme, font, dpi,
                    Hot: state == 1 || state == 4, Pressed: state == 3 || state == 5);
                painter.DrawBackstageEntry(context, state >= 4 ? null : item, state == 2);
            }
            var layouts = new List<string>();
            foreach (var density in Enum.GetValues<RibbonDensity>())
            {
                ribbon.Density = density;
                var layout = RibbonBackstageLayout.Build(new(S(1000), S(650)), font, dpi, ribbon.Backstage);
                layouts.Add(density + ":" + layout + ";" + string.Join(";", layout.Arrange(ribbon.Backstage).Select(entry => entry.Bounds)));
            }
            string geometry = string.Join(Environment.NewLine, layouts);
            string name = $"appearance-{classic}-{office}-{fullWidth}-{dpi}";
            using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); byte[] png = stream.ToArray();
            if (Environment.GetEnvironmentVariable("RIBBONKIT_BACKSTAGE_APPEARANCE_CAPTURE") is { Length: > 0 } output)
            { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), png); File.WriteAllText(Path.Combine(output, name + ".txt"), geometry); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_BACKSTAGE_APPEARANCE_BASELINE") is { Length: > 0 } baseline)
            { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), geometry); }
            var empty = theme with { BackstageAppearance = new() };
            using var emptyImage = new Bitmap(image.Width, image.Height); using var emptyGraphics = Graphics.FromImage(emptyImage); emptyGraphics.Clear(Color.White);
            for (int state = 0; state < 6; state++)
                painter.DrawBackstageEntry(new(emptyGraphics, new(S(4), S(state * 60 + 4), S(250), S(52)), empty, font, dpi,
                    Hot: state == 1 || state == 4, Pressed: state == 3 || state == 5), state >= 4 ? null : item, state == 2);
            using var emptyStream = new MemoryStream(); emptyImage.Save(emptyStream, ImageFormat.Png); Assert.Equal(png, emptyStream.ToArray());
        }
    });
}
