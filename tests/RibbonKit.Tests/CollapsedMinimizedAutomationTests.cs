using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Localization;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class CollapsedMinimizedAutomationTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2024, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.Office2024, FlowDirection.RightToLeft)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.RightToLeft)]
    public void Parent_tree_names_collapsed_opener_and_exposes_minimize_control(
        RibbonTheme theme, FlowDirection flow) => Sta.Run(() =>
    {
        using var host = new Host(theme, flow);
        Assert.Equal(RibbonGroupSizeState.Collapsed, host.Group.SizeState);
        var opener = Assert.Single(host.Tree(), peer => peer.GetAutomationId() == "group-opener");
        Assert.Equal("Font", opener.GetName());
        Assert.IsAssignableFrom<IToggleProvider>(opener.GetPattern(PatternInterface.Toggle));
        host.Group.Header = "Renamed group"; host.Layout(); Assert.Equal("Renamed group", opener.GetName());
        AutomationProperties.SetName(host.Opener, "Authored opener"); Assert.Equal("Authored opener", opener.GetName());
        var minimize = Assert.Single(host.Tree(), peer => peer.GetAutomationId() == "minimize-toggle");
        Assert.Equal(AutomationControlType.Button, minimize.GetAutomationControlType());
        Assert.False(string.IsNullOrWhiteSpace(minimize.GetName()));
        Assert.Equal(host.Minimize.ToolTip, minimize.GetName());
        AutomationProperties.SetName(host.Minimize, "Authored minimize"); Assert.Equal("Authored minimize", minimize.GetName());
    });

    [Theory]
    [InlineData(RibbonTheme.Office2024, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.RightToLeft)]
    public void Minimize_toggle_runs_click_path_once_and_retains_host_and_part_bindings(
        RibbonTheme theme, FlowDirection flow) => Sta.Run(() =>
    {
        using var host = new Host(theme, flow);
        var state = new ToggleButton { IsChecked = false };
        var binding = new Binding(nameof(ToggleButton.IsChecked)) { Source = state, Mode = BindingMode.TwoWay };
        host.Ribbon.SetBinding(Ribbon.IsMinimizedProperty, binding);
        var partBinding = BindingOperations.GetBindingBase(host.Minimize, ToggleButton.IsCheckedProperty);
        int clicks = 0;
        host.Minimize.Click += (_, _) => clicks++;
        var peer = UIElementAutomationPeer.CreatePeerForElement(host.Minimize)!;
        var provider = Assert.IsAssignableFrom<IToggleProvider>(peer.GetPattern(PatternInterface.Toggle));
        provider.Toggle(); host.Layout();
        Assert.True(host.Ribbon.IsMinimized); Assert.True(state.IsChecked); Assert.Equal(1, clicks);
        Assert.Equal(ToggleState.On, provider.ToggleState);
        provider.Toggle(); host.Layout();
        Assert.False(host.Ribbon.IsMinimized); Assert.False(state.IsChecked); Assert.Equal(2, clicks);
        Assert.Equal(ToggleState.Off, provider.ToggleState);
        host.Ribbon.CanUserMinimizeRibbon = false; host.Layout(); Assert.False(peer.IsEnabled());
        Assert.Throws<ElementNotEnabledException>(() => provider.Toggle()); Assert.Equal(2, clicks);
        Assert.False(host.Ribbon.IsMinimized);
        host.Ribbon.CanUserMinimizeRibbon = true; host.Layout();
        host.Minimize.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, host.Minimize)); host.Layout();
        Assert.True(host.Ribbon.IsMinimized); Assert.True(host.Minimize.IsChecked); Assert.Equal(3, clicks);
        state.IsChecked = false; host.Layout(); Assert.False(host.Ribbon.IsMinimized); Assert.False(host.Minimize.IsChecked);
        Assert.Same(binding, BindingOperations.GetBinding(host.Ribbon, Ribbon.IsMinimizedProperty));
        Assert.Same(partBinding, BindingOperations.GetBindingBase(host.Minimize, ToggleButton.IsCheckedProperty));
    });

    private sealed class Host : IDisposable
    {
        private readonly RibbonAnimationLevel _motion = RibbonAnimation.GlobalLevel;
        private readonly Window _window;
        internal readonly Ribbon Ribbon = new();
        internal readonly RibbonGroup Group = new() { Header = "Font" };
        internal readonly ToggleButton Opener;
        internal readonly ToggleButton Minimize;

        internal Host(RibbonTheme theme, FlowDirection flow)
        {
            ThemeManager.Apply(Sta.UseApplication(), theme); RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            Ribbon.FlowDirection = flow;
            var tab = new RibbonTab { Header = "Home" };
            Group.Items.Add(new StackPanel { MinWidth = 650, Children = { new RibbonButton { Header = "Save" } } });
            tab.Groups.Add(Group); Ribbon.Tabs.Add(tab); Ribbon.SelectedTab = tab;
            _window = new RibbonWindow { Content = Ribbon, Width = 420, Height = 400,
                Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            _window.Show(); Layout();
            Opener = (ToggleButton)Group.Template.FindName("PART_CollapsedButton", Group);
            var tabs = (RibbonTabControl)Ribbon.Template.FindName("TabControlHost", Ribbon);
            Minimize = (ToggleButton)tabs.Template.FindName("MinimizeToggle", tabs);
            AutomationProperties.SetAutomationId(Opener, "group-opener"); AutomationProperties.SetAutomationId(Minimize, "minimize-toggle");
        }
        internal void Layout() { Sta.Drain(); _window.UpdateLayout(); Sta.Drain(); }
        internal IEnumerable<AutomationPeer> Tree() => Descendants(UIElementAutomationPeer.CreatePeerForElement(_window)!);
        private static IEnumerable<AutomationPeer> Descendants(AutomationPeer root)
        {
            yield return root;
            foreach (var child in root.GetChildren() ?? []) foreach (var peer in Descendants(child)) yield return peer;
        }
        public void Dispose() { _window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = _motion; Sta.ResetApplication(); }
    }
}
