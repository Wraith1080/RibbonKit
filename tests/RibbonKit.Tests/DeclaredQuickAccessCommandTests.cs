using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public sealed class DeclaredQuickAccessCommandTests
{
    [Fact]
    public void Removed_qat_command_keeps_inherited_bindings_and_window_command_routing() => Sta.Run(() =>
    {
        var command = new RoutedUICommand("Undo", "Undo", typeof(DeclaredQuickAccessCommandTests));
        var ribbon = new Ribbon { DataContext = new { Undo = command } };
        var undo = new RibbonButton { Header = "Undo", Size = RibbonControlSize.Small };
        undo.SetBinding(ButtonBase.CommandProperty, new Binding("Undo"));
        ribbon.QuickAccessItems.Add(undo);
        var window = new Window { Content = ribbon, Width = 700, Height = 350,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        int executed = 0;
        window.CommandBindings.Add(new CommandBinding(command, (_, _) => executed++, (_, e) => e.CanExecute = true));
        try
        {
            window.Show(); Sta.Drain();
            Assert.Same(command, undo.Command);
            ribbon.QuickAccessItems.Clear(); Sta.Drain();
            Assert.Same(command, undo.Command);
            var proxy = Assert.IsType<RibbonButton>(ribbon.CreateCommandProxy(undo, RibbonControlSize.Medium));
            proxy.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Sta.Drain();
            Assert.Equal(1, executed);
            var replacement = new RoutedUICommand("Undo again", "UndoAgain", typeof(DeclaredQuickAccessCommandTests));
            window.CommandBindings.Add(new CommandBinding(replacement, (_, _) => executed++, (_, e) => e.CanExecute = true));
            ribbon.DataContext = new { Undo = replacement };
            Sta.Drain(); Assert.Same(replacement, undo.Command);
            proxy.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Sta.Drain();
            Assert.Equal(2, executed);
            Assert.True(ribbon.AddToQuickAccess(undo)); Sta.Drain();
            Assert.Same(undo, Assert.Single(ribbon.QuickAccessItems));
            undo.DataContext = new { Undo = command }; Sta.Drain();
            ribbon.DataContext = new { Undo = replacement }; Sta.Drain();
            Assert.Same(command, undo.Command); // A local host override still wins.
        }
        finally { window.Close(); Sta.Drain(); }
    });

    [Fact]
    public void Removed_and_replaced_declared_commands_remain_available_without_proxy_chains() => Sta.Run(() =>
    {
        var ribbon = new Ribbon();
        var undo = new RibbonButton { ScreenTipTitle = "Undo (Ctrl+Z)" };
        var redo = new RibbonSplitButton { Header = "Redo", DropDownHeader = "Redo actions" };
        var toggle = new RibbonToggleButton { Header = "Track changes" };
        ribbon.QuickAccessItems.Add(undo);
        ribbon.QuickAccessItems[0] = redo;
        ribbon.QuickAccessItems.Add(toggle);
        ribbon.QuickAccessItems.Clear();

        Assert.Equal(new FrameworkElement[] { undo, redo, toggle },
            RibbonCommandCatalog.CollectAvailable(ribbon).Select(entry => entry.Control));
        Assert.True(ribbon.AddToQuickAccess(undo));
        Assert.Same(undo, Assert.Single(ribbon.QuickAccessItems));
        Assert.False(ribbon.AddToQuickAccess(undo));
        ribbon.QuickAccessItems.Clear();
        Assert.True(ribbon.AddToQuickAccess(undo));
        Assert.Equal(3, RibbonCommandCatalog.CollectAvailable(ribbon).Count);

        var tab = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Commands" };
        var button = new RibbonButton { Header = "Copy" };
        group.Items.Add(button); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.AddToQuickAccess(button);
        ribbon.QuickAccessItems.Add(new TextBox());
        ribbon.QuickAccessItems.Clear();
        group.Items.Remove(button);
        Assert.DoesNotContain(RibbonCommandCatalog.CollectAvailable(ribbon), entry => ReferenceEquals(entry.Control, button));
        Assert.Equal(3, RibbonCommandCatalog.CollectAvailable(ribbon).Count);
    });

    [Fact]
    public void Qat_only_commands_are_deduplicated_and_supply_custom_group_icons() => Sta.Run(() =>
    {
        var ribbon = new Ribbon();
        var undo = new RibbonButton { Header = "Undo", Icon = new DrawingImage() };
        var tab = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Editing" };
        group.Items.Add(undo); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(undo);
        ribbon.QuickAccessItems.Clear();
        Assert.Single(RibbonCommandCatalog.CollectAvailable(ribbon));
        group.Items.Remove(undo);
        Assert.Same(undo.Icon, Assert.Single(RibbonCommandCatalog.CollectIcons(ribbon)));
    });

    [Fact]
    public void Reset_restores_removed_declared_items_repeatedly() => Sta.Run(() =>
    {
        var ribbon = new Ribbon();
        var undo = new RibbonButton { Header = "Undo" };
        Ribbon.SetCommandId(undo, "cmd.undo"); ribbon.QuickAccessItems.Add(undo);
        string baseline = RibbonCustomizationSerializer.Serialize(ribbon);
        for (int i = 0; i < 3; i++)
        {
            ribbon.QuickAccessItems.Clear();
            RibbonCustomizationSerializer.Apply(ribbon, baseline);
            Assert.Same(undo, Assert.Single(ribbon.QuickAccessItems));
            Assert.Equal(baseline, RibbonCustomizationSerializer.Serialize(ribbon));
        }
    });

    [Fact]
    public void Removed_qat_source_round_trips_in_custom_groups_on_a_fresh_consumer() => Sta.Run(() =>
    {
        (Ribbon source, RibbonButton undo) = Create();
        var tab = new RibbonTab { Header = "Favorites" };
        Ribbon.SetCommandId(tab, "custom:favorites"); Ribbon.SetIsCustom(tab, true);
        var group = new RibbonGroup { Header = "Editing", Layout = RibbonGroupLayout.Stacked, Icon = undo.Icon };
        Ribbon.SetCommandId(group, "custom:editing"); Ribbon.SetIsCustom(group, true);
        group.Items.Add(source.CreateCommandProxy(undo, RibbonControlSize.Medium));
        tab.Groups.Add(group); source.Tabs.Add(tab);
        source.QuickAccessItems.Clear();
        string saved = RibbonCustomizationSerializer.Serialize(source);

        (Ribbon target, RibbonButton freshUndo) = Create();
        RibbonCustomizationSerializer.Apply(target, saved);
        Assert.Empty(target.QuickAccessItems);
        var freshGroup = Assert.Single(Assert.Single(target.Tabs).Groups);
        Assert.Same(freshUndo.Icon, freshGroup.Icon);
        var proxy = Assert.IsType<RibbonButton>(Assert.Single(freshGroup.Items));
        Assert.Same(freshUndo, Ribbon.GetQuickAccessSource(proxy));
        Assert.Same(freshUndo, Assert.Single(RibbonCommandCatalog.CollectAvailable(target)).Control);
        int clicks = 0; freshUndo.Click += (_, _) => clicks++;
        proxy.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Sta.Drain(); // WPF's Invoke provider posts the source click to the dispatcher.
        Assert.Equal(1, clicks);
        freshUndo.IsEnabled = false; Assert.False(proxy.IsEnabled);
        freshUndo.IsEnabled = true; Assert.True(proxy.IsEnabled);
        Assert.True(target.AddToQuickAccess(freshUndo));
        Assert.Same(freshUndo, Assert.Single(target.QuickAccessItems));
        string readded = RibbonCustomizationSerializer.Serialize(target);
        target.QuickAccessItems.Clear();
        RibbonCustomizationSerializer.Apply(target, readded);
        Assert.Same(freshUndo, Assert.Single(target.QuickAccessItems));

        static (Ribbon, RibbonButton) Create()
        {
            var ribbon = new Ribbon();
            var command = new RibbonButton { Header = "Undo", Size = RibbonControlSize.Small, Icon = new DrawingImage() };
            Ribbon.SetCommandId(command, "cmd.undo"); ribbon.QuickAccessItems.Add(command);
            return (ribbon, command);
        }
    });
}
