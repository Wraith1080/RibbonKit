using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackdropClosingTests
{
    [Theory]
    [InlineData(RibbonWindowBackdrop.Mica, 0f)] [InlineData(RibbonWindowBackdrop.Mica, .65f)] [InlineData(RibbonWindowBackdrop.Mica, 1f)]
    [InlineData(RibbonWindowBackdrop.Acrylic, 0f)] [InlineData(RibbonWindowBackdrop.Acrylic, .65f)] [InlineData(RibbonWindowBackdrop.Acrylic, 1f)]
    public void Closing_fades_one_composed_panel_and_reopening_restores_the_live_page(RibbonWindowBackdrop material, float navigationOpacity) => FoundationTests.Sta(() =>
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) || SystemInformation.HighContrast) return;
        using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(180, 120, 950, 650), Backdrop = material };
        using var painter = new BackgroundProbe(); using var page = new PrintProbe();
        var field = new TextBox { Text = "Retained native data", Bounds = new(20, 20, 300, 30) }; page.Controls.Add(field);
        var ribbon = new RibbonControl { Renderer = painter, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Subtle };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        ribbon.Backstage.NavigationBackgroundOpacity = navigationOpacity;
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents();
        Assert.Equal(material, host.EffectiveBackdrop); ribbon.Motion.SystemPolicyOverride = true;
        ribbon.IsBackstageOpen = true; ribbon.SettleMotion(); var surface = ribbon.BackstageSurface!;
        nint handle = field.Handle; var parent = page.Parent; int closed = 0; ribbon.Backstage.Closed += (_, _) => closed++;
        surface.Refresh(); Assert.Equal(255, painter.LastPageAlpha);

        ribbon.IsBackstageOpen = false; Assert.True(ribbon.BackstageOpeningMotion!.IsClosing);
        Assert.Equal(navigationOpacity == 1, surface.Visible);
        Assert.Equal(surface.RectangleToScreen(surface.ClientRectangle), ribbon.BackstageOpeningMotion.Bounds);
        Assert.Equal(0, page.PrintRequests); Assert.Equal(handle, field.Handle); Assert.Same(parent, page.Parent);
        Assert.True(ribbon.IsBackstageOpen); Assert.Equal(0, closed);

        ribbon.IsBackstageOpen = true; surface.Refresh();
        Assert.False(ribbon.IsAnimating); Assert.True(surface.Visible); Assert.Equal(255, painter.LastPageAlpha); Assert.Equal(0, closed);
        var target = navigationOpacity < 1 ? surface.PageArea : surface;
        Assert.True(GetLayeredWindowAttributes(target.Handle, out _, out byte alpha, out _)); Assert.Equal(255, alpha);
        ribbon.IsBackstageOpen = false;
        if (navigationOpacity < 1)
        {
            var input = Message.Create(host.Handle, 0x100, (nint)Keys.A, 0);
            Assert.True(ribbon.ObserveMotionInput(ref input)); // Do not type through the fading panel into the document.
        }
        else ribbon.SettleMotion();
        Assert.False(ribbon.IsBackstageOpen); Assert.Equal(1, closed);
        Assert.Equal(handle, field.Handle); Assert.Equal("Retained native data", field.Text); Assert.Equal(0, page.PrintRequests);
    });
    private sealed class BackgroundProbe : RibbonOffice2024Renderer
    {
        internal int LastPageAlpha;
        public override void DrawBackstageBackground(RibbonPaintContext context, int navigationWidth)
        { LastPageAlpha = context.Theme.Surface.A; base.DrawBackstageBackground(context, navigationWidth); }
    }
    private sealed class PrintProbe : Panel
    {
        internal int PrintRequests;
        protected override void WndProc(ref Message message)
        {
            if (message.Msg is 0x317 or 0x318 && Environment.StackTrace.Contains("RibbonKit.WinForms.RibbonMotionScene.Capture", StringComparison.Ordinal)) PrintRequests++;
            base.WndProc(ref message);
        }
    }
    [DllImport("user32.dll")] private static extern bool GetLayeredWindowAttributes(nint window, out uint key, out byte alpha, out uint flags);
}
