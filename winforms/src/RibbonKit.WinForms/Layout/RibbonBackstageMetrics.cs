namespace RibbonKit.WinForms.Layout;

internal readonly record struct RibbonBackstageMetrics
{
    internal RibbonBackstageMetrics(RibbonTheme? theme, RibbonDensity density, RibbonBackstageLayoutDefaults defaults)
    {
        bool touch = density == RibbonDensity.Touch;
        RowHeight = Math.Max(theme?.BackstageMinimumRowHeight ?? (touch ? defaults.TouchRowHeight : defaults.CompactRowHeight), touch ? defaults.TouchRowHeight : 1);
        NavigationPadding = theme?.BackstageNavigationPadding ?? defaults.NavigationPadding;
        BackSize = Math.Max(theme?.BackstageBackButtonSize ?? (touch ? defaults.TouchBackSize : defaults.CompactBackSize), touch ? defaults.TouchBackSize : 1);
        BackInset = theme?.BackstageBackButtonInset ?? defaults.BackInset;
        BackGap = theme?.BackstageBackButtonBottomGap ?? defaults.BackGap;
        IconTextGap = theme?.BackstageIconTextGap ?? defaults.IconTextGap;
        IconInset = theme?.BackstageNavigationIconInset ?? defaults.IconInset;
        IconSize = theme?.BackstageNavigationIconSize ?? defaults.IconSize;
        AutoMinimumWidth = theme?.BackstageAutoMinimumNavigationWidth ?? defaults.AutoMinimumWidth;
        FooterBottomPadding = theme?.BackstageFooterBottomPadding ?? defaults.FooterBottomPadding ?? NavigationPadding;
        FooterUsesNavigationInset = theme?.BackstageFooterBottomPadding is null && defaults.FooterBottomPadding is null;
        PagePadding = theme?.BackstagePagePadding ?? defaults.PagePadding;
        RowTextVerticalPadding = theme?.BackstageRowTextVerticalPadding ?? defaults.RowTextVerticalPadding;
        SeparatorHeight = theme?.BackstageSeparatorHeight ?? defaults.SeparatorHeight;
        MinimumPageWidth = defaults.MinimumPageWidth;
        TrailingPadding = theme?.BackstageTrailingPadding ?? defaults.TrailingPadding;
        AutoWidthPadding = 2 * NavigationPadding + TextOffset + TrailingPadding + defaults.ScrollbarWidth;
    }
    internal int RowHeight { get; }
    internal int NavigationPadding { get; }
    internal int BackSize { get; }
    internal int BackInset { get; }
    internal int BackGap { get; }
    internal int IconTextGap { get; }
    internal int IconInset { get; }
    internal int IconSize { get; }
    internal int TextOffset => IconInset + IconSize + IconTextGap;
    internal int AutoMinimumWidth { get; }
    internal int FooterBottomPadding { get; }
    internal bool FooterUsesNavigationInset { get; }
    internal int AutoWidthPadding { get; }
    internal int PagePadding { get; }
    internal int RowTextVerticalPadding { get; }
    internal int SeparatorHeight { get; }
    internal int MinimumPageWidth { get; }
    internal int TrailingPadding { get; }
    internal static void Validate(RibbonTheme theme)
    {
        if (!Enum.IsDefined(theme.BackstageStyle)) throw new ArgumentOutOfRangeException(nameof(theme));
        RibbonBackstageMotionSettings.Validate(theme.BackstageMotion);
        RibbonBackstageAppearanceSettings.Validate(theme.BackstageAppearance);
        if (theme.BackstageTitleSlideDistanceRatio is { } titleRatio && (!double.IsFinite(titleRatio) || titleRatio < 0 || titleRatio > 1))
            throw new ArgumentOutOfRangeException(nameof(theme), "Backstage title slide distance must be a finite fraction from 0 to 1, or null for the final 25 percent.");
        if (theme.BackstageSlideDistanceRatio is { } ratio && (!double.IsFinite(ratio) || ratio < 0 || ratio > 1))
            throw new ArgumentOutOfRangeException(nameof(theme), "Backstage slide distance must be a finite fraction from 0 to 1, or null for the default travel.");
        int?[] spacing = [theme.BackstageNavigationPadding, theme.BackstageBackButtonInset, theme.BackstageBackButtonBottomGap, theme.BackstageIconTextGap, theme.BackstagePagePadding,
            theme.BackstageNavigationIconInset, theme.BackstageFooterBottomPadding, theme.BackstageRowTextVerticalPadding, theme.BackstageSeparatorHeight, theme.BackstageTrailingPadding];
        if (spacing.Any(n => n is < 0 or > 1024) || theme.BackstageMinimumRowHeight is < 1 or > 1024 || theme.BackstageBackButtonSize is < 1 or > 1024
            || theme.BackstageNavigationIconSize is < 1 or > 1024 || theme.BackstageAutoMinimumNavigationWidth is < 1 or > 1024 || theme.BackstagePageBorderWidth is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(theme), "Backstage spacing must be 0..1024, row/button sizes 1..1024, or null for existing geometry.");
    }
}
