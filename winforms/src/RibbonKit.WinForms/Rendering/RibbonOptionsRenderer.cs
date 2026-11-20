using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Decoration over existing native rows/panels. Do not route through command chrome
// or introduce renderer interception where the native dialog painted text directly.
internal static class RibbonOptionsRenderer
{
    internal static Color Background(RibbonTheme theme) => theme.OptionsAppearance is { BackgroundColor.IsEmpty: false } a ? a.BackgroundColor : theme.Surface;
    internal static Color NavigationBackground(RibbonTheme theme) => theme.OptionsAppearance is { NavigationBackgroundColor.IsEmpty: false } a ? a.NavigationBackgroundColor : theme.BackstageNavigationBackground;
    internal static Color ListBackground(RibbonTheme theme) => theme.OptionsAppearance is { ListBackgroundColor.IsEmpty: false } a ? a.ListBackgroundColor : theme.Surface;
    internal static Color LabelForeground(RibbonTheme theme) => theme.OptionsAppearance is { LabelForeground.IsEmpty: false } a ? a.LabelForeground : theme.Foreground;
    internal static void DrawState(RoundedPainter painter, RibbonPaintContext c, RibbonFill fill, int radius, bool selected, int? width, Color ink, RibbonStateChromeSettings? chrome)
    {
        var state = c with { StateChecked = selected, StateHot = c.Hot || c.StateHot };
        var border = !ink.IsEmpty ? ink : chrome?.Border(state, selected ? c.Theme.Accent : c.Theme.Border) ?? (selected ? c.Theme.Accent : c.Theme.Border);
        int stroke = width is { } n ? Math.Min((int)Math.Ceiling(n * c.DeviceDpi / 96d), Math.Max(0, Math.Min(c.Bounds.Width, c.Bounds.Height) / 2)) : chrome?.HasOverrides == true && border.A != 0 ? 1 : 0;
        RibbonStateChromeRenderer.Paint(painter, state, radius, fill, border, stroke, chrome);
    }
    internal static void DrawNavigation(RibbonRenderer owner, RoundedPainter painter, RibbonPaintContext c, string text, bool selected, bool focused)
    {
        var theme = c.Theme; var a = theme.OptionsAppearance; var m = new RibbonOptionsAppearanceMetrics(theme, c.DeviceDpi);
        using var background = new SolidBrush(NavigationBackground(theme)); c.Graphics.FillRectangle(background, c.Bounds);
        var bounds = m.SelectionBounds(c.Bounds);
        if (!selected && c.Hot && (a?.NavigationHoverBackground is not null || a?.NavigationStateChrome is not null))
            DrawState(painter, c with { Bounds = bounds }, a?.NavigationHoverBackground ?? new(Color.Transparent), m.Radius(owner.GetPopupCornerRadius(theme, c.DeviceDpi)), false, a?.NavigationBorderWidth, a?.NavigationBorderColor ?? Color.Empty, a?.NavigationStateChrome);
        if (selected)
        {
            DrawState(painter, c with { Bounds = bounds }, a?.NavigationSelectedBackground ?? theme.Checked, m.Radius(owner.GetPopupCornerRadius(theme, c.DeviceDpi)), true, a?.NavigationBorderWidth, a?.NavigationBorderColor ?? Color.Empty, a?.NavigationStateChrome);
            var marker = m.MarkerBounds(bounds);
            painter.Draw(c.Graphics, marker, RibbonFrameMetrics.Radius(theme.NavigationMarkerCornerRadius, marker.Width / 2, c.DeviceDpi), a?.NavigationMarkerFill ?? new(theme.Accent), Color.Transparent, 0);
        }
        TextRenderer.DrawText(c.Graphics, text, c.Font, m.TextBounds(bounds), selected ? theme.Accent : theme.Foreground, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if (selected && focused) ControlPaint.DrawFocusRectangle(c.Graphics, m.FocusBounds(c.Bounds), theme.Accent, NavigationBackground(theme));
    }
    internal static void DrawDivider(Graphics g, Rectangle bounds, RibbonTheme theme, int dpi, bool vertical)
    {
        var a = theme.OptionsAppearance; var ink = a is { DividerColor.IsEmpty: false } ? a.DividerColor : theme.Border;
        if (a?.DividerWidth is null)
        {
            using var pen = new Pen(ink);
            if (vertical) g.DrawLine(pen, bounds.Right - 1, bounds.Top, bounds.Right - 1, bounds.Bottom);
            else g.DrawLine(pen, bounds.Left, bounds.Top, bounds.Right, bounds.Top);
            return;
        }
        int width = Math.Min(new RibbonOptionsAppearanceMetrics(theme, dpi).DividerWidth, Math.Max(0, vertical ? bounds.Width : bounds.Height));
        if (width <= 0 || ink.A == 0) return;
        using var brush = new SolidBrush(ink);
        g.FillRectangle(brush, vertical ? new(bounds.Right - width, bounds.Top, width, bounds.Height) : new Rectangle(bounds.Left, bounds.Top, bounds.Width, width));
    }
}
