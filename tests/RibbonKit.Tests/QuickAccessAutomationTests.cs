using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class QuickAccessAutomationTests
{
    [Theory]
    [InlineData(RibbonQuickAccessPosition.TitleBar, FlowDirection.LeftToRight)]
    [InlineData(RibbonQuickAccessPosition.TitleBar, FlowDirection.RightToLeft)]
    [InlineData(RibbonQuickAccessPosition.TabRow, FlowDirection.LeftToRight)]
    [InlineData(RibbonQuickAccessPosition.TabRow, FlowDirection.RightToLeft)]
    [InlineData(RibbonQuickAccessPosition.BelowRibbon, FlowDirection.LeftToRight)]
    [InlineData(RibbonQuickAccessPosition.BelowRibbon, FlowDirection.RightToLeft)]
    public void QAT_item_peers_keep_control_actions_bindings_and_collection_refresh(
        RibbonQuickAccessPosition position, FlowDirection flow) => Sta.Run(() =>
    {
        using var host = new Host(position, flow);
        if (position == RibbonQuickAccessPosition.TabRow)
        {
            var tabs = (RibbonTabControl)host.Ribbon.Template.FindName("TabControlHost", host.Ribbon);
            Assert.Contains(UIElementAutomationPeer.CreatePeerForElement(tabs)!.GetChildren()!,
                peer => peer.GetAutomationId() == "QatTabRowHost");
        }
        var copyBinding = BindingOperations.GetBinding(host.ToggleCopy, ToggleButton.IsCheckedProperty);
        var toggle = Item(host.ToolBar, "copy-bold");
        Assert.Equal(AutomationControlType.Button, toggle.GetAutomationControlType());
        Assert.Equal("Bold", toggle.GetName());
        var provider = Assert.IsAssignableFrom<IToggleProvider>(toggle.GetPattern(PatternInterface.Toggle));
        provider.Toggle(); host.Layout();
        Assert.True(host.State.Checked); Assert.True(host.ToggleCopy.IsChecked);
        Assert.Equal(1, host.ToggleClicks); Assert.Equal(1, host.State.Command.Count);
        provider.Toggle(); host.Layout();
        Assert.False(host.State.Checked); Assert.Equal(2, host.ToggleClicks); Assert.Equal(2, host.State.Command.Count);
        Assert.Equal(ToggleState.Off, provider.ToggleState);
        host.State.Title = "Renamed"; host.Layout(); Assert.Equal("Renamed", toggle.GetName());
        host.State.Command.Allowed = false; host.Layout();
        Assert.False(toggle.IsEnabled()); Assert.Throws<ElementNotEnabledException>(() => provider.Toggle());
        Assert.Equal(2, host.State.Command.Count);
        host.State.Command.Allowed = true; host.Layout();
        Assert.True(toggle.IsEnabled());
        var action = Item(host.ToolBar, "copy-action");
        Assert.IsAssignableFrom<IInvokeProvider>(action.GetPattern(PatternInterface.Invoke)).Invoke();
        host.Layout(); Assert.Equal(1, host.ActionClicks);
        Assert.Equal(3, host.State.Command.Count);
        Assert.Same(copyBinding, BindingOperations.GetBinding(host.ToggleCopy, ToggleButton.IsCheckedProperty));
        Assert.Same(host.CheckedBinding, BindingOperations.GetBinding(host.Toggle, ToggleButton.IsCheckedProperty));
        Assert.Same(host.CommandBinding, BindingOperations.GetBinding(host.Toggle, ButtonBase.CommandProperty));
        host.Ribbon.QuickAccessItems.Remove(host.ToggleCopy); host.Layout();
        var root = UIElementAutomationPeer.CreatePeerForElement(host.ToolBar)!;
        root.ResetChildrenCache(); // Direct in-process queries have no native structure-change listener.
        Assert.DoesNotContain(root.GetChildren()!,
            peer => peer.GetAutomationId() == "copy-bold");
        host.Ribbon.QuickAccessItems.Add(host.ToggleCopy); host.Layout();
        Assert.NotNull(Item(host.ToolBar, "copy-bold").GetPattern(PatternInterface.Toggle));
    });

    [Theory]
    [InlineData(false, FlowDirection.LeftToRight)]
    [InlineData(false, FlowDirection.RightToLeft)]
    [InlineData(true, FlowDirection.LeftToRight)]
    [InlineData(true, FlowDirection.RightToLeft)]
    public void Overflow_and_simplified_item_peers_expose_actions_and_preserve_popup_rules(
        bool simplified, FlowDirection flow) => Sta.Run(() =>
    {
        using var host = new Host(RibbonQuickAccessPosition.TabRow, flow);
        if (simplified) host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        host.Layout();
        var toolbar = simplified
            ? (RibbonQuickAccessToolBar)((RibbonGroupsHost)host.Tab.Content).Template.FindName(
                "PART_SimplifiedToolBar", (RibbonGroupsHost)host.Tab.Content)
            : host.ToolBar;
        toolbar.Width = 30; host.Layout(); Assert.True(toolbar.HasOverflow);
        toolbar.OpenOverflow(); host.Layout();
        var items = (ItemsControl)toolbar.Template.FindName("PART_OverflowHost", toolbar);
        var bold = items.Items.OfType<RibbonToggleButton>().Single();
        var action = items.Items.OfType<RibbonButton>().Single();
        AutomationProperties.SetAutomationId(bold, "overflow-bold");
        AutomationProperties.SetAutomationId(action, "overflow-action");
        var toggle = Assert.IsAssignableFrom<IToggleProvider>(Item(items, "overflow-bold").GetPattern(PatternInterface.Toggle));
        toggle.Toggle(); host.Layout();
        Assert.True(host.State.Checked); Assert.Equal(1, host.ToggleClicks); Assert.Equal(1, host.State.Command.Count);
        var popup = (Popup)toolbar.Template.FindName("PART_OverflowPopup", toolbar);
        Assert.False(popup.IsOpen); // Overflow is a command menu; toggles close it too.
        toolbar.OpenOverflow(); host.Layout();
        Assert.IsAssignableFrom<IInvokeProvider>(Item(items, "overflow-action").GetPattern(PatternInterface.Invoke)).Invoke();
        host.Layout(); Assert.Equal(1, host.ActionClicks); Assert.Equal(2, host.State.Command.Count);
        Assert.False(popup.IsOpen);
    });

    [Fact]
    public void Hand_authored_native_toggle_keeps_three_state_cycle_and_one_command_per_action() => Sta.Run(() =>
    {
        using var host = new Host(RibbonQuickAccessPosition.BelowRibbon, FlowDirection.LeftToRight);
        var native = new ToggleButton { IsThreeState = true, Command = host.State.Command };
        AutomationProperties.SetAutomationId(native, "native-toggle");
        var nativeBinding = new Binding("Checked") { Source = host.State, Mode = BindingMode.TwoWay };
        native.SetBinding(ToggleButton.IsCheckedProperty, nativeBinding);
        host.Ribbon.QuickAccessItems.Add(native); host.Layout();
        var provider = Assert.IsAssignableFrom<IToggleProvider>(Item(host.ToolBar, "native-toggle").GetPattern(PatternInterface.Toggle));
        foreach (var expected in new[] { ToggleState.On, ToggleState.Indeterminate, ToggleState.Off })
        { provider.Toggle(); host.Layout(); Assert.Equal(expected, provider.ToggleState); }
        Assert.Equal(3, host.State.Command.Count);
        Assert.Same(nativeBinding, BindingOperations.GetBinding(native, ToggleButton.IsCheckedProperty));
    });

    private static AutomationPeer Item(ItemsControl host, string id)
    {
        var peer = UIElementAutomationPeer.CreatePeerForElement(host)!;
        peer.ResetChildrenCache();
        return Assert.Single(peer.GetChildren()!, child => child.GetAutomationId() == id);
    }

    private sealed class Host : IDisposable
    {
        internal readonly State State = new();
        internal readonly Ribbon Ribbon;
        internal readonly RibbonTab Tab;
        internal readonly RibbonToggleButton Toggle = new();
        internal readonly RibbonToggleButton ToggleCopy;
        internal readonly RibbonWindow Window;
        internal readonly Binding CheckedBinding = new("Checked") { Mode = BindingMode.TwoWay };
        internal readonly Binding CommandBinding = new("Command");
        internal int ToggleClicks, ActionClicks;
        private readonly RibbonAnimationLevel _motion = RibbonAnimation.GlobalLevel;
        internal RibbonQuickAccessToolBar ToolBar => Ribbon.ActiveQuickAccessToolBar!;
        internal Host(RibbonQuickAccessPosition position, FlowDirection flow)
        {
            var app = Sta.UseApplication(); ThemeManager.Apply(app, RibbonTheme.Office2024);
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            Ribbon = new Ribbon { DataContext = State, QuickAccessPosition = position, QuickAccessMaxWidth = 400,
                FlowDirection = flow };
            Tab = new RibbonTab { Header = "Home" }; var group = new RibbonGroup { Header = "Font" };
            Toggle.SetBinding(RibbonToggleButton.HeaderProperty, "Title");
            Toggle.SetBinding(ToggleButton.IsCheckedProperty, CheckedBinding);
            Toggle.SetBinding(ButtonBase.CommandProperty, CommandBinding);
            Toggle.Click += (_, _) => ToggleClicks++;
            var action = new RibbonButton { Header = "Action" };
            action.SetBinding(ButtonBase.CommandProperty, "Command"); action.Click += (_, _) => ActionClicks++;
            foreach (var control in new FrameworkElement[] { Toggle, action })
            { group.Items.Add(control); Ribbon.SetSimplifiedPlacement(control, RibbonSimplifiedPlacement.Inline); }
            Tab.Groups.Add(group); Ribbon.Tabs.Add(Tab); Ribbon.SelectedTab = Tab;
            Ribbon.AddToQuickAccess(Toggle); Ribbon.AddToQuickAccess(action);
            ToggleCopy = (RibbonToggleButton)Ribbon.QuickAccessItems[0];
            AutomationProperties.SetAutomationId(ToggleCopy, "copy-bold");
            AutomationProperties.SetAutomationId((FrameworkElement)Ribbon.QuickAccessItems[1], "copy-action");
            Window = new RibbonWindow { Content = Ribbon, Width = 1100, Height = 400,
                FlowDirection = flow, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            Window.Show(); Layout();
        }
        internal void Layout() { Sta.Drain(); Window.UpdateLayout(); Sta.Drain(); }
        public void Dispose() { Window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = _motion; Sta.ResetApplication(); }
    }

    private sealed class State : INotifyPropertyChanged
    {
        private bool? _checked = false;
        private string _title = "Bold";
        public event PropertyChangedEventHandler? PropertyChanged;
        public CountingCommand Command { get; } = new();
        public bool? Checked { get => _checked; set { _checked = value; Changed(); } }
        public string Title { get => _title; set { _title = value; Changed(); } }
        private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    }

    private sealed class CountingCommand : ICommand
    {
        private bool _allowed = true;
        public int Count { get; private set; }
        public bool Allowed { get => _allowed; set { _allowed = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); } }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => Allowed;
        public void Execute(object? parameter) => Count++;
    }
}
