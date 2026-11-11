using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed class RibbonHeader2019Renderer : RibbonHeaderRenderer
{
    internal override void DrawTab(RibbonRenderer owner, RibbonPaintContext context, RibbonTab tab, bool selected, RibbonHeaderMetrics metrics)
    {
        bool available = tab.EffectiveEnabled;
        if (selected || available && (context.Hot || context.Pressed))
            owner.PaintRounded(context, RibbonLayoutEngine.Scale(metrics.FilledCornerRadius, context.DeviceDpi), selected ? context.Theme.HeaderSelectedBackground ?? new(context.Theme.Surface) : context.Pressed ? context.Theme.HeaderPressed ?? context.Theme.Pressed : context.Theme.HeaderHover ?? context.Theme.Hover, Color.Transparent, 0);
        bool authoredInk = selected && !context.Theme.HeaderSelectedForeground.IsEmpty;
        var ink = !available ? context.Theme.DisabledColor : authoredInk ? context.Theme.HeaderSelectedForeground : selected ? context.Theme.Foreground : context.Theme.HeaderTextColor;
        if (available && tab.IsContextual && !authoredInk) ink = RibbonRenderer.ContextualTabColor(context with { Theme = context.Theme with { Foreground = ink } }, tab, selected);
        owner.DrawText(context, tab.DisplayText, ink,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping);
        if (available && context.Focused) owner.DrawFocus(context);
    }
    internal override void DrawFileButton(RibbonRenderer owner, RibbonPaintContext context, RibbonDropDownItem menu, RibbonHeaderMetrics metrics)
    {
        bool available = menu.EffectiveEnabled, expanded = available && menu.IsDropDownOpen && !context.FileMenuClosing;
        if (available && (expanded || context.Hot || context.Pressed))
            owner.PaintRounded(context, RibbonLayoutEngine.Scale(metrics.FilledCornerRadius, context.DeviceDpi), expanded || context.Pressed ? context.Theme.HeaderPressed ?? context.Theme.Pressed : context.Theme.HeaderHover ?? context.Theme.Hover, Color.Transparent, 0);
        owner.DrawText(context, menu.Text, available ? context.Theme.HeaderTextColor : context.Theme.DisabledColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping);
        if (available && context.Focused) owner.DrawFocus(context);
    }
    internal override Rectangle GetTabIndicatorBounds(RibbonPaintContext context, RibbonTab tab, RibbonHeaderMetrics metrics) => Rectangle.Empty;
    internal override void DrawTabIndicator(RibbonRenderer owner, RibbonPaintContext context, RibbonTab tab, bool selected, RibbonHeaderMetrics metrics) { }
}
