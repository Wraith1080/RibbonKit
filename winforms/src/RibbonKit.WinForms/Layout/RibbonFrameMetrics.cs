namespace RibbonKit.WinForms.Layout;

internal readonly record struct RibbonFrameMetrics(RibbonTheme Theme)
{
    internal int BodyBlur => Theme.BodyShadowBlur ?? 6;
    internal int BodyOffset => Theme.BodyShadowVerticalOffset ?? 2;
    internal int PopupBlur => Theme.PopupShadowBlur ?? 6;
    internal int PopupOffset => Theme.PopupShadowVerticalOffset ?? 2;
    internal bool HasBodyShadowOverride => Theme.BodyShadowBlur is not null || Theme.BodyShadowVerticalOffset is not null;
    internal static int Radius(int? logical, int legacyDevicePixels, int dpi) => logical is { } value ? RibbonLayoutEngine.Scale(value, dpi) : legacyDevicePixels;
    internal static int PopupPadding(RibbonRenderer renderer, RibbonTheme theme, int dpi) =>
        Math.Clamp(renderer.GetPopupShadowPadding(theme, dpi), 0, RibbonLayoutEngine.Scale(128, dpi));
    internal Padding LayoutMargin(RibbonRenderer renderer, int dpi)
    {
        if (!HasBodyShadowOverride) return Theme.BodyMargin;
        var padding = renderer.GetBodyShadowPadding(Theme, dpi);
        int Logical(int value) => (int)Math.Ceiling(Math.Clamp(value, 0, RibbonLayoutEngine.Scale(128, dpi)) * 96d / dpi);
        return new(Math.Max(Theme.BodyMargin.Left, Logical(padding.Left)), Math.Max(Theme.BodyMargin.Top, Logical(padding.Top)),
            Math.Max(Theme.BodyMargin.Right, Logical(padding.Right)), Math.Max(Theme.BodyMargin.Bottom, Logical(padding.Bottom)));
    }
    internal static void Validate(RibbonTheme theme)
    {
        if (theme.BodyCornerRadius is < 0 or > 64 || theme.BackstagePageCornerRadius is < 0 or > 64 ||
            theme.OptionsButtonCornerRadius is < 0 or > 64 ||
            theme.FocusCornerRadius is < 0 or > 64 || theme.KeyTipCornerRadius is < 0 or > 64 ||
            theme.CheckBoxCornerRadius is < 0 or > 64 || theme.BackstageBackButtonCornerRadius is < 0 or > 64 ||
            theme.NavigationMarkerCornerRadius is < 0 or > 64 || theme.ApplicationMenuFooterCornerRadius is < 0 or > 64 ||
            theme.ApplicationMenuFileButtonCornerRadius is < 0 or > 64 || theme.HeaderIndicatorCornerRadius is < 0 or > 64 ||
            theme.BodyShadowBlur is < 0 or > 32 || theme.PopupShadowBlur is < 0 or > 32 ||
            theme.BodyShadowVerticalOffset is < -32 or > 32 || theme.PopupShadowVerticalOffset is < -32 or > 32)
            throw new ArgumentOutOfRangeException(nameof(theme), "Frame radii must be 0..64, shadow blur 0..32, vertical offsets -32..32, or null for existing geometry.");
    }
}
