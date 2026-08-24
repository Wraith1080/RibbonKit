using System.Windows.Documents;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RibbonKit.Writer.Models;

/// <summary>Document content and lifetime-owned identity for a Writer document.</summary>
public sealed class WriterDocument : INotifyPropertyChanged
{
    public WriterDocument(FlowDocument content, string? path = null,
        WriterDocumentFormat format = WriterDocumentFormat.RichText,
        DocumentPageSettings? pageSettings = null)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        if (path is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ValidateFormat(format);
        Path = path;
        Format = format;
        PageSettings = pageSettings ?? DocumentPageSettings.Letter();
    }

    public FlowDocument Content { get; }

    public string? Path { get; private set; }

    public WriterDocumentFormat Format { get; private set; }

    /// <summary>Gets the logical page settings persisted by the native Writer format.</summary>
    public DocumentPageSettings PageSettings { get; private set; }

    public bool IsDirty { get; private set; }

    public bool IsUntitled => Path is null;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void MarkDirty() => SetDirty(true);

    public void MarkClean() => SetDirty(false);

    /// <summary>Replaces the logical page settings and marks the document dirty when they changed.</summary>
    public bool SetPageSettings(DocumentPageSettings pageSettings)
    {
        ArgumentNullException.ThrowIfNull(pageSettings);
        if (PageSettings == pageSettings)
            return false;

        PageSettings = pageSettings;
        OnPropertyChanged(nameof(PageSettings));
        MarkDirty();
        return true;
    }

    internal void CommitIdentity(string? path, WriterDocumentFormat format)
    {
        ValidateFormat(format);
        if (Path != path)
        {
            Path = path;
            OnPropertyChanged(nameof(Path));
            OnPropertyChanged(nameof(IsUntitled));
        }
        if (Format != format)
        {
            Format = format;
            OnPropertyChanged(nameof(Format));
        }
        SetDirty(false);
    }

    private void SetDirty(bool value)
    {
        if (IsDirty == value)
            return;
        IsDirty = value;
        OnPropertyChanged(nameof(IsDirty));
    }

    private static void ValidateFormat(WriterDocumentFormat format)
    {
        if (!Enum.IsDefined(format))
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown Writer document format.");
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
