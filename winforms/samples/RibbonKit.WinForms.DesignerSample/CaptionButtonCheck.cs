using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Public package-only review. State messages go only to this fixture's own form;
// no caption command is invoked and no desktop input is injected.
internal static class CaptionButtonCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        using var background = new Form { StartPosition = FormStartPosition.Manual, Bounds = new(140, 100, 1180, 850),
            BackColor = Color.LightSteelBlue, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
        Exception? failure = null;
        background.Shown += async (_, _) =>
        {
            try
            {
                foreach (var material in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic })
                {
                    using var form = new StateForm { Backdrop = material, StartPosition = FormStartPosition.Manual, Location = new(165, 125) };
                    var ribbon = form.Controls.OfType<RibbonControl>().Single();
                    form.Show(background); form.Activate(); await Task.Delay(500);
                    if (form.EffectiveBackdrop != material) throw new InvalidOperationException("Requested material unavailable: " + material);
                    foreach (var (name, hit, pressed) in new[] { ("normal", 0, false), ("min-hover", 8, false), ("min-pressed", 8, true),
                        ("max-hover", 9, false), ("max-pressed", 9, true), ("close-hover", 20, false), ("close-pressed", 20, true) })
                    {
                        if (GetForegroundWindow() != form.Handle) throw new InvalidOperationException("Foreground changed; capture stopped.");
                        // Capture in the same state transaction; returning to
                        // the pump lets the real pointer clear a synthetic hover.
                        form.State(hit, pressed); form.Update(); ribbon.Update(); _ = GdiFlush(); _ = DwmFlush();
                        int S(int value) => (int)Math.Round(value * form.DeviceDpi / 96d);
                        var area = form.RectangleToScreen(new Rectangle(form.ClientSize.Width - S(148), 0, S(148), S(32)));
                        using var image = new Bitmap(area.Width, area.Height);
                        using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
                        image.Save(Path.Combine(output, $"{material}-{name}.png"));
                        log.WriteLine($"Captured {material}-{name}; DPI={form.DeviceDpi}; owned caption state only.");
                    }
                    form.State(0, false); form.Close();
                }
                log.WriteLine("PASS: 21 native caption state captures; Solid/Mica/Acrylic; no caption command invoked.");
            }
            catch (Exception error) { failure = error; log.WriteLine("FAIL: " + error); }
            finally { background.Close(); }
        };
        Application.Run(background); if (failure is not null) throw failure;
    }
    private sealed class StateForm : DesignerForm
    {
        internal void State(int hit, bool pressed)
        {
            Capture = false;
            var cancel = Message.Create(Handle, 0x1F, 0, 0); WndProc(ref cancel);
            var hover = Message.Create(Handle, 0xA0, hit, 0); WndProc(ref hover);
            if (pressed) { var down = Message.Create(Handle, 0xA1, hit, 0); WndProc(ref down); }
        }
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
    [DllImport("gdi32.dll")] private static extern bool GdiFlush();
}
