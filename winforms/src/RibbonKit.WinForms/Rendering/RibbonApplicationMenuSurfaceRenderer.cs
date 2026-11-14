using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal static class RibbonApplicationMenuSurfaceRenderer
{
    internal static void DrawBackground(RibbonRenderer owner, RibbonPaintContext c, Rectangle navigation, Rectangle pane)
    {
        var m = new RibbonApplicationMenuSurfaceMetrics(c.Theme, c.DeviceDpi, RibbonPresentationResolver.ApplicationMenuSurface);
        // Always establish an opaque native backing before optional authored alpha.
        using (var backing = new SolidBrush(m.DefaultTint)) c.Graphics.FillRectangle(backing, c.Bounds);
        if (c.Theme.ApplicationMenuAppearance?.SurroundBackground is { } surround) Fill(owner, c, surround);
        var frame = RibbonRenderer.ApplicationFrame(c.Bounds, navigation); int popupRadius = owner.GetPopupCornerRadius(c.Theme, c.DeviceDpi);
        owner.PaintRounded(c with { Bounds = frame }, m.Radius(popupRadius), m.Navigation, m.Border, m.BorderWidth(frame));
        int divider = c.ApplicationMenuDividerX ?? pane.Left - RibbonLayoutEngine.Scale(RibbonApplicationMenuMetrics.PanePadding(c.Theme), c.DeviceDpi) - 1;
        var saved = c.Graphics.Save();
        try
        {
            using var clip = m.ItemClip(frame, popupRadius); c.Graphics.SetClip(clip, CombineMode.Intersect);
            var right = new Rectangle(divider + 1, frame.Top + 1, Math.Max(0, frame.Right - divider - 2), Math.Max(0, frame.Height - 2));
            Fill(owner, c with { Bounds = right }, m.Pane(c.ApplicationMenuDefaultPane));
            var line = m.Divider(frame, divider);
            if (line.Width > 0 && line.Height > 0 && m.DividerInk.A != 0)
            {
                if (c.Theme.ApplicationMenuAppearance?.NavigationDividerWidth is null && c.Theme.ApplicationMenuAppearance?.NavigationDividerInset is null)
                { using var pen = new Pen(m.DividerInk); c.Graphics.DrawLine(pen, line.Left, line.Top, line.Left, line.Bottom - 1); }
                else { using var brush = new SolidBrush(m.DividerInk); c.Graphics.FillRectangle(brush, line); }
            }
        }
        finally { c.Graphics.Restore(saved); }
    }
    internal static void DrawHeader(RibbonRenderer owner, RibbonPaintContext c, string text)
    {
        var m = new RibbonApplicationMenuSurfaceMetrics(c.Theme, c.DeviceDpi, RibbonPresentationResolver.ApplicationMenuSurface);
        var background = c.Theme.MenuHeadingBackground ?? (c.ApplicationMenuDefaultPane ? (RibbonFill?)null : c.Theme.GalleryHeadingBackground);
        if (background is { } fill) owner.PaintRounded(c, m.HeaderRadius, fill, Color.Transparent, 0);
        using var font = new Font(c.Font, m.HeaderStyle(c.Font.Style));
        owner.DrawText(c with { Bounds = m.HeaderText(c.Bounds, c.ApplicationMenuDefaultPane), Font = font }, text,
            c.Theme.MenuHeadingForeground.IsEmpty ? c.Theme.Accent : c.Theme.MenuHeadingForeground,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        var rule = m.HeaderRule(c.Bounds);
        if (rule.Width > 0 && rule.Height > 0 && m.HeaderRuleInk.A != 0)
        {
            if (c.Theme.ApplicationMenuAppearance?.HeaderRuleThickness is null)
            { using var pen = new Pen(m.HeaderRuleInk); c.Graphics.DrawLine(pen, rule.Left, rule.Top, rule.Right - 1, rule.Top); }
            else { using var brush = new SolidBrush(m.HeaderRuleInk); c.Graphics.FillRectangle(brush, rule); }
        }
    }
    private static void Fill(RibbonRenderer owner, RibbonPaintContext c, RibbonFill fill)
    {
        if (c.Bounds.Width <= 0 || c.Bounds.Height <= 0) return;
        if (RibbonFillColors.IsSolid(fill)) { using var brush = new SolidBrush(fill.Start); c.Graphics.FillRectangle(brush, c.Bounds); }
        else owner.PaintRounded(c, 0, fill, Color.Transparent, 0);
    }
}
