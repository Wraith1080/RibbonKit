using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GroupMotionTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly Form Form = new() { Size = new(1500, 750), StartPosition = FormStartPosition.CenterScreen };
        internal readonly RibbonControl Ribbon = new() { Dock = DockStyle.None, Width = 1180 };
        internal readonly RibbonGroup Group = new() { Id = "commands", Text = "Commands", Icon = new InkIcon(Color.Blue), ReductionPriority = 10 };
        internal readonly RibbonComboBoxItem Combo = new() { Id = "font", Text = "Font", IsEditable = true, Value = "Live", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        internal readonly RibbonToggleItem Toggle = new() { Id = "bold", Text = "Bold", Icon = new InkIcon(Color.Red), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        internal Fixture(RibbonDensity density = RibbonDensity.Compact, bool simplified = false)
        {
            Ribbon.Density = density; Ribbon.LayoutMode = simplified ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic;
            Group.Items.AddRange(Toggle, Combo, new RibbonButtonItem { Id = "copy", Text = "Copy", Icon = new InkIcon(Color.Red), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline });
            var tab = new RibbonTab { Id = "home", Text = "Home" }; tab.Groups.Add(Group);
            var other = new RibbonGroup { Id = "other", Text = "Other", CanResize = false }; other.Items.Add(new RibbonButtonItem { Id = "next", Text = "Next command" }); tab.Groups.Add(other);
            Ribbon.Tabs.Add(tab); Ribbon.CommandCatalog.CaptureDefaults(); Form.Controls.Add(Ribbon); Form.Show(); Form.Activate(); SetForegroundWindow(Form.Handle); Application.DoEvents(); Ribbon.Motion.SystemPolicyOverride = true; Ribbon.GroupMotion.ActiveOwnerOverride = true;
        }
        internal void Finish() { var watch = Stopwatch.StartNew(); while (Ribbon.IsAnimating && watch.ElapsedMilliseconds < 1000) { Application.DoEvents(); Thread.Sleep(1); } Assert.False(Ribbon.IsAnimating); }
        internal int NarrowWidth()
        {
            for (int width = 1000; width >= 220; width -= 10)
                if (Ribbon.Engine.BuildBody(Ribbon, Ribbon.SelectedTab!, width, 0).Groups.Any(g => g.Group == Group && g.Collapsed)) return width;
            throw new InvalidOperationException("No collapse threshold");
        }
        public void Dispose() { Ribbon.Dispose(); Form.Dispose(); }
    }
    private sealed class InkIcon(Color color) : IRibbonIcon
    { public void Draw(Graphics g, Rectangle b, RibbonIconContext c) { using var brush = new SolidBrush(color); g.FillRectangle(brush, Rectangle.Inflate(b, -5, -5)); } }

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Classic_adaptive_groups_animate_both_directions_and_keep_editor_identity(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(density); int narrow = f.NarrowWidth(); nint handle = f.Ribbon.Editors.EditorFor(f.Combo)!.Handle;
        using var edit = f.Ribbon.CommandCatalog.BeginCustomization("group.motion"); string profile = edit.ExportJson();
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; int started = f.Ribbon.GroupMotion.StartedCount;
        f.Ribbon.Width = narrow; Assert.True(f.Ribbon.GroupMotion.IsRunning); Assert.True(f.Ribbon.Snapshot.Groups.Single(g => g.Group == f.Group).Collapsed);
        f.Ribbon.Width -= 2; Assert.True(f.Ribbon.GroupMotion.IsRunning); Assert.Equal(started + 1, f.Ribbon.GroupMotion.StartedCount); f.Finish();
        f.Ribbon.Width = 1180; Assert.True(f.Ribbon.GroupMotion.IsRunning); Assert.False(f.Ribbon.Snapshot.Groups.Single(g => g.Group == f.Group).Collapsed); f.Finish();
        Assert.Equal(handle, f.Ribbon.Editors.EditorFor(f.Combo)!.Handle); Assert.Equal("Live", f.Combo.Value); Assert.Equal(profile, edit.ExportJson());
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Simplified_borrowing_and_restoring_animate_root_and_temporary_body_without_cloning(bool minimized) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(simplified: true); f.Ribbon.IsMinimized = minimized; RibbonPopupSurface? body = null;
        if (minimized) { f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => t.Element == f.Ribbon.SelectedTab)); body = f.Ribbon.Popups.Top!; }
        IReadOnlyList<RibbonTarget> Targets() => body?.Targets ?? f.Ribbon.Snapshot.Targets;
        nint handle = f.Ribbon.Editors.EditorFor(f.Combo)!.Handle; using var edit = f.Ribbon.CommandCatalog.BeginCustomization("group.motion"); string profile = edit.ExportJson();
        f.Ribbon.Activate(Targets().Single(t => t.Element == f.Ribbon.Simplified.More), body); var more = f.Ribbon.Popups.Top!;
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
        f.Ribbon.Activate(more.Targets.Single(t => t.Element is RibbonQuickAccessGroupReference r && r.Source == f.Group), more);
        Assert.True(f.Ribbon.GroupMotion.IsRunning, "Opening: " + f.Ribbon.GroupMotion.LastSkipReason); Assert.Contains(Targets(), t => t.Element == f.Group); f.Finish();
        Assert.True(f.Ribbon.Popups.BorrowsGroup(f.Group), "The group stays open after motion");
        Assert.Equal(handle, f.Ribbon.Editors.EditorFor(f.Combo)!.Handle); f.Combo.Value = "Changed"; f.Toggle.Checked = true;
        Assert.True(f.Ribbon.Popups.BorrowsGroup(f.Group), "The live group remains borrowed before restoring");
        f.Ribbon.Popups.CloseTop(false); Assert.True(f.Ribbon.GroupMotion.IsRunning, "Restoring: " + f.Ribbon.GroupMotion.LastSkipReason); Assert.Contains(Targets(), t => t.Element == f.Combo); f.Finish();
        Assert.Equal(handle, f.Ribbon.Editors.EditorFor(f.Combo)!.Handle); Assert.True(f.Toggle.Checked); Assert.Equal("Changed", f.Combo.Value); Assert.Equal(profile, edit.ExportJson());
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Simplified_width_overflow_and_return_animate_without_changing_pins(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(density, simplified: true); nint handle = f.Ribbon.Editors.EditorFor(f.Combo)!.Handle;
        using var edit = f.Ribbon.CommandCatalog.BeginCustomization("group.motion"); string profile = edit.ExportJson();
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.Width = 250;
        Assert.True(f.Ribbon.GroupMotion.IsRunning, f.Ribbon.GroupMotion.LastSkipReason); Assert.DoesNotContain(f.Ribbon.Snapshot.Targets, t => t.Element == f.Combo); f.Finish();
        f.Ribbon.Width = 1180; Assert.True(f.Ribbon.GroupMotion.IsRunning); f.Finish();
        Assert.Contains(f.Ribbon.Snapshot.Targets, t => t.Element == f.Combo); Assert.Equal(handle, f.Ribbon.Editors.EditorFor(f.Combo)!.Handle); Assert.Equal(profile, edit.ExportJson());
    });

    [Fact]
    public void Group_policy_and_interruptions_leave_only_committed_live_state() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); int narrow = f.NarrowWidth();
        f.Ribbon.Width = narrow; Assert.False(f.Ribbon.IsAnimating); f.Ribbon.Width = 1180;
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.Motion.SystemPolicyOverride = false; f.Ribbon.Width = narrow; Assert.False(f.Ribbon.IsAnimating);
        f.Ribbon.Motion.SystemPolicyOverride = true; f.Ribbon.GroupMotion.ActiveOwnerOverride = false; f.Ribbon.Width = 1180; Assert.False(f.Ribbon.GroupMotion.IsRunning); f.Ribbon.Width = narrow; f.Ribbon.GroupMotion.ActiveOwnerOverride = true;
        f.Ribbon.Motion.SystemPolicyOverride = true; f.Ribbon.Width = 1180; Assert.True(f.Ribbon.GroupMotion.IsRunning);
        var input = Message.Create(f.Ribbon.Handle, 0x201, 0, 0); f.Ribbon.ObserveMotionInput(ref input); Assert.False(f.Ribbon.GroupMotion.IsRunning);
        f.Ribbon.Width = narrow; Assert.True(f.Ribbon.GroupMotion.IsRunning); f.Ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown; Assert.False(f.Ribbon.GroupMotion.IsRunning); f.Ribbon.Width = 1180; f.Ribbon.SettleMotion();
        f.Ribbon.Width = narrow; Assert.True(f.Ribbon.GroupMotion.IsRunning); f.Ribbon.AnimationLevel = RibbonAnimationLevel.None; Assert.False(f.Ribbon.GroupMotion.IsRunning);
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.BeginUpdate(); f.Ribbon.Width = 1180; f.Ribbon.EndUpdate(); Assert.False(f.Ribbon.GroupMotion.IsRunning);
        f.Ribbon.Width = narrow; Assert.True(f.Ribbon.GroupMotion.IsRunning); f.Ribbon.Visible = false; Assert.False(f.Ribbon.GroupMotion.IsRunning);
        f.Ribbon.Visible = true; f.Ribbon.Width = 1180; Assert.True(f.Ribbon.GroupMotion.IsRunning); f.Ribbon.Dispose(); Assert.False(f.Ribbon.GroupMotion.IsRunning);
    });
    [Fact]
    public void Fade_group_drawing_blends_fixed_natural_size_ink_without_horizontal_travel() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var wide = new RibbonGroupScene(f.Ribbon, f.Ribbon, f.Ribbon.Snapshot); f.Ribbon.Width = f.NarrowWidth(); using var narrow = new RibbonGroupScene(f.Ribbon, f.Ribbon, f.Ribbon.Snapshot);
        var ink = Ink(narrow.Image, Color.Blue); var point = new Point(ink.Left + ink.Width / 2, ink.Top + ink.Height / 2);
        var old = wide.Image.GetPixel(point.X, point.Y); var next = narrow.Image.GetPixel(point.X, point.Y);
        foreach (double progress in new[] { 0d, .25, .5, 1 })
        {
            using var frame = new Bitmap(narrow.Viewport.Width, narrow.Viewport.Height); using var g = Graphics.FromImage(frame);
            RibbonGroupScene.Draw(g, wide, narrow, progress, 40, f.Ribbon.Theme.Surface, false);
            double p = 1 - Math.Pow(1 - progress, 3); var actual = frame.GetPixel(point.X - narrow.Viewport.X, point.Y - narrow.Viewport.Y);
            Assert.InRange(Math.Abs(actual.R - (old.R + (next.R - old.R) * p)), 0, 2);
            Assert.InRange(Math.Abs(actual.G - (old.G + (next.G - old.G) * p)), 0, 2);
            Assert.InRange(Math.Abs(actual.B - (old.B + (next.B - old.B) * p)), 0, 2);
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Simplified_immediate_fallback_commits_once_without_reentering_popup_open(bool systemEnabled) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(simplified: true); f.Ribbon.Motion.SystemPolicyOverride = systemEnabled;
        f.Ribbon.AnimationLevel = systemEnabled ? RibbonAnimationLevel.None : RibbonAnimationLevel.Subtle;
        f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => t.Element == f.Ribbon.Simplified.More)); var more = f.Ribbon.Popups.Top!;
        f.Ribbon.Activate(more.Targets.Single(t => t.Element is RibbonQuickAccessGroupReference r && r.Source == f.Group), more);
        Assert.True(f.Ribbon.Popups.BorrowsGroup(f.Group)); Assert.False(f.Ribbon.IsAnimating);
        f.Ribbon.Popups.CloseTop(false); Assert.False(f.Ribbon.IsAnimating); Assert.False(f.Ribbon.Popups.BorrowsGroup(f.Group));
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Group_drawings_slide_left_then_right_with_natural_size_opaque_ink(int dpi) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var wide = new RibbonGroupScene(f.Ribbon, f.Ribbon, f.Ribbon.Snapshot); f.Ribbon.Width = f.NarrowWidth(); using var narrow = new RibbonGroupScene(f.Ribbon, f.Ribbon, f.Ribbon.Snapshot);
        int travel = RibbonLayoutEngine.Scale(20, dpi);
        int previous = int.MaxValue;
        foreach (double p in new[] { 0d, .04, .08 })
        {
            using var frame = Draw(wide, narrow, p); var blue = Ink(frame, Color.Blue); Assert.False(blue.IsEmpty); Assert.True(blue.Left <= previous); previous = blue.Left;
        }
        int first = previous; using (var initial = Draw(wide, narrow, 0)) Assert.True(first < Ink(initial, Color.Blue).Left);
        previous = int.MinValue;
        foreach (double p in new[] { .35, .65, 1 })
        {
            using var frame = Draw(narrow, wide, p); var red = Ink(frame, Color.Red); Assert.False(red.IsEmpty); Assert.True(red.Left >= previous); previous = red.Left;
            Assert.Equal(255, frame.GetPixel(red.Left, red.Top).A);
        }
        Bitmap Draw(RibbonGroupScene from, RibbonGroupScene to, double p) { var result = new Bitmap(to.Viewport.Width, to.Viewport.Height, PixelFormat.Format32bppPArgb); using var g = Graphics.FromImage(result); RibbonGroupScene.Draw(g, from, to, p, travel, f.Ribbon.Theme.Surface); return result; }
    });
    [Fact]
    public void Minimized_simplified_QAT_group_without_a_body_keeps_ordinary_popup_behavior() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(simplified: true); var reference = new RibbonQuickAccessGroupReference { Source = f.Group }; f.Ribbon.QuickAccessItems.Add(reference); f.Ribbon.IsMinimized = true;
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => t.Element == reference));
        Assert.True(f.Ribbon.Popups.BorrowsGroup(f.Group)); Assert.False(f.Ribbon.GroupMotion.IsRunning); f.Finish(); f.Ribbon.Popups.CloseTop(false); Assert.False(f.Ribbon.GroupMotion.IsRunning);
    });
    private static Rectangle Ink(Bitmap image, Color color)
    {
        int l = image.Width, t = image.Height, r = -1, b = -1;
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() == color.ToArgb()) { l = Math.Min(l, x); r = Math.Max(r, x); t = Math.Min(t, y); b = Math.Max(b, y); }
        return r < l ? Rectangle.Empty : Rectangle.FromLTRB(l, t, r + 1, b + 1);
    }
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
}
