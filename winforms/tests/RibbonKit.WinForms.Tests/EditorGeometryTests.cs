using RibbonKit.WinForms.Layout;
using RibbonKit.WinForms.Design;
using System.ComponentModel;
using System.Reflection;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class EditorGeometryTests
{
    private static RibbonTheme Custom => RibbonThemes.Office2024Light with
    { EditorLabelGap = 18, EditorTextInset = 10, ComboBoxButtonWidth = 48, ComboBoxMinimumChoiceHeight = 60, ComboBoxChoiceVerticalPadding = 14 };

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Default_editor_geometry_and_paint_remain_identical(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            using var ribbon = new RibbonControl { Density = density };
            var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem();
            var input = new RibbonTextBoxItem { Text = "Name", Value = "Document", InputWidth = 120 };
            var combo = new RibbonComboBoxItem { Text = "Font family", InputWidth = 120 };
            var hidden = new RibbonComboBoxItem { Text = "Hidden label", ShowLabel = false, InputWidth = 120 };
            combo.Items.AddRange("Aptos", "Segoe UI"); combo.SelectedIndex = 0;
            menu.Items.AddRange(input, combo, hidden); group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            using var font = new Font("Segoe UI", points * dpi / 96f);
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            using var pixels = new Bitmap(S(660), S(500)); pixels.SetResolution(dpi, dpi);
            using var graphics = Graphics.FromImage(pixels); graphics.Clear(Color.White);
            var data = new List<string>(); int y = S(4);
            int label = menu.Items.Max(i => RibbonEditorLayout.LabelWidth(i, font, dpi));
            foreach (bool menuRow in new[] { false, true })
            foreach (var item in menu.Items)
            {
                var row = new Rectangle(S(4), y, S(650), S(60)); y += row.Height;
                var content = RibbonEditorLayout.ContentBounds(row, item, dpi, density, ribbon.Theme, menuRow);
                var field = RibbonEditorLayout.Field(content, item, label, font, dpi);
                var context = new RibbonPaintContext(graphics, content, ribbon.Theme, font, dpi) { Density = density, Hot = true };
                if (item is RibbonComboBoxItem choice) painter.DrawComboBox(context, choice, field);
                else painter.DrawTextBox(context, input, field);
                var text = item is RibbonComboBoxItem ? RibbonEditorLayout.ComboText(field, dpi, density)
                    : Rectangle.Inflate(field, -S(4), 0);
                painter.DrawText(context with { Bounds = text }, item == input ? input.Value : "Aptos", Color.Black,
                    TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                data.Add($"{menuRow}/{item.Text}: {content}; {field}; {text}; {RibbonEditorLayout.ComboButton(field, dpi, density)}");
            }
            int choiceHeight = RibbonEditorLayout.ComboRowHeight(font, dpi, density);
            data.Add($"label={label};choice={choiceHeight}");
            for (int i = 0; i < 2; i++)
            {
                painter.DrawComboBoxItem(new(graphics, new(S(4), y, S(650), choiceHeight), ribbon.Theme, font, dpi) { Density = density }, combo, i, i == 0);
                y += choiceHeight;
            }
            string name = $"editors-{office}-{density}-{points}-{dpi}";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_EDITOR_GEOMETRY_CAPTURE") is { Length: > 0 } capture)
            {
                Directory.CreateDirectory(capture); pixels.Save(Path.Combine(capture, name + ".png"));
                File.WriteAllLines(Path.Combine(capture, name + ".txt"), data);
            }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_EDITOR_GEOMETRY_BASELINE") is { Length: > 0 } baseline)
            {
                Assert.Equal(File.ReadAllLines(Path.Combine(baseline, name + ".txt")), data);
                using var expected = new Bitmap(Path.Combine(baseline, name + ".png"));
                Assert.Equal(expected.Size, pixels.Size);
                for (int py = 0; py < pixels.Height; py++)
                for (int px = 0; px < pixels.Width; px++) Assert.Equal(expected.GetPixel(px, py), pixels.GetPixel(px, py));
            }
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Overrides_share_label_field_text_arrow_and_choice_geometry_across_renderers(bool office) => FoundationTests.Sta(() =>
    {
        var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            using var ribbon = new RibbonControl { Theme = Custom, Density = density };
            var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem();
            var input = new RibbonTextBoxItem { Text = "Name", InputWidth = 120, FillMenuWidth = true };
            var combo = new RibbonComboBoxItem { Text = "Font family", InputWidth = 120, FillMenuWidth = true };
            var hidden = new RibbonComboBoxItem { Text = "Hidden", ShowLabel = false, InputWidth = 120, FillMenuWidth = true };
            menu.Items.AddRange(input, combo, hidden); group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            using var font = new Font("Segoe UI", points * dpi / 96f);
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            int measured = TextRenderer.MeasureText(combo.Text, font, new Size(10000, 1000), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
            int label = RibbonEditorLayout.LabelWidth(combo, font, dpi);
            Assert.Equal(measured + S(18), label); Assert.Equal(0, RibbonEditorLayout.LabelWidth(hidden, font, dpi));
            using var pixels = new Bitmap(S(620), S(240)); using var graphics = Graphics.FromImage(pixels); graphics.Clear(Color.White);
            int y = S(4);
            foreach (var item in menu.Items)
            {
                var row = new Rectangle(S(4), y, S(610), S(60)); y += row.Height;
                var content = RibbonEditorLayout.ContentBounds(row, item, dpi, density, Custom, true);
                var field = RibbonEditorLayout.Field(content, item, label, font, dpi, menu: true);
                Assert.Equal(row.Right - S(8), field.Right);
                var context = new RibbonPaintContext(graphics, content, Custom, font, dpi) { Density = density, Hot = true };
                if (item is RibbonComboBoxItem choice)
                {
                    painter.DrawComboBox(context, choice, field);
                    Assert.Equal(new Rectangle(field.Right - S(48), field.Top, S(48), field.Height), record.Arrow);
                    var text = RibbonEditorLayout.ComboText(field, dpi, density, Custom);
                    Assert.Equal(field.Left + S(10), text.Left); Assert.Equal(record.Arrow.Left - S(10), text.Right);
                }
                else
                {
                    painter.DrawTextBox(context, input, field);
                    Assert.Equal(field.Width - 2 * S(10), RibbonEditorLayout.TextBounds(field, dpi, Custom).Width);
                }
                if (item != hidden) Assert.Equal(field.Left - S(18), record.Text.Right);
                var fixedField = RibbonEditorLayout.Field(content, item, label, font, dpi);
                Assert.Equal(RibbonEditorLayout.InputWidth((IRibbonEditorItem)item, dpi, density, font), fixedField.Width);
            }
            Assert.Equal(RibbonEditorLayout.InputWidth(input, dpi, density, font), RibbonEditorLayout.InputWidth(combo, dpi, density, font));
            Assert.Equal(Math.Min(255, Math.Max(S(60), font.Height + 2 * S(14))), RibbonEditorLayout.ComboRowHeight(font, dpi, density, Custom));
            if (Environment.GetEnvironmentVariable("RIBBONKIT_EDITOR_GEOMETRY_CAPTURE") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); pixels.Save(Path.Combine(capture, $"override-{office}-{density}-{points}-{dpi}.png")); }
            var tiny = new Rectangle(10, 10, 7, 10);
            Assert.Equal(tiny, RibbonEditorLayout.ComboButton(tiny, dpi, density, Custom));
            var empty = RibbonEditorLayout.ComboText(tiny, dpi, density, Custom); Assert.Equal(0, empty.Width); Assert.InRange(empty.Left, tiny.Left, tiny.Right);
        }
    });

    [Theory]
    [InlineData(nameof(RibbonTheme.EditorLabelGap), false)] [InlineData(nameof(RibbonTheme.EditorTextInset), false)]
    [InlineData(nameof(RibbonTheme.ComboBoxButtonWidth), true)] [InlineData(nameof(RibbonTheme.ComboBoxMinimumChoiceHeight), true)]
    [InlineData(nameof(RibbonTheme.ComboBoxChoiceVerticalPadding), false)]
    public void Invalid_assignments_are_atomic_and_each_role_resets_to_null(string name, bool positive) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl();
        var original = ribbon.Theme; var builtIn = ribbon.BuiltInTheme; var property = typeof(RibbonTheme).GetProperty(name)!;
        foreach (int value in new[] { positive ? 0 : -1, 1025 })
        {
            var invalid = original with { }; property.SetValue(invalid, value);
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = invalid);
            Assert.Same(original, ribbon.Theme); Assert.Equal(builtIn, ribbon.BuiltInTheme);
        }
        foreach (int value in new[] { positive ? 1 : 0, 1024 })
        {
            var valid = original with { }; property.SetValue(valid, value); ribbon.Theme = valid;
            Assert.Equal(value, property.GetValue(ribbon.Theme)); Assert.Same(original, other.Theme);
            var reset = valid with { }; property.SetValue(reset, null); ribbon.Theme = reset; Assert.Equal(original, ribbon.Theme);
        }
        ribbon.Theme = Custom; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; Assert.Equal(original, ribbon.Theme);
        Assert.False(TypeDescriptor.GetProperties(ribbon)[nameof(RibbonControl.Theme)]!.IsBrowsable);
    });

    [Fact]
    public void Explicit_padding_rounds_both_sides_and_native_choice_limits_keep_touch_floors() => FoundationTests.Sta(() =>
    {
        using var font = new Font("Segoe UI", 30);
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var small = Custom with { ComboBoxMinimumChoiceHeight = 1, ComboBoxChoiceVerticalPadding = 0, ComboBoxButtonWidth = 1 };
            Assert.Equal(Math.Min(255, Math.Max(density == RibbonDensity.Touch ? S(44) : S(1), font.Height)), RibbonEditorLayout.ComboRowHeight(font, dpi, density, small));
            Assert.Equal(density == RibbonDensity.Touch ? S(32) : S(1), RibbonEditorLayout.ComboButton(new(0, 0, 1000, 100), dpi, density, small).Width);
            var largest = Custom with { ComboBoxMinimumChoiceHeight = 1024, ComboBoxChoiceVerticalPadding = 1024 };
            int height = RibbonEditorLayout.ComboRowHeight(font, dpi, density, largest);
            Assert.Equal(255, height); using var native = new ListBox { ItemHeight = height }; Assert.Equal(255, native.ItemHeight);
        }
        using var paddingFont = new Font("Segoe UI", 22);
        var explicitPadding = Custom with { ComboBoxMinimumChoiceHeight = 1, ComboBoxChoiceVerticalPadding = 5 };
        Assert.Equal(paddingFont.Height + 2 * RibbonLayoutEngine.Scale(5, 144), RibbonEditorLayout.ComboRowHeight(paddingFont, 144, theme: explicitPadding));
        Assert.Equal(paddingFont.Height + RibbonLayoutEngine.Scale(10, 144), RibbonEditorLayout.ComboRowHeight(paddingFont, 144, theme: explicitPadding with { ComboBoxChoiceVerticalPadding = null }));
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Native_editors_and_inert_preview_follow_overrides_without_replacing_handles(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1100, Height = 650 };
        var record = new Record(); using var painter = new Office(record);
        var ribbon = new RibbonControl { Renderer = painter, Density = density, AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var menu = new RibbonDropDownItem(); var tab = new RibbonTab(); var group = new RibbonGroup();
        var text = new RibbonTextBoxItem { Text = "Name", Value = "Retained", InputWidth = 120, FillMenuWidth = true };
        var combo = new RibbonComboBoxItem { Text = "Font family", InputWidth = 120, FillMenuWidth = true, IsEditable = true };
        combo.Items.AddRange("Aptos", "Segoe UI"); combo.SelectedIndex = 0;
        menu.Items.AddRange(text, combo); group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        form.Show(); Application.DoEvents(); nint[]? handles = null;
        foreach (var theme in new[] { RibbonThemes.Office2024Light, Custom, Custom with { EditorTextInset = 1024, ComboBoxButtonWidth = 1024 }, RibbonThemes.Office2024Light })
        {
            ribbon.Theme = theme; Application.DoEvents();
            var keys = new Dictionary<RibbonElement, string> { [menu] = "menu", [text] = "text", [combo] = "combo" };
            var preview = RibbonPopupPreviewRenderer.Render(ribbon, menu, "menu", "", keys);
            Assert.NotEmpty(preview.Png); Assert.False(menu.IsDropDownOpen);
            Assert.Equal(RibbonEditorLayout.TextBounds(record.Fields[text], ribbon.DeviceDpi, theme), record.Values[text.Value]);
            Assert.Equal(RibbonEditorLayout.ComboText(record.Fields[combo], ribbon.DeviceDpi, density, theme), record.Values[combo.Value]);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_EDITOR_NATIVE_CAPTURE") is { Length: > 0 } capture)
            {
                Directory.CreateDirectory(capture);
                string name = theme == RibbonThemes.Office2024Light ? "default" : theme.EditorTextInset == 1024 ? "clamped" : "override";
                File.WriteAllBytes(Path.Combine(capture, $"preview-{density}-{name}.png"), Convert.FromBase64String(preview.Png));
            }
            menu.ShowDropDown(); var popup = ribbon.Popups.Top; Assert.NotNull(popup);
            var nativeText = ribbon.Editors.EditorFor(text)!; var nativeCombo = ribbon.Editors.EditorFor(combo)!;
            var current = new[] { nativeText.Handle, nativeCombo.Handle }; handles ??= current; Assert.Equal(handles, current);
            foreach (var item in new RibbonItem[] { text, combo })
            {
                var target = popup.Targets.Single(t => ReferenceEquals(t.Element, item));
                var native = item == text ? (Control)nativeText : nativeCombo;
                var content = RibbonEditorLayout.ContentBounds(native.Parent!.ClientRectangle, item, ribbon.DeviceDpi, density, theme, true);
                var field = RibbonEditorLayout.Field(content, item, target.LabelWidth, ribbon.Font, ribbon.DeviceDpi, menu: true);
                if (item == text)
                {
                    var bounds = RibbonEditorLayout.TextBounds(field, ribbon.DeviceDpi, theme);
                    Assert.Equal(bounds.Left, native.Left); Assert.Equal(bounds.Width, native.Width);
                }
                else
                {
                    Assert.Equal(field.Left, native.Left); Assert.Equal(field.Width, native.Width);
                    var clip = RibbonEditorLayout.ComboText(native.ClientRectangle, ribbon.DeviceDpi, density, theme);
                    using var dc = native.CreateGraphics(); Assert.Equal(clip.Left, native.Region!.GetBounds(dc).Left); Assert.Equal(clip.Width, native.Region.GetBounds(dc).Width);
                }
                var screen = popup.RectangleToScreen(target.VisibleBounds); var uiaBounds = popup.AutomationRoot.Node(item).BoundingRectangle;
                Assert.Equal(screen.Left, uiaBounds.Left); Assert.Equal(screen.Top, uiaBounds.Top); Assert.Equal(screen.Width, uiaBounds.Width); Assert.Equal(screen.Height, uiaBounds.Height);
            }
            Assert.Equal("Retained", text.Value); Assert.Equal("Aptos", combo.Value);
            menu.CloseDropDown(); Application.DoEvents();
        }
        ribbon.Theme = Custom; Application.DoEvents();
        using (var choices = new RibbonChoicePopup(ribbon, combo, form.RectangleToScreen(new(20, 200, 280, 42)), _ => { }, () => { }, () => { }))
        {
            var list = (ListBox)typeof(RibbonChoicePopup).GetField("_list", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(choices)!;
            Assert.Equal(RibbonEditorLayout.ComboRowHeight(ribbon.Font, ribbon.DeviceDpi, density, Custom), list.ItemHeight);
            Assert.Equal(list.ItemHeight, list.GetItemRectangle(0).Height); choices.Move(Keys.End); Assert.Equal(1, list.SelectedIndex);
        }
        form.Close();
    });

    [Theory]
    [InlineData(RibbonLayoutMode.Classic, RibbonDensity.Compact)] [InlineData(RibbonLayoutMode.Classic, RibbonDensity.Touch)]
    [InlineData(RibbonLayoutMode.Simplified, RibbonDensity.Compact)] [InlineData(RibbonLayoutMode.Simplified, RibbonDensity.Touch)]
    public void Inline_fields_measure_label_overrides_and_retain_native_handles(RibbonLayoutMode mode, RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1500, Height = 650 };
        var ribbon = new RibbonControl { LayoutMode = mode, Density = density, AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var combo = new RibbonComboBoxItem { Text = "Font family", InputWidth = 120, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        combo.Items.Add("Aptos"); combo.SelectedIndex = 0;
        var text = new RibbonTextBoxItem { Text = "Name", InputWidth = 120, Value = "Retained" };
        var tab = new RibbonTab(); var group = new RibbonGroup { CanResize = false }; group.Items.Add(combo);
        if (mode == RibbonLayoutMode.Classic) group.Items.Add(text);
        tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Show(); Application.DoEvents();
        nint? comboHandle = null, textHandle = null;
        foreach (var theme in new[] { RibbonThemes.Office2024Light, Custom, RibbonThemes.Office2024Light })
        {
            ribbon.Theme = theme; Application.DoEvents(); var native = ribbon.Editors.EditorFor(combo)!;
            comboHandle ??= native.Handle; Assert.Equal(comboHandle.Value, native.Handle);
            var target = ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, combo));
            int label = RibbonEditorLayout.LabelWidth(combo, ribbon.Font, ribbon.DeviceDpi);
            Assert.Equal(label, target.LabelWidth);
            Assert.True(target.Bounds.Width >= label + RibbonEditorLayout.InputWidth(combo, ribbon.DeviceDpi, density, ribbon.Font, simplified: mode == RibbonLayoutMode.Simplified));
            var field = RibbonEditorLayout.Field(native.Parent!.ClientRectangle, combo, label, ribbon.Font, ribbon.DeviceDpi, mode == RibbonLayoutMode.Simplified);
            Assert.Equal(field.Left, native.Left); Assert.Equal(field.Width, native.Width);
            if (mode == RibbonLayoutMode.Classic)
            {
                var nativeText = ribbon.Editors.EditorFor(text)!; textHandle ??= nativeText.Handle; Assert.Equal(textHandle.Value, nativeText.Handle);
                int inset = RibbonLayoutEngine.Scale(theme.EditorTextInset ?? 4, ribbon.DeviceDpi);
                Assert.Equal(field.Width - 2 * inset, nativeText.Width); Assert.Equal("Retained", nativeText.Text);
            }
            Assert.Equal("Aptos", combo.Value); Assert.Equal(120, combo.InputWidth); Assert.Equal(120, text.InputWidth);
        }
        form.Close(); if (mode != RibbonLayoutMode.Classic) text.Dispose();
    });

    private sealed class Record
    {
        internal Rectangle Text, Arrow;
        internal readonly Dictionary<string, Rectangle> Values = [];
        internal readonly Dictionary<RibbonItem, Rectangle> Fields = [];
    }
    private sealed class Flat(Record record) : RibbonRenderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Values[text] = record.Text = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawTextBox(RibbonPaintContext c, RibbonTextBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawTextBox(c, item, field); }
        public override void DrawComboBox(RibbonPaintContext c, RibbonComboBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawComboBox(c, item, field); }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawDropDownArrow(c, color); }
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Values[text] = record.Text = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawTextBox(RibbonPaintContext c, RibbonTextBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawTextBox(c, item, field); }
        public override void DrawComboBox(RibbonPaintContext c, RibbonComboBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawComboBox(c, item, field); }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawDropDownArrow(c, color); }
    }
}
