using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

/// <summary>
/// Seeds a freshly dropped <c>Ribbon</c> with a starter tab and group so the design surface
/// isn't empty. Runs inside the drop's own edit transaction, so no explicit
/// <see cref="ModelEditingScope"/> is opened here.
/// </summary>
public sealed class RibbonDefaultInitializer : DefaultInitializer
{
    /// <inheritdoc />
    public override void InitializeDefaults(ModelItem item)
    {
        // item == the new Ribbon.
        ModelItem tab = DesignModel.Create(item, "RibbonTab");
        tab.Properties["Header"].SetValue("Home");

        ModelItem group = DesignModel.Create(item, "RibbonGroup");
        group.Properties["Header"].SetValue("Group");

        DesignModel.Add(tab, "Groups", group);   // group -> tab.Groups
        DesignModel.Add(item, "Tabs", tab);      // tab   -> ribbon.Tabs
    }
}
