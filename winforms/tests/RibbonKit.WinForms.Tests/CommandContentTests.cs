using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class CommandContentTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Indicator_overrides_honor_alpha_hide_clipping_and_disabled_policy(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var bitmap = new Bitmap(S(60), S(60)); using var g = Graphics.FromImage(bitmap);
            var slot = new Rectangle(S(10), S(10), S(16) + 1, S(16) + 1);
            var settings = new RibbonCommandContentSettings { IndicatorBorderWidth = 2, IndicatorBorderColor = Color.Magenta, CheckMarkColor = Color.FromArgb(128, 0, 0, 255), CheckMarkStrokeWidth = 2, CheckMarkScale = .7, RadioDotInset = 5, RadioDotColor = Color.Magenta };
            var theme = RibbonThemes.Office2024Light with { CommandContent = settings, Accent = Color.Transparent, DisabledForeground = Color.Transparent, Surface = Color.Transparent };
            var c = new RibbonPaintContext(g, slot, theme, Control.DefaultFont, dpi, Hot: true, Pressed: true, Focused: true);
            using var box = new RibbonCheckBoxItem { Checked = true, Enabled = false }; using var radio = new RibbonRadioButtonItem { Checked = true, Enabled = false };
            g.Clear(Color.Transparent); painter.DrawCheckBoxIndicator(c, box);
            Assert.Equal(Color.Magenta.ToArgb(), bitmap.GetPixel(slot.Left, slot.Top + slot.Height / 2).ToArgb());
            using var disabled = (Bitmap)bitmap.Clone(); g.Clear(Color.Transparent); painter.DrawCheckBoxIndicator(c with { Hot = false, Pressed = false, Focused = false }, box); Same(disabled, bitmap);
            foreach (var mark in new[] { CheckState.Checked, CheckState.Indeterminate })
            {
                box.CheckState = mark;
                g.Clear(Color.Transparent); painter.DrawCheckBoxIndicator(c with { Theme = theme with { CommandContent = settings with { IndicatorBorderWidth = 0 } } }, box);
                var ink = Ink(bitmap); Assert.False(ink.IsEmpty); Assert.True(slot.Contains(ink));
                Assert.InRange(bitmap.GetPixel(ink.Left + ink.Width / 2, ink.Top + ink.Height / 2).A, 1, 128);
                g.Clear(Color.Transparent); painter.DrawCheckBoxIndicator(c with { Theme = theme with { CommandContent = settings with { IndicatorBorderWidth = 0, CheckMarkScale = 0 } } }, box); Assert.True(Ink(bitmap).IsEmpty);
                g.Clear(Color.Transparent); painter.DrawCheckBoxIndicator(c with { Theme = theme with { CommandContent = settings with { IndicatorBorderWidth = 0, CheckMarkStrokeWidth = 0 } } }, box); Assert.True(Ink(bitmap).IsEmpty);
                g.Clear(Color.Transparent); painter.DrawCheckBoxIndicator(c with { Theme = theme with { CommandContent = settings with { IndicatorBorderWidth = 0, CheckMarkStrokeWidth = 32, CheckMarkScale = 4 } } }, box); Assert.True(slot.Contains(Ink(bitmap)));
            }
            g.Clear(Color.Transparent); painter.DrawRadioButtonIndicator(c with { Theme = theme with { CommandContent = settings with { IndicatorBorderWidth = 0 } } }, radio);
            var dot = Ink(bitmap); Assert.False(dot.IsEmpty); Assert.InRange(Math.Abs((dot.Left + dot.Right - 1) / 2d - (slot.Left + slot.Right - 1) / 2d), 0, .5);
            Assert.InRange(Math.Abs((dot.Top + dot.Bottom - 1) / 2d - (slot.Top + slot.Bottom - 1) / 2d), 0, .5);
            foreach (int inset in new[] { 0, 1024 })
            {
                g.Clear(Color.Transparent); painter.DrawRadioButtonIndicator(c with { Theme = theme with { CommandContent = settings with { IndicatorBorderWidth = 0, RadioDotInset = inset } } }, radio);
                Assert.Equal(inset == 1024, Ink(bitmap).IsEmpty); if (inset == 0) Assert.True(slot.Contains(Ink(bitmap)));
            }
            Assert.True(box.Checked); Assert.True(radio.Checked); Assert.False(box.Enabled);
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Triangle_dimensions_and_quick_access_mixed_mark_are_independent_and_clipped(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new RibbonRenderer(); using var bitmap = new Bitmap(S(100), S(70)); using var g = Graphics.FromImage(bitmap);
        var slot = new Rectangle(S(10), S(10), S(30), S(30));
        var settings = new RibbonCommandContentSettings { DropDownGlyphWidth = 16, DropDownGlyphHeight = 10, CheckMarkColor = Color.Magenta, CheckMarkScale = 2, CheckMarkStrokeWidth = 3 };
        var theme = RibbonThemes.Office2024Light with { CommandContent = settings, Checked = new(Color.Transparent) };
        var c = new RibbonPaintContext(g, slot, theme, Control.DefaultFont, dpi);
        g.Clear(Color.Transparent); painter.DrawDropDownArrow(c, Color.Blue);
        var ink = Ink(bitmap); Assert.Equal(S(16), ink.Width); Assert.Equal(S(10), ink.Height); Assert.True(slot.Contains(ink));
        foreach (var value in new[] { settings with { DropDownGlyphWidth = 0 }, settings with { DropDownGlyphHeight = 0 }, settings with { DropDownGlyphWidth = 64, DropDownGlyphHeight = 64 } })
        {
            g.Clear(Color.Transparent); painter.DrawDropDownArrow(c with { Theme = theme with { CommandContent = value } }, Color.Blue);
            if (value.DropDownGlyphWidth == 0 || value.DropDownGlyphHeight == 0) Assert.True(Ink(bitmap).IsEmpty); else Assert.True(slot.Contains(Ink(bitmap)));
        }
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup();
        var box = new RibbonCheckBoxItem { CheckState = CheckState.Indeterminate }; group.Items.Add(box); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var reference = new RibbonQuickAccessReference { Source = box }; ribbon.QuickAccessItems.Add(reference);
        // Suppress the fallback caption to isolate the mixed-state bar.
        using var quiet = new ContentHooks();
        foreach (bool menu in new[] { false, true })
        {
            g.Clear(Color.Transparent); quiet.DrawQuickAccessItem(c, reference, menu); ink = Ink(bitmap);
            Assert.Equal(S(12), ink.Width); Assert.Equal(S(3), ink.Height); Assert.True(slot.Contains(ink));
            g.Clear(Color.Transparent); quiet.DrawQuickAccessItem(c with { Theme = theme with { CommandContent = settings with { CheckMarkScale = 0 } } }, reference, menu); Assert.True(Ink(bitmap).IsEmpty);
        }
        Assert.Equal(CheckState.Indeterminate, box.CheckState);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Disabled_raster_opacity_covers_source_and_independent_qat_without_changing_vectors(int dpi) => FoundationTests.Sta(() =>
    {
        using var painter = new ContentHooks(); using var raster = new Bitmap(16, 16); using (var rg = Graphics.FromImage(raster)) rg.Clear(Color.Red);
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup(); var item = new RibbonButtonItem { Image = raster };
        group.Items.Add(item); tab.Groups.Add(group); ribbon.Tabs.Add(tab); var reference = new RibbonQuickAccessReference { Source = item }; ribbon.QuickAccessItems.Add(reference);
        using var bitmap = new Bitmap(100, 80); using var g = Graphics.FromImage(bitmap); var slot = new Rectangle(10, 10, 60, 40);
        foreach (double opacity in new[] { 0d, .25, 1d })
        foreach (bool sourceEnabled in new[] { false, true })
        {
            item.Enabled = sourceEnabled; reference.Enabled = false;
            var c = new RibbonPaintContext(g, slot, ribbon.Theme with { CommandContent = new() { DisabledImageOpacity = opacity } }, ribbon.Font, dpi);
            g.Clear(Color.Transparent); painter.DrawQuickAccessItem(c, reference, false); var pixel = bitmap.GetPixel(slot.Left + slot.Width / 2, slot.Top + slot.Height / 2);
            Assert.InRange(pixel.A, (int)(opacity * 255) - 1, (int)(opacity * 255) + 1);
            if (opacity > 0) { Assert.InRange(Math.Abs(pixel.R - pixel.G), 0, 1); Assert.InRange(Math.Abs(pixel.G - pixel.B), 0, 1); }
            item.Enabled = false; g.Clear(Color.Transparent); painter.Image(c, item); pixel = bitmap.GetPixel(30, 30); Assert.InRange(pixel.A, (int)(opacity * 255) - 1, (int)(opacity * 255) + 1);
            item.Enabled = true; g.Clear(Color.Transparent); painter.Image(c, item); Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(30, 30).ToArgb());
        }
        var vector = new IconProbe(); item.Icon = vector; item.Enabled = false;
        painter.Image(new(g, slot, ribbon.Theme with { CommandContent = new() { DisabledImageOpacity = 0 } }, ribbon.Font, dpi), item);
        Assert.Equal(1, vector.Calls); Assert.False(vector.Enabled); Assert.Equal(16, raster.Width);
        item.Icon = null;
        foreach (double gray in new[] { 0d, .5, 1d })
        foreach (bool sourceEnabled in new[] { false, true })
        {
            item.Enabled = sourceEnabled; reference.Enabled = false;
            var c = new RibbonPaintContext(g, slot, ribbon.Theme with { CommandContent = new() { DisabledImageGrayscaleAmount = gray, DisabledImageOpacity = 1 } }, ribbon.Font, dpi);
            g.Clear(Color.Transparent); painter.DrawQuickAccessItem(c, reference, false);
            var pixel = bitmap.GetPixel(slot.Left + slot.Width / 2, slot.Top + slot.Height / 2);
            Assert.InRange(pixel.R - pixel.G, (int)Math.Round(255 * (1 - gray)) - 2, (int)Math.Round(255 * (1 - gray)) + 2);
            item.Enabled = false; g.Clear(Color.Transparent); painter.Image(c, item); Assert.Equal(pixel.ToArgb(), bitmap.GetPixel(30, 30).ToArgb());
        }
    });

    [Theory] [InlineData("IndicatorBorderWidth", 32)] [InlineData("RadioDotInset", 1024)] [InlineData("DropDownGlyphWidth", 64)] [InlineData("DropDownGlyphHeight", 64)]
    [InlineData("CheckMarkStrokeWidth", 32)] [InlineData("CheckMarkScale", 4)] [InlineData("DisabledImageOpacity", 1)]
    [InlineData("DisabledImageGrayscaleAmount", 1)]
    public void Invalid_content_roles_are_rejected_before_theme_or_builtin_mutation(string name, double max) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme;
        var property = typeof(RibbonCommandContentSettings).GetProperty(name)!; bool integer = property.PropertyType == typeof(int?);
        foreach (double n in integer ? new[] { -1d, max + 1 } : new[] { -1d, max + 1, double.NaN, double.NegativeInfinity, double.PositiveInfinity })
        {
            var value = new RibbonCommandContentSettings(); property.SetValue(value, integer ? (object)(int)n : n);
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { CommandContent = value }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme);
        }
        foreach (double? n in new double?[] { 0, max, null })
        { var value = new RibbonCommandContentSettings(); property.SetValue(value, n is null ? null : integer ? (object)(int)n.Value : n.Value); ribbon.Theme = original with { CommandContent = value }; Assert.Same(value, ribbon.Theme.CommandContent); }
    });

    [Fact]
    public void Public_hooks_and_independent_immutable_resets_remain_authoritative() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); using var painter = new ContentHooks();
        var value = new RibbonCommandContentSettings { CheckMarkScale = .7, DropDownGlyphWidth = 12, DisabledImageOpacity = .2 };
        var application = ribbon.Theme with { CommandContent = value, CommandAppearance = new() { CornerRadius = 8 } }; ribbon.Theme = application;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = null; Assert.Same(application, ribbon.Theme); Assert.Null(other.Theme.CommandContent);
        ribbon.Theme = application with { CommandContent = value with { CheckMarkScale = null } }; Assert.Equal(12, ribbon.Theme.CommandContent!.DropDownGlyphWidth); Assert.Equal(.7, value.CheckMarkScale);
        ribbon.Theme = application with { CommandContent = null }; Assert.Equal(8, ribbon.Theme.CommandAppearance!.CornerRadius);
        using var bitmap = new Bitmap(240, 70); using var g = Graphics.FromImage(bitmap); var c = new RibbonPaintContext(g, new(5, 5, 220, 55), application, ribbon.Font, 120, Focused: true);
        using var box = new RibbonCheckBoxItem { Text = "Box", Checked = true }; using var radio = new RibbonRadioButtonItem { Text = "Radio", Checked = true }; using var drop = new RibbonDropDownItem { Text = "Menu" };
        painter.DrawItem(c, box, RibbonItemSize.Medium, false); painter.DrawItem(c, radio, RibbonItemSize.Medium, true); painter.DrawItem(c, drop, RibbonItemSize.Medium, false);
        Assert.Equal(1, painter.Boxes); Assert.Equal(1, painter.Radios); Assert.Equal(1, painter.Arrows); Assert.True(painter.Focus > 0); Assert.True(painter.Text > 0);
        using var raster = new Bitmap(16, 16); using var imageItem = new RibbonButtonItem { Image = raster, Enabled = false };
        painter.ReplaceImages = true; painter.Images = 0; g.Clear(Color.Transparent);
        painter.DrawItem(c with { Theme = application with { CommandContent = value with { DisabledImageOpacity = 0 } } }, imageItem, RibbonItemSize.Medium, false);
        Assert.Equal(1, painter.Images); Assert.False(Ink(bitmap).IsEmpty);
    });

    private static Rectangle Ink(Bitmap image)
    {
        var result = Rectangle.Empty;
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).A > 0) result = result.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(result, new(x, y, 1, 1));
        return result;
    }
    private static void Same(Bitmap a, Bitmap b) { for (int y = 0; y < a.Height; y++) for (int x = 0; x < a.Width; x++) Assert.Equal(a.GetPixel(x, y), b.GetPixel(x, y)); }
    private sealed class ContentHooks : RibbonOffice2024Renderer
    {
        internal int Boxes, Radios, Arrows, Focus, Text, Images;
        internal bool ReplaceImages;
        internal void Image(RibbonPaintContext c, RibbonItem item) => base.DrawItemImage(c, item, RibbonItemSize.Small);
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size)
        {
            Images++;
            if (ReplaceImages) c.Graphics.FillRectangle(Brushes.Magenta, c.Bounds);
            else base.DrawItemImage(c, item, size);
        }
        public override void DrawCheckBoxIndicator(RibbonPaintContext c, RibbonCheckBoxItem item) => Boxes++;
        public override void DrawRadioButtonIndicator(RibbonPaintContext c, RibbonRadioButtonItem item) => Radios++;
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) => Arrows++;
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) => Text++;
        public override void DrawFocus(RibbonPaintContext c) => Focus++;
    }
    private sealed class IconProbe : IRibbonIcon
    {
        internal int Calls; internal bool Enabled;
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { Calls++; Enabled = context.Enabled; }
    }

    [Fact]
    public void Default_indicators_arrows_and_disabled_images_match_saved_pixels_and_geometry() => FoundationTests.Sta(() =>
    {
        foreach (bool classic in new[] { false, true })
        foreach (bool office in new[] { false, true })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (bool empty in new[] { false, true })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { CommandContent = empty ? new() : null };
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(700), S(230)); using var g = Graphics.FromImage(image); g.Clear(theme.Surface);
            using var box = new RibbonCheckBoxItem(); using var radio = new RibbonRadioButtonItem();
            using var raster = new Bitmap(16, 16); using (var rg = Graphics.FromImage(raster)) { rg.Clear(Color.OrangeRed); rg.FillRectangle(Brushes.Blue, 0, 0, 8, 8); }
            using var command = new RibbonButtonItem { Image = raster }; using var ribbon = new RibbonControl();
            var tab = new RibbonTab(); var group = new RibbonGroup(); group.Items.Add(command); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            var reference = new RibbonQuickAccessReference { Source = command }; ribbon.QuickAccessItems.Add(reference);
            int x = 0;
            foreach (bool enabled in new[] { true, false })
            foreach (var state in new[] { CheckState.Unchecked, CheckState.Checked, CheckState.Indeterminate })
            foreach (bool active in new[] { false, true })
            {
                box.Enabled = radio.Enabled = enabled; box.CheckState = state; radio.Checked = state != CheckState.Unchecked;
                var c = new RibbonPaintContext(g, new(S(5 + x * 28), S(6), S(16) + x % 2, S(16) + x % 2), theme, ribbon.Font, dpi, active, active, active);
                renderer.DrawCheckBoxIndicator(c, box); renderer.DrawRadioButtonIndicator(c with { Bounds = c.Bounds with { Y = S(34) } }, radio); x++;
            }
            foreach (int side in new[] { 1, 4, 8, 16, 24 })
                renderer.DrawDropDownArrow(new(g, new(S(side * 10), S(65), S(side), S(side)), theme, ribbon.Font, dpi), theme.Foreground);
            x = 0;
            foreach (bool sourceEnabled in new[] { false, true })
            foreach (bool entryEnabled in new[] { false, true })
            {
                command.Enabled = sourceEnabled; reference.Enabled = entryEnabled;
                var c = new RibbonPaintContext(g, new(S(5 + x++ * 100), S(100), S(80), S(40)), theme, ribbon.Font, dpi);
                renderer.DrawQuickAccessItem(c, reference, false);
                renderer.DrawItem(c with { Bounds = c.Bounds with { Y = S(150) } }, command, RibbonItemSize.Medium, false);
            }
            var geometry = new List<string>();
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.Density = density;
                var measured = new RibbonLayoutEngine().Measure(group, 0, ribbon.Font, dpi, theme);
                geometry.Add(measured.Width.ToString()); geometry.AddRange(measured.Items.Select(i => i.Bounds.ToString()));
            }
            using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); var png = stream.ToArray();
            string name = $"content-{classic}-{office}-{dpi}", layout = string.Join(Environment.NewLine, geometry);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_COMMAND_CONTENT_CAPTURE") is { Length: > 0 } output)
            { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), png); File.WriteAllText(Path.Combine(output, name + ".txt"), layout); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_COMMAND_CONTENT_BASELINE") is { Length: > 0 } baseline)
            { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), layout); }
        }
    });
}
