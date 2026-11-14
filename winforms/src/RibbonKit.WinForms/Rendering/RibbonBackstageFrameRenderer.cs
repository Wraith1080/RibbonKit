using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    internal static int BackstageSeamHeight(RibbonTheme theme, int dpi, int height) => Math.Clamp(RibbonLayoutEngine.Scale(
        RibbonPresentationResolver.Backstage(theme).ResolveMetrics(theme, RibbonDensity.Compact).TopSeamHeight, dpi), 0, Math.Max(0, height));
    internal static int BackstageDividerShadowWidth(RibbonTheme theme, int dpi, int available) => Math.Clamp(RibbonLayoutEngine.Scale(
        RibbonPresentationResolver.Backstage(theme).ResolveMetrics(theme, RibbonDensity.Compact).PageDividerShadowWidth, dpi), 0, Math.Max(0, available));
    internal void PaintBackstageFrame(RibbonPaintContext context, int navigationWidth, Action<RibbonPaintContext> paint)
    {
        int seam = BackstageSeamHeight(context.Theme, context.DeviceDpi, context.Bounds.Height);
        var content = context with { Bounds = Rectangle.FromLTRB(context.Bounds.Left, context.Bounds.Top + seam, context.Bounds.Right, context.Bounds.Bottom) };
        if (seam > 0) content = content with { BackgroundBounds = context.BackgroundBounds.IsEmpty ? context.Bounds : context.BackgroundBounds };
        paint(content);
        int shadow = BackstageDividerShadowWidth(context.Theme, context.DeviceDpi, Math.Min(navigationWidth, context.Bounds.Width));
        PaintBackstageDividerShadow(context with { BackstageDividerShadowBounds = new(context.Bounds.Left + Math.Clamp(navigationWidth, 0, context.Bounds.Width) - shadow, content.Bounds.Top, shadow, content.Bounds.Height) });
        if (seam <= 0) return;
        var strip = context with { Bounds = new(context.Bounds.Left, context.Bounds.Top, context.Bounds.Width, seam), BackgroundBounds = context.Bounds };
        var mode = context.Graphics.CompositingMode;
        try
        {
            if (context.CompositedText) context.Graphics.CompositingMode = CompositingMode.SourceCopy;
            PaintFill(strip, context.Theme.BackstageNavigationFill);
            context.Graphics.CompositingMode = CompositingMode.SourceOver;
            PaintFill(strip with { BackgroundBounds = strip.Bounds }, context.Theme.BackstageAppearance?.TopSeamBackground ?? new(RibbonFileButtonRenderer.OpenFill(context.Theme).End));
        }
        finally { context.Graphics.CompositingMode = mode; }
    }
    internal void PaintBackstageDividerShadow(RibbonPaintContext context)
    {
        var shadow = context.BackstageDividerShadowBounds;
        var appearance = context.Theme.BackstageAppearance;
        double opacity = appearance?.PageDividerShadowOpacity ?? .18;
        var color = appearance is null || appearance.PageDividerShadowColor.IsEmpty ? Color.Black : appearance.PageDividerShadowColor;
        if (shadow.Width <= 0 || shadow.Height <= 0 || opacity == 0 || color.A == 0) return;
        var clip = Rectangle.Intersect(context.Bounds, shadow);
        if (clip.Width <= 0 || clip.Height <= 0) return;
        var saved = context.Graphics.Save();
        try
        {
            context.Graphics.SetClip(clip, CombineMode.Intersect);
            context.Graphics.CompositingMode = CompositingMode.SourceOver;
            for (int x = clip.Left; x < clip.Right; x++)
            {
                double coverage = (x - shadow.Left + 1d) / shadow.Width;
                using var brush = new SolidBrush(Color.FromArgb((int)Math.Round(color.A * opacity * coverage * coverage), color));
                context.Graphics.FillRectangle(brush, x, clip.Top, 1, clip.Height);
            }
        }
        finally { context.Graphics.Restore(saved); }
    }
    // Shared page primitive. Siblings resolve their own radius/border defaults.
    internal void DrawRoundedBackstageBackground(RibbonPaintContext context, int navigationWidth)
    {
        PaintFill(context, context.Theme.BackstageNavigationFill);
        int width = Math.Clamp(navigationWidth, 0, context.Bounds.Width);
        var page = new Rectangle(context.Bounds.Left + width, context.Bounds.Top, context.Bounds.Width - width, context.Bounds.Height);
        if (page.Width <= 0 || page.Height <= 0) return;
        int radius = RibbonBackstageLayout.PageCornerRadius(this, context.Theme, context.DeviceDpi, page.Size);
        int stroke = RibbonBackstageLayout.PageBorderWidth(this, context.Theme, context.DeviceDpi, page.Size);
        using var fill = new SolidBrush(context.Theme.Surface); using var border = new SolidBrush(context.Theme.Border);
        context.Graphics.FillRectangle(fill, page.Left + radius, page.Top, page.Width - radius, page.Height);
        context.Graphics.FillRectangle(fill, page.Left, page.Top + radius, radius, page.Height - radius);
        if (radius > 0)
        {
            var state = context.Graphics.Save();
            try
            {
                context.Graphics.SetClip(new Rectangle(page.Location, new Size(radius, radius)), CombineMode.Intersect);
                PaintRounded(context with { Bounds = new(page.Location, new Size(2 * radius, 2 * radius)) }, radius, new(context.Theme.Surface), context.Theme.Border, stroke);
            }
            finally { context.Graphics.Restore(state); }
        }
        context.Graphics.FillRectangle(border, page.Left + radius, page.Top, page.Width - radius, stroke);
        context.Graphics.FillRectangle(border, page.Left, page.Top + radius, stroke, page.Height - radius);
    }
}
