using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Pagination;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Tests.Document;
using RibbonKit.Writer.Tests.Preview;
using Xunit;

namespace RibbonKit.Writer.Tests.Pagination;

[Collection(WriterPreviewTestCollection.Name)]
public sealed class WriterPaginatedDiagnosticProductionTests
{
    [Fact]
    public async Task DedicatedProductionResultMatchesAcceptedPaginatorAndVirtualizesRealPages()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = ProductionWorkspace.Create(CreateStructuredDocument(
                DocumentPageSettings.Letter()));
            await WaitForCurrentAsync(workspace.Controller);
            var result = Assert.IsType<WriterPaginationLayoutResult>(
                workspace.Controller.Current);

            Assert.Equal(ApartmentState.STA, result.WorkerApartment);
            Assert.NotEqual(Environment.CurrentManagedThreadId, result.WorkerThreadId);
            Assert.Equal(result.Generation, workspace.Controller.PublishedGeneration);
            Assert.Equal(result.Generation, workspace.Controller.RequestedGeneration);
            Assert.InRange(result.MappedPages.Length, 1, 3);
            Assert.Equal(result.MappedPages.ToArray(),
                result.Pages.Select(page => page.PageNumber).ToArray());
            Assert.Equal(result.MappedPages.ToArray(),
                workspace.Surface.RenderedPages.Order().ToArray());
            Assert.All(result.Pages, page => Assert.True(
                page.PngBytes.Length > 8 &&
                page.PngBytes[0] == 0x89 && page.PngBytes[1] == 0x50));
            Assert.NotEmpty(result.Insertions);
            using var accepted = new WriterPreviewCloneService().CreateSnapshot(
                workspace.Editor.Document, workspace.Settings);
            var paginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(
                accepted.PrintPaginator);
            Assert.Equal(paginator.PageCount, result.PageCount);
            Assert.Equal(GetPageStartOffsets(accepted.SourceClone, paginator).ToArray(),
                result.PageStartOffsets.ToArray());
            Assert.True(workspace.Controller.LastCaptureMilliseconds < 250,
                $"Immutable capture took {workspace.Controller.LastCaptureMilliseconds:0.###} ms.");

            var clonePicture = accepted.SourceClone.Blocks.OfType<Paragraph>()
                .SelectMany(paragraph => paragraph.Inlines.OfType<InlineUIContainer>())
                .Single();
            var picturePage = paginator.GetPageNumber(clonePicture.ContentStart);
            workspace.Surface.RequestPageForTesting(picturePage);
            await WaitForCurrentAsync(workspace.Controller);
            var pictureGeometry = Assert.Single(
                workspace.Controller.Current!.StructuredObjects,
                item => item.Kind == WriterPaginationObjectKind.Picture &&
                    item.PageNumber == picturePage);
            Assert.InRange(pictureGeometry.Rectangle.Width, 80, 190);
            Assert.InRange(pictureGeometry.Rectangle.Height, 80, 190);
            Assert.InRange(pictureGeometry.Rectangle.Width /
                pictureGeometry.Rectangle.Height, 0.75, 1.75);
            Assert.InRange(pictureGeometry.Rectangle.X, 0,
                workspace.Settings.WidthDip - pictureGeometry.Rectangle.Width);
            Assert.InRange(pictureGeometry.Rectangle.Y, 0,
                workspace.Settings.HeightDip - pictureGeometry.Rectangle.Height);
            TextElement? activated = null;
            workspace.Controller.StructuredObjectActivator = element =>
            {
                activated = element;
                return true;
            };
            var pictureInteraction = workspace.Surface.CaptureObjectInteractionForTesting(
                WriterPaginationObjectKind.Picture);
            workspace.Surface.ApplyInteractionForTesting(pictureInteraction);
            Assert.IsType<InlineUIContainer>(activated);
            Assert.True(workspace.Editor.IsKeyboardFocusWithin);
        }, TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task CrossPageInteractionKeepsNativeSelectionFocusAndUndoAcrossReflow()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = ProductionWorkspace.Create(CreateParagraphDocument(
                DocumentPageSettings.Letter(), 180));
            await WaitForCurrentAsync(workspace.Controller);
            var opening = Assert.IsType<WriterPaginationLayoutResult>(
                workspace.Controller.Current);
            Assert.True(opening.PageCount > 1);
            var pageZero = opening.Insertions.Where(item => item.PageNumber == 0).ToArray();
            var pageOne = opening.Insertions.Where(item => item.PageNumber == 1).ToArray();
            var expectedStart = pageZero[^5].SourceOffset;
            var expectedEnd = pageOne[Math.Min(12, pageOne.Length - 1)].SourceOffset;
            var anchor = workspace.Surface.CaptureInteractionForTesting(0, expectedStart);
            var moving = workspace.Surface.CaptureInteractionForTesting(1, expectedEnd);

            workspace.Surface.ApplyInteractionForTesting(anchor, moving);
            Assert.Equal((expectedStart, expectedEnd), SelectionOffsets(workspace.Editor));
            Assert.True(workspace.Editor.IsKeyboardFocusWithin);
            var authoritative = workspace.Editor.Document;
            var before = DocumentText(authoritative);
            workspace.Editor.Selection.Text = "native cross-page replacement";
            await WaitForCurrentAsync(workspace.Controller);
            var after = DocumentText(authoritative);
            Assert.NotEqual(before, after);
            Assert.Same(authoritative, workspace.Editor.Document);
            Assert.True(workspace.Editor.CanUndo);

            workspace.Editor.Undo();
            await WaitForCurrentAsync(workspace.Controller);
            Assert.Equal(before, DocumentText(authoritative));
            Assert.True(workspace.Editor.CanRedo);
            workspace.Editor.Redo();
            await WaitForCurrentAsync(workspace.Controller);
            Assert.Equal(after, DocumentText(authoritative));

            var selectionBeforeReflow = SelectionOffsets(workspace.Editor);
            var reflow = DocumentPageSettings.A4(DocumentPageOrientation.Landscape,
                new DocumentPageMargins(48, 72, 60, 84));
            workspace.Controller.SetPageSettings(reflow);
            await WaitForCurrentAsync(workspace.Controller);
            var current = Assert.IsType<WriterPaginationLayoutResult>(
                workspace.Controller.Current);
            Assert.Equal(selectionBeforeReflow, SelectionOffsets(workspace.Editor));
            Assert.Same(authoritative, workspace.Editor.Document);
            using var accepted = new WriterPreviewCloneService().CreateSnapshot(
                authoritative, reflow);
            var paginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(
                accepted.PrintPaginator);
            Assert.Equal(GetPageStartOffsets(accepted.SourceClone, paginator).ToArray(),
                current.PageStartOffsets.ToArray());
        }, TimeSpan.FromSeconds(45));
    }

    [Fact]
    public async Task PageWindowAndDocumentReplacementRejectOldEventsImmediately()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = ProductionWorkspace.Create(CreateParagraphDocument(
                DocumentPageSettings.Letter(), 180));
            await WaitForCurrentAsync(workspace.Controller);
            var opening = Assert.IsType<WriterPaginationLayoutResult>(
                workspace.Controller.Current);
            var oldEntry = opening.Insertions.First(item => item.PageNumber == 0);
            var oldInteraction = workspace.Surface.CaptureInteractionForTesting(
                0, oldEntry.SourceOffset);

            var lastPage = opening.PageCount - 1;
            workspace.Surface.RequestPageForTesting(lastPage);
            var caretBeforeStalePage = SelectionOffsets(workspace.Editor);
            workspace.Surface.ApplyInteractionForTesting(oldInteraction);
            Assert.Equal(caretBeforeStalePage, SelectionOffsets(workspace.Editor));
            await WaitForCurrentAsync(workspace.Controller);
            Assert.Equal(lastPage, workspace.Controller.Current!.VisiblePage);
            Assert.DoesNotContain(0, workspace.Surface.RenderedPages);

            var replacement = CreateDocument(workspace.Settings);
            workspace.Editor.Document = replacement;
            workspace.Controller.ReplaceDocument(replacement);
            var caretBeforeStaleDocument = SelectionOffsets(workspace.Editor);
            workspace.Surface.ApplyInteractionForTesting(oldInteraction);
            Assert.Equal(caretBeforeStaleDocument, SelectionOffsets(workspace.Editor));
            await WaitForCurrentAsync(workspace.Controller);
            Assert.Same(replacement, workspace.Editor.Document);
            Assert.NotEqual(opening.DocumentIdentity,
                workspace.Controller.Current!.DocumentIdentity);
            Assert.Equal(1, workspace.Controller.Current.PageCount);
            Assert.Empty(workspace.Controller.Current.Insertions);
            workspace.Surface.RefreshOverlays(workspace.Editor);
        }, TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task DiagnosticChromeProjectsRulerAndMarginGuidesAcrossZoomAndReflow()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = ProductionWorkspace.Create(CreateParagraphDocument(
                DocumentPageSettings.Letter(), 180));
            await WaitForCurrentAsync(workspace.Controller);
            workspace.Controller.SetChrome(showRuler: true, showMarginGuides: true,
                new WriterRulerIndentation(18, 12, 18, 24));
            workspace.Window.UpdateLayout();

            Assert.True(workspace.Surface.RulerElementCount > 10);
            Assert.Equal(workspace.Surface.RenderedPages.Count,
                workspace.Surface.MarginGuideCount);

            workspace.Controller.SetZoom(140);
            var reflow = DocumentPageSettings.A4(DocumentPageOrientation.Landscape,
                new DocumentPageMargins(48, 60, 72, 84));
            workspace.Controller.SetPageSettings(reflow);
            workspace.Controller.SetChrome(showRuler: true, showMarginGuides: true,
                WriterRulerIndentation.Empty);
            await WaitForCurrentAsync(workspace.Controller);
            workspace.Window.UpdateLayout();
            Assert.Equal(140, workspace.Surface.ZoomPercent);
            Assert.True(workspace.Surface.RulerElementCount > 10);
            Assert.Equal(workspace.Surface.RenderedPages.Count,
                workspace.Surface.MarginGuideCount);

            workspace.Controller.SetChrome(showRuler: false, showMarginGuides: false,
                WriterRulerIndentation.Empty);
            Assert.Equal(0, workspace.Surface.RulerElementCount);
            Assert.Equal(0, workspace.Surface.MarginGuideCount);
        }, TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task PictureResizeHandleDelegatesOneCommitToAuthoritativeW3EController()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = ProductionWorkspace.Create(CreateStructuredDocument(
                DocumentPageSettings.Letter()));
            using var pictureController = new WriterPictureInteractionController(
                workspace.Editor, new WriterImageService());
            workspace.Controller.StructuredObjectActivator = element =>
                element is InlineUIContainer picture && pictureController.SelectPicture(picture);
            workspace.Controller.StructuredResizeStarter = (element, handle) =>
                element is InlineUIContainer picture &&
                handle == WriterPaginationResizeHandleKind.PictureBottomRight &&
                pictureController.SelectPicture(picture) &&
                pictureController.BeginExternalResize(WriterPictureResizeHandle.BottomRight);
            workspace.Controller.StructuredResizeUpdater = (_, x, y) =>
                pictureController.UpdateExternalResize(new Vector(x, y));
            workspace.Controller.StructuredResizeCommitter =
                pictureController.CompleteExternalResize;
            workspace.Controller.StructuredResizeCanceler =
                pictureController.CancelExternalResize;

            await WaitForCurrentAsync(workspace.Controller);
            using var accepted = new WriterPreviewCloneService().CreateSnapshot(
                workspace.Editor.Document, workspace.Settings);
            var paginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(
                accepted.PrintPaginator);
            var clonePicture = accepted.SourceClone.Blocks.OfType<Paragraph>()
                .SelectMany(paragraph => paragraph.Inlines.OfType<InlineUIContainer>())
                .Single();
            workspace.Surface.RequestPageForTesting(
                paginator.GetPageNumber(clonePicture.ContentStart));
            await WaitForCurrentAsync(workspace.Controller);

            var interaction = workspace.Surface.CaptureObjectInteractionForTesting(
                WriterPaginationObjectKind.Picture);
            workspace.Surface.ApplyInteractionForTesting(interaction);
            Assert.Equal(8, workspace.Surface.ResizeHandleCount);
            var authoritative = workspace.Editor.Document;
            var opening = CurrentImage(workspace.Editor.Document);
            var openingWidth = opening.Width;
            var openingHeight = opening.Height;

            Assert.True(workspace.Surface.BeginResizeForTesting(
                WriterPaginationObjectKind.Picture,
                WriterPaginationResizeHandleKind.PictureBottomRight));
            Assert.True(workspace.Surface.UpdateResizeForTesting(30, 20));
            Assert.True(workspace.Surface.CompleteResizeForTesting());
            await WaitForCurrentAsync(workspace.Controller);

            var committed = CurrentImage(workspace.Editor.Document);
            Assert.True(committed.Width > openingWidth);
            Assert.True(committed.Height > openingHeight);
            Assert.True(workspace.Editor.CanUndo);
            Assert.Same(authoritative, workspace.Editor.Document);
        }, TimeSpan.FromSeconds(45));
    }

    [Fact]
    public async Task TableOverallHandleDelegatesToAuthoritativeW3EControllerAndCancelsOnReplacement()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = ProductionWorkspace.Create(CreateStructuredDocument(
                DocumentPageSettings.Letter()));
            using var tableInteraction = new WriterTableInteractionController(
                workspace.Editor, () => true);
            using var tableResize = new WriterTableResizeController(workspace.Editor,
                tableInteraction, () => { });
            bool SelectTable(TextElement element)
            {
                if (element is not Table table)
                    return false;
                var cells = tableInteraction.GetOrderedCells(table);
                if (cells.Count == 0)
                    return false;
                tableInteraction.MoveCaret(cells[0]);
                return ReferenceEquals(tableInteraction.CurrentTable, table);
            }
            workspace.Controller.StructuredObjectActivator = SelectTable;
            workspace.Controller.StructuredResizeStarter = (element, handle) =>
                handle == WriterPaginationResizeHandleKind.TableOverall &&
                SelectTable(element) && tableResize.BeginExternalOverallResize();
            workspace.Controller.StructuredResizeUpdater = (_, x, y) =>
                tableResize.UpdateExternalResize(new Vector(x, y));
            workspace.Controller.StructuredResizeCommitter = tableResize.CompleteExternalResize;
            workspace.Controller.StructuredResizeCanceler = tableResize.CancelExternalResize;

            await WaitForCurrentAsync(workspace.Controller);
            using var accepted = new WriterPreviewCloneService().CreateSnapshot(
                workspace.Editor.Document, workspace.Settings);
            var paginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(
                accepted.PrintPaginator);
            var cloneTable = accepted.SourceClone.Blocks.OfType<Table>().Single();
            workspace.Surface.RequestPageForTesting(
                paginator.GetPageNumber(cloneTable.ContentStart));
            await WaitForCurrentAsync(workspace.Controller);
            var interaction = workspace.Surface.CaptureObjectInteractionForTesting(
                WriterPaginationObjectKind.Table);
            workspace.Surface.ApplyInteractionForTesting(interaction);
            Assert.Equal(1, workspace.Surface.ResizeHandleCount);

            var table = workspace.Editor.Document.Blocks.OfType<Table>().Single();
            var openingWidth = table.Columns.Sum(column => column.Width.Value);
            Assert.True(workspace.Surface.BeginResizeForTesting(
                WriterPaginationObjectKind.Table,
                WriterPaginationResizeHandleKind.TableOverall));
            Assert.True(workspace.Surface.UpdateResizeForTesting(24, 12));
            Assert.True(workspace.Surface.CompleteResizeForTesting());
            await WaitForCurrentAsync(workspace.Controller);
            var committedTable = workspace.Editor.Document.Blocks.OfType<Table>().Single();
            Assert.True(committedTable.Columns.Sum(column => column.Width.Value) > openingWidth);
            Assert.True(workspace.Editor.CanUndo);

            Assert.True(workspace.Surface.BeginResizeForTesting(
                WriterPaginationObjectKind.Table,
                WriterPaginationResizeHandleKind.TableOverall));
            var replacement = CreateParagraphDocument(workspace.Settings, 20);
            workspace.Editor.Document = replacement;
            workspace.Controller.ReplaceDocument(replacement);
            Assert.False(tableResize.IsDragging);
            Assert.False(workspace.Surface.UpdateResizeForTesting(10, 10));
            await WaitForCurrentAsync(workspace.Controller);
            Assert.Same(replacement, workspace.Editor.Document);
            Assert.Equal(0, workspace.Surface.ResizeHandleCount);
        }, TimeSpan.FromSeconds(45));
    }

    private static async Task WaitForCurrentAsync(
        WriterPaginatedDiagnosticController controller)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            if (controller.Current is { } current &&
                current.Generation == controller.RequestedGeneration)
                return;
            await Dispatcher.Yield(DispatcherPriority.Background);
            await Task.Delay(20);
        }
        throw new TimeoutException(
            $"Generation {controller.RequestedGeneration} did not publish.");
    }

    private static ImmutableArray<int> GetPageStartOffsets(FlowDocument document,
        DynamicDocumentPaginator paginator)
    {
        var builder = ImmutableArray.CreateBuilder<int>(paginator.PageCount);
        for (var page = 0; page < paginator.PageCount; page++)
        {
            var position = Assert.IsType<TextPointer>(
                paginator.GetPagePosition(paginator.GetPage(page)));
            builder.Add(document.ContentStart.GetOffsetToPosition(position));
        }
        return builder.ToImmutable();
    }

    private static (int Start, int End) SelectionOffsets(RichTextBox editor) => (
        editor.Document.ContentStart.GetOffsetToPosition(editor.Selection.Start),
        editor.Document.ContentStart.GetOffsetToPosition(editor.Selection.End));

    private static string DocumentText(FlowDocument document) =>
        new TextRange(document.ContentStart, document.ContentEnd).Text;

    private static Image CurrentImage(FlowDocument document) => document.Blocks
        .OfType<Paragraph>()
        .SelectMany(paragraph => paragraph.Inlines.OfType<InlineUIContainer>())
        .Select(container => container.Child)
        .OfType<Image>()
        .Single();

    private static FlowDocument CreateParagraphDocument(DocumentPageSettings settings, int count)
    {
        var document = CreateDocument(settings);
        for (var index = 0; index < count; index++)
        {
            document.Blocks.Add(new Paragraph(new Run(
                $"Paragraph {index:D3}: production pagination diagnostic corpus."))
            {
                Margin = new Thickness(0, 0, 0, 6)
            });
        }
        return document;
    }

    private static FlowDocument CreateStructuredDocument(DocumentPageSettings settings)
    {
        var document = CreateParagraphDocument(settings, 92);
        var table = new Table { CellSpacing = 2 };
        table.Columns.Add(new TableColumn { Width = new GridLength(245) });
        table.Columns.Add(new TableColumn { Width = new GridLength(245) });
        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        for (var index = 0; index < 34; index++)
        {
            var row = new TableRow();
            row.Cells.Add(Cell($"Structured row {index:D2}, first column."));
            row.Cells.Add(Cell($"Structured row {index:D2}, second column."));
            group.Rows.Add(row);
        }
        document.Blocks.Add(table);
        var bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null,
            new byte[] { 0x20, 0x80, 0xD0, 0xFF }, 4);
        bitmap.Freeze();
        var paragraph = new Paragraph(new Run("Before picture "));
        paragraph.Inlines.Add(new InlineUIContainer(new Image
        {
            Source = bitmap,
            Width = 180,
            Height = 120
        }));
        paragraph.Inlines.Add(new Run(" after picture."));
        document.Blocks.Add(paragraph);
        document.Blocks.Add(new Paragraph(new Hyperlink(new Run("Safe hyperlink"))
        {
            NavigateUri = new Uri("https://example.invalid/writer-pagination")
        }));
        for (var index = 0; index < 36; index++)
            document.Blocks.Add(new Paragraph(new Run($"Diagnostic tail {index:D2}.")));
        return document;

        static TableCell Cell(string text) => new(new Paragraph(new Run(text))
        {
            Margin = new Thickness(2)
        })
        {
            Padding = new Thickness(3),
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5)
        };
    }

    private static FlowDocument CreateDocument(DocumentPageSettings settings) => new()
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

    private sealed class ProductionWorkspace : IDisposable
    {
        private ProductionWorkspace(DocumentPageSettings settings, RichTextBox editor,
            WriterPaginatedDiagnosticSurface surface,
            WriterPaginatedDiagnosticController controller, Window window)
        {
            Settings = settings;
            Editor = editor;
            Surface = surface;
            Controller = controller;
            Window = window;
        }

        internal DocumentPageSettings Settings { get; }
        internal RichTextBox Editor { get; }
        internal WriterPaginatedDiagnosticSurface Surface { get; }
        internal WriterPaginatedDiagnosticController Controller { get; }
        internal Window Window { get; }

        internal static ProductionWorkspace Create(FlowDocument document)
        {
            var settings = DocumentPageSettings.Letter();
            var editor = new RichTextBox
            {
                Document = document,
                IsUndoEnabled = true,
                Opacity = 0.01
            };
            var surface = new WriterPaginatedDiagnosticSurface();
            var grid = new Grid();
            grid.Children.Add(new AdornerDecorator { Child = editor });
            grid.Children.Add(surface);
            var window = new Window
            {
                Content = grid,
                Width = 1000,
                Height = 760,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                Opacity = 0.01
            };
            window.Show();
            window.UpdateLayout();
            var controller = new WriterPaginatedDiagnosticController(editor, surface, settings);
            return new ProductionWorkspace(settings, editor, surface, controller, window);
        }

        public void Dispose()
        {
            Controller.Dispose();
            if (Window.IsVisible)
                Window.Close();
        }
    }
}
