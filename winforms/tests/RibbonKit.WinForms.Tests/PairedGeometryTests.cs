using RibbonKit.WinForms.Layout;
using System.Reflection;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class PairedGeometryTests
{
    private static RibbonTheme Roomier => RibbonThemes.Office2024Light with
    { CommandContentHorizontalPadding = 12, CommandContentVerticalPadding = 16, ApplicationMenuNavigationMinimumRowHeight = 64,
        ApplicationMenuPaneMinimumRowHeight = 72, ApplicationMenuFooterMinimumRowHeight = 52, ApplicationMenuPaneHorizontalPadding = 22, ApplicationMenuFooterItemGap = 12 };
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Unset_command_and_application_menu_geometry_matches_saved_defaults(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        {
            using var f = new Fixture(); f.Ribbon.Density = density; using var font = new Font("Segoe UI", points * dpi / 96f); f.Ribbon.Font = font;
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var measured = f.Ribbon.Engine.Measure(f.Group, 0, font, dpi, f.Ribbon.Theme with { BodyHeight = 200 }).Items;
            int rowHeight = S(Math.Max(density == RibbonDensity.Touch ? 45 : 26, (int)Math.Ceiling(font.Height * 96d / dpi) + 8) - 1);
            using var pixels = new Bitmap(S(720), S(740)); using var graphics = Graphics.FromImage(pixels); graphics.Clear(Color.White);
            var context = new RibbonPaintContext(graphics, Rectangle.Empty, f.Ribbon.Theme, font, dpi) { Density = density };
            var data = new List<string>(); int y = S(4);
            foreach (var entry in measured)
            {
                var row = new Rectangle(S(4), y, entry.Bounds.Width, rowHeight); y += rowHeight + S(4);
                data.Add($"command:{entry.Item.Text}:{entry.Bounds}");
                if (entry.Item is RibbonSplitButtonItem split)
                {
                    var parts = RibbonSplitLayout.Calculate(row, split, entry.Size, false, dpi, font);
                    data.Add($"parts:{parts}"); painter.DrawSplitItem(context with { Bounds = parts.Primary, Hot = true }, context with { Bounds = parts.DropDown }, split, entry.Size, false);
                }
                else painter.DrawItem(context with { Bounds = row, Hot = true }, entry.Item, entry.Size, false);
            }
            foreach (var part in new[] { RibbonApplicationMenuPart.Navigation, RibbonApplicationMenuPart.Pane, RibbonApplicationMenuPart.Footer })
            {
                var item = part == RibbonApplicationMenuPart.Navigation ? (RibbonItem)f.Navigation : part == RibbonApplicationMenuPart.Pane ? f.PaneChoice : f.Footer;
                int height = part == RibbonApplicationMenuPart.Footer ? Math.Max(S(density == RibbonDensity.Touch ? 44 : 30), font.Height + S(10))
                    : RibbonApplicationMenuMetrics.RowHeight(item, S(400), font, dpi, density, part == RibbonApplicationMenuPart.Navigation);
                var row = new Rectangle(S(4), y, S(400), height); y += height + S(4);
                data.Add($"app:{part}:{row}"); painter.DrawItem(context with { Bounds = row, ApplicationMenuPart = part, Hot = true }, item, RibbonItemSize.Medium, true);
            }
            var layout = RibbonApplicationMenuLayout.Build(f.Ribbon, f.Ribbon.ApplicationMenu.DefaultPane, new(1100, 900));
            data.Add($"container-native-dpi={f.Ribbon.DeviceDpi}:{layout.Size}/{layout.Frame}/{layout.Navigation}/{layout.Pane}/{layout.Header}/{layout.Footer}");
            data.AddRange(layout.Targets.Select(t => $"target:{t.Element!.Text}:{t.Bounds}/{t.Clip}"));
            using var encoded = new MemoryStream(); pixels.Save(encoded, System.Drawing.Imaging.ImageFormat.Png); var png = encoded.ToArray();
            string name = $"paired-{office}-{density}-{points}-{dpi}";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_PAIRED_CAPTURE") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); File.WriteAllBytes(Path.Combine(capture, name + ".png"), png); File.WriteAllLines(Path.Combine(capture, name + ".txt"), data); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_PAIRED_BASELINE") is { Length: > 0 } baseline)
            { Assert.Equal(File.ReadAllLines(Path.Combine(baseline, name + ".txt")), data); Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png); }
        }
    });

    [Theory]
    [InlineData(nameof(RibbonTheme.CommandContentHorizontalPadding), false)] [InlineData(nameof(RibbonTheme.CommandContentVerticalPadding), false)]
    [InlineData(nameof(RibbonTheme.ApplicationMenuNavigationMinimumRowHeight), true)] [InlineData(nameof(RibbonTheme.ApplicationMenuPaneMinimumRowHeight), true)]
    [InlineData(nameof(RibbonTheme.ApplicationMenuFooterMinimumRowHeight), true)] [InlineData(nameof(RibbonTheme.ApplicationMenuPaneHorizontalPadding), false)]
    [InlineData(nameof(RibbonTheme.ApplicationMenuFooterItemGap), false)]
    public void Paired_roles_validate_atomically_and_reset_independently(string name, bool positive) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); var original = ribbon.Theme; var builtIn = ribbon.BuiltInTheme;
        var property = typeof(RibbonTheme).GetProperty(name)!;
        foreach (int value in new[] { positive ? 0 : -1, 1025 })
        {
            var bad = original with { }; property.SetValue(bad, value);
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = bad); Assert.Same(original, ribbon.Theme); Assert.Equal(builtIn, ribbon.BuiltInTheme);
        }
        foreach (int value in new[] { positive ? 1 : 0, 1024 })
        {
            var valid = original with { }; property.SetValue(valid, value); ribbon.Theme = valid; Assert.Equal(value, property.GetValue(ribbon.Theme)); Assert.Same(original, other.Theme);
            var reset = valid with { }; property.SetValue(reset, null); ribbon.Theme = reset; Assert.Equal(original, ribbon.Theme);
        }
        ribbon.Theme = Roomier; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; Assert.Equal(original, ribbon.Theme);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Command_padding_reserves_paint_and_split_content_at_synthetic_dpis(bool office) => FoundationTests.Sta(() =>
    {
        var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        {
            using var f = new Fixture(); f.Ribbon.Density = density; using var font = new Font("Segoe UI", points * dpi / 96f); f.Ribbon.Font = font;
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var before = f.Ribbon.Engine.Measure(f.Group, 0, font, dpi, f.Ribbon.Theme with { BodyHeight = 240 }).Items;
            f.Ribbon.Theme = Roomier; var after = f.Ribbon.Engine.Measure(f.Group, 0, font, dpi, Roomier with { BodyHeight = 240 }).Items;
            int minimum = Math.Max(font.Height, S(16)) + 2 * S(16);
            using var pixels = new Bitmap(S(600), after.Sum(i => i.Bounds.Height + S(4)) + S(8)); using var graphics = Graphics.FromImage(pixels); graphics.Clear(Color.White); int y = S(4);
            for (int i = 0; i < after.Count; i++)
            {
                var entry = after[i]; var row = new Rectangle(S(4), y, entry.Bounds.Width, entry.Bounds.Height); y = row.Bottom + S(4);
                Assert.True(row.Height >= minimum, $"{density}/{dpi}/{points}/{entry.Item.Text}: height {row.Height} < {minimum}");
                Assert.True(row.Width >= before[i].Bounds.Width + 2 * (S(12) - S(5)), $"{density}/{dpi}/{points}/{entry.Item.Text}: width {row.Width}, previous {before[i].Bounds.Width}");
                var primary = row;
                if (entry.Item is RibbonSplitButtonItem split)
                {
                    var parts = RibbonSplitLayout.Calculate(row, split, entry.Size, false, dpi, font); primary = parts.Primary;
                    var context = new RibbonPaintContext(graphics, primary, Roomier, font, dpi) { Density = density, Hot = true };
                    painter.DrawSplitItem(context, context with { Bounds = parts.DropDown }, split, entry.Size, false);
                    f.Ribbon.Theme = RibbonThemes.Office2024Light; var old = RibbonSplitLayout.Calculate(before[i].Bounds, split, entry.Size, false, dpi, font); f.Ribbon.Theme = Roomier;
                    Assert.True(primary.Width >= old.Primary.Width + 2 * (S(12) - S(5)));
                }
                else painter.DrawItem(new(graphics, row, Roomier, font, dpi) { Density = density, Hot = true }, entry.Item, entry.Size, false);
                var content = RibbonContentLayout.Calculate(primary, entry.Size, false, true, dpi, font, density, theme: Roomier);
                Assert.True(primary.Contains(content.Icon)); Assert.InRange(Math.Abs(2 * content.Icon.Top + content.Icon.Height - 2 * primary.Top - primary.Height), 0, 1);
                if (entry.Size == RibbonItemSize.Small) Assert.InRange(Math.Abs(2 * content.Icon.Left + content.Icon.Width - 2 * primary.Left - primary.Width), 0, 1);
                else { Assert.Equal(primary.Left + S(6) + S(12) - S(5), content.Icon.Left); Assert.Equal(primary.Left + S(12) + S(26), record.Text[entry.Item.Text].Left); }
            }
            Assert.True(RibbonApplicationMenuMetrics.RowHeight(f.Navigation, S(400), font, dpi, density, true, Roomier) >= S(64));
            if (Environment.GetEnvironmentVariable("RIBBONKIT_PAIRED_CAPTURE") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); pixels.Save(Path.Combine(capture, $"commands-{office}-{density}-{points}-{dpi}.png")); }
        }
    });

    [Theory] [InlineData(RibbonLayoutMode.Classic)] [InlineData(RibbonLayoutMode.Simplified)]
    public void Native_command_rows_grow_without_changing_editor_handles_or_large_only_tabs(RibbonLayoutMode mode) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var form = new Form { Width = 1500, Height = 800 }; form.Controls.Add(f.Ribbon);
        foreach (var item in f.Group.Items) { item.SimplifiedPlacement = RibbonSimplifiedPlacement.Inline; item.SimplifiedSize = RibbonCommandContentMetrics.Size(item, 0); }
        var text = new RibbonTextBoxItem { Text = "Name", Value = "Retained", InputWidth = 100 }; f.Group.Items.Add(text);
        var largeTab = new RibbonTab(); var largeGroup = new RibbonGroup { CanResize = false }; largeGroup.Items.Add(new RibbonButtonItem { SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Large) }); largeTab.Groups.Add(largeGroup); f.Ribbon.Tabs.Add(largeTab);
        var commandTab = f.Ribbon.SelectedTab; f.Ribbon.LayoutMode = mode; form.Show(); Application.DoEvents();
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            f.Ribbon.Density = density; f.Ribbon.Theme = RibbonThemes.Office2024Light; Application.DoEvents(); int oldHeight = f.Ribbon.Height;
            var editor = mode == RibbonLayoutMode.Classic ? f.Ribbon.Editors.EditorFor(text) : null; nint handle = editor?.Handle ?? 0; int editorWidth = editor?.Width ?? 0;
            f.Ribbon.Theme = Roomier; Application.DoEvents(); Assert.True(f.Ribbon.Height > oldHeight);
            var targets = f.Ribbon.Snapshot.Targets.Where(t => t.Kind == TargetKind.Item && t.Element is RibbonItem item && RibbonCommandContentMetrics.Applies(item, t.Size)).ToArray();
            Assert.NotEmpty(targets); Assert.All(targets, t => Assert.True(t.Bounds.Height >= Math.Max(f.Ribbon.Font.Height, RibbonLayoutEngine.Scale(16, f.Ribbon.DeviceDpi)) + 2 * RibbonLayoutEngine.Scale(16, f.Ribbon.DeviceDpi)));
            if (editor is not null) { Assert.Equal(handle, f.Ribbon.Editors.EditorFor(text)!.Handle); Assert.Equal(editorWidth, editor.Width); Assert.Equal("Retained", editor.Text); }
            f.Ribbon.SelectedTab = largeTab; Application.DoEvents(); int paddedLarge = f.Ribbon.Height;
            f.Ribbon.Theme = RibbonThemes.Office2024Light; Application.DoEvents(); Assert.Equal(paddedLarge, f.Ribbon.Height);
            f.Ribbon.SelectedTab = commandTab; Application.DoEvents(); Assert.Equal(oldHeight, f.Ribbon.Height);
        }
        form.Close();
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Application_menu_rows_insets_footer_and_scroll_reveal_share_the_new_geometry(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.Density = density;
        var original = RibbonApplicationMenuLayout.Build(f.Ribbon, f.Ribbon.ApplicationMenu.DefaultPane, new(1200, 1400)); f.Ribbon.Theme = Roomier;
        var layout = RibbonApplicationMenuLayout.Build(f.Ribbon, f.Ribbon.ApplicationMenu.DefaultPane, new(1200, 1400));
        int S(int n) => RibbonLayoutEngine.Scale(n, f.Ribbon.DeviceDpi);
        Assert.True(layout.Size.Width > original.Size.Width); Assert.Equal(original.Pane.Width, layout.Pane.Width);
        Assert.Equal(layout.Navigation.Right + 1 + S(22), layout.Pane.Left);
        for (int i = 0; i < 9; i++) { f.Ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Command " + i }); f.Ribbon.ApplicationMenu.DefaultPane.Items.Add(new RibbonButtonItem { Text = "Recent " + i }); }
        for (int i = 0; i < 5; i++) f.Ribbon.ApplicationMenu.FooterItems.Add(new RibbonButtonItem { Text = "Footer " + i });
        layout = RibbonApplicationMenuLayout.Build(f.Ribbon, f.Ribbon.ApplicationMenu.DefaultPane, new(1200, 1400));
        Assert.All(layout.Targets.Where(t => f.Ribbon.FileMenu.Items.Contains((RibbonItem)t.Element!)), t => Assert.True(t.Bounds.Height >= S(64)));
        Assert.All(layout.Targets.Where(t => f.Ribbon.ApplicationMenu.DefaultPane.Items.Contains((RibbonItem)t.Element!)), t => Assert.True(t.Bounds.Height >= S(72)));
        Assert.All(layout.Targets.Where(t => f.Ribbon.ApplicationMenu.FooterItems.Contains((RibbonItem)t.Element!)), t => Assert.True(t.Bounds.Height >= S(52)));
        var footerRows = layout.Targets.Where(t => f.Ribbon.ApplicationMenu.FooterItems.Contains((RibbonItem)t.Element!)).GroupBy(t => t.Bounds.Y).OrderBy(g => g.Key).ToArray();
        foreach (var row in footerRows) { var ordered = row.OrderBy(t => t.Bounds.X).ToArray(); for (int i = 1; i < ordered.Length; i++) Assert.Equal(S(12), ordered[i].Bounds.Left - ordered[i - 1].Bounds.Right); }
        using var form = new Form { Width = 1000, Height = 700 }; form.Controls.Add(f.Ribbon); form.Show(); Application.DoEvents();
        using var surface = new RibbonPopupSurface(f.Ribbon, f.Ribbon.FileMenu, new(680, 320)); form.Controls.Add(surface); surface.CreateControl();
        foreach (var item in new[] { f.Ribbon.FileMenu.Items.Last(), f.Ribbon.ApplicationMenu.DefaultPane.Items.Last(), f.Ribbon.ApplicationMenu.FooterItems.Last() })
        {
            surface.FocusElement(item, enterEditor: false); var target = surface.Targets.First(t => t.Element == item); Assert.False(target.VisibleBounds.IsEmpty);
            var node = surface.AutomationRoot.Node(item); var screen = surface.RectangleToScreen(target.VisibleBounds); var bounds = node.BoundingRectangle;
            Assert.Equal(screen.Top, bounds.Top); Assert.Equal(screen.Height, bounds.Height); Assert.Same(node, surface.AutomationRoot.ElementProviderFromPoint(screen.Left + screen.Width / 2, screen.Top + screen.Height / 2));
        }
        f.Ribbon.Theme = Roomier with { ApplicationMenuPaneHorizontalPadding = 1024 };
        var constrained = RibbonApplicationMenuLayout.Build(f.Ribbon, f.Ribbon.ApplicationMenu.DefaultPane, new(180, 220));
        Assert.InRange(constrained.Size.Width, 1, 180); Assert.InRange(constrained.Size.Height, 1, 220); Assert.True(constrained.Pane.Left >= constrained.Navigation.Right); Assert.True(constrained.Pane.Right <= constrained.Frame.Right);
        form.Close();
    });

    [Fact]
    public void Command_padding_keeps_large_menu_and_qat_paint_independent() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var item = (RibbonToggleItem)f.Group.Items[0];
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var font = new Font("Segoe UI", 14 * dpi / 96f);
            byte[] Paint(RibbonTheme theme)
            {
                using var pixels = new Bitmap(800, 700); using var g = Graphics.FromImage(pixels); g.Clear(Color.White);
                var c = new RibbonPaintContext(g, new(4, 4, 320, 260), theme, font, dpi) { Density = density, Hot = true };
                painter.DrawItem(c, item, RibbonItemSize.Large, false);
                painter.DrawItem(c with { Bounds = new(4, 280, 700, 150) }, item, RibbonItemSize.Medium, true);
                painter.DrawQuickAccessItem(c with { Bounds = new(4, 450, 100, 100) }, item, false);
                using var stream = new MemoryStream(); pixels.Save(stream, System.Drawing.Imaging.ImageFormat.Png); return stream.ToArray();
            }
            Assert.Equal(Paint(RibbonThemes.Office2024Light), Paint(Roomier));
        }
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Application_menu_inert_designer_preview_uses_the_same_override_targets(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        ribbon.Density = density; ribbon.Theme = Roomier;
        var navigation = new RibbonButtonItem { Id = "paired-nav", Text = "Open" };
        var pane = new RibbonButtonItem { Id = "paired-pane", Text = "Recent document" };
        var footer = new RibbonButtonItem { Id = "paired-footer", Text = "Options" };
        ribbon.FileMenu.Items.Add(navigation); ribbon.ApplicationMenu.DefaultPane.Items.Add(pane); ribbon.ApplicationMenu.FooterItems.Add(footer);
        var draft = RibbonKit.WinForms.Design.RibbonDesignCommitter.ReadApplicationMenu(ribbon, host);
        draft.SelectionKey = "default"; draft.PreviewWidth = 1200; draft.PreviewHeight = 1200;
        var layout = RibbonApplicationMenuLayout.Build(ribbon, ribbon.ApplicationMenu.DefaultPane, new(1200, 1200));
        var preview = RibbonKit.WinForms.Design.RibbonDesignCommitter.PreviewApplicationMenu(ribbon, host, draft);
        foreach (var pair in new[] { (0, navigation), (1, pane), (2, footer) })
        {
            var node = draft.Entries[pair.Item1].Children.Single(n => n.Properties.Any(p => p.Name == "Id" && p.Value == pair.Item2.Id));
            var region = preview.Regions.Single(r => r.Key == node.Key); var expected = layout.Targets.Single(t => t.Element == pair.Item2).VisibleBounds;
            Assert.Equal(expected, new Rectangle(region.X, region.Y, region.Width, region.Height));
        }
        var right = preview.Regions.Single(r => r.Key == "default"); Assert.Equal(layout.Pane, new Rectangle(right.X, right.Y, right.Width, right.Height));
        Assert.False(ribbon.FileMenu.IsDropDownOpen); Assert.Empty(ribbon.Controls.OfType<TextBoxBase>()); Assert.Equal(0, host.Transactions);
        if (Environment.GetEnvironmentVariable("RIBBONKIT_PAIRED_CAPTURE") is { Length: > 0 } capture)
            File.WriteAllBytes(Path.Combine(capture, $"application-preview-{density}.png"), Convert.FromBase64String(preview.Png));
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Application_menu_low_overrides_preserve_font_artwork_touch_and_editor_floors(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var editor = new RibbonTextBoxItem { Text = "Name" };
        var theme = Roomier with { ApplicationMenuNavigationMinimumRowHeight = 1, ApplicationMenuPaneMinimumRowHeight = 1, ApplicationMenuFooterMinimumRowHeight = 1,
            ApplicationMenuPaneHorizontalPadding = 0, ApplicationMenuFooterItemGap = 0, EditorMinimumHeight = 54 };
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 24 })
        {
            using var font = new Font("Segoe UI", points * dpi / 96f); int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            foreach (bool navigation in new[] { false, true })
            {
                int row = RibbonApplicationMenuMetrics.RowHeight(f.Navigation, S(300), font, dpi, density, navigation, theme);
                Assert.True(row >= font.Height + S(10)); Assert.True(row >= S(navigation ? 40 : 24));
                if (density == RibbonDensity.Touch) Assert.True(row >= S(44));
                Assert.True(RibbonApplicationMenuMetrics.RowHeight(editor, S(300), font, dpi, density, navigation, theme) >= RibbonEditorLayout.Height(font, dpi, density, theme: theme) + S(2));
            }
            Assert.True(RibbonApplicationMenuMetrics.FooterRowHeight([editor], font, dpi, density, theme) >= RibbonEditorLayout.Height(font, dpi, density, theme: theme) + S(2));
        }
    });

    [Fact]
    public void Application_menu_hover_and_native_content_survive_spacing_apply_reset() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var form = new Form { Width = 1200, Height = 800 }; form.Controls.Add(f.Ribbon);
        var content = new UserControl(); var native = new TextBox { Text = "Retained page", Dock = DockStyle.Top }; content.Controls.Add(native); f.Navigation.Content = content;
        f.Navigation.DropDownHeader = "Open documents"; f.Ribbon.FileMenu.Visible = true;
        int clicks = 0; bool veto = false; native.Validating += (_, e) => e.Cancel = veto;
        ((RibbonButtonItem)f.Navigation.Items[0]).Click += (_, _) => clicks++; form.Show(); Application.DoEvents(); nint handle = 0;
        foreach (var theme in new[] { RibbonThemes.Office2024Light, Roomier, RibbonThemes.Office2024Light })
        {
            f.Ribbon.Theme = theme; Application.DoEvents(); f.Ribbon.FileMenu.ShowDropDown(); var surface = f.Ribbon.Popups.Top!;
            Assert.Same(f.Ribbon.ApplicationMenu.DefaultPane, surface.ApplicationPane);
            var target = surface.Targets.Single(t => t.Element == f.Navigation); var row = target.VisibleBounds;
            typeof(RibbonPopupSurface).GetMethod("OnMouseMove", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(surface, [new MouseEventArgs(MouseButtons.None, 0, row.Left + row.Width / 2, row.Top + row.Height / 2, 0)]);
            Assert.Same(f.Navigation, surface.ApplicationPane); Assert.Equal(0, clicks);
            handle = handle == 0 ? native.Handle : handle; Assert.Equal(handle, native.Handle); Assert.Equal("Retained page", native.Text); Assert.Same(content, f.Navigation.Content);
            var layout = surface.ApplicationLayout!; Assert.Equal(layout.Navigation.Right + 1, layout.Header.Left);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_PAIRED_CAPTURE") is { Length: > 0 } capture)
            {
                using var pixels = new Bitmap(surface.Width, surface.Height); surface.DrawToBitmap(pixels, surface.ClientRectangle);
                pixels.Save(Path.Combine(capture, $"application-native-{surface.DeviceDpi}-{(theme == Roomier ? "roomier" : "default")}.png"));
            }
            native.Focus(); veto = true;
            var plain = surface.Targets.Single(t => t.Element == f.Ribbon.FileMenu.Items[1]).VisibleBounds;
            var move = typeof(RibbonPopupSurface).GetMethod("OnMouseMove", BindingFlags.Instance | BindingFlags.NonPublic)!;
            move.Invoke(surface, [new MouseEventArgs(MouseButtons.None, 0, plain.Left + plain.Width / 2, plain.Top + plain.Height / 2, 0)]);
            Assert.Same(f.Navigation, surface.ApplicationPane);
            veto = false;
            // Hover selection is edge-triggered: leave the vetoed row, then re-enter.
            move.Invoke(surface, [new MouseEventArgs(MouseButtons.None, 0, row.Left + row.Width / 2, row.Top + row.Height / 2, 0)]);
            move.Invoke(surface, [new MouseEventArgs(MouseButtons.None, 0, plain.Left + plain.Width / 2, plain.Top + plain.Height / 2, 0)]);
            Assert.Same(f.Ribbon.ApplicationMenu.DefaultPane, surface.ApplicationPane); Assert.Equal(handle, native.Handle);
            f.Ribbon.FileMenu.CloseDropDown(); Application.DoEvents();
        }
        form.Close(); content.Dispose();
    });

    private sealed class Record { internal readonly Dictionary<string, Rectangle> Text = []; }
    private sealed class Flat(Record record) : RibbonRenderer
    { public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text[text] = c.Bounds; base.DrawText(c, text, color, flags); } }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    { public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text[text] = c.Bounds; base.DrawText(c, text, color, flags); } }

    private sealed class Fixture : IDisposable
    {
        internal RibbonControl Ribbon { get; } = new() { Width = 1600, AnimationLevel = RibbonAnimationLevel.None };
        internal RibbonGroup Group { get; } = new() { CanResize = false };
        internal RibbonDropDownItem Navigation { get; } = new() { Text = "Open", MenuDescription = "Select a format and location.", Icon = RibbonStockIcons.Create("open") };
        internal RibbonToggleItem PaneChoice { get; } = new() { Text = "Recent document", MenuDescription = "A retained recent document description.", Icon = RibbonStockIcons.Create("open"), Checked = true };
        internal RibbonButtonItem Footer { get; } = new() { Text = "Options", Icon = RibbonStockIcons.Create("open") };
        internal Fixture()
        {
            Group.Items.AddRange(new RibbonToggleItem { Text = "Bold", Icon = RibbonStockIcons.Create("bold"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Small) },
                new RibbonButtonItem { Text = "Open", Icon = RibbonStockIcons.Create("open"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) },
                new RibbonSplitButtonItem { Text = "Paste", Icon = RibbonStockIcons.Create("open"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) },
                new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true });
            var tab = new RibbonTab(); tab.Groups.Add(Group); Ribbon.Tabs.Add(tab);
            Navigation.Items.Add(new RibbonButtonItem { Text = "Document" }); Ribbon.FileMenu.Items.AddRange(Navigation, new RibbonButtonItem { Text = "Save", Icon = RibbonStockIcons.Create("open") });
            Ribbon.ApplicationMenu.DefaultPane.MenuPresentation = RibbonMenuPresentation.IllustratedChoices;
            Ribbon.ApplicationMenu.DefaultPane.DropDownHeader = "Recent documents"; Ribbon.ApplicationMenu.DefaultPane.Items.Add(PaneChoice);
            Ribbon.ApplicationMenu.FooterItems.Add(Footer);
        }
        public void Dispose() => Ribbon.Dispose();
    }
}
