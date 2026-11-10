namespace RibbonKit.WinForms.Layout;

internal sealed class RibbonBackstage2024Layout : RibbonBackstageLayoutProvider
{
    internal override RibbonBackstageLayoutDefaults Defaults { get; } = new(
        CompactRowHeight: 42, TouchRowHeight: 52, NavigationPadding: 8,
        CompactBackSize: 36, TouchBackSize: 44, BackInset: 20, BackGap: 24,
        IconTextGap: 12, IconInset: 16, IconSize: 16, AutoMinimumWidth: 200,
        FooterBottomPadding: null, PagePadding: 0, RowTextVerticalPadding: 16,
        SeparatorHeight: 9, MinimumPageWidth: 240, TrailingPadding: 8, ScrollbarWidth: 18);
}
