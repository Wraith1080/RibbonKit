using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public class Office2007OrbMotionTests
{
    [Theory]
    [InlineData(RibbonWindowBackdrop.None, false)]
    [InlineData(RibbonWindowBackdrop.Mica, false)]
    [InlineData(RibbonWindowBackdrop.None, true)]
    [InlineData(RibbonWindowBackdrop.Mica, true)]
    public void Native_orb_is_never_suppressed_before_its_replacement_is_published(RibbonWindowBackdrop backdrop, bool closing) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { Bounds = new(-16000, -16000, 1000, 700), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false, Backdrop = backdrop };
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2007Blue, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive };
        using var painter = new HandoffProbe(ribbon); ribbon.Renderer = painter;
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab()); ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info" });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        if (closing) { ribbon.IsBackstageOpen = true; ribbon.SettleMotion(); }
        painter.Unpublished.Clear(); painter.Suppressed = 0; ribbon.IsBackstageOpen = !closing;
        var motion = ribbon.BackstageOpeningMotion!; Assert.NotNull(motion);
        foreach (double p in new[] { 0, .25, .6, 1 }) { motion.SetProgress(p); ribbon.BackstageSurface!.RepaintBackdrop(); }
        Assert.True(painter.Suppressed > 0); Assert.Empty(painter.Unpublished); ribbon.SettleMotion();
    });
    private sealed class HandoffProbe(RibbonControl ribbon) : RibbonOffice2024Renderer
    {
        internal int Suppressed;
        internal List<string> Unpublished = [];
        public override void DrawFileButton(RibbonPaintContext c, RibbonDropDownItem file)
        {
            if (c.SuppressApplicationButton)
            {
                Suppressed++;
                var motion = ribbon.BackstageOpeningMotion!;
                if (motion.ApplicationButtonPlaneBounds.IsEmpty || !IsWindowVisible(motion.ApplicationButtonPlaneHandle))
                    Unpublished.Add(motion.IsClosing ? "closing" : "opening");
                for (nint sibling = GetWindow(motion.ApplicationButtonPlaneHandle, 3); sibling != 0; sibling = GetWindow(sibling, 3))
                    if (sibling == ribbon.BackstageSurface!.Handle) Unpublished.Add("pane above Orb");
            }
            base.DrawFileButton(c, file);
        }
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint window);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint GetWindow(nint window, uint command);

    [Fact]
    public void Stationary_orb_keeps_renderer_callbacks_but_publishes_only_changed_pixels() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { Bounds = new(-16000, -16000, 900, 650), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        var icon = new ChangingIcon();
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2007Blue, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive, ApplicationButtonIcon = icon };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab()); ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info" });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        ribbon.IsBackstageOpen = true; var motion = ribbon.BackstageOpeningMotion!; var handle = motion.ApplicationButtonPlaneHandle;
        Assert.Equal(1, motion.ApplicationButtonPublications); int calls = icon.Calls;
        foreach (double p in new[] { .1, .25, .5, .9, 1 }) motion.SetProgress(p);
        Assert.True(icon.Calls > calls); Assert.Equal(1, motion.ApplicationButtonPublications); Assert.Equal(handle, motion.ApplicationButtonPlaneHandle);
        icon.Ink = Color.Lime; motion.SetProgress(.5); Assert.Equal(2, motion.ApplicationButtonPublications);
        using var image = motion.CaptureApplicationButton()!;
        Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(image.Width / 2, image.Height / 2).ToArgb());
        motion.SetProgress(.7); Assert.Equal(2, motion.ApplicationButtonPublications); Assert.Equal(handle, motion.ApplicationButtonPlaneHandle); ribbon.SettleMotion();
    });
    private sealed class ChangingIcon : IRibbonIcon
    {
        internal int Calls; internal Color Ink = Color.Magenta;
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        { Calls++; using var brush = new SolidBrush(Ink); graphics.FillRectangle(brush, bounds); }
    }

    [Fact]
    public void A_renderer_that_settles_motion_during_first_orb_paint_cannot_leave_a_published_plane_behind() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { Bounds = new(-16000, -16000, 900, 650), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2007Blue, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive };
        using var painter = new SettlingFile(ribbon); ribbon.Renderer = painter;
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab()); ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info" });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        painter.Armed = true; ribbon.IsBackstageOpen = true;
        Assert.True(painter.Settled); Assert.True(ribbon.IsBackstageOpen); Assert.False(ribbon.BackstageOpeningMotion!.OwnsApplicationButton);
        Assert.Equal(0, FindWindowEx(host.Handle, 0, null, "Ribbon motion"));
    });
    private sealed class SettlingFile(RibbonControl ribbon) : RibbonOffice2024Renderer
    {
        internal bool Armed, Settled;
        public override void DrawFileButton(RibbonPaintContext c, RibbonDropDownItem file)
        {
            if (Armed && !Settled && c.FileMenuPreviewOpen && !c.SuppressApplicationButton && c.Bounds.Top >= 0 && c.Bounds.Top < ribbon.ApplicationButtonBounds.Top)
            { Settled = true; ribbon.SettleMotion(); }
            base.DrawFileButton(c, file);
        }
    }
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern nint FindWindowEx(nint parent, nint after, string? className, string? title);

    [Theory]
    [InlineData(RibbonWindowBackdrop.None, RibbonAnimationLevel.Subtle)]
    [InlineData(RibbonWindowBackdrop.None, RibbonAnimationLevel.Expressive)]
    [InlineData(RibbonWindowBackdrop.Mica, RibbonAnimationLevel.Subtle)]
    [InlineData(RibbonWindowBackdrop.Mica, RibbonAnimationLevel.Expressive)]
    public void Whole_orb_stays_at_one_screen_position_and_full_opacity_during_both_live_pane_transitions(RibbonWindowBackdrop backdrop, RibbonAnimationLevel level) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { Bounds = new(-16000, -16000, 1000, 700), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false, Backdrop = backdrop };
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2007Blue, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = level };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        using var page = new Panel(); using var field = new TextBox { Text = "Retained editor", Bounds = new(24, 24, 240, 30) }; page.Controls.Add(field);
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents();
        ribbon.Motion.SystemPolicyOverride = true;
        var handle = field.Handle;
        ribbon.IsBackstageOpen = true;
        var parent = page.Parent; var surface = ribbon.BackstageSurface!;
        foreach (bool closing in new[] { false, true })
        {
            if (closing) ribbon.IsBackstageOpen = false;
            var motion = ribbon.BackstageOpeningMotion!;
            Assert.NotNull(motion); Assert.True(motion.OwnsApplicationButton); Assert.Equal(closing, motion.IsClosing);
            var anchor = motion.ApplicationButtonPlaneBounds;
            var visual = ribbon.RectangleToScreen(ribbon.ApplicationButtonBounds);
            Assert.True(anchor.Contains(visual));
            using var first = motion.CaptureApplicationButton()!;
            Assert.Equal(255, first.GetPixel(visual.Left - anchor.Left + visual.Width / 2, visual.Top - anchor.Top + visual.Height / 2).A);
            foreach (double p in new[] { 0, .2, .55, .85, 1 })
            {
                motion.SetProgress(p); surface.RepaintBackdrop();
                Assert.Equal(anchor, motion.ApplicationButtonPlaneBounds);
                using var frame = motion.CaptureApplicationButton()!;
                Assert.Equal(first.Size, frame.Size);
                for (int y = 0; y < frame.Height; y++) for (int x = 0; x < frame.Width; x++) Assert.Equal(first.GetPixel(x, y), frame.GetPixel(x, y));
                Assert.Equal(handle, field.Handle); Assert.Same(parent, page.Parent); Assert.Equal("Retained editor", field.Text);
            }
            ribbon.SettleMotion(); Assert.False(motion.OwnsApplicationButton); Assert.Null(motion.CaptureApplicationButton());
            Assert.Equal(!closing, ribbon.IsBackstageOpen); Assert.Equal(255, surface.MotionAlpha);
        }
    });

    [Fact]
    public void Stationary_plane_suppresses_custom_file_paint_on_the_travelling_header_without_skipping_its_hook()
    {
        using var painter = new CustomFile(); using var file = new RibbonDropDownItem();
        using var image = new Bitmap(300, 60); using var g = Graphics.FromImage(image);
        var bounds = new Rectangle(0, 0, 300, 60); var orb = new Rectangle(6, -20, 60, 60);
        painter.DrawBackstageHeader(new(g, bounds, RibbonThemes.Office2007Blue, Control.DefaultFont, 96) { SuppressApplicationButton = true }, file, orb);
        Assert.Equal(1, painter.Calls); Assert.NotEqual(Color.Lime.ToArgb(), image.GetPixel(36, 20).ToArgb());
        painter.DrawBackstageHeader(new(g, bounds, RibbonThemes.Office2007Blue, Control.DefaultFont, 96), file, orb);
        Assert.Equal(2, painter.Calls); Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(36, 20).ToArgb());
    }
    private sealed class CustomFile : RibbonOffice2024Renderer
    {
        internal int Calls;
        public override void DrawFileButton(RibbonPaintContext c, RibbonDropDownItem file)
        { Calls++; using var brush = new SolidBrush(Color.Lime); c.Graphics.FillRectangle(brush, c.Bounds); }
    }

    [Fact]
    public void Explicit_rectangular_file_retains_the_existing_pane_presentation() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { Bounds = new(-16000, -16000, 900, 650), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2007Blue, ApplicationButtonShape = RibbonApplicationButtonShape.Rectangular, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Subtle };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab()); ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info" });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        ribbon.IsBackstageOpen = true; var motion = ribbon.BackstageOpeningMotion!;
        Assert.NotNull(motion); Assert.False(motion.OwnsApplicationButton); ribbon.SettleMotion();
    });
}
