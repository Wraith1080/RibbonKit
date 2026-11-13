using RibbonKit.WinForms;

namespace IndependentConsumer;

// Intentionally public API only, copied into an isolated package consumer.
internal static class ThemeAccentAdaptationChecks
{
    internal static int Run(string directory, bool nativeOnly = false)
    {
        Directory.CreateDirectory(directory); List<string> log = [];
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
            if (!nativeOnly) foreach (bool office in new[] { false, true }) foreach (int dpi in new[] { 96, 120, 144, 192 }) Paint(office, dpi);
            foreach (bool flat in new[] { true, false }) Dialog(flat);
            log.Add(nativeOnly ? "LIMIT: offscreen dialogs and native messages at actual device DPI; no separate command/gallery paint scopes, physical input, mixed-monitor or Visual Studio acceptance."
                : "LIMIT: direct paint at four DPI values; native dialogs at actual device DPI. No physical input, mixed-monitor or Visual Studio acceptance.");
            File.WriteAllLines(Path.Combine(directory, "result.log"), log); return 0;
        }
        catch (Exception error) { log.Add("FAIL: " + error); File.WriteAllLines(Path.Combine(directory, "result.log"), log); return 1; }

        void Paint(bool office, int dpi)
        {
            int S(int n) => (int)Math.Round(n * dpi / 96d);
            using RibbonRenderer renderer = office ? new OfficeInk() : new NeutralInk(); var ink = (IInk)renderer;
            foreach (bool flat in new[] { true, false })
            {
                var basis = flat ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light;
                using var image = new Bitmap(S(750), S(390)); using var g = Graphics.FromImage(image); g.Clear(Color.White);
                int row = 0;
                foreach (var accent in new[] { Color.Purple, Color.Teal, Color.DarkRed })
                {
                    var theme = basis with { Accent = accent }; var icon = new IconInk();
                    using var toggle = new RibbonToggleItem { Text = "Toggle", Icon = icon };
                    using var tile = new RibbonGalleryTile { Text = "Gallery", Icon = icon };
                    int column = 0;
                    foreach (var (hot, pressed, check) in new[] { (false,false,false), (true,false,false), (true,true,false), (false,false,true), (true,false,true) })
                    {
                        toggle.Checked = check;
                        var rect = new Rectangle(S(10 + column * 145), S(8 + row * 128), S(135), S(36));
                        var c = new RibbonPaintContext(g, rect, theme, SystemFonts.MessageBoxFont!, dpi, Hot: hot, Pressed: pressed);
                        var expected = pressed ? basis.Pressed.Start : check ? Wash(accent, hot ? .28 : .18) : hot ? basis.Hover.Start : Color.White;
                        var foreground = basis.Foreground;
                        renderer.DrawItem(c, toggle, RibbonItemSize.Medium, false);
                        Require(image.GetPixel(rect.Left + S(6), rect.Top + S(6)).ToArgb() == expected.ToArgb(), "Actual toggle state fill");
                        Require(ink.Color == foreground && icon.Color == foreground, "Actual text/vector hooks follow content policy");
                        rect.Y += S(40); rect.Height = S(72); c = c with { Bounds = rect };
                        renderer.DrawGalleryTile(c, tile, check);
                        Require(image.GetPixel(rect.Left + S(6), rect.Top + S(6)).ToArgb() == expected.ToArgb(), "Actual gallery selection/hover fill");
                        Require(ink.Color == foreground && icon.Color == foreground, "Gallery text/vector hooks");
                        column++;
                    }
                    if (office && !flat)
                    {
                        using var navImage = new Bitmap(S(240), S(60)); using var navGraphics = Graphics.FromImage(navImage);
                        var nav = new RibbonPaintContext(navGraphics, new(Point.Empty, navImage.Size), theme, SystemFonts.MessageBoxFont!, dpi);
                        using var tab = new RibbonTab { Text = "Active page" }; renderer.DrawTab(nav, tab, true);
                        Require(ink.Color == accent, "Active tab keeps its independent accent text");
                        using var page = new RibbonBackstagePageItem { Text = "Active page" }; renderer.DrawBackstageEntry(nav, page, true);
                        Require(ink.Color == accent, "Active Backstage navigation keeps its independent accent text");
                    }
                    var authored = theme with { Hover = new(Color.FromArgb(70, Color.Red), Color.Transparent), Checked = new(Color.Gold), CheckedHover = new(Color.Lime) };
                    var recolored = authored with { Accent = Color.Orange };
                    Require(authored.Hover == recolored.Hover && recolored.Checked.Start == Color.Gold && recolored.CheckedHover!.Value.Start == Color.Lime, "Explicit fills survive recoloring");
                    Require((recolored with { CheckedOverride = null }).Checked.Start == Wash(Color.Orange, .18), "Independent Checked Reset follows current accent");
                    row++;
                }
                image.Save(Path.Combine(directory, $"states-{(flat ? "2019" : "2024")}-{(office ? "office" : "neutral")}-{dpi}.png"));
            }
            log.Add($"PASS: public {(office ? "Office" : "neutral")} renderer/{dpi} DPI; both themes, three accents, grey hover/press, accent toggle/gallery and checked-hover; ordinary command text/vector hooks, independent active navigation ink, explicit fills and Reset.");
        }

        void Dialog(bool flat)
        {
            using var ribbon = new RibbonControl { Theme = flat ? RibbonThemes.Office2019Light : RibbonThemes.Office2024Light };
            using var dialog = new RibbonOptionsDialog(ribbon) { Text = "Accent configuration consumer", Bounds = new(-16000, -16000, 850, 590), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
            var page = new RibbonOptionsPage { Text = "General" }; var field = new TextBox { Text = "Retained native configuration", Location = new(20, 50), Width = 250 };
            page.Controls.Add(field); dialog.Pages.Add(page); dialog.Show(); Application.DoEvents(); field.Focus(); field.Select(2, 5);
            var buttons = Descendants(dialog).OfType<Button>().Where(b => b.Name is "AcceptCustomization" or "CancelCustomization" or "ApplyCustomization").ToArray();
            Require(buttons.Length == 3, "Actual OK/Cancel/Apply controls");
            var handles = buttons.Select(b => b.Handle).ToArray(); var bounds = buttons.Select(b => b.Bounds).ToArray(); var fieldHandle = field.Handle; var parent = field.Parent;
            foreach (var accent in new[] { Color.Purple, Color.Teal, Color.DarkRed })
            {
                ribbon.Theme = ribbon.Theme with { Accent = accent }; Application.DoEvents();
                foreach (var button in buttons)
                {
                    using var image = new Bitmap(button.Width, button.Height); button.DrawToBitmap(image, new(Point.Empty, image.Size));
                    Require(image.GetPixel(button.Width / 2, 5).ToArgb() == (button.Name == "AcceptCustomization" ? accent : ribbon.Theme.Surface).ToArgb(), "Only OK has a solid accent idle fill");
                    if (accent == Color.Purple) image.Save(Path.Combine(directory, $"button-{(flat ? "2019" : "2024")}-{button.Name}-idle.png"));
                    bool primary = button.Name == "AcceptCustomization";
                    SendMessage(button.Handle, 0x0200, 0, (5 << 16) | 5);
                    button.DrawToBitmap(image, new(Point.Empty, image.Size));
                    var hover = primary ? Darken(accent, .10) : Wash(accent, .10);
                    Require(image.GetPixel(button.Width / 2, 5).ToArgb() == hover.ToArgb(), "Primary dark hover and secondary light wash");
                    if (primary) WhiteInk();
                    if (accent == Color.Purple) image.Save(Path.Combine(directory, $"button-{(flat ? "2019" : "2024")}-{button.Name}-hover.png"));
                    button.Focus(); SendMessage(button.Handle, 0x0100, (nint)Keys.Space, 0);
                    button.DrawToBitmap(image, new(Point.Empty, image.Size));
                    Require(image.GetPixel(0, button.Height / 2).ToArgb() == accent.ToArgb(), "Native key press paints the accent border");
                    Require(image.GetPixel(button.Width / 2, 5).ToArgb() == (primary ? Darken(accent, .20) : Wash(accent, .20)).ToArgb(), "Native key press darkens OK further and keeps the secondary wash");
                    if (primary) WhiteInk();
                    if (accent == Color.Purple) image.Save(Path.Combine(directory, $"button-{(flat ? "2019" : "2024")}-{button.Name}-pressed.png"));
                    field.Focus(); // Losing focus cancels the held key without activating OK/Cancel/Apply.
                    SendMessage(button.Handle, 0x02A3, 0, 0);
                    void WhiteInk() => Require(Enumerable.Range(12, button.Width - 24).Any(x => Enumerable.Range(5, button.Height - 10).Any(y => image.GetPixel(x, y).ToArgb() == Color.White.ToArgb())), "OK keeps white text during interaction");
                }
                Require(handles.SequenceEqual(buttons.Select(b => b.Handle)) && bounds.SequenceEqual(buttons.Select(b => b.Bounds)), "Button handles and bounds retained");
                Require(field.Handle == fieldHandle && ReferenceEquals(parent, field.Parent) && field.Text == "Retained native configuration" && field.SelectionStart == 2 && field.SelectionLength == 5, "Native field state retained");
                Require(ReferenceEquals(dialog.AcceptButton, buttons.Single(b => b.Name == "AcceptCustomization")) && ReferenceEquals(dialog.CancelButton, buttons.Single(b => b.Name == "CancelCustomization")), "Native dialog actions retained");
                if (accent == Color.Purple)
                {
                    using var capture = new Bitmap(dialog.Width, dialog.Height); dialog.DrawToBitmap(capture, new(Point.Empty, capture.Size));
                    capture.Save(Path.Combine(directory, $"dialog-{(flat ? "2019" : "2024")}.png"));
                }
            }
            log.Add($"PASS: public {(flat ? "2019" : "2024")} options dialog/{dialog.DeviceDpi} actual DPI; three accents, OK darkens on hover/press with white text; secondary buttons retain white idle/light washes; native messages, accent press borders, handles, bounds, field selection and Accept/Cancel retained.");
        }
    }

    private static Color Wash(Color accent, double opacity) => Color.FromArgb((int)Math.Round(255 + (accent.R - 255) * opacity), (int)Math.Round(255 + (accent.G - 255) * opacity), (int)Math.Round(255 + (accent.B - 255) * opacity));
    private static Color Darken(Color accent, double amount) => Color.FromArgb((int)Math.Round(accent.R * (1 - amount)), (int)Math.Round(accent.G * (1 - amount)), (int)Math.Round(accent.B * (1 - amount)));
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint SendMessage(nint handle, uint message, nint wParam, nint lParam);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static IEnumerable<Control> Descendants(Control c) => c.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(Descendants(child)));
    private interface IInk { Color Color { get; } }
    private sealed class OfficeInk : RibbonOffice2024Renderer, IInk
    { public Color Color { get; private set; } public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { Color = color; base.DrawText(c, text, color, flags); } }
    private sealed class NeutralInk : RibbonRenderer, IInk
    { public Color Color { get; private set; } public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { Color = color; base.DrawText(c, text, color, flags); } }
    private sealed class IconInk : IRibbonIcon
    {
        internal Color Color;
        public void Draw(Graphics g, Rectangle bounds, RibbonIconContext c)
        { Color = c.Foreground; using var pen = new Pen(Color); g.DrawRectangle(pen, Rectangle.Inflate(bounds, -3, -3)); }
    }
}
