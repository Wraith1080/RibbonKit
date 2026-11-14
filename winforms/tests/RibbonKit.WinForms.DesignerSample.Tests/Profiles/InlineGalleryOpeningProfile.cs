using System.Diagnostics;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Fresh-process first-tab timings; application controls and library are unchanged.
internal static class InlineGalleryOpeningProfile
{
    internal static void Run(string output, bool noMotion, bool builtIn)
    {
        Directory.CreateDirectory(output); using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        using var form = new DesignerForm { Location = new(120, 80), StartPosition = FormStartPosition.Manual };
        var ribbon = form.Controls.OfType<RibbonControl>().Single(); var (tab, gallery) = form.GalleryPresenterExample;
        var home = ribbon.Tabs.First(candidate => candidate != tab); ribbon.SelectedTab = home;
        if (noMotion) ribbon.AnimationLevel = RibbonAnimationLevel.None;
        gallery.InRibbonPresenterType = null;
        var data = new Metrics(); TimedView? view = null;
        if (!builtIn) gallery.InRibbonPresenterFactory = () =>
        {
            long start = Stopwatch.GetTimestamp(); view = new(data); data.Factory += Stopwatch.GetElapsedTime(start).TotalMilliseconds; return view;
        };
        Exception? failure = null;
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); await Task.Delay(200);
                log.WriteLine($"DPI={form.DeviceDpi}; animation={ribbon.AnimationLevel}; tab motion={ribbon.TabContentAnimation}; builtIn={builtIn}");
                for (int index = 0; index < 5; index++)
                {
                    data.Reset(); var clock = Stopwatch.StartNew(); ribbon.SelectedTab = tab; double select = clock.Elapsed.TotalMilliseconds;
                    ribbon.Refresh(); Application.DoEvents(); double painted = clock.Elapsed.TotalMilliseconds;
                    while (ribbon.IsAnimating && clock.ElapsedMilliseconds < 2500) await Task.Delay(10);
                    var controls = view is null ? [] : Descendants(view).ToArray();
                    log.WriteLine(FormattableString.Invariant($"visit={index}; select={select:F2}ms; painted={painted:F2}ms; settled={clock.Elapsed.TotalMilliseconds:F2}ms; factory={data.Factory:F2}ms; nativeInit={data.NativeInit:F2}ms; synchronize={data.Synchronize:F2}ms/{data.SyncCalls}; reveal={data.Reveal:F2}ms; controls={controls.Length}; handles={controls.Count(control => control.IsHandleCreated)}; visible={controls.Count(control => control.Visible)}"));
                    if (index == 0 && view is not null)
                    {
                        using var image = new Bitmap(form.Width, form.Height); form.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(output, "first-tab.png"));
                    }
                    ribbon.SelectedTab = home; while (ribbon.IsAnimating) await Task.Delay(10); await Task.Delay(80);
                }
            }
            catch (Exception error) { failure = error; log.WriteLine("FAIL: " + error); }
            finally { form.Close(); }
        };
        Application.Run(form); if (failure is not null) throw failure;
    }
    private sealed class Metrics
    {
        internal double Factory, Synchronize, NativeInit, Reveal; internal int SyncCalls;
        internal void Reset() { Factory = Synchronize = NativeInit = Reveal = 0; SyncCalls = 0; }
    }
    private sealed class TimedView : GalleryStylesStripControl, IRibbonGalleryPresenter
    {
        private readonly Metrics _metrics;
        private long _creatingHandle;
        internal TimedView(Metrics metrics) => _metrics = metrics;
        void IRibbonGalleryPresenter.Synchronize(RibbonGalleryPresenterContext context)
        {
            long start = Stopwatch.GetTimestamp();
            try { base.Synchronize(context); }
            finally { _metrics.SyncCalls++; _metrics.Synchronize += Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        }
        void IRibbonGalleryPresenter.Reveal(RibbonGalleryTile item)
        {
            long start = Stopwatch.GetTimestamp();
            try { base.Reveal(item); }
            finally { _metrics.Reveal += Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        }
        protected override void OnHandleCreated(EventArgs e) { _creatingHandle = Stopwatch.GetTimestamp(); base.OnHandleCreated(e); }
        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            if (_creatingHandle != 0) { _metrics.NativeInit += Stopwatch.GetElapsedTime(_creatingHandle).TotalMilliseconds; _creatingHandle = 0; }
        }
    }
    private static IEnumerable<Control> Descendants(Control parent) => new[] { parent }.Concat(parent.Controls.Cast<Control>().SelectMany(Descendants));
}
