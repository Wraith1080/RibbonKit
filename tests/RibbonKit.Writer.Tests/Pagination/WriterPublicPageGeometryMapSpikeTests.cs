using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Tests.Document;
using Xunit;
using Xunit.Abstractions;

namespace RibbonKit.Writer.Tests.Pagination;

/// <summary>Bounded W2-G spike for public page-local geometry APIs.</summary>
public sealed class WriterPublicPageGeometryMapSpikeTests(ITestOutputHelper output)
{
    [Fact]
    public void PublicCharacterRectCanLocatePaginatorPageStartsOnAnIsolatedClone()
    {
        StaTestHelper.Run(() =>
        {
            var settings = DocumentPageSettings.Letter();
            var source = CreateParagraphDocument(settings, 180);
            var stopwatch = Stopwatch.StartNew();
            using var snapshot = new WriterPreviewCloneService().CreateSnapshot(source, settings);
            var cloneMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            var paginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(snapshot.PrintPaginator);
            var viewer = new FlowDocumentPageViewer { Document = snapshot.SourceClone };
            using var host = Show(viewer, settings.WidthDip + 120, settings.HeightDip + 120);

            var samples = new List<(int PageNumber, int Offset, Rect Rect)>();
            for (var pageNumber = 0; pageNumber < paginator.PageCount; pageNumber++)
            {
                viewer.GoToPage(pageNumber + 1);
                host.UpdateLayout();
                var page = paginator.GetPage(pageNumber);
                var position = Assert.IsType<TextPointer>(paginator.GetPagePosition(page));
                viewer.Selection.Select(position, position);
                var rect = position.GetCharacterRect(LogicalDirection.Forward);
                samples.Add((pageNumber, snapshot.SourceClone.ContentStart.GetOffsetToPosition(position), rect));
            }

            stopwatch.Stop();
            Assert.True(samples.Count >= 3);
            Assert.All(samples, sample =>
            {
                Assert.False(sample.Rect.IsEmpty);
                Assert.True(double.IsFinite(sample.Rect.X));
                Assert.True(double.IsFinite(sample.Rect.Y));
                Assert.True(sample.Rect.Height > 0);
            });
            Assert.Equal(samples.Count, samples.Select(sample => sample.Offset).Distinct().Count());

            output.WriteLine($"Pages={samples.Count}; clone={cloneMilliseconds:0.###} ms; total={stopwatch.Elapsed.TotalMilliseconds:0.###} ms");
            foreach (var sample in samples)
                output.WriteLine($"Page {sample.PageNumber + 1}: offset={sample.Offset}; rect={sample.Rect}");
        });
    }

    [Fact]
    public void CachedPublicInsertionGeometryRoundTripsPlainParagraphPoints()
    {
        StaTestHelper.Run(() =>
        {
            var settings = DocumentPageSettings.Letter();
            var source = CreateParagraphDocument(settings, 180);
            using var snapshot = new WriterPreviewCloneService().CreateSnapshot(source, settings);
            var paginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(snapshot.PrintPaginator);
            var viewer = new FlowDocumentPageViewer { Document = snapshot.SourceClone };
            using var host = Show(viewer, settings.WidthDip + 120, 800);

            var stopwatch = Stopwatch.StartNew();
            var map = PublicPageGeometryMap.Build(snapshot.SourceClone, paginator, viewer, host);
            stopwatch.Stop();
            var sourceParagraphs = source.Blocks.OfType<Paragraph>().ToArray();
            var cloneParagraphs = snapshot.SourceClone.Blocks.OfType<Paragraph>().ToArray();

            foreach (var paragraphIndex in new[] { 0, 35, 72, 109, 146, 179 })
            {
                var sourceRun = Assert.IsType<Run>(sourceParagraphs[paragraphIndex].Inlines.FirstInline);
                var cloneRun = Assert.IsType<Run>(cloneParagraphs[paragraphIndex].Inlines.FirstInline);
                var sourcePosition = sourceRun.ContentStart.GetPositionAtOffset(12);
                var clonePosition = cloneRun.ContentStart.GetPositionAtOffset(12);
                Assert.NotNull(sourcePosition);
                Assert.NotNull(clonePosition);
                var sourceOffset = source.ContentStart.GetOffsetToPosition(sourcePosition);
                var cloneOffset = snapshot.SourceClone.ContentStart.GetOffsetToPosition(clonePosition);
                Assert.Equal(sourceOffset, cloneOffset);

                var entry = map.GetByOffset(cloneOffset);
                var point = new Point(entry.PageRect.X, entry.PageRect.Y + entry.PageRect.Height / 2);
                Assert.Equal(cloneOffset, map.HitTest(entry.PageNumber, point).SourceOffset);
            }

            Assert.Equal(paginator.PageCount, map.PageCount);
            Assert.True(map.Entries.Count > 10000);
            output.WriteLine($"Plain map: pages={map.PageCount}; entries={map.Entries.Count}; build={stopwatch.Elapsed.TotalMilliseconds:0.###} ms");
        });
    }

    [Fact]
    public void PublicMapPreservesSpanningTableOffsetsAndImageHitGeometry()
    {
        StaTestHelper.Run(() =>
        {
            var settings = DocumentPageSettings.Letter();
            var source = CreateStructuredDocument(settings, out var sourceTableRuns,
                out var sourceImageContainer);
            var snapshotStopwatch = Stopwatch.StartNew();
            using var snapshot = new WriterPreviewCloneService().CreateSnapshot(source, settings);
            var snapshotMilliseconds = snapshotStopwatch.Elapsed.TotalMilliseconds;
            var paginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(snapshot.PrintPaginator);
            var viewer = new FlowDocumentPageViewer { Document = snapshot.SourceClone };
            using var host = Show(viewer, settings.WidthDip + 120, 800);

            var mapStopwatch = Stopwatch.StartNew();
            var map = PublicPageGeometryMap.Build(snapshot.SourceClone, paginator, viewer, host);
            mapStopwatch.Stop();
            var cloneTable = Assert.Single(snapshot.SourceClone.Blocks.OfType<Table>());
            var cloneRows = Assert.Single(cloneTable.RowGroups).Rows;
            var cloneTableRuns = cloneRows
                .Select(row => Assert.IsType<Run>(row.Cells[0].Blocks.FirstBlock is Paragraph paragraph
                    ? paragraph.Inlines.FirstInline
                    : null))
                .ToArray();

            var firstTablePage = paginator.GetPageNumber(cloneTableRuns[0].ContentStart);
            var lastTablePage = paginator.GetPageNumber(cloneTableRuns[^1].ContentStart);
            Assert.True(lastTablePage > firstTablePage);
            foreach (var rowIndex in new[] { 0, 18, 36, 54 })
            {
                var sourcePosition = sourceTableRuns[rowIndex].ContentStart.GetPositionAtOffset(8);
                var clonePosition = cloneTableRuns[rowIndex].ContentStart.GetPositionAtOffset(8);
                Assert.NotNull(sourcePosition);
                Assert.NotNull(clonePosition);
                var sourceOffset = source.ContentStart.GetOffsetToPosition(sourcePosition);
                var cloneOffset = snapshot.SourceClone.ContentStart.GetOffsetToPosition(clonePosition);
                Assert.Equal(sourceOffset, cloneOffset);
                var entry = map.GetByOffset(cloneOffset);
                var hit = map.HitTest(entry.PageNumber,
                    new Point(entry.PageRect.X, entry.PageRect.Y + entry.PageRect.Height / 2));
                Assert.Equal(cloneOffset, hit.SourceOffset);
            }

            var cloneImageContainer = snapshot.SourceClone.Blocks.OfType<Paragraph>()
                .SelectMany(paragraph => paragraph.Inlines.OfType<InlineUIContainer>())
                .Single();
            Assert.Equal(source.ContentStart.GetOffsetToPosition(sourceImageContainer.ElementStart),
                snapshot.SourceClone.ContentStart.GetOffsetToPosition(cloneImageContainer.ElementStart));
            var imagePageNumber = paginator.GetPageNumber(cloneImageContainer.ContentStart);
            viewer.GoToPage(imagePageNumber + 1);
            host.UpdateLayout();
            var pageView = Assert.Single(viewer.PageViews, page => page.PageNumber == imagePageNumber);
            var cloneImage = Assert.IsType<Image>(cloneImageContainer.Child);
            var imageOrigin = cloneImage.TranslatePoint(new Point(0, 0), pageView);
            var imageBounds = new Rect(imageOrigin, cloneImage.RenderSize);
            Assert.False(imageBounds.IsEmpty);
            Assert.True(imageBounds.Width > 0);
            Assert.True(imageBounds.Height > 0);
            var imageHit = pageView.InputHitTest(new Point(imageBounds.Left + imageBounds.Width / 2,
                imageBounds.Top + imageBounds.Height / 2));
            Assert.Same(cloneImage, imageHit);

            output.WriteLine($"Structured map: pages={map.PageCount}; table={firstTablePage + 1}-{lastTablePage + 1}; entries={map.Entries.Count}; snapshot={snapshotMilliseconds:0.###} ms; map={mapStopwatch.Elapsed.TotalMilliseconds:0.###} ms; image={imageBounds}");
        });
    }

    private static FlowDocument CreateParagraphDocument(DocumentPageSettings settings, int count)
    {
        var document = new FlowDocument
        {
            PageWidth = settings.WidthDip,
            PageHeight = settings.HeightDip,
            PagePadding = new Thickness(settings.Margins.LeftDip, settings.Margins.TopDip,
                settings.Margins.RightDip, settings.Margins.BottomDip),
            ColumnWidth = settings.ContentWidthDip,
            ColumnGap = 0,
            IsColumnWidthFlexible = false,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14
        };
        for (var index = 0; index < count; index++)
        {
            document.Blocks.Add(new Paragraph(new Run(
                $"Paragraph {index:D3}: deterministic public page geometry corpus."))
            {
                Margin = new Thickness(0, 0, 0, 6)
            });
        }
        return document;
    }

    private static FlowDocument CreateStructuredDocument(DocumentPageSettings settings,
        out Run[] firstColumnRuns, out InlineUIContainer imageContainer)
    {
        var document = CreateParagraphDocument(settings, 12);
        var table = new Table { CellSpacing = 2 };
        table.Columns.Add(new TableColumn { Width = new GridLength(260) });
        table.Columns.Add(new TableColumn { Width = new GridLength(260) });
        var rowGroup = new TableRowGroup();
        table.RowGroups.Add(rowGroup);
        firstColumnRuns = new Run[55];
        for (var rowIndex = 0; rowIndex < firstColumnRuns.Length; rowIndex++)
        {
            var firstRun = new Run($"Table row {rowIndex:D2}, first column public geometry.");
            firstColumnRuns[rowIndex] = firstRun;
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(firstRun) { Margin = new Thickness(2) })
            {
                Padding = new Thickness(3),
                BorderThickness = new Thickness(0.5),
                BorderBrush = Brushes.Gray
            });
            row.Cells.Add(new TableCell(new Paragraph(new Run(
                $"Table row {rowIndex:D2}, second column.")) { Margin = new Thickness(2) })
            {
                Padding = new Thickness(3),
                BorderThickness = new Thickness(0.5),
                BorderBrush = Brushes.Gray
            });
            rowGroup.Rows.Add(row);
        }
        document.Blocks.Add(table);

        var bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null,
            new byte[] { 0x20, 0x80, 0xD0, 0xFF }, 4);
        bitmap.Freeze();
        var image = new Image { Source = bitmap, Width = 180, Height = 120 };
        imageContainer = new InlineUIContainer(image) { BaselineAlignment = BaselineAlignment.Center };
        var imageParagraph = new Paragraph(new Run("Before structured image "));
        imageParagraph.Inlines.Add(imageContainer);
        imageParagraph.Inlines.Add(new Run(" after structured image."));
        document.Blocks.Add(imageParagraph);
        for (var index = 0; index < 20; index++)
            document.Blocks.Add(new Paragraph(new Run($"Structured tail {index:D2}.")));
        return document;
    }

    private static HostedWindow Show(FrameworkElement content, double width, double height)
    {
        var window = new Window
        {
            Content = content,
            Width = width,
            Height = height,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
            Opacity = 0.01
        };
        window.Show();
        window.UpdateLayout();
        window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
        return new HostedWindow(window);
    }

    private sealed class HostedWindow(Window window) : IDisposable
    {
        public void UpdateLayout()
        {
            window.UpdateLayout();
            window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
        }

        public void Dispose()
        {
            if (window.IsVisible)
                window.Close();
        }
    }

    private sealed class PublicPageGeometryMap
    {
        private readonly Dictionary<int, PublicPageGeometryEntry> _byOffset;
        private readonly Dictionary<int, PublicPageGeometryEntry[]> _byPage;

        private PublicPageGeometryMap(int pageCount, List<PublicPageGeometryEntry> entries)
        {
            PageCount = pageCount;
            Entries = entries;
            _byOffset = entries.GroupBy(entry => entry.SourceOffset)
                .ToDictionary(group => group.Key, group => group.First());
            _byPage = entries.GroupBy(entry => entry.PageNumber)
                .ToDictionary(group => group.Key, group => group.ToArray());
        }

        public int PageCount { get; }
        public IReadOnlyList<PublicPageGeometryEntry> Entries { get; }

        public PublicPageGeometryEntry GetByOffset(int sourceOffset) => _byOffset[sourceOffset];

        public PublicPageGeometryEntry HitTest(int pageNumber, Point point)
        {
            var pageEntries = _byPage[pageNumber];
            return pageEntries.MinBy(entry => DistanceSquared(entry.PageRect, point))!;
        }

        public static PublicPageGeometryMap Build(FlowDocument clone,
            DynamicDocumentPaginator paginator, FlowDocumentPageViewer viewer, HostedWindow host)
        {
            var entries = new List<PublicPageGeometryEntry>();
            for (var pageNumber = 0; pageNumber < paginator.PageCount; pageNumber++)
            {
                viewer.GoToPage(pageNumber + 1);
                host.UpdateLayout();
                var pageView = Assert.Single(viewer.PageViews,
                    page => page.PageNumber == pageNumber);
                var pageStart = Assert.IsType<TextPointer>(
                    paginator.GetPagePosition(paginator.GetPage(pageNumber)));
                var pageEnd = pageNumber + 1 < paginator.PageCount
                    ? Assert.IsType<TextPointer>(paginator.GetPagePosition(paginator.GetPage(pageNumber + 1)))
                    : clone.ContentEnd;

                for (var position = pageStart.GetInsertionPosition(LogicalDirection.Forward);
                     position is not null && position.CompareTo(pageEnd) < 0;
                     position = position.GetNextInsertionPosition(LogicalDirection.Forward))
                {
                    if (paginator.GetPageNumber(position) != pageNumber)
                        continue;
                    var rect = position.GetCharacterRect(LogicalDirection.Forward);
                    if (rect.IsEmpty || !double.IsFinite(rect.X) || !double.IsFinite(rect.Y) || rect.Height <= 0)
                        continue;
                    Assert.InRange(rect.Left, -0.5, pageView.ActualWidth + 0.5);
                    Assert.InRange(rect.Top, -0.5, pageView.ActualHeight + 0.5);
                    Assert.InRange(rect.Bottom, -0.5, pageView.ActualHeight + 0.5);
                    entries.Add(new PublicPageGeometryEntry(
                        clone.ContentStart.GetOffsetToPosition(position), pageNumber, rect));
                }
            }
            return new PublicPageGeometryMap(paginator.PageCount, entries);
        }

        private static double DistanceSquared(Rect rect, Point point)
        {
            var xDistance = Math.Abs(point.X - rect.X);
            var yDistance = point.Y < rect.Top
                ? rect.Top - point.Y
                : point.Y > rect.Bottom
                    ? point.Y - rect.Bottom
                    : 0;
            return xDistance * xDistance + yDistance * yDistance * 16;
        }
    }

    private sealed record PublicPageGeometryEntry(int SourceOffset, int PageNumber, Rect PageRect);
}
