using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class EditorAppearanceTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Field_decorations_preserve_state_ink_fills_clips_and_symmetric_corners(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
            using var image = new Bitmap(S(230), S(100)); using var g = Graphics.FromImage(image);
            var bounds = new Rectangle(S(8), S(8), S(210), S(80));
            var settings = new RibbonEditorAppearanceSettings { CornerRadius = 0, BorderWidth = 3, Inset = 4, ActiveBorderColor = Color.DarkOrange };
            var theme = RibbonThemes.Office2024Light with { EditorAppearance = settings, EditorBackground = Color.LightYellow, EditorBorder = Color.DarkBlue,
                Hover = new(Color.LightBlue), Pressed = new(Color.LightPink) };
            var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi, Hot: true, Focused: true);
            using var text = new RibbonTextBoxItem(); using var combo = new RibbonComboBoxItem();
            g.Clear(Color.White); painter.DrawTextBox(c, text, bounds);
            Assert.Equal(Color.DarkOrange.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + bounds.Height / 2).ToArgb());
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Left, bounds.Top).ToArgb());
            text.Enabled = false; g.Clear(Color.White); painter.DrawTextBox(c, text, bounds);
            Assert.Equal(Color.DarkBlue.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + bounds.Height / 2).ToArgb());
            foreach (bool pressed in new[] { false, true })
            {
                g.Clear(Color.White); painter.DrawComboBox(c with { Pressed = pressed }, combo, bounds);
                var button = RibbonEditorLayout.ComboButton(bounds, dpi, theme: theme);
                Assert.Equal((pressed ? Color.LightPink : Color.LightBlue).ToArgb(), image.GetPixel(button.Left + S(5), bounds.Top + bounds.Height / 2).ToArgb());
                Assert.Equal(Color.LightYellow.ToArgb(), image.GetPixel(bounds.Left + S(10), bounds.Top + bounds.Height / 2).ToArgb());
            }
            combo.Enabled = false; g.Clear(Color.White); painter.DrawComboBox(c with { Pressed = true }, combo, bounds);
            Assert.Equal(Color.LightYellow.ToArgb(), image.GetPixel(bounds.Right - S(10), bounds.Top + bounds.Height / 2).ToArgb());
            foreach (int? width in new int?[] { 0, 32 })
            {
                g.Clear(Color.White); painter.DrawTextBox(c with { Theme = theme with { EditorAppearance = settings with { BorderWidth = width } } }, text, bounds);
                if (width == 0) Assert.Equal(Color.LightYellow.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + bounds.Height / 2).ToArgb());
            }
            foreach (int inset in new[] { 0, 1024 })
            {
                g.Clear(Color.White); painter.DrawTextBox(c with { Theme = theme with { EditorAppearance = settings with { Inset = inset } } }, text, bounds);
                Assert.Equal(bounds, record.ChromeBounds);
            }
            g.Clear(Color.White); painter.DrawTextBox(c with { Theme = theme with { EditorAppearance = settings with { CornerRadius = 12, BorderWidth = 2 } } }, text, bounds);
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + S(4)).ToArgb());
            for (int y = bounds.Top; y < bounds.Bottom; y++) for (int x = bounds.Left; x < bounds.Right; x++)
            { Assert.Equal(image.GetPixel(x, y), image.GetPixel(bounds.Right - 1 - (x - bounds.Left), y)); Assert.Equal(image.GetPixel(x, y), image.GetPixel(x, bounds.Bottom - 1 - (y - bounds.Top))); }
            text.Enabled = true; g.Clear(Color.White);
            painter.DrawTextBox(c with { Theme = theme with { EditorAppearance = settings with { ActiveBorderColor = Color.FromArgb(128, 255, 0, 0) } } }, text, bounds);
            var alpha = image.GetPixel(bounds.Left + S(4), bounds.Top + bounds.Height / 2); Assert.True(alpha.R > alpha.G && alpha.G > 0);
            Assert.Equal(bounds, record.ChromeBounds); Assert.True(record.Arrows > 0);
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Choice_shape_and_horizontal_padding_reset_independently_of_field_geometry(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
            using var image = new Bitmap(S(240), S(90)); using var g = Graphics.FromImage(image);
            using var combo = new RibbonComboBoxItem(); combo.Items.Add("Choice");
            var bounds = new Rectangle(S(8), S(8), S(210), S(70));
            var settings = new RibbonEditorAppearanceSettings { CornerRadius = 12, Inset = 0, ChoiceCornerRadius = 0, ChoiceInset = 4, ChoiceTextHorizontalPadding = 14 };
            var theme = RibbonThemes.Office2024Light with { EditorAppearance = settings, PopupBackground = Color.White, Checked = new(Color.LightGreen) };
            var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
            g.Clear(Color.White); painter.DrawComboBoxItem(c, combo, 0, true);
            Assert.Equal(Rectangle.Inflate(bounds, -S(14), 0), record.TextBounds);
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Left, bounds.Top + bounds.Height / 2).ToArgb());
            Assert.Equal(Color.LightGreen.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + bounds.Height / 2).ToArgb());
            foreach (int padding in new[] { 0, 1024 })
            {
                painter.DrawComboBoxItem(c with { Theme = theme with { EditorAppearance = settings with { ChoiceTextHorizontalPadding = padding } } }, combo, 0, true);
                Assert.Equal(padding == 0 ? bounds : new Rectangle(bounds.Left + bounds.Width / 2, bounds.Top, bounds.Width % 2, bounds.Height), record.TextBounds);
            }
            painter.DrawComboBoxItem(c with { Theme = theme with { EditorAppearance = settings with { ChoiceTextHorizontalPadding = null } } }, combo, 0, true);
            Assert.Equal(Rectangle.Inflate(bounds, -S(6), 0), record.TextBounds);
            g.Clear(Color.White); painter.DrawComboBoxItem(c with { Theme = theme with { EditorAppearance = settings with { ChoiceCornerRadius = 12 } } }, combo, 0, true);
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + S(4)).ToArgb());
            g.Clear(Color.White); painter.DrawComboBoxItem(c with { Theme = theme with { EditorAppearance = settings with { ChoiceInset = 0 } } }, combo, 0, true);
            Assert.Equal(Color.LightGreen.ToArgb(), image.GetPixel(bounds.Left, bounds.Top + bounds.Height / 2).ToArgb());
        }
    });

    [Theory] [InlineData("CornerRadius", 64)] [InlineData("ChoiceCornerRadius", 64)] [InlineData("BorderWidth", 32)]
    [InlineData("Inset", 1024)] [InlineData("ChoiceInset", 1024)] [InlineData("ChoiceTextHorizontalPadding", 1024)]
    public void Invalid_editor_roles_leave_theme_and_builtin_selection_unchanged(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme;
        var property = typeof(RibbonEditorAppearanceSettings).GetProperty(name)!;
        foreach (int n in new[] { -1, maximum + 1 })
        {
            var value = new RibbonEditorAppearanceSettings(); property.SetValue(value, n);
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { EditorAppearance = value }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme);
        }
        foreach (int? n in new int?[] { 0, maximum, null })
        { var value = new RibbonEditorAppearanceSettings(); property.SetValue(value, n); ribbon.Theme = original with { EditorAppearance = value }; Assert.Same(value, ribbon.Theme.EditorAppearance); }
    });

    [Fact]
    public void Actual_chrome_text_arrow_hooks_and_immutable_group_reset_retain_precedence() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl();
        var settings = new RibbonEditorAppearanceSettings { CornerRadius = 12, ChoiceInset = 4, ActiveBorderColor = Color.DarkOrange };
        var application = ribbon.Theme with { EditorAppearance = settings, CommandAppearance = new() { BorderWidth = 2 } }; ribbon.Theme = application;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = null; Assert.Same(application, ribbon.Theme); Assert.Null(other.Theme.EditorAppearance);
        ribbon.Theme = application with { EditorAppearance = settings with { CornerRadius = null } }; Assert.Equal(4, ribbon.Theme.EditorAppearance!.ChoiceInset); Assert.Equal(12, settings.CornerRadius);
        ribbon.Theme = application with { EditorAppearance = null }; Assert.Equal(2, ribbon.Theme.CommandAppearance!.BorderWidth);
        foreach (bool office in new[] { false, true })
        {
            var record = new Record { Replace = true }; using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
            using var image = new Bitmap(240, 90); using var g = Graphics.FromImage(image); g.Clear(Color.White);
            using var combo = new RibbonComboBoxItem { Text = "Label" }; combo.Items.Add("Choice");
            var c = new RibbonPaintContext(g, new(5, 5, 225, 70), application, ribbon.Font, 120, Hot: true);
            var field = new Rectangle(60, 5, 170, 70); painter.DrawComboBox(c, combo, field);
            Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(75, 30).ToArgb()); Assert.Equal(2, record.ChromeCalls); Assert.Equal(1, record.Arrows); Assert.Same(application, record.TextTheme);
            painter.DrawComboBoxItem(c, combo, 0, true); Assert.Equal(3, record.ChromeCalls); Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(15, 30).ToArgb());
        }
    });

    private sealed class Record
    {
        internal bool Replace;
        internal int ChromeCalls, Arrows;
        internal Rectangle ChromeBounds, TextBounds;
        internal RibbonTheme? TextTheme;
        internal bool Chrome(RibbonPaintContext c) { ChromeCalls++; ChromeBounds = c.Bounds; if (Replace) c.Graphics.FillRectangle(Brushes.Magenta, c.Bounds); return Replace; }
        internal void Text(RibbonPaintContext c) { TextBounds = c.Bounds; TextTheme = c.Theme; }
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        protected override void DrawChrome(RibbonPaintContext c, RibbonFill fill, Color border) { if (!record.Chrome(c)) base.DrawChrome(c, fill, border); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) => record.Text(c);
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) => record.Arrows++;
    }
    private sealed class Flat(Record record) : RibbonRenderer
    {
        protected override void DrawChrome(RibbonPaintContext c, RibbonFill fill, Color border) { if (!record.Chrome(c)) base.DrawChrome(c, fill, border); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) => record.Text(c);
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) => record.Arrows++;
    }
}
