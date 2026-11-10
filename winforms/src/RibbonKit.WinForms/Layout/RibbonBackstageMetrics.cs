namespace RibbonKit.WinForms.Layout;

internal readonly record struct RibbonBackstageMetrics(RibbonTheme? Theme, RibbonDensity Density)
{
    internal int RowHeight => Math.Max(Theme?.BackstageMinimumRowHeight ?? (Density == RibbonDensity.Touch ? 52 : 42), Density == RibbonDensity.Touch ? 52 : 1);
    internal int NavigationPadding => Theme?.BackstageNavigationPadding ?? 8;
    internal int BackSize => Math.Max(Theme?.BackstageBackButtonSize ?? (Density == RibbonDensity.Touch ? 44 : 36), Density == RibbonDensity.Touch ? 44 : 1);
    internal int BackInset => Theme?.BackstageBackButtonInset ?? 20;
    internal int BackGap => Theme?.BackstageBackButtonBottomGap ?? 24;
    internal int IconTextGap => Theme?.BackstageIconTextGap ?? 12;
    internal int IconInset => Theme?.BackstageNavigationIconInset ?? 16;
    internal int IconSize => Theme?.BackstageNavigationIconSize ?? 16;
    internal int TextOffset => IconInset + IconSize + IconTextGap;
    internal int AutoMinimumWidth => Theme?.BackstageAutoMinimumNavigationWidth ?? 200;
    internal int FooterBottomPadding => Theme?.BackstageFooterBottomPadding ?? NavigationPadding;
    internal int AutoWidthPadding => 2 * NavigationPadding + TextOffset + 8 + 18;
    internal int PagePadding => Theme?.BackstagePagePadding ?? 0;
    internal static void Validate(RibbonTheme theme)
    {
        if (theme.BackstageTitleSlideDistanceRatio is { } titleRatio && (!double.IsFinite(titleRatio) || titleRatio < 0 || titleRatio > 1))
            throw new ArgumentOutOfRangeException(nameof(theme), "Backstage title slide distance must be a finite fraction from 0 to 1, or null for the final 25 percent.");
        if (theme.BackstageSlideDistanceRatio is { } ratio && (!double.IsFinite(ratio) || ratio < 0 || ratio > 1))
            throw new ArgumentOutOfRangeException(nameof(theme), "Backstage slide distance must be a finite fraction from 0 to 1, or null for the default travel.");
        int?[] spacing = [theme.BackstageNavigationPadding, theme.BackstageBackButtonInset, theme.BackstageBackButtonBottomGap, theme.BackstageIconTextGap, theme.BackstagePagePadding,
            theme.BackstageNavigationIconInset, theme.BackstageFooterBottomPadding];
        if (spacing.Any(n => n is < 0 or > 1024) || theme.BackstageMinimumRowHeight is < 1 or > 1024 || theme.BackstageBackButtonSize is < 1 or > 1024
            || theme.BackstageNavigationIconSize is < 1 or > 1024 || theme.BackstageAutoMinimumNavigationWidth is < 1 or > 1024 || theme.BackstagePageBorderWidth is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(theme), "Backstage spacing must be 0..1024, row/button sizes 1..1024, or null for existing geometry.");
    }
}
