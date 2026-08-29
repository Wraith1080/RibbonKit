using System.Windows;
using System.Windows.Media;

namespace RibbonKit.Writer.Editing;

internal enum WriterTableResizeHandleKind
{
    Select,
    Column,
    Row,
    Overall
}

internal readonly record struct WriterTableResizeHandle(WriterTableResizeHandleKind Kind, int Index = -1);

internal sealed record WriterTableLayoutSnapshot(
    Rect Bounds,
    IReadOnlyList<double> ColumnBoundaries,
    IReadOnlyList<double> RowBoundaries,
    int RowGroupIndex,
    double ProjectionScaleX = 1d,
    double ProjectionScaleY = 1d)
{
    internal int ColumnCount => Math.Max(0, ColumnBoundaries.Count - 1);
    internal int RowCount => Math.Max(0, RowBoundaries.Count - 1);
}

internal static class WriterTableResizeGeometry
{
    internal const double MinimumColumnWidth = 24d;
    internal const double MinimumRowHeight = 12d;
    internal const double VisualHandleSize = 8d;
    internal const double HandleHitTargetSize = 18d;

    internal static IReadOnlyDictionary<WriterTableResizeHandle, Rect> GetHandleRects(
        WriterTableLayoutSnapshot layout, DpiScale dpi, double handleSize)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (!double.IsFinite(handleSize) || handleSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(handleSize));
        if (!double.IsFinite(dpi.DpiScaleX) || !double.IsFinite(dpi.DpiScaleY)
            || dpi.DpiScaleX <= 0 || dpi.DpiScaleY <= 0)
            throw new ArgumentOutOfRangeException(nameof(dpi));

        var width = Math.Max(1d / dpi.DpiScaleX, Align(handleSize, dpi.DpiScaleX));
        var height = Math.Max(1d / dpi.DpiScaleY, Align(handleSize, dpi.DpiScaleY));
        var result = new Dictionary<WriterTableResizeHandle, Rect>
        {
            [new WriterTableResizeHandle(WriterTableResizeHandleKind.Select)] =
                At(layout.Bounds.Left, layout.Bounds.Top)
        };

        for (var column = 0; column < layout.ColumnCount; column++)
        {
            result[new WriterTableResizeHandle(WriterTableResizeHandleKind.Column, column)] =
                At(layout.ColumnBoundaries[column + 1], layout.Bounds.Top);
        }
        for (var row = 0; row < layout.RowCount; row++)
        {
            result[new WriterTableResizeHandle(WriterTableResizeHandleKind.Row, row)] =
                At(layout.Bounds.Left, layout.RowBoundaries[row + 1]);
        }
        result[new WriterTableResizeHandle(WriterTableResizeHandleKind.Overall)] =
            At(layout.Bounds.Right, layout.Bounds.Bottom);
        return result;

        Rect At(double x, double y) => new(
            Align(x - width / 2d, dpi.DpiScaleX),
            Align(y - height / 2d, dpi.DpiScaleY), width, height);
    }

    internal static bool TryHitHandle(Point point, WriterTableLayoutSnapshot layout, DpiScale dpi,
        out WriterTableResizeHandle handle)
    {
        var found = false;
        var nearest = double.PositiveInfinity;
        handle = default;
        foreach (var pair in GetHandleRects(layout, dpi, HandleHitTargetSize))
        {
            if (!pair.Value.Contains(point))
                continue;
            var center = new Point(pair.Value.X + pair.Value.Width / 2d,
                pair.Value.Y + pair.Value.Height / 2d);
            var distance = (point - center).LengthSquared;
            if (distance >= nearest)
                continue;
            nearest = distance;
            handle = pair.Key;
            found = true;
        }
        return found;
    }

    internal static double ResizeColumn(double openingWidth, double delta, double maximumWidth) =>
        Math.Clamp(openingWidth + delta, MinimumColumnWidth,
            Math.Max(MinimumColumnWidth, maximumWidth));

    internal static IReadOnlyList<double> ResizeOverallWidths(IReadOnlyList<double> openingWidths,
        double delta, double maximumWidth)
    {
        ArgumentNullException.ThrowIfNull(openingWidths);
        if (openingWidths.Count == 0 || openingWidths.Any(width => !double.IsFinite(width) || width <= 0))
            throw new ArgumentOutOfRangeException(nameof(openingWidths));
        var openingTotal = openingWidths.Sum();
        var minimumTotal = openingWidths.Count * MinimumColumnWidth;
        var targetTotal = Math.Clamp(openingTotal + delta, minimumTotal,
            Math.Max(minimumTotal, maximumWidth));
        var result = new double[openingWidths.Count];
        var active = Enumerable.Range(0, openingWidths.Count).ToList();
        var remainingTotal = targetTotal;
        while (active.Count > 0)
        {
            var remainingBasis = active.Sum(index => openingWidths[index]);
            var belowMinimum = active.Where(index =>
                remainingTotal * openingWidths[index] / remainingBasis < MinimumColumnWidth)
                .ToArray();
            if (belowMinimum.Length == 0)
            {
                foreach (var index in active)
                    result[index] = remainingTotal * openingWidths[index] / remainingBasis;
                break;
            }
            foreach (var index in belowMinimum)
            {
                result[index] = MinimumColumnWidth;
                remainingTotal -= MinimumColumnWidth;
                active.Remove(index);
            }
        }
        return result;
    }

    private static double Align(double value, double scale) => Math.Round(value * scale) / scale;
}
