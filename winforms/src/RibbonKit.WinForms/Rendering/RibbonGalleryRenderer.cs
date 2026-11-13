using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal enum RibbonGalleryChromeKind { None, Tile, Header, Strip, Utility }

// Built-in decoration only; actual renderer hooks, content slots and model/presenter state remain shared.
internal static class RibbonGalleryRenderer
{
    internal static void DrawTileState(RibbonRenderer owner, RibbonPaintContext c, bool selected)
    {
        if (!selected && !c.Hot && !c.Pressed) return;
        var border = new RibbonGalleryAppearanceMetrics(c.Theme, c.DeviceDpi).Border(selected ? c.Theme.Accent : Color.Transparent, RibbonGalleryChromeKind.Tile);
        owner.PaintGalleryChrome(c, c.Theme.CommandStateFill(c.Hot, c.Pressed, selected), border, RibbonGalleryChromeKind.Tile);
    }
    internal static void DrawHeading(RibbonRenderer owner, RibbonPaintContext c, string text, bool office)
    {
        c = c with { Hot = false, Pressed = false, Focused = false };
        var a = c.Theme.GalleryAppearance;
        var fill = a?.HeaderBackground ?? (office ? c.Theme.GalleryHeadingBackground : (RibbonFill?)null);
        var border = new RibbonGalleryAppearanceMetrics(c.Theme, c.DeviceDpi).Border(Color.Transparent, RibbonGalleryChromeKind.Header);
        if (fill is not null || border.A != 0) owner.PaintGalleryChrome(c, fill ?? new(Color.Transparent), border, RibbonGalleryChromeKind.Header);
        using var font = new Font(c.Font, a?.HeaderBold == false ? c.Font.Style & ~FontStyle.Bold : c.Font.Style | FontStyle.Bold);
        var textBounds = new RibbonGalleryMetrics(c.Theme, c.Density).HeadingTextBounds(c.Bounds, c.DeviceDpi);
        owner.DrawText(c with { Bounds = textBounds, Font = font }, text, office ? c.Theme.GalleryHeadingForeground : c.Theme.SecondaryText,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
    }
    internal static void DrawStripFrame(RibbonRenderer owner, RibbonPaintContext c)
    {
        c = c with { Hot = false, Pressed = false, Focused = false };
        var border = new RibbonGalleryAppearanceMetrics(c.Theme, c.DeviceDpi).Border(c.Theme.PopupBorder, RibbonGalleryChromeKind.Strip);
        owner.PaintGalleryChrome(c, c.Theme.GalleryAppearance?.StripBackground ?? new(c.Theme.Surface), border, RibbonGalleryChromeKind.Strip);
    }
}

public partial class RibbonRenderer
{
    internal void PaintGalleryChrome(RibbonPaintContext context, RibbonFill fill, Color border, RibbonGalleryChromeKind kind) =>
        DrawChrome(context with { GalleryChrome = kind }, fill, border);
    private bool TryPaintGalleryChrome(RibbonPaintContext context, RibbonFill fill, Color border, int actualRadius)
    {
        var a = context.Theme.GalleryAppearance;
        bool authored = context.GalleryChrome switch { RibbonGalleryChromeKind.Header => a?.HasHeaderChrome == true, RibbonGalleryChromeKind.Tile => a?.HasTileChrome == true, RibbonGalleryChromeKind.Strip => a?.HasStripChrome == true, RibbonGalleryChromeKind.Utility => a?.HasUtilityChrome == true, _ => false };
        if (!authored) return false;
        var m = new RibbonGalleryAppearanceMetrics(context.Theme, context.DeviceDpi); var bounds = m.Bounds(context.Bounds, context.GalleryChrome);
        border = m.Border(border, context.GalleryChrome);
        PaintRounded(context with { Bounds = bounds }, m.Radius(actualRadius, context.GalleryChrome), fill, border, m.BorderWidth(bounds, border, context.GalleryChrome));
        return true;
    }
}
