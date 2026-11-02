using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DesignerQuickAccessTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly DesignSurface Surface = new(typeof(RibbonForm));
        internal readonly IDesignerHost Host;
        internal readonly RibbonControl Ribbon;
        internal readonly RibbonGroup Group;
        internal readonly RibbonButtonItem Button;
        internal Fixture()
        {
            Host = (IDesignerHost)Surface.GetService(typeof(IDesignerHost))!;
            var form = (RibbonForm)Host.RootComponent; form.Site!.Name = "form"; form.Name = "form";
            Ribbon = Create<RibbonControl>("ribbon"); form.Controls.Add(Ribbon); form.Ribbon = Ribbon;
            var tab = Create<RibbonTab>("homeTab"); tab.Text = "Home"; Ribbon.Tabs.Add(tab);
            Group = Create<RibbonGroup>("commandsGroup"); Group.Text = "Commands"; tab.Groups.Add(Group);
            Button = Create<RibbonButtonItem>("applyButton"); Button.Text = "Apply"; Button.Id = "apply"; Group.Items.Add(Button);
        }
        internal T Create<T>(string name) where T : class, IComponent => (T)Host.CreateComponent(typeof(T), name);
        internal RibbonDesignSnapshot Read() => RibbonDesignSnapshot.Deserialize(RibbonDesignCommitter.Read(Ribbon, Host).Serialize());
        internal bool Apply(RibbonDesignSnapshot draft) => RibbonDesignCommitter.Apply(Ribbon, Host, draft);
        internal RibbonEditorDialog Editor(RibbonDesignSnapshot draft) => new(draft, value => { Apply(value); return null; });
        public void Dispose() => Surface.Dispose();
    }
    private static T Control<T>(Form dialog, string name) where T : Control => (T)dialog.Controls.Find(name, true).Single();
    private static string Value(RibbonDesignNode node, string name) => node.Properties.Single(property => property.Name == name).Value;
    private static void Value(RibbonDesignNode node, string name, string value) => node.Properties.Single(property => property.Name == name).Value = value;
    private static TreeNode Node(TreeNode root, string key) => root.Nodes.Cast<TreeNode>().SelectMany(Descendants).First(tree => tree.Tag is RibbonDesignNode node && node.Key == key);
    private static IEnumerable<TreeNode> Descendants(TreeNode tree) { yield return tree; foreach (TreeNode child in tree.Nodes) foreach (var descendant in Descendants(child)) yield return descendant; }

    [Fact]
    public void Qat_branch_creates_detached_commands_and_cancel_keeps_the_form_untouched() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); int components = f.Host.Container.Components.Count;
        using var dialog = f.Editor(draft); dialog.Show(); Application.DoEvents(); var tree = Control<TreeView>(dialog, "StructureTree"); _ = tree.Handle;
        var qat = tree.Nodes[0].Nodes["QuickAccessRoot"]!; tree.SelectedNode = qat;
        Assert.Null(qat.Tag); Assert.False(Control<ToolStrip>(dialog, "StructureTools").Items["Remove"]!.Enabled);
        Control<Button>(dialog, "NewQatButton").PerformClick(); Control<Button>(dialog, "NewQatToggle").PerformClick();
        Assert.Equal(new[] { "Button", "Toggle" }, draft.CatalogCommands.Select(node => node.Kind)); Assert.Equal(2, draft.QuickAccessItems.Count);
        Assert.All(draft.QuickAccessItems, entry => Assert.Contains(draft.CatalogCommands, source => source.Key == entry.SourceKey));
        Assert.Equal(draft.AllNodes.Count(), draft.AllNodes.Select(node => node.ComponentName).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        draft.Validate(); Assert.Equal(draft.Serialize(), RibbonDesignSnapshot.Deserialize(draft.Serialize()).Serialize());
        dialog.DialogResult = DialogResult.Cancel;
        Assert.Empty(f.Ribbon.QuickAccessItems); Assert.Empty(f.Ribbon.CommandCatalog.Commands); Assert.Equal(components, f.Host.Container.Components.Count);
    });

    [Theory]
    [InlineData("Button")] [InlineData("Toggle")] [InlineData("CheckBox")] [InlineData("RadioButton")]
    [InlineData("DropDown")] [InlineData("SplitButton")] [InlineData("ComboBox")] [InlineData("Gallery")] [InlineData("Group")]
    public void Existing_commands_use_the_matching_reference_without_changing_their_owner(string kind) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture();
        var type = kind switch { "Button" => typeof(RibbonButtonItem), "Toggle" => typeof(RibbonToggleItem), "CheckBox" => typeof(RibbonCheckBoxItem), "RadioButton" => typeof(RibbonRadioButtonItem),
            "DropDown" => typeof(RibbonDropDownItem), "SplitButton" => typeof(RibbonSplitButtonItem), "ComboBox" => typeof(RibbonComboBoxItem), "Gallery" => typeof(RibbonGalleryItem), _ => typeof(RibbonGroup) };
        var source = kind == "Group" ? (RibbonElement)f.Group : (RibbonElement)f.Host.CreateComponent(type, "sourceCommand");
        if (source is RibbonItem item) f.Group.Items.Add(item);
        var parent = source.Parent; var tab = f.Ribbon.SelectedTab; var draft = f.Read(); using var editor = f.Editor(draft);
        editor.AddQuickAccessReference(RibbonDesignCommitter.Key(source)); Assert.True(f.Apply(draft));
        var reference = Assert.Single(f.Ribbon.QuickAccessItems); Assert.Same(source, RibbonQuickAccess.Source(reference)); Assert.Same(parent, source.Parent);
        Assert.Same(tab, f.Ribbon.SelectedTab); Assert.Empty(f.Ribbon.CommandCatalog.Commands); Assert.False(f.Apply(f.Read()));
    });

    [Fact]
    public void New_qat_commands_have_no_tab_slot_and_removing_a_reference_preserves_the_source() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); using (var editor = f.Editor(draft)) editor.AddQuickAccessCommand(true);
        Value(draft.CatalogCommands[0], "Text", "QAT-only Undo"); Value(draft.QuickAccessItems[0], "KeyTip", "U");
        draft.CatalogCommands[0].Icon = new() { Kind = "Svg", Source = "stock:undo", UseForegroundColor = true };
        Assert.True(f.Apply(draft)); var source = Assert.IsType<RibbonToggleItem>(Assert.Single(f.Ribbon.CommandCatalog.Commands)); var reference = Assert.Single(f.Ribbon.QuickAccessItems);
        int clicks = 0; source.Click += (_, _) => clicks++; source.PerformClick(); Assert.Equal(1, clicks);
        Assert.Same(source, RibbonQuickAccess.Source(reference)); Assert.Equal("U", reference.KeyTip); Assert.Single(f.Group.Items);
        var remove = f.Read(); remove.QuickAccessItems.Clear(); Assert.True(f.Apply(remove));
        Assert.Same(source, Assert.Single(f.Ribbon.CommandCatalog.Commands)); Assert.False(source.IsDisposed); Assert.True(reference.IsDisposed);
        Assert.Contains(f.Ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.QuickAccess), entry => ReferenceEquals(entry.Source, source));
        var restore = f.Read(); using (var editor = f.Editor(restore)) editor.AddQuickAccessReference(RibbonDesignCommitter.Key(source)); f.Apply(restore);
        source.PerformClick(); Assert.Equal(2, clicks); Assert.Same(source, RibbonQuickAccess.Source(Assert.Single(f.Ribbon.QuickAccessItems)));
    });

    [Fact]
    public void Removing_legacy_owned_qat_commands_moves_the_same_model_into_the_catalog() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var owned = f.Create<RibbonButtonItem>("legacySave"); owned.Text = "Save"; f.Ribbon.QuickAccessItems.Add(owned);
        int clicks = 0; owned.Click += (_, _) => clicks++; var draft = f.Read();
        Assert.Equal("OwnedQat", Assert.Single(draft.QuickAccessItems).Kind); Assert.Single(draft.CatalogCommands); Assert.False(f.Apply(draft));
        using (var editor = f.Editor(draft))
        {
            var tree = Control<TreeView>(editor, "StructureTree"); _ = tree.Handle; tree.SelectedNode = Node(tree.Nodes[0], draft.QuickAccessItems[0].Key);
            var remove = Control<ToolStrip>(editor, "StructureTools").Items["Remove"]!; Assert.Equal("Remove from QAT", remove.Text); remove.PerformClick();
        }
        Assert.Empty(draft.QuickAccessItems); Assert.Single(draft.CatalogCommands); f.Apply(draft);
        Assert.Same(owned, Assert.Single(f.Ribbon.CommandCatalog.Commands)); Assert.Empty(f.Ribbon.QuickAccessItems); owned.PerformClick(); Assert.Equal(1, clicks);
    });

    [Fact]
    public void Delete_is_blocked_while_referenced_then_uses_the_designer_host_after_removal() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); using (var editor = f.Editor(draft)) editor.AddQuickAccessCommand(false); f.Apply(draft);
        var source = Assert.Single(f.Ribbon.CommandCatalog.Commands); var editing = f.Read();
        using (var editor = f.Editor(editing))
        {
            var tree = Control<TreeView>(editor, "StructureTree"); _ = tree.Handle; tree.SelectedNode = Node(tree.Nodes[0], editing.CatalogCommands[0].Key);
            Assert.False(Control<ToolStrip>(editor, "StructureTools").Items["Remove"]!.Enabled);
        }
        editing.CatalogCommands.Clear(); Assert.Throws<ArgumentException>(() => f.Apply(editing)); Assert.False(source.IsDisposed);
        editing.QuickAccessItems.Clear(); Assert.True(f.Apply(editing)); Assert.True(source.IsDisposed); Assert.Empty(f.Ribbon.CommandCatalog.Commands); Assert.Empty(f.Ribbon.QuickAccessItems);
    });

    [Fact]
    public void Reordering_aliases_and_toolbar_settings_preserve_reference_and_source_identity() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); using (var editor = f.Editor(draft)) { editor.AddQuickAccessReference(RibbonDesignCommitter.Key(f.Button)); editor.AddQuickAccessCommand(false); }
        f.Apply(draft); var first = f.Ribbon.QuickAccessItems[0]; var second = f.Ribbon.QuickAccessItems[1]; var source = RibbonQuickAccess.Source(second);
        var editing = f.Read(); using (var editor = f.Editor(editing))
        {
            var tree = Control<TreeView>(editor, "StructureTree"); _ = tree.Handle; tree.SelectedNode = Node(tree.Nodes[0], editing.QuickAccessItems[1].Key);
            Control<ToolStrip>(editor, "StructureTools").Items["MoveUp"]!.PerformClick();
            tree.SelectedNode = tree.Nodes[0].Nodes["QuickAccessRoot"]!;
            var properties = Control<PropertyGrid>(editor, "ComponentProperties").SelectedObject!;
            TypeDescriptor.GetProperties(properties)["Visible"]!.SetValue(properties, false);
            var position = TypeDescriptor.GetProperties(properties)["Position"]!; position.SetValue(properties, Enum.Parse(position.PropertyType, "TitleBar"));
        }
        Value(editing.QuickAccessItems[0], "Text", "Toolbar alias"); f.Apply(editing);
        Assert.Same(second, f.Ribbon.QuickAccessItems[0]); Assert.Same(first, f.Ribbon.QuickAccessItems[1]); Assert.Same(source, RibbonQuickAccess.Source(second));
        Assert.Equal("Toolbar alias", second.Text); Assert.False(f.Ribbon.QuickAccessVisible); Assert.Equal(RibbonQuickAccessPosition.TitleBar, f.Ribbon.QuickAccessPosition);
    });

    [Theory]
    [InlineData("source")] [InlineData("kind")] [InlineData("id")] [InlineData("property")] [InlineData("position")]
    public void Invalid_qat_drafts_are_rejected_before_mutation(string condition) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); using (var editor = f.Editor(draft)) editor.AddQuickAccessReference(RibbonDesignCommitter.Key(f.Button));
        switch (condition)
        {
            case "source": draft.QuickAccessItems[0].SourceKey = "foreign"; draft.QuickAccessSources.Add(new() { Key = "foreign", Kind = "Button" }); break;
            case "kind": draft.QuickAccessItems[0].Kind = "QatComboReference"; break;
            case "id": Value(draft.QuickAccessItems[0], "Id", f.Button.Id); break;
            case "property": draft.QuickAccessItems[0].Properties.Add(new() { Name = "Unknown", Value = "true" }); break;
            case "position": draft.QuickAccessPosition = "1"; break;
        }
        Assert.Throws<ArgumentException>(() => f.Apply(draft)); Assert.Empty(f.Ribbon.QuickAccessItems); Assert.Empty(f.Ribbon.CommandCatalog.Commands); Assert.Single(f.Group.Items);
    });

    [Theory]
    [InlineData("entry")] [InlineData("catalog")] [InlineData("position")]
    public void External_qat_or_catalog_changes_make_an_open_draft_stale(string change) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read();
        if (change == "entry") f.Ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = f.Button });
        if (change == "catalog") f.Ribbon.CommandCatalog.Commands.Add(new RibbonButtonItem { Text = "External" });
        if (change == "position") f.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        Assert.Throws<ArgumentException>(() => f.Apply(draft)); Assert.Throws<ArgumentException>(() => RibbonThemePreviewRenderer.Render(f.Ribbon, f.Host, draft));
    });

    [Fact]
    public void Qat_catalog_ribbon_and_settings_edits_use_one_framework_undo_redo_unit() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read();
        ((IServiceContainer)f.Surface.GetService(typeof(IServiceContainer))!).AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(f.Host));
        using var undo = new UndoRecorder(f.Host);
        using (var editor = f.Editor(draft)) { editor.AddQuickAccessCommand(true); editor.AddQuickAccessReference(RibbonDesignCommitter.Key(f.Button)); }
        Value(draft.Nodes.Single(node => node.ComponentName == "applyButton"), "Text", "Edited Apply"); draft.QuickAccessPosition = "BelowRibbon";
        f.Apply(draft); Assert.Equal(1, undo.Count); Assert.Equal(2, f.Ribbon.QuickAccessItems.Count);
        undo.Toggle(); Assert.Empty(f.Ribbon.QuickAccessItems); Assert.Empty(f.Ribbon.CommandCatalog.Commands); Assert.Equal("Apply", f.Button.Text); Assert.Equal(RibbonQuickAccessPosition.TabRow, f.Ribbon.QuickAccessPosition);
        undo.Toggle(); Assert.Equal(2, f.Ribbon.QuickAccessItems.Count); Assert.Single(f.Ribbon.CommandCatalog.Commands); Assert.Equal("Edited Apply", f.Button.Text);
        Assert.Same(f.Button, RibbonQuickAccess.Source(f.Ribbon.QuickAccessItems[1])); Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, f.Ribbon.QuickAccessPosition);
    });

    [Fact]
    public void Retargeting_an_existing_reference_preserves_the_entry_and_undo_restores_its_source() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var alternate = f.Create<RibbonButtonItem>("alternate"); alternate.Text = "Alternate"; f.Ribbon.CommandCatalog.Commands.Add(alternate);
        var reference = f.Create<RibbonQuickAccessReference>("applyReference"); reference.Source = f.Button; reference.Text = "Alias"; f.Ribbon.QuickAccessItems.Add(reference);
        var draft = f.Read(); ((IServiceContainer)f.Surface.GetService(typeof(IServiceContainer))!).AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(f.Host));
        using var undo = new UndoRecorder(f.Host); using (var editor = f.Editor(draft)) editor.ChangeQuickAccessSource(draft.QuickAccessItems[0].Key, RibbonDesignCommitter.Key(alternate));
        f.Apply(draft); Assert.Same(reference, Assert.Single(f.Ribbon.QuickAccessItems)); Assert.Same(alternate, reference.Source); Assert.Equal("Alias", reference.Text);
        undo.Toggle(); Assert.Same(reference, Assert.Single(f.Ribbon.QuickAccessItems)); Assert.Same(f.Button, reference.Source);
        undo.Toggle(); Assert.Same(reference, Assert.Single(f.Ribbon.QuickAccessItems)); Assert.Same(alternate, reference.Source);
    });

    [Fact]
    public void Removing_a_reference_can_be_undone_without_replacing_its_existing_source() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); using (var editor = f.Editor(draft)) editor.AddQuickAccessReference(RibbonDesignCommitter.Key(f.Button)); f.Apply(draft);
        var editing = f.Read(); ((IServiceContainer)f.Surface.GetService(typeof(IServiceContainer))!).AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(f.Host));
        using var undo = new UndoRecorder(f.Host); editing.QuickAccessItems.Clear(); f.Apply(editing); Assert.Empty(f.Ribbon.QuickAccessItems);
        undo.Toggle(); Assert.Same(f.Button, RibbonQuickAccess.Source(Assert.Single(f.Ribbon.QuickAccessItems)));
        undo.Toggle(); Assert.Empty(f.Ribbon.QuickAccessItems); Assert.False(f.Button.IsDisposed);
    });

    [Fact]
    public void Static_preview_draws_detached_qat_sources_and_honors_visibility_without_invoking_commands() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var recorder = new QatRecorder(); f.Ribbon.Renderer = recorder; int clicks = 0; f.Button.Click += (_, _) => clicks++;
        var draft = f.Read(); using (var editor = f.Editor(draft)) { editor.AddQuickAccessReference(RibbonDesignCommitter.Key(f.Button)); editor.AddQuickAccessCommand(true); }
        draft.CatalogCommands[0].Icon = new() { Kind = "Svg", Source = "stock:undo", UseForegroundColor = true }; draft.QuickAccessPosition = "BelowRibbon";
        var result = RibbonThemePreviewRenderer.Render(f.Ribbon, f.Host, draft);
        Assert.Equal(2, recorder.Sources.Count); Assert.All(recorder.Sources, source => Assert.NotSame(f.Button, source)); Assert.Equal(0, clicks);
        Assert.Empty(f.Ribbon.QuickAccessItems); Assert.Empty(f.Ribbon.CommandCatalog.Commands); Assert.False(f.Ribbon.IsBackstageOpen);
        if (Environment.GetEnvironmentVariable("RIBBONKIT_QAT_CAPTURE") is { Length: > 0 } directory)
        { Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory, "qat-static-preview.png"), Convert.FromBase64String(result.RibbonPng)); }
        recorder.Sources.Clear(); draft.QuickAccessVisible = false; RibbonThemePreviewRenderer.Render(f.Ribbon, f.Host, draft); Assert.Empty(recorder.Sources);
    });

    [Fact]
    public void Accepted_qat_commands_and_references_are_selected_for_normal_designer_properties() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); using (var editor = f.Editor(draft)) editor.AddQuickAccessCommand(false);
        var selectedTab = f.Ribbon.SelectedTab; f.Apply(draft);
        var selection = (ISelectionService)f.Surface.GetService(typeof(ISelectionService))!; using var policy = new RibbonDesignSelection(f.Ribbon, selection);
        policy.SelectDraft(draft); Assert.Same(Assert.Single(f.Ribbon.CommandCatalog.Commands), selection.PrimarySelection); Assert.Same(selectedTab, f.Ribbon.SelectedTab);
        draft.SelectionKey = draft.QuickAccessItems[0].Key; policy.SelectDraft(draft); Assert.Same(Assert.Single(f.Ribbon.QuickAccessItems), selection.PrimarySelection);
    });

    [Fact]
    public void Serialization_writes_catalog_commands_and_named_source_references() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); using (var editor = f.Editor(draft)) editor.AddQuickAccessCommand(false); f.Apply(draft);
        var source = Assert.Single(f.Ribbon.CommandCatalog.Commands); var entry = Assert.Single(f.Ribbon.QuickAccessItems);
        var serialization = new DesignerSerializationManager(f.Host); using var session = serialization.CreateSession(); var manager = (IDesignerSerializationManager)serialization;
        manager.Context.Push(new CodeTypeDeclaration("QatDesignerForm")); manager.Context.Push(new RootContext(new CodeThisReferenceExpression(), f.Host.RootComponent));
        manager.SetName(f.Ribbon, "ribbon"); manager.SetName(source, source.Site!.Name!); manager.SetName(entry, entry.Site!.Name!);
        var serializer = (CodeDomSerializer)manager.GetSerializer(entry.GetType(), typeof(CodeDomSerializer))!;
        var statements = serializer.SerializeMember(manager, entry, TypeDescriptor.GetProperties(entry)["Source"]!).OfType<CodeAssignStatement>();
        var assignment = Assert.Single(statements, statement => statement.Left is CodePropertyReferenceExpression { PropertyName: "Source" });
        Assert.Equal(source.Site.Name, Assert.IsType<CodeFieldReferenceExpression>(assignment.Right).FieldName);
        var ribbonSerializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonControl), typeof(CodeDomSerializer))!;
        var catalog = ribbonSerializer.SerializeMember(manager, f.Ribbon, TypeDescriptor.GetProperties(f.Ribbon)[nameof(f.Ribbon.CommandCatalog)]!);
        var qat = ribbonSerializer.SerializeMember(manager, f.Ribbon, TypeDescriptor.GetProperties(f.Ribbon)[nameof(f.Ribbon.QuickAccessItems)]!);
        Assert.NotEmpty(catalog.Cast<CodeStatement>()); Assert.NotEmpty(qat.Cast<CodeStatement>()); Assert.Empty(serialization.Errors.Cast<object>());
    });

    [Theory]
    [InlineData(1f)] [InlineData(1.25f)] [InlineData(1.5f)] [InlineData(2f)]
    public void Qat_branch_actions_and_source_picker_fit_at_representative_scales(float scale) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); using var dialog = f.Editor(draft); dialog.AddQuickAccessCommand(false);
        _ = dialog.Handle; dialog.Scale(new SizeF(scale, scale)); dialog.Show(); Application.DoEvents(); var tree = Control<TreeView>(dialog, "StructureTree"); _ = tree.Handle;
        tree.SelectedNode = tree.Nodes[0].Nodes["QuickAccessRoot"]!;
        foreach (var size in new[] { new Size(780, 520), new Size(1100, 760) })
        {
            dialog.ClientSize = new((int)(size.Width * scale), (int)(size.Height * scale)); dialog.PerformLayout();
            var help = Control<GroupBox>(dialog, "QuickAccessHelp"); Assert.True(help.Visible);
            foreach (var button in new[] { "AddQatExisting", "NewQatButton", "NewQatToggle", "EditQatSource", "ChangeQatSource" })
            { var control = Control<Button>(dialog, button); Assert.True(control.Right <= control.Parent!.ClientSize.Width); Assert.True(control.Bottom <= control.Parent.ClientSize.Height); }
        }
        using var picker = dialog.CreateQuickAccessPicker(); var choices = Control<ListBox>(picker, "QatSourceChoices"); Assert.True(choices.Items.Count >= 3);
        Control<TextBox>(picker, "QatSourceSearch").Text = "Apply"; Assert.Single(choices.Items.Cast<object>());
        if (scale == 1 && Environment.GetEnvironmentVariable("RIBBONKIT_QAT_CAPTURE") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); dialog.Show(); Application.DoEvents();
            using (var pixels = new Bitmap(dialog.Width, dialog.Height)) { dialog.DrawToBitmap(pixels, new(Point.Empty, pixels.Size)); pixels.Save(Path.Combine(directory, "qat-editor.png")); }
            picker.Show(); Application.DoEvents(); using (var pixels = new Bitmap(picker.Width, picker.Height)) { picker.DrawToBitmap(pixels, new(Point.Empty, pixels.Size)); pixels.Save(Path.Combine(directory, "qat-source-picker.png")); }
        }
    });
    private sealed class UndoRecorder(IServiceProvider services) : UndoEngine(services)
    {
        private readonly List<UndoUnit> _units = [];
        internal int Count => _units.Count;
        internal void Toggle() => Assert.Single(_units).Undo();
        protected override void AddUndoUnit(UndoUnit unit) => _units.Add(unit);
    }
    private sealed class QatRecorder : RibbonOffice2024Renderer
    {
        internal readonly List<RibbonElement?> Sources = [];
        public override void DrawQuickAccessItem(RibbonPaintContext context, RibbonElement entry, bool menu)
        { Sources.Add(RibbonQuickAccess.Source(entry)); base.DrawQuickAccessItem(context, entry, menu); }
    }
}
