using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class MenuAppearanceCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop; using var painter = new Probe();
            using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1200, 700), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None, QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon, QuickAccessMaxWidth = 28 };
            var menu = new RibbonDropDownItem { Text = "Menu", DropDownHeader = "Menu heading", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained menu text" }; var check = new RibbonCheckBoxItem { Text = "Keep", Checked = true };
            var nested = new RibbonDropDownItem { Text = "Nested", DropDownHeader = "Nested heading" }; nested.Items.AddRange(new RibbonButtonItem { Text = "Child action" }, new RibbonMenuSeparatorItem(), new RibbonCheckBoxItem { Text = "Child check" });
            menu.Items.AddRange(new RibbonButtonItem { Text = "Action" }, new RibbonMenuSeparatorItem(), check, field, nested);
            var group = new RibbonGroup { Text = "Menus", CanResize = false }; group.Items.Add(menu); var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            int clicks = 0; for (int i = 0; i < 80; i++) { var command = new RibbonButtonItem { Text = "Overflow command " + i }; command.Click += (_, _) => clicks++; ribbon.QuickAccessItems.Add(command); }
            form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents(); var original = ribbon.Theme;
            var settings = new RibbonMenuAppearanceSettings { HeaderCornerRadius = 6, HeaderInset = 2, HeaderBorderWidth = 2, HeaderBorderColor = Color.DarkBlue, HeaderBold = false,
                SeparatorThickness = 3, SeparatorColor = Color.Magenta, OverflowBackground = new(Color.LightYellow, Color.LightSkyBlue), OverflowCornerRadius = 6, OverflowInset = 2, OverflowBorderWidth = 2, OverflowBorderColor = Color.DarkBlue };
            nint handle = 0; Control? parent = null; int states = 0;
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original; menu.ShowDropDown(); Application.DoEvents();
                var first = WindowWithText(field.Value); var surface = Content(first); var size = surface.Size;
                painter.Slots.Clear(); using (var image = Print(surface)) { }
                var geometry = NativeGeometry(surface); var slots = painter.Slots.ToDictionary(pair => pair.Key, pair => pair.Value);
                Require(geometry.Length > 0, "Baseline menu has no native editor geometry.");
                var editor = Descendants(first).OfType<TextBox>().Single(control => control.Text == field.Value);
                if (handle == 0) { handle = editor.Handle; parent = editor.Parent; }
                editor.Select(1, 4); menu.CloseDropDown();
                var baselineOverflow = OpenOverflow(ribbon); using (var baselineImage = Print(Content(baselineOverflow))) { }
                var overflowSize = Content(baselineOverflow).Size; var up = painter.Up; var down = painter.Down; Key(Content(baselineOverflow), Keys.Escape);
                ribbon.Theme = original with { MenuAppearance = settings }; painter.Check = true; painter.Headings = painter.Separators = painter.Bands = 0;
                menu.ShowDropDown(); Application.DoEvents(); var window = WindowWithText(field.Value); window.Location = new(-16000, -16000); surface = Content(window);
                using (var image = Print(surface)) image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-menu.png"));
                Require(surface.Size == size && NativeGeometry(surface).SequenceEqual(geometry) && slots.All(pair => painter.Slots.TryGetValue(pair.Key, out var bounds) && bounds == pair.Value), "Menu decoration changed measured rows or native bounds.");
                Require(editor.Handle == handle && ReferenceEquals(editor.Parent, parent) && editor.Text == field.Value && editor.SelectionStart == 1 && editor.SelectionLength == 4 && check.Checked, "Menu decoration replaced native state.");
                nested.ShowDropDown(); Application.DoEvents(); Require(menu.IsDropDownOpen && nested.IsDropDownOpen, "Nested menu lost its owner."); nested.CloseDropDown(); menu.CloseDropDown();
                var overflow = OpenOverflow(ribbon); overflow.Location = new(-16000, -16000); var overflowSurface = Content(overflow);
                using (var image = Print(overflowSurface)) image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-overflow.png"));
                Require(overflowSurface.Size == overflowSize && painter.Up == up && painter.Down == down && !painter.UpEnabled && painter.DownEnabled, "Menu decoration changed overflow band geometry or initial endpoints.");
                Key(overflowSurface, Keys.End); using (var image = Print(overflowSurface)) { }
                Require(painter.UpEnabled && !painter.DownEnabled && clicks == 0, "Overflow keyboard reveal changed endpoint/action semantics.");
                Key(overflowSurface, Keys.Home); using (var image = Print(overflowSurface)) { }
                Require(!painter.UpEnabled && painter.DownEnabled && clicks == 0, "Overflow Home did not restore the first endpoint."); Key(overflowSurface, Keys.Escape);
                Require(painter.Failure is null && painter.Headings > 0 && painter.Separators > 0 && painter.Bands > 0 && painter.GalleryEntries > 0, "Actual menu callbacks failed: " + painter.Failure);
                painter.Check = false; states++;
            }
            ribbon.Theme = ribbon.Theme with { MenuAppearance = settings with { HeaderCornerRadius = null } }; Require(ribbon.Theme.MenuAppearance!.SeparatorThickness == 3, "Member reset cleared separator settings.");
            ribbon.Theme = original; Require(ribbon.Theme.MenuAppearance is null && field.Value == "Retained menu text", "Group reset lost native/model state.");
            evidence.Add($"PASS {scope}: effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density states; actual header/gallery/chrome/text/separator/band callbacks, unchanged menu/native-control/band geometry, retained native editor HWND/parent/value/selection, nested menus, overflow Home/End endpoints without clicks, member/group reset."); form.Close();
        }
        evidence.Add("Offscreen public-package checks. Actual VS, physical input/IME, visible frame pacing and mixed-monitor review remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static Control Content(ToolStripDropDown window) => window.Items.OfType<ToolStripControlHost>().Single().Control;
    private static ToolStripDropDown OpenOverflow(RibbonControl ribbon)
    {
        ribbon.Focus();
        // Escape any retained popup-level/root KeyTip hints before beginning
        // a fresh keyboard session; F10 otherwise toggles existing hints off.
        for (int index = 0; index < 2; index++)
        {
            var escape = Message.Create(ribbon.Handle, 0x100, (nint)(int)Keys.Escape, 0);
            _ = Application.FilterMessage(ref escape) || ribbon.PreProcessMessage(ref escape);
        }
        foreach (var key in new[] { Keys.F10, Keys.D0 })
        {
            var input = Message.Create(ribbon.Handle, 0x100, (nint)(int)key, 0);
            Require(Application.FilterMessage(ref input) || ribbon.PreProcessMessage(ref input), "Overflow KeyTip was not handled: " + key);
        }
        Application.DoEvents(); return Windows().Single();
    }
    private static string[] NativeGeometry(Control surface) => Descendants(surface).OfType<TextBox>().Select(control => control.Text + ":" + control.Bounds).ToArray();
    private static void Key(Control control, Keys key) { var message = Message.Create(control.Handle, 0x100, (nint)(int)key, 0); Require(control.PreProcessMessage(ref message), "Popup key was not handled: " + key); Application.DoEvents(); }
    private static Bitmap Print(Control control) { var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, control.ClientRectangle); return image; }
    private static ToolStripDropDown WindowWithText(string text) => Windows().Single(window => Descendants(window).OfType<TextBox>().Any(control => control.Text == text));
    private static List<ToolStripDropDown> Windows()
    {
        var result = new List<ToolStripDropDown>(); EnumWindow callback = (handle, _) => { if (Control.FromHandle(handle) is ToolStripDropDown { Visible: true } window) result.Add(window); return true; };
        EnumThreadWindows(GetCurrentThreadId(), callback, 0); GC.KeepAlive(callback); return result;
    }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(control => new[] { control }.Concat(Descendants(control)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private delegate bool EnumWindow(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, nint parameter);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal bool Check, UpEnabled, DownEnabled;
        internal int Headings, Separators, Bands, GalleryEntries;
        internal Rectangle Up, Down;
        internal Exception? Failure;
        internal readonly Dictionary<string, Rectangle> Slots = [];
        private bool _headingText;
        private bool _bold;
        public override void DrawMenuHeader(RibbonPaintContext c, string text)
        {
            Slots["header:" + text] = c.Bounds;
            if (Check) Safely(() =>
            {
                using var image = new Bitmap(c.Bounds.Width, c.Bounds.Height); using var g = Graphics.FromImage(image);
                _headingText = true; try { base.DrawMenuHeader(c with { Graphics = g, Bounds = new(Point.Empty, image.Size) }, text); } finally { _headingText = false; }
                int inset = S(2, c); Require(image.GetPixel(inset, image.Height / 2).ToArgb() == Color.DarkBlue.ToArgb() && !_bold, "Heading appearance or text weight was bypassed."); Headings++;
            });
            base.DrawMenuHeader(c, text);
        }
        public override void DrawGalleryHeading(RibbonPaintContext c, string text) { GalleryEntries++; base.DrawGalleryHeading(c, text); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { if (_headingText) _bold = c.Font.Bold; base.DrawText(c, text, color, flags); }
        public override void DrawMenuSeparator(RibbonPaintContext c, RibbonMenuSeparatorItem item)
        {
            Slots["separator"] = c.Bounds;
            if (Check) Safely(() =>
            {
                using var image = new Bitmap(c.Bounds.Width, c.Bounds.Height); using var g = Graphics.FromImage(image); base.DrawMenuSeparator(c with { Graphics = g, Bounds = new(Point.Empty, image.Size) }, item);
                int rows = Enumerable.Range(0, image.Height).Count(y => image.GetPixel(image.Width / 2, y).ToArgb() == Color.Magenta.ToArgb()); Require(rows == Math.Min(image.Height, S(3, c)), "Separator thickness/ink was bypassed."); Separators++;
            });
            base.DrawMenuSeparator(c, item);
        }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { if (menu) Slots["row:" + item.Text] = c.Bounds; base.DrawItem(c, item, size, menu); }
        public override void DrawOverflowScrollButton(RibbonPaintContext c, bool upward, bool enabled)
        {
            if (upward) { Up = c.Bounds; UpEnabled = enabled; } else { Down = c.Bounds; DownEnabled = enabled; }
            if (Check) Safely(() =>
            {
                using var image = new Bitmap(c.Bounds.Width, c.Bounds.Height); using var g = Graphics.FromImage(image); var local = c with { Graphics = g, Bounds = new(Point.Empty, image.Size), Hot = true, Pressed = false };
                base.DrawOverflowScrollButton(local, upward, enabled); Require(image.GetPixel(0, 0).ToArgb() == Color.LightYellow.ToArgb(), "Opaque overflow backing was bypassed.");
                if (enabled) Require(image.GetPixel(S(2, c), image.Height / 2).ToArgb() == Color.DarkBlue.ToArgb(), "Overflow active outline was bypassed."); Bands++;
            });
            base.DrawOverflowScrollButton(c, upward, enabled);
        }
        private static int S(int value, RibbonPaintContext c) => (int)Math.Round(value * c.DeviceDpi / 96d);
        private void Safely(Action action) { try { action(); } catch (Exception error) { Failure ??= error; } }
    }
}
