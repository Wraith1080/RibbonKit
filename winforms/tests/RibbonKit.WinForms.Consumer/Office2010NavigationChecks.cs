using System.Text.Json;
using RibbonKit.WinForms;

namespace IndependentConsumer;

internal static class Office2010NavigationChecks
{
    private static readonly (string Name, Color Color)[] Accents =
    [ ("Blue", Color.FromArgb(30, 90, 156)), ("Red", Color.FromArgb(180, 40, 64)), ("Green", Color.FromArgb(33, 115, 70)),
      ("Purple", Color.FromArgb(128, 57, 123)), ("Teal", Color.FromArgb(0, 112, 112)), ("Orange", Color.FromArgb(168, 75, 0)) ];
    internal static int Run(string directory, bool requireFixed)
    {
        Directory.CreateDirectory(directory);
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
            using var renderer = new RibbonOffice2024Renderer(); using var sheet = new Bitmap(320, 60 * Accents.Length); using var graphics = Graphics.FromImage(sheet);
            var results = new List<Measurement>();
            foreach (var (name, accent) in Accents)
            {
                using var item = new RibbonButtonItem { Text = name }; using var row = new Bitmap(320, 56); using var canvas = Graphics.FromImage(row);
                renderer.DrawBackstageEntry(new(canvas, new(Point.Empty, row.Size), RibbonThemes.Office2010Blue with { Accent = accent }, SystemFonts.MessageBoxFont!, 96), item, true);
                var result = Measure(name, accent, row); results.Add(result); if (requireFixed) Check(result);
                graphics.DrawImageUnscaled(row, 0, results.Count * 60 - 60);
            }
            sheet.Save(Path.Combine(directory, "selected-rows.png"));
            File.WriteAllText(Path.Combine(directory, "measurements.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            if (requireFixed)
            {
                using var notes = new RichTextBox { Text = "Independent page notes", Dock = DockStyle.Fill };
                using var form = new RibbonForm(); using var ribbon = new RibbonControl(); var tab = new RibbonTab { Text = "Home" }; ribbon.Tabs.Add(tab);
                ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = notes }); ribbon.FileMenu.Visible = true; form.Controls.Add(ribbon); form.Ribbon = ribbon;
                VerifySample(form, ribbon, Path.Combine(directory, "native"), "independent");
            }
            File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: " + (requireFixed ? "six selected-ramp hue/smoothness checks and native public consumer" : "retained package color/band measurements captured")); return 0;
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(directory, "failure.log"), error.ToString()); return 1; }
    }
    internal static void VerifySample(RibbonForm form, RibbonControl ribbon, string directory, string name)
    {
        Directory.CreateDirectory(directory); ribbon.AnimationLevel = RibbonAnimationLevel.None;
        var galleries = ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).OfType<RibbonGalleryItem>().ToArray();
        var themeTile = galleries.SelectMany(g => g.Items).OfType<RibbonGalleryTile>().FirstOrDefault(t => t.Text == "Office 2010 Blue");
        if (themeTile is not null) themeTile.PerformClick(); else ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2010Blue;
        ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
        form.StartPosition = FormStartPosition.Manual; form.Bounds = new(-16000, -16000, 1000, 700); form.ShowInTaskbar = false; form.Show(); Application.DoEvents();
        var page = ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>().First(p => p.Content is not null && p.Visible);
        ribbon.IsBackstageOpen = true; ribbon.Backstage.SelectedPage = page; Application.DoEvents();
        var pane = page.Content!; while (pane.Parent is { } parent && !ReferenceEquals(parent, form)) pane = parent;
        var row = Descendants(pane).Single(c => c is Button && c.AccessibleName == page.Text);
        var state = Descendants(page.Content!).OfType<TextBoxBase>().Select(c => (Control: c, Handle: c.Handle, Parent: c.Parent, c.Text, c.SelectionStart, c.SelectionLength, Rtf: (c as RichTextBox)?.Rtf)).ToArray();
        var measurements = new List<Measurement>();
        foreach (var (colorName, color) in Accents)
        {
            ribbon.IsBackstageOpen = false;
            var accentGallery = galleries.FirstOrDefault(g => g.Id.Contains("accent", StringComparison.OrdinalIgnoreCase) || g.Text.Contains("Accent", StringComparison.OrdinalIgnoreCase));
            var tile = accentGallery?.Items.OfType<RibbonGalleryTile>().FirstOrDefault(t => t.Text == colorName);
            if (tile is null) ribbon.Theme = RibbonThemes.Office2010Blue with { Accent = color };
            else { ribbon.SelectedTab = ribbon.Tabs.Single(t => t.Groups.Any(g => g.Items.Contains(accentGallery!))); tile.PerformClick(); }
            ribbon.IsBackstageOpen = true; ribbon.Backstage.SelectedPage = page; Application.DoEvents();
            using var image = new Bitmap(row.Width, row.Height); row.DrawToBitmap(image, new(Point.Empty, image.Size));
            var result = Measure(colorName, ribbon.Theme.Accent, image); Check(result); measurements.Add(result); image.Save(Path.Combine(directory, name + "-" + colorName + ".png"));
            foreach (var s in state) Require(s.Control.Handle == s.Handle && ReferenceEquals(s.Control.Parent, s.Parent) && s.Control.Text == s.Text && s.Control.SelectionStart == s.SelectionStart && s.Control.SelectionLength == s.SelectionLength && (s.Rtf is null || ((RichTextBox)s.Control).Rtf == s.Rtf), "Native page ownership/RTF/selection");
        }
        ribbon.IsBackstageOpen = false;
        File.WriteAllText(Path.Combine(directory, "measurements.json"), JsonSerializer.Serialize(measurements, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: " + name + "; six actual accent selections, live selected-row hue/smoothness, native editor handles/parents/text/RTF/selection. LIMIT: direct paint/offscreen native checks; final visual and actual VS review remain separate.");
    }
    private sealed record Measurement(string Accent, double MaximumHueError, double MaximumLightnessDrop);
    private static Measurement Measure(string name, Color accent, Bitmap row)
    {
        double hueError = 0, drop = 0, previous = -1;
        for (int y = 2; y < row.Height - 2; y++)
        {
            var pixel = row.GetPixel(row.Width * 4 / 5, y); double distance = Math.Abs(pixel.GetHue() - accent.GetHue()); hueError = Math.Max(hueError, Math.Min(distance, 360 - distance));
            if (previous >= 0) drop = Math.Max(drop, previous - pixel.GetBrightness()); previous = pixel.GetBrightness();
        }
        return new(name, hueError, drop);
    }
    private static void Check(Measurement result) => Require(result.MaximumHueError <= 4 && result.MaximumLightnessDrop <= 1d / 255, $"Selected ramp {result}");
    private static IEnumerable<Control> Descendants(Control root) { foreach (Control child in root.Controls) { yield return child; foreach (var c in Descendants(child)) yield return c; } }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
