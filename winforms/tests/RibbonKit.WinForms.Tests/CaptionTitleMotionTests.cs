using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class CaptionTitleMotionTests
{
    [Fact]
    public void Resize_maximize_restore_recenter_the_rendered_title_immediately() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Text = "Centered title", ClientSize = new(1000, 600) };
        using var ribbon = Ribbon(form, RibbonBuiltInTheme.Office2019Light); form.Show(); Application.DoEvents();
        ribbon.IsBackstageOpen = true;
        foreach (int width in new[] { 730, 1200, 640 })
        {
            form.ClientSize = new(width, 580); Application.DoEvents();
            Assert.Null(form.CaptionTitleMotion); AssertTitleCenter(form, ribbon);
        }
        form.WindowState = FormWindowState.Maximized; Application.DoEvents(); AssertTitleCenter(form, ribbon);
        form.WindowState = FormWindowState.Normal; Application.DoEvents(); AssertTitleCenter(form, ribbon);
        Assert.True(ribbon.IsBackstageOpen);
    });

    [Fact]
    public void Open_close_and_reversal_place_title_75_percent_then_slide_but_resize_snaps() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Text = "Moving title", ClientSize = new(1000, 600) };
        using var ribbon = Ribbon(form, RibbonBuiltInTheme.Office2019Light); form.Show(); Application.DoEvents();
        ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.Motion.SystemPolicyOverride = true;
        var normal = form.CaptionTitleDrawingBounds(ribbon);
        int centered = (form.ClientSize.Width - normal.Width) / 2;
        ribbon.IsBackstageOpen = true;
        var opening = Assert.IsType<RibbonCaptionTitleMotion>(form.CaptionTitleMotion);
        Assert.Equal((int)Math.Round(centered + (normal.Left - centered) * .25), opening.Current.Left);
        AssertOpaqueTitle();
        opening.SetProgress(.4); var midway = opening.Current;
        Assert.True(midway.Left > normal.Left);
        ribbon.IsBackstageOpen = false;
        var closing = Assert.IsType<RibbonCaptionTitleMotion>(form.CaptionTitleMotion);
        Assert.Equal((int)Math.Round(normal.Left + (midway.Left - normal.Left) * .25), closing.Current.Left);
        AssertOpaqueTitle(); closing.SetProgress(.3); var reverseFrom = closing.Current;
        Assert.True(reverseFrom.Left < midway.Left);
        ribbon.IsBackstageOpen = true;
        Assert.Equal((int)Math.Round(centered + (reverseFrom.Left - centered) * .25), Assert.IsType<RibbonCaptionTitleMotion>(form.CaptionTitleMotion).Current.Left);
        form.ClientSize = new(800, 560); Application.DoEvents();
        Assert.Null(form.CaptionTitleMotion); AssertTitleCenter(form, ribbon);
        ribbon.IsBackstageOpen = false;
        Assert.NotNull(form.CaptionTitleMotion);
        ribbon.SettleMotion();
        Assert.False(ribbon.IsBackstageOpen); Assert.False(ribbon.IsAnimating); Assert.Null(ribbon.CaptureBackstageCaption());
        Assert.Equal(normal.Left, form.CaptionTitleDrawingBounds(ribbon).Left);

        void AssertOpaqueTitle()
        {
            using var frame = ribbon.CaptureBackstageCaption(); Assert.NotNull(frame);
            var title = form.CaptionTitleDrawingBounds(ribbon);
            Assert.Contains(Enumerable.Range(title.Left, title.Width), x => Enumerable.Range(0, frame.Height).Any(y => frame.GetPixel(x, y).A == 255));
        }
    });

    [Fact]
    public void Title_slide_distance_is_shared_configurable_and_validated_per_ribbon() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Text = "Short title slide", ClientSize = new(1000, 600) };
        using var ribbon = Ribbon(form, RibbonBuiltInTheme.Office2019Light); using var other = new RibbonControl();
        form.Show(); Application.DoEvents(); ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.Motion.SystemPolicyOverride = true;
        foreach (double travel in new[] { 0, .25, 1 })
        {
            ribbon.Theme = ribbon.Theme with { BackstageTitleSlideDistanceRatio = travel };
            var normal = form.CaptionTitleDrawingBounds(ribbon);
            int centered = (form.ClientSize.Width - normal.Width) / 2;
            ribbon.IsBackstageOpen = true;
            Assert.Equal((int)Math.Round(centered + (normal.Left - centered) * travel), form.CaptionTitleDrawingBounds(ribbon).Left);
            Assert.Equal(travel > 0, form.IsCaptionTitleAnimating);
            ribbon.SettleMotion(); ribbon.IsBackstageOpen = false;
            Assert.Equal((int)Math.Round(normal.Left + (centered - normal.Left) * travel), form.CaptionTitleDrawingBounds(ribbon).Left);
            Assert.Equal(travel > 0, form.IsCaptionTitleAnimating); ribbon.SettleMotion();
        }
        var selected = ribbon.Theme;
        foreach (double invalid in new[] { -.01, 1.01, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = selected with { BackstageTitleSlideDistanceRatio = invalid });
        Assert.Same(selected, ribbon.Theme); Assert.Null(other.Theme.BackstageTitleSlideDistanceRatio);
    });

    [Fact]
    public void Office2024_keeps_its_own_title_bar_when_open_resized_and_switched_from_2019() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Text = "Office 2024 title", ClientSize = new(1000, 600), TitleBarColor = Color.Purple };
        using var ribbon = Ribbon(form, RibbonBuiltInTheme.Office2024Light); form.Show(); Application.DoEvents();
        using var field = new TextBox { Text = "Retained page" };
        ((RibbonBackstagePageItem)ribbon.Backstage.Items[0]).Content = field;
        var handle = field.Handle;
        ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.Motion.SystemPolicyOverride = true;
        ribbon.IsBackstageOpen = true;
        AssertSeparateCaption();
        ribbon.SettleMotion();
        foreach (int width in new[] { 730, 1200 }) { form.ClientSize = new(width, 580); AssertSeparateCaption(); }
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light;
        Assert.Equal(0, ribbon.BackstageSurface!.Top);
        using (var caption = ribbon.CaptureBackstageCaption()) Assert.NotNull(caption);
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        AssertSeparateCaption();
        // Full-height presentation is an independent, per-theme override.
        ribbon.Theme = ribbon.Theme with { BackstageExtendsIntoTitleBar = true };
        Assert.Equal(0, ribbon.BackstageSurface!.Top);
        ribbon.Theme = ribbon.Theme with { BackstageExtendsIntoTitleBar = false };
        AssertSeparateCaption();
        ribbon.IsBackstageOpen = false; Assert.Null(form.CaptionTitleMotion);
        ribbon.SettleMotion(); Assert.Null(ribbon.CaptureBackstageCaption());
        Assert.Equal(Color.Purple, form.TitleBarColor); Assert.Equal(handle, field.Handle); Assert.Equal("Retained page", field.Text);

        void AssertSeparateCaption()
        {
            Assert.Equal(form.CaptionHeight, ribbon.BackstageSurface!.Top);
            Assert.Equal(form.ClientSize.Height, ribbon.BackstageSurface.Bottom);
            Assert.Equal(0, ribbon.BackstageSurface.CaptionHeight);
            Assert.False(ribbon.HasBackstageCaption); Assert.True(ribbon.CaptionOverlayBounds.IsEmpty);
            Assert.Null(ribbon.CaptureBackstageCaption()); Assert.Null(form.CaptionTitleMotion);
            Assert.Equal(form.Caption.ContentLeft, form.CaptionTitleDrawingBounds(ribbon).Left);
            using var caption = new Bitmap(form.ClientSize.Width, form.CaptionHeight);
            using var graphics = Graphics.FromImage(caption);
            ribbon.PaintCaptionBackground(graphics, new(Point.Empty, caption.Size), false);
            Assert.Equal(Color.Purple.ToArgb(), caption.GetPixel(caption.Width / 2, 2).ToArgb());
            Assert.Equal(handle, field.Handle);
        }
    });

    [Fact]
    public void Disabled_motion_and_animation_setting_changes_commit_title_positions_immediately() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Text = "Policy title", ClientSize = new(1000, 600) };
        using var ribbon = Ribbon(form, RibbonBuiltInTheme.Office2019Light); form.Show(); Application.DoEvents();
        ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.Motion.SystemPolicyOverride = false;
        ribbon.IsBackstageOpen = true; Assert.Null(form.CaptionTitleMotion); AssertTitleCenter(form, ribbon);
        ribbon.IsBackstageOpen = false; Assert.Null(form.CaptionTitleMotion);
        ribbon.Motion.SystemPolicyOverride = true; ribbon.IsBackstageOpen = true; Assert.NotNull(form.CaptionTitleMotion);
        ribbon.AnimationLevel = RibbonAnimationLevel.None;
        Assert.Null(form.CaptionTitleMotion); AssertTitleCenter(form, ribbon);
        ribbon.IsBackstageOpen = false; ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
        ribbon.IsBackstageOpen = true;
        ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu;
        Assert.False(ribbon.IsBackstageOpen); Assert.False(ribbon.IsAnimating); Assert.Null(ribbon.CaptureBackstageCaption());
    });

    private static RibbonControl Ribbon(RibbonForm form, RibbonBuiltInTheme theme)
    {
        var ribbon = new RibbonControl { BuiltInTheme = theme, FileMenuMode = RibbonFileMenuMode.Backstage };
        ribbon.FileMenu.Visible = true;
        var save = new RibbonButtonItem { Text = "Save" }; var group = new RibbonGroup { Text = "Commands" }; group.Items.Add(save);
        var home = new RibbonTab { Text = "Home" }; home.Groups.Add(group); ribbon.Tabs.Add(home);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = save }); ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info" });
        form.Controls.Add(ribbon); form.Ribbon = ribbon; return ribbon;
    }
    private static void AssertTitleCenter(RibbonForm form, RibbonControl ribbon)
    {
        using var frame = ribbon.CaptureBackstageCaption(); Assert.NotNull(frame);
        Assert.Equal(form.ClientSize.Width, frame.Width); Assert.Equal(form.CaptionHeight, frame.Height);
        var clip = form.CaptionTextBounds(ribbon); int left = clip.Right, right = clip.Left;
        for (int y = 0; y < frame.Height; y++) for (int x = clip.Left; x < clip.Right; x++)
            if (frame.GetPixel(x, y).A > 0) { left = Math.Min(left, x); right = Math.Max(right, x); }
        Assert.True(right > left); Assert.InRange(Math.Abs((left + right) / 2d - form.ClientSize.Width / 2d), 0, 8);
    }
}
