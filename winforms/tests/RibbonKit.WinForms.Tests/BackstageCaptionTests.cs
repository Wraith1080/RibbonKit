using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageCaptionTests
{
    [Theory] [InlineData(RibbonWindowBackdrop.None, 1f)]
    [InlineData(RibbonWindowBackdrop.Mica, 0f)] [InlineData(RibbonWindowBackdrop.Mica, .65f)] [InlineData(RibbonWindowBackdrop.Mica, 1f)]
    [InlineData(RibbonWindowBackdrop.Acrylic, .65f)]
    public void Full_height_surface_owns_caption_background_during_both_slide_directions(RibbonWindowBackdrop material, float opacity) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { Bounds = new(180, 120, 950, 650), Backdrop = material };
        using var page = new Panel(); var field = new TextBox { Text = "Retained" }; page.Controls.Add(field);
        var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive };
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
                Assert.Equal(0, surface.Top); Assert.Equal(host.ClientSize.Height, surface.Height);
                Assert.Equal(host.PointToScreen(Point.Empty).Y, surface.PointToScreen(Point.Empty).Y);
                Assert.True(surface.PageArea.Top < host.CaptionHeight);
                using var frame = new Bitmap(ribbon.Width, ribbon.WindowCaptionHeight); using var graphics = Graphics.FromImage(frame);
                graphics.Clear(Color.Transparent); ribbon.PaintCaptionBackground(graphics, new(Point.Empty, frame.Size), ribbon.HasWindowBackdrop);
                Color normal = host.CaptionColor(ribbon.Theme, ribbon.HasWindowBackdrop);
                // No duplicate color animation remains in the original caption.
                Assert.Equal(normal.A == 0 ? 0 : normal.ToArgb(), frame.GetPixel(Math.Max(0, edge - 1), 2).ToArgb());
                Assert.Equal(Math.Max(0, surface.Left + surface.NavigationBounds.Right), ribbon.CaptionPageBounds.Left);
                using var foreground = ribbon.CaptureBackstageCaption(); Assert.NotNull(foreground);
                Assert.Equal(0, foreground.GetPixel(100, 2).A);
                Assert.Same(parent, field.Parent); Assert.Equal(handle, field.Handle); Assert.Equal("Retained", field.Text);
            }
        }
        ribbon.SettleMotion(); Assert.False(ribbon.IsBackstageOpen); Assert.Equal(0, surface.Left);
    });

    [Theory] [InlineData(0f)] [InlineData(.65f)] [InlineData(1f)]
    public void Transparent_caption_keeps_underlay_and_hides_qat_without_changing_its_preference(float opacity) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(180, 120, 950, 650) };
        using var painter = new CaptionProbe(); using var page = new Panel();
        var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light, Renderer = painter, FileMenuMode = RibbonFileMenuMode.Backstage, QuickAccessPosition = RibbonQuickAccessPosition.TitleBar };
        var command = new RibbonButtonItem { Text = "Save" }; var group = new RibbonGroup(); group.Items.Add(command); var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = command }); ribbon.FileMenu.Visible = true;
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page }); ribbon.Backstage.NavigationBackgroundOpacity = opacity;
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); var authored = ribbon.Theme;
        foreach (var material in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            host.Backdrop = material; bool composited = ribbon.HasWindowBackdrop;
            Assert.Equal(composited ? Color.Transparent : authored.Background, ribbon.CaptionBackground(composited));
            painter.ToolbarPaints = 0; ribbon.IsBackstageOpen = true; ribbon.Refresh();
            Assert.Equal(composited ? Color.Transparent : authored.Background, ribbon.CaptionBackground(composited));
            Assert.Equal(0, painter.ToolbarPaints); Assert.True(ribbon.Snapshot.QuickAccessBounds.IsEmpty); Assert.True(ribbon.QuickAccessVisible);
            if (composited)
            {
                using var foreground = ribbon.CaptureBackstageCaption(); Assert.NotNull(foreground);
                Assert.Equal(0, foreground.GetPixel(100, 2).A);
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

    [Theory] [InlineData(RibbonQuickAccessPosition.TitleBar, true, true)] [InlineData(RibbonQuickAccessPosition.TitleBar, false, true)]
    [InlineData(RibbonQuickAccessPosition.TabRow, true, true)] [InlineData(RibbonQuickAccessPosition.TabRow, false, true)]
    [InlineData(RibbonQuickAccessPosition.BelowRibbon, true, true)] [InlineData(RibbonQuickAccessPosition.BelowRibbon, false, true)]
    [InlineData(RibbonQuickAccessPosition.TitleBar, true, false)] [InlineData(RibbonQuickAccessPosition.TitleBar, false, false)]
    public void Backstage_suppresses_only_effective_titlebar_qat_and_preserves_the_authored_state(RibbonQuickAccessPosition position, bool visible, bool caption) => FoundationTests.Sta(() =>
    {
        using Form host = caption ? new RibbonForm() : new Form(); host.Size = new(1000, 700); using var page = new Panel();
        using var painter = new CaptionProbe();
        var ribbon = new RibbonControl { Renderer = painter, FileMenuMode = RibbonFileMenuMode.Backstage, QuickAccessPosition = position, QuickAccessVisible = visible, AnimationLevel = RibbonAnimationLevel.Subtle };
        var command = new RibbonButtonItem { Text = "Save" }; var reference = new RibbonQuickAccessReference { Source = command };
        var group = new RibbonGroup(); group.Items.Add(command); var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(reference); ribbon.FileMenu.Visible = true; ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        host.Controls.Add(ribbon); if (host is RibbonForm window) window.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        int height = ribbon.Height, changes = 0; var before = ribbon.Snapshot.QuickAccessBounds;
        bool shownDuringBackstage = visible && ribbon.EffectiveQuickAccessPosition != RibbonQuickAccessPosition.TitleBar;
        ribbon.QuickAccessVisibleChanged += (_, _) => changes++; ribbon.QuickAccessPositionChanged += (_, _) => changes++;
        painter.ToolbarPaints = 0; ribbon.IsBackstageOpen = true; Assert.True(ribbon.IsAnimating); AssertPresentation();
        ribbon.SettleMotion(); AssertPresentation(); ribbon.IsBackstageOpen = false; Assert.True(ribbon.IsAnimating); AssertPresentation();
        ribbon.IsBackstageOpen = true; AssertPresentation(); ribbon.IsBackstageOpen = false; ribbon.SettleMotion(); ribbon.Refresh();
        Assert.False(ribbon.IsBackstageOpen); Assert.Equal(before, ribbon.Snapshot.QuickAccessBounds);
        Assert.Equal(visible, ribbon.QuickAccessVisible); Assert.Equal(position, ribbon.QuickAccessPosition); Assert.Equal(0, changes);
        Assert.Same(reference, ribbon.QuickAccessItems[0]); Assert.Same(command, reference.Source); Assert.Equal(height, ribbon.Height);
        Assert.Equal(visible, painter.ToolbarPaints > 0);

        void AssertPresentation()
        {
            ribbon.Refresh(); Assert.Equal(!shownDuringBackstage, ribbon.Snapshot.QuickAccessBounds.IsEmpty);
            Assert.Equal(shownDuringBackstage, painter.ToolbarPaints > 0);
            Assert.Equal(shownDuringBackstage, ribbon.Snapshot.Targets.Any(t => t.Kind is Layout.TargetKind.QuickAccess or Layout.TargetKind.QuickAccessOverflow));
            if (shownDuringBackstage) Assert.Equal(before, ribbon.Snapshot.QuickAccessBounds);
            Assert.Equal(height, ribbon.Height); Assert.Equal(visible, ribbon.QuickAccessVisible); Assert.Equal(position, ribbon.QuickAccessPosition);
        }
    });
}
