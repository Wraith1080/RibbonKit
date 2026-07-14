using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// Reorders <paramref name="item"/> within its parent's <paramref name="parentCollectionProperty"/>
    /// collection by <paramref name="delta"/> (-1 = left/earlier, +1 = right/later). No-op at the ends.
    /// </summary>
    public static void Move(ModelItem item, string parentCollectionProperty, int delta)
    {
        ModelItem parent = item.Parent;
        if (parent is null)
        {
            return;
        }

        ModelItemCollection collection = parent.Properties[parentCollectionProperty].Collection;
        int from = collection.IndexOf(item);
        int to = from + delta;
        if (from < 0 || to < 0 || to >= collection.Count)
        {
            return;
        }

        using (ModelEditingScope scope = item.BeginEdit(delta < 0 ? "Move Left" : "Move Right"))
        {
            collection.Remove(item);
            collection.Insert(to, item);
            scope.Complete();
        }
    }

    /// <summary>Removes <paramref name="item"/> from its parent's <paramref name="parentCollectionProperty"/> collection.</summary>
    public static void Delete(ModelItem item, string parentCollectionProperty)
    {
        ModelItem parent = item.Parent;
        if (parent is null)
        {
            return;
        }

        using (ModelEditingScope scope = item.BeginEdit("Delete"))
        {
            parent.Properties[parentCollectionProperty].Collection.Remove(item);
            scope.Complete();
        }
    }

    // ---- Reads (used by the editor dialog to build its tree) --------------------------

    /// <summary>
    /// The child <see cref="ModelItem"/>s in <paramref name="parent"/>'s named collection property,
    /// or an empty list when the property is unset. A snapshot (safe to iterate while editing).
    /// </summary>
    public static IReadOnlyList<ModelItem> Children(ModelItem parent, string collectionProperty)
    {
        ModelProperty property = parent.Properties[collectionProperty];
        ModelItemCollection collection = property?.Collection;
        return collection is null ? new List<ModelItem>() : collection.ToList();
    }

    /// <summary>The effective <c>Header</c> text of <paramref name="item"/> (includes defaults), or "" when it has none.</summary>
    public static string Header(ModelItem item)
    {
        ModelProperty header = item.Properties["Header"];
        return header?.ComputedValue?.ToString() ?? string.Empty;
    }

    /// <summary>The simple type name of <paramref name="item"/> (e.g. <c>"RibbonButton"</c>).</summary>
    public static string TypeName(ModelItem item) => item.ItemType.Name;

    /// <summary>The zero-based index of <paramref name="item"/> in its parent's collection, or -1.</summary>
    public static int IndexInParent(ModelItem item, string parentCollectionProperty)
    {
        ModelItem parent = item.Parent;
        return parent is null ? -1 : parent.Properties[parentCollectionProperty].Collection.IndexOf(item);
    }

    /// <summary>The number of siblings in <paramref name="item"/>'s parent collection (0 when it has no parent).</summary>
    public static int SiblingCount(ModelItem item, string parentCollectionProperty)
    {
        ModelItem parent = item.Parent;
        return parent is null ? 0 : parent.Properties[parentCollectionProperty].Collection.Count;
    }

    // ---- Scoped creation / rename (each is a single undo, like the right-click verbs) --

    /// <summary>Adds a new tab (seeded with one group) to <paramref name="ribbon"/> and returns it.</summary>
    public static ModelItem AddTab(ModelItem ribbon)
    {
        using (ModelEditingScope scope = ribbon.BeginEdit("Add Tab"))
        {
            ModelItem tab = Create(ribbon, "RibbonTab");
            tab.Properties["Header"].SetValue("New Tab");

            ModelItem group = Create(ribbon, "RibbonGroup");
            group.Properties["Header"].SetValue("New Group");
            Add(tab, "Groups", group);

            Add(ribbon, "Tabs", tab);
            scope.Complete();
            return tab;
        }
    }

    /// <summary>Adds a new group to <paramref name="tab"/> and returns it.</summary>
    public static ModelItem AddGroup(ModelItem tab)
    {
        using (ModelEditingScope scope = tab.BeginEdit("Add Group"))
        {
            ModelItem group = Create(tab, "RibbonGroup");
            group.Properties["Header"].SetValue("New Group");
            Add(tab, "Groups", group);
            scope.Complete();
            return group;
        }
    }

    /// <summary>
    /// Adds a new leaf control of type <paramref name="typeName"/> (a button/toggle/split/drop-down)
    /// to <paramref name="group"/>, labelled <paramref name="label"/>, and returns it.
    /// </summary>
    public static ModelItem AddControl(ModelItem group, string typeName, string label)
    {
        using (ModelEditingScope scope = group.BeginEdit("Add " + label))
        {
            ModelItem control = Create(group, typeName);
            control.Properties["Header"]?.SetValue(label);
            Add(group, "Items", control);
            scope.Complete();
            return control;
        }
    }

    /// <summary>Sets <paramref name="item"/>'s <c>Header</c> to <paramref name="header"/> as a single undo.</summary>
    public static void Rename(ModelItem item, string header)
    {
        ModelProperty property = item.Properties["Header"];
        if (property is null)
        {
            return;
        }

        using (ModelEditingScope scope = item.BeginEdit("Rename"))
        {
            property.SetValue(header);
            scope.Complete();
        }
    }
}
