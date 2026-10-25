using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public sealed class ToggleAutomationActivationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Toggle_pattern_runs_native_click_and_targeted_command_once_with_bindings_retained(
        bool checkBox, bool threeState) => Sta.Run(() =>
    {
        ToggleButton button = checkBox ? new RibbonCheckBox() : new RibbonToggleButton();
        var state = new State();
        var target = new Button();
        var command = new RoutedCommand();
        var parameter = new object();
        var bindings = new Dictionary<DependencyProperty, Binding>
        {
            [ToggleButton.IsCheckedProperty] = new(nameof(State.Checked)) { Source = state, Mode = BindingMode.TwoWay },
            [UIElement.IsEnabledProperty] = new(nameof(State.Enabled)) { Source = state },
            [ButtonBase.CommandParameterProperty] = new() { Source = parameter },
            [ButtonBase.CommandTargetProperty] = new() { Source = target },
            [ButtonBase.CommandProperty] = new() { Source = command },
        };
        button.IsThreeState = threeState;
        var events = new List<string>();
        button.Checked += (_, _) => events.Add("state");
        button.Unchecked += (_, _) => events.Add("state");
        button.Indeterminate += (_, _) => events.Add("state");
        button.Click += (_, _) => events.Add("click");
        target.CommandBindings.Add(new CommandBinding(command, (_, e) =>
        {
            Assert.Same(parameter, e.Parameter);
            Assert.Same(target, e.OriginalSource);
            events.Add("command");
        }, (_, e) => e.CanExecute = true));
        // Install the target handler before Command evaluates CanExecute.
        foreach (var pair in bindings) button.SetBinding(pair.Key, pair.Value);
        var panel = new StackPanel(); panel.Children.Add(button); panel.Children.Add(target);
        var window = new Window { Content = panel, Width = 400, Height = 200,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Sta.Drain();
            var peer = UIElementAutomationPeer.CreatePeerForElement(button)!;
            var provider = Assert.IsAssignableFrom<IToggleProvider>(peer.GetPattern(PatternInterface.Toggle));
            var cycle = threeState ? new bool?[] { true, null, false } : new bool?[] { true, false };
            foreach (var expected in cycle)
            {
                events.Clear();
                provider.Toggle(); Sta.Drain();
                Assert.Equal(expected, state.Checked);
                Assert.Equal(expected, button.IsChecked);
                Assert.Equal(expected switch { true => ToggleState.On, false => ToggleState.Off, _ => ToggleState.Indeterminate }, provider.ToggleState);
                Assert.Equal(new[] { "state", "click", "command" }, events);
            }
            state.Enabled = false; Sta.Drain(); events.Clear();
            Assert.False(peer.IsEnabled());
            Assert.Throws<ElementNotEnabledException>(() => provider.Toggle());
            Assert.Empty(events);
            state.Enabled = true; Sta.Drain();
            Assert.True(peer.IsEnabled());
            provider.Toggle(); Sta.Drain();
            Assert.Equal(new[] { "state", "click", "command" }, events);
            foreach (var pair in bindings) Assert.Same(pair.Value, BindingOperations.GetBinding(button, pair.Key));
        }
        finally { window.Close(); Sta.Drain(); }
    });

    private sealed class State : INotifyPropertyChanged
    {
        private bool? _checked = false;
        private bool _enabled = true;
        public event PropertyChangedEventHandler? PropertyChanged;
        public bool? Checked { get => _checked; set { _checked = value; PropertyChanged?.Invoke(this, new(nameof(Checked))); } }
        public bool Enabled { get => _enabled; set { _enabled = value; PropertyChanged?.Invoke(this, new(nameof(Enabled))); } }
    }
}
