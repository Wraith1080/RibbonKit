using System.Windows.Controls;
using System.Windows.Documents;

namespace RibbonKit.Writer.Printing;

/// <summary>
/// Adapts an already configured and accepted WPF print dialog to Writer's testable print-device seam.
/// </summary>
/// <remarks>
/// Showing and accepting the dialog belongs to the W2-E shell integration. This adapter validates
/// the selected queue/ticket capabilities and never substitutes logical document margins.
/// </remarks>
public sealed class WriterPrintDialogDevice : IWriterPrintDevice
{
    private readonly PrintDialog _dialog;

    /// <summary>Creates a device from an accepted WPF print dialog.</summary>
    public WriterPrintDialogDevice(PrintDialog dialog)
    {
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        var queue = dialog.PrintQueue ?? throw new InvalidOperationException(
            "The print dialog has no selected print queue.");
        var requestedTicket = dialog.PrintTicket ?? queue.DefaultPrintTicket;
        var validation = queue.MergeAndValidatePrintTicket(queue.DefaultPrintTicket, requestedTicket);
        dialog.PrintTicket = validation.ValidatedPrintTicket;
        var printCapabilities = queue.GetPrintCapabilities(validation.ValidatedPrintTicket);
        var imageableArea = printCapabilities.PageImageableArea;
        var pageWidth = printCapabilities.OrientedPageMediaWidth;
        var pageHeight = printCapabilities.OrientedPageMediaHeight;
        if (imageableArea is not null && pageWidth is not null && pageHeight is not null)
        {
            Capabilities = new WriterPrintDeviceCapabilities(pageWidth.Value, pageHeight.Value,
                imageableArea.OriginWidth, imageableArea.OriginHeight,
                imageableArea.ExtentWidth, imageableArea.ExtentHeight);
        }
    }

    /// <inheritdoc />
    public WriterPrintDeviceCapabilities? Capabilities { get; }

    /// <inheritdoc />
    public void Submit(DocumentPaginator paginator, string documentName)
    {
        ArgumentNullException.ThrowIfNull(paginator);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);
        _dialog.PrintDocument(paginator, documentName);
    }
}
