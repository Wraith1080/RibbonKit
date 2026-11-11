using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageMotionSettingsTests
{
    [Theory] [InlineData(RibbonBackstageStyle.Modern)] [InlineData(RibbonBackstageStyle.Classic)]
    public void Authored_durations_curves_fade_and_page_distance_drive_existing_native_engines(RibbonBackstageStyle style) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(style);
        var settings = new RibbonBackstageMotionSettings { PaneDuration = 480, PageDuration = 260, PageSlideDistance = 20,
            PaneEasing = RibbonBackstageEasing.Linear, TitleEasing = RibbonBackstageEasing.SmoothStep,
            PaneFadeEasing = RibbonBackstageEasing.Linear, PageEasing = RibbonBackstageEasing.Linear, PaneFadeAmount = .5 };
        f.Ribbon.Theme = f.Ribbon.Theme with { BackstageMotion = settings };
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
        var normal = f.Host.CaptionTitleDrawingBounds(f.Ribbon);
        f.Ribbon.IsBackstageOpen = true;
        var pane = Assert.IsType<RibbonBackstageMotion>(f.Ribbon.BackstageOpeningMotion); var surface = f.Ribbon.BackstageSurface!;
        Assert.Equal(128, pane.Opacity); Assert.Equal(480, f.Ribbon.BackstageMotionPolicy.PaneDuration);
        int travel = Math.Max(1, (int)Math.Round(surface.Width * .15));
        var title = Assert.IsType<RibbonCaptionTitleMotion>(f.Host.CaptionTitleMotion); var titleStart = title.Current;
        int centered = (f.Host.ClientSize.Width - titleStart.Width) / 2;
        pane.SetProgress(.25); title.SetProgress(.25);
        Assert.Equal(-(int)Math.Round(travel * .75), surface.Left); Assert.Equal(159, pane.Opacity);
        Assert.Equal((int)Math.Round(titleStart.Left + (centered - titleStart.Left) * .15625), title.Current.Left);
        f.Ribbon.SettleMotion(); nint handle = f.Field.Handle; var parent = f.Field.Parent; f.Field.Select(2, 4);

        foreach (var contentStyle in new[] { RibbonTabContentAnimation.SlideDown, RibbonTabContentAnimation.Fade })
        {
            f.Ribbon.TabContentAnimation = contentStyle;
            f.Ribbon.Backstage.SelectedPage = contentStyle == RibbonTabContentAnimation.SlideDown ? f.Second : f.First;
            var page = Assert.IsType<RibbonBackstagePageMotion>(surface.PageMotion);
            Assert.Equal(260, page.Duration); page.SetProgress(.25);
            if (contentStyle == RibbonTabContentAnimation.SlideDown)
                Assert.Equal(-(int)Math.Round(RibbonLayoutEngine.Scale(20, f.Ribbon.DeviceDpi) * .75), page.Target.Top);
            else { Assert.Equal(64, page.Opacity); Assert.Equal(Point.Empty, page.Target.Location); }
            f.Ribbon.SettleMotion(); Assert.Equal(Point.Empty, page.Target.Location); Assert.Equal(255, page.Target.PageOpacity);
        }
        Assert.Equal(handle, f.Field.Handle); Assert.Same(parent, f.Field.Parent); Assert.Equal("Retained native input", f.Field.Text);
        Assert.Equal(2, f.Field.SelectionStart); Assert.Equal(4, f.Field.SelectionLength);
        f.Ribbon.IsBackstageOpen = false; pane = Assert.IsType<RibbonBackstageMotion>(f.Ribbon.BackstageOpeningMotion);
        Assert.Equal(255, pane.Opacity); pane.SetProgress(.25); Assert.Equal(223, pane.Opacity);
        f.Ribbon.SettleMotion(); Assert.False(f.Ribbon.IsAnimating); Assert.False(f.Ribbon.IsBackstageOpen);
        Assert.Equal(normal, f.Host.CaptionTitleDrawingBounds(f.Ribbon));
    });

    [Theory] [InlineData(RibbonBackstageStyle.Modern)] [InlineData(RibbonBackstageStyle.Classic)]
    public void Opaque_slide_has_no_preparation_flash_and_zero_durations_snap_without_losing_owners(RibbonBackstageStyle style) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(style);
        f.Ribbon.Theme = f.Ribbon.Theme with { BackstageMotion = new() { PaneFadeAmount = 0, PageSlideDistance = 0 } };
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.IsBackstageOpen = true;
        var pane = Assert.IsType<RibbonBackstageMotion>(f.Ribbon.BackstageOpeningMotion);
        Assert.Equal(255, pane.Opacity);
        foreach (double p in new[] { 0d, .25, .5, 1 }) { pane.SetProgress(p); Assert.Equal(255, pane.Opacity); }
        f.Ribbon.SettleMotion(); nint handle = f.Field.Handle; var parent = f.Field.Parent;
        f.Ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown; f.Ribbon.Backstage.SelectedPage = f.Second;
        var page = Assert.IsType<RibbonBackstagePageMotion>(f.Ribbon.BackstageSurface!.PageMotion);
        Assert.Equal(Point.Empty, page.Target.Location); page.SetProgress(.5); Assert.Equal(Point.Empty, page.Target.Location); f.Ribbon.SettleMotion();
        f.Ribbon.IsBackstageOpen = false; pane = Assert.IsType<RibbonBackstageMotion>(f.Ribbon.BackstageOpeningMotion);
        foreach (double p in new[] { 0d, .25, .5, 1 }) { pane.SetProgress(p); Assert.Equal(255, pane.Opacity); }
        f.Ribbon.SettleMotion();
        f.Ribbon.Theme = f.Ribbon.Theme with { BackstageMotion = new() { PaneDuration = 0, PageDuration = 0 } };
        int closed = 0; f.Ribbon.Backstage.Closed += (_, _) => closed++;
        f.Ribbon.IsBackstageOpen = true; Assert.True(f.Ribbon.IsBackstageOpen); Assert.False(f.Ribbon.IsAnimating); Assert.Null(f.Host.CaptionTitleMotion);
        f.Ribbon.Backstage.SelectedPage = f.First; Assert.False(f.Ribbon.IsAnimating); Assert.Null(f.Ribbon.BackstageSurface!.PageMotion);
        f.Ribbon.IsBackstageOpen = false; Assert.False(f.Ribbon.IsBackstageOpen); Assert.False(f.Ribbon.IsAnimating); Assert.Equal(1, closed);
        Assert.Equal(handle, f.Field.Handle); Assert.Same(parent, f.Field.Parent);
    });

    [Theory]
    [InlineData(RibbonBackstageEasing.Linear, .25)] [InlineData(RibbonBackstageEasing.SmoothStep, .15625)] [InlineData(RibbonBackstageEasing.EaseOutCubic, .578125)]
    public void Public_curves_have_known_progress_and_preserve_native_alpha_limits(RibbonBackstageEasing curve, double quarter) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2024Light with { BackstageMotion = new()
            { PaneEasing = curve, PaneFadeEasing = curve, TitleEasing = curve, PageEasing = curve } } };
        var policy = ribbon.BackstageMotionPolicy;
        Assert.Equal(quarter, policy.PaneProgress(.25)); Assert.Equal(quarter, policy.TitleProgress(.25)); Assert.Equal(quarter, policy.PageProgress(.25));
        Assert.Equal(0, policy.PaneProgress(-1)); Assert.Equal(1, policy.PageProgress(2));
        Assert.Equal(Math.Max(1, (int)Math.Round(255 * quarter)), policy.PaneOpacity(.25, false));
        Assert.Equal(1, policy.PaneOpacity(-1, false)); Assert.Equal(255, policy.PaneOpacity(2, false));
    });

    [Theory]
    [InlineData(nameof(RibbonBackstageMotionSettings.PaneDuration))] [InlineData(nameof(RibbonBackstageMotionSettings.PageDuration))]
    [InlineData(nameof(RibbonBackstageMotionSettings.PageSlideDistance))] [InlineData(nameof(RibbonBackstageMotionSettings.PaneFadeAmount))]
    [InlineData(nameof(RibbonBackstageMotionSettings.PaneEasing))] [InlineData(nameof(RibbonBackstageMotionSettings.PaneFadeEasing))]
    [InlineData(nameof(RibbonBackstageMotionSettings.TitleEasing))] [InlineData(nameof(RibbonBackstageMotionSettings.PageEasing))]
    public void Invalid_nested_values_are_rejected_before_theme_or_builtin_selection_changes(string member) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme;
        var property = typeof(RibbonBackstageMotionSettings).GetProperty(member)!;
        object[] invalid = member == nameof(RibbonBackstageMotionSettings.PaneFadeAmount) ? [double.NaN, double.PositiveInfinity, double.NegativeInfinity, -.01, 1.01]
            : property.PropertyType == typeof(RibbonBackstageEasing?) ? [(RibbonBackstageEasing)99]
            : [-1, member == nameof(RibbonBackstageMotionSettings.PageSlideDistance) ? 1025 : 60001];
        foreach (object value in invalid)
        {
            var settings = new RibbonBackstageMotionSettings(); property.SetValue(settings, value);
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { BackstageMotion = settings });
            Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme);
        }
        ribbon.Theme = original with { BackstageMotion = new() { PaneDuration = 60000, PageDuration = 60000, PageSlideDistance = 1024, PaneFadeAmount = 1 } };
        Assert.Null(ribbon.BuiltInTheme);
    });

    [Fact]
    public void Immutable_options_reset_independently_and_existing_policy_still_settles_active_transitions() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(RibbonBackstageStyle.Classic); using var other = new RibbonControl();
        var settings = new RibbonBackstageMotionSettings { PaneDuration = 700, PageDuration = 340, PaneFadeAmount = .75 };
        var application = f.Ribbon.Theme with { BackstageMotion = settings }; f.Ribbon.Theme = application;
        f.Ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; Assert.Null(f.Ribbon.Theme.BackstageMotion);
        f.Ribbon.BuiltInTheme = null; Assert.Same(application, f.Ribbon.Theme); Assert.Same(settings, f.Ribbon.Theme.BackstageMotion); Assert.Null(other.Theme.BackstageMotion);
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.IsBackstageOpen = true;
        var motion = Assert.IsType<RibbonBackstageMotion>(f.Ribbon.BackstageOpeningMotion);
        f.Ribbon.Theme = application with { BackstageMotion = settings with { PaneDuration = 200 } };
        Assert.False(motion.IsRunning); Assert.False(f.Ribbon.IsAnimating); Assert.Equal(700, settings.PaneDuration);
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.None; f.Ribbon.IsBackstageOpen = false; f.Ribbon.IsBackstageOpen = true; Assert.False(f.Ribbon.IsAnimating);
        f.Ribbon.IsBackstageOpen = false; f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.Motion.SystemPolicyOverride = false;
        f.Ribbon.IsBackstageOpen = true; Assert.False(f.Ribbon.IsAnimating);
        f.Ribbon.Theme = application with { BackstageMotion = settings with { PaneDuration = null } };
        Assert.Equal(220, f.Ribbon.BackstageMotionPolicy.PaneDuration); Assert.Equal(340, f.Ribbon.BackstageMotionPolicy.PageDuration);
        Assert.Equal(.75, f.Ribbon.BackstageMotionPolicy.PaneFadeAmount); Assert.Equal(700, settings.PaneDuration);
        f.Ribbon.Theme = application with { BackstageMotion = null }; Assert.Null(f.Ribbon.Theme.BackstageMotion);
    });

    private sealed class Fixture : IDisposable
    {
        internal readonly RibbonForm Host = new() { Text = "Backstage authored motion", StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1000, 650), ShowInTaskbar = false };
        internal readonly RibbonControl Ribbon;
        internal readonly TextBox Field = new() { Text = "Retained native input" };
        internal readonly Panel Page = new();
        internal readonly RibbonBackstagePageItem First, Second;
        internal Fixture(RibbonBackstageStyle style)
        {
            Ribbon = new() { FileMenuMode = RibbonFileMenuMode.Backstage,
                Theme = (style == RibbonBackstageStyle.Classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { BackstageTitleCentered = true } };
            First = new() { Text = "Info", Content = Field }; Second = new() { Text = "Print", Content = Page };
            Ribbon.FileMenu.Visible = true; Ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); Ribbon.Backstage.Items.AddRange(First, Second);
            Host.Controls.Add(Ribbon); Host.Ribbon = Ribbon; Host.Show(); Application.DoEvents(); Ribbon.Motion.SystemPolicyOverride = true;
        }
        public void Dispose() { Host.Dispose(); Field.Dispose(); Page.Dispose(); }
    }
}
