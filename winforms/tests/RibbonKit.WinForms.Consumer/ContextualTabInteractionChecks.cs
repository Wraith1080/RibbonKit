using System.ComponentModel;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class ContextualTabInteractionChecks
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
        using var form = new WF.Form { Text = "Independent contextual-tab consumer", Width = 1000, Height = 420, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer }; var home = new RibbonTab { Text = "Home", KeyTip = "H" }; var insert = new RibbonTab { Text = "Insert", KeyTip = "N" };
        var context = new RibbonTab { Text = "Picture Format", KeyTip = "JP", IsContextual = true, ContextualColor = Color.FromArgb(196, 62, 150), Visible = false };
        var review = new RibbonTab { Text = "Review", KeyTip = "R" }; ribbon.Tabs.AddRange(home, insert, context, review, new RibbonTab { Text = "View" }, new RibbonTab { Text = "Options" });
        var group = new RibbonGroup { Text = "Picture", CanResize = false }; context.Groups.Add(group);
        var crop = new RibbonButtonItem { Text = "Crop", KeyTip = "C" }; var menu = new RibbonDropDownItem { Text = "Effects", KeyTip = "E" }; menu.Items.Add(new RibbonButtonItem { Text = "Shadow" }); group.Items.AddRange(crop, menu);
        var reference = new RibbonQuickAccessReference { Source = crop, Text = "Crop", KeyTip = "Q" }; ribbon.QuickAccessItems.Add(reference);
        using var binding = new WF.BindingSource(); var state = new State(); binding.DataSource = state;
        context.DataBindings.Add(new WF.Binding(nameof(context.Visible), binding, nameof(state.Picture), false, WF.DataSourceUpdateMode.OnPropertyChanged));
        var check = new WF.CheckBox { Text = "Picture selected", AutoSize = true };
        check.DataBindings.Add(new WF.Binding(nameof(check.Checked), binding, nameof(state.Picture), false, WF.DataSourceUpdateMode.OnPropertyChanged));
        var host = new WF.FlowLayoutPanel { Dock = WF.DockStyle.Top, Height = 45 }; host.Controls.Add(check);
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "The application owns picture selection. RibbonKit owns contextual header colors, tab navigation, selection fallback and native popup lifetime." };
        var content = new WF.Panel { Dock = WF.DockStyle.Fill }; content.Controls.Add(document); content.Controls.Add(host); form.Controls.Add(content); form.Controls.Add(ribbon);
        int crops = 0, unavailable = 0; crop.Click += (_, _) => crops++;
        EventHandler<FirstChanceExceptionEventArgs> exceptions = (_, e) => { if (e.Exception is COMException { HResult: unchecked((int)0x80040201) }) unavailable++; };
        AppDomain.CurrentDomain.FirstChanceException += exceptions;
        var log = new List<string>(); Exception? error = null; bool completed = false; var cursor = WF.Cursor.Position;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle);
                for (int i = 0; i < 20 && GetForegroundWindow() != form.Handle; i++) await Task.Delay(50);
                Guard(); ribbon.Refresh(); await Click(ribbon, record.Entries[reference]); Check(crops == 0 && !context.Visible, "Hidden contextual sources remain unavailable in QAT");
                await Click(check, check.ClientRectangle); Check(context.Visible && ReferenceEquals(ribbon.SelectedTab, home), "The host binding reveals contextual tools without selecting them");
                await Capture("unselected-context"); await Click(ribbon, record.Tabs[context]);
                Check(ReferenceEquals(ribbon.SelectedTab, context), "Pointer selects the contextual tab"); await Capture("selected-context");
                await Click(ribbon, record.Items[menu]); Check(menu.IsDropDownOpen, "Contextual commands open their normal source menu");
                context.ContextualColor = Color.Teal; Check(menu.IsDropDownOpen, "Changing only contextual tint retains the command popup"); await Key("{ESC}");
                await Click(check, check.ClientRectangle); Check(!context.Visible && ReferenceEquals(ribbon.SelectedTab, review), "Hiding selected contextual tools returns to the next visible tab");
                await Click(check, check.ClientRectangle); ribbon.Focus(); await Key("{F10}JPC");
                Check(ReferenceEquals(ribbon.SelectedTab, context) && crops == 1, "Multi-character contextual KeyTips enter the original command level");
                await Click(ribbon, record.Tabs[home]); await Click(ribbon, record.Entries[reference]); Check(crops == 2 && ReferenceEquals(ribbon.SelectedTab, home), "An inactive visible context remains usable through QAT");
                ribbon.IsMinimized = true; ribbon.Refresh(); await Click(ribbon, record.Tabs[context]); Check(ribbon.IsMinimized, "A contextual tab opens its ordinary minimized flyout"); await Capture("minimized-context");
                state.Picture = false; Check(!context.Visible && ReferenceEquals(ribbon.SelectedTab, review) && WF.Control.FromHandle(GetForegroundWindow()) is not WF.ToolStripDropDown, "Context loss closes the minimized flyout and restores selection");
                ribbon.IsMinimized = false; state.Picture = true; context.ContextualColor = Color.Empty;
                ribbon.Theme = ribbon.Theme with { Accent = Color.DarkGreen }; ribbon.SelectedTab = context; ribbon.Refresh(); await Capture("theme-accent-fallback");
                form.Width = 310; ribbon.Refresh(); ribbon.Focus(); await Key("{F10}JP{ESC}{ESC}"); await Capture("narrow-selected-context");
                Check(ReferenceEquals(ribbon.SelectedTab, context) && record.Tabs[context].IntersectsWith(ribbon.ClientRectangle), "Keyboard selection keeps a contextual tab reachable in a narrow strip");
                context.Enabled = false; Check(ReferenceEquals(ribbon.SelectedTab, review), "Disabling a selected contextual tab uses existing fallback"); context.Enabled = true;
                ribbon.SelectedTab = context; ribbon.Tabs.Remove(context); Check(ReferenceEquals(ribbon.SelectedTab, review) && context.Visible, "Removing the selected contextual tab preserves its authored state and selects a survivor");
                ribbon.Tabs.Insert(2, context); ribbon.SelectedTab = context; context.Dispose(); Check(ReferenceEquals(ribbon.SelectedTab, review), "Contextual disposal releases its commands and selects a survivor");
                Check(unavailable == 0, "Contextual-tab interactions raise zero unavailable-element exceptions");
                log.Add($"PASS: {(flat ? "Neutral" : "Office")} contextual tabs at {ribbon.DeviceDpi} DPI"); completed = true;
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
    private sealed class State : INotifyPropertyChanged
    {
        private bool _picture;
        public bool Picture { get => _picture; set { if (_picture == value) return; _picture = value; PropertyChanged?.Invoke(this, new(nameof(Picture))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Recording
    {
        internal readonly Dictionary<RibbonTab, Rectangle> Tabs = []; internal readonly Dictionary<RibbonItem, Rectangle> Items = []; internal readonly Dictionary<RibbonElement, Rectangle> Entries = [];
    }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tabs[tab] = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Entries[entry] = c.Bounds; base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Items[item] = c.Bounds; base.DrawItem(c, item, size, menu); }
    }
    private sealed class Flat(Recording r) : RibbonRenderer
    {
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tabs[tab] = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Entries[entry] = c.Bounds; base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Items[item] = c.Bounds; base.DrawItem(c, item, size, menu); }
    }
}
