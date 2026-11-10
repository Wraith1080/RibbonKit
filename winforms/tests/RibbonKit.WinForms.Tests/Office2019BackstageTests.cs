using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2019BackstageTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Classic_navigation_has_full_width_rows_and_reference_spacing(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light };
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info" });
        ribbon.Backstage.FooterItems.Add(new RibbonButtonItem { Text = "Options" });
        using var font = new Font("Segoe UI", 10.5f * dpi / 96);
        var layout = RibbonBackstageLayout.Build(new(S(1000), S(600)), font, dpi, ribbon.Backstage);
        Assert.Equal(S(220), layout.Page.Left); Assert.Equal(0, layout.Page.Top);
        Assert.Equal(new Rectangle(S(20), S(20), S(36), S(36)), layout.Back);
        var rows = layout.Arrange(ribbon.Backstage);
        Assert.All(rows, row => { Assert.Equal(0, row.Bounds.Left); Assert.Equal(layout.Page.Left, row.Bounds.Right); });
        Assert.Equal(S(14), layout.Footer.Bottom - rows.Last().Bounds.Bottom);
        using var painter = new RibbonOffice2024Renderer();
        Assert.Equal(0, painter.GetBackstagePageBorderWidth(ribbon.Theme, dpi));
        Assert.Equal(layout.Page, layout.PageContentBounds());
        ribbon.Backstage.NavigationWidth = 260;
        Assert.Equal(S(260), RibbonBackstageLayout.Build(new(S(1000), S(600)), font, dpi, ribbon.Backstage).Page.Left);
        Assert.True(RibbonBackstageLayout.Build(new(S(300), S(110)), font, dpi, ribbon.Backstage).FooterInline);
    });

    [Theory] [InlineData(false, 96)] [InlineData(true, 96)] [InlineData(false, 192)] [InlineData(true, 192)]
    public void Classic_chrome_uses_accent_white_ink_and_full_width_states(bool office, int dpi) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        var theme = RibbonThemes.Office2019Light with { Accent = Color.Purple };
        var icon = new IconProbe(); using var item = new RibbonButtonItem { Text = "Info", Icon = icon };
        using var image = new Bitmap(450, 160); using var g = Graphics.FromImage(image);
        var bounds = new Rectangle(0, 0, 300, 80); var context = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
        foreach (bool selected in new[] { false, true })
        {
            painter.DrawBackstageEntry(context, item, selected);
            Assert.Equal(Color.White, icon.Color);
            Assert.Equal(RibbonLayoutEngine.Scale(16, dpi), icon.Bounds.Width);
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(RibbonLayoutEngine.Scale(26, dpi), 40).ToArgb());
            var expected = selected ? theme.BackstageSelectedFill.Start : theme.Accent;
            Assert.Equal(expected.ToArgb(), image.GetPixel(0, 40).ToArgb());
            Assert.Equal(expected.ToArgb(), image.GetPixel(299, 40).ToArgb());
            Assert.Equal(expected.ToArgb(), image.GetPixel(2, 40).ToArgb()); // No Modern selection marker.
        }
        painter.DrawBackstageEntry(context with { Hot = true }, item, false);
        Assert.Equal(theme.BackstageHoverFill.Start.ToArgb(), image.GetPixel(0, 40).ToArgb());
        item.Enabled = false; painter.DrawBackstageEntry(context, item, false);
        Assert.Equal(theme.BackstageDisabledForeground, icon.Color);
        painter.DrawBackstageEntry(context with { Focused = true }, null, false);
        int backSide = 80 - 2 * RibbonLayoutEngine.Scale(2, dpi);
        Assert.Equal(theme.Accent.ToArgb(), image.GetPixel((300 - backSide) / 2, (80 - backSide) / 2).ToArgb());
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(150, (80 - backSide) / 2).ToArgb());
        painter.DrawBackstageBackground(context with { Bounds = new(0, 0, 450, 160) }, 300);
        Assert.Equal(theme.Accent.ToArgb(), image.GetPixel(0, 0).ToArgb());
        Assert.Equal(theme.Surface.ToArgb(), image.GetPixel(300, 0).ToArgb());
    });

    [Fact]
    public void Open_Backstage_fills_the_entire_caption_and_centers_the_actual_title() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Text = "Centered Backstage", ClientSize = new(1000, 600), TitleBarColor = Color.Purple };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.None };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        using var page = new TextBox { Text = "Native page", Dock = DockStyle.Fill, BackColor = Color.LemonChiffon };
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); form.Activate(); Application.DoEvents();
        // Put the pane boundary through the title to verify each side's ink
        // honors its graphics clip while preserving whole-window centering.
        ribbon.Backstage.MaximumNavigationWidth = 900;
        ribbon.Backstage.NavigationWidth = (int)Math.Round(form.ClientSize.Width * 48d / ribbon.DeviceDpi);
        var before = form.CaptionTextBounds(ribbon); var handle = page.Handle;
        ribbon.IsBackstageOpen = true; Application.DoEvents();
        var text = form.CaptionTextBounds(ribbon);
        Assert.Equal(form.ClientSize.Width - text.Right, text.Left);
        Assert.True(text.Right <= form.Caption.Buttons.Left);
        Assert.Equal(form.ClientSize.Width, ribbon.CaptionOverlayBounds.Width);
        using var image = new Bitmap(form.ClientSize.Width, form.CaptionHeight); using var g = Graphics.FromImage(image);
        // The caption underlay stays ordinary. Actual full-height pane drawing,
        // with a transparent foreground bitmap above it, supplies the scene.
        ribbon.PaintCaptionBackground(g, new(Point.Empty, image.Size), false);
        Assert.Equal(Color.Purple.ToArgb(), image.GetPixel(image.Width - 1, 2).ToArgb());
        ribbon.Painter.DrawBackstageBackground(new(g, new(0, 0, form.ClientSize.Width, form.ClientSize.Height), ribbon.Theme, form.Font, form.DeviceDpi), ribbon.BackstageSurface!.NavigationBounds.Width);
        int split = ribbon.CaptionPageBounds.Left;
        Assert.Equal(ribbon.BackstageSurface!.Left + ribbon.BackstageSurface.NavigationBounds.Right, split);
        Assert.Equal(ribbon.Theme.BackstageNavigationColor.ToArgb(), image.GetPixel(split - 1, 2).ToArgb());
        Assert.Equal(ribbon.Theme.Surface.ToArgb(), image.GetPixel(split + ribbon.Painter.GetBackstagePageCornerRadius(ribbon.Theme, ribbon.DeviceDpi) + 2, 2).ToArgb());
        Assert.Equal(ribbon.Theme.Surface.ToArgb(), image.GetPixel(image.Width - 1, 2).ToArgb());
        using var foreground = ribbon.CaptureBackstageCaption(); Assert.NotNull(foreground);
        Assert.Equal(0, foreground.GetPixel(100, 2).A);
        g.DrawImageUnscaled(foreground, Point.Empty);
        int left = text.Right, right = text.Left;
        for (int y = 0; y < image.Height; y++) for (int x = text.Left; x < text.Right; x++)
            if (foreground.GetPixel(x, y).A > 0)
            { left = Math.Min(left, x); right = Math.Max(right, x); }
        Assert.True(right > left, "Caption title was not painted.");
        Assert.InRange(Math.Abs((left + right) / 2d - form.ClientSize.Width / 2d), 0, 8);
        Assert.Contains(Enumerable.Range(text.Left, split - text.Left), x => Enumerable.Range(0, image.Height).Any(y => image.GetPixel(x, y).ToArgb() == Color.White.ToArgb()));
        Assert.Equal(handle, page.Handle); Assert.Equal(Color.LemonChiffon, page.BackColor);
        ribbon.IsBackstageOpen = false;
        Assert.Equal(before, form.CaptionTextBounds(ribbon)); Assert.Equal(Color.Purple, form.TitleBarColor);
        Assert.Equal(handle, page.Handle); Assert.Equal("Native page", page.Text);
    });

    [Fact]
    public void Shared_overrides_validate_without_leaking_into_other_ribbons_or_options() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light };
        using var other = new RibbonControl();
        var selected = ribbon.Theme;
        foreach (var invalid in new[]
        {
            selected with { BackstageNavigationIconInset = -1 }, selected with { BackstageNavigationIconSize = 0 },
            selected with { BackstageAutoMinimumNavigationWidth = 0 }, selected with { BackstageFooterBottomPadding = -1 },
            selected with { BackstagePageBorderWidth = 33 }
        }) Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = invalid);
        Assert.Same(selected, ribbon.Theme);
        using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "classic-options");
        Assert.Equal(selected.BackstageNavigationBackground, dialog.Controls.Find("OptionsNavigation", true).Single().BackColor);
        var custom = selected with { BackstageNavigationUsesAccent = false, BackstageNavigationBackground = Color.Navy,
            BackstageNavigationHover = new(Color.Lime), BackstageNavigationSelected = new(Color.Pink), BackstageNavigationForeground = Color.Yellow,
            BackstageNavigationIconInset = 9, BackstageNavigationIconSize = 20, BackstageTitleCentered = false, BackstagePageBorderWidth = 2 };
        ribbon.Theme = custom;
        Assert.Equal(Color.Navy, ribbon.Theme.BackstageNavigationColor); Assert.Equal(Color.Lime, ribbon.Theme.BackstageHoverFill.Start);
        Assert.Equal(41, RibbonPresentationResolver.Backstage(custom).ResolveMetrics(custom, RibbonDensity.Compact).TextOffset);
        using (var painter = new RibbonRenderer())
        using (var image = new Bitmap(180, 100))
        using (var graphics = Graphics.FromImage(image))
        {
            painter.DrawBackstageBackground(new(graphics, new(0, 0, 180, 100), custom, ribbon.Font, 96), 80);
            Assert.Equal(custom.Border.ToArgb(), image.GetPixel(80, 0).ToArgb());
            Assert.Equal(custom.Surface.ToArgb(), image.GetPixel(82, 2).ToArgb());
        }
        Assert.False(other.Theme.BackstageFullWidthNavigation); Assert.False(other.Theme.BackstageExtendsIntoTitleBar);
        Assert.False(other.Theme.BackstageTitleCentered); Assert.Same(other.Theme, other.Theme.BackstageCaptionTheme);
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; ribbon.BuiltInTheme = null;
        Assert.Same(custom, ribbon.Theme);
    });

    private sealed class IconProbe : IRibbonIcon
    {
        internal Color Color; internal Rectangle Bounds;
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        { Bounds = bounds; Color = context.Foreground; using var brush = new SolidBrush(Color.Red); graphics.FillRectangle(brush, bounds); }
    }
}
