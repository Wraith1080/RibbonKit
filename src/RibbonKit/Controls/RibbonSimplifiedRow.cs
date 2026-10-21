using System.Windows;

namespace RibbonKit.Controls;

// Row-only geometry: full-group controls and QAT/overflow copies stay unmarked.
internal static class RibbonSimplifiedRow
{
    public static readonly DependencyProperty UseMetricsProperty = DependencyProperty.RegisterAttached(
        "UseMetrics", typeof(bool), typeof(RibbonSimplifiedRow), new PropertyMetadata(false));
    public static readonly PropertyPath UseMetricsPath = new("(0)", UseMetricsProperty);

    public static readonly DependencyProperty SpacingProperty = DependencyProperty.RegisterAttached(
        "Spacing", typeof(double), typeof(RibbonSimplifiedRow), new FrameworkPropertyMetadata(0d,
            FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure
            | FrameworkPropertyMetadataOptions.AffectsArrange));
}
