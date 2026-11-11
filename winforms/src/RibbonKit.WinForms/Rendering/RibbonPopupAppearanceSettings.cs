namespace RibbonKit.WinForms;

/// <summary>Immutable shared floating-popup border and shadow overrides. Existing popup colors, radius, blur, vertical shadow offset and enable policy remain independent.</summary>
public sealed record RibbonPopupAppearanceSettings
{
    /// <summary>Initializes overrides that retain existing floating-popup appearance.</summary>
    public RibbonPopupAppearanceSettings() { }
    /// <summary>Gets optional popup border width in logical pixels, 0..32. Null retains one device pixel; zero hides the border while retaining the legacy one-device-pixel content clearance. Wider borders reserve content space through the actual renderer's GetPopupBorderWidth hook.</summary>
    public int? BorderWidth { get; init; }
    /// <summary>Gets optional horizontal popup-shadow offset in logical pixels, -32..32. Null retains zero. Shared shadow clearance includes both offsets; the existing vertical offset setting stays authoritative.</summary>
    public int? ShadowHorizontalOffset { get; init; }
    /// <summary>Gets optional Gaussian shadow falloff, finite 0.1..16. Null retains 2.5. Larger values concentrate the shadow near the frame; smaller values spread its softness within the existing blur footprint. The shared painter retains its half-device-pixel safety minimum.</summary>
    public double? ShadowFalloff { get; init; }

    internal static void Validate(RibbonPopupAppearanceSettings? settings)
    {
        if (settings is null) return;
        if (settings.BorderWidth is < 0 or > 32 || settings.ShadowHorizontalOffset is < -32 or > 32
            || settings.ShadowFalloff is { } falloff && (!double.IsFinite(falloff) || falloff < .1 || falloff > 16))
            throw new ArgumentOutOfRangeException(nameof(settings), "Popup border width must be 0..32, horizontal shadow offset -32..32 logical pixels and shadow falloff finite 0.1..16, or null for existing appearance.");
    }
}
