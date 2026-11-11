namespace RibbonKit.WinForms.Layout;

internal static class RibbonSharedEffectMetrics
{
    internal static Rectangle FocusBounds(RibbonPaintContext context)
    {
        int inset = context.Theme.FocusInset is { } logical
            ? Math.Min(RibbonLayoutEngine.Scale(logical, context.DeviceDpi), Math.Max(0, (Math.Min(context.Bounds.Width, context.Bounds.Height) - 2) / 2)) : 2;
        return Rectangle.Inflate(context.Bounds, -inset, -inset);
    }
    internal static int FocusStroke(RibbonPaintContext context) => context.Theme.FocusStrokeWidth is { } width ? Math.Max(1, RibbonLayoutEngine.Scale(width, context.DeviceDpi)) : 1;
    internal static void Validate(RibbonTheme theme)
    {
        RibbonChevronSettings.Validate(theme.Chevrons);
        if (theme.FocusInset is < 0 or > 1024 || theme.FocusStrokeWidth is < 1 or > 32 || theme.TabContentSlideDistance is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Focus inset/content slide must be 0..1024 and focus width 1..32 logical pixels, or null for existing effects.");
        if (theme.HeaderIndicatorSpringAmount is { } amount && (!double.IsFinite(amount) || amount < 0 || amount > 1))
            throw new ArgumentOutOfRangeException(nameof(theme), "Indicator spring amount must be a finite fraction from 0 to 1, or null for 0.35.");
    }
}
