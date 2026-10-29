namespace RibbonKit.WinForms;

/// <summary>Optional Windows 11 system material behind a RibbonForm and its ribbon. Native GDI application content uses opaque RibbonBackdropPanel surfaces.</summary>
public enum RibbonWindowBackdrop
{
    /// <summary>Retains the ordinary solid appearance.</summary>
    None = 0,
    /// <summary>Requests the system's long-lived-window material, currently Mica.</summary>
    Mica = 1,
    /// <summary>Requests the system's transient-window material, currently Desktop Acrylic.</summary>
    Acrylic = 2
}
