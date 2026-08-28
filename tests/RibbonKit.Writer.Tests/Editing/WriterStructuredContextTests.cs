using System.Windows.Controls;
using System.Windows.Documents;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

[Collection("Writer UI")]
public sealed class WriterStructuredContextTests
{
    [Fact]
    public void ResolverClassifiesSpecificObjectsAndRejectsReplacedDocument()
    {
        StaTestHelper.Run(() =>
        {
            var textRun = new Run("text");
            var hyperlink = new Hyperlink(new Run("link"))
            {
                NavigateUri = new Uri("https://example.test")
            };
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(textRun);
            paragraph.Inlines.Add(new Run(" "));
            paragraph.Inlines.Add(hyperlink);

            var picture = new InlineUIContainer(new Image());
            var tableText = new Run("cell");
            var cellParagraph = new Paragraph(tableText);
            cellParagraph.Inlines.Add(new Run(" "));
            cellParagraph.Inlines.Add(picture);
            var cell = new TableCell(cellParagraph);
            var row = new TableRow();
            row.Cells.Add(cell);
            var group = new TableRowGroup();
            group.Rows.Add(row);
            var table = new Table();
            table.RowGroups.Add(group);
            var document = new FlowDocument(paragraph);
            document.Blocks.Add(table);
            var editor = new RichTextBox { Document = document };
            using var tables = new WriterTableService(editor);
            var resolver = new WriterStructuredContextResolver(editor, tables);

            var text = resolver.Capture(Target(document, textRun.ContentStart, textRun.ContentEnd));
            var link = resolver.Capture(Target(document, hyperlink.ContentStart, hyperlink.ContentEnd));
            var tableContext = resolver.Capture(Target(document,
                tableText.ContentStart, tableText.ContentEnd));
            var pictureContext = resolver.Capture(Target(document,
                picture.ElementStart, picture.ElementEnd));

            Assert.Equal(WriterStructuredContextKind.Text, text.Kind);
            Assert.Equal(WriterStructuredContextKind.Hyperlink, link.Kind);
            Assert.Same(hyperlink, link.Hyperlink);
            Assert.Equal(WriterStructuredContextKind.Table, tableContext.Kind);
            Assert.Same(table, tableContext.Table);
            Assert.Same(cell, tableContext.TableCell);
            Assert.Equal(WriterStructuredContextKind.Picture, pictureContext.Kind);
            Assert.Same(picture, pictureContext.Picture);
            Assert.True(resolver.IsCurrent(text));
            Assert.True(resolver.IsCurrent(link));
            Assert.True(resolver.IsCurrent(tableContext));
            Assert.True(resolver.IsCurrent(pictureContext));

            editor.Document = new FlowDocument(new Paragraph(new Run("replacement")));

            Assert.False(resolver.IsCurrent(text));
            Assert.False(resolver.IsCurrent(link));
            Assert.False(resolver.IsCurrent(tableContext));
            Assert.False(resolver.IsCurrent(pictureContext));
        });
    }

    private static WriterEditorContextMenuTarget Target(
        FlowDocument document,
        TextPointer start,
        TextPointer end) => new(start, end, document);
}
