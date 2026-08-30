using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace RibbonKit.Writer.Editing;

internal sealed class WriterTableResizeController : IDisposable
{
    private readonly RichTextBox _editor;
    private readonly WriterTableInteractionController _interaction;
    private readonly Action _onCommitted;
    private WriterTableResizeAdorner? _adorner;
    private AdornerLayer? _adornerLayer;
    private Window? _hostWindow;
    private bool _enabled = true;
    private bool _committing;
    private bool _disposed;

    internal WriterTableResizeController(RichTextBox editor,
        WriterTableInteractionController interaction, Action onCommitted)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        _onCommitted = onCommitted ?? throw new ArgumentNullException(nameof(onCommitted));
        _interaction.StateChanged += OnTableStateChanged;
        _editor.LayoutUpdated += OnLayoutUpdated;
        _editor.SizeChanged += OnEditorSizeChanged;
        _editor.AddHandler(ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler(OnEditorScrollChanged));
    }

    internal bool HasAdorner => _adorner is not null;
    internal bool IsDragging => _adorner?.IsDragging == true;

    internal void SetEnabled(bool enabled)
    {
        if (_enabled == enabled)
            return;
        _enabled = enabled;
        if (!enabled)
        {
            CancelActiveResize();
            RemoveAdorner();
        }
        else
            AttachAdorner();
    }

    internal void Refresh()
    {
        if (_disposed || _committing)
            return;
        if (!_enabled || _interaction.CurrentTable is null)
            RemoveAdorner();
        else
            AttachAdorner();
    }

    internal void ReplaceDocument()
    {
        CancelActiveResize();
        RemoveAdorner();
    }

    internal void CancelActiveResize() => _adorner?.CancelDrag();

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _interaction.StateChanged -= OnTableStateChanged;
        _editor.LayoutUpdated -= OnLayoutUpdated;
        _editor.SizeChanged -= OnEditorSizeChanged;
        _editor.RemoveHandler(ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler(OnEditorScrollChanged));
        if (_hostWindow is not null)
            _hostWindow.PreviewKeyDown -= OnHostPreviewKeyDown;
        RemoveAdorner();
    }

    private void OnTableStateChanged(object? sender, EventArgs e) => Refresh();

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        var host = Window.GetWindow(_editor);
        if (!ReferenceEquals(host, _hostWindow))
        {
            if (_hostWindow is not null)
                _hostWindow.PreviewKeyDown -= OnHostPreviewKeyDown;
            _hostWindow = host;
            if (_hostWindow is not null)
                _hostWindow.PreviewKeyDown += OnHostPreviewKeyDown;
        }
        if (_adorner is null)
            AttachAdorner();
    }

    private void OnEditorSizeChanged(object sender, SizeChangedEventArgs e) =>
        _adorner?.InvalidateVisual();

    private void OnEditorScrollChanged(object sender, ScrollChangedEventArgs e) =>
        _adorner?.InvalidateVisual();

    private void OnHostPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || _adorner?.IsDragging != true)
            return;
        _adorner.CancelDrag();
        e.Handled = true;
    }

    private void AttachAdorner()
    {
        if (!_enabled || !_editor.IsLoaded || _interaction.CurrentTable is null)
            return;
        var layer = AdornerLayer.GetAdornerLayer(_editor);
        if (layer is null)
            return;
        if (_adorner is not null && ReferenceEquals(_adornerLayer, layer))
        {
            _adorner.InvalidateVisual();
            return;
        }

        RemoveAdorner();
        _adorner = new WriterTableResizeAdorner(_editor, TryGetLayout, CaptureOpening,
            CommitResize, SelectTable);
        _adornerLayer = layer;
        layer.Add(_adorner);
    }

    private WriterTableLayoutSnapshot? TryGetLayout()
    {
        var table = _interaction.CurrentTable;
        var current = _interaction.CurrentCell;
        if (table is null || current is null)
            return null;
        return WriterTableLayoutResolver.TryCreate(_editor,
            _interaction.GetOrderedCells(table), current.Value.GroupIndex, out var layout)
            ? layout
            : null;
    }

    private WriterTableResizeOpening? CaptureOpening()
    {
        var table = _interaction.CurrentTable;
        var layout = TryGetLayout();
        if (table is null || layout is null)
            return null;
        var cells = _interaction.GetOrderedCells(table);
        var widths = Enumerable.Range(0, layout.ColumnCount)
            .Select(index => Math.Max(WriterTableResizeGeometry.MinimumColumnWidth,
                index < table.Columns.Count && table.Columns[index].Width.IsAbsolute
                    ? table.Columns[index].Width.Value
                    : layout.ColumnBoundaries[index + 1] - layout.ColumnBoundaries[index]))
            .ToArray();
        return new WriterTableResizeOpening(table, layout, cells, table.Columns.Count,
            table.Columns.Select(column => column.Width).ToArray(), widths,
            cells.ToDictionary(cell => cell.Cell, cell => cell.Cell.Padding));
    }

    private bool CommitResize(WriterTableResizeOpening opening,
        IReadOnlyDictionary<int, double> columnWidths,
        IReadOnlyList<WriterTableCellPaddingAdjustment> paddings)
    {
        if (!ReferenceEquals(_interaction.CurrentTable, opening.Table))
            return false;
        _committing = true;
        RemoveAdorner();
        var changed = false;
        try
        {
            using (_interaction.DeferRefresh())
                changed = _interaction.Tables.ApplyResize(opening.Table, columnWidths, paddings);
        }
        finally
        {
            _committing = false;
            _interaction.Refresh();
            Refresh();
        }
        if (changed)
            _onCommitted();
        return changed;
    }

    private void SelectTable()
    {
        if (_interaction.CurrentTable is not { } table)
            return;
        var cells = _interaction.GetOrderedCells(table);
        if (cells.Count == 0)
            return;
        // ContentEnd is an exclusive text boundary and WPF can associate it with the next cell.
        // ElementEnd includes the final structural cell marker, so the rightmost cell is visibly
        // and logically part of the table selection.
        _editor.Selection.Select(cells[0].Cell.ContentStart, cells[^1].Cell.ElementEnd);
        _editor.Focus();
        _interaction.Refresh();
    }

    private void RemoveAdorner()
    {
        if (_adorner is not null && _adornerLayer is not null)
            _adornerLayer.Remove(_adorner);
        _adorner = null;
        _adornerLayer = null;
    }
}

internal sealed record WriterTableResizeOpening(
    Table Table,
    WriterTableLayoutSnapshot Layout,
    IReadOnlyList<WriterTableCellReference> Cells,
    int ExistingColumnCount,
    IReadOnlyList<GridLength> OriginalColumnWidths,
    IReadOnlyList<double> DisplayedColumnWidths,
    IReadOnlyDictionary<TableCell, Thickness> OriginalPaddings);

internal static class WriterTableLayoutResolver
{
    internal static bool TryCreate(RichTextBox editor,
        IReadOnlyList<WriterTableCellReference> cells, int rowGroupIndex,
        out WriterTableLayoutSnapshot layout)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(cells);
        layout = null!;
        var realized = new List<(WriterTableCellReference Cell, Rect Bounds)>();
        foreach (var cell in cells)
        {
            if (TryGetCellBounds(cell.Cell, out var bounds))
                realized.Add((cell, bounds));
        }
        if (realized.Count == 0)
            return false;

        var table = cells[0].Table;
        var columnSpacing = double.IsFinite(table.CellSpacing)
            ? Math.Max(0, table.CellSpacing)
            : 0;
        var hasTableOrigin = TryGetTableOrigin(table, columnSpacing, out var tableOrigin);
        var rawLeft = hasTableOrigin ? tableOrigin.X : realized.Min(item => item.Bounds.Left);
        var rawTop = hasTableOrigin ? tableOrigin.Y : realized.Min(item => item.Bounds.Top);
        var transform = editor.LayoutTransform?.Value ?? Matrix.Identity;
        var scaleX = Math.Max(0.001, Math.Abs(transform.M11));
        var scaleY = Math.Max(0.001, Math.Abs(transform.M22));
        realized = realized.Select(item => (item.Cell,
            ProjectRect(item.Bounds, new Point(rawLeft, rawTop), scaleX, scaleY))).ToList();

        var left = rawLeft;
        var top = rawTop;
        var right = realized.Max(item => item.Bounds.Right);
        var bottom = realized.Max(item => item.Bounds.Bottom);
        // Logical shape must come from the document tree. Immediately after a cloned-table
        // resize WPF can leave an empty trailing cell without usable character geometry for one
        // layout pass; deriving the count from only realized cells would drop that final column.
        var columnCount = cells.Max(item => item.LastColumn) + 1;
        var groupCells = realized.Where(item => item.Cell.GroupIndex == rowGroupIndex).ToArray();
        if (groupCells.Length == 0)
            return false;
        var logicalGroupCells = cells.Where(item => item.GroupIndex == rowGroupIndex).ToArray();
        if (logicalGroupCells.Length == 0)
            return false;
        var rowCount = logicalGroupCells.Max(item => item.LastRow) + 1;
        var columns = BuildBoundaries(columnCount, realized,
            item => item.Cell.Column, item => item.Cell.LastColumn,
            item => item.Bounds.Left, item => item.Bounds.Right, left, right,
            WriterTableResizeGeometry.MinimumColumnWidth);
        for (var column = 0; column < columnCount && column < table.Columns.Count; column++)
        {
            if (table.Columns[column].Width.IsAbsolute)
            {
                // WPF lays out every explicit table column with one CellSpacing contribution.
                // Omitting it makes the adorner drift left by another spacing unit per column.
                columns[column + 1] = columns[column]
                    + (table.Columns[column].Width.Value + columnSpacing) * scaleX;
            }
        }
        if (columnCount > 1 && (table.Columns.Count < columnCount
                || !table.Columns[columnCount - 1].Width.IsAbsolute))
        {
            var priorWidths = Enumerable.Range(0, columnCount - 1)
                .Select(index => columns[index + 1] - columns[index])
                .Where(width => width > WriterTableResizeGeometry.MinimumColumnWidth)
                .OrderBy(width => width).ToArray();
            if (priorWidths.Length > 0)
            {
                var expected = priorWidths[priorWidths.Length / 2];
                if (columns[^1] - columns[^2] < expected * 0.75)
                    columns[^1] = columns[^2] + expected;
            }
        }
        var rows = BuildBoundaries(rowCount, groupCells,
            item => item.Cell.Row, item => item.Cell.LastRow,
            item => item.Bounds.Top, item => item.Bounds.Bottom,
            groupCells.Min(item => item.Bounds.Top), groupCells.Max(item => item.Bounds.Bottom), 1d);
        // Character rectangles describe insertion positions, not cell edges. Center/right text
        // alignment can extend their raw union beyond the table even though the grid did not move.
        // The resolved logical boundaries are the stable source for the adorner perimeter.
        left = columns[0];
        right = columns[^1];
        top = rows[0];
        bottom = rows[^1];
        layout = new WriterTableLayoutSnapshot(new Rect(left, top,
            Math.Max(1, right - left), Math.Max(1, bottom - top)), columns, rows, rowGroupIndex,
            scaleX, scaleY);
        return true;
    }

    internal static Rect ProjectRect(Rect rect, Point anchor, double scaleX, double scaleY) =>
        new(anchor.X + (rect.X - anchor.X) * scaleX,
            anchor.Y + (rect.Y - anchor.Y) * scaleY,
            rect.Width * scaleX, rect.Height * scaleY);

    private static double[] BuildBoundaries<T>(int count, IReadOnlyList<T> items,
        Func<T, int> firstIndex, Func<T, int> lastIndex,
        Func<T, double> firstCoordinate, Func<T, double> lastCoordinate,
        double minimum, double maximum, double minimumSpacing)
    {
        var starts = Enumerable.Range(0, count + 1)
            .Select(_ => new List<double>()).ToArray();
        var ends = Enumerable.Range(0, count + 1)
            .Select(_ => new List<double>()).ToArray();
        starts[0].Add(minimum);
        ends[count].Add(maximum);
        foreach (var item in items)
        {
            starts[firstIndex(item)].Add(firstCoordinate(item));
            ends[lastIndex(item) + 1].Add(lastCoordinate(item));
        }
        var values = new double[count + 1];
        var known = new bool[count + 1];
        for (var i = 0; i < values.Length; i++)
        {
            if (i == 0)
            {
                values[i] = minimum;
                known[i] = true;
                continue;
            }
            var candidates = starts[i].Count > 0 ? starts[i] : ends[i];
            if (candidates.Count == 0)
                continue;
            values[i] = candidates.Average();
            known[i] = true;
        }
        for (var start = 0; start < count; start++)
        {
            if (!known[start])
                continue;
            var end = start + 1;
            while (end <= count && !known[end])
                end++;
            if (end > count)
                break;
            var step = (values[end] - values[start]) / (end - start);
            for (var i = start + 1; i < end; i++)
                values[i] = values[start] + step * (i - start);
            start = end - 1;
        }
        for (var i = 1; i < values.Length; i++)
            values[i] = Math.Max(values[i], values[i - 1] + minimumSpacing);
        return values;
    }

    private static bool TryGetTableOrigin(Table table, double cellSpacing, out Point origin)
    {
        origin = default;
        try
        {
            var rect = table.ElementStart.GetCharacterRect(LogicalDirection.Forward);
            if (rect.IsEmpty || !double.IsFinite(rect.Left) || !double.IsFinite(rect.Top))
                return false;
            origin = new Point(rect.Left + cellSpacing, rect.Top + cellSpacing);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool TryGetCellBounds(TableCell cell, out Rect bounds)
    {
        bounds = Rect.Empty;
        try
        {
            var start = cell.ContentStart.GetInsertionPosition(LogicalDirection.Forward);
            var end = cell.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
            if (start is null || end is null)
                return false;
            var startRect = start.GetCharacterRect(LogicalDirection.Forward);
            var endRect = end.GetCharacterRect(LogicalDirection.Backward);
            if (startRect.IsEmpty || endRect.IsEmpty)
                return false;
            var padding = cell.Padding;
            var border = cell.BorderThickness;
            var left = Math.Min(startRect.Left, endRect.Left) - padding.Left - border.Left;
            var top = Math.Min(startRect.Top, endRect.Top) - padding.Top - border.Top;
            var right = Math.Max(startRect.Right, endRect.Right) + padding.Right + border.Right;
            var bottom = Math.Max(startRect.Bottom, endRect.Bottom) + padding.Bottom + border.Bottom;
            bounds = new Rect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
            return double.IsFinite(bounds.X) && double.IsFinite(bounds.Y)
                && double.IsFinite(bounds.Width) && double.IsFinite(bounds.Height);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}

internal sealed class WriterTableResizeAdorner : Adorner
{
    private static readonly DependencyProperty SelectionBrushProperty =
        DependencyProperty.Register(nameof(SelectionBrush), typeof(Brush),
            typeof(WriterTableResizeAdorner), new FrameworkPropertyMetadata(Brushes.DodgerBlue,
                FrameworkPropertyMetadataOptions.AffectsRender));
    private static readonly DependencyProperty HandleFillProperty =
        DependencyProperty.Register(nameof(HandleFill), typeof(Brush),
            typeof(WriterTableResizeAdorner), new FrameworkPropertyMetadata(Brushes.White,
                FrameworkPropertyMetadataOptions.AffectsRender));

    private readonly Func<WriterTableLayoutSnapshot?> _getLayout;
    private readonly Func<WriterTableResizeOpening?> _captureOpening;
    private readonly Func<WriterTableResizeOpening, IReadOnlyDictionary<int, double>,
        IReadOnlyList<WriterTableCellPaddingAdjustment>, bool> _commit;
    private readonly Action _selectTable;
    private WriterTableResizeHandle? _activeHandle;
    private WriterTableResizeOpening? _opening;
    private Point _openingPointer;
    private Vector _previewDelta;
    private bool _endingCapture;

    internal WriterTableResizeAdorner(RichTextBox editor,
        Func<WriterTableLayoutSnapshot?> getLayout,
        Func<WriterTableResizeOpening?> captureOpening,
        Func<WriterTableResizeOpening, IReadOnlyDictionary<int, double>,
            IReadOnlyList<WriterTableCellPaddingAdjustment>, bool> commit,
        Action selectTable) : base(editor)
    {
        _getLayout = getLayout;
        _captureOpening = captureOpening;
        _commit = commit;
        _selectTable = selectTable;
        Focusable = false;
        SetResourceReference(SelectionBrushProperty, "RibbonKit.Brushes.Accent");
        SetResourceReference(HandleFillProperty, "RibbonKit.Brushes.Control.Background");
    }

    private Brush SelectionBrush => (Brush)GetValue(SelectionBrushProperty);
    private Brush HandleFill => (Brush)GetValue(HandleFillProperty);
    internal bool IsDragging => _activeHandle is not null;

    internal bool BeginDragForTesting(WriterTableResizeHandle handle, Point point) =>
        BeginDrag(handle, point, captureMouse: false);

    internal void UpdateDragForTesting(Point point) => Preview(point - _openingPointer);

    internal void CompleteDragForTesting() => CompleteDrag();

    internal void SelectTableForTesting() => _selectTable();

    internal void SimulateCaptureLossForTesting() => CancelDrag();

    internal WriterTableLayoutSnapshot? RenderedLayoutForTesting => GetRenderedLayout();

    internal void CancelDrag()
    {
        if (_opening is null)
            return;
        RestoreOpening(_opening);
        EndCapture();
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var layout = GetRenderedLayout();
        if (layout is null)
            return;
        var dpi = VisualTreeHelper.GetDpi(this);
        var pen = new Pen(SelectionBrush, Math.Max(1d / dpi.DpiScaleX, 1d));
        drawingContext.DrawRectangle(null, pen, layout.Bounds);
        foreach (var rect in WriterTableResizeGeometry.GetHandleRects(
                     layout, dpi, WriterTableResizeGeometry.HandleHitTargetSize).Values)
            drawingContext.DrawRectangle(Brushes.Transparent, null, rect);
        foreach (var rect in WriterTableResizeGeometry.GetHandleRects(
                     layout, dpi, WriterTableResizeGeometry.VisualHandleSize).Values)
            drawingContext.DrawRectangle(HandleFill, pen, rect);
    }

    protected override HitTestResult? HitTestCore(PointHitTestParameters parameters) =>
        TryGetHandle(parameters.HitPoint, out _)
            ? new PointHitTestResult(this, parameters.HitPoint)
            : null;

    protected override void OnQueryCursor(QueryCursorEventArgs e)
    {
        base.OnQueryCursor(e);
        if (!TryGetHandle(Mouse.GetPosition(this), out var handle))
            return;
        e.Cursor = GetCursor(handle);
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (!TryGetHandle(e.GetPosition(this), out var handle))
            return;
        if (handle.Kind == WriterTableResizeHandleKind.Select)
        {
            _selectTable();
            e.Handled = true;
            return;
        }
        e.Handled = BeginDrag(handle, e.GetPosition(this), captureMouse: true);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_activeHandle is null || _opening is null || e.LeftButton != MouseButtonState.Pressed)
            return;
        Preview(e.GetPosition(this) - _openingPointer);
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_activeHandle is null || _opening is null)
            return;
        CompleteDrag();
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (!_endingCapture && _opening is not null)
            CancelDrag();
    }

    protected override AutomationPeer? OnCreateAutomationPeer() => null;

    internal static Cursor GetCursor(WriterTableResizeHandle handle) => handle.Kind switch
    {
        WriterTableResizeHandleKind.Select => Cursors.Hand,
        WriterTableResizeHandleKind.Column => Cursors.SizeWE,
        WriterTableResizeHandleKind.Row => Cursors.SizeNS,
        WriterTableResizeHandleKind.Overall => Cursors.SizeNWSE,
        _ => Cursors.Arrow
    };

    private bool TryGetHandle(Point point, out WriterTableResizeHandle handle)
    {
        var layout = _getLayout();
        if (layout is null)
        {
            handle = default;
            return false;
        }
        return WriterTableResizeGeometry.TryHitHandle(point, layout,
            VisualTreeHelper.GetDpi(this), out handle);
    }

    private bool BeginDrag(WriterTableResizeHandle handle, Point point, bool captureMouse)
    {
        if (handle.Kind is WriterTableResizeHandleKind.Select || !double.IsFinite(point.X)
            || !double.IsFinite(point.Y))
            return false;
        var opening = _captureOpening();
        if (opening is null || handle.Kind == WriterTableResizeHandleKind.Column
            && (handle.Index < 0 || handle.Index >= opening.DisplayedColumnWidths.Count)
            || handle.Kind == WriterTableResizeHandleKind.Row
            && (handle.Index < 0 || handle.Index >= opening.Layout.RowCount))
            return false;
        _activeHandle = handle;
        _opening = opening;
        _openingPointer = point;
        _previewDelta = default;
        if (captureMouse)
            Mouse.Capture(this);
        return true;
    }

    private void Preview(Vector delta)
    {
        if (_opening is null || _activeHandle is not { } handle)
            return;
        delta = new Vector(delta.X / _opening.Layout.ProjectionScaleX,
            delta.Y / _opening.Layout.ProjectionScaleY);
        RestoreOpening(_opening);
        EnsureColumns(_opening);
        var spacing = double.IsFinite(_opening.Table.CellSpacing)
            ? Math.Max(0, _opening.Table.CellSpacing)
            : 0;
        var maximum = Math.Max(
            _opening.DisplayedColumnWidths.Count * WriterTableResizeGeometry.MinimumColumnWidth,
            GetMaximumWidth((RichTextBox)AdornedElement)
                - _opening.DisplayedColumnWidths.Count * spacing);
        switch (handle.Kind)
        {
            case WriterTableResizeHandleKind.Column:
            {
                var other = _opening.DisplayedColumnWidths.Where((_, index) => index != handle.Index).Sum();
                var width = WriterTableResizeGeometry.ResizeColumn(
                    _opening.DisplayedColumnWidths[handle.Index], delta.X, maximum - other);
                _opening.Table.Columns[handle.Index].Width = new GridLength(width, GridUnitType.Pixel);
                _previewDelta = new Vector(width - _opening.DisplayedColumnWidths[handle.Index], 0);
                break;
            }
            case WriterTableResizeHandleKind.Row:
                _previewDelta = new Vector(0, BoundRowDelta(_opening.Layout, handle.Index, delta.Y));
                break;
            case WriterTableResizeHandleKind.Overall:
            {
                var widths = WriterTableResizeGeometry.ResizeOverallWidths(
                    _opening.DisplayedColumnWidths, delta.X, maximum);
                for (var i = 0; i < widths.Count; i++)
                    _opening.Table.Columns[i].Width = new GridLength(widths[i], GridUnitType.Pixel);
                _previewDelta = new Vector(widths.Sum() - _opening.DisplayedColumnWidths.Sum(),
                    BoundOverallRowDelta(_opening.Layout, delta.Y));
                break;
            }
        }
        InvalidateVisual();
    }

    private void CompleteDrag()
    {
        if (_opening is null)
            return;
        var opening = _opening;
        var widths = new Dictionary<int, double>();
        for (var i = 0; i < opening.DisplayedColumnWidths.Count && i < opening.Table.Columns.Count; i++)
        {
            var width = opening.Table.Columns[i].Width;
            if (width.IsAbsolute && Math.Abs(width.Value - opening.DisplayedColumnWidths[i]) > 0.01)
                widths[i] = width.Value;
        }
        var paddings = BuildPaddingAdjustments(opening, _activeHandle!.Value, _previewDelta.Y);
        RestoreOpening(opening);
        EndCapture();
        if ((widths.Count > 0 || paddings.Length > 0) && !_commit(opening, widths, paddings))
            InvalidateVisual();
    }

    private static WriterTableCellPaddingAdjustment[] BuildPaddingAdjustments(
        WriterTableResizeOpening opening, WriterTableResizeHandle handle, double delta)
    {
        IEnumerable<WriterTableCellReference> cells;
        Func<WriterTableCellReference, double> getDelta;
        if (handle.Kind == WriterTableResizeHandleKind.Row)
        {
            cells = opening.Cells.Where(cell => cell.GroupIndex == opening.Layout.RowGroupIndex
                && cell.Row <= handle.Index && cell.LastRow >= handle.Index);
            getDelta = _ => delta;
        }
        else if (handle.Kind == WriterTableResizeHandleKind.Overall)
        {
            cells = opening.Cells.Where(cell => cell.GroupIndex == opening.Layout.RowGroupIndex);
            var perRow = delta / Math.Max(1, opening.Layout.RowCount);
            getDelta = cell => perRow * cell.RowSpan;
        }
        else
            return [];

        var adjustments = new List<WriterTableCellPaddingAdjustment>();
        foreach (var cell in cells)
        {
            var original = opening.OriginalPaddings[cell.Cell];
            var adjusted = GetAdjustedPadding(original, getDelta(cell));
            if (adjusted != original)
            {
                adjustments.Add(new WriterTableCellPaddingAdjustment(cell.GroupIndex,
                    cell.Row, cell.Column, adjusted));
            }
        }
        return adjustments.ToArray();
    }

    private static Thickness GetAdjustedPadding(Thickness opening, double delta)
    {
        var vertical = Math.Max(0, opening.Top + opening.Bottom + delta);
        var top = vertical / 2d;
        return new Thickness(opening.Left, top, opening.Right, vertical - top);
    }

    private WriterTableLayoutSnapshot? GetRenderedLayout()
    {
        if (_opening is not null && _activeHandle is { Kind: WriterTableResizeHandleKind.Overall })
        {
            // Native table-column layout can lag while the adorner owns mouse capture. Build the
            // complete two-axis preview from the immutable opening snapshot so the bottom-right
            // handle tracks the current pointer immediately on both axes. Do not query live
            // layout first: it can be temporarily unavailable during that same column reflow.
            var opening = _opening.Layout;
            var columns = opening.ColumnBoundaries.ToArray();
            var overallRows = opening.RowBoundaries.ToArray();
            ScaleBoundaries(columns, _previewDelta.X * opening.ProjectionScaleX);
            ScaleBoundaries(overallRows, _previewDelta.Y * opening.ProjectionScaleY);
            var overallBounds = new Rect(opening.Bounds.Left, opening.Bounds.Top,
                Math.Max(1, columns[^1] - opening.Bounds.Left),
                Math.Max(1, overallRows[^1] - opening.Bounds.Top));
            return opening with
            {
                Bounds = overallBounds,
                ColumnBoundaries = columns,
                RowBoundaries = overallRows
            };
        }
        var layout = _getLayout();
        if (layout is null || _opening is null || _activeHandle is not { } handle)
            return layout;
        if (Math.Abs(_previewDelta.Y) < 0.001)
            return layout;
        var rows = layout.RowBoundaries.ToArray();
        if (handle.Kind == WriterTableResizeHandleKind.Row)
        {
            for (var i = handle.Index + 1; i < rows.Length; i++)
                rows[i] += _previewDelta.Y * layout.ProjectionScaleY;
        }
        else
            return layout;
        var bounds = new Rect(layout.Bounds.Left, layout.Bounds.Top,
            layout.Bounds.Width, Math.Max(1, rows[^1] - layout.Bounds.Top));
        return layout with { Bounds = bounds, RowBoundaries = rows };
    }

    private static void ScaleBoundaries(double[] boundaries, double delta)
    {
        if (boundaries.Length < 2)
            return;
        var opening = boundaries[^1] - boundaries[0];
        if (opening <= 0)
            return;
        var target = Math.Max(1, opening + delta);
        for (var i = 1; i < boundaries.Length; i++)
            boundaries[i] = boundaries[0] + (boundaries[i] - boundaries[0]) * target / opening;
    }

    private static double BoundRowDelta(WriterTableLayoutSnapshot layout, int row, double delta)
    {
        var opening = layout.RowBoundaries[row + 1] - layout.RowBoundaries[row];
        return Math.Max(delta, WriterTableResizeGeometry.MinimumRowHeight - opening);
    }

    private static double BoundOverallRowDelta(WriterTableLayoutSnapshot layout, double delta)
    {
        var opening = layout.RowBoundaries[^1] - layout.RowBoundaries[0];
        return Math.Max(delta,
            layout.RowCount * WriterTableResizeGeometry.MinimumRowHeight - opening);
    }

    private static void EnsureColumns(WriterTableResizeOpening opening)
    {
        while (opening.Table.Columns.Count < opening.DisplayedColumnWidths.Count)
            opening.Table.Columns.Add(new TableColumn());
    }

    private static void RestoreOpening(WriterTableResizeOpening opening)
    {
        while (opening.Table.Columns.Count > opening.ExistingColumnCount)
            opening.Table.Columns.RemoveAt(opening.Table.Columns.Count - 1);
        for (var i = 0; i < opening.OriginalColumnWidths.Count; i++)
            opening.Table.Columns[i].Width = opening.OriginalColumnWidths[i];
        foreach (var pair in opening.OriginalPaddings)
            pair.Key.Padding = pair.Value;
    }

    private void EndCapture()
    {
        _endingCapture = true;
        try
        {
            if (IsMouseCaptured)
                ReleaseMouseCapture();
        }
        finally
        {
            _endingCapture = false;
            _activeHandle = null;
            _opening = null;
            _previewDelta = default;
        }
    }

    private static double GetMaximumWidth(RichTextBox editor)
    {
        var pageWidth = editor.Document.PageWidth;
        var padding = editor.Document.PagePadding;
        var width = double.IsFinite(pageWidth)
            ? pageWidth - padding.Left - padding.Right
            : editor.ActualWidth - editor.Padding.Left - editor.Padding.Right;
        return Math.Max(WriterTableResizeGeometry.MinimumColumnWidth, width);
    }
}
