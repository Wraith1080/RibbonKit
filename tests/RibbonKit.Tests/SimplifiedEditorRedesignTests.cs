using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Reflection;
using RibbonKit.Controls;
using RibbonKit.Localization;
using Xunit;

namespace RibbonKit.Tests;

public sealed partial class SimplifiedCustomizationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Redesign_import_drops_transient_and_missing_custom_membership_from_export_and_apply(bool modal) => Sta.Run(() =>
    {
        using var host = new Host();
        var transient = new RibbonTab { Header = "Transient", IsContextual = !modal, IsModal = modal };
        var transientGroup = new RibbonGroup { Header = "Transient" }; var command = new RibbonButton { Header = "Transient command" };
        Ribbon.SetCommandId(transient, "transient-tab"); Ribbon.SetCommandId(command, "transient-command");
        transientGroup.Items.Add(command); transient.Groups.Add(transientGroup); host.Ribbon.Tabs.Add(transient);
        var document = JsonNode.Parse(RibbonCustomizationSerializer.Serialize(host.Ribbon))!;
        document["Tabs"]![0]!["Groups"]!.AsArray().Add(JsonNode.Parse("{\"Id\":\"custom-import\",\"IsCustom\":true,\"Header\":\"Imported\",\"Commands\":[{\"SourceId\":\"transient-command\"},{\"SourceId\":\"missing-command\"},{\"SourceId\":\"first\",\"Header\":\"My copy\",\"Size\":\"Small\"}]}"));
        Assert.True(host.Page.ImportDraft(document.ToJsonString()));
        string draft = host.Page.ExportDraft(); Assert.DoesNotContain("transient-command", draft); Assert.DoesNotContain("missing-command", draft);
        Assert.Contains("My copy", draft); host.Page.ApplyChanges(); host.Layout();
        var custom = host.Tab.Groups.Single(group => Ribbon.GetCommandId(group) == "custom-import");
        var copy = Assert.IsType<RibbonButton>(Assert.Single(custom.Items.Cast<object>()));
        Assert.Same(host.First, Ribbon.GetQuickAccessSource(copy)); Assert.Equal("My copy", copy.Header); Assert.Equal(RibbonControlSize.Small, copy.Size);
        Assert.Same(command, Assert.Single(transientGroup.Items.Cast<object>()));
    });

    [Fact]
    public void Redesign_group_ids_are_scoped_to_tabs_when_validating_imports() => Sta.Run(() =>
    {
        using var host = new Host(); var tab = new RibbonTab { Header = "Insert" }; var group = new RibbonGroup { Header = "Other font" };
        Ribbon.SetCommandId(tab, "insert"); Ribbon.SetCommandId(group, "font"); tab.Groups.Add(group); host.Ribbon.Tabs.Add(tab);
        string layout = RibbonCustomizationSerializer.Serialize(host.Ribbon); Assert.True(host.Page.ImportDraft(layout)); host.Page.ApplyChanges();
        Assert.Contains(group, tab.Groups); Assert.Equal(2, host.Ribbon.Tabs.Count);
    });

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    [InlineData(0)]
    public void Redesign_import_legacy_or_unknown_profiles_survives_mode_template_switches_and_reset_matches_apply(int version) => Sta.Run(() =>
    {
        using var host = new Host(); host.Click("PART_NewGroupButton");
        var group = Assert.IsType<SimplifiedGroupChoice>(host.Current.SelectedItem).Group!;
        host.SelectAvailable(host.First); host.Click("PART_AddButton"); host.Page.ApplyChanges(); host.Layout();
        string groupId = Ribbon.GetCommandId(group)!;
        var document = JsonNode.Parse(RibbonCustomizationSerializer.Serialize(host.Ribbon))!;
        if (version == 0) document.AsObject().Remove("Simplified");
        else if (version == 99) document["Simplified"] = JsonNode.Parse("{\"Version\":99,\"Tabs\":[]}");
        else
        {
            document["Simplified"]!["Version"] = 1;
            foreach (var command in document["Simplified"]!["Tabs"]![0]!["Commands"]!.AsArray()) command!.AsObject().Remove("GroupId");
        }
        Assert.True(host.Page.ImportDraft(document.ToJsonString())); host.Layout();
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout(); host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        host.Page.Template = null; host.Layout(); host.Page.ClearValue(Control.TemplateProperty); host.Layout();
        Assert.Equal(version == 1 ? 2 : 3, host.Selected().Length);
        host.Click("PART_ResetAllButton"); Assert.Equal(new[] { host.First, host.Third }, host.Selected());
        host.Page.ApplyChanges(); host.Layout(); Assert.Equal(host.Selected(), host.Row());
        Assert.Single(host.Tab.Groups.Single(group => Ribbon.GetCommandId(group) == groupId).Items.Cast<object>());
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Redesign_menu_focus_enters_on_every_open_and_Escape_returns_to_the_button(FlowDirection direction) => Sta.Run(() =>
    {
        using var host = new Host(); host.Page.FlowDirection = direction; host.Window.ShowActivated = true; host.Window.Activate();
        var button = host.Part<Button>("PART_NewButton");
        for (int i = 0; i < 2; i++)
        {
            button.Focus(); host.Click("PART_NewButton"); var menu = button.ContextMenu;
            Assert.Same(menu.Items.OfType<MenuItem>().First(), Keyboard.FocusedElement);
            var inputSource = PresentationSource.FromVisual(menu)!;
            menu.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, inputSource, Environment.TickCount, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            menu.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, inputSource, Environment.TickCount, Key.Escape) { RoutedEvent = Keyboard.KeyDownEvent });
            host.Layout(); Assert.False(menu.IsOpen); Assert.Same(button, Keyboard.FocusedElement);
        }
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Redesign_native_drag_routes_show_an_insertion_and_reject_stale_or_foreign_drags(FlowDirection direction) => Sta.Run(() =>
    {
        using var host = new Host(); host.Page.FlowDirection = direction; host.Layout();
        var over = ReorderEvent(host, new Point(3, 3), UIElement.PreviewDragOverEvent);
        host.Current.RaiseEvent(over); Assert.Equal(DragDropEffects.Move, over.Effects);
        var insertionField = typeof(RibbonSimplifiedCustomizePage).GetField("_insertion", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.NotNull(insertionField.GetValue(host.Page));
        var drop = ReorderEvent(host, new Point(3, 3), UIElement.PreviewDropEvent);
        host.Current.RaiseEvent(drop); host.Layout(); Assert.Equal(DragDropEffects.Move, drop.Effects);
        Assert.Equal(new[] { host.Third, host.First }, host.Selected()); Assert.Null(insertionField.GetValue(host.Page));
        Assert.Equal(new[] { host.First, host.Third }, host.Row());
        var foreign = ReorderEvent(host, new Point(3, 3), UIElement.PreviewDropEvent, new RibbonSimplifiedCustomizePage());
        host.Current.RaiseEvent(foreign); Assert.Equal(DragDropEffects.None, foreign.Effects);
        var stale = ReorderEvent(host, new Point(3, 3), UIElement.PreviewDropEvent);
        host.Page.Template = null; host.Layout(); host.Page.ClearValue(Control.TemplateProperty); host.Layout();
        host.Current.RaiseEvent(stale); Assert.Equal(DragDropEffects.None, stale.Effects);
        host.Page.CancelChanges(); Assert.Equal(new[] { host.First, host.Third }, host.Selected());
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Redesign_native_drag_edge_scroll_uses_the_viewport_and_stops_on_leave(FlowDirection direction) => Sta.Run(() =>
    {
        using var host = new Host(); host.Page.FlowDirection = direction; host.Window.Height = 430;
        for (int i = 0; i < 35; i++)
        {
            var command = new RibbonButton { Header = "Command " + i }; Ribbon.SetSimplifiedPlacement(command, RibbonSimplifiedPlacement.Inline); host.GroupA.Items.Add(command);
        }
        host.Page.CancelChanges(); host.Layout();
        var scroll = (ScrollViewer)host.Current.Template.FindName("PART_ScrollViewer", host.Current);
        var viewport = (ScrollContentPresenter)scroll.Template.FindName("PART_ScrollContentPresenter", scroll);
        Rect bounds = viewport.TransformToAncestor(host.Current).TransformBounds(new Rect(viewport.RenderSize));
        host.Current.RaiseEvent(ReorderEvent(host, new Point(bounds.Left + bounds.Width / 2, bounds.Bottom - 1), UIElement.PreviewDragOverEvent));
        var frame = new DispatcherFrame();
        var finish = new DispatcherTimer(TimeSpan.FromMilliseconds(180), DispatcherPriority.Background, (_, _) => frame.Continue = false, Dispatcher.CurrentDispatcher);
        Dispatcher.PushFrame(frame); finish.Stop();
        Assert.True(scroll.VerticalOffset > 0);
        var insertion = typeof(RibbonSimplifiedCustomizePage).GetField("_insertion", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(host.Page)!;
        var line = (Rect)insertion.GetType().GetField("Line", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(insertion)!;
        Assert.InRange(line.Left, bounds.Left, bounds.Right); Assert.InRange(line.Right, bounds.Left, bounds.Right);
        host.Current.RaiseEvent(ReorderEvent(host, new Point(), UIElement.PreviewDragLeaveEvent));
        var timer = (DispatcherTimer)typeof(RibbonSimplifiedCustomizePage).GetField("_dragScroll", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(host.Page)!;
        Assert.False(timer.IsEnabled);
    });

    [Fact]
    public void Redesign_source_tab_filter_and_destination_selection_remain_independent_including_new_drafts() => Sta.Run(() =>
    {
        using var host = new Host();
        var insert = new RibbonTab { Header = "Insert" }; Ribbon.SetCommandId(insert, "insert");
        var group = new RibbonGroup { Header = "Images" }; Ribbon.SetCommandId(group, "images");
        var picture = new RibbonButton { Header = "Picture" }; Ribbon.SetCommandId(picture, "picture");
        group.Items.Add(picture); insert.Groups.Add(group); host.Ribbon.Tabs.Add(insert); host.Page.CancelChanges(); host.Layout();
        var sourceTabs = host.Part<Selector>("PART_SourceTabSelector");
        var targetTabs = host.Part<Selector>("PART_TabSelector");
        Assert.Same(host.Tab, ((SimplifiedTabChoice)sourceTabs.SelectedItem).Tab);
        host.Current.SelectedItem = host.Current.Items.OfType<SimplifiedCustomizeEntry>().First();
        sourceTabs.SelectedItem = sourceTabs.Items.Cast<SimplifiedTabChoice>().Single(choice => choice.Tab == insert);
        Assert.Same(host.Tab, ((SimplifiedTabChoice)targetTabs.SelectedItem).Tab);
        Assert.Same(picture, Assert.Single(host.Available.Items.Cast<SimplifiedCustomizeEntry>()).Control);
        Assert.Same(host.First, Assert.IsType<SimplifiedCustomizeEntry>(host.Current.SelectedItem).Control);
        host.Click("PART_NewTabButton");
        var custom = ((SimplifiedTabChoice)targetTabs.SelectedItem).Tab!;
        var customGroup = Assert.IsType<SimplifiedGroupChoice>(host.Current.SelectedItem).Group!;
        host.SelectAvailable(picture); host.Click("PART_AddButton");
        sourceTabs.SelectedItem = sourceTabs.Items.Cast<SimplifiedTabChoice>().Single(choice => choice.Tab == custom);
        Assert.Same(picture, Assert.Single(host.Available.Items.Cast<SimplifiedCustomizeEntry>()).Control);
        Assert.Equal(new[] { picture }, host.Selected()); Assert.Empty(customGroup.Items);
        host.Page.ApplyChanges(); host.Layout();
        Assert.Same(custom, ((SimplifiedTabChoice)sourceTabs.SelectedItem).Tab);
        Assert.Same(picture, Ribbon.GetQuickAccessSource(Assert.IsAssignableFrom<FrameworkElement>(customGroup.Items[0])));
        Assert.Empty(host.Ribbon.GetSimplifiedChoices(insert));
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Redesign_reorder_preserves_repeated_headers_membership_labels_and_keyboard_equivalence(FlowDirection direction) => Sta.Run(() =>
    {
        using var host = new Host(); host.Page.FlowDirection = direction;
        host.Ribbon.SetSimplifiedChoices(host.Tab, new[] { new SimplifiedCommandChoice(host.First, RibbonControlSize.Small, host.GroupA),
            new SimplifiedCommandChoice(host.Third, RibbonControlSize.Medium, host.GroupB), new SimplifiedCommandChoice(host.Second, RibbonControlSize.Medium, host.GroupA) });
        host.Layout(); string before = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        Assert.Equal(new[] { host.GroupA, host.GroupB, host.GroupA }, host.Current.Items.OfType<SimplifiedGroupChoice>().Select(header => header.Group));
        host.Current.SelectedItem = host.Current.Items.OfType<SimplifiedGroupChoice>().First();
        Assert.False(host.Part<ButtonBase>("PART_RemoveButton").IsEnabled); Assert.False(host.Part<ButtonBase>("PART_UpButton").IsEnabled);
        Assert.True(host.Page.ReorderCommand(host.Second, host.GroupA, 0));
        Assert.Equal(new[] { host.Second, host.First, host.Third }, host.Selected());
        Assert.Equal(new[] { host.GroupA, host.GroupB }, host.Current.Items.OfType<SimplifiedGroupChoice>().Select(header => header.Group));
        host.Click("PART_DownButton"); host.Click("PART_DownButton");
        Assert.Equal(new[] { host.First, host.Third, host.Second }, host.Selected());
        Assert.Equal(RibbonControlSize.Small, host.Current.Items.OfType<SimplifiedCustomizeEntry>().Single(entry => entry.Control == host.First).ShowLabel ? RibbonControlSize.Medium : RibbonControlSize.Small);
        Assert.False(host.Page.ReorderCommand(host.Second, host.GroupB, 0)); Assert.False(host.Page.ReorderCommand(host.Second, host.GroupA, 99));
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        host.Page.ReorderCommand(host.Third, host.GroupB, 0); host.Page.ApplyChanges(); host.Layout();
        Assert.Equal(new[] { host.Third, host.First, host.Second }, host.Row());
        Assert.Equal(new object[] { host.First, host.Second }, host.GroupA.Items.Cast<object>());
        Assert.Single(host.GroupB.Items.Cast<object>());
    });

    [Fact]
    public void Redesign_native_Edit_menu_toggle_changes_only_the_selected_group_occurrence() => Sta.Run(() =>
    {
        using var host = new Host(); host.Click("PART_NewGroupButton");
        var group = Assert.IsType<SimplifiedGroupChoice>(host.Current.SelectedItem).Group!;
        host.SelectAvailable(host.First); host.Click("PART_AddButton");
        host.Window.ShowActivated = true; host.Window.Activate();
        host.Part<Button>("PART_EditButton").Focus(); host.Click("PART_EditButton");
        var menu = host.Part<Button>("PART_EditButton").ContextMenu;
        var label = menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, RibbonLocalization.GetString(RibbonString.ShowItemLabel)));
        Assert.True(label.IsCheckable); Assert.True(label.IsChecked);
        ((IToggleProvider)new MenuItemAutomationPeer(label).GetPattern(PatternInterface.Toggle)).Toggle(); host.Layout();
        Assert.False(host.Current.Items.OfType<SimplifiedCustomizeEntry>().Single(entry => entry.Group == group).ShowLabel);
        Assert.True(host.Current.Items.OfType<SimplifiedCustomizeEntry>().Single(entry => entry.Group == host.GroupA).ShowLabel);
        menu.IsOpen = false; host.Click("PART_EditButton");
        label = host.Part<Button>("PART_EditButton").ContextMenu.Items.OfType<MenuItem>().Single(item => item.IsCheckable);
        Assert.False(label.IsChecked); host.Part<Button>("PART_EditButton").ContextMenu.IsOpen = false;
        host.Page.ApplyChanges();
        Assert.Equal(RibbonControlSize.Small, host.Ribbon.GetSimplifiedChoices(host.Tab).Single(choice => choice.Group == group).Size);
    });

    [Fact]
    public void Redesign_export_round_trips_pending_structure_and_labels_without_applying_and_import_is_cancelable() => Sta.Run(() =>
    {
        using var host = new Host(); string before = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        host.Click("PART_NewTabButton");
        var tab = ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).Tab!;
        var group = Assert.IsType<SimplifiedGroupChoice>(host.Current.SelectedItem).Group!;
        host.Page.RenameDraft(tab, "My tools"); host.Page.RenameDraft(group, "Favorites");
        host.SelectAvailable(host.Second); host.Click("PART_AddButton");
        Assert.IsType<SimplifiedCustomizeEntry>(host.Current.SelectedItem).ShowLabel = false;
        string exported = host.Page.ExportDraft(); Assert.Equal(exported, host.Page.ExportDraft());
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        host.Page.CancelChanges(); Assert.True(host.Page.ImportDraft(exported)); host.Layout();
        var selector = host.Part<Selector>("PART_TabSelector");
        selector.SelectedItem = selector.Items.Cast<SimplifiedTabChoice>().Single(choice => choice.DisplayName == "My tools"); host.Layout();
        Assert.False(Assert.Single(host.Current.Items.OfType<SimplifiedCustomizeEntry>()).ShowLabel);
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon)); host.Page.CancelChanges();
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        Assert.True(host.Page.ImportDraft(exported));
        int applied = 0; host.Ribbon.SimplifiedCustomizationChanged += (_, _) => applied++;
        host.Page.ApplyChanges(); host.Layout(); Assert.Equal(1, applied);
        var restored = host.Ribbon.Tabs.Single(tab => Equals(tab.Header, "My tools"));
        Assert.Equal("Favorites", restored.Groups.Single().Header);
        Assert.Same(host.Second, Ribbon.GetQuickAccessSource(Assert.IsAssignableFrom<FrameworkElement>(restored.Groups.Single().Items[0])));
        Assert.Equal(RibbonControlSize.Small, Assert.Single(host.Ribbon.GetSimplifiedChoices(restored)).Size);
    });

    [Fact]
    public void Redesign_QAT_reset_is_staged_and_preserves_structure_and_session_only_simplified_choices() => Sta.Run(() =>
    {
        using var host = new Host(); host.Ribbon.AddToQuickAccess(host.First);
        var baselinePosition = host.Ribbon.QuickAccessPosition;
        host.Page.ResetLayout = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        host.Ribbon.AddToQuickAccess(host.Second); host.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        host.Ribbon.IsQuickAccessVisible = false;
        Ribbon.SetCommandId(host.Third, null); host.Ribbon.SetSimplifiedChoices(host.Tab, new[] { new SimplifiedCommandChoice(host.Third, RibbonControlSize.Small, host.GroupB) });
        string before = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        ClickResetScope(host.Page, RibbonString.ResetQuickAccessToolbar); host.Page.CancelChanges();
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        ClickResetScope(host.Page, RibbonString.ResetQuickAccessToolbar);
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        host.Page.ApplyChanges(); host.Layout();
        Assert.True(host.Ribbon.IsInQuickAccess(host.First)); Assert.False(host.Ribbon.IsInQuickAccess(host.Second));
        Assert.True(host.Ribbon.IsQuickAccessVisible); Assert.Equal(baselinePosition, host.Ribbon.QuickAccessPosition);
        Assert.Same(host.Tab, host.Ribbon.Tabs.Single()); Assert.Same(host.GroupB, host.Tab.Groups.Last());
        Assert.Same(host.Third, Assert.Single(host.Ribbon.GetSimplifiedChoices(host.Tab)).Source);
    });

    [Fact]
    public void Redesign_full_reset_restores_authored_baseline_and_Cancel_keeps_an_earlier_Apply() => Sta.Run(() =>
    {
        using var host = new Host(); string baseline = RibbonCustomizationSerializer.Serialize(host.Ribbon); host.Page.ResetLayout = baseline;
        host.Click("PART_NewTabButton"); host.SelectAvailable(host.Second); host.Click("PART_AddButton"); host.Page.ApplyChanges(); host.Layout();
        host.Ribbon.AddToQuickAccess(host.Second); string customized = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        ClickResetScope(host.Page, RibbonString.ResetAllCustomizations); host.Page.CancelChanges();
        Assert.Equal(customized, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        ClickResetScope(host.Page, RibbonString.ResetAllCustomizations); host.Page.ApplyChanges(); host.Layout();
        Assert.Equal(baseline, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        host.Page.RenameDraft(host.Tab, "Pending name"); host.Page.CancelChanges();
        Assert.Equal(baseline, RibbonCustomizationSerializer.Serialize(host.Ribbon));
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Redesign_later_QAT_edits_supersede_older_pending_reset_or_import_without_losing_ribbon_edits(bool import) => Sta.Run(() =>
    {
        using var host = new Host(); host.Ribbon.AddToQuickAccess(host.First);
        host.Page.ResetLayout = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        host.Ribbon.AddToQuickAccess(host.Second);
        if (import)
        {
            var document = JsonNode.Parse(host.Page.ResetLayout)!; document["Tabs"]![0]!["Header"] = "Imported name";
            Assert.True(host.Page.ImportDraft(document.ToJsonString()));
        }
        else { ClickResetScope(host.Page, RibbonString.ResetQuickAccessToolbar); host.Page.RenameDraft(host.Tab, "Pending name"); }
        host.Ribbon.AddToQuickAccess(host.Third); host.Page.ApplyChanges(); host.Layout();
        Assert.Equal(3, host.Ribbon.QuickAccessItems.Count); Assert.True(host.Ribbon.IsInQuickAccess(host.Third));
        Assert.Equal(import ? "Imported name" : "Pending name", host.Tab.Header);
    });

    [Fact]
    public void Redesign_later_classic_edits_refresh_and_supersede_the_older_imported_ribbon_scope() => Sta.Run(() =>
    {
        using var host = new Host(); var document = JsonNode.Parse(RibbonCustomizationSerializer.Serialize(host.Ribbon))!;
        document["Tabs"]![0]!["Header"] = "Imported name";
        Assert.True(host.Page.ImportDraft(document.ToJsonString()));
        host.Tab.Header = "Later classic name"; host.Page.ApplyChanges(); host.Layout();
        Assert.Equal("Later classic name", host.Tab.Header);
        Assert.Equal("Later classic name", ((SimplifiedTabChoice)host.Part<Selector>("PART_TabSelector").SelectedItem).DisplayName);
    });

    [Theory]
    [InlineData("foreign")]
    [InlineData("duplicate-tab")]
    [InlineData("duplicate-group")]
    [InlineData("invalid-size")]
    public void Redesign_invalid_import_retains_the_previous_draft_and_live_layout(string kind) => Sta.Run(() =>
    {
        using var host = new Host(); string before = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        host.Page.RenameDraft(host.Tab, "Pending name"); string draft = host.Page.ExportDraft();
        var bad = JsonNode.Parse(before)!;
        switch (kind)
        {
            case "foreign": bad = JsonNode.Parse("{\"theme\":\"dark\"}")!; break;
            case "duplicate-tab": bad["Tabs"]!.AsArray().Add(bad["Tabs"]![0]!.DeepClone()); break;
            case "duplicate-group": bad["Tabs"]![0]!["Groups"]!.AsArray().Add(bad["Tabs"]![0]!["Groups"]![0]!.DeepClone()); break;
            case "invalid-size": bad["Simplified"] = JsonNode.Parse("{\"Version\":2,\"Tabs\":[{\"TabId\":\"home\",\"Commands\":[{\"SourceId\":\"first\",\"Size\":\"Large\"}]}]}"); break;
        }
        Assert.False(host.Page.ImportDraft(bad.ToJsonString())); Assert.Equal(draft, host.Page.ExportDraft());
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        RibbonCustomizationSerializer.Apply(host.Ribbon, bad.ToJsonString()); Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
    });

    private static void ClickResetScope(RibbonSimplifiedCustomizePage page, RibbonString key)
    {
        var button = (ButtonBase)page.Template.FindName("PART_ResetMenuButton", page);
        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        var item = button.ContextMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, RibbonLocalization.GetString(key)));
        Assert.True(item.IsEnabled); item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); button.ContextMenu.IsOpen = false; Sta.Drain();
    }

    private static DragEventArgs ReorderEvent(Host host, Point point, RoutedEvent routedEvent, RibbonSimplifiedCustomizePage? owner = null)
    {
        var type = typeof(RibbonSimplifiedCustomizePage).GetNestedType("CommandDrag", BindingFlags.NonPublic)!;
        int generation = (int)typeof(RibbonSimplifiedCustomizePage).GetField("_dragGeneration", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(host.Page)!;
        var drag = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            new object[] { owner ?? host.Page, host.Tab, host.Third, host.GroupB, generation }, null)!;
        var args = (DragEventArgs)Activator.CreateInstance(typeof(DragEventArgs), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            new object[] { new DataObject(type, drag), DragDropKeyStates.LeftMouseButton, DragDropEffects.Move, host.Current, point }, null)!;
        args.RoutedEvent = routedEvent; return args;
    }
}
