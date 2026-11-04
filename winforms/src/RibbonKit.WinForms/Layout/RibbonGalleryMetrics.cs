namespace RibbonKit.WinForms.Layout;

// Library spacing only; authored tile sizes and presenter card internals keep their owners.
internal readonly record struct RibbonGalleryMetrics(RibbonTheme? Theme, RibbonDensity Density)
{
    internal int ContentPadding => Theme?.GalleryContentPadding ?? 4;
    internal int UtilityWidth => Math.Max(Theme?.GalleryUtilityWidth ?? (Density == RibbonDensity.Touch ? 32 : 18), Density == RibbonDensity.Touch ? 32 : 1);
    internal int TilePadding => Theme?.GalleryTilePadding ?? 4;
    internal int HeadingHorizontalPadding => Theme?.GalleryHeadingHorizontalPadding ?? 6;
    internal int HeadingVerticalPadding => Theme?.GalleryHeadingVerticalPadding ?? 6;
    internal static void Validate(RibbonTheme theme)
    {
        int?[] spacing = [theme.GalleryContentPadding, theme.GalleryTilePadding, theme.GalleryHeadingHorizontalPadding, theme.GalleryHeadingVerticalPadding];
        if (spacing.Any(n => n is < 0 or > 1024) || theme.GalleryUtilityWidth is < 1 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Gallery spacing must be 0..1024, utility width 1..1024, or null for existing geometry.");
    }
    internal Rectangle HeadingTextBounds(Rectangle bounds, int dpi)
    {
        int inset = Math.Min(RibbonLayoutEngine.Scale(HeadingHorizontalPadding, dpi), bounds.Width / 2);
        return Rectangle.FromLTRB(bounds.Left + inset, bounds.Top, bounds.Right - inset, bounds.Bottom);
    }
}
