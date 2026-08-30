using System.Windows.Documents;
using RibbonKit.Writer.Preview;

namespace RibbonKit.Writer.Printing;

/// <summary>Analyzes printer imageable bounds and submits the snapshot's isolated print paginator.</summary>
public sealed class WriterPrintService
{
    /// <summary>Differences at or below this value are treated as printer rounding.</summary>
    public const double PageSizeToleranceDip = 0.5;

    /// <summary>Analyzes all four non-imageable edges for a preview snapshot.</summary>
    public WriterPrintAnalysis Analyze(WriterPreviewSnapshot snapshot,
        WriterPrintDeviceCapabilities? capabilities)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (capabilities is null)
            return new WriterPrintAnalysis(snapshot.PageSettings, null,
                Array.Empty<WriterPrintConflict>(), null);
        var deviceCapabilities = capabilities.Value;
        deviceCapabilities.EnsureValid();
        var settings = snapshot.PageSettings;
        var margins = settings.Margins;
        var pageWidth = deviceCapabilities.PageWidthDip;
        var pageHeight = deviceCapabilities.PageHeightDip;
        var rightNonImageable = Math.Max(0,
            pageWidth - (deviceCapabilities.ImageableOriginXDip + deviceCapabilities.ImageableWidthDip));
        var bottomNonImageable = Math.Max(0,
            pageHeight - (deviceCapabilities.ImageableOriginYDip + deviceCapabilities.ImageableHeightDip));

        var conflicts = new List<WriterPrintConflict>(capacity: 4);
        AddConflict(conflicts, WriterPrintConflictEdges.Left, margins.LeftDip,
            deviceCapabilities.ImageableOriginXDip);
        AddConflict(conflicts, WriterPrintConflictEdges.Top, margins.TopDip,
            deviceCapabilities.ImageableOriginYDip);
        AddConflict(conflicts, WriterPrintConflictEdges.Right, margins.RightDip,
            rightNonImageable);
        AddConflict(conflicts, WriterPrintConflictEdges.Bottom, margins.BottomDip,
            bottomNonImageable);
        WriterPrintPageSizeMismatch? mismatch = null;
        if (Math.Abs(settings.WidthDip - deviceCapabilities.PageWidthDip) > PageSizeToleranceDip ||
            Math.Abs(settings.HeightDip - deviceCapabilities.PageHeightDip) > PageSizeToleranceDip)
        {
            mismatch = new WriterPrintPageSizeMismatch(settings.WidthDip, settings.HeightDip,
                deviceCapabilities.PageWidthDip, deviceCapabilities.PageHeightDip);
        }
        return new WriterPrintAnalysis(settings, deviceCapabilities, conflicts, mismatch);
    }

    /// <summary>
    /// Analyzes a snapshot and submits its isolated flow paginator. The default report-only policy keeps
    /// logical margins unchanged even when the device cannot image the requested rectangle.
    /// </summary>
    public WriterPrintResult Print(WriterPreviewSnapshot snapshot, IWriterPrintDevice device,
        WriterPrintOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(device);
        options ??= new WriterPrintOptions();
        if (!Enum.IsDefined(options.ConflictBehavior))
            throw new ArgumentOutOfRangeException(nameof(options), options.ConflictBehavior,
                "Unknown print conflict behavior.");
        if (string.IsNullOrWhiteSpace(options.DocumentName))
            throw new ArgumentException("A print document name is required.", nameof(options));

        var analysis = Analyze(snapshot, device.Capabilities);
        var shouldSubmit = !analysis.HasConflicts ||
            options.ConflictBehavior == WriterPrintConflictBehavior.ReportOnly;
        if (shouldSubmit)
            device.Submit(snapshot.PrintPaginator, options.DocumentName);
        return new WriterPrintResult(analysis, snapshot.PrintPaginator, shouldSubmit);
    }

    private static void AddConflict(List<WriterPrintConflict> conflicts,
        WriterPrintConflictEdges edge, double requiredMargin, double nonImageable)
    {
        if (requiredMargin >= nonImageable)
            return;
        conflicts.Add(new WriterPrintConflict(edge, requiredMargin, nonImageable,
            nonImageable - requiredMargin));
    }
}
