using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Guards the adorner hosts and named parts required by QAT KeyTip levels.</summary>
public class QuickAccessKeyTipTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Overflow_popup_has_its_own_KeyTip_adorner_layer()
    {
        var document = XDocument.Load(ThemePath("Controls.Shared.xaml"));
        var popup = Named(document, RibbonQuickAccessToolBar.OverflowPopupPartName);

        Assert.Equal("Popup", popup.Name.LocalName);
        Assert.Equal("AdornerDecorator", Assert.Single(popup.Elements()).Name.LocalName);
        Assert.Equal(
            "ToggleButton",
            Named(document, RibbonQuickAccessToolBar.OverflowButtonPartName).Name.LocalName);
        Assert.Equal(
            "ItemsControl",
            Named(document, RibbonQuickAccessToolBar.OverflowHostPartName).Name.LocalName);
    }

    [Fact]
    public void Title_bar_QAT_is_inside_a_KeyTip_adorner_layer()
    {
        var document = XDocument.Load(ThemePath("Controls.Window.xaml"));
        var titleBarContent = Named(document, "TitleBarContentHost");

        Assert.Contains(
            titleBarContent.Ancestors(),
            ancestor => ancestor.Name == Presentation + "AdornerDecorator");
    }

    private static XElement Named(XDocument document, string name) =>
        Assert.Single(
            document.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == name);

    private static string ThemePath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, $"Could not find RibbonKit.sln above {AppContext.BaseDirectory}.");

        return Path.Combine(directory!.FullName, "src", "RibbonKit", "Themes", fileName);
    }
}
