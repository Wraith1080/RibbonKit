namespace RibbonKit.WinForms.Layout;

internal readonly record struct RibbonTipMetrics(RibbonTheme? Theme)
{
    internal int MaximumWidth => Theme?.ScreenTipMaximumWidth ?? 320;
    internal int ContentPadding => Theme?.ScreenTipContentPadding ?? 12;
    internal int TitleGap => Theme?.ScreenTipTitleGap ?? 6;
    internal int PlacementGap => Theme?.ScreenTipPlacementGap ?? 8;
    internal bool HasBadgeOverride => Theme?.KeyTipHorizontalPadding is not null || Theme?.KeyTipVerticalPadding is not null;
    internal Size BadgeSize(Size text, int dpi, bool backstage = false)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        // Preserve legacy summed fractional-DPI rounding at null.
        int x = Theme?.KeyTipHorizontalPadding is { } h ? 2 * S(h) : backstage ? S(8) : 0;
        int y = Theme?.KeyTipVerticalPadding is { } v ? 2 * S(v) : backstage ? S(4) : 0;
        return new(text.Width + x, text.Height + y);
    }
    internal static void Validate(RibbonTheme theme)
    {
        int?[] spacing = [theme.ScreenTipContentPadding, theme.ScreenTipTitleGap, theme.ScreenTipPlacementGap, theme.KeyTipHorizontalPadding, theme.KeyTipVerticalPadding];
        if (spacing.Any(n => n is < 0 or > 1024) || theme.ScreenTipMaximumWidth is < 1 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Tip spacing must be 0..1024, maximum width 1..1024, or null for existing geometry.");
    }
}
