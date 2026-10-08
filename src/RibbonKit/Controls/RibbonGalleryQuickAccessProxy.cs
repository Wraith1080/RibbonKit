using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RibbonKit.Controls;

// Only the native presenter travels, as in InRibbonGallery's own expansion. The
// source keeps every item, container, template, binding and application handler.
// Each dropdown keeps its own viewport in its own Popup HWND.
internal sealed class RibbonGalleryQuickAccessProxy : RibbonDropDownButton
{
    private readonly RibbonGallery _source;
    private readonly GalleryViewport _viewport;
    private Popup? _popup;
    private bool _previewActive;
    private bool _returning;
    private bool _relayingSelection;
    private bool _mousePickPending;

    internal RibbonGalleryQuickAccessProxy(RibbonGallery source, Ribbon owner)
    {
        _source = source;
        _viewport = new GalleryViewport(source)
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            PanningMode = PanningMode.VerticalOnly,
            Focusable = false,
            MaxHeight = 300,
        };
        Loaded += (_, _) => SetBinding(FlowDirectionProperty, new Binding(nameof(FlowDirection)) { Source = owner });
        SetBinding(IconProperty, new Binding(nameof(source.Icon)) { Source = source });
        SetBinding(LargeIconProperty, new Binding(nameof(source.Icon)) { Source = source });
        SetBinding(HeaderProperty, new Binding(nameof(source.Header)) { Source = source });
        SetBinding(ScreenTipTitleProperty, new Binding(nameof(Header)) { Source = this });
        if (source is InRibbonGallery gallery)
            SetBinding(DropDownHeaderProperty, new Binding(nameof(gallery.DropDownHeader)) { Source = gallery });
        else SetBinding(DropDownHeaderProperty, new Binding(nameof(Header)) { Source = this });
        var width = new MultiBinding { Converter = WidthConverter.Instance };
        width.Bindings.Add(source is InRibbonGallery
            ? new Binding(nameof(InRibbonGallery.PopupWidth)) { Source = source }
            : new Binding { Source = double.NaN });
        width.Bindings.Add(new Binding(nameof(ActualWidth)) { Source = source });
        width.Bindings.Add(new Binding(nameof(Width)) { Source = source });
        _viewport.SetBinding(WidthProperty, width);
        Items.Add(_viewport);
        _viewport.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnItemMouseUp), true);
        _viewport.AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnItemMouseDown), true);
        _viewport.PreviewKeyDown += OnGalleryKeyDown;
        _viewport.MouseLeave += (_, _) => CancelPreview();
        _viewport.AddHandler(Selector.SelectedEvent, new RoutedEventHandler(OnTileSelection), true);
        _viewport.AddHandler(Selector.UnselectedEvent, new RoutedEventHandler(OnTileSelection), true);
        KeyboardNavigation.SetDirectionalNavigation(_viewport, KeyboardNavigationMode.Contained);
        Unloaded += (_, _) => Close();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == IsDropDownOpenProperty && _source is not null)
        {
            if (e.NewValue is true)
            {
                if (!_source.BeginQuickAccess(_viewport, Close))
                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(Close));
                else
                {
                    _source.ItemPreview += OnPreview;
                    _source.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnItemMouseUp), true);
                    _source.PreviewKeyDown += OnGalleryKeyDown;
                }
            }
            else ReturnPresenter();
        }
        base.OnPropertyChanged(e);
    }

    public override void OnApplyTemplate()
    {
        Close();
        if (_popup is not null) _popup.Opened -= OnPopupOpened;
        base.OnApplyTemplate();
        _popup = GetTemplateChild("PART_Popup") as Popup;
        if (_popup is not null) _popup.Opened += OnPopupOpened;
        // The gallery has its own native viewport; avoid a second scrolling surface.
        if (GetTemplateChild("PART_PopupScrollViewer") is ScrollViewer outer)
        {
            outer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            outer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }
    }

    internal override bool TryFocusInitialMenuItem(bool last)
    {
        var items = _source.Items.Cast<object>();
        if (last) items = items.Reverse();
        else if (_source.SelectedItem is { } selected) items = new[] { selected }.Concat(items);
        foreach (object item in items)
            if (_source.ItemContainerGenerator.ContainerFromItem(item) is RibbonGalleryItem { IsEnabled: true, IsVisible: true } tile && tile.Focus())
            {
                tile.BringIntoView();
                return true;
            }
        return false;
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        if (_popup?.Child is not { } child) return;
        for (DependencyObject? current = child; current is not null; current = VisualTreeHelper.GetParent(current))
            if (current is UIElement element) element.InvalidateMeasure();
        (PresentationSource.FromVisual(child)?.RootVisual as UIElement)?.UpdateLayout();
    }

    private void OnPreview(object sender, RibbonGalleryPreviewEventArgs e) => _previewActive = true;

    // A presenter hosted in another control's popup no longer has the original
    // selector on its visual event route. Relay native tile events to that selector
    // instead of duplicating its selection/binding logic in the dropdown.
    private void OnTileSelection(object sender, RoutedEventArgs e)
    {
        if (!IsDropDownOpen || _relayingSelection || e.OriginalSource is not RibbonGalleryItem tile
            || _source.ItemContainerGenerator.IndexFromContainer(tile) < 0) return;
        _relayingSelection = true;
        try { _source.RaiseEvent(new RoutedEventArgs(e.RoutedEvent, tile)); }
        finally { _relayingSelection = false; }
    }

    private void CancelPreview()
    {
        if (!_previewActive) return;
        _previewActive = false;
        _source.RaiseEvent(new RoutedEventArgs(RibbonGallery.ItemPreviewCancelledEvent, _source));
    }

    private void OnItemMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!IsDropDownOpen) return;
        DependencyObject? current = e.OriginalSource as DependencyObject;
        while (current is not null && current is not RibbonGalleryItem)
            current = current is Visual ? VisualTreeHelper.GetParent(current) : null;
        if (_mousePickPending || (current is RibbonGalleryItem { IsEnabled: true, IsSelected: true } tile
            && _source.ItemContainerGenerator.IndexFromContainer(tile) >= 0))
            // Finish the mouse-up/capture cycle before returning the presenter.
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(Close));
    }

    private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
    {
        DependencyObject? current = e.OriginalSource as DependencyObject;
        while (current is not null && current is not RibbonGalleryItem)
            current = current is Visual ? VisualTreeHelper.GetParent(current) : null;
        _mousePickPending = current is RibbonGalleryItem { IsEnabled: true } tile
            && _source.ItemContainerGenerator.IndexFromContainer(tile) >= 0;
    }

    private void OnGalleryKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsDropDownOpen || e.Handled) return;
        if (Keyboard.Modifiers == ModifierKeys.None && Keyboard.FocusedElement is RibbonGalleryItem current)
        {
            FocusNavigationDirection? direction = e.Key switch
            {
                Key.Left => FocusNavigationDirection.Left,
                Key.Right => FocusNavigationDirection.Right,
                Key.Up => FocusNavigationDirection.Up,
                Key.Down => FocusNavigationDirection.Down,
                Key.Home => FocusNavigationDirection.First,
                Key.End => FocusNavigationDirection.Last,
                _ => null,
            };
            if (direction is { } move)
            {
                e.Handled = true;
                (move is FocusNavigationDirection.First or FocusNavigationDirection.Last ? _viewport : (FrameworkElement)current)
                    .MoveFocus(new TraversalRequest(move));
                if (Keyboard.FocusedElement is RibbonGalleryItem selected
                    && _source.ItemContainerGenerator.IndexFromContainer(selected) >= 0)
                {
                    selected.SetCurrentValue(ListBoxItem.IsSelectedProperty, true);
                    selected.BringIntoView();
                }
                return;
            }
        }
        if (e.Key == Key.Escape || (e.Key is Key.Enter or Key.Space && Keyboard.FocusedElement is RibbonGalleryItem))
        {
            e.Handled = true;
            if (e.Key != Key.Escape && Keyboard.FocusedElement is RibbonGalleryItem pick)
                pick.SetCurrentValue(ListBoxItem.IsSelectedProperty, true);
            Close();
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                (GetTemplateChild("PART_Toggle") as UIElement)?.Focus()));
        }
    }

    private void Close() => SetCurrentValue(IsDropDownOpenProperty, false);
    private void ReturnPresenter()
    {
        if (_returning) return;
        _returning = true;
        try
        {
            _source.ItemPreview -= OnPreview;
            _source.RemoveHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnItemMouseUp));
            _source.PreviewKeyDown -= OnGalleryKeyDown;
            _mousePickPending = false;
            if (Mouse.Captured is DependencyObject captured
                && (ReferenceEquals(captured, _source) || _viewport.IsAncestorOf(captured)))
                Mouse.Capture(null);
            _source.EndQuickAccess(_viewport);
            CancelPreview();
        }
        finally { _returning = false; }
    }

    private sealed class GalleryViewport(RibbonGallery source) : ScrollViewer
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new GalleryViewportPeer(this, source);
    }

    private sealed class GalleryViewportPeer(GalleryViewport owner, RibbonGallery source) : ScrollViewerAutomationPeer(owner)
    {
        private AutomationPeer SourcePeer => UIElementAutomationPeer.CreatePeerForElement(source);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
        protected override string GetNameCore() => source.Header ?? base.GetNameCore();
        protected override List<AutomationPeer> GetChildrenCore() => SourcePeer.GetChildren() ?? new();
        public override object GetPattern(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.Selection ? SourcePeer.GetPattern(patternInterface) : base.GetPattern(patternInterface);
    }

    private sealed class WidthConverter : IMultiValueConverter
    {
        internal static readonly WidthConverter Instance = new();
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            values[0] is double popup && double.IsFinite(popup) ? Math.Max(1, popup - 12)
            : values[1] is double actual && actual > 0 ? actual
            : values[2] is double width && double.IsFinite(width) && width > 0 ? width : 320d;
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
