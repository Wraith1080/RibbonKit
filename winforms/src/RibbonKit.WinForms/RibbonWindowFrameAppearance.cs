namespace RibbonKit.WinForms;

/// <summary>Selects a RibbonForm's frame presentation independently of the native backdrop.</summary>
public enum RibbonWindowFrameAppearance
{
    /// <summary>Uses the ordinary theme caption and tab row.</summary>
    Standard = 0,
    /// <summary>Uses an Aero-inspired caption and tab band with a native window perimeter.</summary>
    Office2010Aero = 1
}
