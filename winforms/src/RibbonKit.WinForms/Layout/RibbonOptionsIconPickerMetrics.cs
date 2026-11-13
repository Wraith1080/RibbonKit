namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonOptionsIconPickerDefaults(int SelectionInset, int Border, int CompactIcon, int TouchIcon, int TextInset, int CompactCell, int TouchCell, int FontPadding, int CompactMinimum, int TouchMinimum, int MaximumCell);
internal readonly record struct RibbonOptionsIconPickerMetrics(RibbonTheme Theme, int Dpi, RibbonDensity Density)
{
    private static RibbonOptionsIconPickerDefaults D => RibbonPresentationResolver.OptionsIconPicker;
    private int S(int n) => RibbonLayoutEngine.Scale(n, Dpi);
    private static int InsetLimit(Rectangle bounds) => Math.Max(0, (Math.Min(bounds.Width, bounds.Height) - 1) / 2);
    internal int Radius(int fallback) => Theme.OptionsAppearance?.IconPickerCornerRadius is { } n ? S(n) : fallback;
    internal Color Border => Theme.OptionsAppearance is { IconPickerBorderColor.IsEmpty: false } a ? a.IconPickerBorderColor : Theme.Border;
    internal int CellSide(int fontHeight)
    {
        bool touch = Density == RibbonDensity.Touch;
        int legacyCell = touch ? D.TouchCell : D.CompactCell, legacyIcon = touch ? D.TouchIcon : D.CompactIcon;
        int desired = Theme.OptionsAppearance?.IconPickerIconSize is > 0 and { } size ? S(size) + (int)Math.Ceiling((legacyCell - legacyIcon) * Dpi / 96d) : (int)Math.Ceiling(legacyCell * Dpi / 96d);
        int minimum = Math.Max(fontHeight + (int)Math.Ceiling(D.FontPadding * Dpi / 96d), (int)Math.Ceiling((touch ? D.TouchMinimum : D.CompactMinimum) * Dpi / 96d));
        return Math.Clamp(Math.Max(desired, minimum), 1, D.MaximumCell);
    }
    internal Rectangle TileBounds(Rectangle nativeIcon, int side) => new(nativeIcon.Left, nativeIcon.Top, side, side);
    internal Rectangle SelectionBounds(Rectangle cell)
    {
        int inset = Theme.OptionsAppearance?.IconPickerInset is { } n ? Math.Min(S(n), InsetLimit(cell)) : D.SelectionInset;
        return Rectangle.Inflate(cell, -inset, -inset);
    }
    internal int BorderWidth(Rectangle paint) => Theme.OptionsAppearance?.IconPickerBorderWidth is { } n ? Math.Min(S(n), Math.Max(0, Math.Min(paint.Width, paint.Height) / 2)) : D.Border;
    internal Rectangle IconBounds(Rectangle cell)
    {
        var content = ContentBounds(cell);
        int requested = Theme.OptionsAppearance?.IconPickerIconSize is { } n ? S(n) : (int)Math.Ceiling((Density == RibbonDensity.Touch ? D.TouchIcon : D.CompactIcon) * Dpi / 96d);
        int side = Math.Min(requested, Math.Max(0, Math.Min(content.Width, content.Height)));
        return side > 0 ? new(content.Left + (content.Width - side) / 2, content.Top + (content.Height - side) / 2, side, side) : Rectangle.Empty;
    }
    private Rectangle ContentBounds(Rectangle cell)
    {
        var paint = SelectionBounds(cell);
        if (paint.Width <= 0 || paint.Height <= 0) return Rectangle.Empty;
        return Rectangle.Inflate(paint, -Math.Min(BorderWidth(paint), InsetLimit(paint)), -Math.Min(BorderWidth(paint), InsetLimit(paint)));
    }
    internal Rectangle TextBounds(Rectangle cell)
    {
        var content = ContentBounds(cell);
        int inset = Theme.OptionsAppearance?.IconPickerTextInset is { } n ? Math.Min(S(n), InsetLimit(content)) : D.TextInset;
        return Rectangle.Inflate(content, -inset, -inset);
    }
}
