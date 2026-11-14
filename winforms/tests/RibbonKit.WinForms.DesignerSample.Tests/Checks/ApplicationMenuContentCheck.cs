using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class ApplicationMenuContentCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop; using var raster = new Bitmap(16, 16); using (var g = Graphics.FromImage(raster)) g.Clear(Color.DarkRed);
            using var painter = new Probe();
            using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1200, 700), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None, FileMenuMode = RibbonFileMenuMode.CommandMenu };
            ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
            var split = new RibbonSplitButtonItem { Text = "Save As", Icon = RibbonStockIcons.Create("save"), DropDownHeader = "Save options", MenuPresentation = RibbonMenuPresentation.IllustratedChoices, CheckOnClick = true };
            var rich = new RibbonButtonItem { Text = "Document format", Icon = RibbonStockIcons.Create("save"), MenuDescription = "A longer description for the document format that wraps across several lines while retaining its command source and content slots." };
            var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained field" }; split.Items.AddRange(rich, new RibbonMenuSeparatorItem(), field);
            using var body = new UserControl { Size = new(300, 130) }; var editor = new TextBox { Text = "Retained body", Bounds = new(12, 12, 250, 24) }; body.Controls.Add(editor);
            var content = new RibbonDropDownItem { Text = "Open", Content = body, DropDownHeader = "Open options" }; ribbon.FileMenu.Items.AddRange(split, content);
            ribbon.ApplicationMenu.DefaultPane.DropDownHeader = "Recent documents";
            ribbon.ApplicationMenu.DefaultPane.Items.AddRange(new RibbonButtonItem { Text = "Recent document", Image = raster, MenuDescription = "A recent document description that can wrap over several lines." }, new RibbonMenuSeparatorItem());
            ribbon.ApplicationMenu.FooterItems.Add(new RibbonButtonItem { Text = "Options", Image = raster });
            int actions = 0; split.Click += (_, _) => actions++; form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents(); var original = ribbon.Theme;
            var settings = new RibbonApplicationMenuContentSettings { LargeIconSize = 40, SmallIconSize = 24, LargeIconInset = 10, SmallIconInset = 4, IconTextGap = 12, LargeTextInset = 72, TextInset = 10, TextTrailingPadding = 9,
                RowTextVerticalPadding = 20, IconVerticalPadding = 12, DescriptionGap = 6, DescriptionVerticalInset = 8, DescriptionMaxLines = 3, RichTitleBold = false, FooterHorizontalPadding = 14,
                ArrowColumnWidth = 38, SeparatorHeight = 13, SeparatorHorizontalPadding = 12, SeparatorThickness = 3, SeparatorColor = Color.Magenta };
            nint fieldHandle = 0, bodyHandle = 0; Control? fieldParent = null, bodyParent = null; Rectangle bodyBounds = Rectangle.Empty; int states = 0;
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original; Open(); split.ShowDropDown(); var before = Snapshot();
                var native = Descendants(Surface()).OfType<TextBox>().Single(t => t.Text == field.Value); int nativeHeight = native.Height;
                if (fieldHandle == 0) { fieldHandle = native.Handle; fieldParent = native.Parent; } native.Select(1, 4);
                content.ShowDropDown(); if (bodyHandle == 0) { bodyHandle = editor.Handle; bodyParent = editor.Parent; bodyBounds = editor.Bounds; } ribbon.FileMenu.CloseDropDown();
                ribbon.Theme = original with { ApplicationMenuContent = settings }; painter.CheckSeparator = true; Open(); Save("default");
                Require(painter.Images["Recent document"].Width == S(24) && painter.Images["Options"].Width == S(24), "Small/footer icon metrics were bypassed.");
                split.ShowDropDown(); var after = Snapshot(); Save("selected");
                Require(painter.Failure is null && painter.Separators > 0, "Actual Application Menu separator callback failed: " + painter.Failure);
                Require(painter.Images["Document format"].Width == S(40) && painter.Texts["Document format"].Style == FontStyle.Regular, "Rich icon/weight was bypassed.");
                Require(painter.Texts["Document format"].Bounds.Left == painter.Rows["Document format"].Left + S(72), "Rich text gutter was bypassed.");
                Require(painter.Texts[rich.MenuDescription].Bounds.Top == painter.Texts["Document format"].Bounds.Bottom + S(6), "Description gap was bypassed.");
                Require(painter.SplitArrow.Width == S(density == RibbonDensity.Touch ? 44 : 38) && painter.Rows["Document format"].Height >= before.Rows["Document format"].Height, "Measured content/arrow targets did not follow authored metrics.");
                native = Descendants(Surface()).OfType<TextBox>().Single(t => t.Text == field.Value);
                Require(native.Handle == fieldHandle && ReferenceEquals(native.Parent, fieldParent) && native.Height == nativeHeight && native.SelectionStart == 1 && native.SelectionLength == 4, "Native field frame/identity/selection changed.");
                var root = Surface().AccessibilityObject; Enumerable.Range(0, root.GetChildCount()).Select(root.GetChild).Single(child => child?.Name == "Save As")!.DoDefaultAction();
                Require(actions == states + 1 && !ribbon.FileMenu.IsDropDownOpen, "Accessible split primary source/action/dismissal changed.");
                Open(); content.ShowDropDown(); editor.Select(2, 5); split.ShowDropDown(); content.ShowDropDown();
                Require(editor.Handle == bodyHandle && ReferenceEquals(editor.Parent, bodyParent) && editor.Bounds == bodyBounds && editor.Text == "Retained body" && editor.SelectionStart == 2 && editor.SelectionLength == 5, "Borrowed native body was replaced or restyled.");
                ribbon.FileMenu.CloseDropDown(); Require(body.Parent is null && !body.IsDisposed, "Borrowed content was not returned.");
                painter.CheckSeparator = false; ribbon.Theme = original; Open(); split.ShowDropDown(); var restored = Snapshot();
                Require(restored.Size == before.Size && restored.Rows.Count == before.Rows.Count && before.Rows.All(pair => restored.Rows.TryGetValue(pair.Key, out var bounds) && bounds == pair.Value), "Group reset did not restore measured geometry."); ribbon.FileMenu.CloseDropDown(); states++;
                int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
                void Open() { form.Activate(); ribbon.Focus(); Application.DoEvents(); ribbon.FileMenu.ShowDropDown(); Require(ribbon.FileMenu.IsDropDownOpen, "File menu did not open."); }
                (Size Size, Dictionary<string, Rectangle> Rows) Snapshot() { var surface = Surface(); painter.Clear(); using var image = Print(surface); return (surface.Size, painter.Rows.ToDictionary(pair => pair.Key, pair => pair.Value)); }
                void Save(string pane) { Popup().Location = new(-16000, -16000); painter.Clear(); using var image = Print(Surface()); image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-{pane}.png")); }
            }
            ribbon.Theme = original with { ApplicationMenuContent = settings with { LargeIconSize = null }, ApplicationMenuAppearance = new() { RowInset = 4 } }; Require(ribbon.Theme.ApplicationMenuContent!.DescriptionMaxLines == 3, "Member reset cleared another content role.");
            ribbon.Theme = ribbon.Theme with { ApplicationMenuContent = null }; Require(ribbon.Theme.ApplicationMenuAppearance!.RowInset == 4 && editor.Handle == bodyHandle, "Content reset cleared appearance/native state.");
            evidence.Add($"PASS {scope}: effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density states; actual image/text/split/row callbacks, authored sizes/gutters/gaps/weight/arrow targets, measured geometry restored after reset, accessible primary action/dismissal, retained native field HWND/parent/height/value/selection and body HWND/parent/bounds/value/selection, borrowed return and independent reset."); form.Close();
        }
        evidence.Add("Synchronous offscreen public-package checks before normal queued foreground-loss dismissal. AccessibleObject actions are not an external UIA client run. Actual VS, physical input/IME, visible frame pacing and mixed-monitor acceptance remain separate."); File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static Bitmap Print(Control control) { var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, control.ClientRectangle); return image; }
    private static Control Surface() => Popup().Items.OfType<ToolStripControlHost>().Single().Control;
    private static ToolStripDropDown Popup()
    {
        var windows = new List<ToolStripDropDown>(); EnumWindow callback = (handle, _) => { if (Control.FromHandle(handle) is ToolStripDropDown { Visible: true } window) windows.Add(window); return true; };
        EnumThreadWindows(GetCurrentThreadId(), callback, 0); GC.KeepAlive(callback); return windows.Single();
    }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private delegate bool EnumWindow(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, nint parameter);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal readonly Dictionary<string, Rectangle> Rows = [], Images = [];
        internal readonly Dictionary<string, (Rectangle Bounds, FontStyle Style)> Texts = [];
        internal Rectangle SplitArrow;
        internal bool CheckSeparator; internal int Separators; internal Exception? Failure;
        internal void Clear() { Rows.Clear(); Images.Clear(); Texts.Clear(); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { Rows[item.Text] = c.Bounds; base.DrawItem(c, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext arrow, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { Rows[item.Text] = Rectangle.Union(primary.Bounds, arrow.Bounds); SplitArrow = arrow.Bounds; base.DrawSplitItem(primary, arrow, item, size, menu); }
        public override void DrawText(RibbonPaintContext c, string text, Color ink, TextFormatFlags flags) { Texts[text] = (c.Bounds, c.Font.Style); base.DrawText(c, text, ink, flags); }
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { Images[item.Text] = c.Bounds; base.DrawItemImage(c, item, size); }
        public override void DrawMenuSeparator(RibbonPaintContext c, RibbonMenuSeparatorItem item)
        {
            if (CheckSeparator) try
            {
                using var image = new Bitmap(c.Bounds.Width, c.Bounds.Height); using var g = Graphics.FromImage(image); g.Clear(Color.White);
                base.DrawMenuSeparator(c with { Graphics = g, Bounds = new(Point.Empty, image.Size) }, item);
                int rows = Enumerable.Range(0, image.Height).Count(y => image.GetPixel(image.Width / 2, y).ToArgb() == Color.Magenta.ToArgb());
                Require(rows == (int)Math.Round(3 * c.DeviceDpi / 96d), "Live separator did not consume Application Menu thickness/ink."); Separators++;
            }
            catch (Exception error) { Failure ??= error; }
            base.DrawMenuSeparator(c, item);
        }
    }
}
