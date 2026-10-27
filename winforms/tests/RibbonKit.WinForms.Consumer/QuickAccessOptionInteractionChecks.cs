using System.ComponentModel;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public-only fixture: the native runner and the independent consumer compile this source.
internal static class QuickAccessOptionInteractionChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);

    internal static int Run(string directory, bool flat = false)
    {
        directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        var record = new Recording(); using RibbonRenderer renderer = flat ? new Flat(record) : new Office(record);
        using var form = new WF.Form { Text = "Independent QAT option consumer", Width = 850, Height = 390, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer }; var home = new RibbonTab { Text = "Home", KeyTip = "H" }; var options = new RibbonTab { Text = "Options", KeyTip = "O" };
        var group = new RibbonGroup { Text = "Document view" }; options.Groups.Add(group); ribbon.Tabs.AddRange(home, options);
        var grid = new RibbonCheckBoxItem { Text = "Gridlines", ThreeState = true, Icon = new OptionIcon(0), MenuDescription = "Share the original three-state document setting." };
        var print = new RibbonRadioButtonItem { Text = "Print layout", Checked = true, Icon = new OptionIcon(1) };
        var web = new RibbonRadioButtonItem { Text = "Web layout", Icon = new OptionIcon(2) }; var draft = new RibbonRadioButtonItem { Text = "Draft" };
        group.Items.AddRange(grid, print, web, draft);
        var gridRef = new RibbonQuickAccessReference { Source = grid, KeyTip = "G" }; var printRef = new RibbonQuickAccessReference { Source = print, KeyTip = "P" };
        var webRef = new RibbonQuickAccessReference { Source = web, KeyTip = "W" }; var duplicate = new RibbonQuickAccessReference { Source = grid, Text = "Gridlines again", KeyTip = "A" };
        var draftRef = new RibbonQuickAccessReference { Source = draft, KeyTip = "D" }; var copy = new RibbonButtonItem { Text = "Copy" };
        ribbon.QuickAccessItems.AddRange(gridRef, printRef, webRef, duplicate, draftRef, copy);
        var document = new WF.TextBox { Multiline = true, Dock = WF.DockStyle.Fill, Text = "Checkboxes and radio options use icon toggle buttons in QAT. Mixed state has a small bar beneath the action icon. The source retains its binding, radio group and native option presentation." };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        using var binding = new WF.BindingSource(); var state = new State(); binding.DataSource = state;
        grid.DataBindings.Add(new WF.Binding(nameof(grid.CheckState), binding, nameof(state.Grid), false, WF.DataSourceUpdateMode.OnPropertyChanged));
        int clicks = 0, radioClicks = 0, copies = 0, unavailable = 0; grid.Click += (_, _) => clicks++; web.Click += (_, _) => radioClicks++; copy.Click += (_, _) => copies++;
        EventHandler<FirstChanceExceptionEventArgs> exceptions = (_, e) => { if (e.Exception is COMException { HResult: unchecked((int)0x80040201) }) unavailable++; };
        AppDomain.CurrentDomain.FirstChanceException += exceptions;
        var log = new List<string>(); Exception? error = null; bool completed = false; var cursor = WF.Cursor.Position;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle);
                for (int i = 0; i < 20 && GetForegroundWindow() != form.Handle; i++) await Task.Delay(50);
                Guard(); ribbon.Refresh(); await Capture("strip-mixed-icons");
                await Click(ribbon, record.Entries[gridRef]); Check(grid.CheckState == WF.CheckState.Unchecked && state.Grid == WF.CheckState.Unchecked && clicks == 1, "The inactive source follows its three-state cycle and binding once");
                await Click(ribbon, record.Entries[duplicate]); Check(grid.CheckState == WF.CheckState.Checked && clicks == 2, "A second presentation shares the original checkbox state");
                state.Grid = WF.CheckState.Indeterminate; Check(grid.CheckState == WF.CheckState.Indeterminate && clicks == 2, "Programmatic binding updates do not synthesize clicks");
                await Click(ribbon, record.Entries[webRef]); await Click(ribbon, record.Entries[webRef]);
                Check(web.Checked && !print.Checked && radioClicks == 2 && ReferenceEquals(web.Parent, group) && ReferenceEquals(ribbon.SelectedTab, home), "A radio retains its original group and cannot toggle itself off");
                ribbon.Focus(); await Key("{F10}G"); Check(grid.CheckState == WF.CheckState.Unchecked && clicks == 3, "KeyTips activate the same checkbox source once");
                ribbon.SelectedTab = options; ribbon.Refresh(); await Click(ribbon, record.Items[grid]);
                Check(grid.CheckState == WF.CheckState.Checked && state.Grid == WF.CheckState.Checked && clicks == 4, "The original body checkbox updates its QAT presentations");
                await Capture("body-and-qat"); ribbon.SelectedTab = home; ribbon.QuickAccessMaxWidth = 28; ribbon.Refresh();
                await Click(ribbon, record.Overflow); var popup = Popup(); await Click(popup, record.MenuEntries[gridRef]);
                Check(grid.CheckState == WF.CheckState.Indeterminate && clicks == 5 && popup.Visible, "Checkbox activation retains overflow and displays mixed state"); await Capture("overflow-mixed-icons");
                await Key(" "); Check(grid.CheckState == WF.CheckState.Unchecked && clicks == 6 && popup.Visible, "Space repeats the focused overflow option");
                await Click(popup, record.MenuEntries[webRef]); await Key("{ENTER}");
                Check(web.Checked && !print.Checked && radioClicks == 4 && popup.Visible, "Pointer and Enter retain the selected radio and overflow");
                gridRef.Enabled = false; await Click(popup, record.MenuEntries[gridRef]); Check(clicks == 6 && grid.Enabled, "Disabling a reference leaves its source enabled"); gridRef.Enabled = true;
                grid.Enabled = false; await Click(popup, record.MenuEntries[duplicate]); Check(clicks == 6, "Source availability applies to every presentation"); grid.Enabled = true;
                await Click(popup, record.MenuEntries[copy]); Check(copies == 1 && !popup.Visible, "An ordinary command still dismisses overflow");
                ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; ribbon.IsMinimized = true; ribbon.Refresh();
                await Click(ribbon, record.Entries[printRef]); Check(print.Checked && !web.Checked && ribbon.IsMinimized, "BelowRibbon option buttons work while minimized"); await Capture("minimized-below-icons");
                group.Items.Remove(grid); await Click(ribbon, record.Entries[gridRef]); Check(clicks == 6 && !grid.IsDisposed, "A detached source is unavailable and remains borrowed");
                group.Items.Add(grid); state.Grid = WF.CheckState.Checked; await Click(ribbon, record.Entries[duplicate]);
                Check(grid.CheckState == WF.CheckState.Indeterminate && clicks == 7, "Reinsertion restores the shared source and binding");
                grid.Dispose(); await Click(ribbon, record.Entries[gridRef]); Check(clicks == 7, "A disposed source leaves its reference safely unavailable");
                Check(unavailable == 0, "Option QAT interactions raise zero unavailable-element exceptions");
                log.Add($"PASS: {(flat ? "Neutral" : "Office")} QAT options at {ribbon.DeviceDpi} DPI"); completed = true;
            }
            catch (Exception e) { error = e; log.Add("FAIL: " + e); try { await Capture("failure"); } catch { } }
            finally { WF.Cursor.Position = cursor; form.Close(); }
        };
        try { WF.Application.Run(form); } finally { AppDomain.CurrentDomain.FirstChanceException -= exceptions; }
        File.WriteAllLines(Path.Combine(directory, "result.log"), log);
        if (error is not null) Console.Error.WriteLine(error); else Console.WriteLine(string.Join(Environment.NewLine, log));
        return completed && error is null ? 0 : 1;

        void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        void Guard() { if (GetForegroundWindow() != form.Handle && WF.Control.FromHandle(GetForegroundWindow()) is not WF.ToolStripDropDown) throw new InvalidOperationException("Validation Form lost foreground focus."); }
        async Task Key(string keys) { Guard(); WF.SendKeys.SendWait(keys); await Task.Delay(100); }
        async Task Click(WF.Control surface, Rectangle bounds)
        {
            Guard(); var visible = Rectangle.Intersect(surface.ClientRectangle, bounds); Check(!visible.IsEmpty, "Pointer target is visible");
            WF.Cursor.Position = surface.PointToScreen(new(visible.Left + visible.Width / 2, visible.Top + visible.Height / 2)); await Task.Delay(40); Guard();
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(100);
        }
        async Task Capture(string name)
        {
            await Task.Delay(60); Guard(); var bounds = form.Bounds;
            if (WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup) bounds = Rectangle.Union(bounds, popup.Bounds);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected a library QAT overflow.");
    private sealed class State : INotifyPropertyChanged
    {
        private WF.CheckState _grid = WF.CheckState.Indeterminate;
        public WF.CheckState Grid { get => _grid; set { if (_grid == value) return; _grid = value; PropertyChanged?.Invoke(this, new(nameof(Grid))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class OptionIcon(int kind) : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        {
            using var pen = new Pen(context.Enabled ? Color.FromArgb(43, 87, 154) : SystemColors.GrayText, Math.Max(1, bounds.Width / 14f));
            var r = Rectangle.Inflate(bounds, -2, -2); graphics.DrawRectangle(pen, r);
            if (kind == 0)
            {
                graphics.DrawLine(pen, r.Left + r.Width / 2, r.Top, r.Left + r.Width / 2, r.Bottom);
                graphics.DrawLine(pen, r.Left, r.Top + r.Height / 2, r.Right, r.Top + r.Height / 2);
            }
            else for (int i = 1; i <= 3; i++) graphics.DrawLine(pen, r.Left + 2, r.Top + r.Height * i / 4, r.Right - (kind == 2 && i == 2 ? r.Width / 3 : 2), r.Top + r.Height * i / 4);
        }
    }
    private sealed class Recording
    {
        internal readonly Dictionary<RibbonElement, Rectangle> Entries = [], MenuEntries = []; internal readonly Dictionary<RibbonItem, Rectangle> Items = []; internal Rectangle Overflow;
        internal void Entry(RibbonPaintContext c, RibbonElement entry, bool menu) => (menu ? MenuEntries : Entries)[entry] = c.Bounds;
    }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Entry(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Items[item] = c.Bounds; base.DrawItem(c, item, size, menu); }
    }
    private sealed class Flat(Recording r) : RibbonRenderer
    {
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Entry(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Items[item] = c.Bounds; base.DrawItem(c, item, size, menu); }
    }
}
