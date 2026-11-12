using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GalleryContentTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Authored_label_artwork_and_popup_measurement_share_values_without_moving_inline_targets(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi); var capture = new Capture(); using var painter = new Office([], capture);
        using var image = new Bitmap(S(300), S(230)); using var g = Graphics.FromImage(image); using var font = new Font("Segoe UI", 12 * dpi / 96f); using var tile = new RibbonGalleryTile { Text = "Wrapped label", Icon = RibbonStockIcons.Create("save") };
        var a = new RibbonGalleryContentSettings { LabelMaxLines = 3, LabelAlignment = ContentAlignment.BottomRight, ArtworkHeightReserve = 30, ArtworkHorizontalInset = 14, ArtworkTopInset = 10 };
        var theme = RibbonThemes.Office2024Light with { GalleryContent = a }; var bounds = new Rectangle(S(4), S(4), S(280), S(220)); var c = new RibbonPaintContext(g, bounds, theme, font, dpi, Focused: true);
        painter.DrawGalleryTile(c, tile, false); Assert.Equal(bounds.Left + S(14), capture.Image.Left); Assert.Equal(bounds.Top + S(10), capture.Image.Top); Assert.True(bounds.Contains(capture.Image));
        Assert.Equal(bounds.Bottom - S(4), capture.Text.Bottom); Assert.True(capture.Flags.HasFlag(TextFormatFlags.Bottom) && capture.Flags.HasFlag(TextFormatFlags.Right)); Assert.InRange(capture.Text.Height, 1, 3 * font.Height); Assert.Equal(bounds, capture.Focus);
        int texts = capture.Texts; painter.DrawGalleryTile(c with { Theme = theme with { GalleryContent = a with { LabelMaxLines = 0 } } }, tile, false); Assert.Equal(texts, capture.Texts);
        foreach (var alignment in Enum.GetValues<ContentAlignment>()) painter.DrawGalleryTile(c with { Theme = theme with { GalleryContent = a with { LabelAlignment = alignment } } }, tile, false);
        painter.DrawGalleryTile(c with { Theme = theme with { GalleryContent = a with { ArtworkHorizontalInset = 1024, ArtworkTopInset = 1024, ArtworkHeightReserve = 1024 } } }, tile, false);
        using var ribbon = new RibbonControl { Theme = theme }; var tab = new RibbonTab(); var group = new RibbonGroup(); var gallery = new RibbonGalleryItem { IsInRibbon = true, TileHeight = 32 }; gallery.Items.Add(new RibbonGalleryTile()); group.Items.Add(gallery); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var strip = RibbonGalleryStripLayout.Build(gallery, new(S(400), S(100)), dpi, 0); var tall = RibbonGalleryLayout.Build(gallery, font, dpi, new(S(700), S(900)));
        ribbon.Theme = theme with { GalleryContent = null }; var originalStrip = RibbonGalleryStripLayout.Build(gallery, new(S(400), S(100)), dpi, 0); var original = RibbonGalleryLayout.Build(gallery, font, dpi, new(S(700), S(900)));
        Assert.Equal(originalStrip.Viewport, strip.Viewport); Assert.Equal(originalStrip.Expand, strip.Expand); Assert.Equal(originalStrip.Tiles.Select(t => t.Bounds), strip.Tiles.Select(t => t.Bounds)); Assert.True(tall.Targets[0].Bounds.Height >= original.Targets[0].Bounds.Height);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Strip_and_utility_chrome_keep_independent_shapes_alpha_state_and_actual_override_precedence(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            var capture = new Capture(); using RibbonRenderer painter = office ? new Office([], capture) : new Flat([], capture); using var image = new Bitmap(S(250), S(130)); using var g = Graphics.FromImage(image);
            var a = new RibbonGalleryAppearanceSettings { StripCornerRadius = 12, StripInset = 3, StripBorderWidth = 2, StripBorderColor = Color.Navy, StripBackground = new(Color.LightYellow), UtilityCornerRadius = 8, UtilityInset = 4, UtilityBorderWidth = 3, UtilityBorderColor = Color.DarkGreen };
            var theme = RibbonThemes.Office2024Light with { GalleryAppearance = a, GalleryContent = new() { UtilityGlyphColor = Color.Transparent }, Hover = new(Color.LightBlue), Pressed = new(Color.Magenta) };
            var bounds = new Rectangle(S(4), S(4), S(230), S(120)); var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi, true, true, true);
            g.Clear(Color.White); painter.DrawGalleryStripFrame(c); var frame = Rectangle.Inflate(bounds, -S(3), -S(3)); Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(frame.Left, frame.Top + frame.Height / 2).ToArgb()); Assert.False(capture.ChromeContext.Hot || capture.ChromeContext.Pressed || capture.ChromeContext.Focused);
            for (int y = 0; y < S(12); y++) for (int x = 0; x < S(12); x++)
            {
                bool ink = image.GetPixel(frame.Left + x, frame.Top + y).ToArgb() == Color.Navy.ToArgb();
                Assert.Equal(ink, image.GetPixel(frame.Right - 1 - x, frame.Top + y).ToArgb() == Color.Navy.ToArgb()); Assert.Equal(ink, image.GetPixel(frame.Left + x, frame.Bottom - 1 - y).ToArgb() == Color.Navy.ToArgb()); Assert.Equal(ink, image.GetPixel(frame.Right - 1 - x, frame.Bottom - 1 - y).ToArgb() == Color.Navy.ToArgb());
            }
            g.Clear(Color.White); painter.DrawGalleryStripButton(c, RibbonGalleryStripButton.Down, true); Assert.Equal(Color.DarkGreen.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + bounds.Height / 2).ToArgb()); Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2).ToArgb());
            int chrome = capture.Chrome; g.Clear(Color.White); painter.DrawGalleryStripButton(c, RibbonGalleryStripButton.Down, false); Assert.Equal(chrome, capture.Chrome); Assert.True(Ink(image, Color.DarkGreen).IsEmpty);
            g.Clear(Color.White); painter.DrawGalleryStripFrame(c with { Theme = theme with { GalleryAppearance = a with { StripBorderColor = Color.FromArgb(128, Color.Blue) } } }); var alpha = image.GetPixel(frame.Left, frame.Top + frame.Height / 2); Assert.Equal(255, alpha.B); Assert.InRange(alpha.R, 1, 254);
            g.Clear(Color.White); painter.DrawGalleryStripFrame(c with { Theme = theme with { GalleryAppearance = a with { StripBorderWidth = 0 } } }); Assert.True(Ink(image, Color.Navy).IsEmpty);
            painter.DrawGalleryStripFrame(c with { Theme = theme with { GalleryAppearance = a with { StripInset = 1024, StripBorderWidth = 32 } } });
            capture.ReplaceChrome = true; g.Clear(Color.White); painter.DrawGalleryStripFrame(c); Assert.Equal(Color.Orange.ToArgb(), image.GetPixel(bounds.Left, bounds.Top).ToArgb());
            g.Clear(Color.White); painter.DrawGalleryStripButton(c, RibbonGalleryStripButton.Down, true); Assert.Equal(Color.Orange.ToArgb(), image.GetPixel(bounds.Left, bounds.Top).ToArgb());
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Expand_bar_dimensions_ink_hide_and_chevrons_keep_their_existing_action_slot(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi); using var painter = new RibbonOffice2024Renderer(); using var image = new Bitmap(S(90), S(90)); using var g = Graphics.FromImage(image);
        var a = new RibbonGalleryContentSettings { ExpandGlyphHeight = 30, ExpandBarWidth = 18, ExpandBarInset = 3, ExpandBarStrokeWidth = 3, UtilityGlyphColor = Color.Magenta };
        var theme = RibbonThemes.Office2024Light with { GalleryContent = a, Chevrons = new() { Length = 0 } }; var bounds = new Rectangle(S(6), S(6), S(70), S(70)); var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
        g.Clear(Color.White); painter.DrawGalleryStripButton(c, RibbonGalleryStripButton.Expand, true); var ink = Ink(image, Color.Magenta); Assert.False(ink.IsEmpty); Assert.True(bounds.Contains(ink)); Assert.InRange(ink.Height, Math.Max(1, S(3) - 1), S(3) + 1); Assert.InRange(Math.Abs((ink.Left + ink.Right - 1) / 2d - (bounds.Left + (bounds.Width - 1) / 2d)), 0, 1);
        foreach (var hidden in new[] { a with { ExpandGlyphHeight = 0 }, a with { ExpandBarWidth = 0 }, a with { ExpandBarStrokeWidth = 0 }, a with { UtilityGlyphColor = Color.Transparent }, a with { ExpandBarInset = 1024 } })
        { g.Clear(Color.White); painter.DrawGalleryStripButton(c with { Theme = theme with { GalleryContent = hidden } }, RibbonGalleryStripButton.Expand, true); Assert.True(Ink(image, Color.Magenta).IsEmpty); }
        g.Clear(Color.White); painter.DrawGalleryStripButton(c with { Theme = theme with { GalleryContent = a with { ExpandBarWidth = 64, ExpandBarInset = 0, ExpandBarStrokeWidth = 32 } } }, RibbonGalleryStripButton.Expand, true);
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) Assert.True(bounds.Contains(x, y));
        g.Clear(Color.White); painter.DrawGalleryStripButton(c with { Theme = theme with { Chevrons = new() { Length = 16, Depth = 9 }, GalleryContent = a with { ExpandBarWidth = 0 } } }, RibbonGalleryStripButton.Down, true);
        Assert.Contains(Enumerable.Range(bounds.Top, bounds.Height), y => Enumerable.Range(bounds.Left, bounds.Width).Any(x => image.GetPixel(x, y).G < 255));
    });

    [Theory] [InlineData("LabelMaxLines", 32)] [InlineData("ArtworkHeightReserve", 1024)] [InlineData("ArtworkHorizontalInset", 1024)] [InlineData("ArtworkTopInset", 1024)] [InlineData("ExpandGlyphHeight", 64)] [InlineData("ExpandBarWidth", 64)] [InlineData("ExpandBarInset", 1024)]
    public void Content_dimensions_are_atomic_and_support_endpoints(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var property = typeof(RibbonGalleryContentSettings).GetProperty(name)!;
        foreach (int n in new[] { -1, maximum + 1 }) { var a = new RibbonGalleryContentSettings(); property.SetValue(a, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { GalleryContent = a }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (int? n in new int?[] { 0, maximum, null }) { var a = new RibbonGalleryContentSettings(); property.SetValue(a, n); ribbon.Theme = original with { GalleryContent = a }; Assert.Same(a, ribbon.Theme.GalleryContent); }
    });
    [Theory] [InlineData("StripCornerRadius", 64)] [InlineData("StripInset", 1024)] [InlineData("StripBorderWidth", 32)] [InlineData("UtilityCornerRadius", 64)] [InlineData("UtilityInset", 1024)] [InlineData("UtilityBorderWidth", 32)]
    public void Inline_chrome_dimensions_are_atomic_and_support_endpoints(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var property = typeof(RibbonGalleryAppearanceSettings).GetProperty(name)!;
        foreach (int n in new[] { -1, maximum + 1 }) { var a = new RibbonGalleryAppearanceSettings(); property.SetValue(a, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { GalleryAppearance = a }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (int? n in new int?[] { 0, maximum, null }) { var a = new RibbonGalleryAppearanceSettings(); property.SetValue(a, n); ribbon.Theme = original with { GalleryAppearance = a }; Assert.Same(a, ribbon.Theme.GalleryAppearance); }
    });
    [Fact]
    public void Alignment_stroke_validation_and_member_group_resets_preserve_other_roles() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; using var other = new RibbonControl(); var original = ribbon.Theme;
        foreach (double n in new[] { -.1, 32.1, double.NaN, double.NegativeInfinity, double.PositiveInfinity }) { Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { GalleryContent = new() { ExpandBarStrokeWidth = n } }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { GalleryContent = new() { LabelAlignment = (ContentAlignment)999 } }); Assert.Same(original, ribbon.Theme);
        var a = new RibbonGalleryContentSettings { LabelMaxLines = 3, ExpandBarStrokeWidth = 2.5 }; var app = original with { GalleryContent = a, GalleryAppearance = new() { TileInset = 4, StripInset = 3 } }; ribbon.Theme = app; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; ribbon.BuiltInTheme = null; Assert.Same(app, ribbon.Theme); Assert.Null(other.Theme.GalleryContent);
        ribbon.Theme = app with { GalleryContent = a with { LabelMaxLines = null } }; Assert.Equal(2.5, ribbon.Theme.GalleryContent!.ExpandBarStrokeWidth); Assert.Equal(3, a.LabelMaxLines);
        ribbon.Theme = app with { GalleryContent = null }; Assert.Equal(4, ribbon.Theme.GalleryAppearance!.TileInset); Assert.Equal(3, ribbon.Theme.GalleryAppearance.StripInset);
    });
    [Fact]
    public void Shared_disabled_raster_treatment_still_applies_to_authored_artwork_slots() => FoundationTests.Sta(() =>
    {
        using var raster = new Bitmap(80, 20); using (var rg = Graphics.FromImage(raster)) rg.Clear(Color.Red); using var tile = new RibbonGalleryTile { LargeImage = raster, Enabled = false }; using var painter = new RibbonOffice2024Renderer(); using var image = new Bitmap(250, 180); using var g = Graphics.FromImage(image); g.Clear(Color.White);
        var theme = RibbonThemes.Office2024Light with { GalleryContent = new() { LabelMaxLines = 0, ArtworkTopInset = 10, ArtworkHorizontalInset = 12 }, CommandContent = new() { DisabledImageOpacity = 0, DisabledImageGrayscaleAmount = 0 } };
        painter.DrawGalleryTile(new(g, new(4, 4, 240, 170), theme, Control.DefaultFont, 96), tile, false); Assert.True(Ink(image, Color.Red).IsEmpty); Assert.Equal(Color.Red.ToArgb(), raster.GetPixel(0, 0).ToArgb());
    });
    private static Rectangle Ink(Bitmap image, Color ink) { var bounds = Rectangle.Empty; for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() == ink.ToArgb()) bounds = bounds.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(bounds, new(x, y, 1, 1)); return bounds; }
    private sealed class Capture
    {
        internal Rectangle Text, Image, Focus; internal TextFormatFlags Flags; internal int Texts, Chrome; internal bool ReplaceChrome; internal RibbonPaintContext ChromeContext;
    }
    [Fact]
    public void Default_content_frame_utility_pixels_slots_and_layout_match_saved_results() => FoundationTests.Sta(() =>
    {
        foreach (bool classic in new[] { false, true })
        foreach (bool office in new[] { false, true })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi); var log = new List<string>(); using RibbonRenderer painter = office ? new Office(log) : new Flat(log);
            bool empty = Environment.GetEnvironmentVariable("RIBBONKIT_GALLERY_CONTENT_EMPTY") == "1";
            var theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { GalleryContent = empty ? new() : null, GalleryAppearance = empty ? new() : null };
            using var raster = new Bitmap(40, 20); using (var rg = Graphics.FromImage(raster)) rg.Clear(Color.DarkRed);
            using var rasterTile = new RibbonGalleryTile { Text = "A longer document style label wraps here", LargeImage = raster };
            using var vectorTile = new RibbonGalleryTile { Text = "Vector document style", Icon = RibbonStockIcons.Create("save") };
            using var ribbon = new RibbonControl { Theme = theme, Density = density }; var tab = new RibbonTab(); var group = new RibbonGroup(); var gallery = new RibbonGalleryItem { IsInRibbon = true, InRibbonColumnCount = 2, ColumnCount = 3 };
            gallery.Items.AddRange(Enumerable.Range(0, 7).Select(i => new RibbonGalleryTile { Text = "Style " + i, GroupName = i < 4 ? "One" : "Two" }).ToArray()); group.Items.Add(gallery); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            using var image = new Bitmap(S(760), S(520)); using var g = Graphics.FromImage(image); g.Clear(theme.Surface);
            foreach (int points in new[] { 9, 14 })
            {
                using var font = new Font("Segoe UI", points * dpi / 96f); int row = points == 9 ? 0 : 1;
                for (int column = 0; column < 3; column++)
                {
                    var tile = column == 1 ? vectorTile : rasterTile; tile.Enabled = column != 2;
                    var bounds = new Rectangle(S(6 + column * 246), S(6 + row * 154), S(238), S(146));
                    painter.DrawGalleryTile(new(g, bounds, theme, font, dpi, Hot: column == 1, Focused: column == 0) { Density = density }, tile, column == 0);
                }
                var grid = RibbonGalleryLayout.Build(gallery, font, dpi, new(S(700), S(600))); log.Add($"grid:{points}/{grid.Extent}/{grid.ViewportHeight}"); foreach (var target in grid.Targets) log.Add("target:" + target.Bounds);
            }
            using var normal = new Font("Segoe UI", 9 * dpi / 96f);
            var c = new RibbonPaintContext(g, new(S(6), S(322), S(730), S(88)), theme, normal, dpi) { Density = density };
            painter.DrawGalleryStripFrame(c);
            var strip = RibbonGalleryStripLayout.Build(gallery, c.Bounds.Size, dpi, 0); log.Add($"strip:{strip.Viewport}/{strip.Up}/{strip.Down}/{strip.Expand}"); foreach (var tile in strip.Tiles) log.Add("strip-tile:" + tile.Bounds);
            for (int state = 0; state < 4; state++)
            foreach (var button in Enum.GetValues<RibbonGalleryStripButton>())
            {
                int index = state * 3 + (int)button; int width = S(new RibbonGalleryMetrics(theme, density).UtilityWidth);
                var bounds = new Rectangle(S(6 + index * 62), S(430), width, S(72));
                painter.DrawGalleryStripButton(c with { Bounds = bounds, Hot = state == 1, Pressed = state == 2 }, button, state != 3);
            }
            using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); var png = stream.ToArray(); string geometry = string.Join(Environment.NewLine, log), name = $"gallery-content-{classic}-{office}-{dpi}-{density}";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_GALLERY_CONTENT_CAPTURE") is { Length: > 0 } capture) { Directory.CreateDirectory(capture); File.WriteAllBytes(Path.Combine(capture, name + ".png"), png); File.WriteAllText(Path.Combine(capture, name + ".txt"), geometry); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_GALLERY_CONTENT_BASELINE") is { Length: > 0 } baseline) { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), geometry); }
        }
    });
    private sealed class Office(List<string> log, Capture? capture = null) : RibbonOffice2024Renderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color ink, TextFormatFlags flags) { if (capture is not null) { capture.Text = c.Bounds; capture.Flags = flags; capture.Texts++; return; } log.Add($"text:{text}/{c.Bounds}/{flags}"); base.DrawText(c, text, ink, flags); }
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { if (capture is not null) { capture.Image = c.Bounds; return; } log.Add("image:" + c.Bounds); base.DrawItemImage(c, item, size); }
        public override void DrawFocus(RibbonPaintContext c) { if (capture is not null) { capture.Focus = c.Bounds; return; } log.Add("focus:" + c.Bounds); base.DrawFocus(c); }
        protected override void DrawChrome(RibbonPaintContext c, RibbonFill fill, Color border) { if (capture is not null) { capture.Chrome++; capture.ChromeContext = c; if (capture.ReplaceChrome) { using var brush = new SolidBrush(Color.Orange); c.Graphics.FillRectangle(brush, c.Bounds); return; } } base.DrawChrome(c, fill, border); }
    }
    private sealed class Flat(List<string> log, Capture? capture = null) : RibbonRenderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color ink, TextFormatFlags flags) { if (capture is not null) { capture.Text = c.Bounds; capture.Flags = flags; capture.Texts++; return; } log.Add($"text:{text}/{c.Bounds}/{flags}"); base.DrawText(c, text, ink, flags); }
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { if (capture is not null) { capture.Image = c.Bounds; return; } log.Add("image:" + c.Bounds); base.DrawItemImage(c, item, size); }
        public override void DrawFocus(RibbonPaintContext c) { if (capture is not null) { capture.Focus = c.Bounds; return; } log.Add("focus:" + c.Bounds); base.DrawFocus(c); }
        protected override void DrawChrome(RibbonPaintContext c, RibbonFill fill, Color border) { if (capture is not null) { capture.Chrome++; capture.ChromeContext = c; if (capture.ReplaceChrome) { using var brush = new SolidBrush(Color.Orange); c.Graphics.FillRectangle(brush, c.Bounds); return; } } base.DrawChrome(c, fill, border); }
    }
}
