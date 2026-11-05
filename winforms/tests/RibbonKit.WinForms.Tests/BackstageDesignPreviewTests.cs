using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageDesignPreviewTests
{
    [Fact]
    public void Page_radius_override_keeps_the_retained_preview_opaque_without_creating_page_handles() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var preview = fixture.Selection.Backstage; preview.Open(); var original = fixture.Ribbon.Theme;
        using var before = preview.CreateFrame(120, 120); _ = fixture.Surface.Handle; using var layer = new RibbonBackstagePreviewLayer(fixture.Surface); nint retainedHandle = 0;
        foreach (int radius in new[] { 0, 18, 64 })
        {
            fixture.Ribbon.Theme = original with { BackstagePageCornerRadius = radius, BodyShadowBlur = 12, PopupShadowBlur = 12 };
            using var frame = preview.CreateFrame(120, 120); Assert.Equal(before.Size, frame.Size);
            for (int y = 0; y < frame.Height; y += 7) for (int x = 0; x < frame.Width; x += 7) Assert.Equal(255, frame.GetPixel(x, y).A);
            Assert.True(layer.Present(preview.Bounds, () => preview.CreateFrame(120, 120))); var handle = layer.Handle;
            if (retainedHandle != 0) Assert.Equal(retainedHandle, handle); retainedHandle = handle;
            fixture.Surface.Invalidate(true); fixture.Surface.Update(); layer.Update(); Assert.Equal(handle, layer.Handle); Assert.True(layer.IsPresented);
            Assert.False(fixture.Body.IsHandleCreated); Assert.False(fixture.Input.IsHandleCreated); Assert.Null(fixture.Body.Parent); Assert.Equal(0, fixture.Body.Paints); Assert.False(fixture.Ribbon.IsBackstageOpen);
        }
        fixture.Ribbon.Theme = original; using var reset = preview.CreateFrame(120, 120);
        using var a = new MemoryStream(); using var b = new MemoryStream(); before.Save(a, System.Drawing.Imaging.ImageFormat.Png); reset.Save(b, System.Drawing.Imaging.ImageFormat.Png); Assert.Equal(a.ToArray(), b.ToArray());
    });

    [Fact]
    public void Geometry_overrides_share_opaque_preview_bounds_without_hosting_native_pages() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var preview = fixture.Selection.Backstage; preview.Open();
        var originalTheme = fixture.Ribbon.Theme; var original = preview.CaptureState()!;
        fixture.Ribbon.Theme = originalTheme with { BackstageMinimumRowHeight = 62, BackstageNavigationPadding = 14,
            BackstageBackButtonSize = 52, BackstageBackButtonInset = 28, BackstageBackButtonBottomGap = 32, BackstageIconTextGap = 22, BackstagePagePadding = 18 };
        var state = preview.CaptureState()!; int S(int n) => RibbonLayoutEngine.Scale(n, fixture.Ribbon.DeviceDpi);
        Assert.Equal(S(28), preview.BackBounds.Left); Assert.Equal(preview.Bounds.Top + S(28), preview.BackBounds.Top);
        Assert.Equal(S(62), preview.Layout.RowHeight); Assert.Equal(original.TextBounds.Left + S(18), state.TextBounds.Left);
        using var layer = new RibbonBackstagePreviewLayer(fixture.Surface); layer.Present(preview.Bounds, () => preview.CreateFrame(120, 120));
        var handle = layer.Handle; preview.Click(Center(preview.Rows().Single(row => row.Item == fixture.Second).VisibleBounds));
        layer.Present(preview.Bounds, () => preview.CreateFrame(120, 120)); Assert.Equal(handle, layer.Handle);
        Assert.Equal(0, fixture.Body.Paints); Assert.Null(fixture.Body.Parent); Assert.False(fixture.Ribbon.IsBackstageOpen);
        Assert.Same(fixture.First, fixture.Ribbon.Backstage.SelectedPage);
        fixture.Ribbon.Theme = originalTheme; Assert.Equal(original.TextBounds, preview.CaptureState()!.TextBounds);
    });

    [Fact]
    public void Page_selection_uses_one_placeholder_and_never_dirties_the_page_body() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var preview = fixture.Selection.Backstage; preview.Open();
        var before = preview.CaptureState()!; var page = preview.Layout.Page; page.Offset(preview.Bounds.Location);
        preview.Click(Center(preview.Rows().Single(row => row.Item == fixture.Second).VisibleBounds));
        var after = preview.CaptureState()!;
        Assert.Equal(before.Text, after.Text); Assert.Equal(before.TextBounds, after.TextBounds);
        Assert.DoesNotContain(RibbonBackstagePreviewState.Changes(before, after), dirty => dirty.IntersectsWith(page));
        Assert.Same(fixture.Second, fixture.Service.PrimarySelection); Assert.Same(fixture.Second, preview.Page);
        Assert.Same(fixture.First, fixture.Ribbon.Backstage.SelectedPage); Assert.Null(fixture.Body.Parent); Assert.Equal(0, fixture.Body.Paints);
    });

    [Fact]
    public void Shared_placeholder_layer_updates_navigation_and_properties_without_touching_native_page_bodies() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var preview = fixture.Selection.Backstage; _ = fixture.Surface.Handle; preview.Open();
        using var layer = new RibbonBackstagePreviewLayer(fixture.Surface);
        layer.Present(preview.Bounds, () => preview.CreateFrame(120, 120)); var handle = layer.Handle;
        using var first = new Bitmap(900, 500); using (var graphics = Graphics.FromImage(first)) Assert.True(layer.PaintRetained(graphics, Point.Empty));
        preview.Click(Center(preview.Rows().Single(row => row.Item == fixture.Second).VisibleBounds));
        layer.Present(preview.Bounds, () => preview.CreateFrame(120, 120)); Assert.Equal(handle, layer.Handle);
        using var second = new Bitmap(900, 500); using (var graphics = Graphics.FromImage(second)) Assert.True(layer.PaintRetained(graphics, Point.Empty));
        var page = preview.Layout.Page; page.Offset(preview.Bounds.Location);
        for (int y = page.Top; y < page.Bottom; y++) for (int x = page.Left; x < page.Right; x++) Assert.Equal(first.GetPixel(x, y), second.GetPixel(x, y));
        Assert.Same(fixture.Second, fixture.Service.PrimarySelection); Assert.Equal(0, fixture.Body.Paints); Assert.Null(fixture.Body.Parent);
        preview.Close(); layer.HidePreview(); Assert.False(layer.IsPresented); Assert.False(fixture.Ribbon.IsBackstageOpen);
    });

    [Fact]
    public void Page_switch_dirty_regions_cover_pixel_changes_and_exclude_unchanged_page_body() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var preview = fixture.Selection.Backstage; preview.Open();
        var before = preview.CaptureState();
        using var first = new Bitmap(fixture.Surface.Width, fixture.Surface.Height); using (var paint = Graphics.FromImage(first)) preview.Paint(paint, Point.Empty);
        preview.Click(Center(preview.Rows().Single(row => ReferenceEquals(row.Item, fixture.Second)).VisibleBounds));
        var after = preview.CaptureState(); using var dirty = new Region(); dirty.MakeEmpty();
        foreach (var bounds in RibbonBackstagePreviewState.Changes(before, after)) dirty.Union(bounds);
        Assert.False(dirty.IsVisible(fixture.Surface.ClientSize.Width - 10, fixture.Surface.ClientSize.Height - 10));
        using var second = new Bitmap(first.Width, first.Height); using (var paint = Graphics.FromImage(second)) preview.Paint(paint, Point.Empty);
        int changed = 0;
        for (int y = 0; y < first.Height; y++) for (int x = 0; x < first.Width; x++)
            if (first.GetPixel(x, y) != second.GetPixel(x, y))
            { changed++; Assert.True(dirty.IsVisible(x, y), $"Changed preview pixel outside dirty region: {x},{y}"); }
        Assert.True(changed > 0); Assert.Empty(RibbonBackstagePreviewState.Changes(after, preview.CaptureState()));
    });

    [Fact]
    public void Preview_composition_does_not_expose_a_partially_drawn_frame() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); fixture.Ribbon.Renderer = new FailingRenderer(); fixture.Selection.Backstage.Open();
        using var pixels = new Bitmap(fixture.Surface.Width, fixture.Surface.Height); using var graphics = Graphics.FromImage(pixels); graphics.Clear(Color.Magenta);
        Assert.Throws<InvalidOperationException>(() => fixture.Selection.Backstage.Paint(graphics, Point.Empty));
        Assert.Equal(Color.Magenta.ToArgb(), pixels.GetPixel(5, 5).ToArgb()); Assert.Equal(0, fixture.Body.Paints);
    });

    [Fact]
    public void Buffered_preview_preserves_caption_origin_and_the_target_clip() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); using var root = new RibbonForm { ClientSize = new(900, 500) };
        root.Controls.Add(fixture.Ribbon); using var selection = new RibbonDesignSelection(fixture.Ribbon, fixture.Service, root);
        selection.Backstage.Open(); Assert.Equal(root.CaptionHeight, selection.Backstage.Bounds.Top);
        using var pixels = new Bitmap(950, 550); using var graphics = Graphics.FromImage(pixels); graphics.Clear(Color.Magenta);
        var clip = new Rectangle(50, 100, 200, 80); graphics.SetClip(clip);
        selection.Backstage.Paint(graphics, new Point(20, 30));
        Assert.Equal(Color.Magenta.ToArgb(), pixels.GetPixel(30, 35).ToArgb()); // retained caption
        Assert.Equal(Color.Magenta.ToArgb(), pixels.GetPixel(49, 110).ToArgb());
        Assert.NotEqual(Color.Magenta.ToArgb(), pixels.GetPixel(60, 110).ToArgb());
        Assert.Equal(Color.Magenta.ToArgb(), pixels.GetPixel(251, 110).ToArgb());
        Assert.Equal(0, fixture.Body.Paints); Assert.Null(fixture.Body.Parent);
    });

    [Fact]
    public void Each_preview_gesture_notifies_once_and_selecting_the_same_entry_does_not_repaint() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var preview = fixture.Selection.Backstage; int notifications = 0;
        fixture.Selection.Changed += (_, _) => notifications++;
        preview.Open(); Assert.Equal(1, notifications);
        notifications = 0; var second = Center(preview.Rows().Single(row => ReferenceEquals(row.Item, fixture.Second)).VisibleBounds);
        preview.Click(second); Assert.Equal(1, notifications);
        notifications = 0; preview.Click(second); Assert.Equal(0, notifications);
        preview.Click(Center(preview.BackBounds)); Assert.Equal(1, notifications);
    });

    [Fact]
    public void File_and_back_toggle_only_the_designer_overlay_without_runtime_events_or_authoring_changes() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var ribbon = fixture.Ribbon; var preview = fixture.Selection.Backstage;
        var initialTab = ribbon.SelectedTab; var initialPage = ribbon.Backstage.SelectedPage; int lifecycle = 0;
        ribbon.IsBackstageOpenChanged += (_, _) => lifecycle++; ribbon.Backstage.Opening += (_, _) => lifecycle++;
        ribbon.Backstage.Opened += (_, _) => lifecycle++; ribbon.Backstage.Closing += (_, _) => lifecycle++; ribbon.Backstage.Closed += (_, _) => lifecycle++;
        var before = RibbonBackstageCommitter.Read(ribbon, fixture.Host).Revision;
        int transactions = fixture.Host.Transactions, changes = fixture.Host.Changes.Changed;
        var file = ribbon.Snapshot.Targets.Single(target => target.Kind == TargetKind.FileMenu);
        Assert.True(fixture.Selection.CanSelectAt(Center(file.VisibleBounds))); Assert.True(fixture.Selection.SelectAt(Center(file.VisibleBounds)));
        Assert.True(preview.IsOpen); Assert.False(ribbon.IsBackstageOpen); Assert.Same(initialPage, ribbon.Backstage.SelectedPage); Assert.Same(initialTab, ribbon.SelectedTab);
        Assert.True(preview.Click(Center(preview.BackBounds))); Assert.False(preview.IsOpen);
        Assert.Equal(0, lifecycle); Assert.Equal(before, RibbonBackstageCommitter.Read(ribbon, fixture.Host).Revision);
        Assert.Equal(transactions, fixture.Host.Transactions); Assert.Equal(changes, fixture.Host.Changes.Changed);
        Assert.Same(ribbon, fixture.Service.PrimarySelection);
    });

    [Fact]
    public void Navigation_selects_components_and_keeps_command_clicks_and_live_selected_page_untouched() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var preview = fixture.Selection.Backstage; int clicks = 0;
        fixture.Command.Click += (_, _) => clicks++; var original = fixture.Ribbon.Backstage.SelectedPage;
        preview.Open();
        Assert.True(preview.Click(Center(preview.Rows().Single(row => ReferenceEquals(row.Item, fixture.Second)).VisibleBounds)));
        Assert.Same(fixture.Second, fixture.Service.PrimarySelection); Assert.Same(fixture.Second, preview.Page); Assert.Same(original, fixture.Ribbon.Backstage.SelectedPage);
        Assert.True(preview.Click(Center(preview.Rows().Single(row => ReferenceEquals(row.Item, fixture.Command)).VisibleBounds)));
        Assert.Same(fixture.Command, fixture.Service.PrimarySelection); Assert.True(preview.IsOpen); Assert.Equal(0, clicks);
        var draft = RibbonBackstageCommitter.Read(fixture.Ribbon, fixture.Host); Assert.Equal("site:saveCommand", draft.SelectionKey);
        Assert.True(preview.Click(new(fixture.Surface.ClientSize.Width - 5, fixture.Surface.ClientSize.Height - 5)));
        Assert.Equal(0, clicks); Assert.Same(fixture.Command, fixture.Service.PrimarySelection);
    });

    [Fact]
    public void Overlay_paint_never_paints_or_reparents_native_page_content_and_preserves_its_editor_state() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var preview = fixture.Selection.Backstage;
        var handle = fixture.Body.Handle; var editorHandle = fixture.Input.Handle; fixture.Input.Select(1, 3);
        preview.Open(); using var pixels = new Bitmap(fixture.Surface.Width, fixture.Surface.Height); using var graphics = Graphics.FromImage(pixels);
        preview.Paint(graphics, Point.Empty); preview.Click(Center(preview.BackBounds));
        Assert.Equal(0, fixture.Body.Paints); Assert.Null(fixture.Body.Parent); Assert.Same(fixture.Body, fixture.First.Content);
        Assert.Equal(handle, fixture.Body.Handle); Assert.Equal(editorHandle, fixture.Input.Handle); Assert.Equal("Native notes", fixture.Input.Text);
        Assert.Equal(1, fixture.Input.SelectionStart); Assert.Equal(3, fixture.Input.SelectionLength); Assert.False(fixture.Body.IsDisposed);
    });

    [Fact]
    public void Properties_selection_can_reveal_a_hidden_disabled_page_and_tab_or_external_selection_exits_preview() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); fixture.Second.Visible = false; fixture.Second.Enabled = false;
        var preview = fixture.Selection.Backstage; preview.Open(); fixture.Service.SetSelectedComponents(new[] { fixture.Second });
        Assert.Same(fixture.Second, preview.Page); Assert.Contains(preview.Rows(), row => ReferenceEquals(row.Item, fixture.Second));
        Assert.False(fixture.Second.Visible); Assert.False(fixture.Second.Enabled); Assert.True(fixture.Selection.Active);
        fixture.Service.SetSelectedComponents(new[] { fixture.Ribbon.Tabs[0] }); Assert.False(preview.IsOpen); Assert.True(fixture.Selection.Active);
        preview.Open(); using var other = new TextBox(); fixture.Service.SetSelectedComponents(new[] { other });
        Assert.False(preview.IsOpen); Assert.False(fixture.Selection.Active);
    });

    [Theory]
    [InlineData(480, 320)] [InlineData(900, 500)]
    public void Preview_tracks_root_client_bounds_and_brings_a_selected_overflow_page_into_view(int width, int height) => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var preview = fixture.Selection.Backstage; fixture.Surface.ClientSize = new(width, height);
        for (int i = 0; i < 25; i++) fixture.Ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Page " + i });
        preview.Open(); var last = fixture.Ribbon.Backstage.Items.Last(); fixture.Service.SetSelectedComponents(new[] { last });
        Assert.Equal(fixture.Surface.ClientRectangle, preview.Bounds); Assert.Contains(preview.Rows(), row => ReferenceEquals(row.Item, last) && !row.VisibleBounds.IsEmpty);
        Assert.All(preview.Rows(), row => Assert.True(preview.Bounds.Contains(row.VisibleBounds)));
        Assert.True(preview.Bounds.Contains(preview.BackBounds)); Assert.False(preview.Click(new(-1, -1)));
        Assert.True(preview.Click(Center(preview.BackBounds))); Assert.False(preview.IsOpen);
    });

    [Fact]
    public void Explicit_preview_leaves_default_file_mode_hidden_state_and_disposal_ownership_unchanged() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); fixture.Ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu; fixture.Ribbon.FileMenu.Visible = false;
        Assert.False(fixture.Selection.Backstage.CanOpenFromFile(new(12, 12)));
        fixture.Selection.Backstage.Open(); Assert.True(fixture.Selection.Backstage.IsOpen);
        fixture.Selection.Dispose(); Assert.False(fixture.Selection.Backstage.IsOpen);
        Assert.False(fixture.Ribbon.FileMenu.Visible); Assert.Equal(RibbonFileMenuMode.CommandMenu, fixture.Ribbon.FileMenuMode);
        Assert.False(fixture.Body.IsDisposed); Assert.Null(fixture.Body.Parent);
    });

    [Fact]
    public void Footer_surface_preview_selects_bottom_components_and_scrolls_inline_footer_after_resize() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var ribbon = fixture.Ribbon;
        ribbon.Backstage.Items.Remove(fixture.Second); ribbon.Backstage.FooterItems.AddRange(new RibbonMenuSeparatorItem(), fixture.Second);
        for (int i = 0; i < 12; i++) ribbon.Backstage.Items.Add(new RibbonButtonItem { Text = "Command " + i });
        var preview = fixture.Selection.Backstage; preview.Open(); var row = preview.Rows().Single(row => row.Item == fixture.Second);
        Assert.Equal(preview.Bounds.Bottom - preview.Layout.Inset, row.Bounds.Bottom);
        preview.Click(Center(row.VisibleBounds)); Assert.Same(fixture.Second, fixture.Service.PrimarySelection); Assert.Same(fixture.Second, preview.Page);
        fixture.Surface.ClientSize = new(900, 160); fixture.Service.SetSelectedComponents(new[] { fixture.Second });
        Assert.True(preview.Layout.FooterInline); Assert.Contains(preview.Rows(), row => row.Item == fixture.Second && row.VisibleBounds == row.Bounds);
        using var pixels = new Bitmap(900, 160); using var graphics = Graphics.FromImage(pixels); preview.Paint(graphics, Point.Empty);
        Assert.Null(fixture.Body.Parent); Assert.Equal(0, fixture.Body.Paints); Assert.Same(fixture.First, ribbon.Backstage.SelectedPage);
    });

    private static Point Center(Rectangle bounds) { Assert.False(bounds.IsEmpty); return new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2); }
    private sealed class Fixture : IDisposable
    {
        internal readonly DesignerAuthoringTests.Host Host = new();
        internal readonly SelectionService Service = new();
        internal readonly Form Surface = new() { ClientSize = new(900, 500) };
        internal readonly RibbonControl Ribbon = new();
        internal readonly BodyControl Body = new();
        internal readonly TextBox Input = new() { Text = "Native notes" };
        internal readonly RibbonBackstagePageItem First = new() { Text = "Info" }, Second = new() { Text = "Print" };
        internal readonly RibbonButtonItem Command = new() { Text = "Save" };
        internal readonly RibbonDesignSelection Selection;
        internal Fixture()
        {
            Host.Selection = Service; Host.Container.Add(Ribbon, "ribbon"); Ribbon.Site = new DesignSite(Ribbon.Site!, Host); Surface.Controls.Add(Ribbon);
            Ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); Ribbon.FileMenu.Visible = true; Ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
            Body.Controls.Add(Input); First.Content = Body;
            Host.Container.Add(First, "infoPage"); Host.Container.Add(Second, "printPage"); Host.Container.Add(Command, "saveCommand");
            Ribbon.Backstage.Items.AddRange(First, Command, Second); Service.SetSelectedComponents(new[] { Ribbon });
            Selection = new(Ribbon, Service, Surface);
        }
        public void Dispose() { Selection.Dispose(); Host.Dispose(); Body.Dispose(); Surface.Dispose(); }
    }
    private sealed class BodyControl : UserControl
    {
        internal int Paints;
        protected override void OnPaint(PaintEventArgs e) { Paints++; throw new InvalidOperationException("Native page content must not be painted by preview."); }
    }
    private sealed class FailingRenderer : RibbonOffice2024Renderer
    {
        public override void DrawBackstageEntry(RibbonPaintContext context, RibbonItem? item, bool selected) => throw new InvalidOperationException("Incomplete synthetic frame.");
    }
    private sealed class DesignSite(ISite site, IServiceProvider services) : ISite
    {
        public bool DesignMode => true; public IComponent Component => site.Component; public IContainer? Container => site.Container;
        public string? Name { get => site.Name; set => site.Name = value; }
        public object? GetService(Type type) => services.GetService(type);
    }
    private sealed class SelectionService : ISelectionService
    {
        private object[] _selected = [];
        public object? PrimarySelection => _selected.FirstOrDefault(); public int SelectionCount => _selected.Length;
        public event EventHandler? SelectionChanged; public event EventHandler? SelectionChanging;
        public bool GetComponentSelected(object component) => _selected.Contains(component); public ICollection GetSelectedComponents() => _selected;
        public void SetSelectedComponents(ICollection? components) => SetSelectedComponents(components, SelectionTypes.Replace);
        public void SetSelectedComponents(ICollection? components, SelectionTypes selectionType)
        { SelectionChanging?.Invoke(this, EventArgs.Empty); _selected = components?.Cast<object>().ToArray() ?? []; SelectionChanged?.Invoke(this, EventArgs.Empty); }
    }
}
