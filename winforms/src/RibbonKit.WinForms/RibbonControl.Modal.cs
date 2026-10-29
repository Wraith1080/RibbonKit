using System.ComponentModel;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

/// <summary>Identifies an application, user or forced modal transition.</summary>
public enum RibbonModalReason
{
    /// <summary>The application requested entry or exit.</summary>
    Application,
    /// <summary>The user activated the header close button.</summary>
    CloseButton,
    /// <summary>The user pressed Escape after dismissing child surfaces and KeyTips.</summary>
    Escape,
    /// <summary>The active tab was removed, unmerged, hidden, disabled or ceased to be modal. Exit cannot be cancelled.</summary>
    TabUnavailable
}

/// <summary>Describes a modal transition. Cancel is honored only by Entering/Exiting; a forced exit cannot be vetoed.</summary>
public sealed class RibbonModalEventArgs : CancelEventArgs
{
    /// <summary>Initializes transition data for a tab and reason.</summary>
    public RibbonModalEventArgs(RibbonTab tab, RibbonModalReason reason)
    {
        ArgumentNullException.ThrowIfNull(tab);
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason));
        Tab = tab; Reason = reason;
    }
    /// <summary>Gets the entering or departing modal tab.</summary>
    public RibbonTab Tab { get; }
    /// <summary>Gets the transition trigger.</summary>
    public RibbonModalReason Reason { get; }
    /// <summary>Gets whether exit must complete despite cancellation or editor validation.</summary>
    public bool IsForced => Reason == RibbonModalReason.TabUnavailable;
}

public partial class RibbonControl
{
    private RibbonModalScope? _modalScope;
    private readonly RibbonButtonItem _modalClose;
    /// <summary>Gets whether one tab has exclusive presentation. Authored visibility is never rewritten.</summary>
    [Browsable(false), Bindable(true), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsModal => _modalScope?.Tab is not null;
    /// <summary>Gets the exclusively presented tab, or null outside modal mode.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonTab? ModalTab => _modalScope?.Tab;
    /// <summary>Occurs after modal mode starts or ends.</summary>
    public event EventHandler? IsModalChanged;
    /// <summary>Occurs before modal entry. Set Cancel to refuse entry without changing presentation.</summary>
    public event EventHandler<RibbonModalEventArgs>? ModalEntering;
    /// <summary>Occurs after successful modal entry.</summary>
    public event EventHandler<RibbonModalEventArgs>? ModalEntered;
    /// <summary>Occurs before modal exit. Forced TabUnavailable exit ignores Cancel.</summary>
    public event EventHandler<RibbonModalEventArgs>? ModalExiting;
    /// <summary>Occurs after modal exit and presentation restoration.</summary>
    public event EventHandler<RibbonModalEventArgs>? ModalExited;
    /// <summary>Enters a visible, enabled, presented tab marked IsModal. Returns false for cancellation, invalid native editor input or a transition already in progress. Exit the current mode before entering a different one.</summary>
    public bool EnterModal(RibbonTab tab)
    {
        VerifyAccess(); ObjectDisposedException.ThrowIf(_disposing || IsDisposed, this); ArgumentNullException.ThrowIfNull(tab);
        return (_modalScope ??= new(this)).Enter(tab);
    }
    /// <summary>Requests a cancellable application exit, preserving ordinary native editor validation. Returns true when already outside modal mode.</summary>
    public bool ExitModal() { VerifyAccess(); return _modalScope?.Exit(RibbonModalReason.Application) ?? true; }
    internal bool ExitModalByUser(RibbonModalReason reason) => _modalScope?.Exit(reason) ?? false;
    internal IReadOnlyList<RibbonTab> UnfilteredTabs => _merges?.Tabs() ?? Tabs.ToArray();
    internal (TargetKind Kind, RibbonElement? Element)? HeaderAction => IsModal
        ? ModalTab!.CanClose ? (TargetKind.ModalClose, _modalClose) : null
        : (TargetKind.Minimize, null);

    // Owns modal state outside adaptive layout. Presentation filters preserve
    // authored visibility and allow merge/visibility edits to survive exit.
    private sealed class RibbonModalScope(RibbonControl ribbon)
    {
        internal RibbonTab? Tab { get; private set; }
        private RibbonTab? _returnTab;
        private Control? _returnFocus;
        private bool _returnMinimized, _changing;
        internal bool Changing => _changing;
        private int _returnIndex;
        private bool Available(RibbonTab tab) => !ribbon._disposing && !tab.IsDisposed && tab.IsModal && tab.Visible && tab.EffectiveEnabled && ribbon.UnfilteredTabs.Contains(tab);
        internal bool Enter(RibbonTab tab)
        {
            if (_changing || ribbon.IsInDesignMode) return false;
            if (ReferenceEquals(Tab, tab)) return true;
            if (Tab is not null) throw new InvalidOperationException("Exit the current modal tab before entering another.");
            if (!Available(tab)) throw new ArgumentException("Enter a visible, enabled, presented tab marked IsModal.", nameof(tab));
            _changing = true;
            try
            {
                var args = new RibbonModalEventArgs(tab, RibbonModalReason.Application);
                ribbon.ModalEntering?.Invoke(ribbon, args);
                if (args.Cancel || !Available(tab) || !ribbon._editors.CommitFocused() || !Available(tab)) return false;
                if (!ribbon.CloseBackstage(true) || !Available(tab)) return false;
                _returnTab = ribbon.SelectedTab; _returnIndex = ribbon.UnfilteredTabs.ToList().IndexOf(_returnTab!);
                _returnMinimized = ribbon.IsMinimized;
                var focused = ribbon.FindForm() is { } form ? FocusedControl(form) : null;
                _returnFocus = (ribbon._keyTips || ReferenceEquals(focused, ribbon)) && ribbon._priorFocus is { IsDisposed: false, Visible: true, CanFocus: true } priorFocus ? priorFocus : focused;
                ribbon._popups.CloseAll(false); ribbon._editors.CloseLists(); ribbon._galleryStrips.CancelPreviews(); ribbon.ExitKeyTips(false);
                if (!Available(tab)) return false;
                SyncCaption(tab); Tab = tab;
                var previous = ribbon._selectedTab; ribbon._selectedTab = tab; ribbon._focused = tab; ribbon._focusedDropDown = false;
                ribbon._tabOffset = ribbon._bodyOffset = 0; ribbon._fallbackTabIndex = -1;
                ribbon.IsMinimized = false;
                ribbon.ModelChanged();
                if (ReferenceEquals(ribbon.SelectedTab, tab) && !ReferenceEquals(previous, tab)) ribbon.SelectedTabChanged?.Invoke(ribbon, EventArgs.Empty);
                ribbon.IsModalChanged?.Invoke(ribbon, EventArgs.Empty);
                if (Available(tab)) ribbon.ModalEntered?.Invoke(ribbon, args);
                return Available(tab);
            }
            finally { _changing = false; Reconcile(); }
        }
        internal bool Exit(RibbonModalReason reason)
        {
            if (Tab is not { } tab) return true;
            if (_changing) return false;
            bool forced = reason == RibbonModalReason.TabUnavailable;
            if (!forced && reason is RibbonModalReason.CloseButton or RibbonModalReason.Escape && !tab.CanClose) return false;
            _changing = true;
            try
            {
                var args = new RibbonModalEventArgs(tab, reason);
                ribbon.ModalExiting?.Invoke(ribbon, args);
                if (ribbon._disposing || ribbon.IsDisposed) return false;
                if (!forced && (args.Cancel || !ribbon._editors.CommitFocused())) return false;
                bool hadFocus = ribbon.FindForm()?.ContainsFocus == true;
                var focus = _returnFocus;
                Tab = null;
                ribbon._popups.CloseAll(false); ribbon._editors.CloseLists(); ribbon.ExitKeyTips(false);
                foreach (var gallery in ribbon.AllElements.OfType<RibbonGalleryItem>().ToArray()) gallery.CancelPreview();
                var tabs = ribbon.UnfilteredTabs;
                var restored = _returnTab is { Visible: true, EffectiveEnabled: true } prior && tabs.Contains(prior) ? prior
                    : tabs.Skip(Math.Max(0, _returnIndex)).FirstOrDefault(t => t.Visible && t.EffectiveEnabled)
                        ?? tabs.Take(Math.Max(0, _returnIndex)).LastOrDefault(t => t.Visible && t.EffectiveEnabled);
                var previous = ribbon._selectedTab; ribbon._selectedTab = restored; ribbon._focused = restored; ribbon._focusedDropDown = false;
                ribbon._tabOffset = ribbon._bodyOffset = 0; ribbon._fallbackTabIndex = -1;
                ribbon.IsMinimized = _returnMinimized;
                _returnFocus = null; _returnTab = null;
                ribbon.ModelChanged();
                if (ReferenceEquals(ribbon.SelectedTab, restored) && !ReferenceEquals(previous, restored)) ribbon.SelectedTabChanged?.Invoke(ribbon, EventArgs.Empty);
                if (hadFocus && !ribbon._disposing)
                {
                    if (focus is { IsDisposed: false, Visible: true, CanFocus: true } && ReferenceEquals(focus.FindForm(), ribbon.FindForm())) focus.Focus();
                    else if (ribbon.CanFocus) ribbon.Focus();
                }
                ribbon.IsModalChanged?.Invoke(ribbon, EventArgs.Empty);
                ribbon.ModalExited?.Invoke(ribbon, args);
                return true;
            }
            finally { _changing = false; Reconcile(); }
        }
        private void SyncCaption(RibbonTab tab)
        {
            string text = tab.CloseButtonText.Length == 0 ? "Close" : tab.CloseButtonText;
            if (ribbon._modalClose.Text != text) ribbon._modalClose.Text = text;
        }
        internal void Reconcile()
        {
            if (_changing || Tab is not { } tab) return;
            if (!Available(tab)) Exit(RibbonModalReason.TabUnavailable);
            else SyncCaption(tab);
        }
        internal void Release() { Tab = null; _returnTab = null; _returnFocus = null; }
    }
}
