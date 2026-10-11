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
internal sealed class PopupDismissHelper : IDisposable
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
    private Ribbon? _quickAccessRibbon;
    internal bool DismissedByOutsidePress { get; private set; }

    // Moving an open flyout's anchor with a render-only glide would leave its
    // native popup window at the final layout position until the next placement pass.
    internal static bool HasOpenPopupWithin(FrameworkElement surface)
    {
        var window = Window.GetWindow(surface);
        return window is not null && OpenStackByWindow.TryGetValue(window, out var stack)
            && stack.Any(helper => helper._getPopup()?.IsOpen == true
                && (ReferenceEquals(surface, helper._owner) || surface.IsAncestorOf(helper._owner)));
    }

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
        DismissedByOutsidePress = false;

        _window = Window.GetWindow(_owner);
        if (_window is null)
        {
            return;
        }

        _window.PreviewMouseDown += OnWindowPreviewMouseDown;
        _window.PreviewTouchDown += OnWindowPreviewTouchDown;
        _window.PreviewKeyDown += OnWindowPreviewKeyDown;
        _window.Deactivated += OnWindowDeactivated;
        _window.LocationChanged += OnWindowLocationChanged;
        _window.SizeChanged += OnWindowSizeChanged;

        List<PopupDismissHelper> stack = OpenStackByWindow.GetOrCreateValue(_window);
        stack.Remove(this);
        stack.Add(this);
        _quickAccessRibbon = RibbonPopupInteraction.FindQuickAccessRibbon(_owner);
        if (_quickAccessRibbon is not null)
            RibbonPopupInteraction.SetSuppressHover(_quickAccessRibbon, true);
        if (_getPopup()?.Child is { } child)
        {
            RibbonPopupInteraction.SetSuppressHover(child, false);
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(child, OnOutsideCapturedMouseDown);
        }
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
        window.PreviewTouchDown -= OnWindowPreviewTouchDown;
        window.PreviewKeyDown -= OnWindowPreviewKeyDown;
        window.Deactivated -= OnWindowDeactivated;
        window.LocationChanged -= OnWindowLocationChanged;
        window.SizeChanged -= OnWindowSizeChanged;

        if (OpenStackByWindow.TryGetValue(window, out List<PopupDismissHelper>? stack))
        {
            stack.Remove(this);
            if (_quickAccessRibbon is { } ribbon)
                RibbonPopupInteraction.SetSuppressHover(ribbon, stack.Any(helper => ReferenceEquals(helper._quickAccessRibbon, ribbon)));
        }

        if (_getPopup()?.Child is { } child)
            Mouse.RemovePreviewMouseDownOutsideCapturedElementHandler(child, OnOutsideCapturedMouseDown);
        _quickAccessRibbon = null;

        _window = null;
    }

    public void Dispose()
    {
        OnClosed();
        _owner.Unloaded -= OnOwnerUnloaded;
    }

    private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Source-owned popup content can route through the main window's logical
        // tree. Recognize the flyout and its nested openers before dismissing.
        if (e.OriginalSource is DependencyObject source && !IsInsideOwnerOrPopup(source))
        {
            DismissOutsidePress(e);
        }
    }

    private void OnWindowPreviewTouchDown(object? sender, TouchEventArgs e)
    {
        // Dismiss even when a host handles touch and suppresses mouse promotion.
        // Leave the event available to the touched control's own interaction.
        if (e.OriginalSource is DependencyObject source && !IsInsideOwnerOrPopup(source))
            DismissOutsidePress(e);
    }

    private void DismissOutsidePress(InputEventArgs e)
    {
        DismissedByOutsidePress = true;
        _close();
    }

    private void OnOutsideCapturedMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Release selector-owned capture before WPF routes the real press to its
        // hit-tested command. Do not consume it or synthesize another click.
        if (_window?.InputHitTest(Mouse.GetPosition(_window)) is DependencyObject hit
            && !IsInsideOwnerOrPopup(hit))
        {
            if (Mouse.Captured is DependencyObject captured && IsInsideOwnerOrPopup(captured)) Mouse.Capture(null);
            DismissOutsidePress(e);
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
        HashSet<DependencyObject>? anchors = null;
        while (true)
        {
            if (IsWithin(start, _owner) || IsWithin(start, _getPopup()?.Child)) return true;
            if (_window is null || !OpenStackByWindow.TryGetValue(_window, out var stack)) return false;
            // Source-owned content can route through the active ribbon's logical
            // tree. Follow an inner flyout's opener back to this outer flyout.
            var inner = stack.LastOrDefault(helper => !ReferenceEquals(helper, this)
                && helper._getPopup() is { IsOpen: true, Child: { } child } && IsWithin(start, child));
            if (inner is null || !(anchors ??= new()).Add(inner._owner)) return false;
            start = inner._owner;
        }
    }

    private static bool IsWithin(DependencyObject start, DependencyObject? target)
    {
        if (target is null) return false;
        DependencyObject? node = start;
        while (node is not null)
        {
            if (ReferenceEquals(node, target))
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
