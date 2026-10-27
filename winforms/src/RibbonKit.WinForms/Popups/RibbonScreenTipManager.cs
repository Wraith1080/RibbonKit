using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed class RibbonScreenTipManager(RibbonControl ribbon) : IDisposable
{
    internal const int HoverDelay = 600;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 80 };
    private Form? _form;
    private Control? _surface;
    private RibbonItem? _item;
    private Rectangle _anchor;
    private Point? _lastPointer;
    private long _due;
    private bool _attached;
    internal RibbonScreenTipWindow? Window { get; private set; }
    internal bool Pending => _item is not null && Window is null;

    internal void Attach()
    {
        ResetOwner();
        if (ribbon.IsInDesignMode) return;
        _form = ribbon.FindForm();
        if (_form is not null)
        {
            _form.Deactivate += OnOwnerChanged;
            _form.LocationChanged += OnOwnerChanged;
            _form.SizeChanged += OnOwnerChanged;
        }
        if (!_attached) { _timer.Tick += OnTick; _attached = true; }
    }
    internal void Observe(ref Message message)
    {
        if (message.Msg == 0x200) PointerMoved(Control.MousePosition);
        else if (message.Msg is >= 0x100 and <= 0x109 or >= 0x201 and <= 0x20E or 0xA1 or 0xA4 or 0xA7)
            Cancel();
    }
    internal void PointerMoved(Point point)
    {
        // Ignore stationary WM_MOUSEMOVE synthesized by painting/menu changes.
        // A key/press cancellation therefore stays cancelled until real motion.
        if (_lastPointer == point) return;
        _lastPointer = point;
        var hit = FindTarget(point);
        if (hit is { } same && ReferenceEquals(same.Item, _item) && ReferenceEquals(same.Surface, _surface) && same.Anchor == _anchor) return;
        Cancel();
        if (hit is not { } target || Control.MouseButtons != MouseButtons.None || ribbon.KeyTipsActive) return;
        _surface = target.Surface; _item = target.Item; _anchor = target.Anchor;
        _due = Environment.TickCount64 + HoverDelay;
        _timer.Start();
    }
    internal (Control Surface, RibbonItem Item, Rectangle Anchor)? FindTarget(Point screen)
    {
        // Choice strings have no per-row metadata in M2E. Do not let a tip from
        // an underlying command show through the list or another popup surface.
        if (ribbon.Editors.HasOpenList) return null;
        foreach (var surface in ribbon.Popups.Surfaces.Reverse().Cast<Control>().Append(ribbon))
        {
            if (surface.IsDisposed || !surface.Visible || !surface.IsHandleCreated) continue;
            var point = surface.PointToClient(screen);
            if (!surface.ClientRectangle.Contains(point)) continue;
            var targets = surface is RibbonPopupSurface popup ? popup.Targets : ribbon.Snapshot.Targets;
            var target = targets.FirstOrDefault(t => t.Element is RibbonItem && t.VisibleBounds.Contains(point));
            if (target?.Element is not RibbonItem item || item.IsDisposed || !item.Visible || !ReferenceEquals(item.Ribbon, ribbon)) return null;
            if (string.IsNullOrEmpty(RibbonScreenTipLayout.TitleFor(item)) && string.IsNullOrEmpty(item.ScreenTipDescription)) return null;
            var anchor = targets.Where(t => ReferenceEquals(t.Element, item)).Select(t => t.VisibleBounds).Where(b => !b.IsEmpty)
                .Aggregate(Rectangle.Empty, (a, b) => a.IsEmpty ? b : Rectangle.Union(a, b));
            return (surface, item, surface.RectangleToScreen(anchor));
        }
        return null;
    }
    private void OnTick(object? sender, EventArgs e)
    {
        if (_item is null || _surface is null || !ribbon.Visible || ribbon.IsDisposed || ribbon.KeyTipsActive || Control.MouseButtons != MouseButtons.None)
        { Cancel(); return; }
        nint foreground = GetForegroundWindow();
        nint underPointer = WindowFromPoint(Control.MousePosition);
        if ((_form is not { IsHandleCreated: true } || foreground != _form.Handle) && !ribbon.Popups.OwnsWindow(foreground))
        { Cancel(); return; }
        if (underPointer != _surface.Handle && !IsChild(_surface.Handle, underPointer))
        { Cancel(); return; }
        var hit = FindTarget(Control.MousePosition);
        if (hit is not { } target || !ReferenceEquals(target.Item, _item) || !ReferenceEquals(target.Surface, _surface) || target.Anchor != _anchor)
        { Cancel(); return; }
        if (Window is not null || Environment.TickCount64 < _due) return;
        Window = new(ribbon, _item, _anchor, Control.MousePosition);
        Window.Show(_form);
        Window.RaiseAbovePopups();
    }
    private void OnOwnerChanged(object? sender, EventArgs e) => Cancel();
    internal void Cancel()
    {
        _timer.Stop();
        _item = null; _surface = null;
        Window?.Dispose(); Window = null;
    }
    internal void ResetOwner()
    {
        Cancel();
        if (_form is not null)
        {
            _form.Deactivate -= OnOwnerChanged;
            _form.LocationChanged -= OnOwnerChanged;
            _form.SizeChanged -= OnOwnerChanged;
            _form = null;
        }
    }
    public void Dispose() { ResetOwner(); _timer.Dispose(); }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint WindowFromPoint(Point point);
    [DllImport("user32.dll")] private static extern bool IsChild(nint parent, nint child);
}
