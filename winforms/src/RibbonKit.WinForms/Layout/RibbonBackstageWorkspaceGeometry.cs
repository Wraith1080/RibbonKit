namespace RibbonKit.WinForms.Layout;

// One value snapshot drives native placement and both shared paint paths.
internal readonly record struct RibbonBackstageWorkspaceGeometry(Rectangle Header, Rectangle Frame, Rectangle Interior, int Border, int InnerBorder, int Radius)
{
    internal static RibbonBackstageWorkspaceGeometry Measure(Rectangle bounds, int dpi, RibbonBackstageMetrics metrics)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        int header = Math.Min(bounds.Height, S(metrics.WorkspaceHeaderHeight));
        var headerBounds = new Rectangle(bounds.Left, bounds.Top, bounds.Width, header);
        var area = Rectangle.FromLTRB(bounds.Left, bounds.Top + header, bounds.Right, bounds.Bottom);
        int inset = Math.Clamp(S(metrics.WorkspaceInset), 0, Math.Max(0, Math.Min(area.Width, area.Height) / 2));
        var frame = Rectangle.Inflate(area, -inset, -inset);
        int maximum = Math.Max(0, (Math.Min(frame.Width, frame.Height) - 1) / 2);
        int border = Math.Clamp(metrics.WorkspaceBorderWidth is { } b ? S(b) : 1, 0, maximum);
        int inner = Math.Clamp(metrics.WorkspaceInnerBorderWidth is { } i ? S(i) : 1, 0, maximum - border);
        return new(headerBounds, frame, Rectangle.Inflate(frame, -border - inner, -border - inner), border, inner,
            Math.Clamp(S(metrics.WorkspaceRadius), 0, Math.Max(0, Math.Min(frame.Width, frame.Height) / 2)));
    }
}
