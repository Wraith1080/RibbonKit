using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed class RibbonHeader2024Renderer : RibbonHeaderRenderer
{
    internal override void DrawTab(RibbonRenderer owner, RibbonPaintContext context, RibbonTab tab, bool selected, RibbonHeaderMetrics metrics)
    {
        bool enabled = tab.EffectiveEnabled;
        owner.DrawText(context, tab.DisplayText, !enabled ? context.Theme.DisabledColor : selected && !context.Theme.HeaderSelectedForeground.IsEmpty ? context.Theme.HeaderSelectedForeground : tab.IsContextual ? RibbonRenderer.ContextualTabColor(context, tab, selected) : selected ? context.Theme.Accent : context.Theme.HeaderTextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping);
        if (enabled && !context.SuppressTabIndicators && (selected || context.Hot || context.Pressed || context.Focused))
            owner.DrawTabIndicator(context with { Bounds = owner.GetTabIndicatorBounds(context, tab) }, tab, selected);
    }
    internal override Rectangle GetTabIndicatorBounds(RibbonPaintContext context, RibbonTab tab, RibbonHeaderMetrics metrics)
        => IndicatorBounds(context, tab.DisplayText, metrics);
    private static Rectangle IndicatorBounds(RibbonPaintContext context, string text, RibbonHeaderMetrics metrics)
        => metrics.IndicatorBounds(context.Bounds, RibbonCaptionLayout.TextWidth(text, context.Font), context.DeviceDpi);
    internal override void DrawTabIndicator(RibbonRenderer owner, RibbonPaintContext context, RibbonTab tab, bool selected, RibbonHeaderMetrics metrics)
    {
        if (!tab.EffectiveEnabled || context.Bounds.IsEmpty || metrics.IndicatorThickness == 0) return;
        var tint = selected ? !context.Theme.HeaderSelectedForeground.IsEmpty ? context.Theme.HeaderSelectedForeground : tab.IsContextual ? RibbonRenderer.ContextualTabColor(context, tab, true) : context.Theme.Accent : HoverColor(context.Theme);
        PaintIndicator(owner, context, tint, selected);
    }
    private static Color HoverColor(RibbonTheme theme) => theme.HeaderIndicatorHoverColor.IsEmpty ? theme.SecondaryText : theme.HeaderIndicatorHoverColor;
    private static void PaintIndicator(RibbonRenderer owner, RibbonPaintContext context, Color tint, bool selected)
    {
        if (!selected && (context.CompositedText || context.WindowBackdrop))
            tint = Color.FromArgb((int)Math.Round(tint.A * context.Theme.BackdropTabHoverOpacity), tint);
        owner.PaintRounded(context, RibbonFrameMetrics.Radius(context.Theme.HeaderIndicatorCornerRadius, context.Bounds.Height / 2, context.DeviceDpi), new(tint), Color.Transparent, 0);
    }
    internal override void DrawFileButton(RibbonRenderer owner, RibbonPaintContext context, RibbonDropDownItem menu, RibbonHeaderMetrics metrics)
    {
        if (owner.DrawApplicationFileButton(context, menu)) return;
        bool enabled = menu.EffectiveEnabled, open = enabled && menu.IsDropDownOpen && !context.FileMenuClosing;
        owner.DrawText(context, menu.Text, !enabled ? context.Theme.DisabledColor : open ? context.Theme.Accent : context.Theme.HeaderTextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        if (enabled && !context.SuppressTabIndicators && (open || context.Hot || context.Pressed || context.Focused))
        {
            var bounds = IndicatorBounds(context, menu.Text, metrics);
            if (!bounds.IsEmpty) PaintIndicator(owner, context with { Bounds = bounds }, open ? context.Theme.Accent : HoverColor(context.Theme), open);
        }
    }
}
