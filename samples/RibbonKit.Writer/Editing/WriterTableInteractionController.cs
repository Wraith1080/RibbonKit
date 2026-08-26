using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace RibbonKit.Writer.Editing;

/// <summary>
/// Projects the accepted table service onto the live editor's keyboard and selection surface.
/// </summary>
/// <remarks>
/// Table routing is attached to <see cref="UIElement.PreviewKeyDownEvent"/> so it runs before any
/// later paragraph-level Tab projection. The controller deliberately leaves the first-cell
/// Shift+Tab and non-table Tab paths unhandled, allowing the normal WPF keyboard focus contract to
/// remain available.
/// </remarks>
public sealed class WriterTableInteractionController : IDisposable
{
    private readonly RichTextBox _editor;
    private readonly Func<bool> _canRouteTableKeyboard;
    private bool _disposed;

    /// <summary>Creates a table interaction controller over a live native editor.</summary>
    /// <param name="editor">The editor whose table interaction is projected.</param>
    /// <param name="canRouteTableKeyboard">
    /// Optional live capability gate. When supplied, table Tab handling is left to the host while
    /// the active profile or editor state does not permit table editing.
    /// </param>
    public WriterTableInteractionController(RichTextBox editor,
        Func<bool>? canRouteTableKeyboard = null)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _canRouteTableKeyboard = canRouteTableKeyboard ?? AlwaysAllowTableKeyboard;
        Tables = new WriterTableService(editor);
        _editor.PreviewKeyDown += OnPreviewKeyDown;
        _editor.SelectionChanged += OnEditorStateChanged;
        _editor.TextChanged += OnEditorTextChanged;
        Refresh();
    }

    /// <summary>Gets the W3-B table mutation service used by this controller.</summary>
    public WriterTableService Tables { get; }

    /// <summary>Gets the current cell, if the caret or selection is in a table.</summary>
    public WriterTableCellReference? CurrentCell { get; private set; }

    /// <summary>Gets the current table, if the caret or selection is in a table.</summary>
    public Table? CurrentTable { get; private set; }

    /// <summary>Gets whether the current editor range is inside a table.</summary>
    public bool IsInTable => CurrentCell is not null;

    /// <summary>Gets whether the current range describes more than one table cell.</summary>
    public bool CanMerge { get; private set; }

    /// <summary>Gets whether the current cell is spanned and can be split.</summary>
    public bool CanSplit { get; private set; }

    /// <summary>Raised after the current cell or selection projection changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Refreshes contextual table state after an external selection mutation.</summary>
    public void Refresh()
    {
        ThrowIfDisposed();
        CurrentCell = Tables.TryGetCellAtCaret(out var current) ? current : null;
        CurrentTable = CurrentCell?.Table;
        CanSplit = CurrentCell is { RowSpan: > 1 } or { ColumnSpan: > 1 };
        CanMerge = Tables.TryGetSelectionRange(out var range)
            && (range.RowCount > 1 || range.ColumnCount > 1);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Resolves the current selection to a table rectangle when possible.</summary>
    public bool TryGetSelectionRange(out WriterTableRange range) =>
        Tables.TryGetSelectionRange(out range);

    /// <summary>
    /// Handles a table Tab or Shift+Tab action. The first-cell reverse path is intentionally left
    /// unhandled so the host can provide a keyboard focus-exit path.
    /// </summary>
    public bool TryHandleTab(bool reverse)
    {
        ThrowIfDisposed();
        if (!_canRouteTableKeyboard())
            return false;
        var navigationAnchor = reverse ? _editor.Selection.Start : _editor.Selection.End;
        if (!Tables.TryGetCell(navigationAnchor, out var current))
            return false;

        var cells = GetOrderedCells(current.Table);
        var index = -1;
        for (var i = 0; i < cells.Count; i++)
        {
            if (!ReferenceEquals(cells[i].Cell, current.Cell))
                continue;
            index = i;
            break;
        }
        if (index < 0)
            return false;

        if (reverse)
        {
            if (index == 0)
                return false;
            MoveCaret(cells[index - 1]);
            return true;
        }

        if (index < cells.Count - 1)
        {
            MoveCaret(cells[index + 1]);
            return true;
        }

        // The W3-B service owns the native change scope and deterministic next-row caret for the
        // final logical cell. A middle-of-cell Tab is normalized to the cell end first so the
        // final-cell contract does not depend on the current text offset.
        if (!_editor.Selection.IsEmpty || !IsAtCellEnd(current.Cell))
        {
            var end = current.Cell.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
            _editor.Selection.Select(end, end);
        }

        return Tables.TryHandleFinalCellTab();
    }

    /// <summary>
    /// Inserts one literal tab character inside the current table cell. This is the explicit
    /// keyboard escape from cell navigation and is also used by the Table Tools command.
    /// </summary>
    public bool TryInsertLiteralTab()
    {
        ThrowIfDisposed();
        if (!_canRouteTableKeyboard())
            return false;
        if (!Tables.TryGetCellAtCaret(out var current) ||
            !Tables.TryGetCell(_editor.Selection.End, out var end) ||
            !ReferenceEquals(current.Cell, end.Cell))
            return false;

        return WriterInlineInsertion.TryReplaceSelection(_editor, new Run("\t"));
    }

    /// <summary>Moves the caret to the first position in a resolved cell.</summary>
    public void MoveCaret(WriterTableCellReference cell)
    {
        ThrowIfDisposed();
        var position = cell.Cell.ContentStart.GetInsertionPosition(LogicalDirection.Forward);
        _editor.Selection.Select(position, position);
        _editor.Focus();
        Refresh();
    }

    /// <summary>Returns the current table cells in deterministic logical order.</summary>
    public IReadOnlyList<WriterTableCellReference> GetOrderedCells(Table table)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        var cells = new List<WriterTableCellReference>();
        foreach (var group in table.RowGroups)
        {
            foreach (var row in group.Rows)
            {
                foreach (var cell in row.Cells)
                {
                    if (Tables.TryGetCell(cell, out var reference))
                        cells.Add(reference);
                }
            }
        }

        var ordered = cells.OrderBy(item => item.GroupIndex)
            .ThenBy(item => item.Row)
            .ThenBy(item => item.Column)
            .ToList();

        // A cell can begin before a later physical cell while spanning the table's logical
        // bottom-right slot. W3-B defines that occupant as the final-cell Tab target, so move it
        // to the end of its row group without otherwise disturbing top-left document order.
        foreach (var group in ordered.Select(item => item.GroupIndex).Distinct().ToArray())
        {
            var groupCells = ordered.Where(item => item.GroupIndex == group).ToArray();
            var lastRow = groupCells.Max(item => item.LastRow);
            var lastColumn = groupCells.Max(item => item.LastColumn);
            var finalCell = groupCells.Single(item => item.LastRow == lastRow
                && item.LastColumn == lastColumn);
            var finalIndex = ordered.FindIndex(item => ReferenceEquals(item.Cell, finalCell.Cell));
            var groupEnd = ordered.FindLastIndex(item => item.GroupIndex == group);
            if (finalIndex < 0 || finalIndex == groupEnd)
                continue;
            ordered.RemoveAt(finalIndex);
            groupEnd = ordered.FindLastIndex(item => item.GroupIndex == group);
            ordered.Insert(groupEnd + 1, finalCell);
        }

        return ordered;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _editor.PreviewKeyDown -= OnPreviewKeyDown;
        _editor.SelectionChanged -= OnEditorStateChanged;
        _editor.TextChanged -= OnEditorTextChanged;
        Tables.Dispose();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_disposed || e.Handled || e.Key != Key.Tab)
            return;

        var modifiers = e.KeyboardDevice.Modifiers;
        if (modifiers == ModifierKeys.Control)
        {
            if (TryInsertLiteralTab())
                e.Handled = true;
            return;
        }

        if (modifiers == ModifierKeys.None || modifiers == ModifierKeys.Shift)
            e.Handled = TryHandleTab(modifiers == ModifierKeys.Shift);
    }

    private void OnEditorStateChanged(object sender, RoutedEventArgs e) => Refresh();

    private void OnEditorTextChanged(object sender, TextChangedEventArgs e) => Refresh();

    private static bool IsAtCellEnd(TableCell cell, TextPointer pointer)
    {
        var end = cell.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
        return pointer.CompareTo(end) >= 0 || pointer.CompareTo(cell.ContentEnd) == 0;
    }

    private bool IsAtCellEnd(TableCell cell) => IsAtCellEnd(cell, _editor.Selection.Start);

    private static bool AlwaysAllowTableKeyboard() => true;

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
