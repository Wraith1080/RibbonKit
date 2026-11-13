using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ThemeAccentAdaptationTests
{
    [Fact]
    public void Primary_options_button_feedback_darkens_the_idle_accent()
    {
        foreach (var basis in new[] { RibbonThemes.Office2013Light, RibbonThemes.Office2019Light, RibbonThemes.Office2024Light })
        foreach (var accent in new[] { Color.FromArgb(43, 87, 154), Color.Purple, Color.Teal })
        {
            var m = new RibbonOptionsShellMetrics(basis with { Accent = accent }, 96);
            var idle = m.ButtonFill(true, true, false, false).Start;
            var hot = m.ButtonFill(true, true, true, false).Start; var pressed = m.ButtonFill(true, true, true, true).Start;
            Assert.True(hot.GetBrightness() < idle.GetBrightness()); Assert.True(pressed.GetBrightness() < hot.GetBrightness());
            Assert.Equal(Color.White, m.ButtonForeground(true, true, false, false));
            Assert.Equal(Color.White, m.ButtonForeground(true, true, true, false));
            Assert.Equal(Color.White, m.ButtonForeground(true, true, false, true));
        }
    }

    [Fact]
    public void Primary_button_darkening_keeps_alpha_gradients_overrides_and_independent_reset() => FoundationTests.Sta(() =>
    {
        var fill = new RibbonFill(Color.FromArgb(80, 100, 140, 200), Color.FromArgb(170, 60, 100, 150));
        var theme = RibbonThemes.Office2024Light with { OptionsAppearance = new() { AccentButtonBackground = fill, AccentButtonHoverDarkening = .30, AccentButtonPressedDarkening = .50 } };
        var m = new RibbonOptionsShellMetrics(theme, 96); var hot = m.ButtonFill(true, true, true, false); var down = m.ButtonFill(true, true, false, true);
        Assert.Equal(fill.Start.A, hot.Start.A); Assert.Equal(fill.End.A, down.End.A);
        Assert.Equal(70, hot.Start.R); Assert.Equal(30, down.End.R);
        var zero = theme with { OptionsAppearance = theme.OptionsAppearance! with { AccentButtonHoverDarkening = 0, AccentButtonPressedDarkening = 0 } };
        Assert.Equal(fill, new RibbonOptionsShellMetrics(zero, 96).ButtonFill(true, true, true, false));
        Assert.Equal(fill, new RibbonOptionsShellMetrics(zero, 96).ButtonFill(true, true, false, true));
        var reset = theme with { OptionsAppearance = theme.OptionsAppearance! with { AccentButtonHoverDarkening = null } };
        Assert.Equal(90, new RibbonOptionsShellMetrics(reset, 96).ButtonFill(true, true, true, false).Start.R);
        Assert.Equal(down, new RibbonOptionsShellMetrics(reset, 96).ButtonFill(true, true, false, true));
        var authored = theme with { Checked = new(Color.Gold), Pressed = new(Color.Lime), OptionsAppearance = theme.OptionsAppearance! with { AccentButtonHoverBackground = fill, AccentButtonPressedBackground = new(Color.Transparent), AccentButtonForeground = Color.Navy } };
        var a = new RibbonOptionsShellMetrics(authored, 96);
        Assert.Equal(fill, a.ButtonFill(true, true, true, false)); Assert.Equal(Color.Transparent, a.ButtonFill(true, true, false, true).Start); Assert.Equal(Color.Navy, a.ButtonForeground(true, true, true, false));
        var stateReset = authored with { OptionsAppearance = authored.OptionsAppearance! with { AccentButtonHoverBackground = null, AccentButtonPressedBackground = null, AccentButtonForeground = Color.Empty } };
        Assert.Equal(Color.Gold, new RibbonOptionsShellMetrics(stateReset, 96).ButtonFill(true, true, true, false).Start);
        Assert.Equal(Color.Lime, new RibbonOptionsShellMetrics(stateReset, 96).ButtonFill(true, true, false, true).Start);
        Assert.Equal(fill, new RibbonOptionsShellMetrics(theme with { OptionsButtonAccentStates = new() { PressedBackground = fill } }, 96).ButtonFill(true, true, false, true));
        using var ribbon = new RibbonControl { Theme = theme };
        foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, -.01, 1.01 })
        foreach (var bad in new[] { theme with { OptionsAppearance = new() { AccentButtonHoverDarkening = invalid } }, theme with { OptionsAppearance = new() { AccentButtonPressedDarkening = invalid } } })
        { Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = bad); Assert.Same(theme, ribbon.Theme); }
        var legacy = theme with { OptionsButtonAccentStates = null, OptionsAppearance = null };
        Assert.Equal(legacy.Checked, new RibbonOptionsShellMetrics(legacy, 96).ButtonFill(true, true, true, false));
        Assert.Equal(legacy.Foreground, new RibbonOptionsShellMetrics(legacy, 96).ButtonForeground(true, true, true, false));
    });

    [Fact]
    public void Office2019_selection_follows_accent_but_ordinary_hover_press_and_header_remain_neutral()
    {
        var theme = RibbonThemes.Office2019Light; var purple = theme with { Accent = Color.Purple };
        Assert.Equal(new RibbonFill(Color.FromArgb(222, 220, 218)), purple.Hover);
        Assert.Equal(new RibbonFill(Color.FromArgb(207, 205, 203)), purple.Pressed);
        Assert.NotEqual(theme.Checked, purple.Checked); Assert.NotEqual(theme.CommandStateFill(true, false, true), purple.CommandStateFill(true, false, true));
        Assert.Equal(Color.FromArgb(230, 230, 230), purple.Background); Assert.Equal(theme.HeaderHover, purple.HeaderHover);
    }

    [Fact]
    public void Office2024_keeps_grey_hover_press_but_toggle_and_gallery_selection_follow_accent()
    {
        var basis = RibbonThemes.Office2024Light;
        Assert.Equal(Color.FromArgb(217, 225, 237), basis.Checked.Start);
        Assert.Equal(Color.FromArgb(196, 208, 227), basis.CommandStateFill(true, false, true).Start);
        foreach (var accent in new[] { Color.Purple, Color.Teal, Color.DarkRed })
        {
            var theme = basis with { Accent = accent };
            foreach (var fill in new[] { theme.Hover, theme.Pressed })
            { Assert.Equal(fill.Start.R, fill.Start.G); Assert.Equal(fill.Start.G, fill.Start.B); }
            Assert.Equal(basis.Hover, theme.Hover); Assert.Equal(basis.Pressed, theme.Pressed);
            Assert.NotEqual(basis.Checked, theme.Checked); Assert.NotEqual(basis.CommandStateFill(true, false, true), theme.CommandStateFill(true, false, true));
        }
    }

    [Fact]
    public void Only_primary_configuration_buttons_have_accent_idle_fills()
    {
        foreach (var basis in new[] { RibbonThemes.Office2019Light, RibbonThemes.Office2024Light })
        foreach (bool primary in new[] { false, true })
        {
            var theme = basis with { Accent = Color.Purple }; var metrics = new RibbonOptionsShellMetrics(theme, 96);
            Assert.Equal((primary ? theme.Accent : theme.Surface).ToArgb(), metrics.ButtonFill(primary, true, false, false).Start.ToArgb());
            var hot = metrics.ButtonFill(primary, true, true, false).Start;
            Assert.NotEqual(hot.R, hot.G);
            var pressed = metrics.ButtonFill(primary, true, true, true).Start;
            Assert.True(pressed.GetBrightness() < hot.GetBrightness());
        }
    }

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void State_ink_and_fill_policy_reaches_actual_root_menu_qat_split_and_gallery_hooks(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => (int)Math.Round(n * dpi / 96d);
        foreach (bool office in new[] { false, true })
        foreach (var basis in new[] { RibbonThemes.Office2019Light, RibbonThemes.Office2024Light })
        foreach (var accent in new[] { Color.Purple, Color.Teal, Color.DarkRed })
        {
            var theme = basis with { Accent = accent };
            using RibbonRenderer painter = office ? new OfficeInk() : new NeutralInk(); var ink = (IInk)painter;
            var icon = new IconInk(); using var toggle = new RibbonToggleItem { Text = "Action", Icon = icon };
            using var ribbon = new RibbonControl { Theme = theme }; var tab = new RibbonTab(); var group = new RibbonGroup();
            group.Items.Add(toggle); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            using var reference = new RibbonQuickAccessReference { Source = toggle }; ribbon.QuickAccessItems.Add(reference);
            using var split = new RibbonSplitButtonItem { Text = "Split", Icon = icon };
            using var tile = new RibbonGalleryTile { Text = "Choice", Icon = icon };
            using var image = new Bitmap(S(300), S(110)); using var g = Graphics.FromImage(image);
            var c = new RibbonPaintContext(g, new(S(10), S(10), S(120), S(60)), theme, Control.DefaultFont, dpi);
            foreach (var (hot, pressed, check, enabled, focused) in new[]
            { (false,false,false,true,false), (true,false,false,true,false), (true,true,false,true,false),
              (false,false,true,true,false), (true,false,true,true,false), (false,false,false,true,true), (true,true,true,false,false) })
            {
                toggle.Checked = check; toggle.Enabled = enabled; split.Checked = check; split.Enabled = enabled; tile.Enabled = enabled;
                var context = c with { Hot = hot, Pressed = pressed, Focused = focused };
                var expected = !enabled ? theme.DisabledColor : theme.Foreground;
                foreach (bool menu in new[] { false, true })
                {
                    g.Clear(Color.White); painter.DrawItem(context, toggle, RibbonItemSize.Medium, menu);
                    Assert.Equal(expected, ink.Color); Assert.Equal(expected, icon.Color);
                    if (enabled && (hot || pressed || check)) Assert.Equal(theme.CommandStateFill(hot, pressed, check).Start.ToArgb(), image.GetPixel(S(18), S(18)).ToArgb());
                    painter.DrawQuickAccessItem(context, reference, menu); Assert.Equal(expected, icon.Color);
                }
                painter.DrawSplitItem(context, context with { Bounds = new(S(130), S(10), S(30), S(60)) }, split, RibbonItemSize.Medium, false);
                Assert.Equal(expected, ink.Color); Assert.Equal(expected, icon.Color);
                painter.DrawGalleryTile(context, tile, check); Assert.Equal(expected, ink.Color); Assert.Equal(expected, icon.Color);
                painter.DrawItem(context with { ApplicationMenuPart = RibbonApplicationMenuPart.Navigation }, toggle, RibbonItemSize.Medium, true);
                Assert.Equal(expected, ink.Color);
            }
            toggle.Enabled = true; toggle.Checked = true;
            painter.DrawItem(c, toggle, RibbonItemSize.Medium, false);
            Assert.Equal(theme.Foreground, ink.Color);
            split.Enabled = true; split.Checked = false;
            painter.DrawSplitItem(c, c with { Bounds = new(S(130), S(10), S(30), S(60)), Hot = true }, split, RibbonItemSize.Medium, false);
            Assert.Equal(theme.Foreground, icon.Color);
            using var choices = new RibbonDropDownItem { MenuPresentation = RibbonMenuPresentation.IllustratedChoices };
            group.Items.Remove(toggle); choices.Items.Add(toggle); toggle.Checked = true;
            g.Clear(Color.White); painter.DrawItem(c with { Hot = true }, toggle, RibbonItemSize.Medium, true);
            Assert.Equal(theme.CommandStateFill(true, false, true).Start.ToArgb(), image.GetPixel(S(18), S(18)).ToArgb());
            Assert.Equal(theme.Foreground, ink.Color);
        }
    });

    [Fact]
    public void Overrides_reset_and_dialog_palette_validation_are_independent() => FoundationTests.Sta(() =>
    {
        var fill = new RibbonFill(Color.FromArgb(80, Color.Red), Color.Transparent);
        var authored = RibbonThemes.Office2019Light with { Hover = fill, Pressed = new(Color.Lime), Checked = new(Color.Gold) };
        var changed = authored with { Accent = Color.Purple };
        Assert.Equal(fill, changed.Hover); Assert.Equal(Color.Lime, changed.Pressed.Start); Assert.Equal(Color.Gold, changed.Checked.Start);
        Assert.NotEqual(fill, (changed with { HoverOverride = null }).Hover);
        var recipe = changed with { HoverOverride = null, PressedOverride = null, AccentStates = changed.AccentStates! with { HoverBackground = fill, PressedBackground = new(Color.Transparent) } };
        Assert.Equal(fill, recipe.Hover); Assert.Equal(Color.Transparent, recipe.Pressed.Start);
        Assert.NotEqual(fill, (recipe with { AccentStates = recipe.AccentStates! with { HoverBackground = null } }).Hover);
        var neutral = RibbonThemes.Office2024Light with { Accent = Color.Purple };
        var zero = neutral with { OptionsButtonAccentStates = new() { HoverOpacity = 0, PressedOpacity = 0 } };
        Assert.Equal(neutral.Surface.ToArgb(), new RibbonOptionsShellMetrics(zero, 96).ButtonFill(false, true, true, false).Start.ToArgb());
        Assert.Equal(neutral.Hover, zero.Hover);
        var custom = neutral with { OptionsAppearance = new() { ButtonBackground = fill, AccentButtonBackground = new(Color.Lime) } };
        Assert.Equal(fill, new RibbonOptionsShellMetrics(custom, 96).ButtonFill(false, true, false, false));
        Assert.Equal(Color.Lime, new RibbonOptionsShellMetrics(custom, 96).ButtonFill(true, true, false, false).Start);
        using var ribbon = new RibbonControl { Theme = neutral };
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = neutral with { OptionsButtonAccentStates = new() { HoverOpacity = double.NaN } });
        Assert.Same(neutral, ribbon.Theme);
        var legacy = neutral with { OptionsButtonAccentStates = null };
        Assert.Equal(neutral.Surface, new RibbonOptionsShellMetrics(legacy, 96).ButtonFill(false, true, false, false).Start);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Actual_ok_cancel_apply_buttons_recolor_without_changing_native_ownership(bool flat) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = (flat ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { Accent = Color.Purple } };
        using var dialog = new RibbonOptionsDialog(ribbon) { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false };
        var page = new RibbonOptionsPage { Text = "General" }; var field = new TextBox { Text = "Retained configuration" }; page.Controls.Add(field); dialog.Pages.Add(page);
        dialog.Show(); Application.DoEvents(); field.Focus(); field.Select(2, 4); var handle = field.Handle; var parent = field.Parent;
        var buttons = Descendants(dialog).OfType<Button>().Where(b => b.Name is "AcceptCustomization" or "CancelCustomization" or "ApplyCustomization").ToArray();
        Assert.Equal(3, buttons.Length); var handles = buttons.Select(b => b.Handle).ToArray(); var bounds = buttons.Select(b => b.Bounds).ToArray();
        foreach (var accent in new[] { Color.Purple, Color.Teal, Color.DarkRed })
        {
            ribbon.Theme = ribbon.Theme with { Accent = accent };
            foreach (var button in buttons)
            {
                using var image = new Bitmap(button.Width, button.Height); button.DrawToBitmap(image, new(Point.Empty, image.Size));
                Assert.Equal((button.Name == "AcceptCustomization" ? accent : ribbon.Theme.Surface).ToArgb(), image.GetPixel(button.Width / 2, 5).ToArgb());
                Invoke(button, "OnMouseEnter", EventArgs.Empty); button.DrawToBitmap(image, new(Point.Empty, image.Size));
                var hot = image.GetPixel(button.Width / 2, 5); Assert.NotEqual(hot.R, hot.G);
                if (button.Name == "AcceptCustomization")
                {
                    Assert.True(hot.GetBrightness() < accent.GetBrightness());
                    Assert.Contains(Enumerable.Range(12, button.Width - 24), x => Enumerable.Range(5, button.Height - 10).Any(y => image.GetPixel(x, y).ToArgb() == Color.White.ToArgb()));
                }
                Invoke(button, "OnKeyDown", new KeyEventArgs(Keys.Space)); button.DrawToBitmap(image, new(Point.Empty, image.Size));
                Assert.True(image.GetPixel(button.Width / 2, 5).GetBrightness() < hot.GetBrightness());
                if (button.Name == "AcceptCustomization") Assert.Contains(Enumerable.Range(12, button.Width - 24), x => Enumerable.Range(5, button.Height - 10).Any(y => image.GetPixel(x, y).ToArgb() == Color.White.ToArgb()));
                Assert.Equal(accent.ToArgb(), image.GetPixel(0, button.Height / 2).ToArgb());
                Invoke(button, "OnLostFocus", EventArgs.Empty); Invoke(button, "OnMouseLeave", EventArgs.Empty);
            }
            Assert.Equal(handles, buttons.Select(b => b.Handle)); Assert.Equal(bounds, buttons.Select(b => b.Bounds));
            Assert.Equal(handle, field.Handle); Assert.Same(parent, field.Parent); Assert.Equal("Retained configuration", field.Text);
            Assert.Equal(2, field.SelectionStart); Assert.Equal(4, field.SelectionLength);
            Assert.Same(buttons.Single(b => b.Name == "AcceptCustomization"), dialog.AcceptButton); Assert.Same(buttons.Single(b => b.Name == "CancelCustomization"), dialog.CancelButton);
        }
        static void Invoke(Button button, string method, object args) => typeof(RibbonOptionsButton).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(button, [args]);
    });

    [Fact]
    public void Accent_text_is_retained_for_active_tab_backstage_and_options_navigation() => FoundationTests.Sta(() =>
    {
        var theme = RibbonThemes.Office2024Light with { Accent = Color.Purple };
        using var image = new Bitmap(240, 60); using var g = Graphics.FromImage(image); using var renderer = new OfficeInk();
        var c = new RibbonPaintContext(g, new(Point.Empty, image.Size), theme, Control.DefaultFont, 96);
        using var tab = new RibbonTab { Text = "Active page" }; renderer.DrawTab(c, tab, true); Assert.Equal(theme.Accent, renderer.Color);
        renderer.DrawTab(c, tab, false); Assert.Equal(theme.HeaderTextColor, renderer.Color);
        using var page = new RibbonBackstagePageItem { Text = "Active page" }; renderer.DrawBackstageEntry(c, page, true); Assert.Equal(theme.Accent, renderer.Color);
        renderer.DrawBackstageEntry(c, page, false); Assert.Equal(theme.BackstageNavigationInk, renderer.Color);
        using var painter = new RoundedPainter();
        RibbonOptionsRenderer.DrawNavigation(renderer, painter, c, "Active page", true, false);
        Assert.Contains(Enumerable.Range(16, image.Width - 16), x => Enumerable.Range(0, image.Height).Any(y => image.GetPixel(x, y).ToArgb() == theme.Accent.ToArgb()));
        RibbonOptionsRenderer.DrawNavigation(renderer, painter, c, "Inactive page", false, false);
        Assert.DoesNotContain(Enumerable.Range(16, image.Width - 16), x => Enumerable.Range(0, image.Height).Any(y => image.GetPixel(x, y).ToArgb() == theme.Accent.ToArgb()));
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Dialog_button_press_uses_a_darker_wash_and_accent_border_with_authored_overrides(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => (int)Math.Ceiling(n * dpi / 96d);
        foreach (var basis in new[] { RibbonThemes.Office2019Light, RibbonThemes.Office2024Light })
        foreach (bool primary in new[] { false, true })
        {
            var theme = basis with { Accent = Color.Purple }; using var button = new Button { Size = new(S(160), S(48)), Text = "" };
            using var image = new Bitmap(button.Width, button.Height); using var g = Graphics.FromImage(image); using var painter = new RoundedPainter();
            var c = new RibbonPaintContext(g, new(Point.Empty, image.Size), theme, Control.DefaultFont, dpi);
            Paint(false); var hot = image.GetPixel(image.Width / 2, image.Height / 2);
            Paint(true); var pressed = image.GetPixel(image.Width / 2, image.Height / 2);
            Assert.True(pressed.GetBrightness() < hot.GetBrightness()); Assert.Equal(theme.Accent.ToArgb(), image.GetPixel(0, image.Height / 2).ToArgb());
            Paint(true, theme with { OptionsAppearance = new() { ButtonBorderColor = Color.Navy, ButtonBorderWidth = 2 } });
            Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(0, image.Height / 2).ToArgb());
            Paint(true, theme with { OptionsAppearance = new() { ButtonBorderWidth = 0 } });
            Assert.Equal(pressed.ToArgb(), image.GetPixel(0, image.Height / 2).ToArgb());
            Paint(true, theme with { OptionsAppearance = new() { ButtonBorderColor = Color.Transparent } });
            Assert.Equal(pressed.ToArgb(), image.GetPixel(0, image.Height / 2).ToArgb());
            void Paint(bool down, RibbonTheme? palette = null)
            { g.Clear(Color.White); RibbonOptionsButtonRenderer.Draw(painter, c with { Theme = palette ?? theme }, button, primary, true, down, true, false, S(4), Color.White); }
        }
    });

    private static IEnumerable<Control> Descendants(Control c) => c.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(Descendants(child)));
    private interface IInk { Color Color { get; } }
    private sealed class OfficeInk : RibbonOffice2024Renderer, IInk
    { public Color Color { get; private set; } public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) => Color = color; }
    private sealed class NeutralInk : RibbonRenderer, IInk
    { public Color Color { get; private set; } public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) => Color = color; }
    private sealed class IconInk : IRibbonIcon
    { internal Color Color; public void Draw(Graphics g, Rectangle bounds, RibbonIconContext c) => Color = c.Foreground; }
}
