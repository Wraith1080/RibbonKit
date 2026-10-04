using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace RibbonKit.Controls;

// WPF owns keyboard-only focus display. Its template opts into placement refresh
// while loaded so render-only movement cannot strand the native focus adorner.
internal static class FocusVisualTracking
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(FocusVisualTracking), new PropertyMetadata(false, OnEnabledChanged));

    private static readonly DependencyProperty TrackerProperty = DependencyProperty.RegisterAttached(
        "Tracker", typeof(Tracker), typeof(FocusVisualTracking));

    public static bool GetEnabled(DependencyObject target) => (bool)target.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject target, bool value) => target.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not FrameworkElement element) return;
        if ((bool)e.NewValue)
            element.SetValue(TrackerProperty, new Tracker(element));
        else
        {
            (element.GetValue(TrackerProperty) as Tracker)?.Detach();
            element.ClearValue(TrackerProperty);
        }
    }

    private sealed class Tracker
    {
        private readonly FrameworkElement _element;
        private Adorner? _adorner;
        private (Point Origin, Point X, Point Y, Size Size)? _lastPlacement;

        internal Tracker(FrameworkElement element)
        {
            _element = element;
            element.Loaded += OnLoaded;
            element.Unloaded += OnUnloaded;
            if (element.IsLoaded) Start();
        }

        internal void Detach()
        {
            _element.Loaded -= OnLoaded;
            _element.Unloaded -= OnUnloaded;
            Stop();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => Start();
        private void OnUnloaded(object sender, RoutedEventArgs e) => Stop();

        private void Start()
        {
            Stop();
            for (DependencyObject? node = _element; node is not null; node = VisualTreeHelper.GetParent(node))
            {
                if (node is not Adorner adorner) continue;
                _adorner = adorner;
                CompositionTarget.Rendering += OnRendering;
                break;
            }
        }

        private void Stop()
        {
            CompositionTarget.Rendering -= OnRendering;
            _adorner = null;
            _lastPlacement = null;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (_adorner is not { } adorner
                || VisualTreeHelper.GetParent(adorner) is not AdornerLayer layer
                || !adorner.AdornedElement.IsVisible
                || VisualTreeHelper.GetParent(layer) is not Visual root
                || !root.IsAncestorOf(adorner.AdornedElement)) return;

            // Match KeyTip placement tracking: a full basis preserves RTL and scale,
            // and an unchanged frame needs no layout work. Unloading releases the tick.
            var target = adorner.AdornedElement;
            GeneralTransform transform = target.TransformToVisual(layer);
            var placement = (transform.Transform(new Point(0, 0)),
                transform.Transform(new Point(1, 0)), transform.Transform(new Point(0, 1)), target.RenderSize);
            if (_lastPlacement == placement) return;
            _lastPlacement = placement;
            layer.Update(target);
        }
    }
}
