using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageStyleIsolationTests
{
    [Theory]
    [InlineData(RibbonWindowBackdrop.None, 1f)]
    [InlineData(RibbonWindowBackdrop.Mica, .65f)]
    [InlineData(RibbonWindowBackdrop.Acrylic, 0f)]
    public void Modern_neutral_caption_tint_tracks_slide_fade_and_restores_on_close(RibbonWindowBackdrop backdrop, float opacity) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { ClientSize = new(900, 560), Backdrop = backdrop };
        using var ribbon = Create(form); form.Show(); Application.DoEvents();
        ribbon.Backstage.NavigationBackgroundOpacity = opacity; ribbon.Motion.SystemPolicyOverride = true;
        var theme = ribbon.Theme;
        Assert.True(ribbon.BackstageCaptionTintBounds.IsEmpty);
        foreach (bool closing in new[] { false, true })
        {
            ribbon.IsBackstageOpen = !closing;
            var motion = Assert.IsType<RibbonBackstageMotion>(ribbon.BackstageOpeningMotion);
            foreach (double progress in new[] { .2, .6, 1 })
            {
                motion.SetProgress(progress);
                Assert.Equal(form.CaptionHeight, ribbon.BackstageSurface!.Top);
                Assert.False(ribbon.HasBackstageCaption); Assert.Null(form.CaptionTitleMotion);
                var tint = ribbon.CaptionBackground(ribbon.HasWindowBackdrop);
                Assert.Equal(ribbon.HasWindowBackdrop ? (int)Math.Round(motion.Opacity * opacity) : motion.Opacity, tint.A);
                Assert.Equal(theme.BackstageNavigationBackground.ToArgb() & 0xFFFFFF, tint.ToArgb() & 0xFFFFFF);
                Assert.Equal(motion.VisibleWidth, ribbon.BackstageCaptionTintBounds.Width);
                using var frame = new Bitmap(form.ClientSize.Width, form.CaptionHeight);
                using var graphics = Graphics.FromImage(frame);
                ribbon.PaintCaptionBackground(graphics, new(Point.Empty, frame.Size), ribbon.HasWindowBackdrop);
                using var expected = new Bitmap(1, 1); using var canvas = Graphics.FromImage(expected);
                canvas.Clear(form.CaptionColor(theme, ribbon.HasWindowBackdrop)); using var fill = new SolidBrush(tint);
                canvas.FillRectangle(fill, 0, 0, 1, 1);
                Assert.Equal(expected.GetPixel(0, 0), frame.GetPixel(100, 2));
                if (motion.VisibleWidth < frame.Width)
                {
                    var normal = form.CaptionColor(theme, ribbon.HasWindowBackdrop);
                    Assert.Equal(normal.A == 0 ? 0 : normal.ToArgb(), frame.GetPixel(motion.VisibleWidth, 2).ToArgb());
                }
            }
            ribbon.SettleMotion();
            if (!closing)
            {
                var tint = ribbon.CaptionBackground(ribbon.HasWindowBackdrop);
                Assert.Equal(ribbon.HasWindowBackdrop ? (int)Math.Round(255 * opacity) : 255, tint.A);
                Assert.Equal(form.ClientSize.Width, ribbon.BackstageCaptionTintBounds.Width);
            }
        }
        Assert.True(ribbon.BackstageCaptionTintBounds.IsEmpty);
        Assert.Equal(form.CaptionColor(theme, ribbon.HasWindowBackdrop), ribbon.CaptionBackground(ribbon.HasWindowBackdrop));
        Assert.Same(theme, ribbon.Theme); Assert.True(form.TitleBarColor.IsEmpty);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Authored_form_or_theme_caption_color_takes_precedence_over_neutral_tint(bool themeColor) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { ClientSize = new(900, 560) };
        using var ribbon = Create(form); ribbon.AnimationLevel = RibbonAnimationLevel.None;
        form.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true;
        Assert.Equal(ribbon.Theme.BackstageNavigationBackground, ribbon.CaptionBackground(false));
        if (themeColor) ribbon.Theme = ribbon.Theme with { TitleBarBackground = Color.Navy };
        else form.TitleBarColor = Color.Navy;
        Assert.True(ribbon.BackstageCaptionTintBounds.IsEmpty); Assert.Equal(Color.Navy, ribbon.CaptionBackground(false));
        if (themeColor) ribbon.Theme = ribbon.Theme with { TitleBarBackground = Color.Empty };
        else form.TitleBarColor = Color.Empty;
        Assert.Equal(ribbon.Theme.BackstageNavigationBackground, ribbon.CaptionBackground(false));
        ribbon.Theme = ribbon.Theme with { BackstageNavigationBackground = Color.Silver };
        Assert.Equal(Color.Silver.ToArgb(), ribbon.CaptionBackground(false).ToArgb());
        ribbon.IsBackstageOpen = false; Assert.Equal(ribbon.Theme.Background, ribbon.CaptionBackground(false));
    });

    [Fact]
    public void Families_share_pure_bases_and_keep_style_defaults_and_paint_caches_isolated() => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonOffice2024Renderer(); using var ribbon = new RibbonControl();
        var modern = painter.BackstageStyleRenderer(RibbonThemes.Office2024Light);
        var classic = painter.BackstageStyleRenderer(RibbonThemes.Office2019Light);
        Assert.IsType<RibbonBackstage2024Renderer>(modern); Assert.IsType<RibbonBackstage2019Renderer>(classic);
        Assert.Equal(modern.GetType().BaseType, classic.GetType().BaseType);
        Assert.Equal(typeof(RibbonBackstageRenderer), modern.GetType().BaseType);
        Assert.True(typeof(RibbonBackstageRenderer).IsAbstract);
        Assert.Equal(typeof(object), typeof(RibbonBackstageRenderer).BaseType);
        Assert.Equal(RibbonBackstageLayoutProvider.For(RibbonThemes.Office2024Light).GetType().BaseType,
            RibbonBackstageLayoutProvider.For(RibbonThemes.Office2019Light).GetType().BaseType);
        var selected = ribbon.Theme;
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = selected with { BackstageStyle = (RibbonBackstageStyle)27 });
        Assert.Same(selected, ribbon.Theme);
        var classicDefaults = new RibbonBackstageMetrics(RibbonThemes.Office2019Light with
        { BackstageNavigationPadding = null, BackstageNavigationIconInset = null, BackstageAutoMinimumNavigationWidth = null, BackstageFooterBottomPadding = null }, RibbonDensity.Compact);
        Assert.Equal(0, classicDefaults.NavigationPadding); Assert.Equal(26, classicDefaults.IconInset);
        Assert.Equal(220, classicDefaults.AutoMinimumWidth); Assert.Equal(14, classicDefaults.FooterBottomPadding);
        using var before = Paint(RibbonThemes.Office2024Light);
        using var changedClassic = Paint(RibbonThemes.Office2019Light with { Accent = Color.Magenta, BackstagePageBorderWidth = 3 });
        using var after = Paint(RibbonThemes.Office2024Light);
        for (int y = 0; y < before.Height; y++) for (int x = 0; x < before.Width; x++) Assert.Equal(before.GetPixel(x, y), after.GetPixel(x, y));

        Bitmap Paint(RibbonTheme theme)
        {
            var image = new Bitmap(400, 180); using var graphics = Graphics.FromImage(image);
            painter.DrawBackstageBackground(new(graphics, new(0, 0, 400, 180), theme, Control.DefaultFont, 96), 160);
            using var item = new RibbonButtonItem { Text = "Info" };
            painter.DrawBackstageEntry(new(graphics, new(8, 90, 144, 42), theme, Control.DefaultFont, 96), item, true);
            return image;
        }
    });

    [Fact]
    public void Family_state_rendering_retains_application_renderer_text_and_focus_hooks() => FoundationTests.Sta(() =>
    {
        using var painter = new HookRenderer(); using var item = new RibbonButtonItem { Text = "Info" };
        using var image = new Bitmap(300, 50); using var graphics = Graphics.FromImage(image);
        foreach (var theme in new[] { RibbonThemes.Office2019Light, RibbonThemes.Office2024Light })
            painter.DrawBackstageEntry(new(graphics, new(0, 0, 300, 50), theme, Control.DefaultFont, 96) { Hot = true, Focused = true }, item, true);
        Assert.Equal(2, painter.TextCalls); Assert.Equal(2, painter.FocusCalls);
    });

    private sealed class HookRenderer : RibbonOffice2024Renderer
    {
        internal int TextCalls, FocusCalls;
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { TextCalls++; base.DrawText(context, text, color, flags); }
        public override void DrawFocus(RibbonPaintContext context) { FocusCalls++; base.DrawFocus(context); }
    }
    private static RibbonControl Create(RibbonForm form)
    {
        var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info" });
        form.Controls.Add(ribbon); form.Ribbon = ribbon; return ribbon;
    }
}
