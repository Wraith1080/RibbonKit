namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonGalleryChromeDefaults(int Inset, int BorderWidth);

internal readonly record struct RibbonGalleryAppearanceMetrics(RibbonTheme Theme, int Dpi)
{
    private RibbonGalleryAppearanceSettings? A => Theme.GalleryAppearance;
    private static RibbonGalleryChromeDefaults D => RibbonPresentationResolver.GalleryChrome;
    internal Rectangle Bounds(Rectangle bounds, bool header)
    {
        int? authored = header ? A?.HeaderInset : A?.TileInset;
        int inset = authored is { } n ? Math.Min(RibbonLayoutEngine.Scale(n, Dpi), Math.Max(0, (Math.Min(bounds.Width, bounds.Height) - 1) / 2)) : D.Inset;
        return Rectangle.Inflate(bounds, -inset, -inset);
    }
    internal int Radius(int actualRadius, bool header) => (header ? A?.HeaderCornerRadius : A?.TileCornerRadius) is { } n ? RibbonLayoutEngine.Scale(n, Dpi) : actualRadius;
    internal Color Border(Color fallback, bool header) => (header ? A?.HeaderBorderColor : A?.TileBorderColor) is { IsEmpty: false } ink ? ink : fallback;
    internal int BorderWidth(Rectangle bounds, Color ink, bool header) => (header ? A?.HeaderBorderWidth : A?.TileBorderWidth) is { } n
        ? Math.Min(RibbonLayoutEngine.Scale(n, Dpi), Math.Max(0, Math.Min(bounds.Width, bounds.Height) / 2)) : ink.A == 0 ? 0 : D.BorderWidth;
}
