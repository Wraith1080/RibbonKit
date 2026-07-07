using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RibbonKit.Animation;

namespace RibbonKit.Controls;

/// <summary>
/// A gallery that lives directly in the ribbon: a compact strip of tiles with
/// scroll-up / scroll-down / expand buttons on its right edge. The expand button
/// opens an overlay popup showing the full wrapped gallery, positioned over the
/// strip like Office.
/// </summary>
/// <remarks>
/// The strip and the popup share ONE items presenter, re-homed between them while
/// the popup is open (the same pattern as collapsed <see cref="RibbonGroup"/>
/// flyouts). Light-dismiss comes from <see cref="PopupDismissHelper"/> — the popup
/// itself never takes mouse capture.
/// </remarks>
[TemplatePart(Name = ContentHostPartName, Type = typeof(Decorator))]
[TemplatePart(Name = PopupHostPartName, Type = typeof(Border))]
[TemplatePart(Name = PopupPartName, Type = typeof(Popup))]
[TemplatePart(Name = ScrollViewerPartName, Type = typeof(ScrollViewer))]
[TemplatePart(Name = LineUpPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = LineDownPartName, Type = typeof(ButtonBase))]
public class InRibbonGallery : RibbonGallery
{
    private const string ContentHostPartName = "PART_ContentHost";
    private const string PopupHostPartName = "PART_PopupHost";
    private const string PopupPartName = "PART_Popup";
    private const string ScrollViewerPartName = "PART_ScrollViewer";
    private const string LineUpPartName = "PART_LineUp";
    private const string LineDownPartName = "PART_LineDown";

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(
            nameof(IsDropDownOpen),
            typeof(bool),
            typeof(InRibbonGallery),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsDropDownOpenChanged));

    private readonly PopupDismissHelper _dismissHelper;
    private Decorator? _contentHost;
    private Border? _popupHost;
    private Popup? _popup;
    private ScrollViewer? _scrollViewer;
    private ButtonBase? _lineUp;
    private ButtonBase? _lineDown;

    static InRibbonGallery()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(InRibbonGallery),
            new FrameworkPropertyMetadata(typeof(InRibbonGallery)));
    }

    /// <summary>Initializes the gallery and its light-dismiss plumbing.</summary>
    public InRibbonGallery()
    {
        _dismissHelper = new PopupDismissHelper(
            this,
            () => _popup,
            () => SetCurrentValue(IsDropDownOpenProperty, false));

        // Self-heal: if the gallery is pulled out of the tree while expanded (e.g.
        // its host group flyout closes and re-homes its content), close the popup so
        // the items presenter returns to the strip instead of staying orphaned.
        Unloaded += OnGalleryUnloaded;
    }

    private void OnGalleryUnloaded(object sender, RoutedEventArgs e)
    {
        if (IsDropDownOpen)
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
        }
    }

    /// <summary>Whether the expanded gallery popup is open.</summary>
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_lineUp is not null)
        {
            _lineUp.Click -= OnLineUpClick;
        }

        if (_lineDown is not null)
        {
            _lineDown.Click -= OnLineDownClick;
        }

        base.OnApplyTemplate();

        _contentHost = GetTemplateChild(ContentHostPartName) as Decorator;
        _popupHost = GetTemplateChild(PopupHostPartName) as Border;
        _popup = GetTemplateChild(PopupPartName) as Popup;
        _scrollViewer = GetTemplateChild(ScrollViewerPartName) as ScrollViewer;
        _lineUp = GetTemplateChild(LineUpPartName) as ButtonBase;
        _lineDown = GetTemplateChild(LineDownPartName) as ButtonBase;

        if (_lineUp is not null)
        {
            _lineUp.Click += OnLineUpClick;
        }

        if (_lineDown is not null)
        {
            _lineDown.Click += OnLineDownClick;
        }
    }

    private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((InRibbonGallery)d).HandleDropDownStateChanged((bool)e.NewValue);
    }

    /// <summary>
    /// Re-homing is driven by the PROPERTY change, never by Popup.Opened/Closed:
    /// when this gallery sits inside another popup (a collapsed group's flyout) and
    /// that outer popup closes, WPF tears the inner popup down asynchronously and
    /// its Closed event cannot be relied on — the items presenter would stay
    /// orphaned in the popup and the strip would render empty.
    /// </summary>
    private void HandleDropDownStateChanged(bool open)
    {
        if (open)
        {
            _dismissHelper.OnOpened();

            // Move the shared items presenter from the strip into the popup and show
            // a scrollbar there (the strip keeps it hidden).
            if (_contentHost?.Child is { } content && _popupHost is not null)
            {
                _contentHost.Child = null;
                _popupHost.Child = content;
            }

            _scrollViewer?.SetCurrentValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);

            // Fade + slide the expanded gallery in (honors the global animation level).
            RibbonMotion.PlayOpen(_popupHost, RibbonAnimationAction.Gallery);
        }
        else
        {
            _dismissHelper.OnClosed();

            if (_popupHost?.Child is { } content && _contentHost is not null)
            {
                _popupHost.Child = null;
                _contentHost.Child = content;
            }

            _scrollViewer?.SetCurrentValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
        }
    }

    private void OnLineUpClick(object sender, RoutedEventArgs e) => _scrollViewer?.PageUp();

    private void OnLineDownClick(object sender, RoutedEventArgs e) => _scrollViewer?.PageDown();
}
