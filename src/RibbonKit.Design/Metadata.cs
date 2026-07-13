using Microsoft.VisualStudio.DesignTools.Extensibility.Features;
using Microsoft.VisualStudio.DesignTools.Extensibility.Metadata;
using RibbonKit.Design;

// Tells the designer this assembly carries design-time metadata for RibbonKit.
[assembly: ProvideMetadata(typeof(Metadata))]

namespace RibbonKit.Design;

/// <summary>
/// Design-time metadata table for RibbonKit. Attaches the design-time feature providers
/// (default initializer + context-menu verbs) to the ribbon control types.
/// </summary>
/// <remarks>
/// Control types are referenced by their full CLR name as STRINGS, never <c>typeof</c>:
/// the new XAML designer runs this assembly in the Visual Studio (.NET Framework) process,
/// isolated from the running .NET 8/9 control instances, so it cannot load the real types.
/// </remarks>
internal sealed class Metadata : IProvideAttributeTable
{
    // Full CLR type names of the runtime controls (in RibbonKit.dll).
    private const string RibbonType = "RibbonKit.Controls.Ribbon";
    private const string RibbonTabType = "RibbonKit.Controls.RibbonTab";
    private const string RibbonGroupType = "RibbonKit.Controls.RibbonGroup";

    /// <inheritdoc />
    public AttributeTable AttributeTable
    {
        get
        {
            var builder = new AttributeTableBuilder();

            // Ribbon: seed a starter tab/group when dropped, and offer "Add Tab" on right-click.
            builder.AddCustomAttributes(
                RibbonType,
                new FeatureAttribute(typeof(RibbonDefaultInitializer)),
                new FeatureAttribute(typeof(RibbonContextMenuProvider)));

            // Tab: "Add Group".
            builder.AddCustomAttributes(
                RibbonTabType,
                new FeatureAttribute(typeof(RibbonTabContextMenuProvider)));

            // Group: "Add Button / Toggle / Split / Drop-Down".
            builder.AddCustomAttributes(
                RibbonGroupType,
                new FeatureAttribute(typeof(RibbonGroupContextMenuProvider)));

            return builder.CreateTable();
        }
    }
}
