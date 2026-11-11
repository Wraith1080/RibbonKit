using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class HeaderPresentationTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Header_states_keep_saved_pixels_and_indicator_geometry(bool composited) => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonOffice2024Renderer();
        var themes = new[] { RibbonThemes.Office2024Light, RibbonThemes.Office2019Light,
            RibbonThemes.Office2019Light with { HeaderPresentation = RibbonHeaderPresentation.Underline, HeaderIndicatorPadding = 8, HeaderIndicatorCornerRadius = 0 },
            RibbonThemes.Office2024Light with { HeaderPresentation = RibbonHeaderPresentation.Filled, HeaderForeground = Color.Purple, HeaderHover = new(Color.Beige) } };
        for (int variant = 0; variant < themes.Length; variant++)
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            var theme = themes[variant]; int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            using var font = new Font("Segoe UI", 9 * dpi / 96f);
            using var image = new Bitmap(S(640), S(160)); using var graphics = Graphics.FromImage(image);
            var data = new List<string>();
            void Draw(Graphics g)
            {
                g.Clear(theme.Background);
                for (int state = 0; state < 8; state++)
                {
                    using var tab = new RibbonTab { Text = "Scrolled header", Enabled = state != 3, IsContextual = state == 2, ContextualColor = Color.Purple };
                    using var file = new RibbonDropDownItem { Text = "File", Enabled = state != 7 };
                    var bounds = new Rectangle(S(8 + state % 4 * 160), S(8 + state / 4 * 80), S(144), S(44));
                    var context = new RibbonPaintContext(g, bounds, theme, font, dpi, Hot: state is 1 or 5, Pressed: state == 6, Focused: state is 0 or 4)
                        { CompositedText = composited, WindowBackdrop = composited, SuppressTabIndicators = state == 3 };
                    var saved = g.Save();
                    try
                    {
                        // Include a scrolled label whose left side is outside its viewport.
                        if (state == 1) g.SetClip(new Rectangle(bounds.Left + S(40), bounds.Top, bounds.Width - S(40), bounds.Height));
                        if (state < 4) painter.DrawTab(context, tab, state is 0 or 3);
                        else painter.DrawFileButton(context, file);
                        data.Add($"{state}:{painter.GetTabIndicatorBounds(context, tab)}");
                    }
                    finally { g.Restore(saved); }
                }
            }
            if (composited)
            {
                using var buffer = new RibbonBackdropBuffer();
                buffer.Paint(graphics, image.Size, image.Height, theme.Background, Draw);
            }
            else Draw(graphics);
            Compare($"states-{variant}-{dpi}-{composited}", image, data);
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Header_strip_keeps_saved_File_QAT_scroll_and_colored_caption_geometry(bool classic) => FoundationTests.Sta(() =>
    {
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int width in new[] { 360, 1100 })
        foreach (bool colored in new[] { false, true })
        {
            using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, width, 600), ShowInTaskbar = false,
                TitleBarColor = colored ? Color.FromArgb(43, 87, 154) : Color.Empty };
            using var ribbon = new RibbonControl { Theme = classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light, Density = density,
                QuickAccessPosition = RibbonQuickAccessPosition.TabRow, QuickAccessMaxWidth = 80 };
            ribbon.FileMenu.Visible = true;
            for (int i = 0; i < 8; i++) ribbon.Tabs.Add(new RibbonTab { Text = "Document layout " + i });
            for (int i = 0; i < 4; i++) ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save " + i, Icon = RibbonStockIcons.Create("save") });
            host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.Scroll(true, 53);
            using var image = new Bitmap(ribbon.Width, ribbon.BackdropHeaderHeight); using var full = new Bitmap(ribbon.Width, ribbon.Height);
            ribbon.DrawToBitmap(full, ribbon.ClientRectangle);
            using (var g = Graphics.FromImage(image)) g.DrawImageUnscaled(full, Point.Empty);
            var snapshot = ribbon.Snapshot;
            var data = new List<string> { $"dpi={ribbon.DeviceDpi};viewport={snapshot.TabViewport};extent={snapshot.TabExtent};qat={snapshot.QuickAccessBounds}" };
            data.AddRange(snapshot.Targets.Where(t => t.Kind is TargetKind.Tab or TargetKind.FileMenu or TargetKind.TabBack or TargetKind.TabNext or TargetKind.QuickAccess or TargetKind.QuickAccessOverflow)
                .Select(t => $"{t.Kind}/{t.Element?.Text}:{t.Bounds}/{t.Clip}"));
            Compare($"strip-{classic}-{density}-{width}-{colored}", image, data);
        }
    });

    [Theory] [InlineData(RibbonHeaderPresentation.Underline)] [InlineData(RibbonHeaderPresentation.Filled)]
    public void Candidate_header_selection_retains_actual_renderer_text_focus_and_indicator_hooks(RibbonHeaderPresentation presentation) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = presentation == RibbonHeaderPresentation.Filled ? RibbonThemes.Office2024Light : RibbonThemes.Office2019Light };
        var candidate = ribbon.Theme with { HeaderPresentation = presentation, CornerRadius = 0, ExtendTitleBarColorToHeader = false };
        using var painter = new Hooks(); using var tab = new RibbonTab { Text = "Home" };
        using var image = new Bitmap(220, 70); using var g = Graphics.FromImage(image); g.Clear(Color.Beige);
        var context = new RibbonPaintContext(g, new(10, 10, 180, 44), candidate, Control.DefaultFont, 120, Focused: true);
        painter.DrawTab(context, tab, true);
        Assert.Same(candidate, painter.TextTheme); Assert.True(painter.TextFlags.HasFlag(TextFormatFlags.PreserveGraphicsClipping));
        if (presentation == RibbonHeaderPresentation.Underline)
        {
            Assert.Equal(1, painter.MeasureCalls); Assert.Equal(1, painter.IndicatorCalls);
            Assert.Equal(Hooks.Indicator, painter.IndicatorBounds); Assert.Equal(0, painter.FocusCalls);
        }
        else { Assert.Equal(0, painter.MeasureCalls); Assert.Equal(0, painter.IndicatorCalls); Assert.Equal(1, painter.FocusCalls); }
        painter.DrawFileButton(context, ribbon.FileMenu); Assert.Same(candidate, painter.TextTheme);
        Assert.NotSame(candidate, ribbon.Theme); // Candidate paint does not mutate the live selection.
        using var flat = new RibbonRenderer(); Assert.True(flat.GetTabIndicatorBounds(context, tab).IsEmpty);
    });

    private sealed class Hooks : RibbonOffice2024Renderer
    {
        internal static readonly Rectangle Indicator = new(20, 56, 31, 3);
        internal int MeasureCalls, IndicatorCalls, FocusCalls;
        internal Rectangle IndicatorBounds;
        internal RibbonTheme? TextTheme;
        internal TextFormatFlags TextFlags;
        public override Rectangle GetTabIndicatorBounds(RibbonPaintContext context, RibbonTab tab) { MeasureCalls++; return Indicator; }
        public override void DrawTabIndicator(RibbonPaintContext context, RibbonTab tab, bool selected) { IndicatorCalls++; IndicatorBounds = context.Bounds; base.DrawTabIndicator(context, tab, selected); }
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { TextTheme = context.Theme; TextFlags = flags; base.DrawText(context, text, color, flags); }
        public override void DrawFocus(RibbonPaintContext context) { FocusCalls++; base.DrawFocus(context); }
    }

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Authored_header_effects_change_pixels_without_changing_header_targets(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new InkRecorder(); using var tab = new RibbonTab { Text = "Format", IsContextual = true, ContextualColor = Color.Purple };
        using var image = new Bitmap(S(220), S(80)); using var graphics = Graphics.FromImage(image); using var font = new Font("Segoe UI", 9 * dpi / 96f);
        var theme = RibbonThemes.Office2024Light with { HeaderSelectedForeground = Color.Magenta, HeaderIndicatorHoverColor = Color.Lime,
            HeaderIndicatorThickness = 6, HeaderIndicatorBottomInset = 5, HeaderIndicatorCornerRadius = 0 };
        var context = new RibbonPaintContext(graphics, new(S(10), S(10), S(180), S(44)), theme, font, dpi);
        var indicator = painter.GetTabIndicatorBounds(context, tab);
        Assert.Equal(S(6), indicator.Height); Assert.Equal(context.Bounds.Bottom - S(5), indicator.Bottom); Assert.True(context.Bounds.Contains(indicator));
        graphics.Clear(Color.Beige); painter.DrawTab(context, tab, true);
        Assert.Equal(Color.Magenta, painter.Ink); Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(indicator.Left + indicator.Width / 2, indicator.Top + indicator.Height / 2).ToArgb());
        graphics.Clear(Color.Beige); painter.DrawTab(context with { Hot = true }, tab, false);
        Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(indicator.Left + indicator.Width / 2, indicator.Top + indicator.Height / 2).ToArgb());
        graphics.Clear(Color.Beige); var hidden = context with { Theme = theme with { HeaderIndicatorThickness = 0 } };
        Assert.True(painter.GetTabIndicatorBounds(hidden, tab).IsEmpty);
        painter.DrawTabIndicator(hidden with { Bounds = indicator }, tab, true);
        Assert.Equal(Color.Beige.ToArgb(), image.GetPixel(indicator.Left + indicator.Width / 2, indicator.Top + indicator.Height / 2).ToArgb());
        Assert.True(painter.GetTabIndicatorBounds(context with { Theme = theme with { HeaderIndicatorBottomInset = 1024 } }, tab).IsEmpty);

        var filled = theme with { HeaderPresentation = RibbonHeaderPresentation.Filled, HeaderSelectedBackground = new(Color.Pink), HeaderFilledCornerRadius = 10 };
        graphics.Clear(Color.Beige); painter.DrawTab(context with { Theme = filled }, tab, true);
        Assert.Equal(Color.Magenta, painter.Ink); Assert.Equal(Color.Beige.ToArgb(), image.GetPixel(context.Bounds.Left, context.Bounds.Top).ToArgb());
        Assert.Equal(Color.Pink.ToArgb(), image.GetPixel(context.Bounds.Left + context.Bounds.Width / 2, context.Bounds.Bottom - S(4)).ToArgb());
        using var ribbon = new RibbonControl { Width = 430, Theme = RibbonThemes.Office2024Light };
        ribbon.FileMenu.Visible = true; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
        for (int i = 0; i < 7; i++) ribbon.Tabs.Add(new RibbonTab { Text = "Document " + i });
        var before = ribbon.Snapshot; var targets = before.Targets.Select(t => (t.Kind, t.Bounds, t.Clip)).ToArray();
        ribbon.Theme = filled;
        Assert.Equal(before.TabViewport, ribbon.Snapshot.TabViewport); Assert.Equal(before.TabExtent, ribbon.Snapshot.TabExtent);
        Assert.Equal(targets, ribbon.Snapshot.Targets.Select(t => (t.Kind, t.Bounds, t.Clip)).ToArray());
    });

    [Theory]
    [InlineData(nameof(RibbonTheme.HeaderIndicatorThickness), 64)]
    [InlineData(nameof(RibbonTheme.HeaderIndicatorBottomInset), 1024)]
    [InlineData(nameof(RibbonTheme.HeaderFilledCornerRadius), 64)]
    public void Header_effect_limits_validate_before_mutating_theme_selection(string property, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; using var other = new RibbonControl();
        var original = ribbon.Theme; var role = typeof(RibbonTheme).GetProperty(property)!;
        foreach (int invalid in new[] { -1, maximum + 1 })
        {
            var candidate = original with { }; role.SetValue(candidate, invalid);
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = candidate);
            Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme);
        }
        foreach (int? valid in new int?[] { 0, maximum, null })
        {
            var candidate = original with { }; role.SetValue(candidate, valid); ribbon.Theme = candidate;
            Assert.Null(ribbon.BuiltInTheme); Assert.Same(candidate, ribbon.Theme);
        }
        Assert.Null(role.GetValue(other.Theme));
    });

    private sealed class InkRecorder : RibbonOffice2024Renderer
    {
        internal Color Ink;
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { Ink = color; base.DrawText(context, text, color, flags); }
    }

    private static void Compare(string name, Bitmap image, IEnumerable<string> geometry)
    {
        using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); var png = stream.ToArray(); var data = geometry.ToArray();
        if (Environment.GetEnvironmentVariable("RIBBONKIT_HEADER_CAPTURE") is { Length: > 0 } output)
        { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), png); File.WriteAllLines(Path.Combine(output, name + ".txt"), data); }
        if (Environment.GetEnvironmentVariable("RIBBONKIT_HEADER_BASELINE") is { Length: > 0 } baseline)
        { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png); Assert.Equal(File.ReadAllLines(Path.Combine(baseline, name + ".txt")), data); }
    }
}
