using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class SimplifiedRibbonTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly RibbonControl Ribbon = new() { Width = 1200, BindingContext = new BindingContext() };
        internal readonly RibbonTab Home = new() { Id = "home", Text = "Home" }, Insert = new() { Id = "insert", Text = "Insert" };
        internal readonly RibbonGroup Clipboard = new() { Id = "clipboard", Text = "Clipboard" }, Font = new() { Id = "font", Text = "Font" };
        internal readonly RibbonSplitButtonItem Paste = new() { Id = "paste", Text = "Paste", Layout = RibbonSplitButtonLayout.Vertical, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedPriority = 10 };
        internal readonly RibbonButtonItem Copy = new() { Id = "copy", Text = "Copy", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        internal readonly RibbonToggleItem Bold = new() { Id = "bold", Text = "Bold", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedSize = RibbonItemSize.Small };
        internal readonly RibbonComboBoxItem Combo = new() { Id = "combo", Text = "Zoom", IsEditable = true, Value = "100", InputWidth = 120, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        internal readonly RibbonTextBoxItem Field = new() { Id = "field", Text = "Name", Value = "Source", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        internal readonly RibbonButtonItem Picture = new() { Id = "picture", Text = "Picture", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        internal Fixture()
        {
            Paste.Items.Add(new RibbonButtonItem { Id = "paste.text", Text = "Paste text" }); Combo.Items.AddRange("100", "150");
            Clipboard.Items.AddRange(Paste, Copy); Font.Items.AddRange(Bold, Combo, Field); Home.Groups.AddRange(Clipboard, Font);
            var illustration = new RibbonGroup { Id = "illustration", Text = "Illustrations" }; illustration.Items.Add(Picture); Insert.Groups.Add(illustration);
            Ribbon.Tabs.AddRange(Home, Insert); Ribbon.SelectedTab = Home; Ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = Bold }); Ribbon.CommandCatalog.CaptureDefaults();
        }
        internal RibbonCustomizationSession Edit() => Ribbon.CommandCatalog.BeginCustomization("simple.test", RibbonCustomizationTarget.Simplified);
        internal void Show(Form host) { host.Width = 1350; host.Height = 650; host.Controls.Add(Ribbon); host.Show(); Application.DoEvents(); }
        public void Dispose() => Ribbon.Dispose();
    }
    [Fact]
    public void Row_uses_original_sources_horizontal_split_geometry_and_full_group_access_at_every_width() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); int classic = f.Ribbon.Height; var items = f.Clipboard.Items.ToArray(); f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        Assert.True(f.Ribbon.Height < classic); Assert.Empty(f.Ribbon.Snapshot.Groups);
        Assert.Contains(f.Ribbon.Snapshot.Targets, t => t.Element == f.Copy); Assert.Contains(f.Ribbon.Snapshot.Targets, t => t.Element == f.Combo); Assert.DoesNotContain(f.Ribbon.Snapshot.Targets, t => t.Element == f.Field);
        var split = f.Ribbon.Snapshot.Targets.Where(t => t.Element == f.Paste).ToArray(); Assert.Equal(2, split.Length); Assert.Equal(split[0].Bounds.Right, split[1].Bounds.Left); Assert.Equal(split[0].Bounds.Top, split[1].Bounds.Top);
        int compact = f.Ribbon.Height; f.Ribbon.Density = RibbonDensity.Touch; Assert.True(f.Ribbon.Height > compact);
        foreach (int width in new[] { 1200, 400, 150, 24 })
        {
            f.Ribbon.Width = width; var snapshot = f.Ribbon.Snapshot;
            Assert.Single(snapshot.Targets, t => t.Element == f.Ribbon.Simplified.More); Assert.Equal(2, f.Ribbon.Simplified.More.Entries.OfType<RibbonQuickAccessGroupReference>().Count());
            Assert.All(snapshot.Targets.Where(t => t.Kind == TargetKind.Item), t => Assert.True(snapshot.BodyViewport.Contains(t.VisibleBounds)));
        }
        f.Ribbon.LayoutMode = RibbonLayoutMode.Classic; f.Ribbon.Density = RibbonDensity.Compact; Assert.Equal(classic, f.Ribbon.Height); Assert.Equal(items, f.Clipboard.Items); Assert.Same(f.Clipboard, f.Copy.Parent);
        Assert.Throws<ArgumentOutOfRangeException>(() => f.Copy.SimplifiedSize = RibbonItemSize.Large);
        Assert.Throws<ArgumentOutOfRangeException>(() => f.Ribbon.LayoutMode = (RibbonLayoutMode)99);
    });
    [Fact]
    public void More_commands_share_actions_and_borrow_original_group_editors_with_the_same_handle() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var host = new Form(); f.Show(host);
        var original = f.Ribbon.Editors.EditorFor(f.Combo)!; nint handle = original.Handle;
        f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; Assert.Equal(handle, f.Ribbon.Editors.EditorFor(f.Combo)!.Handle);
        host.Width = 130; host.PerformLayout(); Application.DoEvents(); int calls = 0; f.Copy.Click += (_, _) => calls++;
        f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => t.Element == f.Ribbon.Simplified.More));
        var surface = f.Ribbon.Popups.Top!; var copy = surface.Targets.FirstOrDefault(t => t.Element is RibbonQuickAccessReference { Source: var source } && source == f.Copy);
        Assert.True(copy is not null, "width=" + f.Ribbon.Width + "; root=" + string.Join(",", f.Ribbon.Snapshot.Targets.Select(t => t.Element?.Text)) + "; popup=" + string.Join(",", surface.Targets.Select(t => t.Element?.GetType().Name + ":" + (t.Element is null ? "" : RibbonQuickAccess.Caption(t.Element))))); f.Ribbon.Activate(copy!, surface); Assert.Equal(1, calls);
        f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => t.Element == f.Ribbon.Simplified.More)); surface = f.Ribbon.Popups.Top!;
        var group = surface.Targets.Single(t => t.Element is RibbonQuickAccessGroupReference { Source: var source } && source == f.Font);
        f.Ribbon.Activate(group, surface); var full = f.Ribbon.Popups.Top!; Assert.Same(f.Font, full.GroupSource); Assert.Equal(handle, f.Ribbon.Editors.EditorFor(f.Combo)!.Handle); Assert.Same(full, original.Parent?.Parent); Assert.Same(f.Font, f.Combo.Parent);
        f.Ribbon.Popups.CloseAll(false); host.Width = 1350; host.PerformLayout(); f.Ribbon.SyncEditors(); Assert.Equal(handle, original.Handle); Assert.Same(f.Ribbon, original.Parent?.Parent);
        f.Combo.ValueValidating += (_, e) => e.Cancel = f.Combo.Value == "bad"; f.Combo.PerformClick(); original.Text = "bad";
        f.Ribbon.LayoutMode = RibbonLayoutMode.Classic; Assert.Equal(RibbonLayoutMode.Simplified, f.Ribbon.LayoutMode);
        original.Text = "150"; f.Ribbon.LayoutMode = RibbonLayoutMode.Classic; Assert.Equal(RibbonLayoutMode.Classic, f.Ribbon.LayoutMode); Assert.Equal("150", f.Combo.Value);
    });
    [Fact]
    public void Simplified_structural_draft_applies_to_classic_and_profiles_without_changing_qat_or_mode() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; using var edit = f.Edit();
        var group = new RibbonCustomizationGroup { Label = "My group", Commands = [new() { CommandId = "copy" }] }; var tab = new RibbonCustomizationTab { Label = "My tab", Groups = [group] };
        edit.Draft.Ribbon!.Tabs.Add(tab); edit.Draft.Ribbon.Tabs[0].Label = "Start"; edit.Draft.Simplified!.Tabs.Add(new() { TabId = tab.Id, Commands = [new() { CommandId = "copy", Size = RibbonItemSize.Small }] });
        var toolbar = f.Ribbon.QuickAccessItems.ToArray(); Assert.Equal(2, f.Ribbon.Tabs.Count); Assert.True(edit.Apply()); Assert.Equal(toolbar, f.Ribbon.QuickAccessItems); Assert.Equal("Start", f.Home.DisplayText);
        var created = f.Ribbon.Tabs.Single(t => t.Id == tab.Id); var projection = created.Groups[0].Items[0]; Assert.Same(f.Copy, f.Ribbon.CommandCatalog.GetSource(projection));
        f.Ribbon.SelectedTab = created; Assert.Contains(f.Ribbon.Snapshot.Targets, t => t.Element == projection && t.Size == RibbonItemSize.Small);
        f.Ribbon.LayoutMode = RibbonLayoutMode.Classic; Assert.Same(created, f.Ribbon.Tabs.Last()); Assert.Same(f.Clipboard, f.Copy.Parent); Assert.Single(created.Groups[0].Items);
        string json = edit.ExportJson(); Assert.Contains("\"version\": 3", json); Assert.DoesNotContain("LayoutMode", json); Assert.DoesNotContain("Checked", json);
        using var restarted = new Fixture(); using var restore = restarted.Ribbon.CommandCatalog.BeginCustomization("simple.test"); restore.ImportJson(json); Assert.True(restore.Apply()); Assert.Equal(RibbonLayoutMode.Classic, restarted.Ribbon.LayoutMode); Assert.Equal(3, restarted.Ribbon.Tabs.Count);
        edit.Draft.Simplified.Tabs.Clear(); edit.Cancel(); Assert.Single(edit.Draft.Simplified!.Tabs);
        using var legacy = f.Ribbon.CommandCatalog.BeginCustomization("simple.test"); legacy.ImportJson(RibbonCustomizationSerializer.Serialize(new() { Version = 2, ApplicationId = "simple.test" })); Assert.True(legacy.Apply()); Assert.Single(f.Ribbon.Simplified.Preferences.Tabs);
        edit.Reload(); edit.Reset(); Assert.True(edit.Apply()); Assert.Equal(2, f.Ribbon.Tabs.Count); Assert.Empty(f.Ribbon.Simplified.Preferences.Tabs);
    });
    [Fact]
    public void Qat_scope_is_independent_stale_simplified_edits_are_rejected_and_structural_removal_prunes_pins() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var simple = f.Edit(); using var qat = f.Ribbon.CommandCatalog.BeginCustomization("simple.test", RibbonCustomizationTarget.QuickAccess);
        simple.Draft.Simplified!.Tabs.Add(new() { TabId = "home", Commands = [new() { CommandId = "copy" }] }); qat.Draft.QuickAccessVisible = false; Assert.True(qat.Apply()); Assert.True(simple.Apply()); Assert.False(f.Ribbon.QuickAccessVisible);
        using var stale = f.Edit(); simple.Draft.Simplified.Tabs[0].Commands.Add(new() { CommandId = "bold" }); Assert.True(simple.Apply()); Assert.Throws<InvalidOperationException>(() => stale.Apply());
        using var structure = f.Ribbon.CommandCatalog.BeginCustomization("simple.test", RibbonCustomizationTarget.Ribbon);
        var tab = new RibbonCustomizationTab { Label = "Custom", Groups = [new() { Label = "Group", Commands = [new() { CommandId = "copy" }] }] }; structure.Draft.Ribbon!.Tabs.Add(tab); Assert.True(structure.Apply());
        simple.Reload(); simple.Draft.Simplified!.Tabs.Add(new() { TabId = tab.Id, Commands = [new() { CommandId = "copy" }] }); Assert.True(simple.Apply());
        structure.Reload(); structure.Draft.Ribbon!.Tabs.RemoveAll(t => t.Id == tab.Id); Assert.True(structure.Apply()); Assert.DoesNotContain(f.Ribbon.Simplified.Preferences.Tabs, t => t.TabId == tab.Id); Assert.False(f.Ribbon.QuickAccessVisible);
    });
    [Fact]
    public void Transient_defaults_are_visible_but_contextual_and_unknown_selections_do_not_enter_profiles() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var source = new RibbonMergeSource(); var contribution = new RibbonGroupContribution { TargetTabId = "home" }; contribution.Group.Id = "temporary"; var command = new RibbonButtonItem { Id = "temp", Text = "Temporary", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; contribution.Group.Items.Add(command); source.Groups.Add(contribution); f.Ribbon.Merge(source);
        using var edit = f.Edit(); edit.Draft.Simplified!.Tabs.Add(new() { TabId = "home", Commands = [] }); Assert.True(edit.Apply()); f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; Assert.Contains(f.Ribbon.Snapshot.Targets, t => t.Element == command);
        edit.Draft.Simplified.Tabs[0].Commands.Add(new() { CommandId = "temp" }); Assert.Throws<InvalidOperationException>(() => edit.Apply()); edit.Cancel();
        edit.Draft.Simplified!.Tabs[0].Commands.Add(new() { CommandId = "field" }); Assert.Throws<InvalidOperationException>(() => edit.Apply()); edit.Cancel();
        edit.Draft.Simplified!.Tabs[0].Commands.AddRange([new() { CommandId = "copy" }, new() { CommandId = "copy" }]); Assert.Throws<InvalidOperationException>(() => edit.Apply()); edit.Cancel();
        Assert.DoesNotContain("temporary", edit.ExportJson()); f.Ribbon.Unmerge(source); Assert.DoesNotContain(f.Ribbon.Snapshot.Targets, t => t.Element == command);
    });
    [Fact]
    public void Failed_structural_apply_restores_simplified_pins_selected_models_and_independent_preferences() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; using var edit = f.Edit();
        var tab = new RibbonCustomizationTab { Label = "Custom", Groups = [new() { Label = "Commands", Commands = [new() { CommandId = "copy" }] }] };
        edit.Draft.Ribbon!.Tabs.Add(tab); edit.Draft.Simplified!.Tabs.Add(new() { TabId = tab.Id, Commands = [new() { CommandId = "copy" }] }); Assert.True(edit.Apply());
        var selected = f.Ribbon.Tabs.Last(); f.Ribbon.SelectedTab = selected; var tabs = f.Ribbon.Tabs.ToArray(); var qat = f.Ribbon.QuickAccessItems.ToArray();
        edit.Draft.Ribbon!.Tabs.RemoveAll(t => t.Id == tab.Id); edit.Draft.Simplified!.Tabs.Clear(); bool fail = true;
        f.Ribbon.SelectedTabChanged += (_, _) => { if (fail) { fail = false; throw new InvalidOperationException("Host rejected selection"); } };
        Assert.Throws<InvalidOperationException>(() => edit.Apply()); Assert.Equal(tabs, f.Ribbon.Tabs); Assert.Same(selected, f.Ribbon.SelectedTab); Assert.Equal(qat, f.Ribbon.QuickAccessItems);
        Assert.Single(f.Ribbon.Simplified.Preferences.Tabs); Assert.False(f.Copy.IsDisposed); Assert.Equal(RibbonLayoutMode.Simplified, f.Ribbon.LayoutMode); Assert.True(edit.Apply()); Assert.Equal(2, f.Ribbon.Tabs.Count); Assert.Empty(f.Ribbon.Simplified.Preferences.Tabs);
    });
    [Fact]
    public void Minimized_simplified_body_opens_more_and_full_groups_then_closes_on_mode_change() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var host = new Form(); f.Show(host); f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        f.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; f.Ribbon.IsMinimized = true;
        f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => t.Element == f.Home)); var body = f.Ribbon.Popups.Top!;
        Assert.Contains(body.Targets, t => t.Element == f.Copy); Assert.Empty(f.Ribbon.Snapshot.Groups);
        f.Ribbon.Activate(body.Targets.Single(t => t.Element == f.Ribbon.Simplified.More), body); var more = f.Ribbon.Popups.Top!;
        f.Ribbon.Activate(more.Targets.Single(t => t.Element is RibbonQuickAccessGroupReference { Source: var source } && source == f.Font), more);
        Assert.Same(f.Font, f.Ribbon.Popups.Top!.GroupSource); Assert.True(f.Ribbon.Editors.EditorFor(f.Combo)!.Visible);
        f.Ribbon.LayoutMode = RibbonLayoutMode.Classic; Assert.Null(f.Ribbon.Popups.Top); Assert.True(f.Ribbon.IsMinimized); Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, f.Ribbon.QuickAccessPosition);
    });
    [Fact]
    public void Context_unpin_keeps_hidden_defaults_and_simplified_gallery_commits_original_tiles() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var host = new Form(); f.Show(host); f.Copy.Visible = false; f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        Assert.True(f.Ribbon.ShowCustomizationMenu(f.Ribbon, f.Ribbon.PointToScreen(new(20, 60)), f.Paste));
        f.Ribbon.CustomizationMenu!.Items.Single(i => i.Id == "ToggleSimplifiedPin").PerformClick();
        Assert.DoesNotContain(f.Ribbon.Simplified.Preferences.Tabs.Single().Commands, c => c.CommandId == "paste"); Assert.Contains(f.Ribbon.Simplified.Preferences.Tabs.Single().Commands, c => c.CommandId == "copy");
        f.Copy.Visible = true; f.Ribbon.Popups.CloseAll(false); Assert.Contains(f.Ribbon.Snapshot.Targets, t => t.Element == f.Copy);
        var gallery = new RibbonGalleryItem { Id = "styles", Text = "Styles", IsInRibbon = true, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        gallery.Items.AddRange(new RibbonGalleryTile { Text = "Normal" }, new RibbonGalleryTile { Text = "Strong" }); f.Font.Items.Add(gallery);
        using var edit = f.Edit(); edit.Draft.Simplified!.Tabs.Single().Commands.Add(new() { CommandId = "styles" }); Assert.True(edit.Apply());
        var target = f.Ribbon.Snapshot.Targets.Single(t => t.Element == gallery); Assert.True(target.GalleryDropDown); int commits = 0; gallery.SelectionCommitted += (_, _) => commits++;
        f.Ribbon.Activate(target); var popup = f.Ribbon.Popups.Top!; f.Ribbon.Activate(popup.Targets.Single(t => t.Element == gallery.Items[1]), popup);
        Assert.Same(gallery.Items[1], gallery.SelectedItem); Assert.Equal(1, commits); Assert.Same(f.Font, gallery.Parent);
    });
    [Fact]
    public void Options_keep_mode_specific_drafts_grouped_and_share_new_structure_with_classic_page() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var host = new Form(); f.Show(host); f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        using var dialog = new RibbonQuickAccessCustomizationForm(f.Ribbon, "simple.test"); dialog.SelectedPage = dialog.ActiveRibbonPage; dialog.Show(host); Application.DoEvents();
        static T Child<T>(Control parent, string name) where T : Control => (T)parent.Controls.Find(name, true).Single();
        var page = dialog.SimplifiedEditor; Assert.Single(dialog.Pages, p => p.Text == "Customize Ribbon"); Assert.DoesNotContain(dialog.Pages, p => p.Name == "CustomizeRibbonOptionsPage"); Assert.False(dialog.RibbonEditor.ApplyChanges());
        var available = Child<ListBox>(page, "SimplifiedAvailableCommands"); Assert.Contains(available.Items.Cast<object>(), o => o.ToString()!.Contains("Clipboard"));
        Child<Button>(page, "SimplifiedNew").PerformClick(); page.ActionMenu!.Items.OfType<RibbonButtonItem>().Single(i => i.Text == "New tab").PerformClick(); page.ActionMenu.Close();
        Assert.Equal(3, page.Session.Draft.Ribbon!.Tabs.Count); Assert.Equal(2, f.Ribbon.Tabs.Count);
        Child<TextBox>(page, "SimplifiedAvailableSearch").Text = "Picture"; Child<ComboBox>(page, "SimplifiedSourceTab").SelectedIndex = 0; available.SelectedIndex = 1; Child<Button>(page, "SimplifiedAdd").PerformClick();
        Assert.Single(page.Session.Draft.Simplified!.Tabs); Assert.True(page.ApplyChanges(), Child<Label>(page, "SimplifiedCustomizationStatus").Text); Assert.Equal(3, f.Ribbon.Tabs.Count);
        using var acceptGroup = new System.Windows.Forms.Timer { Interval = 30 }; acceptGroup.Tick += (_, _) => { if (Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.Name == "RibbonGroupDialog") is { } group) { acceptGroup.Stop(); ((Button)group.Controls.Find("AcceptGroup", true).Single()).PerformClick(); } };
        acceptGroup.Start(); Child<Button>(page, "SimplifiedNew").PerformClick(); page.ActionMenu!.Items.OfType<RibbonButtonItem>().Single(i => i.Text == "New group").PerformClick(); acceptGroup.Stop(); page.ActionMenu.Close(); var pending = page.Session.Draft.Ribbon.Tabs.Last().Groups.Count;
        f.Ribbon.LayoutMode = RibbonLayoutMode.Classic; Assert.Single(dialog.Pages, p => p.Text == "Customize Ribbon"); Assert.Contains(dialog.Pages, p => p.Name == "CustomizeRibbonOptionsPage"); Assert.False(page.ApplyChanges());
        f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; Assert.Equal(pending, page.Session.Draft.Ribbon!.Tabs.Last().Groups.Count); Assert.Same(dialog.ActiveRibbonPage, dialog.SelectedPage); Assert.DoesNotContain(page.Controls.Find("ApplyCustomization", true), c => c is Button);
        Child<Button>(dialog, "CancelCustomization").PerformClick(); Assert.Equal(3, f.Ribbon.Tabs.Count);
    });
}
