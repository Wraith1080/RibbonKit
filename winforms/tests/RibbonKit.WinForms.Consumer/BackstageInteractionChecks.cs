using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public APIs only. Linked into the native project runner and copied with the package consumer.
internal static class BackstageInteractionChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint parameter, out bool value, uint flags);
    internal static int Run(string directory, bool flat = false, bool widthChecks = false, bool motionChecks = false)
    {
        directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        var record = new Recording(); using RibbonRenderer renderer = flat ? new Flat(record) : new Office(record);
        using var form = new WF.Form { Text = "Independent native Backstage consumer", Width = 970, Height = 590, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer, FileMenuMode = RibbonFileMenuMode.Backstage };
        var home = new RibbonTab { Text = "Home", KeyTip = "H" }; var group = new RibbonGroup { Text = "Commands" };
        var menu = new RibbonDropDownItem { Text = "Menu", KeyTip = "M" }; menu.Items.Add(new RibbonButtonItem { Text = "Nested command" });
        var input = new RibbonTextBoxItem { Text = "Name", Value = "Ready", KeyTip = "D" };
        input.ValueValidating += (_, e) => e.Cancel = input.Value.Length == 0;
        group.Items.AddRange(menu, input); home.Groups.Add(group); ribbon.Tabs.Add(home);
        ribbon.FileMenu.Visible = true;
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Application document. File opens the library-owned Backstage over this ordinary Form." };
        int documentActions = 0;
        var documentAction = new WF.Button { Text = "Document action", Location = new(30, 250) };
        documentAction.Click += (_, _) => documentActions++;
        form.Controls.Add(document); form.Controls.Add(documentAction); form.Controls.Add(ribbon); form.AcceptButton = documentAction;
        using var info = Page("Document information", "Native Controls retain values and bindings between pages.");
        using var print = Page("Print", "Application-owned settings; no printer or document service is called.");
        var field = new WF.TextBox { Location = new(30, 140), Width = 280, TabIndex = 0 };
        info.Controls.Add(new WF.Label { Text = "Document title", AutoSize = true, Location = new(30, 110) }); info.Controls.Add(field);
        var notes = new WF.TextBox { Multiline = true, Location = new(30, 210), Size = new(370, 100), Text = "Retained notes", TabIndex = 1 }; info.Controls.Add(notes);
        var copies = new WF.NumericUpDown { Minimum = 1, Maximum = 99, Location = new(30, 140), TabIndex = 0 };
        var paper = new WF.ComboBox { DropDownStyle = WF.ComboBoxStyle.DropDownList, Location = new(30, 210), Width = 210, TabIndex = 1 }; paper.Items.AddRange(["A4", "Letter"]); paper.SelectedIndex = 0;
        print.Controls.Add(new WF.Label { Text = "Copies / paper", AutoSize = true, Location = new(30, 110) }); print.Controls.AddRange([copies, paper]);
        using var source = new WF.BindingSource(); var state = new State(); source.DataSource = state;
        field.DataBindings.Add(new WF.Binding(nameof(field.Text), source, nameof(state.Name), false, WF.DataSourceUpdateMode.OnPropertyChanged));
        field.Validating += (_, e) => e.Cancel = field.Text.Length == 0;
        var a = new RibbonBackstagePageItem { Text = "Info", KeyTip = "I", Content = info };
        var b = new RibbonBackstagePageItem { Text = "Print", KeyTip = "P", Content = print };
        var save = new RibbonButtonItem { Text = "Save", KeyTip = "S" }; int calls = 0, closes = 0, unavailable = 0;
        save.Click += (_, _) => calls++; ribbon.FileMenu.DropDownClosed += (_, _) => closes++;
        ribbon.Backstage.Items.AddRange(a, new RibbonButtonItem { Text = "New", KeyTip = "N" }, save, b,
            new RibbonButtonItem { Text = "Unavailable", Enabled = false });
        bool cancel = true; ribbon.FileMenu.DropDownOpening += (_, e) => e.Cancel = cancel;
        var log = new List<string>(); Exception? error = null; bool completed = false; var cursor = WF.Cursor.Position;
        EventHandler<FirstChanceExceptionEventArgs> exceptions = (_, e) => { if (e.Exception is COMException { HResult: unchecked((int)0x80040201) }) unavailable++; };
        AppDomain.CurrentDomain.FirstChanceException += exceptions;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle);
                for (int i = 0; i < 20 && GetForegroundWindow() != form.Handle; i++) await Task.Delay(50);
                Guard(); document.Focus(); ribbon.Refresh();
                if (motionChecks) { cancel = false; await CheckMotion(); completed = true; return; }
                ribbon.FileMenu.ShowDropDown(); Check(!ribbon.IsBackstageOpen && closes == 0, "File cancellation prevents Backstage without a close event");
                cancel = false; input.PerformClick(); var nativeInput = Tree(ribbon).OfType<WF.TextBox>().Single(); nativeInput.Text = "";
                ribbon.FileMenu.ShowDropDown(); Check(!ribbon.IsBackstageOpen && nativeInput.Focused, "Pending ribbon editor validation blocks File");
                nativeInput.Text = "Accepted"; await Key("{ENTER}"); document.Focus();
                await Key("{F10}f"); Check(ribbon.IsBackstageOpen && ribbon.FileMenu.IsDropDownOpen && ribbon.SelectedTab == home, "File KeyTip routes to Backstage without changing the ribbon tab");
                var overlay = info.Parent!.Parent!.Parent!; Check(overlay.Parent == form && overlay.Bounds == form.ClientRectangle, "Library overlay covers the ordinary Form client area");
                await Capture("info");
                Check(record.NavigationPoints >= Math.Max(ribbon.Font.SizeInPoints, ribbon.Theme.BackstageNavigationFontSize * 72 / 96),
                    "Navigation uses the WPF-sized font minimum through the shared renderer context");
                if (widthChecks) await CheckWidths(overlay);
                await Key("{F10}");
                var backButton = Button("Back");
                WF.Cursor.Position = backButton.PointToScreen(new(backButton.Width / 2, backButton.Height / 2));
                await Task.Delay(90); await Capture("back-hover");
                Guard(); mouse_event(2, 0, 0, 0, 0);
                try { await Task.Delay(90); await Capture("back-pressed"); }
                finally
                {
                    WF.Cursor.Position = field.PointToScreen(new(5, 5));
                    mouse_event(4, 0, 0, 0, 0);
                }
                Check(ribbon.IsBackstageOpen, "Releasing a Back press outside the button cancels activation");
                await Key("{F6}"); Check(field.Focused, "F6 enters native page content");
                await Key("{ENTER}"); Check(documentActions == 0 && ribbon.IsBackstageOpen, "Page Enter does not activate the covered Form default button");
                await Key("^aRetained title"); Check(state.Name == "Retained title", "Native page edits update the application binding");
                field.Select(2, 4); var parent = info.Parent; nint handle = field.Handle;
                await Click(Button("Print")); Check(ribbon.Backstage.SelectedPage == b && !info.Visible && print.Visible, "Pointer selects the second native page");
                copies.Value = 3; paper.Focus(); await Key("{F4}"); Check(paper.DroppedDown, "Page ComboBox opens its native choice list");
                await Key("{ESC}"); Check(ribbon.IsBackstageOpen && !paper.DroppedDown, "Escape closes a native page choice list before Backstage");
                await Capture("print"); await Key("{F10}i");
                Check(ribbon.Backstage.SelectedPage == a && field.Handle == handle && info.Parent == parent && field.SelectionStart == 2 && field.SelectionLength == 4, "Page switches retain the live HWND, parent, text selection and bindings");
                field.Focus(); await Key("^a{BACKSPACE}"); await Key("{ESC}"); Check(ribbon.IsBackstageOpen, "Invalid page content blocks Escape close");
                await Click(Button("Save")); Check(ribbon.IsBackstageOpen && calls == 0, "Invalid page content blocks commands after navigation focus moves");
                field.Focus(); await Key("^aAccepted title");
                for (int i = 0; i < 13; i++) { await Key("{TAB}"); Check(overlay.ContainsFocus && !document.Focused, "Tab remains inside Backstage"); }
                await Key("+{TAB}"); Check(overlay.ContainsFocus, "Shift+Tab remains inside Backstage");
                ribbon.IsMinimized = true; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
                form.Width = 480; form.Height = 360; await Task.Delay(120);
                Check(ribbon.IsBackstageOpen && overlay.Bounds == form.ClientRectangle && info.Parent == parent && field.Handle == handle, "Resize and minimized/below-QAT changes preserve the overlay and native handles");
                await Capture("narrow"); await Click(Button("Back"));
                Check(!ribbon.IsBackstageOpen && document.Focused && info.Parent == parent, "Back restores document focus while retaining parked content");
                form.Width = 970; form.Height = 590; ribbon.IsMinimized = false; await Task.Delay(100);
                menu.ShowDropDown(); Check(menu.IsDropDownOpen, "Existing ribbon menu opens normally");
                ribbon.IsBackstageOpen = true; Check(ribbon.IsBackstageOpen && !menu.IsDropDownOpen, "Backstage closes an existing library popup");
                menu.ShowDropDown(); Check(!menu.IsDropDownOpen, "Underlying library popups cannot overlap Backstage");
                await Key("{F10}s"); Check(!ribbon.IsBackstageOpen && calls == 1, "Command KeyTip closes Backstage and executes once");
                await Click(ribbon, record.File); Check(ribbon.IsBackstageOpen && field.Text == "Accepted title" && field.Handle == handle && copies.Value == 3, "Pointer reopen preserves both native pages");
                ribbon.FileMenu.Enabled = false; Check(!ribbon.IsBackstageOpen, "Disabling File forces surface teardown");
                ribbon.FileMenu.Enabled = true; ribbon.IsBackstageOpen = true;
                ribbon.Backstage.Items.Remove(a); Check(info.Parent is null && !info.IsDisposed, "Removing a page detaches borrowed content without disposing it");
                ribbon.Dispose(); Check(print.Parent is null && !print.IsDisposed && !field.IsDisposed, "Ribbon disposal releases remaining borrowed pages without disposing application content");
                Check(unavailable == 0, "Backstage interactions raise zero unavailable-element exceptions");
                log.Add($"PASS: {(flat ? "Neutral" : "Office")} Backstage at {form.DeviceDpi} DPI"); completed = true;
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
        async Task Key(string keys) { Guard(); WF.SendKeys.SendWait(keys); await Task.Delay(90); }
        WF.Button Button(string text) => Tree(form).OfType<WF.Button>().Single(c => c.Text == text && c.Visible);
        async Task Click(WF.Control control, Rectangle? bounds = null)
        {
            Guard(); var r = Rectangle.Intersect(control.ClientRectangle, bounds ?? control.ClientRectangle);
            Check(!r.IsEmpty && control.Visible, "Pointer target is visible");
            WF.Cursor.Position = control.PointToScreen(new(r.Left + r.Width / 2, r.Top + r.Height / 2)); await Task.Delay(40); Guard();
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(100);
        }
        async Task Capture(string name)
        {
            await Task.Delay(60); Guard(); using var bitmap = new Bitmap(form.Width, form.Height); using var g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(form.Location, Point.Empty, bitmap.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
        async Task CheckMotion()
        {
            if (!SystemParametersInfo(0x1042, 0, out bool enabled, 0) || !enabled) { log.Add("SKIP: OS client-area animations disabled; native playback was not forced."); return; }
            await Task.Delay(250); nint handle = 0; WF.Control? parent = null;
            foreach (var level in new[] { RibbonAnimationLevel.Subtle, RibbonAnimationLevel.Expressive })
            {
                ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.Density = level == RibbonAnimationLevel.Subtle ? RibbonDensity.Compact : RibbonDensity.Touch; ribbon.IsMinimized = level == RibbonAnimationLevel.Expressive; ribbon.AnimationLevel = level; ribbon.Refresh();
                await QuickClick(ribbon, record.File); Check(ribbon.IsBackstageOpen && ribbon.IsAnimating, level + " File input starts navigation opening");
                var overlay = info.Parent!.Parent!.Parent!; if (handle == 0) { handle = field.Handle; parent = info.Parent; }
                Check(info.Visible && field.Handle == handle && ReferenceEquals(parent, info.Parent) && state.Name == field.Text, level + " application page is live with retained HWND/parent/binding");
                var page = info.RectangleToScreen(info.ClientRectangle); var client = overlay.RectangleToScreen(overlay.ClientRectangle); int navigation = info.Parent!.Parent!.Left;
                await Task.Delay(20); using var first = Frame(client); using var livePage = Frame(page); var deadline = Stopwatch.StartNew();
                await Task.Delay(30); using (var middle = Frame(client)) middle.Save(Path.Combine(directory, level + "-middle.png"));
                while (ribbon.IsAnimating && deadline.ElapsedMilliseconds < 1500) await Task.Delay(3);
                Check(!ribbon.IsAnimating && ribbon.IsBackstageOpen, level + " navigation completes without a host acknowledgement"); using var final = Frame(client); using var finalPage = Frame(page);
                Check(Different(first, final, new(0, 0, navigation, first.Height)), level + " navigation visibly changes during opening");
                Check(Different(livePage, finalPage, new(Point.Empty, livePage.Size)), level + " live application page visibly fades without moving its controls");
                Check(FadedText(livePage, finalPage), level + " intermediate page contains faded native text rather than a blank host");
                Check(field.Handle == handle && info.Parent == parent && ribbon.SelectedTab == home && ribbon.IsMinimized == (level == RibbonAnimationLevel.Expressive), level + " tab/minimized and native content state are retained");
                Check(GetForegroundWindow() == form.Handle, level + " inert drawing never activates"); first.Save(Path.Combine(directory, level + "-first.png")); final.Save(Path.Combine(directory, level + "-final.png"));
                await QuickClick(Button("Back")); Check(ribbon.IsBackstageOpen && ribbon.IsAnimating, "Back starts a validated closing fade");
                using (var closing = Frame(client)) closing.Save(Path.Combine(directory, level + "-closing-first.png")); await Task.Delay(30); using (var closing = Frame(client)) closing.Save(Path.Combine(directory, level + "-closing-middle.png")); await FinishMotion();
                Check(!ribbon.IsBackstageOpen && !ribbon.IsAnimating, "Closing completes autonomously"); using (var closed = Frame(client)) closed.Save(Path.Combine(directory, level + "-closed.png"));
            }
            ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; await QuickClick(ribbon, record.File); Check(ribbon.IsAnimating, "Page input begins during opening");
            await QuickClick(field); Check(!ribbon.IsAnimating && field.Focused, "Native field input settles navigation without losing focus"); await Key("^aUpdated title"); Check(state.Name == "Updated title" && field.Handle == handle, "Native text and binding update normally");
            await QuickClick(Button("Print")); Check(ribbon.Backstage.SelectedPage == b && print.Visible && info.Parent == parent && ribbon.IsAnimating, "Page selection fades and preserves the borrowed parent"); await Task.Delay(25); using (var switching = Frame(form.RectangleToScreen(form.ClientRectangle))) switching.Save(Path.Combine(directory, "page-switch.png")); await FinishMotion(); await Key("{ESC}"); await FinishMotion();
            ribbon.Backstage.SelectedPage = a; await QuickClick(ribbon, record.File); await QuickClick(field); await Key("^a{BACKSPACE}"); await Key("{ESC}"); Check(ribbon.IsBackstageOpen && !ribbon.IsAnimating, "Invalid native page still blocks closing");
            await Key("Accepted title"); await Key("{ESC}"); await FinishMotion(); Check(!ribbon.IsBackstageOpen && state.Name == "Accepted title", "A valid page closes normally");
            document.Focus(); WF.SendKeys.SendWait("{F10}f"); await Task.Delay(15); Check(ribbon.IsBackstageOpen && ribbon.IsAnimating, "File KeyTip starts the same opening path"); WF.SendKeys.SendWait("p"); await Task.Delay(25); Check(ribbon.Backstage.SelectedPage == b && ribbon.IsAnimating, "A Backstage KeyTip selects and fades its page during opening"); await FinishMotion(); await Key("{ESC}"); await FinishMotion();
            ribbon.Backstage.SelectedPage = a; await QuickClick(ribbon, record.File); Check(ribbon.IsAnimating, "Resize begins during opening"); form.Width -= 25; Check(!ribbon.IsAnimating && ribbon.IsBackstageOpen, "Resize settles while keeping Backstage open"); await Key("{ESC}"); await FinishMotion();
            await QuickClick(ribbon, record.File); await QuickClick(Button("Save")); Check(calls == 1 && !ribbon.IsBackstageOpen && !ribbon.IsAnimating, "Backstage command closes synchronously before application work");
            await QuickClick(ribbon, record.File); WF.SendKeys.SendWait("{ESC}"); Check(ribbon.IsAnimating, "Alt+F4 test begins during closing"); int hostCloses = 0; WF.FormClosingEventHandler cancelHost = (_, e) => { hostCloses++; e.Cancel = true; }; form.FormClosing += cancelHost;
            WF.SendKeys.SendWait("%{F4}"); await Task.Delay(60); Check(hostCloses == 1 && !ribbon.IsBackstageOpen && !ribbon.IsAnimating, "Alt+F4 during closing still reaches the cancellable host once"); form.FormClosing -= cancelHost;
            ribbon.AnimationLevel = RibbonAnimationLevel.None; await QuickClick(ribbon, record.File); Check(ribbon.IsBackstageOpen && !ribbon.IsAnimating, "None keeps Backstage immediate"); await Key("{ESC}");
            log.Add($"PASS: public Backstage navigation/page opening and closing motion, Subtle/Expressive, Compact/Touch, native input/validation/KeyTips/resize and retained live application controls; DPI={ribbon.DeviceDpi}; renderer={(flat ? "neutral" : "Office2024")}");
        }
        async Task FinishMotion() { var deadline = Stopwatch.StartNew(); while (ribbon.IsAnimating && deadline.ElapsedMilliseconds < 2000) await Task.Delay(3); Check(!ribbon.IsAnimating, "Library completes motion without host acknowledgement"); }
        async Task QuickClick(WF.Control control, Rectangle? bounds = null)
        {
            Guard(); var r = bounds ?? control.ClientRectangle; Check(!r.IsEmpty && control.Visible, "Motion pointer target is visible"); WF.Cursor.Position = control.PointToScreen(new(r.Left + r.Width / 2, r.Top + r.Height / 2)); await Task.Delay(20); mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(15);
        }
        static Bitmap Frame(Rectangle bounds) { var image = new Bitmap(bounds.Width, bounds.Height); using var g = Graphics.FromImage(image); g.CopyFromScreen(bounds.Location, Point.Empty, image.Size); return image; }
        static bool Different(Bitmap first, Bitmap last, Rectangle area) { for (int y = area.Top; y < area.Bottom; y++) for (int x = area.Left; x < area.Right; x++) if (first.GetPixel(x, y) != last.GetPixel(x, y)) return true; return false; }
        static bool FadedText(Bitmap first, Bitmap last) { for (int y = 0; y < Math.Min(90, first.Height); y++) for (int x = 0; x < Math.Min(600, first.Width); x++) { var end = last.GetPixel(x, y); var start = first.GetPixel(x, y); if (end.R < 30 && end.G < 30 && end.B < 30 && start.R is > 40 and < 235 && Math.Abs(start.R - start.G) < 3 && Math.Abs(start.B - start.G) < 3) return true; } return false; }
        async Task CheckWidths(WF.Control overlay)
        {
            int S(int value) => (int)Math.Round(value * form.DeviceDpi / 96d);
            var model = ribbon.Backstage;
            var pageArea = info.Parent!.Parent!;
            Check(model.NavigationWidth is null, "Navigation defaults to designer-authored Auto");
            await Key("{F10}");
            model.DataBindings.Add(new WF.Binding(nameof(model.NavigationWidth), source, nameof(state.Width), true, WF.DataSourceUpdateMode.OnPropertyChanged) { DataSourceNullValue = null });
            model.NavigationWidth = 224;
            Check(pageArea.Left == S(224) && state.Width == 224, "Authored logical width and binding share measured DPI geometry");
            await Capture("width-explicit");
            model.NavigationWidth = 400;
            Check(pageArea.Left == S(320) && model.NavigationWidth == 400, "Maximum bounds constrain display without rewriting the preferred width");
            form.Width = 480; await Task.Delay(100);
            Check(pageArea.Width >= overlay.Width / 2 && pageArea.Left < S(320) && model.NavigationWidth == 400, "Narrow hosts preserve page space and the authored preference");
            await Capture("width-narrow"); form.Width = 970; await Task.Delay(100);
            Check(pageArea.Left == S(320) && model.NavigationWidth == 400, "Widening restores the constrained preferred width");
            model.NavigationWidth = null; int automatic = pageArea.Left;
            a.Text = "Document information and publication settings";
            Check(pageArea.Left > automatic && pageArea.Left <= S(320), "Auto measures current visible captions and reserves the icon and padding slots");
            await Capture("width-auto-long"); a.Text = "Info";
            Check(pageArea.Left == automatic && model.NavigationWidth is null && state.Width is null, "Short captions and bound Auto restore the baseline width");
            var parent = info.Parent; nint handle = field.Handle;
            field.Focus(); field.Text = "";
            model.NavigationWidth = 240;
            Check(pageArea.Left == S(240) && state.Width == 240 && field.Focused && field.Text == "" && info.Parent == parent && field.Handle == handle, "Property changes preserve pending native edits, focus and HWNDs without validation");
            field.Text = "Untitled"; field.Select(2, 3);
            model.NavigationWidth = 224; ribbon.IsBackstageOpen = false; ribbon.IsBackstageOpen = true;
            Check(pageArea.Left == S(224) && info.Parent == parent && field.Handle == handle && field.SelectionStart == 2 && field.SelectionLength == 3, "Closing and reopening retains width, native parent, handle and selection");
            model.NavigationWidth = null;
            await Capture("width-auto");
            await Key("{F10}");
        }
    }
    private static WF.Panel Page(string title, string description)
    {
        var panel = new WF.Panel { AutoScroll = true, BackColor = Color.White, Size = new(500, 400) };
        var headingFont = new Font("Segoe UI", 22);
        panel.Disposed += (_, _) => headingFont.Dispose();
        panel.Controls.Add(new WF.Label { Text = title, AutoSize = true, Location = new(26, 24), Font = headingFont });
        panel.Controls.Add(new WF.Label { Text = description, AutoSize = true, Location = new(30, 80) });
        return panel;
    }
    private static IEnumerable<WF.Control> Tree(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Tree(c)));
    private sealed class State : INotifyPropertyChanged
    {
        private string _name = "Untitled";
        private int? _width;
        public int? Width { get => _width; set { if (_width == value) return; _width = value; PropertyChanged?.Invoke(this, new(nameof(Width))); } }
        public string Name { get => _name; set { if (_name == value) return; _name = value; PropertyChanged?.Invoke(this, new(nameof(Name))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Recording { internal Rectangle File; internal float NavigationPoints; }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawBackstageEntry(RibbonPaintContext c, RibbonItem? item, bool selected) { if (item is not null) r.NavigationPoints = c.Font.SizeInPoints; base.DrawBackstageEntry(c, item, selected); }
        public override void DrawFileButton(RibbonPaintContext c, RibbonDropDownItem menu) { r.File = c.Bounds; base.DrawFileButton(c, menu); }
    }
    private sealed class Flat(Recording r) : RibbonRenderer
    {
        public override void DrawBackstageEntry(RibbonPaintContext c, RibbonItem? item, bool selected) { if (item is not null) r.NavigationPoints = c.Font.SizeInPoints; base.DrawBackstageEntry(c, item, selected); }
        public override void DrawFileButton(RibbonPaintContext c, RibbonDropDownItem menu) { r.File = c.Bounds; base.DrawFileButton(c, menu); }
    }
}
