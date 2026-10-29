using System.ComponentModel;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

/// <summary>A native dockable ribbon; all dimensions are measured by the library.</summary>
[DefaultProperty(nameof(Tabs)), DefaultEvent(nameof(SelectedTabChanged))]
[Designer("RibbonKit.WinForms.Design.RibbonControlDesigner, RibbonKit.WinForms.Design")]
public partial class RibbonControl : Control, ISupportInitialize
{
    internal bool PopupOnly;
    private readonly RibbonLayoutEngine _engine = new();
    private readonly RibbonRenderer _defaultRenderer = new RibbonOffice2024Renderer();
    private readonly RibbonInputFilter _input;
    private readonly RibbonPopupManager _popups;
    private readonly RibbonEditorManager _editors;
    private readonly RibbonGalleryStripManager _galleryStrips;
    private readonly RibbonScreenTipManager _screenTips;
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private RibbonLayoutSnapshot _snapshot = new();
    private RibbonTab? _selectedTab;
    private RibbonTheme _theme = RibbonThemes.Office2024Light;
    private RibbonRenderer? _renderer;
    private RibbonElement? _focused, _hot, _pressed;
    private bool _focusedDropDown, _hotDropDown, _pressedDropDown;
    private TargetKind? _hotScroll, _pressedScroll;
    private Control? _priorFocus;
    private int _tabOffset, _bodyOffset, _updateDepth, _fallbackTabIndex = -1;
    private bool _layoutDirty = true, _disposing, _keyTips, _topTips, _keyboardFocusCues;
    private string _keyPrefix = "";
    /// <summary>Initializes a RibbonControl instance.</summary>
    public RibbonControl()
    {
        Tabs = new(this);
        QuickAccessItems = new(this);
        QuickAccessOverflow = new(this);
        FileMenu = new() { Text = "File", KeyTip = "F", Visible = false };
        FileMenu.Parent = this;
        Backstage = new() { Text = "Backstage" };
        Backstage.Parent = this;
        MessageBar = new() { Text = "Messages", Parent = this };
        CommandCatalog = new(this) { Text = "Application commands", Parent = this };
        _simplified = new(this);
        _modalClose = new() { Text = "Close", KeyTip = "X" };
        _modalClose.Parent = this;
        _modalClose.Click += (_, _) => ExitModalByUser(RibbonModalReason.CloseButton);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable | ControlStyles.ResizeRedraw, true);
        Dock = DockStyle.Top;
        TabStop = true;
        AccessibleName = "Ribbon";
        _popups = new(this);
        _editors = new(this);
        _galleryStrips = new(this);
        _screenTips = new(this);
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
            if (value is not null && (!GetPresentedTabs().Contains(value) || !value.Visible || !value.EffectiveEnabled))
                throw new ArgumentException("Select a visible, enabled tab belonging to this ribbon.", nameof(value));
            if (ReferenceEquals(_selectedTab, value))
                return;
            if (AnimationLevel != RibbonAnimationLevel.None && _motion?.Changing != true && _updateDepth == 0 && (!IsMinimized || _motion?.CanCoalesceHeaderSelection == true) && value is not null && _selectedTab is not null)
            { Motion.Change(false, () => SelectedTab = value); return; }
            SettleMotion();
            _popups.CloseAll(false);
            _screenTips.Cancel();
            _selectedTab = value;
            _bodyOffset = 0;
            _layoutDirty = true;
            if (_focused is not RibbonTab)
            {
                _focused = value;
                _focusedDropDown = false;
            }
            SyncEditors();
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
            if (value.TabHeight <= 0 || value.BodyHeight <= 0 || value.BodyScrollButtonVerticalInset < 0 || value.HeaderButtonInset < 0 || value.LargeButtonMaxWidth < 60 || value.CollapsedGroupMaxWidth < 80 || value.CornerRadius < 0 || value.PopupCornerRadius < 0 ||
                !float.IsFinite(value.BackstageNavigationFontSize) || value.BackstageNavigationFontSize <= 0 || value.MessageMinimumHeight <= 0 ||
                value.MessageContentPadding.Left < 0 || value.MessageContentPadding.Top < 0 || value.MessageContentPadding.Right < 0 || value.MessageContentPadding.Bottom < 0 ||
                value.BodyMargin.Left < 0 || value.BodyMargin.Top < 0 || value.BodyMargin.Right < 0 || value.BodyMargin.Bottom < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (Equals(_theme, value))
                return;
            _theme = value;
            ModelChanged();
        }
    }
    /// <summary>Gets or sets a borrowed renderer; null selects RibbonOffice2024Renderer. Reassignment refreshes measurement after a renderer's layout policy changes.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonRenderer? Renderer
    {
        get => _renderer; set
        {
            VerifyAccess();
            _renderer = value;
            ModelChanged();
        }
    }
    internal RibbonTheme ClassicLayoutTheme
    {
        get
        {
            int text = (int)Math.Ceiling(Font.Height * 96d / DeviceDpi);
            var metrics = RibbonDensityMetrics.For(Density);
            int rows = Math.Max(metrics.CommandRow, text + 8) * 3 + Math.Max(20, text + 6) + RibbonLayoutEngine.CommandVerticalInset;
            return _theme with
            {
                TabHeight = Math.Max(Math.Max(_theme.TabHeight, metrics.Touch ? metrics.TabHeight : 0), text + 12),
                BodyHeight = Math.Max(_theme.BodyHeight, rows)
            };
        }
    }
    internal RibbonTheme LayoutTheme => LayoutMode == RibbonLayoutMode.Classic ? ClassicLayoutTheme : ClassicLayoutTheme with
    {
        BodyHeight = Math.Max(RibbonDensityMetrics.For(Density).SimplifiedBody, (int)Math.Ceiling(Font.Height * 96d / DeviceDpi) + (Density == RibbonDensity.Touch ? 20 : 14))
    };
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
    internal RibbonEditorManager Editors => _editors;
    internal RibbonGalleryStripManager GalleryStrips => _galleryStrips;
    internal RibbonScreenTipManager ScreenTips => _screenTips;
    internal bool KeyTipsActive => _keyTips;
    internal IEnumerable<RibbonElement> AllElements => Tabs.SelectMany(t => t.Tree()).Concat(_merges?.ActiveElements ?? []).Concat(QuickAccessItems.SelectMany(e => e.Tree())).Concat(FileMenu.Tree()).Concat(Backstage.Tree()).Concat(MessageBar.Tree()).Concat(CommandCatalog.Tree()).Concat(IsModal ? _modalClose.Tree() : []).Concat(_simplified?.Elements ?? []);
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
        {
            if (node is RibbonQuickAccessReference reference) RibbonQuickAccess.ValidateSource(reference.Source, this);
            if (node is RibbonQuickAccessComboBoxReference comboReference) RibbonQuickAccess.ValidateComboSource(comboReference.Source, this);
            if (node is RibbonQuickAccessDropDownReference menuReference) RibbonQuickAccess.ValidateMenuSource(menuReference.Source, this);
            if (node is RibbonQuickAccessGroupReference groupReference) RibbonQuickAccess.ValidateGroupSource(groupReference.Source, this);
            if (!ids.Add(node.Id))
                throw new ArgumentException("Duplicate ribbon identity: " + node.Id);
        }
    }
    internal static void DetachElement(RibbonElement element)
    {
        switch (element.Parent)
        {
            case RibbonMergeSource source when element is RibbonTab sourceTab:
                source.Tabs.Remove(sourceTab);
                break;
            case RibbonMergeSource source when element is RibbonGroupContribution contribution:
                source.Groups.Remove(contribution);
                break;
            case RibbonControl r when element is RibbonTab tab:
                r.Tabs.Remove(tab);
                break;
            case RibbonControl r when ReferenceEquals(element, r.FileMenu):
                r.ModelChanged();
                break;
            case RibbonControl r when ReferenceEquals(element, r.Backstage):
                r.ModelChanged();
                break;
            case RibbonBackstage backstage when element is RibbonItem entry:
                backstage.Items.Remove(entry);
                break;
            case RibbonMessageBar bar when element is RibbonMessage message:
                bar.Items.Remove(message);
                break;
            case RibbonControl r when ReferenceEquals(element, r.MessageBar):
                r.ModelChanged();
                break;
            case RibbonCommandCatalog catalog when element is RibbonItem command:
                catalog.Commands.Remove(command);
                break;
            case RibbonControl r when ReferenceEquals(element, r.CommandCatalog):
                r.ModelChanged();
                break;
            case RibbonControl r:
                r.QuickAccessItems.Remove(element);
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
            _fallbackTabIndex = GetPresentedTabs().ToList().IndexOf(tab) is var presented && presented >= 0 ? presented : index;
    }
    internal void ModelChanged(bool layout = true, bool preservePopups = false, RibbonElement? origin = null)
    {
        SettleMotion();
        if (_disposing)
            return;
        VerifyAccess();
        if (CommandCatalog?.Structure is { } structure)
        {
            if (structure.Synchronizing) return;
            if (structure.Applying) { _engine.Clear(); _layoutDirty = true; return; }
            structure.Synchronize(origin);
        }
        _screenTips?.Cancel();
        MessageBar.Reconcile();
        _modalScope?.Reconcile();
        foreach (var entry in QuickAccessItems) entry.MergeProvenance = GetMergeSource(entry);
        foreach (var gallery in AllElements.OfType<RibbonGalleryItem>().ToArray()) gallery.Reconcile();
        RefreshBackstage();
        if (layout)
        {
            _engine.Clear();
            _layoutDirty = true;
            if (!preservePopups) _popups.CloseAll(false);
        }
        var presentedTabs = GetPresentedTabs();
        if (_selectedTab is null || !presentedTabs.Contains(_selectedTab) || !_selectedTab.Visible || !_selectedTab.EffectiveEnabled)
        {
            int index = _fallbackTabIndex >= 0 ? _fallbackTabIndex : Math.Max(0, _selectedTab is null ? 0 : presentedTabs.ToList().IndexOf(_selectedTab));
            var next = presentedTabs.Skip(index).FirstOrDefault(t => t.Visible && t.EffectiveEnabled) ?? presentedTabs.Take(index).LastOrDefault(t => t.Visible && t.EffectiveEnabled);
            _fallbackTabIndex = -1;
            if (!ReferenceEquals(next, _selectedTab))
            {
                _popups.CloseAll(false);
                _selectedTab = next;
                _layoutDirty = true;
                SelectedTabChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        if (_focused is { } focused && (!AllElements.Contains(focused) || !focused.Visible || !focused.EffectiveEnabled))
        {
            _focused = null;
            _focusedDropDown = false;
        }
        if (_updateDepth == 0)
        {
            UpdateHeight();
            SyncEditors();
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
        var layout = LayoutTheme;
        Height = RibbonLayoutEngine.Scale(layout.TabHeight, DeviceDpi) + (IsMinimized ? 0 : RibbonLayoutEngine.Scale(layout.BodyHeight, DeviceDpi)
            + RibbonLayoutEngine.Scale(_theme.BodyMargin.Top, DeviceDpi) + RibbonLayoutEngine.Scale(_theme.BodyMargin.Bottom, DeviceDpi)) + QuickAccessHeight
            + (IsMinimized && (JoinBelowRibbonQuickAccess || MessageBar.HasOpenMessages && JoinMessageBar) ? RibbonLayoutEngine.Scale(_theme.BodyMargin.Top, DeviceDpi) + RibbonLayoutEngine.Scale(_theme.BodyMargin.Bottom, DeviceDpi) : 0)
            + MessageHeight;
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
    internal void SyncEditors()
    {
        if (_editors is null || _disposing || _updateDepth > 0) return;
        EnsureLayout();
        _editors.Sync();
        _galleryStrips.Sync();
    }
    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs e)
    {
        _screenTips?.Cancel();
        base.OnLayout(e);
        SyncEditors();
    }
    /// <inheritdoc />
    protected override void OnResize(EventArgs e)
    {
        SettleMotion();
        _layoutDirty = true;
        if (MessageBar is { HasOpenMessages: true }) UpdateHeight();
        _screenTips?.Cancel();
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
            foreach (var element in Tabs.Cast<RibbonElement>().Concat(QuickAccessItems).Append(FileMenu).Append(Backstage).Append(MessageBar).Append(CommandCatalog))
                element.BindingContext = BindingContext;
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
        if (!Visible) _motion?.Settle();
        base.OnVisibleChanged(e);
        if (_popups is null)
            return;
        RefreshBackstage();
        if (!Visible)
        {
            _screenTips.Cancel();
            _popups.CloseAll(false);
            _editors.Release(this);
            _galleryStrips.Release(this);
            ExitKeyTips(false);
        }
        else SyncEditors();
        AccessibilityChanged();
    }
    /// <inheritdoc />
    protected override void OnParentChanged(EventArgs e)
    {
        _motion?.Settle();
        ReleaseBackstageSurface();
        _popups?.ResetOwner();
        _screenTips?.ResetOwner();
        base.OnParentChanged(e);
        if (IsHandleCreated) _screenTips?.Attach();
    }
    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _input.Attach();
        _screenTips.Attach();
        ModelChanged();
    }
    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        _motion?.Settle();
        ReleaseBackstageSurface();
        _input.Detach();
        _screenTips.ResetOwner();
        _popups.CloseAll(false);
        DisposeAutomation();
        base.OnHandleDestroyed(e);
    }
    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        EnsureLayout();
        var context = new RibbonPaintContext(e.Graphics, ClientRectangle, Theme, Font, DeviceDpi) { Density = Density, SuppressTabIndicators = CapturingMotionIndicators };
        Painter.DrawSurface(context, false);
        PaintMessages(context);
        if (!_snapshot.BodyBounds.IsEmpty) Painter.DrawBody(context with { Bounds = _snapshot.BodyFrameBounds, IsBottomEdgeConnected = MessageBar.HasOpenMessages && MessageTopConnected });
        if (!_snapshot.QuickAccessBounds.IsEmpty) Painter.DrawQuickAccessToolbar(context with { Bounds = _snapshot.QuickAccessBounds,
            IsQuickAccessToolbarJoined = JoinBelowRibbonQuickAccess && !_snapshot.BodyBounds.IsEmpty,
            IsBottomEdgeConnected = MessageBar.HasOpenMessages && MessageTopConnected }, QuickAccessPosition);
        PaintMessageContents(context);
        var state = e.Graphics.Save();
        try
        {
            e.Graphics.SetClip(_snapshot.BodyViewport);
            foreach (var slot in _snapshot.Groups)
                Painter.DrawGroup(context with
                {
                    Bounds = slot.Bounds,
                    GroupSeparatorBounds = slot.Frame,
                    Hot = slot.Group.EffectiveEnabled && ReferenceEquals(slot.Group, _hot),
                    Pressed = slot.Group.EffectiveEnabled && ReferenceEquals(slot.Group, _pressed),
                    Focused = slot.Group.EffectiveEnabled && _keyboardFocusCues && Focused && ReferenceEquals(slot.Group, _focused)
                }, slot.Group, slot.Collapsed);
            foreach (var divider in _snapshot.SimplifiedDividers) { using var brush = new SolidBrush(Theme.Border); e.Graphics.FillRectangle(brush, divider); }
        }
        finally { e.Graphics.Restore(state); }
        foreach (var target in _snapshot.Targets)
        {
            if (target.DropDownPart || target.Element is IRibbonEditorItem)
                continue;
            var arrow = RibbonSplitLayout.IsSplit(target.Element) ? _snapshot.Targets.First(t => ReferenceEquals(t.Element, target.Element) && t.DropDownPart) : null;
            if (target.VisibleBounds.IsEmpty && (arrow is null || arrow.VisibleBounds.IsEmpty))
                continue;
            state = e.Graphics.Save();
            try
            {
                e.Graphics.SetClip(target.Clip);
                var ctx = context with
                {
                    Bounds = target.Bounds,
                    Hot = target.Enabled && !_hotDropDown && ReferenceEquals(target.Element, _hot),
                    Pressed = target.Enabled && !_pressedDropDown && ReferenceEquals(target.Element, _pressed),
                    Focused = target.Enabled && !_focusedDropDown && _keyboardFocusCues && Focused && target.Element is not null && ReferenceEquals(target.Element, _focused),
                    ReserveIcon = target.ReserveIcon,
                    IsGalleryDropDown = target.GalleryDropDown
                };
                if (target.Kind == TargetKind.QuickAccess && target.Element is RibbonQuickAccessDropDownReference splitReference && arrow is not null)
                    Painter.DrawQuickAccessSplitItem(ctx, ctx with { Bounds = arrow.Bounds,
                        Hot = target.Enabled && _hotDropDown && ReferenceEquals(target.Element, _hot),
                        Pressed = target.Enabled && _pressedDropDown && ReferenceEquals(target.Element, _pressed),
                        Focused = target.Enabled && _focusedDropDown && _keyboardFocusCues && Focused && ReferenceEquals(target.Element, _focused) }, splitReference, false);
                else if (target.Kind == TargetKind.QuickAccess)
                    Painter.DrawQuickAccessItem(ctx, target.Element!, false);
                else if (target.Kind == TargetKind.QuickAccessOverflow)
                    Painter.DrawQuickAccessOverflow(ctx);
                else if (target.Kind == TargetKind.ModalClose)
                    Painter.DrawModalClose(ctx, _modalClose.Text);
                else if (target.Kind == TargetKind.FileMenu)
                    Painter.DrawFileButton(ctx, FileMenu);
                else if (target.Element is RibbonTab tab)
                    Painter.DrawTab(ctx, tab, ReferenceEquals(tab, SelectedTab));
                else if (target.Element is RibbonSplitButtonItem split && arrow is not null)
                    Painter.DrawSplitItem(ctx, ctx with
                    {
                        Bounds = arrow.Bounds,
                        Hot = target.Enabled && _hotDropDown && ReferenceEquals(target.Element, _hot),
                        Pressed = target.Enabled && _pressedDropDown && ReferenceEquals(target.Element, _pressed),
                        Focused = target.Enabled && _focusedDropDown && _keyboardFocusCues && Focused && ReferenceEquals(target.Element, _focused)
                    }, split, target.Size, false);
                else if (target.Kind == TargetKind.MessageAction && target.Element is RibbonButtonItem action)
                    Painter.DrawMessageAction(ctx, action);
                else if (target.Kind == TargetKind.MessageClose)
                    Painter.DrawMessageClose(ctx, target.Enabled);
                else if (target.Element is RibbonItem item)
                    Painter.DrawItem(ctx, item, target.Size, false);
                else if (target.Kind == TargetKind.Minimize)
                    Painter.DrawMinimizeButton(ctx with { Hot = Enabled && _hotScroll == target.Kind, Pressed = Enabled && _pressedScroll == target.Kind, Focused = false }, IsMinimized);
                else if (target.Element is null)
                    Painter.DrawScrollButton(ctx with
                    {
                        Hot = _hotScroll == target.Kind, Pressed = _pressedScroll == target.Kind, Focused = false,
                        IsBodyScrollButton = target.Kind is TargetKind.BodyBack or TargetKind.BodyNext
                    }, target.Kind is TargetKind.TabBack or TargetKind.BodyBack);
            }
            finally { e.Graphics.Restore(state); }
        }
        if (_keyTips && !_popups.HasOpen)
            DrawTips(e.Graphics, TipTargets());
    }
    internal void DrawTips(Graphics graphics, IReadOnlyList<RibbonTarget> targets)
    {
        var keys = KeyTipResolver.Resolve(targets.Select(t => (t.TipText, t.PreferredKeyTip)));
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].VisibleBounds.IsEmpty || !keys[i].StartsWith(_keyPrefix, StringComparison.Ordinal))
                continue;
            var rect = targets[i].VisibleBounds;
            var size = TextRenderer.MeasureText(keys[i], Font);
            var badge = new Rectangle(rect.Left + Math.Max(0, (rect.Width - size.Width) / 2), Math.Max(rect.Top, rect.Bottom - size.Height), size.Width, size.Height);
            Painter.DrawKeyTip(new(graphics, badge, Theme, Font, DeviceDpi) { Density = Density }, keys[i]);
        }
    }
    private IReadOnlyList<RibbonTarget> TipTargets() => _popups.Top?.Targets.Where(t => t.Element is not null && t.Enabled).ToArray() ?? Snapshot.Targets.Where(t => t.Element is not null && (_topTips ? t.Element is not RibbonSimplifiedMore && t.Kind is TargetKind.Tab or TargetKind.FileMenu or TargetKind.QuickAccess or TargetKind.QuickAccessOverflow or TargetKind.ModalClose or TargetKind.MessageAction or TargetKind.MessageClose : t.Element is RibbonSimplifiedMore || t.Kind is TargetKind.Item or TargetKind.Group or TargetKind.ModalClose)).ToArray();
    private RibbonTarget? Hit(Point point) => Snapshot.Targets.LastOrDefault(t => t.Enabled && t.VisibleBounds.Contains(point));
    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var target = Hit(e.Location);
        TargetKind? scroll = target?.Element is null ? target?.Kind : null;
        if (!ReferenceEquals(target?.Element, _hot) || (target?.DropDownPart ?? false) != _hotDropDown || scroll != _hotScroll)
        {
            _hot = target?.Element;
            _hotDropDown = target?.DropDownPart ?? false;
            _hotScroll = scroll;
            Invalidate();
        }
    }
    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hot = null;
        _hotScroll = null;
        Invalidate();
    }
    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs e)
    {
        SetKeyboardFocusCues(false);
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;
        _keyTips = false;
        var hit = Hit(e.Location);
        if (hit is null)
        {
            _pressed = null;
            Capture = false;
            return;
        }
        if (e.Clicks == 2 && hit.Element is RibbonTab)
        {
            _pressed = null; _pressedScroll = null; Capture = false;
            ToggleMinimizedByUser();
            return;
        }
        _pressed = hit.Element;
        _pressedScroll = hit.Element is null ? hit.Kind : null;
        _pressedDropDown = hit.DropDownPart;
        Capture = true;
        if (hit?.Element is { } element)
            FocusElement(element, hit.DropDownPart);
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
        var pressedScroll = _pressedScroll;
        _pressed = null;
        Capture = false;
        if (hit is not null && ReferenceEquals(pressed, hit.Element) && _pressedDropDown == hit.DropDownPart && (hit.Element is not null || pressedScroll == hit.Kind))
            Activate(hit);
        Invalidate();
    }
    /// <inheritdoc />
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        _pressed = null;
        _pressedScroll = null;
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
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) => ProcessCustomizationKey(keyData) || ProcessRibbonKey(keyData) || base.ProcessCmdKey(ref msg, keyData);
    internal bool ProcessRibbonKey(Keys keys)
    {
        _screenTips.Cancel();
        if (IsBackstageOpen) return ProcessBackstageKey(keys);
        if (keys == (Keys.Control | Keys.F1)) { ToggleMinimizedByUser(); return true; }
        if (keys is Keys.Escape or Keys.Enter or Keys.Space or Keys.Tab or (Keys.Tab | Keys.Shift)
            or Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End)
            SetKeyboardFocusCues(true);
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
            else if (IsModal && !_keyTips)
                ExitModalByUser(RibbonModalReason.Escape);
            else
                ExitKeyTips(true);
            return true;
        }
        var tipKey = keys & Keys.KeyCode;
        if (_keyTips && (keys & (Keys.Control | Keys.Alt)) == 0 && (tipKey is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9))
        {
            var character = tipKey is >= Keys.A and <= Keys.Z ? (char)('A' + tipKey - Keys.A) : (char)('0' + tipKey - Keys.D0);
            var prefix = _keyPrefix + character;
            var candidates = TipTargets();
            var tips = KeyTipResolver.Resolve(candidates.Select(t => (t.TipText, t.PreferredKeyTip)));
            int match = Array.FindIndex(tips, t => t == prefix);
            if (match >= 0)
            {
                _keyPrefix = "";
                var target = candidates[match];
                if (target.Enabled)
                {
                    if (target.Element is RibbonTab)
                        _topTips = false;
                    else if (target.Kind != TargetKind.QuickAccessOverflow && target.Element is not RibbonGroup and not RibbonQuickAccessGroupReference
                        && !(target.Element is RibbonQuickAccessDropDownReference menuReference && (menuReference.Source is not RibbonSplitButtonItem || target.DropDownPart))
                        && (target.Element is not RibbonDropDownItem || target.Element is RibbonSplitButtonItem && !target.DropDownPart))
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
        int current = Array.FindIndex(targets, t => ReferenceEquals(t.Element, _focused) && t.DropDownPart == _focusedDropDown);
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
                FocusElement(targets[next].Element!, targets[next].DropDownPart);
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
                if (IsMinimized) Activate(targets[current]);
                else if (targets.FirstOrDefault(t => t.Kind is TargetKind.Item or TargetKind.Group) is { Element: { } first })
                    FocusElement(first);
                return true;
            }
            if (current >= 0 && targets[current].Element is RibbonDropDownItem dropDown && keys == Keys.Down)
            {
                dropDown.ShowDropDown();
                return true;
            }
            if (current >= 0 && targets[current].Element is RibbonQuickAccessDropDownReference or RibbonQuickAccessGroupReference && keys == Keys.Down)
            {
                Activate(targets[current] with { DropDownPart = true });
                return true;
            }
            static int Scope(RibbonTarget t) => t.Element is RibbonSimplifiedMore ? 2 : t.Kind is TargetKind.Tab or TargetKind.FileMenu or TargetKind.ModalClose ? 0 : t.Kind is TargetKind.QuickAccess or TargetKind.QuickAccessOverflow ? 1 : t.Kind is TargetKind.MessageAction or TargetKind.MessageClose ? 3 : 2;
            var scope = targets.Where(t => current < 0 || Scope(t) == Scope(targets[current])).ToArray();
            int index = Array.FindIndex(scope, t => ReferenceEquals(t.Element, _focused) && t.DropDownPart == _focusedDropDown);
            int next = keys == Keys.Home ? 0 : keys == Keys.End ? scope.Length - 1 : (index + (keys is Keys.Left or Keys.Up ? -1 : 1) + scope.Length) % Math.Max(1, scope.Length);
            if (scope.Length > 0)
            {
                FocusElement(scope[next].Element!, scope[next].DropDownPart);
                if (scope[next].Element is RibbonTab tab)
                    SelectedTab = tab;
            }
            return true;
        }
        return false;
    }
    internal void Activate(RibbonTarget target, Control? opener = null)
    {
        SettleMotion();
        if (IsBackstageOpen) return;
        _screenTips.Cancel();
        if (!target.Enabled)
            return;
        if (target.Element is RibbonQuickAccessComboBoxReference openReference && _editors.IsQuickAccessOpen(openReference))
        { _editors.CloseQuickAccess(openReference); return; }
        if (target.Element is not IRibbonEditorItem && !_editors.CommitFocused()) return;
        if (target.Element is { } element && !ReferenceEquals(element.Ribbon, this)) return;
        if (target.Kind is TargetKind.MessageAction or TargetKind.MessageClose) { ActivateMessageTarget(target); return; }
        if (target.Kind == TargetKind.QuickAccess)
        {
            Control quickAccessOpener = opener ?? (Control?)_popups.Surfaces.LastOrDefault(s => s.Targets.Any(t => ReferenceEquals(t.Element, target.Element))) ?? this;
            ActivateQuickAccess(target.Element!, quickAccessOpener, target.VisibleBounds, target.DropDownPart);
            return;
        }
        if (target.Kind == TargetKind.QuickAccessOverflow) { _popups.Toggle(target.Element ?? QuickAccessOverflow, opener ?? this, target.VisibleBounds); return; }
        switch (target.Element)
        {
            case RibbonTab tab:
                FocusElement(tab);
                SelectedTab = tab;
                if (IsMinimized) _popups.Toggle(tab, this, target.VisibleBounds);
                break;
            case RibbonGroup group:
                Control groupOpener = opener ?? (Control?)_popups.Surfaces.LastOrDefault(s => s.Targets.Any(t => ReferenceEquals(t.Element, group))) ?? this;
                _popups.Toggle(group, groupOpener, target.VisibleBounds);
                break;
            case RibbonSplitButtonItem split when !target.DropDownPart:
                ActivateSplitPrimary(split, opener);
                break;
            case RibbonDropDownItem drop:
                OpenDropDown(drop);
                break;
            case RibbonItem editor when editor is IRibbonEditorItem:
                editor.PerformClick();
                break;
            case RibbonItem item:
                bool keepGroup = KeepQuickAccessGroupOpen(item, opener);
                item.PerformClick();
                if (!keepGroup) _popups.CloseAll(true);
                break;
            default:
                if (target.Kind == TargetKind.Minimize) IsMinimized = !IsMinimized;
                else Scroll(target.Kind is TargetKind.TabBack or TargetKind.TabNext, target.Kind is TargetKind.TabBack or TargetKind.BodyBack ? -120 : 120);
                break;
        }
    }
    internal void Scroll(bool tabs, int amount)
    {
        _screenTips.Cancel();
        EnsureLayout();
        if (tabs)
            _tabOffset = Math.Clamp(_tabOffset + amount, 0, Math.Max(0, _snapshot.TabExtent - _snapshot.TabViewport.Width));
        else
            _bodyOffset = Math.Clamp(_bodyOffset + amount, 0, Math.Max(0, _snapshot.BodyExtent - _snapshot.BodyViewport.Width));
        _layoutDirty = true;
        SyncEditors();
        Invalidate();
        AccessibilityChanged();
    }
    internal void ActivateSplitPrimary(RibbonSplitButtonItem split, Control? opener = null)
    {
        bool keepGroup = KeepQuickAccessGroupOpen(split, opener);
        split.PerformClick();
        if (!keepGroup) _popups.CloseAll(true);
    }
    internal bool KeyboardFocusCues => _keyboardFocusCues;
    internal void SetKeyboardFocusCues(bool visible)
    {
        if (_keyboardFocusCues == visible)
            return;
        // Logical/native focus remains available to keyboard and UI Automation.
        // Only the painted cue follows keyboard navigation versus pointer presses.
        _keyboardFocusCues = visible;
        Invalidate();
        _popups.InvalidateAll();
    }
    internal void FocusElement(RibbonElement element, bool dropDownPart = false, bool enterEditor = true)
    {
        SettleMotion();
        if (IsBackstageOpen) return;
        if (!ReferenceEquals(element, _focused) && !_editors.CommitFocused()) return;
        _focused = element;
        _focusedDropDown = RibbonSplitLayout.IsSplit(element) && dropDownPart;
        Focus();
        var target = Snapshot.Targets.FirstOrDefault(t => ReferenceEquals(t.Element, element) && t.DropDownPart == _focusedDropDown);
        if (target is not null)
        {
            int delta = target.Bounds.Left < target.Clip.Left ? target.Bounds.Left - target.Clip.Left : target.Bounds.Right > target.Clip.Right ? target.Bounds.Right - target.Clip.Right : 0;
            if (delta != 0)
                Scroll(target.Kind == TargetKind.Tab, delta);
        }
        Invalidate();
        if (enterEditor && element is RibbonItem editor && editor is IRibbonEditorItem) _editors.Focus(editor);
        if (enterEditor && element is RibbonGalleryItem { IsInRibbon: true } gallery) _galleryStrips.Focus(gallery);
        AccessibilityFocusChanged(element);
    }
    internal void EditorFocused(RibbonItem item)
    {
        _focused = item;
        _focusedDropDown = false;
        Invalidate();
    }
    internal void FocusEditor(RibbonItem item)
    {
        _screenTips.Cancel();
        foreach (var popup in _popups.Surfaces.Reverse())
            if (popup.Targets.Any(t => ReferenceEquals(t.Element, item)))
            {
                popup.FocusElement(item);
                return;
            }
        if (Snapshot.Targets.Any(t => ReferenceEquals(t.Element, item))) FocusElement(item);
    }
    internal RibbonElement? FocusedElement => _popups.Top?.FocusedElement ?? _focused;
    internal bool FocusedDropDownPart => _popups.Top?.FocusedDropDownPart ?? _focusedDropDown;
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
        if (IsBackstageOpen) { _backstageSurface?.ToggleKeyTips(); return; }
        _screenTips.Cancel();
        SetKeyboardFocusCues(true);
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
        if (IsModal && ReferenceEquals(item, FileMenu)) return;
        if (ReferenceEquals(item, FileMenu) && FileMenuMode == RibbonFileMenuMode.Backstage)
        {
            IsBackstageOpen = !IsBackstageOpen;
            return;
        }
        if (IsBackstageOpen) return;
        if (!_editors.CommitFocused() || !ReferenceEquals(item.Ribbon, this) || !item.EffectiveEnabled || !item.Visible) return;
        foreach (var surface in _popups.Surfaces.Reverse())
            if (surface.Targets.Where(t => ReferenceEquals(t.Element, item)).ToArray() is { Length: > 0 } rows)
            {
                var anchor = rows.Select(t => t.VisibleBounds).Where(r => !r.IsEmpty).Aggregate(Rectangle.Empty, (a, b) => a.IsEmpty ? b : Rectangle.Union(a, b));
                if (!anchor.IsEmpty)
                    _popups.Toggle(item, surface, anchor);
                return;
            }
        var bounds = Snapshot.Targets.Where(t => ReferenceEquals(t.Element, item)).Select(t => t.VisibleBounds).Where(r => !r.IsEmpty)
            .Aggregate(Rectangle.Empty, (a, b) => a.IsEmpty ? b : Rectangle.Union(a, b));
        if (!bounds.IsEmpty)
            _popups.Toggle(item, this, bounds);
    }
    internal void CloseDropDown(RibbonDropDownItem item)
    {
        if (ReferenceEquals(item, FileMenu) && IsBackstageOpen) IsBackstageOpen = false;
        else _popups.CloseMenu(CommandCatalog.Structure.Source(item) as RibbonDropDownItem ?? item, true);
    }
    internal bool IsInDesignMode => LicenseManager.UsageMode == LicenseUsageMode.Designtime || DesignMode;
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposing)
        {
            _disposing = true;
            _motion?.Dispose();
            _modalScope?.Release();
            ReleaseBackstageSurface();
            ReleaseCustomizationMenu();
            _input.Dispose();
            _screenTips.Dispose();
            _popups.Dispose();
            _merges?.Dispose();
            _galleryStrips.Dispose();
            _editors.Dispose();
            _simplified.Dispose();
            foreach (var tab in Tabs.ToArray())
                tab.Dispose();
            Tabs.Clear();
            foreach (var entry in QuickAccessItems.ToArray()) entry.Dispose();
            QuickAccessItems.Clear();
            FileMenu.Dispose();
            Backstage.Dispose();
            MessageBar.Dispose();
            CommandCatalog.Dispose();
            QuickAccessOverflow.Dispose();
            _modalClose.Dispose();
            _defaultRenderer.Dispose();
            DisposeAutomation();
        }
        base.Dispose(disposing);
    }
}
