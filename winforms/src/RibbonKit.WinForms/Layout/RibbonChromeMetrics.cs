namespace RibbonKit.WinForms.Layout;

// Spacing only. Density target sizes, device-pixel joins and native caption
// geometry keep their existing owners. No mutable/global coordination.
internal readonly record struct RibbonChromeMetrics(RibbonTheme Theme, RibbonDensity Density)
{
    internal int GroupPadding(bool collapsed = false) => Theme.GroupHorizontalPadding ?? (collapsed ? RibbonLayoutEngine.CollapsedGroupSideInset : RibbonLayoutEngine.GroupSideInset);
    internal int ColumnGap(bool simplified = false) => Theme.GroupColumnGap ?? (simplified ? RibbonDensityMetrics.For(Density).SimplifiedGap : RibbonDensityMetrics.For(Density).ColumnGap);
    internal int BodyInset(bool simplified = false) => Theme.BodyContentHorizontalPadding ?? (simplified ? 4 : 2);
    internal int SeparatorInset(bool simplified = false) => Theme.GroupSeparatorVerticalInset ?? (simplified ? 6 : 12);
    internal int TabPadding => Theme.TabHorizontalPadding ?? 14;
    internal int TabMinimum => Math.Max(Theme.TabMinimumWidth ?? 58, Density == RibbonDensity.Touch ? 44 : 1);
    internal int IndicatorPadding => Theme.HeaderIndicatorPadding ?? 4;
    internal int QuickAccessPadding => Theme.QuickAccessHorizontalPadding ?? 2;
    internal int QuickAccessGap => Theme.QuickAccessItemGap ?? 0;
    internal int ActionGap => Theme.MessageActionGap ?? 10;
    internal int TitleGap => Theme.MessageTitleTextGap ?? 8;
    internal static void Validate(RibbonTheme theme)
    {
        int?[] spacing = [theme.GroupHorizontalPadding, theme.GroupColumnGap, theme.BodyContentHorizontalPadding, theme.GroupSeparatorVerticalInset,
            theme.TabHorizontalPadding, theme.HeaderIndicatorPadding, theme.QuickAccessHorizontalPadding, theme.QuickAccessItemGap, theme.MessageActionGap, theme.MessageTitleTextGap];
        if (spacing.Any(n => n is < 0 or > 1024) || theme.TabMinimumWidth is < 1 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Chrome spacing must be 0..1024, tab minimum 1..1024, or null for existing geometry.");
    }
}
