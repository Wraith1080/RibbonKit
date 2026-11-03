using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace IndependentConsumer;

// Copied outside the checkout and restored only from the local native package.
internal static class ApplicationMenuChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    internal static int Run(string directory, string mode)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles(); Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
        var material = mode.Contains("Mica") ? RibbonWindowBackdrop.Mica : mode.Contains("Acrylic") ? RibbonWindowBackdrop.Acrylic : RibbonWindowBackdrop.None;
        var density = mode.Contains("Touch") ? RibbonDensity.Touch : RibbonDensity.Compact;
        var record = new Record(); using RibbonRenderer renderer = mode.Contains("Flat") ? new Flat(record) : new Office(record);
        using var form = new RibbonForm { Text = "Independent native Application Menu", Size = new(1000, 720), StartPosition = FormStartPosition.CenterScreen, Backdrop = material };
        var ribbon = new RibbonControl { Renderer = renderer, AnimationLevel = RibbonAnimationLevel.Subtle, Density = density };
        form.Controls.Add(new TextBox { Dock = DockStyle.Fill, Multiline = true, Text = "Application-owned document. File commands, recent data and native content are declared through public APIs." }); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        ribbon.Tabs.Add(new RibbonTab { Text = "Home", KeyTip = "H" }); ribbon.FileMenu.Visible = true;
        using var body = new UserControl { Name = "ApplicationPaneBody", AccessibleName = "Native application pane", Size = new(380, 420), BackColor = Color.White };
        body.Controls.Add(new Label { Text = "Native document options", AutoSize = true, Location = new(16, 18) });
        var editor = new TextBox { Name = "RetainedEditor", AccessibleName = "Retained editor", Text = "Retained", Location = new(16, 55), Width = 270 };
        var combo = new ComboBox { Location = new(16, 95), Width = 270, DropDownStyle = ComboBoxStyle.DropDownList }; combo.Items.AddRange(["Document", "Template"]); combo.SelectedIndex = 0;
        body.Controls.AddRange([editor, combo]);
        var open = new RibbonDropDownItem { Text = "Open", KeyTip = "O", DropDownHeader = "Open document", Content = body };
        var save = new RibbonSplitButtonItem { Text = "Save", KeyTip = "S", DropDownKeyTip = "A", CheckOnClick = true, DropDownHeader = "Save a copy" };
        var nested = new RibbonDropDownItem { Text = "Export", KeyTip = "X" }; var leaf = new RibbonButtonItem { Text = "PDF document", KeyTip = "P" }; nested.Items.Add(leaf); save.Items.Add(nested);
        var options = new RibbonButtonItem { Text = "Options", KeyTip = "T" }; var disabled = new RibbonButtonItem { Text = "Unavailable", Enabled = false };
        ribbon.FileMenu.Items.AddRange(open, save, disabled, new RibbonMenuSeparatorItem());
        for (int i = 0; i < 22; i++) ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Additional command " + i });
        ribbon.ApplicationMenu.DefaultPane.DropDownHeader = "Recent documents";
        for (int i = 0; i < 30; i++) ribbon.ApplicationMenu.DefaultPane.Items.Add(new RibbonButtonItem { Text = "Report " + (i + 1), KeyTip = i == 0 ? "R" : "" });
        ribbon.ApplicationMenu.FooterItems.Add(options);
        int actions = 0; bool stateBeforeClick = false; save.Click += (_, _) => { actions++; stateBeforeClick = save.Checked; }; options.Click += (_, _) => actions++; leaf.Click += (_, _) => actions++;
        Exception? failure = null; var log = new List<string>(); var pointer = Cursor.Position; bool complete = false;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(100); Guard();
                Check(form.EffectiveBackdrop == material, "Effective requested backdrop: " + form.EffectiveBackdrop);
                ribbon.Refresh(); await Click(ribbon, record.File); var surface = Surface(); await Capture("default");
                Check(record.Footer.Height > 0 && record.Footer.Top >= record.Navigation.Bottom && record.Footer.Top >= record.Pane.Bottom, "Independent measured columns and fixed reachable footer");
                var ao = surface.AccessibilityObject;
                var unavailable = Children(ao).Single(c => c.Name == disabled.Text); Check((unavailable.State & AccessibleStates.Unavailable) != 0, "Disabled command accessibility state");
                var last = Children(ao).Single(c => c.Name == "Additional command 21"); last.Select(AccessibleSelection.TakeFocus); await Task.Delay(40);
                Check(!last.Bounds.IsEmpty && surface.RectangleToScreen(record.Navigation).IntersectsWith(last.Bounds), "Accessibility focus reveals navigation overflow");
                Check(!Children(ao).Single(c => c.Name == options.Text).Bounds.IsEmpty, "Footer stays reachable while navigation scrolls");
                open.ShowDropDown(); await Task.Delay(80); Check(open.IsDropDownOpen && body.Parent is not null, "Public ShowDropDown selects retained native pane");
                editor.Focus(); var handle = editor.Handle; await Key("{END}!"); Check(editor.Text == "Retained!", "Native editor accepts keyboard messages"); await Capture("native-pane");
                Check(Children(ao).Any(c => c.Name == "Application menu content"), "Native content accessibility region is present alongside commands");
                save.ShowDropDown(); await Task.Delay(80); Check(body.Parent is null && ribbon.FileMenu.IsDropDownOpen && save.IsDropDownOpen, "Switching panes detaches and retains one native body");
                await Capture("save-pane");
                nested.ShowDropDown(); await Task.Delay(80); Check(nested.IsDropDownOpen, "Right-pane nested dropdown uses native submenu stack");
                await Key("{ESC}"); Check(!nested.IsDropDownOpen && ribbon.FileMenu.IsDropDownOpen, "Escape dismisses only nested submenu");
                open.ShowDropDown(); await Task.Delay(60); Check(editor.Handle == handle && editor.Text == "Retained!", "Switching back preserves HWND and text");
                await Key("{ESC}"); Check(!ribbon.FileMenu.IsDropDownOpen && body.Parent is null && !body.IsDisposed, "Escape closes File and returns borrowed content");
                await Key("{F10}"); await Key("f"); await Key("o"); Check(open.IsDropDownOpen && editor.Handle == handle, "File and pane KeyTips reopen the retained native body");
                await Key("{ESC}"); ribbon.FileMenu.ShowDropDown(); await Task.Delay(60); save.ShowDropDown(); await Task.Delay(60);
                var saveRow = Children(Surface().AccessibilityObject).Single(c => c.Name == save.Text); saveRow.DoDefaultAction(); await Task.Delay(50);
                Check(actions == 1 && stateBeforeClick && !ribbon.FileMenu.IsDropDownOpen, "Split primary invokes once, updates state before Click and dismisses File");
                ribbon.FileMenu.ShowDropDown(); await Task.Delay(50); var active = Surface();
                await Click(active, record.Items[options]); Check(actions == 2 && !ribbon.FileMenu.IsDropDownOpen, "Pointer footer action invokes once and closes");
                ribbon.FileMenu.ShowDropDown(); await Task.Delay(50); await Click(form, new(850, 630, 40, 30)); Check(!ribbon.FileMenu.IsDropDownOpen, "Outside click dismisses File");
                log.Add($"PASS: actual DPI={ribbon.DeviceDpi}, density={density}, renderer={renderer.GetType().Name}, effective={form.EffectiveBackdrop}; public package only"); complete = true;
            }
            catch (Exception error) { failure = error; log.Add("FAIL: " + error); }
            finally { ribbon.FileMenu.CloseDropDown(); form.Close(); }
        };
        try { Application.Run(form); } finally { Cursor.Position = pointer; }
        File.WriteAllLines(Path.Combine(directory, "result.log"), log);
        return complete && failure is null ? 0 : 1;
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        void Guard() { if (GetForegroundWindow() != form.Handle && Control.FromHandle(GetForegroundWindow()) is not ToolStripDropDown) throw new InvalidOperationException("Fixture lost foreground; input stopped."); }
        async Task Key(string keys) { Guard(); SendKeys.SendWait(keys); await Task.Delay(75); }
        async Task Click(Control control, Rectangle bounds)
        {
            Guard(); bounds.Intersect(control.ClientRectangle); Check(bounds.Width > 0 && bounds.Height > 0, "Pointer target is visible");
            Cursor.Position = control.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(35); Guard();
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(80);
        }
        async Task Capture(string name)
        {
            await Task.Delay(80); Guard(); var bounds = Rectangle.Union(form.Bounds, Surface().TopLevelControl!.Bounds);
            using var pixels = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(pixels);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); pixels.Save(Path.Combine(directory, name + ".png"));
        }
        Control Surface() => Control.FromHandle(GetForegroundWindow()) is ToolStripDropDown window ? window.Items.OfType<ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected native File popup");
    }
    private static IEnumerable<AccessibleObject> Children(AccessibleObject root) => Enumerable.Range(0, root.GetChildCount()).Select(root.GetChild).OfType<AccessibleObject>();
    private sealed class Record { internal Rectangle File, Navigation, Pane, Footer; internal Dictionary<RibbonItem, Rectangle> Items = []; }
    private sealed class Office(Record r) : RibbonOffice2024Renderer
    {
        public override void DrawFileButton(RibbonPaintContext c, RibbonDropDownItem m) { r.File = c.Bounds; base.DrawFileButton(c, m); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem i, RibbonItemSize s, bool m) { r.Items[i] = c.Bounds; base.DrawItem(c, i, s, m); }
        public override void DrawApplicationMenuBackground(RibbonPaintContext c, Rectangle n, Rectangle p, Rectangle f) { r.Navigation = n; r.Pane = p; r.Footer = f; base.DrawApplicationMenuBackground(c, n, p, f); }
    }
    private sealed class Flat(Record r) : RibbonRenderer
    {
        public override void DrawFileButton(RibbonPaintContext c, RibbonDropDownItem m) { r.File = c.Bounds; base.DrawFileButton(c, m); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem i, RibbonItemSize s, bool m) { r.Items[i] = c.Bounds; base.DrawItem(c, i, s, m); }
        public override void DrawApplicationMenuBackground(RibbonPaintContext c, Rectangle n, Rectangle p, Rectangle f) { r.Navigation = n; r.Pane = p; r.Footer = f; base.DrawApplicationMenuBackground(c, n, p, f); }
    }
}
