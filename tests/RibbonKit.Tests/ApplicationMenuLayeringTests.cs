using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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
}
