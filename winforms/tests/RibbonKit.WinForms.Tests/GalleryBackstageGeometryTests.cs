using System.Text.Json;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GalleryBackstageGeometryTests
{
    private static RibbonTheme Roomier => RibbonThemes.Office2024Light with
    {
        GalleryContentPadding = 10, GalleryUtilityWidth = 40, GalleryTilePadding = 10,
        GalleryHeadingHorizontalPadding = 16, GalleryHeadingVerticalPadding = 12,
        BackstageMinimumRowHeight = 62, BackstageNavigationPadding = 14, BackstageBackButtonSize = 52,
        BackstageBackButtonInset = 28, BackstageBackButtonBottomGap = 32, BackstageIconTextGap = 22, BackstagePagePadding = 18
    };
    public static TheoryData<string, bool> Roles => new()
    {
        { nameof(RibbonTheme.GalleryContentPadding), false }, { nameof(RibbonTheme.GalleryUtilityWidth), true },
        { nameof(RibbonTheme.GalleryTilePadding), false }, { nameof(RibbonTheme.GalleryHeadingHorizontalPadding), false }, { nameof(RibbonTheme.GalleryHeadingVerticalPadding), false },
        { nameof(RibbonTheme.BackstageMinimumRowHeight), true }, { nameof(RibbonTheme.BackstageNavigationPadding), false },
        { nameof(RibbonTheme.BackstageBackButtonSize), true }, { nameof(RibbonTheme.BackstageBackButtonInset), false },
        { nameof(RibbonTheme.BackstageBackButtonBottomGap), false }, { nameof(RibbonTheme.BackstageIconTextGap), false }, { nameof(RibbonTheme.BackstagePagePadding), false }
    };

    [Theory] [MemberData(nameof(Roles))]
    public void Roles_validate_atomically_and_reset_independently(string name, bool size) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        var original = ribbon.Theme; var property = typeof(RibbonTheme).GetProperty(name)!;
        foreach (int value in new[] { size ? 0 : -1, 1025 })
        {
            var bad = Roomier with { }; property.SetValue(bad, value);
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = bad);
            Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2024Light, ribbon.BuiltInTheme);
        }
        foreach (int value in new[] { size ? 1 : 0, 1024 })
        {
            var valid = Roomier with { CommandContentVerticalPadding = 16, MenuLeadingPadding = 14, QuickAccessItemGap = 6 };
            property.SetValue(valid, value); ribbon.Theme = valid; Assert.Same(original, other.Theme);
            var reset = valid with { }; property.SetValue(reset, null); ribbon.Theme = reset;
            foreach (var (role, _) in Roles.Select(row => ((string)row[0], (bool)row[1])))
                Assert.Equal(role == name ? null : typeof(RibbonTheme).GetProperty(role)!.GetValue(valid), typeof(RibbonTheme).GetProperty(role)!.GetValue(ribbon.Theme));
            Assert.Equal(16, ribbon.Theme.CommandContentVerticalPadding); Assert.Equal(14, ribbon.Theme.MenuLeadingPadding); Assert.Equal(6, ribbon.Theme.QuickAccessItemGap);
        }
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Gallery_shared_slots_scale_constrain_scroll_and_reset(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Density = density };
        var gallery = new RibbonGalleryItem { IsInRibbon = true, MaxVisibleRows = 2, TileWidth = 90, TileHeight = 80 };
        var group = new RibbonGroup(); group.Items.Add(gallery); var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        for (int i = 0; i < 80; i++) gallery.Items.Add(new RibbonGalleryTile { Text = "Style " + i, GroupName = i < 40 ? "First" : "Second" });
        var selected = (RibbonGalleryTile)gallery.Items[7]; gallery.SelectedItem = selected;
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 24 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi); using var font = new Font("Segoe UI", points * dpi / 96f);
            var work = new Size(S(600), S(400)); var stripSize = new Size(S(380), S(110)); ribbon.Theme = RibbonThemes.Office2024Light;
            var before = RibbonGalleryLayout.Build(gallery, font, dpi, work); var beforeStrip = RibbonGalleryStripLayout.Build(gallery, stripSize, dpi, 0);
            ribbon.Theme = Roomier; var popup = RibbonGalleryLayout.Build(gallery, font, dpi, work); var strip = RibbonGalleryStripLayout.Build(gallery, stripSize, dpi, 0);
            Assert.Equal(S(10), popup.Targets[0].Bounds.Left); Assert.Equal(font.Height + S(24), popup.Headings[0].Bounds.Height);
            Assert.True(popup.Targets[0].Bounds.Height >= 2 * font.Height + S(54)); Assert.True(popup.Extent.Height > popup.ViewportHeight);
            Assert.Equal(S(40), strip.Up.Width); Assert.Equal(S(10), strip.Up.Left - strip.Viewport.Right);
            Assert.Equal(strip.Up.Bottom, strip.Down.Top); Assert.Equal(strip.Down.Bottom, strip.Expand.Top); Assert.Equal(strip.Viewport.Bottom, strip.Expand.Bottom);
            Assert.Equal(gallery.InRibbonColumnCount, strip.Tiles.Count); Assert.All(strip.Tiles, tile => Assert.True(strip.Viewport.Contains(tile.Bounds)));
            var last = RibbonGalleryStripLayout.Build(gallery, stripSize, dpi, int.MaxValue); Assert.Equal(last.LastRow, last.Row); Assert.Same(gallery.Items[^1], last.Tiles[^1].Tile);
            foreach (int padding in new[] { 0, 1024 })
            {
                ribbon.Theme = Roomier with { GalleryContentPadding = padding, GalleryUtilityWidth = 1024 };
                var tiny = RibbonGalleryStripLayout.Build(gallery, new(S(19), S(13)), dpi, 0);
                Assert.True(tiny.Viewport.Width >= 0 && tiny.Viewport.Height >= 0); Assert.True(new Rectangle(Point.Empty, new(S(19), S(13))).Contains(tiny.Expand));
                var narrow = RibbonGalleryLayout.Build(gallery, font, dpi, new(S(80), S(100)));
                Assert.True(narrow.Extent.Width <= S(80)); Assert.True(narrow.ViewportHeight >= 1);
            }
            ribbon.Theme = RibbonThemes.Office2024Light;
            var reset = RibbonGalleryLayout.Build(gallery, font, dpi, work); var resetStrip = RibbonGalleryStripLayout.Build(gallery, stripSize, dpi, 0);
            Assert.Equal(before.Extent, reset.Extent); Assert.Equal(before.Targets, reset.Targets); Assert.Equal(before.Headings, reset.Headings);
            Assert.Equal(beforeStrip.Viewport, resetStrip.Viewport); Assert.Equal(beforeStrip.Tiles, resetStrip.Tiles);
        }
        Assert.Same(selected, gallery.SelectedItem); Assert.Equal(90, gallery.TileWidth); Assert.Equal(80, gallery.TileHeight);
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Backstage_auto_width_rows_footer_and_page_host_share_spacing(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Density = density };
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Document information and permissions" });
        ribbon.Backstage.FooterItems.Add(new RibbonButtonItem { Text = "Options" }); ribbon.Backstage.MaximumNavigationWidth = 900;
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 24 })
        {
            using var font = new Font("Segoe UI", points * dpi / 96f); int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var size = new Size(S(1200), S(700)); ribbon.Theme = RibbonThemes.Office2024Light;
            var before = RibbonBackstageLayout.Build(size, font, dpi, ribbon.Backstage); ribbon.Theme = Roomier;
            var changed = RibbonBackstageLayout.Build(size, font, dpi, ribbon.Backstage);
            Assert.Equal(Math.Max(S(62), font.Height + S(16)), changed.RowHeight); Assert.Equal(S(28), changed.Back.Left); Assert.Equal(S(28), changed.Back.Top);
            Assert.Equal(S(52), changed.Back.Width); Assert.Equal(changed.Back.Bottom + S(32), changed.Navigation.Top);
            Assert.Equal(S(14), changed.Arrange(ribbon.Backstage)[0].Bounds.Left); Assert.True(changed.Page.Left > before.Page.Left);
            var content = changed.PageContentBounds(1);
            Assert.Equal(changed.Page.Left + 1 + S(18), content.Left); Assert.Equal(changed.Page.Right - S(18), content.Right);
            foreach (int width in new[] { 0, 19, 220, 1200 })
            foreach (int height in new[] { 0, 13, 110, 700 })
            {
                ribbon.Theme = Roomier with { BackstageNavigationPadding = 1024, BackstagePagePadding = 1024, BackstageBackButtonInset = 1024 };
                var small = RibbonBackstageLayout.Build(new(S(width), S(height)), font, dpi, ribbon.Backstage);
                Assert.True(small.Back.Width >= 0 && small.Back.Bottom <= S(height)); Assert.True(small.Navigation.Height >= 0);
                Assert.True(small.PageContentBounds().Width >= 0); Assert.True(small.PageContentBounds().Height >= 0);
            }
            ribbon.Theme = RibbonThemes.Office2024Light; Assert.Equal(before, RibbonBackstageLayout.Build(size, font, dpi, ribbon.Backstage));
        }
    });

    [Fact]
    public void Live_overrides_retain_inline_native_cards_and_backstage_page_handles_without_validation() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1400, Height = 800 }; using var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None };
        form.Controls.Add(ribbon); var group = new RibbonGroup { CanResize = false }; var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var gallery = new RibbonGalleryItem { IsInRibbon = true, InRibbonPresenterType = typeof(InlineGalleryPresenterTests.View) };
        gallery.Items.AddRange(Enumerable.Range(0, 15).Select(i => new RibbonGalleryTile { Text = "Style " + i }).ToArray()); gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[7]; group.Items.Add(gallery);
        using var pageBody = new UserControl(); var field = new TextBox { Text = "Retained draft" }; pageBody.Controls.Add(field);
        var page = new RibbonBackstagePageItem { Text = "Info", Content = pageBody }; ribbon.Backstage.Items.Add(page); ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; ribbon.FileMenu.Visible = true;
        form.Show(); Application.DoEvents(); var surface = ribbon.GalleryStrips.InlineSurfaceFor(gallery)!;
        var view = (InlineGalleryPresenterTests.View)surface.PresenterControl; var handle = view.Handle; var child = view.Child.Handle; var selection = gallery.SelectedItem;
        foreach (var theme in new[] { Roomier, RibbonThemes.Office2024Light })
        {
            ribbon.Theme = theme; Application.DoEvents(); Assert.Same(view, ribbon.GalleryStrips.InlineSurfaceFor(gallery)!.PresenterControl);
            Assert.Equal(handle, view.Handle); Assert.Equal(child, view.Child.Handle); Assert.Same(selection, gallery.SelectedItem);
            Assert.Equal(view.Parent!.ClientSize, view.Context.Viewport); Assert.False(view.InputDuringSync);
        }
        ribbon.IsBackstageOpen = true; Application.DoEvents(); Assert.True(ribbon.IsBackstageOpen); Assert.NotNull(pageBody.Parent);
        var parent = pageBody.Parent; nint bodyHandle = pageBody.Handle, fieldHandle = field.Handle;
        field.Focus(); field.Select(2, 4); int validations = 0; field.Validating += (_, e) => { validations++; e.Cancel = true; };
        var oldBounds = pageBody.Parent!.Parent!.Bounds;
        foreach (var theme in new[] { Roomier, RibbonThemes.Office2024Light })
        {
            ribbon.Theme = theme; Application.DoEvents(); Assert.Same(parent, pageBody.Parent); Assert.Equal(bodyHandle, pageBody.Handle); Assert.Equal(fieldHandle, field.Handle);
            Assert.Same(page, ribbon.Backstage.SelectedPage); Assert.Equal("Retained draft", field.Text); Assert.Equal(2, field.SelectionStart); Assert.Equal(4, field.SelectionLength);
            Assert.Equal(0, validations); Assert.True(field.Focused);
        }
        Assert.Equal(oldBounds, pageBody.Parent!.Parent!.Bounds);
    });

    [Fact]
    public void Default_measurements_and_paint_match_saved_baseline() => FoundationTests.Sta(() =>
    {
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        {
            using var ribbon = new RibbonControl { Density = density };
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            ribbon.Renderer = painter;
            using var font = new Font("Segoe UI", points * dpi / 96f);
            var gallery = new RibbonGalleryItem { Text = "Styles", IsInRibbon = true };
            var group = new RibbonGroup { Text = "Gallery", CanResize = false }; group.Items.Add(gallery);
            var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            for (int i = 0; i < 15; i++) gallery.Items.Add(new RibbonGalleryTile { Text = "Style " + i, GroupName = i < 6 ? "Document" : "Other", Icon = RibbonStockIcons.Create("open") });
            ribbon.Backstage.Items.AddRange(new RibbonBackstagePageItem { Text = "Document information" }, new RibbonBackstagePageItem { Text = "Print" });
            ribbon.Backstage.FooterItems.Add(new RibbonButtonItem { Text = "Options" });
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var popup = RibbonGalleryLayout.Build(gallery, font, dpi, new(S(600), S(500)));
            var strip = RibbonGalleryStripLayout.Build(gallery, new(S(360), S(110)), dpi, 0);
            var back = RibbonBackstageLayout.Build(new(S(750), S(440)), font, dpi, ribbon.Backstage);
            var measurement = JsonSerializer.Serialize(new { popup.Extent, popup.ViewportHeight, popup.Columns,
                Targets = popup.Targets.Select(t => t.Bounds), Headings = popup.Headings.Select(h => h.Bounds), strip.Viewport, strip.Up, strip.Down, strip.Expand,
                Tiles = strip.Tiles.Select(t => t.Bounds), back.Back, back.Navigation, back.Page, back.Footer, back.RowHeight, back.Inset,
                Group = ribbon.Engine.Measure(group, 0, font, dpi, ribbon.ClassicLayoutTheme).Width });
            using var bitmap = new Bitmap(S(750), S(440)); using var graphics = Graphics.FromImage(bitmap);
            var context = new RibbonPaintContext(graphics, new(Point.Empty, bitmap.Size), ribbon.Theme, font, dpi) { Density = density };
            painter.DrawBackstageBackground(context, back.Page.Left);
            painter.DrawBackstageEntry(context with { Bounds = back.Back }, null, false);
            foreach (var slot in back.Arrange(ribbon.Backstage)) painter.DrawBackstageEntry(context with { Bounds = slot.Bounds }, slot.Item, true);
            var origin = new Point(back.Page.Left + S(8), S(8));
            foreach (var heading in popup.Headings.Take(1)) { var bounds = heading.Bounds; bounds.Offset(origin); painter.DrawGalleryHeading(context with { Bounds = bounds }, heading.Text); }
            foreach (var target in popup.Targets.Take(6)) { var bounds = target.Bounds; bounds.Offset(origin); painter.DrawGalleryTile(context with { Bounds = bounds }, (RibbonGalleryTile)target.Element!, false); }
            using var png = new MemoryStream(); bitmap.Save(png, System.Drawing.Imaging.ImageFormat.Png);
            string name = $"gallery-backstage-{office}-{density}-{dpi}-{points}";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_GB_CAPTURE") is { Length: > 0 } output)
            { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), png.ToArray()); File.WriteAllText(Path.Combine(output, name + ".json"), measurement); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_GB_BASELINE") is { Length: > 0 } baseline)
            { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png.ToArray()); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".json")), measurement); }
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Override_paint_uses_measured_heading_tile_and_navigation_slots(bool office) => FoundationTests.Sta(() =>
    {
        var record = new PaintRecord(); using RibbonRenderer painter = office ? new Office(record) : new Neutral(record);
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 24 })
        {
            using var font = new Font("Segoe UI", points * dpi / 96f); int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            using var bitmap = new Bitmap(S(720), S(320)); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(Roomier.Surface);
            var context = new RibbonPaintContext(graphics, new(S(12), S(12), S(300), font.Height + S(24)), Roomier, font, dpi) { Density = density };
            painter.DrawGalleryHeading(context, "Heading"); Assert.Equal(context.Bounds.Left + S(16), record.Text.Left);
            var tileBounds = new Rectangle(S(12), context.Bounds.Bottom + S(8), S(180), 2 * font.Height + S(54));
            using var tile = new RibbonGalleryTile { Text = "Tile", Icon = RibbonStockIcons.Create("open") };
            painter.DrawGalleryTile(context with { Bounds = tileBounds }, tile, true);
            Assert.Equal(tileBounds.Left + S(10), record.Text.Left); Assert.Equal(tileBounds.Bottom - S(10), record.Text.Bottom);
            Assert.True(record.Text.Height >= 2 * font.Height);
            using var item = new RibbonBackstagePageItem { Text = "Document information" };
            var row = new Rectangle(S(350), S(60), S(340), Math.Max(S(62), font.Height + S(16)));
            painter.DrawBackstageEntry(context with { Bounds = row }, item, true); Assert.Equal(row.Left + S(54), record.Text.Left); Assert.Equal(row.Right - S(8), record.Text.Right);
            foreach (int padding in new[] { 0, 1024 })
            {
                painter.DrawGalleryTile(context with { Bounds = new(0, 0, 19, 13), Theme = Roomier with { GalleryTilePadding = padding } }, tile, false);
                Assert.True(record.Text.Width >= 0 && record.Text.Height >= 0); Assert.True(new Rectangle(0, 0, 19, 13).Contains(record.Text));
            }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_GB_CAPTURE") is { Length: > 0 } output)
            { Directory.CreateDirectory(output); bitmap.Save(Path.Combine(output, $"override-{office}-{density}-{dpi}-{points}.png")); }
        }
    });

    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void Borrowed_gallery_bodies_and_qat_dropdowns_use_source_spacing_without_replacing_root_view(int route) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1250, Height = 700 }; using var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None, Theme = Roomier };
        form.Controls.Add(ribbon); var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Styles" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var gallery = new RibbonGalleryItem { IsInRibbon = true, UseDropDownPresenterInQuickAccess = true,
            InRibbonPresenterType = typeof(InlineGalleryPresenterTests.View), DropDownPresenterType = typeof(InlineGalleryPresenterTests.View) };
        group.Items.Add(gallery); gallery.Items.AddRange(Enumerable.Range(0, 18).Select(i => new RibbonGalleryTile { Text = "Style " + i }).ToArray()); gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[7];
        var direct = new RibbonQuickAccessDropDownReference { Source = gallery }; var borrowed = new RibbonQuickAccessGroupReference { Source = group }; ribbon.QuickAccessItems.Add(direct);
        form.Show(); Application.DoEvents(); Assert.True(ribbon.GalleryStrips.InlineSurfaceFor(gallery) is not null, string.Join(";", ribbon.Snapshot.Targets.Select(t => $"{t.Kind}:{t.Element?.GetType().Name}:{t.Bounds}:{t.Size}:{t.GalleryDropDown}"))); var root = ribbon.GalleryStrips.InlineSurfaceFor(gallery)!.PresenterControl; var rootHandle = root.Handle;
        if (route == 0) { form.Width = 240; Application.DoEvents(); ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, group))); }
        else if (route == 1) { ribbon.QuickAccessItems.Add(borrowed); ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, borrowed))); }
        else { ribbon.IsMinimized = true; ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.Tab)); }
        var popup = ribbon.Popups.Top!; var body = ribbon.GalleryStrips.InlineSurfaceFor(gallery, popup)!.PresenterControl;
        Assert.NotSame(root, body); Assert.False(root.IsDisposed); Assert.Equal(rootHandle, root.Handle);
        Assert.Equal(RibbonLayoutEngine.Scale(10, ribbon.DeviceDpi), body.Parent!.Left);
        Assert.Equal(((InlineGalleryPresenterTests.View)body).Context.Viewport, body.Parent.ClientSize);
        ribbon.Popups.CloseAll(false); if (route == 1) ribbon.QuickAccessItems.Remove(borrowed); Assert.False(root.IsDisposed); form.Width = 1250; ribbon.IsMinimized = false; Application.DoEvents();
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, direct)));
        var directPopup = ribbon.Popups.Top!; Assert.True(directPopup.HasGalleryPresenter);
        var view = Descendants(directPopup).OfType<InlineGalleryPresenterTests.View>().Single(); Assert.True(view.Context.IsQuickAccess);
        Assert.Equal(RibbonLayoutEngine.Scale(10, ribbon.DeviceDpi), view.Parent!.Left);
        var selection = gallery.SelectedItem; ribbon.Theme = RibbonThemes.Office2024Light; Application.DoEvents();
        Assert.False(gallery.IsDropDownOpen); Assert.Same(selection, gallery.SelectedItem); Assert.Equal(rootHandle, root.Handle);
    });

    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));

    [Fact]
    public void Roomier_keyboard_reveal_commit_and_native_accessibility_follow_measured_targets() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1300, Height = 750 }; using var ribbon = new RibbonControl { Theme = Roomier, AnimationLevel = RibbonAnimationLevel.None };
        form.Controls.Add(ribbon); var gallery = GalleryTests.Attach(ribbon); gallery.IsInRibbon = true; gallery.MaxVisibleRows = 2;
        for (int i = 0; i < 40; i++) gallery.Items.Add(new RibbonGalleryTile { Text = "Style " + i, GroupName = "Styles" });
        gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0]; int commits = 0; gallery.SelectionCommitted += (_, _) => commits++;
        form.Show(); Application.DoEvents(); var strip = ribbon.GalleryStrips.SurfaceFor(gallery)!; ribbon.FocusElement(gallery);
        Key(strip, Keys.End); Key(strip, Keys.Enter); Assert.Same(gallery.Items[^1], gallery.SelectedItem); Assert.Equal(1, commits);
        Assert.Equal(strip.LayoutSnapshot.LastRow, strip.LayoutSnapshot.Row); Key(strip, Keys.F4); var popup = ribbon.Popups.Top!;
        Assert.True(popup.Targets.Single(t => ReferenceEquals(t.Element, gallery.SelectedItem)).VisibleBounds.Height > 0);
        Key(popup, Keys.Home); Key(popup, Keys.Enter); Assert.Same(gallery.Items[0], gallery.SelectedItem); Assert.Equal(2, commits);
        gallery.InRibbonPresenterType = typeof(InlineGalleryPresenterTests.View); Application.DoEvents(); var surface = ribbon.GalleryStrips.InlineSurfaceFor(gallery)!;
        var view = (InlineGalleryPresenterTests.View)surface.PresenterControl; var selected = surface.AccessibilityObject.GetSelected()!;
        var expected = view.RectangleToScreen(view.GetItemBounds(gallery.SelectedItem!));
        Assert.Equal(Rectangle.Intersect(expected, view.Parent!.RectangleToScreen(view.Parent.ClientRectangle)), selected.Bounds);
        var utilities = Enumerable.Range(0, surface.AccessibilityObject.GetChildCount()).Select(i => surface.AccessibilityObject.GetChild(i)!).Where(a => a.Role == AccessibleRole.PushButton).ToArray();
        Assert.Equal(3, utilities.Length); Assert.All(utilities, a => Assert.Equal(RibbonLayoutEngine.Scale(40, ribbon.DeviceDpi), a.Bounds.Width));
        static void Key(Control control, Keys keys)
        { var message = Message.Create(control.Handle, 0x100, (nint)keys, 0); typeof(Control).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(control, [message, keys]); }
    });

    private sealed class PaintRecord { internal Rectangle Text; }
    private sealed class Office(PaintRecord record) : RibbonOffice2024Renderer
    { public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text = c.Bounds; base.DrawText(c, text, color, flags); } }
    private sealed class Neutral(PaintRecord record) : RibbonRenderer
    { public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text = c.Bounds; base.DrawText(c, text, color, flags); } }
}
