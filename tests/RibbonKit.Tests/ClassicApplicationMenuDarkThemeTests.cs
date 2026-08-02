using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Guards complete dark application-menu palettes for the hybrid 2007/2010 Black themes.</summary>
public sealed class ClassicApplicationMenuDarkThemeTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    private static readonly string[] SurfaceKeys =
    [
        "RibbonKit.Brushes.ApplicationMenu.FrameBorder",
        "RibbonKit.Brushes.ApplicationMenu.FrameRim",
        "RibbonKit.Brushes.ApplicationMenu.FrameBand",
        "RibbonKit.Brushes.ApplicationMenu.TopBandBackground",
        "RibbonKit.Brushes.ApplicationMenu.FooterBackground",
        "RibbonKit.Brushes.ApplicationMenu.NavBackground",
        "RibbonKit.Brushes.ApplicationMenu.PaneBackground",
        "RibbonKit.Brushes.ApplicationMenu.PaneSurface",
        "RibbonKit.Brushes.ApplicationMenu.PaneBorder",
        "RibbonKit.Brushes.ApplicationMenu.HeaderBackground",
        "RibbonKit.Brushes.ApplicationMenu.Separator",
        "RibbonKit.Brushes.ApplicationMenu.SeparatorHighlight",
        "RibbonKit.Brushes.ApplicationMenu.ButtonBackground",
        "RibbonKit.Brushes.ApplicationMenu.ButtonBorder",
    ];

    [Fact]
    public void Classic_black_overlays_own_a_complete_dark_application_menu_palette()
    {
        foreach (string themeFile in new[] { "Tokens.Office2007.Dark.xaml", "Tokens.Office2010.Dark.xaml" })
        {
            XDocument document = XDocument.Load(ThemePart(themeFile));

            Assert.True(Luminance(Resource(document, "RibbonKit.Brushes.ApplicationMenu.Foreground")) > 180d);
            Assert.True(Luminance(Resource(document, "RibbonKit.Brushes.ApplicationMenu.SecondaryForeground")) > 180d);
            Assert.True(Luminance(Resource(document, "RibbonKit.Brushes.ApplicationMenu.HeadingForeground")) > 180d);

            foreach (string key in SurfaceKeys)
            {
                XElement resource = Resource(document, key);
                Assert.True(
                    Luminance(resource) < 150d,
                    $"{themeFile}'s {key} is not a dark override.");
            }
        }
    }

    [Fact]
    public void Every_theme_defines_application_menu_scoped_foregrounds()
    {
        string[] themeFiles =
        [
            "Tokens.Office2007.xaml",
            "Tokens.Office2007.Dark.xaml",
            "Tokens.Office2010.xaml",
            "Tokens.Office2010.Dark.xaml",
            "Tokens.Office2013.xaml",
            "Tokens.Office2013.Dark.xaml",
            "Tokens.Office2019.xaml",
            "Tokens.Office2019.Dark.xaml",
            "Tokens.Office2024.xaml",
            "Tokens.Office2024.Dark.xaml",
        ];

        foreach (string themeFile in themeFiles)
        {
            XDocument document = XDocument.Load(ThemePart(themeFile));
            Assert.Equal(Presentation + "SolidColorBrush", Resource(
                document,
                "RibbonKit.Brushes.ApplicationMenu.Foreground").Name);
            Assert.Equal(Presentation + "SolidColorBrush", Resource(
                document,
                "RibbonKit.Brushes.ApplicationMenu.SecondaryForeground").Name);
            Assert.Equal(Presentation + "SolidColorBrush", Resource(
                document,
                "RibbonKit.Brushes.ApplicationMenu.HeadingForeground").Name);
        }
    }

    [Fact]
    public void Application_menu_templates_consume_only_the_scoped_foregrounds()
    {
        XDocument document = XDocument.Load(ThemePart("Controls.ApplicationMenu.xaml"));
        string[] resourceAttributes = document.Root!
            .DescendantsAndSelf()
            .Attributes()
            .Select(attribute => attribute.Value)
            .Where(value => value.Contains("DynamicResource RibbonKit.Brushes.", StringComparison.Ordinal))
            .ToArray();

        Assert.DoesNotContain(
            resourceAttributes,
            value => value.Contains("RibbonKit.Brushes.Text.", StringComparison.Ordinal));
        Assert.True(resourceAttributes.Count(value => value.Contains(
            "RibbonKit.Brushes.ApplicationMenu.Foreground",
            StringComparison.Ordinal)) >= 5);
        Assert.True(resourceAttributes.Count(value => value.Contains(
            "RibbonKit.Brushes.ApplicationMenu.SecondaryForeground",
            StringComparison.Ordinal)) >= 2);
        Assert.Contains(
            resourceAttributes,
            value => value.Contains(
                "RibbonKit.Brushes.ApplicationMenu.HeadingForeground",
                StringComparison.Ordinal));
    }

    private static XElement Resource(XDocument document, string key) =>
        Assert.Single(
            document.Root!.Elements(),
            element => (string?)element.Attribute(Xaml + "Key") == key);

    private static double Luminance(XElement resource)
    {
        string[] colors = resource
            .DescendantsAndSelf()
            .Attributes("Color")
            .Select(attribute => attribute.Value)
            .ToArray();
        Assert.NotEmpty(colors);
        return colors.Max(ColorLuminance);
    }

    private static double ColorLuminance(string color)
    {
        string value = color.TrimStart('#');
        Assert.True(value.Length is 6 or 8, $"Unsupported color literal #{value}.");
        int start = value.Length == 8 ? 2 : 0;
        byte red = byte.Parse(value.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        byte green = byte.Parse(value.AsSpan(start + 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        byte blue = byte.Parse(value.AsSpan(start + 4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return (0.2126d * red) + (0.7152d * green) + (0.0722d * blue);
    }

    private static string ThemePart(string name) =>
        Path.Combine(RepositoryRoot(), "src", "RibbonKit", "Themes", name);

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
