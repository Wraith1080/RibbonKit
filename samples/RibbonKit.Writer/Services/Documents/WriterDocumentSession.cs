using System.Windows.Documents;
using System.ComponentModel;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Services.Documents;

/// <summary>Coordinates document lifetime operations and commits state only at safe boundaries.</summary>
public sealed class WriterDocumentSession : INotifyPropertyChanged
{
    private readonly IWriterDocumentPersistence _persistence;
    private readonly IUnsavedChangesDecider _decider;
    private readonly IWriterSaveDestinationProvider _destinationProvider;
    private readonly WriterDocumentFormatTransitionPolicy _transitionPolicy;
    private readonly IWriterFormatTransitionDecider _transitionDecider;

    public WriterDocumentSession(IWriterDocumentPersistence persistence, IUnsavedChangesDecider decider,
        IWriterSaveDestinationProvider? destinationProvider = null,
        WriterDocumentProfile? defaultProfile = null,
        IWriterFormatTransitionDecider? transitionDecider = null,
        WriterDocumentFormatTransitionPolicy? transitionPolicy = null)
    {
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _decider = decider ?? throw new ArgumentNullException(nameof(decider));
        _destinationProvider = destinationProvider ?? NullSaveDestinationProvider.Instance;
        DefaultProfile = WriterDocumentProfiles.EnsureCanonical(
            defaultProfile ?? WriterDocumentProfiles.Default, nameof(defaultProfile));
        _transitionPolicy = transitionPolicy ?? WriterDocumentFormatTransitionPolicy.Default;
        _transitionDecider = transitionDecider ?? AllowFormatTransitionDecider.Instance;
        _currentDocument = DefaultProfile.CreateUntitledDocument();
    }

    private WriterDocument _currentDocument;

    /// <summary>Gets the profile used by no-argument <see cref="NewAsync()"/>.</summary>
    public WriterDocumentProfile DefaultProfile { get; }

    public WriterDocument CurrentDocument => _currentDocument;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Task<bool> NewAsync(CancellationToken cancellationToken = default) =>
        NewAsync(DefaultProfile, cancellationToken);

    /// <summary>Creates an untitled document with an explicit profile after the unsaved decision.</summary>
    public Task<bool> NewAsync(WriterDocumentProfile profile,
        CancellationToken cancellationToken = default)
    {
        WriterDocumentProfiles.EnsureCanonical(profile);
        return ReplaceAsync(DocumentTransition.New, () => profile.CreateUntitledDocument(), cancellationToken);
    }

    /// <summary>Creates an untitled document with an explicit format identity.</summary>
    public Task<bool> NewAsync(WriterDocumentFormat format,
        CancellationToken cancellationToken = default) =>
        NewAsync(WriterDocumentProfiles.ForFormat(format), cancellationToken);

    public async Task<bool> OpenAsync(string path, WriterDocumentFormat format,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ValidateFormat(format);
        if (!await CanReplaceAsync(DocumentTransition.Open, cancellationToken).ConfigureAwait(true))
            return false;

        WriterDocument? candidate;
        try
        {
            candidate = await _persistence.LoadAsync(path, format, cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        if (candidate is null)
            return false;

        candidate.CommitIdentity(path, format);
        SetCurrentDocument(candidate);
        return true;
    }

    /// <summary>Opens a document using an explicit profile identity.</summary>
    public Task<bool> OpenAsync(string path, WriterDocumentProfile profile,
        CancellationToken cancellationToken = default)
    {
        WriterDocumentProfiles.EnsureCanonical(profile);
        return OpenAsync(path, profile.Format, cancellationToken);
    }

    public Task<bool> SaveAsync(CancellationToken cancellationToken = default)
    {
        var path = CurrentDocument.Path;
        return path is null ? SaveUntitledAsync(cancellationToken) : SaveToAsync(path, CurrentDocument.Format,
            cancellationToken, commitIdentity: false);
    }

    public Task<bool> SaveAsAsync(string path, WriterDocumentFormat format,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ValidateFormat(format);
        return SaveToAsync(path, format, cancellationToken, commitIdentity: true);
    }

    /// <summary>Saves under an explicit profile, committing its identity only after success.</summary>
    public Task<bool> SaveAsAsync(string path, WriterDocumentProfile profile,
        CancellationToken cancellationToken = default)
    {
        WriterDocumentProfiles.EnsureCanonical(profile);
        return SaveAsAsync(path, profile.Format, cancellationToken);
    }

    public async Task<bool> RequestCloseAsync(CancellationToken cancellationToken = default)
    {
        if (!CurrentDocument.IsDirty)
            return true;

        var decision = await GetDecisionAsync(DocumentTransition.Close, cancellationToken).ConfigureAwait(true);
        return decision switch
        {
            UnsavedChangesDecision.Cancel => false,
            UnsavedChangesDecision.Discard => true,
            UnsavedChangesDecision.Save => await SaveAsync(cancellationToken).ConfigureAwait(true),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<bool> ReplaceAsync(DocumentTransition transition, Func<WriterDocument> candidateFactory,
        CancellationToken cancellationToken)
    {
        if (!await CanReplaceAsync(transition, cancellationToken).ConfigureAwait(true))
            return false;

        SetCurrentDocument(candidateFactory());
        return true;
    }

    private async Task<bool> CanReplaceAsync(DocumentTransition transition, CancellationToken cancellationToken)
    {
        if (!CurrentDocument.IsDirty)
            return true;

        var decision = await GetDecisionAsync(transition, cancellationToken).ConfigureAwait(true);
        return decision switch
        {
            UnsavedChangesDecision.Cancel => false,
            UnsavedChangesDecision.Discard => true,
            UnsavedChangesDecision.Save => await SaveAsync(cancellationToken).ConfigureAwait(true),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<UnsavedChangesDecision> GetDecisionAsync(DocumentTransition transition,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _decider.DecideAsync(CurrentDocument, transition, cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return UnsavedChangesDecision.Cancel;
        }
    }

    private async Task<bool> SaveToAsync(string path, WriterDocumentFormat format,
        CancellationToken cancellationToken, bool commitIdentity)
    {
        if (!await ConfirmFormatTransitionAsync(format, cancellationToken).ConfigureAwait(true))
            return false;

        try
        {
            if (!await _persistence.SaveAsync(CurrentDocument, path, format, cancellationToken).ConfigureAwait(true))
                return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        if (commitIdentity)
            CurrentDocument.CommitIdentity(path, format);
        else
            CurrentDocument.MarkClean();
        return true;
    }

    private async Task<bool> SaveUntitledAsync(CancellationToken cancellationToken)
    {
        WriterSaveDestination? destination;
        try
        {
            destination = await _destinationProvider.GetDestinationAsync(CurrentDocument, cancellationToken)
                .ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        return destination is not null && await SaveToAsync(destination.Path, destination.Format,
            cancellationToken, commitIdentity: true).ConfigureAwait(true);
    }

    private async Task<bool> ConfirmFormatTransitionAsync(
        WriterDocumentFormat targetFormat,
        CancellationToken cancellationToken)
    {
        var transition = _transitionPolicy.Evaluate(CurrentDocument, targetFormat);
        if (!transition.RequiresConfirmation)
            return true;

        WriterFormatTransitionDecision decision;
        try
        {
            decision = await _transitionDecider.DecideAsync(
                CurrentDocument, transition, cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        return decision switch
        {
            WriterFormatTransitionDecision.Continue => true,
            WriterFormatTransitionDecision.Cancel => false,
            _ => throw new ArgumentOutOfRangeException(nameof(decision), decision,
                "Unknown format-transition decision.")
        };
    }

    private void SetCurrentDocument(WriterDocument document)
    {
        if (ReferenceEquals(_currentDocument, document))
            return;
        _currentDocument = document;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentDocument)));
    }

    private static void ValidateFormat(WriterDocumentFormat format)
    {
        if (!Enum.IsDefined(format))
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown Writer document format.");
    }

    private sealed class NullSaveDestinationProvider : IWriterSaveDestinationProvider
    {
        public static NullSaveDestinationProvider Instance { get; } = new();
        public Task<WriterSaveDestination?> GetDestinationAsync(WriterDocument document,
            CancellationToken cancellationToken) => Task.FromResult<WriterSaveDestination?>(null);
    }

    private sealed class AllowFormatTransitionDecider : IWriterFormatTransitionDecider
    {
        public static AllowFormatTransitionDecider Instance { get; } = new();

        public Task<WriterFormatTransitionDecision> DecideAsync(WriterDocument document,
            WriterDocumentFormatTransition transition, CancellationToken cancellationToken) =>
            Task.FromResult(WriterFormatTransitionDecision.Continue);
    }
}
