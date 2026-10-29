using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Closing releases the real popup/editors immediately. Only this inert drawing
// remains, so the outside click can focus and edit the application's control.
internal sealed class RibbonTabPopupDismissal : IDisposable
{
    private readonly RibbonControl _ribbon;
    private readonly Form _form;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 8 };
    private readonly double _duration;
    private readonly int _travel;
    private readonly long _start;
    private RibbonMotionWindow? _window;
    private Bitmap? _image;
    private bool _disposed;
    internal Rectangle Bounds { get; }
    internal bool IsRunning => _image is not null;
    private RibbonTabPopupDismissal(RibbonControl ribbon, RibbonPopupWindow popup, RibbonPopupSurface surface)
    {
        _ribbon = ribbon; _form = ribbon.FindForm()!;
        bool expressive = ribbon.AnimationLevel == RibbonAnimationLevel.Expressive;
        _duration = expressive ? 182 : 130; _travel = RibbonLayoutEngine.Scale(expressive ? 12 : 8, ribbon.DeviceDpi);
        int pad = RibbonLayoutEngine.Scale(8, ribbon.DeviceDpi); Bounds = Rectangle.Inflate(popup.Bounds, pad, pad);
        try
        {
            _image = new(Bounds.Width, Bounds.Height, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(_image))
            {
                var context = new RibbonPaintContext(g, new(pad, pad, popup.Width, popup.Height), ribbon.Theme, ribbon.Font, ribbon.DeviceDpi) { Density = ribbon.Density };
                if (ribbon.Theme.PopupShadowEnabled && SystemInformation.IsDropShadowEnabled) ribbon.Painter.DrawPopupShadow(context);
                ribbon.Painter.DrawPopupFrame(context);
                using var content = new Bitmap(surface.Width, surface.Height, PixelFormat.Format24bppRgb);
                RibbonMotionScene.Capture(ribbon, surface, content);
                var location = surface.PointToScreen(Point.Empty); g.TranslateTransform(location.X - Bounds.X, location.Y - Bounds.Y);
                if (surface.Region is { } clip) g.SetClip(clip, System.Drawing.Drawing2D.CombineMode.Intersect);
                g.DrawImageUnscaled(content, Point.Empty);
            }
            _window = new(_form); _window.RenderDismissal(_image, Bounds, 1, 0);
            _start = Stopwatch.GetTimestamp();
            _form.LocationChanged += Unsafe; _form.ClientSizeChanged += Unsafe; _form.Deactivate += Unsafe; _form.HandleDestroyed += Unsafe;
            ribbon.LocationChanged += Unsafe; ribbon.SizeChanged += Unsafe; ribbon.HandleDestroyed += Unsafe;
            _timer.Tick += Tick; _timer.Start();
        }
        catch { Dispose(); throw; }
    }
    internal static RibbonTabPopupDismissal? Start(RibbonControl ribbon, RibbonPopupWindow popup, RibbonPopupSurface surface)
    {
        if (ribbon.AnimationLevel == RibbonAnimationLevel.None || !ribbon.Motion.SystemEnabled || !ribbon.CanAnimatePopupOpening ||
            ribbon.FindForm() is not { Visible: true, Opacity: 1 } form || form.TransparencyKey != Color.Empty || !popup.Visible ||
            (long)popup.Width * popup.Height > 4_194_304) return null;
        try { return new(ribbon, popup, surface); }
        catch (Win32Exception e) { Trace.WriteLine("Temporary ribbon dismissal skipped: " + e.Message); return null; }
    }
    private void Tick(object? sender, EventArgs e)
    {
        if (!IsRunning || _ribbon.AnimationLevel == RibbonAnimationLevel.None || !_ribbon.Motion.SystemEnabled) { Dispose(); return; }
        double t = Math.Clamp(Stopwatch.GetElapsedTime(_start).TotalMilliseconds / _duration, 0, 1), p = 1 - Math.Pow(1 - t, 3);
        try { _window!.RenderDismissal(_image!, Bounds, 1 - p, -(int)Math.Round(_travel * p)); }
        catch { Dispose(); throw; }
        if (t >= 1) Dispose();
    }
    private void Unsafe(object? sender, EventArgs e) => Dispose();
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        _timer.Stop(); _timer.Dispose();
        _form.LocationChanged -= Unsafe; _form.ClientSizeChanged -= Unsafe; _form.Deactivate -= Unsafe; _form.HandleDestroyed -= Unsafe;
        _ribbon.LocationChanged -= Unsafe; _ribbon.SizeChanged -= Unsafe; _ribbon.HandleDestroyed -= Unsafe;
        _window?.Dispose(); _window = null; _image?.Dispose(); _image = null;
    }
}
