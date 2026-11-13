using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal static class RibbonOptionsIconPickerRenderer
{
    internal static void Draw(RoundedPainter painter, RibbonPaintContext c, IRibbonIcon? icon, bool selected, int fallbackRadius)
    {
        var metrics = new RibbonOptionsIconPickerMetrics(c.Theme, c.DeviceDpi, c.Density);
        using var background = new SolidBrush(c.Theme.Surface); c.Graphics.FillRectangle(background, c.Bounds);
        if (selected)
        {
            var paint = metrics.SelectionBounds(c.Bounds);
            painter.Draw(c.Graphics, paint, metrics.Radius(fallbackRadius), c.Theme.OptionsAppearance?.IconPickerSelectedBackground ?? c.Theme.Checked, metrics.Border, metrics.BorderWidth(paint));
        }
        if (icon is not null)
        {
            var bounds = metrics.IconBounds(c.Bounds);
            if (!bounds.IsEmpty) icon.Draw(c.Graphics, bounds, new(c.DeviceDpi, c.Theme.Foreground, true));
        }
        else
        {
            var bounds = metrics.TextBounds(c.Bounds);
            if (bounds.Width > 0 && bounds.Height > 0) TextRenderer.DrawText(c.Graphics, "None", c.Font, bounds, c.Theme.Foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }
}
