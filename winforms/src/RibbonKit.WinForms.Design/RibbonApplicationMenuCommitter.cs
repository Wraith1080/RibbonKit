using System.ComponentModel;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design;

internal static partial class RibbonDesignCommitter
{
    private static readonly string[] ApplicationSections = ["file", "default", "footer"];
    private static RibbonDropDownItem ApplicationSource(RibbonControl ribbon, string key) => key == "file" ? ribbon.FileMenu : ribbon.ApplicationMenu.DefaultPane;
    private static RibbonItemCollection ApplicationCollection(RibbonControl ribbon, string key) => key == "footer" ? ribbon.ApplicationMenu.FooterItems : ApplicationSource(ribbon, key).Items;
    private static IEnumerable<RibbonDesignNode> ApplicationNodes(RibbonBackstageSnapshot draft) => RibbonDesignSnapshot.Flatten(draft.Entries.SelectMany(n => n.Children));
    private static IEnumerable<RibbonElement> ApplicationElements(RibbonControl ribbon) => ribbon.FileMenu.Items.SelectMany(i => i.Tree())
        .Concat(ribbon.ApplicationMenu.DefaultPane.Items.SelectMany(i => i.Tree())).Concat(ribbon.ApplicationMenu.FooterItems.SelectMany(i => i.Tree()));
    private static string ApplicationFingerprint(RibbonBackstageSnapshot draft) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        new RibbonBackstageSnapshot { Settings = draft.Settings, Entries = draft.Entries, ContentRevision = draft.ContentRevision }.Serialize())));
    internal static RibbonBackstageSnapshot ReadApplicationMenu(RibbonControl ribbon, IDesignerHost host)
    {
        EnsureNestedDesignModels(ribbon);
        var elements = ApplicationElements(ribbon).ToArray();
        var types = RibbonDropDownContentTypes.Available(host, ribbon);
        var names = host.Container.Components.Cast<IComponent>().Select(c => c.Site?.Name ?? "").ToHashSet(StringComparer.OrdinalIgnoreCase);
        RibbonDesignNode Map(RibbonElement item) => new() { Key = Key(item), ComponentName = item.Site?.Name ?? UniqueName(item.GetType().Name, names), Kind = Kind(item),
            Properties = Properties(item, types), Icon = ReadIcon(item), Choices = item is RibbonComboBoxItem combo ? combo.Items.ToList() : [], Children = ChildElements(item).Select(Map).ToList() };
        var snapshot = new RibbonBackstageSnapshot
        {
            ReservedNames = host.Container.Components.Cast<IComponent>().Where(c => !elements.Contains(c)).Select(c => c.Site?.Name ?? "").Where(n => n.Length > 0).ToList(),
            ReservedIds = ribbon.AllElements.Except(elements).Select(e => e.Id).ToList(),
            ContentRevision = string.Join("|", elements.Append(ribbon.FileMenu).Append(ribbon.ApplicationMenu.DefaultPane).OfType<RibbonDropDownItem>()
                .Select(d => Key(d) + ":" + (d.Content is { } body ? RuntimeHelpers.GetHashCode(body) : 0))),
            SelectionKey = (host.GetService(typeof(ISelectionService)) as ISelectionService)?.PrimarySelection is RibbonElement selected && elements.Contains(selected) ? Key(selected) : ""
        };
        foreach (string name in new[] { "FileMenuMode", "FileVisible" })
        {
            object owner = name == "FileVisible" ? ribbon.FileMenu : ribbon;
            var property = TypeDescriptor.GetProperties(owner)[name == "FileVisible" ? "Visible" : name]!;
            snapshot.Settings.Add(new() { Name = name, Type = property.PropertyType.Name, Category = "File", Value = property.Converter.ConvertToInvariantString(property.GetValue(owner)) ?? "",
                Options = property.PropertyType.IsEnum ? Enum.GetNames(property.PropertyType) : [] });
        }
        foreach (string key in ApplicationSections)
        {
            var root = new RibbonDesignNode { Key = key, Kind = "Section", ComponentName = key,
                Children = ApplicationCollection(ribbon, key).Select(Map).ToList() };
            if (key != "footer") root.Properties = Properties(ApplicationSource(ribbon, key), types)
                .Where(p => p.Name is "DropDownHeader" or "ContentType" || key == "file" && p.Name is "Text" or "KeyTip").ToList();
            snapshot.Entries.Add(root);
            if (key != "footer" && ApplicationSource(ribbon, key).Content is { } body) snapshot.ContentNames[key] = body.GetType().Name;
        }
        foreach (var menu in elements.OfType<RibbonDropDownItem>().Where(d => d.Content is not null)) snapshot.ContentNames[Key(menu)] = menu.Content!.GetType().Name;
        foreach (var kind in Kinds.Where(pair => RibbonDesignSnapshot.ChildKinds("DropDown").Contains(pair.Key) || pair.Key == "GalleryTile"))
        { using var item = (RibbonElement)Activator.CreateInstance(kind.Value)!; snapshot.Templates.Add(new() { Kind = kind.Key, Properties = Properties(item, types) }); }
        snapshot.Revision = ApplicationFingerprint(snapshot);
        return snapshot;
    }
    private static void ValidateApplicationDraft(RibbonBackstageSnapshot before, RibbonBackstageSnapshot draft)
    {
        if (draft.Revision != before.Revision || draft.ContentRevision != before.ContentRevision) throw new ArgumentException("Application Menu changed. Cancel and reopen its editor.");
        if (!draft.Entries.Select(n => n.Key).SequenceEqual(ApplicationSections) || draft.Entries.Any(n => n.Kind != "Section")) throw new ArgumentException("Preserve the three Application Menu sections.");
        if (draft.Settings.Count != before.Settings.Count || !draft.Settings.Select(p => p.Name).ToHashSet().SetEquals(before.Settings.Select(p => p.Name))) throw new ArgumentException("Unsupported File settings.");
        var originals = ApplicationNodes(before).ToDictionary(n => n.Key, StringComparer.Ordinal);
        var keys = new HashSet<string>(StringComparer.Ordinal); var names = before.ReservedNames.ToHashSet(StringComparer.OrdinalIgnoreCase); var ids = before.ReservedIds.ToHashSet(StringComparer.Ordinal);
        void Schema(List<RibbonDesignProperty> values, List<RibbonDesignProperty> schema)
        { if (values.Count != schema.Count || !values.Select(p => p.Name).ToHashSet().SetEquals(schema.Select(p => p.Name))) throw new ArgumentException("Unsupported or incomplete Application Menu properties."); }
        void Children(IEnumerable<RibbonDesignNode> nodes, string parent)
        {
            foreach (var node in nodes)
            {
                if (!RibbonDesignSnapshot.ChildKinds(parent).Contains(node.Kind) || !keys.Add(node.Key) || string.IsNullOrWhiteSpace(node.Key)
                    || string.IsNullOrWhiteSpace(node.ComponentName) || !names.Add(node.ComponentName)) throw new ArgumentException("Commands need valid types, unique keys and component names.");
                originals.TryGetValue(node.Key, out var original);
                if (original is not null ? original.Kind != node.Kind : !node.Key.StartsWith("new:", StringComparison.Ordinal)) throw new ArgumentException("Preserve existing command identities and types.");
                Schema(node.Properties, before.Templates.Single(t => t.Kind == node.Kind).Properties);
                string id = node.Properties.Single(p => p.Name == "Id").Value;
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new ArgumentException("Commands need unique nonempty IDs.");
                if (node.Choices.Count != 0 && node.Kind != "ComboBox" || node.SourceKey.Length != 0) throw new ArgumentException("Unsupported command data.");
                if (node.Icon.Kind is not ("None" or "Svg" or "Custom") || node.Icon.Kind == "Custom" && (original is null || !SameIcon(original.Icon, node.Icon))) throw new ArgumentException("Custom icons may only be preserved.");
                Children(node.Children, node.Kind);
            }
        }
        for (int i = 0; i < 3; i++) { Schema(draft.Entries[i].Properties, before.Entries[i].Properties); Children(draft.Entries[i].Children, "DropDown"); }
    }
    internal static RibbonControl ProbeApplicationMenu(RibbonControl ribbon, IDesignerHost host, RibbonBackstageSnapshot draft, bool artwork)
    {
        var before = ReadApplicationMenu(ribbon, host); ValidateApplicationDraft(before, draft);
        draft.Templates = before.Templates;
        var originals = ApplicationNodes(before).ToDictionary(n => n.Key, StringComparer.Ordinal);
        var real = ApplicationElements(ribbon).ToDictionary(Key, StringComparer.Ordinal);
        var types = RibbonDropDownContentTypes.Available(host, ribbon);
        var probe = new RibbonControl { Site = new ApplicationPreviewSite(), Font = ribbon.Font, Density = ribbon.Density, Theme = ribbon.Theme };
        try
        {
            foreach (var section in draft.Entries)
            {
                if (section.Key != "footer")
                    foreach (var value in section.Properties)
                    {
                        var owner = ApplicationSource(probe, section.Key); var source = ApplicationSource(ribbon, section.Key);
                        if (value.Name == "ContentType" && source.ContentType is null && source.Content is not null && RibbonDropDownContentTypes.Resolve(value.Value, types) is not null) throw new ArgumentException("Clear borrowed Content before selecting ContentType.");
                        var property = CheckedProperty(owner, value.Name); property.SetValue(owner, ConvertValue(property, value.Value, types));
                    }
                foreach (var node in section.Children) ApplicationCollection(probe, section.Key).Add((RibbonItem)BuildProbe(node, originals, draft.Templates, types));
            }
            var prepared = ApplicationElements(probe).ToDictionary(e => e.Id);
            foreach (var node in ApplicationNodes(draft))
            {
                var item = prepared[node.Properties.Single(p => p.Name == "Id").Value]; real.TryGetValue(node.Key, out var source);
                if (item is RibbonDropDownItem { ContentType: not null } && source is RibbonDropDownItem { ContentType: null, Content: not null }) throw new ArgumentException("Clear borrowed Content before selecting ContentType.");
                if (node.Icon.Kind == "Svg") ((RibbonItem)item).Icon = new RibbonSvgIcon(node.Icon.Source, node.Icon.UseForegroundColor);
                if (artwork && source is RibbonItem original && item is RibbonItem target)
                { if (node.Icon.Kind == "Custom") target.Icon = original.Icon; target.Image = original.Image; target.LargeImage = original.LargeImage; }
            }
            foreach (var value in draft.Settings)
            {
                object owner = value.Name == "FileVisible" ? probe.FileMenu : probe;
                var property = TypeDescriptor.GetProperties(owner)[value.Name == "FileVisible" ? "Visible" : value.Name]!;
                property.SetValue(owner, property.Converter.ConvertFromInvariantString(value.Value));
            }
            return probe;
        }
        catch { probe.Dispose(); throw; }
    }
    internal static bool ApplyApplicationMenu(RibbonControl ribbon, IDesignerHost host, RibbonBackstageSnapshot draft)
    {
        var before = ReadApplicationMenu(ribbon, host); ValidateApplicationDraft(before, draft);
        using var probe = ProbeApplicationMenu(ribbon, host, draft, false);
        if (ApplicationFingerprint(draft) == before.Revision) return false;
        var nodes = ApplicationNodes(draft).ToArray();
        var originals = ApplicationNodes(before).ToDictionary(n => n.Key, StringComparer.Ordinal);
        var existing = ApplicationElements(ribbon).ToDictionary(Key, StringComparer.Ordinal);
        var names = new RibbonComponentNames(host, nodes.Select(n => ((IComponent?)existing.GetValueOrDefault(n.Key), n.ComponentName)));
        var prepared = ApplicationElements(probe).ToDictionary(e => e.Id);
        var types = RibbonDropDownContentTypes.Available(host, ribbon);
        foreach (var reference in ribbon.QuickAccessItems)
        {
            RibbonElement? source = reference switch { RibbonQuickAccessReference r => r.Source, RibbonQuickAccessDropDownReference r => r.Source, RibbonQuickAccessComboBoxReference r => r.Source, _ => null };
            if (source is not null && existing.ContainsValue(source) && !nodes.Any(n => n.Key == Key(source))) throw new ArgumentException("Remove the Quick Access reference before removing its source.");
        }
        var changes = host.GetService(typeof(IComponentChangeService)) as IComponentChangeService ?? throw new InvalidOperationException("Designer change service unavailable.");
        using var transaction = host.CreateTransaction("Edit Application Menu"); bool initializing = false;
        try
        {
            var target = new Dictionary<string, RibbonElement>(existing, StringComparer.Ordinal);
            foreach (var node in nodes)
            {
                if (!target.TryGetValue(node.Key, out var item)) target[node.Key] = item = (RibbonElement)host.CreateComponent(Kinds[node.Kind], node.ComponentName);
                else if (item.Site is null) host.Container.Add(item, node.ComponentName);
            }
            var roots = new[] { TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.FileMenu)]!, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.ApplicationMenu)]! };
            string Shape(IEnumerable<RibbonDesignNode> values) => string.Join("|", values.Select(n => n.Key + "[" + Shape(n.Children) + "]"));
            bool structural = !before.Entries.Select(n => Shape(n.Children)).SequenceEqual(draft.Entries.Select(n => Shape(n.Children)));
            // Capture nested scalars before their aggregate ownership paths. An
            // absolute parent snapshot can omit a child's default-valued member.
            foreach (var node in nodes)
                foreach (var value in node.Properties)
                    if (originals.GetValueOrDefault(node.Key)?.Properties.Single(p => p.Name == value.Name).Value != value.Value)
                        changes.OnComponentChanging(target[node.Key], CheckedProperty(target[node.Key], value.Name));
            for (int i = 0; i < 2; i++)
                foreach (var value in draft.Entries[i].Properties)
                    if (before.Entries[i].Properties.Single(p => p.Name == value.Name).Value != value.Value)
                        changes.OnComponentChanging(ApplicationSource(ribbon, draft.Entries[i].Key), CheckedProperty(ApplicationSource(ribbon, draft.Entries[i].Key), value.Name));
            if (structural) foreach (var root in roots) changes.OnComponentChanging(ribbon, root);
            var collections = ApplicationSections.Select(k => (Owner: (object)(k == "footer" ? (RibbonElement)ribbon.ApplicationMenu : ApplicationSource(ribbon, k)), Name: k == "footer" ? "FooterItems" : "Items", Items: (System.Collections.IList)ApplicationCollection(ribbon, k)))
                .Concat(target.Values.OfType<RibbonDropDownItem>().Select(d => (Owner: (object)d, Name: "Items", Items: (System.Collections.IList)d.Items))).ToArray();
            if (structural) foreach (var collection in collections) changes.OnComponentChanging(collection.Owner, TypeDescriptor.GetProperties(collection.Owner)[collection.Name]!);
            ribbon.BeginInit(); initializing = true;
            if (structural) foreach (var collection in collections) collection.Items.Clear();
            void Set(object owner, PropertyDescriptor property, object? value)
            {
                var old = property.GetValue(owner); if (Equals(old, value)) return;
                changes.OnComponentChanging(owner, property); property.SetValue(owner, value); changes.OnComponentChanged(owner, property, old, value);
            }
            void Scalars(RibbonElement owner, IEnumerable<RibbonDesignProperty> properties, RibbonDesignNode? baseline)
            {
                foreach (var value in properties)
                {
                    if (baseline?.Properties.Single(p => p.Name == value.Name).Value == value.Value) continue;
                    var property = CheckedProperty(owner, value.Name); Set(owner, property, ConvertValue(property, value.Value, types));
                }
            }
            foreach (var node in nodes)
            {
                var item = target[node.Key]; originals.TryGetValue(node.Key, out var baseline);
                if (item is RibbonComboBoxItem combo && !combo.Items.SequenceEqual(node.Choices))
                { var property = TypeDescriptor.GetProperties(combo)["Items"]!; changes.OnComponentChanging(combo, property); combo.Items.Clear(); combo.Items.AddRange(node.Choices.ToArray()); changes.OnComponentChanged(combo, property, null, null); }
                Scalars(item, node.Properties.Where(p => item is not RibbonComboBoxItem || p.Name is not ("Value" or "SelectedIndex")), baseline);
                if (item is RibbonComboBoxItem selection && SelectionProperty(selection, node, baseline ?? draft.Templates.Single(t => t.Kind == node.Kind), baseline is null || !baseline.Choices.SequenceEqual(node.Choices)) is { } value) Scalars(item, [value], null);
                if (baseline is null ? node.Icon.Kind != "None" : !SameIcon(baseline.Icon, node.Icon)) Set(item, TypeDescriptor.GetProperties(item)["Icon"]!, node.Icon.Kind == "Svg" ? ((RibbonItem)prepared[item.Id]).Icon : null);
                if (structural && item is RibbonDropDownItem dropdown) foreach (var child in node.Children) dropdown.Items.Add((RibbonItem)target[child.Key]);
            }
            for (int i = 0; i < 3; i++)
            {
                var section = draft.Entries[i];
                if (section.Key != "footer") Scalars(ApplicationSource(ribbon, section.Key), section.Properties, before.Entries[i]);
                if (structural) foreach (var node in section.Children) ApplicationCollection(ribbon, section.Key).Add((RibbonItem)target[node.Key]);
            }
            foreach (var setting in draft.Settings)
            {
                object owner = setting.Name == "FileVisible" ? ribbon.FileMenu : ribbon;
                var property = TypeDescriptor.GetProperties(owner)[setting.Name == "FileVisible" ? "Visible" : setting.Name]!;
                Set(owner, property, property.Converter.ConvertFromInvariantString(setting.Value));
            }
            if (structural) foreach (var collection in collections) changes.OnComponentChanged(collection.Owner, TypeDescriptor.GetProperties(collection.Owner)[collection.Name]!, null, null);
            if (structural) foreach (var root in roots) changes.OnComponentChanged(ribbon, root, null, null);
            foreach (var removed in existing.Where(p => !nodes.Any(n => n.Key == p.Key)).Reverse()) host.DestroyComponent(removed.Value);
            initializing = false; ribbon.EndInit(); names.Apply(changes); transaction.Commit(); return true;
        }
        catch { transaction.Cancel(); throw; }
        finally { if (initializing) ribbon.EndInit(); }
    }
    private sealed class ApplicationPreviewSite : ISite
    {
        public IComponent Component => null!;
        public IContainer? Container => null;
        public bool DesignMode => true;
        public string? Name { get; set; }
        public object? GetService(Type serviceType) => null;
    }
    internal static RibbonBackstagePreview PreviewApplicationMenu(RibbonControl ribbon, IDesignerHost host, RibbonBackstageSnapshot draft)
    {
        using var probe = ProbeApplicationMenu(ribbon, host, draft, true);
        var preparedMenus = ApplicationElements(probe).OfType<RibbonDropDownItem>().ToDictionary(d => d.Id);
        foreach (var section in draft.Entries.Where(s => s.Key != "footer"))
            if (draft.ContentNames.ContainsKey(section.Key) && ApplicationSource(probe, section.Key).ContentType is null) ApplicationSource(probe, section.Key).ContentType = typeof(UserControl);
        foreach (var node in ApplicationNodes(draft))
            if (draft.ContentNames.ContainsKey(node.Key) && preparedMenus.TryGetValue(node.Properties.Single(p => p.Name == "Id").Value, out var bodyMenu) && bodyMenu.ContentType is null) bodyMenu.ContentType = typeof(UserControl);
        if (probe.FileMenu.ContentType is not null)
        {
            var body = RibbonPopupPreviewRenderer.Render(probe, probe.FileMenu, "file", "", new Dictionary<RibbonElement, string>(), true);
            return new() { Png = body.Png, Regions = body.Hits.Select(h => new RibbonBackstagePreviewRegion { Key = "file", X = h.X, Y = h.Y, Width = h.Width, Height = h.Height }).ToList() };
        }
        using var surface = new RibbonPopupSurface(probe, probe.FileMenu, new(Math.Clamp(draft.PreviewWidth, 320, 1600), Math.Clamp(draft.PreviewHeight, 260, 1200)), probe.FileMenu);
        var nodes = ApplicationNodes(draft).ToDictionary(n => n.Properties.Single(p => p.Name == "Id").Value);
        var left = draft.Entries[0].Children.FirstOrDefault(n => n.Key == draft.SelectionKey || RibbonDesignSnapshot.Flatten(n.Children).Any(c => c.Key == draft.SelectionKey));
        if (left is not null && probe.FileMenu.Items.FirstOrDefault(i => i.Id == left.Properties.Single(p => p.Name == "Id").Value) is RibbonDropDownItem pane and not RibbonGalleryItem) surface.PreviewApplicationPane(pane);
        using var pixels = new Bitmap(surface.Width, surface.Height, PixelFormat.Format24bppRgb);
        surface.DrawToBitmap(pixels, surface.ClientRectangle);
        var result = new RibbonBackstagePreview();
        foreach (var target in surface.Targets.Where(t => !t.VisibleBounds.IsEmpty && t.Element is not null))
        {
            if (!nodes.TryGetValue(target.Element!.Id, out var node)) continue;
            var bounds = target.VisibleBounds;
            result.Regions.Add(new() { Key = node.Key, X = bounds.X, Y = bounds.Y, Width = bounds.Width, Height = bounds.Height });
        }
        if (surface.ApplicationLayout is { } layout) result.Regions.Add(new() { Key = left?.Key ?? "default", X = layout.Pane.X, Y = layout.Pane.Y, Width = layout.Pane.Width, Height = layout.Pane.Height });
        using var stream = new MemoryStream(); pixels.Save(stream, ImageFormat.Png); result.Png = Convert.ToBase64String(stream.ToArray()); return result;
    }
}
