using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RibbonKit.Writer.Tests.Document;
using RibbonKit.Writer.Editing;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

public sealed class WriterFindReplaceServiceTests
{
    [Fact]
    public void CaseSensitiveAndInsensitiveFindUseOrdinalComparison()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture(new Paragraph(new Run("Alpha alpha")));
            var insensitive = fixture.Service.FindNext("alpha", matchCase: false, wrap: false);
            Assert.True(insensitive.Found);
            Assert.Equal("Alpha", fixture.Editor.Selection.Text);

            fixture.Editor.Selection.Select(fixture.Editor.Document.ContentStart,
                fixture.Editor.Document.ContentStart);
            var sensitive = fixture.Service.FindNext("alpha", matchCase: true, wrap: false);
            Assert.True(sensitive.Found);
            Assert.Equal("alpha", fixture.Editor.Selection.Text);
        });
    }

    [Fact]
    public void FindNextAdvancesAfterSelectionAndWrapsDeterministically()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture(new Paragraph(new Run("one two one")));
            var first = fixture.Service.FindNext("one", wrap: true);
            var second = fixture.Service.FindNext("one", wrap: true);
            var wrapped = fixture.Service.FindNext("one", wrap: true);

            Assert.True(first.Found);
            Assert.False(first.Wrapped);
            Assert.Equal("one", fixture.TextAt(first));
            Assert.True(second.Found);
            Assert.False(second.Wrapped);
            Assert.True(wrapped.Found);
            Assert.True(wrapped.Wrapped);
            Assert.Equal(0, wrapped.Index);
        });
    }

    [Fact]
    public void CurrentSelectionStartCanReturnTheCurrentMatch()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture(new Paragraph(new Run("one two")));
            var first = fixture.Service.FindNext("one", wrap: false);
            Assert.True(first.Found);

            var current = fixture.Service.FindNext(new WriterFindOptions
            {
                Query = "one",
                Wrap = false,
                StartBehavior = WriterFindStartBehavior.CurrentSelection
            });

            Assert.True(current.Found);
            Assert.Equal(first.Index, current.Index);
        });
    }

    [Fact]
    public void EmptyQueryIsRejectedWithoutChangingSelection()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture(new Paragraph(new Run("text")));
            fixture.Editor.Selection.Select(fixture.Editor.Document.ContentStart,
                fixture.Editor.Document.ContentStart.GetPositionAtOffset(2)!);
            var before = fixture.Editor.Selection.Text;
            var result = fixture.Service.FindNext(string.Empty);

            Assert.False(result.Found);
            Assert.True(result.EmptyQuery);
            Assert.Equal(before, fixture.Editor.Selection.Text);
            Assert.Equal(0, fixture.Service.ReplaceAll(string.Empty, "x"));
        });
    }

    [Fact]
    public void ReplaceOnePreservesAdjacentFormattingAndUndo()
    {
        StaTestHelper.Run(() =>
        {
            var first = new Run("red") { Foreground = Brushes.Red };
            var second = new Run(" blue") { Foreground = Brushes.Blue };
            var paragraph = new Paragraph(first);
            paragraph.Inlines.Add(second);
            using var fixture = CreateFixture(paragraph);

            var replaced = fixture.Service.ReplaceNext("red", "green", matchCase: true, wrap: false);

            Assert.True(replaced.Replaced);
            Assert.Contains("green blue", new TextRange(fixture.Editor.Document.ContentStart,
                fixture.Editor.Document.ContentEnd).Text);
            Assert.Equal(Colors.Blue, ((SolidColorBrush)new TextRange(second.ContentStart,
                second.ContentEnd).GetPropertyValue(TextElement.ForegroundProperty)).Color);
            fixture.Editor.Undo();
            Assert.Contains("red blue", new TextRange(fixture.Editor.Document.ContentStart,
                fixture.Editor.Document.ContentEnd).Text);
        });
    }

    [Fact]
    public void ReplaceAllDoesNotRescanReplacementAndIsOneUndoUnit()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture(new Paragraph(new Run("a a")));
            var count = fixture.Service.ReplaceAll("a", "aa", matchCase: true);

            Assert.Equal(2, count);
            Assert.Contains("aa aa", new TextRange(fixture.Editor.Document.ContentStart,
                fixture.Editor.Document.ContentEnd).Text);
            Assert.True(fixture.Editor.CanUndo);
            fixture.Editor.Undo();
            Assert.Contains("a a", new TextRange(fixture.Editor.Document.ContentStart,
                fixture.Editor.Document.ContentEnd).Text);
        });
    }

    [Fact]
    public void ParagraphBoundaryCanBeFoundButNeverReplaced()
    {
        StaTestHelper.Run(() =>
        {
            var document = new FlowDocument();
            document.Blocks.Add(new Paragraph(new Run("first")));
            document.Blocks.Add(new Paragraph(new Run("second")));
            using var fixture = CreateFixture(document);

            var result = fixture.Service.FindNext("first\nsecond", matchCase: true, wrap: false);
            Assert.True(result.Found);
            Assert.True(result.CrossesParagraphBoundary);
            var replacement = fixture.Service.ReplaceNext("first\nsecond", "merged", true, false);
            Assert.False(replacement.Replaced);
            Assert.True(replacement.StructuralBoundary);
            Assert.Equal(0, fixture.Service.ReplaceAll("first\nsecond", "merged", true));
            Assert.Equal(2, fixture.Editor.Document.Blocks.Count);
        });
    }

    [Fact]
    public void DisabledOrReadOnlyEditorCannotReplace()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture(new Paragraph(new Run("text")));
            fixture.Editor.IsReadOnly = true;
            var readOnly = fixture.Service.ReplaceNext("text", "changed", wrap: false);
            Assert.False(readOnly.Replaced);
            Assert.True(readOnly.ReadOnly);
            fixture.Editor.IsReadOnly = false;
            fixture.Editor.IsEnabled = false;
            Assert.Equal(0, fixture.Service.ReplaceAll("text", "changed"));
        });
    }

    [Fact]
    public void EmptyDocumentNeverProducesAZeroLengthMatch()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateFixture(new FlowDocument());
            var result = fixture.Service.FindNext("x", matchCase: true, wrap: true);
            Assert.False(result.Found);
            Assert.Equal(0, fixture.Service.ReplaceAll("x", "replacement", matchCase: true));
        });
    }

    [Fact]
    public void EmbeddedInlineContentIsAFindBarrierAndSurvivesReplacement()
    {
        StaTestHelper.Run(() =>
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run("a"));
            paragraph.Inlines.Add(new InlineUIContainer(new Button { Content = "object" }));
            paragraph.Inlines.Add(new Run("b"));
            using var fixture = CreateFixture(paragraph);

            Assert.False(fixture.Service.FindNext("ab", matchCase: true, wrap: false).Found);
            Assert.Equal(0, fixture.Service.ReplaceAll("ab", "merged", matchCase: true));
            Assert.Single(paragraph.Inlines.OfType<InlineUIContainer>());
            Assert.Equal(1, fixture.Service.ReplaceAll("a", "A", matchCase: true));
            Assert.Single(paragraph.Inlines.OfType<InlineUIContainer>());
        });
    }

    [Fact]
    public void EmbeddedBlockContentIsABarrierAndPreservesItsImage()
    {
        StaTestHelper.Run(() =>
        {
            var section = new Section();
            section.Blocks.Add(new Paragraph(new Run("left")));
            var image = new Image { Width = 8, Height = 8 };
            var block = new BlockUIContainer(image);
            section.Blocks.Add(block);
            section.Blocks.Add(new Paragraph(new Run("right")));
            var document = new FlowDocument(section);
            using var fixture = CreateFixture(document);

            Assert.False(fixture.Service.FindNext("left\nright", matchCase: true, wrap: false).Found);
            Assert.Equal(0, fixture.Service.ReplaceAll("left\nright", "merged", matchCase: true));
            Assert.Same(image, block.Child);
            Assert.Single(section.Blocks.OfType<BlockUIContainer>());
        });
    }

    [Fact]
    public void FloaterAndFigureAreBarriersEvenWhenEmptyOrContainingText()
    {
        StaTestHelper.Run(() =>
        {
            var document = new FlowDocument();
            var paragraph = new Paragraph(new Run("a"));
            var floater = new Floater();
            paragraph.Inlines.Add(floater);
            paragraph.Inlines.Add(new Run("b"));
            var figure = new Figure();
            figure.Blocks.Add(new Paragraph(new Run("inside")));
            paragraph.Inlines.Add(figure);
            paragraph.Inlines.Add(new Run("c"));
            document.Blocks.Add(paragraph);
            using var fixture = CreateFixture(document);

            Assert.False(fixture.Service.FindNext("a\nb", matchCase: true, wrap: false).Found);
            Assert.False(fixture.Service.FindNext("b\nc", matchCase: true, wrap: false).Found);
            Assert.Equal(1, fixture.Service.ReplaceAll("inside", "changed", matchCase: true));
            Assert.Contains("changed", new TextRange(figure.ContentStart, figure.ContentEnd).Text);
            Assert.Single(paragraph.Inlines.OfType<Floater>());
            Assert.Single(paragraph.Inlines.OfType<Figure>());
        });
    }

    [Fact]
    public void ParagraphCanonicalizationHandlesListTableLineBreakAndRuns()
    {
        StaTestHelper.Run(() =>
        {
            var document = new FlowDocument();
            var lineBreakParagraph = new Paragraph();
            lineBreakParagraph.Inlines.Add(new Run("a"));
            lineBreakParagraph.Inlines.Add(new LineBreak());
            lineBreakParagraph.Inlines.Add(new Run("b"));
            document.Blocks.Add(lineBreakParagraph);

            var list = new List();
            var firstItem = new ListItem(new Paragraph(new Run("c")));
            var secondItem = new ListItem(new Paragraph(new Run("d")));
            list.ListItems.Add(firstItem);
            list.ListItems.Add(secondItem);
            document.Blocks.Add(list);

            var table = new Table();
            table.Columns.Add(new TableColumn());
            var group = new TableRowGroup();
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(new Run("e"))));
            row.Cells.Add(new TableCell(new Paragraph(new Run("f"))));
            group.Rows.Add(row);
            table.RowGroups.Add(group);
            document.Blocks.Add(table);
            using var fixture = CreateFixture(document);

            Assert.True(fixture.Service.FindNext("a\nb", matchCase: true, wrap: false).Found);
            Assert.True(fixture.Service.FindNext("c\nd", matchCase: true, wrap: false).Found);
            Assert.True(fixture.Service.FindNext("e\nf", matchCase: true, wrap: false).Found);

            var crossRun = new Paragraph();
            crossRun.Inlines.Add(new Run("cross"));
            crossRun.Inlines.Add(new Run("run"));
            document.Blocks.Add(crossRun);
            Assert.True(fixture.Service.FindNext("crossrun", matchCase: true, wrap: false).Found);

            var spanParagraph = new Paragraph();
            var span = new Span(new Run("span"));
            span.Inlines.Add(new Run("text"));
            spanParagraph.Inlines.Add(span);
            document.Blocks.Add(spanParagraph);
            Assert.True(fixture.Service.FindNext("spantext", matchCase: true, wrap: false).Found);
        });
    }

    [Fact]
    public void ReplacementCaretTracksActualInsertedEndForPlainEmojiAndNewline()
    {
        StaTestHelper.Run(() =>
        {
            using (var plain = CreateFixture(new Paragraph(new Run("aTAIL"))))
            {
                Assert.True(plain.Service.ReplaceNext("a", "replacement", true, false).Replaced);
                plain.Editor.Selection.Text = "X";
                Assert.Contains("replacementXTAIL", DocumentText(plain.Editor));
            }

            using (var emoji = CreateFixture(new Paragraph(new Run("aTAIL"))))
            {
                Assert.True(emoji.Service.ReplaceNext("a", "😀", true, false).Replaced);
                emoji.Editor.Selection.Text = "X";
                Assert.Contains("😀XTAIL", DocumentText(emoji.Editor));
            }

            using (var newline = CreateFixture(new Paragraph(new Run("aTAIL")),
                       new Paragraph(new Run("kept"))))
            {
                Assert.True(newline.Service.ReplaceNext("a", "one\r\ntwo", true, false).Replaced);
                newline.Editor.Selection.Text = "X";
                var text = DocumentText(newline.Editor);
                Assert.Contains("one\r\ntwoXTAIL", text);
                Assert.Contains("kept", text);
                Assert.True(newline.Editor.Document.Blocks.Count >= 2);
            }
        });
    }

    private static Fixture CreateFixture(params Block[] blocks)
    {
        var document = new FlowDocument();
        foreach (var block in blocks)
            document.Blocks.Add(block);
        return CreateFixture(document);
    }

    private static Fixture CreateFixture(FlowDocument document)
    {
        var editor = new RichTextBox { Document = document, IsUndoEnabled = true };
        var window = new Window { Content = editor, Width = 220, Height = 140, ShowInTaskbar = false };
        window.Show();
        editor.Focus();
        return new Fixture(window, editor, new WriterFindReplaceService(editor));
    }

    private static string DocumentText(RichTextBox editor) =>
        new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd).Text;

    private sealed class Fixture(Window window, RichTextBox editor, WriterFindReplaceService service) : IDisposable
    {
        public RichTextBox Editor { get; } = editor;
        public WriterFindReplaceService Service { get; } = service;

        public string TextAt(WriterFindResult result) =>
            new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd).Text
                .Substring(result.Index, result.Length);

        public void Dispose()
        {
            Service.Dispose();
            window.Close();
        }
    }
}
