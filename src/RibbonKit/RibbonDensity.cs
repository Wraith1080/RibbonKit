namespace RibbonKit;

/// <summary>Controls the target size and spacing of ribbon commands, independently of theme.</summary>
public enum RibbonDensity
{
    /// <summary>The standard compact ribbon geometry, optimized for mouse and keyboard use.</summary>
    Compact = 0,

    /// <summary>Roomier command targets and spacing for touch use.</summary>
    Touch = 1,
}
