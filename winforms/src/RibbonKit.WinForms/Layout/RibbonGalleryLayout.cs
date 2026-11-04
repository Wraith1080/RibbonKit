namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonGalleryHeading(string Text, Rectangle Bounds);
internal sealed record RibbonGalleryLayout(Size Extent, int ViewportHeight, int Columns, IReadOnlyList<RibbonTarget> Targets, IReadOnlyList<RibbonGalleryHeading> Headings)
{
    internal static RibbonGalleryLayout Build(RibbonGalleryItem gallery, Font font, int dpi, Size workArea)
    {
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        var spacing = new RibbonGalleryMetrics(gallery.Ribbon?.Theme, gallery.Ribbon?.Density ?? RibbonDensity.Compact);
        int maxWidth = Math.Max(1, workArea.Width - S(24));
        int padding = Math.Min(S(spacing.ContentPadding), Math.Max(0, (maxWidth - 1) / 2));
        int scroll = SystemInformation.GetVerticalScrollBarWidthForDpi(dpi);
        var metrics = RibbonDensityMetrics.For(gallery.Ribbon?.Density ?? RibbonDensity.Compact);
        int tileWidth = Math.Min(S(Math.Max(gallery.TileWidth, metrics.GalleryWidth)), Math.Max(1, maxWidth - 2 * padding - scroll));
        int columns = Math.Clamp((maxWidth - 2 * padding - scroll) / Math.Max(1, tileWidth), 1, gallery.ColumnCount);
        int tileHeight = Math.Max(S(Math.Max(gallery.TileHeight, metrics.GalleryHeight)), 2 * font.Height + S(24 + 3 * spacing.TilePadding));
        int headingHeight = font.Height + S(2 * spacing.HeadingVerticalPadding), y = padding, column = 0;
        string? group = null;
        var targets = new List<RibbonTarget>(); var headings = new List<RibbonGalleryHeading>();
        int width = columns * tileWidth + 2 * padding;
        foreach (var tile in gallery.Items.OfType<RibbonGalleryTile>().Where(t => t.Visible && !t.IsDisposed))
        {
            if (group != tile.GroupName)
            {
                if (column > 0) { y += tileHeight; column = 0; }
                group = tile.GroupName;
                if (group.Length > 0) { headings.Add(new(group, new(padding, y, columns * tileWidth, headingHeight))); y += headingHeight; }
            }
            targets.Add(new(TargetKind.Item, tile, new(padding + column * tileWidth, y, tileWidth, tileHeight), Rectangle.Empty, RibbonItemSize.Large));
            if (++column == columns) { y += tileHeight; column = 0; }
        }
        if (column > 0) y += tileHeight;
        int height = Math.Max(S(32), y + padding);
        int viewport = Math.Max(1, Math.Min(Math.Min(height, workArea.Height - S(32)),
            gallery.MaxVisibleRows * tileHeight + 2 * padding + (headings.Count > 0 ? headingHeight : 0)));
        return new(new(width, height), viewport, columns, targets, headings);
    }
}
