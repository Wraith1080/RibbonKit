using System.Drawing.Imaging;
using System.Text.Json;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class OptionsFieldAppearanceTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Frame_border_radius_alpha_hide_and_content_clearance_fit_four_corners(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi); using var painter = new RoundedPainter(); using var image = new Bitmap(S(220), S(140)); using var g = Graphics.FromImage(image); var a = new RibbonOptionsAppearanceSettings { FrameCornerRadius = 12, FrameBorderWidth = 3, FrameBorderColor = Color.Navy }; var theme = RibbonThemes.Office2019Light with { OptionsAppearance = a }; var bounds = new Rectangle(S(5), S(5), S(200), S(120)); var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi); var m = new RibbonOptionsFieldMetrics(theme, dpi);
        g.Clear(Color.White); RibbonOptionsFieldRenderer.DrawFrame(painter, c, 0); Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(bounds.Left, bounds.Top + bounds.Height / 2).ToArgb()); Assert.Equal(S(3), m.Clearance(bounds.Size));
        for (int y = 0; y < S(12); y++) for (int x = 0; x < S(12); x++) { var pixel = image.GetPixel(bounds.Left + x, bounds.Top + y); Assert.Equal(pixel, image.GetPixel(bounds.Right - 1 - x, bounds.Top + y)); Assert.Equal(pixel, image.GetPixel(bounds.Left + x, bounds.Bottom - 1 - y)); Assert.Equal(pixel, image.GetPixel(bounds.Right - 1 - x, bounds.Bottom - 1 - y)); }
        foreach (var hidden in new[] { a with { FrameBorderWidth = 0 }, a with { FrameBorderColor = Color.Transparent } }) { g.Clear(Color.White); RibbonOptionsFieldRenderer.DrawFrame(painter, c with { Theme = theme with { OptionsAppearance = hidden } }, 0); Assert.False(Has(image, Color.Navy)); Assert.True(new RibbonOptionsFieldMetrics(theme with { OptionsAppearance = hidden }, dpi).Clearance(bounds.Size) >= 1); }
        g.Clear(Color.White); RibbonOptionsFieldRenderer.DrawFrame(painter, c with { Theme = theme with { OptionsAppearance = a with { FrameBorderColor = Color.FromArgb(128, Color.Blue) } } }, 0); var alpha = image.GetPixel(bounds.Left, bounds.Top + bounds.Height / 2); Assert.Equal(255, alpha.B); Assert.InRange(alpha.R, 1, 254);
        var small = new Rectangle(3, 3, 7, 5); g.Clear(Color.White); RibbonOptionsFieldRenderer.DrawFrame(painter, c with { Bounds = small, Theme = theme with { OptionsAppearance = a with { FrameBorderWidth = 32, FrameCornerRadius = 64 } } }, 0); for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) Assert.True(small.Contains(x, y));
    });
    [Fact]
    public void Search_layout_retains_each_legacy_rounding_recipe_and_reserves_authored_native_height() => FoundationTests.Sta(() =>
    {
        foreach (int dpi in new[] { 96, 110, 120, 125, 144, 192 })
        {
            int C(int n) => (int)Math.Ceiling(n * dpi / 96d); var bounds = new Rectangle(13, 17, 410, 90); var m = new RibbonOptionsFieldMetrics(RibbonThemes.Office2019Light, dpi);
            Assert.Equal(new Rectangle(bounds.Left + C(4), bounds.Top + 35, bounds.Width - 2 * C(4), 20), m.SearchTextBounds(bounds, 20, false)); Assert.Equal(new Rectangle(bounds.Left + C(4), bounds.Top + 35, bounds.Width - C(8), 20), m.SearchTextBounds(bounds, 20, true));
            var theme = RibbonThemes.Office2019Light with { OptionsAppearance = new() { FrameBorderWidth = 32 }, EditorTextInset = 0 }; m = new(theme, dpi); int height = m.TextFrameHeight(C(30), C(20)); var frame = new Rectangle(0, 0, C(220), height); var text = m.SearchTextBounds(frame, C(20), true); Assert.True(text.Left >= m.Clearance(frame.Size)); Assert.True(text.Top >= m.Clearance(frame.Size)); Assert.True(frame.Bottom - text.Bottom >= m.Clearance(frame.Size)); Assert.True(frame.Contains(text));
            Assert.Equal(1, new RibbonOptionsFieldMetrics(theme with { OptionsAppearance = new() { FrameBorderWidth = 0 } }, dpi).Clearance(frame.Size));
        }
    });
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Closed_choice_reuses_editor_inset_foreground_field_clearance_and_actual_hooks(int dpi) => FoundationTests.Sta(() =>
    {
        var capture = new ChoiceCapture(); using var painter = new ChoiceProbe(capture); using var model = new RibbonComboBoxItem(); using var image = new Bitmap(400, 100); using var g = Graphics.FromImage(image); var bounds = new Rectangle(5, 5, 360, 70); var theme = RibbonThemes.Office2019Light with { EditorTextInset = 14, EditorForeground = Color.DarkGreen, EditorAppearance = new() { BorderWidth = 3, Inset = 2 } }; var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
        RibbonOptionsFieldRenderer.DrawChoice(painter, c, model, "Native choice", true); Assert.Equal(1, capture.Fields); Assert.Equal(Color.DarkGreen, capture.Color); Assert.True(bounds.Contains(capture.Text)); Assert.True(capture.Text.Top > bounds.Top); Assert.Equal(bounds.Left + RibbonLayoutEngine.Scale(14, dpi), capture.Text.Left); Assert.True(capture.Flags.HasFlag(TextFormatFlags.NoPrefix));
        var original = new RibbonOptionsFieldMetrics(RibbonThemes.Office2019Light, dpi).ChoiceTextBounds(bounds, RibbonDensity.Compact); Assert.Equal(bounds.Left + RibbonLayoutEngine.Scale(6, dpi), original.Left); Assert.Equal(bounds.Top, original.Top); Assert.Equal(bounds.Height, original.Height);
        model.Enabled = false; RibbonOptionsFieldRenderer.DrawChoice(painter, c, model, "Disabled choice", false); Assert.Equal(SystemColors.GrayText, capture.Color);
        foreach (int width in new[] { 0, 1, 7, 31 })
        { var tiny = new RibbonOptionsFieldMetrics(theme with { EditorTextInset = 1024, EditorAppearance = new() { BorderWidth = 32, Inset = 1024 } }, dpi).ChoiceTextBounds(new(0, 0, width, 7), RibbonDensity.Touch); Assert.True(tiny.Width >= 0 && tiny.Height >= 0); }
    });
    [Theory] [InlineData("FrameCornerRadius", 64)] [InlineData("FrameBorderWidth", 32)]
    public void Frame_dimensions_validate_atomically_and_accept_endpoints(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var p = typeof(RibbonOptionsAppearanceSettings).GetProperty(name)!;
        foreach (int n in new[] { -1, maximum + 1 }) { var a = new RibbonOptionsAppearanceSettings(); p.SetValue(a, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { OptionsAppearance = a }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (int? n in new int?[] { 0, maximum, null }) { var a = new RibbonOptionsAppearanceSettings(); p.SetValue(a, n); ribbon.Theme = original with { OptionsAppearance = a }; Assert.Same(a, ribbon.Theme.OptionsAppearance); }
    });
    [Fact]
    public void Native_frame_layout_resets_clearance_and_regions_without_replacing_or_reparenting_children() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var frame = new RibbonOptionsFrame(ribbon) { Size = new(300, 180) }; using var list = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None }; list.Items.AddRange(["Alpha", "Beta", "Gamma"]); list.SelectedIndex = 1; frame.Controls.Add(list); _ = frame.Handle; nint handle = list.Handle; var parent = list.Parent; var original = list.Bounds;
        ribbon.Theme = ribbon.Theme with { OptionsAppearance = new() { FrameCornerRadius = 16, FrameBorderWidth = 3, FrameBorderColor = Color.Navy, ButtonBorderWidth = 2, NavigationCornerRadius = 12 } }; frame.RefreshAppearance(); var m = new RibbonOptionsFieldMetrics(ribbon.Theme, frame.DeviceDpi); Assert.Equal(new Padding(m.Clearance(frame.ClientSize)), frame.Padding); Assert.Equal(frame.Padding.Left, list.Left); Assert.NotNull(list.Region); Assert.False(list.Region!.IsVisible(0, 0)); Assert.Equal(handle, list.Handle); Assert.Same(parent, list.Parent); Assert.Equal(1, list.SelectedIndex);
        ribbon.Theme = ribbon.Theme with { OptionsAppearance = ribbon.Theme.OptionsAppearance! with { FrameCornerRadius = null, FrameBorderWidth = null, FrameBorderColor = Color.Empty } }; frame.RefreshAppearance(); Assert.Equal(original, list.Bounds); Assert.Equal(handle, list.Handle); Assert.Equal(2, ribbon.Theme.OptionsAppearance!.ButtonBorderWidth); Assert.Equal(12, ribbon.Theme.OptionsAppearance.NavigationCornerRadius);
    });
    [Fact]
    public void Frame_reset_retains_native_padding_scaling_while_a_wider_outline_is_authored() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2019Light with { OptionsAppearance = new() { FrameBorderWidth = 4 } } }; using var plain = new RibbonControl { Theme = RibbonThemes.Office2019Light }; using var frame = new RibbonOptionsFrame(ribbon) { Size = new(300, 180) }; using var reference = new RibbonOptionsFrame(plain) { Size = frame.Size }; frame.PerformLayout(); reference.PerformLayout(); frame.Scale(new SizeF(1.5f, 1.5f)); reference.Scale(new SizeF(1.5f, 1.5f)); ribbon.Theme = RibbonThemes.Office2019Light; frame.RefreshAppearance(); Assert.Equal(reference.Padding, frame.Padding);
    });
    [Theory] [InlineData(RibbonLayoutMode.Classic, 8, 9)] [InlineData(RibbonLayoutMode.Simplified, 8, 9)] [InlineData(RibbonLayoutMode.Classic, 32, 14)] [InlineData(RibbonLayoutMode.Simplified, 32, 14)]
    public void Actual_customization_search_list_and_group_name_fields_reserve_strokes_keep_drafts_and_ownership(RibbonLayoutMode mode, int borderWidth, int points) => FoundationTests.Sta(() =>
    {
        using var font = new Font("Segoe UI", points); using var ribbon = new RibbonControl { Font = font, LayoutMode = mode, Theme = RibbonThemes.Office2019Light with { OptionsAppearance = new() { FrameCornerRadius = 12, FrameBorderWidth = borderWidth, FrameBorderColor = Color.Navy }, EditorTextInset = 12, EditorForeground = Color.DarkGreen } }; Seed(ribbon); using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "test.fields") { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; dialog.Show(); Application.DoEvents();
        var draft = dialog.Editor.Session.Draft; var sources = ribbon.QuickAccessItems.ToArray();
        foreach (var page in dialog.Pages)
        {
            dialog.SelectedPage = page; Application.DoEvents(); foreach (var frame in Descendants(page).OfType<RibbonOptionsFrame>())
            {
                var m = new RibbonOptionsFieldMetrics(ribbon.Theme, frame.DeviceDpi); Assert.Equal(new Padding(m.Clearance(frame.ClientSize)), frame.Padding);
                foreach (var child in frame.Controls.Cast<Control>()) { nint handle = child.Handle; var parent = child.Parent; frame.RefreshAppearance(); Assert.Equal(handle, child.Handle); Assert.Same(parent, child.Parent); if (child is TextBox text) { Assert.True(text.Left >= frame.Padding.Left && text.Top >= frame.Padding.Top && text.Bottom <= frame.Height - frame.Padding.Bottom); Assert.Equal(Color.DarkGreen, text.ForeColor); } else Assert.Equal(frame.Padding.Left, child.Left); }
            }
        }
        Assert.Same(draft, dialog.Editor.Session.Draft); Assert.Equal(sources, ribbon.QuickAccessItems); dialog.Close(); Assert.Equal(sources, ribbon.QuickAccessItems);
        using var host = new Form { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000) }; host.Show(); var group = new RibbonCustomizationGroup { Label = "Custom group" }; using var edit = new RibbonCustomizationGroupDialog(ribbon, host, group, group.Label, false) { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000) }; edit.Show(host); Application.DoEvents(); var name = (TextBox)edit.Controls.Find("RibbonGroupName", true).Single(); var nameFrame = (RibbonOptionsFrame)name.Parent!; Assert.True(name.Top >= nameFrame.Padding.Top && name.Bottom <= nameFrame.Height - nameFrame.Padding.Bottom); Assert.Equal(Color.DarkGreen, name.ForeColor); edit.Close(); Assert.Equal("Custom group", group.Label);
    });
    private sealed class ChoiceCapture { internal int Fields; internal Rectangle Text; internal Color Color; internal TextFormatFlags Flags; }
    private sealed class ChoiceProbe(ChoiceCapture c) : RibbonOffice2024Renderer
    {
        public override void DrawComboBox(RibbonPaintContext context, RibbonComboBoxItem item, Rectangle field) { c.Fields++; base.DrawComboBox(context, item, field); }
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { c.Text = context.Bounds; c.Color = color; c.Flags = flags; }
    }
    private static bool Has(Bitmap image, Color color) => Enumerable.Range(0, image.Height).Any(y => Enumerable.Range(0, image.Width).Any(x => image.GetPixel(x, y).ToArgb() == color.ToArgb()));
    [Fact]
    public void Default_native_frames_regions_choice_text_slots_and_geometry_match_saved_results() => FoundationTests.Sta(() =>
    {
        foreach (var original in new[] { RibbonThemes.Office2019Light, RibbonThemes.Office2024Light })
        foreach (bool office in new[] { false, true })
        foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (int points in new[] { 9, 14 })
        {
            var theme = original with { OptionsAppearance = Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_FIELDS_EMPTY") == "1" ? new() : null };
            var log = new List<string>(); using RibbonRenderer renderer = office ? new Office(log) : new Flat(log); using var font = new Font("Segoe UI", points);
            using var ribbon = new RibbonControl { Theme = theme, Renderer = renderer, LayoutMode = mode, Density = density, Font = font }; Seed(ribbon);
            using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "test.fields") { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; dialog.Show(); Application.DoEvents();
            var shots = new List<Bitmap>(); var geometry = new List<string>();
            try
            {
                foreach (var page in dialog.Pages)
                {
                    dialog.SelectedPage = page; Application.DoEvents(); dialog.Controls.Find("OptionsNavigation", true).Single().Focus();
                    foreach (var frame in Descendants(page).OfType<RibbonOptionsFrame>())
                    {
                        log.Clear(); var image = new Bitmap(frame.Width, frame.Height); frame.DrawToBitmap(image, new(Point.Empty, image.Size)); shots.Add(image); geometry.AddRange(log);
                        geometry.Add(JsonSerializer.Serialize(new { Page = page.Name, Frame = frame.Name, frame.Bounds, frame.Padding, Children = frame.Controls.Cast<Control>().Select(c => new { Type = c.GetType().Name, c.Name, c.Bounds, Region = c.Region?.GetRegionData()?.Data }) }));
                    }
                    foreach (var combo in Descendants(page).OfType<RibbonOptionsComboBox>()) { log.Clear(); var image = new Bitmap(combo.Width, combo.Height); combo.DrawToBitmap(image, new(Point.Empty, image.Size)); shots.Add(image); geometry.Add(combo.Name + ":" + combo.Bounds); geometry.AddRange(log); }
                }
                using var comboHost = new RibbonOptionsComboBox(ribbon) { Size = new(240, 44), Font = dialog.Font }; comboHost.Items.AddRange(["Native choice", "Second choice"]); comboHost.SelectedIndex = 0; dialog.SelectedPage!.Controls.Add(comboHost); comboHost.CreateControl();
                foreach (bool enabled in new[] { true, false }) { log.Clear(); comboHost.Enabled = enabled; var image = new Bitmap(comboHost.Width, comboHost.Height); comboHost.DrawToBitmap(image, new(Point.Empty, image.Size)); shots.Add(image); geometry.AddRange(log); }
                int width = shots.Max(i => i.Width), height = shots.Sum(i => i.Height); using var sheet = new Bitmap(width, height); using var g = Graphics.FromImage(sheet); g.Clear(theme.Surface); int y = 0; foreach (var shot in shots) { g.DrawImageUnscaled(shot, 0, y); y += shot.Height; }
                geometry.Add($"native:{dialog.DeviceDpi}/{dialog.Size}/{dialog.MinimumSize}"); string data = string.Join(Environment.NewLine, geometry), name = $"options-fields-{theme.BackstageStyle}-{office}-{mode}-{density}-{points}"; using var stream = new MemoryStream(); sheet.Save(stream, ImageFormat.Png);
                if (Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_FIELDS_CAPTURE") is { Length: > 0 } output) { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), stream.ToArray()); File.WriteAllText(Path.Combine(output, name + ".txt"), data); }
                if (Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_FIELDS_BASELINE") is { Length: > 0 } baseline) { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), stream.ToArray()); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), data); }
            }
            finally { foreach (var image in shots) image.Dispose(); }
            dialog.Close();
        }
    });
    private static void Seed(RibbonControl ribbon) { var tab = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "commands", Text = "Commands" }; var save = new RibbonButtonItem { Id = "save", Text = "Save" }; group.Items.Add(save); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = save }); ribbon.CommandCatalog.CaptureDefaults(); }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private sealed class Office(List<string> log) : RibbonOffice2024Renderer
    { public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { log.Add($"text:{text}/{c.Bounds}/{color.ToArgb()}/{flags}"); base.DrawText(c, text, color, flags); } }
    private sealed class Flat(List<string> log) : RibbonRenderer
    { public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { log.Add($"text:{text}/{c.Bounds}/{color.ToArgb()}/{flags}"); base.DrawText(c, text, color, flags); } }
}
