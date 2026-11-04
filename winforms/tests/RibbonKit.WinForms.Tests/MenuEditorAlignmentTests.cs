using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MenuEditorAlignmentTests
{
    private static RibbonTheme Wider => RibbonThemes.Office2024Light with
    { MenuIconSize = 36, MenuLeadingPadding = 14, MenuIconTextGap = 16, MenuTrailingPadding = 18, MenuMinimumWidth = 520 };

    [Fact]
    public void Live_native_presenters_share_menu_columns_and_keep_handles_values_and_authored_widths() => FoundationTests.Sta(() =>
    {
        var record = new Record(); using var painter = new Office(record); using var form = new Form { Width = 1100, Height = 700 };
        var ribbon = new RibbonControl { Renderer = painter, AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem(); var command = new RibbonButtonItem { Text = "Action" };
        var name = new RibbonTextBoxItem { Text = "Name", Value = "Retained", InputWidth = 80 };
        var combo = new RibbonComboBoxItem { Text = "Font family", InputWidth = 80 }; combo.Items.Add("Aptos"); combo.SelectedIndex = 0;
        var hidden = new RibbonComboBoxItem { Text = "Hidden command", ShowLabel = false, InputWidth = 80 }; hidden.Items.Add("Calibri"); hidden.SelectedIndex = 0;
        menu.Items.AddRange(command, name, combo, hidden); group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        form.Show(); Application.DoEvents(); nint[]? handles = null;
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var theme in new[] { RibbonThemes.Office2024Light, Wider })
        {
            ribbon.Density = density; ribbon.Theme = theme; Application.DoEvents(); menu.ShowDropDown();
            // Check synchronous native layout before external activation messages;
            // focus/input acceptance is a separate fixture/user review scope.
            var popup = ribbon.Popups.Top; Assert.NotNull(popup);
            Control[] natives = [ribbon.Editors.EditorFor(name)!, ribbon.Editors.EditorFor(combo)!, ribbon.Editors.EditorFor(hidden)!];
            handles ??= natives.Select(c => c.Handle).ToArray(); Assert.Equal(handles, natives.Select(c => c.Handle));
            record.Text.Clear(); record.Fields.Clear(); using var bitmap = new Bitmap(popup.Width, popup.Height); popup.DrawToBitmap(bitmap, popup.ClientRectangle);
            int column = popup.RectangleToScreen(record.Text[command.Text]).Left;
            Assert.Equal(column, natives[0].Parent!.RectangleToScreen(record.Text[name.Text]).Left);
            Assert.Equal(column, natives[1].Parent!.RectangleToScreen(record.Text[combo.Text]).Left);
            Assert.Equal(column, natives[2].Parent!.RectangleToScreen(record.Fields[hidden]).Left);
            Assert.Equal(natives[0].Parent!.RectangleToScreen(record.Fields[name]).Left, natives[1].Parent!.RectangleToScreen(record.Fields[combo]).Left);
            Assert.Equal("Retained", name.Value); Assert.Equal("Aptos", combo.Value); Assert.Equal("Calibri", hidden.Value);
            Assert.All(new IRibbonEditorItem[] { name, combo, hidden }, item => Assert.Equal(80, item.InputWidth));
            if (Environment.GetEnvironmentVariable("RIBBONKIT_MENU_EDITOR_CAPTURE") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); bitmap.Save(Path.Combine(capture, $"native-{density}-{theme.MenuIconSize.HasValue}-{ribbon.DeviceDpi}dpi.png")); }
            menu.CloseDropDown(); Application.DoEvents();
        }
    });

    [Fact]
    public void Menu_measurement_reserves_gutters_labels_and_exact_authored_field_width_at_fractional_dpi() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem();
        var name = new RibbonTextBoxItem { Text = "Name", InputWidth = 80 };
        var combo = new RibbonComboBoxItem { Text = "Font family", InputWidth = 80 };
        var hidden = new RibbonComboBoxItem { Text = new string('W', 80), ShowLabel = false, InputWidth = 80 };
        menu.Items.AddRange(name, combo, hidden); group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var theme in new[] { RibbonThemes.Office2024Light, Wider, Wider with { MenuLeadingPadding = 0, MenuIconSize = 1, MenuIconTextGap = 0, MenuTrailingPadding = 0 } })
        {
            ribbon.Density = density; using var font = new Font("Segoe UI", points * dpi / 96f);
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            int labelWidth = menu.Items.Select(i => RibbonEditorLayout.LabelWidth(i, font, dpi)).Max();
            int column = S(RibbonMenuMetrics.For(density, theme).MenuTextInset);
            foreach (var item in menu.Items)
            {
                int width = RibbonEditorLayout.MenuWidth(item, labelWidth, font, dpi, density, theme);
                var row = new Rectangle(S(4), 0, width - S(8), S(80));
                var content = RibbonEditorLayout.ContentBounds(row, item, dpi, density, theme, true);
                var field = RibbonEditorLayout.Field(content, item, labelWidth, font, dpi);
                Assert.Equal(RibbonEditorLayout.InputWidth((IRibbonEditorItem)item, dpi, density, font), field.Width);
                Assert.Equal(row.Left + column, item == hidden ? field.Left : content.Left);
                Assert.True(row.Right - field.Right >= S(theme.MenuTrailingPadding ?? 8));
                Assert.Equal(row, RibbonEditorLayout.ContentBounds(row, item, dpi, density, theme, false));
            }
        }
        ribbon.Theme = Wider with { MenuMinimumWidth = 1 }; menu.Items.Remove(name); menu.Items.Remove(combo);
        using var onlyHidden = new RibbonPopupSurface(ribbon, menu, new(2000, 2000));
        Assert.Equal(RibbonEditorLayout.MenuWidth(hidden, 0, ribbon.Font, ribbon.DeviceDpi, ribbon.Density, ribbon.Theme), onlyHidden.Width);
        name.Dispose(); combo.Dispose();
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Menu_textbox_and_combo_labels_align_with_command_text_and_hidden_combo_reserves_no_label_space(bool office) => FoundationTests.Sta(() =>
    {
        var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
        var errors = new List<string>();
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            using var ribbon = new RibbonControl { Theme = Wider, Density = density, Renderer = painter };
            var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem();
            var command = new RibbonButtonItem { Text = "Action", Icon = RibbonStockIcons.Create("open") };
            var name = new RibbonTextBoxItem { Text = "Name", InputWidth = 80, Value = "Document" };
            var combo = new RibbonComboBoxItem { Text = "Font family", InputWidth = 80 }; combo.Items.Add("Aptos"); combo.SelectedIndex = 0;
            var hidden = new RibbonComboBoxItem { Text = "Hidden combo command name", ShowLabel = false, InputWidth = 80 }; hidden.Items.Add("Calibri"); hidden.SelectedIndex = 0;
            menu.Items.AddRange(command, name, combo, hidden); group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            var keys = menu.Items.ToDictionary(i => (RibbonElement)i, i => i.Text); keys[menu] = "menu";
            var preview = RibbonPopupPreviewRenderer.Render(ribbon, menu, "menu", "", keys);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_MENU_EDITOR_CAPTURE") is { Length: > 0 } capture)
            { Directory.CreateDirectory(capture); File.WriteAllBytes(Path.Combine(capture, $"menu-editors-{office}-{density}.png"), Convert.FromBase64String(preview.Png)); }
            int column = record.Text[command.Text].Left;
            Check(record.Text[name.Text].Left == column, $"{density}: TextBox label {record.Text[name.Text].Left}, command {column}");
            Check(record.Text[combo.Text].Left == column, $"{density}: ComboBox label {record.Text[combo.Text].Left}, command {column}");
            Check(record.Fields[name].Left == record.Fields[combo].Left, $"{density}: Visible labels share a field column");
            Check(record.Fields[hidden].Left == column, $"{density}: Hidden-label ComboBox starts at command column ({record.Fields[hidden].Left} vs {column})");
            Check(!record.Text.ContainsKey(hidden.Text), "Hidden label was not painted");
            foreach (var item in new RibbonItem[] { name, combo, hidden })
            {
                Check(record.Fields[item].Width == RibbonEditorLayout.InputWidth((IRibbonEditorItem)item, ribbon.DeviceDpi, density, ribbon.Font), $"{density}: Authored width is retained for {item.Text}");
                if (item is RibbonTextBoxItem text) Assert.Null(ribbon.Editors.EditorFor(text));
                else Assert.Null(ribbon.Editors.EditorFor((RibbonComboBoxItem)item));
            }
            Assert.Equal("Hidden combo command name", hidden.Text); Assert.Equal("Document", name.Value); Assert.False(menu.IsDropDownOpen);
        }
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
        void Check(bool value, string error) { if (!value) errors.Add(error); }
    });

    private sealed class Record
    {
        internal readonly Dictionary<string, Rectangle> Text = [];
        internal readonly Dictionary<RibbonItem, Rectangle> Fields = [];
    }
    private sealed class Flat(Record record) : RibbonRenderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text[text] = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawTextBox(RibbonPaintContext c, RibbonTextBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawTextBox(c, item, field); }
        public override void DrawComboBox(RibbonPaintContext c, RibbonComboBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawComboBox(c, item, field); }
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text[text] = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawTextBox(RibbonPaintContext c, RibbonTextBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawTextBox(c, item, field); }
        public override void DrawComboBox(RibbonPaintContext c, RibbonComboBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawComboBox(c, item, field); }
    }
}
