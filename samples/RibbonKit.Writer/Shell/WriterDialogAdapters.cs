using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Documents;

namespace RibbonKit.Writer.Shell;

/// <summary>Adapts shell dialogs to the document session's unsaved decision contract.</summary>
public sealed class WriterUnsavedChangesDecider : IUnsavedChangesDecider
{
    private readonly IWriterDialogService _dialogs;
    public WriterUnsavedChangesDecider(IWriterDialogService dialogs) => _dialogs = dialogs;
    public Task<UnsavedChangesDecision> DecideAsync(WriterDocument document, DocumentTransition transition, CancellationToken cancellationToken) => _dialogs.ConfirmUnsavedAsync(document, transition, cancellationToken);
}

/// <summary>Adapts shell dialogs to the document session's save destination contract.</summary>
public sealed class WriterSaveDestinationProvider : IWriterSaveDestinationProvider
{
    private readonly IWriterDialogService _dialogs;
    public WriterSaveDestinationProvider(IWriterDialogService dialogs) => _dialogs = dialogs;
    public async Task<WriterSaveDestination?> GetDestinationAsync(WriterDocument document, CancellationToken cancellationToken)
    {
        var destination = await _dialogs.ShowSaveAsync(document, cancellationToken);
        if (destination?.Format == WriterDocumentFormat.PlainText &&
            !await _dialogs.ConfirmPlainTextFidelityAsync(cancellationToken))
            return null;
        return destination;
    }
}
