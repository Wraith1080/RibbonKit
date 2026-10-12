using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Layout;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class GroupQuickAccessTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Press_inside_native_group_flyout_keeps_it_open_until_the_command_click(bool inactive) => Sta.Run(() =>
    {
        using var host = new GroupHost(inactive);
        host.Open();
        Press(host.Command, Mouse.PreviewMouseDownEvent); host.Layout();
        Assert.True(host.Popup.IsOpen);
        Assert.True(host.Flyout.IsAncestorOf(host.Command));
        int invoked = 0; host.Command.Click += (_, _) => invoked++;
        host.Command.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); host.Layout();
        Assert.Equal(1, invoked);
        Assert.False(host.Popup.IsOpen);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Native_content_press_in_overflow_keeps_the_outer_and_group_popups_open(bool inactive) => Sta.Run(() =>
    {
        using var host = new GroupHost(inactive);
        host.Ribbon.QuickAccessItems.Clear();
        host.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
        host.Ribbon.QuickAccessMaxWidth = 65;
        for (int i = 0; i < 3; i++) host.Ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Icon = new DrawingImage(), Size = RibbonControlSize.Small });
        host.Ribbon.AddToQuickAccess(host.Group); host.Layout();
        var tabs = (RibbonTabControl)host.Ribbon.Template.FindName("TabControlHost", host.Ribbon);
        var toolbar = (RibbonQuickAccessToolBar)tabs.Template.FindName("QatTabRowHost", tabs);
        Assert.True(toolbar.HasOverflow);
        ((ToggleButton)toolbar.Template.FindName("PART_OverflowButton", toolbar)).IsChecked = true; host.Layout();
        var proxy = ((ItemsControl)toolbar.Template.FindName("PART_OverflowHost", toolbar)).Items.OfType<RibbonDropDownButton>().Single();
        proxy.IsDropDownOpen = true; host.Layout();
        Assert.False(((Popup)proxy.Template.FindName("PART_Popup", proxy)).IsOpen);
        Press(host.Command, Mouse.PreviewMouseDownEvent); host.Layout();
        Assert.True(host.Popup.IsOpen);
        Assert.True(((Popup)toolbar.Template.FindName("PART_OverflowPopup", toolbar)).IsOpen);
        Assert.True(host.Flyout.IsAncestorOf(host.Command));
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Toggle_and_option_clicks_keep_the_group_flyout_open(bool qat) => Sta.Run(() =>
    {
        using var host = new GroupHost();
        var toggle = new RibbonToggleButton { Header = "Bold" };
        var check = new RibbonCheckBox { Header = "Show grid" };
        var radio = new RibbonRadioButton { Header = "Layout" };
        host.Group.Items.Add(new StackPanel { Children = { toggle, check, radio } }); host.Layout();
        if (qat) host.Open();
        else { host.Group.CollapsedButton!.IsChecked = true; host.Layout(); }
        foreach (ToggleButton option in new ToggleButton[] { toggle, check, radio })
        {
            Press(option, Mouse.PreviewMouseDownEvent); host.Layout();
            Assert.True(host.Popup.IsOpen);
            option.SetCurrentValue(ToggleButton.IsCheckedProperty, true);
            option.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); host.Layout();
            Assert.True(option.IsChecked);
            Assert.True(host.Popup.IsOpen);
        }
        Press(host.Outside, Mouse.PreviewMouseDownEvent); host.Layout();
        Assert.False(host.Popup.IsOpen);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Group_reuses_its_native_flyout_and_controls_without_selecting_an_inactive_tab(bool inactive) => Sta.Run(() =>
    {
        using var host = new GroupHost(inactive);
        var selected = host.Ribbon.SelectedTab;
        var sourceParent = host.Group.Parent;
        var sourceBinding = BindingOperations.GetBindingExpression(host.Combo, Selector.SelectedIndexProperty);
        host.Open();
        Assert.True(host.Popup.IsOpen);
        var unusedPopup = (Popup)host.Proxy.Template.FindName("PART_Popup", host.Proxy);
        Assert.False(unusedPopup.IsOpen,
            $"The unused QAT popup opened with content size {unusedPopup.Child.RenderSize}.");
        Assert.Equal(RibbonGroupSizeState.Collapsed, host.Group.SizeState);
        Assert.Same(selected, host.Ribbon.SelectedTab);
        Assert.Same(sourceParent, host.Group.Parent);
        Assert.Null(host.Normal.Child);
        Assert.NotNull(host.Flyout.Child);
        Assert.True(host.Flyout.IsAncestorOf(host.Combo));
        Assert.True(host.Flyout.IsAncestorOf(host.Command));
        Assert.Same(host.Proxy, host.Popup.PlacementTarget);
        host.Combo.SetCurrentValue(Selector.SelectedIndexProperty, 1);
        Assert.Same(sourceBinding, BindingOperations.GetBindingExpression(host.Combo, Selector.SelectedIndexProperty));
        Assert.Equal(1, host.Model.Index);
        var peer = UIElementAutomationPeer.CreatePeerForElement(host.Proxy)!;
        var expand = Assert.IsAssignableFrom<IExpandCollapseProvider>(peer.GetPattern(PatternInterface.ExpandCollapse));
        Assert.Equal(System.Windows.Automation.ExpandCollapseState.Expanded, expand.ExpandCollapseState);
        expand.Collapse(); host.Layout();
        Assert.True(host.Group.IsQuickAccessCollapsed);
        Assert.NotNull(host.Normal.Child);
        Assert.Null(host.Flyout.Child);
        Assert.Same(sourceParent, host.Group.Parent);
    });

    [Fact]
    public void Outside_press_and_first_click_keep_permanent_group_layout_and_sibling_popup_stable() => Sta.Run(() =>
    {
        using var host = new GroupHost();
        host.Open();
        Press(host.Outside, Mouse.PreviewMouseDownEvent);
        host.Layout();
        Assert.False(host.Popup.IsOpen);
        Assert.True(host.Group.IsQuickAccessCollapsed);
        Assert.Equal(RibbonGroupSizeState.Collapsed, host.Group.SizeState);
        bool clicked = false;
        host.Outside.AddHandler(Mouse.MouseUpEvent, new MouseButtonEventHandler((_, _) =>
        {
            Assert.True(host.Group.IsQuickAccessCollapsed);
            host.Outside.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, host.Outside));
        }), true);
        host.Outside.Click += (_, _) =>
        {
            Assert.True(host.Group.IsQuickAccessCollapsed);
            clicked = true;
            host.SiblingMenu.IsDropDownOpen = true;
        };
        Press(host.Outside, Mouse.MouseUpEvent);
        Assert.True(clicked);
        host.Layout();
        Assert.True(host.Group.IsQuickAccessCollapsed);
        Assert.True(host.SiblingMenu.IsDropDownOpen);
        Assert.True(host.SiblingPopup.IsOpen);
        Assert.True(host.SiblingPopup.Child!.IsVisible);
    });

    [Theory]
    [InlineData("remove")]
    [InlineData("reset")]
    [InlineData("tab")]
    [InlineData("template")]
    [InlineData("unload")]
    [InlineData("escape")]
    public void Interrupted_sessions_restore_ownership_and_keep_collapse_until_removal(string interruption) => Sta.Run(() =>
    {
        using var host = new GroupHost();
        host.Open();
        var originalPopup = host.Popup;
        Assert.True(host.Group.IsQuickAccessCollapsed);
        switch (interruption)
        {
            case "remove": host.Ribbon.QuickAccessItems.Remove(host.Proxy); break;
            case "reset": host.Ribbon.QuickAccessItems.Clear(); RibbonCustomizationSerializer.Apply(host.Ribbon, RibbonCustomizationSerializer.Serialize(host.Ribbon)); break;
            case "tab": host.Ribbon.SelectedTab = host.OtherTab; break;
            case "template": host.Group.Template = new ControlTemplate(typeof(RibbonGroup)); host.Group.ApplyTemplate(); break;
            case "unload": host.Proxy.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent)); break;
            case "escape":
                host.Window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
                    PresentationSource.FromVisual(host.Window)!, 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
                break;
        }
        host.Layout();
        Assert.Equal(interruption is not ("remove" or "reset"), host.Group.IsQuickAccessCollapsed);
        Assert.False(originalPopup.IsOpen);
        Press(host.Outside, Mouse.MouseUpEvent); host.Layout();
        Assert.Equal(interruption is not ("remove" or "reset"), host.Group.IsQuickAccessCollapsed);
        Assert.False(RibbonPopupInteraction.GetSuppressHover(host.Ribbon));
    });

    [Fact]
    public void Source_collapsed_opener_can_take_the_shared_popup_before_closed_is_dispatched() => Sta.Run(() =>
    {
        using var host = new GroupHost();
        host.Open();
        var sourceOpener = host.Group.CollapsedButton!;
        Press(sourceOpener, Mouse.PreviewMouseDownEvent);
        sourceOpener.SetCurrentValue(ToggleButton.IsCheckedProperty, true);
        host.Layout();
        Assert.False(host.Proxy.IsDropDownOpen);
        Assert.True(host.Popup.IsOpen);
        Assert.Same(sourceOpener, host.Popup.PlacementTarget);
        Assert.True(host.Flyout.IsAncestorOf(host.Command));
        Assert.False(RibbonPopupInteraction.GetSuppressHover(host.Ribbon));
    });

    [Fact]
    public void Original_dropdown_reclaims_native_items_before_deferred_proxy_return() => Sta.Run(() =>
    {
        using var host = new GroupHost();
        host.Ribbon.QuickAccessItems.Clear();
        host.Ribbon.AddToQuickAccess(host.SiblingMenu); host.Layout();
        var proxy = (RibbonDropDownButton)host.Ribbon.QuickAccessItems[0];
        var item = Assert.Single(host.SiblingMenu.Items.Cast<object>());
        proxy.IsDropDownOpen = true; host.Layout();
        Press(host.SiblingMenu, Mouse.PreviewMouseDownEvent);
        Assert.False(proxy.IsDropDownOpen);
        host.SiblingMenu.IsDropDownOpen = true;
        Assert.Same(item, Assert.Single(host.SiblingMenu.Items.Cast<object>()));
        Assert.Empty(proxy.Items);
        host.Layout();
        Assert.True(host.SiblingPopup.IsOpen);
        Assert.False(proxy.IsDropDownOpen);
    });

    [Fact]
    public void Custom_group_qat_identity_resolves_to_recreated_group_and_declared_groups_stay_out_of_ribbon_catalog() => Sta.Run(() =>
    {
        using var host = new GroupHost();
        var custom = new RibbonGroup { Header = "Favorites" };
        Ribbon.SetIsCustom(custom, true); Ribbon.SetCommandId(custom, "custom:favorites");
        custom.Items.Add(host.Ribbon.CreateCommandProxy(host.Command, RibbonControlSize.Small)!);
        host.OtherTab.Groups.Add(custom);
        host.Ribbon.AddToQuickAccess(custom);
        string json = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        RibbonCustomizationSerializer.Apply(host.Ribbon, json); host.Layout();
        var restored = Assert.Single(host.OtherTab.Groups, Ribbon.GetIsCustom);
        Assert.NotSame(custom, restored);
        Assert.Same(restored, Ribbon.GetQuickAccessSource((DependencyObject)host.Ribbon.QuickAccessItems[1]));
        Assert.True(restored.IsQuickAccessCollapsed);
        var declared = new RibbonGroup { Header = "Declared" };
        host.Ribbon.QuickAccessItems.Add(declared); host.Layout();
        Assert.DoesNotContain(RibbonCommandCatalog.CollectAvailable(host.Ribbon), entry => entry.Control is RibbonGroup);
        Assert.Contains(RibbonCommandCatalog.CollectAvailable(host.Ribbon, true), entry => ReferenceEquals(entry.Control, declared));
    });

    [Fact]
    public void Qat_membership_keeps_group_collapsed_and_removal_uses_current_adaptive_width() => Sta.Run(() =>
    {
        using var host = new GroupHost();
        Assert.Equal(RibbonGroupSizeState.Collapsed, host.Group.SizeState);
        double compactWidth = host.Group.ActualWidth;
        host.Open();
        Assert.Equal(compactWidth, host.Group.ActualWidth);
        host.Proxy.IsDropDownOpen = false; host.Layout();
        Assert.Equal(compactWidth, host.Group.ActualWidth);
        host.Ribbon.QuickAccessItems.Clear(); host.Layout();
        Assert.False(host.Group.IsQuickAccessCollapsed);
        Assert.True(host.Group.ActualWidth > compactWidth);
        host.Ribbon.AddToQuickAccess(host.Group); host.Layout();
        host.Window.Width = 260; host.Layout();
        host.Ribbon.QuickAccessItems.Clear(); host.Layout();
        Assert.Equal(RibbonGroupSizeState.Collapsed, host.Group.SizeState);
        Assert.False(host.Group.IsQuickAccessCollapsed);
        host.Window.Width = 900; host.Layout();
        Assert.Equal(RibbonGroupSizeState.Large, host.Group.SizeState);
        host.Group.CanResize = false; host.Layout();
        host.Ribbon.AddToQuickAccess(host.Group); host.Layout();
        Assert.Equal(RibbonGroupSizeState.Collapsed, host.Group.SizeState);
        host.Ribbon.QuickAccessItems.Clear(); host.Layout();
        Assert.Equal(RibbonGroupSizeState.Large, host.Group.SizeState);
    });

    [Fact]
    public void Group_catalog_is_qat_only_and_identity_order_duplicates_reset_and_merged_exclusions_round_trip() => Sta.Run(() =>
    {
        using var host = new GroupHost();
        Assert.DoesNotContain(RibbonCommandCatalog.CollectAvailable(host.Ribbon), entry => entry.Control is RibbonGroup);
        Assert.Contains(RibbonCommandCatalog.CollectAvailable(host.Ribbon, true), entry => ReferenceEquals(entry.Control, host.Group));
        Assert.False(host.Ribbon.AddToQuickAccess(host.Group));
        host.Ribbon.AddToQuickAccess(host.Command);
        host.Open();
        string saved = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        Assert.DoesNotContain("Collapsed", saved);
        Assert.DoesNotContain("QuickAccessCollapsed", saved);
        host.Proxy.IsDropDownOpen = false; host.Layout();
        using var fresh = new GroupHost();
        RibbonCustomizationSerializer.Apply(fresh.Ribbon, saved); fresh.Layout();
        Assert.Equal(2, fresh.Ribbon.QuickAccessItems.Count);
        Assert.Same(fresh.Group, Ribbon.GetQuickAccessSource((DependencyObject)fresh.Ribbon.QuickAccessItems[0]));
        Assert.Same(fresh.Command, Ribbon.GetQuickAccessSource((DependencyObject)fresh.Ribbon.QuickAccessItems[1]));
        var merged = new RibbonGroup { Header = "Merged" }; fresh.OtherTab.Groups.Add(merged);
        Ribbon.SetCommandId(merged, "group.merged"); Ribbon.SetMergeSourceInternal(merged, new RibbonMergeSource());
        fresh.Ribbon.AddToQuickAccess(merged);
        Assert.DoesNotContain("group.merged\"}", RibbonCustomizationSerializer.Serialize(fresh.Ribbon));
        RibbonCustomizationSerializer.Apply(fresh.Ribbon, fresh.Defaults); fresh.Layout();
        Assert.Single(fresh.Ribbon.QuickAccessItems);
    });

    [Fact]
    public void Automatic_group_qat_identity_survives_reconciliation_of_named_siblings() => Sta.Run(() =>
    {
        using var host = new GroupHost();
        host.Group.ClearValue(Ribbon.CommandIdProperty);
        string json = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        RibbonCustomizationSerializer.Apply(host.Ribbon, json); host.Layout();
        Assert.Same(host.Group, Ribbon.GetQuickAccessSource((DependencyObject)Assert.Single(host.Ribbon.QuickAccessItems)));
        RibbonCustomizationSerializer.Apply(host.Ribbon, json); host.Layout();
        Assert.Same(host.Group, Ribbon.GetQuickAccessSource((DependencyObject)Assert.Single(host.Ribbon.QuickAccessItems)));
    });

    [Fact]
    public void Caption_resolves_group_context_menu_and_duplicate_state() => Sta.Run(() =>
    {
        using var host = new GroupHost();
        var resolve = typeof(Ribbon).GetMethod("ResolveCommandControl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var caption = (DependencyObject)host.Group.Template.FindName("GroupCaption", host.Group);
        Assert.Same(host.Group, resolve.Invoke(host.Ribbon, new object[] { caption }));
        var create = typeof(Ribbon).GetMethod("CreateRibbonContextMenu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var menu = (ContextMenu)create.Invoke(host.Ribbon, new object[] { host.Group })!;
        Assert.False(((MenuItem)menu.Items[0]).IsEnabled);
        host.Ribbon.QuickAccessItems.Clear();
        menu = (ContextMenu)create.Invoke(host.Ribbon, new object[] { host.Group })!;
        Assert.True(((MenuItem)menu.Items[0]).IsEnabled);
    });

    [Fact]
    public void Hover_suppression_keeps_qat_and_nested_controls_interactive_and_cleans_up() => Sta.Run(() =>
    {
        using var host = new GroupHost();
        host.Open();
        Assert.True(RibbonPopupInteraction.GetSuppressHover(host.Ribbon));
        Assert.True(RibbonPopupInteraction.GetSuppressHover(host.Outside));
        Assert.True(RibbonPopupInteraction.GetSuppressHover(host.OtherTab));
        Assert.False(RibbonPopupInteraction.GetSuppressHover(host.Proxy));
        Assert.False(RibbonPopupInteraction.GetSuppressHover(host.Combo));
        host.Menu.IsDropDownOpen = true; host.Layout();
        Assert.True(host.Menu.IsDropDownOpen);
        Assert.True(host.Popup.IsOpen);
        host.Proxy.IsDropDownOpen = false; host.Layout();
        Assert.False(host.Menu.IsDropDownOpen);
        Assert.False(RibbonPopupInteraction.GetSuppressHover(host.Ribbon));
    });

    [Theory]
    [InlineData("group", false)]
    [InlineData("combo", false)]
    [InlineData("gallery", false)]
    [InlineData("dropdown", false)]
    [InlineData("group", true)]
    [InlineData("combo", true)]
    [InlineData("gallery", true)]
    [InlineData("dropdown", true)]
    public void Qat_popups_leave_the_original_outside_press_available_to_commands_and_file(string kind, bool file) => Sta.Run(() =>
    {
        using var host = new GroupHost();
        if (file) { ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2007); host.Layout(); }
        RibbonDropDownButton proxy = host.Proxy;
        if (kind != "group")
        {
            host.Ribbon.QuickAccessItems.Clear();
            FrameworkElement source = kind switch
            {
                "combo" => host.Combo,
                "gallery" => new RibbonGallery { Header = "Styles", ItemsSource = new[] { "Plain", "Heading" } },
                _ => host.SiblingMenu,
            };
            if (kind == "gallery") host.Group.Items.Add(source);
            host.Ribbon.AddToQuickAccess(source); host.Layout();
            proxy = (RibbonDropDownButton)host.Ribbon.QuickAccessItems[0];
        }
        proxy.IsDropDownOpen = true; host.Layout();
        var tabs = (RibbonTabControl)host.Ribbon.Template.FindName("TabControlHost", host.Ribbon);
        UIElement target = file ? (ToggleButton)tabs.Template.FindName("PART_ApplicationButton", tabs) : host.Outside;
        Assert.True(RibbonPopupInteraction.GetSuppressHover(target));
        Assert.False(RibbonPopupInteraction.GetSuppressHover(proxy));
        var press = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            { RoutedEvent = Mouse.PreviewMouseDownEvent, Source = target };
        InputManager.Current.ProcessInput(press);
        Assert.False(press.Handled);
        if (file) ((ToggleButton)target).SetCurrentValue(ToggleButton.IsCheckedProperty, true);
        else
        {
            int clicked = 0; host.Outside.Click += (_, _) => clicked++;
            host.Outside.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(1, clicked);
        }
        host.Layout();
        Assert.False(proxy.IsDropDownOpen);
        Assert.False(RibbonPopupInteraction.GetSuppressHover(host.Ribbon));
        if (file) Assert.True(host.Ribbon.IsBackstageOpen);
    });

    [Fact]
    public void Membership_transition_preserves_bound_transforms_and_stops_when_motion_is_disabled() => Sta.Run(() =>
    {
        using var host = new GroupHost();
        host.Ribbon.QuickAccessItems.Clear(); host.Layout();
        var sibling = host.Ribbon.Tabs[0].Groups[1];
        var transform = new ScaleTransform(0.95, 0.95);
        sibling.SetBinding(UIElement.RenderTransformProperty, new Binding { Source = transform });
        var expression = BindingOperations.GetBindingExpression(sibling, UIElement.RenderTransformProperty);
        bool respect = RibbonAnimation.RespectSystemReduceMotion;
        try
        {
            RibbonAnimation.RespectSystemReduceMotion = false;
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;
            host.Ribbon.AddToQuickAccess(host.Group);
            Assert.Equal(RibbonGroupSizeState.Collapsed, host.Group.SizeState);
            var combined = Assert.IsType<TransformGroup>(sibling.RenderTransform);
            Assert.Same(transform, combined.Children[0]);
            Assert.IsType<TranslateTransform>(combined.Children[1]);
            Assert.Same(expression, BindingOperations.GetBindingExpression(sibling, UIElement.RenderTransformProperty));
            ((RibbonGroupsPanel)VisualTreeHelper.GetParent(host.Group)).InvalidateStateCache(); host.Window.UpdateLayout();
            Assert.Same(combined, sibling.RenderTransform); // Adaptive probing did not start another animation.
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            Assert.Same(transform, sibling.RenderTransform);
            Assert.False(sibling.HasAnimatedProperties);
            host.Ribbon.QuickAccessItems.Clear(); host.Layout();
            Assert.Same(transform, sibling.RenderTransform);
        }
        finally { RibbonAnimation.RespectSystemReduceMotion = respect; }
    });

    internal static void Press(UIElement target, RoutedEvent routedEvent) => InputManager.Current.ProcessInput(
        new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            { RoutedEvent = routedEvent, Source = target });

    private sealed class SelectionModel { public int Index { get; set; } }

    private sealed class GroupHost : IDisposable
    {
        internal readonly SelectionModel Model = new();
        internal readonly Ribbon Ribbon = new() { QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon, Backstage = new Backstage() };
        internal readonly RibbonGroup Group = new() { Header = "Source group" };
        internal readonly RibbonTab OtherTab = new() { Header = "Other" };
        internal readonly RibbonButton Command = new() { Header = "Command", Width = 190 };
        internal readonly RibbonComboBox Combo = new() { Header = "Choice", Width = 120 };
        internal readonly RibbonDropDownButton Menu = new() { Header = "Nested" };
        internal readonly RibbonButton Outside = new() { Header = "Outside", Width = 80 };
        internal readonly RibbonDropDownButton SiblingMenu = new() { Header = "Sibling" };
        internal readonly RibbonWindow Window;
        internal readonly RibbonDropDownButton Proxy;
        internal readonly string Defaults;
        internal Popup Popup => (Popup)Group.Template.FindName("PART_Popup", Group);
        internal Popup SiblingPopup => (Popup)SiblingMenu.Template.FindName("PART_Popup", SiblingMenu);
        internal Decorator Normal => (Decorator)Group.Template.FindName("PART_NormalHost", Group);
        internal Border Flyout => (Border)Group.Template.FindName("PART_PopupHost", Group);

        internal GroupHost(bool inactive = false)
        {
            ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024);
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            Combo.Items.Add("First"); Combo.Items.Add("Second");
            Combo.SetBinding(Selector.SelectedIndexProperty, new Binding(nameof(SelectionModel.Index)) { Source = Model, Mode = BindingMode.TwoWay });
            Menu.Items.Add(new RibbonMenuItem { Header = "Nested action" });
            SiblingMenu.Items.Add(new RibbonMenuItem { Header = "Sibling action" });
            Group.Items.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { Command, Combo, Menu } });
            var home = new RibbonTab { Header = "Home" }; home.Groups.Add(Group);
            var sibling = new RibbonGroup { Header = "Sibling", CanResize = false };
            sibling.Items.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { Outside, SiblingMenu } });
            home.Groups.Add(sibling);
            Ribbon.SetCommandId(home, "tab.home"); Ribbon.SetCommandId(OtherTab, "tab.other");
            Ribbon.SetCommandId(Group, "group.source"); Ribbon.SetCommandId(sibling, "group.sibling");
            Ribbon.SetCommandId(Command, "command.action");
            Ribbon.Tabs.Add(home); Ribbon.Tabs.Add(OtherTab); Ribbon.SelectedTab = inactive ? OtherTab : home;
            Ribbon.AddToQuickAccess(Group);
            Proxy = (RibbonDropDownButton)Ribbon.QuickAccessItems[0];
            Defaults = RibbonCustomizationSerializer.Serialize(Ribbon);
            Window = new RibbonWindow { Content = Ribbon, Width = 900, Height = 350, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false };
            Window.Show(); Layout();
            Group.ApplyTemplate();
        }
        internal void Open() { Proxy.IsDropDownOpen = true; Layout(); }
        internal void Layout() { Sta.Drain(); Window.UpdateLayout(); Sta.Drain(); }
        public void Dispose()
        {
            Window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle; Sta.ResetApplication();
        }
    }
}
