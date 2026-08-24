using System.Windows.Documents;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Printing;

/// <summary>Edges of the logical page that a printer may clip.</summary>
[Flags]
public enum WriterPrintConflictEdges
{
    /// <summary>No edge conflict.</summary>
    None = 0,

    /// <summary>The requested left margin is smaller than the device's left non-imageable area.</summary>
    Left = 1,

    /// <summary>The requested top margin is smaller than the device's top non-imageable area.</summary>
    Top = 2,

    /// <summary>The requested right margin is smaller than the device's right non-imageable area.</summary>
    Right = 4,

    /// <summary>The requested bottom margin is smaller than the device's bottom non-imageable area.</summary>
    Bottom = 8,

    /// <summary>The printer media size differs from the preview page size.</summary>
    PageSize = 16,

    /// <summary>The printer did not report enough data to verify its imageable bounds.</summary>
    CapabilitiesUnavailable = 32
}

/// <summary>Describes the printer's page and imageable rectangle in WPF DIPs.</summary>
public readonly record struct WriterPrintDeviceCapabilities
{
    private const double BoundsToleranceDip = 0.5;

    /// <summary>Creates and validates a page/imageable rectangle.</summary>
    public WriterPrintDeviceCapabilities(double pageWidthDip, double pageHeightDip,
        double imageableOriginXDip, double imageableOriginYDip,
        double imageableWidthDip, double imageableHeightDip)
    {
        ValidateFinitePositive(pageWidthDip, nameof(pageWidthDip));
        ValidateFinitePositive(pageHeightDip, nameof(pageHeightDip));
        ValidateFiniteNonNegative(imageableOriginXDip, nameof(imageableOriginXDip));
        ValidateFiniteNonNegative(imageableOriginYDip, nameof(imageableOriginYDip));
        ValidateFiniteNonNegative(imageableWidthDip, nameof(imageableWidthDip));
        ValidateFiniteNonNegative(imageableHeightDip, nameof(imageableHeightDip));
        if (imageableOriginXDip + imageableWidthDip > pageWidthDip + BoundsToleranceDip ||
            imageableOriginYDip + imageableHeightDip > pageHeightDip + BoundsToleranceDip)
            throw new ArgumentException("The imageable rectangle must lie within the printer page.");
        PageWidthDip = pageWidthDip;
        PageHeightDip = pageHeightDip;
        ImageableOriginXDip = imageableOriginXDip;
        ImageableOriginYDip = imageableOriginYDip;
        ImageableWidthDip = imageableWidthDip;
        ImageableHeightDip = imageableHeightDip;
    }

    /// <summary>Gets the printer page width.</summary>
    public double PageWidthDip { get; }

    /// <summary>Gets the printer page height.</summary>
    public double PageHeightDip { get; }

    /// <summary>Gets the imageable rectangle's left origin.</summary>
    public double ImageableOriginXDip { get; }

    /// <summary>Gets the imageable rectangle's top origin.</summary>
    public double ImageableOriginYDip { get; }

    /// <summary>Gets the imageable rectangle width.</summary>
    public double ImageableWidthDip { get; }

    /// <summary>Gets the imageable rectangle height.</summary>
    public double ImageableHeightDip { get; }

    /// <summary>Creates capabilities from a page size and an imageable rectangle.</summary>
    public static WriterPrintDeviceCapabilities Create(
        double pageWidthDip, double pageHeightDip,
        double imageableOriginXDip, double imageableOriginYDip,
        double imageableWidthDip, double imageableHeightDip)
    {
        return new(pageWidthDip, pageHeightDip, imageableOriginXDip, imageableOriginYDip,
            imageableWidthDip, imageableHeightDip);
    }

    internal void EnsureValid() => _ = new WriterPrintDeviceCapabilities(
        PageWidthDip, PageHeightDip, ImageableOriginXDip, ImageableOriginYDip,
        ImageableWidthDip, ImageableHeightDip);

    private static void ValidateFinitePositive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(name, value,
                "Printer page dimensions must be finite and positive.");
    }

    private static void ValidateFiniteNonNegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(name, value,
                "Printer dimensions must be finite and non-negative.");
    }
}

/// <summary>Explicitly reports a printer media size that differs from the preview page.</summary>
public sealed record WriterPrintPageSizeMismatch(
    double LogicalWidthDip,
    double LogicalHeightDip,
    double DeviceWidthDip,
    double DeviceHeightDip)
{
    /// <summary>Gets the absolute width difference.</summary>
    public double WidthDifferenceDip => Math.Abs(LogicalWidthDip - DeviceWidthDip);

    /// <summary>Gets the absolute height difference.</summary>
    public double HeightDifferenceDip => Math.Abs(LogicalHeightDip - DeviceHeightDip);
}

/// <summary>One explicitly reported logical-page clipping edge.</summary>
public sealed record WriterPrintConflict(
    WriterPrintConflictEdges Edge,
    double RequiredMarginDip,
    double DeviceNonImageableDip,
    double ClippingDip)
{
    /// <summary>Gets a human-readable edge label.</summary>
    public string EdgeName => Edge.ToString();
}

/// <summary>Result of comparing logical margins with a printer imageable rectangle.</summary>
public sealed class WriterPrintAnalysis
{
    internal WriterPrintAnalysis(DocumentPageSettings pageSettings,
        WriterPrintDeviceCapabilities? capabilities, IReadOnlyList<WriterPrintConflict> conflicts,
        WriterPrintPageSizeMismatch? pageSizeMismatch)
    {
        PageSettings = pageSettings;
        Capabilities = capabilities;
        Conflicts = conflicts.ToArray();
        PageSizeMismatch = pageSizeMismatch;
        ConflictingEdges = conflicts.Aggregate(WriterPrintConflictEdges.None,
            (edges, conflict) => edges | conflict.Edge);
        if (pageSizeMismatch is not null)
            ConflictingEdges |= WriterPrintConflictEdges.PageSize;
        if (capabilities is null)
            ConflictingEdges |= WriterPrintConflictEdges.CapabilitiesUnavailable;
    }

    /// <summary>Gets the logical settings that were analyzed.</summary>
    public DocumentPageSettings PageSettings { get; }

    /// <summary>Gets the printer capabilities used for the analysis, or null when unavailable.</summary>
    public WriterPrintDeviceCapabilities? Capabilities { get; }

    /// <summary>Gets whether the printer reported a complete page and imageable rectangle.</summary>
    public bool AreCapabilitiesAvailable => Capabilities is not null;

    /// <summary>Gets every conflicting edge, in stable left/top/right/bottom order.</summary>
    public IReadOnlyList<WriterPrintConflict> Conflicts { get; }

    /// <summary>Gets the explicit media-size mismatch, if any.</summary>
    public WriterPrintPageSizeMismatch? PageSizeMismatch { get; }

    /// <summary>Gets all conflicting edges as a flags value.</summary>
    public WriterPrintConflictEdges ConflictingEdges { get; }

    /// <summary>Gets whether printing has a margin, media-size, or capability conflict.</summary>
    public bool HasConflicts => ConflictingEdges != WriterPrintConflictEdges.None;
}

/// <summary>Controls how the print service handles imageable-area conflicts.</summary>
public enum WriterPrintConflictBehavior
{
    /// <summary>Report conflicts and submit the exact preview paginator unchanged.</summary>
    ReportOnly = 0,

    /// <summary>Report conflicts and refuse submission.</summary>
    Reject = 1
}

/// <summary>Options for one print submission.</summary>
public sealed record WriterPrintOptions
{
    /// <summary>Gets or initializes the conflict behavior.</summary>
    public WriterPrintConflictBehavior ConflictBehavior { get; init; } = WriterPrintConflictBehavior.ReportOnly;

    /// <summary>Gets or initializes the device job name.</summary>
    public string DocumentName { get; init; } = "RibbonKit Writer document";
}

/// <summary>Result of a print analysis and optional submission.</summary>
public sealed class WriterPrintResult
{
    internal WriterPrintResult(WriterPrintAnalysis analysis, DocumentPaginator paginator, bool submitted)
    {
        Analysis = analysis;
        Paginator = paginator;
        Submitted = submitted;
    }

    /// <summary>Gets the full imageable-area analysis.</summary>
    public WriterPrintAnalysis Analysis { get; }

    /// <summary>Gets the exact snapshot paginator passed to the print device.</summary>
    public DocumentPaginator Paginator { get; }

    /// <summary>Gets whether the print device accepted the paginator.</summary>
    public bool Submitted { get; }
}

/// <summary>A testable seam for a physical or virtual printer.</summary>
public interface IWriterPrintDevice
{
    /// <summary>Gets the current page and imageable rectangle capabilities, or null when unavailable.</summary>
    WriterPrintDeviceCapabilities? Capabilities { get; }

    /// <summary>Submits the supplied paginator without changing its page settings.</summary>
    void Submit(DocumentPaginator paginator, string documentName);
}
