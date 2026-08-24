using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Xps.Packaging;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Preview;

[Collection(WriterPreviewTestCollection.Name)]
public sealed class WriterDocumentPreviewViewTests
{
    [Fact]
    public void HostedDocumentViewerUsesRealSnapshotAndSupportsNavigationAndModes()
    {
        StaTestHelper.Run(() =>
        {
            var document = new FlowDocument();
            for (var i = 0; i < 90; i++)
                document.Blocks.Add(new Paragraph(new Run($"Preview line {i} ")));
            var snapshot = new WriterPreviewCloneService().CreateSnapshot(document,
                DocumentPageSettings.Letter());
            var view = new WriterDocumentPreviewView();
            var host = new Window
            {
                Content = view,
                Width = 900,
                Height = 720,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                Opacity = 0.01
            };
            host.Show();
            view.SetSnapshot(snapshot);
            host.UpdateLayout();
            PumpDispatcher(view.Dispatcher);

            Assert.Same(snapshot.Paginator, view.PrimaryPageView.DocumentPaginator);
            Assert.Equal(0, view.PrimaryPageView.PageNumber);
            Assert.True(view.PageCount > 1);
            Assert.Equal(1, view.CurrentPageNumber);
            Assert.False(view.GoToPreviousPage());
            Assert.True(view.CanGoToNextPage);
            Assert.True(view.GoToNextPage());
            PumpDispatcher(view.Dispatcher);
            Assert.Equal(2, view.CurrentPageNumber);
            Assert.True(view.GoToPreviousPage());
            PumpDispatcher(view.Dispatcher);
            Assert.Equal(1, view.CurrentPageNumber);

            view.ViewMode = WriterPreviewViewMode.TwoPages;
            host.UpdateLayout();
            PumpDispatcher(view.Dispatcher);
            Assert.Equal(WriterPreviewViewMode.TwoPages, view.ViewMode);
            Assert.Same(snapshot.Paginator, view.SecondaryPageView.DocumentPaginator);
            Assert.Equal(1, view.SecondaryPageView.PageNumber);
            Assert.True(CountDescendants<DocumentPageView>(view) >= 2);
            view.ViewMode = WriterPreviewViewMode.PageWidth;
            host.UpdateLayout();
            PumpDispatcher(view.Dispatcher);
            Assert.Equal(WriterPreviewViewMode.PageWidth, view.ViewMode);
            Assert.InRange(view.Zoom, view.MinZoom, view.MaxZoom);
            var scaledPageWidth = snapshot.PageSize.Width * view.Zoom / 100;
            Assert.InRange(view.Viewer.ViewportWidth - scaledPageWidth, 47, 49);
            var originalZoom = view.Zoom;
            var stateChanges = 0;
            view.StateChanged += (_, _) => stateChanges++;
            host.Width = 360;
            view.Width = 360;
            host.UpdateLayout();
            PumpDispatcher(view.Dispatcher);
            Assert.True(view.Zoom < originalZoom);
            Assert.True(view.Zoom < 80);
            Assert.True(stateChanges > 0);
            scaledPageWidth = snapshot.PageSize.Width * view.Zoom / 100;
            Assert.InRange(view.Viewer.ViewportWidth - scaledPageWidth, 47, 49);
            host.Width = 2200;
            view.Width = 2200;
            host.UpdateLayout();
            PumpDispatcher(view.Dispatcher);
            Assert.True(view.Zoom > 200);
            scaledPageWidth = snapshot.PageSize.Width * view.Zoom / 100;
            Assert.InRange(view.Viewer.ViewportWidth - scaledPageWidth, 47, 49);
            Assert.Throws<ArgumentOutOfRangeException>(() => view.GoToPage(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => view.GoToPage(view.PageCount + 1));
            view.GoToPage(view.PageCount);
            PumpDispatcher(view.Dispatcher);
            Assert.Equal(view.PageCount, view.CurrentPageNumber);
            Assert.False(view.GoToNextPage());
            host.Close();
        });
    }

    [Fact]
    public void ReplacingSnapshotDoesNotRetainOldDocument()
    {
        StaTestHelper.Run(() =>
        {
            var service = new WriterPreviewCloneService();
            var first = service.CreateSnapshot(new FlowDocument(new Paragraph(new Run("first"))),
                DocumentPageSettings.A4());
            var second = service.CreateSnapshot(new FlowDocument(new Paragraph(new Run("second"))),
                DocumentPageSettings.Letter());
            var view = new WriterDocumentPreviewView();
            var host = new Window { Content = view, Width = 640, Height = 480, ShowInTaskbar = false };
            host.Show();
            try
            {
                view.SetSnapshot(first);
                view.SetSnapshot(second);
                host.UpdateLayout();
                PumpDispatcher(view.Dispatcher);

                Assert.Same(second.Document, view.Snapshot!.Document);
                Assert.Same(second.Paginator, view.PrimaryPageView.DocumentPaginator);
                Assert.Equal(1, view.CurrentPageNumber);
            }
            finally
            {
                host.Close();
            }
        });
    }

    [Fact]
    public void PageFourRemainsStableWhenSerializedAfterPreviewNavigation()
    {
        StaTestHelper.Run(() =>
        {
            var document = new FlowDocument();
            for (var index = 1; index <= 100; index++)
            {
                document.Blocks.Add(new Paragraph(new Run(
                    $"Paragraph {index}: stable preview and print pagination proof.")));
            }
            using var snapshot = new WriterPreviewCloneService().CreateSnapshot(document,
                DocumentPageSettings.A4());
            Assert.True(snapshot.Paginator.PageCount >= 4);
            var view = new WriterDocumentPreviewView();
            var host = new Window { Content = view, Width = 900, Height = 720, ShowInTaskbar = false };
            var xpsPath = Path.Combine(Path.GetTempPath(),
                $"RibbonKit.Writer.W2D.{Guid.NewGuid():N}.xps");
            try
            {
                host.Show();
                view.SetSnapshot(snapshot);
                view.GoToPage(4);
                host.UpdateLayout();
                PumpDispatcher(view.Dispatcher);
                var expectedPage = snapshot.Paginator.GetPage(3);
                var expectedSignature = GetGlyphSignature(expectedPage.Visual);
                Assert.NotEmpty(expectedSignature);

                using (var output = new XpsDocument(xpsPath, FileAccess.ReadWrite))
                    XpsDocument.CreateXpsDocumentWriter(output).Write(snapshot.Paginator);
                using (var output = new XpsDocument(xpsPath, FileAccess.Read))
                {
                    var outputPaginator = output.GetFixedDocumentSequence().DocumentPaginator;
                    outputPaginator.ComputePageCount();
                    var actualPage = outputPaginator.GetPage(3);
                    Assert.Equal(expectedPage.Size, actualPage.Size);
                    Assert.Equal(expectedSignature, GetGlyphSignature(actualPage.Visual));
                }
            }
            finally
            {
                view.SetSnapshot(null);
                host.Close();
                if (File.Exists(xpsPath))
                    File.Delete(xpsPath);
            }
        });
    }

    private static void PumpDispatcher(Dispatcher dispatcher)
    {
        var frame = new DispatcherFrame();
        dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static int CountDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        var count = root is T ? 1 : 0;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            count += CountDescendants<T>(VisualTreeHelper.GetChild(root, index));
        return count;
    }

    private static IReadOnlyList<string> GetGlyphSignature(DependencyObject root)
    {
        var result = new List<string>();
        CollectGlyphSignature(root, result);
        return result;
    }

    private static void CollectGlyphSignature(DependencyObject root, List<string> result)
    {
        if (root is Glyphs glyphs)
        {
            result.Add(FormattableString.Invariant(
                $"{glyphs.OriginX:R}|{glyphs.OriginY:R}|{glyphs.FontRenderingEmSize:R}|{glyphs.UnicodeString}|{glyphs.Indices}"));
        }
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            CollectGlyphSignature(VisualTreeHelper.GetChild(root, index), result);
    }
}
