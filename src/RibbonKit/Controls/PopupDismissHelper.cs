using System.Runtime.CompilerServices;
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
    // Several StaysOpen flyouts can be nested on the same owner window (most visibly a
    // drop-down inside the QAT overflow). Routed-event handler order follows subscription order,
    // so without an explicit stack the older OUTER helper sees Escape first, closes its host and
    // marks the event handled before the newer INNER helper can close its own popup. The inner
    // StaysOpen popup may then outlive its unloaded owner and leave this handler subscribed,
    // swallowing Escape for every later flyout until the process exits.
    private static readonly ConditionalWeakTable<Window, List<PopupDismissHelper>> OpenStackByWindow = new();

    private readonly FrameworkElement _owner;
    private readonly Func<Popup?> _getPopup;
    private readonly Action _close;
    private Window? _window;

    public PopupDismissHelper(FrameworkElement owner, Func<Popup?> getPopup, Action close)
    {
        _owner = owner;
        _getPopup = getPopup;
        _close = close;
        _owner.Unloaded += OnOwnerUnloaded;
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

        List<PopupDismissHelper> stack = OpenStackByWindow.GetOrCreateValue(_window);
        stack.Remove(this);
        stack.Add(this);
    }

    /// <summary>Call from the popup's Closed event.</summary>
    public void OnClosed()
    {
        if (_window is null)
        {
            return;
        }

        Window window = _window;
        window.PreviewMouseDown -= OnWindowPreviewMouseDown;
        window.PreviewKeyDown -= OnWindowPreviewKeyDown;
        window.Deactivated -= OnWindowDeactivated;
        window.LocationChanged -= OnWindowLocationChanged;
        window.SizeChanged -= OnWindowSizeChanged;

        if (OpenStackByWindow.TryGetValue(window, out List<PopupDismissHelper>? stack))
        {
            stack.Remove(this);
        }

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
        if (e.Key == Key.Escape && TryDismissTopmostForEscape())
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Closes this flyout only when it is the newest open flyout on its owner window.
    /// Older helpers deliberately leave Escape unhandled so the routed event can reach the
    /// nested helper that belongs to the visually topmost popup.
    /// </summary>
    internal bool TryDismissTopmostForEscape()
    {
        if (_window is null
            || !OpenStackByWindow.TryGetValue(_window, out List<PopupDismissHelper>? stack)
            || stack.Count == 0
            || !ReferenceEquals(stack[^1], this))
        {
            return false;
        }

        _close();
        return true;
    }

    private void OnWindowDeactivated(object? sender, EventArgs e) => _close();

    private void OnWindowLocationChanged(object? sender, EventArgs e) => _close();

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e) => _close();

    private void OnOwnerUnloaded(object sender, RoutedEventArgs e)
    {
        if (_window is null)
        {
            return;
        }

        // A host popup can unload this owner before the nested Popup raises Closed. Ask the
        // surface to close, but unregister in finally so even a coerced/no-op close cannot leave
        // a PreviewKeyDown handler swallowing Escape for the rest of the application lifetime.
        try
        {
            _close();
        }
        finally
        {
            OnClosed();
        }
    }

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
