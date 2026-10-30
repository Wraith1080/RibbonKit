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
    private readonly double _duration;
    // Cap this large native scene near 60 updates/second; progress uses elapsed
    // time, so pacing does not extend its authored transition duration.
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private readonly Point _initialPointer = Cursor.Position;
    private Action? _completed;
    private long _start;
    private bool _disposed, _subscribed;
    private RibbonBackstageBackground? _background;
    internal bool IsRunning => !_disposed;
    internal bool IsClosing { get; }
    internal Rectangle Bounds { get; }
    internal int VisibleWidth => Math.Clamp(_surface.Right - _origin.X, 0, _width);
    internal byte Opacity => _surface.MotionAlpha;

    private RibbonBackstageMotion(RibbonControl ribbon, RibbonBackstageSurface surface, Form form, bool closing, Action? completed)
    {
        _ribbon = ribbon; _surface = surface; _form = form; IsClosing = closing; _completed = completed;
        _origin = surface.Location; _width = surface.Width;
        _travel = Math.Max(1, (int)Math.Round(_width * .15));
        Bounds = surface.RectangleToScreen(surface.ClientRectangle);
        _duration = ribbon.AnimationLevel == RibbonAnimationLevel.Expressive ? 308 : 220;
        _timer.Tick += Tick;
    }
    internal static RibbonBackstageMotion? Prepare(RibbonControl ribbon, RibbonBackstageSurface surface, Form form, bool closing = false, Action? completed = null)
    {
        if (ribbon.AnimationLevel == RibbonAnimationLevel.None || !ribbon.CanAnimatePopupOpening || !ribbon.Motion.SystemEnabled ||
            form.WindowState == FormWindowState.Minimized || form.Opacity != 1 || form.TransparencyKey != Color.Empty ||
            surface.Width < 1 || surface.Height < 1 || surface.HasApplicationOpacity) return null;
        var motion = new RibbonBackstageMotion(ribbon, surface, form, closing, completed);
        try
        {
            surface.AcquireMotionOpacity(closing ? (byte)255 : (byte)1);
            surface.SetMotionAlpha(closing ? (byte)255 : (byte)1);
            // Move before showing, and seed native redirection at near-zero opacity.
            if (!closing) surface.MoveForMotion(new(motion._origin.X - motion._travel, motion._origin.Y));
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
        _subscribed = true; _start = Stopwatch.GetTimestamp(); _timer.Start();
        try { _surface.RepaintBackdrop(); _ribbon.InvalidateBackstageCaption(); }
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
        double t = Math.Clamp(progress, 0, 1), p = 1 - Math.Pow(1 - t, 3);
        double fade = t * t * (3 - 2 * t);
        _surface.SetMotionAlpha((byte)Math.Clamp((int)Math.Round(255 * (IsClosing ? 1 - fade : fade)), 1, 255));
        int offset = (int)Math.Round(_travel * (IsClosing ? p : 1 - p));
        _surface.MoveForMotion(new(_origin.X - offset, _origin.Y));
        _surface.RepaintBackdrop();
        _ribbon.InvalidateBackstageCaption();
    }
    private void Tick(object? sender, EventArgs e)
    {
        if (!_ribbon.Motion.SystemEnabled || _ribbon.AnimationLevel == RibbonAnimationLevel.None) { Dispose(); return; }
        double progress = Stopwatch.GetElapsedTime(_start).TotalMilliseconds / _duration;
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
            if (!_surface.IsDisposed)
            {
                _surface.MoveForMotion(_origin);
                _surface.RestoreMotionOpacity();
            }
            _background?.Dispose(); _background = null;
            _ribbon.InvalidateBackstageCaption(); _timer.Dispose();
        }
    }
}
