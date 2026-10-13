using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Localization;
using Xunit;

namespace RibbonKit.Tests;

public sealed partial class SimplifiedCustomizationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Tab_move_existing_before_or_after_is_drafted_then_preserves_live_objects_and_profiles(bool after) => Sta.Run(() =>
    {
        using var host = new Host();
        var insert = AddMoveTab(host, "Insert", "insert", false);
        var custom = AddMoveTab(host, "Tools", "tools", true);
        var context = new RibbonTab { Header = "Context", IsContextual = true }; host.Ribbon.Tabs.Insert(1, context);
        var modal = new RibbonTab { Header = "Modal", IsModal = true }; host.Ribbon.Tabs.Add(modal);
        var group = custom.Groups.Single(); var copy = host.Ribbon.CreateCommandProxy(host.First, RibbonControlSize.Medium); group.Items.Add(copy);
        host.Ribbon.SetSimplifiedChoices(custom, new[] { new SimplifiedCommandChoice(host.First, RibbonControlSize.Small, group) });
        host.Ribbon.AddToQuickAccess(host.First); host.Ribbon.SelectedTab = custom; host.Page.CancelChanges(); host.Layout();
        SelectMoveTab(host, custom); string original = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        var target = after ? host.Tab : insert;
        Assert.True(host.Page.StageTabMove(custom, target, after));
        Assert.Equal(new[] { host.Tab, custom, insert }, MoveDraftTabs(host));
        Assert.Same(custom, ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab);
        Assert.Equal(original, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        host.Page.ApplyChanges(); host.Layout();
        Assert.Equal(new[] { host.Tab, custom, insert }, host.Ribbon.Tabs.Where(RibbonCustomizePage.IsCustomizableTab));
        Assert.Same(custom, host.Ribbon.SelectedTab); Assert.Same(group, custom.Groups.Single()); Assert.Same(copy, group.Items[0]);
        Assert.Equal(RibbonControlSize.Small, Assert.Single(host.Ribbon.GetSimplifiedChoices(custom)).Size);
        Assert.Same(host.First, Ribbon.GetQuickAccessSource(Assert.IsAssignableFrom<FrameworkElement>(Assert.Single(host.Ribbon.QuickAccessItems))));
        Assert.Contains(context, host.Ribbon.Tabs); Assert.Contains(modal, host.Ribbon.Tabs);
        Assert.True(host.Page.StageTabMove(custom, insert, true)); host.Page.CancelChanges();
        Assert.Equal(new[] { host.Tab, custom, insert }, MoveDraftTabs(host));
        Assert.DoesNotContain(host.Part<Selector>("PART_SourceTabSelector").Items.Cast<SimplifiedTabChoice>(), choice => choice.Tab == custom);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Tab_move_creation_and_repeated_placements_keep_their_chronological_order_in_export_and_apply(bool import) => Sta.Run(() =>
    {
        using var host = new Host(); var insert = AddMoveTab(host, "Insert", "insert", false); host.Page.CancelChanges();
        host.Click("PART_NewTabButton"); var first = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        host.Page.RenameDraft(first, "First tools"); host.SelectAvailable(host.First); host.Click("PART_AddButton");
        Assert.True(host.Page.StageTabMove(first, host.Tab, false));
        host.Click("PART_NewTabButton"); var second = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        host.Page.RenameDraft(second, "Second tools");
        Assert.Equal(new[] { first, second, host.Tab, insert }, MoveDraftTabs(host));
        Assert.True(host.Page.StageTabMove(second, insert, true)); Assert.True(host.Page.StageTabMove(first, second, true));
        string firstId = Ribbon.GetCommandId(first)!; string secondId = Ribbon.GetCommandId(second)!;
        var expected = new[] { "home", "insert", secondId, firstId };
        string exported = host.Page.ExportDraft();
        Assert.Equal(expected, JsonNode.Parse(exported)!["Tabs"]!.AsArray().Select(tab => tab!["Id"]!.GetValue<string>()));
        Assert.Equal(new[] { host.Tab, insert }, host.Ribbon.Tabs);
        if (import) { host.Page.CancelChanges(); Assert.True(host.Page.ImportDraft(exported)); }
        host.Page.ApplyChanges(); host.Layout();
        Assert.Equal(expected, host.Ribbon.Tabs.Select(Ribbon.GetCommandId));
        Assert.Same(host.First, Assert.Single(host.Ribbon.GetSimplifiedChoices(host.Ribbon.Tabs.Last())).Source);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        Assert.Equal(expected, host.Ribbon.Tabs.Select(Ribbon.GetCommandId));
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        Assert.Equal(expected, MoveDraftTabs(host).Select(Ribbon.GetCommandId));
    });

    [Fact]
    public void Tab_move_rejects_builtin_transient_missing_self_and_unchanged_positions() => Sta.Run(() =>
    {
        using var host = new Host(); var custom = AddMoveTab(host, "Tools", "tools", true);
        var excluded = new RibbonTab { Header = "Context", IsContextual = true }; host.Ribbon.Tabs.Add(excluded); host.Page.CancelChanges();
        Assert.False(host.Page.StageTabMove(host.Tab, custom, true)); Assert.False(host.Page.StageTabMove(custom, custom, false));
        Assert.False(host.Page.StageTabMove(custom, excluded, false)); Assert.False(host.Page.StageTabMove(custom, new RibbonTab(), false));
        Assert.False(host.Page.StageTabMove(custom, host.Tab, true)); Assert.False(host.Part<Button>("PART_ApplyButton").IsEnabled);
        Assert.False(MoveMenuItem(host).IsEnabled); host.Part<Button>("PART_EditButton").ContextMenu.IsOpen = false;
        SelectMoveTab(host, custom); Assert.True(MoveMenuItem(host).IsEnabled); host.Part<Button>("PART_EditButton").ContextMenu.IsOpen = false;
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; Assert.False(host.Page.StageTabMove(custom, host.Tab, false));
    });

    [Fact]
    public void Tab_move_skips_removed_anchors_and_resolves_replaced_custom_tabs_by_stable_id() => Sta.Run(() =>
    {
        using var host = new Host(); var insert = AddMoveTab(host, "Insert", "insert", false); var custom = AddMoveTab(host, "Tools", "tools", true);
        host.Page.CancelChanges(); Assert.True(host.Page.StageTabMove(custom, insert, false));
        host.Ribbon.Tabs.Remove(insert); host.Page.ApplyChanges(); Assert.Equal(new[] { host.Tab, custom }, host.Ribbon.Tabs);
        Assert.True(host.Page.StageTabMove(custom, host.Tab, false));
        host.Ribbon.Tabs.Remove(custom); var replacement = AddMoveTab(host, "Replacement tools", "tools", true);
        host.Page.ApplyChanges(); Assert.Equal(new[] { replacement, host.Tab }, host.Ribbon.Tabs);
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight, false)]
    [InlineData(FlowDirection.RightToLeft, true)]
    public void Tab_move_native_dialog_filters_targets_and_keeps_cancel_and_ok_draft_safe(FlowDirection direction, bool cancel) => Sta.Run(() =>
    {
        using var host = new Host(); var custom = AddMoveTab(host, "Tools", "tools", true);
        var context = new RibbonTab { Header = "Context", IsContextual = true }; host.Ribbon.Tabs.Add(context);
        host.Page.FlowDirection = direction; host.Page.CancelChanges(); SelectMoveTab(host, custom);
        string original = RibbonCustomizationSerializer.Serialize(host.Ribbon); bool seen = false;
        var item = MoveMenuItem(host);
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            var dialog = Application.Current.Windows.OfType<RibbonCustomizeMoveTabDialog>().Single();
            try
            {
                seen = true; Assert.Equal(direction, dialog.FlowDirection); Assert.Same(host.Window, dialog.Owner);
                var position = (Selector)dialog.Template.FindName("PART_PositionSelector", dialog);
                var target = (Selector)dialog.Template.FindName("PART_TargetTabSelector", dialog);
                Assert.Same(host.Tab, Assert.Single(target.Items.Cast<SimplifiedTabChoice>()).Tab);
                Assert.True(((Button)dialog.Template.FindName("PART_OkButton", dialog)).IsEnabled);
                position.SelectedIndex = 0; dialog.UpdateLayout(); Save(dialog, "move-tab-" + direction);
                ((ButtonBase)dialog.Template.FindName(cancel ? "PART_CancelButton" : "PART_OkButton", dialog)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
            finally { if (dialog.IsVisible) dialog.Close(); }
        }));
        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); host.Part<Button>("PART_EditButton").ContextMenu.IsOpen = false;
        host.Layout(); Assert.True(seen); Assert.Equal(original, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        Assert.Equal(cancel ? new[] { host.Tab, custom } : new[] { custom, host.Tab }, MoveDraftTabs(host));
        host.Page.CancelChanges(); Assert.Equal(new[] { host.Tab, custom }, MoveDraftTabs(host));
    });

    private static RibbonTab AddMoveTab(Host host, string name, string id, bool custom)
    {
        var tab = new RibbonTab { Header = name }; Ribbon.SetCommandId(tab, id); Ribbon.SetIsCustom(tab, custom);
        var group = custom ? RibbonCustomizePage.CreateCustomGroup(host.Ribbon) : new RibbonGroup { Header = name };
        if (!custom) Ribbon.SetCommandId(group, id + "-group");
        tab.Groups.Add(group); host.Ribbon.Tabs.Add(tab); return tab;
    }
    [Theory]
    [InlineData(RibbonDensity.Compact)]
    [InlineData(RibbonDensity.Touch)]
    public void Tab_move_dialog_centers_its_final_size_and_refreshes_localization(RibbonDensity density) => Sta.Run(() =>
    {
        using var host = new Host(); var custom = AddMoveTab(host, "Tools", "tools", true);
        host.Ribbon.Density = density; host.Window.Left = 120; host.Window.Top = 100; host.Window.Height = 650;
        host.Page.CancelChanges(); SelectMoveTab(host, custom); host.Layout(); var item = MoveMenuItem(host);
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            var dialog = Application.Current.Windows.OfType<RibbonCustomizeMoveTabDialog>().Single();
            var previous = RibbonLocalization.Provider;
            try
            {
                dialog.UpdateLayout(); Assert.InRange(Math.Abs(dialog.Top + dialog.ActualHeight / 2 - host.Window.Top - host.Window.ActualHeight / 2), 0, 3);
                Assert.InRange(Math.Abs(dialog.Left + dialog.ActualWidth / 2 - host.Window.Left - host.Window.ActualWidth / 2), 0, 3);
                RibbonLocalization.Provider = new PageProvider(); host.Layout();
                Assert.Equal("localized:MoveTab", dialog.Title);
                var position = (Selector)dialog.Template.FindName("PART_PositionSelector", dialog);
                var label = new TextBlock(); label.SetBinding(TextBlock.TextProperty, new Binding(position.DisplayMemberPath) { Source = position.Items[0] });
                Assert.Equal("localized:Before", label.Text);
                ((ButtonBase)dialog.Template.FindName("PART_CancelButton", dialog)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
            finally { RibbonLocalization.Provider = previous; if (dialog.IsVisible) dialog.Close(); }
        }));
        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); host.Part<Button>("PART_EditButton").ContextMenu.IsOpen = false;
        Assert.False(host.Part<Button>("PART_ApplyButton").IsEnabled);
    });

    [Fact]
    public void Tab_move_collection_notification_retains_merge_provenance_and_unmerge_cleanup() => Sta.Run(() =>
    {
        using var host = new Host(); var source = new RibbonMergeSource(); var child = new RibbonTab { Header = "Child" };
        child.Groups.Add(new RibbonGroup { Header = "Child group" }); source.Tabs.Add(child);
        Assert.True(host.Ribbon.Merge(source)); host.Ribbon.Tabs.Move(host.Ribbon.Tabs.IndexOf(child), 0);
        Assert.True(source.IsMerged); Assert.True(Ribbon.GetIsMerged(child));
        Assert.True(host.Ribbon.Unmerge(source)); Assert.DoesNotContain(child, host.Ribbon.Tabs); Assert.False(source.IsMerged);
    });
    [Fact]
    public void Tab_move_during_modal_mode_preserves_the_custom_tabs_authored_visibility() => Sta.Run(() =>
    {
        using var host = new Host(); var custom = AddMoveTab(host, "Tools", "tools", true);
        var modal = new RibbonTab { Header = "Print", IsModal = true, Visibility = Visibility.Collapsed }; host.Ribbon.Tabs.Add(modal);
        host.Page.CancelChanges(); Assert.True(host.Ribbon.EnterModal(modal)); host.Layout();
        Assert.True(host.Page.StageTabMove(custom, host.Tab, false)); host.Page.ApplyChanges();
        Assert.Same(modal, host.Ribbon.SelectedTab); Assert.True(host.Ribbon.IsModal);
        Assert.True(host.Ribbon.ExitModal()); host.Layout();
        Assert.Equal(Visibility.Visible, custom.Visibility); Assert.Equal(new[] { custom, host.Tab }, MoveDraftTabs(host));
    });
    private static RibbonTab[] MoveDraftTabs(Host host) => host.Part<Selector>("PART_TabSelector").Items.Cast<SimplifiedTabChoice>().Select(choice => choice.Tab!).ToArray();
    private static void SelectMoveTab(Host host, RibbonTab tab) => host.Part<Selector>("PART_TabSelector").SelectedItem = host.Part<Selector>("PART_TabSelector").Items.Cast<SimplifiedTabChoice>().Single(choice => choice.Tab == tab);
    private static MenuItem MoveMenuItem(Host host)
    {
        host.Click("PART_EditButton");
        return host.Part<Button>("PART_EditButton").ContextMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, RibbonLocalization.GetString(RibbonString.MoveTab)));
    }
}
