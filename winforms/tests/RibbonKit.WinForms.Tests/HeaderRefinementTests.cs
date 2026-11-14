using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class HeaderRefinementTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Office2010_hover_and_press_have_top_sides_and_open_square_foot_without_double_alpha(int dpi) => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonOffice2024Renderer(); using var tab = new RibbonTab { Text = "" };
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        var bounds = new Rectangle(S(5), S(5), S(100), S(40)); var theme = RibbonThemes.Office2010Blue;
        foreach (bool pressed in new[] { false, true })
        {
            using var image = new Bitmap(S(120), S(55)); using var expected = new Bitmap(image.Width, image.Height);
            using var g = Graphics.FromImage(image); using var eg = Graphics.FromImage(expected); g.Clear(Color.Magenta); eg.Clear(Color.Magenta);
            var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi, Hot: !pressed, Pressed: pressed);
            var noBorder = theme with { HeaderAppearance = theme.HeaderAppearance! with { FilledBorderColor = Color.Transparent } };
            painter.DrawTab(c with { Graphics = eg, Theme = noBorder }, tab, false);
            painter.DrawTab(c, tab, false);
            int x = bounds.Left + bounds.Width / 2; var border = pressed ? theme.HeaderAppearance!.FilledPressedBorderColor : theme.HeaderAppearance!.FilledHoverBorderColor;
            Assert.Equal(border.ToArgb(), image.GetPixel(x, bounds.Top).ToArgb());
            Assert.Equal(border.ToArgb(), image.GetPixel(bounds.Left, bounds.Bottom - 1).ToArgb());
            Assert.Equal(expected.GetPixel(x, bounds.Bottom - 1), image.GetPixel(x, bounds.Bottom - 1));
            painter.DrawTab(c with { Theme = theme with { HeaderAppearance = theme.HeaderAppearance! with { FilledTabBottomBorderVisible = true } } }, tab, false);
            Assert.Equal(border.ToArgb(), image.GetPixel(x, bounds.Bottom - 1).ToArgb());
        }
        Assert.True(new RibbonHeaderAppearanceSettings().FilledTabBottomBorderVisible);
        Assert.True(RibbonThemes.Office2013Light.HeaderAppearance!.FilledTabBottomBorderVisible);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Minimize_slot_restores_the_full_header_gradient_and_preserves_composited_alpha(int dpi) => FoundationTests.Sta(() =>
    {
        foreach (bool office in new[] { false, true })
        foreach (bool composited in new[] { false, true })
        foreach (var direction in Enum.GetValues<RibbonGradientDirection>())
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            var fill = RibbonThemes.Office2010Blue.BackgroundFill!.Value with { Direction = direction };
            if (composited) fill = RibbonFillColors.Map(fill, color => Color.FromArgb(100, color));
            var theme = RibbonThemes.Office2010Blue with { BackgroundFill = fill };
            using var image = new Bitmap(360, 95); using var expected = new Bitmap(360, 95);
            using var g = Graphics.FromImage(image); using var eg = Graphics.FromImage(expected);
            var header = new Rectangle(0, 20, 360, 60); var slot = new Rectangle(305, 20, 45, 60);
            var c = new RibbonPaintContext(g, header, theme, Control.DefaultFont, dpi) { BackgroundBounds = header, CompositedText = composited };
            if (composited) { g.CompositingMode = CompositingMode.SourceCopy; eg.CompositingMode = CompositingMode.SourceCopy; }
            painter.DrawSurface(c with { Graphics = eg }, false); painter.DrawSurface(c, false);
            g.CompositingMode = CompositingMode.SourceOver;
            using (var noise = new SolidBrush(Color.Magenta)) g.FillRectangle(noise, slot);
            painter.DrawMinimizeButton(c with { Bounds = slot }, false);
            for (int y = slot.Top; y < slot.Bottom; y++) Assert.Equal(expected.GetPixel(slot.Left, y), image.GetPixel(slot.Left, y));
            Assert.Equal(CompositingMode.SourceOver, g.CompositingMode);
        }
    });

    [Fact]
    public void Actual_ribbon_minimize_slot_keeps_the_band_gradient_in_expanded_and_minimized_states() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Bounds = new(-16000, -16000, 1000, 600), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var painter = new HeaderContextProbe();
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue, Renderer = painter, QuickAccessVisible = true, AnimationLevel = RibbonAnimationLevel.None };
        ribbon.Tabs.AddRange(new RibbonTab { Text = "Home" }, new RibbonTab { Text = "Edit" }); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        var group = new RibbonGroup(); var command = new RibbonButtonItem { Text = "Save" }; group.Items.Add(command); ribbon.Tabs[0].Groups.Add(group);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = command });
        form.Show(); Application.DoEvents();
        foreach (var position in Enum.GetValues<RibbonQuickAccessPosition>())
        foreach (bool minimized in new[] { false, true })
        {
            ribbon.QuickAccessPosition = position; ribbon.IsMinimized = minimized;
            using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
            var slot = ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.Minimize).Bounds;
            for (int y = slot.Top + 1; y < slot.Bottom - 1; y++) Assert.Equal(image.GetPixel(slot.Left - 3, y), image.GetPixel(slot.Left + 1, y));
            Assert.Equal(ribbon.Theme.BackgroundFill, ribbon.HeaderPaintTheme(false, chrome: true).BackgroundFill);
            if (position == RibbonQuickAccessPosition.TabRow)
                Assert.Equal(new Rectangle(0, ribbon.WindowCaptionHeight, ribbon.Width, RibbonLayoutEngine.Scale(ribbon.LayoutTheme.TabHeight, ribbon.DeviceDpi)), painter.QatBackgroundBounds);
        }
    });

    [Theory] [InlineData(RibbonBuiltInTheme.Office2010Blue, 64)] [InlineData(RibbonBuiltInTheme.Office2013Light, 64)]
    [InlineData(RibbonBuiltInTheme.Office2019Light, 64)] [InlineData(RibbonBuiltInTheme.Office2024Light, 50)]
    public void File_minimum_width_is_shared_by_all_themes_layouts_and_routes_with_independent_reset(RibbonBuiltInTheme family, int minimum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 1200, BuiltInTheme = family, AnimationLevel = RibbonAnimationLevel.None };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); var basis = ribbon.Theme;
        Assert.Equal(minimum, basis.FileButtonMinimumWidth ?? 50);
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var route in Enum.GetValues<RibbonFileMenuMode>())
        {
            ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.FileMenuMode = route;
            ribbon.Theme = basis with { FileButtonMinimumWidth = 104 };
            int S(int n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi);
            var slot = ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.FileMenu).Bounds; Assert.Equal(S(104), slot.Width);
            ribbon.FileMenu.Enabled = false; Assert.Equal(slot, ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.FileMenu).Bounds); ribbon.FileMenu.Enabled = true;
            ribbon.Theme = basis with { FileButtonMinimumWidth = null, TabHorizontalPadding = 0 }; ribbon.FileMenu.Text = "";
            Assert.Equal(S(50), ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.FileMenu).Bounds.Width);
            ribbon.Theme = basis with { FileButtonMinimumWidth = 1, TabHorizontalPadding = 0 };
            Assert.Equal(S(density == RibbonDensity.Touch ? 44 : 1), ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.FileMenu).Bounds.Width);
            ribbon.FileMenu.Text = "A much longer File command caption";
            Assert.True(ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.FileMenu).Bounds.Width > S(104));
            ribbon.FileMenu.Text = "File";
        }
        var before = ribbon.Theme;
        foreach (int invalid in new[] { -1, 0, 1025 }) { Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = before with { FileButtonMinimumWidth = invalid }); Assert.Same(before, ribbon.Theme); }
        ribbon.Theme = basis with { FileButtonMinimumWidth = 104 }; var authored = ribbon.Theme;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; ribbon.BuiltInTheme = null; Assert.Same(authored, ribbon.Theme);
        ribbon.Width = 40;
        Assert.All(ribbon.Snapshot.Targets.Where(t => t.Kind == TargetKind.FileMenu), t => Assert.InRange(t.Bounds.Right, 0, ribbon.Width));
        foreach (int dpi in new[] { 96, 120, 144, 192 }) Assert.Equal(RibbonLayoutEngine.Scale(104, dpi), RibbonLayoutEngine.Scale(RibbonPresentationResolver.Header(authored).ResolveMetrics(authored, RibbonDensity.Compact).FileMinimum, dpi));
    });

    private sealed class HeaderContextProbe : RibbonOffice2024Renderer
    {
        internal Rectangle QatBackgroundBounds;
        public override void DrawQuickAccessToolbar(RibbonPaintContext context, RibbonQuickAccessPosition position)
        { if (position == RibbonQuickAccessPosition.TabRow) QatBackgroundBounds = context.BackgroundBounds; base.DrawQuickAccessToolbar(context, position); }
    }
}
