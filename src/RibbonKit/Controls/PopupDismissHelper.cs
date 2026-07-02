using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RibbonKit.Controls;

/// <summary>
/// Explicit light-dismiss for RibbonKit flyouts. RibbonKit popups use
/// <c>StaysOpen=True</c> so that WPF's popup mouse-capture (the source of endless
/// close/reopen races on the opener button) never engages. This helper supplies the
/// dismissal instead: while a flyout is open, any mouse press outside the owner and
/// popup, window deactivation, window move/resize, or Esc closes it.
/// </summary>
internal sealed class PopupDismissHelper
{
    private readonly FrameworkElement _owner;
    private readonly Func<Popup?> _getPopup;
    private readonly Action _close;
    private Window? _window;

    public PopupDismissHelper(FrameworkElement owner, Func<Popup?> getPopup, Action close)
    {
        _owner = owner;
        _getPopup = getPopup;
        _close = close;
    }

    /// <summary>Call from the popup's Opened event.</summary>
    public void OnOpened()
    {
        OnClosed(); // Defensive: never double-subscribe.

        _window = Window.GetWindow(_owner);
        if (_window is null)
        {
            return;
        }

        _window.PreviewMouseDown += OnWindowPreviewMouseDown;
        _window.PreviewKeyDown += OnWindowPreviewKeyDown;
        _window.Deactivated += OnWindowDeactivated;
        _window.LocationChanged += OnWindowLocationChanged;
        _window.SizeChanged += OnWindowSizeChanged;
    }

    /// <summary>Call from the popup's Closed event.</summary>
    public void OnClosed()
    {
        if (_window is null)
        {
            return;
        }

        _window.PreviewMouseDown -= OnWindowPreviewMouseDown;
        _window.PreviewKeyDown -= OnWindowPreviewKeyDown;
        _window.Deactivated -= OnWindowDeactivated;
        _window.LocationChanged -= OnWindowLocationChanged;
        _window.SizeChanged -= OnWindowSizeChanged;
        _window = null;
    }

    private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Clicks INSIDE the popup arrive on the popup's own window, not here — so any
        // press we see is within the main window. Close unless it is on the owner
        // (whose own toggle click handles open/close) or, defensively, in the popup.
        if (e.OriginalSource is DependencyObject source && !IsInsideOwnerOrPopup(source))
        {
            _close();
        }
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _close();
            e.Handled = true;
        }
    }

    private void OnWindowDeactivated(object? sender, EventArgs e) => _close();

    private void OnWindowLocationChanged(object? sender, EventArgs e) => _close();

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e) => _close();

    private bool IsInsideOwnerOrPopup(DependencyObject start)
    {
        UIElement? popupChild = _getPopup()?.Child;

        DependencyObject? node = start;
        while (node is not null)
        {
            if (ReferenceEquals(node, _owner) || ReferenceEquals(node, popupChild))
            {
                return true;
            }

            node = GetParent(node);
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject node) =>
        node is Visual or Visual3D
            ? VisualTreeHelper.GetParent(node) ?? LogicalTreeHelper.GetParent(node)
            : LogicalTreeHelper.GetParent(node);
}
