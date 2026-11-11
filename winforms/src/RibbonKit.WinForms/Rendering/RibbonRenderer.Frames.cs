using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    /// <summary>Gets the body, joined QAT/message and standalone panel corner radius in device pixels. The neutral renderer returns zero. Office resolves BodyCornerRadius, or its legacy doubled CornerRadius at null. Measurement must not mutate ribbon state.</summary>
    public virtual int GetBodyCornerRadius(RibbonTheme theme, int deviceDpi) => 0;
    /// <summary>Gets required body-shadow clearance in device pixels. Neutral returns empty. Explicit body shadow overrides reserve at least this space without mutating authored BodyMargin; layout clamps each side to 128 logical pixels. Null shadow roles preserve legacy margins. Measurement must not mutate ribbon state.</summary>
    public virtual Padding GetBodyShadowPadding(RibbonTheme theme, int deviceDpi) => Padding.Empty;
    /// <summary>Gets floating popup shadow/frame clearance on each side in device pixels. Null blur/offset roles retain eight logical pixels with legacy DPI rounding; authored offsets reserve scaled blur plus the larger absolute offset, with at least one device pixel. Native chrome, tip placement and popup motion share it, clamped to 128 logical pixels. Custom painters should reserve their complete footprint without mutating ribbon state.</summary>
    public virtual int GetPopupShadowPadding(RibbonTheme theme, int deviceDpi) =>
        new RibbonPopupFrameMetrics(theme, deviceDpi, RibbonPresentationResolver.PopupFrame).ShadowPadding;
}
