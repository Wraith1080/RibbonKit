using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class TouchGeometryCorrectionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Small_touch_icons_center_in_the_command_area_without_moving_arrows_or_menu_gutters(bool office) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonControl { Density = RibbonDensity.Touch };
        var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); host.Tabs.Add(tab);
        RibbonItem[] items = [new RibbonToggleItem { Text = "Bold", Checked = true, Icon = new RibbonSvgIcon("stock:bold", true) },
            new RibbonToggleItem { Text = "Italic", Icon = new RibbonSvgIcon("stock:italic", true) },
            new RibbonToggleItem { Text = "Underline", Checked = true, Icon = new RibbonSvgIcon("stock:underline", true) },
            new RibbonDropDownItem { Text = "More", Icon = new RibbonSvgIcon("stock:bold", true) },
            new RibbonSplitButtonItem { Text = "Split", Icon = new RibbonSvgIcon("stock:bold", true) }];
        group.Items.AddRange(items);
        var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Neutral(record);
        var failures = new List<string>();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (bool pressed in new[] { false, true })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            using var bitmap = new Bitmap(S(390), S(65)); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(host.Theme.Surface);
            int x = S(8);
            foreach (var item in items)
            {
                int width = item is RibbonSplitButtonItem ? 100 : item is RibbonDropDownItem ? 72 : 44;
                var bounds = new Rectangle(x, S(8), S(width), S(45));
                var context = new RibbonPaintContext(graphics, bounds, host.Theme, host.Font, dpi, true, pressed, false) { Density = host.Density };
                var primary = bounds; record.Arrow = Rectangle.Empty;
                if (item is RibbonSplitButtonItem split)
                {
                    var parts = RibbonSplitLayout.Calculate(bounds, split, RibbonItemSize.Small, false, dpi, host.Font); primary = parts.Primary;
                    painter.DrawSplitItem(context with { Bounds = parts.Primary }, context with { Bounds = parts.DropDown }, split, RibbonItemSize.Small, false);
                    Assert.Equal(parts.DropDown, record.Arrow);
                }
                else
                {
                    painter.DrawItem(context, item, RibbonItemSize.Small, false);
                    if (item is RibbonDropDownItem) primary.Width -= S(28);
                }
                if (Math.Abs(record.Image.Left + record.Image.Width / 2d - (primary.Left + primary.Width / 2d)) > .5)
                    failures.Add($"{item.Text}, {dpi} DPI, pressed={pressed}: {record.Image} within {primary}");
                Assert.Equal(new Size(S(16), S(16)), record.Image.Size);
                Assert.InRange(Math.Abs(record.Image.Top + record.Image.Height / 2d - (primary.Top + primary.Height / 2d)), 0, .5);
                if (!record.Arrow.IsEmpty) Assert.False(record.Image.IntersectsWith(record.Arrow));
                x = bounds.Right + S(8);
            }
            Capture(bitmap, $"small-touch-{(office ? "office" : "neutral")}-{dpi}-{(pressed ? "pressed" : "normal")}");
            var compact = RibbonContentLayout.Calculate(new(0, 0, S(28), S(26)), RibbonItemSize.Small, false, true, dpi, host.Font);
            Assert.Equal(S(6), compact.Icon.Left);
            var menu = RibbonContentLayout.Calculate(new(0, 0, S(200), S(44)), RibbonItemSize.Small, true, true, dpi, host.Font, host.Density);
            Assert.Equal(S(6), menu.Icon.Left);
        }
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    });

    [Theory]
    [InlineData(RibbonGroupLayout.Columns)]
    [InlineData(RibbonGroupLayout.TwoRows)]
    [InlineData(RibbonGroupLayout.ThreeRows)]
    public void Touch_fields_scale_proportionally_match_widths_and_honor_designer_InputWidth(RibbonGroupLayout layout) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonControl { Width = 1400 };
        var tab = new RibbonTab(); var group = new RibbonGroup { Layout = layout, CanResize = false }; tab.Groups.Add(group); host.Tabs.Add(tab);
        var combo = new RibbonComboBoxItem { Text = "Font", ShowLabel = false, InputWidth = 50, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var text = new RibbonTextBoxItem { Text = "Name", InputWidth = 50 }; group.Items.AddRange(combo, text);
        foreach (int points in new[] { 9, 14 })
        {
            using var font = new Font("Segoe UI", points); host.Font = font;
            foreach (int dpi in new[] { 96, 104, 120, 144, 168, 192 })
            foreach (int authoredWidth in new[] { 50, 100, 120 })
            {
                int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
                combo.InputWidth = text.InputWidth = authoredWidth;
                host.Density = RibbonDensity.Compact;
                var compact = Fields();
                Assert.InRange(compact.Combo.Width, S(authoredWidth) - 1, S(authoredWidth));
                host.Density = RibbonDensity.Touch;
                var touch = Fields();
                int compactHeight = font.Height + S(8), touchHeight = Math.Max(S(42), compactHeight);
                int expectedWidth = Math.Max((int)Math.Ceiling(S(authoredWidth) * (double)touchHeight / compactHeight), S(authoredWidth) + S(32) - S(24));
                Assert.Equal(expectedWidth, touch.Combo.Width);
                Assert.Equal(touch.Combo.Width, touch.Text.Width);
                Assert.True(RibbonEditorLayout.ComboText(touch.Combo, dpi, host.Density).Width >= RibbonEditorLayout.ComboText(compact.Combo, dpi).Width);
                Assert.Equal(authoredWidth, combo.InputWidth); Assert.Equal(authoredWidth, text.InputWidth);
                host.Density = RibbonDensity.Compact; Assert.Equal(compact, Fields());

                (Rectangle Combo, Rectangle Text) Fields()
                {
                    var measured = host.Engine.Measure(group, 0, host.Font, dpi, host.LayoutTheme);
                    Rectangle Field(RibbonItem item)
                    {
                        var entry = measured.Items.Single(i => i.Item == item);
                        return RibbonEditorLayout.Field(entry.Bounds, item, entry.LabelWidth, host.Font, dpi);
                    }
                    return (Field(combo), Field(text));
                }
            }
            host.LayoutMode = RibbonLayoutMode.Simplified;
            var compactTarget = host.Snapshot.Targets.Single(t => t.Element == combo);
            host.Density = RibbonDensity.Touch;
            var touchTarget = host.Snapshot.Targets.Single(t => t.Element == combo);
            var touchField = RibbonEditorLayout.Field(touchTarget.Bounds, combo, touchTarget.LabelWidth, font, host.DeviceDpi, simplified: true);
            int naturalHeight = font.Height + RibbonLayoutEngine.Scale(8, host.DeviceDpi);
            int compactMinimum = Math.Max(RibbonLayoutEngine.Scale(26, host.DeviceDpi), naturalHeight);
            int touchMinimum = Math.Max(RibbonLayoutEngine.Scale(44, host.DeviceDpi), naturalHeight);
            int inputWidth = RibbonLayoutEngine.Scale(combo.InputWidth, host.DeviceDpi);
            int expected = Math.Max((int)Math.Ceiling(inputWidth * (double)touchMinimum / compactMinimum), inputWidth + RibbonLayoutEngine.Scale(32, host.DeviceDpi) - RibbonLayoutEngine.Scale(24, host.DeviceDpi));
            Assert.Equal(expected, touchField.Width);
            Assert.True(touchTarget.Bounds.Width > compactTarget.Bounds.Width);
            host.LayoutMode = RibbonLayoutMode.Classic; host.Density = RibbonDensity.Compact;
        }
    });

    private static void Capture(Bitmap bitmap, string name)
    {
        if (Environment.GetEnvironmentVariable("RIBBONKIT_NATIVE_CAPTURE_DIR") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory); bitmap.Save(Path.Combine(directory, name + ".png"));
    }
    private sealed class Record { internal Rectangle Image, Arrow; }
    private sealed class Neutral(Record record) : RibbonRenderer
    {
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { record.Image = c.Bounds; base.DrawItemImage(c, item, size); }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawDropDownArrow(c, color); }
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { record.Image = c.Bounds; base.DrawItemImage(c, item, size); }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawDropDownArrow(c, color); }
    }
}
