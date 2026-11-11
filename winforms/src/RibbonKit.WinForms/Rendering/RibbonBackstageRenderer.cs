namespace RibbonKit.WinForms;

// A style-free contract. Families borrow the owner's neutral drawing primitives
// and virtual hooks; neither inherits another family's visual defaults.
internal abstract class RibbonBackstageRenderer
{
    internal virtual int GetBackstagePageShadowInset(RibbonTheme theme, int dpi) => 0;
    internal abstract int GetBackstagePageBorderWidth(RibbonTheme theme, int dpi);
    internal abstract int GetBackstagePageCornerRadius(RibbonTheme theme, int dpi);
    internal abstract void DrawBackground(RibbonRenderer owner, RibbonPaintContext context, int navigationWidth);
    internal abstract void DrawEntryState(RibbonRenderer owner, RibbonPaintContext context, bool selected);
    protected static void PaintNavigationState(RibbonRenderer owner, RibbonPaintContext context, bool selected)
    {
        if (context.Theme.BackstageFullWidthNavigation)
        {
            owner.PaintFullWidthBackstageState(context, selected || context.Pressed ? context.Theme.BackstageSelectedFill : context.Theme.BackstageHoverFill);
            return;
        }
        var fill = context.Pressed ? context.Theme.BackstageNavigationSelected ?? context.Theme.Pressed
            : selected ? context.Theme.BackstageSelectedFill : context.Theme.BackstageHoverFill;
        var authored = context.Pressed || selected ? context.Theme.BackstageNavigationSelected : context.Theme.BackstageNavigationHover;
        if ((context.WindowBackdrop || context.CompositedText) && authored is null)
            fill = RibbonRenderer.BackstageStateFill(context, context.Pressed);
        owner.PaintBackstageChrome(context, fill);
    }
}

public partial class RibbonRenderer
{
    internal RibbonBackstageRenderer BackstageStyleRenderer(RibbonTheme theme) => RibbonPresentationResolver.Backstage(theme).Renderer;
    internal void PaintBackstageChrome(RibbonPaintContext context, RibbonFill fill) => DrawChrome(context with { IsBackstageNavigationState = true }, fill, Color.Transparent);
    internal void PaintFullWidthBackstageState(RibbonPaintContext context, RibbonFill fill)
    {
        var settings = context.Theme.BackstageAppearance;
        var border = settings is null || settings.NavigationBorderColor.IsEmpty ? Color.Transparent : settings.NavigationBorderColor;
        PaintRounded(context, Layout.RibbonLayoutEngine.Scale(settings?.NavigationCornerRadius ?? 0, context.DeviceDpi), fill, border,
            border.A == 0 ? 0 : settings?.NavigationBorderWidth is { } width ? Layout.RibbonLayoutEngine.Scale(width, context.DeviceDpi) : 1);
    }
    private bool TryPaintBackstageState(RibbonPaintContext context, RibbonFill fill, Color fallbackBorder, int defaultRadius)
    {
        if (!context.IsBackstageNavigationState || context.Theme.BackstageAppearance is not { HasNavigationOverrides: true } settings) return false;
        var border = settings.NavigationBorderColor.IsEmpty ? fallbackBorder : settings.NavigationBorderColor;
        PaintRounded(context with { Bounds = Rectangle.Inflate(context.Bounds, -1, -1) },
            Layout.RibbonFrameMetrics.Radius(settings.NavigationCornerRadius, defaultRadius, context.DeviceDpi), fill, border,
            border.A == 0 ? 0 : settings.NavigationBorderWidth is { } width ? Layout.RibbonLayoutEngine.Scale(width, context.DeviceDpi) : 1);
        return true;
    }
    internal static RibbonFill BackstageStateFill(RibbonPaintContext context, bool pressed) => BackdropStateFill(context, pressed);
}
