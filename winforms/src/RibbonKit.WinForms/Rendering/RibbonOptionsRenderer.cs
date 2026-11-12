using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Decoration over existing native rows/panels. Do not route through command chrome
// or introduce renderer interception where the native dialog painted text directly.
internal static class RibbonOptionsRenderer
{
    internal static void DrawNavigation(RibbonRenderer owner, RoundedPainter painter, RibbonPaintContext c, string text, bool selected, bool focused)
    {
        var theme = c.Theme; var a = theme.OptionsAppearance; var m = new RibbonOptionsAppearanceMetrics(theme, c.DeviceDpi);
        using var background = new SolidBrush(theme.BackstageNavigationBackground); c.Graphics.FillRectangle(background, c.Bounds);
        var bounds = m.SelectionBounds(c.Bounds);
        if (selected)
        {
            painter.Draw(c.Graphics, bounds, m.Radius(owner.GetPopupCornerRadius(theme, c.DeviceDpi)), a?.NavigationSelectedBackground ?? theme.Checked, Color.Transparent, 0);
            var marker = m.MarkerBounds(bounds);
            painter.Draw(c.Graphics, marker, RibbonFrameMetrics.Radius(theme.NavigationMarkerCornerRadius, marker.Width / 2, c.DeviceDpi), a?.NavigationMarkerFill ?? new(theme.Accent), Color.Transparent, 0);
        }
        TextRenderer.DrawText(c.Graphics, text, c.Font, m.TextBounds(bounds), selected ? theme.Accent : theme.Foreground, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if (selected && focused) ControlPaint.DrawFocusRectangle(c.Graphics, m.FocusBounds(c.Bounds), theme.Accent, theme.BackstageNavigationBackground);
    }
    internal static void DrawDivider(Graphics g, Rectangle bounds, RibbonTheme theme, int dpi, bool vertical)
    {
        var a = theme.OptionsAppearance; var ink = a is { DividerColor.IsEmpty: false } ? a.DividerColor : theme.Border;
        if (a?.DividerWidth is null)
        {
            using var pen = new Pen(ink);
            if (vertical) g.DrawLine(pen, bounds.Right - 1, bounds.Top, bounds.Right - 1, bounds.Bottom);
            else g.DrawLine(pen, bounds.Left, bounds.Top, bounds.Right, bounds.Top);
            return;
        }
        int width = Math.Min(new RibbonOptionsAppearanceMetrics(theme, dpi).DividerWidth, Math.Max(0, vertical ? bounds.Width : bounds.Height));
        if (width <= 0 || ink.A == 0) return;
        using var brush = new SolidBrush(ink);
        g.FillRectangle(brush, vertical ? new(bounds.Right - width, bounds.Top, width, bounds.Height) : new Rectangle(bounds.Left, bounds.Top, bounds.Width, width));
    }
}
