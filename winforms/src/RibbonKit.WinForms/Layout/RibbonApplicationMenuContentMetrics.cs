namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonApplicationMenuContentDefaults(int LargeIcon, int SmallIcon, int LargeIconInset, int SmallIconInset, int Gap, int LargeTextInset, int TextInset,
    int PaintTrailing, int MeasureTrailing, int RowPadding, int IconPadding, int DescriptionGap, int DescriptionInset, int DescriptionLines, int FooterPadding, int FooterIconPreferred, int FooterPlainPreferred, int CompactArrow, int TouchArrow, int SeparatorHeight, int SeparatorInset, int SeparatorStroke);

// One component-scoped owner for content dimensions; preserve legacy summed rounding at null.
internal readonly record struct RibbonApplicationMenuContentMetrics(RibbonTheme? Theme, int Dpi, RibbonDensity Density)
{
    private RibbonApplicationMenuContentSettings? A => Theme?.ApplicationMenuContent;
    private static RibbonApplicationMenuContentDefaults D => RibbonPresentationResolver.ApplicationMenuContent;
    private int S(int n) => RibbonLayoutEngine.Scale(n, Dpi);
    private int Delta(int? value, int fallback) => value is { } n ? S(n) - S(fallback) : 0;
    internal int LargeIcon => S(A?.LargeIconSize ?? D.LargeIcon);
    internal int SmallIcon => S(A?.SmallIconSize ?? D.SmallIcon);
    internal int IconInset(bool large) => S(large ? A?.LargeIconInset ?? D.LargeIconInset : A?.SmallIconInset ?? D.SmallIconInset);
    internal int Gap => S(A?.IconTextGap ?? D.Gap);
    internal int Trailing(bool measure = false) => S(A?.TextTrailingPadding ?? (measure ? D.MeasureTrailing : D.PaintTrailing));
    internal int RowPadding => S(A?.RowTextVerticalPadding ?? D.RowPadding);
    internal int DescriptionInset => S(A?.DescriptionVerticalInset ?? D.DescriptionInset);
    internal int DescriptionGap => S(A?.DescriptionGap ?? D.DescriptionGap);
    internal int Lines => A?.DescriptionMaxLines ?? D.DescriptionLines;
    internal int FooterPadding => S(A?.FooterHorizontalPadding ?? D.FooterPadding);
    internal int FooterBothPadding => A?.FooterHorizontalPadding is { } n ? 2 * S(n) : S(2 * D.FooterPadding);
    internal int FooterIconSpan => S(D.SmallIcon + D.Gap) + Delta(A?.SmallIconSize, D.SmallIcon) + Delta(A?.IconTextGap, D.Gap);
    internal bool CustomIconGeometry => A?.LargeIconSize is not null || A?.SmallIconSize is not null || A?.LargeIconInset is not null || A?.SmallIconInset is not null || A?.IconTextGap is not null || A?.LargeTextInset is not null;
    internal bool CustomFooterGeometry => A?.SmallIconSize is not null || A?.IconTextGap is not null || A?.FooterHorizontalPadding is not null;
    internal int Arrow => S(Math.Max(A?.ArrowColumnWidth ?? (Density == RibbonDensity.Touch ? D.TouchArrow : D.CompactArrow), Density == RibbonDensity.Touch ? D.TouchArrow : 1));
    internal int ArrowIn(Rectangle bounds) => A?.ArrowColumnWidth is not null ? Math.Min(Arrow, Math.Max(1, bounds.Width / 2)) : Arrow;
    internal int SeparatorHeight => S(A?.SeparatorHeight ?? D.SeparatorHeight);
    internal int TextLeading(bool large, bool hasIcon, bool measure = false)
    {
        if (large)
        {
            int requested = S(A?.LargeTextInset ?? D.LargeTextInset);
            bool authored = A?.LargeTextInset is not null || A?.LargeIconSize is not null || A?.LargeIconInset is not null || A?.IconTextGap is not null;
            return authored && hasIcon ? Math.Max(requested, IconInset(true) + LargeIcon + Gap) : requested;
        }
        if (!hasIcon) return S(A?.TextInset ?? D.TextInset);
        if (measure) return S(D.SmallIconInset + D.SmallIcon + D.Gap) + Delta(A?.SmallIconInset, D.SmallIconInset) + Delta(A?.SmallIconSize, D.SmallIcon) + Delta(A?.IconTextGap, D.Gap);
        return IconInset(false) + SmallIcon + Gap;
    }
    internal int ArtworkHeight(bool large) => S((large ? D.LargeIcon : D.SmallIcon) + D.IconPadding)
        + Delta(large ? A?.LargeIconSize : A?.SmallIconSize, large ? D.LargeIcon : D.SmallIcon) + Delta(A?.IconVerticalPadding, D.IconPadding);
    internal int TextPadding(bool described) => described && A?.DescriptionVerticalInset is not null ? Math.Max(RowPadding, 2 * DescriptionInset) : RowPadding;
    internal int DescriptionHeight(string text, Font font, int width) => text.Length == 0 || Lines == 0 ? 0 : Math.Min(Lines * font.Height,
        TextRenderer.MeasureText(text, font, new(Math.Max(1, width), int.MaxValue), RibbonMenuLayout.DescriptionFlags).Height);
    internal FontStyle RichStyle(FontStyle host) => A?.RichTitleBold == false ? host & ~FontStyle.Bold : host | FontStyle.Bold;
    internal int NavigationWidthPadding => S(D.LargeTextInset + D.MeasureTrailing) + TextLeading(true, true) - S(D.LargeTextInset) + Trailing(true) - S(D.MeasureTrailing);
    internal int FooterPreferredPadding(bool hasIcon) => A?.FooterHorizontalPadding is { } n ? 2 * S(n) + (hasIcon ? FooterIconSpan : 0)
        : S(hasIcon ? D.FooterIconPreferred : D.FooterPlainPreferred) + (hasIcon ? Delta(A?.SmallIconSize, D.SmallIcon) + Delta(A?.IconTextGap, D.Gap) : 0);
    internal Rectangle IconBounds(Rectangle bounds, bool large)
    {
        int side = large ? LargeIcon : SmallIcon, inset = IconInset(large);
        if ((large ? A?.LargeIconSize : A?.SmallIconSize) is not null || (large ? A?.LargeIconInset : A?.SmallIconInset) is not null)
        { inset = Math.Min(inset, Math.Max(0, bounds.Width)); side = Math.Min(side, Math.Max(0, Math.Min(bounds.Width - inset, bounds.Height))); }
        return new(bounds.Left + inset, bounds.Top + (bounds.Height - side) / 2, side, side);
    }
    internal int ClampedLeading(Rectangle bounds, bool large, bool hasIcon)
    {
        int inset = TextLeading(large, hasIcon);
        bool authored = large ? A?.LargeTextInset is not null || A?.LargeIconSize is not null || A?.LargeIconInset is not null || A?.IconTextGap is not null
            : A?.TextInset is not null || A?.SmallIconSize is not null || A?.SmallIconInset is not null || A?.IconTextGap is not null;
        return authored ? Math.Min(inset, Math.Max(0, bounds.Width)) : inset;
    }
    internal Rectangle SeparatorBounds(Rectangle bounds)
    {
        int inset = A?.SeparatorHorizontalPadding is { } n ? Math.Min(S(n), Math.Max(0, (bounds.Width - 1) / 2)) : S(D.SeparatorInset);
        int stroke = Math.Min(A?.SeparatorThickness is { } w ? S(w) : D.SeparatorStroke, Math.Max(0, bounds.Height));
        return new(bounds.Left + inset, bounds.Top + (bounds.Height - stroke) / 2, Math.Max(0, bounds.Width - 2 * inset), stroke);
    }
    internal Color SeparatorInk => A is { SeparatorColor.IsEmpty: false } a ? a.SeparatorColor : Theme?.Border ?? Color.Transparent;
}
