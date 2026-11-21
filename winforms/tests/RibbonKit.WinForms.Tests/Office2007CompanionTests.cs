using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2007CompanionTests
{
    private static Color C(int rgb) => Color.FromArgb(255, rgb >> 16 & 255, rgb >> 8 & 255, rgb & 255);
    [Fact]
    public void Companion_palette_keeps_blue_primary_creases_gold_selection_white_wells_and_scoped_tip_fill()
    {
        var t = RibbonThemes.Office2007Blue; var a = t.OptionsAppearance!;
        Assert.Equal(C(0xBED3EC), a.BackgroundColor); Assert.Equal(C(0xF5F4F3), a.NavigationBackgroundColor); Assert.Equal(Color.White, a.ListBackgroundColor);
        Assert.Equal(t.ApplicationMenuAppearance!.SurroundBackground, new RibbonFill(a.BackgroundColor));
        Assert.Equal(t.ApplicationMenuAppearance.FooterSurfaceBackground, a.FooterBackground);
        Assert.Equal(t.BodyBackground, t.PopupFill); Assert.Equal(t.PopupFill, a.BackgroundFill); Assert.Equal(t.PopupFill, a.ListBackgroundFill);
        Assert.Equal(t.PopupFill, t.MenuAppearance!.OverflowBackground);
        Assert.Equal(t.Checked, a.NavigationSelectedBackground); Assert.Equal(t.Checked, a.RowSelectedBackground); Assert.Equal(t.Checked, a.TreeSelectedBackground);
        Assert.Equal(t.Hover, a.NavigationHoverBackground); Assert.Equal(t.Hover, a.ButtonHoverBackground); Assert.Equal(t.Pressed, a.ButtonPressedBackground);
        Assert.Equal(new[] { .45, .45 }, a.AccentButtonBackground!.Value.IntermediateStops!.Select(s => s.Offset));
        Assert.Equal(new[] { C(0x3585C6), C(0x1F63A6) }, a.AccentButtonBackground.Value.IntermediateStops!.Select(s => s.Color));
        Assert.Equal(C(0x7D9EC0), a.ButtonIdleStateChrome!.IdleBorderColor); Assert.Equal(C(0x17578F), a.AccentButtonStateChrome!.IdleBorderColor);
        Assert.Equal(0, a.FrameCornerRadius); Assert.Equal(0, a.RowCornerRadius); Assert.Equal(0, a.TreeCornerRadius);
        Assert.Equal(Color.White, t.EditorBackground); Assert.Equal(new RibbonFill(Color.White), t.GalleryAppearance!.StripBackground);
        Assert.Equal(C(0x9EBED9), t.MenuAppearance!.SeparatorColor); Assert.Equal(new RibbonFill(C(0xFBFDFF)), t.TipAppearance!.ScreenTipBackground);
        Assert.Equal(t.OptionsAppearance, (t with { Accent = Color.Purple }).OptionsAppearance);
        foreach (double p in new[] { 0d, .2, .45, .7, 1 })
        {
            var idle = RibbonFillColors.At(a.AccentButtonBackground.Value, p);
            Assert.True(RibbonFillColors.At(a.AccentButtonHoverBackground!.Value, p).GetBrightness() > idle.GetBrightness());
            Assert.True(RibbonFillColors.At(a.AccentButtonPressedBackground!.Value, p).GetBrightness() < idle.GetBrightness());
        }
    }

    [Fact]
    public void Idle_ink_only_replaces_idle_fallback_and_empty_reset_keeps_active_precedence()
    {
        using var image = new Bitmap(2, 2); using var g = Graphics.FromImage(image);
        var c = new RibbonPaintContext(g, new(0, 0, 2, 2), RibbonThemes.Office2007Blue, Control.DefaultFont, 96);
        var a = new RibbonStateChromeSettings { IdleBorderColor = Color.Blue, CheckedBorderColor = Color.Gold };
        Assert.Equal(Color.Blue, a.Border(c, Color.Red)); Assert.True(a.HasOverrides);
        Assert.Equal(Color.Red, a.Border(c with { Hot = true }, Color.Red)); Assert.Equal(Color.Red, a.Border(c with { Pressed = true }, Color.Red));
        Assert.Equal(Color.Gold, a.Border(c with { StateChecked = true }, Color.Red));
        Assert.Equal(Color.Gold, a.Border(c with { StateChecked = true, Hot = true }, Color.Red));
        Assert.Equal(Color.Red, (a with { IdleBorderColor = Color.Empty }).Border(c, Color.Red));
        Assert.Equal(Color.Transparent, (a with { IdleBorderColor = Color.Transparent }).Border(c, Color.Red));
        Assert.False((new RibbonStateChromeSettings()).HasOverrides);
    }

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Options_buttons_paint_distinct_resting_rims_primary_washes_and_independent_explicit_overrides(int dpi) => FoundationTests.Sta(() =>
    {
        using var painter = new RoundedPainter(); using var button = new Button { Text = "" }; using var image = new Bitmap(220, 100); using var g = Graphics.FromImage(image);
        var t = RibbonThemes.Office2007Blue; var a = t.OptionsAppearance!; var bounds = new Rectangle(10, 10, 200, 80);
        var c = new RibbonPaintContext(g, bounds, t, Control.DefaultFont, dpi);
        foreach (bool primary in new[] { false, true }) foreach (int state in new[] { 0, 1, 2, 3 })
        {
            button.Enabled = state != 3; g.Clear(Color.Magenta);
            RibbonOptionsButtonRenderer.Draw(painter, c, button, primary, state is 1 or 3, state is 2 or 3, false, false, 3, Color.White);
            var ink = state == 3 ? a.ButtonDisabledBorderColor : primary ? C(0x17578F) : state == 0 ? C(0x7D9EC0) : state == 1 ? C(0xC1A877) : C(0xB6A892);
            Assert.Equal(ink.ToArgb(), image.GetPixel(110, 10).ToArgb());
            Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(110, 9).ToArgb());
            var fill = new RibbonOptionsShellMetrics(t, dpi).ButtonFill(primary, button.Enabled, state == 1, state == 2);
            Assert.Equal(RibbonFillColors.At(fill, 35d / 79).ToArgb(), image.GetPixel(110, 45).ToArgb());
        }
        button.Enabled = true;
        RibbonOptionsButtonRenderer.Draw(painter, c with { Theme = t with { OptionsAppearance = a with { ButtonBorderColor = Color.Magenta } } }, button, true, false, false, false, false, 3, Color.White);
        Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(110, 10).ToArgb());
        var reset = a with { ButtonIdleStateChrome = null };
        RibbonOptionsButtonRenderer.Draw(painter, c with { Theme = t with { OptionsAppearance = reset } }, button, false, false, false, false, false, 3, Color.White);
        Assert.Equal(t.PopupBorder.ToArgb(), image.GetPixel(110, 10).ToArgb());
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Customization_selection_and_hover_use_separate_gold_rims_without_recoloring_commands(int dpi) => FoundationTests.Sta(() =>
    {
        using var painter = new RoundedPainter(); using var owner = new RibbonOffice2024Renderer(); using var check = new RibbonCheckBoxItem();
        using var image = new Bitmap(260, 70); using var g = Graphics.FromImage(image);
        var t = RibbonThemes.Office2007Blue with { Accent = Color.Purple }; var bounds = new Rectangle(6, 6, 248, 56); var c = new RibbonPaintContext(g, bounds, t, Control.DefaultFont, dpi);
        foreach (bool tree in new[] { false, true }) foreach (bool selected in new[] { false, true })
        {
            g.Clear(Color.White);
            if (tree) RibbonOptionsTreeRenderer.Draw(owner, painter, c with { StateHot = true }, default, "", selected, false, false, null, true, false, check, null);
            else RibbonOptionsRowRenderer.Draw(painter, c with { Hot = true }, null, "", selected);
            Assert.Equal((selected ? C(0xC19A4B) : C(0xC1A877)).ToArgb(), image.GetPixel(100, bounds.Top).ToArgb());
            Assert.NotEqual(Color.Purple.ToArgb(), image.GetPixel(100, bounds.Top).ToArgb());
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void ScreenTip_fill_and_border_are_scoped_clip_safe_and_reset_without_affecting_menu_frames(int dpi) => FoundationTests.Sta(() =>
    {
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(200, 90); using var g = Graphics.FromImage(image); var t = RibbonThemes.Office2007Blue;
            var c = new RibbonPaintContext(g, new(5, 5, 190, 80), t, Control.DefaultFont, dpi);
            g.Clear(Color.Magenta); painter.DrawPopupFrame(c); Assert.Equal(RibbonFillColors.At(t.PopupFill, 35d / 79).ToArgb(), image.GetPixel(100, 40).ToArgb());
            painter.DrawPopupFrame(c with { IsScreenTip = true }); Assert.Equal(C(0xFBFDFF).ToArgb(), image.GetPixel(100, 40).ToArgb());
            painter.DrawPopupFrame(c with { IsScreenTip = true, Theme = t with { TipAppearance = t.TipAppearance! with { ScreenTipBackground = new(Color.Lime), ScreenTipBorderColor = Color.Red } } });
            Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(100, 40).ToArgb()); Assert.Equal(Color.Red.ToArgb(), image.GetPixel(100, 5).ToArgb());
            g.Clear(Color.Magenta); var clip = new Rectangle(60, 20, 60, 40); g.SetClip(clip); painter.DrawPopupFrame(c with { IsScreenTip = true });
            Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(59, 40).ToArgb()); Assert.Equal(C(0xFBFDFF).ToArgb(), image.GetPixel(60, 40).ToArgb()); g.ResetClip();
            painter.DrawPopupFrame(c with { IsScreenTip = true, Theme = t with { TipAppearance = null } }); Assert.Equal(RibbonFillColors.At(t.PopupFill, 35d / 79).ToArgb(), image.GetPixel(100, 40).ToArgb());
            foreach (var old in new[] { RibbonThemes.Office2024Light, RibbonThemes.Office2019Light, RibbonThemes.Office2013Light, RibbonThemes.Office2010Blue })
            {
                g.Clear(Color.Magenta); painter.DrawPopupFrame(c with { Theme = old }); using var before = (Bitmap)image.Clone();
                g.Clear(Color.Magenta); painter.DrawPopupFrame(c with { Theme = old, IsScreenTip = true });
                for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) Assert.Equal(before.GetPixel(x, y), image.GetPixel(x, y));
            }
        }
    });

    [Fact]
    public void Invalid_tip_stop_alpha_is_rejected_before_theme_or_builtin_identity_changes() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2007Blue }; var before = ribbon.Theme;
        var translucent = new RibbonFill(Color.White) { IntermediateStops = new([new(.4, Color.FromArgb(128, Color.Blue))]) };
        Assert.Throws<ArgumentException>(() => ribbon.Theme = before with { TipAppearance = before.TipAppearance! with { ScreenTipBackground = translucent } });
        Assert.Same(before, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2007Blue, ribbon.BuiltInTheme);
    });

    [Fact]
    public void Actual_passive_tip_native_and_layered_frames_receive_the_scoped_role() => FoundationTests.Sta(() =>
    {
        using var painter = new TipProbe(); using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2007Blue, Renderer = painter };
        using var item = new RibbonButtonItem { Text = "Save", ScreenTipDescription = "Save the current document." };
        using var tip = new RibbonScreenTipWindow(ribbon, item, new(100, 100, 100, 40), new(110, 110)) { Location = new(-16000, -16000) };
        tip.Show(); tip.Update(); using var image = new Bitmap(tip.Width, tip.Height); tip.DrawToBitmap(image, tip.ClientRectangle);
        Assert.NotEmpty(painter.Frames); Assert.All(painter.Frames, c => Assert.True(c.IsScreenTip));
        Assert.Contains(painter.Frames, c => c.Bounds.Left > 0); // The layered frame includes shadow padding.
        Assert.DoesNotContain(painter.Frames, c => c.IsApplicationMenu);
    });

    private sealed class TipProbe : RibbonOffice2024Renderer
    {
        internal readonly List<RibbonPaintContext> Frames = [];
        public override void DrawPopupFrame(RibbonPaintContext c) { Frames.Add(c); base.DrawPopupFrame(c); }
    }
}
