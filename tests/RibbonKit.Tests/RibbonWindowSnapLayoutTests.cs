using System.IO;
using System.Windows;
using System.Xml.Linq;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Contracts for the native maximize hit test used by Windows 11 Snap Layouts.</summary>
public class RibbonWindowSnapLayoutTests
{
    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Ribbon_window_template_exposes_maximize_and_restore_hit_test_parts()
    {
        XDocument document = XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "src",
            "RibbonKit",
            "Themes",
            "Controls.Window.xaml"));

        XElement maximize = NamedElement(document, "PART_MaximizeButton");
        XElement restore = NamedElement(document, "PART_RestoreButton");
        XElement captionStyle = Assert.Single(
            document.Descendants(),
            element => element.Name.LocalName == "Style"
                && (string?)element.Attribute(Xaml + "Key") == "CaptionButtonStyle");
        XElement backgroundSetter = Assert.Single(
            captionStyle.Elements(),
            element => element.Name.LocalName == "Setter"
                && (string?)element.Attribute("Property") == "Background");
        XElement chrome = Assert.Single(
            captionStyle.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == "Chrome");
        string[] stateBackgrounds = captionStyle
            .Descendants()
            .Where(element => element.Name.LocalName == "Setter"
                && (string?)element.Attribute("TargetName") == "Chrome"
                && (string?)element.Attribute("Property") == "Background")
            .Select(element => Assert.IsType<XAttribute>(element.Attribute("Value")).Value)
            .ToArray();

        Assert.Equal("Button", maximize.Name.LocalName);
        Assert.Equal("{x:Static SystemCommands.MaximizeWindowCommand}", (string?)maximize.Attribute("Command"));
        Assert.Equal("Button", restore.Name.LocalName);
        Assert.Equal("{x:Static SystemCommands.RestoreWindowCommand}", (string?)restore.Attribute("Command"));
        Assert.Equal("Transparent", (string?)backgroundSetter.Attribute("Value"));
        Assert.Equal("{TemplateBinding Background}", (string?)chrome.Attribute("Background"));
        Assert.Contains($"{{DynamicResource {ThemeManager.CaptionHoverKey}}}", stateBackgrounds);
        Assert.Contains($"{{DynamicResource {ThemeManager.CaptionPressedKey}}}", stateBackgrounds);
    }

    [Theory]
    [InlineData(120, 80)]
    [InlineData(-120, 80)]
    [InlineData(120, -80)]
    [InlineData(-120, -80)]
    public void Screen_point_decoder_preserves_signed_multi_monitor_coordinates(int x, int y)
    {
        IntPtr lParam = PackScreenPoint(x, y);

        Point point = RibbonWindow.ScreenPointFromLParam(lParam);

        Assert.Equal(new Point(x, y), point);
    }

    [Theory]
    [InlineData(100, 200, true)]
    [InlineData(145, 233, true)]
    [InlineData(146, 200, false)]
    [InlineData(100, 234, false)]
    [InlineData(99, 200, false)]
    [InlineData(100, 199, false)]
    public void Screen_bounds_match_win32_edge_rules(double x, double y, bool expected)
    {
        bool actual = RibbonWindow.IsScreenPointWithinBounds(
            new Point(x, y),
            new Point(100, 200),
            new Point(146, 234));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Screen_bounds_allow_reversed_corners_for_transformed_templates()
    {
        Assert.True(RibbonWindow.IsScreenPointWithinBounds(
            new Point(120, 220),
            new Point(146, 234),
            new Point(100, 200)));
    }

    private static IntPtr PackScreenPoint(int x, int y)
    {
        long packed = (ushort)x | ((long)(ushort)y << 16);
        return new IntPtr(packed);
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
