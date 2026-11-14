using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed class RibbonBackstage2024Renderer : RibbonBackstageRenderer
{
    internal override void DrawEntryState(RibbonRenderer owner, RibbonPaintContext context, bool selected)
    {
        if (!context.Hot && !context.Pressed && !selected) return;
        PaintNavigationState(owner, context, selected);
    }
    internal override int GetBackstagePageBorderWidth(RibbonTheme theme, int dpi) => theme.BackstagePageBorderWidth is { } width ? RibbonLayoutEngine.Scale(width, dpi) : 1;
    internal override int GetBackstagePageCornerRadius(RibbonTheme theme, int dpi) => RibbonLayoutEngine.Scale(theme.BackstagePageCornerRadius ?? 2d * theme.CornerRadius, dpi);
    internal override void DrawBackground(RibbonRenderer owner, RibbonPaintContext context, int navigationWidth)
        => owner.DrawRoundedBackstageBackground(context, navigationWidth);
}
