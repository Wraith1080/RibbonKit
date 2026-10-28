using System.Runtime.InteropServices;
using System.IO;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public APIs only; linked into NativeTests and copied with the independent package consumer.
internal static class MessageInteractionChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, nuint extra);
    internal static int Run(string directory, bool neutral)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        Directory.CreateDirectory(directory); var record = new Recording();
        using RibbonRenderer painter = neutral ? new Neutral(record) : new Office(record);
        using var form = new WF.Form { Text = "Independent message-panel consumer", Width = 1050, Height = 700, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = painter }; var tab = new RibbonTab { Text = "Home", KeyTip = "H" }; var group = new RibbonGroup { Text = "Commands" };
        group.Items.Add(new RibbonButtonItem { Text = "Save" }); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save" });
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Application-owned retained draft" };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        var first = new RibbonMessage { Title = "PROTECTED VIEW", Text = "Review this document before enabling editing.", IsOpen = false };
        first.Action.Text = "Enable editing"; first.Action.KeyTip = "R";
        var second = new RibbonMessage { Text = "A second notification stays independent of the first.", IsOpen = false };
        ribbon.MessageBar.Items.AddRange(first, second); int actions = 0, dismissals = 0;
        first.Action.Click += (_, _) => actions++; first.Dismissed += (_, _) => dismissals++;
        var log = new List<string>(); Exception? error = null; var cursor = WF.Cursor.Position;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle);
                for (int i = 0; i < 30 && GetForegroundWindow() != form.Handle; i++) await Task.Delay(50);
                Guard();
                foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
                foreach (var position in new[] { RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.BelowRibbon })
                foreach (bool minimized in new[] { false, true })
                {
                    first.IsOpen = second.IsOpen = false;
                    ribbon.Density = density; ribbon.QuickAccessPosition = position; ribbon.IsMinimized = minimized; ribbon.Refresh();
                    int baseline = ribbon.Height; document.Focus(); document.Select(2, 4); nint handle = document.Handle;
                    first.IsOpen = second.IsOpen = true; ribbon.Refresh();
                    Check(document.Focused && document.Handle == handle && document.SelectionStart == 2 && document.SelectionLength == 4,
                        "Message arrival preserves application focus, native HWND and text selection");
                    bool connected = !neutral && (!minimized || position == RibbonQuickAccessPosition.BelowRibbon);
                    Check(record.TopConnected == connected && record.Rows.Count == 2 && record.Rows[first].Bottom == record.Rows[second].Top,
                        $"{density}/{position}/{minimized}: stacked messages have the correct attached or exposed top edge");
                    if (minimized && position != RibbonQuickAccessPosition.BelowRibbon)
                        Check(record.Stack.Top == record.Tab.Bottom + (neutral ? 0 : S(ribbon.Theme.BodyMargin.Top)), "Minimized body with QAT not BelowRibbon leaves a standalone message panel");
                    if (connected) Check(record.BottomConnected && record.Frame.Bottom == record.Stack.Top, "The preceding body/QAT frame meets the messages with a square lower edge");
                    await Capture($"{density}-{position}-{(minimized ? "minimized" : "expanded")}");
                    int before = actions; ribbon.Focus(); await Key("{F10}R");
                    Check(actions == before + 1 && first.IsOpen, "Action KeyTip invokes exactly once without automatic dismissal");
                    await Click(record.Actions[first.Action]); Check(actions == before + 2 && first.IsOpen, "Pointer action invokes exactly once and retains the message");
                    await Click(record.Closes[first]); Check(!first.IsOpen && second.IsOpen && ribbon.MessageBar.Items.Count == 2, "Pointer dismissal retains the model and its open sibling");
                    await Key("{ENTER}"); Check(!second.IsOpen && !ribbon.MessageBar.HasOpenMessages && ribbon.Height == baseline, "Keyboard dismissal restores the original ribbon height and foot geometry");
                }
                first.IsOpen = true; first.IsDismissible = false; ribbon.IsMinimized = true; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
                form.Width = 420; ribbon.Density = RibbonDensity.Touch; ribbon.Refresh();
                Check(!record.Closes.ContainsKey(first) && record.Stack.Top == record.Tab.Bottom + (neutral ? 0 : S(ribbon.Theme.BodyMargin.Top)), "Narrow Touch standalone messages omit a nondismissible close target");
                await Capture("Touch-not-below-minimized-narrow");
                first.Dismiss(); Check(!first.IsOpen && dismissals == 9, "Programmatic Dismiss remains idempotent and available for nondismissible messages");
                first.Dismiss(); Check(dismissals == 9, "Repeated dismissal raises no duplicate event");
                log.Add($"PASS: {(neutral ? "neutral" : "Office2024")} public message panels at actual {ribbon.DeviceDpi} DPI");
            }
            catch (Exception e) { error = e; log.Add(e.ToString()); }
            finally { WF.Cursor.Position = cursor; File.WriteAllLines(Path.Combine(directory, "result.log"), log); form.Close(); }
        };
        WF.Application.Run(form); foreach (var line in log) Console.WriteLine(line); return error is null ? 0 : 1;
        int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
        void Check(bool ok, string text) { if (!ok) throw new InvalidOperationException(text); log.Add("PASS: " + text); }
        void Guard() { if (GetForegroundWindow() != form.Handle) throw new InvalidOperationException("Message consumer lost foreground focus."); }
        async Task Key(string keys) { Guard(); WF.SendKeys.SendWait(keys); await Task.Delay(60); ribbon.Refresh(); }
        async Task Click(Rectangle bounds)
        {
            Check(!bounds.IsEmpty && ribbon.ClientRectangle.Contains(bounds), "Measured pointer target is visible"); Guard();
            WF.Cursor.Position = ribbon.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(30);
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(60); ribbon.Refresh();
        }
        async Task Capture(string name)
        {
            await Task.Delay(80); Guard(); form.Refresh(); form.Update();
            using var bitmap = new Bitmap(form.Width, form.Height); using var g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(form.Location, Point.Empty, form.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private sealed class Recording
    {
        internal Rectangle Frame, Stack, Tab; internal bool TopConnected, BottomConnected;
        internal readonly Dictionary<RibbonMessage, Rectangle> Rows = [], Closes = [];
        internal readonly Dictionary<RibbonButtonItem, Rectangle> Actions = [];
        internal void Surface() { Frame = Stack = Rectangle.Empty; TopConnected = BottomConnected = false; Rows.Clear(); Closes.Clear(); Actions.Clear(); }
        internal void Close(Rectangle bounds) { foreach (var row in Rows) if (row.Value.Contains(bounds)) Closes[row.Key] = bounds; }
    }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawSurface(RibbonPaintContext c, bool popup) { if (!popup) r.Surface(); base.DrawSurface(c, popup); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tab = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawBody(RibbonPaintContext c) { r.Frame = c.Bounds; r.BottomConnected = c.IsBottomEdgeConnected; base.DrawBody(c); }
        public override void DrawMessageBar(RibbonPaintContext c) { r.Stack = c.Bounds; r.TopConnected = c.IsTopEdgeConnected; base.DrawMessageBar(c); }
        public override void DrawMessage(RibbonPaintContext c, RibbonMessage m, Rectangle title, Rectangle text, Rectangle icon, bool first) { r.Rows[m] = c.Bounds; base.DrawMessage(c, m, title, text, icon, first); }
        public override void DrawMessageAction(RibbonPaintContext c, RibbonButtonItem action) { r.Actions[action] = c.Bounds; base.DrawMessageAction(c, action); }
        public override void DrawMessageClose(RibbonPaintContext c, bool enabled) { r.Close(c.Bounds); base.DrawMessageClose(c, enabled); }
    }
    private sealed class Neutral(Recording r) : RibbonRenderer
    {
        public override void DrawSurface(RibbonPaintContext c, bool popup) { if (!popup) r.Surface(); base.DrawSurface(c, popup); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tab = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawMessageBar(RibbonPaintContext c) { r.Stack = c.Bounds; r.TopConnected = c.IsTopEdgeConnected; base.DrawMessageBar(c); }
        public override void DrawMessage(RibbonPaintContext c, RibbonMessage m, Rectangle title, Rectangle text, Rectangle icon, bool first) { r.Rows[m] = c.Bounds; base.DrawMessage(c, m, title, text, icon, first); }
        public override void DrawMessageAction(RibbonPaintContext c, RibbonButtonItem action) { r.Actions[action] = c.Bounds; base.DrawMessageAction(c, action); }
        public override void DrawMessageClose(RibbonPaintContext c, bool enabled) { r.Close(c.Bounds); base.DrawMessageClose(c, enabled); }
    }
}
