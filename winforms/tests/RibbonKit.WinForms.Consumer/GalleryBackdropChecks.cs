using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public-only coverage: no Showcase types, styles, icons or paint helpers.
internal static class GalleryBackdropChecks
{
    internal static int Run(string directory)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        Directory.CreateDirectory(directory); List<string> log = []; Exception? failure = null;
        using var renderer = new LabelRecorder();
        using var form = new RibbonForm { Width = 1150, Height = 600, Text = "Independent gallery/material consumer", StartPosition = WF.FormStartPosition.CenterScreen };
        using var ribbon = new RibbonControl { Renderer = renderer, AnimationLevel = RibbonAnimationLevel.None };
        var gallery = new RibbonGalleryItem { Id = "styles", Text = "Styles", IsInRibbon = true, InRibbonColumnCount = 3, TileWidth = 110, TileHeight = 90 };
        foreach (var label in new[] { "Normal", "Emphasis", "Strong" }) gallery.Items.Add(new RibbonGalleryTile { Text = label, Icon = new LegacyGdiIcon() });
        gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0];
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Styles" }; group.Items.Add(gallery); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon); form.Ribbon = ribbon;
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); await Task.Delay(150);
                foreach (var density in Enum.GetValues<RibbonDensity>())
                {
                    ribbon.Density = density; form.Backdrop = RibbonWindowBackdrop.None; ribbon.Refresh(); await Task.Delay(120);
                    var surface = Descendants(ribbon).Single(c => c.Visible && c.AccessibleName == "Styles gallery strip");
                    nint handle = surface.Handle; var baseline = Capture(surface, density + "-Solid");
                    var ink = renderer.Labels.ToDictionary(p => p.Key, p => CountInk(baseline, p.Value)); baseline.Dispose();
                    if (ink.Count != 3 || ink.Values.Any(count => count < 20)) throw new InvalidOperationException("The solid fixture did not show all three labels.");
                    foreach (var material in new[] { RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic })
                    {
                        form.Backdrop = material; ribbon.Refresh(); await Task.Delay(150);
                        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) && !WF.SystemInformation.HighContrast && form.EffectiveBackdrop != material)
                            throw new InvalidOperationException("The requested native material did not activate.");
                        using var actual = Capture(surface, density + "-" + material);
                        foreach (var (label, bounds) in renderer.Labels)
                        {
                            int count = CountInk(actual, bounds);
                            if (count < ink[label] * .85 || count > ink[label] * 1.15) throw new InvalidOperationException($"{density}/{material}: {label} lost its dark label ink ({count}/{ink[label]}).");
                            log.Add($"PASS: {density}/{material}/{label}: dark ink={count}, solid={ink[label]}");
                        }
                        if (surface.Handle != handle || !ReferenceEquals(gallery.SelectedItem, gallery.Items[0])) throw new InvalidOperationException("Material switching replaced the gallery HWND or selection.");
                    }
                }
                log.Add("PASS: independent gallery label opacity, native handle and selection across Solid/Mica/Acrylic and both densities; DPI=" + ribbon.DeviceDpi);
            }
            catch (Exception error) { failure = error; log.Add("FAIL: " + error); }
            finally { form.Close(); }
        };
        WF.Application.Run(form); File.WriteAllLines(Path.Combine(directory, "result.log"), log); return failure is null ? 0 : 1;

        Bitmap Capture(WF.Control surface, string name)
        {
            GetWindowThreadProcessId(GetForegroundWindow(), out uint foreground);
            if (foreground != Environment.ProcessId) throw new InvalidOperationException("Gallery consumer lost foreground; capture stopped.");
            var frame = new Bitmap(surface.Width, surface.Height);
            using (var graphics = Graphics.FromImage(frame)) graphics.CopyFromScreen(surface.PointToScreen(Point.Empty), Point.Empty, frame.Size);
            frame.Save(Path.Combine(directory, name + ".png")); return frame;
        }
    }
    private static int CountInk(Bitmap image, Rectangle bounds)
    {
        int count = 0; bounds = Rectangle.Intersect(bounds, new(Point.Empty, image.Size));
        for (int y = bounds.Top; y < bounds.Bottom; y++) for (int x = bounds.Left; x < bounds.Right; x++)
        { var pixel = image.GetPixel(x, y); if (pixel.R < 150 && pixel.G < 150 && pixel.B < 150) count++; }
        return count;
    }
    private static IEnumerable<WF.Control> Descendants(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private sealed class LabelRecorder : RibbonRenderer
    {
        internal readonly Dictionary<string, Rectangle> Labels = [];
        public override void DrawText(RibbonPaintContext context, string text, Color color, WF.TextFormatFlags flags)
        {
            if (text is "Normal" or "Emphasis" or "Strong") Labels[text] = context.Bounds;
            base.DrawText(context, text, color, flags);
        }
    }
    private sealed class LegacyGdiIcon : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        {
            using var font = new Font("Segoe UI", 12);
            WF.TextRenderer.DrawText(graphics, "Aa", font, bounds, context.Foreground, WF.TextFormatFlags.HorizontalCenter | WF.TextFormatFlags.VerticalCenter);
        }
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
}
