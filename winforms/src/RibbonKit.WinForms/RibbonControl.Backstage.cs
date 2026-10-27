using System.ComponentModel;

namespace RibbonKit.WinForms;

public partial class RibbonControl
{
    private RibbonFileMenuMode _fileMenuMode;
    private RibbonBackstageSurface? _backstageSurface;
    private Form? _backstageForm;
    private Control? _backstagePriorFocus;
    private bool _backstageOpen, _backstageChanging, _backstageRefreshing;

    /// <summary>Gets the stable owned Backstage declaration. Its page Controls remain application-owned.</summary>
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
            if (_fileMenuMode == value || IsBackstageOpen && !CloseBackstage(true)) return;
            _popups.CloseMenu(FileMenu, false);
            _fileMenuMode = value;
            FileMenuModeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after FileMenuMode changes.</summary>
    public event EventHandler? FileMenuModeChanged;
    /// <summary>Gets or requests Backstage visibility. Opening requires Backstage mode, an available File entry and a shown ordinary Form. Cancelled opening, closing or page validation keeps the previous value. The selected ribbon tab and minimized state are retained.</summary>
    [Category("Behavior"), Bindable(true), DefaultValue(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsBackstageOpen
    {
        get => _backstageOpen;
        set { VerifyAccess(); if (value) OpenBackstage(); else CloseBackstage(true); }
    }
    /// <summary>Occurs after Backstage visibility changes, including forced teardown.</summary>
    public event EventHandler? IsBackstageOpenChanged;
    internal RibbonBackstageSurface? BackstageSurface => _backstageSurface;
    private bool CanShowBackstage => !_disposing && !IsDisposed && !IsInDesignMode && Visible && Enabled
        && FileMenuMode == RibbonFileMenuMode.Backstage && HasFileMenu && FileMenu.EffectiveEnabled
        && !Backstage.IsDisposed && Backstage.Visible && Backstage.EffectiveEnabled;
    private void OpenBackstage()
    {
        if (_backstageOpen || _backstageChanging || !CanShowBackstage || FindForm() is not { Visible: true, IsDisposed: false } form) return;
        if (!_editors.CommitFocused()) return;
        _backstageChanging = true;
        try
        {
            if (!FileMenu.CanOpen() || !CanShowBackstage || !Backstage.CanOpen() || !CanShowBackstage || !ReferenceEquals(FindForm(), form) || form.IsDisposed) return;
            _backstagePriorFocus = _keyTips ? _priorFocus : FocusedControl(form);
            _screenTips.Cancel();
            _editors.CloseLists();
            _popups.CloseAll(false);
            bool tips = _keyTips;
            ExitKeyTips(false);
            if (_backstageSurface is null || !ReferenceEquals(_backstageForm, form))
            {
                ReleaseBackstageSurface();
                _backstageForm = form;
                _backstageSurface = new(this) { Visible = false };
                form.Controls.Add(_backstageSurface);
                form.ClientSizeChanged += BackstageHostResized;
            }
            Backstage.Reconcile();
            _backstageSurface.Synchronize();
            _backstageSurface.Bounds = form.ClientRectangle;
            _backstageOpen = true;
            _backstageSurface.Visible = true;
            _backstageSurface.BringToFront();
            _backstageSurface.FocusInitial(tips);
            Invalidate();
            IsBackstageOpenChanged?.Invoke(this, EventArgs.Empty);
            if (_backstageOpen) Backstage.OnOpened();
        }
        finally { _backstageChanging = false; }
    }
    private void BackstageHostResized(object? sender, EventArgs e)
    {
        if (_backstageSurface is { IsDisposed: false } surface && _backstageForm is { } form) surface.Bounds = form.ClientRectangle;
    }
    internal bool ValidateBackstagePage() => !IsBackstageOpen || _backstageSurface?.ValidatePage() != false;
    private bool CloseBackstage(bool restoreFocus, bool force = false)
    {
        if (!_backstageOpen) return true;
        if (!force && _backstageChanging) return false;
        bool changing = _backstageChanging;
        _backstageChanging = true;
        try
        {
            if (!force && (!ValidateBackstagePage() || !Backstage.CanClose())) return false;
            if (!_backstageOpen) return true; // A callback may force teardown.
            bool hadFocus = FindForm()?.ContainsFocus == true;
            _backstageOpen = false;
            if (_backstageSurface is { IsDisposed: false } surface) { surface.HideKeyTips(); surface.Visible = false; }
            if (restoreFocus && !_disposing && hadFocus)
            {
                if (_backstagePriorFocus is { IsDisposed: false, CanFocus: true } previous) previous.Focus();
                else if (CanFocus) FocusElement(FileMenu, enterEditor: false);
            }
            _backstagePriorFocus = null;
            Invalidate();
            FileMenu.OnClosed();
            Backstage.OnClosed();
            IsBackstageOpenChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        finally { _backstageChanging = changing; }
    }
    internal void RefreshBackstage()
    {
        if (Backstage is null || _backstageRefreshing) return;
        _backstageRefreshing = true;
        try
        {
            if (_backstageOpen && !CanShowBackstage) CloseBackstage(false, true);
            if (!Backstage.IsDisposed) Backstage.Reconcile();
            _backstageSurface?.Synchronize();
        }
        finally { _backstageRefreshing = false; }
    }
    internal void DetachBackstageContent(Control content) => _backstageSurface?.DetachContent(content);
    private void ReleaseBackstageSurface()
    {
        CloseBackstage(false, true);
        if (_backstageForm is { } form) form.ClientSizeChanged -= BackstageHostResized;
        _backstageForm = null;
        var surface = _backstageSurface;
        _backstageSurface = null;
        surface?.Dispose();
    }
    internal void BackstageSurfaceDisposed(RibbonBackstageSurface surface)
    {
        if (!ReferenceEquals(surface, _backstageSurface)) return;
        CloseBackstage(false, true);
        if (_backstageForm is { } form) form.ClientSizeChanged -= BackstageHostResized;
        _backstageForm = null;
        _backstageSurface = null;
    }
    internal void ActivateBackstage(RibbonItem item)
    {
        if (!IsBackstageOpen || !Backstage.Items.Contains(item) || !item.Visible || !item.EffectiveEnabled) return;
        if (item is RibbonBackstagePageItem page) page.PerformClick();
        else if (CloseBackstage(true)) item.PerformClick();
    }
    internal bool ProcessBackstageKey(Keys keys) => _backstageSurface?.ProcessSurfaceKey(keys) ?? false;
    internal static Control? FocusedControl(ContainerControl container)
    {
        Control? active = container.ActiveControl;
        while (active is ContainerControl { ActiveControl: { } child }) active = child;
        return active;
    }
}
