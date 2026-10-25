using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class SimplifiedAutomationTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2024, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.Office2024, FlowDirection.RightToLeft)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.RightToLeft)]
    public void Selected_tab_exposes_visible_simplified_commands_and_keeps_classic_round_trip(
        RibbonTheme theme, FlowDirection direction) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), theme);
        var previousMotion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var ribbon = new Ribbon { LayoutMode = RibbonLayoutMode.Simplified, FlowDirection = direction };
        var tab = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Font" };
        var bold = new RibbonToggleButton { Header = "Bold" };
        var state = new ToggleButton { IsChecked = false };
        var sourceBinding = new Binding(nameof(ToggleButton.IsChecked)) { Source = state, Mode = BindingMode.TwoWay };
        bold.SetBinding(ToggleButton.IsCheckedProperty, sourceBinding);
        AutomationProperties.SetAutomationId(bold, "source-bold");
        var action = new RibbonButton { Header = "Save" };
        AutomationProperties.SetAutomationId(action, "source-action");
        int toggleClicks = 0, actionClicks = 0;
        bold.Click += (_, _) => toggleClicks++; action.Click += (_, _) => actionClicks++;
        Ribbon.SetSimplifiedPlacement(bold, RibbonSimplifiedPlacement.Inline);
        Ribbon.SetSimplifiedPlacement(action, RibbonSimplifiedPlacement.Inline);
        group.Items.Add(bold); group.Items.Add(action); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var other = new RibbonTab { Header = "Other" };
        var otherGroup = new RibbonGroup { Header = "Other commands" };
        var otherAction = new RibbonButton { Header = "Other action" };
        Ribbon.SetSimplifiedPlacement(otherAction, RibbonSimplifiedPlacement.Inline);
        otherGroup.Items.Add(otherAction); other.Groups.Add(otherGroup); ribbon.Tabs.Add(other);
        ribbon.SelectedTab = tab;
        var window = new RibbonWindow { Content = ribbon, Width = 1100, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout();
            var groups = (RibbonGroupsHost)tab.Content;
            var toolbar = groups.SimplifiedToolBar!;
            var copy = toolbar.Items.OfType<RibbonToggleButton>().Single();
            var actionCopy = toolbar.Items.OfType<RibbonButton>().Single();
            AutomationProperties.SetAutomationId(copy, "inline-bold");
            AutomationProperties.SetAutomationId(actionCopy, "inline-action");
            var copyBinding = BindingOperations.GetBinding(copy, ToggleButton.IsCheckedProperty);
            var tabPeer = TabPeer("Home");
            var row = Assert.Single(Tree(tabPeer), peer => peer.GetAutomationControlType() == AutomationControlType.ToolBar);
            Assert.Single(Tree(row), peer => peer.GetAutomationId() == "inline-bold");
            Assert.DoesNotContain(Tree(tabPeer), peer => peer.GetAutomationId() == "source-bold");
            var toggle = Assert.Single(Tree(tabPeer), peer => peer.GetAutomationId() == "inline-bold");
            Assert.Equal("Bold", toggle.GetName());
            Assert.Equal(AutomationControlType.Button, toggle.GetAutomationControlType());
            var provider = Assert.IsAssignableFrom<IToggleProvider>(toggle.GetPattern(PatternInterface.Toggle));
            provider.Toggle(); Layout(); Assert.True(state.IsChecked); Assert.Equal(1, toggleClicks);
            provider.Toggle(); Layout(); Assert.False(state.IsChecked); Assert.Equal(2, toggleClicks);
            bold.IsEnabled = false; Layout(); Assert.False(toggle.IsEnabled());
            Assert.Throws<ElementNotEnabledException>(() => provider.Toggle());
            Assert.Equal(2, toggleClicks); bold.IsEnabled = true;
            bold.Header = "Renamed Bold"; Layout(); Assert.Equal("Renamed Bold", toggle.GetName());
            Assert.IsAssignableFrom<IInvokeProvider>(Assert.Single(Tree(tabPeer), peer =>
                peer.GetAutomationId() == "inline-action").GetPattern(PatternInterface.Invoke)).Invoke();
            Layout(); Assert.Equal(1, actionClicks);
            Assert.Same(sourceBinding, BindingOperations.GetBinding(bold, ToggleButton.IsCheckedProperty));
            Assert.Same(copyBinding, BindingOperations.GetBinding(copy, ToggleButton.IsCheckedProperty));
            ribbon.SelectedTab = other; Layout();
            Assert.DoesNotContain(Tree(TabPeer("Other")), peer => peer.GetAutomationId() == "inline-bold");
            ribbon.SelectedTab = tab; ribbon.LayoutMode = RibbonLayoutMode.Classic; Layout();
            Assert.Single(Tree(TabPeer("Home")), peer => peer.GetAutomationId() == "source-bold");
            Assert.DoesNotContain(Tree(TabPeer("Home")), peer => peer.GetAutomationId() == "inline-bold");
            ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout();
            Assert.Single(Tree(TabPeer("Home")), peer => peer.GetName() == "Renamed Bold"
                && peer.GetAutomationControlType() == AutomationControlType.Button);
            Assert.Same(sourceBinding, BindingOperations.GetBinding(bold, ToggleButton.IsCheckedProperty));
        }
        finally { window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = previousMotion; Sta.ResetApplication(); }

        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
        AutomationPeer TabPeer(string name)
        {
            var tabs = (RibbonTabControl)ribbon.Template.FindName("TabControlHost", ribbon);
            var peer = UIElementAutomationPeer.CreatePeerForElement(tabs)!;
            peer.ResetChildrenCache(); // Direct queries have no native structure listener.
            var item = Assert.Single(peer.GetChildren()!, child => child.GetName() == name);
            item.ResetChildrenCache();
            var host = (RibbonGroupsHost)((RibbonTab)ribbon.SelectedTab!).Content;
            UIElementAutomationPeer.FromElement(host)?.ResetChildrenCache();
            return item;
        }
    });

    private static IEnumerable<AutomationPeer> Tree(AutomationPeer peer)
    {
        yield return peer;
        foreach (var child in peer.GetChildren() ?? [])
            foreach (var descendant in Tree(child)) yield return descendant;
    }
}
