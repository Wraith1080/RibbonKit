using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ApplicationMenuSurfaceTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Independent_backgrounds_frame_divider_alpha_and_safe_clips_preserve_paint_slots(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new RibbonOffice2024Renderer(); using var image = new Bitmap(S(510), S(380)); using var g = Graphics.FromImage(image);
        var nav = new Rectangle(S(5) + 1, S(5) + 1, S(150), S(320) - 2); var frame = RibbonRenderer.ApplicationFrame(new(Point.Empty, image.Size), nav);
        var pane = new Rectangle(nav.Right + 1 + S(10), nav.Top + S(32), S(320), S(270));
        int x = nav.Right;
        var settings = new RibbonApplicationMenuAppearanceSettings { SurroundBackground = new(Color.Beige), NavigationBackground = new(Color.LightBlue), DefaultPaneBackground = new(Color.LightGreen), ActivePaneBackground = new(Color.LightPink),
            InnerFrameCornerRadius = 12, InnerFrameBorderWidth = 3, InnerFrameBorderColor = Color.Navy, NavigationDividerWidth = 3, NavigationDividerInset = 8, NavigationDividerColor = Color.Magenta };
        var theme = RibbonThemes.Office2024Light with { ApplicationMenuAppearance = settings };
        var c = new RibbonPaintContext(g, new(Point.Empty, image.Size), theme, Control.DefaultFont, dpi) { ApplicationMenuDefaultPane = true, ApplicationMenuDividerX = x };
        void Draw(RibbonPaintContext value) { g.Clear(Color.White); painter.DrawApplicationMenuBackground(value, nav, pane, Rectangle.Empty); }
        Draw(c);
        Assert.Equal(Color.Beige.ToArgb(), image.GetPixel(0, image.Height / 2).ToArgb());
        Assert.Equal(Color.LightBlue.ToArgb(), image.GetPixel(nav.Left + S(20), nav.Top + S(40)).ToArgb());
        Assert.Equal(Color.LightGreen.ToArgb(), image.GetPixel(pane.Left + S(20), pane.Top + S(40)).ToArgb());
        Assert.Equal(new Rectangle(x, frame.Top + S(8), S(3), frame.Height - 2 * S(8)), Ink(image, Color.Magenta));
        Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(frame.Left, frame.Top + frame.Height / 2).ToArgb());
        var borderOnly = (Bitmap)image.Clone();
        using (borderOnly) for (int y = 0; y < S(12); y++) for (int px = 0; px < S(12); px++)
        {
            bool ink = borderOnly.GetPixel(frame.Left + px, frame.Top + y).ToArgb() == Color.Navy.ToArgb();
            Assert.Equal(ink, borderOnly.GetPixel(frame.Right - 1 - px, frame.Top + y).ToArgb() == Color.Navy.ToArgb());
            Assert.Equal(ink, borderOnly.GetPixel(frame.Left + px, frame.Bottom - 1 - y).ToArgb() == Color.Navy.ToArgb());
            Assert.Equal(ink, borderOnly.GetPixel(frame.Right - 1 - px, frame.Bottom - 1 - y).ToArgb() == Color.Navy.ToArgb());
        }
        Draw(c with { ApplicationMenuDefaultPane = false }); Assert.Equal(Color.LightPink.ToArgb(), image.GetPixel(pane.Left + S(20), pane.Top + S(40)).ToArgb());
        foreach (var a in new[] { settings with { NavigationDividerWidth = 0, InnerFrameBorderWidth = 0 }, settings with { NavigationDividerColor = Color.Transparent, InnerFrameBorderColor = Color.Transparent } })
        { Draw(c with { Theme = theme with { ApplicationMenuAppearance = a } }); Assert.True(Ink(image, Color.Magenta).IsEmpty); Assert.True(Ink(image, Color.Navy).IsEmpty); }
        Draw(c with { Theme = theme with { ApplicationMenuAppearance = settings with { DefaultPaneBackground = new(Color.FromArgb(128, Color.Red)) } } });
        var alpha = image.GetPixel(pane.Left + S(20), pane.Top + S(40)); Assert.Equal(255, alpha.A); Assert.InRange(alpha.G, 1, 254); Assert.True(alpha.R > alpha.G);
        Draw(c with { Theme = theme with { ApplicationMenuAppearance = settings with { SurroundBackground = new(Color.Transparent) } } }); Assert.Equal(255, image.GetPixel(0, 0).A);
        foreach (int n in new[] { 0, 1 })
        {
            var value = theme with { ApplicationMenuAppearance = new() { DefaultPaneTintStrength = n, NavigationDividerWidth = 0 } };
            Draw(c with { Theme = value }); Assert.Equal((n == 0 ? value.PopupColor : Color.FromArgb(255, value.Border)).ToArgb(), image.GetPixel(pane.Left + S(20), pane.Top + S(40)).ToArgb());
        }
        var clamped = settings with { InnerFrameBorderWidth = 32, InnerFrameCornerRadius = 64, NavigationDividerWidth = 32, NavigationDividerInset = 1024 };
        Draw(c with { Theme = theme with { ApplicationMenuAppearance = clamped } });
        var m = new RibbonApplicationMenuSurfaceMetrics(theme, dpi, RibbonPresentationResolver.ApplicationMenuSurface); using var clip = m.ItemClip(frame, painter.GetPopupCornerRadius(theme, dpi));
        Assert.False(clip.IsVisible(frame.Left, frame.Top)); Assert.True(clip.IsVisible(frame.Left + frame.Width / 2, frame.Top + frame.Height / 2));
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Heading_weight_text_insets_rule_alpha_and_shape_keep_actual_hooks_and_surface_isolation(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new Probe(); using var image = new Bitmap(S(300), S(80)); using var g = Graphics.FromImage(image); using var font = new Font("Segoe UI", 10, FontStyle.Bold | FontStyle.Italic);
        var bounds = new Rectangle(S(4), S(4), S(280), S(60));
        var a = new RibbonApplicationMenuAppearanceSettings { HeaderBold = false, DefaultHeaderTextInset = 6, ActiveHeaderTextInset = 14, HeaderCornerRadius = 10, HeaderRuleThickness = 3, HeaderRuleColor = Color.Magenta };
        var theme = RibbonThemes.Office2024Light with { ApplicationMenuAppearance = a, MenuHeadingBackground = new(Color.LightYellow) };
        var c = new RibbonPaintContext(g, bounds, theme, font, dpi) { ApplicationMenuPart = RibbonApplicationMenuPart.Pane };
        foreach (bool isDefault in new[] { false, true })
        {
            g.Clear(Color.White); painter.DrawMenuHeader(c with { ApplicationMenuDefaultPane = isDefault }, "Heading");
            Assert.Equal(Rectangle.Inflate(bounds, -S(isDefault ? 6 : 14), 0), painter.TextBounds); Assert.Equal(FontStyle.Italic, painter.Style); Assert.Same(theme, painter.Theme);
            Assert.Equal(new Rectangle(bounds.Left, bounds.Bottom - S(3), bounds.Width, S(3)), Ink(image, Color.Magenta));
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Left, bounds.Top).ToArgb());
        }
        foreach (var hide in new[] { a with { HeaderRuleThickness = 0 }, a with { HeaderRuleColor = Color.Transparent } })
        { g.Clear(Color.White); painter.DrawMenuHeader(c with { Theme = theme with { ApplicationMenuAppearance = hide } }, "Heading"); Assert.True(Ink(image, Color.Magenta).IsEmpty); }
        g.Clear(Color.White); painter.DrawMenuHeader(c with { Theme = theme with { ApplicationMenuAppearance = a with { HeaderRuleColor = Color.FromArgb(128, Color.Blue) } } }, "Heading"); Assert.InRange(image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Bottom - 1).R, 1, 254);
        painter.DrawMenuHeader(c with { Theme = theme with { ApplicationMenuAppearance = a with { HeaderBold = null, DefaultHeaderTextInset = 1024, ActiveHeaderTextInset = 1024, HeaderRuleThickness = 32 } } }, "Heading"); Assert.Equal(FontStyle.Bold | FontStyle.Italic, painter.Style); Assert.True(painter.TextBounds.Width > 0);
        g.Clear(Color.White); painter.DrawMenuHeader(c with { ApplicationMenuPart = RibbonApplicationMenuPart.None }, "Ordinary"); using var before = (Bitmap)image.Clone();
        g.Clear(Color.White); painter.DrawMenuHeader(c with { ApplicationMenuPart = RibbonApplicationMenuPart.None, Theme = theme with { ApplicationMenuAppearance = null } }, "Ordinary"); Same(before, image);
        Assert.True(painter.Headers > 0 && painter.GalleryHeaders > 0);
    });

    [Theory] [InlineData("InnerFrameCornerRadius", 64)] [InlineData("InnerFrameBorderWidth", 32)] [InlineData("NavigationDividerWidth", 32)] [InlineData("NavigationDividerInset", 1024)]
    [InlineData("HeaderCornerRadius", 64)] [InlineData("DefaultHeaderTextInset", 1024)] [InlineData("ActiveHeaderTextInset", 1024)] [InlineData("HeaderRuleThickness", 32)]
    public void Invalid_surface_dimensions_are_atomic_and_endpoints_are_supported(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var property = typeof(RibbonApplicationMenuAppearanceSettings).GetProperty(name)!;
        foreach (int n in new[] { -1, maximum + 1 }) { var a = new RibbonApplicationMenuAppearanceSettings(); property.SetValue(a, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { ApplicationMenuAppearance = a }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (int? n in new int?[] { 0, maximum, null }) { var a = new RibbonApplicationMenuAppearanceSettings(); property.SetValue(a, n); ribbon.Theme = original with { ApplicationMenuAppearance = a }; Assert.Same(a, ribbon.Theme.ApplicationMenuAppearance); }
    });

    [Fact]
    public void Pane_tint_validation_and_independent_resets_preserve_application_and_command_roles() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; using var other = new RibbonControl(); var original = ribbon.Theme;
        foreach (double n in new[] { -.1, 1.1, double.NaN, double.PositiveInfinity, double.NegativeInfinity }) { Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { ApplicationMenuAppearance = new() { DefaultPaneTintStrength = n } }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        var a = new RibbonApplicationMenuAppearanceSettings { DefaultPaneTintStrength = .5, DefaultPaneBackground = new(Color.Transparent), RowInset = 4, HeaderRuleColor = Color.Magenta };
        var app = original with { ApplicationMenuAppearance = a }; ribbon.Theme = app; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; ribbon.BuiltInTheme = null; Assert.Same(app, ribbon.Theme); Assert.Null(other.Theme.ApplicationMenuAppearance);
        ribbon.Theme = app with { ApplicationMenuAppearance = a with { DefaultPaneBackground = null, HeaderRuleColor = Color.Empty } }; Assert.Equal(4, ribbon.Theme.ApplicationMenuAppearance!.RowInset); Assert.Equal(.5, ribbon.Theme.ApplicationMenuAppearance.DefaultPaneTintStrength); Assert.NotNull(a.DefaultPaneBackground);
        ribbon.Theme = app with { ApplicationMenuAppearance = null }; Assert.Null(ribbon.Theme.ApplicationMenuAppearance);
    });

    private static Rectangle Ink(Bitmap image, Color ink) { var result = Rectangle.Empty; for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() == ink.ToArgb()) result = result.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(result, new(x, y, 1, 1)); return result; }
    private static void Same(Bitmap a, Bitmap b) { for (int y = 0; y < a.Height; y++) for (int x = 0; x < a.Width; x++) Assert.Equal(a.GetPixel(x, y), b.GetPixel(x, y)); }
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal Rectangle TextBounds; internal FontStyle Style; internal RibbonTheme? Theme; internal int Headers, GalleryHeaders;
        public override void DrawText(RibbonPaintContext c, string text, Color ink, TextFormatFlags flags) { TextBounds = c.Bounds; Style = c.Font.Style; Theme = c.Theme; }
        public override void DrawMenuHeader(RibbonPaintContext c, string text) { Headers++; base.DrawMenuHeader(c, text); }
        public override void DrawGalleryHeading(RibbonPaintContext c, string text) { GalleryHeaders++; base.DrawGalleryHeading(c, text); }
    }

    [Fact]
    public void Default_background_frame_and_headers_match_saved_pixels() => FoundationTests.Sta(() =>
    {
        foreach (bool classic in new[] { false, true })
        foreach (bool office in new[] { false, true })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int fontSize in new[] { 9, 14 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            var theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { ApplicationMenuAppearance = Environment.GetEnvironmentVariable("RIBBONKIT_APPLICATION_SURFACE_EMPTY") == "1" ? new() : null };
            using var font = new Font("Segoe UI", fontSize * dpi / 96f);
            using var image = new Bitmap(S(1040), S(390)); using var g = Graphics.FromImage(image); g.Clear(Color.White);
            for (int state = 0; state < 2; state++)
            {
                using var part = new Bitmap(S(510), S(380)); using var pg = Graphics.FromImage(part); pg.Clear(Color.White);
                var bounds = new Rectangle(Point.Empty, part.Size);
                var nav = new Rectangle(bounds.Left + S(5) + 1, bounds.Top + S(5) + 1, S(150), S(320) - 2);
                var pane = new Rectangle(nav.Right + 1 + S(10), nav.Top + S(32), S(320), S(270));
                var footer = new Rectangle(bounds.Left + S(5), bounds.Top + S(338), S(500), S(30));
                var c = new RibbonPaintContext(pg, bounds, theme, font, dpi) { ApplicationMenuDefaultPane = state == 0 };
                painter.DrawApplicationMenuBackground(c, nav, pane, footer);
                var header = state == 0 ? new Rectangle(pane.Left, nav.Top, pane.Width, S(28)) : new Rectangle(nav.Right + 1, nav.Top, bounds.Right - S(6) - nav.Right - 1, S(28));
                painter.DrawMenuHeader(c with { Bounds = header, ApplicationMenuPart = RibbonApplicationMenuPart.Pane }, state == 0 ? "Recent documents" : "Preview and print");
                g.DrawImageUnscaled(part, S(5 + state * 520), S(5));
            }
            using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); var png = stream.ToArray();
            string name = $"surface-{classic}-{office}-{dpi}-{fontSize}.png";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_APPLICATION_SURFACE_CAPTURE") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); File.WriteAllBytes(Path.Combine(capture, name), png); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_APPLICATION_SURFACE_BASELINE") is { Length: > 0 } baseline)
                Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name)), png);
        }
    });
}
