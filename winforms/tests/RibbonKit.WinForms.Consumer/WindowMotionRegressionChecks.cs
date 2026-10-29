using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class WindowMotionRegressionChecks
{
    internal static int Run(string directory, string scope, bool neutral)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); List<string> log = []; Exception? failure = null; bool completed = false;
        var record = new Recording(); using RibbonRenderer painter = neutral ? new Neutral(record) : new Office(record);
        using var form = new RibbonForm { Text = "Independent window motion regression", Size = new(1200, 720), StartPosition = WF.FormStartPosition.CenterScreen };
        using var backdrop = new PaintBackdrop { FormBorderStyle = WF.FormBorderStyle.None, ShowInTaskbar = false, BackColor = Color.FromArgb(41, 57, 73), StartPosition = WF.FormStartPosition.Manual };
        var ribbon = new RibbonControl { Renderer = painter, QuickAccessPosition = RibbonQuickAccessPosition.TitleBar };
        var tab = new RibbonTab { Id = "home", Text = "Home" }; var otherTab = new RibbonTab { Id = "insert", Text = "Insert" };
        var group = new RibbonGroup { Text = "Commands", ReductionPriority = 10 };
        var combo = new RibbonComboBoxItem { Id = "font", Text = "Font", IsEditable = true, Value = "Retained", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var bold = new RibbonToggleItem { Id = "bold", Text = "Bold", Icon = new Icon(), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        group.Items.AddRange(combo, bold, new RibbonButtonItem { Text = "Copy", Icon = new Icon(), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }); tab.Groups.Add(group);
        var fixedGroup = new RibbonGroup { Text = "Other", CanResize = false }; fixedGroup.Items.Add(new RibbonButtonItem { Text = "Another command", Icon = new Icon() }); tab.Groups.Add(fixedGroup);
        var insert = new RibbonGroup { Text = "Insert commands" }; insert.Items.Add(new RibbonButtonItem { Text = "Picture", Icon = new Icon() }); otherTab.Groups.Add(insert);
        ribbon.Tabs.AddRange(tab, otherTab); ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = bold }); ribbon.FileMenu.Visible = true; ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
        using var info = new WF.Panel { BackColor = Color.White }; using var print = new WF.Panel { BackColor = Color.White };
        info.Controls.Add(new WF.TextBox { Location = new(35, 35), Width = 300, Text = "Retained application content" });
        print.Controls.Add(new WF.TextBox { Location = new(35, 35), Width = 300, Text = "Print settings remain native" });
        info.Controls.Add(new WF.Label { Location = new(35, 85), Width = 450, Text = "Repeated live page fades must remain steady after completion." });
        var firstPage = new RibbonBackstagePageItem { Text = "Info", Content = info }; var secondPage = new RibbonBackstagePageItem { Text = "Print", Content = print }; ribbon.Backstage.Items.AddRange(firstPage, secondPage);
        form.Controls.Add(new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Application-owned document" }); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        var pointer = WF.Cursor.Position;
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(150); Guard();
                if (scope is "frame" or "minimize")
                {
                    // Invisible native resize borders include the desktop behind
                    // the Form. A moving video there is not a frame regression.
                    backdrop.Bounds = Rectangle.Inflate(form.Bounds, 16, 16); backdrop.Show();
                    SetWindowPos(backdrop.Handle, form.Handle, 0, 0, 0, 0, 0x13); await Task.Delay(60); Guard();
                }
                log.Add($"Actual DPI={form.DeviceDpi}; scope={scope}; renderer={(neutral ? "neutral" : "Office")}");
                WF.Cursor.Position = new(form.Right + 15, form.Top + 150);
                if (scope == "groups") await Groups();
                else if (scope == "backstage") await Backstage();
                else if (scope == "input") await Input();
                else await Frame(scope == "minimize");
                completed = true;
            }
            catch (Exception e) { failure = e; log.Add("FAIL: " + e); }
            finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); form.Close(); }
        };
        WF.Application.Run(form); WF.Cursor.Position = pointer;
        return failure is null && completed ? 0 : 1;

        async Task Frame(bool minimize = false)
        {
            foreach (var density in Enum.GetValues<RibbonDensity>())
            {
                ribbon.Density = density; ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsMinimized = false; ribbon.SelectedTab = tab; ribbon.Refresh(); await Task.Delay(60);
                var bounds = form.Bounds; int captionBottom = record.Tab.Top + form.PointToScreen(Point.Empty).Y - form.Top;
                var headerProbe = Rectangle.Inflate(record.Tab, -5, -10); var ribbonOrigin = ribbon.PointToScreen(Point.Empty);
                headerProbe.Offset(ribbonOrigin.X - form.Left, ribbonOrigin.Y - form.Top);
                using var before = Capture(bounds); var frames = new List<Bitmap>();
                try
                {
                    ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; if (minimize) ribbon.IsMinimized = true; else ribbon.SelectedTab = otherTab;
                    Check(ribbon.IsAnimating, minimize ? "Minimize motion starts in RibbonForm" : "Tab motion starts in RibbonForm");
                    for (int i = 0; i < 18; i++) { frames.Add(Capture(bounds)); await Task.Delay(12); }
                    await Finish(); using var after = Capture(bounds);
                    before.Save(Path.Combine(directory, density + "-caption-before.png")); after.Save(Path.Combine(directory, density + "-caption-after.png"));
                    for (int i = 0; i < frames.Count; i++) frames[i].Save(Path.Combine(directory, $"{density}-caption-{i:D2}.png"));
                    int mismatches = frames.Max(frame => EdgeDifferences(before, frame, captionBottom));
                    log.Add($"{density}: maximum caption/frame edge changed pixels={mismatches}");
                    Check(mismatches <= 4, "Native caption corners and separator remain unchanged throughout motion");
                    Check(frames.All(frame => Ink(frame, headerProbe) > 5), "Native tab labels remain visible inside their measured header band");
                }
                finally { foreach (var frame in frames) frame.Dispose(); }
            }
        }
        async Task Input()
        {
            int clicks = 0; bold.Click += (_, _) => clicks++;
            ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.SelectedTab = otherTab;
            Check(ribbon.IsAnimating, "Caption input test begins during motion");
            Guard(); var bounds = record.QuickAccess; WF.Cursor.Position = ribbon.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2));
            await Task.Delay(20); Guard(); mouse_event(2, 0, 0, 0, 0); await Task.Delay(35); Guard(); mouse_event(4, 0, 0, 0, 0); await Task.Delay(80);
            Check(clicks == 1 && bold.Checked, "Native pointer passes through the child drawing plane and invokes QAT once");
            Check(!ribbon.IsAnimating, "Real input settles motion");
        }
        async Task Groups()
        {
            foreach (var density in Enum.GetValues<RibbonDensity>())
            foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
            foreach (var style in Enum.GetValues<RibbonTabContentAnimation>())
            {
                ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.Density = density; ribbon.LayoutMode = mode; ribbon.TabContentAnimation = style; form.Width = 1200; ribbon.Refresh();
                var editor = Tree(ribbon).OfType<WF.ComboBox>().Single(); nint handle = editor.Handle;
                using var edit = ribbon.CommandCatalog.BeginCustomization("window-motion"); string profile = edit.ExportJson();
                int narrow = 0;
                for (int width = 1000; width >= 220; width -= 20) { form.Width = width; ribbon.Refresh(); if (mode == RibbonLayoutMode.Classic ? record.Collapsed : !editor.Visible) { narrow = width; break; } }
                Check(narrow > 0, "Resize threshold exists"); form.Width = 1200; ribbon.Refresh();
                ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; form.Width = narrow;
                Check(ribbon.IsAnimating, $"{density}/{mode}/{style}: collapse starts on host width change");
                form.Width -= 2; Check(ribbon.IsAnimating, "Continued width change retains playback"); await Task.Delay(25);
                using (var frame = Capture(form.Bounds)) frame.Save(Path.Combine(directory, $"{density}-{mode}-{style}-collapse.png")); await Finish();
                form.Width = 1200; Check(ribbon.IsAnimating, "Group restoration starts on host expansion"); await Finish();
                Check(editor.Handle == handle && combo.Value == "Retained" && edit.ExportJson() == profile, "Width motion retains native handle, value and profile");
            }
        }
        async Task Backstage()
        {
            ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.IsBackstageOpen = true; nint handle = info.Controls[0].Handle;
            await Finish();
            foreach (var page in new[] { secondPage, firstPage, secondPage, firstPage })
            {
                ribbon.Backstage.SelectedPage = page; Check(ribbon.IsAnimating, "Live page fade starts");
                var pageBounds = page.Content!.RectangleToScreen(page.Content.ClientRectangle); var probe = new Rectangle(pageBounds.Left + 15, pageBounds.Top + 15, Math.Min(500, pageBounds.Width - 20), Math.Min(130, pageBounds.Height - 20));
                var frames = new List<(bool Animating, Bitmap Image)>();
                try
                {
                    var watch = Stopwatch.StartNew();
                    while (watch.ElapsedMilliseconds < 450) { Guard(); frames.Add((ribbon.IsAnimating, Capture(probe))); await Task.Delay(10); }
                    int index = log.Count;
                    for (int i = 0; i < frames.Count; i++) frames[i].Image.Save(Path.Combine(directory, $"page-{index}-{i:D2}-{frames[i].Animating}.png"));
                    var settled = frames.Where(f => !f.Animating).Select(f => f.Image).ToArray();
                    Check(settled.Length >= 2, "Page fade completes autonomously");
                    int changed = settled.Max(frame => Differences(settled[^1], frame)); log.Add($"Post-fade changed page pixels={changed}");
                    Check(changed <= 4, "Page pixels remain steady across fade completion");
                }
                finally { foreach (var frame in frames) frame.Image.Dispose(); }
            }
            Check(info.Controls[0].Handle == handle && info.Controls[0].Text == "Retained application content", "Repeated fades retain native page state");
            ribbon.IsBackstageOpen = false; await Finish();
        }
        async Task Finish() { var watch = Stopwatch.StartNew(); while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1500) await Task.Delay(5); Check(!ribbon.IsAnimating, "Motion completes without host acknowledgement"); }
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        void Guard() { GetWindowThreadProcessId(GetForegroundWindow(), out uint process); if (process != Environment.ProcessId) throw new InvalidOperationException("Foreground ownership lost; input/capture stopped."); }
        Bitmap Capture(Rectangle bounds) { Guard(); var image = new Bitmap(bounds.Width, bounds.Height); using var g = Graphics.FromImage(image); g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); return image; }
    }
    private static int Ink(Bitmap frame, Rectangle bounds)
    { int count = 0; for (int y = bounds.Top; y < bounds.Bottom; y++) for (int x = bounds.Left; x < bounds.Right; x++) if (frame.GetPixel(x, y).GetBrightness() < .8) count++; return count; }
    private static int EdgeDifferences(Bitmap first, Bitmap next, int captionBottom)
    {
        int count = 0;
        for (int y = 0; y < first.Height; y++) for (int x = 0; x < first.Width; x++)
            if (y < 7 || x < 8 || x >= first.Width - 8 || y >= first.Height - 2 || y == captionBottom - 1)
            { var a = first.GetPixel(x, y); var b = next.GetPixel(x, y); if (Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) > 24) count++; }
        return count;
    }
    private static int Differences(Bitmap first, Bitmap next)
    { int count = 0; for (int y = 0; y < first.Height; y++) for (int x = 0; x < first.Width; x++) if (first.GetPixel(x, y) != next.GetPixel(x, y)) count++; return count; }
    private static IEnumerable<WF.Control> Tree(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Tree(c)));
    private sealed class Icon : IRibbonIcon { public void Draw(Graphics g, Rectangle bounds, RibbonIconContext context) { using var brush = new SolidBrush(context.Foreground); g.FillEllipse(brush, bounds); } }
    private sealed class PaintBackdrop : WF.Form { protected override bool ShowWithoutActivation => true; }
    private sealed class Recording { internal Rectangle Tab, QuickAccess; internal bool Collapsed; }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { if (!menu) r.QuickAccess = c.Bounds; base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tab = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup group, bool collapsed) { if (group.Text == "Commands") r.Collapsed = collapsed; base.DrawGroup(c, group, collapsed); }
    }
    private sealed class Neutral(Recording r) : RibbonRenderer
    {
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { if (!menu) r.QuickAccess = c.Bounds; base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tab = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup group, bool collapsed) { if (group.Text == "Commands") r.Collapsed = collapsed; base.DrawGroup(c, group, collapsed); }
    }
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
}
