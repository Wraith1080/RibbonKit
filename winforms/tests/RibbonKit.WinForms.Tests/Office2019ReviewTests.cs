using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2019ReviewTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Collapsed_group_hover_has_dropdown_fill_without_a_frame(bool simplified) => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonOffice2024Renderer();
        var theme = RibbonThemes.Office2019Light with { Border = Color.Magenta };
        using var image = new Bitmap(180, 130); using var g = Graphics.FromImage(image); g.Clear(Color.White);
        var context = new RibbonPaintContext(g, new(10, 10, 140, 100), theme, Control.DefaultFont, 96, Hot: true) { IsSimplifiedGroup = simplified };
        painter.DrawGroup(context, new RibbonGroup { Text = "Group" }, true);
        Assert.Equal(theme.Hover.Start.ToArgb(), image.GetPixel(11, 25).ToArgb());
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Application_menu_split_hover_has_no_action_divider(bool arrowHot) => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonOffice2024Renderer(); var theme = RibbonThemes.Office2019Light with { Border = Color.Magenta };
        using var image = new Bitmap(250, 90); using var g = Graphics.FromImage(image); g.Clear(Color.White);
        var primary = new RibbonPaintContext(g, new(10, 10, 180, 60), theme, Control.DefaultFont, 96, Hot: !arrowHot) { ApplicationMenuPart = RibbonApplicationMenuPart.Navigation };
        var secondary = primary with { Bounds = new(190, 10, 35, 60), Hot = arrowHot };
        painter.DrawSplitItem(primary, secondary, new RibbonSplitButtonItem { Text = "Save As" }, RibbonItemSize.Medium, true);
        Assert.Equal(theme.Hover.Start.ToArgb(), image.GetPixel(190, 17).ToArgb());
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Message_panel_has_horizontal_rules_without_side_borders(bool connected) => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonOffice2024Renderer(); var theme = RibbonThemes.Office2019Light with { MessageBorder = Color.Magenta };
        using var image = new Bitmap(250, 100); using var g = Graphics.FromImage(image); g.Clear(Color.White);
        painter.DrawMessageBar(new(g, new(10, 10, 220, 70), theme, Control.DefaultFont, 96) { IsTopEdgeConnected = connected });
        Assert.Equal(theme.MessageBackground.Start.ToArgb(), image.GetPixel(10, 40).ToArgb());
        Assert.Equal(theme.MessageBackground.Start.ToArgb(), image.GetPixel(229, 40).ToArgb());
        Assert.Equal(theme.MessageBorder.ToArgb(), image.GetPixel(10, 79).ToArgb());
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Header_scrollers_match_the_tab_height_and_use_the_entire_paint_slot(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2019Light, Width = 700, Density = density };
        for (int i = 0; i < 12; i++) ribbon.Tabs.Add(new RibbonTab { Text = "Long tab header " + i });
        var snapshot = ribbon.Snapshot; var scroll = snapshot.Targets.Single(t => t.Kind == TargetKind.TabBack);
        Assert.Equal(snapshot.TabViewport.Height, scroll.Bounds.Width); Assert.Equal(snapshot.TabViewport.Height, scroll.Bounds.Height);
        using var painter = new RibbonOffice2024Renderer(); using var image = new Bitmap(100, 100); using var g = Graphics.FromImage(image); g.Clear(Color.Magenta);
        foreach (var (hot, pressed) in new[] { (false, false), (true, false), (true, true) })
        {
            painter.DrawScrollButton(new(g, new(10, 10, 60, 60), ribbon.Theme, ribbon.Font, 120, Hot: hot, Pressed: pressed), true);
            Assert.Equal(ribbon.Theme.PopupBorder.ToArgb(), image.GetPixel(10, 10).ToArgb());
            Assert.Equal(ribbon.Theme.PopupBorder.ToArgb(), image.GetPixel(69, 69).ToArgb());
        }
    });

    [Fact]
    public void Library_options_buttons_have_square_themed_edges() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2019Light with { PopupBorder = Color.Magenta } };
        using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "review");
        var buttons = Descendants(dialog).OfType<Button>().Where(b => b.Name != "AcceptCustomization").ToArray();
        Assert.True(buttons.Length > 10);
        foreach (var button in buttons)
        {
            button.AutoSize = false; button.Size = new(90, 34); using var image = new Bitmap(90, 34); button.DrawToBitmap(image, new(Point.Empty, image.Size));
            Assert.True(image.GetPixel(0, 0).ToArgb() == Color.Magenta.ToArgb(), $"{button.Name}: rounded or non-themed corner.");
        }
    });

    [Fact]
    public void Maximized_File_application_menu_has_no_left_work_area_gutter() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { WindowState = FormWindowState.Maximized };
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2019Light, AnimationLevel = RibbonAnimationLevel.None };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Open" });
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents(); ribbon.FileMenu.ShowDropDown();
        var popup = Assert.IsType<RibbonPopupSurface>(ribbon.Popups.Top);
        int anchor = ribbon.PointToScreen(ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.FileMenu).Bounds.Location).X;
        int expected = Math.Max(Screen.FromControl(form).WorkingArea.Left, anchor);
        Assert.Equal(expected, popup.PointToScreen(Point.Empty).X - 1);
    });

    [Fact]
    public void Default_options_buttons_match_saved_pixels() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "review-default");
        int index = 0;
        foreach (var button in Descendants(dialog).OfType<Button>())
        {
            using var image = new Bitmap(button.Width, button.Height); button.DrawToBitmap(image, new(Point.Empty, image.Size));
            using var png = new MemoryStream(); image.Save(png, System.Drawing.Imaging.ImageFormat.Png);
            string name = $"options-{index++}-{button.Name}.png";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_REVIEW_CAPTURE") is { Length: > 0 } output)
            { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name), png.ToArray()); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_REVIEW_BASELINE") is { Length: > 0 } baseline)
                Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name)), png.ToArray());
        }
        Assert.True(index > 10);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Header_width_override_scales_and_preserves_touch_minimum_and_body_scroll_chrome(int dpi) => FoundationTests.Sta(() =>
    {
        var theme = RibbonThemes.Office2019Light with { HeaderScrollButtonWidth = 18, HeaderScrollButtonBackground = new(Color.Lime), HeaderScrollButtonBorder = Color.Transparent };
        Assert.Equal(RibbonLayoutEngine.Scale(18, dpi), new RibbonChromeMetrics(theme, RibbonDensity.Compact).HeaderScrollWidth(dpi, 100));
        Assert.Equal(RibbonLayoutEngine.Scale(32, dpi), new RibbonChromeMetrics(theme, RibbonDensity.Touch).HeaderScrollWidth(dpi, 100));
        using var painter = new RibbonOffice2024Renderer(); using var image = new Bitmap(100, 100); using var g = Graphics.FromImage(image);
        var context = new RibbonPaintContext(g, new(10, 10, 60, 60), theme, Control.DefaultFont, dpi);
        painter.DrawScrollButton(context, true); Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(10, 10).ToArgb());
        g.Clear(Color.Magenta); painter.DrawScrollButton(context with { IsBodyScrollButton = true }, true);
        Assert.NotEqual(Color.Lime.ToArgb(), image.GetPixel(10, 10).ToArgb());
    });

    [Fact]
    public void Review_settings_are_per_ribbon_and_rejected_geometry_preserves_the_selected_preset() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl();
        var application = RibbonThemes.Office2024Light with { HeaderScrollButtonWidth = 60, OptionsButtonCornerRadius = 8, ApplicationMenuWorkAreaInset = 12 };
        ribbon.Theme = application; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light;
        foreach (var invalid in new[]
        {
            ribbon.Theme with { HeaderScrollButtonWidth = 0 }, ribbon.Theme with { HeaderScrollButtonWidth = 1025 },
            ribbon.Theme with { OptionsButtonCornerRadius = -1 }, ribbon.Theme with { OptionsButtonCornerRadius = 65 },
            ribbon.Theme with { ApplicationMenuWorkAreaInset = -1 }, ribbon.Theme with { ApplicationMenuWorkAreaInset = 1025 }
        }) Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = invalid);
        Assert.Same(RibbonThemes.Office2019Light, ribbon.Theme);
        Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme);
        ribbon.BuiltInTheme = null; Assert.Same(application, ribbon.Theme);
        Assert.Null(other.Theme.OptionsButtonCornerRadius); Assert.Null(other.Theme.HeaderScrollButtonWidth);
        Assert.Null(other.Theme.ApplicationMenuWorkAreaInset); Assert.True(other.Theme.MessageSideBordersVisible);
        Assert.True(other.Theme.ApplicationMenuSplitDividerVisible);
    });

    private static IEnumerable<Control> Descendants(Control parent)
    { foreach (Control child in parent.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
}
