namespace RibbonKit.WinForms.Layout;

internal readonly record struct RibbonBackstageMetrics(RibbonTheme? Theme, RibbonDensity Density)
{
    internal int RowHeight => Math.Max(Theme?.BackstageMinimumRowHeight ?? (Density == RibbonDensity.Touch ? 52 : 42), Density == RibbonDensity.Touch ? 52 : 1);
    internal int NavigationPadding => Theme?.BackstageNavigationPadding ?? 8;
    internal int BackSize => Math.Max(Theme?.BackstageBackButtonSize ?? (Density == RibbonDensity.Touch ? 44 : 36), Density == RibbonDensity.Touch ? 44 : 1);
    internal int BackInset => Theme?.BackstageBackButtonInset ?? 20;
    internal int BackGap => Theme?.BackstageBackButtonBottomGap ?? 24;
    internal int IconTextGap => Theme?.BackstageIconTextGap ?? 12;
    internal int TextOffset => 32 + IconTextGap;
    internal int AutoWidthPadding => 2 * NavigationPadding + TextOffset + 8 + 18;
    internal int PagePadding => Theme?.BackstagePagePadding ?? 0;
    internal static void Validate(RibbonTheme theme)
    {
        int?[] spacing = [theme.BackstageNavigationPadding, theme.BackstageBackButtonInset, theme.BackstageBackButtonBottomGap, theme.BackstageIconTextGap, theme.BackstagePagePadding];
        if (spacing.Any(n => n is < 0 or > 1024) || theme.BackstageMinimumRowHeight is < 1 or > 1024 || theme.BackstageBackButtonSize is < 1 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Backstage spacing must be 0..1024, row/button sizes 1..1024, or null for existing geometry.");
    }
}
