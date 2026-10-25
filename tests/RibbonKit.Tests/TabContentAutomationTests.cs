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

public sealed class TabContentAutomationTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2024, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.Office2024, FlowDirection.RightToLeft)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.RightToLeft)]
    public void Selected_tab_exposes_commands_without_replacing_consumer_bindings(
        RibbonTheme theme, FlowDirection direction) => Sta.Run(() =>
    {
        var app = Sta.UseApplication();
        ThemeManager.Apply(app, theme);
        var previousMotion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var state = new State();
        var ribbon = new Ribbon { DataContext = state, FlowDirection = direction,
            QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon };
        var home = new RibbonTab(); home.SetBinding(HeaderedContentControl.HeaderProperty, "Title");
        var group = new RibbonGroup(); group.SetBinding(HeaderedItemsControl.HeaderProperty, "Title");
        var bold = new RibbonToggleButton(); bold.SetBinding(RibbonToggleButton.HeaderProperty, "Title");
        var checkedBinding = new Binding("Checked") { Mode = BindingMode.TwoWay };
        bold.SetBinding(ToggleButton.IsCheckedProperty, checkedBinding);
        var enabledBinding = new Binding("Enabled");
        bold.SetBinding(UIElement.IsEnabledProperty, enabledBinding);
        AutomationProperties.SetAutomationId(bold, "source-bold");
        var action = new RibbonButton { Header = "Action" };
        var commandBinding = new Binding("Command");
        action.SetBinding(ButtonBase.CommandProperty, commandBinding);
        AutomationProperties.SetAutomationId(action, "source-action");
        group.Items.Add(bold); group.Items.Add(action); home.Groups.Add(group);
        var other = new RibbonTab { Header = "Other" };
        var otherGroup = new RibbonGroup { Header = "Other group" };
        var otherAction = new RibbonButton { Header = "Other action" };
        AutomationProperties.SetAutomationId(otherAction, "other-action");
        otherGroup.Items.Add(otherAction); other.Groups.Add(otherGroup);
        ribbon.Tabs.Add(home); ribbon.Tabs.Add(other);
        state.Selected = home;
        var selectionBinding = new Binding("Selected") { Mode = BindingMode.TwoWay };
        ribbon.SetBinding(Ribbon.SelectedTabProperty, selectionBinding);
        ribbon.AddToQuickAccess(bold);
        var copy = Assert.IsType<RibbonToggleButton>(Assert.Single(ribbon.QuickAccessItems));
        var window = new RibbonWindow { Content = ribbon, Width = 1000, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
        AutomationPeer TabPeer(RibbonTab tab)
        {
            var host = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
            var peer = UIElementAutomationPeer.CreatePeerForElement(host)!;
            // This in-process fixture has no native selection/structure listener.
            peer.ResetChildrenCache();
            var item = Assert.Single(peer.GetChildren()!, child => child.GetName() == tab.Header.ToString());
            item.ResetChildrenCache();
            return item;
        }
        AutomationPeer Leaf(RibbonTab tab, string id) =>
            Assert.Single(Tree(TabPeer(tab)), peer => peer.GetAutomationId() == id);
        try
        {
            window.Show(); Layout();
            Assert.True(((FrameworkElement)home.Content).ActualWidth > ribbon.ActualWidth * .8);
            var actionPeer = Leaf(home, "source-action");
            var boldPeer = Leaf(home, "source-bold");
            Assert.DoesNotContain(Tree(TabPeer(other)), peer => peer.GetAutomationId() == "other-action");
            Assert.IsAssignableFrom<IInvokeProvider>(actionPeer.GetPattern(PatternInterface.Invoke)).Invoke();
            Layout(); Assert.Equal(1, state.Command.Count);
            state.Command.Allowed = false; Layout(); Assert.False(actionPeer.IsEnabled());
            state.Command.Allowed = true; Layout(); Assert.True(actionPeer.IsEnabled());
            Assert.IsAssignableFrom<IToggleProvider>(boldPeer.GetPattern(PatternInterface.Toggle)).Toggle();
            Layout(); Assert.True(state.Checked); Assert.True(copy.IsChecked);
            state.Enabled = false; Layout(); Assert.False(boldPeer.IsEnabled()); Assert.False(copy.IsEnabled);
            state.Enabled = true; state.Checked = false; state.Title = "Updated"; Layout();
            Assert.Equal("Updated", boldPeer.GetName()); Assert.Equal("Updated", copy.Header);
            Assert.Equal(ToggleState.Off, Assert.IsAssignableFrom<IToggleProvider>(boldPeer.GetPattern(PatternInterface.Toggle)).ToggleState);
            var content = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
            var presenter = Assert.IsType<ContentPresenter>(content.Template.FindName("PART_SelectedContentHost", content));
            Assert.Equal(HorizontalAlignment.Stretch, presenter.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Stretch, presenter.VerticalAlignment);
            Assert.Same(home.Content, presenter.Content);
            Assert.IsAssignableFrom<ISelectionItemProvider>(TabPeer(other).GetPattern(PatternInterface.SelectionItem)).Select();
            Layout(); Assert.Same(other, state.Selected); Assert.Same(other.Content, presenter.Content); Leaf(other, "other-action");
            Assert.DoesNotContain(Tree(TabPeer(home)), peer => peer.GetAutomationId() == "source-bold");
            var alignmentBinding = new Binding("Alignment");
            home.SetBinding(Control.HorizontalContentAlignmentProperty, alignmentBinding);
            state.Selected = home; Layout(); Assert.Same(home.Content, presenter.Content); Leaf(home, "source-bold");
            Assert.Equal(HorizontalAlignment.Right, presenter.HorizontalAlignment);
            Assert.Same(alignmentBinding, BindingOperations.GetBinding(home, Control.HorizontalContentAlignmentProperty));
            Assert.Same(selectionBinding, BindingOperations.GetBinding(ribbon, Ribbon.SelectedTabProperty));
            Assert.Same(checkedBinding, BindingOperations.GetBinding(bold, ToggleButton.IsCheckedProperty));
            Assert.Same(enabledBinding, BindingOperations.GetBinding(bold, UIElement.IsEnabledProperty));
            Assert.Same(commandBinding, BindingOperations.GetBinding(action, ButtonBase.CommandProperty));
        }
        finally { window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = previousMotion; Sta.ResetApplication(); }
    });

    private static IEnumerable<AutomationPeer> Tree(AutomationPeer root)
    {
        yield return root;
        foreach (var child in root.GetChildren() ?? [])
            foreach (var peer in Tree(child)) yield return peer;
    }

    private sealed class State : INotifyPropertyChanged
    {
        private bool _checked, _enabled = true;
        private string _title = "Home";
        private RibbonTab? _selected;
        public event PropertyChangedEventHandler? PropertyChanged;
        public CountingCommand Command { get; } = new();
        public HorizontalAlignment Alignment => HorizontalAlignment.Right;
        public string Title { get => _title; set { _title = value; Changed(); } }
        public bool Checked { get => _checked; set { _checked = value; Changed(); } }
        public bool Enabled { get => _enabled; set { _enabled = value; Changed(); } }
        public RibbonTab? Selected { get => _selected; set { _selected = value; Changed(); } }
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
