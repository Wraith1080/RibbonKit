using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class QuickAccessFrameTests
{
    [Fact]
    public void Joined_frame_keeps_command_geometry_and_renderer_changes_remeasure_the_row() => FoundationTests.Sta(() =>
    {
        using var neutral = new RibbonRenderer(); using var separate = new SeparateOffice();
        using var ribbon = new RibbonControl { Width = 900 };
        var tab = new RibbonTab(); var group = new RibbonGroup(); var command = new RibbonButtonItem { Text = "Save" };
        group.Items.Add(command); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = command });
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (var margins in new[] { new Padding(7, 1, 7, 7), new Padding(13, 3, 9, 11), Padding.Empty })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi);
            ribbon.IsMinimized = false; ribbon.Density = density; ribbon.Theme = ribbon.Theme with { BodyMargin = margins };
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; ribbon.Renderer = null;
            var body = ribbon.Snapshot.BodyBounds; var target = ribbon.Snapshot.Targets.Single(t => t.Element == command).Bounds; int height = ribbon.Height;
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
            var joined = ribbon.Snapshot;
            Assert.Equal(body, joined.BodyBounds); Assert.Equal(target, joined.Targets.Single(t => t.Element == command).Bounds);
            Assert.Equal(height + ribbon.QuickAccessHeight, ribbon.Height);
            Assert.Equal(body.Left, joined.QuickAccessBounds.Left); Assert.Equal(body.Right, joined.QuickAccessBounds.Right);
            Assert.Equal(body.Bottom, joined.QuickAccessBounds.Top); Assert.Equal(joined.QuickAccessBounds.Bottom, joined.BodyFrameBounds.Bottom);
            Assert.Equal(S(margins.Bottom), ribbon.Height - joined.BodyFrameBounds.Bottom);
            ribbon.IsMinimized = true;
            var card = ribbon.Snapshot;
            Assert.True(card.BodyBounds.IsEmpty); Assert.Equal(card.TabViewport.Bottom + S(margins.Top), card.QuickAccessBounds.Top);
            Assert.Equal(card.QuickAccessBounds.Bottom + S(margins.Bottom), ribbon.Height);
            foreach (var renderer in new RibbonRenderer[] { neutral, separate })
            {
                ribbon.Renderer = renderer;
                Assert.Equal(new Rectangle(0, ribbon.Snapshot.TabViewport.Bottom, ribbon.Width, ribbon.QuickAccessHeight), ribbon.Snapshot.QuickAccessBounds);
                Assert.Equal(ribbon.Snapshot.TabViewport.Bottom + ribbon.QuickAccessHeight, ribbon.Height);
                ribbon.Renderer = null;
                Assert.Equal(card.QuickAccessBounds, ribbon.Snapshot.QuickAccessBounds);
            }
            ribbon.QuickAccessVisible = false;
            Assert.True(ribbon.Snapshot.QuickAccessBounds.IsEmpty); Assert.Equal(ribbon.Snapshot.TabViewport.Bottom, ribbon.Height);
            ribbon.QuickAccessVisible = true;
        }
        using var mutable = new MutableOffice(); ribbon.Renderer = mutable;
        mutable.Join = false; ribbon.Renderer = mutable;
        Assert.Equal(ribbon.Width, ribbon.Snapshot.QuickAccessBounds.Width);
        Assert.Equal(ribbon.Snapshot.TabViewport.Bottom + ribbon.QuickAccessHeight, ribbon.Height);
    });

    [Fact]
    public void Below_row_scales_margins_and_clamps_narrow_widths_at_representative_dpi()
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int width in new[] { 0, 9, 39, 400, 401 })
        foreach (int row in new[] { 32, 48 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var client = new Size(S(width), S(250)); var margin = new Padding(13, 3, 9, 11);
            var bounds = RibbonQuickAccessLayout.BelowBounds(client, S(row), margin, true, dpi);
            Assert.Equal(Math.Min(S(13), client.Width), bounds.Left);
            Assert.InRange(bounds.Right, bounds.Left, client.Width);
            Assert.Equal(client.Height - S(11), bounds.Bottom); Assert.Equal(S(row), bounds.Height);
            Assert.Equal(new Rectangle(0, client.Height - S(row), client.Width, S(row)), RibbonQuickAccessLayout.BelowBounds(client, S(row), margin, false, dpi));
        }
    }

    [Fact]
    public void Joined_row_preserves_one_outer_frame_shadow_and_symmetric_corners() => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonOffice2024Renderer();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int odd in new[] { 0, 1 })
        foreach (bool shadow in new[] { false, true })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var theme = RibbonThemes.Office2024Light with { Background = Color.FromArgb(240, 244, 248), Surface = Color.FromArgb(255, 253, 249), BodyShadowColor = shadow ? Color.FromArgb(24, 0, 0, 0) : Color.Transparent };
            var frame = new Rectangle(S(10), S(10), S(260) + odd, S(180) + odd);
            var row = new Rectangle(frame.Left, frame.Bottom - S(32), frame.Width, S(32));
            using var expected = new Bitmap(frame.Right + S(10), frame.Bottom + S(12)); using var actual = new Bitmap(expected.Width, expected.Height);
            using (var g = Graphics.FromImage(expected)) { g.Clear(theme.Background); renderer.DrawBody(new(g, frame, theme, Control.DefaultFont, dpi)); }
            using (var g = Graphics.FromImage(actual))
            {
                g.Clear(theme.Background); renderer.DrawBody(new(g, frame, theme, Control.DefaultFont, dpi));
                renderer.DrawQuickAccessToolbar(new(g, row, theme, Control.DefaultFont, dpi) { IsQuickAccessToolbarJoined = true }, RibbonQuickAccessPosition.BelowRibbon);
            }
            // A joined row may add only the single-device-pixel internal hairline.
            foreach (int y in new[] { row.Top - S(7), row.Top - 1, row.Top, row.Top + 1, row.Bottom - 1, row.Bottom, row.Bottom + S(4) })
            for (int x = frame.Left - S(4); x < frame.Right + S(4); x++)
                Assert.Equal(y == row.Top && x > frame.Left && x < frame.Right - 1 ? theme.Border.ToArgb() : expected.GetPixel(x, y).ToArgb(), actual.GetPixel(x, y).ToArgb());
            if (shadow) continue;
            int radius = S(theme.CornerRadius * 2);
            for (int y = 0; y < radius; y++) for (int x = 0; x < radius; x++)
            {
                var corner = actual.GetPixel(frame.Left + x, frame.Top + y);
                Assert.Equal(corner, actual.GetPixel(frame.Right - 1 - x, frame.Top + y));
                Assert.Equal(corner, actual.GetPixel(frame.Left + x, frame.Bottom - 1 - y));
                Assert.Equal(corner, actual.GetPixel(frame.Right - 1 - x, frame.Bottom - 1 - y));
            }
        }
    });

    private sealed class SeparateOffice : RibbonOffice2024Renderer
    {
        public override bool JoinBelowRibbonQuickAccess => false;
    }
    private sealed class MutableOffice : RibbonOffice2024Renderer
    {
        internal bool Join = true;
        public override bool JoinBelowRibbonQuickAccess => Join;
    }
}
