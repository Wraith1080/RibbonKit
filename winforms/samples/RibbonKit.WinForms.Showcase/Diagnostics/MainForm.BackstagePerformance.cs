using System.Diagnostics;
using System.Text.Json;

namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal async Task MeasureBackstagePerformance(string directory)
    {
        Directory.CreateDirectory(directory); var rows = new List<object>(); var cursor = Cursor.Position;
        using var process = Process.GetCurrentProcess();
        try
        {
            Activate(); SetForegroundWindow(Handle); await Task.Delay(180);
            foreach (var sample in new[]
            {
                ("restored-mica-clear", false, RibbonWindowBackdrop.Mica, 0f),
                ("max-solid", true, RibbonWindowBackdrop.None, 1f),
                ("max-mica-clear", true, RibbonWindowBackdrop.Mica, 0f),
                ("max-mica-opaque", true, RibbonWindowBackdrop.Mica, 1f),
                ("max-acrylic-clear", true, RibbonWindowBackdrop.Acrylic, 0f),
                ("max-acrylic-opaque", true, RibbonWindowBackdrop.Acrylic, 1f)
            })
            {
                ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsBackstageOpen = false;
                WindowState = FormWindowState.Normal; Bounds = new(200, 130, 1100, 740);
                Backdrop = sample.Item3; ribbon.Backstage.NavigationBackgroundOpacity = sample.Item4;
                if (sample.Item2) WindowState = FormWindowState.Maximized;
                await Task.Delay(100); Guard(); Cursor.Position = new(Right - 4, Top + 15);
                ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
                await Measure(true, false, 0); await Measure(false, false, 0);
                for (int trial = 1; trial <= 3; trial++) { await Measure(true, true, trial); await Measure(false, true, trial); }
                ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsBackstageOpen = true; await Task.Delay(60); Guard();
                var capture = Rectangle.Intersect(Bounds, SystemInformation.VirtualScreen);
                using (var image = new Bitmap(capture.Width, capture.Height))
                {
                    using var graphics = Graphics.FromImage(image); graphics.CopyFromScreen(capture.Location, Point.Empty, capture.Size);
                    image.Save(Path.Combine(directory, sample.Item1 + ".png"));
                }
                ribbon.IsBackstageOpen = false;

                async Task Measure(bool opening, bool record, int trial)
                {
                    Guard(); process.Refresh(); var cpu = process.TotalProcessorTime; long allocated = GC.GetTotalAllocatedBytes(false);
                    var watch = Stopwatch.StartNew(); double last = 0, maximumGap = 0; int ticks = 0;
                    using var timer = new System.Windows.Forms.Timer { Interval = 16 };
                    timer.Tick += (_, _) => { double now = watch.Elapsed.TotalMilliseconds; maximumGap = Math.Max(maximumGap, now - last); last = now; ticks++; };
                    timer.Start(); ribbon.IsBackstageOpen = opening;
                    while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 3000) await Task.Delay(1);
                    timer.Stop(); maximumGap = Math.Max(maximumGap, watch.Elapsed.TotalMilliseconds - last); Guard();
                    if (ribbon.IsAnimating || ribbon.IsBackstageOpen != opening) throw new InvalidOperationException("Backstage did not finish its requested transition.");
                    process.Refresh();
                    if (record) rows.Add(new { scene = sample.Item1, trial, opening, width = ClientSize.Width, height = ClientSize.Height,
                        dpi = DeviceDpi, effectiveBackdrop = EffectiveBackdrop.ToString(), elapsedMs = watch.Elapsed.TotalMilliseconds,
                        cpuMs = (process.TotalProcessorTime - cpu).TotalMilliseconds, allocatedBytes = GC.GetTotalAllocatedBytes(false) - allocated,
                        maximumUiGapMs = maximumGap, uiTicks = ticks });
                    await Task.Delay(40);
                }
            }
            File.WriteAllText(Path.Combine(directory, "result.json"), JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: actual Showcase restored/maximized opening and closing samples completed.");
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(directory, "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
        finally { Cursor.Position = cursor; }
        void Guard()
        {
            GetWindowThreadProcessId(GetForegroundWindow(), out uint processId);
            if (processId != Environment.ProcessId || !TraceVisible(Handle)) throw new InvalidOperationException("Showcase lost visible foreground; measurement stopped.");
        }
    }
}
