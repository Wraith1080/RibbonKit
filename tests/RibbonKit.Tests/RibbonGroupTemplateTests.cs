using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Template contracts for the collapsed RibbonGroup representation and flyout.</summary>
public class RibbonGroupTemplateTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Collapsed_group_flyout_compensates_for_its_shadow_margin()
    {
        XDocument document = LoadTemplate();
        XElement popup = NamedElement(document, "PART_Popup");

        Assert.Equal("Bottom", (string?)popup.Attribute("Placement"));
        Assert.Equal("-4", (string?)popup.Attribute("HorizontalOffset"));
        Assert.Equal(
            "{Binding ElementName=PART_CollapsedButton}",
            (string?)popup.Attribute("PlacementTarget"));

        XElement host = NamedElement(document, "PART_PopupHost");
        Assert.Equal("4,2,8,8", (string?)host.Attribute("Margin"));
    }

    [Fact]
    public void Disabled_collapsed_group_uses_the_standard_command_opacity()
    {
        XDocument document = LoadTemplate();
        XElement collapsedButton = NamedElement(document, "PART_CollapsedButton");
        XElement disabled = Assert.Single(
            collapsedButton.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "IsEnabled"
                && (string?)trigger.Attribute("Value") == "False");
        XElement opacity = Assert.Single(
            disabled.Elements(Presentation + "Setter"),
            setter => (string?)setter.Attribute("Property") == "Opacity");

        Assert.Equal("0.4", (string?)opacity.Attribute("Value"));
    }

    private static XElement NamedElement(XDocument document, string name) =>
        Assert.Single(
            document.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == name);

    private static XDocument LoadTemplate() => XDocument.Load(Path.Combine(
        RepositoryRoot(),
        "src",
        "RibbonKit",
        "Themes",
        "Controls.Groups.xaml"));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        return Assert.IsType<DirectoryInfo>(directory).FullName;
    }
}
