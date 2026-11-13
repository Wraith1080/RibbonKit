using Xunit;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms.Tests;

public sealed class CaptionButtonAppearanceTests
{
    [Fact]
    public void Native_caption_button_defaults_can_be_compared_across_extraction() => FoundationTests.Sta(() =>
    {
        string? output = Environment.GetEnvironmentVariable("RIBBONKIT_D8B_CAPTURE");
        if (!string.IsNullOrEmpty(output)) Directory.CreateDirectory(output);
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (bool material in new[] { false, true })
        foreach (bool restore in new[] { false, true })
        {
            using var form = new StateForm { Text = "Caption button baseline", ClientSize = new(900, 500), ShowIcon = false,
                WindowState = restore ? FormWindowState.Maximized : FormWindowState.Normal };
            using var ribbon = new RibbonControl { BuiltInTheme = family };
            form.Controls.Add(ribbon); form.Ribbon = ribbon; ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); _ = form.Handle;
            foreach (int hit in new[] { 8, 9, 20 })
            foreach (int state in new[] { 0, 1, 2 })
            {
                form.State(state == 0 ? 0 : hit, state == 2);
                using var buffer = new RibbonBackdropBuffer(); using var image = new Bitmap(form.ClientSize.Width, form.CaptionHeight);
                using var g = Graphics.FromImage(image);
                buffer.Paint(g, image.Size, image.Height, Color.Transparent, canvas => form.PaintCaption(canvas, ribbon, material), copyToDestination: false);
                if (!string.IsNullOrEmpty(output)) buffer.Image.Save(Path.Combine(output, $"{family}-{material}-{restore}-{hit}-{state}.png"));
                ribbon.Theme = ribbon.Theme with { CaptionButtonAppearance = new() };
                using var reset = new RibbonBackdropBuffer();
                reset.Paint(g, image.Size, image.Height, Color.Transparent, canvas => form.PaintCaption(canvas, ribbon, material), copyToDestination: false);
                Assert.Equal(Pixels(buffer.Image), Pixels(reset.Image));
            }
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Authored_state_shape_alpha_and_glyph_geometry_fit_unchanged_targets(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new RoundedPainter(); using var image = new Bitmap(S(110), S(90)); using var g = Graphics.FromImage(image);
        var target = new Rectangle(S(8), S(8), S(80), S(64));
        var a = new RibbonCaptionButtonAppearanceSettings { StateInset = 3, CornerRadius = 9, HoverBackground = new(Color.FromArgb(128, Color.Blue)), GlyphSize = 0 };
        var theme = RibbonThemes.Office2019Light with { CaptionButtonAppearance = a };
        var c = new RibbonPaintContext(g, target, theme, Control.DefaultFont, dpi, Hot: true) { CompositedText = true };
        void Draw(RibbonCaptionButtonAppearanceSettings value, int hit = 8, bool restore = false)
        {
            g.Clear(Color.Transparent); RibbonCaptionButtonRenderer.Draw(painter, c with { Theme = theme with { CaptionButtonAppearance = value } }, hit, restore, true, bounds => { using var clear = new SolidBrush(Color.Transparent); g.FillRectangle(clear, bounds); });
        }
        Draw(a); var paint = new RibbonCaptionButtonMetrics(theme, dpi).StateBounds(target);
        Assert.Equal(128, image.GetPixel(paint.Left + paint.Width / 2, paint.Top + paint.Height / 2).A);
        for (int y = 0; y < S(9); y++) for (int x = 0; x < S(9); x++)
        {
            var pixel = image.GetPixel(paint.Left + x, paint.Top + y);
            Assert.Equal(pixel, image.GetPixel(paint.Right - 1 - x, paint.Top + y));
            Assert.Equal(pixel, image.GetPixel(paint.Left + x, paint.Bottom - 1 - y));
            Assert.Equal(pixel, image.GetPixel(paint.Right - 1 - x, paint.Bottom - 1 - y));
        }
        Assert.Equal(0, image.GetPixel(target.Left, target.Top).A);
        Draw(a with { HoverBackground = new(Color.Transparent) }); Assert.All(Pixels(image), pixel => Assert.Equal(0, pixel.A));
        var glyphOnly = a with { HoverBackground = new(Color.Transparent), GlyphSize = 24, GlyphStrokeWidth = 2, GlyphColor = Color.FromArgb(115, Color.Red) };
        Draw(glyphOnly); Assert.Contains(Pixels(image), pixel => pixel.A == 115 && pixel.R > 245 && pixel.G < 10);
        Draw(glyphOnly with { GlyphStrokeWidth = 0 }); Assert.All(Pixels(image), pixel => Assert.Equal(0, pixel.A));
        var metrics = new RibbonCaptionButtonMetrics(theme with { CaptionButtonAppearance = glyphOnly }, dpi);
        Assert.Equal(S(24), metrics.GlyphBounds(target).Width);
        Assert.Equal(3, metrics.RestoreOffset(S(24)));
        Assert.Equal(S(3), new RibbonCaptionButtonMetrics(theme with { CaptionButtonAppearance = glyphOnly with { RestoreOffset = 3 } }, dpi).RestoreOffset(S(24)));
        var tiny = new Rectangle(S(4), S(4), 7, 5);
        g.Clear(Color.Transparent);
        RibbonCaptionButtonRenderer.Draw(painter, c with { Bounds = tiny, Theme = theme with { CaptionButtonAppearance = glyphOnly with { GlyphSize = 256, GlyphStrokeWidth = 32, RestoreOffset = 256 } } }, 9, true, true, _ => { });
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (!tiny.Contains(x, y)) Assert.Equal(0, image.GetPixel(x, y).A);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Restore_cutout_repaints_one_gradient_coverage_and_close_state_ink_is_independent(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new RoundedPainter(); using var image = new Bitmap(S(100), S(70)); using var g = Graphics.FromImage(image);
        var a = new RibbonCaptionButtonAppearanceSettings { StateInset = 2, CornerRadius = 8, HoverBackground = new(Color.FromArgb(90, Color.Blue), Color.FromArgb(160, Color.Lime)),
            GlyphSize = 24, GlyphStrokeWidth = 1, RestoreOffset = 6, GlyphColor = Color.Red, CloseHotGlyphColor = Color.Purple, CloseHoverBackground = new(Color.Transparent) };
        var theme = RibbonThemes.Office2024Light with { CaptionButtonAppearance = a };
        var c = new RibbonPaintContext(g, new(S(5), S(5), S(80), S(56)), theme, Control.DefaultFont, dpi, Hot: true) { CompositedText = true };
        void Draw(int hit, bool restore, RibbonCaptionButtonAppearanceSettings settings)
        {
            g.Clear(Color.Transparent); RibbonCaptionButtonRenderer.Draw(painter, c with { Theme = theme with { CaptionButtonAppearance = settings } }, hit, restore, true,
                bounds => { using var clear = new SolidBrush(Color.Transparent); g.FillRectangle(clear, bounds); });
        }
        Draw(9, true, a); var metrics = new RibbonCaptionButtonMetrics(theme, dpi); var glyph = metrics.GlyphBounds(c.Bounds);
        var point = new Point(glyph.Left + S(3), glyph.Top + S(6) + S(4)); var cutout = image.GetPixel(point.X, point.Y);
        Draw(9, false, a with { GlyphSize = 0 }); Assert.Equal(image.GetPixel(point.X, point.Y), cutout);
        Draw(20, false, a); Assert.Contains(Pixels(image), pixel => pixel.A == 255 && pixel.R == Color.Purple.R && pixel.B == Color.Purple.B);
        Draw(20, false, a with { CloseHotGlyphColor = Color.Transparent }); Assert.All(Pixels(image), pixel => Assert.Equal(0, pixel.A));
    });

    [Theory] [InlineData("StateInset", 1024)] [InlineData("CornerRadius", 64)] [InlineData("GlyphSize", 256)] [InlineData("RestoreOffset", 256)] [InlineData("GlyphStrokeWidth", 32)]
    public void Invalid_values_leave_theme_and_builtin_selection_unchanged(string member, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var theme = ribbon.Theme; var selected = ribbon.BuiltInTheme;
        var property = typeof(RibbonCaptionButtonAppearanceSettings).GetProperty(member)!;
        foreach (double number in member == "GlyphStrokeWidth" ? new[] { -1d, maximum + 1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity } : new[] { -1d, maximum + 1d })
        {
            var a = new RibbonCaptionButtonAppearanceSettings(); property.SetValue(a, member == "GlyphStrokeWidth" ? (object)number : (int)number);
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = theme with { CaptionButtonAppearance = a });
            Assert.Same(theme, ribbon.Theme); Assert.Equal(selected, ribbon.BuiltInTheme);
        }
    });

    [Fact]
    public void Native_state_paint_and_independent_resets_retain_targets_and_title_theme() => FoundationTests.Sta(() =>
    {
        using var form = new StateForm { ClientSize = new(900, 500), Text = "Caption controls" };
        using var ribbon = new RibbonControl(); using var other = new RibbonControl();
        form.Controls.Add(ribbon); form.Ribbon = ribbon; ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); _ = form.Handle;
        var original = form.Caption.Targets.ToArray(); var title = form.CaptionTitleDrawingBounds(ribbon);
        var a = new RibbonCaptionButtonAppearanceSettings { HoverBackground = new(Color.Magenta), ClosePressedBackground = new(Color.FromArgb(80, Color.Blue)), GlyphSize = 0, StateInset = 4 };
        var theme = ribbon.Theme with { CaptionButtonAppearance = a, BackstageAppearance = new() { CaptionTintOpacity = .4 }, TitleBarForeground = Color.Teal }; ribbon.Theme = theme;
        var native = form.Handle; form.State(8, false);
        using var buffer = new RibbonBackdropBuffer(); using var image = new Bitmap(form.ClientSize.Width, form.CaptionHeight); using var g = Graphics.FromImage(image);
        buffer.Paint(g, image.Size, image.Height, Color.Transparent, canvas => form.PaintCaption(canvas, ribbon, true), copyToDestination: false);
        var target = form.Caption.Targets.Single(t => t.Hit == 8).Bounds;
        Assert.Equal(Color.Magenta.ToArgb(), buffer.Image.GetPixel(target.Left + 8, 8).ToArgb());
        Assert.Equal(original, form.Caption.Targets); Assert.Equal(title, form.CaptionTitleDrawingBounds(ribbon)); Assert.Equal(native, form.Handle);
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = null; Assert.Same(theme, ribbon.Theme);
        ribbon.Theme = theme with { CaptionButtonAppearance = a with { HoverBackground = null, ClosePressedBackground = null } }; Assert.Equal(4, ribbon.Theme.CaptionButtonAppearance!.StateInset);
        ribbon.Theme = theme with { CaptionButtonAppearance = null }; Assert.Equal(.4, ribbon.Theme.BackstageAppearance!.CaptionTintOpacity); Assert.Equal(Color.Teal, ribbon.Theme.TitleBarForeground); Assert.Null(other.Theme.CaptionButtonAppearance);
    });

    private static IEnumerable<Color> Pixels(Bitmap image)
    {
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) yield return image.GetPixel(x, y);
    }
    private sealed class StateForm : RibbonForm
    {
        internal void State(int hit, bool pressed)
        {
            Capture = false;
            var cancel = Message.Create(Handle, 0x1F, 0, 0); WndProc(ref cancel);
            var hover = Message.Create(Handle, 0xA0, hit, 0); WndProc(ref hover);
            if (pressed) { var down = Message.Create(Handle, 0xA1, hit, 0); WndProc(ref down); }
        }
    }
}
