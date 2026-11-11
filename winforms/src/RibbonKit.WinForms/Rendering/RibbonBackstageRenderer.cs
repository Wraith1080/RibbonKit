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
        var fill = context.Pressed ? context.Theme.BackstageNavigationSelected ?? context.Theme.Pressed
            : selected ? context.Theme.BackstageSelectedFill : context.Theme.BackstageHoverFill;
        owner.PaintBackstageChrome(context, context.WindowBackdrop || context.CompositedText ? RibbonRenderer.BackstageStateFill(context, context.Pressed) : fill);
    }
}

public partial class RibbonRenderer
{
    internal RibbonBackstageRenderer BackstageStyleRenderer(RibbonTheme theme) => RibbonPresentationResolver.Backstage(theme).Renderer;
    internal void PaintBackstageChrome(RibbonPaintContext context, RibbonFill fill) => DrawChrome(context, fill, Color.Transparent);
    internal static RibbonFill BackstageStateFill(RibbonPaintContext context, bool pressed) => BackdropStateFill(context, pressed);
}
