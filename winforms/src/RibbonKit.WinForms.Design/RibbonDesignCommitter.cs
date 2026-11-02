using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Security.Cryptography;
using System.Text;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design;

// Design-only draft mapping. Existing components are retained so resources,
// bindings, event descriptors and external references keep their owners.
internal static partial class RibbonDesignCommitter
{
    private static readonly Dictionary<string, Type> Kinds = new()
    {
        ["Tab"] = typeof(RibbonTab), ["Group"] = typeof(RibbonGroup), ["Button"] = typeof(RibbonButtonItem),
        ["Toggle"] = typeof(RibbonToggleItem), ["CheckBox"] = typeof(RibbonCheckBoxItem), ["RadioButton"] = typeof(RibbonRadioButtonItem),
        ["DropDown"] = typeof(RibbonDropDownItem), ["SplitButton"] = typeof(RibbonSplitButtonItem),
        ["TextBox"] = typeof(RibbonTextBoxItem), ["ComboBox"] = typeof(RibbonComboBoxItem),
        ["Gallery"] = typeof(RibbonGalleryItem), ["GalleryTile"] = typeof(RibbonGalleryTile), ["Separator"] = typeof(RibbonMenuSeparatorItem),
        ["QatReference"] = typeof(RibbonQuickAccessReference), ["QatMenuReference"] = typeof(RibbonQuickAccessDropDownReference),
        ["QatComboReference"] = typeof(RibbonQuickAccessComboBoxReference), ["QatGroupReference"] = typeof(RibbonQuickAccessGroupReference)
    };
    private static readonly string[] ScalarNames =
    [
        "Id", "Text", "KeyTip", "Enabled", "Visible", "IsContextual", "ContextualColor", "CanClose", "CloseButtonText",
        "Priority", "ReductionMode", "Layout", "Row", "SizePolicy", "Checked", "CheckState", "GroupName", "MenuDescription", "DropDownHeader", "MenuPresentation",
        "ScreenTipTitle", "ScreenTipDescription", "ShowLabel", "IsEditable", "Value", "InputWidth", "ReadOnly", "MaxLength", "SelectedIndex",
        "IsInRibbon", "InRibbonColumnCount", "ColumnCount", "TileWidth", "TileHeight", "MaxVisibleRows", "DropDownKeyTip", "DropDownPresenterType"
    ];
    private static readonly string[] SizePolicies = new[]
    {
        RibbonSizePolicy.Fixed(RibbonItemSize.Large).ToString(), RibbonSizePolicy.Fixed(RibbonItemSize.Medium).ToString(),
        RibbonSizePolicy.Fixed(RibbonItemSize.Small).ToString(), RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small).ToString()
    }.Concat(from large in Enum.GetValues<RibbonItemSize>() from medium in Enum.GetValues<RibbonItemSize>() from small in Enum.GetValues<RibbonItemSize>()
        where large <= medium && medium <= small
        select RibbonSizePolicy.Adaptive(large, medium, small).ToString()).Distinct(StringComparer.Ordinal).ToArray();

    internal static RibbonDesignSnapshot Read(RibbonControl ribbon, IDesignerHost host)
    {
        EnsureNestedDesignModels(ribbon);
        var elements = EditableElements(ribbon).ToArray();
        var presenterTypes = RibbonGalleryPresenterTypes.Available(host, ribbon);
        var used = new HashSet<string>(host.Container.Components.Cast<IComponent>().Select(component => component.Site?.Name ?? ""), StringComparer.OrdinalIgnoreCase);
        var backdropHost = BackdropHost(ribbon, host);
        var snapshot = new RibbonDesignSnapshot
        {
            ComponentName = ribbon.Site?.Name ?? ribbon.Name,
            BuiltInTheme = ribbon.BuiltInTheme?.ToString() ?? "",
            ThemeRevision = ribbon.Theme + "\n" + ribbon.ApplicationTheme,
            ApplicationTheme = ribbon.ApplicationTheme == RibbonThemes.Office2024Light ? "Office 2024 Light" : "Custom application theme",
            ThemeChoices = Enum.GetNames<RibbonBuiltInTheme>(),
            AnimationLevel = ribbon.AnimationLevel.ToString(),
            AnimationChoices = Enum.GetNames<RibbonAnimationLevel>(),
            TabContentAnimation = ribbon.TabContentAnimation.ToString(),
            TabContentAnimationChoices = Enum.GetNames<RibbonTabContentAnimation>(),
            CanSetBackdrop = backdropHost is not null,
            Backdrop = (backdropHost?.Backdrop ?? RibbonWindowBackdrop.None).ToString(),
            BackdropChoices = Enum.GetNames<RibbonWindowBackdrop>(),
            NavigationBackgroundOpacity = ribbon.Backstage.NavigationBackgroundOpacity,
            QuickAccessVisible = ribbon.QuickAccessVisible,
            QuickAccessPosition = ribbon.QuickAccessPosition.ToString(),
            ReservedNames = host.Container.Components.Cast<IComponent>().Where(component => !elements.Contains(component)).Select(component => component.Site?.Name ?? "").Where(name => name.Length != 0).ToList(),
            ReservedIds = OutsideElements(ribbon).Except(elements).Select(element => element.Id).ToList()
        };
        RibbonDesignNode Map(RibbonElement element)
        {
            string name = element.Site?.Name ?? UniqueName(element.GetType().Name, used);
            return new RibbonDesignNode { Key = Key(element), ComponentName = name, Kind = Kind(element),
                Properties = Properties(element, presenterTypes), Icon = ReadIcon(element), Choices = element is RibbonComboBoxItem choice ? choice.Items.ToList() : [], Children = ChildElements(element).Select(Map).ToList() };
        }
        snapshot.Tabs = ribbon.Tabs.Select(Map).ToList();
        ReadQuickAccess(ribbon, snapshot, Map);
        snapshot.SelectionKey = (host.GetService(typeof(ISelectionService)) as ISelectionService)?.PrimarySelection is RibbonElement selected && elements.Contains(selected) ? Key(selected) : "";
        snapshot.Revision = Fingerprint(snapshot);
        foreach (var kind in Kinds)
        {
            using var element = (RibbonElement)Activator.CreateInstance(kind.Value)!;
            snapshot.Templates.Add(new() { Kind = kind.Key, Properties = Properties(element, presenterTypes) });
        }
        return snapshot;
    }
    internal static void EnsureNestedDesignModels(RibbonControl ribbon)
    {
        // Stable content models need qualified names for absolute Undo state.
        if (ribbon.Site?.GetService(typeof(INestedContainer)) is not INestedContainer nested) return;
        if (ribbon.Backstage.Site is null) nested.Add(ribbon.Backstage, nameof(ribbon.Backstage));
        if (ribbon.CommandCatalog.Site is null) nested.Add(ribbon.CommandCatalog, nameof(ribbon.CommandCatalog));
    }
    internal static bool Apply(RibbonControl ribbon, IDesignerHost host, RibbonDesignSnapshot draft)
    {
        var before = Read(ribbon, host);
        if (before.Revision != draft.Revision) throw new ArgumentException("The ribbon changed while the editor was open. Cancel and reopen Edit Ribbon.");
        var theme = ParseTheme(draft.BuiltInTheme);
        var animation = ParseAnimation(draft.AnimationLevel);
        var contentAnimation = ParseContentAnimation(draft.TabContentAnimation);
        var backdrop = ParseBackdrop(draft.Backdrop, before.CanSetBackdrop);
        ValidateNavigationOpacity(draft.NavigationBackgroundOpacity);
        _ = ParseQuickAccessPosition(draft.QuickAccessPosition);
        draft.CanSetBackdrop = before.CanSetBackdrop;
        draft.ThemeRevision = before.ThemeRevision;
        // Reserved names/IDs and property schemas come from the live designer,
        // not from data sent back by a dialog.
        draft.ReservedIds = before.ReservedIds; draft.ReservedNames = before.ReservedNames; draft.QuickAccessSources = before.QuickAccessSources; draft.Validate();
        var quickAccess = PrepareQuickAccess(ribbon, host, before, draft);
        var namedElements = EditableElements(ribbon).ToDictionary(Key, StringComparer.Ordinal);
        var presenterTypes = RibbonGalleryPresenterTypes.Available(host, ribbon);
        foreach (var node in draft.CommandNodes.Where(node => node.Kind == "Gallery"))
        {
            var type = RibbonGalleryPresenterTypes.Resolve(node.Properties.Single(property => property.Name == "DropDownPresenterType").Value, presenterTypes);
            if (type is not null && namedElements.GetValueOrDefault(node.Key) is RibbonGalleryItem { DropDownPresenterFactory: not null })
                throw new ArgumentException("Clear the application's DropDownPresenterFactory before selecting DropDownPresenterType. The designer preserves runtime factories.");
        }
        var requestedNames = draft.AllNodes.Select(node => ((IComponent?)namedElements.GetValueOrDefault(node.Key), node.ComponentName)).ToList();
        if (before.ComponentName.Length != 0 || draft.ComponentName.Length != 0) requestedNames.Add((ribbon, draft.ComponentName));
        var names = new RibbonComponentNames(host, requestedNames);
        var existing = Elements(ribbon.Tabs).ToDictionary(Key, StringComparer.Ordinal);
        var originalNodes = before.Nodes.ToDictionary(node => node.Key, StringComparer.Ordinal);
        var preparedIcons = new Dictionary<string, RibbonSvgIcon>(StringComparer.Ordinal);
        foreach (var node in draft.Nodes)
        {
            if (originalNodes.TryGetValue(node.Key, out var original))
            {
                if (node.Kind != original.Kind) throw new ArgumentException("Existing component types must be preserved.");
            }
            else if (!node.Key.StartsWith("new:", StringComparison.Ordinal) || host.Container.Components[node.ComponentName] is not null)
                throw new ArgumentException("A new component needs a unique component name.");
            if (!Kinds.ContainsKey(node.Kind)) throw new ArgumentException("Unsupported command type: " + node.Kind);
            var schema = before.Templates.Single(template => template.Kind == node.Kind);
            if (!schema.Properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal).SetEquals(node.Properties.Select(property => property.Name)))
                throw new ArgumentException("The property set is incomplete or unsupported for " + node.Kind + ". Cancel and reopen Edit Ribbon.");
            if (node.Icon.Kind == "Custom" && (original is null || !SameIcon(node.Icon, original.Icon))) throw new ArgumentException("Custom icon providers can only be preserved or explicitly replaced.");
            if (node.Kind == "Tab" && node.Icon.Kind != "None") throw new ArgumentException("Tabs do not have command icons.");
            if (node.Icon.Kind is not ("None" or "Svg" or "Custom")) throw new ArgumentException("Unsupported icon selection.");
            if (node.Icon.Kind == "Svg" && (original == null || !SameIcon(node.Icon, original.Icon)))
                preparedIcons.Add(node.Key, new RibbonSvgIcon(node.Icon.Source, node.Icon.UseForegroundColor));
        }
        if (Fingerprint(draft) == before.Revision) return false;
        // Exercise conversions, setters, native choice constraints and runtime
        // collection/ID rules on detached built-in models before live mutation.
        using (var probe = new RibbonControl())
            foreach (var node in draft.Tabs) probe.Tabs.Add((RibbonTab)BuildProbe(node, originalNodes, before.Templates, presenterTypes));
        var retained = draft.Nodes.Select(node => node.Key).ToHashSet(StringComparer.Ordinal);
        var retainedComponents = existing.Where(pair => retained.Contains(pair.Key)).Select(pair => pair.Value).ToHashSet();
        var changes = (IComponentChangeService?)host.GetService(typeof(IComponentChangeService))
            ?? throw new InvalidOperationException("The designer change service is unavailable.");
        var selected = ribbon.SelectedTab;
        var gallerySelections = existing.Values.OfType<RibbonGalleryItem>().ToDictionary(gallery => gallery, gallery => gallery.SelectedItem);
        var transaction = host.CreateTransaction("Edit Ribbon");
        bool initializing = false;
        try
        {
            if (ribbon.Backstage.NavigationBackgroundOpacity != draft.NavigationBackgroundOpacity)
            {
                var rootProperty = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.Backstage)]!;
                var property = TypeDescriptor.GetProperties(ribbon.Backstage)[nameof(ribbon.Backstage.NavigationBackgroundOpacity)]!;
                var old = ribbon.Backstage.NavigationBackgroundOpacity;
                changes.OnComponentChanging(ribbon, rootProperty);
                changes.OnComponentChanging(ribbon.Backstage, property);
                property.SetValue(ribbon.Backstage, draft.NavigationBackgroundOpacity);
                changes.OnComponentChanged(ribbon.Backstage, property, old, draft.NavigationBackgroundOpacity);
                changes.OnComponentChanged(ribbon, rootProperty, null, null);
            }
            if (BackdropHost(ribbon, host) is { } form && form.Backdrop != backdrop)
            {
                var property = TypeDescriptor.GetProperties(form)[nameof(form.Backdrop)]!;
                var old = form.Backdrop;
                changes.OnComponentChanging(form, property); property.SetValue(form, backdrop); changes.OnComponentChanged(form, property, old, backdrop);
            }
            if (ribbon.BuiltInTheme != theme)
            {
                var property = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.BuiltInTheme)]!;
                var old = ribbon.BuiltInTheme;
                changes.OnComponentChanging(ribbon, property); property.SetValue(ribbon, theme); changes.OnComponentChanged(ribbon, property, old, theme);
            }
            if (ribbon.AnimationLevel != animation)
            {
                var property = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.AnimationLevel)]!;
                var old = ribbon.AnimationLevel;
                changes.OnComponentChanging(ribbon, property); property.SetValue(ribbon, animation); changes.OnComponentChanged(ribbon, property, old, animation);
            }
            if (ribbon.TabContentAnimation != contentAnimation)
            {
                var property = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.TabContentAnimation)]!;
                var old = ribbon.TabContentAnimation;
                changes.OnComponentChanging(ribbon, property); property.SetValue(ribbon, contentAnimation); changes.OnComponentChanged(ribbon, property, old, contentAnimation);
            }
            if (RibbonComponentNames.SameEntries(before.Tabs, draft.Tabs))
            { quickAccess.Apply(existing, changes); names.Apply(changes); transaction.Commit(); return true; }
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
                if (original == null ? node.Icon.Kind != "None" : !SameIcon(node.Icon, original.Icon))
                {
                    var property = TypeDescriptor.GetProperties(element)["Icon"] ?? throw new ArgumentException("This element has no icon.");
                    var old = property.GetValue(element);
                    object? next = node.Icon.Kind == "Svg" ? preparedIcons[node.Key] : null;
                    changes.OnComponentChanging(element, property); property.SetValue(element, next); changes.OnComponentChanged(element, property, old, next);
                }
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
                var property = CheckedProperty(element, value.Name); object? old = property.GetValue(element), next = ConvertValue(property, value.Value, presenterTypes);
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
            quickAccess.Apply(target, changes);
            foreach (var collection in collections) changes.OnComponentChanged(collection.Owner, collection.Property, null, null);
            foreach (var removed in existing.Where(pair => !retained.Contains(pair.Key)).Reverse()) host.DestroyComponent(removed.Value);
            initializing = false; ribbon.EndInit();
            names.Apply(changes);
            transaction.Commit(); return true;
        }
        catch { transaction.Cancel(); throw; }
        finally { if (initializing) ribbon.EndInit(); ((IDisposable)transaction).Dispose(); }
    }

    internal static RibbonElement BuildProbe(RibbonDesignNode node, Dictionary<string, RibbonDesignNode> originals, List<RibbonDesignNode> templates, IEnumerable<Type>? presenterTypes = null)
    {
        var element = (RibbonElement)Activator.CreateInstance(Kinds[node.Kind])!;
        try
        {
            if (element is RibbonComboBoxItem combo) combo.Items.AddRange(node.Choices.ToArray());
            foreach (var value in node.Properties)
            {
                if (element is RibbonComboBoxItem && value.Name is "Value" or "SelectedIndex") continue;
                var property = CheckedProperty(element, value.Name); property.SetValue(element, ConvertValue(property, value.Value, presenterTypes));
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
            if (Collection(element) is { } children) foreach (var child in node.Children) children.Add(BuildProbe(child, originals, templates, presenterTypes));
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
    private static object? ConvertValue(PropertyDescriptor property, string value, IEnumerable<Type>? presenterTypes = null) => property.Name == "DropDownPresenterType"
        ? RibbonGalleryPresenterTypes.Resolve(value, presenterTypes ?? []) : property.PropertyType == typeof(string) ? value : property.Converter.ConvertFromInvariantString(value);
    internal static bool SameIcon(RibbonDesignIcon first, RibbonDesignIcon second) => first.Kind == second.Kind && first.Source == second.Source && first.UseForegroundColor == second.UseForegroundColor;
    internal static RibbonDesignIcon ReadIcon(RibbonElement element)
    {
        var icon = element switch { RibbonItem item => item.Icon, RibbonGroup group => group.Icon, _ => null };
        bool raster = element is RibbonItem { Image: not null } or RibbonItem { LargeImage: not null };
        return icon is RibbonSvgIcon svg ? new() { Kind = "Svg", Source = svg.Source, Name = svg.Name, UseForegroundColor = svg.UseForegroundColor, HasRasterImage = raster }
            : new() { Kind = icon == null ? "None" : "Custom", Name = icon?.GetType().Name ?? "", HasRasterImage = raster };
    }
    internal static List<RibbonDesignProperty> Properties(RibbonElement element, IEnumerable<Type>? presenterTypes = null) => ScalarNames
        .Where(name => name != "Checked" || element is not RibbonCheckBoxItem)
        .Where(name => name is not ("DropDownHeader" or "MenuPresentation") || element is not RibbonGalleryItem)
        .Select(name => TypeDescriptor.GetProperties(element)[name]).Where(property => property is { IsReadOnly: false })
        .Select(property => new RibbonDesignProperty { Name = property!.Name, Category = property.Category,
            Description = property.Name switch
            {
                "DropDownHeader" => "Noninteractive heading above this dropdown's commands, for example Paste options. Empty hides the heading.",
                "MenuPresentation" => "Standard uses normal command rows. IllustratedChoices uses larger artwork, bold titles and descriptions for leaf commands.",
                "MenuDescription" => "Explanation displayed below this command's Text inside a dropdown, up to two wrapped lines. Separate from the hover ScreenTip.",
                "SizePolicy" => "Command sizes in the expanded, intermediate and minimum group states.",
                _ => property.Description
            },
            Type = property.PropertyType == typeof(Type) ? "String" : property.PropertyType.Name,
            Value = property.PropertyType == typeof(Type) ? RibbonGalleryPresenterTypes.Label(property.GetValue(element) as Type)
                : property.PropertyType == typeof(string) ? (string?)property.GetValue(element) ?? "" : property.Converter.ConvertToInvariantString(property.GetValue(element)) ?? "",
            Options = property.PropertyType == typeof(Type) ? new[] { RibbonGalleryPresenterTypes.None }.Concat((presenterTypes ?? []).Select(RibbonGalleryPresenterTypes.Label)).ToArray()
                : property.PropertyType.IsEnum ? Enum.GetNames(property.PropertyType) : property.PropertyType == typeof(RibbonSizePolicy)
                ? SizePolicies.Concat(new[] { property.Converter.ConvertToInvariantString(property.GetValue(element))! }).Distinct(StringComparer.Ordinal).ToArray() : [] }).ToList();
    private static string Kind(RibbonElement element) => Kinds.FirstOrDefault(pair => pair.Value == element.GetType()).Key
        ?? throw new ArgumentException("Edit Ribbon currently supports built-in component types. Use the existing property editor for " + element.GetType().Name + ".");
    internal static string Key(RibbonElement element) => element.Site?.Name is { Length: > 0 } name ? "site:" + name : "id:" + element.Id;
    private static string UniqueName(string type, HashSet<string> used)
    {
        string stem = char.ToLowerInvariant(type[0]) + type.Substring(1); int suffix = 1; string name;
        do { name = stem + suffix++; } while (!used.Add(name)); return name;
    }
    internal static RibbonBuiltInTheme? ParseTheme(string value) => value switch
    {
        "" => null,
        nameof(RibbonBuiltInTheme.Office2024Light) => RibbonBuiltInTheme.Office2024Light,
        _ => throw new ArgumentException("Select a supported built-in theme or the application theme.")
    };
    internal static RibbonAnimationLevel ParseAnimation(string value) => value switch
    {
        nameof(RibbonAnimationLevel.None) => RibbonAnimationLevel.None,
        nameof(RibbonAnimationLevel.Subtle) => RibbonAnimationLevel.Subtle,
        nameof(RibbonAnimationLevel.Expressive) => RibbonAnimationLevel.Expressive,
        _ => throw new ArgumentException("Select a supported animation level.")
    };
    internal static RibbonTabContentAnimation ParseContentAnimation(string value) => value switch
    {
        nameof(RibbonTabContentAnimation.Fade) => RibbonTabContentAnimation.Fade,
        nameof(RibbonTabContentAnimation.SlideDown) => RibbonTabContentAnimation.SlideDown,
        _ => throw new ArgumentException("Select Fade or SlideDown for tab content animation.")
    };
    private static RibbonForm? BackdropHost(RibbonControl ribbon, IDesignerHost host) => host.RootComponent is RibbonForm form
        && !form.IsDisposed && ReferenceEquals(ribbon.FindForm(), form)
        && TypeDescriptor.GetProperties(form)[nameof(form.Backdrop)] is { IsReadOnly: false } ? form : null;
    internal static RibbonWindowBackdrop ParseBackdrop(string value, bool canSetBackdrop)
    {
        var backdrop = value switch
        {
            nameof(RibbonWindowBackdrop.None) => RibbonWindowBackdrop.None,
            nameof(RibbonWindowBackdrop.Mica) => RibbonWindowBackdrop.Mica,
            nameof(RibbonWindowBackdrop.Acrylic) => RibbonWindowBackdrop.Acrylic,
            _ => throw new ArgumentException("Select None, Mica or Acrylic for the window backdrop.")
        };
        if (!canSetBackdrop && backdrop != RibbonWindowBackdrop.None) throw new ArgumentException("Window backdrop requires an editable containing RibbonForm.");
        return backdrop;
    }
    internal static void ValidateNavigationOpacity(float value)
    {
        if (!float.IsFinite(value) || value < 0 || value > 1) throw new ArgumentException("Backstage navigation opacity must be between 0 and 1.");
    }
    private static string Fingerprint(RibbonDesignSnapshot snapshot) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(new RibbonDesignSnapshot { ComponentName = snapshot.ComponentName, Tabs = snapshot.Tabs, CatalogCommands = snapshot.CatalogCommands, QuickAccessItems = snapshot.QuickAccessItems, QuickAccessVisible = snapshot.QuickAccessVisible, QuickAccessPosition = snapshot.QuickAccessPosition, BuiltInTheme = snapshot.BuiltInTheme, ThemeRevision = snapshot.ThemeRevision, AnimationLevel = snapshot.AnimationLevel, TabContentAnimation = snapshot.TabContentAnimation, CanSetBackdrop = snapshot.CanSetBackdrop, Backdrop = snapshot.Backdrop, NavigationBackgroundOpacity = snapshot.NavigationBackgroundOpacity }.Serialize())));
    private static IEnumerable<RibbonElement> Elements(IEnumerable<RibbonElement> elements)
    { foreach (var element in elements) { yield return element; foreach (var child in Elements(ChildElements(element))) yield return child; } }
    private static IEnumerable<RibbonElement> ChildElements(RibbonElement element) => element switch
    {
        RibbonTab tab => tab.Groups, RibbonGroup group => group.Items, RibbonDropDownItem drop => drop.Items,
        RibbonBackstage backstage => backstage.NavigationItems, RibbonCommandCatalog catalog => catalog.Commands,
        RibbonMessageBar messages => messages.Items, RibbonMessage message => [message.Action], _ => []
    };
    private static IEnumerable<RibbonElement> OutsideElements(RibbonControl ribbon) => Elements(new RibbonElement[] { ribbon.FileMenu, ribbon.Backstage, ribbon.CommandCatalog, ribbon.MessageBar }.Concat(ribbon.QuickAccessItems));
    private static IList? Collection(object owner) => owner switch { RibbonControl ribbon => ribbon.Tabs, RibbonTab tab => tab.Groups, RibbonGroup group => group.Items, RibbonDropDownItem drop => drop.Items, _ => null };
    private static string CollectionName(object owner) => owner is RibbonControl ? "Tabs" : owner is RibbonTab ? "Groups" : "Items";
}
