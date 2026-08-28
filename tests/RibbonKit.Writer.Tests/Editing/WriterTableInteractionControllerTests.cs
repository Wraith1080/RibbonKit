using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

[Collection("Writer UI")]
public sealed class WriterTableInteractionControllerTests
{
    [Fact]
    public void ForwardReverseAndFinalCellTabUseNativeNavigationAndOneUndoUnit()
    {
        StaTestHelper.Run(() =>
        {
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(new Run("before")))
            };
            using var controller = new WriterTableInteractionController(editor);
            var window = HostEditor(editor);
            try
            {
                window.Show();
                window.UpdateLayout();
                var paragraph = (Paragraph)editor.Document.Blocks.First();
                editor.Selection.Select(paragraph.ContentStart, paragraph.ContentStart);
                var table = Assert.IsType<Table>(controller.Tables.InsertTable(2, 2));
                editor.IsUndoEnabled = false;
                editor.IsUndoEnabled = true;

                var cells = controller.GetOrderedCells(table);
                Assert.Equal(4, cells.Count);
                controller.MoveCaret(cells[0]);
                Assert.True(controller.TryHandleTab(reverse: false));
                Assert.True(controller.Tables.TryGetCellAtCaret(out var second));
                Assert.Same(cells[1].Cell, second.Cell);
                Assert.True(controller.TryHandleTab(reverse: true));
                Assert.True(controller.Tables.TryGetCellAtCaret(out var first));
                Assert.Same(cells[0].Cell, first.Cell);
                Assert.False(controller.TryHandleTab(reverse: true));
                editor.Selection.Select(cells[0].Cell.ContentStart, cells[1].Cell.ContentEnd);
                Assert.True(controller.TryHandleTab(reverse: false));
                Assert.True(controller.Tables.TryGetCellAtCaret(out var afterSelection));
                Assert.Same(cells[2].Cell, afterSelection.Cell);
                Assert.False(editor.CanUndo);

                var final = cells[^1];
                var finalEnd = final.Cell.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
                editor.Selection.Select(finalEnd, finalEnd);
                var changed = 0;
                editor.TextChanged += CountTextChanges;
                Assert.True(controller.TryHandleTab(reverse: false));
                editor.TextChanged -= CountTextChanges;

                Assert.Equal(1, changed);
                Assert.Equal(3, table.RowGroups[0].Rows.Count);
                Assert.True(controller.Tables.TryGetCellAtCaret(out var appended));
                Assert.Equal(2, appended.Row);
                Assert.Equal(0, appended.Column);
                Assert.True(editor.CanUndo);
                Assert.True(editor.Undo());
                Assert.Equal(2, table.RowGroups[0].Rows.Count);
                Assert.True(editor.Redo());
                Assert.Equal(3, table.RowGroups[0].Rows.Count);

                void CountTextChanges(object sender, TextChangedEventArgs e) => changed++;
            }
            finally
            {
                if (window.IsVisible)
                    window.Close();
            }
        });
    }

    [Fact]
    public void LiteralTabReplacesOneCellSelectionAndIsOneNativeUndoUnit()
    {
        StaTestHelper.Run(() =>
        {
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(new Run("before")))
            };
            using var controller = new WriterTableInteractionController(editor);
            var window = HostEditor(editor);
            try
            {
                window.Show();
                window.UpdateLayout();
                var paragraph = (Paragraph)editor.Document.Blocks.First();
                editor.Selection.Select(paragraph.ContentStart, paragraph.ContentStart);
                var table = Assert.IsType<Table>(controller.Tables.InsertTable(1, 1));
                var cell = table.RowGroups[0].Rows[0].Cells[0];
                cell.Blocks.Clear();
                var run = new Run("replace");
                cell.Blocks.Add(new Paragraph(run));
                editor.IsUndoEnabled = false;
                editor.IsUndoEnabled = true;
                editor.Selection.Select(run.ContentStart, run.ContentEnd);
                var changed = 0;
                editor.TextChanged += CountTextChanges;

                Assert.True(controller.TryInsertLiteralTab());
                editor.TextChanged -= CountTextChanges;

                Assert.Equal(1, changed);
                Assert.True(editor.Selection.IsEmpty);
                Assert.Contains("\t", new TextRange(cell.ContentStart, cell.ContentEnd).Text);
                Assert.True(controller.Tables.TryGetCellAtCaret(out var caret));
                Assert.Same(cell, caret.Cell);
                Assert.True(editor.CanUndo);
                Assert.True(editor.Undo());
                Assert.Contains("replace", new TextRange(cell.ContentStart, cell.ContentEnd).Text);

                void CountTextChanges(object sender, TextChangedEventArgs e) => changed++;
            }
            finally
            {
                if (window.IsVisible)
                    window.Close();
            }
        });
    }

    [Fact]
    public void LiveCapabilityGateLeavesTableKeyboardRoutingUnhandled()
    {
        StaTestHelper.Run(() =>
        {
            var allowed = false;
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(new Run("before")))
            };
            using var controller = new WriterTableInteractionController(editor, () => allowed);
            var window = HostEditor(editor);
            try
            {
                window.Show();
                window.UpdateLayout();
                var paragraph = (Paragraph)editor.Document.Blocks.First();
                editor.Selection.Select(paragraph.ContentStart, paragraph.ContentStart);
                var table = Assert.IsType<Table>(controller.Tables.InsertTable(1, 1));
                var cell = table.RowGroups[0].Rows[0].Cells[0];
                var end = cell.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
                editor.Selection.Select(end, end);

                Assert.False(controller.TryInsertLiteralTab());
                Assert.False(controller.TryHandleTab(reverse: false));
                Assert.Single(table.RowGroups[0].Rows);
                Assert.DoesNotContain("\t", new TextRange(cell.ContentStart, cell.ContentEnd).Text);

                allowed = true;
                Assert.True(controller.TryInsertLiteralTab());
            }
            finally
            {
                if (window.IsVisible)
                    window.Close();
            }
        });
    }

    [Fact]
    public void BottomRightSpannedCellRemainsTheDeterministicFinalTabTarget()
    {
        StaTestHelper.Run(() =>
        {
            var table = new Table();
            var group = new TableRowGroup();
            var firstRow = new TableRow();
            var secondRow = new TableRow();
            var a = Cell("A", rowSpan: 2);
            var b = Cell("B");
            var c = Cell("C", rowSpan: 2);
            var d = Cell("D");
            firstRow.Cells.Add(a);
            firstRow.Cells.Add(b);
            firstRow.Cells.Add(c);
            secondRow.Cells.Add(d);
            group.Rows.Add(firstRow);
            group.Rows.Add(secondRow);
            table.RowGroups.Add(group);
            var document = new FlowDocument();
            document.Blocks.Add(table);
            document.Blocks.Add(new Paragraph(new Run("after")));
            var editor = new RichTextBox { Document = document };
            using var controller = new WriterTableInteractionController(editor);
            var window = HostEditor(editor);
            try
            {
                window.Show();
                window.UpdateLayout();
                var ordered = controller.GetOrderedCells(table);
                Assert.Equal(new[] { a, b, d, c }, ordered.Select(item => item.Cell).ToArray());
                controller.MoveCaret(ordered[1]);
                Assert.True(controller.TryHandleTab(reverse: false));
                Assert.True(controller.Tables.TryGetCellAtCaret(out var afterB));
                Assert.Same(d, afterB.Cell);
                Assert.True(controller.TryHandleTab(reverse: false));
                Assert.True(controller.Tables.TryGetCellAtCaret(out var afterD));
                Assert.Same(c, afterD.Cell);
                Assert.True(controller.TryHandleTab(reverse: true));
                Assert.True(controller.Tables.TryGetCellAtCaret(out var reverseFromC));
                Assert.Same(d, reverseFromC.Cell);
                var end = c.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
                editor.Selection.Select(end, end);
                editor.IsUndoEnabled = false;
                editor.IsUndoEnabled = true;

                Assert.True(controller.TryHandleTab(reverse: false));

                Assert.Equal(3, group.Rows.Count);
                Assert.True(controller.Tables.TryGetCellAtCaret(out var appended));
                Assert.Equal(2, appended.Row);
                Assert.Equal(0, appended.Column);
                Assert.True(editor.CanUndo);
                Assert.True(editor.Undo());
                Assert.Equal(2, group.Rows.Count);
            }
            finally
            {
                if (window.IsVisible)
                    window.Close();
            }
        });
    }

    [Fact]
    public void RefreshDeferralHoldsCommittedTableStateUntilFinalSelectionPublishes()
    {
        StaTestHelper.Run(() =>
        {
            var outside = new Paragraph(new Run("outside"));
            var editor = new RichTextBox { Document = new FlowDocument(outside) };
            using var controller = new WriterTableInteractionController(editor);
            var window = HostEditor(editor);
            try
            {
                window.Show();
                window.UpdateLayout();
                editor.Selection.Select(outside.ContentStart, outside.ContentStart);
                var table = Assert.IsType<Table>(controller.Tables.InsertTable(1, 1));
                var cell = table.RowGroups[0].Rows[0].Cells[0];
                editor.Selection.Select(cell.ContentStart, cell.ContentStart);
                controller.Refresh();
                Assert.True(controller.IsInTable);
                var changes = 0;
                controller.StateChanged += (_, _) => changes++;

                using (controller.DeferRefresh())
                {
                    editor.Selection.Select(outside.ContentStart, outside.ContentStart);
                    controller.Refresh();
                    Assert.True(controller.IsInTable);
                    Assert.Equal(0, changes);
                }

                Assert.False(controller.IsInTable);
                Assert.Equal(1, changes);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void DeleteTableLeavesCaretParagraphAndIsOneNativeUndoUnit()
    {
        StaTestHelper.Run(() =>
        {
            var anchor = new Paragraph(new Run("anchor"));
            var editor = new RichTextBox { Document = new FlowDocument(anchor) };
            using var controller = new WriterTableInteractionController(editor);
            var window = HostEditor(editor);
            try
            {
                window.Show();
                window.UpdateLayout();
                editor.Selection.Select(anchor.ContentStart, anchor.ContentStart);
                var table = Assert.IsType<Table>(controller.Tables.InsertTable(1, 1));
                editor.IsUndoEnabled = false;
                editor.IsUndoEnabled = true;

                Assert.True(controller.Tables.DeleteTable(table));

                Assert.DoesNotContain(editor.Document.Blocks, block => block is Table);
                Assert.IsType<Paragraph>(editor.Selection.Start.Paragraph);
                Assert.True(editor.CanUndo);
                Assert.True(editor.Undo());
                Assert.Single(editor.Document.Blocks.OfType<Table>());
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static TableCell Cell(string text, int rowSpan = 1) => new(new Paragraph(new Run(text)))
    {
        RowSpan = rowSpan
    };

    private static Window HostEditor(RichTextBox editor) => new()
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
