using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Tests.Document;
using Xunit;
using Xunit.Abstractions;

namespace RibbonKit.Writer.Tests.Pagination;

/// <summary>
/// Test-only W2-G proof for structured anchors, page-local Writer chrome and viewport transforms.
/// It deliberately does not replace the production Paper surface.
/// </summary>
public sealed class WriterStructuredViewportPrototypeTests(ITestOutputHelper output)
{
    [Fact]
    public void StructuredAnchorsAndLiveSelectionSurviveAcceptedPaginatorReflow()
    {
        StaTestHelper.Run(() =>
        {
            var openingSettings = DocumentPageSettings.Letter();
            var source = CreateStructuredDocument(openingSettings, FlowDirection.LeftToRight,
                out var anchors);
            var editor = new RichTextBox { Document = source, IsUndoEnabled = true };
            using var editorHost = Show(editor, 720, 520);
            editor.Focus();
            editor.Selection.Select(
                anchors.TableRuns[18].ContentStart.GetPositionAtOffset(4)!,
                anchors.Hyperlink.ContentStart.GetPositionAtOffset(8)!);
            var selection = SelectionOffsets(editor);
            var authoritativeDocument = editor.Document;

            var portrait = MeasureStructuredSnapshot(source, openingSettings);
            var landscapeSettings = openingSettings.WithOrientation(
                DocumentPageOrientation.Landscape);
            var landscape = MeasureStructuredSnapshot(source, landscapeSettings);

            Assert.Same(authoritativeDocument, editor.Document);
            Assert.Equal(selection, SelectionOffsets(editor));
            Assert.True(editor.IsKeyboardFocusWithin);
            Assert.True(portrait.LastTablePage > portrait.FirstTablePage);
            Assert.True(landscape.LastTablePage > landscape.FirstTablePage);
            Assert.Equal(anchors.SourceOffsets, portrait.CloneOffsets);
            Assert.Equal(anchors.SourceOffsets, landscape.CloneOffsets);
            Assert.True(portrait.PageCount != landscape.PageCount
                || portrait.ImagePage != landscape.ImagePage
                || portrait.HyperlinkPage != landscape.HyperlinkPage
                || portrait.LastTablePage != landscape.LastTablePage);

            output.WriteLine($"Structured reflow: portrait={portrait.PageCount} pages, " +
                $"table={portrait.FirstTablePage + 1}-{portrait.LastTablePage + 1}, " +
                $"image={portrait.ImagePage + 1}, link={portrait.HyperlinkPage + 1}; " +
                $"landscape={landscape.PageCount} pages, " +
                $"table={landscape.FirstTablePage + 1}-{landscape.LastTablePage + 1}, " +
                $"image={landscape.ImagePage + 1}, link={landscape.HyperlinkPage + 1}");
        });
    }

    [Fact]
    public void PageLocalTableAndPictureChromeIsVisualOnlyAndNeverEntersPaginatorContent()
    {
        StaTestHelper.Run(() =>
        {
            var settings = DocumentPageSettings.Letter();
            var source = CreateStructuredDocument(settings, FlowDirection.LeftToRight,
                out _);
            var serializedBefore = XamlWriter.Save(source);
            using var scene = SnapshotScene.Create(source, settings);
            var cloneTable = Assert.Single(scene.Document.Blocks.OfType<Table>());
            var cloneRows = Assert.Single(cloneTable.RowGroups).Rows;
            var tableRun = FirstRun(cloneRows[24].Cells[0]);
            var tablePage = scene.Paginator.GetPageNumber(tableRun.ContentStart);
            scene.ShowPage(tablePage);
            var tableRect = tableRun.ContentStart.GetCharacterRect(LogicalDirection.Forward);
            Assert.False(tableRect.IsEmpty);

            var imageContainer = FindImageContainer(scene.Document);
            var imagePage = scene.Paginator.GetPageNumber(imageContainer.ContentStart);
            scene.ShowPage(imagePage);
            var image = Assert.IsType<Image>(imageContainer.Child);
            var pageView = scene.GetPageView(imagePage);
            var imageOrigin = image.TranslatePoint(new Point(0, 0), pageView);
            var imageRect = new Rect(imageOrigin, image.RenderSize);
            Assert.False(imageRect.IsEmpty);

            var overlays = new Dictionary<int, Canvas>();
            var tableOverlay = GetOverlay(overlays, tablePage, settings);
            AddFrame(tableOverlay, tableRect, "table-selection");
            var pictureOverlay = GetOverlay(overlays, imagePage, settings);
            AddFrame(pictureOverlay, imageRect, "picture-selection");
            foreach (var handle in WriterPictureResizeGeometry.GetHandleRects(
                         image.RenderSize, new DpiScale(1.5, 1.5)).Values)
            {
                var marker = new Rectangle
                {
                    Width = handle.Width,
                    Height = handle.Height,
                    Tag = "picture-resize-handle"
                };
                Canvas.SetLeft(marker, imageRect.Left + handle.Left);
                Canvas.SetTop(marker, imageRect.Top + handle.Top);
                pictureOverlay.Children.Add(marker);
            }

            Assert.Contains(tableOverlay.Children.OfType<Rectangle>(),
                element => Equals(element.Tag, "table-selection"));
            Assert.Contains(pictureOverlay.Children.OfType<Rectangle>(),
                element => Equals(element.Tag, "picture-selection"));
            Assert.Equal(8, pictureOverlay.Children.OfType<Rectangle>()
                .Count(element => Equals(element.Tag, "picture-resize-handle")));
            Assert.DoesNotContain("table-selection", XamlWriter.Save(scene.Document),
                StringComparison.Ordinal);
            Assert.DoesNotContain("picture-selection", XamlWriter.Save(scene.Document),
                StringComparison.Ordinal);
            Assert.Equal(serializedBefore, XamlWriter.Save(source));

            output.WriteLine($"Visual-only chrome: table page={tablePage + 1}; " +
                $"picture page={imagePage + 1}; overlays={overlays.Count}; " +
                $"picture handles=8 at 150% DPI");
        });
    }

    [Fact]
    public void RtlZoomDpiAndScrollTransformHasAnExactInverseForPageHitMapping()
    {
        StaTestHelper.Run(() =>
        {
            var settings = DocumentPageSettings.Letter();
            var source = CreateStructuredDocument(settings, FlowDirection.RightToLeft,
                out _);
            using var scene = SnapshotScene.Create(source, settings);
            var table = Assert.Single(scene.Document.Blocks.OfType<Table>());
            var target = FirstRun(Assert.Single(table.RowGroups).Rows[30].Cells[0])
                .ContentStart.GetPositionAtOffset(7)!;
            var targetOffset = scene.Document.ContentStart.GetOffsetToPosition(target);
            var page = scene.Paginator.GetPageNumber(target);
            scene.ShowPage(page);
            var targetRect = target.GetCharacterRect(LogicalDirection.Forward);
            var pagePoint = new Point(targetRect.X,
                targetRect.Y + targetRect.Height / 2d);
            var pageEntries = BuildPageEntries(scene.Document, scene.Paginator, page);
            Assert.Equal(targetOffset, HitTest(pageEntries, pagePoint));
            var pageCount = scene.Paginator.PageCount;

            var combinations = 0;
            foreach (var zoom in new[] { 0.75, 1.0, 1.5, 2.0 })
            foreach (var dpi in new[] { 1.0, 1.5, 2.0 })
            foreach (var scroll in new[] { new Vector(0, 0), new Vector(137.25, 291.5) })
            {
                var transform = new PageViewportTransform(settings.WidthDip, true, zoom,
                    scroll, new DpiScale(dpi, dpi));
                var pixels = transform.PageToDevice(pagePoint);
                var roundTrip = transform.DeviceToPage(pixels);
                Assert.Equal(pagePoint.X, roundTrip.X, 8);
                Assert.Equal(pagePoint.Y, roundTrip.Y, 8);
                Assert.Equal(targetOffset, HitTest(pageEntries, roundTrip));

                var handles = WriterPictureResizeGeometry.GetHandleRects(
                    new Size(180, 120), new DpiScale(dpi, dpi));
                Assert.All(handles.Values, handle =>
                {
                    Assert.Equal(Math.Round(handle.Left * dpi), handle.Left * dpi, 8);
                    Assert.Equal(Math.Round(handle.Top * dpi), handle.Top * dpi, 8);
                    Assert.Equal(Math.Round(handle.Width * dpi), handle.Width * dpi, 8);
                    Assert.Equal(Math.Round(handle.Height * dpi), handle.Height * dpi, 8);
                });
                combinations++;
            }

            Assert.Equal(pageCount, scene.Paginator.PageCount);
            Assert.Equal(FlowDirection.RightToLeft, scene.Document.FlowDirection);
            output.WriteLine($"Viewport inverse: page={page + 1}; offset={targetOffset}; " +
                $"combinations={combinations}; paginator pages={pageCount}");
        });
    }

    private static StructuredMeasurement MeasureStructuredSnapshot(FlowDocument source,
        DocumentPageSettings settings)
    {
        using var scene = SnapshotScene.Create(source, settings);
        var table = Assert.Single(scene.Document.Blocks.OfType<Table>());
        var rows = Assert.Single(table.RowGroups).Rows;
        var tableRuns = rows.Select(row => FirstRun(row.Cells[0])).ToArray();
        var image = FindImageContainer(scene.Document);
        var hyperlink = scene.Document.Blocks.OfType<Paragraph>()
            .SelectMany(paragraph => paragraph.Inlines.OfType<Hyperlink>()).Single();
        var cloneOffsets = new[] { 0, 18, 36, 54 }
            .Select(index => scene.Document.ContentStart.GetOffsetToPosition(
                tableRuns[index].ContentStart.GetPositionAtOffset(8)!))
            .Append(scene.Document.ContentStart.GetOffsetToPosition(image.ElementStart))
            .Append(scene.Document.ContentStart.GetOffsetToPosition(hyperlink.ElementStart))
            .ToArray();
        return new StructuredMeasurement(scene.Paginator.PageCount,
            scene.Paginator.GetPageNumber(tableRuns[0].ContentStart),
            scene.Paginator.GetPageNumber(tableRuns[^1].ContentStart),
            scene.Paginator.GetPageNumber(image.ContentStart),
            scene.Paginator.GetPageNumber(hyperlink.ContentStart), cloneOffsets);
    }

    private static FlowDocument CreateStructuredDocument(DocumentPageSettings settings,
        FlowDirection flowDirection, out StructuredAnchors anchors)
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
            FontSize = 14,
            FlowDirection = flowDirection
        };
        for (var index = 0; index < 12; index++)
            document.Blocks.Add(new Paragraph(new Run(
                $"Opening paragraph {index:D2} for structured pagination.")));

        var table = new Table { CellSpacing = 2 };
        table.Columns.Add(new TableColumn { Width = new GridLength(250) });
        table.Columns.Add(new TableColumn { Width = new GridLength(250) });
        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        var runs = new Run[55];
        for (var rowIndex = 0; rowIndex < runs.Length; rowIndex++)
        {
            runs[rowIndex] = new Run($"Table row {rowIndex:D2}, authoritative first column.");
            var row = new TableRow();
            row.Cells.Add(Cell(runs[rowIndex]));
            row.Cells.Add(Cell(new Run($"Table row {rowIndex:D2}, second column.")));
            group.Rows.Add(row);
        }
        document.Blocks.Add(table);

        var bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null,
            new byte[] { 0x20, 0x80, 0xD0, 0xFF }, 4);
        bitmap.Freeze();
        var imageContainer = new InlineUIContainer(new Image
        {
            Source = bitmap,
            Width = 180,
            Height = 120
        });
        var imageParagraph = new Paragraph(new Run("Before image "));
        imageParagraph.Inlines.Add(imageContainer);
        imageParagraph.Inlines.Add(new Run(" after image."));
        document.Blocks.Add(imageParagraph);

        var hyperlink = new Hyperlink(new Run("authoritative hyperlink anchor"))
        {
            NavigateUri = new Uri("https://example.invalid/w2-g")
        };
        var linkParagraph = new Paragraph(new Run("Before link "));
        linkParagraph.Inlines.Add(hyperlink);
        linkParagraph.Inlines.Add(new Run(" after link."));
        document.Blocks.Add(linkParagraph);
        for (var index = 0; index < 24; index++)
            document.Blocks.Add(new Paragraph(new Run($"Structured tail {index:D2}.")));

        var offsets = new[] { 0, 18, 36, 54 }
            .Select(index => document.ContentStart.GetOffsetToPosition(
                runs[index].ContentStart.GetPositionAtOffset(8)!))
            .Append(document.ContentStart.GetOffsetToPosition(imageContainer.ElementStart))
            .Append(document.ContentStart.GetOffsetToPosition(hyperlink.ElementStart))
            .ToArray();
        anchors = new StructuredAnchors(runs, imageContainer, hyperlink, offsets);
        return document;

        static TableCell Cell(Run run) => new(new Paragraph(run)
        {
            Margin = new Thickness(2)
        })
        {
            Padding = new Thickness(3),
            BorderThickness = new Thickness(0.5),
            BorderBrush = Brushes.Gray
        };
    }

    private static Run FirstRun(TableCell cell) => Assert.IsType<Run>(
        Assert.IsType<Paragraph>(cell.Blocks.FirstBlock).Inlines.FirstInline);

    private static InlineUIContainer FindImageContainer(FlowDocument document) =>
        document.Blocks.OfType<Paragraph>()
            .SelectMany(paragraph => paragraph.Inlines.OfType<InlineUIContainer>()).Single();

    private static (int Start, int End) SelectionOffsets(RichTextBox editor) => (
        editor.Document.ContentStart.GetOffsetToPosition(editor.Selection.Start),
        editor.Document.ContentStart.GetOffsetToPosition(editor.Selection.End));

    private static Canvas GetOverlay(IDictionary<int, Canvas> overlays, int page,
        DocumentPageSettings settings)
    {
        if (overlays.TryGetValue(page, out var existing))
            return existing;
        var canvas = new Canvas
        {
            Width = settings.WidthDip,
            Height = settings.HeightDip,
            IsHitTestVisible = false
        };
        overlays.Add(page, canvas);
        return canvas;
    }

    private static void AddFrame(Canvas canvas, Rect bounds, string tag)
    {
        var frame = new Rectangle
        {
            Width = Math.Max(1, bounds.Width),
            Height = Math.Max(1, bounds.Height),
            Stroke = Brushes.DodgerBlue,
            StrokeThickness = 1,
            Fill = Brushes.Transparent,
            Tag = tag
        };
        Canvas.SetLeft(frame, bounds.Left);
        Canvas.SetTop(frame, bounds.Top);
        canvas.Children.Add(frame);
    }

    private static PageEntry[] BuildPageEntries(FlowDocument document,
        DynamicDocumentPaginator paginator, int page)
    {
        var start = Assert.IsType<TextPointer>(paginator.GetPagePosition(paginator.GetPage(page)));
        var end = page + 1 < paginator.PageCount
            ? Assert.IsType<TextPointer>(paginator.GetPagePosition(paginator.GetPage(page + 1)))
            : document.ContentEnd;
        var entries = new List<PageEntry>();
        for (var position = start.GetInsertionPosition(LogicalDirection.Forward);
             position is not null && position.CompareTo(end) < 0;
             position = position.GetNextInsertionPosition(LogicalDirection.Forward))
        {
            if (paginator.GetPageNumber(position) != page)
                continue;
            var rect = position.GetCharacterRect(LogicalDirection.Forward);
            if (rect.IsEmpty || !double.IsFinite(rect.X) || !double.IsFinite(rect.Y))
                continue;
            entries.Add(new PageEntry(document.ContentStart.GetOffsetToPosition(position), rect));
        }
        return entries.ToArray();
    }

    private static int HitTest(IEnumerable<PageEntry> entries, Point point) => entries
        .MinBy(entry => Math.Pow(entry.Rect.X - point.X, 2)
            + Math.Pow(entry.Rect.Y + entry.Rect.Height / 2d - point.Y, 2) * 16)!.Offset;

    private static HostedWindow Show(FrameworkElement content, double width, double height,
        bool showActivated = true)
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
            ShowActivated = showActivated,
            Opacity = 0.01
        };
        window.Show();
        window.UpdateLayout();
        window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
        return new HostedWindow(window);
    }

    private sealed class SnapshotScene : IDisposable
    {
        private readonly WriterPreviewSnapshot _snapshot;
        private readonly HostedWindow _host;

        private SnapshotScene(WriterPreviewSnapshot snapshot, FlowDocumentPageViewer viewer,
            HostedWindow host)
        {
            _snapshot = snapshot;
            Viewer = viewer;
            _host = host;
            Document = snapshot.SourceClone;
            Paginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(snapshot.PrintPaginator);
        }

        internal FlowDocument Document { get; }
        internal DynamicDocumentPaginator Paginator { get; }
        internal FlowDocumentPageViewer Viewer { get; }

        internal static SnapshotScene Create(FlowDocument source, DocumentPageSettings settings)
        {
            var snapshot = new WriterPreviewCloneService().CreateSnapshot(source, settings);
            var viewer = new FlowDocumentPageViewer { Document = snapshot.SourceClone };
            var host = Show(viewer, settings.WidthDip + 120, 800,
                showActivated: false);
            return new SnapshotScene(snapshot, viewer, host);
        }

        internal void ShowPage(int page)
        {
            Viewer.GoToPage(page + 1);
            _host.UpdateLayout();
        }

        internal DocumentPageView GetPageView(int page) => Assert.Single(Viewer.PageViews,
            view => view.PageNumber == page);

        public void Dispose()
        {
            _host.Dispose();
            _snapshot.Dispose();
        }
    }

    private sealed class HostedWindow(Window window) : IDisposable
    {
        internal void UpdateLayout()
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

    private readonly record struct PageViewportTransform(double PageWidth, bool IsRtl,
        double Zoom, Vector Scroll, DpiScale Dpi)
    {
        internal Point PageToDevice(Point page)
        {
            var logicalX = IsRtl ? PageWidth - page.X : page.X;
            return new Point((logicalX * Zoom - Scroll.X) * Dpi.DpiScaleX,
                (page.Y * Zoom - Scroll.Y) * Dpi.DpiScaleY);
        }

        internal Point DeviceToPage(Point device)
        {
            var logicalX = (device.X / Dpi.DpiScaleX + Scroll.X) / Zoom;
            var pageX = IsRtl ? PageWidth - logicalX : logicalX;
            return new Point(pageX,
                (device.Y / Dpi.DpiScaleY + Scroll.Y) / Zoom);
        }
    }

    private sealed record StructuredAnchors(Run[] TableRuns,
        InlineUIContainer Image, Hyperlink Hyperlink, int[] SourceOffsets);
    private sealed record StructuredMeasurement(int PageCount, int FirstTablePage,
        int LastTablePage, int ImagePage, int HyperlinkPage, int[] CloneOffsets);
    private sealed record PageEntry(int Offset, Rect Rect);
}
