using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed partial class RibbonPopupSurface
{
    private int _overflowExtent, _overflowOffset;
    private System.Windows.Forms.Timer? _overflowTimer;
    private TargetKind? _overflowRepeatKind;
    private bool _overflowRepeatPressed;
    private bool HasOverflowBands => OwnerElement is RibbonQuickAccessOverflow && _overflowExtent > ClientSize.Height;
    private int OverflowBandHeight => Math.Min(RibbonLayoutEngine.Scale(_ribbon.Density == RibbonDensity.Touch ? 28 : 16, _ribbon.DeviceDpi), Math.Max(0, (ClientSize.Height - 1) / 2));
    internal Rectangle OverflowViewport => HasOverflowBands
        ? new(0, OverflowBandHeight, ClientSize.Width, Math.Max(1, ClientSize.Height - 2 * OverflowBandHeight)) : ClientRectangle;
    internal Rectangle OverflowUpBounds => HasOverflowBands ? new(0, 0, ClientSize.Width, OverflowBandHeight) : Rectangle.Empty;
    internal Rectangle OverflowDownBounds => HasOverflowBands ? new(0, ClientSize.Height - OverflowBandHeight, ClientSize.Width, OverflowBandHeight) : Rectangle.Empty;
    private int OverflowLimit => Math.Max(0, _overflowExtent - OverflowViewport.Height);
    private static bool IsOverflowScroller(TargetKind? kind) => kind is TargetKind.OverflowUp or TargetKind.OverflowDown;
    private bool CanScrollOverflow(bool upward) => HasOverflowBands && (upward ? _overflowOffset > 0 : _overflowOffset < OverflowLimit);
    private bool OverflowChildOpen => _ribbon.Editors.HasOpenList || _ribbon.Popups.Surfaces.Contains(this) && !ReferenceEquals(_ribbon.Popups.Top, this);

    private void InitializeOverflowScrolling()
    {
        _overflowTimer = new() { Interval = 350 };
        _overflowTimer.Tick += OnOverflowRepeat;
    }
    private void DisposeOverflowScrolling()
    {
        StopOverflowRepeat();
        _overflowTimer?.Dispose();
        _overflowTimer = null;
    }
    private void PaintOverflowScrollers(RibbonPaintContext context)
    {
        if (!HasOverflowBands) return;
        Paint(OverflowUpBounds, TargetKind.OverflowUp, true);
        Paint(OverflowDownBounds, TargetKind.OverflowDown, false);
        void Paint(Rectangle bounds, TargetKind kind, bool upward)
        {
            bool enabled = CanScrollOverflow(upward);
            _ribbon.Painter.DrawOverflowScrollButton(context with
                { Bounds = bounds, Hot = enabled && _hotKind == kind, Pressed = enabled && _pressedKind == kind && Capture }, upward, enabled);
        }
    }
    private RibbonTarget? HitOverflowScroller(Point point)
    {
        if (OverflowUpBounds.Contains(point) && CanScrollOverflow(true)) return new(TargetKind.OverflowUp, null, OverflowUpBounds, ClientRectangle);
        if (OverflowDownBounds.Contains(point) && CanScrollOverflow(false)) return new(TargetKind.OverflowDown, null, OverflowDownBounds, ClientRectangle);
        return null;
    }
    internal bool ScrollOverflow(bool downward)
    {
        if (!Visible || OverflowChildOpen || !CanScrollOverflow(!downward)) return false;
        var starts = _targets.Select(t => t.Bounds.Top - _contentPadding);
        int next = downward ? starts.Where(y => y > _overflowOffset).DefaultIfEmpty(OverflowLimit).Min()
            : starts.Where(y => y < _overflowOffset).DefaultIfEmpty(0).Max();
        SetOverflowOffset(next);
        return true;
    }
    private void SetOverflowOffset(int offset)
    {
        offset = Math.Clamp(offset, 0, OverflowLimit);
        if (offset == _overflowOffset) return;
        _ribbon.ScreenTips.Cancel();
        if (_ribbon.Popups.Surfaces.Contains(this)) _ribbon.Popups.PrepareChoice(this);
        _overflowOffset = offset;
        _hot = null;
        if (!IsOverflowScroller(_pressedKind)) { _pressed = null; _pressedKind = null; Capture = false; }
        _ribbon.SyncEditors(); Invalidate(); _ribbon.AccessibilityChanged();
    }
    private void UpdateOverflowHover(TargetKind? kind)
    {
        if (Capture && IsOverflowScroller(_pressedKind))
        {
            if (kind != _pressedKind) StopOverflowRepeat();
            else if (_overflowRepeatKind != kind) StartOverflowRepeat(kind!.Value, true);
            return;
        }
        if (!IsOverflowScroller(kind)) StopOverflowRepeat();
        else if (_overflowRepeatKind != kind) StartOverflowRepeat(kind!.Value, false);
    }
    private void StartOverflowRepeat(TargetKind kind, bool pressed)
    {
        StopOverflowRepeat();
        if (_overflowTimer is null || !CanScrollOverflow(kind == TargetKind.OverflowUp) || OverflowChildOpen) return;
        _overflowRepeatKind = kind; _overflowRepeatPressed = pressed;
        _overflowTimer.Interval = 350; _overflowTimer.Start();
    }
    private void OnOverflowRepeat(object? sender, EventArgs e)
    {
        if (_overflowRepeatKind is not { } kind || !Visible || OverflowChildOpen
            || HitOverflowScroller(PointToClient(Cursor.Position))?.Kind != kind
            || _overflowRepeatPressed && (!Capture || _pressedKind != kind)
            || !ScrollOverflow(kind == TargetKind.OverflowDown))
        { StopOverflowRepeat(); return; }
        _overflowTimer!.Interval = 90;
    }
    private void StopOverflowRepeat()
    {
        _overflowTimer?.Stop();
        _overflowRepeatKind = null;
    }
    protected override void OnVisibleChanged(EventArgs e)
    {
        if (!Visible) StopOverflowRepeat();
        base.OnVisibleChanged(e);
    }
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (OwnerElement is not RibbonQuickAccessOverflow) return;
        StopOverflowRepeat();
        _overflowOffset = Math.Clamp(_overflowOffset, 0, OverflowLimit);
        Invalidate();
    }
}
