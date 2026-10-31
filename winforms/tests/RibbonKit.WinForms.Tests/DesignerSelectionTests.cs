using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DesignerSelectionTests
{
    [Theory]
    [InlineData(RibbonLayoutMode.Classic)] [InlineData(RibbonLayoutMode.Simplified)]
    public void Hidden_disabled_contextual_preview_retains_authored_state_and_runtime_startup(RibbonLayoutMode mode) => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var ribbon = fixture.Ribbon; ribbon.LayoutMode = mode;
        var startup = ribbon.SelectedTab; var contextual = fixture.Context;
        var before = RibbonDesignCommitter.Read(ribbon, fixture.Host).Serialize();
        int selections = 0; ribbon.SelectedTabChanged += (_, _) => selections++;
        fixture.Preview.PreviewTab = fixture.Preview.TabChoices.Single(choice => choice.Contains("contextTab"));
        Assert.Same(contextual, ribbon.DisplaySelectedTab); Assert.Same(startup, ribbon.SelectedTab);
        Assert.False(contextual.Visible); Assert.False(contextual.Enabled); Assert.True(contextual.IsContextual);
        Assert.Equal(Color.Purple, contextual.ContextualColor); Assert.Equal(0, selections);
        Assert.Contains(ribbon.Snapshot.Targets, target => ReferenceEquals(target.Element, contextual) && !target.VisibleBounds.IsEmpty);
        Assert.Contains(ribbon.Snapshot.Targets, target => ReferenceEquals(target.Element, fixture.Toggle));
        Assert.Empty(ribbon.Controls.Cast<Control>()); Assert.Null(ribbon.Popups.Top);
        var after = RibbonDesignCommitter.Read(ribbon, fixture.Host);
        // Selection metadata is transient and excluded from the authoring revision.
        Assert.Equal(RibbonDesignSnapshot.Deserialize(before).Revision, after.Revision);
        Assert.False(RibbonDesignCommitter.Apply(ribbon, fixture.Host, after));
        Assert.Equal(0, fixture.Host.Transactions); Assert.Equal(0, fixture.Host.Changes.Changed);
    });

    [Fact]
    public void Picking_commands_fields_and_split_parts_selects_components_without_activation() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var ribbon = fixture.Ribbon;
        fixture.Context.Enabled = true; fixture.Preview.Select(fixture.Context);
        int clicks = 0; fixture.Toggle.Click += (_, _) => clicks++; fixture.Split.Click += (_, _) => clicks++;
        foreach (var element in new RibbonElement[] { fixture.Toggle, fixture.Field, fixture.Combo, fixture.Split })
        {
            var targets = ribbon.Snapshot.Targets.Where(target => ReferenceEquals(target.Element, element)).ToArray(); Assert.NotEmpty(targets);
            foreach (var target in targets)
            {
                var point = Center(target.VisibleBounds); Assert.Same(element, fixture.Preview.HitTest(point));
                Assert.True(fixture.Preview.SelectAt(point)); Assert.Same(element, fixture.Selection.PrimarySelection);
            }
        }
        Assert.Equal(0, clicks); Assert.False(fixture.Toggle.Checked); Assert.Equal("Untouched", fixture.Field.Value);
        Assert.Equal(1, fixture.Combo.SelectedIndex); Assert.Equal("Beta", fixture.Combo.Value); Assert.Null(ribbon.Popups.Top);
        Assert.Empty(ribbon.Controls.Cast<Control>());
        Assert.False(fixture.Preview.SelectAt(new Point(-1, -1)));
    });

    [Fact]
    public void Group_and_split_selection_outlines_follow_measured_bounds_and_caption_changes() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); fixture.Preview.Select(fixture.Context); var ribbon = fixture.Ribbon;
        var group = fixture.Context.Groups[0]; var slot = ribbon.Snapshot.Groups.Single();
        var captionPoint = new Point(slot.Frame.Left + slot.Frame.Width / 2, slot.Frame.Bottom - 4);
        Assert.Same(group, fixture.Preview.HitTest(captionPoint));
        Assert.Equal(Rectangle.Intersect(slot.Frame, ribbon.Snapshot.BodyViewport), fixture.Preview.SelectionBounds(group));
        var parts = ribbon.Snapshot.Targets.Where(target => ReferenceEquals(target.Element, fixture.Split)).ToArray(); Assert.Equal(2, parts.Length);
        Assert.Equal(parts.Select(target => target.VisibleBounds).Aggregate(Rectangle.Union), fixture.Preview.SelectionBounds(fixture.Split));
        fixture.Split.Text = "A different and much longer command caption";
        var updated = ribbon.Snapshot.Targets.Where(target => ReferenceEquals(target.Element, fixture.Split)).Select(target => target.VisibleBounds).Aggregate(Rectangle.Union);
        Assert.Equal(updated, fixture.Preview.SelectionBounds(fixture.Split));
    });

    [Fact]
    public void Properties_selection_reveals_its_tab_and_unrelated_selection_releases_surface_picking() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); Assert.True(fixture.Preview.Active);
        fixture.Selection.SetSelectedComponents(new[] { fixture.Field });
        Assert.Same(fixture.Context, fixture.Ribbon.DisplaySelectedTab); Assert.True(fixture.Preview.Active);
        using var unrelated = new Button(); fixture.Selection.SetSelectedComponents(new[] { unrelated });
        Assert.False(fixture.Preview.Active); Assert.Same(fixture.Context, fixture.Ribbon.DisplaySelectedTab);
        fixture.Preview.Select(unrelated); Assert.Same(unrelated, fixture.Selection.PrimarySelection);
    });

    [Fact]
    public void Reset_removal_and_disposal_restore_the_normal_display_without_serializing_preview() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var ribbon = fixture.Ribbon; var startup = ribbon.SelectedTab;
        fixture.Preview.Select(fixture.Context); fixture.Preview.PreviewTab = "(Startup tab)";
        Assert.Same(startup, ribbon.DisplaySelectedTab); Assert.Same(ribbon, fixture.Selection.PrimarySelection);
        Assert.DoesNotContain(ribbon.Snapshot.Targets, target => ReferenceEquals(target.Element, fixture.Context));
        fixture.Preview.Select(fixture.Context); ribbon.Tabs.Remove(fixture.Context);
        Assert.Same(startup, ribbon.DisplaySelectedTab); Assert.DoesNotContain(ribbon.Snapshot.Groups, group => ReferenceEquals(group.Group, fixture.Context.Groups[0]));
        ribbon.Tabs.Add(fixture.Context); fixture.Preview.Select(fixture.Context); fixture.Preview.Dispose();
        Assert.Same(startup, ribbon.DisplaySelectedTab);
        fixture.Selection.SetSelectedComponents(new[] { fixture.Field }); Assert.Same(startup, ribbon.DisplaySelectedTab);
        var properties = TypeDescriptor.GetProperties(ribbon);
        Assert.Equal(DesignerSerializationVisibility.Hidden, properties[nameof(ribbon.SelectedTab)]!.SerializationVisibility);
        Assert.DoesNotContain(properties.Cast<PropertyDescriptor>(), property => property.Name.Contains("Preview"));
    });

    [Fact]
    public void Narrow_preview_reveals_clipped_headers_and_never_hits_outside_the_measured_viewport() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var ribbon = fixture.Ribbon; ribbon.Width = 280;
        for (int i = 0; i < 8; i++) ribbon.Tabs.Add(new RibbonTab { Text = "More " + i });
        var last = ribbon.Tabs.Last(); fixture.Preview.Select(last);
        var header = ribbon.Snapshot.Targets.Single(target => ReferenceEquals(target.Element, last));
        Assert.False(header.VisibleBounds.IsEmpty); Assert.True(ribbon.Snapshot.TabViewport.Contains(Center(header.VisibleBounds)));
        Assert.Same(last, fixture.Preview.HitTest(Center(header.VisibleBounds)));
        var scroller = ribbon.Snapshot.Targets.First(target => target.Kind == TargetKind.TabBack);
        Assert.True(fixture.Preview.SelectAt(Center(scroller.VisibleBounds))); Assert.Same(last, fixture.Selection.PrimarySelection);
        Assert.False(fixture.Preview.CanSelectAt(new Point(ribbon.Width + 1, header.Bounds.Top)));
        fixture.Preview.PreviewTab = "(Startup tab)";
        var startup = ribbon.Snapshot.Targets.Single(target => ReferenceEquals(target.Element, ribbon.SelectedTab));
        Assert.False(startup.VisibleBounds.IsEmpty); Assert.True(ribbon.Snapshot.TabViewport.Contains(Center(startup.VisibleBounds)));
    });

    [Fact]
    public void Inactive_and_nested_components_transfer_to_the_draft_and_selection_only_accept_is_a_noop() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); fixture.Preview.Select(fixture.Field);
        var draft = RibbonDesignSnapshot.Deserialize(RibbonDesignCommitter.Read(fixture.Ribbon, fixture.Host).Serialize());
        Assert.Equal("site:nameField", draft.SelectionKey);
        using var dialog = new RibbonEditorDialog(draft, snapshot =>
        {
            Assert.False(RibbonDesignCommitter.Apply(fixture.Ribbon, fixture.Host, snapshot)); fixture.Preview.SelectDraft(snapshot); return null;
        });
        var tree = FindTree(dialog); _ = tree.Handle;
        Assert.Equal("site:nameField", Assert.IsType<RibbonDesignNode>(tree.SelectedNode!.Tag).Key);
        tree.SelectedNode = tree.SelectedNode.Parent!.Nodes.Cast<TreeNode>().Single(node => ((RibbonDesignNode)node.Tag!).Key == "site:toggle");
        dialog.AcceptDraft(); Assert.Same(fixture.Toggle, fixture.Selection.PrimarySelection); Assert.Equal(0, fixture.Host.Transactions);
    });

    [Fact]
    public void Accepted_new_component_selection_resolves_its_generated_site_name() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var draft = RibbonDesignCommitter.Read(fixture.Ribbon, fixture.Host);
        using var dialog = new RibbonEditorDialog(draft, snapshot =>
        {
            RibbonDesignCommitter.Apply(fixture.Ribbon, fixture.Host, snapshot); fixture.Preview.SelectDraft(snapshot); return null;
        });
        var tree = FindTree(dialog); _ = tree.Handle;
        var tools = Descendants(dialog).OfType<ToolStrip>().Single(tool => tool.Name == "StructureTools");
        ((ToolStripDropDownButton)tools.Items["Add"]!).DropDownItems["AddContextualTab"]!.PerformClick();
        var created = Assert.IsType<RibbonDesignNode>(tree.SelectedNode!.Tag); dialog.AcceptDraft();
        var selected = Assert.IsType<RibbonTab>(fixture.Selection.PrimarySelection);
        Assert.Equal(created.ComponentName, selected.Site!.Name); Assert.True(selected.IsContextual);
        Assert.Same(selected, fixture.Ribbon.DisplaySelectedTab); Assert.Equal(1, fixture.Host.Committed);
    });

    [Fact]
    public void Preview_is_unavailable_at_runtime_and_cannot_take_another_ribbons_tab() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); using var runtime = new RibbonControl(); var tab = new RibbonTab(); runtime.Tabs.Add(tab);
        Assert.Throws<InvalidOperationException>(() => runtime.SetDesignPreviewTab(tab));
        Assert.Throws<ArgumentException>(() => fixture.Ribbon.SetDesignPreviewTab(tab));
        Assert.Same(tab, runtime.DisplaySelectedTab); Assert.Null(runtime.DesignPreviewTab);
    });

    [Fact]
    public void Static_field_preview_uses_the_shared_renderer_without_creating_native_editors() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); using var renderer = new FieldRenderer(); fixture.Ribbon.Renderer = renderer;
        fixture.Preview.Select(fixture.Context);
        using var bitmap = new Bitmap(fixture.Ribbon.Width, fixture.Ribbon.Height);
        fixture.Ribbon.DrawToBitmap(bitmap, new(Point.Empty, bitmap.Size));
        Assert.Equal("Untouched", Assert.Single(renderer.Inputs)); Assert.Equal("Beta", Assert.Single(renderer.Choices));
        Assert.Same(fixture.Context, renderer.SelectedTab); Assert.Empty(fixture.Ribbon.Controls.Cast<Control>());
        Assert.False(fixture.Field.DataBindings.Count > 0); Assert.Equal(1, fixture.Combo.SelectedIndex);
    });
    private sealed class FieldRenderer : RibbonOffice2024Renderer
    {
        internal List<string> Inputs = [], Choices = [];
        internal RibbonTab? SelectedTab;
        public override void DrawTextBox(RibbonPaintContext context, RibbonTextBoxItem item, Rectangle bounds)
        { Inputs.Add(item.Value); base.DrawTextBox(context, item, bounds); }
        public override void DrawComboBox(RibbonPaintContext context, RibbonComboBoxItem item, Rectangle bounds)
        { Choices.Add(item.Value); base.DrawComboBox(context, item, bounds); }
        public override void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected)
        { if (selected) SelectedTab = tab; base.DrawTab(context, tab, selected); }
    }

    private static Point Center(Rectangle bounds) { Assert.False(bounds.IsEmpty); return new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2); }
    private static IEnumerable<Control> Descendants(Control control) => control.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(Descendants(child)));
    private static TreeView FindTree(Control dialog) => Descendants(dialog).OfType<TreeView>().Single();

    private sealed class Fixture : IDisposable
    {
        internal readonly DesignerAuthoringTests.Host Host = new();
        internal readonly SelectionService Selection = new();
        internal readonly RibbonControl Ribbon;
        internal readonly RibbonTab Context;
        internal readonly RibbonToggleItem Toggle;
        internal readonly RibbonSplitButtonItem Split;
        internal readonly RibbonTextBoxItem Field;
        internal readonly RibbonComboBoxItem Combo;
        internal readonly RibbonDesignSelection Preview;
        internal Fixture()
        {
            Host.Selection = Selection; Ribbon = new RibbonControl { Width = 1100 }; Host.Container.Add(Ribbon, "ribbon");
            Ribbon.Site = new DesignSite(Ribbon.Site!, Host);
            var home = Add(new RibbonTab { Text = "Home" }, "homeTab");
            home.Groups.Add(Add(new RibbonGroup { Text = "Home group" }, "homeGroup")); Ribbon.Tabs.Add(home);
            Context = Add(new RibbonTab { Text = "Picture tools", IsContextual = true, Visible = false, Enabled = false, ContextualColor = Color.Purple }, "contextTab");
            var group = Add(new RibbonGroup { Text = "Editing" }, "contextGroup"); Context.Groups.Add(group);
            Toggle = Add(new RibbonToggleItem { Text = "Toggle", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }, "toggle");
            Split = Add(new RibbonSplitButtonItem { Text = "Split" }, "split");
            Field = Add(new RibbonTextBoxItem { Text = "Name", Value = "Untouched" }, "nameField");
            Combo = Add(new RibbonComboBoxItem { Text = "Font" }, "fontField"); Combo.Items.AddRange("Alpha", "Beta"); Combo.SelectedIndex = 1;
            group.Items.AddRange(Toggle, Split, Field, Combo); Ribbon.Tabs.Add(Context);
            Selection.SetSelectedComponents(new[] { Ribbon }); Preview = new(Ribbon, Selection);
        }
        private T Add<T>(T component, string name) where T : IComponent { Host.Container.Add(component, name); return component; }
        public void Dispose() { Preview.Dispose(); Host.Dispose(); }
    }
    private sealed class DesignSite(ISite original, IServiceProvider services) : ISite
    {
        public IComponent Component => original.Component; public IContainer? Container => original.Container; public bool DesignMode => true;
        public string? Name { get => original.Name; set => original.Name = value; }
        public object? GetService(Type type) => services.GetService(type);
    }
    private sealed class SelectionService : ISelectionService
    {
        private object[] _selected = [];
        public object? PrimarySelection => _selected.FirstOrDefault(); public int SelectionCount => _selected.Length;
        public event EventHandler? SelectionChanged; public event EventHandler? SelectionChanging;
        public bool GetComponentSelected(object component) => _selected.Contains(component);
        public ICollection GetSelectedComponents() => _selected;
        public void SetSelectedComponents(ICollection? components) => SetSelectedComponents(components, SelectionTypes.Replace);
        public void SetSelectedComponents(ICollection? components, SelectionTypes selectionType)
        { SelectionChanging?.Invoke(this, EventArgs.Empty); _selected = components?.Cast<object>().ToArray() ?? []; SelectionChanged?.Invoke(this, EventArgs.Empty); }
    }
}
