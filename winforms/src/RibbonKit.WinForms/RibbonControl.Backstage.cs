using System.ComponentModel;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonControl
{
    /// <summary>Gets File's stable Application Menu declaration. Left commands remain in FileMenu.Items; CommandMenu keeps its numeric and serialized identity.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public RibbonApplicationMenu ApplicationMenu { get; }
    private RibbonFileMenuMode _fileMenuMode;
    private RibbonBackstageSurface? _backstageSurface;
    private Form? _backstageForm;
    private Control? _backstagePriorFocus;
    private bool _backstageOpen, _backstageChanging, _backstageRefreshing;
    private bool _backstageClosing;
    private bool _backstagePositioning;
    internal bool UsesMaterialBackstage => HasWindowBackdrop && Theme.BackstageStyle != RibbonBackstageStyle.Office2007;
    internal bool BackstageKeepsHeaders => RibbonPresentationResolver.Backstage(Theme).Placement == RibbonBackstagePlacement.BelowTabs;
    private bool BackstageHeadersUnselected => BackstageKeepsHeaders && (IsBackstageOpen || DesignPreviewBackstage);
    private bool _backstageContentInitialized, _initializingBackstageContent;
    private RibbonBackstageMotion? _backstageMotion;
    private RibbonMotionWindow? _backstageCaption;
    internal RibbonBackstageMotion? BackstageOpeningMotion => _backstageMotion;
    internal RibbonBackstageCaptionPolicy BackstageCaptionPolicy
    {
        get { var theme = Theme; return RibbonPresentationResolver.Backstage(theme).Caption.Resolve(theme); }
    }
    internal RibbonBackstageMotionPolicy BackstageMotionPolicy
    {
        get { var theme = Theme; return RibbonPresentationResolver.Backstage(theme).Motion.Resolve(theme, AnimationLevel); }
    }
    internal bool HasFullHeightBackstage => HasWindowCaption && BackstageCaptionPolicy.ExtendsIntoTitleBar;
    internal bool HasBackstageCaption => HasFullHeightBackstage && (IsBackstageOpen || WindowHost?.IsCaptionTitleAnimating == true);
    internal Bitmap? CaptureBackstageCaption() => _backstageCaption?.CaptureFrame();
    internal void UpdateBackstageCaption()
    {
        if (!HasBackstageCaption || _disposing || IsDisposed || WindowHost is not { IsDisposed: false, Disposing: false } host)
        {
            var caption = _backstageCaption; _backstageCaption = null;
            if (caption is null) return;
            try
            {
                // The underlying caption was painting background only while
                // this plane owned its ink. Publish the normal title before
                // removing the plane, rather than waiting for queued WM_PAINT.
                // Detach first so repaint callbacks can safely reenter/teardown.
                if (!_disposing && !IsDisposed && IsHandleCreated && HasWindowCaption &&
                    WindowHost is { IsDisposed: false, Disposing: false, IsHandleCreated: true } owner)
                {
                    Invalidate(new Rectangle(0, 0, Width, WindowCaptionHeight));
                    Update(); owner.Update();
                }
            }
            finally { caption.Dispose(); }
            return;
        }
        (_backstageCaption ??= new RibbonMotionWindow(host, clipToOwner: true)).RenderCaption(host, this);
    }

    /// <summary>Gets the stable owned Backstage declaration. Explicit page Content remains application-owned; optional ContentType instances are page-owned.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public RibbonBackstage Backstage { get; }
    /// <summary>Gets or chooses the surface opened by File. CommandMenu preserves existing consumers. Switching closes the current surface and respects page validation and Closing cancellation.</summary>
    [Category("Behavior"), DefaultValue(RibbonFileMenuMode.CommandMenu), Bindable(true)]
    public RibbonFileMenuMode FileMenuMode
    {
        get => _fileMenuMode;
        set
        {
            VerifyAccess();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_fileMenuMode == value || IsBackstageOpen && !CloseBackstage(true, animate: false)) return;
            _popups.CloseMenu(FileMenu, false);
            _fileMenuMode = value;
            FileMenuModeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after FileMenuMode changes.</summary>
    public event EventHandler? FileMenuModeChanged;
    /// <summary>Gets or requests Backstage visibility. AnimationLevel slides/fades the live panes; Theme.BackstageExtendsIntoTitleBar chooses whether they extend behind an integrated transparent caption, and Theme.BackstageSlideDistanceRatio controls travel. An accepted animated close retains true until motion completes; Closing/validation cancellation keeps it open. Command, mode and modal transitions close immediately. The selected ribbon tab and minimized state are retained.</summary>
    [Category("Behavior"), Bindable(true), DefaultValue(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsBackstageOpen
    {
        get => _backstageOpen;
        set { VerifyAccess(); if (value) OpenBackstage(); else CloseBackstage(true); }
    }
    /// <summary>Occurs after Backstage visibility changes, including forced teardown.</summary>
    public event EventHandler? IsBackstageOpenChanged;
    internal RibbonBackstageSurface? BackstageSurface => _backstageSurface;
    private Rectangle BackstageHostBounds(Form form)
    {
        var client = form is RibbonForm windowHost && ReferenceEquals(windowHost, WindowHost)
            ? windowHost.AeroContentBounds : form.ClientRectangle;
        if (BackstageKeepsHeaders)
        {
            EnsureLayout();
            int measuredBottom = _snapshot.Targets.Where(t => t.Kind is TargetKind.Tab or TargetKind.FileMenu or TargetKind.TabBack or TargetKind.TabNext)
                .Select(t => t.Bounds.Bottom).Append(_snapshot.TabViewport.Bottom).Max();
            int top = Math.Clamp(form.PointToClient(PointToScreen(new(0, measuredBottom))).Y, client.Top, client.Bottom);
            return Rectangle.FromLTRB(client.Left, top, client.Right, client.Bottom);
        }
        int caption = !HasFullHeightBackstage && form is RibbonForm { CustomCaption: true } window ? window.CaptionHeight : 0;
        return Rectangle.FromLTRB(client.Left, Math.Min(caption, client.Bottom), client.Right, client.Bottom);
    }
    private bool CanShowBackstage => !_disposing && !IsDisposed && !IsInDesignMode && !IsModal && Visible && Enabled
        && FileMenuMode == RibbonFileMenuMode.Backstage && HasFileMenu && FileMenu.EffectiveEnabled
        && !Backstage.IsDisposed && Backstage.Visible && Backstage.EffectiveEnabled;
    private void OpenBackstage()
    {
        if (_backstageClosing)
        {
            _backstageClosing = false; var closing = _backstageMotion; _backstageMotion = null;
            try { InvalidateBackstageFileHeader(); }
            finally { closing?.Cancel(); }
            Update();
            WindowHost?.BeginBackstageTitle(this, opening: true); UpdateBackstageCaption(); return;
        }
        if (_backstageOpen || _backstageChanging || !CanShowBackstage || FindForm() is not { Visible: true, IsDisposed: false } form) return;
        if (!_editors.CommitFocused()) return;
        _backstageChanging = true;
        RibbonBackstageBackground? openingBackground = null;
        try
        {
            if (!FileMenu.CanOpen() || !CanShowBackstage || !Backstage.CanOpen() || !CanShowBackstage || !ReferenceEquals(FindForm(), form) || form.IsDisposed) return;
            _motion?.Settle();
            _backstagePriorFocus = _keyTips ? _priorFocus : FocusedControl(form);
            _screenTips.Cancel();
            _galleryStrips.CancelPreviews();
            _editors.CloseLists();
            _popups.CloseAll(false);
            openingBackground = RibbonBackstageBackground.Capture(this, form);
            bool tips = _keyTips;
            ExitKeyTips(false);
            if (_backstageSurface is null || !ReferenceEquals(_backstageForm, form))
            {
                ReleaseBackstageSurface();
                _backstageForm = form;
                _backstageSurface = new(this) { Visible = false };
                form.Controls.Add(_backstageSurface);
                form.ClientSizeChanged += BackstageHostResized;
                form.Layout += BackstageHostLayout;
            }
            Backstage.Reconcile();
            _backstageSurface.Synchronize();
            _backstageSurface.Bounds = BackstageHostBounds(form);
            _ = _backstageSurface.Handle;
            _backstageSurface.PerformLayout();
            _backstageSurface.BringToFront();
            _backstageMotion?.Dispose();
            _backstageMotion = RibbonBackstageMotion.Prepare(this, _backstageSurface, form);
            if (_backstageMotion is { } opening) { opening.HoldBackground(openingBackground); openingBackground = null; }
            WindowHost?.BeginBackstageTitle(this, opening: true);
            _backstageOpen = true;
            _layoutDirty = true; // Suppress title-bar QAT presentation without changing its preference or host height.
            _backstageSurface.Visible = true;
            _backstageSurface.BringToFront();
            _backstageSurface.FocusInitial(tips);
            UpdateBackstageCaption();
            _backstageMotion?.Start();
            Invalidate();
            IsBackstageOpenChanged?.Invoke(this, EventArgs.Empty);
            if (_backstageOpen) Backstage.OnOpened();
        }
        catch { _backstageMotion?.Dispose(); _backstageMotion = null; throw; }
        finally { openingBackground?.Dispose(); _backstageChanging = false; }
    }
    private void BackstageHostResized(object? sender, EventArgs e)
    {
        if (_backstagePositioning) return;
        _backstagePositioning = true;
        try
        {
            if (_backstageSurface is { IsDisposed: false } surface && _backstageForm is { } form) surface.Bounds = BackstageHostBounds(form);
            WindowHost?.SnapCaptionTitle();
        }
        finally { _backstagePositioning = false; }
    }
    private void BackstageHostLayout(object? sender, LayoutEventArgs e)
    {
        // A native motion frame changes the pane's own Location. Only host or
        // ribbon reflow may recompute its settled anchor; do not undo travel.
        if ((BackstageKeepsHeaders || WindowHost is { FrameAppearance: not RibbonWindowFrameAppearance.Standard })
            && !ReferenceEquals(e.AffectedControl, _backstageSurface)) BackstageHostResized(sender, e);
    }
    internal bool ValidateBackstagePage() => !IsBackstageOpen || _backstageSurface?.ValidatePage() != false;
    private bool CloseBackstage(bool restoreFocus, bool force = false, bool animate = true)
    {
        if (!_backstageOpen) return true;
        _backstageSurface?.SettlePageMotion();
        if (_backstageClosing) { _backstageMotion?.Dispose(); return true; }
        _backstageMotion?.Dispose(); _backstageMotion = null;
        if (!force && _backstageChanging) return false;
        bool changing = _backstageChanging;
        _backstageChanging = true;
        try
        {
            if (!force && (!ValidateBackstagePage() || !Backstage.CanClose())) return false;
            if (!_backstageOpen) return true; // A callback may force teardown.
            bool hadFocus = FindForm()?.ContainsFocus == true;
            WindowHost?.BeginBackstageTitle(this, opening: false, allowMotion: !force && animate);
            if (!force && animate && _backstageSurface is { IsDisposed: false } closingSurface && _backstageForm is { } form)
            {
                _backstageMotion = RibbonBackstageMotion.Prepare(this, closingSurface, form, true, () => FinishBackstageClose(restoreFocus, hadFocus));
                if (_backstageMotion is { } closing)
                {
                    _backstageClosing = true;
                    closing.Start();
                    return true;
                }
            }
            FinishBackstageClose(restoreFocus, hadFocus, animateTitle: !force && animate);
            return true;
        }
        finally { _backstageChanging = changing; }
    }
    private void InvalidateBackstageFileHeader()
    {
        if (BackstageKeepsHeaders || _disposing || !IsHandleCreated) return;
        var file = Snapshot.Targets.FirstOrDefault(target => target.Kind == TargetKind.FileMenu);
        if (file is not { VisibleBounds.IsEmpty: false }) return;
        Invalidate(file.VisibleBounds);
    }
    private bool PointerOverBackstageFile(RibbonTarget file) => file.Enabled && _backstageForm is { ContainsFocus: true } &&
        file.VisibleBounds.Contains(PointToClient(MousePosition));

    private void FinishBackstageClose(bool restoreFocus, bool hadFocus, bool animateTitle = true)
    {
        if (!_backstageOpen) return;
        bool changing = _backstageChanging; _backstageChanging = true;
        try
        {
            if (_backstageClosing && !BackstageKeepsHeaders && !_disposing &&
                Snapshot.Targets.FirstOrDefault(target => target.Kind == TargetKind.FileMenu) is { } file)
            {
                bool hot = PointerOverBackstageFile(file);
                if (hot || ReferenceEquals(_hot, FileMenu))
                {
                    _hot = hot ? FileMenu : null;
                    _hotDropDown = false;
                    _hotScroll = null;
                }
            }
            _backstageClosing = false; _backstageMotion = null; _backstageOpen = false;
            _layoutDirty = true;
            // Uncover the ordinary caption before its foreground handback.
            if (_backstageSurface is { IsDisposed: false } surface) { surface.HideKeyTips(); surface.Visible = false; }
            WindowHost?.RetargetClosedTitle(this, animateTitle);
            UpdateBackstageCaption();
            if (restoreFocus && !_disposing && hadFocus && FindForm() is { Disposing: false, IsDisposed: false, ContainsFocus: true })
            {
                if (_backstagePriorFocus is { IsDisposed: false, CanFocus: true } previous) previous.Focus();
                else if (CanFocus) FocusElement(FileMenu, enterEditor: false);
            }
            _backstagePriorFocus = null; Invalidate(); FileMenu.OnClosed(); Backstage.OnClosed(); IsBackstageOpenChanged?.Invoke(this, EventArgs.Empty);
        }
        finally { _backstageChanging = changing; }
    }
    private void InitializeBackstageContent()
    {
        if (Backstage is null || Backstage.IsDisposed || _disposing || IsDisposed || IsInDesignMode || _updateDepth > 0 || _initializingBackstageContent) return;
        _backstageContentInitialized = true;
        _initializingBackstageContent = true;
        try
        {
            foreach (var page in Backstage.NavigationItems.OfType<RibbonBackstagePageItem>().ToArray())
                if (!page.IsDisposed && ReferenceEquals(page.Parent, Backstage)) page.EnsureContent();
        }
        finally { _initializingBackstageContent = false; }
    }
    internal void RefreshBackstage()
    {
        if (Backstage is null || _backstageRefreshing) return;
        _backstageRefreshing = true;
        try
        {
            if (_backstageContentInitialized) InitializeBackstageContent();
            if (_backstageOpen && !CanShowBackstage) CloseBackstage(false, true);
            if (!Backstage.IsDisposed) Backstage.Reconcile();
            _backstageSurface?.Synchronize();
            if (_backstageForm is not null) BackstageHostResized(this, EventArgs.Empty);
        }
        finally { _backstageRefreshing = false; }
    }
    internal void DetachBackstageContent(Control content) => _backstageSurface?.DetachContent(content);
    private void ReleaseBackstageSurface()
    {
        _backstageMotion?.Dispose(); _backstageMotion = null;
        CloseBackstage(false, true);
        WindowHost?.SnapCaptionTitle();
        _backstageCaption?.Dispose(); _backstageCaption = null;
        if (_backstageForm is { } form) { form.ClientSizeChanged -= BackstageHostResized; form.Layout -= BackstageHostLayout; }
        _backstageForm = null;
        var surface = _backstageSurface;
        _backstageSurface = null;
        surface?.Dispose();
    }
    internal void BackstageSurfaceDisposed(RibbonBackstageSurface surface)
    {
        if (!ReferenceEquals(surface, _backstageSurface)) return;
        CloseBackstage(false, true);
        if (_backstageForm is { } form) { form.ClientSizeChanged -= BackstageHostResized; form.Layout -= BackstageHostLayout; }
        _backstageForm = null;
        _backstageSurface = null;
    }
    internal void ActivateBackstage(RibbonItem item)
    {
        if (!IsBackstageOpen || item is RibbonMenuSeparatorItem || !Backstage.Contains(item) || !item.Visible || !item.EffectiveEnabled) return;
        if (item is RibbonBackstagePageItem page) page.PerformClick();
        else if (CloseBackstage(true, animate: false)) item.PerformClick();
    }
    internal bool ProcessBackstageKey(Keys keys) => _backstageSurface?.ProcessSurfaceKey(keys) ?? false;
    internal bool IsExposedBackstageTarget(RibbonTarget target) => Theme.BackstageStyle == RibbonBackstageStyle.Office2007 && target.Kind == TargetKind.FileMenu
        || BackstageKeepsHeaders && (target.Kind is TargetKind.Tab or TargetKind.FileMenu or TargetKind.TabBack or TargetKind.TabNext or TargetKind.Minimize
        || target.Kind is TargetKind.QuickAccess or TargetKind.QuickAccessOverflow && EffectiveQuickAccessPosition != RibbonQuickAccessPosition.BelowRibbon);
    private bool IsExposedBackstageElement(RibbonElement element) => BackstageKeepsHeaders && Snapshot.Targets.Any(t => ReferenceEquals(t.Element, element) && IsExposedBackstageTarget(t));
    private bool BackstageHeaderHasKeyboardInput(Keys keys) => BackstageKeepsHeaders && ContainsFocus && keys is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.Enter or Keys.Space or Keys.Tab or (Keys.Tab | Keys.Shift);
    /// <inheritdoc />
    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        if (_backstageSurface is not null && BackstageKeepsHeaders) BackstageHostResized(this, e);
    }
    internal static Control? FocusedControl(ContainerControl container)
    {
        Control? active = container.ActiveControl;
        while (active is ContainerControl { ActiveControl: { } child }) active = child;
        return active;
    }
}
