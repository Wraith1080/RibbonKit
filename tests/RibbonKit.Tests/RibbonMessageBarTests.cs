using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Xml.Linq;
using RibbonKit.Animation;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Guards the repeatable message-bar API, dismissal behavior, and theme contract.</summary>
public sealed class RibbonMessageBarTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    private static readonly XNamespace RibbonKitNamespace = "urn:ribbonkit";

    [Fact]
    public void Message_bar_is_a_repeatable_items_control_with_open_dismissible_items()
    {
        Assert.True(typeof(RibbonMessageBar).IsSubclassOf(typeof(ItemsControl)));
        Assert.True(typeof(RibbonMessage).IsSubclassOf(typeof(Control)));

        Sta.Run(() =>
        {
            var message = new RibbonMessage();
            Assert.True(message.IsOpen);
            Assert.True(message.IsPresented);
            Assert.True(message.IsDismissible);
            Assert.True(Enum.IsDefined(RibbonAnimationAction.MessageBar));
            Assert.Equal(
                "RibbonMessageBar",
                UIElementAutomationPeer.CreatePeerForElement(new RibbonMessageBar()).GetClassName());
            Assert.Equal(
                "RibbonMessage",
                UIElementAutomationPeer.CreatePeerForElement(message).GetClassName());
        });
    }

    [Fact]
    public void Dismiss_is_idempotent_and_raises_once()
    {
        Sta.Run(() =>
        {
            var first = new RibbonMessage();
            var second = new RibbonMessage();
            var messageBar = new RibbonMessageBar();
            var ribbon = new Ribbon { MessageBar = messageBar };
            messageBar.Items.Add(first);
            messageBar.Items.Add(second);
            int dismissed = 0;
            second.Dismissed += (_, _) => dismissed++;

            Assert.True(messageBar.HasOpenMessages);
            Assert.True(ribbon.HasOpenMessages);
            Assert.False(first.IsLastOpenMessage);
            Assert.True(second.IsLastOpenMessage);

            second.Dismiss();
            second.Dismiss();

            Assert.False(second.IsOpen);
            Assert.False(second.IsPresented);
            Assert.True(messageBar.HasOpenMessages);
            Assert.True(first.IsLastOpenMessage);
            Assert.Equal(1, dismissed);

            first.Dismiss();
            Assert.False(first.IsPresented);
            Assert.False(messageBar.HasOpenMessages);
            Assert.False(ribbon.HasOpenMessages);

            first.IsOpen = true;
            Assert.True(first.IsPresented);
            Assert.True(messageBar.HasOpenMessages);
            Assert.True(ribbon.HasOpenMessages);
        });
    }

    [Fact]
    public void Applying_template_for_pending_open_does_not_enter_close_path()
    {
        Sta.Run(() =>
        {
            var root = new FrameworkElementFactory(typeof(Border), "PART_Root");
            var message = new RibbonMessage
            {
                Template = new ControlTemplate(typeof(RibbonMessage))
                {
                    VisualTree = root,
                },
            };
            FieldInfo pendingField = typeof(RibbonMessage).GetField(
                "_entrancePending",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            pendingField.SetValue(message, true);

            Assert.True(message.ApplyTemplate());

            Assert.True(message.IsOpen);
            Assert.True(message.IsPresented);
            Assert.False((bool)pendingField.GetValue(message)!);
        });
    }

    [Fact]
    public void Shared_template_stacks_messages_and_keeps_action_close_and_icon_states_independent()
    {
        XDocument document = XDocument.Load(ThemePart("Controls.MessageBar.xaml"));

        Assert.Contains(
            document.Descendants(Presentation + "StackPanel"),
            panel => (string?)panel.Attribute("Orientation") == "Vertical");
        Assert.Single(
            document.Descendants().Attributes(Xaml + "Name"),
            name => name.Value == "PART_ActionButton");
        Assert.Single(
            document.Descendants().Attributes(Xaml + "Name"),
            name => name.Value == "PART_CloseButton");
        Assert.Single(
            document.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "Icon"
                && (string?)trigger.Attribute("Value") == "{x:Null}");
        Assert.Contains(
            document.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "IsPresented"
                && (string?)trigger.Attribute("Value") == "False");
        Assert.Contains(
            document.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "IsLastOpenMessage"
                && trigger.Descendants(Presentation + "Setter").Any(
                    setter => (string?)setter.Attribute("Property") == "CornerRadius"));
        Assert.Contains(
            document.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "HasOpenMessages"
                && (string?)trigger.Attribute("Value") == "False");
        Assert.Contains(
            document.Descendants(Presentation + "Setter"),
            setter => (string?)setter.Attribute("Property") == "MinHeight"
                && (string?)setter.Attribute("Value") == "34");
        Assert.Contains(
            document.Descendants(Presentation + "Setter"),
            setter => (string?)setter.Attribute("Property") == "ClipToBounds"
                && (string?)setter.Attribute("Value") == "True");
        Assert.Single(
            document.Descendants().Attributes(Xaml + "Name"),
            name => name.Value == "PART_Root");
        XElement closeButton = Assert.Single(
            document.Descendants(Presentation + "Button"),
            button => (string?)button.Attribute(Xaml + "Name") == "PART_CloseButton");
        Assert.Equal("22", (string?)closeButton.Attribute("Width"));
        Assert.Equal("22", (string?)closeButton.Attribute("Height"));
        Assert.Equal("4,0,8,0", (string?)closeButton.Attribute("Margin"));
        XElement closeGlyphOffset = Assert.Single(
            closeButton.Descendants(Presentation + "TranslateTransform"));
        Assert.Equal("0.5", (string?)closeGlyphOffset.Attribute("X"));
        Assert.Equal("0.5", (string?)closeGlyphOffset.Attribute("Y"));
        XElement closeChrome = Assert.Single(
            closeButton.Descendants(Presentation + "Border"),
            border => (string?)border.Attribute(Xaml + "Name") == "Chrome");
        Assert.Equal("1", (string?)closeChrome.Attribute("BorderThickness"));
        Assert.Equal(
            "{DynamicResource RibbonKit.Metrics.SmallControlCornerRadius}",
            (string?)closeChrome.Attribute("CornerRadius"));
        Assert.Contains(
            closeButton.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "IsMouseOver"
                && trigger.Descendants(Presentation + "Setter").Any(
                    setter => (string?)setter.Attribute("Property") == "BorderBrush"
                        && ((string?)setter.Attribute("Value"))?.Contains(
                            "RibbonKit.Brushes.MessageBar.CloseHoverBorder",
                            StringComparison.Ordinal) == true));

        XDocument ribbonChrome = XDocument.Load(ThemePart("Controls.RibbonChrome.xaml"));
        XElement messageHost = Assert.Single(
            ribbonChrome.Descendants(Presentation + "ContentPresenter"),
            presenter => (string?)presenter.Attribute(Xaml + "Name") == "MessageBarHost");
        Assert.Equal("{TemplateBinding MessageBar}", (string?)messageHost.Attribute("Content"));
        Assert.Contains(
            ribbonChrome.Descendants(Presentation + "Trigger"),
            trigger => (string?)trigger.Attribute("Property") == "HasOpenMessages"
                && (string?)trigger.Attribute("Value") == "True");
        Assert.Contains(
            ribbonChrome.Descendants(Presentation + "DataTrigger"),
            trigger => ((string?)trigger.Attribute("Binding"))?.Contains(
                "HasOpenMessages",
                StringComparison.Ordinal) == true);
        Assert.DoesNotContain(
            ribbonChrome.Descendants(Presentation + "Trigger")
                .Concat(ribbonChrome.Descendants(Presentation + "DataTrigger"))
                .Where(trigger => (string?)trigger.Attribute("Property") == "HasOpenMessages"
                    || ((string?)trigger.Attribute("Binding"))?.Contains(
                        "HasOpenMessages",
                        StringComparison.Ordinal) == true)
                .SelectMany(trigger => trigger.Descendants(Presentation + "Setter")),
            setter => (string?)setter.Attribute("Property") == "Effect");

        string controlCode = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "src", "RibbonKit", "Controls", "RibbonMessageBar.cs"));
        Assert.Contains("_entrancePending = true;", controlCode, StringComparison.Ordinal);
        Assert.Contains("_entranceHandledForCurrentOpen", controlCode, StringComparison.Ordinal);
        Assert.Contains(
            "if (!_entranceHandledForCurrentOpen && !_entrancePending)",
            controlCode,
            StringComparison.Ordinal);
        Assert.Contains("ApplyTemplate();", controlCode, StringComparison.Ordinal);
        Assert.Contains("TryPlayPendingEntrance()", controlCode, StringComparison.Ordinal);
        Assert.Contains("RibbonMotion.PlayOpen(", controlCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Dispatcher.BeginInvoke", controlCode, StringComparison.Ordinal);

        int unloadedStart = controlCode.IndexOf("private void OnUnloaded", StringComparison.Ordinal);
        int beginShowStart = controlCode.IndexOf("private void BeginShow", unloadedStart, StringComparison.Ordinal);
        Assert.True(unloadedStart >= 0 && beginShowStart > unloadedStart);
        Assert.DoesNotContain(
            "_entranceHandledForCurrentOpen = false;",
            controlCode[unloadedStart..beginShowStart],
            StringComparison.Ordinal);
    }

    [Fact]
    public void Every_theme_variant_defines_the_message_bar_palette_and_connection_metrics()
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
        string[] keys =
        [
            "RibbonKit.Brushes.MessageBar.Background",
            "RibbonKit.Brushes.MessageBar.Border",
            "RibbonKit.Brushes.MessageBar.Foreground",
            "RibbonKit.Brushes.MessageBar.IconForeground",
            "RibbonKit.Brushes.MessageBar.CloseHoverBorder",
        ];

        foreach (string themeFile in themeFiles)
        {
            XDocument document = XDocument.Load(ThemePart(themeFile));
            foreach (string key in keys)
            {
                XElement resource = Assert.Single(
                    document.Root!.Elements(),
                    element => (string?)element.Attribute(Xaml + "Key") == key);
                Assert.EndsWith("Brush", resource.Name.LocalName, StringComparison.Ordinal);
            }

            Assert.Equal(
                Presentation + "Thickness",
                Assert.Single(
                    document.Root!.Elements(),
                    element => (string?)element.Attribute(Xaml + "Key")
                        == "RibbonKit.Metrics.QatExtenderMarginMessageBar").Name);
            Assert.Equal(
                Presentation + "CornerRadius",
                Assert.Single(
                    document.Root!.Elements(),
                    element => (string?)element.Attribute(Xaml + "Key")
                        == "RibbonKit.Metrics.QatExtenderCornerRadiusMessageBar").Name);
            Assert.Equal(
                Presentation + "Thickness",
                Assert.Single(
                    document.Root!.Elements(),
                    element => (string?)element.Attribute(Xaml + "Key")
                        == "RibbonKit.Metrics.MessageBar.Margin").Name);
            Assert.Equal(
                Presentation + "CornerRadius",
                Assert.Single(
                    document.Root!.Elements(),
                    element => (string?)element.Attribute(Xaml + "Key")
                        == "RibbonKit.Metrics.MessageBar.LastCornerRadius").Name);
            Assert.Equal(
                Presentation + "Thickness",
                Assert.Single(
                    document.Root!.Elements(),
                    element => (string?)element.Attribute(Xaml + "Key")
                        == "RibbonKit.Metrics.MessageBar.BorderThickness").Name);
        }
    }

    [Fact]
    public void Showcase_and_rtl_lab_start_empty_and_add_messages_from_the_ribbon()
    {
        string sampleRoot = Path.Combine(RepositoryRoot(), "samples", "RibbonKit.Showcase");
        foreach ((string xamlName, string codeName) in new[]
        {
            ("MainWindow.xaml", "MainWindow.xaml.cs"),
            ("LocalizationRtlDemo.xaml", "LocalizationRtlDemo.xaml.cs"),
        })
        {
            XDocument document = XDocument.Load(Path.Combine(sampleRoot, xamlName));
            XElement[] messages = document.Descendants(RibbonKitNamespace + "RibbonMessage").ToArray();
            Assert.Equal(2, messages.Length);
            Assert.All(messages, message => Assert.Equal("False", (string?)message.Attribute("IsOpen")));
            Assert.Contains(
                document.Descendants(RibbonKitNamespace + "RibbonButton"),
                button => (string?)button.Attribute("Click") == "OnAddMessage");

            string code = File.ReadAllText(Path.Combine(sampleRoot, codeName));
            Assert.Contains("private void OnAddMessage", code, StringComparison.Ordinal);
            Assert.Contains("nextMessage.IsOpen = true;", code, StringComparison.Ordinal);
        }
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

        Assert.True(directory is not null, $"Could not find RibbonKit.sln above {AppContext.BaseDirectory}.");
        return directory!.FullName;
    }
}
