namespace RibbonKit.WinForms.Layout;

// Illustrated choice rows have their own artwork/text slots. Ordinary menu,
// QAT projection and Application Menu-specific rows retain their own metrics.
internal readonly record struct RibbonIllustratedMenuMetrics(RibbonDensity Density, RibbonTheme? Theme)
{
    private const int LegacyRowPadding = 12, DefaultDescriptionGap = 2;
    internal static RibbonIllustratedMenuMetrics For(RibbonDensity density, RibbonTheme? theme = null) => new(density, theme);
    internal int MinimumWidth => Theme?.IllustratedMenuMinimumWidth ?? 300;
    internal int MinimumRowHeight => Math.Max(Theme?.IllustratedMenuMinimumRowHeight ?? 64, Density == RibbonDensity.Touch ? 44 : 1);
    internal int IconSize => Theme?.IllustratedMenuIconSize ?? 36;
    internal int Leading => Theme?.IllustratedMenuLeadingPadding ?? 8;
    internal int Trailing => Theme?.IllustratedMenuTrailingPadding ?? 8;
    internal int IconTextGap => Theme?.IllustratedMenuIconTextGap ?? 12;
    internal int DescriptionGap => Theme?.IllustratedMenuDescriptionGap ?? DefaultDescriptionGap;
    internal bool HasOverride => Theme?.IllustratedMenuMinimumWidth is not null || Theme?.IllustratedMenuMinimumRowHeight is not null ||
        Theme?.IllustratedMenuIconSize is not null || Theme?.IllustratedMenuLeadingPadding is not null || Theme?.IllustratedMenuTrailingPadding is not null ||
        Theme?.IllustratedMenuIconTextGap is not null || Theme?.IllustratedMenuDescriptionGap is not null;

    internal int RowHeight(Font font, int dpi, bool description)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        // Preserve the old summed padding/rounding. A changed description gap
        // adds its exact device-pixel difference to the measured stack.
        int text = font.Height * (description ? 3 : 1) + S(LegacyRowPadding);
        if (description) text += S(DescriptionGap) - S(DefaultDescriptionGap);
        int height = Math.Max(S(MinimumRowHeight), text);
        return HasOverride ? Math.Max(height, S(IconSize) + S(LegacyRowPadding)) : height;
    }
    internal int ContentWidth(int dpi) => RibbonLayoutEngine.Scale(Leading, dpi) + RibbonLayoutEngine.Scale(IconSize, dpi)
        + RibbonLayoutEngine.Scale(IconTextGap, dpi) + RibbonLayoutEngine.Scale(Trailing, dpi);
    internal int WidthPadding(int dpi, int legacy) => HasOverride
        ? Math.Max(RibbonLayoutEngine.Scale(legacy, dpi), ContentWidth(dpi) + RibbonLayoutEngine.Scale(8, dpi))
        : RibbonLayoutEngine.Scale(legacy, dpi);

    internal static void Validate(RibbonTheme theme)
    {
        if (theme.IllustratedMenuMinimumWidth is < 1 or > 1024 || theme.IllustratedMenuMinimumRowHeight is < 1 or > 1024 ||
            theme.IllustratedMenuIconSize is < 1 or > 1024 || theme.IllustratedMenuLeadingPadding is < 0 or > 1024 ||
            theme.IllustratedMenuTrailingPadding is < 0 or > 1024 || theme.IllustratedMenuIconTextGap is < 0 or > 1024 ||
            theme.IllustratedMenuDescriptionGap is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Illustrated menu width/row/icon dimensions must be 1..1024 and padding/gap values 0..1024, or null for existing defaults.");
    }
}
