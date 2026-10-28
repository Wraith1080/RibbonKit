using System.Text.Json;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class CustomizationTests
{
    [Fact]
    public void Catalog_separates_group_availability_and_excludes_transient_and_surface_content() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture();
        var nested = new RibbonButtonItem { Id = "cmd.nested", Text = "Popup only" }; f.Menu.Items.Add(nested);
        f.Ribbon.FileMenu.Items.Add(new RibbonButtonItem { Id = "cmd.file", Text = "File command" });
        f.Ribbon.Backstage.Items.Add(new RibbonButtonItem { Id = "cmd.backstage", Text = "Backstage" });
        var message = new RibbonMessage { Text = "Notice" }; message.Action.Id = "cmd.notice"; f.Ribbon.MessageBar.Items.Add(message);
        var modal = new RibbonTab { IsModal = true }; var modalGroup = new RibbonGroup(); modalGroup.Items.Add(new RibbonButtonItem { Id = "cmd.modal" }); modal.Groups.Add(modalGroup); f.Ribbon.Tabs.Add(modal);
        using var merged = new RibbonMergeSource(); var mergedTab = new RibbonTab(); var mergedGroup = new RibbonGroup(); var temporary = new RibbonButtonItem { Id = "cmd.temp" }; mergedGroup.Items.Add(temporary); mergedTab.Groups.Add(mergedGroup); merged.Tabs.Add(mergedTab);
        f.Ribbon.Merge(merged); f.Ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = temporary });
        var ribbon = f.Ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.Ribbon);
        var qat = f.Ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.QuickAccess);
        Assert.DoesNotContain(ribbon, e => e.IsGroup); Assert.Contains(qat, e => e.Source == f.Group && e.IsGroup);
        Assert.Contains(ribbon, e => e.Source == f.Field && !e.CanAddToQuickAccess); Assert.DoesNotContain(qat, e => e.Source == f.Field);
        Assert.Contains(ribbon, e => e.Source == f.Undo); Assert.Contains(ribbon, e => e.Source == f.Redo);
        Assert.Single(qat, e => e.Source == f.Bold);
        Assert.DoesNotContain(ribbon.Concat(qat), e => new[] { "cmd.nested", "cmd.file", "cmd.backstage", "cmd.notice", "cmd.modal", "cmd.temp" }.Contains(e.CommandId));
        var bold = ribbon.Single(e => e.Source == f.Bold); f.Bold.Text = "Emphasis"; Assert.Equal("Emphasis", bold.Text);
        Assert.All(qat, e => Assert.True(e.HasStableIdentity));
        Assert.IsType<RibbonQuickAccessComboBoxReference>(f.Ribbon.CommandCatalog.CreateQuickAccessEntry(f.Combo.Id));
        Assert.IsType<RibbonQuickAccessDropDownReference>(f.Ribbon.CommandCatalog.CreateQuickAccessEntry(f.Menu.Id));
        Assert.IsType<RibbonQuickAccessGroupReference>(f.Ribbon.CommandCatalog.CreateQuickAccessEntry(f.Group.Id));
    });

    [Fact]
    public void Draft_apply_cancel_reset_and_restart_preserve_sources_and_latest_applied_checkpoint() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var edit = f.Ribbon.CommandCatalog.BeginCustomization("test.app");
        int applied = 0; edit.Applied += (_, _) => applied++;
        var original = f.Ribbon.QuickAccessItems.ToArray();
        Assert.Equal(edit.Draft.QuickAccess[1].Id, f.Ribbon.CommandCatalog.GetQuickAccessEntryId(original[1]));
        edit.Draft.QuickAccess.RemoveAt(0); edit.Draft.QuickAccess.Add(new() { CommandId = f.Redo.Id }); edit.Draft.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        Assert.Equal(original, f.Ribbon.QuickAccessItems); Assert.Equal(RibbonQuickAccessPosition.TabRow, f.Ribbon.QuickAccessPosition);
        f.Bold.Checked = true;
        Assert.True(edit.Apply()); Assert.Equal(1, applied); Assert.Same(f.Ribbon.CommandCatalog, f.Undo.Parent);
        Assert.Contains(f.Ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.Ribbon), e => e.Source == f.Undo);
        Assert.Contains(f.Ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.QuickAccess), e => e.Source == f.Undo);
        Assert.Equal(new[] { f.Bold.Id, f.Redo.Id }, Sources(f.Ribbon));
        string json = edit.ExportJson(); Assert.DoesNotContain("Checked", json); Assert.DoesNotContain("Density", json);
        edit.Draft.QuickAccess.Clear(); edit.Cancel(); Assert.Equal(2, edit.Draft.QuickAccess.Count); Assert.Equal(new[] { f.Bold.Id, f.Redo.Id }, Sources(f.Ribbon));
        edit.Reset(); Assert.Equal(new[] { f.Undo.Id, f.Bold.Id }, edit.Draft.QuickAccess.Select(e => e.CommandId));
        Assert.True(edit.Apply()); Assert.Same(f.Undo, f.Ribbon.QuickAccessItems[0]); Assert.Same(f.Ribbon, f.Undo.Parent); Assert.True(f.Bold.Checked);
        edit.ImportJson(json); Assert.Equal(new[] { f.Undo.Id, f.Bold.Id }, Sources(f.Ribbon)); Assert.True(edit.Apply());
        using var restarted = new Fixture(); using var restored = restarted.Ribbon.CommandCatalog.BeginCustomization("test.app");
        restored.ImportJson(json); Assert.Equal(new[] { restarted.Undo.Id, restarted.Bold.Id }, Sources(restarted.Ribbon));
        Assert.True(restored.Apply()); Assert.Equal(new[] { restarted.Bold.Id, restarted.Redo.Id }, Sources(restarted.Ribbon));
        Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, restarted.Ribbon.QuickAccessPosition); Assert.False(restarted.Bold.Checked);
    });

    [Fact]
    public void Rejected_json_and_identity_conflicts_leave_both_live_models_and_draft_unchanged() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var edit = f.Ribbon.CommandCatalog.BeginCustomization("test.app"); string json = edit.ExportJson();
        var live = f.Ribbon.QuickAccessItems.ToArray(); var draft = edit.Draft;
        foreach (string bad in new[] { "{", "{}", "null", json.Replace("\"version\": 1", "\"version\": 99"), json.Replace("test.app", "other.app"),
            json.Replace("RibbonKit.WinForms.Customization", "RibbonKit.WPF"), json.Replace("\"version\": 1", "\"version\": 1, \"version\": 1"),
            json.Replace("cmd.bold", "cmd.missing"), json.Replace("\"TabRow\"", "99"), json.Insert(json.LastIndexOf('}'), ", \"unknown\": true") })
        {
            var error = Record.Exception(() => edit.ImportJson(bad));
            Assert.True(error is JsonException or InvalidOperationException or ArgumentException, bad);
            Assert.Same(draft, edit.Draft); Assert.Equal(live, f.Ribbon.QuickAccessItems);
        }
        edit.Draft.QuickAccess.Add(new() { Id = edit.Draft.QuickAccess[0].Id, CommandId = f.Redo.Id });
        Assert.Throws<InvalidOperationException>(() => edit.Apply()); Assert.Equal(live, f.Ribbon.QuickAccessItems); edit.Cancel();
        edit.Draft.QuickAccess.Add(new() { Id = f.Undo.Id, CommandId = f.Undo.Id });
        Assert.Throws<InvalidOperationException>(() => edit.Apply()); Assert.Equal(live, f.Ribbon.QuickAccessItems);
    });

    [Fact]
    public void Transient_qat_references_survive_apply_and_external_permanent_changes_require_reload() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var edit = f.Ribbon.CommandCatalog.BeginCustomization("test.app");
        using var merged = new RibbonMergeSource(); var tab = new RibbonTab(); var group = new RibbonGroup(); var command = new RibbonButtonItem { Id = "cmd.context" }; group.Items.Add(command); tab.Groups.Add(group); merged.Tabs.Add(tab);
        f.Ribbon.Merge(merged); var temporary = new RibbonQuickAccessReference { Source = command }; f.Ribbon.QuickAccessItems.Insert(1, temporary);
        edit.Draft.QuickAccess.Reverse(); Assert.True(edit.Apply()); Assert.Same(temporary, f.Ribbon.QuickAccessItems[1]); Assert.DoesNotContain(command.Id, edit.ExportJson());
        f.Ribbon.Unmerge(merged); edit.Draft.QuickAccessVisible = false; Assert.True(edit.Apply()); Assert.Contains(temporary, f.Ribbon.QuickAccessItems);
        f.Ribbon.QuickAccessItems.Add(f.Ribbon.CommandCatalog.CreateQuickAccessEntry(f.Copy.Id));
        var live = f.Ribbon.QuickAccessItems.ToArray(); Assert.Throws<InvalidOperationException>(() => edit.Apply()); Assert.Equal(live, f.Ribbon.QuickAccessItems);
        edit.Reload(); Assert.True(edit.Apply()); Assert.Contains(temporary, f.Ribbon.QuickAccessItems);
    });

    [Fact]
    public void Invalid_native_input_blocks_apply_and_valid_apply_keeps_the_editor_handle_and_value() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var form = new Form { Width = 1200, Height = 600 }; form.Controls.Add(f.Ribbon); form.Show(); Application.DoEvents();
        f.Field.Value = "Accepted"; f.Field.ValueValidating += (_, e) => e.Cancel = f.Field.Value.Length == 0;
        f.Field.PerformClick(); var editor = f.Ribbon.Editors.EditorFor(f.Field)!; nint handle = editor.Handle;
        using var edit = f.Ribbon.CommandCatalog.BeginCustomization("test.app"); edit.Draft.QuickAccess.Add(new() { CommandId = f.Group.Id });
        var live = f.Ribbon.QuickAccessItems.ToArray(); editor.Text = "";
        Assert.False(edit.Apply()); Assert.Equal(live, f.Ribbon.QuickAccessItems); Assert.True(editor.Focused);
        editor.Text = "Committed"; Assert.True(edit.Apply()); Assert.Equal(handle, editor.Handle); Assert.Equal("Committed", f.Field.Value);
        Assert.IsType<RibbonQuickAccessGroupReference>(f.Ribbon.QuickAccessItems.Last());
    });

    [Fact]
    public void A_validation_callback_cannot_silently_retarget_a_draft_source() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var form = new Form { Width = 1200, Height = 600 }; form.Controls.Add(f.Ribbon); form.Show(); Application.DoEvents();
        f.Field.Value = "Before"; f.Field.PerformClick(); var editor = f.Ribbon.Editors.EditorFor(f.Field)!;
        using var edit = f.Ribbon.CommandCatalog.BeginCustomization("test.app"); edit.Draft.QuickAccess.Add(new() { CommandId = f.Redo.Id });
        var live = f.Ribbon.QuickAccessItems.ToArray(); f.Field.ValueValidating += (_, _) => f.Redo.Id = "cmd.renamed";
        editor.Text = "After"; Assert.Throws<InvalidOperationException>(() => edit.Apply()); Assert.Equal(live, f.Ribbon.QuickAccessItems);
    });

    [Fact]
    public void Runtime_ids_work_for_drafts_but_export_requires_explicit_source_ids_and_failed_apply_rolls_back() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var source = new RibbonButtonItem { Text = "Undo" }; ribbon.QuickAccessItems.Add(source);
        using var edit = ribbon.CommandCatalog.BeginCustomization("test.app"); Assert.True(edit.Apply()); Assert.Throws<InvalidOperationException>(() => edit.ExportJson());
        source.Id = "cmd.undo"; edit.Reload(); Assert.Contains("cmd.undo", edit.ExportJson());
        var old = ribbon.QuickAccessItems.ToArray(); bool fail = true;
        ribbon.QuickAccessPositionChanged += (_, _) => { if (fail) { fail = false; throw new InvalidOperationException("Host callback failure"); } };
        edit.Draft.QuickAccess.Clear(); edit.Draft.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        Assert.Throws<InvalidOperationException>(() => edit.Apply()); Assert.Equal(old, ribbon.QuickAccessItems); Assert.Same(ribbon, source.Parent); Assert.Equal(RibbonQuickAccessPosition.TabRow, ribbon.QuickAccessPosition);
    });

    private static string[] Sources(RibbonControl ribbon) => ribbon.QuickAccessItems.Select(e => RibbonQuickAccess.Source(e)!.Id).ToArray();
    private sealed class Fixture : IDisposable
    {
        internal readonly RibbonControl Ribbon = new() { BindingContext = new BindingContext(), Width = 1200 };
        internal readonly RibbonGroup Group = new() { Id = "group.font", Text = "Font" };
        internal readonly RibbonToggleItem Bold = new() { Id = "cmd.bold", Text = "Bold" };
        internal readonly RibbonButtonItem Copy = new() { Id = "cmd.copy", Text = "Copy" }, Undo = new() { Id = "cmd.undo", Text = "Undo" }, Redo = new() { Id = "cmd.redo", Text = "Redo" };
        internal readonly RibbonComboBoxItem Combo = new() { Id = "cmd.font", Text = "Font" };
        internal readonly RibbonTextBoxItem Field = new() { Id = "cmd.name", Text = "Name" };
        internal readonly RibbonDropDownItem Menu = new() { Id = "cmd.menu", Text = "Menu" };
        internal Fixture()
        {
            var tab = new RibbonTab { Id = "tab.home", Text = "Home" }; tab.Groups.Add(Group); Ribbon.Tabs.Add(tab);
            Combo.Items.AddRange("Arial", "Calibri"); Combo.SelectedIndex = 0;
            Group.Items.AddRange(Bold, Copy, Combo, Field, Menu);
            Ribbon.CommandCatalog.Commands.Add(Redo);
            Ribbon.QuickAccessItems.AddRange(Undo, new RibbonQuickAccessReference { Id = "qat.bold", Source = Bold, Text = "Strong emphasis" });
        }
        public void Dispose() => Ribbon.Dispose();
    }
}
