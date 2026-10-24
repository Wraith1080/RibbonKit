using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace RibbonKit.Controls;

// Shared by QAT and Simplified overflow; the template owns all presentation.
internal sealed class OverflowMenuScroller : IDisposable
{
    private readonly ScrollViewer _viewer;
    private readonly ItemsControl _items;
    private readonly DispatcherTimer _hoverTimer;
    private RepeatButton? _up;
    private RepeatButton? _down;
    private RepeatButton? _hover;
    private double _authoredMaxHeight;
    private bool _open;

    internal OverflowMenuScroller(ScrollViewer viewer, ItemsControl items)
    {
        _viewer = viewer;
        _items = items;
        _hoverTimer = new DispatcherTimer(DispatcherPriority.Input, viewer.Dispatcher);
        _hoverTimer.Tick += OnHoverTick;
        viewer.Loaded += OnLoaded;
        viewer.Unloaded += OnUnloaded;
        viewer.IsVisibleChanged += OnVisibilityChanged;
        viewer.ScrollChanged += OnScrollChanged;
        items.AddHandler(Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnFocus));
    }

    internal void Open(FrameworkElement anchor, FrameworkElement? surface)
    {
        if (!_open) _authoredMaxHeight = _viewer.MaxHeight;
        _open = true;
        double inset = 16; // Keep native popup shadows clear of the work-area edges.
        if (surface is Border border)
            inset += border.Margin.Top + border.Margin.Bottom + border.Padding.Top + border.Padding.Bottom
                + border.BorderThickness.Top + border.BorderThickness.Bottom;
        double height = RibbonOverflowMenu.WorkAreaHeight(anchor) - inset;
        _viewer.SetCurrentValue(FrameworkElement.MaxHeightProperty, Math.Min(_authoredMaxHeight, Math.Max(1, height)));
        _viewer.ScrollToTop();
        AttachButtons();
    }

    internal void Close()
    {
        StopHover();
        if (!_open) return;
        _open = false;
        _viewer.SetCurrentValue(FrameworkElement.MaxHeightProperty, _authoredMaxHeight);
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => AttachButtons();
    private void OnUnloaded(object sender, RoutedEventArgs e) => Close();
    private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!_viewer.IsVisible) StopHover();
    }

    private void AttachButtons()
    {
        _viewer.ApplyTemplate();
        var up = _viewer.Template?.FindName("PART_ScrollUp", _viewer) as RepeatButton;
        var down = _viewer.Template?.FindName("PART_ScrollDown", _viewer) as RepeatButton;
        if (!ReferenceEquals(up, _up) || !ReferenceEquals(down, _down))
        {
            StopHover();
            Connect(_up, false); Connect(_down, false);
            _up = up; _down = down;
            Connect(_up, true); Connect(_down, true);
        }
        UpdateButtons();
    }

    private void Connect(RepeatButton? button, bool attach)
    {
        if (button is null) return;
        if (attach)
        {
            button.Click += OnClick;
            button.MouseEnter += OnEnter;
            button.MouseLeave += OnLeave;
            button.PreviewMouseLeftButtonDown += OnPress;
            button.PreviewMouseLeftButtonUp += OnRelease;
        }
        else
        {
            button.Click -= OnClick;
            button.MouseEnter -= OnEnter;
            button.MouseLeave -= OnLeave;
            button.PreviewMouseLeftButtonDown -= OnPress;
            button.PreviewMouseLeftButtonUp -= OnRelease;
        }
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e) => UpdateButtons();

    private void UpdateButtons()
    {
        // Compare with the entire allocation, not the band-reduced viewport:
        // removing commands must also remove bands when everything fits again.
        bool overflow = _viewer.ExtentHeight > _viewer.ActualHeight + .5;
        if (_up is not null)
        {
            _up.Visibility = overflow ? Visibility.Visible : Visibility.Collapsed;
            _up.IsEnabled = _viewer.VerticalOffset > .5;
        }
        if (_down is not null)
        {
            _down.Visibility = overflow ? Visibility.Visible : Visibility.Collapsed;
            _down.IsEnabled = _viewer.VerticalOffset < _viewer.ScrollableHeight - .5;
        }
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        Scroll(ReferenceEquals(sender, _down));
        e.Handled = true; // Scrolling is never a command invocation/dismissal.
    }

    private bool Scroll(bool down)
    {
        if (!_open || !_viewer.IsVisible || PopupDismissHelper.HasOpenPopupWithin(_items)) return false;
        double offset = _viewer.VerticalOffset;
        var starts = _items.Items.OfType<FrameworkElement>()
            .Where(item => item.IsVisible).Select(item => item.TranslatePoint(new Point(), _items).Y);
        double next = down ? starts.Where(y => y > offset + .5).DefaultIfEmpty(_viewer.ScrollableHeight).Min()
            : starts.Where(y => y < offset - .5).DefaultIfEmpty(0).Max();
        _viewer.ScrollToVerticalOffset(Math.Clamp(next, 0, _viewer.ScrollableHeight));
        return true;
    }

    private void OnFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!_open || e.NewFocus is not System.Windows.Media.Visual focused) return;
        foreach (var item in _items.Items.OfType<FrameworkElement>())
            if (ReferenceEquals(item, focused) || item.IsAncestorOf(focused))
            {
                item.BringIntoView();
                break;
            }
    }

    private void OnEnter(object sender, MouseEventArgs e) => StartHover((RepeatButton)sender);
    private void OnLeave(object sender, MouseEventArgs e) => StopHover();
    private void OnPress(object sender, MouseButtonEventArgs e) => StopHover();
    private void OnRelease(object sender, MouseButtonEventArgs e)
    {
        if (sender is RepeatButton { IsMouseOver: true } button) StartHover(button);
    }

    private void StartHover(RepeatButton button)
    {
        StopHover();
        if (!button.IsEnabled || !_open) return;
        _hover = button;
        _hoverTimer.Interval = TimeSpan.FromMilliseconds(350);
        _hoverTimer.Start();
    }

    private void OnHoverTick(object? sender, EventArgs e)
    {
        if (_hover is not { IsEnabled: true } || !Scroll(ReferenceEquals(_hover, _down)))
        {
            StopHover();
            return;
        }
        _hoverTimer.Interval = TimeSpan.FromMilliseconds(90);
    }

    private void StopHover() { _hoverTimer.Stop(); _hover = null; }

    public void Dispose()
    {
        Close();
        Connect(_up, false); Connect(_down, false);
        _hoverTimer.Tick -= OnHoverTick;
        _viewer.Loaded -= OnLoaded; _viewer.Unloaded -= OnUnloaded;
        _viewer.IsVisibleChanged -= OnVisibilityChanged; _viewer.ScrollChanged -= OnScrollChanged;
        _items.RemoveHandler(Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnFocus));
    }
}
