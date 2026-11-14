using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class OptionsFieldCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop; using var painter = new Probe(); using var owner = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1200, 700), ShowInTaskbar = false, Backdrop = backdrop }; using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None }; owner.Controls.Add(ribbon); owner.Ribbon = ribbon;
            foreach (string name in new[] { "Home", "Insert" }) { var tab = new RibbonTab { Id = name, Text = name }; var group = new RibbonGroup { Id = name + ".commands", Text = "Commands" }; group.Items.Add(new RibbonButtonItem { Id = name + ".save", Text = name + " Save" }); tab.Groups.Add(group); ribbon.Tabs.Add(tab); } ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = (RibbonButtonItem)ribbon.Tabs[0].Groups[0].Items[0] }); ribbon.CommandCatalog.CaptureDefaults(); owner.Show(); Application.DoEvents(); var original = ribbon.Theme; int states = 0, choices = 0;
            var appearance = new RibbonOptionsAppearanceSettings { FrameCornerRadius = 12, FrameBorderWidth = 3, FrameBorderColor = Color.Navy, ButtonBorderWidth = 2, HeadingBold = false, NavigationCornerRadius = 12 };
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original with { OptionsAppearance = appearance, EditorTextInset = 12, EditorForeground = Color.DarkGreen, EditorAppearance = new() { CornerRadius = 8, BorderWidth = 3, Inset = 2, ChoiceCornerRadius = 6, ChoiceTextHorizontalPadding = 14 } };
                using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "RibbonKit.Fields.Check") { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; dialog.Show(owner); Application.DoEvents(); var draft = dialog.Editor.Session.Draft; var sources = ribbon.QuickAccessItems.ToArray(); int frames = 0;
                foreach (var page in dialog.Pages)
                {
                    dialog.SelectedPage = page; Application.DoEvents(); foreach (var frame in Descendants(page).Where(c => c.GetType().Name == "RibbonOptionsFrame"))
                    {
                        frame.PerformLayout(); int stroke = (int)Math.Round(3 * frame.DeviceDpi / 96d); Require(frame.Padding.Left == stroke, "Native frame clearance did not reserve the authored stroke.");
                        using var image = Print(frame); Require(image.GetPixel(0, image.Height / 2).ToArgb() == Color.Navy.ToArgb(), "A native child covered the frame outline.");
                        foreach (var child in frame.Controls.Cast<Control>()) { nint handle = child.Handle; var parent = child.Parent; frame.PerformLayout(); Require(child.Handle == handle && ReferenceEquals(parent, child.Parent), "Frame layout replaced or reparented a native child."); if (child is TextBox input) Require(input.Left >= frame.Padding.Left && input.Top >= frame.Padding.Top && input.Bottom <= frame.Height - frame.Padding.Bottom && input.ForeColor == Color.DarkGreen, "Native search text crossed its frame clearance."); else Require(child.Left == frame.Padding.Left && child.Top == frame.Padding.Top && child.Region is not null, "Native list lost its inset/corner region."); }
                        frames++;
                    }
                    if (mode == RibbonLayoutMode.Simplified && Descendants(page).OfType<ComboBox>().FirstOrDefault(c => c.Name == "SimplifiedSourceTab") is { } combo)
                    {
                        painter.Texts.Clear(); using (var image = Print(combo)) image.Save(Path.Combine(output, $"{scope}-{density}-choice.png")); int inset = (int)Math.Round(12 * combo.DeviceDpi / 96d); Require(painter.Texts.Any(t => t.Color == Color.DarkGreen && t.Bounds.Left == inset && t.Bounds.Top > 0), "Actual selected-value text bypassed editor roles or stroke clearance.");
                        dialog.Activate(); Require(combo.Focus(), "Choice fixture could not establish native focus."); int selected = combo.SelectedIndex, changed = 0; combo.SelectedIndexChanged += (_, _) => changed++; combo.DroppedDown = true; var popup = Popup(); var list = Descendants(popup).OfType<ListBox>().Single(); using (var image = Print(list)) image.Save(Path.Combine(output, $"{scope}-{density}-rows.png")); Key(combo, Keys.Down); Require(combo.SelectedIndex == selected && changed == 0, "Arrow preview committed early."); Key(combo, Keys.Escape); Require(combo.SelectedIndex == selected && changed == 0, "Escape did not discard the staged choice."); combo.DroppedDown = true; Key(combo, Keys.Down); Key(combo, Keys.Enter); Require(combo.SelectedIndex == Math.Min(selected + 1, combo.Items.Count - 1) && changed == 1 && ReferenceEquals(dialog, combo.FindForm()), "Accepted choice lost selection/event/input ownership."); choices++;
                    }
                    using var pixels = Print(page); pixels.Save(Path.Combine(output, $"{scope}-{mode}-{density}-{page.Name}.png"));
                }
                Require(frames >= 8 && ReferenceEquals(draft, dialog.Editor.Session.Draft) && sources.SequenceEqual(ribbon.QuickAccessItems), "Frame/choice work replaced the draft or live sources."); var handles = Descendants(dialog).Where(c => c is TextBox or ListBox or TreeView or ComboBox).ToDictionary(c => c, c => c.Handle); ribbon.Theme = original; foreach (var frame in Descendants(dialog).Where(c => c.GetType().Name == "RibbonOptionsFrame")) frame.PerformLayout(); Require(handles.All(p => !p.Key.IsDisposed && p.Key.Handle == p.Value), "Reset replaced native controls."); Descendants(dialog).OfType<Button>().Single(b => b.Name == "CancelCustomization").PerformClick(); Require(sources.SequenceEqual(ribbon.QuickAccessItems), "Cancel applied a customization draft."); states++;
            }
            ribbon.Theme = original with { OptionsAppearance = appearance with { FrameCornerRadius = null, FrameBorderWidth = null, FrameBorderColor = Color.Empty } }; Require(ribbon.Theme.OptionsAppearance!.ButtonBorderWidth == 2 && ribbon.Theme.OptionsAppearance.HeadingBold == false && ribbon.Theme.OptionsAppearance.NavigationCornerRadius == 12, "Frame member Reset cleared accepted roles."); ribbon.Theme = ribbon.Theme with { OptionsAppearance = null }; Require(ribbon.Theme.OptionsAppearance is null, "Group Reset failed.");
            evidence.Add($"PASS {scope}: effective owner={owner.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density states; actual native frame/child paint, stroke clearance and regions, editor text roles/field clearance, retained HWND/parent/draft/live sources and Cancel, {choices} active choice preview/Escape/Enter sequences with one commit event; independent/group Reset."); owner.Close();
        }
evidence.Add("Synchronous offscreen public-package checks before queued foreground-loss dismissal; no runtime activation/choice-lifetime workaround. Physical input/IME, actual VS, mixed-monitor, external UIA and visible pacing remain separate. Presets are chosen before opening; existing layout/font/DPI refresh applies frame geometry."); File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static void Key(Control control, Keys key) { var message = Message.Create(control.Handle, 0x100, (nint)(int)key, 0); Require(control.PreProcessMessage(ref message), "Choice key was not handled: " + key); }
    private static ToolStripDropDown Popup() { var windows = new List<ToolStripDropDown>(); EnumWindow callback = (h, _) => { if (Control.FromHandle(h) is ToolStripDropDown { Visible: true } window) windows.Add(window); return true; }; EnumThreadWindows(GetCurrentThreadId(), callback, 0); GC.KeepAlive(callback); return windows.Single(); }
    private static Bitmap Print(Control control) { var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, new(Point.Empty, image.Size)); return image; }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private delegate bool EnumWindow(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, nint parameter);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private sealed class Probe : RibbonOffice2024Renderer
    { internal readonly List<(Rectangle Bounds, Color Color)> Texts = []; public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { Texts.Add((c.Bounds, color)); base.DrawText(c, text, color, flags); } }
}
