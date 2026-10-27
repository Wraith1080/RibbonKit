using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// This fixture is also copied out of the checkout for package-only verification.
// It uses public models, paint hooks, native Controls and guarded pointer/keys.
internal static class ScreenTipInteractionChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetFocus();
    [DllImport("user32.dll")] private static extern nint GetCapture();
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll")] private static extern nint GetTopWindow(nint window);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out NativeRect rect);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(nint window, System.Text.StringBuilder text, int count);
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { internal int Left, Top, Right, Bottom; public override string ToString() => $"{Left},{Top},{Right},{Bottom}"; }
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    internal static int Run(string directory, bool flat)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); var results = new List<string>(); Exception? error = null; bool completed = false;
        var exceptionLog = Path.Combine(directory, "unavailable-element.log"); int unavailableCount = 0;
        void OnFirstChance(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is COMException { HResult: unchecked((int)0x80040201) })
            {
                Interlocked.Increment(ref unavailableCount);
                lock (results) File.AppendAllText(exceptionLog, e.Exception + Environment.NewLine + Environment.StackTrace + Environment.NewLine);
            }
        }
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;
        using var form = new WF.Form { Text = "Public ScreenTip consumer", Width = 1080, Height = 460, StartPosition = WF.FormStartPosition.CenterScreen, AutoScaleMode = WF.AutoScaleMode.Dpi };
        var recording = new Recording();
        using RibbonRenderer renderer = flat ? new FlatRecorder(recording) : new OfficeRecorder(recording);
        var ribbon = new RibbonControl { Renderer = renderer }; var tab = new RibbonTab { Text = "Home" };
        var commands = new RibbonGroup { Text = "Commands" }; var editors = new RibbonGroup { Text = "Editors" }; var menus = new RibbonGroup { Text = "Menus" };
        var full = new RibbonButtonItem { Text = "Find matching text throughout this document without shortening the original command caption" };
        var split = new RibbonSplitButtonItem { Text = "Paste", ScreenTipTitle = "Paste contents", ScreenTipDescription = "Paste the clipboard contents or choose another format.", Layout = RibbonSplitButtonLayout.Vertical };
        split.Items.Add(new RibbonButtonItem { Text = "Text only" });
        var disabled = new RibbonButtonItem { Text = "Unavailable", Enabled = false, ScreenTipDescription = "Select a document before using this command.", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        var check = new RibbonCheckBoxItem { Text = "Ruler", ScreenTipDescription = "Show the ruler." };
        var radio = new RibbonRadioButtonItem { Text = "Print view", ScreenTipDescription = "Show printed pages." };
        var text = new RibbonTextBoxItem { Id = "tip.name", Text = "Name", Value = "Selected text is retained", InputWidth = 175, ScreenTipDescription = "Edit the document name without losing selection." };
        var combo = new RibbonComboBoxItem { Id = "tip.font", Text = "Font", InputWidth = 175, IsEditable = true, ScreenTipDescription = "Choose or type a font name." };
        combo.Items.AddRange(new[] { "Arial", "Calibri", "Segoe UI" }); combo.SelectedIndex = 0;
        var menu = new RibbonDropDownItem { Text = "More", ScreenTipDescription = "Open additional commands." };
        var row = new RibbonButtonItem { Text = "Popup command", ScreenTipDescription = "This description is owned by the shared library popup." };
        var popupText = new RibbonTextBoxItem { Id = "tip.popup", Text = "Popup name", Value = "Popup selection", ScreenTipDescription = "A native editor inside the command menu." };
        var nested = new RibbonDropDownItem { Text = "Nested" }; var nestedRow = new RibbonCheckBoxItem { Text = "Nested option", ScreenTipDescription = "Nested menu hover." };
        nested.Items.Add(nestedRow); menu.Items.AddRange(row, popupText, nested);
        commands.Items.AddRange(full, split, disabled, check, radio); editors.Items.AddRange(text, combo); menus.Items.Add(menu);
        tab.Groups.AddRange(commands, editors, menus); ribbon.Tabs.Add(tab);
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Application document retains keyboard focus." };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        int clicks = 0, rowClicks = 0; full.Click += (_, _) => clicks++; disabled.Click += (_, _) => clicks++;
        row.Click += (_, _) => rowClicks++;
        var cursor = WF.Cursor.Position;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(200);
                Guard(); document.Focus(); ribbon.Refresh();
                nint focus = GetFocus(), foreground = GetForegroundWindow(), capture = GetCapture();
                await Hover(ribbon, full, "full-caption");
                Check(recording.Title == full.Text && full.Text.Length > 80, "Full caption reaches the public renderer intact");
                Check(GetFocus() == focus && GetForegroundWindow() == foreground && GetCapture() == capture, "Tip does not activate, focus or capture");
                await Hover(ribbon, split, "authored-description");
                Check(recording.Title == split.ScreenTipTitle && recording.Description == split.ScreenTipDescription, "Authored title and description use the shared renderer hook");
                var splitTip = Tip(); Guard(); WF.Cursor.Position = ribbon.PointToScreen(Centre(recording.Arrows[split])); await Task.Delay(140);
                Check(ReferenceEquals(splitTip, Tip()), "Both split halves retain one passive tip");
                await Hover(ribbon, disabled, "disabled");
                Check(clicks == 0 && !recording.DisabledInteraction, "Disabled descriptions cause no interaction flags or execution");
                await Click(ribbon, recording.Bounds[disabled]); Check(Tip() is null && clicks == 0, "Disabled press cancels its tip and rejects activation");
                await Hover(ribbon, check, "checkbox"); await Hover(ribbon, radio, "radio");

                var nativeText = Descendants(ribbon).OfType<WF.TextBox>().Single(c => c.Name == text.Id);
                text.PerformClick(); nativeText.Select(3, 8); focus = GetFocus();
                await HoverBounds(nativeText, nativeText.ClientRectangle, "native-editor");
                Check(nativeText.Focused && GetFocus() == focus && nativeText.SelectionStart == 3 && nativeText.SelectionLength == 8 && text.Value == nativeText.Text,
                    "Native TextBox focus, selection and value survive a tip");
                await Key("{RIGHT}"); Check(Tip() is null, "Editor keyboard input dismisses the tip");
                await Task.Delay(720); Check(Tip() is null, "Stationary pointer does not reopen a keyboard-cancelled tip");
                var nativeCombo = Descendants(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == combo.Id);
                combo.PerformClick(); nativeCombo.Select(1, 2); focus = GetFocus();
                await HoverBounds(nativeCombo, nativeCombo.ClientRectangle, "combo-editor");
                Check(nativeCombo.Focused && GetFocus() == focus && nativeCombo.SelectionStart == 1 && nativeCombo.SelectionLength == 2, "Editable ComboBox retains selection and focus");
                combo.ShowDropDown(); await Task.Delay(150); Check(Tip() is null && combo.IsDropDownOpen, "Opening choices cancels the item tip");
                await Task.Delay(720); Check(Tip() is null, "Choice list suppresses underlying item tips");
                combo.CloseDropDown();

                menu.ShowDropDown(); await Task.Delay(120); var popup = Popup();
                focus = GetFocus(); foreground = GetForegroundWindow();
                await Hover(popup, row, "menu-row");
                Check(GetFocus() == focus && GetForegroundWindow() == foreground && menu.IsDropDownOpen, "Menu tip preserves the active popup session");
                await Click(popup, recording.Bounds[row]);
                Check(rowClicks == 1 && !menu.IsDropDownOpen && Tip() is null, "Clicking a popup command with its tip visible executes once and closes cleanly");
                menu.ShowDropDown(); await Task.Delay(120); popup = Popup();
                popupText.PerformClick(); var popupEditor = Descendants(popup).OfType<WF.TextBox>().Single(c => c.Name == popupText.Id); popupEditor.Select(1, 4);
                await HoverBounds(popupEditor, popupEditor.ClientRectangle, "popup-editor");
                Check(popupEditor.Focused && popupEditor.SelectionStart == 1 && popupEditor.SelectionLength == 4, "Popup native editor retains selection");
                nested.ShowDropDown(); await Task.Delay(100); var child = Popup();
                await Hover(child, nestedRow, "nested-row"); await Key("{ESC}");
                Check(Tip() is null && !nested.IsDropDownOpen && menu.IsDropDownOpen, "Escape closes one menu level and cancels its tip");
                await Key("{ESC}"); Check(!menu.IsDropDownOpen, "Second Escape closes the parent menu");

                await Move(ribbon, recording.Bounds[full]); await Task.Delay(180);
                await Move(ribbon, recording.Bounds[disabled]); await Task.Delay(250);
                Check(Tip() is null, "Moving to another item cancels the first delayed show");
                await Task.Delay(600); Check(recording.Title == disabled.Text && Tip() is not null, "Only the current delayed target is shown");
                disabled.ScreenTipDescription = "Updated metadata"; Check(Tip() is null, "Metadata changes dismiss stale content");
                await Move(ribbon, recording.Bounds[full]); await Task.Delay(180); await Key("{LEFT}");
                await Task.Delay(720); Check(Tip() is null, "Keyboard input cancels a pending delayed show");
                await Hover(ribbon, full, "before-resize"); form.Width -= 10; Check(Tip() is null, "Resize dismisses visible tips");
                await Hover(ribbon, full, "before-remove"); commands.Items.Remove(full); Check(Tip() is null, "Removing the target dismisses its tip");
                commands.Items.Add(full); ribbon.Refresh();
                await Hover(ribbon, full, "before-command"); full.PerformClick(); Check(Tip() is null && clicks == 1, "Programmatic command cancels the tip and executes exactly once");
                recording.Collapsed.Clear(); form.Width = 240; await Task.Delay(100); ribbon.Refresh();
                Check(recording.Collapsed.ContainsKey(commands), "Narrow layout exposes the collapsed command group");
                await Click(ribbon, recording.Collapsed[commands]); popup = Popup();
                await Hover(popup, full, "collapsed-group"); await Key("{ESC}");
                form.Width = 1080; await Task.Delay(100); ribbon.Refresh();
                await Hover(ribbon, disabled, "before-deactivation");
                using (var other = new WF.Form { Text = "Validation foreground switch", Width = 260, Height = 120 })
                {
                    other.Show(); other.Activate(); SetForegroundWindow(other.Handle); await Task.Delay(150);
                    Check(GetForegroundWindow() == other.Handle && Tip() is null, "Host deactivation dismisses the tip");
                    other.Close();
                }
                form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(150); Guard();
                var work = WF.Screen.FromControl(form).WorkingArea;
                form.Top = work.Bottom - ribbon.Height - 12; await Task.Delay(100); ribbon.Refresh();
                await Hover(ribbon, disabled, "work-area-edge");
                Check(work.Contains(Tip()!.Bounds), "Tip flips above its anchor and stays inside the working area");
                results.Add($"PASS {(flat ? "neutral" : "Office")} ScreenTips at {ribbon.DeviceDpi} DPI"); completed = true;
            }
            catch (Exception ex) { error = ex; results.Add(ex.ToString()); }
            finally
            {
                if (GetForegroundWindow() == form.Handle || WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown)
                    WF.Cursor.Position = cursor;
                form.Close();
            }
        };
        WF.Application.Run(form);
        AppDomain.CurrentDomain.FirstChanceException -= OnFirstChance;
        results.Add($"{(unavailableCount == 0 ? "PASS" : "FAIL")} Normal menu/input/teardown raised {unavailableCount} unavailable-element exceptions");
        if (unavailableCount != 0) completed = false;
        if (WF.Application.OpenForms.Cast<WF.Form>().Any(f => !f.IsDisposed && f.Owner == form)) { results.Add("FAIL Orphaned tip window"); completed = false; }
        File.WriteAllLines(Path.Combine(directory, "result.log"), results);
        return completed && error is null ? 0 : 1;

        WF.Form? Tip() => WF.Application.OpenForms.Cast<WF.Form>().FirstOrDefault(f => f.Owner == form && f.FormBorderStyle == WF.FormBorderStyle.None && f.Visible);
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); results.Add("PASS " + message); }
        void Guard() => Check(GetForegroundWindow() == form.Handle || WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown,
            "Validation window or its popup owns foreground before input");
        async Task Move(WF.Control surface, Rectangle bounds)
        {
            Guard(); WF.Cursor.Position = surface.PointToScreen(Centre(bounds)); await Task.Delay(40);
        }
        async Task Hover(WF.Control surface, RibbonItem item, string name) => await HoverBounds(surface, recording.Bounds[item], name);
        async Task HoverBounds(WF.Control surface, Rectangle bounds, string name)
        {
            await Move(document, document.ClientRectangle); await Move(surface, bounds);
            await Task.Delay(260); Guard(); Check(Tip() is null, "Hover delay prevents immediate appearance: " + name);
            using var beforeContent = name == "nested-row" ? new Bitmap(bounds.Width, bounds.Height) : null;
            if (beforeContent is not null)
                using (var graphics = Graphics.FromImage(beforeContent)) graphics.CopyFromScreen(surface.PointToScreen(bounds.Location), Point.Empty, beforeContent.Size);
            if (surface.TopLevelControl is WF.ToolStripDropDown beforePopup)
            {
                Stacking(name + "-before");
                using var beforeImage = new Bitmap(beforePopup.Width, beforePopup.Height);
                using (var graphics = Graphics.FromImage(beforeImage)) graphics.CopyFromScreen(beforePopup.Location, Point.Empty, beforeImage.Size);
                beforeImage.Save(Path.Combine(directory, name + "-before.png"));
            }
            await Task.Delay(600); Guard();
            if (Tip() is null)
                results.Add($"Hover diagnostic: {name}; pointer={WF.Cursor.Position}; surface={surface.Bounds}; visible={surface.Visible}; focus={GetFocus()}; last-title={recording.Title}");
            Check(Tip() is not null, "Delayed hover shows: " + name);
            var tip = Tip()!;
            var captureBounds = tip.Bounds;
            if (surface.TopLevelControl is WF.ToolStripDropDown popup)
            {
                nint below = GetWindow(tip.Handle, 2); // GW_HWNDNEXT: lower in top-level Z order
                while (below != 0 && below != popup.Handle) below = GetWindow(below, 2);
                Check(below == popup.Handle, "Tip stays above the library popup: " + name);
                var frameBounds = Rectangle.Inflate(popup.Bounds, (int)Math.Round(8 * ribbon.DeviceDpi / 96d), (int)Math.Round(8 * ribbon.DeviceDpi / 96d));
                foreach (var frame in Windows())
                {
                    var caption = new System.Text.StringBuilder(200); GetWindowText(frame, caption, caption.Capacity); GetWindowRect(frame, out var rect);
                    if (caption.ToString() != "Ribbon popup frame and shadow" || rect.Left != frameBounds.Left || rect.Top != frameBounds.Top || rect.Right != frameBounds.Right || rect.Bottom != frameBounds.Bottom) continue;
                    Check(GetWindow(frame, 4) == GetWindow(popup.Handle, 4), "Decorative frame shares the popup ownership level: " + name);
                    nint next = GetWindow(popup.Handle, 2);
                    while (next != 0 && next != frame) next = GetWindow(next, 2);
                    Check(next == frame, "Decorative frame remains below popup content: " + name);
                }
                Stacking(name + "-after");
                captureBounds = Rectangle.Union(captureBounds, popup.Bounds);
            }
            if (beforeContent is not null)
            {
                using var afterContent = new Bitmap(beforeContent.Width, beforeContent.Height);
                using (var graphics = Graphics.FromImage(afterContent)) graphics.CopyFromScreen(surface.PointToScreen(bounds.Location), Point.Empty, afterContent.Size);
                bool same = true;
                for (int y = 0; y < beforeContent.Height; y++) for (int x = 0; x < beforeContent.Width; x++) same &= beforeContent.GetPixel(x, y) == afterContent.GetPixel(x, y);
                Check(same, "Nested command pixels remain visible when its tip appears");
            }
            captureBounds.Inflate(10, 10);
            using var image = new Bitmap(captureBounds.Width, captureBounds.Height);
            using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(captureBounds.Location, Point.Empty, image.Size);
            image.Save(Path.Combine(directory, name + ".png"));
        }
        async Task Click(WF.Control surface, Rectangle bounds)
        {
            await Move(surface, bounds); Guard(); mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(80);
        }
        async Task Key(string keys)
        {
            Guard(); Check(WF.Control.ModifierKeys == WF.Keys.None, "Keyboard modifiers are released before input");
            WF.SendKeys.SendWait(keys); await Task.Delay(80);
        }
        void Stacking(string name)
        {
            var lines = new List<string>();
            foreach (var window in Windows())
            {
                var caption = new System.Text.StringBuilder(200); GetWindowText(window, caption, caption.Capacity); GetWindowRect(window, out var rect);
                lines.Add($"{window} {WF.Control.FromHandle(window)?.GetType().Name} owner={GetWindow(window, 4)} [{rect}] {caption}");
            }
            File.WriteAllLines(Path.Combine(directory, name + "-stacking.log"), lines);
        }
        IEnumerable<nint> Windows()
        {
            for (nint window = GetTopWindow(0); window != 0; window = GetWindow(window, 2))
            {
                GetWindowThreadProcessId(window, out uint process); if (process == Environment.ProcessId) yield return window;
            }
        }
    }
    private static Point Centre(Rectangle rect) => new(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
    private static IEnumerable<WF.Control> Descendants(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup
        ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected a ribbon popup");
    private sealed class Recording
    {
        internal readonly Dictionary<RibbonItem, Rectangle> Bounds = [], Arrows = [];
        internal readonly Dictionary<RibbonGroup, Rectangle> Collapsed = [];
        internal string Title = "", Description = "";
        internal bool DisabledInteraction;
        internal void Item(RibbonPaintContext c, RibbonItem item) { Bounds[item] = c.Bounds; if (!item.Enabled) DisabledInteraction |= c.Hot || c.Pressed || c.Focused; }
        internal void Tip(string title, string description) { Title = title; Description = description; }
    }
    private sealed class OfficeRecorder(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup group, bool collapsed) { if (collapsed) r.Collapsed[group] = c.Bounds; base.DrawGroup(c, group, collapsed); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Item(c, item); base.DrawItem(c, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext p, RibbonPaintContext d, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { r.Item(p, item); r.Arrows[item] = d.Bounds; base.DrawSplitItem(p, d, item, size, menu); }
        public override void DrawScreenTip(RibbonPaintContext c, string title, string description, Rectangle titleBounds, Rectangle descriptionBounds) { r.Tip(title, description); base.DrawScreenTip(c, title, description, titleBounds, descriptionBounds); }
    }
    private sealed class FlatRecorder(Recording r) : RibbonRenderer
    {
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup group, bool collapsed) { if (collapsed) r.Collapsed[group] = c.Bounds; base.DrawGroup(c, group, collapsed); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Item(c, item); base.DrawItem(c, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext p, RibbonPaintContext d, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { r.Item(p, item); r.Arrows[item] = d.Bounds; base.DrawSplitItem(p, d, item, size, menu); }
        public override void DrawScreenTip(RibbonPaintContext c, string title, string description, Rectangle titleBounds, Rectangle descriptionBounds) { r.Tip(title, description); base.DrawScreenTip(c, title, description, titleBounds, descriptionBounds); }
    }
}
