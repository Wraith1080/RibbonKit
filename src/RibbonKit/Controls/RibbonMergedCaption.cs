namespace RibbonKit.Controls;

/// <summary>
/// What the user pressed on a caption merged into the ribbon (see
/// <see cref="Ribbon.ShowMergedCaption"/>).
/// </summary>
public enum RibbonMergedCaptionAction
{
    /// <summary>The minimize button.</summary>
    Minimize,

    /// <summary>The restore button — a merged caption only appears while maximized.</summary>
    Restore,

    /// <summary>The close button.</summary>
    Close,
}

/// <summary>
/// Arguments for <see cref="Ribbon.MergedCaptionActionRequested"/>: which caption button the user
/// pressed. The ribbon draws the caption but knows nothing about what it represents, so the host
/// that called <see cref="Ribbon.ShowMergedCaption"/> decides what the action means.
/// </summary>
public class RibbonMergedCaptionEventArgs : EventArgs
{
    /// <summary>Initializes the arguments for <paramref name="action"/>.</summary>
    public RibbonMergedCaptionEventArgs(RibbonMergedCaptionAction action) => Action = action;

    /// <summary>The button the user pressed.</summary>
    public RibbonMergedCaptionAction Action { get; }
}
