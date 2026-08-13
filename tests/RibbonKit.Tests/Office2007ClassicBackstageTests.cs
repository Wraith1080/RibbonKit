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
        AssertSetter(design, "NavColumn", "Margin", "8,8,0,8");
        AssertSetter(design, "NavColumn", "BorderThickness", "1,1,0,1");
        AssertSetter(
            design,
            "ContentArea",
            "Background",
            "{DynamicResource RibbonKit.Brushes.ScreenTip.Background}");
        AssertSetter(design, "ContentArea", "Margin", "0,8,8,8");
        AssertSetter(design, "ContentArea", "Padding", "28,14");
        AssertSetter(design, "ContentArea", "BorderThickness", "0,1,1,1");

        XElement translucent = Assert.Single(
            template.Descendants(Presentation + "MultiTrigger"),
            trigger => HasCondition(trigger, "controls:Backstage.Design", "Classic2007")
                && HasCondition(trigger, "Translucent", "True"));
        AssertSetter(
            translucent,
            "RootGrid",
            "Background",
            "{DynamicResource RibbonKit.Brushes.Ribbon.Background}");

        XElement backButton = Assert.Single(
            template.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "controls:Backstage.Design"
                && (string?)trigger.Attribute("Value") == "Classic2007"
                && trigger.Elements(Presentation + "Setter").Any(
                    setter => (string?)setter.Attribute("TargetName") == "GlassFill"));
        AssertSetter(backButton, "GlassFill", "Visibility", "Visible");
        AssertSetter(backButton, "Arrow", "Stroke", "#FFFFFF");
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
                && HasCondition(trigger, "IsMouseOver", "True"));
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
    }

    private static bool HasCondition(XElement trigger, string property, string value) =>
        trigger
            .Descendants(Presentation + "Condition")
            .Any(condition => (string?)condition.Attribute("Property") == property
                && (string?)condition.Attribute("Value") == value);

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
