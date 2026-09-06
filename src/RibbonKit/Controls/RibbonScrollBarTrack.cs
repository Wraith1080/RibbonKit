using System.Windows;
using System.Windows.Controls.Primitives;

namespace RibbonKit.Controls;

/// <summary>
/// Supplies RibbonKit's theme minimum to WPF Track's native proportional-thumb calculation.
/// </summary>
internal sealed class RibbonScrollBarTrack : Track
{
    internal static readonly DependencyProperty MinimumThumbLengthProperty =
        DependencyProperty.Register(
            nameof(MinimumThumbLength),
            typeof(double),
            typeof(RibbonScrollBarTrack),
            new FrameworkPropertyMetadata(
                0d,
                FrameworkPropertyMetadataOptions.AffectsArrange,
                OnMinimumThumbLengthChanged),
            IsValidMinimumThumbLength);

    internal double MinimumThumbLength
    {
        get => (double)GetValue(MinimumThumbLengthProperty);
        set => SetValue(MinimumThumbLengthProperty, value);
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        UpdateNativeMinimumThumbResources();
    }

    private static void OnMinimumThumbLengthChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        ((RibbonScrollBarTrack)dependencyObject).UpdateNativeMinimumThumbResources();
    }

    private static bool IsValidMinimumThumbLength(object value)
    {
        double length = (double)value;
        return double.IsFinite(length) && length >= 0d;
    }

    private void UpdateNativeMinimumThumbResources()
    {
        // Track derives its proportional minimum from half of these system resources.
        // Keeping the override local preserves native value/density behavior without
        // changing host application or unrelated ScrollBar resources.
        double systemButtonExtent = MinimumThumbLength * 2d;
        Resources[SystemParameters.VerticalScrollBarButtonHeightKey] = systemButtonExtent;
        Resources[SystemParameters.HorizontalScrollBarButtonWidthKey] = systemButtonExtent;
    }
}
