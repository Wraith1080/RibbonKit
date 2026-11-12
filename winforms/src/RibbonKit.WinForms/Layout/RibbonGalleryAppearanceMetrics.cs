namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonGalleryChromeDefaults(int Inset, int BorderWidth);

internal readonly record struct RibbonGalleryAppearanceMetrics(RibbonTheme Theme, int Dpi)
{
    private RibbonGalleryAppearanceSettings? A => Theme.GalleryAppearance;
    private static RibbonGalleryChromeDefaults D => RibbonPresentationResolver.GalleryChrome;
    internal Rectangle Bounds(Rectangle bounds, RibbonGalleryChromeKind kind)
    {
        int? authored = kind switch { RibbonGalleryChromeKind.Header => A?.HeaderInset, RibbonGalleryChromeKind.Strip => A?.StripInset, RibbonGalleryChromeKind.Utility => A?.UtilityInset, _ => A?.TileInset };
        int inset = authored is { } n ? Math.Min(RibbonLayoutEngine.Scale(n, Dpi), Math.Max(0, (Math.Min(bounds.Width, bounds.Height) - 1) / 2)) : D.Inset;
        return Rectangle.Inflate(bounds, -inset, -inset);
    }
    internal int Radius(int actualRadius, RibbonGalleryChromeKind kind) => (kind switch { RibbonGalleryChromeKind.Header => A?.HeaderCornerRadius, RibbonGalleryChromeKind.Strip => A?.StripCornerRadius, RibbonGalleryChromeKind.Utility => A?.UtilityCornerRadius, _ => A?.TileCornerRadius }) is { } n ? RibbonLayoutEngine.Scale(n, Dpi) : actualRadius;
    internal Color Border(Color fallback, RibbonGalleryChromeKind kind) => (kind switch { RibbonGalleryChromeKind.Header => A?.HeaderBorderColor, RibbonGalleryChromeKind.Strip => A?.StripBorderColor, RibbonGalleryChromeKind.Utility => A?.UtilityBorderColor, _ => A?.TileBorderColor }) is { IsEmpty: false } ink ? ink : fallback;
    internal int BorderWidth(Rectangle bounds, Color ink, RibbonGalleryChromeKind kind) => (kind switch { RibbonGalleryChromeKind.Header => A?.HeaderBorderWidth, RibbonGalleryChromeKind.Strip => A?.StripBorderWidth, RibbonGalleryChromeKind.Utility => A?.UtilityBorderWidth, _ => A?.TileBorderWidth }) is { } n
        ? Math.Min(RibbonLayoutEngine.Scale(n, Dpi), Math.Max(0, Math.Min(bounds.Width, bounds.Height) / 2)) : ink.A == 0 ? 0 : D.BorderWidth;
}
