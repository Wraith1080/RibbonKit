using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RibbonKit.Controls;

// Private template plumbing. The top rim uses the same side thickness as the rows;
// only the content is clipped, with Border's half-stroke corner adjustment.
internal sealed class MessageBarGeometryConverter : IMultiValueConverter
{
    public static readonly DependencyProperty RowBorderThicknessProperty = DependencyProperty.RegisterAttached(
        "RowBorderThickness", typeof(Thickness), typeof(MessageBarGeometryConverter), new PropertyMetadata(new Thickness()));
    public static Thickness GetRowBorderThickness(DependencyObject target) => (Thickness)target.GetValue(RowBorderThicknessProperty);
    public static void SetRowBorderThickness(DependencyObject target, Thickness value) => target.SetValue(RowBorderThicknessProperty, value);

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[2] is not CornerRadius corners || values[3] is not Thickness inset)
            return DependencyProperty.UnsetValue;
        var sides = values[4] is Thickness thickness ? thickness : new Thickness();
        bool rounded = corners.TopLeft > 0 || corners.TopRight > 0;
        var rim = rounded ? new Thickness(sides.Left, inset.Top, sides.Right, 0) : inset;
        if (parameter is "Thickness") return rim;
        if (!rounded || values[0] is not double width || values[1] is not double height
            || width <= 0 || height <= 0) return null;

        if (values[5] is FrameworkElement { UseLayoutRounding: true } element)
        {
            DpiScale dpi = VisualTreeHelper.GetDpi(element);
            rim = Round(rim); inset = Round(inset);
            Thickness Round(Thickness value) => new(
                Math.Round(value.Left * dpi.DpiScaleX) / dpi.DpiScaleX,
                Math.Round(value.Top * dpi.DpiScaleY) / dpi.DpiScaleY,
                Math.Round(value.Right * dpi.DpiScaleX) / dpi.DpiScaleX,
                Math.Round(value.Bottom * dpi.DpiScaleY) / dpi.DpiScaleY);
        }

        // Cover the curved corners and stop before the first row's straight side borders.
        double band = Math.Ceiling(Math.Max(corners.TopLeft, corners.TopRight)
            + Math.Max(Math.Max(rim.Left, rim.Right), rim.Top) / 2) + 1;
        if (parameter is "Rim")
            return new RectangleGeometry(new Rect(0, 0, width, Math.Min(height, band)));

        width += inset.Left + inset.Right;
        height += inset.Top + inset.Bottom;
        var inner = TopRoundedRectangle(new Rect(rim.Left, rim.Top,
                Math.Max(0, width - rim.Left - rim.Right), Math.Max(0, height - rim.Top)),
            new Size(Math.Max(0, corners.TopLeft - rim.Left / 2), Math.Max(0, corners.TopLeft - rim.Top / 2)),
            new Size(Math.Max(0, corners.TopRight - rim.Right / 2), Math.Max(0, corners.TopRight - rim.Top / 2)));
        var lower = new RectangleGeometry(new Rect(0, Math.Min(height, band), width, Math.Max(0, height - band)));
        var clip = new CombinedGeometry(GeometryCombineMode.Union, inner, lower)
        { Transform = new TranslateTransform(-inset.Left, -inset.Top) };
        clip.Freeze();
        return clip;
    }

    private static Geometry TopRoundedRectangle(Rect bounds, Size left, Size right)
    {
        var geometry = new StreamGeometry();
        double scale = left.Width + right.Width > bounds.Width ? bounds.Width / (left.Width + right.Width) : 1;
        left = new Size(left.Width * scale, Math.Min(left.Height, bounds.Height));
        right = new Size(right.Width * scale, Math.Min(right.Height, bounds.Height));
        using (var drawing = geometry.Open())
        {
            drawing.BeginFigure(new Point(bounds.Left + left.Width, bounds.Top), true, true);
            drawing.LineTo(new Point(bounds.Right - right.Width, bounds.Top), true, false);
            Arc(new Point(bounds.Right, bounds.Top + right.Height), right);
            drawing.LineTo(bounds.BottomRight, true, false);
            drawing.LineTo(bounds.BottomLeft, true, false);
            drawing.LineTo(new Point(bounds.Left, bounds.Top + left.Height), true, false);
            Arc(new Point(bounds.Left + left.Width, bounds.Top), left);
            void Arc(Point point, Size radius)
            {
                if (radius.Width <= 0 || radius.Height <= 0) drawing.LineTo(point, true, false);
                else drawing.ArcTo(point, radius, 0, false, SweepDirection.Clockwise, true, false);
            }
        }
        geometry.Freeze();
        return geometry;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
