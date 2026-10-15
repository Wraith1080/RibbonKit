using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

/// <summary>Seeds new supported commands with visible, labeled simplified presentation.</summary>
public sealed class SimplifiedCommandInitializer : DefaultInitializer
{
    /// <inheritdoc />
    public override void InitializeDefaults(ModelItem item)
    {
        DesignLog.Write("Toolbox simplified defaults: " + DesignModel.TypeName(item));
        DesignModel.InitializeSimplifiedDefaults(item);
    }
}
