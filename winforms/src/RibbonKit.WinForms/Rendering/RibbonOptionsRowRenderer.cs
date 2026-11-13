using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Native command-list paint only: renderer interception, list targets, focus and
// draft/input ownership retain their existing owners.
internal static class RibbonOptionsRowRenderer
{
    internal static void Draw(RoundedPainter painter, RibbonPaintContext c, RibbonElement? source, string text, bool selected, bool simplified = false, bool heading = false)
    {
        var a = c.Theme.OptionsAppearance; var metrics = new RibbonOptionsRowMetrics(c.Theme, c.DeviceDpi);
        if (!selected || a?.RowSelectedBackground is null && a?.RowCornerRadius is null)
        {
            using var brush = new SolidBrush(selected ? c.Theme.Checked.Start : c.Theme.Surface); c.Graphics.FillRectangle(brush, c.Bounds);
        }
        else
        {
            using var background = new SolidBrush(c.Theme.Surface); c.Graphics.FillRectangle(background, c.Bounds);
            painter.Draw(c.Graphics, c.Bounds, metrics.Radius, a.RowSelectedBackground ?? new(c.Theme.Checked.Start), Color.Transparent, 0);
        }
        var slots = metrics.Resolve(c.Bounds, simplified, heading);
        if (!slots.Icon.IsEmpty) DrawIcon(c.Graphics, source, slots.Icon, c.DeviceDpi, c.Theme.Foreground);
        if (slots.Text.Width > 0 && slots.Text.Height > 0) TextRenderer.DrawText(c.Graphics, text, c.Font, slots.Text, c.Theme.Foreground,
            TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
    internal static void DrawIcon(Graphics graphics, RibbonElement? source, Rectangle bounds, int dpi, Color color)
    {
        if (source is RibbonGroup { Icon: { } groupIcon }) groupIcon.Draw(graphics, bounds, new(dpi, color, true));
        else if (source is RibbonItem { Icon: { } icon }) icon.Draw(graphics, bounds, new(dpi, color, true));
        else if (source is RibbonItem { Image: { } image }) graphics.DrawImage(image, bounds);
    }
}
