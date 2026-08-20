using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Documents;
using RibbonKit.Writer.Services.RecentFiles;

namespace RibbonKit.Writer.Shell;

/// <summary>Coordinates Writer document lifetime commands and observable shell state.</summary>
public sealed class WriterShellViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly WriterDocumentSession _session;
    private readonly RecentFileService _recents;
    private readonly IWriterDialogService _dialogs;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private WriterDocument _document;
    private bool _isBusy;
    private string _statusText = "Ready";
    private bool _disposed;

    /// <summary>Creates a shell over the existing document session and recent-file service.</summary>
    public WriterShellViewModel(WriterDocumentSession session, RecentFileService recents, IWriterDialogService dialogs)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _recents = recents ?? throw new ArgumentNullException(nameof(recents));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _document = session.CurrentDocument;
        Subscribe(_document);
        _session.PropertyChanged += SessionChanged;
        _recents.Load();
        RefreshRecents();
        NewCommand = new AsyncCommand(_ => NewAsync(), this);
        OpenCommand = new AsyncCommand(_ => OpenAsync(), this);
        SaveCommand = new AsyncCommand(_ => SaveAsync(), this);
        SaveAsCommand = new AsyncCommand(_ => SaveAsAsync(), this);
        ExitCommand = new AsyncCommand(_ => RequestExitAsync(), this);
        OpenRecentCommand = new AsyncCommand(p => OpenRecentAsync(p as RecentFileEntry), this);
    }

    /// <summary>Raised when the shell has approved application exit.</summary>
    public event EventHandler? ExitRequested;
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>The currently displayed document.</summary>
    public WriterDocument CurrentDocument => _document;
    /// <summary>Window title including identity and dirty marker.</summary>
    public string Title => (_document.IsUntitled ? "Untitled" : Path.GetFileName(_document.Path)) + (_document.IsDirty ? " *" : "") + " - RibbonKit Writer";
    /// <summary>Human-readable last operation result.</summary>
    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }
    /// <summary>Whether a file operation is currently running.</summary>
    public bool IsBusy { get => _isBusy; private set { if (Set(ref _isBusy, value)) { OnPropertyChanged(nameof(CanOperate)); RaiseCommands(); } } }
    /// <summary>Whether a new file operation can begin.</summary>
    public bool CanOperate => !IsBusy;
    /// <summary>Loaded recent-file entries.</summary>
    public ObservableCollection<RecentFileEntry> RecentEntries { get; } = new();
    public ICommand NewCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAsCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand OpenRecentCommand { get; }

    public Task<bool> NewAsync(CancellationToken cancellationToken = default) =>
        RunAsync("New", () => RunTransitionAsync("New", () => _session.NewAsync(cancellationToken)));
    public Task<bool> SaveAsync(CancellationToken cancellationToken = default) => RunAsync("Save", () => SaveCoreAsync(false, cancellationToken));
    public Task<bool> SaveAsAsync(CancellationToken cancellationToken = default) => RunAsync("Save As", () => SaveCoreAsync(true, cancellationToken));
    public Task<bool> RequestCloseAsync(CancellationToken cancellationToken = default) =>
        RunAsync("Close", () => RunTransitionAsync("Close", () => _session.RequestCloseAsync(cancellationToken)));
    public Task<bool> OpenAsync(CancellationToken cancellationToken = default) =>
        RunAsync("Open", () => RunTransitionAsync("Open", () => OpenSelectionAsync(cancellationToken)));
    public Task<bool> OpenRecentAsync(RecentFileEntry? entry, CancellationToken cancellationToken = default) =>
        entry is null
            ? CancelledRecentOpen()
            : RunAsync("Open recent", () => RunTransitionAsync("Open recent", () => OpenRecentPathAsync(entry, cancellationToken)));

    /// <summary>Marks the current document dirty after an editor-originated mutation.</summary>
    public void MarkEditorDirty() => _document.MarkDirty();

    /// <summary>Requests a single close decision and raises <see cref="ExitRequested"/> when approved.</summary>
    public async Task<bool> RequestExitAsync(CancellationToken cancellationToken = default)
    {
        var approved = await RequestCloseAsync(cancellationToken);
        if (approved) ExitRequested?.Invoke(this, EventArgs.Empty);
        return approved;
    }
    private async Task<bool> SaveCoreAsync(bool saveAs, CancellationToken cancellationToken)
    {
        WriterSaveDestination? destination = null;
        if (saveAs || _document.IsUntitled) destination = await SelectSaveDestinationAsync(cancellationToken);
        if (saveAs || _document.IsUntitled)
        {
            if (destination is null) { StatusText = saveAs ? "Save As cancelled" : "Save cancelled"; return false; }
            return await SaveAndRecentAsync(() => _session.SaveAsAsync(destination.Path, destination.Format, cancellationToken), destination);
        }
        return await SaveAndRecentAsync(() => _session.SaveAsync(cancellationToken), new WriterSaveDestination(_document.Path!, _document.Format));
    }
    private async Task<WriterSaveDestination?> SelectSaveDestinationAsync(CancellationToken cancellationToken)
    {
        var destination = await _dialogs.ShowSaveAsync(_document, cancellationToken);
        if (destination?.Format == WriterDocumentFormat.PlainText && !await _dialogs.ConfirmPlainTextFidelityAsync(cancellationToken)) return null;
        return destination;
    }
    private async Task<bool> SaveAndRecentAsync(Func<Task<bool>> save, WriterSaveDestination destination)
    {
        if (!await save()) { StatusText = "Save failed; document unchanged"; return false; }
        var status = destination.Format == WriterDocumentFormat.RichText
            ? "Saved RTF (advanced content best effort)"
            : "Saved TXT; formatting and page content are not preserved";
        if (!AddRecent(destination))
            status = AppendRecentFailure(status);
        StatusText = status;
        return true;
    }
    private async Task<bool> OpenSelectionAsync(CancellationToken cancellationToken)
    {
        var selected = await _dialogs.ShowOpenAsync(cancellationToken);
        if (selected is null)
        {
            StatusText = "Open cancelled";
            return false;
        }
        return await OpenPathAsync(selected, cancellationToken);
    }
    private Task<bool> CancelledRecentOpen()
    {
        StatusText = "Open recent cancelled";
        return Task.FromResult(false);
    }
    private async Task<bool> RunTransitionAsync(string operation, Func<Task<bool>> transition)
    {
        var previous = _document;
        var wasDirty = previous.IsDirty;
        var succeeded = await transition();
        var openedDestination = succeeded && (operation is "Open" or "Open recent") && _document.Path is not null
            ? new WriterSaveDestination(_document.Path, _document.Format)
            : null;
        var recentAvailable = AddSavedPreviousRecent(previous, wasDirty, openedDestination);
        if (openedDestination is not null)
        {
            // Add the target after the implicit previous-document save so the file just opened
            // remains the newest entry. Keep this as a separate call so a failed load still
            // records the saved previous document without inventing a target entry.
            var targetAvailable = AddRecent(openedDestination);
            recentAvailable &= targetAvailable;
        }

        if (operation is "New" or "Close")
        {
            StatusText = succeeded
                ? operation == "New" ? "New document created" : "Close approved"
                : wasDirty ? $"{operation} cancelled or save failed" : $"{operation} cancelled";
        }
        else if (!succeeded && string.Equals(StatusText, operation + "…", StringComparison.Ordinal))
        {
            StatusText = wasDirty && !previous.IsDirty
                ? $"{operation} cancelled or load failed"
                : wasDirty ? $"{operation} cancelled or save failed" : $"{operation} cancelled";
        }

        if (!recentAvailable)
            StatusText = AppendRecentFailure(StatusText);
        return succeeded;
    }
    private bool AddSavedPreviousRecent(WriterDocument previous, bool wasDirty,
        WriterSaveDestination? openedDestination = null)
    {
        if (!wasDirty || previous.IsDirty || previous.Path is null)
            return true;
        var previousDestination = new WriterSaveDestination(previous.Path, previous.Format);
        if (openedDestination is not null && SameRecentIdentity(previousDestination, openedDestination))
            return true;
        return AddRecent(previousDestination);
    }
    private async Task<bool> OpenPathAsync(WriterOpenSelection selected, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _session.OpenAsync(selected.Path, selected.Format, cancellationToken)) return false;
            StatusText = selected.Format == WriterDocumentFormat.RichText
                ? "Opened RTF (advanced content best effort)"
                : "Opened TXT; formatting and page content are not present";
            return true;
        }
        catch (Exception ex) { StatusText = "Open failed"; await _dialogs.ShowErrorAsync(ex.Message); return false; }
    }
    private async Task<bool> OpenRecentPathAsync(RecentFileEntry entry, CancellationToken cancellationToken)
    {
        if (!File.Exists(entry.Path))
        {
            StatusText = "Open recent failed";
            await _dialogs.ShowErrorAsync($"Recent file is no longer available: {entry.Path}");
            return false;
        }
        return await OpenPathAsync(new WriterOpenSelection(entry.Path, entry.Format), cancellationToken);
    }
    private bool AddRecent(WriterSaveDestination destination)
    {
        try
        {
            var available = _recents.TryAdd(destination.Path, destination.Format);
            RefreshRecents();
            return available;
        }
        catch
        {
            RefreshRecents();
            return false;
        }
    }
    private static string AppendRecentFailure(string status) =>
        status.EndsWith("(recent list unavailable)", StringComparison.Ordinal)
            ? status
            : status + " (recent list unavailable)";
    private static bool SameRecentIdentity(WriterSaveDestination left, WriterSaveDestination right) =>
        left.Format == right.Format && string.Equals(left.Path, right.Path, StringComparison.OrdinalIgnoreCase);
    private async Task<bool> RunAsync(string operation, Func<Task<bool>> operationFunc)
    {
        if (!await _operationGate.WaitAsync(0)) return false;
        IsBusy = true; StatusText = operation + "…";
        try
        {
            var succeeded = await operationFunc();
            if (!succeeded && string.Equals(StatusText, operation + "…", StringComparison.Ordinal))
                StatusText = operation + " cancelled";
            return succeeded;
        }
        catch (OperationCanceledException) { StatusText = operation + " cancelled"; return false; }
        catch (Exception ex) { StatusText = operation + " failed"; await _dialogs.ShowErrorAsync(ex.Message); return false; }
        finally { IsBusy = false; _operationGate.Release(); }
    }
    private void SessionChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName != nameof(WriterDocumentSession.CurrentDocument)) return; Unsubscribe(_document); _document = _session.CurrentDocument; Subscribe(_document); RefreshState(); }
    private void DocumentChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Title));
    private void RefreshState() { OnPropertyChanged(nameof(CurrentDocument)); OnPropertyChanged(nameof(Title)); }
    private void RefreshRecents() { RecentEntries.Clear(); foreach (var entry in _recents.Entries) RecentEntries.Add(entry); }
    private void Subscribe(WriterDocument document) => document.PropertyChanged += DocumentChanged;
    private void Unsubscribe(WriterDocument document) => document.PropertyChanged -= DocumentChanged;
    private void RaiseCommands() { (NewCommand as AsyncCommand)?.RaiseCanExecuteChanged(); (OpenCommand as AsyncCommand)?.RaiseCanExecuteChanged(); (SaveCommand as AsyncCommand)?.RaiseCanExecuteChanged(); (SaveAsCommand as AsyncCommand)?.RaiseCanExecuteChanged(); (ExitCommand as AsyncCommand)?.RaiseCanExecuteChanged(); (OpenRecentCommand as AsyncCommand)?.RaiseCanExecuteChanged(); }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnPropertyChanged(propertyName); return true; }
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    public void Dispose() { if (_disposed) return; _disposed = true; _session.PropertyChanged -= SessionChanged; Unsubscribe(_document); _operationGate.Dispose(); }

    private sealed class AsyncCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        private readonly WriterShellViewModel _owner;
        public AsyncCommand(Func<object?, Task> execute, WriterShellViewModel owner) { _execute = execute; _owner = owner; }
        public bool CanExecute(object? parameter) => _owner.CanOperate;
        public event EventHandler? CanExecuteChanged;
        public async void Execute(object? parameter) => await _execute(parameter);
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
