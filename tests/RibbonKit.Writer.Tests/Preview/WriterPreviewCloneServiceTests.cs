using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Markup;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Preview;

[Collection(WriterPreviewTestCollection.Name)]
public sealed class WriterPreviewCloneServiceTests
{
    [Fact]
    public void SnapshotUsesDistinctDocumentAndExactOneColumnPageInputs()
    {
        StaTestHelper.Run(() =>
        {
            var source = new FlowDocument(new Paragraph(new Run("live content")))
            {
                PageWidth = 1200,
                PageHeight = 1600,
                PagePadding = new Thickness(7),
                ColumnWidth = 260,
                ColumnGap = 19,
                IsColumnWidthFlexible = true,
                FontSize = 18,
                Foreground = Brushes.DarkBlue
            };
            var settings = DocumentPageSettings.A4(DocumentPageOrientation.Landscape,
                new DocumentPageMargins(24, 36, 48, 60));
            var originalText = new TextRange(source.ContentStart, source.ContentEnd).Text;

            var snapshot = new WriterPreviewCloneService().CreateSnapshot(source, settings);

            Assert.NotSame(source, snapshot.SourceClone);
            Assert.NotSame(source.Blocks.First(), snapshot.SourceClone.Blocks.First());
            Assert.NotSame(((IDocumentPaginatorSource)source).DocumentPaginator, snapshot.Paginator);
            Assert.Same(snapshot.Document.DocumentPaginator, snapshot.Paginator);
            Assert.Equal(settings.WidthDip, snapshot.SourceClone.PageWidth, 4);
            Assert.Equal(settings.HeightDip, snapshot.SourceClone.PageHeight, 4);
            Assert.Equal(new Thickness(24, 36, 48, 60), snapshot.SourceClone.PagePadding);
            Assert.Equal(settings.ContentWidthDip, snapshot.SourceClone.ColumnWidth, 4);
            Assert.Equal(0, snapshot.SourceClone.ColumnGap);
            Assert.False(snapshot.SourceClone.IsColumnWidthFlexible);
            Assert.Equal(0, snapshot.SourceClone.ColumnRuleWidth);
            Assert.Equal(18, snapshot.SourceClone.FontSize);
            Assert.Equal(Brushes.DarkBlue.ToString(), snapshot.SourceClone.Foreground.ToString());
            Assert.Equal(new Size(settings.WidthDip, settings.HeightDip),
                snapshot.Paginator.GetPage(0).Size);
            Assert.Equal(originalText, new TextRange(source.ContentStart, source.ContentEnd).Text);

            ((Run)((Paragraph)snapshot.SourceClone.Blocks.First()).Inlines.FirstInline!).Text = "preview edit";
            Assert.Contains("live content", new TextRange(source.ContentStart, source.ContentEnd).Text);
            ((Run)((Paragraph)source.Blocks.First()).Inlines.FirstInline!).Text = "live edit";
            Assert.Contains("preview edit", new TextRange(
                snapshot.SourceClone.ContentStart, snapshot.SourceClone.ContentEnd).Text);
        });
    }

    [Fact]
    public void XamlPackageRoundTripPreservesTableAndImageContent()
    {
        StaTestHelper.Run(() =>
        {
            var image = new Image
            {
                Width = 12,
                Height = 12,
                Source = new DrawingImage(new GeometryDrawing(Brushes.CornflowerBlue,
                    new Pen(Brushes.DarkBlue, 1), new RectangleGeometry(new Rect(0, 0, 12, 12))))
            };
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run("before "));
            paragraph.Inlines.Add(new InlineUIContainer(image));
            paragraph.Inlines.Add(new Run(" after"));
            var table = new Table();
            var group = new TableRowGroup();
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(new Run("cell one"))));
            row.Cells.Add(new TableCell(new Paragraph(new Run("cell two"))));
            group.Rows.Add(row);
            table.RowGroups.Add(group);
            var source = new FlowDocument();
            source.Blocks.Add(paragraph);
            source.Blocks.Add(table);

            var snapshot = new WriterPreviewCloneService().CreateSnapshot(source,
                DocumentPageSettings.Letter());

            var clonedParagraph = Assert.IsType<Paragraph>(snapshot.SourceClone.Blocks.First());
            var clonedContainer = Assert.IsType<InlineUIContainer>(clonedParagraph.Inlines.ElementAt(1));
            var clonedImage = Assert.IsType<Image>(clonedContainer.Child);
            Assert.NotNull(clonedImage.Source);
            var clonedTable = Assert.IsType<Table>(snapshot.SourceClone.Blocks.ElementAt(1));
            Assert.NotSame(paragraph, clonedParagraph);
            Assert.NotSame(image, clonedImage);
            Assert.NotSame(table, clonedTable);
            Assert.NotSame(group, clonedTable.RowGroups.First());
            Assert.NotSame(row, clonedTable.RowGroups.First().Rows.First());
            Assert.NotSame(row.Cells.First(), clonedTable.RowGroups.First().Rows.First().Cells.First());
            Assert.Equal(2, clonedTable.RowGroups.First().Rows.First().Cells.Count);
            Assert.Equal("cell one", new TextRange(
                clonedTable.RowGroups.First().Rows.First().Cells.First().ContentStart,
                clonedTable.RowGroups.First().Rows.First().Cells.First().ContentEnd).Text.Trim());
            clonedImage.Width = 30;
            clonedTable.RowGroups.First().Rows.First().Cells.First().Blocks.Clear();
            clonedTable.RowGroups.First().Rows.First().Cells.First().Blocks.Add(
                new Paragraph(new Run("preview cell")));
            Assert.Equal(12, image.Width);
            Assert.Equal("cell one", new TextRange(row.Cells.First().ContentStart,
                row.Cells.First().ContentEnd).Text.Trim());
            Assert.NotSame(DocumentPage.Missing, snapshot.Paginator.GetPage(0));
        });
    }

    [Fact]
    public void RoundTripPreservesRepresentativeDocumentFormatting()
    {
        StaTestHelper.Run(() =>
        {
            var paragraph = new Paragraph
            {
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(9, 8, 7, 6)
            };
            paragraph.Inlines.Add(new Bold(new Run("bold")));
            paragraph.Inlines.Add(new Italic(new Run(" italic")));
            var list = new List { MarkerStyle = TextMarkerStyle.Square };
            list.ListItems.Add(new ListItem(new Paragraph(new Run("listed"))));
            var source = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                Language = XmlLanguage.GetLanguage("ar-SA")
            };
            source.Blocks.Add(paragraph);
            source.Blocks.Add(list);

            var snapshot = new WriterPreviewCloneService().CreateSnapshot(source,
                DocumentPageSettings.Letter());

            var clonedParagraph = Assert.IsType<Paragraph>(snapshot.SourceClone.Blocks.First());
            Assert.Equal(FlowDirection.RightToLeft, snapshot.SourceClone.FlowDirection);
            Assert.Equal("ar-SA", snapshot.SourceClone.Language.IetfLanguageTag,
                ignoreCase: true);
            Assert.Equal(TextAlignment.Right, clonedParagraph.TextAlignment);
            Assert.Equal(new Thickness(9, 8, 7, 6), clonedParagraph.Margin);
            var clonedBold = Assert.IsAssignableFrom<Span>(clonedParagraph.Inlines.FirstInline);
            var clonedItalic = Assert.IsAssignableFrom<Span>(clonedParagraph.Inlines.ElementAt(1));
            Assert.Equal(FontWeights.Bold, clonedBold.FontWeight);
            Assert.Equal(FontStyles.Italic, clonedItalic.FontStyle);
            Assert.Equal(TextMarkerStyle.Square,
                Assert.IsType<List>(snapshot.SourceClone.Blocks.ElementAt(1)).MarkerStyle);
        });
    }

    [Fact]
    public void A4AndLetterProduceDeterministicPaginatorPageSizes()
    {
        StaTestHelper.Run(() =>
        {
            var source = new FlowDocument(new Paragraph(new Run("pagination")));
            var service = new WriterPreviewCloneService();
            var a4 = service.CreateSnapshot(source, DocumentPageSettings.A4());
            var letter = service.CreateSnapshot(source, DocumentPageSettings.Letter());

            Assert.Equal(new Size(DocumentPageSettings.A4().WidthDip, DocumentPageSettings.A4().HeightDip),
                a4.Paginator.GetPage(0).Size);
            Assert.Equal(new Size(DocumentPageSettings.Letter().WidthDip, DocumentPageSettings.Letter().HeightDip),
                letter.Paginator.GetPage(0).Size);
            Assert.Equal(1, a4.Paginator.PageCount);
            Assert.Equal(1, letter.Paginator.PageCount);
        });
    }

    [Fact]
    public void RepeatedLongContentSnapshotsProduceTheSamePageCount()
    {
        StaTestHelper.Run(() =>
        {
            var source = new FlowDocument();
            for (var index = 0; index < 160; index++)
                source.Blocks.Add(new Paragraph(new Run($"Deterministic paragraph {index}.")));
            var service = new WriterPreviewCloneService();

            var settingsCases = new[]
            {
                DocumentPageSettings.A4(),
                DocumentPageSettings.A4(DocumentPageOrientation.Landscape,
                    new DocumentPageMargins(30, 40, 50, 60)),
                DocumentPageSettings.Letter(),
                DocumentPageSettings.Letter(DocumentPageOrientation.Landscape,
                    new DocumentPageMargins(42, 36, 42, 36))
            };
            foreach (var settings in settingsCases)
            {
                var first = service.CreateSnapshot(source, settings);
                var second = service.CreateSnapshot(source, settings);

                Assert.True(first.Paginator.PageCount > 1);
                Assert.Equal(first.Paginator.PageCount, second.Paginator.PageCount);
                Assert.Equal(new Size(settings.WidthDip, settings.HeightDip),
                    first.Paginator.GetPage(0).Size);
                Assert.Equal(new Thickness(settings.Margins.LeftDip, settings.Margins.TopDip,
                    settings.Margins.RightDip, settings.Margins.BottomDip), first.SourceClone.PagePadding);
            }
        });
    }
}
