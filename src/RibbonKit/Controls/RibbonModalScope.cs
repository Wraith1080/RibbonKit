using System.Windows;

namespace RibbonKit.Controls;

/// <summary>
/// Why a modal-tab transition is happening. Carried on <see cref="RibbonModalEventArgs"/> so a
/// handler can tell a user-initiated close from an application- or teardown-driven one.
/// </summary>
public enum RibbonModalReason
{
    /// <summary>The application called <see cref="Ribbon.EnterModal"/> / <see cref="Ribbon.ExitModal()"/>.</summary>
    Application,

    /// <summary>The user clicked the close affordance at the end of the tab strip.</summary>
    CloseButton,

    /// <summary>
    /// The modal tab left the ribbon (removed from <see cref="Ribbon.Tabs"/>, or unmerged once
    /// tab merging lands). Modal mode ends unconditionally — this reason is never cancellable.
    /// </summary>
    TabRemoved,
}

/// <summary>
/// Arguments for the ribbon's modal-tab transitions. <see cref="Cancel"/> is honoured on the
/// "-ing" events (<see cref="Ribbon.ModalEntering"/> / <see cref="Ribbon.ModalExiting"/>) and
/// ignored on the "-ed" ones, except when <see cref="Reason"/> is
/// <see cref="RibbonModalReason.TabRemoved"/> — the tab is already gone, so exit cannot be refused.
/// </summary>
public class RibbonModalEventArgs : EventArgs
{
    /// <summary>Initializes the arguments for a transition involving <paramref name="tab"/>.</summary>
    public RibbonModalEventArgs(RibbonTab tab, RibbonModalReason reason)
    {
        Tab = tab;
        Reason = reason;
    }

    /// <summary>The tab entering or leaving modal mode.</summary>
    public RibbonTab Tab { get; }

    /// <summary>What triggered the transition.</summary>
    public RibbonModalReason Reason { get; }

    /// <summary>Set to <see langword="true"/> in a "-ing" handler to refuse the transition.</summary>
    public bool Cancel { get; set; }
}

/// <summary>
/// Owns modal-tab state for one <see cref="Ribbon"/>: which tab is modal, what every other tab's
/// visibility was before it was hidden, and which tab to reselect on exit.
/// </summary>
/// <remarks>
/// <para>
/// Modal mode hides the other tabs with plain <see cref="UIElement.Visibility"/>, which is what
/// makes it cheap: the ribbon's existing selection guard (<c>OnTabIsVisibleChanged</c> →
/// <c>FindFirstVisibleTab</c>) and <c>KeyTipService</c>'s visible-tab filter then do the right
/// thing with no special-casing — matching architecture §8's rule that core layout must never
/// know about modal tabs.
/// </para>
/// <para>
/// The catch is that <b>visibility is also persisted</b>:
/// <c>RibbonCustomizationSerializer</c> captures <c>tab.Visibility</c> per tab, so saving ribbon
/// state while modal would write every other tab as hidden and restore a one-tab ribbon on the
/// next run. That is why this scope records each tab's pre-modal value and publishes it through
/// <see cref="Ribbon.GetAuthoredVisibility"/>, which the serializer reads instead of the live
/// property. See docs/06-MERGE-AND-MODAL-PLAN.md §5.1.
/// </para>
/// </remarks>
internal sealed class RibbonModalScope
{
    private readonly Ribbon _ribbon;

    // Pre-modal Visibility per tab, for every tab this scope hid (plus the modal tab itself, in
    // case it was hidden before being entered). Empty when not modal.
    private readonly Dictionary<RibbonTab, Visibility> _authored = new();

    private RibbonTab? _modalTab;
    private RibbonTab? _restoreSelection;

    // Guards this scope's own Visibility/selection writes so re-entrant notifications
    // (OnTabIsVisibleChanged, OnTabsCollectionChanged) can't record derived state as authored.
    private bool _applying;

    internal RibbonModalScope(Ribbon ribbon) => _ribbon = ribbon;

    /// <summary>The tab currently held modal, or <see langword="null"/>.</summary>
    internal RibbonTab? ModalTab => _modalTab;

    /// <summary>Whether a modal tab is active.</summary>
    internal bool IsActive => _modalTab is not null;

    /// <summary>
    /// The visibility <paramref name="tab"/> would have if modal mode were not active — the live
    /// value when this scope did not hide it.
    /// </summary>
    internal Visibility GetAuthoredVisibility(RibbonTab tab) =>
        _authored.TryGetValue(tab, out Visibility authored) ? authored : tab.Visibility;

    /// <summary>
    /// Sets the visibility <paramref name="tab"/> should have once modal mode ends. Applies
    /// immediately when the tab is not currently hidden by this scope.
    /// </summary>
    internal void SetAuthoredVisibility(RibbonTab tab, Visibility value)
    {
        if (_authored.ContainsKey(tab) && !ReferenceEquals(tab, _modalTab))
        {
            // Held hidden by modal mode: remember the new intent, apply it at exit.
            _authored[tab] = value;
            return;
        }

        tab.Visibility = value;
    }

    /// <summary>
    /// Enters modal mode on <paramref name="tab"/>. Returns <see langword="false"/> when a handler
    /// cancelled, or when an existing modal tab refused to exit first.
    /// </summary>
    internal bool Enter(RibbonTab tab, RibbonModalReason reason)
    {
        if (ReferenceEquals(_modalTab, tab))
        {
            return true;
        }

        // Only one tab can be modal; the outgoing one gets a normal, cancellable exit.
        if (_modalTab is not null && !Exit(RibbonModalReason.Application, force: false))
        {
            return false;
        }

        if (!_ribbon.RaiseModalEntering(tab, reason))
        {
            return false;
        }

        _applying = true;
        try
        {
            _restoreSelection = _ribbon.SelectedTab;

            // Record the modal tab's own pre-modal visibility too: an app may enter modal on a
            // tab it keeps collapsed the rest of the time, and exit must put it back.
            _authored[tab] = tab.Visibility;
            tab.Visibility = Visibility.Visible;

            // Select BEFORE hiding the others. The ribbon hands selection to the first visible
            // tab when the selected one disappears, so hiding first would flash the wrong tab.
            _ribbon.SelectedTab = tab;

            foreach (RibbonTab other in _ribbon.Tabs)
            {
                if (ReferenceEquals(other, tab))
                {
                    continue;
                }

                _authored[other] = other.Visibility;
                other.Visibility = Visibility.Collapsed;
            }

            _modalTab = tab;
        }
        finally
        {
            _applying = false;
        }

        _ribbon.OnModalStateChanged();
        _ribbon.RaiseModalEntered(tab, reason);
        return true;
    }

    /// <summary>
    /// Leaves modal mode. Returns <see langword="false"/> when a handler cancelled; pass
    /// <paramref name="force"/> to skip the cancellable event (used when the modal tab is
    /// being removed and refusing is not an option).
    /// </summary>
    internal bool Exit(RibbonModalReason reason, bool force)
    {
        if (_modalTab is not { } tab)
        {
            return true;
        }

        if (!force && !_ribbon.RaiseModalExiting(tab, reason))
        {
            return false;
        }

        _applying = true;
        try
        {
            // Clear the flag first so anything that reacts to a visibility change (selection
            // guard, coercion of IsMinimized/IsBackstageOpen) already sees a non-modal ribbon.
            _modalTab = null;

            foreach (KeyValuePair<RibbonTab, Visibility> entry in _authored)
            {
                // A tab removed while modal is no longer ours to restore.
                if (_ribbon.Tabs.Contains(entry.Key))
                {
                    entry.Key.Visibility = entry.Value;
                }
            }

            _authored.Clear();

            RibbonTab? restore = _restoreSelection;
            _restoreSelection = null;

            // The pre-modal tab may have been removed or hidden in the meantime.
            if (restore is null
                || !_ribbon.Tabs.Contains(restore)
                || restore.Visibility != Visibility.Visible)
            {
                restore = _ribbon.Tabs.FirstOrDefault(t => t.Visibility == Visibility.Visible);
            }

            _ribbon.SelectedTab = restore;
        }
        finally
        {
            _applying = false;
        }

        _ribbon.OnModalStateChanged();
        _ribbon.RaiseModalExited(tab, reason);
        return true;
    }

    /// <summary>
    /// A tab joined the ribbon. While modal it is hidden immediately and recorded, so exiting
    /// reveals it correctly (docs/06-MERGE-AND-MODAL-PLAN.md §5.6 — this is also the path a tab
    /// merged during modal mode will take).
    /// </summary>
    internal void OnTabAdded(RibbonTab tab)
    {
        if (_applying || _modalTab is null || ReferenceEquals(_modalTab, tab))
        {
            return;
        }

        _authored[tab] = tab.Visibility;
        tab.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// A tab left the ribbon. Removing the modal tab ends modal mode unconditionally — the tab
    /// is gone, so a handler cannot meaningfully refuse.
    /// </summary>
    internal void OnTabRemoved(RibbonTab tab)
    {
        if (_applying)
        {
            return;
        }

        if (ReferenceEquals(_modalTab, tab))
        {
            Exit(RibbonModalReason.TabRemoved, force: true);
            return;
        }

        _authored.Remove(tab);
    }

    /// <summary>
    /// The tab collection was rebuilt wholesale (a <see cref="System.Collections.Specialized.NotifyCollectionChangedAction.Reset"/>
    /// — <c>Tabs.Clear()</c> reports no removed items). <c>RibbonCustomizationSerializer.Apply</c>
    /// does exactly this, so modal state must be reconciled against the new contents.
    /// </summary>
    internal void OnCollectionReset()
    {
        if (_applying || _modalTab is null)
        {
            return;
        }

        if (!_ribbon.Tabs.Contains(_modalTab))
        {
            Exit(RibbonModalReason.TabRemoved, force: true);
            return;
        }

        foreach (RibbonTab tab in _authored.Keys.ToList())
        {
            if (!_ribbon.Tabs.Contains(tab))
            {
                _authored.Remove(tab);
            }
        }

        // Tabs re-added by the rebuild have their authored visibility again — hide them back.
        foreach (RibbonTab tab in _ribbon.Tabs)
        {
            if (ReferenceEquals(tab, _modalTab) || _authored.ContainsKey(tab))
            {
                continue;
            }

            _authored[tab] = tab.Visibility;
            tab.Visibility = Visibility.Collapsed;
        }
    }
}
