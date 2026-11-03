using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Read the composed scrollbar during the real entrance, not a requested repaint
// after settlement. Only the fixture's owned window/model is operated here.
internal static class GalleryScrollbarPaintCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        foreach (var backdrop in Enum.GetValues<RibbonWindowBackdrop>())
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            string name = backdrop + "-" + density;
            using var form = new DesignerForm { Backdrop = backdrop, StartPosition = FormStartPosition.Manual, Location = new(140, 100) };
            var ribbon = form.Ribbon!; var (tab, gallery) = form.GalleryPresenterExample;
            gallery.DropDownPresenterType = null; gallery.DropDownPresenterFactory = null;
            gallery.MaxVisibleRows = 2; ribbon.SelectedTab = tab; ribbon.Density = density; ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
            Exception? failure = null;
            form.Shown += async (_, _) =>
            {
                try
                {
                    form.Activate(); await Task.Delay(250);
                    log.WriteLine($"{name}: Requested={backdrop}; Effective={form.EffectiveBackdrop}; DPI={ribbon.DeviceDpi}");
                    Require(form.EffectiveBackdrop == backdrop, name + ": requested backdrop must be active");
                    for (int opening = 0; opening < 2; opening++)
                    {
                        gallery.ShowDropDown();
                        var popup = Control.FromHandle(GetForegroundWindow()) as ToolStripDropDown;
                        Require(popup is { Visible: true } && gallery.IsDropDownOpen, name + ": gallery is open");
                        Require(ribbon.IsAnimating, name + ": the entrance must actually animate");
                        var surface = popup!.Items.OfType<ToolStripControlHost>().Single().Control;
                        int barWidth = surface.Width - surface.ClientSize.Width;
                        Require(barWidth > 4, name + ": a native vertical scrollbar is required");
                        var bounds = popup.Bounds;
                        var origin = surface.PointToScreen(new(surface.ClientSize.Width + 2, 2)); origin.Offset(-bounds.X, -bounds.Y);
                        var bar = new Rectangle(origin, new(barWidth - 4, surface.ClientSize.Height - 4));
                        nint owner = form.Handle;
                        List<(Bitmap Image, double Dark)> frames = [];
                        using var stop = new CancellationTokenSource();
                        var worker = Task.Run(() =>
                        {
                            while (!stop.IsCancellationRequested)
                            {
                                var foreground = GetForegroundWindow();
                                Require(foreground == owner || GetAncestor(foreground, 3) == owner, name + ": fixture lost foreground");
                                _ = DwmFlush(); _ = GdiFlush();
                                var image = new Bitmap(bounds.Width, bounds.Height);
                                using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                                frames.Add((image, DarkFraction(image, bar)));
                            }
                        });
                        try { await Task.Delay(350); }
                        finally { stop.Cancel(); await worker; }
                        try
                        {
                            Require(frames.Count >= 3, name + ": capture multiple entrance/handoff frames");
                            double worst = frames.Max(frame => frame.Dark);
                            for (int i = 0; i < frames.Count; i++)
                                frames[i].Image.Save(Path.Combine(output, $"{name}-open{opening}-{i:00}.png"));
                            log.WriteLine($"{name} opening={opening}: samples={frames.Count}; maxDarkFraction={worst:F4}; settled={!ribbon.IsAnimating}");
                            Require(worst < .35, name + ": scrollbar became a dark band during entrance");
                            Require(!ribbon.IsAnimating && gallery.IsDropDownOpen, name + ": handoff keeps the same popup open");
                            gallery.CloseDropDown(); await Task.Delay(80);
                        }
                        finally { foreach (var frame in frames) frame.Image.Dispose(); }
                    }
                    log.WriteLine(name + ": PASS first opening and reopening");
                }
                catch (Exception error) { failure = error; }
                finally { form.Close(); }
            };
            Application.Run(form);
            if (failure is not null) throw new InvalidOperationException(name, failure);
        }
    }
    private static double DarkFraction(Bitmap image, Rectangle bar)
    {
        int dark = 0;
        for (int y = bar.Top; y < bar.Bottom; y++) for (int x = bar.Left; x < bar.Right; x++)
        { var pixel = image.GetPixel(x, y); if (pixel.R + pixel.G + pixel.B < 192) dark++; }
        return dark / (double)(bar.Width * bar.Height);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
    [DllImport("gdi32.dll")] private static extern bool GdiFlush();
}
