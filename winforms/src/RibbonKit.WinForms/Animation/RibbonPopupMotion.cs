using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Native editor HWNDs remain alive and focused behind an inert drawing. Galleries
// fade their whole card/shadow while the items slide inside its fixed viewport.
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
    private RibbonDrawingHold? _popupHold;
    private RibbonMotionWindow? _window;
    private Bitmap? _image;
    private Bitmap? _frame, _composition;
    private Graphics? _compositionGraphics;
    private Point _contentOrigin;
    private Region? _clip;
    private SolidBrush? _background;
    private long _start, _last;
    private bool _disposed, _subscribed;
    private readonly Point _initialPointer;
    internal Rectangle Bounds { get; private set; }
    internal bool IsRunning => !_disposed && _image is not null;
    internal bool FadesGallery { get; }
    internal readonly List<double> FrameIntervals = new(24);
    private RibbonPopupMotion(RibbonControl ribbon, RibbonPopupWindow popup, Control surface, RibbonDrawingHold hold, long requestedAt)
    {
        _ribbon = ribbon; _popup = popup; _surface = surface; _hold = hold; _initialPointer = Cursor.Position;
        _start = requestedAt;
        FadesGallery = surface is RibbonPopupSurface { GallerySource: not null };
        bool expressive = ribbon.AnimationLevel == RibbonAnimationLevel.Expressive;
        _duration = expressive ? 182 : 130; _travel = RibbonLayoutEngine.Scale(expressive ? 12 : 8, ribbon.DeviceDpi); _timer.Tick += Tick;
    }
    internal static RibbonPopupMotion? Prepare(RibbonControl ribbon, RibbonPopupWindow popup, Control surface)
    {
        if (ribbon.AnimationLevel == RibbonAnimationLevel.None || !ribbon.Motion.SystemEnabled || !ribbon.CanAnimatePopupOpening || ribbon.IsDisposed ||
            surface.Width < 1 || surface.Height < 1 || (long)surface.Width * surface.Height > 4_194_304) return null;
        if (ribbon.FindForm() is { } form && (form.Opacity != 1 || form.TransparencyKey != Color.Empty)) return null;
        if (RibbonDrawingHold.HasOpacity(surface.Handle)) return null;
        long requestedAt = Stopwatch.GetTimestamp();
        RibbonPopupMotion? motion = null;
        try
        {
            if (RibbonDrawingHold.HasOpacity(popup.Handle)) return null;
            motion = new(ribbon, popup, surface, RibbonDrawingHold.Create(surface.Handle), requestedAt);
            if (motion.FadesGallery) motion._popupHold = RibbonDrawingHold.Create(popup.Handle, compositeChildren: true);
            return motion;
        }
        catch (Win32Exception error) { motion?.Dispose(); Trace.WriteLine("Popup motion skipped: " + error.Message); return null; }
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
            RibbonMotionScene.Capture(_ribbon, _surface, _image, includeNonClient: FadesGallery);
            if (_disposed) return;
            _clip = _surface.Region?.Clone(); _background = new(_ribbon.Theme.PopupColor);
            if (FadesGallery) PrepareGallery();
            _window = new(_popup);
            _window.PreparePopup(Bounds);
            // Gallery preparation happens behind a transparent native card.
            // Its full fade and item slide receive the same fresh visible clock.
            if (FadesGallery) _start = Stopwatch.GetTimestamp();
            double initialProgress = FadesGallery ? 0 : Math.Clamp(Stopwatch.GetElapsedTime(_start).TotalMilliseconds / _duration, 0, 1);
            _window.RenderPopup(this, initialProgress);
            _last = Stopwatch.GetTimestamp();
            _popup.LocationChanged += Unsafe; _popup.ClientSizeChanged += Unsafe; _popup.VisibleChanged += Visibility; _popup.Disposed += Unsafe; _popup.HandleDestroyed += Unsafe;
            _surface.ClientSizeChanged += Unsafe; _surface.FontChanged += Unsafe; _surface.Disposed += Unsafe; _surface.HandleDestroyed += Unsafe;
            _subscribed = true; _timer.Start();
        }
        catch (Win32Exception error) { Trace.WriteLine("Popup motion skipped: " + error.Message); Dispose(); }
        catch { Dispose(); throw; }
    }
    private void PrepareGallery()
    {
        var content = Bounds;
        int padding = RibbonFrameMetrics.PopupPadding(_ribbon.Painter, _ribbon.Theme, _ribbon.DeviceDpi);
        Bounds = Rectangle.Inflate(_popup.Bounds, padding, padding);
        _contentOrigin = new(content.X - Bounds.X, content.Y - Bounds.Y);
        _clip ??= new Region(new Rectangle(Point.Empty, _surface.Size));
        _clip.Translate(_contentOrigin.X, _contentOrigin.Y);
        _frame = new(Bounds.Width, Bounds.Height, PixelFormat.Format32bppPArgb);
        using (var graphics = Graphics.FromImage(_frame))
        {
            var context = new RibbonPaintContext(graphics, new(padding, padding, _popup.Width, _popup.Height), _ribbon.Theme, _ribbon.Font, _ribbon.DeviceDpi) { Density = _ribbon.Density };
            if (_ribbon.Theme.PopupShadowEnabled && SystemInformation.IsDropShadowEnabled) _ribbon.Painter.DrawPopupShadow(context);
            _ribbon.Painter.DrawPopupFrame(context);
        }
        _composition = new(Bounds.Width, Bounds.Height, PixelFormat.Format32bppPArgb);
        _compositionGraphics = Graphics.FromImage(_composition);
    }
    internal void Draw(Graphics graphics, ImageAttributes alpha, double progress)
    {
        graphics.CompositingMode = CompositingMode.SourceCopy; graphics.Clear(Color.Transparent); graphics.CompositingMode = CompositingMode.SourceOver;
        if (FadesGallery)
        {
            var canvas = _compositionGraphics!;
            canvas.Clear(Color.Transparent); canvas.DrawImageUnscaled(_frame!, Point.Empty);
            var clip = canvas.Save();
            double p = 1 - Math.Pow(1 - Math.Clamp(progress, 0, 1), 3);
            try
            {
                canvas.SetClip(_clip!, CombineMode.Intersect);
                canvas.DrawImageUnscaled(_image!, _contentOrigin.X, _contentOrigin.Y - (int)Math.Round(_travel * (1 - p)));
            }
            finally { canvas.Restore(clip); }
            canvas.Flush();
            _fade.Matrix33 = (float)Math.Clamp(progress, 0, 1); alpha.SetColorMatrix(_fade);
            graphics.DrawImage(_composition!, new Rectangle(Point.Empty, Bounds.Size), 0, 0, Bounds.Width, Bounds.Height, GraphicsUnit.Pixel, alpha);
            return;
        }
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
        _hold?.Dispose(); _hold = null; _popupHold?.Dispose(); _popupHold = null;
        if (FadesGallery) _popup.CompleteGalleryAppearance(_window?.Handle ?? 0);
        _window?.Dispose(); _window = null;
        _compositionGraphics?.Dispose(); _compositionGraphics = null; _composition?.Dispose(); _composition = null; _frame?.Dispose(); _frame = null;
        _image?.Dispose(); _image = null; _clip?.Dispose(); _clip = null; _background?.Dispose(); _background = null; _timer.Dispose();
    }
}
