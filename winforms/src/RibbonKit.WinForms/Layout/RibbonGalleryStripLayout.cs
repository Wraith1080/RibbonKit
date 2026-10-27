namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonGalleryStripTile(RibbonGalleryTile Tile, Rectangle Bounds);
internal sealed record RibbonGalleryStripLayout(Rectangle Viewport, Rectangle Up, Rectangle Down, Rectangle Expand, IReadOnlyList<RibbonGalleryStripTile> Tiles, int Row, int LastRow)
{
    internal const int UtilityWidth = 18, Padding = 4;
    internal static RibbonGalleryStripLayout Build(RibbonGalleryItem source, Size size, int dpi, int row)
    {
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        int padding = S(Padding), utility = Math.Min(Math.Max(0, size.Width - 2 * padding), S(UtilityWidth));
        int leftInset = Math.Min(padding, size.Width), topInset = Math.Min(padding, size.Height);
        int x = Math.Max(0, size.Width - padding - utility);
        var viewport = Rectangle.FromLTRB(leftInset, topInset, Math.Max(leftInset, x - padding), Math.Max(topInset, size.Height - padding));
        var visible = source.Items.OfType<RibbonGalleryTile>().Where(t => t.Visible && !t.IsDisposed).ToArray();
        int columns = source.InRibbonColumnCount;
        int last = Math.Max(0, (visible.Length - 1) / columns); row = Math.Clamp(row, 0, last);
        var tiles = new List<RibbonGalleryStripTile>();
        for (int column = 0, index = row * columns; column < columns && index < visible.Length; column++, index++)
        {
            int left = viewport.Left + viewport.Width * column / columns;
            int right = viewport.Left + viewport.Width * (column + 1) / columns;
            tiles.Add(new(visible[index], Rectangle.FromLTRB(left, viewport.Top, right, viewport.Bottom)));
        }
        int buttonsHeight = Math.Max(0, size.Height - 2 * padding);
        int first = buttonsHeight / 3, second = 2 * buttonsHeight / 3;
        // Equal thirds fill the interior; distribute rounding across shared
        // edges so hit testing and painting preserve the inset at every DPI.
        return new(viewport, new(x, topInset, utility, first), new(x, topInset + first, utility, second - first),
            new(x, topInset + second, utility, buttonsHeight - second), tiles, row, last);
    }
}
