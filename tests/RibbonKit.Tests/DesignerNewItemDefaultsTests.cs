using System.IO;
using System.Reflection;
using Xunit;

namespace RibbonKit.Tests;

// Executes the shipped designer policy without loading the VS SDK. The adapter's
// ModelProperty.IsSet mapping and native Toolbox transaction are separate live gates.
public sealed class DesignerNewItemDefaultsTests
{
    [Theory]
    [InlineData("RibbonButton")]
    [InlineData("RibbonToggleButton")]
    [InlineData("RibbonSplitButton")]
    [InlineData("RibbonDropDownButton")]
    [InlineData("RibbonComboBox")]
    [InlineData("RibbonGallery")]
    [InlineData("InRibbonGallery")]
    public void New_supported_commands_are_inline_and_labeled_without_sizing_or_selection_guesses(string type)
    {
        var item = new Item(type);
        item.Initialize();
        Assert.Equal("Inline", item.Values["Ribbon.SimplifiedPlacement"]);
        Assert.Equal("Medium", item.Values["Ribbon.SimplifiedSize"]);
        Assert.False(string.IsNullOrWhiteSpace(item.Values["Header"]));
        foreach (string property in new[] { "Size", "Width", "Height", "SelectedIndex", "SelectedItem", "SelectedValue", "Name", "Icon", "Command" })
            Assert.False(item.Values.ContainsKey(property), property);
    }

    [Theory]
    [InlineData("RibbonTab", "Groups", "RibbonGroup", "Header", "New Group")]
    [InlineData("Backstage", "Items", "BackstageTabItem", "Header", "Info")]
    [InlineData("RibbonApplicationMenu", "Items", "RibbonApplicationMenuItem", "Header", "New Command")]
    [InlineData("RibbonComboBox", "Items", "ComboBoxItem", "Content", "Item")]
    [InlineData("RibbonGallery", "Items", "RibbonGalleryItem", "Content", "Item")]
    [InlineData("InRibbonGallery", "Items", "RibbonGalleryItem", "Content", "Item")]
    [InlineData("RibbonSplitButton", "Items", "RibbonMenuItem", "Header", "Menu Item")]
    [InlineData("RibbonDropDownButton", "Items", "RibbonMenuItem", "Header", "Menu Item")]
    [InlineData("RibbonMessageBar", "Items", "RibbonMessage", "Title", "Notification")]
    public void Empty_new_structures_receive_one_semantically_owned_child_and_do_not_duplicate_it(
        string type, string collection, string childType, string captionProperty, string caption)
    {
        var item = new Item(type);
        item.Initialize();
        item.Initialize();
        var child = Assert.Single(item.Children[collection]);
        Assert.Equal(childType, child.Type);
        Assert.Equal(caption, child.Values[captionProperty]);
        Assert.False(child.Values.ContainsKey("Content") && childType == "RibbonApplicationMenuItem");
    }

    [Fact]
    public void New_ribbon_has_one_home_tab_and_group_and_reinitialization_preserves_edits()
    {
        var ribbon = new Item("Ribbon");
        ribbon.Initialize();
        var tab = Assert.Single(ribbon.Children["Tabs"]);
        var group = Assert.Single(tab.Children["Groups"]);
        Assert.Equal("Home", tab.Values["Header"]);
        Assert.Equal("Group", group.Values["Header"]);
        tab.Values["Header"] = "Authored Home";
        group.Values["Header"] = "";
        ribbon.Initialize();
        Assert.Same(tab, Assert.Single(ribbon.Children["Tabs"]));
        Assert.Equal("Authored Home", tab.Values["Header"]);
        Assert.Equal("", group.Values["Header"]);
    }

    [Theory]
    [InlineData("Overflow", "Small", "")]
    [InlineData("{Binding Placement}", "{Binding LabelSize}", "{Binding Caption}")]
    public void Explicit_values_empty_captions_bindings_and_data_sources_survive(string placement, string size, string caption)
    {
        var item = new Item("RibbonComboBox");
        item.Values["Ribbon.SimplifiedPlacement"] = placement;
        item.Values["Ribbon.SimplifiedSize"] = size;
        item.Values["Header"] = caption;
        item.Values["ItemsSource"] = "{Binding Choices}";
        item.Values["Width"] = "{Binding EditorWidth}";
        item.Values["SelectedItem"] = "{Binding Selection}";
        item.Values["InputWidth"] = "{Binding InputWidth}";
        var before = item.Values.ToArray();
        item.Initialize();
        Assert.Equal(before, item.Values.ToArray());
        Assert.Empty(item.Children);
    }

    [Theory]
    [InlineData("Ribbon", "Tabs")]
    [InlineData("RibbonTab", "Groups")]
    [InlineData("Backstage", "Items")]
    [InlineData("RibbonApplicationMenu", "Items")]
    [InlineData("RibbonComboBox", "Items")]
    [InlineData("RibbonGallery", "Items")]
    [InlineData("InRibbonGallery", "Items")]
    [InlineData("RibbonSplitButton", "Items")]
    [InlineData("RibbonDropDownButton", "Items")]
    public void Explicit_empty_collections_and_existing_children_are_not_seeded(string type, string collection)
    {
        var empty = new Item(type);
        empty.Values[collection] = "";
        empty.Initialize();
        Assert.Empty(empty.Children);

        var populated = new Item(type);
        var authored = new Item("Existing authored child");
        populated.Children[collection] = new() { authored };
        populated.Initialize();
        Assert.Same(authored, Assert.Single(populated.Children[collection]));
        Assert.Empty(authored.Values);
    }

    [Theory]
    [InlineData("RibbonComboBox")]
    [InlineData("RibbonGallery")]
    [InlineData("InRibbonGallery")]
    [InlineData("RibbonSplitButton")]
    [InlineData("RibbonDropDownButton")]
    [InlineData("Backstage")]
    [InlineData("RibbonApplicationMenu")]
    public void Explicit_null_items_source_is_an_authored_choice(string type)
    {
        var item = new Item(type);
        item.Values["ItemsSource"] = "{x:Null}";
        item.Initialize();
        Assert.Empty(item.Children);
    }

    [Theory]
    [InlineData("RibbonCheckBox")]
    [InlineData("RibbonRadioButton")]
    [InlineData("RibbonTextBox")]
    [InlineData("RibbonScrollBar")]
    [InlineData("RibbonGroupSeparator")]
    [InlineData("RibbonMenuItem")]
    [InlineData("RibbonGalleryItem")]
    [InlineData("RibbonApplicationMenuItem")]
    public void Native_inputs_and_item_entries_do_not_get_independent_simplified_command_defaults(string type)
    {
        var item = new Item(type);
        item.Initialize();
        Assert.False(item.Values.ContainsKey("Ribbon.SimplifiedPlacement"));
        Assert.False(item.Values.ContainsKey("Ribbon.SimplifiedSize"));
        Assert.Empty(item.Children);
    }

    [Fact]
    public void New_combo_uses_input_width_and_native_item_without_outer_width()
    {
        var combo = new Item("RibbonComboBox");
        combo.Initialize();
        Assert.Equal("130", combo.Values["InputWidth"]);
        Assert.False(combo.Values.ContainsKey("Width"));
        Assert.Equal("ComboBoxItem", Assert.Single(combo.Children["Items"]).Type);
        var native = Apply.DeclaringType!.GetMethod("IsFrameworkType", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.True((bool)native.Invoke(null, new object[] { "ComboBoxItem" })!);
        Assert.False((bool)native.Invoke(null, new object[] { "RibbonComboBox" })!);
    }

    [Theory]
    [InlineData("90")]
    [InlineData("")]
    [InlineData("{Binding Width}")]
    public void Authored_combo_input_width_is_preserved(string value)
    {
        var combo = new Item("RibbonComboBox");
        combo.Values["InputWidth"] = value;
        combo.Initialize();
        Assert.Equal(value, combo.Values["InputWidth"]);
    }

    private sealed class Item(string type)
    {
        public string Type { get; } = type;
        public Dictionary<string, string> Values { get; } = new();
        public Dictionary<string, List<Item>> Children { get; } = new();

        public void Initialize(bool starterTab = false) => Apply.Invoke(null, new object[]
        {
            Type, new Func<string, bool>(Values.ContainsKey),
            new Func<string, int>(property => Children.TryGetValue(property, out var children) ? children.Count : 0),
            new Action<string, string>((property, value) => Values.Add(property, value)),
            new Action<string, string, string, string>((collection, childType, property, value) =>
            {
                var child = new Item(childType);
                child.Values.Add(property, value);
                child.Initialize(Type == "Ribbon" && childType == "RibbonTab");
                Children[collection] = new() { child };
            }), starterTab,
        });
    }

    private static readonly MethodInfo Apply = LoadPolicy();

    private static MethodInfo LoadPolicy()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln"))) directory = directory.Parent;
        Assert.NotNull(directory);
        string configuration = typeof(DesignerNewItemDefaultsTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        string path = Path.Combine(directory.FullName, "src", "RibbonKit.Design", "bin", configuration, "net472", "RibbonKit.DesignTools.dll");
        return Assembly.LoadFrom(path).GetType("RibbonKit.Design.NewItemDefaults", throwOnError: true)!
            .GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic)!;
    }
}
