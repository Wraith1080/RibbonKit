using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal static class RibbonHeaderBodyRenderer
{
    internal static void DrawSelectedTab(RibbonRenderer owner, RibbonPaintContext c, int radius, RibbonFill fill)
    {
        int stroke = Math.Min(RibbonHeaderBodyConnectionMetrics.TabStroke(c.Theme, c.DeviceDpi), Math.Max(0, Math.Min(c.Bounds.Width, c.Bounds.Height) / 2));
        bool joined = c.HeaderBodyConnectionBounds.Height > 0 && c.HeaderBodyConnectionBounds.Top == c.Bounds.Bottom;
        var gap = joined ? new Rectangle(c.Bounds.Left + stroke, c.Bounds.Bottom - stroke, Math.Max(0, c.Bounds.Width - 2 * stroke), stroke) : Rectangle.Empty;
        DrawFrame(owner, c with { IsBottomEdgeConnected = joined }, radius, fill, RibbonHeaderBodyConnectionMetrics.TabBorder(c.Theme), stroke, gap);
    }
    internal static void DrawBody(RibbonRenderer owner, RibbonPaintContext c, int radius, RibbonFill fill, Color border, int stroke)
    {
        stroke = Math.Clamp(stroke, 0, Math.Max(0, Math.Min(c.Bounds.Width, c.Bounds.Height) / 2));
        var connection = c.HeaderBodyConnectionBounds;
        var gap = connection.Height > 0 && connection.Top == c.Bounds.Top
            ? Rectangle.Intersect(c.Bounds, new(connection.Left, connection.Top, connection.Width, Math.Min(stroke, c.Bounds.Height))) : Rectangle.Empty;
        DrawFrame(owner, c, radius, fill, border, stroke, gap);
    }
    private static void DrawFrame(RibbonRenderer owner, RibbonPaintContext c, int radius, RibbonFill fill, Color border, int stroke, Rectangle gap)
    {
        if (gap.Width <= 0 || gap.Height <= 0) { owner.PaintRounded(c, radius, fill, border, stroke); return; }
        // Disjoint clips paint every pixel once, retaining authored alpha and the
        // original full-frame gradient instead of erasing a seam after painting it.
        var saved = c.Graphics.Save();
        try { c.Graphics.ExcludeClip(gap); owner.PaintRounded(c, radius, fill, border, stroke); }
        finally { c.Graphics.Restore(saved); }
        saved = c.Graphics.Save();
        try { c.Graphics.SetClip(gap, CombineMode.Intersect); owner.PaintRounded(c, radius, fill, Color.Transparent, 0); }
        finally { c.Graphics.Restore(saved); }
    }
}
