using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class OptionsIconPickerCheck
{
    // Exercise the real shared group dialog through a public editor's New Group
    // button. The timer inspects the modal child and always closes the probe.
    internal static void WithPicker(RibbonControl model, Form owner, Action<Form, RibbonCustomizationEditor> inspect, Action<RibbonCustomizationEditor>? after = null)
    {
        using var parent = new Form { Font = model.Font, StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1100, 700), ShowInTaskbar = false }; var editor = new RibbonCustomizationEditor(model, "RibbonKit.Picker.Check") { Dock = DockStyle.Fill }; parent.Controls.Add(editor); parent.Show(owner); Application.DoEvents(); var button = (Button)editor.Controls.Find("NewRibbonGroup", true).Single(); Require(button.Enabled, "New Group fixture has no selected native node.");
        Exception? failure = null; bool inspected = false; var deadline = DateTime.UtcNow.AddSeconds(5); using var timer = new System.Windows.Forms.Timer { Interval = 15 };
        timer.Tick += (_, _) =>
        {
            var dialog = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.Name == "RibbonGroupDialog" && f.Visible);
            if (dialog is null) { if (DateTime.UtcNow > deadline) { timer.Stop(); failure = new TimeoutException("Group editor did not open."); parent.Close(); } return; }
            timer.Stop(); dialog.StartPosition = FormStartPosition.Manual; dialog.Location = new(-16000, -16000); dialog.ShowInTaskbar = false;
            try { inspect(dialog, editor); inspected = true; } catch (Exception error) { failure = error; }
            finally { if (!dialog.IsDisposed && dialog.Visible && (failure is not null || dialog.DialogResult == DialogResult.None)) ((Button)dialog.Controls.Find("CancelGroup", true).Single()).PerformClick(); }
        };
        timer.Start(); try { button.PerformClick(); } finally { timer.Stop(); }
        if (failure is not null) throw new InvalidOperationException("Public group-editor inspection failed.", failure); Require(inspected, "Group editor callback was not reached."); after?.Invoke(editor); parent.Close();
    }
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            var icon = new ProbeIcon(); using var renderer = new ProbeRenderer(); using var raster = new Bitmap(20, 20); using (var g = Graphics.FromImage(raster)) g.Clear(Color.DarkOrange); using var owner = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1200, 700), ShowInTaskbar = false, Backdrop = backdrop }; using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = renderer, AnimationLevel = RibbonAnimationLevel.None }; owner.Controls.Add(ribbon); owner.Ribbon = ribbon;
            var tab = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "commands", Text = "Commands" }; var vector = new RibbonButtonItem { Id = "vector", Text = "Vector", Icon = icon }; group.Items.AddRange(vector, new RibbonButtonItem { Id = "alias", Text = "Shared artwork", Icon = icon }, new RibbonButtonItem { Id = "raster", Text = "Raster", Image = raster }); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.CommandCatalog.CaptureDefaults(); owner.Show(); Application.DoEvents(); var original = ribbon.Theme; string scope = family + "-" + backdrop; int states = 0;
            var a = new RibbonOptionsAppearanceSettings { IconPickerCornerRadius = 9, IconPickerInset = 3, IconPickerBorderWidth = 3, IconPickerBorderColor = Color.Navy, IconPickerSelectedBackground = new(Color.LightBlue, Color.LightGreen), IconPickerIconSize = 16, IconPickerTextInset = 0, TreeCheckSize = 12, RowIconSize = 16, FrameBorderWidth = 3 };
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified }) foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original with { OptionsAppearance = a }; int liveGroups = ribbon.Tabs.Sum(t => t.Groups.Count); var sources = ribbon.Tabs.ToArray();
                WithPicker(ribbon, owner, (dialog, editor) =>
                {
                    var list = (ListView)dialog.Controls.Find("RibbonGroupIcons", true).Single(); var items = list.Items.Cast<ListViewItem>().ToArray(); var cells = items.Select(i => i.Bounds).ToArray(); var handle = list.Handle; var parent = list.Parent; var spacing = SendMessage(handle, 0x1033, 0, 0); var selected = list.SelectedItems.Cast<ListViewItem>().Single(); Require(items.Length == 3 && items[0].Text == "None" && list.Items["alias"] is null, "Native picker artwork deduplication changed."); icon.Bounds.Clear(); renderer.Radii = renderer.Texts = 0;
                    using (var image = Print(list)) { Require(icon.Bounds.Count > 0 && icon.Bounds.All(b => b.Width == S(16, list.DeviceDpi)), "Actual picker artwork bypassed authored size."); Require(renderer.Radii > 0 && renderer.Texts == 0, "Picker radius/direct native text contract changed."); Require(image.GetPixel(selected.GetBounds(ItemBoundsPortion.Icon).Left + S(3, list.DeviceDpi), selected.GetBounds(ItemBoundsPortion.Icon).Top + selected.GetBounds(ItemBoundsPortion.Icon).Height / 2).ToArgb() == Color.Navy.ToArgb(), "Actual native selection lost authored outline."); image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-picker.png")); }
                    int smallSide = selected.GetBounds(ItemBoundsPortion.Icon).Width;
                    foreach (int size in new[] { 16, 48 }) foreach (int inset in new[] { 0, 4 })
                    {
                        ribbon.Theme = ribbon.Theme with { OptionsAppearance = a with { IconPickerIconSize = size, IconPickerInset = inset } }; list.PerformLayout();
                        foreach (var item in new[] { items[0], items[1] })
                        {
                            item.Selected = true; item.EnsureVisible(); var cell = item.GetBounds(ItemBoundsPortion.Icon); Require(cell.Width == cell.Height, "None/artwork native cells are not square."); Require(ReferenceEquals(list.HitTest(new(cell.Left + cell.Width / 2, cell.Top + cell.Height / 2)).Item, item), "Square cell lost its native target.");
                            using var image = Print(list); image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-size{size}-inset{inset}-{item.Index}.png"));
                            if (size == 48) Require(cell.Width > smallSide, "Tile size did not resize None and artwork together.");
                        }
                    }
                    selected.Selected = true;
                    ribbon.Theme = ribbon.Theme with { OptionsAppearance = a with { IconPickerIconSize = 0, IconPickerBorderWidth = 0, IconPickerSelectedBackground = new(Color.Transparent) } }; icon.Bounds.Clear(); using (var hidden = Print(list)) Require(icon.Bounds.Count == 0, "Zero artwork size still drew an icon.");
                    ribbon.Theme = ribbon.Theme with { OptionsAppearance = a with { IconPickerCornerRadius = null, IconPickerInset = null, IconPickerBorderWidth = null, IconPickerBorderColor = Color.Empty, IconPickerSelectedBackground = null, IconPickerIconSize = null, IconPickerTextInset = null } }; using (var reset = Print(list)) reset.Save(Path.Combine(output, $"{scope}-{mode}-{density}-reset.png")); Require(list.Handle == handle && ReferenceEquals(parent, list.Parent) && items.SequenceEqual(list.Items.Cast<ListViewItem>()) && items.All(i => i.GetBounds(ItemBoundsPortion.Icon).Width == i.GetBounds(ItemBoundsPortion.Icon).Height) && ReferenceEquals(selected, list.SelectedItems.Cast<ListViewItem>().Single()), "Picker appearance/reset changed cells, selection or native ownership."); Require(ribbon.Theme.OptionsAppearance is { TreeCheckSize: 12, RowIconSize: 16, FrameBorderWidth: 3 }, "Picker Reset cleared accepted roles."); ((TextBox)dialog.Controls.Find("RibbonGroupName", true).Single()).Text = "Discard"; list.Items["vector"]!.Selected = true;
                }, editor => Require(editor.Session.Draft.Ribbon!.Tabs.Sum(t => t.Groups.Count) == liveGroups, "Group Cancel inserted a draft group."));
                WithPicker(ribbon, owner, (dialog, editor) =>
                {
                    var name = (TextBox)dialog.Controls.Find("RibbonGroupName", true).Single(); var accept = (Button)dialog.Controls.Find("AcceptGroup", true).Single(); name.Text = " "; accept.PerformClick(); Require(dialog.Visible && editor.Session.Draft.Ribbon!.Tabs.Sum(t => t.Groups.Count) == liveGroups, "Invalid group name was accepted."); name.Text = "Accepted sample"; ((ListView)dialog.Controls.Find("RibbonGroupIcons", true).Single()).Items["raster"]!.Selected = true; ((ComboBox)dialog.Controls.Find("RibbonGroupLayout", true).Single()).SelectedItem = RibbonCustomizationGroupLayout.Large; accept.PerformClick();
                }, editor => { var added = editor.Session.Draft.Ribbon!.Tabs.SelectMany(t => t.Groups).Single(g => g.Label == "Accepted sample"); Require(added.IconCommandId == "raster" && added.Layout == RibbonCustomizationGroupLayout.Large && editor.Session.Draft.Ribbon.Tabs.Sum(t => t.Groups.Count) == liveGroups + 1, "Group OK lost its staged name/icon/layout."); });
                Require(sources.SequenceEqual(ribbon.Tabs) && ribbon.Tabs.Sum(t => t.Groups.Count) == liveGroups && ReferenceEquals(vector, group.Items[0]) && raster.Width == 20, "Detached group edits changed live source ownership."); states++;
            }
            ribbon.Theme = original with { OptionsAppearance = null }; Require(ribbon.Theme.OptionsAppearance is null, "Whole-group Reset failed."); evidence.Add($"PASS {scope}: effective owner={owner.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density profiles through public editor/New Group route; actual native picker paint/radius/direct text, size/zero hide, deduplication, shared square cells, adaptive size/inset/None bounds, retained items/HWND/parent/selection, independent/group Reset, invalid-name rejection, OK/Cancel draft metadata, live sources and borrowed artwork."); owner.Close();
        }
        using (var demo = new DesignerForm()) demo.VerifyOptionsIconPickerDemo(output);
        evidence.Add("PASS demo: requested new Theme effects 10 contains three groups. Tab 9 retains six, tab 8 five, tabs 1-7 seven/five/four/seven/six/three/eight. Public presets/hide/independent/all-picker resets, isolated public-model group editor preview, real ribbon mode/sources and idempotence pass."); evidence.Add("Synchronous offscreen native/package checks at actual 120 DPI; modal children are moved offscreen by the inspection callback. No runtime lifecycle or activation workaround. Physical input/IME, actual VS, external UIA, mixed-monitor and visible pacing remain separate. Caption/title A05 and the closing-title flicker are proposed next; A10 remains separate."); File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static int S(int n, int dpi) => (int)Math.Round(n * dpi / 96d);
    private static Bitmap Print(Control c) { c.PerformLayout(); var image = new Bitmap(c.Width, c.Height); c.DrawToBitmap(image, new(Point.Empty, image.Size)); return image; }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
    private sealed class ProbeIcon : IRibbonIcon { internal readonly List<Rectangle> Bounds = []; public void Draw(Graphics g, Rectangle b, RibbonIconContext c) { Bounds.Add(b); using var ink = new SolidBrush(c.Foreground); g.FillRectangle(ink, b); } }
    private sealed class ProbeRenderer : RibbonOffice2024Renderer { internal int Radii, Texts; public override int GetPopupCornerRadius(RibbonTheme theme, int dpi) { Radii++; return 8; } public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { Texts++; base.DrawText(c, text, color, flags); } }
}
