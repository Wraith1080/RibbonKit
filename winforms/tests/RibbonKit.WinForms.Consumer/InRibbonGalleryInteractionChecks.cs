using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public models, renderer geometry and native input only. This fixture also ships
// with the independent consumer; it has no dependency on Showcase or internals.
internal static class InRibbonGalleryInteractionChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    internal static int Run(string directory, bool flat = false, bool qatOnly = false, bool stripOnly = false, bool reductionOnly = false)
    {
        directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        var record = new Recording(); using RibbonRenderer painter = flat ? new Flat(record) : new Office(record);
        using var form = new WF.Form { Text = "Independent in-ribbon and QAT gallery consumer", Width = 1100, Height = 570, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = painter };
        var home = new RibbonTab { Text = "Home", KeyTip = "H" }; var other = new RibbonTab { Text = "Other", KeyTip = "O" }; var group = new RibbonGroup { Text = "Styles" };
        var gallery = new RibbonGalleryItem { Text = "Styles", KeyTip = "G", IsInRibbon = true, InRibbonColumnCount = 3, ColumnCount = 4, TileWidth = 96, TileHeight = 76, MaxVisibleRows = 2 };
        var tiles = Enumerable.Range(0, 12).Select(i => new RibbonGalleryTile { Text = "Style " + i, GroupName = i < 6 ? "Document" : "More styles", Enabled = i != 1 }).ToArray();
        gallery.Items.AddRange(tiles); gallery.SelectedItem = tiles[4];
        var field = new RibbonTextBoxItem { Id = "retained.name", Text = "Name", Value = "Retained native editor" };
        group.Items.AddRange(gallery, field); home.Groups.Add(group); ribbon.Tabs.AddRange(home, other);
        var first = new RibbonQuickAccessDropDownReference { Source = gallery }; var second = new RibbonQuickAccessDropDownReference { Source = gallery, Text = "Same styles" };
        ribbon.QuickAccessItems.AddRange(first, second);
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "The gallery strip and QAT share one source. Preview is temporary; only activation commits. The application owns this document." };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        RibbonGalleryTile? preview = null; int commits = 0, cancels = 0, unavailable = 0;
        gallery.ItemPreview += (_, e) => preview = e.Item;
        gallery.ItemPreviewCancelled += (_, _) => { preview = null; cancels++; };
        gallery.SelectionCommitted += (_, _) => { preview = null; commits++; };
        var log = new List<string>(); Exception? error = null; bool completed = false; var cursor = WF.Cursor.Position;
        EventHandler<FirstChanceExceptionEventArgs> exceptions = (_, e) => { if (e.Exception is COMException { HResult: unchecked((int)0x80040201) }) unavailable++; };
        AppDomain.CurrentDomain.FirstChanceException += exceptions;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle);
                for (int i = 0; i < 20 && GetForegroundWindow() != form.Handle; i++) await Task.Delay(50);
                Guard(); document.Focus(); await MoveToDocument(); var strip = Strip(ribbon); nint stripHandle = strip.Handle; Read(strip);
                if (reductionOnly)
                {
                    group.ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse;
                    gallery.SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small); gallery.Icon = new FallbackIcon(); var policy = gallery.SizePolicy;
                    foreach (var density in Enum.GetValues<RibbonDensity>())
                    {
                        ribbon.Density = density; form.Width = 1100; record.Fallback = Rectangle.Empty; ribbon.Refresh(); var full = record.Gallery;
                        Check(strip.Visible && record.Fallback.IsEmpty, $"{density}: wide gallery uses its strip");
                        for (int width = 1080; width >= 260; width -= 10)
                        {
                            form.Width = width; record.Fallback = Rectangle.Empty; ribbon.Refresh();
                            if (!record.Fallback.IsEmpty) break;
                        }
                        Check(!record.Fallback.IsEmpty && record.GallerySize == RibbonItemSize.Large && record.Fallback.Height == full.Height && record.Fallback.Width < full.Width, $"{density}: reduced gallery keeps a full-height Large dropdown");
                        Check(!strip.Visible && !strip.IsDisposed && gallery.IsInRibbon && gallery.SizePolicy == policy, "Reduction parks the strip without replacing authored gallery state");
                        await Capture(density + "-large-fallback"); await Click(ribbon, record.Fallback); Check(gallery.IsDropDownOpen, "Pointer activation opens the source gallery from its Large fallback");
                        int beforeCommit = commits; await Key("{END}{ENTER}"); Check(commits == beforeCommit + 1 && gallery.SelectedItem == tiles[11] && !gallery.IsDropDownOpen, "Fallback keyboard commits the source tile exactly once");
                        form.Width = 1100; ribbon.Refresh(); Check(ReferenceEquals(strip, Strip(ribbon)) && strip.Handle == stripHandle && gallery.SelectedItem == tiles[11], "Growing restores the same strip HWND and selected tile");
                    }
                    Check(unavailable == 0, "Gallery reduction input raises zero unavailable-element exceptions");
                    log.Add($"PASS: {(flat ? "Neutral" : "Office")} gallery reduction at {form.DeviceDpi} DPI"); completed = true; return;
                }
                if (!qatOnly)
                {
                Check(record.Tiles.Keys.OrderBy(t => Array.IndexOf(tiles, t)).SequenceEqual(tiles.Skip(3).Take(3)), "Strip initially reveals the source selection's row");
                await Hover(strip, record.Tiles[tiles[3]]); Check(preview == tiles[3] && gallery.SelectedItem == tiles[4] && commits == 0, $"Strip pointer preview leaves source selection untouched (preview={preview?.Text}, selected={gallery.SelectedItem?.Text}, commits={commits}, cancels={cancels})");
                await Capture("strip-preview"); await Click(strip, record.Tiles[tiles[3]]);
                Check(gallery.SelectedItem == tiles[3] && commits == 1 && preview is null, "Strip click commits the original source once");
                await MoveToDocument(); Read(strip); await Click(strip, record.Buttons[RibbonGalleryStripButton.Down]);
                Read(strip); Check(record.Tiles.ContainsKey(tiles[6]) && gallery.SelectedItem == tiles[3], "Down scrolls a realized row without selecting");
                await Click(strip, record.Buttons[RibbonGalleryStripButton.Expand]); Check(gallery.IsDropDownOpen, "Strip expand opens the shared gallery popup");
                await Capture("expanded-heading"); await Key("{HOME}{RIGHT}"); Check(preview == tiles[2] && gallery.SelectedItem == tiles[3], "Expanded keyboard preview skips disabled tiles");
                await Key("{ESC}"); Read(strip);
                Check(!gallery.IsDropDownOpen && record.Tiles.ContainsKey(tiles[6]) && preview is null && strip.Focused, "Cancel restores strip focus and retains the browsed row");
                await Key("{HOME}{RIGHT}"); Check(preview == tiles[2] && gallery.SelectedItem == tiles[3], "Strip keyboard preview skips disabled tiles without committing");
                await Key("{END}{ENTER}"); Check(gallery.SelectedItem == tiles[11] && commits == 2 && preview is null, "Strip End and Enter commit the last tile once");
                await MoveToDocument();
                for (int cycle = 0; cycle < 2; cycle++)
                {
                    Read(strip); Check(!record.Available[RibbonGalleryStripButton.Down], "Down is disabled at the last row");
                    for (int i = 0; i < 5; i++) { Read(strip); await Click(strip, record.Buttons[RibbonGalleryStripButton.Up]); }
                    Read(strip); Check(record.Tiles.ContainsKey(tiles[0]) && !record.Available[RibbonGalleryStripButton.Up], "Repeated upward scrolling clamps at the first row");
                    for (int i = 0; i < 5; i++) { Read(strip); await Click(strip, record.Buttons[RibbonGalleryStripButton.Down]); }
                    Read(strip); Check(record.Tiles.ContainsKey(tiles[11]) && !record.Available[RibbonGalleryStripButton.Down], "Direction reversal returns to the last realized row");
                }
                Check(commits == 2 && gallery.SelectedItem == tiles[11], "Scroll endpoints and reversals do not commit");
                if (stripOnly) { log.Add($"PASS: {(flat ? "Neutral" : "Office")} gallery strip at {form.DeviceDpi} DPI"); completed = true; return; }
                }
                else gallery.SelectedItem = tiles[11];
                int commitBaseline = commits;
                await MoveToDocument(); ribbon.Refresh(); await Click(ribbon, record.Entries[first]); Check(first.IsDropDownOpen && strip.Visible, "QAT opens the same gallery while retaining the source strip");
                await Key("{ESC}"); ribbon.Refresh(); await Click(ribbon, record.Entries[second]);
                Check(second.IsDropDownOpen && !first.IsDropDownOpen && gallery.Items.Cast<RibbonGalleryTile>().SequenceEqual(tiles), "A second reference borrows the original tile collection");
                await Key("{ESC}"); ribbon.SelectedTab = other; ribbon.Refresh();
                Check(!strip.Visible && !strip.IsDisposed, "Inactive-tab strip is parked without disposal");
                await Click(ribbon, record.Entries[first]); await Key("{HOME}{ENTER}");
                Check(commits == commitBaseline + 1 && gallery.SelectedItem == tiles[0] && ribbon.SelectedTab == other && !first.IsDropDownOpen, "Inactive-tab QAT commits the source without selecting its tab");
                ribbon.QuickAccessMaxWidth = 28; ribbon.Refresh(); await Click(ribbon, record.Overflow); var overflow = Popup(); overflow.Refresh();
                await Click(overflow, record.MenuEntries[first]); await Capture("overflow-gallery"); await Key("{END}{ENTER}");
                Check(commits == commitBaseline + 2 && gallery.SelectedItem == tiles[11] && !overflow.Visible && !first.IsDropDownOpen, "Overflow gallery commit dismisses both popup levels once");
                ribbon.SelectedTab = home; ribbon.QuickAccessMaxWidth = 240; ribbon.Refresh();
                Check(ReferenceEquals(strip, Strip(ribbon)) && strip.Handle == stripHandle, "Reactivating the source reuses its strip presenter and HWND");
                var editor = Tree(ribbon).OfType<WF.TextBox>().Single(c => c.Name == field.Id); nint editorHandle = editor.Handle; editor.Select(2, 3);
                var groupReference = new RibbonQuickAccessGroupReference { Source = group }; ribbon.QuickAccessItems.Add(groupReference); ribbon.Refresh();
                Check(!strip.Visible, "A QAT group reference constrains its source to collapsed presentation");
                await Click(ribbon, record.Entries[groupReference]); var groupPopup = Popup();
                Check(ReferenceEquals(strip, Strip(groupPopup)) && strip.Handle == stripHandle && Tree(groupPopup).Contains(editor) && editor.Handle == editorHandle, "Borrowed group reuses one strip and its native editor");
                await Capture("borrowed-group"); await Key("{ESC}"); ribbon.QuickAccessItems.Remove(groupReference); ribbon.Refresh();
                Check(ReferenceEquals(strip, Strip(ribbon)) && !strip.IsDisposed && editor.Handle == editorHandle && editor.SelectionStart == 2 && editor.SelectionLength == 3, "Closing and removing the group reference restores strip and editor state");
                groupReference.Dispose(); first.Dispose(); Check(!gallery.IsDisposed && gallery.Items.Count == 12, "Disposing borrowed references leaves the gallery alive");
                strip.Focus(); await Key("{HOME}"); int before = cancels; gallery.Visible = false;
                Check(preview is null && cancels == before + 1 && !strip.Visible && !strip.IsDisposed, "Hiding a source cancels its preview once and parks its strip");
                gallery.Visible = true; ribbon.Refresh(); gallery.Dispose(); Check(strip.IsDisposed, "Source disposal releases its reusable presenter");
                Check(unavailable == 0, "Strip, QAT and borrowed-group input raise zero unavailable-element exceptions");
                log.Add($"PASS: {(flat ? "Neutral" : "Office")} {(qatOnly ? "QAT gallery scope" : "in-ribbon/QAT gallery")} at {form.DeviceDpi} DPI"); completed = true;
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
        void Read(WF.Control c) { record.Tiles.Clear(); c.Refresh(); }
        async Task Key(string keys) { Guard(); WF.SendKeys.SendWait(keys); await Task.Delay(100); }
        async Task MoveToDocument() { WF.Cursor.Position = document.PointToScreen(new(25, 45)); await Task.Delay(70); }
        async Task Hover(WF.Control c, Rectangle bounds) { Guard(); WF.Cursor.Position = c.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(120); }
        async Task Click(WF.Control c, Rectangle bounds)
        {
            bounds = Rectangle.Intersect(c.ClientRectangle, bounds); Check(!bounds.IsEmpty, "Pointer target is visible");
            await Hover(c, bounds); Guard(); mouse_event(2, 0, 0, 0, 0); await Task.Delay(25); mouse_event(4, 0, 0, 0, 0); await Task.Delay(90);
        }
        async Task Capture(string name)
        {
            await Task.Delay(60); Guard(); var bounds = form.Bounds;
            if (WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup) bounds = Rectangle.Union(bounds, popup.Bounds);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private static IEnumerable<WF.Control> Tree(WF.Control c) => c.Controls.Cast<WF.Control>().SelectMany(child => new[] { child }.Concat(Tree(child)));
    private static WF.Control Strip(WF.Control parent) => Tree(parent).Single(c => c.AccessibleName == "Styles gallery strip");
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected a library popup.");
    private sealed class Recording
    {
        internal readonly Dictionary<RibbonGalleryTile, Rectangle> Tiles = [];
        internal readonly Dictionary<RibbonGalleryStripButton, Rectangle> Buttons = [];
        internal readonly Dictionary<RibbonGalleryStripButton, bool> Available = [];
        internal readonly Dictionary<RibbonElement, Rectangle> Entries = [], MenuEntries = [];
        internal Rectangle Overflow, Gallery, Fallback;
        internal RibbonItemSize GallerySize;
        internal void Button(RibbonPaintContext c, RibbonGalleryStripButton button, bool enabled) { Buttons[button] = c.Bounds; Available[button] = enabled; }
        internal void Entry(RibbonPaintContext c, RibbonElement entry, bool menu) => (menu ? MenuEntries : Entries)[entry] = c.Bounds;
    }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { if (item is RibbonGalleryItem) { r.Gallery = c.Bounds; r.GallerySize = size; if (c.IsGalleryDropDown) r.Fallback = c.Bounds; } base.DrawItem(c, item, size, menu); }
        public override void DrawGalleryTile(RibbonPaintContext c, RibbonGalleryTile tile, bool selected) { r.Tiles[tile] = c.Bounds; base.DrawGalleryTile(c, tile, selected); }
        public override void DrawGalleryStripButton(RibbonPaintContext c, RibbonGalleryStripButton button, bool enabled) { r.Button(c, button, enabled); base.DrawGalleryStripButton(c, button, enabled); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Entry(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
    }
    private sealed class Flat(Recording r) : RibbonRenderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { if (item is RibbonGalleryItem) { r.Gallery = c.Bounds; r.GallerySize = size; if (c.IsGalleryDropDown) r.Fallback = c.Bounds; } base.DrawItem(c, item, size, menu); }
        public override void DrawGalleryTile(RibbonPaintContext c, RibbonGalleryTile tile, bool selected) { r.Tiles[tile] = c.Bounds; base.DrawGalleryTile(c, tile, selected); }
        public override void DrawGalleryStripButton(RibbonPaintContext c, RibbonGalleryStripButton button, bool enabled) { r.Button(c, button, enabled); base.DrawGalleryStripButton(c, button, enabled); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Entry(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
    }
    private sealed class FallbackIcon : IRibbonIcon { public void Draw(Graphics g, Rectangle bounds, RibbonIconContext context) { using var pen = new Pen(Color.SteelBlue, Math.Max(1, bounds.Width / 16f)); var rect = Rectangle.Inflate(bounds, -3, -3); g.DrawRectangle(pen, rect); } }
}
