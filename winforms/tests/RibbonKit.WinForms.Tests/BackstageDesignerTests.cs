using System.ComponentModel;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageDesignerTests
{
    [Fact]
    public void Detached_draft_and_selection_only_accept_preserve_defaults_without_an_undo_entry() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var draft = RibbonBackstageSnapshot.Deserialize(fixture.Read().Serialize());
        Assert.Equal("CommandMenu", Setting(draft, "FileMenuMode").Value); Assert.Equal("False", Setting(draft, "FileVisible").Value);
        draft.SelectionKey = draft.Entries[1].Key; draft.PreviewWidth = 900; draft.PreviewDpi = 192;
        Assert.False(RibbonBackstageCommitter.Apply(fixture.Ribbon, fixture.Host, draft)); Assert.Equal(0, fixture.Host.Transactions);
        Value(draft.Entries[0], "Text").Value = "Unaccepted";
        Assert.Equal("Info", fixture.Page.Text); Assert.Same(fixture.Content, fixture.Page.Content); Assert.Equal(0, fixture.Host.Changes.Changed);
    });

    [Fact]
    public void One_transaction_keeps_content_handles_text_selection_icons_events_and_binding_owners() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var draft = fixture.Read(); var handle = fixture.Content.Handle; var editorHandle = fixture.Input.Handle;
        fixture.Input.Select(2, 4); var source = new State(); using var bindingSource = new BindingSource { DataSource = source };
        fixture.Page.DataBindings.Add(new Binding(nameof(fixture.Page.Text), bindingSource, nameof(State.Title), false, DataSourceUpdateMode.OnPropertyChanged));
        var binding = fixture.Page.DataBindings[0]; var icon = fixture.Page.Icon; int clicks = 0; fixture.Command.Click += (_, _) => clicks++;
        draft = fixture.Read(); Value(draft.Entries[0], "Text").Value = "Details"; draft.Entries.Reverse();
        Setting(draft, "FileMenuMode").Value = "Backstage"; Setting(draft, "FileVisible").Value = "True";
        Setting(draft, "NavigationWidth").Value = "230";
        Assert.True(RibbonBackstageCommitter.Apply(fixture.Ribbon, fixture.Host, draft));
        Assert.Equal(1, fixture.Host.Committed); Assert.Equal("Edit Backstage", fixture.Host.TransactionDescription);
        Assert.Same(fixture.Page, fixture.Ribbon.Backstage.Items[1]); Assert.Same(fixture.Content, fixture.Page.Content);
        Assert.Same(icon, fixture.Page.Icon); Assert.Same(binding, fixture.Page.DataBindings[0]);
        Assert.Equal(handle, fixture.Content.Handle); Assert.Equal(editorHandle, fixture.Input.Handle);
        Assert.Null(fixture.Content.Parent); Assert.Equal("Native page text", fixture.Input.Text); Assert.Equal(2, fixture.Input.SelectionStart); Assert.Equal(4, fixture.Input.SelectionLength);
        Assert.Equal("Details", source.Title); Assert.Equal(0, clicks); fixture.Command.PerformClick(); Assert.Equal(1, clicks);
        Assert.Equal(RibbonFileMenuMode.Backstage, fixture.Ribbon.FileMenuMode); Assert.True(fixture.Ribbon.FileMenu.Visible); Assert.Equal(230, fixture.Ribbon.Backstage.NavigationWidth);
    });

    [Theory]
    [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Navigation_preview_renders_without_painting_reparenting_or_selecting_live_pages(int dpi) => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); fixture.Page.Visible = false; fixture.Page.Enabled = false;
        var draft = fixture.Read(); draft.SelectionKey = draft.Entries[0].Key; draft.PreviewDpi = dpi;
        int pageSelections = 0, clicks = 0; fixture.Ribbon.Backstage.SelectedPageChanged += (_, _) => pageSelections++;
        fixture.Command.Click += (_, _) => clicks++; var before = fixture.Read().Revision;
        var frame = RibbonBackstagePreviewRenderer.Render(fixture.Ribbon, fixture.Host, draft);
        using var stream = new MemoryStream(Convert.FromBase64String(frame.Png)); using var image = Image.FromStream(stream);
        Assert.Equal(draft.PreviewWidth, image.Width); Assert.Equal(2, frame.Regions.Count);
        Assert.All(frame.Regions, region => { Assert.True(region.Width > 0 && region.Height > 0); Assert.True(region.X >= 0 && region.Y + region.Height <= image.Height); });
        Assert.False(fixture.Page.Visible); Assert.False(fixture.Page.Enabled); Assert.Null(fixture.Content.Parent);
        Assert.Equal(0, fixture.Content.Paints); Assert.Equal(0, pageSelections); Assert.Equal(0, clicks);
        Assert.Equal(before, fixture.Read().Revision); Assert.Equal(0, fixture.Host.Transactions); Assert.False(fixture.Ribbon.IsBackstageOpen);
    });

    [Theory]
    [InlineData("stale")] [InlineData("content")] [InlineData("schema")] [InlineData("id")] [InlineData("outside-id")]
    [InlineData("name")] [InlineData("kind")] [InlineData("children")] [InlineData("width")]
    [InlineData("opacity")] [InlineData("custom-icon")] [InlineData("bad-svg")] [InlineData("qat")]
    public void Invalid_drafts_are_rejected_before_live_changes(string failure) => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var draft = fixture.Read(); var page = draft.Entries[0];
        switch (failure)
        {
            case "stale": fixture.Command.Text = "External"; break;
            case "content": fixture.Page.Content = null; break;
            case "schema": page.Properties.RemoveAt(0); break;
            case "id": Value(page, "Id").Value = Value(draft.Entries[1], "Id").Value; break;
            case "outside-id": Value(page, "Id").Value = fixture.Ribbon.Tabs[0].Id; break;
            case "name": page.ComponentName = draft.Entries[1].ComponentName; break;
            case "kind": page.Kind = "Button"; break;
            case "children": page.Children.Add(new()); break;
            case "width": Setting(draft, "NavigationWidth").Value = "-1"; break;
            case "opacity": Setting(draft, "NavigationBackgroundOpacity").Value = "2"; break;
            case "custom-icon": page.Icon = new() { Kind = "Custom", Name = "Unknown" }; break;
            case "bad-svg": page.Icon = new() { Kind = "Svg", Source = "<svg" }; break;
            case "qat": fixture.Ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = fixture.Command }); draft = fixture.Read(); draft.Entries.RemoveAt(1); break;
        }
        string before = fixture.Read().Revision;
        Assert.ThrowsAny<Exception>(() => RibbonBackstageCommitter.Apply(fixture.Ribbon, fixture.Host, draft));
        Assert.Equal(before, fixture.Read().Revision); Assert.Equal(0, fixture.Host.Transactions); Assert.Equal(0, fixture.Host.Changes.Changed);
        Assert.False(fixture.Content.IsDisposed);
    });

    [Fact]
    public void Removing_a_page_releases_borrowed_content_and_retains_other_component_identity() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var draft = fixture.Read(); draft.Entries.RemoveAt(0);
        Assert.True(RibbonBackstageCommitter.Apply(fixture.Ribbon, fixture.Host, draft));
        Assert.True(fixture.Page.IsDisposed); Assert.False(fixture.Content.IsDisposed); Assert.Null(fixture.Content.Parent);
        Assert.Same(fixture.Command, Assert.Single(fixture.Ribbon.Backstage.Items)); Assert.Single(fixture.Host.Destroyed);
    });

    [Fact]
    public void Custom_form_add_reorder_remove_icons_and_preview_pick_are_detached_until_ok() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var draft = fixture.Read(); int accepts = 0;
        using var dialog = new RibbonBackstageDialog(draft, value => { accepts++; RibbonBackstageCommitter.Apply(fixture.Ribbon, fixture.Host, value); return null; }, value => RibbonBackstagePreviewRenderer.Render(fixture.Ribbon, fixture.Host, value));
        var tree = Controls(dialog).OfType<TreeView>().Single(); _ = tree.Handle;
        var tools = Controls(dialog).OfType<ToolStrip>().Single(tool => tool.Name == "BackstageTools"); var add = (ToolStripDropDownButton)tools.Items["Add"]!;
        Assert.Equal("BackstageRoot", tree.SelectedNode!.Name); Assert.False(tools.Items["Remove"]!.Enabled);
        add.DropDownItems["AddPage"]!.PerformClick(); var created = Assert.IsType<RibbonDesignNode>(tree.SelectedNode!.Tag);
        Value(created, "Text").Value = "Print"; dialog.SetIcon(new() { Kind = "Svg", Source = "stock:print", UseForegroundColor = true });
        tools.Items["MoveUp"]!.PerformClick(); Assert.Same(created, draft.Entries[1]); Assert.Equal(2, fixture.Ribbon.Backstage.Items.Count);
        dialog.RefreshPreview(); var frame = RibbonBackstagePreviewRenderer.Render(fixture.Ribbon, fixture.Host, draft); var first = frame.Regions[0];
        dialog.SelectPreview(new(first.X + 1, first.Y + 1)); Assert.Same(draft.Entries[0], tree.SelectedNode!.Tag); Assert.Equal(0, accepts);
        tree.SelectedNode = tree.Nodes[0].Nodes[0].Nodes[1]; dialog.AcceptDraft();
        Assert.Equal(DialogResult.OK, dialog.DialogResult); Assert.Equal(1, accepts); Assert.Equal(1, fixture.Host.Committed);
        var page = Assert.IsType<RibbonBackstagePageItem>(fixture.Ribbon.Backstage.Items[1]); Assert.Equal("Print", page.Text);
        Assert.Null(page.Content); Assert.Equal("stock:print", Assert.IsType<RibbonSvgIcon>(page.Icon).Source);
    });

    [Fact]
    public void Cancel_and_empty_root_removal_do_not_mutate_live_components_or_content() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var draft = fixture.Read(); var before = fixture.Read().Revision;
        using (var dialog = new RibbonBackstageDialog(draft, _ => throw new Exception("Must not commit"), value => RibbonBackstagePreviewRenderer.Render(fixture.Ribbon, fixture.Host, value)))
        {
            var tree = Controls(dialog).OfType<TreeView>().Single(); _ = tree.Handle; var tools = Controls(dialog).OfType<ToolStrip>().Single(tool => tool.Name == "BackstageTools");
            while (tree.Nodes[0].Nodes[0].Nodes.Count != 0) { tree.SelectedNode = tree.Nodes[0].Nodes[0].Nodes[0]; tools.Items["Remove"]!.PerformClick(); }
            Assert.Same(tree.Nodes[0], tree.SelectedNode); Assert.False(tools.Items["Remove"]!.Enabled); dialog.DialogResult = DialogResult.Cancel;
        }
        Assert.Equal(before, fixture.Read().Revision); Assert.Same(fixture.Content, fixture.Page.Content); Assert.Equal(0, fixture.Host.Transactions);
    });

    [Fact]
    public void Moving_a_page_to_bottom_retains_component_content_binding_artwork_and_noop_accept() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var icon = fixture.Page.Icon; var handle = fixture.Input.Handle; fixture.Input.Select(1, 3);
        using var source = new BindingSource { DataSource = new State() };
        fixture.Page.DataBindings.Add(new Binding(nameof(fixture.Page.Text), source, nameof(State.Title)));
        var binding = fixture.Page.DataBindings[0]; var draft = fixture.Read(); Value(draft.Entries[0], "Placement").Value = "Bottom";
        Assert.True(RibbonBackstageCommitter.Apply(fixture.Ribbon, fixture.Host, draft));
        Assert.Same(fixture.Page, Assert.Single(fixture.Ribbon.Backstage.FooterItems)); Assert.Same(fixture.Command, Assert.Single(fixture.Ribbon.Backstage.Items));
        Assert.Same(fixture.Page, fixture.Ribbon.Backstage.SelectedPage); Assert.Same(fixture.Content, fixture.Page.Content);
        Assert.Same(icon, fixture.Page.Icon); Assert.Same(binding, fixture.Page.DataBindings[0]); Assert.Equal(handle, fixture.Input.Handle);
        Assert.Equal(1, fixture.Input.SelectionStart); Assert.Equal(3, fixture.Input.SelectionLength); Assert.Null(fixture.Content.Parent);
        var read = fixture.Read(); Assert.Equal("Main", Value(read.Entries[0], "Placement").Value); Assert.Equal("Bottom", Value(read.Entries[1], "Placement").Value);
        Assert.False(RibbonBackstageCommitter.Apply(fixture.Ribbon, fixture.Host, read)); Assert.Equal(1, fixture.Host.Committed);
        Assert.Contains(fixture.Page.Id, RibbonDesignCommitter.Read(fixture.Ribbon, fixture.Host).ReservedIds);
    });

    [Fact]
    public void Placement_dropdown_and_separator_schema_reject_invalid_changes_before_transactions() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var draft = fixture.Read();
        var properties = TypeDescriptor.GetProperties(new RibbonEditorDialog.DraftProperties(draft.Entries[0]));
        var placement = properties["Placement"]!;
        Assert.True(placement.Converter.GetStandardValuesExclusive()); Assert.Equal(new[] { "Main", "Bottom" }, placement.Converter.GetStandardValues()!.Cast<string>());
        var separator = draft.Templates.Single(node => node.Kind == "Separator"); Assert.Equal(new[] { "Id", "Placement", "Visible" }, separator.Properties.Select(property => property.Name).Order());
        Value(draft.Entries[0], "Placement").Value = "Overlay";
        Assert.Throws<ArgumentException>(() => RibbonBackstageCommitter.Apply(fixture.Ribbon, fixture.Host, draft)); Assert.Equal(0, fixture.Host.Transactions);
    });

    [Fact]
    public void Dialog_adds_bottom_separators_commands_and_pages_and_keeps_section_nodes_protected() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var draft = fixture.Read();
        using var dialog = new RibbonBackstageDialog(draft, value => { RibbonBackstageCommitter.Apply(fixture.Ribbon, fixture.Host, value); return null; }, value => RibbonBackstagePreviewRenderer.Render(fixture.Ribbon, fixture.Host, value));
        var tree = Controls(dialog).OfType<TreeView>().Single(); _ = tree.Handle;
        var tools = Controls(dialog).OfType<ToolStrip>().Single(tool => tool.Name == "BackstageTools"); var add = (ToolStripDropDownButton)tools.Items["Add"]!;
        var bottom = tree.Nodes[0].Nodes["BottomEntries"]!; tree.SelectedNode = bottom;
        tools.Items["Remove"]!.PerformClick(); Assert.Same(bottom, tree.SelectedNode); Assert.False(tools.Items["Remove"]!.Enabled);
        add.DropDownItems["AddSeparator"]!.PerformClick(); var separator = (RibbonDesignNode)tree.SelectedNode!.Tag!;
        Assert.Equal("Bottom", Value(separator, "Placement").Value); Assert.False(Controls(dialog).Single(control => control.Name == "BackstageIconActions").Visible);
        add.DropDownItems["AddCommand"]!.PerformClick(); var command = (RibbonDesignNode)tree.SelectedNode!.Tag!;
        tools.Items["MoveUp"]!.PerformClick(); Assert.Same(command, tree.SelectedNode!.Parent!.Nodes[0].Tag);
        add.DropDownItems["AddPage"]!.PerformClick(); var page = (RibbonDesignNode)tree.SelectedNode!.Tag!;
        Assert.Equal(2, fixture.Ribbon.Backstage.Items.Count); Assert.Empty(fixture.Ribbon.Backstage.FooterItems);
        dialog.RefreshPreview(); var frame = RibbonBackstagePreviewRenderer.Render(fixture.Ribbon, fixture.Host, draft);
        var region = frame.Regions.Single(region => region.Key == separator.Key); dialog.SelectPreview(new(region.X + 1, region.Y + 1)); Assert.Same(separator, tree.SelectedNode!.Tag);
        dialog.AcceptDraft(); Assert.Equal(DialogResult.OK, dialog.DialogResult); Assert.Equal(1, fixture.Host.Committed);
        Assert.Collection(fixture.Ribbon.Backstage.FooterItems, item => Assert.IsType<RibbonButtonItem>(item), item => Assert.IsType<RibbonMenuSeparatorItem>(item), item => Assert.IsType<RibbonBackstagePageItem>(item));
        Assert.False(RibbonBackstageCommitter.Apply(fixture.Ribbon, fixture.Host, fixture.Read()));
    });

    [Theory]
    [InlineData(96)] [InlineData(144)]
    public void Dialog_preview_pins_bottom_entries_and_uses_separator_height_without_native_page_paint(int dpi) => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); fixture.Ribbon.Backstage.Items.Remove(fixture.Page); fixture.Ribbon.Backstage.FooterItems.AddRange(new RibbonMenuSeparatorItem(), fixture.Page);
        var draft = fixture.Read(); draft.PreviewHeight = 700; draft.PreviewDpi = dpi;
        var frame = RibbonBackstagePreviewRenderer.Render(fixture.Ribbon, fixture.Host, draft);
        var rule = frame.Regions.Single(region => draft.Entries.Single(node => node.Key == region.Key).Kind == "Separator");
        var page = frame.Regions.Single(region => draft.Entries.Single(node => node.Key == region.Key).Kind == "Page");
        Assert.Equal(Layout.RibbonLayoutEngine.Scale(9, dpi), rule.Height); Assert.Equal(rule.Y + rule.Height, page.Y);
        Assert.Equal(700 - Layout.RibbonLayoutEngine.Scale(8, dpi), page.Y + page.Height);
        Assert.Null(fixture.Content.Parent); Assert.Equal(0, fixture.Content.Paints); Assert.Equal(0, fixture.Host.Transactions);
    });

    [Fact]
    public void Dialog_preview_keeps_one_shared_placeholder_when_a_different_page_is_selected() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var draft = fixture.Read(); draft.PreviewWidth = 700; draft.PreviewHeight = 500;
        draft.SelectionKey = draft.Entries[0].Key; var first = RibbonBackstagePreviewRenderer.Render(fixture.Ribbon, fixture.Host, draft);
        draft.SelectionKey = draft.Entries[1].Key; var second = RibbonBackstagePreviewRenderer.Render(fixture.Ribbon, fixture.Host, draft);
        using var firstStream = new MemoryStream(Convert.FromBase64String(first.Png)); using var secondStream = new MemoryStream(Convert.FromBase64String(second.Png));
        using var a = new Bitmap(firstStream); using var b = new Bitmap(secondStream);
        for (int y = 0; y < a.Height; y++) for (int x = 350; x < a.Width; x++) Assert.Equal(a.GetPixel(x, y), b.GetPixel(x, y));
        Assert.Equal(0, fixture.Content.Paints); Assert.Null(fixture.Content.Parent); Assert.Equal(0, fixture.Host.Transactions);
    });

    private static IEnumerable<Control> Controls(Control parent) => parent.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(Controls(child)));
    private static RibbonDesignProperty Value(RibbonDesignNode node, string name) => node.Properties.Single(property => property.Name == name);
    private static RibbonDesignProperty Setting(RibbonBackstageSnapshot draft, string name) => draft.Settings.Single(property => property.Name == name);
    private sealed class State { public string Title { get; set; } = "Info"; }
    private sealed class NeverPaintPage : UserControl
    {
        internal int Paints;
        protected override void OnPaint(PaintEventArgs e) { Paints++; throw new InvalidOperationException("Do not capture or paint native page content for designer preview."); }
    }
    private sealed class Fixture : IDisposable
    {
        internal readonly DesignerAuthoringTests.Host Host = new();
        internal readonly RibbonControl Ribbon;
        internal readonly NeverPaintPage Content = new() { Name = "infoContent" };
        internal readonly TextBox Input = new() { Text = "Native page text" };
        internal readonly RibbonBackstagePageItem Page;
        internal readonly RibbonButtonItem Command;
        internal Fixture()
        {
            Ribbon = DesignerAuthoringTests.MakeRibbon(Host); Content.Controls.Add(Input);
            Page = new() { Text = "Info", Id = "backstage-info", Content = Content, Icon = RibbonStockIcons.Create("info") };
            Command = new() { Text = "Save", Id = "backstage-save" }; Host.Container.Add(Page, "infoPage"); Host.Container.Add(Command, "saveCommand");
            Ribbon.Backstage.Items.AddRange(Page, Command);
        }
        internal RibbonBackstageSnapshot Read() => RibbonBackstageCommitter.Read(Ribbon, Host);
        public void Dispose() { Host.Dispose(); Content.Dispose(); }
    }
}
