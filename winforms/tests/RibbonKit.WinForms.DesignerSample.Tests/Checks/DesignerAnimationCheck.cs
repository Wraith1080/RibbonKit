using System.Diagnostics;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Exercise this sample's compiled initializer and host manifest, using only
// public APIs. The offscreen Form runs its real native windows and UI timer.
internal static class DesignerAnimationCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        using var trace = new TextWriterTraceListener(log);
        Trace.Listeners.Add(trace);
        try
        {
            using var form = new DesignerForm { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000) };
            form.Shown += async (_, _) =>
            {
                try
                {
                    await Task.Delay(100);
                    var ribbon = form.Controls.OfType<RibbonControl>().Single();
                    log.WriteLine("Authored animation level: " + ribbon.AnimationLevel);
                    bool queried = SystemParametersInfo(0x1042, 0, out bool enabled, 0);
                    log.WriteLine($"Windows client-area animation: queried={queried}, enabled={enabled}");
                    if (!queried || !enabled) throw new InvalidOperationException("Windows animation is disabled; native motion cannot be verified without changing the user's policy.");
                    if (ribbon.AnimationLevel == RibbonAnimationLevel.None) throw new InvalidOperationException("Select Subtle or Expressive in this sample before running the animation check.");
                    var next = ribbon.Tabs.First(tab => tab.Visible && tab.Enabled && !ReferenceEquals(tab, ribbon.SelectedTab));
                    ribbon.SelectedTab = next;
                    await Check("Tab switch", ribbon);
                    ribbon.IsBackstageOpen = true;
                    await Check("Backstage opening", ribbon);
                    var pages = ribbon.Backstage.Items.Concat(ribbon.Backstage.FooterItems).OfType<RibbonBackstagePageItem>().Where(page => page.Visible && page.Enabled && page.Content is not null).ToArray();
                    if (pages.Length < 2) throw new InvalidOperationException("Keep two available native pages for the page animation check.");
                    var native = pages.Select(page => (Page: page, Content: page.Content!, Parent: page.Content!.Parent, Handle: page.Content!.Handle)).ToArray();
                    // Check mode only: include real rich-text HWNDs without
                    // changing the user's authored Forms or saved resources.
                    var richFields = pages.Select(page =>
                    {
                        var field = new RichTextBox { Text = "Retain rich text during page motion", Bounds = new(20, 220, 300, 90) };
                        page.Content!.Controls.Add(field); _ = field.Handle; field.Select(2, 5);
                        return (Field: field, Handle: field.Handle, Parent: field.Parent, Rtf: field.Rtf);
                    }).ToArray();
                    foreach (var style in Enum.GetValues<RibbonTabContentAnimation>())
                    {
                        ribbon.TabContentAnimation = style;
                        ribbon.Backstage.SelectedPage = pages.First(page => !ReferenceEquals(page, ribbon.Backstage.SelectedPage));
                        await Check("Backstage page " + style, ribbon);
                        foreach (var entry in native)
                            if (!ReferenceEquals(entry.Page.Content, entry.Content) || !ReferenceEquals(entry.Content.Parent, entry.Parent) || entry.Content.Handle != entry.Handle)
                                throw new InvalidOperationException("Page animation changed native content ownership/handles.");
                        foreach (var entry in richFields)
                            if (entry.Field.Handle != entry.Handle || !ReferenceEquals(entry.Field.Parent, entry.Parent) || entry.Field.Rtf != entry.Rtf || entry.Field.SelectionStart != 2 || entry.Field.SelectionLength != 5)
                                throw new InvalidOperationException("Page animation changed native rich-text ownership/data/selection.");
                    }
                    ribbon.IsBackstageOpen = false;
                    await Check("Backstage closing", ribbon);
                    log.WriteLine("PASS: compiled authored animation level starts/completes native tab, Backstage opening/closing and Fade/SlideDown page motion; native page/rich-text owners, handles, RTF and selection retained; public package APIs only. This is not visual or frame-pacing acceptance.");
                }
                catch (Exception error) { log.WriteLine("FAIL: " + error); Environment.ExitCode = 1; }
                finally { form.Close(); }
            };
            Application.Run(form);
        }
        finally { Trace.Listeners.Remove(trace); }

        async Task Check(string action, RibbonControl ribbon)
        {
            log.WriteLine(action + " starts: IsAnimating=" + ribbon.IsAnimating);
            if (!ribbon.IsAnimating) throw new InvalidOperationException(action + " did not start animation.");
            var elapsed = Stopwatch.StartNew(); int samples = 0;
            while (ribbon.IsAnimating && elapsed.ElapsedMilliseconds < 3000) { samples++; await Task.Delay(15); }
            log.WriteLine($"{action} completed: IsAnimating={ribbon.IsAnimating}, samples={samples}, elapsedMs={elapsed.ElapsedMilliseconds}");
            if (ribbon.IsAnimating) throw new InvalidOperationException(action + " did not complete.");
        }
    }
    [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint parameter, out bool value, uint flags);
}
