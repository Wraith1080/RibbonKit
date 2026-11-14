using System.ComponentModel;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Samples;
using RibbonKit.WinForms.Showcase.Persistence;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2010ThemeTests
{
    [Fact]
    public void Builtin_shell_keeps_stable_identities_application_reset_and_independent_window_policy() => FoundationTests.Sta(() =>
    {
        Assert.Equal(new[] { 0, 1, 2, 3 }, Enum.GetValues<RibbonBuiltInTheme>().Select(v => (int)v));
        var theme = RibbonThemes.Office2010Blue;
        Assert.Equal(RibbonHeaderPresentation.Filled, theme.HeaderPresentation); Assert.True(theme.HeaderAppearance!.ConnectSelectedTabToBody);
        Assert.Equal(RibbonWindowCornerPreference.Rounded, theme.WindowCornerPreference); Assert.Equal(3, theme.CornerRadius);
        Assert.Equal(Padding.Empty, theme.BodyMargin); Assert.Equal(0, theme.BodyCornerRadius); Assert.False(theme.ExtendTitleBarColorToHeader);
        Assert.Equal(theme.TitleBarBackgroundFill!.Value.End, theme.BackgroundFill!.Value.Start);
        Assert.Equal(theme.HeaderSelectedBackground!.Value.End, theme.BodyBackground!.Value.Start);
        using var form = new RibbonForm(); using var ribbon = new RibbonControl(); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        var application = ribbon.Theme with { Accent = Color.Purple }; ribbon.Theme = application;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2010Blue; Assert.Same(theme, ribbon.Theme);
        Assert.Equal(RibbonWindowCornerPreference.Rounded, form.EffectiveWindowCornerPreference);
        form.WindowCornerPreference = RibbonWindowCornerPreference.Square; Assert.Equal(RibbonWindowCornerPreference.Square, form.EffectiveWindowCornerPreference);
        form.WindowCornerPreference = null; Assert.Equal(RibbonWindowCornerPreference.Rounded, form.EffectiveWindowCornerPreference);
        TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.BuiltInTheme)]!.ResetValue(ribbon); Assert.Same(application, ribbon.Theme);
        Assert.False(form.IsHandleCreated);
    });

    [Fact]
    public void Identity_recipe_preserves_reference_depth_alpha_direction_and_explicit_role_ownership()
    {
        var theme = RibbonThemes.Office2010Blue; var recipe = theme.IdentityAccent!;
        Assert.Equal(recipe.FileBackground, recipe.AdaptFill(recipe.FileBackground, theme.Accent));
        var fill = recipe.FileBackground with { Direction = RibbonGradientDirection.Horizontal, End = Color.FromArgb(20, Color.White) };
        var changed = recipe.AdaptFill(fill, Color.Purple);
        Assert.Equal(fill.Direction, changed.Direction); Assert.Equal(fill.End.A, changed.End.A);
        Assert.Equal(fill.IntermediateStops![0].Offset, changed.IntermediateStops![0].Offset);
        Assert.NotEqual(changed.Start, changed.IntermediateStops[0].Color); Assert.NotEqual(changed.End, changed.IntermediateStops[0].Color);
        Assert.Equal(fill, recipe.AdaptFill(fill, Color.Purple, 0));
        Assert.Equal(theme.Hover, (theme with { Accent = Color.Green }).Hover);
        Assert.Equal(theme.Checked, (theme with { Accent = Color.Green }).Checked);
        Assert.Equal(recipe.CaptionBackground, recipe.CreateCaptionFill(theme.Accent));
        Assert.NotEqual(recipe.CaptionBackground, recipe.CreateCaptionFill(Color.Green));
        Assert.Equal(recipe.CaptionBackground, (recipe with { CaptionStrength = 0 }).CreateCaptionFill(Color.Green));
        foreach (var accent in new[] { Color.Black, Color.White, Color.Red, Color.Teal })
            Assert.True(RibbonFillColors.IsOpaque(recipe.CreateCaptionFill(accent)));
    }

    [Theory] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)] [InlineData(-.1)] [InlineData(1.1)]
    public void Invalid_identity_strengths_and_caption_alpha_fail_atomically(double invalid) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue }; var before = ribbon.Theme;
        foreach (var recipe in new[] { before.IdentityAccent! with { FileStrength = invalid }, before.IdentityAccent! with { CaptionStrength = invalid } })
        { Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = before with { IdentityAccent = recipe }); Assert.Same(before, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2010Blue, ribbon.BuiltInTheme); }
        var alpha = new RibbonFill(Color.White) { IntermediateStops = new(new[] { new RibbonGradientStop(.5, Color.FromArgb(10, Color.Blue)) }) };
        Assert.Throws<ArgumentException>(() => ribbon.Theme = before with { IdentityAccent = before.IdentityAccent! with { CaptionBackground = alpha } }); Assert.Same(before, ribbon.Theme);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Actual_hooks_paint_gold_states_blue_file_and_independent_tab_outlines(int dpi) => FoundationTests.Sta(() =>
    {
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(180, 100); using var graphics = Graphics.FromImage(image);
            var theme = RibbonThemes.Office2010Blue;
            var c = new RibbonPaintContext(graphics, new(10, 10, 100, 61), theme, Control.DefaultFont, dpi);
            using var toggle = new RibbonToggleItem { Text = "" };
            foreach (var (hot, pressed, check) in new[] { (true, false, false), (false, true, false), (false, false, true), (true, false, true) })
            {
                toggle.Checked = check; graphics.Clear(Color.White);
                renderer.DrawItem(c with { Hot = hot, Pressed = pressed }, toggle, RibbonItemSize.Small, false);
                var pixel = image.GetPixel(60, 40); Assert.True(pixel.R > 230 && pixel.G > 180 && pixel.B < 180, "Gold state remains dimensional and independent of blue Accent.");
            }
            using var file = new RibbonDropDownItem { Text = "" };
            var colors = new HashSet<int>();
            foreach (var state in new[] { c, c with { Hot = true }, c with { Pressed = true } })
            {
                graphics.Clear(Color.Magenta); renderer.DrawFileButton(state, file);
                colors.Add(image.GetPixel(60, 40).ToArgb());
                Assert.NotEqual(image.GetPixel(60, 20), image.GetPixel(60, 50));
                Assert.NotEqual(Color.Magenta.ToArgb(), image.GetPixel(10, 70).ToArgb()); // Square joined foot.
            }
            Assert.Equal(3, colors.Count);
            using var split = new RibbonSplitButtonItem { Text = "" };
            graphics.Clear(Color.White); renderer.DrawSplitItem(c with { Hot = true }, c with { Bounds = new(110, 10, 25, 61) }, split, RibbonItemSize.Small, false);
            Assert.True(image.GetPixel(60, 40).R > 230 && image.GetPixel(60, 40).B < 180);
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(120, 20).ToArgb()); // Away from the dropdown glyph.
            var custom = theme with { Accent = Color.Purple, FileButtonAppearance = theme.FileButtonAppearance! with { Background = new(Color.Lime), StateChrome = null, BorderColor = Color.Transparent } };
            graphics.Clear(Color.White); renderer.DrawFileButton(c with { Theme = custom }, file); Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(60, 40).ToArgb());
            custom = custom with { FileButtonAppearance = custom.FileButtonAppearance! with { Background = null } };
            renderer.DrawFileButton(c with { Theme = custom }, file); Assert.NotEqual(Color.Lime.ToArgb(), image.GetPixel(60, 40).ToArgb());
        }
        using var painter = new RibbonOffice2024Renderer(); using var bitmap = new Bitmap(160, 100); using var g = Graphics.FromImage(bitmap);
        using var tab = new RibbonTab { Text = "" }; var context = new RibbonPaintContext(g, new(10, 10, 100, 61), RibbonThemes.Office2010Blue, Control.DefaultFont, dpi);
        painter.DrawTab(context with { Hot = true }, tab, false); Assert.Equal(Color.FromArgb(211, 158, 40).ToArgb(), bitmap.GetPixel(60, 10).ToArgb());
        painter.DrawTab(context with { Pressed = true }, tab, false); Assert.Equal(Color.FromArgb(203, 154, 52).ToArgb(), bitmap.GetPixel(60, 10).ToArgb());
        painter.DrawTab(context, tab, true); Assert.Equal(context.Theme.BodyBorder.ToArgb(), bitmap.GetPixel(60, 10).ToArgb());
        painter.DrawTab(context with { Hot = true, Theme = context.Theme with { HeaderAppearance = context.Theme.HeaderAppearance! with { FilledBorderColor = Color.Red } } }, tab, false);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(60, 10).ToArgb());
    });

    [Theory] [InlineData(RibbonFileMenuMode.CommandMenu)] [InlineData(RibbonFileMenuMode.Backstage)]
    public void File_routes_open_with_their_own_ramp_and_restore_idle_without_losing_native_content(RibbonFileMenuMode mode) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Bounds = new(-16000, -16000, 1000, 600), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue, FileMenuMode = mode, AnimationLevel = RibbonAnimationLevel.None };
        ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); ribbon.FileMenu.Visible = true; ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Open" });
        using var field = new TextBox { Text = "Retained" }; ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = field });
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        if (mode == RibbonFileMenuMode.Backstage) { ribbon.FileMenu.PerformClick(); ribbon.FileMenu.CloseDropDown(); }
        var handle = field.Handle; var parent = field.Parent;
        using var renderer = new RibbonOffice2024Renderer(); using var image = new Bitmap(120, 90); using var g = Graphics.FromImage(image);
        var c = new RibbonPaintContext(g, new(10, 10, 100, 61), ribbon.Theme, Control.DefaultFont, 96);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            ribbon.FileMenu.PerformClick(); Assert.True(ribbon.FileMenu.IsDropDownOpen);
            renderer.DrawFileButton(c, ribbon.FileMenu); Assert.Equal(ribbon.Theme.IdentityAccent!.FileOpenBackground.IntermediateStops![0].Color.ToArgb(), image.GetPixel(25, 40).ToArgb());
            renderer.DrawFileButton(c with { FileMenuClosing = true }, ribbon.FileMenu); Assert.Equal(ribbon.Theme.IdentityAccent.FileBackground.IntermediateStops![0].Color.ToArgb(), image.GetPixel(25, 40).ToArgb());
            ribbon.FileMenu.CloseDropDown(); Assert.Equal(handle, field.Handle); Assert.Same(parent, field.Parent); Assert.Equal("Retained", field.Text);
        }
    });

    [Fact]
    public void Header_and_file_outline_hiding_reset_and_explicit_precedence_remain_independent() => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonOffice2024Renderer(); using var image = new Bitmap(120, 80); using var g = Graphics.FromImage(image);
        using var tab = new RibbonTab { Text = "" }; using var file = new RibbonDropDownItem { Text = "" };
        var theme = RibbonThemes.Office2010Blue with { HeaderHover = new(Color.Transparent), HeaderFilledCornerRadius = 0 };
        var c = new RibbonPaintContext(g, new(10, 10, 100, 61), theme, Control.DefaultFont, 96, Hot: true);
        foreach (var a in new[] { theme.HeaderAppearance! with { FilledHoverBorderColor = Color.Transparent }, theme.HeaderAppearance! with { FilledHoverBorderColor = Color.Red, FilledBorderWidth = 0 } })
        { g.Clear(Color.White); painter.DrawTab(c with { Theme = theme with { HeaderAppearance = a } }, tab, false); Assert.Equal(Color.White.ToArgb(), image.GetPixel(60, 10).ToArgb()); }
        var fixedInk = theme with { FileButtonAppearance = theme.FileButtonAppearance! with { BorderColor = Color.Lime, StateChrome = null } };
        painter.DrawFileButton(c with { Theme = fixedInk, Hot = false }, file); Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(60, 10).ToArgb());
        fixedInk = fixedInk with { HeaderAppearance = fixedInk.HeaderAppearance! with { FilledBorderColor = Color.Red } };
        painter.DrawFileButton(c with { Theme = fixedInk, Hot = false }, file); Assert.Equal(Color.Red.ToArgb(), image.GetPixel(60, 10).ToArgb());
        var reset = theme with { Accent = Color.Purple, FileButtonAppearance = theme.FileButtonAppearance! with { StateChrome = null } };
        painter.DrawFileButton(c with { Theme = reset, Hot = false }, file);
        Assert.Equal(reset.IdentityAccent!.AdaptFill(new(reset.IdentityAccent.FileBorderColor), reset.Accent).Start.ToArgb(), image.GetPixel(60, 10).ToArgb());
    });

    [Fact]
    public void Sample_title_toggle_uses_shared_gradient_recipe_and_resets_accent_caption_and_application_independently() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm(); using var ribbon = new RibbonControl(); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        var application = ribbon.Theme; var selection = new SampleAppearance(ribbon, form);
        selection.SelectTheme(RibbonBuiltInTheme.Office2010Blue); var neutral = ribbon.Theme.TitleBarBackgroundFill;
        selection.SelectAccent(Color.Purple); selection.SetColorTitleBar(true);
        Assert.Equal(RibbonBuiltInTheme.Office2010Blue, selection.SelectedTheme); Assert.True(form.TitleBarColor.IsEmpty);
        Assert.Equal(Color.White, form.CaptionTheme(ribbon.Theme, false).Foreground);
        Assert.Equal(ribbon.Theme.IdentityAccent!.CreateCaptionFill(Color.Purple), ribbon.Theme.TitleBarBackgroundFill);
        selection.SelectAccent(Color.Teal); Assert.Equal(ribbon.Theme.IdentityAccent.CreateCaptionFill(Color.Teal), ribbon.Theme.TitleBarBackgroundFill);
        selection.SetColorTitleBar(false); Assert.Equal(neutral, ribbon.Theme.TitleBarBackgroundFill); Assert.Equal(Color.Teal, ribbon.Theme.Accent);
        selection.SelectAccent(null); Assert.Same(RibbonThemes.Office2010Blue, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2010Blue, ribbon.BuiltInTheme);
        selection.SelectTheme(null); Assert.Same(application, ribbon.Theme); Assert.False(form.IsHandleCreated);
    });

    [Fact]
    public void Designer_choice_and_inert_candidate_roundtrip_without_mutating_authored_theme_or_handles() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var application = ribbon.Theme; var selected = ribbon.SelectedTab;
        var draft = RibbonDesignCommitter.Read(ribbon, host); Assert.Contains("Office2010Blue", draft.ThemeChoices);
        draft.BuiltInTheme = "Office2010Blue"; var preview = RibbonThemePreviewRenderer.Render(ribbon, host, draft);
        Assert.NotEmpty(preview.RibbonPng); Assert.Same(application, ribbon.Theme); Assert.Same(selected, ribbon.SelectedTab); Assert.False(ribbon.IsHandleCreated);
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Same(RibbonThemes.Office2010Blue, ribbon.Theme);
        draft = RibbonDesignCommitter.Read(ribbon, host); draft.BuiltInTheme = ""; Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Same(application, ribbon.Theme);
        var preferences = new ShowcasePreferences { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue, ColorTitleBar = true, AccentArgb = Color.Purple.ToArgb() };
        Assert.Equal(preferences.BuiltInTheme, ShowcasePreferences.FromJson(preferences.ToJson()).BuiltInTheme);
    });
}
