using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed class RibbonGroupMotion(RibbonControl ribbon) : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 8 };
    private RibbonGroupScene? _from, _to;
    private RibbonMotionWindow? _window;
    private Control? _surface;
    private Form? _form;
    private readonly List<Control> _ancestors = [];
    private long _start;
    private bool _attached, _disposed;
    internal bool Changing { get; private set; }
    internal bool IsRunning => _window is not null;
    internal int StartedCount { get; private set; }
    internal string? LastSkipReason;
    internal bool? ActiveOwnerOverride;
    internal Rectangle Bounds => new(_to!.Origin.X + _to.Viewport.X, _to.Origin.Y + _to.Viewport.Y, _to.Viewport.Width, _to.Viewport.Height);
    internal nint BehindWindow => _surface is null ? 0 : ribbon.Popups.WindowAbove(_surface);
    internal void Change(Control surface, Func<RibbonLayoutSnapshot> layout, Action mutation)
    {
        if (Changing) { mutation(); return; }
        LastSkipReason = null;
        if (!Eligible(surface, layout()))
        { LastSkipReason ??= "Departure unavailable"; Settle(); Changing = true; try { mutation(); } finally { Changing = false; } return; }
        if (_surface is not null && !ReferenceEquals(_surface, surface)) Settle();
        Changing = true;
        bool enteredMutation = false, committed = false;
        try
        {
            var before = _from ?? new RibbonGroupScene(ribbon, surface, layout());
            _from = before;
            enteredMutation = true; mutation(); committed = true;
            ribbon.SyncEditors();
            if (surface.IsDisposed || !surface.Visible || !Eligible(surface, layout())) { LastSkipReason = "Arrival unavailable"; Settle(); return; }
            var after = new RibbonGroupScene(ribbon, surface, layout());
            if (!RibbonGroupScene.Changes(before.Groups, after.Groups) || before.Dpi != after.Dpi || before.Origin != after.Origin)
            { LastSkipReason = "Unchanged groups, DPI or position"; after.Dispose(); Settle(); return; }
            _to?.Dispose(); _to = after;
            if (_window is null)
            {
                _surface = surface; _form = ribbon.FindForm();
                _window = new(surface.TopLevelControl!); _start = Stopwatch.GetTimestamp(); StartedCount++;
                surface.Disposed += Unsafe; surface.HandleDestroyed += Unsafe; surface.VisibleChanged += Visibility;
                if (_form is not null) _form.Deactivate += Deactivate;
                for (Control? node = surface; node is not null; node = node.Parent)
                { _ancestors.Add(node); node.LocationChanged += Unsafe; node.ParentChanged += Unsafe; if (node is ScrollableControl scroll) scroll.Scroll += Scrolled; }
                if (!_attached) { _timer.Tick += Tick; _attached = true; } _timer.Start();
            }
            _window.RenderGroup(this, Progress);
        }
        catch (Win32Exception error) when (!enteredMutation || committed)
        { LastSkipReason = error.Message; Trace.WriteLine("Group motion skipped: " + error.Message); Settle(); if (!enteredMutation) mutation(); }
        catch { Settle(); throw; }
        finally { Changing = false; }
    }
    private bool Eligible(Control surface, RibbonLayoutSnapshot layout)
    {
        if (!(ActiveOwnerOverride ?? ribbon.Popups.HasActiveOwner)) { LastSkipReason = "Inactive owner"; return false; }
        if (_disposed || !ribbon.CanAnimateGroups || !ribbon.Motion.SystemEnabled || !surface.Visible || !surface.IsHandleCreated || surface.TopLevelControl is not { Visible: true } ||
            ribbon.FindForm() is not { Opacity: 1 } form || form.TransparencyKey != Color.Empty || layout.BodyViewport.Width < 1 || layout.BodyViewport.Height < 5 ||
            surface.Width < 1 || surface.Height < 1 || (long)surface.Width * surface.Height > 4_194_304) return false;
        var screen = surface.RectangleToScreen(Rectangle.Intersect(surface.ClientRectangle, layout.BodyViewport));
        for (var parent = surface.Parent; parent is not null; parent = parent.Parent)
            screen = Rectangle.Intersect(screen, parent.RectangleToScreen(parent.ClientRectangle));
        return screen.Width > 0 && screen.Height > 4;
    }
    private double Progress => Math.Min(1, Stopwatch.GetElapsedTime(_start).TotalMilliseconds / (ribbon.AnimationLevel == RibbonAnimationLevel.Expressive ? 210 : 150));
    internal void Draw(Graphics graphics, ImageAttributes alpha, double progress) => RibbonGroupScene.Draw(graphics, _from!, _to!, progress, RibbonLayoutEngine.Scale(ribbon.AnimationLevel == RibbonAnimationLevel.Expressive ? 20 : 12, ribbon.DeviceDpi), ribbon.Theme.Surface, ribbon.TabContentAnimation == RibbonTabContentAnimation.SlideDown, alpha);
    private void Tick(object? sender, EventArgs e)
    {
        if (Changing) return;
        if (!IsRunning || !ribbon.CanAnimateGroups || !ribbon.Motion.SystemEnabled) { Settle(); return; }
        double p = Progress;
        try { _window!.RenderGroup(this, p); } catch { Settle(); throw; }
        if (p >= 1) Settle();
    }
    internal Bitmap? CaptureFrame() => _window?.CaptureFrame();
    internal void ObserveInput(ref Message message)
    { if (message.Msg is 0x100 or 0x104 or 0x201 or 0x204 or 0x207 or 0x20A or 0x20E or 0x240 or 0x246) Settle(); }
    private void Unsafe(object? sender, EventArgs e) { if (!Changing) Settle(); }
    private void Visibility(object? sender, EventArgs e) { if (_surface?.Visible != true) Settle(); }
    private void Deactivate(object? sender, EventArgs e) { if (ribbon.Popups.HasOpen) ribbon.Popups.QueueActivationCheck(); else Settle(); }
    private void Scrolled(object? sender, ScrollEventArgs e) => Settle();
    internal void Settle()
    {
        _timer.Stop();
        if (_surface is not null) { _surface.Disposed -= Unsafe; _surface.HandleDestroyed -= Unsafe; _surface.VisibleChanged -= Visibility; }
        if (_form is not null) _form.Deactivate -= Deactivate;
        foreach (var node in _ancestors) { node.LocationChanged -= Unsafe; node.ParentChanged -= Unsafe; if (node is ScrollableControl scroll) scroll.Scroll -= Scrolled; }
        _ancestors.Clear(); _form = null; _surface = null;
        _window?.Dispose(); _window = null; _from?.Dispose(); _from = null; _to?.Dispose(); _to = null;
    }
    public void Dispose() { if (_disposed) return; _disposed = true; Settle(); _timer.Dispose(); }
}
