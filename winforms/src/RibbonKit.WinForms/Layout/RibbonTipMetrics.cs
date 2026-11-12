namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonTipDefaults(int MaximumWidth, int ContentPadding, int TitleGap, int PlacementGap, int BackstageHorizontalAllowance, int BackstageVerticalAllowance, int BackstageRightClearance, int KeyTipStroke, int OfficeKeyTipRadius);

internal readonly record struct RibbonTipMetrics(RibbonTheme? Theme)
{
    private RibbonTipAppearanceSettings? A => Theme?.TipAppearance;
    private static RibbonTipDefaults D => RibbonPresentationResolver.Tips;
    internal int MaximumWidth => Theme?.ScreenTipMaximumWidth ?? D.MaximumWidth;
    internal int ContentPadding => Theme?.ScreenTipContentPadding ?? D.ContentPadding;
    internal int TitleGap => Theme?.ScreenTipTitleGap ?? D.TitleGap;
    internal int PlacementGap => Theme?.ScreenTipPlacementGap ?? D.PlacementGap;
    internal FontStyle TitleStyle(Font font) => A?.ScreenTipTitleBold == false ? font.Style & ~FontStyle.Bold : font.Style | FontStyle.Bold;
    internal Rectangle KeyTipPaintBounds(Rectangle bounds, int dpi)
    {
        int inset = Math.Min(RibbonLayoutEngine.Scale(A?.KeyTipInset ?? 0, dpi), Math.Max(0, (Math.Min(bounds.Width, bounds.Height) - 1) / 2));
        return Rectangle.Inflate(bounds, -inset, -inset);
    }
    internal int KeyTipStroke(Rectangle bounds, int dpi) => A?.KeyTipBorderWidth is { } n
        ? Math.Min(RibbonLayoutEngine.Scale(n, dpi), Math.Max(0, Math.Min(bounds.Width, bounds.Height) / 2)) : D.KeyTipStroke;
    internal Rectangle BackstageBadgeBounds(Rectangle bounds, Size text, int dpi)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        var desired = BadgeSize(text, dpi, backstage: true);
        var size = new Size(Math.Min(Math.Max(0, bounds.Width - S(A?.BackstageKeyTipHorizontalClearance ?? D.BackstageHorizontalAllowance)), desired.Width),
            Math.Min(Math.Max(0, bounds.Height - S(A?.BackstageKeyTipVerticalClearance ?? D.BackstageVerticalAllowance)), desired.Height));
        return new(bounds.Left + Math.Max(0, bounds.Width - S(A?.BackstageKeyTipRightClearance ?? D.BackstageRightClearance) - size.Width), bounds.Top + (bounds.Height - size.Height) / 2, size.Width, size.Height);
    }
    internal bool HasBadgeOverride => Theme?.KeyTipHorizontalPadding is not null || Theme?.KeyTipVerticalPadding is not null;
    internal Size BadgeSize(Size text, int dpi, bool backstage = false)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        // Preserve legacy summed fractional-DPI rounding at null.
        int x = Theme?.KeyTipHorizontalPadding is { } h ? 2 * S(h) : backstage ? S(D.BackstageHorizontalAllowance) : 0;
        int y = Theme?.KeyTipVerticalPadding is { } v ? 2 * S(v) : backstage ? S(D.BackstageVerticalAllowance) : 0;
        return new(text.Width + x, text.Height + y);
    }
    internal static void Validate(RibbonTheme theme)
    {
        RibbonTipAppearanceSettings.Validate(theme.TipAppearance);
        int?[] spacing = [theme.ScreenTipContentPadding, theme.ScreenTipTitleGap, theme.ScreenTipPlacementGap, theme.KeyTipHorizontalPadding, theme.KeyTipVerticalPadding];
        if (spacing.Any(n => n is < 0 or > 1024) || theme.ScreenTipMaximumWidth is < 1 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Tip spacing must be 0..1024, maximum width 1..1024, or null for existing geometry.");
    }
}
