using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal static class RibbonOptionsFieldRenderer
{
    internal static void DrawFrame(RoundedPainter painter, RibbonPaintContext c, int fallbackRadius)
    {
        var m = new RibbonOptionsFieldMetrics(c.Theme, c.DeviceDpi);
        painter.Draw(c.Graphics, c.Bounds, m.Radius(fallbackRadius), new(c.Theme.Surface), m.Border, m.BorderWidth(c.Bounds.Size));
    }
    internal static void DrawChoice(RibbonRenderer owner, RibbonPaintContext c, RibbonComboBoxItem model, string text, bool enabled)
    {
        using var background = new SolidBrush(c.Theme.Surface); c.Graphics.FillRectangle(background, c.Bounds);
        owner.DrawComboBox(c, model, c.Bounds);
        var bounds = new RibbonOptionsFieldMetrics(c.Theme, c.DeviceDpi).ChoiceTextBounds(c.Bounds, c.Density);
        if (bounds.Width > 0 && bounds.Height > 0) owner.DrawText(c with { Bounds = bounds }, text, enabled ? c.Theme.EditorTextColor : SystemColors.GrayText,
            TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
    }
}
