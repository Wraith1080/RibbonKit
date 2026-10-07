using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RibbonKit.Controls;

// WPF owns keyboard-only focus display. Its template opts into placement refresh
// while loaded so render-only movement cannot strand the native focus adorner.
internal static class FocusVisualTracking
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(FocusVisualTracking), new PropertyMetadata(false, OnEnabledChanged));

    // Shared templates opt accent-filled surfaces into contrast correction. This is
    // deliberately local: page content and host controls keep their own focus styles.
    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.RegisterAttached(
        "Background", typeof(Brush), typeof(FocusVisualTracking), new PropertyMetadata(null));

    private static readonly DependencyProperty TrackerProperty = DependencyProperty.RegisterAttached(
        "Tracker", typeof(Tracker), typeof(FocusVisualTracking));

    public static bool GetEnabled(DependencyObject target) => (bool)target.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject target, bool value) => target.SetValue(EnabledProperty, value);
    public static Brush? GetBackground(DependencyObject target) => (Brush?)target.GetValue(BackgroundProperty);
    public static void SetBackground(DependencyObject target, Brush? value) => target.SetValue(BackgroundProperty, value);

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
        private readonly MatrixTransform _surfaceTransform = new();
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
                _element.LayoutUpdated += OnLayoutUpdated;
                CompositionTarget.Rendering += OnRendering;
                UpdateSurface();
                break;
            }
        }

        private void Stop()
        {
            CompositionTarget.Rendering -= OnRendering;
            _element.LayoutUpdated -= OnLayoutUpdated;
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
            if (_lastPlacement != placement)
            {
                _lastPlacement = placement;
                layer.Update(target);
            }
            UpdateSurface();
        }

        private void OnLayoutUpdated(object? sender, EventArgs e) => UpdateSurface();

        private void UpdateSurface()
        {
            if (_adorner is not { AdornedElement: FrameworkElement target } adorner
                || VisualTreeHelper.GetParent(adorner) is not AdornerLayer layer
                || VisualTreeHelper.GetParent(layer) is not Visual root
                || !root.IsAncestorOf(target) || !target.IsVisible
                || _element.Parent is not Canvas canvas) return;

            // Shared templates name their painted border Chrome. Measure that surface,
            // not the control's larger layout/hit-test slot. Unmarked host templates
            // keep the ordinary control-bounds fallback; no host helper is required.
            FrameworkElement surface = target;
            if (_element is Border && target is Control control
                && (control.Template?.FindName("Chrome", control)
                    ?? control.Template?.FindName("HeaderChrome", control)) is Border { IsVisible: true } chrome)
                surface = chrome;
            else if (_element is Ellipse)
                surface = FindOrb(target) ?? target;

            UpdateBrush(target, surface);

            const double inset = 2;
            double width = Math.Max(0, surface.RenderSize.Width - inset * 2);
            double height = Math.Max(0, surface.RenderSize.Height - inset * 2);
            if (_element.Width != width) _element.Width = width;
            if (_element.Height != height) _element.Height = height;
            if (_element is Border ring)
            {
                var radius = surface is Border border ? border.CornerRadius
                    : target.TryFindResource("RibbonKit.Metrics.ControlCornerRadius") is CornerRadius fallback
                        ? fallback : new CornerRadius();
                // An inset contour has correspondingly smaller radii. Square corners
                // stay square, and unequal corners stay attached to their original side.
                if (surface is Border)
                    radius = new CornerRadius(Math.Max(0, radius.TopLeft - inset),
                        Math.Max(0, radius.TopRight - inset), Math.Max(0, radius.BottomRight - inset),
                        Math.Max(0, radius.BottomLeft - inset));
                if (ring.CornerRadius != radius) ring.SetCurrentValue(Border.CornerRadiusProperty, radius);
            }

            // Map the full basis into the focus template's LTR canvas. This includes
            // template margins, alignment, native adorner mirroring and surface transforms.
            // Absolute vector placement avoids a second independent layout rounding pass.
            GeneralTransform transform = surface.TransformToVisual(canvas);
            Point origin = transform.Transform(new Point(inset, inset));
            Point x = transform.Transform(new Point(inset + 1, inset));
            Point y = transform.Transform(new Point(inset, inset + 1));
            var matrix = new Matrix(x.X - origin.X, x.Y - origin.Y,
                y.X - origin.X, y.Y - origin.Y, origin.X, origin.Y);
            if (_surfaceTransform.Matrix != matrix) _surfaceTransform.Matrix = matrix;
            if (!ReferenceEquals(_element.RenderTransform, _surfaceTransform))
                _element.RenderTransform = _surfaceTransform;
        }

        private void UpdateBrush(FrameworkElement target, FrameworkElement surface)
        {
            if (target.TryFindResource("RibbonKit.Brushes.Input.FocusBorder") is not Brush brush) return;
            if (brush is SolidColorBrush normal
                && GetBackground(target) is SolidColorBrush { Color.A: 255, Opacity: 1 } band)
            {
                Color background = band.Color;
                if (surface is Border { Background: SolidColorBrush fill })
                {
                    double alpha = fill.Color.A / 255d * fill.Opacity;
                    background = Color.FromRgb(Composite(fill.Color.R, background.R, alpha),
                        Composite(fill.Color.G, background.G, alpha), Composite(fill.Color.B, background.B, alpha));
                }
                if (Contrast(normal.Color, background) < 3
                    && target.TryFindResource("RibbonKit.Brushes.KeyboardFocus.Light") is SolidColorBrush light
                    && target.TryFindResource("RibbonKit.Brushes.KeyboardFocus.Dark") is SolidColorBrush dark)
                    brush = Contrast(light.Color, background) >= Contrast(dark.Color, background) ? light : dark;
            }

            if (_element is Border ring && !ReferenceEquals(ring.BorderBrush, brush))
                ring.SetCurrentValue(Border.BorderBrushProperty, brush);
            else if (_element is Ellipse circle && !ReferenceEquals(circle.Stroke, brush))
                circle.SetCurrentValue(Shape.StrokeProperty, brush);

            static byte Composite(byte foreground, byte background, double alpha) =>
                (byte)Math.Round(foreground * alpha + background * (1 - alpha));
        }

        private static double Contrast(Color a, Color b)
        {
            double x = Luminance(a), y = Luminance(b);
            return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
            static double Luminance(Color color) =>
                0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
            static double Linear(byte value)
            {
                double channel = value / 255d;
                return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
            }
        }

        private static Ellipse? FindOrb(DependencyObject node)
        {
            if (node is Ellipse { Name: "OrbFill", IsVisible: true } orb) return orb;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
                if (FindOrb(VisualTreeHelper.GetChild(node, i)) is { } found) return found;
            return null;
        }
    }
}
