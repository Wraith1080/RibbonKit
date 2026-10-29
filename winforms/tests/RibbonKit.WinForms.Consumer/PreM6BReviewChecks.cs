using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public runtime APIs and native pointer input; no Showcase presentation helpers.
internal static class PreM6BReviewChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private delegate bool Visitor(nint window, nint state);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, Visitor visitor, nint state);
    internal static int Run(string directory, bool neutral, bool popupOnly = false, bool optionsOnly = false)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles(); WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); List<string> log = []; Exception? error = null; var cursor = WF.Cursor.Position;
        var record = new Recording(); using RibbonRenderer renderer = neutral ? new Neutral(record) : new Office(record);
        using var form = new WF.Form { Text = "Independent pre-M6B review", Size = new(1240, 780), StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer }; var home = new RibbonTab { Text = "Home" }; var commands = new RibbonGroup { Text = "Clipboard" };
        var copy = new RibbonButtonItem { Text = "Copy", Icon = new Icon(), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; commands.Items.Add(copy);
        commands.Items.Add(new RibbonComboBoxItem { Text = "Font", IsEditable = true, Value = "Retained", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }); home.Groups.Add(commands); ribbon.Tabs.Add(home);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = copy }); ribbon.CommandCatalog.CaptureDefaults();
        var document = new WF.RichTextBox { Dock = WF.DockStyle.Fill, Text = "Application-owned retained draft" }; form.Controls.Add(document); form.Controls.Add(ribbon);
        ribbon.MessageBar.Items.AddRange(new RibbonMessage { Title = "NOTICE 1", Text = "Joined message review" }, new RibbonMessage { Title = "NOTICE 2", Text = "Independent second message" });
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(300);
                foreach (var density in Enum.GetValues<RibbonDensity>())
                {
                    ribbon.Density = density;
                    if (!optionsOnly)
                    foreach (var layoutMode in popupOnly ? Enum.GetValues<RibbonLayoutMode>() : new[] { RibbonLayoutMode.Classic })
                    {
                    foreach (var position in Enum.GetValues<RibbonQuickAccessPosition>())
                    foreach (bool minimized in new[] { false, true })
                    {
                        if (popupOnly && !minimized) continue;
                        ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.LayoutMode = layoutMode; ribbon.QuickAccessPosition = position; ribbon.IsMinimized = minimized; ribbon.Refresh();
                        string prefix = $"{density}-{layoutMode}-{position}-{minimized}"; await Task.Delay(40);
                        Check(record.QatItem.Width == record.QatItem.Height, prefix + ": ordinary QAT target is square");
                        bool connected = !neutral && (!minimized || position == RibbonQuickAccessPosition.BelowRibbon);
                        Check(record.Connected == connected, prefix + ": message top connection follows the preceding frame"); Capture(prefix + "-messages");
                        if (!minimized) continue;
                        ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; await Click(ribbon.PointToScreen(Center(record.Tab))); await Finish();
                        var popup = Popups().Single(); var expected = ribbon.PointToScreen(new(S(ribbon.Theme.BodyMargin.Left), S((density == RibbonDensity.Touch ? 48 : 40) + ribbon.Theme.BodyMargin.Top)));
                        Check(popup.Left == expected.X && popup.Top == expected.Y && popup.Width == ribbon.Width - S(ribbon.Theme.BodyMargin.Horizontal), "Temporary ribbon keeps side margins and anchors directly after the tabs");
                        var panelScreen = ribbon.RectangleToScreen(record.Messages); Check(popup.Bounds.IntersectsWith(panelScreen), "Temporary ribbon overlays message geometry");
                        Capture(prefix + "-overlay"); nint documentHandle = document.Handle;
                        await Click(document.PointToScreen(new(100, Math.Max(20, document.Height - 45))));
                        Check(Popups().Count == 0 && ribbon.IsAnimating && document.Focused, "Outside click releases the popup, focuses the document and starts dismissal motion");
                        Capture(prefix + "-dismissing"); await Task.Delay(45); Capture(prefix + "-dismissing-middle"); await Finish(); Capture(prefix + "-dismissed"); Check(ribbon.IsMinimized && document.Handle == documentHandle && document.Text.Contains("retained draft"), "Dismissal preserves minimized state and application data/handle");
                    }
                    }
                    if (popupOnly) continue;
                    ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsMinimized = false;
                    using var options = new RibbonQuickAccessCustomizationForm(ribbon, "consumer.pre-m6b"); options.Show(form); SetForegroundWindow(options.Handle); await Task.Delay(80);
                    (int Left, int LeftWidth, int Right, int RightWidth)? previous = null;
                    Rectangle[]? previousActions = null;
                    foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
                    {
                        ribbon.LayoutMode = mode;
                        foreach (bool qat in new[] { true, false })
                        {
                            options.SelectedPage = options.Pages.Single(p => p.Text == (qat ? "Quick Access Toolbar" : "Customize Ribbon")); await Task.Delay(35);
                            WF.Control editor = qat ? options.Editor : mode == RibbonLayoutMode.Classic ? options.RibbonEditor : options.SimplifiedEditor;
                            var searches = Descendants(editor).OfType<WF.TextBox>().OrderBy(c => c.PointToScreen(Point.Empty).X).ToArray();
                            Check(searches.Length == 2, "Customization page retains two native searches");
                            var left = searches[0].Parent!; var right = searches[1].Parent!;
                            var columns = (left.PointToScreen(Point.Empty).X, left.Width, right.PointToScreen(Point.Empty).X, right.Width);
                            if (previous is { } expectedColumns) Check(columns == expectedColumns, "QAT, Classic and Simplified pages preserve identical search/list column widths and positions"); previous = columns;
                            foreach (var list in Descendants(editor).Where(c => c is WF.ListBox or WF.TreeView))
                            { var frame = list.Parent!; Check(frame.Width == left.Width || frame.Width == right.Width, "Native list/tree frame matches its search width"); }
                            string[] names = qat ? ["AddCommand", "RemoveCommand", "MoveCommandUp", "MoveCommandDown"]
                                : mode == RibbonLayoutMode.Classic ? ["AddRibbonCommand", "RemoveRibbonItem", "MoveRibbonItemUp", "MoveRibbonItemDown"]
                                : ["SimplifiedAdd", "SimplifiedRemove", "SimplifiedMoveUp", "SimplifiedMoveDown"];
                            var actions = names.Select(name => Descendants(editor).OfType<WF.Button>().Single(b => b.Name == name))
                                .Select(b => new Rectangle(options.PointToClient(b.PointToScreen(Point.Empty)), b.Size)).ToArray();
                            Check(actions[0].Top == actions[2].Top && actions[1].Top == actions[3].Top, "Add/Up and Remove/Down share vertical positions");
                            if (previousActions is not null) Check(actions.SequenceEqual(previousActions), "All four action buttons keep identical bounds across QAT, Classic and Simplified pages");
                            previousActions = actions;
                            Capture(density + "-" + mode + (qat ? "-qat" : "-ribbon"), options);
                        }
                    }
                    options.Close(); SetForegroundWindow(form.Handle); await Task.Delay(60);
                }
                log.Add($"PASS: {(neutral ? "neutral" : "Office2024")} pre-M6B review at actual {ribbon.DeviceDpi} DPI");
            }
            catch (Exception e) { error = e; log.Add(e.ToString()); }
            finally { WF.Cursor.Position = cursor; File.WriteAllLines(Path.Combine(directory, "result.log"), log); form.Close(); }
        };
        WF.Application.Run(form); foreach (string line in log) Console.WriteLine(line); return error is null ? 0 : 1;
        int S(int value) => (int)Math.Round(value * ribbon.DeviceDpi / 96d);
        void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException(text); log.Add("PASS: " + text); }
        void Capture(string name, WF.Form? target = null)
        {
            target ??= form; DwmFlush(); using var image = new Bitmap(target.Width, target.Height); using var g = Graphics.FromImage(image); g.CopyFromScreen(target.Location, Point.Empty, target.Size); image.Save(Path.Combine(directory, name + ".png"));
        }
        async Task Click(Point point) { WF.Cursor.Position = point; await Task.Delay(20); mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(20); }
        async Task Finish() { var watch = System.Diagnostics.Stopwatch.StartNew(); while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1500) await Task.Delay(10); Check(!ribbon.IsAnimating, "Motion completes autonomously"); }
    }
    private static Point Center(Rectangle r) => new(r.Left + r.Width / 2, r.Top + r.Height / 2);
    private static List<WF.ToolStripDropDown> Popups() { List<WF.ToolStripDropDown> windows = []; EnumThreadWindows(GetCurrentThreadId(), (h, _) => { if (WF.Control.FromHandle(h) is WF.ToolStripDropDown { Visible: true } popup) windows.Add(popup); return true; }, 0); return windows; }
    private static IEnumerable<WF.Control> Descendants(WF.Control c) => c.Controls.Cast<WF.Control>().SelectMany(child => new[] { child }.Concat(Descendants(child)));
    private sealed class Icon : IRibbonIcon { public void Draw(Graphics g, Rectangle b, RibbonIconContext c) { using var pen = new Pen(c.Foreground, 2); g.DrawRectangle(pen, Rectangle.Inflate(b, -3, -3)); } }
    private sealed class Recording { internal Rectangle Tab, Messages, QatItem; internal bool Connected; }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tab = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawMessageBar(RibbonPaintContext c) { r.Messages = c.Bounds; r.Connected = c.IsTopEdgeConnected; base.DrawMessageBar(c); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement item, bool menu) { if (!menu) r.QatItem = c.Bounds; base.DrawQuickAccessItem(c, item, menu); }
    }
    private sealed class Neutral(Recording r) : RibbonRenderer
    {
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tab = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawMessageBar(RibbonPaintContext c) { r.Messages = c.Bounds; r.Connected = c.IsTopEdgeConnected; base.DrawMessageBar(c); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement item, bool menu) { if (!menu) r.QatItem = c.Bounds; base.DrawQuickAccessItem(c, item, menu); }
    }
}
