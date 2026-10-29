using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GeometryMotionTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly Form Host = new() { Size = new(1200, 700) };
        internal readonly RibbonControl Ribbon = new() { BindingContext = new BindingContext() };
        internal readonly RibbonComboBoxItem Combo = new() { Id = "font", Text = "Font", IsEditable = true, Value = "100", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        internal Fixture()
        {
            Application.EnableVisualStyles(); var tab = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "group", Text = "Commands" };
            var copy = new RibbonButtonItem { Id = "copy", Text = "Copy", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; Combo.Items.AddRange("100", "150"); group.Items.AddRange(copy, Combo); tab.Groups.Add(group); Ribbon.Tabs.Add(tab); Ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = copy });
            Host.Controls.Add(Ribbon); Host.Show(); Application.DoEvents(); Ribbon.Motion.SystemPolicyOverride = true;
        }
        internal void Finish() { var watch = Stopwatch.StartNew(); while (Ribbon.IsAnimating && watch.ElapsedMilliseconds < 1000) { Application.DoEvents(); Thread.Sleep(1); } Assert.False(Ribbon.IsAnimating); }
        public void Dispose() { Ribbon.Dispose(); Host.Dispose(); }
    }
    private static long Style(Control c) => GetWindowLongPtr(c.Handle, -20).ToInt64();
    [Theory] [InlineData(RibbonTabContentAnimation.Fade)] [InlineData(RibbonTabContentAnimation.SlideDown)]
    public void Header_arrivals_cover_context_merge_and_live_add_and_coalesce_immediate_selection(RibbonTabContentAnimation style) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var context = new RibbonTab { Text = "Picture Format", IsContextual = true, Visible = false }; context.Groups.Add(new RibbonGroup { Text = "Picture" }); f.Ribbon.Tabs.Add(context);
        f.Ribbon.TabContentAnimation = style; f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; nint editor = f.Ribbon.Editors.EditorFor(f.Combo)!.Handle;
        context.Visible = true; Assert.True(f.Ribbon.IsAnimating); int count = f.Ribbon.Motion.StartedCount;
        f.Ribbon.SelectedTab = context; Assert.True(f.Ribbon.IsAnimating); Assert.Equal(count, f.Ribbon.Motion.StartedCount); f.Finish(); context.Visible = false; Assert.False(f.Ribbon.IsAnimating);
        using var source = new RibbonMergeSource { Order = -1 }; var chart = new RibbonTab { Text = "Chart Design" }; chart.Groups.Add(new RibbonGroup()); source.Tabs.Add(chart);
        Assert.True(f.Ribbon.Merge(source)); Assert.True(f.Ribbon.IsAnimating); Assert.False(f.Ribbon.Merge(source)); Assert.True(f.Ribbon.IsAnimating); count = f.Ribbon.Motion.StartedCount;
        f.Ribbon.SelectedTab = chart; Assert.True(f.Ribbon.IsAnimating); Assert.Equal(count, f.Ribbon.Motion.StartedCount); Assert.Same(source, chart.Parent); f.Finish();
        Assert.True(f.Ribbon.Unmerge(source)); Assert.False(f.Ribbon.IsAnimating); Assert.Same(source, chart.Parent);
        f.Ribbon.Tabs.AddRange(new RibbonTab { Text = "Review" }, new RibbonTab { Text = "Layout" }); Assert.True(f.Ribbon.IsAnimating); count = f.Ribbon.Motion.StartedCount; f.Finish(); Assert.Equal(count, f.Ribbon.Motion.StartedCount);
        Assert.Equal(editor, f.Ribbon.Editors.EditorFor(f.Combo)!.Handle);
    });
    [Fact]
    public void Header_arrival_policy_batches_and_failures_keep_immediate_state_and_cleanup() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var context = new RibbonTab { Text = "Format", IsContextual = true, Visible = false }; f.Ribbon.Tabs.Add(context); context.Visible = true; Assert.False(f.Ribbon.IsAnimating); context.Visible = false;
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.Motion.SystemPolicyOverride = false; context.Visible = true; Assert.False(f.Ribbon.IsAnimating); context.Visible = false; f.Ribbon.Motion.SystemPolicyOverride = true;
        f.Ribbon.BeginUpdate(); context.Visible = true; f.Ribbon.EndUpdate(); Assert.False(f.Ribbon.IsAnimating); context.Visible = false;
        long previousStyle = Style(f.Ribbon); using var source = new RibbonMergeSource(); source.Tabs.Add(new RibbonTab { Id = "home", Text = "Conflict" });
        Assert.Throws<ArgumentException>(() => f.Ribbon.Merge(source)); Assert.False(source.IsMerged); Assert.False(f.Ribbon.IsAnimating); Assert.Equal(previousStyle, Style(f.Ribbon));
        context.Visible = true; Assert.True(f.Ribbon.IsAnimating); f.Host.Width -= 30; Assert.False(f.Ribbon.IsAnimating); Assert.Equal(previousStyle, Style(f.Ribbon));
        context.Visible = false; f.Ribbon.IsMinimized = true; f.Ribbon.SettleMotion(); // Test header arrival after minimizing; overlapping requests intentionally snap.
        context.Visible = true; Assert.True(f.Ribbon.IsAnimating); f.Ribbon.SelectedTab = context; Assert.True(f.Ribbon.IsAnimating); f.Finish(); Assert.True(f.Ribbon.IsMinimized);
    });
    [Theory] [InlineData(RibbonTabContentAnimation.Fade)] [InlineData(RibbonTabContentAnimation.SlideDown)]
    public void Header_drawing_keeps_committed_slots_and_applies_the_shared_style_only_to_arrivals(RibbonTabContentAnimation style) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var renderer = new HeaderBlocks(); f.Ribbon.Renderer = renderer; f.Ribbon.TabContentAnimation = style;
        using var from = new RibbonMotionScene(f.Ribbon); var arriving = new RibbonTab { Id = "arriving", Text = "Chart" }; f.Ribbon.Tabs.Insert(0, arriving); using var to = new RibbonMotionScene(f.Ribbon); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        var target = to.Headers[arriving]; var newHome = to.Headers[f.Ribbon.Tabs[1]];
        foreach (double progress in new[] { .25, .5, .75, 1 })
        {
            using var frame = new Bitmap(f.Ribbon.Width, f.Ribbon.Height, PixelFormat.Format32bppPArgb); using var g = Graphics.FromImage(frame); using var alpha = new ImageAttributes(); RibbonMotionScene.Render(g, to.Bounds, f.Ribbon, from, to, progress, alpha);
            int x = target.Left + target.Width / 2; int y = target.Top + target.Height / 2;
            var color = frame.GetPixel(x, y); Assert.Equal(255, color.G);
            if (style == RibbonTabContentAnimation.Fade && progress < 1) Assert.InRange(color.R, 1, 254); else Assert.Equal(0, color.R);
            Assert.Equal(Color.Red.ToArgb(), frame.GetPixel(newHome.Left + newHome.Width / 2, newHome.Top + newHome.Height / 2).ToArgb());
        }
    });
    private sealed class HeaderBlocks : RibbonRenderer
    {
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { using var brush = new SolidBrush(tab.Id == "arriving" ? Color.Lime : Color.Red); c.Graphics.FillRectangle(brush, c.Bounds); }
    }
    [Theory] [InlineData(RibbonAnimationLevel.Subtle)] [InlineData(RibbonAnimationLevel.Expressive)]
    public void Slide_down_keeps_arriving_content_opaque_and_natural_size_without_departing_body(RibbonAnimationLevel level) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var first = f.Ribbon.SelectedTab!; first.Groups[0].Items.Insert(0, new RibbonButtonItem { Text = "Old", Icon = new SolidIcon(Color.Magenta) });
        var next = new RibbonTab { Text = "Insert" }; var group = new RibbonGroup { Text = "New commands" }; group.Items.Add(new RibbonButtonItem { Text = "New", Icon = new SolidIcon(Color.Lime) }); next.Groups.Add(group); f.Ribbon.Tabs.Add(next);
        Assert.Equal(RibbonTabContentAnimation.Fade, f.Ribbon.TabContentAnimation); f.Ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown;
        using var from = new RibbonMotionScene(f.Ribbon); f.Ribbon.SelectedTab = next; using var to = new RibbonMotionScene(f.Ribbon); f.Ribbon.AnimationLevel = level;
        var arrival = Ink(to.Image, Color.Lime, to.Body); Assert.False(arrival.IsEmpty); int previousTop = int.MinValue;
        foreach (double progress in new[] { 0, .25, .5, .75, 1 })
        {
            using var frame = new Bitmap(f.Ribbon.Width, f.Ribbon.Height, PixelFormat.Format32bppPArgb); using var g = Graphics.FromImage(frame); using var alpha = new ImageAttributes(); RibbonMotionScene.Render(g, to.Bounds, f.Ribbon, from, to, progress, alpha);
            var ink = Ink(frame, Color.Lime, to.Body); Assert.Equal(arrival.Width, ink.Width); Assert.InRange(ink.Height, 1, arrival.Height); if (progress > 0) Assert.Equal(arrival.Height, ink.Height); Assert.True(ink.Top >= previousTop); Assert.True(ink.Top <= arrival.Top); previousTop = ink.Top;
            Assert.True(Ink(frame, Color.Magenta, to.Body).IsEmpty);
            if (progress == 0) Assert.True(ink.Top < arrival.Top); if (progress == 1) Assert.Equal(arrival, ink);
        }
        Rectangle Ink(Bitmap image, Color color, Rectangle region)
        {
            var ink = Rectangle.Empty;
            for (int y = region.Top; y < region.Bottom; y++) for (int x = region.Left; x < region.Right; x++)
                if (image.GetPixel(x, y).ToArgb() == color.ToArgb()) ink = ink.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(ink, new(x, y, 1, 1));
            return ink;
        }
    });
    [Fact]
    public void Tab_content_style_validates_and_settles_without_changing_none_policy() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); Assert.Throws<ArgumentOutOfRangeException>(() => f.Ribbon.TabContentAnimation = (RibbonTabContentAnimation)99);
        var next = new RibbonTab { Text = "Insert" }; f.Ribbon.Tabs.Add(next); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.SelectedTab = next; Assert.True(f.Ribbon.IsAnimating);
        f.Ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown; Assert.False(f.Ribbon.IsAnimating); Assert.Same(next, f.Ribbon.SelectedTab);
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.None; f.Ribbon.SelectedTab = f.Ribbon.Tabs[0]; Assert.False(f.Ribbon.IsAnimating);
    });
    private sealed class SolidIcon(Color color) : IRibbonIcon { public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { using var brush = new SolidBrush(color); graphics.FillRectangle(brush, bounds); } }
    [Theory]
    [InlineData(false, RibbonTabContentAnimation.Fade)] [InlineData(true, RibbonTabContentAnimation.Fade)]
    [InlineData(false, RibbonTabContentAnimation.SlideDown)] [InlineData(true, RibbonTabContentAnimation.SlideDown)]
    public void Expressive_indicator_springs_in_both_directions_and_settles_to_the_exact_pill(bool reverse, RibbonTabContentAnimation style) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var painter = new IndicatorRecorder(); f.Ribbon.Renderer = painter; f.Ribbon.TabContentAnimation = style;
        var home = f.Ribbon.SelectedTab!; var next = new RibbonTab { Text = "Illustrations and pictures" }; next.Groups.Add(new RibbonGroup()); f.Ribbon.Tabs.Add(next);
        if (reverse) f.Ribbon.SelectedTab = next;
        using var from = new RibbonMotionScene(f.Ribbon); f.Ribbon.SelectedTab = reverse ? home : next; using var to = new RibbonMotionScene(f.Ribbon);
        var canvas = Rectangle.Union(from.Bounds, to.Bounds); var start = from.Indicator; var end = to.Indicator; int direction = Math.Sign(end.X - start.X);
        foreach (var level in new[] { RibbonAnimationLevel.Subtle, RibbonAnimationLevel.Expressive })
        {
            f.Ribbon.AnimationLevel = level; int overshoot = 0;
            foreach (double progress in new[] { 0, .25, .6, .9, 1 })
            {
                using var frame = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format32bppPArgb); using var graphics = Graphics.FromImage(frame); using var alpha = new ImageAttributes(); painter.Indicators.Clear();
                RibbonMotionScene.Render(graphics, canvas, f.Ribbon, from, to, progress, alpha); var indicator = Assert.Single(painter.Indicators);
                Assert.InRange(indicator.Width, Math.Min(start.Width, end.Width), Math.Max(start.Width, end.Width)); Assert.Equal(end.Height, indicator.Height);
                if (progress == 0) Assert.Equal(start, indicator);
                if (progress == 1) Assert.Equal(end, indicator);
                if (level == RibbonAnimationLevel.Subtle) Assert.InRange(indicator.X, Math.Min(start.X, end.X), Math.Max(start.X, end.X));
                else if (progress == .6) { overshoot = direction * (indicator.X - end.X); Assert.True(overshoot > 0, $"Expressive should pass its destination before settling: {indicator} -> {end}"); }
                else if (progress == .9) Assert.InRange(direction * (indicator.X - end.X), 0, overshoot - 1);
                int pixels = 0;
                for (int y = end.Top; y < end.Bottom; y++)
                for (int x = 0; x < frame.Width; x++)
                    if (frame.GetPixel(x, y).ToArgb() == f.Ribbon.Theme.Accent.ToArgb()) { Assert.True(Rectangle.Intersect(indicator, to.IndicatorClip).Contains(x, y), $"Stale or unclipped pill at {x},{y}"); pixels++; }
                Assert.True(pixels > 0);
            }
        }
    });
    [Theory] [InlineData("qat")] [InlineData("density")]
    public void The_active_indicator_has_one_interpolated_rectangle_and_no_departure_copy(string change) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var painter = new IndicatorRecorder(); f.Ribbon.Renderer = painter;
        using var from = new RibbonMotionScene(f.Ribbon);
        if (change == "qat") f.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; else f.Ribbon.Density = RibbonDensity.Touch;
        using var to = new RibbonMotionScene(f.Ribbon); Assert.NotEqual(from.Indicator, to.Indicator);
        var canvas = Rectangle.Union(from.Bounds, to.Bounds);
        foreach (double progress in new[] { 0, .25, .5, .75, 1 })
        {
            using var frame = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format32bppPArgb); using var graphics = Graphics.FromImage(frame); using var alpha = new ImageAttributes(); painter.Indicators.Clear();
            RibbonMotionScene.Render(graphics, canvas, f.Ribbon, from, to, progress, alpha);
            var expected = RibbonMotionScene.Between(RibbonMotionScene.At(from.Indicator, from.Bounds), RibbonMotionScene.At(to.Indicator, to.Bounds), 1 - Math.Pow(1 - progress, 3)); expected.Offset(-canvas.X, -canvas.Y);
            Assert.Equal(expected, Assert.Single(painter.Indicators)); int pixels = 0;
            for (int y = Math.Min(from.Indicator.Top, to.Indicator.Top); y < Math.Max(from.Indicator.Bottom, to.Indicator.Bottom); y++)
            for (int x = 0; x < frame.Width; x++)
                if (frame.GetPixel(x, y).ToArgb() == f.Ribbon.Theme.Accent.ToArgb()) { Assert.True(expected.Contains(x, y), $"Stale indicator at {x},{y}; expected {expected}"); pixels++; }
            Assert.True(pixels > 0);
        }
    });
    [Fact]
    public void Shared_tab_activation_keeps_motion_alive_and_retains_native_editor_handles() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var home = f.Ribbon.SelectedTab; var next = new RibbonTab { Text = "Insert" }; var group = new RibbonGroup { Text = "Insert" }; group.Items.Add(new RibbonButtonItem { Text = "Picture" }); next.Groups.Add(group); f.Ribbon.Tabs.Add(next);
        var editor = f.Ribbon.Editors.EditorFor(f.Combo)!; nint handle = editor.Handle; f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        foreach (var tab in new[] { next, home! })
        {
            f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, tab)));
            Assert.Same(tab, f.Ribbon.SelectedTab); Assert.True(f.Ribbon.IsAnimating); f.Finish(); Assert.True(f.Ribbon.Motion.FrameIntervals.Count >= 2);
            Assert.Equal(handle, f.Ribbon.Editors.EditorFor(f.Combo)!.Handle);
        }
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.None; f.Ribbon.SelectedTab = next; Assert.False(f.Ribbon.IsAnimating);
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.Motion.SystemPolicyOverride = false; f.Ribbon.SelectedTab = home; Assert.False(f.Ribbon.IsAnimating);
        f.Ribbon.Motion.SystemPolicyOverride = true; f.Ribbon.SelectedTab = next; Assert.True(f.Ribbon.IsAnimating); f.Ribbon.SelectedTab = null; Assert.False(f.Ribbon.IsAnimating); Assert.Null(f.Ribbon.SelectedTab);
        f.Ribbon.SelectedTab = home; f.Ribbon.IsMinimized = true; f.Ribbon.SelectedTab = next; Assert.False(f.Ribbon.IsAnimating);
    });
    private sealed class IndicatorRecorder : RibbonOffice2024Renderer
    {
        internal readonly List<Rectangle> Indicators = [];
        public override void DrawTabIndicator(RibbonPaintContext c, RibbonTab tab, bool selected) { Indicators.Add(c.Bounds); base.DrawTabIndicator(c, tab, selected); }
    }
    [Fact]
    public void Density_choice_through_the_qat_popup_keeps_its_animation() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var mouseIcon = new ModeIcon(); var touchIcon = new ModeIcon(); var selector = new RibbonDropDownItem { Id = "density", Text = "Mouse", Icon = mouseIcon };
        var mouse = new RibbonRadioButtonItem { Text = "Mouse", Checked = true }; var touch = new RibbonRadioButtonItem { Text = "Touch" }; selector.Items.AddRange(mouse, touch); f.Ribbon.Tabs[0].Groups[0].Items.Add(selector);
        void Sync() { mouse.Checked = f.Ribbon.Density == RibbonDensity.Compact; touch.Checked = !mouse.Checked; selector.Text = mouse.Checked ? "Mouse" : "Touch"; var icon = mouse.Checked ? mouseIcon : touchIcon; if (!ReferenceEquals(selector.Icon, icon)) selector.Icon = icon; }
        touch.Click += (_, _) => { f.Ribbon.Density = RibbonDensity.Touch; Sync(); }; f.Ribbon.DensityChanged += (_, _) => Sync();
        var reference = new RibbonQuickAccessDropDownReference { Source = selector }; f.Ribbon.QuickAccessItems.Add(reference); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
        f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, reference))); var popup = f.Ribbon.Popups.Top!;
        f.Ribbon.Activate(popup.Targets.Single(t => ReferenceEquals(t.Element, touch)), popup);
        Assert.Equal(RibbonDensity.Touch, f.Ribbon.Density); Assert.Equal(1, f.Ribbon.Motion.StartedCount); Assert.True(f.Ribbon.IsAnimating); f.Finish();
    });
    private sealed class ModeIcon : IRibbonIcon { public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) => graphics.DrawEllipse(Pens.Blue, bounds); }
    [Fact]
    public void Qat_placement_context_command_keeps_its_animation_and_customization_notification() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; int applied = 0; f.Ribbon.CustomizationApplied += (_, _) => applied++;
        using var menu = f.Ribbon.CreateCustomizationMenu(f.Ribbon.QuickAccessItems[0], true); menu.Parent = f.Ribbon;
        ((RibbonButtonItem)menu.Items.Single(i => i.Id == "MoveQuickAccess")).PerformClick();
        Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, f.Ribbon.QuickAccessPosition); Assert.Equal(1, applied); Assert.True(f.Ribbon.IsAnimating); f.Finish();
    });
    [Theory] [InlineData("layout")] [InlineData("density")] [InlineData("qat")]
    public void Geometry_commits_once_and_opt_in_drawings_finish_without_changing_editor_handles(string change) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; var editor = f.Ribbon.Editors.EditorFor(f.Combo)!; nint handle = editor.Handle; long style = Style(f.Ribbon);
        if (change == "layout") f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        else if (change == "density") f.Ribbon.Density = RibbonDensity.Touch;
        else f.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        Assert.True(f.Ribbon.IsAnimating); Assert.Equal(1, f.Ribbon.Motion.StartedCount); Assert.Equal(handle, f.Ribbon.Editors.EditorFor(f.Combo)!.Handle); Assert.Equal(style | 0x80000, Style(f.Ribbon));
        using var frame = f.Ribbon.Motion.CaptureFrame(); Assert.NotNull(frame); f.Finish(); Assert.Equal(style, Style(f.Ribbon)); Assert.Same(f.Combo.Parent, f.Ribbon.Tabs[0].Groups[0]); Assert.Equal(handle, editor.Handle);
    });
    [Fact]
    public void None_system_policy_and_startup_preserve_immediate_geometry() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); Assert.Equal(RibbonAnimationLevel.None, f.Ribbon.AnimationLevel); f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; Assert.False(f.Ribbon.IsAnimating);
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.Motion.SystemPolicyOverride = false; f.Ribbon.Density = RibbonDensity.Touch; Assert.False(f.Ribbon.IsAnimating); Assert.Equal(RibbonDensity.Touch, f.Ribbon.Density);
        using var unseen = new RibbonControl { AnimationLevel = RibbonAnimationLevel.Expressive }; unseen.Density = RibbonDensity.Touch; Assert.False(unseen.IsAnimating);
    });
    [Fact]
    public void Input_interruption_resize_and_policy_change_release_native_holds() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; long style = Style(f.Ribbon);
        f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; var input = Message.Create(f.Ribbon.Handle, 0x201, 1, 0); f.Ribbon.ObserveMotionInput(ref input); Assert.False(f.Ribbon.IsAnimating); Assert.Equal(style, Style(f.Ribbon));
        f.Ribbon.Density = RibbonDensity.Touch; f.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; Assert.False(f.Ribbon.IsAnimating); Assert.Equal(RibbonDensity.Touch, f.Ribbon.Density); Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, f.Ribbon.QuickAccessPosition); Assert.Equal(style, Style(f.Ribbon));
        f.Ribbon.Density = RibbonDensity.Compact; f.Host.Width -= 40; Assert.False(f.Ribbon.IsAnimating); Assert.Equal(style, Style(f.Ribbon));
        f.Ribbon.LayoutMode = RibbonLayoutMode.Classic; f.Ribbon.AnimationLevel = RibbonAnimationLevel.None; Assert.False(f.Ribbon.IsAnimating); Assert.Equal(style, Style(f.Ribbon));
    });
    [Fact]
    public void Invalid_input_and_application_callback_failure_do_not_leave_drawings_or_opacity_behind() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; long style = Style(f.Ribbon); var native = f.Ribbon.Editors.EditorFor(f.Combo)!;
        f.Combo.ValueValidating += (_, e) => e.Cancel = f.Combo.Value == "bad"; f.Combo.PerformClick(); native.Text = "bad"; f.Ribbon.Density = RibbonDensity.Touch; Assert.Equal(RibbonDensity.Compact, f.Ribbon.Density); Assert.False(f.Ribbon.IsAnimating);
        native.Text = "150"; f.Ribbon.LayoutModeChanged += (_, _) => throw new InvalidOperationException("Application handler failed");
        Assert.Throws<InvalidOperationException>(() => f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified); Assert.False(f.Ribbon.IsAnimating); Assert.Equal(style, Style(f.Ribbon)); Assert.Equal("150", f.Combo.Value);
    });
    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Captures_preserve_themed_arrow_clipping_labels_and_unchanged_glyph_weight(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var painter = new ArrowMarker(); f.Ribbon.Renderer = painter; f.Ribbon.Density = density;
        using var from = new RibbonMotionScene(f.Ribbon); using var to = new RibbonMotionScene(f.Ribbon);
        var combo = f.Ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, f.Combo));
        var field = RibbonEditorLayout.Field(combo.Bounds, f.Combo, combo.LabelWidth, f.Ribbon.Font, f.Ribbon.DeviceDpi);
        Assert.Equal(Color.Magenta.ToArgb(), from.Image.GetPixel(field.Right - 5, field.Top + field.Height / 2).ToArgb());
        var label = new Rectangle(combo.Bounds.Left, combo.Bounds.Top, field.Left - combo.Bounds.Left - 3, combo.Bounds.Height);
        Assert.True(Enumerable.Range(label.Top, label.Height).Sum(y => Enumerable.Range(label.Left, label.Width).Count(x => from.Image.GetPixel(x, y).GetBrightness() < .5f)) > 5);
        using var baseline = Render(0);
        var tab = f.Ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.Tab).VisibleBounds;
        using var live = new Bitmap(f.Ribbon.Width, f.Ribbon.Height); f.Ribbon.DrawToBitmap(live, new(Point.Empty, live.Size));
        for (int y = tab.Top; y < tab.Bottom; y++)
        for (int x = tab.Left; x < tab.Right; x++) Assert.Equal(live.GetPixel(x, y), baseline.GetPixel(x, y));
        foreach (double progress in new[] { .25, .5, .75, 1 })
        {
            using var frame = Render(progress);
            foreach (var region in new[] { tab, field, label })
            for (int y = region.Top; y < region.Bottom; y++)
            for (int x = region.Left; x < region.Right; x++)
            {
                var expected = baseline.GetPixel(x, y); var actual = frame.GetPixel(x, y);
                Assert.InRange(Math.Abs(expected.R - actual.R) + Math.Abs(expected.G - actual.G) + Math.Abs(expected.B - actual.B), 0, 6);
            }
        }
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "TestResults", "visual", "m6a-offscreen")); Directory.CreateDirectory(directory);
        from.Image.Save(Path.Combine(directory, density + "-endpoint.png")); baseline.Save(Path.Combine(directory, density + "-frame.png"));
        Bitmap Render(double progress)
        {
            var bitmap = new Bitmap(f.Ribbon.Width, f.Ribbon.Height, PixelFormat.Format32bppPArgb); using var g = Graphics.FromImage(bitmap); using var alpha = new ImageAttributes(); RibbonMotionScene.Render(g, from.Bounds, f.Ribbon, from, to, progress, alpha); return bitmap;
        }
    });
    private sealed class ArrowMarker : RibbonOffice2024Renderer
    {
        public override void DrawComboBox(RibbonPaintContext context, RibbonComboBoxItem item, Rectangle field)
        { base.DrawComboBox(context, item, field); using var brush = new SolidBrush(Color.Magenta); context.Graphics.FillRectangle(brush, new Rectangle(field.Right - 9, field.Top + field.Height / 2 - 3, 6, 6)); }
    }
    [Fact]
    public void Gallery_children_are_captured_and_parent_movement_releases_the_drawing_plane() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var gallery = new RibbonGalleryItem { Text = "Styles", IsInRibbon = true }; gallery.Items.Add(new RibbonGalleryTile { Text = "Style" });
        var group = new RibbonGroup { Text = "Styles" }; group.Items.Add(gallery); f.Ribbon.Tabs[0].Groups.Add(group);
        using var scene = new RibbonMotionScene(f.Ribbon); using var live = new Bitmap(f.Ribbon.Width, f.Ribbon.Height); f.Ribbon.DrawToBitmap(live, new(Point.Empty, live.Size));
        var strip = f.Ribbon.GalleryStrips.SurfaceFor(gallery)!; Assert.True(strip.Visible);
        for (int y = strip.Top; y < strip.Bottom; y++)
        for (int x = strip.Left; x < strip.Right; x++) Assert.Equal(live.GetPixel(x, y), scene.Image.GetPixel(x, y));
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; Assert.True(f.Ribbon.IsAnimating); f.Host.Left += 10; Assert.False(f.Ribbon.IsAnimating);
    });
    [Fact]
    public void Application_transparency_and_a_clipped_destination_preserve_immediate_layout() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; long style = Style(f.Ribbon);
        f.Host.Opacity = .9; f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; Assert.False(f.Ribbon.IsAnimating); Assert.Equal(.9, f.Host.Opacity); Assert.Equal(style, Style(f.Ribbon));
        f.Host.Opacity = 1; f.Ribbon.AnimationLevel = RibbonAnimationLevel.None; f.Ribbon.LayoutMode = RibbonLayoutMode.Classic;
        f.Host.ClientSize = new(f.Host.ClientSize.Width, f.Ribbon.Height + 2); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.Density = RibbonDensity.Touch;
        Assert.Equal(RibbonDensity.Touch, f.Ribbon.Density); Assert.False(f.Ribbon.IsAnimating); Assert.Equal(style, Style(f.Ribbon));
    });
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint window, int index);
}
