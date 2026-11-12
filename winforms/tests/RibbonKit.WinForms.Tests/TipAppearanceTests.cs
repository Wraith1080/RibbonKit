using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class TipAppearanceTests
{
    private static RibbonTipAppearanceSettings Authored => new() { ScreenTipTitleBold = false, KeyTipInset = 2, KeyTipBorderWidth = 3, BackstageKeyTipRightClearance = 20, BackstageKeyTipHorizontalClearance = 24, BackstageKeyTipVerticalClearance = 10 };
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Title_weight_uses_the_same_host_styles_for_measuring_and_actual_text_hooks(int dpi) => FoundationTests.Sta(() =>
    {
        const string title = "Search a long title across the current document";
        using var font = new Font("Segoe UI", 14 * dpi / 96f, FontStyle.Bold | FontStyle.Italic);
        var theme = RibbonThemes.Office2019Light with { TipAppearance = Authored, ScreenTipMaximumWidth = 190 };
        using var normal = new Font(font, FontStyle.Italic); var m = new RibbonTipMetrics(theme);
        Assert.Equal(normal.Style, m.TitleStyle(font));
        var layout = RibbonScreenTipLayout.Measure(title, "", font, dpi, new(800, 900), theme);
        int width = RibbonLayoutEngine.Scale(190 - 24, dpi);
        Assert.Equal(TextRenderer.MeasureText(title, normal, new(width, int.MaxValue), RibbonScreenTipLayout.TextFlags).Height, layout.Title.Height);
        using var image = new Bitmap(800, 900); using var g = Graphics.FromImage(image);
        foreach (bool office in new[] { false, true })
        {
            var log = new List<string>(); using RibbonRenderer painter = office ? new Office(log) : new Flat(log);
            painter.DrawScreenTip(new(g, new(Point.Empty, layout.Size), theme, font, dpi), title, "", layout.Title, Rectangle.Empty);
            Assert.Single(log); Assert.Contains("/Italic/", log[0]);
            Assert.Equal(FontStyle.Bold | FontStyle.Italic, new RibbonTipMetrics(theme with { TipAppearance = Authored with { ScreenTipTitleBold = null } }).TitleStyle(font));
        }
    });
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Badge_outline_clamps_hides_keeps_alpha_corners_text_slots_and_actual_hooks(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            var log = new List<string>(); using RibbonRenderer painter = office ? new Office(log) : new Flat(log);
            using var image = new Bitmap(S(180), S(80)); using var g = Graphics.FromImage(image);
            var bounds = new Rectangle(S(5), S(5), S(160), S(60)); var paint = Rectangle.Inflate(bounds, -S(2), -S(2));
            var theme = RibbonThemes.Office2019Light with { TipAppearance = Authored, KeyTipCornerRadius = 10, KeyTipBorder = Color.Navy, KeyTipBackground = new(Color.LightYellow) };
            var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi); g.Clear(Color.White); painter.DrawKeyTip(c, "");
            Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(paint.Left, paint.Top + paint.Height / 2).ToArgb()); Assert.Contains(bounds.ToString(), log.Single());
            for (int y = 0; y < S(12); y++) for (int x = 0; x < S(12); x++)
            {
                var pixel = image.GetPixel(paint.Left + x, paint.Top + y);
                Assert.Equal(pixel, image.GetPixel(paint.Right - 1 - x, paint.Top + y)); Assert.Equal(pixel, image.GetPixel(paint.Left + x, paint.Bottom - 1 - y)); Assert.Equal(pixel, image.GetPixel(paint.Right - 1 - x, paint.Bottom - 1 - y));
            }
            g.Clear(Color.White); painter.DrawKeyTip(c with { Theme = theme with { KeyTipBorder = Color.FromArgb(128, Color.Blue) } }, ""); var alpha = image.GetPixel(paint.Left, paint.Top + paint.Height / 2); Assert.Equal(255, alpha.B); Assert.InRange(alpha.R, 1, 254);
            foreach (var hidden in new[] { theme with { TipAppearance = Authored with { KeyTipBorderWidth = 0 } }, theme with { KeyTipBorder = Color.Transparent } })
            { g.Clear(Color.White); painter.DrawKeyTip(c with { Theme = hidden }, ""); Assert.DoesNotContain(Enumerable.Range(0, image.Height), y => Enumerable.Range(0, image.Width).Any(x => image.GetPixel(x, y).ToArgb() == Color.Navy.ToArgb())); }
            g.Clear(Color.White); painter.DrawKeyTip(c with { Theme = theme with { TipAppearance = Authored with { KeyTipInset = 1024, KeyTipBorderWidth = 32 } } }, "");
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) Assert.True(bounds.Contains(x, y));
            using var replace = new Replacement(); replace.DrawKeyTip(c, "A"); replace.DrawScreenTip(c, "Title", "", bounds, Rectangle.Empty); Assert.Equal(2, replace.Calls);
            foreach (var empty in new[] { new Rectangle(bounds.Location, new Size(0, bounds.Height)), new Rectangle(bounds.Location, new Size(bounds.Width, 0)) })
            {
                log.Clear(); g.Clear(Color.White); painter.DrawKeyTip(c with { Bounds = empty }, "AB"); Assert.Empty(log);
                Assert.DoesNotContain(Enumerable.Range(0, image.Height), y => Enumerable.Range(0, image.Width).Any(x => image.GetPixel(x, y).ToArgb() != Color.White.ToArgb()));
            }
        }
    });
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Backstage_clearances_match_legacy_rounding_and_fit_short_narrow_targets(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi); var text = new Size(S(28), S(18)); var defaults = new RibbonTipMetrics(null);
        foreach (var bounds in new[] { new Rectangle(13, 17, S(200), S(70)), new Rectangle(13, 17, S(11), S(7)), new Rectangle(13, 17, 0, 0) })
        {
            var desired = defaults.BadgeSize(text, dpi, true); var size = new Size(Math.Min(Math.Max(0, bounds.Width - S(8)), desired.Width), Math.Min(Math.Max(0, bounds.Height - S(4)), desired.Height));
            var expected = new Rectangle(bounds.Left + Math.Max(0, bounds.Width - S(4) - size.Width), bounds.Top + (bounds.Height - size.Height) / 2, size.Width, size.Height);
            Assert.Equal(expected, defaults.BackstageBadgeBounds(bounds, text, dpi));
            foreach (var a in new[] { Authored, Authored with { BackstageKeyTipRightClearance = 1024 }, Authored with { BackstageKeyTipHorizontalClearance = 0, BackstageKeyTipVerticalClearance = 0 }, Authored with { BackstageKeyTipHorizontalClearance = 1024, BackstageKeyTipVerticalClearance = 1024 } })
                Assert.True(bounds.Contains(new RibbonTipMetrics(RibbonThemes.Office2019Light with { TipAppearance = a }).BackstageBadgeBounds(bounds, text, dpi)));
        }
        var roomy = new Rectangle(0, 0, S(200), S(70)); var actual = new RibbonTipMetrics(RibbonThemes.Office2019Light with { TipAppearance = Authored }).BackstageBadgeBounds(roomy, text, dpi);
        Assert.Equal(roomy.Right - S(20), actual.Right);
    });
    [Theory] [InlineData("KeyTipInset", 1024)] [InlineData("KeyTipBorderWidth", 32)] [InlineData("BackstageKeyTipRightClearance", 1024)] [InlineData("BackstageKeyTipHorizontalClearance", 1024)] [InlineData("BackstageKeyTipVerticalClearance", 1024)]
    public void Dimensions_validate_before_theme_selection_mutation_and_accept_endpoints(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var p = typeof(RibbonTipAppearanceSettings).GetProperty(name)!;
        foreach (int n in new[] { -1, maximum + 1 }) { var a = new RibbonTipAppearanceSettings(); p.SetValue(a, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { TipAppearance = a }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (int? n in new int?[] { 0, maximum, null }) { var a = new RibbonTipAppearanceSettings(); p.SetValue(a, n); ribbon.Theme = original with { TipAppearance = a }; Assert.Same(a, ribbon.Theme.TipAppearance); }
    });
    [Fact]
    public void Member_group_and_builtin_resets_retain_other_roles_and_instances() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); var a = Authored;
        var theme = ribbon.Theme with { TipAppearance = a, KeyTipHorizontalPadding = 8, KeyTipCornerRadius = 7, KeyTipBorder = Color.Navy, ScreenTipContentPadding = 20, GalleryContent = new() { LabelMaxLines = 3 } }; ribbon.Theme = theme;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; ribbon.BuiltInTheme = null; Assert.Same(theme, ribbon.Theme);
        ribbon.Theme = theme with { TipAppearance = a with { ScreenTipTitleBold = null } }; Assert.Equal(3, ribbon.Theme.TipAppearance!.KeyTipBorderWidth); Assert.False(a.ScreenTipTitleBold);
        ribbon.Theme = theme with { TipAppearance = null }; Assert.Equal(8, ribbon.Theme.KeyTipHorizontalPadding); Assert.Equal(7, ribbon.Theme.KeyTipCornerRadius); Assert.Equal(20, ribbon.Theme.ScreenTipContentPadding); Assert.Equal(3, ribbon.Theme.GalleryContent!.LabelMaxLines); Assert.Null(other.Theme.TipAppearance);
    });
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Actual_backstage_badges_and_passive_tip_keep_page_editor_and_source_ownership(bool office) => FoundationTests.Sta(() =>
    {
        var record = new NativeRecord(); using RibbonRenderer painter = office ? new NativeOffice(record) : new NativeFlat(record);
        using var form = new Form { Width = 1000, Height = 650, StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false };
        using var ribbon = new RibbonControl { Renderer = painter, AnimationLevel = RibbonAnimationLevel.None, FileMenuMode = RibbonFileMenuMode.Backstage };
        var item = new RibbonButtonItem { Text = "Search document", ScreenTipDescription = "Retain the complete description" }; var tab = new RibbonTab(); var group = new RibbonGroup(); group.Items.Add(item); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.FileMenu.Visible = true; form.Controls.Add(ribbon);
        using var body = new UserControl(); var editor = new TextBox { Text = "Retained editor draft" }; body.Controls.Add(editor); var page = new RibbonBackstagePageItem { Text = "Information", KeyTip = "AB", Content = body }; ribbon.Backstage.Items.Add(page);
        form.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true; Application.DoEvents(); editor.Focus(); editor.Select(2, 5); var parent = body.Parent; nint handle = editor.Handle; var surface = ribbon.BackstageSurface!; surface.ToggleKeyTips();
        void Paint() { using var image = new Bitmap(surface.Width, surface.Height); surface.DrawToBitmap(image, surface.ClientRectangle); }
        Paint(); var before = record.Badge;
        ribbon.Theme = ribbon.Theme with { TipAppearance = Authored }; Paint(); int S(int n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi); Assert.Equal(before.Left - S(16), record.Badge.Left); Assert.Equal(before.Width, record.Badge.Width);
        using var tip = new RibbonScreenTipWindow(ribbon, item, new(200, 200, 100, 30), new(210, 210)); tip.Show(form); using var pixels = new Bitmap(tip.Width, tip.Height); tip.DrawToBitmap(pixels, tip.ClientRectangle);
        Assert.True(record.TipCalls > 0); Assert.False(record.TitleBold); Assert.True(editor.Focused); Assert.Equal(handle, editor.Handle); Assert.Same(parent, body.Parent); Assert.Equal(2, editor.SelectionStart); Assert.Equal(5, editor.SelectionLength); Assert.Equal("Retained editor draft", editor.Text); Assert.Same(page, ribbon.Backstage.SelectedPage);
        tip.Dispose(); ribbon.Theme = ribbon.Theme with { TipAppearance = null }; Paint(); Assert.Equal(before, record.Badge); Assert.Equal(handle, editor.Handle); Assert.Same(parent, body.Parent);
    });
    private sealed class Replacement : RibbonOffice2024Renderer
    {
        internal int Calls;
        public override void DrawKeyTip(RibbonPaintContext c, string keys) => Calls++;
        public override void DrawScreenTip(RibbonPaintContext c, string title, string description, Rectangle t, Rectangle d) => Calls++;
    }
    private sealed class NativeRecord { internal Rectangle Badge; internal bool TitleBold; internal int TipCalls; }
    private sealed class NativeOffice(NativeRecord r) : RibbonOffice2024Renderer
    {
        public override void DrawKeyTip(RibbonPaintContext c, string keys) { if (keys == "AB") r.Badge = c.Bounds; base.DrawKeyTip(c, keys); }
        public override void DrawScreenTip(RibbonPaintContext c, string t, string d, Rectangle title, Rectangle description) { r.TipCalls++; base.DrawScreenTip(c, t, d, title, description); }
        public override void DrawText(RibbonPaintContext c, string t, Color color, TextFormatFlags flags) { if (t == "Search document") r.TitleBold = c.Font.Bold; base.DrawText(c, t, color, flags); }
    }
    private sealed class NativeFlat(NativeRecord r) : RibbonRenderer
    {
        public override void DrawKeyTip(RibbonPaintContext c, string keys) { if (keys == "AB") r.Badge = c.Bounds; base.DrawKeyTip(c, keys); }
        public override void DrawScreenTip(RibbonPaintContext c, string t, string d, Rectangle title, Rectangle description) { r.TipCalls++; base.DrawScreenTip(c, t, d, title, description); }
        public override void DrawText(RibbonPaintContext c, string t, Color color, TextFormatFlags flags) { if (t == "Search document") r.TitleBold = c.Font.Bold; base.DrawText(c, t, color, flags); }
    }
    [Fact]
    public void Default_tip_pixels_text_slots_and_measurement_match_saved_results() => FoundationTests.Sta(() =>
    {
        foreach (bool old in new[] { true, false })
        foreach (bool office in new[] { false, true })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi); var log = new List<string>();
            using RibbonRenderer painter = office ? new Office(log) : new Flat(log);
            var theme = (old ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { TipAppearance = Environment.GetEnvironmentVariable("RIBBONKIT_TIP_APPEARANCE_EMPTY") == "1" ? new() : null };
            using var image = new Bitmap(S(740), S(360)); using var g = Graphics.FromImage(image); g.Clear(theme.Surface);
            foreach (int points in new[] { 9, 14 })
            {
                using var font = new Font("Segoe UI", points * dpi / 96f, FontStyle.Italic); int top = points == 9 ? S(6) : S(175);
                const string title = "Find the matching document style"; const string description = "Search the current document.\r\nKeep the full command description.";
                var layout = RibbonScreenTipLayout.Measure(title, description, font, dpi, new(S(600), S(160)), theme);
                var placed = RibbonScreenTipLayout.Place(new(50, 30, 90, 30), new(70, 35), layout.Size, new(-100, -100, S(900), S(700)), dpi, theme);
                log.Add($"layout:{points}/{layout}/{placed}");
                var state = g.Save(); g.TranslateTransform(0, top);
                var c = new RibbonPaintContext(g, new(Point.Empty, layout.Size), theme, font, dpi) { Density = density };
                painter.DrawPopupFrame(c); painter.DrawScreenTip(c, title, description, layout.Title, layout.Description); g.Restore(state);
                foreach (string keys in new[] { "AB", "", "Z" })
                    painter.DrawKeyTip(new(g, new(S(400 + keys.Length * 90), top + S(8), S(70), S(40)), theme, font, dpi) { Density = density }, keys);
                var metrics = new RibbonTipMetrics(theme); log.Add($"badge:{metrics.BadgeSize(new(37, 21), dpi)}/{metrics.BadgeSize(new(37, 21), dpi, true)}");
            }
            using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); string name = $"tips-{old}-{office}-{dpi}-{density}"; var data = string.Join(Environment.NewLine, log);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_TIP_APPEARANCE_CAPTURE") is { Length: > 0 } output) { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), stream.ToArray()); File.WriteAllText(Path.Combine(output, name + ".txt"), data); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_TIP_APPEARANCE_BASELINE") is { Length: > 0 } baseline) { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), stream.ToArray()); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), data); }
        }
    });
    private sealed class Office(List<string> log) : RibbonOffice2024Renderer
    { public override void DrawText(RibbonPaintContext c, string text, Color ink, TextFormatFlags flags) { log.Add($"text:{text}/{c.Bounds}/{c.Font.Style}/{ink.ToArgb()}/{flags}"); base.DrawText(c, text, ink, flags); } }
    private sealed class Flat(List<string> log) : RibbonRenderer
    { public override void DrawText(RibbonPaintContext c, string text, Color ink, TextFormatFlags flags) { log.Add($"text:{text}/{c.Bounds}/{c.Font.Style}/{ink.ToArgb()}/{flags}"); base.DrawText(c, text, ink, flags); } }
}
