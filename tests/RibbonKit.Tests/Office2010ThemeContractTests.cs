using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Xml.Linq;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Guards the generation-specific chrome and glass-state contracts of Office 2010.</summary>
public sealed class Office2010ThemeContractTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Theory]
    [InlineData("Tokens.Office2010.xaml")]
    [InlineData("Tokens.Office2010.Dark.xaml")]
    public void Title_bar_flows_continuously_into_the_tab_strip(string themeFile)
    {
        XDocument document = XDocument.Load(ThemePart(themeFile));
        XElement titleBar = Resource(document, "RibbonKit.Brushes.TitleBar.Background");
        XElement tabStrip = Resource(document, "RibbonKit.Brushes.Ribbon.Background");

        Assert.Equal(Presentation + "LinearGradientBrush", titleBar.Name);
        Assert.Equal(Presentation + "LinearGradientBrush", tabStrip.Name);
        Assert.Equal(
            Stops(titleBar).Last().Attribute("Color")?.Value,
            Stops(tabStrip).First().Attribute("Color")?.Value,
            ignoreCase: true);
    }

    [Theory]
    [InlineData("RibbonKit.Brushes.Control.HoverBackground")]
    [InlineData("RibbonKit.Brushes.Control.CheckedBackground")]
    [InlineData("RibbonKit.Brushes.Control.CheckedHoverBackground")]
    public void Button_state_glass_finishes_with_a_bright_bottom_inner_glow(string key)
    {
        XDocument document = XDocument.Load(ThemePart("Tokens.Office2010.xaml"));
        XElement brush = Resource(document, key);
        XElement[] stops = Stops(brush);

        Assert.Equal(Presentation + "LinearGradientBrush", brush.Name);
        Assert.True(stops.Length >= 4, $"{key} must reserve a narrow final stop for its bottom glow.");
        Assert.Equal(1d, Offset(stops[^1]));
        Assert.InRange(Offset(stops[^2]), 0.85d, 0.95d);
        Assert.True(
            Luminance(stops[^1]) > Luminance(stops[^2]),
            $"{key}'s final stop must be brighter than the lower-face stop before it.");
    }

    [Theory]
    [InlineData("RibbonKit.Brushes.ApplicationButton.Background")]
    [InlineData("RibbonKit.Brushes.ApplicationButton.HoverBackground")]
    [InlineData("RibbonKit.Brushes.ApplicationButton.PressedBackground")]
    [InlineData("RibbonKit.Brushes.ApplicationButton.MenuOpenBackground")]
    public void File_button_body_has_no_uniform_bright_bottom_foot(string key)
    {
        XDocument document = XDocument.Load(ThemePart("Tokens.Office2010.xaml"));
        XElement brush = Resource(document, key);
        XElement[] stops = Stops(brush);

        Assert.Equal(Presentation + "LinearGradientBrush", brush.Name);
        Assert.Equal(3, stops.Length);
        Assert.Equal(1d, Offset(stops[^1]));
        Assert.True(
            Offset(stops[^2]) <= 0.75d,
            $"{key} must leave the localized lower bloom to the radial inner-rim token.");
    }

    [Theory]
    [InlineData("RibbonKit.Brushes.Control.PressedBackground")]
    [InlineData("RibbonKit.Brushes.ApplicationButton.PressedBackground")]
    public void Pressed_state_glass_has_no_bright_bottom_foot(string key)
    {
        XDocument document = XDocument.Load(ThemePart("Tokens.Office2010.xaml"));
        XElement brush = Resource(document, key);
        XElement[] stops = Stops(brush);

        Assert.Equal(Presentation + "LinearGradientBrush", brush.Name);
        Assert.Equal(3, stops.Length);
        Assert.Equal(1d, Offset(stops[^1]));
        Assert.True(
            Offset(stops[^2]) <= 0.75d,
            $"{key} must not reserve a narrow final segment that recreates the released-state glow.");
    }

    [Fact]
    public void Pressed_washes_avoid_the_shared_rim_and_file_press_hides_its_scoped_rim()
    {
        XDocument buttons = XDocument.Load(ThemePart("Controls.Buttons.xaml"));
        XElement[] pressedWashes = buttons
            .Descendants()
            .Where(element => (string?)element.Attribute(Xaml + "Name") == "PressWash")
            .ToArray();

        Assert.Equal(2, pressedWashes.Length);
        Assert.All(pressedWashes, wash => Assert.DoesNotContain(
            wash.DescendantsAndSelf().Attributes(),
            attribute => attribute.Value.Contains("RibbonKit.Brushes.Control.InnerGlow", StringComparison.Ordinal)));

        XDocument chrome = XDocument.Load(ThemePart("Controls.RibbonChrome.xaml"));
        XElement applicationButton = Assert.Single(
            chrome.Descendants(Presentation + "ToggleButton"),
            element => (string?)element.Attribute(Xaml + "Name") == "PART_ApplicationButton");
        Assert.DoesNotContain(
            applicationButton.DescendantsAndSelf().Attributes(),
            attribute => attribute.Value.Contains("RibbonKit.Brushes.Control.InnerGlow", StringComparison.Ordinal));

        XElement innerRim = Assert.Single(
            applicationButton.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == "InnerRim");
        Assert.Contains(
            innerRim.Attributes(),
            attribute => attribute.Value.Contains(
                "RibbonKit.Brushes.ApplicationButton.InnerGlow",
                StringComparison.Ordinal));

        XElement pressedTrigger = Assert.Single(
            applicationButton.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "IsPressed"
                && (string?)trigger.Attribute("Value") == "True");
        Assert.Contains(
            pressedTrigger.Elements(Presentation + "Setter"),
            setter => (string?)setter.Attribute("TargetName") == "InnerRim"
                && (string?)setter.Attribute("Property") == "Opacity"
                && (string?)setter.Attribute("Value") == "0");
    }

    [Fact]
    public void Every_theme_defines_the_file_button_inner_glow_token()
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
            XElement brush = Resource(document, "RibbonKit.Brushes.ApplicationButton.InnerGlow");
            XName expectedType = themeFile.StartsWith("Tokens.Office2010", StringComparison.Ordinal)
                ? Presentation + "RadialGradientBrush"
                : Presentation + "SolidColorBrush";
            Assert.Equal(expectedType, brush.Name);
        }
    }

    [Fact]
    public void Classic2010_backstage_selection_is_square_and_content_casts_a_drop_shadow()
    {
        XDocument document = XDocument.Load(ThemePart("Controls.Backstage.xaml"));

        Assert.DoesNotContain(
            document.Descendants().Attributes(Xaml + "Name"),
            attribute => attribute.Value == "Classic2010Pointer");

        XElement selectedTrigger = Assert.Single(
            document.Descendants(Presentation + "MultiTrigger"),
            trigger => trigger
                .Element(Presentation + "MultiTrigger.Conditions")!
                .Elements(Presentation + "Condition")
                .Any(condition => (string?)condition.Attribute("Property") == "controls:Backstage.Design"
                    && (string?)condition.Attribute("Value") == "Classic2010")
                && trigger
                    .Element(Presentation + "MultiTrigger.Conditions")!
                    .Elements(Presentation + "Condition")
                    .Any(condition => (string?)condition.Attribute("Property") == "IsSelected"
                        && (string?)condition.Attribute("Value") == "True"));
        Assert.Contains(
            selectedTrigger.Elements(Presentation + "Setter"),
            setter => (string?)setter.Attribute("TargetName") == "Chrome"
                && (string?)setter.Attribute("Property") == "Background"
                && setter.Attribute("Value")!.Value.Contains(
                    "RibbonKit.Brushes.Backstage.ItemSelectedGlass",
                    StringComparison.Ordinal));
        Assert.DoesNotContain(
            selectedTrigger.Elements(Presentation + "Setter"),
            setter => (string?)setter.Attribute("TargetName") == "Chrome"
                && (string?)setter.Attribute("Property") == "Margin");

        XElement contentArea = Assert.Single(
            document.Descendants(Presentation + "Border"),
            element => (string?)element.Attribute(Xaml + "Name") == "ContentArea");

        XElement backstageTemplate = Assert.Single(
            contentArea.Ancestors(Presentation + "ControlTemplate"));
        XElement classicTrigger = Assert.Single(
            backstageTemplate.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "controls:Backstage.Design"
                && (string?)trigger.Attribute("Value") == "Classic2010"
                && trigger
                    .Elements(Presentation + "Setter")
                    .Any(setter => (string?)setter.Attribute("TargetName") == "ContentArea"));
        Assert.Contains(
            classicTrigger.Elements(Presentation + "Setter"),
            setter => (string?)setter.Attribute("TargetName") == "ContentArea"
                && (string?)setter.Attribute("Property") == "Effect"
                && setter.Attribute("Value")!.Value.Contains(
                    "RibbonKit.Effects.Backstage.ContentShadow",
                    StringComparison.Ordinal));

        XDocument theme = XDocument.Load(ThemePart("Tokens.Office2010.xaml"));
        XElement selectedGlass = Resource(theme, "RibbonKit.Brushes.Backstage.ItemSelectedGlass");
        Assert.Equal(Presentation + "RadialGradientBrush", selectedGlass.Name);
        Assert.Equal("0.43,0.78", (string?)selectedGlass.Attribute("Center"));
        Assert.Equal(4, Stops(selectedGlass).Length);
    }

    [Fact]
    public void Modern_backstage_content_uses_the_same_theme_scoped_shadow_hook()
    {
        XDocument document = XDocument.Load(ThemePart("Controls.Backstage.xaml"));
        XElement contentArea = Assert.Single(
            document.Descendants(Presentation + "Border"),
            element => (string?)element.Attribute(Xaml + "Name") == "ContentArea");
        XElement backstageTemplate = Assert.Single(contentArea.Ancestors(Presentation + "ControlTemplate"));
        XElement modernTrigger = Assert.Single(
            backstageTemplate.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "controls:Backstage.Design"
                && (string?)trigger.Attribute("Value") == "Modern");

        Assert.Contains(
            modernTrigger.Elements(Presentation + "Setter"),
            setter => (string?)setter.Attribute("TargetName") == "ContentArea"
                && (string?)setter.Attribute("Property") == "Effect"
                && setter.Attribute("Value")!.Value.Contains(
                    "RibbonKit.Effects.Backstage.ContentShadow",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Every_theme_defines_the_backstage_content_drop_shadow_token()
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
            XElement effect = Resource(document, "RibbonKit.Effects.Backstage.ContentShadow");
            Assert.Equal(Presentation + "DropShadowEffect", effect.Name);
            string expectedOpacity = themeFile switch
            {
                "Tokens.Office2007.xaml" or "Tokens.Office2007.Dark.xaml" => "0.24",
                "Tokens.Office2010.xaml" or "Tokens.Office2010.Dark.xaml" => "0.24",
                "Tokens.Office2024.xaml" => "0.12",
                "Tokens.Office2024.Dark.xaml" => "0.14",
                _ => "0",
            };
            Assert.Equal(expectedOpacity, (string?)effect.Attribute("Opacity"));
        }
    }

    [Fact]
    public void Compact_button_templates_do_not_draw_persistent_idle_outlines()
    {
        XDocument buttons = XDocument.Load(ThemePart("Controls.Buttons.xaml"));
        XDocument dropDowns = XDocument.Load(ThemePart("Controls.DropDowns.xaml"));
        Assert.DoesNotContain(
            buttons.Descendants().Concat(dropDowns.Descendants()).Attributes(),
            attribute => attribute.Value.Contains("IdleOutline", StringComparison.Ordinal)
                || attribute.Value.Contains("RibbonKit.Brushes.Control.IdleBorder", StringComparison.Ordinal));

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
            XDocument theme = XDocument.Load(ThemePart(themeFile));
            Assert.DoesNotContain(
                theme.Root!.Elements(),
                element => (string?)element.Attribute(Xaml + "Key")
                    == "RibbonKit.Brushes.Control.IdleBorder");
        }
    }

    [Fact]
    public void Colored_Office2010_title_bar_uses_smooth_glass_without_a_dark_lower_half()
    {
        MethodInfo captionGlass = typeof(ThemeManager).GetMethod(
            "CaptionGlass",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Color accent = Color.FromRgb(0x2B, 0x57, 0x9A);
        var brush = Assert.IsType<LinearGradientBrush>(captionGlass.Invoke(null, [accent]));

        Assert.Equal(5, brush.GradientStops.Count);
        Assert.Equal(accent, brush.GradientStops[2].Color);
        Assert.True(
            brush.GradientStops.Select(stop => stop.Offset).SequenceEqual(
                brush.GradientStops.Select(stop => stop.Offset).OrderBy(offset => offset).Distinct()));
        double baseLuminance = Luminance(accent);
        Assert.All(
            brush.GradientStops,
            stop => Assert.True(
                Luminance(stop.Color) >= baseLuminance,
                $"The colored title glass darkened below the accent at offset {stop.Offset}."));
        Assert.True(Luminance(brush.GradientStops[^1].Color) > baseLuminance);
    }

    [Theory]
    [InlineData("Controls.Buttons.xaml", 2)]
    [InlineData("Controls.DropDowns.xaml", 5)]
    [InlineData("Controls.Groups.xaml", 2)]
    [InlineData("Controls.Galleries.xaml", 2)]
    public void Every_ribbon_button_family_consumes_the_shared_hover_glass(
        string templateFile,
        int minimumConsumers)
    {
        XDocument document = XDocument.Load(ThemePart(templateFile));
        int consumers = document
            .Descendants()
            .Attributes()
            .Count(attribute => attribute.Value.Contains(
                "RibbonKit.Brushes.Control.HoverBackground",
                StringComparison.Ordinal));

        Assert.True(
            consumers >= minimumConsumers,
            $"{templateFile} exposes only {consumers} shared hover-glass consumers; expected at least {minimumConsumers}.");
    }

    private static XElement Resource(XDocument document, string key) =>
        Assert.Single(
            document.Root!.Elements(),
            element => (string?)element.Attribute(Xaml + "Key") == key);

    private static XElement[] Stops(XElement brush) =>
        brush.Elements(Presentation + "GradientStop").ToArray();

    private static double Offset(XElement stop) =>
        double.Parse(stop.Attribute("Offset")!.Value, CultureInfo.InvariantCulture);

    private static double Luminance(XElement stop)
    {
        string value = stop.Attribute("Color")!.Value.TrimStart('#');
        Assert.True(value.Length is 6 or 8, $"Unsupported color literal #{value}.");
        int start = value.Length == 8 ? 2 : 0;
        byte red = byte.Parse(value.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        byte green = byte.Parse(value.AsSpan(start + 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        byte blue = byte.Parse(value.AsSpan(start + 4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return (0.2126d * red) + (0.7152d * green) + (0.0722d * blue);
    }

    private static double Luminance(Color color) =>
        (0.2126d * color.R) + (0.7152d * color.G) + (0.0722d * color.B);

    private static string ThemePart(string name) =>
        Path.Combine(RepositoryRoot(), "src", "RibbonKit", "Themes", name);

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, $"Could not find RibbonKit.sln above {AppContext.BaseDirectory}.");
        return directory!.FullName;
    }
}
