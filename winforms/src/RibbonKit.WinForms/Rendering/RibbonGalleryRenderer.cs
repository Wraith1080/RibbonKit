using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal enum RibbonGalleryChromeKind { None, Tile, Header }

// Built-in decoration only; actual renderer hooks, content slots and model/presenter state remain shared.
internal static class RibbonGalleryRenderer
{
    internal static void DrawTileState(RibbonRenderer owner, RibbonPaintContext c, bool selected)
    {
        if (!selected && !c.Hot && !c.Pressed) return;
        var border = new RibbonGalleryAppearanceMetrics(c.Theme, c.DeviceDpi).Border(selected ? c.Theme.Accent : Color.Transparent, header: false);
        owner.PaintGalleryChrome(c, c.Pressed ? c.Theme.Pressed : c.Hot ? c.Theme.Hover : c.Theme.Checked, border, header: false);
    }
    internal static void DrawHeading(RibbonRenderer owner, RibbonPaintContext c, string text, bool office)
    {
        c = c with { Hot = false, Pressed = false, Focused = false };
        var a = c.Theme.GalleryAppearance;
        var fill = a?.HeaderBackground ?? (office ? c.Theme.GalleryHeadingBackground : (RibbonFill?)null);
        var border = new RibbonGalleryAppearanceMetrics(c.Theme, c.DeviceDpi).Border(Color.Transparent, header: true);
        if (fill is not null || border.A != 0) owner.PaintGalleryChrome(c, fill ?? new(Color.Transparent), border, header: true);
        using var font = new Font(c.Font, a?.HeaderBold == false ? c.Font.Style & ~FontStyle.Bold : c.Font.Style | FontStyle.Bold);
        var textBounds = new RibbonGalleryMetrics(c.Theme, c.Density).HeadingTextBounds(c.Bounds, c.DeviceDpi);
        owner.DrawText(c with { Bounds = textBounds, Font = font }, text, office ? c.Theme.GalleryHeadingForeground : c.Theme.SecondaryText,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
    }
}

public partial class RibbonRenderer
{
    internal void PaintGalleryChrome(RibbonPaintContext context, RibbonFill fill, Color border, bool header) =>
        DrawChrome(context with { GalleryChrome = header ? RibbonGalleryChromeKind.Header : RibbonGalleryChromeKind.Tile }, fill, border);
    private bool TryPaintGalleryChrome(RibbonPaintContext context, RibbonFill fill, Color border, int actualRadius)
    {
        bool header = context.GalleryChrome == RibbonGalleryChromeKind.Header;
        var a = context.Theme.GalleryAppearance;
        if (a is null || (header ? !a.HasHeaderChrome : context.GalleryChrome != RibbonGalleryChromeKind.Tile || !a.HasTileChrome)) return false;
        var m = new RibbonGalleryAppearanceMetrics(context.Theme, context.DeviceDpi); var bounds = m.Bounds(context.Bounds, header);
        border = m.Border(border, header);
        PaintRounded(context with { Bounds = bounds }, m.Radius(actualRadius, header), fill, border, m.BorderWidth(bounds, border, header));
        return true;
    }
}
