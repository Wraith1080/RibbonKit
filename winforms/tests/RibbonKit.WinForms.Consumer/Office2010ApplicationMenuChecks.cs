using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using RibbonKit.WinForms;

namespace IndependentConsumer;

// Compiles against both the retained pre-F5 package and the new package. All
// contexts originate in public renderer hooks on real native Application Menus.
internal static class Office2010ApplicationMenuChecks
{
    internal static int Run(string directory, bool current)
    {
        Directory.CreateDirectory(directory);
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
            using var form = new RibbonForm(); using var ribbon = new RibbonControl(); form.Controls.Add(ribbon); form.Ribbon = ribbon;
            ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); ribbon.FileMenu.Visible = true;
            var save = new RibbonSplitButtonItem { Text = "Save As", Id = "save", DropDownHeader = "Save a copy of the document" };
            save.Items.Add(new RibbonButtonItem { Text = "Word document", MenuDescription = "Save in the default file format." });
            save.Items.Add(new RibbonButtonItem { Text = "Word template", MenuDescription = "Save as a reusable template." });
            var custom = new RibbonDropDownItem { Text = "Native page", DropDownHeader = "Application-owned content" };
            using var notes = new TextBox { Text = "Retained native editor", Dock = DockStyle.Fill }; custom.Content = notes;
            ribbon.FileMenu.Items.AddRange(new RibbonButtonItem { Text = "New" }, save, custom);
            ribbon.ApplicationMenu.DefaultPane.DropDownHeader = "Recent Documents";
            ribbon.ApplicationMenu.DefaultPane.Items.Add(new RibbonButtonItem { Text = "1  Quarterly report.docx" });
            var footer = new RibbonButtonItem { Text = "Options" }; ribbon.ApplicationMenu.FooterItems.Add(footer);
            int commands = 0, footers = 0; save.Click += (_, _) => commands++; footer.Click += (_, _) => footers++;
            VerifySample(form, ribbon, directory, "independent", current);
            ribbon.FileMenu.ShowDropDown(); custom.ShowDropDown(); Application.DoEvents(); var hwnd = notes.Handle; var parent = notes.Parent; notes.Select(2, 5);
            save.ShowDropDown(); custom.ShowDropDown(); Application.DoEvents();
            Require(notes.Handle == hwnd && ReferenceEquals(notes.Parent, parent) && notes.Text == "Retained native editor" && notes.SelectionStart == 2 && notes.SelectionLength == 5, "Native editor ownership, text and selection");
            save.ShowDropDown(); Application.DoEvents(); Child(Surface().AccessibilityObject, save.Text).DoDefaultAction(); Application.DoEvents();
            Require(commands == 1 && !ribbon.FileMenu.IsDropDownOpen, "Split primary invokes once and dismisses File");
            ribbon.FileMenu.ShowDropDown(); Application.DoEvents(); Child(Surface().AccessibilityObject, footer.Text).DoDefaultAction(); Application.DoEvents();
            Require(footers == 1 && !ribbon.FileMenu.IsDropDownOpen, "Footer invokes once and dismisses File");
            File.AppendAllText(Path.Combine(directory, "result.log"), "\nPASS: public native split/footer actions and retained editor HWND/parent/text/selection.");
            return 0;
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(directory, "failure.log"), error.ToString()); return 1; }
    }

    internal static void VerifySample(RibbonForm form, RibbonControl ribbon, string directory, string name, bool current = true)
    {
        Directory.CreateDirectory(directory); ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu;
        var tile = ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).OfType<RibbonGalleryItem>().SelectMany(g => g.Items).OfType<RibbonGalleryTile>().FirstOrDefault(t => t.Text == "Office 2010 Blue");
        if (tile is null) ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2010Blue; else tile.PerformClick();
        var previousRenderer = ribbon.Renderer; using var painter = new Recorder(); ribbon.Renderer = painter;
        form.StartPosition = FormStartPosition.Manual; form.Bounds = new(-16000, -16000, 1100, 760); form.ShowInTaskbar = false; form.Show(); Application.DoEvents();
        var hashes = new Dictionary<string, string>();
        try
        {
            var split = ribbon.FileMenu.Items.OfType<RibbonSplitButtonItem>().First(i => i.Visible);
            var footer = ribbon.ApplicationMenu.FooterItems.OfType<RibbonButtonItem>().First(i => i.Visible);
            ribbon.FileMenu.ShowDropDown(); Application.DoEvents(); using var normal = Capture("default");
            Require(painter.Items.ContainsKey(footer) && painter.Splits.ContainsKey(split), "Actual Application Menu item/split/footer renderer hooks");
            var footerContext = painter.Items[footer]; var primary = painter.Splits[split].Primary; var arrow = painter.Splits[split].Arrow;
            var surfaceBounds = Surface().ClientRectangle; var navigation = painter.Navigation; var pane = painter.Pane; var footerBounds = painter.Footer;
            var header = painter.Header; var headerText = painter.HeaderText; var background = painter.Background;
            int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d, MidpointRounding.AwayFromZero);
            int hx = header.Bounds.Right - S(12), hy = header.Bounds.Top + S(4);
            bool headingMatches = normal.GetPixel(hx, hy) == normal.GetPixel(hx, pane.Bottom - S(4));
            var samples = new Dictionary<string, int[]>(); var borderPixels = new Dictionary<string, int>();
            foreach (int state in new[] { 0, 1, 2 })
            {
                using var row = new Bitmap(footerContext.Bounds.Width, footerContext.Bounds.Height); using var g = Graphics.FromImage(row);
                g.Clear(Color.FromArgb(218, 229, 241));
                painter.DrawItem(footerContext with { Graphics = g, Bounds = new(Point.Empty, row.Size), Hot = state == 1, Pressed = state == 2 }, footer, RibbonItemSize.Medium, true);
                row.Save(Path.Combine(directory, $"footer-{state}.png"));
                samples["footer-" + state] = Enumerable.Range(S(6), row.Height - S(12)).Select(y => row.GetPixel(row.Width - S(12), y).ToArgb()).ToArray();
                borderPixels["footer-" + state] = Count(row, state == 0 ? ribbon.Theme.PopupBorder : state == 1 ? ribbon.Theme.CommandAppearance!.StateChrome!.HoverBorderColor : ribbon.Theme.CommandAppearance!.StateChrome!.PressedBorderColor);
            }
            // A real open pane supplies the IsDropDownOpen model state. Paint the
            // complete row through the captured public hook without moving input.
            split.ShowDropDown(); Application.DoEvents(); using var selected = Capture("selected-pane");
            var geometry = new { Surface = surfaceBounds, Navigation = navigation, Pane = pane, Footer = footerBounds, Main = primary.Bounds, Arrow = arrow.Bounds };
            foreach (int state in new[] { 0, 1, 2, 3 })
            {
                var whole = Rectangle.Union(primary.Bounds, arrow.Bounds); using var row = new Bitmap(whole.Width, whole.Height); using var g = Graphics.FromImage(row); g.Clear(Color.FromArgb(250, 250, 250));
                var mainBounds = primary.Bounds; mainBounds.Offset(-whole.Left, -whole.Top); var arrowBounds = arrow.Bounds; arrowBounds.Offset(-whole.Left, -whole.Top);
                painter.DrawSplitItem(primary with { Graphics = g, Bounds = mainBounds, Hot = state is 1 or 3, Pressed = state == 3 }, arrow with { Graphics = g, Bounds = arrowBounds, Hot = state == 2 }, split, RibbonItemSize.Medium, true);
                row.Save(Path.Combine(directory, $"split-{state}.png"));
                borderPixels["split-" + state] = Count(row, state == 3 ? ribbon.Theme.CommandAppearance!.StateChrome!.PressedBorderColor : ribbon.Theme.CommandAppearance!.StateChrome!.HoverBorderColor);
            }
            Require(ribbon.FileMenu.IsDropDownOpen && split.IsDropDownOpen && !ribbon.IsBackstageOpen, "CommandMenu route and selected pane");
            ribbon.FileMenu.CloseDropDown();
            // The same actual contexts also check the three unaffected families.
            // They contain no Showcase resources or internal API access.
            foreach (var (family, theme) in new[] { ("2013", RibbonThemes.Office2013Light), ("2019", RibbonThemes.Office2019Light), ("2024", RibbonThemes.Office2024Light) })
            {
                using var sheet = new Bitmap(surfaceBounds.Width, surfaceBounds.Height); using var g = Graphics.FromImage(sheet);
                painter.DrawApplicationMenuBackground(background with { Graphics = g, Theme = theme }, navigation, pane, footerBounds);
                painter.DrawMenuHeader(header with { Graphics = g, Theme = theme }, headerText);
                painter.DrawItem(footerContext with { Graphics = g, Theme = theme, Hot = false, Pressed = false }, footer, RibbonItemSize.Medium, true);
                using var bytes = new MemoryStream(); sheet.Save(bytes, System.Drawing.Imaging.ImageFormat.Png); hashes[family] = Convert.ToHexString(SHA256.HashData(bytes.ToArray()));
                sheet.Save(Path.Combine(directory, "unchanged-" + family + ".png"));
            }
            File.WriteAllText(Path.Combine(directory, "measurements.json"), JsonSerializer.Serialize(new { Name = name, Dpi = ribbon.DeviceDpi, HeadingMatchesPane = headingMatches, FooterSamples = samples, StateBorderPixels = borderPixels, LegacyHashes = hashes, Geometry = geometry }, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
            if (current) Require(headingMatches && samples["footer-0"].Distinct().Count() > 1 && borderPixels.Values.All(n => n > 0), "Default header matches page, idle footer is dimensional and split/footer states have outlines");
            File.WriteAllText(Path.Combine(directory, "result.log"), $"PASS: {name}; actual native menu, public renderer hooks, default/selected pane, split/footer state captures at {ribbon.DeviceDpi} DPI. LIMIT: offscreen/native print evidence; physical input, actual VS and visual acceptance remain separate.");
        }
        finally { ribbon.FileMenu.CloseDropDown(); ribbon.Renderer = previousRenderer; }
        Bitmap Capture(string file)
        { var control = Surface(); var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(directory, file + ".png")); return image; }
    }

    private sealed class Recorder : RibbonOffice2024Renderer
    {
        internal readonly Dictionary<RibbonItem, RibbonPaintContext> Items = [];
        internal readonly Dictionary<RibbonSplitButtonItem, (RibbonPaintContext Primary, RibbonPaintContext Arrow)> Splits = [];
        internal Rectangle Navigation, Pane, Footer; internal RibbonPaintContext Background, Header; internal string HeaderText = "";
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { if (menu) Items[item] = c; base.DrawItem(c, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext p, RibbonPaintContext a, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { if (menu) Splits[item] = (p, a); base.DrawSplitItem(p, a, item, size, menu); }
        public override void DrawApplicationMenuBackground(RibbonPaintContext c, Rectangle n, Rectangle p, Rectangle f) { Background = c; Navigation = n; Pane = p; Footer = f; base.DrawApplicationMenuBackground(c, n, p, f); }
        public override void DrawMenuHeader(RibbonPaintContext c, string text) { Header = c; HeaderText = text; base.DrawMenuHeader(c, text); }
    }
    private static Control Surface()
    {
        Control? found = null;
        EnumThreadWindows(GetCurrentThreadId(), (hwnd, _) => { if (Control.FromHandle(hwnd) is ToolStripDropDown window && window.Visible) found = window.Items.OfType<ToolStripControlHost>().Select(i => i.Control).FirstOrDefault(c => c.GetType().Name == "RibbonPopupSurface") ?? found; return true; }, IntPtr.Zero);
        return found ?? throw new InvalidOperationException("Expected own native Application Menu surface");
    }
    private static AccessibleObject Child(AccessibleObject root, string text)
    { for (int i = 0; i < root.GetChildCount(); i++) if (root.GetChild(i) is { } child && child.Name == text) return child; throw new InvalidOperationException("Missing menu accessibility row " + text); }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static int Count(Bitmap image, Color ink) { int count = 0; for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() == ink.ToArgb()) count++; return count; }
    private delegate bool WindowCallback(IntPtr hwnd, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint threadId, WindowCallback callback, IntPtr parameter);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
}
