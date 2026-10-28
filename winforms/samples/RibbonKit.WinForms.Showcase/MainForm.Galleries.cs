namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private readonly List<RibbonGalleryItem> _sampleGalleries = [];
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
    internal async Task CaptureGalleries(string directory)
    {
        Directory.CreateDirectory(directory);
        Width = Math.Min(1600, Screen.FromControl(this).WorkingArea.Width - 32);
        Activate(); await Task.Delay(100);
        using (var strip = new Bitmap(Width, Height))
        {
            using var graphics = Graphics.FromImage(strip); graphics.CopyFromScreen(Location, Point.Empty, Size);
            strip.Save(Path.Combine(directory, "in-ribbon-styles.png"));
        }
        foreach (var gallery in _sampleGalleries)
        {
            if (!ContainsFocus) throw new InvalidOperationException("Showcase lost foreground before gallery capture.");
            gallery.ShowDropDown(); await Task.Delay(100);
            if (!gallery.IsDropDownOpen || Control.FromHandle(GetForegroundWindow()) is not ToolStripDropDown popup) throw new InvalidOperationException("Expected shown Showcase gallery.");
            var bounds = Rectangle.Union(Bounds, popup.Bounds);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            bitmap.Save(Path.Combine(directory, gallery.Id + ".png"));
            gallery.CloseDropDown();
        }
    }
    private void InitializeGalleries()
    {
        var styles = new RibbonGalleryItem { Id = "gallery.styles", Text = "Styles", KeyTip = "GS", IsInRibbon = true, InRibbonColumnCount = 3, ColumnCount = 3, TileWidth = 100, TileHeight = 80, MaxVisibleRows = 3, SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small), Icon = new SampleIcon(0), ScreenTipDescription = "Preview a document style, then click to apply it. Escape restores the selected style." };
        var colors = new RibbonGalleryItem { Id = "gallery.colors", Text = "Text color", KeyTip = "GC", ColumnCount = 4, TileWidth = 64, TileHeight = 68, MaxVisibleRows = 3, Icon = new SampleIcon(7) };
        _sampleGalleries.AddRange([styles, colors]);
        var group = new RibbonGroup { Id = "group.galleries", Text = "Galleries", ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse }; group.Items.AddRange(styles, colors); home.Groups.Add(group);
        var fonts = new Dictionary<RibbonGalleryTile, Font>();
        foreach (var (text, points, style, heading) in new[] {
            ("Normal", 10f, FontStyle.Regular, "Document"), ("Emphasis", 10f, FontStyle.Italic, "Document"), ("Strong", 10f, FontStyle.Bold, "Document"),
            ("Title", 22f, FontStyle.Bold, "Headings"), ("Heading 1", 18f, FontStyle.Bold, "Headings"), ("Heading 2", 14f, FontStyle.Bold, "Headings"),
            ("Subtitle", 14f, FontStyle.Italic, "Headings"), ("Caption", 9f, FontStyle.Italic, "Headings"), ("Unavailable", 10f, FontStyle.Regular, "Headings") })
        {
            var tile = new RibbonGalleryTile { Id = "style." + text.Replace(" ", "").ToLowerInvariant(), Text = text, GroupName = heading, Icon = new StyleTileIcon(style, points), Enabled = text != "Unavailable" };
            styles.Items.Add(tile); fonts[tile] = new Font("Segoe UI", points, style);
        }
        void ApplyStyle(RibbonGalleryTile? tile) { if (tile is not null && fonts.TryGetValue(tile, out var font)) document.Font = font; }
        styles.SelectedItemChanged += (_, _) => ApplyStyle(styles.SelectedItem);
        styles.ItemPreview += (_, e) => ApplyStyle(e.Item);
        styles.ItemPreviewCancelled += (_, _) => ApplyStyle(styles.SelectedItem);
        styles.SelectionCommitted += (_, _) => status.Text = "Style applied: " + styles.SelectedItem?.Text;
        styles.SelectedItem = (RibbonGalleryTile)styles.Items[0];
        var palette = new Dictionary<RibbonGalleryTile, Color>();
        foreach (var (text, color) in new[] { ("Black", Color.Black), ("Blue", Color.RoyalBlue), ("Teal", Color.Teal), ("Green", Color.ForestGreen),
            ("Red", Color.Firebrick), ("Orange", Color.DarkOrange), ("Purple", Color.Purple), ("Gray", Color.DimGray),
            ("Navy", Color.Navy), ("Brown", Color.SaddleBrown), ("Pink", Color.DeepPink), ("Olive", Color.Olive) })
        {
            var tile = new RibbonGalleryTile { Id = "color." + text.ToLowerInvariant(), Text = text, GroupName = "Text colors", Icon = new ColorTileIcon(color) };
            colors.Items.Add(tile); palette[tile] = color;
        }
        void ApplyColor(RibbonGalleryTile? tile) { if (tile is not null && palette.TryGetValue(tile, out var color)) document.ForeColor = color; }
        colors.SelectedItemChanged += (_, _) => ApplyColor(colors.SelectedItem);
        colors.ItemPreview += (_, e) => ApplyColor(e.Item);
        colors.ItemPreviewCancelled += (_, _) => ApplyColor(colors.SelectedItem);
        colors.SelectionCommitted += (_, _) => status.Text = "Text color applied: " + colors.SelectedItem?.Text;
        colors.SelectedItem = (RibbonGalleryTile)colors.Items[0];
        Disposed += (_, _) => { foreach (var font in fonts.Values) font.Dispose(); };
    }
    private sealed class StyleTileIcon(FontStyle style, float size) : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        {
            using var paper = new SolidBrush(Color.White); graphics.FillRectangle(paper, bounds);
            using var line = new Pen(context.Enabled ? Color.SlateGray : SystemColors.GrayText); graphics.DrawRectangle(line, bounds.X, bounds.Y, Math.Max(0, bounds.Width - 1), Math.Max(0, bounds.Height - 1));
            using var font = new Font("Segoe UI", Math.Min(size, 15) * context.DeviceDpi / 96f, style, GraphicsUnit.Pixel);
            TextRenderer.DrawText(graphics, "Aa", font, bounds, context.Foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
    private sealed class ColorTileIcon(Color color) : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        {
            using var fill = new SolidBrush(context.Enabled ? color : SystemColors.GrayText); graphics.FillRectangle(fill, bounds);
        }
    }
}
