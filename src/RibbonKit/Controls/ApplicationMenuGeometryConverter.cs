using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RibbonKit.Controls;

// Private template plumbing: token changes and layout both invalidate these bindings.
internal sealed class ApplicationMenuGeometryConverter : IMultiValueConverter
{
    // Ribbon supplies the viewport directly: menus declared in a Window's resources can
    // have an inheritance context that bypasses the ribbon during ancestor binding lookup.
    internal static readonly DependencyProperty AvailableWidthProperty = DependencyProperty.RegisterAttached(
        "AvailableWidth", typeof(double), typeof(ApplicationMenuGeometryConverter), new PropertyMetadata(0d));
    public static double GetAvailableWidth(DependencyObject target) => (double)target.GetValue(AvailableWidthProperty);
    public static void SetAvailableWidth(DependencyObject target, double value) => target.SetValue(AvailableWidthProperty, value);

    internal static readonly DependencyProperty AvailableHeightProperty = DependencyProperty.RegisterAttached(
        "AvailableHeight", typeof(double), typeof(ApplicationMenuGeometryConverter), new PropertyMetadata(double.PositiveInfinity));
    public static double GetAvailableHeight(DependencyObject target) => (double)target.GetValue(AvailableHeightProperty);
    public static void SetAvailableHeight(DependencyObject target, double value) => target.SetValue(AvailableHeightProperty, value);

    internal static readonly DependencyProperty PaneMinimumWidthProperty = DependencyProperty.RegisterAttached(
        "PaneMinimumWidth", typeof(double), typeof(ApplicationMenuGeometryConverter), new PropertyMetadata(0d));
    public static double GetPaneMinimumWidth(DependencyObject target) => (double)target.GetValue(PaneMinimumWidthProperty);
    public static void SetPaneMinimumWidth(DependencyObject target, double value) => target.SetValue(PaneMinimumWidthProperty, value);

    internal static readonly DependencyProperty PaneMaximumWidthProperty = DependencyProperty.RegisterAttached(
        "PaneMaximumWidth", typeof(double), typeof(ApplicationMenuGeometryConverter), new PropertyMetadata(0d));
    public static double GetPaneMaximumWidth(DependencyObject target) => (double)target.GetValue(PaneMaximumWidthProperty);
    public static void SetPaneMaximumWidth(DependencyObject target, double value) => target.SetValue(PaneMaximumWidthProperty, value);

    public static readonly DependencyProperty ViewportInsetProperty = DependencyProperty.RegisterAttached(
        "ViewportInset", typeof(double), typeof(ApplicationMenuGeometryConverter), new PropertyMetadata(0d));

    public static double GetViewportInset(DependencyObject target) => (double)target.GetValue(ViewportInsetProperty);
    public static void SetViewportInset(DependencyObject target, double value) => target.SetValue(ViewportInsetProperty, value);

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is "PaneWidth")
        {
            if (values[3] is not double maximum) return DependencyProperty.UnsetValue;
            if (values[0] is not double viewport || viewport <= 0 ||
                values[4] is not double inset || inset <= 0) return maximum;
            double nav = values[1] is double width ? width : 0;
            double minimum = values[2] is double min ? Math.Min(min, maximum) : 0;
            return Math.Clamp(viewport - nav - inset, minimum, maximum);
        }

        if (values[0] is not double w || values[1] is not double h ||
            values[2] is not CornerRadius corners || w <= 0 || h <= 0) return null;
        bool outside = parameter is "Outside";
        if (!outside && corners == new CornerRadius()) return null;
        if (parameter is "Inner")
            corners = new CornerRadius(Math.Max(0, corners.TopLeft - 1), Math.Max(0, corners.TopRight - 1),
                Math.Max(0, corners.BottomRight - 1), Math.Max(0, corners.BottomLeft - 1));
        Geometry silhouette = RoundedRectangle(w, h, corners);
        if (!outside) return silhouette;
        double blur = values[3] is double radius ? radius : 0;
        double depth = values[4] is double shadowDepth ? Math.Abs(shadowDepth) : 0;
        var halo = new Rect(0, 0, w, h);
        halo.Inflate(blur * 2 + depth, blur * 2 + depth);
        return new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(halo), silhouette);
    }

    private static Geometry RoundedRectangle(double width, double height, CornerRadius corners)
    {
        if (corners.TopLeft == corners.TopRight && corners.TopLeft == corners.BottomRight &&
            corners.TopLeft == corners.BottomLeft)
            return new RectangleGeometry(new Rect(0, 0, width, height), corners.TopLeft, corners.TopLeft);

        double tl = corners.TopLeft, tr = corners.TopRight, br = corners.BottomRight, bl = corners.BottomLeft;
        double scale = Math.Min(1, Math.Min(Math.Min(Ratio(width, tl + tr), Ratio(width, bl + br)),
            Math.Min(Ratio(height, tl + bl), Ratio(height, tr + br))));
        tl *= scale; tr *= scale; br *= scale; bl *= scale;
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(tl, 0), true, true);
            context.LineTo(new Point(width - tr, 0), true, false);
            Arc(new Point(width, tr), tr);
            context.LineTo(new Point(width, height - br), true, false);
            Arc(new Point(width - br, height), br);
            context.LineTo(new Point(bl, height), true, false);
            Arc(new Point(0, height - bl), bl);
            context.LineTo(new Point(0, tl), true, false);
            Arc(new Point(tl, 0), tl);
            void Arc(Point end, double radius)
            {
                if (radius == 0) context.LineTo(end, true, false);
                else context.ArcTo(end, new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
            }
        }
        geometry.Freeze();
        return geometry;
        static double Ratio(double extent, double sum) => sum == 0 ? 1 : extent / sum;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
