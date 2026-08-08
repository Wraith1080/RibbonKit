using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Guards neutral Backstage navigation chrome in dark and historical Black palettes.</summary>
public class BackstageDarkPaletteTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Classic_backstage_uses_a_theme_scoped_navigation_brush()
    {
        XDocument document = XDocument.Load(ThemePart("Controls.Backstage.xaml"));
        XElement nav = Assert.Single(
            document.Descendants(Presentation + "Border"),
            element => (string?)element.Attribute(Xaml + "Name") == "NavColumn");

        Assert.Contains(
            "RibbonKit.Brushes.Backstage.Classic.NavBackground",
            (string?)nav.Attribute("Background"),
            StringComparison.Ordinal);

        Assert.Contains(
            document.Descendants(Presentation + "Setter"),
            setter => (string?)setter.Attribute("TargetName") == "Chrome"
                && (string?)setter.Attribute("Property") == "BorderBrush"
                && ((string?)setter.Attribute("Value"))?.Contains(
                    "RibbonKit.Brushes.Backstage.ItemSelectedBorder",
                    StringComparison.Ordinal) == true);
    }

    [Theory]
    [InlineData("Tokens.Office2024.Dark.xaml")]
    [InlineData("Tokens.Office2019.Dark.xaml")]
    [InlineData("Tokens.Office2013.Dark.xaml")]
    [InlineData("Tokens.Office2010.Dark.xaml")]
    [InlineData("Tokens.Office2007.Dark.xaml")]
    public void Dark_classic_backstage_navigation_tokens_are_greyscale(string themeFile)
    {
        XDocument document = XDocument.Load(ThemePart(themeFile));
        string[] keys =
        [
            "RibbonKit.Brushes.Backstage.Classic.NavBackground",
            "RibbonKit.Brushes.Backstage.ItemHoverBackground",
            "RibbonKit.Brushes.Backstage.ItemSelectedBackground",
            "RibbonKit.Brushes.Backstage.ItemSelectedBorder",
            "RibbonKit.Brushes.Backstage.ItemSelectedGlass",
        ];

        foreach (string key in keys)
        {
            XElement resource = Resource(document, key);
            XAttribute[] colors = resource
                .DescendantsAndSelf()
                .Attributes("Color")
                .ToArray();
            Assert.NotEmpty(colors);
            Assert.All(colors, color => Assert.True(
                IsGreyscale(color.Value),
                $"{themeFile} resource {key} contains non-neutral color {color.Value}."));
        }
    }

    [Theory]
    [InlineData("Tokens.Office2010.Dark.xaml", 2, false)]
    [InlineData("Tokens.Office2007.Dark.xaml", 4, true)]
    public void Classic2010_black_navigation_preserves_its_generation_glass_shape(
        string themeFile,
        int expectedStops,
        bool expectsHardCrease)
    {
        XDocument document = XDocument.Load(ThemePart(themeFile));
        XElement nav = Resource(document, "RibbonKit.Brushes.Backstage.NavBackground");
        XElement selected = Resource(document, "RibbonKit.Brushes.Backstage.ItemSelectedGlass");

        XElement[] navStops = nav.Elements(Presentation + "GradientStop").ToArray();
        Assert.Equal(expectedStops, navStops.Length);
        Assert.Equal(
            expectsHardCrease,
            navStops.GroupBy(stop => (string?)stop.Attribute("Offset")).Any(group => group.Count() > 1));

        Assert.All(
            navStops.Concat(selected.Elements(Presentation + "GradientStop")),
            stop => Assert.True(IsGreyscale((string)stop.Attribute("Color")!)));
    }

    [Fact]
    public void Every_palette_defines_the_new_classic_navigation_contract()
    {
        string[] files = Directory.GetFiles(ThemesDirectory(), "Tokens.Office*.xaml");
        Assert.Equal(10, files.Length);

        foreach (string file in files)
        {
            XDocument document = XDocument.Load(file);
            _ = Resource(document, "RibbonKit.Brushes.Backstage.Classic.NavBackground");
            _ = Resource(document, "RibbonKit.Brushes.Backstage.ItemSelectedBorder");
        }
    }

    private static bool IsGreyscale(string color)
    {
        string rgb = color.TrimStart('#');
        if (rgb.Length == 8)
        {
            rgb = rgb[2..];
        }

        return rgb.Length == 6
            && string.Equals(rgb[..2], rgb.Substring(2, 2), StringComparison.OrdinalIgnoreCase)
            && string.Equals(rgb[..2], rgb.Substring(4, 2), StringComparison.OrdinalIgnoreCase);
    }

    private static XElement Resource(XDocument document, string key) =>
        Assert.Single(
            document.Root!.Elements(),
            element => (string?)element.Attribute(Xaml + "Key") == key);

    private static string ThemePart(string fileName) => Path.Combine(ThemesDirectory(), fileName);

    private static string ThemesDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory!.FullName, "src", "RibbonKit", "Themes");
    }
}
