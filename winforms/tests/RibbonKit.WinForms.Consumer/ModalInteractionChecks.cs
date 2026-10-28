using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public APIs and native input only; shared with the isolated package consumer.
internal static class ModalInteractionChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    internal static int Run(string directory, bool flat = false)
    {
        directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles(); WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        var record = new Recording(); using RibbonRenderer painter = flat ? new Flat(record) : new Office(record);
        using var form = new WF.Form { Text = "Independent modal consumer", Width = 1100, Height = 550, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = painter, FileMenuMode = RibbonFileMenuMode.Backstage }; ribbon.FileMenu.Visible = true;
        using var page = new WF.Panel(); ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        var home = new RibbonTab { Id = "home", Text = "Home", KeyTip = "H" }; var other = new RibbonTab { Text = "Other", KeyTip = "O" }; var group = new RibbonGroup { Text = "Application commands" };
        var launch = new RibbonButtonItem { Text = "Print preview", KeyTip = "P" }; var action = new RibbonButtonItem { Text = "Quick action" }; group.Items.AddRange(launch, action); home.Groups.Add(group); ribbon.Tabs.AddRange(home, other);
        using var source = new RibbonMergeSource(); var preview = new RibbonTab { Text = "Print Preview", KeyTip = "P", IsModal = true, CloseButtonText = "Close Print Preview" };
        var tools = new RibbonGroup { Text = "Preview settings" }; var copies = new RibbonTextBoxItem { Id = "modal.copies", Text = "Copies", Value = "1", KeyTip = "N" };
        copies.ValueValidating += (_, e) => e.Cancel = copies.Value == "";
        var menu = new RibbonDropDownItem { Text = "Options", KeyTip = "M" }; menu.Items.Add(new RibbonButtonItem { Text = "A preview option" }); tools.Items.AddRange(copies, menu); preview.Groups.Add(tools); source.Tabs.Add(preview);
        launch.Click += (_, _) => { ribbon.Merge(source); ribbon.EnterModal(preview); };
        var reference = new RibbonQuickAccessReference { Source = action }; ribbon.QuickAccessItems.Add(reference);
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Application-owned preview/document area. File and minimize are blocked in modal mode. QAT stays available. Escape dismisses child surfaces before the modal tab." };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        int actions = 0, entries = 0, exits = 0, unavailable = 0; bool cancelEntry = true, cancelExit = false;
        action.Click += (_, _) => actions++; ribbon.ModalEntering += (_, e) => e.Cancel = cancelEntry; ribbon.ModalEntered += (_, _) => entries++; ribbon.ModalExiting += (_, e) => e.Cancel = cancelExit; ribbon.ModalExited += (_, _) => exits++;
        var log = new List<string>(); Exception? error = null; bool completed = false; var cursor = WF.Cursor.Position;
        EventHandler<FirstChanceExceptionEventArgs> exceptions = (_, e) => { if (e.Exception is COMException { HResult: unchecked((int)0x80040201) }) unavailable++; };
        AppDomain.CurrentDomain.FirstChanceException += exceptions;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle);
                for (int i = 0; i < 20 && GetForegroundWindow() != form.Handle; i++) await Task.Delay(50);
                Guard(); document.Focus(); ribbon.Merge(source); Check(!ribbon.EnterModal(preview) && ribbon.SelectedTab == home && entries == 0, "Cancelled entry leaves normal presentation and selection intact");
                cancelEntry = false; await Key("{F10}hp"); ribbon.Refresh();
                Check(ribbon.IsModal && ribbon.ModalTab == preview && ribbon.GetPresentedTabs().SequenceEqual(new[] { preview }) && entries == 1, "KeyTips enter one exclusive modal tab");
                Check(home.Visible && other.Visible && ribbon.FileMenu.Visible, "Modal presentation preserves authored visibility");
                ribbon.IsMinimized = true; ribbon.IsBackstageOpen = true; ribbon.FileMenu.ShowDropDown(); await Key("^{F1}");
                Check(!ribbon.IsMinimized && !ribbon.IsBackstageOpen && !ribbon.FileMenu.IsDropDownOpen, "Programmatic requests and Ctrl+F1 cannot open File or minimize in modal mode");
                await Capture("modal-header"); ribbon.Refresh(); await Click(ribbon, record.Qat[reference]); Check(actions == 1 && ribbon.IsModal, "QAT executes an original command whose authored tab is outside modal presentation");
                menu.ShowDropDown(); await Task.Delay(80); Check(menu.IsDropDownOpen, "Modal content keeps ordinary child popup behavior"); await Key("{ESC}"); Check(!menu.IsDropDownOpen && ribbon.IsModal, "First Escape dismisses only the child popup");
                copies.PerformClick(); var editor = Tree(ribbon).OfType<WF.TextBox>().Single(c => c.Name == copies.Id); nint handle = editor.Handle;
                await Key("^a{BACKSPACE}"); Check(!ribbon.ExitModal() && ribbon.IsModal && editor.Focused, "Invalid native input blocks ordinary modal exit");
                await Key("1{ENTER}"); cancelExit = true; ribbon.Refresh(); await Click(ribbon, record.Close); Check(ribbon.IsModal && exits == 0, "Cancelling the close button keeps the modal tab active");
                cancelExit = false; await Key("{F10}x");
                Check(!ribbon.IsModal && ribbon.SelectedTab == home && document.Focused && exits == 1, "Close KeyTip restores prior selection and document focus");
                ribbon.Unmerge(source); ribbon.IsMinimized = true; document.Focus(); ribbon.Merge(source); Check(ribbon.EnterModal(preview), "Application entry works from a minimized ribbon");
                Check(!ribbon.IsMinimized && ribbon.SelectedTab == preview, "Entry expands the modal content");
                preview.CanClose = false; await Key("{ESC}"); Check(ribbon.IsModal, "CanClose false blocks user Escape"); preview.CanClose = true;
                document.Focus(); await Key("{ESC}"); Check(!ribbon.IsModal && ribbon.IsMinimized && document.Focused, "Escape from application content restores the prior minimized state and focus");
                ribbon.IsMinimized = false; document.Focus(); ribbon.EnterModal(preview); copies.PerformClick(); Check(editor.Handle == handle, "Modal cycles reuse the same native editor HWND");
                using var added = new RibbonMergeSource(); var later = new RibbonTab { Text = "Later context" }; added.Tabs.Add(later); ribbon.Merge(added); home.Visible = false;
                Check(ribbon.GetPresentedTabs().Single() == preview && later.Visible, "Merging and authored visibility changes during modal do not expose other tabs");
                cancelExit = true; ribbon.Unmerge(source); Check(!ribbon.IsModal && ribbon.SelectedTab == other && ribbon.GetPresentedTabs().Contains(later) && !home.Visible, "Unmerging the active modal tab forces exit and retains new context and visibility edits");
                await Capture("restored-context");
                Check(unavailable == 0, "Modal entry, native input, KeyTips and forced teardown raise zero unavailable-element exceptions");
                log.Add($"PASS: {(flat ? "Neutral" : "Office")} modal consumer at {form.DeviceDpi} DPI"); completed = true;
            }
            catch (Exception e) { error = e; log.Add("FAIL: " + e); try { await Capture("failure"); } catch { } }
            finally { WF.Cursor.Position = cursor; form.Close(); }
        };
        try { WF.Application.Run(form); } finally { AppDomain.CurrentDomain.FirstChanceException -= exceptions; }
        File.WriteAllLines(Path.Combine(directory, "result.log"), log);
        if (error is not null) Console.Error.WriteLine(error); else Console.WriteLine(string.Join(Environment.NewLine, log));
        return completed && error is null ? 0 : 1;
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        void Guard() { if (GetForegroundWindow() != form.Handle && WF.Control.FromHandle(GetForegroundWindow()) is not WF.ToolStripDropDown) throw new InvalidOperationException("Validation Form lost foreground focus."); }
        async Task Key(string keys) { Guard(); WF.SendKeys.SendWait(keys); await Task.Delay(110); }
        async Task Click(WF.Control c, Rectangle bounds)
        {
            Guard(); bounds = Rectangle.Intersect(c.ClientRectangle, bounds); Check(!bounds.IsEmpty, "Pointer target is visible");
            WF.Cursor.Position = c.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(60); Guard();
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(110);
        }
        async Task Capture(string name)
        {
            await Task.Delay(60); Guard(); var bounds = form.Bounds;
            using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap); graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private static IEnumerable<WF.Control> Tree(WF.Control c) => c.Controls.Cast<WF.Control>().SelectMany(child => new[] { child }.Concat(Tree(child)));
    private sealed class Recording { internal Rectangle Close; internal readonly Dictionary<RibbonElement, Rectangle> Qat = []; }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawModalClose(RibbonPaintContext c, string text) { r.Close = c.Bounds; base.DrawModalClose(c, text); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Qat[entry] = c.Bounds; base.DrawQuickAccessItem(c, entry, menu); }
    }
    private sealed class Flat(Recording r) : RibbonRenderer
    {
        public override void DrawModalClose(RibbonPaintContext c, string text) { r.Close = c.Bounds; base.DrawModalClose(c, text); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Qat[entry] = c.Bounds; base.DrawQuickAccessItem(c, entry, menu); }
    }
}
