using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Services.Persistence;

/// <summary>Describes the fidelity guarantees of a Writer persistence format.</summary>
public sealed record WriterPersistenceCapabilities(
    bool PreservesFormatting,
    bool PreservesImages,
    bool PreservesTables,
    bool PreservesPageSettings,
    string Description)
{
    /// <summary>Whether hyperlinks round-trip in this serializer.</summary>
    public bool PreservesHyperlinks { get; init; }

    public bool HasFidelityLoss => !PreservesFormatting || !PreservesImages || !PreservesTables
        || !PreservesHyperlinks || !PreservesPageSettings;
}

/// <summary>Canonical persistence fidelity facts shared by profiles and serializers.</summary>
public static class WriterPersistenceCapabilityCatalog
{
    /// <summary>Gets the fidelity facts for a supported Writer format.</summary>
    public static WriterPersistenceCapabilities Get(WriterDocumentFormat format) => format switch
    {
        WriterDocumentFormat.PlainText => new(
            PreservesFormatting: false,
            PreservesImages: false,
            PreservesTables: false,
            PreservesPageSettings: false,
            "Plain text stores characters only; formatting, images, tables, and page settings are lost."),
        WriterDocumentFormat.RichText => new(
            PreservesFormatting: true,
            PreservesImages: false,
            PreservesTables: false,
            PreservesPageSettings: false,
            "RTF preserves representative text formatting; advanced content is best effort."),
        WriterDocumentFormat.RibbonKitWriter => new(
            PreservesFormatting: true,
            PreservesImages: true,
            PreservesTables: false,
            PreservesPageSettings: true,
            "RibbonKit Writer preserves supported text formatting, portable images, hyperlinks and page settings; tables remain a later structured-content slice.")
        {
            PreservesHyperlinks = true
        },
        _ => throw new ArgumentOutOfRangeException(nameof(format), format,
            "Unknown Writer document format.")
    };
}
