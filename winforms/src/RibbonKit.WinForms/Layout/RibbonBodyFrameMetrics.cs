namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonBodyFrameDefaults(bool PaintShadow, bool BorderFallback, bool AllowBottomBorderOnly, int RadiusDivisor);

// Resolved frame values only. Palette/header selectors never choose a body family.
internal readonly record struct RibbonBodyFrameMetrics
{
    internal RibbonBodyFrameMetrics(RibbonTheme theme, int dpi, RibbonBodyFrameDefaults defaults)
    {
        Radius = defaults.RadiusDivisor == 0 ? 0 : theme.BodyCornerRadius is { } radius ? RibbonLayoutEngine.Scale(radius, dpi) : theme.CornerRadius * dpi / defaults.RadiusDivisor;
        PaintShadow = defaults.PaintShadow; BorderFallback = defaults.BorderFallback;
        BottomBorderOnly = defaults.AllowBottomBorderOnly && theme.BodyBottomBorderOnly;
        BorderWidth = Width(theme.BodyAppearance?.BorderWidth, dpi);
        var frame = new RibbonFrameMetrics(theme);
        int blur = RibbonLayoutEngine.Scale(frame.BodyBlur, dpi), offset = RibbonLayoutEngine.Scale(frame.BodyOffset, dpi);
        ShadowPadding = !PaintShadow || frame.BodyBlur == 0 || theme.BodyShadowColor.A == 0 ? Padding.Empty
            : new(blur, Math.Max(0, blur - offset), blur, Math.Max(0, blur + offset));
    }
    internal int Radius { get; }
    internal bool PaintShadow { get; }
    internal bool BorderFallback { get; }
    internal bool BottomBorderOnly { get; }
    internal int BorderWidth { get; }
    internal Padding ShadowPadding { get; }
    internal static int Width(int? logical, int dpi) => logical is { } width ? RibbonLayoutEngine.Scale(width, dpi) : 1;
    internal static int MessageBorderWidth(RibbonPaintContext context) => Width(context.Theme.BodyAppearance?.MessageBorderWidth, context.DeviceDpi);
    internal static Rectangle SeparatorBounds(RibbonTheme theme, int dpi, Rectangle bounds)
    {
        int width = Width(theme.BodyAppearance?.GroupSeparatorWidth, dpi);
        return new(bounds.Right - width, bounds.Top, width, bounds.Height);
    }
    internal static bool JoinQuickAccess(RibbonTheme theme, RibbonRenderer renderer) => renderer.JoinBelowRibbonQuickAccess && theme.BodyAppearance?.JoinBelowRibbonQuickAccess != false;
    internal static bool JoinMessages(RibbonTheme theme, RibbonRenderer renderer) => renderer.JoinMessageBarToBody && theme.BodyAppearance?.JoinMessageBarToBody != false;
}
