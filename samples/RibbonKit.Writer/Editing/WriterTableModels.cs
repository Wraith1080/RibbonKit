using System.Windows;
using System.Windows.Documents;

namespace RibbonKit.Writer.Editing;

/// <summary>Specifies horizontal placement of a table within the document content area.</summary>
public enum WriterTableHorizontalAlignment
{
    /// <summary>Places the table against the leading content edge.</summary>
    Left,

    /// <summary>Centers the table between the content edges.</summary>
    Center,

    /// <summary>Places the table against the trailing content edge.</summary>
    Right
}

/// <summary>Specifies vertical placement of content within a table cell's padded height.</summary>
public enum WriterTableCellVerticalAlignment
{
    /// <summary>Places cell content at the top.</summary>
    Top,

    /// <summary>Centers cell content vertically.</summary>
    Center,

    /// <summary>Places cell content at the bottom.</summary>
    Bottom
}

/// <summary>Chooses which side of a resolved table cell receives inserted rows or columns.</summary>
public enum WriterTableInsertPlacement
{
    /// <summary>Insert immediately before the cell's occupied rows or columns.</summary>
    Before,

    /// <summary>Insert immediately after the cell's occupied rows or columns.</summary>
    After
}

/// <summary>
/// Identifies one logical cell in a native <see cref="Table"/> grid.
/// </summary>
/// <remarks>
/// The row index is local to <see cref="RowGroup"/>. A cell may occupy more than one logical row or
/// column; <see cref="Row"/> and <see cref="Column"/> identify its top-left grid position.
/// References are intentionally cheap snapshots. Resolve a fresh reference after a structural
/// mutation because WPF table collections may be rebuilt by the service.
/// </remarks>
public readonly record struct WriterTableCellReference
{
    internal WriterTableCellReference(Table table, TableRowGroup rowGroup, TableCell cell,
        int groupIndex, int row, int column, int rowSpan, int columnSpan)
    {
        Table = table;
        RowGroup = rowGroup;
        Cell = cell;
        GroupIndex = groupIndex;
        Row = row;
        Column = column;
        RowSpan = rowSpan;
        ColumnSpan = columnSpan;
    }

    /// <summary>Gets the containing table.</summary>
    public Table Table { get; }

    /// <summary>Gets the containing row group.</summary>
    public TableRowGroup RowGroup { get; }

    /// <summary>Gets the native cell.</summary>
    public TableCell Cell { get; }

    /// <summary>Gets the zero-based row-group index in the table.</summary>
    public int GroupIndex { get; }

    /// <summary>Gets the zero-based row index within <see cref="RowGroup"/>.</summary>
    public int Row { get; }

    /// <summary>Gets the zero-based logical column index.</summary>
    public int Column { get; }

    /// <summary>Gets the number of logical rows occupied by the cell.</summary>
    public int RowSpan { get; }

    /// <summary>Gets the number of logical columns occupied by the cell.</summary>
    public int ColumnSpan { get; }

    /// <summary>Gets the last logical row occupied by the cell.</summary>
    public int LastRow => checked(Row + RowSpan - 1);

    /// <summary>Gets the last logical column occupied by the cell.</summary>
    public int LastColumn => checked(Column + ColumnSpan - 1);

    /// <summary>Gets whether this reference contains a non-null native cell.</summary>
    public bool IsValid => Table is not null && RowGroup is not null && Cell is not null;
}

/// <summary>Describes an inclusive rectangular range in one native table row group.</summary>
public readonly record struct WriterTableRange
{
    /// <summary>Creates a rectangular range. End coordinates are inclusive.</summary>
    public WriterTableRange(Table table, TableRowGroup rowGroup, int startRow, int startColumn,
        int endRow, int endColumn)
    {
        Table = table ?? throw new ArgumentNullException(nameof(table));
        RowGroup = rowGroup ?? throw new ArgumentNullException(nameof(rowGroup));
        StartRow = Math.Min(startRow, endRow);
        StartColumn = Math.Min(startColumn, endColumn);
        EndRow = Math.Max(startRow, endRow);
        EndColumn = Math.Max(startColumn, endColumn);
    }

    /// <summary>Gets the containing table.</summary>
    public Table Table { get; }

    /// <summary>Gets the containing row group.</summary>
    public TableRowGroup RowGroup { get; }

    /// <summary>Gets the inclusive first row.</summary>
    public int StartRow { get; }

    /// <summary>Gets the inclusive first column.</summary>
    public int StartColumn { get; }

    /// <summary>Gets the inclusive last row.</summary>
    public int EndRow { get; }

    /// <summary>Gets the inclusive last column.</summary>
    public int EndColumn { get; }

    /// <summary>Gets the number of logical rows in the range.</summary>
    public int RowCount => checked(EndRow - StartRow + 1);

    /// <summary>Gets the number of logical columns in the range.</summary>
    public int ColumnCount => checked(EndColumn - StartColumn + 1);

    /// <summary>Gets whether the range has valid non-negative coordinates.</summary>
    public bool IsValid => Table is not null && RowGroup is not null && StartRow >= 0 &&
        StartColumn >= 0 && EndRow >= StartRow && EndColumn >= StartColumn;

    /// <summary>Creates a range spanning the two resolved cells.</summary>
    /// <exception cref="ArgumentException">The cells do not belong to one table row group.</exception>
    public static WriterTableRange Between(WriterTableCellReference first, WriterTableCellReference second)
    {
        if (!first.IsValid || !second.IsValid || !ReferenceEquals(first.Table, second.Table) ||
            !ReferenceEquals(first.RowGroup, second.RowGroup))
            throw new ArgumentException("The two cells must belong to the same table row group.");

        return new WriterTableRange(first.Table, first.RowGroup, first.Row, first.Column,
            second.Row, second.Column);
    }
}

/// <summary>Describes the result of a table operation that may create a new caret target.</summary>
public readonly record struct WriterTableOperationResult(bool Succeeded,
    WriterTableCellReference? CaretCell)
{
    /// <summary>Gets a result that indicates no mutation occurred.</summary>
    public static WriterTableOperationResult None => new(false, null);

    /// <summary>Gets a successful result targeting the supplied cell.</summary>
    public static WriterTableOperationResult Success(WriterTableCellReference cell) => new(true, cell);
}

internal readonly record struct WriterTableCellPaddingAdjustment(
    int GroupIndex, int Row, int Column, Thickness Padding);
