using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class QuickAccessComboInteractionChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, WindowCallback callback, nint parameter);
    private delegate bool WindowCallback(nint window, nint parameter);

    internal static int Run(string directory, bool flat = false)
    {
        directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        using var form = new WF.Form { Text = "Independent QAT ComboBox chooser", Width = 1000, Height = 430, StartPosition = WF.FormStartPosition.CenterScreen };
        var record = new Recording(); using RibbonRenderer renderer = flat ? new FlatRecorder(record) : new OfficeRecorder(record);
        var ribbon = new RibbonControl { Renderer = renderer };
        var home = new RibbonTab { Text = "Home", KeyTip = "H" }; var other = new RibbonTab { Text = "Other", KeyTip = "O" };
        var group = new RibbonGroup { Text = "Native source fields" };
        var source = new RibbonComboBoxItem { Id = "qat.font", Text = "Font", InputWidth = 160, ScreenTipDescription = "Choose a font from Quick Access; the original field owns editing and selection." };
        source.Items.AddRange("Alpha", "Beta", "Blocked", "Gamma"); source.SelectedIndex = 0;
        var editable = new RibbonComboBoxItem { Id = "qat.zoom", Text = "Zoom", InputWidth = 160, IsEditable = true, Value = "137%" };
        editable.Items.AddRange("100%", "125%", "150%", "200%");
        group.Items.AddRange(source, editable); home.Groups.Add(group); ribbon.Tabs.AddRange(home, other);
        var first = new RibbonQuickAccessComboBoxReference { Source = source }; var duplicate = new RibbonQuickAccessComboBoxReference { Source = source, Text = "Text font" };
        var editReference = new RibbonQuickAccessComboBoxReference { Source = editable };
        ribbon.QuickAccessItems.AddRange(first, duplicate, editReference);
        using var binding = new WF.BindingSource(); var state = new State(); binding.DataSource = state;
        source.DataBindings.Add(new WF.Binding(nameof(source.Value), binding, nameof(state.Font), false, WF.DataSourceUpdateMode.OnValidation));
        source.ValueValidating += (_, e) => e.Cancel = source.Value == "Blocked";
        editable.ValueValidating += (_, e) => e.Cancel = editable.Value.Contains('!');
        int commits = 0, editCommits = 0; source.ValueCommitted += (_, _) => commits++; editable.ValueCommitted += (_, _) => editCommits++;
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Quick Access chooses existing values. Native typing remains in the original source field." };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        var log = new List<string>(); bool completed = false; Exception? error = null; var cursor = WF.Cursor.Position;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(150); Guard(); ribbon.Refresh();
                var sourceEditor = Tree(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == source.Id);
                var editEditor = Tree(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == editable.Id);
                nint sourceHandle = sourceEditor.Handle, editHandle = editEditor.Handle;
                await Capture("source-fields"); ribbon.SelectedTab = other; ribbon.Refresh();
                await Open(first); var choices = List(); await Capture("inactive-source-choices");
                using (var measure = choices.CreateGraphics())
                    Check(source.Items.All(text => WF.TextRenderer.MeasureText(measure, text, choices.Font, new(int.MaxValue, int.MaxValue), WF.TextFormatFlags.SingleLine | WF.TextFormatFlags.NoPadding | WF.TextFormatFlags.NoPrefix).Width
                        <= choices.ClientSize.Width - 2 * (int)Math.Round(6 * ribbon.DeviceDpi / 96d)), $"QAT choice labels fit their measured native list content width (client={choices.ClientSize}, listFont={choices.Font.SizeInPoints}/{choices.Font.Height}, ribbonFont={ribbon.Font.SizeInPoints}/{ribbon.Font.Height}, dpi={choices.DeviceDpi}/{ribbon.DeviceDpi}, maxText={source.Items.Max(text => WF.TextRenderer.MeasureText(measure, text, choices.Font, new(int.MaxValue, int.MaxValue), WF.TextFormatFlags.SingleLine | WF.TextFormatFlags.NoPadding | WF.TextFormatFlags.NoPrefix).Width)})");
                var anchor = ribbon.RectangleToScreen(record.Entries[first]);
                Check(choices.TopLevelControl!.Bounds.Top >= anchor.Bottom && Math.Abs(choices.TopLevelControl.Left - anchor.Left) < 3, "Inactive source choices open at the QAT entry");
                Check(first.IsDropDownOpen && !duplicate.IsDropDownOpen && !source.IsDropDownOpen, "The projection has its own open state");
                foreach (int index in new[] { 1, 2, 3 })
                {
                    WF.Cursor.Position = choices.PointToScreen(new(10, choices.GetItemRectangle(index).Top + choices.ItemHeight / 2)); await Task.Delay(70);
                    Check(source.SelectedIndex == 0 && state.Font == "Alpha" && commits == 0 && record.Rows.Count(row => row.Value.Hot) == 1,
                        "Hover highlights only its row without selecting or committing");
                }
                await Capture("hover-last-row");
                WF.Cursor.Position = document.PointToScreen(new(20, document.Height - 20)); await Task.Delay(100);
                Check(record.Rows.All(row => !row.Value.Hot) && record.Rows.Count(row => row.Value.Selected) == 1 && record.Rows[0].Selected,
                    "Leaving the list clears hover and retains only the actual selected row");
                await Capture("hover-cleared");
                WF.Cursor.Position = choices.PointToScreen(new(10, choices.ItemHeight + choices.ItemHeight / 2)); await Task.Delay(60);
                mouse_event(2, 0, 0, 0, 0); await Task.Delay(60);
                Check(source.SelectedIndex == 0 && commits == 0, "Pressing a row waits for a matching release before changing the source");
                WF.Cursor.Position = choices.PointToScreen(new(10, 3 * choices.ItemHeight + choices.ItemHeight / 2)); await Task.Delay(60);
                mouse_event(4, 0, 0, 0, 0); await Task.Delay(80);
                Check(source.SelectedIndex == 0 && commits == 0 && first.IsDropDownOpen, "Dragging to another row cancels the press without selecting");
                await Key("{DOWN}"); Check(source.Value == "Beta" && state.Font == "Alpha", "Keyboard preview shares source state while deferred binding waits");
                Check(record.Rows.All(row => !row.Value.Hot) && record.Rows.Count(row => row.Value.Selected) == 1 && record.Rows[1].Selected,
                    "Keyboard navigation clears pointer hover and repaints the old selection");
                await Capture("keyboard-selection");
                await Key("{ESC}"); Check(!first.IsDropDownOpen && source.Value == "Alpha" && commits == 0 && ribbon.Focused, "Escape restores the source and returns to its QAT opener");
                await Open(duplicate); await Key("{DOWN}{DOWN}{ENTER}");
                Check(duplicate.IsDropDownOpen && state.Font == "Alpha" && commits == 0, "Source validation rejects a QAT choice without committing");
                await Key("{ESC}"); Check(source.Value == "Alpha", "Rejected preview cancels to the previous value");
                await Open(first); choices = List(); await Click(choices, new(0, choices.ItemHeight, choices.ClientSize.Width, choices.ItemHeight));
                Check(!first.IsDropDownOpen && source.Value == "Beta" && state.Font == "Beta" && commits == 1, "A pointer choice commits the original binding once");
                ribbon.Focus(); await Key("{F10}2"); await Key("g{ENTER}");
                Check(source.Value == "Gamma" && state.Font == "Gamma" && commits == 2 && ReferenceEquals(ribbon.SelectedTab, other), "Numeric KeyTips and native incremental search work from inactive tabs");
                await Open(editReference); await Key("{DOWN}{ESC}");
                Check(editable.Value == "137%" && editable.SelectedIndex == -1 && editCommits == 0, "An editable source keeps unmatched text when the chooser is canceled");
                await Open(editReference); await Key("1{ENTER}");
                Check(editable.SelectedIndex >= 0 && editCommits == 1, "Editable-source QAT supports choice search without another text field");
                ribbon.SelectedTab = home; ribbon.Refresh();
                Check(sourceEditor.Handle == sourceHandle && editEditor.Handle == editHandle && Tree(ribbon).OfType<WF.ComboBox>().Count() == 2, "QAT reuses the source models and retains exactly the original two native editors");
                await Click(editEditor, editEditor.ClientRectangle); await Key("^a149{%}{ENTER}"); int acceptedEdits = editCommits;
                await Open(editReference); await Key("{ESC}");
                Check(editable.Value == "149%" && editable.SelectedIndex == -1 && editCommits == acceptedEdits, "Free typing remains available at the original source field");
                await Click(editEditor, editEditor.ClientRectangle); await Key("^aInvalid!"); ribbon.Refresh(); await Click(ribbon, record.Entries[editReference]);
                Check(!editReference.IsDropDownOpen && editEditor.ContainsFocus, "An invalid active source draft blocks the QAT chooser");
                await Key("{ESC}");
                ribbon.SelectedTab = other; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; ribbon.IsMinimized = true; ribbon.Refresh();
                await Open(first); await Capture("minimized-below-choices"); await Key("{ESC}");
                Check(ribbon.IsMinimized && !first.IsDropDownOpen, "BelowRibbon choosers remain usable while minimized");
                ribbon.IsMinimized = false; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; ribbon.QuickAccessMaxWidth = 56; ribbon.Refresh();
                await Click(ribbon, record.Overflow); var overflow = Popup(); await Capture("overflow");
                await Click(overflow, record.MenuEntries[duplicate]); choices = List(); await Capture("overflow-choices");
                var row = overflow.RectangleToScreen(record.MenuEntries[duplicate]);
                Check(choices.TopLevelControl!.Bounds.Left >= row.Right && !overflow.IsDisposed && overflow.Visible, "Overflow choices open beside their own row and preserve their parent");
                await Key("{HOME}{ENTER}"); Check(source.Value == "Alpha" && commits == 3 && !duplicate.IsDropDownOpen && overflow.Visible, "Choosing from overflow updates the source once and retains the parent menu");
                await Click(overflow, record.MenuEntries[duplicate]); await Key("{ESC}"); Check(overflow.Visible, "First Escape closes only the choice list");
                await Key("{ESC}"); Check(overflow.IsDisposed || !overflow.Visible, "Second Escape closes the QAT overflow");
                await Click(ribbon, record.Overflow); overflow = Popup(); await Click(overflow, record.MenuEntries[duplicate]);
                source.Items.Insert(0, "New first"); await Task.Delay(80);
                Check(!duplicate.IsDropDownOpen && source.Value == "Alpha" && source.SelectedIndex == 1, "Changing choices closes the stale list and preserves a surviving selection");
                await Click(overflow, record.MenuEntries[duplicate]); source.Value = "Gamma"; await Task.Delay(60);
                Check(!duplicate.IsDropDownOpen && source.Value == "Gamma", "An authoritative source update closes preview without rollback");
                await Click(overflow, record.MenuEntries[duplicate]); ribbon.QuickAccessItems.Remove(duplicate);
                Check(!duplicate.IsDropDownOpen && (overflow.IsDisposed || !overflow.Visible), "Removing a projection closes its list and stale parent surface");
                ribbon.QuickAccessMaxWidth = 240; ribbon.Refresh(); await Open(first); group.Items.Remove(source);
                Check(!first.IsDropDownOpen && sourceEditor.IsDisposed, "Removing the source closes its choice presentation and releases its editor");
                ribbon.Refresh(); await Click(ribbon, record.Entries[first]); Check(!first.IsDropDownOpen, "A detached source cannot reopen its chooser");
                group.Items.Add(source); ribbon.Refresh(); await Open(first); source.Dispose();
                Check(!first.IsDropDownOpen, "Source disposal tears down the chooser safely");
                log.Add($"PASS: {(flat ? "Neutral" : "Office")} QAT ComboBox at {ribbon.DeviceDpi} DPI"); completed = true;
            }
            catch (Exception e) { error = e; log.Add("FAIL: " + e); try { await Capture("failure"); } catch { } }
            finally { WF.Cursor.Position = cursor; form.Close(); }
        };
        WF.Application.Run(form); File.WriteAllLines(Path.Combine(directory, "result.log"), log);
        if (error is not null) Console.Error.WriteLine(error); else Console.WriteLine(string.Join(Environment.NewLine, log));
        return completed && error is null ? 0 : 1;

        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        void Guard() { if (GetForegroundWindow() != form.Handle && WF.Control.FromHandle(GetForegroundWindow()) is not WF.ToolStripDropDown) throw new InvalidOperationException("The validation Form lost foreground focus."); }
        async Task Key(string keys) { Guard(); WF.SendKeys.SendWait(keys); await Task.Delay(100); }
        async Task Click(WF.Control surface, Rectangle bounds)
        {
            Guard(); var visible = Rectangle.Intersect(surface.ClientRectangle, bounds); Check(!visible.IsEmpty, "Pointer target is visible");
            WF.Cursor.Position = surface.PointToScreen(new(visible.Left + visible.Width / 2, visible.Top + visible.Height / 2)); await Task.Delay(40); Guard();
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(100);
        }
        async Task Open(RibbonQuickAccessComboBoxReference reference) { ribbon.Refresh(); await Click(ribbon, record.Entries[reference]); Check(reference.IsDropDownOpen, "QAT chooser opens through native pointer input"); }
        async Task Capture(string name)
        {
            await Task.Delay(50); Guard(); var bounds = form.Bounds;
            foreach (var popup in Windows()) bounds = Rectangle.Union(bounds, popup.Bounds);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var g = Graphics.FromImage(bitmap); g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private static IEnumerable<WF.Control> Tree(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Tree(c)));
    private static IEnumerable<WF.ToolStripDropDown> Windows()
    {
        var windows = new List<WF.ToolStripDropDown>();
        EnumThreadWindows(GetCurrentThreadId(), (window, _) => { if (WF.Control.FromHandle(window) is WF.ToolStripDropDown { Visible: true } popup) windows.Add(popup); return true; }, 0);
        return windows;
    }
    private static WF.Control Popup() => Windows().SelectMany(popup => popup.Items.OfType<WF.ToolStripControlHost>().Select(host => host.Control)).First(control => control is not WF.ListBox);
    private static WF.ListBox List() => Windows().SelectMany(popup => popup.Items.OfType<WF.ToolStripControlHost>().Select(host => host.Control)).OfType<WF.ListBox>().Single();
    private sealed class State : INotifyPropertyChanged
    {
        private string _font = "Alpha";
        public string Font { get => _font; set { if (_font == value) return; _font = value; PropertyChanged?.Invoke(this, new(nameof(Font))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Recording
    {
        internal readonly Dictionary<RibbonElement, Rectangle> Entries = [], MenuEntries = [];
        internal readonly Dictionary<int, (bool Hot, bool Selected)> Rows = [];
        internal Rectangle Overflow;
        internal void Item(RibbonPaintContext context, RibbonElement entry, bool menu) { (menu ? MenuEntries : Entries)[entry] = context.Bounds; }
    }
    private sealed class OfficeRecorder(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Item(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawComboBoxItem(RibbonPaintContext c, RibbonComboBoxItem item, int index, bool selected) { r.Rows[index] = (c.Hot, selected); base.DrawComboBoxItem(c, item, index, selected); }
    }
    private sealed class FlatRecorder(Recording r) : RibbonRenderer
    {
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Item(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawComboBoxItem(RibbonPaintContext c, RibbonComboBoxItem item, int index, bool selected) { r.Rows[index] = (c.Hot, selected); base.DrawComboBoxItem(c, item, index, selected); }
    }
}
