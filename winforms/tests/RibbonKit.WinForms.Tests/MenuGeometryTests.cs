using System.ComponentModel;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MenuGeometryTests
{
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    private static RibbonTheme Custom => RibbonThemes.Office2024Light with
    { MenuMinimumRowHeight = 70, MenuIconSize = 36, MenuLeadingPadding = 13, MenuTrailingPadding = 17, MenuIconTextGap = 15 };

    [Fact]
    public void Default_menu_measurement_and_paint_remain_identical_to_the_preconsolidation_capture() => FoundationTests.Sta(() =>
    {
        var capture = Environment.GetEnvironmentVariable("RIBBONKIT_GEOMETRY_CAPTURE");
        var baseline = Environment.GetEnvironmentVariable("RIBBONKIT_GEOMETRY_BASELINE");
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (bool office in new[] { true, false })
        foreach (int pointSize in new[] { 9, 14 })
        {
            using var font = new Font("Segoe UI", pointSize * dpi / 96f);
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var menu = new RibbonDropDownItem();
            menu.Items.AddRange(new RibbonButtonItem { Text = "Open document", Icon = RibbonStockIcons.Create("open") },
                new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true },
                new RibbonRadioButtonItem { Text = "Current document", Checked = true },
                new RibbonDropDownItem { Text = "Export", MenuDescription = "Choose a document format for the current selection." },
                new RibbonMenuSeparatorItem(), new RibbonTextBoxItem { Text = "Name", InputWidth = 130 });
            var items = menu.Items.ToArray();
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            int width = S(360), top = S(4);
            var heights = RibbonMenuLayout.RowHeights(items, width - S(8), font, dpi, density);
            var rows = heights.Select(h => { var row = new Rectangle(S(4), top, width - S(8), h); top += h; return row; }).ToArray();
            using var pixels = new Bitmap(width, top + S(4));
            using (var graphics = Graphics.FromImage(pixels))
            {
                var context = new RibbonPaintContext(graphics, new(Point.Empty, pixels.Size), RibbonThemes.Office2024Light, font, dpi) { Density = density };
                renderer.DrawSurface(context, true); renderer.DrawPopupFrame(context);
                for (int i = 0; i < items.Length; i++) renderer.DrawItem(context with { Bounds = rows[i] }, items[i], RibbonItemSize.Medium, true);
            }
            string name = $"{(office ? "office" : "flat")}-{density}-{dpi}-{pointSize}";
            string measurements = string.Join("\n", rows.Select(r => r.ToString()));
            if (capture is not null)
            {
                Directory.CreateDirectory(capture); pixels.Save(Path.Combine(capture, name + ".png"));
                File.WriteAllText(Path.Combine(capture, name + ".txt"), measurements);
            }
            if (baseline is not null)
            {
                Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), measurements);
                using var expected = new Bitmap(Path.Combine(baseline, name + ".png"));
                Assert.Equal(expected.Size, pixels.Size);
                for (int y = 0; y < pixels.Height; y++)
                for (int x = 0; x < pixels.Width; x++) Assert.Equal(expected.GetPixel(x, y), pixels.GetPixel(x, y));
            }
            Assert.Equal(heights[0], heights[1]); Assert.Equal(heights[0], heights[^1]);
            Assert.Equal(S(9), heights[4]);
            var content = RibbonMenuLayout.Content(default, items[0], rows[0], font, dpi, density);
            Assert.Equal(rows[0].Left + S(density == RibbonDensity.Touch ? 52 : 42), content.Text.Left);
            Assert.Equal(S(density == RibbonDensity.Touch ? 32 : 24), content.Icon.Width);
        }
    });

    [Fact]
    public void Overrides_share_measured_icon_text_and_row_slots_across_fonts_densities_and_synthetic_DPI() => FoundationTests.Sta(() =>
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (bool office in new[] { true, false })
        foreach (int points in new[] { 9, 24 })
        {
            using var font = new Font("Segoe UI", points * dpi / 96f);
            var record = new Slots(); using RibbonRenderer renderer = office ? new Office(record) : new Flat(record);
            using var command = new RibbonButtonItem { Text = "Open", Icon = RibbonStockIcons.Create("open") };
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            int height = RibbonMenuLayout.RowHeight(command, S(350), font, dpi, density, theme: Custom);
            Assert.True(height >= S(70)); Assert.True(height >= font.Height + S(10));
            using var pixels = new Bitmap(S(350), height); using var graphics = Graphics.FromImage(pixels);
            var row = new Rectangle(Point.Empty, pixels.Size);
            renderer.DrawItem(new(graphics, row, Custom, font, dpi) { Density = density }, command, RibbonItemSize.Medium, true);
            Assert.Equal(S(13), record.Icon.Left); Assert.Equal(S(36), record.Icon.Width);
            Assert.Equal(S(64), record.Text.Left); Assert.Equal(row.Right - S(81), record.Text.Width);
            Assert.True(row.Contains(record.Icon)); Assert.True(row.Contains(record.Text)); Assert.True(record.Icon.Right < record.Text.Left);
            using var check = new RibbonCheckBoxItem { Text = "Check", Checked = true };
            renderer.DrawItem(new(graphics, row, Custom with { MenuIconSize = 12 }, font, dpi) { Density = density }, check, RibbonItemSize.Medium, true);
            Assert.Equal(S(12), record.Indicator.Width); Assert.True(record.Indicator.Right <= record.Text.Left);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_GEOMETRY_CAPTURE") is { } capture)
            {
                Directory.CreateDirectory(capture);
                pixels.Save(Path.Combine(capture, $"override-{(office ? "office" : "flat")}-{density}-{dpi}-{points}.png"));
            }
        }
    });

    [Fact]
    public void Invalid_metrics_are_atomic_and_null_reset_preserves_per_ribbon_builtin_and_designer_contracts() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = Custom }; using var other = new RibbonControl();
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        var selected = ribbon.Theme;
        foreach (var invalid in new[] { Custom with { MenuMinimumRowHeight = 0 }, Custom with { MenuMinimumRowHeight = 1025 },
            Custom with { MenuIconSize = 0 }, Custom with { MenuIconSize = 1025 }, Custom with { MenuLeadingPadding = -1 },
            Custom with { MenuLeadingPadding = 1025 }, Custom with { MenuTrailingPadding = -1 }, Custom with { MenuTrailingPadding = 1025 },
            Custom with { MenuIconTextGap = -1 }, Custom with { MenuIconTextGap = 1025 } })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = invalid);
            Assert.Same(selected, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2024Light, ribbon.BuiltInTheme);
        }
        ribbon.BuiltInTheme = null; Assert.Equal(Custom, ribbon.Theme); Assert.Null(other.Theme.MenuIconSize);
        Assert.False(TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.Theme)]!.IsBrowsable);
        Assert.Equal(DesignerSerializationVisibility.Hidden, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.Theme)]!.SerializationVisibility);
        ribbon.Theme = ribbon.Theme with { MenuMinimumRowHeight = null, MenuIconSize = null, MenuLeadingPadding = null, MenuTrailingPadding = null, MenuIconTextGap = null };
        Assert.Equal(RibbonThemes.Office2024Light, ribbon.Theme);
        ribbon.Theme = Custom with { MenuMinimumRowHeight = 1, MenuIconSize = 1, MenuLeadingPadding = 0, MenuTrailingPadding = 0, MenuIconTextGap = 0 };
        Assert.True(RibbonMenuMetrics.For(RibbonDensity.Touch, ribbon.Theme).MenuRow >= 44);
    });

    [Fact]
    public void Live_theme_changes_remeasure_popup_hit_targets_and_retained_native_editors_then_reset_exactly() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000, Height = 700 };
        var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem { Text = "Menu" };
        var action = new RibbonButtonItem { Text = "Action" }; var editorItem = new RibbonTextBoxItem { Text = "Name", Value = "Retained", InputWidth = 120 };
        menu.Items.AddRange(action, editorItem); group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        int calls = 0; action.Click += (_, _) => calls++;
        form.Show(); Application.DoEvents(); menu.ShowDropDown();
        var original = ribbon.Popups.Top!; var defaultSize = original.Size; var defaultTargets = original.Targets.Select(t => t.Bounds).ToArray();
        var editor = ribbon.Editors.EditorFor(editorItem)!; nint handle = editor.Handle;
        editor.Text = "Retained text"; editor.Select(2, 4);
        ribbon.Theme = Custom; Assert.True(original.IsDisposed); Assert.False(menu.IsDropDownOpen);
        menu.ShowDropDown(); var popup = ribbon.Popups.Top!;
        Assert.Same(editor, ribbon.Editors.EditorFor(editorItem)); Assert.Equal(handle, editor.Handle); Assert.Equal("Retained text", editor.Text);
        Assert.True(popup.Height > defaultSize.Height); Assert.All(popup.Targets, t => Assert.True(t.Bounds.Height >= RibbonLayoutEngine.Scale(70, ribbon.DeviceDpi)));
        var fieldRow = popup.Targets.Single(t => t.Element == editorItem).Bounds;
        Assert.True(fieldRow.Contains(editor.Parent!.Bounds));
        var target = popup.Targets.Single(t => t.Element == action); var point = new Point(target.Bounds.Right - 3, target.Bounds.Top + target.Bounds.Height / 2);
        var node = popup.AutomationRoot.Node(action); var screen = popup.RectangleToScreen(target.VisibleBounds); var uia = node.BoundingRectangle;
        Assert.Equal(screen.Left, uia.Left); Assert.Equal(screen.Top, uia.Top); Assert.Equal(screen.Width, uia.Width); Assert.Equal(screen.Height, uia.Height);
        Assert.Same(node, popup.AutomationRoot.ElementProviderFromPoint(screen.Left + 2, screen.Top + 2));
        popup.FocusElement(action); Assert.Same(action, popup.FocusedElement);
        nint position = (nint)((point.Y << 16) | point.X); SendMessage(popup.Handle, 0x201, 1, position); SendMessage(popup.Handle, 0x202, 0, position);
        Assert.Equal(1, calls); Assert.False(menu.IsDropDownOpen);
        ribbon.Theme = RibbonThemes.Office2024Light; menu.ShowDropDown();
        Assert.Equal(defaultSize, ribbon.Popups.Top!.Size); Assert.Equal(defaultTargets, ribbon.Popups.Top!.Targets.Select(t => t.Bounds));
        Assert.Equal(handle, editor.Handle); Assert.Same(menu, group.Items[0]);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Override_rows_remain_reachable_in_workarea_constrained_menus_and_overflow(bool overflow) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900, Height = 650 }; var ribbon = new RibbonControl { Theme = Custom, QuickAccessMaxWidth = 28 }; form.Controls.Add(ribbon);
        var menu = new RibbonDropDownItem(); var tab = new RibbonTab(); var group = new RibbonGroup(); group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var commands = Enumerable.Range(0, 20).Select(i => new RibbonButtonItem { Text = "Command " + i }).ToArray();
        if (overflow) ribbon.QuickAccessItems.AddRange(commands); else menu.Items.AddRange(commands);
        var area = new Size(300, 250); using var popup = new RibbonPopupSurface(ribbon, overflow ? ribbon.QuickAccessOverflow : menu, area);
        form.Controls.Add(popup); form.Show(); Application.DoEvents();
        Assert.True(popup.Width <= area.Width); Assert.True(popup.Height <= area.Height);
        popup.FocusElement(commands[^1]); Assert.False(popup.Targets[^1].VisibleBounds.IsEmpty);
        if (overflow) Assert.True(popup.OverflowViewport.Contains(popup.Targets[^1].Bounds));
        popup.FocusElement(commands[0]); Assert.False(popup.Targets[0].VisibleBounds.IsEmpty);
    });

    [Fact]
    public void Application_illustrated_and_choice_list_geometry_remain_component_scoped() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup(); var item = new RibbonButtonItem { Text = "Document" };
        var illustrated = new RibbonDropDownItem { MenuPresentation = RibbonMenuPresentation.IllustratedChoices }; illustrated.Items.Add(item); group.Items.Add(illustrated); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        int application = RibbonApplicationMenuMetrics.RowHeight(item, 300, ribbon.Font, 96, ribbon.Density, true);
        int rich = RibbonMenuLayout.RowHeight(item, 300, ribbon.Font, 96, ribbon.Density);
        int choice = RibbonEditorLayout.ComboRowHeight(ribbon.Font, 96, ribbon.Density);
        ribbon.Theme = Custom;
        Assert.Equal(application, RibbonApplicationMenuMetrics.RowHeight(item, 300, ribbon.Font, 96, ribbon.Density, true));
        Assert.Equal(rich, RibbonMenuLayout.RowHeight(item, 300, ribbon.Font, 96, ribbon.Density, theme: Custom));
        Assert.Equal(choice, RibbonEditorLayout.ComboRowHeight(ribbon.Font, 96, ribbon.Density));
    });

    private sealed class Slots { internal Rectangle Icon, Text, Indicator; }
    [Fact]
    public void Qat_group_rows_share_icon_and_leading_slots_while_retaining_their_chooser_clearance() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var icon = new GroupIcon();
        var group = new RibbonGroup { Text = "Commands", Icon = icon }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var reference = new RibbonQuickAccessGroupReference { Source = group }; ribbon.QuickAccessItems.Add(reference);
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (bool office in new[] { true, false })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var slots = new Slots(); using RibbonRenderer renderer = office ? new Office(slots) : new Flat(slots);
            using var pixels = new Bitmap(S(300), S(90)); using var graphics = Graphics.FromImage(pixels);
            renderer.DrawQuickAccessItem(new(graphics, new(Point.Empty, pixels.Size), Custom, ribbon.Font, dpi) { Density = density }, reference, true);
            Assert.Equal(S(13), icon.Bounds.Left); Assert.Equal(S(36), icon.Bounds.Width); Assert.Equal(S(64), slots.Text.Left);
            Assert.Equal(pixels.Width - S(16), slots.Text.Right);
        }
    });
    private sealed class GroupIcon : IRibbonIcon
    {
        internal Rectangle Bounds;
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { Bounds = bounds; graphics.FillRectangle(Brushes.SteelBlue, bounds); }
    }
    private sealed class Office(Slots r) : RibbonOffice2024Renderer
    {
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem i, RibbonItemSize s) { r.Icon = c.Bounds; base.DrawItemImage(c, i, s); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { r.Text = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawCheckBoxIndicator(RibbonPaintContext c, RibbonCheckBoxItem i) { r.Indicator = c.Bounds; base.DrawCheckBoxIndicator(c, i); }
    }
    private sealed class Flat(Slots r) : RibbonRenderer
    {
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem i, RibbonItemSize s) { r.Icon = c.Bounds; base.DrawItemImage(c, i, s); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { r.Text = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawCheckBoxIndicator(RibbonPaintContext c, RibbonCheckBoxItem i) { r.Indicator = c.Bounds; base.DrawCheckBoxIndicator(c, i); }
    }
}
