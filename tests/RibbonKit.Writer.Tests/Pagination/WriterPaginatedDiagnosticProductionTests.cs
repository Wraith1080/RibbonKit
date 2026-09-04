using System.Collections.Immutable;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
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
            Assert.All(result.MappedPages, page => Assert.Contains(
                page, result.Pages.Select(item => item.PageNumber)));
            Assert.All(result.Pages, page => Assert.Contains(
                page.PageNumber, result.RetainedPages));
            Assert.All(result.MappedPages, page => Assert.Contains(
                page, workspace.Surface.RenderedPages));
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
            var phases = result.PhaseTimings;
            Assert.All(new[]
            {
                phases.PackageLoadMilliseconds,
                phases.FormattingMilliseconds,
                phases.PageCountMilliseconds,
                phases.PageStartsMilliseconds,
                phases.ObjectMappingMilliseconds,
                phases.ViewerRealizationMilliseconds,
                phases.InsertionGeometryMilliseconds,
                phases.RasterizationMilliseconds,
                phases.StructuredGeometryMilliseconds
            }, value => Assert.True(value >= 0));
            Assert.InRange(phases.AccountedMilliseconds, 0,
                result.WorkerMilliseconds + 5);
            Assert.True(workspace.Controller.LastEndToEndMilliseconds >=
                result.WorkerMilliseconds);
            Assert.Contains("phases L/C/S/G/R",
                workspace.Surface.StatusTextForTesting, StringComparison.Ordinal);
            Assert.Contains("end ", workspace.Surface.StatusTextForTesting,
                StringComparison.Ordinal);

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
    public async Task LongNativeSpellingDocumentPublishesAndUnderlinesTheExactToken()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var settings = DocumentPageSettings.Letter();
            var document = CreateParagraphDocument(settings, 179);
            var misspelling = new Run("qzxwvv");
            var probe = new Paragraph(new Run("Spelling probe "));
            probe.Inlines.Add(misspelling);
            probe.Inlines.Add(new Run(" ends here."));
            document.Blocks.InsertBefore(document.Blocks.FirstBlock, probe);
            using var workspace = ProductionWorkspace.Create(document,
                enableSpellCheck: true);

            await WaitForCurrentAsync(workspace.Controller);
            var expectedStart = document.ContentStart.GetOffsetToPosition(
                misspelling.ContentStart);
            var expectedEnd = document.ContentStart.GetOffsetToPosition(
                misspelling.ContentEnd);
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (!workspace.Surface.SpellingRangesForTesting.Contains(
                       (expectedStart, expectedEnd)) && DateTime.UtcNow < deadline)
            {
                workspace.Surface.RefreshOverlays(workspace.Editor);
                await Dispatcher.Yield(DispatcherPriority.Background);
                await Task.Delay(20);
            }

            Assert.Contains((expectedStart, expectedEnd),
                workspace.Surface.SpellingRangesForTesting);
            var result = Assert.IsType<WriterPaginationLayoutResult>(
                workspace.Controller.Current);
            var tokenGeometry = result.Insertions.Where(item =>
                    item.SourceOffset >= expectedStart &&
                    item.SourceOffset <= expectedEnd)
                .ToArray();
            Assert.NotEmpty(tokenGeometry);
            var page = Assert.Single(tokenGeometry.Select(item => item.PageNumber).Distinct());
            var expectedLeft = tokenGeometry.Min(item => item.Rectangle.X);
            var expectedRight = tokenGeometry.Max(item => item.Rectangle.X +
                Math.Max(1, item.Rectangle.Width));
            var underline = Assert.Single(
                workspace.Surface.SpellingOverlayBoundsForTesting,
                item => item.PageNumber == page);
            Assert.Equal(expectedLeft, underline.Bounds.Left, 1);
            Assert.Equal(expectedRight, underline.Bounds.Right, 1);
            Assert.True(workspace.Controller.LastEndToEndMilliseconds < 5000,
                $"Staged publication took " +
                $"{workspace.Controller.LastEndToEndMilliseconds:0.###} ms.");
        }, TimeSpan.FromSeconds(30));
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
            Assert.False(workspace.Surface.IsPageInteractiveForTesting(0));

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
            workspace.Controller.StructuredResizeStarter = (element, handle, _, _) =>
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
    public async Task TableBoundariesAreImmutablePageLocalAndIgnoreCellTextAlignment()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = ProductionWorkspace.Create(CreateStructuredDocument(
                DocumentPageSettings.Letter()));
            await WaitForCurrentAsync(workspace.Controller);
            using var accepted = new WriterPreviewCloneService().CreateSnapshot(
                workspace.Editor.Document, workspace.Settings);
            var paginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(
                accepted.PrintPaginator);
            var cloneTable = accepted.SourceClone.Blocks.OfType<Table>().Single();
            workspace.Surface.RequestPageForTesting(
                paginator.GetPageNumber(cloneTable.ContentStart));
            await WaitForCurrentAsync(workspace.Controller);

            var opening = workspace.Controller.Current!.Tables;
            Assert.NotEmpty(opening);
            Assert.All(opening, table =>
            {
                Assert.True(table.HasTrustedColumnBoundaries);
                Assert.Equal(3, table.ColumnBoundaries.Length);
                Assert.True(table.ColumnBoundaries[0] < table.ColumnBoundaries[1]);
                Assert.True(table.ColumnBoundaries[1] < table.ColumnBoundaries[2]);
                Assert.InRange(table.ColumnBoundaries[1] - table.ColumnBoundaries[0],
                    240, 255);
                Assert.InRange(table.ColumnBoundaries[2] - table.ColumnBoundaries[1],
                    240, 255);
                Assert.NotEmpty(table.RowBoundaries);
                Assert.All(table.RowBoundaries, row =>
                    Assert.InRange(row.PositionDip, table.Bounds.Y,
                        table.Bounds.Y + table.Bounds.Height + 0.01));
            });
            var openingColumns = opening.Select(table =>
                table.ColumnBoundaries.ToArray()).ToArray();
            var starts = 0;
            workspace.Controller.StructuredResizeStarter = (_, _, _, _) =>
            {
                starts++;
                return true;
            };
            var first = opening[0];
            var current = Assert.IsType<WriterPaginationLayoutResult>(
                workspace.Controller.Current);
            var invalidColumn = new WriterPaginationResizeInteraction(
                current.Generation, current.DocumentIdentity, first.PageNumber,
                first.ObjectIdentity, WriterPaginationObjectKind.Table,
                WriterPaginationResizeHandleKind.TableColumn,
                WriterPaginationResizePhase.Start, 0, 0, 99, -1);
            Assert.False(workspace.Surface.ApplyResizeRequestForTesting(invalidColumn));
            Assert.False(workspace.Surface.ApplyResizeRequestForTesting(
                invalidColumn with { Generation = invalidColumn.Generation - 1,
                    HandleIndex = 0 }));
            Assert.Equal(0, starts);

            var sourceTable = workspace.Editor.Document.Blocks.OfType<Table>().Single();
            foreach (var paragraph in sourceTable.RowGroups[0].Rows
                         .SelectMany(row => row.Cells)
                         .SelectMany(cell => cell.Blocks.OfType<Paragraph>()))
                paragraph.TextAlignment = TextAlignment.Right;
            workspace.Controller.RefreshFormatting();
            await WaitForCurrentAsync(workspace.Controller);

            var aligned = workspace.Controller.Current!.Tables;
            Assert.Equal(openingColumns.Length, aligned.Length);
            for (var tableIndex = 0; tableIndex < aligned.Length; tableIndex++)
            for (var column = 0; column < aligned[tableIndex].ColumnBoundaries.Length; column++)
                Assert.Equal(openingColumns[tableIndex][column],
                    aligned[tableIndex].ColumnBoundaries[column], 3);
        }, TimeSpan.FromSeconds(45));
    }

    [Fact]
    public async Task RowGroupsAndSpansPublishRowBoundariesButAutoColumnsStayUnsupported()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = ProductionWorkspace.Create(
                CreateStructuralMatrixDocument(DocumentPageSettings.Letter()));
            await WaitForCurrentAsync(workspace.Controller);
            using var accepted = new WriterPreviewCloneService().CreateSnapshot(
                workspace.Editor.Document, workspace.Settings);
            var paginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(
                accepted.PrintPaginator);
            var cloneTable = accepted.SourceClone.Blocks.OfType<Table>().Single();
            workspace.Surface.RequestPageForTesting(
                paginator.GetPageNumber(cloneTable.ContentStart));
            await WaitForCurrentAsync(workspace.Controller);

            var current = Assert.IsType<WriterPaginationLayoutResult>(
                workspace.Controller.Current);
            var fragment = Assert.Single(current.Tables);
            Assert.False(fragment.HasTrustedColumnBoundaries);
            Assert.Empty(fragment.ColumnBoundaries);
            var expectedRows = new[]
            {
                (Group: 0, Row: 0), (Group: 0, Row: 1), (Group: 0, Row: 2),
                (Group: 1, Row: 0), (Group: 1, Row: 1)
            };
            Assert.Equal(expectedRows, fragment.RowBoundaries
                .Select(row => (row.RowGroupIndex, row.RowIndex)));
            Assert.Equal(fragment.RowBoundaries.Length, fragment.RowBoundaries
                .Select(row => (row.RowGroupIndex, row.RowIndex)).Distinct().Count());

            var interaction = workspace.Surface.CaptureObjectInteractionForTesting(
                WriterPaginationObjectKind.Table);
            workspace.Controller.StructuredObjectActivator = element => element is Table;
            workspace.Surface.ApplyInteractionForTesting(interaction);
            var peers = workspace.Surface.ResizeHandlePeersForTesting();
            Assert.Equal(expectedRows.Length, peers.Count);
            Assert.All(peers, peer => Assert.StartsWith(
                "Resize table row group ", peer.GetName(), StringComparison.Ordinal));

            var starts = 0;
            WriterPaginationResizeHandleKind? startedHandle = null;
            var startedIndex = -1;
            var startedGroup = -1;
            workspace.Controller.StructuredResizeStarter = (_, handle, index, group) =>
            {
                starts++;
                startedHandle = handle;
                startedIndex = index;
                startedGroup = group;
                return true;
            };
            workspace.Controller.StructuredResizeCanceler = () => { };
            var unsupportedColumn = new WriterPaginationResizeInteraction(
                current.Generation, current.DocumentIdentity, fragment.PageNumber,
                fragment.ObjectIdentity, WriterPaginationObjectKind.Table,
                WriterPaginationResizeHandleKind.TableColumn,
                WriterPaginationResizePhase.Start, 0, 0, 0, -1);
            Assert.False(workspace.Surface.ApplyResizeRequestForTesting(unsupportedColumn));
            Assert.False(workspace.Surface.ApplyResizeRequestForTesting(unsupportedColumn with
            {
                Handle = WriterPaginationResizeHandleKind.TableOverall,
                HandleIndex = -1
            }));
            Assert.Equal(0, starts);

            var rowBoundary = fragment.RowBoundaries.Single(row =>
                row.RowGroupIndex == 1 && row.RowIndex == 1);
            var supportedRow = unsupportedColumn with
            {
                Handle = WriterPaginationResizeHandleKind.TableRow,
                HandleIndex = rowBoundary.RowIndex,
                RowGroupIndex = rowBoundary.RowGroupIndex
            };
            Assert.True(workspace.Surface.ApplyResizeRequestForTesting(supportedRow));
            Assert.Equal(1, starts);
            Assert.Equal(WriterPaginationResizeHandleKind.TableRow, startedHandle);
            Assert.Equal(1, startedIndex);
            Assert.Equal(1, startedGroup);
            Assert.True(workspace.Surface.ApplyResizeRequestForTesting(supportedRow with
            {
                Phase = WriterPaginationResizePhase.Cancel
            }));

            var openingRows = fragment.RowBoundaries.ToDictionary(
                row => (row.RowGroupIndex, row.RowIndex), row => row.PositionDip);
            var sourceTable = workspace.Editor.Document.Blocks.OfType<Table>().Single();
            foreach (var paragraph in sourceTable.RowGroups.Cast<TableRowGroup>()
                         .SelectMany(group => group.Rows.Cast<TableRow>())
                         .SelectMany(row => row.Cells.Cast<TableCell>())
                         .SelectMany(cell => cell.Blocks.OfType<Paragraph>()))
                paragraph.TextAlignment = TextAlignment.Right;
            workspace.Controller.RefreshFormatting();
            await WaitForCurrentAsync(workspace.Controller);

            var aligned = Assert.Single(workspace.Controller.Current!.Tables);
            Assert.False(aligned.HasTrustedColumnBoundaries);
            Assert.Empty(aligned.ColumnBoundaries);
            Assert.All(aligned.RowBoundaries, row => Assert.Equal(
                openingRows[(row.RowGroupIndex, row.RowIndex)], row.PositionDip, 3));
        }, TimeSpan.FromSeconds(45));
    }

    [Theory]
    [InlineData(50, 1)]
    [InlineData(100, 1.25)]
    [InlineData(175, 1.5)]
    [InlineData(200, 2)]
    public void ResizeHandleSizesRemainScreenStableAndPixelAligned(
        double zoomPercent, double dpiScale)
    {
        var dpi = new DpiScale(dpiScale, dpiScale);
        var visual = WriterPaginatedDiagnosticSurface.GetLogicalHandleSizeForTesting(
            WriterTableResizeGeometry.VisualHandleSize, zoomPercent, dpi);
        var hit = WriterPaginatedDiagnosticSurface.GetLogicalHandleSizeForTesting(
            WriterTableResizeGeometry.HandleHitTargetSize, zoomPercent, dpi);
        var zoom = zoomPercent / 100d;

        Assert.InRange(visual.Width * zoom, 7.5, 8.5);
        Assert.InRange(hit.Width * zoom, 17.5, 18.5);
        Assert.Equal(Math.Round(visual.Width * zoom * dpiScale),
            visual.Width * zoom * dpiScale, 6);
        Assert.Equal(Math.Round(hit.Width * zoom * dpiScale),
            hit.Width * zoom * dpiScale, 6);
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
            var activationCount = 0;
            workspace.Controller.StructuredObjectActivator = element =>
            {
                activationCount++;
                return SelectTable(element);
            };
            workspace.Controller.StructuredResizeStarter = (element, handle, index, _) =>
                SelectTable(element) && tableResize.BeginExternalResize(handle switch
                {
                    WriterPaginationResizeHandleKind.TableColumn =>
                        new WriterTableResizeHandle(WriterTableResizeHandleKind.Column, index),
                    WriterPaginationResizeHandleKind.TableRow =>
                        new WriterTableResizeHandle(WriterTableResizeHandleKind.Row, index),
                    WriterPaginationResizeHandleKind.TableOverall =>
                        new WriterTableResizeHandle(WriterTableResizeHandleKind.Overall),
                    _ => new WriterTableResizeHandle(WriterTableResizeHandleKind.Select)
                });
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
            var tableObjectPeers = workspace.Surface.StructuredObjectPeersForTesting();
            Assert.Equal(tableObjectPeers.Count, tableObjectPeers
                .Select(peer => peer.GetAutomationId()).Distinct(StringComparer.Ordinal).Count());
            var tableObjectPeer = tableObjectPeers
                .First(peer => peer.GetName().StartsWith("Select table on page ",
                    StringComparison.Ordinal));
            Assert.Equal(AutomationControlType.Button,
                tableObjectPeer.GetAutomationControlType());
            var tableObjectInvoke = Assert.IsAssignableFrom<IInvokeProvider>(
                tableObjectPeer.GetPattern(PatternInterface.Invoke));
            tableObjectInvoke.Invoke();
            await workspace.Editor.Dispatcher.InvokeAsync(() => { });
            Assert.Equal(1, activationCount);
            Assert.True(workspace.Surface.ResizeHandleCount > 3);

            workspace.Controller.RefreshFormatting();
            await WaitForCurrentAsync(workspace.Controller);
            tableObjectInvoke.Invoke();
            await workspace.Editor.Dispatcher.InvokeAsync(() => { });
            Assert.Equal(1, activationCount);

            var currentInteraction = workspace.Surface.CaptureObjectInteractionForTesting(
                WriterPaginationObjectKind.Table);
            workspace.Surface.ApplyInteractionForTesting(currentInteraction);

            var surfacePeer = UIElementAutomationPeer.CreatePeerForElement(workspace.Surface);
            Assert.Equal(AutomationControlType.Pane, surfacePeer.GetAutomationControlType());
            Assert.Equal("Opt-in paginated editing diagnostic", surfacePeer.GetName());
            var handlePeers = workspace.Surface.ResizeHandlePeersForTesting();
            Assert.Equal(handlePeers.Count, handlePeers
                .Select(peer => peer.GetAutomationId()).Distinct(StringComparer.Ordinal).Count());
            var columnPeer = handlePeers.First(peer =>
                peer.GetName().StartsWith("Resize table column 1 on page ",
                    StringComparison.Ordinal));
            Assert.Equal(AutomationControlType.Button, columnPeer.GetAutomationControlType());
            var columnInvoke = Assert.IsAssignableFrom<IInvokeProvider>(
                columnPeer.GetPattern(PatternInterface.Invoke));

            var table = workspace.Editor.Document.Blocks.OfType<Table>().Single();
            var openingWidth = table.Columns.Sum(column => column.Width.Value);
            var openingFirstColumn = table.Columns[0].Width.Value;
            columnInvoke.Invoke();
            await WaitForCurrentAsync(workspace.Controller);
            table = workspace.Editor.Document.Blocks.OfType<Table>().Single();
            Assert.True(table.Columns[0].Width.Value > openingFirstColumn);
            Assert.True(workspace.Editor.CanUndo);

            var interaction = workspace.Surface.CaptureObjectInteractionForTesting(
                WriterPaginationObjectKind.Table);
            workspace.Surface.ApplyInteractionForTesting(interaction);
            var rowBoundary = workspace.Controller.Current!.Tables
                .SelectMany(item => item.RowBoundaries)
                .First();
            var openingPadding = table.RowGroups[rowBoundary.RowGroupIndex]
                .Rows[rowBoundary.RowIndex].Cells[0].Padding;
            Assert.True(workspace.Surface.BeginResizeForTesting(
                WriterPaginationObjectKind.Table,
                WriterPaginationResizeHandleKind.TableRow,
                rowBoundary.RowIndex, rowBoundary.RowGroupIndex));
            Assert.True(workspace.Surface.UpdateResizeForTesting(0, 12));
            Assert.True(workspace.Surface.CompleteResizeForTesting());
            await WaitForCurrentAsync(workspace.Controller);
            table = workspace.Editor.Document.Blocks.OfType<Table>().Single();
            Assert.True(table.RowGroups[rowBoundary.RowGroupIndex]
                .Rows[rowBoundary.RowIndex].Cells[0].Padding.Top > openingPadding.Top);

            interaction = workspace.Surface.CaptureObjectInteractionForTesting(
                WriterPaginationObjectKind.Table);
            workspace.Surface.ApplyInteractionForTesting(interaction);
            openingWidth = table.Columns.Sum(column => column.Width.Value);
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

    [Fact]
    public async Task KeyboardResizeIsOneNativeUndoUnitAndEscapeCancels()
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
            workspace.Controller.StructuredResizeStarter = (element, handle, index, _) =>
                SelectTable(element) && tableResize.BeginExternalResize(handle switch
                {
                    WriterPaginationResizeHandleKind.TableColumn =>
                        new WriterTableResizeHandle(WriterTableResizeHandleKind.Column, index),
                    WriterPaginationResizeHandleKind.TableRow =>
                        new WriterTableResizeHandle(WriterTableResizeHandleKind.Row, index),
                    WriterPaginationResizeHandleKind.TableOverall =>
                        new WriterTableResizeHandle(WriterTableResizeHandleKind.Overall),
                    _ => new WriterTableResizeHandle(WriterTableResizeHandleKind.Select)
                });
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
            workspace.Surface.ApplyInteractionForTesting(
                workspace.Surface.CaptureObjectInteractionForTesting(
                    WriterPaginationObjectKind.Table));

            var peer = workspace.Surface.ResizeHandlePeersForTesting().First(item =>
                item.GetName().StartsWith("Resize table column 1 on page ",
                    StringComparison.Ordinal));
            Assert.False(peer.IsKeyboardFocusable());
            Assert.Contains("Control+Alt+R", peer.GetHelpText(),
                StringComparison.OrdinalIgnoreCase);
            var surfacePeer = UIElementAutomationPeer.CreatePeerForElement(workspace.Surface);
            Assert.Contains("Control+Alt+R", surfacePeer.GetHelpText(),
                StringComparison.OrdinalIgnoreCase);
            Assert.Equal(Key.R, WriterPaginatedDiagnosticSurface.GetEffectiveKeyForTesting(
                Key.System, Key.R));

            var table = workspace.Editor.Document.Blocks.OfType<Table>().Single();
            var openingWidth = table.Columns[0].Width.Value;
            Assert.True(workspace.Surface.ApplyHostKeyForTesting(Key.R,
                ModifierKeys.Control | ModifierKeys.Alt));
            Assert.Contains("column 1", workspace.Surface.KeyboardResizeTargetNameForTesting,
                StringComparison.OrdinalIgnoreCase);
            Assert.True(workspace.Surface.ApplyHostKeyForTesting(Key.Tab));
            Assert.Contains("column 2", workspace.Surface.KeyboardResizeTargetNameForTesting,
                StringComparison.OrdinalIgnoreCase);
            Assert.True(workspace.Surface.ApplyHostKeyForTesting(Key.Tab,
                ModifierKeys.Shift));
            Assert.Contains("column 1", workspace.Surface.KeyboardResizeTargetNameForTesting,
                StringComparison.OrdinalIgnoreCase);
            Assert.True(workspace.Surface.ApplyHostKeyForTesting(Key.Enter));
            Assert.False(workspace.Surface.ApplyKeyboardResizeKeyForTesting(Key.Up));
            Assert.True(workspace.Surface.ApplyKeyboardResizeKeyForTesting(Key.Right));
            Assert.True(workspace.Surface.ApplyKeyboardResizeKeyForTesting(
                Key.Right, ModifierKeys.Shift));
            Assert.True(workspace.Surface.ApplyKeyboardResizeKeyForTesting(Key.Enter));
            await WaitForCurrentAsync(workspace.Controller);

            table = workspace.Editor.Document.Blocks.OfType<Table>().Single();
            Assert.Equal(openingWidth + 13, table.Columns[0].Width.Value, 6);
            Assert.True(workspace.Editor.CanUndo);
            Assert.True(workspace.Editor.IsKeyboardFocusWithin);
            workspace.Editor.Undo();
            await WaitForCurrentAsync(workspace.Controller);
            Assert.Equal(openingWidth, workspace.Editor.Document.Blocks.OfType<Table>()
                .Single().Columns[0].Width.Value, 6);

            workspace.Surface.ApplyInteractionForTesting(
                workspace.Surface.CaptureObjectInteractionForTesting(
                    WriterPaginationObjectKind.Table));
            var generationBeforeCancel = workspace.Controller.RequestedGeneration;
            Assert.True(workspace.Surface.ApplyHostKeyForTesting(Key.R,
                ModifierKeys.Control | ModifierKeys.Alt));
            Assert.True(workspace.Surface.ApplyHostKeyForTesting(Key.Enter));
            Assert.True(workspace.Surface.ApplyKeyboardResizeKeyForTesting(
                Key.Right, ModifierKeys.Shift));
            Assert.True(workspace.Surface.ApplyKeyboardResizeKeyForTesting(Key.Escape));
            Assert.False(tableResize.IsDragging);
            Assert.Equal(generationBeforeCancel, workspace.Controller.RequestedGeneration);
            Assert.Equal(openingWidth, workspace.Editor.Document.Blocks.OfType<Table>()
                .Single().Columns[0].Width.Value, 6);
            Assert.True(workspace.Editor.IsKeyboardFocusWithin);
        }, TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task SequentialForwardReverseScrollingReusesSessionAndPrefetchesDirectionally()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = ProductionWorkspace.Create(CreateParagraphDocument(
                DocumentPageSettings.Letter(), 420));
            var opening = await WaitForVisibleAsync(workspace.Controller);
            Assert.True(opening.PageCount > 7);
            await WaitForPrefetchAsync(workspace.Controller, opening.Generation);
            var sessionCount = workspace.Controller.WorkStatistics.SessionsCreatedCount;
            Assert.Equal(1, sessionCount);

            for (var page = 1; page <= 4; page++)
            {
                workspace.Surface.RequestPageForTesting(page);
                var visible = await WaitForVisibleAsync(workspace.Controller);
                Assert.Equal(page, visible.VisiblePage);
                Assert.True(visible.ReusedLayoutSession);
                Assert.Equal(0, visible.PhaseTimings.PackageLoadMilliseconds);
                Assert.Equal(0, visible.PhaseTimings.PageCountMilliseconds);
                Assert.Equal(0, visible.PhaseTimings.PageStartsMilliseconds);
                await WaitForPrefetchAsync(workspace.Controller, visible.Generation);
            }

            var forward = Assert.IsType<WriterPaginationLayoutResult>(
                workspace.Controller.Current);
            Assert.Equal(WriterPaginationRequestKind.Prefetch, forward.RequestKind);
            Assert.Contains(forward.Pages, page =>
                page.PageNumber > forward.MappedPages.Max());

            workspace.Surface.RequestPageForTesting(3);
            var reverseVisible = await WaitForVisibleAsync(workspace.Controller);
            Assert.True(reverseVisible.ReusedLayoutSession);
            await WaitForPrefetchAsync(workspace.Controller, reverseVisible.Generation);
            var reverse = Assert.IsType<WriterPaginationLayoutResult>(
                workspace.Controller.Current);
            Assert.Contains(reverse.Pages, page =>
                page.PageNumber < reverse.MappedPages.Min());

            var missesBeforeRevisit = workspace.Controller.WorkStatistics.CacheMissCount;
            workspace.Surface.RequestPageForTesting(4);
            var revisit = await WaitForVisibleAsync(workspace.Controller);
            Assert.Equal(0, revisit.CacheMissCount);
            Assert.True(revisit.ReusedLayoutSession);
            Assert.Equal(missesBeforeRevisit,
                workspace.Controller.WorkStatistics.CacheMissCount);
            Assert.Equal(sessionCount,
                workspace.Controller.WorkStatistics.SessionsCreatedCount);
        }, TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task FastJumpShowsPlaceholderAndAcceptsOnlyLatestViewport()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = ProductionWorkspace.Create(CreateParagraphDocument(
                DocumentPageSettings.Letter(), 700));
            var opening = await WaitForVisibleAsync(workspace.Controller);
            Assert.True(opening.PageCount > 10);
            await WaitForPrefetchAsync(workspace.Controller, opening.Generation);
            var oldInteraction = workspace.Surface.CaptureInteractionForTesting(0,
                opening.Insertions.First(item => item.PageNumber == 0).SourceOffset);
            var selection = SelectionOffsets(workspace.Editor);

            var lastPage = opening.PageCount - 1;
            workspace.Surface.RequestPageForTesting(lastPage);
            var abandonedGeneration = workspace.Controller.RequestedGeneration;
            Assert.Contains(lastPage, workspace.Surface.PlaceholderPages);
            Assert.False(workspace.Surface.CanCreateInteractionForTesting(lastPage));

            var latestPage = opening.PageCount / 2;
            workspace.Surface.RequestPageForTesting(latestPage);
            var latestGeneration = workspace.Controller.RequestedGeneration;
            Assert.True(latestGeneration > abandonedGeneration);
            Assert.Contains(latestPage, workspace.Surface.PlaceholderPages);
            Assert.False(workspace.Surface.IsPageInteractiveForTesting(latestPage));
            workspace.Surface.ApplyInteractionForTesting(oldInteraction);
            Assert.Equal(selection, SelectionOffsets(workspace.Editor));

            var latest = await WaitForVisibleAsync(workspace.Controller);
            Assert.Equal(latestGeneration, latest.Generation);
            Assert.Equal(latestPage, latest.VisiblePage);
            Assert.DoesNotContain(lastPage, latest.MappedPages);
            Assert.True(workspace.Surface.IsPageInteractiveForTesting(latestPage));
            Assert.False(workspace.Surface.IsPageInteractiveForTesting(lastPage));
        }, TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task BoundedCacheEvictsDistantPagesAndRerendersAnEvictedRevisit()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            const int pageLimit = 3;
            const long byteLimit = 32L * 1024 * 1024;
            using var workspace = ProductionWorkspace.Create(CreateParagraphDocument(
                DocumentPageSettings.Letter(), 600), pageCacheLimit: pageLimit,
                cacheByteLimit: byteLimit);
            var opening = await WaitForVisibleAsync(workspace.Controller);
            Assert.True(opening.PageCount > 9);
            var sessionCount = workspace.Controller.WorkStatistics.SessionsCreatedCount;

            foreach (var page in new[] { 2, 4, 6, 8 })
            {
                workspace.Surface.RequestPageForTesting(page);
                await WaitForVisibleAsync(workspace.Controller);
            }

            var statistics = workspace.Controller.WorkStatistics;
            Assert.InRange(statistics.CachedPageCount, 1, pageLimit);
            Assert.InRange(statistics.CachedBytes, 1, byteLimit);
            Assert.True(statistics.EvictedPageCount > 0);
            Assert.DoesNotContain(0, workspace.Surface.RenderedPages);

            var misses = statistics.CacheMissCount;
            workspace.Surface.RequestPageForTesting(0);
            var revisit = await WaitForVisibleAsync(workspace.Controller);
            Assert.True(revisit.CacheMissCount > 0);
            Assert.True(workspace.Controller.WorkStatistics.CacheMissCount > misses);
            Assert.Equal(sessionCount,
                workspace.Controller.WorkStatistics.SessionsCreatedCount);
            Assert.True(workspace.Surface.IsPageInteractiveForTesting(0));
        }, TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task DecodedPixelFootprintDrivesByteEvictionAndReleasesSurfaceFrames()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            const int pageLimit = 20;
            const long byteLimit = 20L * 1024 * 1024;
            using var workspace = ProductionWorkspace.Create(CreateParagraphDocument(
                DocumentPageSettings.Letter(), 500), pageCacheLimit: pageLimit,
                cacheByteLimit: byteLimit);
            var opening = await WaitForVisibleAsync(workspace.Controller);
            await WaitForPrefetchAsync(workspace.Controller, opening.Generation);
            var initial = Assert.IsType<WriterPaginationLayoutResult>(
                workspace.Controller.Current);
            Assert.True(initial.CachedDecodedBytes > initial.CachedEncodedBytes);
            Assert.True(initial.CachedBytes >= initial.CachedDecodedBytes +
                initial.CachedEncodedBytes);
            var releasedBefore = workspace.Surface.ReleasedPageFrameCount;

            for (var page = 1; page <= Math.Min(7, initial.PageCount - 1); page++)
            {
                workspace.Surface.RequestPageForTesting(page);
                var visible = await WaitForVisibleAsync(workspace.Controller);
                await WaitForPrefetchAsync(workspace.Controller, visible.Generation);
            }

            var statistics = workspace.Controller.WorkStatistics;
            Assert.InRange(statistics.CachedBytes, 1, byteLimit);
            Assert.True(statistics.CachedDecodedBytes > statistics.CachedEncodedBytes);
            Assert.True(statistics.CachedPageCount < pageLimit);
            Assert.True(statistics.EvictedPageCount > 0);
            Assert.True(workspace.Surface.ReleasedPageFrameCount > releasedBefore);
            Assert.Equal(statistics.CachedPageCount,
                workspace.Surface.RenderedPages.Count);
        }, TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task MixedContentMultiCycleScrollingStaysInOneBoundedLayoutSession()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            const int pageLimit = 5;
            const long byteLimit = 32L * 1024 * 1024;
            using var workspace = ProductionWorkspace.Create(CreateMixedContentDocument(
                DocumentPageSettings.Letter(), 260), pageCacheLimit: pageLimit,
                cacheByteLimit: byteLimit);
            var opening = await WaitForVisibleAsync(workspace.Controller);
            Assert.True(opening.PageCount > pageLimit);
            await WaitForPrefetchAsync(workspace.Controller, opening.Generation);
            var sessionCount = workspace.Controller.WorkStatistics.SessionsCreatedCount;
            var kinds = new HashSet<WriterPaginationObjectKind>();

            for (var cycle = 0; cycle < 2; cycle++)
            {
                for (var page = 1; page < opening.PageCount; page++)
                {
                    workspace.Surface.RequestPageForTesting(page);
                    var visible = await WaitForVisibleAsync(workspace.Controller);
                    foreach (var item in visible.StructuredObjects)
                        kinds.Add(item.Kind);
                    await WaitForPrefetchAsync(workspace.Controller, visible.Generation);
                }
                for (var page = opening.PageCount - 2; page >= 0; page--)
                {
                    workspace.Surface.RequestPageForTesting(page);
                    var visible = await WaitForVisibleAsync(workspace.Controller);
                    foreach (var item in visible.StructuredObjects)
                        kinds.Add(item.Kind);
                    await WaitForPrefetchAsync(workspace.Controller, visible.Generation);
                }
            }

            var statistics = workspace.Controller.WorkStatistics;
            Assert.Equal(sessionCount, statistics.SessionsCreatedCount);
            Assert.Equal(0, statistics.SessionsDisposedCount);
            Assert.InRange(statistics.CachedPageCount, 1, pageLimit);
            Assert.InRange(statistics.CachedBytes, 1, byteLimit);
            Assert.True(statistics.EvictedPageCount > 0);
            Assert.True(workspace.Surface.ReleasedPageFrameCount > 0);
            Assert.Contains(WriterPaginationObjectKind.Table, kinds);
            Assert.Contains(WriterPaginationObjectKind.Picture, kinds);
        }, TimeSpan.FromSeconds(90));
    }

    [Fact]
    public async Task LayoutIdentityInvalidatesForContentFormattingSettingsDpiAndDocumentOnly()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = ProductionWorkspace.Create(CreateParagraphDocument(
                DocumentPageSettings.Letter(), 220));
            var opening = await WaitForVisibleAsync(workspace.Controller);
            var layoutIdentity = opening.LayoutIdentity;
            var sessionCount = workspace.Controller.WorkStatistics.SessionsCreatedCount;

            workspace.Surface.RequestPageForTesting(1);
            var scrolled = await WaitForVisibleAsync(workspace.Controller);
            Assert.Equal(layoutIdentity, scrolled.LayoutIdentity);
            Assert.Equal(sessionCount,
                workspace.Controller.WorkStatistics.SessionsCreatedCount);

            workspace.Editor.CaretPosition = workspace.Editor.Document.ContentEnd;
            workspace.Editor.CaretPosition.InsertTextInRun("content invalidation");
            var content = await WaitForVisibleAsync(workspace.Controller);
            Assert.True(content.LayoutIdentity > layoutIdentity);
            Assert.True(workspace.Controller.WorkStatistics.SessionsCreatedCount > sessionCount);
            layoutIdentity = content.LayoutIdentity;
            sessionCount = workspace.Controller.WorkStatistics.SessionsCreatedCount;

            workspace.Editor.Document.FontSize += 1;
            workspace.Controller.RefreshFormatting();
            var formatting = await WaitForVisibleAsync(workspace.Controller);
            Assert.True(formatting.LayoutIdentity > layoutIdentity);
            Assert.True(workspace.Controller.WorkStatistics.SessionsCreatedCount > sessionCount);
            layoutIdentity = formatting.LayoutIdentity;
            sessionCount = workspace.Controller.WorkStatistics.SessionsCreatedCount;

            workspace.Controller.SetPageSettings(DocumentPageSettings.A4(
                DocumentPageOrientation.Landscape,
                new DocumentPageMargins(48, 60, 72, 84)));
            var settings = await WaitForVisibleAsync(workspace.Controller);
            Assert.True(settings.LayoutIdentity > layoutIdentity);
            Assert.True(workspace.Controller.WorkStatistics.SessionsCreatedCount > sessionCount);
            layoutIdentity = settings.LayoutIdentity;
            sessionCount = workspace.Controller.WorkStatistics.SessionsCreatedCount;

            workspace.Surface.RaiseDpiScaleChangedForTesting();
            var dpi = await WaitForVisibleAsync(workspace.Controller);
            Assert.True(dpi.LayoutIdentity > layoutIdentity);
            Assert.True(workspace.Controller.WorkStatistics.SessionsCreatedCount > sessionCount);
            layoutIdentity = dpi.LayoutIdentity;
            sessionCount = workspace.Controller.WorkStatistics.SessionsCreatedCount;

            var replacement = CreateParagraphDocument(workspace.Settings, 40);
            workspace.Editor.Document = replacement;
            workspace.Controller.ReplaceDocument(replacement);
            var document = await WaitForVisibleAsync(workspace.Controller);
            Assert.True(document.LayoutIdentity > layoutIdentity);
            Assert.NotEqual(opening.DocumentIdentity, document.DocumentIdentity);
            Assert.True(workspace.Controller.WorkStatistics.SessionsCreatedCount > sessionCount);
            Assert.True(workspace.Controller.WorkStatistics.SessionsDisposedCount >= 5);
        }, TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task LongDocumentBurstCoalescesPendingWorkAndCancelsActiveLayout()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = ProductionWorkspace.Create(CreateParagraphDocument(
                DocumentPageSettings.Letter(), 1600));
            var activeProgress = await WaitForActiveProgressAsync(workspace.Controller);
            Assert.Equal(workspace.Controller.RequestedGeneration,
                activeProgress.Generation);
            Assert.NotEqual(WriterPaginationWorkPhase.Idle, activeProgress.Phase);
            Assert.True(activeProgress.PhaseElapsedMilliseconds >= 0);
            workspace.Surface.ShowWorkProgress(activeProgress,
                workspace.Controller.WorkStatistics);
            Assert.Contains(activeProgress.Generation.ToString(),
                workspace.Surface.StatusTextForTesting, StringComparison.Ordinal);
            await WaitForCurrentAsync(workspace.Controller);
            var opening = workspace.Controller.WorkStatistics;
            var firstStress = DocumentPageSettings.Legal(
                DocumentPageOrientation.Landscape,
                new DocumentPageMargins(54, 60, 66, 72));
            workspace.Controller.SetPageSettings(firstStress);
            await WaitForStartedAsync(workspace.Controller, opening.StartedCount);

            for (var index = 0; index < 20; index++)
            {
                var margins = new DocumentPageMargins(
                    54 + index % 4 * 6,
                    60 + index % 3 * 6,
                    66 + index % 4 * 6,
                    72 + index % 3 * 6);
                workspace.Controller.SetPageSettings((index & 1) == 0
                    ? DocumentPageSettings.A4(DocumentPageOrientation.Landscape, margins)
                    : DocumentPageSettings.Legal(DocumentPageOrientation.Portrait, margins));
            }
            workspace.Controller.SetPageSettings(workspace.Settings);
            await WaitForCurrentAsync(workspace.Controller);

            var statistics = workspace.Controller.WorkStatistics;
            Assert.True(statistics.CanceledActiveCount > opening.CanceledActiveCount,
                $"No active layout was cancelled: {statistics}.");
            Assert.True(statistics.SupersededPendingCount > opening.SupersededPendingCount,
                $"No pending layout was coalesced: {statistics}.");
            Assert.Equal(workspace.Controller.RequestedGeneration,
                workspace.Controller.PublishedGeneration);
            var current = Assert.IsType<WriterPaginationLayoutResult>(
                workspace.Controller.Current);
            Assert.True(current.PageCount > 25);
            Assert.InRange(current.MappedPages.Length, 1, 3);
            Assert.Equal(workspace.Settings.WidthDip, current.PageSettings.WidthDip, 6);
            Assert.Equal(workspace.Settings.HeightDip, current.PageSettings.HeightDip, 6);
            Assert.Equal(workspace.Settings.Margins.LeftDip,
                current.PageSettings.LeftMarginDip, 6);
            Assert.Contains($"cancelled {statistics.CanceledActiveCount:N0}",
                workspace.Surface.StatusTextForTesting, StringComparison.Ordinal);
            Assert.Contains($"coalesced {statistics.SupersededPendingCount:N0}",
                workspace.Surface.StatusTextForTesting, StringComparison.Ordinal);
            await workspace.Editor.Dispatcher.InvokeAsync(() => { },
                DispatcherPriority.ApplicationIdle);
        }, TimeSpan.FromSeconds(90));
    }

    private static async Task<WriterPaginationLayoutResult> WaitForVisibleAsync(
        WriterPaginatedDiagnosticController controller)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            if (controller.LastVisible is { } visible &&
                visible.Generation == controller.RequestedGeneration &&
                visible.RequestKind == WriterPaginationRequestKind.Visible)
                return visible;
            await Dispatcher.Yield(DispatcherPriority.Background);
            await Task.Delay(10);
        }
        throw new TimeoutException(
            $"Visible generation {controller.RequestedGeneration} did not publish.");
    }

    private static async Task<WriterPaginationLayoutResult> WaitForPrefetchAsync(
        WriterPaginatedDiagnosticController controller, long generation)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            if (controller.PrefetchSettledGeneration == generation &&
                controller.Current is { } settled && settled.Generation == generation)
                return settled;
            if (controller.Current is { } current &&
                current.Generation == generation &&
                current.RequestKind == WriterPaginationRequestKind.Prefetch)
                return current;
            await Dispatcher.Yield(DispatcherPriority.Background);
            await Task.Delay(10);
        }
        throw new TimeoutException($"Prefetch generation {generation} did not publish.");
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

    private static async Task WaitForStartedAsync(
        WriterPaginatedDiagnosticController controller, int previousStartedCount)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            if (controller.WorkStatistics.StartedCount > previousStartedCount)
                return;
            await Dispatcher.Yield(DispatcherPriority.Background);
            await Task.Delay(5);
        }
        throw new TimeoutException("The stress pagination generation did not start.");
    }

    private static async Task<WriterPaginationWorkProgress> WaitForActiveProgressAsync(
        WriterPaginatedDiagnosticController controller)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var progress = controller.WorkProgress;
            if (progress.Phase != WriterPaginationWorkPhase.Idle)
                return progress;
            await Dispatcher.Yield(DispatcherPriority.Background);
            await Task.Delay(5);
        }
        throw new TimeoutException("The pagination worker did not expose an active phase.");
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

    private static FlowDocument CreateMixedContentDocument(
        DocumentPageSettings settings, int count)
    {
        var document = CreateDocument(settings);
        var bitmap = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null,
            new byte[]
            {
                0x35, 0x88, 0xD0, 0xFF, 0x70, 0xB0, 0x55, 0xFF,
                0xD0, 0x88, 0x35, 0xFF, 0x88, 0x55, 0xB0, 0xFF
            }, 8);
        bitmap.Freeze();
        for (var index = 0; index < count; index++)
        {
            document.Blocks.Add(new Paragraph(new Run(
                $"Mixed paragraph {index:D3}: bounded cache corpus."))
            {
                Margin = new Thickness(0, 0, 0, 5)
            });
            if (index > 0 && index % 65 == 0)
            {
                var table = new Table { CellSpacing = 2 };
                table.Columns.Add(new TableColumn { Width = new GridLength(245) });
                table.Columns.Add(new TableColumn { Width = new GridLength(245) });
                var group = new TableRowGroup();
                table.RowGroups.Add(group);
                for (var rowIndex = 0; rowIndex < 5; rowIndex++)
                {
                    var row = new TableRow();
                    row.Cells.Add(Cell($"Table {index:D3}, row {rowIndex}, first."));
                    row.Cells.Add(Cell($"Table {index:D3}, row {rowIndex}, second."));
                    group.Rows.Add(row);
                }
                document.Blocks.Add(table);
            }
            if (index > 0 && index % 80 == 0)
            {
                var paragraph = new Paragraph(new Run("Before mixed picture "));
                paragraph.Inlines.Add(new InlineUIContainer(new Image
                {
                    Source = bitmap,
                    Width = 220,
                    Height = 130
                }));
                paragraph.Inlines.Add(new Run(" after mixed picture."));
                document.Blocks.Add(paragraph);
            }
        }
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

    private static FlowDocument CreateStructuralMatrixDocument(
        DocumentPageSettings settings)
    {
        var document = CreateParagraphDocument(settings, 8);
        var table = new Table { CellSpacing = 2 };
        for (var index = 0; index < 3; index++)
            table.Columns.Add(new TableColumn { Width = GridLength.Auto });

        var first = new TableRowGroup();
        table.RowGroups.Add(first);
        first.Rows.Add(Row(Cell("Group 1 header", columnSpan: 2), Cell("Group 1 column 3")));
        first.Rows.Add(Row(Cell("Group 1 row span", rowSpan: 2),
            Cell("Group 1 row 2 column 2"), Cell("Group 1 row 2 column 3")));
        first.Rows.Add(Row(Cell("Group 1 row 3 columns 2-3", columnSpan: 2)));

        var second = new TableRowGroup();
        table.RowGroups.Add(second);
        second.Rows.Add(Row(Cell("Group 2 row 1 column 1"),
            Cell("Group 2 row 1 column 2"), Cell("Group 2 row 1 column 3")));
        second.Rows.Add(Row(Cell("Group 2 footer", columnSpan: 3)));
        document.Blocks.Add(table);
        for (var index = 0; index < 8; index++)
            document.Blocks.Add(new Paragraph(new Run($"Structural tail {index:D2}.")));
        return document;

        static TableRow Row(params TableCell[] cells)
        {
            var row = new TableRow();
            foreach (var cell in cells)
                row.Cells.Add(cell);
            return row;
        }

        static TableCell Cell(string text, int rowSpan = 1, int columnSpan = 1) =>
            new(new Paragraph(new Run(text)) { Margin = new Thickness(2) })
            {
                RowSpan = rowSpan,
                ColumnSpan = columnSpan,
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

        internal static ProductionWorkspace Create(FlowDocument document,
            bool enableSpellCheck = false,
            int pageCacheLimit = WriterDedicatedPaginationEngine.DefaultPageCacheLimit,
            long cacheByteLimit = WriterDedicatedPaginationEngine.DefaultCacheByteLimit)
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
            SpellCheck.SetIsEnabled(editor, enableSpellCheck);
            var controller = new WriterPaginatedDiagnosticController(editor, surface, settings,
                pageCacheLimit, cacheByteLimit);
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
