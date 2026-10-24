using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RibbonKit.Controls;
using RibbonKit.Localization;
using Xunit;

namespace RibbonKit.Tests;

public sealed partial class SimplifiedCustomizationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Remove_custom_group_header_is_a_cancelable_structure_draft(bool committed) => Sta.Run(() =>
    {
        using var host = new Host();
        host.Click("PART_NewGroupButton");
        var group = ((SimplifiedGroupChoice)host.Current.SelectedItem).Group!;
        string id = Ribbon.GetCommandId(group)!;
        host.SelectAvailable(host.First); host.Click("PART_AddButton");
        if (committed) { host.Page.ApplyChanges(); host.Layout(); }
        string before = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        SelectRemovalGroup(host, group);
        Assert.True(host.Part<Button>("PART_RemoveButton").IsEnabled);
        host.Click("PART_RemoveButton");
        Assert.DoesNotContain(host.Current.Items.OfType<SimplifiedGroupChoice>(), choice => choice.Group == group);
        Assert.DoesNotContain(JsonNode.Parse(host.Page.ExportDraft())!["Tabs"]![0]!["Groups"]!.AsArray(), data => data!["Id"]!.GetValue<string>() == id);
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        host.Page.CancelChanges(); host.Layout();
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        Assert.Equal(committed, host.Tab.Groups.Contains(group));
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Remove_custom_tab_is_drafted_exported_and_cancelable(bool committed) => Sta.Run(() =>
    {
        using var host = new Host(); host.Click("PART_NewTabButton");
        var custom = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        host.SelectAvailable(host.First); host.Click("PART_AddButton");
        if (committed) { host.Page.ApplyChanges(); host.Layout(); }
        string before = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        var action = RemovalTabAction(host); Assert.True(action.IsEnabled);
        InvokeRemovalTab(host, action);
        Assert.DoesNotContain(MoveDraftTabs(host), tab => tab == custom);
        Assert.DoesNotContain(JsonNode.Parse(host.Page.ExportDraft())!["Tabs"]!.AsArray(), data => data!["Id"]!.GetValue<string>() == Ribbon.GetCommandId(custom));
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        host.Page.CancelChanges(); host.Layout();
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        Assert.Equal(committed, host.Ribbon.Tabs.Contains(custom));
    });

    [Fact]
    public void Removing_one_custom_group_preserves_other_copies_sources_qat_and_live_identity() => Sta.Run(() =>
    {
        using var host = new Host(); host.Ribbon.AddToQuickAccess(host.First);
        host.Click("PART_NewGroupButton"); var first = ((SimplifiedGroupChoice)host.Current.SelectedItem).Group!;
        host.SelectAvailable(host.First); host.Click("PART_AddButton");
        host.Click("PART_NewGroupButton"); var second = ((SimplifiedGroupChoice)host.Current.SelectedItem).Group!;
        host.SelectAvailable(host.First); host.Click("PART_AddButton"); host.Page.ApplyChanges(); host.Layout();
        var survivingCopy = second.Items[0]; var qat = host.Ribbon.QuickAccessItems[0];
        SelectRemovalGroup(host, first); host.Click("PART_RemoveButton"); host.Page.ApplyChanges(); host.Layout();
        Assert.DoesNotContain(first, host.Tab.Groups); Assert.Contains(second, host.Tab.Groups);
        Assert.Same(survivingCopy, second.Items[0]); Assert.Same(qat, host.Ribbon.QuickAccessItems[0]);
        Assert.Same(host.First, host.GroupA.Items[0]);
        Assert.Contains(host.Ribbon.GetSimplifiedChoices(host.Tab), choice => choice.Group == second && choice.Source == host.First);
        Assert.DoesNotContain(host.Ribbon.GetSimplifiedChoices(host.Tab), choice => choice.Group == first);
        string saved = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        Assert.True(host.Page.ImportDraft(saved)); host.Page.ApplyChanges(); host.Layout();
        Assert.DoesNotContain(host.Tab.Groups, group => Ribbon.GetCommandId(group) == Ribbon.GetCommandId(first));
        Assert.Contains(host.Tab.Groups, group => Ribbon.GetCommandId(group) == Ribbon.GetCommandId(second));
        Assert.True(host.Ribbon.IsInQuickAccess(host.First));
    });

    [Fact]
    public void Removing_created_anchors_keeps_later_tabs_and_groups_in_draft_order() => Sta.Run(() =>
    {
        using var host = new Host(); var insert = AddMoveTab(host, "Insert", "insert", false);
        host.Page.CancelChanges(); SelectMoveTab(host, host.Tab);
        host.Click("PART_NewTabButton"); var removed = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        host.Click("PART_NewTabButton"); var survivor = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        SelectMoveTab(host, removed); InvokeRemovalTab(host, RemovalTabAction(host));
        Assert.Equal(new[] { host.Tab, survivor, insert }, MoveDraftTabs(host));
        bool addedRemoved = false;
        host.Ribbon.Tabs.CollectionChanged += (_, e) => addedRemoved |= e.NewItems?.Contains(removed) == true;
        host.Page.ApplyChanges(); host.Layout();
        Assert.False(addedRemoved); Assert.Equal(new[] { host.Tab, survivor, insert }, host.Ribbon.Tabs);
        SelectMoveTab(host, host.Tab); host.Click("PART_NewGroupButton");
        var removedGroup = ((SimplifiedGroupChoice)host.Current.SelectedItem).Group!;
        host.Click("PART_NewGroupButton"); var survivorGroup = ((SimplifiedGroupChoice)host.Current.SelectedItem).Group!;
        SelectRemovalGroup(host, removedGroup); host.Click("PART_RemoveButton"); host.Page.ApplyChanges(); host.Layout();
        Assert.DoesNotContain(removedGroup, host.Tab.Groups); Assert.Contains(survivorGroup, host.Tab.Groups);
    });

    [Fact]
    public void Authored_structures_are_protected_and_selected_tab_removal_keeps_a_valid_tab() => Sta.Run(() =>
    {
        using var host = new Host(); SelectRemovalGroup(host, host.GroupA);
        Assert.False(host.Part<Button>("PART_RemoveButton").IsEnabled);
        host.Click("PART_RemoveButton"); Assert.Contains(host.GroupA, host.Tab.Groups);
        var protectedTab = RemovalTabAction(host); Assert.False(protectedTab.IsEnabled); InvokeRemovalTab(host, protectedTab);
        Assert.Contains(host.Tab, MoveDraftTabs(host));
        host.Click("PART_NewTabButton"); var custom = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        host.Page.ApplyChanges(); host.Ribbon.SelectedTab = custom; host.Layout();
        InvokeRemovalTab(host, RemovalTabAction(host)); host.Page.ApplyChanges(); host.Layout();
        Assert.DoesNotContain(custom, host.Ribbon.Tabs); Assert.Same(host.Tab, host.Ribbon.SelectedTab);
        Assert.Same(host.Tab, ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab);
    });

    private static void SelectRemovalGroup(Host host, RibbonGroup group) => host.Current.SelectedItem =
        host.Current.Items.OfType<SimplifiedGroupChoice>().First(choice => choice.Group == group);

    [Fact]
    public void A_move_anchored_to_a_removed_draft_tab_keeps_its_final_order() => Sta.Run(() =>
    {
        using var host = new Host(); var insert = AddMoveTab(host, "Insert", "insert", false);
        var moving = AddMoveTab(host, "Tools", "tools", true); host.Page.CancelChanges();
        SelectMoveTab(host, host.Tab); host.Click("PART_NewTabButton");
        var anchor = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        Assert.True(host.Page.StageTabMove(moving, anchor, false));
        SelectMoveTab(host, anchor); InvokeRemovalTab(host, RemovalTabAction(host));
        Assert.Equal(new[] { host.Tab, moving, insert }, MoveDraftTabs(host));
        host.Page.ApplyChanges(); host.Layout(); Assert.Equal(new[] { host.Tab, moving, insert }, host.Ribbon.Tabs);
    });

    [Fact]
    public void Deletions_synchronize_with_classic_settings_in_both_directions() => Sta.Run(() =>
    {
        using var host = new Host(); host.Click("PART_NewTabButton");
        var custom = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        host.SelectAvailable(host.First); host.Click("PART_AddButton"); host.Page.ApplyChanges(); host.Layout();
        SelectMoveTab(host, host.Tab); host.Click("PART_NewGroupButton");
        var group = ((SimplifiedGroupChoice)host.Current.SelectedItem).Group!;
        host.SelectAvailable(host.Second); host.Click("PART_AddButton"); host.Page.ApplyChanges(); host.Layout();
        var classic = new RibbonCustomizePage { Ribbon = host.Ribbon };
        Grid.SetRow(classic, 1);
        ((Grid)host.Window.Content).Children.Add(classic); host.Layout();
        SelectRemovalGroup(host, group); host.Click("PART_RemoveButton"); host.Page.ApplyChanges(); host.Layout();
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        var tree = (TreeView)classic.Template.FindName("PART_Tree", classic);
        var homeNode = tree.Items.OfType<RibbonCustomizeNode>().Single(node => node.Item == host.Tab);
        Assert.DoesNotContain(homeNode.Children, node => node.Item == group);
        var customNode = tree.Items.OfType<RibbonCustomizeNode>().Single(node => node.Item == custom);
        var row = Assert.IsType<TreeViewItem>(tree.ItemContainerGenerator.ContainerFromItem(customNode));
        row.IsSelected = true; host.Layout();
        Assert.Same(custom, Assert.IsType<RibbonCustomizeNode>(tree.SelectedItem).Item);
        Assert.True(classic.IsEnabled); Assert.True(customNode.IsCustom);
        Assert.True(((ButtonBase)classic.Template.FindName("PART_RemoveButton", classic)).IsEnabled);
        ((ButtonBase)classic.Template.FindName("PART_RemoveButton", classic)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); host.Layout();
        Assert.DoesNotContain(custom, host.Ribbon.Tabs);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        Assert.DoesNotContain(custom, MoveDraftTabs(host));
        host.Click("PART_NewTabButton"); var another = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        host.Page.ApplyChanges(); host.Layout(); InvokeRemovalTab(host, RemovalTabAction(host)); host.Page.ApplyChanges(); host.Layout();
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        Assert.DoesNotContain(tree.Items.OfType<RibbonCustomizeNode>(), node => node.Item == another);
        Assert.Same(host.First, host.GroupA.Items[0]);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Imported_structure_removals_and_profiles_survive_export_import_and_apply(bool removeTab) => Sta.Run(() =>
    {
        using var host = new Host(); host.Click("PART_NewTabButton");
        var custom = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        var group = ((SimplifiedGroupChoice)host.Current.SelectedItem).Group!;
        host.SelectAvailable(host.First); host.Click("PART_AddButton"); host.Page.ApplyChanges(); host.Layout();
        string opening = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        Assert.True(host.Page.ImportDraft(opening)); host.Layout();
        var projected = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        Assert.Equal(Ribbon.GetCommandId(custom), Ribbon.GetCommandId(projected));
        if (removeTab) InvokeRemovalTab(host, RemovalTabAction(host));
        else { host.Current.SelectedItem = host.Current.Items.OfType<SimplifiedGroupChoice>().First(); host.Click("PART_RemoveButton"); }
        string deleted = host.Page.ExportDraft();
        Assert.Equal(opening, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        host.Page.CancelChanges(); Assert.True(host.Page.ImportDraft(deleted)); host.Page.ApplyChanges(); host.Layout();
        if (removeTab) Assert.DoesNotContain(host.Ribbon.Tabs, tab => Ribbon.GetCommandId(tab) == Ribbon.GetCommandId(custom));
        else Assert.Empty(host.Ribbon.Tabs.Single(tab => Ribbon.GetCommandId(tab) == Ribbon.GetCommandId(custom)).Groups);
        Assert.DoesNotContain(JsonNode.Parse(RibbonCustomizationSerializer.Serialize(host.Ribbon))!["Simplified"]!["Tabs"]!.AsArray()
            .SelectMany(tab => tab!["Commands"]!.AsArray()), command => command!["GroupId"]?.GetValue<string>() == Ribbon.GetCommandId(group));
    });

    private static MenuItem RemovalTabAction(Host host)
    {
        host.Click("PART_EditButton");
        return Assert.Single(host.Part<Button>("PART_EditButton").ContextMenu.Items.OfType<MenuItem>(), item => Equals(item.Header, RibbonLocalization.GetString(RibbonString.RemoveTab)));
    }
    private static void InvokeRemovalTab(Host host, MenuItem action)
    {
        action.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        host.Part<Button>("PART_EditButton").ContextMenu.IsOpen = false; host.Layout();
    }
}
