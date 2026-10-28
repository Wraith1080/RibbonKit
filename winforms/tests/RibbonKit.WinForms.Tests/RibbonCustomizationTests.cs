using System.Text.Json;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class RibbonCustomizationTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly RibbonControl Ribbon = new() { Width = 1300, BindingContext = new BindingContext() };
        internal readonly RibbonTab Home = new() { Id = "home", Text = "Home" }, Other = new() { Id = "other", Text = "Other" };
        internal readonly RibbonGroup Group = new() { Id = "font", Text = "Font" };
        internal readonly RibbonToggleItem Bold = new() { Id = "bold", Text = "Bold" };
        internal readonly RibbonTextBoxItem Field = new() { Id = "field", Text = "Name", Value = "Before" };
        internal readonly RibbonComboBoxItem Combo = new() { Id = "combo", Text = "Font" };
        internal readonly RibbonGalleryItem Gallery = new() { Id = "styles", Text = "Styles", IsInRibbon = true };
        internal Fixture()
        {
            Combo.Items.AddRange("Arial", "Calibri"); Combo.SelectedIndex = 0;
            Gallery.Items.AddRange(new RibbonGalleryTile { Id = "normal", Text = "Normal" }, new RibbonGalleryTile { Id = "strong", Text = "Strong" });
            Group.Items.AddRange(Bold, Field, Combo, Gallery); Home.Groups.Add(Group); Ribbon.Tabs.AddRange(Home, Other);
            Ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = Bold }); Ribbon.CommandCatalog.CaptureDefaults();
        }
        internal RibbonCustomizationSession Edit() => Ribbon.CommandCatalog.BeginCustomization("structure.test", RibbonCustomizationTarget.Ribbon);
        internal static RibbonCustomizationGroup Add(RibbonCustomizationSession session, params string[] commands)
        {
            var group = new RibbonCustomizationGroup { Label = "My commands", Commands = commands.Select(id => new RibbonCustomizationCommand { CommandId = id }).ToList() };
            session.Draft.Ribbon!.Tabs.Add(new() { Label = "My tab", Groups = [group] }); return group;
        }
        public void Dispose() => Ribbon.Dispose();
    }
    [Fact]
    public void Structural_apply_preserves_authored_models_labels_and_hidden_source_commands() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var edit = f.Edit(); var commands = f.Group.Items.ToArray();
        edit.Draft.Ribbon!.Tabs.Reverse(); edit.Draft.Ribbon.Tabs.Single(t => t.Id == "home").Label = "Start";
        edit.Draft.Ribbon.Tabs.Single(t => t.Id == "home").Visible = false; Fixture.Add(edit, "bold");
        Assert.Equal(new[] { f.Home, f.Other }, f.Ribbon.Tabs); Assert.True(edit.Apply());
        Assert.Same(f.Other, f.Ribbon.Tabs[0]); Assert.Same(f.Home, f.Ribbon.Tabs[1]); Assert.Same(f.Group, f.Home.Groups[0]); Assert.Equal(commands, f.Group.Items);
        Assert.Equal("Home", f.Home.Text); Assert.Equal("Start", f.Home.DisplayText); Assert.True(f.Home.Visible); Assert.DoesNotContain(f.Home, f.Ribbon.GetPresentedTabs());
        var proxy = Assert.IsType<RibbonToggleItem>(f.Ribbon.Tabs.Last().Groups[0].Items[0]); int clicks = 0; f.Bold.Click += (_, _) => clicks++;
        proxy.PerformClick(); Assert.True(f.Bold.Checked); Assert.True(proxy.Checked); Assert.Equal(1, clicks); Assert.Same(f.Bold, f.Ribbon.CommandCatalog.GetSource(proxy));
        Assert.Single(f.Ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.Ribbon), e => e.CommandId == "bold");
        edit.Reset(); Assert.True(edit.Apply()); Assert.Equal(new[] { f.Home, f.Other }, f.Ribbon.Tabs); Assert.Equal("Home", f.Home.DisplayText); Assert.False(proxy.IsDisposed);
        edit.Cancel(); Assert.Equal(2, edit.Draft.Ribbon!.Tabs.Count);
    });
    [Fact]
    public void Scoped_pages_apply_independently_and_full_export_round_trips_with_legacy_qat_compatibility() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var ribbon = f.Edit(); using var qat = f.Ribbon.CommandCatalog.BeginCustomization("structure.test", RibbonCustomizationTarget.QuickAccess);
        Fixture.Add(ribbon, "bold", "combo"); qat.Draft.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        Assert.True(qat.Apply()); Assert.True(ribbon.Apply()); Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, f.Ribbon.QuickAccessPosition);
        string json = ribbon.ExportJson(); Assert.Contains("\"version\": 3", json); Assert.DoesNotContain("Checked", json);
        using var restarted = new Fixture(); using var restore = restarted.Ribbon.CommandCatalog.BeginCustomization("structure.test"); restore.ImportJson(json); Assert.True(restore.Apply());
        Assert.Equal(3, restarted.Ribbon.Tabs.Count); Assert.Equal("My tab", restarted.Ribbon.Tabs.Last().DisplayText);
        restore.Reset(); Assert.True(restore.Apply()); Assert.Equal(2, restarted.Ribbon.Tabs.Count);
        using var legacy = f.Ribbon.CommandCatalog.BeginCustomization("structure.test");
        var version1 = new RibbonCustomizationState { Version = 1, ApplicationId = "structure.test", QuickAccessVisible = false };
        legacy.ImportJson(RibbonCustomizationSerializer.Serialize(version1)); Assert.True(legacy.Apply()); Assert.Equal(3, f.Ribbon.Tabs.Count); Assert.False(f.Ribbon.QuickAccessVisible);
        ribbon.Reload(); ribbon.Draft.Ribbon!.Tabs.Reverse(); f.Ribbon.Tabs[0].Groups.Add(new RibbonGroup { Id = "external", Text = "External" });
        Assert.Throws<InvalidOperationException>(() => ribbon.Apply());
    });
    [Fact]
    public void Invalid_structure_and_ids_are_rejected_without_changing_live_ownership() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var edit = f.Edit(); var tabs = f.Ribbon.Tabs.ToArray();
        foreach (var tab in edit.Draft.Ribbon!.Tabs) tab.Visible = false;
        Assert.Throws<InvalidOperationException>(() => edit.Apply()); edit.Cancel();
        edit.Draft.Ribbon!.Tabs.RemoveAt(0); Assert.Throws<InvalidOperationException>(() => edit.Apply()); edit.Cancel();
        Fixture.Add(edit, "missing"); Assert.Throws<InvalidOperationException>(() => edit.Apply()); edit.Cancel();
        var custom = Fixture.Add(edit, "bold"); custom.Commands[0].Id = f.Group.Id; Assert.Throws<InvalidOperationException>(() => edit.Apply()); edit.Cancel();
        edit.Draft.Ribbon!.Tabs[0].Groups[0].Commands.Add(new() { CommandId = "bold" }); Assert.Throws<InvalidOperationException>(() => edit.Apply());
        Assert.Equal(tabs, f.Ribbon.Tabs); Assert.Same(f.Home, f.Group.Parent); Assert.Same(f.Group, f.Bold.Parent);
        string bad = "{\"format\":\"RibbonKit.WinForms.Customization\",\"version\":3}"; Assert.ThrowsAny<Exception>(() => RibbonCustomizationSerializer.Deserialize(bad, "structure.test"));
    });
    [Fact]
    public void New_presentations_share_source_state_actions_and_borrow_assets() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var check = new RibbonCheckBoxItem { Id = "check", ThreeState = true }; var radio = new RibbonRadioButtonItem { Id = "radio" };
        var split = new RibbonSplitButtonItem { Id = "split", CheckOnClick = true }; f.Group.Items.AddRange(check, radio, split);
        using var image = new Bitmap(16, 16); f.Bold.Image = image;
        using var edit = f.Edit(); Fixture.Add(edit, "bold", "check", "radio", "split"); Assert.True(edit.Apply());
        var models = f.Ribbon.Tabs.Last().Groups[0].Items; int actions = 0;
        f.Bold.Click += (_, _) => actions++; check.Click += (_, _) => actions++; radio.Click += (_, _) => actions++; split.Click += (_, _) => actions++;
        foreach (var model in models) model.PerformClick(); Assert.Equal(4, actions);
        Assert.True(f.Bold.Checked); Assert.Equal(CheckState.Checked, check.CheckState); Assert.True(radio.Checked); Assert.True(split.Checked);
        f.Bold.Text = "Strong"; Assert.Equal("Strong", models[0].Text); f.Group.Enabled = false; Assert.All(models, m => Assert.False(m.Enabled));
        edit.Reset(); Assert.True(edit.Apply()); f.Ribbon.Dispose(); Assert.Equal(16, image.Width);
    });
    [Fact]
    public void Native_editor_projections_keep_separate_handles_and_commit_through_original_source() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var host = new Form { Width = 1400, Height = 550 }; host.Controls.Add(f.Ribbon); host.Show(); Application.DoEvents();
        var original = f.Ribbon.Editors.EditorFor(f.Field)!; nint originalHandle = original.Handle;
        using var edit = f.Edit(); Fixture.Add(edit, "field", "combo"); Assert.True(edit.Apply());
        var custom = f.Ribbon.Tabs.Last(); f.Ribbon.SelectedTab = custom; Application.DoEvents();
        var field = Assert.IsType<RibbonTextBoxItem>(custom.Groups[0].Items[0]); var combo = Assert.IsType<RibbonComboBoxItem>(custom.Groups[0].Items[1]);
        var native = f.Ribbon.Editors.EditorFor(field)!; Assert.NotEqual(originalHandle, native.Handle); nint customHandle = native.Handle;
        int commits = 0; f.Field.ValueValidating += (_, e) => e.Cancel = f.Field.Value.Length == 0; f.Field.ValueCommitted += (_, _) => commits++;
        field.PerformClick(); native.Text = ""; Assert.False(edit.Apply()); Assert.Same(custom, f.Ribbon.SelectedTab);
        native.Text = "After"; Assert.True(edit.Apply()); Assert.Equal("After", f.Field.Value); Assert.Equal("After", field.Value); Assert.Equal(1, commits);
        Assert.Equal(customHandle, native.Handle); Assert.Equal(originalHandle, original.Handle);
        combo.SelectedIndex = 1; Assert.Equal(1, f.Combo.SelectedIndex); Assert.Equal("Calibri", f.Combo.Value);
        f.Ribbon.SelectedTab = custom; field.PerformClick(); native.Text = "Retarget"; f.Field.ValueValidating += (_, _) => f.Combo.Id = "changed";
        Assert.Throws<InvalidOperationException>(() => edit.Apply()); Assert.Same(custom, f.Ribbon.Tabs.Last());
    });
    [Fact]
    public void Gallery_preview_commit_and_source_tile_changes_keep_original_identity() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var edit = f.Edit(); Fixture.Add(edit, "styles"); Assert.True(edit.Apply());
        var gallery = Assert.IsType<RibbonGalleryItem>(f.Ribbon.Tabs.Last().Groups[0].Items[0]); int previews = 0, commits = 0;
        f.Gallery.ItemPreview += (_, _) => previews++; f.Gallery.SelectionCommitted += (_, _) => commits++;
        var tile = Assert.IsType<RibbonGalleryTile>(gallery.Items[1]); gallery.Preview(tile); Assert.Same(f.Gallery.Items[1], f.Gallery.PreviewItem); Assert.Same(tile, gallery.PreviewItem); Assert.Equal(1, previews);
        f.Bold.Text = "Refresh other command"; Assert.Same(tile, gallery.PreviewItem);
        tile.PerformClick(); Assert.Same(f.Gallery.Items[1], f.Gallery.SelectedItem); Assert.Same(tile, gallery.SelectedItem); Assert.Equal(1, commits);
        gallery.Enabled = false; tile.PerformClick(); Assert.Equal(1, commits); gallery.Preview(tile); Assert.Null(f.Gallery.PreviewItem); gallery.Enabled = true;
        f.Gallery.Items[0].Dispose(); Assert.Single(gallery.Items); Assert.Same(tile, gallery.Items[0]);
    });
    [Fact]
    public void Apply_failure_rolls_back_structure_and_transient_tabs_and_keeps_sources_alive() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var modal = new RibbonTab { Id = "preview", IsModal = true }; var context = new RibbonTab { Id = "picture", IsContextual = true };
        f.Ribbon.Tabs.Insert(1, context); f.Ribbon.Tabs.Add(modal);
        using var edit = f.Ribbon.CommandCatalog.BeginCustomization("structure.test"); edit.Draft.Ribbon = f.Ribbon.CommandCatalog.Structure.Capture(); Fixture.Add(edit, "bold"); edit.Draft.Ribbon!.Tabs.Reverse();
        edit.Draft.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; var before = f.Ribbon.Tabs.ToArray(); bool fail = true;
        f.Ribbon.QuickAccessPositionChanged += (_, _) => { if (fail) { fail = false; throw new InvalidOperationException("host rejected"); } };
        Assert.Throws<InvalidOperationException>(() => edit.Apply()); Assert.Equal(before, f.Ribbon.Tabs); Assert.Same(f.Home, f.Group.Parent); Assert.False(f.Bold.IsDisposed);
        Assert.Equal(RibbonQuickAccessPosition.TabRow, f.Ribbon.QuickAccessPosition); Assert.True(edit.Apply()); Assert.Contains(modal, f.Ribbon.Tabs); Assert.Contains(context, f.Ribbon.Tabs);
        Assert.DoesNotContain("preview", edit.ExportJson()); Assert.DoesNotContain("picture", edit.ExportJson());
    });
    [Fact]
    public void Projection_dropdown_uses_original_events_content_and_close_contract() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var menu = new RibbonDropDownItem { Id = "menu", Text = "Menu" }; var row = new RibbonButtonItem { Id = "menu.action", Text = "Action" }; menu.Items.Add(row); f.Group.Items.Add(menu);
        using var edit = f.Edit(); Fixture.Add(edit, "menu"); Assert.True(edit.Apply()); var custom = f.Ribbon.Tabs.Last();
        using var host = new Form { Width = 1300, Height = 500 }; host.Controls.Add(f.Ribbon); host.Show(); f.Ribbon.SelectedTab = custom; Application.DoEvents();
        var proxy = Assert.IsType<RibbonDropDownItem>(custom.Groups[0].Items[0]); int opening = 0, closing = 0;
        menu.DropDownOpening += (_, _) => opening++; menu.DropDownClosed += (_, _) => closing++;
        proxy.ShowDropDown(); Assert.True(proxy.IsDropDownOpen); Assert.False(menu.IsDropDownOpen); Assert.Equal(1, opening);
        Assert.Same(menu, f.Ribbon.Popups.Top!.MenuSource); Assert.Same(menu, row.Parent);
        proxy.CloseDropDown(); Assert.False(proxy.IsDropDownOpen); Assert.False(menu.IsDropDownOpen); Assert.Equal(1, closing);
        proxy.ShowDropDown(); menu.CloseDropDown(); Assert.False(proxy.IsDropDownOpen); Assert.Equal(2, closing);
    });
    [Fact]
    public void Combined_identity_preflight_and_external_definition_reload_do_not_rewrite_sources() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var edit = f.Ribbon.CommandCatalog.BeginCustomization("structure.test"); edit.Draft.Ribbon = f.Ribbon.CommandCatalog.Structure.Capture();
        var group = Fixture.Add(edit, "bold"); edit.Draft.QuickAccess.Add(new() { Id = group.Id, CommandId = "bold" }); int changed = 0; f.Ribbon.CustomizationApplied += (_, _) => changed++;
        var tabs = f.Ribbon.Tabs.ToArray(); Assert.Throws<InvalidOperationException>(() => edit.Apply()); Assert.Equal(tabs, f.Ribbon.Tabs); Assert.Equal(0, changed); Assert.Throws<InvalidOperationException>(() => edit.ExportJson());
        edit.Cancel(); edit.Draft.Ribbon = f.Ribbon.CommandCatalog.Structure.Capture(); var authored = edit.Draft.Ribbon.Tabs[0].Groups[0]; edit.Draft.Ribbon.Tabs[0].Groups.Clear(); edit.Draft.Ribbon.Tabs[1].Groups.Add(authored);
        Assert.Throws<InvalidOperationException>(() => edit.Apply()); Assert.Same(f.Home, f.Group.Parent);
        f.Ribbon.Tabs.Remove(f.Other); edit.Reload(); Assert.True(edit.Apply()); Assert.Single(f.Ribbon.Tabs);
        f.Home.Groups.Remove(f.Group); edit.Reload(); Assert.True(edit.Apply()); Assert.Empty(f.Home.Groups); Assert.Null(f.Group.Parent); f.Group.Dispose(); f.Other.Dispose();
    });
    [Fact]
    public void Custom_native_input_updates_original_validation_binding_only_after_acceptance() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var source = new BindingSource(); var state = new BoundValue { Value = "Initial" }; source.DataSource = state;
        f.Field.DataBindings.Add(new Binding(nameof(f.Field.Value), source, nameof(state.Value), true, DataSourceUpdateMode.OnValidation));
        using var edit = f.Edit(); Fixture.Add(edit, "field"); Assert.True(edit.Apply()); var custom = f.Ribbon.Tabs.Last();
        using var host = new Form { Width = 1400, Height = 500 }; host.Controls.Add(f.Ribbon); host.Show(); f.Ribbon.SelectedTab = custom; Application.DoEvents();
        var proxy = (RibbonTextBoxItem)custom.Groups[0].Items[0]; proxy.PerformClick(); var native = f.Ribbon.Editors.EditorFor(proxy)!;
        f.Field.ValueValidating += (_, e) => e.Cancel = f.Field.Value == "Rejected";
        native.Text = "Rejected"; Assert.False(edit.Apply()); Assert.Equal("Initial", state.Value);
        native.Text = "Accepted"; Assert.True(edit.Apply()); Assert.Equal("Accepted", state.Value); Assert.Single(f.Field.DataBindings); Assert.Empty(proxy.DataBindings);
    });
    private sealed class BoundValue { public string Value { get; set; } = ""; }
    [Fact]
    public void Editor_controls_stage_hierarchy_and_filters_without_modifying_authored_commands() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var page = new RibbonCustomizationEditor(f.Ribbon, "structure.test"); using var host = new Form { Width = 1100, Height = 650 }; host.Controls.Add(page); page.Dock = DockStyle.Fill; host.Show(); Application.DoEvents();
        static T Child<T>(Control root, string name) where T : Control => (T)root.Controls.Find(name, true).Single();
        var tree = Child<TreeView>(page, "RibbonHierarchy"); Assert.Equal(2, tree.Nodes.Count); Assert.Equal(1, tree.Nodes[0].StateImageIndex);
        Child<Button>(page, "NewRibbonTab").PerformClick(); Assert.Equal(3, page.Session.Draft.Ribbon!.Tabs.Count); Assert.Equal(2, f.Ribbon.Tabs.Count);
        Child<TextBox>(page, "RibbonCommandSearch").Text = "Bold"; Assert.Single(Child<ListBox>(page, "RibbonAvailableCommands").Items);
        Child<Button>(page, "AddRibbonCommand").PerformClick(); Assert.Single(page.Session.Draft.Ribbon.Tabs.Single(t => t.IsCustom).Groups[0].Commands);
        Child<TextBox>(page, "RibbonHierarchySearch").Text = "Bold"; Assert.Equal(3, page.Session.Draft.Ribbon.Tabs.Count);
        Assert.True(page.ApplyChanges()); Assert.Equal(3, f.Ribbon.Tabs.Count); Assert.Same(f.Group, f.Home.Groups[0]);
    });
}
