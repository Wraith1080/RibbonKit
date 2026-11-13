using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageCaptionAppearanceTests
{
    [Fact]
    public void Stable_caption_paint_can_be_compared_across_the_rewrite() => FoundationTests.Sta(() =>
    {
        var output = Environment.GetEnvironmentVariable("RIBBONKIT_D8_CAPTURE");
        if (!string.IsNullOrEmpty(output)) Directory.CreateDirectory(output);
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (bool composited in new[] { false, true })
        foreach (bool authored in new[] { false, true })
        {
            using var form = Host(); using var ribbon = Create(form, family);
            if (authored) form.TitleBarColor = Color.Navy;
            form.Show(); Application.DoEvents();
            foreach (bool open in new[] { false, true })
            {
                ribbon.IsBackstageOpen = open;
                using var image = Paint();
                if (!string.IsNullOrEmpty(output)) image.Save(Path.Combine(output, $"{family}-{composited}-{authored}-{open}.png"));
                ribbon.Theme = ribbon.Theme with { BackstageAppearance = new() };
                using var reset = Paint();
                Assert.Equal(Pixels(image, new(Point.Empty, image.Size)), Pixels(reset, new(Point.Empty, reset.Size)));
                Bitmap Paint()
                {
                    var frame = new Bitmap(form.ClientSize.Width, form.CaptionHeight);
                    using var graphics = Graphics.FromImage(frame);
                    ribbon.PaintCaptionBackground(graphics, new(Point.Empty, frame.Size), composited);
                    form.PaintCaption(graphics, ribbon, composited, foregroundOnly: true); return frame;
                }
            }
        }
    });

    [Fact]
    public void Temporal_caption_frames_keep_each_clipped_title_segment_in_its_own_ink() => FoundationTests.Sta(() =>
    {
        using var form = Host(); using var ribbon = Create(form, RibbonBuiltInTheme.Office2019Light);
        form.Show(); Application.DoEvents(); SendMessage(form.Handle, 0x86, 1, 0);
        ribbon.Motion.SystemPolicyOverride = true; ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
        ribbon.IsBackstageOpen = true; ribbon.SettleMotion(); ribbon.IsBackstageOpen = false;
        var pane = Assert.IsType<RibbonBackstageMotion>(ribbon.BackstageOpeningMotion);
        var title = Assert.IsType<RibbonCaptionTitleMotion>(form.CaptionTitleMotion);
        var output = Environment.GetEnvironmentVariable("RIBBONKIT_D8_FRAMES");
        if (!string.IsNullOrEmpty(output)) Directory.CreateDirectory(output);
        foreach (double progress in new[] { .1, .35, .6, .8, .95 })
        {
            pane.SetProgress(progress); title.SetProgress(progress); ribbon.UpdateBackstageCaption();
            using var frame = ribbon.CaptureBackstageCaption(); Assert.NotNull(frame);
            var titleBounds = form.CaptionTitleDrawingBounds(ribbon);
            var normal = form.CaptionTheme(ribbon.Theme, true);
            Check(ribbon.CaptionNavigationBounds, form.BackstageNavigationCaptionTheme(ribbon, normal, true).Foreground);
            Check(ribbon.CaptionPageBounds, ribbon.Theme.Foreground);
            if (!string.IsNullOrEmpty(output))
            {
                frame.Save(Path.Combine(output, $"close-{progress:0.00}-plane.png"));
                using var composed = new Bitmap(frame.Width, frame.Height); using var g = Graphics.FromImage(composed);
                g.Clear(ribbon.Theme.Background);
                using var nav = new SolidBrush(Color.FromArgb(pane.Opacity, ribbon.Theme.BackstageNavigationColor)); g.FillRectangle(nav, ribbon.CaptionNavigationBounds);
                using var page = new SolidBrush(Color.FromArgb(pane.Opacity, ribbon.Theme.Surface)); g.FillRectangle(page, ribbon.CaptionPageBounds);
                g.DrawImageUnscaled(frame, Point.Empty); composed.Save(Path.Combine(output, $"close-{progress:0.00}-estimated-underlay.png"));
            }
            void Check(Rectangle part, Color ink)
            {
                var opaque = Pixels(frame, Rectangle.Intersect(titleBounds, part)).Where(c => c.A == 255);
                Assert.All(opaque, pixel => Assert.Equal(ink.ToArgb(), pixel.ToArgb()));
            }
        }
        ribbon.IsBackstageOpen = true; ribbon.SettleMotion();
        Assert.True(ribbon.IsBackstageOpen); Assert.False(ribbon.IsAnimating);
    });

    [Theory]
    [InlineData(RibbonWindowBackdrop.None, false)] [InlineData(RibbonWindowBackdrop.None, true)]
    [InlineData(RibbonWindowBackdrop.Mica, false)] [InlineData(RibbonWindowBackdrop.Mica, true)]
    [InlineData(RibbonWindowBackdrop.Acrylic, false)] [InlineData(RibbonWindowBackdrop.Acrylic, true)]
    public void Closing_title_uses_opaque_readable_ink_over_the_fading_navigation(RibbonWindowBackdrop backdrop, bool active) => FoundationTests.Sta(() =>
    {
        using var form = Host(); using var ribbon = Create(form, RibbonBuiltInTheme.Office2019Light);
        form.Backdrop = backdrop;
        form.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        SendMessage(form.Handle, 0x86, active ? 1 : 0, 0);
        ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
        ribbon.IsBackstageOpen = true; ribbon.SettleMotion(); ribbon.IsBackstageOpen = false;
        var pane = Assert.IsType<RibbonBackstageMotion>(ribbon.BackstageOpeningMotion);
        pane.SetProgress(.8);
        var titleMotion = Assert.IsType<RibbonCaptionTitleMotion>(form.CaptionTitleMotion);
        titleMotion.SetProgress(.8); ribbon.UpdateBackstageCaption();
        using var frame = ribbon.CaptureBackstageCaption(); Assert.NotNull(frame);
        var title = Rectangle.Intersect(form.CaptionTitleDrawingBounds(ribbon), ribbon.CaptionNavigationBounds);
        Assert.True(title.Width > 15);
        var pixels = Pixels(frame, title).Where(c => c.A == 255).ToArray();
        Assert.NotEmpty(pixels);
        var normal = form.CaptionTheme(ribbon.Theme, true);
        Assert.Contains(pixels, c => c.ToArgb() == (active ? normal.Foreground : normal.SecondaryText).ToArgb());
        Assert.DoesNotContain(pixels, c => c.R > 240 && c.G > 240 && c.B > 240);
        // Text clipped into the adjacent page must use page ink, with no white
        // residue from the transparent navigation pass.
        var pageTitle = Rectangle.Intersect(form.CaptionTitleDrawingBounds(ribbon), ribbon.CaptionPageBounds);
        Assert.DoesNotContain(Pixels(frame, pageTitle), c => c.A == 255 && c.R > 240 && c.G > 240 && c.B > 240);
        ribbon.IsBackstageOpen = true;
        Assert.NotNull(form.CaptionTitleMotion); ribbon.SettleMotion();
        ribbon.IsBackstageOpen = false; ribbon.SettleMotion();
        Assert.False(ribbon.IsBackstageOpen); Assert.Null(ribbon.CaptureBackstageCaption()); Assert.Null(form.CaptionTitleMotion);
    });

    [Theory] [InlineData(RibbonWindowBackdrop.None)] [InlineData(RibbonWindowBackdrop.Mica)] [InlineData(RibbonWindowBackdrop.Acrylic)]
    public void Tint_overrides_follow_motion_and_preserve_caption_and_native_ownership(RibbonWindowBackdrop backdrop) => FoundationTests.Sta(() =>
    {
        using var form = Host(); form.Backdrop = backdrop;
        using var ribbon = Create(form, RibbonBuiltInTheme.Office2024Light);
        using var field = new TextBox { Text = "Retained native value" };
        ((RibbonBackstagePageItem)ribbon.Backstage.Items[0]).Content = field;
        form.Show(); Application.DoEvents(); ribbon.Backstage.NavigationBackgroundOpacity = .65f;
        ribbon.Theme = ribbon.Theme with { BackstageAppearance = new() { CaptionTintEnabled = true, CaptionTintColor = Color.FromArgb(128, Color.Navy), CaptionTintOpacity = .5, MarkerWidth = 7 } };
        ribbon.Motion.SystemPolicyOverride = true; ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
        ribbon.IsBackstageOpen = true; var handle = field.Handle; var parent = field.Parent;
        foreach (bool closing in new[] { false, true })
        {
            if (closing) ribbon.IsBackstageOpen = false;
            var motion = Assert.IsType<RibbonBackstageMotion>(ribbon.BackstageOpeningMotion);
            foreach (double p in new[] { .2, .6, 1 })
            {
                motion.SetProgress(p);
                int original = ribbon.HasWindowBackdrop ? (int)Math.Round(motion.Opacity * .65f) : motion.Opacity;
                var tint = ribbon.CaptionBackground(ribbon.HasWindowBackdrop);
                Assert.Equal((int)Math.Round(original * .5 * 128 / 255d), tint.A);
                Assert.Equal(Color.Navy.ToArgb() & 0xffffff, tint.ToArgb() & 0xffffff);
                Assert.Equal(motion.VisibleWidth, ribbon.BackstageCaptionTintBounds.Width);
                Assert.Equal(form.CaptionHeight, ribbon.BackstageSurface!.Top); Assert.False(ribbon.HasBackstageCaption);
            }
            ribbon.SettleMotion();
        }
        ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsBackstageOpen = true;
        var authored = ribbon.Theme;
        form.TitleBarColor = Color.Purple;
        Assert.True(ribbon.BackstageCaptionTintBounds.IsEmpty); Assert.Equal(Color.Purple.ToArgb(), ribbon.CaptionBackground(false).ToArgb());
        form.TitleBarColor = Color.Empty;
        ribbon.Theme = authored with { TitleBarBackground = Color.Green };
        Assert.True(ribbon.BackstageCaptionTintBounds.IsEmpty); Assert.Equal(Color.Green.ToArgb(), ribbon.CaptionBackground(false).ToArgb());
        ribbon.Theme = authored with { BackstageExtendsIntoTitleBar = true };
        Assert.True(ribbon.BackstageCaptionTintBounds.IsEmpty); Assert.True(ribbon.HasBackstageCaption);
        ribbon.Theme = authored with { BackstageAppearance = authored.BackstageAppearance! with { CaptionTintEnabled = false } };
        Assert.True(ribbon.BackstageCaptionTintBounds.IsEmpty);
        ribbon.Theme = authored with { BackstageAppearance = authored.BackstageAppearance! with { CaptionTintOpacity = 0 } };
        Assert.Equal(0, ribbon.CaptionBackground(false).A);
        ribbon.Theme = authored with { BackstageAppearance = authored.BackstageAppearance! with { CaptionTintColor = Color.Transparent } };
        Assert.Equal(0, ribbon.CaptionBackground(false).A);
        ribbon.Theme = authored with { BackstageAppearance = authored.BackstageAppearance! with { CaptionTintColor = Color.Empty, CaptionTintOpacity = null, CaptionTintEnabled = null } };
        Assert.Equal(Color.FromArgb(ribbon.HasWindowBackdrop ? (int)Math.Round(255 * .65f) : 255, ribbon.Theme.BackstageNavigationColor).ToArgb(), ribbon.CaptionBackground(false).ToArgb());
        Assert.Equal(7, ribbon.Theme.BackstageAppearance!.MarkerWidth);
        Assert.Equal(handle, field.Handle); Assert.Same(parent, field.Parent); Assert.Equal("Retained native value", field.Text);
    });

    [Fact]
    public void Tint_settings_validate_atomically_and_reset_independently() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl();
        var selected = ribbon.BuiltInTheme; var theme = ribbon.Theme;
        foreach (double invalid in new[] { -.01, 1.01, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = theme with { BackstageAppearance = new() { CaptionTintOpacity = invalid } });
            Assert.Same(theme, ribbon.Theme); Assert.Equal(selected, ribbon.BuiltInTheme);
        }
        var authored = theme with { BackstageAppearance = new() { CaptionTintEnabled = false, CaptionTintColor = Color.Crimson, CaptionTintOpacity = .4, MarkerWidth = 8 }, BackstagePagePadding = 12 };
        ribbon.Theme = authored; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = null;
        Assert.Same(authored, ribbon.Theme);
        ribbon.Theme = authored with { BackstageAppearance = authored.BackstageAppearance! with { CaptionTintEnabled = null, CaptionTintColor = Color.Empty, CaptionTintOpacity = null } };
        Assert.Equal(8, ribbon.Theme.BackstageAppearance!.MarkerWidth); Assert.Equal(12, ribbon.Theme.BackstagePagePadding); Assert.Null(other.Theme.BackstageAppearance);
    });

    [Fact]
    public void Classic_separate_caption_can_opt_in_and_custom_tint_uses_authored_or_readable_ink() => FoundationTests.Sta(() =>
    {
        using var form = Host(); using var ribbon = Create(form, RibbonBuiltInTheme.Office2019Light);
        ribbon.Theme = ribbon.Theme with { BackstageExtendsIntoTitleBar = false };
        form.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true;
        Assert.True(ribbon.BackstageCaptionTintBounds.IsEmpty);
        ribbon.Theme = ribbon.Theme with { BackstageAppearance = new() { CaptionTintEnabled = true, CaptionTintColor = Color.Navy } };
        Assert.False(ribbon.BackstageCaptionTintBounds.IsEmpty);
        var normal = form.CaptionTheme(ribbon.Theme, false);
        Assert.Equal(Color.White, form.BackstageTintCaptionTheme(ribbon, normal, false).Foreground);
        ribbon.Theme = ribbon.Theme with { TitleBarForeground = Color.Gold };
        normal = form.CaptionTheme(ribbon.Theme, false);
        Assert.Same(normal, form.BackstageTintCaptionTheme(ribbon, normal, false));
        Assert.Equal(Color.Gold, normal.Foreground);
    });

    private static IEnumerable<Color> Pixels(Bitmap image, Rectangle bounds)
    {
        for (int y = bounds.Top; y < bounds.Bottom; y++) for (int x = bounds.Left; x < bounds.Right; x++) yield return image.GetPixel(x, y);
    }
    private static RibbonForm Host() => new() { Text = "RibbonKit independent designer consumer", ShowIcon = false,
        StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false, ClientSize = new(1000, 600) };
    private static RibbonControl Create(RibbonForm form, RibbonBuiltInTheme family)
    {
        var ribbon = new RibbonControl { BuiltInTheme = family, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.None };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info" }); form.Controls.Add(ribbon); form.Ribbon = ribbon; return ribbon;
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
}
