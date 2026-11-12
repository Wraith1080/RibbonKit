using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GalleryAppearanceTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Authored_tile_chrome_keeps_state_precedence_four_corners_and_content_slots(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            var capture = new Capture(); using RibbonRenderer painter = office ? new Office([], capture) : new Flat([], capture);
            using var tile = new RibbonGalleryTile { Text = "Tile", Icon = RibbonStockIcons.Create("save") }; using var image = new Bitmap(S(250), S(150)); using var g = Graphics.FromImage(image);
            var a = new RibbonGalleryAppearanceSettings { TileCornerRadius = 12, TileInset = 3, TileBorderWidth = 2, TileBorderColor = Color.Navy };
            var theme = RibbonThemes.Office2024Light with { GalleryAppearance = a, Hover = new(Color.LightBlue), Pressed = new(Color.Magenta), Checked = new(Color.LightGreen) };
            var bounds = new Rectangle(S(4), S(4), S(230), S(140)); var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
            painter.DrawGalleryTile(c with { Theme = theme with { GalleryAppearance = null } }, tile, true); var text = capture.Text; var art = capture.Artwork;
            g.Clear(Color.White); painter.DrawGalleryTile(c, tile, true); Assert.Equal(text, capture.Text); Assert.Equal(art, capture.Artwork);
            var frame = Rectangle.Inflate(bounds, -S(3), -S(3)); Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(frame.Left, frame.Top + frame.Height / 2).ToArgb());
            for (int y = 0; y < S(12); y++) for (int x = 0; x < S(12); x++)
            {
                bool ink = image.GetPixel(frame.Left + x, frame.Top + y).ToArgb() == Color.Navy.ToArgb();
                Assert.Equal(ink, image.GetPixel(frame.Right - 1 - x, frame.Top + y).ToArgb() == Color.Navy.ToArgb());
                Assert.Equal(ink, image.GetPixel(frame.Left + x, frame.Bottom - 1 - y).ToArgb() == Color.Navy.ToArgb());
                Assert.Equal(ink, image.GetPixel(frame.Right - 1 - x, frame.Bottom - 1 - y).ToArgb() == Color.Navy.ToArgb());
            }
            foreach (var (hot, pressed, ink) in new[] { (false, false, Color.LightGreen), (true, false, Color.LightBlue), (true, true, Color.Magenta) })
            { g.Clear(Color.White); painter.DrawGalleryTile(c with { Hot = hot, Pressed = pressed }, tile, true); Assert.Equal(ink.ToArgb(), image.GetPixel(frame.Left + frame.Width / 2, frame.Top + frame.Height / 2).ToArgb()); }
            int chrome = capture.Chrome; g.Clear(Color.White); painter.DrawGalleryTile(c, tile, false); Assert.Equal(chrome, capture.Chrome); Assert.True(Ink(image, Color.Navy).IsEmpty);
            tile.Enabled = false; g.Clear(Color.White); painter.DrawGalleryTile(c with { Hot = true, Pressed = true, Focused = true }, tile, true); Assert.Equal(Color.LightGreen.ToArgb(), image.GetPixel(frame.Left + frame.Width / 2, frame.Top + frame.Height / 2).ToArgb()); Assert.Equal(Rectangle.Empty, capture.Focus); tile.Enabled = true;
            g.Clear(Color.White); painter.DrawGalleryTile(c with { Theme = theme with { GalleryAppearance = a with { TileBorderWidth = 0 } }, Focused = true }, tile, true); Assert.True(Ink(image, Color.Navy).IsEmpty); Assert.Equal(bounds, capture.Focus);
            g.Clear(Color.White); painter.DrawGalleryTile(c with { Theme = theme with { GalleryAppearance = a with { TileBorderColor = Color.FromArgb(128, Color.Blue) } } }, tile, true); var alpha = image.GetPixel(frame.Left, frame.Top + frame.Height / 2); Assert.Equal(255, alpha.B); Assert.InRange(alpha.R, 1, 254);
            painter.DrawGalleryTile(c with { Theme = theme with { GalleryAppearance = a with { TileInset = 1024, TileBorderWidth = 32 } } }, tile, true);
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Authored_headings_keep_weight_ink_text_bounds_and_other_surface_owners(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            var capture = new Capture(); using RibbonRenderer painter = office ? new Office([], capture) : new Flat([], capture); using var font = new Font("Segoe UI", 11, FontStyle.Bold | FontStyle.Italic);
            using var image = new Bitmap(S(300), S(100)); using var g = Graphics.FromImage(image); var bounds = new Rectangle(S(4), S(4), S(280), S(80));
            var a = new RibbonGalleryAppearanceSettings { HeaderCornerRadius = 10, HeaderInset = 3, HeaderBorderWidth = 2, HeaderBorderColor = Color.Navy, HeaderBackground = new(Color.LightYellow, Color.LightBlue), HeaderBold = false };
            var theme = RibbonThemes.Office2024Light with { GalleryAppearance = a }; var c = new RibbonPaintContext(g, bounds, theme, font, dpi, true, true, true);
            painter.DrawGalleryHeading(c with { Theme = theme with { GalleryAppearance = null } }, "Heading"); var text = capture.Text;
            g.Clear(Color.White); painter.DrawGalleryHeading(c, "Heading"); Assert.Equal(text, capture.Text); Assert.Equal(FontStyle.Italic, capture.Style); Assert.False(capture.Hot || capture.Pressed || capture.Focused); Assert.Equal((office ? theme.GalleryHeadingForeground : theme.SecondaryText).ToArgb(), capture.Ink.ToArgb());
            var frame = Rectangle.Inflate(bounds, -S(3), -S(3)); Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(frame.Left, frame.Top + frame.Height / 2).ToArgb());
            Assert.NotEqual(image.GetPixel(frame.Left + frame.Width / 2, frame.Top + S(8)), image.GetPixel(frame.Left + frame.Width / 2, frame.Bottom - S(8)));
            g.Clear(Color.White); painter.DrawGalleryHeading(c with { Theme = theme with { GalleryAppearance = a with { HeaderBorderColor = Color.Transparent } } }, "Heading"); Assert.True(Ink(image, Color.Navy).IsEmpty);
            painter.DrawGalleryHeading(c with { Theme = theme with { GalleryAppearance = a with { HeaderBold = null, HeaderInset = 1024, HeaderBorderWidth = 32 } } }, "Heading"); Assert.Equal(FontStyle.Bold | FontStyle.Italic, capture.Style);
            g.Clear(Color.White); painter.DrawMenuHeader(c, "Ordinary"); using var menu = (Bitmap)image.Clone();
            g.Clear(Color.White); painter.DrawMenuHeader(c with { Theme = theme with { GalleryAppearance = null } }, "Ordinary"); Same(menu, image);
            g.Clear(Color.White); painter.DrawGalleryStripFrame(c); painter.DrawGalleryStripButton(c, RibbonGalleryStripButton.Expand, true); using var strip = (Bitmap)image.Clone();
            g.Clear(Color.White); painter.DrawGalleryStripFrame(c with { Theme = theme with { GalleryAppearance = null } }); painter.DrawGalleryStripButton(c with { Theme = theme with { GalleryAppearance = null } }, RibbonGalleryStripButton.Expand, true); Same(strip, image);
        }
    });

    [Theory] [InlineData("TileCornerRadius", 64)] [InlineData("TileInset", 1024)] [InlineData("TileBorderWidth", 32)] [InlineData("HeaderCornerRadius", 64)] [InlineData("HeaderInset", 1024)] [InlineData("HeaderBorderWidth", 32)]
    public void Gallery_dimensions_validate_before_mutation_and_support_endpoints(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var property = typeof(RibbonGalleryAppearanceSettings).GetProperty(name)!;
        foreach (int n in new[] { -1, maximum + 1 }) { var a = new RibbonGalleryAppearanceSettings(); property.SetValue(a, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { GalleryAppearance = a }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (int? n in new int?[] { 0, maximum, null }) { var a = new RibbonGalleryAppearanceSettings(); property.SetValue(a, n); ribbon.Theme = original with { GalleryAppearance = a }; Assert.Same(a, ribbon.Theme.GalleryAppearance); }
    });

    [Fact]
    public void Gallery_reset_and_geometry_preserve_other_roles_and_source_identity() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); var gallery = new RibbonGalleryItem(); var tab = new RibbonTab(); var group = new RibbonGroup(); group.Items.Add(gallery); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        gallery.Items.AddRange(Enumerable.Range(0, 9).Select(i => new RibbonGalleryTile { Text = "Tile " + i, GroupName = i < 4 ? "One" : "Two" }).ToArray()); var selected = (RibbonGalleryTile)gallery.Items[0]; gallery.SelectedItem = selected;
        var a = new RibbonGalleryAppearanceSettings { TileCornerRadius = 12, HeaderBold = false, HeaderBackground = new(Color.FromArgb(100, Color.Yellow)) };
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            ribbon.Density = density; var before = RibbonGalleryLayout.Build(gallery, ribbon.Font, ribbon.DeviceDpi, new(900, 700));
            var app = ribbon.Theme with { GalleryAppearance = a, ApplicationMenuAppearance = new() { RowInset = 4 } }; ribbon.Theme = app;
            var after = RibbonGalleryLayout.Build(gallery, ribbon.Font, ribbon.DeviceDpi, new(900, 700)); Assert.Equal(before.Extent, after.Extent); Assert.Equal(before.Targets.Select(t => t.Bounds), after.Targets.Select(t => t.Bounds)); Assert.Equal(before.Headings, after.Headings);
            ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; ribbon.BuiltInTheme = null; Assert.Same(app, ribbon.Theme); Assert.Null(other.Theme.GalleryAppearance);
            ribbon.Theme = app with { GalleryAppearance = a with { HeaderBackground = null, TileBorderColor = Color.Empty } }; Assert.False(ribbon.Theme.GalleryAppearance!.HeaderBold); Assert.Equal(12, a.TileCornerRadius);
            ribbon.Theme = app with { GalleryAppearance = null }; Assert.Equal(4, ribbon.Theme.ApplicationMenuAppearance!.RowInset); Assert.Same(selected, gallery.SelectedItem);
        }
    });

    [Fact]
    public void Actual_protected_chrome_override_can_replace_gallery_builtin_decoration() => FoundationTests.Sta(() =>
    {
        var capture = new Capture { ReplaceChrome = true }; using var painter = new Office([], capture); using var tile = new RibbonGalleryTile(); using var image = new Bitmap(200, 100); using var g = Graphics.FromImage(image);
        var theme = RibbonThemes.Office2024Light with { GalleryAppearance = new() { TileInset = 10, TileBorderWidth = 3, TileBorderColor = Color.Navy, HeaderInset = 10, HeaderBackground = new(Color.LightBlue) } };
        var c = new RibbonPaintContext(g, new(4, 4, 180, 88), theme, Control.DefaultFont, 96); g.Clear(Color.White); painter.DrawGalleryTile(c, tile, true); Assert.Equal(Color.Orange.ToArgb(), image.GetPixel(4, 4).ToArgb());
        g.Clear(Color.White); painter.DrawGalleryHeading(c, "Heading"); Assert.Equal(Color.Orange.ToArgb(), image.GetPixel(4, 4).ToArgb()); Assert.Equal(2, capture.Chrome);
    });

    private static Rectangle Ink(Bitmap image, Color ink) { var bounds = Rectangle.Empty; for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() == ink.ToArgb()) bounds = bounds.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(bounds, new(x, y, 1, 1)); return bounds; }
    private static void Same(Bitmap a, Bitmap b) { for (int y = 0; y < a.Height; y++) for (int x = 0; x < a.Width; x++) Assert.Equal(a.GetPixel(x, y), b.GetPixel(x, y)); }
    private sealed class Capture
    {
        internal Rectangle Text, Artwork, Focus; internal FontStyle Style; internal Color Ink; internal bool Hot, Pressed, Focused, ReplaceChrome; internal int Chrome;
        internal void Record(RibbonPaintContext c, Color ink) { Text = c.Bounds; Style = c.Font.Style; Ink = ink; Hot = c.Hot; Pressed = c.Pressed; Focused = c.Focused; }
    }
    [Fact]
    public void Default_gallery_tile_and_heading_pixels_slots_and_layout_match_saved_results() => FoundationTests.Sta(() =>
    {
        foreach (bool classic in new[] { false, true })
        foreach (bool office in new[] { false, true })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (int fontSize in new[] { 9, 14 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var log = new List<string>(); using RibbonRenderer painter = office ? new Office(log) : new Flat(log);
            var theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { GalleryAppearance = Environment.GetEnvironmentVariable("RIBBONKIT_GALLERY_APPEARANCE_EMPTY") == "1" ? new() : null };
            using var font = new Font("Segoe UI", fontSize * dpi / 96f); using var raster = new Bitmap(40, 20); using (var rg = Graphics.FromImage(raster)) rg.Clear(Color.DarkRed);
            using var gallery = new RibbonGalleryItem { ColumnCount = 3, MaxVisibleRows = 2 };
            var tile = new RibbonGalleryTile { Text = "Document style", Image = raster, GroupName = "Styles" }; gallery.Items.Add(tile);
            using var image = new Bitmap(S(620), S(410)); using var g = Graphics.FromImage(image); g.Clear(theme.Surface);
            var c = new RibbonPaintContext(g, new(S(6), S(6), S(608), S(32)), theme, font, dpi) { Density = density };
            painter.DrawGalleryHeading(c, "Gallery heading");
            for (int state = 0; state < 9; state++)
            {
                tile.Enabled = state < 6; bool selected = state is 3 or 4 or 5 or 7;
                var bounds = new Rectangle(S(8 + state % 3 * 202), S(46 + state / 3 * 114), S(196), S(108));
                painter.DrawGalleryTile(c with { Bounds = bounds, Hot = state is 1 or 4 or 6, Pressed = state is 2 or 5, Focused = state is 7 or 8 }, tile, selected);
            }
            painter.DrawMenuHeader(c with { Bounds = new(S(6), S(389), S(300), S(18)) }, "Ordinary menu");
            using var ribbon = new RibbonControl { Theme = theme, Density = density, Font = font }; var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Styles" }; var source = new RibbonGalleryItem { ColumnCount = 3, MaxVisibleRows = 2 };
            source.Items.AddRange(Enumerable.Range(0, 7).Select(i => new RibbonGalleryTile { Text = "Style " + i, GroupName = i < 4 ? "One" : "Two" }).ToArray()); group.Items.Add(source); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            var layout = RibbonGalleryLayout.Build(source, font, dpi, new(S(650), S(500))); log.Add($"layout:{layout.Extent}/{layout.ViewportHeight}/{layout.Columns}"); foreach (var target in layout.Targets) log.Add("target:" + target.Bounds); foreach (var heading in layout.Headings) log.Add("heading:" + heading.Bounds);
            using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); var png = stream.ToArray(); var geometry = string.Join(Environment.NewLine, log);
            string name = $"gallery-{classic}-{office}-{dpi}-{density}-{fontSize}";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_GALLERY_APPEARANCE_CAPTURE") is { Length: > 0 } capture) { Directory.CreateDirectory(capture); File.WriteAllBytes(Path.Combine(capture, name + ".png"), png); File.WriteAllText(Path.Combine(capture, name + ".txt"), geometry); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_GALLERY_APPEARANCE_BASELINE") is { Length: > 0 } baseline) { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), geometry); }
        }
    });
    private sealed class Office(List<string> log, Capture? capture = null) : RibbonOffice2024Renderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { if (capture is not null) { capture.Record(c, color); return; } log.Add($"text:{text}/{c.Bounds}/{c.Font.Style}/{color.ToArgb()}"); base.DrawText(c, text, color, flags); }
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { if (capture is not null) { capture.Artwork = c.Bounds; return; } log.Add("image:" + c.Bounds); base.DrawItemImage(c, item, size); }
        public override void DrawFocus(RibbonPaintContext c) { if (capture is not null) { capture.Focus = c.Bounds; return; } log.Add("focus:" + c.Bounds); base.DrawFocus(c); }
        protected override void DrawChrome(RibbonPaintContext c, RibbonFill fill, Color border) { if (capture is not null) { capture.Chrome++; if (capture.ReplaceChrome) { using var brush = new SolidBrush(Color.Orange); c.Graphics.FillRectangle(brush, c.Bounds); return; } } base.DrawChrome(c, fill, border); }
    }
    private sealed class Flat(List<string> log, Capture? capture = null) : RibbonRenderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { if (capture is not null) { capture.Record(c, color); return; } log.Add($"text:{text}/{c.Bounds}/{c.Font.Style}/{color.ToArgb()}"); base.DrawText(c, text, color, flags); }
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { if (capture is not null) { capture.Artwork = c.Bounds; return; } log.Add("image:" + c.Bounds); base.DrawItemImage(c, item, size); }
        public override void DrawFocus(RibbonPaintContext c) { if (capture is not null) { capture.Focus = c.Bounds; return; } log.Add("focus:" + c.Bounds); base.DrawFocus(c); }
        protected override void DrawChrome(RibbonPaintContext c, RibbonFill fill, Color border) { if (capture is not null) { capture.Chrome++; if (capture.ReplaceChrome) { using var brush = new SolidBrush(Color.Orange); c.Graphics.FillRectangle(brush, c.Bounds); return; } } base.DrawChrome(c, fill, border); }
    }
}
