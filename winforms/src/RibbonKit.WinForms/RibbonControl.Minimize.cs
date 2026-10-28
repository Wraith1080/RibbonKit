using System.ComponentModel;

namespace RibbonKit.WinForms;

public partial class RibbonControl
{
    private bool _isMinimized;

    /// <summary>Gets or sets whether only the tab row is docked. Activating a tab temporarily opens its groups. Programmatic changes use the normal editor parking contract: valid edits commit and invalid drafts roll back.</summary>
    [DefaultValue(false), Bindable(true), Category("Behavior")]
    public bool IsMinimized
    {
        get => _isMinimized;
        set
        {
            VerifyAccess();
            if (value && IsModal) return;
            if (_isMinimized == value) return;
            bool restore = ContainsFocus || _popups.ContainsFocus || _editors.ContainsFocus;
            _screenTips.Cancel();
            ExitKeyTips(false);
            _popups.CloseAll(false);
            _isMinimized = value;
            _layoutDirty = true;
            _focused = SelectedTab;
            _focusedDropDown = false;
            _hot = _pressed = null;
            _hotScroll = _pressedScroll = null;
            Capture = false;
            if (_updateDepth == 0) { UpdateHeight(); SyncEditors(); }
            if (restore && CanFocus) Focus();
            Invalidate();
            AccessibilityChanged();
            IsMinimizedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Occurs after the minimized state changes. Presentation updates are deferred during an update batch.</summary>
    public event EventHandler? IsMinimizedChanged;

    internal void ToggleMinimizedByUser()
    {
        if (!Enabled || !Visible || IsInDesignMode || !_editors.CommitFocused()) return;
        IsMinimized = !IsMinimized;
    }

    internal void TabPopupClosed()
    {
        if (_keyTips) { _topTips = true; _keyPrefix = ""; }
    }
}
