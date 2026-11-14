using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    internal void PaintFilledHeaderState(RibbonPaintContext context, int radius, RibbonFill fill, RibbonStateChromeSettings? chrome = null, Color borderFallback = default, bool tabState = false)
    {
        var appearance = context.Theme.HeaderAppearance;
        var border = appearance is null || appearance.FilledBorderColor.IsEmpty ? borderFallback.IsEmpty ? Color.Transparent : borderFallback : appearance.FilledBorderColor;
        if (appearance is null || appearance.FilledBorderColor.IsEmpty)
        {
            var tabBorder = tabState ? context.Pressed ? appearance?.FilledPressedBorderColor ?? Color.Empty : context.Hot ? appearance?.FilledHoverBorderColor ?? Color.Empty : Color.Empty : Color.Empty;
            if (!tabBorder.IsEmpty) border = tabBorder;
            border = chrome?.Border(context, border) ?? border;
        }
        int stroke = border.A == 0 ? 0 : appearance?.FilledBorderWidth is { } width ? RibbonLayoutEngine.Scale(width, context.DeviceDpi) : 1;
        if (tabState && appearance?.FilledTabBottomBorderVisible == false)
            RibbonHeaderBodyRenderer.DrawOpenBottomTab(this, context, radius, fill, border, stroke);
        else PaintStateChrome(context, radius, fill, border, stroke, chrome);
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
