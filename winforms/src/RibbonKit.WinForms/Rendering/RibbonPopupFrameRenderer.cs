using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Neutral/Office popup frames share structure and differ through actual renderer
// radius/measurement hooks. Native windows, caches and content keep their owners.
internal static class RibbonPopupFrameRenderer
{
    internal static void DrawFrame(RibbonRenderer owner, RibbonPaintContext context) =>
        owner.PaintRounded(context, owner.GetPopupCornerRadius(context.Theme, context.DeviceDpi), context.Theme.PopupFill, context.Theme.PopupBorder,
            RibbonPopupFrameMetrics.PaintedBorder(owner, context.Theme, context.DeviceDpi));
    internal static void DrawShadow(RibbonRenderer owner, RibbonPaintContext context)
    {
        var metrics = new RibbonPopupFrameMetrics(context.Theme, context.DeviceDpi, RibbonPresentationResolver.PopupFrame);
        var bounds = context.Bounds; bounds.Offset(metrics.HorizontalOffset, metrics.VerticalOffset);
        owner.PaintPopupShadow(context, bounds, owner.GetPopupCornerRadius(context.Theme, context.DeviceDpi), metrics.Blur, metrics.Falloff);
    }
}

public partial class RibbonRenderer
{
    internal void PaintPopupShadow(RibbonPaintContext context, Rectangle bounds, int radius, int blur, double falloff) =>
        _shadow.Draw(context.Graphics, bounds, radius, blur, context.Theme.PopupShadowColor, falloff: falloff);
}
