using System.ComponentModel;
using System.Reflection;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GalleryTests
{
    [Fact]
    public void Gallery_owns_tiles_and_selection_bindings_but_borrows_artwork_and_rejects_foreign_presentations() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BindingContext = new BindingContext() }; var gallery = Attach(ribbon);
        using var image = new Bitmap(24, 12); image.SetPixel(0, 0, Color.Red);
        var a = new RibbonGalleryTile { Id = "tile.a", Text = "First", Image = image }; var b = new RibbonGalleryTile { Text = "Second" }; gallery.Items.AddRange(a, b);
        Assert.Same(ribbon, a.Ribbon); Assert.Same(gallery, a.Parent); Assert.Same(ribbon.BindingContext, a.BindingContext);
        Assert.Equal(DesignerSerializationVisibility.Content, TypeDescriptor.GetProperties(gallery)[nameof(gallery.Items)]!.SerializationVisibility);
        Assert.Throws<ArgumentException>(() => gallery.Items.Add(new RibbonButtonItem()));
        Assert.Throws<ArgumentException>(() => ribbon.FileMenu.Items.Add(new RibbonGalleryTile()));
        using var otherGallery = new RibbonGalleryItem(); Assert.Throws<InvalidOperationException>(() => otherGallery.Items.Add(a));
        Assert.Throws<ArgumentException>(() => ribbon.Tabs.Add(new RibbonTab { Id = a.Id }));
        using var galleryReference = new RibbonQuickAccessDropDownReference { Source = gallery }; Assert.Same(gallery, galleryReference.Source);
        Assert.Throws<ArgumentException>(() => gallery.SelectedItem = new RibbonGalleryTile());
        using var binding = new BindingSource(); var state = new State { Choice = a }; binding.DataSource = state;
        gallery.DataBindings.Add(new Binding(nameof(gallery.SelectedItem), binding, nameof(state.Choice), true, DataSourceUpdateMode.OnPropertyChanged));
        Assert.Same(a, gallery.SelectedItem); int commits = 0; gallery.SelectionCommitted += (_, _) => commits++;
        state.Choice = b; Assert.Same(b, gallery.SelectedItem); Assert.Equal(0, commits);
        gallery.SelectedItem = a; Assert.Same(a, state.Choice);
        a.Enabled = false; a.Visible = false; Assert.Same(a, gallery.SelectedItem);
        gallery.Items.Remove(a); Assert.Null(gallery.SelectedItem); Assert.Null(state.Choice); Assert.False(a.IsDisposed);
        gallery.Items.Add(a); ribbon.Dispose(); Assert.True(a.IsDisposed); Assert.True(b.IsDisposed); Assert.Equal(Color.Red.ToArgb(), image.GetPixel(0, 0).ToArgb());
    });

    [Fact]
    public void Preview_cancel_and_commit_have_distinct_events_and_survive_reentrant_mutation() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var gallery = Attach(ribbon); var a = new RibbonGalleryTile { Text = "A" }; var b = new RibbonGalleryTile { Text = "B" };
        gallery.Items.AddRange(a, b); gallery.SelectedItem = a; var events = new List<string>();
        gallery.ItemPreview += (_, e) => events.Add("preview:" + e.Item.Text);
        gallery.ItemPreviewCancelled += (_, _) => events.Add("cancel");
        gallery.SelectedItemChanged += (_, _) => events.Add("selected:" + gallery.SelectedItem?.Text);
        gallery.SelectionCommitted += (_, _) => events.Add("commit:" + gallery.SelectedItem?.Text);
        gallery.Preview(b); gallery.Preview(b); Assert.Same(a, gallery.SelectedItem); gallery.CancelPreview(); gallery.CancelPreview();
        Assert.Equal(new[] { "preview:B", "cancel" }, events); events.Clear();
        gallery.Preview(b); b.PerformClick(); Assert.Equal(new[] { "preview:B", "selected:B", "commit:B" }, events); events.Clear();
        b.PerformClick(); Assert.Equal(new[] { "commit:B" }, events); events.Clear();
        gallery.Preview(a); gallery.SelectedItem = a; Assert.Equal(new[] { "preview:A", "cancel", "selected:A" }, events); events.Clear();
        gallery.Preview(b); b.Enabled = false; Assert.Null(gallery.PreviewItem); Assert.Equal(new[] { "preview:B", "cancel" }, events); events.Clear();
        b.Enabled = true;
        EventHandler<RibbonGalleryPreviewEventArgs> remove = (_, e) => gallery.Items.Remove(e.Item);
        gallery.ItemPreview += remove; gallery.Preview(b); Assert.DoesNotContain(b, gallery.Items); Assert.Null(gallery.PreviewItem);
        Assert.Equal(new[] { "preview:B", "cancel" }, events); gallery.ItemPreview -= remove;
        gallery.Items.Add(b); events.Clear();
        gallery.SelectedItemChanged += (_, _) => { if (gallery.SelectedItem == b) b.Dispose(); };
        b.PerformClick(); Assert.Null(gallery.SelectedItem); Assert.DoesNotContain("commit:B", events);
    });

    [Fact]
    public void Grouped_grid_geometry_is_dpi_scaled_and_bounded_without_reordering_or_mutating_selection() => FoundationTests.Sta(() =>
    {
        using var gallery = new RibbonGalleryItem { ColumnCount = 4, MaxVisibleRows = 2 };
        gallery.Items.AddRange(Enumerable.Range(0, 13).Select(i => new RibbonGalleryTile { Text = "Tile " + i, GroupName = i < 5 ? "First" : "Second", Visible = i != 1 }).ToArray());
        var selected = (RibbonGalleryTile)gallery.Items[^1]; gallery.SelectedItem = selected;
        Assert.Throws<ArgumentOutOfRangeException>(() => gallery.ColumnCount = 0); Assert.Throws<ArgumentOutOfRangeException>(() => gallery.TileWidth = 20);
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int width in new[] { 220, 1200 })
        foreach (int fontSize in new[] { 9, 18 })
        {
            using var font = new Font("Segoe UI", fontSize * dpi / 96f);
            var layout = RibbonGalleryLayout.Build(gallery, font, dpi, new(width * dpi / 96, 600 * dpi / 96));
            Assert.Equal(12, layout.Targets.Count); Assert.Equal(new[] { "First", "Second" }, layout.Headings.Select(h => h.Text));
            Assert.InRange(layout.Columns, 1, 4); Assert.True(layout.ViewportHeight < layout.Extent.Height);
            Assert.All(layout.Targets, t => { Assert.True(t.Bounds.Right <= layout.Extent.Width); Assert.True(t.Bounds.Height >= 2 * font.Height); Assert.DoesNotContain(layout.Headings, h => h.Bounds.IntersectsWith(t.Bounds)); });
            Assert.Equal(gallery.Items.Where(i => i.Visible), layout.Targets.Select(t => t.Element)); Assert.Same(selected, gallery.SelectedItem);
        }
    });

    [Fact]
    public void Gallery_popup_preserves_selected_row_and_keyboard_preview_then_cancels_one_nested_level() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900, Height = 550 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon);
        var gallery = Attach(ribbon); gallery.MaxVisibleRows = 2;
        gallery.Items.AddRange(Enumerable.Range(0, 24).Select(i => new RibbonGalleryTile { Text = "Tile " + i, GroupName = i < 12 ? "First" : "Second", Enabled = i != 22 }).ToArray());
        gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[20]; int previews = 0, cancelled = 0, commits = 0;
        gallery.ItemPreview += (_, _) => previews++; gallery.ItemPreviewCancelled += (_, _) => cancelled++; gallery.SelectionCommitted += (_, _) => commits++;
        form.Show(); Application.DoEvents(); gallery.ShowDropDown(); var popup = ribbon.Popups.Top!;
        Assert.Same(gallery.SelectedItem, popup.FocusedElement); Assert.True(popup.Targets.Single(t => t.Element == gallery.SelectedItem).VisibleBounds.Height > 0); Assert.Equal(0, previews);
        Key(popup, Keys.Left); Assert.Same(gallery.Items[19], gallery.PreviewItem); Assert.Same(gallery.Items[20], gallery.SelectedItem);
        Key(popup, Keys.End); Assert.Same(gallery.Items[23], gallery.PreviewItem); Key(popup, Keys.Enter);
        Assert.Same(gallery.Items[23], gallery.SelectedItem); Assert.False(ribbon.Popups.HasOpen); Assert.Equal(1, commits); Assert.Equal(0, cancelled);
        gallery.ShowDropDown(); popup = ribbon.Popups.Top!; Key(popup, Keys.Home); Key(popup, Keys.Escape);
        Assert.Equal(1, cancelled); Assert.Same(gallery.Items[23], gallery.SelectedItem); Assert.False(ribbon.Popups.HasOpen);
        var parent = new RibbonDropDownItem { Text = "Parent" }; var group = (RibbonGroup)gallery.Parent!; group.Items.Remove(gallery); parent.Items.Add(gallery); group.Items.Add(parent);
        parent.ShowDropDown(); gallery.ShowDropDown(); Assert.Equal(2, ribbon.Popups.Surfaces.Count());
        popup = ribbon.Popups.Top!; Key(popup, Keys.Home); Key(popup, Keys.Escape); Assert.Single(ribbon.Popups.Surfaces); Assert.Equal(2, cancelled);
        gallery.ShowDropDown(); popup = ribbon.Popups.Top!; Key(popup, Keys.Home); gallery.Enabled = false; Assert.Single(ribbon.Popups.Surfaces); Assert.Equal(3, cancelled);
    });

    internal static RibbonGalleryItem Attach(RibbonControl ribbon)
    {
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Galleries" }; var gallery = new RibbonGalleryItem { Text = "Styles", KeyTip = "S" };
        group.Items.Add(gallery); tab.Groups.Add(group); ribbon.Tabs.Add(tab); return gallery;
    }
    [Fact]
    public void Gallery_renderers_preserve_artwork_aspect_and_keep_paint_separate_from_selection_and_preview() => FoundationTests.Sta(() =>
    {
        using var gallery = new RibbonGalleryItem(); using var image = new Bitmap(30, 10); using (var graphics = Graphics.FromImage(image)) graphics.Clear(Color.ForestGreen);
        var tile = new RibbonGalleryTile { Text = "Landscape image", LargeImage = image }; gallery.Items.Add(tile); gallery.SelectedItem = tile;
        int events = 0; gallery.ItemPreview += (_, _) => events++; gallery.SelectionCommitted += (_, _) => events++; gallery.SelectedItemChanged += (_, _) => events++;
        using var office = new RibbonOffice2024Renderer(); using var neutral = new RibbonRenderer();
        foreach (var renderer in new[] { neutral, office })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (bool enabled in new[] { true, false })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            tile.Enabled = enabled; var bounds = new Rectangle(0, 0, S(120), S(100));
            using var font = new Font("Segoe UI", 9 * dpi / 96f); using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap);
            var theme = RibbonThemes.Office2024Light; graphics.Clear(theme.Surface);
            renderer.DrawGalleryTile(new(graphics, bounds, theme, font, dpi, true, false, true), tile, true);
            int labelTop = bounds.Bottom - S(4) - 2 * font.Height;
            var center = bitmap.GetPixel(bounds.Width / 2, (S(6) + labelTop - S(4)) / 2);
            if (enabled) Assert.Equal(Color.ForestGreen.ToArgb(), center.ToArgb()); else Assert.NotEqual(Color.ForestGreen.ToArgb(), center.ToArgb());
            Assert.Same(tile, gallery.SelectedItem); Assert.Equal(0, events);
        }
        gallery.Dispose(); Assert.Equal(Color.ForestGreen.ToArgb(), image.GetPixel(0, 0).ToArgb());
    });
    private static void Key(Control control, Keys keys)
    {
        var message = Message.Create(control.Handle, 0x100, (nint)(int)keys, 0);
        typeof(Control).GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, [message, keys]);
    }
    private sealed class State : INotifyPropertyChanged
    {
        private RibbonGalleryTile? _choice;
        public RibbonGalleryTile? Choice { get => _choice; set { if (_choice == value) return; _choice = value; PropertyChanged?.Invoke(this, new(nameof(Choice))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
