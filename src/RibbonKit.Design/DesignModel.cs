using Microsoft.VisualStudio.DesignTools.Extensibility.Metadata;
using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

/// <summary>
/// Small helpers over the designer Model API so the type-creation details live in ONE place.
/// </summary>
internal static class DesignModel
{
    /// <summary>CLR namespace that all the ribbon controls live in (inside RibbonKit.dll).</summary>
    private const string ControlsNamespace = "RibbonKit.Controls";

    /// <summary>
    /// Creates a new design-time <see cref="ModelItem"/> for one of our controls, identified by
    /// simple type name (e.g. "RibbonTab"). The caller adds it into the right collection.
    /// </summary>
    /// <remarks>
    /// VERIFY IN VS: the new designer identifies types with <see cref="TypeIdentifier"/> (a
    /// namespace + name string), not <c>System.Type</c>. If the designer can't resolve a type,
    /// this is the single line to adjust — e.g. the namespace form (CLR vs xmlns) or the
    /// <see cref="ModelFactory.CreateItem(EditingContext, TypeIdentifier)"/> overload.
    /// </remarks>
    public static ModelItem Create(ModelItem sibling, string typeName)
    {
        var id = new TypeIdentifier(ControlsNamespace, typeName);
        return ModelFactory.CreateItem(sibling.Context, id);
    }

    /// <summary>
    /// Adds <paramref name="child"/> into <paramref name="parent"/>'s content collection
    /// (Ribbon→Tabs, RibbonTab→Groups, RibbonGroup→Items — all declared as the content property).
    /// </summary>
    public static void AddChild(ModelItem parent, ModelItem child) =>
        parent.Content.Collection.Add(child);
}
