using System.IO;
using System.Windows.Media;
using System.Xml.Linq;
using RibbonKit.Controls;
using RibbonKit.Interop;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Contracts for the opaque and opt-in Aero-inspired Office 2007 window treatments.</summary>
public class Office2007WindowFrameTests
{
    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    private static readonly string[] RemovedGlobalFrameKeys =
    [
        "RibbonKit.Brushes.WindowFrame.Outline",
        "RibbonKit.Brushes.WindowFrame.InactiveOutline",
        "RibbonKit.Brushes.WindowFrame.Band",
        "RibbonKit.Brushes.WindowFrame.InactiveBand",
        "RibbonKit.Metrics.WindowFrame.OutlineThickness",
        "RibbonKit.Metrics.WindowFrame.BandThickness",
        "RibbonKit.Metrics.WindowFrame.MaximizedTopBandHeight",
    ];

    private static readonly string[] AeroBrushKeys =
    [
        "RibbonKit.Brushes.WindowFrame.AeroFallback",
        "RibbonKit.Brushes.WindowFrame.AeroInactiveFallback",
        "RibbonKit.Brushes.WindowFrame.AeroTint",
        "RibbonKit.Brushes.WindowFrame.AeroInnerHighlight",
        "RibbonKit.Brushes.WindowFrame.AeroReflection",
        "RibbonKit.Brushes.WindowFrame.AeroGrain",
        "RibbonKit.Brushes.WindowFrame.AeroTitleForeground",
        "RibbonKit.Brushes.WindowFrame.AeroCaptionHover",
        "RibbonKit.Brushes.WindowFrame.AeroCaptionPressed",
    ];

    [Fact]
    public void Office_2007_tokens_define_the_opaque_title_state_without_a_global_frame_band()
    {
        XDocument theme = LoadTheme("Tokens.Office2007.xaml");

        Assert.Equal("34", Value(theme, "RibbonKit.Metrics.WindowFrame.CaptionHeight"));
        Assert.Equal("0.78", Value(theme, "RibbonKit.Metrics.WindowFrame.InactiveTitleBarOpacity"));
        Assert.Equal("#9BBBE3", Attribute(theme, AeroBrushKeys[0], "Color"));
        Assert.Equal("#BCC8D6", Attribute(theme, AeroBrushKeys[1], "Color"));
        Assert.Equal("#1A78A5", Attribute(theme, AeroBrushKeys[2], "Color"));
        Assert.Equal("6,0,6,6", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroThickness"));
        Assert.Equal("0.16", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroTintIntensity"));
        Assert.Equal("0,34,0,0", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroInnerHighlightMargin"));
        Assert.Equal("1,0,1,1", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroInnerHighlightThickness"));
        Assert.Equal("0.40", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroReflectionOpacity"));
        Assert.Equal("0.10", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroGrainOpacity"));
        Assert.Equal("0.58", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroInactiveOverlayOpacity"));
        Assert.Equal("1", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroInactiveBackstageFrameOpacity"));
        Assert.Equal("0", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroMaterialTitleBackgroundOpacity"));
        AssertRemovedGlobalFrameKeysAreAbsent(theme);
    }

    [Fact]
    public void Office_2007_black_does_not_reintroduce_global_frame_resources()
    {
        XDocument theme = LoadTheme("Tokens.Office2007.Dark.xaml");

        AssertRemovedGlobalFrameKeysAreAbsent(theme);
    }

    [Theory]
    [InlineData("Tokens.Office2010.xaml")]
    [InlineData("Tokens.Office2013.xaml")]
    [InlineData("Tokens.Office2019.xaml")]
    [InlineData("Tokens.Office2024.xaml")]
    public void Other_generations_keep_the_shared_title_contract_neutral(string themeFile)
    {
        XDocument theme = LoadTheme(themeFile);

        Assert.Equal("34", Value(theme, "RibbonKit.Metrics.WindowFrame.CaptionHeight"));
        Assert.Equal("1", Value(theme, "RibbonKit.Metrics.WindowFrame.InactiveTitleBarOpacity"));
        foreach (string key in AeroBrushKeys.Take(6))
        {
            Assert.Equal("Transparent", Attribute(theme, key, "Color"));
        }

        Assert.Equal("0", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroThickness"));
        Assert.Equal("0", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroTintIntensity"));
        Assert.Equal("0", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroInnerHighlightMargin"));
        Assert.Equal("0", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroInnerHighlightThickness"));
        Assert.Equal("0", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroReflectionOpacity"));
        Assert.Equal("0", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroGrainOpacity"));
        Assert.Equal("1", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroInactiveOverlayOpacity"));
        Assert.Equal("1", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroInactiveBackstageFrameOpacity"));
        Assert.Equal("1", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroMaterialTitleBackgroundOpacity"));
        AssertRemovedGlobalFrameKeysAreAbsent(theme);
    }

    [Fact]
    public void Shared_window_template_keeps_the_measured_window_root_flush_with_the_physical_host()
    {
        XDocument template = LoadTheme("Controls.Window.xaml");
        XElement physicalHost = NamedElement(template, "PhysicalWindowFrameHost");
        XElement aeroFrameHost = NamedElement(template, "AeroFrameHost");
        XElement windowRoot = NamedElement(template, "PART_WindowRoot");
        XElement titleBackground = NamedElement(template, "TitleBarBackgroundLayer");
        XElement windowChrome = Assert.Single(
            template.Descendants(),
            element => element.Name.LocalName == "WindowChrome");

        Assert.Same(physicalHost, aeroFrameHost.Parent);
        Assert.Same(aeroFrameHost, windowRoot.Parent);
        Assert.Equal("0", (string?)aeroFrameHost.Attribute("BorderThickness"));
        Assert.Equal(
            "{DynamicResource RibbonKit.Metrics.WindowFrame.CaptionHeight}",
            (string?)windowChrome.Attribute("CaptionHeight"));
        Assert.Equal(
            "{DynamicResource RibbonKit.Brushes.TitleBar.Background}",
            (string?)titleBackground.Attribute("Background"));

        XElement titleTint = NamedElement(template, "AeroTitleTintLayer");
        XElement frameTint = NamedElement(template, "AeroFrameTintLayer");
        Assert.Equal("{TemplateBinding AeroFrameTint}", (string?)titleTint.Attribute("Background"));
        Assert.Equal(
            "{TemplateBinding AeroFrameTintIntensity}",
            (string?)titleTint.Attribute("Opacity"));
        Assert.Equal("{TemplateBinding AeroFrameTint}", (string?)frameTint.Attribute("BorderBrush"));
        Assert.Equal(
            "{TemplateBinding AeroFrameTintIntensity}",
            (string?)frameTint.Attribute("Opacity"));

        XElement innerHighlight = NamedElement(template, "AeroFrameInnerHighlight");
        Assert.Equal(
            "{DynamicResource RibbonKit.Metrics.WindowFrame.AeroInnerHighlightMargin}",
            (string?)innerHighlight.Attribute("Margin"));
        XElement titleHighlight = NamedElement(template, "AeroTitleBottomHighlight");
        Assert.Equal("TitleBarBand", (string?)titleHighlight.Parent!.Attribute(Xaml + "Name"));
        Assert.Equal("10", (string?)titleHighlight.Attribute("Panel.ZIndex"));
        Assert.Equal("Collapsed", (string?)titleHighlight.Attribute("Visibility"));
        foreach (string layerName in new[]
                 {
                     "AeroTitleReflectionLayer",
                     "AeroTitleGrainLayer",
                     "AeroFrameReflectionLayer",
                     "AeroFrameGrainLayer",
                 })
        {
            Assert.Equal("Collapsed", (string?)NamedElement(template, layerName).Attribute("Visibility"));
        }

        Assert.DoesNotContain(
            template.Descendants(),
            element => RemovedGlobalFrameKeys.Contains((string?)element.Attribute(Xaml + "Key"))
                || (string?)element.Attribute(Xaml + "Name") is "WindowFrameOutline"
                    or "WindowFrameBand"
                    or "MaximizedWindowTopBand");
    }

    [Fact]
    public void Maximized_inactive_and_backstage_triggers_preserve_the_expected_frame_layers()
    {
        XDocument template = LoadTheme("Controls.Window.xaml");
        XElement controlTemplate = Assert.Single(
            template.Descendants(),
            element => element.Name.LocalName == "ControlTemplate"
                && (string?)element.Attribute("TargetType") == "{x:Type controls:RibbonWindow}");

        XElement maximized = Trigger(controlTemplate, "WindowState", "Maximized");
        AssertSetter(maximized, "AeroFrameHost", "BorderThickness", "0");
        AssertSetter(maximized, "AeroFrameOverlay", "Visibility", "Collapsed");

        XElement inactive = Trigger(controlTemplate, "IsActive", "False");
        AssertSetter(
            inactive,
            "TitleBarBand",
            "Opacity",
            "{DynamicResource RibbonKit.Metrics.WindowFrame.InactiveTitleBarOpacity}");

        XElement backstage = Trigger(controlTemplate, "IsTitleBarContentVisible", "False");
        AssertSetter(backstage, "TitleBarContentHost", "Visibility", "Collapsed");
        AssertSetter(backstage, "AeroFrameInnerHighlight", "Visibility", "Collapsed");
        AssertSetter(backstage, "AeroTitleBottomHighlight", "Visibility", "Collapsed");
        AssertSetter(backstage, "AeroTitleReflectionLayer", "Visibility", "Collapsed");
        AssertSetter(backstage, "AeroTitleGrainLayer", "Visibility", "Collapsed");
        AssertSetter(backstage, "AeroFrameReflectionLayer", "Visibility", "Collapsed");
        AssertSetter(backstage, "AeroFrameGrainLayer", "Visibility", "Collapsed");

        XElement inactiveBackstage = Assert.Single(
            controlTemplate.Descendants(),
            element => element.Name.LocalName == "MultiTrigger"
                && HasCondition(element, "FrameAppearance", "Office2007Aero")
                && HasCondition(element, "IsActive", "False")
                && HasCondition(element, "IsTitleBarContentVisible", "False"));
        AssertSetter(
            inactiveBackstage,
            "AeroFrameOverlay",
            "Opacity",
            "{DynamicResource RibbonKit.Metrics.WindowFrame.AeroInactiveBackstageFrameOpacity}");
    }

    [Fact]
    public void Aero_appearance_and_accepted_acrylic_are_separate_template_conditions()
    {
        XDocument template = LoadTheme("Controls.Window.xaml");
        XElement controlTemplate = Assert.Single(
            template.Descendants(),
            element => element.Name.LocalName == "ControlTemplate"
                && (string?)element.Attribute("TargetType") == "{x:Type controls:RibbonWindow}");

        XElement aero = Trigger(controlTemplate, "FrameAppearance", "Office2007Aero");
        AssertSetter(
            aero,
            "AeroFrameHost",
            "BorderThickness",
            "{DynamicResource RibbonKit.Metrics.WindowFrame.AeroThickness}");
        AssertSetter(aero, "AeroFrameOverlay", "Visibility", "Visible");
        AssertSetter(aero, "AeroTitleVisual", "Visibility", "Visible");
        AssertSetter(aero, "AeroTitleBottomHighlight", "Visibility", "Visible");
        AssertSetter(
            aero,
            "TitleBarBackgroundLayer",
            "Background",
            "{DynamicResource RibbonKit.Brushes.WindowFrame.AeroFallback}");

        XElement inactiveAero = Assert.Single(
            controlTemplate.Descendants(),
            element => element.Name.LocalName == "MultiTrigger"
                && element.Descendants().Any(condition => condition.Name.LocalName == "Condition"
                    && (string?)condition.Attribute("Property") == "FrameAppearance"
                    && (string?)condition.Attribute("Value") == "Office2007Aero")
                && element.Descendants().Any(condition => condition.Name.LocalName == "Condition"
                    && (string?)condition.Attribute("Property") == "IsActive"
                    && (string?)condition.Attribute("Value") == "False")
                && !HasCondition(element, "IsTitleBarContentVisible", "True")
                && !HasCondition(element, "IsTitleBarContentVisible", "False"));
        AssertSetter(
            inactiveAero,
            "TitleBarBackgroundLayer",
            "Background",
            "{DynamicResource RibbonKit.Brushes.WindowFrame.AeroInactiveFallback}");
        foreach (string layerName in new[]
                 {
                     "AeroTitleReflectionLayer",
                     "AeroTitleGrainLayer",
                     "AeroFrameReflectionLayer",
                     "AeroFrameGrainLayer",
                 })
        {
            AssertSetter(inactiveAero, layerName, "Visibility", "Collapsed");
        }
        foreach (string contentName in new[]
                 {
                     "PART_WindowIcon",
                     "TitleBarContentHost",
                     "PART_Title",
                     "PART_MinimizeButton",
                     "PART_MaximizeButton",
                     "PART_RestoreButton",
                     "PART_CloseButton",
                 })
        {
            AssertSetter(
                inactiveAero,
                contentName,
                "Opacity",
                "{DynamicResource RibbonKit.Metrics.WindowFrame.InactiveTitleBarOpacity}");
        }

        XElement inactiveNormalAero = Assert.Single(
            controlTemplate.Descendants(),
            element => element.Name.LocalName == "MultiTrigger"
                && HasCondition(element, "FrameAppearance", "Office2007Aero")
                && HasCondition(element, "IsActive", "False")
                && HasCondition(element, "IsTitleBarContentVisible", "True"));
        AssertSetter(inactiveNormalAero, "TitleBarBand", "Opacity", "1");
        AssertSetter(
            inactiveNormalAero,
            "AeroTitleVisual",
            "Opacity",
            "{DynamicResource RibbonKit.Metrics.WindowFrame.AeroInactiveOverlayOpacity}");

        XElement material = Assert.Single(
            controlTemplate.Descendants(),
            element => element.Name.LocalName == "MultiTrigger"
                && element.Descendants().Any(condition => condition.Name.LocalName == "Condition"
                    && (string?)condition.Attribute("Property") == "FrameAppearance"
                    && (string?)condition.Attribute("Value") == "Office2007Aero")
                && element.Descendants().Any(condition => condition.Name.LocalName == "Condition"
                    && (string?)condition.Attribute("Property") == "ActiveBackdrop"
                    && (string?)condition.Attribute("Value") == "Acrylic")
                && element.Elements().Any(setter => setter.Name.LocalName == "Setter"
                    && (string?)setter.Attribute("TargetName") == "AeroFrameHost"));
        AssertSetter(material, "AeroFrameHost", "BorderBrush", "Transparent");
        AssertSetter(
            material,
            "TitleBarBackgroundLayer",
            "Opacity",
            "{DynamicResource RibbonKit.Metrics.WindowFrame.AeroMaterialTitleBackgroundOpacity}");

        XElement activeMaterialDecoration = Assert.Single(
            controlTemplate.Descendants(),
            element => element.Name.LocalName == "MultiTrigger"
                && element.Descendants().Any(condition => condition.Name.LocalName == "Condition"
                    && (string?)condition.Attribute("Property") == "FrameAppearance"
                    && (string?)condition.Attribute("Value") == "Office2007Aero")
                && element.Descendants().Any(condition => condition.Name.LocalName == "Condition"
                    && (string?)condition.Attribute("Property") == "ActiveBackdrop"
                    && (string?)condition.Attribute("Value") == "Acrylic")
                && element.Descendants().Any(condition => condition.Name.LocalName == "Condition"
                    && (string?)condition.Attribute("Property") == "IsActive"
                    && (string?)condition.Attribute("Value") == "True")
                && element.Descendants().Any(condition => condition.Name.LocalName == "Condition"
                    && (string?)condition.Attribute("Property") == "IsTitleBarContentVisible"
                    && (string?)condition.Attribute("Value") == "True"));
        foreach (string layerName in new[]
                 {
                     "AeroTitleReflectionLayer",
                     "AeroTitleGrainLayer",
                     "AeroFrameReflectionLayer",
                     "AeroFrameGrainLayer",
                 })
        {
            AssertSetter(activeMaterialDecoration, layerName, "Visibility", "Visible");
        }
    }

    [Fact]
    public void Frame_appearance_does_not_enable_a_system_backdrop()
    {
        Sta.Run(() =>
        {
            var window = new RibbonWindow();

            Assert.Equal(RibbonWindowFrameAppearance.Default, window.FrameAppearance);
            Assert.Equal(RibbonBackdrop.None, window.ActiveBackdrop);
            Assert.Null(window.AeroFrameTint);
            Assert.Equal(0d, window.AeroFrameTintIntensity);

            window.FrameAppearance = RibbonWindowFrameAppearance.Office2007Aero;
            var tint = new SolidColorBrush(Colors.Purple);
            window.AeroFrameTint = tint;
            window.AeroFrameTintIntensity = 0.35;

            Assert.Equal(RibbonWindowFrameAppearance.Office2007Aero, window.FrameAppearance);
            Assert.Equal(RibbonBackdrop.None, window.ActiveBackdrop);
            Assert.Same(tint, window.AeroFrameTint);
            Assert.Equal(0.35, window.AeroFrameTintIntensity);

            window.SetActiveBackdrop(RibbonBackdrop.Acrylic);
            Assert.Equal(RibbonBackdrop.Acrylic, window.ActiveBackdrop);

            Assert.Throws<ArgumentException>(() => window.AeroFrameTintIntensity = -0.01);
            Assert.Throws<ArgumentException>(() => window.AeroFrameTintIntensity = 1.01);
            Assert.Throws<ArgumentException>(() => window.AeroFrameTintIntensity = double.NaN);
        });
    }

    private static void AssertRemovedGlobalFrameKeysAreAbsent(XDocument document)
    {
        foreach (string key in RemovedGlobalFrameKeys)
        {
            Assert.DoesNotContain(
                document.Root!.Elements(),
                element => (string?)element.Attribute(Xaml + "Key") == key);
        }
    }

    private static XElement Trigger(XElement template, string property, string value) =>
        Assert.Single(
            template.Descendants(),
            element => element.Name.LocalName == "Trigger"
                && (string?)element.Attribute("Property") == property
                && (string?)element.Attribute("Value") == value);

    private static bool HasCondition(XElement trigger, string property, string value) =>
        trigger
            .Descendants()
            .Any(condition => condition.Name.LocalName == "Condition"
                && (string?)condition.Attribute("Property") == property
                && (string?)condition.Attribute("Value") == value);

    private static void AssertSetter(
        XElement trigger,
        string targetName,
        string property,
        string value)
    {
        XElement setter = Assert.Single(
            trigger.Elements(),
            element => element.Name.LocalName == "Setter"
                && (string?)element.Attribute("TargetName") == targetName
                && (string?)element.Attribute("Property") == property);

        Assert.Equal(value, (string?)setter.Attribute("Value"));
    }

    private static string Value(XDocument document, string key) => Resource(document, key).Value;

    private static string Attribute(XDocument document, string key, string attribute) =>
        Assert.IsType<XAttribute>(Resource(document, key).Attribute(attribute)).Value;

    private static XElement Resource(XDocument document, string key) =>
        Assert.Single(
            document.Root!.Elements(),
            element => (string?)element.Attribute(Xaml + "Key") == key);

    private static XElement NamedElement(XDocument document, string name) =>
        Assert.Single(
            document.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == name);

    private static XDocument LoadTheme(string fileName) =>
        XDocument.Load(Path.Combine(ThemesDirectory(), fileName));

    private static string ThemesDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(Assert.IsType<DirectoryInfo>(directory).FullName, "src", "RibbonKit", "Themes");
    }
}
