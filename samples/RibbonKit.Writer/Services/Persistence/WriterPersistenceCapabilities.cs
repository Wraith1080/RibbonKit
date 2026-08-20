namespace RibbonKit.Writer.Services.Persistence;

/// <summary>Describes the fidelity guarantees of a Writer persistence format.</summary>
public sealed record WriterPersistenceCapabilities(
    bool PreservesFormatting,
    bool PreservesImages,
    bool PreservesTables,
    bool PreservesPageSettings,
    string Description)
{
    public bool HasFidelityLoss => !PreservesFormatting || !PreservesImages || !PreservesTables || !PreservesPageSettings;
}
