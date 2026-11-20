using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ApplicationMenuCaptionPlacementTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Frame_clears_caption_cutout_while_orb_overlap_and_content_position_are_retained(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            var theme = RibbonThemes.Office2007Blue; var a = theme.ApplicationButtonAppearance!;
            int captionHeight = S(density == RibbonDensity.Touch ? 48 : 36);
            var orb = RibbonApplicationButtonMetrics.Measure(a, density, dpi, captionHeight, S(32), S(1000));
            int shadow = RibbonApplicationButtonMetrics.ShadowPadding(a, dpi);
            var composition = new RibbonApplicationMenuComposition(Rectangle.Inflate(orb, shadow, shadow), new(0, 0, S(1000), captionHeight));
            var requested = new Point(orb.Left + S(a.MenuOffset.X), orb.Top + S(a.MenuOffset.Y));
            Assert.True(requested.Y < captionHeight); // The old frame began inside the excluded caption.
            var origin = composition.BelowCaptionOrigin(requested, new(0, 0, S(1200), S(900)));
            Assert.Equal(requested.X, origin.X); Assert.Equal(captionHeight, origin.Y);
            Assert.True(origin.Y < orb.Bottom); // The real Orb still overlaps the frame.
            int minimum = S(a.MenuTopBandHeight);
            Assert.Equal(requested.Y + composition.TopClearance(requested.Y, minimum), origin.Y + composition.TopClearance(origin.Y, minimum));
            using var ribbon = new RibbonControl { Theme = theme, Density = density };
            for (int i = 0; i < 8; i++) ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Command " + i });
            ribbon.ApplicationMenu.DefaultPane.DropDownHeader = "Recent documents";
            ribbon.ApplicationMenu.DefaultPane.Items.Add(new RibbonButtonItem { Text = "Report" });
            ribbon.ApplicationMenu.FooterItems.Add(new RibbonButtonItem { Text = "Options" });
            var beforeLayout = RibbonApplicationMenuLayout.Build(ribbon, ribbon.ApplicationMenu.DefaultPane, new(S(700), S(900) - requested.Y), composition.TopClearance(requested.Y, minimum));
            var afterLayout = RibbonApplicationMenuLayout.Build(ribbon, ribbon.ApplicationMenu.DefaultPane, new(S(700), S(900) - origin.Y), composition.TopClearance(origin.Y, minimum));
            Assert.Equal(beforeLayout.Size.Width, afterLayout.Size.Width);
            Assert.Equal(beforeLayout.Size.Height + requested.Y, afterLayout.Size.Height + origin.Y);
            Assert.Equal(beforeLayout.Targets.Select(t => ScreenBounds(t.Bounds, requested)), afterLayout.Targets.Select(t => ScreenBounds(t.Bounds, origin)));
            using var painter = new RibbonOffice2024Renderer();
            using var before = Paint(requested); using var after = Paint(origin);
            Assert.Equal(0, before.GetPixel(before.Width / 2, 0).A);
            Assert.Equal(theme.ApplicationMenuAppearance!.OuterBorderColor.ToArgb(), after.GetPixel(after.Width / 2, 0).ToArgb());
            using var exclusion = composition.Exclusion(new(origin, after.Size));
            var center = new Point(orb.Left + orb.Width / 2 - origin.X, Math.Max(0, orb.Top + orb.Height / 2 - origin.Y));
            Assert.True(exclusion.IsVisible(center));
            var output = Environment.GetEnvironmentVariable("RIBBONKIT_CAPTION_MENU_EVIDENCE");
            if (!string.IsNullOrEmpty(output))
            {
                Directory.CreateDirectory(output);
                before.Save(Path.Combine(output, $"top-frame-before-{density}-{dpi}.png"));
                after.Save(Path.Combine(output, $"top-frame-after-{density}-{dpi}.png"));
            }
            Bitmap Paint(Point location)
            {
                var image = new Bitmap(S(500), S(320)); using var g = Graphics.FromImage(image);
                using var cutout = composition.Exclusion(new(location, image.Size)); g.ExcludeClip(cutout);
                painter.DrawPopupFrame(new(g, new(Point.Empty, image.Size), theme, Control.DefaultFont, dpi) { IsApplicationMenu = true });
                return image;
            }
            static Rectangle ScreenBounds(Rectangle bounds, Point location) { bounds.Offset(location); return bounds; }
        }
    });

    [Fact]
    public void Requested_offsets_plain_hosts_work_area_clamps_and_above_caption_fallback_keep_their_owners()
    {
        var work = new Rectangle(10, 10, 1000, 700);
        var composition = new RibbonApplicationMenuComposition(new(106, 118, 50, 50), new(100, 100, 900, 36));
        Assert.Equal(new Point(112, 160), composition.BelowCaptionOrigin(new(112, 160), work));
        Assert.Equal(new Point(112, 136), composition.BelowCaptionOrigin(new(112, 0), work));
        var plain = composition with { Caption = Rectangle.Empty };
        Assert.Equal(new Point(112, 126), plain.BelowCaptionOrigin(new(112, 126), work));
        Assert.Equal(new Point(112, work.Top), plain.BelowCaptionOrigin(new(112, -100), work));
        var bottom = new RibbonApplicationMenuComposition(new(106, 675, 50, 50), new(100, 655, 900, 40));
        var origin = bottom.BelowCaptionOrigin(new(106, 683), work);
        Assert.True(bottom.OpensAbove(origin, work, 18, 96, out int aboveBottom));
        var bounds = RibbonPopupManager.CalculateOrbMenuBounds(new(origin.X, aboveBottom), work, new(500, 400), above: true);
        Assert.True(work.Contains(bounds)); Assert.True(bounds.Bottom <= bottom.Caption.Top && bounds.Bottom <= bottom.Orb.Top);
        var outside = bottom with { Caption = new(100, 1000, 900, 40) };
        Assert.Equal(work.Bottom - 1, outside.BelowCaptionOrigin(new(106, 683), work).Y);
    }

    [Fact]
    public void Hidden_native_window_and_hosted_surface_keep_the_full_top_edge_outside_the_orb() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2007Blue };
        var composition = new RibbonApplicationMenuComposition(new(106, 122, 60, 60), new(100, 100, 900, 45));
        var oldOrigin = new Point(106, 132); var origin = composition.BelowCaptionOrigin(oldOrigin, new(0, 0, 1200, 900));
        foreach (var location in new[] { oldOrigin, origin })
        {
            using var popup = new RibbonPopupWindow(ribbon, () => { }) { AutoSize = false, Size = new(500, 350), Location = location, IsApplicationMenu = true, ApplicationComposition = composition };
            using var child = new Panel { Size = new(498, 348) };
            popup.Items.Add(new ToolStripControlHost(child) { AutoSize = false, Size = child.Size, Margin = Padding.Empty, Padding = Padding.Empty });
            popup.PerformLayout(); using var image = new Bitmap(popup.Width, popup.Height); popup.DrawToBitmap(image, popup.ClientRectangle);
            Assert.NotNull(popup.Region); Assert.NotNull(child.Region);
            Assert.Equal(location == origin, popup.Region.IsVisible(new Point(popup.Width / 2, 0)));
            Assert.Equal(location == origin, child.Region.IsVisible(new Point(child.Width / 2, 0)));
            Assert.False(popup.Region.IsVisible(popup.PointToClient(new(136, 155))));
        }
    });
}
