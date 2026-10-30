using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageCaptionTests
{
    [Theory] [InlineData(RibbonWindowBackdrop.None, 1f)]
    [InlineData(RibbonWindowBackdrop.Mica, 0f)] [InlineData(RibbonWindowBackdrop.Mica, .65f)] [InlineData(RibbonWindowBackdrop.Mica, 1f)]
    [InlineData(RibbonWindowBackdrop.Acrylic, .65f)]
    public void Caption_tint_tracks_the_surface_edge_and_fade_in_both_directions(RibbonWindowBackdrop material, float opacity) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { Bounds = new(180, 120, 950, 650), Backdrop = material };
        using var page = new Panel(); var field = new TextBox { Text = "Retained" }; page.Controls.Add(field);
        var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page }); ribbon.Backstage.NavigationBackgroundOpacity = opacity;
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        ribbon.IsBackstageOpen = true; nint handle = field.Handle; var parent = field.Parent; var surface = ribbon.BackstageSurface!;
        foreach (bool closing in new[] { false, true })
        {
            if (closing) { ribbon.SettleMotion(); ribbon.IsBackstageOpen = false; }
            var motion = ribbon.BackstageOpeningMotion!;
            foreach (double progress in new[] { 0, .25, .5, 1 })
            {
                motion.SetProgress(progress); int edge = surface.Right;
                Assert.Equal(ribbon.WindowCaptionHeight, surface.Top);
                Assert.Equal(host.PointToScreen(new(0, ribbon.WindowCaptionHeight)).Y, surface.PointToScreen(Point.Empty).Y);
                using var frame = new Bitmap(ribbon.Width, ribbon.WindowCaptionHeight); using var graphics = Graphics.FromImage(frame);
                graphics.Clear(Color.Transparent); ribbon.PaintCaptionBackground(graphics, new(Point.Empty, frame.Size), ribbon.HasWindowBackdrop);
                Color normal = ribbon.HasWindowBackdrop ? Color.Transparent : ribbon.Theme.Background;
                if (edge > 0)
                {
                    var tint = ribbon.CaptionBackground(ribbon.HasWindowBackdrop);
                    using var expectedImage = new Bitmap(1, 1); using (var paint = Graphics.FromImage(expectedImage))
                    { paint.Clear(normal); using var brush = new SolidBrush(tint); paint.FillRectangle(brush, 0, 0, 1, 1); }
                    var expected = expectedImage.GetPixel(0, 0); var actual = frame.GetPixel(edge - 1, 2);
                    Assert.Equal(expected.A, actual.A);
                    if (expected.A > 0) // GDI+ premultiplies partial-alpha channels with integer rounding.
                    { Assert.InRange(Math.Abs(expected.R - actual.R), 0, 1); Assert.InRange(Math.Abs(expected.G - actual.G), 0, 1); Assert.InRange(Math.Abs(expected.B - actual.B), 0, 1); }
                }
                if (edge < frame.Width) Assert.Equal(normal.A == 0 ? 0 : normal.ToArgb(), frame.GetPixel(edge, 2).ToArgb());
                Assert.Same(parent, field.Parent); Assert.Equal(handle, field.Handle); Assert.Equal("Retained", field.Text);
            }
        }
        ribbon.SettleMotion(); Assert.False(ribbon.IsBackstageOpen); Assert.Equal(0, surface.Left);
    });

    [Theory] [InlineData(0f)] [InlineData(.65f)] [InlineData(1f)]
    public void Open_caption_matches_rail_and_qat_is_hidden_without_changing_its_preference(float opacity) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(180, 120, 950, 650) };
        using var painter = new CaptionProbe(); using var page = new Panel();
        var ribbon = new RibbonControl { Renderer = painter, FileMenuMode = RibbonFileMenuMode.Backstage, QuickAccessPosition = RibbonQuickAccessPosition.TitleBar };
        var command = new RibbonButtonItem { Text = "Save" }; var group = new RibbonGroup(); group.Items.Add(command); var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = command }); ribbon.FileMenu.Visible = true;
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page }); ribbon.Backstage.NavigationBackgroundOpacity = opacity;
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); var authored = ribbon.Theme;
        foreach (var material in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            host.Backdrop = material; bool composited = ribbon.HasWindowBackdrop;
            Assert.Equal(composited ? Color.Transparent : authored.Background, ribbon.CaptionBackground(composited));
            painter.ToolbarPaints = 0; ribbon.IsBackstageOpen = true; ribbon.Refresh();
            int alpha = composited ? (int)Math.Round(255 * opacity) : 255;
            Assert.Equal(Color.FromArgb(alpha, authored.BackstageNavigationBackground), ribbon.CaptionBackground(composited));
            Assert.Equal(0, painter.ToolbarPaints); Assert.True(ribbon.Snapshot.QuickAccessBounds.IsEmpty); Assert.True(ribbon.QuickAccessVisible);
            if (composited)
            {
                using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.CopyBackdropSnapshot(image);
                var color = image.GetPixel(ribbon.Snapshot.QuickAccessBounds.Right + 20, 2);
                Assert.InRange(color.A, Math.Max(0, alpha - 1), Math.Min(255, alpha + 1));
            }
            ribbon.IsBackstageOpen = false; ribbon.Refresh();
            Assert.True(painter.ToolbarPaints > 0); Assert.False(ribbon.Snapshot.QuickAccessBounds.IsEmpty);
            Assert.Equal(composited ? Color.Transparent : authored.Background, ribbon.CaptionBackground(composited));
            Assert.Same(authored, ribbon.Theme);
        }
    });
    private sealed class CaptionProbe : RibbonOffice2024Renderer
    {
        internal int ToolbarPaints;
        public override void DrawQuickAccessToolbar(RibbonPaintContext context, RibbonQuickAccessPosition position)
        { ToolbarPaints++; base.DrawQuickAccessToolbar(context, position); }
    }

    [Theory] [InlineData(RibbonQuickAccessPosition.TitleBar, true)] [InlineData(RibbonQuickAccessPosition.TitleBar, false)]
    [InlineData(RibbonQuickAccessPosition.TabRow, true)] [InlineData(RibbonQuickAccessPosition.TabRow, false)]
    [InlineData(RibbonQuickAccessPosition.BelowRibbon, true)] [InlineData(RibbonQuickAccessPosition.BelowRibbon, false)]
    public void Qat_stays_hidden_through_opening_closing_and_reopening_and_restores_the_authored_state(RibbonQuickAccessPosition position, bool visible) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { Size = new(1000, 700) }; using var page = new Panel();
        using var painter = new CaptionProbe();
        var ribbon = new RibbonControl { Renderer = painter, FileMenuMode = RibbonFileMenuMode.Backstage, QuickAccessPosition = position, QuickAccessVisible = visible, AnimationLevel = RibbonAnimationLevel.Subtle };
        var command = new RibbonButtonItem { Text = "Save" }; var reference = new RibbonQuickAccessReference { Source = command };
        var group = new RibbonGroup(); group.Items.Add(command); var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(reference); ribbon.FileMenu.Visible = true; ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        int height = ribbon.Height, changes = 0; var before = ribbon.Snapshot.QuickAccessBounds;
        ribbon.QuickAccessVisibleChanged += (_, _) => changes++; ribbon.QuickAccessPositionChanged += (_, _) => changes++;
        painter.ToolbarPaints = 0; ribbon.IsBackstageOpen = true; Assert.True(ribbon.IsAnimating); AssertHidden();
        ribbon.SettleMotion(); AssertHidden(); ribbon.IsBackstageOpen = false; Assert.True(ribbon.IsAnimating); AssertHidden();
        ribbon.IsBackstageOpen = true; AssertHidden(); ribbon.IsBackstageOpen = false; ribbon.SettleMotion(); ribbon.Refresh();
        Assert.False(ribbon.IsBackstageOpen); Assert.Equal(before, ribbon.Snapshot.QuickAccessBounds);
        Assert.Equal(visible, ribbon.QuickAccessVisible); Assert.Equal(position, ribbon.QuickAccessPosition); Assert.Equal(0, changes);
        Assert.Same(reference, ribbon.QuickAccessItems[0]); Assert.Same(command, reference.Source); Assert.Equal(height, ribbon.Height);
        Assert.Equal(visible, painter.ToolbarPaints > 0);

        void AssertHidden()
        {
            ribbon.Refresh(); Assert.True(ribbon.Snapshot.QuickAccessBounds.IsEmpty); Assert.Equal(0, painter.ToolbarPaints);
            Assert.DoesNotContain(ribbon.Snapshot.Targets, t => t.Kind is Layout.TargetKind.QuickAccess or Layout.TargetKind.QuickAccessOverflow);
            Assert.Equal(height, ribbon.Height); Assert.Equal(visible, ribbon.QuickAccessVisible); Assert.Equal(position, ribbon.QuickAccessPosition);
        }
    });
}
