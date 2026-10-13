using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Text.Json.Nodes;
using RibbonKit.Controls;
using RibbonKit.Localization;
using RibbonKit.Showcase;
using Xunit;

namespace RibbonKit.Tests;

public sealed partial class SimplifiedCustomizationTests
{
    [Fact]
    public void Review_qat_only_catalog_survives_removal_and_adds_persistent_custom_membership() => Sta.Run(() =>
    {
        using var host = new Host();
        var undo = new RibbonButton { Header = "Undo" }; Ribbon.SetCommandId(undo, "undo-only");
        host.Ribbon.QuickAccessItems.Add(undo); host.Ribbon.AddToQuickAccess(host.First);
        host.Ribbon.QuickAccessItems.Clear(); host.Page.CancelChanges(); host.Layout();
        var sources = host.Part<Selector>("PART_SourceTabSelector");
        sources.SelectedItem = sources.Items.Cast<SimplifiedTabChoice>().Single(choice => choice.CommandsNotInRibbon);
        Assert.Same(undo, Assert.Single(host.Available.Items.Cast<SimplifiedCustomizeEntry>()).Control);
        host.Click("PART_NewTabButton"); var custom = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        host.SelectAvailable(undo); host.Click("PART_AddButton");
        Assert.Same(undo, Assert.Single(host.Selected())); Assert.Empty(custom.Groups[0].Items);
        string draft = host.Page.ExportDraft(); host.Page.CancelChanges(); Assert.Single(host.Ribbon.Tabs);
        Assert.True(host.Page.ImportDraft(draft)); host.Page.ApplyChanges(); host.Layout();
        custom = host.Ribbon.Tabs.Single(Ribbon.GetIsCustom);
        Assert.Same(undo, Ribbon.GetQuickAccessSource(Assert.IsType<RibbonButton>(custom.Groups[0].Items[0])));
        Assert.Same(undo, Assert.Single(host.Ribbon.GetSimplifiedChoices(custom)).Source);
        Assert.Empty(host.Ribbon.QuickAccessItems);
        Assert.DoesNotContain(sources.Items.Cast<SimplifiedTabChoice>(), choice => choice.Tab == custom);
    });

    [Fact]
    public void Review_normal_additions_seed_hidden_labels_without_overwriting_saved_preferences() => Sta.Run(() =>
    {
        using var host = new Host(); host.Ribbon.LayoutMode = RibbonLayoutMode.Classic;
        var classic = new RibbonCustomizePage { Ribbon = host.Ribbon };
        var grid = (Grid)host.Window.Content; grid.Children.Remove(host.Page); Grid.SetRow(classic, 1); grid.Children.Add(classic); host.Layout();
        void Click(string part) { ((ButtonBase)classic.Template.FindName(part, classic)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); host.Layout(); }
        Click("PART_NewTabButton"); var tab = host.Ribbon.Tabs.Single(Ribbon.GetIsCustom); var group = tab.Groups.Single();
        Assert.Equal(RibbonGroupLayout.Stacked, group.Layout); Assert.NotNull(group.Icon);
        var tree = (TreeView)classic.Template.FindName("PART_Tree", classic);
        var tabNode = tree.Items.Cast<RibbonCustomizeNode>().Single(node => node.Item == tab);
        tabNode.Children.Single().IsSelected = true; host.Layout();
        var list = (ListBox)classic.Template.FindName("PART_AvailableList", classic);
        list.SelectedItem = list.Items.Cast<RibbonCommandEntry>().Single(entry => entry.Control == host.First); Click("PART_AddButton");
        Assert.Equal(RibbonControlSize.Small, Assert.Single(host.Ribbon.GetSimplifiedChoices(tab)).Size);
        host.Ribbon.SetSimplifiedChoices(tab, new[] { new SimplifiedCommandChoice(host.First, RibbonControlSize.Medium, group) });
        group.Layout = RibbonGroupLayout.Large;
        list.SelectedItem = list.Items.Cast<RibbonCommandEntry>().Single(entry => entry.Control == host.Second); Click("PART_AddButton");
        Assert.Equal(RibbonGroupLayout.Large, group.Layout);
        Assert.All(group.Items.Cast<RibbonButton>(), command => Assert.Equal(RibbonControlSize.Large, command.Size));
        Assert.Equal(new[] { RibbonControlSize.Medium, RibbonControlSize.Small }, host.Ribbon.GetSimplifiedChoices(tab).Select(choice => choice.Size));
        grid.Children.Remove(classic); grid.Children.Add(host.Page); host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        host.Ribbon.SelectedTab = tab; host.Page.CancelChanges(); host.Layout();
        Assert.DoesNotContain(host.Part<Selector>("PART_SourceTabSelector").Items.Cast<SimplifiedTabChoice>(), choice => choice.Tab == tab);
        Assert.Contains(host.Part<Selector>("PART_TabSelector").Items.Cast<SimplifiedTabChoice>(), choice => choice.Tab == tab);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Review_group_metadata_is_drafted_exported_imported_and_applied_without_changing_labels(bool import) => Sta.Run(() =>
    {
        using var host = new Host(); host.Click("PART_NewGroupButton");
        var group = Assert.IsType<SimplifiedGroupChoice>(host.Current.SelectedItem).Group!;
        Assert.NotNull(group.Icon); host.SelectAvailable(host.First); host.Click("PART_AddButton");
        host.Page.ApplyChanges(); host.Layout(); string id = Ribbon.GetCommandId(group)!;
        string original = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        host.Page.ApplyEditDraft(group, new RibbonCustomizeEditDialog { ItemName = "Tools", CanEditIcon = true, CanEditLayout = true,
            SelectedIcon = null, SelectedLayout = RibbonGroupLayout.Large });
        Assert.Equal(original, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        string edited = host.Page.ExportDraft(); var data = JsonNode.Parse(edited)!["Tabs"]![0]!["Groups"]!.AsArray().Single(data => data!["Id"]!.GetValue<string>() == id)!;
        Assert.Equal("Large", data["Layout"]!.GetValue<string>()); Assert.Equal("Large", data["Commands"]![0]!["Size"]!.GetValue<string>());
        Assert.Null(data["IconCommandId"]);
        host.Page.CancelChanges(); Assert.Equal(original, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        if (import) Assert.True(host.Page.ImportDraft(edited));
        else host.Page.ApplyEditDraft(group, new RibbonCustomizeEditDialog { ItemName = "Tools", CanEditIcon = true, CanEditLayout = true,
            SelectedIcon = null, SelectedLayout = RibbonGroupLayout.Large });
        host.Page.ApplyChanges(); host.Layout(); group = host.Tab.Groups.Single(group => Ribbon.GetCommandId(group) == id);
        Assert.Equal("Tools", group.Header); Assert.Null(group.Icon); Assert.Equal(RibbonGroupLayout.Large, group.Layout);
        Assert.Equal(RibbonControlSize.Large, Assert.IsType<RibbonButton>(group.Items[0]).Size);
        Assert.Equal(RibbonControlSize.Medium, host.Ribbon.GetSimplifiedChoices(host.Tab).Single(choice => choice.Group == group).Size);
        host.Page.ApplyEditDraft(group, new RibbonCustomizeEditDialog { CanEditIcon = true, CanEditLayout = true,
            SelectedIcon = RibbonCommandCatalog.DefaultGroupIcon(host.Ribbon), SelectedLayout = RibbonGroupLayout.Stacked });
        string reset = host.Page.ExportDraft(); host.Page.CancelChanges(); Assert.True(host.Page.ImportDraft(reset)); host.Page.ApplyChanges();
        group = host.Tab.Groups.Single(group => Ribbon.GetCommandId(group) == id);
        Assert.Equal(RibbonGroupLayout.Stacked, group.Layout); Assert.NotNull(group.Icon);
        Assert.Equal(RibbonControlSize.Medium, Assert.IsType<RibbonButton>(group.Items[0]).Size);
    });

    [Fact]
    public void Review_group_edit_opens_the_full_shared_dialog_with_default_icon_and_stacked_layout() => Sta.Run(() =>
    {
        using var host = new Host(); host.Click("PART_NewGroupButton");
        var group = Assert.IsType<SimplifiedGroupChoice>(host.Current.SelectedItem).Group!;
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            var dialog = Application.Current.Windows.OfType<RibbonCustomizeEditDialog>().Single();
            try
            {
                Assert.Same(host.Window, dialog.Owner); Assert.True(dialog.CanEditIcon); Assert.True(dialog.CanEditLayout);
                Assert.NotNull(group.Icon); Assert.Same(group.Icon, dialog.SelectedIcon); Assert.Equal(RibbonGroupLayout.Stacked, dialog.SelectedLayout);
                Assert.Equal(Visibility.Visible, ((FrameworkElement)dialog.Template.FindName("PART_IconSection", dialog)).Visibility);
                dialog.UpdateLayout(); Save(dialog, "review-group-edit");
                ((TextBox)dialog.Template.FindName("PART_NameBox", dialog)).Text = "Favorites";
                ((ButtonBase)dialog.Template.FindName("PART_OkButton", dialog)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
            finally { if (dialog.IsVisible) dialog.Close(); }
        }));
        host.Click("PART_RenameGroupButton"); Assert.NotEqual("Favorites", group.Header);
        host.Page.ApplyChanges(); Assert.Equal("Favorites", group.Header);
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Review_footer_balances_columns_and_keeps_icon_actions_accessible(FlowDirection direction) => Sta.Run(() =>
    {
        using var host = new Host(); host.Page.FlowDirection = direction; host.Layout();
        var reset = host.Part<Button>("PART_ResetMenuButton"); var apply = host.Part<Button>("PART_ApplyButton");
        Assert.Equal(reset.TranslatePoint(new Point(), host.Page).Y, apply.TranslatePoint(new Point(), host.Page).Y, 1);
        foreach (var name in new[] { "PART_ImportButton", "PART_ExportButton" })
        {
            var button = host.Part<Button>(name); Assert.IsType<TextBlock>(button.Content);
            Assert.False(string.IsNullOrWhiteSpace(System.Windows.Automation.AutomationProperties.GetName(button)));
            Assert.NotNull(button.ToolTip);
            Assert.True(button.ActualWidth < reset.ActualWidth);
        }
        Save(host.Window, "review-footer-" + direction);
    });

    [Fact]
    public void Review_showcase_authored_tabs_have_simplified_defaults_including_transient_modes() => Sta.Run(() =>
    {
        Sta.UseApplication(showcaseResources: true);
        var window = new MainWindow();
        try
        {
            foreach (var tab in window.MainRibbon.Tabs.Where(tab => !Ribbon.GetIsCustom(tab)))
                Assert.Contains(Ribbon.SimplifiedSources(tab), source => Ribbon.GetSimplifiedPlacement(source.Declaration) == RibbonSimplifiedPlacement.Inline);
        }
        finally { window.Close(); Sta.ResetApplication(); }
    });
}
