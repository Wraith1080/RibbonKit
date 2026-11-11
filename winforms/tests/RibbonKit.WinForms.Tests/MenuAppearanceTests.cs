using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MenuAppearanceTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Heading_shape_ink_and_bold_settings_reach_actual_chrome_and_text_without_changing_slots(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            var record = new Record(); using RibbonRenderer renderer = office ? new Office(record) : new Flat(record);
            using var font = new Font("Segoe UI", 9, FontStyle.Bold | FontStyle.Italic);
            using var image = new Bitmap(S(240), S(100)); using var g = Graphics.FromImage(image); var bounds = new Rectangle(S(8), S(8), S(220), S(80));
            var settings = new RibbonMenuAppearanceSettings { HeaderCornerRadius = 0, HeaderInset = 4, HeaderBorderWidth = 2, HeaderBorderColor = Color.DarkRed, HeaderBold = false };
            var theme = RibbonThemes.Office2024Light with { MenuAppearance = settings, MenuHeadingBackground = new(Color.LightBlue), MenuHeadingForeground = Color.Navy };
            var c = new RibbonPaintContext(g, bounds, theme, font, dpi, Hot: true, Pressed: true, Focused: true);
            g.Clear(Color.White); renderer.DrawMenuHeader(c, "Heading");
            Assert.Equal(Color.DarkRed.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + bounds.Height / 2).ToArgb());
            Assert.Equal(Color.LightBlue.ToArgb(), image.GetPixel(bounds.Left + S(7), bounds.Top + bounds.Height / 2).ToArgb());
            Assert.Equal(Rectangle.Inflate(bounds, -S(6), 0), record.TextBounds); Assert.Equal(FontStyle.Italic, record.Style); Assert.Equal(Color.Navy.ToArgb(), record.TextColor.ToArgb());
            Assert.False(record.Hot || record.Pressed || record.Focused); Assert.Same(theme, record.Theme);
            renderer.DrawMenuHeader(c with { Theme = theme with { MenuAppearance = settings with { HeaderBold = null } } }, "Heading"); Assert.True(record.Style.HasFlag(FontStyle.Bold));
            g.Clear(Color.White); renderer.DrawMenuHeader(c with { Theme = theme with { MenuAppearance = settings with { HeaderCornerRadius = 12 } } }, "Heading");
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + S(4)).ToArgb());
            g.Clear(Color.White); renderer.DrawMenuHeader(c with { Theme = theme with { MenuAppearance = settings with { HeaderBorderWidth = 0 } } }, "Heading");
            Assert.Equal(Color.LightBlue.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + bounds.Height / 2).ToArgb());
            renderer.DrawMenuHeader(c with { Theme = theme with { MenuAppearance = settings with { HeaderInset = 1024, HeaderBorderWidth = 32 } } }, "Heading");
            renderer.DrawGalleryHeading(c, "Gallery"); Assert.True(record.Style.HasFlag(FontStyle.Bold));
            Assert.NotEqual(Color.Navy.ToArgb(), record.TextColor.ToArgb());
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Separator_thickness_color_alpha_and_hide_are_centered_and_keep_other_surfaces_independent(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var separator = new RibbonMenuSeparatorItem();
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(240), S(70)); using var g = Graphics.FromImage(image); var bounds = new Rectangle(S(8), S(8), S(220), S(20));
            var theme = RibbonThemes.Office2024Light with { MenuAppearance = new() { SeparatorThickness = 3, SeparatorColor = Color.FromArgb(128, 0, 0, 255) } };
            var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
            g.Clear(Color.Transparent); renderer.DrawMenuSeparator(c, separator);
            var expected = new Rectangle(bounds.Left + S(8), bounds.Top + (bounds.Height - S(3)) / 2, bounds.Width - 2 * S(8), S(3)); Assert.Equal(expected, Ink(image)); Assert.Equal(128, image.GetPixel(expected.Left, expected.Top).A);
            g.Clear(Color.Transparent); renderer.DrawMenuSeparator(c with { Theme = theme with { MenuAppearance = theme.MenuAppearance! with { SeparatorThickness = 0 } } }, separator); Assert.True(Ink(image).IsEmpty);
            g.Clear(Color.Transparent); renderer.DrawMenuSeparator(c with { Theme = theme with { MenuAppearance = theme.MenuAppearance! with { SeparatorColor = Color.Transparent } } }, separator); Assert.True(Ink(image).IsEmpty);
            g.Clear(Color.Transparent); renderer.DrawMenuSeparator(c with { Theme = theme with { MenuAppearance = theme.MenuAppearance! with { SeparatorThickness = 32 } } }, separator); Assert.Equal(bounds.Height, Ink(image).Height);
            foreach (int surface in new[] { 0, 1, 2 })
            {
                void Paint(RibbonTheme value)
                {
                    var current = c with { Theme = value };
                    if (surface == 0) renderer.DrawMenuSeparator(current with { ApplicationMenuPart = RibbonApplicationMenuPart.Navigation }, separator);
                    else if (surface == 1) renderer.DrawBackstageEntry(current, separator, false);
                    else renderer.DrawGroupSeparator(current, separator);
                }
                g.Clear(Color.Transparent); Paint(theme with { MenuAppearance = null }); using var baseline = (Bitmap)image.Clone();
                g.Clear(Color.Transparent); Paint(theme); Same(baseline, image);
            }
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Overflow_backing_stays_opaque_with_gradients_and_endpoint_masking_preserves_targets(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(140), S(90)); using var g = Graphics.FromImage(image); var bounds = new Rectangle(S(8), S(8), S(120), S(70));
            var settings = new RibbonMenuAppearanceSettings { OverflowBackground = new(Color.LightYellow, Color.LightBlue), OverflowCornerRadius = 0, OverflowInset = 4, OverflowBorderWidth = 2, OverflowBorderColor = Color.DarkRed };
            var theme = RibbonThemes.Office2024Light with { MenuAppearance = settings, Hover = new(Color.FromArgb(128, Color.Green)), Pressed = new(Color.LightPink) };
            var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi, Hot: true, Pressed: true);
            g.Clear(Color.Transparent); renderer.DrawOverflowScrollButton(c, false, true);
            Assert.Equal(Color.DarkRed.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + bounds.Height / 2).ToArgb());
            Assert.Equal(Color.LightPink.ToArgb(), image.GetPixel(bounds.Left + S(7), bounds.Top + bounds.Height / 2).ToArgb());
            for (int y = bounds.Top; y < bounds.Bottom; y++) for (int x = bounds.Left; x < bounds.Right; x++) Assert.Equal(255, image.GetPixel(x, y).A);
            g.Clear(Color.Transparent); renderer.DrawOverflowScrollButton(c, true, false); using var disabled = (Bitmap)image.Clone();
            g.Clear(Color.Transparent); renderer.DrawOverflowScrollButton(c with { Hot = false, Pressed = false }, true, false); Same(disabled, image);
            foreach (var value in new[] { settings with { OverflowCornerRadius = 12 }, settings with { OverflowInset = 1024, OverflowBorderWidth = 32 }, settings with { OverflowInset = 0, OverflowBorderWidth = 0 } })
            { g.Clear(Color.Transparent); renderer.DrawOverflowScrollButton(c with { Theme = theme with { MenuAppearance = value } }, false, true); Assert.True(bounds.Contains(Ink(image))); }
            renderer.DrawOverflowScrollButton(c with { Bounds = new(bounds.Location, new Size(1, 1)) }, false, true);
        }
    });

    [Theory] [InlineData("HeaderCornerRadius", 64)] [InlineData("HeaderInset", 1024)] [InlineData("HeaderBorderWidth", 32)] [InlineData("SeparatorThickness", 32)]
    [InlineData("OverflowCornerRadius", 64)] [InlineData("OverflowInset", 1024)] [InlineData("OverflowBorderWidth", 32)]
    public void Invalid_numeric_roles_are_atomic_and_accept_independent_null_reset(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var property = typeof(RibbonMenuAppearanceSettings).GetProperty(name)!;
        foreach (int n in new[] { -1, maximum + 1 })
        { var settings = new RibbonMenuAppearanceSettings(); property.SetValue(settings, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { MenuAppearance = settings }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (int? n in new int?[] { 0, maximum, null }) { var settings = new RibbonMenuAppearanceSettings(); property.SetValue(settings, n); ribbon.Theme = original with { MenuAppearance = settings }; Assert.Same(settings, ribbon.Theme.MenuAppearance); }
    });

    [Fact]
    public void Overflow_fill_validation_and_immutable_reset_preserve_application_theme_and_other_groups() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; using var other = new RibbonControl(); var original = ribbon.Theme;
        foreach (var fill in new[] { new RibbonFill(Color.Transparent), new RibbonFill(Color.White, Color.FromArgb(254, Color.Blue)), new RibbonFill(Color.FromArgb(128, Color.Red), Color.White) })
        { Assert.Throws<ArgumentException>(() => ribbon.Theme = original with { MenuAppearance = new() { OverflowBackground = fill } }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        var settings = new RibbonMenuAppearanceSettings { HeaderBold = false, HeaderInset = 3, SeparatorThickness = 3, OverflowBackground = new(Color.LightYellow, Color.LightBlue) };
        var application = original with { MenuAppearance = settings, PopupAppearance = new() { BorderWidth = 2 } }; ribbon.Theme = application;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; ribbon.BuiltInTheme = null; Assert.Same(application, ribbon.Theme); Assert.Null(other.Theme.MenuAppearance);
        ribbon.Theme = application with { MenuAppearance = settings with { HeaderInset = null } }; Assert.Equal(3, ribbon.Theme.MenuAppearance!.SeparatorThickness); Assert.Equal(3, settings.HeaderInset);
        ribbon.Theme = application with { MenuAppearance = null }; Assert.Equal(2, ribbon.Theme.PopupAppearance!.BorderWidth);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Decoration_only_changes_preserve_native_rows_header_bands_uia_and_editor_state(bool simplified) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Bounds = new(-16000, -16000, 1200, 700), ShowInTaskbar = false };
        using var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem { Text = "Menu", DropDownHeader = "Heading", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var separator = new RibbonMenuSeparatorItem(); var check = new RibbonCheckBoxItem { Text = "Keep", Checked = true }; var input = new RibbonTextBoxItem { Text = "Name", Value = "Retained" };
        menu.Items.AddRange(separator, check, input); group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Show(); Application.DoEvents();
        ribbon.LayoutMode = simplified ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic; menu.ShowDropDown();
        var before = ribbon.Popups.Top!; var size = before.Size; var header = before.MenuHeaderBounds; var rows = before.Targets.Select(t => t.Bounds).ToArray();
        var editor = ribbon.Editors.EditorFor(input)!; nint handle = editor.Handle; var parent = editor.Parent; editor.Select(1, 3);
        ribbon.Theme = ribbon.Theme with { MenuAppearance = new() { HeaderCornerRadius = 8, HeaderBorderWidth = 2, HeaderBorderColor = Color.Red, HeaderBold = false, SeparatorThickness = 3, SeparatorColor = Color.Blue, OverflowInset = 4 } };
        menu.ShowDropDown(); var after = ribbon.Popups.Top!;
        Assert.Equal(size, after.Size); Assert.Equal(header, after.MenuHeaderBounds); Assert.Equal(rows, after.Targets.Select(t => t.Bounds));
        Assert.Equal(handle, editor.Handle); Assert.Same(parent, editor.Parent); Assert.Equal("Retained", editor.Text); Assert.Equal(1, editor.SelectionStart); Assert.Equal(3, editor.SelectionLength);
        Assert.DoesNotContain(after.AutomationRoot.Children, node => node.Element == separator); Assert.True(check.Checked); form.Close();
    });

    [Fact]
    public void Public_heading_gallery_chrome_and_text_overrides_keep_precedence()
    {
        FoundationTests.Sta(() =>
        {
            var record = new Record { Replace = true }; using var renderer = new Office(record); using var image = new Bitmap(220, 100); using var g = Graphics.FromImage(image);
            var theme = RibbonThemes.Office2024Light with { MenuAppearance = new() { HeaderCornerRadius = 8, OverflowInset = 4 }, MenuHeadingBackground = new(Color.LightBlue) };
            var c = new RibbonPaintContext(g, new(5, 5, 210, 80), theme, Control.DefaultFont, 120, Hot: true);
            renderer.DrawMenuHeader(c, "Heading"); Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(10, 10).ToArgb()); Assert.Equal(1, record.ChromeCalls); Assert.Same(theme, record.Theme);
            renderer.DrawOverflowScrollButton(c, false, true); Assert.Equal(2, record.ChromeCalls);
            using var custom = new GalleryOverride(); custom.DrawMenuHeader(c, "Heading"); Assert.Equal(1, custom.Calls); Assert.Equal(Color.Cyan.ToArgb(), image.GetPixel(10, 10).ToArgb());
        });
    }

    private static Rectangle Ink(Bitmap image)
    {
        var bounds = Rectangle.Empty;
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).A > 0) bounds = bounds.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(bounds, new(x, y, 1, 1));
        return bounds;
    }
    private static void Same(Bitmap a, Bitmap b) { for (int y = 0; y < a.Height; y++) for (int x = 0; x < a.Width; x++) Assert.Equal(a.GetPixel(x, y), b.GetPixel(x, y)); }
    private sealed class Record
    {
        internal bool Replace, Hot, Pressed, Focused;
        internal int ChromeCalls;
        internal FontStyle Style;
        internal Color TextColor;
        internal Rectangle TextBounds;
        internal RibbonTheme? Theme;
        internal bool Chrome(RibbonPaintContext c) { ChromeCalls++; if (Replace) c.Graphics.FillRectangle(Brushes.Magenta, c.Bounds); return Replace; }
        internal void Text(RibbonPaintContext c, Color color) { Style = c.Font.Style; TextColor = color; TextBounds = c.Bounds; Hot = c.Hot; Pressed = c.Pressed; Focused = c.Focused; Theme = c.Theme; }
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        protected override void DrawChrome(RibbonPaintContext c, RibbonFill fill, Color border) { if (!record.Chrome(c)) base.DrawChrome(c, fill, border); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) => record.Text(c, color);
    }
    private sealed class Flat(Record record) : RibbonRenderer
    {
        protected override void DrawChrome(RibbonPaintContext c, RibbonFill fill, Color border) { if (!record.Chrome(c)) base.DrawChrome(c, fill, border); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) => record.Text(c, color);
    }
    private sealed class GalleryOverride : RibbonRenderer
    {
        internal int Calls;
        public override void DrawGalleryHeading(RibbonPaintContext c, string text) { Calls++; c.Graphics.FillRectangle(Brushes.Cyan, c.Bounds); }
    }
}
