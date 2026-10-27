using System.ComponentModel;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public-only fixture, shared by the native runner and external package consumer.
internal static class QuickAccessGroupInteractionChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint handle);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);

    internal static int Run(string directory, bool flat = false)
    {
        directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        using var form = new WF.Form { Text = "Independent QAT group consumer", Width = 1100, Height = 500, StartPosition = WF.FormStartPosition.CenterScreen };
        var record = new Recording(); using RibbonRenderer renderer = flat ? new Flat(record) : new Office(record);
        var ribbon = new RibbonControl { Renderer = renderer };
        var home = new RibbonTab { Text = "Home", KeyTip = "H" }; var other = new RibbonTab { Text = "Other", KeyTip = "O" };
        var group = new RibbonGroup { Text = "Formatting", CanResize = false, KeyTip = "ZF" };
        var medium = RibbonSizePolicy.Fixed(RibbonItemSize.Medium);
        var bold = new RibbonToggleItem { Text = "Bold", KeyTip = "B", SizePolicy = medium };
        var ruler = new RibbonCheckBoxItem { Text = "Ruler", KeyTip = "R" };
        var track = new RibbonSplitButtonItem { Text = "Track changes", KeyTip = "T", DropDownKeyTip = "D", CheckOnClick = true, SizePolicy = medium };
        var trackChoice = new RibbonButtonItem { Text = "Track selected text", KeyTip = "C" }; track.Items.Add(trackChoice);
        var more = new RibbonDropDownItem { Text = "More", KeyTip = "N", SizePolicy = medium }; var action = new RibbonButtonItem { Text = "Action", KeyTip = "X" }; more.Items.Add(action);
        var name = new RibbonTextBoxItem { Id = "group.name", Text = "Name", Value = "Document", KeyTip = "V", InputWidth = 140 };
        var font = new RibbonComboBoxItem { Id = "group.font", Text = "Style", KeyTip = "F", InputWidth = 140 }; font.Items.AddRange("Normal", "Heading"); font.SelectedIndex = 0;
        var copy = new RibbonButtonItem { Text = "Copy", KeyTip = "C", SizePolicy = medium };
        group.Items.AddRange(bold, ruler, track, more, name, font, copy); home.Groups.Add(group); ribbon.Tabs.AddRange(home, other);
        var first = new RibbonQuickAccessGroupReference { Source = group }; var duplicate = new RibbonQuickAccessGroupReference { Source = group, Text = "Formatting tools" };
        var fontRef = new RibbonQuickAccessComboBoxReference { Source = font };
        var document = new WF.TextBox { Multiline = true, Dock = WF.DockStyle.Fill, Text = "The library owns group references, retained collapse and native flyouts. Commands, values and bindings belong to this consumer." };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        using var binding = new WF.BindingSource(); var state = new State(); binding.DataSource = state;
        bold.DataBindings.Add(new WF.Binding(nameof(bold.Checked), binding, nameof(state.Bold), false, WF.DataSourceUpdateMode.OnPropertyChanged));
        int toggles = 0, primaries = 0, actions = 0, copies = 0, commits = 0, unavailable = 0;
        bold.Click += (_, _) => toggles++; track.Click += (_, _) => primaries++; action.Click += (_, _) => actions++; trackChoice.Click += (_, _) => actions++;
        copy.Click += (_, _) => copies++; name.ValueCommitted += (_, _) => commits++;
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
                var editor = Tree(ribbon).OfType<WF.TextBox>().Single(c => c.Name == name.Id); nint editorHandle = editor.Handle;
                var chooser = Tree(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == font.Id); nint chooserHandle = chooser.Handle;
                int expandedWidth = record.Group.Width;
                ribbon.QuickAccessItems.AddRange(first, duplicate, fontRef); ribbon.Refresh();
                Check(record.Collapsed && record.Group.Width < expandedWidth && !group.CanResize && !editor.IsDisposed, "QAT membership collapses a fixed source group while retaining its editor and authored policy");
                await Capture("membership-collapsed"); ribbon.SelectedTab = other; ribbon.Refresh();
                await Click(ribbon, record.Entries[fontRef]); await Key("{HOME}{DOWN}{ENTER}");
                Check(font.Value == "Heading" && !fontRef.IsDropDownOpen && ReferenceEquals(ribbon.SelectedTab, other), "A ComboBox QAT chooser still uses its collapsed inactive source group");
                await Open(first); var popup = Popup();
                Check(ReferenceEquals(ribbon.SelectedTab, other) && Tree(popup).Contains(editor) && Tree(popup).Contains(chooser) && editor.Handle == editorHandle && chooser.Handle == chooserHandle,
                    "An inactive group flyout reuses the original TextBox and ComboBox handles without selecting its tab");
                await Capture("inactive-group-flyout"); await Click(editor, editor.ClientRectangle); await Key("^aRenamed document{ENTER}");
                Check(name.Value == "Renamed document" && commits == 1 && first.IsDropDownOpen, "The projected field commits once to its original source");
                await Click(chooser, chooser.ClientRectangle); await Key("{F4}{HOME}{ENTER}");
                Check(font.Value == "Normal" && first.IsDropDownOpen, "The shared native ComboBox remains usable in its group flyout");
                await Click(popup, record.Items[bold]); Check(bold.Checked && state.Bold && toggles == 1 && first.IsDropDownOpen, "A group toggle updates its source binding once and retains the flyout");
                await Click(popup, record.Items[bold]); await Click(popup, record.Items[ruler]);
                Check(!bold.Checked && !state.Bold && toggles == 2 && ruler.Checked && first.IsDropDownOpen, "Repeated toggles and checkboxes keep the same group flyout open");
                await Click(popup, record.Primary[track]); Check(track.Checked && primaries == 1 && first.IsDropDownOpen, "A checking split primary retains the group flyout");
                await Click(popup, record.Arrows[track]); Check(track.IsDropDownOpen && first.IsDropDownOpen && primaries == 1, "Its separate arrow opens the source menu without a second primary action");
                await Click(Popup(), record.Items[trackChoice]); Check(actions == 1 && !first.IsDropDownOpen && !track.IsDropDownOpen, "An ordinary nested menu action dismisses the stack");
                ribbon.Focus(); await Key("{F10}1B"); Check(first.IsDropDownOpen && bold.Checked && state.Bold && toggles == 3, "Group QAT KeyTips continue into the original command level");
                await Key(" {ESC}"); Check(!bold.Checked && toggles == 4 && !first.IsDropDownOpen, "Space repeats the focused toggle and Escape closes the group");
                await Open(first); await Open(duplicate); popup = Popup();
                Check(!first.IsDropDownOpen && duplicate.IsDropDownOpen && Tree(popup).Contains(editor) && editor.Handle == editorHandle, "Two group references share one live source presentation and editor");
                await Click(popup, record.Items[copy]); Check(copies == 1 && !duplicate.IsDropDownOpen, "An ordinary group command executes once and dismisses its flyout");
                ribbon.SelectedTab = home; ribbon.Refresh(); await Click(ribbon, record.Group); popup = Popup();
                Check(!first.IsDropDownOpen && !duplicate.IsDropDownOpen && Tree(popup).Contains(editor) && editor.Handle == editorHandle, "The source collapsed opener reuses the same native content");
                popup.MouseDown += (_, e) => log.Add($"SOURCE MOUSE DOWN {e.Location} clicks={e.Clicks}");
                popup.MouseUp += (_, e) => log.Add($"SOURCE MOUSE UP {e.Location} clicks={e.Clicks}");
                await Click(popup, record.Items[bold]); Check(bold.Checked && toggles == 5 && !popup.IsDisposed, $"The retained source opener has the same toggle policy (checked={bold.Checked}, calls={toggles}, disposed={popup.IsDisposed})"); await Key("{ESC}");
                ribbon.SelectedTab = other; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; ribbon.IsMinimized = true; ribbon.Refresh();
                await Open(first); await Capture("minimized-below-group"); Check(ribbon.IsMinimized, "BelowRibbon group references work while minimized"); await Key("{ESC}");
                ribbon.IsMinimized = false; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; ribbon.QuickAccessMaxWidth = 28; ribbon.Refresh();
                await Click(ribbon, record.Overflow); var overflow = Popup(); await Click(overflow, record.MenuEntries[first]); popup = Popup();
                var row = overflow.RectangleToScreen(record.MenuEntries[first]); Check(overflow.Visible && popup.TopLevelControl!.Left >= row.Right, "A group flyout opens beside its QAT overflow row and retains its parent");
                await Click(popup, record.Items[more]); await Capture("overflow-group-nested");
                await Key("{ESC}"); Check(first.IsDropDownOpen && overflow.Visible && !more.IsDropDownOpen, "Nested Escape retains group and overflow parents");
                await Key("{ESC}"); Check(!first.IsDropDownOpen && overflow.Visible, "The group closes before its overflow parent");
                await Key("{RIGHT}"); Check(first.IsDropDownOpen, "Right reopens the restored group row"); await Key("{ESC}{ESC}");
                ribbon.QuickAccessMaxWidth = 240; ribbon.Refresh(); await Open(first); group.Enabled = false;
                Check(!first.IsDropDownOpen, "Disabling a source closes its projected group flyout"); group.Enabled = true;
                await Open(duplicate); home.Groups.Remove(group);
                Check(!duplicate.IsDropDownOpen && editor.IsDisposed && chooser.IsDisposed && name.Value == "Renamed document", "Detaching the source closes its flyout and native presenters while retaining model values");
                home.Groups.Add(group); await Open(duplicate); popup = Popup();
                editor = Tree(popup).OfType<WF.TextBox>().Single(c => c.Name == name.Id); editorHandle = editor.Handle;
                chooser = Tree(popup).OfType<WF.ComboBox>().Single(c => c.Name == font.Id); chooserHandle = chooser.Handle;
                Check(editor.Text == "Renamed document" && font.Value == "Normal", "Reinsertion presents the retained source values in new native fields");
                await Key("{ESC}"); ribbon.SelectedTab = home; ribbon.QuickAccessItems.Remove(first); ribbon.Refresh();
                Check(record.Collapsed && !group.IsDisposed, "Removing one reference retains the membership constraint");
                ribbon.QuickAccessItems.Remove(duplicate); ribbon.Refresh();
                Check(!record.Collapsed && record.Group.Width == expandedWidth && editor.Handle == editorHandle && Tree(ribbon).Contains(editor), "Removing the last reference restores authored sizing and the original editor to the body");
                ribbon.QuickAccessItems.Add(first); ribbon.Refresh(); await Open(first); group.Dispose();
                Check(!first.IsDropDownOpen && editor.IsDisposed && chooser.IsDisposed, "Source disposal closes the flyout and releases its owned editors");
                Check(unavailable == 0, "Group switching/input/removal raises zero unavailable-element exceptions");
                log.Add($"PASS: {(flat ? "Neutral" : "Office")} QAT groups at {ribbon.DeviceDpi} DPI"); completed = true;
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
        async Task Open(RibbonQuickAccessGroupReference reference) { ribbon.Refresh(); await Click(ribbon, record.Entries[reference]); Check(reference.IsDropDownOpen, "Pointer opens the QAT group reference"); }
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
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected a library group/menu popup.");
    private sealed class State : INotifyPropertyChanged
    {
        private bool _bold;
        public bool Bold { get => _bold; set { if (_bold == value) return; _bold = value; PropertyChanged?.Invoke(this, new(nameof(Bold))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Recording
    {
        internal readonly Dictionary<RibbonElement, Rectangle> Entries = [], MenuEntries = [];
        internal readonly Dictionary<RibbonItem, Rectangle> Items = [], Primary = [], Arrows = [];
        internal Rectangle Group, Overflow; internal bool Collapsed;
        internal void Entry(RibbonPaintContext c, RibbonElement entry, bool menu) => (menu ? MenuEntries : Entries)[entry] = c.Bounds;
    }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup group, bool collapsed) { r.Group = c.Bounds; r.Collapsed = collapsed; base.DrawGroup(c, group, collapsed); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Entry(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Items[item] = c.Bounds; base.DrawItem(c, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext p, RibbonPaintContext d, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { r.Primary[item] = p.Bounds; r.Arrows[item] = d.Bounds; base.DrawSplitItem(p, d, item, size, menu); }
    }
    private sealed class Flat(Recording r) : RibbonRenderer
    {
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup group, bool collapsed) { r.Group = c.Bounds; r.Collapsed = collapsed; base.DrawGroup(c, group, collapsed); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Entry(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Items[item] = c.Bounds; base.DrawItem(c, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext p, RibbonPaintContext d, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { r.Primary[item] = p.Bounds; r.Arrows[item] = d.Bounds; base.DrawSplitItem(p, d, item, size, menu); }
    }
}
