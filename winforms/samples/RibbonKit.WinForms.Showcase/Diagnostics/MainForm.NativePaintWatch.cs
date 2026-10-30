using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal void PrepareNativePaintWatch()
    {
        Text = "RibbonKit native paint review"; Backdrop = RibbonWindowBackdrop.Mica;
        StartPosition = FormStartPosition.Manual; Bounds = new(250, 150, 1204, 842); ribbon.SelectedTab = inputs;
    }
    internal async Task WatchNativePaint(string directory)
    {
        Directory.CreateDirectory(directory); await Task.Delay(300);
        var origin = PointToScreen(Point.Empty); var size = ClientSize;
        var regions = new List<(string Name, Rectangle Area)>();
        // Font tips naturally cover Zoom; watch the always-uncovered Font/Name
        // text so ordinary popup occlusion is not misclassified as a dropout.
        foreach (string id in new[] { fontInput.Id, nameInput.Id })
        {
            var field = Descendants(ribbon).Single(c => c.Name == id);
            var bounds = RectangleToClient(field.RectangleToScreen(field.ClientRectangle)); bounds.Width = Math.Min(130, bounds.Width - 24);
            regions.Add((id, bounds));
        }
        var doc = RectangleToClient(document.RectangleToScreen(document.ClientRectangle)); doc.Inflate(-8, -8);
        for (int y = doc.Top, band = 0; y < doc.Bottom; y += 100, band++)
            regions.Add(($"document-band-{band}", Rectangle.FromLTRB(doc.Left, y, doc.Right, Math.Min(doc.Bottom, y + 100))));
        var result = await Task.Run(() =>
        {
            int samples = 0, dropouts = 0; int[]? baseline = null, least = new int[regions.Count];
            using var image = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            var pixels = new int[size.Width * size.Height]; var timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < 22000)
            {
                GetWindowThreadProcessId(GetForegroundWindow(), out uint foreground);
                if (foreground != Environment.ProcessId) return "ABORT: review lost foreground; no further capture.";
                using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(origin, Point.Empty, size);
                var data = image.LockBits(new(Point.Empty, size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try { Marshal.Copy(data.Scan0, pixels, 0, pixels.Length); }
                finally { image.UnlockBits(data); }
                var counts = new int[regions.Count]; bool bad = false;
                for (int region = 0; region < regions.Count; region++)
                {
                    var area = regions[region].Area;
                    for (int y = area.Top; y < area.Bottom; y += 2) for (int x = area.Left; x < area.Right; x += 2)
                    { int p = pixels[y * size.Width + x]; if ((p & 255) < 130 && ((p >> 8) & 255) < 130 && ((p >> 16) & 255) < 130) counts[region]++; }
                    if (baseline is not null) { least[region] = Math.Min(least[region], counts[region]); if (baseline[region] >= 8 && counts[region] < baseline[region] * .65) bad = true; }
                }
                if (baseline is null)
                {
                    image.Save(Path.Combine(directory, "baseline.png"));
                    if (counts[0] < 8 || counts[1] < 8) return "FAIL: native field text absent in the initial watch frame.";
                    baseline = counts; least = (int[])counts.Clone();
                }
                if (bad) { if (dropouts < 8) image.Save(Path.Combine(directory, $"dropout-{dropouts}.png")); dropouts++; }
                samples++; Thread.Sleep(5);
            }
            image.Save(Path.Combine(directory, "last.png"));
            return $"{(dropouts == 0 ? "PASS" : "FAIL")}: actual native pointer watch; samples={samples}; dropout samples={dropouts}\r\n"
                + string.Join("\r\n", regions.Select((r, i) => $"{r.Name}: baseline={baseline![i]}, least={least[i]}"));
        });
        File.WriteAllText(Path.Combine(directory, "result.log"), result);
        if (!result.StartsWith("PASS", StringComparison.Ordinal)) Environment.ExitCode = 1;
    }
}
