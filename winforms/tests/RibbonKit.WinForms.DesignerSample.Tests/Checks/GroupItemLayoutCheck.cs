using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Public-API package consumer. No project reference, internal layout access,
// Showcase resources or modification of the user's authored Form is required.
internal static class GroupItemLayoutCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        using var painter = new Recorder();
        using var form = new QuietForm { ClientSize = new(1480, 360), StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false };
        var ribbon = new RibbonControl { Renderer = painter, QuickAccessVisible = false, AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var tab = new RibbonTab { Text = "Group layout examples" }; ribbon.Tabs.Add(tab);
        RibbonGroup Group(string text, RibbonGroupLayout layout = RibbonGroupLayout.Columns)
        {
            var group = new RibbonGroup { Text = text, Layout = layout, CanResize = false }; tab.Groups.Add(group); return group;
        }
        RibbonButtonItem Button(string text, RibbonItemRow row, RibbonItemSize size = RibbonItemSize.Medium) => new()
        { Text = text, Row = row, SizePolicy = RibbonSizePolicy.Fixed(size), Icon = RibbonStockIcons.Create("save") };
        var autoTwo = Group("Automatic two-item stack");
        autoTwo.Items.AddRange(Button("Copy", RibbonItemRow.First), Button("Paste", RibbonItemRow.First));
        var autoThree = Group("Automatic three-item stack");
        autoThree.Items.AddRange(Button("Ruler", RibbonItemRow.First), Button("Gridlines", RibbonItemRow.First), Button("Draft", RibbonItemRow.First));
        var explicitTwo = Group("Explicit two rows", RibbonGroupLayout.TwoRows);
        var name = new RibbonTextBoxItem { Text = "Name", InputWidth = 100, Value = "Document", Row = RibbonItemRow.First };
        var font = new RibbonComboBoxItem { Text = "Font", InputWidth = 100, Row = RibbonItemRow.First }; font.Items.AddRange("Segoe UI", "Arial"); font.SelectedIndex = 0;
        var bold = Button("Bold", RibbonItemRow.Second, RibbonItemSize.Small);
        var italic = Button("Italic", RibbonItemRow.Second, RibbonItemSize.Small);
        var save = Button("Save", RibbonItemRow.Second);
        var separator = new RibbonMenuSeparatorItem();
        var split = new RibbonSplitButtonItem { Text = "Print", Layout = RibbonSplitButtonLayout.Vertical, Icon = RibbonStockIcons.Create("print") };
        split.Items.Add(new RibbonButtonItem { Text = "Preview" }); explicitTwo.Items.AddRange(name, font, bold, italic, save, separator, split);
        var explicitThree = Group("Explicit three rows", RibbonGroupLayout.ThreeRows);
        foreach (var row in Enum.GetValues<RibbonItemRow>())
            explicitThree.Items.AddRange(Button(row + " A", row), Button(row + " B", row, RibbonItemSize.Small));
        form.Show(); Application.DoEvents();
        var editor = Descendants(ribbon).OfType<TextBox>().Single(control => control.Text == "Document");
        var choice = Descendants(ribbon).OfType<ComboBox>().Single(control => control.Text == "Segoe UI");
        var handle = editor.Handle; var choiceHandle = choice.Handle; int compactFieldWidth = 0, compactFieldHeight = 0;
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.Density = density; Application.DoEvents();
            painter.Items.Clear(); painter.Groups.Clear(); painter.Separators.Clear(); painter.Fields.Clear(); painter.Images.Clear();
            using var pixels = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(pixels, ribbon.ClientRectangle);
            pixels.Save(Path.Combine(output, $"group-layout-{density}-{ribbon.DeviceDpi}dpi.png"));
            // Native presenters paint in their own client coordinates; use the
            // public control tree to compare their full slots in ribbon space.
            Rectangle NativeBounds(Control control) => ribbon.RectangleToClient(control.Parent!.RectangleToScreen(control.Parent.ClientRectangle));
            Rectangle B(RibbonItem item) => item == name ? NativeBounds(editor) : item == font ? NativeBounds(choice) : painter.Items[item];
            Check(B(name).Top == B(font).Top && B(font).Left > B(name).Right, "First row flows horizontally");
            Check(B(bold).Top == B(save).Top && B(italic).Left > B(bold).Right && B(save).Left > B(italic).Right, "Second row flows independently");
            Check(B(bold).Top > B(name).Bottom && B(bold).Left == B(name).Left, "Explicit rows share a leading edge");
            Check(B(separator).Left > Math.Max(B(font).Right, B(save).Right) && B(split).Left > B(separator).Right, "Separator spans and separates sections");
            Check(painter.Separators.Count == 1 && painter.Separators[0].Height > painter.Separators[0].Width, "Group separator paints vertically");
            Check(editor.Handle == handle && choice.Handle == choiceHandle && name.Value == "Document" && font.SelectedIndex == 0, "Native editor handles and values retained");
            int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
            // Existing Compact summed padding can clamp one device pixel at
            // fractional DPI. Preserve that reviewed starting geometry.
            if (density == RibbonDensity.Compact)
            {
                compactFieldWidth = painter.Fields[font].Width;
                compactFieldHeight = painter.Fields[font].Height;
                Check(compactFieldWidth >= S(100) - 1 && compactFieldWidth <= S(100), "Compact field retains its authored width and existing rounding");
            }
            int fieldWidth = density == RibbonDensity.Touch
                ? Math.Max((int)Math.Ceiling(S(100) * (double)painter.Fields[font].Height / compactFieldHeight), S(100) + S(32) - S(24)) : compactFieldWidth;
            Check(painter.Fields[name].Width == fieldWidth && painter.Fields[font].Width == fieldWidth,
                $"Matching TextBox and ComboBox widths scale with field height: {density}, {ribbon.DeviceDpi} DPI, expected {fieldWidth}, TextBox {painter.Fields[name]}, ComboBox {painter.Fields[font]}");
            Check(name.InputWidth == 100 && font.InputWidth == 100, "Density preserves authored InputWidth values");
            if (density == RibbonDensity.Touch)
                foreach (var item in new[] { bold, italic })
                {
                    var icon = painter.Images[item]; var slot = B(item);
                    Check(Math.Abs(icon.Left + icon.Width / 2d - (slot.Left + slot.Width / 2d)) <= .5, "Small Touch icon is centered in its actual row slot");
                }
            var rows = explicitThree.Items.GroupBy(item => item.Row).Select(row => row.Select(item => B(item).Top).Distinct().Single()).ToArray();
            Check(rows.Length == 3 && rows[0] < rows[1] && rows[1] < rows[2], "Three explicit rows remain distinct");
            Check(!ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.QuickAccess).Any(entry => entry.Source is RibbonMenuSeparatorItem), "Separator stays outside command catalog");
            var groupBounds = painter.Groups[autoTwo]; var bounds = autoTwo.Items.Select(B).ToArray();
            Check(bounds[0].Left == bounds[1].Left && bounds[0].Bottom < bounds[1].Top, "Automatic two-item stack preserved");
            Check(bounds[0].Top > groupBounds.Top + 10 * ribbon.DeviceDpi / 96, "Partial automatic stack centers vertically");
        }
        File.WriteAllText(Path.Combine(output, "result.log"), $"PASS: package-only public APIs at {ribbon.DeviceDpi} DPI; automatic two/three-item stacks, explicit two/three rows, vertical separator, Large vertical split, centered Small Touch icons, proportional matching TextBox/ComboBox widths with unchanged InputWidth, native editor handle/value retention, Compact/Touch captures. Actual VS save/reopen and mixed-monitor review remain separate.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static IEnumerable<Control> Descendants(Control owner) => owner.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(Descendants(child)));
    private sealed class QuietForm : Form { protected override bool ShowWithoutActivation => true; }
    private sealed class Recorder : RibbonOffice2024Renderer
    {
        internal Dictionary<RibbonItem, Rectangle> Items { get; } = [];
        internal Dictionary<RibbonItem, Rectangle> Fields { get; } = [];
        internal Dictionary<RibbonItem, Rectangle> Images { get; } = [];
        internal Dictionary<RibbonGroup, Rectangle> Groups { get; } = [];
        internal List<Rectangle> Separators { get; } = [];
        public override void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed) { Groups[group] = context.Bounds; base.DrawGroup(context, group, collapsed); }
        public override void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu) { Items[item] = context.Bounds; base.DrawItem(context, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext dropDown, RibbonSplitButtonItem item, RibbonItemSize size, bool menu)
        { Items[item] = Rectangle.Union(primary.Bounds, dropDown.Bounds); base.DrawSplitItem(primary, dropDown, item, size, menu); }
        public override void DrawTextBox(RibbonPaintContext context, RibbonTextBoxItem item, Rectangle editorBounds) { Items[item] = context.Bounds; Fields[item] = editorBounds; base.DrawTextBox(context, item, editorBounds); }
        public override void DrawComboBox(RibbonPaintContext context, RibbonComboBoxItem item, Rectangle editorBounds) { Items[item] = context.Bounds; Fields[item] = editorBounds; base.DrawComboBox(context, item, editorBounds); }
        protected override void DrawItemImage(RibbonPaintContext context, RibbonItem item, RibbonItemSize size) { Images[item] = context.Bounds; base.DrawItemImage(context, item, size); }
        public override void DrawGroupSeparator(RibbonPaintContext context, RibbonMenuSeparatorItem item) { Separators.Add(context.Bounds); base.DrawGroupSeparator(context, item); }
    }
}
