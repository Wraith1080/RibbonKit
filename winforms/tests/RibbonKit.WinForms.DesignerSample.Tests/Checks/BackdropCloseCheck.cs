using System.Diagnostics;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Shown package-only consumer: capture only these two owned windows while the
// native close transition runs. Never capture a different foreground app.
internal static class BackdropCloseCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        using var background = new Form { StartPosition = FormStartPosition.Manual, Bounds = new(180, 130, 1000, 700),
            FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false, BackColor = Color.FromArgb(220, 228, 236) };
        Exception? failure = null;
        background.Shown += async (_, _) =>
        {
            try
            {
                foreach (var material in new[] { RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic })
                {
                    using var form = new CloseProbe { Text = "Backdrop close regression", Backdrop = material, StartPosition = FormStartPosition.Manual, Bounds = new(205, 155, 900, 600) };
                    var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None };
                    var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" }; group.Items.Add(new RibbonButtonItem { Text = "Apply" }); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
                    form.Controls.Add(ribbon); form.Ribbon = ribbon;
                    var content = new RibbonBackdropPanel { Bounds = new(30, 320, 600, 160), BackColor = Color.White };
                    content.Controls.Add(new TextBox { Text = "Native document stays opaque", Bounds = new(20, 25, 420, 30) }); form.Controls.Add(content);
                    form.Show(background); form.Activate(); await Task.Delay(180);
                    Require(form.EffectiveBackdrop == material, "Material unavailable on this machine");
                    nint window = form.Handle, backdropWindow = background.Handle; int dpi = form.DeviceDpi;
                    bool cancel = true; form.FormClosing += (_, args) => args.Cancel = cancel;
                    form.Close(); Require(!form.IsDisposed && form.EffectiveBackdrop == material && form.Handle == window, "Cancelled close preserves material/handle"); cancel = false;
                    var area = form.RectangleToScreen(form.ClientRectangle);
                    Require(GetForegroundWindow() == window, "Foreground changed before capture");
                    using var ready = new ManualResetEventSlim(); var frames = new List<(double Time, Bitmap Image)>();
                    var capture = Task.Run(() =>
                    {
                        var clock = Stopwatch.StartNew();
                        for (int index = 0; index < 48; index++)
                        {
                            nint foreground = GetForegroundWindow();
                            Require(foreground == window || foreground == backdropWindow, "Foreground changed during owned-window capture");
                            var pixels = new Bitmap(area.Width, area.Height);
                            try { using var graphics = Graphics.FromImage(pixels); graphics.CopyFromScreen(area.Location, Point.Empty, area.Size); }
                            catch { pixels.Dispose(); throw; }
                            frames.Add((clock.Elapsed.TotalMilliseconds, pixels));
                            if (index == 4) ready.Set();
                            Thread.Sleep(8);
                        }
                    });
                    try
                    {
                        Require(await Task.Run(() => ready.Wait(2000)), "Capture did not start");
                        form.Observe = true; form.Close(); await capture;
                        int worst = 0;
                        for (int index = 0; index < frames.Count; index++)
                        {
                            var frame = frames[index]; int dark = 0, total = 0;
                            // Away from caption glyphs, frame edges and buttons.
                            for (int y = 6 * dpi / 96; y < 26 * dpi / 96; y += 2)
                            for (int x = 190 * dpi / 96; x < area.Width - 160 * dpi / 96; x += 3)
                            { var p = frame.Image.GetPixel(x, y); if (p.R < 35 && p.G < 35 && p.B < 35) dark++; total++; }
                            int black = total == 0 ? 0 : 100 * dark / total; worst = Math.Max(worst, black);
                            frame.Image.Save(Path.Combine(output, $"{material}-{index:00}.png"));
                            log.WriteLine($"{material} frame={index:00} t={frame.Time:F1}ms black={black}%");
                        }
                        int expected = material == RibbonWindowBackdrop.Mica ? 2 : 3;
                        // Attribute 39 accepts setting on this OS but its getter
                        // reports unsupported. The component test also observes
                        // the managed alpha mode at native destruction.
                        bool passed = form.MaterialAtDestroy == expected && worst < 30;
                        log.WriteLine($"{(passed ? "PASS" : "FAIL")}: {material}; DPI={dpi}; native at WM_DESTROY={form.MaterialAtDestroy}, bitmap alpha={form.AlphaAtDestroy}; worst sampled caption black={worst}%; cancelled close retained handle.");
                        if (!passed) failure ??= new InvalidOperationException("Backdrop was released before native destruction or a black closing frame was sampled: " + material);
                    }
                    finally
                    {
                        try { await capture; } finally { foreach (var frame in frames) frame.Image.Dispose(); }
                    }
                }
            }
            catch (Exception error) { failure = error; log.WriteLine("FAIL: " + error); }
            finally { background.Close(); }
        };
        Application.Run(background);
        if (failure != null) throw failure;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class CloseProbe : RibbonForm
    {
        internal bool Observe;
        internal int MaterialAtDestroy = -1, AlphaAtDestroy = -1;
        protected override void WndProc(ref Message message)
        {
            if (Observe && message.Msg == 0x2)
            { _ = DwmGetWindowAttribute(message.HWnd, 38, out MaterialAtDestroy, sizeof(int)); _ = DwmGetWindowAttribute(message.HWnd, 39, out AlphaAtDestroy, sizeof(int)); }
            base.WndProc(ref message);
        }
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
}
