namespace RibbonKit;

/// <summary>How Quick Access Toolbar icons are presented on a colored title bar or tab strip.</summary>
public enum RibbonQuickAccessIconAppearance
{
    /// <summary>Use the icon's alpha mask as a white silhouette. This is the default.</summary>
    WhiteOverlay,

    /// <summary>Keep the original icon colors with a small white glow behind the icon.</summary>
    ColorWithGlow,

    /// <summary>
    /// Adjust solid vector fills and strokes for contrast while retaining colored accents and
    /// internal detail. Generated black parts become partially transparent openings.
    /// Unsupported image and drawing types retain their original appearance.
    /// </summary>
    AdaptiveColor,
}
