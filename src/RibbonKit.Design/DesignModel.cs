using System;
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

    /// <summary>
    /// Moves <paramref name="item"/> out of its current <paramref name="fromCollectionProperty"/> and into
    /// <paramref name="targetParent"/>'s <paramref name="toCollectionProperty"/> at <paramref name="insertIndex"/>,
    /// as a single undo. Handles both plain reordering (same parent+collection — the insert index is
    /// adjusted for the removal shift) and cross-parent moves (a group to another tab, a control to
    /// another group/panel). The caller is responsible for compatibility (which collection accepts the
    /// item). No-op when either collection can't be resolved.
    /// </summary>
    public static void MoveInto(ModelItem item, string fromCollectionProperty, ModelItem targetParent, string toCollectionProperty, int insertIndex)
    {
        ModelItem fromParent = item.Parent;
        if (fromParent is null || targetParent is null)
        {
            return;
        }

        ModelItemCollection fromCollection = FindProperty(fromParent, fromCollectionProperty)?.Collection;
        ModelItemCollection toCollection = FindProperty(targetParent, toCollectionProperty)?.Collection;
        if (fromCollection is null || toCollection is null)
        {
            return;
        }

        bool sameCollection = ReferenceEquals(fromParent, targetParent) && fromCollectionProperty == toCollectionProperty;

        try
        {
            using (ModelEditingScope scope = item.BeginEdit("Move"))
            {
                int oldIndex = fromCollection.IndexOf(item);
                fromCollection.Remove(item);

                int index = insertIndex;
                // Removing an earlier item from the same collection shifts every later index down by one.
                if (sameCollection && oldIndex >= 0 && oldIndex < index)
                {
                    index--;
                }

                index = Math.Max(0, Math.Min(index, toCollection.Count));
                toCollection.Insert(index, item);
                scope.Complete();
            }
        }
        catch (Exception ex)
        {
            DesignLog.Error("MoveInto", ex);
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
    /// The named <see cref="ModelProperty"/>, or <see langword="null"/> when the item's type doesn't
    /// define it. NOTE: the designer's <c>Properties[name]</c> indexer THROWS
    /// <see cref="ArgumentException"/> for an unknown property (it does NOT return null), so every
    /// "does this type have property X?" check must go through here. This is why the editor can walk
    /// mixed control types (a group holding buttons, combo boxes, galleries — only some have a Header).
    /// </summary>
    public static ModelProperty FindProperty(ModelItem item, string name)
    {
        try
        {
            return item.Properties[name];
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// The child <see cref="ModelItem"/>s in <paramref name="parent"/>'s named collection property,
    /// or an empty list when the property is absent/unset. A snapshot (safe to iterate while editing).
    /// </summary>
    public static IReadOnlyList<ModelItem> Children(ModelItem parent, string collectionProperty)
    {
        ModelItemCollection collection = FindProperty(parent, collectionProperty)?.Collection;
        return collection is null ? new List<ModelItem>() : collection.ToList();
    }

    /// <summary>The effective <c>Header</c> text of <paramref name="item"/> (includes defaults), or "" when the type has no Header.</summary>
    public static string Header(ModelItem item) =>
        FindProperty(item, "Header")?.ComputedValue?.ToString() ?? string.Empty;

    /// <summary>Whether <paramref name="item"/>'s type defines a <c>Header</c> property (so it can be renamed).</summary>
    public static bool HasHeader(ModelItem item) => FindProperty(item, "Header") != null;

    /// <summary>Whether <paramref name="item"/>'s type defines the named property.</summary>
    public static bool HasProperty(ModelItem item, string name) => FindProperty(item, name) != null;

    /// <summary>The effective value of the named property (includes defaults), or null when absent.</summary>
    public static object GetValue(ModelItem item, string name) => FindProperty(item, name)?.ComputedValue;

    /// <summary>The effective value of the named property as text, or "" when absent/null.</summary>
    public static string GetString(ModelItem item, string name) => GetValue(item, name)?.ToString() ?? string.Empty;

    /// <summary>The effective value of the named boolean property (false when absent or unparseable).</summary>
    public static bool GetBool(ModelItem item, string name)
    {
        object value = GetValue(item, name);
        return value is bool b ? b : bool.TryParse(value?.ToString(), out bool parsed) && parsed;
    }

    /// <summary>
    /// Sets the named property to <paramref name="value"/> as a single undo. Enums and brushes can be
    /// passed as strings — the property's type converter resolves them (same as the QAT verb's enum set).
    /// A conversion failure (e.g. an invalid colour string) is logged, not thrown.
    /// </summary>
    public static void SetProperty(ModelItem item, string name, object value)
    {
        ModelProperty property = FindProperty(item, name);
        if (property is null)
        {
            return;
        }

        try
        {
            using (ModelEditingScope scope = item.BeginEdit("Set " + name))
            {
                property.SetValue(value);
                scope.Complete();
            }
        }
        catch (Exception ex)
        {
            DesignLog.Error("SetProperty " + name + " = '" + value + "'", ex);
        }
    }

    // ---- Attached properties (Ribbon.CommandId, …) ------------------------------------
    //
    // These live on a DIFFERENT declaring type than the element they're set on, so the plain
    // Properties[name] indexer can't reach them (it only sees an element's own members and THROWS for
    // an attached one). They must be resolved by a type-qualified PropertyIdentifier — the same
    // identifier form TabPreview already uses. The exact ModelPropertyCollection accessor for that in
    // this SDK build can't be verified from the Linux sandbox (the reference assembly is supplied by VS
    // at design time), so the accessor is bound by REFLECTION and the working shape is logged — a
    // Windows build confirms it, and a wrong guess degrades to a no-op instead of breaking the build.

    /// <summary>
    /// Resolves the <see cref="ModelProperty"/> for an attached property declared on
    /// <paramref name="ownerTypeName"/> (e.g. <c>Ribbon.CommandId</c> or <c>KeyTip.Keys</c>) on
    /// <paramref name="item"/>, whether or not it's currently set. Returns null (logged) when the model
    /// can't resolve it.
    /// </summary>
    public static ModelProperty FindAttached(ModelItem item, string ownerTypeName, string name)
    {
        if (item is null)
        {
            return null;
        }

        // Fast path: when the property is already set (e.g. the showcase buttons carry rk:Ribbon.CommandId
        // / rk:KeyTip.Keys), the string indexer resolves it under one of these key forms. Which one the
        // model uses is unverified, so try each — none throws past FindProperty.
        string ownerShort = ownerTypeName.Substring(ownerTypeName.LastIndexOf('.') + 1);
        ModelProperty existing =
            FindProperty(item, name)
            ?? FindProperty(item, ownerShort + "." + name)
            ?? FindProperty(item, ownerTypeName + "." + name);
        if (existing != null)
        {
            return existing;
        }

        // Slow path (property not yet set on this element): resolve the attachable member by a
        // type-qualified PropertyIdentifier via whichever collection accessor this SDK exposes
        // (Find(PropertyIdentifier), confirmed on Windows for CommandId).
        try
        {
            var pid = new PropertyIdentifier(new TypeIdentifier(ownerTypeName), name);
            return ResolveByIdentifier(item.Properties, pid);
        }
        catch (Exception ex)
        {
            DesignLog.Error("FindAttached " + ownerTypeName + "." + name, ex);
            return null;
        }
    }

    /// <summary>
    /// Reflectively invokes <c>Find(PropertyIdentifier)</c> or the <c>this[PropertyIdentifier]</c> indexer
    /// on the property collection — whichever exists — logging which shape resolved the member. Binding by
    /// reflection avoids a hard compile dependency on an accessor whose exact signature is unverified here.
    /// </summary>
    private static ModelProperty ResolveByIdentifier(object properties, PropertyIdentifier pid)
    {
        Type type = properties.GetType();

        var find = type.GetMethod("Find", new[] { typeof(PropertyIdentifier) });
        if (find != null && find.Invoke(properties, new object[] { pid }) is ModelProperty viaFind)
        {
            DesignLog.Write("FindAttached: resolved via Find(PropertyIdentifier)");
            return viaFind;
        }

        var indexer = type.GetProperty("Item", typeof(ModelProperty), new[] { typeof(PropertyIdentifier) });
        if (indexer != null && indexer.GetValue(properties, new object[] { pid }) is ModelProperty viaIndexer)
        {
            DesignLog.Write("FindAttached: resolved via this[PropertyIdentifier]");
            return viaIndexer;
        }

        DesignLog.Write("FindAttached: no PropertyIdentifier accessor on " + type.FullName);
        return null;
    }

    /// <summary>The effective value of an attached property as text, or "" when absent/unresolved.</summary>
    public static string GetAttachedString(ModelItem item, string ownerTypeName, string name) =>
        FindAttached(item, ownerTypeName, name)?.ComputedValue?.ToString() ?? string.Empty;

    /// <summary>
    /// Sets (or, for empty text, clears) an attached property as a single undo. Clearing removes the
    /// attribute rather than writing an empty string, matching how the icon editor treats a blank value.
    /// </summary>
    public static void SetAttached(ModelItem item, string ownerTypeName, string name, string value)
    {
        ModelProperty property = FindAttached(item, ownerTypeName, name);
        if (property is null)
        {
            DesignLog.Error("SetAttached", new Exception("could not resolve attached property " + ownerTypeName + "." + name));
            return;
        }

        try
        {
            using (ModelEditingScope scope = item.BeginEdit("Set " + name))
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    property.ClearValue();
                }
                else
                {
                    property.SetValue(value.Trim());
                }

                scope.Complete();
            }
        }
        catch (Exception ex)
        {
            DesignLog.Error("SetAttached " + ownerTypeName + "." + name + " = '" + value + "'", ex);
        }
    }

    /// <summary>
    /// The resource key of a <c>{StaticResource …}</c>-valued property (e.g. an icon), or "" when the
    /// property is unset or isn't a static resource. Reading this back is also the key diagnostic: it
    /// logs the model type of an existing icon value, which reveals exactly how a StaticResource is
    /// represented in the model (and therefore how to create one).
    /// </summary>
    public static string GetStaticResourceKey(ModelItem item, string name)
    {
        ModelProperty property = FindProperty(item, name);
        ModelItem value = property?.Value;
        if (value is null)
        {
            return string.Empty;
        }

        return FindProperty(value, "ResourceKey")?.ComputedValue?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Sets the named property to a <c>{StaticResource <paramref name="key"/>}</c> reference — used for
    /// icon properties, whose values are <c>DrawingImage</c> resources in Icons.xaml (there is no plain
    /// value / URI form). A raw <c>StaticResourceExtension</c> CLR object loses its key when serialized
    /// (the model writes an empty <c>{StaticResource}</c>); the model serializes the model TREE, so the
    /// extension must be created as a <see cref="ModelItem"/> with its <c>ResourceKey</c> set in the model.
    /// Read-back is logged so a Windows build confirms whether the key round-trips.
    /// </summary>
    public static void SetStaticResource(ModelItem item, string name, string key)
    {
        ModelProperty property = FindProperty(item, name);
        if (property is null || string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        key = key.Trim();

        try
        {
            using (ModelEditingScope scope = item.BeginEdit("Set " + name))
            {
                ModelItem extension = CreateStaticResourceItem(item, key);
                if (extension is null)
                {
                    DesignLog.Error("SetStaticResource", new Exception("could not create a StaticResource model item"));
                    return;
                }

                property.SetValue(extension);
                scope.Complete();
            }
        }
        catch (Exception ex)
        {
            DesignLog.Error("SetStaticResource " + name + " = {StaticResource " + key + "}", ex);
        }
    }

    /// <summary>
    /// Creates a StaticResource markup-extension <see cref="ModelItem"/> with <c>ResourceKey</c> =
    /// <paramref name="key"/>. Tries a couple of type-identifier forms since the exact one the new
    /// designer wants is unverified; each attempt is logged so we can see which works.
    /// </summary>
    private static ModelItem CreateStaticResourceItem(ModelItem context, string key)
    {
        const string PresentationNs = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var attempts = new[]
        {
            new TypeIdentifier(PresentationNs, "StaticResourceExtension"),
            new TypeIdentifier(PresentationNs, "StaticResource"),
            new TypeIdentifier("System.Windows.StaticResourceExtension"),
        };

        foreach (TypeIdentifier id in attempts)
        {
            try
            {
                ModelItem extension = ModelFactory.CreateItem(context.Context, id);
                ModelProperty resourceKey = FindProperty(extension, "ResourceKey");
                if (resourceKey != null)
                {
                    resourceKey.SetValue(key);
                    return extension;
                }
            }
            catch (Exception ex)
            {
                DesignLog.Write("create " + id + " failed: " + ex.Message);
            }
        }

        return null;
    }

    /// <summary>The simple type name of <paramref name="item"/> (e.g. <c>"RibbonButton"</c>).</summary>
    public static string TypeName(ModelItem item) => item.ItemType.Name;

    /// <summary>The zero-based index of <paramref name="item"/> in its parent's collection, or -1.</summary>
    public static int IndexInParent(ModelItem item, string parentCollectionProperty)
    {
        ModelItemCollection collection = item.Parent is null ? null : FindProperty(item.Parent, parentCollectionProperty)?.Collection;
        return collection?.IndexOf(item) ?? -1;
    }

    /// <summary>The number of siblings in <paramref name="item"/>'s parent collection (0 when it has no parent).</summary>
    public static int SiblingCount(ModelItem item, string parentCollectionProperty)
    {
        ModelItemCollection collection = item.Parent is null ? null : FindProperty(item.Parent, parentCollectionProperty)?.Collection;
        return collection?.Count ?? 0;
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
    /// into <paramref name="parent"/>'s <paramref name="collectionProperty"/> (a group's <c>Items</c>
    /// or a container's <c>Children</c>), labelled <paramref name="label"/>. When <paramref name="size"/>
    /// is given it seeds the control's <c>Size</c> (e.g. "Small" for stacked icon rows). Returns the control.
    /// </summary>
    public static ModelItem AddControl(ModelItem parent, string collectionProperty, string typeName, string header = null, string size = null)
    {
        using (ModelEditingScope scope = parent.BeginEdit("Add " + (header ?? typeName)))
        {
            ModelItem control = CreateAny(parent, typeName);
            if (control is null)
            {
                DesignLog.Error("AddControl", new Exception("could not create " + typeName));
                return null;
            }

            if (!string.IsNullOrEmpty(header))
            {
                FindProperty(control, "Header")?.SetValue(header);
            }

            if (size != null)
            {
                FindProperty(control, "Size")?.SetValue(size);
            }

            Add(parent, collectionProperty, control);
            scope.Complete();
            return control;
        }
    }

    /// <summary>Creates a control by simple type name, trying the RibbonKit xmlns first, then WPF framework types (for <c>Separator</c> etc.).</summary>
    private static ModelItem CreateAny(ModelItem context, string typeName)
    {
        try
        {
            return ModelFactory.CreateItem(context.Context, new TypeIdentifier(Xmlns, typeName));
        }
        catch
        {
            // Not a RibbonKit type — fall through to the framework namespaces.
        }

        return CreateFramework(context, typeName);
    }

    /// <summary>
    /// Adds a new <c>StackPanel</c> (a framework type, created via the presentation namespace) into
    /// <paramref name="parent"/>'s <paramref name="collectionProperty"/>, with the given
    /// <paramref name="orientation"/> ("Vertical"/"Horizontal"), and returns it. Null if it couldn't
    /// be created (logged).
    /// </summary>
    public static ModelItem AddStackPanel(ModelItem parent, string collectionProperty, string orientation)
    {
        using (ModelEditingScope scope = parent.BeginEdit("Add Stack Panel"))
        {
            ModelItem stack = CreateFramework(parent, "StackPanel");
            if (stack is null)
            {
                DesignLog.Error("AddStackPanel", new Exception("could not create StackPanel"));
                return null;
            }

            FindProperty(stack, "Orientation")?.SetValue(orientation);
            Add(parent, collectionProperty, stack);
            scope.Complete();
            return stack;
        }
    }

    /// <summary>Creates a WPF framework element (e.g. <c>StackPanel</c>) by simple type name, trying the presentation xmlns then the CLR name.</summary>
    private static ModelItem CreateFramework(ModelItem context, string typeName)
    {
        const string PresentationNs = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var attempts = new[]
        {
            new TypeIdentifier(PresentationNs, typeName),
            new TypeIdentifier("System.Windows.Controls." + typeName),
        };

        foreach (TypeIdentifier id in attempts)
        {
            try
            {
                return ModelFactory.CreateItem(context.Context, id);
            }
            catch (Exception ex)
            {
                DesignLog.Write("create framework " + id + " failed: " + ex.Message);
            }
        }

        return null;
    }

    /// <summary>Sets <paramref name="item"/>'s <c>Header</c> to <paramref name="header"/> as a single undo (no-op if the type has no Header).</summary>
    public static void Rename(ModelItem item, string header)
    {
        ModelProperty property = FindProperty(item, "Header");
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

    // ---- Caption (Header for controls/tabs/backstage items; Content for combo/gallery items) --

    /// <summary>
    /// The property that carries an item's editable text caption: <c>Header</c> if it's a simple value,
    /// else <c>Content</c> if it's a simple value, else <c>Tag</c> for a gallery item (whose Content is a
    /// visual, not text), else null. Complex values (e.g. a <c>StackPanel</c> in a gallery item's Content)
    /// are skipped so the tree never shows a stringified object handle.
    /// </summary>
    public static string CaptionProperty(ModelItem item)
    {
        if (IsScalarValue(FindProperty(item, "Header")))
        {
            return "Header";
        }

        if (IsScalarValue(FindProperty(item, "Content")))
        {
            return "Content";
        }

        // Gallery items carry a visual in Content but identify themselves by Tag.
        if (TypeNameOrEmpty(item) == "RibbonGalleryItem" && IsScalarValue(FindProperty(item, "Tag")))
        {
            return "Tag";
        }

        return null;
    }

    /// <summary>
    /// True when the property holds a simple text/number value (or is unset). Keys off the value's TYPE,
    /// not <c>ModelProperty.Value</c>: this designer wraps even a plain string in a child ModelItem, so
    /// a <c>Value != null</c> test wrongly flags string Header/Content as "complex". A real complex value
    /// (a StackPanel, etc.) surfaces here as a non-string object.
    /// </summary>
    private static bool IsScalarValue(ModelProperty property)
    {
        if (property is null)
        {
            return false;
        }

        object computed = property.ComputedValue;
        return computed is null || computed is string || computed.GetType().IsPrimitive || computed is decimal;
    }

    private static string TypeNameOrEmpty(ModelItem item)
    {
        try
        {
            return item.ItemType.Name;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>Whether <paramref name="item"/> has an editable caption (Header or Content).</summary>
    public static bool HasCaption(ModelItem item) => CaptionProperty(item) != null;

    /// <summary>
    /// The single complex element held in <paramref name="item"/>'s <c>Content</c> (e.g. a gallery item's
    /// visual StackPanel), or null when Content is unset or a plain text value (a string is the item's
    /// caption, not a child element).
    /// </summary>
    public static ModelItem ContentElement(ModelItem item)
    {
        ModelProperty content = FindProperty(item, "Content");
        return content is null || IsScalarValue(content) ? null : content.Value;
    }

    /// <summary>The item's caption text (from Header or Content), or "".</summary>
    public static string GetCaption(ModelItem item)
    {
        string property = CaptionProperty(item);
        return property is null ? string.Empty : GetString(item, property);
    }

    /// <summary>Sets the item's caption (Header or Content, whichever it has) as a single undo.</summary>
    public static void SetCaption(ModelItem item, string text)
    {
        string property = CaptionProperty(item);
        if (property != null)
        {
            SetProperty(item, property, text);
        }
    }

    /// <summary>
    /// Adds a child item (a combo/gallery/backstage entry) of type <paramref name="typeName"/> to
    /// <paramref name="container"/>'s <c>Items</c>, captioned <paramref name="label"/> on
    /// <paramref name="captionProperty"/> (<c>Content</c> or <c>Header</c>). Returns the item, or null.
    /// </summary>
    public static ModelItem AddItem(ModelItem container, string typeName, string captionProperty, string label)
    {
        using (ModelEditingScope scope = container.BeginEdit("Add " + label))
        {
            ModelItem item = CreateAny(container, typeName);
            if (item is null)
            {
                DesignLog.Error("AddItem", new System.Exception("could not create " + typeName));
                return null;
            }

            if (!string.IsNullOrEmpty(captionProperty) && !string.IsNullOrEmpty(label))
            {
                FindProperty(item, captionProperty)?.SetValue(label);
            }

            Add(container, "Items", item);
            scope.Complete();
            return item;
        }
    }
}
