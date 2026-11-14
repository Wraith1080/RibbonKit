using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2010ApplicationMenuTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Footer_keeps_hover_and_press_fills_but_gains_idle_depth_and_state_outlines(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new InkOnly(); using var button = new RibbonButtonItem();
        using var image = new Bitmap(S(180), S(58)); using var g = Graphics.FromImage(image);
        var theme = RibbonThemes.Office2010Blue; var bounds = new Rectangle(S(8), S(8), S(160), S(40));
        var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi) { ApplicationMenuPart = RibbonApplicationMenuPart.Footer };
        foreach (int state in new[] { 0, 1, 2 })
        {
            c = c with { Hot = state == 1, Pressed = state == 2 }; g.Clear(Color.White); painter.DrawItem(c, button, RibbonItemSize.Medium, true);
            var border = image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + 1);
            Assert.Equal((state == 0 ? theme.PopupBorder : state == 1 ? theme.ApplicationMenuAppearance!.FooterStateChrome!.HoverBorderColor : theme.ApplicationMenuAppearance!.FooterStateChrome!.PressedBorderColor).ToArgb(), border.ToArgb());
            var top = image.GetPixel(bounds.Right - S(12), bounds.Top + S(8)); var bottom = image.GetPixel(bounds.Right - S(12), bounds.Bottom - S(8));
            Assert.NotEqual(top, bottom);
            using var before = new Bitmap(image.Width, image.Height); using var bg = Graphics.FromImage(before); bg.Clear(Color.White);
            painter.DrawItem(c with { Graphics = bg, Theme = theme with { ApplicationMenuAppearance = null } }, button, RibbonItemSize.Medium, true);
            for (int y = bounds.Top + S(6); y < bounds.Bottom - S(6); y++)
                if (state != 0) Assert.Equal(before.GetPixel(bounds.Right - S(12), y), image.GetPixel(bounds.Right - S(12), y));
            if (state == 0) Assert.NotEqual(before.GetPixel(bounds.Right - S(12), bounds.Bottom - S(8)), bottom);
        }
        button.Enabled = false; g.Clear(Color.White); painter.DrawItem(c with { Hot = true }, button, RibbonItemSize.Medium, true);
        Assert.Equal(theme.PopupBorder.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + 1).ToArgb());
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Default_heading_reveals_its_page_and_active_heading_and_footer_band_are_independent(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new InkOnly(); using var image = new Bitmap(S(510), S(380)); using var g = Graphics.FromImage(image);
        var theme = RibbonThemes.Office2010Blue; var a = theme.ApplicationMenuAppearance!;
        var nav = new Rectangle(S(5) + 1, S(5) + 1, S(150), S(320) - 2); var pane = new Rectangle(nav.Right + S(11), nav.Top + S(32), S(320), S(270)); var footer = new Rectangle(S(5), S(338), S(500), S(30));
        var c = new RibbonPaintContext(g, new(Point.Empty, image.Size), theme, Control.DefaultFont, dpi) { ApplicationMenuDefaultPane = true };
        var heading = new Rectangle(pane.Left, nav.Top, pane.Width, S(28));
        void Paint(RibbonTheme value, bool defaultPane)
        { c = c with { Theme = value, ApplicationMenuDefaultPane = defaultPane }; painter.DrawApplicationMenuBackground(c, nav, pane, footer); painter.DrawMenuHeader(c with { Bounds = heading, ApplicationMenuPart = RibbonApplicationMenuPart.Pane }, "Recent documents"); }
        Paint(theme, true); Assert.Equal(a.DefaultPaneBackground!.Value.Start.ToArgb(), image.GetPixel(heading.Left + S(3), heading.Top + S(3)).ToArgb());
        Assert.NotEqual(image.GetPixel(image.Width / 2, S(4)), image.GetPixel(image.Width / 2, image.Height - S(6)));
        Paint(theme, false); Assert.Equal(a.ActiveHeaderBackground!.Value.Start.ToArgb(), image.GetPixel(heading.Left + S(3), heading.Top + S(3)).ToArgb());
        Paint(theme with { ApplicationMenuAppearance = a with { DefaultPaneBackground = new(Color.Lime), DefaultHeaderBackground = new(Color.Transparent), FooterSurfaceBackground = new(Color.Red), SurroundRimWidth = 0 } }, true);
        Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(heading.Left + S(3), heading.Top + S(3)).ToArgb()); Assert.Equal(Color.Red.ToArgb(), image.GetPixel(S(2), image.Height - S(3)).ToArgb());
        Paint(theme with { ApplicationMenuAppearance = a with { DefaultHeaderBackground = null } }, true);
        Assert.Equal(theme.MenuHeadingBackground!.Value.Start.ToArgb(), image.GetPixel(heading.Left + S(3), heading.Top + S(3)).ToArgb());
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Open_split_uses_dim_hover_primary_full_hover_arrow_and_two_tone_divider(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new InkOnly(); using var form = new Form { Bounds = new(-16000, -16000, 900, 600), ShowInTaskbar = false };
        using var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon); ribbon.FileMenu.Visible = true;
        var split = new RibbonSplitButtonItem(); split.Items.Add(new RibbonButtonItem()); ribbon.FileMenu.Items.Add(split); form.Show(); Application.DoEvents(); ribbon.FileMenu.ShowDropDown(); split.ShowDropDown(); Assert.True(split.IsDropDownOpen);
        using var image = new Bitmap(S(280), S(80)); using var g = Graphics.FromImage(image); var theme = RibbonThemes.Office2010Blue; var a = theme.ApplicationMenuAppearance!;
        var bounds = new Rectangle(S(8), S(8), S(260), S(60)); var targets = RibbonApplicationMenuMetrics.Expand(new(TargetKind.Item, split, bounds, bounds), dpi, RibbonDensity.Compact).ToArray();
        var c = new RibbonPaintContext(g, targets[0].Bounds, theme, Control.DefaultFont, dpi) { ApplicationMenuPart = RibbonApplicationMenuPart.Navigation }; var arrow = c with { Bounds = targets[1].Bounds };
        int x = bounds.Left + S(12), ax = arrow.Bounds.Left + arrow.Bounds.Width / 2, y = bounds.Top + bounds.Height / 2;
        void Draw(RibbonTheme value, bool mainHot = false, bool arrowHot = false, bool press = false) { g.Clear(a.NavigationBackground!.Value.Start); painter.DrawSplitItem(c with { Theme = value, Hot = mainHot, Pressed = press }, arrow with { Theme = value, Hot = arrowHot }, split, RibbonItemSize.Medium, true); }
        Draw(theme); var main = image.GetPixel(x, y); var drop = image.GetPixel(ax, y); Assert.True(main.B > drop.B); Assert.NotEqual(main, drop);
        var m = new RibbonApplicationMenuChromeMetrics(theme, dpi, RibbonPresentationResolver.ApplicationMenuChrome); var divider = m.SplitDivider(bounds, arrow.Bounds);
        Assert.NotEqual(image.GetPixel(divider.Left, y), image.GetPixel(divider.Right - 1, y)); Assert.Equal(a.SplitDividerColor.ToArgb(), image.GetPixel(divider.Right - 1, y).ToArgb());
        Draw(theme, true); Assert.Equal(image.GetPixel(x, y), image.GetPixel(ax, y)); Assert.Equal(drop, image.GetPixel(ax, y));
        Draw(theme, false, true); Assert.Equal(image.GetPixel(x, y), image.GetPixel(ax, y));
        Draw(theme, true, false, true); Assert.NotEqual(image.GetPixel(x, y), image.GetPixel(ax, y)); Assert.Equal(drop, image.GetPixel(ax, y));
        Draw(theme with { ApplicationMenuAppearance = a with { SplitDividerColor = Color.Transparent } }); Assert.NotEqual(a.SplitDividerColor.ToArgb(), image.GetPixel(divider.Right - 1, y).ToArgb());
        Draw(theme with { ApplicationMenuSplitDividerVisible = false }); Assert.Equal(drop, image.GetPixel(divider.Left, y));
        form.Close();
    });

    [Fact]
    public void Explicit_ink_and_zero_width_win_and_invalid_roles_do_not_mutate_the_theme() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue }; var original = ribbon.Theme; var a = original.ApplicationMenuAppearance!;
        foreach (var invalid in new[] { a with { SurroundRimWidth = -1 }, a with { SurroundRimInset = 1025 }, a with { SplitDividerHighlightWidth = 33 }, a with { RowStateChrome = new() { InnerBorderWidth = 33 } }, a with { FooterStateChrome = new() { InnerBorderInset = -1 } } })
        { Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { ApplicationMenuAppearance = invalid }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2010Blue, ribbon.BuiltInTheme); }
        using var painter = new InkOnly(); using var item = new RibbonButtonItem(); using var image = new Bitmap(200, 60); using var g = Graphics.FromImage(image);
        var c = new RibbonPaintContext(g, new(8, 8, 180, 40), original with { ApplicationMenuAppearance = a with { RowBorderColor = Color.Magenta, RowCornerRadius = 0, RowStateChrome = a.RowStateChrome! with { InnerBorderWidth = 0 } } }, Control.DefaultFont, 96, Hot: true) { ApplicationMenuPart = RibbonApplicationMenuPart.Navigation };
        painter.DrawItem(c, item, RibbonItemSize.Medium, true); Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(98, 9).ToArgb());
        painter.DrawItem(c with { Theme = c.Theme with { ApplicationMenuAppearance = c.Theme.ApplicationMenuAppearance! with { RowBorderWidth = 0 } } }, item, RibbonItemSize.Medium, true); Assert.NotEqual(Color.Magenta.ToArgb(), image.GetPixel(98, 9).ToArgb());
        ribbon.Theme = original with { ApplicationMenuAppearance = a with { FooterBackground = null, RowStateChrome = null } }; Assert.Null(ribbon.Theme.ApplicationMenuAppearance!.FooterBackground); Assert.NotNull(ribbon.Theme.ApplicationMenuAppearance.ActiveHeaderBackground);
        ribbon.Theme = original with { ApplicationMenuAppearance = null }; Assert.Null(ribbon.Theme.ApplicationMenuAppearance);
    });

    private sealed class InkOnly : RibbonOffice2024Renderer
    { public override void DrawText(RibbonPaintContext c, string text, Color ink, TextFormatFlags flags) { } protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { } public override void DrawSubmenuArrow(RibbonPaintContext c, Color ink) { } }
}
