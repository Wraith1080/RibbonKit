using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageSynchronizationTests
{
    [Fact]
    public void Backstage_page_release_keeps_pressed_paint_through_the_native_repaint() => FoundationTests.Sta(() =>
    {
        using var host = new Form { ClientSize = new(850, 550) };
        using var painter = new BackstageEntryRecorder();
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue,
            Renderer = painter, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.None };
        ribbon.FileMenu.Visible = true;
        ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        var info = new RibbonBackstagePageItem { Text = "Info" };
        var print = new RibbonBackstagePageItem { Text = "Print" };
        ribbon.Backstage.Items.AddRange(info, print);
        host.Controls.Add(ribbon);
        host.Show();
        Application.DoEvents();
        ribbon.IsBackstageOpen = true;
        Application.DoEvents();
        Assert.Same(info, ribbon.Backstage.SelectedPage);

        var surface = ribbon.BackstageSurface!;
        var entry = surface.Controls.Cast<Control>().SelectMany(c => c.Controls.OfType<Button>()).Single(c => c.Text == print.Text);
        painter.WatchedItem = print;
        nint point = entry.Width / 2 | (entry.Height / 2 << 16);
        const int wmLeftButtonDown = 0x0201, wmLeftButtonUp = 0x0202;
        SendMessage(entry.Handle, wmLeftButtonDown, 1, point);
        entry.Update();
        Assert.Contains((false, true), painter.States);
        painter.States.Clear();

        // Release just outside the row to exercise Button's synchronous
        // repaint without depending on the test window's desktop z-order.
        SendMessage(entry.Handle, wmLeftButtonUp, 0, point | (nint)0xffff);
        Assert.Same(info, ribbon.Backstage.SelectedPage);
        Assert.Contains((false, true), painter.States);
        Assert.DoesNotContain((false, false), painter.States);
        entry.Update();
        Assert.Contains((false, false), painter.States);

        SendMessage(entry.Handle, wmLeftButtonDown, 1, point);
        entry.Capture = false;
        painter.States.Clear();
        entry.Update();
        Assert.Contains((false, false), painter.States);
        Assert.Same(info, ribbon.Backstage.SelectedPage);
    });

    [Theory] [InlineData(RibbonBuiltInTheme.Office2019Light)] [InlineData(RibbonBuiltInTheme.Office2024Light)]
    public void Real_panes_slide_in_their_theme_caption_bounds_without_reparenting(RibbonBuiltInTheme theme) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { ClientSize = new(1000, 600) };
        using var ribbon = new RibbonControl { BuiltInTheme = theme, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive };
        using var page = new Panel(); var field = new TextBox { Text = "Retained", Location = new(20, 100) }; page.Controls.Add(field);
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        ribbon.IsBackstageOpen = true;
        var surface = ribbon.BackstageSurface!; var handle = field.Handle; var parent = field.Parent;
        int split = surface.NavigationBounds.Right;
        bool fullHeight = theme == RibbonBuiltInTheme.Office2019Light;
        int top = fullHeight ? 0 : host.CaptionHeight;
        Assert.Equal(top, surface.Top); Assert.Equal(host.ClientSize.Height - top, surface.Height);
        Assert.Equal(fullHeight ? host.CaptionHeight : 0, surface.CaptionHeight);
        Assert.Equal(fullHeight, surface.Top + surface.PageArea.Top < host.CaptionHeight);
        Assert.True(page.PointToScreen(Point.Empty).Y >= host.PointToScreen(new(0, host.CaptionHeight)).Y);
        foreach (bool closing in new[] { false, true })
        {
            if (closing) { ribbon.SettleMotion(); ribbon.IsBackstageOpen = false; }
            var motion = ribbon.BackstageOpeningMotion!;
            foreach (double progress in new[] { 0, .25, .5, .75, 1 })
            {
                motion.SetProgress(progress);
                Assert.Equal(top, surface.Top);
                Assert.Equal(fullHeight ? Math.Max(0, surface.Left + split) : 0, ribbon.CaptionPageBounds.Left);
                if (progress == 0 && !closing || progress == 1 && closing) Assert.True(surface.Left < 0);
                using var caption = ribbon.CaptureBackstageCaption();
                if (fullHeight)
                {
                    Assert.NotNull(caption);
                    Assert.Equal(0, caption.GetPixel(100, 2).A); Assert.Equal(host.ClientSize.Width, caption.Width);
                    Assert.Equal((nint)(-1), Hit(surface, host.PointToScreen(new(50, host.CaptionHeight / 2))));
                    Assert.Equal((nint)(-1), Hit(surface.PageArea, host.PointToScreen(new(Math.Max(0, surface.Left) + split + 10, host.CaptionHeight / 2))));
                }
                else { Assert.Null(caption); Assert.True(ribbon.CaptionOverlayBounds.IsEmpty); }
                Assert.True(GetLayeredWindowAttributes(surface.Handle, out _, out byte alpha, out _));
                Assert.Equal(motion.Opacity, alpha);
                Assert.Equal(handle, field.Handle); Assert.Same(parent, field.Parent); Assert.Equal("Retained", field.Text);
            }
        }
        ribbon.SettleMotion(); Assert.False(ribbon.IsBackstageOpen);
        Assert.Null(ribbon.CaptureBackstageCaption());
        Assert.Equal(handle, field.Handle); Assert.Same(parent, field.Parent);
    });

    [Fact]
    public void Slide_override_is_per_ribbon_and_rejects_nonfinite_or_out_of_range_distances() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { ClientSize = new(900, 600) };
        using var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive };
        using var other = new RibbonControl();
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info" });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        foreach (double distance in new[] { 0, .1 })
        {
            ribbon.Theme = ribbon.Theme with { BackstageSlideDistanceRatio = distance };
            ribbon.IsBackstageOpen = true;
            Assert.Equal(-(int)Math.Round(ribbon.BackstageSurface!.Width * distance), ribbon.BackstageSurface.Left);
            ribbon.SettleMotion(); ribbon.IsBackstageOpen = false; ribbon.SettleMotion();
        }
        var current = ribbon.Theme;
        foreach (double invalid in new[] { -.01, 1.01, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = current with { BackstageSlideDistanceRatio = invalid });
        Assert.Same(current, ribbon.Theme); Assert.Null(other.Theme.BackstageSlideDistanceRatio);
    });

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLayeredWindowAttributes(nint window, out uint key, out byte alpha, out uint flags);
    private static nint Hit(Control control, Point screen) => SendMessage(control.Handle, 0x84, 0, (nint)((screen.Y << 16) | (screen.X & 0xffff)));
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);

    private sealed class BackstageEntryRecorder : RibbonOffice2024Renderer
    {
        internal RibbonBackstagePageItem? WatchedItem { get; set; }
        internal List<(bool Selected, bool Pressed)> States { get; } = [];

        public override void DrawBackstageEntry(RibbonPaintContext context, RibbonItem? item, bool selected)
        {
            if (WatchedItem is not null && ReferenceEquals(item, WatchedItem)) States.Add((selected, context.Pressed));
            base.DrawBackstageEntry(context, item, selected);
        }
    }
}
