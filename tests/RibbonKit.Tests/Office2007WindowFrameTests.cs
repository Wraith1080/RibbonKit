using System.IO;
using System.Xml.Linq;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Contracts for the corrected opaque Office 2007 RibbonWindow treatment.</summary>
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

    [Fact]
    public void Office_2007_tokens_define_the_opaque_title_state_without_a_global_frame_band()
    {
        XDocument theme = LoadTheme("Tokens.Office2007.xaml");

        Assert.Equal("34", Value(theme, "RibbonKit.Metrics.WindowFrame.CaptionHeight"));
        Assert.Equal("0.78", Value(theme, "RibbonKit.Metrics.WindowFrame.InactiveTitleBarOpacity"));
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
        AssertRemovedGlobalFrameKeysAreAbsent(theme);
    }

    [Fact]
    public void Shared_window_template_keeps_the_measured_window_root_flush_with_the_physical_host()
    {
        XDocument template = LoadTheme("Controls.Window.xaml");
        XElement physicalHost = NamedElement(template, "PhysicalWindowFrameHost");
        XElement windowRoot = NamedElement(template, "PART_WindowRoot");
        XElement titleBar = NamedElement(template, "TitleBarBand");
        XElement windowChrome = Assert.Single(
            template.Descendants(),
            element => element.Name.LocalName == "WindowChrome");

        Assert.Same(physicalHost, windowRoot.Parent);
        Assert.Equal(
            "{DynamicResource RibbonKit.Metrics.WindowFrame.CaptionHeight}",
            (string?)windowChrome.Attribute("CaptionHeight"));
        Assert.Equal(
            "{DynamicResource RibbonKit.Brushes.TitleBar.Background}",
            (string?)titleBar.Attribute("Background"));
        Assert.DoesNotContain(
            template.Descendants(),
            element => RemovedGlobalFrameKeys.Contains((string?)element.Attribute(Xaml + "Key"))
                || (string?)element.Attribute(Xaml + "Name") is "WindowFrameOutline"
                    or "WindowFrameBand"
                    or "MaximizedWindowTopBand");
    }

    [Fact]
    public void Maximized_and_inactive_triggers_do_not_inset_the_complete_window()
    {
        XDocument template = LoadTheme("Controls.Window.xaml");
        XElement controlTemplate = Assert.Single(
            template.Descendants(),
            element => element.Name.LocalName == "ControlTemplate"
                && (string?)element.Attribute("TargetType") == "{x:Type controls:RibbonWindow}");

        XElement maximized = Trigger(controlTemplate, "WindowState", "Maximized");
        Assert.DoesNotContain(
            maximized.Elements(),
            element => ((string?)element.Attribute("TargetName"))?.StartsWith(
                "WindowFrame",
                StringComparison.Ordinal) == true);

        XElement inactive = Trigger(controlTemplate, "IsActive", "False");
        AssertSetter(
            inactive,
            "TitleBarBand",
            "Opacity",
            "{DynamicResource RibbonKit.Metrics.WindowFrame.InactiveTitleBarOpacity}");
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
