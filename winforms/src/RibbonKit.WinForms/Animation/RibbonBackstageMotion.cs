using System.Diagnostics;
using System.ComponentModel;

namespace RibbonKit.WinForms;

// Translate the one live surface, including its native pages. Only its location
// changes alongside temporary native opacity; child layout, handles and ownership stay intact.
internal sealed class RibbonBackstageMotion : IDisposable
{
    private readonly RibbonControl _ribbon;
    private readonly RibbonBackstageSurface _surface;
    private readonly Form _form;
    private readonly Point _origin;
    private readonly int _width;
    private readonly int _travel;
    private readonly RibbonBackstageMotionPolicy _policy;
    // Cap this large native scene near 60 updates/second; progress uses elapsed
    // time, so pacing does not extend its authored transition duration.
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private readonly Point _initialPointer = Cursor.Position;
    private Action? _completed;
    private long _start;
    private bool _disposed, _subscribed;
    private RibbonBackstageBackground? _background;
    private RibbonMotionWindow? _applicationButton;
    internal bool OwnsApplicationButton => !_disposed && _applicationButton is not null;
    internal Bitmap? CaptureApplicationButton() => _applicationButton?.CaptureFrame();
    internal Rectangle ApplicationButtonPlaneBounds => _applicationButton?.Bounds ?? Rectangle.Empty;
    internal nint ApplicationButtonPlaneHandle => _applicationButton?.Handle ?? 0;
    internal int ApplicationButtonPublications => _applicationButton?.PublicationCount ?? 0;
    internal bool IsRunning => !_disposed;
    internal bool IsClosing { get; }
    internal Rectangle Bounds { get; }
    internal Point Origin => _origin;
    internal int VisibleWidth => Math.Clamp(_surface.Right - _origin.X, 0, _width);
    internal byte Opacity => _surface.MotionAlpha;

    private RibbonBackstageMotion(RibbonControl ribbon, RibbonBackstageSurface surface, Form form, bool closing, Action? completed, RibbonBackstageMotionPolicy policy)
    {
        _ribbon = ribbon; _surface = surface; _form = form; IsClosing = closing; _completed = completed;
        _origin = surface.Location; _width = surface.Width;
        _policy = policy;
        _travel = _policy.PaneTravel(_width);
        Bounds = surface.RectangleToScreen(surface.ClientRectangle);
        _timer.Tick += Tick;
    }
    internal static RibbonBackstageMotion? Prepare(RibbonControl ribbon, RibbonBackstageSurface surface, Form form, bool closing = false, Action? completed = null)
    {
        var policy = ribbon.BackstageMotionPolicy;
        if (ribbon.AnimationLevel == RibbonAnimationLevel.None || !ribbon.CanAnimatePopupOpening || !ribbon.Motion.SystemEnabled ||
            form.WindowState == FormWindowState.Minimized || form.Opacity != 1 || form.TransparencyKey != Color.Empty ||
            surface.Width < 1 || surface.Height < 1 || policy.PaneDuration == 0 || surface.HasApplicationOpacity) return null;
        var motion = new RibbonBackstageMotion(ribbon, surface, form, closing, completed, policy);
        try
        {
            byte alpha = policy.PaneOpacity(0, closing);
            // Let the first native drawing finish before exposing even an
            // opening whose authored fade starts at full opacity.
            surface.AcquireMotionOpacity(closing ? alpha : (byte)1);
            surface.SetMotionAlpha(alpha);
            // Material owns a separate page layer. Solid must seed its whole
            // native drawing at the unclipped origin before the initial move.
            if (!closing && ribbon.UsesMaterialBackstage) surface.MoveForMotion(new(motion._origin.X - motion._travel, motion._origin.Y));
            return motion;
        }
        catch (Win32Exception error) { Trace.WriteLine("Backstage motion skipped: " + error.Message); motion.Cancel(); return null; }
        catch { motion.Cancel(); throw; }
    }
    internal void Start()
    {
        if (_disposed) return;
        if (!_surface.Visible || !_ribbon.IsBackstageOpen) { Dispose(); return; }
        _surface.ClientSizeChanged += Unsafe; _surface.HandleDestroyed += Unsafe; _surface.VisibleChanged += Visibility; _surface.Disposed += Unsafe;
        _form.LocationChanged += Unsafe; _form.Deactivate += Unsafe; _form.HandleDestroyed += Unsafe;
        _subscribed = true;
        try
        {
            if (_ribbon.Theme.BackstageStyle == RibbonBackstageStyle.Office2007 && _ribbon.EffectiveApplicationButtonShape == RibbonApplicationButtonShape.Orb)
            {
                // Publish before transferring paint ownership. Native seed/flush
                // callbacks must never suppress File for a still-hidden plane.
                var drawing = new RibbonMotionWindow(_form, clipToOwner: true);
                try
                {
                    drawing.RenderBackstageApplicationButton(_ribbon, IsClosing);
                    if (_disposed) { drawing.Dispose(); return; }
                    _applicationButton = drawing;
                }
                catch { drawing.Dispose(); throw; }
            }
            if (!IsClosing && !_ribbon.UsesMaterialBackstage)
            {
                // Alpha remains one while every native child draws at its
                // final, unclipped position. Otherwise the cached pane starts
                // with unpainted left pixels that travel can briefly reveal.
                _surface.RepaintBackdrop(publishOpacity: false);
                if (_disposed) return;
                _surface.MoveForMotion(new(_origin.X - _travel, _origin.Y));
                PaintFrame(retainPage: true);
            }
            else PaintFrame();
            // Cold native drawing can outlast the transition itself. Start its
            // clock only after frame zero is ready, including under CPU load.
            if (_disposed) return;
            _start = Stopwatch.GetTimestamp(); _timer.Start();
        }
        catch (Win32Exception error) { Trace.WriteLine("Backstage presentation skipped: " + error.Message); Dispose(); }
        catch { Dispose(); throw; }
    }
    internal void HoldBackground(RibbonBackstageBackground? background)
    {
        _background = background;
        if (_disposed || background is null) return;
        try { background.ShowBehind(_surface); }
        catch (Win32Exception error) { Trace.WriteLine("Backstage background skipped: " + error.Message); Dispose(); }
        catch { Dispose(); throw; }
    }
    internal void SetProgress(double progress)
    {
        if (_disposed) return;
        double p = _policy.PaneProgress(progress);
        _surface.SetMotionAlpha(_policy.PaneOpacity(progress, IsClosing));
        int offset = (int)Math.Round(_travel * (IsClosing ? p : 1 - p));
        _surface.MoveForMotion(new(_origin.X - offset, _origin.Y));
        PaintFrame(retainPage: true);
    }
    private void PaintFrame(bool retainPage = false)
    {
        _surface.RepaintBackdrop(retainPage, publishOpacity: false);
        _ribbon.InvalidateBackstageCaption();
        _surface.PublishMotionOpacity();
        _applicationButton?.RenderBackstageApplicationButton(_ribbon, IsClosing);
    }
    private void Tick(object? sender, EventArgs e)
    {
        if (!_ribbon.Motion.SystemEnabled || _ribbon.AnimationLevel == RibbonAnimationLevel.None) { Dispose(); return; }
        double progress = Stopwatch.GetElapsedTime(_start).TotalMilliseconds / _policy.PaneDuration;
        try { SetProgress(progress); }
        catch { Dispose(); throw; }
        if (progress >= 1) Dispose();
    }
    internal bool ObserveInput(ref Message message)
    {
        var control = Control.FromChildHandle(message.HWnd);
        if (!IsRunning || control?.FindForm() != _form) return false;
        if (message.Msg == 0x200)
        {
            if (Cursor.Position != _initialPointer && Bounds.Contains(Cursor.Position)) Dispose();
        }
        else if (message.Msg is 0x100 or 0x104 or 0x201 or 0x204 or 0x207 or 0x20A or 0x20E or 0x240 or 0x246)
        {
            bool consume = IsClosing && (message.Msg is 0x100 or 0x104 || Bounds.Contains(Cursor.Position)) &&
                !((message.Msg is 0x100 or 0x104) && (Keys)(int)message.WParam == Keys.F4 && Control.ModifierKeys.HasFlag(Keys.Alt));
            Dispose(); return consume;
        }
        return false;
    }
    internal void Cancel() { _completed = null; Dispose(); }
    private void Unsafe(object? sender, EventArgs e) => Dispose();
    private void Visibility(object? sender, EventArgs e) { if (!_surface.Visible) Dispose(); }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true; _timer.Stop();
        if (_subscribed)
        {
            _surface.ClientSizeChanged -= Unsafe; _surface.HandleDestroyed -= Unsafe; _surface.VisibleChanged -= Visibility; _surface.Disposed -= Unsafe;
            _form.LocationChanged -= Unsafe; _form.Deactivate -= Unsafe; _form.HandleDestroyed -= Unsafe; _subscribed = false;
        }
        var completed = _completed; _completed = null;
        try { completed?.Invoke(); } // Hide before restoring the closing location.
        finally
        {
            try
            {
                if (!_surface.IsDisposed)
                {
                    _surface.MoveForMotion(_origin, clipFrame: false);
                    if (_applicationButton is not null) _surface.Invalidate(_surface.HeaderBounds, true);
                    _surface.RestoreMotionOpacity();
                }
                if (_applicationButton is not null && !_ribbon.IsDisposed)
                {
                    _ribbon.Invalidate(_ribbon.ApplicationButtonBounds); _ribbon.Update();
                }
            }
            finally
            {
                try { _background?.Dispose(); }
                finally
                {
                    _background = null;
                    try { _applicationButton?.Dispose(); }
                    finally
                    {
                        _applicationButton = null;
                        try { _ribbon.InvalidateBackstageCaption(); }
                        finally { _timer.Dispose(); }
                    }
                }
            }
        }
    }
}
