using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RibbonKit.Layout;

// Two adjacent one-DIP row margins become a single two-DIP gap. Keeping the
// gap in the panel leaves the first/last edge flush, including grouped lists.
internal sealed class DropDownItemsPanel : StackPanel
{
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing), typeof(double), typeof(DropDownItemsPanel),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure),
        value => value is double spacing && double.IsFinite(spacing) && spacing >= 0);

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        Size desired = base.MeasureOverride(constraint);
        int rows = InternalChildren.Cast<UIElement>().Count(IsRow);
        return new Size(desired.Width, desired.Height + Math.Max(0, rows - 1) * LayoutSpacing);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = 0;
        double spacing = LayoutSpacing;
        bool first = true;
        foreach (UIElement child in InternalChildren)
        {
            if (!IsRow(child))
            {
                child.Arrange(new Rect());
                continue;
            }
            if (!first) y += spacing;
            first = false;
            child.Arrange(new Rect(0, y, Math.Max(finalSize.Width, child.DesiredSize.Width), child.DesiredSize.Height));
            y += child.DesiredSize.Height;
        }
        return finalSize;
    }

    // An empty group's ItemsPresenter occupies no row and needs no trailing gap.
    private static bool IsRow(UIElement child) => child.Visibility != Visibility.Collapsed && child.DesiredSize.Height > 0;

    // Round each gap before summing it. Rounding only the final measured height
    // can make nested group content overrun its slot at fractional display DPI.
    private double LayoutSpacing => UseLayoutRounding
        ? Math.Round(Spacing * VisualTreeHelper.GetDpi(this).DpiScaleY) / VisualTreeHelper.GetDpi(this).DpiScaleY
        : Spacing;
}
