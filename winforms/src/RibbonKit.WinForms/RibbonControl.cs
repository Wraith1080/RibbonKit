using System.ComponentModel;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

/// <summary>A native dockable ribbon; all dimensions are measured by the library.</summary>
[DefaultProperty(nameof(Tabs)), DefaultEvent(nameof(SelectedTabChanged))]
[Designer("RibbonKit.WinForms.Design.RibbonControlDesigner, RibbonKit.WinForms.Design")]
public partial class RibbonControl : Control, ISupportInitialize
{
    private readonly RibbonLayoutEngine _engine = new();
    private readonly RibbonRenderer _defaultRenderer = new();
    private readonly RibbonInputFilter _input;
    private readonly RibbonPopupManager _popups;
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private RibbonLayoutSnapshot _snapshot = new();
    private RibbonTab? _selectedTab;
    private RibbonTheme _theme = RibbonThemes.Office2024Light;
    private RibbonRenderer? _renderer;
    private RibbonElement? _focused, _hot, _pressed;
    private Control? _priorFocus;
    private int _tabOffset, _bodyOffset, _updateDepth, _fallbackTabIndex = -1;
    private bool _layoutDirty = true, _disposing, _keyTips, _topTips;
    private string _keyPrefix = "";
    /// <summary>Initializes a RibbonControl instance.</summary>
    public RibbonControl()
    {
        Tabs = new(this);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable | ControlStyles.ResizeRedraw, true);
        Dock = DockStyle.Top;
        TabStop = true;
        AccessibleName = "Ribbon";
        _popups = new(this);
        _input = new(this);
        UpdateHeight();
    }
    /// <summary>Gets the stable, owned tab collection.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonTabCollection Tabs
    {
        get;
    }
    /// <summary>Gets or selects a visible, enabled tab belonging to this ribbon.</summary>
    [Bindable(true), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonTab? SelectedTab
    {
        get => _selectedTab;
        set
        {
            VerifyAccess();
            if (value is not null && (!Tabs.Contains(value) || !value.Visible || !value.EffectiveEnabled))
                throw new ArgumentException("Select a visible, enabled tab belonging to this ribbon.", nameof(value));
            if (ReferenceEquals(_selectedTab, value))
                return;
            _popups.CloseAll(false);
            _selectedTab = value;
            _bodyOffset = 0;
            _layoutDirty = true;
            if (_focused is not RibbonTab)
                _focused = value;
            Invalidate();
            SelectedTabChanged?.Invoke(this, EventArgs.Empty);
            AccessibilityChanged();
        }
    }
    /// <summary>Occurs after the selected tab changes.</summary>
    public event EventHandler? SelectedTabChanged;
    /// <summary>Gets or sets the immutable palette and logical metrics.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonTheme Theme
    {
        get => _theme; set
        {
            VerifyAccess();
            ArgumentNullException.ThrowIfNull(value);
            if (value.TabHeight <= 0 || value.BodyHeight <= 0 || value.CornerRadius < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (Equals(_theme, value))
                return;
            _theme = value;
            ModelChanged();
        }
    }
    /// <summary>Gets or sets a borrowed renderer; null selects the library renderer.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonRenderer? Renderer
    {
        get => _renderer; set
        {
            VerifyAccess();
            _renderer = value;
            Invalidate();
            _popups.InvalidateAll();
        }
    }
    internal RibbonTheme LayoutTheme
    {
        get
        {
            int text = (int)Math.Ceiling(Font.Height * 96d / DeviceDpi);
            return _theme with
            {
                TabHeight = Math.Max(_theme.TabHeight, text + 12),
                BodyHeight = Math.Max(_theme.BodyHeight, Math.Max(26, text + 8) * 3 + Math.Max(20, text + 6) + 8)
            };
        }
    }
    internal RibbonRenderer Painter => Renderer ?? _defaultRenderer;
    internal RibbonLayoutEngine Engine => _engine;
    internal RibbonLayoutSnapshot Snapshot
    {
        get
        {
            EnsureLayout();
            return _snapshot;
        }
    }
    internal RibbonPopupManager Popups => _popups;
    internal bool KeyTipsActive => _keyTips;
    internal IEnumerable<RibbonElement> AllElements => Tabs.SelectMany(t => t.Tree());
    internal void VerifyAccess()
    {
        if (Environment.CurrentManagedThreadId != _threadId)
            throw new InvalidOperationException("Ribbon components must be changed on their owning UI thread.");
    }
    internal void ValidateIdentityChange(RibbonElement element, string id)
    {
        VerifyAccess();
        if (AllElements.Any(n => !ReferenceEquals(n, element) && n.Id == id))
            throw new ArgumentException("Duplicate ribbon identity: " + id);
    }
    internal void ValidateAddition(RibbonElement element, RibbonElement? replacing)
    {
        var excluded = replacing?.Tree().ToHashSet() ?? [];
        var ids = AllElements.Where(n => !excluded.Contains(n)).Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var node in element.Tree())
            if (!ids.Add(node.Id))
                throw new ArgumentException("Duplicate ribbon identity: " + node.Id);
    }
    internal static void DetachElement(RibbonElement element)
    {
        switch (element.Parent)
        {
            case RibbonControl r when element is RibbonTab tab:
                r.Tabs.Remove(tab);
                break;
            case RibbonTab t when element is RibbonGroup group:
                t.Groups.Remove(group);
                break;
            case RibbonGroup g when element is RibbonItem item:
                g.Items.Remove(item);
                break;
            case RibbonDropDownItem d when element is RibbonItem item:
                d.Items.Remove(item);
                break;
        }
    }
    internal void RemovingTab(RibbonTab tab, int index)
    {
        if (ReferenceEquals(tab, _selectedTab))
            _fallbackTabIndex = index;
    }
    internal void ModelChanged(bool layout = true)
    {
        if (_disposing)
            return;
        VerifyAccess();
        if (layout)
        {
            _engine.Clear();
            _layoutDirty = true;
            _popups.CloseAll(false);
        }
        if (_selectedTab is null || !Tabs.Contains(_selectedTab) || !_selectedTab.Visible || !_selectedTab.EffectiveEnabled)
        {
            int index = _fallbackTabIndex >= 0 ? _fallbackTabIndex : Math.Max(0, _selectedTab is null ? 0 : Tabs.IndexOf(_selectedTab));
            var next = Tabs.Skip(index).FirstOrDefault(t => t.Visible && t.EffectiveEnabled) ?? Tabs.Take(index).LastOrDefault(t => t.Visible && t.EffectiveEnabled);
            _fallbackTabIndex = -1;
            if (!ReferenceEquals(next, _selectedTab))
            {
                _selectedTab = next;
                _layoutDirty = true;
                SelectedTabChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        if (_focused is { } focused && (!AllElements.Contains(focused) || !focused.Visible || !focused.EffectiveEnabled))
            _focused = null;
        if (_updateDepth == 0)
        {
            UpdateHeight();
            Invalidate();
            _popups.InvalidateAll();
            AccessibilityChanged();
        }
    }
    /// <summary>Defers measurement while applying a batch of component changes.</summary>
    public void BeginUpdate()
    {
        VerifyAccess();
        _updateDepth++;
    }
    /// <summary>Ends a matching update batch and refreshes presentation.</summary>
    public void EndUpdate()
    {
        VerifyAccess();
        if (_updateDepth == 0)
            throw new InvalidOperationException("EndUpdate requires a matching BeginUpdate.");
        if (--_updateDepth == 0)
            ModelChanged();
    }
    /// <summary>Provides BeginInit for native ribbon presentation.</summary>
    public void BeginInit() => BeginUpdate();
    /// <summary>Provides EndInit for native ribbon presentation.</summary>
    public void EndInit() => EndUpdate();
    private void UpdateHeight()
    {
        if (_theme is null || _disposing)
            return;
        Height = RibbonLayoutEngine.Scale(LayoutTheme.TabHeight + LayoutTheme.BodyHeight + 2, DeviceDpi);
    }
    private void EnsureLayout()
    {
        if (!_layoutDirty || _updateDepth > 0)
            return;
        _snapshot = _engine.Build(this, _tabOffset, _bodyOffset);
        _layoutDirty = false;
        _tabOffset = Math.Clamp(_tabOffset, 0, Math.Max(0, _snapshot.TabExtent - _snapshot.TabViewport.Width));
        _bodyOffset = Math.Clamp(_bodyOffset, 0, Math.Max(0, _snapshot.BodyExtent - _snapshot.BodyViewport.Width));
    }
    /// <inheritdoc />
    protected override void OnResize(EventArgs e)
    {
        _layoutDirty = true;
        _popups?.CloseAll(false);
        base.OnResize(e);
    }
    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (_engine is not null)
            ModelChanged();
    }
    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ModelChanged();
    }
    /// <inheritdoc />
    protected override void OnBindingContextChanged(EventArgs e)
    {
        base.OnBindingContextChanged(e);
        if (Tabs is not null)
            foreach (var tab in Tabs)
                tab.BindingContext = BindingContext;
    }
    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (_popups is not null)
        {
            if (!Enabled)
                _popups.CloseAll(false);
            ModelChanged(false);
        }
    }
    /// <inheritdoc />
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (_popups is null)
            return;
        if (!Visible)
        {
            _popups.CloseAll(false);
            ExitKeyTips(false);
        }
        AccessibilityChanged();
    }
    /// <inheritdoc />
    protected override void OnParentChanged(EventArgs e)
    {
        _popups?.ResetOwner();
        base.OnParentChanged(e);
    }
    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _input.Attach();
        ModelChanged();
    }
    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        _input.Detach();
        _popups.CloseAll(false);
        DisposeAutomation();
        base.OnHandleDestroyed(e);
    }
    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        EnsureLayout();
        var context = new RibbonPaintContext(e.Graphics, ClientRectangle, Theme, Font, DeviceDpi);
        Painter.DrawSurface(context, false);
        var state = e.Graphics.Save();
        try
        {
            e.Graphics.SetClip(_snapshot.BodyViewport);
            using var brush = new SolidBrush(Theme.Surface);
            e.Graphics.FillRectangle(brush, _snapshot.BodyViewport);
            foreach (var slot in _snapshot.Groups)
                Painter.DrawGroup(context with
                {
                    Bounds = slot.Bounds,
                    Hot = ReferenceEquals(slot.Group, _hot),
                    Pressed = ReferenceEquals(slot.Group, _pressed),
                    Focused = Focused && ReferenceEquals(slot.Group, _focused)
                }, slot.Group, slot.Collapsed);
        }
        finally { e.Graphics.Restore(state); }
        foreach (var target in _snapshot.Targets)
        {
            if (target.VisibleBounds.IsEmpty)
                continue;
            state = e.Graphics.Save();
            try
            {
                e.Graphics.SetClip(target.Clip);
                var ctx = context with
                {
                    Bounds = target.Bounds,
                    Hot = ReferenceEquals(target.Element, _hot),
                    Pressed = ReferenceEquals(target.Element, _pressed),
                    Focused = Focused && target.Element is not null && ReferenceEquals(target.Element, _focused),
                    ReserveIcon = target.ReserveIcon
                };
                if (target.Element is RibbonTab tab)
                    Painter.DrawTab(ctx, tab, ReferenceEquals(tab, SelectedTab));
                else if (target.Element is RibbonItem item)
                    Painter.DrawItem(ctx, item, target.Size, false);
                else if (target.Element is null)
                {
                    using var b = new SolidBrush(Theme.Background);
                    e.Graphics.FillRectangle(b, target.Bounds);
                    TextRenderer.DrawText(e.Graphics, target.Kind is TargetKind.TabBack or TargetKind.BodyBack ? "‹" : "›", Font, target.Bounds, Theme.Foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
            finally { e.Graphics.Restore(state); }
        }
        if (_keyTips && !_popups.HasOpen)
            DrawTips(e.Graphics, TipTargets());
    }
    internal void DrawTips(Graphics graphics, IReadOnlyList<RibbonTarget> targets)
    {
        var keys = KeyTipResolver.Resolve(targets.Select(t => (t.Element?.Text ?? "", t.Element?.KeyTip ?? "")));
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].VisibleBounds.IsEmpty || !keys[i].StartsWith(_keyPrefix, StringComparison.Ordinal))
                continue;
            var rect = targets[i].VisibleBounds;
            var size = TextRenderer.MeasureText(keys[i], Font);
            var badge = new Rectangle(rect.Left + Math.Max(0, (rect.Width - size.Width) / 2), Math.Max(rect.Top, rect.Bottom - size.Height), size.Width, size.Height);
            Painter.DrawKeyTip(new(graphics, badge, Theme, Font, DeviceDpi), keys[i]);
        }
    }
    private IReadOnlyList<RibbonTarget> TipTargets() => _popups.Top?.Targets.Where(t => t.Element is not null && t.Enabled).ToArray() ?? Snapshot.Targets.Where(t => t.Element is not null && (_topTips ? t.Kind == TargetKind.Tab : t.Kind != TargetKind.Tab)).ToArray();
    private RibbonTarget? Hit(Point point) => Snapshot.Targets.LastOrDefault(t => t.VisibleBounds.Contains(point));
    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var target = Hit(e.Location)?.Element;
        if (!ReferenceEquals(target, _hot))
        {
            _hot = target;
            Invalidate();
        }
    }
    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hot = null;
        Invalidate();
    }
    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;
        _keyTips = false;
        var hit = Hit(e.Location);
        _pressed = hit?.Element;
        Capture = true;
        if (hit?.Element is { } element)
            FocusElement(element);
        Invalidate();
    }
    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left)
            return;
        var hit = Hit(e.Location);
        var pressed = _pressed;
        _pressed = null;
        Capture = false;
        if (hit is not null && ReferenceEquals(pressed, hit.Element))
            Activate(hit);
        Invalidate();
    }
    /// <inheritdoc />
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        _pressed = null;
        Invalidate();
    }
    /// <inheritdoc />
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        Scroll(false, e.Delta < 0 ? 120 : -120);
    }
    /// <inheritdoc />
    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End || base.IsInputKey(keyData);
    /// <inheritdoc />
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) => ProcessRibbonKey(keyData) || base.ProcessCmdKey(ref msg, keyData);
    internal bool ProcessRibbonKey(Keys keys)
    {
        if (keys == Keys.Escape)
        {
            if (_popups.HasOpen)
                _popups.CloseTop(true);
            else if (_keyTips && !_topTips)
            {
                _topTips = true;
                _keyPrefix = "";
                Invalidate();
            }
            else
                ExitKeyTips(true);
            return true;
        }
        if (_keyTips && keys is >= Keys.A and <= Keys.Z || _keyTips && keys is >= Keys.D0 and <= Keys.D9)
        {
            var character = keys is >= Keys.A and <= Keys.Z ? (char)('A' + keys - Keys.A) : (char)('0' + keys - Keys.D0);
            var prefix = _keyPrefix + character;
            var candidates = TipTargets();
            var tips = KeyTipResolver.Resolve(candidates.Select(t => (t.Element?.Text ?? "", t.Element?.KeyTip ?? "")));
            int match = Array.FindIndex(tips, t => t == prefix);
            if (match >= 0)
            {
                _keyPrefix = "";
                var target = candidates[match];
                if (target.Enabled)
                {
                    if (target.Element is RibbonTab)
                        _topTips = false;
                    else if (target.Element is not RibbonDropDownItem && target.Element is not RibbonGroup)
                        _keyTips = false;
                    Activate(target);
                }
            }
            else if (tips.Any(t => t.StartsWith(prefix, StringComparison.Ordinal)))
                _keyPrefix = prefix;
            Invalidate();
            _popups.InvalidateAll();
            return true;
        }
        if (!Focused)
            return false;
        var targets = Snapshot.Targets.Where(t => t.Element is not null && t.Enabled).ToArray();
        int current = Array.FindIndex(targets, t => ReferenceEquals(t.Element, _focused));
        if (keys is Keys.Enter or Keys.Space)
        {
            if (current >= 0)
                Activate(targets[current]);
            return true;
        }
        if (keys is Keys.Tab or (Keys.Tab | Keys.Shift))
        {
            int next = current + (keys.HasFlag(Keys.Shift) ? -1 : 1);
            if (next >= 0 && next < targets.Length)
                FocusElement(targets[next].Element!);
            else
            {
                ExitKeyTips(false);
                FindForm()?.SelectNextControl(this, !keys.HasFlag(Keys.Shift), true, true, true);
            }
            return true;
        }
        if (keys is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End)
        {
            if (current >= 0 && targets[current].Kind == TargetKind.Tab && keys == Keys.Down)
            {
                if (targets.FirstOrDefault(t => t.Kind is TargetKind.Item or TargetKind.Group) is { Element: { } first })
                    FocusElement(first);
                return true;
            }
            if (current >= 0 && targets[current].Element is RibbonDropDownItem dropDown && keys == Keys.Down)
            {
                dropDown.ShowDropDown();
                return true;
            }
            var scope = targets.Where(t => current < 0 || (t.Kind == TargetKind.Tab) == (targets[current].Kind == TargetKind.Tab)).ToArray();
            int index = Array.FindIndex(scope, t => ReferenceEquals(t.Element, _focused));
            int next = keys == Keys.Home ? 0 : keys == Keys.End ? scope.Length - 1 : (index + (keys is Keys.Left or Keys.Up ? -1 : 1) + scope.Length) % Math.Max(1, scope.Length);
            if (scope.Length > 0)
            {
                FocusElement(scope[next].Element!);
                if (scope[next].Element is RibbonTab tab)
                    SelectedTab = tab;
            }
            return true;
        }
        return false;
    }
    internal void Activate(RibbonTarget target)
    {
        if (!target.Enabled)
            return;
        switch (target.Element)
        {
            case RibbonTab tab:
                SelectedTab = tab;
                FocusElement(tab);
                break;
            case RibbonGroup group:
                _popups.Toggle(group, this, target.VisibleBounds);
                break;
            case RibbonDropDownItem drop:
                OpenDropDown(drop);
                break;
            case RibbonItem item:
                item.PerformClick();
                _popups.CloseAll(true);
                break;
            default:
                Scroll(target.Kind is TargetKind.TabBack or TargetKind.TabNext, target.Kind is TargetKind.TabBack or TargetKind.BodyBack ? -120 : 120);
                break;
        }
    }
    internal void Scroll(bool tabs, int amount)
    {
        EnsureLayout();
        if (tabs)
            _tabOffset = Math.Clamp(_tabOffset + amount, 0, Math.Max(0, _snapshot.TabExtent - _snapshot.TabViewport.Width));
        else
            _bodyOffset = Math.Clamp(_bodyOffset + amount, 0, Math.Max(0, _snapshot.BodyExtent - _snapshot.BodyViewport.Width));
        _layoutDirty = true;
        Invalidate();
        AccessibilityChanged();
    }
    internal void FocusElement(RibbonElement element)
    {
        _focused = element;
        Focus();
        var target = Snapshot.Targets.FirstOrDefault(t => ReferenceEquals(t.Element, element));
        if (target is not null)
        {
            int delta = target.Bounds.Left < target.Clip.Left ? target.Bounds.Left - target.Clip.Left : target.Bounds.Right > target.Clip.Right ? target.Bounds.Right - target.Clip.Right : 0;
            if (delta != 0)
                Scroll(target.Kind == TargetKind.Tab, delta);
        }
        Invalidate();
        AccessibilityFocusChanged(element);
    }
    internal RibbonElement? FocusedElement => _popups.Top?.FocusedElement ?? _focused;
    /// <inheritdoc />
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _focused ??= Snapshot.Targets.FirstOrDefault(t => t.Enabled && t.Element is not null)?.Element;
        Invalidate();
    }
    /// <inheritdoc />
    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }
    internal void ToggleKeyTips()
    {
        if (_keyTips)
        {
            ExitKeyTips(true);
            return;
        }
        _priorFocus = FindForm()?.ActiveControl;
        _keyTips = true;
        _topTips = !_popups.HasOpen;
        _keyPrefix = "";
        Focus();
        Invalidate();
        _popups.InvalidateAll();
    }
    internal void ExitKeyTips(bool restore)
    {
        _keyTips = false;
        _keyPrefix = "";
        Invalidate();
        _popups.InvalidateAll();
        if (restore && _priorFocus is { IsDisposed: false, CanFocus: true } prior && !ReferenceEquals(prior, this))
            prior.Focus();
    }
    internal void OpenDropDown(RibbonDropDownItem item)
    {
        if (IsInDesignMode)
            return;
        foreach (var surface in _popups.Surfaces.Reverse())
            if (surface.Targets.FirstOrDefault(t => ReferenceEquals(t.Element, item)) is { } row)
            {
                _popups.Toggle(item, surface, row.VisibleBounds);
                return;
            }
        if (Snapshot.Targets.FirstOrDefault(t => ReferenceEquals(t.Element, item)) is { } target && !target.VisibleBounds.IsEmpty)
            _popups.Toggle(item, this, target.VisibleBounds);
    }
    internal void CloseDropDown(RibbonDropDownItem item) => _popups.Close(item, true);
    internal bool IsInDesignMode => LicenseManager.UsageMode == LicenseUsageMode.Designtime || DesignMode;
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposing)
        {
            _disposing = true;
            _input.Dispose();
            _popups.Dispose();
            foreach (var tab in Tabs.ToArray())
                tab.Dispose();
            Tabs.Clear();
            _defaultRenderer.Dispose();
            DisposeAutomation();
        }
        base.Dispose(disposing);
    }
}
