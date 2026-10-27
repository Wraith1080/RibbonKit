using System.ComponentModel;
using System.Reflection;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class InRibbonGalleryTests
{
    [Fact]
    public void In_ribbon_metrics_and_realized_rows_remain_dpi_aligned_across_direction_reversals() => FoundationTests.Sta(() =>
    {
        using var source = new RibbonGalleryItem(); Assert.False(source.IsInRibbon); Assert.Equal(3, source.InRibbonColumnCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => source.InRibbonColumnCount = 0);
        Assert.True(((BindableAttribute)TypeDescriptor.GetProperties(source)[nameof(source.IsInRibbon)]!.Attributes[typeof(BindableAttribute)]!).Bindable);
        source.Items.AddRange(Enumerable.Range(0, 11).Select(i => new RibbonGalleryTile { Text = "Tile " + i, Visible = i != 1 }).ToArray());
        source.SelectedItem = (RibbonGalleryTile)source.Items[5];
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int logicalHeight in new[] { 76, 77 })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            int last = 0;
            for (int cycle = 0; cycle < 2; cycle++)
            foreach (int requested in Enumerable.Range(0, 6).Concat(Enumerable.Range(0, 6).Reverse()))
            {
                var layout = RibbonGalleryStripLayout.Build(source, new(S(270), S(logicalHeight)), dpi, requested);
                Assert.Equal(3, layout.LastRow); last = layout.Row;
                Assert.InRange(layout.Row, 0, 3); Assert.Equal(S(4), layout.Viewport.Top); Assert.Equal(S(logicalHeight) - S(4), layout.Viewport.Bottom);
                Assert.Equal(layout.Up.Bottom, layout.Down.Top); Assert.Equal(layout.Down.Bottom, layout.Expand.Top);
                Assert.Equal(S(4), layout.Up.Top); Assert.Equal(S(logicalHeight) - S(4), layout.Expand.Bottom);
                Assert.True(layout.Expand.Right <= S(270) - S(4)); Assert.True(layout.Up.Left - layout.Viewport.Right >= S(4));
                var heights = new[] { layout.Up.Height, layout.Down.Height, layout.Expand.Height };
                Assert.InRange(heights.Max() - heights.Min(), 0, 1);
                Assert.All(layout.Tiles, t => { Assert.True(layout.Viewport.Contains(t.Bounds)); Assert.False(t.Bounds.IntersectsWith(layout.Up)); });
                Assert.Equal(source.Items.Where(t => t.Visible).Skip(layout.Row * 3).Take(3), layout.Tiles.Select(t => t.Tile));
            }
            Assert.Equal(0, last); Assert.Same(source.Items[5], source.SelectedItem);
        }
    });

    [Fact]
    public void Utility_hover_and_press_preserve_all_four_gallery_frame_edges() => FoundationTests.Sta(() =>
    {
        using var source = new RibbonGalleryItem(); using var neutral = new RibbonRenderer(); using var office = new RibbonOffice2024Renderer();
        foreach (var renderer in new RibbonRenderer[] { neutral, office })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int logicalHeight in new[] { 76, 77 })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            var size = new Size(S(270), S(logicalHeight)); var layout = RibbonGalleryStripLayout.Build(source, size, dpi, 0);
            using var font = new Font("Segoe UI", 9 * dpi / 96f);
            using var baseline = Paint(false, false);
            foreach (bool pressed in new[] { false, true })
            {
                using var active = Paint(true, pressed);
                for (int x = 0; x < size.Width; x++) { Assert.Equal(baseline.GetPixel(x, 0), active.GetPixel(x, 0)); Assert.Equal(baseline.GetPixel(x, size.Height - 1), active.GetPixel(x, size.Height - 1)); }
                for (int y = 0; y < size.Height; y++) { Assert.Equal(baseline.GetPixel(0, y), active.GetPixel(0, y)); Assert.Equal(baseline.GetPixel(size.Width - 1, y), active.GetPixel(size.Width - 1, y)); }
            }
            Bitmap Paint(bool buttons, bool pressed)
            {
                var bitmap = new Bitmap(size.Width, size.Height); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(Color.Magenta);
                var context = new RibbonPaintContext(graphics, new(Point.Empty, size), RibbonThemes.Office2024Light, font, dpi);
                renderer.DrawGalleryStripFrame(context);
                if (buttons) foreach (var (button, bounds) in new[] { (RibbonGalleryStripButton.Up, layout.Up), (RibbonGalleryStripButton.Down, layout.Down), (RibbonGalleryStripButton.Expand, layout.Expand) })
                    renderer.DrawGalleryStripButton(context with { Bounds = bounds, Hot = true, Pressed = pressed }, button, true);
                return bitmap;
            }
        }
    });

    [Fact]
    public void Strip_keyboard_commit_and_popup_cancel_preserve_viewed_row_and_source_identity() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1200, Height = 600 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon); var gallery = GalleryTests.Attach(ribbon);
        gallery.IsInRibbon = true; gallery.Items.AddRange(Enumerable.Range(0, 12).Select(i => new RibbonGalleryTile { Text = "Tile " + i }).ToArray());
        gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[4]; int commits = 0, cancels = 0; gallery.SelectionCommitted += (_, _) => commits++; gallery.ItemPreviewCancelled += (_, _) => cancels++;
        form.Show(); Application.DoEvents(); var strip = Assert.IsType<RibbonGalleryStripSurface>(ribbon.GalleryStrips.SurfaceFor(gallery));
        Assert.True(strip.Visible); Assert.Equal(1, strip.LayoutSnapshot.Row); ribbon.FocusElement(gallery); Assert.True(strip.Focused);
        Key(strip, Keys.End); Assert.Same(gallery.Items[11], gallery.PreviewItem); Assert.Same(gallery.Items[4], gallery.SelectedItem);
        Key(strip, Keys.Enter); Assert.Same(gallery.Items[11], gallery.SelectedItem); Assert.Equal(1, commits); Assert.Null(gallery.PreviewItem);
        Key(strip, Keys.Space); Assert.Equal(2, commits);
        Key(strip, Keys.PageUp); Assert.Equal(2, strip.LayoutSnapshot.Row); int before = cancels;
        Key(strip, Keys.F4); Assert.True(gallery.IsDropDownOpen); Assert.True(strip.Visible); Assert.Same(strip, ribbon.GalleryStrips.SurfaceFor(gallery));
        var popup = ribbon.Popups.Top!; Key(popup, Keys.Home); var preview = gallery.PreviewItem; Assert.NotNull(preview);
        gallery.CancelPreview(strip); Assert.Same(preview, gallery.PreviewItem);
        Key(popup, Keys.Escape); Assert.False(gallery.IsDropDownOpen); Assert.Equal(2, strip.LayoutSnapshot.Row); Assert.True(strip.Focused); Assert.True(cancels >= before + 1);
        gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0]; Assert.Equal(0, strip.LayoutSnapshot.Row);
        gallery.IsInRibbon = false; Assert.True(strip.IsDisposed); Assert.False(gallery.IsDisposed); Assert.Equal(12, gallery.Items.Count);
    });

    [Fact]
    public void Qat_copies_share_the_original_gallery_and_commit_from_an_inactive_tab_or_overflow() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1200, Height = 600 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon); var gallery = GalleryTests.Attach(ribbon);
        gallery.IsInRibbon = true; gallery.Items.AddRange(Enumerable.Range(0, 10).Select(i => new RibbonGalleryTile { Text = "Tile " + i }).ToArray()); gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[4];
        var home = ribbon.Tabs[0]; var other = new RibbonTab { Text = "Other" }; ribbon.Tabs.Add(other);
        var first = new RibbonQuickAccessDropDownReference { Source = gallery }; var second = new RibbonQuickAccessDropDownReference { Source = gallery }; ribbon.QuickAccessItems.AddRange(first, second);
        int commits = 0; gallery.SelectionCommitted += (_, _) => commits++;
        form.Show(); Application.DoEvents(); var strip = ribbon.GalleryStrips.SurfaceFor(gallery)!;
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Element == first)); Assert.True(first.IsDropDownOpen); Assert.Same(gallery, ribbon.Popups.Top!.GallerySource); Assert.True(strip.Visible);
        Key(ribbon.Popups.Top, Keys.Home); ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Element == second)); Assert.False(first.IsDropDownOpen); Assert.True(second.IsDropDownOpen); Assert.Single(ribbon.Popups.Surfaces);
        ribbon.SelectedTab = other; Assert.False(second.IsDropDownOpen); Assert.False(strip.Visible); Assert.False(strip.IsDisposed);
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Element == first)); Key(ribbon.Popups.Top!, Keys.End); Key(ribbon.Popups.Top!, Keys.Enter);
        Assert.Equal(1, commits); Assert.Same(gallery.Items[9], gallery.SelectedItem); Assert.Same(other, ribbon.SelectedTab);
        ribbon.SelectedTab = home; Assert.Same(strip, ribbon.GalleryStrips.SurfaceFor(gallery)); Assert.Equal(3, strip.LayoutSnapshot.Row);
        ribbon.QuickAccessMaxWidth = 28; ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.QuickAccessOverflow));
        var overflow = ribbon.Popups.Top!; ribbon.Activate(overflow.Targets.Single(t => t.Element == first), overflow); Assert.True(first.IsDropDownOpen); Assert.Equal(2, ribbon.Popups.Surfaces.Count());
        Key(ribbon.Popups.Top!, Keys.Home); Key(ribbon.Popups.Top!, Keys.Enter); Assert.Equal(2, commits); Assert.False(ribbon.Popups.HasOpen); Assert.Same(gallery.Items[0], gallery.SelectedItem);
        first.Dispose(); Assert.False(gallery.IsDisposed); Assert.Equal(10, gallery.Items.Count); Assert.Same(strip, ribbon.GalleryStrips.SurfaceFor(gallery));
    });

    [Fact]
    public void Borrowed_group_popup_reuses_its_strip_presenter_and_native_editor_without_disposal() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1200, Height = 600 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon); var gallery = GalleryTests.Attach(ribbon);
        gallery.IsInRibbon = true; gallery.Items.AddRange(new RibbonGalleryTile { Text = "First" }, new RibbonGalleryTile { Text = "Second" });
        var group = (RibbonGroup)gallery.Parent!; var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained native editor" }; group.Items.Add(field);
        form.Show(); Application.DoEvents(); var strip = ribbon.GalleryStrips.SurfaceFor(gallery)!; var editor = ribbon.Editors.EditorFor(field)!; nint handle = editor.Handle; editor.Select(2, 3);
        var reference = new RibbonQuickAccessGroupReference { Source = group }; ribbon.QuickAccessItems.Add(reference); Assert.False(strip.Visible);
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Element == reference)); Assert.Same(strip, ribbon.GalleryStrips.SurfaceFor(gallery)); Assert.Same(ribbon.Popups.Top, strip.Parent); Assert.Equal(handle, editor.Handle);
        ribbon.Popups.CloseAll(true); Assert.False(strip.IsDisposed); Assert.Null(strip.Parent); Assert.Equal(handle, editor.Handle);
        ribbon.QuickAccessItems.Remove(reference); Assert.Same(ribbon, strip.Parent); Assert.True(strip.Visible); Assert.Equal(handle, editor.Handle); Assert.Equal(2, editor.SelectionStart); Assert.Equal(3, editor.SelectionLength);
        reference.Dispose(); Assert.False(gallery.IsDisposed);
    });

    [Fact]
    public void Preview_ownership_prevents_stale_presentations_from_cancelling_a_new_preview() => FoundationTests.Sta(() =>
    {
        using var gallery = new RibbonGalleryItem(); var a = new RibbonGalleryTile(); var b = new RibbonGalleryTile(); gallery.Items.AddRange(a, b);
        var strip = new object(); var popup = new object(); int cancelled = 0; gallery.ItemPreviewCancelled += (_, _) => cancelled++;
        gallery.Preview(a, strip); gallery.Preview(b, popup); gallery.CancelPreview(strip); Assert.Same(b, gallery.PreviewItem); Assert.Equal(0, cancelled);
        gallery.CancelPreview(popup); Assert.Null(gallery.PreviewItem); Assert.Equal(1, cancelled);
        gallery.Preview(a, popup); gallery.SelectedItem = b; Assert.Null(gallery.PreviewItem); Assert.Equal(2, cancelled);
    });
    private static void Key(Control control, Keys key)
    {
        var message = Message.Create(control.Handle, 0x100, (nint)(int)key, 0);
        typeof(Control).GetMethod("ProcessCmdKey", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(control, [message, key]);
    }
}
