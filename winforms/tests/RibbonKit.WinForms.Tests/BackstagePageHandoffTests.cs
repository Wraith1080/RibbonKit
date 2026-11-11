using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstagePageHandoffTests
{
    [Theory]
    [InlineData(RibbonBuiltInTheme.Office2019Light, RibbonAnimationLevel.Expressive)]
    [InlineData(RibbonBuiltInTheme.Office2024Light, RibbonAnimationLevel.Expressive)]
    [InlineData(RibbonBuiltInTheme.Office2019Light, RibbonAnimationLevel.None)]
    [InlineData(RibbonBuiltInTheme.Office2024Light, RibbonAnimationLevel.None)]
    public void Incoming_opaque_page_paints_before_outgoing_page_is_hidden(RibbonBuiltInTheme theme, RibbonAnimationLevel level) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 950, 600), ShowInTaskbar = false };
        using var ribbon = new RibbonControl { BuiltInTheme = theme, FileMenuMode = RibbonFileMenuMode.Backstage,
            AnimationLevel = RibbonAnimationLevel.Expressive, TabContentAnimation = RibbonTabContentAnimation.SlideDown };
        using var outgoing = new Panel(); using var incoming = new PageProbe();
        var field = new TextBox { Text = "Retained outgoing field" }; outgoing.Controls.Add(field);
        var first = new RibbonBackstagePageItem { Text = "Info", Content = outgoing };
        var second = new RibbonBackstagePageItem { Text = "Print", Content = incoming };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); ribbon.Backstage.Items.AddRange(first, second);
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        ribbon.IsBackstageOpen = true; ribbon.SettleMotion(); Application.DoEvents();
        nint handle = field.Handle; var parent = outgoing.Parent;
        ribbon.AnimationLevel = level;
        bool painted = false, outgoingVisibleAtPaint = false, outgoingDrawingRetained = false;
        incoming.FirstVisiblePaint = () =>
        {
            painted = true; outgoingVisibleAtPaint = outgoing.Visible;
            outgoingDrawingRetained = (GetWindowLong(parent!.Handle, -20) & 0x80000) != 0;
        };
        ribbon.Backstage.SelectedPage = second; Application.DoEvents();
        Assert.True(painted, "The visible incoming page must have a native drawing.");
        Assert.True(outgoingVisibleAtPaint, "Hiding the outgoing page before drawing its replacement exposes the empty page viewport.");
        Assert.Equal(level != RibbonAnimationLevel.None, outgoingDrawingRetained);
        Assert.False(outgoing.Visible); Assert.True(incoming.Visible); Assert.Same(second, ribbon.Backstage.SelectedPage);
        Assert.Equal(handle, field.Handle); Assert.Same(parent, outgoing.Parent); Assert.Equal("Retained outgoing field", field.Text);
        ribbon.SettleMotion(); Assert.False(ribbon.IsAnimating);
    });

    private sealed class PageProbe : Panel
    {
        internal Action? FirstVisiblePaint;
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Visible && FirstVisiblePaint is { } observe) { FirstVisiblePaint = null; observe(); }
            base.OnPaint(e);
        }
    }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint window, int index);
}
