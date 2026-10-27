using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal static class RibbonArrowPainter
{
    internal static void DrawHorizontal(Graphics graphics, Rectangle slot, int dpi, Color color, bool backward)
    {
        float width = Math.Min(slot.Width, RibbonLayoutEngine.Scale(6, dpi));
        float height = Math.Min(slot.Height, RibbonLayoutEngine.Scale(10, dpi));
        float stroke = Math.Max(1, 1.5f * dpi / 96);
        if (width <= stroke || height <= stroke) return;
        float centerX = slot.Left + (slot.Width - 1) / 2f, centerY = slot.Top + (slot.Height - 1) / 2f;
        float dx = (width - stroke) / 2, dy = (height - stroke) / 2;
        if (backward) dx = -dx;
        var state = graphics.Save();
        try
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(color, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            graphics.DrawLines(pen, [new PointF(centerX - dx, centerY - dy), new PointF(centerX + dx, centerY), new PointF(centerX - dx, centerY + dy)]);
        }
        finally { graphics.Restore(state); }
    }
    internal static void DrawDown(Graphics graphics, Rectangle slot, int dpi, Color color)
    {
        int width = Math.Min(slot.Width, RibbonLayoutEngine.Scale(8, dpi));
        int height = Math.Min(slot.Height, RibbonLayoutEngine.Scale(6, dpi));
        if (width <= 0 || height <= 0)
            return;
        int left = slot.Left + (slot.Width - width) / 2, top = slot.Top + (slot.Height - height) / 2;
        var state = graphics.Save();
        try
        {
            graphics.SetClip(new Rectangle(left, top, width, height), CombineMode.Intersect);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(color);
            graphics.FillPolygon(brush, [new PointF(left, top), new PointF(left + width - 1, top), new PointF(left + (width - 1) / 2f, top + height - 1)]);
        }
        finally { graphics.Restore(state); }
    }
}
