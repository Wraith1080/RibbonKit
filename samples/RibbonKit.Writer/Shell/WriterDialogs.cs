using System.Windows;
using Microsoft.Win32;
using System.IO;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Documents;

namespace RibbonKit.Writer.Shell;

/// <summary>Selected path and format returned by an open dialog.</summary>
public sealed record WriterOpenSelection(string Path, WriterDocumentFormat Format);

/// <summary>Converts a save-dialog result into a destination whose extension and format agree.</summary>
public static class WriterSaveDialogSelection
{
    /// <summary>
    /// Resolves the Writer save filters without relying on the native dialog's automatic extension.
    /// The selected filter is authoritative, including when the typed extension is missing or differs.
    /// </summary>
    public static WriterSaveDestination? Resolve(string? selectedPath, int filterIndex)
    {
        if (string.IsNullOrWhiteSpace(selectedPath))
            return null;

        var format = filterIndex switch
        {
            1 => WriterDocumentFormat.RichText,
            2 => WriterDocumentFormat.PlainText,
            3 => WriterDocumentFormat.RibbonKitWriter,
            _ => throw new ArgumentOutOfRangeException(nameof(filterIndex), filterIndex,
                "Writer save filter index must be 1 (RTF), 2 (TXT), or 3 (RKW).")
        };
        var extension = format switch
        {
            WriterDocumentFormat.RichText => ".rtf",
            WriterDocumentFormat.PlainText => ".txt",
            WriterDocumentFormat.RibbonKitWriter => ".rkw",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        var normalizedPath = string.Equals(Path.GetExtension(selectedPath), extension,
            StringComparison.OrdinalIgnoreCase)
            ? selectedPath
            : Path.ChangeExtension(selectedPath, extension);
        return new WriterSaveDestination(normalizedPath, format);
    }
}

/// <summary>Dialog boundary used by the shell and replaced by fakes in tests.</summary>
public interface IWriterDialogService
{
    Task<WriterOpenSelection?> ShowOpenAsync(CancellationToken cancellationToken = default);
    Task<WriterSaveDestination?> ShowSaveAsync(WriterDocument document, CancellationToken cancellationToken = default);
    Task<UnsavedChangesDecision> ConfirmUnsavedAsync(WriterDocument document, DocumentTransition transition, CancellationToken cancellationToken = default);
    Task<bool> ConfirmPlainTextFidelityAsync(CancellationToken cancellationToken = default);
    Task ShowErrorAsync(string message, CancellationToken cancellationToken = default);
    Task ShowInfoAsync(string message, CancellationToken cancellationToken = default);
}

/// <summary>Windows dialog implementation for the Writer shell.</summary>
public sealed class WriterDialogService : IWriterDialogService
{
    private Window? _owner;
    public WriterDialogService(Window? owner = null) => _owner = owner;
    public Window? Owner { get => _owner; set => _owner = value; }

    public Task<WriterOpenSelection?> ShowOpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dialog = new OpenFileDialog { Filter = "Writer documents (*.rkw;*.rtf;*.txt)|*.rkw;*.rtf;*.txt|Rich Text (*.rtf)|*.rtf|Plain Text (*.txt)|*.txt|RibbonKit Writer (*.rkw)|*.rkw", CheckFileExists = true };
        if (dialog.ShowDialog(_owner) != true) return Task.FromResult<WriterOpenSelection?>(null);
        var format = System.IO.Path.GetExtension(dialog.FileName).ToLowerInvariant() switch
        {
            ".txt" => WriterDocumentFormat.PlainText,
            ".rkw" => WriterDocumentFormat.RibbonKitWriter,
            _ => WriterDocumentFormat.RichText
        };
        return Task.FromResult<WriterOpenSelection?>(new WriterOpenSelection(dialog.FileName, format));
    }

    public Task<WriterSaveDestination?> ShowSaveAsync(WriterDocument document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dialog = new SaveFileDialog { Filter = "Rich Text (*.rtf)|*.rtf|Plain Text (*.txt)|*.txt|RibbonKit Writer (*.rkw)|*.rkw",
            FilterIndex = document.Format switch
            {
                WriterDocumentFormat.PlainText => 2,
                WriterDocumentFormat.RibbonKitWriter => 3,
                _ => 1
            },
            AddExtension = false,
            FileName = document.Path is null ? "Untitled" : System.IO.Path.GetFileName(document.Path), OverwritePrompt = true };
        if (dialog.ShowDialog(_owner) != true) return Task.FromResult<WriterSaveDestination?>(null);
        return Task.FromResult(WriterSaveDialogSelection.Resolve(dialog.FileName, dialog.FilterIndex));
    }

    public Task<UnsavedChangesDecision> ConfirmUnsavedAsync(WriterDocument document, DocumentTransition transition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = MessageBox.Show(_owner, $"Save changes to {document.Path ?? "Untitled"} before {transition.ToString().ToLowerInvariant()}?", "RibbonKit Writer", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes ? UnsavedChangesDecision.Save : result == MessageBoxResult.No ? UnsavedChangesDecision.Discard : UnsavedChangesDecision.Cancel);
    }

    public Task<bool> ConfirmPlainTextFidelityAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = MessageBox.Show(_owner, "Plain text loses formatting, images, tables, and page settings. Continue?", "Save as Plain Text", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    public Task ShowErrorAsync(string message, CancellationToken cancellationToken = default) { cancellationToken.ThrowIfCancellationRequested(); MessageBox.Show(_owner, message, "RibbonKit Writer", MessageBoxButton.OK, MessageBoxImage.Error); return Task.CompletedTask; }
    public Task ShowInfoAsync(string message, CancellationToken cancellationToken = default) { cancellationToken.ThrowIfCancellationRequested(); MessageBox.Show(_owner, message, "RibbonKit Writer", MessageBoxButton.OK, MessageBoxImage.Information); return Task.CompletedTask; }
}
