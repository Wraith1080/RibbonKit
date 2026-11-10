namespace RibbonKit.WinForms.Layout;

// Classic owns its defaults; changing Modern's geometry does not change them.
internal sealed class RibbonBackstage2019Layout : RibbonBackstageLayoutProvider
{
    internal override int NavigationPadding => 0;
    internal override int IconInset => 26;
    internal override int AutoMinimumWidth => 220;
    internal override int FooterBottomPadding(int navigationPadding) => 14;
    internal override bool SeparateFooterClearance => true;
}
