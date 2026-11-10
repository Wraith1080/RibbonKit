using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2019ThemeTests
{
    [Fact]
    public void Selection_clones_reset_and_invalid_values_preserve_application_ownership() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl();
        var application = ribbon.Theme with { BodyMargin = new(12), Accent = Color.Purple, BodyContentHorizontalPadding = 9 };
        ribbon.Theme = application; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light;
        var selected = ribbon.Theme;
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = selected with { HeaderPresentation = (RibbonHeaderPresentation)27 });
        Assert.Same(selected, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme);
        ribbon.BuiltInTheme = null; Assert.Same(application, ribbon.Theme);
        ribbon.Theme = RibbonThemes.Office2019Light with { Accent = Color.Purple };
        Assert.Null(ribbon.BuiltInTheme); Assert.Equal(RibbonHeaderPresentation.Filled, ribbon.Theme.HeaderPresentation);
        Assert.Equal(Padding.Empty, ribbon.LayoutTheme.BodyMargin); Assert.Same(RibbonThemes.Office2024Light, other.Theme);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Filled_headers_have_square_white_selection_and_honor_the_viewport_clip(int dpi) => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonOffice2024Renderer(); var tab = new RibbonTab { Text = "A long scrolled header" };
        foreach (var background in new[] { Color.FromArgb(230, 230, 230), Color.FromArgb(43, 87, 154) })
        {
            var theme = RibbonThemes.Office2019Light with { Background = background, HeaderForeground = Color.White };
            var bounds = new Rectangle(10, 10, 260, 54); var clip = new Rectangle(100, 10, 120, 54);
            using var image = new Bitmap(300, 80); using var g = Graphics.FromImage(image); g.Clear(Color.Magenta); g.SetClip(clip);
            var context = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
            painter.DrawTab(context, tab, true);
            Assert.True(painter.GetTabIndicatorBounds(context, tab).IsEmpty);
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(100, 10).ToArgb());
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(219, 63).ToArgb());
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
                if (!clip.Contains(x, y)) Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(x, y).ToArgb());
            g.ResetClip(); g.Clear(background); painter.DrawTab(context with { Bounds = new(10, 10, 260, 54), Hot = true }, tab, false);
            Assert.Equal(theme.HeaderHover!.Value.Start.ToArgb(), image.GetPixel(10, 10).ToArgb());
            painter.DrawTabIndicator(context with { Bounds = new(10, 65, 100, 3) }, tab, true);
            Assert.Equal(background.ToArgb(), image.GetPixel(20, 66).ToArgb());
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Body_is_borderless_on_three_edges_but_popup_keeps_square_border_and_shadow(int dpi) => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonOffice2024Renderer(); var theme = RibbonThemes.Office2019Light;
        var bounds = new Rectangle(30, 30, 180, 80); using var image = new Bitmap(250, 150); using var g = Graphics.FromImage(image);
        var context = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
        g.Clear(Color.Magenta); painter.DrawBody(context);
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Left, bounds.Top).ToArgb());
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Right - 1, bounds.Top + 4).ToArgb());
        Assert.Equal(theme.Border.ToArgb(), image.GetPixel(bounds.Left, bounds.Bottom - 1).ToArgb());
        Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(bounds.Left - 1, bounds.Top + 4).ToArgb());
        Assert.Equal(Padding.Empty, painter.GetBodyShadowPadding(theme, dpi));
        g.Clear(Color.Transparent); painter.DrawPopupShadow(context); painter.DrawPopupFrame(context);
        Assert.Equal(0, painter.GetPopupCornerRadius(theme, dpi));
        foreach (var point in new[] { bounds.Location, new Point(bounds.Right - 1, bounds.Top), new Point(bounds.Left, bounds.Bottom - 1), new Point(bounds.Right - 1, bounds.Bottom - 1) })
            Assert.Equal(theme.PopupBorder.ToArgb(), image.GetPixel(point.X, point.Y).ToArgb());
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Left + 10, bounds.Top + 10).ToArgb());
        Assert.True(image.GetPixel(bounds.Right + 1, bounds.Top + 30).A > 0);
        Assert.True(painter.GetPopupShadowPadding(theme, dpi) > 0);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Body_background_spans_client_width_while_content_keeps_inner_padding(bool neutral) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = neutral ? new RibbonRenderer() : new RibbonOffice2024Renderer();
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2019Light, Renderer = painter };
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Editing" };
        group.Items.Add(new RibbonButtonItem { Text = "Open" }); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = (RibbonButtonItem)group.Items[0] });
        ribbon.MessageBar.Items.Add(new RibbonMessage { Text = "Notice" });
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>()) foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int width in new[] { 320, 900 }) foreach (var qat in new[] { RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.BelowRibbon })
        {
            ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Width = width; ribbon.QuickAccessPosition = qat;
            var layout = ribbon.Snapshot;
            Assert.Equal(0, layout.BodyFrameBounds.Left); Assert.Equal(ribbon.ClientSize.Width, layout.BodyFrameBounds.Right);
            Assert.True(layout.BodyViewport.Left > layout.BodyBounds.Left); Assert.True(layout.BodyViewport.Right < layout.BodyBounds.Right);
        }
    });

    [Fact]
    public void Caption_coloring_extends_to_header_and_resets_without_touching_authored_themes() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { ClientSize = new(1000, 500) }; using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2019Light };
        ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        var authored = ribbon.Theme; Assert.Equal(authored.Background, ribbon.HeaderPaintTheme(false).Background);
        form.TitleBarColor = Color.FromArgb(43, 87, 154);
        var colored = ribbon.HeaderPaintTheme(false); var chrome = ribbon.HeaderPaintTheme(false, true);
        Assert.Equal(form.TitleBarColor, colored.Background); Assert.Equal(Color.White, colored.HeaderForeground);
        Assert.Equal(authored.Foreground, colored.Foreground); Assert.Equal(Color.White, colored.Surface);
        Assert.Equal(Color.White, chrome.Foreground); Assert.Same(colored, ribbon.HeaderPaintTheme(false));
        form.TitleBarColor = Color.LightYellow;
        Assert.Equal(Color.Black, ribbon.HeaderPaintTheme(false).HeaderForeground);
        ribbon.Theme = RibbonThemes.Office2024Light;
        Assert.Same(ribbon.Theme, ribbon.HeaderPaintTheme(false)); Assert.Equal(Color.LightYellow, form.TitleBarColor);
        ribbon.Theme = authored; form.TitleBarColor = Color.Empty;
        Assert.Equal(authored.Background, ribbon.HeaderPaintTheme(false).Background); Assert.Equal(authored.Foreground, ribbon.HeaderPaintTheme(false, true).Foreground);
        Assert.Same(authored, ribbon.Theme);
    });

    [Fact]
    public void Fixed_focus_keytip_and_checkbox_radii_honor_square_theme() => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonOffice2024Renderer();
        var theme = RibbonThemes.Office2019Light with { KeyTipBackground = new(Color.Navy), KeyTipBorder = Color.Red };
        using var image = new Bitmap(100, 90); using var g = Graphics.FromImage(image);
        var bounds = new Rectangle(10, 10, 70, 60); var context = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, 96);
        g.Clear(Color.Magenta); painter.DrawKeyTip(context, ""); Assert.Equal(Color.Red.ToArgb(), image.GetPixel(10, 10).ToArgb());
        g.Clear(Color.Magenta); painter.DrawFocus(context); Assert.Equal(theme.Accent.ToArgb(), image.GetPixel(12, 12).ToArgb());
        g.Clear(Color.Magenta); painter.DrawCheckBoxIndicator(context, new RibbonCheckBoxItem()); Assert.NotEqual(Color.Magenta.ToArgb(), image.GetPixel(10, 10).ToArgb());
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Explicit_decorative_radius_can_override_the_square_preset_independently(int dpi) => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonOffice2024Renderer();
        var theme = RibbonThemes.Office2019Light with { KeyTipCornerRadius = 12, KeyTipBackground = new(Color.Navy) };
        using var image = new Bitmap(100, 80); using var g = Graphics.FromImage(image); g.Clear(Color.Magenta);
        painter.DrawKeyTip(new(g, new(10, 10, 70, 50), theme, Control.DefaultFont, dpi), "");
        Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(10, 10).ToArgb());
        Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(40, 30).ToArgb());
        Assert.Equal(0, painter.GetPopupCornerRadius(theme, dpi)); Assert.Equal(0, theme.FocusCornerRadius);
    });

    [Fact]
    public void Light_theme_switching_retains_native_editor_handles_values_and_closes_stale_popups() => FoundationTests.Sta(() =>
    {
        using var host = new Form { ClientSize = new(1100, 600) }; using var ribbon = new RibbonControl();
        var text = new RibbonTextBoxItem { Text = "Name", Value = "Retained text" }; var combo = new RibbonComboBoxItem { Text = "Font" };
        combo.Items.AddRange("First", "Second"); combo.SelectedIndex = 1;
        var menu = new RibbonDropDownItem { Text = "Menu" }; menu.Items.Add(new RibbonButtonItem { Text = "Action" });
        var group = new RibbonGroup { Text = "Fields", CanResize = false }; group.Items.AddRange(text, combo, menu);
        var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab); host.Controls.Add(ribbon);
        host.Show(); Application.DoEvents(); ribbon.SyncEditors();
        var nativeText = ribbon.Editors.EditorFor(text)!; var nativeCombo = ribbon.Editors.EditorFor(combo)!;
        var textHandle = nativeText.Handle; var comboHandle = nativeCombo.Handle; nativeText.Select(2, 5);
        menu.ShowDropDown(); Assert.True(menu.IsDropDownOpen);
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.SyncEditors();
        Assert.False(menu.IsDropDownOpen); Assert.Equal(textHandle, nativeText.Handle); Assert.Equal(comboHandle, nativeCombo.Handle);
        Assert.Equal("Retained text", nativeText.Text); Assert.Equal(2, nativeText.SelectionStart); Assert.Equal(5, nativeText.SelectionLength); Assert.Equal(1, combo.SelectedIndex);
        ribbon.BuiltInTheme = null; ribbon.SyncEditors(); Assert.Same(RibbonThemes.Office2024Light, ribbon.Theme);
        Assert.Equal(textHandle, nativeText.Handle); Assert.Equal(comboHandle, nativeCombo.Handle);
    });

    [Fact]
    public void Designer_catalog_accepts_2019_and_inert_previews_preserve_models() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var selected = ribbon.SelectedTab; var command = ribbon.Tabs[0].Groups[0].Items[0]; var custom = ribbon.Theme with { Accent = Color.Purple };
        ribbon.Theme = custom; var draft = RibbonDesignCommitter.Read(ribbon, host);
        Assert.Contains("Office2019Light", draft.ThemeChoices); draft.BuiltInTheme = "Office2019Light";
        var preview = RibbonThemePreviewRenderer.Render(ribbon, host, draft); Assert.NotNull(preview);
        Assert.Same(custom, ribbon.Theme); Assert.Null(ribbon.BuiltInTheme);
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(1, host.Committed);
        Assert.Same(selected, ribbon.SelectedTab); Assert.Same(command, ribbon.Tabs[0].Groups[0].Items[0]);
        Assert.Same(RibbonThemes.Office2019Light, ribbon.Theme);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Gallery_backstage_and_footer_state_chrome_have_square_corners(bool neutral) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = neutral ? new RibbonRenderer() : new RibbonOffice2024Renderer();
        var theme = RibbonThemes.Office2019Light; using var image = new Bitmap(120, 90); using var g = Graphics.FromImage(image);
        var bounds = new Rectangle(10, 10, 90, 60); var context = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, 120);
        g.Clear(Color.Magenta); painter.DrawGalleryTile(context, new RibbonGalleryTile(), true);
        Assert.Equal(theme.Accent.ToArgb(), image.GetPixel(11, 11).ToArgb());
        g.Clear(Color.Magenta); painter.DrawItem(context with { ApplicationMenuPart = RibbonApplicationMenuPart.Footer }, new RibbonButtonItem(), RibbonItemSize.Medium, true);
        Assert.Equal(theme.PopupBorder.ToArgb(), image.GetPixel(11, 11).ToArgb());
        g.Clear(Color.Magenta); painter.DrawBackstageEntry(context, null, false);
        // The circular Back control is centered in the wider row.
        int side = Math.Min(bounds.Width, bounds.Height) - 2 * RibbonLayoutEngine.Scale(2, context.DeviceDpi);
        int left = bounds.Left + (bounds.Width - side) / 2, top = bounds.Top + (bounds.Height - side) / 2;
        Assert.NotEqual(Color.Magenta.ToArgb(), image.GetPixel(left, top).ToArgb());
    });
}
