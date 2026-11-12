using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class ApplicationMenuChromeCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop; using var painter = new Probe();
            using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1200, 700), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None, FileMenuMode = RibbonFileMenuMode.CommandMenu };
            ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
            var split = new RibbonSplitButtonItem { Text = "Save As", CheckOnClick = true }; split.Items.Add(new RibbonButtonItem { Text = "Copy" });
            using var body = new UserControl { Size = new(300, 130) }; var editor = new TextBox { Text = "Retained application text", Bounds = new(12, 12, 250, 24) }; body.Controls.Add(editor);
            var content = new RibbonDropDownItem { Text = "Open", Content = body, DropDownHeader = "Open options" };
            ribbon.FileMenu.Items.AddRange(split, content, new RibbonButtonItem { Text = "Save" }, new RibbonButtonItem { Text = "Disabled", Enabled = false });
            var options = new RibbonButtonItem { Text = "Options" }; var footer = new RibbonSplitButtonItem { Text = "Footer split", Checked = true }; footer.Items.Add(new RibbonButtonItem { Text = "Footer child" }); ribbon.ApplicationMenu.FooterItems.AddRange(options, footer);
            ribbon.ApplicationMenu.DefaultPane.Items.Add(new RibbonButtonItem { Text = "Recent document" });
            int primaryActions = 0, footerActions = 0; split.Click += (_, _) => primaryActions++; options.Click += (_, _) => footerActions++;
            form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents(); var original = ribbon.Theme;
            var settings = new RibbonApplicationMenuAppearanceSettings { RowCornerRadius = 8, RowInset = 3, RowBorderWidth = 2, RowBorderColor = Color.DarkBlue,
                FooterInset = 3, FooterBorderWidth = 2, FooterBorderColor = Color.DarkOrange, SplitDividerWidth = 3, SplitDividerInset = 6, SplitDividerColor = Color.Magenta,
                SelectedPrimaryTintStrength = .55, SelectedPrimaryBackground = new(Color.LightSkyBlue) };
            nint handle = 0; Control? editorParent = null; int states = 0;
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original; form.Activate(); ribbon.Focus(); Application.DoEvents();
                ribbon.FileMenu.ShowDropDown(); split.ShowDropDown(); Application.DoEvents();
                var windows = Windows(); Require(windows.Count == 1, $"{scope}/{mode}/{density}: expected one baseline window; found {windows.Count}; File open={ribbon.FileMenu.IsDropDownOpen}, split open={split.IsDropDownOpen}, host active={Form.ActiveForm == form}.");
                var surface = Content(windows.Single()); painter.Slots.Clear(); using (var before = Print(surface)) { }
                var size = surface.Size; var slots = painter.Slots.ToDictionary(pair => pair.Key, pair => pair.Value); ribbon.FileMenu.CloseDropDown();
                ribbon.Theme = original with { ApplicationMenuAppearance = settings, ApplicationMenuSplitDividerVisible = true }; painter.Check = true; painter.Rows = painter.Footers = painter.Splits = 0;
                ribbon.FileMenu.ShowDropDown(); split.ShowDropDown(); Application.DoEvents(); var window = Windows().Single(); window.Location = new(-16000, -16000); surface = Content(window);
                using (var image = Print(surface)) image.Save(Path.Combine(output, $"{scope}-{mode}-{density}.png"));
                Require(surface.Size == size && slots.All(pair => painter.Slots.TryGetValue(pair.Key, out var value) && value == pair.Value), "Application chrome changed measured command geometry.");
                Require(painter.Failure is null && painter.Rows > 0 && painter.Footers > 0 && painter.Splits > 0, "Actual Application Menu callback failed: " + painter.Failure); painter.Check = false;
                bool oldChecked = split.Checked; Invoke(surface, "Save As"); Require(primaryActions == states + 1 && split.Checked != oldChecked && !ribbon.FileMenu.IsDropDownOpen, "Split primary invocation did not retain its source/toggle/dismissal contract.");
                ribbon.FileMenu.ShowDropDown(); content.ShowDropDown(); Application.DoEvents();
                if (handle == 0) { handle = editor.Handle; editorParent = editor.Parent; }
                editor.Select(2, 5); Require(editor.Handle == handle && ReferenceEquals(editor.Parent, editorParent) && editor.Text == "Retained application text", "Native pane identity/value changed.");
                split.ShowDropDown(); content.ShowDropDown(); Application.DoEvents();
                Require(editor.Handle == handle && editor.SelectionStart == 2 && editor.SelectionLength == 5 && ReferenceEquals(content.Content, body), "Pane switching replaced native content or selection.");
                surface = Content(Windows().Single()); Invoke(surface, "Options"); Require(footerActions == states + 1 && !ribbon.FileMenu.IsDropDownOpen && body.Parent is null && !body.IsDisposed, "Footer action or borrowed-content return changed."); states++;
            }
            ribbon.Theme = ribbon.Theme with { ApplicationMenuAppearance = settings with { SelectedPrimaryBackground = null } };
            Require(ribbon.Theme.ApplicationMenuAppearance!.SelectedPrimaryTintStrength == .55 && ribbon.Theme.ApplicationMenuAppearance.SplitDividerWidth == 3, "Selected fill reset cleared another role.");
            ribbon.Theme = original; Require(ribbon.Theme.ApplicationMenuAppearance is null && editor.Handle == handle, "Group reset replaced native content.");
            evidence.Add($"PASS {scope}: effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density states, actual row/footer/split callbacks, authored outlines/dividers/selected fill, unchanged slots, accessible primary/footer actions and dismissal, retained native pane HWND/parent/value/selection and source identity, member/group reset."); form.Close();
        }
        using (var demo = new DesignerForm()) demo.VerifyApplicationMenuChromeDemo(output);
        evidence.Add("PASS demo: new Theme effects 5, earlier tabs retain seven/five/four/seven groups; public presets/member/group reset, File split preview and File-mode restore, idempotence checked.");
        evidence.Add("Offscreen public-package checks through WinForms AccessibleObject. External UIA, actual VS, physical input/IME, visible frame pacing and mixed-monitor acceptance remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static void Invoke(Control surface, string name)
    {
        var root = surface.AccessibilityObject; int count = root.GetChildCount(); Require(count >= 0, "Application Menu accessibility children were unavailable.");
        Enumerable.Range(0, count).Select(root.GetChild).Single(child => child?.Name == name)!.DoDefaultAction(); Application.DoEvents();
    }
    private static Control Content(ToolStripDropDown window) => window.Items.OfType<ToolStripControlHost>().Single().Control;
    private static Bitmap Print(Control control) { var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, control.ClientRectangle); return image; }
    private static List<ToolStripDropDown> Windows()
    {
        var result = new List<ToolStripDropDown>(); EnumWindow callback = (handle, _) => { if (Control.FromHandle(handle) is ToolStripDropDown { Visible: true } window) result.Add(window); return true; };
        EnumThreadWindows(GetCurrentThreadId(), callback, 0); GC.KeepAlive(callback); return result;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private delegate bool EnumWindow(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, nint parameter);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal bool Check;
        internal int Rows, Footers, Splits;
        internal Exception? Failure;
        internal readonly Dictionary<string, Rectangle> Slots = [];
        private bool _replay;
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu)
        {
            Slots["item:" + item.Text] = c.Bounds;
            if (Check && item.Text == "Options") Safely(() =>
            {
                using var image = new Bitmap(c.Bounds.Width + 8, c.Bounds.Height + 8); using var g = Graphics.FromImage(image); g.Clear(Color.White);
                _replay = true; try { base.DrawItem(c with { Graphics = g, Bounds = new(4, 4, c.Bounds.Width, c.Bounds.Height), Hot = false, Pressed = false, Focused = false }, item, size, menu); } finally { _replay = false; }
                Require(image.GetPixel(4 + S(3, c), 4 + c.Bounds.Height / 2).ToArgb() == Color.DarkOrange.ToArgb(), "Footer outline was bypassed."); Footers++;
            });
            base.DrawItem(c, item, size, menu);
        }
        public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext arrow, RibbonSplitButtonItem item, RibbonItemSize size, bool menu)
        {
            var whole = Rectangle.Union(primary.Bounds, arrow.Bounds); Slots["split:" + item.Text] = whole;
            if (Check && (item.Text is "Save As" or "Footer split") && (item.IsDropDownOpen || item.Checked)) Safely(() =>
            {
                bool footer = item.Text == "Footer split";
                using var image = new Bitmap(whole.Width + 8, whole.Height + 8); using var g = Graphics.FromImage(image); g.Clear(Color.White);
                var p = primary.Bounds; p.Offset(4 - whole.Left, 4 - whole.Top); var a = arrow.Bounds; a.Offset(4 - whole.Left, 4 - whole.Top);
                _replay = true;
                try { base.DrawSplitItem(primary with { Graphics = g, Bounds = p, Hot = false, Pressed = false, Focused = false }, arrow with { Graphics = g, Bounds = a, Hot = false, Pressed = false, Focused = false }, item, size, menu); }
                finally { _replay = false; }
                Require(image.GetPixel(4 + S(3, primary), 4 + whole.Height / 2).ToArgb() == (footer ? Color.DarkOrange : Color.DarkBlue).ToArgb(), $"{item.Text} split perimeter outline was bypassed.");
                Require(Ink(image, Color.Magenta) == new Rectangle(a.Left, 4 + S(6, primary), S(3, primary), whole.Height - 2 * S(6, primary)), "Split divider width/clearance/ink was bypassed.");
                if (!footer && item.IsDropDownOpen) Require(image.GetPixel(4 + S(10, primary), 4 + whole.Height / 2).ToArgb() == Color.LightSkyBlue.ToArgb(), "Selected-primary fill was bypassed.");
                if (footer) Splits++; else Rows++;
            });
            base.DrawSplitItem(primary, arrow, item, size, menu);
        }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { if (!_replay) base.DrawText(c, text, color, flags); }
        public override void DrawSubmenuArrow(RibbonPaintContext c, Color color) { if (!_replay) base.DrawSubmenuArrow(c, color); }
        public override void DrawFocus(RibbonPaintContext c) { if (!_replay) base.DrawFocus(c); }
        private static int S(int n, RibbonPaintContext c) => (int)Math.Round(n * c.DeviceDpi / 96d);
        private static Rectangle Ink(Bitmap image, Color color)
        {
            var bounds = Rectangle.Empty; for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() == color.ToArgb()) bounds = bounds.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(bounds, new(x, y, 1, 1)); return bounds;
        }
        private void Safely(Action action) { try { action(); } catch (Exception error) { Failure ??= error; } }
    }
}
