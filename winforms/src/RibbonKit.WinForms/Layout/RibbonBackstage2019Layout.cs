namespace RibbonKit.WinForms.Layout;

// Classic owns its defaults; changing Modern's geometry does not change them.
internal sealed class RibbonBackstage2019Layout : RibbonBackstageLayoutProvider
{
    internal override RibbonBackstageLayoutDefaults Defaults { get; } = new(
        CompactRowHeight: 42, TouchRowHeight: 52, NavigationPadding: 0,
        CompactBackSize: 36, TouchBackSize: 44, BackInset: 20, BackGap: 24,
        IconTextGap: 12, IconInset: 26, IconSize: 16, AutoMinimumWidth: 220,
        FooterBottomPadding: 14, PagePadding: 0, RowTextVerticalPadding: 16,
        SeparatorHeight: 9, MinimumPageWidth: 240, TrailingPadding: 8, ScrollbarWidth: 18);
}
