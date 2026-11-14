using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Measures only fixture-owned popup openings. No desktop input is injected.
internal static class GalleryOpeningProfile
{
    internal static void Run(string output, bool captureFrames = false, bool animatedOnly = false, int? entryCount = null)
    {
        Directory.CreateDirectory(output); using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        using var form = new DesignerForm { StartPosition = FormStartPosition.Manual, Location = new(180, 120) };
        var ribbon = form.Controls.OfType<RibbonControl>().Single(); var (tab, gallery) = form.GalleryPresenterExample; ribbon.SelectedTab = tab;
        if (entryCount is { } count)
        {
            if (count is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(entryCount));
            ribbon.BeginUpdate();
            try
            {
                gallery.Items.Clear();
                for (int i = 0; i < count; i++) gallery.Items.Add(new RibbonGalleryTile { Text = $"Style {i + 1}" });
                gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0];
            }
            finally { ribbon.EndUpdate(); }
        }
        log.WriteLine($"Entries={gallery.Items.Count} DPI={ribbon.DeviceDpi}");
        gallery.DropDownPresenterType = null; // This timing diagnostic substitutes instrumented factories.
        var clock = new Stopwatch(); using var renderer = new TimedRenderer(); ribbon.Renderer = renderer;
        var captures = new List<(string Name, Bitmap Image)>();
        Exception? failure = null;
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); await Task.Delay(500);
                foreach (var level in captureFrames || animatedOnly ? new[] { RibbonAnimationLevel.Expressive } : new[] { RibbonAnimationLevel.Expressive, RibbonAnimationLevel.None })
                foreach (bool custom in captureFrames ? new[] { true } : new[] { true, false })
                {
                    ribbon.AnimationLevel = level;
                    Metrics? data = null;
                    gallery.DropDownPresenterFactory = custom ? () =>
                    {
                        long start = Stopwatch.GetTimestamp(); var view = new TimedView(clock, data!);
                        data!.View = view;
                        data!.Factory += Stopwatch.GetElapsedTime(start).TotalMilliseconds; return view;
                    } : null;
                    for (int index = 0; index < (captureFrames ? 1 : 4); index++)
                    {
                        form.Activate(); await Task.Delay(80);
                        Require(GetForegroundWindow() == form.Handle, "Foreground unavailable before opening");
                        data = new(); renderer.Shadow = renderer.Frame = 0; clock.Restart(); gallery.ShowDropDown();
                        double synchronous = clock.Elapsed.TotalMilliseconds;
                        Require(gallery.IsDropDownOpen, "Popup closed before timing completed");
                        var timedPopup = data.View?.TopLevelControl;
                        object? timedMotion = timedPopup?.GetType().GetProperty("OpeningMotion", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(timedPopup);
                        CancellationTokenSource? captureStop = null; Task? captureTask = null;
                        if (captureFrames)
                        {
                            var area = (Rectangle)timedMotion!.GetType().GetProperty("Bounds", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(timedMotion)!;
                            using var first = timedMotion.GetType().GetMethod("CaptureFrame", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(timedMotion, null) as Bitmap;
                            if (first is not null)
                            {
                                var copy = new Bitmap(first.Width, first.Height);
                                using (var graphics = Graphics.FromImage(copy)) graphics.DrawImageUnscaled(first, Point.Empty);
                                captures.Add(("drawing-start.png", copy));
                            }
                            nint owner = form.Handle;
                            captureStop = new(); var stop = captureStop.Token;
                            // Screen capture/DWM waits must not starve the native motion timer.
                            captureTask = Task.Run(() =>
                            {
                                int frame = 0;
                                do
                                {
                                    nint foreground = GetForegroundWindow(); Require(foreground == owner || GetAncestor(foreground, 3) == owner, "Foreground lost during capture");
                                    _ = GdiFlush(); _ = DwmFlush();
                                    var image = new Bitmap(area.Width, area.Height);
                                    using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
                                    captures.Add(($"frame-{frame++:00}.png", image));
                                    log.WriteLine(FormattableString.Invariant($"SCREEN frame={frame - 1} elapsed={clock.Elapsed.TotalMilliseconds:F2}ms ink={Ink(image)}"));
                                } while (!stop.IsCancellationRequested);
                            });
                        }
                        while (ribbon.IsAnimating && clock.Elapsed.TotalMilliseconds - synchronous < 2000)
                        { await Task.Delay(15); }
                        Require(!ribbon.IsAnimating, "Popup opening motion did not settle");
                        double settled = clock.Elapsed.TotalMilliseconds; await Task.Delay(20);
                        if (captureStop is not null)
                        {
                            captureStop.Cancel(); await captureTask!; captureStop.Dispose();
                        }
                        foreach (var capture in captures) { capture.Image.Save(Path.Combine(output, capture.Name)); capture.Image.Dispose(); }
                        captures.Clear();
                        var ticks = timedMotion?.GetType().GetField("FrameIntervals", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(timedMotion) as List<double>;
                        int cards = data.View?.Controls.OfType<GalleryStyleTileControl>().Count() ?? 0;
                        int nativeCards = data.View?.Controls.OfType<GalleryStyleTileControl>().Count(card => card.IsHandleCreated) ?? 0;
                        log.WriteLine(FormattableString.Invariant($"{level} custom={custom} open={index} sync={synchronous:F2}ms settled={settled:F2}ms factory={data.Factory:F2}ms syncView={data.Sync:F2}ms measure={data.Measure:F2}ms handle={data.Handle:F2}ms load={data.Load:F2}ms print={data.Print:F2}ms paint={data.Paint:F2}ms nativePaintAt={data.FirstNativePaint:F2}ms shadow={renderer.Shadow:F2}ms frame={renderer.Frame:F2}ms boundsCalls={data.Bounds} paintCalls={data.Paints} cards={cards} nativeCards={nativeCards} ticks={ticks?.Count ?? 0} firstTick={ticks?.FirstOrDefault() ?? 0:F2}ms"));
                        gallery.CloseDropDown(); await Task.Delay(50);
                    }
                }
                log.WriteLine("COMPLETE: owned-window diagnostic timings; animation duration is separate from synchronous opening.");
            }
            catch (Exception error) { failure = error; log.WriteLine("FAIL: " + error); }
            finally { form.Close(); }
        };
        Application.Run(form);
        foreach (var capture in captures) capture.Image.Dispose();
        if (failure is not null) throw failure;
        static int Ink(Bitmap image)
        {
            var area = new Rectangle(0, 0, image.Width, image.Height); var data = image.LockBits(area, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                int count = 0; var row = new byte[Math.Abs(data.Stride)];
                for (int y = 16; y < image.Height - 36; y++)
                {
                    Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, row.Length);
                    for (int x = 16; x < image.Width - 16; x++)
                    { int i = x * 4, b = row[i], g = row[i + 1], r = row[i + 2]; if (row[i + 3] > 16 && (r + g + b < 600 || Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)) > 25)) count++; }
                }
                return count;
            }
            finally { image.UnlockBits(data); }
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class Metrics
    {
        internal double Factory, Sync, Measure, Handle, Load, Print, Paint, FirstNativePaint = -1;
        internal int Bounds, Paints;
        internal Control? View;
    }
    private sealed class TimedView(Stopwatch clock, Metrics data) : GalleryStylesControl, IRibbonGalleryPresenter
    {
        private bool _printing;
        void IRibbonGalleryPresenter.Synchronize(RibbonGalleryPresenterContext context)
        { long start = Stopwatch.GetTimestamp(); base.Synchronize(context); data.Sync += Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        Rectangle IRibbonGalleryPresenter.GetItemBounds(RibbonGalleryTile item) { data.Bounds++; return base.GetItemBounds(item); }
        public override Size GetPreferredSize(Size proposedSize)
        { long start = Stopwatch.GetTimestamp(); var result = base.GetPreferredSize(proposedSize); data.Measure += Stopwatch.GetElapsedTime(start).TotalMilliseconds; return result; }
        protected override void OnHandleCreated(EventArgs e)
        { long start = Stopwatch.GetTimestamp(); base.OnHandleCreated(e); data.Handle += Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        protected override void OnLoad(EventArgs e)
        { long start = Stopwatch.GetTimestamp(); base.OnLoad(e); data.Load += Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        protected override void OnPaint(PaintEventArgs e)
        {
            long start = Stopwatch.GetTimestamp(); base.OnPaint(e); double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (_printing) data.Print += elapsed; else { data.Paint += elapsed; if (data.FirstNativePaint < 0) data.FirstNativePaint = clock.Elapsed.TotalMilliseconds; }
            data.Paints++;
        }
        protected override void WndProc(ref Message m)
        {
            bool previous = _printing; _printing |= m.Msg is 0x317 or 0x318;
            try { base.WndProc(ref m); } finally { _printing = previous; }
        }
    }
    private sealed class TimedRenderer : RibbonOffice2024Renderer
    {
        internal double Shadow, Frame;
        public override void DrawPopupShadow(RibbonPaintContext context)
        { long start = Stopwatch.GetTimestamp(); base.DrawPopupShadow(context); Shadow += Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        public override void DrawPopupFrame(RibbonPaintContext context)
        { long start = Stopwatch.GetTimestamp(); base.DrawPopupFrame(context); Frame += Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    [DllImport("gdi32.dll")] private static extern bool GdiFlush();
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
}
