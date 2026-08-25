using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

[Collection("Writer UI")]
public sealed class WriterTableServiceTests
{
    [Fact]
    public void InsertsApprovedGridAndResolvesCaretToCell()
    {
        StaTestHelper.Run(() =>
        {
            var document = new FlowDocument(new Paragraph(new Run("before")));
            var editor = new RichTextBox { Document = document };
            var paragraph = Assert.IsType<Paragraph>(document.Blocks.First());
            editor.Selection.Select(paragraph.ContentStart, paragraph.ContentStart);
            using var service = new WriterTableService(editor);

            var table = service.InsertTable(8, 8);
            Assert.NotNull(table);
            Assert.Equal(8, table.RowGroups[0].Rows.Count);
            Assert.Equal(8, table.Columns.Count);
            Assert.All(table.RowGroups[0].Rows, row => Assert.Equal(8, row.Cells.Count));
            Assert.True(service.TryGetCell(editor.Selection.Start, out var current));
            Assert.Equal(0, current.Row);
            Assert.Equal(0, current.Column);
            Assert.Same(table, current.Table);
        });
    }

    [Fact]
    public void InsertsIntoAnEmptyFlowDocument()
    {
        StaTestHelper.Run(() =>
        {
            var document = new FlowDocument();
            var editor = new RichTextBox { Document = document };
            using var service = new WriterTableService(editor);

            var table = service.InsertTable(1, 1);
            Assert.NotNull(table);
            Assert.Single(document.Blocks);
            Assert.IsType<Table>(document.Blocks.First());
            Assert.True(service.TryGetCellAtCaret(out var reference));
            Assert.Equal(0, reference.Row);
            Assert.Equal(0, reference.Column);
        });
    }

    [Fact]
    public void InsertionHonorsBlockBoundariesAndRejectsMiddleOrCrossBlockSelection()
    {
        StaTestHelper.Run(() =>
        {
            var first = new Paragraph(new Run("first"));
            var second = new Paragraph(new Run("second"));
            var document = new FlowDocument(first);
            document.Blocks.Add(second);
            var editor = new RichTextBox { Document = document };
            using var service = new WriterTableService(editor);

            editor.Selection.Select(first.ContentStart, first.ContentStart);
            Assert.NotNull(service.InsertTable(1, 1));
            Assert.IsType<Table>(document.Blocks.First());

            editor.Selection.Select(second.ContentEnd, second.ContentEnd);
            Assert.NotNull(service.InsertTable(1, 1));
            Assert.IsType<Table>(document.Blocks.Last());

            var beforeCount = document.Blocks.Count;
            var middle = first.ContentStart.GetPositionAtOffset(2, LogicalDirection.Forward)!;
            editor.Selection.Select(middle, middle);
            Assert.Null(service.InsertTable(1, 1));
            Assert.Equal(beforeCount, document.Blocks.Count);

            // The insertion at the second paragraph's trailing edge can invalidate that paragraph's
            // old ContentEnd pointer while WPF rebalances the block tree. Resolve both endpoints
            // from the live document before exercising the cross-block rejection path.
            var liveParagraphs = document.Blocks.OfType<Paragraph>().ToArray();
            Assert.Equal(2, liveParagraphs.Length);
            editor.Selection.Select(liveParagraphs[0].ContentStart, liveParagraphs[1].ContentEnd);
            Assert.Null(service.InsertTable(1, 1));
            Assert.Equal(beforeCount, document.Blocks.Count);
        });
    }

    [Fact]
    public void RowAndColumnMutationsPreserveSpansAndCaret()
    {
        StaTestHelper.Run(() =>
        {
            var (editor, table, service) = CreateTable(3, 3);
            Assert.True(service.TryGetCell(table.RowGroups[0].Rows[1].Cells[1], out var reference));

            Assert.True(service.InsertRows(reference, 1, WriterTableInsertPlacement.Before));
            Assert.Equal(4, table.RowGroups[0].Rows.Count);
            Assert.True(service.TryGetCell(editor.Selection.Start, out var insertedRowCaret));
            Assert.Equal(1, insertedRowCaret.Row);

            Assert.True(service.InsertColumns(insertedRowCaret, 2, WriterTableInsertPlacement.After));
            table = editor.Document.Blocks.OfType<Table>().Single();
            Assert.Equal(5, table.Columns.Count);
            Assert.True(service.TryGetCell(editor.Selection.Start, out insertedRowCaret));
            Assert.True(service.DeleteRows(insertedRowCaret));
            Assert.Equal(3, table.RowGroups[0].Rows.Count);
            Assert.True(service.TryGetCell(editor.Selection.Start, out var afterDeleteRow));
            Assert.True(service.DeleteColumns(afterDeleteRow, 1));
            table = editor.Document.Blocks.OfType<Table>().Single();
            Assert.Equal(4, table.Columns.Count);
            AssertValidGrid(table);
        });
    }

    [Fact]
    public void MergeAndSplitMoveContentAndRestoreRectangularGrid()
    {
        StaTestHelper.Run(() =>
        {
            var (editor, table, service) = CreateTable(3, 3);
            var group = table.RowGroups[0];
            ((Paragraph)group.Rows[0].Cells[0].Blocks.First()).Inlines.Add(new Run("one"));
            ((Paragraph)group.Rows[1].Cells[1].Blocks.First()).Inlines.Add(new Run("two"));
            Assert.True(service.TryGetCell(group.Rows[0].Cells[0], out var first));
            Assert.True(service.TryGetCell(group.Rows[1].Cells[1], out var second));

            Assert.True(service.TryMergeCells(first, second, out var merged));
            Assert.Equal(2, merged.RowSpan);
            Assert.Equal(2, merged.ColumnSpan);
            Assert.Contains("one", new TextRange(merged.Cell.ContentStart, merged.Cell.ContentEnd).Text);
            Assert.Contains("two", new TextRange(merged.Cell.ContentStart, merged.Cell.ContentEnd).Text);
            AssertValidGrid(table);
            Assert.True(service.TrySplitCell(merged));
            Assert.Equal(3, group.Rows[0].Cells.Count);
            Assert.Equal(3, group.Rows[1].Cells.Count);
            AssertValidGrid(table);
            Assert.True(service.TryGetCell(editor.Selection.Start, out var caret));
            Assert.Same(group.Rows[0].Cells[0], caret.Cell);
        });
    }

    [Fact]
    public void RejectsMergeThatCutsAcrossAnExistingSpan()
    {
        StaTestHelper.Run(() =>
        {
            var (editor, table, service) = CreateTable(3, 3);
            var group = table.RowGroups[0];
            Assert.True(service.TryGetCell(group.Rows[0].Cells[0], out var first));
            Assert.True(service.TryGetCell(group.Rows[1].Cells[1], out var second));
            Assert.True(service.TryMergeCells(first, second, out var merged));
            Assert.True(service.TryGetCell(group.Rows[0].Cells[1], out var right));

            Assert.False(service.TryMergeCells(merged, right, out _));
            Assert.Equal(2, merged.RowSpan);
            Assert.Equal(2, merged.ColumnSpan);
            AssertValidGrid(table);
        });
    }

    [Fact]
    public void DeleteRowsAndColumnsTrimSpansWithoutLeavingDetachedCells()
    {
        StaTestHelper.Run(() =>
        {
            var (editor, table, service) = CreateTable(3, 3);
            var group = table.RowGroups[0];
            Assert.True(service.TryGetCell(group.Rows[0].Cells[0], out var topLeft));
            Assert.True(service.TryGetCell(group.Rows[1].Cells[1], out var lowerRight));
            Assert.True(service.TryMergeCells(topLeft, lowerRight, out var merged));

            Assert.True(service.DeleteRows(merged));
            Assert.Equal(2, table.RowGroups[0].Rows.Count);
            Assert.True(service.TryGetCell(editor.Selection.Start, out var afterRowDelete));
            Assert.Equal(1, afterRowDelete.RowSpan);
            Assert.Equal(2, afterRowDelete.ColumnSpan);

            Assert.True(service.DeleteColumns(afterRowDelete));
            table = editor.Document.Blocks.OfType<Table>().Single();
            Assert.Equal(2, table.Columns.Count);
            AssertValidGrid(table);
        });
    }

    [Fact]
    public void FinalCellTabCreatesRowAndMovesCaretToItsFirstCell()
    {
        StaTestHelper.Run(() =>
        {
            var (editor, table, service) = CreateTable(2, 2);
            var last = table.RowGroups[0].Rows[1].Cells[1];
            var end = last.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
            editor.Selection.Select(end, end);

            Assert.True(service.TryHandleFinalCellTab());
            Assert.Equal(3, table.RowGroups[0].Rows.Count);
            Assert.True(service.TryGetCell(editor.Selection.Start, out var reference));
            Assert.Equal(2, reference.Row);
            Assert.Equal(0, reference.Column);
            AssertValidGrid(table);
        });
    }

    [Fact]
    public void SelectionRangeCanDriveMergeWithoutRibbonState()
    {
        StaTestHelper.Run(() =>
        {
            var (editor, table, service) = CreateTable(2, 2);
            var first = table.RowGroups[0].Rows[0].Cells[0];
            var last = table.RowGroups[0].Rows[1].Cells[1];
            editor.Selection.Select(first.ContentStart, last.ContentEnd);

            Assert.True(service.TryGetSelectionRange(out var range));
            Assert.Equal(2, range.RowCount);
            Assert.Equal(2, range.ColumnCount);
            Assert.True(service.TryMergeSelection(out var merged));
            Assert.Equal(2, merged.RowSpan);
            Assert.Equal(2, merged.ColumnSpan);
            AssertValidGrid(table);
        });
    }

    [Fact]
    public void SameCellMergeIsRejectedWithoutNativeHistoryOrTreeChange()
    {
        StaTestHelper.Run(() =>
        {
            var created = CreateTable(2, 2);
            using var service = created.Service;
            var editor = created.Editor;
            var table = created.Table;
            var window = HostEditor(editor);
            window.Show();
            window.UpdateLayout();
            editor.IsUndoEnabled = false;
            editor.IsUndoEnabled = true;

            var cell = table.RowGroups[0].Rows[0].Cells[0];
            editor.Selection.Select(cell.ContentStart, cell.ContentEnd);
            Assert.True(service.TryGetCell(editor.Selection.Start, out var reference));
            var start = editor.Selection.Start;
            var end = editor.Selection.End;
            var rows = table.RowGroups[0].Rows.ToArray();
            var cells = rows.SelectMany(row => row.Cells).ToArray();
            var changed = 0;
            editor.TextChanged += (_, _) => changed++;

            Assert.False(service.TryMergeCells(reference, reference, out _));
            Assert.Equal(0, changed);
            Assert.False(editor.CanUndo);
            Assert.Equal(2, table.RowGroups[0].Rows.Count);
            Assert.Equal(cells, table.RowGroups[0].Rows.SelectMany(row => row.Cells));
            Assert.Equal(0, start.CompareTo(editor.Selection.Start));
            Assert.Equal(0, end.CompareTo(editor.Selection.End));
            if (window.IsVisible)
                window.Close();
        });
    }

    [Fact]
    public void EmptyCellMergeNormalizesPlaceholdersAndNativeSplitUndoRestoresThem()
    {
        StaTestHelper.Run(() =>
        {
            var created = CreateTable(2, 2);
            using var service = created.Service;
            var editor = created.Editor;
            var table = created.Table;
            var window = HostEditor(editor);
            window.Show();
            window.UpdateLayout();
            editor.IsUndoEnabled = false;
            editor.IsUndoEnabled = true;
            var changed = 0;
            editor.TextChanged += (_, _) => changed++;

            Assert.True(service.TryGetCell(table.RowGroups[0].Rows[0].Cells[0], out var first));
            Assert.True(service.TryGetCell(table.RowGroups[0].Rows[1].Cells[1], out var last));
            changed = 0;
            Assert.True(service.TryMergeCells(first, last, out var merged));
            Assert.Equal(1, changed);
            Assert.Single(merged.Cell.Blocks);
            var mergedParagraph = Assert.IsType<Paragraph>(merged.Cell.Blocks.First());
            var mergedRun = Assert.IsType<Run>(mergedParagraph.Inlines.Single());
            Assert.Empty(mergedRun.Text);
            Assert.True(editor.CanUndo);

            Assert.True(editor.Undo());
            table = editor.Document.Blocks.OfType<Table>().Single();
            Assert.Equal(2, table.RowGroups[0].Rows.Count);
            Assert.All(table.RowGroups[0].Rows.SelectMany(row => row.Cells), AssertSingleEmptyParagraph);
            Assert.True(editor.Redo());
            table = editor.Document.Blocks.OfType<Table>().Single();
            Assert.True(service.TryGetCell(table.RowGroups[0].Rows[0].Cells[0], out merged));
            AssertSingleEmptyParagraph(merged.Cell);

            editor.Selection.Select(merged.Cell.ContentStart, merged.Cell.ContentStart);
            changed = 0;
            Assert.True(service.TrySplitCell(merged));
            Assert.Equal(1, changed);
            table = editor.Document.Blocks.OfType<Table>().Single();
            Assert.All(table.RowGroups[0].Rows.SelectMany(row => row.Cells), AssertSingleEmptyParagraph);
            Assert.True(editor.Undo());
            table = editor.Document.Blocks.OfType<Table>().Single();
            Assert.Single(table.RowGroups[0].Rows[0].Cells);
            AssertSingleEmptyParagraph(table.RowGroups[0].Rows[0].Cells[0]);
            Assert.True(editor.Redo());
            table = editor.Document.Blocks.OfType<Table>().Single();
            Assert.All(table.RowGroups[0].Rows.SelectMany(row => row.Cells), AssertSingleEmptyParagraph);
            if (window.IsVisible)
                window.Close();
        });
    }

    [Fact]
    public void AlreadySpannedExplicitRangeIsRejectedWithoutNativeHistoryOrTreeChange()
    {
        StaTestHelper.Run(() =>
        {
            var created = CreateTable(2, 2);
            using var service = created.Service;
            var editor = created.Editor;
            var table = created.Table;
            Assert.True(service.TryGetCell(table.RowGroups[0].Rows[0].Cells[0], out var first));
            Assert.True(service.TryGetCell(table.RowGroups[0].Rows[1].Cells[1], out var last));
            Assert.True(service.TryMergeCells(first, last, out var merged));
            var group = table.RowGroups[0];
            var range = new WriterTableRange(table, group, 0, 0, 1, 1);

            var window = HostEditor(editor);
            window.Show();
            window.UpdateLayout();
            editor.IsUndoEnabled = false;
            editor.IsUndoEnabled = true;
            editor.Selection.Select(merged.Cell.ContentStart, merged.Cell.ContentEnd);
            var start = editor.Selection.Start;
            var end = editor.Selection.End;
            var rows = group.Rows.ToArray();
            var cells = rows.SelectMany(row => row.Cells).ToArray();
            var rowSpan = merged.Cell.RowSpan;
            var columnSpan = merged.Cell.ColumnSpan;
            var changed = 0;
            editor.TextChanged += (_, _) => changed++;

            Assert.False(service.TryMergeCells(range, out _));
            Assert.Equal(0, changed);
            Assert.False(editor.CanUndo);
            Assert.Equal(rowSpan, group.Rows[0].Cells[0].RowSpan);
            Assert.Equal(columnSpan, group.Rows[0].Cells[0].ColumnSpan);
            Assert.Equal(cells, group.Rows.SelectMany(row => row.Cells));
            Assert.Equal(0, start.CompareTo(editor.Selection.Start));
            Assert.Equal(0, end.CompareTo(editor.Selection.End));
            if (window.IsVisible)
                window.Close();
        });
    }

    [Fact]
    public void FinalCellTabCreatesRowWhenFinalCellIsSpanned()
    {
        StaTestHelper.Run(() =>
        {
            var (editor, table, service) = CreateTable(3, 3);
            var group = table.RowGroups[0];
            Assert.True(service.TryGetCell(group.Rows[1].Cells[1], out var first));
            Assert.True(service.TryGetCell(group.Rows[2].Cells[2], out var second));
            Assert.True(service.TryMergeCells(first, second, out var finalCell));
            var end = finalCell.Cell.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
            editor.Selection.Select(end, end);

            Assert.True(service.TryHandleFinalCellTab());
            Assert.Equal(4, group.Rows.Count);
            Assert.True(service.TryGetCell(editor.Selection.Start, out var caret));
            Assert.Equal(3, caret.Row);
            Assert.Equal(0, caret.Column);
            AssertValidGrid(table);
        });
    }

    [Fact]
    public void OccupancyDiscoveryRejectsUncoveredLogicalSlots()
    {
        StaTestHelper.Run(() =>
        {
            var table = new Table();
            var group = new TableRowGroup();
            var fullRow = new TableRow();
            fullRow.Cells.Add(new TableCell(new Paragraph(new Run("a"))));
            fullRow.Cells.Add(new TableCell(new Paragraph(new Run("b"))));
            var shortRow = new TableRow();
            shortRow.Cells.Add(new TableCell(new Paragraph(new Run("c"))));
            group.Rows.Add(fullRow);
            group.Rows.Add(shortRow);
            table.RowGroups.Add(group);
            table.Columns.Add(new TableColumn());
            table.Columns.Add(new TableColumn());
            var document = new FlowDocument(table);
            var editor = new RichTextBox { Document = document };
            editor.Selection.Select(fullRow.Cells[0].ContentStart, fullRow.Cells[0].ContentStart);
            using var service = new WriterTableService(editor);

            Assert.False(service.TryGetCell(editor.Selection.Start, out _));
            Assert.False(service.InsertRows(1));
        });
    }

    [Fact]
    public void OccupancyDiscoveryRejectsExcessiveDiscoveredDimensions()
    {
        StaTestHelper.Run(() =>
        {
            var group = new TableRowGroup();
            for (var index = 0; index <= 1024; index++)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(index == 0 ? "bounded" : ""))));
                group.Rows.Add(row);
            }
            var table = new Table();
            table.RowGroups.Add(group);
            var document = new FlowDocument(table);
            var editor = new RichTextBox { Document = document };
            var cell = group.Rows[0].Cells[0];
            editor.Selection.Select(cell.ContentStart, cell.ContentStart);
            using var service = new WriterTableService(editor);

            Assert.False(service.TryGetCell(editor.Selection.Start, out _));
            Assert.False(service.InsertColumns(1));
        });
    }

    [Fact]
    public void MergeDropsOnlySemanticPlaceholdersAndPreservesFormattedEmptyBlocks()
    {
        StaTestHelper.Run(() =>
        {
            var (editor, table, service) = CreateTable(1, 2);
            var formatted = new Paragraph(new Run()) { Margin = new Thickness(7) };
            table.RowGroups[0].Rows[0].Cells[1].Blocks.Clear();
            table.RowGroups[0].Rows[0].Cells[1].Blocks.Add(formatted);
            Assert.True(service.TryGetCell(table.RowGroups[0].Rows[0].Cells[0], out var first));
            Assert.True(service.TryGetCell(table.RowGroups[0].Rows[0].Cells[1], out var second));

            Assert.True(service.TryMergeCells(first, second, out var merged));
            var preserved = Assert.IsType<Paragraph>(Assert.Single(merged.Cell.Blocks));
            Assert.Equal(new Thickness(7), preserved.Margin);
            Assert.Empty(Assert.IsType<Run>(preserved.Inlines.Single()).Text);
            Assert.Same(editor.Document, table.Parent);
        });
    }

    [Fact]
    public void MultiGroupFailureIsAtomicWhenAnotherGroupHasAnInvalidGrid()
    {
        StaTestHelper.Run(() =>
        {
            var table = new Table();
            var valid = new TableRowGroup();
            var validFirst = new TableRow();
            validFirst.Cells.Add(new TableCell(new Paragraph(new Run("a"))));
            var validSecond = new TableRow();
            validSecond.Cells.Add(new TableCell(new Paragraph(new Run("b"))));
            valid.Rows.Add(validFirst);
            valid.Rows.Add(validSecond);
            var invalid = new TableRowGroup();
            var invalidFirst = new TableRow();
            invalidFirst.Cells.Add(new TableCell(new Paragraph(new Run("c"))));
            invalidFirst.Cells.Add(new TableCell(new Paragraph(new Run("d"))));
            var invalidSecond = new TableRow();
            invalidSecond.Cells.Add(new TableCell(new Paragraph(new Run("e"))));
            invalid.Rows.Add(invalidFirst);
            invalid.Rows.Add(invalidSecond);
            table.RowGroups.Add(valid);
            table.RowGroups.Add(invalid);
            var document = new FlowDocument(table);
            var editor = new RichTextBox { Document = document };
            editor.Selection.Select(valid.Rows[0].Cells[0].ContentStart,
                valid.Rows[0].Cells[0].ContentStart);
            using var service = new WriterTableService(editor);
            var before = new TextRange(document.ContentStart, document.ContentEnd).Text;
            var beforeCaret = document.ContentStart.GetOffsetToPosition(editor.Selection.Start);

            Assert.False(service.InsertColumns(1));
            Assert.Single(valid.Rows[0].Cells);
            Assert.Single(valid.Rows[1].Cells);
            Assert.Equal(2, invalid.Rows[0].Cells.Count);
            Assert.Single(invalid.Rows[1].Cells);
            Assert.Equal(before, new TextRange(document.ContentStart, document.ContentEnd).Text);
            Assert.Equal(beforeCaret, document.ContentStart.GetOffsetToPosition(editor.Selection.Start));
        });
    }

    [Fact]
    public void DeleteColumnsRejectsUnequalValidGroupsWithoutEmptyRows()
    {
        StaTestHelper.Run(() =>
        {
            var table = new Table();
            var wide = new TableRowGroup();
            for (var rowIndex = 0; rowIndex < 2; rowIndex++)
            {
                var row = new TableRow();
                for (var columnIndex = 0; columnIndex < 3; columnIndex++)
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{rowIndex}:{columnIndex}"))));
                wide.Rows.Add(row);
            }
            var narrow = new TableRowGroup();
            for (var rowIndex = 0; rowIndex < 2; rowIndex++)
                narrow.Rows.Add(new TableRow
                {
                    Cells = { new TableCell(new Paragraph(new Run($"n:{rowIndex}"))) }
                });
            table.RowGroups.Add(wide);
            table.RowGroups.Add(narrow);
            for (var columnIndex = 0; columnIndex < 3; columnIndex++)
                table.Columns.Add(new TableColumn());
            var document = new FlowDocument(table);
            var editor = new RichTextBox { Document = document };
            editor.Selection.Select(wide.Rows[0].Cells[0].ContentStart,
                wide.Rows[0].Cells[0].ContentStart);
            using var service = new WriterTableService(editor);
            Assert.True(service.TryGetCell(editor.Selection.Start, out var reference));
            var before = new TextRange(document.ContentStart, document.ContentEnd).Text;
            var changed = 0;
            editor.TextChanged += (_, _) => changed++;

            Assert.False(service.DeleteColumns(reference, 2));
            Assert.Equal(0, changed);
            Assert.Equal(before, new TextRange(document.ContentStart, document.ContentEnd).Text);
            Assert.Equal(3, wide.Rows[0].Cells.Count);
            Assert.Equal(3, wide.Rows[1].Cells.Count);
            Assert.Single(narrow.Rows[0].Cells);
            Assert.Single(narrow.Rows[1].Cells);
            AssertValidGrid(table);
        });
    }

    [Fact]
    public void StructuralCountsAreBoundedBeforeAnyMutation()
    {
        StaTestHelper.Run(() =>
        {
            var (editor, table, service) = CreateTable(1, 1);
            Assert.True(service.TryGetCell(editor.Selection.Start, out var reference));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.InsertRows(reference, 9));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.InsertColumns(reference, 9));
            Assert.Single(table.RowGroups[0].Rows);
            Assert.Single(table.RowGroups[0].Rows[0].Cells);
        });
    }

    [Fact]
    public void ForeignPointerAndInvalidMutationLeaveDocumentAndCaretUnchanged()
    {
        StaTestHelper.Run(() =>
        {
            var (editor, table, service) = CreateTable(2, 2);
            var foreignDocument = new FlowDocument(new Paragraph(new Run("foreign")));
            var foreignPointer = foreignDocument.ContentStart;
            Assert.False(service.TryGetCell(foreignPointer, out _));
            Assert.True(service.TryGetCell(editor.Selection.Start, out var current));
            var before = new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd).Text;
            var beforeCaret = editor.Document.ContentStart.GetOffsetToPosition(editor.Selection.Start);

            var invalidRange = new WriterTableRange(table, current.RowGroup, 0, 0, 99, 99);
            Assert.False(service.TryMergeCells(invalidRange, out _));
            var foreign = CreateTable(1, 1);
            using (foreign.Service)
            {
                Assert.True(foreign.Service.TryGetCell(foreign.Table.RowGroups[0].Rows[0].Cells[0],
                    out var foreignReference));
                Assert.False(service.TryMergeCells(current, foreignReference, out _));
            }
            Assert.False(service.DeleteColumns(default, 1));
            Assert.Equal(before, new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd).Text);
            Assert.Equal(beforeCaret, editor.Document.ContentStart.GetOffsetToPosition(editor.Selection.Start));
            AssertValidGrid(table);
        });
    }

    [Fact]
    public void FormattingAndDistributionUseNativeTableProperties()
    {
        StaTestHelper.Run(() =>
        {
            var (editor, table, service) = CreateTable(2, 2);
            var cell = table.RowGroups[0].Rows[0].Cells[0];
            Assert.True(service.TryGetCell(cell, out var reference));
            Assert.True(service.SetCellAlignment(reference, TextAlignment.Center));
            Assert.True(service.SetCellPadding(reference, new Thickness(2, 3, 4, 5)));
            Assert.True(service.SetCellBorder(reference, Brushes.Red, new Thickness(1)));
            Assert.True(service.SetCellBackground(reference, Brushes.Yellow));
            Assert.True(service.SetColumnWidth(table, 0, new GridLength(120)));
            table = editor.Document.Blocks.OfType<Table>().Single();
            Assert.True(service.TryGetCell(table.RowGroups[0].Rows[0].Cells[0], out reference));
            Assert.True(service.SetTableBackground(table, Brushes.WhiteSmoke));
            Assert.True(service.SetRowHeight(reference, 30));
            Assert.True(service.DistributeColumns(table, 0, 2, 240));
            table = editor.Document.Blocks.OfType<Table>().Single();
            Assert.True(service.TryGetCell(table.RowGroups[0].Rows[0].Cells[0], out reference));
            cell = reference.Cell;
            Assert.True(service.DistributeRows(reference, 2, 80));

            Assert.Equal(TextAlignment.Center, cell.TextAlignment);
            Assert.Equal(2, cell.Padding.Left);
            Assert.Equal(4, cell.Padding.Right);
            Assert.True(cell.Padding.Top + cell.Padding.Bottom >= 5);
            Assert.Equal(Brushes.Red.Color, Assert.IsType<SolidColorBrush>(cell.BorderBrush).Color);
            Assert.Equal(new Thickness(1), cell.BorderThickness);
            Assert.Equal(Brushes.Yellow.Color, Assert.IsType<SolidColorBrush>(cell.Background).Color);
            Assert.Equal(new GridLength(120), table.Columns[0].Width);
            Assert.Equal(new GridLength(120), table.Columns[1].Width);
            Assert.Equal(Brushes.WhiteSmoke.Color, Assert.IsType<SolidColorBrush>(table.Background).Color);
        });
    }

    [Fact]
    public void ColumnClonePreservesSupportedCellContentNestedTableImageHyperlinkAndResources()
    {
        StaTestHelper.Run(() =>
        {
            var (editor, table, service) = CreateTable(1, 1);
            var cell = table.RowGroups[0].Rows[0].Cells[0];
            cell.Resources["CellBrush"] = Brushes.CornflowerBlue;
            var paragraph = Assert.IsType<Paragraph>(cell.Blocks.First());
            paragraph.Inlines.Clear();
            paragraph.Inlines.Add(new Hyperlink(new Run("link"))
            {
                NavigateUri = new Uri("https://example.com")
            });
            var bitmap = new WriteableBitmap(2, 2, 96, 96, PixelFormats.Bgra32, null);
            bitmap.Freeze();
            paragraph.Inlines.Add(new InlineUIContainer(new Image
            {
                Source = bitmap,
                Width = 24,
                Height = 18,
                IsHitTestVisible = false
            }));
            var nested = new Table();
            var nestedGroup = new TableRowGroup();
            var nestedRow = new TableRow();
            nestedRow.Cells.Add(new TableCell(new Paragraph(new Run("nested"))));
            nestedGroup.Rows.Add(nestedRow);
            nested.RowGroups.Add(nestedGroup);
            cell.Blocks.Add(nested);
            Assert.True(service.TryGetCell(cell, out var reference));

            Assert.True(service.SetColumnWidth(table, 0, new GridLength(120)));
            var replacement = editor.Document.Blocks.OfType<Table>().Single();
            var replacementCell = replacement.RowGroups[0].Rows[0].Cells[0];
            var replacementParagraph = Assert.IsType<Paragraph>(replacementCell.Blocks.First());
            var link = Assert.IsType<Hyperlink>(replacementParagraph.Inlines.First());
            Assert.Equal(new Uri("https://example.com"), link.NavigateUri);
            Assert.Contains("link", new TextRange(replacementCell.ContentStart,
                replacementCell.ContentEnd).Text);
            var image = Assert.IsType<Image>(Assert.IsType<InlineUIContainer>(
                replacementParagraph.Inlines.Skip(1).First()).Child);
            Assert.Equal(24, image.Width);
            Assert.Equal(18, image.Height);
            Assert.IsAssignableFrom<BitmapSource>(image.Source);
            var replacementNested = Assert.Single(replacementCell.Blocks.OfType<Table>());
            Assert.Contains("nested", new TextRange(replacementNested.ContentStart,
                replacementNested.ContentEnd).Text);
            Assert.Equal(Brushes.CornflowerBlue.Color,
                Assert.IsType<SolidColorBrush>(replacementCell.Resources["CellBrush"]).Color);
            Assert.True(service.TryGetCell(editor.Selection.Start, out var freshReference));
            Assert.Equal(reference.Row, freshReference.Row);
            Assert.Equal(reference.Column, freshReference.Column);
        });
    }

    [Fact]
    public void RealizedEditorUsesNativeUndoForRowsAndClonedColumnMetadata()
    {
        StaTestHelper.Run(() =>
        {
            var created = CreateTable(2, 2);
            using var service = created.Service;
            var editor = created.Editor;
            var table = created.Table;
            var document = editor.Document;
            var window = HostEditor(editor);
            try
            {
                editor.IsUndoEnabled = false;
                editor.IsUndoEnabled = true;
                window.Show();
                window.UpdateLayout();
                var changed = 0;
                editor.TextChanged += (_, _) => changed++;

            Assert.True(service.TryGetCell(table.RowGroups[0].Rows[0].Cells[0], out var reference));
            editor.Selection.Select(reference.Cell.ContentStart, reference.Cell.ContentStart);
            changed = 0;
            Assert.True(service.InsertRows(reference));
            Assert.True(editor.CanUndo);
            Assert.Equal(1, changed);
            Assert.Equal(3, table.RowGroups[0].Rows.Count);
            Assert.True(service.TryGetCell(editor.Selection.Start, out var insertedCaret));
            Assert.Equal(1, insertedCaret.Row);

            Assert.True(editor.Undo());
            Assert.Equal(2, table.RowGroups[0].Rows.Count);
            Assert.True(editor.Redo());
            Assert.Equal(3, table.RowGroups[0].Rows.Count);

            table = document.Blocks.OfType<Table>().Single();
            editor.Selection.Select(table.RowGroups[0].Rows[0].Cells[0].ContentStart,
                table.RowGroups[0].Rows[0].Cells[0].ContentStart);
            Assert.True(service.TryGetCell(editor.Selection.Start, out reference));
            changed = 0;
            Assert.True(service.SetColumnWidth(table, 0, new GridLength(120)));
            Assert.True(editor.CanUndo);
            Assert.Equal(1, changed);
            table = document.Blocks.OfType<Table>().Single();
            Assert.Equal(2, table.Columns.Count);
            Assert.Equal(new GridLength(120), table.Columns[0].Width);
            Assert.True(service.TryGetCell(editor.Selection.Start, out var widthCaret));
            Assert.Equal(reference.Row, widthCaret.Row);
            Assert.Equal(reference.Column, widthCaret.Column);

            changed = 0;
            Assert.False(service.SetColumnWidth(table, 0, new GridLength(120)));
            Assert.Equal(0, changed);
            Assert.True(editor.CanUndo);
            Assert.True(editor.Undo());
            table = document.Blocks.OfType<Table>().Single();
            Assert.Equal(3, table.RowGroups[0].Rows.Count);
            Assert.NotEqual(new GridLength(120), table.Columns[0].Width);
            Assert.True(editor.Redo());
            table = document.Blocks.OfType<Table>().Single();
            Assert.Equal(new GridLength(120), table.Columns[0].Width);

            changed = 0;
            Assert.True(service.SetColumnBackground(table, 0, Brushes.Yellow));
            Assert.Equal(1, changed);
            table = document.Blocks.OfType<Table>().Single();
            Assert.Same(Brushes.Yellow, table.Columns[0].Background);
            Assert.True(editor.Undo());
            table = document.Blocks.OfType<Table>().Single();
            Assert.NotSame(Brushes.Yellow, table.Columns[0].Background);
            Assert.True(editor.Redo());
            table = document.Blocks.OfType<Table>().Single();
            Assert.Same(Brushes.Yellow, table.Columns[0].Background);

            editor.Selection.Select(table.RowGroups[0].Rows[0].Cells[0].ContentStart,
                table.RowGroups[0].Rows[0].Cells[0].ContentStart);
            Assert.True(service.TryGetCell(editor.Selection.Start, out reference));
            changed = 0;
            Assert.True(service.InsertColumns(reference, 1));
            Assert.Equal(1, changed);
            table = document.Blocks.OfType<Table>().Single();
            Assert.Equal(3, table.Columns.Count);
            Assert.True(service.TryGetCell(editor.Selection.Start, out var insertedColumnCaret));
            Assert.Equal(reference.Row, insertedColumnCaret.Row);
            Assert.True(editor.Undo());
            Assert.Equal(2, document.Blocks.OfType<Table>().Single().Columns.Count);
            Assert.True(editor.Redo());
            Assert.Equal(3, document.Blocks.OfType<Table>().Single().Columns.Count);

            table = document.Blocks.OfType<Table>().Single();
            editor.Selection.Select(table.RowGroups[0].Rows[0].Cells[0].ContentStart,
                table.RowGroups[0].Rows[0].Cells[0].ContentStart);
            Assert.True(service.TryGetCell(editor.Selection.Start, out reference));
            changed = 0;
            Assert.True(service.DeleteColumns(reference, 1));
            Assert.Equal(1, changed);
            Assert.Equal(2, document.Blocks.OfType<Table>().Single().Columns.Count);
            Assert.True(editor.Undo());
            Assert.Equal(3, document.Blocks.OfType<Table>().Single().Columns.Count);
            Assert.True(editor.Redo());
            Assert.Equal(2, document.Blocks.OfType<Table>().Single().Columns.Count);
            }
            finally
            {
                if (window.IsVisible)
                    window.Close();
            }
        });
    }

    [Fact]
    public void RealizedEditorUsesNativeUndoForFormattingMergeSplitAndFinalTab()
    {
        StaTestHelper.Run(() =>
        {
            var created = CreateTable(2, 2);
            using var service = created.Service;
            var editor = created.Editor;
            var table = created.Table;
            var window = HostEditor(editor);
            try
            {
                editor.IsUndoEnabled = false;
                editor.IsUndoEnabled = true;
                window.Show();
                window.UpdateLayout();
                var changed = 0;
                editor.TextChanged += (_, _) => changed++;
                var cell = table.RowGroups[0].Rows[0].Cells[0];
                editor.Selection.Select(cell.ContentStart, cell.ContentStart);
                Assert.True(service.TryGetCell(cell, out var reference));

                changed = 0;
                Assert.True(service.SetCellAlignment(reference, TextAlignment.Center));
                Assert.Equal(1, changed);
                Assert.Equal(TextAlignment.Center, cell.TextAlignment);
                Assert.True(editor.Undo());
                Assert.NotEqual(TextAlignment.Center, cell.TextAlignment);
                Assert.True(editor.Redo());
                Assert.Equal(TextAlignment.Center, cell.TextAlignment);

                editor.Selection.Select(table.RowGroups[0].Rows[0].Cells[0].ContentStart,
                    table.RowGroups[0].Rows[0].Cells[0].ContentStart);
                Assert.True(service.TryGetCell(editor.Selection.Start, out var first));
                Assert.True(service.TryGetCell(table.RowGroups[0].Rows[1].Cells[1], out var last));
                changed = 0;
                Assert.True(service.TryMergeCells(first, last, out var merged));
                Assert.Equal(1, changed);
                Assert.Single(table.RowGroups[0].Rows[0].Cells);
                Assert.True(editor.Undo());
                Assert.Equal(2, table.RowGroups[0].Rows[0].Cells.Count);
                Assert.True(editor.Redo());
                Assert.Single(table.RowGroups[0].Rows[0].Cells);

                var liveMergedCell = table.RowGroups[0].Rows[0].Cells[0];
                editor.Selection.Select(liveMergedCell.ContentStart, liveMergedCell.ContentStart);
                Assert.True(service.TryGetCell(editor.Selection.Start, out merged));
                changed = 0;
                Assert.True(service.TrySplitCell(merged));
                Assert.Equal(1, changed);
                Assert.Equal(2, table.RowGroups[0].Rows[0].Cells.Count);
                Assert.True(editor.Undo());
                Assert.Single(table.RowGroups[0].Rows[0].Cells);
                Assert.True(editor.Redo());
                Assert.Equal(2, table.RowGroups[0].Rows[0].Cells.Count);

                var final = table.RowGroups[0].Rows[1].Cells[1];
                var end = final.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
                editor.Selection.Select(end, end);
                changed = 0;
                Assert.True(service.TryHandleFinalCellTab());
                Assert.Equal(1, changed);
                Assert.Equal(3, table.RowGroups[0].Rows.Count);
                Assert.True(editor.Undo());
                Assert.Equal(2, table.RowGroups[0].Rows.Count);
                Assert.True(editor.Redo());
                Assert.Equal(3, table.RowGroups[0].Rows.Count);

                editor.Selection.Select(table.RowGroups[0].Rows[0].Cells[0].ContentStart,
                    table.RowGroups[0].Rows[0].Cells[0].ContentStart);
                Assert.True(service.TryGetCell(editor.Selection.Start, out reference));
                changed = 0;
                Assert.False(service.SetCellAlignment(reference, table.RowGroups[0].Rows[0].Cells[0].TextAlignment));
                Assert.Equal(0, changed);
            }
            finally
            {
                if (window.IsVisible)
                    window.Close();
            }
        });
    }

    private static Window HostEditor(RichTextBox editor)
    {
        return new Window
        {
            Content = editor,
            Width = 500,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
            Opacity = 0.01
        };
    }

    private static (RichTextBox Editor, Table Table, WriterTableService Service) CreateTable(int rows,
        int columns)
    {
        var document = new FlowDocument(new Paragraph(new Run("before")));
        var editor = new RichTextBox { Document = document };
        var paragraph = (Paragraph)document.Blocks.First();
        editor.Selection.Select(paragraph.ContentStart, paragraph.ContentStart);
        var service = new WriterTableService(editor);
        var table = service.InsertTable(rows, columns);
        Assert.NotNull(table);
        return (editor, table, service);
    }

    private static void AssertValidGrid(Table table)
    {
        Assert.NotEmpty(table.RowGroups);
        foreach (var group in table.RowGroups)
        {
            Assert.NotEmpty(group.Rows);
            var occupied = new HashSet<(int Row, int Column)>();
            for (var row = 0; row < group.Rows.Count; row++)
            {
                var column = 0;
                foreach (var cell in group.Rows[row].Cells)
                {
                    while (occupied.Contains((row, column)))
                        column++;
                    Assert.True(cell.RowSpan > 0);
                    Assert.True(cell.ColumnSpan > 0);
                    Assert.True(row + cell.RowSpan <= group.Rows.Count);
                    for (var rowOffset = 0; rowOffset < cell.RowSpan; rowOffset++)
                    for (var columnOffset = 0; columnOffset < cell.ColumnSpan; columnOffset++)
                        Assert.True(occupied.Add((row + rowOffset, column + columnOffset)));
                    column += cell.ColumnSpan;
                }
            }
        }
    }

    private static void AssertSingleEmptyParagraph(TableCell cell)
    {
        var paragraph = Assert.IsType<Paragraph>(Assert.Single(cell.Blocks));
        var run = Assert.IsType<Run>(Assert.Single(paragraph.Inlines));
        Assert.Empty(run.Text);
    }
}
