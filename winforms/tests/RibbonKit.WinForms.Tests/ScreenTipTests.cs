using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ScreenTipTests
{
    [Fact]
    public void Metadata_is_localizable_and_caption_fallback_preserves_the_model() => FoundationTests.Sta(() =>
    {
        using var item = new RibbonButtonItem { Text = "Find matching text throughout this document without shortening the model caption" };
        string caption = item.Text;
        Assert.Equal(caption, RibbonScreenTipLayout.TitleFor(item));
        foreach (string name in new[] { nameof(item.ScreenTipTitle), nameof(item.ScreenTipDescription) })
        {
            var property = TypeDescriptor.GetProperties(item)[name]!;
            Assert.True(property.IsLocalizable);
            Assert.Equal(DesignerSerializationVisibility.Visible, property.SerializationVisibility);
            Assert.Equal("", ((DefaultValueAttribute)property.Attributes[typeof(DefaultValueAttribute)]!).Value);
            property.SetValue(item, "Authored text"); Assert.Equal("Authored text", property.GetValue(item));
            property.SetValue(item, null); Assert.Equal("", property.GetValue(item));
        }
        item.ScreenTipTitle = "Find"; item.ScreenTipDescription = "Search the current document.";
        Assert.Equal("Find", RibbonScreenTipLayout.TitleFor(item));
        item.ScreenTipTitle = ""; Assert.Equal(caption, RibbonScreenTipLayout.TitleFor(item));
        Assert.Equal(caption, item.Text);
    });

    [Fact]
    public void Pending_tips_cancel_on_input_model_layout_removal_and_disposal() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900 };
        var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup();
        var item = new RibbonSplitButtonItem { Text = "Split", ScreenTipDescription = "One tip for both halves", Layout = RibbonSplitButtonLayout.Vertical, Enabled = false };
        group.Items.Add(item); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon);
        form.Show(); Application.DoEvents();
        var targets = ribbon.Snapshot.Targets.Where(t => ReferenceEquals(t.Element, item)).ToArray();
        Point PointIn(RibbonTarget t) => ribbon.PointToScreen(new(t.VisibleBounds.Left + 2, t.VisibleBounds.Top + 2));
        var first = ribbon.ScreenTips.FindTarget(PointIn(targets[0]));
        var second = ribbon.ScreenTips.FindTarget(PointIn(targets[1]));
        Assert.NotNull(first); Assert.NotNull(second); Assert.Equal(first!.Value.Anchor, second!.Value.Anchor);
        Assert.Same(item, first.Value.Item); Assert.False(targets[0].Enabled);
        void Arm()
        {
            ribbon.ScreenTips.PointerMoved(new(-30000, -30000));
            ribbon.ScreenTips.PointerMoved(PointIn(targets[0]));
            Assert.True(ribbon.ScreenTips.Pending);
        }
        foreach (int message in new[] { 0x100, 0x102, 0x201, 0x204, 0x20A })
        {
            Arm(); var input = Message.Create(ribbon.Handle, message, 0, 0); ribbon.ScreenTips.Observe(ref input);
            Assert.False(ribbon.ScreenTips.Pending);
            ribbon.ScreenTips.PointerMoved(PointIn(targets[0])); Assert.False(ribbon.ScreenTips.Pending);
        }
        Arm(); item.ScreenTipDescription = "Changed"; Assert.False(ribbon.ScreenTips.Pending);
        Arm(); ribbon.Width -= 10; Assert.False(ribbon.ScreenTips.Pending);
        Arm(); form.Left += 1; Assert.False(ribbon.ScreenTips.Pending);
        Arm(); group.Items.Remove(item); Assert.False(ribbon.ScreenTips.Pending);
        group.Items.Add(item); ribbon.PerformLayout(); targets = ribbon.Snapshot.Targets.Where(t => ReferenceEquals(t.Element, item)).ToArray();
        Arm(); item.Dispose(); Assert.False(ribbon.ScreenTips.Pending);
        ribbon.Dispose(); Assert.Null(ribbon.ScreenTips.Window);
    });

    [Fact]
    public void Tip_measurement_placement_and_shared_frames_fit_work_areas_at_each_scale() => FoundationTests.Sta(() =>
    {
        const string title = "Find matching text throughout this document with the full original caption";
        const string description = "Search without changing the current selection.\r\nThe complete command caption remains in the item model.";
        using var flat = new RibbonRenderer(); using var office = new RibbonOffice2024Renderer();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var size in new[] { new Size(800, 600), new Size(801, 601), new Size(190, 150), new Size(191, 151) })
        {
            var layout = RibbonScreenTipLayout.Measure(title, description, SystemFonts.DefaultFont, dpi, size);
            Assert.InRange(layout.Size.Width, 1, Math.Min(size.Width, RibbonLayoutEngine.Scale(320, dpi)));
            Assert.InRange(layout.Size.Height, 1, size.Height);
            Assert.True(new Rectangle(Point.Empty, layout.Size).Contains(layout.Title));
            if (!layout.Description.IsEmpty) Assert.True(new Rectangle(Point.Empty, layout.Size).Contains(layout.Description));
            var work = new Rectangle(-900, -600, size.Width, size.Height);
            foreach (var anchor in new[] { new Rectangle(work.Left, work.Top, 40, 20), new Rectangle(work.Right - 40, work.Bottom - 20, 40, 20) })
                Assert.True(work.Contains(RibbonScreenTipLayout.Place(anchor, anchor.Location, layout.Size, work, dpi)));
            foreach (var painter in new[] { flat, office })
            {
                using var bitmap = new Bitmap(layout.Size.Width, layout.Size.Height);
                using var graphics = Graphics.FromImage(bitmap);
                var context = new RibbonPaintContext(graphics, new(Point.Empty, bitmap.Size), RibbonThemes.Office2024Light, SystemFonts.DefaultFont, dpi);
                painter.DrawPopupFrame(context);
                int radius = painter.GetPopupCornerRadius(context.Theme, dpi);
                for (int y = 0; y < radius; y++) for (int x = 0; x < radius; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    Assert.Equal(pixel, bitmap.GetPixel(bitmap.Width - 1 - x, y));
                    Assert.Equal(pixel, bitmap.GetPixel(x, bitmap.Height - 1 - y));
                    Assert.Equal(pixel, bitmap.GetPixel(bitmap.Width - 1 - x, bitmap.Height - 1 - y));
                }
                painter.DrawScreenTip(context, title, description, layout.Title, layout.Description);
            }
        }
    });
}
