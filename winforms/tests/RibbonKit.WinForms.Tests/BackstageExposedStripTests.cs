using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageExposedStripTests
{
    [Theory]
    [InlineData(RibbonAnimationLevel.Subtle, false)]
    [InlineData(RibbonAnimationLevel.Expressive, false)]
    [InlineData(RibbonAnimationLevel.Subtle, true)]
    [InlineData(RibbonAnimationLevel.Expressive, true)]
    public void Slide_down_paints_exposed_rows_and_rounded_edge_without_repainting_page_body(RibbonAnimationLevel level, bool settleEarly) => FoundationTests.Sta(() =>
    {
        Application.EnableVisualStyles();
        using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 950, 650), ShowInTaskbar = false };
        using var first = new Panel(); using var second = new PaintPage();
        var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        var a = new RibbonBackstagePageItem { Text = "Info", Content = first }; var b = new RibbonBackstagePageItem { Text = "Print", Content = second };
        ribbon.Backstage.Items.AddRange(a, b); host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents();
        ribbon.Motion.SystemPolicyOverride = true; ribbon.IsBackstageOpen = true;
        ribbon.Backstage.SelectedPage = b; ribbon.Backstage.SelectedPage = a; Application.DoEvents();
        ribbon.AnimationLevel = level; ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown;
        ribbon.Backstage.SelectedPage = b; var motion = ribbon.BackstageSurface!.PageMotion!;
        int travel = -motion.Target.Top; Assert.True(travel > 0); second.Painted.Clear();
        motion.SetProgress(.5);
        if (settleEarly) ribbon.SettleMotion();
        else motion.SetProgress(1);
        Assert.Contains(second.Painted, clip => clip.Top == 0 && clip.Bottom > 0);
        Assert.Contains(second.Painted, clip => clip.Bottom > travel); // The rounded edge exposes pixels below the rectangular top strip.
        Assert.All(second.Painted, clip => Assert.InRange(clip.Bottom, 1, second.Height / 10));
        ribbon.SettleMotion(); Assert.Equal(Point.Empty, second.Parent!.Location);
    });
    private sealed class PaintPage : Panel
    {
        internal readonly List<Rectangle> Painted = [];
        internal PaintPage() => BackColor = Color.White;
        protected override void OnPaint(PaintEventArgs e) { Painted.Add(e.ClipRectangle); base.OnPaint(e); }
    }
}
