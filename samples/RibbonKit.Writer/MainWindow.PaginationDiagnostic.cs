using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

    internal bool IsPaginationDiagnosticEnabled =>
        _paginationDiagnosticController is not null;

    private void InitializePaginationDiagnostic()
    {
        if (!WriterPaginationDiagnosticOptions.IsEnabled)
            return;
        if (WriterPaginationDiagnosticOptions.ShouldSeedDocument)
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
        WriterPaginationResizeHandleKind handle)
    {
        CancelPaginationResize();
        if (element is InlineUIContainer picture &&
            handle != WriterPaginationResizeHandleKind.TableOverall &&
            _pictureInteractionController?.SelectPicture(picture) == true &&
            _pictureInteractionController.BeginExternalResize(ToPictureHandle(handle)))
        {
            _paginationResizeKind = WriterPaginationObjectKind.Picture;
            return true;
        }
        if (element is Table table &&
            handle == WriterPaginationResizeHandleKind.TableOverall &&
            _tableInteractionController is not null && _tableResizeController is not null)
        {
            var cells = _tableInteractionController.GetOrderedCells(table);
            if (cells.Count == 0)
                return false;
            _tableInteractionController.MoveCaret(cells[0]);
            if (ReferenceEquals(_tableInteractionController.CurrentTable, table) &&
                _tableResizeController.BeginExternalOverallResize())
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
}
