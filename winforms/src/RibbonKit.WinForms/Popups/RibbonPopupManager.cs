using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed class RibbonPopupManager : IDisposable
{
    private sealed record Session(RibbonElement Owner, Control Opener, Rectangle Anchor, ToolStripDropDown Window, RibbonPopupSurface Surface);
    private readonly RibbonControl _ribbon;
    private readonly List<Session> _sessions = [];
    private Form? _form;
    internal RibbonPopupManager(RibbonControl ribbon) => _ribbon = ribbon;
    internal bool HasOpen => _sessions.Count > 0;
    internal bool ContainsFocus => _sessions.Any(s => s.Window.ContainsFocus);
    internal RibbonPopupSurface? Top => _sessions.LastOrDefault()?.Surface;
    internal IEnumerable<RibbonPopupSurface> Surfaces => _sessions.Select(s => s.Surface);
    internal bool IsExpanded(RibbonElement element) => _sessions.Any(s => ReferenceEquals(s.Owner, element));
    internal void Toggle(RibbonElement owner, Control opener, Rectangle anchor)
    {
        if (_ribbon.IsInDesignMode)
            return;
        if (IsExpanded(owner))
        {
            Close(owner, true);
            return;
        }
        if (owner is RibbonDropDownItem drop && !drop.CanOpen())
            return;
        if (ReferenceEquals(opener, _ribbon))
            CloseAll(false);
        else
        {
            int parent = _sessions.FindIndex(s => ReferenceEquals(s.Surface, opener));
            while (_sessions.Count > parent + 1)
                CloseTop(false);
        }
        if (_form is null)
        {
            _form = _ribbon.FindForm();
            if (_form is not null)
            {
                _form.Deactivate += OnDeactivate;
                _form.LocationChanged += OnMove;
                _form.ResizeBegin += OnMove;
            }
        }
        var screenAnchor = opener.RectangleToScreen(anchor);
        var work = Screen.FromRectangle(screenAnchor).WorkingArea;
        var surface = new RibbonPopupSurface(_ribbon, owner, work.Size);
        var host = new ToolStripControlHost(surface) { AutoSize = false, Size = surface.Size, Margin = Padding.Empty, Padding = Padding.Empty };
        var window = new ToolStripDropDown { AutoSize = false, AutoClose = false, Padding = Padding.Empty, Margin = Padding.Empty, Size = surface.Size, AccessibleName = owner.Text + " commands" };
        window.Items.Add(host);
        var session = new Session(owner, opener, anchor, window, surface);
        _sessions.Add(session);
        window.Closed += (_, _) => { if (_sessions.Contains(session)) Close(owner, false); };
        Point location = new(Math.Clamp(screenAnchor.Left, work.Left, Math.Max(work.Left, work.Right - window.Width)), screenAnchor.Bottom);
        if (location.Y + window.Height > work.Bottom)
            location.Y = Math.Max(work.Top, screenAnchor.Top - window.Height);
        window.Show(location);
        surface.FocusFirst();
        _ribbon.AccessibilityChanged();
    }
    private void OnDeactivate(object? sender, EventArgs e)
    {
        if (_ribbon.IsHandleCreated && !_ribbon.IsDisposed)
            _ribbon.BeginInvoke(() => { if (_form?.ContainsFocus != true && !ContainsFocus) CloseAll(false); });
    }
    private void OnMove(object? sender, EventArgs e) => CloseAll(false);
    internal void OutsidePress(Point point)
    {
        int inside = _sessions.FindLastIndex(s => s.Window.Bounds.Contains(point) || s.Opener.RectangleToScreen(s.Anchor).Contains(point));
        if (inside < 0)
            CloseAll(false);
        else
            while (_sessions.Count > inside + 1)
                CloseTop(false);
    }
    internal void Close(RibbonElement owner, bool restoreFocus)
    {
        int index = _sessions.FindIndex(s => ReferenceEquals(s.Owner, owner));
        if (index < 0)
            return;
        while (_sessions.Count > index + 1)
            CloseTop(false);
        CloseTop(restoreFocus);
    }
    internal void CloseTop(bool restoreFocus)
    {
        if (_sessions.Count == 0)
            return;
        var session = _sessions[^1];
        _sessions.RemoveAt(_sessions.Count - 1);
        session.Window.Close();
        session.Window.Dispose();
        if (session.Owner is RibbonDropDownItem drop)
            drop.OnClosed();
        if (restoreFocus && !session.Opener.IsDisposed)
        {
            if (session.Opener is RibbonPopupSurface surface)
                surface.FocusElement(session.Owner);
            else if (_ribbon.AllElements.Contains(session.Owner) && session.Owner.Visible)
                _ribbon.FocusElement(session.Owner);
            else
                _ribbon.Focus();
        }
        _ribbon.AccessibilityChanged();
    }
    internal void CloseAll(bool restoreFocus)
    {
        while (_sessions.Count > 0)
            CloseTop(restoreFocus && _sessions.Count == 1);
    }
    internal void InvalidateAll()
    {
        foreach (var session in _sessions)
            session.Surface.Invalidate();
    }
    internal void ResetOwner()
    {
        CloseAll(false);
        if (_form is not null)
        {
            _form.Deactivate -= OnDeactivate;
            _form.LocationChanged -= OnMove;
            _form.ResizeBegin -= OnMove;
            _form = null;
        }
    }
    public void Dispose() => ResetOwner();
}

internal sealed partial class RibbonPopupSurface : ScrollableControl
{
    private readonly RibbonControl _ribbon;
    private readonly List<RibbonTarget> _targets = [];
    private RibbonElement? _hot, _pressed, _focused;
    internal RibbonElement OwnerElement
    {
        get;
    }
    internal RibbonControl Ribbon => _ribbon;
    internal RibbonElement? FocusedElement => _focused;
    internal IReadOnlyList<RibbonTarget> Targets => _targets.Select(t => t with { Bounds = new(t.Bounds.X + AutoScrollPosition.X, t.Bounds.Y + AutoScrollPosition.Y, t.Bounds.Width, t.Bounds.Height), Clip = ClientRectangle }).ToArray();
    internal RibbonPopupSurface(RibbonControl ribbon, RibbonElement owner, Size workArea)
    {
        _ribbon = ribbon;
        OwnerElement = owner;
        Font = ribbon.Font;
        AutoScroll = true;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        int S(double n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi);
        int width, height;
        if (owner is RibbonGroup group)
        {
            var measured = ribbon.Engine.Measure(group, 0, Font, ribbon.DeviceDpi, ribbon.LayoutTheme);
            width = measured.Width + S(8);
            height = S(ribbon.LayoutTheme.BodyHeight) + S(8);
            foreach (var item in measured.Items)
                _targets.Add(new(TargetKind.Item, item.Item, new(item.Bounds.X + S(4), item.Bounds.Y + S(4), item.Bounds.Width, item.Bounds.Height), Rectangle.Empty, item.Size, item.ReserveIcon));
        }
        else
        {
            var items = ((RibbonDropDownItem)owner).Items.Where(i => i.Visible).ToArray();
            width = Math.Max(S(170), items.Select(i => TextRenderer.MeasureText(i.Text, Font).Width + S(56)).DefaultIfEmpty(S(170)).Max());
            int rowHeight = Math.Max(S(28), Font.Height + S(10));
            height = Math.Max(S(32), items.Length * rowHeight + S(8));
            for (int i = 0; i < items.Length; i++)
                _targets.Add(new(TargetKind.Item, items[i], new(S(4), S(4) + i * rowHeight, width - S(8), rowHeight), Rectangle.Empty, RibbonItemSize.Medium));
        }
        AutoScrollMinSize = new(width, height);
        Size = new(Math.Min(width + SystemInformation.VerticalScrollBarWidth, workArea.Width - S(24)), Math.Min(height + (width > workArea.Width - S(24) ? SystemInformation.HorizontalScrollBarHeight : 0), workArea.Height - S(32)));
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var context = new RibbonPaintContext(e.Graphics, ClientRectangle, _ribbon.Theme, Font, DeviceDpi);
        _ribbon.Painter.DrawSurface(context, true);
        foreach (var target in Targets)
            if (target.Element is RibbonItem item)
                _ribbon.Painter.DrawItem(context with
                {
                    Bounds = target.Bounds,
                    Hot = ReferenceEquals(_hot, item),
                    Pressed = ReferenceEquals(_pressed, item),
                    Focused = Focused && ReferenceEquals(_focused, item),
                    ReserveIcon = target.ReserveIcon
                }, item, target.Size, OwnerElement is RibbonDropDownItem);
        if (_ribbon.KeyTipsActive && ReferenceEquals(_ribbon.Popups.Top, this))
            _ribbon.DrawTips(e.Graphics, Targets.Where(t => t.Enabled).ToArray());
    }
    private RibbonTarget? Hit(Point point) => Targets.FirstOrDefault(t => t.VisibleBounds.Contains(point));
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _hot = Hit(e.Location)?.Element;
        Invalidate();
    }
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hot = null;
        Invalidate();
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;
        _pressed = Hit(e.Location)?.Element;
        Capture = true;
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        var hit = Hit(e.Location);
        var pressed = _pressed;
        _pressed = null;
        Capture = false;
        if (e.Button == MouseButtons.Left && hit?.Element is { } item && ReferenceEquals(pressed, item))
        {
            _ribbon.ExitKeyTips(false);
            _ribbon.Activate(hit);
        }
    }
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        _pressed = null;
        Invalidate();
    }
    internal void FocusFirst()
    {
        if (Targets.FirstOrDefault(t => t.Enabled)?.Element is { } item)
            FocusElement(item);
        else
            Focus();
    }
    internal void FocusElement(RibbonElement item)
    {
        _focused = item;
        Focus();
        if (_targets.FirstOrDefault(t => ReferenceEquals(t.Element, item)) is { } target)
        {
            int x = -AutoScrollPosition.X, y = -AutoScrollPosition.Y;
            if (target.Bounds.Left < x)
                x = target.Bounds.Left;
            else if (target.Bounds.Right > x + ClientSize.Width)
                x = target.Bounds.Right - ClientSize.Width;
            if (target.Bounds.Top < y)
                y = target.Bounds.Top;
            else if (target.Bounds.Bottom > y + ClientSize.Height)
                y = target.Bounds.Bottom - ClientSize.Height;
            AutoScrollPosition = new(Math.Max(0, x), Math.Max(0, y));
        }
        Invalidate();
        _ribbon.AccessibilityFocusChanged(item);
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keys)
    {
        if (keys == Keys.Escape)
        {
            _ribbon.Popups.CloseTop(true);
            return true;
        }
        var targets = Targets.Where(t => t.Enabled).ToArray();
        if (keys is Keys.Enter or Keys.Space or Keys.Right)
        {
            var target = targets.FirstOrDefault(t => ReferenceEquals(t.Element, _focused));
            if (target is not null && (keys != Keys.Right || target.Element is RibbonDropDownItem))
            {
                _ribbon.Activate(target);
                return true;
            }
        }
        if (keys is Keys.Up or Keys.Down or Keys.Left or Keys.Right or Keys.Home or Keys.End or Keys.Tab or (Keys.Tab | Keys.Shift))
        {
            int i = Array.FindIndex(targets, t => ReferenceEquals(t.Element, _focused));
            int next = keys == Keys.Home ? 0 : keys == Keys.End ? targets.Length - 1 : (i + (keys is Keys.Up or Keys.Left or (Keys.Tab | Keys.Shift) ? -1 : 1) + targets.Length) % Math.Max(1, targets.Length);
            if (targets.Length > 0)
                FocusElement(targets[next].Element!);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keys);
    }
}
