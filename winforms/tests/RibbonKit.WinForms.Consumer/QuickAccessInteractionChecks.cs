using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// The same source is compiled by the native runner and copied to a fresh
// package-only consumer. Geometry comes exclusively from public renderer hooks.
internal static class QuickAccessInteractionChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetFocus();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);

    internal static int Run(string directory, bool flat = false)
    {
        directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        var log = new List<string>(); var record = new Recording();
        using RibbonRenderer renderer = flat ? new FlatRecorder(record) : new OfficeRecorder(record);
        using var form = new WF.Form { Text = "Independent Quick Access consumer", Width = 1000, Height = 420, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer };
        var home = new RibbonTab { Text = "Home", KeyTip = "H" }; var insert = new RibbonTab { Text = "Insert", KeyTip = "N" };
        var group = new RibbonGroup { Text = "Commands" }; home.Groups.Add(group);
        var copy = new RibbonButtonItem { Text = "Copy", Icon = new TestIcon(), ScreenTipDescription = "A shared source command." };
        var bold = new RibbonToggleItem { Text = "Bold", Icon = new TestIcon(), MenuDescription = "Shares checked state with its ribbon command." };
        group.Items.AddRange(copy, bold); ribbon.Tabs.AddRange(home, insert);
        var copyRef = new RibbonQuickAccessReference { Source = copy }; var boldRef = new RibbonQuickAccessReference { Source = bold };
        var duplicate = new RibbonQuickAccessReference { Source = bold, Text = "Strong emphasis" };
        var save = new RibbonButtonItem { Text = "Save", Icon = new TestIcon() };
        ribbon.QuickAccessItems.AddRange(copyRef, boldRef, duplicate, save);
        int copies = 0, toggles = 0, saves = 0, lastCalls = 0; copy.Click += (_, _) => copies++; bold.Click += (_, _) => toggles++; save.Click += (_, _) => saves++;
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Public package consumer. The library owns all toolbar geometry, source references and overflow." };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        using var bindingSource = new WF.BindingSource(); var state = new State(); bindingSource.DataSource = state;
        bold.DataBindings.Add(new WF.Binding(nameof(bold.Checked), bindingSource, nameof(state.Bold), false, WF.DataSourceUpdateMode.OnPropertyChanged));
        Exception? failure = null; bool completed = false; var cursor = WF.Cursor.Position;
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(200); Guard(); ribbon.Refresh();
                int expandedHeight = ribbon.Height; await Capture("tab-row");
                document.Focus(); nint documentFocus = GetFocus();
                var tipSlot = record.Entries[copyRef]; WF.Cursor.Position = ribbon.PointToScreen(new(tipSlot.Left + tipSlot.Width / 2, tipSlot.Top + tipSlot.Height / 2));
                await Task.Delay(850); Guard();
                Check(record.Tips.Contains("Copy") && GetFocus() == documentFocus, "A source ScreenTip appears at the reference without taking document focus");
                await Capture("reference-screentip");
                await Click(ribbon, record.Entries[copyRef]); Check(copies == 1, "Pointer reference activation executes its source once");
                ribbon.SelectedTab = insert; ribbon.Refresh(); await Click(ribbon, record.Entries[boldRef]);
                Check(bold.Checked && state.Bold && toggles == 1 && ReferenceEquals(ribbon.SelectedTab, insert), "An inactive source toggles once through its existing binding without selecting its tab");
                await Click(ribbon, record.Entries[duplicate]); Check(!bold.Checked && !state.Bold && toggles == 2, "Two independent presentations share one toggle state");
                copy.Text = "Copy selection"; copy.Icon = new TestIcon(); ribbon.Refresh();
                Check(record.Captions[copyRef] == "Copy selection", "Source label and borrowed icon changes reach the reference");
                copy.Enabled = false; ribbon.Refresh(); await Click(ribbon, record.Entries[copyRef]); Check(copies == 1, "Disabled sources reject pointer activation");
                copy.Enabled = true; group.Items.Remove(copy); ribbon.Refresh(); await Click(ribbon, record.Entries[copyRef]);
                Check(copies == 1 && ReferenceEquals(ribbon.QuickAccessItems[0], copyRef), "Removing a source retains its disabled declaration and order");
                group.Items.Add(copy); ribbon.Refresh(); await Click(ribbon, record.Entries[copyRef]); Check(copies == 2, "Reinserting the same source restores its existing reference");
                ribbon.Focus(); await Key("{F10}1"); Check(copies == 3, "A numeric toolbar KeyTip executes once");
                ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; ribbon.Refresh();
                Check(ribbon.Height > expandedHeight && record.Strip.Width == ribbon.ClientSize.Width && record.Strip.Top == expandedHeight, "BelowRibbon adds a full-width row downward");
                await Capture("below-ribbon"); await Click(ribbon, record.Entries[save]); Check(saves == 1, "A toolbar-only command uses the normal activation path");
                ribbon.QuickAccessVisible = false; Check(ribbon.Height == expandedHeight && ribbon.QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon && ribbon.QuickAccessItems.Count == 4, "Hiding retains placement and authored declarations");
                ribbon.QuickAccessVisible = true; ribbon.IsMinimized = true; ribbon.Refresh(); await Capture("minimized-below");
                await Click(ribbon, record.Entries[save]); Check(saves == 2 && ribbon.IsMinimized, "BelowRibbon commands remain available while minimized");
                ribbon.IsMinimized = false; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
                for (int i = 0; i < 70; i++)
                {
                    var item = new RibbonButtonItem { Text = "Toolbar command " + i, Icon = new TestIcon() };
                    if (i == 69) item.Click += (_, _) => lastCalls++;
                    ribbon.QuickAccessItems.Add(item);
                }
                ribbon.QuickAccessMaxWidth = 112; ribbon.Refresh(); await Click(ribbon, record.Overflow); var popup = Popup();
                Check(popup is WF.ScrollableControl scrolling && !scrolling.AutoScroll && !scrolling.VerticalScroll.Visible && !scrolling.HorizontalScroll.Visible,
                    "Long QAT overflow has no native scrollbars");
                Check(!record.Up.IsEmpty && !record.Down.IsEmpty && !record.UpEnabled && record.DownEnabled, "Separate scroller bands disable the top endpoint");
                await Click(popup, record.Down); Check(record.UpEnabled && record.DownEnabled && lastCalls == 0, "A scroller click moves rows without invoking a command or closing overflow");
                await Click(popup, record.Up); Check(!record.UpEnabled && record.DownEnabled, "The up scroller returns to the top endpoint");
                WF.Cursor.Position = popup.PointToScreen(new(record.Down.Left + record.Down.Width / 2, record.Down.Top + record.Down.Height / 2));
                await WaitForScrollRepeat();
                Check(record.UpEnabled && !record.MenuEntries.ContainsKey(duplicate), "Hover repeat advances past the first command row");
                Check(record.DownHot && !record.DownPressed, "An enabled scroll band paints hover feedback");
                await Capture("overflow-scroller-hover");
                WF.Cursor.Position = popup.PointToScreen(new(popup.Width / 2, popup.Height / 2)); await Task.Delay(70);
                var afterHover = record.MenuEntries.Keys.First(); await Task.Delay(200); popup.Refresh();
                Check(ReferenceEquals(afterHover, record.MenuEntries.Keys.First()), "Leaving a scroll band stops hover repeat");
                await Key("{HOME}");
                WF.Cursor.Position = popup.PointToScreen(new(record.Down.Left + record.Down.Width / 2, record.Down.Top + record.Down.Height / 2));
                mouse_event(2, 0, 0, 0, 0);
                try
                {
                    await WaitForScrollRepeat();
                    Check(record.DownPressed, "A held scroll band paints pressed feedback while repeating");
                    await Capture("overflow-scroller-pressed");
                }
                finally { mouse_event(4, 0, 0, 0, 0); }
                WF.Cursor.Position = popup.PointToScreen(new(popup.Width / 2, popup.Height / 2)); await Task.Delay(100); Guard(); popup.Refresh();
                Check(record.UpEnabled && !record.MenuEntries.ContainsKey(duplicate) && lastCalls == 0, "Holding a scroller repeats without command activation");
                async Task WaitForScrollRepeat()
                {
                    for (int i = 0; i < 20; i++)
                    {
                        await Task.Delay(75); Guard(); popup.Refresh();
                        if (record.UpEnabled && !record.MenuEntries.ContainsKey(duplicate)) return;
                    }
                }
                await Key("{HOME}"); mouse_event(0x800, 0, 0, unchecked((uint)-120), 0); await Task.Delay(100); Guard(); popup.Refresh();
                Check(record.UpEnabled && lastCalls == 0, "The mouse wheel scrolls the same overflow viewport");
                await Key("{HOME}");
                int beforeOverflowToggles = toggles;
                await Click(popup, record.MenuEntries[duplicate]);
                Check(toggles == beforeOverflowToggles + 1 && bold.Checked && state.Bold && !Closed(popup), "A pointer toggle updates its source and binding while keeping overflow open");
                await Click(popup, record.MenuEntries[duplicate]);
                Check(toggles == beforeOverflowToggles + 2 && !bold.Checked && !state.Bold && !Closed(popup), "A second pointer toggle uses the same overflow surface");
                await Capture("overflow"); await Key("{END}");
                Check(record.UpEnabled && !record.DownEnabled, "End reveals the final command and disables the lower scroller");
                await Capture("overflow-last"); await Key("{ENTER}");
                Check(lastCalls == 1 && Closed(popup), "Overflow scrolls to its last command and Enter executes once");
                ribbon.Focus(); await Key("{F10}0"); popup = Popup(); await Capture("overflow-keytips"); await Key("3");
                Check(toggles == beforeOverflowToggles + 3 && bold.Checked && state.Bold && !Closed(popup), "An overflow toggle KeyTip retains the popup and shared source state");
                await Key(" "); Check(toggles == beforeOverflowToggles + 4 && !bold.Checked && !state.Bold && !Closed(popup), "Space toggles the focused entry without dismissing overflow");
                await Key("{ENTER}"); Check(toggles == beforeOverflowToggles + 5 && bold.Checked && state.Bold && !Closed(popup), "Enter toggles the same entry without dismissing overflow");
                await Key("{ESC}"); Check(Closed(popup), "Escape still dismisses overflow after repeated toggles");
                ribbon.Focus(); await Key("{F10}0"); popup = Popup(); await Key("Q65");
                Check(lastCalls == 2 && Closed(popup), "The final command has an unambiguous multi-character KeyTip in a large overflow");
                ribbon.Refresh(); await Click(ribbon, record.Overflow); popup = Popup(); await Key("{ESC}");
                Check(Closed(popup) && ribbon.Focused, "Escape closes overflow and returns keyboard focus to its opener");
                await Key("{ENTER}"); popup = Popup(); Check(!Closed(popup), "The restored overflow opener remains keyboard actionable");
                ribbon.QuickAccessVisible = false; Check(Closed(popup), "Hiding the toolbar closes its transient surface");
                ribbon.QuickAccessVisible = true; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; form.Width = 300; await Task.Delay(120); ribbon.Refresh();
                await Capture("narrow-below"); await Click(ribbon, record.Overflow); popup = Popup(); await Key("{END}{ENTER}");
                Check(lastCalls == 3 && Closed(popup), "The below-ribbon overflow keeps commands reachable in a narrow Form");
                form.Width = 1000; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; await Task.Delay(100); ribbon.Refresh();
                await Click(ribbon, record.Overflow); popup = Popup(); ribbon.QuickAccessItems.Remove(duplicate);
                Check(Closed(popup) && bold.Checked, "Removing one presentation closes stale overflow without changing source state");
                copy.Dispose(); ribbon.Refresh(); await Click(ribbon, record.Entries[copyRef]); Check(copies == 3, "A disposed source leaves a harmless disabled reference");
                log.Add($"PASS: {(flat ? "Neutral" : "Office")} Quick Access at {ribbon.DeviceDpi} DPI"); completed = true;
            }
            catch (Exception e) { failure = e; log.Add(e.ToString()); }
            finally { WF.Cursor.Position = cursor; form.Close(); }
        };
        try { WF.Application.Run(form); } catch (Exception e) { failure = e; log.Add(e.ToString()); }
        File.WriteAllLines(Path.Combine(directory, "result.log"), log);
        if (failure is not null) Console.Error.WriteLine(failure);
        else Console.WriteLine(string.Join(Environment.NewLine, log));
        return completed && failure is null ? 0 : 1;

        void Check(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        void Guard()
        {
            nint foreground = GetForegroundWindow();
            if (foreground != form.Handle && WF.Control.FromHandle(foreground) is not WF.ToolStripDropDown)
                throw new InvalidOperationException("The test Form lost foreground focus.");
        }
        async Task Click(WF.Control surface, Rectangle bounds)
        {
            Guard(); var visible = Rectangle.Intersect(surface.ClientRectangle, bounds); Check(!visible.IsEmpty, "Native click target is visible");
            WF.Cursor.Position = surface.PointToScreen(new(visible.Left + visible.Width / 2, visible.Top + visible.Height / 2)); await Task.Delay(40); Guard();
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(100);
        }
        async Task Key(string keys) { Guard(); WF.SendKeys.SendWait(keys); await Task.Delay(120); }
        async Task Capture(string name)
        {
            await Task.Delay(60); Guard(); var bounds = form.Bounds;
            if (WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup) bounds = Rectangle.Union(bounds, popup.Bounds);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup
        ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected the library overflow popup.");
    private static bool Closed(WF.Control control) => control.IsDisposed || !control.Visible;
    private sealed class State : INotifyPropertyChanged
    {
        private bool _bold;
        public bool Bold { get => _bold; set { if (_bold == value) return; _bold = value; PropertyChanged?.Invoke(this, new(nameof(Bold))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class TestIcon : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        {
            using var pen = new Pen(context.Enabled ? Color.SteelBlue : SystemColors.GrayText, Math.Max(1, context.DeviceDpi / 96f));
            var rect = Rectangle.Inflate(bounds, -2, -2); graphics.DrawRectangle(pen, rect); graphics.DrawLine(pen, rect.Left + 2, rect.Top + rect.Height / 2, rect.Right - 2, rect.Top + rect.Height / 2);
        }
    }
    private sealed class Recording
    {
        internal readonly Dictionary<RibbonElement, Rectangle> Entries = [], MenuEntries = [];
        internal readonly Dictionary<RibbonElement, string> Captions = [];
        internal readonly HashSet<string> Tips = [];
        internal Rectangle Overflow, Strip, Up, Down;
        internal bool UpEnabled, DownEnabled, DownHot, DownPressed;
        internal void Scroll(RibbonPaintContext context, bool upward, bool enabled)
        {
            if (upward) { Up = context.Bounds; UpEnabled = enabled; }
            else { Down = context.Bounds; DownEnabled = enabled; DownHot = context.Hot; DownPressed = context.Pressed; }
        }
        internal void Item(RibbonPaintContext c, RibbonElement entry, bool menu)
        {
            (menu ? MenuEntries : Entries)[entry] = c.Bounds;
            Captions[entry] = entry.Text.Length > 0 ? entry.Text : (entry as RibbonQuickAccessReference)?.Source?.Text ?? "";
        }
    }
    private sealed class OfficeRecorder(Recording record) : RibbonOffice2024Renderer
    {
        public override void DrawSurface(RibbonPaintContext c, bool popup) { if (popup) record.MenuEntries.Clear(); base.DrawSurface(c, popup); }
        public override void DrawOverflowScrollButton(RibbonPaintContext c, bool upward, bool enabled) { record.Scroll(c, upward, enabled); base.DrawOverflowScrollButton(c, upward, enabled); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { record.Item(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { record.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext c, RibbonQuickAccessPosition position) { record.Strip = c.Bounds; base.DrawQuickAccessToolbar(c, position); }
        public override void DrawScreenTip(RibbonPaintContext c, string title, string description, Rectangle titleBounds, Rectangle descriptionBounds) { record.Tips.Add(title); base.DrawScreenTip(c, title, description, titleBounds, descriptionBounds); }
    }
    private sealed class FlatRecorder(Recording record) : RibbonRenderer
    {
        public override void DrawSurface(RibbonPaintContext c, bool popup) { if (popup) record.MenuEntries.Clear(); base.DrawSurface(c, popup); }
        public override void DrawOverflowScrollButton(RibbonPaintContext c, bool upward, bool enabled) { record.Scroll(c, upward, enabled); base.DrawOverflowScrollButton(c, upward, enabled); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { record.Item(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { record.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext c, RibbonQuickAccessPosition position) { record.Strip = c.Bounds; base.DrawQuickAccessToolbar(c, position); }
        public override void DrawScreenTip(RibbonPaintContext c, string title, string description, Rectangle titleBounds, Rectangle descriptionBounds) { record.Tips.Add(title); base.DrawScreenTip(c, title, description, titleBounds, descriptionBounds); }
    }
}
