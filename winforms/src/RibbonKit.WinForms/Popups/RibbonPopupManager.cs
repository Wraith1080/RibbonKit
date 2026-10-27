using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed class RibbonPopupManager : IDisposable
{
    private sealed record Session(RibbonElement Owner, RibbonDropDownItem? MenuSource, Control Opener, Rectangle Anchor, ToolStripDropDown Window, RibbonPopupSurface Surface);
    private readonly RibbonControl _ribbon;
    private readonly List<Session> _sessions = [];
    private Form? _form;
    private bool _activationCheckPending;
    private RibbonDropDownItem? _preparingMenu;
    // Opening handlers may construct descendants without dismissing the parent
    // overflow. Changes to the source or its opener still close stale sessions.
    internal bool IsPreparingContent(RibbonElement? element, bool includeOwner = false)
    {
        if (_preparingMenu is null || element is null) return false;
        if (!includeOwner) element = element.Parent as RibbonElement;
        for (; element is not null; element = element.Parent as RibbonElement)
            if (ReferenceEquals(element, _preparingMenu)) return true;
        return false;
    }
    internal RibbonPopupManager(RibbonControl ribbon) => _ribbon = ribbon;
    internal bool HasOpen => _sessions.Count > 0;
    internal bool ContainsFocus => _sessions.Any(s => s.Window.ContainsFocus);
    internal bool OwnsWindow(nint handle) => _sessions.Any(s => s.Window.IsHandleCreated && s.Window.Handle == handle);
    internal void PrepareChoice(Control opener)
    {
        int parent = _sessions.FindIndex(s => ReferenceEquals(s.Surface, opener));
        while (_sessions.Count > parent + 1) CloseTop(false);
    }
    internal RibbonPopupSurface? Top => _sessions.LastOrDefault()?.Surface;
    internal IEnumerable<RibbonPopupSurface> Surfaces => _sessions.Select(s => s.Surface);
    internal bool IsExpanded(RibbonElement element) => _sessions.Any(s => ReferenceEquals(s.Owner, element));
    internal void Toggle(RibbonElement owner, Control opener, Rectangle anchor)
    {
        _ribbon.ScreenTips.Cancel();
        if (_ribbon.IsInDesignMode)
            return;
        if (IsExpanded(owner))
        {
            Close(owner, true);
            return;
        }
        var menuSource = owner is RibbonQuickAccessDropDownReference reference ? reference.Source : owner as RibbonDropDownItem;
        var groupSource = owner is RibbonQuickAccessGroupReference groupReference ? groupReference.Source : owner as RibbonGroup;
        if (!CurrentProjection(owner, menuSource, groupSource)) return;
        // The same source menu has one live presentation, including its editors.
        if (menuSource is not null && _sessions.FirstOrDefault(s => ReferenceEquals(s.MenuSource, menuSource)) is { } previous)
            Close(previous.Owner, false);
        if (groupSource is not null && _sessions.FirstOrDefault(s => ReferenceEquals(s.Surface.GroupSource, groupSource)) is { } previousGroup)
            Close(previousGroup.Owner, false);
        if (menuSource is not null)
        {
            var priorPreparation = _preparingMenu; _preparingMenu = menuSource;
            bool allowed;
            try { allowed = menuSource.CanOpen(); }
            finally { _preparingMenu = priorPreparation; }
            if (!allowed) return;
        }
        if (!CurrentProjection(owner, menuSource, groupSource)) return;
        // Opening callbacks may remove/hide an owner or destroy its current
        // presentation. Revalidate before creating another popup session.
        if (opener.IsDisposed || !opener.Visible || !_ribbon.Visible || _ribbon.IsDisposed || !owner.EffectiveEnabled || !owner.Visible)
            return;
        var currentTargets = opener is RibbonPopupSurface popup ? popup.Targets : _ribbon.Snapshot.Targets;
        if (!currentTargets.Any(t => ReferenceEquals(t.Element, owner) && !t.VisibleBounds.IsEmpty))
            return;
        anchor = currentTargets.Where(t => ReferenceEquals(t.Element, owner)).Select(t => t.VisibleBounds).Where(r => !r.IsEmpty)
            .Aggregate(Rectangle.Empty, (a, b) => a.IsEmpty ? b : Rectangle.Union(a, b));
        if (ReferenceEquals(opener, _ribbon))
            CloseAll(false);
        else
        {
            int parent = _sessions.FindIndex(s => ReferenceEquals(s.Surface, opener));
            while (_sessions.Count > parent + 1)
                CloseTop(false);
        }
        if (opener.IsDisposed || !opener.Visible || owner.IsDisposed || !owner.EffectiveEnabled || !owner.Visible
            || !CurrentProjection(owner, menuSource, groupSource)) return;
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
        var placementAnchor = owner is RibbonTab
            ? opener.RectangleToScreen(new Rectangle(0, opener.ClientSize.Height - 1, opener.ClientSize.Width, 1)) : screenAnchor;
        var work = Screen.FromRectangle(placementAnchor).WorkingArea;
        var surface = new RibbonPopupSurface(_ribbon, owner, new(Math.Max(1, work.Width - 2), Math.Max(1, work.Height - 2)), menuSource, groupSource);
        var host = new ToolStripControlHost(surface) { AutoSize = false, Size = surface.Size, Margin = Padding.Empty, Padding = Padding.Empty };
        var window = new RibbonPopupWindow(_ribbon, QueueActivationCheck)
        {
            AutoSize = false, AutoClose = false, Padding = new(1), Margin = Padding.Empty,
            Size = new(surface.Width + 2, surface.Height + 2), DropShadowEnabled = false,
            AccessibleName = (owner is RibbonQuickAccessDropDownReference or RibbonQuickAccessGroupReference ? RibbonQuickAccess.Caption(owner) : owner.Text) + " commands"
        };
        window.Items.Add(host);
        var session = new Session(owner, menuSource, opener, anchor, window, surface);
        _sessions.Add(session);
        window.Closed += (_, _) => { if (_sessions.Contains(session)) Close(owner, false); };
        bool submenu = opener is RibbonPopupSurface parentSurface && (parentSurface.MenuSource is not null || parentSurface.OwnerElement is RibbonQuickAccessOverflow);
        var bounds = CalculateBounds(placementAnchor, work, window.Size, submenu, _ribbon.DeviceDpi);
        if (bounds.Size != window.Size)
        {
            window.Size = bounds.Size;
            surface.Size = new(Math.Max(1, bounds.Width - 2), Math.Max(1, bounds.Height - 2));
            host.Size = surface.Size;
        }
        window.Show(bounds.Location);
        opener.Invalidate();
        _ribbon.Invalidate();
        _ribbon.SyncEditors();
        surface.FocusFirst();
        _ribbon.AccessibilityChanged();
    }
    private static bool CurrentProjection(RibbonElement owner, RibbonDropDownItem? menuSource, RibbonGroup? groupSource) => owner switch
    {
        RibbonQuickAccessDropDownReference menu => RibbonQuickAccess.Available(menu) && ReferenceEquals(menu.Source, menuSource),
        RibbonQuickAccessGroupReference group => RibbonQuickAccess.Available(group) && ReferenceEquals(group.Source, groupSource),
        _ => true
    };
    internal static Rectangle CalculateBounds(Rectangle anchor, Rectangle work, Size size, bool submenu, int dpi)
    {
        int inset = RibbonLayoutEngine.Scale(8, dpi), rowPadding = RibbonLayoutEngine.Scale(4, dpi);
        if (work.Width > 2 * inset && work.Height > 2 * inset) work.Inflate(-inset, -inset);
        size = new(Math.Max(1, Math.Min(size.Width, work.Width)), Math.Max(1, Math.Min(size.Height, work.Height)));
        // The row is inset by the menu padding and the one-device-pixel frame.
        int x = submenu ? anchor.Right + rowPadding + 1 : anchor.Left;
        int y = submenu ? anchor.Top - rowPadding : anchor.Bottom;
        if (submenu && x + size.Width > work.Right) x = anchor.Left - size.Width - rowPadding - 1;
        if (!submenu && y + size.Height > work.Bottom) y = anchor.Top - size.Height;
        return new(Math.Clamp(x, work.Left, work.Right - size.Width), Math.Clamp(y, work.Top, work.Bottom - size.Height), size.Width, size.Height);
    }
    private void OnDeactivate(object? sender, EventArgs e) => QueueActivationCheck();
    internal void QueueActivationCheck()
    {
        if (_activationCheckPending || !HasOpen || !_ribbon.IsHandleCreated || _ribbon.IsDisposed)
            return;
        _activationCheckPending = true;
        _ribbon.BeginInvoke(() =>
        {
            _activationCheckPending = false;
            if (!HasOpen || _ribbon.IsDisposed)
                return;
            // Per-thread keyboard focus can still identify a popup after another
            // application becomes active. Use the real foreground window instead.
            nint foreground = GetForegroundWindow();
            if (_form is { IsHandleCreated: true } && foreground == _form.Handle)
                return;
            if (_sessions.Any(s => s.Window.IsHandleCreated && s.Window.Handle == foreground))
                return;
            if (_ribbon.Editors.OwnsWindow(foreground)) return;
            CloseAll(false);
            _ribbon.ExitKeyTips(false);
        });
    }
    private void OnMove(object? sender, EventArgs e) => CloseAll(false);
    internal void OutsidePress(Point point)
    {
        int inside = _sessions.FindLastIndex(s =>
            (s.Window.Bounds.Contains(point) && (s.Window.Region is null || s.Window.Region.IsVisible(s.Window.PointToClient(point))))
            || s.Opener.RectangleToScreen(s.Anchor).Contains(point));
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
    internal void CloseMenu(RibbonDropDownItem source, bool restoreFocus)
    {
        if (_sessions.FirstOrDefault(s => ReferenceEquals(s.MenuSource, source)) is { } session) Close(session.Owner, restoreFocus);
    }
    internal void CloseTop(bool restoreFocus)
    {
        _ribbon.ScreenTips.Cancel();
        if (_sessions.Count == 0)
            return;
        var session = _sessions[^1];
        _sessions.RemoveAt(_sessions.Count - 1);
        if (!HasOpen)
            _activationCheckPending = false;
        _ribbon.Editors.Release(session.Surface);
        session.Window.Close();
        session.Window.Dispose();
        if (!session.Opener.IsDisposed) session.Opener.Invalidate();
        _ribbon.Invalidate();
        session.MenuSource?.OnClosed();
        if (session.Owner is RibbonTab) _ribbon.TabPopupClosed();
        if (restoreFocus && !session.Opener.IsDisposed)
        {
            if (session.Opener is RibbonPopupSurface surface)
                surface.FocusElement(session.Owner, RibbonSplitLayout.IsSplit(session.Owner));
            else if ((_ribbon.AllElements.Contains(session.Owner) || session.Owner is RibbonQuickAccessOverflow) && session.Owner.Visible)
                _ribbon.FocusElement(session.Owner, RibbonSplitLayout.IsSplit(session.Owner));
            else
                _ribbon.Focus();
        }
        _ribbon.SyncEditors();
        _ribbon.AccessibilityChanged();
    }
    internal void CloseAll(bool restoreFocus)
    {
        while (_sessions.Count > 0)
            CloseTop(restoreFocus && _sessions.Count == 1);
    }
    internal void InvalidateAll()
    {
        foreach (var session in _sessions.ToArray())
        {
            if (!_sessions.Contains(session)) continue;
            if (!CurrentProjection(session.Owner, session.MenuSource, session.Surface.GroupSource)
                || ReferenceEquals(session.Owner, _ribbon.FileMenu) && (!session.Owner.EffectiveEnabled || !session.Owner.Visible)
                || session.Surface.GroupSource is { } group && (!group.EffectiveEnabled || !group.Visible || !ReferenceEquals(group.Ribbon, _ribbon)))
            { Close(session.Owner, false); continue; }
            session.Surface.Invalidate();
            session.Window.Invalidate();
        }
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
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();


}

internal sealed partial class RibbonPopupSurface : ScrollableControl
{
    private readonly RibbonControl _ribbon;
    private readonly List<RibbonTarget> _targets = [];
    private readonly int _contentPadding;
    private readonly bool _singleColumn;
    private RibbonLayoutSnapshot? _bodyLayout;
    private int _bodyOffset, _bodyWidth;
    private RibbonElement? _hot, _pressed, _focused;
    private TargetKind? _hotKind, _pressedKind;
    private bool _hotDropDown, _pressedDropDown, _focusedDropDown;
    internal RibbonElement OwnerElement
    {
        get;
    }
    internal RibbonControl Ribbon => _ribbon;
    internal RibbonDropDownItem? MenuSource { get; }
    internal RibbonGroup? GroupSource { get; }
    internal RibbonElement? FocusedElement => _focused;
    internal bool FocusedDropDownPart => _focusedDropDown;
    internal IReadOnlyList<RibbonTarget> Targets => _targets.Select(t => t with
    {
        Bounds = new(t.Bounds.X + AutoScrollPosition.X, t.Bounds.Y + (OwnerElement is RibbonQuickAccessOverflow ? OverflowViewport.Top - _overflowOffset : AutoScrollPosition.Y),
            OwnerElement is RibbonQuickAccessOverflow ? Math.Max(0, ClientSize.Width - 2 * _contentPadding)
                : _singleColumn ? Math.Max(t.Bounds.Width, ClientSize.Width - t.Bounds.X - _contentPadding) : t.Bounds.Width, t.Bounds.Height),
        Clip = OwnerElement is RibbonQuickAccessOverflow ? OverflowViewport : _bodyLayout is null ? ClientRectangle : Rectangle.Intersect(ClientRectangle,
            new(t.Clip.X + AutoScrollPosition.X, t.Clip.Y + AutoScrollPosition.Y, t.Clip.Width, t.Clip.Height))
    }).SelectMany(t => RibbonSplitLayout.Expand(t, _ribbon.DeviceDpi, MenuSource is not null || OwnerElement is RibbonQuickAccessOverflow)).ToArray();
    internal RibbonPopupSurface(RibbonControl ribbon, RibbonElement owner, Size workArea, RibbonDropDownItem? menuSource = null, RibbonGroup? groupSource = null)
    {
        _ribbon = ribbon;
        OwnerElement = owner;
        MenuSource = menuSource ?? (owner as RibbonDropDownItem) ?? (owner as RibbonQuickAccessDropDownReference)?.Source;
        GroupSource = groupSource ?? (owner as RibbonGroup) ?? (owner as RibbonQuickAccessGroupReference)?.Source;
        Font = ribbon.Font;
        AutoScroll = owner is not RibbonQuickAccessOverflow;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        int S(double n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi);
        _contentPadding = S(4);
        int width, height;
        if (owner is RibbonTab tab)
        {
            width = _bodyWidth = Math.Clamp(Math.Max(S(200), ribbon.ClientSize.Width), 1, Math.Max(1, workArea.Width - S(24)));
            _bodyLayout = ribbon.Engine.BuildBody(ribbon, tab, width, 0);
            _targets.AddRange(_bodyLayout.Targets);
            height = _bodyLayout.BodyBounds.Bottom + S(ribbon.LayoutTheme.BodyMargin.Bottom);
        }
        else if (GroupSource is { } group)
        {
            var measured = ribbon.Engine.Measure(group, 0, Font, ribbon.DeviceDpi, ribbon.LayoutTheme);
            // Group measurement includes a strip caption and trailing column
            // spacing. Neither is painted in the flyout: fit its command bounds.
            int left = measured.Items.Select(i => i.Bounds.Left).DefaultIfEmpty(0).Min();
            int top = measured.Items.Select(i => i.Bounds.Top).DefaultIfEmpty(0).Min();
            int right = measured.Items.Select(i => i.Bounds.Right).DefaultIfEmpty(S(62)).Max();
            int bottom = measured.Items.Select(i => i.Bounds.Bottom).DefaultIfEmpty(S(24)).Max();
            width = right - left + 2 * _contentPadding;
            height = bottom - top + 2 * _contentPadding;
            foreach (var item in measured.Items)
                _targets.Add(new(TargetKind.Item, item.Item, new(item.Bounds.X - left + _contentPadding, item.Bounds.Y - top + _contentPadding, item.Bounds.Width, item.Bounds.Height), Rectangle.Empty, item.Size, item.ReserveIcon, LabelWidth: item.LabelWidth));
        }
        else if (owner is RibbonQuickAccessOverflow)
        {
            var items = ribbon.Snapshot.QuickAccessOverflowItems;
            width = Math.Max(S(170), items.Select(e => TextRenderer.MeasureText(RibbonQuickAccess.Caption(e), Font).Width + S(56)).DefaultIfEmpty(S(170)).Max());
            if (items.Select(RibbonQuickAccess.Command).Any(c => c is not null && RibbonMenuLayout.HasDescription(c))) width = Math.Max(width, S(280));
            int top = S(4);
            var visible = ribbon.QuickAccessItems.Where(e => e.Visible).ToList();
            foreach (var entry in items)
            {
                int rowHeight = RibbonQuickAccess.Command(entry) is { } command ? RibbonMenuLayout.RowHeight(command, width - S(8), Font, ribbon.DeviceDpi) : S(28);
                _targets.Add(new(TargetKind.QuickAccess, entry, new(S(4), top, width - S(8), rowHeight), Rectangle.Empty, RibbonItemSize.Medium,
                    AutomaticKeyTip: RibbonQuickAccess.AutomaticKeyTip(visible.IndexOf(entry), visible.Count)));
                top += rowHeight;
            }
            height = Math.Max(S(32), top + S(4));
        }
        else
        {
            var items = MenuSource!.Items.Where(i => i.Visible).ToArray();
            width = Math.Max(S(170), items.Where(i => i is not RibbonMenuSeparatorItem).Select(i => TextRenderer.MeasureText(i.Text, Font).Width + S(56)).DefaultIfEmpty(S(170)).Max());
            if (items.Any(RibbonMenuLayout.HasDescription)) width = Math.Max(width, S(280));
            int labelWidth = items.Where(i => i is IRibbonEditorItem).Select(i => RibbonEditorLayout.LabelWidth(i, Font, ribbon.DeviceDpi)).DefaultIfEmpty(0).Max();
            width = Math.Max(width, items.OfType<IRibbonEditorItem>().Select(i => labelWidth + S(i.InputWidth + 14)).DefaultIfEmpty(0).Max());
            int top = S(4);
            for (int i = 0; i < items.Length; i++)
            {
                int rowHeight = RibbonMenuLayout.RowHeight(items[i], width - S(8), Font, ribbon.DeviceDpi);
                _targets.Add(new(TargetKind.Item, items[i], new(S(4), top, width - S(8), rowHeight), Rectangle.Empty, RibbonItemSize.Medium, LabelWidth: labelWidth));
                top += rowHeight;
            }
            height = Math.Max(S(32), top + S(4));
        }
        _singleColumn = owner is not RibbonTab && _targets.Select(t => t.Bounds.Left).Distinct().Take(2).Count() <= 1;
        int maxWidth = Math.Max(1, workArea.Width - S(24)), maxHeight = Math.Max(1, workArea.Height - S(32));
        if (owner is RibbonQuickAccessOverflow)
        {
            _overflowExtent = height;
            InitializeOverflowScrolling();
            Size = new(Math.Min(width, maxWidth), Math.Min(height, maxHeight));
            return;
        }
        AutoScrollMinSize = new(width, height);
        int verticalBar = SystemInformation.GetVerticalScrollBarWidthForDpi(ribbon.DeviceDpi);
        int horizontalBar = SystemInformation.GetHorizontalScrollBarHeightForDpi(ribbon.DeviceDpi);
        bool vertical = height > maxHeight;
        bool horizontal = width + (vertical ? verticalBar : 0) > maxWidth;
        vertical = height + (horizontal ? horizontalBar : 0) > maxHeight;
        Size = new(Math.Min(width + (vertical ? verticalBar : 0), maxWidth),
            Math.Min(height + (horizontal ? horizontalBar : 0), maxHeight));
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var context = new RibbonPaintContext(e.Graphics, ClientRectangle, _ribbon.Theme, Font, DeviceDpi);
        _ribbon.Painter.DrawSurface(context, true);
        if (_bodyLayout is { } body)
        {
            var saved = e.Graphics.Save();
            try
            {
                var clip = body.BodyViewport; clip.Offset(AutoScrollPosition);
                e.Graphics.SetClip(Rectangle.Intersect(ClientRectangle, clip));
                foreach (var slot in body.Groups)
                {
                    var bounds = slot.Bounds; bounds.Offset(AutoScrollPosition);
                    _ribbon.Painter.DrawGroup(context with { Bounds = bounds,
                        Hot = slot.Group.EffectiveEnabled && ReferenceEquals(_hot, slot.Group),
                        Pressed = slot.Group.EffectiveEnabled && ReferenceEquals(_pressed, slot.Group),
                        Focused = slot.Group.EffectiveEnabled && _ribbon.KeyboardFocusCues && Focused && ReferenceEquals(_focused, slot.Group) }, slot.Group, slot.Collapsed);
                }
            }
            finally { e.Graphics.Restore(saved); }
        }
        var targets = Targets;
        foreach (var target in targets.Where(t => !t.DropDownPart))
        {
            if (target.VisibleBounds.IsEmpty && !(RibbonSplitLayout.IsSplit(target.Element)
                && targets.Any(t => t.DropDownPart && ReferenceEquals(t.Element, target.Element) && !t.VisibleBounds.IsEmpty))) continue;
            var saved = e.Graphics.Save();
            try
            {
                e.Graphics.SetClip(target.Clip);
                if (target.Kind == TargetKind.QuickAccess)
                {
                    var primary = context with { Bounds = target.Bounds,
                        Hot = target.Enabled && !_hotDropDown && ReferenceEquals(_hot, target.Element), Pressed = target.Enabled && !_pressedDropDown && ReferenceEquals(_pressed, target.Element),
                        Focused = target.Enabled && !_focusedDropDown && _ribbon.KeyboardFocusCues && Focused && ReferenceEquals(_focused, target.Element) };
                    if (target.Element is RibbonQuickAccessDropDownReference { Source: RibbonSplitButtonItem } splitReference)
                    {
                        var arrow = targets.First(t => ReferenceEquals(t.Element, target.Element) && t.DropDownPart);
                        _ribbon.Painter.DrawQuickAccessSplitItem(primary, primary with { Bounds = arrow.Bounds,
                            Hot = target.Enabled && _hotDropDown && ReferenceEquals(_hot, target.Element), Pressed = target.Enabled && _pressedDropDown && ReferenceEquals(_pressed, target.Element),
                            Focused = target.Enabled && _focusedDropDown && _ribbon.KeyboardFocusCues && Focused && ReferenceEquals(_focused, target.Element) }, splitReference, true);
                    }
                    else _ribbon.Painter.DrawQuickAccessItem(primary, target.Element!, true);
                }
                else if (target.Element is RibbonItem item && item is not IRibbonEditorItem)
                {
                    var primary = context with
                    {
                        Bounds = target.Bounds,
                        Hot = target.Enabled && !_hotDropDown && ReferenceEquals(_hot, item),
                        Pressed = target.Enabled && !_pressedDropDown && ReferenceEquals(_pressed, item),
                        Focused = target.Enabled && !_focusedDropDown && _ribbon.KeyboardFocusCues && Focused && ReferenceEquals(_focused, item),
                        ReserveIcon = target.ReserveIcon
                    };
                    if (item is RibbonSplitButtonItem split)
                    {
                        var arrow = targets.First(t => ReferenceEquals(t.Element, item) && t.DropDownPart);
                        _ribbon.Painter.DrawSplitItem(primary, primary with
                        {
                            Bounds = arrow.Bounds,
                            Hot = target.Enabled && _hotDropDown && ReferenceEquals(_hot, item),
                            Pressed = target.Enabled && _pressedDropDown && ReferenceEquals(_pressed, item),
                            Focused = target.Enabled && _focusedDropDown && _ribbon.KeyboardFocusCues && Focused && ReferenceEquals(_focused, item)
                        }, split, target.Size, MenuSource is not null);
                    }
                    else
                        _ribbon.Painter.DrawItem(primary, item, target.Size, MenuSource is not null);
                }
                else if (target.Kind is TargetKind.BodyBack or TargetKind.BodyNext)
                    _ribbon.Painter.DrawScrollButton(context with { Bounds = target.Bounds, Hot = _hotKind == target.Kind,
                        Pressed = _pressedKind == target.Kind, IsBodyScrollButton = true }, target.Kind == TargetKind.BodyBack);
            }
            finally { e.Graphics.Restore(saved); }
        }
        if (_ribbon.KeyTipsActive && ReferenceEquals(_ribbon.Popups.Top, this))
        {
            var saved = e.Graphics.Save();
            try
            {
                if (OwnerElement is RibbonQuickAccessOverflow) e.Graphics.SetClip(OverflowViewport);
                _ribbon.DrawTips(e.Graphics, Targets.Where(t => t.Enabled && t.Element is not null).ToArray());
            }
            finally { e.Graphics.Restore(saved); }
        }
        PaintOverflowScrollers(context);
    }
    private RibbonTarget? Hit(Point point) => HitOverflowScroller(point) ?? Targets.FirstOrDefault(t => t.Enabled && t.VisibleBounds.Contains(point));
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var target = Hit(e.Location);
        _hot = target?.Element;
        _hotKind = target?.Element is null ? target?.Kind : null;
        _hotDropDown = target?.DropDownPart ?? false;
        UpdateOverflowHover(_hotKind);
        Invalidate();
    }
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hot = null;
        _hotKind = null;
        StopOverflowRepeat();
        Invalidate();
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        _ribbon.SetKeyboardFocusCues(false);
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;
        var hit = Hit(e.Location);
        if (hit is null)
        {
            _pressed = null;
            Capture = false;
            return;
        }
        _pressed = hit.Element;
        _pressedKind = hit.Element is null ? hit.Kind : null;
        _pressedDropDown = hit.DropDownPart;
        Capture = true;
        if (IsOverflowScroller(hit.Kind))
        {
            ScrollOverflow(hit.Kind == TargetKind.OverflowDown);
            StartOverflowRepeat(hit.Kind, true);
            Invalidate();
            return;
        }
        if (hit.Element is { } element)
            FocusElement(element, hit.DropDownPart);
        Invalidate();
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        var hit = Hit(e.Location);
        var pressed = _pressed;
        var kind = _pressedKind;
        _pressed = null;
        Capture = false;
        if (IsOverflowScroller(kind))
        {
            StopOverflowRepeat();
            UpdateOverflowHover(hit?.Kind);
            Invalidate();
            return;
        }
        if (e.Button == MouseButtons.Left && hit is not null && ReferenceEquals(pressed, hit.Element)
            && (hit.Element is not null || kind == hit.Kind) && _pressedDropDown == hit.DropDownPart)
        {
            _ribbon.ExitKeyTips(false);
            ActivateTarget(hit);
        }
    }
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        _pressed = null;
        _pressedKind = null;
        StopOverflowRepeat();
        Invalidate();
    }
    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        StopOverflowRepeat();
        _ribbon.Popups.QueueActivationCheck();
    }
    internal void FocusFirst()
    {
        if (Targets.FirstOrDefault(t => t.Enabled && t.Element is not null)?.Element is { } item)
            FocusElement(item);
        else
            Focus();
    }
    internal void FocusElement(RibbonElement item, bool dropDownPart = false, bool enterEditor = true)
    {
        if (item is RibbonMenuSeparatorItem || !item.EffectiveEnabled || !item.Visible) return;
        _focused = item;
        _focusedDropDown = RibbonSplitLayout.IsSplit(item) && dropDownPart;
        Focus();
        if (_bodyLayout is { } body && _targets.FirstOrDefault(t => ReferenceEquals(t.Element, item)) is { } bodyTarget)
        {
            int delta = bodyTarget.Bounds.Left < body.BodyViewport.Left ? bodyTarget.Bounds.Left - body.BodyViewport.Left
                : bodyTarget.Bounds.Right > body.BodyViewport.Right ? bodyTarget.Bounds.Right - body.BodyViewport.Right : 0;
            if (delta != 0) ScrollBody(delta);
        }
        if (_targets.SelectMany(t => RibbonSplitLayout.Expand(t, _ribbon.DeviceDpi, MenuSource is not null || OwnerElement is RibbonQuickAccessOverflow)).FirstOrDefault(t => ReferenceEquals(t.Element, item) && t.DropDownPart == _focusedDropDown) is { } target)
        {
            if (OwnerElement is RibbonQuickAccessOverflow)
            {
                int offset = _overflowOffset;
                if (target.Bounds.Top - _contentPadding < offset) offset = target.Bounds.Top - _contentPadding;
                else if (target.Bounds.Bottom + _contentPadding > offset + OverflowViewport.Height) offset = target.Bounds.Bottom + _contentPadding - OverflowViewport.Height;
                SetOverflowOffset(offset);
            }
            else
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
                AutoScrollPosition = new(_bodyLayout is null ? Math.Max(0, x) : 0, Math.Max(0, y));
            }
        }
        Invalidate();
        _ribbon.SyncEditors();
        if (enterEditor && item is RibbonItem editor && editor is IRibbonEditorItem) _ribbon.Editors.Focus(editor);
        _ribbon.AccessibilityFocusChanged(item);
    }
    internal void EditorFocused(RibbonItem item)
    {
        _focused = item;
        _focusedDropDown = false;
        Invalidate();
    }
    internal void NavigateFromEditor(RibbonItem item, Keys keys)
        => NavigateFromElement(item, keys);
    internal void NavigateFromElement(RibbonElement item, Keys keys)
    {
        FocusElement(item, enterEditor: false);
        ProcessPopupKey(keys);
    }
    protected override void OnScroll(ScrollEventArgs se)
    {
        _ribbon.ScreenTips.Cancel();
        if (_ribbon.Popups.Surfaces.Contains(this)) _ribbon.Popups.PrepareChoice(this);
        base.OnScroll(se);
        _ribbon.SyncEditors();
    }
    private void ActivateTarget(RibbonTarget target)
    {
        if (target.Kind is TargetKind.BodyBack or TargetKind.BodyNext) ScrollBody(target.Kind == TargetKind.BodyBack ? -120 : 120);
        else _ribbon.Activate(target, this);
    }
    private void ScrollBody(int amount)
    {
        if (_bodyLayout is not { } body || OwnerElement is not RibbonTab tab) return;
        _ribbon.ScreenTips.Cancel();
        if (_ribbon.Popups.Surfaces.Contains(this)) _ribbon.Popups.PrepareChoice(this);
        _bodyOffset = Math.Clamp(_bodyOffset + amount, 0, Math.Max(0, body.BodyExtent - body.BodyViewport.Width));
        _bodyLayout = _ribbon.Engine.BuildBody(_ribbon, tab, _bodyWidth, _bodyOffset);
        _targets.Clear(); _targets.AddRange(_bodyLayout.Targets);
        _ribbon.SyncEditors(); Invalidate(); _ribbon.AccessibilityChanged();
    }
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (OwnerElement is RibbonQuickAccessOverflow)
        {
            StopOverflowRepeat();
            for (int i = 0; i < Math.Max(1, Math.Abs(e.Delta) / SystemInformation.MouseWheelScrollDelta); i++) ScrollOverflow(e.Delta < 0);
            if (e is HandledMouseEventArgs handled) handled.Handled = true;
        }
        else if (_bodyLayout is { } body && body.BodyExtent > body.BodyViewport.Width) ScrollBody(e.Delta < 0 ? 120 : -120);
        else base.OnMouseWheel(e);
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keys)
        => ProcessPopupKey(keys) || base.ProcessCmdKey(ref msg, keys);
    private bool ProcessPopupKey(Keys keys)
    {
        if (!Focused) return false;
        if (keys is Keys.Escape or Keys.Enter or Keys.Space or Keys.Tab or (Keys.Tab | Keys.Shift)
            or Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End)
            _ribbon.SetKeyboardFocusCues(true);
        if (keys == Keys.Escape)
        {
            _ribbon.Popups.CloseTop(true);
            return true;
        }
        if (keys == Keys.Left && MenuSource is not null)
        {
            _ribbon.Popups.CloseTop(true);
            return true;
        }
        var targets = Targets.Where(t => t.Enabled && t.Element is not null).ToArray();
        if (keys is Keys.Enter or Keys.Space or Keys.Right)
        {
            var target = targets.FirstOrDefault(t => ReferenceEquals(t.Element, _focused) && t.DropDownPart == _focusedDropDown);
            if (target is not null && (keys != Keys.Right || target.Element is RibbonDropDownItem or RibbonQuickAccessDropDownReference or RibbonQuickAccessGroupReference))
            {
                if (keys == Keys.Right && target.Element is RibbonQuickAccessDropDownReference)
                    ActivateTarget(target with { DropDownPart = true });
                else if (keys == Keys.Right && target.Element is RibbonDropDownItem drop)
                    drop.ShowDropDown();
                else
                    ActivateTarget(target);
                return true;
            }
        }
        if (keys is Keys.Up or Keys.Down or Keys.Left or Keys.Right or Keys.Home or Keys.End or Keys.Tab or (Keys.Tab | Keys.Shift))
        {
            int i = Array.FindIndex(targets, t => ReferenceEquals(t.Element, _focused) && t.DropDownPart == _focusedDropDown);
            int next = keys == Keys.Home ? 0 : keys == Keys.End ? targets.Length - 1 : (i + (keys is Keys.Up or Keys.Left or (Keys.Tab | Keys.Shift) ? -1 : 1) + targets.Length) % Math.Max(1, targets.Length);
            if (targets.Length > 0)
                FocusElement(targets[next].Element!, targets[next].DropDownPart);
            return true;
        }
        return false;
    }
}
