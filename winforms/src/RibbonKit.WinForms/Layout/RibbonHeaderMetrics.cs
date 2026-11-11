namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonHeaderLayoutDefaults(int TabPadding, int TabMinimum, int FileMinimum,
    int IndicatorPadding, int IndicatorThickness, int IndicatorBottomInset, int FilledCornerRadius);

// Resolved logical values only; no live ribbon, window, palette or family choice.
// The shared strip engine continues to measure text and reserve File/QAT/overflow.
internal readonly record struct RibbonHeaderMetrics
{
    internal RibbonHeaderMetrics(RibbonTheme theme, RibbonDensity density, RibbonHeaderLayoutDefaults defaults)
    {
        TabPadding = theme.TabHorizontalPadding ?? defaults.TabPadding;
        TabMinimum = Math.Max(theme.TabMinimumWidth ?? defaults.TabMinimum, density == RibbonDensity.Touch ? 44 : 1);
        FileMinimum = defaults.FileMinimum;
        IndicatorPadding = theme.HeaderIndicatorPadding ?? defaults.IndicatorPadding;
        IndicatorThickness = theme.HeaderIndicatorThickness ?? defaults.IndicatorThickness;
        IndicatorBottomInset = theme.HeaderIndicatorBottomInset ?? defaults.IndicatorBottomInset;
        FilledCornerRadius = theme.HeaderFilledCornerRadius ?? defaults.FilledCornerRadius;
        _scrollWidth = theme.HeaderScrollButtonWidth;
        _matchTabHeight = theme.HeaderScrollButtonsMatchTabHeight;
        _scrollMinimum = RibbonDensityMetrics.For(density).ScrollButton;
        _touch = density == RibbonDensity.Touch;
    }
    internal int TabPadding { get; }
    internal int TabMinimum { get; }
    internal int FileMinimum { get; }
    internal int IndicatorPadding { get; }
    internal int IndicatorThickness { get; }
    internal int IndicatorBottomInset { get; }
    internal int FilledCornerRadius { get; }
    private readonly int? _scrollWidth;
    private readonly bool _matchTabHeight, _touch;
    private readonly int _scrollMinimum;
    internal int HeaderScrollWidth(int dpi, int tabHeight)
    {
        int legacy = RibbonLayoutEngine.Scale(_scrollMinimum, dpi);
        int desired = _scrollWidth is { } width ? RibbonLayoutEngine.Scale(width, dpi) : _matchTabHeight ? tabHeight : legacy;
        return _touch ? Math.Max(legacy, desired) : desired;
    }
    internal Rectangle IndicatorBounds(Rectangle bounds, int labelWidth, int dpi)
    {
        int padding = RibbonLayoutEngine.Scale(IndicatorPadding, dpi);
        int width = Math.Max(0, Math.Min(labelWidth + 2 * padding, bounds.Width - 2 * padding));
        int height = Math.Min(RibbonLayoutEngine.Scale(IndicatorThickness, dpi), Math.Max(0, bounds.Height - RibbonLayoutEngine.Scale(IndicatorBottomInset, dpi)));
        if (width == 0 || height == 0) return Rectangle.Empty;
        return new(bounds.Left + (bounds.Width - width) / 2, bounds.Bottom - height - RibbonLayoutEngine.Scale(IndicatorBottomInset, dpi), width, height);
    }
    internal static void Validate(RibbonTheme theme)
    {
        if (theme.HeaderIndicatorThickness is < 0 or > 64 || theme.HeaderFilledCornerRadius is < 0 or > 64 || theme.HeaderIndicatorBottomInset is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Header thickness/radius must be 0..64, bottom inset 0..1024, or null for existing effects.");
    }
}
