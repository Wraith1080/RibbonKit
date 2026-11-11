using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class PopupAppearanceCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop;
            using var painter = new Probe();
            using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1200, 700), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None };
            var menu = new RibbonDropDownItem { Text = "Menu", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained popup text" };
            var nested = new RibbonDropDownItem { Text = "Nested" }; nested.Items.Add(new RibbonButtonItem { Text = "Child command" }); menu.Items.AddRange(field, nested);
            var combo = new RibbonComboBoxItem { Text = "Choices", SimplifiedSize = RibbonItemSize.Medium, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; combo.Items.AddRange("Alpha", "Beta", "Gamma"); combo.SelectedIndex = 0;
            using var body = new Panel { Size = new(260, 100) };
            var borrowedText = new TextBox { Text = "Borrowed input", Location = new(12, 12), Width = 210 }; body.Controls.Add(borrowedText);
            var borrowed = new RibbonDropDownItem { Text = "Content", Content = body, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var group = new RibbonGroup { Text = "Popups", CanResize = false }; group.Items.AddRange(menu, combo, borrowed); var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
            var original = ribbon.Theme; var settings = new RibbonPopupAppearanceSettings { BorderWidth = 3, ShadowHorizontalOffset = 6, ShadowFalloff = 1.4 };
            nint editorHandle = 0; Control? editorParent = null; int states = 0;
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original; menu.ShowDropDown(); Application.DoEvents();
                var baseline = WindowWithText("Retained popup text"); var baselineContent = baseline.Items.OfType<ToolStripControlHost>().Single().Control.Size;
                var editor = Descendants(baseline).OfType<TextBox>().Single(control => control.Text == field.Value);
                if (editorHandle == 0) { editorHandle = editor.Handle; editorParent = editor.Parent; }
                editor.Select(2, 4); menu.CloseDropDown();
                ribbon.Theme = original with { PopupAppearance = settings }; painter.Check = true; int framesBefore = painter.Frames;
                menu.ShowDropDown(); Application.DoEvents(); var window = WindowWithText("Retained popup text"); window.Location = new(-16000, -16000);
                using (var image = new Bitmap(window.Width, window.Height)) { window.DrawToBitmap(image, window.ClientRectangle); image.Save(Path.Combine(output, $"{scope}-{mode}-{density}.png")); }
                int inset = painter.GetPopupBorderWidth(ribbon.Theme, ribbon.DeviceDpi);
                var content = window.Items.OfType<ToolStripControlHost>().Single().Control;
                Require(window.Padding == new Padding(inset) && content.Location == new Point(inset, inset), "Native popup did not reserve its border.");
                Require(content.Size == baselineContent && window.Size == new Size(content.Width + 2 * inset, content.Height + 2 * inset), "Authored border changed the content budget unexpectedly.");
                Require(editor.Handle == editorHandle && ReferenceEquals(editor.Parent, editorParent) && editor.Text == field.Value && editor.SelectionStart == 2 && editor.SelectionLength == 4, "Popup frame replaced native editor state.");
                nested.ShowDropDown(); Application.DoEvents();
                var child = Windows().Single(candidate => !ReferenceEquals(candidate, window)); child.Location = new(-16000, -16000);
                Require(menu.IsDropDownOpen && nested.IsDropDownOpen && child.Padding == new Padding(inset), "Nested popup lost its parent or frame settings.");
                nested.CloseDropDown(); Require(menu.IsDropDownOpen, "Closing child dismissed its parent."); menu.CloseDropDown();
                Require(painter.Failure is null && painter.Frames > framesBefore, "Actual popup frame callback failed: " + painter.Failure); painter.Check = false; states++;
            }
            painter.Check = true;
            nint borrowedHandle = borrowedText.Handle; borrowedText.Select(1, 4); borrowed.ShowDropDown(); Application.DoEvents();
            var borrowedWindow = WindowWithText("Borrowed input"); borrowedWindow.Location = new(-16000, -16000);
            Require(Descendants(borrowedWindow).Contains(body) && borrowedText.Handle == borrowedHandle && borrowedText.SelectionStart == 1 && borrowedText.SelectionLength == 4, "Borrowed content was replaced or lost selection.");
            borrowed.CloseDropDown(); Require(body.Parent is null && !body.IsDisposed && borrowedText.Text == "Borrowed input", "Borrowed content did not return to its owner.");
            var comboEditor = Descendants(ribbon).OfType<ComboBox>().Single(control => control.Text == "Alpha"); nint comboHandle = comboEditor.Handle;
            combo.ShowDropDown(); Application.DoEvents(); var choiceWindow = Windows().Single(); choiceWindow.Location = new(-16000, -16000);
            var list = Descendants(choiceWindow).OfType<ListBox>().Single(); int border = painter.GetPopupBorderWidth(ribbon.Theme, ribbon.DeviceDpi);
            Require(choiceWindow.Padding == new Padding(border) && list.Width == choiceWindow.Width - 2 * border && list.Height == choiceWindow.Height - 2 * border, "Native choice list did not reserve its border.");
            list.SelectedIndex = 1; var key = Message.Create(comboEditor.Handle, 0x100, (nint)(int)Keys.Enter, 0); Require(comboEditor.PreProcessMessage(ref key), "Choice commit key was not handled.");
            Require(combo.SelectedIndex == 1 && !combo.IsDropDownOpen && comboEditor.Handle == comboHandle, "Choice commit changed its retained editor owner."); painter.Check = false;
            Require(painter.Failure is null, "Native frame/shadow replay failed: " + painter.Failure);
            int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
            Require(painter.GetPopupShadowPadding(ribbon.Theme, ribbon.DeviceDpi) == S(6) + S(6), "Horizontal shadow clearance was not included.");
            using (var image = new Bitmap(S(340), S(220)))
            {
                using var g = Graphics.FromImage(image); var c = new RibbonPaintContext(g, new(S(30), S(30), S(270), S(150)), ribbon.Theme, ribbon.Font, ribbon.DeviceDpi);
                painter.DrawPopupShadow(c); painter.DrawPopupFrame(c); image.Save(Path.Combine(output, scope + "-frame-shadow.png"));
            }
            ribbon.Theme = ribbon.Theme with { PopupAppearance = settings with { BorderWidth = null } }; Require(ribbon.Theme.PopupAppearance!.ShadowFalloff == 1.4, "Member reset cleared another popup setting.");
            ribbon.Theme = original; Require(ribbon.Theme.PopupAppearance is null && editorHandle != 0 && !body.IsDisposed, "Group reset lost an owner.");
            evidence.Add($"PASS {scope}: effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density states, actual frame/shadow callbacks, border/content/nested geometry, native text HWND/parent/value/selection, borrowed body return, retained choice commit and independent/group reset. Native shadow policy={SystemInformation.IsDropShadowEnabled}."); form.Close();
        }
        using (var demo = new DesignerForm()) demo.VerifyPopupAppearanceDemo(output);
        evidence.Add("PASS demo: Try popup frames / Popup frame effects on Theme effects 4, public buttons/reset/idempotence, existing editor groups and earlier tabs retained.");
        evidence.Add("Offscreen public-package checks. Actual VS, physical input/IME, visible frame pacing and mixed-monitor acceptance remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static ToolStripDropDown WindowWithText(string text) => Windows().Single(window => Descendants(window).OfType<TextBox>().Any(control => control.Text == text));
    private static List<ToolStripDropDown> Windows()
    {
        var result = new List<ToolStripDropDown>();
        EnumWindow callback = (handle, _) => { if (Control.FromHandle(handle) is ToolStripDropDown { Visible: true } window) result.Add(window); return true; };
        EnumThreadWindows(GetCurrentThreadId(), callback, 0); GC.KeepAlive(callback); return result;
    }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(control => new[] { control }.Concat(Descendants(control)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private delegate bool EnumWindow(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, nint parameter);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal bool Check;
        internal int Frames;
        internal Exception? Failure;
        public override void DrawPopupFrame(RibbonPaintContext c)
        {
            Frames++;
            if (Check) Safely(() =>
            {
                using var image = new Bitmap(c.Bounds.Width, c.Bounds.Height); using var g = Graphics.FromImage(image);
                base.DrawPopupFrame(c with { Graphics = g, Bounds = new(Point.Empty, image.Size) });
                Require(image.GetPixel(0, image.Height / 2).ToArgb() == c.Theme.PopupBorder.ToArgb(), "Actual border paint was bypassed.");
                int stroke = GetPopupBorderWidth(c.Theme, c.DeviceDpi);
                var surface = c.Theme.PopupBackground.IsEmpty ? c.Theme.Surface : c.Theme.PopupBackground;
                Require(image.GetPixel(stroke, image.Height / 2).ToArgb() == surface.ToArgb(), "Actual border width was bypassed.");
            });
            base.DrawPopupFrame(c);
        }
        public override void DrawPopupShadow(RibbonPaintContext c)
        {
            if (Check) Safely(() =>
            {
                int padding = GetPopupShadowPadding(c.Theme, c.DeviceDpi);
                using var image = new Bitmap(c.Bounds.Width + 2 * padding + 2, c.Bounds.Height + 2 * padding + 2); using var g = Graphics.FromImage(image);
                base.DrawPopupShadow(c with { Graphics = g, Bounds = new(padding + 1, padding + 1, c.Bounds.Width, c.Bounds.Height) });
                Require(image.GetPixel(0, image.Height / 2).A == 0 && image.GetPixel(image.Width - 1, image.Height / 2).A == 0, "Shadow escaped its reserved clearance.");
            });
            base.DrawPopupShadow(c);
        }
        private void Safely(Action action) { try { action(); } catch (Exception error) { Failure ??= error; } }
    }
}
