using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Guards the application menu's two-level paint ordering.</summary>
public class ApplicationMenuLayeringTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Menu_layer_stays_above_ribbon_chrome_but_below_application_button()
    {
        var document = XDocument.Load(RibbonChromePath());

        var menuLayer = Named(document, "ApplicationMenuLayer");
        var applicationButtonLayer = Named(document, "ApplicationButtonLayer");
        var tabRowQat = Named(document, "QatTabRowHost");
        var tabScroll = Named(document, "PART_TabScroll");

        Assert.True(ZIndex(menuLayer) > ZIndex(tabRowQat));
        Assert.True(ZIndex(menuLayer) > ZIndex(tabScroll.Parent!));
        Assert.True(ZIndex(applicationButtonLayer) > ZIndex(menuLayer));
    }

    [Fact]
    public void Menu_presenter_is_a_named_child_of_the_placement_layer()
    {
        var document = XDocument.Load(RibbonChromePath());

        var menuLayer = Named(document, RibbonTabControl.ApplicationMenuLayerPartName);
        var presenter = Named(document, RibbonTabControl.ApplicationMenuPresenterPartName);

        Assert.Same(menuLayer, presenter.Parent);
        Assert.Equal(
            "{DynamicResource RibbonKit.Metrics.ApplicationMenuMargin}",
            (string?)presenter.Attribute("Margin"));
    }

    [Theory]
    [InlineData("Office2007", "False", "0,8,0,0", "3")]
    [InlineData("Office2010", "True", "0", "3")]
    [InlineData("Office2013", "True", "0", "0")]
    [InlineData("Office2019", "True", "0", "0")]
    [InlineData("Office2024", "True", "0,6,0,0", "8")]
    public void Theme_selects_overlay_connected_or_floating_menu_geometry(
        string theme,
        string anchorsBelow,
        string margin,
        string cornerRadius)
    {
        var document = XDocument.Load(ThemePath(theme));

        Assert.Equal(
            anchorsBelow,
            Resource(document, "RibbonKit.Behaviors.ApplicationMenuAnchorBelowButton").Value.Trim());
        Assert.Equal(
            margin,
            Resource(document, "RibbonKit.Metrics.ApplicationMenuMargin").Value.Trim());
        Assert.Equal(
            cornerRadius,
            Resource(document, "RibbonKit.Metrics.ApplicationMenuCornerRadius").Value.Trim());
        Assert.Equal(
            "DropShadowEffect",
            Resource(document, "RibbonKit.Effects.ApplicationButtonMenuOpenShadow").Name.LocalName);
    }

    [Fact]
    public void Office2024_rounds_the_visible_inner_surfaces_as_well_as_the_outer_frame()
    {
        var document = XDocument.Load(ThemePath("Office2024"));

        Assert.Equal("7", Resource(document, "RibbonKit.Metrics.ApplicationMenuInnerCornerRadius").Value.Trim());
        Assert.Equal("6,6,0,0", Resource(document, "RibbonKit.Metrics.ApplicationMenuTopBandCornerRadius").Value.Trim());
        Assert.Equal("0,0,6,6", Resource(document, "RibbonKit.Metrics.ApplicationMenuFooterCornerRadius").Value.Trim());
    }

    [Fact]
    public void Menu_open_file_button_uses_surface_tokens_separate_from_backstage()
    {
        var document = XDocument.Load(RibbonChromePath());

        var trigger = document
            .Descendants(Presentation + "DataTrigger")
            .Single(element =>
                ((string?)element.Attribute("Binding"))?.Contains("IsApplicationMenuOpen", StringComparison.Ordinal) == true
                && element.Elements(Presentation + "Setter").Any(setter =>
                    (string?)setter.Attribute("Property") == "Background"));

        var setters = trigger.Elements(Presentation + "Setter").ToArray();
        Assert.Contains(setters, setter =>
            (string?)setter.Attribute("TargetName") == "Chrome"
            && (string?)setter.Attribute("Property") == "Background"
            && (string?)setter.Attribute("Value") ==
                "{DynamicResource RibbonKit.Brushes.ApplicationButton.MenuOpenBackground}");
        Assert.Contains(setters, setter =>
            (string?)setter.Attribute("Property") == "Foreground"
            && (string?)setter.Attribute("Value") ==
                "{DynamicResource RibbonKit.Brushes.ApplicationButton.MenuOpenForeground}");
    }

    [Theory]
    [InlineData("Office2007")]
    [InlineData("Office2010")]
    [InlineData("Office2013")]
    [InlineData("Office2019")]
    [InlineData("Office2024")]
    public void Every_theme_defines_application_menu_open_file_button_tokens(string theme)
    {
        var document = XDocument.Load(ThemePath(theme));

        Assert.Equal(
            "SolidColorBrush",
            Resource(document, "RibbonKit.Brushes.ApplicationButton.MenuOpenBackground").Name.LocalName);
        Assert.Equal(
            "SolidColorBrush",
            Resource(document, "RibbonKit.Brushes.ApplicationButton.MenuOpenForeground").Name.LocalName);
    }

    [Fact]
    public void Open_menu_promotes_tab_control_above_below_ribbon_qat()
    {
        var document = XDocument.Load(RibbonChromePath());

        var trigger = document
            .Descendants(Presentation + "Trigger")
            .Single(element =>
                (string?)element.Attribute("Property") == "IsApplicationMenuOpen"
                && (string?)element.Attribute("Value") == "True");

        var setter = Assert.Single(trigger.Elements(Presentation + "Setter"));
        Assert.Equal("TabControlHost", (string?)setter.Attribute("TargetName"));
        Assert.Equal("Panel.ZIndex", (string?)setter.Attribute("Property"));
        Assert.Equal("1", (string?)setter.Attribute("Value"));

        // No local value means the normal, closed state remains at WPF's default z-index (0).
        Assert.Null(Named(document, "TabControlHost").Attribute("Panel.ZIndex"));
        Assert.Equal(0, ZIndex(Named(document, "QatBelowHost")));
    }

    private static XElement Named(XDocument document, string name) =>
        Assert.Single(
            document.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == name);

    private static int ZIndex(XElement element) =>
        int.TryParse((string?)element.Attribute("Panel.ZIndex"), out int value) ? value : 0;

    private static XElement Resource(XDocument document, string key) =>
        Assert.Single(
            document.Root!.Elements(),
            element => (string?)element.Attribute(Xaml + "Key") == key);

    private static string RibbonChromePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, $"Could not find RibbonKit.sln above {AppContext.BaseDirectory}.");

        return Path.Combine(
            directory!.FullName,
            "src",
            "RibbonKit",
            "Themes",
            "Controls.RibbonChrome.xaml");
    }

    private static string ThemePath(string theme)
    {
        string chrome = RibbonChromePath();
        return Path.Combine(Path.GetDirectoryName(chrome)!, $"Tokens.{theme}.xaml");
    }
}
