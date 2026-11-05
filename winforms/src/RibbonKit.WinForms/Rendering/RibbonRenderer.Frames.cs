using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    /// <summary>Gets the body, joined QAT/message and standalone panel corner radius in device pixels. The neutral renderer returns zero. Office resolves BodyCornerRadius, or its legacy doubled CornerRadius at null. Measurement must not mutate ribbon state.</summary>
    public virtual int GetBodyCornerRadius(RibbonTheme theme, int deviceDpi) => 0;
    /// <summary>Gets required body-shadow clearance in device pixels. Neutral returns empty. Explicit body shadow overrides reserve at least this space without mutating authored BodyMargin; layout clamps each side to 128 logical pixels. Null shadow roles preserve legacy margins. Measurement must not mutate ribbon state.</summary>
    public virtual Padding GetBodyShadowPadding(RibbonTheme theme, int deviceDpi) => Padding.Empty;
    /// <summary>Gets floating popup shadow/frame clearance on each side in device pixels. Both null shadow roles retain eight logical pixels with legacy DPI rounding; explicit roles resolve scaled blur plus absolute scaled offset, with at least one device pixel for the frame. Native chrome, tip placement and popup motion share it, clamped to 128 logical pixels. Custom shadow painters should reserve their complete footprint here without mutating ribbon state.</summary>
    public virtual int GetPopupShadowPadding(RibbonTheme theme, int deviceDpi)
    {
        if (theme.PopupShadowBlur is null && theme.PopupShadowVerticalOffset is null)
            return Math.Max(1, RibbonLayoutEngine.Scale(8, deviceDpi));
        var metrics = new RibbonFrameMetrics(theme);
        return Math.Max(1, RibbonLayoutEngine.Scale(metrics.PopupBlur, deviceDpi) + Math.Abs(RibbonLayoutEngine.Scale(metrics.PopupOffset, deviceDpi)));
    }
}
