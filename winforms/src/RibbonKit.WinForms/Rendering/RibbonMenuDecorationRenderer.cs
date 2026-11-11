using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal enum RibbonMenuDecorationKind { None, Header, Overflow }

internal static class RibbonMenuDecorationRenderer
{
    internal static void DrawHeading(RibbonRenderer owner, RibbonPaintContext context, string text, bool galleryFallback)
    {
        context = context with { Hot = false, Pressed = false, Focused = false };
        var metrics = new RibbonMenuAppearanceMetrics(context.Theme, context.DeviceDpi, RibbonPresentationResolver.MenuDecoration);
        var fill = context.Theme.MenuHeadingBackground ?? (galleryFallback ? context.Theme.GalleryHeadingBackground : (RibbonFill?)null);
        var border = metrics.Border(header: true);
        if (fill is not null || border.A != 0) owner.PaintMenuDecoration(context, fill ?? new(Color.Transparent), border, header: true);
        var style = context.Theme.MenuAppearance?.HeaderBold == false ? context.Font.Style & ~FontStyle.Bold : context.Font.Style | FontStyle.Bold;
        using var font = new Font(context.Font, style);
        var foreground = !context.Theme.MenuHeadingForeground.IsEmpty ? context.Theme.MenuHeadingForeground : galleryFallback ? context.Theme.GalleryHeadingForeground : context.Theme.SecondaryText;
        owner.DrawText(context with { Font = font, Bounds = RibbonMenuLayout.HeaderTextBounds(context.Bounds, context.DeviceDpi, context.Density, context.Theme) }, text, foreground,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
    }
    internal static void DrawSeparator(RibbonPaintContext context)
    {
        bool ordinary = context.ApplicationMenuPart == RibbonApplicationMenuPart.None && !context.IsBackstageMenuSeparator;
        int inset = RibbonLayoutEngine.Scale(context.ApplicationMenuPart == RibbonApplicationMenuPart.None ? RibbonMenuMetrics.For(context.Density, context.Theme).MenuSeparatorHorizontalPadding : 8, context.DeviceDpi);
        if (context.Bounds.Width <= 2 * inset || context.Bounds.Height <= 0) return;
        var metrics = new RibbonMenuAppearanceMetrics(context.Theme, context.DeviceDpi, RibbonPresentationResolver.MenuDecoration);
        var bounds = metrics.SeparatorBounds(context.Bounds, inset, ordinary);
        var ink = ordinary && context.Theme.MenuAppearance is { SeparatorColor.IsEmpty: false } settings ? settings.SeparatorColor : context.Theme.Border;
        if (bounds.Height <= 0 || ink.A == 0) return;
        using var brush = new SolidBrush(ink); context.Graphics.FillRectangle(brush, bounds);
    }
    internal static void DrawOverflow(RibbonRenderer owner, RibbonPaintContext context, bool upward, bool enabled)
    {
        var fill = context.Theme.MenuAppearance?.OverflowBackground ?? new(Color.FromArgb(255, context.Theme.PopupColor));
        owner.PaintRounded(context, 0, fill, Color.Transparent, 0);
        if (enabled && (context.Hot || context.Pressed))
        {
            var metrics = new RibbonMenuAppearanceMetrics(context.Theme, context.DeviceDpi, RibbonPresentationResolver.MenuDecoration);
            owner.PaintMenuDecoration(context, context.Pressed ? context.Theme.Pressed : context.Theme.Hover, metrics.Border(header: false), header: false);
        }
        RibbonArrowPainter.DrawVertical(context.Graphics, context.Bounds, context.DeviceDpi, enabled ? context.Theme.Foreground : context.Theme.DisabledColor, upward, context.Theme.Chevrons);
    }
}

public partial class RibbonRenderer
{
    internal void PaintMenuDecoration(RibbonPaintContext context, RibbonFill fill, Color border, bool header) =>
        DrawChrome(context with { MenuDecoration = header ? RibbonMenuDecorationKind.Header : RibbonMenuDecorationKind.Overflow }, fill, border);
    private bool TryPaintMenuDecoration(RibbonPaintContext context, RibbonFill fill, Color border, int defaultRadius)
    {
        var settings = context.Theme.MenuAppearance;
        bool header = context.MenuDecoration == RibbonMenuDecorationKind.Header;
        if (settings is null || (header ? !settings.HasHeaderChrome : context.MenuDecoration != RibbonMenuDecorationKind.Overflow || !settings.HasOverflowChrome)) return false;
        var metrics = new RibbonMenuAppearanceMetrics(context.Theme, context.DeviceDpi, RibbonPresentationResolver.MenuDecoration);
        var bounds = metrics.PaintBounds(context.Bounds, header);
        PaintRounded(context with { Bounds = bounds }, metrics.Radius(defaultRadius, header), fill, border, metrics.BorderWidth(bounds, header, border));
        return true;
    }
}
