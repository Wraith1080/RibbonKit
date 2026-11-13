using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class OptionsTreeAppearanceTests
{
    private static RibbonOptionsAppearanceSettings Authored => new() { TreeCornerRadius = 9, TreeSelectedBackground = new(Color.LightBlue, Color.LightGreen), TreeCheckSize = 12, TreeIconSize = 10, TreeIconTextGap = 4, TreeFocusInset = 5, RowIconSize = 16, FrameBorderWidth = 3 };
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Selection_fill_alpha_hide_four_corners_and_native_focus_fit_the_row(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi); using var owner = new RibbonOffice2024Renderer(); using var painter = new RoundedPainter(); using var check = new RibbonCheckBoxItem(); using var image = new Bitmap(S(250), S(100)); using var g = Graphics.FromImage(image); var row = new Rectangle(S(7), S(7), S(230), S(80)); var theme = RibbonThemes.Office2019Light with { OptionsAppearance = Authored, Surface = Color.White }; var c = new RibbonPaintContext(g, row, theme, Control.DefaultFont, dpi); var slots = new RibbonOptionsTreeSlots(Rectangle.Empty, Rectangle.Empty, Rectangle.Empty, Rectangle.Empty);
        void Draw(RibbonTheme t, bool selected, bool focus = false, bool cues = false) => RibbonOptionsTreeRenderer.Draw(owner, painter, c with { Theme = t, Focused = focus }, slots, "", selected, false, false, null, true, cues, check, null);
        g.Clear(Color.Magenta); Draw(theme, true); Assert.Equal(Color.LightBlue.ToArgb(), image.GetPixel(row.Left + row.Width / 2, row.Top).ToArgb()); Assert.Equal(Color.LightGreen.ToArgb(), image.GetPixel(row.Left + row.Width / 2, row.Bottom - 1).ToArgb());
        foreach (var color in new[] { Color.Navy, Color.FromArgb(128, Color.Blue), Color.Transparent })
        {
            g.Clear(Color.Magenta); Draw(theme with { OptionsAppearance = Authored with { TreeSelectedBackground = new(color) } }, true);
            for (int y = 0; y < S(9); y++) for (int x = 0; x < S(9); x++) { var pixel = image.GetPixel(row.Left + x, row.Top + y); Assert.Equal(pixel, image.GetPixel(row.Right - 1 - x, row.Top + y)); Assert.Equal(pixel, image.GetPixel(row.Left + x, row.Bottom - 1 - y)); Assert.Equal(pixel, image.GetPixel(row.Right - 1 - x, row.Bottom - 1 - y)); }
            var center = image.GetPixel(row.Left + row.Width / 2, row.Top + row.Height / 2); if (color.A == 0) Assert.Equal(Color.White.ToArgb(), center.ToArgb()); else if (color.A < 255) { Assert.Equal(255, center.B); Assert.InRange(center.R, 1, 254); } else Assert.Equal(color.ToArgb(), center.ToArgb()); Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(row.Left - 1, row.Top).ToArgb());
        }
        g.Clear(Color.White); Draw(theme, true, false, true); using var hidden = (Bitmap)image.Clone(); Draw(theme, true, true, false); Assert.Equal(Pixels(hidden), Pixels(image)); Draw(theme, true, true, true); Assert.NotEqual(Pixels(hidden), Pixels(image));
        Assert.Equal(Rectangle.Inflate(row, -S(5), -S(5)), new RibbonOptionsTreeMetrics(theme, dpi, RibbonDensity.Compact).FocusBounds(row)); Assert.Equal(Rectangle.Inflate(row, -1, -1), new RibbonOptionsTreeMetrics(RibbonThemes.Office2019Light, dpi, RibbonDensity.Compact).FocusBounds(row));
    });
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Shared_chevron_dimensions_stroke_zero_hide_and_small_slot_clip_keep_both_orientations(int dpi) => FoundationTests.Sta(() =>
    {
        using var image = new Bitmap(100, 100); using var g = Graphics.FromImage(image); var slot = new Rectangle(17, 19, 31, 29); var theme = RibbonThemes.Office2019Light; var c = new RibbonPaintContext(g, slot, theme, Control.DefaultFont, dpi);
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch }) foreach (bool expanded in new[] { false, true })
        {
            var m = new RibbonOptionsTreeMetrics(theme, dpi, density); var legacy = m.Chevron(slot, expanded); Assert.Equal(RibbonLayoutEngine.Scale(density == RibbonDensity.Touch ? 16 : 12, dpi), legacy.Length); Assert.Equal(legacy.Length / 2, legacy.Depth); Assert.False(legacy.Authored);
            foreach (var hidden in new[] { new RibbonChevronSettings { Length = 0 }, new() { Depth = 0 }, new() { StrokeWidth = 0 } }) { g.Clear(Color.White); RibbonOptionsTreeRenderer.DrawChevron(c with { Theme = theme with { Chevrons = hidden }, Density = density }, slot, expanded); for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) Assert.Equal(Color.White.ToArgb(), image.GetPixel(x, y).ToArgb()); }
            g.Clear(Color.White); RibbonOptionsTreeRenderer.DrawChevron(c with { Theme = theme with { Chevrons = new() { Length = 64, Depth = 64, StrokeWidth = 3 } }, Density = density }, slot, expanded); bool ink = false; for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) { ink = true; Assert.True(slot.Contains(x, y)); } Assert.True(ink);
        }
    });
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Decoration_slots_keep_native_text_check_expand_targets_and_clamp_each_authored_override(int dpi)
    {
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            var native = new RibbonOptionsTreeMetrics(RibbonThemes.Office2019Light, dpi, density); var node = new Rectangle(64, 17, 150, 24); var baseline = native.Resolve(node, 1, native.Indent, 0, 40, 400, true);
            foreach (var profile in new[] { Authored, new() { TreeIconSize = 256 }, new() { TreeIconSize = 0 }, new() { TreeIconTextGap = 1024 }, new() { TreeCheckSize = 64 }, new() { TreeCheckSize = 0 }, new() { TreeFocusInset = 1024 } })
            {
                var m = new RibbonOptionsTreeMetrics(RibbonThemes.Office2019Light with { OptionsAppearance = profile }, dpi, density); var slots = m.Resolve(node, 1, native.Indent, 0, 40, 400, true); Assert.Equal(baseline.Expand, slots.Expand); Assert.Equal(baseline.Check, slots.Check); Assert.Equal(baseline.Text, slots.Text);
                foreach (int width in new[] { 0, 1, 7, 31, 400 }) foreach (int height in new[] { 0, 1, 7, 31 })
                {
                    var small = m.Resolve(node, 1, native.Indent, 80, height, width, false); if ((profile.TreeIconSize is not null || profile.TreeIconTextGap is not null) && !small.Icon.IsEmpty) Assert.True(new Rectangle(0, node.Top, width, height).Contains(small.Icon));
                    var checkSlot = new Rectangle(17, 19, width, height); var check = m.CheckBounds(checkSlot); if (profile.TreeCheckSize is not null && !check.IsEmpty) Assert.True(checkSlot.Contains(check));
                    var focus = m.FocusBounds(new(0, 0, width, height)); if (profile.TreeFocusInset is not null) Assert.True(focus.Width >= 0 && focus.Height >= 0);
                }
            }
        }
    }
    [Theory] [InlineData("TreeCornerRadius", 64)] [InlineData("TreeCheckSize", 64)] [InlineData("TreeIconSize", 256)] [InlineData("TreeIconTextGap", 1024)] [InlineData("TreeFocusInset", 1024)]
    public void Dimensions_validate_before_theme_or_builtin_mutation_and_allow_endpoints(string name, int maximum) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light }; var original = ribbon.Theme; var property = typeof(RibbonOptionsAppearanceSettings).GetProperty(name)!;
        foreach (int n in new[] { -1, maximum + 1 }) { var a = new RibbonOptionsAppearanceSettings(); property.SetValue(a, n); Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { OptionsAppearance = a }); Assert.Same(original, ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2019Light, ribbon.BuiltInTheme); }
        foreach (int? n in new int?[] { 0, maximum, null }) { var a = new RibbonOptionsAppearanceSettings(); property.SetValue(a, n); ribbon.Theme = original with { OptionsAppearance = a }; Assert.Same(a, ribbon.Theme.OptionsAppearance); }
    });
    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void Native_clicks_hot_pressed_and_zero_paint_keep_actual_indicator_hooks_and_single_toggle(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        var log = new List<string>(); using var painter = new Office(log); using var ribbon = new RibbonControl { Density = density, Renderer = painter, Theme = RibbonThemes.Office2019Light with { OptionsAppearance = Authored, Chevrons = new() { Length = 14, Depth = 8, StrokeWidth = 2 } } }; using var host = new Form { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), Size = new(600, 400), ShowInTaskbar = false }; using var tree = new RibbonCustomizationTree(ribbon) { Dock = DockStyle.Fill, ItemHeight = 40 }; host.Controls.Add(tree); var root = new TreeNode("Tab") { StateImageIndex = 1 }; root.Nodes.Add(new TreeNode("Group")); tree.Nodes.Add(root); tree.UpdateMetrics(); root.Expand(); host.Show(); Application.DoEvents(); var before = tree.Geometry(root); nint handle = tree.Handle; var parent = tree.Parent; int toggles = 0; tree.CheckRequested += n => { toggles++; n.StateImageIndex = 1 - n.StateImageIndex; };
        var hit = new Point(before.Check.Left + before.Check.Width / 2, before.Check.Top + before.Check.Height / 2); Message(tree, 0x200, hit); Message(tree, 0x201, hit); log.Clear(); using (var shot = new Bitmap(tree.Width, tree.Height)) tree.DrawToBitmap(shot, tree.ClientRectangle); Assert.Contains(log, s => s.StartsWith("check:") && s.Contains("/True/True/")); Message(tree, 0x203, hit); Message(tree, 0x202, hit); Assert.Equal(1, toggles); Assert.Equal(0, root.StateImageIndex);
        ribbon.Theme = ribbon.Theme with { OptionsAppearance = Authored with { TreeCheckSize = 0, TreeIconSize = 0 }, Chevrons = new() { Length = 0 } }; log.Clear(); using (var shot = new Bitmap(tree.Width, tree.Height)) tree.DrawToBitmap(shot, tree.ClientRectangle); Assert.DoesNotContain(log, s => s.StartsWith("check:")); Message(tree, 0x201, hit); Message(tree, 0x202, hit); Assert.Equal(2, toggles); Assert.Equal(1, root.StateImageIndex);
        var expand = before.Expand; var point = new Point(expand.Left + expand.Width / 2, expand.Top + expand.Height / 2); Message(tree, 0x201, point); Message(tree, 0x203, point); Message(tree, 0x202, point); Assert.False(root.IsExpanded); Message(tree, 0x201, point); Message(tree, 0x202, point); Assert.True(root.IsExpanded); var after = tree.Geometry(root); Assert.Equal(before.Expand, after.Expand); Assert.Equal(before.Check, after.Check); Assert.Equal(before.Text, after.Text); Assert.True(after.Icon.IsEmpty); Assert.Equal(handle, tree.Handle); Assert.Same(parent, tree.Parent); Assert.Same(root, tree.SelectedNode);
    });
    [Fact]
    public void Partial_group_builtin_and_chevron_resets_retain_independent_list_frame_and_palette_roles() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var other = new RibbonControl(); var theme = ribbon.Theme with { OptionsAppearance = Authored, Chevrons = new() { Length = 14 }, TipAppearance = new() { KeyTipBorderWidth = 3 } }; ribbon.Theme = theme; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = null; Assert.Same(theme, ribbon.Theme);
        ribbon.Theme = theme with { OptionsAppearance = Authored with { TreeSelectedBackground = null, TreeCornerRadius = null } }; Assert.Equal(12, ribbon.Theme.OptionsAppearance!.TreeCheckSize); Assert.Equal(16, ribbon.Theme.OptionsAppearance.RowIconSize); ribbon.Theme = theme with { OptionsAppearance = null }; Assert.Equal(14, ribbon.Theme.Chevrons!.Length); Assert.Equal(3, ribbon.Theme.TipAppearance!.KeyTipBorderWidth); ribbon.Theme = theme with { Chevrons = null }; Assert.Same(theme.OptionsAppearance, ribbon.Theme.OptionsAppearance); Assert.Equal(12, ribbon.Theme.OptionsAppearance.TreeCheckSize); Assert.Null(other.Theme.OptionsAppearance);
    });
    private static byte[] Pixels(Bitmap image) { using var bytes = new MemoryStream(); image.Save(bytes, ImageFormat.Png); return bytes.ToArray(); }
    private static void Message(Control control, int message, Point point) => SendMessage(control.Handle, message, 0, (nint)((point.Y << 16) | (point.X & 0xffff)));
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
    [Fact]
    public void Default_native_tree_states_slots_and_actual_indicator_hooks_match_saved_results() => FoundationTests.Sta(() =>
    {
        foreach (var original in new[] { RibbonThemes.Office2019Light, RibbonThemes.Office2024Light })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (int points in new[] { 9, 14 })
        foreach (bool office in new[] { false, true })
        {
            var log = new List<string>(); using RibbonRenderer painter = office ? new Office(log) : new Flat(log); var icon = new Icon(log); using var font = new Font("Segoe UI", points, FontStyle.Italic); using var ribbon = new RibbonControl { Theme = original, Renderer = painter, Font = font, Density = density }; Seed(ribbon, icon);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_TREE_EMPTY") == "1") ribbon.Theme = original with { OptionsAppearance = new(), Chevrons = new() };
            using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "test.tree") { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; dialog.SelectedPage = dialog.Pages.First(p => p.Name != "QuickAccessOptionsPage"); dialog.Show(); Application.DoEvents(); var tree = (RibbonCustomizationTree)dialog.Controls.Find("RibbonHierarchy", true).Single(); var root = tree.Nodes[0]; var shots = new List<Bitmap>(); var geometry = new List<string>();
            try
            {
                foreach (bool expanded in new[] { true, false }) foreach (bool enabled in new[] { true, false }) foreach (bool focused in new[] { false, true })
                {
                    tree.Enabled = true; if (expanded) root.ExpandAll(); else root.Collapse(); tree.SelectedNode = root; tree.Enabled = enabled; dialog.Activate();
                    if (focused && enabled) tree.Focus(); else dialog.Controls.Find("OptionsNavigation", true).Single().Focus();
                    log.Clear(); var image = new Bitmap(tree.Width, tree.Height); tree.DrawToBitmap(image, new(Point.Empty, image.Size)); shots.Add(image); geometry.AddRange(log);
                    geometry.Add(JsonSerializer.Serialize(new { expanded, enabled, focused, ActualFocus = tree.Focused, tree.Bounds, tree.ItemHeight, tree.Indent, Nodes = Visible(tree).Select(n => new { n.Text, n.Bounds, n.IsExpanded, n.IsSelected, n.StateImageIndex, Slots = tree.Geometry(n) }) }));
                }
                using var sheet = new Bitmap(shots.Max(i => i.Width), shots.Sum(i => i.Height)); using var g = Graphics.FromImage(sheet); g.Clear(original.Surface); int y = 0; foreach (var shot in shots) { g.DrawImageUnscaled(shot, 0, y); y += shot.Height; }
                geometry.Add("dpi:" + tree.DeviceDpi); string name = $"options-tree-{original.BackstageStyle}-{density}-{points}-{office}"; string data = string.Join(Environment.NewLine, geometry); using var stream = new MemoryStream(); sheet.Save(stream, ImageFormat.Png);
                if (Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_TREE_CAPTURE") is { Length: > 0 } output) { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name + ".png"), stream.ToArray()); File.WriteAllText(Path.Combine(output, name + ".txt"), data); }
                if (Environment.GetEnvironmentVariable("RIBBONKIT_OPTIONS_TREE_BASELINE") is { Length: > 0 } baseline) { Assert.Equal(File.ReadAllBytes(Path.Combine(baseline, name + ".png")), stream.ToArray()); Assert.Equal(File.ReadAllText(Path.Combine(baseline, name + ".txt")), data); }
            }
            finally { foreach (var shot in shots) shot.Dispose(); }
            dialog.Close();
        }
    });
    private static IEnumerable<TreeNode> Visible(TreeView tree) { for (var node = tree.TopNode; node is not null && node.Bounds.Top < tree.Height; node = node.NextVisibleNode) yield return node; }
    private static void Seed(RibbonControl ribbon, IRibbonIcon icon)
    {
        for (int i = 0; i < 2; i++) { var tab = new RibbonTab { Id = "tab" + i, Text = "Tab " + i }; var group = new RibbonGroup { Id = "group" + i, Text = "Group " + i, Icon = icon }; group.Items.Add(new RibbonButtonItem { Id = "save" + i, Text = "Save " + i, Icon = icon }); tab.Groups.Add(group); ribbon.Tabs.Add(tab); } ribbon.CommandCatalog.CaptureDefaults();
    }
    private sealed class Icon(List<string> log) : IRibbonIcon { public void Draw(Graphics g, Rectangle b, RibbonIconContext c) { log.Add($"icon:{b}/{c}"); using var ink = new SolidBrush(c.Foreground); g.FillRectangle(ink, b); } }
    private sealed class Office(List<string> log) : RibbonOffice2024Renderer { public override void DrawCheckBoxIndicator(RibbonPaintContext c, RibbonCheckBoxItem item) { log.Add($"check:{c.Bounds}/{c.Hot}/{c.Pressed}/{c.Focused}/{item.Checked}/{item.Enabled}"); base.DrawCheckBoxIndicator(c, item); } }
    private sealed class Flat(List<string> log) : RibbonRenderer { public override void DrawCheckBoxIndicator(RibbonPaintContext c, RibbonCheckBoxItem item) { log.Add($"check:{c.Bounds}/{c.Hot}/{c.Pressed}/{c.Focused}/{item.Checked}/{item.Enabled}"); base.DrawCheckBoxIndicator(c, item); } }
}
