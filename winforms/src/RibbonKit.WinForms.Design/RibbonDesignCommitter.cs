using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Security.Cryptography;
using System.Text;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design;

// Design-only draft mapping. Existing components are retained so resources,
// bindings, event descriptors and external references keep their owners.
internal static class RibbonDesignCommitter
{
    private static readonly Dictionary<string, Type> Kinds = new()
    {
        ["Tab"] = typeof(RibbonTab), ["Group"] = typeof(RibbonGroup), ["Button"] = typeof(RibbonButtonItem),
        ["Toggle"] = typeof(RibbonToggleItem), ["CheckBox"] = typeof(RibbonCheckBoxItem), ["RadioButton"] = typeof(RibbonRadioButtonItem),
        ["DropDown"] = typeof(RibbonDropDownItem), ["SplitButton"] = typeof(RibbonSplitButtonItem),
        ["TextBox"] = typeof(RibbonTextBoxItem), ["ComboBox"] = typeof(RibbonComboBoxItem),
        ["Gallery"] = typeof(RibbonGalleryItem), ["GalleryTile"] = typeof(RibbonGalleryTile), ["Separator"] = typeof(RibbonMenuSeparatorItem)
    };
    private static readonly string[] ScalarNames =
    [
        "Id", "Text", "KeyTip", "Enabled", "Visible", "IsContextual", "ContextualColor", "CanClose", "CloseButtonText",
        "Priority", "ReductionMode", "SizePolicy", "Checked", "CheckState", "GroupName", "MenuDescription",
        "ScreenTipTitle", "ScreenTipDescription", "IsEditable", "Value", "InputWidth", "ReadOnly", "MaxLength", "SelectedIndex",
        "IsInRibbon", "InRibbonColumnCount", "ColumnCount", "TileWidth", "TileHeight", "MaxVisibleRows"
    ];
    private static readonly string[] SizePolicies = new[]
    {
        RibbonSizePolicy.Fixed(RibbonItemSize.Large).ToString(), RibbonSizePolicy.Fixed(RibbonItemSize.Medium).ToString(),
        RibbonSizePolicy.Fixed(RibbonItemSize.Small).ToString(), RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small).ToString()
    }.Concat(from large in Enum.GetValues<RibbonItemSize>() from medium in Enum.GetValues<RibbonItemSize>() from small in Enum.GetValues<RibbonItemSize>()
        select RibbonSizePolicy.Adaptive(large, medium, small).ToString()).Distinct(StringComparer.Ordinal).ToArray();

    internal static RibbonDesignSnapshot Read(RibbonControl ribbon, IDesignerHost host)
    {
        var elements = Elements(ribbon.Tabs).ToArray();
        var used = new HashSet<string>(host.Container.Components.Cast<IComponent>().Select(component => component.Site?.Name ?? ""), StringComparer.OrdinalIgnoreCase);
        var snapshot = new RibbonDesignSnapshot
        {
            ReservedNames = host.Container.Components.Cast<IComponent>().Where(component => !elements.Contains(component)).Select(component => component.Site?.Name ?? "").Where(name => name.Length != 0).ToList(),
            ReservedIds = OutsideElements(ribbon).Select(element => element.Id).ToList()
        };
        RibbonDesignNode Map(RibbonElement element)
        {
            string name = element.Site?.Name ?? UniqueName(element.GetType().Name, used);
            return new RibbonDesignNode { Key = Key(element), ComponentName = name, Kind = Kind(element),
                Properties = Properties(element), Choices = element is RibbonComboBoxItem choice ? choice.Items.ToList() : [], Children = ChildElements(element).Select(Map).ToList() };
        }
        snapshot.Tabs = ribbon.Tabs.Select(Map).ToList();
        snapshot.Revision = Fingerprint(snapshot);
        foreach (var kind in Kinds)
        {
            using var element = (RibbonElement)Activator.CreateInstance(kind.Value)!;
            snapshot.Templates.Add(new() { Kind = kind.Key, Properties = Properties(element) });
        }
        return snapshot;
    }
    internal static bool Apply(RibbonControl ribbon, IDesignerHost host, RibbonDesignSnapshot draft)
    {
        var before = Read(ribbon, host);
        if (before.Revision != draft.Revision) throw new ArgumentException("The ribbon changed while the editor was open. Cancel and reopen Edit Ribbon.");
        // Reserved names/IDs and property schemas come from the live designer,
        // not from data sent back by a dialog.
        draft.ReservedIds = before.ReservedIds; draft.ReservedNames = before.ReservedNames; draft.Validate();
        var existing = Elements(ribbon.Tabs).ToDictionary(Key, StringComparer.Ordinal);
        var originalNodes = before.Nodes.ToDictionary(node => node.Key, StringComparer.Ordinal);
        foreach (var node in draft.Nodes)
        {
            if (originalNodes.TryGetValue(node.Key, out var original))
            {
                if (node.Kind != original.Kind || node.ComponentName != original.ComponentName) throw new ArgumentException("Existing component types and names must be preserved. Rename components in Visual Studio's Properties window.");
            }
            else if (!node.Key.StartsWith("new:", StringComparison.Ordinal) || host.Container.Components[node.ComponentName] is not null)
                throw new ArgumentException("A new component needs a unique component name.");
            if (!Kinds.ContainsKey(node.Kind)) throw new ArgumentException("Unsupported command type: " + node.Kind);
            var schema = before.Templates.Single(template => template.Kind == node.Kind);
            if (!schema.Properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal).SetEquals(node.Properties.Select(property => property.Name)))
                throw new ArgumentException("The property set is incomplete or unsupported for " + node.Kind + ". Cancel and reopen Edit Ribbon.");
        }
        if (Fingerprint(draft) == before.Revision) return false;
        // Exercise conversions, setters, native choice constraints and runtime
        // collection/ID rules on detached built-in models before live mutation.
        using (var probe = new RibbonControl())
            foreach (var node in draft.Tabs) probe.Tabs.Add((RibbonTab)BuildProbe(node, originalNodes, before.Templates));
        var retained = draft.Nodes.Select(node => node.Key).ToHashSet(StringComparer.Ordinal);
        var retainedComponents = existing.Where(pair => retained.Contains(pair.Key)).Select(pair => pair.Value).ToHashSet();
        foreach (var entry in ribbon.QuickAccessItems)
        {
            var source = entry switch
            {
                RibbonQuickAccessReference reference => (RibbonElement?)reference.Source,
                RibbonQuickAccessDropDownReference reference => reference.Source,
                RibbonQuickAccessComboBoxReference reference => reference.Source,
                RibbonQuickAccessGroupReference reference => reference.Source,
                _ => null
            };
            if (source is not null && existing.ContainsValue(source) && !retained.Contains(Key(source)))
                throw new ArgumentException("Remove the command's Quick Access reference before removing its source from the ribbon.");
        }
        var changes = (IComponentChangeService?)host.GetService(typeof(IComponentChangeService))
            ?? throw new InvalidOperationException("The designer change service is unavailable.");
        var selected = ribbon.SelectedTab;
        var gallerySelections = existing.Values.OfType<RibbonGalleryItem>().ToDictionary(gallery => gallery, gallery => gallery.SelectedItem);
        var transaction = host.CreateTransaction("Edit Ribbon");
        bool initializing = false;
        try
        {
            var target = new Dictionary<string, RibbonElement>(existing, StringComparer.Ordinal);
            foreach (var node in draft.Nodes)
            {
                if (!target.TryGetValue(node.Key, out var element)) target[node.Key] = element = (RibbonElement)host.CreateComponent(Kinds[node.Kind], node.ComponentName);
                else if (element.Site is null) host.Container.Add(element, node.ComponentName);
            }
            var owners = new object[] { ribbon }.Concat(target.Values).ToArray();
            var collections = owners.Select(owner => (Owner: owner, Collection: Collection(owner))).Where(pair => pair.Collection is not null)
                .Select(pair => (pair.Owner, Property: TypeDescriptor.GetProperties(pair.Owner)[CollectionName(pair.Owner)]!, Collection: pair.Collection!)).ToArray();
            foreach (var collection in collections) changes.OnComponentChanging(collection.Owner, collection.Property);
            ribbon.BeginInit(); initializing = true;
            foreach (var collection in collections) collection.Collection.Clear();
            foreach (var node in draft.Nodes)
            {
                var element = target[node.Key]; var original = originalNodes.GetValueOrDefault(node.Key);
                bool choicesChanged = false;
                if (element is RibbonComboBoxItem combo && !combo.Items.SequenceEqual(node.Choices))
                {
                    choicesChanged = true;
                    var property = TypeDescriptor.GetProperties(combo)[nameof(combo.Items)]!;
                    changes.OnComponentChanging(combo, property); combo.Items.Clear(); combo.Items.AddRange(node.Choices.ToArray()); changes.OnComponentChanged(combo, property, null, null);
                }
                foreach (var value in node.Properties)
                {
                    if (element is RibbonComboBoxItem && value.Name is "Value" or "SelectedIndex") continue;
                    if (original?.Properties.FirstOrDefault(property => property.Name == value.Name)?.Value == value.Value) continue;
                    SetScalar(element, value);
                }
                if (element is RibbonComboBoxItem selection)
                {
                    var baseline = original ?? before.Templates.Single(template => template.Kind == node.Kind);
                    if (SelectionProperty(selection, node, baseline, choicesChanged) is { } property) SetScalar(element, property);
                }
            }
            void SetScalar(RibbonElement element, RibbonDesignProperty value)
            {
                var property = CheckedProperty(element, value.Name); object? old = property.GetValue(element), next = ConvertValue(property, value.Value);
                if (Equals(old, next)) return;
                changes.OnComponentChanging(element, property); property.SetValue(element, next); changes.OnComponentChanged(element, property, old, next);
            }
            void Fill(object owner, IEnumerable<RibbonDesignNode> nodes)
            {
                var collection = Collection(owner)!;
                foreach (var node in nodes)
                {
                    var element = target[node.Key]; if (Collection(element) is not null) Fill(element, node.Children);
                    collection.Add(element);
                }
            }
            Fill(ribbon, draft.Tabs);
            foreach (var pair in gallerySelections)
                if (retainedComponents.Contains(pair.Key) && pair.Value is not null && pair.Key.Items.Contains(pair.Value)) pair.Key.SelectedItem = pair.Value;
            if (selected is not null && ribbon.Tabs.Contains(selected)) ribbon.SelectedTab = selected;
            foreach (var collection in collections) changes.OnComponentChanged(collection.Owner, collection.Property, null, null);
            foreach (var removed in existing.Where(pair => !retained.Contains(pair.Key)).Reverse()) host.DestroyComponent(removed.Value);
            initializing = false; ribbon.EndInit();
            transaction.Commit(); return true;
        }
        catch { transaction.Cancel(); throw; }
        finally { if (initializing) ribbon.EndInit(); ((IDisposable)transaction).Dispose(); }
    }

    private static RibbonElement BuildProbe(RibbonDesignNode node, Dictionary<string, RibbonDesignNode> originals, List<RibbonDesignNode> templates)
    {
        var element = (RibbonElement)Activator.CreateInstance(Kinds[node.Kind])!;
        try
        {
            if (element is RibbonComboBoxItem combo) combo.Items.AddRange(node.Choices.ToArray());
            foreach (var value in node.Properties)
            {
                if (element is RibbonComboBoxItem && value.Name is "Value" or "SelectedIndex") continue;
                var property = CheckedProperty(element, value.Name); property.SetValue(element, ConvertValue(property, value.Value));
            }
            if (element is RibbonComboBoxItem selection)
            {
                var baseline = originals.GetValueOrDefault(node.Key) ?? templates.Single(template => template.Kind == node.Kind);
                // A probe starts empty, so seed the original selection when the
                // draft leaves it unchanged. Changed choices use the same
                // dependency resolution as the live apply operation.
                var value = SelectionProperty(selection, node, baseline, !baseline.Choices.SequenceEqual(node.Choices))
                    ?? node.Properties.Single(property => property.Name == "SelectedIndex");
                var property = CheckedProperty(element, value.Name); property.SetValue(element, ConvertValue(property, value.Value));
            }
            if (Collection(element) is { } children) foreach (var child in node.Children) children.Add(BuildProbe(child, originals, templates));
            return element;
        }
        catch { element.Dispose(); throw; }
    }
    private static RibbonDesignProperty? SelectionProperty(RibbonComboBoxItem selection, RibbonDesignNode node, RibbonDesignNode baseline, bool choicesChanged)
    {
        var value = node.Properties.Single(property => property.Name == "Value");
        var index = node.Properties.Single(property => property.Name == "SelectedIndex");
        bool valueEdited = baseline.Properties.Single(property => property.Name == "Value").Value != value.Value;
        bool indexEdited = baseline.Properties.Single(property => property.Name == "SelectedIndex").Value != index.Value;
        if (!valueEdited && !indexEdited && !choicesChanged) return null;
        int selectedIndex = int.Parse(index.Value, System.Globalization.CultureInfo.InvariantCulture);
        bool sameChoiceAtIndex = selectedIndex >= 0 && selectedIndex < node.Choices.Count && node.Choices[selectedIndex] == value.Value;
        // Mode is set first. A changed Value derives its selection; an explicitly
        // changed index selects that choice. Choice edits retain the value, or
        // its exact duplicate index. New nodes compare against built-in defaults.
        bool useValue = valueEdited && (!indexEdited || selection.IsEditable && selectedIndex < 0)
            || choicesChanged && !indexEdited && !sameChoiceAtIndex;
        return useValue ? value : index;
    }
    private static PropertyDescriptor CheckedProperty(RibbonElement element, string name)
    {
        var property = TypeDescriptor.GetProperties(element)[name];
        if (!ScalarNames.Contains(name) || property is null || property.IsReadOnly) throw new ArgumentException("Unsupported property: " + name);
        return property;
    }
    private static object? ConvertValue(PropertyDescriptor property, string value) => property.PropertyType == typeof(string) ? value : property.Converter.ConvertFromInvariantString(value);
    private static List<RibbonDesignProperty> Properties(RibbonElement element) => ScalarNames
        .Where(name => name != "Checked" || element is not RibbonCheckBoxItem)
        .Select(name => TypeDescriptor.GetProperties(element)[name]).Where(property => property is { IsReadOnly: false })
        .Select(property => new RibbonDesignProperty { Name = property!.Name, Category = property.Category,
            Description = property.PropertyType == typeof(RibbonSizePolicy) ? "Command sizes in the expanded, intermediate and minimum group states." : property.Description,
            Type = property.PropertyType.Name, Value = property.PropertyType == typeof(string) ? (string?)property.GetValue(element) ?? "" : property.Converter.ConvertToInvariantString(property.GetValue(element)) ?? "",
            Options = property.PropertyType.IsEnum ? Enum.GetNames(property.PropertyType) : property.PropertyType == typeof(RibbonSizePolicy) ? SizePolicies.ToArray() : [] }).ToList();
    private static string Kind(RibbonElement element) => Kinds.FirstOrDefault(pair => pair.Value == element.GetType()).Key
        ?? throw new ArgumentException("Edit Ribbon currently supports built-in component types. Use the existing property editor for " + element.GetType().Name + ".");
    private static string Key(RibbonElement element) => element.Site?.Name is { Length: > 0 } name ? "site:" + name : "id:" + element.Id;
    private static string UniqueName(string type, HashSet<string> used)
    {
        string stem = char.ToLowerInvariant(type[0]) + type.Substring(1); int suffix = 1; string name;
        do { name = stem + suffix++; } while (!used.Add(name)); return name;
    }
    private static string Fingerprint(RibbonDesignSnapshot snapshot) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(new RibbonDesignSnapshot { Tabs = snapshot.Tabs }.Serialize())));
    private static IEnumerable<RibbonElement> Elements(IEnumerable<RibbonElement> elements)
    { foreach (var element in elements) { yield return element; foreach (var child in Elements(ChildElements(element))) yield return child; } }
    private static IEnumerable<RibbonElement> ChildElements(RibbonElement element) => element switch
    {
        RibbonTab tab => tab.Groups, RibbonGroup group => group.Items, RibbonDropDownItem drop => drop.Items,
        RibbonBackstage backstage => backstage.Items, RibbonCommandCatalog catalog => catalog.Commands,
        RibbonMessageBar messages => messages.Items, RibbonMessage message => [message.Action], _ => []
    };
    private static IEnumerable<RibbonElement> OutsideElements(RibbonControl ribbon) => Elements(new RibbonElement[] { ribbon.FileMenu, ribbon.Backstage, ribbon.CommandCatalog, ribbon.MessageBar }.Concat(ribbon.QuickAccessItems));
    private static IList? Collection(object owner) => owner switch { RibbonControl ribbon => ribbon.Tabs, RibbonTab tab => tab.Groups, RibbonGroup group => group.Items, RibbonDropDownItem drop => drop.Items, _ => null };
    private static string CollectionName(object owner) => owner is RibbonControl ? "Tabs" : owner is RibbonTab ? "Groups" : "Items";
}
