using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Xml.Linq;
using RibbonKit.Automation;
using RibbonKit.Controls;
using Xunit;
using RibbonCheckBoxAutomationPeer = RibbonKit.Automation.RibbonCheckBoxAutomationPeer;
using RibbonKeyTipService = RibbonKit.Controls.KeyTipService;
using RibbonRadioButtonAutomationPeer = RibbonKit.Automation.RibbonRadioButtonAutomationPeer;

namespace RibbonKit.Tests;

/// <summary>Guards the runtime, accessibility, theme, and designer contracts of compact option controls.</summary>
public sealed class RibbonOptionControlTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Option_controls_are_lookless_wpf_controls_with_ribbon_style_keys() => Sta.Run(() =>
    {
        var checkBox = new TestRibbonCheckBox();
        var radioButton = new TestRibbonRadioButton();

        Assert.IsAssignableFrom<CheckBox>(checkBox);
        Assert.IsAssignableFrom<RadioButton>(radioButton);
        Assert.Equal(typeof(RibbonCheckBox), checkBox.StyleKey);
        Assert.Equal(typeof(RibbonRadioButton), radioButton.StyleKey);
    });

    [Fact]
    public void Option_controls_create_rich_screen_tips() => Sta.Run(() =>
    {
        var checkBox = new RibbonCheckBox
        {
            ScreenTipTitle = "Show ruler",
            ScreenTipText = "Shows the horizontal ruler.",
        };
        var radioButton = new RibbonRadioButton
        {
            ScreenTipTitle = "Compact",
            ScreenTipText = "Uses compact spacing.",
        };

        var checkTip = Assert.IsType<RibbonScreenTip>(checkBox.ToolTip);
        Assert.Equal("Show ruler", checkTip.Title);
        Assert.Equal("Shows the horizontal ruler.", checkTip.Description);
        var radioTip = Assert.IsType<RibbonScreenTip>(radioButton.ToolTip);
        Assert.Equal("Compact", radioTip.Title);
        Assert.Equal("Uses compact spacing.", radioTip.Description);
    });

    [Fact]
    public void Automation_peers_preserve_toggle_and_selection_patterns_and_use_headers_as_names() => Sta.Run(() =>
    {
        var checkBox = new RibbonCheckBox { Header = "Show ruler" };
        var radioButton = new RibbonRadioButton { Header = "Compact" };

        var checkPeer = Assert.IsType<RibbonCheckBoxAutomationPeer>(
            UIElementAutomationPeer.CreatePeerForElement(checkBox));
        Assert.Equal("Show ruler", checkPeer.GetName());
        Assert.Equal(AutomationControlType.CheckBox, checkPeer.GetAutomationControlType());
        Assert.NotNull(checkPeer.GetPattern(PatternInterface.Toggle));

        var radioPeer = Assert.IsType<RibbonRadioButtonAutomationPeer>(
            UIElementAutomationPeer.CreatePeerForElement(radioButton));
        Assert.Equal("Compact", radioPeer.GetName());
        Assert.Equal(AutomationControlType.RadioButton, radioPeer.GetAutomationControlType());
        Assert.NotNull(radioPeer.GetPattern(PatternInterface.SelectionItem));
    });

    [Fact]
    public void KeyTip_invocation_toggles_checks_and_selects_radio_groups() => Sta.Run(() =>
    {
        var checkBox = new RibbonCheckBox();
        int checkClicks = 0;
        checkBox.Click += (_, _) => checkClicks++;
        RibbonKeyTipService.InvokeControl(checkBox);
        Assert.True(checkBox.IsChecked);
        Assert.Equal(1, checkClicks);

        var compact = new RibbonRadioButton { GroupName = "Density", IsChecked = true };
        var comfortable = new RibbonRadioButton { GroupName = "Density" };
        int radioClicks = 0;
        comfortable.Click += (_, _) => radioClicks++;
        var parent = new StackPanel { Children = { compact, comfortable } };
        Assert.Same(parent, comfortable.Parent);

        RibbonKeyTipService.InvokeControl(comfortable);

        Assert.False(compact.IsChecked);
        Assert.True(comfortable.IsChecked);
        Assert.Equal(1, radioClicks);
    });

    [Fact]
    public void Compact_inputs_participate_in_KeyTip_discovery_and_use_their_headers() => Sta.Run(() =>
    {
        var checkBox = new RibbonCheckBox { Header = "Show ruler" };
        var radioButton = new RibbonRadioButton { Header = "Compact" };
        var textBox = new RibbonTextBox { Header = "Find" };

        Assert.True(RibbonKeyTipService.IsRibbonKeyTipControl(checkBox));
        Assert.True(RibbonKeyTipService.IsRibbonKeyTipControl(radioButton));
        Assert.True(RibbonKeyTipService.IsRibbonKeyTipControl(textBox));

        Assert.Equal("Show ruler", RibbonKeyTipService.GetLabel(checkBox));
        Assert.Equal("Compact", RibbonKeyTipService.GetLabel(radioButton));
        Assert.Equal("Find", RibbonKeyTipService.GetLabel(textBox));
    });

    [Fact]
    public void Every_theme_variant_defines_the_checked_input_glyph_token()
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
            XElement glyph = Assert.Single(
                document.Root!.Elements(),
                element => (string?)element.Attribute(Xaml + "Key") == "RibbonKit.Brushes.Input.Glyph");
            Assert.Equal(Presentation + "SolidColorBrush", glyph.Name);
        }
    }

    [Fact]
    public void Shared_templates_and_designer_offer_both_option_controls()
    {
        XDocument templates = XDocument.Load(ThemePart("Controls.Buttons.xaml"));
        string[] targetTypes = templates.Root!
            .Elements(Presentation + "Style")
            .Select(style => (string?)style.Attribute("TargetType") ?? string.Empty)
            .ToArray();
        Assert.Contains("{x:Type controls:RibbonCheckBox}", targetTypes);
        Assert.Contains("{x:Type controls:RibbonRadioButton}", targetTypes);

        foreach (string targetType in new[] { "RibbonCheckBox", "RibbonRadioButton" })
        {
            XElement template = Assert.Single(
                templates.Descendants(Presentation + "ControlTemplate"),
                element => ((string?)element.Attribute("TargetType"))?.Contains(targetType) == true);
            XElement chrome = Assert.Single(
                template.Descendants(Presentation + "Border"),
                element => (string?)element.Attribute(Xaml + "Name") == "Chrome");
            XElement hoverWash = Assert.Single(
                template.Descendants(Presentation + "Border"),
                element => (string?)element.Attribute(Xaml + "Name") == "HoverWash");
            XElement contentGrid = Assert.Single(
                template.Descendants(Presentation + "Grid"),
                element => (string?)element.Attribute(Xaml + "Name") == "ContentGrid");

            Assert.Null(chrome.Attribute("Padding"));
            Assert.Same(chrome, hoverWash.Parent!.Parent);
            Assert.Equal("5,1,5,1", (string?)contentGrid.Attribute("Margin"));
        }

        string repository = RepositoryRoot();
        string editor = File.ReadAllText(Path.Combine(repository, "src", "RibbonKit.Design", "RibbonEditorWindow.cs"));
        string metadata = File.ReadAllText(Path.Combine(repository, "src", "RibbonKit.Design", "Metadata.cs"));
        string toolbox = File.ReadAllText(Path.Combine(repository, "src", "RibbonKit", "tools", "VisualStudioToolsManifest.xml"));

        Assert.Contains("MakeControlMenuItem(\"Check Box\", \"RibbonCheckBox\", true)", editor);
        Assert.Contains("MakeControlMenuItem(\"Radio Button\", \"RibbonRadioButton\", true)", editor);
        Assert.Contains("RibbonCheckBoxType, controlProvider", metadata);
        Assert.Contains("RibbonRadioButtonType, controlProvider", metadata);
        Assert.Contains("RibbonKit.Controls.RibbonCheckBox", toolbox);
        Assert.Contains("RibbonKit.Controls.RibbonRadioButton", toolbox);
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

        Assert.NotNull(directory);
        return directory!.FullName;
    }

    private sealed class TestRibbonCheckBox : RibbonCheckBox
    {
        internal object? StyleKey => DefaultStyleKey;
    }

    private sealed class TestRibbonRadioButton : RibbonRadioButton
    {
        internal object? StyleKey => DefaultStyleKey;
    }
}
