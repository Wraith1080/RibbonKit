using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

[Collection("Writer UI")]
public sealed class WriterParagraphKeyboardControllerTests
{
    [Theory]
    [InlineData(false, true, false, false, WriterParagraphTabAction.IncreaseIndentation, true)]
    [InlineData(false, true, true, false, WriterParagraphTabAction.DecreaseIndentation, true)]
    [InlineData(false, false, false, false, WriterParagraphTabAction.InsertLiteralTab, true)]
    [InlineData(false, false, true, false, WriterParagraphTabAction.Unhandled, false)]
    [InlineData(false, false, false, true, WriterParagraphTabAction.InsertLiteralTab, true)]
    [InlineData(true, true, false, false, WriterParagraphTabAction.Unhandled, false)]
    [InlineData(true, false, false, true, WriterParagraphTabAction.Unhandled, false)]
    public void TabDecisionKeepsTableOwnershipAndDefinesMidParagraphPolicy(
        bool inTableCell,
        bool atParagraphBoundary,
        bool reverse,
        bool control,
        WriterParagraphTabAction expectedAction,
        bool expectedHandled)
    {
        var decision = WriterParagraphTabDecision.Decide(
            inTableCell, atParagraphBoundary, reverse, control);

        Assert.Equal(expectedAction, decision.Action);
        Assert.Equal(expectedHandled, decision.IsHandled);
        Assert.Equal(!expectedHandled, decision.IsUnhandled);
    }

    [Fact]
    public void CtrlTabInsertsOneLiteralTabAndIsUndoable()
    {
        StaTestHelper.Run(() =>
        {
            var run = new Run("beforeafter");
            var paragraph = new Paragraph(run);
            var editor = new RichTextBox { Document = new FlowDocument(paragraph) };
            using var controller = new WriterParagraphKeyboardController(editor);
            var window = HostEditor(editor);
            window.Show();
            window.UpdateLayout();
            editor.Focus();
            var insertion = run.ContentStart.GetPositionAtOffset(6, LogicalDirection.Forward);
            editor.Selection.Select(insertion, insertion);
            editor.IsUndoEnabled = false;
            editor.IsUndoEnabled = true;

            Assert.True(controller.TryInsertLiteralTab());
            Assert.Contains("before\tafter", new TextRange(
                editor.Document.ContentStart, editor.Document.ContentEnd).Text);
            Assert.True(editor.CanUndo);

            Assert.True(editor.Undo());
            Assert.DoesNotContain("\t", new TextRange(
                editor.Document.ContentStart, editor.Document.ContentEnd).Text);
        });
    }

    [Fact]
    public void MidParagraphTabInsertsLiteralTextWhenAcceptsTabIsFalse()
    {
        StaTestHelper.Run(() =>
        {
            var run = new Run("beforeafter");
            var paragraph = new Paragraph(run);
            var editor = new RichTextBox { Document = new FlowDocument(paragraph) };
            using var controller = new WriterParagraphKeyboardController(editor);
            var window = HostEditor(editor);
            window.Show();
            window.UpdateLayout();
            editor.Focus();
            var insertion = run.ContentStart.GetPositionAtOffset(6, LogicalDirection.Forward);
            editor.Selection.Select(insertion, insertion);
            editor.IsUndoEnabled = false;
            editor.IsUndoEnabled = true;

            Assert.True(controller.TryHandleTab(reverse: false));
            Assert.Contains("before\tafter", new TextRange(
                editor.Document.ContentStart, editor.Document.ContentEnd).Text);
            Assert.True(editor.CanUndo);
            Assert.True(editor.Undo());
            Assert.DoesNotContain("\t", new TextRange(
                editor.Document.ContentStart, editor.Document.ContentEnd).Text);
        });
    }

    [Fact]
    public void ParagraphBoundaryUsesNativeIndentationAndOneUndoUnit()
    {
        StaTestHelper.Run(() =>
        {
            var paragraph = new Paragraph(new Run("paragraph"));
            var editor = new RichTextBox { Document = new FlowDocument(paragraph) };
            using var controller = new WriterParagraphKeyboardController(editor);
            var window = HostEditor(editor);
            window.Show();
            window.UpdateLayout();
            editor.Focus();
            editor.Selection.Select(paragraph.ContentStart, paragraph.ContentStart);
            editor.IsUndoEnabled = false;
            editor.IsUndoEnabled = true;
            var originalLeft = paragraph.Margin.Left;

            Assert.True(controller.TryHandleTab(reverse: false));
            Assert.True(paragraph.Margin.Left > 0);
            Assert.True(editor.CanUndo);

            Assert.True(editor.Undo());
            if (double.IsNaN(originalLeft))
                Assert.True(double.IsNaN(paragraph.Margin.Left));
            else
                Assert.Equal(originalLeft, paragraph.Margin.Left);
        });
    }

    [Fact]
    public void TableCellIsNeverHandledByParagraphController()
    {
        StaTestHelper.Run(() =>
        {
            var table = new Table();
            var group = new TableRowGroup();
            var row = new TableRow();
            var cell = new TableCell(new Paragraph(new Run("cell")));
            row.Cells.Add(cell);
            group.Rows.Add(row);
            table.RowGroups.Add(group);
            var editor = new RichTextBox { Document = new FlowDocument(table) };
            using var controller = new WriterParagraphKeyboardController(editor);
            var window = HostEditor(editor);
            window.Show();
            window.UpdateLayout();
            var position = cell.ContentStart.GetInsertionPosition(LogicalDirection.Forward);
            editor.Selection.Select(position, position);

            Assert.True(controller.IsInTableCell());
            Assert.True(WriterParagraphKeyboardController.IsPointerInTableCell(position));
            Assert.False(controller.TryHandleTab(reverse: false));
            Assert.False(controller.TryHandleTab(reverse: true));
            Assert.False(controller.TryInsertLiteralTab());
        });
    }

    [Fact]
    public void AttachAndDetachAreIdempotent()
    {
        StaTestHelper.Run(() =>
        {
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(new Run("text")))
            };
            using var controller = new WriterParagraphKeyboardController(editor);

            Assert.True(controller.IsAttached);
            controller.Attach();
            Assert.True(controller.IsAttached);
            controller.Detach();
            Assert.False(controller.IsAttached);
            controller.Detach();
            controller.Attach();
            Assert.True(controller.IsAttached);
        });
    }

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
