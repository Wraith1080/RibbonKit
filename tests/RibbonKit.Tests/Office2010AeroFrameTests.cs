using System.IO;
using System.Linq;
using System.Xml.Linq;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Contracts for the opt-in, continuous Office 2010 title/tab Aero surface.</summary>
public sealed class Office2010AeroFrameTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Office2010_tokens_supply_frame_geometry_and_translucent_hover_glass()
    {
        XDocument theme = LoadTheme("Tokens.Office2010.xaml");

        Assert.Equal("#9FBAD8", Attribute(theme, "RibbonKit.Brushes.WindowFrame.AeroFallback", "Color"));
        Assert.Equal("#A06C829D", Attribute(theme, "RibbonKit.Brushes.WindowFrame.AeroInnerHighlight", "Color"));
        Assert.Equal("6,0,6,6", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroThickness"));
        Assert.Equal("0,69,0,0", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroInnerHighlightMargin"));
        Assert.Equal("1,0,1,1", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroInnerHighlightThickness"));
        Assert.Equal("0", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroMaterialTitleBackgroundOpacity"));
        Assert.Equal("1", Value(theme, "RibbonKit.Metrics.WindowFrame.AeroCaptionBorderThickness"));
        Assert.Equal("1", Value(theme, "RibbonKit.Metrics.ContentBorderThickness"));
        Assert.Equal("2,2,2,0", Value(theme, "RibbonKit.Metrics.ApplicationButtonMargin"));
        Assert.Equal("22,7,22,9", Value(theme, "RibbonKit.Metrics.ApplicationButtonPadding"));
        Assert.Equal("0,2,2,0", Value(theme, "RibbonKit.Metrics.ApplicationButtonAeroMargin"));
        Assert.Equal("24,7,22,9", Value(theme, "RibbonKit.Metrics.ApplicationButtonAeroPadding"));
        AssertReadabilityResourcesAreAbsent(theme);
        foreach (string hoverKey in new[]
                 {
                     "RibbonKit.Brushes.Tab.HoverBackground",
                     "RibbonKit.Brushes.TabStrip.ControlHoverBackground",
                 })
        {
            Assert.All(
                Resource(theme, hoverKey).Elements(Presentation + "GradientStop"),
                stop => Assert.Matches("^#[0-9A-Fa-f]{8}$", stop.Attribute("Color")!.Value));
            Assert.DoesNotContain(
                Resource(theme, hoverKey).Elements(Presentation + "GradientStop"),
                stop => stop.Attribute("Color")!.Value.StartsWith("#FF", StringComparison.OrdinalIgnoreCase));
        }
        Assert.Equal(
            "#A8FBE9B0",
            Attribute(theme, "RibbonKit.Brushes.Tab.ConnectFootHover", "Color"));
        Assert.Equal(
            "#FFD39E28",
            Attribute(theme, "RibbonKit.Brushes.Tab.HoverBorder", "Color"));
        Assert.Equal(
            "#40FFFFFF",
            Attribute(theme, "RibbonKit.Brushes.WindowFrame.AeroCaptionHover", "Color"));
        Assert.Equal(
            "#FF8C8C8C",
            Attribute(theme, "RibbonKit.Brushes.WindowFrame.AeroCaptionHoverBorder", "Color"));
        Assert.Equal(
            "#70E81123",
            Attribute(theme, "RibbonKit.Brushes.WindowFrame.AeroCaptionCloseHover", "Color"));
        Assert.Equal(
            "#FFE81123",
            Attribute(theme, "RibbonKit.Brushes.WindowFrame.AeroCaptionCloseHoverBorder", "Color"));

        XDocument dark = LoadTheme("Tokens.Office2010.Dark.xaml");
        Assert.Equal(
            "#F2F2F2",
            Attribute(dark, "RibbonKit.Brushes.WindowFrame.AeroTitleForeground", "Color"));
        Assert.Equal(
            "#FFF0C85E",
            Attribute(dark, "RibbonKit.Brushes.Tab.HoverBorder", "Color"));
        AssertReadabilityResourcesAreAbsent(dark);
    }

    [Fact]
    public void Window_template_exposes_the_2010_frame_without_a_title_only_seam_or_decoration()
    {
        Assert.Equal(2, (int)RibbonWindowFrameAppearance.Office2010Aero);

        XDocument template = LoadTheme("Controls.Window.xaml");
        XElement controlTemplate = WindowTemplate(template);
        XElement trigger = Trigger(controlTemplate, "FrameAppearance", "Office2010Aero");

        AssertSetter(
            trigger,
            "AeroFrameHost",
            "BorderThickness",
            "{DynamicResource RibbonKit.Metrics.WindowFrame.AeroThickness}");
        AssertSetter(trigger, "AeroFrameOverlay", "Visibility", "Visible");
        AssertSetter(trigger, "AeroTitleVisual", "Visibility", "Visible");
        Assert.DoesNotContain(
            trigger.Elements(Presentation + "Setter"),
            setter => (string?)setter.Attribute("TargetName") == "AeroTitleBottomHighlight"
                && (string?)setter.Attribute("Property") == "Visibility"
                && (string?)setter.Attribute("Value") == "Visible");

        Assert.DoesNotContain(
            template.Descendants(),
            element => ((string?)element.Attribute(Xaml + "Name"))?.Contains(
                "Readability",
                StringComparison.Ordinal) == true);

        XElement material = Assert.Single(
            controlTemplate.Descendants(Presentation + "MultiTrigger"),
            candidate => HasCondition(candidate, "FrameAppearance", "Office2010Aero")
                && HasCondition(candidate, "ActiveBackdrop", "Acrylic")
                && candidate.Elements(Presentation + "Setter").Any(
                    setter => (string?)setter.Attribute("TargetName") == "AeroFrameHost"));
        AssertSetter(material, "AeroFrameHost", "BorderBrush", "Transparent");
        Assert.DoesNotContain(
            controlTemplate.Descendants(Presentation + "MultiTrigger"),
            candidate => HasCondition(candidate, "FrameAppearance", "Office2010Aero")
                && HasCondition(candidate, "ActiveBackdrop", "Acrylic")
                && candidate.Elements(Presentation + "Setter").Any(
                    setter => (string?)setter.Attribute("TargetName") is
                        "AeroTitleReflectionLayer" or "AeroTitleGrainLayer"
                        or "AeroFrameReflectionLayer" or "AeroFrameGrainLayer"));

        XElement captionStyle = Assert.Single(
            controlTemplate.Descendants(Presentation + "Style"),
            style => (string?)style.Attribute(Xaml + "Key") == "CaptionButtonStyle");
        XElement captionTemplate = Assert.Single(
            captionStyle.Descendants(Presentation + "ControlTemplate"));
        XElement captionHover = Assert.Single(
            captionTemplate.Descendants(Presentation + "MultiTrigger"),
            candidate => HasCondition(
                    candidate,
                    "Tag",
                    "{x:Static controls:RibbonWindowFrameAppearance.Office2010Aero}")
                && HasCondition(candidate, "IsMouseOver", "True"));
        AssertSetter(
            captionHover,
            "Office2010StateChrome",
            "Background",
            "{DynamicResource RibbonKit.Brushes.WindowFrame.AeroCaptionHover}");
        AssertSetter(
            captionHover,
            "Office2010StateChrome",
            "BorderBrush",
            "{DynamicResource RibbonKit.Brushes.WindowFrame.AeroCaptionHoverBorder}");
        AssertSetter(
            captionHover,
            "Office2010StateChrome",
            "BorderThickness",
            "{DynamicResource RibbonKit.Metrics.WindowFrame.AeroCaptionBorderThickness}");
        AssertSetter(captionHover, "Chrome", "Visibility", "Collapsed");
        AssertSetter(captionHover, "Office2010StateChrome", "Visibility", "Visible");
        Assert.Equal(
            "{TemplateBinding FrameAppearance}",
            (string?)NamedElement(template, "PART_RestoreButton").Attribute("Tag"));

        XElement closeStyle = Assert.Single(
            controlTemplate.Descendants(Presentation + "Style"),
            style => (string?)style.Attribute(Xaml + "Key") == "CloseCaptionButtonStyle");
        XElement closeTemplate = Assert.Single(
            closeStyle.Descendants(Presentation + "ControlTemplate"));
        XElement closeHover = Assert.Single(
            closeTemplate.Descendants(Presentation + "MultiTrigger"),
            candidate => HasCondition(
                    candidate,
                    "Tag",
                    "{x:Static controls:RibbonWindowFrameAppearance.Office2010Aero}")
                && HasCondition(candidate, "IsMouseOver", "True"));
        AssertSetter(
            closeHover,
            "Office2010StateChrome",
            "Background",
            "{DynamicResource RibbonKit.Brushes.WindowFrame.AeroCaptionCloseHover}");
        AssertSetter(
            closeHover,
            "Office2010StateChrome",
            "BorderBrush",
            "{DynamicResource RibbonKit.Brushes.WindowFrame.AeroCaptionCloseHoverBorder}");
        AssertSetter(closeHover, "Chrome", "Visibility", "Collapsed");
        AssertSetter(closeHover, "Office2010StateChrome", "Visibility", "Visible");
    }

    [Fact]
    public void Ribbon_tab_row_uses_the_same_fallback_and_live_tint_without_an_authored_blur_box()
    {
        XDocument chrome = LoadTheme("Controls.RibbonChrome.xaml");

        XElement ribbonSurface = NamedElement(chrome, "RibbonSurface");
        XElement ribbonTemplate = Assert.Single(
            ribbonSurface.Ancestors(Presentation + "ControlTemplate"));
        XElement transparentSurface = Assert.Single(
            ribbonTemplate.Descendants(Presentation + "DataTrigger"),
            trigger => BindingContains(trigger, "FrameAppearance")
                && (string?)trigger.Attribute("Value") == "Office2010Aero");
        AssertSetter(transparentSurface, "RibbonSurface", "Background", "Transparent");

        XElement material = NamedElement(chrome, "Office2010TabMaterialLayer");
        XElement tint = NamedElement(chrome, "Office2010TabTintLayer");
        Assert.Equal("Collapsed", (string?)material.Attribute("Visibility"));
        Assert.Equal("Collapsed", (string?)tint.Attribute("Visibility"));
        Assert.Equal(
            "{DynamicResource RibbonKit.Brushes.WindowFrame.AeroFallback}",
            (string?)material.Attribute("Background"));
        Assert.Contains("AeroFrameTint", (string?)tint.Attribute("Background"), StringComparison.Ordinal);
        Assert.Contains("AeroFrameTintIntensity", (string?)tint.Attribute("Opacity"), StringComparison.Ordinal);
        Assert.DoesNotContain(
            chrome.Descendants(),
            element => ((string?)element.Attribute(Xaml + "Name"))?.Contains(
                "Readability",
                StringComparison.Ordinal) == true);

        XElement tabTemplate = Assert.Single(
            material.Ancestors(Presentation + "ControlTemplate"));
        XElement appearance = Assert.Single(
            tabTemplate.Descendants(Presentation + "DataTrigger"),
            trigger => BindingContains(trigger, "FrameAppearance")
                && (string?)trigger.Attribute("Value") == "Office2010Aero");
        AssertSetter(appearance, "Office2010TabMaterialLayer", "Visibility", "Visible");
        AssertSetter(appearance, "Office2010TabTintLayer", "Visibility", "Visible");
        AssertSetter(
            appearance,
            "PART_ApplicationButton",
            "Margin",
            "{DynamicResource RibbonKit.Metrics.ApplicationButtonAeroMargin}");
        AssertSetter(
            appearance,
            "PART_ApplicationButton",
            "Padding",
            "{DynamicResource RibbonKit.Metrics.ApplicationButtonAeroPadding}");
        XElement contentHost = NamedElement(chrome, "ContentHost");
        Assert.Equal(
            "{DynamicResource RibbonKit.Metrics.ContentBorderThickness}",
            (string?)contentHost.Attribute("BorderThickness"));
        Assert.DoesNotContain(
            chrome.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == "Office2010ContentRightBorder");
        XElement acrylic = Assert.Single(
            tabTemplate.Descendants(Presentation + "MultiDataTrigger"),
            trigger => HasBindingCondition(trigger, "FrameAppearance", "Office2010Aero")
                && HasBindingCondition(trigger, "ActiveBackdrop", "Acrylic"));
        AssertSetter(
            acrylic,
            "Office2010TabMaterialLayer",
            "Opacity",
            "{DynamicResource RibbonKit.Metrics.WindowFrame.AeroMaterialTitleBackgroundOpacity}");

        XDocument groups = LoadTheme("Controls.Groups.xaml");
        XElement groupsTabTemplate = Assert.Single(
            groups.Descendants(Presentation + "ControlTemplate"),
            element => (string?)element.Attribute("TargetType") == "{x:Type controls:RibbonTab}");
        XElement hover = Assert.Single(
            groupsTabTemplate.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("SourceName") == "HeaderChrome"
                && (string?)trigger.Attribute("Property") == "IsMouseOver"
                && (string?)trigger.Attribute("Value") == "True");
        AssertSetter(
            hover,
            "HeaderChrome",
            "BorderBrush",
            "{DynamicResource RibbonKit.Brushes.Tab.HoverBorder}");
    }

    [Fact]
    public void Showcase_offers_2010_aero_only_as_an_explicit_generation_matched_choice()
    {
        XDocument showcase = XDocument.Load(
            Path.Combine(RepositoryRoot(), "samples", "RibbonKit.Showcase", "MainWindow.xaml"));
        XElement toggle = NamedElement(showcase, "Office2010AeroFrameToggle");
        Assert.Equal("2010 Aero", (string?)toggle.Attribute("Header"));
        Assert.Equal("OnToggleOffice2010AeroFrame", (string?)toggle.Attribute("Checked"));
        Assert.Equal("OnToggleOffice2010AeroFrame", (string?)toggle.Attribute("Unchecked"));

        string code = File.ReadAllText(
            Path.Combine(RepositoryRoot(), "samples", "RibbonKit.Showcase", "MainWindow.xaml.cs"));
        Assert.Contains(
            "RibbonWindowFrameAppearance.Office2010Aero when theme == RibbonTheme.Office2010",
            code,
            StringComparison.Ordinal);
        Assert.Contains(
            "FrameAppearance == RibbonWindowFrameAppearance.Office2010Aero",
            code,
            StringComparison.Ordinal);
    }

    private static XElement WindowTemplate(XDocument document) =>
        Assert.Single(
            document.Descendants(Presentation + "ControlTemplate"),
            element => (string?)element.Attribute("TargetType")
                == "{x:Type controls:RibbonWindow}");

    private static XElement Trigger(XElement template, string property, string value) =>
        Assert.Single(
            template.Descendants(Presentation + "Trigger"),
            element => (string?)element.Attribute("Property") == property
                && (string?)element.Attribute("Value") == value);

    private static bool BindingContains(XElement trigger, string property) =>
        ((string?)trigger.Attribute("Binding"))?.Contains(property, StringComparison.Ordinal) == true;

    private static bool HasCondition(XElement trigger, string property, string value) =>
        trigger.Descendants(Presentation + "Condition").Any(
            condition => (string?)condition.Attribute("Property") == property
                && (string?)condition.Attribute("Value") == value);

    private static bool HasBindingCondition(XElement trigger, string property, string value) =>
        trigger.Descendants(Presentation + "Condition").Any(
            condition => ((string?)condition.Attribute("Binding"))?.Contains(
                    property,
                    StringComparison.Ordinal) == true
                && (string?)condition.Attribute("Value") == value);

    private static void AssertSetter(
        XElement trigger,
        string targetName,
        string property,
        string value) =>
        Assert.Contains(
            trigger.Elements(Presentation + "Setter"),
            setter => (string?)setter.Attribute("TargetName") == targetName
                && (string?)setter.Attribute("Property") == property
                && (string?)setter.Attribute("Value") == value);

    private static XElement NamedElement(XDocument document, string name) =>
        Assert.Single(
            document.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == name);

    private static XElement Resource(XDocument document, string key) =>
        Assert.Single(
            document.Root!.Elements(),
            element => (string?)element.Attribute(Xaml + "Key") == key);

    private static void AssertReadabilityResourcesAreAbsent(XDocument document)
    {
        string[] removedKeys =
        {
            "RibbonKit.Brushes.WindowFrame.Office2010Readability",
            "RibbonKit.Effects.WindowFrame.Office2010ReadabilityBlur",
            "RibbonKit.Metrics.WindowFrame.Office2010ReadabilityHeight",
            "RibbonKit.Metrics.WindowFrame.Office2010ReadabilityOpacity",
        };

        foreach (string key in removedKeys)
        {
            Assert.DoesNotContain(
                document.Root!.Elements(),
                element => (string?)element.Attribute(Xaml + "Key") == key);
        }
    }

    private static string Attribute(XDocument document, string key, string attribute) =>
        Resource(document, key).Attribute(attribute)!.Value;

    private static string Value(XDocument document, string key) =>
        Resource(document, key).Value.Trim();

    private static XDocument LoadTheme(string name) =>
        XDocument.Load(Path.Combine(RepositoryRoot(), "src", "RibbonKit", "Themes", name));

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
