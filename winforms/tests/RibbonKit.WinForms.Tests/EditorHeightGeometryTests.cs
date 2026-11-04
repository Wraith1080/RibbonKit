using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class EditorHeightGeometryTests
{
    private static RibbonTheme Roomier => RibbonThemes.Office2024Light with { EditorMinimumHeight = 54, EditorVerticalPadding = 10 };

    [Fact]
    public void Field_rows_grow_at_all_dpis_fonts_and_densities_without_changing_widths() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup();
        var text = new RibbonTextBoxItem { Text = "Name", InputWidth = 120 };
        var combo = new RibbonComboBoxItem { Text = "Font", InputWidth = 120 };
        group.Items.AddRange(text, combo); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var theme in new[] { Roomier, Roomier with { EditorMinimumHeight = null }, Roomier with { EditorVerticalPadding = null },
            Roomier with { EditorMinimumHeight = 1, EditorVerticalPadding = 0 }, Roomier with { EditorMinimumHeight = 1, EditorVerticalPadding = 5 },
            Roomier with { EditorMinimumHeight = 1024, EditorVerticalPadding = 1024 } })
        {
            ribbon.Density = density; using var font = new Font("Segoe UI", points * dpi / 96f);
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            ribbon.Theme = RibbonThemes.Office2024Light;
            int oldWidth = RibbonEditorLayout.InputWidth(text, dpi, density, font);
            int oldSimplifiedWidth = RibbonEditorLayout.InputWidth(combo, dpi, density, font, simplified: true);
            ribbon.Theme = theme;
            int expected = Math.Max(S(Math.Max(theme.EditorMinimumHeight ?? (density == RibbonDensity.Touch ? 42 : 0), density == RibbonDensity.Touch ? 42 : 0)),
                font.Height + (theme.EditorVerticalPadding is { } padding ? 2 * S(padding) : S(8)));
            int rowHeight = RibbonMenuLayout.RowHeight(text, S(500), font, dpi, density, theme: theme);
            Assert.True(rowHeight >= expected + S(2));
            var row = new Rectangle(0, 0, S(500), rowHeight);
            foreach (var item in new RibbonItem[] { text, combo })
            {
                var field = RibbonEditorLayout.Field(row, item, 0, font, dpi);
                Assert.Equal(expected, field.Height); Assert.Equal((rowHeight - expected) / 2, field.Top);
            }
            Assert.Equal(oldWidth, RibbonEditorLayout.InputWidth(text, dpi, density, font));
            Assert.Equal(oldWidth, RibbonEditorLayout.InputWidth(combo, dpi, density, font));
            Assert.Equal(oldSimplifiedWidth, RibbonEditorLayout.InputWidth(combo, dpi, density, font, simplified: true));
            int projected = RibbonMenuLayout.RowHeight(combo, S(500), font, dpi, density, theme: theme, quickAccess: true);
            Assert.Equal(Math.Max(S(density == RibbonDensity.Touch ? 44 : 28), font.Height + S(10)), projected);
            int app = RibbonApplicationMenuMetrics.RowHeight(text, S(500), font, dpi, density, false, theme);
            Assert.True(app >= expected + S(2));
            foreach (var layout in Enum.GetValues<RibbonGroupLayout>())
            {
                group.Layout = layout; ribbon.Engine.Clear();
                var measured = ribbon.Engine.Measure(group, 0, font, dpi, theme with { BodyHeight = 110 });
                Assert.All(measured.Items, item => Assert.True(item.Bounds.Height >= expected));
                Assert.False(measured.Items[0].Bounds.IntersectsWith(measured.Items[1].Bounds));
            }
        }
    });

    [Theory] [InlineData(true)] [InlineData(false)]
    public void Height_roles_validate_atomically_and_reset_independently(bool minimum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var original = ribbon.Theme; var builtIn = ribbon.BuiltInTheme;
        foreach (int invalid in new[] { minimum ? 0 : -1, 1025 })
        {
            var bad = minimum ? original with { EditorMinimumHeight = invalid } : original with { EditorVerticalPadding = invalid };
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = bad); Assert.Same(original, ribbon.Theme); Assert.Equal(builtIn, ribbon.BuiltInTheme);
        }
        ribbon.Theme = Roomier;
        ribbon.Theme = ribbon.Theme with { EditorMinimumHeight = null }; Assert.Equal(10, ribbon.Theme.EditorVerticalPadding);
        ribbon.Theme = ribbon.Theme with { EditorVerticalPadding = null }; Assert.Equal(original, ribbon.Theme);
        ribbon.Theme = Roomier; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; Assert.Equal(original, ribbon.Theme);
    });

    [Theory]
    [InlineData(RibbonLayoutMode.Classic, RibbonDensity.Compact)] [InlineData(RibbonLayoutMode.Classic, RibbonDensity.Touch)]
    [InlineData(RibbonLayoutMode.Simplified, RibbonDensity.Compact)] [InlineData(RibbonLayoutMode.Simplified, RibbonDensity.Touch)]
    public void Inline_rows_and_native_text_grow_and_center_while_handles_values_and_widths_stay(RibbonLayoutMode mode, RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1500, Height = 800 }; var record = new Record(); using var painter = new Office(record);
        var ribbon = new RibbonControl { Density = density, LayoutMode = mode, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup { CanResize = false };
        var plainTab = new RibbonTab { Text = "Commands" }; var plainGroup = new RibbonGroup(); plainGroup.Items.Add(new RibbonButtonItem()); plainTab.Groups.Add(plainGroup);
        var combo = new RibbonComboBoxItem { Text = "Font", InputWidth = 120, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        combo.Items.Add("Aptos"); combo.SelectedIndex = 0; group.Items.Add(combo);
        var text = new RibbonTextBoxItem { Text = "Name", Value = "Retained", InputWidth = 120 };
        if (mode == RibbonLayoutMode.Classic) group.Items.Add(text);
        tab.Groups.Add(group); ribbon.Tabs.AddRange(tab, plainTab); form.Show(); Application.DoEvents();
        int initialHeight = ribbon.Height, width = ribbon.Editors.EditorFor(combo)!.Width; nint comboHandle = ribbon.Editors.EditorFor(combo)!.Handle;
        nint textHandle = mode == RibbonLayoutMode.Classic ? ribbon.Editors.EditorFor(text)!.Handle : 0;
        foreach (var theme in new[] { Roomier, RibbonThemes.Office2024Light })
        {
            ribbon.Theme = theme; Application.DoEvents(); var native = ribbon.Editors.EditorFor(combo)!;
            Assert.Equal(comboHandle, native.Handle); Assert.Equal(width, native.Width);
            Assert.Equal(theme == Roomier, ribbon.Height > initialHeight);
            int fieldTabHeight = ribbon.Height;
            // A QAT group can have been measured while a taller tab was active.
            // Switching tabs must discard that cached command-band height.
            if (mode == RibbonLayoutMode.Classic) _ = ribbon.Engine.Measure(plainGroup, 0, ribbon.Font, ribbon.DeviceDpi, ribbon.ClassicLayoutTheme);
            ribbon.SelectedTab = plainTab; Application.DoEvents(); Assert.Equal(initialHeight, ribbon.Height);
            Assert.All(ribbon.Snapshot.Targets.Where(t => t.Kind == TargetKind.Item), t => Assert.True(ribbon.Snapshot.BodyBounds.Contains(t.Bounds)));
            ribbon.SelectedTab = tab; Application.DoEvents(); Assert.Equal(fieldTabHeight, ribbon.Height);
            using var pixels = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(pixels, ribbon.ClientRectangle);
            foreach (var item in group.Items)
            {
                var target = ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, item));
                var editor = item == combo ? (Control)native : ribbon.Editors.EditorFor(text)!;
                var field = record.Fields[item];
                int wanted = RibbonEditorLayout.Height(ribbon.Font, ribbon.DeviceDpi, density, mode == RibbonLayoutMode.Simplified, theme);
                Assert.Equal(wanted, field.Height); Assert.InRange(Math.Abs(2 * field.Top + field.Height - editor.Parent!.ClientSize.Height), 0, 1);
                Assert.InRange(Math.Abs(2 * editor.Top + editor.Height - (2 * field.Top + field.Height)), 0, 1);
                Assert.True(target.Bounds.Height >= field.Height); Assert.True(ribbon.Snapshot.BodyBounds.Contains(target.Bounds));
                Assert.True(record.LabelFlags[item.Text].HasFlag(TextFormatFlags.VerticalCenter));
                var bounds = ribbon.AutomationRoot.Node(item).BoundingRectangle; var screen = ribbon.RectangleToScreen(target.VisibleBounds);
                Assert.Equal(screen.Top, bounds.Top); Assert.Equal(screen.Height, bounds.Height);
            }
            if (mode == RibbonLayoutMode.Classic) { Assert.Equal(textHandle, ribbon.Editors.EditorFor(text)!.Handle); Assert.Equal("Retained", text.Value); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_EDITOR_HEIGHT_CAPTURE") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); pixels.Save(Path.Combine(capture, $"inline-{mode}-{density}-{theme == Roomier}.png")); }
        }
        Assert.Equal(initialHeight, ribbon.Height); Assert.Equal("Aptos", combo.Value);
        form.Close(); if (mode != RibbonLayoutMode.Classic) text.Dispose();
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Menu_native_fields_and_previews_share_taller_rows_with_fill_and_label_modes(bool office) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1100, Height = 800 }; var record = new Record();
        using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
        var ribbon = new RibbonControl { Renderer = painter, AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var menu = new RibbonDropDownItem(); var group = new RibbonGroup(); var tab = new RibbonTab();
        var text = new RibbonTextBoxItem { Text = "Name", Value = "Retained", InputWidth = 120 };
        var combo = new RibbonComboBoxItem { Text = "Font", InputWidth = 120, ShowLabel = false, IsEditable = true };
        combo.Items.Add("Aptos"); combo.SelectedIndex = 0; menu.Items.AddRange(new RibbonButtonItem { Text = "Action" }, text, combo);
        group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Show(); Application.DoEvents();
        nint[]? handles = null;
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (bool fill in new[] { false, true })
        foreach (var theme in new[] { Roomier, RibbonThemes.Office2024Light })
        {
            ribbon.Density = density; ribbon.Theme = theme; text.FillMenuWidth = combo.FillMenuWidth = fill; Application.DoEvents();
            var keys = menu.Items.ToDictionary(i => (RibbonElement)i, i => i.Text); keys[menu] = "menu";
            var preview = RibbonPopupPreviewRenderer.Render(ribbon, menu, "menu", "", keys);
            Assert.Equal(record.Fields[text].Height, record.Fields[combo].Height); int expected = record.Fields[text].Height;
            menu.ShowDropDown(); var popup = ribbon.Popups.Top!;
            Control[] editors = [ribbon.Editors.EditorFor(text)!, ribbon.Editors.EditorFor(combo)!];
            handles ??= editors.Select(e => e.Handle).ToArray(); Assert.Equal(handles, editors.Select(e => e.Handle));
            foreach (var item in new RibbonItem[] { text, combo })
            {
                var target = popup.Targets.Single(t => ReferenceEquals(t.Element, item)); var editor = item == text ? editors[0] : editors[1];
                var content = RibbonEditorLayout.ContentBounds(editor.Parent!.ClientRectangle, item, ribbon.DeviceDpi, density, theme, true);
                var field = RibbonEditorLayout.Field(content, item, target.LabelWidth, ribbon.Font, ribbon.DeviceDpi, menu: true);
                Assert.Equal(expected, field.Height); Assert.True(target.Bounds.Height >= field.Height);
                Assert.InRange(Math.Abs(2 * editor.Top + editor.Height - (2 * field.Top + field.Height)), 0, 1);
                var node = popup.AutomationRoot.Node(item).BoundingRectangle; var screen = popup.RectangleToScreen(target.VisibleBounds);
                Assert.Equal(screen.Top, node.Top); Assert.Equal(screen.Height, node.Height);
            }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_EDITOR_HEIGHT_CAPTURE") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); File.WriteAllBytes(Path.Combine(capture, $"menu-{office}-{density}-{fill}-{theme == Roomier}.png"), Convert.FromBase64String(preview.Png)); }
            Assert.Equal("Retained", text.Value); Assert.Equal("Aptos", combo.Value); menu.CloseDropDown(); Application.DoEvents();
        }
        form.Close();
    });

    [Fact]
    public void Menu_only_editors_and_small_projections_do_not_enlarge_the_ribbon_body() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 1200 }; var tab = new RibbonTab(); var group = new RibbonGroup();
        var menu = new RibbonDropDownItem(); menu.Items.Add(new RibbonTextBoxItem());
        var small = new RibbonComboBoxItem { SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Small), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedSize = RibbonItemSize.Small };
        group.Items.AddRange(menu, small); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        {
            ribbon.LayoutMode = mode; ribbon.Theme = RibbonThemes.Office2024Light; int original = ribbon.Height;
            ribbon.Theme = Roomier; Assert.Equal(original, ribbon.Height);
        }
    });

    [Fact]
    public void Borrowed_group_and_application_menu_rows_fit_taller_fields_with_work_area_clipping() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 1200, Theme = Roomier };
        var main = new RibbonTab(); var commands = new RibbonGroup(); commands.Items.Add(new RibbonButtonItem()); main.Groups.Add(commands);
        var other = new RibbonTab(); var fields = new RibbonGroup { CanResize = false };
        fields.Items.AddRange(new RibbonTextBoxItem(), new RibbonComboBoxItem(), new RibbonTextBoxItem()); other.Groups.Add(fields);
        ribbon.Tabs.AddRange(main, other);
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.Density = density;
            using var groupPopup = new RibbonPopupSurface(ribbon, fields, new(1000, 500));
            int height = RibbonEditorLayout.Height(ribbon.Font, ribbon.DeviceDpi, density, theme: Roomier);
            Assert.All(groupPopup.Targets, t => Assert.True(t.Bounds.Height >= height));
            Assert.True(groupPopup.Height >= groupPopup.Targets.Max(t => t.Bounds.Bottom));
            Assert.All(groupPopup.Targets, t => Assert.DoesNotContain(groupPopup.Targets, otherTarget => otherTarget != t && t.Bounds.IntersectsWith(otherTarget.Bounds)));
            if (ribbon.ApplicationMenu.DefaultPane.Items.Count == 0)
            {
                ribbon.ApplicationMenu.DefaultPane.Items.AddRange(Enumerable.Range(0, 10).Select(_ => new RibbonTextBoxItem()).ToArray());
                ribbon.ApplicationMenu.FooterItems.Add(new RibbonComboBoxItem());
            }
            var app = RibbonApplicationMenuLayout.Build(ribbon, ribbon.ApplicationMenu.DefaultPane, new(700, 240));
            Assert.InRange(app.Size.Height, 1, 240); Assert.True(app.PaneExtent > app.Pane.Height);
            Assert.All(app.Targets.Where(t => t.Element is IRibbonEditorItem), t => Assert.True(t.Bounds.Height >= height));
        }
    });

    [Fact]
    public void Tall_menu_rows_scroll_reveal_and_retain_native_editors() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000, Height = 650 };
        var ribbon = new RibbonControl { Theme = Roomier, AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem();
        var fields = Enumerable.Range(0, 30).Select(i => new RibbonTextBoxItem { Text = "Value " + i, Value = "Retained " + i }).ToArray();
        menu.Items.AddRange(fields); group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        form.Show(); Application.DoEvents();
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.Density = density; Application.DoEvents(); menu.ShowDropDown(); var popup = ribbon.Popups.Top!;
            popup.FocusElement(fields[^1], enterEditor: false); var last = popup.Targets.Single(t => t.Element == fields[^1]);
            Assert.Equal(last.Bounds, last.VisibleBounds); var native = ribbon.Editors.EditorFor(fields[^1])!; nint handle = native.Handle;
            var bounds = popup.AutomationRoot.Node(fields[^1]).BoundingRectangle; var screen = popup.RectangleToScreen(last.VisibleBounds);
            Assert.Equal(screen.Top, bounds.Top); Assert.Equal(screen.Height, bounds.Height);
            popup.FocusElement(fields[0], enterEditor: false); Assert.Equal(handle, ribbon.Editors.EditorFor(fields[^1])!.Handle);
            popup.FocusElement(fields[^1], enterEditor: false); Assert.Equal(handle, ribbon.Editors.EditorFor(fields[^1])!.Handle);
            Assert.Equal("Retained 29", native.Text); menu.CloseDropDown(); Application.DoEvents();
        }
        form.Close();
    });

    private sealed class Record
    {
        internal readonly Dictionary<RibbonItem, Rectangle> Fields = [];
        internal readonly Dictionary<string, TextFormatFlags> LabelFlags = [];
    }
    private sealed class Flat(Record record) : RibbonRenderer
    {
        public override void DrawTextBox(RibbonPaintContext c, RibbonTextBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawTextBox(c, item, field); }
        public override void DrawComboBox(RibbonPaintContext c, RibbonComboBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawComboBox(c, item, field); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.LabelFlags[text] = flags; base.DrawText(c, text, color, flags); }
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        public override void DrawTextBox(RibbonPaintContext c, RibbonTextBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawTextBox(c, item, field); }
        public override void DrawComboBox(RibbonPaintContext c, RibbonComboBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawComboBox(c, item, field); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.LabelFlags[text] = flags; base.DrawText(c, text, color, flags); }
    }
}
