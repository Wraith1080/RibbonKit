using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// These Application Menu recipes differ from ordinary command chrome. Keep their
// precedence and direct primitive path; public item/split/content/focus hooks
// continue to enter through the actual renderer, without a substitute renderer.
internal static class RibbonApplicationMenuChromeRenderer
{
    internal static void DrawItemState(RibbonRenderer owner, RibbonPaintContext context, bool footer, bool marked, bool open)
    {
        if (!footer && !context.Hot && !context.Pressed && !marked && !open) return;
        var fill = context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : open ? context.Theme.Pressed : marked ? context.Theme.Checked : new RibbonFill(context.Theme.PopupColor);
        Paint(owner, context, fill, footer);
    }
    private static void Paint(RibbonRenderer owner, RibbonPaintContext context, RibbonFill fill, bool footer)
    {
        var metrics = new RibbonApplicationMenuChromeMetrics(context.Theme, context.DeviceDpi, RibbonPresentationResolver.ApplicationMenuChrome);
        var bounds = metrics.PaintBounds(context.Bounds, footer);
        owner.PaintRounded(context with { Bounds = bounds }, metrics.Radius(footer), fill, metrics.Border(footer), metrics.BorderWidth(bounds, footer));
    }
    internal static void DrawSplitState(RibbonRenderer owner, RibbonPaintContext primary, RibbonPaintContext arrow, bool marked, bool open, bool footer)
    {
        bool rowHot = primary.Hot || arrow.Hot;
        bool active = open || marked || rowHot || primary.Pressed || arrow.Pressed;
        if (!footer && !active) return;
        var whole = primary with { Bounds = Rectangle.Union(primary.Bounds, arrow.Bounds), Hot = false, Pressed = false, Focused = false };
        var metrics = new RibbonApplicationMenuChromeMetrics(whole.Theme, whole.DeviceDpi, RibbonPresentationResolver.ApplicationMenuChrome);
        Half(primary, marked, false, open); Half(arrow, false, open, false);
        if (active && whole.Theme.ApplicationMenuSplitDividerVisible)
        {
            var divider = metrics.SplitDivider(whole.Bounds, arrow.Bounds);
            if (divider.Width > 0 && divider.Height > 0 && metrics.SplitInk.A != 0)
            {
                if (whole.Theme.ApplicationMenuAppearance?.SplitDividerWidth is null && whole.Theme.ApplicationMenuAppearance?.SplitDividerInset is null)
                { using var pen = new Pen(metrics.SplitInk); whole.Graphics.DrawLine(pen, divider.Left, divider.Top, divider.Left, divider.Bottom - 1); }
                else { using var brush = new SolidBrush(metrics.SplitInk); whole.Graphics.FillRectangle(brush, divider); }
            }
        }
        void Half(RibbonPaintContext half, bool check, bool expanded, bool selected)
        {
            var saved = whole.Graphics.Save();
            try
            {
                whole.Graphics.SetClip(half.Bounds, CombineMode.Intersect);
                var fill = half.Pressed ? half.Theme.Pressed : rowHot ? half.Theme.Hover : expanded ? half.Theme.Pressed
                    : selected ? metrics.SelectedPrimary : check ? half.Theme.Checked : new RibbonFill(half.Theme.PopupColor);
                Paint(owner, whole, fill, footer);
            }
            finally { whole.Graphics.Restore(saved); }
        }
    }
}
