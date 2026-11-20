using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class PaintFoundationTests
{
    private static RibbonFill Crease => new(Color.White, Color.Black)
    {
        IntermediateStops = RibbonGradientStops.WithHardTransitions([new(.5, Color.White), new(.5, Color.Black)])
    };
    private static RibbonGroupPanelAppearanceSettings Panel => new()
    {
        Background = new(Color.LightBlue), BorderColor = Color.Navy, InnerBorderColor = Color.White,
        LabelBackground = new(Color.Gold), LabelForeground = Color.Blue
    };

    [Fact]
    public void Hard_stops_copy_order_compare_by_value_and_preserve_the_strict_constructor()
    {
        var source = new[] { new RibbonGradientStop(.5, Color.White), new RibbonGradientStop(.5, Color.Black), new RibbonGradientStop(.5, Color.Red) };
        var stops = RibbonGradientStops.WithHardTransitions(source);
        var equal = RibbonGradientStops.WithHardTransitions(source.ToList()); source[2] = new(.75, Color.Blue);
        Assert.Equal(Color.Red, stops[2].Color); Assert.Equal(stops, equal); Assert.Equal(stops.GetHashCode(), equal.GetHashCode());
        Assert.Throws<ArgumentOutOfRangeException>(() => new RibbonGradientStops(stops));
        var smooth = new[] { new RibbonGradientStop(.25, Color.Lime), new RibbonGradientStop(.75, Color.Blue) };
        Assert.Equal(new RibbonGradientStops(smooth), RibbonGradientStops.WithHardTransitions(smooth));
        Assert.Equal(new RibbonFill(Color.Red), new RibbonFill(Color.Red) { IntermediateStops = RibbonGradientStops.WithHardTransitions([]) });
        Assert.Single(new HashSet<RibbonFill> { Crease, Crease with { IntermediateStops = RibbonGradientStops.WithHardTransitions(Crease.IntermediateStops!) } });
        Assert.NotEqual(Crease, Crease with { IntermediateStops = RibbonGradientStops.WithHardTransitions(Crease.IntermediateStops!.Reverse()) });
        var triple = Crease with { IntermediateStops = stops };
        Assert.Equal(Color.White.ToArgb(), RibbonFillColors.At(triple, .5 - 1e-12).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), RibbonFillColors.At(triple, .5).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), RibbonFillColors.At(triple, .5 + 1e-12).ToArgb());
        Assert.Equal(Color.Black.ToArgb(), RibbonFillColors.At(triple, 1).ToArgb());
    }

    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(-.1)] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)]
    public void Hard_factory_rejects_nonfinite_and_endpoint_offsets(double offset) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RibbonGradientStops.WithHardTransitions([new(offset, Color.Red)]));

    [Fact]
    public void Hard_factory_rejects_null_and_decreasing_offsets_and_maps_every_alpha_without_spacing()
    {
        Assert.Throws<ArgumentNullException>(() => RibbonGradientStops.WithHardTransitions(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => RibbonGradientStops.WithHardTransitions([new(.6, Color.Red), new(.5, Color.Blue)]));
        var mapped = RibbonFillColors.Map(Crease with { Direction = RibbonGradientDirection.Horizontal }, c => Color.FromArgb(128, c));
        Assert.Equal(new[] { .5, .5 }, mapped.IntermediateStops!.Select(s => s.Offset));
        Assert.All(mapped.IntermediateStops!, s => Assert.Equal(128, s.Color.A));
        Assert.Equal(128, mapped.Start.A); Assert.Equal(128, mapped.End.A); Assert.False(RibbonFillColors.IsOpaque(mapped));
        Assert.False(RibbonFillColors.IsSolid(Crease)); Assert.Equal(RibbonGradientDirection.Horizontal, mapped.Direction);
        Assert.Equal(128, RibbonFillColors.At(mapped, .5).A);
        var changed = Crease with { Start = Color.Lime, End = Color.Blue };
        Assert.Equal(Color.Lime.ToArgb(), RibbonFillColors.At(changed, 0).ToArgb()); Assert.Equal(Color.Blue.ToArgb(), RibbonFillColors.At(changed, 1).ToArgb());
    }

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Hard_paint_uses_exact_pixel_boundary_on_both_axes_and_retains_full_surface_clips(int dpi)
    {
        foreach (bool office in new[] { false, true })
        foreach (bool horizontal in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(81, 81); using var g = Graphics.FromImage(image);
            var fill = Crease with { Direction = horizontal ? RibbonGradientDirection.Horizontal : RibbonGradientDirection.Vertical };
            var context = new RibbonPaintContext(g, new(10, 10, 61, 61), RibbonThemes.Office2010Blue with { BackgroundFill = fill }, Control.DefaultFont, dpi);
            g.Clear(Color.Magenta); painter.DrawSurface(context, false);
            for (int i = 0; i < 61; i++) Assert.Equal((i < 30 ? Color.White : Color.Black).ToArgb(), image.GetPixel(horizontal ? 10 + i : 40, horizontal ? 40 : 10 + i).ToArgb());
            using var full = (Bitmap)image.Clone(); g.Clear(Color.Magenta); g.SetClip(new Rectangle(20, 20, 40, 40));
            painter.DrawSurface(context with { Bounds = new(25, 25, 30, 30), BackgroundBounds = context.Bounds }, false);
            for (int y = 0; y < 81; y++) for (int x = 0; x < 81; x++)
                Assert.Equal((x >= 25 && x < 55 && y >= 25 && y < 55 ? full.GetPixel(x, y) : Color.Magenta).ToArgb(), image.GetPixel(x, y).ToArgb());
        }
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Command_and_rounded_alpha_paths_use_the_same_hard_sampler_and_restore_graphics(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        using var image = new Bitmap(65, 65); using var g = Graphics.FromImage(image);
        var fill = RibbonFillColors.Map(Crease, c => Color.FromArgb(128, c));
        var theme = RibbonThemes.Office2010Blue with { Hover = fill, CommandAppearance = new() { Inset = 0, CornerRadius = 5, BorderWidth = 0 } };
        g.Clear(Color.Transparent); painter.DrawItem(new(g, new(0, 0, 65, 65), theme, Control.DefaultFont, 96, Hot: true), new RibbonButtonItem(), RibbonItemSize.Large, false);
        Assert.Equal(128, image.GetPixel(32, 31).A); Assert.InRange(image.GetPixel(32, 31).R, 254, 255);
        Assert.Equal(128, image.GetPixel(32, 32).A); Assert.Equal(0, image.GetPixel(32, 32).R);
        Assert.Equal(CompositingMode.SourceOver, g.CompositingMode);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Panels_scale_rims_and_label_bands_from_measured_frames_and_obey_the_caller_clip(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var group = new RibbonGroup(); using var image = new Bitmap(S(160), S(140)); using var g = Graphics.FromImage(image);
            var frame = new Rectangle(S(10), S(10), S(130), S(112));
            var context = new RibbonPaintContext(g, frame, RibbonThemes.Office2010Blue with { BodyAppearance = new() { GroupPanel = Panel } }, Control.DefaultFont, dpi)
            { GroupFrameBounds = frame, GroupCaptionBounds = RibbonGroupPanelMetrics.CaptionBounds(frame, Control.DefaultFont, dpi) };
            var metrics = RibbonGroupPanelMetrics.Resolve(context, false); g.Clear(Color.Magenta); painter.DrawGroup(context, group, false);
            int middle = metrics.Panel.Left + metrics.Panel.Width / 2;
            Assert.Equal(frame.Left + S(2), metrics.Panel.Left); Assert.Equal(S(1), metrics.Stroke);
            Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(middle, metrics.Panel.Top).ToArgb());
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(middle, metrics.Panel.Top + S(1)).ToArgb());
            Assert.Equal(Color.LightBlue.ToArgb(), image.GetPixel(middle, metrics.Panel.Top + S(4)).ToArgb());
            Assert.Equal(Color.Gold.ToArgb(), image.GetPixel(middle, metrics.Caption.Top + S(3)).ToArgb());
            Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(frame.Left, frame.Top).ToArgb());
            using var full = (Bitmap)image.Clone(); g.Clear(Color.Magenta);
            var clip = new Rectangle(middle, frame.Top, S(20), frame.Height / 2); g.SetClip(clip);
            painter.DrawGroupPanel(context, group, false);
            Assert.Equal(clip, Rectangle.Round(g.ClipBounds));
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
                Assert.Equal((clip.Contains(x, y) ? full.GetPixel(x, y) : Color.Magenta).ToArgb(), image.GetPixel(x, y).ToArgb());
        }
    });

    [Fact]
    public void Decoration_handles_transparency_zero_width_extreme_radius_small_bounds_and_no_collapsed_band() => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonRenderer(); using var group = new RibbonGroup(); using var image = new Bitmap(100, 100); using var g = Graphics.FromImage(image);
        var panel = Panel with { Background = new(Color.FromArgb(128, Color.Blue)), BorderWidth = 0, InnerBorderWidth = 0, Inset = 0, CornerRadius = 0 };
        var context = new RibbonPaintContext(g, new(0, 0, 81, 81), RibbonThemes.Office2010Blue with { BodyAppearance = new() { GroupPanel = panel } }, Control.DefaultFont, 96);
        foreach (bool collapsed in new[] { false, true }) foreach (bool simplified in new[] { false, true })
        {
            g.Clear(Color.Transparent); painter.DrawGroupPanel(context with { IsSimplifiedGroup = simplified }, group, collapsed);
            Assert.Equal(128, image.GetPixel(40, 10).A);
            Assert.Equal(collapsed || simplified ? 128 : 255, image.GetPixel(40, 70).A);
        }
        foreach (int side in new[] { 1, 2, 3, 8, 81 })
        {
            g.Clear(Color.Transparent); painter.DrawGroupPanel(context with { Bounds = new(0, 0, side, side), Theme = context.Theme with { BodyAppearance = new() { GroupPanel = Panel with { CornerRadius = 64, BorderWidth = 32, InnerBorderWidth = 32, InnerBorderInset = 1024, Inset = 1024 } } } }, group, false);
            Assert.Equal(0, image.GetPixel(side, 0).A);
        }
    });

    [Theory] [InlineData(RibbonLayoutMode.Classic)] [InlineData(RibbonLayoutMode.Simplified)]
    public void Opt_in_and_reset_preserve_snapshots_reduction_targets_native_editors_and_renderer_hooks(RibbonLayoutMode mode) => FoundationTests.Sta(() =>
    {
        using var host = new Form { Bounds = new(-16000, -16000, 1100, 650), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var painter = new PanelHooks(); using var ribbon = new RibbonControl { Renderer = painter };
        var tab = new RibbonTab(); var fixedGroup = new RibbonGroup { Text = "Editor", CanResize = false };
        var field = new RibbonTextBoxItem { Value = "Native selection", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        fixedGroup.Items.Add(field); tab.Groups.Add(fixedGroup);
        for (int i = 0; i < 4; i++)
        {
            var group = new RibbonGroup { Text = "Long group " + i };
            for (int j = 0; j < 4; j++) group.Items.Add(new RibbonButtonItem { Text = "Command " + j, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline });
            tab.Groups.Add(group);
        }
        ribbon.Tabs.Add(tab); host.Controls.Add(ribbon); host.Show(); Application.DoEvents();
        var editor = Assert.IsAssignableFrom<TextBoxBase>(ribbon.Editors.EditorFor(field)); nint handle = editor.Handle; var parent = editor.Parent; editor.Select(2, 4);
        ribbon.LayoutMode = mode;
        foreach (var density in Enum.GetValues<RibbonDensity>()) foreach (int width in new[] { 1050, 460, 180 })
        {
            ribbon.Density = density; host.ClientSize = new(width, 650); var original = ribbon.Theme; var before = ribbon.Snapshot;
            var targets = before.Targets.Select(t => (t.Kind, t.Element, t.Bounds, t.Clip, t.Size)).ToArray(); var groups = before.Groups.ToArray();
            ribbon.Theme = original with { BodyAppearance = new() { GroupPanel = Panel } };
            Assert.Equal(targets, ribbon.Snapshot.Targets.Select(t => (t.Kind, t.Element, t.Bounds, t.Clip, t.Size)).ToArray()); Assert.Equal(groups, ribbon.Snapshot.Groups.ToArray());
            Assert.Equal(before.BodyViewport, ribbon.Snapshot.BodyViewport);
            using var image = new Bitmap(ribbon.Width, ribbon.Height); painter.Calls.Clear(); ribbon.DrawToBitmap(image, ribbon.ClientRectangle);
            Assert.NotEmpty(painter.Calls); Assert.All(painter.Calls, c => Assert.False(c.GroupFrameBounds.IsEmpty));
            Assert.Equal(handle, editor.Handle); Assert.Same(parent, editor.Parent); Assert.Equal("Native selection", editor.Text); Assert.Equal(2, editor.SelectionStart); Assert.Equal(4, editor.SelectionLength);
            ribbon.Theme = original; Assert.Equal(targets, ribbon.Snapshot.Targets.Select(t => (t.Kind, t.Element, t.Bounds, t.Clip, t.Size)).ToArray());
        }
    });

    [Fact]
    public void Invalid_decoration_assignment_is_atomic() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var original = ribbon.Theme;
        foreach (var bad in new[] { Panel with { BorderWidth = -1 }, Panel with { BorderWidth = 33 }, Panel with { InnerBorderWidth = 33 }, Panel with { CornerRadius = 65 }, Panel with { Inset = 1025 }, Panel with { InnerBorderInset = -1 } })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { BodyAppearance = new() { GroupPanel = bad } }); Assert.Same(original, ribbon.Theme);
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Label_gradients_use_band_coordinates_inside_all_rounded_corners_and_caption_ink_is_independent(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new CaptionHooks(); using var group = new RibbonGroup { Text = "A deliberately long group caption" };
        using var image = new Bitmap(S(150), S(130)); using var g = Graphics.FromImage(image); g.Clear(Color.Transparent);
        var settings = Panel with { Background = null, BorderWidth = 0, InnerBorderWidth = 0, CornerRadius = 12, LabelBackground = Crease };
        var context = new RibbonPaintContext(g, new(S(5), S(7), S(130), S(112)), RibbonThemes.Office2010Blue with { BodyAppearance = new() { GroupPanel = settings } }, Control.DefaultFont, dpi);
        var metrics = RibbonGroupPanelMetrics.Resolve(context, false); g.CompositingMode = CompositingMode.SourceCopy;
        painter.DrawGroup(context, group, false); Assert.Equal(CompositingMode.SourceCopy, g.CompositingMode);
        for (int y = metrics.Caption.Top; y < metrics.Caption.Bottom; y++)
        {
            double position = (double)(y - metrics.Caption.Top) / (metrics.Caption.Height - 1);
            Assert.Equal((position < .5 ? Color.White : Color.Black).ToArgb(), image.GetPixel(metrics.Caption.Left + metrics.Caption.Width / 2, y).ToArgb());
        }
        using var mask = new Bitmap(image.Width, image.Height); using var maskGraphics = Graphics.FromImage(mask); maskGraphics.Clear(Color.Transparent);
        painter.PaintRounded(context with { Graphics = maskGraphics, Bounds = metrics.Panel }, metrics.Radius, new(Color.White), Color.Transparent, 0);
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) Assert.True(image.GetPixel(x, y).A <= mask.GetPixel(x, y).A);
        for (int y = 0; y < metrics.Radius; y++) for (int x = 0; x < metrics.Radius; x++)
        {
            int left = metrics.Panel.Left + x, right = metrics.Panel.Right - 1 - x, top = metrics.Panel.Top + y, bottom = metrics.Panel.Bottom - 1 - y;
            Assert.Equal(mask.GetPixel(left, top), mask.GetPixel(right, top)); Assert.Equal(mask.GetPixel(left, top), mask.GetPixel(left, bottom)); Assert.Equal(mask.GetPixel(left, top), mask.GetPixel(right, bottom));
        }
        Assert.Equal(Color.Blue, painter.Ink); Assert.True(metrics.Caption.Contains(painter.TextBounds));
        group.Enabled = false; painter.DrawGroup(context, group, false); Assert.Equal(context.Theme.DisabledColor, painter.Ink);
    });

    [Theory] [InlineData(RibbonLayoutMode.Classic)] [InlineData(RibbonLayoutMode.Simplified)]
    public void Minimized_body_popup_uses_shared_measured_panel_hooks_without_replacing_group_models(RibbonLayoutMode mode) => FoundationTests.Sta(() =>
    {
        using var host = new Form { Bounds = new(-16000, -16000, 900, 650), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var painter = new PanelHooks(); using var ribbon = new RibbonControl { Renderer = painter, LayoutMode = mode,
            Theme = RibbonThemes.Office2010Blue with { BodyAppearance = new() { GroupPanel = Panel } } };
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" };
        group.Items.Add(new RibbonButtonItem { Text = "Command", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        host.Controls.Add(ribbon); host.Show(); Application.DoEvents(); ribbon.IsMinimized = true;
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, tab)));
        var popup = Assert.IsType<RibbonPopupSurface>(ribbon.Popups.Top);
        using var image = new Bitmap(popup.Width, popup.Height); painter.Calls.Clear(); popup.DrawToBitmap(image, popup.ClientRectangle);
        Assert.NotEmpty(painter.Calls); Assert.All(painter.Calls, c =>
        {
            Assert.False(c.GroupFrameBounds.IsEmpty); Assert.Equal(mode == RibbonLayoutMode.Simplified, c.IsSimplifiedGroup);
            if (mode == RibbonLayoutMode.Classic) Assert.False(c.GroupCaptionBounds.IsEmpty); else Assert.True(c.GroupCaptionBounds.IsEmpty);
        });
        Assert.Same(group, tab.Groups[0]); ribbon.Popups.CloseAll(false);
    });

    private sealed class PanelHooks : RibbonOffice2024Renderer
    {
        internal List<RibbonPaintContext> Calls { get; } = [];
        public override void DrawGroupPanel(RibbonPaintContext context, RibbonGroup group, bool collapsed) { Calls.Add(context); base.DrawGroupPanel(context, group, collapsed); }
    }
    private sealed class CaptionHooks : RibbonOffice2024Renderer
    {
        internal Color Ink;
        internal Rectangle TextBounds;
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { Ink = color; TextBounds = context.Bounds; }
    }
}
