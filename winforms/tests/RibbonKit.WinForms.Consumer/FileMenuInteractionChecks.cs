using System.ComponentModel;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class FileMenuInteractionChecks
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
        using var form = new WF.Form { Text = "Independent File menu consumer", Width = 930, Height = 430, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer }; var home = new RibbonTab { Text = "Home", KeyTip = "H" }; var insert = new RibbonTab { Text = "Insert", KeyTip = "N" }; ribbon.Tabs.AddRange(home, insert);
        var menu = ribbon.FileMenu; menu.Visible = true;
        var open = new RibbonButtonItem { Text = "Open", KeyTip = "O", MenuDescription = "Open an existing document." };
        var recent = new RibbonDropDownItem { Text = "Recent", KeyTip = "R" }; var example = new RibbonButtonItem { Text = "Example document", KeyTip = "E" }; recent.Items.Add(example);
        var field = new RibbonTextBoxItem { Text = "Document name", Id = "file.name", KeyTip = "D", Value = "Untitled", InputWidth = 170 };
        bool cancel = true; int openings = 0, closes = 0, actions = 0, commits = 0, unavailable = 0;
        menu.DropDownOpening += (_, e) => { openings++; e.Cancel = cancel; if (!cancel && menu.Items.Count == 0) menu.Items.AddRange(open, new RibbonMenuSeparatorItem(), recent, field, new RibbonButtonItem { Text = "Unavailable", Enabled = false }); };
        menu.DropDownClosed += (_, _) => closes++; open.Click += (_, _) => actions++; example.Click += (_, _) => actions++; field.ValueCommitted += (_, _) => commits++;
        ribbon.QuickAccessItems.AddRange(new RibbonButtonItem { Text = "Save", KeyTip = "Q" }, new RibbonButtonItem { Text = "Undo" }, new RibbonButtonItem { Text = "Redo" });
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "File commands belong to this application. RibbonKit owns their header geometry, native menu stack, keyboard navigation and editor presentation. Backstage pages follow in a later slice." };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        using var binding = new WF.BindingSource(); var state = new State(); binding.DataSource = state;
        field.DataBindings.Add(new WF.Binding(nameof(field.Value), binding, nameof(state.Name), false, WF.DataSourceUpdateMode.OnPropertyChanged));
        EventHandler<FirstChanceExceptionEventArgs> exceptions = (_, e) => { if (e.Exception is COMException { HResult: unchecked((int)0x80040201) }) unavailable++; };
        AppDomain.CurrentDomain.FirstChanceException += exceptions;
        var log = new List<string>(); Exception? error = null; bool completed = false; var cursor = WF.Cursor.Position;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle);
                for (int i = 0; i < 20 && GetForegroundWindow() != form.Handle; i++) await Task.Delay(50);
                Guard(); ribbon.Refresh();
                Check(record.File.Left == record.Body.Left && record.File.Left > 0 && record.File.Right == record.QuickAccess.Left,
                    "File aligns with the inset body and QAT follows its measured right edge");
                await Click(ribbon, record.File);
                Check(!menu.IsDropDownOpen && openings == 1 && closes == 0, "The File opening callback can cancel without opening or closing a session"); cancel = false;
                await Click(ribbon, record.File); Check(menu.IsDropDownOpen && openings == 2 && ReferenceEquals(ribbon.SelectedTab, home), "Pointer opens lazily populated File commands without changing the tab");
                await Capture("file-command-menu"); var popup = Popup();
                Check(popup.TopLevelControl!.Left == ribbon.PointToScreen(record.File.Location).X,
                    "The File popup anchors at the same inset left edge as its button and the body");
                var editor = Tree(popup).OfType<WF.TextBox>().Single(c => c.Name == field.Id); nint handle = editor.Handle;
                await Click(editor, editor.ClientRectangle); await Key("^aReport{ENTER}"); Check(field.Value == "Report" && state.Name == "Report" && commits == 1 && menu.IsDropDownOpen, "The original native field commits once and shares its application binding");
                await Click(popup, record.Items[recent]); Check(recent.IsDropDownOpen && menu.IsDropDownOpen, "Recent opens as a nested File menu"); await Capture("file-nested-menu");
                await Key("{ESC}"); Check(!recent.IsDropDownOpen && menu.IsDropDownOpen, "Escape closes the nested level first");
                await Key("{ESC}"); Check(!menu.IsDropDownOpen && closes == 1, "Escape closes File and restores its opener");
                ribbon.Focus(); await Key("{F10}FO"); Check(actions == 1 && !menu.IsDropDownOpen, "Root File and command KeyTips execute once and dismiss the menu");
                await Click(ribbon, record.File); popup = Popup(); editor = Tree(popup).OfType<WF.TextBox>().Single(c => c.Name == field.Id);
                Check(editor.Handle == handle && editor.Text == "Report", "Reopening File retains the same native editor and value");
                menu.Enabled = false; Check(!menu.IsDropDownOpen, "Disabling File closes its open stack"); menu.Enabled = true;
                ribbon.IsMinimized = true; ribbon.Refresh(); await Click(ribbon, record.File); await Capture("minimized-file-menu");
                Check(menu.IsDropDownOpen && ribbon.IsMinimized && ReferenceEquals(ribbon.SelectedTab, home), "File opens while minimized without opening the tab body"); await Key("{ESC}");
                ribbon.IsMinimized = false; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; form.Width = 340; ribbon.Refresh();
                await Click(ribbon, record.File); await Capture("narrow-below-file-menu"); await Key("{ESC}");
                Check(record.File.Right <= record.Tabs[home].Left && record.QuickAccess.Left == 0 && record.QuickAccess.Width == ribbon.ClientSize.Width, "A narrow File header leaves tabs and the separate full-width QAT row reachable");
                menu.Visible = false; ribbon.Refresh(); Check(!menu.IsDropDownOpen && record.Tabs[home].Left < record.File.Right, "Hiding File releases its header slot");
                menu.Visible = true; ribbon.Refresh(); await Click(ribbon, record.File); await Click(Popup(), record.Items[recent]);
                await Click(Popup(), record.Items[example]); Check(actions == 2 && !menu.IsDropDownOpen && !recent.IsDropDownOpen, "A nested command dismisses the complete File stack once");
                await Click(ribbon, record.File); menu.Dispose(); Check(!menu.IsDropDownOpen && editor.IsDisposed, "Disposing the owned File model closes its stack and releases its native editor");
                Check(unavailable == 0, "File interactions raise zero unavailable-element exceptions");
                log.Add($"PASS: {(flat ? "Neutral" : "Office")} File menu at {ribbon.DeviceDpi} DPI"); completed = true;
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
    private static IEnumerable<WF.Control> Tree(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Tree(c)));
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected a library File popup.");
    private sealed class State : INotifyPropertyChanged
    {
        private string _name = "Untitled";
        public string Name { get => _name; set { if (_name == value) return; _name = value; PropertyChanged?.Invoke(this, new(nameof(Name))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Recording
    {
        internal Rectangle File, QuickAccess, Body; internal readonly Dictionary<RibbonItem, Rectangle> Items = []; internal readonly Dictionary<RibbonTab, Rectangle> Tabs = [];
    }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawBody(RibbonPaintContext c) { r.Body = c.Bounds; base.DrawBody(c); }
        public override void DrawFileButton(RibbonPaintContext c, RibbonDropDownItem menu) { r.File = c.Bounds; base.DrawFileButton(c, menu); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext c, RibbonQuickAccessPosition position) { r.QuickAccess = c.Bounds; base.DrawQuickAccessToolbar(c, position); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tabs[tab] = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Items[item] = c.Bounds; base.DrawItem(c, item, size, menu); }
    }
    private sealed class Flat(Recording r) : RibbonRenderer
    {
        public override void DrawBody(RibbonPaintContext c) { r.Body = c.Bounds; base.DrawBody(c); }
        public override void DrawFileButton(RibbonPaintContext c, RibbonDropDownItem menu) { r.File = c.Bounds; base.DrawFileButton(c, menu); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext c, RibbonQuickAccessPosition position) { r.QuickAccess = c.Bounds; base.DrawQuickAccessToolbar(c, position); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tabs[tab] = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Items[item] = c.Bounds; base.DrawItem(c, item, size, menu); }
    }
}
