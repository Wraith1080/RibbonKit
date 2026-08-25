using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

public sealed class WriterParagraphIndentDragTests
{
    [Fact]
    public void MixedSelectionDragAppliesOneDeltaAndUndoRestoresEveryParagraph()
    {
        StaTestHelper.Run(() =>
        {
            var first = new Paragraph(new Run("first"))
            {
                Margin = new Thickness(24, 0, 12, 0),
                TextIndent = 6
            };
            var second = new Paragraph(new Run("second"))
            {
                Margin = new Thickness(48, 0, 20, 0),
                TextIndent = -8
            };
            using var fixture = CreateFixture(first, second);
            fixture.Editor.SelectAll();

            using (var drag = fixture.Adapter.BeginParagraphIndentDrag(
                       WriterRulerIndentMarker.Left, 400))
            {
                Assert.NotNull(drag);
                drag!.Update(64);
                drag.Commit();
            }

            Assert.Equal(64, first.Margin.Left, 6);
            Assert.Equal(88, second.Margin.Left, 6);
            Assert.Equal(WriterSelectionValueKind.Mixed, fixture.Adapter.State.Indentation.Kind);
            fixture.Adapter.Undo();
            Assert.Equal(24, first.Margin.Left, 6);
            Assert.Equal(48, second.Margin.Left, 6);
            Assert.Equal(6, first.TextIndent, 6);
            Assert.Equal(-8, second.TextIndent, 6);
        });
    }

    [Fact]
    public void RightAndHangingMarkersUseLogicalContentCoordinates()
    {
        StaTestHelper.Run(() =>
        {
            var paragraph = new Paragraph(new Run("marker"))
            {
                Margin = new Thickness(24, 0, 30, 0),
                TextIndent = -10
            };
            using var fixture = CreateFixture(paragraph);
            fixture.Editor.SelectAll();

            var right = fixture.Adapter.BeginParagraphIndentDrag(
                WriterRulerIndentMarker.Right, 400);
            Assert.NotNull(right);
            right!.Update(380);
            right.Commit();
            Assert.Equal(20, paragraph.Margin.Right, 6);

            var hanging = fixture.Adapter.BeginParagraphIndentDrag(
                WriterRulerIndentMarker.Hanging, 400);
            Assert.NotNull(hanging);
            // The opening hanging/body marker is the left margin at 24 DIPs. Moving it by 12 DIPs
            // moves Margin.Left and compensates TextIndent, preserving the first-line position.
            hanging!.Update(36);
            hanging.Commit();
            Assert.Equal(14, paragraph.Margin.Left + paragraph.TextIndent, 6);
            Assert.Equal(-22, paragraph.TextIndent, 6);
            Assert.Equal(36, paragraph.Margin.Left, 6);
        });
    }

    [Fact]
    public void NegativeFirstLineIndentationIsRepresentedAsHangingIndentation()
    {
        StaTestHelper.Run(() =>
        {
            var paragraph = new Paragraph(new Run("hanging"));
            using var fixture = CreateFixture(paragraph);
            fixture.Editor.SelectAll();

            fixture.Adapter.SetParagraphIndentation(18, -7, 26);

            Assert.Equal(18, paragraph.Margin.Left, 6);
            Assert.Equal(26, paragraph.Margin.Right, 6);
            Assert.Equal(-7, paragraph.TextIndent, 6);
            Assert.Equal(7, fixture.Adapter.State.HangingIndentation.Value, 6);
            Assert.Equal(-7, fixture.Adapter.State.TextIndentation.Value, 6);
            var rulerIndentation = fixture.Adapter.ReadRulerIndentation();
            Assert.Equal(-7, rulerIndentation.TextIndentDip, 6);
            Assert.Equal(7, rulerIndentation.HangingDip, 6);
            Assert.Equal(18, rulerIndentation.HangingMarkerDip, 6);
            fixture.Adapter.Undo();
            Assert.True(double.IsNaN(paragraph.Margin.Left));
            Assert.True(double.IsNaN(paragraph.Margin.Right));
            Assert.True(double.IsNaN(paragraph.TextIndent) || Math.Abs(paragraph.TextIndent) < 0.0001);
        });
    }

    [Fact]
    public void CancelledMarkerDragRestoresOpeningValuesWithoutChangingSelection()
    {
        StaTestHelper.Run(() =>
        {
            var paragraph = new Paragraph(new Run("cancel"))
            {
                Margin = new Thickness(12, 0, 15, 0),
                TextIndent = 4
            };
            using var fixture = CreateFixture(paragraph);
            fixture.Editor.SelectAll();
            var start = fixture.Editor.Selection.Start;
            var end = fixture.Editor.Selection.End;

            var drag = fixture.Adapter.BeginParagraphIndentDrag(
                WriterRulerIndentMarker.FirstLine, 400);
            Assert.NotNull(drag);
            drag!.Update(36);
            drag.Cancel();

            Assert.Equal(12, paragraph.Margin.Left, 6);
            Assert.Equal(15, paragraph.Margin.Right, 6);
            Assert.Equal(4, paragraph.TextIndent, 6);
            Assert.Equal(0, start.CompareTo(fixture.Editor.Selection.Start));
            Assert.Equal(0, end.CompareTo(fixture.Editor.Selection.End));
        });
    }

    [Fact]
    public void CancelledInheritedDragLeavesRawLocalsTextChangesHistoryAndSelectionUntouched()
    {
        StaTestHelper.Run(() =>
        {
            var paragraph = new Paragraph(new Run("inherited"));
            using var fixture = CreateFixture(paragraph);
            fixture.Editor.SelectAll();
            var start = fixture.Editor.Selection.Start;
            var end = fixture.Editor.Selection.End;
            var localMargin = paragraph.ReadLocalValue(Paragraph.MarginProperty);
            var localTextIndent = paragraph.ReadLocalValue(Paragraph.TextIndentProperty);
            var textChanges = 0;
            fixture.Editor.TextChanged += (_, _) => textChanges++;
            var canUndo = fixture.Editor.CanUndo;

            var drag = fixture.Adapter.BeginParagraphIndentDrag(
                WriterRulerIndentMarker.Hanging, 400);
            Assert.NotNull(drag);
            drag!.Update(36);
            drag.Cancel();

            Assert.Same(localMargin, paragraph.ReadLocalValue(Paragraph.MarginProperty));
            Assert.Same(localTextIndent, paragraph.ReadLocalValue(Paragraph.TextIndentProperty));
            Assert.Equal(0, textChanges);
            Assert.Equal(canUndo, fixture.Editor.CanUndo);
            Assert.Equal(0, start.CompareTo(fixture.Editor.Selection.Start));
            Assert.Equal(0, end.CompareTo(fixture.Editor.Selection.End));
        });
    }

    [Fact]
    public void CommittedDeferredDragIsOneUndoRedoUnitAndRestoresInheritedLocals()
    {
        StaTestHelper.Run(() =>
        {
            var paragraph = new Paragraph(new Run("commit"));
            using var fixture = CreateFixture(paragraph);
            fixture.Editor.SelectAll();

            var drag = fixture.Adapter.BeginParagraphIndentDrag(
                WriterRulerIndentMarker.Hanging, 400);
            Assert.NotNull(drag);
            drag!.Update(24);
            drag.Commit();

            Assert.Equal(24, paragraph.Margin.Left, 6);
            Assert.Equal(-24, paragraph.TextIndent, 6);
            Assert.True(fixture.Editor.CanUndo);
            fixture.Adapter.Undo();
            Assert.True(double.IsNaN(paragraph.Margin.Left));
            Assert.True(double.IsNaN(paragraph.TextIndent) || Math.Abs(paragraph.TextIndent) < 0.0001);
            Assert.True(fixture.Editor.CanRedo);
            fixture.Adapter.Redo();
            Assert.Equal(24, paragraph.Margin.Left, 6);
            Assert.Equal(-24, paragraph.TextIndent, 6);
        });
    }

    [Fact]
    public void InvalidMarkerIsRejectedBeforeCreatingAParagraphDrag()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture(new Paragraph(new Run("invalid")));
            fixture.Editor.SelectAll();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                fixture.Adapter.BeginParagraphIndentDrag((WriterRulerIndentMarker)99, 400));
            Assert.False(fixture.Editor.CanUndo);
        });
    }

    private static Fixture CreateFixture(params Paragraph[] paragraphs)
    {
        var document = new FlowDocument();
        foreach (var paragraph in paragraphs)
            document.Blocks.Add(paragraph);
        var editor = new RichTextBox { Document = document, IsUndoEnabled = true };
        var window = new Window { Content = editor, Width = 420, Height = 180, ShowInTaskbar = false };
        window.Show();
        editor.Focus();
        return new Fixture(window, editor, new WriterEditingAdapter(editor));
    }

    private sealed class Fixture(Window window, RichTextBox editor, WriterEditingAdapter adapter) : IDisposable
    {
        public RichTextBox Editor { get; } = editor;
        public WriterEditingAdapter Adapter { get; } = adapter;

        public void Dispose()
        {
            Adapter.Dispose();
            if (window.IsVisible)
                window.Close();
        }
    }
}
