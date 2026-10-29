using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// A popup keeps its real frame/anchor/shadow. Only its inert inner drawing moves;
// native editor HWNDs remain alive and focused behind the drawing hold.
internal sealed class RibbonPopupMotion : IDisposable
{
    private readonly RibbonControl _ribbon;
    private readonly RibbonPopupWindow _popup;
    private readonly Control _surface;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 8 };
    private readonly ColorMatrix _fade = new();
    private readonly double _duration;
    private readonly int _travel;
    private RibbonDrawingHold? _hold;
    private RibbonMotionWindow? _window;
    private Bitmap? _image;
    private Region? _clip;
    private SolidBrush? _background;
    private long _start, _last;
    private bool _disposed, _subscribed;
    private readonly Point _initialPointer;
    internal Rectangle Bounds { get; private set; }
    internal bool IsRunning => !_disposed && _image is not null;
    internal readonly List<double> FrameIntervals = new(24);
    private RibbonPopupMotion(RibbonControl ribbon, RibbonPopupWindow popup, Control surface, RibbonDrawingHold hold)
    {
        _ribbon = ribbon; _popup = popup; _surface = surface; _hold = hold; _initialPointer = Cursor.Position;
        bool expressive = ribbon.AnimationLevel == RibbonAnimationLevel.Expressive;
        _duration = expressive ? 182 : 130; _travel = RibbonLayoutEngine.Scale(expressive ? 12 : 8, ribbon.DeviceDpi); _timer.Tick += Tick;
    }
    internal static RibbonPopupMotion? Prepare(RibbonControl ribbon, RibbonPopupWindow popup, Control surface)
    {
        if (ribbon.AnimationLevel == RibbonAnimationLevel.None || !ribbon.Motion.SystemEnabled || !ribbon.CanAnimatePopupOpening || ribbon.IsDisposed ||
            surface.Width < 1 || surface.Height < 1 || (long)surface.Width * surface.Height > 4_194_304) return null;
        if (ribbon.FindForm() is { } form && (form.Opacity != 1 || form.TransparencyKey != Color.Empty)) return null;
        if (RibbonDrawingHold.HasOpacity(surface.Handle)) return null;
        try { return new(ribbon, popup, surface, RibbonDrawingHold.Create(surface.Handle)); }
        catch (Win32Exception error) { Trace.WriteLine("Popup motion skipped: " + error.Message); return null; }
    }
    internal void Start()
    {
        if (_disposed) return;
        if (!_popup.Visible || !_surface.Visible || _surface.IsDisposed) { Dispose(); return; }
        try
        {
            Bounds = _surface.RectangleToScreen(_surface.ClientRectangle);
            if (!_popup.Bounds.Contains(Bounds)) { Dispose(); return; }
            _image = new(_surface.Width, _surface.Height, PixelFormat.Format24bppRgb);
            RibbonMotionScene.Capture(_ribbon, _surface, _image);
            if (_disposed) return;
            _clip = _surface.Region?.Clone(); _background = new(_ribbon.Theme.Surface);
            _window = new(_popup); _window.RenderPopup(this, 0);
            _start = _last = Stopwatch.GetTimestamp();
            _popup.LocationChanged += Unsafe; _popup.ClientSizeChanged += Unsafe; _popup.VisibleChanged += Visibility; _popup.Disposed += Unsafe; _popup.HandleDestroyed += Unsafe;
            _surface.ClientSizeChanged += Unsafe; _surface.FontChanged += Unsafe; _surface.Disposed += Unsafe; _surface.HandleDestroyed += Unsafe;
            _subscribed = true; _timer.Start();
        }
        catch (Win32Exception error) { Trace.WriteLine("Popup motion skipped: " + error.Message); Dispose(); }
        catch { Dispose(); throw; }
    }
    internal void Draw(Graphics graphics, ImageAttributes alpha, double progress)
    {
        graphics.CompositingMode = CompositingMode.SourceCopy; graphics.Clear(Color.Transparent); graphics.CompositingMode = CompositingMode.SourceOver;
        var saved = graphics.Save();
        try
        {
            if (_clip is not null) graphics.SetClip(_clip, CombineMode.Intersect);
            graphics.FillRectangle(_background!, new Rectangle(Point.Empty, Bounds.Size));
            double p = 1 - Math.Pow(1 - Math.Clamp(progress, 0, 1), 3); _fade.Matrix33 = (float)p; alpha.SetColorMatrix(_fade);
            graphics.DrawImage(_image!, new Rectangle(0, -(int)Math.Round(_travel * (1 - p)), _image!.Width, _image.Height), 0, 0, _image.Width, _image.Height, GraphicsUnit.Pixel, alpha);
        }
        finally { graphics.Restore(saved); }
    }
    private void Tick(object? sender, EventArgs e)
    {
        if (!IsRunning || !_ribbon.Motion.SystemEnabled || _ribbon.AnimationLevel == RibbonAnimationLevel.None) { Dispose(); return; }
        long now = Stopwatch.GetTimestamp(); FrameIntervals.Add(Stopwatch.GetElapsedTime(_last, now).TotalMilliseconds); _last = now;
        double progress = Stopwatch.GetElapsedTime(_start, now).TotalMilliseconds / _duration;
        try { _window!.RenderPopup(this, Math.Min(1, progress)); }
        catch { Dispose(); throw; }
        if (progress >= 1) Dispose();
    }
    internal bool PointerMoved => Cursor.Position != _initialPointer;
    internal Bitmap? CaptureFrame() => _window?.CaptureFrame();
    private void Unsafe(object? sender, EventArgs e) => Dispose();
    private void Visibility(object? sender, EventArgs e) { if (!_popup.Visible) Dispose(); }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true; _timer.Stop();
        if (_subscribed)
        {
            _popup.LocationChanged -= Unsafe; _popup.ClientSizeChanged -= Unsafe; _popup.VisibleChanged -= Visibility; _popup.Disposed -= Unsafe; _popup.HandleDestroyed -= Unsafe;
            _surface.ClientSizeChanged -= Unsafe; _surface.FontChanged -= Unsafe; _surface.Disposed -= Unsafe; _surface.HandleDestroyed -= Unsafe; _subscribed = false;
        }
        _hold?.Dispose(); _hold = null; _window?.Dispose(); _window = null;
        _image?.Dispose(); _image = null; _clip?.Dispose(); _clip = null; _background?.Dispose(); _background = null; _timer.Dispose();
    }
}
