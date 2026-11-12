namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonOptionsNavigationDefaults(int SelectionInset, int TextPadding, int MarkerWidth, int MarkerInset, int MarkerDeduction, int FocusInset, int DividerWidth);

internal readonly record struct RibbonOptionsAppearanceMetrics(RibbonTheme Theme, int Dpi)
{
    private RibbonOptionsAppearanceSettings? A => Theme.OptionsAppearance;
    private static RibbonOptionsNavigationDefaults D => RibbonPresentationResolver.OptionsNavigation;
    // Options controls use ceiling, including the legacy summed marker deduction.
    private int S(int n) => (int)Math.Ceiling(n * Dpi / 96d);
    internal Rectangle SelectionBounds(Rectangle bounds)
    {
        int inset = A?.NavigationSelectionInset is { } n ? Math.Min(S(n), Math.Max(0, bounds.Height / 2)) : S(D.SelectionInset);
        return Rectangle.Inflate(bounds, 0, -inset);
    }
    internal Rectangle TextBounds(Rectangle bounds)
    {
        int inset = A?.NavigationTextPadding is { } n ? Math.Min(S(n), Math.Max(0, bounds.Width / 2)) : S(D.TextPadding);
        return Rectangle.Inflate(bounds, -inset, 0);
    }
    internal Rectangle MarkerBounds(Rectangle bounds)
    {
        int requested = S(A?.NavigationMarkerInset ?? D.MarkerInset);
        int inset = A?.NavigationMarkerInset is not null ? Math.Min(requested, Math.Max(0, bounds.Height / 2)) : requested;
        int deduction = A?.NavigationMarkerInset is not null ? 2 * requested : S(D.MarkerDeduction);
        return new(bounds.Left, bounds.Top + inset, Math.Min(Math.Max(0, bounds.Width), S(A?.NavigationMarkerWidth ?? D.MarkerWidth)), Math.Max(0, bounds.Height - deduction));
    }
    internal Rectangle FocusBounds(Rectangle bounds)
    {
        int inset = A?.NavigationFocusInset is { } n ? Math.Min(S(n), Math.Max(0, (Math.Min(bounds.Width, bounds.Height) - 1) / 2)) : D.FocusInset;
        return Rectangle.Inflate(bounds, -inset, -inset);
    }
    internal int Radius(int actual) => A?.NavigationCornerRadius is { } n ? S(n) : actual;
    internal int DividerWidth => A?.DividerWidth is { } n ? S(n) : D.DividerWidth;
}
