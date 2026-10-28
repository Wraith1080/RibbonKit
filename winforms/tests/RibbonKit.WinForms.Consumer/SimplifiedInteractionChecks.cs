using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// This fixture also compiles outside the checkout against the public package.
internal static class SimplifiedInteractionChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private delegate bool WindowVisitor(nint window, nint state);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, WindowVisitor visit, nint state);
    internal static int Run(string directory, bool neutral)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); List<string> log = []; Exception? failure = null; var record = new Recording();
        using RibbonRenderer renderer = neutral ? new NeutralRecorder(record) : new OfficeRecorder(record);
        using var host = new WF.Form { Text = "Independent Simplified Ribbon", Width = 1300, Height = 720, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer, CustomizationApplicationId = "consumer.simple" };
        var home = new RibbonTab { Id = "home", Text = "Home", KeyTip = "H" }; var insert = new RibbonTab { Id = "insert", Text = "Insert" };
        var clipboard = new RibbonGroup { Id = "clipboard", Text = "Clipboard" }; var font = new RibbonGroup { Id = "font", Text = "Font" };
        var paste = new RibbonSplitButtonItem { Id = "paste", Text = "Paste", Icon = new SampleIcon(), Layout = RibbonSplitButtonLayout.Vertical, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedPriority = 10 };
        paste.Items.Add(new RibbonButtonItem { Text = "Paste text" });
        var copy = new RibbonButtonItem { Id = "copy", Text = "Copy selection", Icon = new SampleIcon(), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var bold = new RibbonToggleItem { Id = "bold", Text = "Bold", Icon = new SampleIcon(), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedSize = RibbonItemSize.Small };
        var combo = new RibbonComboBoxItem { Id = "zoom", Text = "Zoom", InputWidth = 120, IsEditable = true, Value = "100", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        combo.Items.AddRange("100", "150"); clipboard.Items.AddRange(paste, copy); font.Items.AddRange(bold, combo); home.Groups.AddRange(clipboard, font);
        insert.Groups.Add(new RibbonGroup { Id = "illustrations", Text = "Illustrations" }); ribbon.Tabs.AddRange(home, insert);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = bold }); ribbon.CommandCatalog.CaptureDefaults();
        host.Controls.Add(new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Independent public API consumer; no Showcase resources or helpers." }); host.Controls.Add(ribbon);
        int calls = 0; copy.Click += (_, _) => calls++;
        host.Shown += async (_, _) =>
        {
            try
            {
                await Activate(host); ribbon.Refresh(); int classic = ribbon.Height;
                var native = Descendants(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == combo.Id); nint handle = native.Handle;
                ribbon.LayoutMode = RibbonLayoutMode.Simplified; ribbon.Refresh();
                Check(ribbon.Height < classic && native.Handle == handle && native.Visible, "Simplified row retains the original native editor and reduces the body height");
                Check(record.Primary[paste].Right == record.Arrows[paste].Left && record.Primary[paste].Top == record.Arrows[paste].Top, "Vertical Classic split projects horizontally in Simplified mode");
                await Capture(host, "compact-row");
                foreach (var density in Enum.GetValues<RibbonDensity>())
                {
                    ribbon.Density = density; host.Width = 230; await Task.Delay(60); record.Entries.Clear(); ribbon.Refresh();
                    await Click(ribbon, record.More); var more = Popup(); more.Refresh();
                    Check(record.Entries.ContainsKey(copy), "Narrow " + density + " exposes displaced original commands in More Commands");
                    await CapturePopup(more, density + "-more"); await Click(more, record.Entries[copy]);
                    Check(calls == (density == RibbonDensity.Compact ? 1 : 2), density + " displaced action invokes its original handler once");
                    ribbon.Refresh(); await Click(ribbon, record.More); more = Popup(); more.Refresh(); await Click(more, record.Entries[font]); var full = Popup();
                    Check(native.Visible && native.Handle == handle && Descendants(full).Contains(native) && ReferenceEquals(combo.Parent, font), density + " full-group flyout borrows the same native editor without changing model ownership");
                    await CapturePopup(full, density + "-group");
                    ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Width = 1300; await Activate(host); ribbon.Refresh();
                    Check(native.Handle == handle && Descendants(ribbon).Contains(native), "Returning to Classic closes popups and restores native editor presentation");
                    ribbon.LayoutMode = RibbonLayoutMode.Simplified; await Capture(host, density + "-wide");
                }
                ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; var message = new RibbonMessage { Title = "Notice", Text = "Application-owned message under the Simplified body." }; ribbon.MessageBar.Items.Add(message);
                await Capture(host, "touch-qat-message"); ribbon.IsMinimized = true; ribbon.Refresh(); await Click(ribbon, record.Tabs[home]); var body = Popup(); body.Refresh();
                Check(record.Primary[paste].Height < (int)Math.Ceiling(60 * ribbon.DeviceDpi / 96d), "Minimized tab opens the same single-row Simplified geometry");
                await CapturePopup(body, "touch-minimized"); ribbon.LayoutMode = RibbonLayoutMode.Classic; ribbon.IsMinimized = false; message.Dismiss(); ribbon.LayoutMode = RibbonLayoutMode.Simplified;
                foreach (var density in Enum.GetValues<RibbonDensity>())
                {
                    ribbon.Density = density;
                    using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "consumer.simple"); dialog.SelectedPage = dialog.Pages.Single(p => p.Name == "CustomizeSimplifiedOptionsPage"); dialog.Show(host); await Activate(dialog);
                    var page = dialog.SimplifiedEditor; var source = Child<WF.ComboBox>(page, "SimplifiedSourceTab"); var target = Child<WF.ComboBox>(page, "SimplifiedTargetTab");
                    var left = Child<WF.ListBox>(page, "SimplifiedAvailableCommands"); var right = Child<WF.ListBox>(page, "SimplifiedSelectedCommands");
                    Check(dialog.Pages.Count(p => p.Text == "Customize Ribbon") == 1 && source != target && left.ItemHeight == right.ItemHeight, density + " has one active Customize Ribbon page with independent selectors and uniform grouped rows");
                    Check(page.Controls.Find("ApplyCustomization", true).Length == 0, "Simplified page uses only the shared Options Apply button");
                    int gap = (int)Math.Ceiling(8 * page.DeviceDpi / 96d); var searchFrame = Child<WF.TextBox>(page, "SimplifiedAvailableSearch").Parent!;
                    Check(source.Bottom + gap == searchFrame.Top && searchFrame.Bottom + gap == left.Parent!.Top, density + " reserves native selector height and equal gaps around the search field; selector=" + source.Bounds + "; search=" + searchFrame.Bounds + "; list=" + left.Parent!.Bounds + "; gap=" + gap + "; item=" + source.ItemHeight + "; preferred=" + source.GetPreferredSize(Size.Empty) + "; type=" + source.GetType());
                    source.SelectedIndex = 0; Check(target.SelectedIndex == 0, "Changing available-command scope preserves the independent target tab");
                    var custom = new RibbonCustomizationTab { Label = "My tab", Groups = [new() { Label = "My group", Commands = [new() { CommandId = "copy" }] }] };
                    page.Session.Draft.Ribbon!.Tabs.Add(custom); page.Session.Draft.Simplified!.Tabs.Add(new() { TabId = custom.Id, Commands = [new() { CommandId = "copy" }] }); page.RefreshDraft();
                    Child<WF.Button>(dialog, "ApplyCustomization").PerformClick(); Check(ribbon.Tabs.Any(t => t.Id == custom.Id), "Shared footer Apply commits the structural draft and Simplified pins together");
                    File.WriteAllText(Path.Combine(directory, density + "-profile.json"), page.Session.ExportJson());
                    await Capture(dialog, density + "-options"); Child<WF.Button>(page, "SimplifiedNew").PerformClick(); await Task.Delay(60);
                    WF.ToolStripDropDown? menu = null; EnumThreadWindows(GetCurrentThreadId(), (window, _) => { if (WF.Control.FromHandle(window) is WF.ToolStripDropDown { Visible: true } candidate) menu = candidate; return true; }, 0);
                    Check(menu is not null, "New opens the Options-owned themed action menu"); await CapturePopup(menu!, density + "-new-menu"); menu!.Close(); await Activate(dialog);
                    page.Session.Draft.Ribbon.Tabs.Add(new() { Label = "Pending" }); ribbon.LayoutMode = RibbonLayoutMode.Classic; Check(!page.ApplyChanges(), "Inactive Simplified page cannot apply while Classic is active");
                    Check(ribbon.Tabs.Any(t => t.Id == custom.Id), "Classic mode sees the same applied tab structure"); ribbon.LayoutMode = RibbonLayoutMode.Simplified; Check(page.Session.Draft.Ribbon.Tabs.Any(t => t.Label == "Pending"), "Mode switches preserve the pending Simplified draft");
                    Child<WF.Button>(dialog, "CancelCustomization").PerformClick(); Check(!ribbon.Tabs.Any(t => t.Text == "Pending"), "Cancel discards unapplied edits after the last Apply"); await Activate(host);
                }
                log.Add($"PASS: native DPI={ribbon.DeviceDpi}; renderer={(neutral ? "neutral" : "Office2024")}; focused Simplified checks only");
            }
            catch (Exception error) { failure = error; log.Add(error.ToString()); }
            finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); host.Close(); }
        };
        WF.Application.Run(host); return failure is null ? 0 : 1;
        void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException(text); log.Add("PASS: " + text); }
        async Task Activate(WF.Control control)
        {
            for (int attempt = 0; attempt < 4 && GetForegroundWindow() != control.Handle; attempt++) { ((WF.Form)control).Activate(); SetForegroundWindow(control.Handle); await Task.Delay(150); }
            Check(GetForegroundWindow() == control.Handle, "Native input/capture target has foreground");
        }
        async Task Click(WF.Control control, Rectangle bounds)
        {
            if (bounds.IsEmpty) throw new InvalidOperationException("Missing recorded public paint target");
            nint point = (nint)(((bounds.Top + bounds.Height / 2) << 16) | (bounds.Left + bounds.Width / 2));
            SendMessage(control.Handle, 0x201, 1, point); SendMessage(control.Handle, 0x202, 0, point); await Task.Delay(80);
        }
        async Task Capture(WF.Control control, string name) { await Task.Delay(60); control.Refresh(); var bounds = control.RectangleToScreen(control.ClientRectangle); Save(bounds, name); }
        async Task CapturePopup(WF.Control control, string name) { await Task.Delay(60); control.Refresh(); Save(control.RectangleToScreen(control.ClientRectangle), name); }
        void Save(Rectangle bounds, string name) { using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap); graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png")); }
    }
    private static T Child<T>(WF.Control root, string name) where T : WF.Control => (T)root.Controls.Find(name, true).Single();
    private static IEnumerable<WF.Control> Descendants(WF.Control control) => control.Controls.Cast<WF.Control>().SelectMany(c => Descendants(c).Prepend(c));
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected a native ribbon popup");
    private sealed class Recording
    {
        internal Rectangle More; internal readonly Dictionary<RibbonElement, Rectangle> Entries = [];
        internal readonly Dictionary<RibbonTab, Rectangle> Tabs = []; internal readonly Dictionary<RibbonSplitButtonItem, Rectangle> Primary = [], Arrows = [];
        internal void Entry(RibbonPaintContext context, RibbonElement entry, bool menu)
        {
            if (!menu) return;
            RibbonElement? source = entry switch { RibbonQuickAccessGroupReference r => r.Source, RibbonQuickAccessReference r => r.Source, RibbonQuickAccessDropDownReference r => r.Source, RibbonQuickAccessComboBoxReference r => r.Source, _ => null };
            if (source is not null) Entries[source] = context.Bounds;
        }
    }
    private sealed class OfficeRecorder(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.More = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Entry(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tabs[tab] = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawSplitItem(RibbonPaintContext p, RibbonPaintContext d, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { r.Primary[item] = p.Bounds; r.Arrows[item] = d.Bounds; base.DrawSplitItem(p, d, item, size, menu); }
    }
    private sealed class NeutralRecorder(Recording r) : RibbonRenderer
    {
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.More = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Entry(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tabs[tab] = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawSplitItem(RibbonPaintContext p, RibbonPaintContext d, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { r.Primary[item] = p.Bounds; r.Arrows[item] = d.Bounds; base.DrawSplitItem(p, d, item, size, menu); }
    }
    private sealed class SampleIcon : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { using var pen = new Pen(context.Foreground, Math.Max(1, context.DeviceDpi / 96f)); bounds.Inflate(-bounds.Width / 5, -bounds.Height / 6); graphics.DrawRectangle(pen, bounds); graphics.DrawLine(pen, bounds.Left, bounds.Top + bounds.Height / 2, bounds.Right, bounds.Top + bounds.Height / 2); }
    }
}
