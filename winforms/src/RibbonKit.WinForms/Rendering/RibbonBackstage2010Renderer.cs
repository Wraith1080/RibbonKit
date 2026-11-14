using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed class RibbonBackstage2010Renderer : RibbonBackstageRenderer
{
    internal override int GetBackstagePageBorderWidth(RibbonTheme theme, int dpi) => RibbonLayoutEngine.Scale(theme.BackstagePageBorderWidth ?? 0, dpi);
    internal override int GetBackstagePageCornerRadius(RibbonTheme theme, int dpi) => RibbonLayoutEngine.Scale(theme.BackstagePageCornerRadius ?? 0, dpi);
    internal override void DrawBackground(RibbonRenderer owner, RibbonPaintContext context, int navigationWidth) => owner.DrawRoundedBackstageBackground(context, navigationWidth);
    internal override void DrawEntryState(RibbonRenderer owner, RibbonPaintContext context, bool selected)
    {
        if (!context.Hot && !context.Pressed && !selected) return;
        var recipe = context.Theme.IdentityAccent;
        var border = selected || context.Pressed ? recipe?.AdaptNavigationFill(new(recipe.NavigationBorderColor), context.Theme.Accent).Start ?? Color.Transparent : Color.Transparent;
        if (context.Theme.BackstageFullWidthNavigation) owner.PaintFullWidthBackstageState(context, selected || context.Pressed ? context.Theme.BackstageSelectedFill : context.Theme.BackstageHoverFill, border);
        else PaintNavigationState(owner, context, selected);
        var a = context.Theme.BackstageAppearance;
        if (context.Hot && !context.Pressed && !selected && a is { NavigationHoverEdgeColor.IsEmpty: false } && a.NavigationHoverEdgeColor.A > 0)
        {
            int width = Math.Min(context.Bounds.Height / 2, a.NavigationHoverEdgeWidth is { } n ? RibbonLayoutEngine.Scale(n, context.DeviceDpi) : 1);
            if (width > 0)
            {
                using var edge = new SolidBrush(a.NavigationHoverEdgeColor);
                context.Graphics.FillRectangle(edge, context.Bounds.Left, context.Bounds.Top, context.Bounds.Width, width);
                context.Graphics.FillRectangle(edge, context.Bounds.Left, context.Bounds.Bottom - width, context.Bounds.Width, width);
            }
        }
    }
}
