using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Guards the split and merged-dropdown visual-state contract of application-menu rows.</summary>
public class ApplicationMenuVisualStateTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Split_divider_is_hidden_only_in_the_neutral_resting_state()
    {
        XElement template = ItemTemplate();

        Assert.DoesNotContain(
            template.Descendants(Presentation + "Trigger"),
            trigger =>
                (string?)trigger.Attribute("Property") == "IsSplitPresentation"
                && Sets(trigger, "SplitLine", "Visibility", "Visible"));

        XElement[] hoverStates = template
            .Descendants(Presentation + "MultiTrigger")
            .Where(trigger =>
                HasCondition(trigger, null, "IsSplitPresentation", "True")
                && (HasCondition(trigger, "PART_Primary", "IsMouseOver", "True")
                    || HasCondition(trigger, "PART_Arrow", "IsMouseOver", "True"))
                && Sets(trigger, "SplitLine", "Visibility", "Visible"))
            .ToArray();

        Assert.Equal(2, hoverStates.Length);
        Assert.Contains(hoverStates, trigger => HasCondition(trigger, "PART_Primary", "IsMouseOver", "True"));
        Assert.Contains(hoverStates, trigger => HasCondition(trigger, "PART_Arrow", "IsMouseOver", "True"));

        XElement activeSplit = Assert.Single(
            template.Descendants(Presentation + "MultiTrigger"),
            trigger =>
                HasCondition(trigger, null, "IsActive", "True")
                && HasCondition(trigger, null, "IsSplitPresentation", "True"));
        Assert.True(Sets(activeSplit, "SplitLine", "Visibility", "Visible"));
    }

    [Fact]
    public void Pane_content_cannot_reverse_inherit_visual_hover_to_the_nav_row()
    {
        XElement template = ItemTemplate();

        Assert.DoesNotContain(
            template.Descendants(Presentation + "Trigger"),
            trigger =>
                trigger.Attribute("SourceName") is null
                && (string?)trigger.Attribute("Property") == "IsMouseOver");

        Assert.Contains(
            template.Descendants(Presentation + "Trigger"),
            trigger =>
                (string?)trigger.Attribute("SourceName") == "PART_Primary"
                && (string?)trigger.Attribute("Property") == "IsMouseOver");
        Assert.Contains(
            template.Descendants(Presentation + "Trigger"),
            trigger =>
                (string?)trigger.Attribute("SourceName") == "PART_Arrow"
                && (string?)trigger.Attribute("Property") == "IsMouseOver");

        XElement activeSplit = Assert.Single(
            template.Descendants(Presentation + "MultiTrigger"),
            trigger =>
                HasCondition(trigger, null, "IsActive", "True")
                && HasCondition(trigger, null, "IsSplitPresentation", "True"));

        Assert.True(SetsResource(
            activeSplit,
            "MainFill",
            "Opacity",
            "RibbonKit.Metrics.ApplicationMenuDimOpacity"));
        Assert.False(Sets(activeSplit, "Outline", "Opacity", "0"));

        XElement active = Assert.Single(
            template.Descendants(Presentation + "Trigger"),
            trigger =>
                (string?)trigger.Attribute("Property") == "IsActive"
                && (string?)trigger.Attribute("Value") == "True");
        Assert.True(Sets(active, "Outline", "Opacity", "1"));
    }

    [Theory]
    [InlineData("Office2007", "0.32")]
    [InlineData("Office2010", "0.35")]
    [InlineData("Office2013", "0.4")]
    [InlineData("Office2019", "0.4")]
    [InlineData("Office2024", "0.4")]
    public void Active_split_command_half_keeps_its_theme_dim_level(string theme, string expected)
    {
        var document = XDocument.Load(ThemePath(theme));
        XElement opacity = Assert.Single(
            document.Root!.Elements(),
            element =>
                (string?)element.Attribute(Xaml + "Key")
                == "RibbonKit.Metrics.ApplicationMenuDimOpacity");

        Assert.Equal(expected, opacity.Value.Trim());
    }

    [Fact]
    public void Non_split_dropdown_press_paints_both_hit_areas_as_one_surface()
    {
        XElement template = ItemTemplate();
        XElement[] mergedPressStates = template
            .Descendants(Presentation + "MultiTrigger")
            .Where(trigger =>
                HasCondition(trigger, null, "IsSplitPresentation", "False")
                && trigger.Descendants(Presentation + "Condition")
                    .Any(condition => (string?)condition.Attribute("Property") == "IsPressed"))
            .ToArray();

        Assert.Equal(2, mergedPressStates.Length);
        Assert.Contains(mergedPressStates, trigger => HasCondition(trigger, "PART_Primary", "IsPressed", "True"));
        Assert.Contains(mergedPressStates, trigger => HasCondition(trigger, "PART_Arrow", "IsPressed", "True"));

        foreach (XElement state in mergedPressStates)
        {
            Assert.True(SetsResource(state, "MainFill", "Background", "RibbonKit.Brushes.Control.PressedBackground"));
            Assert.True(SetsResource(state, "ArrowFill", "Background", "RibbonKit.Brushes.Control.PressedBackground"));
        }
    }

    private static bool HasCondition(
        XElement trigger,
        string? sourceName,
        string property,
        string value) =>
        trigger.Descendants(Presentation + "Condition").Any(condition =>
            (string?)condition.Attribute("SourceName") == sourceName
            && (string?)condition.Attribute("Property") == property
            && (string?)condition.Attribute("Value") == value);

    private static bool Sets(
        XElement trigger,
        string targetName,
        string property,
        string value) =>
        trigger.Elements(Presentation + "Setter").Any(setter =>
            (string?)setter.Attribute("TargetName") == targetName
            && (string?)setter.Attribute("Property") == property
            && (string?)setter.Attribute("Value") == value);

    private static bool SetsResource(
        XElement trigger,
        string targetName,
        string property,
        string key) =>
        trigger.Elements(Presentation + "Setter").Any(setter =>
            (string?)setter.Attribute("TargetName") == targetName
            && (string?)setter.Attribute("Property") == property
            && (string?)setter.Attribute("Value") == $"{{DynamicResource {key}}}");

    private static XElement ItemTemplate()
    {
        var document = XDocument.Load(ApplicationMenuTemplatePath());
        return Assert.Single(
            document.Descendants(Presentation + "ControlTemplate"),
            template => ((string?)template.Attribute("TargetType"))?.Contains("RibbonApplicationMenuItem") == true);
    }

    private static string ApplicationMenuTemplatePath() =>
        Path.Combine(RepositoryRoot(), "src", "RibbonKit", "Themes", "Controls.ApplicationMenu.xaml");

    private static string ThemePath(string theme) =>
        Path.Combine(RepositoryRoot(), "src", "RibbonKit", "Themes", $"Tokens.{theme}.xaml");

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
