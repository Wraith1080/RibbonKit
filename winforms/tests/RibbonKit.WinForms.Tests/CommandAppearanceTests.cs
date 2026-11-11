using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class CommandAppearanceTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Command_outline_radius_inset_and_hide_settings_preserve_content_and_disabled_policy(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            using var painter = new Hooks(office); using var image = new Bitmap(S(250), S(90)); using var graphics = Graphics.FromImage(image);
            var settings = new RibbonCommandAppearanceSettings { Inset = 4, BorderWidth = 3, BorderColor = Color.DarkRed, CornerRadius = 0 };
            var theme = RibbonThemes.Office2024Light with { CommandAppearance = settings, Hover = new(Color.LightBlue) };
            using var item = new RibbonButtonItem { Text = "Command" }; var bounds = new Rectangle(S(8), S(8), S(220), S(60));
            var context = new RibbonPaintContext(graphics, bounds, theme, Control.DefaultFont, dpi, Hot: true);
            graphics.Clear(Color.White); painter.DrawItem(context, item, RibbonItemSize.Medium, false);
            Assert.Equal(Color.DarkRed.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + bounds.Height / 2).ToArgb());
            Assert.Equal(Color.LightBlue.ToArgb(), image.GetPixel(bounds.Left + S(7), bounds.Top + bounds.Height / 2).ToArgb());
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Left, bounds.Top).ToArgb());
            graphics.Clear(Color.White); painter.DrawItem(context with { Theme = theme with { CommandAppearance = settings with { CornerRadius = 12 } } }, item, RibbonItemSize.Medium, false);
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + S(4)).ToArgb());
            graphics.Clear(Color.White); painter.DrawItem(context with { Theme = theme with { CommandAppearance = settings with { BorderWidth = 0 } } }, item, RibbonItemSize.Medium, false);
            Assert.Equal(Color.LightBlue.ToArgb(), image.GetPixel(bounds.Left + S(4), bounds.Top + bounds.Height / 2).ToArgb());
            graphics.Clear(Color.White); painter.DrawItem(context with { Theme = theme with { CommandAppearance = settings with { Inset = 1024 } } }, item, RibbonItemSize.Medium, false);
            Assert.True(bounds.Contains(Ink(image, color => color.ToArgb() != Color.White.ToArgb())));
            item.Enabled = false; graphics.Clear(Color.White); painter.ChromeCalls = 0;
            painter.DrawItem(context with { Pressed = true, Focused = true }, item, RibbonItemSize.Medium, false);
            Assert.Equal(0, painter.ChromeCalls); Assert.True(Ink(image, color => color.ToArgb() != Color.White.ToArgb()).IsEmpty);
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Split_seams_and_explicit_companion_alpha_work_in_root_menu_and_material_qat(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool qat in new[] { false, true })
        foreach (bool material in new[] { false, true })
        foreach (bool vertical in qat ? new[] { false } : new[] { false, true })
        {
            using var painter = new Hooks(true); using var split = new RibbonSplitButtonItem { Layout = vertical ? RibbonSplitButtonLayout.Vertical : RibbonSplitButtonLayout.Horizontal };
            using var reference = new RibbonQuickAccessDropDownReference { Source = split };
            using var owner = new RibbonControl();
            if (qat)
            { var tab = new RibbonTab(); var group = new RibbonGroup(); group.Items.Add(split); tab.Groups.Add(group); owner.Tabs.Add(tab); owner.QuickAccessItems.Add(reference); Assert.True(RibbonQuickAccess.Available(reference)); }
            var companion = Color.FromArgb(128, 30, 110, 220);
            var settings = new RibbonCommandAppearanceSettings { CornerRadius = 0, Inset = 0, BorderWidth = 0, SplitSeparatorWidth = 3, SplitSeparatorInset = 5,
                SplitSeparatorColor = Color.Magenta, SplitCompanionBackground = new(companion) };
            var theme = RibbonThemes.Office2024Light with { CommandAppearance = settings, Hover = new(Color.LightGreen), BackdropHover = new(Color.Red) };
            using var image = new Bitmap(S(250), S(140)); using var g = Graphics.FromImage(image);
            var whole = new Rectangle(S(10), S(10), S(220), S(110));
            var primary = vertical ? new Rectangle(whole.Left, whole.Top, whole.Width, S(70)) : new Rectangle(whole.Left, whole.Top, S(150), whole.Height);
            var arrow = vertical ? Rectangle.FromLTRB(whole.Left, primary.Bottom, whole.Right, whole.Bottom) : Rectangle.FromLTRB(primary.Right, whole.Top, whole.Right, whole.Bottom);
            void Draw(RibbonCommandAppearanceSettings? value, bool menu = false)
            {
                g.Clear(Color.White);
                var context = new RibbonPaintContext(g, primary, theme with { CommandAppearance = value }, Control.DefaultFont, dpi, Hot: true) { CompositedText = material, WindowBackdrop = material };
                var drop = context with { Bounds = arrow, Hot = false };
                if (qat) painter.DrawQuickAccessSplitItem(context, drop, reference, menu); else painter.DrawSplitItem(context, drop, split, RibbonItemSize.Large, false);
            }
            Draw(settings);
            var expectedSeam = vertical ? new Rectangle(whole.Left + S(5), arrow.Top, whole.Width - 2 * S(5), S(3)) : new Rectangle(arrow.Left, whole.Top + S(5), S(3), whole.Height - 2 * S(5));
            Assert.Equal(expectedSeam, Ink(image, color => color.ToArgb() == Color.Magenta.ToArgb()));
            var point = new Point(arrow.Left + arrow.Width / 2, arrow.Top + arrow.Height / 2);
            Assert.Equal(Blend(Color.White, companion), image.GetPixel(point.X, point.Y));
            var activeInk = qat && material ? Blend(Color.White, Color.FromArgb((int)Math.Round(255 * theme.QuickAccessBackdropOpacity), Color.Red)) : Color.LightGreen;
            Assert.Equal(activeInk.ToArgb(), image.GetPixel(primary.Left + primary.Width / 2, primary.Top + primary.Height / 2).ToArgb());
            Draw(settings with { SplitSeparatorWidth = 0 }); Assert.True(Ink(image, color => color.ToArgb() == Color.Magenta.ToArgb()).IsEmpty);
            Draw(settings with { SplitCompanionBackground = null }); Assert.Equal(Color.White.ToArgb(), image.GetPixel(point.X, point.Y).ToArgb());
            Draw(null); Assert.Equal(Color.White.ToArgb(), image.GetPixel(point.X, point.Y).ToArgb());
            Assert.Equal(activeInk.ToArgb(), image.GetPixel(primary.Left + primary.Width / 2, primary.Top + primary.Height / 2).ToArgb());
            if (qat) { Draw(settings, menu: true); Assert.Equal(Blend(Color.White, companion), image.GetPixel(point.X, point.Y)); }
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Authored_command_paint_keeps_native_geometry_targets_models_and_editor_state(bool classic) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Bounds = new(-16000, -16000, 1100, 700), ShowInTaskbar = false };
        using var ribbon = new RibbonControl { Theme = classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light };
        var tab = new RibbonTab(); var group = new RibbonGroup { CanResize = false };
        var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained commands", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var split = new RibbonSplitButtonItem { Text = "Split", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; split.Items.Add(new RibbonButtonItem { Text = "Child" }); group.Items.AddRange(field, split); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var reference = new RibbonQuickAccessDropDownReference { Source = split }; ribbon.QuickAccessItems.Add(reference); form.Controls.Add(ribbon); form.Show(); Application.DoEvents();
        var editor = Assert.IsAssignableFrom<TextBoxBase>(ribbon.Editors.EditorFor(field)); nint handle = editor.Handle; editor.Select(2, 5); var parent = editor.Parent;
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.LayoutMode = mode; ribbon.Density = density; var original = ribbon.Theme; var before = ribbon.Snapshot;
            ribbon.Theme = original with { CommandAppearance = new() { CornerRadius = 12, BorderWidth = 3, BorderColor = Color.Blue, Inset = 4, SplitSeparatorWidth = 3, SplitSeparatorInset = 5, SplitCompanionBackground = new(Color.LightBlue) } };
            Assert.Equal(before.Targets.Select(target => (target.Bounds, target.Clip)).ToArray(), ribbon.Snapshot.Targets.Select(target => (target.Bounds, target.Clip)).ToArray());
            Assert.Equal(before.BodyViewport, ribbon.Snapshot.BodyViewport); Assert.Equal(before.BodyExtent, ribbon.Snapshot.BodyExtent);
            Assert.Equal(handle, editor.Handle); Assert.Same(parent, editor.Parent); Assert.Equal("Retained commands", editor.Text); Assert.Equal(2, editor.SelectionStart); Assert.Equal(5, editor.SelectionLength); Assert.Same(split, reference.Source);
            ribbon.Theme = original;
        }
    });

    [Fact]
    public void Actual_chrome_text_focus_hooks_and_immutable_group_reset_keep_precedence() => FoundationTests.Sta(() =>
    {
        using var painter = new Hooks(true) { ReplaceChrome = true }; using var ribbon = new RibbonControl(); using var other = new RibbonControl();
        var settings = new RibbonCommandAppearanceSettings { CornerRadius = 12, Inset = 4, BorderWidth = 3, BorderColor = Color.DarkBlue, SplitSeparatorWidth = 3 };
        var application = ribbon.Theme with { CommandAppearance = settings, BodyAppearance = new() { BorderWidth = 2 } }; ribbon.Theme = application;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = null; Assert.Same(application, ribbon.Theme); Assert.Null(other.Theme.CommandAppearance);
        using var image = new Bitmap(200, 70); using var g = Graphics.FromImage(image); using var item = new RibbonButtonItem { Text = "Command" };
        painter.DrawItem(new(g, new(5, 5, 180, 55), application, ribbon.Font, 120, Hot: true, Focused: true), item, RibbonItemSize.Medium, false);
        Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(20, 30).ToArgb()); Assert.Equal(1, painter.ChromeCalls); Assert.Equal(1, painter.FocusCalls); Assert.Same(application, painter.TextTheme);
        ribbon.Theme = application with { CommandAppearance = settings with { Inset = null } }; Assert.Equal(3, ribbon.Theme.CommandAppearance!.SplitSeparatorWidth); Assert.Equal(4, settings.Inset);
        ribbon.Theme = application with { CommandAppearance = null }; Assert.Equal(2, ribbon.Theme.BodyAppearance!.BorderWidth);
    });

    [Theory] [InlineData("CornerRadius", 64)] [InlineData("BorderWidth", 32)] [InlineData("Inset", 1024)]
    [InlineData("SplitSeparatorWidth", 32)] [InlineData("SplitSeparatorInset", 1024)]
    public void Invalid_command_roles_leave_theme_and_builtin_selection_unchanged(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme;
        var property = typeof(RibbonCommandAppearanceSettings).GetProperty(name)!;
        foreach (int value in new[] { -1, maximum + 1 })
        {
            var settings = new RibbonCommandAppearanceSettings(); property.SetValue(settings, value);
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { CommandAppearance = settings }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme);
        }
        foreach (int? value in new int?[] { 0, maximum, null })
        { var settings = new RibbonCommandAppearanceSettings(); property.SetValue(settings, value); ribbon.Theme = original with { CommandAppearance = settings }; Assert.Same(settings, ribbon.Theme.CommandAppearance); }
    });

    private static Color Blend(Color background, Color color)
    {
        using var image = new Bitmap(1, 1); using var graphics = Graphics.FromImage(image); graphics.Clear(background); using var brush = new SolidBrush(color); graphics.FillRectangle(brush, 0, 0, 1, 1); return image.GetPixel(0, 0);
    }
    private static Rectangle Ink(Bitmap image, Func<Color, bool> match)
    {
        var result = Rectangle.Empty;
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (match(image.GetPixel(x, y))) result = result.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(result, new(x, y, 1, 1));
        return result;
    }
    private sealed class Hooks(bool office) : RibbonOffice2024Renderer
    {
        internal bool ReplaceChrome;
        internal int ChromeCalls, FocusCalls;
        internal RibbonTheme? TextTheme;
        protected override void DrawChrome(RibbonPaintContext context, RibbonFill fill, Color border)
        {
            ChromeCalls++;
            if (ReplaceChrome) { using var brush = new SolidBrush(Color.Magenta); context.Graphics.FillRectangle(brush, context.Bounds); }
            else if (office) base.DrawChrome(context, fill, border);
            else { if (!TryFlat(context, fill, border)) throw new InvalidOperationException("Flat test path failed."); }
        }
        private bool TryFlat(RibbonPaintContext context, RibbonFill fill, Color border)
        {
            // Exercise the actual flat public entry with the same candidate theme.
            using var flat = new RibbonRenderer();
            if (context.IsCommandState) { flat.PaintCommandState(context, fill, border); return true; }
            return false;
        }
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) => TextTheme = context.Theme;
        public override void DrawFocus(RibbonPaintContext context) => FocusCalls++;
        public override void DrawDropDownArrow(RibbonPaintContext context, Color color) { }
        public override void DrawSubmenuArrow(RibbonPaintContext context, Color color) { }
    }

    [Fact]
    public void Default_command_states_and_split_perimeters_match_saved_images() => FoundationTests.Sta(() =>
    {
        foreach (bool classic in new[] { false, true })
        foreach (bool office in new[] { false, true })
        foreach (bool composited in new[] { false, true })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (bool emptyGroup in new[] { false, true })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            var theme = (classic ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light) with { CommandAppearance = emptyGroup ? new() : null };
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(900), S(590)); using var graphics = Graphics.FromImage(image); graphics.Clear(theme.Surface);
            using var font = new Font("Segoe UI", 9 * dpi / 96f); var geometry = new List<string>();
            for (int state = 0; state < 8; state++)
            {
                using var command = new RibbonToggleItem { Text = "Command", Checked = state == 3, Enabled = state != 4 };
                var context = new RibbonPaintContext(graphics, new(S(10), S(8 + state * 72), S(125), S(54)), theme, font, dpi, Hot: state is 1 or 4, Pressed: state == 2, Focused: state == 5)
                    { CompositedText = composited, WindowBackdrop = composited };
                painter.DrawItem(context, command, RibbonItemSize.Medium, false);
                using var split = new RibbonSplitButtonItem { Text = "Split command", Checked = state == 3, Enabled = state != 4 };
                foreach (bool vertical in new[] { false, true })
                {
                    split.Layout = vertical ? RibbonSplitButtonLayout.Vertical : RibbonSplitButtonLayout.Horizontal;
                    var bounds = new Rectangle(S(vertical ? 340 : 155), context.Bounds.Top, S(165), S(62));
                    var (primary, arrow) = RibbonSplitLayout.Calculate(bounds, split, RibbonItemSize.Large, false, dpi);
                    painter.DrawSplitItem(context with { Bounds = primary, Hot = state is 1 or 4, Pressed = state == 2 }, context with { Bounds = arrow, Hot = state == 6, Pressed = state == 7 }, split, RibbonItemSize.Large, false);
                    geometry.Add($"{state}/{vertical}:{primary}/{arrow}");
                }
                using var reference = new RibbonQuickAccessDropDownReference { Source = split };
                painter.DrawQuickAccessSplitItem(context with { Bounds = new(S(530), context.Bounds.Top, S(100), S(54)) }, context with { Bounds = new(S(630), context.Bounds.Top, S(25), S(54)), Hot = state == 6, Pressed = state == 7 }, reference, false);
                painter.DrawItem(context with { Bounds = new(S(680), context.Bounds.Top, S(200), S(54)), ReserveIcon = true }, command, RibbonItemSize.Medium, true);
            }
            Compare($"command-{classic}-{office}-{composited}-{dpi}", image, string.Join(Environment.NewLine, geometry));
        }
    });
    private static void Compare(string name, Bitmap image, string geometry)
    {
        using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); var png = stream.ToArray();
        if (Environment.GetEnvironmentVariable("RIBBONKIT_COMMAND_APPEARANCE_CAPTURE") is { Length: > 0 } output)
        { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), png); File.WriteAllText(Path.Combine(output, name + ".txt"), geometry); }
        if (Environment.GetEnvironmentVariable("RIBBONKIT_COMMAND_APPEARANCE_BASELINE") is { Length: > 0 } baseline)
        { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), png); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), geometry); }
    }
}
