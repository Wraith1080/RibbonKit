namespace RibbonKit.Controls;

/// <summary>
/// Implemented by ribbon controls whose rendering size is driven by the adaptive
/// sizing engine of their parent <see cref="RibbonGroup"/>.
/// </summary>
public interface IRibbonSizeAware
{
    /// <summary>Called by the owning group when its size state changes.</summary>
    void ApplySizeState(RibbonGroupSizeState state);
}
