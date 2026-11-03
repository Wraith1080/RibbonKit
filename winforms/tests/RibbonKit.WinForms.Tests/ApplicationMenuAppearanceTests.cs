using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ApplicationMenuAppearanceTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Scrolled_row_text_stays_inside_the_viewport(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        using var item = new RibbonButtonItem { Text = "Visible clipped command" };
        using var image = new Bitmap(240, 80); using var graphics = Graphics.FromImage(image);
        var viewport = new Rectangle(0, 0, 240, 35);
        foreach (var part in new[] { RibbonApplicationMenuPart.Navigation, RibbonApplicationMenuPart.Pane, RibbonApplicationMenuPart.Footer })
        {
            graphics.ResetClip(); graphics.Clear(Color.White); graphics.SetClip(viewport);
            // Application Menu uses an opaque popup, including over Mica/Acrylic hosts.
            painter.DrawItem(new RibbonPaintContext(graphics, new(0, 15, 240, 40), new RibbonTheme(), SystemFonts.MessageBoxFont!, 96)
                { ApplicationMenuPart = part }, item, RibbonItemSize.Medium, true);
            Assert.Contains(Enumerable.Range(0, viewport.Bottom).SelectMany(y => Enumerable.Range(0, image.Width).Select(x => image.GetPixel(x, y))), pixel => pixel.ToArgb() != Color.White.ToArgb());
            for (int y = viewport.Bottom; y < image.Height; y++) for (int x = 0; x < image.Width; x++) Assert.Equal(Color.White.ToArgb(), image.GetPixel(x, y).ToArgb());
        }
    });

    [Theory]
    [InlineData(false, 96)] [InlineData(true, 96)] [InlineData(false, 120)] [InlineData(true, 120)]
    [InlineData(false, 144)] [InlineData(true, 144)] [InlineData(false, 192)] [InlineData(true, 192)]
    public void Split_row_hover_is_uniform_and_only_unhovered_selection_dims_the_primary(bool office, int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var form = new Form { Width = 900 }; var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon); ribbon.FileMenu.Visible = true;
        var split = new RibbonSplitButtonItem(); ribbon.FileMenu.Items.Add(split);
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer(); var theme = new RibbonTheme();
        form.Show(); Application.DoEvents();
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            ribbon.Density = density; ribbon.FileMenu.ShowDropDown(); split.ShowDropDown(); Assert.True(split.IsDropDownOpen);
            var bounds = new Rectangle(S(4), S(4), S(180), S(density == RibbonDensity.Touch ? 44 : 40));
            var targets = RibbonApplicationMenuMetrics.Expand(new(TargetKind.Item, split, bounds, bounds, RibbonItemSize.Medium), dpi, density).ToArray();
            foreach (int hotPart in new[] { -1, 0, 1 })
            {
                using var pixels = new Bitmap(bounds.Right + S(4), bounds.Bottom + S(4)); using var graphics = Graphics.FromImage(pixels); graphics.Clear(theme.Surface);
                var context = new RibbonPaintContext(graphics, targets[0].Bounds, theme, ribbon.Font, dpi) { Hot = hotPart == 0, Density = density, ApplicationMenuPart = RibbonApplicationMenuPart.Navigation };
                painter.DrawSplitItem(context, context with { Bounds = targets[1].Bounds, Hot = hotPart == 1 }, split, RibbonItemSize.Medium, true);
                var main = pixels.GetPixel(targets[0].Bounds.Left + S(6), bounds.Top + bounds.Height / 2);
                var arrow = pixels.GetPixel(targets[1].Bounds.Left + S(3), bounds.Top + bounds.Height / 2);
                if (hotPart < 0)
                {
                    Assert.Equal(theme.Pressed.Start.ToArgb(), arrow.ToArgb());
                    Assert.True(main.R > arrow.R && main.G > arrow.G && main.B > arrow.B, $"Selected main={main}, arrow={arrow}");
                }
                else { Assert.Equal(theme.Hover.Start.ToArgb(), main.ToArgb()); Assert.Equal(main, arrow); }
                Assert.False(context.Pressed);
            }
            ribbon.FileMenu.CloseDropDown();
        }
        form.Close();
    });

    [Theory]
    [InlineData(false, 96)] [InlineData(true, 96)] [InlineData(false, 120)] [InlineData(true, 120)]
    [InlineData(false, 144)] [InlineData(true, 144)] [InlineData(false, 192)] [InlineData(true, 192)]
    public void Outer_inset_matches_default_pane_and_active_heading_has_a_shaded_band(bool office, int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        var theme = new RibbonTheme(); using var image = new Bitmap(S(510), S(380)); using var graphics = Graphics.FromImage(image);
        var nav = new Rectangle(S(5) + 1, S(5) + 1, S(150), S(320) - 2);
        var pane = new Rectangle(nav.Right + 1 + S(10), nav.Top + S(28) + S(4), S(320), S(270));
        var context = new RibbonPaintContext(graphics, new(Point.Empty, image.Size), theme, SystemFonts.MessageBoxFont!, dpi) { ApplicationMenuDefaultPane = true };
        painter.DrawApplicationMenuBackground(context, nav, pane, Rectangle.Empty);
        var background = image.GetPixel(pane.Left + S(50), pane.Bottom - S(10));
        Assert.Equal(background, image.GetPixel(S(2), nav.Top + nav.Height / 2));
        var defaultHeader = new Rectangle(pane.Left, nav.Top, pane.Width, S(28));
        painter.DrawMenuHeader(context with { Bounds = defaultHeader, ApplicationMenuPart = RibbonApplicationMenuPart.Pane }, "Recent documents");
        Assert.Equal(background, image.GetPixel(defaultHeader.Left + S(3), defaultHeader.Top + S(3)));
        context = context with { ApplicationMenuDefaultPane = false };
        painter.DrawApplicationMenuBackground(context, nav, pane, Rectangle.Empty);
        var activeHeader = new Rectangle(nav.Right + 1, nav.Top, image.Width - S(5) - 1 - nav.Right - 1, S(28));
        painter.DrawMenuHeader(context with { Bounds = activeHeader, ApplicationMenuPart = RibbonApplicationMenuPart.Pane }, "Preview and print the document");
        Assert.Equal(theme.GalleryHeadingBackground.Start.ToArgb(), image.GetPixel(activeHeader.Left + S(3), activeHeader.Top + S(3)).ToArgb());
        Assert.Equal(background, image.GetPixel(S(2), nav.Top + nav.Height / 2));
    });

    [Theory]
    [InlineData(false, 96)] [InlineData(true, 96)] [InlineData(false, 120)] [InlineData(true, 120)]
    [InlineData(false, 144)] [InlineData(true, 144)] [InlineData(false, 192)] [InlineData(true, 192)]
    public void Split_primary_and_arrow_paint_the_same_height_and_outer_inset(bool office, int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        using var split = new RibbonSplitButtonItem();
        var theme = new RibbonTheme { Pressed = new(Color.Magenta), PopupBorder = Color.Blue };
        foreach (var part in new[] { RibbonApplicationMenuPart.Navigation, RibbonApplicationMenuPart.Footer })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            var bounds = new Rectangle(S(4), S(4), S(180), S(density == RibbonDensity.Touch ? 44 : 40));
            var targets = RibbonApplicationMenuMetrics.Expand(new(TargetKind.Item, split, bounds, bounds, RibbonItemSize.Medium), dpi, density).ToArray();
            Assert.Equal(targets[0].Bounds.Top, targets[1].Bounds.Top); Assert.Equal(targets[0].Bounds.Bottom, targets[1].Bounds.Bottom);
            using var pixels = new Bitmap(bounds.Right + S(4), bounds.Bottom + S(4)); using var graphics = Graphics.FromImage(pixels); graphics.Clear(theme.Surface);
            var context = new RibbonPaintContext(graphics, targets[0].Bounds, theme, SystemFonts.MessageBoxFont!, dpi, Pressed: true) { Density = density, ApplicationMenuPart = part };
            painter.DrawSplitItem(context, context with { Bounds = targets[1].Bounds }, split, RibbonItemSize.Medium, true);
            (int Top, int Bottom) Ink(int x)
            {
                var ys = Enumerable.Range(0, pixels.Height).Where(y => pixels.GetPixel(x, y).ToArgb() == Color.Magenta.ToArgb()).ToArray();
                Assert.NotEmpty(ys); return (ys[0], ys[^1]);
            }
            var primary = Ink(targets[0].Bounds.Left + targets[0].Bounds.Width / 2);
            var arrow = Ink(targets[1].Bounds.Left + targets[1].Bounds.Width / 2);
            if (primary != arrow)
            {
                string directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/application-menu/visual-refinement/split-mismatch"));
                Directory.CreateDirectory(directory); pixels.Save(Path.Combine(directory, $"{office}-{dpi}-{part}-{density}.png"));
            }
            Assert.Equal(primary, arrow);
            int stroke = part == RibbonApplicationMenuPart.Footer ? 1 : 0;
            Assert.Equal(bounds.Top + 1 + stroke, primary.Top); Assert.Equal(bounds.Bottom - 2 - stroke, primary.Bottom);
        }
    });

    [Theory]
    [InlineData(false, 96)] [InlineData(true, 96)] [InlineData(false, 120)] [InlineData(true, 120)]
    [InlineData(false, 144)] [InlineData(true, 144)] [InlineData(false, 192)] [InlineData(true, 192)]
    public void Inner_frame_has_continuous_hairlines_and_matching_four_corner_strokes(bool office, int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        var theme = new RibbonTheme { Border = Color.DarkMagenta };
        using var image = new Bitmap(S(510), S(380)); using var graphics = Graphics.FromImage(image); graphics.Clear(theme.PopupColor);
        var nav = new Rectangle(S(5) + 1, S(5) + 1, S(150), S(320) - 2);
        var pane = new Rectangle(nav.Right + 1 + S(10), nav.Top + S(28), S(320), S(280));
        var footer = new Rectangle(S(5), S(338), S(500), S(30));
        var context = new RibbonPaintContext(graphics, new(Point.Empty, image.Size), theme, SystemFonts.MessageBoxFont!, dpi) { ApplicationMenuDefaultPane = true };
        painter.DrawApplicationMenuBackground(context, nav, pane, footer);
        var frame = RibbonRenderer.ApplicationFrame(context.Bounds, nav); int color = theme.Border.ToArgb();
        Assert.Equal(color, image.GetPixel(frame.Left + frame.Width / 2, frame.Top).ToArgb());
        Assert.Equal(color, image.GetPixel(frame.Left + frame.Width / 2, frame.Bottom - 1).ToArgb());
        Assert.Equal(color, image.GetPixel(frame.Left, frame.Top + frame.Height / 2).ToArgb());
        Assert.Equal(color, image.GetPixel(frame.Right - 1, frame.Top + frame.Height / 2).ToArgb());
        for (int y = 0; y < S(6); y++) for (int x = 0; x < S(6); x++)
        {
            bool stroke = image.GetPixel(frame.Left + x, frame.Top + y).ToArgb() == color;
            Assert.Equal(stroke, image.GetPixel(frame.Right - 1 - x, frame.Top + y).ToArgb() == color);
            Assert.Equal(stroke, image.GetPixel(frame.Left + x, frame.Bottom - 1 - y).ToArgb() == color);
            Assert.Equal(stroke, image.GetPixel(frame.Right - 1 - x, frame.Bottom - 1 - y).ToArgb() == color);
        }
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Footer_buttons_align_to_inner_frame_and_navigation_recent_and_rich_rows_have_distinct_measurements(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Density = density };
        var nav = new RibbonButtonItem { Text = "Save" }; ribbon.FileMenu.Items.Add(nav);
        var recent = new RibbonButtonItem { Text = "1  Report.docx" }; ribbon.ApplicationMenu.DefaultPane.Items.Add(recent);
        ribbon.ApplicationMenu.FooterItems.AddRange(new RibbonButtonItem { Text = "Options" }, new RibbonButtonItem { Text = "Exit" }, new RibbonSplitButtonItem { Text = "Save As" });
        using var surface = new RibbonPopupSurface(ribbon, ribbon.FileMenu, new(1200, 800)); var layout = surface.ApplicationLayout!;
        Assert.Equal(layout.Frame.Right, surface.Targets.Last(t => t.Clip == layout.Footer).Bounds.Right);
        Assert.True(layout.Footer.Top > layout.Frame.Bottom);
        Assert.Equal(RibbonApplicationMenuMetrics.ArrowWidth(ribbon.DeviceDpi, density), surface.Targets.Last(t => t.DropDownPart).Bounds.Width);
        Assert.True(layout.Pane.Left > layout.Navigation.Right); Assert.True(layout.Header.IsEmpty);
        int simple = RibbonApplicationMenuMetrics.RowHeight(recent, 280, ribbon.Font, ribbon.DeviceDpi, density, false);
        int navigation = RibbonApplicationMenuMetrics.RowHeight(nav, 150, ribbon.Font, ribbon.DeviceDpi, density, true);
        if (density == RibbonDensity.Compact) Assert.True(simple < navigation);
        using var pane = new RibbonDropDownItem { MenuPresentation = RibbonMenuPresentation.IllustratedChoices };
        var rich = new RibbonButtonItem { Text = "Word Template", MenuDescription = "Save the document as a template that can be used to format future documents." }; pane.Items.Add(rich);
        Assert.True(RibbonApplicationMenuMetrics.RowHeight(rich, 260, ribbon.Font, ribbon.DeviceDpi, density, false) > simple);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Rich_icon_slot_is_32_logical_pixels_and_recent_text_uses_the_pane_edge(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var renderer = new Recorder(); using var image = new Bitmap(S(340), S(120)); using var graphics = Graphics.FromImage(image);
        using var pane = new RibbonDropDownItem { MenuPresentation = RibbonMenuPresentation.IllustratedChoices };
        var item = new RibbonButtonItem { Text = "Word Document", Icon = RibbonStockIcons.Create("save"), MenuDescription = "Save in the default file format." }; pane.Items.Add(item);
        renderer.DrawItem(new RibbonPaintContext(graphics, new(0, 0, image.Width, S(60)), new RibbonTheme(), SystemFonts.MessageBoxFont!, dpi) { ApplicationMenuPart = RibbonApplicationMenuPart.Pane }, item, RibbonItemSize.Medium, true);
        Assert.Equal(S(32), renderer.Icon.Width); Assert.True(renderer.Title.Left >= renderer.Icon.Right + S(8));
        renderer.DrawItem(new RibbonPaintContext(graphics, new(0, S(64), image.Width, S(28)), new RibbonTheme(), SystemFonts.MessageBoxFont!, dpi) { ApplicationMenuPart = RibbonApplicationMenuPart.Pane }, new RibbonButtonItem { Text = "Report" }, RibbonItemSize.Medium, true);
        Assert.Equal(S(6), renderer.Title.Left);
    });
    private sealed class Recorder : RibbonOffice2024Renderer
    {
        internal Rectangle Icon, Title;
        protected override void DrawItemImage(RibbonPaintContext context, RibbonItem item, RibbonItemSize size) { Icon = context.Bounds; base.DrawItemImage(context, item, size); }
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { if (text is "Word Document" or "Report") Title = context.Bounds; base.DrawText(context, text, color, flags); }
    }
}
