using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class GalleryContentCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop; using var raster = new Bitmap(80, 20); using (var g = Graphics.FromImage(raster)) g.Clear(Color.DarkRed); using var painter = new Probe();
            using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1400, 700), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None };
            var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Galleries", CanResize = false }; var popup = Make("Popup", false); var inline = Make("Inline", true); var custom = Make("Custom", true);
            ((RibbonGalleryTile)popup.Items[0]).Icon = null; popup.Items[0].LargeImage = raster; var made = new List<RetainedView>(); custom.InRibbonPresenterFactory = () => { var view = new RetainedView(); made.Add(view); return view; };
            group.Items.AddRange(popup, inline, custom); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
            Require(made.Count == 1, "Permanent presenter was not created once."); var view = made[0]; nint handle = view.Editor.Handle; var parent = view.Editor.Parent; view.Editor.Select(2, 5); var original = ribbon.Theme; var selected = popup.SelectedItem;
            int commits = 0, previews = 0, cancels = 0, activeChecks = 0, states = 0; custom.SelectionCommitted += (_, _) => commits++; custom.ItemPreview += (_, _) => previews++; custom.ItemPreviewCancelled += (_, _) => cancels++;
            var content = new RibbonGalleryContentSettings { LabelMaxLines = 3, LabelAlignment = ContentAlignment.BottomRight, ArtworkHeightReserve = 20, ArtworkHorizontalInset = 14, ArtworkTopInset = 10,
                ExpandGlyphHeight = 24, ExpandBarWidth = 14, ExpandBarInset = 2, ExpandBarStrokeWidth = 3, UtilityGlyphColor = Color.Magenta };
            var appearance = new RibbonGalleryAppearanceSettings { StripCornerRadius = 12, StripInset = 2, StripBorderWidth = 2, StripBorderColor = Color.Navy, StripBackground = new(Color.LightYellow), UtilityCornerRadius = 4, UtilityInset = 1, UtilityBorderWidth = 2, UtilityBorderColor = Color.DarkGreen, HeaderBold = false };
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original; Open(); var before = Snapshot(Surface()); popup.CloseDropDown(); var beforeInline = Snapshot(ribbon);
                ribbon.Theme = original with { GalleryContent = content, GalleryAppearance = appearance }; painter.Check = true; painter.Frames = painter.Utilities = 0;
                Open(); var after = Snapshot(Surface()); Save("popup", Surface());
                Require(after.Slots.Keys.Order().SequenceEqual(before.Slots.Keys.Order()) && after.Slots["tile:Popup document style 0"].Height >= before.Slots["tile:Popup document style 0"].Height, "Content changed source order or did not reserve label height.");
                var label = painter.Labels["Popup document style 1"]; var row = painter.Slots["tile:Popup document style 1"]; var artwork = painter.Images["Popup document style 1"];
                Require(label.Flags.HasFlag(TextFormatFlags.Right) && label.Flags.HasFlag(TextFormatFlags.Bottom) && label.Bounds.Height <= 3 * ribbon.Font.Height && artwork.Left == row.Left + S(14) && artwork.Top == row.Top + S(10), "Actual label/artwork settings were bypassed.");
                var fitted = painter.Images["Popup document style 0"]; Require(Math.Abs(fitted.Width - 4 * fitted.Height) <= 2, "Authored raster slot lost aspect fitting."); popup.CloseDropDown(); var afterInline = Snapshot(ribbon); Save("inline", ribbon);
                Require(beforeInline.Size == afterInline.Size && Same(beforeInline.Slots, afterInline.Slots), "Content/frame decoration changed inline targets.");
                Require(painter.Failure is null && (mode != RibbonLayoutMode.Classic || painter.Frames > 0 && painter.Utilities > 0), "Actual frame/utility callbacks failed: " + painter.Failure); painter.Check = false;
                Require(ReferenceEquals(selected, popup.SelectedItem) && made.Count == 1 && ReferenceEquals(made[0], view) && !view.IsDisposed && view.Editor.Handle == handle && ReferenceEquals(view.Editor.Parent, parent) && view.Editor.Text == "Retained presenter text" && view.Editor.SelectionStart == 2 && view.Editor.SelectionLength == 5, "Settings replaced native presenter/source state.");
                if (view.Context.IsActive)
                {
                    var target = (RibbonGalleryTile)custom.Items[1]; Require(view.Context.Preview(target) && view.Context.Preview(null) && view.Context.Commit(target), "Current presenter could not preview/cancel/commit."); activeChecks++; Require(commits == activeChecks && previews == activeChecks && cancels == activeChecks, "Presenter events were duplicated.");
                }
                ribbon.Theme = original; Open(); var restored = Snapshot(Surface()); Require(restored.Size == before.Size && Same(before.Slots, restored.Slots), "Content Reset did not restore popup geometry."); popup.CloseDropDown(); states++;
                int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
                void Open() { form.Activate(); ribbon.Focus(); Application.DoEvents(); popup.ShowDropDown(); Require(popup.IsDropDownOpen, "Grouped content gallery did not open."); }
                (Size Size, Dictionary<string, Rectangle> Slots) Snapshot(Control control) { painter.Clear(); using var image = Print(control); return (control.Size, painter.Slots.ToDictionary(pair => pair.Key, pair => pair.Value)); }
                void Save(string kind, Control control) { if (kind == "popup") Popup().Location = new(-16000, -16000); using var image = Print(control); image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-{kind}.png")); }
            }
            Require(activeChecks > 0, "No active presenter route was checked."); ribbon.Theme = original with { GalleryContent = content with { LabelMaxLines = null }, GalleryAppearance = appearance }; Require(ribbon.Theme.GalleryContent!.ExpandBarWidth == 14, "Label member reset cleared glyphs.");
            ribbon.Theme = ribbon.Theme with { GalleryContent = null }; Require(ribbon.Theme.GalleryAppearance!.HeaderBold == false && made.Count == 1 && view.Editor.Handle == handle, "Content reset cleared appearance/native ownership.");
            evidence.Add($"PASS {scope}: effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density states; actual content/image/text/frame/utility/chrome callbacks, label alignment/reserve and aspect fitting, restored popup geometry and unchanged inline targets, retained custom presenter/editor HWND/parent/value/selection and source selection, {activeChecks} active preview/cancel/commit sequences, member/group reset."); form.Close();
        }
        using (var demo = new DesignerForm()) demo.VerifyGalleryContentDemo(output);
        evidence.Add("PASS demo: D5b controls on new Theme effects 7; accepted tab 6 retains three groups and earlier tabs remain seven/five/four/seven/six; public presets/hide/independent resets, preview and idempotence.");
        evidence.Add("Synchronous offscreen public-package paint/state checks before queued foreground-loss dismissal. External UIA, actual VS, physical input/IME, visible frame pacing and mixed-monitor acceptance remain separate."); File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static RibbonGalleryItem Make(string name, bool inline)
    {
        var gallery = new RibbonGalleryItem { Text = name, IsInRibbon = inline, InRibbonColumnCount = 2, ColumnCount = 3, MaxVisibleRows = 2, TileWidth = 90, TileHeight = 80, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        gallery.Items.AddRange(Enumerable.Range(0, 9).Select(i => new RibbonGalleryTile { Text = name + " document style " + i, GroupName = i < 5 ? "Group one" : "Group two", Icon = RibbonStockIcons.Create("save") }).ToArray()); gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0]; return gallery;
    }
    private static bool Same(Dictionary<string, Rectangle> a, Dictionary<string, Rectangle> b) => a.Count == b.Count && a.All(pair => b.TryGetValue(pair.Key, out var value) && value == pair.Value);
    private static Bitmap Print(Control control) { var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, control.ClientRectangle); return image; }
    private static Control Surface() => Popup().Items.OfType<ToolStripControlHost>().Single().Control;
    private static ToolStripDropDown Popup()
    {
        var windows = new List<ToolStripDropDown>(); EnumWindow callback = (handle, _) => { if (Control.FromHandle(handle) is ToolStripDropDown { Visible: true } window) windows.Add(window); return true; }; EnumThreadWindows(GetCurrentThreadId(), callback, 0); GC.KeepAlive(callback); return windows.Single();
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private delegate bool EnumWindow(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, nint parameter);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private sealed class RetainedView : UserControl, IRibbonGalleryPresenter
    {
        internal readonly TextBox Editor = new() { Text = "Retained presenter text", Bounds = new(8, 8, 140, 24) }; internal RibbonGalleryPresenterContext Context = null!;
        internal RetainedView() { Size = new(180, 80); Controls.Add(Editor); }
        public override Size GetPreferredSize(Size proposedSize) => new(180, 80);
        public void Synchronize(RibbonGalleryPresenterContext context) => Context = context;
        public Rectangle GetItemBounds(RibbonGalleryTile item) { int index = Context.Gallery.Items.IndexOf(item); return index < 0 ? Rectangle.Empty : new(4 + index % 2 * 80, 38 + index / 2 * 32, 76, 28); }
        public RibbonGalleryTile? HitTest(Point point) => Context.Gallery.Items.OfType<RibbonGalleryTile>().FirstOrDefault(tile => GetItemBounds(tile).Contains(point));
        public RibbonGalleryTile? Navigate(RibbonGalleryTile? current, Keys key) => current;
        public void Reveal(RibbonGalleryTile item) { }
        public void ScrollView(RibbonGalleryScroll amount) { }
        public bool CanScrollBackward => false;
        public bool CanScrollForward => false;
    }
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal bool Check; internal int Frames, Utilities; internal Exception? Failure; internal readonly Dictionary<string, Rectangle> Slots = [], Images = [];
        internal readonly Dictionary<string, (Rectangle Bounds, TextFormatFlags Flags)> Labels = [];
        internal void Clear() { Slots.Clear(); Images.Clear(); Labels.Clear(); }
        public override void DrawGalleryTile(RibbonPaintContext c, RibbonGalleryTile tile, bool selected) { Slots["tile:" + tile.Text] = c.Bounds; base.DrawGalleryTile(c, tile, selected); }
        public override void DrawGalleryHeading(RibbonPaintContext c, string text) { Slots["header:" + text] = c.Bounds; base.DrawGalleryHeading(c, text); }
        public override void DrawGalleryStripFrame(RibbonPaintContext c)
        {
            Slots["frame:" + c.Bounds.Width] = c.Bounds;
            if (Check) Safely(() => { using var image = new Bitmap(c.Bounds.Width, c.Bounds.Height); using var g = Graphics.FromImage(image); g.Clear(Color.White); base.DrawGalleryStripFrame(c with { Graphics = g, Bounds = new(Point.Empty, image.Size) }); Require(image.GetPixel(S(2, c), image.Height / 2).ToArgb() == Color.Navy.ToArgb(), "Frame outline bypassed settings."); Frames++; });
            base.DrawGalleryStripFrame(c);
        }
        public override void DrawGalleryStripButton(RibbonPaintContext c, RibbonGalleryStripButton button, bool enabled)
        {
            Slots["utility:" + button + ":" + c.Bounds.Width] = c.Bounds;
            if (Check && enabled) Safely(() =>
            {
                using var image = new Bitmap(c.Bounds.Width, c.Bounds.Height); using var g = Graphics.FromImage(image); g.Clear(Color.White); base.DrawGalleryStripButton(c with { Graphics = g, Bounds = new(Point.Empty, image.Size), Hot = true, Pressed = false }, button, true);
                Require(image.GetPixel(S(1, c), image.Height / 2).ToArgb() == Color.DarkGreen.ToArgb(), "Utility outline bypassed settings.");
                Require(Enumerable.Range(0, image.Height).Any(y => Enumerable.Range(0, image.Width).Any(x => image.GetPixel(x, y).R > image.GetPixel(x, y).G && image.GetPixel(x, y).B > image.GetPixel(x, y).G)), "Utility glyph ink was bypassed."); Utilities++;
            });
            base.DrawGalleryStripButton(c, button, enabled);
        }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { Labels[text] = (c.Bounds, flags); base.DrawText(c, text, color, flags); }
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { Images[item.Text] = c.Bounds; base.DrawItemImage(c, item, size); }
        private static int S(int n, RibbonPaintContext c) => (int)Math.Round(n * c.DeviceDpi / 96d);
        private void Safely(Action action) { try { action(); } catch (Exception error) { Failure ??= error; } }
    }
}
