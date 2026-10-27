using System.Diagnostics;
using System.Text.Json;

namespace RibbonKit.WinForms.Showcase;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        long start = Stopwatch.GetTimestamp();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        using var form = new MainForm();
        if (args.Length == 2 && args[0] == "--capture-backstage")
            form.Shown += async (_, _) => { await Task.Yield(); await form.CaptureBackstage(args[1]); form.Close(); };
        if (args.Length == 2 && args[0] == "--capture")
            form.Shown += (_, _) => form.BeginInvoke(() => { form.CaptureSnapshots(args[1]); form.Close(); });
        if (args.Length == 1 && args[0] == "--measure-startup")
            form.Shown += (_, _) => form.BeginInvoke(() =>
            {
                form.PaintRibbonForMeasurement();
                form.Update();
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    ManagedReadyMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds,
                    ProcessAgeMs = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMilliseconds
                }));
                form.Close();
            });
        Application.Run(form);
    }
}
