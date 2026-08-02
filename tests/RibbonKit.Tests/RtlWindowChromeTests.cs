using System.IO;
using System.Xml.Linq;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Contracts separating physical WindowChrome geometry from mirrored content.</summary>
public class RtlWindowChromeTests
{
    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Ribbon_window_keeps_physical_frame_ltr_and_mirrors_inner_content()
    {
        XDocument document = LoadTheme("Controls.Window.xaml");
        XElement physicalHost = NamedElement(document, "PhysicalWindowFrameHost");
        XElement windowRoot = NamedElement(document, "PART_WindowRoot");
        XElement logicalHost = NamedElement(document, "WindowAdornerHost");

        Assert.Equal("LeftToRight", (string?)physicalHost.Attribute("FlowDirection"));
        Assert.Equal("LeftToRight", (string?)windowRoot.Attribute("FlowDirection"));
        Assert.Same(physicalHost, windowRoot.Parent);
        Assert.Equal(
            "{TemplateBinding FlowDirection}",
            (string?)logicalHost.Attribute("FlowDirection"));
        Assert.Same(windowRoot, logicalHost.Parent);
    }

    [Fact]
    public void Options_dialog_keeps_physical_frame_ltr_and_mirrors_inner_content()
    {
        XDocument document = LoadTheme("Controls.OptionsDialog.xaml");
        XElement physicalHost = NamedElement(document, "OptionsDialogPhysicalFrameHost");
        XElement windowRoot = NamedElement(document, "PART_WindowRoot");
        XElement logicalRoot = NamedElement(document, "OptionsDialogLogicalRoot");

        Assert.Equal("LeftToRight", (string?)physicalHost.Attribute("FlowDirection"));
        Assert.Equal("LeftToRight", (string?)windowRoot.Attribute("FlowDirection"));
        Assert.Same(physicalHost, windowRoot.Parent);
        Assert.Equal(
            "{TemplateBinding FlowDirection}",
            (string?)logicalRoot.Attribute("FlowDirection"));
        Assert.Same(windowRoot, logicalRoot.Parent);
    }

    private static XDocument LoadTheme(string fileName) =>
        XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "src",
            "RibbonKit",
            "Themes",
            fileName));

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
