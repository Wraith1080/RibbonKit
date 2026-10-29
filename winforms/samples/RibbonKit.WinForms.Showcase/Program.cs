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
        if (args.Length == 2 && args[0] == "--capture-document-hover")
            form.Shown += async (_, _) => { await Task.Yield(); try { form.Backdrop = RibbonWindowBackdrop.Acrylic; await form.CaptureMotionInput(args[1], hoverOnly: true); } finally { form.Close(); } };
        if (args.Length == 2 && args[0] == "--capture-motion-backdrop-tail")
            form.Shown += async (_, _) => { await Task.Yield(); try { form.Backdrop = RibbonWindowBackdrop.Acrylic; await form.CaptureMotionInput(args[1], tailOnly: true); } finally { form.Close(); } };
        if (args.Length == 2 && args[0] == "--capture-motion-backdrop-interactions")
            form.Shown += async (_, _) => { await Task.Yield(); try { form.Backdrop = RibbonWindowBackdrop.Acrylic; await form.CaptureMotionInput(args[1], interactionsOnly: true); } finally { form.Close(); } };
        if (args.Length == 2 && args[0] == "--capture-backdrop-density")
            form.Shown += async (_, _) => { await Task.Yield(); try { await form.CaptureBackdropDensity(args[1]); } finally { form.Close(); } };
        if (args.Length == 2 && args[0] == "--capture-backdrop-menu")
            form.Shown += async (_, _) => { await Task.Yield(); try { await form.CaptureBackdropMenu(args[1]); } finally { form.Close(); } };
        if (args.Length == 2 && args[0] == "--capture-backdrops")
            form.Shown += async (_, _) => { await Task.Yield(); try { await form.CaptureBackdrops(args[1]); } finally { form.Close(); } };
        if (args.Length == 2 && args[0] == "--capture-motion-interactions")
            form.Shown += async (_, _) => { await Task.Yield(); try { await form.CaptureMotionInput(args[1], interactionsOnly: true); } finally { form.Close(); } };
        if (args.Length == 2 && args[0] == "--capture-motion-input-tail")
            form.Shown += async (_, _) => { await Task.Yield(); try { await form.CaptureMotionInput(args[1], tailOnly: true); } finally { form.Close(); } };
        if (args.Length == 2 && args[0] == "--capture-motion-input")
            form.Shown += async (_, _) => { await Task.Yield(); try { await form.CaptureMotionInput(args[1]); } finally { form.Close(); } };
        if (args.Length == 2 && args[0] == "--capture-backstage-motion")
            form.Shown += async (_, _) => { await Task.Yield(); try { await form.CaptureBackstageMotion(args[1]); } finally { form.Close(); } };
        if (args.Length == 2 && args[0] == "--capture-view")
            form.Shown += async (_, _) => { await Task.Yield(); await form.CaptureView(args[1]); form.Close(); };
        if (args.Length == 2 && args[0] == "--capture-review")
            form.Shown += async (_, _) => { await Task.Yield(); await form.CaptureReview(args[1]); form.Close(); };
        if (args.Length == 2 && args[0] == "--capture-review-followup")
            form.Shown += async (_, _) => { await Task.Yield(); await form.CaptureReviewFollowup(args[1]); form.Close(); };
        if (args.Length == 2 && args[0] == "--capture-tab-scrollers")
            form.Shown += async (_, _) => { await Task.Yield(); await form.CaptureReviewFollowup(args[1], true); form.Close(); };
        if (args.Length == 2 && args[0] == "--capture-simplified")
            form.Shown += async (_, _) => { await Task.Yield(); await form.CaptureSimplified(args[1]); form.Close(); };
        if (args.Length == 2 && args[0] == "--capture-command-icons")
            form.Shown += (_, _) => form.BeginInvoke(() => { form.CaptureCommandIcons(args[1]); form.Close(); });
        if (args.Length == 2 && args[0] is "--capture-options" or "--capture-options-touch")
            form.Shown += async (_, _) => { await Task.Yield(); await form.CaptureOptions(args[1], args[0] == "--capture-options-touch"); form.Close(); };
        if (args.Length == 2 && args[0] == "--capture-customization")
            form.Shown += async (_, _) => { await Task.Yield(); await form.CaptureCustomization(args[1]); form.Close(); };
        if (args.Length == 2 && args[0] == "--capture-messages")
            form.Shown += async (_, _) => { await Task.Yield(); await form.CaptureMessages(args[1]); form.Close(); };
        if (args.Length == 2 && args[0] == "--capture-command-menus")
            form.Shown += async (_, _) => { await Task.Yield(); await form.CaptureCommandMenus(args[1]); form.Close(); };
        if (args.Length == 2 && args[0] == "--capture-density")
            form.Shown += async (_, _) => { await Task.Yield(); await form.CaptureDensity(args[1]); form.Close(); };
        if (args.Length == 2 && args[0] == "--capture-modal")
            form.Shown += async (_, _) => { await Task.Yield(); await form.CaptureModal(args[1]); form.Close(); };
        if (args.Length == 2 && args[0] == "--capture-galleries")
            form.Shown += async (_, _) => { await Task.Yield(); await form.CaptureGalleries(args[1]); form.Close(); };
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
