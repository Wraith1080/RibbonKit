namespace RibbonKit;

/// <summary>
/// The rendering size of an individual ribbon control.
/// </summary>
public enum RibbonControlSize
{
    /// <summary>Large icon (32px) with the label underneath — full-height button.</summary>
    Large,

    /// <summary>Small icon (16px) with the label to the right — single-row button.</summary>
    Medium,

    /// <summary>Small icon (16px) only — most compact form.</summary>
    Small,
}

/// <summary>
/// The size state of a <see cref="Controls.RibbonGroup"/>, assigned by the adaptive
/// sizing engine as available ribbon width changes.
/// </summary>
public enum RibbonGroupSizeState
{
    /// <summary>Full width — adaptable controls render at their largest declared size.</summary>
    Large,

    /// <summary>Reduced — adaptable controls render at their middle declared size.</summary>
    Medium,

    /// <summary>Most compact in-place form — adaptable controls at their smallest size.</summary>
    Small,

    /// <summary>
    /// The group renders as a single button (icon + name + chevron); its full content
    /// opens in a flyout when clicked — like modern Office.
    /// </summary>
    Collapsed,
}

/// <summary>
/// Where the ribbon renders its <see cref="Controls.Ribbon.QuickAccessItems"/>.
/// (A third placement — the window title bar — is available manually via
/// <see cref="Controls.RibbonWindow.TitleBarContent"/>.)
/// </summary>
public enum RibbonQuickAccessPosition
{
    /// <summary>In the tab strip row, right after the application button.</summary>
    TabRow,

    /// <summary>In a full-width row below the ribbon — the classic Office option.</summary>
    BelowRibbon,
}

/// <summary>
/// How the sizing engine reduces a <see cref="Controls.RibbonGroup"/> when ribbon
/// width runs out.
/// </summary>
public enum RibbonGroupReductionMode
{
    /// <summary>
    /// Default (Office 2024 behavior): the group goes straight from its full layout to
    /// a single collapsed button whose flyout shows the full content.
    /// </summary>
    Collapse,

    /// <summary>
    /// Controls with a SizeDefinition shrink in place first (Large → Medium → Small),
    /// then the group collapses to a button as the last step.
    /// </summary>
    ResizeThenCollapse,

    /// <summary>
    /// Controls shrink in place only; the group never collapses to a button.
    /// </summary>
    Resize,
}
