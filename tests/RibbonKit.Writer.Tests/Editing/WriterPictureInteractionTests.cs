using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Services.Persistence;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

[Collection("Writer UI")]
public sealed class WriterPictureInteractionTests
{
    [Theory]
    [InlineData(WriterPictureResizeHandle.TopLeft, -20, -10, 120, 60)]
    [InlineData(WriterPictureResizeHandle.TopRight, 20, -10, 120, 60)]
    [InlineData(WriterPictureResizeHandle.BottomRight, 20, 10, 120, 60)]
    [InlineData(WriterPictureResizeHandle.BottomLeft, -20, 10, 120, 60)]
    public void CornerHandlesPreserveAspectRatio(WriterPictureResizeHandle handle,
        double deltaX, double deltaY, double expectedWidth, double expectedHeight)
    {
        var resized = WriterPictureResizeGeometry.Resize(new Size(100, 50),
            new Vector(deltaX, deltaY), handle, new Size(500, 500));

        Assert.Equal(expectedWidth, resized.Width, 6);
        Assert.Equal(expectedHeight, resized.Height, 6);
        Assert.Equal(2d, resized.Width / resized.Height, 6);
    }

    [Theory]
    [InlineData(WriterPictureResizeHandle.Top, 20, -10, 100, 60)]
    [InlineData(WriterPictureResizeHandle.Right, 20, -10, 120, 50)]
    [InlineData(WriterPictureResizeHandle.Bottom, 20, 10, 100, 60)]
    [InlineData(WriterPictureResizeHandle.Left, -20, 10, 120, 50)]
    public void EdgeHandlesChangeOnlyOneAxis(WriterPictureResizeHandle handle,
        double deltaX, double deltaY, double expectedWidth, double expectedHeight)
    {
        var resized = WriterPictureResizeGeometry.Resize(new Size(100, 50),
            new Vector(deltaX, deltaY), handle, new Size(500, 500));

        Assert.Equal(expectedWidth, resized.Width, 6);
        Assert.Equal(expectedHeight, resized.Height, 6);
    }

    [Fact]
    public void ResizeHandlesExposeDirectionAppropriateMouseCursors()
    {
        Assert.Same(Cursors.SizeNWSE,
            WriterPictureResizeAdorner.GetCursor(WriterPictureResizeHandle.TopLeft));
        Assert.Same(Cursors.SizeNWSE,
            WriterPictureResizeAdorner.GetCursor(WriterPictureResizeHandle.BottomRight));
        Assert.Same(Cursors.SizeNESW,
            WriterPictureResizeAdorner.GetCursor(WriterPictureResizeHandle.TopRight));
        Assert.Same(Cursors.SizeNESW,
            WriterPictureResizeAdorner.GetCursor(WriterPictureResizeHandle.BottomLeft));
        Assert.Same(Cursors.SizeWE,
            WriterPictureResizeAdorner.GetCursor(WriterPictureResizeHandle.Left));
        Assert.Same(Cursors.SizeWE,
            WriterPictureResizeAdorner.GetCursor(WriterPictureResizeHandle.Right));
        Assert.Same(Cursors.SizeNS,
            WriterPictureResizeAdorner.GetCursor(WriterPictureResizeHandle.Top));
        Assert.Same(Cursors.SizeNS,
            WriterPictureResizeAdorner.GetCursor(WriterPictureResizeHandle.Bottom));
    }

    [Theory]
    [InlineData(WriterPictureResizeHandle.TopLeft, 6, 6)]
    [InlineData(WriterPictureResizeHandle.Top, 0, 6)]
    [InlineData(WriterPictureResizeHandle.TopRight, -6, 6)]
    [InlineData(WriterPictureResizeHandle.Right, -6, 0)]
    [InlineData(WriterPictureResizeHandle.BottomRight, -6, -6)]
    [InlineData(WriterPictureResizeHandle.Bottom, 0, -6)]
    [InlineData(WriterPictureResizeHandle.BottomLeft, 6, -6)]
    [InlineData(WriterPictureResizeHandle.Left, 6, 0)]
    public void ResizeHandlesUseLargerInvisibleHitTargets(
        WriterPictureResizeHandle expected, double offsetX, double offsetY)
    {
        var size = new Size(200, 100);
        var dpi = new DpiScale(1.5, 1.5);
        var visible = WriterPictureResizeGeometry.GetHandleRects(size, dpi,
            WriterPictureResizeAdorner.VisibleHandleSize)[expected];
        var point = new Point(visible.X + visible.Width / 2d + offsetX,
            visible.Y + visible.Height / 2d + offsetY);

        Assert.False(visible.Contains(point));
        Assert.True(WriterPictureResizeAdorner.TryGetHandle(point, size, dpi,
            out var actual));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ResizeGeometryEnforcesFinitePositiveMinimumAndPageBounds()
    {
        var minimum = WriterPictureResizeGeometry.Resize(new Size(100, 50),
            new Vector(-1000, -1000), WriterPictureResizeHandle.BottomRight,
            new Size(300, 200));
        var maximum = WriterPictureResizeGeometry.Resize(new Size(100, 50),
            new Vector(1000, 1000), WriterPictureResizeHandle.BottomRight,
            new Size(300, 120));
        var edgeMinimum = WriterPictureResizeGeometry.Resize(new Size(100, 50),
            new Vector(1000, 0), WriterPictureResizeHandle.Left,
            new Size(300, 200));

        Assert.True(minimum.Width >= WriterPictureResizeGeometry.MinimumDimension);
        Assert.True(minimum.Height >= WriterPictureResizeGeometry.MinimumDimension);
        Assert.Equal(new Size(240, 120), maximum);
        Assert.Equal(WriterPictureResizeGeometry.MinimumDimension, edgeMinimum.Width, 6);
        Assert.Equal(50, edgeMinimum.Height, 6);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WriterPictureResizeGeometry.Resize(new Size(double.NaN, 10), new Vector(),
                WriterPictureResizeHandle.Right, new Size(100, 100)));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(1.75)]
    [InlineData(2.0)]
    public void HandleGeometryIsCompleteAndPixelAlignedAtRepresentativeDpi(double scale)
    {
        var dpi = new DpiScale(scale, scale);
        var handles = WriterPictureResizeGeometry.GetHandleRects(new Size(123.4, 67.8), dpi);

        Assert.Equal(Enum.GetValues<WriterPictureResizeHandle>(), handles.Keys);
        Assert.All(handles.Values, rect =>
        {
            Assert.True(rect.Width > 0);
            Assert.True(rect.Height > 0);
            Assert.Equal(Math.Round(rect.X * scale), rect.X * scale, 6);
            Assert.Equal(Math.Round(rect.Y * scale), rect.Y * scale, 6);
        });
    }

    [Fact]
    public void SelectionIsDocumentBoundStableAcrossFocusAndClearsOutsideOrWhenStale()
    {
        StaTestHelper.Run(() =>
        {
            var (document, paragraph, container, _) = CreatePictureDocument(100, 50);
            var editor = new RichTextBox { Document = document };
            var other = new Button();
            using var controller = new WriterPictureInteractionController(editor,
                new WriterImageService());

            Assert.True(controller.SelectPicture(container));
            Assert.True(controller.HasSelection);
            other.Focus();
            Assert.True(controller.HasSelection);

            editor.Selection.Select(paragraph.ContentStart, paragraph.ContentStart);
            Assert.False(controller.HasSelection);
            Assert.True(controller.SelectPicture(container));
            paragraph.Inlines.Remove(container);
            controller.Refresh();
            Assert.False(controller.HasSelection);

            var replacement = new FlowDocument(new Paragraph(new Run("replacement")));
            controller.ReplaceDocument(replacement);
            editor.Document = replacement;
            Assert.False(controller.SelectPicture(container));
        });
    }

    [Fact]
    public void PictureHitTestingAndAdornerFollowPaperContinuousZoomScrollAndRtlLayout()
    {
        StaTestHelper.Run(() =>
        {
            var (document, _, container, image) = CreatePictureDocument(100, 50);
            document.FlowDirection = FlowDirection.RightToLeft;
            var editor = new RichTextBox
            {
                Document = document,
                FlowDirection = FlowDirection.RightToLeft,
                LayoutTransform = new ScaleTransform(1.5, 1.5)
            };
            var paper = new Border { Child = editor };
            var viewport = new ScrollViewer { Content = paper };
            var surface = new WriterEditorSurface
            {
                Width = 520,
                Height = 360,
                FlowDirection = FlowDirection.RightToLeft
            };
            surface.Children.Add(viewport);
            surface.Attach(editor, viewport, paper);
            surface.PageSettings = DocumentPageSettings.Letter();
            surface.ZoomPercent = 150;
            surface.ViewMode = WriterEditorViewMode.Paper;
            using var window = new TestWindow(new AdornerDecorator { Child = surface });
            window.Show();
            window.UpdateLayout();
            using var controller = new WriterPictureInteractionController(editor,
                new WriterImageService());

            var imageOrigin = image.TranslatePoint(new Point(0, 0), editor);
            Assert.True(controller.TrySelectAtPoint(new Point(
                imageOrigin.X + image.ActualWidth / 2d,
                imageOrigin.Y + image.ActualHeight / 2d)));
            window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
            window.UpdateLayout();
            var layer = AdornerLayer.GetAdornerLayer(image);
            var paperAdorner = Assert.IsType<WriterPictureResizeAdorner>(
                Assert.Single(layer!.GetAdorners(image)!));
            Assert.Equal(image.ActualWidth, paperAdorner.ActualWidth, 2);
            Assert.Equal(image.ActualHeight, paperAdorner.ActualHeight, 2);

            viewport.ScrollToHorizontalOffset(viewport.ScrollableWidth);
            viewport.ScrollToVerticalOffset(viewport.ScrollableHeight);
            surface.ViewMode = WriterEditorViewMode.Continuous;
            window.UpdateLayout();
            Assert.True(controller.HasSelection);
            Assert.Same(paperAdorner, Assert.Single(layer.GetAdorners(image)!));
            Assert.Equal(FlowDirection.RightToLeft, editor.FlowDirection);

            surface.ViewMode = WriterEditorViewMode.Paper;
            window.UpdateLayout();
            Assert.True(controller.HasSelection);
            Assert.Same(paperAdorner, Assert.Single(layer.GetAdorners(image)!));
        });
    }

    [Fact]
    public void RibbonSizeCommitIsOneUndoRedoUnitAndPreservesOlderHistory()
    {
        StaTestHelper.Run(() =>
        {
            var (document, _, container, _) = CreatePictureDocument(100, 50);
            var editor = new RichTextBox { Document = document, IsUndoEnabled = true };
            using var window = new TestWindow(new AdornerDecorator { Child = editor });
            window.Show();
            window.UpdateLayout();
            var imageService = new WriterImageService();
            imageService.ResetUndoHistory(document);
            using var controller = new WriterPictureInteractionController(editor, imageService);
            editor.Selection.Select(document.ContentEnd, document.ContentEnd);
            editor.BeginChange();
            try
            {
                editor.Selection.Text = "older";
            }
            finally
            {
                editor.EndChange();
            }
            Assert.True(editor.CanUndo);
            Assert.True(controller.SelectPicture(container));

            Assert.True(controller.TrySetSize(180, 90));
            var committed = GetOnlyImage(document);
            Assert.Equal(180, committed.Width, 6);
            Assert.Equal(90, committed.Height, 6);

            editor.Undo();
            imageService.TryRestoreAfterUndo(editor);
            var opening = GetOnlyImage(document);
            Assert.Equal(100, opening.Width, 6);
            Assert.Equal(50, opening.Height, 6);
            Assert.True(editor.CanUndo);

            editor.Redo();
            imageService.NotifyRedo(editor);
            var redone = GetOnlyImage(document);
            Assert.Equal(180, redone.Width, 6);
            Assert.Equal(90, redone.Height, 6);

            editor.Undo();
            imageService.TryRestoreAfterUndo(editor);
            editor.Undo();
            Assert.DoesNotContain("older", new TextRange(document.ContentStart,
                document.ContentEnd).Text);
        });
    }

    [Fact]
    public void InteractionAdornerIsVisualOnlyAndHasNoAutomationPeerOrSerializedState()
    {
        StaTestHelper.Run(() =>
        {
            var (document, _, container, image) = CreatePictureDocument(100, 50);
            var editor = new RichTextBox { Document = document };
            var decorator = new AdornerDecorator { Child = editor };
            using var window = new TestWindow(decorator);
            window.Show();
            window.UpdateLayout();
            using var controller = new WriterPictureInteractionController(editor,
                new WriterImageService());
            Assert.True(controller.SelectPicture(container));
            window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));

            var layer = AdornerLayer.GetAdornerLayer(image);
            var adorner = Assert.Single(layer!.GetAdorners(image)!);
            Assert.IsType<WriterPictureResizeAdorner>(adorner);
            Assert.Null(UIElementAutomationPeer.CreatePeerForElement(adorner));
            var serialized = System.Windows.Markup.XamlWriter.Save(document);
            Assert.DoesNotContain(nameof(WriterPictureResizeAdorner), serialized,
                StringComparison.Ordinal);

            controller.ClearSelection();
            Assert.Null(layer.GetAdorners(image));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EscapeAndCaptureLossRestoreOpeningGeometryWithoutUndoOrDocumentChange(
        bool simulateCaptureLoss)
    {
        StaTestHelper.Run(() =>
        {
            var (document, _, container, image) = CreatePictureDocument(100, 50);
            var editor = new RichTextBox { Document = document, IsUndoEnabled = true };
            var decorator = new AdornerDecorator { Child = editor };
            using var window = new TestWindow(decorator);
            window.Show();
            window.UpdateLayout();
            var textChanges = 0;
            editor.TextChanged += (_, _) => textChanges++;
            using var controller = new WriterPictureInteractionController(editor,
                new WriterImageService());
            Assert.True(controller.SelectPicture(container));
            window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
            var layer = AdornerLayer.GetAdornerLayer(image);
            var adorner = Assert.IsType<WriterPictureResizeAdorner>(
                Assert.Single(layer!.GetAdorners(image)!));

            adorner.BeginDragForTesting(WriterPictureResizeHandle.Right, new Point(0, 0));
            adorner.UpdateDragForTesting(new Point(40, 0));
            Assert.Equal(140, image.Width, 6);
            Assert.Equal(50, image.Height, 6);
            Assert.False(editor.CanUndo);
            Assert.Equal(0, textChanges);

            if (simulateCaptureLoss)
                adorner.SimulateCaptureLossForTesting();
            else
                adorner.CancelDrag();

            Assert.Equal(100, image.Width, 6);
            Assert.Equal(50, image.Height, 6);
            Assert.False(editor.CanUndo);
            Assert.Equal(0, textChanges);
        });
    }

    [Fact]
    public void PointerReleaseCommitsExactlyOneBoundedUndoUnit()
    {
        StaTestHelper.Run(() =>
        {
            var (document, _, container, image) = CreatePictureDocument(100, 50);
            var editor = new RichTextBox { Document = document, IsUndoEnabled = true };
            var decorator = new AdornerDecorator { Child = editor };
            using var window = new TestWindow(decorator);
            window.Show();
            window.UpdateLayout();
            var imageService = new WriterImageService();
            imageService.ResetUndoHistory(document);
            using var controller = new WriterPictureInteractionController(editor, imageService);
            Assert.True(controller.SelectPicture(container));
            window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
            var layer = AdornerLayer.GetAdornerLayer(image);
            var adorner = Assert.IsType<WriterPictureResizeAdorner>(
                Assert.Single(layer!.GetAdorners(image)!));

            adorner.BeginDragForTesting(WriterPictureResizeHandle.BottomRight,
                new Point(0, 0));
            adorner.UpdateDragForTesting(new Point(40, 20));
            Assert.False(editor.CanUndo);
            adorner.CompleteDragForTesting();

            var committed = GetOnlyImage(document);
            Assert.Equal(140, committed.Width, 6);
            Assert.Equal(70, committed.Height, 6);
            Assert.True(editor.CanUndo);
            editor.Undo();
            imageService.TryRestoreAfterUndo(editor);
            var opening = GetOnlyImage(document);
            Assert.Equal(100, opening.Width, 6);
            Assert.Equal(50, opening.Height, 6);
            Assert.False(editor.CanUndo);
        });
    }

    [Fact]
    public void ResizeAndRemovalKeepTheirUndoRedoUnitsInOrder()
    {
        StaTestHelper.Run(() =>
        {
            var (document, _, container, _) = CreatePictureDocument(100, 50);
            var editor = new RichTextBox { Document = document, IsUndoEnabled = true };
            using var window = new TestWindow(new AdornerDecorator { Child = editor });
            window.Show();
            window.UpdateLayout();
            var imageService = new WriterImageService();
            imageService.ResetUndoHistory(document);
            using var controller = new WriterPictureInteractionController(editor, imageService);
            Assert.True(controller.SelectPicture(container));
            Assert.True(controller.TrySetSize(180, 90));
            Assert.True(controller.TryRemoveSelectedPicture());
            Assert.Empty(WriterInlineInsertion.EnumerateImages(document));

            editor.Undo();
            imageService.TryRestoreAfterUndo(editor);
            var restoredRemoval = GetOnlyImage(document);
            Assert.Equal(180, restoredRemoval.Width, 6);
            Assert.Equal(90, restoredRemoval.Height, 6);

            editor.Undo();
            imageService.TryRestoreAfterUndo(editor);
            var restoredResize = GetOnlyImage(document);
            Assert.Equal(100, restoredResize.Width, 6);
            Assert.Equal(50, restoredResize.Height, 6);

            editor.Redo();
            imageService.NotifyRedo(editor);
            var redoneResize = GetOnlyImage(document);
            Assert.Equal(180, redoneResize.Width, 6);
            Assert.Equal(90, redoneResize.Height, 6);
            editor.Redo();
            imageService.NotifyRedo(editor);
            Assert.Empty(WriterInlineInsertion.EnumerateImages(document));
        });
    }

    [Fact]
    public void BoundedResizeDimensionsSurviveDocumentCloneUsedByPreviewPrintAndPersistence()
    {
        StaTestHelper.Run(() =>
        {
            var (document, _, container, _) = CreatePictureDocument(100, 50);
            var editor = new RichTextBox { Document = document };
            var imageService = new WriterImageService();
            imageService.ResetUndoHistory(document);
            using var controller = new WriterPictureInteractionController(editor, imageService);
            Assert.True(controller.SelectPicture(container));
            Assert.True(controller.TrySetSize(144, 72));

            using var snapshot = new WriterPreviewCloneService().CreateSnapshot(document,
                DocumentPageSettings.Letter());
            var clonedImage = GetOnlyImage(snapshot.SourceClone);
            Assert.Equal(144, clonedImage.Width, 6);
            Assert.Equal(72, clonedImage.Height, 6);
            var xaml = System.Windows.Markup.XamlWriter.Save(document);
            Assert.DoesNotContain("Adorner", xaml, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task CommittedDimensionsSurviveNativeSaveCloseReopen()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var (document, _, container, _) = CreatePictureDocument(100, 50);
            var editor = new RichTextBox { Document = document };
            var imageService = new WriterImageService();
            imageService.ResetUndoHistory(document);
            using var controller = new WriterPictureInteractionController(editor, imageService);
            Assert.True(controller.SelectPicture(container));
            Assert.True(controller.TrySetSize(222, 111));

            var path = Path.Combine(Path.GetTempPath(), $"writer-picture-{Guid.NewGuid():N}.rkw");
            try
            {
                var persistence = new WriterDocumentPersistence();
                var writerDocument = new WriterDocument(document, format:
                    WriterDocumentFormat.RibbonKitWriter);
                Assert.True(await persistence.SaveAsync(writerDocument, path,
                    WriterDocumentFormat.RibbonKitWriter, default));
                var reopened = await persistence.LoadAsync(path,
                    WriterDocumentFormat.RibbonKitWriter, default);
                Assert.NotNull(reopened);
                var reopenedImage = GetOnlyImage(reopened.Content);
                Assert.Equal(222, reopenedImage.Width, 6);
                Assert.Equal(111, reopenedImage.Height, 6);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        });
    }

    private static (FlowDocument Document, Paragraph Paragraph,
        InlineUIContainer Container, Image Image) CreatePictureDocument(double width, double height)
    {
        var bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null,
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 4);
        bitmap.Freeze();
        var image = new Image
        {
            Source = bitmap,
            Width = width,
            Height = height,
            Stretch = Stretch.Uniform,
            IsHitTestVisible = false
        };
        var container = new InlineUIContainer(image)
        {
            BaselineAlignment = BaselineAlignment.Center
        };
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(container);
        paragraph.Inlines.Add(new Run(" tail"));
        return (new FlowDocument(paragraph)
        {
            PageWidth = 816,
            PageHeight = 1056,
            PagePadding = new Thickness(96)
        }, paragraph, container, image);
    }

    private static Image GetOnlyImage(FlowDocument document)
    {
        var container = Assert.Single(WriterInlineInsertion.EnumerateImages(document));
        Assert.True(WriterInlineInsertion.TryGetImage(container, out var image));
        return image;
    }

    private sealed class TestWindow : Window, IDisposable
    {
        internal TestWindow(UIElement content) => Content = content;

        public void Dispose() => Close();
    }
}
