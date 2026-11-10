using System.Drawing.Drawing2D;

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
    internal virtual Rectangle CaptionTintBounds(RibbonControl ribbon) => Rectangle.Empty;
    protected static Color NormalCaptionColor(RibbonControl ribbon, bool composited) =>
        ribbon.WindowHost?.CaptionColor(ribbon.Theme, composited) ?? (composited ? Color.Transparent : ribbon.Theme.Background);
    internal virtual Color CaptionColor(RibbonControl ribbon, bool composited) => NormalCaptionColor(ribbon, composited);
    internal void PaintCaptionBackground(RibbonControl ribbon, Graphics graphics, Rectangle bounds, bool composited)
    {
        using var normal = new SolidBrush(NormalCaptionColor(ribbon, composited));
        graphics.FillRectangle(normal, bounds);
        var tint = Rectangle.Intersect(bounds, CaptionTintBounds(ribbon));
        if (tint.Width <= 0 || tint.Height <= 0) return;
        using var brush = new SolidBrush(CaptionColor(ribbon, composited));
        var mode = graphics.CompositingMode;
        try { graphics.CompositingMode = CompositingMode.SourceOver; graphics.FillRectangle(brush, tint); }
        finally { graphics.CompositingMode = mode; }
    }
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
