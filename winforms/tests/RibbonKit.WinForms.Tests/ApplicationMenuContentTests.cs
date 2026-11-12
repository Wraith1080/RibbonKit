using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ApplicationMenuContentTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Authored_icons_gutters_descriptions_and_weight_share_measurement_and_keep_actual_hooks(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi); var record = new Record(); using var painter = new Office(record);
        using var font = new Font("Segoe UI", 10 * dpi / 96f, FontStyle.Bold | FontStyle.Italic); using var image = new Bitmap(S(400), S(240)); using var g = Graphics.FromImage(image);
        using var pane = new RibbonDropDownItem { MenuPresentation = RibbonMenuPresentation.IllustratedChoices };
        var item = new RibbonButtonItem { Text = "Rich", Icon = RibbonStockIcons.Create("save"), MenuDescription = "A longer description that occupies several wrapped lines in the shared content slots and exercises the line cap." }; pane.Items.Add(item);
        var a = new RibbonApplicationMenuContentSettings { LargeIconSize = 40, SmallIconSize = 24, LargeIconInset = 10, SmallIconInset = 4, IconTextGap = 12, LargeTextInset = 72, TextInset = 10, TextTrailingPadding = 9,
            RowTextVerticalPadding = 16, IconVerticalPadding = 12, DescriptionGap = 6, DescriptionVerticalInset = 8, DescriptionMaxLines = 3, RichTitleBold = false, FooterHorizontalPadding = 10 };
        var theme = RibbonThemes.Office2024Light with { ApplicationMenuContent = a };
        var bounds = new Rectangle(S(4), S(4), S(360), S(210)); var c = new RibbonPaintContext(g, bounds, theme, font, dpi) { ApplicationMenuPart = RibbonApplicationMenuPart.Pane, Focused = true };
        painter.DrawItem(c, item, RibbonItemSize.Medium, true);
        Assert.Equal(new Rectangle(bounds.Left + S(10), bounds.Top + (bounds.Height - S(40)) / 2, S(40), S(40)), record.Image);
        Assert.Equal(bounds.Left + S(72), record.Texts[0].Bounds.Left); Assert.Equal(FontStyle.Italic, record.Texts[0].Style); Assert.Equal(2, record.Texts.Count);
        Assert.Equal(record.Texts[0].Bounds.Bottom + S(6), record.Texts[1].Bounds.Top); Assert.InRange(record.Texts[1].Bounds.Height, 1, 3 * font.Height); Assert.Equal(bounds, record.Focus);
        int height = RibbonApplicationMenuMetrics.RowHeight(item, bounds.Width, font, dpi, RibbonDensity.Compact, false, theme);
        record.Texts.Clear(); painter.DrawItem(c with { Theme = theme with { ApplicationMenuContent = a with { DescriptionMaxLines = 0, LargeIconSize = 0 } } }, item, RibbonItemSize.Medium, true);
        Assert.Single(record.Texts); Assert.True(RibbonApplicationMenuMetrics.RowHeight(item, bounds.Width, font, dpi, RibbonDensity.Compact, false, theme with { ApplicationMenuContent = a with { DescriptionMaxLines = 0 } }) < height);
        using var plain = new RibbonButtonItem { Text = "Plain" }; record.Texts.Clear(); painter.DrawItem(c, plain, RibbonItemSize.Medium, true); Assert.Equal(bounds.Left + S(10), record.Texts[0].Bounds.Left);
        record.Texts.Clear(); painter.DrawItem(c with { ApplicationMenuPart = RibbonApplicationMenuPart.Footer }, item, RibbonItemSize.Medium, true); Assert.Single(record.Texts); Assert.Equal(S(24), record.Image.Width); Assert.Equal(record.Image.Right + S(12), record.Texts[0].Bounds.Left);
        int images = record.Images; painter.DrawItem(c with { Theme = theme with { ApplicationMenuContent = a with { LargeIconSize = 0 } } }, item, RibbonItemSize.Medium, true); Assert.Equal(images, record.Images);
        painter.DrawItem(c with { Theme = theme with { ApplicationMenuContent = a with { LargeIconSize = 1024, LargeIconInset = 1024, LargeTextInset = 1024, DescriptionVerticalInset = 1024 } } }, item, RibbonItemSize.Medium, true);
        Assert.InRange(record.Texts.Last().Bounds.Width, 0, bounds.Width);
        painter.DrawItem(c with { ApplicationMenuPart = RibbonApplicationMenuPart.Footer, Theme = theme with { ApplicationMenuContent = a with { SmallIconSize = 1024, FooterHorizontalPadding = 1024 } } }, item, RibbonItemSize.Medium, true);
        Assert.True(record.Image.Left >= bounds.Left && record.Image.Right <= bounds.Right && record.Image.Top >= bounds.Top && record.Image.Bottom <= bounds.Bottom);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Arrow_targets_separator_rows_and_authored_ink_keep_density_and_surface_boundaries(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi); var record = new Record(); using var painter = new Office(record); using var image = new Bitmap(S(360), S(100)); using var g = Graphics.FromImage(image);
        using var split = new RibbonSplitButtonItem { Text = "Split" }; using var separator = new RibbonMenuSeparatorItem();
        var a = new RibbonApplicationMenuContentSettings { ArrowColumnWidth = 38, SeparatorHeight = 13, SeparatorHorizontalPadding = 12, SeparatorThickness = 3, SeparatorColor = Color.Magenta };
        var theme = RibbonThemes.Office2024Light with { ApplicationMenuContent = a }; var bounds = new Rectangle(S(4), S(4), S(340), S(80));
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            var targets = RibbonApplicationMenuMetrics.Expand(new(TargetKind.Item, split, bounds, bounds), dpi, density, theme).ToArray(); Assert.Equal(S(density == RibbonDensity.Touch ? 44 : 38), targets[1].Bounds.Width);
            var c = new RibbonPaintContext(g, targets[0].Bounds, theme, Control.DefaultFont, dpi) { ApplicationMenuPart = RibbonApplicationMenuPart.Navigation, Density = density };
            painter.DrawSplitItem(c, c with { Bounds = targets[1].Bounds }, split, RibbonItemSize.Medium, true); Assert.Equal(targets[1].Bounds, record.Arrow);
            var wide = theme with { ApplicationMenuContent = a with { ArrowColumnWidth = 1024 } }; Assert.Equal(bounds.Width / 2, RibbonApplicationMenuMetrics.Expand(new(TargetKind.Item, split, bounds, bounds), dpi, density, wide).Last().Bounds.Width);
        }
        Assert.Equal(S(13), RibbonApplicationMenuMetrics.RowHeight(separator, bounds.Width, Control.DefaultFont, dpi, RibbonDensity.Compact, false, theme));
        var row = new Rectangle(bounds.Left, bounds.Top, bounds.Width, S(13)); var context = new RibbonPaintContext(g, row, theme, Control.DefaultFont, dpi) { ApplicationMenuPart = RibbonApplicationMenuPart.Pane };
        g.Clear(Color.White); painter.DrawMenuSeparator(context, separator); Assert.Equal(new Rectangle(row.Left + S(12), row.Top + (row.Height - S(3)) / 2, row.Width - 2 * S(12), S(3)), Ink(image, Color.Magenta));
        foreach (var hide in new[] { a with { SeparatorThickness = 0 }, a with { SeparatorColor = Color.Transparent } }) { g.Clear(Color.White); painter.DrawMenuSeparator(context with { Theme = theme with { ApplicationMenuContent = hide } }, separator); Assert.True(Ink(image, Color.Magenta).IsEmpty); }
        g.Clear(Color.White); painter.DrawMenuSeparator(context with { Theme = theme with { ApplicationMenuContent = a with { SeparatorColor = Color.FromArgb(128, Color.Blue), SeparatorHorizontalPadding = 1024, SeparatorThickness = 32 } } }, separator); Assert.Equal(255, image.GetPixel(row.Left + row.Width / 2, row.Top + row.Height / 2).A);
        g.Clear(Color.White); painter.DrawMenuSeparator(context with { ApplicationMenuPart = RibbonApplicationMenuPart.None }, separator); using var before = (Bitmap)image.Clone();
        g.Clear(Color.White); painter.DrawMenuSeparator(context with { ApplicationMenuPart = RibbonApplicationMenuPart.None, Theme = theme with { ApplicationMenuContent = null } }, separator); Same(before, image);
        Assert.Equal(0, RibbonApplicationMenuMetrics.RowHeight(separator, bounds.Width, Control.DefaultFont, dpi, RibbonDensity.Compact, false, theme with { ApplicationMenuContent = a with { SeparatorHeight = 0 } }));
    });

    public static IEnumerable<object[]> Dimensions() => new[] { "LargeIconSize", "SmallIconSize", "LargeIconInset", "SmallIconInset", "IconTextGap", "LargeTextInset", "TextInset", "TextTrailingPadding", "RowTextVerticalPadding", "IconVerticalPadding", "DescriptionGap", "DescriptionVerticalInset", "FooterHorizontalPadding", "SeparatorHeight", "SeparatorHorizontalPadding", "ArrowColumnWidth", "DescriptionMaxLines", "SeparatorThickness" }
        .Select(name => new object[] { name, name == "ArrowColumnWidth" ? 1 : 0, name is "DescriptionMaxLines" or "SeparatorThickness" ? 32 : 1024 });
    [Theory] [MemberData(nameof(Dimensions))]
    public void Content_dimensions_validate_atomically_and_support_endpoints(string name, int minimum, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var property = typeof(RibbonApplicationMenuContentSettings).GetProperty(name)!;
        foreach (int n in new[] { minimum - 1, maximum + 1 }) { var a = new RibbonApplicationMenuContentSettings(); property.SetValue(a, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { ApplicationMenuContent = a }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (int? n in new int?[] { minimum, maximum, null }) { var a = new RibbonApplicationMenuContentSettings(); property.SetValue(a, n); ribbon.Theme = original with { ApplicationMenuContent = a }; Assert.Same(a, ribbon.Theme.ApplicationMenuContent); }
    });

    [Fact]
    public void Footer_indicators_reserve_authored_artwork_and_touch_rows_keep_their_minimum() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2024Light with { ApplicationMenuContent = new() { SmallIconSize = 64, IconTextGap = 12, FooterHorizontalPadding = 14 } } };
        var check = new RibbonCheckBoxItem { Text = "Keep document formatting" }; ribbon.ApplicationMenu.FooterItems.Add(check);
        var layout = RibbonApplicationMenuLayout.Build(ribbon, ribbon.ApplicationMenu.DefaultPane, new(1600, 1000));
        int S(int n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi);
        int label = TextRenderer.MeasureText(check.Text, ribbon.Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
        Assert.True(layout.Targets.Single(t => ReferenceEquals(t.Element, check)).Bounds.Width >= label + S(64) + S(12) + 2 * S(14));
        using var item = new RibbonButtonItem(); var small = ribbon.Theme with { ApplicationMenuContent = new() { LargeIconSize = 0, SmallIconSize = 0, RowTextVerticalPadding = 0, IconVerticalPadding = 0 } };
        Assert.True(RibbonApplicationMenuMetrics.RowHeight(item, S(200), ribbon.Font, ribbon.DeviceDpi, RibbonDensity.Touch, true, small) >= S(44));
    });

    [Fact]
    public void Live_application_separators_keep_their_surface_context_and_authored_paint() => FoundationTests.Sta(() =>
    {
        var record = new Record(); using var painter = new Office(record); using var ribbon = new RibbonControl { Renderer = painter, Theme = RibbonThemes.Office2019Light with { ApplicationMenuContent = new() { SeparatorHeight = 13, SeparatorHorizontalPadding = 12, SeparatorThickness = 3, SeparatorColor = Color.Magenta } } };
        ribbon.FileMenu.Items.AddRange(new RibbonButtonItem { Text = "Command" }, new RibbonMenuSeparatorItem()); ribbon.ApplicationMenu.DefaultPane.Items.AddRange(new RibbonButtonItem { Text = "Recent" }, new RibbonMenuSeparatorItem()); ribbon.ApplicationMenu.FooterItems.Add(new RibbonMenuSeparatorItem());
        using var surface = new RibbonPopupSurface(ribbon, ribbon.FileMenu, new(1200, 800)); using var image = new Bitmap(surface.Width, surface.Height); surface.DrawToBitmap(image, surface.ClientRectangle);
        Assert.Contains(RibbonApplicationMenuPart.Navigation, record.Separators); Assert.Contains(RibbonApplicationMenuPart.Pane, record.Separators); Assert.Contains(RibbonApplicationMenuPart.Footer, record.Separators); Assert.DoesNotContain(RibbonApplicationMenuPart.None, record.Separators); Assert.False(Ink(image, Color.Magenta).IsEmpty);
    });

    [Fact]
    public void Content_member_and_group_reset_restore_application_without_clearing_appearance() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); var a = new RibbonApplicationMenuContentSettings { LargeIconSize = 40, RichTitleBold = false, DescriptionMaxLines = 3 };
        var app = ribbon.Theme with { ApplicationMenuContent = a, ApplicationMenuAppearance = new() { RowInset = 4 } }; ribbon.Theme = app; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = null; Assert.Same(app, ribbon.Theme); Assert.Null(other.Theme.ApplicationMenuContent);
        ribbon.Theme = app with { ApplicationMenuContent = a with { LargeIconSize = null, SeparatorColor = Color.Empty } }; Assert.False(ribbon.Theme.ApplicationMenuContent!.RichTitleBold); Assert.Equal(40, a.LargeIconSize);
        ribbon.Theme = app with { ApplicationMenuContent = null }; Assert.Equal(4, ribbon.Theme.ApplicationMenuAppearance!.RowInset);
    });

    private static Rectangle Ink(Bitmap image, Color ink) { var bounds = Rectangle.Empty; for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() == ink.ToArgb()) bounds = bounds.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(bounds, new(x, y, 1, 1)); return bounds; }
    private static void Same(Bitmap a, Bitmap b) { for (int y = 0; y < a.Height; y++) for (int x = 0; x < a.Width; x++) Assert.Equal(a.GetPixel(x, y), b.GetPixel(x, y)); }
    [Fact]
    public void Default_content_pixels_slots_and_measurements_match_saved_results() => FoundationTests.Sta(() =>
    {
        foreach (bool classic in new[] { false, true })
        foreach (bool office in new[] { false, true })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (int fontSize in new[] { 9, 14 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
            var theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { ApplicationMenuContent = Environment.GetEnvironmentVariable("RIBBONKIT_APPLICATION_CONTENT_EMPTY") == "1" ? new() : null };
            using var font = new Font("Segoe UI", fontSize * dpi / 96f); using var raster = new Bitmap(16, 16); using (var rg = Graphics.FromImage(raster)) rg.Clear(Color.DarkRed);
            using var richPane = new RibbonDropDownItem { MenuPresentation = RibbonMenuPresentation.IllustratedChoices };
            var rich = new RibbonButtonItem { Text = "Illustrated choice", Icon = RibbonStockIcons.Create("save"), MenuDescription = "Save a reusable document with formatting and settings. More description text wraps onto another line." }; richPane.Items.Add(rich);
            using var plain = new RibbonButtonItem { Text = "Plain command" }; using var icon = new RibbonButtonItem { Text = "With image", Image = raster };
            using var disabled = new RibbonButtonItem { Text = "Disabled image", Image = raster, Enabled = false };
            using var described = new RibbonButtonItem { Text = "Described command", MenuDescription = "An ordinary command description that wraps over several lines to exercise the existing cap." };
            using var split = new RibbonSplitButtonItem { Text = "Split command", Icon = RibbonStockIcons.Create("save") };
            using var check = new RibbonCheckBoxItem { Text = "Check", Checked = true }; using var radio = new RibbonRadioButtonItem { Text = "Radio", Checked = true }; using var separator = new RibbonMenuSeparatorItem();
            RibbonItem[] items = [plain, icon, disabled, described, rich, split, check, radio, separator];
            using var image = new Bitmap(S(1080), S(820)); using var g = Graphics.FromImage(image); g.Clear(theme.PopupColor);
            foreach (var part in new[] { RibbonApplicationMenuPart.Navigation, RibbonApplicationMenuPart.Pane, RibbonApplicationMenuPart.Footer })
            {
                int column = (int)part - 1;
                for (int i = 0; i < items.Length; i++)
                {
                    var bounds = new Rectangle(S(6 + column * 358), S(6 + i * 90), S(350), S(84));
                    var c = new RibbonPaintContext(g, bounds, theme, font, dpi, Hot: i % 2 == 0, Focused: i == 5) { ApplicationMenuPart = part, Density = density };
                    if (items[i] is RibbonSplitButtonItem sp)
                    {
                        var targets = RibbonApplicationMenuMetrics.Expand(new(TargetKind.Item, sp, bounds, bounds), dpi, density).ToArray();
                        painter.DrawSplitItem(c with { Bounds = targets[0].Bounds }, c with { Bounds = targets[1].Bounds }, sp, RibbonItemSize.Medium, true);
                        record.Log.Add($"targets:{targets[0].Bounds}/{targets[1].Bounds}");
                    }
                    else painter.DrawItem(c, items[i], RibbonItemSize.Medium, true);
                    record.Log.Add($"measure:{part}/{i}/{RibbonApplicationMenuMetrics.RowHeight(items[i], S(350), font, dpi, density, part == RibbonApplicationMenuPart.Navigation, theme)}");
                }
            }
            record.Log.Add("footer-height:" + RibbonApplicationMenuMetrics.FooterRowHeight(items, font, dpi, density, theme));
            using var ribbon = new RibbonControl { Theme = theme, Font = font, Density = density };
            ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Navigation" }); ribbon.FileMenu.Items.Add(new RibbonSplitButtonItem { Text = "Split" });
            ribbon.ApplicationMenu.DefaultPane.Items.Add(new RibbonButtonItem { Text = "Recent", MenuDescription = "A recent document description." });
            ribbon.ApplicationMenu.FooterItems.Add(new RibbonButtonItem { Text = "Options", Image = raster });
            var layout = RibbonApplicationMenuLayout.Build(ribbon, ribbon.ApplicationMenu.DefaultPane, new(1200, 900));
            record.Log.Add($"layout:{layout.Size}/{layout.Navigation}/{layout.Pane}/{layout.Footer}"); foreach (var target in layout.Targets) record.Log.Add("layout-target:" + target.Bounds);
            using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); var png = stream.ToArray(); string geometry = string.Join(Environment.NewLine, record.Log);
            string name = $"content-{classic}-{office}-{dpi}-{density}-{fontSize}";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_APPLICATION_CONTENT_CAPTURE") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); File.WriteAllBytes(Path.Combine(capture, name + ".png"), png); File.WriteAllText(Path.Combine(capture, name + ".txt"), geometry); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_APPLICATION_CONTENT_BASELINE") is { Length: > 0 } baseline)
            { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), geometry); }
        }
    });

    private sealed class Record
    {
        internal readonly List<string> Log = [];
        internal readonly List<(Rectangle Bounds, FontStyle Style)> Texts = []; internal Rectangle Image, Arrow, Focus; internal int Images;
        internal readonly List<RibbonApplicationMenuPart> Separators = [];
        internal void Text(RibbonPaintContext c, string text, Color color) { Texts.Add((c.Bounds, c.Font.Style)); Log.Add($"text:{text}/{c.Bounds}/{c.Font.Style}/{color.ToArgb()}"); }
        internal void Icon(RibbonPaintContext c, RibbonItemSize size) { Image = c.Bounds; Images++; Log.Add($"icon:{c.Bounds}/{size}"); }
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text(c, text, color); base.DrawText(c, text, color, flags); }
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { record.Icon(c, size); base.DrawItemImage(c, item, size); }
        public override void DrawSubmenuArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawSubmenuArrow(c, color); }
        public override void DrawFocus(RibbonPaintContext c) { record.Focus = c.Bounds; base.DrawFocus(c); }
        public override void DrawMenuSeparator(RibbonPaintContext c, RibbonMenuSeparatorItem item) { record.Separators.Add(c.ApplicationMenuPart); base.DrawMenuSeparator(c, item); }
    }
    private sealed class Flat(Record record) : RibbonRenderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text(c, text, color); base.DrawText(c, text, color, flags); }
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { record.Icon(c, size); base.DrawItemImage(c, item, size); }
    }
}
