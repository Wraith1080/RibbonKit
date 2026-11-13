using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageCaptionHandoffTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Settled_and_temporary_caption_titles_use_identical_glyph_coverage(bool active) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { Text = "Caption title raster consistency", ClientSize = new(950, 600), ShowIcon = false };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light };
        ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); host.Controls.Add(ribbon); host.Ribbon = ribbon;
        _ = host.Handle; _ = ribbon.Handle; SendMessage(host.Handle, 0x86, active ? 1 : 0, 0);
        using var ordinary = new Bitmap(host.ClientSize.Width, host.CaptionHeight);
        using (var g = Graphics.FromImage(ordinary)) { g.Clear(ribbon.Theme.Background); host.PaintCaption(g, ribbon, foregroundOnly: true); }
        using var plane = new RibbonBackdropBuffer();
        using var temporary = new Bitmap(ordinary.Width, ordinary.Height);
        using (var g = Graphics.FromImage(temporary))
        {
            g.Clear(ribbon.Theme.Background);
            plane.Paint(g, ordinary.Size, ordinary.Height, Color.Transparent, canvas => host.PaintCaption(canvas, ribbon, composited: true, foregroundOnly: true), copyToDestination: false);
            g.DrawImageUnscaled(plane.Image, Point.Empty);
        }
        var text = host.CaptionTitleDrawingBounds(ribbon);
        for (int y = text.Top; y < text.Bottom; y++) for (int x = text.Left; x < text.Right; x++)
            Assert.Equal(temporary.GetPixel(x, y), ordinary.GetPixel(x, y));
    });

    [Theory]
    [InlineData(false, false, RibbonWindowBackdrop.None)] [InlineData(true, false, RibbonWindowBackdrop.None)]
    [InlineData(false, true, RibbonWindowBackdrop.None)] [InlineData(true, true, RibbonWindowBackdrop.None)]
    [InlineData(false, false, RibbonWindowBackdrop.Mica)] [InlineData(true, false, RibbonWindowBackdrop.Mica)]
    [InlineData(false, true, RibbonWindowBackdrop.Mica)] [InlineData(true, true, RibbonWindowBackdrop.Mica)]
    public void Closing_paints_the_normal_caption_before_retiring_its_foreground_plane(bool titleFirst, bool titleQat, RibbonWindowBackdrop backdrop) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { Text = "Caption handoff retains title", ClientSize = new(950, 600), Backdrop = backdrop,
            StartPosition = FormStartPosition.Manual, Location = new(140, 120), ShowInTaskbar = false };
        using var ribbon = new ObservedRibbon { BuiltInTheme = RibbonBuiltInTheme.Office2019Light, FileMenuMode = RibbonFileMenuMode.Backstage,
            AnimationLevel = RibbonAnimationLevel.Expressive, QuickAccessPosition = titleQat ? RibbonQuickAccessPosition.TitleBar : RibbonQuickAccessPosition.BelowRibbon };
        var button = new RibbonButtonItem { Text = "Save" }; var group = new RibbonGroup { Text = "Commands" }; group.Items.Add(button);
        var home = new RibbonTab { Text = "Home" }; home.Groups.Add(group); ribbon.Tabs.Add(home);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = button });
        using var field = new TextBox { Text = "Retained value" };
        ribbon.FileMenu.Visible = true; ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = field });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            ribbon.IsBackstageOpen = true; ribbon.SettleMotion();
            var handle = field.Handle; var parent = field.Parent;
            ribbon.IsBackstageOpen = false;
            var pane = Assert.IsType<RibbonBackstageMotion>(ribbon.BackstageOpeningMotion);
            var title = Assert.IsType<RibbonCaptionTitleMotion>(host.CaptionTitleMotion);
            ribbon.WatchedPlane = FindWindowEx(host.Handle, 0, null, "Ribbon motion");
            Assert.NotEqual(0, ribbon.WatchedPlane);
            ribbon.Handbacks.Clear(); ribbon.Observe = true;
            if (titleFirst) title.SetProgress(1);
            pane.SetProgress(1); pane.Dispose();
            host.CaptionTitleMotion?.SetProgress(1);
            ribbon.Observe = false;
            Assert.False(ribbon.IsBackstageOpen); Assert.False(host.IsCaptionTitleAnimating);
            Assert.Null(ribbon.CaptureBackstageCaption());
            // No message pump is allowed between retiring the layer and this
            // assertion: its replacement must already have painted underneath.
            Assert.Contains(ribbon.Handbacks, frame => frame.PlanePresent && !frame.SurfaceVisible);
            Assert.Equal(handle, field.Handle); Assert.Same(parent, field.Parent); Assert.Equal("Retained value", field.Text);
            Assert.Equal(titleQat ? RibbonQuickAccessPosition.TitleBar : RibbonQuickAccessPosition.BelowRibbon, ribbon.EffectiveQuickAccessPosition);
        }
    });

    private sealed class ObservedRibbon : RibbonControl
    {
        internal bool Observe;
        internal nint WatchedPlane;
        internal readonly List<(bool PlanePresent, bool SurfaceVisible)> Handbacks = [];
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!Observe || IsBackstageOpen || WindowHost?.IsCaptionTitleAnimating == true) return;
            Handbacks.Add((IsWindow(WatchedPlane), BackstageSurface?.Visible == true));
        }
    }
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern nint FindWindowEx(nint parent, nint after, string? className, string caption);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool IsWindow(nint window);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
}
