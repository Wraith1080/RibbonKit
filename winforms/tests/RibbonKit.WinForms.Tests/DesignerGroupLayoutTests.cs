using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DesignerGroupLayoutTests
{
    [Theory]
    [InlineData("TwoRows")] [InlineData("ThreeRows")]
    public void Orientation_rows_and_group_separator_preview_detached_then_apply_in_one_transaction(string layout) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var group = ribbon.Tabs[0].Groups[0]; var button = group.Items[0]; button.SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium);
        var split = new RibbonSplitButtonItem(); host.Container.Add(split, "split"); group.Items.Add(split);
        var draft = RibbonDesignSnapshot.Deserialize(RibbonDesignCommitter.Read(ribbon, host).Serialize());
        var splitNode = draft.Nodes.Single(n => n.Kind == "SplitButton"); var groupNode = draft.Nodes.Single(n => n.Kind == "Group");
        Assert.Equal(new[] { "Horizontal", "Vertical" }, splitNode.Properties.Single(p => p.Name == "Layout").Options);
        Assert.Equal(new[] { "Columns", "TwoRows", "ThreeRows" }, groupNode.Properties.Single(p => p.Name == "Layout").Options);
        Assert.Contains("Separator", RibbonDesignSnapshot.ChildKinds("Group"));
        Set(splitNode, "Layout", "Vertical"); Set(groupNode, "Layout", layout); Set(draft.Nodes.Single(n => n.Kind == "Button"), "Row", "Second");
        var separator = RibbonDesignSnapshot.Deserialize(new RibbonDesignSnapshot { Tabs = [draft.Templates.Single(n => n.Kind == "Separator")] }.Serialize()).Tabs[0];
        separator.Key = "new:separator"; separator.ComponentName = "groupSeparator"; Set(separator, "Id", "group.separator"); groupNode.Children.Insert(1, separator);
        RibbonThemePreviewRenderer.Render(ribbon, host, draft);
        Assert.Equal(RibbonGroupLayout.Columns, group.Layout); Assert.Equal(RibbonItemRow.First, button.Row); Assert.Equal(RibbonSplitButtonLayout.Horizontal, split.Layout);
        Assert.DoesNotContain(group.Items, i => i is RibbonMenuSeparatorItem); Assert.Equal(0, host.Transactions);
        using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(ribbon, host, value); return null; });
        dialog.AcceptDraft(); Assert.Equal(1, host.Committed);
        Assert.Equal(Enum.Parse<RibbonGroupLayout>(layout), group.Layout); Assert.Equal(RibbonItemRow.Second, button.Row); Assert.Equal(RibbonSplitButtonLayout.Vertical, split.Layout);
        Assert.IsType<RibbonMenuSeparatorItem>(group.Items[1]); Assert.Equal("groupSeparator", group.Items[1].Site!.Name);
        var reopened = RibbonDesignCommitter.Read(ribbon, host);
        Assert.Equal(layout, reopened.Nodes.Single(n => n.Kind == "Group").Properties.Single(p => p.Name == "Layout").Value);
        Assert.Equal("Second", reopened.Nodes.Single(n => n.Kind == "Button").Properties.Single(p => p.Name == "Row").Value);
        Set(reopened.Nodes.Single(n => n.Kind == "Group"), "Layout", "Columns");
        using var cancelled = new RibbonEditorDialog(reopened, _ => throw new InvalidOperationException("Cancel must not commit"));
        cancelled.DialogResult = DialogResult.Cancel; Assert.Equal(Enum.Parse<RibbonGroupLayout>(layout), group.Layout); Assert.Equal(1, host.Committed);
    });

    [Fact]
    public void Layout_and_row_undo_together_through_the_framework_component_serializer() => FoundationTests.Sta(() =>
    {
        using var surface = new DesignSurface(typeof(Form)); var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var ribbon = (RibbonControl)host.CreateComponent(typeof(RibbonControl), "ribbon");
        var tab = (RibbonTab)host.CreateComponent(typeof(RibbonTab), "tab"); ribbon.Tabs.Add(tab);
        var group = (RibbonGroup)host.CreateComponent(typeof(RibbonGroup), "group"); tab.Groups.Add(group);
        var split = (RibbonSplitButtonItem)host.CreateComponent(typeof(RibbonSplitButtonItem), "split"); group.Items.Add(split);
        ((IServiceContainer)surface.GetService(typeof(IServiceContainer))!).AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(host));
        using var undo = new Recorder(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host); Set(draft.Nodes.Single(n => n.Kind == "Group"), "Layout", "TwoRows");
        Set(draft.Nodes.Single(n => n.Kind == "SplitButton"), "Layout", "Vertical"); Set(draft.Nodes.Single(n => n.Kind == "SplitButton"), "Row", "Second");
        RibbonDesignCommitter.Apply(ribbon, host, draft); Assert.Equal(1, undo.Count);
        undo.Toggle(); Assert.Equal(RibbonGroupLayout.Columns, group.Layout); Assert.Equal(RibbonSplitButtonLayout.Horizontal, split.Layout); Assert.Equal(RibbonItemRow.First, split.Row);
        undo.Toggle(); Assert.Equal(RibbonGroupLayout.TwoRows, group.Layout); Assert.Equal(RibbonSplitButtonLayout.Vertical, split.Layout); Assert.Equal(RibbonItemRow.Second, split.Row);
    });

    [Fact]
    public void New_layout_properties_emit_standard_codedom_and_reset_to_existing_defaults() => FoundationTests.Sta(() =>
    {
        using var group = new RibbonGroup { Layout = RibbonGroupLayout.TwoRows }; using var split = new RibbonSplitButtonItem { Row = RibbonItemRow.Second, Layout = RibbonSplitButtonLayout.Vertical };
        foreach (var (component, name, propertyName, field) in new (IComponent, string, string, string)[] { (group, "group", "Layout", "TwoRows"), (split, "split", "Row", "Second"), (split, "split", "Layout", "Vertical") })
        {
            var property = TypeDescriptor.GetProperties(component)[propertyName]!;
            var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
            var manager = (IDesignerSerializationManager)serialization; manager.SetName(component, name);
            var serializer = (CodeDomSerializer)manager.GetSerializer(component.GetType(), typeof(CodeDomSerializer))!;
            var assignment = Assert.Single(serializer.SerializeMember(manager, component, property).OfType<CodeAssignStatement>());
            Assert.Equal(field, Assert.IsType<CodeFieldReferenceExpression>(assignment.Right).FieldName); Assert.Empty(serialization.Errors.Cast<object>());
            property.ResetValue(component); Assert.False(property.ShouldSerializeValue(component));
        }
    });

    private static void Set(RibbonDesignNode node, string name, string value) => node.Properties.Single(p => p.Name == name).Value = value;
    private sealed class Recorder(IServiceProvider services) : UndoEngine(services)
    {
        private readonly List<UndoUnit> _units = [];
        internal int Count => _units.Count;
        internal void Toggle() => Assert.Single(_units).Undo();
        protected override void AddUndoUnit(UndoUnit unit) => _units.Add(unit);
    }
}
