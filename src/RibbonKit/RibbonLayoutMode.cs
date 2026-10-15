namespace RibbonKit;

/// <summary>The arrangement of commands in the ribbon body, independently of target density.</summary>
public enum RibbonLayoutMode
{
    /// <summary>The full-height ribbon with its original group layouts.</summary>
    Classic = 0,
    /// <summary>A single row of selected commands with overflow and native group flyouts.</summary>
    Simplified = 1,
}

/// <summary>Where a command is exposed when its ribbon uses simplified layout.</summary>
public enum RibbonSimplifiedPlacement
{
    /// <summary>Available through its full-group flyout in More Commands.</summary>
    Overflow = 0,
    /// <summary>Shown in the single command row when space and the control type allow.</summary>
    Inline = 1,
}
