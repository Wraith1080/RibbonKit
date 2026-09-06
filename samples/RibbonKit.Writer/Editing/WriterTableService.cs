using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RibbonKit.Writer.Editing;

/// <summary>
/// Provides UI-independent discovery and mutation helpers for native FlowDocument tables.
/// </summary>
/// <remarks>
/// The service is deliberately separate from Writer's ribbon and keyboard projection. It owns the
/// table grid invariants, keeps every created row and cell valid, and restores the editor caret after
/// each structural operation. Mutations are grouped with the native RichTextBox change scope so the
/// editor's own undo/redo manager remains the single user-visible history. Column metadata operations
/// replace a deep-cloned table inside that scope because WPF does not include direct TableColumn
/// collection/property edits in its native undo unit. Structural selection ranges intentionally
/// collapse to the affected logical caret after a successful mutation; callers should save and
/// restore both selection endpoints themselves when they need a non-collapsed range.
/// </remarks>
public sealed class WriterTableService : IDisposable
{
    /// <summary>Maximum rows or columns created by one bounded W3-B operation.</summary>
    public const int MaximumStructuralCount = 8;

    private bool _disposed;

    /// <summary>Creates a table service over an existing native editor.</summary>
    /// <param name="editor">The editor whose live FlowDocument is mutated.</param>
    public WriterTableService(RichTextBox editor)
    {
        Editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    /// <summary>Gets the native editor used by this service.</summary>
    public RichTextBox Editor { get; }

    /// <summary>Gets whether a document mutation is currently allowed.</summary>
    public bool CanMutate => Editor.IsEnabled && !Editor.IsReadOnly;

    /// <summary>
    /// Inserts a native table at the paragraph containing the current caret and moves the caret to
    /// the first cell. Dimensions are limited to Writer's supported 1×1 through 8×8 table range.
    /// </summary>
    /// <param name="rows">The number of rows to create.</param>
    /// <param name="columns">The number of columns to create.</param>
    /// <returns>The created table, or <see langword="null"/> when insertion is unavailable.</returns>
    public Table? InsertTable(int rows, int columns) =>
        InsertTable(rows, columns, SystemColors.ControlDarkBrush);

    /// <summary>
    /// Inserts a native table with a visible outer frame and cell grid at the current caret.
    /// </summary>
    /// <param name="rows">The number of rows to create.</param>
    /// <param name="columns">The number of columns to create.</param>
    /// <param name="borderBrush">The brush used for the outer frame and cell grid.</param>
    /// <returns>The created table, or <see langword="null"/> when insertion is unavailable.</returns>
    public Table? InsertTable(int rows, int columns, Brush borderBrush)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(borderBrush);
        if (rows is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(rows), rows, "Rows must be between 1 and 8.");
        if (columns is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(columns), columns,
                "Columns must be between 1 and 8.");
        if (!CanMutate || !Editor.Selection.IsEmpty)
            return null;

        Table? created = null;
        WriterTableCellReference? caret = null;
        if (!Mutate(() =>
        {
            if (!TryInsertTableAtCaret(rows, columns, borderBrush, out created))
                return false;
            var group = created!.RowGroups[0];
            var cell = group.Rows[0].Cells[0];
            caret = MakeReference(created, group, cell);
            return caret.HasValue;
        }, () => SetCaret(caret)))
            return null;

        return created;
    }

    /// <summary>Resolves a pointer to the innermost native table cell containing it.</summary>
    /// <param name="pointer">A pointer in <see cref="Editor"/>'s current document.</param>
    /// <param name="reference">The resolved cell and logical grid coordinates.</param>
    /// <returns><see langword="true"/> when the pointer is inside a table cell.</returns>
    public bool TryGetCell(TextPointer? pointer, out WriterTableCellReference reference)
    {
        ThrowIfDisposed();
        reference = default;
        if (pointer is null)
            return false;

        var ancestor = FindAncestor<TableCell>(pointer.Parent as DependencyObject);
        if (ancestor is not null && TryGetCell(ancestor, out reference))
            return true;

        // ContentStart/ContentEnd can have a pointer parent outside the cell. Scan the document as
        // a fallback and prefer the smallest containing range (nested tables resolve innermost).
        TableCell? bestCell = null;
        var bestDistance = int.MaxValue;
        foreach (var table in EnumerateTables(Editor.Document))
        {
            foreach (var group in table.RowGroups)
            {
                foreach (var cell in group.Rows.SelectMany(static row => row.Cells))
                {
                    try
                    {
                        if (pointer.CompareTo(cell.ContentStart) < 0 ||
                            pointer.CompareTo(cell.ContentEnd) > 0)
                            continue;
                        var distance = cell.ContentStart.GetOffsetToPosition(cell.ContentEnd);
                        if (distance < bestDistance)
                        {
                            bestCell = cell;
                            bestDistance = distance;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // A pointer from another document cannot be compared to this cell.
                    }
                }
            }
        }

        return bestCell is not null && TryGetCell(bestCell, out reference);
    }

    /// <summary>Resolves a native cell to its current logical coordinates.</summary>
    public bool TryGetCell(TableCell? cell, out WriterTableCellReference reference)
    {
        ThrowIfDisposed();
        reference = default;
        if (cell is null)
            return false;

        var table = FindAncestor<Table>(cell);
        var group = FindAncestor<TableRowGroup>(cell);
        if (table is null || group is null)
            return false;
        if (!IsTableInDocument(table))
            return false;
        if (!WriterTableGrid.TryBuild(table, group, out var grid))
            return false;
        var placement = grid.Placements.FirstOrDefault(item => ReferenceEquals(item.Cell, cell));
        if (placement is null)
            return false;

        reference = MakeReference(table, group, cell, placement, grid);
        return true;
    }

    /// <summary>Resolves the current collapsed or selected editor range to a table rectangle.</summary>
    public bool TryGetSelectionRange(out WriterTableRange range)
    {
        ThrowIfDisposed();
        return TryGetSelectionRange(Editor.Selection.Start, Editor.Selection.End, out range);
    }

    /// <summary>Resolves a captured range to a table rectangle without changing editor selection.</summary>
    public bool TryGetSelectionRange(
        TextPointer? start,
        TextPointer? end,
        out WriterTableRange range)
    {
        ThrowIfDisposed();
        range = default;
        if (start is null || end is null)
            return false;
        try
        {
            if (start.CompareTo(end) > 0)
                (start, end) = (end, start);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        var collapsed = start.CompareTo(end) == 0;
        WriterTableCellReference first;
        WriterTableCellReference last;
        var hasFirst = collapsed
            ? TryGetCell(start, out first)
            : TryGetSelectionEdgeCell(start, LogicalDirection.Forward, out first);
        var hasLast = collapsed
            ? TryGetCell(end, out last)
            : TryGetSelectionEdgeCell(end, LogicalDirection.Backward, out last);
        if (!hasFirst || !hasLast ||
            !ReferenceEquals(first.Table, last.Table) ||
            !ReferenceEquals(first.RowGroup, last.RowGroup))
            return false;

        range = WriterTableRange.Between(first, last);
        return range.IsValid;
    }

    /// <summary>
    /// Returns whether a pointer lies in one of the cells covered by a captured table selection.
    /// </summary>
    public bool IsPointerInsideTableSelection(TextPointer pointer, TextPointer start, TextPointer end)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(pointer);
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        if (!TryGetSelectionRange(start, end, out var range)
            || !TryGetCell(pointer, out var cell)
            || !ReferenceEquals(cell.Table, range.Table)
            || !ReferenceEquals(cell.RowGroup, range.RowGroup))
            return false;
        return cell.Row >= range.StartRow && cell.LastRow <= range.EndRow
            && cell.Column >= range.StartColumn && cell.LastColumn <= range.EndColumn;
    }

    /// <summary>Resolves the current caret to a table cell.</summary>
    public bool TryGetCellAtCaret(out WriterTableCellReference reference)
    {
        ThrowIfDisposed();
        return TryGetCell(Editor.Selection.Start, out reference);
    }

    /// <summary>
    /// Deletes one captured live table and replaces its block slot with an empty caret paragraph.
    /// </summary>
    public bool DeleteTable(Table table)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        Paragraph? caretParagraph = null;
        return Mutate(() =>
        {
            if (!IsTableInDocument(table))
                return false;
            caretParagraph = new Paragraph(new Run());
            return ReplaceTableWithBlock(table, caretParagraph);
        }, () =>
        {
            if (caretParagraph is null)
                throw new InvalidOperationException("The table deletion caret was not created.");
            var caret = caretParagraph.ContentStart.GetInsertionPosition(LogicalDirection.Forward);
            Editor.Selection.Select(caret, caret);
            Editor.Focus();
        });
    }

    /// <summary>Inserts rows relative to the supplied cell.</summary>
    public bool InsertRows(WriterTableCellReference reference, int count = 1,
        WriterTableInsertPlacement placement = WriterTableInsertPlacement.After)
    {
        ThrowIfDisposed();
        ValidateCount(count, nameof(count));
        ValidatePlacement(placement);
        WriterTableCellReference? caret = null;
        return Mutate(() =>
        {
            if (!TryGetCurrentGrid(reference, out var grid, out var current))
                return false;
            var boundary = placement == WriterTableInsertPlacement.Before
                ? current.Row
                : checked(current.Row + current.RowSpan);
            if (boundary < 0 || boundary > grid.Rows.Count)
                return false;

            var placements = grid.Placements.Select(static item => item.Clone()).ToList();
            foreach (var item in placements)
            {
                if (item.Row < boundary && item.RowSpan > boundary - item.Row)
                    item.RowSpan = checked(item.RowSpan + count);
                else if (item.Row >= boundary)
                    item.Row = checked(item.Row + count);
            }

            var rows = grid.Rows.ToList();
            var insertedRows = Enumerable.Range(0, count).Select(_ => new TableRow()).ToList();
            rows.InsertRange(boundary, insertedRows);
            var width = Math.Max(1, grid.ColumnCount);
            AddMissingCells(placements, insertedRows, boundary, width, count);
            EnsureColumns(grid.Table, width);
            ReplaceRows(grid.RowGroup, rows, placements);
            caret = MakeCaretReference(grid.Table, grid.RowGroup, boundary, 0);
            return caret.HasValue;
        }, () => SetCaret(caret));
    }

    /// <summary>Inserts rows relative to the current editor cell.</summary>
    public bool InsertRows(int count = 1, WriterTableInsertPlacement placement = WriterTableInsertPlacement.After)
    {
        ThrowIfDisposed();
        return TryGetCell(Editor.Selection.Start, out var reference) &&
            InsertRows(reference, count, placement);
    }

    /// <summary>Deletes rows beginning at the supplied cell's row.</summary>
    public bool DeleteRows(WriterTableCellReference reference, int count = 1)
    {
        ThrowIfDisposed();
        ValidateCount(count, nameof(count));
        WriterTableCellReference? caret = null;
        return Mutate(() =>
        {
            if (!TryGetCurrentGrid(reference, out var grid, out var current) ||
                count > grid.Rows.Count - current.Row ||
                grid.Rows.Count - count < 1)
                return false;

            var start = current.Row;
            var end = checked(start + count);
            var survivors = grid.Rows.Select((row, index) => (row, index))
                .Where(item => item.index < start || item.index >= end)
                .Select(item => item.row).ToList();
            var placements = RemoveRows(grid.Placements, start, end);
            // A cell anchored in a deleted row may survive through a row span and must be detached
            // before it can be reattached to a surviving row.
            for (var row = start; row < end; row++)
                grid.Rows[row].Cells.Clear();
            grid.RowGroup.Rows.RemoveRange(start, count);
            ReplaceRows(grid.RowGroup, survivors, placements);
            NormalizeGroup(grid.Table, grid.RowGroup);
            caret = MakeCaretReference(grid.Table, grid.RowGroup,
                Math.Min(start, grid.RowGroup.Rows.Count - 1), 0);
            return caret.HasValue;
        }, () => SetCaret(caret));
    }

    /// <summary>Deletes rows beginning at the current editor cell's row.</summary>
    public bool DeleteRows(int count = 1)
    {
        ThrowIfDisposed();
        return TryGetCell(Editor.Selection.Start, out var reference) && DeleteRows(reference, count);
    }

    /// <summary>Inserts columns relative to the supplied cell across every row group.</summary>
    public bool InsertColumns(WriterTableCellReference reference, int count = 1,
        WriterTableInsertPlacement placement = WriterTableInsertPlacement.After)
    {
        ThrowIfDisposed();
        ValidateCount(count, nameof(count));
        ValidatePlacement(placement);
        WriterTableCellReference? caret = null;
        return Mutate(() =>
        {
            if (!TryGetCurrentGrid(reference, out var selectedGrid, out var current))
                return false;
            var oldWidth = GetTableColumnCount(reference.Table);
            var boundary = placement == WriterTableInsertPlacement.Before
                ? current.Column
                : checked(current.Column + current.ColumnSpan);
            if (boundary < 0 || boundary > oldWidth)
                return false;

            var newWidth = checked(oldWidth + count);
            foreach (var group in reference.Table.RowGroups.Cast<TableRowGroup>())
            {
                if (!WriterTableGrid.TryBuild(reference.Table, group, out _))
                    return false;
            }
            var replacement = CloneTable(reference.Table);
            foreach (var group in replacement.RowGroups.Cast<TableRowGroup>())
            {
                if (!WriterTableGrid.TryBuild(replacement, group, out var grid))
                    return false;
                var placements = grid.Placements.Select(static item => item.Clone()).ToList();
                foreach (var item in placements)
                {
                    if (item.Column < boundary && item.ColumnSpan > boundary - item.Column)
                        item.ColumnSpan = checked(item.ColumnSpan + count);
                    else if (item.Column >= boundary)
                        item.Column = checked(item.Column + count);
                }

                AddMissingCells(placements, grid.Rows, 0, newWidth, grid.Rows.Count);
                EnsureColumns(replacement, newWidth);
                ReplaceRows(group, grid.Rows, placements);
            }

            if (!ReplaceTable(reference.Table, replacement))
                return false;
            caret = FindLogicalCaret(replacement, reference.GroupIndex, current.Row, boundary);
            return caret.HasValue;
        }, () => SetCaret(caret));
    }

    /// <summary>Inserts columns relative to the current editor cell.</summary>
    public bool InsertColumns(int count = 1, WriterTableInsertPlacement placement = WriterTableInsertPlacement.After)
    {
        ThrowIfDisposed();
        return TryGetCell(Editor.Selection.Start, out var reference) &&
            InsertColumns(reference, count, placement);
    }

    /// <summary>Deletes columns beginning at the supplied cell's logical column.</summary>
    public bool DeleteColumns(WriterTableCellReference reference, int count = 1)
    {
        ThrowIfDisposed();
        ValidateCount(count, nameof(count));
        WriterTableCellReference? caret = null;
        return Mutate(() =>
        {
            if (!TryGetCurrentGrid(reference, out var selectedGrid, out var current))
                return false;
            var width = GetTableColumnCount(reference.Table);
            if (width != selectedGrid.ColumnCount || count > width - current.Column ||
                width - count < 1)
                return false;

            var start = current.Column;
            var end = checked(start + count);
            foreach (var group in reference.Table.RowGroups.Cast<TableRowGroup>())
            {
                if (!WriterTableGrid.TryBuild(reference.Table, group, out var grid))
                    return false;
            }
            var replacement = CloneTable(reference.Table);
            EnsureColumns(replacement, width);
            // Reduce the declared table width before normalizing row coverage. Otherwise a
            // removed column would be re-created as a gap filler by NormalizeGroup.
            replacement.Columns.RemoveRange(start, count);
            foreach (var group in replacement.RowGroups.Cast<TableRowGroup>())
            {
                if (!WriterTableGrid.TryBuild(replacement, group, out var grid))
                    return false;
                var placements = RemoveColumns(grid.Placements, start, end);
                ReplaceRows(group, grid.Rows, placements);
                NormalizeGroup(replacement, group);
                if (group.Rows.Any(row => row.Cells.Count == 0) ||
                    !WriterTableGrid.TryBuild(replacement, group, out _))
                    return false;
            }

            if (!ReplaceTable(reference.Table, replacement))
                return false;
            caret = FindLogicalCaret(replacement, reference.GroupIndex, current.Row,
                Math.Min(start, width - count - 1));
            return caret.HasValue;
        }, () => SetCaret(caret));
    }

    /// <summary>Deletes columns beginning at the current editor cell's logical column.</summary>
    public bool DeleteColumns(int count = 1)
    {
        ThrowIfDisposed();
        return TryGetCell(Editor.Selection.Start, out var reference) && DeleteColumns(reference, count);
    }

    /// <summary>Merges a rectangular range whose cell boundaries align with the supplied cells.</summary>
    public bool TryMergeCells(WriterTableCellReference first, WriterTableCellReference second,
        out WriterTableCellReference merged)
    {
        ThrowIfDisposed();
        merged = default;
        if (!first.IsValid || !second.IsValid || !ReferenceEquals(first.Table, second.Table) ||
            !ReferenceEquals(first.RowGroup, second.RowGroup))
            return false;
        var range = WriterTableRange.Between(first, second);
        return TryMergeCells(range, out merged);
    }

    /// <summary>Merges an already-normalized rectangular table range.</summary>
    public bool TryMergeCells(WriterTableRange range, out WriterTableCellReference merged)
    {
        ThrowIfDisposed();
        merged = default;
        // A one-slot range names one cell, not a merge. Keep this rejection outside Mutate so
        // the native editor does not receive an empty undo unit or TextChanged notification.
        if (!range.IsValid || (range.StartRow == range.EndRow && range.StartColumn == range.EndColumn))
            return false;
        // A range that is already exactly occupied by one spanned cell is also a no-op. Resolve
        // this before entering Mutate because assigning the same spans and rebuilding rows would
        // otherwise still create native history and a TextChanged event.
        if (!TryGetGrid(range, out var existingGrid) || range.EndRow >= existingGrid.Rows.Count ||
            range.EndColumn >= existingGrid.ColumnCount)
            return false;
        var existingSelection = existingGrid.Placements.Where(item =>
                item.Row >= range.StartRow && item.RowSpan <= range.EndRow - item.Row + 1 &&
                item.Column >= range.StartColumn &&
                item.ColumnSpan <= range.EndColumn - item.Column + 1)
            .ToList();
        if (existingSelection.Count == 1 && existingSelection[0].Row == range.StartRow &&
            existingSelection[0].Column == range.StartColumn &&
            existingSelection[0].RowSpan == range.EndRow - range.StartRow + 1 &&
            existingSelection[0].ColumnSpan == range.EndColumn - range.StartColumn + 1)
            return false;
        WriterTableCellReference? result = null;
        var ok = Mutate(() =>
        {
            if (!TryGetGrid(range, out var grid) || !range.IsValid ||
                range.EndRow >= grid.Rows.Count || range.EndColumn >= grid.ColumnCount)
                return false;

            var selected = grid.Placements.Where(item =>
                    item.Row >= range.StartRow && item.RowSpan <= range.EndRow - item.Row + 1 &&
                    item.Column >= range.StartColumn &&
                    item.ColumnSpan <= range.EndColumn - item.Column + 1)
                .ToList();
            if (selected.Count == 0 || grid.Matrix is null)
                return false;

            var cells = new HashSet<TableCell>();
            for (var row = range.StartRow; row <= range.EndRow; row++)
            {
                for (var column = range.StartColumn; column <= range.EndColumn; column++)
                {
                    var cell = grid.Matrix[row, column];
                    if (cell is null)
                        return false;
                    cells.Add(cell);
                }
            }

            if (cells.Count != selected.Count || selected.Any(item =>
                    item.Row < range.StartRow || item.Column < range.StartColumn ||
                    item.RowSpan > range.EndRow - item.Row + 1 ||
                    item.ColumnSpan > range.EndColumn - item.Column + 1))
                return false;

            var target = selected.FirstOrDefault(item => item.Row == range.StartRow &&
                item.Column == range.StartColumn);
            if (target is null)
                return false;

            foreach (var item in selected.Where(item => !ReferenceEquals(item, target)))
                MoveBlocks(item.Cell, target.Cell);
            target.RowSpan = range.RowCount;
            target.ColumnSpan = range.ColumnCount;
            grid.Placements.RemoveAll(item => selected.Contains(item) && !ReferenceEquals(item, target));
            ReplaceRows(range.RowGroup, grid.Rows, grid.Placements);
            result = MakeReference(range.Table, range.RowGroup, target.Cell);
            return result.HasValue;
        }, () => SetCaret(result));
        if (ok && result.HasValue)
        {
            merged = result.Value;
            return true;
        }
        return false;
    }

    /// <summary>Merges the current selection when it describes one valid table rectangle.</summary>
    public bool TryMergeSelection(out WriterTableCellReference merged)
    {
        ThrowIfDisposed();
        merged = default;
        return TryGetSelectionRange(out var range) && TryMergeCells(range, out merged);
    }

    /// <summary>Splits a spanned cell into one cell per logical row and column.</summary>
    public bool TrySplitCell(WriterTableCellReference reference)
    {
        ThrowIfDisposed();
        WriterTableCellReference? caret = null;
        return Mutate(() =>
        {
            if (!TryGetCurrentGrid(reference, out var grid, out var current) ||
                (current.RowSpan == 1 && current.ColumnSpan == 1))
                return false;

            var target = grid.Placements.First(item => ReferenceEquals(item.Cell, current.Cell));
            var originalRowSpan = target.RowSpan;
            var originalColumnSpan = target.ColumnSpan;
            target.RowSpan = 1;
            target.ColumnSpan = 1;
            var additions = new List<WriterTableCellPlacement>();
            var rowEnd = checked(current.Row + originalRowSpan);
            var columnEnd = checked(current.Column + originalColumnSpan);
            for (var row = current.Row; row < rowEnd; row++)
            {
                for (var column = current.Column; column < columnEnd; column++)
                {
                    if (row == current.Row && column == current.Column)
                        continue;
                    additions.Add(new WriterTableCellPlacement(CreateCellLike(current.Cell), row, column, 1, 1));
                }
            }
            grid.Placements.AddRange(additions);
            ReplaceRows(grid.RowGroup, grid.Rows, grid.Placements);
            caret = MakeReference(grid.Table, grid.RowGroup, target.Cell);
            return caret.HasValue;
        }, () => SetCaret(caret));
    }

    /// <summary>Splits the table cell containing the current caret.</summary>
    public bool TrySplitCurrentCell()
    {
        ThrowIfDisposed();
        return TryGetCellAtCaret(out var reference) && TrySplitCell(reference);
    }

    /// <summary>Handles Tab when the caret is at the final cell of its table.</summary>
    /// <returns><see langword="true"/> when a new row was created and focus moved to it.</returns>
    public bool TryHandleFinalCellTab()
    {
        ThrowIfDisposed();
        if (!Editor.Selection.IsEmpty || !TryGetCell(Editor.Selection.Start, out var reference) ||
            !IsAtCellEnd(reference.Cell))
            return false;
        if (!TryGetCurrentGrid(reference, out var grid, out var current) ||
            grid.Matrix is null || grid.Rows.Count == 0 || grid.ColumnCount == 0 ||
            !ReferenceEquals(grid.Matrix[grid.Rows.Count - 1, grid.ColumnCount - 1], current.Cell))
            return false;
        if (!ReferenceEquals(reference.RowGroup, reference.Table.RowGroups[^1]) ||
            checked(current.Row + current.RowSpan) < grid.Rows.Count)
            return false;

        WriterTableCellReference? caret = null;
        return Mutate(() =>
        {
            if (!InsertRowsCore(grid, checked(current.Row + current.RowSpan), 1, out caret))
                return false;
            return caret.HasValue;
        }, () => SetCaret(caret));
    }

    /// <summary>Alias used by keyboard projections for final-cell Tab handling.</summary>
    public bool HandleTab() => TryHandleFinalCellTab();

    /// <summary>Sets the text alignment of a cell.</summary>
    public bool SetCellAlignment(WriterTableCellReference reference, TextAlignment alignment)
    {
        ThrowIfDisposed();
        if (!Enum.IsDefined(alignment))
            throw new ArgumentOutOfRangeException(nameof(alignment), alignment,
                "Unknown text alignment.");
        return Mutate(() =>
        {
            if (!TryGetCell(reference.Cell, out var current) || current.Cell.TextAlignment == alignment)
                return false;
            current.Cell.TextAlignment = alignment;
            return true;
        });
    }

    /// <summary>Sets the text alignment of every cell intersecting a table range.</summary>
    public bool SetCellAlignment(WriterTableRange range, TextAlignment alignment)
    {
        ThrowIfDisposed();
        if (!Enum.IsDefined(alignment))
            throw new ArgumentOutOfRangeException(nameof(alignment), alignment,
                "Unknown text alignment.");
        return Mutate(() =>
        {
            if (!TryGetRangeCells(range, out var cells))
                return false;
            var changed = false;
            foreach (var cell in cells.Where(cell => cell.TextAlignment != alignment))
            {
                cell.TextAlignment = alignment;
                changed = true;
            }
            return changed;
        });
    }

    /// <summary>
    /// Redistributes a cell's existing vertical padding to place its content at the top, center,
    /// or bottom without changing horizontal padding or row height.
    /// </summary>
    public bool SetCellVerticalAlignment(WriterTableCellReference reference,
        WriterTableCellVerticalAlignment alignment)
    {
        ThrowIfDisposed();
        if (!Enum.IsDefined(alignment))
            throw new ArgumentOutOfRangeException(nameof(alignment), alignment,
                "Unknown cell vertical alignment.");
        return Mutate(() =>
        {
            if (!TryGetCell(reference.Cell, out var current))
                return false;
            var padding = current.Cell.Padding;
            var vertical = Math.Max(0, padding.Top + padding.Bottom);
            var top = alignment switch
            {
                WriterTableCellVerticalAlignment.Top => 0,
                WriterTableCellVerticalAlignment.Center => vertical / 2d,
                WriterTableCellVerticalAlignment.Bottom => vertical,
                _ => throw new ArgumentOutOfRangeException(nameof(alignment))
            };
            var adjusted = new Thickness(padding.Left, top, padding.Right, vertical - top);
            if (adjusted == padding)
                return false;
            current.Cell.Padding = adjusted;
            return true;
        });
    }

    /// <summary>Sets the vertical content alignment of every cell intersecting a table range.</summary>
    public bool SetCellVerticalAlignment(WriterTableRange range,
        WriterTableCellVerticalAlignment alignment)
    {
        ThrowIfDisposed();
        if (!Enum.IsDefined(alignment))
            throw new ArgumentOutOfRangeException(nameof(alignment), alignment,
                "Unknown cell vertical alignment.");
        return Mutate(() =>
        {
            if (!TryGetRangeCells(range, out var cells))
                return false;
            var changed = false;
            foreach (var cell in cells)
            {
                var padding = cell.Padding;
                var vertical = Math.Max(0, padding.Top + padding.Bottom);
                var top = alignment switch
                {
                    WriterTableCellVerticalAlignment.Top => 0,
                    WriterTableCellVerticalAlignment.Center => vertical / 2d,
                    WriterTableCellVerticalAlignment.Bottom => vertical,
                    _ => throw new ArgumentOutOfRangeException(nameof(alignment))
                };
                var adjusted = new Thickness(padding.Left, top, padding.Right, vertical - top);
                if (adjusted == padding)
                    continue;
                cell.Padding = adjusted;
                changed = true;
            }
            return changed;
        });
    }

    /// <summary>Sets the padding of a cell.</summary>
    public bool SetCellPadding(WriterTableCellReference reference, Thickness padding)
    {
        ThrowIfDisposed();
        ValidateThickness(padding, nameof(padding));
        return Mutate(() =>
        {
            if (!TryGetCell(reference.Cell, out var current) || current.Cell.Padding == padding)
                return false;
            current.Cell.Padding = padding;
            return true;
        });
    }

    /// <summary>Sets a cell's border brush and thickness.</summary>
    public bool SetCellBorder(WriterTableCellReference reference, Brush? brush, Thickness thickness)
    {
        ThrowIfDisposed();
        ValidateThickness(thickness, nameof(thickness));
        return Mutate(() =>
        {
            if (!TryGetCell(reference.Cell, out var current) ||
                Equals(current.Cell.BorderBrush, brush) && current.Cell.BorderThickness == thickness)
                return false;
            current.Cell.BorderBrush = brush;
            current.Cell.BorderThickness = thickness;
            return true;
        });
    }

    /// <summary>Sets a cell background brush; pass <see langword="null"/> to clear it.</summary>
    public bool SetCellBackground(WriterTableCellReference reference, Brush? brush)
    {
        ThrowIfDisposed();
        return Mutate(() =>
        {
            if (!TryGetCell(reference.Cell, out var current) ||
                Equals(current.Cell.Background, brush))
                return false;
            current.Cell.Background = brush;
            return true;
        });
    }

    /// <summary>Sets a table background brush; pass <see langword="null"/> to clear it.</summary>
    public bool SetTableBackground(Table table, Brush? brush)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        return Mutate(() =>
        {
            if (!IsTableInDocument(table) || Equals(table.Background, brush))
                return false;
            table.Background = brush;
            return true;
        });
    }

    /// <summary>Sets the text alignment inherited by a table's cells.</summary>
    public bool SetTableAlignment(Table table, TextAlignment alignment)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        if (!Enum.IsDefined(alignment))
            throw new ArgumentOutOfRangeException(nameof(alignment), alignment,
                "Unknown text alignment.");
        return Mutate(() =>
        {
            if (!IsTableInDocument(table) || table.TextAlignment == alignment)
                return false;
            table.TextAlignment = alignment;
            return true;
        });
    }

    /// <summary>Places a table horizontally without changing text alignment inside its cells.</summary>
    public bool SetTableHorizontalAlignment(Table table, WriterTableHorizontalAlignment alignment,
        double tableWidth, double availableWidth)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        if (!Enum.IsDefined(alignment))
            throw new ArgumentOutOfRangeException(nameof(alignment), alignment,
                "Unknown table alignment.");
        if (!double.IsFinite(tableWidth) || tableWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(tableWidth));
        if (!double.IsFinite(availableWidth) || availableWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(availableWidth));

        var remaining = Math.Max(0, availableWidth - tableWidth);
        var (left, right) = alignment switch
        {
            WriterTableHorizontalAlignment.Left => (0d, remaining),
            WriterTableHorizontalAlignment.Center => (remaining / 2d, remaining / 2d),
            WriterTableHorizontalAlignment.Right => (remaining, 0d),
            _ => throw new ArgumentOutOfRangeException(nameof(alignment))
        };
        return Mutate(() =>
        {
            if (!IsTableInDocument(table))
                return false;
            var top = double.IsFinite(table.Margin.Top) ? table.Margin.Top : 0;
            var bottom = double.IsFinite(table.Margin.Bottom) ? table.Margin.Bottom : 0;
            var margin = new Thickness(left, top, right, bottom);
            if (table.Margin == margin)
                return false;
            table.Margin = margin;
            return true;
        });
    }

    /// <summary>Sets a table's border brush and thickness.</summary>
    public bool SetTableBorder(Table table, Brush? brush, Thickness thickness)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        ValidateThickness(thickness, nameof(thickness));
        return Mutate(() =>
        {
            if (!IsTableInDocument(table) ||
                Equals(table.BorderBrush, brush) && table.BorderThickness == thickness)
                return false;
            table.BorderBrush = brush;
            table.BorderThickness = thickness;
            return true;
        });
    }

    /// <summary>Sets the outer table frame and every cell border in one mutation.</summary>
    public bool SetAllTableBorders(Table table, Brush? brush, Thickness tableThickness,
        Thickness cellThickness)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        ValidateThickness(tableThickness, nameof(tableThickness));
        ValidateThickness(cellThickness, nameof(cellThickness));
        return Mutate(() =>
        {
            if (!IsTableInDocument(table))
                return false;
            var cells = table.RowGroups.Cast<TableRowGroup>()
                .SelectMany(static group => group.Rows.Cast<TableRow>())
                .SelectMany(static row => row.Cells.Cast<TableCell>())
                .ToArray();
            if (Equals(table.BorderBrush, brush) && table.BorderThickness == tableThickness
                && cells.All(cell => Equals(cell.BorderBrush, brush)
                    && cell.BorderThickness == cellThickness))
                return false;

            table.BorderBrush = brush;
            table.BorderThickness = tableThickness;
            foreach (var cell in cells)
            {
                cell.BorderBrush = brush;
                cell.BorderThickness = cellThickness;
            }
            return true;
        });
    }

    /// <summary>Sets a table column's width.</summary>
    public bool SetColumnWidth(Table table, int column, GridLength width)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        ValidateGridLength(width, nameof(width));
        WriterTableCellReference? caret = null;
        return Mutate(() =>
        {
            if (!IsTableInDocument(table) || column < 0 ||
                !TryGetLogicalTableWidth(table, out var logicalWidth) || column >= logicalWidth)
                return false;
            var active = TryGetCell(Editor.Selection.Start, out var activeReference) &&
                ReferenceEquals(activeReference.Table, table)
                ? activeReference
                : default;
            var hasColumn = column < table.Columns.Count;
            if (hasColumn && table.Columns[column].Width == width)
                return false;
            var replacement = CloneTable(table);
            EnsureColumns(replacement, column + 1);
            replacement.Columns[column].Width = width;
            if (!ReplaceTable(table, replacement))
                return false;
            if (active.IsValid)
                caret = FindLogicalCaret(replacement, active.GroupIndex, active.Row, active.Column);
            return true;
        }, () => SetCaret(caret));
    }

    /// <summary>Sets every column occupied by a cell to the supplied width.</summary>
    public bool SetCellWidth(WriterTableCellReference reference, GridLength width)
    {
        ThrowIfDisposed();
        ValidateGridLength(width, nameof(width));
        WriterTableCellReference? caret = null;
        return Mutate(() =>
        {
            if (!TryGetCurrentGrid(reference, out var grid, out var current))
                return false;
            var changed = false;
            var cellColumnEnd = checked(current.Column + current.ColumnSpan);
            for (var i = current.Column; i < cellColumnEnd; i++)
            {
                if (i >= reference.Table.Columns.Count || reference.Table.Columns[i].Width != width)
                    changed = true;
            }
            if (!changed)
                return false;
            var active = TryGetCell(Editor.Selection.Start, out var activeReference) &&
                ReferenceEquals(activeReference.Table, reference.Table)
                ? activeReference
                : default;
            var replacement = CloneTable(reference.Table);
            EnsureColumns(replacement, cellColumnEnd);
            for (var i = current.Column; i < cellColumnEnd; i++)
                replacement.Columns[i].Width = width;
            if (!ReplaceTable(reference.Table, replacement))
                return false;
            if (active.IsValid)
                caret = FindLogicalCaret(replacement, active.GroupIndex, active.Row, active.Column);
            return true;
        }, () => SetCaret(caret));
    }

    internal bool ApplyResize(Table table, IReadOnlyDictionary<int, double> columnWidths,
        IReadOnlyList<WriterTableCellPaddingAdjustment> cellPaddings)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(columnWidths);
        ArgumentNullException.ThrowIfNull(cellPaddings);
        foreach (var pair in columnWidths)
        {
            if (pair.Key < 0 || !double.IsFinite(pair.Value) || pair.Value <= 0)
                throw new ArgumentOutOfRangeException(nameof(columnWidths));
        }
        foreach (var adjustment in cellPaddings)
        {
            if (adjustment.GroupIndex < 0 || adjustment.Row < 0 || adjustment.Column < 0)
                throw new ArgumentOutOfRangeException(nameof(cellPaddings));
            ValidateThickness(adjustment.Padding, nameof(cellPaddings));
        }

        WriterTableCellReference? caret = null;
        return Mutate(() =>
        {
            if (!IsTableInDocument(table) || !TryGetLogicalTableWidth(table, out var logicalWidth)
                || columnWidths.Keys.Any(column => column >= logicalWidth))
                return false;
            var active = TryGetCell(Editor.Selection.Start, out var activeReference)
                && ReferenceEquals(activeReference.Table, table) ? activeReference : default;
            var replacement = CloneTable(table);
            var changed = false;
            if (columnWidths.Count > 0)
            {
                EnsureColumns(replacement, logicalWidth);
                foreach (var pair in columnWidths)
                {
                    var width = new GridLength(pair.Value, GridUnitType.Pixel);
                    if (replacement.Columns[pair.Key].Width == width)
                        continue;
                    replacement.Columns[pair.Key].Width = width;
                    changed = true;
                }
            }

            foreach (var adjustment in cellPaddings)
            {
                if (adjustment.GroupIndex >= replacement.RowGroups.Count)
                    return false;
                var group = replacement.RowGroups[adjustment.GroupIndex];
                if (!WriterTableGrid.TryBuild(replacement, group, out var grid))
                    return false;
                var placement = grid.Placements.FirstOrDefault(item =>
                    item.Row == adjustment.Row && item.Column == adjustment.Column);
                if (placement is null)
                    return false;
                if (placement.Cell.Padding == adjustment.Padding)
                    continue;
                placement.Cell.Padding = adjustment.Padding;
                changed = true;
            }

            if (!changed || !ReplaceTable(table, replacement))
                return false;
            if (active.IsValid)
                caret = FindLogicalCaret(replacement, active.GroupIndex, active.Row, active.Column);
            return true;
        }, () => SetCaret(caret));
    }

    /// <summary>
    /// Applies a minimum row height using symmetric cell padding. Native FlowDocument tables expose
    /// column width but no fixed row-height property.
    /// </summary>
    public bool SetRowHeight(WriterTableCellReference reference, double minimumHeight)
    {
        ThrowIfDisposed();
        ValidateDimension(minimumHeight, nameof(minimumHeight));
        return Mutate(() =>
        {
            if (!TryGetCurrentGrid(reference, out var grid, out var current))
                return false;
            var changed = false;
            foreach (var item in grid.Placements.Where(item => item.Row <= current.Row &&
                         item.RowSpan > current.Row - item.Row))
            {
                var padding = item.Cell.Padding;
                var vertical = Math.Max(padding.Top + padding.Bottom, minimumHeight);
                var extra = Math.Max(0, vertical - padding.Top - padding.Bottom) / 2;
                var next = new Thickness(padding.Left, padding.Top + extra,
                    padding.Right, padding.Bottom + extra);
                if (next != padding)
                {
                    item.Cell.Padding = next;
                    changed = true;
                }
            }
            return changed;
        });
    }

    /// <summary>Sets a column background brush; pass <see langword="null"/> to clear it.</summary>
    public bool SetColumnBackground(Table table, int column, Brush? brush)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        if (column < 0)
            throw new ArgumentOutOfRangeException(nameof(column), column,
                "The column must be non-negative.");
        WriterTableCellReference? caret = null;
        return Mutate(() =>
        {
            if (!IsTableInDocument(table) || !TryGetLogicalTableWidth(table, out var logicalWidth) ||
                column >= logicalWidth)
                return false;
            var active = TryGetCell(Editor.Selection.Start, out var activeReference) &&
                ReferenceEquals(activeReference.Table, table)
                ? activeReference
                : default;
            var hasColumn = column < table.Columns.Count;
            if (hasColumn && Equals(table.Columns[column].Background, brush))
                return false;
            var replacement = CloneTable(table);
            EnsureColumns(replacement, column + 1);
            replacement.Columns[column].Background = brush;
            if (!ReplaceTable(table, replacement))
                return false;
            if (active.IsValid)
                caret = FindLogicalCaret(replacement, active.GroupIndex, active.Row, active.Column);
            return true;
        }, () => SetCaret(caret));
    }

    /// <summary>Distributes a contiguous range of columns using equal pixel widths.</summary>
    public bool DistributeColumns(Table table, int firstColumn, int columnCount, double totalWidth)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        ValidateCount(columnCount, nameof(columnCount));
        ValidateDimension(totalWidth, nameof(totalWidth));
        if (totalWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalWidth), totalWidth,
                "The total width must be positive.");
        WriterTableCellReference? caret = null;
        return Mutate(() =>
        {
            if (!IsTableInDocument(table) || firstColumn < 0 ||
                !TryGetLogicalTableWidth(table, out var logicalWidth) ||
                firstColumn > logicalWidth - columnCount)
                return false;
            var width = new GridLength(totalWidth / columnCount, GridUnitType.Pixel);
            var changed = false;
            var columnEnd = checked(firstColumn + columnCount);
            for (var i = firstColumn; i < columnEnd; i++)
            {
                if (i >= table.Columns.Count || table.Columns[i].Width != width)
                    changed = true;
            }
            if (!changed)
                return false;
            var active = TryGetCell(Editor.Selection.Start, out var activeReference) &&
                ReferenceEquals(activeReference.Table, table)
                ? activeReference
                : default;
            var replacement = CloneTable(table);
            EnsureColumns(replacement, columnEnd);
            for (var i = firstColumn; i < columnEnd; i++)
                replacement.Columns[i].Width = width;
            if (!ReplaceTable(table, replacement))
                return false;
            if (active.IsValid)
                caret = FindLogicalCaret(replacement, active.GroupIndex, active.Row, active.Column);
            return true;
        }, () => SetCaret(caret));
    }

    /// <summary>Distributes a contiguous range of rows through symmetric minimum-height padding.</summary>
    public bool DistributeRows(WriterTableCellReference reference, int rowCount, double totalHeight)
    {
        ThrowIfDisposed();
        ValidateCount(rowCount, nameof(rowCount));
        ValidateDimension(totalHeight, nameof(totalHeight));
        if (totalHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalHeight), totalHeight,
                "The total height must be positive.");
        return Mutate(() =>
        {
            if (!TryGetCurrentGrid(reference, out var grid, out var current) ||
                checked(current.Row + rowCount) > grid.Rows.Count)
                return false;
            var each = totalHeight / rowCount;
            var changed = false;
            var rowEnd = checked(current.Row + rowCount);
            for (var row = current.Row; row < rowEnd; row++)
            {
                foreach (var item in grid.Placements.Where(item => item.Row <= row &&
                             item.RowSpan > row - item.Row))
                {
                    var padding = item.Cell.Padding;
                    var vertical = Math.Max(padding.Top + padding.Bottom, each);
                    var extra = Math.Max(0, vertical - padding.Top - padding.Bottom) / 2;
                    var next = new Thickness(padding.Left, padding.Top + extra,
                        padding.Right, padding.Bottom + extra);
                    if (next != padding)
                    {
                        item.Cell.Padding = next;
                        changed = true;
                    }
                }
            }
            return changed;
        });
    }

    /// <summary>Releases the service's editor reference.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
    }

    private bool TryInsertTableAtCaret(int rows, int columns, Brush borderBrush, out Table? table)
    {
        table = new Table
        {
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1)
        };
        for (var column = 0; column < columns; column++)
            table.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        for (var row = 0; row < rows; row++)
        {
            var tableRow = new TableRow();
            for (var column = 0; column < columns; column++)
                tableRow.Cells.Add(CreateEmptyCell(borderBrush, new Thickness(0.5)));
            group.Rows.Add(tableRow);
        }
        table.RowGroups.Add(group);

        var paragraph = Editor.Selection.Start.Paragraph;
        if (paragraph is null)
        {
            Editor.Document.Blocks.Add(table);
            return true;
        }

        var caret = Editor.Selection.Start;
        var paragraphStart = paragraph.ContentStart.GetInsertionPosition(LogicalDirection.Forward);
        var paragraphEnd = paragraph.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
        var insertBefore = caret.CompareTo(paragraphStart) <= 0;
        var insertAfter = caret.CompareTo(paragraphEnd) >= 0 ||
            caret.CompareTo(paragraph.ContentEnd) >= 0;
        // A Table is a Block and cannot occupy the middle of a Paragraph without changing the
        // paragraph's inline structure. Keep the operation explicitly unchanged there; the
        // future UI projection can offer a split-paragraph command when that behavior is wanted.
        if (!insertBefore && !insertAfter)
            return false;

        var owner = paragraph.Parent;
        switch (owner)
        {
            case FlowDocument document:
                if (insertBefore) document.Blocks.InsertBefore(paragraph, table);
                else document.Blocks.InsertAfter(paragraph, table);
                return true;
            case TableCell cell:
                if (insertBefore) cell.Blocks.InsertBefore(paragraph, table);
                else cell.Blocks.InsertAfter(paragraph, table);
                return true;
            case Section section:
                if (insertBefore) section.Blocks.InsertBefore(paragraph, table);
                else section.Blocks.InsertAfter(paragraph, table);
                return true;
            case ListItem item:
                if (insertBefore) item.Blocks.InsertBefore(paragraph, table);
                else item.Blocks.InsertAfter(paragraph, table);
                return true;
            case Figure figure:
                if (insertBefore) figure.Blocks.InsertBefore(paragraph, table);
                else figure.Blocks.InsertAfter(paragraph, table);
                return true;
            case Floater floater:
                if (insertBefore) floater.Blocks.InsertBefore(paragraph, table);
                else floater.Blocks.InsertAfter(paragraph, table);
                return true;
            default:
                return false;
        }
    }

    private bool InsertRowsCore(WriterTableGrid grid, int boundary, int count,
        out WriterTableCellReference? caret)
    {
        caret = null;
        if (boundary < 0 || boundary > grid.Rows.Count)
            return false;
        var placements = grid.Placements.Select(static item => item.Clone()).ToList();
        foreach (var item in placements)
        {
            if (item.Row < boundary && item.RowSpan > boundary - item.Row)
                item.RowSpan = checked(item.RowSpan + count);
            else if (item.Row >= boundary)
                item.Row = checked(item.Row + count);
        }
        var rows = grid.Rows.ToList();
        var insertedRows = Enumerable.Range(0, count).Select(_ => new TableRow()).ToList();
        rows.InsertRange(boundary, insertedRows);
        var width = Math.Max(1, grid.ColumnCount);
        AddMissingCells(placements, insertedRows, boundary, width, count);
        EnsureColumns(grid.Table, width);
        ReplaceRows(grid.RowGroup, rows, placements);
        caret = MakeCaretReference(grid.Table, grid.RowGroup, boundary, 0);
        return caret.HasValue;
    }

    private bool TryGetCurrentGrid(WriterTableCellReference reference, out WriterTableGrid grid,
        out WriterTableCellPlacement current)
    {
        grid = null!;
        current = null!;
        if (!reference.IsValid || !TryGetCell(reference.Cell, out var resolved) ||
            !ReferenceEquals(resolved.Table, reference.Table) ||
            !ReferenceEquals(resolved.RowGroup, reference.RowGroup) ||
            !WriterTableGrid.TryBuild(reference.Table, reference.RowGroup, out grid))
            return false;
        current = grid.Placements.FirstOrDefault(item => ReferenceEquals(item.Cell, resolved.Cell))!;
        return current is not null;
    }

    private bool TryGetGrid(WriterTableRange range, out WriterTableGrid grid)
    {
        grid = null!;
        return range.IsValid && IsTableInDocument(range.Table) &&
            range.Table.RowGroups.Contains(range.RowGroup) &&
            WriterTableGrid.TryBuild(range.Table, range.RowGroup, out grid);
    }

    private bool TryGetRangeCells(WriterTableRange range, out IReadOnlyList<TableCell> cells)
    {
        cells = Array.Empty<TableCell>();
        if (!TryGetGrid(range, out var grid) || grid.Matrix is null
            || range.EndRow >= grid.Rows.Count || range.EndColumn >= grid.ColumnCount)
            return false;
        var selected = new HashSet<TableCell>();
        for (var row = range.StartRow; row <= range.EndRow; row++)
        {
            for (var column = range.StartColumn; column <= range.EndColumn; column++)
            {
                var cell = grid.Matrix[row, column];
                if (cell is null)
                    return false;
                selected.Add(cell);
            }
        }
        cells = selected.ToArray();
        return cells.Count > 0;
    }

    private WriterTableCellReference? MakeCaretReference(Table table, TableRowGroup group,
        int row, int column)
    {
        if (!WriterTableGrid.TryBuild(table, group, out var grid) || grid.Rows.Count == 0)
            return null;
        row = Math.Clamp(row, 0, grid.Rows.Count - 1);
        column = Math.Clamp(column, 0, Math.Max(0, grid.ColumnCount - 1));
        var cell = grid.Matrix?[row, column];
        return cell is null ? null : MakeReference(table, group, cell, grid);
    }

    private WriterTableCellReference MakeReference(Table table, TableRowGroup group, TableCell cell)
    {
        if (!WriterTableGrid.TryBuild(table, group, out var grid))
            throw new InvalidOperationException("The table no longer has a valid grid.");
        var placement = grid.Placements.FirstOrDefault(item => ReferenceEquals(item.Cell, cell))
            ?? throw new InvalidOperationException("The cell is not part of the table grid.");
        return MakeReference(table, group, cell, placement, grid);
    }

    private WriterTableCellReference MakeReference(Table table, TableRowGroup group, TableCell cell,
        WriterTableGrid grid)
    {
        var placement = grid.Placements.First(item => ReferenceEquals(item.Cell, cell));
        return MakeReference(table, group, cell, placement, grid);
    }

    private WriterTableCellReference MakeReference(Table table, TableRowGroup group, TableCell cell,
        WriterTableCellPlacement placement, WriterTableGrid grid)
    {
        var groupIndex = table.RowGroups.IndexOf(group);
        return new WriterTableCellReference(table, group, cell, groupIndex, placement.Row,
            placement.Column, placement.RowSpan, placement.ColumnSpan);
    }

    private void SetCaret(WriterTableCellReference? reference)
    {
        if (!reference.HasValue)
            return;
        var cell = reference.Value.Cell;
        var position = cell.ContentStart.GetInsertionPosition(LogicalDirection.Forward);
        Editor.Selection.Select(position, position);
        Editor.Focus();
    }

    private bool IsAtCellEnd(TableCell cell)
    {
        var caret = Editor.Selection.Start;
        var end = cell.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
        return caret.CompareTo(end) >= 0 || caret.CompareTo(cell.ContentEnd) == 0;
    }

    private bool Mutate(Func<bool> mutation, Action? restoreCaret = null)
    {
        if (!CanMutate)
            return false;
        WriterTableDocumentSnapshot before;
        try
        {
            before = CaptureSnapshot();
        }
        catch
        {
            return false;
        }

        var changed = false;
        var began = false;
        var documentChanged = false;
        TextChangedEventHandler onTextChanged = (_, _) => documentChanged = true;
        Editor.TextChanged += onTextChanged;
        try
        {
            Editor.BeginChange();
            began = true;
            changed = mutation();
        }
        catch
        {
            changed = false;
        }
        finally
        {
            try
            {
                if (began)
                    Editor.EndChange();
            }
            catch
            {
                changed = false;
            }
            Editor.TextChanged -= onTextChanged;
        }

        if (!changed)
        {
            // A false result after a native TextChanged event means a delegate edited before a
            // postcondition failed. Roll that edit back; ordinary invalid/no-op paths emit no event
            // and therefore retain their pointers, selection and native undo stack untouched.
            if (documentChanged)
                try { RestoreSnapshot(before); } catch { }
            return false;
        }

        try
        {
            restoreCaret?.Invoke();
            return true;
        }
        catch
        {
            try { RestoreSnapshot(before); } catch { }
            return false;
        }
    }

    private WriterTableDocumentSnapshot CaptureSnapshot()
    {
        var document = Editor.Document;
        var startOffset = document.ContentStart.GetOffsetToPosition(Editor.Selection.Start);
        var endOffset = document.ContentStart.GetOffsetToPosition(Editor.Selection.End);
        using var stream = new MemoryStream();
        new TextRange(document.ContentStart, document.ContentEnd).Save(stream, DataFormats.XamlPackage);
        return new WriterTableDocumentSnapshot(stream.ToArray(), startOffset, endOffset);
    }

    private void RestoreSnapshot(WriterTableDocumentSnapshot snapshot)
    {
        using var stream = new MemoryStream(snapshot.Bytes, writable: false);
        var document = Editor.Document;
        new TextRange(document.ContentStart, document.ContentEnd).Load(stream, DataFormats.XamlPackage);
        var max = document.ContentStart.GetOffsetToPosition(document.ContentEnd);
        var start = document.ContentStart.GetPositionAtOffset(
            Math.Clamp(snapshot.StartOffset, 0, max), LogicalDirection.Forward) ?? document.ContentEnd;
        var end = document.ContentStart.GetPositionAtOffset(
            Math.Clamp(snapshot.EndOffset, 0, max), LogicalDirection.Forward) ?? document.ContentEnd;
        if (start.CompareTo(end) > 0)
            (start, end) = (end, start);
        Editor.Selection.Select(start, end);
    }

    private bool IsTableInDocument(Table table) => EnumerateTables(Editor.Document)
        .Any(candidate => ReferenceEquals(candidate, table));

    private static void ReplaceRows(TableRowGroup group, IReadOnlyList<TableRow> rows,
        IReadOnlyList<WriterTableCellPlacement> placements)
    {
        foreach (var row in rows)
            row.Cells.Clear();
        foreach (var placement in placements.OrderBy(item => item.Row).ThenBy(item => item.Column))
        {
            placement.Cell.RowSpan = placement.RowSpan;
            placement.Cell.ColumnSpan = placement.ColumnSpan;
            if (placement.Row >= 0 && placement.Row < rows.Count)
                rows[placement.Row].Cells.Add(placement.Cell);
        }
        // The collection may contain the same row instances before or after a caller changes its
        // TableRowGroup. This assignment is intentionally idempotent for direct collection edits.
        if (group.Rows.Count != rows.Count || !group.Rows.SequenceEqual(rows))
        {
            group.Rows.Clear();
            foreach (var row in rows)
                group.Rows.Add(row);
        }
    }

    private static List<WriterTableCellPlacement> RemoveRows(
        IReadOnlyList<WriterTableCellPlacement> source, int start, int end)
    {
        var result = new List<WriterTableCellPlacement>();
        foreach (var original in source)
        {
            var surviving = Enumerable.Range(original.Row, original.RowSpan)
                .Where(row => row < start || row >= end).ToArray();
            if (surviving.Length == 0)
                continue;
            var firstSurvivor = surviving[0];
            var newRow = firstSurvivor < start ? firstSurvivor : firstSurvivor - (end - start);
            result.Add(new WriterTableCellPlacement(original.Cell, newRow, original.Column,
                surviving.Length, original.ColumnSpan));
        }
        return result;
    }

    private static List<WriterTableCellPlacement> RemoveColumns(
        IReadOnlyList<WriterTableCellPlacement> source, int start, int end)
    {
        var result = new List<WriterTableCellPlacement>();
        foreach (var original in source)
        {
            var surviving = Enumerable.Range(original.Column, original.ColumnSpan)
                .Where(column => column < start || column >= end).ToArray();
            if (surviving.Length == 0)
                continue;
            var firstSurvivor = surviving[0];
            var newColumn = firstSurvivor < start ? firstSurvivor : firstSurvivor - (end - start);
            result.Add(new WriterTableCellPlacement(original.Cell, original.Row, newColumn,
                original.RowSpan, surviving.Length));
        }
        return result;
    }

    private static void AddMissingCells(ICollection<WriterTableCellPlacement> placements,
        IReadOnlyList<TableRow> rows, int firstRow, int width, int rowCount)
    {
        var styleCandidates = placements.ToArray();
        var endRow = checked(firstRow + rowCount);
        for (var row = firstRow; row < endRow; row++)
        {
            for (var column = 0; column < width; column++)
            {
                if (placements.Any(item => item.Row <= row && item.RowSpan > row - item.Row &&
                        item.Column <= column && item.ColumnSpan > column - item.Column))
                    continue;
                var styleSource = styleCandidates
                    .OrderBy(item => DistanceFromSpan(item.Row, item.RowSpan, row))
                    .ThenBy(item => DistanceFromSpan(item.Column, item.ColumnSpan, column))
                    .Select(static item => item.Cell)
                    .FirstOrDefault();
                var cell = styleSource is null ? CreateEmptyCell() : CreateCellLike(styleSource);
                placements.Add(new WriterTableCellPlacement(cell, row, column, 1, 1));
            }
        }
    }

    private static int DistanceFromSpan(int start, int span, int value)
    {
        if (value < start)
            return start - value;
        var end = checked(start + span);
        return value >= end ? value - end + 1 : 0;
    }

    private static void NormalizeGroup(Table table, TableRowGroup group)
    {
        if (!WriterTableGrid.TryBuild(table, group, out var grid))
            return;
        var placements = grid.Placements.Select(static item => item.Clone()).ToList();
        AddMissingCells(placements, grid.Rows, 0, Math.Max(1, grid.ColumnCount), grid.Rows.Count);
        ReplaceRows(group, grid.Rows, placements);
        EnsureColumns(table, Math.Max(1, grid.ColumnCount));
    }

    private static void EnsureColumns(Table table, int count)
    {
        while (table.Columns.Count < count)
            table.Columns.Add(new TableColumn());
    }

    private Table CloneTable(Table source)
    {
        // The bounded W3-B clone contract is native FlowDocument content that the package serializer
        // can materialize: paragraphs/runs, hyperlinks, inline UI images with frozen in-memory
        // sources, nested tables, and local resource dictionaries. Unsupported custom/external
        // objects throw before the original table is replaced, so the mutation remains unchanged.
        var tables = EnumerateTables(Editor.Document).ToList();
        var ordinal = tables.FindIndex(candidate => ReferenceEquals(candidate, source));
        if (ordinal < 0)
            throw new InvalidOperationException("The native table is not in the editor document.");

        using var stream = new MemoryStream();
        new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd)
            .Save(stream, DataFormats.XamlPackage);
        stream.Position = 0;
        var cloneDocument = new FlowDocument();
        new TextRange(cloneDocument.ContentStart, cloneDocument.ContentEnd)
            .Load(stream, DataFormats.XamlPackage);
        var clonedTables = EnumerateTables(cloneDocument).ToList();
        var clone = clonedTables.ElementAtOrDefault(ordinal) ??
            throw new InvalidOperationException("The native table could not be cloned.");
        CopyTableResources(tables, clonedTables);
        if (!DetachTable(clone))
            throw new InvalidOperationException("The cloned table has no detachable parent.");
        return clone;
    }

    private static void CopyTableResources(IReadOnlyList<Table> sources,
        IReadOnlyList<Table> clones)
    {
        var count = Math.Min(sources.Count, clones.Count);
        for (var tableIndex = 0; tableIndex < count; tableIndex++)
        {
            var source = sources[tableIndex];
            var clone = clones[tableIndex];
            CopyLocalResources(source.Resources, clone.Resources);
            var groupCount = Math.Min(source.RowGroups.Count, clone.RowGroups.Count);
            for (var groupIndex = 0; groupIndex < groupCount; groupIndex++)
            {
                var sourceRows = source.RowGroups[groupIndex].Rows;
                var cloneRows = clone.RowGroups[groupIndex].Rows;
                var rowCount = Math.Min(sourceRows.Count, cloneRows.Count);
                for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    var sourceCells = sourceRows[rowIndex].Cells;
                    var cloneCells = cloneRows[rowIndex].Cells;
                    var cellCount = Math.Min(sourceCells.Count, cloneCells.Count);
                    for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
                        CopyLocalResources(sourceCells[cellIndex].Resources,
                            cloneCells[cellIndex].Resources);
                }
            }
        }
    }

    private static void CopyLocalResources(ResourceDictionary source, ResourceDictionary clone)
    {
        foreach (var key in source.Keys)
            clone[key] = source[key];
    }

    private static bool DetachTable(Table table)
    {
        switch (table.Parent)
        {
            case FlowDocument document:
                document.Blocks.Remove(table);
                return true;
            case TableCell cell:
                cell.Blocks.Remove(table);
                return true;
            case Section section:
                section.Blocks.Remove(table);
                return true;
            case ListItem item:
                item.Blocks.Remove(table);
                return true;
            case Figure figure:
                figure.Blocks.Remove(table);
                return true;
            case Floater floater:
                floater.Blocks.Remove(table);
                return true;
            default:
                return false;
        }
    }

    private static bool ReplaceTable(Table original, Table replacement)
    {
        switch (original.Parent)
        {
            case FlowDocument document:
                document.Blocks.InsertBefore(original, replacement);
                document.Blocks.Remove(original);
                return true;
            case TableCell cell:
                cell.Blocks.InsertBefore(original, replacement);
                cell.Blocks.Remove(original);
                return true;
            case Section section:
                section.Blocks.InsertBefore(original, replacement);
                section.Blocks.Remove(original);
                return true;
            case ListItem item:
                item.Blocks.InsertBefore(original, replacement);
                item.Blocks.Remove(original);
                return true;
            case Figure figure:
                figure.Blocks.InsertBefore(original, replacement);
                figure.Blocks.Remove(original);
                return true;
            case Floater floater:
                floater.Blocks.InsertBefore(original, replacement);
                floater.Blocks.Remove(original);
                return true;
            default:
                return false;
        }
    }

    private static bool ReplaceTableWithBlock(Table original, Block replacement)
    {
        switch (original.Parent)
        {
            case FlowDocument document:
                document.Blocks.InsertBefore(original, replacement);
                document.Blocks.Remove(original);
                return true;
            case TableCell cell:
                cell.Blocks.InsertBefore(original, replacement);
                cell.Blocks.Remove(original);
                return true;
            case Section section:
                section.Blocks.InsertBefore(original, replacement);
                section.Blocks.Remove(original);
                return true;
            case ListItem item:
                item.Blocks.InsertBefore(original, replacement);
                item.Blocks.Remove(original);
                return true;
            case Figure figure:
                figure.Blocks.InsertBefore(original, replacement);
                figure.Blocks.Remove(original);
                return true;
            case Floater floater:
                floater.Blocks.InsertBefore(original, replacement);
                floater.Blocks.Remove(original);
                return true;
            default:
                return false;
        }
    }

    private static TableRowGroup GetGroup(Table table, int groupIndex) =>
        groupIndex >= 0 && groupIndex < table.RowGroups.Count
            ? table.RowGroups[groupIndex]
            : throw new InvalidOperationException("The cloned table has a different row-group shape.");

    private static WriterTableCellReference? FindLogicalCaret(Table table, int groupIndex,
        int row, int column)
    {
        var group = GetGroup(table, groupIndex);
        if (!WriterTableGrid.TryBuild(table, group, out var grid) || grid.Matrix is null ||
            row < 0 || row >= grid.Rows.Count || column < 0 || column >= grid.ColumnCount ||
            grid.Matrix[row, column] is not { } cell)
            return null;
        return new WriterTableCellReference(table, group, cell, groupIndex,
            grid.Placements.First(item => ReferenceEquals(item.Cell, cell)).Row,
            grid.Placements.First(item => ReferenceEquals(item.Cell, cell)).Column,
            cell.RowSpan, cell.ColumnSpan);
    }

    private static int GetTableColumnCount(Table table)
    {
        return TryGetLogicalTableWidth(table, out var width) ? width : 1;
    }

    private static bool TryGetLogicalTableWidth(Table table, out int width)
    {
        width = 0;
        foreach (var group in table.RowGroups)
        {
            if (!WriterTableGrid.TryBuild(table, group, out var grid))
                return false;
            width = Math.Max(width, grid.ColumnCount);
        }
        width = Math.Max(1, width);
        return true;
    }

    private static TableCell CreateEmptyCell(Brush? borderBrush = null,
        Thickness? borderThickness = null)
    {
        var cell = new TableCell(new Paragraph(new Run()));
        if (borderBrush is not null)
            cell.BorderBrush = borderBrush;
        if (borderThickness is { } thickness)
            cell.BorderThickness = thickness;
        return cell;
    }

    private static TableCell CreateCellLike(TableCell source)
    {
        var result = CreateEmptyCell();
        result.Padding = source.Padding;
        result.BorderBrush = source.BorderBrush;
        result.BorderThickness = source.BorderThickness;
        result.Background = source.Background;
        result.TextAlignment = source.TextAlignment;
        result.FlowDirection = source.FlowDirection;
        result.LineHeight = source.LineHeight;
        result.LineStackingStrategy = source.LineStackingStrategy;
        return result;
    }

    private static void MoveBlocks(TableCell source, TableCell target)
    {
        foreach (var block in target.Blocks.ToList())
        {
            if (IsSemanticEmptyPlaceholder(block))
                target.Blocks.Remove(block);
        }
        foreach (var block in source.Blocks.ToList())
        {
            source.Blocks.Remove(block);
            if (!IsSemanticEmptyPlaceholder(block))
                target.Blocks.Add(block);
        }
        if (target.Blocks.Count == 0)
            target.Blocks.Add(new Paragraph(new Run()));
    }

    private static bool IsSemanticEmptyPlaceholder(Block block)
    {
        if (block is not Paragraph paragraph || HasLocalFormatting(paragraph,
                Paragraph.TextAlignmentProperty, Paragraph.MarginProperty, Paragraph.PaddingProperty,
                Paragraph.TextIndentProperty, Paragraph.LineHeightProperty,
                Paragraph.LineStackingStrategyProperty))
            return false;

        return paragraph.Inlines.Cast<Inline>().All(inline => inline is Run run &&
            string.IsNullOrEmpty(run.Text) && !HasLocalFormatting(run,
                TextElement.FontFamilyProperty, TextElement.FontSizeProperty,
                TextElement.FontStretchProperty, TextElement.FontStyleProperty,
                TextElement.FontWeightProperty, TextElement.ForegroundProperty,
                TextElement.BackgroundProperty, Inline.TextDecorationsProperty));
    }

    private static bool HasLocalFormatting(DependencyObject element,
        params DependencyProperty[] properties)
    {
        return properties.Any(property =>
            element.ReadLocalValue(property) != DependencyProperty.UnsetValue);
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T result)
                return result;
            current = GetParent(current);
        }
        return null;
    }

    private bool TryGetSelectionEdgeCell(TextPointer pointer, LogicalDirection inward,
        out WriterTableCellReference reference)
    {
        reference = default;
        try
        {
            if (TryGetCell(pointer, out var boundary))
            {
                var includesBoundary = inward == LogicalDirection.Forward
                    ? pointer.CompareTo(boundary.Cell.ContentEnd) < 0
                    : pointer.CompareTo(boundary.Cell.ContentStart
                        .GetInsertionPosition(LogicalDirection.Forward)
                        ?? boundary.Cell.ContentStart) > 0;
                if (includesBoundary)
                {
                    reference = boundary;
                    return true;
                }

                // A mouse cell selection can stop exactly at the next cell's ContentStart.
                // One symbol backward is still part of that cell's structural boundary, so
                // select the preceding physical cell instead of relying on pointer affinity.
                return TryGetAdjacentCell(boundary, inward, out reference);
            }

            var offset = inward == LogicalDirection.Forward ? 1 : -1;
            var inner = pointer.GetPositionAtOffset(offset, inward);
            return inner is not null && TryGetCell(inner, out reference);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private bool TryGetAdjacentCell(WriterTableCellReference boundary, LogicalDirection direction,
        out WriterTableCellReference reference)
    {
        reference = default;
        if (!WriterTableGrid.TryBuild(boundary.Table, boundary.RowGroup, out var grid))
            return false;
        var ordered = grid.Placements
            .OrderBy(item => item.Row)
            .ThenBy(item => item.Column)
            .ToList();
        var index = ordered.FindIndex(item => ReferenceEquals(item.Cell, boundary.Cell));
        if (index < 0)
            return false;
        index += direction == LogicalDirection.Forward ? 1 : -1;
        if (index < 0 || index >= ordered.Count)
            return false;
        var adjacent = ordered[index];
        reference = MakeReference(boundary.Table, boundary.RowGroup, adjacent.Cell, adjacent, grid);
        return true;
    }

    private static DependencyObject? GetParent(DependencyObject current) => current switch
    {
        FrameworkContentElement content => content.Parent,
        FrameworkElement element => element.Parent,
        _ => null
    };

    private static IEnumerable<Table> EnumerateTables(FlowDocument document)
    {
        return EnumerateTables(document.Blocks);
    }

    private static IEnumerable<Table> EnumerateTables(BlockCollection blocks)
    {
        foreach (var block in blocks)
        {
            if (block is Table table)
            {
                yield return table;
                foreach (var group in table.RowGroups)
                foreach (var row in group.Rows)
                foreach (var cell in row.Cells)
                foreach (var nested in EnumerateTables(cell.Blocks))
                    yield return nested;
            }
            else if (block is Section section)
            {
                foreach (var nested in EnumerateTables(section.Blocks))
                    yield return nested;
            }
            else if (block is List list)
            {
                foreach (var item in list.ListItems)
                foreach (var nested in EnumerateTables(item.Blocks))
                    yield return nested;
            }
            else if (block is Paragraph paragraph)
            {
                foreach (var nested in EnumerateInlineTables(paragraph.Inlines))
                    yield return nested;
            }
        }
    }

    private static IEnumerable<Table> EnumerateInlineTables(InlineCollection inlines)
    {
        foreach (var inline in inlines)
        {
            if (inline is Figure figure)
            {
                foreach (var nested in EnumerateTables(figure.Blocks))
                    yield return nested;
            }
            else if (inline is Floater floater)
            {
                foreach (var nested in EnumerateTables(floater.Blocks))
                    yield return nested;
            }
            else if (inline is Span span)
            {
                foreach (var nested in EnumerateInlineTables(span.Inlines))
                    yield return nested;
            }
        }
    }

    private static void ValidateCount(int count, string parameterName)
    {
        if (count is < 1 or > MaximumStructuralCount)
            throw new ArgumentOutOfRangeException(parameterName, count,
                $"The count must be between 1 and {MaximumStructuralCount}.");
    }

    private static void ValidatePlacement(WriterTableInsertPlacement placement)
    {
        if (!Enum.IsDefined(placement))
            throw new ArgumentOutOfRangeException(nameof(placement), placement,
                "Unknown table insertion placement.");
    }

    private static void ValidateDimension(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(parameterName, value,
                "The dimension must be finite and non-negative.");
    }

    private static void ValidateThickness(Thickness value, string parameterName)
    {
        if (!double.IsFinite(value.Left) || !double.IsFinite(value.Top) ||
            !double.IsFinite(value.Right) || !double.IsFinite(value.Bottom) ||
            value.Left < 0 || value.Top < 0 || value.Right < 0 || value.Bottom < 0)
            throw new ArgumentOutOfRangeException(parameterName, value,
                "Thickness values must be finite and non-negative.");
    }

    private static void ValidateGridLength(GridLength value, string parameterName)
    {
        if ((!value.IsAuto && !value.IsStar && !value.IsAbsolute) ||
            !double.IsFinite(value.Value) || value.Value < 0 || (value.IsStar && value.Value <= 0))
            throw new ArgumentOutOfRangeException(parameterName, value, "Invalid grid length.");
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}

internal sealed class WriterTableCellPlacement
{
    public WriterTableCellPlacement(TableCell cell, int row, int column, int rowSpan, int columnSpan)
    {
        Cell = cell;
        Row = row;
        Column = column;
        RowSpan = rowSpan;
        ColumnSpan = columnSpan;
    }

    public TableCell Cell { get; }
    public int Row { get; set; }
    public int Column { get; set; }
    public int RowSpan { get; set; }
    public int ColumnSpan { get; set; }

    public WriterTableCellPlacement Clone() => new(Cell, Row, Column, RowSpan, ColumnSpan);
}

internal sealed class WriterTableGrid
{
    private const int MaximumDiscoveredDimension = 1024;

    private WriterTableGrid(Table table, TableRowGroup group, IReadOnlyList<TableRow> rows,
        IReadOnlyList<WriterTableCellPlacement> placements, int columnCount,
        TableCell?[,] matrix)
    {
        Table = table;
        RowGroup = group;
        Rows = rows;
        Placements = placements.ToList();
        ColumnCount = columnCount;
        Matrix = matrix;
    }

    public Table Table { get; }
    public TableRowGroup RowGroup { get; }
    public IReadOnlyList<TableRow> Rows { get; }
    public List<WriterTableCellPlacement> Placements { get; }
    public int ColumnCount { get; }
    public TableCell?[,] Matrix { get; }

    public static bool TryBuild(Table table, TableRowGroup group, out WriterTableGrid grid)
    {
        grid = null!;
        var rows = group.Rows.Cast<TableRow>().ToList();
        if (rows.Count == 0 || rows.Count > MaximumDiscoveredDimension)
            return false;

        var occupied = new Dictionary<(int Row, int Column), TableCell>();
        var placements = new List<WriterTableCellPlacement>();
        // FlowDocument renders the logical grid from cells and their spans. Table.Columns only
        // supplies optional width metadata and can contain unused trailing columns, so discovery
        // derives width from occupied cell extents.
        var width = 0;
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var column = 0;
            foreach (var cell in rows[rowIndex].Cells)
            {
                while (occupied.ContainsKey((rowIndex, column)))
                    column++;
                var rowSpan = cell.RowSpan;
                var columnSpan = cell.ColumnSpan;
                if (rowSpan < 1 || columnSpan < 1 || rowSpan > rows.Count - rowIndex ||
                    columnSpan > int.MaxValue - column)
                    return false;
                var columnEnd = checked(column + columnSpan);
                if (columnEnd > MaximumDiscoveredDimension)
                    return false;
                for (var row = rowIndex; row < rowIndex + rowSpan; row++)
                {
                    for (var col = column; col < columnEnd; col++)
                    {
                        if (occupied.ContainsKey((row, col)))
                            return false;
                        occupied[(row, col)] = cell;
                    }
                }
                placements.Add(new WriterTableCellPlacement(cell, rowIndex, column,
                    rowSpan, columnSpan));
                width = Math.Max(width, columnEnd);
                column = columnEnd;
            }
        }

        if (width == 0)
            return false;
        for (var row = 0; row < rows.Count; row++)
        {
            for (var column = 0; column < width; column++)
            {
                if (!occupied.ContainsKey((row, column)))
                    return false;
            }
        }

        var matrix = new TableCell?[rows.Count, width];
        foreach (var item in occupied)
            matrix[item.Key.Row, item.Key.Column] = item.Value;
        grid = new WriterTableGrid(table, group, rows, placements, width, matrix);
        return true;
    }
}

/// <summary>
/// Rollback-only state for an exceptional mutation failure; it is never exposed as user undo history.
/// </summary>
internal sealed class WriterTableDocumentSnapshot
{
    public WriterTableDocumentSnapshot(byte[] bytes, int startOffset, int endOffset)
    {
        Bytes = bytes;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }

    public byte[] Bytes { get; }
    public int StartOffset { get; }
    public int EndOffset { get; }
}
