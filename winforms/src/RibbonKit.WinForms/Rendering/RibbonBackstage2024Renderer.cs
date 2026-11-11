using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed class RibbonBackstage2024Renderer : RibbonBackstageRenderer
{
    internal override void DrawEntryState(RibbonRenderer owner, RibbonPaintContext context, bool selected)
    {
        if (!context.Hot && !context.Pressed && !selected) return;
        PaintNavigationState(owner, context, selected);
    }
    internal override int GetBackstagePageBorderWidth(RibbonTheme theme, int dpi) => theme.BackstagePageBorderWidth is { } width ? RibbonLayoutEngine.Scale(width, dpi) : 1;
    internal override int GetBackstagePageCornerRadius(RibbonTheme theme, int dpi) => RibbonLayoutEngine.Scale(theme.BackstagePageCornerRadius ?? 2d * theme.CornerRadius, dpi);
    internal override void DrawBackground(RibbonRenderer owner, RibbonPaintContext context, int navigationWidth)
    {
        using var rail = new SolidBrush(context.Theme.BackstageNavigationColor);
        context.Graphics.FillRectangle(rail, context.Bounds);
        int width = Math.Clamp(navigationWidth, 0, context.Bounds.Width);
        var page = new Rectangle(context.Bounds.Left + width, context.Bounds.Top, context.Bounds.Width - width, context.Bounds.Height);
        if (page.Width <= 0 || page.Height <= 0) return;
        int radius = Layout.RibbonBackstageLayout.PageCornerRadius(owner, context.Theme, context.DeviceDpi, page.Size);
        int stroke = Layout.RibbonBackstageLayout.PageBorderWidth(owner, context.Theme, context.DeviceDpi, page.Size);
        using var fill = new SolidBrush(context.Theme.Surface); using var border = new SolidBrush(context.Theme.Border);
        context.Graphics.FillRectangle(fill, page.Left + radius, page.Top, page.Width - radius, page.Height);
        context.Graphics.FillRectangle(fill, page.Left, page.Top + radius, radius, page.Height - radius);
        if (radius > 0)
        {
            var state = context.Graphics.Save();
            try
            {
                // Cache only the small corner. Page resizing must not allocate
                // a full-page rounded bitmap for each intermediate size.
                context.Graphics.SetClip(new Rectangle(page.Location, new Size(radius, radius)), System.Drawing.Drawing2D.CombineMode.Intersect);
                owner.PaintRounded(context with { Bounds = new(page.Location, new Size(2 * radius, 2 * radius)) }, radius, new(context.Theme.Surface), context.Theme.Border, stroke);
            }
            finally { context.Graphics.Restore(state); }
        }
        context.Graphics.FillRectangle(border, page.Left + radius, page.Top, page.Width - radius, stroke);
        context.Graphics.FillRectangle(border, page.Left, page.Top + radius, stroke, page.Height - radius);
    }
}
