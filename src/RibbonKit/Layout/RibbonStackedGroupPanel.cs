using System.Windows;
using System.Windows.Controls;
using RibbonKit.Controls;

namespace RibbonKit.Layout;

// Compact groups already have a height constraint and retain native WrapPanel
// layout. Touch groups can grow to fit larger targets, so wrap by command count
// rather than letting an unbounded column increase the ribbon's height.
internal sealed class RibbonStackedGroupPanel : WrapPanel
{
    private const int RowsPerColumn = 3;
    private bool LimitRows => Orientation == Orientation.Vertical && Ribbon.GetDensity(this) == RibbonDensity.Touch;

    protected override Size MeasureOverride(Size constraint)
    {
        if (!LimitRows) return base.MeasureOverride(constraint);

        var childConstraint = new Size(double.IsNaN(ItemWidth) ? constraint.Width : ItemWidth,
            double.IsNaN(ItemHeight) ? constraint.Height : ItemHeight);
        double width = 0, height = 0, columnWidth = 0, columnHeight = 0;
        int rows = 0;
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(childConstraint);
            if (child.Visibility == Visibility.Collapsed) continue;
            var size = ItemSize(child);
            columnWidth = Math.Max(columnWidth, size.Width);
            columnHeight += size.Height;
            if (++rows == RowsPerColumn)
            {
                width += columnWidth; height = Math.Max(height, columnHeight);
                rows = 0; columnWidth = columnHeight = 0;
            }
        }
        return new Size(width + columnWidth, Math.Max(height, columnHeight));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!LimitRows) return base.ArrangeOverride(finalSize);

        double x = 0;
        int start = 0;
        while (start < InternalChildren.Count)
        {
            int end = start, rows = 0;
            double columnWidth = 0;
            while (end < InternalChildren.Count && rows < RowsPerColumn)
            {
                var child = InternalChildren[end++];
                if (child.Visibility == Visibility.Collapsed) continue;
                columnWidth = Math.Max(columnWidth, ItemSize(child).Width);
                rows++;
            }
            double y = 0;
            for (int i = start; i < end; i++)
            {
                var child = InternalChildren[i];
                if (child.Visibility == Visibility.Collapsed) { child.Arrange(new Rect()); continue; }
                double itemHeight = ItemSize(child).Height;
                child.Arrange(new Rect(x, y, columnWidth, itemHeight));
                y += itemHeight;
            }
            x += columnWidth; start = end;
        }
        return finalSize;
    }

    private Size ItemSize(UIElement child) => new(double.IsNaN(ItemWidth) ? child.DesiredSize.Width : ItemWidth,
        double.IsNaN(ItemHeight) ? child.DesiredSize.Height : ItemHeight);
}
