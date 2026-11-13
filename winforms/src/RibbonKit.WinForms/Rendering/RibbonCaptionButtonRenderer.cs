using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Decoration only. RibbonForm supplies native targets/state and restores the
// correct caption underlay; input, window actions and title paint retain owners.
internal static class RibbonCaptionButtonRenderer
{
    internal static void Draw(RoundedPainter painter, RibbonPaintContext c, int hit, bool restore, bool foregroundOnly, Action<Rectangle> restoreBackground)
    {
        if (c.Bounds.Width <= 0 || c.Bounds.Height <= 0) return;
        var a = c.Theme.CaptionButtonAppearance;
        var metrics = new RibbonCaptionButtonMetrics(c.Theme, c.DeviceDpi);
        RibbonFill fill = hit == 20
            ? (c.Pressed ? a?.ClosePressedBackground : a?.CloseHoverBackground) ?? new(Color.FromArgb(c.CompositedText ? c.Pressed ? 235 : 210 : 255, 232, 17, 35))
            : (c.Pressed ? a?.PressedBackground : a?.HoverBackground) ?? new(!c.CompositedText ? c.Pressed ? c.Theme.Pressed.Start : c.Theme.Hover.Start
                : c.Pressed ? c.Theme.BackdropPressed?.Start ?? Color.FromArgb(40, c.Theme.Foreground) : c.Theme.BackdropHover?.Start ?? Color.FromArgb(24, c.Theme.Foreground));
        var state = c.Graphics.Save();
        try
        {
            c.Graphics.SetClip(c.Bounds, CombineMode.Intersect);
            if (c.Hot) PaintState();
            var glyph = metrics.GlyphBounds(c.Bounds); float stroke = metrics.Stroke(c.Bounds);
            if (glyph.IsEmpty || stroke <= 0) return;
            var color = hit == 20 && c.Hot ? a is { CloseHotGlyphColor.IsEmpty: false } ? a.CloseHotGlyphColor : Color.White
                : a is { GlyphColor.IsEmpty: false } ? a.GlyphColor : c.Theme.Foreground;
            using var pen = new Pen(color, stroke);
            int x = glyph.Left, y = glyph.Top, size = glyph.Width;
            if (hit == 8) c.Graphics.DrawLine(pen, x, y + size / 2, x + size, y + size / 2);
            else if (hit == 9 && restore)
            {
                int offset = metrics.RestoreOffset(size), side = size - offset;
                c.Graphics.DrawRectangle(pen, x + offset, y, side, side);
                var front = new Rectangle(x, y + offset, side, side);
                var mode = c.Graphics.CompositingMode;
                try
                {
                    if (c.CompositedText || foregroundOnly) c.Graphics.CompositingMode = CompositingMode.SourceCopy;
                    restoreBackground(front);
                }
                finally { c.Graphics.CompositingMode = mode; }
                if (c.Hot)
                {
                    var clip = c.Graphics.Save();
                    try { c.Graphics.SetClip(front, CombineMode.Intersect); PaintState(); }
                    finally { c.Graphics.Restore(clip); }
                }
                c.Graphics.DrawRectangle(pen, front);
            }
            else if (hit == 9) c.Graphics.DrawRectangle(pen, glyph);
            else { c.Graphics.DrawLine(pen, x, y, x + size, y + size); c.Graphics.DrawLine(pen, x + size, y, x, y + size); }
        }
        finally { c.Graphics.Restore(state); }

        void PaintState()
        {
            var bounds = metrics.StateBounds(c.Bounds);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            int radius = metrics.Radius(bounds);
            if (radius == 0 && fill.Start == fill.End) { using var brush = new SolidBrush(fill.Start); c.Graphics.FillRectangle(brush, bounds); }
            else painter.Draw(c.Graphics, bounds, radius, fill, Color.Transparent, stroke: 0);
        }
    }
}
