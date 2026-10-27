using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Copied unchanged into the isolated package consumer. This Form supplies no
// popup, layout or editor-transfer implementation and uses only public APIs.
internal static class MinimizeInteractionChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetFocus();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, nuint extra);

    internal static int Run(string directory, bool flat = false)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); var results = new List<string>(); var recording = new Recording();
        using RibbonRenderer renderer = flat ? new FlatRecorder(recording) : new OfficeRecorder(recording);
        using var form = new WF.Form { Text = "Public minimize checks", Width = 1150, Height = 600, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer }; var home = new RibbonTab { Text = "Home", KeyTip = "H" }; var insert = new RibbonTab { Text = "Insert", KeyTip = "N" };
        var commands = new RibbonGroup { Text = "Commands", ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse, KeyTip = "ZC" };
        var copy = new RibbonButtonItem { Text = "Copy", KeyTip = "C", ScreenTipDescription = "Copy the current selection." };
        var split = new RibbonSplitButtonItem { Text = "Paste", Layout = RibbonSplitButtonLayout.Vertical, KeyTip = "V", DropDownKeyTip = "M" };
        var paste = new RibbonButtonItem { Text = "Paste text only", MenuDescription = "Insert plain clipboard text." }; split.Items.Add(paste);
        var check = new RibbonCheckBoxItem { Text = "Show ruler", KeyTip = "B" };
        commands.Items.AddRange(copy, split, check, new RibbonButtonItem { Text = "Unavailable", Enabled = false });
        var fields = new RibbonGroup { Text = "Native fields", ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse, KeyTip = "ZF" };
        var name = new RibbonTextBoxItem { Id = "minimize.name", Text = "Name", Value = "Document", InputWidth = 150, KeyTip = "D" };
        var font = new RibbonComboBoxItem { Id = "minimize.font", Text = "Font", InputWidth = 150, IsEditable = true, KeyTip = "F" };
        font.Items.AddRange(new[] { "Arial", "Calibri", "Segoe UI" }); font.SelectedIndex = 1; fields.Items.AddRange(name, font);
        name.ValueValidating += (_, e) => e.Cancel = name.Value.Contains('!');
        var menus = new RibbonGroup { Text = "Menus", KeyTip = "ZM" }; var more = new RibbonDropDownItem { Text = "More", KeyTip = "O" };
        var nested = new RibbonDropDownItem { Text = "Nested", MenuDescription = "Open another command level." };
        var choice = new RibbonButtonItem { Text = "Nested choice" }; nested.Items.Add(choice); more.Items.Add(nested); menus.Items.Add(more);
        home.Groups.AddRange(commands, fields, menus);
        var insertGroup = new RibbonGroup { Text = "Insert" }; var picture = new RibbonButtonItem { Text = "Picture", KeyTip = "P" }; insertGroup.Items.Add(picture); insert.Groups.Add(insertGroup);
        ribbon.Tabs.AddRange(home, insert);
        for (int i = 0; i < 4; i++)
        {
            var group = new RibbonGroup { Text = "Extra " + i, KeyTip = "Z" + i, ReductionPriority = 10 };
            group.Items.Add(new RibbonButtonItem { Text = "Extra command " + i }); home.Groups.Add(group);
        }
        for (int i = 0; i < 5; i++) ribbon.Tabs.Add(new RibbonTab { Text = "Additional " + i });
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Application document. Ctrl+F1 toggles the ribbon; double-click a tab for the same action." };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        using var source = new WF.BindingSource(); var state = new State(); source.DataSource = state;
        ribbon.DataBindings.Add(new WF.Binding(nameof(ribbon.IsMinimized), source, nameof(state.Minimized), false, WF.DataSourceUpdateMode.OnPropertyChanged));
        int clicks = 0, choices = 0, changes = 0; copy.Click += (_, _) => clicks++; choice.Click += (_, _) => choices++; ribbon.IsMinimizedChanged += (_, _) => changes++;
        WF.Form? other = null; bool completed = false; Exception? error = null; var originalCursor = WF.Cursor.Position;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); await Activate(); ribbon.Refresh();
                int expandedHeight = ribbon.Height; var commandSize = recording.Items[copy].Size;
                var nativeName = Descendants(ribbon).OfType<WF.TextBox>().Single(c => c.Name == name.Id);
                var nativeFont = Descendants(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == font.Id);
                await Capture("expanded"); await Click(document, document.ClientRectangle); nint documentFocus = GetFocus();
                state.Minimized = true; await Task.Delay(100);
                Check(ribbon.IsMinimized && ribbon.Height < expandedHeight && GetFocus() == documentFocus, "Bound minimized state reduces only the ribbon body and preserves document focus");
                Check(!nativeName.Visible && !nativeName.IsDisposed, "Minimize parks the existing native field");
                state.Minimized = false; await Task.Delay(100);
                Check(!ribbon.IsMinimized && ribbon.Height == expandedHeight && nativeName.Visible, "Source binding restores the full body and native field");
                await Key("^{F1}");
                Check(ribbon.IsMinimized && state.Minimized && GetFocus() == documentFocus, "Ctrl+F1 from the document updates the bindable state without taking focus");
                await Capture("minimized"); await OpenHome(); var body = Popup();
                Check(recording.Items[copy].Size == commandSize && body.TopLevelControl!.Top >= ribbon.PointToScreen(new(0, ribbon.Height)).Y, "A tab opens its shared group geometry below the minimized row");
                Check(ribbon.IsMinimized && ribbon.Height < expandedHeight, "Opening the temporary body does not expand the docked control");
                await Capture("temporary-body"); await Click(body, recording.Items[check]);
                Check(check.Checked && Closed(body) && ribbon.IsMinimized, "Option activation changes its source state once and dismisses the temporary body");
                recording.Keys.Clear(); await Key("{F10}"); await Capture("keytips-tabs");
                Check(recording.Keys.Contains("H"), "F10 paints the tab KeyTips");
                await Key("H"); await Capture("keytips-body");
                body = Popup(); await Key("B");
                Check(!check.Checked && Closed(body) && ribbon.IsMinimized, "Tab and command KeyTips traverse the minimized body");
                ribbon.Focus(); await Key("{HOME}{DOWN}"); body = Popup();
                Check(!Closed(body), "Down from the focused tab opens its groups");
                await Click(body, recording.Items[more]); var menu = Popup(); await Click(menu, recording.Items[nested]); var child = Popup();
                await Capture("nested-stack"); await Key("{ESC}");
                Check(Closed(child) && more.IsDropDownOpen && !Closed(body), "First Escape closes only the deepest command menu");
                await Key("{ESC}"); Check(!more.IsDropDownOpen && !Closed(body), "Second Escape preserves the temporary tab body");
                await Key("{ESC}"); Check(Closed(body) && ribbon.Focused, "Final Escape returns to the tab row");
                await OpenHome(); body = Popup(); await Click(body, recording.Items[more]); menu = Popup(); await Click(menu, recording.Items[nested]); child = Popup(); await Click(child, recording.Items[choice]);
                Check(choices == 1 && Closed(body) && ribbon.IsMinimized, "A nested command executes once and closes every transient level");
                await OpenHome(); body = Popup();
                Check(ReferenceEquals(nativeName, Descendants(body).OfType<WF.TextBox>().Single(c => c.Name == name.Id)), "The temporary body borrows the same TextBox instance");
                await Click(nativeName, nativeName.ClientRectangle); await Key("^aEdited document"); await Key("{HOME}{RIGHT}{RIGHT}+{RIGHT}+{RIGHT}+{RIGHT}");
                int start = nativeName.SelectionStart, length = nativeName.SelectionLength;
                await Key("^{F1}"); ribbon.Refresh();
                Check(!ribbon.IsMinimized && !state.Minimized && Closed(body) && nativeName.Visible && !nativeName.IsDisposed && name.Value == "Edited document"
                    && nativeName.SelectionStart == start && nativeName.SelectionLength == length, "Expanding from a popup field commits and preserves the same TextBox and selection");
                await Click(nativeName, nativeName.ClientRectangle); await Key("^aInvalid!"); await Key("^{F1}");
                Check(!ribbon.IsMinimized && nativeName.Focused && name.Value == "Invalid!", "Validation cancellation blocks the user toggle and leaves the draft active");
                ribbon.IsMinimized = true; await Task.Delay(100);
                Check(ribbon.IsMinimized && name.Value == "Edited document" && !nativeName.IsDisposed, "An authoritative property change uses normal invalid-draft rollback while parking");
                await OpenHome(); body = Popup();
                Check(ReferenceEquals(nativeFont, Descendants(body).OfType<WF.ComboBox>().Single(c => c.Name == font.Id)), "The temporary body borrows the same ComboBox instance");
                await Click(nativeFont, nativeFont.ClientRectangle); await Key("{F4}"); await Key("{DOWN}"); await Key("^{F1}");
                Check(!ribbon.IsMinimized && !font.IsDropDownOpen && font.SelectedIndex == 2 && !nativeFont.IsDisposed && nativeFont.Visible, "Ctrl+F1 commits and closes native choices before restoring the field");
                int before = changes; await HeldChord();
                Check(ribbon.IsMinimized && changes == before + 1, "Holding Ctrl+F1 toggles once and consumes repeat keydowns");
                await OpenHome(); body = Popup(); await Click(ribbon, recording.Minimize);
                Check(!ribbon.IsMinimized && Closed(body), "The chevron pins the expanded body and dismisses its temporary presentation");
                await Task.Delay(WF.SystemInformation.DoubleClickTime + 50); await DoubleClickTab(home);
                Check(ribbon.IsMinimized, "Double-clicking a tab minimizes the body");
                await Task.Delay(WF.SystemInformation.DoubleClickTime + 50); await DoubleClickTab(home);
                Check(!ribbon.IsMinimized, "Double-clicking a minimized tab restores the docked body");
                await Click(ribbon, recording.Minimize); await OpenHome(); body = Popup(); await Capture("before-deactivation");
                other = new WF.Form { Text = "Validation foreground switch", Width = 300, Height = 160 }; other.Controls.Add(new WF.TextBox { Dock = WF.DockStyle.Fill });
                other.Show(); other.Activate(); SetForegroundWindow(other.Handle); await Task.Delay(150);
                Check(GetForegroundWindow() == other.Handle && Closed(body), "Deactivation closes the minimized popup stack without stealing focus");
                bool mode = ribbon.IsMinimized; await Key("^{F1}"); Check(ribbon.IsMinimized == mode, "Ctrl+F1 in another Form does not toggle this ribbon");
                other.Close(); other.Dispose(); other = null; await Activate();
                form.Width = 280; await Task.Delay(100); ribbon.Refresh(); recording.Collapsed.Clear();
                await OpenHome(); body = Popup(); await Capture("narrow-body");
                var collapsed = recording.Collapsed.First(p => !Rectangle.Intersect(p.Value, body.ClientRectangle).IsEmpty);
                Check(recording.ScrollNext != Rectangle.Empty, "A narrow temporary body uses the shared body scrollers");
                var beforeScroll = collapsed.Value; await Click(body, recording.ScrollNext);
                Check(recording.Collapsed[collapsed.Key].Left < beforeScroll.Left && ribbon.IsMinimized, "Scrolling moves the temporary group viewport without changing minimized state");
                await Click(body, recording.ScrollBack); await Click(body, recording.Collapsed[collapsed.Key]); var groupPopup = Popup();
                Check(!ReferenceEquals(body, groupPopup) && !Closed(body), "A collapsed group opens above its existing temporary tab session");
                await Capture("collapsed-group"); await Key("{ESC}"); Check(!Closed(body), "Escape from a collapsed group preserves the tab session");
                await Key("{ESC}"); form.Width = 90; await Task.Delay(100); ribbon.Refresh(); ribbon.Focus();
                await Key("{HOME}{DOWN}"); var veryNarrowBody = Popup();
                Check(veryNarrowBody.Width >= (int)Math.Round(200 * ribbon.DeviceDpi / 96d), "A very narrow tab row still opens a usable temporary body through keyboard navigation");
                await Capture("very-narrow-body"); await Key("{ESC}");
                form.Width = 1150; await Task.Delay(100); ribbon.Refresh();
                await OpenHome(); body = Popup(); await Click(body, recording.Items[copy]);
                Check(clicks == 1 && Closed(body) && ribbon.IsMinimized, "A body command invokes once and retains minimized mode");
                await OpenHome(); body = Popup(); await Click(ribbon, recording.Tabs[insert]); var insertBody = Popup();
                Check(Closed(body) && ReferenceEquals(ribbon.SelectedTab, insert) && recording.Items.ContainsKey(picture), "Selecting another minimized tab replaces the temporary content");
                await Click(document, document.ClientRectangle); Check(Closed(insertBody), "An outside document click dismisses the temporary tab");
                await OpenHome(); body = Popup(); home.Enabled = false; await Task.Delay(100);
                Check(Closed(body) && ReferenceEquals(ribbon.SelectedTab, insert) && ribbon.IsMinimized, "Disabling the selected tab dismisses its temporary body before selecting a fallback");
                home.Enabled = true;
                await Task.Delay(WF.SystemInformation.DoubleClickTime + 50); // A separate tab gesture after source-driven dismissal.
                await OpenHome(); body = Popup(); form.Left += 10; await Task.Delay(100); Check(Closed(body), "Moving the host closes the temporary body");
                await OpenHome(); body = Popup(); ribbon.Tabs.Remove(home); await Task.Delay(100);
                Check(Closed(body) && ReferenceEquals(ribbon.SelectedTab, insert) && ribbon.IsMinimized && nativeName.IsDisposed && nativeFont.IsDisposed
                    && name.Value == "Edited document", "Removing the selected tab closes stale windows, releases its presentations and applies selection fallback");
                home.Dispose();
                await Capture("selection-fallback"); completed = true; results.Add($"PASS {(flat ? "Neutral" : "Office")} minimize checks at {ribbon.DeviceDpi} DPI");
            }
            catch (Exception e)
            {
                error = e; results.Add("FAIL " + e);
                try { await Capture("failure"); } catch (Exception captureError) { results.Add("Failure capture unavailable: " + captureError.Message); }
            }
            finally
            {
                other?.Dispose();
                if (GetForegroundWindow() == form.Handle || WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown) WF.Cursor.Position = originalCursor;
                form.Close();
            }
        };
        WF.Application.Run(form); File.WriteAllLines(Path.Combine(directory, "result.log"), results);
        return completed && error is null ? 0 : 1;

        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); results.Add("PASS " + message); }
        void Guard() => Check(GetForegroundWindow() == form.Handle || (other is { IsDisposed: false } && GetForegroundWindow() == other.Handle)
            || WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown, "Validation window or its popup owns foreground before input");
        async Task Activate()
        {
            form.Activate(); SetForegroundWindow(form.Handle);
            for (int i = 0; i < 20 && GetForegroundWindow() != form.Handle; i++) await Task.Delay(50);
            Guard();
        }
        async Task OpenHome() { ribbon.Refresh(); await Click(ribbon, recording.Tabs[home]); Check(ribbon.IsMinimized && WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown, "A minimized tab opens through native mouse input"); }
        async Task Click(WF.Control surface, Rectangle bounds)
        {
            Guard(); var visible = Rectangle.Intersect(surface.ClientRectangle, bounds); Check(!visible.IsEmpty, "Native click target is visible");
            WF.Cursor.Position = surface.PointToScreen(Centre(visible)); await Task.Delay(40); Guard();
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(100);
        }
        async Task DoubleClickTab(RibbonTab tab)
        {
            ribbon.Refresh(); Guard(); WF.Cursor.Position = ribbon.PointToScreen(Centre(recording.Tabs[tab]));
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(70); Guard();
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(100);
        }
        async Task Key(string keys) { Guard(); Check(WF.Control.ModifierKeys == WF.Keys.None, "Input modifiers are released"); WF.SendKeys.SendWait(keys); await Task.Delay(100); }
        async Task HeldChord()
        {
            Guard(); Check(WF.Control.ModifierKeys == WF.Keys.None, "Input modifiers are released");
            try
            {
                keybd_event(0x11, 0, 0, 0); keybd_event(0x70, 0, 0, 0); await Task.Delay(80);
                Guard(); keybd_event(0x70, 0, 0, 0); await Task.Delay(80);
                Guard(); keybd_event(0x70, 0, 0, 0); await Task.Delay(80);
            }
            finally { keybd_event(0x70, 0, 2, 0); keybd_event(0x11, 0, 2, 0); }
            await Task.Delay(100);
        }
        async Task Capture(string name)
        {
            await Task.Delay(70); Guard(); var bounds = form.Bounds;
            if (WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup) bounds = Rectangle.Union(bounds, popup.Bounds);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private static bool Closed(WF.Control c) => c.IsDisposed || !c.Visible;
    private static Point Centre(Rectangle b) => new(b.Left + b.Width / 2, b.Top + b.Height / 2);
    private static IEnumerable<WF.Control> Descendants(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup
        ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected a library popup");
    private sealed class State : INotifyPropertyChanged
    {
        private bool _minimized;
        public bool Minimized { get => _minimized; set { if (_minimized == value) return; _minimized = value; PropertyChanged?.Invoke(this, new(nameof(Minimized))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Recording
    {
        internal readonly Dictionary<RibbonItem, Rectangle> Items = [];
        internal readonly Dictionary<RibbonTab, Rectangle> Tabs = [];
        internal readonly Dictionary<RibbonGroup, Rectangle> Collapsed = [];
        internal Rectangle Minimize, ScrollBack, ScrollNext;
        internal int TabHeight;
        internal void Scroll(RibbonPaintContext c, bool backward)
        {
            // This fixture's default body scrollers are taller than its tab row.
            // Identify the recorded public slot without an internal paint flag.
            if (c.Bounds.Height <= TabHeight) return;
            if (backward) ScrollBack = c.Bounds; else ScrollNext = c.Bounds;
        }
        internal readonly HashSet<string> Keys = [];
    }
    private sealed class OfficeRecorder(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawKeyTip(RibbonPaintContext c, string text) { r.Keys.Add(text); base.DrawKeyTip(c, text); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.TabHeight = c.Bounds.Height; r.Tabs[tab] = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Items[item] = c.Bounds; base.DrawItem(c, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext p, RibbonPaintContext d, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { r.Items[item] = p.Bounds; base.DrawSplitItem(p, d, item, size, menu); }
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup group, bool collapsed) { if (collapsed) r.Collapsed[group] = c.Bounds; base.DrawGroup(c, group, collapsed); }
        public override void DrawMinimizeButton(RibbonPaintContext c, bool minimized) { r.Minimize = c.Bounds; base.DrawMinimizeButton(c, minimized); }
        public override void DrawScrollButton(RibbonPaintContext c, bool backward) { r.Scroll(c, backward); base.DrawScrollButton(c, backward); }
    }
    private sealed class FlatRecorder(Recording r) : RibbonRenderer
    {
        public override void DrawKeyTip(RibbonPaintContext c, string text) { r.Keys.Add(text); base.DrawKeyTip(c, text); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.TabHeight = c.Bounds.Height; r.Tabs[tab] = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Items[item] = c.Bounds; base.DrawItem(c, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext p, RibbonPaintContext d, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { r.Items[item] = p.Bounds; base.DrawSplitItem(p, d, item, size, menu); }
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup group, bool collapsed) { if (collapsed) r.Collapsed[group] = c.Bounds; base.DrawGroup(c, group, collapsed); }
        public override void DrawMinimizeButton(RibbonPaintContext c, bool minimized) { r.Minimize = c.Bounds; base.DrawMinimizeButton(c, minimized); }
        public override void DrawScrollButton(RibbonPaintContext c, bool backward) { r.Scroll(c, backward); base.DrawScrollButton(c, backward); }
    }
}
