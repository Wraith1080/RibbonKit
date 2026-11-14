using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    internal void PaintFilledHeaderState(RibbonPaintContext context, int radius, RibbonFill fill, RibbonStateChromeSettings? chrome = null)
    {
        var appearance = context.Theme.HeaderAppearance;
        var border = appearance is null || appearance.FilledBorderColor.IsEmpty ? Color.Transparent : appearance.FilledBorderColor;
        if (appearance is null || appearance.FilledBorderColor.IsEmpty) border = chrome?.Border(context, border) ?? border;
        PaintStateChrome(context, radius, fill, border, border.A == 0 ? 0 : appearance?.FilledBorderWidth is { } width ? RibbonLayoutEngine.Scale(width, context.DeviceDpi) : 1, chrome);
    }
    private bool TryPaintHeaderScroller(RibbonPaintContext context, RibbonFill fill, Color border, int defaultRadius)
    {
        if (!context.IsHeaderScrollButton || context.Theme.HeaderAppearance is not { HasScrollerOverrides: true } appearance) return false;
        PaintHeaderScroller(context with { Bounds = Rectangle.Inflate(context.Bounds, -1, -1) }, fill, border, defaultRadius);
        return true;
    }
    private void PaintHeaderScroller(RibbonPaintContext context, RibbonFill fill, Color border, int defaultRadius)
    {
        var appearance = context.Theme.HeaderAppearance;
        PaintRounded(context, RibbonFrameMetrics.Radius(appearance?.ScrollerCornerRadius, defaultRadius, context.DeviceDpi), fill, border,
            border.A == 0 ? 0 : appearance?.ScrollerBorderWidth is { } width ? RibbonLayoutEngine.Scale(width, context.DeviceDpi) : 1);
    }
}
