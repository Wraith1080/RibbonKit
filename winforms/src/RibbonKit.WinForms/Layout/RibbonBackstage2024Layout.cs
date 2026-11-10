namespace RibbonKit.WinForms.Layout;

internal sealed class RibbonBackstage2024Layout : RibbonBackstageLayoutProvider
{
    internal override int NavigationPadding => 8;
    internal override int IconInset => 16;
    internal override int AutoMinimumWidth => 200;
    internal override int FooterBottomPadding(int navigationPadding) => navigationPadding;
}
