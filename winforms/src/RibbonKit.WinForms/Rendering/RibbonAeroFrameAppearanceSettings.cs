namespace RibbonKit.WinForms;

/// <summary>Palette for the Aero-inspired native frame. The solid background spans the caption, tab band and restored edges.</summary>
public sealed record RibbonAeroFrameAppearanceSettings
{
    /// <summary>Gets the width in logical pixels of the restored left, right and bottom edges. The default is six. Zero keeps the upper band only. Valid values are zero through 64.</summary>
    public int RestoredFrameThickness { get; init; } = 6;

    /// <summary>Gets the opaque reference fill for choosing readable caption ink over active native material.</summary>
    public RibbonFill ActiveBackground { get; init; } = new(Color.FromArgb(239, 249, 255), Color.FromArgb(164, 194, 222))
    {
        IntermediateStops = new([new(.39, Color.FromArgb(210, 232, 248)), new(.43, Color.FromArgb(178, 208, 234)), new(.75, Color.FromArgb(196, 220, 239))])
    };

    /// <summary>Gets the neutral fill used by solid and inactive Aero windows.</summary>
    public RibbonFill InactiveBackground { get; init; } = new(Color.FromArgb(243, 247, 251), Color.FromArgb(200, 212, 224));

    /// <summary>Gets the default tint used when the form has no authored frame or caption color.</summary>
    public Color TintColor { get; init; } = Color.FromArgb(89, 130, 170);

    /// <summary>Gets the base native perimeter color for an active window with native material.</summary>
    public Color ActiveBorderColor { get; init; } = Color.FromArgb(89, 123, 157);

    /// <summary>Gets the native perimeter color for solid and inactive Aero windows.</summary>
    public Color InactiveBorderColor { get; init; } = Color.FromArgb(153, 169, 186);

    internal void Validate()
    {
        if (RestoredFrameThickness is < 0 or > 64)
            throw new ArgumentOutOfRangeException(nameof(RestoredFrameThickness));
        if (!RibbonFillColors.IsOpaque(ActiveBackground) || !RibbonFillColors.IsOpaque(InactiveBackground) ||
            TintColor.A != 255 || ActiveBorderColor.A != 255 || InactiveBorderColor.A != 255)
            throw new ArgumentException("Aero frame fallback fills, tint and border colors must be opaque.");
    }
}
