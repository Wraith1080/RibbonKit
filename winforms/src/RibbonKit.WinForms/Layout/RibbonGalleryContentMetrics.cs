namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonGalleryContentDefaults(int LabelLines, int PaintReserve, int MeasureReserve, int ArtworkHorizontalExtra, int ArtworkTopExtra, int ExpandHeight, int BarWidth, int BarInset, int BarStroke);

internal readonly record struct RibbonGalleryContentMetrics(RibbonTheme? Theme, int Dpi, RibbonDensity Density)
{
    private RibbonGalleryContentSettings? A => Theme?.GalleryContent;
    private static RibbonGalleryContentDefaults D => RibbonPresentationResolver.GalleryContent;
    private int Padding => new RibbonGalleryMetrics(Theme, Density).TilePadding;
    private int S(int n) => RibbonLayoutEngine.Scale(n, Dpi);
    internal int Lines => A?.LabelMaxLines ?? D.LabelLines;
    internal int MinimumTileHeight(Font font) => Math.Max(0, Lines * font.Height + S((A?.ArtworkHeightReserve ?? D.MeasureReserve) + 3 * Padding)
        + (A?.ArtworkTopInset is { } n ? S(n) - S(Padding + D.ArtworkTopExtra) : 0));
    internal (Rectangle Label, Rectangle Artwork) Content(Rectangle bounds, Font font)
    {
        int reserve = S((A?.ArtworkHeightReserve ?? D.PaintReserve) + 3 * Padding) + (A?.ArtworkTopInset is { } n ? S(n) - S(Padding + D.ArtworkTopExtra) : 0);
        int height = Math.Min(Lines * font.Height, Math.Max(0, bounds.Height - Math.Max(0, reserve)));
        int side = Math.Min(S(Padding), Math.Max(0, bounds.Width / 2)), bottom = Math.Min(S(Padding), Math.Max(0, bounds.Height));
        var label = new Rectangle(bounds.Left + side, Math.Max(bounds.Top, bounds.Bottom - bottom - height), Math.Max(0, bounds.Width - 2 * side), height);
        int artSide = Math.Min(S(A?.ArtworkHorizontalInset ?? Padding + D.ArtworkHorizontalExtra), Math.Max(0, bounds.Width / 2));
        int top = Math.Min(bounds.Bottom, bounds.Top + S(A?.ArtworkTopInset ?? Padding + D.ArtworkTopExtra));
        var art = Rectangle.FromLTRB(bounds.Left + artSide, top, bounds.Right - artSide, Math.Max(top, label.Top - S(Padding)));
        return (label, art);
    }
    internal TextFormatFlags LabelFlags
    {
        get
        {
            var alignment = A?.LabelAlignment ?? ContentAlignment.MiddleCenter;
            var flags = TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
            if (alignment is ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter) flags |= TextFormatFlags.HorizontalCenter;
            if (alignment is ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight) flags |= TextFormatFlags.Right;
            if (alignment is ContentAlignment.MiddleLeft or ContentAlignment.MiddleCenter or ContentAlignment.MiddleRight) flags |= TextFormatFlags.VerticalCenter;
            if (alignment is ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight) flags |= TextFormatFlags.Bottom;
            return flags;
        }
    }
    internal Rectangle ExpandSlot(Rectangle slot)
    {
        int height = Math.Min(Math.Max(0, slot.Height), S(A?.ExpandGlyphHeight ?? D.ExpandHeight));
        return new(slot.Left, slot.Top + (slot.Height - height) / 2, slot.Width, height);
    }
    internal int BarInset => S(A?.ExpandBarInset ?? D.BarInset);
    internal float BarStroke => A?.ExpandBarStrokeWidth is { } n ? (float)(n * Dpi / 96d) : Math.Max(1, S(D.BarStroke));
    internal float BarWidth(Rectangle glyph) => Math.Min(S(A?.ExpandBarWidth ?? D.BarWidth), glyph.Width - 2 * BarInset);
    internal bool HasAuthoredBar => A?.ExpandBarWidth is not null || A?.ExpandBarInset is not null || A?.ExpandBarStrokeWidth is not null;
    internal Color GlyphInk(Color fallback) => A is { UtilityGlyphColor.IsEmpty: false } a ? a.UtilityGlyphColor : fallback;
}
