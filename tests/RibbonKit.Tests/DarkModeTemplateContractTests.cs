using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Guards dark-aware foreground paths and solid nested-control surfaces.</summary>
public sealed class DarkModeTemplateContractTests
{
    private const string ControlSurfaceBackground =
        "{DynamicResource RibbonKit.Brushes.Control.SurfaceBackground}";

    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Dropdown_split_and_editable_combo_forward_the_theme_foreground()
    {
        XDocument document = XDocument.Load(ThemePart("Controls.DropDowns.xaml"));

        XElement dropdown = Template(document, "RibbonDropDownButton");
        AssertForegroundBinding(Named(dropdown, "ToggleButton", "PART_Toggle"));

        XElement split = Template(document, "RibbonSplitButton");
        AssertForegroundBinding(Named(split, "Button", "PART_Primary"));
        AssertForegroundBinding(Named(split, "ToggleButton", "PART_Toggle"));

        XElement combo = Template(document, "RibbonComboBox");
        Assert.Equal(
            "{TemplateBinding Foreground}",
            (string?)Named(combo, "TextBox", "PART_EditableTextBox").Attribute("Foreground"));
    }

    [Fact]
    public void Customize_lists_and_tree_forward_the_page_foreground()
    {
        XDocument document = XDocument.Load(ThemePart("Controls.Customize.xaml"));
        string[] partNames =
        {
            "PART_AvailableList",
            "PART_CurrentList",
            "PART_Tree",
            "PART_IconList",
        };

        XElement[] controls = document
            .Descendants()
            .Where(element => partNames.Contains((string?)element.Attribute(Xaml + "Name")))
            .ToArray();

        Assert.Equal(5, controls.Length); // PART_AvailableList appears on both built-in pages.
        Assert.All(
            controls,
            element => Assert.Equal(
                "{TemplateBinding Foreground}",
                (string?)element.Attribute("Foreground")));

        XElement tree = Assert.Single(
            controls,
            element => (string?)element.Attribute(Xaml + "Name") == "PART_Tree");
        XElement itemStyle = Assert.Single(
            tree.Descendants(Presentation + "Style"),
            element => (string?)element.Attribute("TargetType") == "{x:Type TreeViewItem}");
        XElement foreground = Assert.Single(
            itemStyle.Elements(Presentation + "Setter"),
            element => (string?)element.Attribute("Property") == "Foreground");
        Assert.Equal(
            "{Binding Foreground, RelativeSource={RelativeSource AncestorType={x:Type TreeView}}}",
            (string?)foreground.Attribute("Value"));
    }

    [Fact]
    public void Gallery_items_supply_an_inherited_theme_foreground()
    {
        XDocument document = XDocument.Load(ThemePart("Controls.Galleries.xaml"));
        XElement style = Assert.Single(
            document.Root!.Elements(Presentation + "Style"),
            element => (string?)element.Attribute("TargetType")
                == "{x:Type controls:RibbonGalleryItem}");
        XElement setter = Assert.Single(
            style.Elements(Presentation + "Setter"),
            element => (string?)element.Attribute("Property") == "Foreground");

        Assert.Equal(
            "{DynamicResource RibbonKit.Brushes.Text.Primary}",
            (string?)setter.Attribute("Value"));
    }

    [Fact]
    public void Combo_and_in_ribbon_gallery_use_the_control_surface_background()
    {
        XDocument dropdowns = XDocument.Load(ThemePart("Controls.DropDowns.xaml"));
        XElement combo = Template(dropdowns, "RibbonComboBox");
        Assert.Equal(
            ControlSurfaceBackground,
            (string?)Named(combo, "Border", "Chrome").Attribute("Background"));

        XDocument galleries = XDocument.Load(ThemePart("Controls.Galleries.xaml"));
        XElement gallery = Template(galleries, "InRibbonGallery");
        XElement surface = Assert.Single(
            gallery.Descendants(Presentation + "Border"),
            element => (string?)element.Attribute("Grid.ColumnSpan") == "2");
        Assert.Equal(ControlSurfaceBackground, (string?)surface.Attribute("Background"));
    }

    [Fact]
    public void Control_surface_background_is_solid_in_every_theme_variant()
    {
        string[] themeFiles =
        {
            "Tokens.Office2007.xaml",
            "Tokens.Office2010.xaml",
            "Tokens.Office2013.xaml",
            "Tokens.Office2019.xaml",
            "Tokens.Office2024.xaml",
            "Tokens.Office2019.Dark.xaml",
            "Tokens.Office2024.Dark.xaml",
        };

        foreach (string themeFile in themeFiles)
        {
            XDocument document = XDocument.Load(ThemePart(themeFile));
            XElement resource = Assert.Single(
                document.Root!.Elements(),
                element => (string?)element.Attribute(Xaml + "Key")
                    == "RibbonKit.Brushes.Control.SurfaceBackground");

            Assert.Equal(Presentation + "SolidColorBrush", resource.Name);
        }
    }

    [Fact]
    public void Application_menu_hit_area_inherits_its_nav_items_foreground()
    {
        XDocument document = XDocument.Load(ThemePart("Controls.ApplicationMenu.xaml"));
        XElement style = Assert.Single(
            document.Descendants(Presentation + "Style"),
            element => (string?)element.Attribute(Xaml + "Key") == "RibbonKit.ApplicationMenuHitArea");
        XElement setter = Assert.Single(
            style.Elements(Presentation + "Setter"),
            element => (string?)element.Attribute("Property") == "Foreground");

        Assert.Contains("AncestorType={x:Type controls:RibbonApplicationMenuItem}",
            (string?)setter.Attribute("Value"));
    }

    private static void AssertForegroundBinding(XElement element) =>
        Assert.Equal(
            "{Binding Foreground, RelativeSource={RelativeSource TemplatedParent}}",
            (string?)element.Attribute("Foreground"));

    private static XElement Named(XElement template, string elementName, string name) =>
        Assert.Single(
            template.Descendants(Presentation + elementName),
            element => (string?)element.Attribute(Xaml + "Name") == name);

    private static XElement Template(XDocument document, string targetType) =>
        Assert.Single(
            document.Descendants(Presentation + "ControlTemplate"),
            template => ((string?)template.Attribute("TargetType"))?.Contains(targetType) == true);

    private static string ThemePart(string name) =>
        Path.Combine(RepositoryRoot(), "src", "RibbonKit", "Themes", name);

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, $"Could not find RibbonKit.sln above {AppContext.BaseDirectory}.");
        return directory!.FullName;
    }
}
