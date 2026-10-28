using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MenuTests
{
    [Fact]
    public void Menu_metadata_and_separator_ownership_preserve_authored_state() => FoundationTests.Sta(() =>
    {
        using var menu = new RibbonDropDownItem(); using var group = new RibbonGroup();
        using var separator = new RibbonMenuSeparatorItem { Text = "Ignored", KeyTip = "S" };
        separator.PerformClick(); Assert.Null(separator.Parent);
        Assert.Throws<ArgumentException>(() => group.Items.Add(separator)); Assert.Empty(group.Items);
        var command = new RibbonButtonItem(); group.Items.Add(command);
        Assert.Throws<ArgumentException>(() => group.Items[0] = separator); Assert.Same(command, group.Items[0]);
        menu.Items.Add(separator); Assert.Same(menu, separator.Parent);
        Assert.False(new RibbonTarget(TargetKind.Item, separator, new(0, 0, 200, 10), Rectangle.Empty, RibbonItemSize.Medium).Enabled);
        var property = TypeDescriptor.GetProperties(command)[nameof(command.MenuDescription)]!;
        Assert.True(property.IsLocalizable); Assert.Equal("", property.GetValue(command));
        property.SetValue(command, "Inline description"); Assert.Equal("Inline description", command.MenuDescription);
        Assert.Equal("", command.ScreenTipDescription); property.SetValue(command, null); Assert.Equal("", command.MenuDescription);
        var editor = (EditorAttribute)TypeDescriptor.GetProperties(menu)[nameof(menu.Items)]!.Attributes[typeof(EditorAttribute)]!;
        Assert.Contains("MenuItemCollectionEditor", editor.EditorTypeName);
        separator.Dispose(); Assert.Empty(menu.Items); Assert.Single(group.Items);
    });

    [Fact]
    public void Rich_rows_share_text_hooks_and_keep_descriptions_separators_and_editors_in_their_slots() => FoundationTests.Sta(() =>
    {
        using var painter = new Recorder();
        RibbonItem[] items = [new RibbonButtonItem(), new RibbonDropDownItem(), new RibbonSplitButtonItem(),
            new RibbonCheckBoxItem(), new RibbonRadioButtonItem()];
        const string description = "Apply the chosen format to the current document. This longer explanation remains available in full in the model and is limited to two displayed lines.";
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 18 })
        {
            using var font = new Font("Segoe UI", points);
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            int width = S(280);
            foreach (var item in items)
            foreach (bool enabled in new[] { true, false })
            {
                item.Text = "Keep formatting"; item.MenuDescription = description; item.Enabled = enabled;
                int height = RibbonMenuLayout.RowHeight(item, width, font, dpi);
                var row = new Rectangle(0, 0, width, height);
                using var image = new Bitmap(width, height); using var graphics = Graphics.FromImage(image);
                var context = new RibbonPaintContext(graphics, row, RibbonThemes.Office2024Light, font, dpi) { Hot = true, Pressed = true, Focused = true };
                painter.Texts.Clear(); painter.Arrows = 0;
                if (item is RibbonSplitButtonItem split)
                {
                    int arrow = S(20);
                    painter.DrawSplitItem(context with { Bounds = new(0, 0, width - arrow, height) }, context with { Bounds = new(width - arrow, 0, arrow, height) }, split, RibbonItemSize.Medium, true);
                }
                else painter.DrawItem(context, item, RibbonItemSize.Medium, true);
                var title = Assert.Single(painter.Texts, t => t.Text == item.Text);
                var body = Assert.Single(painter.Texts, t => t.Text == description);
                Assert.Equal(title.Bounds.Left, body.Bounds.Left); Assert.Equal(title.Bounds.Bottom + S(2), body.Bounds.Top);
                Assert.InRange(body.Bounds.Height, 1, 2 * font.Height); Assert.True(row.Contains(body.Bounds));
                Assert.True(body.Bounds.Bottom <= row.Bottom - S(5));
                Assert.Equal(enabled ? context.Theme.SecondaryText : SystemColors.GrayText, body.Color);
                Assert.Equal(item is RibbonDropDownItem ? 1 : 0, painter.Arrows);
                Assert.Equal(description, item.MenuDescription);
            }
            using var separator = new RibbonMenuSeparatorItem();
            int separatorHeight = RibbonMenuLayout.RowHeight(separator, width, font, dpi);
            Assert.Equal(S(9), separatorHeight);
            using var lineImage = new Bitmap(width, separatorHeight); using var lineGraphics = Graphics.FromImage(lineImage);
            painter.DrawItem(new(lineGraphics, new(0, 0, width, separatorHeight), RibbonThemes.Office2024Light, font, dpi) { Hot = true, Pressed = true, Focused = true }, separator, RibbonItemSize.Medium, true);
            Assert.False(painter.SeparatorInteraction);
            Assert.Equal(RibbonThemes.Office2024Light.Border.ToArgb(), lineImage.GetPixel(S(8), (separatorHeight - 1) / 2).ToArgb());
            using var editor = new RibbonTextBoxItem { MenuDescription = description };
            Assert.Equal(Math.Max(S(28), font.Height + S(10)), RibbonMenuLayout.RowHeight(editor, width, font, dpi));
        }
        foreach (var item in items) item.Dispose();
    });

    [Fact]
    public void Mixed_menu_geometry_and_submenu_placement_fit_the_work_area() => FoundationTests.Sta(() =>
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            var work = new Rectangle(-1200, -200, 1200, 800); var size = new Size(240, 260);
            var anchor = new Rectangle(-900, -100, 200, 40);
            var child = RibbonPopupManager.CalculateBounds(anchor, work, size, true, dpi);
            Assert.True(child.Left > anchor.Right); Assert.True(work.Contains(child));
            anchor.X = -250;
            child = RibbonPopupManager.CalculateBounds(anchor, work, size, true, dpi);
            Assert.True(child.Right < anchor.Left); Assert.True(work.Contains(child));
            anchor.Y = work.Bottom - 40;
            var root = RibbonPopupManager.CalculateBounds(anchor, work, size, false, dpi);
            Assert.Equal(anchor.Top, root.Bottom);
            Assert.True(work.Contains(RibbonPopupManager.CalculateBounds(anchor, work, new(2000, 2000), true, dpi)));
        }
        using var form = new Form { Width = 1000, Height = 700 }; var ribbon = new RibbonControl();
        var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem { Text = "Menu" };
        var first = new RibbonButtonItem { Text = "First", MenuDescription = "An inline command description." };
        var separator = new RibbonMenuSeparatorItem(); var nested = new RibbonDropDownItem { Text = "Nested" };
        var choice = new RibbonButtonItem { Text = "Choice" }; nested.Items.Add(choice);
        var disabled = new RibbonButtonItem { Enabled = false }; var hidden = new RibbonButtonItem { Visible = false };
        var editor = new RibbonTextBoxItem { Text = "Value", Value = "Retained" };
        menu.Items.AddRange(first, separator, disabled, hidden, nested, editor);
        group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon);
        using var surface = new RibbonPopupSurface(ribbon, menu, new(1600, 1000));
        Assert.DoesNotContain(surface.Targets, t => ReferenceEquals(t.Element, hidden));
        var targets = surface.Targets;
        for (int i = 1; i < targets.Count; i++) Assert.Equal(targets[i - 1].Bounds.Bottom, targets[i].Bounds.Top);
        Assert.False(targets.Single(t => ReferenceEquals(t.Element, separator)).Enabled);
        Assert.False(targets.Single(t => ReferenceEquals(t.Element, disabled)).Enabled);
        Assert.Single(targets.Where(t => t.Element is not RibbonMenuSeparatorItem).Select(t => t.Bounds.Height).Distinct());
        first.MenuDescription = "Updated";
        using var updated = new RibbonPopupSurface(ribbon, menu, new(1600, 1000));
        Assert.Equal("Retained", editor.Value); Assert.Same(menu, first.Parent);
        Assert.True(updated.Size.Height <= surface.Size.Height);
    });

    private sealed class Recorder : RibbonRenderer
    {
        internal readonly List<(string Text, Rectangle Bounds, Color Color)> Texts = [];
        internal int Arrows; internal bool SeparatorInteraction;
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { Texts.Add((text, c.Bounds, color)); base.DrawText(c, text, color, flags); }
        public override void DrawSubmenuArrow(RibbonPaintContext c, Color color) { Arrows++; base.DrawSubmenuArrow(c, color); }
        public override void DrawMenuSeparator(RibbonPaintContext c, RibbonMenuSeparatorItem item) { SeparatorInteraction |= c.Hot || c.Pressed || c.Focused; base.DrawMenuSeparator(c, item); }
    }
}
