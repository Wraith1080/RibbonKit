using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class ApplicationMenuSurfaceCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop; using var painter = new Probe();
            using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1200, 700), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None, FileMenuMode = RibbonFileMenuMode.CommandMenu };
            ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
            var split = new RibbonSplitButtonItem { Text = "Save As", DropDownHeader = "Save options" }; split.Items.Add(new RibbonButtonItem { Text = "Copy" });
            painter.IsDefaultPane = () => !split.IsDropDownOpen;
            using var body = new UserControl { Size = new(300, 130) }; var editor = new TextBox { Text = "Retained application text", Bounds = new(12, 12, 250, 24) }; body.Controls.Add(editor);
            var content = new RibbonDropDownItem { Text = "Open", Content = body, DropDownHeader = "Open options" };
            ribbon.FileMenu.Items.AddRange(split, content, new RibbonButtonItem { Text = "Save" }); ribbon.ApplicationMenu.DefaultPane.DropDownHeader = "Recent documents";
            ribbon.ApplicationMenu.DefaultPane.Items.Add(new RibbonButtonItem { Text = "Recent document" }); ribbon.ApplicationMenu.FooterItems.Add(new RibbonButtonItem { Text = "Options" });
            form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents(); var original = ribbon.Theme;
            var settings = new RibbonApplicationMenuAppearanceSettings { SurroundBackground = new(Color.LightYellow), NavigationBackground = new(Color.LightBlue), DefaultPaneBackground = new(Color.LightGreen), ActivePaneBackground = new(Color.LightPink),
                InnerFrameCornerRadius = 12, InnerFrameBorderWidth = 3, InnerFrameBorderColor = Color.Navy, NavigationDividerWidth = 3, NavigationDividerInset = 8, NavigationDividerColor = Color.Magenta,
                HeaderCornerRadius = 6, HeaderBold = false, DefaultHeaderTextInset = 6, ActiveHeaderTextInset = 14, HeaderRuleThickness = 3, HeaderRuleColor = Color.DarkGreen };
            nint handle = 0; Control? parent = null; int states = 0;
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original; form.Activate(); ribbon.Focus(); Application.DoEvents();
                Open(); var defaults = Snapshot(); split.ShowDropDown(); var selected = Snapshot(); ribbon.FileMenu.CloseDropDown();
                ribbon.Theme = original with { ApplicationMenuAppearance = settings, MenuHeadingBackground = new(Color.LightCyan) }; painter.Check = true; painter.Backgrounds = painter.Headers = 0;
                Open(); CheckSnapshot(defaults); Save("default"); split.ShowDropDown(); CheckSnapshot(selected); Save("selected");
                Require(painter.Failure is null && painter.Backgrounds >= 2 && painter.Headers >= 2, "Actual surface/header callback failed: " + painter.Failure); painter.Check = false;
                content.ShowDropDown(); if (handle == 0) { handle = editor.Handle; parent = editor.Parent; } editor.Select(2, 5);
                split.ShowDropDown(); content.ShowDropDown();
                Require(editor.Handle == handle && ReferenceEquals(editor.Parent, parent) && editor.Text == "Retained application text" && editor.SelectionStart == 2 && editor.SelectionLength == 5 && ReferenceEquals(content.Content, body), "Native content identity/state changed.");
                ribbon.FileMenu.CloseDropDown(); Require(body.Parent is null && !body.IsDisposed, "Borrowed content was not returned."); states++;
                void Open()
                {
                    form.Activate(); ribbon.Focus(); Application.DoEvents(); ribbon.FileMenu.ShowDropDown();
                    // These are synchronous paint/geometry/state checks. An offscreen
                    // host may legitimately lose foreground to the user's application;
                    // pump pending host work before opening, then inspect before the
                    // normal queued deactivation dismisses the popup. No input gate is claimed.
                    Require(ribbon.FileMenu.IsDropDownOpen, $"{scope}/{mode}/{density}: File menu did not open; authored={painter.Check}; paint failure={painter.Failure}.");
                }
                (Size Size, Dictionary<string, Rectangle> Slots) Snapshot() { var surface = Surface(); painter.Slots.Clear(); using var image = Print(surface); return (surface.Size, painter.Slots.ToDictionary(pair => pair.Key, pair => pair.Value)); }
                void CheckSnapshot((Size Size, Dictionary<string, Rectangle> Slots) before) { var after = Snapshot(); Require(before.Size == after.Size && before.Slots.Count == after.Slots.Count && before.Slots.All(pair => after.Slots.TryGetValue(pair.Key, out var bounds) && bounds == pair.Value), "Surface effects changed measured slots or menu size."); }
                void Save(string pane) { Popup().Location = new(-16000, -16000); using var image = Print(Surface()); image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-{pane}.png")); }
            }
            ribbon.Theme = ribbon.Theme with { ApplicationMenuAppearance = settings with { InnerFrameCornerRadius = null, HeaderBold = null } }; Require(ribbon.Theme.ApplicationMenuAppearance!.NavigationDividerWidth == 3, "Member reset cleared another role.");
            ribbon.Theme = original; Require(ribbon.Theme.ApplicationMenuAppearance is null && editor.Handle == handle, "Group reset changed native content.");
            evidence.Add($"PASS {scope}: effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density states; actual background/header/text callbacks, default/selected fills, frame/divider/rule paint and heading weight/insets, unchanged slots/sizes, native pane HWND/parent/value/selection/source, borrowed-content return and member/group reset."); form.Close();
        }
        evidence.Add("Synchronous offscreen public-package paint/geometry/state checks before queued foreground-loss dismissal. External UIA, actual VS, physical input/IME, visible frame pacing and mixed-monitor acceptance remain separate."); File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
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
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal bool Check; internal int Backgrounds, Headers; internal Exception? Failure; internal readonly Dictionary<string, Rectangle> Slots = [];
        internal Func<bool> IsDefaultPane = () => true;
        private bool _headerReplay; private FontStyle _style; private Rectangle _text;
        public override void DrawApplicationMenuBackground(RibbonPaintContext c, Rectangle navigation, Rectangle pane, Rectangle footer)
        {
            Slots["navigation"] = navigation; Slots["pane"] = pane; Slots["footer"] = footer;
            if (Check) Safely(() =>
            {
                using var image = new Bitmap(c.Bounds.Width, c.Bounds.Height); using var g = Graphics.FromImage(image); base.DrawApplicationMenuBackground(c with { Graphics = g }, navigation, pane, footer);
                Require(image.GetPixel(0, 0).ToArgb() == Color.LightYellow.ToArgb(), "Surround fill was bypassed.");
                Require(image.GetPixel(navigation.Left + S(20, c), navigation.Top + S(40, c)).ToArgb() == Color.LightBlue.ToArgb(), "Navigation fill was bypassed.");
                Require(image.GetPixel(pane.Left + S(20, c), pane.Top + S(20, c)).ToArgb() == (IsDefaultPane() ? Color.LightGreen : Color.LightPink).ToArgb(), "Pane fill was bypassed.");
                Require(image.GetPixel(navigation.Left - 1, navigation.Top + navigation.Height / 2).ToArgb() == Color.Navy.ToArgb(), "Inner frame was bypassed.");
                var seam = Ink(image, Color.Magenta); Require(seam == new Rectangle(navigation.Right, navigation.Top - 1 + S(8, c), S(3, c), navigation.Height + 2 - 2 * S(8, c)), "Navigation divider was bypassed."); Backgrounds++;
            });
            base.DrawApplicationMenuBackground(c, navigation, pane, footer);
        }
        public override void DrawMenuHeader(RibbonPaintContext c, string text)
        {
            Slots["header"] = c.Bounds;
            if (Check) Safely(() =>
            {
                using var image = new Bitmap(c.Bounds.Width + 8, c.Bounds.Height + 8); using var g = Graphics.FromImage(image); g.Clear(Color.White); var local = c with { Graphics = g, Bounds = new(4, 4, c.Bounds.Width, c.Bounds.Height) };
                _headerReplay = true; try { base.DrawMenuHeader(local, text); } finally { _headerReplay = false; }
                Require((_style & FontStyle.Bold) == 0 && _text == Rectangle.Inflate(local.Bounds, -S(text == "Recent documents" ? 6 : 14, c), 0), "Heading weight/inset was bypassed.");
                Require(Ink(image, Color.DarkGreen) == new Rectangle(4, local.Bounds.Bottom - S(3, c), c.Bounds.Width, S(3, c)), "Heading rule was bypassed."); Headers++;
            });
            base.DrawMenuHeader(c, text);
        }
        public override void DrawText(RibbonPaintContext c, string text, Color ink, TextFormatFlags flags) { if (_headerReplay) { _style = c.Font.Style; _text = c.Bounds; } else base.DrawText(c, text, ink, flags); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { Slots["item:" + item.Text] = c.Bounds; base.DrawItem(c, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext arrow, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { Slots["split-primary:" + item.Text] = primary.Bounds; Slots["split-arrow:" + item.Text] = arrow.Bounds; base.DrawSplitItem(primary, arrow, item, size, menu); }
        private static int S(int n, RibbonPaintContext c) => (int)Math.Round(n * c.DeviceDpi / 96d);
        private static Rectangle Ink(Bitmap image, Color ink) { var bounds = Rectangle.Empty; for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() == ink.ToArgb()) bounds = bounds.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(bounds, new(x, y, 1, 1)); return bounds; }
        private void Safely(Action action) { try { action(); } catch (Exception error) { Failure ??= error; } }
    }
}
