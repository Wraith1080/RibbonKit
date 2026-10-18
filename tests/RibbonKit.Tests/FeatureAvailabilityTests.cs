using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using RibbonKit.Controls;
using RibbonKit.Theming;
using RibbonKit.Localization;
using Xunit;

namespace RibbonKit.Tests;

public sealed class FeatureAvailabilityTests
{
    [Fact]
    public void Policies_default_enabled_and_are_not_customization_data() => Sta.Run(() =>
    {
        var ribbon = new Ribbon();
        foreach (string name in Names)
        {
            Assert.True((bool)typeof(Ribbon).GetProperty(name)!.GetValue(ribbon)!);
            typeof(Ribbon).GetProperty(name)!.SetValue(ribbon, false);
        }
        string json = RibbonCustomizationSerializer.Serialize(ribbon);
        foreach (string name in Names) Assert.DoesNotContain(name, json);
        RibbonCustomizationSerializer.Apply(ribbon, json);
        foreach (string name in Names) Assert.False((bool)typeof(Ribbon).GetProperty(name)!.GetValue(ribbon)!);
    });

    [Fact]
    public void Capability_preserves_two_way_binding_latest_request_and_source() => Sta.Run(() =>
    {
        var state = new LayoutState();
        var ribbon = new Ribbon();
        var binding = new Binding(nameof(LayoutState.Mode)) { Source = state, Mode = BindingMode.TwoWay };
        ribbon.SetBinding(Ribbon.LayoutModeProperty, binding);
        Assert.Equal(RibbonLayoutMode.Simplified, ribbon.LayoutMode);
        ribbon.IsSimplifiedRibbonEnabled = false;
        Assert.Equal(RibbonLayoutMode.Classic, ribbon.LayoutMode);
        Assert.Equal(RibbonLayoutMode.Simplified, state.Mode);
        Assert.Equal(0, state.Writes);
        Assert.Same(binding, BindingOperations.GetBinding(ribbon, Ribbon.LayoutModeProperty));
        state.Mode = RibbonLayoutMode.Classic;
        ribbon.IsSimplifiedRibbonEnabled = true;
        Assert.Equal(RibbonLayoutMode.Classic, ribbon.LayoutMode);
        ribbon.IsSimplifiedRibbonEnabled = false;
        state.Mode = RibbonLayoutMode.Simplified;
        ribbon.IsSimplifiedRibbonEnabled = true;
        Assert.Equal(RibbonLayoutMode.Simplified, ribbon.LayoutMode);
        Assert.Equal(2, state.Writes);
    });

    [Fact]
    public void Explicit_child_scopes_and_reparenting_follow_owner_but_nested_ribbons_are_independent() => Sta.Run(() =>
    {
        var disabled = new Ribbon { IsSimplifiedRibbonEnabled = false };
        var enabled = new Ribbon();
        var child = new StackPanel();
        Ribbon.SetLayoutMode(child, RibbonLayoutMode.Simplified);
        var scope = new StackPanel(); scope.Children.Add(child);
        var group = new RibbonGroup(); group.Items.Add(scope);
        var tab = new RibbonTab(); tab.Groups.Add(group); disabled.Tabs.Add(tab);
        ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024);
        disabled.SelectedTab = tab;
        var panel = new StackPanel(); panel.Children.Add(disabled); panel.Children.Add(enabled);
        var window = new Window { Content = panel, Width = 900, Height = 600, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
        window.Show(); Sta.Drain(); window.UpdateLayout(); Sta.Drain();
        Assert.Equal(RibbonLayoutMode.Classic, Ribbon.GetLayoutMode(child));
        var nested = new Ribbon { LayoutMode = RibbonLayoutMode.Simplified };
        scope.Children.Add(nested);
        Assert.Equal(RibbonLayoutMode.Simplified, nested.LayoutMode);
        var enabledGroup = new RibbonGroup(); var enabledTab = new RibbonTab();
        enabledTab.Groups.Add(enabledGroup); enabled.Tabs.Add(enabledTab); enabled.SelectedTab = enabledTab;
        group.Items.Remove(scope); enabledGroup.Items.Add(scope);
        Sta.Drain(); window.UpdateLayout(); Sta.Drain();
        Assert.Equal(RibbonLayoutMode.Simplified, Ribbon.GetLayoutMode(child));
        enabled.IsSimplifiedRibbonEnabled = false;
        Assert.Equal(RibbonLayoutMode.Classic, Ribbon.GetLayoutMode(child));
        }
        finally { window.Close(); Sta.Drain(); Sta.ResetApplication(); }
    });

    [Theory]
    [InlineData("CanUserCustomizeRibbon", RibbonString.CustomizeRibbon)]
    [InlineData("CanUserCustomizeQuickAccess", RibbonString.AddToQuickAccessToolbar)]
    [InlineData("CanUserMinimizeRibbon", RibbonString.CollapseRibbon)]
    [InlineData("CanUserChangeLayoutMode", RibbonString.SimplifiedRibbon)]
    [InlineData("CanUserToggleQuickAccessVisibility", RibbonString.HideQuickAccessToolbar)]
    public void Menu_omits_disallowed_actions_and_stale_handlers_cannot_act(string policy, RibbonString caption) => Sta.Run(() =>
    {
        var ribbon = new Ribbon(); var command = new RibbonButton();
        var oldMenu = ribbon.CreateRibbonContextMenu(command);
        var oldAction = oldMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, RibbonLocalization.GetString(caption)));
        int requests = 0; ribbon.RibbonCustomizeRequested += (_, _) => requests++;
        typeof(Ribbon).GetProperty(policy)!.SetValue(ribbon, false);
        oldAction.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Empty(ribbon.QuickAccessItems); Assert.False(ribbon.IsMinimized);
        Assert.True(ribbon.IsQuickAccessVisible); Assert.Equal(RibbonLayoutMode.Classic, ribbon.LayoutMode); Assert.Equal(0, requests);
        Assert.DoesNotContain(ribbon.CreateRibbonContextMenu(command).Items.OfType<MenuItem>(), item => Equals(item.Header, oldAction.Header));
    });

    [Fact]
    public void Detached_pages_update_eligibility_preserve_host_disable_and_reject_apply() => Sta.Run(() =>
    {
        var ribbon = new Ribbon();
        var classic = new RibbonCustomizePage { Ribbon = ribbon };
        var simplified = new RibbonSimplifiedCustomizePage { Ribbon = ribbon };
        var qat = new RibbonQuickAccessPage { Ribbon = ribbon };
        Assert.True(classic.IsEnabled); Assert.True(qat.IsEnabled);
        ribbon.CanUserCustomizeRibbon = false; ribbon.CanUserCustomizeQuickAccess = false;
        Assert.False(classic.IsEnabled); Assert.False(simplified.IsEnabled); Assert.False(qat.IsEnabled);
        ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        Assert.False(simplified.IsEnabled);
        string before = RibbonCustomizationSerializer.Serialize(ribbon);
        simplified.ApplyChanges(); Assert.Equal(before, RibbonCustomizationSerializer.Serialize(ribbon));
        ribbon.CanUserCustomizeRibbon = true; Assert.True(simplified.IsEnabled);
        ribbon.IsSimplifiedRibbonEnabled = false; Assert.False(simplified.IsEnabled); Assert.True(classic.IsEnabled);
        qat.IsEnabled = false; ribbon.CanUserCustomizeQuickAccess = true; Assert.False(qat.IsEnabled);
    });

    [Fact]
    public void User_import_preserves_locked_QAT_objects_placement_visibility_while_host_restore_can_change_them() => Sta.Run(() =>
    {
        var ribbon = new Ribbon(); var tab = new RibbonTab { Header = "Home" }; var group = new RibbonGroup();
        var source = new RibbonButton { Header = "Save" }; group.Items.Add(source); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        Ribbon.SetCommandId(tab, "home"); Ribbon.SetCommandId(group, "group"); Ribbon.SetCommandId(source, "save");
        string baseline = RibbonCustomizationSerializer.Serialize(ribbon);
        Assert.True(ribbon.AddToQuickAccess(source)); var qat = Assert.Single(ribbon.QuickAccessItems);
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; ribbon.IsQuickAccessVisible = false;
        ribbon.CanUserCustomizeQuickAccess = false; ribbon.CanUserRepositionQuickAccess = false; ribbon.CanUserToggleQuickAccessVisibility = false;
        tab.Header = "Changed";
        RibbonCustomizationSerializer.ApplyUserLayout(ribbon, baseline);
        Assert.Equal("Home", tab.Header); Assert.Same(qat, Assert.Single(ribbon.QuickAccessItems));
        Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, ribbon.QuickAccessPosition); Assert.False(ribbon.IsQuickAccessVisible);
        RibbonCustomizationSerializer.Apply(ribbon, baseline);
        Assert.Empty(ribbon.QuickAccessItems); Assert.Equal(RibbonQuickAccessPosition.TabRow, ribbon.QuickAccessPosition); Assert.True(ribbon.IsQuickAccessVisible);
    });

    [Fact]
    public void Permission_flags_leave_host_state_and_host_command_APIs_available() => Sta.Run(() =>
    {
        var ribbon = new Ribbon();
        foreach (string name in Names.Skip(1)) typeof(Ribbon).GetProperty(name)!.SetValue(ribbon, false);
        ribbon.LayoutMode = RibbonLayoutMode.Simplified; ribbon.Density = RibbonDensity.Touch;
        ribbon.IsMinimized = true; ribbon.IsQuickAccessVisible = false; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        Assert.Equal(RibbonLayoutMode.Simplified, ribbon.LayoutMode); Assert.Equal(RibbonDensity.Touch, ribbon.Density);
        Assert.True(ribbon.IsMinimized); Assert.False(ribbon.IsQuickAccessVisible); Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, ribbon.QuickAccessPosition);
        Assert.True(ribbon.AddToQuickAccess(new RibbonButton { Header = "Host command" }));
    });

    [Fact]
    public void Simplified_draft_is_inert_while_disabled_and_can_be_applied_after_reenable() => Sta.Run(() =>
    {
        var ribbon = new Ribbon { LayoutMode = RibbonLayoutMode.Simplified };
        var tab = new RibbonTab { Header = "Home" }; Ribbon.SetCommandId(tab, "home"); ribbon.Tabs.Add(tab);
        var page = new RibbonSimplifiedCustomizePage { Ribbon = ribbon };
        var imported = RibbonCustomizationSerializer.CaptureLayout(ribbon); imported.Tabs[0].Header = "Prepared";
        Assert.True(page.ImportDraft(RibbonCustomizationSerializer.WriteLayout(imported)));
        ribbon.CanUserCustomizeRibbon = false; page.ApplyChanges(); Assert.Equal("Home", tab.Header);
        ribbon.CanUserCustomizeRibbon = true; ribbon.IsSimplifiedRibbonEnabled = false; page.ApplyChanges(); Assert.Equal("Home", tab.Header);
        ribbon.IsSimplifiedRibbonEnabled = true; page.ApplyChanges(); Assert.Equal("Prepared", tab.Header);
    });

    [Theory]
    [InlineData("CanUserCustomizeQuickAccess")]
    [InlineData("CanUserRepositionQuickAccess")]
    [InlineData("CanUserToggleQuickAccessVisibility")]
    public void User_import_respects_each_QAT_permission_independently(string locked) => Sta.Run(() =>
    {
        var ribbon = new Ribbon(); var command = new RibbonButton { Header = "Save" }; Ribbon.SetCommandId(command, "save");
        ribbon.QuickAccessItems.Add(command);
        var imported = RibbonCustomizationSerializer.CaptureLayout(ribbon); imported.QuickAccess.Clear();
        imported.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; imported.IsQuickAccessVisible = false;
        typeof(Ribbon).GetProperty(locked)!.SetValue(ribbon, false);
        RibbonCustomizationSerializer.ApplyUserLayout(ribbon, RibbonCustomizationSerializer.WriteLayout(imported));
        Assert.Equal(locked == "CanUserCustomizeQuickAccess" ? 1 : 0, ribbon.QuickAccessItems.Count);
        Assert.Equal(locked == "CanUserRepositionQuickAccess" ? RibbonQuickAccessPosition.TabRow : RibbonQuickAccessPosition.BelowRibbon, ribbon.QuickAccessPosition);
        Assert.Equal(locked == "CanUserToggleQuickAccessVisibility", ribbon.IsQuickAccessVisible);
    });

    [Fact]
    public void Cached_QAT_menu_rechecks_permissions_and_host_configuration_remains_available() => Sta.Run(() =>
    {
        var ribbon = new Ribbon();
        var menu = (ContextMenu)typeof(Ribbon).GetMethod("EnsureQatContextMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(ribbon, null)!;
        var below = menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, RibbonLocalization.GetString(RibbonString.ShowQuickAccessToolbarBelowRibbon)));
        ribbon.CanUserRepositionQuickAccess = false;
        below.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); Assert.Equal(RibbonQuickAccessPosition.TabRow, ribbon.QuickAccessPosition);
        typeof(Ribbon).GetMethod("OnQatContextMenuOpened", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(ribbon, new object[] { menu, new RoutedEventArgs() });
        Assert.Equal(Visibility.Collapsed, below.Visibility);
        ribbon.CanUserRepositionQuickAccess = true;
        typeof(Ribbon).GetMethod("OnQatContextMenuOpened", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(ribbon, new object[] { menu, new RoutedEventArgs() });
        Assert.Equal(Visibility.Visible, below.Visibility);
        below.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, ribbon.QuickAccessPosition);
    });

    [Fact]
    public void User_add_cannot_reveal_a_hidden_QAT_when_visibility_is_locked_but_host_add_can() => Sta.Run(() =>
    {
        var ribbon = new Ribbon { IsQuickAccessVisible = false, CanUserToggleQuickAccessVisibility = false };
        var userCommand = new RibbonButton { Header = "User command" };
        var add = ribbon.CreateRibbonContextMenu(userCommand).Items.OfType<MenuItem>()
            .Single(item => Equals(item.Header, RibbonLocalization.GetString(RibbonString.AddToQuickAccessToolbar)));
        add.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Single(ribbon.QuickAccessItems); Assert.False(ribbon.IsQuickAccessVisible);
        Assert.True(ribbon.AddToQuickAccess(new RibbonButton { Header = "Host command" }));
        Assert.True(ribbon.IsQuickAccessVisible);
    });

    private static readonly string[] Names = { "IsSimplifiedRibbonEnabled", "CanUserCustomizeRibbon", "CanUserCustomizeQuickAccess",
        "CanUserRepositionQuickAccess", "CanUserToggleQuickAccessVisibility", "CanUserMinimizeRibbon", "CanUserChangeLayoutMode", "CanUserChangeDensity" };

    private sealed class LayoutState : INotifyPropertyChanged
    {
        private RibbonLayoutMode _mode = RibbonLayoutMode.Simplified;
        public int Writes { get; private set; }
        public RibbonLayoutMode Mode { get => _mode; set { _mode = value; Writes++; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Mode))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
