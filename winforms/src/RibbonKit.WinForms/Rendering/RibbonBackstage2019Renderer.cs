using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed class RibbonBackstage2019Renderer : RibbonBackstageRenderer
{
    internal override int GetBackstagePageBorderWidth(RibbonTheme theme, int dpi) => RibbonLayoutEngine.Scale(theme.BackstagePageBorderWidth ?? 0, dpi);
    internal override int GetBackstagePageCornerRadius(RibbonTheme theme, int dpi) => RibbonLayoutEngine.Scale(theme.BackstagePageCornerRadius ?? 0, dpi);
    internal override void DrawBackground(RibbonRenderer owner, RibbonPaintContext context, int navigationWidth)
        => owner.DrawNeutralBackstageBackground(context, navigationWidth);
    internal override void DrawEntryState(RibbonRenderer owner, RibbonPaintContext context, bool selected)
    {
        if (!context.Hot && !context.Pressed && !selected) return;
        PaintNavigationState(owner, context, selected);
    }
}
