using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using RibbonKit.Controls;
using KeyTipService = RibbonKit.Controls.KeyTipService;
using Xunit;

namespace RibbonKit.Tests;

public sealed class CommandActivationAuditTests
{
    [Theory]
    [InlineData("button")]
    [InlineData("split")]
    [InlineData("menu")]
    [InlineData("toggle")]
    [InlineData("check")]
    [InlineData("radio")]
    [InlineData("peerless")]
    [InlineData("native-button")]
    [InlineData("native-toggle")]
    [InlineData("native-check")]
    [InlineData("native-radio")]
    public void Activation_executes_commands_once_and_respects_disabled_state(string kind) => Sta.Run(() =>
    {
        int executions = 0, clicks = 0;
        var parameter = new object();
        var command = new CallbackCommand(value => { Assert.Same(parameter, value); executions++; });
        Control source = kind switch
        {
            "button" => new RibbonButton(), "split" => new RibbonSplitButton(),
            "menu" => new RibbonMenuItem(), "toggle" => new RibbonToggleButton(),
            "check" => new RibbonCheckBox(), "radio" => new RibbonRadioButton(),
            "native-button" => new Button(), "native-toggle" => new ToggleButton(),
            "native-check" => new CheckBox(), "native-radio" => new RadioButton(), _ => new PeerlessButton(),
        };
        if (source is RibbonSplitButton split)
        { split.CommandParameter = parameter; split.Command = command; split.Click += (_, _) => clicks++; }
        else
        {
            var button = Assert.IsAssignableFrom<ButtonBase>(source);
            button.CommandParameter = parameter; button.Command = command; button.Click += (_, _) => clicks++;
        }
        WithWindow(source, () =>
        {
            KeyTipService.InvokeControl(source); Drain();
            Assert.Equal(1, executions);
            Assert.Equal(1, clicks);
            if (source is ToggleButton stateful) Assert.True(stateful.IsChecked);
            source.IsEnabled = false;
            KeyTipService.InvokeControl(source); Drain();
            Assert.Equal(1, executions);
            Assert.Equal(1, clicks);
        });
    });

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Other_button_copies_execute_the_current_source_command_once(bool split, bool customGroup) => Sta.Run(() =>
    {
        var ribbon = new Ribbon();
        int executions = 0;
        var parameter = new object();
        var replacement = new CallbackCommand(value => { Assert.Same(parameter, value); executions++; });
        FrameworkElement source = split ? new RibbonSplitButton { Header = "Action" } : new RibbonButton { Header = "Action" };
        var copy = ribbon.CreateCommandProxy(source, customGroup ? RibbonControlSize.Medium : RibbonControlSize.Small);
        if (split) { ((RibbonSplitButton)source).CommandParameter = parameter; ((RibbonSplitButton)source).Command = replacement; }
        else { ((RibbonButton)source).CommandParameter = parameter; ((RibbonButton)source).Command = replacement; }
        var panel = new StackPanel(); panel.Children.Add(source); panel.Children.Add(copy);
        WithWindow(panel, () =>
        {
            KeyTipService.InvokeControl(copy); Drain();
            Assert.Equal(1, executions);
            source.IsEnabled = false; Drain();
            KeyTipService.InvokeControl(copy); Drain();
            Assert.Equal(1, executions);
        });
    });

    [Fact]
    public void Native_three_state_KeyTips_keep_the_native_cycle_before_command_execution() => Sta.Run(() =>
    {
        var states = new List<bool?>();
        var toggle = new ToggleButton { IsThreeState = true };
        toggle.Command = new CallbackCommand(_ => states.Add(toggle.IsChecked));
        WithWindow(toggle, () =>
        {
            for (int i = 0; i < 3; i++) { KeyTipService.InvokeControl(toggle); Drain(); }
            Assert.Equal(new bool?[] { true, null, false }, states);
        });
    });

    [Fact]
    public void Native_radio_KeyTips_keep_group_exclusivity_and_repeat_activation() => Sta.Run(() =>
    {
        int executions = 0;
        var first = new RadioButton { GroupName = "alignment", IsChecked = true };
        var second = new RadioButton { GroupName = "alignment", Command = new CallbackCommand(_ => executions++) };
        var panel = new StackPanel(); panel.Children.Add(first); panel.Children.Add(second);
        WithWindow(panel, () =>
        {
            KeyTipService.InvokeControl(second); Drain();
            Assert.False(first.IsChecked); Assert.True(second.IsChecked); Assert.Equal(1, executions);
            KeyTipService.InvokeControl(second); Drain();
            Assert.False(first.IsChecked); Assert.True(second.IsChecked); Assert.Equal(2, executions);
        });
    });

    [Fact]
    public void Custom_automation_provider_that_already_clicks_is_not_executed_twice() => Sta.Run(() =>
    {
        int executions = 0, clicks = 0;
        var toggle = new ClickingToggle { Command = new CallbackCommand(_ => executions++) };
        toggle.Click += (_, _) => clicks++;
        WithWindow(toggle, () =>
        {
            KeyTipService.InvokeControl(toggle); Drain();
            Assert.True(toggle.IsChecked); Assert.Equal(1, clicks); Assert.Equal(1, executions);
        });
    });

    private static void WithWindow(FrameworkElement content, Action check)
    {
        var window = new Window { Content = content, Width = 400, Height = 200, Left = -10000,
            Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try { window.Show(); Drain(); check(); }
        finally { window.Close(); Drain(); }
    }

    private static void Drain() => Sta.Drain(DispatcherPriority.ApplicationIdle);

    private sealed class PeerlessButton : Button
    {
        protected override AutomationPeer OnCreateAutomationPeer() => null!;
    }

    private sealed class ClickingToggle : ToggleButton
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new ClickingPeer(this);
        internal void ActivateNative() => OnClick();
    }

    private sealed class ClickingPeer(ClickingToggle owner) : FrameworkElementAutomationPeer(owner), IToggleProvider
    {
        public void Toggle() => ((ClickingToggle)Owner).ActivateNative();
        public ToggleState ToggleState => ((ClickingToggle)Owner).IsChecked == true ? ToggleState.On : ToggleState.Off;
        public override object? GetPattern(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.Toggle ? this : base.GetPattern(patternInterface);
    }

    private sealed class CallbackCommand(Action<object?> execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
