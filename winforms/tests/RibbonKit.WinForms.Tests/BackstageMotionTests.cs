using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageMotionTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly Form Host = new() { Size = new(1050, 720) };
        internal readonly RibbonControl Ribbon;
        internal readonly PrintProbe Page = new();
        internal readonly TextBox Field = new() { Text = "Untitled", Location = new(30, 30) };
        internal Fixture(bool office)
        {
            Application.EnableVisualStyles(); Ribbon = new() { FileMenuMode = RibbonFileMenuMode.Backstage, Renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer() }; Ribbon.FileMenu.Visible = true; Ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
            Page.Controls.Add(Field); Ribbon.Backstage.Items.AddRange(new RibbonBackstagePageItem { Text = "Info", Content = Page, Icon = new Icon() }, new RibbonButtonItem { Text = "Save", Icon = new Icon() });
            Host.Controls.Add(Ribbon); Host.Show(); Application.DoEvents(); Ribbon.Motion.SystemPolicyOverride = true;
        }
        public void Dispose() { var painter = Ribbon.Renderer; Host.Dispose(); Page.Dispose(); painter?.Dispose(); }
        internal void Close() { Ribbon.IsBackstageOpen = false; Ribbon.SettleMotion(); }
    }
    private sealed class PrintProbe : Panel
    {
        internal int PrintRequests;
        protected override void WndProc(ref Message m)
        {
            // Windows can ask a TextBox's parent to repaint during normal
            // BringToFront. Count only requests caused by motion capture.
            if (m.Msg is 0x317 or 0x318 && Environment.StackTrace.Contains("RibbonKit.WinForms.RibbonMotionScene.Capture", StringComparison.Ordinal)) PrintRequests++;
            base.WndProc(ref m);
        }
    }
    private sealed class Icon : IRibbonIcon { public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) => graphics.FillRectangle(Brushes.Magenta, bounds); }

    [Theory] [InlineData(false, RibbonAnimationLevel.Subtle)] [InlineData(true, RibbonAnimationLevel.Subtle)]
    [InlineData(false, RibbonAnimationLevel.Expressive)] [InlineData(true, RibbonAnimationLevel.Expressive)]
    public void Opening_only_holds_navigation_and_capture_never_prints_or_reparents_application_content(bool office, RibbonAnimationLevel level) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(office); f.Ribbon.AnimationLevel = level; f.Ribbon.IsBackstageOpen = true;
        Assert.True(f.Ribbon.IsBackstageOpen); Assert.True(f.Ribbon.IsAnimating, $"Navigation={f.Ribbon.BackstageSurface!.NavigationBounds}; eligible={f.Ribbon.CanAnimatePopupOpening}; system={f.Ribbon.Motion.SystemEnabled}"); var surface = f.Ribbon.BackstageSurface!; var bounds = f.Page.Bounds; var parent = f.Page.Parent; nint handle = f.Field.Handle;
        Assert.True(f.Page.Visible); Assert.Equal(0, f.Page.PrintRequests); Assert.False(RibbonDrawingHold.HasOpacity(f.Page.Handle)); Assert.False(RibbonDrawingHold.HasOpacity(f.Field.Handle));
        Assert.All(surface.NavigationControls, c => Assert.True(RibbonDrawingHold.HasOpacity(c.Handle)));
        f.Ribbon.SettleMotion(); Assert.False(f.Ribbon.IsAnimating); Assert.All(surface.NavigationControls, c => Assert.False(RibbonDrawingHold.HasOpacity(c.Handle)));
        Assert.Equal(bounds, f.Page.Bounds); Assert.Same(parent, f.Page.Parent); Assert.Equal(handle, f.Field.Handle); Assert.Equal("Untitled", f.Field.Text);
        f.Close(); Assert.False(f.Ribbon.IsAnimating); f.Ribbon.IsBackstageOpen = true; Assert.True(f.Ribbon.IsAnimating); Assert.Same(parent, f.Page.Parent); Assert.Equal(handle, f.Field.Handle); Assert.Equal(0, f.Page.PrintRequests);
    });

    [Theory] [InlineData(RibbonAnimationLevel.Subtle)] [InlineData(RibbonAnimationLevel.Expressive)]
    public void Navigation_drawing_fades_and_travels_horizontally_at_natural_size(RibbonAnimationLevel level) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(true); f.Ribbon.AnimationLevel = level; f.Ribbon.IsBackstageOpen = true; var motion = f.Ribbon.BackstageOpeningMotion!; var frames = new List<Bitmap>();
        try
        {
            foreach (double progress in new[] { 0, .5, 1 })
            {
                var frame = new Bitmap(motion.Bounds.Width, motion.Bounds.Height, PixelFormat.Format32bppPArgb); frames.Add(frame); using var g = Graphics.FromImage(frame); using var alpha = new ImageAttributes(); motion.Draw(g, alpha, progress);
            }
            Assert.True(Pink(frames[0]).IsEmpty); var middle = Pink(frames[1]); var last = Pink(frames[2]); Assert.False(last.IsEmpty); Assert.Equal(last.Size, middle.Size); Assert.Equal(last.Top, middle.Top); Assert.True(middle.Left < last.Left);
            Assert.InRange(frames[1].GetPixel(middle.Left, middle.Top).G, 1, 254); Assert.Equal(0, f.Page.PrintRequests);
            string directory = Path.Combine("TestResults", "visual", "m6a-backstage"); Directory.CreateDirectory(directory); for (int i = 0; i < frames.Count; i++) frames[i].Save(Path.Combine(directory, $"{level}-{i}.png"));
        }
        finally { foreach (var image in frames) image.Dispose(); }
    });

    [Fact]
    public void Policies_input_mutation_resize_and_cancelled_close_settle_without_changing_backstage_semantics() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(true); var ribbon = f.Ribbon;
        ribbon.IsBackstageOpen = true; Assert.False(ribbon.IsAnimating); f.Close();
        ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; ribbon.Motion.SystemPolicyOverride = false; ribbon.IsBackstageOpen = true; Assert.False(ribbon.IsAnimating); f.Close(); ribbon.Motion.SystemPolicyOverride = true;
        ribbon.BeginUpdate(); ribbon.IsBackstageOpen = true; Assert.False(ribbon.IsAnimating); ribbon.EndUpdate(); f.Close();
        ribbon.IsBackstageOpen = true; Assert.True(ribbon.IsAnimating); var input = Message.Create(f.Field.Handle, 0x100, (nint)Keys.Tab, 0); ribbon.ObserveMotionInput(ref input); Assert.False(ribbon.IsAnimating); Assert.True(ribbon.IsBackstageOpen); f.Close();
        ribbon.IsBackstageOpen = true; Assert.True(ribbon.IsAnimating); ribbon.Backstage.NavigationWidth = (ribbon.Backstage.NavigationWidth ?? 240) + 20; Assert.False(ribbon.IsAnimating); f.Close();
        ribbon.IsBackstageOpen = true; Assert.True(ribbon.IsAnimating); f.Host.Width -= 20; Assert.False(ribbon.IsAnimating); f.Close();
        ribbon.IsBackstageOpen = true; Assert.True(ribbon.IsAnimating); System.ComponentModel.CancelEventHandler cancel = (_, e) => e.Cancel = true; ribbon.Backstage.Closing += cancel; f.Close(); Assert.True(ribbon.IsBackstageOpen); Assert.False(ribbon.IsAnimating); ribbon.Backstage.Closing -= cancel; f.Close();
        ribbon.IsBackstageOpen = true; Assert.True(ribbon.IsAnimating); ribbon.AnimationLevel = RibbonAnimationLevel.None; Assert.False(ribbon.IsAnimating); Assert.True(ribbon.IsBackstageOpen); Assert.Equal(0, f.Page.PrintRequests);
    });

    [Fact]
    public void Disposal_and_opening_callback_changes_leave_no_hold_and_keep_borrowed_pages_alive() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(false); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; EventHandler disable = (_, _) => f.Ribbon.AnimationLevel = RibbonAnimationLevel.None; f.Ribbon.IsBackstageOpenChanged += disable;
        f.Ribbon.IsBackstageOpen = true; Assert.True(f.Ribbon.IsBackstageOpen); Assert.False(f.Ribbon.IsAnimating); f.Close(); f.Ribbon.IsBackstageOpenChanged -= disable;
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.IsBackstageOpen = true; Assert.True(f.Ribbon.IsAnimating); f.Ribbon.Dispose(); Assert.False(f.Ribbon.IsAnimating); Assert.False(f.Page.IsDisposed); Assert.Null(f.Page.Parent);
    });

    [Fact]
    public void Live_page_opening_and_page_switch_fade_native_parents_without_capturing_content() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(true); using var second = new TextBox { Text = "Print settings" }; var page = new RibbonBackstagePageItem { Text = "Print", Content = second }; f.Ribbon.Backstage.Items.Add(page);
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.IsBackstageOpen = true; var surface = f.Ribbon.BackstageSurface!;
        Assert.True(GetLayeredWindowAttributes(surface.Handle, out _, out byte opening, out _)); Assert.Equal(1, opening); nint handle = f.Field.Handle; var parent = f.Page.Parent; f.Ribbon.SettleMotion();
        f.Ribbon.Backstage.SelectedPage = page; Assert.True(f.Ribbon.IsAnimating); Assert.True(GetLayeredWindowAttributes(surface.Handle, out _, out byte switching, out _)); Assert.Equal(1, switching); Assert.True(second.Visible); f.Ribbon.SettleMotion();
        Assert.False(RibbonDrawingHold.HasOpacity(surface.PageArea.Handle)); Assert.Equal("Print settings", second.Text); f.Ribbon.Backstage.SelectedPage = (RibbonBackstagePageItem)f.Ribbon.Backstage.Items[0]; Assert.True(f.Ribbon.IsAnimating); f.Ribbon.SettleMotion();
        Assert.Same(parent, f.Page.Parent); Assert.Equal(handle, f.Field.Handle); Assert.Equal(0, f.Page.PrintRequests); Assert.False(RibbonDrawingHold.HasOpacity(f.Page.Handle));
    });

    [Fact]
    public void Closing_completes_once_and_reopening_or_new_input_resolves_the_pending_close() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(true); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; int closed = 0; f.Ribbon.Backstage.Closed += (_, _) => closed++;
        f.Ribbon.IsBackstageOpen = true; f.Ribbon.SettleMotion(); var surface = f.Ribbon.BackstageSurface!;
        f.Ribbon.IsBackstageOpen = false; Assert.True(f.Ribbon.IsBackstageOpen); Assert.True(f.Ribbon.BackstageOpeningMotion!.IsClosing); Assert.Equal(0, closed);
        f.Ribbon.IsBackstageOpen = true; Assert.True(f.Ribbon.IsBackstageOpen); Assert.False(f.Ribbon.IsAnimating); Assert.Equal(0, closed); Assert.False(RibbonDrawingHold.HasOpacity(surface.Handle));
        f.Ribbon.IsBackstageOpen = false; var input = Message.Create(f.Field.Handle, 0x100, (nint)Keys.A, 0); Assert.True(f.Ribbon.ObserveMotionInput(ref input)); Assert.False(f.Ribbon.IsBackstageOpen); Assert.Equal(1, closed); Assert.Equal("Untitled", f.Field.Text); Assert.False(RibbonDrawingHold.HasOpacity(surface.Handle));
        f.Ribbon.IsBackstageOpen = true; f.Ribbon.IsBackstageOpen = false; Assert.True(f.Ribbon.IsAnimating); f.Ribbon.AnimationLevel = RibbonAnimationLevel.None; Assert.False(f.Ribbon.IsBackstageOpen); Assert.Equal(2, closed); Assert.False(f.Ribbon.IsAnimating);
    });

    [Fact]
    public void Commands_stay_synchronous_and_throwing_close_callbacks_release_native_opacity() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(false); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; bool invoked = false;
        var command = (RibbonButtonItem)f.Ribbon.Backstage.Items[1]; command.Click += (_, _) => { Assert.False(f.Ribbon.IsBackstageOpen); invoked = true; };
        f.Ribbon.IsBackstageOpen = true; f.Ribbon.ActivateBackstage(command); Assert.True(invoked); Assert.False(f.Ribbon.IsAnimating);
        f.Ribbon.IsBackstageOpen = true; var surface = f.Ribbon.BackstageSurface!; EventHandler fail = (_, _) => throw new InvalidOperationException("Application close callback"); f.Ribbon.Backstage.Closed += fail;
        f.Ribbon.IsBackstageOpen = false; Assert.Throws<InvalidOperationException>(() => f.Ribbon.SettleMotion()); Assert.False(f.Ribbon.IsBackstageOpen); Assert.False(f.Ribbon.IsAnimating); Assert.False(RibbonDrawingHold.HasOpacity(surface.Handle)); f.Ribbon.Backstage.Closed -= fail;
    });

    [DllImport("user32.dll")] private static extern bool GetLayeredWindowAttributes(nint window, out uint color, out byte alpha, out uint flags);

    private static Rectangle Pink(Bitmap image)
    {
        int left = image.Width, top = image.Height, right = -1, bottom = -1; var data = image.LockBits(new(Point.Empty, image.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb); int[] row = new int[image.Width];
        try
        {
            for (int y = 0; y < image.Height; y++) { Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, row.Length); for (int x = 0; x < row.Length; x++) { var c = Color.FromArgb(row[x]); if (c.R > 245 && c.B > 245 && c.G < 240) { left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y); } } }
        }
        finally { image.UnlockBits(data); }
        return right < 0 ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }
}
