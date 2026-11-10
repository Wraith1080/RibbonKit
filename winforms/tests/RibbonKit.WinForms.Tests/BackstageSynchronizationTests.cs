using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageSynchronizationTests
{
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
}
