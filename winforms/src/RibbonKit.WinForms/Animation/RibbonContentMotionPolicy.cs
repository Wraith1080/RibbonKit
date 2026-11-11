using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Scene-captured theme overrides over the existing level-dependent defaults.
internal readonly record struct RibbonContentMotionPolicy(int? SlideDistance, double IndicatorSpringAmount)
{
    internal static RibbonContentMotionPolicy Resolve(RibbonTheme theme) => new(theme.TabContentSlideDistance, theme.HeaderIndicatorSpringAmount ?? .35);
    internal int DeviceSlideDistance(RibbonAnimationLevel level, int dpi) => RibbonLayoutEngine.Scale(SlideDistance ?? (level == RibbonAnimationLevel.Expressive ? 12 : 8), dpi);
}
