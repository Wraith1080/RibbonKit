using System.IO;
using System.Xml.Linq;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Contracts for RibbonKit's optional Office 2007 glass Backstage interpretation.</summary>
public class Office2007GlassBackstageTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly XNamespace RibbonKit =
        "urn:ribbonkit";

    [Fact]
    public void Glass2007_is_an_additive_backstage_design()
    {
        Assert.Equal(3, (int)RibbonBackstageDesign.Glass2007);
    }

    [Fact]
    public void Shared_template_provides_opaque_and_translucent_Glass2007_rails()
    {
        XDocument document = LoadBackstageTemplate();
        XElement template = Assert.Single(
            document.Descendants(Presentation + "ControlTemplate"),
            element => (string?)element.Attribute("TargetType") == "{x:Type controls:Backstage}");
        XElement design = Assert.Single(
            template.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "controls:Backstage.Design"
                && (string?)trigger.Attribute("Value") == "Glass2007"
                && trigger.Elements(Presentation + "Setter").Any(
                    setter => (string?)setter.Attribute("TargetName") == "NavColumn"));

        AssertSetter(
            design,
            "NavColumn",
            "Background",
            "{DynamicResource RibbonKit.Brushes.Backstage.NavBackground}");
        AssertSetter(
            design,
            "ContentArea",
            "Effect",
            "{DynamicResource RibbonKit.Effects.Backstage.ContentShadow}");

        XElement translucent = Assert.Single(
            template.Descendants(Presentation + "MultiTrigger"),
            trigger => HasCondition(trigger, "controls:Backstage.Design", "Glass2007")
                && HasCondition(trigger, "Translucent", "True"));
        AssertSetter(translucent, "NavColumn", "Background", "Transparent");
        AssertSetter(translucent, "Glass2007RailTint", "Visibility", "Visible");

        XElement tint = NamedElement(document, "Glass2007RailTint");
        Assert.Equal(
            "{Binding AeroFrameTint, RelativeSource={RelativeSource AncestorType={x:Type controls:RibbonWindow}}}",
            (string?)tint.Attribute("Background"));
        Assert.Contains("AeroFrameTintIntensity", (string?)tint.Attribute("Opacity"));
        Assert.DoesNotContain(
            document.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") is
                "Glass2007RailReflection" or "Glass2007RailGrain");
        XElement backButtonGlass = Assert.Single(
            document.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "controls:Backstage.Design"
                && (string?)trigger.Attribute("Value") == "Glass2007"
                && trigger.Elements(Presentation + "Setter").Any(
                    setter => (string?)setter.Attribute("TargetName") == "GlassFill"));
        AssertSetter(
            backButtonGlass,
            "GlassFill",
            "Opacity",
            "{DynamicResource RibbonKit.Metrics.Backstage.GlassChromeOpacity}");
        AssertSetter(
            backButtonGlass,
            "GlassFill",
            "Fill",
            "{DynamicResource RibbonKit.Brushes.Backstage.ItemSelectedGlass}");
        AssertSetter(
            backButtonGlass,
            "GlassFill",
            "Stroke",
            "{DynamicResource RibbonKit.Brushes.Backstage.ItemSelectedBorder}");
        AssertSetter(backButtonGlass, "Arrow", "Stroke", "#FFFFFF");
        foreach (string themeName in new[]
                 {
                     "Office2007",
                     "Office2010",
                     "Office2013",
                     "Office2019",
                     "Office2024",
                 })
        {
            XDocument theme = XDocument.Load(Path.Combine(
                RepositoryRoot(),
                "src",
                "RibbonKit",
                "Themes",
                $"Tokens.{themeName}.xaml"));
            XElement opacity = Assert.Single(
                theme.Root!.Elements(),
                element => (string?)element.Attribute(Xaml + "Key")
                    == "RibbonKit.Metrics.Backstage.GlassChromeOpacity");
            Assert.Equal("0.88", opacity.Value);
        }
        XElement inactiveMaterial = Assert.Single(
            template.Descendants(Presentation + "MultiDataTrigger"),
            trigger => HasBindingCondition(trigger, "Backstage.Design", "Glass2007")
                && HasBindingCondition(trigger, "Translucent", "True")
                && HasBindingCondition(trigger, "IsActive", "False"));
        AssertSetter(
            inactiveMaterial,
            "Glass2007RailMaterial",
            "Opacity",
            "{DynamicResource RibbonKit.Metrics.WindowFrame.InactiveTitleBarOpacity}");

        XElement opaqueAero = Assert.Single(
            template.Descendants(Presentation + "MultiDataTrigger"),
            trigger => HasBindingCondition(trigger, "Backstage.Design", "Glass2007")
                && HasBindingCondition(trigger, "Translucent", "False")
                && HasBindingCondition(trigger, "FrameAppearance", "Office2007Aero"));
        AssertSetter(opaqueAero, "Glass2007OpaqueRailHighlight", "Visibility", "Visible");
        Assert.Equal(
            "1,1,0,1",
            (string?)NamedElement(document, "Glass2007OpaqueRailHighlight").Attribute("BorderThickness"));
        Assert.DoesNotContain(
            design.Elements(Presentation + "Setter"),
            setter => (string?)setter.Attribute("TargetName") == "ContentArea"
                && ((string?)setter.Attribute("Property"))!.StartsWith("Border", StringComparison.Ordinal));
    }

    [Fact]
    public void Glass2007_navigation_uses_generation_aware_glass_and_gold_tokens()
    {
        XDocument document = LoadBackstageTemplate();
        XElement baseTrigger = Assert.Single(
            document.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "controls:Backstage.Design"
                && (string?)trigger.Attribute("Value") == "Glass2007"
                && trigger.Elements(Presentation + "Setter").Any(
                    setter => (string?)setter.Attribute("TargetName") == "Chrome"));
        AssertSetter(baseTrigger, "Chrome", "CornerRadius", "2");
        AssertSetter(baseTrigger, "Chrome", "Margin", "6,1");
        AssertSetter(
            baseTrigger,
            "NavText",
            "TextElement.Foreground",
            "{DynamicResource RibbonKit.Brushes.WindowFrame.AeroTitleForeground}");
        AssertSetter(
            baseTrigger,
            "NavIcon",
            "Fill",
            "{DynamicResource RibbonKit.Brushes.WindowFrame.AeroTitleForeground}");

        XElement hover = Assert.Single(
            document.Descendants(Presentation + "MultiTrigger"),
            trigger => HasCondition(trigger, "controls:Backstage.Design", "Glass2007")
                && HasCondition(trigger, "IsMouseOver", "True"));
        AssertSetter(
            hover,
            "Glass2007StateChrome",
            "Background",
            "{DynamicResource RibbonKit.Brushes.Control.HoverBackground}");
        AssertSetter(hover, "Glass2007StateChrome", "Visibility", "Visible");
        AssertSetter(
            hover,
            "Chrome",
            "BorderBrush",
            "{DynamicResource RibbonKit.Brushes.Control.HoverBorder}");

        XElement selected = Assert.Single(
            document.Descendants(Presentation + "MultiTrigger"),
            trigger => HasCondition(trigger, "controls:Backstage.Design", "Glass2007")
                && HasCondition(trigger, "IsSelected", "True"));
        AssertSetter(
            selected,
            "Glass2007StateChrome",
            "Background",
            "{DynamicResource RibbonKit.Brushes.Backstage.ItemSelectedGlass}");
        Assert.Equal(
            "{DynamicResource RibbonKit.Metrics.Backstage.GlassChromeOpacity}",
            (string?)NamedElement(document, "Glass2007StateChrome").Attribute("Opacity"));
        AssertSetter(
            selected,
            "Chrome",
            "BorderBrush",
            "{DynamicResource RibbonKit.Brushes.Backstage.ItemSelectedBorder}");
        Assert.DoesNotContain(
            document.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == "Glass2007InnerRim");
    }

    [Fact]
    public void Showcase_exposes_Glass2007_and_resets_frame_tint_to_the_shared_default()
    {
        string root = RepositoryRoot();
        var showcase = XDocument.Load(Path.Combine(
            root,
            "samples",
            "RibbonKit.Showcase",
            "MainWindow.xaml"));
        XElement choice = Assert.Single(
            showcase.Descendants(RibbonKit + "RibbonButton"),
            element => (string?)element.Attribute("Tag") == "Glass2007");
        Assert.Equal("2007 Glass", (string?)choice.Attribute("Header"));

        XElement slider = NamedElement(showcase, "AeroFrameTintIntensitySlider");
        Assert.Equal("0.16", (string?)slider.Attribute("Value"));
        XElement reset = Assert.Single(
            showcase.Descendants(Presentation + "Button"),
            element => (string?)element.Attribute("Click") == "OnResetAeroFrameTintIntensity");
        Assert.Equal("Reset", (string?)reset.Attribute("Content"));
        Assert.Equal("18", (string?)reset.Attribute("Height"));

        string code = File.ReadAllText(Path.Combine(
            root,
            "samples",
            "RibbonKit.Showcase",
            "MainWindow.xaml.cs"));
        Assert.Contains(
            "ShowcaseAppearancePreferences.DefaultAeroFrameTintIntensity",
            code,
            StringComparison.Ordinal);
    }

    private static bool HasCondition(XElement trigger, string property, string value) =>
        trigger
            .Descendants(Presentation + "Condition")
            .Any(condition => (string?)condition.Attribute("Property") == property
                && (string?)condition.Attribute("Value") == value);

    private static bool HasBindingCondition(XElement trigger, string bindingFragment, string value) =>
        trigger
            .Descendants(Presentation + "Condition")
            .Any(condition => ((string?)condition.Attribute("Binding"))?.Contains(
                    bindingFragment,
                    StringComparison.Ordinal) == true
                && (string?)condition.Attribute("Value") == value);

    private static void AssertSetter(
        XElement trigger,
        string targetName,
        string property,
        string value)
    {
        XElement setter = Assert.Single(
            trigger.Elements(Presentation + "Setter"),
            element => (string?)element.Attribute("TargetName") == targetName
                && (string?)element.Attribute("Property") == property);
        Assert.Equal(value, (string?)setter.Attribute("Value"));
    }

    private static XElement NamedElement(XDocument document, string name) =>
        Assert.Single(
            document.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == name);

    private static XDocument LoadBackstageTemplate() =>
        XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "src",
            "RibbonKit",
            "Themes",
            "Controls.Backstage.xaml"));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        return Assert.IsType<DirectoryInfo>(directory).FullName;
    }
}
