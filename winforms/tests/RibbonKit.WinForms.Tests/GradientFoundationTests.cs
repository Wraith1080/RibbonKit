using System.Drawing.Drawing2D;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GradientFoundationTests
{
    private static RibbonFill Ramp => new(Color.Red, Color.Red) { IntermediateStops = new(new[] { new RibbonGradientStop(.5, Color.Lime) }) };
    [Fact]
    public void Empty_state_groups_do_not_enable_chrome_or_switch_native_button_paint()
    {
        var empty = new RibbonStateChromeSettings { InnerBorderWidth = 3, InnerBorderInset = 10 };
        Assert.False(new RibbonCommandAppearanceSettings { StateChrome = empty }.HasChromeOverrides);
        Assert.False(new RibbonGalleryAppearanceSettings { StateChrome = empty }.HasTileChrome);
        Assert.False(new RibbonEditorAppearanceSettings { StateChrome = empty }.HasChoiceChrome);
        Assert.False(new RibbonOptionsAppearanceSettings { StateChrome = empty }.HasButtonOverrides(false));
    }

    [Fact]
    public void Stops_are_immutable_structurally_equal_and_keep_legacy_endpoint_edits()
    {
        var source = new[] { new RibbonGradientStop(.25, Color.Blue), new RibbonGradientStop(.75, Color.Lime) };
        var stops = new RibbonGradientStops(source); var equal = new RibbonGradientStops(source.ToList());
        source[0] = new(.1, Color.Black);
        Assert.Equal(.25, stops[0].Offset); Assert.Equal(stops, equal); Assert.Equal(stops.GetHashCode(), equal.GetHashCode());
        var fill = new RibbonFill(Color.Red, Color.White) { IntermediateStops = stops };
        Assert.Equal(fill, fill with { IntermediateStops = equal });
        Assert.Equal(RibbonGradientDirection.Vertical, (fill with { Direction = RibbonGradientDirection.Vertical }).Direction);
        Assert.Equal(new RibbonFill(Color.Red), new RibbonFill(Color.Red) { IntermediateStops = new(Array.Empty<RibbonGradientStop>()) });
        var changed = fill with { Start = Color.Yellow, End = Color.Black };
        Assert.Equal(Color.Yellow.ToArgb(), RibbonFillColors.At(changed, 0).ToArgb()); Assert.Equal(Color.Black.ToArgb(), RibbonFillColors.At(changed, 1).ToArgb());
        var (start, end) = changed; Assert.Equal(Color.Yellow, start); Assert.Equal(Color.Black, end);
        Assert.Single(new HashSet<RibbonFill> { fill, fill with { IntermediateStops = equal } });
    }

    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(-.1)] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)]
    public void Invalid_stop_positions_are_rejected(double offset) => Assert.Throws<ArgumentOutOfRangeException>(() => new RibbonGradientStops(new[] { new RibbonGradientStop(offset, Color.Red) }));

    [Fact]
    public void Unsorted_duplicate_stops_and_invalid_directions_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new RibbonGradientStops(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RibbonGradientStops(new[] { new RibbonGradientStop(.5, Color.Red), new RibbonGradientStop(.5, Color.Blue) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RibbonGradientStops(new[] { new RibbonGradientStop(.6, Color.Red), new RibbonGradientStop(.3, Color.Blue) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RibbonFill(Color.Red) { Direction = (RibbonGradientDirection)99 });
    }

    [Fact]
    public void Opaque_surface_and_nested_chrome_validation_is_atomic() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2013Light };
        var before = ribbon.Theme; var transparentMiddle = Ramp with { IntermediateStops = new(new[] { new RibbonGradientStop(.5, Color.FromArgb(20, Color.Lime)) }) };
        foreach (var candidate in new[] { before with { BodyBackground = transparentMiddle }, before with { BackgroundFill = transparentMiddle }, before with { PopupBackgroundFill = transparentMiddle }, before with { QuickAccessBackgroundFill = transparentMiddle }, before with { TitleBarBackgroundFill = transparentMiddle }, before with { BackstageNavigationBackgroundFill = transparentMiddle } })
        {
            Assert.Throws<ArgumentException>(() => ribbon.Theme = candidate);
            Assert.Same(before, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2013Light, ribbon.BuiltInTheme);
        }
        foreach (var candidate in new[] { before with { CommandAppearance = new() { StateChrome = new() { InnerBorderWidth = 33 } } }, before with { GalleryAppearance = new() { StateChrome = new() { InnerBorderInset = -1 } } }, before with { OptionsAppearance = new() { StateChrome = new() { InnerBorderWidth = -1 } } } })
        { Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = candidate); Assert.Same(before, ribbon.Theme); }
    });

    [Fact]
    public void Color_setters_and_form_override_own_their_roles_and_reset_independently() => FoundationTests.Sta(() =>
    {
        var theme = RibbonThemes.Office2019Light with { BackgroundFill = Ramp, PopupBackgroundFill = Ramp, QuickAccessBackgroundFill = Ramp, TitleBarBackgroundFill = Ramp, BackstageNavigationBackgroundFill = Ramp };
        var color = theme with { Background = Color.Black, PopupBackground = Color.Black, TitleBarBackground = Color.Black, BackstageNavigationBackground = Color.Black };
        Assert.Null(color.BackgroundFill); Assert.Null(color.QuickAccessBackgroundFill); Assert.Null(color.PopupBackgroundFill); Assert.Null(color.TitleBarBackgroundFill); Assert.Null(color.BackstageNavigationBackgroundFill);
        Assert.Equal(Ramp, (color with { BackgroundFill = Ramp }).BackgroundFill);
        using var form = new RibbonForm(); using var ribbon = new RibbonControl { Theme = theme }; form.Controls.Add(ribbon); form.Ribbon = ribbon;
        Assert.Equal(Ramp, form.CaptionFill(theme, false)); Assert.Equal(153, form.CaptionFill(theme, true)!.Value.IntermediateStops![0].Color.A);
        form.TitleBarColor = Color.Purple; Assert.Null(form.CaptionFill(theme, false)); Assert.Equal(Color.Purple, form.CaptionColor(theme, false));
        form.TitleBarColor = Color.Empty; Assert.Equal(Ramp, form.CaptionFill(theme, false));
        Assert.Equal(Ramp, (theme with { PopupBackgroundFill = null }).BackgroundFill);
    });

    [Fact]
    public void Alpha_and_darkening_map_every_stop_and_keep_direction()
    {
        var fill = Ramp with { Direction = RibbonGradientDirection.Horizontal };
        var theme = RibbonThemes.Office2024Light with { BackdropHover = fill, QuickAccessBackdropOpacity = .5f, OptionsAppearance = new() { AccentButtonBackground = fill, AccentButtonHoverDarkening = .1 } };
        using var image = new Bitmap(10, 10); using var g = Graphics.FromImage(image);
        var context = new RibbonPaintContext(g, new(0, 0, 10, 10), theme, SystemFonts.MessageBoxFont!, 96, Hot: true) { CompositedText = true };
        var alpha = RibbonRenderer.QuickAccessFill(context, fill, false);
        Assert.Equal(128, alpha.IntermediateStops![0].Color.A); Assert.Equal(fill.Direction, alpha.Direction); Assert.Equal(.5, alpha.IntermediateStops[0].Offset);
        var dark = new Layout.RibbonOptionsShellMetrics(theme, 96).ButtonFill(true, true, true, false);
        Assert.Equal(fill.Direction, dark.Direction); Assert.Equal(230, dark.IntermediateStops![0].Color.G);
    }

    [Theory] [InlineData(false, 96)] [InlineData(true, 96)] [InlineData(false, 120)] [InlineData(true, 144)] [InlineData(true, 192)]
    public void Actual_paint_paths_keep_middle_stops_and_background_coordinates(bool office, int dpi) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        var theme = RibbonThemes.Office2013Light with { BackgroundFill = Ramp, PopupBackgroundFill = Ramp, QuickAccessBackgroundFill = Ramp, BackstageNavigationBackgroundFill = Ramp, Hover = Ramp, Checked = Ramp, CheckedHover = Ramp, CommandAppearance = new() { CornerRadius = 0, Inset = 0, BorderWidth = 0 }, GalleryAppearance = new() { TileCornerRadius = 0, TileInset = 0, TileBorderWidth = 0 }, FileButtonAppearance = new() { Background = Ramp, HoverBackground = Ramp } };
        using var image = new Bitmap(65, 65); using var g = Graphics.FromImage(image); var c = new RibbonPaintContext(g, new(0, 0, 65, 65), theme, SystemFonts.MessageBoxFont!, dpi, Hot: true);
        foreach (Action paint in new Action[] { () => renderer.DrawSurface(c, false), () => renderer.DrawPopupFrame(c), () => renderer.DrawItem(c, new RibbonButtonItem(), RibbonItemSize.Large, false), () => renderer.DrawGalleryTile(c, new RibbonGalleryTile(), true), () => renderer.DrawFileButton(c, new RibbonDropDownItem()), () => renderer.DrawQuickAccessToolbar(c with { Theme = theme with { BodyBackground = new(Color.White) } }, RibbonQuickAccessPosition.BelowRibbon), () => renderer.DrawBackstageBackground(c, 65) })
        { g.Clear(Color.White); paint(); Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(32, 32).ToArgb()); }
        g.Clear(Color.White); renderer.DrawSurface(c with { Bounds = new(0, 16, 65, 16), BackgroundBounds = new(0, 0, 65, 65) }, false);
        Assert.Equal(RibbonFillColors.At(Ramp, 24d / 64).ToArgb(), image.GetPixel(32, 24).ToArgb()); Assert.Equal(Color.White.ToArgb(), image.GetPixel(32, 15).ToArgb());
        g.Clear(Color.White); renderer.DrawSurface(c with { Theme = theme with { BackgroundFill = Ramp with { Direction = RibbonGradientDirection.Horizontal } } }, false);
        Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(32, 2).ToArgb()); Assert.Equal(Color.Red.ToArgb(), image.GetPixel(0, 32).ToArgb());
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void State_ink_rim_precedence_and_transparent_interior_use_actual_command_hooks(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        var chrome = new RibbonStateChromeSettings { HoverBorderColor = Color.Red, PressedBorderColor = Color.Lime, CheckedBorderColor = Color.Blue, CheckedHoverBorderColor = Color.Purple, InnerBorderColor = Color.Black, InnerBorderWidth = 1, InnerBorderInset = 2 };
        var theme = RibbonThemes.Office2013Light with { Hover = new(Color.White), Pressed = new(Color.White), Checked = new(Color.White), CommandAppearance = new() { CornerRadius = 0, StateChrome = chrome } };
        using var image = new Bitmap(51, 51); using var g = Graphics.FromImage(image); g.CompositingMode = CompositingMode.SourceCopy;
        foreach (var (hot, pressed, check, expected) in new[] { (true, false, false, Color.Red), (true, true, false, Color.Lime), (false, false, true, Color.Blue), (true, false, true, Color.Purple) })
        {
            g.Clear(Color.White); var c = new RibbonPaintContext(g, new(0, 0, 51, 51), theme, SystemFonts.MessageBoxFont!, 96, hot, pressed);
            renderer.DrawItem(c, new RibbonToggleItem { Checked = check }, RibbonItemSize.Large, false);
            Assert.Equal(expected.ToArgb(), image.GetPixel(1, 25).ToArgb()); Assert.Equal(Color.Black.ToArgb(), image.GetPixel(3, 25).ToArgb()); Assert.Equal(255, image.GetPixel(25, 25).A); Assert.Equal(CompositingMode.SourceCopy, g.CompositingMode);
        }
        var explicitTheme = theme with { CommandAppearance = theme.CommandAppearance! with { BorderColor = Color.Gold } };
        g.Clear(Color.White); renderer.DrawItem(new(g, new(0, 0, 51, 51), explicitTheme, SystemFonts.MessageBoxFont!, 96, Hot: true), new RibbonButtonItem(), RibbonItemSize.Large, false);
        Assert.Equal(Color.Gold.ToArgb(), image.GetPixel(1, 25).ToArgb());
        renderer.DrawItem(new(g, new(0, 0, 3, 3), theme with { CommandAppearance = new() { StateChrome = chrome with { InnerBorderInset = 1024, InnerBorderWidth = 32 } } }, SystemFonts.MessageBoxFont!, 192, Hot: true), new RibbonButtonItem(), RibbonItemSize.Small, false);
    });

    [Fact]
    public void Extended_clipped_alpha_surface_composes_each_pixel_once()
    {
        using var painter = new RibbonRenderer(); using var image = new Bitmap(65, 65); using var g = Graphics.FromImage(image); g.Clear(Color.Transparent);
        var fill = RibbonFillColors.Map(Ramp, color => Color.FromArgb(128, color));
        painter.PaintFill(new(g, new(0, 0, 65, 65), RibbonThemes.Office2024Light, SystemFonts.MessageBoxFont!, 96) { BackgroundBounds = new(0, 16, 65, 33) }, fill);
        foreach (int y in new[] { 0, 16, 32, 48, 64 }) Assert.Equal(128, image.GetPixel(32, y).A);
        Assert.InRange(image.GetPixel(32, 32).G, 254, 255); Assert.Equal(0, image.GetPixel(32, 32).R);
    }

    [Fact]
    public void Native_caption_restore_uses_full_gradient_coordinates_and_form_color_wins() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Bounds = new(-16000, -16000, 600, 450), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2013Light with { TitleBarBackgroundFill = Ramp } }; form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        int height = ribbon.WindowCaptionHeight; Assert.True(height > 10);
        using var image = new Bitmap(form.ClientSize.Width, height); using var g = Graphics.FromImage(image); g.Clear(Color.Magenta);
        var restore = new Rectangle(20, 3, 20, 5); ribbon.PaintCaptionBackground(g, restore, false);
        Assert.Equal(RibbonFillColors.At(Ramp, 5d / (height - 1)).ToArgb(), image.GetPixel(25, 5).ToArgb()); Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(1, 1).ToArgb());
        form.TitleBarColor = Color.Purple; ribbon.PaintCaptionBackground(g, restore, false); Assert.Equal(Color.Purple.ToArgb(), image.GetPixel(25, 5).ToArgb());
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void File_gallery_choice_and_options_chrome_share_opt_in_ink_and_width_reset(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        var chrome = new RibbonStateChromeSettings { HoverBorderColor = Color.Navy, CheckedBorderColor = Color.Navy, InnerBorderColor = Color.Black, InnerBorderWidth = 1 };
        var theme = RibbonThemes.Office2013Light with { FileButtonAppearance = new() { StateChrome = chrome }, GalleryAppearance = new() { StateChrome = chrome }, EditorAppearance = new() { StateChrome = chrome, ChoiceBorderWidth = 2 }, OptionsAppearance = new() { StateChrome = chrome } };
        using var image = new Bitmap(65, 65); using var g = Graphics.FromImage(image); var c = new RibbonPaintContext(g, new(0, 0, 65, 65), theme, SystemFonts.MessageBoxFont!, 96, Hot: true);
        painter.DrawFileButton(c, new RibbonDropDownItem()); Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(0, 32).ToArgb());
        painter.DrawGalleryTile(c with { Hot = false }, new RibbonGalleryTile(), true); Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(1, 32).ToArgb());
        using var choice = new RibbonComboBoxItem(); choice.Items.Add(""); g.Clear(Color.White); painter.DrawComboBoxItem(c, choice, 0, false); Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(2, 32).ToArgb());
        using var rounded = new RoundedPainter(); using var button = new Button { Size = image.Size };
        RibbonOptionsButtonRenderer.Draw(rounded, c, button, true, true, false, false, false, 0, Color.White); Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(0, 32).ToArgb());
        var hidden = theme with { OptionsAppearance = theme.OptionsAppearance! with { ButtonBorderWidth = 0, StateChrome = chrome with { InnerBorderWidth = 0 } } };
        g.Clear(Color.White); RibbonOptionsButtonRenderer.Draw(rounded, c with { Theme = hidden }, button, true, true, false, false, false, 0, Color.White); Assert.NotEqual(Color.Navy.ToArgb(), image.GetPixel(0, 32).ToArgb());
    });
}
