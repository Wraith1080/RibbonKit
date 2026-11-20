using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageTravelPreparationTests
{
    [Theory]
    [InlineData(RibbonBuiltInTheme.Office2024Light)]
    [InlineData(RibbonBuiltInTheme.Office2010Blue)]
    public void Solid_opening_seeds_the_full_native_pane_before_travel_without_repainting_unexposed_fields(RibbonBuiltInTheme theme) => FoundationTests.Sta(() =>
    {
        Application.EnableVisualStyles();
        using var host = new RibbonForm { Bounds = new(80, 80, 1050, 750), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        var ribbon = new RibbonControl { BuiltInTheme = theme, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive };
        using var renderer = new PreparationProbe(ribbon); ribbon.Renderer = renderer;
        using var page = new Panel { BackColor = Color.White };
        var field = new FieldProbe { Text = "Retained native field", Bounds = new(350, 160, 200, 30) };
        page.Controls.Add(field);
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents();
        ribbon.Motion.SystemPolicyOverride = true;
        ribbon.IsBackstageOpen = true;
        var surface = ribbon.BackstageSurface!; var motion = ribbon.BackstageOpeningMotion!;
        Assert.Equal(0, renderer.FirstPaintLeft);
        Assert.Equal(1, renderer.FirstPaintAlpha);
        Assert.True(surface.Left < 0, "Preparation must retain the authored slide.");
        var parent = field.Parent; nint handle = field.Handle;
        field.Select(2, 4); RibbonDrawingHold.UpdatePending(field.Handle); field.Paints = field.Erases = 0;
        foreach (double progress in new[] { .25, .5, .75, 1d }) motion.SetProgress(progress);
        Assert.Equal(0, field.Paints); Assert.Equal(0, field.Erases);
        Assert.Same(parent, field.Parent); Assert.Equal(handle, field.Handle);
        Assert.Equal("Retained native field", field.Text); Assert.Equal(2, field.SelectionStart); Assert.Equal(4, field.SelectionLength);
        ribbon.SettleMotion(); Assert.Equal(0, surface.Left);
    });

    [Fact]
    public void Cancelling_motion_from_initial_native_paint_keeps_the_open_pane_at_its_settled_origin() => FoundationTests.Sta(() =>
    {
        Application.EnableVisualStyles();
        using var host = new RibbonForm { Bounds = new(80, 80, 950, 650), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive };
        using var renderer = new PreparationProbe(ribbon) { FirstPainted = () => ribbon.AnimationLevel = RibbonAnimationLevel.None };
        using var page = new TextBox { Text = "Retained content" };
        ribbon.Renderer = renderer; ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents();
        ribbon.Motion.SystemPolicyOverride = true; ribbon.IsBackstageOpen = true;
        Assert.NotNull(renderer.FirstPaintLeft); Assert.True(ribbon.IsBackstageOpen); Assert.False(ribbon.IsAnimating);
        Assert.Equal(0, ribbon.BackstageSurface!.Left); Assert.Equal(255, ribbon.BackstageSurface.MotionAlpha);
        Assert.Equal("Retained content", page.Text);
    });

    private sealed class PreparationProbe(RibbonControl ribbon) : RibbonOffice2024Renderer
    {
        internal int? FirstPaintLeft, FirstPaintAlpha;
        internal Action? FirstPainted;
        public override void DrawBackstageBackground(RibbonPaintContext context, int navigationWidth)
        {
            if (FirstPaintLeft is null && ribbon.BackstageSurface is { Visible: true } surface)
            {
                FirstPaintLeft = surface.Left;
                if (GetLayeredWindowAttributes(surface.Handle, out _, out byte alpha, out _)) FirstPaintAlpha = alpha;
                var painted = FirstPainted; FirstPainted = null; painted?.Invoke();
            }
            base.DrawBackstageBackground(context, navigationWidth);
        }
    }
    private sealed class FieldProbe : TextBox
    {
        internal int Paints, Erases;
        protected override void WndProc(ref Message message)
        {
            if (Visible && message.Msg == 0xF) Paints++;
            if (Visible && message.Msg == 0x14) Erases++;
            base.WndProc(ref message);
        }
    }
    [DllImport("user32.dll")] private static extern bool GetLayeredWindowAttributes(nint window, out uint color, out byte alpha, out uint flags);
}
