using System.Drawing.Imaging;
using System.Reflection;
using System.Text.Json;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class OptionsRowAppearanceTests
{
    private static RibbonOptionsAppearanceSettings Authored => new() { RowTextPadding = 12, RowIconSize = 16, RowIconTextGap = 10, RowCornerRadius = 9, RowSelectedBackground = new(Color.LightBlue, Color.LightGreen), RowHeadingBold = false, FrameBorderWidth = 3, ButtonBorderWidth = 2, NavigationCornerRadius = 12 };
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Selection_gradient_alpha_hide_and_four_corners_stay_inside_unchanged_targets(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi); using var painter = new RoundedPainter(); using var image = new Bitmap(S(250), S(80)); using var g = Graphics.FromImage(image); var bounds = new Rectangle(S(7), S(7), S(230), S(60)); var theme = RibbonThemes.Office2019Light with { OptionsAppearance = Authored, Surface = Color.White }; var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
        g.Clear(Color.Magenta); RibbonOptionsRowRenderer.Draw(painter, c, null, "", true, heading: true);
        Assert.Equal(Color.LightBlue.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top).ToArgb()); Assert.Equal(Color.LightGreen.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Bottom - 1).ToArgb());
        foreach (var solid in new[] { Color.Navy, Color.FromArgb(128, Color.Blue), Color.Transparent })
        {
            var t = theme with { OptionsAppearance = Authored with { RowSelectedBackground = new(solid) } }; g.Clear(Color.Magenta); RibbonOptionsRowRenderer.Draw(painter, c with { Theme = t }, null, "", true, heading: true);
            for (int y = 0; y < S(9); y++) for (int x = 0; x < S(9); x++) { var pixel = image.GetPixel(bounds.Left + x, bounds.Top + y); Assert.Equal(pixel, image.GetPixel(bounds.Right - 1 - x, bounds.Top + y)); Assert.Equal(pixel, image.GetPixel(bounds.Left + x, bounds.Bottom - 1 - y)); Assert.Equal(pixel, image.GetPixel(bounds.Right - 1 - x, bounds.Bottom - 1 - y)); }
            var center = image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2); if (solid.A == 0) Assert.Equal(Color.White.ToArgb(), center.ToArgb()); else if (solid.A < 255) { Assert.Equal(255, center.B); Assert.InRange(center.R, 1, 254); } else Assert.Equal(solid.ToArgb(), center.ToArgb()); Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(bounds.Left - 1, bounds.Top).ToArgb());
        }
        g.Clear(Color.Magenta); RibbonOptionsRowRenderer.Draw(painter, c, null, "", false, heading: true); Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Left, bounds.Top).ToArgb());
    });
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Authored_content_fits_short_rows_and_zero_icons_reclaim_the_slot(int dpi)
    {
        var row = new Rectangle(13, 17, 300, 40); var theme = RibbonThemes.Office2019Light with { OptionsAppearance = Authored }; var m = new RibbonOptionsRowMetrics(theme, dpi); var slots = m.Resolve(row, false, false); Assert.True(row.Contains(slots.Icon)); Assert.Equal(row.Left + RibbonLayoutEngine.Scale(12, dpi), slots.Icon.Left); Assert.Equal(slots.Icon.Right + RibbonLayoutEngine.Scale(10, dpi), slots.Text.Left);
        var hidden = new RibbonOptionsRowMetrics(theme with { OptionsAppearance = Authored with { RowIconSize = 0 } }, dpi).Resolve(row, false, false); Assert.True(hidden.Icon.IsEmpty); Assert.Equal(row.Left + RibbonLayoutEngine.Scale(12, dpi), hidden.Text.Left);
        foreach (var profile in new[] { new RibbonOptionsAppearanceSettings { RowTextPadding = 1024 }, new() { RowIconSize = 256 }, new() { RowIconSize = 0 }, new() { RowIconTextGap = 1024 }, Authored with { RowTextPadding = 1024, RowIconSize = 256, RowIconTextGap = 1024 } })
        foreach (int width in new[] { 0, 1, 7, 31 }) foreach (int height in new[] { 0, 1, 7, 31 })
        { var tiny = new Rectangle(5, 6, width, height); var clamped = new RibbonOptionsRowMetrics(theme with { OptionsAppearance = profile }, dpi).Resolve(tiny, true, false); Assert.True(clamped.Text.Width >= 0 && clamped.Text.Height >= 0); if (!clamped.Icon.IsEmpty) Assert.True(tiny.Contains(clamped.Icon)); Assert.InRange(clamped.Text.Left, tiny.Left, tiny.Right); }
    }
    [Fact]
    public void Null_slots_keep_legacy_ceiling_rounding_and_heading_font_policy()
    {
        foreach (int dpi in new[] { 96, 110, 120, 125, 144, 192 }) foreach (bool simplified in new[] { false, true })
        { int C(int n) => (int)Math.Ceiling(n * dpi / 96d); var row = new Rectangle(13, 17, 400, 80); int gap = C(simplified ? 8 : 6), side = C(20); var m = new RibbonOptionsRowMetrics(RibbonThemes.Office2019Light, dpi); Assert.Equal(new Rectangle(row.Left + gap, row.Top + (row.Height - side) / 2, side, side), m.Resolve(row, simplified, false).Icon); Assert.Equal(new Rectangle(row.Left + 2 * gap + side, row.Top, row.Width - 3 * gap - side, row.Height), m.Resolve(row, simplified, false).Text); Assert.Equal(Rectangle.Inflate(row, -gap, 0), m.Resolve(row, simplified, true).Text); }
        var host = FontStyle.Italic | FontStyle.Underline; var metrics = new RibbonOptionsRowMetrics(RibbonThemes.Office2019Light, 120); Assert.Equal(FontStyle.Bold, metrics.HeadingStyle(host)); Assert.Equal(host, new RibbonOptionsRowMetrics(RibbonThemes.Office2019Light with { OptionsAppearance = new() { RowHeadingBold = false } }, 120).HeadingStyle(host | FontStyle.Bold)); Assert.Equal(host | FontStyle.Bold, new RibbonOptionsRowMetrics(RibbonThemes.Office2019Light with { OptionsAppearance = new() { RowHeadingBold = true } }, 120).HeadingStyle(host));
    }
    [Theory] [InlineData("RowTextPadding", 1024)] [InlineData("RowIconSize", 256)] [InlineData("RowIconTextGap", 1024)] [InlineData("RowCornerRadius", 64)]
    public void Row_dimensions_validate_before_mutation_and_accept_zero_maximum_and_null(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var property = typeof(RibbonOptionsAppearanceSettings).GetProperty(name)!;
        foreach (int n in new[] { -1, maximum + 1 }) { var a = new RibbonOptionsAppearanceSettings(); property.SetValue(a, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { OptionsAppearance = a }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (int? n in new int?[] { 0, maximum, null }) { var a = new RibbonOptionsAppearanceSettings(); property.SetValue(a, n); ribbon.Theme = original with { OptionsAppearance = a }; Assert.Same(a, ribbon.Theme.OptionsAppearance); }
    });
    [Fact]
    public void Independent_group_and_builtin_resets_preserve_accepted_roles_and_other_ribbons() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); var theme = ribbon.Theme with { OptionsAppearance = Authored, TipAppearance = new() { KeyTipBorderWidth = 3 } }; ribbon.Theme = theme; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = null; Assert.Same(theme, ribbon.Theme);
        ribbon.Theme = theme with { OptionsAppearance = Authored with { RowCornerRadius = null, RowSelectedBackground = null } }; Assert.Equal(16, ribbon.Theme.OptionsAppearance!.RowIconSize); Assert.Equal(3, ribbon.Theme.OptionsAppearance.FrameBorderWidth); ribbon.Theme = theme with { OptionsAppearance = null }; Assert.Equal(3, ribbon.Theme.TipAppearance!.KeyTipBorderWidth); Assert.Null(other.Theme.OptionsAppearance);
    });
    [Theory] [InlineData(RibbonLayoutMode.Classic)] [InlineData(RibbonLayoutMode.Simplified)]
    public void Actual_callbacks_use_shared_slots_hide_icons_and_keep_native_rows_drafts_and_sources(RibbonLayoutMode mode) => FoundationTests.Sta(() =>
    {
        var log = new List<string>(); var icon = new RecordingIcon(log); using var bitmap = new Bitmap(20, 20); using var font = new Font("Segoe UI", 14, FontStyle.Italic); using var ribbon = new RibbonControl { LayoutMode = mode, Font = font, Theme = RibbonThemes.Office2019Light with { OptionsAppearance = Authored } }; Seed(ribbon, icon, bitmap); using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "test.rows") { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; dialog.Show(); Application.DoEvents(); var draft = dialog.Editor.Session.Draft; var sources = ribbon.QuickAccessItems.ToArray(); int painted = 0;
        foreach (var page in dialog.Pages)
        {
            dialog.SelectedPage = page; Application.DoEvents(); foreach (var list in Descendants(page).OfType<ListBox>())
            {
                nint handle = list.Handle; var parent = list.Parent; var bounds = list.Bounds; int height = list.ItemHeight; var selected = list.SelectedItem; using var image = new Bitmap(list.Width, list.ItemHeight); using var g = Graphics.FromImage(image); var row = new Rectangle(Point.Empty, image.Size);
                log.Clear(); for (int i = 0; i < list.Items.Count; i++) PaintRow(list, g, row, i, DrawItemState.Selected); Assert.NotEmpty(log); Assert.All(log, item => Assert.Contains("Width=" + RibbonLayoutEngine.Scale(16, list.DeviceDpi), item));
                ribbon.Theme = ribbon.Theme with { OptionsAppearance = Authored with { RowIconSize = 0 } }; log.Clear(); for (int i = 0; i < list.Items.Count; i++) PaintRow(list, g, row, i, DrawItemState.Selected); Assert.Empty(log);
                ribbon.Theme = ribbon.Theme with { OptionsAppearance = Authored }; Assert.Equal(handle, list.Handle); Assert.Same(parent, list.Parent); Assert.Equal(bounds, list.Bounds); Assert.Equal(height, list.ItemHeight); Assert.Same(selected, list.SelectedItem); painted++;
            }
            if (page.Controls.Find("SimplifiedCustomizationEditor", true).FirstOrDefault() is { } simplified) { var heading = (Font)simplified.GetType().GetField("_headerFont", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(simplified)!; Assert.False(heading.Bold); Assert.True(heading.Italic); }
        }
        Assert.True(painted >= 3); Assert.Same(draft, dialog.Editor.Session.Draft); Assert.Equal(sources, ribbon.QuickAccessItems); dialog.Close(); Assert.Equal(sources, ribbon.QuickAccessItems); Assert.True(bitmap.Width > 0);
    });
    [Fact]
    public void Default_native_command_rows_icons_focus_headers_and_geometry_match_saved_results() => FoundationTests.Sta(() =>
    {
        foreach (var theme in new[] { RibbonThemes.Office2019Light, RibbonThemes.Office2024Light })
        foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (int points in new[] { 9, 14 })
        {
            var log = new List<string>(); var icon = new RecordingIcon(log); using var bitmap = new Bitmap(20, 20); using (var ink = Graphics.FromImage(bitmap)) ink.Clear(Color.DarkOrange);
            using var font = new Font("Segoe UI", points, FontStyle.Italic); using var ribbon = new RibbonControl { Theme = theme, LayoutMode = mode, Density = density, Font = font }; Seed(ribbon, icon, bitmap);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_ROWS_EMPTY") == "1") ribbon.Theme = theme with { OptionsAppearance = new() };
            using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "test.rows") { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; dialog.Show(); Application.DoEvents();
            var shots = new List<Bitmap>(); var geometry = new List<string>();
            try
            {
                foreach (var page in dialog.Pages)
                {
                    dialog.SelectedPage = page; Application.DoEvents();
                    foreach (var list in Descendants(page).OfType<ListBox>().Where(l => l.Name != "OptionsNavigation"))
                    {
                        geometry.Add(JsonSerializer.Serialize(new { list.Name, list.Bounds, list.ItemHeight, list.SelectedIndex, Font = list.Font.ToString(), Rows = list.Items.Cast<object>().Select(i => i.ToString()) }));
                        foreach (int index in Enumerable.Range(0, list.Items.Count))
                        foreach (var state in new[] { DrawItemState.None, DrawItemState.Selected, DrawItemState.Selected | DrawItemState.Focus, DrawItemState.Selected | DrawItemState.Focus | DrawItemState.NoFocusRect })
                        {
                            using var row = new Bitmap(Math.Max(1, list.Width), list.ItemHeight); using var g = Graphics.FromImage(row); g.Clear(Color.White); log.Clear(); PaintRow(list, g, new(0, 0, row.Width, row.Height), index, state); geometry.AddRange(log); geometry.Add($"{list.Name}:{index}:{state}"); shots.Add((Bitmap)row.Clone());
                        }
                    }
                }
                using var sheet = new Bitmap(shots.Max(i => i.Width), shots.Sum(i => i.Height)); using var graphics = Graphics.FromImage(sheet); graphics.Clear(theme.Surface); int y = 0; foreach (var shot in shots) { graphics.DrawImageUnscaled(shot, 0, y); y += shot.Height; }
                geometry.Add($"native:{dialog.DeviceDpi}/{dialog.Size}"); var data = string.Join(Environment.NewLine, geometry); string name = $"options-rows-{theme.BackstageStyle}-{mode}-{density}-{points}"; using var stream = new MemoryStream(); sheet.Save(stream, ImageFormat.Png);
                if (Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_ROWS_CAPTURE") is { Length: > 0 } output) { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), stream.ToArray()); File.WriteAllText(Path.Combine(output, name + ".txt"), data); }
                if (Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_ROWS_BASELINE") is { Length: > 0 } baseline) { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), stream.ToArray()); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), data); }
            }
            finally { foreach (var shot in shots) shot.Dispose(); }
            dialog.Close();
        }
    });
    private static void PaintRow(ListBox list, Graphics graphics, Rectangle bounds, int index, DrawItemState state) => typeof(ListBox).GetMethod("OnDrawItem", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(list, [new DrawItemEventArgs(graphics, list.Font, bounds, index, state, list.ForeColor, list.BackColor)]);
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static void Seed(RibbonControl ribbon, IRibbonIcon icon, Bitmap bitmap)
    {
        var tab = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "commands", Text = "Commands", Icon = icon }; var vector = new RibbonButtonItem { Id = "vector", Text = "Vector command", Icon = icon, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; var raster = new RibbonButtonItem { Id = "raster", Text = "Raster command", Image = bitmap, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; group.Items.AddRange(vector, raster, new RibbonButtonItem { Id = "plain", Text = "Plain command", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = vector }); ribbon.QuickAccessItems.Add(new RibbonQuickAccessGroupReference { Source = group }); ribbon.CommandCatalog.CaptureDefaults();
    }
    private sealed class RecordingIcon(List<string> log) : IRibbonIcon
    { public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { log.Add($"icon:{bounds}/{context}"); using var brush = new SolidBrush(context.Foreground); graphics.FillRectangle(brush, bounds); } }
}
