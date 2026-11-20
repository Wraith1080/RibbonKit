using System.ComponentModel;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Layout;
using RibbonKit.WinForms.Samples;
using RibbonKit.WinForms.Showcase.Persistence;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2007ThemeTests
{
    private static Color C(int rgb) => Color.FromArgb(255, rgb >> 16 & 255, rgb >> 8 & 255, rgb & 255);
    private static void Stops(RibbonFill fill, params (double Offset, int Rgb)[] expected)
    {
        var stops = new[] { new RibbonGradientStop(0, fill.Start) }.Concat(fill.IntermediateStops?.AsEnumerable() ?? []).Append(new(1, fill.End)).ToArray();
        Assert.Equal(expected.Select(s => s.Offset), stops.Select(s => s.Offset));
        Assert.Equal(expected.Select(s => C(s.Rgb).ToArgb()), stops.Select(s => s.Color.ToArgb()));
        Assert.True(RibbonFillColors.IsOpaque(fill));
    }

    [Fact]
    public void Palette_uses_exact_reference_valleys_crease_stops_and_ordinary_ink()
    {
        var t = RibbonThemes.Office2007Blue;
        Stops(t.TitleBarBackgroundFill!.Value, (0, 0xE4EBF6), (.14, 0xD5E5FA), (.28, 0xCADEF7), (1, 0xE4EFFD));
        Assert.Null(t.BackgroundFill); Assert.Equal(C(0xBFDBFF), t.Background);
        Stops(t.BodyBackground!.Value, (0, 0xDBE6F4), (.17, 0xC8D9ED), (1, 0xE3F4FF));
        Assert.Equal(t.BodyBackground, t.QuickAccessBackgroundFill);
        Stops(t.HeaderSelectedBackground!.Value, (0, 0xF0F6FF), (.08, 0xF6FAFF), (.15, 0xEBF3FE), (1, 0xDBE6F5));
        Stops(t.Hover, (0, 0xFFFFF7), (.38, 0xFFE798), (.38, 0xFFD758), (.48, 0xFFD350), (1, 0xFFE89F));
        Stops(t.Pressed, (0, 0xFEB96C), (.40, 0xFDA461), (.40, 0xFC8F3D), (.63, 0xFC973D), (1, 0xFEBE63));
        Stops(t.Checked, (0, 0xFFF3D2), (.38, 0xFFDE84), (.38, 0xFFCB43), (.48, 0xFFC739), (1, 0xFFDC8A));
        Stops(t.CheckedHover!.Value, (0, 0xFFEEC0), (.38, 0xFFD670), (.38, 0xFFC12E), (.48, 0xFFBD26), (1, 0xFFD478));
        var panel = t.BodyAppearance!.GroupPanel!;
        Stops(panel.Background!.Value, (0, 0xDEE8F5), (.16, 0xC7D8ED), (1, 0xD9E8F6));
        Assert.Equal(C(0xA8BFD4), panel.BorderColor); Assert.Equal(C(0xE4ECF5), panel.InnerBorderColor);
        Assert.Equal(new RibbonFill(C(0xC1D9F1)), panel.LabelBackground); Assert.Equal(C(0x15428B), panel.LabelForeground);
        Assert.Equal(C(0x1E1E1E), t.Foreground); Assert.Equal(Color.White, t.EditorBackground);
        Assert.Equal(C(0x15428B), t.HeaderSelectedForeground); Assert.Equal(C(0x1E5A9C), t.Accent);
        Assert.Null(t.AccentStates); Assert.Null(t.AeroFrameAppearance);
    }

    [Theory] [InlineData(RibbonFileMenuMode.CommandMenu)] [InlineData(RibbonFileMenuMode.Backstage)]
    public void Family_selection_preserves_explicit_shape_file_route_material_frame_and_application_reset(RibbonFileMenuMode route) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Backdrop = RibbonWindowBackdrop.Acrylic, FrameAppearance = RibbonWindowFrameAppearance.Standard };
        using var ribbon = new RibbonControl { FileMenuMode = route, ApplicationButtonShape = RibbonApplicationButtonShape.Rectangular };
        form.Controls.Add(ribbon); form.Ribbon = ribbon;
        var application = ribbon.Theme with { Accent = Color.Purple }; ribbon.Theme = application;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2007Blue;
        Assert.Same(RibbonThemes.Office2007Blue, ribbon.Theme); Assert.Equal(route, ribbon.FileMenuMode);
        Assert.Equal(RibbonWindowBackdrop.Acrylic, form.Backdrop); Assert.Equal(RibbonWindowFrameAppearance.Standard, form.FrameAppearance);
        Assert.Equal(RibbonApplicationButtonShape.Rectangular, ribbon.EffectiveApplicationButtonShape);
        TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.ApplicationButtonShape)]!.ResetValue(ribbon);
        Assert.Equal(RibbonApplicationButtonShape.Orb, ribbon.EffectiveApplicationButtonShape);
        TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.BuiltInTheme)]!.ResetValue(ribbon); Assert.Same(application, ribbon.Theme);
        Assert.False(form.IsHandleCreated);
    });

    [Fact]
    public void Accent_and_caption_rebasing_preserve_exact_creases_and_gold_states() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm(); using var ribbon = new RibbonControl(); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        var application = ribbon.Theme; var selection = new SampleAppearance(ribbon, form);
        selection.SelectTheme(RibbonBuiltInTheme.Office2007Blue); var t = ribbon.Theme;
        foreach (var accent in new[] { Color.Purple, Color.Teal, Color.White, Color.Black })
        {
            selection.SelectAccent(accent); selection.SetColorTitleBar(true);
            Assert.Equal(t.Hover, ribbon.Theme.Hover); Assert.Equal(t.Pressed, ribbon.Theme.Pressed);
            Assert.Equal(t.Checked, ribbon.Theme.Checked); Assert.Equal(t.ApplicationButtonAppearance, ribbon.Theme.ApplicationButtonAppearance);
            var fill = t.IdentityAccent!.AdaptFill(t.IdentityAccent.FileBackground, accent);
            Assert.Equal(new[] { .45, .45 }, fill.IntermediateStops!.Select(s => s.Offset));
            Assert.Equal(t.IdentityAccent.CreateCaptionFill(accent), ribbon.Theme.TitleBarBackgroundFill);
            selection.SetColorTitleBar(false); Assert.Equal(t.TitleBarBackgroundFill, ribbon.Theme.TitleBarBackgroundFill);
        }
        selection.SelectAccent(null); Assert.Same(t, ribbon.Theme);
        selection.SelectTheme(null); Assert.Same(application, ribbon.Theme);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Both_public_painters_keep_hard_state_colors_split_companion_and_rectangular_override(int dpi) => FoundationTests.Sta(() =>
    {
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(260, 160); using var g = Graphics.FromImage(image);
            var t = RibbonThemes.Office2007Blue;
            var c = new RibbonPaintContext(g, new(10, 10, 100, 101), t, Control.DefaultFont, dpi);
            using var toggle = new RibbonToggleItem { Text = "" };
            foreach (var (hot, press, check, fill) in new[] { (true, false, false, t.Hover), (false, true, false, t.Pressed), (false, false, true, t.Checked), (true, false, true, t.CheckedHover!.Value) })
            {
                g.Clear(Color.Magenta); toggle.Checked = check;
                painter.DrawItem(c with { Hot = hot, Pressed = press }, toggle, RibbonItemSize.Small, false);
                // Away from rims/text, the actual state samples the original fill, including its hard crease.
                int inset = 1; int height = c.Bounds.Height - 2 * inset;
                foreach (int relative in new[] { 25, 40, 60 })
                    Assert.Equal(RibbonFillColors.At(fill, relative / (double)(height - 1)).ToArgb(), image.GetPixel(60, c.Bounds.Top + inset + relative).ToArgb());
            }
            using var split = new RibbonSplitButtonItem { Text = "" };
            g.Clear(Color.Magenta); painter.DrawSplitItem(c with { Hot = true }, c with { Bounds = new(110, 10, 35, 101) }, split, RibbonItemSize.Small, false);
            Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(125, 30).ToArgb());
            using var file = new RibbonDropDownItem { Text = "" };
            g.Clear(Color.Magenta); painter.DrawFileButton(c with { ApplicationButtonShape = RibbonApplicationButtonShape.Rectangular }, file);
            Assert.Equal(C(0x17578F).ToArgb(), image.GetPixel(60, 10).ToArgb());
            Assert.NotEqual(Color.Magenta.ToArgb(), image.GetPixel(10, 110).ToArgb()); // square connected foot
            painter.DrawFileButton(c with { ApplicationButtonShape = RibbonApplicationButtonShape.Rectangular, FileMenuPreviewOpen = true }, file);
            Assert.Equal(t.Accent.ToArgb(), image.GetPixel(60, 60).ToArgb());
        }
    });

    [Fact]
    public void Designer_preview_and_preferences_roundtrip_new_identity_without_running_native_content() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var application = ribbon.Theme; var selected = ribbon.SelectedTab;
        var draft = RibbonDesignCommitter.Read(ribbon, host); Assert.Contains("Office2007Blue", draft.ThemeChoices);
        draft.BuiltInTheme = "Office2007Blue"; var preview = RibbonThemePreviewRenderer.Render(ribbon, host, draft);
        Assert.NotEmpty(preview.RibbonPng); Assert.Same(application, ribbon.Theme); Assert.Same(selected, ribbon.SelectedTab); Assert.False(ribbon.IsHandleCreated);
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Same(RibbonThemes.Office2007Blue, ribbon.Theme);
        draft = RibbonDesignCommitter.Read(ribbon, host); draft.BuiltInTheme = ""; Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Same(application, ribbon.Theme);
        var preferences = new ShowcasePreferences { BuiltInTheme = RibbonBuiltInTheme.Office2007Blue, FileMenuMode = RibbonFileMenuMode.Backstage, FrameAppearance = RibbonWindowFrameAppearance.Standard, ColorTitleBar = true, AccentArgb = Color.Purple.ToArgb() };
        var restored = ShowcasePreferences.FromJson(preferences.ToJson()); Assert.Equal(preferences.BuiltInTheme, restored.BuiltInTheme);
        Assert.Equal(preferences.FileMenuMode, restored.FileMenuMode); Assert.Equal(preferences.FrameAppearance, restored.FrameAppearance);
    });

    [Fact]
    public void Modern_backstage_with_caption_Qat_and_default_orb_resolves_measurement_without_recursion() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Bounds = new(-16000, -16000, 1000, 600), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2007Blue, QuickAccessVisible = true, QuickAccessPosition = RibbonQuickAccessPosition.TitleBar, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.None };
        var home = new RibbonTab { Text = "Home" }; var group = new RibbonGroup(); var command = new RibbonButtonItem { Text = "Save" }; group.Items.Add(command); home.Groups.Add(group); ribbon.Tabs.Add(home);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = command }); ribbon.FileMenu.Visible = true;
        using var field = new TextBox { Text = "Retained" }; ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = field });
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        for (int attempt = 0; attempt < 2; attempt++)
        {
            ribbon.FileMenu.PerformClick(); Application.DoEvents(); Assert.True(ribbon.FileMenu.IsDropDownOpen);
            Assert.Equal(RibbonQuickAccessPosition.TitleBar, ribbon.EffectiveQuickAccessPosition);
            Assert.False(ribbon.ApplicationButtonBounds.IsEmpty); using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
            ribbon.FileMenu.CloseDropDown(); Application.DoEvents(); Assert.Equal("Retained", field.Text);
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Preset_frames_connect_selected_tab_and_clip_measured_group_labels_without_double_edges(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(240), S(190)); using var g = Graphics.FromImage(image); g.Clear(Color.Magenta);
            var t = RibbonThemes.Office2007Blue; var tabBounds = new Rectangle(S(40), S(4), S(80), S(30));
            var bodyBounds = new Rectangle(S(4), tabBounds.Bottom, S(230), S(150));
            var connection = new Rectangle(tabBounds.Left + S(1), bodyBounds.Top, tabBounds.Width - S(2), S(1));
            var c = new RibbonPaintContext(g, bodyBounds, t, Control.DefaultFont, dpi) { HeaderBodyConnectionBounds = connection };
            using var tab = new RibbonTab { Text = "" }; painter.DrawBody(c); painter.DrawTab(c with { Bounds = tabBounds }, tab, true);
            Assert.Equal(t.BodyBorder.ToArgb(), image.GetPixel(bodyBounds.Left + S(10), bodyBounds.Top).ToArgb());
            Assert.Equal(t.BodyBackground!.Value.Start.ToArgb(), image.GetPixel(tabBounds.Left + S(30), bodyBounds.Top).ToArgb());
            Assert.Equal(t.HeaderSelectedBackground!.Value.End.ToArgb(), image.GetPixel(tabBounds.Left + S(30), tabBounds.Bottom - 1).ToArgb());
            var frame = new Rectangle(S(12), S(45), S(150), S(120));
            c = c with { Bounds = frame, GroupFrameBounds = frame, GroupCaptionBounds = RibbonGroupPanelMetrics.CaptionBounds(frame, Control.DefaultFont, dpi) };
            using var group = new RibbonGroup(); var metrics = RibbonGroupPanelMetrics.Resolve(c, false);
            g.Clear(Color.Magenta); painter.DrawGroupPanel(c, group, false); int middle = metrics.Panel.Left + metrics.Panel.Width / 2;
            Assert.Equal(C(0xA8BFD4).ToArgb(), image.GetPixel(middle, metrics.Panel.Top).ToArgb());
            Assert.Equal(C(0xE4ECF5).ToArgb(), image.GetPixel(middle, metrics.Panel.Top + S(1)).ToArgb());
            Assert.Equal(C(0xC1D9F1).ToArgb(), image.GetPixel(middle, metrics.Caption.Top + S(3)).ToArgb());
            Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(frame.Left, frame.Top).ToArgb());
            using var full = (Bitmap)image.Clone(); g.Clear(Color.Magenta); var clip = new Rectangle(middle, metrics.Caption.Top, S(20), S(6)); g.SetClip(clip);
            painter.DrawGroupPanel(c, group, false);
            for (int y = clip.Top - 1; y <= clip.Bottom; y++) for (int x = clip.Left - 1; x <= clip.Right; x++)
                Assert.Equal((clip.Contains(x, y) ? full.GetPixel(x, y) : Color.Magenta).ToArgb(), image.GetPixel(x, y).ToArgb());
        }
    });
}
