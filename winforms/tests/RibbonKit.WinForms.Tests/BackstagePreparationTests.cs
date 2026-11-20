using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstagePreparationTests
{
    [Fact]
    public void Initial_native_paint_does_not_consume_the_close_animation_duration() => FoundationTests.Sta(() =>
    {
        Application.EnableVisualStyles();
        using var host = new Form { Bounds = new(80, 100, 900, 650), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var renderer = new SlowFirstFrame();
        var ribbon = new RibbonControl { Renderer = renderer, FileMenuMode = RibbonFileMenuMode.Backstage };
        using var page = new TextBox { Text = "Retained native page" };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        host.Controls.Add(ribbon); host.Show(); Application.DoEvents();
        ribbon.Motion.SystemPolicyOverride = true; ribbon.IsBackstageOpen = true;
        renderer.Delay = 250;
        ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        ribbon.IsBackstageOpen = false;
        Assert.Equal(0, renderer.Delay);
        Application.DoEvents();
        Assert.True(ribbon.IsAnimating, "Preparing the first native drawing must not spend the animation's time budget.");
        Assert.True(ribbon.IsBackstageOpen);
        ribbon.SettleMotion();
        Assert.False(ribbon.IsBackstageOpen);
        Assert.Equal("Retained native page", page.Text);
    });

    private sealed class SlowFirstFrame : RibbonRenderer
    {
        internal int Delay;
        public override void DrawBackstageBackground(RibbonPaintContext context, int navigationWidth)
        {
            int delay = Delay; Delay = 0;
            if (delay > 0) Thread.Sleep(delay);
            base.DrawBackstageBackground(context, navigationWidth);
        }
    }
}
