namespace RibbonKit.WinForms;

/// <summary>Immutable command indicator and shared raster-image overrides. These decorate existing content slots without changing command targets, text layout or native editor frames.</summary>
public sealed record RibbonCommandContentSettings
{
    /// <summary>Initializes overrides that retain existing content paint.</summary>
    public RibbonCommandContentSettings() { }
    /// <summary>Gets optional checkbox/radio outline width in logical pixels, 0..32. Null retains one device pixel; zero hides it. Width clamps inside the indicator slot.</summary>
    public int? IndicatorBorderWidth { get; init; }
    /// <summary>Gets optional checkbox/radio outline ink, including alpha. Empty retains enabled, active and disabled palette ink; Transparent hides it.</summary>
    public Color IndicatorBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional tick/mixed-mark ink, including alpha. Empty retains Surface for checkboxes and Foreground/DisabledColor for the Quick Access mixed bar. Existing Accent, DisabledColor and Pressed roles still supply indicator fills.</summary>
    public Color CheckMarkColor { get; init; } = Color.Empty;
    /// <summary>Gets optional tick/mixed-mark stroke width in logical pixels, finite 0..32. Null retains the DPI-scaled 1.5-pixel checkbox stroke with its one-device-pixel minimum and the two-logical-pixel Quick Access mixed bar. Zero hides the mark.</summary>
    public double? CheckMarkStrokeWidth { get; init; }
    /// <summary>Gets optional tick/mixed-mark scale, finite 0..4. Null retains 1; zero hides it. Authored marks scale about the existing visual center and clip inside the unchanged indicator slot.</summary>
    public double? CheckMarkScale { get; init; }
    /// <summary>Gets optional radio-dot inset in logical pixels, 0..1024. Null retains four logical pixels. Zero fills the circle; values that consume the slot hide the dot.</summary>
    public int? RadioDotInset { get; init; }
    /// <summary>Gets optional radio-dot ink, including alpha. Empty retains Accent or DisabledColor; Transparent hides its paint.</summary>
    public Color RadioDotColor { get; init; } = Color.Empty;
    /// <summary>Gets optional filled dropdown-triangle width in logical pixels, 0..64. Null retains eight. Clamps to the existing glyph slot; zero hides it. Stroked chevrons use Chevrons instead.</summary>
    public int? DropDownGlyphWidth { get; init; }
    /// <summary>Gets optional filled dropdown-triangle height in logical pixels, 0..64. Null retains six. Clamps to the existing glyph slot; zero hides it. Arrow color still comes from the actual renderer caller.</summary>
    public int? DropDownGlyphHeight { get; init; }
    /// <summary>Gets optional grayscale disabled-raster opacity, finite 0..1. Null retains 0.55 for ordinary images and the framework treatment for independently disabled Quick Access references. Explicit values apply to both paths. Enabled images and borrowed vector-icon rendering remain unchanged.</summary>
    public double? DisabledImageOpacity { get; init; }
    /// <summary>Gets optional disabled-raster grayscale amount, finite 0..1. Zero retains source colors; one uses the existing grayscale coefficients. Null retains each ordinary/Quick Access recipe. Authoring either raster setting selects the shared matrix treatment with 0.55 opacity and full grayscale as independent null fallbacks. Vector icons remain unchanged.</summary>
    public double? DisabledImageGrayscaleAmount { get; init; }

    internal static void Validate(RibbonCommandContentSettings? settings)
    {
        if (settings is null) return;
        if (settings.IndicatorBorderWidth is < 0 or > 32 || settings.RadioDotInset is < 0 or > 1024 || settings.DropDownGlyphWidth is < 0 or > 64 || settings.DropDownGlyphHeight is < 0 or > 64
            || Invalid(settings.CheckMarkStrokeWidth, 32) || Invalid(settings.CheckMarkScale, 4) || Invalid(settings.DisabledImageOpacity, 1) || Invalid(settings.DisabledImageGrayscaleAmount, 1))
            throw new ArgumentOutOfRangeException(nameof(settings), "Indicator width must be 0..32, dot inset 0..1024, triangle dimensions 0..64, mark stroke 0..32, mark scale 0..4 and disabled opacity/grayscale amount 0..1. Numeric overrides must be finite, or null for existing paint.");
        static bool Invalid(double? value, double maximum) => value is { } n && (!double.IsFinite(n) || n < 0 || n > maximum);
    }
}
