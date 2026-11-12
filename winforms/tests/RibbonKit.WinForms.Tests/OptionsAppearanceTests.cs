using System.Drawing.Imaging;
using System.Reflection;
using System.Text.Json;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class OptionsAppearanceTests
{
    private static RibbonOptionsAppearanceSettings Authored => new() { NavigationCornerRadius = 10, NavigationSelectionInset = 3, NavigationTextPadding = 24, NavigationSelectedBackground = new(Color.LightYellow), NavigationMarkerWidth = 5, NavigationMarkerInset = 20, NavigationMarkerFill = new(Color.Magenta), NavigationFocusInset = 5, DividerWidth = 3, DividerColor = Color.Navy };
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Selected_shapes_marker_alpha_hide_focus_and_existing_radius_hooks_keep_native_text_contract(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => (int)Math.Ceiling(n * dpi / 96d);
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer owner = office ? new OfficeProbe() : new FlatProbe(); using var painter = new RoundedPainter(); using var image = new Bitmap(S(270), S(120)); using var g = Graphics.FromImage(image);
            var theme = RibbonThemes.Office2019Light with { OptionsAppearance = Authored, BackstageNavigationBackground = Color.White, NavigationMarkerCornerRadius = 0 }; var bounds = new Rectangle(S(5), S(5), S(250), S(100)); var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
            g.Clear(Color.White); RibbonOptionsRenderer.DrawNavigation(owner, painter, c, "", true, false); var m = new RibbonOptionsAppearanceMetrics(theme, dpi); var selected = m.SelectionBounds(bounds); var marker = m.MarkerBounds(selected);
            Assert.Equal(Color.LightYellow.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2).ToArgb()); Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(marker.Left, marker.Top + marker.Height / 2).ToArgb()); Assert.Equal(S(5), marker.Width); Assert.Equal(S(24), m.TextBounds(selected).Left - bounds.Left);
            for (int y = 0; y < S(10); y++) for (int x = 0; x < S(10); x++)
            {
                var pixel = image.GetPixel(selected.Left + x, selected.Top + y); Assert.Equal(pixel, image.GetPixel(selected.Right - 1 - x, selected.Top + y)); Assert.Equal(pixel, image.GetPixel(selected.Left + x, selected.Bottom - 1 - y)); Assert.Equal(pixel, image.GetPixel(selected.Right - 1 - x, selected.Bottom - 1 - y));
            }
            g.Clear(Color.White); RibbonOptionsRenderer.DrawNavigation(owner, painter, c, "", false, false); Assert.True(AllWhite(image));
            foreach (var hidden in new[] { Authored with { NavigationMarkerWidth = 0 }, Authored with { NavigationMarkerInset = 1024 }, Authored with { NavigationMarkerFill = new(Color.Transparent) } })
            { g.Clear(Color.White); RibbonOptionsRenderer.DrawNavigation(owner, painter, c with { Theme = theme with { OptionsAppearance = hidden } }, "", true, false); Assert.DoesNotContain(Enumerable.Range(0, image.Height), y => Enumerable.Range(0, image.Width).Any(x => image.GetPixel(x, y).ToArgb() == Color.Magenta.ToArgb())); }
            g.Clear(Color.White); RibbonOptionsRenderer.DrawNavigation(owner, painter, c with { Theme = theme with { OptionsAppearance = Authored with { NavigationSelectedBackground = new(Color.FromArgb(128, Color.Blue)), NavigationMarkerWidth = 0 } } }, "", true, false); var alpha = image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2); Assert.Equal(255, alpha.B); Assert.InRange(alpha.R, 1, 254);
            g.Clear(Color.White); RibbonOptionsRenderer.DrawNavigation(owner, painter, c with { Theme = theme with { OptionsAppearance = Authored with { NavigationSelectedBackground = new(Color.Transparent), NavigationMarkerWidth = 0 } } }, "", true, true); Assert.False(AllWhite(image));
            var focus = m.FocusBounds(bounds); for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) Assert.True(focus.Contains(x, y));
            // Existing native text painting stays direct; these probes reject new DrawText interception.
            RibbonOptionsRenderer.DrawNavigation(owner, painter, c with { Theme = theme with { OptionsAppearance = null } }, "Native options text", true, false);
            Assert.True(owner is OfficeProbe op ? op.Radii > 0 : ((FlatProbe)owner).Radii > 0);
        }
    });
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Dividers_keep_orientation_alpha_zero_hide_and_panel_clamps(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => (int)Math.Ceiling(n * dpi / 96d); var bounds = new Rectangle(S(5), S(5), S(180), S(80)); using var image = new Bitmap(S(200), S(100)); using var g = Graphics.FromImage(image);
        foreach (bool vertical in new[] { false, true })
        {
            var theme = RibbonThemes.Office2019Light with { OptionsAppearance = Authored }; g.Clear(Color.White); RibbonOptionsRenderer.DrawDivider(g, bounds, theme, dpi, vertical);
            int count = 0; for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() == Color.Navy.ToArgb()) { Assert.True(bounds.Contains(x, y)); count++; }
            Assert.Equal(S(3) * (vertical ? bounds.Height : bounds.Width), count);
            g.Clear(Color.White); RibbonOptionsRenderer.DrawDivider(g, bounds, theme with { OptionsAppearance = Authored with { DividerColor = Color.FromArgb(128, Color.Blue) } }, dpi, vertical); var pixel = image.GetPixel(vertical ? bounds.Right - 1 : bounds.Left + 4, vertical ? bounds.Top + 4 : bounds.Top); Assert.Equal(255, pixel.B); Assert.InRange(pixel.R, 1, 254);
            foreach (var hidden in new[] { Authored with { DividerWidth = 0 }, Authored with { DividerColor = Color.Transparent } }) { g.Clear(Color.White); RibbonOptionsRenderer.DrawDivider(g, bounds, theme with { OptionsAppearance = hidden }, dpi, vertical); Assert.True(AllWhite(image)); }
            var small = new Rectangle(2, 2, 3, 3); g.Clear(Color.White); RibbonOptionsRenderer.DrawDivider(g, small, theme with { OptionsAppearance = Authored with { DividerWidth = 32 } }, dpi, vertical);
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) Assert.True(small.Contains(x, y));
        }
    });
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Legacy_ceiling_rounding_and_authored_short_row_clamps_are_independent_of_target_geometry(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => (int)Math.Ceiling(n * dpi / 96d); var bounds = new Rectangle(13, 17, S(200), S(70)); var m = new RibbonOptionsAppearanceMetrics(RibbonThemes.Office2019Light, dpi); var selected = Rectangle.Inflate(bounds, 0, -S(2));
        Assert.Equal(selected, m.SelectionBounds(bounds)); Assert.Equal(Rectangle.Inflate(selected, -S(12), 0), m.TextBounds(selected)); Assert.Equal(new Rectangle(selected.Left, selected.Top + S(7), S(3), selected.Height - S(14)), m.MarkerBounds(selected)); Assert.Equal(Rectangle.Inflate(bounds, -1, -1), m.FocusBounds(bounds)); Assert.Equal(1, m.DividerWidth);
        foreach (var small in new[] { new Rectangle(13, 17, 0, 0), new Rectangle(13, 17, 1, 1), new Rectangle(13, 17, 11, 7), bounds })
        {
            var authored = new RibbonOptionsAppearanceMetrics(RibbonThemes.Office2019Light with { OptionsAppearance = Authored with { NavigationSelectionInset = 1024, NavigationTextPadding = 1024, NavigationFocusInset = 1024, NavigationMarkerInset = 1024, NavigationMarkerWidth = 64 } }, dpi); var row = authored.SelectionBounds(small); Assert.True(small.Contains(row)); Assert.True(row.Contains(authored.TextBounds(row))); Assert.True(small.Contains(authored.FocusBounds(small))); Assert.True(row.Contains(authored.MarkerBounds(row))); Assert.Equal(0, authored.MarkerBounds(row).Height);
        }
    });
    [Theory] [InlineData("NavigationCornerRadius", 64)] [InlineData("NavigationSelectionInset", 1024)] [InlineData("NavigationTextPadding", 1024)] [InlineData("NavigationMarkerWidth", 64)] [InlineData("NavigationMarkerInset", 1024)] [InlineData("NavigationFocusInset", 1024)] [InlineData("DividerWidth", 32)]
    public void Dimensions_validate_atomically_and_allow_zero_maximum_and_null(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var p = typeof(RibbonOptionsAppearanceSettings).GetProperty(name)!;
        foreach (int n in new[] { -1, maximum + 1 }) { var a = new RibbonOptionsAppearanceSettings(); p.SetValue(a, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { OptionsAppearance = a }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (int? n in new int?[] { 0, maximum, null }) { var a = new RibbonOptionsAppearanceSettings(); p.SetValue(a, n); ribbon.Theme = original with { OptionsAppearance = a }; Assert.Same(a, ribbon.Theme.OptionsAppearance); }
    });
    [Fact]
    public void Member_group_builtin_resets_preserve_existing_roles_and_other_ribbons() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); var theme = ribbon.Theme with { OptionsAppearance = Authored, OptionsButtonCornerRadius = 4, NavigationMarkerCornerRadius = 3, TipAppearance = new() { KeyTipBorderWidth = 3 } }; ribbon.Theme = theme;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; ribbon.BuiltInTheme = null; Assert.Same(theme, ribbon.Theme);
        ribbon.Theme = theme with { OptionsAppearance = Authored with { NavigationMarkerWidth = null } }; Assert.Equal(3, ribbon.Theme.OptionsAppearance!.DividerWidth); Assert.Equal(5, Authored.NavigationMarkerWidth);
        ribbon.Theme = theme with { OptionsAppearance = null }; Assert.Equal(4, ribbon.Theme.OptionsButtonCornerRadius); Assert.Equal(3, ribbon.Theme.NavigationMarkerCornerRadius); Assert.Equal(3, ribbon.Theme.TipAppearance!.KeyTipBorderWidth); Assert.Null(other.Theme.OptionsAppearance);
    });
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Actual_navigation_callbacks_keep_native_handles_geometry_selection_validation_and_checkpoints(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer(); using var ribbon = new RibbonControl { Renderer = painter }; using var dialog = new RibbonOptionsDialog(ribbon) { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false };
        var page = new RibbonOptionsPage { Text = "General" }; var editor = new TextBox { Text = "Native retained draft" }; page.Controls.Add(editor); dialog.Pages.Add(page); dialog.Pages.Add(new RibbonOptionsPage { Text = "Advanced" }); int applied = 0, cancelled = 0; string checkpoint = "Original"; bool valid = false;
        page.Applying += (_, e) => e.Cancel = !valid; page.Applied += (_, _) => { checkpoint = editor.Text; applied++; }; page.Cancelled += (_, _) => { editor.Text = checkpoint; cancelled++; };
        dialog.Show(); Application.DoEvents(); editor.Focus(); editor.Select(2, 4); nint handle = editor.Handle; var parent = editor.Parent; var nav = Descendants(dialog).OfType<ListBox>().Single(); nint navHandle = nav.Handle; var geometry = Descendants(dialog).Select(c => c.Bounds).ToArray();
        ribbon.Theme = ribbon.Theme with { OptionsAppearance = Authored }; using var image = new Bitmap(nav.Width, nav.ItemHeight); using var g = Graphics.FromImage(image); DrawRow(nav, g, new(Point.Empty, image.Size), 0, true);
        Assert.Equal(geometry, Descendants(dialog).Select(c => c.Bounds)); Assert.Equal(handle, editor.Handle); Assert.Equal(navHandle, nav.Handle); Assert.Same(parent, editor.Parent); Assert.Equal(2, editor.SelectionStart); Assert.Equal(4, editor.SelectionLength); Assert.Equal("Native retained draft", editor.Text); Assert.Same(page, dialog.SelectedPage);
        var apply = Descendants(dialog).OfType<Button>().Single(b => b.Name == "ApplyCustomization"); apply.PerformClick(); Assert.Equal(0, applied); valid = true; apply.PerformClick(); Assert.Equal(1, applied); editor.Text = "Unapplied draft";
        ribbon.Theme = ribbon.Theme with { OptionsAppearance = null }; Assert.Equal(handle, editor.Handle); Assert.Equal(navHandle, nav.Handle); Descendants(dialog).OfType<Button>().Single(b => b.Name == "CancelCustomization").PerformClick(); Assert.Equal(1, cancelled); Assert.Equal("Native retained draft", checkpoint);
    });
    private sealed class OfficeProbe : RibbonOffice2024Renderer
    {
        internal int Radii;
        public override int GetPopupCornerRadius(RibbonTheme theme, int dpi) { Radii++; return 0; }
        public override void DrawText(RibbonPaintContext c, string text, Color ink, TextFormatFlags flags) => throw new InvalidOperationException("Native Options text must retain its direct painter.");
    }
    private sealed class FlatProbe : RibbonRenderer
    {
        internal int Radii;
        public override int GetPopupCornerRadius(RibbonTheme theme, int dpi) { Radii++; return 0; }
        public override void DrawText(RibbonPaintContext c, string text, Color ink, TextFormatFlags flags) => throw new InvalidOperationException("Native Options text must retain its direct painter.");
    }
    private static bool AllWhite(Bitmap image) => !Enumerable.Range(0, image.Height).Any(y => Enumerable.Range(0, image.Width).Any(x => image.GetPixel(x, y).ToArgb() != Color.White.ToArgb()));
    [Fact]
    public void Default_native_navigation_divider_pixels_and_control_geometry_match_saved_results() => FoundationTests.Sta(() =>
    {
        foreach (var original in new[] { RibbonThemes.Office2019Light, RibbonThemes.Office2024Light })
        foreach (bool office in new[] { false, true })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (int points in new[] { 9, 14 })
        {
            var theme = original with { OptionsAppearance = Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_APPEARANCE_EMPTY") == "1" ? new() : null };
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer(); using var font = new Font("Segoe UI", points);
            using var ribbon = new RibbonControl { Theme = theme, Renderer = painter, Font = font, Density = density };
            using var dialog = new RibbonOptionsDialog(ribbon) { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false };
            var first = new RibbonOptionsPage { Text = "General settings" }; var editor = new TextBox { Text = "Retained native draft", Bounds = new(12, 12, 160, 24) }; first.Controls.Add(editor); dialog.Pages.Add(first); dialog.Pages.Add(new RibbonOptionsPage { Text = "Advanced settings" }); dialog.Show(); Application.DoEvents();
            var nav = Descendants(dialog).OfType<ListBox>().Single(c => c.Name == "OptionsNavigation"); var rail = nav.Parent!; var footer = Descendants(dialog).OfType<FlowLayoutPanel>().Single(); Assert.True(nav.Focus());
            using var image = new Bitmap(Math.Max(footer.Width, 2 * rail.Width + 20), Math.Max(rail.Height, 3 * nav.ItemHeight + footer.Height + 20)); using var g = Graphics.FromImage(image); g.Clear(Color.White);
            for (int i = 0; i < 2; i++) DrawRow(nav, g, new(0, i * (nav.ItemHeight + 4), nav.ClientSize.Width, nav.ItemHeight), i, i == 0);
            var state = g.Save(); g.TranslateTransform(rail.Width + 8, 0); Paint(rail, g); g.Restore(state); state = g.Save(); g.TranslateTransform(0, image.Height - footer.Height); Paint(footer, g); g.Restore(state);
            var geometry = JsonSerializer.Serialize(new { dialog.DeviceDpi, dialog.Size, dialog.MinimumSize, Navigation = nav.Bounds, nav.ItemHeight, Rail = rail.Bounds, Footer = footer.Bounds, Page = first.Bounds, Editor = editor.Bounds, Buttons = footer.Controls.Cast<Control>().Select(c => new { c.Name, c.Bounds }) });
            using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png); string name = $"options-{theme.BackstageStyle}-{office}-{density}-{points}";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_APPEARANCE_CAPTURE") is { Length: > 0 } output) { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), stream.ToArray()); File.WriteAllText(Path.Combine(output, name + ".json"), geometry); }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_APPEARANCE_BASELINE") is { Length: > 0 } baseline) { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), stream.ToArray()); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".json")), geometry); }
            dialog.Close();
        }
    });
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static void DrawRow(ListBox list, Graphics g, Rectangle bounds, int index, bool selected) => typeof(ListBox).GetMethod("OnDrawItem", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(list, [new DrawItemEventArgs(g, list.Font, bounds, index, selected ? DrawItemState.Selected : DrawItemState.None)]);
    private static void Paint(Control control, Graphics g) => typeof(Control).GetMethod("OnPaint", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, [new PaintEventArgs(g, control.ClientRectangle)]);
}
