using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstagePresentationPolicyTests
{
    [Theory]
    [InlineData(RibbonBackstageStyle.Modern, RibbonAnimationLevel.Subtle)]
    [InlineData(RibbonBackstageStyle.Modern, RibbonAnimationLevel.Expressive)]
    [InlineData(RibbonBackstageStyle.Classic, RibbonAnimationLevel.Subtle)]
    [InlineData(RibbonBackstageStyle.Classic, RibbonAnimationLevel.Expressive)]
    public void Mixed_caption_settings_keep_actual_pane_fade_and_short_title_curves_in_both_directions(RibbonBackstageStyle style, RibbonAnimationLevel level) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Text = "Policy travel", ClientSize = new(1000, 600) };
        using var ribbon = Create(form, style);
        using var field = new TextBox { Text = "Retained policy value" };
        ((RibbonBackstagePageItem)ribbon.Backstage.Items[0]).Content = field;
        // Deliberately oppose the built-ins' coverage while centering both titles.
        ribbon.Theme = ribbon.Theme with { BackstageExtendsIntoTitleBar = style == RibbonBackstageStyle.Modern, BackstageTitleCentered = true };
        form.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true; ribbon.AnimationLevel = level;
        var normal = form.CaptionTitleDrawingBounds(ribbon);
        int centered = (form.ClientSize.Width - normal.Width) / 2;
        ribbon.IsBackstageOpen = true;
        var surface = ribbon.BackstageSurface!; var handle = field.Handle; var parent = field.Parent;
        int travel = Math.Max(1, (int)Math.Round(surface.Width * .15));
        foreach (bool closing in new[] { false, true })
        {
            if (closing) { ribbon.SettleMotion(); ribbon.IsBackstageOpen = false; }
            var pane = Assert.IsType<RibbonBackstageMotion>(ribbon.BackstageOpeningMotion);
            var title = Assert.IsType<RibbonCaptionTitleMotion>(form.CaptionTitleMotion);
            int target = closing ? normal.Left : centered;
            int origin = closing ? centered : normal.Left;
            int titleStart = (int)Math.Round(target + (origin - target) * .25);
            Assert.Equal(titleStart, title.Current.Left);
            pane.SetProgress(.25); title.SetProgress(.25);
            Assert.Equal(-(int)Math.Round(travel * (closing ? .578125 : .421875)), surface.Left);
            Assert.Equal(closing ? 215 : 40, pane.Opacity);
            Assert.Equal((int)Math.Round(titleStart + (target - titleStart) * .578125), title.Current.Left);
            Assert.Equal(style == RibbonBackstageStyle.Modern ? 0 : form.CaptionHeight, surface.Top);
            Assert.True(ribbon.BackstageCaptionTintBounds.IsEmpty);
            Assert.Equal(handle, field.Handle); Assert.Same(parent, field.Parent); Assert.Equal("Retained policy value", field.Text);
        }
        ribbon.SettleMotion(); Assert.False(ribbon.IsBackstageOpen); Assert.Null(ribbon.CaptureBackstageCaption());
    });

    [Theory] [InlineData(RibbonBackstageStyle.Modern)] [InlineData(RibbonBackstageStyle.Classic)]
    public void Ordinary_form_ignores_integrated_caption_requests_and_retains_shared_pane_motion(RibbonBackstageStyle style) => FoundationTests.Sta(() =>
    {
        using var form = new Form { ClientSize = new(900, 560) };
        using var ribbon = Create(form, style);
        ribbon.Theme = ribbon.Theme with { BackstageExtendsIntoTitleBar = true, BackstageTitleCentered = true, Background = Color.Ivory };
        form.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true; ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        ribbon.IsBackstageOpen = true;
        Assert.Equal(0, ribbon.BackstageSurface!.Top); Assert.False(ribbon.HasFullHeightBackstage); Assert.False(ribbon.HasBackstageCaption);
        Assert.True(ribbon.BackstageCaptionTintBounds.IsEmpty); Assert.Null(ribbon.CaptureBackstageCaption());
        Assert.Equal(Color.Ivory, ribbon.CaptionBackground(false)); Assert.Equal(Color.Transparent, ribbon.CaptionBackground(true));
        var motion = Assert.IsType<RibbonBackstageMotion>(ribbon.BackstageOpeningMotion);
        motion.SetProgress(.5); Assert.True(ribbon.BackstageSurface.Left < 0); Assert.Equal(128, motion.Opacity);
        ribbon.SettleMotion(); ribbon.IsBackstageOpen = false; ribbon.SettleMotion(); Assert.False(ribbon.IsAnimating);
    });

    [Theory]
    [InlineData(RibbonBackstageStyle.Modern, RibbonAnimationLevel.Subtle)]
    [InlineData(RibbonBackstageStyle.Modern, RibbonAnimationLevel.Expressive)]
    [InlineData(RibbonBackstageStyle.Classic, RibbonAnimationLevel.Subtle)]
    [InlineData(RibbonBackstageStyle.Classic, RibbonAnimationLevel.Expressive)]
    public void Native_page_switches_keep_existing_duration_travel_and_fade_for_each_family(RibbonBackstageStyle family, RibbonAnimationLevel level) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { ClientSize = new(1000, 600) };
        using var ribbon = Create(form, family);
        using var first = new TextBox { Text = "Retained first value" }; using var second = new TextBox { Text = "Retained second value" };
        var a = (RibbonBackstagePageItem)ribbon.Backstage.Items[0]; a.Content = first;
        var b = new RibbonBackstagePageItem { Text = "Print", Content = second }; ribbon.Backstage.Items.Add(b);
        form.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true; ribbon.IsBackstageOpen = true;
        var firstHandle = first.Handle; var secondHandle = second.Handle; var firstParent = first.Parent; var secondParent = second.Parent;
        ribbon.AnimationLevel = level;
        foreach (var style in new[] { RibbonTabContentAnimation.Fade, RibbonTabContentAnimation.SlideDown })
        {
            ribbon.TabContentAnimation = style; ribbon.Backstage.SelectedPage = style == RibbonTabContentAnimation.Fade ? b : a;
            var motion = Assert.IsType<RibbonBackstagePageMotion>(ribbon.BackstageSurface!.PageMotion);
            Assert.Equal(level == RibbonAnimationLevel.Expressive ? 182 : 130, motion.Duration);
            motion.SetProgress(.25);
            if (style == RibbonTabContentAnimation.Fade) { Assert.Equal(147, motion.Opacity); Assert.Equal(Point.Empty, motion.Target.Location); }
            else
            {
                int travel = RibbonLayoutEngine.Scale(level == RibbonAnimationLevel.Expressive ? 12 : 8, ribbon.DeviceDpi);
                Assert.Equal(-(int)Math.Round(travel * .421875), motion.Target.Top); Assert.Equal(255, motion.Opacity);
            }
            Assert.Equal(firstHandle, first.Handle); Assert.Equal(secondHandle, second.Handle);
            Assert.Same(firstParent, first.Parent); Assert.Same(secondParent, second.Parent);
            Assert.Equal("Retained first value", first.Text); Assert.Equal("Retained second value", second.Text);
            ribbon.SettleMotion(); Assert.Equal(Point.Empty, motion.Target.Location); Assert.Equal(255, motion.Target.PageOpacity);
        }
    });

    private static RibbonControl Create(Form form, RibbonBackstageStyle style)
    {
        var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage, Theme = RibbonThemes.Office2024Light with { BackstageStyle = style, Accent = Color.Magenta } };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info" }); form.Controls.Add(ribbon);
        if (form is RibbonForm host) host.Ribbon = ribbon;
        return ribbon;
    }
}
