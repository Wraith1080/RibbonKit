using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Public customization and real WPF formatting, without Writer/Showcase code or resources.
internal static class CommandCopyChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var editor = new RichTextBox(new FlowDocument(new Paragraph(new Run("portable selected text"))));
        var bold = new RibbonToggleButton { Header = "Bold", CommandTarget = editor, Command = EditingCommands.ToggleBold };
        int boldClicks = 0;
        bold.Click += (_, _) => boldClicks++;
        var parameter = new object();
        int actions = 0, splitActions = 0;
        var action = new RibbonButton { Header = "Action", CommandParameter = parameter,
            Command = new CallbackCommand(value => { Assert.Same(parameter, value); actions++; }) };
        var split = new RibbonSplitButton { Header = "Split", CommandParameter = parameter,
            Command = new CallbackCommand(value => { Assert.Same(parameter, value); splitActions++; }) };
        var sourceGroup = new RibbonGroup { Header = "Commands", CanResize = false };
        var sources = new FrameworkElement[] { bold, action, split };
        for (int i = 0; i < sources.Length; i++) { Ribbon.SetCommandId(sources[i], $"cmd.{i}"); sourceGroup.Items.Add(sources[i]); }
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(sourceGroup);
        Ribbon.SetCommandId(tab, "tab.home"); Ribbon.SetCommandId(sourceGroup, "group.commands");
        var ribbon = new Ribbon(); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        foreach (var source in sources) Assert.True(ribbon.AddToQuickAccess(source));

        // Restore both kinds of copies through the public saved-customization contract.
        var saved = JsonNode.Parse(RibbonCustomizationSerializer.Serialize(ribbon))!;
        saved["Tabs"]![0]!["Groups"]!.AsArray().Add(new JsonObject
        {
            ["Id"] = "custom:favorites", ["IsCustom"] = true, ["Header"] = "Favorites", ["Layout"] = "Stacked",
            ["Commands"] = new JsonArray(sources.Select((_, i) => (JsonNode)new JsonObject
                { ["SourceId"] = $"cmd.{i}", ["Size"] = "Medium" }).ToArray()),
        });
        RibbonCustomizationSerializer.Apply(ribbon, saved.ToJsonString());
        var custom = Assert.Single(tab.Groups, Ribbon.GetIsCustom);
        var panel = new DockPanel(); DockPanel.SetDock(ribbon, Dock.Top); panel.Children.Add(ribbon); panel.Children.Add(editor);
        var window = new RibbonWindow { Content = panel, Width = 1100, Height = 550,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        int layouts = 0, formattingRounds = 0;
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            window.Show(); Layout();
            foreach (var theme in new[] { RibbonTheme.Office2010, RibbonTheme.Office2024Flat, RibbonTheme.CrystalLight })
            foreach (bool dark in new[] { false, true })
            foreach (var density in Enum.GetValues<RibbonDensity>())
            foreach (var direction in Enum.GetValues<FlowDirection>())
            foreach (var position in Enum.GetValues<RibbonQuickAccessPosition>())
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                ribbon.Density = density; window.FlowDirection = direction; ribbon.QuickAccessPosition = position;
                Layout(); editor.SelectAll();
                foreach (var host in new[] { ribbon.QuickAccessItems.OfType<FrameworkElement>().ToArray(), custom.Items.OfType<FrameworkElement>().ToArray() })
                {
                    var copy = Assert.Single(host.OfType<RibbonToggleButton>());
                    // RibbonKit's public Toggle provider completes the native click/command path.
                    // One request must produce one source Click and one formatting change.
                    int beforeBold = boldClicks;
                    Toggle(copy); Layout();
                    Assert.Equal(beforeBold + 1, boldClicks);
                    Assert.Equal(FontWeights.Bold, editor.Selection.GetPropertyValue(TextElement.FontWeightProperty));
                    Assert.True(bold.IsChecked);
                    Toggle(copy); Layout();
                    Assert.Equal(beforeBold + 2, boldClicks);
                    Assert.Equal(FontWeights.Normal, editor.Selection.GetPropertyValue(TextElement.FontWeightProperty));
                    Assert.False(bold.IsChecked);
                    Assert.Equal("portable selected text", editor.Selection.Text.TrimEnd('\r', '\n'));
                    formattingRounds++;
                    int before = actions;
                    Assert.Single(host.OfType<RibbonButton>()).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout();
                    Assert.Equal(before + 1, actions);
                    before = splitActions;
                    var splitCopy = Assert.Single(host.OfType<RibbonSplitButton>());
                    splitCopy.RaiseEvent(new RoutedEventArgs(RibbonSplitButton.ClickEvent, splitCopy)); Layout();
                    Assert.Equal(before + 1, splitActions);
                }
                layouts++;
            }
            Console.WriteLine($"Command copies: {layouts} theme/palette/density/direction/placement layouts; {formattingRounds} Bold round trips and {actions + splitActions} ordinary/split executions passed after public customization restore.");
        }
        finally
        {
            window.Close(); ThemeManager.SetDarkMode(application, false);
            ThemeManager.Apply(application, RibbonTheme.Office2024); RibbonAnimation.GlobalLevel = motion;
        }
        void Layout()
        {
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }
    }

    private static void Toggle(RibbonToggleButton copy)
    {
        ((IToggleProvider)UIElementAutomationPeer.CreatePeerForElement(copy)!.GetPattern(PatternInterface.Toggle)).Toggle();
    }

    private sealed class CallbackCommand(Action<object?> execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
