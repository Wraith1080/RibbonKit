using System.IO;
using System.Windows;
using System.Windows.Media;
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

    [Theory]
    [InlineData(1.00, -1920, 80, -1800, 114, 46, 34)]
    [InlineData(1.25, 0, 0, 150, 42.5, 57.5, 42.5)]
    [InlineData(1.50, 1920, 0, 2100, 51, 69, 51)]
    [InlineData(1.75, -2560, -1440, -2350, -1380.5, 80.5, 59.5)]
    [InlineData(2.00, 3840, -2160, 4080, -2092, 92, 68)]
    public void Client_DIP_bounds_follow_native_screen_origin_at_every_required_scale(
        double scale,
        double clientX,
        double clientY,
        double expectedX,
        double expectedY,
        double expectedWidth,
        double expectedHeight)
    {
        Rect bounds = RibbonWindow.CalculateScreenBounds(
            new Point(clientX, clientY),
            new Point(120d, 34d),
            new Point(166d, 68d),
            new DpiScale(scale, scale));

        Assert.Equal(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), bounds);
    }

    [Fact]
    public void Client_DIP_bounds_preserve_mirrored_template_corners()
    {
        Rect bounds = RibbonWindow.CalculateScreenBounds(
            new Point(-1280d, 100d),
            new Point(92d, 20d),
            new Point(46d, 54d),
            new DpiScale(1.5d, 1.5d));

        Assert.Equal(new Rect(-1211d, 130d, 69d, 51d), bounds);
    }

    [Theory]
    [InlineData(1.00)]
    [InlineData(1.25)]
    [InlineData(1.50)]
    [InlineData(1.75)]
    [InlineData(2.00)]
    public void Maximized_overhang_is_converted_to_the_same_DIP_inset_at_every_required_scale(
        double scale)
    {
        const double expectedInset = 8d;
        double overhangPixels = expectedInset * scale;
        var workArea = new Rect(-2560d, 0d, 2560d, 1440d);
        var windowRect = new Rect(
            workArea.Left - overhangPixels,
            workArea.Top - overhangPixels,
            workArea.Width + (2d * overhangPixels),
            workArea.Height + (2d * overhangPixels));

        Thickness inset = RibbonWindow.CalculateMaximizeInset(
            windowRect,
            workArea,
            new DpiScale(scale, scale));

        Assert.Equal(new Thickness(expectedInset), inset);
    }

    [Fact]
    public void Maximized_inset_clamps_edges_that_do_not_overhang()
    {
        Thickness inset = RibbonWindow.CalculateMaximizeInset(
            new Rect(4d, 6d, 1910d, 1060d),
            new Rect(0d, 0d, 1920d, 1080d),
            new DpiScale(1.25d, 1.25d));

        Assert.Equal(default, inset);
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
