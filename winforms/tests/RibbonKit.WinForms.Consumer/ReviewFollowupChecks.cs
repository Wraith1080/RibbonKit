using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class ReviewFollowupChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private delegate bool Visitor(nint window, nint state);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, Visitor visitor, nint state);
    internal static int Run(string directory, bool neutral, bool groupsOnly = false, RibbonDensity? onlyDensity = null)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles(); WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); List<string> log = []; Exception? error = null; var cursor = WF.Cursor.Position;
        var record = new Recording(); using RibbonRenderer painter = neutral ? new Neutral(record) : new Office(record);
        using var form = new WF.Form { Size = new(1240, 760), Text = "Independent review follow-up", StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = painter, FileMenuMode = RibbonFileMenuMode.Backstage }; ribbon.FileMenu.Visible = true;
        var tab = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "fields", Text = "Native editors", Icon = new Icon(), CanResize = false };
        var combo = new RibbonComboBoxItem { Id = "font", Text = "Font", IsEditable = true, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; combo.Items.AddRange("Segoe UI", "Calibri"); combo.SelectedIndex = 0;
        var toggle = new RibbonToggleItem { Id = "bold", Text = "Bold", Icon = new Icon(), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; group.Items.AddRange(combo, toggle); tab.Groups.Add(group);
        for (int i = 0; i < 18; i++) { var extra = new RibbonGroup { Id = "g" + i, Text = "Group " + i }; extra.Items.Add(new RibbonButtonItem { Id = "c" + i, Text = "Command " + i }); tab.Groups.Add(extra); }
        ribbon.Tabs.Add(tab); ribbon.CommandCatalog.CaptureDefaults(); ribbon.Backstage.Items.Add(new RibbonButtonItem { Text = "Save", KeyTip = "SV" });
        form.Controls.Add(new WF.RichTextBox { Dock = WF.DockStyle.Fill, Text = "Application document" }); form.Controls.Add(ribbon); int clicks = 0; toggle.Click += (_, _) => clicks++;
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(250);
                foreach (var density in Enum.GetValues<RibbonDensity>())
                {
                    if (onlyDensity is not null && onlyDensity != density) continue;
                    ribbon.Density = density; ribbon.LayoutMode = RibbonLayoutMode.Classic; record.ComboHeight = 0; ribbon.Refresh(); await Task.Delay(35); int normalHeight = record.ComboHeight;
                    Check(normalHeight > 0, "Classic comparison field is actually painted before measuring its Simplified height");
                    ribbon.LayoutMode = RibbonLayoutMode.Simplified; ribbon.Refresh(); await Task.Delay(30);
                    Check(record.ComboHeight > normalHeight, "Simplified " + density + " ComboBox grows within the existing row");
                    Check(record.More.Right == ribbon.Width - S(ribbon.Theme.BodyMargin.Right) - S(4), "More Commands target is aligned to the body's right inset");
                    var native = Descendants(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == combo.Id); nint handle = native.Handle;
                    Capture(density + "-row"); await Click(ribbon, record.More); var more = Popup(); more.Refresh(); await Task.Delay(20);
                    await Click(more, record.Entries[group]); var full = Popup(); ribbon.Refresh(); full.Refresh(); await Task.Delay(20);
                    Check(record.Collapsed.TryGetValue(group, out var button) && button.Height < S(55) && button.Width > button.Height, "Open full group has a medium inline group button");
                    Check(native.Handle == handle && Descendants(full).Contains(native), "Full group borrows the same native editor"); Capture(density + "-borrowed-group");
                    combo.Value = "Changed in popup"; full.Refresh(); int before = clicks; await Click(full, record.Commands[toggle]); ribbon.Refresh(); await Task.Delay(30);
                    Check(clicks == before + 1 && toggle.Checked && !record.Collapsed.ContainsKey(group), "Toggle action restores the live row with its latest checked state");
                    Check(native.Handle == handle && Descendants(ribbon).Contains(native) && combo.Value == "Changed in popup", "Closing restores native editor identity and changed value"); Capture(density + "-restored-row"); toggle.Checked = false;
                    if (groupsOnly)
                    {
                        await Click(ribbon, record.More); more = Popup(); more.Refresh(); await Click(more, record.Entries[group]); ribbon.Refresh();
                        await Click(ribbon, record.Collapsed[group]); ribbon.Refresh();
                        Check(!record.Collapsed.ContainsKey(group) && Descendants(ribbon).Contains(native), "Clicking the transient group button dismisses and restores the row");
                        continue;
                    }

                    ribbon.IsBackstageOpen = true; await Task.Delay(30); WF.SendKeys.SendWait("{F10}"); await Task.Delay(30); form.Refresh();
                    Check(record.Badges > 0 && record.FittingBadges, "Backstage letter badges fit the actual navigation font at native DPI"); Capture(density + "-backstage-letters");
                    ribbon.IsBackstageOpen = false; ribbon.LayoutMode = RibbonLayoutMode.Classic;
                    using var options = new RibbonQuickAccessCustomizationForm(ribbon, "review.followup"); options.SelectedPage = options.Pages.Single(p => p.Text == "Customize Ribbon"); options.Show(form); SetForegroundWindow(options.Handle); await Task.Delay(70);
                    var tree = (WF.TreeView)Descendants(options.RibbonEditor).Single(c => c.Name == "RibbonHierarchy"); tree.TopNode = tree.Nodes[0].Nodes[8]; string top = tree.TopNode.Text; nint treeHandle = tree.Handle;
                    bool callback = false; int paints = 0, applied = 0; tree.DrawNode += (_, _) => { if (callback) paints++; };
                    options.SelectedPage!.Applied += (_, _) => { applied++; callback = true; tree.Refresh(); tree.Update(); callback = false; };
                    ((WF.Button)Descendants(options).Single(c => c.Name == "ApplyCustomization")).PerformClick();
                    Check(applied == 1 && paints == 0 && tree.Handle == treeHandle && tree.TopNode.Text == top, "Apply holds tree redraw through host callbacks and retains its scroll position/handle");
                    Capture(density + "-options-applied", options);
                    ((WF.Button)Descendants(options).Single(c => c.Name == "AcceptCustomization")).PerformClick();
                    Check(applied == 2 && paints == 0 && !options.Visible, "OK closes before the held tree can repaint"); SetForegroundWindow(form.Handle); await Task.Delay(50);
                }
                log.Add($"PASS: {(neutral ? "neutral" : "Office2024")} follow-up at actual {ribbon.DeviceDpi} DPI");
            }
            catch (Exception e) { error = e; log.Add(e.ToString()); }
            finally { WF.Cursor.Position = cursor; File.WriteAllLines(Path.Combine(directory, "result.log"), log); form.Close(); }
        };
        WF.Application.Run(form); foreach (var line in log) Console.WriteLine(line); return error is null ? 0 : 1;
        int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
        void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException(text); log.Add("PASS: " + text); }
        async Task Click(WF.Control control, Rectangle bounds) { if (bounds.IsEmpty) throw new InvalidOperationException("Missing pointer target"); WF.Cursor.Position = control.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(25); mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(60); }
        void Capture(string name, WF.Form? target = null) { target ??= form; target.Refresh(); using var image = new Bitmap(target.Width, target.Height); using var g = Graphics.FromImage(image); g.CopyFromScreen(target.Location, Point.Empty, target.Size); image.Save(Path.Combine(directory, name + ".png")); }
    }
    private static IEnumerable<WF.Control> Descendants(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static WF.Control Popup() { WF.ToolStripDropDown? first = null; EnumThreadWindows(GetCurrentThreadId(), (h, _) => { if (WF.Control.FromHandle(h) is WF.ToolStripDropDown { Visible: true } popup) first ??= popup; return true; }, 0); return first?.Items.OfType<WF.ToolStripControlHost>().Single().Control ?? throw new InvalidOperationException("Expected popup"); }
    private sealed class Icon : IRibbonIcon { public void Draw(Graphics g, Rectangle b, RibbonIconContext c) { using var p = new Pen(c.Foreground, 2); g.DrawRectangle(p, Rectangle.Inflate(b, -3, -3)); } }
    private sealed class Recording
    {
        internal Rectangle More; internal int ComboHeight, Badges; internal bool FittingBadges = true;
        internal readonly Dictionary<RibbonElement, Rectangle> Entries = [], Commands = [], Collapsed = [];
        internal void Entry(RibbonPaintContext c, RibbonElement entry, bool menu) { if (menu && entry is RibbonQuickAccessGroupReference { Source: { } source }) Entries[source] = c.Bounds; }
        internal void Badge(RibbonPaintContext c, string keys) { Badges++; var size = WF.TextRenderer.MeasureText(keys, c.Font); FittingBadges &= c.Bounds.Width >= size.Width && c.Bounds.Height >= size.Height + 2; }
    }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawSurface(RibbonPaintContext c, bool popup) { if (!popup) r.Collapsed.Clear(); base.DrawSurface(c, popup); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.More = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement e, bool menu) { r.Entry(c, e, menu); base.DrawQuickAccessItem(c, e, menu); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem i, RibbonItemSize s, bool menu) { r.Commands[i] = c.Bounds; base.DrawItem(c, i, s, menu); }
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup g, bool collapsed) { if (collapsed) r.Collapsed[g] = c.Bounds; base.DrawGroup(c, g, collapsed); }
        public override void DrawComboBox(RibbonPaintContext c, RibbonComboBoxItem i, Rectangle field) { r.ComboHeight = field.Height; base.DrawComboBox(c, i, field); }
        public override void DrawKeyTip(RibbonPaintContext c, string keys) { r.Badge(c, keys); base.DrawKeyTip(c, keys); }
    }
    private sealed class Neutral(Recording r) : RibbonRenderer
    {
        public override void DrawSurface(RibbonPaintContext c, bool popup) { if (!popup) r.Collapsed.Clear(); base.DrawSurface(c, popup); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.More = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement e, bool menu) { r.Entry(c, e, menu); base.DrawQuickAccessItem(c, e, menu); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem i, RibbonItemSize s, bool menu) { r.Commands[i] = c.Bounds; base.DrawItem(c, i, s, menu); }
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup g, bool collapsed) { if (collapsed) r.Collapsed[g] = c.Bounds; base.DrawGroup(c, g, collapsed); }
        public override void DrawComboBox(RibbonPaintContext c, RibbonComboBoxItem i, Rectangle field) { r.ComboHeight = field.Height; base.DrawComboBox(c, i, field); }
        public override void DrawKeyTip(RibbonPaintContext c, string keys) { r.Badge(c, keys); base.DrawKeyTip(c, keys); }
    }
}
