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
    public void Complete_surface_slides_and_fades_without_printing_or_reparenting_pages(bool office, RibbonAnimationLevel level) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(office); f.Ribbon.AnimationLevel = level; f.Ribbon.IsBackstageOpen = true;
        var surface = f.Ribbon.BackstageSurface!; var motion = f.Ribbon.BackstageOpeningMotion!;
        var pageBounds = f.Page.Bounds; var parent = f.Page.Parent; nint handle = f.Field.Handle;
        Assert.True(f.Ribbon.IsAnimating); Assert.InRange(surface.Right / (double)surface.Width, .849, .851); int start = surface.Left;
        Assert.True(RibbonDrawingHold.HasOpacity(surface.Handle)); Assert.Equal(1, motion.Opacity); Assert.True(f.Page.Visible);
        int previous = surface.Left;
        foreach (double progress in new[] { .1, .25, .5, 1 })
        {
            motion.SetProgress(progress);
            Assert.True(surface.Left > previous); previous = surface.Left;
            Assert.InRange(motion.Opacity, 1, 255);
            if (progress == .5) Assert.InRange(motion.Opacity, 127, 128);
            Assert.Equal(surface.PointToScreen(Point.Empty).X + surface.PageArea.Left + parent!.Left + f.Page.Left, f.Page.PointToScreen(Point.Empty).X);
            Assert.Equal(pageBounds, f.Page.Bounds); Assert.Same(parent, f.Page.Parent); Assert.Equal(handle, f.Field.Handle);
            Assert.False(RibbonDrawingHold.HasOpacity(f.Page.Handle)); Assert.False(RibbonDrawingHold.HasOpacity(f.Field.Handle));
        }
        Assert.Equal(0, surface.Left); Assert.Equal(255, motion.Opacity); f.Ribbon.SettleMotion();
        f.Ribbon.IsBackstageOpen = false; var closing = f.Ribbon.BackstageOpeningMotion!;
        Assert.True(closing.IsClosing); closing.SetProgress(.25); Assert.True(surface.Left < 0); Assert.True(surface.Visible);
        int? hiddenAt = null; surface.VisibleChanged += (_, _) => { if (!surface.Visible) hiddenAt = surface.Left; };
        closing.SetProgress(1); Assert.Equal(1, closing.Opacity); Assert.Equal(start, surface.Left); Assert.InRange(surface.Right / (double)surface.Width, .849, .851); f.Ribbon.SettleMotion();
        Assert.Equal(start, hiddenAt);
        Assert.False(f.Ribbon.IsBackstageOpen); Assert.Equal(0, surface.Left); Assert.Equal(0, f.Page.PrintRequests);
        f.Ribbon.IsBackstageOpen = true; Assert.True(f.Ribbon.IsAnimating); Assert.Equal(start, surface.Left);
        Assert.Equal(handle, f.Field.Handle); Assert.Equal("Untitled", f.Field.Text);
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
    public void Page_switches_are_immediate_and_keep_native_handles_selection_and_values() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(true); using var second = new TextBox { Text = "Print settings" };
        var page = new RibbonBackstagePageItem { Text = "Print", Content = second }; f.Ribbon.Backstage.Items.Add(page);
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.IsBackstageOpen = true;
        var surface = f.Ribbon.BackstageSurface!; nint handle = f.Field.Handle; var parent = f.Page.Parent;
        f.Field.Select(2, 3); f.Ribbon.Backstage.SelectedPage = page;
        Assert.False(f.Ribbon.IsAnimating); Assert.Equal(0, surface.Left); Assert.True(second.Visible);
        f.Ribbon.Backstage.SelectedPage = (RibbonBackstagePageItem)f.Ribbon.Backstage.Items[0];
        Assert.False(f.Ribbon.IsAnimating); Assert.Same(parent, f.Page.Parent); Assert.Equal(handle, f.Field.Handle);
        Assert.Equal(2, f.Field.SelectionStart); Assert.Equal(3, f.Field.SelectionLength); Assert.Equal("Print settings", second.Text);
        Assert.Equal(0, f.Page.PrintRequests); Assert.Equal(255, surface.MotionAlpha);
    });
    [Fact]
    public void Closing_completes_once_and_reopening_or_new_input_resolves_the_pending_close() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(true); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; int closed = 0; f.Ribbon.Backstage.Closed += (_, _) => closed++;
        f.Ribbon.IsBackstageOpen = true; f.Ribbon.SettleMotion(); var surface = f.Ribbon.BackstageSurface!;
        f.Ribbon.IsBackstageOpen = false; Assert.True(f.Ribbon.IsBackstageOpen); Assert.True(f.Ribbon.BackstageOpeningMotion!.IsClosing); Assert.Equal(0, closed);
        f.Ribbon.IsBackstageOpen = true; Assert.True(f.Ribbon.IsBackstageOpen); Assert.False(f.Ribbon.IsAnimating); Assert.Equal(0, closed);
        Assert.Equal(0, surface.Left); Assert.Equal(255, surface.MotionAlpha);
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


}
