using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Ordinary Form, public APIs and native input; no Showcase presentation helpers.
internal static class PopupMotionInteractionChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetFocus();
    [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint parameter, out bool value, uint flags);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    internal static int Run(string directory, bool neutral, bool interactionsOnly = false)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles(); WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); List<string> log = []; Exception? failure = null; var record = new Record(); var pointer = WF.Cursor.Position;
        using RibbonRenderer renderer = neutral ? new Neutral(record) : new Office(record);
        using var host = new WF.Form { Text = "Public popup opening motion", Width = 1200, Height = 760, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer }; var tab = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "commands", Text = "Commands" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var menu = new RibbonDropDownItem { Id = "paste", Text = "Paste", Icon = new Icon(), DropDownHeader = "Paste options" };
        var plain = new RibbonButtonItem { Id = "plain", Text = "Paste text only", Icon = new Icon(), MenuDescription = "Insert plain clipboard text without its original formatting." };
        var nested = new RibbonDropDownItem { Id = "more", Text = "More", Icon = new Icon() }; nested.Items.Add(new RibbonButtonItem { Id = "nested", Text = "Nested command" });
        var combo = new RibbonComboBoxItem { Id = "font", Text = "Font", IsEditable = true, Value = "100" }; combo.Items.AddRange("100", "150"); combo.SelectedIndex = 0; menu.Items.AddRange(plain, nested, combo);
        var gallery = new RibbonGalleryItem { Id = "styles", Text = "Styles", Icon = new Icon(), ColumnCount = 3 };
        for (int i = 0; i < 6; i++) gallery.Items.Add(new RibbonGalleryTile { Id = "style." + i, Text = "Style " + i, GroupName = i < 3 ? "Document" : "Headings", Icon = new Icon() });
        gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0]; group.Items.AddRange(menu, gallery);
        var utilities = new RibbonGroup { Id = "utilities", Text = "Utilities" }; utilities.Items.Add(new RibbonButtonItem { Text = "Utility command", Icon = new Icon() }); tab.Groups.Add(utilities);
        var qatMenu = new RibbonQuickAccessDropDownReference { Id = "qat.paste", Source = menu }; var qatGallery = new RibbonQuickAccessDropDownReference { Id = "qat.styles", Source = gallery }; var qatGroup = new RibbonQuickAccessGroupReference { Id = "qat.group", Source = utilities };
        ribbon.QuickAccessItems.AddRange(qatMenu, qatGallery, qatGroup);
        host.Controls.Add(new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "The application supplies no animation or popup implementation." }); host.Controls.Add(ribbon);
        int clicks = 0, commits = 0; plain.Click += (_, _) => clicks++; gallery.SelectionCommitted += (_, _) => commits++; nint editorHandle = 0;
        host.Shown += async (_, _) =>
        {
            try
            {
                await Activate(); await Task.Delay(300); ribbon.Refresh();
                if (!SystemParametersInfo(0x1042, 0, out bool enabled, 0) || !enabled) { log.Add("SKIP: OS client-area animations disabled; native playback was not forced."); return; }
                foreach (var density in interactionsOnly ? [] : Enum.GetValues<RibbonDensity>())
                {
                    ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.Density = density; ribbon.AnimationLevel = density == RibbonDensity.Compact ? RibbonAnimationLevel.Subtle : RibbonAnimationLevel.Expressive; ribbon.Refresh();
                    foreach (bool qat in new[] { false, true })
                    foreach (bool styles in new[] { false, true })
                    {
                        string name = density + (qat ? "-qat-" : "-body-") + (styles ? "gallery" : "menu");
                        var popup = await Play(name, async () => await Click(ribbon, qat ? record.Qat[styles ? qatGallery.Id : qatMenu.Id] : record.Body[styles ? gallery.Id : menu.Id]));
                        if (!styles)
                        {
                            var editor = Descendants(Content(popup)).OfType<WF.ComboBox>().Single(c => c.Name == combo.Id);
                            if (editorHandle == 0) editorHandle = editor.Handle; Check(editor.Handle == editorHandle && editor.Text == "100", name + " retains native editor HWND/text across openings");
                        }
                        Check(ReferenceEquals(menu.Parent, group) && ReferenceEquals(gallery.Parent, group) && commits == 0 && clicks == 0, name + " frame playback does not execute or commit a command");
                        await Escape(); Check(!popup.Visible && !ribbon.IsAnimating, name + " closes immediately");
                    }
                }
                if (interactionsOnly) { ribbon.Density = RibbonDensity.Touch; ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.Refresh(); }
                await Click(ribbon, record.Body[menu.Id]); var parent = Popup(); Check(ribbon.IsAnimating, "Parent opening starts");
                var child = await Play("nested-menu", async () => await Click(Content(parent), record.Menu[nested.Id])); Check(!ReferenceEquals(parent, child) && parent.Visible, "Submenu leaves parent open"); await Escape(); Check(parent.Visible && !ribbon.IsAnimating, "Nested Escape returns to settled parent"); await Escape();
                await Click(ribbon, record.Body[menu.Id]); var immediate = Popup(); Check(ribbon.IsAnimating, "Command input begins during opening"); await Click(Content(immediate), record.Menu[plain.Id]); Check(clicks == 1 && !immediate.Visible && !ribbon.IsAnimating, $"Pointer input settles and invokes exactly once (clicks={clicks}, visible={immediate.Visible}, playing={ribbon.IsAnimating})");
                await Click(ribbon, record.Body[gallery.Id]); var picking = Popup(); Check(ribbon.IsAnimating, "Gallery input begins during opening"); await Click(Content(picking), record.Tiles[gallery.Items[4].Id]); Check(commits == 1 && ReferenceEquals(gallery.SelectedItem, gallery.Items[4]) && !ribbon.IsAnimating, "Gallery input settles and commits exactly once");
                await Click(ribbon, record.Body[menu.Id]); var fields = Popup(); var nativeField = Descendants(Content(fields)).OfType<WF.ComboBox>().Single(c => c.Name == combo.Id); var fieldBounds = nativeField.Bounds;
                await Click(nativeField, nativeField.ClientRectangle); WF.SendKeys.SendWait("{F4}"); await Task.Delay(25);
                using (var fieldFrame = Capture(fields.Bounds)) fieldFrame.Save(Path.Combine(directory, "native-field.png"));
                Check(!ribbon.IsAnimating && combo.IsDropDownOpen, $"Native ComboBox choices remain immediate (playing={ribbon.IsAnimating}, open={combo.IsDropDownOpen}, bounds={fieldBounds}, surface={Content(fields).Size})"); WF.SendKeys.SendWait("{DOWN}{ENTER}"); await Task.Delay(35); Check(combo.Value == "150", "Native ComboBox selection still works"); await Escape();
                await Play("qat-group", async () => await Click(ribbon, record.Qat[qatGroup.Id])); await Escape();
                ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsMinimized = true; ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.Refresh();
                await Play("minimized-tab", async () => await Click(ribbon, record.Tab)); await Escape();
                ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsMinimized = false; ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; ribbon.Refresh();
                await Click(ribbon, record.Body[menu.Id]); Check(ribbon.IsAnimating, "Deactivation starts with an opening popup");
                using (var other = new WF.Form { Text = "Other application window", Width = 300, Height = 180 }) { other.Show(); other.Activate(); SetForegroundWindow(other.Handle); await Task.Delay(100); Check(!menu.IsDropDownOpen && !ribbon.IsAnimating, "Real deactivation dismisses popup and drawing"); other.Close(); }
                await Activate(); ribbon.Refresh(); await Click(ribbon, record.Body[menu.Id]); int closing = 0; WF.FormClosingEventHandler cancel = (_, e) => { closing++; e.Cancel = true; }; host.FormClosing += cancel;
                WF.SendKeys.SendWait("%{F4}"); await Task.Delay(60); Check(closing == 1 && !menu.IsDropDownOpen && !ribbon.IsAnimating, "Alt+F4 reaches the cancellable host once during opening"); host.FormClosing -= cancel;
                log.Add($"PASS: public popup motion, menus/galleries/QAT/group/minimized body, Compact/Touch, Subtle/Expressive, input and activation; DPI={ribbon.DeviceDpi}; renderer={(neutral ? "neutral" : "Office2024")}");
            }
            catch (Exception error) { failure = error; log.Add(error.ToString()); }
            finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); WF.Cursor.Position = pointer; host.Close(); }
        };
        WF.Application.Run(host); return failure is null ? 0 : 1;
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        async Task Activate() { for (int i = 0; i < 4 && GetForegroundWindow() != host.Handle; i++) { host.Activate(); SetForegroundWindow(host.Handle); await Task.Delay(150); } Check(GetForegroundWindow() == host.Handle, "Host has foreground"); }
        async Task Click(WF.Control control, Rectangle bounds)
        {
            Check(!bounds.IsEmpty, "Native pointer target was painted"); WF.Cursor.Position = control.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(20); mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(15);
        }
        async Task Escape() { WF.SendKeys.SendWait("{ESC}"); await Task.Delay(25); }
        WF.ToolStripDropDown Popup() => WF.Control.FromChildHandle(GetFocus())?.TopLevelControl as WF.ToolStripDropDown ?? WF.Control.FromHandle(GetForegroundWindow()) as WF.ToolStripDropDown ?? throw new InvalidOperationException("No focused native popup.");
        static WF.Control Content(WF.ToolStripDropDown popup) => popup.Items.OfType<WF.ToolStripControlHost>().Single().Control;
        async Task<WF.ToolStripDropDown> Play(string name, Func<Task> trigger)
        {
            await trigger(); Check(ribbon.IsAnimating, name + " starts inert opening motion"); var popup = Popup(); var bounds = popup.Bounds; List<Bitmap> frames = [];
            try
            {
                foreach (int delay in new[] { 8, 25, 25 }) { await Task.Delay(delay); Check(popup.Bounds == bounds && popup.Visible, name + " keeps its frame anchored"); frames.Add(Capture(bounds)); }
                var deadline = Stopwatch.StartNew(); while (ribbon.IsAnimating && deadline.ElapsedMilliseconds < 1500) await Task.Delay(3);
                Check(!ribbon.IsAnimating && popup.Visible, name + " finishes autonomously and stays open"); Check(GetForegroundWindow() == popup.Handle || GetForegroundWindow() == host.Handle, name + " drawing never activates");
                using var final = Capture(bounds); Check(Different(frames[0], final), name + " presents visible intermediate content");
                foreach (var p in new[] { Point.Empty, new Point(bounds.Width - 1, 0), new Point(0, bounds.Height - 1), new Point(bounds.Width - 1, bounds.Height - 1), new Point(0, bounds.Height / 2), new Point(bounds.Width - 1, bounds.Height / 2) }) Check(frames[0].GetPixel(p.X, p.Y) == final.GetPixel(p.X, p.Y), name + " frame/corner pixel stays fixed");
                for (int i = 0; i < frames.Count; i++) frames[i].Save(Path.Combine(directory, name + "-" + i + ".png")); final.Save(Path.Combine(directory, name + "-final.png")); return popup;
            }
            finally { foreach (var frame in frames) frame.Dispose(); }
        }
    }
    private static Bitmap Capture(Rectangle bounds) { var image = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(image); graphics.CopyFromScreen(bounds.Location, Point.Empty, image.Size); return image; }
    private static bool Different(Bitmap first, Bitmap last) { for (int y = 2; y < first.Height - 2; y++) for (int x = 2; x < first.Width - 2; x++) if (first.GetPixel(x, y) != last.GetPixel(x, y)) return true; return false; }
    private static IEnumerable<WF.Control> Descendants(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(child => Descendants(child).Prepend(child));
    private sealed class Record { internal readonly Dictionary<string, Rectangle> Body = [], Menu = [], Qat = [], Tiles = []; internal Rectangle Tab; }
    private sealed class Office(Record r) : RibbonOffice2024Renderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { (menu ? r.Menu : r.Body)[item.Id] = c.Bounds; base.DrawItem(c, item, size, menu); }
        public override void DrawGalleryTile(RibbonPaintContext c, RibbonGalleryTile tile, bool selected) { r.Tiles[tile.Id] = c.Bounds; base.DrawGalleryTile(c, tile, selected); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { if (!menu) r.Qat[entry.Id] = c.Bounds; base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tab = c.Bounds; base.DrawTab(c, tab, selected); }
    }
    private sealed class Neutral(Record r) : RibbonRenderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { (menu ? r.Menu : r.Body)[item.Id] = c.Bounds; base.DrawItem(c, item, size, menu); }
        public override void DrawGalleryTile(RibbonPaintContext c, RibbonGalleryTile tile, bool selected) { r.Tiles[tile.Id] = c.Bounds; base.DrawGalleryTile(c, tile, selected); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { if (!menu) r.Qat[entry.Id] = c.Bounds; base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tab = c.Bounds; base.DrawTab(c, tab, selected); }
    }
    private sealed class Icon : IRibbonIcon { public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { using var pen = new Pen(context.Foreground, Math.Max(1, context.DeviceDpi / 96f)); bounds.Inflate(-bounds.Width / 5, -bounds.Height / 6); graphics.DrawRectangle(pen, bounds); graphics.DrawLine(pen, bounds.Left, bounds.Top + bounds.Height / 2, bounds.Right, bounds.Top + bounds.Height / 2); } }
}
