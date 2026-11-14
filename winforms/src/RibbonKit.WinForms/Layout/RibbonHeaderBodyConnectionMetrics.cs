namespace RibbonKit.WinForms.Layout;

// Decoration from existing measured bounds only. No layout, live models or HWNDs.
internal static class RibbonHeaderBodyConnectionMetrics
{
    internal static bool Enabled(RibbonTheme theme) => theme.HeaderPresentation == RibbonHeaderPresentation.Filled && theme.HeaderAppearance?.ConnectSelectedTabToBody == true;
    internal static Color TabBorder(RibbonTheme theme) => theme.HeaderAppearance is { FilledBorderColor.IsEmpty: false } a ? a.FilledBorderColor : theme.BodyBorderColor;
    internal static int TabStroke(RibbonTheme theme, int dpi) => TabBorder(theme).A == 0 ? 0
        : RibbonBodyFrameMetrics.Width(theme.HeaderAppearance?.FilledBorderWidth ?? theme.BodyAppearance?.BorderWidth, dpi);
    internal static Rectangle Resolve(RibbonTheme theme, int dpi, Rectangle body, Rectangle tab, Rectangle clip)
    {
        if (!Enabled(theme) || body.Width <= 0 || body.Height <= 0 || tab.Bottom != body.Top || Rectangle.Intersect(tab, clip).Width <= 0) return Rectangle.Empty;
        int tabStroke = Math.Min(TabStroke(theme, dpi), Math.Max(0, Math.Min(tab.Width, tab.Height) / 2));
        int bodyStroke = Math.Min(RibbonBodyFrameMetrics.Width(theme.BodyAppearance?.BorderWidth, dpi), Math.Max(0, Math.Min(body.Width, body.Height) / 2));
        int left = Math.Max(Math.Max(tab.Left + tabStroke, clip.Left), body.Left + bodyStroke);
        int right = Math.Min(Math.Min(tab.Right - tabStroke, clip.Right), body.Right - bodyStroke);
        return new(left, body.Top, Math.Max(0, right - left), body.Height);
    }
}
