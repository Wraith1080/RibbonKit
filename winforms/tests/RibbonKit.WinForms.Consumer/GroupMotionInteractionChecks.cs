using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class GroupMotionInteractionChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private delegate bool Visitor(nint window, nint state);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, Visitor visitor, nint state);
    internal static int Run(string directory, bool neutral, bool fadeOnly = false)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles(); WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); var log = new List<string>(); Exception? failure = null; var pointer = WF.Cursor.Position;
        var record = new Recording(); using RibbonRenderer painter = neutral ? new Neutral(record) : new Office(record);
        using var form = new WF.Form { Text = "Independent group motion", Size = new(1250, 760), StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = painter }; var tab = new RibbonTab { Id = "home", Text = "Home" };
        var group = new RibbonGroup { Id = "group", Text = "Native commands", Icon = new Icon(Color.Blue), ReductionPriority = 10 };
        var combo = new RibbonComboBoxItem { Id = "font", Text = "Font", IsEditable = true, Value = "Live value", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var toggle = new RibbonToggleItem { Id = "bold", Text = "Bold", Icon = new Icon(Color.Red), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        group.Items.AddRange(toggle, combo, new RibbonButtonItem { Id = "copy", Text = "Copy", Icon = new Icon(Color.Red), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline });
        var other = new RibbonGroup { Id = "other", Text = "Other", CanResize = false }; other.Items.Add(new RibbonButtonItem { Id = "next", Text = "Next command" }); tab.Groups.AddRange(group, other); ribbon.Tabs.Add(tab); ribbon.CommandCatalog.CaptureDefaults();
        var document = new WF.RichTextBox { Dock = WF.DockStyle.Fill, Text = "Application document stays live" }; form.Controls.Add(document); form.Controls.Add(ribbon);
        ribbon.MouseDown += (_, e) => log.Add($"Pointer down {e.Location}; More={record.More}; Contains={record.More.Contains(e.Location)}");
        ribbon.MouseUp += (_, e) => log.Add($"Pointer up {e.Location}; More={record.More}; Contains={record.More.Contains(e.Location)}");
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(150);
                if (GetForegroundWindow() != form.Handle)
                {
                    // Windows may deny a background process's focus request.
                    // Activate this test window through its actual caption.
                    WF.Cursor.Position = new(form.Left + form.Width / 2, form.Top + 18);
                    mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(60);
                }
                Check(GetForegroundWindow() == form.Handle, "Native test host has foreground ownership before exercising motion");
                foreach (var density in Enum.GetValues<RibbonDensity>())
                {
                    ribbon.TabContentAnimation = fadeOnly ? RibbonTabContentAnimation.Fade : RibbonTabContentAnimation.SlideDown;
                    ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.Density = density; ribbon.LayoutMode = RibbonLayoutMode.Classic; form.Width = 1250; ribbon.Refresh();
                    var native = Children(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == combo.Id); nint handle = native.Handle;
                    using var edit = ribbon.CommandCatalog.BeginCustomization("group.motion"); string profile = edit.ExportJson();
                    int narrow = 0;
                    for (int width = 1000; width >= 250; width -= 20) { form.Width = width; ribbon.Refresh(); if (record.Collapsed.GetValueOrDefault(group)) { narrow = width; break; } }
                    Check(narrow > 0, "Classic collapse threshold found"); form.Width = 1250; ribbon.Refresh();
                    ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; form.Width = narrow;
                    Check(ribbon.IsAnimating && record.Collapsed[group], density + " Classic collapse starts during real host resize"); form.Width -= 2; Check(ribbon.IsAnimating, "Continued host resizing retains group playback"); await Task.Delay(35); Capture(density + "-classic-collapse"); await Finish();
                    form.Width = 1250; Check(ribbon.IsAnimating && !record.Collapsed[group], "Classic expansion starts"); await Task.Delay(35); Capture(density + "-classic-expand"); await Finish();
                    Check(native.Handle == handle && combo.Value == "Live value", "Classic endpoints retain editor HWND and text");
                    ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.LayoutMode = RibbonLayoutMode.Simplified; ribbon.Refresh();
                    ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; form.Width = 260;
                    Check(ribbon.IsAnimating && !native.Visible, "Simplified width overflow starts leftward motion"); await Finish();
                    form.Width = 1250; Check(ribbon.IsAnimating, "Simplified width restoration starts rightward motion"); await Finish();
                    Check(native.Visible && native.Handle == handle, "Width restoration retains the original native field");
                    ribbon.AnimationLevel = RibbonAnimationLevel.None;
                    await Click(ribbon, record.More); var more = Popup(); more.Refresh(); ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
                    await Click(more, record.Entries[group]); Check(ribbon.IsAnimating, "Simplified full-group collapse starts"); Capture(density + "-simplified-collapse"); await Finish();
                    Check(record.Collapsed[group], "Simplified medium group button remains after playback"); combo.Value = "Changed"; toggle.Checked = true;
                    await Click(ribbon, record.Groups[group]); Check(ribbon.IsAnimating, "Clicking the medium group button starts rightward restoration"); Capture(density + "-simplified-restore"); await Finish();
                    Check(!record.Collapsed.GetValueOrDefault(group) && native.Handle == handle && Children(ribbon).Contains(native) && combo.Value == "Changed" && toggle.Checked, "Restored live controls keep latest values and checked state");
                    Check(profile == edit.ExportJson(), "Group motion does not change customization JSON"); combo.Value = "Live value"; toggle.Checked = false;
                    ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsMinimized = true; ribbon.Refresh(); await Click(ribbon, record.Tab); var body = Popup(); body.Refresh(); await Click(body, record.More); more = Popup(); more.Refresh(); ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
                    await Click(more, record.Entries[group]); Check(ribbon.IsAnimating, "Temporary minimized row collapse starts"); await Finish();
                    await Click(body, record.Groups[group]); Check(ribbon.IsAnimating, "Temporary minimized row restore starts"); await Finish();
                    Check(Children(body).Contains(native) && native.Handle == handle, "Temporary row restores its original editor");
                    WF.SendKeys.SendWait("{ESC}"); await Task.Delay(40); ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsMinimized = false;
                }
                log.Add($"PASS: {(neutral ? "neutral" : "Office2024")} {ribbon.TabContentAnimation} group motion at actual {ribbon.DeviceDpi} DPI");
            }
            catch (Exception e) { failure = e; log.Add(e.ToString()); log.Add($"More={record.More}, ribbon={ribbon.Bounds}, foreground={WF.Control.FromHandle(GetForegroundWindow())}"); try { Capture("failure"); } catch (Exception capture) { log.Add(capture.Message); } }
            finally { WF.Cursor.Position = pointer; File.WriteAllLines(Path.Combine(directory, "result.log"), log); form.Close(); }
        };
        WF.Application.Run(form); foreach (var line in log) Console.WriteLine(line); return failure is null ? 0 : 1;
        void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException(text); log.Add("PASS: " + text); }
        async Task Finish() { for (int i = 0; ribbon.IsAnimating && i < 100; i++) await Task.Delay(10); Check(!ribbon.IsAnimating, "Library completes without host acknowledgement"); }
        async Task Click(WF.Control surface, Rectangle bounds) { WF.Cursor.Position = surface.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(20); mouse_event(2, 0, 0, 0, 0); await Task.Delay(20); mouse_event(4, 0, 0, 0, 0); await Task.Delay(35); }
        void Capture(string name) { Check(GetForegroundWindow() == form.Handle || WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown, "Capture belongs to the host/popup"); using var image = new Bitmap(form.Width, form.Height); using var g = Graphics.FromImage(image); g.CopyFromScreen(form.Location, Point.Empty, form.Size); image.Save(Path.Combine(directory, name + ".png")); }
    }
    private static IEnumerable<WF.Control> Children(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Children(c)));
    private static WF.Control Popup() { WF.ToolStripDropDown? top = null; EnumThreadWindows(GetCurrentThreadId(), (h, _) => { if (WF.Control.FromHandle(h) is WF.ToolStripDropDown { Visible: true } p) top ??= p; return true; }, 0); return top?.Items.OfType<WF.ToolStripControlHost>().Single().Control ?? throw new InvalidOperationException("Expected popup"); }
    private sealed class Icon(Color color) : IRibbonIcon { public void Draw(Graphics g, Rectangle bounds, RibbonIconContext context) { using var brush = new SolidBrush(color); g.FillRectangle(brush, Rectangle.Inflate(bounds, -5, -5)); } }
    private sealed class Recording { internal Rectangle More, Tab; internal readonly Dictionary<RibbonGroup, bool> Collapsed = []; internal readonly Dictionary<RibbonGroup, Rectangle> Groups = [], Entries = []; }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawSurface(RibbonPaintContext c, bool popup) { if (!popup) r.Collapsed.Clear(); base.DrawSurface(c, popup); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab t, bool selected) { r.Tab = c.Bounds; base.DrawTab(c, t, selected); }
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup g, bool collapsed) { r.Collapsed[g] = collapsed; r.Groups[g] = c.Bounds; base.DrawGroup(c, g, collapsed); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.More = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement e, bool menu) { if (e is RibbonQuickAccessGroupReference { Source: { } group }) r.Entries[group] = c.Bounds; base.DrawQuickAccessItem(c, e, menu); }
    }
    private sealed class Neutral(Recording r) : RibbonRenderer
    {
        public override void DrawSurface(RibbonPaintContext c, bool popup) { if (!popup) r.Collapsed.Clear(); base.DrawSurface(c, popup); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab t, bool selected) { r.Tab = c.Bounds; base.DrawTab(c, t, selected); }
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup g, bool collapsed) { r.Collapsed[g] = collapsed; r.Groups[g] = c.Bounds; base.DrawGroup(c, g, collapsed); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.More = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement e, bool menu) { if (e is RibbonQuickAccessGroupReference { Source: { } group }) r.Entries[group] = c.Bounds; base.DrawQuickAccessItem(c, e, menu); }
    }
}
