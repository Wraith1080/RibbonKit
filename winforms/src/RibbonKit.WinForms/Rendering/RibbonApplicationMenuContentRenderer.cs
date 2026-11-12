using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal static class RibbonApplicationMenuContentRenderer
{
    internal static void Draw(RibbonRenderer owner, RibbonPaintContext c, RibbonItem item, bool arrow)
    {
        var m = new RibbonApplicationMenuContentMetrics(c.Theme, c.DeviceDpi, c.Density);
        var bounds = c.Bounds; bool footer = c.ApplicationMenuPart == RibbonApplicationMenuPart.Footer;
        bool navigation = c.ApplicationMenuPart == RibbonApplicationMenuPart.Navigation;
        bool rich = c.ApplicationMenuPart == RibbonApplicationMenuPart.Pane && RibbonApplicationMenuMetrics.Illustrated(item);
        bool large = navigation || rich, hasIcon = RibbonApplicationMenuMetrics.HasIcon(item) || item is RibbonCheckBoxItem or RibbonRadioButtonItem;
        bool enabled = item.EffectiveEnabled; var color = enabled ? c.Theme.Foreground : c.Theme.DisabledColor;
        int arrowWidth = item is RibbonDropDownItem && (arrow || item is RibbonSplitButtonItem) ? m.ArrowIn(bounds) : 0;
        int leading = m.ClampedLeading(bounds, large, hasIcon);
        var text = new Rectangle(bounds.Left + leading, bounds.Top, Math.Max(0, bounds.Width - leading - m.Trailing() - arrowWidth), bounds.Height);
        var image = m.IconBounds(bounds, large);
        if (footer)
        {
            int label = TextRenderer.MeasureText(item.Text, c.Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
            int contentWidth = Math.Min(bounds.Width - m.FooterBothPadding - arrowWidth, label + (hasIcon ? m.FooterIconSpan : 0));
            int left = bounds.Left + Math.Max(m.FooterPadding, (bounds.Width - arrowWidth - contentWidth) / 2);
            image.X = left; text.X = left + (hasIcon ? m.FooterIconSpan : 0); text.Width = Math.Max(0, bounds.Right - m.FooterPadding - arrowWidth - text.Left);
            if (c.Theme.ApplicationMenuContent is { } a && (a.FooterHorizontalPadding is not null || a.SmallIconSize is not null || a.IconTextGap is not null))
            {
                image.X = Math.Clamp(image.X, bounds.Left, Math.Max(bounds.Left, bounds.Right - image.Width));
                text.X = Math.Min(text.X, bounds.Right); text.Width = Math.Max(0, bounds.Right - m.FooterPadding - arrowWidth - text.Left);
            }
        }
        if (hasIcon && image.Width > 0 && image.Height > 0)
        {
            if (item is RibbonCheckBoxItem box) owner.DrawCheckBoxIndicator(c with { Bounds = image }, box);
            else if (item is RibbonRadioButtonItem radio) owner.DrawRadioButtonIndicator(c with { Bounds = image }, radio);
            else owner.PaintApplicationMenuImage(c with { Bounds = image }, item, large ? RibbonItemSize.Large : RibbonItemSize.Small);
        }
        bool described = !footer && item.MenuDescription.Length > 0 && m.Lines > 0;
        if (described)
        {
            int descriptionHeight = m.DescriptionHeight(item.MenuDescription, c.Font, text.Width);
            int top = bounds.Top + Math.Max(m.DescriptionInset, (bounds.Height - c.Font.Height - m.DescriptionGap - descriptionHeight) / 2);
            text.Y = top; text.Height = c.Font.Height;
            using var title = rich ? new Font(c.Font, m.RichStyle(c.Font.Style)) : null;
            owner.DrawText(c with { Bounds = text, Font = title ?? c.Font }, item.Text, rich && enabled ? c.Theme.SecondaryText : color,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            var description = new Rectangle(text.Left, text.Bottom + m.DescriptionGap, text.Width,
                Math.Max(0, Math.Min(descriptionHeight, bounds.Bottom - m.DescriptionInset - text.Bottom - m.DescriptionGap)));
            owner.DrawText(c with { Bounds = description }, item.MenuDescription, color, RibbonMenuLayout.DescriptionFlags);
        }
        else owner.DrawText(c with { Bounds = text }, item.Text, color, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        if (arrow && arrowWidth > 0) owner.DrawSubmenuArrow(c with { Bounds = new(bounds.Right - arrowWidth, bounds.Top, arrowWidth, bounds.Height) }, enabled ? c.Theme.SecondaryText : color);
        if (c.Focused) owner.DrawFocus(c);
    }
    internal static void DrawSeparator(RibbonPaintContext c)
    {
        var m = new RibbonApplicationMenuContentMetrics(c.Theme, c.DeviceDpi, c.Density); var bounds = m.SeparatorBounds(c.Bounds);
        if (bounds.Width <= 0 || bounds.Height <= 0 || m.SeparatorInk.A == 0) return;
        using var brush = new SolidBrush(m.SeparatorInk); c.Graphics.FillRectangle(brush, bounds);
    }
}
