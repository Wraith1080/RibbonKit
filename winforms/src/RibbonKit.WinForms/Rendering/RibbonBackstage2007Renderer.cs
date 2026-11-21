using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed class RibbonBackstage2007Renderer : RibbonBackstageRenderer
{
    internal override int GetBackstagePageBorderWidth(RibbonTheme theme, int dpi) => RibbonLayoutEngine.Scale(theme.BackstagePageBorderWidth ?? 0, dpi);
    internal override int GetBackstagePageCornerRadius(RibbonTheme theme, int dpi) => RibbonLayoutEngine.Scale(theme.BackstagePageCornerRadius ?? 0, dpi);
    internal override void DrawEntryState(RibbonRenderer owner, RibbonPaintContext context, bool selected)
    {
        if (!context.Hot && !context.Pressed && !selected) return;
        if (!context.Theme.BackstageFullWidthNavigation) { PaintNavigationState(owner, context, selected); return; }
        var fill = context.Pressed ? context.Theme.Pressed : selected ? context.Theme.BackstageSelectedFill : context.Theme.BackstageHoverFill;
        owner.PaintWorkspaceNavigation(context with { StateChecked = selected }, fill);
    }
    internal override void DrawBackground(RibbonRenderer owner, RibbonPaintContext context, int navigationWidth)
    {
        var theme = context.Theme;
        var settings = theme.BackstageAppearance?.Workspace;
        var metrics = RibbonPresentationResolver.Backstage(theme).ResolveMetrics(theme, context.Density);
        var geometry = RibbonBackstageWorkspaceGeometry.Measure(context.Bounds, context.DeviceDpi, metrics);
        owner.PaintFill(context, theme.BackstageNavigationFill);
        owner.PaintWorkspaceHeaderBackground(context with { Bounds = geometry.Header, BackgroundBounds = geometry.Header });
        if (geometry.Frame.Width <= 0 || geometry.Frame.Height <= 0) return;
        var border = settings is { BorderColor.IsEmpty: false } ? settings.BorderColor : theme.Border;
        var saved = context.Graphics.Save();
        try
        {
            using var clip = RoundedPainter.CreateOpaqueRegion(geometry.Frame.Size, geometry.Radius);
            clip.Translate(geometry.Frame.Left, geometry.Frame.Top);
            context.Graphics.SetClip(clip, CombineMode.Intersect);
            owner.PaintFill(context with { Bounds = geometry.Frame, BackgroundBounds = geometry.Interior }, theme.BackstageNavigationFill);
            int pageLeft = Math.Clamp(context.Bounds.Left + navigationWidth, geometry.Interior.Left, geometry.Interior.Right);
            var page = Rectangle.FromLTRB(pageLeft, geometry.Interior.Top, geometry.Interior.Right, geometry.Interior.Bottom);
            owner.PaintFill(context with { Bounds = page, BackgroundBounds = page }, new(theme.Surface));
            if (geometry.Border > 0 && pageLeft > geometry.Interior.Left)
            {
                using var ink = new SolidBrush(border);
                context.Graphics.FillRectangle(ink, Math.Max(geometry.Interior.Left, pageLeft - geometry.Border), geometry.Interior.Top,
                    Math.Min(geometry.Border, pageLeft - geometry.Interior.Left), geometry.Interior.Height);
            }
            owner.PaintRounded(context with { Bounds = geometry.Frame }, geometry.Radius, new(Color.Transparent), border, geometry.Border);
            if (geometry.InnerBorder > 0 && settings is { InnerBorderColor.A: > 0 })
                owner.PaintRounded(context with { Bounds = Rectangle.Inflate(geometry.Frame, -geometry.Border, -geometry.Border) },
                    Math.Max(0, geometry.Radius - geometry.Border), new(Color.Transparent), settings.InnerBorderColor, geometry.InnerBorder);
        }
        finally { context.Graphics.Restore(saved); }
    }
    internal static RibbonFill HeaderFill(RibbonTheme theme) => theme.BackstageAppearance?.Workspace?.HeaderBackground
        ?? theme.FileButtonAppearance?.Background ?? theme.IdentityAccent?.AdaptFill(theme.IdentityAccent.FileBackground, theme.Accent, theme.IdentityAccent.FileStrength) ?? new(theme.Accent);
}

public partial class RibbonRenderer
{
    internal void PaintWorkspaceHeaderBackground(RibbonPaintContext c)
    {
        if (!c.BackstageHeaderBackgroundBounds.IsEmpty) c = c with { BackgroundBounds = c.BackstageHeaderBackgroundBounds };
        // Repeated native parent/child paints must not accumulate authored alpha.
        // This opaque presentation gives the header one stable navigation-color base.
        var mode = c.Graphics.CompositingMode;
        try
        {
            c.Graphics.CompositingMode = CompositingMode.SourceOver;
            PaintFill(c, new(Color.FromArgb(255, c.Theme.BackstageNavigationColor)));
            PaintFill(c, RibbonBackstage2007Renderer.HeaderFill(c.Theme));
        }
        finally { c.Graphics.CompositingMode = mode; }
    }
    internal void PaintWorkspaceNavigation(RibbonPaintContext c, RibbonFill fill)
    {
        var appearance = c.Theme.BackstageAppearance;
        var chrome = appearance?.Workspace?.NavigationStateChrome;
        var border = appearance is { NavigationBorderColor.IsEmpty: false } ? appearance.NavigationBorderColor : chrome?.Border(c, Color.Transparent) ?? Color.Transparent;
        int width = appearance?.NavigationBorderWidth is { } n ? RibbonLayoutEngine.Scale(n, c.DeviceDpi) : 1;
        RibbonStateChromeRenderer.Paint(_rounded, c, RibbonLayoutEngine.Scale(appearance?.NavigationCornerRadius ?? 3, c.DeviceDpi), fill, border, width, chrome);
    }

    /// <summary>Paints an Office2007 Backstage header through the existing Back-entry, File-button and glyph hooks. Bounds is the clip/target; BackgroundBounds anchors the full header. The supplied File visual is measured in the same device coordinates, including any caption overlap. It is passive paint; the native Back target owns input.</summary>
    public virtual void DrawBackstageHeader(RibbonPaintContext context, RibbonDropDownItem fileMenu, Rectangle applicationButtonBounds)
    {
        var canvas = context.BackgroundBounds.IsEmpty ? context.Bounds : context.BackgroundBounds;
        PaintWorkspaceHeaderBackground(context with { BackgroundBounds = canvas });
        var saved = context.Graphics.Save();
        try
        {
            context.Graphics.SetClip(context.Bounds, CombineMode.Intersect);
            int S(int n) => RibbonLayoutEngine.Scale(n, context.DeviceDpi);
            int left = Math.Min(canvas.Right, Math.Max(canvas.Left + S(12), applicationButtonBounds.Right + S(8)));
            int right = Math.Min(canvas.Right, Math.Max(left, canvas.Left + S(context.Theme.BackstageAppearance?.Workspace?.ReturnWidth ?? 300)));
            if (RibbonPresentationResolver.Backstage(context.Theme).ResolveMetrics(context.Theme, context.Density).BackButtonVisible)
                DrawBackstageEntry(context with { Bounds = Rectangle.FromLTRB(left, canvas.Top, right, canvas.Bottom), BackgroundBounds = canvas }, null, false);
            if (applicationButtonBounds.Width > 0 && applicationButtonBounds.Height > 0)
            {
                var fileClip = context.Graphics.Save();
                try
                {
                    if (context.SuppressApplicationButton) context.Graphics.SetClip(Rectangle.Empty, CombineMode.Intersect);
                    DrawFileButton(context with { Bounds = applicationButtonBounds, Hot = false, Pressed = false, Focused = false, FileMenuPreviewOpen = true }, fileMenu);
                }
                finally { context.Graphics.Restore(fileClip); }
            }
            var ink = context.Theme.BackstageAppearance?.Workspace is { HeaderForeground.IsEmpty: false } a ? a.HeaderForeground : Color.White;
            int labelRight = Math.Max(canvas.Left, canvas.Right - S(16));
            DrawText(context with { Bounds = Rectangle.FromLTRB(Math.Min(labelRight, right + S(8)), canvas.Top, labelRight, canvas.Bottom) },
                fileMenu.Text, ink, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
        finally { context.Graphics.Restore(saved); }
    }

    private void DrawWorkspaceReturn(RibbonPaintContext c)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, c.DeviceDpi);
        PaintWorkspaceHeaderBackground(c);
        var settings = c.Theme.BackstageAppearance?.Workspace;
        var ink = settings is { HeaderForeground.IsEmpty: false } ? settings.HeaderForeground : Color.White;
        var target = Rectangle.Inflate(c.Bounds, -Math.Min(S(4), c.Bounds.Width / 2), -Math.Min(S(4), c.Bounds.Height / 2));
        if (c.Hot || c.Pressed)
        {
            PaintRounded(c with { Bounds = target }, S(3), c.Pressed ? c.Theme.Pressed : c.Theme.Hover,
                settings?.NavigationStateChrome?.Border(c, c.Theme.Border) ?? c.Theme.Border, 1);
            ink = c.Theme.BackstageNavigationInk;
        }
        if (target.Width > S(24) && target.Height > 0)
        {
            int x = target.Left + S(12), y = target.Top + target.Height / 2;
            using var pen = new Pen(ink, Math.Max(1, S(2))) { LineJoin = LineJoin.Round };
            c.Graphics.DrawLines(pen, new Point[] { new(x + S(5), y - S(5)), new(x, y), new(x + S(5), y + S(5)) });
            c.Graphics.DrawLine(pen, x, y, x + S(13), y);
            DrawText(c with { Bounds = Rectangle.FromLTRB(x + S(24), target.Top, target.Right, target.Bottom) }, settings?.ReturnText ?? "Back to document", ink,
                TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }
        if (c.Focused) DrawFocus(c with { Bounds = target });
    }
}
