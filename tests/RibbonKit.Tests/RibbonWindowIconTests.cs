using System.IO;
using System.Xml.Linq;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Contracts for the application icon in RibbonWindow's custom caption.</summary>
public class RibbonWindowIconTests
{
    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Ribbon_window_template_renders_the_window_icon_in_its_caption()
    {
        XDocument document = XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "src",
            "RibbonKit",
            "Themes",
            "Controls.Window.xaml"));

        XElement icon = NamedElement(document, "PART_WindowIcon");
        XElement titleBarContent = NamedElement(document, "TitleBarContentHost");
        XElement nullIconTrigger = Assert.Single(
            document.Descendants(),
            element => element.Name.LocalName == "Trigger"
                && (string?)element.Attribute("Property") == "Icon"
                && (string?)element.Attribute("Value") == "{x:Null}");

        Assert.Equal("Image", icon.Name.LocalName);
        Assert.Equal("{TemplateBinding Icon}", (string?)icon.Attribute("Source"));
        Assert.Equal("16", (string?)icon.Attribute("Width"));
        Assert.Equal("16", (string?)icon.Attribute("Height"));
        Assert.Same(icon.Parent, titleBarContent.Parent);
        Assert.Contains(
            nullIconTrigger.Elements(),
            element => element.Name.LocalName == "Setter"
                && (string?)element.Attribute("TargetName") == "PART_WindowIcon"
                && (string?)element.Attribute("Property") == "Visibility"
                && (string?)element.Attribute("Value") == "Collapsed");
    }

    [Fact]
    public void Orb_owners_suppress_only_the_custom_caption_icon()
    {
        Sta.Run(() =>
        {
            var window = new RibbonWindow();
            var first = new Ribbon();
            var second = new Ribbon();

            window.UpdateApplicationButtonShape(first, RibbonApplicationButtonShape.Orb);
            window.UpdateApplicationButtonShape(second, RibbonApplicationButtonShape.Tab);
            Assert.True(window.IsTitleBarIconSuppressed);

            window.UpdateApplicationButtonShape(first, RibbonApplicationButtonShape.Tab);
            Assert.False(window.IsTitleBarIconSuppressed);

            window.UpdateApplicationButtonShape(first, RibbonApplicationButtonShape.Orb);
            window.UpdateApplicationButtonShape(second, RibbonApplicationButtonShape.Orb);
            window.UnregisterApplicationButton(first);
            Assert.True(window.IsTitleBarIconSuppressed);

            window.UnregisterApplicationButton(second);
            Assert.False(window.IsTitleBarIconSuppressed);
        });
    }

    private static XElement NamedElement(XDocument document, string name) =>
        Assert.Single(
            document.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == name);

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
