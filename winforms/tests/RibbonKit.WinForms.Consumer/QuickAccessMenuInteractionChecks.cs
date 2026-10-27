using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public renderer geometry and native input; the same file runs outside the
// checkout against a package, without Showcase resources or implementation.
internal static class QuickAccessMenuInteractionChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);

    internal static int Run(string directory, bool flat = false)
    {
        directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        using var form = new WF.Form { Text = "Independent QAT menu and split consumer", Width = 1050, Height = 490, StartPosition = WF.FormStartPosition.CenterScreen };
        var r = new Recording(); using RibbonRenderer renderer = flat ? new FlatRecorder(r) : new OfficeRecorder(r);
        var ribbon = new RibbonControl { Renderer = renderer };
        var home = new RibbonTab { Text = "Home", KeyTip = "H" }; var other = new RibbonTab { Text = "Other", KeyTip = "O" }; var group = new RibbonGroup { Text = "Source commands" };
        var menu = new RibbonDropDownItem { Text = "Select", KeyTip = "S", ScreenTipDescription = "The same source menu, presented at Quick Access." };
        var action = new RibbonButtonItem { Text = "Select all", KeyTip = "A", MenuDescription = "Execute the original source command." };
        var toggle = new RibbonToggleItem { Text = "Keep selection", KeyTip = "K" };
        var nested = new RibbonDropDownItem { Text = "More", KeyTip = "N" }; var nestedAction = new RibbonButtonItem { Text = "Special selection", KeyTip = "X" }; nested.Items.Add(nestedAction);
        var name = new RibbonTextBoxItem { Id = "qat.menu.name", Text = "Name", Value = "Document", KeyTip = "V", InputWidth = 150 };
        var combo = new RibbonComboBoxItem { Id = "qat.menu.style", Text = "Style", KeyTip = "Y", InputWidth = 150 }; combo.Items.AddRange("Normal", "Heading", "Caption"); combo.SelectedIndex = 0;
        menu.Items.AddRange(action, toggle, new RibbonMenuSeparatorItem(), nested, name, combo);
        var split = new RibbonSplitButtonItem { Text = "Track changes", KeyTip = "T", Layout = RibbonSplitButtonLayout.Vertical, CheckOnClick = true };
        var splitChoice = new RibbonButtonItem { Text = "Track selected text", KeyTip = "C", MenuDescription = "A menu action leaves the primary checked state alone." }; split.Items.Add(splitChoice);
        group.Items.AddRange(menu, split); home.Groups.Add(group); ribbon.Tabs.AddRange(home, other);
        var first = new RibbonQuickAccessDropDownReference { Source = menu }; var duplicate = new RibbonQuickAccessDropDownReference { Source = menu, Text = "Selection tools" };
        var splitRef = new RibbonQuickAccessDropDownReference { Source = split };
        ribbon.QuickAccessItems.AddRange(first, duplicate, splitRef);
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "The library owns QAT menus, split targets, nested dismissal and native editor presentation. Sources retain commands and handlers." };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        int actions = 0, primaries = 0, choices = 0, opened = 0, closed = 0, unavailable = 0; bool cancel = false;
        var order = new List<string>(); action.Click += (_, _) => actions++; split.Click += (_, _) => { primaries++; order.Add("click"); }; split.CheckedChanged += (_, _) => order.Add("check"); splitChoice.Click += (_, _) => choices++;
        var dynamic = new RibbonButtonItem { Text = "Created on opening" };
        menu.DropDownOpening += (_, e) =>
        {
            opened++; e.Cancel = cancel;
            if (!cancel) { if (!menu.Items.Contains(dynamic)) menu.Items.Add(dynamic); dynamic.Text = "Opening " + opened; }
        };
        menu.DropDownClosed += (_, _) => closed++;
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
                await Capture("tab-row"); ribbon.SelectedTab = other; ribbon.Refresh(); await Open(first); var popup = Popup();
                var anchor = ribbon.RectangleToScreen(r.Entries[first]);
                Check(popup.TopLevelControl!.Top >= anchor.Bottom && Math.Abs(popup.TopLevelControl.Left - anchor.Left) <= 2, "An inactive source menu opens beneath its QAT presentation");
                Check(!menu.IsDropDownOpen && !duplicate.IsDropDownOpen && ReferenceEquals(ribbon.SelectedTab, other), "Source and references keep separate open state without selecting the source tab");
                await Capture("inactive-source-menu"); var editor = Tree(popup).OfType<WF.TextBox>().Single(c => c.Name == name.Id); nint editorHandle = editor.Handle;
                await Click(popup, r.Items[action]); Check(actions == 1 && opened == 1 && closed == 1 && !first.IsDropDownOpen, "A projected menu calls its original handler and source opening/closed events once");
                await Click(ribbon, r.Primary[splitRef]); Check(split.Checked && primaries == 1 && order.SequenceEqual(new[] { "check", "click" }) && !splitRef.IsDropDownOpen, "The primary half toggles before exactly one source Click");
                await Click(ribbon, r.Arrows[splitRef]); Check(splitRef.IsDropDownOpen && split.Checked && primaries == 1, "The split arrow opens without invoking or toggling its primary");
                await Capture("split-menu"); await Key("{ESC}{ENTER}"); Check(splitRef.IsDropDownOpen && primaries == 1, "Escape restores the menu half and Enter reopens that half");
                popup = Popup(); await Click(popup, r.Items[splitChoice]); Check(choices == 1 && primaries == 1 && split.Checked && !splitRef.IsDropDownOpen, "A split menu action executes independently and dismisses the menu");
                ribbon.Focus(); await Key("{F10}3A"); Check(primaries == 2 && !split.Checked, "Automatic primary KeyTip activates only the primary half");
                await Key("{F10}3BC"); Check(choices == 2 && primaries == 2 && !split.Checked && !splitRef.IsDropDownOpen, "Automatic arrow KeyTip continues into source menu KeyTips");
                await Key("{F10}1N"); Check(first.IsDropDownOpen && nested.IsDropDownOpen, "A reference menu preserves nested source KeyTip navigation");
                await Capture("nested-keytips"); await Key("{ESC}"); Check(first.IsDropDownOpen && !nested.IsDropDownOpen, "Escape dismisses the nested menu before its reference menu");
                await Key("{ESC}"); Check(!first.IsDropDownOpen, "The next Escape returns to Quick Access");
                await Open(first); await Open(duplicate); popup = Popup();
                Check(!first.IsDropDownOpen && duplicate.IsDropDownOpen && editor.Handle == editorHandle && Tree(popup).Contains(editor), "Two references share one source menu and the same native field");
                Check(menu.Items.Contains(name) && menu.Items.Contains(combo) && menu.Items.Contains(action), "Borrowed commands remain in the source collection");
                await Click(editor, editor.ClientRectangle); await Key("^aRenamed document{ENTER}"); Check(name.Value == "Renamed document", "A projected native field edits its original model");
                var nativeCombo = Tree(popup).OfType<WF.ComboBox>().Single(c => c.Name == combo.Id);
                await Click(nativeCombo, nativeCombo.ClientRectangle); await Key("{F4}{DOWN}{ENTER}");
                Check(combo.Value == "Heading" && duplicate.IsDropDownOpen, "Native ComboBox choices work inside the borrowed menu");
                await Key("{ESC}"); Check(!duplicate.IsDropDownOpen, "Editor Escape closes the containing projected menu");
                ribbon.SelectedTab = home; menu.ShowDropDown(); await Task.Delay(80); popup = Popup();
                Check(menu.IsDropDownOpen && !first.IsDropDownOpen && !duplicate.IsDropDownOpen && Tree(popup).Contains(editor) && editor.Handle == editorHandle, "The ordinary source menu reuses the editor after QAT closes");
                menu.CloseDropDown(); ribbon.SelectedTab = other;
                ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; ribbon.IsMinimized = true; ribbon.Refresh();
                await Open(first); await Capture("minimized-below-menu"); await Key("{ESC}"); Check(ribbon.IsMinimized, "BelowRibbon menus work without expanding a minimized ribbon");
                ribbon.IsMinimized = false; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; ribbon.QuickAccessMaxWidth = 28; ribbon.Refresh();
                await Click(ribbon, r.Overflow); var overflow = Popup(); await Capture("overflow-split");
                await Click(overflow, r.MenuEntries[first]); popup = Popup();
                var row = overflow.RectangleToScreen(r.MenuEntries[first]); Check(popup.TopLevelControl!.Left >= row.Right && overflow.Visible && r.Items.ContainsKey(dynamic), "Opening callbacks populate the source menu beside its overflow row while retaining the parent");
                await Click(popup, r.Items[nested]); await Capture("overflow-nested-stack"); await Key("{ESC}"); Check(first.IsDropDownOpen && overflow.Visible, "Nested Escape retains the reference and overflow parents");
                await Key("{ESC}"); Check(!first.IsDropDownOpen && overflow.Visible, "The reference menu closes before overflow"); await Key("{ESC}");
                await Click(ribbon, r.Overflow); overflow = Popup(); await Click(overflow, r.MenuPrimary[splitRef]);
                Check(primaries == 3 && split.Checked && !splitRef.IsDropDownOpen && overflow.Visible, "A toggle split primary executes once and retains overflow");
                await Click(overflow, r.MenuPrimary[splitRef]);
                Check(primaries == 4 && !split.Checked && overflow.Visible, "The same split primary can toggle off without reopening overflow");
                await Key(" "); Check(primaries == 5 && split.Checked && overflow.Visible, "Space repeats the focused split toggle while retaining overflow");
                await Click(overflow, r.MenuArrows[splitRef]);
                Check(splitRef.IsDropDownOpen && primaries == 5, "The overflow split arrow remains a separate target");
                await Capture("overflow-split-menu"); await Key("{ESC}{ESC}");
                ribbon.QuickAccessMaxWidth = 240; ribbon.Refresh(); cancel = true; int before = closed;
                await Click(ribbon, r.Entries[first]); Check(!first.IsDropDownOpen && closed == before, "A canceled source opening creates no popup or closing event"); cancel = false;
                await Open(first); menu.Enabled = false; Check(!first.IsDropDownOpen, "Disabling a borrowed source closes its presentation"); menu.Enabled = true;
                await Open(first); ribbon.QuickAccessItems.Remove(first); Check(!first.IsDropDownOpen && !editor.IsDisposed && menu.Items.Contains(name), "Removing a reference closes its menu without disposing source models or editor");
                ribbon.Refresh(); await Open(duplicate); menu.Dispose(); Check(!duplicate.IsDropDownOpen && editor.IsDisposed, "Disposing the source tears down its presentations and owned editor");
                Check(unavailable == 0, "Projected menu input/removal/teardown raises zero unavailable-element exceptions");
                log.Add($"PASS: {(flat ? "Neutral" : "Office")} QAT menus/splits at {ribbon.DeviceDpi} DPI"); completed = true;
            }
            catch (Exception e) { error = e; log.Add("FAIL: " + e); try { await Capture("failure"); } catch { } }
            finally { WF.Cursor.Position = cursor; form.Close(); }
        };
        try { WF.Application.Run(form); }
        finally { AppDomain.CurrentDomain.FirstChanceException -= exceptions; }
        File.WriteAllLines(Path.Combine(directory, "result.log"), log);
        if (error is not null) Console.Error.WriteLine(error); else Console.WriteLine(string.Join(Environment.NewLine, log));
        return completed && error is null ? 0 : 1;

        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        void Guard() { if (GetForegroundWindow() != form.Handle && WF.Control.FromHandle(GetForegroundWindow()) is not WF.ToolStripDropDown) throw new InvalidOperationException("Validation Form lost foreground focus."); }
        async Task Key(string keys) { Guard(); WF.SendKeys.SendWait(keys); await Task.Delay(100); }
        async Task Open(RibbonQuickAccessDropDownReference reference) { ribbon.Refresh(); await Click(ribbon, r.Entries[reference]); Check(reference.IsDropDownOpen, "Native pointer opens the reference menu"); }
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
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected a library menu popup.");
    private sealed class Recording
    {
        internal readonly Dictionary<RibbonElement, Rectangle> Entries = [], MenuEntries = [], Primary = [], Arrows = [], MenuPrimary = [], MenuArrows = [];
        internal readonly Dictionary<RibbonItem, Rectangle> Items = [];
        internal Rectangle Overflow;
        internal void Item(RibbonPaintContext c, RibbonElement entry, bool menu) => (menu ? MenuEntries : Entries)[entry] = c.Bounds;
        internal void Split(RibbonPaintContext p, RibbonPaintContext d, RibbonElement entry, bool menu)
        { (menu ? MenuPrimary : Primary)[entry] = p.Bounds; (menu ? MenuArrows : Arrows)[entry] = d.Bounds; }
    }
    private sealed class OfficeRecorder(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Item(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessSplitItem(RibbonPaintContext p, RibbonPaintContext d, RibbonQuickAccessDropDownReference entry, bool menu) { r.Split(p, d, entry, menu); base.DrawQuickAccessSplitItem(p, d, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { if (menu) r.Items[item] = c.Bounds; base.DrawItem(c, item, size, menu); }
    }
    private sealed class FlatRecorder(Recording r) : RibbonRenderer
    {
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Item(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessSplitItem(RibbonPaintContext p, RibbonPaintContext d, RibbonQuickAccessDropDownReference entry, bool menu) { r.Split(p, d, entry, menu); base.DrawQuickAccessSplitItem(p, d, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { if (menu) r.Items[item] = c.Bounds; base.DrawItem(c, item, size, menu); }
    }
}
