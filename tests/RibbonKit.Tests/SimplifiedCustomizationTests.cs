using System.IO;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Localization;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed partial class SimplifiedCustomizationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Classic_only_or_unknown_sections_preserve_profiles_across_rebuilt_custom_tabs_and_groups(bool unknownVersion) => Sta.Run(() =>
    {
        using var host = new Host(); host.Click("PART_NewGroupButton");
        host.SelectAvailable(host.First); host.Click("PART_AddButton");
        Assert.IsType<SimplifiedCustomizeEntry>(host.Current.SelectedItem).ShowLabel = false;
        host.Click("PART_NewTabButton"); host.SelectAvailable(host.Second); host.Click("PART_AddButton"); host.Page.ApplyChanges(); host.Layout();
        var tab = host.Ribbon.Tabs.Last(); string tabId = Ribbon.GetCommandId(tab)!;
        var before = JsonNode.Parse(RibbonCustomizationSerializer.Serialize(host.Ribbon))!;
        var legacy = before.DeepClone(); legacy.AsObject().Remove("Simplified");
        if (unknownVersion) legacy["Simplified"] = JsonNode.Parse("{\"Version\":99,\"Tabs\":[]}");
        RibbonCustomizationSerializer.Apply(host.Ribbon, legacy.ToJsonString()); host.Layout();
        Assert.NotSame(tab, host.Ribbon.Tabs.Single(candidate => Ribbon.GetCommandId(candidate) == tabId));
        var after = JsonNode.Parse(RibbonCustomizationSerializer.Serialize(host.Ribbon))!;
        Assert.True(JsonNode.DeepEquals(before["Simplified"], after["Simplified"]));
        Assert.Equal(2, host.Ribbon.GetSimplifiedChoices(host.Tab).Count(choice => choice.Source == host.First));
        Assert.Contains(host.Ribbon.GetSimplifiedChoices(host.Tab), choice => choice.Source == host.First && choice.Size == RibbonControlSize.Small);
    });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Applying_membership_resolves_the_current_source_and_skips_removed_commands(bool replacementExists) => Sta.Run(() =>
    {
        using var host = new Host(); host.Click("PART_NewGroupButton");
        var group = ((SimplifiedGroupChoice)host.Current.SelectedItem).Group!;
        host.SelectAvailable(host.First); host.Click("PART_AddButton"); host.GroupA.Items.Remove(host.First);
        var replacement = new RibbonButton { Header = "Replacement copy" }; Ribbon.SetCommandId(replacement, "first");
        if (replacementExists) host.GroupA.Items.Add(replacement);
        host.Page.ApplyChanges(); host.Layout();
        if (replacementExists)
        {
            Assert.Same(replacement, Ribbon.GetQuickAccessSource(group.Items.OfType<FrameworkElement>().Single()));
            Assert.All(host.Ribbon.GetSimplifiedChoices(host.Tab).Where(choice => Ribbon.GetCommandId(choice.Source) == "first"), choice => Assert.Same(replacement, choice.Source));
        }
        else
        {
            Assert.Empty(group.Items);
            Assert.DoesNotContain(host.Ribbon.GetSimplifiedChoices(host.Tab), choice => choice.Source == host.First);
        }
    });

    [Fact]
    public void Structure_draft_survives_mode_and_template_changes_and_cancel_leaves_no_live_edits() => Sta.Run(() =>
    {
        using var host = new Host(); string before = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        host.Page.RenameDraft(host.Tab, "Renamed Home"); host.Page.RenameDraft(host.GroupA, "Renamed Clipboard");
        host.Click("PART_NewTabButton");
        var tab = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        host.Page.RenameDraft(tab, "Favorites"); host.Click("PART_NewGroupButton");
        var group = ((SimplifiedGroupChoice)host.Current.SelectedItem).Group!;
        host.Page.RenameDraft(group, "Editing favorites"); host.SelectAvailable(host.Second); host.Click("PART_AddButton");
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon)); Assert.Empty(group.Items);
        Assert.DoesNotContain(tab, host.Ribbon.Tabs);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        host.Page.Template = null; host.Layout(); host.Page.ClearValue(Control.TemplateProperty); host.Layout();
        Assert.Same(tab, ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab);
        Assert.Equal("Favorites", ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).DisplayName);
        Assert.Equal(new[] { host.Second }, host.Selected());
        host.Page.CancelChanges(); host.Layout();
        Assert.Single(host.Part<Selector>("PART_TabSelector").Items.Cast<object>());
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
    });

    [Fact]
    public void Custom_group_membership_and_group_specific_profiles_apply_invoke_and_round_trip_independently() => Sta.Run(() =>
    {
        using var host = new Host(); host.Ribbon.AddToQuickAccess(host.First);
        host.Click("PART_NewGroupButton");
        var group = ((SimplifiedGroupChoice)host.Current.SelectedItem).Group!;
        string groupId = Ribbon.GetCommandId(group)!; host.Page.RenameDraft(group, "Favorites");
        host.SelectAvailable(host.First); host.Click("PART_AddButton");
        Assert.IsType<SimplifiedCustomizeEntry>(host.Current.SelectedItem).ShowLabel = false;
        host.SelectAvailable(host.Second); host.Click("PART_AddButton"); host.Click("PART_UpButton");
        Assert.Empty(group.Items); Assert.Equal(new[] { host.First, host.Third, host.Second, host.First }, host.Selected());
        host.Page.ApplyChanges(); host.Layout();
        Assert.Contains(group, host.Tab.Groups); Assert.Equal(RibbonGroupLayout.Stacked, group.Layout);
        Assert.Equal(new[] { host.First, host.Second }, group.Items.OfType<FrameworkElement>().Select(Ribbon.GetQuickAccessSource));
        var choices = host.Ribbon.GetSimplifiedChoices(host.Tab);
        Assert.Equal(2, choices.Count(choice => choice.Source == host.First));
        Assert.Equal(RibbonControlSize.Medium, choices.Single(choice => choice.Source == host.First && choice.Group == host.GroupA).Size);
        Assert.Equal(RibbonControlSize.Small, choices.Single(choice => choice.Source == host.First && choice.Group == group).Size);
        int clicks = 0; host.First.Click += (_, _) => clicks++;
        var copies = host.ToolBar.Items.OfType<RibbonButton>().Where(copy => Ribbon.GetQuickAccessSource(copy) == host.First).ToArray();
        Assert.Equal(2, copies.Length); Assert.NotSame(copies[0], copies[1]);
        foreach (var copy in copies) copy.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        host.Layout();
        Assert.Equal(2, clicks);
        // Context actions on an overflow copy retain its group assignment.
        host.ToolBar.Width = 1; host.Layout(); host.ToolBar.OpenOverflow(); host.Layout();
        var customCopy = host.ToolBar.OverflowEntries.Single(copy => Ribbon.GetQuickAccessSource(copy) == host.First && copy.GetValue(Ribbon.SimplifiedCommandGroupProperty) == group);
        var remove = host.Ribbon.CreateRibbonContextMenu(customCopy).Items.OfType<MenuItem>().Single(item => Equals(item.Header, RibbonLocalization.GetString(RibbonString.RemoveFromSimplifiedRibbon)));
        remove.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); host.Layout();
        Assert.Single(host.Ribbon.GetSimplifiedChoices(host.Tab), choice => choice.Source == host.First);
        Assert.True(host.Ribbon.PinToSimplifiedRibbon((FrameworkElement)group.Items[0])); host.Layout();
        host.ToolBar.Width = double.NaN; host.Layout();
        string saved = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        Assert.Equal(2, JsonNode.Parse(saved)!["Simplified"]!["Version"]!.GetValue<int>());
        RibbonCustomizationSerializer.Apply(host.Ribbon, saved); host.Layout();
        var restored = host.Tab.Groups.Single(candidate => Ribbon.GetCommandId(candidate) == groupId);
        Assert.NotSame(group, restored); Assert.Equal("Favorites", restored.Header);
        Assert.Equal(2, host.Ribbon.GetSimplifiedChoices(host.Tab).Count(choice => choice.Source == host.First));
        Assert.Contains(host.Ribbon.GetSimplifiedChoices(host.Tab), choice => choice.Group == restored && choice.Source == host.Second);
        Assert.True(host.Ribbon.IsInQuickAccess(host.First));
        host.Page.CancelChanges(); host.Layout(); host.Click("PART_ResetButton"); host.Page.ApplyChanges(); host.Layout();
        Assert.Equal(new[] { host.First, host.Third }, host.Row()); Assert.Equal(2, restored.Items.Count);
        Assert.Equal(new object[] { host.First, host.Second }, host.GroupA.Items.Cast<object>());
    });

    [Fact]
    public void A_new_tab_accepts_whole_ribbon_commands_and_classic_editor_refreshes_after_apply() => Sta.Run(() =>
    {
        using var host = new Host();
        host.Ribbon.PinToSimplifiedRibbon(host.Second);
        var classic = new RibbonCustomizePage { Ribbon = host.Ribbon };
        ((Grid)host.Window.Content).Children.Add(classic); host.Layout();
        host.Click("PART_NewTabButton");
        var tab = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        host.Page.RenameDraft(tab, "My tools"); host.SelectAvailable(host.Second); host.Click("PART_AddButton");
        host.Page.ApplyChanges(); host.Layout();
        Assert.Equal(new[] { host.Tab, tab }, host.Ribbon.Tabs);
        Assert.Equal(new[] { host.Second }, host.Ribbon.GetSimplifiedChoices(tab).Select(choice => choice.Source));
        Assert.Same(host.Second, Ribbon.GetQuickAccessSource(tab.Groups.Single().Items.OfType<FrameworkElement>().Single()));
        var declaration = tab.Groups.Single().Items.OfType<FrameworkElement>().Single();
        Assert.True(host.Ribbon.RemoveFromSimplifiedRibbon(declaration));
        Assert.Empty(host.Ribbon.GetSimplifiedChoices(tab));
        Assert.Contains(host.Ribbon.GetSimplifiedChoices(host.Tab), choice => choice.Source == host.Second);
        Assert.True(host.Ribbon.PinToSimplifiedRibbon(declaration)); host.Layout();
        host.Ribbon.SelectedTab = tab; host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        Assert.True(classic.IsEnabled);
        var tree = (TreeView)classic.Template.FindName("PART_Tree", classic);
        Assert.Contains(tree.Items.Cast<RibbonCustomizeNode>(), node => node.Item == tab && node.Children.Single().Children.Count == 1);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        string saved = RibbonCustomizationSerializer.Serialize(host.Ribbon), id = Ribbon.GetCommandId(tab)!;
        RibbonCustomizationSerializer.Apply(host.Ribbon, saved); host.Layout();
        var restored = host.Ribbon.Tabs.Single(candidate => Ribbon.GetCommandId(candidate) == id);
        Assert.Equal("My tools", restored.Header); Assert.Same(restored.Groups.Single(), host.Ribbon.GetSimplifiedChoices(restored).Single().Group);
        Assert.Same(host.Second, host.Ribbon.GetSimplifiedChoices(restored).Single().Source);
    });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Options_OK_applies_new_structure_and_Cancel_discards_it(bool ok) => Sta.Run(() =>
    {
        using var host = new Host(); string before = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        var page = new RibbonSimplifiedCustomizePage { Ribbon = host.Ribbon };
        var dialog = new RibbonOptionsDialog { Width = 800, Height = 600, ShowActivated = false, ShowInTaskbar = false, Left = -10000, Top = -10000 };
        dialog.Pages.Add(new RibbonOptionsPage { Content = page, Header = "Customize Ribbon" }); dialog.Show(); Sta.Drain();
        ClickAction(page, "PART_NewTabButton"); Sta.Drain();
        var list = (ListBox)page.Template.FindName("PART_AvailableList", page);
        list.SelectedItem = list.Items.Cast<SimplifiedCustomizeEntry>().Single(entry => entry.Control == host.First);
        ((ButtonBase)page.Template.FindName("PART_AddButton", page)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        if (ok) ((ButtonBase)dialog.Template.FindName("PART_OkButton", dialog)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        else dialog.Close(); host.Layout();
        Assert.Equal(ok ? 2 : 1, host.Ribbon.Tabs.Count);
        if (!ok) Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        else Assert.Single(host.Ribbon.GetSimplifiedChoices(host.Ribbon.Tabs.Last()));
    });

    [Fact]
    public void Group_selection_limits_builtin_membership_and_rename_dialog_only_changes_the_draft() => Sta.Run(() =>
    {
        using var host = new Host();
        host.Current.SelectedItem = host.Current.Items.OfType<SimplifiedGroupChoice>().Single(entry => entry.Group == host.GroupA); host.Layout();
        host.SelectAvailable(host.Third); Assert.False(host.Part<ButtonBase>("PART_AddButton").IsEnabled);
        Assert.DoesNotContain(host.Available.Items.Cast<SimplifiedCustomizeEntry>(), entry => entry.Control is RibbonGroup);
        host.Page.FlowDirection = FlowDirection.RightToLeft;
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            var dialog = Application.Current.Windows.OfType<RibbonCustomizeEditDialog>().Single(); dialog.ApplyTemplate();
            ((TextBox)dialog.Template.FindName("PART_NameBox", dialog)).Text = "Draft Home";
            ((ButtonBase)dialog.Template.FindName("PART_OkButton", dialog)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }));
        host.Click("PART_RenameTabButton");
        Assert.Equal("Home", host.Tab.Header);
        Assert.Equal("Draft Home", ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).DisplayName);
        Save(host.Window, "structure-editor-rtl");
        host.Page.CancelChanges(); Assert.Equal("Home", host.Tab.Header);
    });

    [Fact]
    public void Version_one_migrates_and_unknown_group_ids_do_not_move_commands_to_another_group() => Sta.Run(() =>
    {
        using var host = new Host(); host.Ribbon.PinToSimplifiedRibbon(host.Second);
        var legacy = JsonNode.Parse(RibbonCustomizationSerializer.Serialize(host.Ribbon))!;
        legacy["Simplified"]!["Version"] = 1;
        foreach (var command in legacy["Simplified"]!["Tabs"]![0]!["Commands"]!.AsArray()) command!.AsObject().Remove("GroupId");
        host.Ribbon.ResetSimplifiedOverrides(); RibbonCustomizationSerializer.Apply(host.Ribbon, legacy.ToJsonString()); host.Layout();
        Assert.Equal(new[] { host.First, host.Third, host.Second }, host.Row());
        Assert.All(host.Ribbon.GetSimplifiedChoices(host.Tab), choice => Assert.NotNull(choice.Group));
        var current = JsonNode.Parse(RibbonCustomizationSerializer.Serialize(host.Ribbon))!;
        current["Simplified"]!["Tabs"]![0]!["Commands"]![0]!["GroupId"] = "missing";
        RibbonCustomizationSerializer.Apply(host.Ribbon, current.ToJsonString()); host.Layout();
        Assert.Equal(new[] { host.Third, host.Second }, host.Row());
        // Untagged built-in groups preserve the legacy source-only fallback.
        host.GroupA.ClearValue(Ribbon.CommandIdProperty);
        host.Ribbon.PinToSimplifiedRibbon(host.First);
        string saved = RibbonCustomizationSerializer.Serialize(host.Ribbon); host.Ribbon.ResetSimplifiedOverrides();
        RibbonCustomizationSerializer.Apply(host.Ribbon, saved); host.Layout(); Assert.Contains(host.First, host.Row());
    });

    [Theory]
    [InlineData("{\"Version\":2,\"Tabs\":null}")]
    [InlineData("{\"Version\":2,\"Tabs\":[{\"TabId\":\"home\"}]}")]
    [InlineData("{\"Version\":2,\"Tabs\":[{\"TabId\":\"home\",\"Commands\":[null]}]}")]
    [InlineData("{\"Version\":2,\"Tabs\":[{\"TabId\":\"home\",\"Commands\":[{\"SourceId\":\"first\",\"GroupId\":{},\"Size\":\"Medium\"}]}]}")]
    public void Malformed_version_two_leaves_all_live_structure_and_selections_unchanged(string section) => Sta.Run(() =>
    {
        using var host = new Host(); host.Ribbon.PinToSimplifiedRibbon(host.Second);
        string before = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        var json = JsonNode.Parse(before)!; json["Tabs"]![0]!["Header"] = "Must not apply"; json["Simplified"] = JsonNode.Parse(section);
        RibbonCustomizationSerializer.Apply(host.Ribbon, json.ToJsonString()); host.Layout();
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Grouped_and_selected_lists_scroll_by_pixels_and_reveal_the_last_command(FlowDirection direction) => Sta.Run(() =>
    {
        using var host = new Host(); host.Page.FlowDirection = direction; host.Window.Height = 430;
        foreach (var group in new[] { host.GroupA, host.GroupB })
        for (int i = 0; i < 16; i++)
        {
            var command = new RibbonButton { Header = group.Header + " " + i };
            Ribbon.SetSimplifiedPlacement(command, RibbonSimplifiedPlacement.Inline); group.Items.Add(command);
        }
        host.Page.CancelChanges(); host.Layout();
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            host.Ribbon.Density = density; Ribbon.SetDensity(host.Page, density); host.Layout();
            foreach (var list in new[] { host.Available, host.Current })
            {
                var viewer = (ScrollViewer)list.Template.FindName("PART_ScrollViewer", list);
                Assert.True(viewer.ScrollableHeight > 0, $"No scroll range: extent={viewer.ExtentHeight}, viewport={viewer.ViewportHeight}");
                viewer.ScrollToTop(); host.Layout();
                var first = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
                double before = first.TransformToAncestor(list).Transform(new Point()).Y;
                viewer.ScrollToVerticalOffset(7.5); host.Layout();
                double moved = before - first.TransformToAncestor(list).Transform(new Point()).Y;
                Assert.InRange(moved, 7, 8);
                Assert.InRange(viewer.VerticalOffset, 7, 8);
                viewer.ScrollToTop(); host.Layout(); viewer.LineDown(); host.Layout();
                Assert.InRange(viewer.VerticalOffset, 15, 17);
                list.SelectedIndex = list.Items.Count - 1; list.ScrollIntoView(list.SelectedItem); host.Layout();
                var last = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(list.Items.Count - 1);
                var bounds = last.TransformToAncestor(viewer).TransformBounds(new Rect(last.RenderSize));
                Assert.InRange(bounds.Top, 0, viewer.ViewportHeight);
                Assert.InRange(bounds.Bottom, 0, viewer.ViewportHeight + 1);
                Assert.Equal(list.Items.Count - 1, list.SelectedIndex);
                viewer.ScrollToTop(); host.Layout();
            }
        }
    });

    [Fact]
    public void Draft_order_labels_apply_cancel_and_classic_QAT_isolation() => Sta.Run(() =>
    {
        using var host = new Host();
        host.Ribbon.AddToQuickAccess(host.First);
        string before = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        host.SelectAvailable(host.Second); host.Click("PART_AddButton");
        host.Click("PART_UpButton"); host.Click("PART_UpButton");
        Assert.Equal(new[] { host.Second, host.First, host.Third }, host.Selected());
        var entry = host.Current.Items.OfType<SimplifiedCustomizeEntry>().First();
        entry.ShowLabel = false;
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        host.Page.ApplyChanges(); host.Layout();
        Assert.Equal(new[] { host.Second, host.First, host.Third }, host.Row());
        Assert.Equal(RibbonControlSize.Small, ((RibbonButton)host.ToolBar.Items.OfType<FrameworkElement>().Single(item => Ribbon.GetQuickAccessSource(item) == host.Second)).Size);
        Assert.Equal(RibbonControlSize.Medium, Ribbon.GetSimplifiedSize(host.Second));
        Assert.Equal(new[] { host.GroupA, host.GroupB }, host.Tab.Groups);
        Assert.True(host.Ribbon.IsInQuickAccess(host.First));
        Assert.Equal(1, host.ToolBar.Items.OfType<RibbonGroupSeparator>().Count(separator => !host.ToolBar.IsOverflowed(separator)));
        string applied = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        host.Current.SelectedItem = host.Current.Items.OfType<SimplifiedCustomizeEntry>().First(); host.Click("PART_RemoveButton");
        host.Page.CancelChanges(); host.Layout();
        Assert.Equal(applied, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        Assert.Equal(host.Row(), host.Selected());
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        Assert.False(host.Page.IsEnabled);
        Assert.Equal(new object[] { host.First, host.Second }, host.GroupA.Items.Cast<object>());
    });

    [Fact]
    public void Draft_survives_tab_mode_and_template_changes_and_reset_uses_authored_defaults() => Sta.Run(() =>
    {
        using var host = new Host();
        var other = new RibbonTab { Header = "Insert" };
        Ribbon.SetCommandId(other, "insert");
        var group = new RibbonGroup { Header = "Insert" };
        var command = new RibbonButton { Header = "Picture" };
        Ribbon.SetCommandId(command, "picture"); Ribbon.SetSimplifiedPlacement(command, RibbonSimplifiedPlacement.Inline);
        group.Items.Add(command); other.Groups.Add(group); host.Ribbon.Tabs.Add(other);
        host.Page.CancelChanges();
        host.SelectAvailable(host.Second); host.Click("PART_AddButton");
        var tabs = host.Part<Selector>("PART_TabSelector");
        tabs.SelectedItem = tabs.Items.Cast<SimplifiedTabChoice>().Single(entry => entry.Tab == other); host.Layout();
        host.Current.SelectedItem = host.Current.Items.OfType<SimplifiedCustomizeEntry>().First(); host.Click("PART_RemoveButton");
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        tabs.SelectedItem = tabs.Items.Cast<SimplifiedTabChoice>().Single(entry => entry.Tab == host.Tab); host.Layout();
        Assert.Contains(host.Second, host.Selected());
        host.Page.Template = null; host.Layout(); host.Page.ClearValue(Control.TemplateProperty); host.Layout();
        Assert.Contains(host.Second, host.Selected());
        host.Page.ApplyChanges(); host.Layout();
        Assert.Empty(host.Ribbon.GetSimplifiedChoices(other));
        host.Click("PART_ResetButton");
        Assert.Contains(host.Second, host.Row()); // Reset is still only a draft.
        host.Page.ApplyChanges(); host.Layout();
        Assert.Equal(new[] { host.First, host.Third }, host.Row());
        Assert.Empty(host.Ribbon.GetSimplifiedChoices(other));
        host.Click("PART_ResetAllButton"); host.Page.ApplyChanges(); host.Layout();
        Assert.Same(command, Assert.Single(host.Ribbon.GetSimplifiedChoices(other)).Source);
    });

    [Fact]
    public void Pin_unpin_resolves_copies_duplicates_and_overflow_native_sources() => Sta.Run(() =>
    {
        using var host = new Host();
        host.ToolBar.Width = 40; host.Layout(); host.ToolBar.OpenOverflow(); host.Layout();
        var overflow = host.ToolBar.OverflowEntries.Single(item => Ribbon.GetQuickAccessSource(item) == host.First);
        var menu = host.Ribbon.CreateRibbonContextMenu(overflow);
        var remove = menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, RibbonLocalization.GetString(RibbonString.RemoveFromSimplifiedRibbon)));
        remove.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); host.Layout();
        Assert.DoesNotContain(host.First, host.Row());
        var copy = host.Ribbon.CreateCommandProxy(host.Second, RibbonControlSize.Small);
        Assert.True(host.Ribbon.PinToSimplifiedRibbon(copy));
        Assert.False(host.Ribbon.PinToSimplifiedRibbon(host.Second));
        Assert.True(host.Ribbon.RemoveFromSimplifiedRibbon(copy));
        var duplicate = new RibbonButton { Header = "Duplicate" }; Ribbon.SetCommandId(duplicate, "first"); host.GroupA.Items.Add(duplicate);
        Assert.True(host.Ribbon.PinToSimplifiedRibbon(duplicate));
        Assert.Same(host.First, host.Ribbon.GetSimplifiedChoices(host.Tab).Last().Source);
        Assert.False(host.Ribbon.PinToSimplifiedRibbon(host.First));
        Assert.False(host.Ribbon.PinToSimplifiedRibbon(new RibbonTextBox()));
        Assert.False(host.Ribbon.PinToSimplifiedRibbon(host.GroupA));
        var nativeMenu = host.Ribbon.CreateRibbonContextMenu(host.Second);
        nativeMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, RibbonLocalization.GetString(RibbonString.PinToSimplifiedRibbon)))
            .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        host.Layout(); Assert.Contains(host.Second, host.Row());
        host.ToolBar.Width = double.NaN; host.Layout();
    });

    [Fact]
    public void Serializer_round_trip_reconciles_ids_order_empty_tabs_and_missing_commands() => Sta.Run(() =>
    {
        using var host = new Host();
        host.Ribbon.SetSimplifiedChoices(host.Tab, new[] { new SimplifiedCommandChoice(host.Third, RibbonControlSize.Small), new SimplifiedCommandChoice(host.Second, RibbonControlSize.Medium) });
        var json = JsonNode.Parse(RibbonCustomizationSerializer.Serialize(host.Ribbon))!.AsObject();
        var commands = json["Simplified"]!["Tabs"]![0]!["Commands"]!.AsArray();
        commands.Add(commands[0]!.DeepClone());
        commands.Add(new JsonObject { ["SourceId"] = "missing", ["Size"] = "Medium" });
        host.Ribbon.ResetSimplifiedOverrides();
        RibbonCustomizationSerializer.Apply(host.Ribbon, json.ToJsonString()); host.Layout();
        Assert.Equal(new[] { host.Third, host.Second }, host.Row());
        Assert.Equal(RibbonControlSize.Small, host.Ribbon.GetSimplifiedChoices(host.Tab)[0].Size);
        host.Ribbon.SetSimplifiedChoices(host.Tab, Array.Empty<SimplifiedCommandChoice>());
        string empty = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        host.Ribbon.ResetSimplifiedOverrides(); RibbonCustomizationSerializer.Apply(host.Ribbon, empty); host.Layout();
        Assert.Empty(host.Row()); Assert.True(host.ToolBar.HasOverflow);
        host.GroupB.Items.Remove(host.Third); host.Layout();
        RibbonCustomizationSerializer.Apply(host.Ribbon, json.ToJsonString()); host.Layout();
        Assert.Equal(new[] { host.Second }, host.Row());
    });

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"Version\":1}")]
    [InlineData("{\"Version\":\"one\",\"Tabs\":[]}")]
    [InlineData("{\"Version\":null,\"Tabs\":[]}")]
    [InlineData("{\"Version\":1,\"Tabs\":null}")]
    [InlineData("{\"Version\":1,\"Tabs\":[null]}")]
    [InlineData("{\"Version\":1,\"Tabs\":[{\"TabId\":\"home\"}]}")]
    [InlineData("{\"Version\":1,\"Tabs\":[{\"TabId\":\"home\",\"Commands\":null}]}")]
    [InlineData("{\"Version\":1,\"Tabs\":[{\"TabId\":\"home\",\"Commands\":[null]}]}")]
    [InlineData("{\"Version\":1,\"Tabs\":[{\"TabId\":\"home\",\"Commands\":[{\"SourceId\":\"first\",\"Size\":\"Large\"}]}]}")]
    public void Old_unknown_and_malformed_optional_sections_do_not_corrupt_choices(string section) => Sta.Run(() =>
    {
        using var host = new Host();
        host.Ribbon.PinToSimplifiedRibbon(host.Second);
        string before = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        var json = JsonNode.Parse(before)!.AsObject(); json.Remove("Simplified");
        RibbonCustomizationSerializer.Apply(host.Ribbon, json.ToJsonString()); host.Layout();
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        json["Simplified"] = JsonNode.Parse("{\"Version\":99,\"Tabs\":[]}");
        RibbonCustomizationSerializer.Apply(host.Ribbon, json.ToJsonString()); host.Layout();
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        json["Simplified"] = JsonNode.Parse(section);
        RibbonCustomizationSerializer.Apply(host.Ribbon, json.ToJsonString()); host.Layout();
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
    });

    [Fact]
    public void Transient_groups_and_tabs_are_excluded_and_runtime_choices_keep_working() => Sta.Run(() =>
    {
        using var host = new Host();
        var merged = new RibbonGroup { Header = "Merged" };
        var transient = new RibbonButton { Header = "Transient" };
        Ribbon.SetCommandId(transient, "transient");
        Ribbon.SetSimplifiedPlacement(transient, RibbonSimplifiedPlacement.Inline);
        merged.Items.Add(transient);
        var mergeSource = new RibbonMergeSource();
        mergeSource.Groups.Add(new RibbonGroupContribution { TargetTabId = "home", Group = merged });
        host.Ribbon.Merge(mergeSource);
        var contextual = new RibbonTab { Header = "Context", IsContextual = true };
        Ribbon.SetCommandId(contextual, "context"); host.Ribbon.Tabs.Add(contextual);
        host.Page.CancelChanges(); host.Layout();
        Assert.DoesNotContain(contextual, host.Part<Selector>("PART_TabSelector").Items.Cast<SimplifiedTabChoice>().Select(entry => entry.Tab));
        Assert.DoesNotContain(host.Available.Items.Cast<SimplifiedCustomizeEntry>(), entry => entry.Control == transient);
        Assert.False(host.Ribbon.PinToSimplifiedRibbon(transient));
        host.Ribbon.PinToSimplifiedRibbon(host.Second); host.Layout();
        Assert.Contains(transient, host.Row());
        var saved = JsonNode.Parse(RibbonCustomizationSerializer.Serialize(host.Ribbon))!["Simplified"]!.ToJsonString();
        Assert.DoesNotContain("transient", saved); Assert.DoesNotContain("context", saved);
        host.Ribbon.Unmerge(mergeSource); host.Layout(); Assert.DoesNotContain(transient, host.Row());
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Options_OK_commits_Cancel_close_discards_and_mode_switch_selects_counterpart(bool ok) => Sta.Run(() =>
    {
        using var host = new Host();
        var page = new RibbonSimplifiedCustomizePage { Ribbon = host.Ribbon };
        var classic = new RibbonOptionsPage { Header = "Classic", Content = new RibbonCustomizePage { Ribbon = host.Ribbon } };
        var simplified = new RibbonOptionsPage { Header = "Simplified", Content = page };
        var dialog = new RibbonOptionsDialog { ShowActivated = false, ShowInTaskbar = false, Left = -10000, Top = -10000 };
        dialog.Pages.Add(classic); dialog.Pages.Add(simplified); dialog.SelectedPage = simplified;
        dialog.Show(); Sta.Drain(); dialog.UpdateLayout();
        Assert.Equal(Visibility.Collapsed, classic.Visibility); Assert.Equal(Visibility.Visible, simplified.Visibility);
        var list = (ListBox)page.Template.FindName("PART_AvailableList", page);
        list.SelectedItem = list.Items.Cast<SimplifiedCustomizeEntry>().Single(entry => entry.Control == host.Second);
        ((ButtonBase)page.Template.FindName("PART_AddButton", page)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; Sta.Drain(); Assert.Same(classic, dialog.SelectedPage);
        Assert.Equal(Visibility.Visible, classic.Visibility); Assert.Equal(Visibility.Collapsed, simplified.Visibility);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; Sta.Drain(); Assert.Same(simplified, dialog.SelectedPage);
        Assert.Equal(Visibility.Collapsed, classic.Visibility); Assert.Equal(Visibility.Visible, simplified.Visibility);
        bool observedSaved = false; dialog.Applied += (_, _) => observedSaved = host.Ribbon.GetSimplifiedChoices(host.Tab).Any(choice => choice.Source == host.Second);
        ((ButtonBase)dialog.Template.FindName(ok ? "PART_OkButton" : "PART_CancelButton", dialog)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        host.Layout(); Assert.Equal(ok, host.Row().Contains(host.Second)); Assert.Equal(ok, observedSaved);
    });

    [Fact]
    public void Opening_the_Showcase_dialog_selects_simplified_and_keeps_classic_disabled() => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(showcaseResources: true), RibbonTheme.Office2024);
        var main = new RibbonKit.Showcase.MainWindow { Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            typeof(RibbonKit.Showcase.MainWindow).GetField("_restoringAppearance", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(main, true);
            main.Loaded -= (RoutedEventHandler)typeof(RibbonKit.Showcase.MainWindow).GetMethod("OnWindowLoaded", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.CreateDelegate(typeof(RoutedEventHandler), main);
            main.Show(); Sta.Drain();
            main.MainRibbon.LayoutMode = RibbonLayoutMode.Simplified;
            var dialog = main.CreateCustomizationDialog(false);
            Assert.IsType<RibbonSimplifiedCustomizePage>(dialog.SelectedPage!.Content);
            Assert.Equal(RibbonLocalization.GetString(RibbonString.CustomizeRibbonPage), dialog.SelectedPage.Header);
            Assert.Equal(Visibility.Collapsed, dialog.Pages.Single(page => page.Content is RibbonCustomizePage).Visibility);
            Assert.False(dialog.Pages.Single(page => page.Content is RibbonCustomizePage).IsEnabled);
            dialog.Show(); Sta.Drain(); dialog.UpdateLayout();
            Save(dialog, "showcase-customize-redesign");
            dialog.Close();
        }
        finally { main.Close(); Sta.ResetApplication(); }
    });

    [Fact]
    public void Page_render_and_app_disabled_state_remain_bounded() => Sta.Run(() =>
    {
        using var host = new Host();
        var combo = host.Part<RibbonComboBox>("PART_TabSelector");
        Assert.Same(host.Tab, Assert.IsType<SimplifiedTabChoice>(combo.SelectedItem).Tab);
        Assert.Contains(Descendants(combo).OfType<TextBlock>(), text => text.Text == "Home");
        Assert.Equal("Copy", System.Windows.Automation.AutomationProperties.GetName((DependencyObject)host.Current.ItemContainerGenerator.ContainerFromItem(host.Current.Items.OfType<SimplifiedCustomizeEntry>().First())));
        Save(host.Window, "simplified-customize-office2024");
        host.Page.FlowDirection = FlowDirection.RightToLeft;
        ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.CrystalLight); host.Layout();
        Save(host.Window, "simplified-customize-crystal-rtl");
        host.Page.IsEnabled = false;
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout(); Assert.False(host.Page.IsEnabled);
    });

    [Fact]
    public void Reordering_across_groups_uses_contiguous_dividers_even_after_resizing() => Sta.Run(() =>
    {
        using var host = new Host();
        host.Ribbon.SetSimplifiedChoices(host.Tab, new[] { new SimplifiedCommandChoice(host.First, RibbonControlSize.Medium),
            new SimplifiedCommandChoice(host.Third, RibbonControlSize.Medium), new SimplifiedCommandChoice(host.Second, RibbonControlSize.Medium) });
        host.Layout();
        Assert.Equal(new[] { host.First, host.Third, host.Second }, host.Row());
        Assert.Equal(2, host.ToolBar.Items.OfType<RibbonGroupSeparator>().Count(separator => !host.ToolBar.IsOverflowed(separator)));
        host.ToolBar.Width = 45; host.Layout();
        host.ToolBar.Width = double.NaN; host.Layout();
        Assert.Equal(2, host.ToolBar.Items.OfType<RibbonGroupSeparator>().Count(separator => !host.ToolBar.IsOverflowed(separator)));
    });

    [Fact]
    public void Page_actions_follow_a_live_localization_provider() => Sta.Run(() =>
    {
        using var host = new Host();
        IRibbonLocalizationProvider? previous = RibbonLocalization.Provider;
        try
        {
            RibbonLocalization.Provider = new PageProvider(); host.Layout();
            Assert.Equal("localized:Reset", System.Windows.Automation.AutomationProperties.GetName(host.Part<Button>("PART_ResetMenuButton")));
            Assert.Equal("localized:Apply", host.Part<Button>("PART_ApplyButton").Content);
            Assert.Equal("localized:New", System.Windows.Automation.AutomationProperties.GetName(host.Part<Button>("PART_NewButton")));
            Assert.Equal("localized:AllTabs", host.Part<Selector>("PART_SourceTabSelector").Items.Cast<SimplifiedTabChoice>().First().DisplayName);
            host.Tab.Header = "Changed by host"; host.GroupA.Header = "Host group"; host.Layout();
            Assert.Equal("Changed by host", ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).DisplayName);
            Assert.Equal("Host group", host.Current.Items.OfType<SimplifiedGroupChoice>().Single(entry => entry.Group == host.GroupA).DisplayName);
            Assert.Empty(Descendants(host.Page).OfType<RibbonCheckBox>());
            host.Current.SelectedItem = host.Current.Items.OfType<SimplifiedCustomizeEntry>().First();
            host.Click("PART_EditButton");
            Assert.Contains(host.Part<Button>("PART_EditButton").ContextMenu.Items.OfType<MenuItem>(), item => Equals(item.Header, "localized:ShowItemLabel"));
        }
        finally { RibbonLocalization.Provider = previous; }
    });

    private sealed class PageProvider : IRibbonLocalizationProvider
    {
        public string? GetString(RibbonString key, System.Globalization.CultureInfo culture) => "localized:" + key;
    }

    [Fact]
    public void Custom_tab_copy_pins_to_active_tab_and_round_trips_without_changing_source_defaults() => Sta.Run(() =>
    {
        using var host = new Host();
        var custom = new RibbonTab { Header = "Favorites" }; Ribbon.SetCommandId(custom, "custom-tab"); Ribbon.SetIsCustom(custom, true);
        var group = new RibbonGroup { Header = "Favorites" }; Ribbon.SetCommandId(group, "custom-group"); Ribbon.SetIsCustom(group, true);
        var copy = host.Ribbon.CreateCommandProxy(host.First, RibbonControlSize.Medium);
        ((RibbonButton)copy).Header = "Copy favorite";
        group.Items.Add(copy); custom.Groups.Add(group); host.Ribbon.Tabs.Add(custom); host.Ribbon.SelectedTab = custom; host.Layout();
        Assert.Empty(host.Ribbon.GetSimplifiedChoices(custom));
        Assert.True(host.Ribbon.PinToSimplifiedRibbon(copy)); host.Layout();
        Assert.Equal("Copy favorite", ((RibbonGroupsHost)custom.Content).SimplifiedToolBar!.Items.OfType<RibbonButton>().Single().Header);
        var toolbar = ((RibbonGroupsHost)custom.Content).SimplifiedToolBar!;
        toolbar.Width = 1; host.Layout(); toolbar.OpenOverflow(); host.Layout();
        Assert.Equal("Copy favorite", toolbar.OverflowEntries.OfType<RibbonButton>().Single().Header);
        toolbar.CloseOverflow(); toolbar.Width = double.NaN; host.Layout();
        Assert.Same(host.First, Assert.Single(host.Ribbon.GetSimplifiedChoices(custom)).Source);
        group.IsEnabled = false; host.Layout();
        Assert.False(((RibbonGroupsHost)custom.Content).SimplifiedToolBar!.Items.OfType<RibbonButton>().Single().IsEnabled);
        group.IsEnabled = true; host.Layout();
        Assert.True(((RibbonGroupsHost)custom.Content).SimplifiedToolBar!.Items.OfType<RibbonButton>().Single().IsEnabled);
        host.First.IsEnabled = false; host.Layout();
        Assert.False(((RibbonGroupsHost)custom.Content).SimplifiedToolBar!.Items.OfType<RibbonButton>().Single().IsEnabled);
        host.First.IsEnabled = true; host.Layout();
        string saved = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        RibbonCustomizationSerializer.Apply(host.Ribbon, saved); host.Layout();
        var restored = host.Ribbon.Tabs.Single(tab => Ribbon.GetCommandId(tab) == "custom-tab");
        Assert.NotSame(custom, restored);
        Assert.Same(host.First, Assert.Single(host.Ribbon.GetSimplifiedChoices(restored)).Source);
        host.Ribbon.SelectedTab = restored; host.Layout();
        Assert.True(host.Ribbon.RemoveFromSimplifiedRibbon((FrameworkElement)restored.Groups[0].Items[0]));
        Assert.Empty(host.Ribbon.GetSimplifiedChoices(restored));
        Assert.Equal(new[] { host.First, host.Third }, host.Ribbon.GetSimplifiedChoices(host.Tab).Select(choice => choice.Source));
    });

    [Fact]
    public void One_context_entry_routes_to_the_active_customize_page() => Sta.Run(() =>
    {
        using var host = new Host();
        int classic = 0, simplified = 0;
        host.Ribbon.RibbonCustomizeRequested += (_, _) => classic++;
        host.Ribbon.SimplifiedCustomizeRequested += (_, _) => simplified++;
        foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
        {
            host.Ribbon.LayoutMode = mode; host.Layout();
            var menu = host.Ribbon.CreateRibbonContextMenu(host.First);
            var entry = Assert.Single(menu.Items.OfType<MenuItem>(), item => Equals(item.Header, RibbonLocalization.GetString(RibbonString.CustomizeRibbon)));
            Assert.True(entry.IsEnabled);
            entry.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }
        Assert.Equal(1, classic); Assert.Equal(1, simplified);
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Tab_selector_fills_its_column_and_command_labels_fit_resizing(FlowDirection direction) => Sta.Run(() =>
    {
        using var host = new Host(); host.Page.FlowDirection = direction;
        foreach (double width in new[] { 660d, 1000d, 760d })
        {
            host.Window.Width = width; host.Layout();
            var selector = host.Part<RibbonComboBox>("PART_TabSelector");
            var input = (FrameworkElement)selector.Template.FindName("InputBox", selector);
            Assert.InRange(Math.Abs(selector.ActualWidth - host.Current.ActualWidth), 0, 1);
            Assert.InRange(Math.Abs(input.ActualWidth - selector.ActualWidth), 0, 1);
            Assert.Equal(Visibility.Collapsed, ((TextBlock)selector.Template.FindName("HeaderText", selector)).Visibility);
            Assert.DoesNotContain(Descendants(host.Page).OfType<TextBlock>(), text => text.Text == RibbonLocalization.GetString(RibbonString.SimplifiedTab));
        }
        Save(host.Window, "customize-ribbon-responsive-" + direction);
    });

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static void Save(FrameworkElement element, string name)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "TestResults", "simplified-customization");
        Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        var drawing = new DrawingVisual();
        using (var context = drawing.RenderOpen())
        {
            var bounds = new Rect(0, 0, element.ActualWidth, element.ActualHeight);
            context.DrawRectangle(Brushes.White, null, bounds);
            context.DrawRectangle(new VisualBrush(element) { ViewboxUnits = BrushMappingMode.Absolute, Viewbox = bounds }, null, bounds);
        }
        bitmap.Render(drawing); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }

    private static void ClickAction(RibbonSimplifiedCustomizePage page, string name)
    {
        var action = name switch
        {
            "PART_NewTabButton" => ("PART_NewButton", RibbonString.NewTab),
            "PART_NewGroupButton" => ("PART_NewButton", RibbonString.NewGroup),
            "PART_RenameTabButton" => ("PART_EditButton", RibbonString.RenameTab),
            "PART_RenameGroupButton" => ("PART_EditButton", RibbonString.RenameGroup),
            "PART_ResetButton" => ("PART_ResetMenuButton", RibbonString.ResetTab),
            "PART_ResetAllButton" => ("PART_ResetMenuButton", RibbonString.ResetAllTabs),
            _ => (name, (RibbonString?)null),
        };
        var button = (ButtonBase)page.Template.FindName(action.Item1, page);
        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (action.Item2 is not { } key) return;
        var menu = button.ContextMenu;
        var item = menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, RibbonLocalization.GetString(key)));
        Assert.True(item.IsEnabled); item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); menu.IsOpen = false;
    }

    private sealed class Host : IDisposable
    {
        internal readonly Ribbon Ribbon = new() { LayoutMode = RibbonLayoutMode.Simplified };
        internal readonly RibbonTab Tab = new() { Header = "Home" };
        internal readonly RibbonGroup GroupA = new() { Header = "Clipboard" };
        internal readonly RibbonGroup GroupB = new() { Header = "Font" };
        internal readonly RibbonButton First = new() { Header = "Copy" };
        internal readonly RibbonButton Second = new() { Header = "Paste" };
        internal readonly RibbonButton Third = new() { Header = "Font" };
        internal readonly RibbonSimplifiedCustomizePage Page = new();
        internal readonly RibbonWindow Window;
        internal RibbonQuickAccessToolBar ToolBar => ((RibbonGroupsHost)Tab.Content).SimplifiedToolBar!;
        internal ListBox Available => Part<ListBox>("PART_AvailableList");
        internal ListBox Current => Part<ListBox>("PART_CurrentList");
        internal Host()
        {
            ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024); RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            Ribbon.SetCommandId(Tab, "home"); Ribbon.SetCommandId(GroupA, "clipboard"); Ribbon.SetCommandId(GroupB, "font");
            Ribbon.SetCommandId(First, "first"); Ribbon.SetCommandId(Second, "second"); Ribbon.SetCommandId(Third, "third");
            Ribbon.SetSimplifiedPlacement(First, RibbonSimplifiedPlacement.Inline); Ribbon.SetSimplifiedPlacement(Third, RibbonSimplifiedPlacement.Inline);
            GroupA.Items.Add(First); GroupA.Items.Add(Second); GroupB.Items.Add(Third); Tab.Groups.Add(GroupA); Tab.Groups.Add(GroupB); Ribbon.Tabs.Add(Tab);
            Page.Ribbon = Ribbon; Page.Margin = new Thickness(20);
            var grid = new Grid(); grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); grid.RowDefinitions.Add(new RowDefinition());
            grid.Children.Add(Ribbon); Grid.SetRow(Page, 1); grid.Children.Add(Page);
            Window = new RibbonWindow { Content = grid, Width = 1000, Height = 600, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            Window.Show(); Layout();
        }
        internal T Part<T>(string name) where T : DependencyObject => (T)Page.Template.FindName(name, Page);
        internal void Click(string name) { ClickAction(Page, name); Layout(); }
        internal void SelectAvailable(FrameworkElement control) => Available.SelectedItem = Available.Items.Cast<SimplifiedCustomizeEntry>().Single(entry => entry.Control == control);
        internal FrameworkElement[] Selected() => Current.Items.OfType<SimplifiedCustomizeEntry>().Select(entry => entry.Control).ToArray();
        internal FrameworkElement[] Row() => ToolBar.Items.OfType<FrameworkElement>().Where(item => item is not RibbonGroupQuickAccessProxy and not RibbonGroupSeparator).Select(item => Ribbon.GetQuickAccessSource(item)!).ToArray();
        internal void Layout() { Sta.Drain(); Window.UpdateLayout(); Sta.Drain(); Window.UpdateLayout(); }
        public void Dispose() { Window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle; Sta.ResetApplication(); }
    }
}
