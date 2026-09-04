using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Pagination;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.View;

namespace RibbonKit.Writer;

public partial class MainWindow
{
    private WriterPaginatedDiagnosticSurface? _paginationDiagnosticSurface;
    private WriterPaginatedDiagnosticController? _paginationDiagnosticController;
    private WriterPaginationObjectKind? _paginationResizeKind;
    private long _paginationProbePeakWorkingSet;
    private long _paginationProbePeakPrivateBytes;

    internal bool IsPaginationDiagnosticEnabled =>
        _paginationDiagnosticController is not null;

    private void InitializePaginationDiagnostic()
    {
        if (!WriterPaginationDiagnosticOptions.IsEnabled)
            return;
        if (WriterPaginationDiagnosticOptions.ShouldSeedMixedStressDocument)
        {
            Shell.CurrentDocument.CommitIdentity(path: null,
                WriterDocumentFormat.RibbonKitWriter);
            SeedPaginationMixedStressDocument();
        }
        else if (WriterPaginationDiagnosticOptions.ShouldSeedStressDocument)
        {
            Shell.CurrentDocument.CommitIdentity(path: null,
                WriterDocumentFormat.RibbonKitWriter);
            SeedPaginationStressDocument();
        }
        else if (WriterPaginationDiagnosticOptions.ShouldSeedStructuralTableDocument)
        {
            Shell.CurrentDocument.CommitIdentity(path: null,
                WriterDocumentFormat.RibbonKitWriter);
            SeedPaginationStructuralTableDocument();
        }
        else if (WriterPaginationDiagnosticOptions.ShouldSeedDocument)
        {
            Shell.CurrentDocument.CommitIdentity(path: null,
                WriterDocumentFormat.RibbonKitWriter);
            SeedPaginationDiagnosticDocument();
        }

        _paginationDiagnosticSurface = new WriterPaginatedDiagnosticSurface();
        PaginationDiagnosticHost.Children.Add(_paginationDiagnosticSurface);
        _paginationDiagnosticController = new WriterPaginatedDiagnosticController(
            DocumentEditor, _paginationDiagnosticSurface,
            Shell.CurrentDocument.PageSettings)
        {
            StructuredObjectActivator = ActivatePaginationObject,
            StructuredResizeStarter = BeginPaginationResize,
            StructuredResizeUpdater = UpdatePaginationResize,
            StructuredResizeCommitter = CommitPaginationResize,
            StructuredResizeCanceler = CancelPaginationResize
        };
        _paginationDiagnosticController.SetZoom(
            _editingController?.Zoom.Value ?? 100d);
        RefreshPaginationDiagnosticChrome();
        SpellCheck.SetIsEnabled(DocumentEditor, true);
        if (WriterPaginationDiagnosticOptions.ShouldRunStressBurst)
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,
                QueuePaginationDiagnosticStressBurst);
        if (WriterPaginationDiagnosticOptions.ShouldRunScrollProbe)
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,
                new Action(() => _ = RunPaginationScrollProbeAsync()));
    }

    private void DisposePaginationDiagnostic()
    {
        CancelPaginationResize();
        _paginationDiagnosticController?.Dispose();
        _paginationDiagnosticController = null;
        if (_paginationDiagnosticSurface is not null)
        {
            PaginationDiagnosticHost.Children.Remove(_paginationDiagnosticSurface);
            _paginationDiagnosticSurface = null;
        }
    }

    private void ApplyPaginationDiagnosticVisibility(WriterViewMode mode)
    {
        var active = _paginationDiagnosticController is not null &&
            mode == WriterViewMode.Paper;
        PaginationDiagnosticHost.Visibility = active
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Keep the single native editor loaded, realized and focusable in this HWND. The
        // diagnostic compositor receives pointer input, while keyboard commands continue to
        // route to the transparent authoritative RichTextBox beneath it.
        EditorSurface.Opacity = active ? 0 : 1;
        EditorSurface.IsHitTestVisible = !active;
    }

    private bool ActivatePaginationObject(TextElement element)
    {
        if (element is InlineUIContainer picture)
            return _pictureInteractionController?.SelectPicture(picture) == true;
        if (element is not Table table || _tableInteractionController is null)
            return false;
        var cells = _tableInteractionController.GetOrderedCells(table);
        if (cells.Count == 0)
            return false;
        _tableInteractionController.MoveCaret(cells[0]);
        return ReferenceEquals(_tableInteractionController.CurrentTable, table);
    }

    private bool BeginPaginationResize(TextElement element,
        WriterPaginationResizeHandleKind handle, int handleIndex, int rowGroupIndex)
    {
        CancelPaginationResize();
        if (element is InlineUIContainer picture &&
            handle is >= WriterPaginationResizeHandleKind.PictureTopLeft and
                <= WriterPaginationResizeHandleKind.PictureLeft &&
            _pictureInteractionController?.SelectPicture(picture) == true &&
            _pictureInteractionController.BeginExternalResize(ToPictureHandle(handle)))
        {
            _paginationResizeKind = WriterPaginationObjectKind.Picture;
            return true;
        }
        if (element is Table table && handle is WriterPaginationResizeHandleKind.TableColumn
                or WriterPaginationResizeHandleKind.TableRow
                or WriterPaginationResizeHandleKind.TableOverall &&
            _tableInteractionController is not null && _tableResizeController is not null)
        {
            var cells = _tableInteractionController.GetOrderedCells(table);
            if (cells.Count == 0)
                return false;
            var target = handle == WriterPaginationResizeHandleKind.TableRow
                ? cells.FirstOrDefault(cell => cell.GroupIndex == rowGroupIndex &&
                    cell.Row <= handleIndex && cell.LastRow >= handleIndex)
                : cells[0];
            if (target.Cell is null)
                return false;
            _tableInteractionController.MoveCaret(target);
            var tableHandle = handle switch
            {
                WriterPaginationResizeHandleKind.TableColumn =>
                    new WriterTableResizeHandle(WriterTableResizeHandleKind.Column, handleIndex),
                WriterPaginationResizeHandleKind.TableRow =>
                    new WriterTableResizeHandle(WriterTableResizeHandleKind.Row, handleIndex),
                _ => new WriterTableResizeHandle(WriterTableResizeHandleKind.Overall)
            };
            if (ReferenceEquals(_tableInteractionController.CurrentTable, table) &&
                _tableResizeController.BeginExternalResize(tableHandle))
            {
                _paginationResizeKind = WriterPaginationObjectKind.Table;
                return true;
            }
        }
        return false;
    }

    private void UpdatePaginationResize(WriterPaginationResizeHandleKind handle,
        double deltaX, double deltaY)
    {
        var delta = new Vector(deltaX, deltaY);
        if (_paginationResizeKind == WriterPaginationObjectKind.Picture)
            _pictureInteractionController?.UpdateExternalResize(delta);
        else if (_paginationResizeKind == WriterPaginationObjectKind.Table)
            _tableResizeController?.UpdateExternalResize(delta);
    }

    private bool CommitPaginationResize()
    {
        var kind = _paginationResizeKind;
        _paginationResizeKind = null;
        return kind switch
        {
            WriterPaginationObjectKind.Picture =>
                _pictureInteractionController?.CompleteExternalResize() == true,
            WriterPaginationObjectKind.Table =>
                _tableResizeController?.CompleteExternalResize() == true,
            _ => false
        };
    }

    private void CancelPaginationResize()
    {
        var kind = _paginationResizeKind;
        _paginationResizeKind = null;
        if (kind == WriterPaginationObjectKind.Picture)
            _pictureInteractionController?.CancelExternalResize();
        else if (kind == WriterPaginationObjectKind.Table)
            _tableResizeController?.CancelExternalResize();
    }

    private void RefreshPaginationDiagnosticChrome()
    {
        if (_paginationDiagnosticController is null || _editingController is null)
            return;
        _paginationDiagnosticController.SetChrome(_rulerVisible, _marginGuidesVisible,
            _editingController.Editing.ReadRulerIndentation());
    }

    private static WriterPictureResizeHandle ToPictureHandle(
        WriterPaginationResizeHandleKind handle) => handle switch
    {
        WriterPaginationResizeHandleKind.PictureTopLeft => WriterPictureResizeHandle.TopLeft,
        WriterPaginationResizeHandleKind.PictureTop => WriterPictureResizeHandle.Top,
        WriterPaginationResizeHandleKind.PictureTopRight => WriterPictureResizeHandle.TopRight,
        WriterPaginationResizeHandleKind.PictureRight => WriterPictureResizeHandle.Right,
        WriterPaginationResizeHandleKind.PictureBottomRight => WriterPictureResizeHandle.BottomRight,
        WriterPaginationResizeHandleKind.PictureBottom => WriterPictureResizeHandle.Bottom,
        WriterPaginationResizeHandleKind.PictureBottomLeft => WriterPictureResizeHandle.BottomLeft,
        WriterPaginationResizeHandleKind.PictureLeft => WriterPictureResizeHandle.Left,
        _ => throw new ArgumentOutOfRangeException(nameof(handle), handle,
            "The diagnostic handle is not a picture handle.")
    };

    private void SeedPaginationDiagnosticDocument()
    {
        var document = Shell.CurrentDocument.Content;
        if (!string.IsNullOrWhiteSpace(
                new TextRange(document.ContentStart, document.ContentEnd).Text))
            return;
        document.Blocks.Clear();
        for (var index = 0; index < 92; index++)
        {
            var spellingProbe = index is 0 or 36 or 37 or 72
                ? " and spelling qzxwvv"
                : string.Empty;
            document.Blocks.Add(new Paragraph(new Run(
                $"Diagnostic paragraph {index:D3}: cross-page native editing{spellingProbe}."))
            {
                Margin = new Thickness(0, 0, 0, 6)
            });
        }

        var table = new Table { CellSpacing = 2 };
        table.Columns.Add(new TableColumn { Width = new GridLength(230) });
        table.Columns.Add(new TableColumn { Width = new GridLength(230) });
        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        for (var rowIndex = 0; rowIndex < 34; rowIndex++)
        {
            var row = new TableRow();
            row.Cells.Add(DiagnosticCell($"Table row {rowIndex:D2}, first column."));
            row.Cells.Add(DiagnosticCell($"Table row {rowIndex:D2}, second column."));
            group.Rows.Add(row);
        }
        document.Blocks.Add(table);

        var bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null,
            new byte[] { 0x35, 0x88, 0xD0, 0xFF }, 4);
        bitmap.Freeze();
        var picture = new InlineUIContainer(new Image
        {
            Source = bitmap,
            Width = 180,
            Height = 120
        });
        var pictureParagraph = new Paragraph(new Run("Before diagnostic picture "));
        pictureParagraph.Inlines.Add(picture);
        pictureParagraph.Inlines.Add(new Run(" after picture."));
        document.Blocks.Add(pictureParagraph);
        document.Blocks.Add(new Paragraph(new Hyperlink(
            new Run("Diagnostic safe hyperlink"))
        {
            NavigateUri = new Uri("https://example.invalid/writer-pagination")
        }));
        for (var index = 0; index < 36; index++)
            document.Blocks.Add(new Paragraph(new Run($"Diagnostic tail {index:D2}.")));

        static TableCell DiagnosticCell(string text) => new(new Paragraph(new Run(text))
        {
            Margin = new Thickness(2)
        })
        {
            Padding = new Thickness(3),
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5)
        };
    }

    private void SeedPaginationStructuralTableDocument()
    {
        var document = Shell.CurrentDocument.Content;
        if (!string.IsNullOrWhiteSpace(
                new TextRange(document.ContentStart, document.ContentEnd).Text))
            return;
        document.Blocks.Clear();
        for (var index = 0; index < 18; index++)
        {
            document.Blocks.Add(new Paragraph(new Run(
                $"Structural diagnostic paragraph {index:D2}."))
            {
                Margin = new Thickness(0, 0, 0, 6)
            });
        }

        var table = new Table { CellSpacing = 2 };
        for (var index = 0; index < 3; index++)
            table.Columns.Add(new TableColumn { Width = GridLength.Auto });
        var first = new TableRowGroup();
        table.RowGroups.Add(first);
        first.Rows.Add(Row(Cell("Group 1 header", columnSpan: 2),
            Cell("Group 1 column 3")));
        first.Rows.Add(Row(Cell("Group 1 row span", rowSpan: 2),
            Cell("Group 1 row 2 column 2"), Cell("Group 1 row 2 column 3")));
        first.Rows.Add(Row(Cell("Group 1 row 3 columns 2-3", columnSpan: 2)));

        var second = new TableRowGroup();
        table.RowGroups.Add(second);
        second.Rows.Add(Row(Cell("Group 2 row 1 column 1"),
            Cell("Group 2 row 1 column 2"), Cell("Group 2 row 1 column 3")));
        second.Rows.Add(Row(Cell("Group 2 footer", columnSpan: 3)));
        document.Blocks.Add(table);
        for (var index = 0; index < 18; index++)
            document.Blocks.Add(new Paragraph(new Run($"Structural tail {index:D2}.")));

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

    private void SeedPaginationStressDocument()
    {
        var document = Shell.CurrentDocument.Content;
        if (!string.IsNullOrWhiteSpace(
                new TextRange(document.ContentStart, document.ContentEnd).Text))
            return;
        document.Blocks.Clear();
        for (var index = 0; index < WriterPaginationDiagnosticOptions.StressBlockCount; index++)
        {
            var spellingProbe = index % 79 == 0 ? " qzxwvv" : string.Empty;
            document.Blocks.Add(new Paragraph(new Run(
                $"Stress {index:D4}: pagination remains responsive" +
                $"{spellingProbe}."))
            {
                Margin = new Thickness(0, 0, 0, 4)
            });
        }
    }

    private void SeedPaginationMixedStressDocument()
    {
        var document = Shell.CurrentDocument.Content;
        if (!string.IsNullOrWhiteSpace(
                new TextRange(document.ContentStart, document.ContentEnd).Text))
            return;
        document.Blocks.Clear();
        var bitmap = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null,
            new byte[]
            {
                0x35, 0x88, 0xD0, 0xFF, 0x70, 0xB0, 0x55, 0xFF,
                0xD0, 0x88, 0x35, 0xFF, 0x88, 0x55, 0xB0, 0xFF
            }, 8);
        bitmap.Freeze();
        for (var index = 0; index < WriterPaginationDiagnosticOptions.StressBlockCount; index++)
        {
            var spellingProbe = index % 79 == 0 ? " qzxwvv" : string.Empty;
            document.Blocks.Add(new Paragraph(new Run(
                $"Mixed stress {index:D4}: pagination retains native text" +
                $"{spellingProbe}."))
            {
                Margin = new Thickness(0, 0, 0, 4)
            });
            if (index > 0 && index % 75 == 0)
            {
                var table = new Table { CellSpacing = 2 };
                table.Columns.Add(new TableColumn { Width = new GridLength(230) });
                table.Columns.Add(new TableColumn { Width = new GridLength(230) });
                var group = new TableRowGroup();
                table.RowGroups.Add(group);
                for (var rowIndex = 0; rowIndex < 6; rowIndex++)
                {
                    var row = new TableRow();
                    row.Cells.Add(MixedCell(
                        $"Table {index / 75:D2}, row {rowIndex:D2}, first."));
                    row.Cells.Add(MixedCell(
                        $"Table {index / 75:D2}, row {rowIndex:D2}, second."));
                    group.Rows.Add(row);
                }
                document.Blocks.Add(table);
            }
            if (index > 0 && index % 90 == 0)
            {
                var paragraph = new Paragraph(new Run(
                    $"Mixed picture {index / 90:D2} "));
                paragraph.Inlines.Add(new InlineUIContainer(new Image
                {
                    Source = bitmap,
                    Width = 240,
                    Height = 140
                }));
                paragraph.Inlines.Add(new Run(" remains in the authoritative document."));
                document.Blocks.Add(paragraph);
            }
        }

        static TableCell MixedCell(string text) => new(new Paragraph(new Run(text))
        {
            Margin = new Thickness(2)
        })
        {
            Padding = new Thickness(3),
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5)
        };
    }

    private void QueuePaginationDiagnosticStressBurst()
    {
        if (_paginationDiagnosticController is null)
            return;
        var original = Shell.CurrentDocument.PageSettings;
        for (var index = 0; index < 18; index++)
        {
            var margins = new DocumentPageMargins(
                54 + index % 4 * 6,
                60 + index % 3 * 6,
                66 + index % 4 * 6,
                72 + index % 3 * 6);
            var settings = (index & 1) == 0
                ? DocumentPageSettings.A4(DocumentPageOrientation.Landscape, margins)
                : DocumentPageSettings.Legal(DocumentPageOrientation.Portrait, margins);
            _paginationDiagnosticController.SetPageSettings(settings);
        }
        _paginationDiagnosticController.SetPageSettings(original);
    }

    private async Task RunPaginationScrollProbeAsync()
    {
        try
        {
            _paginationProbePeakWorkingSet = 0;
            _paginationProbePeakPrivateBytes = 0;
            var initial = await WaitForPaginationVisibleAsync();
            await WaitForPaginationPrefetchAsync(initial.Generation);
            WritePaginationProbe("initial", initial, 0);

            var cycles = WriterPaginationDiagnosticOptions.ScrollProbeCycles;
            var lastSequential = cycles > 1
                ? initial.PageCount - 1
                : Math.Min(initial.PageCount - 1, 6);
            for (var cycle = 1; cycle <= cycles; cycle++)
            {
                WritePaginationProcessSnapshot($"cycle-{cycle}-start");
                for (var page = 1; page <= lastSequential; page++)
                    await MeasurePaginationPageRequestAsync(page,
                        $"cycle-{cycle}-forward");
                for (var page = lastSequential - 1; page >= 1; page--)
                    await MeasurePaginationPageRequestAsync(page,
                        $"cycle-{cycle}-reverse");
                WritePaginationProcessSnapshot($"cycle-{cycle}-end");
            }
            await MeasurePaginationPageRequestAsync(Math.Min(2, lastSequential),
                "cached-revisit");

            if (initial.PageCount > 4 && _paginationDiagnosticSurface is not null &&
                _paginationDiagnosticController is not null)
            {
                var abandonedPage = initial.PageCount - 1;
                var latestPage = Math.Max(1, initial.PageCount - 3);
                var watch = Stopwatch.StartNew();
                _paginationDiagnosticSurface.RequestPageForTesting(abandonedPage);
                var abandonedPlaceholder = _paginationDiagnosticSurface.PlaceholderPages
                    .Contains(abandonedPage);
                _paginationDiagnosticSurface.RequestPageForTesting(latestPage);
                var latestPlaceholder = _paginationDiagnosticSurface.PlaceholderPages
                    .Contains(latestPage);
                var latest = await WaitForPaginationVisibleAsync();
                watch.Stop();
                WriterPaginationDiagnosticOptions.WriteTelemetry(
                    $"probe fast-jump abandoned={abandonedPage + 1:N0} " +
                    $"latest={latestPage + 1:N0} placeholders=" +
                    $"{abandonedPlaceholder}/{latestPlaceholder} accepted=" +
                    $"{latest.VisiblePage + 1:N0} elapsed={watch.Elapsed.TotalMilliseconds:0.0}ms");
                WritePaginationProbe("fast-jump", latest, watch.Elapsed.TotalMilliseconds);
            }

            if (_paginationDiagnosticController is not null)
            {
                var original = Shell.CurrentDocument.PageSettings;
                var reflow = DocumentPageSettings.A4(DocumentPageOrientation.Landscape,
                    new DocumentPageMargins(48, 60, 72, 84));
                var watch = Stopwatch.StartNew();
                _paginationDiagnosticController.SetPageSettings(reflow);
                var reflowIdentity = _paginationDiagnosticController.LayoutIdentity;
                var reflowed = await WaitForPaginationNewLayoutAsync(reflowIdentity);
                watch.Stop();
                WritePaginationProbe("reflow", reflowed, watch.Elapsed.TotalMilliseconds);

                watch.Restart();
                _paginationDiagnosticController.SetPageSettings(original);
                var restoreIdentity = _paginationDiagnosticController.LayoutIdentity;
                var restored = await WaitForPaginationNewLayoutAsync(restoreIdentity);
                watch.Stop();
                WritePaginationProbe("reflow-restore", restored,
                    watch.Elapsed.TotalMilliseconds);
            }

            var dispatcherWatch = Stopwatch.StartNew();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            dispatcherWatch.Stop();
            WritePaginationProcessSnapshot("before-idle-settle");
            await Task.Delay(2500);
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            WritePaginationProcessSnapshot("after-idle-settle");
            var process = Process.GetCurrentProcess();
            process.Refresh();
            var statistics = _paginationDiagnosticController?.WorkStatistics ?? default;
            WriterPaginationDiagnosticOptions.WriteTelemetry(
                $"probe complete ui-idle={dispatcherWatch.Elapsed.TotalMilliseconds:0.0}ms " +
                $"working={process.WorkingSet64 / 1024d / 1024d:0.0}MB " +
                $"private={process.PrivateMemorySize64 / 1024d / 1024d:0.0}MB " +
                $"managed={GC.GetTotalMemory(false) / 1024d / 1024d:0.0}MB " +
                $"peak-working={_paginationProbePeakWorkingSet / 1024d / 1024d:0.0}MB " +
                $"peak-private={_paginationProbePeakPrivateBytes / 1024d / 1024d:0.0}MB " +
                $"sessions={statistics.SessionsCreatedCount:N0}/{statistics.SessionsDisposedCount:N0} " +
                $"cache={statistics.CachedPageCount:N0}pages/" +
                $"{statistics.CachedBytes / 1024d / 1024d:0.0}MB-total/" +
                $"{statistics.CachedDecodedBytes / 1024d / 1024d:0.0}MB-decoded " +
                $"hits={statistics.CacheHitCount:N0} misses={statistics.CacheMissCount:N0} " +
                $"evicted={statistics.EvictedPageCount:N0}");
        }
        catch (Exception exception)
        {
            WriterPaginationDiagnosticOptions.WriteTelemetry(
                $"probe failed {exception.GetType().Name}: {exception.Message}");
        }
        finally
        {
            if (WriterPaginationDiagnosticOptions.ShouldExitAfterProbe)
                Close();
        }
    }

    private async Task MeasurePaginationPageRequestAsync(int pageNumber, string direction)
    {
        if (_paginationDiagnosticSurface is null)
            return;
        var watch = Stopwatch.StartNew();
        _paginationDiagnosticSurface.RequestPageForTesting(pageNumber);
        var result = await WaitForPaginationVisibleAsync();
        watch.Stop();
        WritePaginationProbe(direction, result, watch.Elapsed.TotalMilliseconds);
        await WaitForPaginationPrefetchAsync(result.Generation);
    }

    private async Task<WriterPaginationLayoutResult> WaitForPaginationVisibleAsync()
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            if (_paginationDiagnosticController is { } controller &&
                controller.LastVisible is { } visible &&
                visible.Generation == controller.RequestedGeneration)
                return visible;
            await Task.Delay(15);
        }
        throw new TimeoutException("The live pagination visible request did not publish.");
    }

    private async Task WaitForPaginationPrefetchAsync(long generation)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            if (_paginationDiagnosticController is not { } controller ||
                controller.RequestedGeneration != generation)
                return;
            if (controller.PrefetchSettledGeneration == generation)
                return;
            if (controller.Current is { } current && current.Generation == generation &&
                current.RequestKind == WriterPaginationRequestKind.Prefetch)
                return;
            await Task.Delay(15);
        }
    }

    private async Task<WriterPaginationLayoutResult> WaitForPaginationNewLayoutAsync(
        long layoutIdentity)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            if (_paginationDiagnosticController?.LastNewSession is { } result &&
                result.LayoutIdentity == layoutIdentity)
                return result;
            await Task.Delay(15);
        }
        throw new TimeoutException(
            $"Pagination layout identity {layoutIdentity} did not publish.");
    }

    private void WritePaginationProbe(string label, WriterPaginationLayoutResult result,
        double elapsedMilliseconds)
    {
        var process = Process.GetCurrentProcess();
        process.Refresh();
        _paginationProbePeakWorkingSet = Math.Max(_paginationProbePeakWorkingSet,
            process.WorkingSet64);
        _paginationProbePeakPrivateBytes = Math.Max(_paginationProbePeakPrivateBytes,
            process.PrivateMemorySize64);
        var statistics = _paginationDiagnosticController?.WorkStatistics ?? default;
        WriterPaginationDiagnosticOptions.WriteTelemetry(
            $"probe {label} page={result.VisiblePage + 1:N0}/{result.PageCount:N0} " +
            $"elapsed={elapsedMilliseconds:0.0}ms worker={result.WorkerMilliseconds:0.0}ms " +
            $"session={(result.ReusedLayoutSession ? "reused" : "new")} " +
            $"hit/miss={result.CacheHitCount:N0}/{result.CacheMissCount:N0} " +
            $"retained={result.RetainedPages.Length:N0} " +
            $"cache={result.CachedBytes / 1024d / 1024d:0.0}MB-total/" +
            $"{result.CachedDecodedBytes / 1024d / 1024d:0.0}MB-decoded/" +
            $"{result.CachedEncodedBytes / 1024d / 1024d:0.0}MB-encoded " +
            $"evicted={result.EvictedPageCount:N0} " +
            $"working={process.WorkingSet64 / 1024d / 1024d:0.0}MB " +
            $"private={process.PrivateMemorySize64 / 1024d / 1024d:0.0}MB " +
            $"managed={GC.GetTotalMemory(false) / 1024d / 1024d:0.0}MB " +
            $"work={statistics.CompletedCount:N0}/{statistics.StartedCount:N0}");
    }

    private void WritePaginationProcessSnapshot(string label)
    {
        var process = Process.GetCurrentProcess();
        process.Refresh();
        _paginationProbePeakWorkingSet = Math.Max(_paginationProbePeakWorkingSet,
            process.WorkingSet64);
        _paginationProbePeakPrivateBytes = Math.Max(_paginationProbePeakPrivateBytes,
            process.PrivateMemorySize64);
        var statistics = _paginationDiagnosticController?.WorkStatistics ?? default;
        WriterPaginationDiagnosticOptions.WriteTelemetry(
            $"probe {label} working={process.WorkingSet64 / 1024d / 1024d:0.0}MB " +
            $"private={process.PrivateMemorySize64 / 1024d / 1024d:0.0}MB " +
            $"managed={GC.GetTotalMemory(false) / 1024d / 1024d:0.0}MB " +
            $"cache={statistics.CachedPageCount:N0}pages/" +
            $"{statistics.CachedBytes / 1024d / 1024d:0.0}MB-total/" +
            $"{statistics.CachedDecodedBytes / 1024d / 1024d:0.0}MB-decoded " +
            $"sessions={statistics.SessionsCreatedCount:N0}/" +
            $"{statistics.SessionsDisposedCount:N0}");
    }
}
