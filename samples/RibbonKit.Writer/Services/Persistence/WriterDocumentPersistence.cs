using System.Text;
using System.IO;
using System.Windows.Documents;
using System.Windows;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Documents;

namespace RibbonKit.Writer.Services.Persistence;

/// <summary>WPF-native TXT and RTF persistence for Writer documents.</summary>
public sealed class WriterDocumentPersistence : IWriterDocumentPersistence
{
    public static WriterPersistenceCapabilities GetCapabilities(WriterDocumentFormat format) => format switch
    {
        WriterDocumentFormat.PlainText => new(false, false, false, false,
            "Plain text stores characters only; formatting, images, tables, and page settings are lost."),
        WriterDocumentFormat.RichText => new(true, false, false, false,
            "RTF preserves representative text formatting; advanced content is best effort."),
        WriterDocumentFormat.RibbonKitWriter => throw new NotSupportedException(
            "RibbonKit Writer (.rkw) persistence is owned by W2-B."),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown Writer document format.")
    };

    public async Task<WriterDocument?> LoadAsync(string path, WriterDocumentFormat format,
        CancellationToken cancellationToken)
    {
        Validate(path, format);
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var document = format switch
        {
            WriterDocumentFormat.PlainText => LoadText(bytes),
            WriterDocumentFormat.RichText => LoadRtf(bytes),
            _ => throw new NotSupportedException("Unsupported Writer document format.")
        };
        cancellationToken.ThrowIfCancellationRequested();
        return new WriterDocument(document, path, format);
    }

    public async Task<bool> SaveAsync(WriterDocument document, string path, WriterDocumentFormat format,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        Validate(path, format);
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = format switch
        {
            WriterDocumentFormat.PlainText => Encoding.UTF8.GetBytes(CanonicalizeText(new TextRange(
                document.Content.ContentStart, document.Content.ContentEnd).Text)),
            WriterDocumentFormat.RichText => SaveRtf(document.Content),
            _ => throw new NotSupportedException("Unsupported Writer document format.")
        };
        cancellationToken.ThrowIfCancellationRequested();
        await AtomicFileWriter.WriteAsync(path, bytes, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static FlowDocument LoadText(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var document = new FlowDocument();
        document.Blocks.Add(new Paragraph(new Run(reader.ReadToEnd())));
        return document;
    }

    private static FlowDocument LoadRtf(byte[] bytes)
    {
        if (bytes.Length < 5 || !Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 5)).StartsWith("{\\rtf", StringComparison.Ordinal))
            throw new InvalidDataException("The input is not a valid RTF document.");
        using var stream = new MemoryStream(bytes, writable: false);
        var document = new FlowDocument();
        var range = new TextRange(document.ContentStart, document.ContentEnd);
        range.Load(stream, DataFormats.Rtf);
        return document;
    }

    private static string CanonicalizeText(string text) => text.EndsWith("\r\n", StringComparison.Ordinal)
        ? text[..^2] : text.EndsWith('\n') ? text[..^1] : text;

    private static byte[] SaveRtf(FlowDocument document)
    {
        using var stream = new MemoryStream();
        var range = new TextRange(document.ContentStart, document.ContentEnd);
        range.Save(stream, DataFormats.Rtf);
        return stream.ToArray();
    }

    private static void Validate(string path, WriterDocumentFormat format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Enum.IsDefined(format))
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown Writer document format.");
        if (format == WriterDocumentFormat.RibbonKitWriter)
            throw new NotSupportedException("RibbonKit Writer (.rkw) persistence is owned by W2-B.");
        var extension = Path.GetExtension(path);
        var expected = format == WriterDocumentFormat.PlainText ? ".txt" : ".rtf";
        if (!string.Equals(extension, expected, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"The destination must use the {expected} extension.", nameof(path));
    }
}
