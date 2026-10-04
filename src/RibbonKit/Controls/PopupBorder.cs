using System.Windows;
using System.Windows.Media;

namespace RibbonKit.Controls;

// FrameworkElement rounds margins during measure/arrange but uses their stored
// values again for layout clipping. Keep both on the same pixel grid so a
// fractional popup margin cannot produce a full-face clip over the shadow.
internal sealed class PopupBorder : System.Windows.Controls.Border
{
    static PopupBorder()
    {
        MarginProperty.OverrideMetadata(typeof(PopupBorder), new FrameworkPropertyMetadata
        { CoerceValueCallback = CoerceMargin });
        UseLayoutRoundingProperty.OverrideMetadata(typeof(PopupBorder), new FrameworkPropertyMetadata
        { PropertyChangedCallback = OnRoundingChanged });
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        CoerceValue(MarginProperty);
    }

    private static void OnRoundingChanged(DependencyObject target, DependencyPropertyChangedEventArgs args) =>
        target.CoerceValue(MarginProperty);

    private static object CoerceMargin(DependencyObject target, object value)
    {
        var border = (PopupBorder)target;
        if (!border.UseLayoutRounding) return value;
        DpiScale dpi = VisualTreeHelper.GetDpi(border);
        var margin = (Thickness)value;
        return new Thickness(Round(margin.Left, dpi.DpiScaleX), Round(margin.Top, dpi.DpiScaleY),
            Round(margin.Right, dpi.DpiScaleX), Round(margin.Bottom, dpi.DpiScaleY));
    }

    private static double Round(double value, double scale) => double.IsFinite(value)
        ? Math.Round(value * scale) / scale : value;
}
