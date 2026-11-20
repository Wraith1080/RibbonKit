using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageFilePresentationTests
{
    private sealed class HoverRibbon : RibbonControl
    {
        internal void HoverFile()
        {
            var bounds = Snapshot.Targets.Single(t => t.Kind == TargetKind.FileMenu).Bounds;
            OnMouseMove(new(MouseButtons.None, 0, bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2, 0));
        }
    }

    [Fact]
    public void Integrated_Office2024_paints_File_before_the_closing_pane_reveals_it() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 950, 650) };
        using var ribbon = new HoverRibbon { BuiltInTheme = RibbonBuiltInTheme.Office2024Light,
            FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive };
        ribbon.FileMenu.Visible = true;
        ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = new Panel() });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents();
        ribbon.Motion.SystemPolicyOverride = true;
        ribbon.IsBackstageOpen = true; ribbon.SettleMotion(); ribbon.Refresh(); Application.DoEvents();
        var file = ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.FileMenu).VisibleBounds;
        int fileInvalidations = 0;
        ribbon.Invalidated += (_, e) => { if (e.InvalidRect.Contains(file)) fileInvalidations++; };
        ribbon.IsBackstageOpen = false;
        var closing = Assert.IsType<RibbonBackstageMotion>(ribbon.BackstageOpeningMotion);
        Assert.True(closing.IsClosing);
        Assert.True(fileInvalidations > 0, "The open File drawing was left under the outgoing pane.");
        int initial = fileInvalidations;
        closing.SetProgress(.5);
        Assert.True(fileInvalidations > initial, "The covered File drawing must be invalidated again as the pane fades.");
        Assert.True(ribbon.FileMenu.IsDropDownOpen);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Closing_reveals_the_final_File_hover_without_changing_its_open_lifecycle(bool office) => FoundationTests.Sta(() =>
    {
        using var host = new Form { Size = new(950, 650) };
        using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        var ribbon = new HoverRibbon { Renderer = renderer, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        using var page = new Panel(); ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        host.Controls.Add(ribbon); host.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        using var closed = FileImage();
        ribbon.IsBackstageOpen = true; ribbon.SettleMotion(); using var opened = FileImage();
        Assert.False(Same(closed, opened));
        ribbon.IsBackstageOpen = false; ribbon.BackstageOpeningMotion!.SetProgress(.5);
        Assert.True(ribbon.IsBackstageOpen); Assert.True(ribbon.FileMenu.IsDropDownOpen);
        using var closing = FileImage(); Assert.True(Same(closed, closing));
        ribbon.IsBackstageOpen = true; using var reopened = FileImage(); Assert.True(Same(opened, reopened));
        ribbon.IsBackstageOpen = false; ribbon.SettleMotion(); using var finished = FileImage(); Assert.True(Same(closed, finished));

        Bitmap FileImage()
        {
            ribbon.HoverFile();
            using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
            var bounds = ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.FileMenu).Bounds;
            return image.Clone(bounds, image.PixelFormat);
        }
        static bool Same(Bitmap first, Bitmap second)
        {
            if (first.Size != second.Size) return false;
            for (int y = 0; y < first.Height; y++) for (int x = 0; x < first.Width; x++)
                if (first.GetPixel(x, y) != second.GetPixel(x, y)) return false;
            return true;
        }
    });
}
