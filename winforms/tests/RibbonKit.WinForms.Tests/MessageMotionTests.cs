using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MessageMotionTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly Form Host = new() { Size = new(1050, 700) };
        internal readonly RibbonControl Ribbon;
        internal readonly FrameRecord Record = new();
        internal readonly RibbonComboBoxItem Combo = new() { Text = "Font", IsEditable = true, Value = "Draft" };
        internal Fixture(bool office)
        {
            Application.EnableVisualStyles(); Ribbon = new() { Renderer = office ? new Office(Record) : new Neutral(Record) };
            var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" }; group.Items.Add(Combo); tab.Groups.Add(group); Ribbon.Tabs.Add(tab);
            Ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save", Icon = new Icon(Color.Cyan) }); Host.Controls.Add(Ribbon); Host.Show(); Application.DoEvents(); Ribbon.Motion.SystemPolicyOverride = true;
        }
        public void Dispose() { var renderer = Ribbon.Renderer; Host.Dispose(); renderer?.Dispose(); }
    }
    private sealed class Icon(Color color) : IRibbonIcon { public void Draw(Graphics g, Rectangle b, RibbonIconContext c) { using var brush = new SolidBrush(color); g.FillRectangle(brush, b); } }
    private sealed class FrameRecord { internal Rectangle Panel; internal bool Top, Bottom, MessageBottom; }
    private sealed class Office(FrameRecord r) : RibbonOffice2024Renderer
    {
        public override void DrawMessageBar(RibbonPaintContext c) { r.Panel = c.Bounds; r.Top = c.IsTopEdgeConnected; r.MessageBottom = c.IsBottomEdgeConnected; base.DrawMessageBar(c); }
        public override void DrawBody(RibbonPaintContext c) { r.Bottom = c.IsBottomEdgeConnected; base.DrawBody(c); }
    }
    private sealed class Neutral(FrameRecord r) : RibbonRenderer
    {
        public override void DrawMessageBar(RibbonPaintContext c) { r.Panel = c.Bounds; r.Top = c.IsTopEdgeConnected; r.MessageBottom = c.IsBottomEdgeConnected; base.DrawMessageBar(c); }
    }
    private static RibbonMessage Row(string text, Color color) => new() { Title = text, Text = "Application-owned notification", Icon = new Icon(color) };

    [Theory]
    [InlineData(true, RibbonQuickAccessPosition.BelowRibbon, false, RibbonDensity.Compact)]
    [InlineData(true, RibbonQuickAccessPosition.TabRow, true, RibbonDensity.Touch)]
    [InlineData(false, RibbonQuickAccessPosition.BelowRibbon, true, RibbonDensity.Touch)]
    [InlineData(false, RibbonQuickAccessPosition.TabRow, false, RibbonDensity.Compact)]
    public void Arrival_and_close_commit_once_and_preserve_native_editors(bool office, RibbonQuickAccessPosition placement, bool minimized, RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(office); var ribbon = f.Ribbon; ribbon.QuickAccessPosition = placement; ribbon.Density = density;
        var editor = ribbon.Editors.EditorFor(f.Combo)!; nint handle = editor.Handle; var parent = f.Combo.Parent; ribbon.IsMinimized = minimized;
        int baseline = ribbon.Height; var body = ribbon.Snapshot.BodyBounds; var qat = ribbon.Snapshot.QuickAccessBounds;
        var row = Row("Notice", Color.Lime); int changes = 0, dismissed = 0; row.IsOpenChanged += (_, _) => changes++; row.Dismissed += (_, _) => dismissed++;
        ribbon.AnimationLevel = density == RibbonDensity.Touch ? RibbonAnimationLevel.Expressive : RibbonAnimationLevel.Subtle;
        ribbon.MessageBar.Items.Add(row); Assert.True(ribbon.IsAnimating); Assert.True(ribbon.Height > baseline); Assert.Same(row, Assert.Single(ribbon.Snapshot.Messages).Message); Assert.Equal(body, ribbon.Snapshot.BodyBounds); Assert.Equal(qat, ribbon.Snapshot.QuickAccessBounds);
        ribbon.SettleMotion(); int started = ribbon.Motion.StartedCount;
        ribbon.FocusElement(row.Close); ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.MessageClose));
        Assert.False(row.IsOpen); Assert.Equal(1, changes); Assert.Equal(1, dismissed); Assert.Equal(baseline, ribbon.Height); Assert.True(ribbon.IsAnimating); Assert.Equal(started + 1, ribbon.Motion.StartedCount); Assert.NotSame(row.Close, ribbon.FocusedElement);
        ribbon.SettleMotion(); row.Dismiss(); Assert.False(ribbon.IsAnimating); Assert.Equal(1, dismissed);
        row.IsOpen = true; Assert.True(ribbon.IsAnimating); Assert.Equal(2, changes); ribbon.SettleMotion();
        ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsMinimized = false;
        Assert.Same(editor, ribbon.Editors.EditorFor(f.Combo)); Assert.Equal(handle, editor.Handle); Assert.Same(parent, f.Combo.Parent); Assert.Equal("Draft", f.Combo.Value);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Batches_opposite_requests_visibility_disposal_and_policy_settle_to_the_latest_state(bool office) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(office); var ribbon = f.Ribbon; int baseline = ribbon.Height; var first = Row("First", Color.Lime); var second = Row("Second", Color.Magenta);
        ribbon.MessageBar.Items.Add(first); Assert.False(ribbon.IsAnimating); first.IsOpen = false;
        ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; ribbon.Motion.SystemPolicyOverride = false; first.IsOpen = true; Assert.False(ribbon.IsAnimating); first.IsOpen = false; ribbon.Motion.SystemPolicyOverride = true;
        ribbon.BeginUpdate(); first.IsOpen = true; ribbon.MessageBar.Items.Add(second); ribbon.EndUpdate(); Assert.False(ribbon.IsAnimating);
        first.Dismiss(); Assert.True(ribbon.IsAnimating); second.Dismiss(); Assert.True(ribbon.IsAnimating); Assert.Empty(ribbon.Snapshot.Messages); Assert.Equal(1, ribbon.Motion.StartedCount); ribbon.SettleMotion();
        first.IsOpen = true; Assert.True(ribbon.IsAnimating); first.IsOpen = false; Assert.False(ribbon.IsAnimating); Assert.Equal(baseline, ribbon.Height);
        first.IsOpen = second.IsOpen = true; ribbon.SettleMotion(); ribbon.MessageBar.Visible = false; Assert.True(ribbon.IsAnimating); ribbon.SettleMotion(); ribbon.MessageBar.Visible = true; Assert.True(ribbon.IsAnimating); ribbon.SettleMotion();
        second.Visible = false; Assert.True(ribbon.IsAnimating); ribbon.SettleMotion(); second.Visible = true; Assert.True(ribbon.IsAnimating); ribbon.SettleMotion();
        first.Dispose(); Assert.False(ribbon.IsAnimating); Assert.Single(ribbon.Snapshot.Messages); ribbon.MessageBar.Items.Clear(); Assert.True(ribbon.IsAnimating); Assert.Equal(baseline, ribbon.Height); ribbon.SettleMotion();
        int started = ribbon.Motion.StartedCount; ribbon.MessageBar.Items.AddRange(Row("One", Color.Lime), Row("Two", Color.Magenta), Row("Three", Color.Blue)); Assert.True(ribbon.IsAnimating); Assert.Equal(started + 1, ribbon.Motion.StartedCount); Assert.Equal(3, ribbon.Snapshot.Messages.Count);
        var input = Message.Create(ribbon.Handle, 0x100, (nint)Keys.Tab, 0); ribbon.ObserveMotionInput(ref input); Assert.False(ribbon.IsAnimating);
        ribbon.MessageBar.Items.Clear(); Assert.True(ribbon.IsAnimating); f.Host.Width -= 20; Assert.False(ribbon.IsAnimating); Assert.Equal(baseline, ribbon.Height);
        ribbon.MessageBar.Items.Add(first = Row("Resize", Color.Lime)); Assert.True(ribbon.IsAnimating); ribbon.AnimationLevel = RibbonAnimationLevel.None; Assert.False(ribbon.IsAnimating); Assert.True(first.IsOpen);
    });

    [Theory]
    [InlineData(true, RibbonQuickAccessPosition.BelowRibbon, false, true)]
    [InlineData(true, RibbonQuickAccessPosition.BelowRibbon, true, true)]
    [InlineData(true, RibbonQuickAccessPosition.TabRow, true, false)]
    [InlineData(false, RibbonQuickAccessPosition.TabRow, false, true)]
    public void Drawing_reveals_natural_rows_moves_survivors_and_keeps_the_dynamic_frame_join(bool office, RibbonQuickAccessPosition placement, bool minimized, bool qatVisible) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(office); var ribbon = f.Ribbon; ribbon.QuickAccessPosition = placement; ribbon.QuickAccessVisible = qatVisible; ribbon.IsMinimized = minimized;
        ribbon.Density = minimized ? RibbonDensity.Touch : RibbonDensity.Compact;
        var first = Row("First", Color.Lime); var second = Row("Second", Color.Magenta); ribbon.MessageBar.Items.AddRange(first, second);
        using var both = new RibbonMotionScene(ribbon); first.Dismiss(); using var one = new RibbonMotionScene(ribbon); second.Dismiss(); using var empty = new RibbonMotionScene(ribbon);
        ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        foreach (var (from, to, name) in new[] { (empty, both, "show"), (both, one, "dismiss-first"), (both, empty, "clear") })
        foreach (double progress in new[] { 0d, .25, .5, .75, 1 })
        {
            var canvas = Rectangle.Union(from.Bounds, to.Bounds); using var frame = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format32bppPArgb); using var g = Graphics.FromImage(frame); using var alpha = new ImageAttributes();
            f.Record.Panel = Rectangle.Empty; f.Record.Top = f.Record.Bottom = false; RibbonMotionScene.Render(g, canvas, ribbon, from, to, progress, alpha);
            double p = 1 - Math.Pow(1 - progress, 3);
            var panel = RibbonMotionScene.Between(from.Messages.IsEmpty ? to.Messages with { Height = 0 } : from.Messages, to.Messages.IsEmpty ? from.Messages with { Height = 0 } : to.Messages, p);
            bool connected = panel.Height > 0 && office && (!minimized || placement == RibbonQuickAccessPosition.BelowRibbon && qatVisible);
            if (panel.Height > 0) { Assert.Equal(panel, f.Record.Panel); Assert.Equal(connected, f.Record.Top); Assert.False(f.Record.MessageBottom); }
            Assert.Equal(connected, f.Record.Bottom);
            var expected = to.MessageRows.TryGetValue(second, out var survivor) ? survivor : both.MessageRows[second];
            var secondInk = Ink(frame, c => c.R > 100 && c.B > 100 && c.G < 30); var naturalInk = Ink(both.Image, c => c.R > 100 && c.B > 100 && c.G < 30);
            if (!secondInk.IsEmpty) { Assert.True(panel.Contains(secondInk)); Assert.Equal(naturalInk.Width, secondInk.Width); }
            if (name == "dismiss-first")
            {
                var source = both.MessageRows[second]; var position = RibbonMotionScene.Between(RibbonMotionScene.At(source.Bounds, from.Bounds), RibbonMotionScene.At(expected.Bounds, to.Bounds), p); position.Offset(-canvas.X, -canvas.Y); var expectedInk = naturalInk; expectedInk.Offset(position.Location - (Size)source.Bounds.Location); Assert.Equal(expectedInk, secondInk);
            }
            if (panel.Height == 0) Assert.True(secondInk.IsEmpty);
            if (progress == .5)
            {
                string directory = Path.Combine("TestResults", "visual", "m6a-messages"); Directory.CreateDirectory(directory); frame.Save(Path.Combine(directory, $"{office}-{placement}-{minimized}-{qatVisible}-{name}.png"));
            }
        }
    });

    [Fact]
    public void Throwing_dismissal_callbacks_release_the_drawing_and_keep_committed_state() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(true); var ribbon = f.Ribbon; var row = Row("Notice", Color.Lime); ribbon.MessageBar.Items.Add(row); ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        row.Dismissed += (_, _) => throw new InvalidOperationException("Application callback");
        Assert.Throws<InvalidOperationException>(row.Dismiss); Assert.False(row.IsOpen); Assert.False(ribbon.IsAnimating); Assert.False(RibbonDrawingHold.HasOpacity(ribbon.Handle)); Assert.Single(ribbon.MessageBar.Items);
    });

    private static Rectangle Ink(Bitmap image, Func<Color, bool> predicate)
    {
        var data = image.LockBits(new(Point.Empty, image.Size), ImageLockMode.ReadOnly, image.PixelFormat); int bytes = Image.GetPixelFormatSize(image.PixelFormat) / 8;
        int left = image.Width, top = image.Height, right = -1, bottom = -1;
        var pixels = new byte[Math.Abs(data.Stride)];
        try { for (int y = 0; y < image.Height; y++) { System.Runtime.InteropServices.Marshal.Copy(data.Scan0 + y * data.Stride, pixels, 0, pixels.Length); for (int x = 0; x < image.Width; x++) { int i = x * bytes; if (predicate(Color.FromArgb(pixels[i + 2], pixels[i + 1], pixels[i]))) { left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y); } } } }
        finally { image.UnlockBits(data); }
        return right < 0 ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }
}
