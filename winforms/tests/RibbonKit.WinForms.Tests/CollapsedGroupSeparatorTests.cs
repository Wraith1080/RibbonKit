using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class CollapsedGroupSeparatorTests
{
    [Fact]
    public void Expanded_and_collapsed_group_dividers_align_without_enlarging_click_targets_in_root_and_minimized_body() => FoundationTests.Sta(() =>
    {
        using var painter = new Recorder(); using var form = new Form { Width = 500, Height = 500 }; var ribbon = new RibbonControl { Renderer = painter }; form.Controls.Add(ribbon);
        var tab = new RibbonTab { Text = "Home" }; ribbon.Tabs.Add(tab);
        var expanded = new RibbonGroup { Text = "Expanded", CanResize = false }; expanded.Items.Add(new RibbonButtonItem { Text = "Action", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) }); tab.Groups.Add(expanded);
        foreach (string name in new[] { "Editing", "Galleries" })
        {
            var group = new RibbonGroup { Text = name }; group.Items.AddRange(Enumerable.Range(0, 5).Select(i => new RibbonButtonItem { Text = "Command " + i }).ToArray()); tab.Groups.Add(group);
        }
        form.Show(); Application.DoEvents();
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.Density = density; int S(int value) => RibbonLayoutEngine.Scale(value, ribbon.DeviceDpi);
            Assert.Equal(2, ribbon.Snapshot.Groups.Count(g => g.Collapsed));
            foreach (var slot in ribbon.Snapshot.Groups.Where(g => g.Collapsed))
            {
                Assert.Equal(ribbon.Snapshot.BodyBounds.Top, slot.Frame.Top); Assert.Equal(ribbon.Snapshot.BodyBounds.Bottom, slot.Frame.Bottom);
                Assert.Equal(slot.Frame.Left + S(3), slot.Bounds.Left); Assert.Equal(slot.Frame.Top + S(6), slot.Bounds.Top);
                Assert.Equal(slot.Frame.Bottom - S(8), slot.Bounds.Bottom); Assert.Equal(slot.Bounds, ribbon.Snapshot.Targets.Single(t => t.Element == slot.Group).Bounds);
            }
            using (var bitmap = new Bitmap(ribbon.Width, ribbon.Height))
            {
                painter.Frames.Clear(); ribbon.DrawToBitmap(bitmap, ribbon.ClientRectangle); Check(bitmap);
            }
            ribbon.IsMinimized = true; ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Element == tab)); var body = ribbon.Popups.Top!;
            using (var bitmap = new Bitmap(body.Width, body.Height))
            {
                painter.Frames.Clear(); body.DrawToBitmap(bitmap, body.ClientRectangle); Check(bitmap);
            }
            ribbon.Popups.CloseAll(false); ribbon.IsMinimized = false;

            void Check(Bitmap bitmap)
            {
                Assert.Equal(3, painter.Frames.Count); Assert.Equal(2, painter.Frames.Count(f => f.Collapsed));
                Assert.Single(painter.Frames.Select(f => f.Bounds.Top).Distinct()); Assert.Single(painter.Frames.Select(f => f.Bounds.Bottom).Distinct());
                foreach (var frame in painter.Frames.Select(f => f.Bounds))
                {
                    int top = frame.Top + S(12), bottom = frame.Bottom - S(12);
                    for (int y = top; y < bottom; y++) Assert.Equal(ribbon.Theme.Border.ToArgb(), bitmap.GetPixel(frame.Right - 1, y).ToArgb());
                    Assert.Equal(ribbon.Theme.Surface.ToArgb(), bitmap.GetPixel(frame.Right - 1, top - 1).ToArgb());
                    Assert.Equal(ribbon.Theme.Surface.ToArgb(), bitmap.GetPixel(frame.Right - 1, bottom).ToArgb());
                }
            }
        }
    });
    private sealed class Recorder : RibbonOffice2024Renderer
    {
        internal readonly List<(Rectangle Bounds, bool Collapsed)> Frames = [];
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup group, bool collapsed) { Frames.Add((c.GroupSeparatorBounds, collapsed)); base.DrawGroup(c, group, collapsed); }
    }
}
