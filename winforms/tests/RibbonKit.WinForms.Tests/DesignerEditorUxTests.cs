using System.ComponentModel;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using Xunit;

namespace RibbonKit.WinForms.Tests;

// The SDK-free client dialog source is linked into this .NET test host.
// These checks do not substitute for the packaged net472 dialog inside VS.
public sealed class DesignerEditorUxTests
{
    [Fact]
    public void Contextual_tab_creation_and_color_edits_stay_detached_until_one_accept() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host); int accepts = 0;
        using var dialog = new RibbonEditorDialog(draft, value => { accepts++; RibbonDesignCommitter.Apply(ribbon, host, value); return null; });
        var tree = Control<TreeView>(dialog, "StructureTree"); _ = tree.Handle;
        var tools = Control<ToolStrip>(dialog, "StructureTools"); var add = (ToolStripDropDownButton)tools.Items["Add"]!;
        add.DropDownItems["AddContextualTab"]!.PerformClick();
        var created = draft.Tabs.Last(); Assert.Equal("True", Value(created, "IsContextual")); Assert.Single(tree.Nodes); Assert.Equal(2, tree.Nodes[0].Nodes.Count);
        Assert.Contains("Contextual tab", tree.SelectedNode!.Text); Assert.Single(ribbon.Tabs); Assert.Equal(0, accepts);
        var contextual = Control<CheckBox>(dialog, "ContextualTab"); Assert.True(contextual.Checked);
        dialog.SetHeaderColor(Color.MediumVioletRed); Assert.Equal(Color.MediumVioletRed, ColorValue(created));
        contextual.Checked = false; Assert.False(Control<Button>(dialog, "HeaderColor").Enabled);
        contextual.Checked = true; Assert.Equal(Color.MediumVioletRed, ColorValue(created));
        dialog.SetHeaderColor(Color.Empty); Assert.True(ColorValue(created).IsEmpty);
        dialog.SetHeaderColor(Color.DarkCyan); dialog.AcceptDraft();
        Assert.Equal(DialogResult.OK, dialog.DialogResult); Assert.Equal(1, accepts); Assert.Equal(1, host.Committed);
        Assert.True(ribbon.Tabs[1].IsContextual); Assert.Equal(Color.DarkCyan, ribbon.Tabs[1].ContextualColor);
        Assert.Equal(created.ComponentName, ribbon.Tabs[1].Site!.Name);
    });

    [Fact]
    public void Add_menu_targets_owned_collections_and_context_menu_reorders_without_accepting() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        using (var dialog = new RibbonEditorDialog(draft, _ => throw new InvalidOperationException("Draft must not be accepted")))
        {
            var tree = Control<TreeView>(dialog, "StructureTree"); _ = tree.Handle;
            var add = (ToolStripDropDownButton)Control<ToolStrip>(dialog, "StructureTools").Items["Add"]!;
            Assert.DoesNotContain(add.DropDownItems.Cast<ToolStripItem>(), item => item.Name == "AddButton");
            Assert.False(add.DropDownItems["AddGroup"]!.Enabled);
            tree.SelectedNode = tree.Nodes[0].Nodes[0];
            Assert.True(add.DropDownItems["AddGroup"]!.Enabled);
            tree.SelectedNode = tree.Nodes[0].Nodes[0].Nodes[0];
            add.DropDownItems["AddComboBox"]!.PerformClick();
            Assert.Equal("ComboBox", Assert.IsType<RibbonDesignNode>(tree.SelectedNode!.Tag).Kind);
            Assert.Equal("ComboBox", draft.Tabs[0].Children[0].Children.Last().Kind);
            var menu = tree.ContextMenuStrip!; menu.Items["MoveUp"]!.PerformClick();
            Assert.Equal("ComboBox", draft.Tabs[0].Children[0].Children[2].Kind);
            menu.Items["Remove"]!.PerformClick();
            Assert.Equal(3, draft.Tabs[0].Children[0].Children.Count);
            var cancel = Control<Button>(dialog, "CancelDraft"); Assert.Equal(DialogResult.Cancel, cancel.DialogResult);
            Assert.Equal(0, host.Transactions);
        }
        Assert.Equal(3, ribbon.Tabs[0].Groups[0].Items.Count); Assert.Equal(0, host.Transactions);
    });

    [Theory]
    [InlineData(1f)] [InlineData(1.25f)] [InlineData(1.5f)] [InlineData(2f)]
    public void Synthetic_scaling_and_resize_keep_actions_and_editor_regions_separate(float scale) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        using var dialog = new RibbonEditorDialog(RibbonDesignCommitter.Read(ribbon, host), _ => null);
        _ = dialog.Handle; var tree = Control<TreeView>(dialog, "StructureTree"); _ = tree.Handle; tree.SelectedNode = tree.Nodes[0].Nodes[0];
        Control<CheckBox>(dialog, "ContextualTab").Checked = true; dialog.SetHeaderColor(Color.DarkCyan);
        // Scaling a hidden form checks layout arithmetic, not physical DPI or
        // VS's process/thread DPI context. The user reviews those live gates.
        dialog.PerformAutoScale(); dialog.Scale(new SizeF(scale, scale));
        foreach (var size in new[] { new Size((int)(780 * scale), (int)(520 * scale)), new Size((int)(1100 * scale), (int)(760 * scale)) })
        {
            dialog.ClientSize = size; Layout(dialog);
            var footer = Control<TableLayoutPanel>(dialog, "DialogFooter");
            var ok = Control<Button>(dialog, "AcceptDraft"); var cancel = Control<Button>(dialog, "CancelDraft");
            Assert.True(ok.Right <= cancel.Left, $"Footer actions overlap: {ok.Bounds}, {cancel.Bounds}");
            Assert.True(cancel.Right <= footer.ClientSize.Width, $"Footer clipped: {cancel.Bounds}, {footer.ClientSize}");
            var tools = Control<ToolStrip>(dialog, "StructureTools"); var split = Control<SplitContainer>(dialog, "StructureSplit");
            Assert.True(tools.Bottom <= split.Top); Assert.True(split.Bottom <= footer.Top);
            var header = Control<GroupBox>(dialog, "TabHeader"); var grid = Control<PropertyGrid>(dialog, "ComponentProperties");
            Assert.True(header.Bottom <= grid.Top, $"Header/grid overlap: {header.Bounds}, {grid.Bounds}");
            AssertChildrenFit(header);
            Assert.True(grid.Height > 80 * scale, $"Property grid too small: {grid.Bounds}");
            Assert.True(split.Panel1.Width > 100 * scale && split.Panel2.Width > 250 * scale);
        }
    });

    [Theory]
    [InlineData(12f)] [InlineData(16f)]
    public void Larger_fonts_keep_header_controls_and_footer_actions_available(float points) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        using var font = new Font(SystemFonts.MessageBoxFont!.FontFamily, points);
        using var dialog = new RibbonEditorDialog(RibbonDesignCommitter.Read(ribbon, host), _ => null) { Font = font };
        var tree = Control<TreeView>(dialog, "StructureTree"); _ = tree.Handle; tree.SelectedNode = tree.Nodes[0].Nodes[0];
        Control<CheckBox>(dialog, "ContextualTab").Checked = true; dialog.SetHeaderColor(Color.DarkCyan);
        _ = dialog.Handle; dialog.ClientSize = new Size(780, 600); Layout(dialog);
        AssertChildrenFit(Control<GroupBox>(dialog, "TabHeader"));
        var footer = Control<TableLayoutPanel>(dialog, "DialogFooter");
        var ok = Control<Button>(dialog, "AcceptDraft"); var cancel = Control<Button>(dialog, "CancelDraft");
        Assert.True(ok.Right <= cancel.Left && cancel.Right <= footer.ClientSize.Width);
        Assert.True(Control<PropertyGrid>(dialog, "ComponentProperties").Height > 80);
    });

    [Fact]
    public void Ribbon_root_stays_out_of_the_draft_and_cannot_be_removed_or_reordered() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(ribbon, host, value); return null; });
        var tree = Control<TreeView>(dialog, "StructureTree"); _ = tree.Handle;
        var root = Assert.Single(tree.Nodes.Cast<TreeNode>()); Assert.Equal("RibbonRoot", root.Name); Assert.Null(root.Tag);
        Assert.Same(root, tree.SelectedNode); Assert.Single(root.Nodes); Assert.Single(draft.Tabs);
        var tools = Control<ToolStrip>(dialog, "StructureTools");
        Assert.False(tools.Items["Remove"]!.Enabled); Assert.False(tools.Items["MoveUp"]!.Enabled); Assert.False(tools.Items["MoveDown"]!.Enabled);
        var grid = Control<PropertyGrid>(dialog, "ComponentProperties");
        var tabs = TypeDescriptor.GetProperties(grid.SelectedObject!)["Tabs"]!;
        Assert.True(tabs.IsReadOnly); Assert.Equal(1, tabs.GetValue(grid.SelectedObject));
        Assert.All(RibbonDesignSnapshot.Deserialize(draft.Serialize()).Nodes, node => Assert.NotEqual("Ribbon", node.Kind));
        dialog.AcceptDraft(); Assert.Equal(0, host.Transactions);
    });

    [Fact]
    public void Removing_the_last_draft_tab_preserves_the_root_and_allows_adding_a_replacement() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        using var dialog = new RibbonEditorDialog(draft, _ => throw new InvalidOperationException("Do not accept"));
        var tree = Control<TreeView>(dialog, "StructureTree"); _ = tree.Handle; var root = tree.Nodes[0]; tree.SelectedNode = root.Nodes[0];
        var tools = Control<ToolStrip>(dialog, "StructureTools"); tools.Items["Remove"]!.PerformClick();
        Assert.Same(root, tree.SelectedNode); Assert.Empty(root.Nodes); Assert.Empty(draft.Tabs);
        ((ToolStripDropDownButton)tools.Items["Add"]!).DropDownItems["AddTab"]!.PerformClick();
        Assert.Single(root.Nodes); Assert.Single(draft.Tabs); Assert.Same(root.Nodes[0], tree.SelectedNode);
        Assert.Single(ribbon.Tabs); Assert.Equal(0, host.Transactions);
    });

    [Fact]
    public void Size_policy_dropdown_covers_every_policy_and_commits_the_selected_canonical_value() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(ribbon, host, value); return null; });
        var tree = Control<TreeView>(dialog, "StructureTree"); _ = tree.Handle; tree.SelectedNode = tree.Nodes[0].Nodes[0].Nodes[0].Nodes[0];
        var grid = Control<PropertyGrid>(dialog, "ComponentProperties"); var descriptor = TypeDescriptor.GetProperties(grid.SelectedObject!)["SizePolicy"]!;
        var converter = descriptor.Converter; Assert.True(converter.GetStandardValuesSupported()); Assert.True(converter.GetStandardValuesExclusive());
        var choices = converter.GetStandardValues()!.Cast<string>().ToArray(); Assert.Equal(27, choices.Distinct().Count());
        Assert.Equal("Fixed Large", converter.ConvertToInvariantString(choices[0]));
        Assert.Equal("Large → Medium → Small", converter.ConvertToInvariantString(choices[3]));
        var runtimeConverter = TypeDescriptor.GetConverter(typeof(RibbonSizePolicy));
        foreach (var choice in choices)
        {
            Assert.Equal(choice, converter.ConvertFromInvariantString(converter.ConvertToInvariantString(choice)!));
            Assert.Equal(choice, runtimeConverter.ConvertFromInvariantString(choice)!.ToString());
        }
        Assert.Throws<FormatException>(() => converter.ConvertFromInvariantString("invalid policy"));
        descriptor.SetValue(grid.SelectedObject, "Large → Medium → Small"); dialog.AcceptDraft();
        Assert.Equal(RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small), ribbon.Tabs[0].Groups[0].Items[0].SizePolicy);
        Assert.Equal(1, host.Committed);
    });

    private static string Value(RibbonDesignNode node, string name) => node.Properties.Single(property => property.Name == name).Value;
    private static Color ColorValue(RibbonDesignNode node) => (Color)TypeDescriptor.GetConverter(typeof(Color)).ConvertFromInvariantString(Value(node, "ContextualColor"))!;
    private static T Control<T>(System.Windows.Forms.Control root, string name) where T : System.Windows.Forms.Control => Assert.IsType<T>(root.Controls.Find(name, true).Single());
    private static void Layout(System.Windows.Forms.Control control)
    {
        control.PerformLayout(); foreach (System.Windows.Forms.Control child in control.Controls) Layout(child); control.PerformLayout();
    }
    private static void AssertChildrenFit(System.Windows.Forms.Control parent)
    {
        foreach (System.Windows.Forms.Control child in parent.Controls)
        {
            Assert.True(child.Left >= 0 && child.Top >= 0 && child.Right <= parent.ClientSize.Width && child.Bottom <= parent.ClientSize.Height,
                $"Clipped {child.Name} in {parent.Name}: {child.Bounds}, available {parent.ClientSize}");
            AssertChildrenFit(child);
        }
    }
}
