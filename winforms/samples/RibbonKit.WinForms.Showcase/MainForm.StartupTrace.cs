using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Showcase.Persistence;

namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal static string PrepareStartupTrace(string directory, string variant)
    {
        string settings = Path.Combine(directory, "isolated-preferences");
        if (Directory.Exists(settings)) throw new InvalidOperationException("Startup trace requires a fresh directory.");
        Directory.CreateDirectory(settings);
        string original = Path.Combine(DefaultSettingsDirectory, "preferences.json");
        var preferences = File.Exists(original) ? ShowcasePreferences.FromJson(File.ReadAllText(original)) : new ShowcasePreferences();
        var backdrop = variant switch
        {
            "solid" => RibbonWindowBackdrop.None,
            "mica" => RibbonWindowBackdrop.Mica,
            "acrylic" => RibbonWindowBackdrop.Acrylic,
            _ => throw new ArgumentException("Choose solid, mica or acrylic.", nameof(variant))
        };
        preferences = preferences with { Backdrop = backdrop,
            Window = new(250, 150, 1204, 842, 120, false) };
        File.WriteAllText(Path.Combine(settings, "preferences.json"), preferences.ToJson());
        return settings;
    }
    internal void InstallStartupTrace(string directory, string variant)
    {
        Task<string>? capture = null;
        Load += (_, _) =>
        {
            nint window = Handle, editor = document.Handle, status = statusStrip.Handle;
            string header = $"Variant={variant}; applied={EffectiveBackdrop}; native style=0x{TraceStyle(window).ToInt64():X}; Load starts capture before show\r\n";
            capture = Task.Run(() => TraceFirstFrames(directory, header, window, editor, status));
        };
        Shown += async (_, _) =>
        {
            try
            {
                if (capture is null) throw new InvalidOperationException("Load did not start the trace.");
                string result = await capture;
                File.WriteAllText(Path.Combine(directory, "result.log"), result);
                if (!result.EndsWith("blackFrames=0", StringComparison.Ordinal) || result.Contains("RESULT: presented=0;", StringComparison.Ordinal))
                    Environment.ExitCode = 1;
            }
            catch (Exception error) { File.WriteAllText(Path.Combine(directory, "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            finally { Close(); }
        };
    }
    private static string TraceFirstFrames(string directory, string header, nint window, nint editor, nint status)
    {
        var timer = Stopwatch.StartNew(); var rows = new List<string> { header, "ms,visible,presented,blackDocPercent,blackStatusPercent,greenCenter,visibleWidth,visibleHeight" };
        int frame = 0, blackFrames = 0, presented = 0;
        while (timer.ElapsedMilliseconds < 850)
        {
            TraceRect(window, out var rect); TraceRect(editor, out var doc); TraceRect(status, out var bar);
            int width = rect.Right - rect.Left, height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0) { Thread.Sleep(3); continue; }
            using var image = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(new(rect.Left, rect.Top), Point.Empty, image.Size);
            var data = image.LockBits(new(Point.Empty, image.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var pixels = new int[width * height];
            try { Marshal.Copy(data.Scan0, pixels, 0, pixels.Length); } finally { image.UnlockBits(data); }
            int pink = 0, total = 0, left = width, top = height, right = 0, bottom = 0, black = 0, paper = 0, blackStatus = 0, statusSamples = 0;
            for (int y = 4; y < height - 4; y += 4) for (int x = 4; x < width - 4; x += 4)
            {
                int p = pixels[y * width + x], r = (p >> 16) & 255, g = (p >> 8) & 255, b = p & 255;
                bool isPink = r > 240 && g < 15 && b > 240; total++;
                if (isPink) pink++; else { left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y); }
                if (x > doc.Left - rect.Left + 8 && x < doc.Right - rect.Left - 8 && y > doc.Top - rect.Top + 8 && y < doc.Bottom - rect.Top - 8)
                { paper++; if (r < 30 && g < 30 && b < 30) black++; }
                if (x > bar.Left - rect.Left + 4 && x < bar.Right - rect.Left - 4 && y > bar.Top - rect.Top + 4 && y < bar.Bottom - rect.Top - 4)
                { statusSamples++; if (r < 30 && g < 30 && b < 30) blackStatus++; }
            }
            bool visible = TraceVisible(window), shown = visible && pink < total * .8;
            GetWindowThreadProcessId(GetForegroundWindow(), out uint foreground);
            if (visible && foreground != Environment.ProcessId)
            {
                rows.Add("ABORT: trace lost foreground; discard this comparison.");
                return string.Join("\r\n", rows);
            }
            int dark = paper == 0 ? 0 : black * 100 / paper;
            int darkStatus = statusSamples == 0 ? 0 : blackStatus * 100 / statusSamples;
            if (shown) { presented++; if (dark > 25 || darkStatus > 25) blackFrames++; }
            int center = (pixels[(height / 2) * width + width / 2] >> 8) & 255;
            rows.Add($"{timer.ElapsedMilliseconds},{visible},{shown},{dark},{darkStatus},{center},{right - left},{bottom - top}");
            if (shown && (presented <= 8 || dark > 25 || darkStatus > 25)) image.Save(Path.Combine(directory, $"frame-{frame:D3}.png"));
            frame++; Thread.Sleep(4);
        }
        rows.Add($"RESULT: presented={presented}; blackFrames={blackFrames}"); return string.Join("\r\n", rows);
    }
    private static nint TraceStyle(nint window) => IntPtr.Size == 8 ? TraceGetStyle64(window, -16) : TraceGetStyle32(window, -16);
    [StructLayout(LayoutKind.Sequential)] private struct TraceRectangle { internal int Left, Top, Right, Bottom; }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint TraceGetStyle64(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern nint TraceGetStyle32(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "GetWindowRect")] private static extern bool TraceRect(nint window, out TraceRectangle rectangle);
    [DllImport("user32.dll", EntryPoint = "IsWindowVisible")] private static extern bool TraceVisible(nint window);
}
