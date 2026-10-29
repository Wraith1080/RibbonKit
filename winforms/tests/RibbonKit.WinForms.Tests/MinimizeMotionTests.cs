using System.Diagnostics;
using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MinimizeMotionTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly Form Host = new() { Size = new(1200, 700) };
        internal readonly RibbonControl Ribbon;
        internal readonly RibbonComboBoxItem Combo = new() { Id = "font", Text = "Font", IsEditable = true, Value = "100", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        internal readonly RibbonMessage Message = new() { Title = "Notice", Text = "Application-owned message", Icon = new SolidIcon(Color.Lime) };
        internal Fixture(bool office, RibbonLayoutMode mode)
        {
            Application.EnableVisualStyles(); Ribbon = new() { BindingContext = new BindingContext(), LayoutMode = mode, Renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer() };
            var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" };
            group.Items.Add(new RibbonButtonItem { Text = "Copy", Icon = new SolidIcon(Color.Magenta), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline });
            Combo.Items.AddRange("100", "150"); group.Items.Add(Combo); tab.Groups.Add(group); Ribbon.Tabs.Add(tab);
            Ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save", Icon = new SolidIcon(Color.Cyan) }); Ribbon.MessageBar.Items.Add(Message);
            Host.Controls.Add(Ribbon); Host.Show(); Application.DoEvents(); Ribbon.Motion.SystemPolicyOverride = true;
        }
        internal void Finish() { var watch = Stopwatch.StartNew(); while (Ribbon.IsAnimating && watch.ElapsedMilliseconds < 1500) { Application.DoEvents(); Thread.Sleep(1); } Assert.False(Ribbon.IsAnimating); }
        public void Dispose() { var painter = Ribbon.Renderer; Host.Dispose(); painter?.Dispose(); }
    }
    private sealed class SolidIcon(Color color) : IRibbonIcon { public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { using var brush = new SolidBrush(color); graphics.FillRectangle(brush, bounds); } }

    [Theory]
    [InlineData(true, RibbonLayoutMode.Classic, RibbonAnimationLevel.Subtle)]
    [InlineData(true, RibbonLayoutMode.Simplified, RibbonAnimationLevel.Expressive)]
    [InlineData(false, RibbonLayoutMode.Classic, RibbonAnimationLevel.Expressive)]
    [InlineData(false, RibbonLayoutMode.Simplified, RibbonAnimationLevel.Subtle)]
    public void Minimize_and_restore_commit_once_and_keep_qat_messages_and_editor_identity(bool office, RibbonLayoutMode mode, RibbonAnimationLevel level) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(office, mode); var ribbon = f.Ribbon; var selected = ribbon.SelectedTab; var owner = f.Combo.Parent; nint handle = ribbon.Editors.EditorFor(f.Combo)!.Handle; int changes = 0;
        ribbon.IsMinimizedChanged += (_, _) => changes++;
        foreach (var placement in Enum.GetValues<RibbonQuickAccessPosition>())
        foreach (bool messages in new[] { false, true })
        {
            ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.QuickAccessPosition = placement; f.Message.IsOpen = messages; ribbon.Density = messages ? RibbonDensity.Touch : RibbonDensity.Compact;
            int expanded = ribbon.Height, count = changes, started = ribbon.Motion.StartedCount; ribbon.AnimationLevel = level;
            ribbon.IsMinimized = true; Assert.True(ribbon.IsAnimating); Assert.True(ribbon.Height < expanded); Assert.Empty(ribbon.Snapshot.Groups); Assert.Equal(count + 1, changes); Assert.Equal(started + 1, ribbon.Motion.StartedCount);
            Assert.Equal(messages, !ribbon.Snapshot.MessageBounds.IsEmpty); Assert.Equal(placement == RibbonQuickAccessPosition.BelowRibbon, ribbon.Snapshot.QuickAccessBounds.Top >= ribbon.Snapshot.TabViewport.Bottom);
            f.Finish(); Assert.True(ribbon.Motion.FrameIntervals.Count >= 2);
            ribbon.IsMinimized = false; Assert.True(ribbon.IsAnimating); Assert.Equal(expanded, ribbon.Height); Assert.True(ribbon.Snapshot.BodyBounds.Height > 0); Assert.Equal(count + 2, changes); f.Finish();
            Assert.Equal(handle, ribbon.Editors.EditorFor(f.Combo)!.Handle); Assert.Same(owner, f.Combo.Parent); Assert.Same(selected, ribbon.SelectedTab); Assert.Equal("100", f.Combo.Value);
        }
    });

    [Theory]
    [InlineData(true, RibbonLayoutMode.Classic, RibbonQuickAccessPosition.TabRow)] [InlineData(true, RibbonLayoutMode.Classic, RibbonQuickAccessPosition.BelowRibbon)]
    [InlineData(true, RibbonLayoutMode.Simplified, RibbonQuickAccessPosition.TabRow)] [InlineData(true, RibbonLayoutMode.Simplified, RibbonQuickAccessPosition.BelowRibbon)]
    [InlineData(false, RibbonLayoutMode.Classic, RibbonQuickAccessPosition.TabRow)] [InlineData(false, RibbonLayoutMode.Classic, RibbonQuickAccessPosition.BelowRibbon)]
    [InlineData(false, RibbonLayoutMode.Simplified, RibbonQuickAccessPosition.TabRow)] [InlineData(false, RibbonLayoutMode.Simplified, RibbonQuickAccessPosition.BelowRibbon)]
    public void Drawing_reveals_opaque_natural_size_body_and_moves_intact_qat_and_messages(bool office, RibbonLayoutMode mode, RibbonQuickAccessPosition placement) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(office, mode); var ribbon = f.Ribbon; ribbon.QuickAccessPosition = placement;
        using var expanded = new RibbonMotionScene(ribbon); ribbon.IsMinimized = true; using var collapsed = new RibbonMotionScene(ribbon); ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        var canvas = Rectangle.Union(expanded.Bounds, collapsed.Bounds); var originalBody = Ink(expanded.Image, Color.Magenta); var originalQat = Ink(expanded.Image, Color.Cyan); var originalMessage = Ink(expanded.Image, Color.Lime);
        Assert.False(originalBody.IsEmpty); Assert.False(originalQat.IsEmpty); Assert.False(originalMessage.IsEmpty);
        foreach (bool restore in new[] { false, true })
        foreach (double progress in new[] { 0, .25, .5, .75, 1 })
        {
            var from = restore ? collapsed : expanded; var to = restore ? expanded : collapsed;
            using var frame = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format32bppPArgb); using var graphics = Graphics.FromImage(frame); using var alpha = new ImageAttributes();
            RibbonMotionScene.Render(graphics, canvas, ribbon, from, to, progress, alpha); double p = 1 - Math.Pow(1 - progress, 3);
            var body = RibbonMotionScene.Between(from.Body.IsEmpty ? expanded.Body with { Height = 0 } : from.Body, to.Body.IsEmpty ? expanded.Body with { Height = 0 } : to.Body, p);
            int inset = RibbonLayoutEngine.Scale(4, ribbon.DeviceDpi); var clip = Rectangle.Inflate(body, -inset, -inset); var visibleBody = Ink(frame, Color.Magenta);
            if (!visibleBody.IsEmpty) { Assert.True(clip.Contains(visibleBody)); Assert.Equal(originalBody.Width, visibleBody.Width); Assert.Equal(originalBody.Left, visibleBody.Left); }
            if (body.Height == expanded.Body.Height) Assert.Equal(originalBody, visibleBody);
            if (body.Height <= 2 * inset) Assert.True(visibleBody.IsEmpty);
            var qat = originalQat; qat.Offset(0, (int)Math.Round((to.Qat.Y - from.Qat.Y) * p) + from.Qat.Y - expanded.Qat.Y); Assert.Equal(qat, Ink(frame, Color.Cyan));
            var message = originalMessage; message.Offset(0, (int)Math.Round((to.Messages.Y - from.Messages.Y) * p) + from.Messages.Y - expanded.Messages.Y); Assert.Equal(message, Ink(frame, Color.Lime));
            if (progress == .5) { string directory = Path.Combine("TestResults", "visual", "m6a-minimize"); Directory.CreateDirectory(directory); frame.Save(Path.Combine(directory, $"{office}-{mode}-{placement}-{restore}.png")); }
        }
    });

    [Fact]
    public void Minimize_policies_interruptions_popup_and_modal_paths_settle_without_leaving_a_drawing() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(true, RibbonLayoutMode.Classic); var ribbon = f.Ribbon; int expanded = ribbon.Height;
        ribbon.IsMinimized = true; Assert.False(ribbon.IsAnimating); ribbon.IsMinimized = false;
        ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.Motion.SystemPolicyOverride = false; ribbon.IsMinimized = true; Assert.False(ribbon.IsAnimating);
        ribbon.Motion.SystemPolicyOverride = true; ribbon.BeginUpdate(); ribbon.IsMinimized = false; ribbon.EndUpdate(); Assert.False(ribbon.IsAnimating);
        ribbon.IsMinimized = true; Assert.True(ribbon.IsAnimating); ribbon.IsMinimized = false; Assert.False(ribbon.IsAnimating); Assert.Equal(expanded, ribbon.Height);
        ribbon.IsMinimized = true; Assert.True(ribbon.IsAnimating); f.Host.Width -= 20; Assert.False(ribbon.IsAnimating);
        ribbon.IsMinimized = false; Assert.True(ribbon.IsAnimating); ribbon.AnimationLevel = RibbonAnimationLevel.None; Assert.False(ribbon.IsAnimating);
        ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; ribbon.IsMinimized = true; Assert.True(ribbon.IsAnimating);
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, ribbon.SelectedTab))); Assert.False(ribbon.IsAnimating); Assert.True(ribbon.Popups.HasOpen); ribbon.Popups.CloseAll(false);
        var modal = new RibbonTab { Text = "Preview", IsModal = true }; ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.Tabs.Add(modal); ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        Assert.True(ribbon.EnterModal(modal)); Assert.False(ribbon.IsMinimized); Assert.False(ribbon.IsAnimating); Assert.True(ribbon.ExitModal()); Assert.True(ribbon.IsMinimized); Assert.False(ribbon.IsAnimating);
        using var startup = new RibbonControl { AnimationLevel = RibbonAnimationLevel.Subtle }; startup.IsMinimized = true; Assert.False(startup.IsAnimating);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Utility_chevron_uses_committed_direction_and_hidden_qat_preserves_the_message(bool office) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(office, RibbonLayoutMode.Classic); var ribbon = f.Ribbon; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; ribbon.QuickAccessVisible = false;
        using var expanded = new RibbonMotionScene(ribbon); ribbon.IsMinimized = true; using var collapsed = new RibbonMotionScene(ribbon); ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        var button = ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.Minimize).VisibleBounds; var canvas = Rectangle.Union(expanded.Bounds, collapsed.Bounds);
        foreach (bool restore in new[] { false, true })
        foreach (double progress in new[] { 0, .25, .5, .75, 1 })
        {
            var from = restore ? collapsed : expanded; var to = restore ? expanded : collapsed;
            using var frame = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format32bppPArgb); using var graphics = Graphics.FromImage(frame); using var alpha = new ImageAttributes();
            RibbonMotionScene.Render(graphics, canvas, ribbon, from, to, progress, alpha);
            for (int y = button.Top; y < button.Bottom; y++) for (int x = button.Left; x < button.Right; x++) Assert.Equal(to.Image.GetPixel(x, y).ToArgb(), frame.GetPixel(x, y).ToArgb());
            Assert.False(Ink(frame, Color.Lime).IsEmpty);
        }
        ribbon.IsMinimized = false; Assert.True(ribbon.IsAnimating); f.Finish(); ribbon.IsMinimized = true; Assert.True(ribbon.IsAnimating); f.Finish();
        Assert.True(ribbon.Snapshot.QuickAccessBounds.IsEmpty); Assert.True(ribbon.Snapshot.MessageBounds.Top >= ribbon.Snapshot.TabViewport.Bottom); Assert.True(f.Message.IsOpen);
    });

    private static Rectangle Ink(Bitmap image, Color color)
    {
        int left = image.Width, top = image.Height, right = -1, bottom = -1;
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
            if (image.GetPixel(x, y).ToArgb() == color.ToArgb()) { left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y); }
        return right < 0 ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }
}
