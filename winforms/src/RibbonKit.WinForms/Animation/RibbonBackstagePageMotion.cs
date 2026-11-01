using System.ComponentModel;
using System.Diagnostics;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Animate the persistent native page slot. Its Content never changes parent,
// size, ownership or handle, and is never captured into a bitmap.
internal sealed class RibbonBackstagePageMotion : IDisposable
{
    private readonly RibbonControl _ribbon;
    private readonly Control _viewport;
    private readonly Form _form;
    private readonly Point _origin;
    private readonly Size _size;
    private readonly int _travel;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private long _start;
    private bool _disposed, _subscribed;
    internal RibbonBackstageSurface.PageSlot Target { get; }
    internal RibbonTabContentAnimation Style { get; }
    internal bool IsRunning => !_disposed;
    internal byte Opacity { get; private set; } = 255;
    internal double Duration { get; }

    private RibbonBackstagePageMotion(RibbonControl ribbon, RibbonBackstageSurface.PageSlot target, Control viewport, Form form)
    {
        _ribbon = ribbon; Target = target; _viewport = viewport; _form = form;
        _origin = target.Location; _size = target.Size; Style = ribbon.TabContentAnimation;
        _travel = RibbonLayoutEngine.Scale(ribbon.AnimationLevel == RibbonAnimationLevel.Expressive ? 12 : 8, ribbon.DeviceDpi);
        Duration = ribbon.AnimationLevel == RibbonAnimationLevel.Expressive ? 182 : 130;
        _timer.Tick += Tick;
    }
    internal static RibbonBackstagePageMotion? Prepare(RibbonControl ribbon, RibbonBackstageSurface.PageSlot target, Control viewport)
    {
        if (!ribbon.IsBackstageOpen || ribbon.AnimationLevel == RibbonAnimationLevel.None || !ribbon.CanAnimatePopupOpening || !ribbon.Motion.SystemEnabled ||
            !viewport.Visible || target.IsDisposed || target.Width < 1 || target.Height < 1 ||
            ribbon.FindForm() is not { Visible: true, IsDisposed: false } form || form.WindowState == FormWindowState.Minimized ||
            form.Opacity != 1 || form.TransparencyKey != Color.Empty || target.HasApplicationOpacity) return null;
        var motion = new RibbonBackstagePageMotion(ribbon, target, viewport, form);
        try
        {
            // A stable native drawing is also useful for opaque sliding: move
            // its redirected pixels instead of repainting rich text each step.
            target.AcquirePageOpacity(motion.Style == RibbonTabContentAnimation.Fade ? (byte)1 : (byte)255);
            motion.SetProgress(0);
            return motion;
        }
        catch (Win32Exception error) { Trace.WriteLine("Backstage page motion skipped: " + error.Message); motion.Dispose(); return null; }
        catch { motion.Dispose(); throw; }
    }
    internal void Start()
    {
        if (_disposed) return;
        if (!Target.Visible || !_ribbon.IsBackstageOpen) { Dispose(); return; }
        Target.SizeChanged += Unsafe; Target.HandleDestroyed += Unsafe; Target.ParentChanged += Unsafe;
        Target.VisibleChanged += Visibility; Target.Disposed += Unsafe;
        _viewport.SizeChanged += Unsafe;
        _form.Deactivate += Unsafe; _form.LocationChanged += Unsafe; _form.HandleDestroyed += Unsafe;
        _subscribed = true; _start = Stopwatch.GetTimestamp();
        try { RibbonDrawingHold.UpdatePending(Target.Handle); _timer.Start(); }
        catch { Dispose(); throw; }
    }
    internal void SetProgress(double progress)
    {
        if (_disposed) return;
        double t = Math.Clamp(progress, 0, 1), p = 1 - Math.Pow(1 - t, 3);
        if (Style == RibbonTabContentAnimation.Fade)
        {
            Opacity = (byte)Math.Clamp((int)Math.Round(255 * p), 1, 255);
            Target.SetPageOpacity(Opacity);
        }
        else
        {
            Target.MoveForMotion(new(_origin.X, _origin.Y - (int)Math.Round(_travel * (1 - p))));
            RibbonDrawingHold.UpdatePending(Target.Handle);
        }
    }
    private void Tick(object? sender, EventArgs e)
    {
        if (!_ribbon.IsBackstageOpen || !_ribbon.Motion.SystemEnabled || _ribbon.AnimationLevel == RibbonAnimationLevel.None ||
            _ribbon.TabContentAnimation != Style || !ReferenceEquals(Target.Parent, _viewport) || Target.Size != _size)
        { Dispose(); return; }
        double progress = Stopwatch.GetElapsedTime(_start).TotalMilliseconds / Duration;
        try { SetProgress(progress); }
        catch { Dispose(); throw; }
        if (progress >= 1) Dispose();
    }
    internal void ObserveInput(ref Message message)
    {
        if (IsRunning && message.Msg is 0x100 or 0x104 or 0x201 or 0x204 or 0x207 or 0x20A or 0x20E or 0x240 or 0x246
            && Control.FromChildHandle(message.HWnd)?.FindForm() == _form) Dispose();
    }
    private void Unsafe(object? sender, EventArgs e) => Dispose();
    private void Visibility(object? sender, EventArgs e) { if (!Target.Visible) Dispose(); }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true; _timer.Stop();
        if (_subscribed)
        {
            Target.SizeChanged -= Unsafe; Target.HandleDestroyed -= Unsafe; Target.ParentChanged -= Unsafe;
            Target.VisibleChanged -= Visibility; Target.Disposed -= Unsafe;
            _viewport.SizeChanged -= Unsafe;
            _form.Deactivate -= Unsafe; _form.LocationChanged -= Unsafe; _form.HandleDestroyed -= Unsafe;
        }
        try { if (!Target.IsDisposed) { Target.MoveForMotion(_origin); Target.RestorePageOpacity(); } }
        finally { Opacity = 255; _timer.Dispose(); }
    }
}
