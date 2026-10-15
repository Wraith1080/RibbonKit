using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using RibbonKit.Animation;
using RibbonKit.Layout;

namespace RibbonKit.Controls;

public partial class RibbonGroup
{
    private readonly HashSet<RibbonGroupQuickAccessProxy> _quickAccessReferences = new();
    private RibbonGroupQuickAccessProxy? _quickAccessProxy;
    private Ribbon? _quickAccessRibbon;
    private RibbonTab? _quickAccessTab;
    private Panel? _detachedPopupParent;
    private int _detachedPopupIndex;
    private PlacementMode _quickAccessOriginalPlacement;
    private CustomPopupPlacementCallback? _quickAccessOriginalPlacementCallback;
    private RibbonGroupSizeState _adaptiveSizeState = RibbonGroupSizeState.Large;
    private readonly List<RibbonDensityTransition> _quickAccessTransitions = new();
    private readonly List<DependencyObject> _quickAccessHoverContent = new();
    private bool _endingQuickAccess;

    internal bool IsQuickAccessCollapsed => _quickAccessReferences.Count != 0;
    internal FrameworkElement? QuickAccessContent => _popupHost?.Child as FrameworkElement;
    internal bool FocusLastQuickAccessItem { get => _focusLastItem; set => _focusLastItem = value; }

    internal void RetainQuickAccess(RibbonGroupQuickAccessProxy proxy)
    {
        if (_quickAccessReferences.Contains(proxy)) return;
        var positions = CaptureGroupPositions();
        _quickAccessReferences.Add(proxy);
        SetValue(SizeStatePropertyKey, RibbonGroupSizeState.Collapsed);
        CommitQuickAccessLayout(positions);
    }

    internal void ReleaseQuickAccess(RibbonGroupQuickAccessProxy proxy)
    {
        if (!_quickAccessReferences.Contains(proxy)) return;
        if (_quickAccessReferences.Count == 1) EndQuickAccess();
        else EndQuickAccess(proxy);
        var positions = CaptureGroupPositions();
        _quickAccessReferences.Remove(proxy);
        if (!IsQuickAccessCollapsed) SetValue(SizeStatePropertyKey, _adaptiveSizeState);
        CommitQuickAccessLayout(positions);
    }

    // Collapse is derived from live source-flyout projections (QAT or simplified),
    // never persisted as group layout.
    private void CommitQuickAccessLayout(List<(FrameworkElement Surface, Point Position)> positions)
    {
        InvalidateHostPanel();
        Window.GetWindow(this)?.UpdateLayout();
        PlayCommittedGroupTransition(positions);
    }

    internal bool BeginQuickAccess(RibbonGroupQuickAccessProxy proxy, Ribbon ribbon)
    {
        EndQuickAccess();
        var sourceTab = ribbon.Tabs.FirstOrDefault(tab => tab.Groups.Contains(this));
        // Realize the source host without displaying/selecting its tab. This
        // preserves its logical ownership and inherited bindings/resources/flow.
        if (!IsLoaded && sourceTab?.Content is FrameworkElement sourceHost)
            sourceHost.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        ApplyTemplate();
        if (_popup is null || _normalHost is null || _popupHost is null || !IsQuickAccessCollapsed) return false;
        _collapsedButton?.SetCurrentValue(ToggleButton.IsCheckedProperty, false);
        _popup.SetCurrentValue(Popup.IsOpenProperty, false);
        RestoreFlyoutContent();
        _quickAccessProxy = proxy;
        _quickAccessRibbon = ribbon;
        _quickAccessTab = sourceTab;
        ribbon.Tabs.CollectionChanged += OnQuickAccessCollectionChanged;
        if (_quickAccessTab is not null) _quickAccessTab.Groups.CollectionChanged += OnQuickAccessCollectionChanged;
        DependencyPropertyDescriptor.FromProperty(Ribbon.SelectedTabProperty, typeof(Ribbon))
            .AddValueChanged(ribbon, OnQuickAccessTabChanged);
        Unloaded += OnQuickAccessUnloaded;
        // WPF defers a popup in an unloaded visual tree until Loaded. Detach the
        // existing popup for an inactive tab; its template/namescope/content stay
        // source-owned, and it can use the live QAT anchor without selecting a tab.
        if (!_popup.IsLoaded && VisualTreeHelper.GetParent(_popup) is Panel parent)
        {
            _detachedPopupParent = parent;
            _detachedPopupIndex = parent.Children.IndexOf(_popup);
            parent.Children.Remove(_popup);
        }
        _popup.SetCurrentValue(Popup.PlacementTargetProperty, proxy);
        _quickAccessOriginalPlacement = _popup.Placement;
        _quickAccessOriginalPlacementCallback = _popup.CustomPopupPlacementCallback;
        if (RibbonOverflowMenu.IsEntry(proxy)) RibbonOverflowMenu.ConfigurePopup(proxy, _popup, _popupHost);
        _popup.SetCurrentValue(Popup.IsOpenProperty, true);
        return true;
    }

    internal void CloseQuickAccess(RibbonGroupQuickAccessProxy proxy)
    {
        if (!ReferenceEquals(_quickAccessProxy, proxy) || _endingQuickAccess) return;
        CloseFlyout();
    }

    internal void EndQuickAccess(RibbonGroupQuickAccessProxy? expected = null)
    {
        if (_quickAccessProxy is not { } proxy || _endingQuickAccess
            || expected is not null && !ReferenceEquals(expected, proxy)) return;
        _endingQuickAccess = true;
        try
        {
            _dismissHelper?.OnClosed();
            _popup?.SetCurrentValue(Popup.IsOpenProperty, false);
            RestoreFlyoutContent();
            RestoreQuickAccessHoverContent();
            proxy.SetCurrentValue(RibbonDropDownButton.IsDropDownOpenProperty, false);
            if (_quickAccessRibbon is { } ribbon)
            {
                ribbon.Tabs.CollectionChanged -= OnQuickAccessCollectionChanged;
                DependencyPropertyDescriptor.FromProperty(Ribbon.SelectedTabProperty, typeof(Ribbon))
                    .RemoveValueChanged(ribbon, OnQuickAccessTabChanged);
            }
            if (_quickAccessTab is not null) _quickAccessTab.Groups.CollectionChanged -= OnQuickAccessCollectionChanged;
            _quickAccessProxy = null;
            _quickAccessRibbon = null;
            _quickAccessTab = null;
            Unloaded -= OnQuickAccessUnloaded;
            _popup?.SetCurrentValue(Popup.PlacementTargetProperty, _collapsedButton);
            _popup?.SetCurrentValue(Popup.PlacementProperty, _quickAccessOriginalPlacement);
            _popup?.SetCurrentValue(Popup.CustomPopupPlacementCallbackProperty, _quickAccessOriginalPlacementCallback);
            _quickAccessOriginalPlacementCallback = null;
            if (_detachedPopupParent is { } parent && _popup is not null)
                parent.Children.Insert(Math.Min(_detachedPopupIndex, parent.Children.Count), _popup);
            _detachedPopupParent = null;
        }
        finally { _endingQuickAccess = false; }
    }

    private void OnQuickAccessCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_quickAccessTab is { } tab && _quickAccessRibbon is { } ribbon
            && (!ribbon.Tabs.Contains(tab) || !tab.Groups.Contains(this))) EndQuickAccess();
    }

    private void OnQuickAccessTabChanged(object? sender, EventArgs e) => EndQuickAccess();

    private void AllowQuickAccessContentHover()
    {
        foreach (DependencyObject item in Items.OfType<DependencyObject>())
        {
            RibbonPopupInteraction.SetSuppressHover(item, false);
            _quickAccessHoverContent.Add(item);
        }
    }

    private void RestoreQuickAccessHoverContent()
    {
        foreach (var item in _quickAccessHoverContent) item.ClearValue(RibbonPopupInteraction.SuppressHoverProperty);
        _quickAccessHoverContent.Clear();
    }
    private void OnQuickAccessUnloaded(object sender, RoutedEventArgs e)
    {
        EndQuickAccess();
        CancelQuickAccessTransitions();
    }

    private List<(FrameworkElement Surface, Point Position)> CaptureGroupPositions()
    {
        CancelQuickAccessTransitions();
        var positions = new List<(FrameworkElement, Point)>();
        if (VisualTreeHelper.GetParent(this) is not RibbonGroupsPanel panel || !IsVisible) return positions;
        foreach (FrameworkElement surface in panel.Children.OfType<FrameworkElement>())
            if (surface.IsVisible && surface.ActualWidth > 0)
                positions.Add((surface, surface.TranslatePoint(new Point(), panel)));
        return positions;
    }

    private void PlayCommittedGroupTransition(List<(FrameworkElement Surface, Point Position)> positions)
    {
        if (!RibbonAnimation.IsEnabled(RibbonAnimationAction.DropdownMenu)) return;
        for (DependencyObject? node = this; node is Visual visual; node = VisualTreeHelper.GetParent(visual))
            if (node is Ribbon { IsLayoutMotionChanging: true }) return;
        foreach (var (surface, position) in positions)
        {
            if (!surface.IsVisible || VisualTreeHelper.GetParent(surface) is not RibbonGroupsPanel panel
                || PopupDismissHelper.HasOpenPopupWithin(surface)) continue;
            Point final = surface.TranslatePoint(new Point(), panel);
            _quickAccessTransitions.Add(new RibbonDensityTransition(surface,
                action: RibbonAnimationAction.DropdownMenu, offsetX: position.X - final.X));
        }
    }

    private void CancelQuickAccessTransitions()
    {
        foreach (var transition in _quickAccessTransitions) transition.Dispose();
        _quickAccessTransitions.Clear();
    }
}
