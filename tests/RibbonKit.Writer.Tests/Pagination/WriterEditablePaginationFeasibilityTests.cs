using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Pagination;

/// <summary>
/// Bounded W2-G characterization of the stock WPF editing and pagination surfaces.
/// These tests deliberately do not prototype a replacement Writer control.
/// </summary>
public sealed class WriterEditablePaginationFeasibilityTests
{
    [Fact]
    public void LiveDocumentPaginatorTracksNativeCrossPageEditingAndMatchesAcceptedPreviewInputs()
    {
        StaTestHelper.Run(() =>
        {
            var settings = DocumentPageSettings.Letter();
            var document = CreateDeterministicDocument(settings);
            var editor = new RichTextBox
            {
                Document = document,
                IsUndoEnabled = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            using var host = ShowInBottomlessEditorHost(editor, settings.WidthDip);
            ApplyPageSettings(document, settings);
            host.UpdateLayout();

            var livePaginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(
                ((IDocumentPaginatorSource)document).DocumentPaginator);
            livePaginator.PageSize = new Size(settings.WidthDip, settings.HeightDip);
            livePaginator.ComputePageCount();
            Assert.True(livePaginator.PageCount >= 3);

            var pageTwo = livePaginator.GetPage(1);
            var pageTwoStart = Assert.IsType<TextPointer>(livePaginator.GetPagePosition(pageTwo));
            var previousPagePosition = FindInsertionPositionOnPage(
                livePaginator, pageTwoStart, LogicalDirection.Backward, 0);
            var secondPagePosition = FindInsertionPositionOnPage(
                livePaginator, pageTwoStart, LogicalDirection.Forward, 1);

            editor.Focus();
            editor.Selection.Select(previousPagePosition, secondPagePosition);
            Assert.Equal(0, livePaginator.GetPageNumber(editor.Selection.Start));
            Assert.Equal(1, livePaginator.GetPageNumber(editor.Selection.End));

            var originalText = GetText(document);
            editor.Selection.Text = "cross-page typing";
            Assert.True(editor.CanUndo);
            Assert.NotEqual(originalText, GetText(document));

            editor.Undo();
            Assert.Equal(originalText, GetText(document));
            Assert.True(editor.CanRedo);

            editor.Redo();
            var replacedText = GetText(document);
            Assert.NotEqual(originalText, replacedText);
            Assert.True(editor.CanUndo);

            editor.Undo();
            livePaginator.ComputePageCount();
            using var acceptedPreview = new WriterPreviewCloneService().CreateSnapshot(document, settings);

            Assert.Equal(livePaginator.PageCount, acceptedPreview.PrintPaginator.PageCount);
            Assert.Equal(GetPageStartOffsets(document, livePaginator),
                GetPageStartOffsets(acceptedPreview.SourceClone,
                    Assert.IsAssignableFrom<DynamicDocumentPaginator>(acceptedPreview.PrintPaginator)));
        });
    }

    [Fact]
    public void RichTextBoxFormatsThePaginatedDocumentAsOneBottomlessEditingSurface()
    {
        StaTestHelper.Run(() =>
        {
            var settings = DocumentPageSettings.Letter();
            var document = CreateDeterministicDocument(settings);
            var editor = new RichTextBox
            {
                Document = document,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            using var host = ShowInBottomlessEditorHost(editor, settings.WidthDip);
            ApplyPageSettings(document, settings);
            host.UpdateLayout();

            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            paginator.PageSize = new Size(settings.WidthDip, settings.HeightDip);
            paginator.ComputePageCount();
            Assert.True(paginator.PageCount >= 3);

            var renderScope = FindVisualDescendant(editor,
                element => element.GetType().FullName == "MS.Internal.Documents.FlowDocumentView");
            Assert.NotNull(renderScope);
            Assert.Null(FindVisualDescendant(editor, element => element is DocumentPageView));

            // The finite paginator repeats the top and bottom margins on every page. The editor's
            // bottomless formatter applies them only once around one continuous layout, so its
            // desired height is materially shorter than the same content's stacked page boxes.
            var stackedPageHeight = paginator.PageCount * settings.HeightDip;
            var repeatedMarginHeight = (paginator.PageCount - 1)
                * (settings.Margins.TopDip + settings.Margins.BottomDip);
            Assert.True(editor.ActualHeight < stackedPageHeight - repeatedMarginHeight / 2,
                $"Bottomless editor height {editor.ActualHeight:0.###}; " +
                $"paginated stack {stackedPageHeight:0.###}; pages {paginator.PageCount}.");
        });
    }

    [Fact]
    public void StockPagedViewerCanReferenceTheLiveDocumentButOnlyProvidesReadOnlySelection()
    {
        StaTestHelper.Run(() =>
        {
            var settings = DocumentPageSettings.Letter();
            var document = CreateDeterministicDocument(settings);
            var editor = new RichTextBox { Document = document };
            var viewer = new FlowDocumentPageViewer();
            ApplyPageSettings(document, settings);

            viewer.Document = document;
            Assert.Same(document, editor.Document);
            Assert.Same(document, viewer.Document);

            Assert.NotNull(typeof(FlowDocumentPageViewer).GetProperty("Selection"));
            Assert.Null(typeof(FlowDocumentPageViewer).GetProperty("CaretPosition"));
            Assert.Null(typeof(FlowDocumentPageViewer).GetProperty("CanUndo"));

            var readDocument = CreateDeterministicDocument(settings);
            var readViewer = new FlowDocumentPageViewer { Document = readDocument };
            using var readHost = Show(readViewer, settings.WidthDip, 700);
            readViewer.Selection.Select(readDocument.ContentStart, readDocument.ContentEnd);
            Assert.False(readViewer.Selection.IsEmpty);
            Assert.False(EditingCommands.Delete.CanExecute(null, readViewer));
            Assert.False(ApplicationCommands.Paste.CanExecute(null, readViewer));
            Assert.False(ApplicationCommands.Undo.CanExecute(null, readViewer));
            Assert.False(ApplicationCommands.Redo.CanExecute(null, readViewer));
        });
    }

    private static FlowDocument CreateDeterministicDocument(DocumentPageSettings settings)
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14
        };
        ApplyPageSettings(document, settings);

        for (var index = 0; index < 180; index++)
        {
            document.Blocks.Add(new Paragraph(new Run(
                $"Paragraph {index:D3}: deterministic W2-G pagination corpus."))
            {
                Margin = new Thickness(0, 0, 0, 6)
            });
        }

        return document;
    }

    private static void ApplyPageSettings(FlowDocument document, DocumentPageSettings settings)
    {
        document.PageWidth = settings.WidthDip;
        document.PageHeight = settings.HeightDip;
        document.PagePadding = new Thickness(settings.Margins.LeftDip, settings.Margins.TopDip,
            settings.Margins.RightDip, settings.Margins.BottomDip);
        document.ColumnWidth = settings.ContentWidthDip;
        document.ColumnGap = 0;
        document.IsColumnWidthFlexible = false;
    }

    private static TextPointer FindInsertionPositionOnPage(DynamicDocumentPaginator paginator,
        TextPointer origin, LogicalDirection direction, int pageNumber)
    {
        for (var position = origin.GetInsertionPosition(direction);
             position is not null;
             position = position.GetNextInsertionPosition(direction))
        {
            if (paginator.GetPageNumber(position) == pageNumber)
                return position;
        }

        throw new Xunit.Sdk.XunitException(
            $"No insertion position was found on paginator page {pageNumber + 1}.");
    }

    private static int[] GetPageStartOffsets(FlowDocument document,
        DynamicDocumentPaginator paginator)
    {
        var offsets = new int[paginator.PageCount];
        for (var pageNumber = 0; pageNumber < paginator.PageCount; pageNumber++)
        {
            var page = paginator.GetPage(pageNumber);
            var position = Assert.IsType<TextPointer>(paginator.GetPagePosition(page));
            offsets[pageNumber] = document.ContentStart.GetOffsetToPosition(position);
        }
        return offsets;
    }

    private static string GetText(FlowDocument document) =>
        new TextRange(document.ContentStart, document.ContentEnd).Text;

    private static HostedWindow ShowInBottomlessEditorHost(RichTextBox editor, double width)
    {
        var viewport = new ScrollViewer
        {
            Content = editor,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        return Show(viewport, width + 80, 700);
    }

    private static HostedWindow Show(FrameworkElement content, double width, double height)
    {
        var window = CreateWindow(content, width, height);
        window.Show();
        window.UpdateLayout();
        window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
        return new HostedWindow(window);
    }

    private static Window CreateWindow(FrameworkElement content, double width, double height) =>
        new()
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

    private static DependencyObject? FindVisualDescendant(DependencyObject root,
        Func<DependencyObject, bool> predicate)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (predicate(child))
                return child;
            var descendant = FindVisualDescendant(child, predicate);
            if (descendant is not null)
                return descendant;
        }
        return null;
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
}
