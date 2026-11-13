using System.Drawing.Imaging;
using System.Reflection;
using System.Text.Json;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class OptionsShellAppearanceTests
{
    private static RibbonOptionsAppearanceSettings Authored => new() { ButtonInset = 3, ButtonBorderWidth = 2, ButtonBorderColor = Color.Navy, ButtonFocusInset = 6, ButtonBackground = new(Color.LightYellow), AccentButtonBackground = new(Color.LightGreen), HeadingFontSizeIncrement = 8, HeadingMinimumFontSize = 20, HeadingBold = false, HeadingBottomPadding = 8 };
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Button_shapes_idle_fills_state_eligibility_alpha_hide_and_four_corners_keep_content_targets(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => (int)Math.Ceiling(n * dpi / 96d); var theme = RibbonThemes.Office2019Light with { OptionsAppearance = Authored, OptionsButtonCornerRadius = 10, Surface = Color.White, Hover = new(Color.LightBlue), Checked = new(Color.Pink), Pressed = new(Color.Orange) };
        using var painter = new RoundedPainter(); using var button = new Button { Text = "", Size = new(S(190), S(70)) }; var bounds = new Rectangle(S(5), S(5), button.Width, button.Height); using var image = new Bitmap(S(210), S(90)); using var g = Graphics.FromImage(image); var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi); var metrics = new RibbonOptionsShellMetrics(theme, dpi); var paint = metrics.ButtonBounds(bounds);
        foreach (bool accent in new[] { false, true })
        {
            g.Clear(Color.White); Draw(theme, accent); Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(paint.Left, paint.Top + paint.Height / 2).ToArgb()); Assert.Equal((accent ? Color.LightGreen : Color.LightYellow).ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2).ToArgb());
            for (int y = 0; y < S(10); y++) for (int x = 0; x < S(10); x++) { var pixel = image.GetPixel(paint.Left + x, paint.Top + y); Assert.Equal(pixel, image.GetPixel(paint.Right - 1 - x, paint.Top + y)); Assert.Equal(pixel, image.GetPixel(paint.Left + x, paint.Bottom - 1 - y)); Assert.Equal(pixel, image.GetPixel(paint.Right - 1 - x, paint.Bottom - 1 - y)); }
            foreach (var state in new[] { (Hot: true, Pressed: false, Enabled: true, Color: accent ? Color.Pink : Color.LightBlue), (Hot: true, Pressed: true, Enabled: true, Color: Color.Orange), (Hot: true, Pressed: true, Enabled: false, Color: accent ? Color.LightBlue : Color.White) })
            { button.Enabled = state.Enabled; g.Clear(Color.White); Draw(theme, accent, state.Hot, state.Pressed); Assert.Equal(state.Color.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2).ToArgb()); }
            button.Enabled = true;
            foreach (var hidden in new[] { Authored with { ButtonBorderWidth = 0 }, Authored with { ButtonBorderColor = Color.Transparent } }) { g.Clear(Color.White); Draw(theme with { OptionsAppearance = hidden }, accent); Assert.False(HasColor(image, Color.Navy)); }
            g.Clear(Color.White); Draw(theme with { OptionsAppearance = Authored with { ButtonBorderColor = Color.FromArgb(128, Color.Blue), ButtonBackground = new(Color.White), AccentButtonBackground = new(Color.White) } }, accent); var alpha = image.GetPixel(paint.Left, paint.Top + paint.Height / 2); Assert.Equal(255, alpha.B); Assert.InRange(alpha.R, 1, 254);
            g.Clear(Color.White); Draw(theme with { OptionsAppearance = Authored with { ButtonBorderWidth = 0, ButtonBackground = new(Color.FromArgb(128, Color.Blue)), AccentButtonBackground = new(Color.FromArgb(128, Color.Blue)) } }, accent); alpha = image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2); Assert.Equal(255, alpha.B); Assert.InRange(alpha.R, 1, 254);
        }
        g.Clear(Color.White); Draw(theme with { OptionsAppearance = Authored with { ButtonInset = 1024, ButtonBorderWidth = 32 } }, false); for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) Assert.True(bounds.Contains(x, y));
        g.Clear(Color.White); Draw(theme with { OptionsAppearance = Authored with { ButtonInset = 0, ButtonBorderWidth = 0, ButtonBackground = new(Color.White) } }, false, focus: true); Assert.False(AllWhite(image)); var focus = metrics.ButtonFocusBounds(bounds); for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) Assert.True(focus.Contains(x, y));
        Assert.Equal(Rectangle.Inflate(bounds, -4, -4), new RibbonOptionsShellMetrics(theme with { OptionsAppearance = null }, dpi).ButtonFocusBounds(bounds));
        void Draw(RibbonTheme palette, bool accent, bool hot = false, bool pressed = false, bool focus = false) => RibbonOptionsButtonRenderer.Draw(painter, c with { Theme = palette }, button, accent, hot, pressed, true, focus, 0, Color.White);
    });
    [Theory] [InlineData("ButtonInset", 1024)] [InlineData("ButtonBorderWidth", 32)] [InlineData("ButtonFocusInset", 1024)] [InlineData("HeadingBottomPadding", 1024)]
    public void Shell_dimensions_validate_before_mutation_and_support_endpoints(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var p = typeof(RibbonOptionsAppearanceSettings).GetProperty(name)!;
        foreach (int n in new[] { -1, maximum + 1 }) { var a = new RibbonOptionsAppearanceSettings(); p.SetValue(a, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { OptionsAppearance = a }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (int? n in new int?[] { 0, maximum, null }) { var a = new RibbonOptionsAppearanceSettings(); p.SetValue(a, n); ribbon.Theme = original with { OptionsAppearance = a }; Assert.Same(a, ribbon.Theme.OptionsAppearance); }
    });
    [Theory] [InlineData("HeadingFontSizeIncrement", 0f, 64f)] [InlineData("HeadingMinimumFontSize", 1f, 128f)]
    public void Heading_sizes_reject_nonfinite_values_atomically_and_preserve_other_host_styles(string name, float minimum, float maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var p = typeof(RibbonOptionsAppearanceSettings).GetProperty(name)!;
        foreach (float n in new[] { minimum - 1, maximum + 1, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) { var a = new RibbonOptionsAppearanceSettings(); p.SetValue(a, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { OptionsAppearance = a }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (float? n in new float?[] { minimum, maximum, null }) { var a = new RibbonOptionsAppearanceSettings(); p.SetValue(a, n); ribbon.Theme = original with { OptionsAppearance = a }; }
        foreach (int points in new[] { 9, 14, 36 })
        {
            using var font = new Font("Segoe UI", points, FontStyle.Italic | FontStyle.Bold); var metrics = new RibbonOptionsShellMetrics(original with { OptionsAppearance = Authored }, 120); Assert.Equal(Math.Max(points + 8, 20), metrics.HeadingPoints(font)); Assert.Equal(FontStyle.Italic, metrics.HeadingStyle(font)); Assert.Equal(FontStyle.Italic | FontStyle.Bold, new RibbonOptionsShellMetrics(original with { OptionsAppearance = Authored with { HeadingBold = true } }, 120).HeadingStyle(font));
            metrics = new(original, 120); Assert.Equal(Math.Max(points + 5, 14), metrics.HeadingPoints(font)); Assert.Equal(FontStyle.Bold, metrics.HeadingStyle(font));
        }
    });
    [Fact]
    public void Scoped_button_opt_in_and_member_group_resets_preserve_navigation_existing_roles_and_borrowed_images() => FoundationTests.Sta(() =>
    {
        using var asset = new Bitmap(20, 12); using (var ag = Graphics.FromImage(asset)) ag.Clear(Color.DarkRed); using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2024Light };
        using var button = new RibbonOptionsButton(ribbon) { Size = new(150, 48), Text = "Apply", Image = asset }; using var accent = new RibbonOptionsButton(ribbon, true) { Size = button.Size, Text = "OK" }; var bounds = button.Bounds; nint handle = button.Handle;
        byte[] original = Pixels(button); ribbon.Theme = ribbon.Theme with { OptionsAppearance = new() { NavigationCornerRadius = 10, DividerWidth = 3, HeadingBold = false, AccentButtonBackground = new(Color.LightGreen) } }; Assert.Equal(original, Pixels(button));
        ribbon.Theme = ribbon.Theme with { OptionsAppearance = ribbon.Theme.OptionsAppearance! with { ButtonBorderWidth = 2, ButtonBorderColor = Color.Navy } }; Assert.NotEqual(original, Pixels(button)); Assert.Equal(handle, button.Handle); Assert.Equal(bounds, button.Bounds); Assert.Same(asset, button.Image);
        var a = ribbon.Theme.OptionsAppearance!; ribbon.Theme = ribbon.Theme with { OptionsAppearance = a with { ButtonBorderWidth = null, ButtonBorderColor = Color.Empty } }; Assert.Equal(original, Pixels(button)); Assert.Equal(3, ribbon.Theme.OptionsAppearance!.DividerWidth); Assert.False(ribbon.Theme.OptionsAppearance.HeadingBold);
        ribbon.Theme = ribbon.Theme with { OptionsAppearance = a with { HeadingBold = null } }; Assert.Equal(2, ribbon.Theme.OptionsAppearance!.ButtonBorderWidth); ribbon.Theme = ribbon.Theme with { OptionsAppearance = null }; Assert.Equal(original, Pixels(button)); Assert.Null(ribbon.Theme.OptionsButtonCornerRadius); Assert.Equal(Color.DarkRed.ToArgb(), asset.GetPixel(0, 0).ToArgb());
        static byte[] Pixels(Control c) { using var image = new Bitmap(c.Width, c.Height); c.DrawToBitmap(image, new(Point.Empty, image.Size)); using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); return stream.ToArray(); }
    });
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Actual_heading_button_native_owners_and_apply_cancel_follow_existing_layout_lifetime(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer(); using var ribbon = new RibbonControl { Renderer = renderer, Theme = RibbonThemes.Office2019Light with { OptionsAppearance = Authored with { NavigationCornerRadius = 12, DividerWidth = 3 } } }; using var dialog = new RibbonOptionsDialog(ribbon) { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false };
        var page = new RibbonOptionsPage { Text = "General options" }; var editor = new TextBox { Text = "Retained native draft" }; page.Controls.Add(editor); dialog.Pages.Add(page); bool valid = false; int applied = 0, cancelled = 0; page.Applying += (_, e) => e.Cancel = !valid; page.Applied += (_, _) => applied++; page.Cancelled += (_, _) => cancelled++;
        dialog.Show(); Application.DoEvents(); var heading = Descendants(dialog).OfType<Label>().Single(c => c.Text == page.Text); var metrics = new RibbonOptionsShellMetrics(ribbon.Theme, dialog.DeviceDpi); Assert.Equal(metrics.HeadingPoints(dialog.Font), heading.Font.SizeInPoints); Assert.Equal(metrics.HeadingStyle(dialog.Font), heading.Font.Style); Assert.Equal(metrics.HeadingPadding, heading.Padding.Bottom);
        var buttons = Descendants(dialog).OfType<Button>().ToDictionary(c => c.Name, c => (Control: c, Handle: c.Handle, Bounds: c.Bounds)); nint handle = editor.Handle; var parent = editor.Parent; editor.Select(2, 4); using (var image = new Bitmap(heading.Width, heading.Height)) heading.DrawToBitmap(image, new(Point.Empty, image.Size));
        ribbon.Theme = ribbon.Theme with { OptionsAppearance = ribbon.Theme.OptionsAppearance! with { ButtonInset = null, ButtonBorderWidth = null, ButtonBorderColor = Color.Empty, ButtonFocusInset = null, ButtonBackground = null, AccentButtonBackground = null } };
        foreach (var value in buttons.Values) { using var image = new Bitmap(value.Control.Width, value.Control.Height); value.Control.DrawToBitmap(image, new(Point.Empty, image.Size)); Assert.Equal(value.Handle, value.Control.Handle); Assert.Equal(value.Bounds, value.Control.Bounds); }
        Assert.Equal(handle, editor.Handle); Assert.Same(parent, editor.Parent); Assert.Equal(2, editor.SelectionStart); Assert.Equal(4, editor.SelectionLength); Assert.Equal(12, ribbon.Theme.OptionsAppearance!.NavigationCornerRadius);
        Assert.Same(buttons["AcceptCustomization"].Control, dialog.AcceptButton); Assert.Same(buttons["CancelCustomization"].Control, dialog.CancelButton); buttons["ApplyCustomization"].Control.PerformClick(); Assert.Equal(0, applied); valid = true; buttons["ApplyCustomization"].Control.PerformClick(); Assert.Equal(1, applied); valid = false; buttons["AcceptCustomization"].Control.PerformClick(); Assert.True(dialog.Visible); buttons["CancelCustomization"].Control.PerformClick(); Assert.Equal(1, cancelled);
    });
    private static bool HasColor(Bitmap image, Color color) => Enumerable.Range(0, image.Height).Any(y => Enumerable.Range(0, image.Width).Any(x => image.GetPixel(x, y).ToArgb() == color.ToArgb()));
    private static bool AllWhite(Bitmap image) => !Enumerable.Range(0, image.Height).Any(y => Enumerable.Range(0, image.Width).Any(x => image.GetPixel(x, y).ToArgb() != Color.White.ToArgb()));
    [Fact]
    public void Default_native_button_states_images_heading_fonts_and_geometry_match_saved_results() => FoundationTests.Sta(() =>
    {
        foreach (var original in new[] { RibbonThemes.Office2019Light, RibbonThemes.Office2024Light })
        foreach (bool office in new[] { false, true })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (int points in new[] { 9, 14 })
        {
            var theme = original with { OptionsAppearance = Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_SHELL_EMPTY") == "1" ? new() : null };
            using var asset = new Bitmap(20, 12); using (var ag = Graphics.FromImage(asset)) ag.Clear(Color.DarkRed);
            using var font = new Font("Segoe UI", points, points == 14 ? FontStyle.Italic : FontStyle.Regular); using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var ribbon = new RibbonControl { Theme = theme, Renderer = renderer, Font = font, Density = density };
            using var dialog = new RibbonOptionsDialog(ribbon) { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false };
            var page = new RibbonOptionsPage { Text = "General options", BackColor = theme.Surface }; page.Controls.Add(new TextBox { Text = "Retained draft" }); dialog.Pages.Add(page); dialog.Show(); Application.DoEvents();
            int S(int n) => (int)Math.Ceiling(n * dialog.DeviceDpi / 96d); int w = S(145), h = S(48); var heading = Descendants(dialog).OfType<Label>().Single(c => c.Text == page.Text);
            using var sheet = new Bitmap(Math.Max(4 * w, heading.Width), 2 * h + heading.Height); using var canvas = Graphics.FromImage(sheet); canvas.Clear(theme.Surface);
            for (int accent = 0; accent < 2; accent++) for (int state = 0; state < 4; state++)
            {
                using var button = new RibbonOptionsButton(ribbon, accent != 0) { Text = "&Apply", Size = new(w, h), Font = dialog.Font, Padding = new(4), Image = state >= 2 ? asset : null, ImageAlign = ContentAlignment.MiddleLeft, TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight };
                page.Controls.Add(button); button.CreateControl(); if (state == 1) Invoke(button, "OnMouseEnter", EventArgs.Empty); if (state == 2) Invoke(button, "OnKeyDown", new KeyEventArgs(Keys.Space)); if (state == 3) button.Enabled = false;
                using var tile = new Bitmap(w, h); using (var g = Graphics.FromImage(tile)) { g.Clear(theme.Surface); Invoke(button, "OnPaint", new PaintEventArgs(g, button.ClientRectangle)); } canvas.DrawImageUnscaled(tile, state * w, accent * h);
            }
            using (var tile = new Bitmap(heading.Width, heading.Height)) { heading.DrawToBitmap(tile, new(Point.Empty, tile.Size)); canvas.DrawImageUnscaled(tile, 0, 2 * h); }
            string geometry = JsonSerializer.Serialize(new { dialog.DeviceDpi, dialog.Size, dialog.MinimumSize, page.Bounds, Heading = heading.Bounds, HeadingPoints = heading.Font.SizeInPoints, HeadingStyle = heading.Font.Style, Controls = Descendants(dialog).Select(c => new { Type = c.GetType().Name, c.Name, c.Bounds }) });
            using var stream = new MemoryStream(); sheet.Save(stream, ImageFormat.Png); string name = $"options-shell-{theme.BackstageStyle}-{office}-{density}-{points}";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_SHELL_CAPTURE") is { Length: > 0 } output) { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), stream.ToArray()); File.WriteAllText(Path.Combine(output, name + ".json"), geometry); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_SHELL_BASELINE") is { Length: > 0 } baseline) { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), stream.ToArray()); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".json")), geometry); }
            dialog.Close();
        }
    });
    private static void Invoke(Control control, string method, object args) => typeof(RibbonOptionsButton).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, [args]);
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
}
