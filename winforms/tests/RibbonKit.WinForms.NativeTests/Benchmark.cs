using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using RibbonKit.WinForms.Layout;
using WF = System.Windows.Forms;

namespace RibbonKit.WinForms.NativeTests;

// Instrumentation may inspect layout/popups. The separate package consumer and
// external UIA client use public capabilities exclusively.
internal static class Benchmark
{
    private static readonly List<object> Resources = [];
    private static readonly List<object> Sweeps = [];
    private static readonly List<double> DropDowns = [], Groups = [], Nested = [];
    [DllImport("user32.dll")] private static extern int GetGuiResources(nint process, int flag);

    internal static int Run(string output, bool measureResize = true, int popupCycles = 100)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2);
        WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        using var form = new WF.Form { Text = "RibbonKit native baseline", Width = 1100, Height = 350, StartPosition = WF.FormStartPosition.CenterScreen };
        using var ribbon = new MeasuredRibbon();
        var home = new RibbonTab { Text = "Home" };
        var menu = new RibbonDropDownItem { Text = "Menu" };
        menu.Items.Add(new RibbonButtonItem { Text = "Choice" });
        for (int g = 0; g < 5; g++)
        {
            var group = new RibbonGroup { Text = "Group " + g, ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse };
            for (int i = 0; i < 6; i++)
                group.Items.Add(new RibbonButtonItem { Text = "Command " + i, SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small) });
            if (g == 0)
                group.Items.Add(menu);
            home.Groups.Add(group);
        }
        ribbon.Tabs.AddRange(home, new RibbonTab { Text = "Insert" });
        form.Controls.Add(ribbon);
        using var timer = new WF.Timer { Interval = 33 };
        var phase = new Stopwatch();
        var work = new List<double>();
        int pass = -1;
        long allocated = 0;
        TimeSpan cpu = default;
        Exception? failure = null;
        double firstDropDown = 0, firstGroup = 0;
        form.Shown += (_, _) => form.BeginInvoke(() => { ribbon.Refresh(); SampleResources("warmup"); if (measureResize) { StartSweep(); timer.Start(); } else MeasurePopups(); });
        void StartSweep()
        {
            pass++;
            work.Clear();
            ribbon.Reset();
            phase.Restart();
            allocated = GC.GetAllocatedBytesForCurrentThread();
            cpu = Process.GetCurrentProcess().TotalProcessorTime;
        }
        timer.Tick += (_, _) =>
        {
            try
            {
                if (phase.Elapsed.TotalSeconds < 30)
                {
                    double fraction = phase.Elapsed.TotalSeconds % 8 / 4;
                    int width = (int)(260 + 840 * (fraction <= 1 ? fraction : 2 - fraction));
                    long start = Stopwatch.GetTimestamp();
                    form.Width = width;
                    ribbon.Refresh();
                    work.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                    return;
                }
                Sweeps.Add(new
                {
                    Pass = pass + 1,
                    ElapsedMs = phase.Elapsed.TotalMilliseconds,
                    WorkMs = work.ToArray(),
                    ribbon.PaintCount,
                    ribbon.LayoutCount,
                    PaintMs = ribbon.PaintWork.ToArray(),
                    PaintIntervalsMs = ribbon.PaintIntervals.ToArray(),
                    AllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocated,
                    CpuMs = (Process.GetCurrentProcess().TotalProcessorTime - cpu).TotalMilliseconds
                });
                SampleResources("resize-" + (pass + 1));
                if (pass < 2)
                {
                    StartSweep();
                    return;
                }
                timer.Stop();
                MeasurePopups();
            }
            catch (Exception e) { failure = e; timer.Stop(); form.Close(); }
        };
        void MeasurePopups()
        {
            try
            {
                // Keep the opener expanded explicitly; Form.Width can be capped
                // by the current monitor's maximum tracking size.
                foreach (var group in home.Groups.Skip(1))
                    group.Visible = false;
                form.Width = 1100;
                ribbon.Refresh();
                for (int i = -1; i < popupCycles; i++)
                {
                    long start = Stopwatch.GetTimestamp();
                    menu.ShowDropDown();
                    if (ribbon.Popups.Top is not { } popup)
                        throw new InvalidOperationException("Expanded dropdown opener was not reachable.");
                    popup.Refresh();
                    if (i == -1)
                        firstDropDown = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    if (i >= 0)
                        DropDowns.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                    menu.CloseDropDown();
                    if (i is 0 or 24 or 49 or 99)
                        SampleResources("dropdown-" + (i + 1));
                }
                foreach (var group in home.Groups.Skip(1))
                    group.Visible = true;
                form.Width = 260;
                ribbon.Refresh();
                var opener = ribbon.Snapshot.Targets.First(t => t.Kind == TargetKind.Group);
                for (int i = -1; i < popupCycles; i++)
                {
                    long start = Stopwatch.GetTimestamp();
                    ribbon.Activate(opener);
                    ribbon.Popups.Top!.Refresh();
                    if (i == -1)
                        firstGroup = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    if (i >= 0)
                        Groups.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                    if (i % 10 == 0)
                    {
                        start = Stopwatch.GetTimestamp();
                        menu.ShowDropDown();
                        ribbon.Popups.Top!.Refresh();
                        Nested.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                        ribbon.Popups.CloseTop(true);
                    }
                    ribbon.Popups.CloseAll(true);
                    if (i is 0 or 24 or 49 or 99)
                        SampleResources("group-" + (i + 1));
                }
                if (ribbon.Popups.HasOpen || WF.Control.MouseButtons != WF.MouseButtons.None)
                    throw new InvalidOperationException("Popup/capture did not settle.");
                form.Close();
            }
            catch (Exception e) { failure = e; timer.Stop(); form.Close(); }
        }
        WF.Application.Run(form);
        ribbon.Dispose();
        form.Dispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        SampleResources("disposed-gc");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(output, JsonSerializer.Serialize(new
        {
            Date = DateTimeOffset.Now,
            Runtime = RuntimeInformation.FrameworkDescription,
            OS = RuntimeInformation.OSDescription,
            CpuCount = Environment.ProcessorCount,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            CommandCount = 32,
            Displays = WF.Screen.AllScreens.Select(s => new { s.DeviceName, Bounds = s.Bounds.ToString(), s.Primary }),
            Dpi = ribbon.DeviceDpi,
            ResizeSweeps = Sweeps,
            FirstDropDownMs = firstDropDown,
            FirstGroupMs = firstGroup,
            DropDownMs = DropDowns,
            GroupMs = Groups,
            NestedMs = Nested,
            Resources,
            Error = failure?.ToString(),
            Limit = "Instrumented native Update/paint completion; not compositor latency, frame-rate or leak certification. Resize is timer-paced, not human drag. Dropdown workload keeps one group visible; collapsed workload shows all five."
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(failure is null ? "PASS: requested native baseline scopes completed: " + output : failure);
        return failure is null ? 0 : 1;
    }
    private static void SampleResources(string stage)
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        Resources.Add(new
        {
            Stage = stage,
            ManagedBytes = GC.GetTotalMemory(false),
            process.PrivateMemorySize64,
            process.HandleCount,
            Gdi = GetGuiResources(process.Handle, 0),
            User = GetGuiResources(process.Handle, 1)
        });
    }
    private sealed class MeasuredRibbon : RibbonControl
    {
        internal int PaintCount, LayoutCount;
        internal readonly List<double> PaintWork = [], PaintIntervals = [];
        private long _lastPaint;
        internal void Reset()
        {
            PaintCount = LayoutCount = 0;
            PaintWork.Clear();
            PaintIntervals.Clear();
            _lastPaint = 0;
        }
        protected override void OnPaint(WF.PaintEventArgs e)
        {
            long start = Stopwatch.GetTimestamp();
            base.OnPaint(e);
            PaintCount++;
            PaintWork.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            if (_lastPaint != 0)
                PaintIntervals.Add(Stopwatch.GetElapsedTime(_lastPaint, start).TotalMilliseconds);
            _lastPaint = start;
        }
        protected override void OnLayout(WF.LayoutEventArgs e)
        {
            base.OnLayout(e);
            LayoutCount++;
        }
    }
}
