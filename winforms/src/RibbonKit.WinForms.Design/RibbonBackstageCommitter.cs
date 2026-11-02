using System.ComponentModel;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design;

internal static class RibbonBackstageCommitter
{
    private static readonly string[] EntryProperties = ["Id", "Text", "KeyTip", "Enabled", "Visible", "MenuDescription", "ScreenTipTitle", "ScreenTipDescription"];
    private static readonly string[] Settings = ["FileMenuMode", "FileVisible", "NavigationWidth", "MinimumNavigationWidth", "MaximumNavigationWidth", "NavigationBackgroundOpacity"];
    private static (object Owner, PropertyDescriptor Property) Setting(RibbonControl ribbon, string name)
    {
        object owner = name == "FileMenuMode" ? ribbon : name == "FileVisible" ? ribbon.FileMenu : ribbon.Backstage;
        return (owner, TypeDescriptor.GetProperties(owner)[name == "FileVisible" ? "Visible" : name] ?? throw new ArgumentException("Unsupported Backstage setting."));
    }
    internal static RibbonBackstageSnapshot Read(RibbonControl ribbon, IDesignerHost host)
    {
        var entries = ribbon.Backstage.NavigationItems.ToArray();
        var contentTypes = RibbonBackstageContentTypes.Available(host, ribbon);
        var used = host.Container.Components.Cast<IComponent>().Select(component => component.Site?.Name ?? "").ToHashSet(StringComparer.OrdinalIgnoreCase);
        string Name(RibbonItem item)
        {
            if (item.Site?.Name is { Length: > 0 } existing) return existing;
            string stem = item is RibbonBackstagePageItem ? "backstagePage" : item is RibbonMenuSeparatorItem ? "backstageSeparator" : "backstageCommand", name; int suffix = 1;
            do { name = stem + suffix++; } while (!used.Add(name)); return name;
        }
        var snapshot = new RibbonBackstageSnapshot
        {
            ReservedNames = host.Container.Components.Cast<IComponent>().Where(component => !entries.Contains(component)).Select(component => component.Site?.Name ?? "").Where(name => name.Length != 0).ToList(),
            ReservedIds = ribbon.AllElements.Except(entries).Select(element => element.Id).Distinct().ToList(),
            ContentRevision = string.Join("|", entries.OfType<RibbonBackstagePageItem>().Select(page => RibbonDesignCommitter.Key(page) + ":" + (page.Content is { } content ? RuntimeHelpers.GetHashCode(content) : 0))),
            SelectionKey = (host.GetService(typeof(ISelectionService)) as ISelectionService)?.PrimarySelection is RibbonItem selected && entries.Contains(selected) ? RibbonDesignCommitter.Key(selected) : ""
        };
        snapshot.Settings = Settings.Select(name =>
        {
            var (owner, property) = Setting(ribbon, name);
            return new RibbonDesignProperty { Name = name, Category = name.StartsWith("File") ? "File entry" : property.Category,
                Description = name == "FileVisible" ? "Show the File entry. Choose Backstage as FileMenuMode to open these pages at runtime." : property.Description,
                Type = property.PropertyType.Name, Value = property.Converter.ConvertToInvariantString(property.GetValue(owner)) ?? "",
                Options = property.PropertyType.IsEnum ? Enum.GetNames(property.PropertyType) : [] };
        }).ToList();
        foreach (var item in entries)
        {
            string kind = item is RibbonBackstagePageItem ? "Page" : item is RibbonMenuSeparatorItem ? "Separator" : item.GetType() == typeof(RibbonButtonItem) ? "Button" : throw new ArgumentException("Edit Backstage supports native pages, buttons and separators.");
            var node = new RibbonDesignNode { Key = RibbonDesignCommitter.Key(item), ComponentName = Name(item), Kind = kind,
                Properties = Properties(item, contentTypes, ribbon.Backstage.FooterItems.Contains(item)), Icon = RibbonDesignCommitter.ReadIcon(item) };
            snapshot.Entries.Add(node);
            if (item is RibbonBackstagePageItem page)
                snapshot.ContentNames[node.Key] = page.Content is { } content ? (content.Site?.Name ?? content.Name) + " (" + content.GetType().FullName + ")" : "No content assigned";
        }
        foreach (string kind in new[] { "Page", "Button", "Separator" })
        {
            using var item = Create(kind); snapshot.Templates.Add(new() { Kind = kind, Properties = Properties(item, contentTypes) });
        }
        snapshot.Revision = Fingerprint(snapshot); return snapshot;
    }
    private static List<RibbonDesignProperty> Properties(RibbonItem item, Type[] contentTypes, bool footer = false)
    {
        var wanted = item is RibbonMenuSeparatorItem ? new[] { "Id", "Visible" } : EntryProperties;
        var properties = RibbonDesignCommitter.Properties(item).Where(property => wanted.Contains(property.Name)).ToList();
        properties.Add(new() { Name = "Placement", Category = "Navigation", Type = "String", Value = footer ? "Bottom" : "Main", Options = ["Main", "Bottom"],
            Description = "Place this entry in the main navigation or the bottom-aligned section. Order is preserved within each section." });
        if (item is RibbonBackstagePageItem page)
            properties.Add(new() { Name = nameof(page.ContentType), Category = "Page content", Type = "String",
                Description = "Choose a UserControl class; build your project to refresh this list. It is created during runtime form initialization and owned by this page. Existing application-owned Content must be cleared first.",
                Value = RibbonBackstageContentTypes.Label(page.ContentType),
                Options = new[] { RibbonBackstageContentTypes.None }.Concat(contentTypes.Select(RibbonBackstageContentTypes.Label)).ToArray() });
        return properties;
    }
    private static Type ItemType(string kind) => kind switch { "Page" => typeof(RibbonBackstagePageItem), "Button" => typeof(RibbonButtonItem), "Separator" => typeof(RibbonMenuSeparatorItem), _ => throw new ArgumentException("Backstage entries must be pages, commands or separators.") };
    private static RibbonItem Create(string kind) => (RibbonItem)Activator.CreateInstance(ItemType(kind))!;
    internal static bool IsFooter(RibbonDesignNode node) => node.Properties.Single(property => property.Name == "Placement").Value == "Bottom";
    private static string Fingerprint(RibbonBackstageSnapshot snapshot) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        new RibbonBackstageSnapshot { Settings = snapshot.Settings, Entries = snapshot.Entries, ContentRevision = snapshot.ContentRevision }.Serialize())));
    private static void Validate(RibbonBackstageSnapshot before, RibbonBackstageSnapshot draft)
    {
        if (draft.Revision != before.Revision || draft.ContentRevision != before.ContentRevision) throw new ArgumentException("Backstage changed while the editor was open. Cancel and reopen Edit Backstage.");
        if (!Settings.ToHashSet(StringComparer.Ordinal).SetEquals(draft.Settings.Select(property => property.Name)) || draft.Settings.Count != Settings.Length)
            throw new ArgumentException("Unsupported or incomplete Backstage settings.");
        var originals = before.Entries.ToDictionary(node => node.Key, StringComparer.Ordinal);
        var keys = new HashSet<string>(StringComparer.Ordinal); var names = before.ReservedNames.ToHashSet(StringComparer.OrdinalIgnoreCase); var ids = before.ReservedIds.ToHashSet(StringComparer.Ordinal);
        foreach (var node in draft.Entries)
        {
            if (string.IsNullOrWhiteSpace(node.Key) || !keys.Add(node.Key) || !names.Add(node.ComponentName) || string.IsNullOrWhiteSpace(node.ComponentName)) throw new ArgumentException("Backstage entries need unique component keys and names.");
            if (originals.TryGetValue(node.Key, out var original))
            { if (node.Kind != original.Kind) throw new ArgumentException("Preserve existing component types."); }
            else if (!node.Key.StartsWith("new:", StringComparison.Ordinal)) throw new ArgumentException("Unsupported new component identity.");
            if (node.Kind is not ("Page" or "Button" or "Separator") || node.Children.Count != 0 || node.Choices.Count != 0) throw new ArgumentException("Backstage entries must be pages, buttons or separators without children.");
            var schema = before.Templates.Single(template => template.Kind == node.Kind).Properties;
            if (node.Properties.Count != schema.Count || !schema.Select(property => property.Name).ToHashSet(StringComparer.Ordinal).SetEquals(node.Properties.Select(property => property.Name))) throw new ArgumentException("Unsupported or incomplete Backstage entry properties.");
            if (node.Properties.Single(property => property.Name == "Placement").Value is not ("Main" or "Bottom")) throw new ArgumentException("Choose Main or Bottom placement.");
            string id = node.Properties.Single(property => property.Name == "Id").Value;
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new ArgumentException("Backstage entries need nonempty, unique IDs.");
            if (node.Icon.Kind is not ("None" or "Svg" or "Custom") || node.Icon.Kind == "Custom" && (original is null || !RibbonDesignCommitter.SameIcon(original.Icon, node.Icon))) throw new ArgumentException("Custom artwork can only be preserved or explicitly replaced.");
            if (node.Kind == "Separator" && node.Icon.Kind != "None" && (original is null || !RibbonDesignCommitter.SameIcon(original.Icon, node.Icon))) throw new ArgumentException("Separators are decorative and do not accept new artwork.");
        }
    }
    internal static RibbonControl Probe(RibbonControl ribbon, IDesignerHost host, RibbonBackstageSnapshot draft, bool artwork)
    {
        var before = Read(ribbon, host); Validate(before, draft);
        var originals = ribbon.Backstage.NavigationItems.ToDictionary(RibbonDesignCommitter.Key, StringComparer.Ordinal);
        var contentTypes = RibbonBackstageContentTypes.Available(host, ribbon);
        var probe = new RibbonControl { Theme = ribbon.Theme, Density = ribbon.Density };
        try
        {
            foreach (var value in draft.Settings) { var (owner, property) = Setting(probe, value.Name); property.SetValue(owner, property.Converter.ConvertFromInvariantString(value.Value)); }
            foreach (var node in draft.Entries)
            {
                var item = Create(node.Kind);
                try
                {
                    foreach (var value in node.Properties)
                    {
                        if (value.Name == "Placement") continue;
                        var property = TypeDescriptor.GetProperties(item)[value.Name]!;
                        var converted = value.Name == nameof(RibbonBackstagePageItem.ContentType) ? RibbonBackstageContentTypes.Resolve(value.Value, contentTypes)
                            : property.PropertyType == typeof(string) ? value.Value : property.Converter.ConvertFromInvariantString(value.Value);
                        if (value.Name == nameof(RibbonBackstagePageItem.ContentType) && converted is not null
                            && originals.TryGetValue(node.Key, out var originalPage) && originalPage is RibbonBackstagePageItem { ContentType: null, Content: not null })
                            throw new ArgumentException("Clear the existing application-owned Content before selecting ContentType. The designer will not replace or dispose it.");
                        property.SetValue(item, converted);
                    }
                    // Validate SVG even for commit probes. Borrowed content is
                    // never assigned, instantiated, reparented or captured.
                    if (node.Icon.Kind == "Svg") item.Icon = new RibbonSvgIcon(node.Icon.Source, node.Icon.UseForegroundColor);
                    if (artwork && originals.TryGetValue(node.Key, out var original))
                    { if (node.Icon.Kind == "Custom") item.Icon = original.Icon; item.Image = original.Image; item.LargeImage = original.LargeImage; }
                    (IsFooter(node) ? probe.Backstage.FooterItems : probe.Backstage.Items).Add(item);
                }
                catch { item.Dispose(); throw; }
            }
            return probe;
        }
        catch { probe.Dispose(); throw; }
    }
    internal static bool Apply(RibbonControl ribbon, IDesignerHost host, RibbonBackstageSnapshot draft)
    {
        var before = Read(ribbon, host); Validate(before, draft);
        using var probe = Probe(ribbon, host, draft, false);
        if (Fingerprint(draft) == before.Revision) return false;
        var existing = ribbon.Backstage.NavigationItems.ToDictionary(RibbonDesignCommitter.Key, StringComparer.Ordinal);
        var names = new RibbonComponentNames(host, draft.Entries.Select(node => ((IComponent?)existing.GetValueOrDefault(node.Key), node.ComponentName)));
        var prepared = probe.Backstage.NavigationItems.ToDictionary(item => item.Id, StringComparer.Ordinal);
        foreach (var reference in ribbon.QuickAccessItems.OfType<RibbonQuickAccessReference>())
            if (reference.Source is { } source && existing.ContainsValue(source) && !draft.Entries.Any(node => node.Key == RibbonDesignCommitter.Key(source)))
                throw new ArgumentException("Remove the command's Quick Access reference before removing its Backstage source.");
        var originalNodes = before.Entries.ToDictionary(node => node.Key, StringComparer.Ordinal);
        var contentTypes = RibbonBackstageContentTypes.Available(host, ribbon);
        var changes = host.GetService(typeof(IComponentChangeService)) as IComponentChangeService ?? throw new InvalidOperationException("The designer change service is unavailable.");
        var selected = ribbon.Backstage.SelectedPage;
        using var transaction = host.CreateTransaction("Edit Backstage"); bool initializing = false;
        try
        {
            if (RibbonComponentNames.SameEntries(before.Entries, draft.Entries)
                && RibbonBackstageSnapshot.Json(before.Settings) == RibbonBackstageSnapshot.Json(draft.Settings))
            { names.Apply(changes); transaction.Commit(); return true; }
            var target = new Dictionary<string, RibbonItem>(existing, StringComparer.Ordinal);
            foreach (var node in draft.Entries)
            {
                if (!target.TryGetValue(node.Key, out var item)) target[node.Key] = item = (RibbonItem)host.CreateComponent(ItemType(node.Kind), node.ComponentName);
                else if (item.Site is null) host.Container.Add(item, node.ComponentName);
            }
            var collection = TypeDescriptor.GetProperties(ribbon.Backstage)[nameof(RibbonBackstage.Items)]!;
            var footerCollection = TypeDescriptor.GetProperties(ribbon.Backstage)[nameof(RibbonBackstage.FooterItems)]!;
            var backstageProperty = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.Backstage)]!;
            var fileProperty = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.FileMenu)]!;
            bool fileChanged = draft.Settings.Single(setting => setting.Name == "FileVisible").Value != before.Settings.Single(setting => setting.Name == "FileVisible").Value;
            // Backstage and FileMenu are stable unsited content properties.
            // Notify their sited root as well as the nested model/collection so
            // the designer records the complete serialized ownership path.
            changes.OnComponentChanging(ribbon, backstageProperty);
            if (fileChanged) changes.OnComponentChanging(ribbon, fileProperty);
            changes.OnComponentChanging(ribbon.Backstage, collection); changes.OnComponentChanging(ribbon.Backstage, footerCollection); ribbon.BeginInit(); initializing = true;
            ribbon.Backstage.Items.Clear(); ribbon.Backstage.FooterItems.Clear();
            void Set(object owner, PropertyDescriptor property, object? value)
            {
                var old = property.GetValue(owner); if (Equals(old, value)) return;
                changes.OnComponentChanging(owner, property); property.SetValue(owner, value); changes.OnComponentChanged(owner, property, old, value);
            }
            foreach (var setting in draft.Settings)
            {
                var (owner, property) = Setting(ribbon, setting.Name); Set(owner, property, property.Converter.ConvertFromInvariantString(setting.Value));
            }
            foreach (var node in draft.Entries)
            {
                var item = target[node.Key]; originalNodes.TryGetValue(node.Key, out var original);
                foreach (var value in node.Properties)
                {
                    if (value.Name == "Placement") continue;
                    if (original?.Properties.Single(property => property.Name == value.Name).Value == value.Value) continue;
                    var property = TypeDescriptor.GetProperties(item)[value.Name]!;
                    Set(item, property, value.Name == nameof(RibbonBackstagePageItem.ContentType) ? RibbonBackstageContentTypes.Resolve(value.Value, contentTypes)
                        : property.PropertyType == typeof(string) ? value.Value : property.Converter.ConvertFromInvariantString(value.Value));
                }
                if (original is null ? node.Icon.Kind != "None" : !RibbonDesignCommitter.SameIcon(original.Icon, node.Icon))
                    Set(item, TypeDescriptor.GetProperties(item)[nameof(item.Icon)]!, node.Icon.Kind == "Svg" ? prepared[node.Properties.Single(value => value.Name == "Id").Value].Icon : null);
                (IsFooter(node) ? ribbon.Backstage.FooterItems : ribbon.Backstage.Items).Add(item);
            }
            if (selected is not null && ribbon.Backstage.Available(selected)) ribbon.Backstage.SelectedPage = selected;
            changes.OnComponentChanged(ribbon.Backstage, collection, null, null);
            changes.OnComponentChanged(ribbon.Backstage, footerCollection, null, null);
            changes.OnComponentChanged(ribbon, backstageProperty, null, null);
            if (fileChanged) changes.OnComponentChanged(ribbon, fileProperty, null, null);
            foreach (var removed in existing.Where(pair => !draft.Entries.Any(node => node.Key == pair.Key))) host.DestroyComponent(removed.Value);
            initializing = false; ribbon.EndInit(); names.Apply(changes); transaction.Commit(); return true;
        }
        catch { transaction.Cancel(); throw; }
        finally { if (initializing) ribbon.EndInit(); }
    }
}
