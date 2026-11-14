using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class CommandContentCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop;
            using var painter = new Probe();
            using var raster = new Bitmap(16, 16); using (var g = Graphics.FromImage(raster)) g.Clear(Color.Red);
            using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1200, 700), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None, QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon };
            var box = new RibbonCheckBoxItem { Text = "Checked", Checked = true, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var radio = new RibbonRadioButtonItem { Text = "Selected", Checked = true, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained D1b input", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var image = new RibbonButtonItem { Text = "Raster disabled", Image = raster, Enabled = false, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var source = new RibbonButtonItem { Text = "Raster source", Image = raster, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var drop = new RibbonDropDownItem { Text = "Choices", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var menuCheck = new RibbonCheckBoxItem { Text = "Menu check", Checked = true }; drop.Items.Add(menuCheck);
            var group = new RibbonGroup { Text = "Content", CanResize = false }; group.Items.AddRange(box, radio, field, image, source, drop);
            var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            var reference = new RibbonQuickAccessReference { Source = source, Enabled = false }; ribbon.QuickAccessItems.Add(reference);
            form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
            var editor = Descendants(ribbon).OfType<TextBox>().Single(control => control.Text == field.Value); nint handle = editor.Handle; var parent = editor.Parent; editor.Select(2, 5);
            var original = ribbon.Theme;
            var settings = new RibbonCommandContentSettings { IndicatorBorderWidth = 2, IndicatorBorderColor = Color.DarkBlue, CheckMarkColor = Color.Magenta,
                CheckMarkStrokeWidth = 3, CheckMarkScale = .7, RadioDotInset = 3, RadioDotColor = Color.DarkOrange, DropDownGlyphWidth = 14, DropDownGlyphHeight = 9, DisabledImageOpacity = .2, DisabledImageGrayscaleAmount = 0 };
            int states = 0;
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original; Application.DoEvents();
                using (var baseline = Print(ribbon)) { }
                var before = painter.Slots.ToDictionary(pair => pair.Key, pair => pair.Value);
                ribbon.Theme = original with { CommandContent = settings }; painter.Check = true; painter.Kinds.Clear();
                using var painted = Print(ribbon); painted.Save(Path.Combine(output, $"{scope}-{mode}-{density}.png")); painter.Check = false;
                Require(painter.Failure is null, "Actual content callback failed: " + painter.Failure);
                Require(new[] { "check", "radio", "arrow", "image", "qat" }.All(painter.Kinds.Contains), "Missing actual content callbacks: " + string.Join(",", painter.Kinds));
                Require(before.All(pair => painter.Slots.TryGetValue(pair.Key, out var bounds) && bounds == pair.Value), "Content paint changed measured native slots.");
                Require(editor.Handle == handle && ReferenceEquals(editor.Parent, parent) && editor.Text == field.Value && editor.SelectionStart == 2 && editor.SelectionLength == 5, "Content settings replaced native editor state.");
                Require(box.Checked && radio.Checked && ReferenceEquals(reference.Source, source) && source.Enabled && !reference.Enabled, "Content paint changed command or QAT state."); states++;
            }
            drop.ShowDropDown(); Require(drop.IsDropDownOpen && drop.Items.Contains(menuCheck), "Menu lost its owned checkbox.");
            ribbon.Theme = ribbon.Theme with { CommandContent = settings with { CheckMarkScale = null } };
            // Theme assignment retains the established popup-close lifecycle.
            Require(!drop.IsDropDownOpen && ribbon.Theme.CommandContent!.RadioDotInset == 3 && menuCheck.Checked && drop.Items.Contains(menuCheck), "Independent reset changed the theme/model contract.");
            drop.ShowDropDown(); Require(drop.IsDropDownOpen && drop.Items.Contains(menuCheck), "Retained menu did not reopen after theme assignment."); drop.CloseDropDown();
            ribbon.Theme = original; Require(ribbon.Theme.CommandContent is null && editor.Handle == handle, "Group reset replaced editor.");
            evidence.Add($"PASS {scope}: effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density states, actual check/radio/triangle/image/QAT settings, unchanged slots, retained editor HWND/parent/value/selection and models, existing theme-close/menu-reopen lifecycle and independent/group reset."); form.Close();
        }
        evidence.Add("Offscreen public-package checks. Actual VS, visible frame pacing, physical input/IME and mixed-monitor review remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static Bitmap Print(RibbonControl ribbon) { var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle); return image; }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(control => new[] { control }.Concat(Descendants(control)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal bool Check;
        internal Exception? Failure;
        internal readonly Dictionary<string, Rectangle> Slots = [];
        internal readonly HashSet<string> Kinds = [];
        public override void DrawCheckBoxIndicator(RibbonPaintContext c, RibbonCheckBoxItem item)
        {
            Slots[item.Text] = c.Bounds;
            if (Check) Safely(() => { using var image = Replay(c, local => base.DrawCheckBoxIndicator(local, item)); Require(Has(image, Color.Magenta), "Check mark setting was bypassed."); Kinds.Add("check"); });
            base.DrawCheckBoxIndicator(c, item);
        }
        public override void DrawRadioButtonIndicator(RibbonPaintContext c, RibbonRadioButtonItem item)
        {
            Slots[item.Text] = c.Bounds;
            if (Check) Safely(() => { using var image = Replay(c, local => base.DrawRadioButtonIndicator(local, item)); Require(Has(image, Color.DarkOrange), "Radio dot setting was bypassed."); Kinds.Add("radio"); });
            base.DrawRadioButtonIndicator(c, item);
        }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color)
        {
            if (Check && c.Bounds.Width > 1 && c.Bounds.Height > 1) Safely(() =>
            {
                using var image = Replay(c, local => base.DrawDropDownArrow(local, Color.Magenta));
                var ink = Ink(image); int S(int n) => (int)Math.Round(n * c.DeviceDpi / 96d);
                Require(ink.Width == Math.Min(c.Bounds.Width, S(14)) && ink.Height == Math.Min(c.Bounds.Height, S(9)), "Triangle dimensions were bypassed."); Kinds.Add("arrow");
            });
            base.DrawDropDownArrow(c, color);
        }
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size)
        {
            if (Check && item.Text == "Raster disabled") Safely(() => { using var image = Replay(c, local => base.DrawItemImage(local, item, size)); CheckOpacity(image); Kinds.Add("image"); });
            base.DrawItemImage(c, item, size);
        }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement item, bool menu)
        {
            if (Check && item is RibbonQuickAccessReference { Enabled: false }) Safely(() => { using var image = Replay(c with { Hot = false, Pressed = false, Focused = false }, local => base.DrawQuickAccessItem(local, item, menu)); CheckOpacity(image); Kinds.Add("qat"); });
            base.DrawQuickAccessItem(c, item, menu);
        }
        private void Safely(Action action) { try { action(); } catch (Exception error) { Failure ??= error; } }
        private static Bitmap Replay(RibbonPaintContext c, Action<RibbonPaintContext> draw)
        {
            var image = new Bitmap(c.Bounds.Width + 8, c.Bounds.Height + 8); using var g = Graphics.FromImage(image); g.Clear(Color.Transparent);
            draw(c with { Graphics = g, Bounds = new(4, 4, c.Bounds.Width, c.Bounds.Height) }); return image;
        }
        private static void CheckOpacity(Bitmap image)
        {
            int alpha = 0; bool color = false;
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) { var pixel = image.GetPixel(x, y); alpha = Math.Max(alpha, pixel.A); color |= pixel.A > 0 && pixel.R > pixel.G + 50; }
            Require(alpha is >= 49 and <= 52, "Disabled raster opacity was bypassed: " + alpha);
            Require(color, "Disabled raster grayscale setting was bypassed.");
        }
        private static bool Has(Bitmap image, Color color)
        {
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() == color.ToArgb()) return true; return false;
        }
        private static Rectangle Ink(Bitmap image)
        {
            var ink = Rectangle.Empty;
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).A > 0) ink = ink.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(ink, new(x, y, 1, 1));
            return ink;
        }
    }
}
