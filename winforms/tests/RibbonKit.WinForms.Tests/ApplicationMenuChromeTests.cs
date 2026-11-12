using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ApplicationMenuChromeTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Row_and_footer_shape_outlines_alpha_and_hide_preserve_state_and_content_bounds(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
            using var item = new RibbonButtonItem { Text = "Command" }; using var image = new Bitmap(S(250), S(100)); using var g = Graphics.FromImage(image);
            var bounds = new Rectangle(S(8), S(8), S(230), S(80));
            var settings = new RibbonApplicationMenuAppearanceSettings { RowCornerRadius = 0, RowInset = 4, RowBorderWidth = 3, RowBorderColor = Color.DarkRed, FooterInset = 5, FooterBorderWidth = 2, FooterBorderColor = Color.Navy };
            var theme = RibbonThemes.Office2024Light with { ApplicationMenuAppearance = settings, Pressed = new(Color.Magenta) };
            foreach (var part in new[] { RibbonApplicationMenuPart.Navigation, RibbonApplicationMenuPart.Pane, RibbonApplicationMenuPart.Footer })
            {
                var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi, Pressed: true) { ApplicationMenuPart = part };
                g.Clear(Color.White); painter.DrawItem(c with { Theme = theme with { ApplicationMenuAppearance = null } }, item, RibbonItemSize.Medium, true); var text = record.Text;
                g.Clear(Color.White); painter.DrawItem(c, item, RibbonItemSize.Medium, true); Assert.Equal(text, record.Text);
                bool footer = part == RibbonApplicationMenuPart.Footer;
                Assert.Equal((footer ? Color.Navy : Color.DarkRed).ToArgb(), image.GetPixel(bounds.Left + S(footer ? 5 : 4), bounds.Top + bounds.Height / 2).ToArgb());
                Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(bounds.Left + S(9), bounds.Top + bounds.Height / 2).ToArgb());
                item.Enabled = false; g.Clear(Color.White); painter.DrawItem(c with { Hot = true, Focused = true }, item, RibbonItemSize.Medium, true);
                Assert.Equal(footer ? Color.Navy.ToArgb() : Color.White.ToArgb(), image.GetPixel(bounds.Left + S(footer ? 5 : 4), bounds.Top + bounds.Height / 2).ToArgb()); item.Enabled = true;
                g.Clear(Color.White); painter.DrawItem(c with { Theme = theme with { ApplicationMenuAppearance = settings with { RowBorderWidth = 0, FooterBorderWidth = 0 } } }, item, RibbonItemSize.Medium, true);
                Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(bounds.Left + S(footer ? 5 : 4), bounds.Top + bounds.Height / 2).ToArgb());
                painter.DrawItem(c with { Theme = theme with { ApplicationMenuAppearance = settings with { RowInset = 1024, FooterInset = 1024, RowBorderWidth = 32, FooterBorderWidth = 32 } } }, item, RibbonItemSize.Medium, true);
            }
            var row = new RibbonPaintContext(g, bounds, theme with { ApplicationMenuAppearance = settings with { RowCornerRadius = 12 } }, Control.DefaultFont, dpi, Pressed: true) { ApplicationMenuPart = RibbonApplicationMenuPart.Navigation };
            g.Clear(Color.White); painter.DrawItem(row, item, RibbonItemSize.Medium, true);
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + S(4)).ToArgb());
            for (int y = bounds.Top; y < bounds.Bottom; y++) for (int x = bounds.Left; x < bounds.Right; x++)
            { Assert.Equal(image.GetPixel(x, y), image.GetPixel(bounds.Right - 1 - (x - bounds.Left), y)); Assert.Equal(image.GetPixel(x, y), image.GetPixel(x, bounds.Bottom - 1 - (y - bounds.Top))); }
            g.Clear(Color.White); painter.DrawItem(row with { Theme = theme with { ApplicationMenuAppearance = settings with { RowBorderColor = Color.FromArgb(128, 0, 0, 255) } } }, item, RibbonItemSize.Medium, true);
            var alpha = image.GetPixel(bounds.Left + S(4), bounds.Top + bounds.Height / 2); Assert.Equal(255, alpha.B); Assert.InRange(alpha.R, 1, 254);
            g.Clear(Color.White); painter.DrawItem(row with { ApplicationMenuPart = RibbonApplicationMenuPart.None }, item, RibbonItemSize.Medium, false); using var ordinary = (Bitmap)image.Clone();
            g.Clear(Color.White); painter.DrawItem(row with { ApplicationMenuPart = RibbonApplicationMenuPart.None, Theme = row.Theme with { ApplicationMenuAppearance = null } }, item, RibbonItemSize.Medium, false); Same(ordinary, image);
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Split_divider_selected_primary_and_hover_precedence_keep_one_shared_perimeter(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new Office(new Record()); using var form = new Form { Bounds = new(-16000, -16000, 1000, 700), ShowInTaskbar = false };
        using var ribbon = new RibbonControl { Renderer = painter, AnimationLevel = RibbonAnimationLevel.None, FileMenuMode = RibbonFileMenuMode.CommandMenu }; ribbon.FileMenu.Visible = true;
        var split = new RibbonSplitButtonItem(); split.Items.Add(new RibbonButtonItem { Text = "Child" }); ribbon.FileMenu.Items.Add(split); form.Controls.Add(ribbon); form.Show(); Application.DoEvents();
        using var image = new Bitmap(S(270), S(110)); using var g = Graphics.FromImage(image); var whole = new Rectangle(S(10), S(10), S(245), S(85));
        var settings = new RibbonApplicationMenuAppearanceSettings { RowInset = 2, RowBorderWidth = 2, RowBorderColor = Color.Blue, FooterInset = 2, FooterBorderWidth = 2, FooterBorderColor = Color.Blue,
            SplitDividerWidth = 3, SplitDividerInset = 6, SplitDividerColor = Color.Magenta, SelectedPrimaryTintStrength = 0 };
        var theme = RibbonThemes.Office2024Light with { ApplicationMenuAppearance = settings, PopupBackground = Color.White, Hover = new(Color.LightBlue), Pressed = new(Color.DeepPink) };
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (var part in new[] { RibbonApplicationMenuPart.Navigation, RibbonApplicationMenuPart.Pane, RibbonApplicationMenuPart.Footer })
        {
            ribbon.Density = density; ribbon.FileMenu.ShowDropDown(); split.ShowDropDown(); Assert.True(split.IsDropDownOpen);
            var targets = RibbonApplicationMenuMetrics.Expand(new(TargetKind.Item, split, whole, whole), dpi, density).ToArray();
            var c = new RibbonPaintContext(g, targets[0].Bounds, theme, Control.DefaultFont, dpi) { Density = density, ApplicationMenuPart = part };
            var drop = c with { Bounds = targets[1].Bounds }; var mainPoint = new Point(whole.Left + S(10), whole.Top + whole.Height / 2); var arrowPoint = new Point(drop.Bounds.Left + drop.Bounds.Width / 2, mainPoint.Y);
            void Draw(RibbonTheme value, bool hover = false) { g.Clear(Color.White); painter.DrawSplitItem(c with { Theme = value }, drop with { Theme = value, Hot = hover }, split, RibbonItemSize.Medium, true); }
            Draw(theme); Assert.Equal(Color.White.ToArgb(), image.GetPixel(mainPoint.X, mainPoint.Y).ToArgb()); Assert.Equal(Color.DeepPink.ToArgb(), image.GetPixel(arrowPoint.X, arrowPoint.Y).ToArgb());
            Assert.Equal(new Rectangle(drop.Bounds.Left, whole.Top + S(6), S(3), whole.Height - 2 * S(6)), Ink(image, Color.Magenta));
            Draw(theme with { ApplicationMenuAppearance = settings with { SelectedPrimaryTintStrength = 1 } }); Assert.Equal(Color.DeepPink.ToArgb(), image.GetPixel(mainPoint.X, mainPoint.Y).ToArgb());
            var authored = settings with { SelectedPrimaryBackground = new(Color.FromArgb(128, Color.Green)), SelectedPrimaryTintStrength = 1 };
            Draw(theme with { ApplicationMenuAppearance = authored }); var pixel = image.GetPixel(mainPoint.X, mainPoint.Y); Assert.True(pixel.G > pixel.R && pixel.R > 0);
            Draw(theme with { ApplicationMenuAppearance = authored }, hover: true); Assert.Equal(Color.LightBlue.ToArgb(), image.GetPixel(mainPoint.X, mainPoint.Y).ToArgb()); Assert.Equal(Color.LightBlue.ToArgb(), image.GetPixel(arrowPoint.X, arrowPoint.Y).ToArgb());
            foreach (var value in new[] { theme with { ApplicationMenuSplitDividerVisible = false }, theme with { ApplicationMenuAppearance = settings with { SplitDividerWidth = 0 } }, theme with { ApplicationMenuAppearance = settings with { SplitDividerColor = Color.Transparent } } })
            { Draw(value); Assert.True(Ink(image, Color.Magenta).IsEmpty); }
            Draw(theme with { ApplicationMenuAppearance = settings with { SplitDividerInset = 1024 } }); Assert.Equal(whole.Height % 2, Ink(image, Color.Magenta).Height);
            split.CloseDropDown(); ribbon.FileMenu.CloseDropDown();
        }
        form.Close();
    });

    [Theory] [InlineData("RowCornerRadius", 64)] [InlineData("RowInset", 1024)] [InlineData("RowBorderWidth", 32)] [InlineData("FooterInset", 1024)]
    [InlineData("FooterBorderWidth", 32)] [InlineData("SplitDividerWidth", 32)] [InlineData("SplitDividerInset", 1024)]
    public void Invalid_application_chrome_roles_are_atomic_and_reset_independently(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var property = typeof(RibbonApplicationMenuAppearanceSettings).GetProperty(name)!;
        foreach (int n in new[] { -1, maximum + 1 }) { var value = new RibbonApplicationMenuAppearanceSettings(); property.SetValue(value, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { ApplicationMenuAppearance = value }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (int? n in new int?[] { 0, maximum, null }) { var value = new RibbonApplicationMenuAppearanceSettings(); property.SetValue(value, n); ribbon.Theme = original with { ApplicationMenuAppearance = value }; Assert.Same(value, ribbon.Theme.ApplicationMenuAppearance); }
    });

    [Fact]
    public void Tint_validation_application_restore_and_group_reset_preserve_other_settings() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; using var other = new RibbonControl(); var original = ribbon.Theme;
        foreach (double n in new[] { -.1, 1.1, double.NaN, double.NegativeInfinity, double.PositiveInfinity })
        { Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { ApplicationMenuAppearance = new() { SelectedPrimaryTintStrength = n } }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        var settings = new RibbonApplicationMenuAppearanceSettings { RowCornerRadius = 8, SplitDividerWidth = 3, SelectedPrimaryTintStrength = 1 };
        var application = original with { ApplicationMenuAppearance = settings, PopupAppearance = new() { BorderWidth = 2 } }; ribbon.Theme = application; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; ribbon.BuiltInTheme = null;
        Assert.Same(application, ribbon.Theme); Assert.Null(other.Theme.ApplicationMenuAppearance);
        ribbon.Theme = application with { ApplicationMenuAppearance = settings with { RowCornerRadius = null, SelectedPrimaryTintStrength = 0 } }; Assert.Equal(3, ribbon.Theme.ApplicationMenuAppearance!.SplitDividerWidth); Assert.Equal(8, settings.RowCornerRadius);
        ribbon.Theme = application with { ApplicationMenuAppearance = null }; Assert.Equal(2, ribbon.Theme.PopupAppearance!.BorderWidth);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Authored_application_chrome_preserves_layout_targets_native_content_and_uia(bool simplified) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Bounds = new(-16000, -16000, 1100, 700), ShowInTaskbar = false }; using var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None };
        ribbon.FileMenu.Visible = true; form.Controls.Add(ribbon); var split = new RibbonSplitButtonItem { Text = "Save As" }; split.Items.Add(new RibbonButtonItem { Text = "Copy" }); ribbon.FileMenu.Items.Add(split);
        using var body = new UserControl { Size = new(280, 130) }; var editor = new TextBox { Text = "Retained application content", Bounds = new(12, 12, 240, 24) }; body.Controls.Add(editor);
        var content = new RibbonDropDownItem { Text = "Content", Content = body }; ribbon.FileMenu.Items.Add(content); var footer = new RibbonButtonItem { Text = "Options" }; ribbon.ApplicationMenu.FooterItems.Add(footer);
        form.Show(); Application.DoEvents(); ribbon.LayoutMode = simplified ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic; ribbon.FileMenu.ShowDropDown(); content.ShowDropDown();
        var before = ribbon.Popups.Top!; var bounds = before.Targets.Select(t => t.Bounds).ToArray(); var layout = before.ApplicationLayout!; nint handle = editor.Handle; var parent = editor.Parent; editor.Select(2, 5);
        ribbon.Theme = ribbon.Theme with { ApplicationMenuAppearance = new() { RowCornerRadius = 8, RowInset = 3, RowBorderWidth = 2, RowBorderColor = Color.DarkBlue, FooterInset = 3, FooterBorderWidth = 2, SplitDividerWidth = 3, SelectedPrimaryTintStrength = .5 } };
        ribbon.FileMenu.ShowDropDown(); content.ShowDropDown(); var after = ribbon.Popups.Top!;
        Assert.Equal(before.Size, after.Size); Assert.Equal(layout.Navigation, after.ApplicationLayout!.Navigation); Assert.Equal(layout.Footer, after.ApplicationLayout.Footer); Assert.Equal(bounds, after.Targets.Select(t => t.Bounds));
        Assert.Equal(handle, editor.Handle); Assert.Same(parent, editor.Parent); Assert.Equal("Retained application content", editor.Text); Assert.Equal(2, editor.SelectionStart); Assert.Equal(5, editor.SelectionLength);
        Assert.Contains(after.AutomationRoot.Children, node => node.Element == split); Assert.Contains(after.AutomationRoot.Children, node => node.Element == footer); form.Close();
    });

    [Fact]
    public void Item_split_text_image_arrow_and_focus_hooks_remain_on_the_actual_renderer() => FoundationTests.Sta(() =>
    {
        var record = new Record(); using var renderer = new Office(record); using var image = new Bitmap(280, 90); using var g = Graphics.FromImage(image); using var split = new RibbonSplitButtonItem { Text = "Split" };
        var theme = RibbonThemes.Office2024Light with { ApplicationMenuAppearance = new() { RowCornerRadius = 8, RowBorderWidth = 2, RowBorderColor = Color.Navy } };
        var c = new RibbonPaintContext(g, new(5, 5, 220, 75), theme, Control.DefaultFont, 120, Hot: true, Focused: true) { ApplicationMenuPart = RibbonApplicationMenuPart.Navigation };
        renderer.DrawSplitItem(c, c with { Bounds = new(225, 5, 45, 75) }, split, RibbonItemSize.Medium, true);
        Assert.Equal(1, record.Splits); Assert.Equal(1, record.Arrows); Assert.Equal(2, record.Focus.Count); Assert.Equal(c.Bounds, record.Focus[0]); Assert.Same(theme, record.Theme);
        using var raster = new Bitmap(16, 16); using var item = new RibbonButtonItem { Text = "Icon", Image = raster }; renderer.DrawItem(c, item, RibbonItemSize.Medium, true); Assert.Equal(1, record.Images); Assert.Equal(1, record.Items);
    });

    private static Rectangle Ink(Bitmap image, Color color)
    {
        var result = Rectangle.Empty; for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() == color.ToArgb()) result = result.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(result, new(x, y, 1, 1)); return result;
    }
    private static void Same(Bitmap a, Bitmap b) { for (int y = 0; y < a.Height; y++) for (int x = 0; x < a.Width; x++) Assert.Equal(a.GetPixel(x, y), b.GetPixel(x, y)); }
    private sealed class Record { internal Rectangle Text; internal RibbonTheme? Theme; internal int Items, Splits, Arrows, Images; internal readonly List<Rectangle> Focus = []; }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { record.Items++; base.DrawItem(c, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext arrow, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { record.Splits++; base.DrawSplitItem(primary, arrow, item, size, menu); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text = c.Bounds; record.Theme = c.Theme; }
        public override void DrawSubmenuArrow(RibbonPaintContext c, Color color) => record.Arrows++;
        public override void DrawFocus(RibbonPaintContext c) => record.Focus.Add(c.Bounds);
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) => record.Images++;
    }
    private sealed class Flat(Record record) : RibbonRenderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text = c.Bounds; record.Theme = c.Theme; }
        public override void DrawSubmenuArrow(RibbonPaintContext c, Color color) => record.Arrows++;
        public override void DrawFocus(RibbonPaintContext c) => record.Focus.Add(c.Bounds);
    }

    [Fact]
    public void Default_application_row_split_and_footer_states_match_saved_pixels_and_targets() => FoundationTests.Sta(() =>
    {
        foreach (bool classic in new[] { false, true })
        foreach (bool office in new[] { false, true })
        foreach (bool empty in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var form = new Form { Bounds = new(-16000, -16000, 1000, 700), ShowInTaskbar = false };
            using var ribbon = new RibbonControl { Renderer = painter, Theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { ApplicationMenuAppearance = empty ? new() : null }, AnimationLevel = RibbonAnimationLevel.None, FileMenuMode = RibbonFileMenuMode.CommandMenu };
            ribbon.FileMenu.Visible = true; var split = new RibbonSplitButtonItem { Text = "Split command" }; split.Items.Add(new RibbonButtonItem { Text = "Child" }); ribbon.FileMenu.Items.Add(split);
            form.Controls.Add(ribbon); form.Show(); Application.DoEvents();
            using var button = new RibbonButtonItem { Text = "Command" }; using var toggle = new RibbonToggleItem { Text = "Checked command", Checked = true };
            foreach (int dpi in new[] { 96, 120, 144, 192 })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.Density = density; int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
                using var font = new Font("Segoe UI", 9 * dpi / 96f);
                using var image = new Bitmap(S(870), S(540)); using var g = Graphics.FromImage(image); g.Clear(ribbon.Theme.PopupColor); var geometry = new List<string>();
                for (int state = 0; state < 10; state++)
                {
                    split.Enabled = state != 4; split.Checked = state == 6;
                    if (state is 5 or 8) { if (!ribbon.FileMenu.IsDropDownOpen) ribbon.FileMenu.ShowDropDown(); split.ShowDropDown(); Assert.True(split.IsDropDownOpen); }
                    else split.CloseDropDown();
                    int column = 0;
                    foreach (var part in new[] { RibbonApplicationMenuPart.Navigation, RibbonApplicationMenuPart.Pane, RibbonApplicationMenuPart.Footer })
                    {
                        var bounds = new Rectangle(S(8 + column++ * 286), S(8 + state * 52), S(278), S(46));
                        var theme = state == 9 ? ribbon.Theme with { PopupBorder = Color.Transparent } : ribbon.Theme;
                        var c = new RibbonPaintContext(g, bounds, theme, font, dpi, Hot: state == 1, Pressed: state is 2 or 4) { Density = density, ApplicationMenuPart = part };
                        if (state < 4 || state == 9) painter.DrawItem(c, state is 3 or 9 ? toggle : button, RibbonItemSize.Medium, true);
                        else
                        {
                            var targets = RibbonApplicationMenuMetrics.Expand(new(TargetKind.Item, split, bounds, bounds), dpi, density).ToArray();
                            painter.DrawSplitItem(c with { Bounds = targets[0].Bounds, Pressed = state is 4 or 8 }, c with { Bounds = targets[1].Bounds, Hot = state == 7 }, split, RibbonItemSize.Medium, true);
                            geometry.Add($"{part}/{state}:{targets[0].Bounds};{targets[1].Bounds}");
                        }
                    }
                }
                string name = $"application-chrome-{classic}-{office}-{dpi}-{density}", data = string.Join(Environment.NewLine, geometry);
                using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); var png = stream.ToArray();
                if (Environment.GetEnvironmentVariable("RIBBONKIT_APPLICATION_CHROME_CAPTURE") is { Length: > 0 } capture)
                { Directory.CreateDirectory(capture); File.WriteAllBytes(Path.Combine(capture, name + ".png"), png); File.WriteAllText(Path.Combine(capture, name + ".txt"), data); }
                if (Environment.GetEnvironmentVariable("RIBBONKIT_APPLICATION_CHROME_BASELINE") is { Length: > 0 } baseline)
                { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), data); }
            }
            form.Close();
        }
    });
}
