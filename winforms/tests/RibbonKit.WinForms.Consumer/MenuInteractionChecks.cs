using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Runs unchanged against a fresh package outside the repository. Only public
// model/renderer APIs and normal Windows input are used.
internal static class MenuInteractionChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint hwnd, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);

    internal static int Run(string directory, bool flat = false)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); var results = new List<string>(); var recording = new Recording();
        using RibbonRenderer renderer = flat ? new FlatRecorder(recording) : new OfficeRecorder(recording);
        using var form = new WF.Form { Text = "Public rich-menu checks", Width = 900, Height = 600, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer }; var tab = new RibbonTab { Text = "Home", KeyTip = "H" };
        var group = new RibbonGroup { Text = "Menu commands" }; var menu = new RibbonDropDownItem { Text = "Format", KeyTip = "M" };
        var first = new RibbonButtonItem { Text = "Keep source formatting", MenuDescription = "Preserve the source document's fonts, colors, and paragraph styles.", ScreenTipDescription = "Insert with source formatting.", KeyTip = "K" };
        var separator = new RibbonMenuSeparatorItem { Text = "Never painted", KeyTip = "Z", ScreenTipDescription = "Never shown" };
        var unavailable = new RibbonButtonItem { Text = "Unavailable format", MenuDescription = "Available after selecting a compatible document.", Enabled = false };
        var hidden = new RibbonButtonItem { Text = "Hidden format", Visible = false };
        var nested = new RibbonDropDownItem { Text = "Page background", MenuDescription = "Choose a page color for the current document.", KeyTip = "P" };
        var white = new RibbonRadioButtonItem { Text = "White", Checked = true, MenuDescription = "Use the standard white page background." };
        var sepia = new RibbonRadioButtonItem { Text = "Sepia", MenuDescription = "Use a warm background for comfortable reading." };
        nested.Items.AddRange(white, new RibbonMenuSeparatorItem(), sepia);
        var split = new RibbonSplitButtonItem { Text = "Apply style", MenuDescription = "Apply the selected style or open the style choices.", KeyTip = "A", DropDownKeyTip = "S" };
        var choice = new RibbonButtonItem { Text = "Choose style", MenuDescription = "Choose a different style for the selection." }; split.Items.Add(choice);
        var check = new RibbonCheckBoxItem { Text = "Show ruler", MenuDescription = "Display measurements above the document." };
        var editor = new RibbonTextBoxItem { Text = "Name", Value = "Document", InputWidth = 160, MenuDescription = "Native editor stays compact" };
        menu.Items.AddRange(first, separator, unavailable, hidden, nested, split, check, editor);
        group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Application-owned document" };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        int clicks = 0, splitClicks = 0, choices = 0; first.Click += (_, _) => clicks++; split.Click += (_, _) => splitClicks++; choice.Click += (_, _) => choices++;
        bool completed = false; Exception? error = null; var originalCursor = WF.Cursor.Position;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle);
                for (int i = 0; i < 20 && GetForegroundWindow() != form.Handle; i++) await Task.Delay(50);
                Guard(); ribbon.Refresh(); await Open(); var parent = Popup();
                Check(!recording.Bounds.ContainsKey(hidden), "Hidden rows have no painted target");
                Check(recording.Texts.Contains(first.MenuDescription) && recording.Texts.Contains(nested.MenuDescription), "Shared text hook receives full descriptions");
                Check(recording.Separators > 0 && recording.SubmenuArrows > 0, "Public separator and submenu-arrow hooks are used");
                Check(recording.Bounds[first].Height > recording.Bounds[editor].Height && recording.Bounds[separator].Height < recording.Bounds[editor].Height, "Rich commands, compact native fields, and decorative separators get distinct heights");
                Check(!recording.SeparatorInteraction && !recording.DisabledInteraction, "Decorative and disabled rows have no interaction chrome");
                await Capture("mixed-menu"); await Click(parent, recording.Bounds[separator]);
                Check(menu.IsDropDownOpen && clicks == 0 && menu.Items.Contains(separator), "Clicking a separator leaves its menu and command state intact");
                await Key("{HOME}"); parent.Refresh(); Check(ReferenceEquals(recording.Focused, first), "Home focuses first actionable row");
                await Key("{DOWN}"); parent.Refresh(); Check(ReferenceEquals(recording.Focused, nested), "Down skips decorative, disabled, and hidden rows");
                await Key("{RIGHT}"); var child = Popup();
                await Capture("submenu-right");
                Check(nested.IsDropDownOpen && child.TopLevelControl!.Left >= parent.TopLevelControl!.Right, $"Right opens a nested menu beside the parent (open={nested.IsDropDownOpen}, parent={parent.TopLevelControl!.Bounds}, child={child.TopLevelControl!.Bounds})");
                await Key("{LEFT}"); parent.Refresh();
                Check(!nested.IsDropDownOpen && menu.IsDropDownOpen && ReferenceEquals(recording.Focused, nested), "Left closes one level and restores the opener");
                await Key("{RIGHT}"); await Key("{ESC}"); parent.Refresh();
                Check(!nested.IsDropDownOpen && menu.IsDropDownOpen && ReferenceEquals(recording.Focused, nested), "Escape closes one level and restores the opener");
                await Click(parent, recording.Bounds[nested]); child = Popup(); await Click(child, recording.Bounds[sepia]);
                Check(sepia.Checked && !white.Checked && !menu.IsDropDownOpen, "Nested radio activation closes the chain and preserves exclusivity");
                await Open(); parent = Popup(); await Click(parent, recording.Bounds[split]);
                Check(splitClicks == 1 && !split.IsDropDownOpen && !menu.IsDropDownOpen, "Rich split primary invokes exactly once");
                await Open(); parent = Popup(); await Click(parent, recording.Arrows[split]); child = Popup();
                Check(split.IsDropDownOpen && splitClicks == 1, "Rich split arrow opens choices independently");
                await Capture("split-submenu"); await Click(child, recording.Bounds[choice]);
                Check(choices == 1 && !menu.IsDropDownOpen, "Nested split choice invokes exactly once");
                await Open(); parent = Popup(); await Click(parent, recording.Bounds[check]);
                Check(check.Checked && !menu.IsDropDownOpen, "Described checkbox uses the ordinary command activation path");
                await Open(); parent = Popup();
                var textBox = Descendants(parent).OfType<WF.TextBox>().Single(t => t.Text == "Document");
                await Click(textBox, textBox.ClientRectangle); await Key("^a"); await Key("Edited");
                await Capture("native-editor"); await Key("{ENTER}"); await Key("{ESC}");
                Check(editor.Value == "Edited" && !menu.IsDropDownOpen, "Committed native field edits survive popup dismissal");
                await Open(); parent = Popup();
                Guard(); WF.Cursor.Position = document.PointToScreen(new(20, 20)); await Task.Delay(60);
                Guard(); WF.Cursor.Position = parent.PointToScreen(Centre(recording.Bounds[first])); await Task.Delay(850); Guard();
                Check(WF.Application.OpenForms.Cast<WF.Form>().Any(f => f.Owner == form && f.FormBorderStyle == WF.FormBorderStyle.None && f.Visible), "Described rows retain delayed passive ScreenTips");
                await Capture("rich-row-tip"); await Click(parent, recording.Bounds[first]);
                Check(clicks == 1 && !menu.IsDropDownOpen, "Clicking a described row with its tip visible invokes exactly once");
                form.Width = 440; var work = WF.Screen.FromControl(form).WorkingArea;
                form.Location = new(work.Right - form.Width, work.Top + 30); await Task.Delay(100); ribbon.Refresh();
                await Open(); parent = Popup(); await Click(parent, recording.Bounds[nested]); child = Popup();
                Check(child.TopLevelControl!.Right <= parent.TopLevelControl!.Left, "Submenu flips left at the monitor edge");
                Check(work.Contains(child.TopLevelControl.Bounds), "Flipped child remains inside the work area");
                await Capture("submenu-left"); await Key("{ESC}"); await Key("{ESC}");
                await Open(); menu.Items.Remove(first); await Task.Delay(60);
                Check(!menu.IsDropDownOpen && clicks == 1 && editor.Value == "Edited", "Collection mutation dismisses the chain without losing editor or command state");
                first.Dispose();
                for (int i = 0; i < 30; i++) menu.Items.Add(new RibbonButtonItem { Text = "Additional format " + (i + 1), MenuDescription = "Another document format." });
                await Open(); parent = Popup(); var scrollParent = (WF.ScrollableControl)parent;
                Check(scrollParent.VerticalScroll.Visible, "An overflowing rich menu exposes native scrolling");
                await Click(parent, recording.Bounds[nested]); Check(nested.IsDropDownOpen, "A scrollable parent can open a nested menu");
                Guard(); SendMessage(parent.Handle, 0x115, 1, 0); await Task.Delay(100); // WM_VSCROLL / SB_LINEDOWN
                Check(!nested.IsDropDownOpen && menu.IsDropDownOpen, "Scrolling the parent closes the child before its anchor moves");
                await Capture("scrolling-menu"); await Click(document, document.ClientRectangle);
                Check(!menu.IsDropDownOpen && editor.Value == "Edited", "Outside click dismisses an overflowing menu and retains its field value");
                completed = true; results.Add($"PASS {(flat ? "Neutral" : "Office")} menu checks at {ribbon.DeviceDpi} DPI");
            }
            catch (Exception e) { error = e; results.Add("FAIL " + e); }
            finally
            {
                if (GetForegroundWindow() == form.Handle || WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown) WF.Cursor.Position = originalCursor;
                form.Close();
            }
        };
        WF.Application.Run(form); File.WriteAllLines(Path.Combine(directory, "result.log"), results);
        return completed && error is null ? 0 : 1;

        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); results.Add("PASS " + message); }
        void Guard() => Check(GetForegroundWindow() == form.Handle || WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown,
            "Validation window or its popup owns foreground before input");
        async Task Open() { await Click(ribbon, recording.Bounds[menu]); Check(menu.IsDropDownOpen, "Menu opens through native mouse input"); }
        async Task Click(WF.Control control, Rectangle bounds)
        {
            Guard(); WF.Cursor.Position = control.PointToScreen(Centre(bounds)); await Task.Delay(35); Guard();
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(90);
        }
        async Task Key(string keys) { Guard(); Check(WF.Control.ModifierKeys == WF.Keys.None, "Input modifiers are released"); WF.SendKeys.SendWait(keys); await Task.Delay(90); }
        async Task Capture(string name)
        {
            await Task.Delay(70); Guard();
            var bounds = form.Bounds;
            foreach (var popup in WF.Application.OpenForms.Cast<WF.Form>().Where(f => f.Owner == form && f.Visible)) bounds = Rectangle.Union(bounds, popup.Bounds);
            // ToolStripDropDown is not included in Application.OpenForms.
            if (WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown active) bounds = Rectangle.Union(bounds, active.Bounds);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private static Point Centre(Rectangle bounds) => new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
    private static IEnumerable<WF.Control> Descendants(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup
        ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected an active library popup");
    private sealed class Recording
    {
        internal readonly Dictionary<RibbonItem, Rectangle> Bounds = [], Arrows = [];
        internal readonly HashSet<string> Texts = [];
        internal RibbonItem? Focused; internal int Separators, SubmenuArrows; internal bool SeparatorInteraction, DisabledInteraction;
        internal void Item(RibbonPaintContext c, RibbonItem item) { Bounds[item] = c.Bounds; if (c.Focused) Focused = item; if (!item.Enabled) DisabledInteraction |= c.Hot || c.Pressed || c.Focused; }
        internal void Separator(RibbonPaintContext c) { Separators++; SeparatorInteraction |= c.Hot || c.Pressed || c.Focused; }
    }
    private sealed class OfficeRecorder(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Item(c, item); base.DrawItem(c, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext p, RibbonPaintContext d, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { r.Item(p, item); r.Arrows[item] = d.Bounds; base.DrawSplitItem(p, d, item, size, menu); }
        public override void DrawTextBox(RibbonPaintContext c, RibbonTextBoxItem item, Rectangle bounds) { r.Item(c, item); base.DrawTextBox(c, item, bounds); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, WF.TextFormatFlags flags) { r.Texts.Add(text); base.DrawText(c, text, color, flags); }
        public override void DrawMenuSeparator(RibbonPaintContext c, RibbonMenuSeparatorItem item) { r.Separator(c); base.DrawMenuSeparator(c, item); }
        public override void DrawSubmenuArrow(RibbonPaintContext c, Color color) { r.SubmenuArrows++; base.DrawSubmenuArrow(c, color); }
    }
    private sealed class FlatRecorder(Recording r) : RibbonRenderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Item(c, item); base.DrawItem(c, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext p, RibbonPaintContext d, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { r.Item(p, item); r.Arrows[item] = d.Bounds; base.DrawSplitItem(p, d, item, size, menu); }
        public override void DrawTextBox(RibbonPaintContext c, RibbonTextBoxItem item, Rectangle bounds) { r.Item(c, item); base.DrawTextBox(c, item, bounds); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, WF.TextFormatFlags flags) { r.Texts.Add(text); base.DrawText(c, text, color, flags); }
        public override void DrawMenuSeparator(RibbonPaintContext c, RibbonMenuSeparatorItem item) { r.Separator(c); base.DrawMenuSeparator(c, item); }
        public override void DrawSubmenuArrow(RibbonPaintContext c, Color color) { r.SubmenuArrows++; base.DrawSubmenuArrow(c, color); }
    }
}
