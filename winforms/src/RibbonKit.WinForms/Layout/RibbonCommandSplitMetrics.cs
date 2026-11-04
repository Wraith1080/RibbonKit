namespace RibbonKit.WinForms.Layout;

// Real horizontal command splits share their preferred width and target sizing.
// Menu/QAT projection and Large vertical splits retain their own geometry.
internal readonly record struct RibbonCommandSplitMetrics(RibbonDensity Density, RibbonTheme? Theme)
{
    internal static RibbonCommandSplitMetrics For(RibbonDensity density, RibbonTheme? theme = null) => new(density, theme);
    private RibbonDensityMetrics Defaults => RibbonDensityMetrics.For(Density);
    internal int Arrow => Math.Max(Theme?.CommandSplitArrowWidth ?? Defaults.HorizontalSplitArrow, Defaults.Touch ? 44 : 1);
    internal int PrimaryMinimum(bool large) => Math.Max(large ? Theme?.CommandLargeSplitPrimaryMinimumWidth ?? Defaults.LargeHorizontalSplitPrimaryMinimum
        : Theme?.CommandSplitPrimaryMinimumWidth ?? Defaults.HorizontalSplitPrimaryMinimum, Defaults.Touch ? large ? 72 : 56 : 0);
    private bool HasOverride(bool large) => Theme?.CommandSplitArrowWidth is not null ||
        (large ? Theme?.CommandLargeSplitPrimaryMinimumWidth is not null : Theme?.CommandSplitPrimaryMinimumWidth is not null);
    private int ClampArrow(int width, int desired) => Math.Min(Math.Max(0, width),
        Math.Min(desired, Math.Max(1, Defaults.Touch ? (width - 1) / 2 : width / 3)));
    internal int ArrowWidth(int width, int dpi) => ClampArrow(width, RibbonLayoutEngine.Scale(Arrow, dpi));

    internal int MeasureWidth(int baseWidth, int dpi, bool large, bool simplified = false, int contentExtra = 0)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        int legacyArrow = S(Defaults.HorizontalSplitArrow);
        int legacy = large ? Defaults.Touch ? Math.Max(S(Defaults.LargeHorizontalSplitPrimaryMinimum) + legacyArrow,
            baseWidth + legacyArrow - S(Defaults.SplitArrow)) : baseWidth
            : (simplified ? baseWidth : Math.Max(baseWidth, S(Defaults.HorizontalSplitPrimaryMinimum))) + legacyArrow;
        if (!HasOverride(large) && contentExtra == 0) return legacy;
        int arrow = S(Arrow);
        int primary = Math.Max(S(PrimaryMinimum(large)), legacy - ClampArrow(legacy, legacyArrow) + contentExtra);
        // Preserve the existing half/third cap while reserving the authored slot
        // at preferred size. Only externally constrained rows need to clip it.
        return Math.Max(primary + arrow, Defaults.Touch ? 2 * arrow + 1 : 3 * arrow);
    }

    internal static void Validate(RibbonTheme theme)
    {
        if (theme.CommandSplitArrowWidth is < 1 or > 1024 || theme.CommandSplitPrimaryMinimumWidth is < 0 or > 1024 ||
            theme.CommandLargeSplitPrimaryMinimumWidth is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Command split arrow width must be 1..1024 and primary minima 0..1024, or null for existing density/layout defaults.");
    }
}
