namespace RibbonKit.WinForms.Layout;

internal readonly record struct RibbonCaptionMetrics(RibbonTheme? Theme)
{
    internal int Leading(bool icon) => Theme?.CaptionLeadingPadding ?? (icon ? 10 : 8);
    internal int IconGap => Theme?.CaptionIconTextGap ?? 10;
    internal int QuickAccessGap => Theme?.CaptionQuickAccessTextGap ?? 10;
    internal int TitleReservation => Theme?.CaptionTitleReservationWidth ?? 100;
    internal static void Validate(RibbonTheme theme)
    {
        int?[] spacing = [theme.CaptionLeadingPadding, theme.CaptionIconTextGap, theme.CaptionQuickAccessTextGap];
        if (spacing.Any(n => n is < 0 or > 1024) || theme.CaptionTitleReservationWidth is < 1 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Caption spacing must be 0..1024, title reservation 1..1024, or null for existing geometry.");
    }
}
