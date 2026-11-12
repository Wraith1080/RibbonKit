using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Drawing recipes only. Public owner hooks and native window/keyboard services retain control.
internal static class RibbonTipRenderer
{
    internal static void DrawScreenTip(RibbonRenderer owner, RibbonPaintContext c, string title, string description, Rectangle titleBounds, Rectangle descriptionBounds)
    {
        using var font = new Font(c.Font, new RibbonTipMetrics(c.Theme).TitleStyle(c.Font));
        owner.DrawText(c with { Bounds = titleBounds, Font = font }, title, c.Theme.Foreground, RibbonScreenTipLayout.TextFlags);
        if (!descriptionBounds.IsEmpty) owner.DrawText(c with { Bounds = descriptionBounds }, description, c.Theme.Foreground, RibbonScreenTipLayout.TextFlags);
    }
    internal static void DrawKeyTip(RibbonRenderer owner, RibbonPaintContext c, string keys, bool office)
    {
        if (c.Bounds.Width <= 0 || c.Bounds.Height <= 0) return;
        var m = new RibbonTipMetrics(c.Theme); var bounds = m.KeyTipPaintBounds(c.Bounds, c.DeviceDpi);
        owner.PaintRounded(c with { Bounds = bounds }, office ? RibbonFrameMetrics.Radius(c.Theme.KeyTipCornerRadius, RibbonPresentationResolver.Tips.OfficeKeyTipRadius, c.DeviceDpi) : 0,
            c.Theme.KeyTipBackground ?? new(office ? Color.FromArgb(255, 255, 225) : c.Theme.Surface),
            c.Theme.KeyTipBorder.IsEmpty ? c.Theme.Border : c.Theme.KeyTipBorder, m.KeyTipStroke(bounds, c.DeviceDpi));
        owner.DrawText(c, keys, c.Theme.KeyTipForeground.IsEmpty ? office ? Color.Black : c.Theme.Foreground : c.Theme.KeyTipForeground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}
