using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Contracts for RibbonKit's distinct opaque Classic2007 Backstage concept.</summary>
public class Office2007ClassicBackstageTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly XNamespace RibbonKit = "urn:ribbonkit";

    [Fact]
    public void Classic2007_is_additive_and_preserves_the_Glass2007_serialized_value()
    {
        Assert.Equal(3, (int)RibbonBackstageDesign.Glass2007);
        Assert.Equal(4, (int)RibbonBackstageDesign.Classic2007);
    }

    [Fact]
    public void Shared_template_builds_an_opaque_framed_Classic2007_shell()
    {
        XDocument document = LoadBackstageTemplate();
        XElement template = Assert.Single(
            document.Descendants(Presentation + "ControlTemplate"),
            element => (string?)element.Attribute("TargetType") == "{x:Type controls:Backstage}");
        XElement design = Assert.Single(
            template.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "controls:Backstage.Design"
                && (string?)trigger.Attribute("Value") == "Classic2007"
                && trigger.Elements(Presentation + "Setter").Any(
                    setter => (string?)setter.Attribute("TargetName") == "NavColumn"));

        AssertSetter(
            design,
            "RootGrid",
            "Background",
            "{DynamicResource RibbonKit.Brushes.Ribbon.Background}");
        AssertSetter(
            design,
            "NavColumn",
            "Background",
            "{DynamicResource RibbonKit.Brushes.Backstage.NavBackground}");
        AssertSetter(design, "Classic2007ShellMaterial", "Visibility", "Visible");
        AssertSetter(design, "Classic2007ShellChrome", "Visibility", "Visible");
        AssertSetter(design, "Classic2007PaneDivider", "Visibility", "Visible");
        AssertSetter(design, "Classic2007AeroJoinBevel", "Visibility", "Visible");
        AssertSetter(design, "PART_BackButton", "Visibility", "Collapsed");
        AssertSetter(design, "NavColumn", "Margin", "8,36,0,8");
        AssertSetter(design, "NavColumn", "Padding", "0,4,0,0");
        AssertSetter(design, "NavColumn", "BorderThickness", "0");
        AssertSetter(
            design,
            "ContentArea",
            "Background",
            "{DynamicResource RibbonKit.Brushes.Backstage.NavBackground}");
        AssertSetter(design, "ContentArea", "Margin", "0,36,8,8");
        AssertSetter(design, "ContentArea", "Padding", "20,6");
        AssertSetter(design, "ContentArea", "BorderThickness", "0");

        XElement shellMaterial = NamedElement(document, "Classic2007ShellMaterial");
        Assert.Contains(
            shellMaterial.Elements(Presentation + "Border"),
            border => (string?)border.Attribute("Background")
                == "{DynamicResource RibbonKit.Brushes.Ribbon.Background}");

        XElement shellChrome = NamedElement(document, "Classic2007ShellChrome");
        Assert.Contains(
            shellChrome.Descendants(Presentation + "Border"),
            border => (string?)border.Attribute("BorderBrush")
                == "{DynamicResource RibbonKit.Brushes.Ribbon.Border}");
        XElement innerFrameShadow = NamedElement(document, "Classic2007InnerFrameShadow");
        Assert.Equal("8,36,8,8", (string?)innerFrameShadow.Attribute("Margin"));
        Assert.Equal(
            "{DynamicResource RibbonKit.Effects.Backstage.ContentShadow}",
            (string?)innerFrameShadow.Attribute("Effect"));
        XElement paneDivider = NamedElement(document, "Classic2007PaneDivider");
        Assert.Equal(
            "{DynamicResource RibbonKit.Brushes.Ribbon.Border}",
            (string?)paneDivider.Attribute("Background"));
        Assert.Equal("0,37,0,9", (string?)paneDivider.Attribute("Margin"));
        XElement aeroJoin = NamedElement(document, "Classic2007AeroJoinBevel");
        Assert.Equal("#FFFFFFFF", (string?)aeroJoin.Attribute("BorderBrush"));
        Assert.Equal("1", (string?)aeroJoin.Attribute("BorderThickness"));

        XElement translucent = Assert.Single(
            template.Descendants(Presentation + "MultiTrigger"),
            trigger => HasCondition(trigger, "controls:Backstage.Design", "Classic2007")
                && HasCondition(trigger, "Translucent", "True"));
        AssertSetter(
            translucent,
            "RootGrid",
            "Background",
            "{DynamicResource RibbonKit.Brushes.Ribbon.Background}");

        Assert.DoesNotContain(
            document.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == "Classic2007Orb");
    }

    [Fact]
    public void Classic2007_navigation_uses_large_icons_and_gold_generation_states()
    {
        XDocument document = LoadBackstageTemplate();
        XElement baseTrigger = Assert.Single(
            document.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "controls:Backstage.Design"
                && (string?)trigger.Attribute("Value") == "Classic2007"
                && trigger.Elements(Presentation + "Setter").Any(
                    setter => (string?)setter.Attribute("TargetName") == "NavIconHost"));

        AssertSetter(baseTrigger, "NavIconHost", "Width", "26");
        AssertSetter(baseTrigger, "NavIconHost", "Height", "26");
        AssertSetter(baseTrigger, "Chrome", "Margin", "6,2");
        AssertSetter(
            baseTrigger,
            "NavText",
            "TextElement.Foreground",
            "{DynamicResource RibbonKit.Brushes.Backstage.Classic2007.Foreground}");

        XElement hover = Assert.Single(
            document.Descendants(Presentation + "MultiTrigger"),
            trigger => HasCondition(trigger, "controls:Backstage.Design", "Classic2007")
                && HasCondition(trigger, "IsMouseOver", "True")
                && trigger.Elements(Presentation + "Setter").Any(
                    setter => (string?)setter.Attribute("TargetName") == "Chrome"));
        AssertSetter(
            hover,
            "Chrome",
            "Background",
            "{DynamicResource RibbonKit.Brushes.Control.HoverBackground}");
        AssertSetter(
            hover,
            "Chrome",
            "BorderBrush",
            "{DynamicResource RibbonKit.Brushes.Control.HoverBorder}");

        XElement selected = Assert.Single(
            document.Descendants(Presentation + "MultiTrigger"),
            trigger => HasCondition(trigger, "controls:Backstage.Design", "Classic2007")
                && HasCondition(trigger, "IsSelected", "True"));
        AssertSetter(
            selected,
            "Chrome",
            "Background",
            "{DynamicResource RibbonKit.Brushes.Control.HoverBackground}");
        AssertSetter(selected, "NavText", "TextElement.FontWeight", "SemiBold");
    }

    [Fact]
    public void Ribbon_and_Classic2007_proxy_share_one_orb_chrome_template()
    {
        XDocument document = LoadRibbonChromeTemplate();
        XElement orbTemplate = Assert.Single(
            document.Root!.Elements(Presentation + "DataTemplate"),
            element => (string?)element.Attribute(Xaml + "Key")
                == "RibbonKit.Templates.ApplicationOrbChrome");
        _ = Assert.Single(
            orbTemplate.Descendants(Presentation + "Ellipse"),
            element => (string?)element.Attribute(Xaml + "Name") == "OrbFill");
        _ = Assert.Single(
            orbTemplate.Descendants(Presentation + "Viewbox"),
            element => (string?)element.Attribute(Xaml + "Name") == "OrbGlyph");

        XElement ribbonOrb = NamedElement(document, "Orb");
        Assert.Equal(
            "{StaticResource RibbonKit.Templates.ApplicationOrbChrome}",
            (string?)ribbonOrb.Attribute("ContentTemplate"));

        Assert.DoesNotContain(
            document.Descendants(Presentation + "Condition"),
            condition => ((string?)condition.Attribute("Binding"))?.Contains(
                "Tag.Backstage.Design",
                System.StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Showcase_exposes_Classic2007_without_replacing_the_modern_Glass2007_page()
    {
        XDocument showcase = XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "samples",
            "RibbonKit.Showcase",
            "MainWindow.xaml"));

        XElement classicChoice = Assert.Single(
            showcase.Descendants(RibbonKit + "RibbonButton"),
            element => (string?)element.Attribute("Tag") == "Classic2007");
        Assert.Equal("2007 Classic", (string?)classicChoice.Attribute("Header"));
        XElement modernChoice = Assert.Single(
            showcase.Descendants(RibbonKit + "RibbonButton"),
            element => (string?)element.Attribute("Tag") == "Glass2007");
        Assert.Equal("2007 Modern", (string?)modernChoice.Attribute("Header"));

        _ = Assert.Single(
            showcase.Descendants(Presentation + "TextBlock"),
            element => (string?)element.Attribute("Text") == "Good morning");
        XElement dashboardTitle = Assert.Single(
            showcase.Descendants(Presentation + "TextBlock"),
            element => (string?)element.Attribute("Text") == "Information about RibbonKit Showcase");
        Assert.Contains(
            dashboardTitle.Ancestors(Presentation + "ScrollViewer")
                .Descendants(Presentation + "DataTrigger"),
            trigger => (string?)trigger.Attribute("Value") == "Classic2007");
        XElement dashboardGrid = Assert.Single(
            dashboardTitle.Ancestors(Presentation + "Grid"),
            grid => (string?)grid.Attribute("MaxWidth") == "1080");
        Assert.Equal("8", (string?)dashboardGrid.Attribute("Margin"));

        XElement cardStyle = Assert.Single(
            showcase.Descendants(Presentation + "Style"),
            style => (string?)style.Attribute(Xaml + "Key") == "Classic2007Card");
        Assert.Contains(
            cardStyle.Elements(Presentation + "Setter"),
            setter => (string?)setter.Attribute("Property") == "Effect"
                && (string?)setter.Attribute("Value")
                    == "{DynamicResource RibbonKit.Effects.Backstage.ContentShadow}");
    }

    private static bool HasCondition(XElement trigger, string property, string value) =>
        trigger
            .Descendants(Presentation + "Condition")
            .Any(condition => (string?)condition.Attribute("Property") == property
                && (string?)condition.Attribute("Value") == value);

    private static XElement NamedElement(XDocument document, string name) =>
        Assert.Single(
            document.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == name);

    private static void AssertSetter(
        XElement trigger,
        string targetName,
        string property,
        string expectedValue)
    {
        XElement setter = Assert.Single(
            trigger.Elements(Presentation + "Setter"),
            element => (string?)element.Attribute("TargetName") == targetName
                && (string?)element.Attribute("Property") == property);
        Assert.Equal(expectedValue, (string?)setter.Attribute("Value"));
    }

    private static XDocument LoadBackstageTemplate() => XDocument.Load(Path.Combine(
        RepositoryRoot(),
        "src",
        "RibbonKit",
        "Themes",
        "Controls.Backstage.xaml"));

    private static XDocument LoadRibbonChromeTemplate() => XDocument.Load(Path.Combine(
        RepositoryRoot(),
        "src",
        "RibbonKit",
        "Themes",
        "Controls.RibbonChrome.xaml"));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
    }
}
