using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

internal sealed class RibbonGeometryMotion(RibbonControl ribbon) : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 8 };
    private RibbonMotionScene? _from, _to;
    private RibbonMotionWindow? _window;
    private RibbonDrawingHold? _hold;
    private Form? _form;
    private readonly List<Control> _ancestors = [];
    private long _start, _last;
    private double _duration;
    private bool _attached, _settling;
    private bool _disposed;
    private bool _headerArrival;
    private bool _messageChange;
    internal bool Changing { get; private set; }
    internal bool IsRunning => _from is not null && _to is not null;
    internal bool CanCoalesceHeaderSelection => _headerArrival && IsRunning && FrameIntervals.Count == 0;
    internal readonly List<double> FrameIntervals = new(64);
    internal int StartedCount { get; private set; }
    internal bool? SystemPolicyOverride;
    internal string? LastSkipReason;
    internal bool SystemEnabled => SystemPolicyOverride ?? (SystemParametersInfo(0x1042, 0, out bool enabled, 0) && enabled);
    internal void Change(bool toolbar, Action mutation, bool headers = false, bool messages = false)
    {
        if (Changing) { mutation(); return; }
        if (IsRunning)
        {
            if (CanCoalesceHeaderSelection || messages && _messageChange && FrameIntervals.Count == 0)
            {
                // Showing/merging and selecting a tab often share one command.
                // Before the first tick, retain its departure and update only
                // the arrival capture instead of cancelling the entrance.
                Changing = true;
                try
                {
                    mutation();
                    if (_hold is null || _from is null || ribbon.IsDisposed || !ribbon.IsHandleCreated || !SafeBounds()) { Release(); return; }
                    var next = new RibbonMotionScene(ribbon);
                    if (next.Dpi != _from.Dpi || next.Bounds.Width != _from.Bounds.Width) { next.Dispose(); Release(); return; }
                    if (messages && _messageChange && Math.Sign(next.MessageRows.Count - _to!.MessageRows.Count) != Math.Sign(_to.MessageRows.Count - _from.MessageRows.Count)) { next.Dispose(); Release(); return; }
                    _to!.Dispose(); _to = next; _window!.Render(ribbon, _from, _to, 0);
                    _duration = Math.Max(_duration, Duration(toolbar));
                    _start = _last = Stopwatch.GetTimestamp();
                }
                catch { Release(); throw; }
                finally { Changing = false; }
                return;
            }
            // Repeated requests settle to the newest real state while the old
            // drawing still covers it. A later independent request can animate.
            Changing = true; try { mutation(); } finally { Settle(); Changing = false; } return;
        }
        if (_disposed || !Eligible()) { Trace.WriteLine("Ribbon motion skipped: " + LastSkipReason); Changing = true; try { mutation(); } finally { Changing = false; } return; }
        Changing = true;
        try
        {
            _from = new(ribbon); _form = ribbon.FindForm()!;
            try { _window = new(_form, clipToOwner: _form is RibbonForm); _window.Render(ribbon, _from, _from, 0); _hold = RibbonDrawingHold.Create(ribbon.Handle); }
            catch (Win32Exception error) { LastSkipReason = error.Message; Trace.WriteLine(LastSkipReason); Release(); }
            var before = (ribbon.LayoutMode, ribbon.Density, ribbon.QuickAccessPosition, ribbon.SelectedTab, ribbon.IsMinimized);
            mutation();
            if (_hold is null || ribbon.IsDisposed || !ribbon.IsHandleCreated || !SafeBounds()) { Release(); return; }
            bool arrivals = _from!.HasHeaderArrivals(ribbon);
            if (headers ? !arrivals : messages ? !_from.HasMessageChanges(ribbon) : before == (ribbon.LayoutMode, ribbon.Density, ribbon.QuickAccessPosition, ribbon.SelectedTab, ribbon.IsMinimized)) { Release(); return; }
            _to = new(ribbon);
            if (_to.Dpi != _from!.Dpi || _to.Bounds.Width != _from.Bounds.Width) { Release(); return; }
            _headerArrival = headers && arrivals;
            _messageChange = messages;
            _window!.Render(ribbon, _from, _to, 0);
            _duration = Duration(toolbar);
            _start = _last = Stopwatch.GetTimestamp(); FrameIntervals.Clear(); StartedCount++;
            if (!_attached) { _timer.Tick += Tick; _attached = true; }
            _form.ResizeBegin += Unsafe; _form.Deactivate += Unsafe;
            ribbon.LocationChanged += Unsafe;
            for (var parent = ribbon.Parent; parent is not null; parent = parent.Parent)
            {
                _ancestors.Add(parent); parent.LocationChanged += Unsafe; parent.ClientSizeChanged += Unsafe;
                parent.ParentChanged += Unsafe; parent.VisibleChanged += Unsafe; parent.Disposed += Unsafe;
                if (parent is ScrollableControl scroll) scroll.Scroll += Scrolled;
            }
            _timer.Start();
        }
        catch { Release(); throw; }
        finally { Changing = false; }
    }
    private double Duration(bool toolbar) => (_from!.Minimized != ribbon.IsMinimized ? 180d : _from.HasMessageChanges(ribbon) ? 160d : toolbar ? 150d : 130d)
        * (ribbon.AnimationLevel == RibbonAnimationLevel.Expressive ? 1.4d : 1d);
    private bool Eligible()
    {
        if (ribbon.PopupOnly || ribbon.AnimationLevel == RibbonAnimationLevel.None || !ribbon.IsHandleCreated || !ribbon.Visible || !ribbon.Enabled || ribbon.IsInDesignMode || ribbon.IsBackstageOpen || !SystemEnabled) { LastSkipReason = "Policy, visibility or unavailable presentation"; return false; }
        if (!SafeBounds()) return false;
        var form = ribbon.FindForm()!;
        if (form.Opacity != 1 || form.TransparencyKey != Color.Empty) { LastSkipReason = "Application-owned form transparency"; return false; }
        if (RibbonDrawingHold.HasOpacity(ribbon.Handle)) { LastSkipReason = "Existing application-owned opacity"; return false; }
        LastSkipReason = null; return true;
    }
    private bool SafeBounds()
    {
        if (ribbon.Width < 1 || ribbon.Height < 1 || (long)ribbon.Width * ribbon.Height > 4_194_304 || ribbon.FindForm() is not { Visible: true } form || form.WindowState == FormWindowState.Minimized) { LastSkipReason = "Bounds or unavailable owner"; return false; }
        var screen = ribbon.RectangleToScreen(ribbon.ClientRectangle);
        for (var parent = ribbon.Parent; parent is not null; parent = parent.Parent)
            if (!parent.RectangleToScreen(parent.ClientRectangle).Contains(screen)) { LastSkipReason = "Clipped ancestor " + parent.GetType().Name; return false; }
        return true;
    }
    private void Tick(object? sender, EventArgs e)
    {
        if (!IsRunning || _form is null || !ribbon.Visible || !SystemEnabled || ribbon.AnimationLevel == RibbonAnimationLevel.None) { Settle(); return; }
        long now = Stopwatch.GetTimestamp(); FrameIntervals.Add(Stopwatch.GetElapsedTime(_last, now).TotalMilliseconds); _last = now;
        double progress = Stopwatch.GetElapsedTime(_start, now).TotalMilliseconds / _duration;
        try { _window!.Render(ribbon, _from!, _to!, Math.Min(1, progress)); }
        catch { Settle(); throw; }
        if (progress >= 1) Settle();
    }
    internal Bitmap? CaptureFrame() => _window?.CaptureFrame();
    internal void ObserveInput(ref Message message)
    {
        if (!IsRunning || message.Msg is not (0x100 or 0x104 or 0x201 or 0x204 or 0x207 or 0x20A or 0x20E or 0x240 or 0x246)) return;
        if (Control.FromChildHandle(message.HWnd)?.FindForm() == _form || ribbon.Popups.OwnsWindow(message.HWnd)) Settle();
    }
    private void Unsafe(object? sender, EventArgs e) => Settle();
    private void Scrolled(object? sender, ScrollEventArgs e) => Settle();
    internal void Settle()
    {
        if (_settling) return; _settling = true;
        try { Release(); }
        finally { _settling = false; }
    }
    private void Release()
    {
        _timer.Stop();
        _headerArrival = false;
        _messageChange = false;
        ribbon.LocationChanged -= Unsafe;
        if (_form is not null) { _form.ResizeBegin -= Unsafe; _form.Deactivate -= Unsafe; _form = null; }
        foreach (var parent in _ancestors)
        {
            parent.LocationChanged -= Unsafe; parent.ClientSizeChanged -= Unsafe; parent.ParentChanged -= Unsafe;
            parent.VisibleChanged -= Unsafe; parent.Disposed -= Unsafe;
            if (parent is ScrollableControl scroll) scroll.Scroll -= Scrolled;
        }
        _ancestors.Clear();
        _hold?.Dispose(); _hold = null;
        _window?.Dispose(); _window = null; _from?.Dispose(); _from = null; _to?.Dispose(); _to = null;
        if (!ribbon.IsDisposed && ribbon.IsHandleCreated) ribbon.Invalidate(true);
    }
    public void Dispose() { if (_disposed) return; _disposed = true; Settle(); _timer.Dispose(); }
    [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint parameter, out bool value, uint flags);
}
