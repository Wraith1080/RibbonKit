namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonPopupFrameDefaults(int BorderWidth, int Blur, int HorizontalOffset, int VerticalOffset, double Falloff, int LegacyPadding);

// Shared frame/footprint values; the actual renderer retains all measurement hooks.
internal readonly record struct RibbonPopupFrameMetrics(RibbonTheme Theme, int Dpi, RibbonPopupFrameDefaults Defaults)
{
    internal int BorderWidth => Theme.PopupAppearance?.BorderWidth is { } width ? RibbonLayoutEngine.Scale(width, Dpi) : Defaults.BorderWidth;
    internal int Blur => RibbonLayoutEngine.Scale(Theme.PopupShadowBlur ?? Defaults.Blur, Dpi);
    internal int HorizontalOffset => RibbonLayoutEngine.Scale(Theme.PopupAppearance?.ShadowHorizontalOffset ?? Defaults.HorizontalOffset, Dpi);
    internal int VerticalOffset => RibbonLayoutEngine.Scale(Theme.PopupShadowVerticalOffset ?? Defaults.VerticalOffset, Dpi);
    internal double Falloff => Theme.PopupAppearance?.ShadowFalloff ?? Defaults.Falloff;
    internal int ShadowPadding => Theme.PopupShadowBlur is null && Theme.PopupShadowVerticalOffset is null && Theme.PopupAppearance?.ShadowHorizontalOffset is null
        ? Math.Max(1, RibbonLayoutEngine.Scale(Defaults.LegacyPadding, Dpi)) : Math.Max(1, Blur + Math.Max(Math.Abs(HorizontalOffset), Math.Abs(VerticalOffset)));
    internal static int PaintedBorder(RibbonRenderer renderer, RibbonTheme theme, int dpi) => Math.Clamp(renderer.GetPopupBorderWidth(theme, dpi), 0, RibbonLayoutEngine.Scale(128, dpi));
    internal static int ContentInset(RibbonRenderer renderer, RibbonTheme theme, int dpi, Size? available = null)
    {
        int inset = Math.Max(1, PaintedBorder(renderer, theme, dpi));
        return available is { } size ? Math.Min(inset, Math.Max(1, (Math.Min(size.Width, size.Height) - 1) / 2)) : inset;
    }
}
