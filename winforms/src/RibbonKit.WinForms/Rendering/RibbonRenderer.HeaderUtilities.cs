namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    private void DrawHeaderUtilityState(RibbonPaintContext c, RibbonFill fallback)
    {
        var a = c.Theme.HeaderAppearance;
        DrawChrome(c with { IsHeaderUtilityState = true }, (c.Pressed ? a?.UtilityPressedBackground : a?.UtilityHoverBackground) ?? fallback, Color.Transparent);
    }
    private bool TryPaintHeaderUtility(RibbonPaintContext c, RibbonFill fill, Color border, int radius)
    {
        if (!c.IsHeaderUtilityState || c.Theme.HeaderAppearance?.UtilityStateChrome is not { HasOverrides: true } chrome) return false;
        border = chrome.Border(c, border);
        PaintStateChrome(c with { Bounds = Rectangle.Inflate(c.Bounds, -1, -1) }, radius, fill, border, border.A == 0 ? 0 : 1, chrome);
        return true;
    }
}
