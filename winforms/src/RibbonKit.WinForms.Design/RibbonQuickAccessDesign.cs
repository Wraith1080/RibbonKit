using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design;

internal static partial class RibbonDesignCommitter
{
    internal static IEnumerable<RibbonElement> EditableElements(RibbonControl ribbon) => Elements(ribbon.Tabs)
        .Concat(Elements(ribbon.CommandCatalog.Commands)).Concat(ribbon.QuickAccessItems).Distinct();
    private static bool OwnedQuickAccess(RibbonElement entry) => entry is RibbonButtonItem;
    private static string SourceKind(RibbonElement source) => source switch
    {
        RibbonGroup => "Group", RibbonComboBoxItem => "ComboBox", RibbonGalleryItem => "Gallery", RibbonSplitButtonItem => "SplitButton",
        RibbonDropDownItem => "DropDown", RibbonRadioButtonItem => "RadioButton", RibbonCheckBoxItem => "CheckBox", RibbonToggleItem => "Toggle", RibbonButtonItem => "Button", _ => ""
    };
    private static Dictionary<string, RibbonElement> QuickAccessSources(RibbonControl ribbon) => ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.QuickAccess)
        .Select(entry => entry.Source).Concat(ribbon.QuickAccessItems.Select(RibbonQuickAccess.Source).OfType<RibbonElement>()).Distinct()
        .ToDictionary(Key, StringComparer.Ordinal);
    private static void ReadQuickAccess(RibbonControl ribbon, RibbonDesignSnapshot snapshot, Func<RibbonElement, RibbonDesignNode> map)
    {
        // Directly owned legacy QAT buttons also appear in the command shelf.
        // Their synthetic toolbar row keeps their original ownership until removed.
        snapshot.CatalogCommands = ribbon.CommandCatalog.Commands.Cast<RibbonElement>()
            .Concat(ribbon.QuickAccessItems.Where(OwnedQuickAccess)).Select(map).ToList();
        snapshot.QuickAccessItems = ribbon.QuickAccessItems.Select(entry =>
        {
            if (OwnedQuickAccess(entry)) return new RibbonDesignNode { Key = "owned:" + Key(entry), Kind = "OwnedQat", SourceKey = Key(entry) };
            var node = map(entry); node.SourceKey = RibbonQuickAccess.Source(entry) is { } source ? Key(source) : ""; return node;
        }).ToList();
        snapshot.QuickAccessSources = QuickAccessSources(ribbon).Select(pair => new RibbonDesignSource
        {
            Key = pair.Key, Kind = SourceKind(pair.Value), Caption = pair.Value.DisplayText,
            Category = pair.Value is RibbonGroup ? "Ribbon group" : pair.Value.Parent is RibbonCommandCatalog ? "Commands without a tab" : "Existing command"
        }).ToList();
    }
    internal static RibbonQuickAccessPosition ParseQuickAccessPosition(string value) => value switch
    {
        nameof(RibbonQuickAccessPosition.TabRow) => RibbonQuickAccessPosition.TabRow,
        nameof(RibbonQuickAccessPosition.BelowRibbon) => RibbonQuickAccessPosition.BelowRibbon,
        nameof(RibbonQuickAccessPosition.TitleBar) => RibbonQuickAccessPosition.TitleBar,
        _ => throw new ArgumentException("Choose TabRow, BelowRibbon or TitleBar for the Quick Access Toolbar.")
    };
    private static QuickAccessPlan PrepareQuickAccess(RibbonControl ribbon, IDesignerHost host, RibbonDesignSnapshot before, RibbonDesignSnapshot draft)
        => new(ribbon, host, before, draft);
    internal static void ValidateQuickAccessDraft(RibbonControl ribbon, IDesignerHost host, RibbonDesignSnapshot before, RibbonDesignSnapshot draft)
        => _ = PrepareQuickAccess(ribbon, host, before, draft);

    private sealed class QuickAccessPlan
    {
        private readonly RibbonControl _ribbon;
        private readonly IDesignerHost _host;
        private readonly RibbonDesignSnapshot _before, _draft;
        private readonly Dictionary<string, RibbonElement> _existing, _external;
        private readonly Dictionary<string, RibbonDesignNode> _originals;
        private readonly Type[] _controlTypes;
        private readonly Dictionary<string, RibbonSvgIcon> _icons = new(StringComparer.Ordinal);
        private readonly bool _changed;
        internal QuickAccessPlan(RibbonControl ribbon, IDesignerHost host, RibbonDesignSnapshot before, RibbonDesignSnapshot draft)
        {
            _ribbon = ribbon; _host = host; _before = before; _draft = draft;
            _controlTypes = RibbonDropDownContentTypes.Available(host, ribbon);
            _existing = Elements(ribbon.CommandCatalog.Commands).Concat(ribbon.QuickAccessItems).Distinct().ToDictionary(Key, StringComparer.Ordinal);
            _external = QuickAccessSources(ribbon);
            _originals = RibbonDesignSnapshot.Flatten(before.CatalogCommands).Concat(before.QuickAccessItems.Where(node => node.Kind != "OwnedQat")).ToDictionary(node => node.Key, StringComparer.Ordinal);
            _changed = State(before) != State(draft);
            var sourceKinds = draft.CommandNodes.ToDictionary(node => node.Key, node => node.Kind, StringComparer.Ordinal);
            foreach (var source in _external) sourceKinds.TryAdd(source.Key, SourceKind(source.Value));
            // A removed declaration must not re-enter through the live source list.
            foreach (var removed in before.CommandNodes.Where(node => !draft.CommandNodes.Any(current => current.Key == node.Key))) sourceKinds.Remove(removed.Key);
            foreach (var entry in draft.QuickAccessItems)
            {
                if (entry.Kind == "OwnedQat")
                {
                    if (!before.QuickAccessItems.Any(old => old.Kind == "OwnedQat" && old.Key == entry.Key && old.SourceKey == entry.SourceKey))
                        throw new ArgumentException("Preserve existing owned QAT entries; new commands use catalog references.");
                    continue;
                }
                if (entry.SourceKey.Length == 0)
                {
                    if (!before.QuickAccessItems.Any(old => old.Key == entry.Key && old.SourceKey.Length == 0)) throw new ArgumentException("Choose a source command for the new Quick Access entry.");
                }
                else if (!sourceKinds.TryGetValue(entry.SourceKey, out string? kind) || RibbonDesignSnapshot.QuickAccessKind(kind) != entry.Kind)
                    throw new ArgumentException("The Quick Access source is missing or incompatible with this entry.");
            }
            var auxiliary = RibbonDesignSnapshot.Flatten(draft.CatalogCommands).Concat(draft.QuickAccessItems.Where(node => node.Kind != "OwnedQat")).ToArray();
            foreach (var node in auxiliary)
            {
                if (_originals.TryGetValue(node.Key, out var original))
                {
                    if (node.Kind != original.Kind) throw new ArgumentException("Preserve existing QAT command/reference component types.");
                }
                else if (!node.Key.StartsWith("new:", StringComparison.Ordinal) || host.Container.Components[node.ComponentName] is not null)
                    throw new ArgumentException("New QAT components need unique names and identities.");
                if (!Kinds.ContainsKey(node.Kind)) throw new ArgumentException("Unsupported QAT command or reference type.");
                if (original is null && draft.CatalogCommands.Contains(node) && node.Kind is not ("Button" or "Toggle")) throw new ArgumentException("Create a button or toggle for a new QAT-only command.");
                var schema = before.Templates.Single(template => template.Kind == node.Kind);
                if (!schema.Properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal).SetEquals(node.Properties.Select(property => property.Name)))
                    throw new ArgumentException("Unsupported or incomplete QAT property set.");
                if (RibbonDesignSnapshot.IsQuickAccessReference(node.Kind) && (node.Icon.Kind != "None" || node.Icon.HasRasterImage || node.Choices.Count != 0))
                    throw new ArgumentException("A QAT reference borrows its source's icon and command data.");
                if (node.Icon.Kind == "Custom" && (original is null || !SameIcon(node.Icon, original.Icon))) throw new ArgumentException("Custom icons can only be preserved or explicitly replaced.");
                if (node.Icon.Kind is not ("None" or "Svg" or "Custom")) throw new ArgumentException("Unsupported QAT icon selection.");
                if (node.Icon.Kind == "Svg" && (original is null || !SameIcon(node.Icon, original.Icon))) _icons.Add(node.Key, new RibbonSvgIcon(node.Icon.Source, node.Icon.UseForegroundColor));
            }
            if (!_changed) return;
            using var probe = new RibbonControl();
            foreach (var node in draft.CatalogCommands) probe.CommandCatalog.Commands.Add((RibbonItem)BuildProbe(node, _originals, before.Templates, _controlTypes));
            foreach (var entry in draft.QuickAccessItems.Where(node => node.Kind != "OwnedQat"))
                using (BuildProbe(entry, _originals, before.Templates, _controlTypes)) { }
        }
        private static string State(RibbonDesignSnapshot value) => RibbonComponentNames.ContentState(new() { CatalogCommands = value.CatalogCommands, QuickAccessItems = value.QuickAccessItems });
        internal void Apply(Dictionary<string, RibbonElement> tabTargets, IComponentChangeService changes)
        {
            Set(_ribbon, nameof(_ribbon.QuickAccessVisible), _draft.QuickAccessVisible, changes);
            Set(_ribbon, nameof(_ribbon.QuickAccessPosition), ParseQuickAccessPosition(_draft.QuickAccessPosition), changes);
            if (!_changed) return;
            var nodes = RibbonDesignSnapshot.Flatten(_draft.CatalogCommands).Concat(_draft.QuickAccessItems.Where(node => node.Kind != "OwnedQat")).ToArray();
            var targets = new Dictionary<string, RibbonElement>(_existing, StringComparer.Ordinal);
            foreach (var node in nodes)
                if (!targets.ContainsKey(node.Key)) targets[node.Key] = (RibbonElement)_host.CreateComponent(Kinds[node.Kind], node.ComponentName);
                else if (targets[node.Key].Site is null) _host.Container.Add(targets[node.Key], node.ComponentName);
            var catalogProperty = TypeDescriptor.GetProperties(_ribbon)[nameof(_ribbon.CommandCatalog)]!;
            var commandsProperty = TypeDescriptor.GetProperties(_ribbon.CommandCatalog)[nameof(_ribbon.CommandCatalog.Commands)]!;
            var qatProperty = TypeDescriptor.GetProperties(_ribbon)[nameof(_ribbon.QuickAccessItems)]!;
            var collections = targets.Values.Where(element => Collection(element) is not null)
                .Select(element => (Owner: element, Collection: Collection(element)!, Property: TypeDescriptor.GetProperties(element)[CollectionName(element)]!)).ToArray();
            changes.OnComponentChanging(_ribbon, catalogProperty); changes.OnComponentChanging(_ribbon.CommandCatalog, commandsProperty); changes.OnComponentChanging(_ribbon, qatProperty);
            foreach (var collection in collections) changes.OnComponentChanging(collection.Owner, collection.Property);
            _ribbon.BeginUpdate();
            try
            {
                var gallerySelections = targets.Values.OfType<RibbonGalleryItem>().ToDictionary(gallery => gallery, gallery => gallery.SelectedItem);
                _ribbon.QuickAccessItems.Clear(); _ribbon.CommandCatalog.Commands.Clear();
                foreach (var collection in collections) collection.Collection.Clear();
                foreach (var node in nodes)
                {
                    var target = targets[node.Key]; _originals.TryGetValue(node.Key, out var original);
                    if (original is null ? node.Icon.Kind != "None" : !SameIcon(node.Icon, original.Icon))
                        Set(target, "Icon", node.Icon.Kind == "Svg" ? _icons[node.Key] : null, changes);
                    bool choicesChanged = target is RibbonComboBoxItem combo && !combo.Items.SequenceEqual(node.Choices);
                    if (choicesChanged && target is RibbonComboBoxItem choice)
                    {
                        var property = TypeDescriptor.GetProperties(choice)[nameof(choice.Items)]!;
                        changes.OnComponentChanging(choice, property); choice.Items.Clear(); choice.Items.AddRange(node.Choices.ToArray()); changes.OnComponentChanged(choice, property, null, null);
                    }
                    foreach (var value in node.Properties)
                    {
                        if (target is RibbonComboBoxItem && value.Name is "Value" or "SelectedIndex") continue;
                        if (original?.Properties.Single(property => property.Name == value.Name).Value == value.Value) continue;
                        Set(target, value.Name, ConvertValue(CheckedProperty(target, value.Name), value.Value, _controlTypes), changes);
                    }
                    if (target is RibbonComboBoxItem selection && SelectionProperty(selection, node, original ?? _before.Templates.Single(template => template.Kind == node.Kind), choicesChanged) is { } selected)
                        Set(target, selected.Name, ConvertValue(CheckedProperty(target, selected.Name), selected.Value), changes);
                }
                void Fill(RibbonDesignNode node)
                {
                    if (Collection(targets[node.Key]) is not { } children) return;
                    foreach (var child in node.Children) { Fill(child); children.Add(targets[child.Key]); }
                }
                var owned = _draft.QuickAccessItems.Where(node => node.Kind == "OwnedQat").Select(node => node.SourceKey).ToHashSet(StringComparer.Ordinal);
                foreach (var node in _draft.CatalogCommands)
                {
                    Fill(node);
                    if (!owned.Contains(node.Key)) _ribbon.CommandCatalog.Commands.Add((RibbonItem)targets[node.Key]);
                }
                var sources = new Dictionary<string, RibbonElement>(_external, StringComparer.Ordinal);
                foreach (var pair in tabTargets) sources[pair.Key] = pair.Value;
                foreach (var pair in targets) sources[pair.Key] = pair.Value;
                foreach (var entry in _draft.QuickAccessItems)
                {
                    if (entry.Kind == "OwnedQat") _ribbon.QuickAccessItems.Add(targets[entry.SourceKey]);
                    else
                    {
                        var model = targets[entry.Key]; Set(model, "Source", entry.SourceKey.Length == 0 ? null : sources[entry.SourceKey], changes); _ribbon.QuickAccessItems.Add(model);
                    }
                }
                foreach (var selected in gallerySelections)
                    if (selected.Value is not null && selected.Key.Items.Contains(selected.Value)) selected.Key.SelectedItem = selected.Value;
                foreach (var collection in collections) changes.OnComponentChanged(collection.Owner, collection.Property, null, null);
                changes.OnComponentChanged(_ribbon.CommandCatalog, commandsProperty, null, null); changes.OnComponentChanged(_ribbon, catalogProperty, null, null); changes.OnComponentChanged(_ribbon, qatProperty, null, null);
                var retained = nodes.Select(node => node.Key).ToHashSet(StringComparer.Ordinal);
                foreach (var removed in _existing.Where(pair => !retained.Contains(pair.Key)).Reverse()) _host.DestroyComponent(removed.Value);
            }
            finally { _ribbon.EndUpdate(); }
        }
        private static void Set(object owner, string name, object? value, IComponentChangeService changes)
        {
            var property = TypeDescriptor.GetProperties(owner)[name] ?? throw new ArgumentException("Unsupported QAT property: " + name);
            var old = property.GetValue(owner); if (Equals(old, value)) return;
            changes.OnComponentChanging(owner, property); property.SetValue(owner, value); changes.OnComponentChanged(owner, property, old, value);
        }
    }
}
