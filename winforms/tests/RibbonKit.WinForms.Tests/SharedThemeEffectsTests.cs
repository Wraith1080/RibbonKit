using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class SharedThemeEffectsTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Header_border_scroller_and_contextual_ink_overrides_preserve_targets_and_body_scrollers(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new Hooks(); using var tab = new RibbonTab { IsContextual = true, ContextualColor = Color.FromArgb(128, 210, 20, 150) };
        foreach (var family in new[] { RibbonThemes.Office2019Light, RibbonThemes.Office2024Light })
        {
            var settings = new RibbonHeaderAppearanceSettings { FilledBorderColor = Color.Magenta, FilledBorderWidth = 3,
                ScrollerCornerRadius = 0, ScrollerBorderWidth = 3, ContextualUnselectedOpacity = .25 };
            var theme = family with { HeaderPresentation = RibbonHeaderPresentation.Filled, HeaderAppearance = settings, HeaderFilledCornerRadius = 0,
                HeaderScrollButtonBackground = new(Color.LightBlue), HeaderScrollButtonBorder = Color.DarkOrange, Chevrons = new() { StrokeWidth = 0 } };
            using var image = new Bitmap(S(250), S(110)); using var graphics = Graphics.FromImage(image);
            var bounds = new Rectangle(S(8), S(8), S(200), S(44)); var context = new RibbonPaintContext(graphics, bounds, theme, Control.DefaultFont, dpi);
            foreach (int state in new[] { 0, 1, 2 })
            {
                graphics.Clear(Color.Beige); painter.DrawTab(context with { Hot = state == 1, Pressed = state == 2 }, tab, state == 0);
                Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(bounds.Left, bounds.Top + bounds.Height / 2).ToArgb());
                Assert.Equal(state == 0 ? RibbonRenderer.ContextualTabColor(context, tab, true) : RibbonRenderer.ContextualTabColor(context, tab, false), painter.Ink);
            }
            var color = tab.ContextualColor; double opacity = color.A / 255d * .25;
            Assert.Equal(Color.FromArgb((int)Math.Round(color.R * opacity + theme.Background.R * (1 - opacity)),
                (int)Math.Round(color.G * opacity + theme.Background.G * (1 - opacity)), (int)Math.Round(color.B * opacity + theme.Background.B * (1 - opacity))), painter.Ink);
            painter.DrawTab(context with { Theme = theme with { HeaderSelectedForeground = Color.Lime } }, tab, true); Assert.Equal(Color.Lime, painter.Ink);
            painter.DrawScrollButton(context, false); Assert.Equal(Color.DarkOrange.ToArgb(), image.GetPixel(bounds.Left, bounds.Top + bounds.Height / 2).ToArgb());
            graphics.Clear(Color.Beige); painter.DrawScrollButton(context with { Theme = theme with { HeaderAppearance = settings with { ScrollerCornerRadius = 12 } } }, false);
            Assert.Equal(Color.Beige.ToArgb(), image.GetPixel(bounds.Left, bounds.Top).ToArgb());
            byte[] Body(RibbonTheme value)
            {
                graphics.Clear(Color.Beige); painter.DrawScrollButton(context with { Theme = value, IsBodyScrollButton = true }, true);
                using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); return stream.ToArray();
            }
            Assert.Equal(Body(theme with { HeaderAppearance = null }), Body(theme));
            using var ribbon = new RibbonControl { Theme = family, Width = 450 };
            ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); var before = ribbon.Snapshot.Targets.Select(target => (target.Bounds, target.Clip)).ToArray();
            ribbon.Theme = theme; Assert.Equal(before, ribbon.Snapshot.Targets.Select(target => (target.Bounds, target.Clip)).ToArray());
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Shared_chevrons_consume_size_stroke_hide_and_short_slot_clips(int dpi) => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonRenderer(); using var image = new Bitmap(120, 120); using var graphics = Graphics.FromImage(image);
        var slot = new Rectangle(15, 15, 80, 80); var theme = RibbonThemes.Office2024Light;
        // Count only inside the utility slot; the shared painter owns no target layout.
        graphics.Clear(Color.Beige); painter.DrawSubmenuArrow(new(graphics, slot, theme, Control.DefaultFont, dpi), Color.DarkRed); var normal = Ink(image, color => color.ToArgb() != Color.Beige.ToArgb(), slot);
        graphics.Clear(Color.Beige); painter.DrawSubmenuArrow(new(graphics, slot, theme with { Chevrons = new() { Length = 20, Depth = 12, StrokeWidth = 2 } }, Control.DefaultFont, dpi), Color.DarkRed);
        var larger = Ink(image, color => color.ToArgb() != Color.Beige.ToArgb(), slot); Assert.True(larger.Width > normal.Width && larger.Height > normal.Height);
        foreach (var settings in new[] { new RibbonChevronSettings { Length = 0 }, new RibbonChevronSettings { Depth = 0 }, new RibbonChevronSettings { StrokeWidth = 0 } })
        {
            graphics.Clear(Color.Beige); painter.DrawSubmenuArrow(new(graphics, slot, theme with { Chevrons = settings }, Control.DefaultFont, dpi), Color.DarkRed);
            Assert.True(Ink(image, color => color.ToArgb() != Color.Beige.ToArgb(), slot).IsEmpty);
        }
        var tiny = new Rectangle(6, 6, 4, 3); graphics.Clear(Color.Beige);
        painter.DrawSubmenuArrow(new(graphics, tiny, theme with { Chevrons = new() { Length = 64, Depth = 64, StrokeWidth = 32 } }, Control.DefaultFont, dpi), Color.DarkRed);
        Assert.True(Ink(image, color => color.ToArgb() != Color.Beige.ToArgb(), new(Point.Empty, image.Size)).IsEmpty);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Focus_overrides_stay_visible_clipped_and_keep_public_hooks(int dpi) => FoundationTests.Sta(() =>
    {
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(150, 90); using var graphics = Graphics.FromImage(image);
            foreach (int inset in new[] { 4, 1024 })
            {
                var theme = RibbonThemes.Office2024Light with { FocusInset = inset, FocusStrokeWidth = 2, FocusCornerRadius = 0 };
                var context = new RibbonPaintContext(graphics, new(10, 10, 120, 60), theme, Control.DefaultFont, dpi);
                graphics.Clear(Color.Beige); painter.DrawFocus(context);
                var ink = Ink(image, color => color.ToArgb() != Color.Beige.ToArgb(), new(Point.Empty, image.Size));
                Assert.False(ink.IsEmpty); Assert.True(context.Bounds.Contains(ink));
                Assert.True(RibbonSharedEffectMetrics.FocusBounds(context).Width >= 2);
            }
        }
        using var hooks = new Hooks(); using var tab = new RibbonTab(); using var frame = new Bitmap(200, 60); using var g = Graphics.FromImage(frame);
        hooks.DrawTab(new(g, new(0, 0, 190, 52), RibbonThemes.Office2019Light with { FocusInset = 4, FocusStrokeWidth = 3 }, Control.DefaultFont, dpi, Focused: true), tab, true);
        Assert.Equal(1, hooks.FocusCalls);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Open_File_highlight_uses_fill_ink_inset_and_independent_radius(bool classic) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Bounds = new(-16000, -16000, 900, 600), ShowInTaskbar = false };
        using var painter = new Hooks(); using var ribbon = new RibbonControl { Renderer = painter, Theme = classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light, FileMenuMode = RibbonFileMenuMode.CommandMenu, AnimationLevel = RibbonAnimationLevel.None };
        ribbon.FileMenu.Visible = true; ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Open" }); ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); form.Controls.Add(ribbon); form.Show(); Application.DoEvents(); ribbon.FileMenu.ShowDropDown();
        Assert.True(ribbon.FileMenu.IsDropDownOpen);
        using var image = new Bitmap(150, 70); using var graphics = Graphics.FromImage(image);
        var theme = ribbon.Theme with { ApplicationMenuFileButtonCornerRadius = 0, HeaderAppearance = new() { FileHighlightInset = 7, FileHighlightBackground = new(Color.Navy), FileHighlightForeground = Color.Yellow } };
        var context = new RibbonPaintContext(graphics, new(4, 4, 130, 55), theme, Control.DefaultFont, 120);
        graphics.Clear(Color.Beige); painter.DrawFileButton(context, ribbon.FileMenu);
        Assert.Equal(Color.Yellow, painter.Ink); Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(20, 20).ToArgb()); Assert.Equal(Color.Beige.ToArgb(), image.GetPixel(5, 5).ToArgb());
        Assert.Equal(Rectangle.Inflate(context.Bounds, -9, -9), painter.TextBounds);
        painter.DrawFileButton(context with { Theme = theme with { HeaderAppearance = theme.HeaderAppearance with { FileHighlightForeground = Color.Empty } } }, ribbon.FileMenu); Assert.Equal(Color.White, painter.Ink);
        painter.DrawFileButton(context with { Theme = theme with { HeaderAppearance = theme.HeaderAppearance with { FileHighlightBackground = new(Color.White), FileHighlightForeground = Color.Empty } } }, ribbon.FileMenu); Assert.Equal(Color.Black, painter.Ink);
        ribbon.FileMenu.CloseDropDown();
    });

    [Theory]
    [InlineData("header", "FilledBorderWidth", 32)] [InlineData("header", "ScrollerCornerRadius", 64)] [InlineData("header", "ScrollerBorderWidth", 32)] [InlineData("header", "FileHighlightInset", 1024)] [InlineData("header", "ContextualUnselectedOpacity", 1)]
    [InlineData("chevron", "Length", 64)] [InlineData("chevron", "Depth", 64)] [InlineData("chevron", "StrokeWidth", 32)]
    [InlineData("theme", "FocusInset", 1024)] [InlineData("theme", "FocusStrokeWidth", 32)] [InlineData("theme", "TabContentSlideDistance", 1024)] [InlineData("theme", "HeaderIndicatorSpringAmount", 1)]
    public void Invalid_effect_values_leave_theme_and_builtin_selection_intact(string group, string member, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme;
        var type = group == "header" ? typeof(RibbonHeaderAppearanceSettings) : group == "chevron" ? typeof(RibbonChevronSettings) : typeof(RibbonTheme);
        var property = type.GetProperty(member)!;
        object[] invalid = property.PropertyType == typeof(double?) ? [double.NaN, double.PositiveInfinity, double.NegativeInfinity, -.1, maximum + .1] : [member == "FocusStrokeWidth" ? 0 : -1, maximum + 1];
        foreach (var value in invalid)
        {
            var header = new RibbonHeaderAppearanceSettings(); var chevron = new RibbonChevronSettings(); var candidate = original with { HeaderAppearance = header, Chevrons = chevron };
            property.SetValue(group == "header" ? header : group == "chevron" ? chevron : candidate, value);
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = candidate); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme);
        }
        foreach (object? value in new object?[] { property.PropertyType == typeof(double?) ? (object)(double)maximum : maximum, null })
        {
            var header = new RibbonHeaderAppearanceSettings(); var chevron = new RibbonChevronSettings(); var candidate = original with { HeaderAppearance = header, Chevrons = chevron };
            property.SetValue(group == "header" ? header : group == "chevron" ? chevron : candidate, value); ribbon.Theme = candidate; Assert.Same(candidate, ribbon.Theme);
        }
    });

    [Fact]
    public void Groups_reset_independently_and_application_theme_is_retained() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl();
        var header = new RibbonHeaderAppearanceSettings { FilledBorderColor = Color.Red, FilledBorderWidth = 3, ContextualUnselectedOpacity = .8 };
        var application = ribbon.Theme with { HeaderAppearance = header, Chevrons = new() { Length = 18 }, FocusInset = 5, TabContentSlideDistance = 20 };
        ribbon.Theme = application; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = null; Assert.Same(application, ribbon.Theme);
        ribbon.Theme = application with { HeaderAppearance = header with { FilledBorderWidth = null } }; Assert.Equal(Color.Red, ribbon.Theme.HeaderAppearance!.FilledBorderColor); Assert.Equal(3, header.FilledBorderWidth);
        ribbon.Theme = application with { HeaderAppearance = null }; Assert.Equal(18, ribbon.Theme.Chevrons!.Length); Assert.Equal(5, ribbon.Theme.FocusInset);
        Assert.Null(other.Theme.HeaderAppearance); Assert.Null(other.Theme.Chevrons);
    });

    private static Rectangle Ink(Bitmap image, Func<Color, bool> match, Rectangle bounds)
    {
        var result = Rectangle.Empty;
        for (int y = bounds.Top; y < bounds.Bottom; y++) for (int x = bounds.Left; x < bounds.Right; x++)
            if (match(image.GetPixel(x, y))) result = result.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(result, new(x, y, 1, 1));
        return result;
    }
    private sealed class Hooks : RibbonOffice2024Renderer
    {
        internal Color Ink;
        internal Rectangle TextBounds;
        internal int FocusCalls;
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { Ink = color; TextBounds = context.Bounds; }
        public override void DrawFocus(RibbonPaintContext context) { FocusCalls++; base.DrawFocus(context); }
    }

    [Fact]
    public void Default_shared_effects_keep_saved_pixels() => FoundationTests.Sta(() =>
    {
        foreach (bool classic in new[] { false, true })
        foreach (bool office in new[] { false, true })
        foreach (bool composited in new[] { false, true })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (bool emptyGroups in new[] { false, true })
        {
            var theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with
                { HeaderAppearance = emptyGroups ? new() : null, Chevrons = emptyGroups ? new() : null };
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(420, 500); using var graphics = Graphics.FromImage(image); graphics.Clear(theme.Background);
            var context = new RibbonPaintContext(graphics, new(8, 8, 60, 50), theme, Control.DefaultFont, dpi) { CompositedText = composited };
            for (int state = 0; state < 3; state++)
            {
                var row = context with { Bounds = new(8, 8 + state * 65, 60, 50), Hot = state == 1, Pressed = state == 2 };
                painter.DrawScrollButton(row, true); painter.DrawScrollButton(row with { Bounds = new(78, row.Bounds.Y, 60, 50), IsBodyScrollButton = true }, false);
                painter.DrawScrollButton(row with { Bounds = new(148, row.Bounds.Y, 60, 50), Theme = theme with { HeaderScrollButtonBackground = new(Color.Beige) } }, false);
                painter.DrawFocus(row with { Bounds = new(225, row.Bounds.Y, 160, 50) });
            }
            painter.DrawOverflowScrollButton(context with { Bounds = new(8, 215, 60, 48) }, true, true);
            painter.DrawGalleryStripButton(context with { Bounds = new(78, 215, 60, 48) }, RibbonGalleryStripButton.Down, true);
            painter.DrawSubmenuArrow(context with { Bounds = new(148, 215, 60, 48) }, theme.Foreground);
            using var tab = new RibbonTab { Text = "Context", IsContextual = true, ContextualColor = Color.FromArgb(180, 180, 30, 160) };
            painter.DrawTab(context with { Bounds = new(8, 285, 180, 50), Focused = true }, tab, false);
            painter.DrawTab(context with { Bounds = new(215, 285, 180, 50), Focused = true }, tab, true);
            using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); var png = stream.ToArray(); string name = $"shared-{classic}-{office}-{composited}-{dpi}.png";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_SHARED_EFFECT_CAPTURE") is { Length: > 0 } output)
            { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name), png); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_SHARED_EFFECT_BASELINE") is { Length: > 0 } baseline)
                Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name)), png);
        }
    });
}
