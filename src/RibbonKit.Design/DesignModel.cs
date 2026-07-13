using Microsoft.VisualStudio.DesignTools.Extensibility.Metadata;
using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

/// <summary>
/// Small helpers over the designer Model API so the type-creation details live in ONE place.
/// </summary>
internal static class DesignModel
{
    /// <summary>
    /// The XAML namespace the ribbon controls live under — RibbonKit declares
    /// <c>[assembly: XmlnsDefinition("urn:ribbonkit", "RibbonKit.Controls")]</c>, which is why
    /// the showcase uses <c>xmlns:rk="urn:ribbonkit"</c>. The designer resolves a
    /// <see cref="TypeIdentifier"/> by XAML namespace + type name (NOT the CLR namespace), so this
    /// must match the xmlns, otherwise <see cref="ModelFactory.CreateItem"/> can't find the type.
    /// </summary>
    private const string Xmlns = "urn:ribbonkit";

    /// <summary>Creates a new design-time <see cref="ModelItem"/> for one of our controls by simple type name.</summary>
    public static ModelItem Create(ModelItem context, string typeName)
    {
        var id = new TypeIdentifier(Xmlns, typeName);
        return ModelFactory.CreateItem(context.Context, id);
    }

    /// <summary>
    /// Adds <paramref name="child"/> into the named collection property of <paramref name="parent"/>
    /// (Ribbon→"Tabs", RibbonTab→"Groups", RibbonGroup→"Items"). Using the property name explicitly
    /// avoids any ambiguity over which property is the "content" one (esp. for the group's Items).
    /// </summary>
    public static void Add(ModelItem parent, string collectionProperty, ModelItem child) =>
        parent.Properties[collectionProperty].Collection.Add(child);
}
