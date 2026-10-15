using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

/// <summary>Seeds new structural/items controls without changing assigned properties.</summary>
public sealed class RibbonItemDefaultInitializer : DefaultInitializer
{
    /// <inheritdoc />
    public override void InitializeDefaults(ModelItem item)
    {
        DesignLog.Write("Toolbox item defaults: " + DesignModel.TypeName(item));
        DesignModel.InitializeNewItemDefaults(item);
    }
}
