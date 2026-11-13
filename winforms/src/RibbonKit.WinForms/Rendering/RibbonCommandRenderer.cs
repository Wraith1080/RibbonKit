using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Model-free state chrome over existing primary/dropdown bounds. Content, events,
// focus, layout and popup ownership stay on the actual public renderer/runtime.
internal static class RibbonCommandRenderer
{
    internal static void DrawSplitChrome(RibbonRenderer owner, RibbonPaintContext primary, RibbonPaintContext dropDown,
        RibbonPaintContext whole, bool check, bool open, bool vertical, bool quickAccess, bool menu)
    {
        if (!check && !open && !primary.Hot && !dropDown.Hot && !primary.Pressed && !dropDown.Pressed) return;
        Half(primary, check, false); Half(dropDown, false, open);
        var metrics = new RibbonCommandAppearanceMetrics(whole.Theme, whole.DeviceDpi, 0, Color.Transparent, RibbonPresentationResolver.CommandChrome);
        var seam = metrics.SeparatorBounds(whole.Bounds, dropDown.Bounds, vertical);
        if (seam.Width > 0 && seam.Height > 0) { using var brush = new SolidBrush(metrics.SplitColor); whole.Graphics.FillRectangle(brush, seam); }
        void Half(RibbonPaintContext part, bool marked, bool expanded)
        {
            var saved = whole.Graphics.Save();
            try
            {
                whole.Graphics.SetClip(part.Bounds, CombineMode.Intersect);
                bool active = expanded || part.Pressed || part.Hot || marked;
                var theme = quickAccess ? whole.Theme : part.Theme;
                var fill = active ? theme.CommandStateFill(part.Hot, expanded || part.Pressed, marked) : new RibbonFill(quickAccess ? theme.Surface : Color.Transparent);
                if (!active && whole.Theme.CommandAppearance?.SplitCompanionBackground is { } authored) fill = authored;
                else if (quickAccess) fill = !active && whole.CompositedText && !menu ? new(Color.Transparent)
                    : RibbonRenderer.QuickAccessFill(whole, fill, menu, pressed: part.Pressed || expanded, marked: marked && !part.Hot);
                owner.PaintCommandState(whole, fill, quickAccess ? Color.Transparent : whole.Theme.Border);
            }
            finally { whole.Graphics.Restore(saved); }
        }
    }
}

public partial class RibbonRenderer
{
    internal void PaintCommandState(RibbonPaintContext context, RibbonFill fill, Color border)
        => DrawChrome(context with { IsCommandState = true }, fill, border);
    private bool TryPaintCommandState(RibbonPaintContext context, RibbonFill fill, Color border, int defaultRadius)
    {
        if (!context.IsCommandState || context.Theme.CommandAppearance is not { HasChromeOverrides: true }) return false;
        var metrics = new RibbonCommandAppearanceMetrics(context.Theme, context.DeviceDpi, defaultRadius, border, RibbonPresentationResolver.CommandChrome);
        PaintRounded(context with { Bounds = metrics.StateBounds(context.Bounds) }, metrics.Radius, fill, metrics.Border, metrics.BorderWidth);
        return true;
    }
}
