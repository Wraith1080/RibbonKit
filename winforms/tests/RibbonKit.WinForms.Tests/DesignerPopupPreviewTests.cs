using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Runtime.Serialization.Json;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DesignerPopupPreviewTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly DesignerAuthoringTests.Host Host = new();
        internal readonly RibbonControl Ribbon;
        internal readonly RibbonDropDownItem Menu;
        internal readonly RibbonButtonItem Button;
        internal readonly RibbonSplitButtonItem Nested;
        internal readonly RibbonButtonItem Child;
        internal readonly RibbonTextBoxItem Text;
        internal readonly RibbonComboBoxItem Combo;
        internal readonly RibbonMenuSeparatorItem Separator;
        internal readonly RibbonButtonItem Hidden;
        internal int Clicks, Validations;
        internal Fixture(bool split = false)
        {
            Ribbon = DesignerAuthoringTests.MakeRibbon(Host);
            Menu = split ? new RibbonSplitButtonItem() : new RibbonDropDownItem();
            Menu.Text = "Actions"; Menu.DropDownHeader = "Document actions"; Site(Menu, "actions");
            Button = new RibbonButtonItem { Text = "Apply", MenuDescription = "Apply these document settings" }; Site(Button, "applyPopup");
            Nested = new RibbonSplitButtonItem { Text = "More", DropDownHeader = "More actions" }; Site(Nested, "more");
            Child = new RibbonButtonItem { Text = "Nested command", Enabled = false }; Site(Child, "nestedCommand"); Nested.Items.Add(Child);
            Text = new RibbonTextBoxItem { Text = "Name", Value = "Retained text" }; Site(Text, "popupName");
            Combo = new RibbonComboBoxItem { Text = "Choice" }; Combo.Items.AddRange("Alpha", "Beta"); Combo.SelectedIndex = 1; Site(Combo, "popupChoice");
            Separator = new RibbonMenuSeparatorItem(); Site(Separator, "popupSeparator");
            Hidden = new RibbonButtonItem { Text = "Hidden", Visible = false }; Site(Hidden, "hiddenCommand");
            Menu.Items.AddRange(Button, Separator, Text, Combo, Nested, Hidden);
            Ribbon.Tabs[0].Groups[0].Items.Add(Menu);
            Button.Click += (_, _) => Clicks++; Child.Click += (_, _) => Clicks++; Nested.Click += (_, _) => Clicks++;
            Text.ValueValidating += (_, _) => Validations++; Combo.ValueValidating += (_, _) => Validations++;
        }
        private void Site(RibbonElement component, string name) => Host.Container.Add(component, name);
        internal RibbonDesignSnapshot Draft(string? key = null)
        {
            var result = RibbonDesignCommitter.Read(Ribbon, Host); result.SelectionKey = key ?? RibbonDesignCommitter.Key(Menu); return result;
        }
        internal RibbonThemePreview Render(RibbonDesignSnapshot draft) => RibbonThemePreviewRenderer.Render(Ribbon, Host, draft);
        public void Dispose() { Ribbon.Dispose(); Host.Dispose(); }
    }

    [Theory]
    [InlineData(false, RibbonDensity.Compact)] [InlineData(true, RibbonDensity.Touch)]
    public void Shared_menu_preview_preserves_live_state_and_maps_static_fields_separators_and_submenus(bool split, RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(split); f.Ribbon.Density = density;
        var selected = f.Ribbon.SelectedTab; var draft = f.Draft();
        string original = draft.Serialize();
        var result = Assert.IsType<RibbonPopupPreview>(f.Render(draft).Popup);
        Assert.Equal(RibbonDesignCommitter.Key(f.Menu), result.OwnerKey); Assert.Empty(result.ParentKey);
        foreach (var item in new RibbonItem[] { f.Button, f.Text, f.Combo, f.Separator, f.Nested })
            Assert.Contains(result.Hits, hit => hit.Key == RibbonDesignCommitter.Key(item) && hit.Width > 0 && hit.Height > 0);
        Assert.DoesNotContain(result.Hits, hit => hit.Key == RibbonDesignCommitter.Key(f.Hidden));
        Assert.Single(result.Hits, hit => hit.OpensSubmenu);
        using var bytes = new MemoryStream(Convert.FromBase64String(result.Png)); using var image = Image.FromStream(bytes);
        Assert.True(image.Width > 150 && image.Height > 100);
        foreach (var hit in result.Hits) Assert.True(new Rectangle(0, 0, image.Width, image.Height).Contains(new Rectangle(hit.X, hit.Y, hit.Width, hit.Height)));
        Assert.Equal(original, draft.Serialize()); Assert.Equal(0, f.Clicks); Assert.Equal(0, f.Validations); Assert.Equal(0, f.Host.Transactions);
        Assert.Null(f.Ribbon.Popups.Top); Assert.False(f.Menu.IsDropDownOpen); Assert.Same(selected, f.Ribbon.SelectedTab);
        Assert.Equal("Retained text", f.Text.Value); Assert.Equal("Beta", f.Combo.Value);
        draft.SelectionKey = RibbonDesignCommitter.Key(f.Nested);
        var submenu = Assert.IsType<RibbonPopupPreview>(f.Render(draft).Popup);
        Assert.Equal(result.OwnerKey, submenu.ParentKey);
        Assert.Contains(submenu.Hits, hit => hit.Key == RibbonDesignCommitter.Key(f.Child));
        Capture(result, split ? "split-touch" : "dropdown-compact"); Capture(submenu, "nested");
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Custom_content_is_a_placeholder_without_creating_painting_or_reparenting_application_controls(bool borrowed) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture();
        using var body = borrowed ? new Body() : null;
        if (borrowed) f.Menu.Content = body; else f.Menu.ContentType = typeof(ThrowingBody);
        var result = Assert.IsType<RibbonPopupPreview>(f.Render(f.Draft()).Popup);
        Assert.Single(result.Hits); Assert.Equal(RibbonDesignCommitter.Key(f.Menu), result.Hits[0].Key);
        Assert.False(result.Hits[0].OpensSubmenu); Assert.Equal(0, f.Host.Transactions); Assert.Equal(0, f.Clicks);
        if (body is not null) { Assert.False(body.IsHandleCreated); Assert.Null(body.Parent); Assert.Equal(0, body.Paints); Assert.Same(body, f.Menu.Content); }
        Assert.False(f.Menu.IsDropDownOpen); Assert.Null(f.Ribbon.Popups.Top);
        Capture(result, borrowed ? "borrowed-body" : "declared-body");
    });

    [Fact]
    public void Preview_clicks_only_select_draft_properties_and_submenu_navigation_is_not_an_edit() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Draft();
        using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(f.Ribbon, f.Host, value); return null; }, f.Render);
        var tree = (TreeView)dialog.Controls.Find("StructureTree", true).Single(); _ = tree.Handle;
        dialog.RenderPopupPreview(RibbonDesignCommitter.Key(f.Menu));
        var row = f.Render(f.Draft()).Popup!.Hits.First(hit => hit.Key == RibbonDesignCommitter.Key(f.Combo));
        Assert.True(dialog.SelectPopupPreviewAt(new(row.X + 2, row.Y + row.Height / 2)));
        Assert.Equal(RibbonDesignCommitter.Key(f.Combo), draft.SelectionKey);
        var grid = (PropertyGrid)dialog.Controls.Find("ComponentProperties", true).Single();
        Assert.Equal("ComboBox", TypeDescriptor.GetClassName(grid.SelectedObject!));
        Assert.Equal("Beta", f.Combo.Value); Assert.Equal(0, f.Validations);
        var arrow = f.Render(f.Draft()).Popup!.Hits.Single(hit => hit.OpensSubmenu);
        Assert.True(dialog.SelectPopupPreviewAt(new(arrow.X + arrow.Width / 2, arrow.Y + arrow.Height / 2)));
        Assert.Equal(RibbonDesignCommitter.Key(f.Nested), draft.SelectionKey);
        var back = (Button)dialog.Controls.Find("PopupPreviewBack", true).Single(); Assert.True(back.Enabled); dialog.PreviewParentPopup();
        Assert.False(back.Enabled); dialog.ClosePopupPreview(); dialog.AcceptDraft();
        Assert.Equal(0, f.Host.Transactions); Assert.Equal(0, f.Clicks); Assert.False(f.Menu.IsDropDownOpen);
    });

    [Fact]
    public void Draft_menu_edits_refresh_pixels_but_cancel_preserves_live_declarations() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Draft();
        var initial = f.Render(draft).Popup!;
        draft.CommandNodes.Single(node => node.Key == RibbonDesignCommitter.Key(f.Button)).Properties.Single(property => property.Name == "Text").Value = "Changed caption";
        Assert.NotEqual(initial.Png, f.Render(draft).Popup!.Png);
        Assert.Equal("Apply", f.Button.Text); Assert.Equal(0, f.Host.Transactions); Assert.Equal(0, f.Clicks);
        using var dialog = new RibbonEditorDialog(draft, _ => throw new InvalidOperationException("Cancel must not commit."), f.Render);
        dialog.RenderPopupPreview(initial.OwnerKey); dialog.DialogResult = DialogResult.Cancel;
        Assert.Equal("Apply", f.Button.Text); Assert.Equal(0, f.Host.Transactions);
    });

    [Fact]
    public void Clearing_a_declared_body_in_the_draft_previews_preserved_menu_rows_without_changing_the_live_body() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Menu.ContentType = typeof(ThrowingBody);
        var draft = f.Draft();
        Assert.Single(f.Render(draft).Popup!.Hits);
        draft.CommandNodes.Single(node => node.Key == RibbonDesignCommitter.Key(f.Menu)).Properties.Single(property => property.Name == "ContentType").Value = "(None)";
        var preview = f.Render(draft).Popup!;
        Assert.Contains(preview.Hits, hit => hit.Key == RibbonDesignCommitter.Key(f.Combo));
        Assert.Equal(typeof(ThrowingBody), f.Menu.ContentType); Assert.Null(f.Menu.Content);
        Assert.Equal(0, f.Host.Transactions); Assert.Equal(0, f.Clicks);
    });

    [Fact]
    public void Popup_response_roundtrips_and_invalid_or_stale_requests_cannot_open_runtime_windows() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Draft(); var response = f.Render(draft);
        var serializer = new DataContractJsonSerializer(typeof(RibbonThemePreview));
        using var stream = new MemoryStream(); serializer.WriteObject(stream, response); stream.Position = 0;
        var copy = (RibbonThemePreview)serializer.ReadObject(stream)!;
        Assert.Equal(response.Popup!.Png, copy.Popup!.Png); Assert.Equal(response.Popup.Hits.Count, copy.Popup.Hits.Count);
        using var dialog = new RibbonEditorDialog(draft, _ => null, f.Render);
        Assert.Throws<ArgumentException>(() => dialog.RenderPopupPreview("not-a-component"));
        dialog.RenderPopupPreview(response.Popup.OwnerKey);
        f.Button.Text = "Changed externally";
        var arrow = response.Popup.Hits.Single(hit => hit.OpensSubmenu);
        var picture = (PictureBox)dialog.Controls.Find("PopupPreviewImage", true).Single();
        typeof(Control).GetMethod("OnMouseClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(picture, [new MouseEventArgs(MouseButtons.Left, 1, arrow.X + arrow.Width / 2, arrow.Y + arrow.Height / 2, 0)]);
        Assert.Null(picture.Image);
        Assert.StartsWith("Preview unavailable:", dialog.Controls.Find("PopupPreviewHelp", true).Single().Text);
        Assert.Throws<ArgumentException>(() => f.Render(draft)); Assert.Null(f.Ribbon.Popups.Top); Assert.Equal(0, f.Host.Transactions);
    });

    [Fact]
    public void Preview_navigation_is_not_an_undo_unit_and_authored_edits_commit_as_one_unit() => FoundationTests.Sta(() =>
    {
        using var surface = new DesignSurface(typeof(Form)); var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var ribbon = (RibbonControl)host.CreateComponent(typeof(RibbonControl), "ribbon");
        var tab = (RibbonTab)host.CreateComponent(typeof(RibbonTab), "home"); ribbon.Tabs.Add(tab);
        var group = (RibbonGroup)host.CreateComponent(typeof(RibbonGroup), "commands"); tab.Groups.Add(group);
        var menu = (RibbonSplitButtonItem)host.CreateComponent(typeof(RibbonSplitButtonItem), "paste"); group.Items.Add(menu);
        var child = (RibbonButtonItem)host.CreateComponent(typeof(RibbonButtonItem), "pasteText"); child.Text = "Before"; menu.Items.Add(child);
        ((IServiceContainer)surface.GetService(typeof(IServiceContainer))!).AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(host));
        using var undo = new UndoRecorder(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host); draft.SelectionKey = RibbonDesignCommitter.Key(menu);
        var preview = RibbonThemePreviewRenderer.Render(ribbon, host, draft).Popup!;
        using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(ribbon, host, value); return null; },
            value => RibbonThemePreviewRenderer.Render(ribbon, host, value));
        _ = dialog.Controls.Find("StructureTree", true).Single().Handle;
        dialog.RenderPopupPreview(preview.OwnerKey); var hit = Assert.Single(preview.Hits);
        Assert.True(dialog.SelectPopupPreviewAt(new(hit.X + 4, hit.Y + 4))); dialog.ClosePopupPreview();
        Assert.Equal(0, undo.Count); Assert.False(RibbonDesignCommitter.Apply(ribbon, host, draft));
        draft.CommandNodes.Single(node => node.Key == hit.Key).Properties.Single(property => property.Name == "Text").Value = "After";
        dialog.AcceptDraft(); Assert.Equal(1, undo.Count); Assert.Equal("After", child.Text);
        undo.Toggle(); Assert.Equal("Before", child.Text); Assert.Same(child, menu.Items.Single());
        undo.Toggle(); Assert.Equal("After", child.Text); Assert.False(menu.IsDropDownOpen);
    });

    [Fact]
    public void Shown_preview_keeps_properties_and_full_menu_visible_without_a_runtime_popup() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(true); var draft = f.Draft();
        using var dialog = new RibbonEditorDialog(draft, _ => null, f.Render) { Location = new(120, 100), StartPosition = FormStartPosition.Manual };
        dialog.Show(); Application.DoEvents();
        var picture = (PictureBox)dialog.Controls.Find("PopupPreviewImage", true).Single();
        var split = (SplitContainer)dialog.Controls.Find("PopupPreviewSplit", true).Single();
        var grid = (PropertyGrid)dialog.Controls.Find("ComponentProperties", true).Single();
        Assert.False(split.Panel2Collapsed); Assert.True(grid.Visible); Assert.True(picture.Visible); Assert.NotNull(picture.Image);
        Assert.True(picture.Width > 100 && picture.Height > 100);
        if (Environment.GetEnvironmentVariable("RIBBONKIT_POPUP_PREVIEW_CAPTURE") is { Length: > 0 } path)
        {
            Directory.CreateDirectory(path);
            using var image = new Bitmap(dialog.Width, dialog.Height); dialog.DrawToBitmap(image, new(Point.Empty, image.Size));
            image.Save(Path.Combine(path, "editor-pane.png"));
        }
        var hit = f.Render(f.Draft()).Popup!.Hits.First(item => item.Key == RibbonDesignCommitter.Key(f.Button));
        Assert.True(dialog.SelectPopupPreviewAt(new(hit.X + 4, hit.Y + 4)));
        Assert.Equal(RibbonDesignCommitter.Key(f.Button), draft.SelectionKey);
        Assert.Null(f.Ribbon.Popups.Top); Assert.Equal(0, f.Clicks); Assert.Equal(0, f.Validations);
        dialog.Close();
    });

    [Fact]
    public void Moving_adding_and_removing_rows_refreshes_the_image_while_selection_alone_does_not() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Draft(); int renders = 0;
        using var dialog = new RibbonEditorDialog(draft, _ => null, value => { renders++; return f.Render(value); });
        var tree = (TreeView)dialog.Controls.Find("StructureTree", true).Single(); _ = tree.Handle;
        var tools = (ToolStrip)dialog.Controls.Find("StructureTools", true).Single();
        var picture = (PictureBox)dialog.Controls.Find("PopupPreviewImage", true).Single();
        dialog.RenderPopupPreview(RibbonDesignCommitter.Key(f.Menu)); var image = picture.Image;
        var hit = f.Render(f.Draft()).Popup!.Hits.First(hit => hit.Key == RibbonDesignCommitter.Key(f.Button));
        dialog.SelectPopupPreviewAt(new(hit.X + 2, hit.Y + 2)); Assert.Equal(1, renders); Assert.Same(image, picture.Image);
        tools.Items["MoveDown"]!.PerformClick(); Assert.True(renders > 1); Assert.NotSame(image, picture.Image); image = picture.Image;
        tree.SelectedNode = tree.SelectedNode!.Parent;
        ((ToolStripDropDownButton)tools.Items["Add"]!).DropDownItems["AddButton"]!.PerformClick();
        Assert.NotSame(image, picture.Image); image = picture.Image;
        tools.Items["Remove"]!.PerformClick(); Assert.NotSame(image, picture.Image);
        Assert.Same(f.Button, f.Menu.Items[0]); Assert.Equal(0, f.Host.Transactions); Assert.Equal(0, f.Clicks);
    });

    [Fact]
    public void Edit_popup_shortcut_finds_nested_and_quick_access_sources_without_activation() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var selected = new Selection();
        f.Ribbon.Site = new DesignSite(f.Ribbon.Site!, f.Host);
        using var policy = new RibbonDesignSelection(f.Ribbon, selected);
        selected.SetSelectedComponents(new[] { f.Text }); Assert.Same(f.Menu, policy.PopupSource);
        selected.SetSelectedComponents(new[] { f.Child }); Assert.Same(f.Nested, policy.PopupSource);
        var reference = new RibbonQuickAccessDropDownReference { Source = f.Menu }; f.Ribbon.QuickAccessItems.Add(reference);
        selected.SetSelectedComponents(new[] { reference }); Assert.Same(f.Menu, policy.PopupSource);
        selected.SetSelectedComponents(new[] { f.Ribbon.Tabs[0].Groups[0].Items.OfType<RibbonGalleryItem>().Single() }); Assert.Null(policy.PopupSource);
        Assert.Equal(0, f.Clicks); Assert.Equal(0, f.Validations); Assert.Null(f.Ribbon.Popups.Top);
    });

    [Theory]
    [InlineData(1f)] [InlineData(1.5f)] [InlineData(2f)]
    public void Open_preview_resize_keeps_properties_menu_and_footer_in_separate_regions(float scale) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Draft();
        using var dialog = new RibbonEditorDialog(draft, _ => null, f.Render);
        _ = dialog.Handle; dialog.Scale(new SizeF(scale, scale)); dialog.ClientSize = new((int)(780 * scale), (int)(520 * scale));
        dialog.RenderPopupPreview(RibbonDesignCommitter.Key(f.Menu)); dialog.PerformLayout();
        var popup = (SplitContainer)dialog.Controls.Find("PopupPreviewSplit", true).Single();
        var grid = (PropertyGrid)dialog.Controls.Find("ComponentProperties", true).Single();
        var viewport = dialog.Controls.Find("PopupPreviewViewport", true).Single();
        var outer = (SplitContainer)dialog.Controls.Find("StructureSplit", true).Single();
        var footer = dialog.Controls.Find("DialogFooter", true).Single();
        Assert.True(outer.Bottom <= footer.Top); Assert.True(popup.Panel1.Height > 30); Assert.True(popup.Panel2.Height > 30);
        Assert.True(popup.Panel1.ClientRectangle.Contains(grid.Bounds)); Assert.True(popup.Panel2.ClientRectangle.Contains(viewport.Bounds));
        Assert.False(dialog.SelectPopupPreviewAt(new(-1, -1))); Assert.Equal(0, f.Host.Transactions);
    });

    private static void Capture(RibbonPopupPreview preview, string name)
    {
        if (Environment.GetEnvironmentVariable("RIBBONKIT_POPUP_PREVIEW_CAPTURE") is not { Length: > 0 } path) return;
        Directory.CreateDirectory(path); File.WriteAllBytes(Path.Combine(path, name + ".png"), Convert.FromBase64String(preview.Png));
    }
    public sealed class ThrowingBody : UserControl { public ThrowingBody() => throw new InvalidOperationException("A preview must not instantiate this control."); }
    private sealed class UndoRecorder(IServiceProvider services) : UndoEngine(services)
    {
        private UndoUnit? _unit;
        internal int Count;
        internal void Toggle() => _unit!.Undo();
        protected override void AddUndoUnit(UndoUnit unit) { _unit = unit; Count++; }
    }
    private sealed class Selection : ISelectionService
    {
        private object[] _items = [];
        public object? PrimarySelection => _items.FirstOrDefault(); public int SelectionCount => _items.Length;
        public event EventHandler? SelectionChanged; public event EventHandler? SelectionChanging;
        public bool GetComponentSelected(object component) => _items.Contains(component);
        public System.Collections.ICollection GetSelectedComponents() => _items;
        public void SetSelectedComponents(System.Collections.ICollection? components) => SetSelectedComponents(components, SelectionTypes.Replace);
        public void SetSelectedComponents(System.Collections.ICollection? components, SelectionTypes selectionType)
        { SelectionChanging?.Invoke(this, EventArgs.Empty); _items = components?.Cast<object>().ToArray() ?? []; SelectionChanged?.Invoke(this, EventArgs.Empty); }
    }
    private sealed class DesignSite(ISite original, IServiceProvider services) : ISite
    {
        public IComponent Component => original.Component; public IContainer? Container => original.Container;
        public bool DesignMode => true; public string? Name { get => original.Name; set => original.Name = value; }
        public object? GetService(Type type) => services.GetService(type) ?? original.GetService(type);
    }
    private sealed class Body : UserControl
    {
        internal int Paints;
        protected override void OnPaint(PaintEventArgs e) { Paints++; base.OnPaint(e); }
    }
}
