using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ApplicationMenuJoinTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Roomier_pane_padding_keeps_divider_header_and_navigation_on_one_boundary(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var theme = RibbonThemes.Office2024Light with { ApplicationMenuPaneHorizontalPadding = 22, Border = Color.Blue,
                Hover = new(Color.Magenta), GalleryHeadingBackground = new(Color.Cyan) };
            using var pixels = new Bitmap(S(600), S(560)); using var graphics = Graphics.FromImage(pixels);
            var nav = new Rectangle(S(5) + 1, S(5) + 1, S(150), S(450));
            var pane = new Rectangle(nav.Right + 1 + S(22), nav.Top + S(32), S(320), S(410));
            var context = new RibbonPaintContext(graphics, new(Point.Empty, pixels.Size), theme, SystemFonts.MessageBoxFont!, dpi);
            painter.DrawApplicationMenuBackground(context, nav, pane, Rectangle.Empty);
            painter.DrawMenuHeader(context with { Bounds = new(nav.Right + 1, nav.Top, pixels.Width - nav.Right - S(5) - 2, S(28)),
                ApplicationMenuPart = RibbonApplicationMenuPart.Pane }, "Open options");
            using var item = new RibbonButtonItem { Text = "Open" };
            var row = new Rectangle(nav.Left, nav.Top + S(64), nav.Width, S(64));
            painter.DrawItem(context with { Bounds = row, Hot = true, ApplicationMenuPart = RibbonApplicationMenuPart.Navigation }, item, RibbonItemSize.Medium, true);
            Capture(pixels, $"join-{office}-{dpi}.png");
            Assert.Equal(theme.Border.ToArgb(), pixels.GetPixel(nav.Right, row.Top + row.Height / 2).ToArgb());
            Assert.Equal(theme.GalleryHeadingBackground.Start.ToArgb(), pixels.GetPixel(nav.Right + 1, nav.Top + S(3)).ToArgb());
            Assert.Equal(theme.Hover.Start.ToArgb(), pixels.GetPixel(nav.Right - 2, row.Top + row.Height / 2).ToArgb());
        }
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Wider_navigation_fills_the_join_while_scrollers_and_clamped_content_keep_their_slots(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Density = density };
        var page = new RibbonDropDownItem { Text = "Open", DropDownHeader = "Open options" };
        ribbon.FileMenu.Items.Add(page); page.Items.Add(new RibbonButtonItem { Text = "Document" });
        for (int i = 0; i < 8; i++) ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Command " + i });
        int dpi = ribbon.DeviceDpi; int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        int bar = SystemInformation.GetVerticalScrollBarWidthForDpi(dpi);
        var rows = ribbon.Theme with { ApplicationMenuNavigationMinimumRowHeight = 64 };
        ribbon.Theme = rows;
        var original = RibbonApplicationMenuLayout.Build(ribbon, page, new(1200, 1400));
        foreach (int padding in new[] { 0, 10, 22, 1024 })
        foreach (var maximum in new[] { new Size(1200, 1400), new Size(680, 320), new Size(180, 220) })
        {
            ribbon.Theme = rows with { ApplicationMenuPaneHorizontalPadding = padding };
            var layout = RibbonApplicationMenuLayout.Build(ribbon, page, maximum);
            bool scroll = layout.NavigationExtent > layout.Navigation.Height;
            Assert.Equal(layout.Navigation.Right + (scroll ? bar : 0), layout.NavigationDividerX);
            Assert.Equal(layout.NavigationDividerX + 1, layout.Header.Left);
            Assert.All(layout.Targets.Where(t => ribbon.FileMenu.Items.Contains((RibbonItem)t.Element!)), t => Assert.Equal(layout.Navigation.Right, t.Bounds.Right));
            Assert.True(layout.Pane.Left >= layout.Header.Left); Assert.True(layout.Pane.Right < layout.Frame.Right);
            Assert.True(layout.Size.Width <= maximum.Width); Assert.True(layout.Size.Height <= maximum.Height);
            if (padding == 22 && maximum.Height == 1400)
            {
                Assert.Equal(original.Navigation.Width + S(22) - S(10), layout.Navigation.Width);
                Assert.Equal(original.Pane.Width, layout.Pane.Width);
                Assert.Equal(layout.NavigationDividerX + 1 + S(22), layout.Pane.Left);
            }
            using var pixels = new Bitmap(layout.Size.Width, layout.Size.Height); using var g = Graphics.FromImage(pixels);
            var c = new RibbonPaintContext(g, new(Point.Empty, pixels.Size), ribbon.Theme, ribbon.Font, dpi)
            { Density = density, ApplicationMenuDividerX = layout.NavigationDividerX };
            ribbon.Painter.DrawApplicationMenuBackground(c, layout.Navigation, layout.Pane, layout.Footer);
            ribbon.Painter.DrawMenuHeader(c with { Bounds = layout.Header, ApplicationMenuPart = RibbonApplicationMenuPart.Pane }, page.DropDownHeader);
            var target = layout.Targets.First(t => t.Element == page);
            ribbon.Painter.DrawItem(c with { Bounds = target.Bounds, Hot = true, ApplicationMenuPart = RibbonApplicationMenuPart.Navigation }, page, RibbonItemSize.Medium, true);
            int y = Math.Max(layout.Header.Bottom + 1, layout.Navigation.Top + layout.Navigation.Height / 2);
            Assert.Equal(ribbon.Theme.Border.ToArgb(), pixels.GetPixel(layout.NavigationDividerX, y).ToArgb());
            Capture(pixels, $"native-{density}-{padding}-{maximum.Width}x{maximum.Height}.png");
        }
    });

    private static void Capture(Bitmap pixels, string name)
    {
        if (Environment.GetEnvironmentVariable("RIBBONKIT_APPLICATION_JOIN_CAPTURE") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory); pixels.Save(Path.Combine(directory, name));
    }
}
