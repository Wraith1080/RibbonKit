namespace RibbonKit.WinForms.Layout;

// Logical presentation fallbacks. Each family supplies every value explicitly;
// the shared measurement/arrangement code carries no Office defaults.
internal sealed record RibbonBackstageLayoutDefaults(
    int CompactRowHeight, int TouchRowHeight, int NavigationPadding,
    int CompactBackSize, int TouchBackSize, int BackInset, int BackGap,
    int IconTextGap, int IconInset, int IconSize, int AutoMinimumWidth,
    int? FooterBottomPadding, int PagePadding, int RowTextVerticalPadding,
    int SeparatorHeight, int MinimumPageWidth, int TrailingPadding, int ScrollbarWidth,
    bool BackButtonVisible = true, int TopSeamHeight = 0, int PageDividerShadowWidth = 0);
