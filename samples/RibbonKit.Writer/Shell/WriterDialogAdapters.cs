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
    public Task<WriterSaveDestination?> GetDestinationAsync(WriterDocument document, CancellationToken cancellationToken) =>
        _dialogs.ShowSaveAsync(document, cancellationToken);
}

/// <summary>Adapts the shell's generic format-warning dialog to the W0-E session contract.</summary>
public sealed class WriterFormatTransitionDecider : IWriterFormatTransitionDecider
{
    private readonly IWriterDialogService _dialogs;

    public WriterFormatTransitionDecider(IWriterDialogService dialogs) =>
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

    public Task<WriterFormatTransitionDecision> DecideAsync(WriterDocument document,
        WriterDocumentFormatTransition transition, CancellationToken cancellationToken) =>
        _dialogs.ConfirmFormatTransitionAsync(document, transition, cancellationToken);
}
