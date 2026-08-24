using System.Text;
using System.IO;
using System.Windows.Documents;
using System.Windows;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Documents;

namespace RibbonKit.Writer.Services.Persistence;

/// <summary>Versioned native, TXT and RTF persistence for Writer documents.</summary>
public sealed class WriterDocumentPersistence : IWriterDocumentPersistence
{
    public static WriterPersistenceCapabilities GetCapabilities(WriterDocumentFormat format) => format switch
    {
        WriterDocumentFormat.PlainText => new(false, false, false, false,
            "Plain text stores characters only; formatting, images, tables, and page settings are lost."),
        WriterDocumentFormat.RichText => new(true, false, false, false,
            "RTF preserves representative text formatting; advanced content is best effort."),
        WriterDocumentFormat.RibbonKitWriter => new(true, false, false, true,
            "RibbonKit Writer v1 preserves supported text formatting and page settings; images and tables arrive in W3."),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown Writer document format.")
    };

    public async Task<WriterDocument?> LoadAsync(string path, WriterDocumentFormat format,
        CancellationToken cancellationToken)
    {
        Validate(path, format);
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = format == WriterDocumentFormat.RibbonKitWriter
            ? await ReadNativePackageAsync(path, cancellationToken)
            : await File.ReadAllBytesAsync(path, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (format == WriterDocumentFormat.RibbonKitWriter)
        {
            var package = WriterRkwPackage.Load(bytes);
            cancellationToken.ThrowIfCancellationRequested();
            return new WriterDocument(package.Content, path, format, package.PageSettings);
        }

        var content = format switch
        {
            WriterDocumentFormat.PlainText => LoadText(bytes),
            WriterDocumentFormat.RichText => LoadRtf(bytes),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown Writer document format.")
        };
        cancellationToken.ThrowIfCancellationRequested();
        return new WriterDocument(content, path, format);
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
            WriterDocumentFormat.RibbonKitWriter => WriterRkwPackage.Save(document),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown Writer document format.")
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

    private static async Task<byte[]> ReadNativePackageAsync(string path,
        CancellationToken cancellationToken)
    {
        await using var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (source.Length > WriterRkwPackage.MaximumFileBytes)
            throw new InvalidDataException("The native Writer package exceeds the file size limit.");

        using var destination = new MemoryStream((int)source.Length);
        var buffer = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (destination.Length + read > WriterRkwPackage.MaximumFileBytes)
                throw new InvalidDataException("The native Writer package exceeds the file size limit.");
            destination.Write(buffer, 0, read);
        }
        return destination.ToArray();
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
        var extension = Path.GetExtension(path);
        var expected = format switch
        {
            WriterDocumentFormat.PlainText => ".txt",
            WriterDocumentFormat.RichText => ".rtf",
            WriterDocumentFormat.RibbonKitWriter => ".rkw",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown Writer document format.")
        };
        if (!string.Equals(extension, expected, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"The destination must use the {expected} extension.", nameof(path));
    }
}
