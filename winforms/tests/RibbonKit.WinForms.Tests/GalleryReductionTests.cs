using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GalleryReductionTests
{
    [Fact]
    public void Reduced_in_ribbon_gallery_keeps_a_large_exclusive_column_across_density_dpi_and_size_policies() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); host.Tabs.Add(tab);
        var before = new RibbonButtonItem { Text = "Before", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        var gallery = new RibbonGalleryItem { Text = "Styles", IsInRibbon = true, TileWidth = 96, SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small) };
        var after = new RibbonButtonItem { Text = "After", SizePolicy = before.SizePolicy }; group.Items.AddRange(before, gallery, after);
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            host.Density = density; gallery.SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small);
            var engine = new RibbonLayoutEngine(); var full = engine.Measure(group, 0, host.Font, dpi, host.LayoutTheme).Items.Single(i => i.Item == gallery);
            Assert.False(full.GalleryDropDown);
            foreach (int state in new[] { 1, 2 })
            {
                var items = engine.Measure(group, state, host.Font, dpi, host.LayoutTheme).Items;
                var reduced = items.Single(i => i.Item == gallery);
                Assert.True(reduced.GalleryDropDown); Assert.Equal(RibbonItemSize.Large, reduced.Size); Assert.Equal(full.Bounds.Height, reduced.Bounds.Height); Assert.True(reduced.Bounds.Width < full.Bounds.Width);
                Assert.True(items[0].Bounds.Right < reduced.Bounds.Left); Assert.True(reduced.Bounds.Right < items[2].Bounds.Left);
            }
            foreach (var size in new[] { RibbonItemSize.Medium, RibbonItemSize.Small })
            {
                gallery.SizePolicy = RibbonSizePolicy.Fixed(size); engine.Clear(); var reduced = engine.Measure(group, 0, host.Font, dpi, host.LayoutTheme).Items.Single(i => i.Item == gallery);
                Assert.True(reduced.GalleryDropDown); Assert.Equal(RibbonItemSize.Large, reduced.Size); Assert.Equal(size, gallery.SizePolicy.Large);
                gallery.IsInRibbon = false; engine.Clear(); var ordinary = engine.Measure(group, 0, host.Font, dpi, host.LayoutTheme).Items.Single(i => i.Item == gallery);
                Assert.False(ordinary.GalleryDropDown); Assert.Equal(size, ordinary.Size); gallery.IsInRibbon = true;
            }
        }
    });

    [Fact]
    public void Resize_parks_the_strip_and_routes_large_fallback_popups_without_replacing_source_presenter_or_selection() => FoundationTests.Sta(() =>
    {
        using var painter = new Recorder(); using var form = new Form { Width = 1400, Height = 600 }; var host = new RibbonControl { Renderer = painter }; form.Controls.Add(host);
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Styles", ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse }; tab.Groups.Add(group); host.Tabs.Add(tab);
        var gallery = new RibbonGalleryItem { Text = "Styles", IsInRibbon = true, TileWidth = 96, SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small) };
        gallery.Items.AddRange(Enumerable.Range(0, 12).Select(i => new RibbonGalleryTile { Text = "Style " + i }).ToArray()); gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[4];
        group.Items.AddRange(gallery, new RibbonButtonItem { Text = "Other", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) });
        int commits = 0; gallery.SelectionCommitted += (_, _) => commits++; var policy = gallery.SizePolicy;
        form.Show(); Application.DoEvents(); var strip = host.GalleryStrips.SurfaceFor(gallery)!; nint handle = strip.Handle;
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            host.Density = density; form.Width = 1400; host.Refresh();
            var full = host.Snapshot.Targets.Single(t => t.Element == gallery); Assert.False(full.GalleryDropDown); Assert.True(strip.Visible);
            RibbonTarget? fallback = null;
            for (int width = 1300; width >= 220; width -= 10)
            {
                form.Width = width; host.Refresh(); fallback = host.Snapshot.Targets.FirstOrDefault(t => t.Element == gallery && t.GalleryDropDown);
                if (fallback is not null) break;
            }
            Assert.NotNull(fallback); Assert.Equal(RibbonItemSize.Large, fallback.Size); Assert.Equal(full.Bounds.Height, fallback.Bounds.Height);
            Assert.False(strip.Visible); Assert.False(strip.IsDisposed); Assert.Equal(policy, gallery.SizePolicy); Assert.True(gallery.IsInRibbon);
            painter.Fallback = false; painter.Frames = 0; using (var bitmap = new Bitmap(host.Width, host.Height)) host.DrawToBitmap(bitmap, host.ClientRectangle);
            Assert.True(painter.Fallback); Assert.Equal(0, painter.Frames);
            host.Activate(fallback); Assert.Same(gallery, host.Popups.Top!.GallerySource);
            var tile = host.Popups.Top.Targets.First(t => t.Element is RibbonGalleryTile); host.Activate(tile, host.Popups.Top); Assert.False(host.Popups.HasOpen); Assert.Same(tile.Element, gallery.SelectedItem);
            host.IsMinimized = true; host.Activate(host.Snapshot.Targets.Single(t => t.Element == tab)); var body = host.Popups.Top!;
            Assert.True(body.Targets.Single(t => t.Element == gallery).GalleryDropDown);
            painter.Fallback = false; using (var bitmap = new Bitmap(body.Width, body.Height)) body.DrawToBitmap(bitmap, body.ClientRectangle); Assert.True(painter.Fallback);
            host.Popups.CloseAll(false); host.IsMinimized = false; form.Width = 1400; host.Refresh();
            Assert.Same(strip, host.GalleryStrips.SurfaceFor(gallery)); Assert.Equal(handle, strip.Handle); Assert.True(strip.Visible); Assert.Same(tile.Element, gallery.SelectedItem);
        }
        Assert.Equal(2, commits);
    });
    private sealed class Recorder : RibbonOffice2024Renderer
    {
        internal bool Fallback; internal int Frames;
        public override void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu) { if (item is RibbonGalleryItem) Fallback = context.IsGalleryDropDown; base.DrawItem(context, item, size, menu); }
        public override void DrawGalleryStripFrame(RibbonPaintContext context) { Frames++; base.DrawGalleryStripFrame(context); }
    }
}
