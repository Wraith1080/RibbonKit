using System.Runtime.InteropServices;
using System.IO;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public APIs only; linked into the native runner without Showcase resources.
internal static class WindowIntegrationChecks
{
    internal static int Run(string directory, bool neutral, bool frameOnly = false, bool tailOnly = false, bool projectionsOnly = false, bool snapOnly = false)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); List<string> log = []; Exception? failure = null; bool closeRequested = false, completed = false;
        var record = new Recording();
        using RibbonRenderer renderer = neutral ? new Neutral(record) : new Office(record);
        using var form = new RibbonForm { Text = "Independent RibbonForm — window and QAT checks", Width = 1100, Height = 650, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer, QuickAccessPosition = RibbonQuickAccessPosition.TitleBar, QuickAccessMaxWidth = 400 };
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Application document and lifetime stay consumer-owned." };
        var home = new RibbonTab { Id = "home", Text = "Home" }; var commands = new RibbonGroup { Id = "commands", Text = "Commands" };
        var bold = new RibbonToggleItem { Id = "bold", Text = "Bold", Icon = new Icon(), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var menu = new RibbonDropDownItem { Id = "menu", Text = "Actions", Icon = new Icon() }; menu.Items.Add(new RibbonButtonItem { Text = "Action" });
        var gallery = new RibbonGalleryItem { Id = "gallery", Text = "Styles", Icon = new Icon() }; gallery.Items.Add(new RibbonGalleryTile { Text = "Normal" });
        commands.Items.AddRange(bold, menu, gallery); home.Groups.Add(commands);
        var fields = new RibbonGroup { Id = "fields", Text = "Fields" };
        var combo = new RibbonComboBoxItem { Id = "font", Text = "Font", IsEditable = true, Value = "First" }; combo.Items.AddRange("First", "Second"); fields.Items.Add(combo); home.Groups.Add(fields);
        ribbon.Tabs.Add(home); ribbon.FileMenu.Visible = true;
        var boldRef = new RibbonQuickAccessReference { Source = bold };
        var menuRef = new RibbonQuickAccessDropDownReference { Source = menu };
        var galleryRef = new RibbonQuickAccessDropDownReference { Source = gallery };
        var comboRef = new RibbonQuickAccessComboBoxReference { Source = combo };
        var groupRef = new RibbonQuickAccessGroupReference { Source = fields };
        ribbon.QuickAccessItems.AddRange(boldRef, menuRef, galleryRef, comboRef, groupRef);
        ribbon.MessageBar.Items.Add(new RibbonMessage { Title = "Notice", Text = "Messages remain below the ribbon." });
        int clicks = 0; bold.Click += (_, _) => clicks++;
        form.Controls.Add(document); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        var cursor = WF.Cursor.Position;
        form.FormClosed += (_, _) => { if (closeRequested) { completed = true; log.Add("PASS: Real close button closes the test-owned Form"); } };
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(150); Guard();
                Check(ribbon.EffectiveQuickAccessPosition == RibbonQuickAccessPosition.TitleBar, $"TitleBar supported at actual {form.DeviceDpi} DPI");
                if (snapOnly)
                {
                    int width = form.Width; Guard(); keybd_event(91, 0, 0, 0); keybd_event(37, 0, 0, 0); keybd_event(37, 0, 2, 0); keybd_event(91, 0, 2, 0); await Task.Delay(300);
                    Check(form.Width < width && form.RectangleToScreen(form.ClientRectangle).Left >= WF.Screen.FromControl(form).WorkingArea.Left, "Windows Snap keyboard command resizes the native window");
                    // Snap Assist can own the foreground. Read-only capture and test-owned cleanup are safe; send no more input.
                    await Capture("snapped", false); completed = true; return;
                }
                nint? editorHandle = null;
                foreach (var density in frameOnly ? Array.Empty<RibbonDensity>() : new[] { RibbonDensity.Compact, RibbonDensity.Touch })
                foreach (var layout in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
                {
                    ribbon.Density = density; ribbon.LayoutMode = layout; ribbon.Refresh(); await Task.Delay(40);
                    Check(record.Entries[boldRef].Bottom <= record.Tab.Top && record.Toolbar.Right < ribbon.Width - form.DeviceDpi, $"{density}/{layout}: caption QAT, tabs and system buttons have separate geometry");
                    int before = clicks; await Click(ribbon, record.Entries[boldRef]); Check(clicks == before + 1, "Caption QAT invokes original command once");
                    foreach (var reference in new RibbonElement[] { menuRef, galleryRef, comboRef, groupRef })
                    {
                        await Click(ribbon, record.Entries[reference]);
                        Check(reference switch { RibbonQuickAccessDropDownReference d => d.IsDropDownOpen, RibbonQuickAccessComboBoxReference c => c.IsDropDownOpen, RibbonQuickAccessGroupReference g => g.IsDropDownOpen, _ => false }, "Caption projection opens " + reference.GetType().Name);
                        if (reference == groupRef)
                        {
                            var editor = Tree(Popup()).OfType<WF.ComboBox>().Single();
                            editorHandle ??= editor.Handle; Check(editor.Handle == editorHandle, "Borrowed group retains the native ComboBox handle");
                        }
                        await Capture($"{density}-{layout}-{(reference == menuRef ? "menu" : reference == galleryRef ? "gallery" : reference == comboRef ? "chooser" : "group")}");
                        await Key("{ESC}");
                    }
                    ribbon.IsMinimized = true; ribbon.Refresh();
                    await Click(ribbon, record.Tab); Check(Popup().PointToScreen(Point.Empty).Y >= ribbon.PointToScreen(new(0, record.Tab.Bottom)).Y, "Temporary minimized body anchors below tabs"); await Key("{ESC}");
                    await Capture($"{density}-{layout}-minimized"); ribbon.IsMinimized = false;
                }
                if (projectionsOnly) { completed = true; return; }
                ribbon.Density = RibbonDensity.Compact; ribbon.LayoutMode = RibbonLayoutMode.Classic;
                foreach (var position in new[] { RibbonQuickAccessPosition.BelowRibbon, RibbonQuickAccessPosition.TitleBar, RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.TitleBar })
                { ribbon.QuickAccessPosition = position; ribbon.Refresh(); Check(!ribbon.IsAnimating, "Caption placement changes commit immediately"); await Capture(position.ToString()); }
                using (var session = ribbon.CommandCatalog.BeginCustomization("window"))
                { var state = RibbonCustomizationSerializer.Deserialize(session.ExportJson(), "window"); Check(state.QuickAccessPosition == RibbonQuickAccessPosition.TitleBar, "Profile retains TitleBar"); }
                ribbon.Refresh(); await Task.Delay(60);
                if (!tailOnly)
                {
                var originalBounds = form.Bounds;
                var caption = form.PointToScreen(new(ribbon.Width / 2, record.Tab.Top / 2));
                await Drag(caption, new(caption.X + 45, caption.Y + 25));
                Check(form.Location != originalBounds.Location, "Real caption drag moves the native window");
                var size = form.Size;
                var edge = new Point(form.Right - 2, form.Top + form.Height / 2);
                await Drag(edge, new(edge.X + 35, edge.Y)); Check(form.Width > size.Width, "Real native edge drag resizes");
                // Use public Win32 nonclient hit tests to locate the custom/native button targets.
                var max = Button(9); Guard(); WF.Cursor.Position = max; await Task.Delay(1100); await Capture("maximize-hover"); await ClickScreen(max); await Task.Delay(150);
                Check(form.WindowState == WF.FormWindowState.Maximized, "Real maximize button click maximizes");
                Check(ScreenClientInsideWorkArea(), "Maximized client edges stay within the monitor work area"); await Capture("maximized");
                await ClickScreen(Button(9)); Check(form.WindowState == WF.FormWindowState.Normal, "Restore button returns to normal");
                }
                Guard(); keybd_event(18, 0, 0, 0); keybd_event(32, 0, 0, 0); keybd_event(32, 0, 2, 0); keybd_event(18, 0, 2, 0); await Task.Delay(150); await Capture("system-menu"); await Key("{ESC}");
                await ClickScreen(Button(8)); await Task.Delay(100); Check(form.WindowState == WF.FormWindowState.Minimized, "Real minimize button click minimizes");
                ShowWindow(form.Handle, 9); await Task.Delay(150); Guard();
                ribbon.QuickAccessVisible = false; form.Ribbon = null;
                Check(ribbon.QuickAccessPosition == RibbonQuickAccessPosition.TitleBar && ribbon.EffectiveQuickAccessPosition == RibbonQuickAccessPosition.TabRow, "Detach retains hidden preference with visible TabRow fallback");
                ribbon.QuickAccessVisible = true; ribbon.Refresh(); await Capture("detached-tabrow");
                using var ordinary = new WF.Form { Text = "Ordinary Form fallback", Size = form.Size, StartPosition = WF.FormStartPosition.CenterScreen };
                ordinary.Controls.Add(ribbon); ordinary.Show(); ordinary.Activate(); await Task.Delay(100);
                Check(ribbon.EffectiveQuickAccessPosition == RibbonQuickAccessPosition.TabRow && ribbon.QuickAccessItems.Count == 5, "Ordinary Form keeps all original declarations");
                form.Controls.Add(ribbon); ordinary.Close(); form.Ribbon = ribbon; form.Activate(); await Task.Delay(100); Guard();
                Check(ribbon.EffectiveQuickAccessPosition == RibbonQuickAccessPosition.TitleBar && combo.Value == "First", $"Reattach restores TitleBar and source state ({ribbon.EffectiveQuickAccessPosition}, {combo.Value})");
                closeRequested = true; await ClickScreen(Button(20)); Check(form.IsDisposed || !form.Visible, "Real close button closes the test-owned Form");
            }
            catch (Exception e) { failure = e; log.Add("FAIL: " + e); try { await Capture("failure"); } catch { } }
            finally { WF.Cursor.Position = cursor; form.Close(); }
        };
        WF.Application.Run(form); WF.Cursor.Position = cursor; if (!completed && failure is null) { failure = new InvalidOperationException("Native validation ended before its close check."); log.Add("FAIL: " + failure); } File.WriteAllLines(Path.Combine(directory, "result.log"), log); return failure is null ? 0 : 1;

        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); log.Add("PASS: " + message); File.WriteAllLines(Path.Combine(directory, "progress.log"), log); }
        void Guard() { var active = GetForegroundWindow(); GetWindowThreadProcessId(active, out uint owner); if (owner != Environment.ProcessId) throw new InvalidOperationException("Test window lost foreground; input stopped."); }
        async Task Key(string keys) { Guard(); WF.SendKeys.SendWait(keys); await Task.Delay(80); }
        async Task Click(WF.Control surface, Rectangle bounds) => await ClickScreen(surface.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)));
        async Task ClickScreen(Point point) { Guard(); WF.Cursor.Position = point; await Task.Delay(40); Guard(); mouse_event(2, 0, 0, 0, 0); await Task.Delay(35); mouse_event(4, 0, 0, 0, 0); await Task.Delay(100); }
        async Task Drag(Point from, Point to) { Guard(); WF.Cursor.Position = from; mouse_event(2, 0, 0, 0, 0); await Task.Delay(50); for (int step = 1; step <= 5; step++) { Guard(); WF.Cursor.Position = new(from.X + (to.X - from.X) * step / 5, from.Y + (to.Y - from.Y) * step / 5); await Task.Delay(30); } mouse_event(4, 0, 0, 0, 0); await Task.Delay(100); }
        Point Button(int hit)
        {
            int y = form.PointToScreen(Point.Empty).Y + Math.Max(12, record.Tab.Top / 2);
            for (int x = form.Right - 12; x > form.Right - 240; x -= 3)
                if ((int)SendMessage(form.Handle, 0x84, 0, Pack(new(x, y))) == hit) return new(x, y);
            throw new InvalidOperationException("Caption target missing: " + hit);
        }
        bool ScreenClientInsideWorkArea() { var bounds = form.RectangleToScreen(form.ClientRectangle); var work = WF.Screen.FromControl(form).WorkingArea; log.Add($"GEOMETRY: client={bounds}; work={work}"); return work.Contains(bounds); }
        async Task Capture(string name, bool verifyForeground = true) { await Task.Delay(35); if (verifyForeground) Guard(); var bounds = form.Bounds; if (WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup) bounds = Rectangle.Union(bounds, popup.Bounds); using var image = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(image); graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); image.Save(Path.Combine(directory, name + ".png")); }
    }
    private static nint Pack(Point point) => unchecked((nint)((point.Y << 16) | (point.X & 65535)));
    private static IEnumerable<WF.Control> Tree(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Tree(c)));
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected a library popup.");
    private sealed class Recording { internal Dictionary<RibbonElement, Rectangle> Entries = []; internal Rectangle Tab, Toolbar; }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { if (!menu) r.Entries[entry] = c.Bounds; base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext c, RibbonQuickAccessPosition p) { r.Toolbar = c.Bounds; base.DrawQuickAccessToolbar(c, p); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tab = c.Bounds; base.DrawTab(c, tab, selected); }
    }
    private sealed class Neutral(Recording r) : RibbonRenderer
    {
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { if (!menu) r.Entries[entry] = c.Bounds; base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext c, RibbonQuickAccessPosition p) { r.Toolbar = c.Bounds; base.DrawQuickAccessToolbar(c, p); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tab = c.Bounds; base.DrawTab(c, tab, selected); }
    }
    private sealed class Icon : IRibbonIcon { public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { using var brush = new SolidBrush(context.Foreground); graphics.FillEllipse(brush, bounds); } }
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wparam, nint lparam);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint window, int command);
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, nuint extra);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
}
