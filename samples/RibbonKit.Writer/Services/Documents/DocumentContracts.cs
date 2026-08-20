using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Services.Documents;

public enum UnsavedChangesDecision
{
    Cancel,
    Discard,
    Save
}

public enum DocumentTransition
{
    New,
    Open,
    Close
}

/// <summary>Loads and persists documents without coupling the session to file dialogs or file IO.</summary>
public interface IWriterDocumentPersistence
{
    Task<WriterDocument?> LoadAsync(string path, WriterDocumentFormat format, CancellationToken cancellationToken);

    /// <returns><see langword="true"/> only when the document was persisted.</returns>
    Task<bool> SaveAsync(WriterDocument document, string path, WriterDocumentFormat format,
        CancellationToken cancellationToken);
}

public sealed record WriterSaveDestination
{
    public WriterSaveDestination(string path, WriterDocumentFormat format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Enum.IsDefined(format))
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown Writer document format.");
        Path = path;
        Format = format;
    }

    public string Path { get; }
    public WriterDocumentFormat Format { get; }
}

/// <summary>Chooses a destination for saving an untitled document.</summary>
public interface IWriterSaveDestinationProvider
{
    Task<WriterSaveDestination?> GetDestinationAsync(WriterDocument document,
        CancellationToken cancellationToken);
}

/// <summary>Provides the user's choice before a dirty document is replaced or closed.</summary>
public interface IUnsavedChangesDecider
{
    Task<UnsavedChangesDecision> DecideAsync(WriterDocument document, DocumentTransition transition,
        CancellationToken cancellationToken);
}
