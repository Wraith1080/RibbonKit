using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

/// <summary>Creates a ribbon tab without the designer's generic TabItem content and sizing.</summary>
public sealed class RibbonTabDefaultInitializer : DefaultInitializer
{
    /// <summary>Prevents the built-in TabItem initializer from running before RibbonKit's defaults.</summary>
    public RibbonTabDefaultInitializer() => SkipDesignerInitialization = true;

    /// <inheritdoc />
    public override void InitializeDefaults(ModelItem item)
    {
        DesignLog.Write("Toolbox ribbon tab defaults: skip generic designer initialization");
        DesignModel.InitializeNewItemDefaults(item);
    }
}
