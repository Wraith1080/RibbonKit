using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DesignerRearrangementTests
{
    [Theory]
    [InlineData(RibbonGroupLayout.TwoRows, "Second")] [InlineData(RibbonGroupLayout.ThreeRows, "Third")]
    public void Move_picker_reparents_across_tabs_with_an_explicit_row_and_preserves_command_identity(RibbonGroupLayout mode, string row) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var source = ribbon.Tabs[0].Groups[0]; var command = source.Items[0]; var policy = command.SizePolicy; int clicks = 0; ((RibbonButtonItem)command).Click += (_, _) => clicks++;
        var tab = new RibbonTab { Text = "Other" }; var group = new RibbonGroup { Text = "Rows", Layout = mode };
        host.Container.Add(tab, "otherTab"); host.Container.Add(group, "rowGroup"); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var reference = new RibbonQuickAccessReference { Source = (RibbonButtonItem)command }; ribbon.QuickAccessItems.Add(reference);
        var draft = RibbonDesignCommitter.Read(ribbon, host); var node = draft.Nodes.Single(n => n.ComponentName == command.Site!.Name);
        using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(ribbon, host, value); return null; });
        var tree = Control<TreeView>(dialog, "StructureTree"); _ = tree.Handle; tree.SelectedNode = Nodes(tree.Nodes[0]).Single(t => t.Tag == node);
        Assert.True(Control<ToolStrip>(dialog, "StructureTools").Items["MoveTo"]!.Enabled);
        using var picker = dialog.CreateMoveDialog(); picker.StartPosition = FormStartPosition.Manual; picker.Location = new(-16000, -16000); picker.Show(); Application.DoEvents();
        var destination = Control<ComboBox>(picker, "MoveDestination");
        destination.SelectedItem = destination.Items.Cast<object>().Single(item => item.ToString()!.Contains("(rowGroup)"));
        var rows = Control<ComboBox>(picker, "MoveRow"); Assert.True(rows.Enabled);
        Assert.Equal(mode == RibbonGroupLayout.TwoRows ? new[] { "First", "Second" } : new[] { "First", "Second", "Third" }, rows.Items.Cast<string>());
        rows.SelectedItem = row; Control<Button>(picker, "AcceptMove").PerformClick();
        Assert.Equal(DialogResult.OK, picker.DialogResult); Assert.Equal(row, node.Properties.Single(p => p.Name == "Row").Value);
        Assert.Same(node, draft.Tabs[1].Children[0].Children.Single()); Assert.DoesNotContain(node, draft.Tabs[0].Children[0].Children);
        Assert.Same(source, command.Parent); Assert.Equal(policy, command.SizePolicy); Assert.Equal(0, host.Transactions);
        if (Environment.GetEnvironmentVariable("RIBBONKIT_NATIVE_CAPTURE_DIR") is { Length: > 0 } output)
        { Directory.CreateDirectory(output); using var image = new Bitmap(picker.Width, picker.Height); picker.DrawToBitmap(image, new Rectangle(Point.Empty, picker.Size)); image.Save(Path.Combine(output, $"move-to-{mode}.png")); }
        dialog.AcceptDraft(); Assert.Equal(1, host.Committed); Assert.Same(group, command.Parent); Assert.Same(command, reference.Source);
        Assert.Equal(policy, command.SizePolicy); Assert.Equal(Enum.Parse<RibbonItemRow>(row), command.Row); ((RibbonButtonItem)command).PerformClick(); Assert.Equal(1, clicks);
        Assert.Equal(node.Key, RibbonDesignCommitter.Read(ribbon, host).Nodes.Single(n => n.ComponentName == node.ComponentName).Key);
    });

    [Fact]
    public void Group_moves_preserve_its_layout_and_cancel_invalid_and_cyclic_moves_leave_the_live_tree_untouched() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var group = ribbon.Tabs[0].Groups[0]; group.Layout = RibbonGroupLayout.ThreeRows;
        var tab = new RibbonTab { Text = "Other" }; host.Container.Add(tab, "otherTab"); ribbon.Tabs.Add(tab);
        var draft = RibbonDesignCommitter.Read(ribbon, host); var groupNode = draft.Tabs[0].Children.Single();
        using var dialog = new RibbonEditorDialog(draft, _ => throw new InvalidOperationException("Cancel must not apply"));
        var tree = Control<TreeView>(dialog, "StructureTree"); _ = tree.Handle; tree.SelectedNode = Nodes(tree.Nodes[0]).Single(n => n.Tag == groupNode);
        Assert.False(dialog.MoveSelectionTo(groupNode.Key)); Assert.False(dialog.MoveSelectionTo(groupNode.Children[0].Key));
        Assert.True(dialog.MoveSelectionTo(draft.Tabs[1].Key)); Assert.Empty(draft.Tabs[0].Children); Assert.Same(groupNode, draft.Tabs[1].Children.Single());
        Assert.Equal("ThreeRows", groupNode.Properties.Single(p => p.Name == "Layout").Value);
        dialog.DialogResult = DialogResult.Cancel; Assert.Same(ribbon.Tabs[0], group.Parent); Assert.Equal(0, host.Transactions);
    });

    [Fact]
    public void Reparenting_and_row_assignment_undo_and_redo_together_without_replacing_components() => FoundationTests.Sta(() =>
    {
        using var surface = new DesignSurface(typeof(Form)); var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var ribbon = (RibbonControl)host.CreateComponent(typeof(RibbonControl), "ribbon"); var tab = (RibbonTab)host.CreateComponent(typeof(RibbonTab), "tab"); ribbon.Tabs.Add(tab);
        var source = (RibbonGroup)host.CreateComponent(typeof(RibbonGroup), "sourceGroup"); var target = (RibbonGroup)host.CreateComponent(typeof(RibbonGroup), "targetGroup"); target.Layout = RibbonGroupLayout.TwoRows; tab.Groups.AddRange(source, target);
        var combo = (RibbonComboBoxItem)host.CreateComponent(typeof(RibbonComboBoxItem), "font"); combo.Text = "Font"; source.Items.Add(combo);
        ((IServiceContainer)surface.GetService(typeof(IServiceContainer))!).AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(host));
        using var undo = new Recorder(host); var draft = RibbonDesignCommitter.Read(ribbon, host);
        using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(ribbon, host, value); return null; });
        var tree = Control<TreeView>(dialog, "StructureTree"); _ = tree.Handle; tree.SelectedNode = Nodes(tree.Nodes[0]).Single(n => n.Tag is RibbonDesignNode { Kind: "ComboBox" });
        Assert.True(dialog.MoveSelectionTo(draft.Tabs[0].Children[1].Key, "Third")); // TwoRows maps the third slot to its last row.
        draft.Nodes.Single(n => n.Kind == "ComboBox").Properties.Single(p => p.Name == "ShowLabel").Value = "False";
        dialog.AcceptDraft(); Assert.Equal(1, undo.Count); Assert.Same(target, combo.Parent); Assert.Equal(RibbonItemRow.Second, combo.Row); Assert.False(combo.ShowLabel);
        undo.Toggle(); Assert.Same(source, combo.Parent); Assert.Equal(RibbonItemRow.First, combo.Row); Assert.True(combo.ShowLabel);
        undo.Toggle(); Assert.Same(target, combo.Parent); Assert.Equal(RibbonItemRow.Second, combo.Row); Assert.False(combo.ShowLabel); Assert.Equal("Font", combo.Text);
        var property = TypeDescriptor.GetProperties(combo)[nameof(combo.ShowLabel)]!; var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
        var manager = (IDesignerSerializationManager)serialization; manager.SetName(combo, "font");
        var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonComboBoxItem), typeof(CodeDomSerializer))!;
        var assignment = Assert.Single(serializer.SerializeMember(manager, combo, property).OfType<CodeAssignStatement>());
        Assert.Equal(false, Assert.IsType<CodePrimitiveExpression>(assignment.Right).Value); Assert.Empty(serialization.Errors.Cast<object>());
        property.ResetValue(combo); Assert.True(combo.ShowLabel); Assert.False(property.ShouldSerializeValue(combo));
    });

    [Fact]
    public void Existing_nonmonotonic_policy_is_preserved_without_offering_other_growing_sequences() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var command = ribbon.Tabs[0].Groups[0].Items[0]; command.SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Small, RibbonItemSize.Large, RibbonItemSize.Medium);
        var draft = RibbonDesignCommitter.Read(ribbon, host); var property = draft.Nodes.Single(n => n.Kind == "Button").Properties.Single(p => p.Name == "SizePolicy");
        Assert.Equal(command.SizePolicy.ToString(), property.Value); Assert.Equal(11, property.Options.Length);
        Assert.All(property.Options.Where(value => value != property.Value), value =>
        { var policy = (RibbonSizePolicy)TypeDescriptor.GetConverter(typeof(RibbonSizePolicy)).ConvertFromInvariantString(value)!; Assert.True(policy.Large <= policy.Medium && policy.Medium <= policy.Small); });
        Assert.False(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(0, host.Transactions);
    });
    private static T Control<T>(Control root, string name) where T : Control => (T)root.Controls.Find(name, true).Single();
    private static IEnumerable<TreeNode> Nodes(TreeNode root) => new[] { root }.Concat(root.Nodes.Cast<TreeNode>().SelectMany(Nodes));
    private sealed class Recorder(IServiceProvider services) : UndoEngine(services)
    {
        private readonly List<UndoUnit> _units = [];
        internal int Count => _units.Count;
        internal void Toggle() => Assert.Single(_units).Undo();
        protected override void AddUndoUnit(UndoUnit unit) => _units.Add(unit);
    }
}
