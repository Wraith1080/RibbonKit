using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public sealed class ToggleCommandProxyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Copied_toggle_executes_the_source_command_once(bool customGroup) => Sta.Run(() =>
    {
        int executions = 0, clicks = 0;
        var parameter = new object();
        var source = new RibbonToggleButton { Header = "Bold", CommandParameter = parameter,
            Command = new CallbackCommand(value => { Assert.Same(parameter, value); executions++; }) };
        source.Click += (_, _) => clicks++;
        var copy = Copy(source, customGroup);

        source.InvokeFromKeyTip();
        source.InvokeFromKeyTip();
        Assert.Equal(2, executions);
        Assert.False(source.IsChecked);

        copy.InvokeFromKeyTip();
        Assert.Equal(3, clicks);
        Assert.True(source.IsChecked);
        Assert.Equal(3, executions);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Copied_toggle_keeps_event_based_actions_and_checked_state(bool customGroup) => Sta.Run(() =>
    {
        int checks = 0, unchecks = 0, clicks = 0;
        var source = new RibbonToggleButton { Header = "Dark mode" };
        source.Checked += (_, _) => checks++;
        source.Unchecked += (_, _) => unchecks++;
        source.Click += (_, _) => clicks++;
        var copy = Copy(source, customGroup);

        copy.InvokeFromKeyTip();
        Assert.True(source.IsChecked);
        Assert.True(copy.IsChecked);
        copy.InvokeFromKeyTip();
        Assert.False(source.IsChecked);
        Assert.False(copy.IsChecked);
        Assert.Equal(1, checks);
        Assert.Equal(1, unchecks);
        Assert.Equal(2, clicks);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Copied_toggle_uses_live_routed_command_target_and_parameter_after_click(bool customGroup) => Sta.Run(() =>
    {
        int first = 0, replacement = 0;
        var target = new Border();
        var parameter = new object();
        var nextParameter = new object();
        var command = new RoutedUICommand("First", "First", typeof(ToggleCommandProxyTests));
        var next = new RoutedUICommand("Next", "Next", typeof(ToggleCommandProxyTests));
        target.CommandBindings.Add(new CommandBinding(command, (_, e) =>
        {
            Assert.Same(target, e.OriginalSource); Assert.Same(parameter, e.Parameter); first++;
        }, (_, e) => e.CanExecute = true));
        var source = new RibbonToggleButton { Header = "Action", CommandTarget = target,
            CommandParameter = parameter, Command = command };
        source.CommandBindings.Add(new CommandBinding(next, (_, e) =>
        {
            Assert.Same(source, e.OriginalSource); Assert.Same(nextParameter, e.Parameter); replacement++;
        }, (_, e) => e.CanExecute = true));
        var copy = Copy(source, customGroup);
        copy.InvokeFromKeyTip();
        Assert.Equal(1, first);
        source.Click += (_, _) =>
        {
            source.CommandTarget = null; source.CommandParameter = nextParameter; source.Command = next;
        };
        copy.InvokeFromKeyTip();
        Assert.Equal(1, first);
        Assert.Equal(1, replacement);
        Assert.False(source.IsChecked);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Copied_toggle_honors_can_execute_after_source_click(bool customGroup) => Sta.Run(() =>
    {
        int executions = 0;
        bool allowed = true;
        var source = new RibbonToggleButton { Header = "Action",
            Command = new CallbackCommand(_ => executions++, _ => allowed) };
        source.Click += (_, _) => allowed = false;
        Copy(source, customGroup).InvokeFromKeyTip();
        Assert.Equal(0, executions);
    });

    private static RibbonToggleButton Copy(RibbonToggleButton source, bool customGroup)
    {
        var ribbon = new Ribbon();
        if (customGroup)
        {
            var group = new RibbonGroup { Header = "Favorites" };
            Ribbon.SetIsCustom(group, true);
            var copy = ribbon.CreateCommandProxy(source, RibbonControlSize.Medium);
            group.Items.Add(copy);
            return Assert.IsType<RibbonToggleButton>(copy);
        }
        Assert.True(ribbon.AddToQuickAccess(source));
        return Assert.IsType<RibbonToggleButton>(Assert.Single(ribbon.QuickAccessItems));
    }

    private sealed class CallbackCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
    {
        public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
