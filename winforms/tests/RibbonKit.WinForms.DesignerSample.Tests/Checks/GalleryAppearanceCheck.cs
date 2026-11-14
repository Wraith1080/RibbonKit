using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class GalleryAppearanceCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop; using var painter = new Probe();
            using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1300, 700), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None };
            var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Galleries", CanResize = false };
            var popup = Make("Popup", false); var inline = Make("Inline", true); var custom = Make("Custom", true); var made = new List<RetainedView>();
            custom.InRibbonPresenterFactory = () => { var view = new RetainedView(); made.Add(view); return view; };
            group.Items.AddRange(popup, inline, custom); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
            Require(made.Count == 1, "Permanent inline presenter was not created once."); var view = made[0]; nint handle = view.Editor.Handle; var parent = view.Editor.Parent; view.Editor.Select(2, 5);
            var original = ribbon.Theme; var selected = popup.SelectedItem; int commits = 0, previews = 0, cancels = 0, activeChecks = 0, states = 0;
            custom.SelectionCommitted += (_, _) => commits++; custom.ItemPreview += (_, _) => previews++; custom.ItemPreviewCancelled += (_, _) => cancels++;
            var settings = new RibbonGalleryAppearanceSettings { TileCornerRadius = 10, TileInset = 3, TileBorderWidth = 2, TileBorderColor = Color.Navy,
                HeaderCornerRadius = 8, HeaderInset = 2, HeaderBorderWidth = 2, HeaderBorderColor = Color.DarkGreen, HeaderBackground = new(Color.LightYellow), HeaderBold = false };
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original; Open(); var before = Snapshot(Surface()); popup.CloseDropDown();
                ribbon.Theme = original with { GalleryAppearance = settings }; painter.Check = true; painter.Tiles = painter.Headers = painter.Chrome = 0;
                Open(); var after = Snapshot(Surface()); Save("popup", Surface());
                Require(before.Size == after.Size && Same(before.Slots, after.Slots), "Gallery decoration changed measured tile/header slots.");
                Require(painter.Failure is null && painter.Tiles > 0 && painter.Headers > 0 && painter.Chrome > 0, "Actual gallery callbacks failed: " + painter.Failure);
                popup.CloseDropDown(); var beforeInline = Snapshot(ribbon); Save("inline", ribbon); painter.Check = false;
                Require(ReferenceEquals(selected, popup.SelectedItem) && made.Count == 1 && ReferenceEquals(made[0], view) && !view.IsDisposed && view.Editor.Handle == handle && ReferenceEquals(view.Editor.Parent, parent)
                    && view.Editor.Text == "Retained presenter text" && view.Editor.SelectionStart == 2 && view.Editor.SelectionLength == 5, "Theme paint replaced presenter/native/source state.");
                if (view.Context.IsActive)
                {
                    var target = (RibbonGalleryTile)custom.Items[1]; Require(view.Context.Preview(target), "Current presenter could not preview."); Require(view.Context.Preview(null), "Current presenter could not cancel preview.");
                    Require(view.Context.Commit(target), "Current presenter could not commit."); activeChecks++; Require(commits == activeChecks && previews == activeChecks && cancels == activeChecks, "Presenter source events were duplicated.");
                }
                ribbon.Theme = original; var restoredInline = Snapshot(ribbon); Require(beforeInline.Size == restoredInline.Size && Same(beforeInline.Slots, restoredInline.Slots), "Reset changed inline geometry.");
                Open(); var restored = Snapshot(Surface()); Require(before.Size == restored.Size && Same(before.Slots, restored.Slots), "Reset changed popup geometry."); popup.CloseDropDown(); states++;
                void Open() { form.Activate(); ribbon.Focus(); Application.DoEvents(); popup.ShowDropDown(); Require(popup.IsDropDownOpen, "Grouped gallery did not open."); }
                (Size Size, Dictionary<string, Rectangle> Slots) Snapshot(Control control) { painter.Slots.Clear(); using var image = Print(control); return (control.Size, painter.Slots.ToDictionary(pair => pair.Key, pair => pair.Value)); }
                void Save(string kind, Control control) { if (kind == "popup") Popup().Location = new(-16000, -16000); using var image = Print(control); image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-{kind}.png")); }
            }
            Require(activeChecks > 0, "No active presenter contract was checked.");
            ribbon.Theme = original with { GalleryAppearance = settings with { TileCornerRadius = null } }; Require(ribbon.Theme.GalleryAppearance!.HeaderBold == false, "Tile member reset cleared headings."); ribbon.Theme = original;
            Require(ribbon.Theme.GalleryAppearance is null && made.Count == 1 && view.Editor.Handle == handle, "Group reset changed presenter ownership.");
            evidence.Add($"PASS {scope}: effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density states, actual tile/heading/chrome/text callbacks and authored outline/fill/weight, unchanged popup/inline geometry and source selection, retained custom presenter/editor HWND/parent/value/selection, {activeChecks} active presenter preview/cancel/commit sequences, member/group reset."); form.Close();
        }
        evidence.Add("Synchronous offscreen public-package paint/state checks before queued foreground-loss dismissal. External UIA, actual VS, physical input/IME, visible frame pacing and mixed-monitor acceptance remain separate."); File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static RibbonGalleryItem Make(string name, bool inline)
    {
        var gallery = new RibbonGalleryItem { Text = name, IsInRibbon = inline, InRibbonColumnCount = 2, ColumnCount = 3, MaxVisibleRows = 2, TileWidth = 90, TileHeight = 80, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        gallery.Items.AddRange(Enumerable.Range(0, 9).Select(i => new RibbonGalleryTile { Text = name + " style " + i, GroupName = i < 5 ? "Group one" : "Group two", Icon = RibbonStockIcons.Create("save") }).ToArray()); gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0]; return gallery;
    }
    private static bool Same(Dictionary<string, Rectangle> a, Dictionary<string, Rectangle> b) => a.Count == b.Count && a.All(pair => b.TryGetValue(pair.Key, out var value) && value == pair.Value);
    private static Bitmap Print(Control control) { var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, control.ClientRectangle); return image; }
    private static Control Surface() => Popup().Items.OfType<ToolStripControlHost>().Single().Control;
    private static ToolStripDropDown Popup()
    {
        var windows = new List<ToolStripDropDown>(); EnumWindow callback = (handle, _) => { if (Control.FromHandle(handle) is ToolStripDropDown { Visible: true } window) windows.Add(window); return true; };
        EnumThreadWindows(GetCurrentThreadId(), callback, 0); GC.KeepAlive(callback); return windows.Single();
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private delegate bool EnumWindow(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, nint parameter);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private sealed class RetainedView : UserControl, IRibbonGalleryPresenter
    {
        internal readonly TextBox Editor = new() { Text = "Retained presenter text", Bounds = new(8, 8, 140, 24) };
        internal RibbonGalleryPresenterContext Context = null!;
        internal RetainedView() { Size = new(180, 80); Controls.Add(Editor); }
        public override Size GetPreferredSize(Size proposedSize) => new(180, 80);
        public void Synchronize(RibbonGalleryPresenterContext context) => Context = context;
        public Rectangle GetItemBounds(RibbonGalleryTile item) { int index = Context.Gallery.Items.IndexOf(item); return index < 0 ? Rectangle.Empty : new(4 + index % 2 * 80, 38 + index / 2 * 32, 76, 28); }
        public RibbonGalleryTile? HitTest(Point location) => Context.Gallery.Items.OfType<RibbonGalleryTile>().FirstOrDefault(tile => GetItemBounds(tile).Contains(location));
        public RibbonGalleryTile? Navigate(RibbonGalleryTile? current, Keys key) => current;
        public void Reveal(RibbonGalleryTile item) { }
        public void ScrollView(RibbonGalleryScroll amount) { }
        public bool CanScrollBackward => false;
        public bool CanScrollForward => false;
    }
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal bool Check; internal int Tiles, Headers, Chrome; internal Exception? Failure; internal readonly Dictionary<string, Rectangle> Slots = [];
        private bool _replay; private FontStyle _headerStyle;
        public override void DrawGalleryTile(RibbonPaintContext c, RibbonGalleryTile tile, bool selected)
        {
            Slots["tile:" + tile.Text] = c.Bounds;
            if (Check && selected) Safely(() =>
            {
                using var image = new Bitmap(c.Bounds.Width, c.Bounds.Height); using var g = Graphics.FromImage(image); g.Clear(Color.White);
                _replay = true; try { base.DrawGalleryTile(c with { Graphics = g, Bounds = new(Point.Empty, image.Size), Hot = false, Pressed = false, Focused = false }, tile, true); } finally { _replay = false; }
                Require(image.GetPixel(S(3, c), image.Height / 2).ToArgb() == Color.Navy.ToArgb(), "Selected tile bypassed authored outline."); Tiles++;
            });
            base.DrawGalleryTile(c, tile, selected);
        }
        public override void DrawGalleryHeading(RibbonPaintContext c, string text)
        {
            Slots["header:" + text] = c.Bounds;
            if (Check) Safely(() =>
            {
                using var image = new Bitmap(c.Bounds.Width, c.Bounds.Height); using var g = Graphics.FromImage(image); g.Clear(Color.White);
                _replay = true; try { base.DrawGalleryHeading(c with { Graphics = g, Bounds = new(Point.Empty, image.Size) }, text); } finally { _replay = false; }
                Require(image.GetPixel(S(2, c), image.Height / 2).ToArgb() == Color.DarkGreen.ToArgb() && (_headerStyle & FontStyle.Bold) == 0, "Heading outline/weight was bypassed."); Headers++;
            });
            base.DrawGalleryHeading(c, text);
        }
        protected override void DrawChrome(RibbonPaintContext c, RibbonFill fill, Color border) { if (Check) Chrome++; base.DrawChrome(c, fill, border); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { if (_replay) _headerStyle = c.Font.Style; else base.DrawText(c, text, color, flags); }
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { if (!_replay) base.DrawItemImage(c, item, size); }
        private static int S(int n, RibbonPaintContext c) => (int)Math.Round(n * c.DeviceDpi / 96d);
        private void Safely(Action action) { try { action(); } catch (Exception error) { Failure ??= error; } }
    }
}
