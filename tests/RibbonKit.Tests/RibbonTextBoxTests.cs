using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using RibbonKit.Controls;
using Xunit;
using RibbonKeyTipService = RibbonKit.Controls.KeyTipService;
using RibbonTextBoxAutomationPeer = RibbonKit.Automation.RibbonTextBoxAutomationPeer;

namespace RibbonKit.Tests;

/// <summary>Guards the runtime, accessibility, template, and designer contracts of RibbonTextBox.</summary>
public sealed class RibbonTextBoxTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Text_box_is_a_lookless_native_editor_with_a_ribbon_style_key() => Sta.Run(() =>
    {
        var textBox = new TestRibbonTextBox();

        Assert.IsAssignableFrom<TextBox>(textBox);
        Assert.Equal(typeof(RibbonTextBox), textBox.StyleKey);
        Assert.Equal(130d, textBox.InputWidth);
    });

    [Fact]
    public void Native_text_state_and_rich_screen_tip_remain_available() => Sta.Run(() =>
    {
        var textBox = new RibbonTextBox
        {
            Text = "Quarterly report",
            IsReadOnly = true,
            MaxLength = 40,
            ScreenTipTitle = "Document title",
            ScreenTipText = "Edits the current title.",
        };

        Assert.Equal("Quarterly report", textBox.Text);
        Assert.True(textBox.IsReadOnly);
        Assert.Equal(40, textBox.MaxLength);
        var screenTip = Assert.IsType<RibbonScreenTip>(textBox.ToolTip);
        Assert.Equal("Document title", screenTip.Title);
        Assert.Equal("Edits the current title.", screenTip.Description);
    });

    [Fact]
    public void Automation_peer_preserves_text_box_identity_and_value_pattern() => Sta.Run(() =>
    {
        var textBox = new RibbonTextBox { Header = "Find", Text = "RibbonKit" };

        var peer = Assert.IsType<RibbonTextBoxAutomationPeer>(
            UIElementAutomationPeer.CreatePeerForElement(textBox));
        Assert.Equal("Find", peer.GetName());
        Assert.Equal(AutomationControlType.Edit, peer.GetAutomationControlType());
        Assert.NotNull(peer.GetPattern(PatternInterface.Value));
    });

    [Fact]
    public void KeyTip_invocation_gives_the_text_box_logical_focus() => Sta.Run(() =>
    {
        var textBox = new RibbonTextBox();
        var focusScope = new StackPanel { Children = { textBox } };
        FocusManager.SetIsFocusScope(focusScope, true);

        RibbonKeyTipService.InvokeControl(textBox);

        Assert.Same(textBox, FocusManager.GetFocusedElement(focusScope));
    });

    [Fact]
    public void KeyTip_text_input_keeps_its_containing_flyout_open() => Sta.Run(() =>
    {
        Assert.True(RibbonKeyTipService.KeepsContainingSurfaceOpenAfterKeyTip(new RibbonTextBox()));
        Assert.True(RibbonKeyTipService.KeepsContainingSurfaceOpenAfterKeyTip(new RibbonComboBox()));
        Assert.True(RibbonKeyTipService.KeepsContainingSurfaceOpenAfterKeyTip(new InRibbonGallery()));

        Assert.False(RibbonKeyTipService.KeepsContainingSurfaceOpenAfterKeyTip(new RibbonButton()));
        Assert.False(RibbonKeyTipService.KeepsContainingSurfaceOpenAfterKeyTip(new RibbonCheckBox()));
        Assert.False(RibbonKeyTipService.KeepsContainingSurfaceOpenAfterKeyTip(new RibbonRadioButton()));
    });

    [Fact]
    public void Shared_template_and_designer_preserve_native_editing_and_offer_the_control()
    {
        XDocument templates = XDocument.Load(ThemePart("Controls.DropDowns.xaml"));
        XElement style = Assert.Single(
            templates.Root!.Elements(Presentation + "Style"),
            element => (string?)element.Attribute("TargetType") == "{x:Type controls:RibbonTextBox}");
        XElement template = Assert.Single(style.Descendants(Presentation + "ControlTemplate"));
        XElement contentHost = Assert.Single(
            template.Descendants(Presentation + "ScrollViewer"),
            element => (string?)element.Attribute(Xaml + "Name") == "PART_ContentHost");

        Assert.Equal("{TemplateBinding Padding}", (string?)contentHost.Attribute("Padding"));
        Assert.Contains("RibbonKit.Brushes.Control.SurfaceBackground", style.ToString());
        Assert.Contains("RibbonKit.Brushes.Text.Primary", style.ToString());

        string repository = RepositoryRoot();
        string editor = File.ReadAllText(Path.Combine(repository, "src", "RibbonKit.Design", "RibbonEditorWindow.cs"));
        string metadata = File.ReadAllText(Path.Combine(repository, "src", "RibbonKit.Design", "Metadata.cs"));
        string toolbox = File.ReadAllText(Path.Combine(repository, "src", "RibbonKit", "tools", "VisualStudioToolsManifest.xml"));

        Assert.Contains("MakeControlMenuItem(\"Text Box\", \"RibbonTextBox\", true)", editor);
        Assert.Contains("\"RibbonTextBox\" => TextBoxSpecs", editor);
        Assert.Contains("RibbonTextBoxType, controlProvider", metadata);
        Assert.Contains("RibbonKit.Controls.RibbonTextBox", toolbox);
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

    private sealed class TestRibbonTextBox : RibbonTextBox
    {
        internal object? StyleKey => DefaultStyleKey;
    }
}
