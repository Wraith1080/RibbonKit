using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Shared frame algorithms over resolved inputs. Public renderer hooks/caches own
// the drawing; command/caption layout and reduction remain in their existing paths.
internal static class RibbonBodyRenderer
{
    internal static bool PaintConnectedMessageShadow(RibbonRenderer owner, RibbonPaintContext context, Rectangle upper, Rectangle messages)
    {
        owner.PaintShadow(context with { Bounds = Rectangle.Union(upper, messages), IsTopEdgeConnected = false, IsBottomEdgeConnected = false }, owner.GetBodyCornerRadius(context.Theme, context.DeviceDpi));
        return true;
    }
    internal static void DrawBody(RibbonRenderer owner, RibbonPaintContext context, in RibbonBodyFrameMetrics metrics)
    {
        if (!metrics.BorderFallback && context.Theme.BodyBackground is null && context.Theme.BodyBorder.IsEmpty)
        { using var surface = new SolidBrush(context.Theme.Surface); context.Graphics.FillRectangle(surface, context.Bounds); return; }
        int radius = metrics.PaintShadow ? owner.GetBodyCornerRadius(context.Theme, context.DeviceDpi) : metrics.Radius;
        if (metrics.PaintShadow) owner.PaintShadow(context, radius);
        var border = metrics.BorderFallback ? context.Theme.BodyBorderColor : context.Theme.BodyBorder.IsEmpty ? Color.Transparent : context.Theme.BodyBorder;
        if (metrics.BottomBorderOnly)
        {
            owner.PaintRounded(context, radius, context.Theme.BodyFill, Color.Transparent, 0);
            int width = Math.Min(metrics.BorderWidth, Math.Max(0, context.Bounds.Height));
            using var brush = new SolidBrush(border);
            if (width > 0) context.Graphics.FillRectangle(brush, context.Bounds.Left, context.Bounds.Bottom - width, context.Bounds.Width, width);
        }
        else if (RibbonHeaderBodyConnectionMetrics.Enabled(context.Theme))
            RibbonHeaderBodyRenderer.DrawBody(owner, context, radius, context.Theme.BodyFill, border, !metrics.BorderFallback && context.Theme.BodyBorder.IsEmpty ? 0 : metrics.BorderWidth);
        else owner.PaintRounded(context, radius, context.Theme.BodyFill, border, !metrics.BorderFallback && context.Theme.BodyBorder.IsEmpty ? 0 : metrics.BorderWidth);
    }
    internal static void DrawMessageBar(RibbonRenderer owner, RibbonPaintContext context)
    {
        int radius = owner.GetBodyCornerRadius(context.Theme, context.DeviceDpi); owner.PaintShadow(context, radius);
        var saved = context.Graphics.Save();
        try
        {
            context.Graphics.SetClip(context.Bounds, CombineMode.Intersect);
            var bounds = context.Bounds;
            // The preceding body/QAT owns the shared horizontal edge.
            if (context.IsTopEdgeConnected) { int width = RibbonBodyFrameMetrics.MessageBorderWidth(context); bounds.Y -= width; bounds.Height += width; }
            owner.PaintMessageFrame(context with { Bounds = bounds }, radius);
        }
        finally { context.Graphics.Restore(saved); }
    }
    internal static void DrawJoinedQuickAccess(RibbonRenderer owner, RibbonPaintContext context)
    {
        if (!context.IsQuickAccessToolbarJoined) { owner.DrawBody(context); return; }
        if (context.Bounds.Width <= 2) return;
        var appearance = context.Theme.BodyAppearance;
        int inset = appearance?.QuickAccessSeparatorInset is { } logical ? Math.Min(RibbonLayoutEngine.Scale(logical, context.DeviceDpi), context.Bounds.Width / 2) : context.Theme.BodyBottomBorderOnly ? 0 : 1;
        int width = Math.Min(context.Bounds.Height, RibbonBodyFrameMetrics.Width(appearance?.QuickAccessSeparatorWidth, context.DeviceDpi));
        var color = appearance is null || appearance.QuickAccessSeparatorColor.IsEmpty ? context.Theme.BodyBorderColor : appearance.QuickAccessSeparatorColor;
        using var brush = new SolidBrush(color);
        if (width > 0) context.Graphics.FillRectangle(brush, context.Bounds.Left + inset, context.Bounds.Top, context.Bounds.Width - 2 * inset, width);
    }
    internal static void DrawGroupFrame(RibbonRenderer owner, RibbonPaintContext context)
    {
        if (context.IsSimplifiedGroup) return;
        var frame = context.GroupSeparatorBounds.IsEmpty ? context.Bounds : context.GroupSeparatorBounds;
        int inset = Math.Min(frame.Height / 2, RibbonLayoutEngine.Scale(new RibbonChromeMetrics(context.Theme, context.Density).SeparatorInset(), context.DeviceDpi));
        var bounds = RibbonBodyFrameMetrics.SeparatorBounds(context.Theme, context.DeviceDpi, new(frame.Right - 1, frame.Top + inset, 1, frame.Height - 2 * inset));
        bounds = Rectangle.Intersect(frame, bounds);
        if (bounds.Width > 0 && bounds.Height > 0) owner.DrawGroupSeparator(context with { Bounds = bounds });
    }
}
