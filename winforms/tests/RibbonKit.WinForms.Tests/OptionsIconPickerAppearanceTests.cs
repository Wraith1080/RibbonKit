using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class OptionsIconPickerAppearanceTests
{
    private static RibbonOptionsAppearanceSettings Authored => new() { IconPickerCornerRadius = 9, IconPickerInset = 3, IconPickerBorderWidth = 3, IconPickerBorderColor = Color.Navy, IconPickerSelectedBackground = new(Color.LightBlue), IconPickerIconSize = 16, IconPickerTextInset = 8, TreeCheckSize = 12, RowIconSize = 16, FrameBorderWidth = 3 };
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Selected_tile_fill_outline_alpha_zero_and_four_corners_fit_existing_cell(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi); using var painter = new RoundedPainter(); using var image = new Bitmap(S(100), S(100)); using var g = Graphics.FromImage(image); var cell = new Rectangle(S(7), S(7), S(80), S(80)); var theme = RibbonThemes.Office2019Light with { OptionsAppearance = Authored, Surface = Color.White }; var c = new RibbonPaintContext(g, cell, theme, Control.DefaultFont, dpi); var m = new RibbonOptionsIconPickerMetrics(theme, dpi, RibbonDensity.Compact); var paint = m.SelectionBounds(cell); var blank = new BlankIcon();
        void Draw(RibbonOptionsAppearanceSettings a) { g.Clear(Color.Magenta); RibbonOptionsIconPickerRenderer.Draw(painter, c with { Theme = theme with { OptionsAppearance = a } }, blank, true, 0); }
        Draw(Authored); Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(paint.Left, paint.Top + paint.Height / 2).ToArgb());
        for (int y = 0; y < S(9); y++) for (int x = 0; x < S(9); x++) { var pixel = image.GetPixel(paint.Left + x, paint.Top + y); Assert.Equal(pixel, image.GetPixel(paint.Right - 1 - x, paint.Top + y)); Assert.Equal(pixel, image.GetPixel(paint.Left + x, paint.Bottom - 1 - y)); Assert.Equal(pixel, image.GetPixel(paint.Right - 1 - x, paint.Bottom - 1 - y)); }
        Draw(Authored with { IconPickerBorderWidth = 0 }); Assert.False(Has(image, Color.Navy)); Draw(Authored with { IconPickerBorderColor = Color.Transparent }); Assert.False(Has(image, Color.Navy)); Draw(Authored with { IconPickerBorderColor = Color.FromArgb(128, Color.Blue) }); var alpha = image.GetPixel(paint.Left, paint.Top + paint.Height / 2); Assert.Equal(255, alpha.B); Assert.InRange(alpha.R, 1, 254);
        Draw(Authored with { IconPickerSelectedBackground = new(Color.Transparent) }); Assert.Equal(Color.White.ToArgb(), image.GetPixel(paint.Left + paint.Width / 2, paint.Top + paint.Height / 2).ToArgb()); Assert.True(Has(image, Color.Navy));
        Draw(Authored with { IconPickerSelectedBackground = new(Color.LightBlue, Color.LightGreen), IconPickerBorderWidth = 0 }); Assert.Equal(Color.LightBlue.ToArgb(), image.GetPixel(paint.Left + paint.Width / 2, paint.Top).ToArgb()); Assert.Equal(Color.LightGreen.ToArgb(), image.GetPixel(paint.Left + paint.Width / 2, paint.Bottom - 1).ToArgb()); Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(cell.Left - 1, cell.Top).ToArgb());
    });
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Artwork_zero_hide_and_none_inset_are_independent_of_selection_and_native_cell_geometry(int dpi) => FoundationTests.Sta(() =>
    {
        var cell = new Rectangle(13, 17, 120, 90); var theme = RibbonThemes.Office2019Light with { OptionsAppearance = Authored }; var m = new RibbonOptionsIconPickerMetrics(theme, dpi, RibbonDensity.Compact); var icon = m.IconBounds(cell); Assert.True(cell.Contains(icon)); Assert.Equal(RibbonLayoutEngine.Scale(16, dpi), icon.Width);
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch }) { var legacy = new RibbonOptionsIconPickerMetrics(RibbonThemes.Office2019Light, dpi, density); Assert.Equal((int)Math.Ceiling((density == RibbonDensity.Touch ? 32 : 24) * dpi / 96d), legacy.IconBounds(cell).Width); Assert.Equal(Rectangle.Inflate(cell, -1, -1), legacy.SelectionBounds(cell)); Assert.Equal(Rectangle.Inflate(cell, -2, -2), legacy.TextBounds(cell)); }
        var insetContent = new RibbonOptionsIconPickerMetrics(theme with { OptionsAppearance = Authored with { IconPickerInset = 1024, IconPickerTextInset = 0 } }, dpi, RibbonDensity.Compact); Assert.True(insetContent.IconBounds(cell).IsEmpty || insetContent.TextBounds(cell).Contains(insetContent.IconBounds(cell)));
        foreach (int width in new[] { 0, 1, 7, 31 }) foreach (int height in new[] { 0, 1, 7, 31 }) { var small = new Rectangle(5, 7, width, height); var tiny = new RibbonOptionsIconPickerMetrics(theme with { OptionsAppearance = Authored with { IconPickerInset = 1024, IconPickerTextInset = 1024, IconPickerIconSize = 256, IconPickerBorderWidth = 32 } }, dpi, RibbonDensity.Touch); var art = tiny.IconBounds(small); if (!art.IsEmpty) Assert.True(small.Contains(art)); Assert.True(tiny.SelectionBounds(small).Width >= 0 && tiny.TextBounds(small).Height >= 0); Assert.InRange(tiny.BorderWidth(tiny.SelectionBounds(small)), 0, Math.Max(0, Math.Min(width, height) / 2)); }
        using var painter = new RoundedPainter(); using var image = new Bitmap(120, 90); using var g = Graphics.FromImage(image); var hidden = new Icon([]); var c = new RibbonPaintContext(g, new(Point.Empty, image.Size), theme with { Surface = Color.White, OptionsAppearance = Authored with { IconPickerIconSize = 0 } }, Control.DefaultFont, dpi); RibbonOptionsIconPickerRenderer.Draw(painter, c, hidden, false, 0); Assert.False(HasInk(image)); RibbonOptionsIconPickerRenderer.Draw(painter, c with { Theme = c.Theme with { OptionsAppearance = Authored with { IconPickerTextInset = 4 } } }, null, false, 0); Assert.True(HasInk(image));
    });
    [Theory] [InlineData("IconPickerCornerRadius", 64)] [InlineData("IconPickerInset", 1024)] [InlineData("IconPickerBorderWidth", 32)] [InlineData("IconPickerIconSize", 256)] [InlineData("IconPickerTextInset", 1024)]
    public void Picker_dimensions_validate_atomically_and_allow_zero_maximum_and_null(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var property = typeof(RibbonOptionsAppearanceSettings).GetProperty(name)!;
        foreach (int n in new[] { -1, maximum + 1 }) { var a = new RibbonOptionsAppearanceSettings(); property.SetValue(a, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { OptionsAppearance = a }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (int? n in new int?[] { 0, maximum, null }) { var a = new RibbonOptionsAppearanceSettings(); property.SetValue(a, n); ribbon.Theme = original with { OptionsAppearance = a }; Assert.Same(a, ribbon.Theme.OptionsAppearance); }
    });
    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Actual_tiles_keep_radius_hook_native_cells_items_validation_and_group_ok_cancel(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        Application.EnableVisualStyles(); var log = new List<string>(); using var painter = new Probe(); using var raster = new Bitmap(20, 20); using var ribbon = new RibbonControl { Renderer = painter, Density = density, Theme = RibbonThemes.Office2019Light with { CornerRadius = 12, OptionsAppearance = Authored } }; Seed(ribbon, new Icon(log), raster); using var host = new Form { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; host.Show(); var group = new RibbonCustomizationGroup { Label = "Original", IconCommandId = "alias", Commands = [new() { CommandId = "vector", Size = RibbonItemSize.Small }] };
        using (var dialog = new RibbonCustomizationGroupDialog(ribbon, host, group, group.Label, false) { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false })
        {
            dialog.Show(host); Application.DoEvents(); var list = (ListView)dialog.Controls.Find("RibbonGroupIcons", true).Single(); var items = list.Items.Cast<ListViewItem>().ToArray(); var bounds = items.Select(i => i.Bounds).ToArray(); var parent = list.Parent; var handle = list.Handle; var spacing = SendMessage(handle, 0x1033, 0, 0); var selected = list.SelectedItems.Cast<ListViewItem>().Single(); painter.Radii = painter.Texts = 0; log.Clear(); using (var shot = Paint(list)) { Assert.Equal(1, painter.Radii); Assert.Equal(0, painter.Texts); Assert.Contains(log, s => s.Contains("Width=" + RibbonLayoutEngine.Scale(16, list.DeviceDpi))); } Assert.Equal("alias", selected.Tag); Assert.Equal(4, items.Length);
            ribbon.Theme = ribbon.Theme with { OptionsAppearance = Authored with { IconPickerIconSize = 0, IconPickerBorderWidth = 0, IconPickerSelectedBackground = new(Color.Transparent) } }; log.Clear(); using (var shot = Paint(list)) Assert.Empty(log); ribbon.Theme = ribbon.Theme with { OptionsAppearance = Authored with { IconPickerCornerRadius = null, IconPickerInset = null, IconPickerBorderWidth = null, IconPickerBorderColor = Color.Empty, IconPickerSelectedBackground = null, IconPickerIconSize = null, IconPickerTextInset = null } }; using (var shot = Paint(list)) { } Assert.Equal(items, list.Items.Cast<ListViewItem>()); Assert.All(items, item => Assert.Equal(item.GetBounds(ItemBoundsPortion.Icon).Width, item.GetBounds(ItemBoundsPortion.Icon).Height)); Assert.Equal(handle, list.Handle); Assert.Same(parent, list.Parent); Assert.Same(selected, list.SelectedItems.Cast<ListViewItem>().Single()); Assert.Equal(12, ribbon.Theme.OptionsAppearance!.TreeCheckSize);
            var name = (TextBox)dialog.Controls.Find("RibbonGroupName", true).Single(); var ok = (Button)dialog.Controls.Find("AcceptGroup", true).Single(); name.Text = " "; ok.PerformClick(); Assert.True(dialog.Visible); Assert.Equal("Original", group.Label); name.Text = "  New label  "; list.Items["raster"]!.Selected = true; ((ComboBox)dialog.Controls.Find("RibbonGroupLayout", true).Single()).SelectedItem = RibbonCustomizationGroupLayout.Large; ok.PerformClick(); Assert.Equal("New label", group.Label); Assert.Equal("raster", group.IconCommandId); Assert.Equal(RibbonItemSize.Large, group.Commands[0].Size);
        }
        using var canceled = new RibbonCustomizationGroupDialog(ribbon, host, group, group.Label!, false) { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; canceled.Show(host); ((TextBox)canceled.Controls.Find("RibbonGroupName", true).Single()).Text = "Discard"; ((Button)canceled.Controls.Find("CancelGroup", true).Single()).PerformClick(); Assert.Equal("New label", group.Label); Assert.True(raster.Width > 0);
    });
    [Fact]
    public void Picker_partial_group_builtin_resets_retain_tree_list_frames_and_other_ribbons() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); var theme = ribbon.Theme with { OptionsAppearance = Authored, Chevrons = new() { Length = 14 } }; ribbon.Theme = theme; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = null; Assert.Same(theme, ribbon.Theme); ribbon.Theme = theme with { OptionsAppearance = Authored with { IconPickerBorderWidth = null, IconPickerBorderColor = Color.Empty } }; Assert.Equal(16, ribbon.Theme.OptionsAppearance!.IconPickerIconSize); Assert.Equal(12, ribbon.Theme.OptionsAppearance.TreeCheckSize); Assert.Equal(16, ribbon.Theme.OptionsAppearance.RowIconSize); Assert.Equal(3, ribbon.Theme.OptionsAppearance.FrameBorderWidth); ribbon.Theme = theme with { OptionsAppearance = null }; Assert.Equal(14, ribbon.Theme.Chevrons!.Length); Assert.Null(other.Theme.OptionsAppearance);
    });
    private sealed class BlankIcon : IRibbonIcon { public void Draw(Graphics g, Rectangle b, RibbonIconContext c) { } }
    private sealed class Probe : RibbonOffice2024Renderer { internal int Radii, Texts; public override int GetPopupCornerRadius(RibbonTheme theme, int dpi) { Radii++; return 8; } public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { Texts++; base.DrawText(c, text, color, flags); } }
    private static bool HasInk(Bitmap image) => Enumerable.Range(0, image.Height).Any(y => Enumerable.Range(0, image.Width).Any(x => image.GetPixel(x, y).ToArgb() != Color.White.ToArgb()));
    private static bool Has(Bitmap image, Color color) => Enumerable.Range(0, image.Height).Any(y => Enumerable.Range(0, image.Width).Any(x => image.GetPixel(x, y).ToArgb() == color.ToArgb()));
    [Fact]
    public void Default_native_picker_tile_callbacks_slots_selection_and_cell_spacing_match_saved_results() => FoundationTests.Sta(() =>
    {
        Application.EnableVisualStyles();
        foreach (var original in new[] { RibbonThemes.Office2019Light, RibbonThemes.Office2024Light })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (int points in new[] { 9, 14 })
        foreach (bool office in new[] { false, true })
        {
            var log = new List<string>(); using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer(); using var font = new Font("Segoe UI", points, FontStyle.Italic); using var raster = new Bitmap(20, 20); using (var g = Graphics.FromImage(raster)) g.Clear(Color.DarkOrange); using var ribbon = new RibbonControl { Theme = original, Renderer = painter, Density = density, Font = font }; Seed(ribbon, new Icon(log), raster);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_PICKER_EMPTY") == "1") ribbon.Theme = original with { OptionsAppearance = new() };
            using var host = new Form { Font = font, StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; host.Show(); var group = new RibbonCustomizationGroup { Label = "Sample group", IconCommandId = "alias" }; using var dialog = new RibbonCustomizationGroupDialog(ribbon, host, group, group.Label, false) { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; dialog.Show(host); Application.DoEvents(); var list = (ListView)dialog.Controls.Find("RibbonGroupIcons", true).Single(); var shots = new List<Bitmap>(); var geometry = new List<string>();
            try
            {
                foreach (int selected in new[] { 0, 2 }) foreach (bool focused in new[] { false, true }) foreach (bool enabled in new[] { false, true })
                {
                    list.Enabled = true; foreach (ListViewItem item in list.Items) item.Selected = item.Index == selected; list.Enabled = enabled; dialog.Activate(); if (focused && enabled) list.Focus(); else dialog.Controls.Find("RibbonGroupName", true).Single().Focus(); log.Clear(); var image = Paint(list); shots.Add(image); geometry.AddRange(log);
                    geometry.Add(JsonSerializer.Serialize(new { selected, focused, enabled, ActualFocus = list.Focused, list.Bounds, Spacing = (long)SendMessage(list.Handle, 0x1033, 0, 0), Font = list.Font.ToString(), Items = list.Items.Cast<ListViewItem>().Select(i => new { i.Text, i.Tag, i.Bounds, i.Selected }) }));
                }
                using var sheet = new Bitmap(shots.Max(i => i.Width), shots.Sum(i => i.Height)); using var g = Graphics.FromImage(sheet); g.Clear(original.Surface); int y = 0; foreach (var shot in shots) { g.DrawImageUnscaled(shot, 0, y); y += shot.Height; }
                geometry.Add("dpi:" + list.DeviceDpi); string name = $"options-picker-{original.BackstageStyle}-{density}-{points}-{office}"; string data = string.Join(Environment.NewLine, geometry); using var stream = new MemoryStream(); sheet.Save(stream, ImageFormat.Png);
                if (Environment.GetEnvironmentVariable("RIBBONKIT_PICKER_CAPTURE") is { Length: > 0 } output) { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), stream.ToArray()); File.WriteAllText(Path.Combine(output, name + ".txt"), data); }
                if (Environment.GetEnvironmentVariable("RIBBONKIT_PICKER_BASELINE") is { Length: > 0 } baseline) { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), stream.ToArray()); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), data); }
            }
            finally { foreach (var shot in shots) shot.Dispose(); }
            dialog.Close(); host.Close();
        }
    });
    private static Bitmap Paint(ListView list)
    {
        list.PerformLayout(); var image = new Bitmap(list.Width, list.Height); using var g = Graphics.FromImage(image); g.Clear(list.BackColor); var draw = list.GetType().GetMethod("OnDrawItem", BindingFlags.Instance | BindingFlags.NonPublic)!;
        foreach (ListViewItem item in list.Items) { var state = item.Selected ? ListViewItemStates.Selected | (list.Focused ? ListViewItemStates.Focused : 0) : 0; draw.Invoke(list, [new DrawListViewItemEventArgs(g, item, item.Bounds, item.Index, state)]); } return image;
    }
    private static void Seed(RibbonControl ribbon, IRibbonIcon icon, Bitmap raster)
    {
        var tab = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "commands", Text = "Commands" }; group.Items.AddRange(new RibbonButtonItem { Id = "vector", Text = "Vector", Icon = icon }, new RibbonButtonItem { Id = "alias", Text = "Shared vector", Icon = icon }, new RibbonButtonItem { Id = "raster", Text = "Raster", Image = raster }, new RibbonButtonItem { Id = "large", Text = "Large raster", LargeImage = raster }); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.CommandCatalog.CaptureDefaults();
    }
    private sealed class Icon(List<string> log) : IRibbonIcon { public void Draw(Graphics g, Rectangle b, RibbonIconContext c) { log.Add($"icon:{b}/{c}"); using var ink = new SolidBrush(c.Foreground); g.FillRectangle(ink, b); } }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
}
