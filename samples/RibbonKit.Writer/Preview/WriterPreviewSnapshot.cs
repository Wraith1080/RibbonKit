using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.IO.Packaging;
using System.Windows.Xps.Packaging;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Preview;

/// <summary>
/// An immutable-at-the-boundary preview document and its eagerly materialized page layout.
/// </summary>
/// <remarks>
/// The document is a clone of the live editor document. Its flow paginator is serialized before
/// any viewer can access pages, producing stable fixed pages for preview. Printing uses the same
/// isolated flow clone so bitmap resources are serialized only once into the device spool.
/// </remarks>
public sealed class WriterPreviewSnapshot : IDisposable
{
    internal WriterPreviewSnapshot(FlowDocument document, DocumentPaginator paginator,
        DocumentPaginator printPaginator,
        DocumentPageSettings pageSettings, XpsDocument xpsDocument, Package package,
        Stream backingStream, Uri packageUri, FixedDocumentSequence fixedDocument)
    {
        SourceClone = document ?? throw new ArgumentNullException(nameof(document));
        Paginator = paginator ?? throw new ArgumentNullException(nameof(paginator));
        PrintPaginator = printPaginator ?? throw new ArgumentNullException(nameof(printPaginator));
        PageSettings = pageSettings ?? throw new ArgumentNullException(nameof(pageSettings));
        _xpsDocument = xpsDocument ?? throw new ArgumentNullException(nameof(xpsDocument));
        _package = package ?? throw new ArgumentNullException(nameof(package));
        _backingStream = backingStream ?? throw new ArgumentNullException(nameof(backingStream));
        _packageUri = packageUri ?? throw new ArgumentNullException(nameof(packageUri));
        Document = fixedDocument ?? throw new ArgumentNullException(nameof(fixedDocument));
    }

    private readonly XpsDocument _xpsDocument;
    private readonly Package _package;
    private readonly Stream _backingStream;
    private readonly Uri _packageUri;
    private bool _disposed;

    /// <summary>Gets the stable fixed document consumed by preview.</summary>
    public FixedDocumentSequence Document { get; }

    /// <summary>Gets the isolated flow clone used only to create the fixed document.</summary>
    internal FlowDocument SourceClone { get; }

    /// <summary>Gets the stable fixed paginator consumed by preview.</summary>
    public DocumentPaginator Paginator { get; }

    /// <summary>Gets the isolated flow paginator submitted to the printer.</summary>
    internal DocumentPaginator PrintPaginator { get; }

    /// <summary>Gets the logical page settings applied to the snapshot.</summary>
    public DocumentPageSettings PageSettings { get; }

    /// <summary>Gets the logical page size in device-independent pixels.</summary>
    public Size PageSize => new(PageSettings.WidthDip, PageSettings.HeightDip);

    /// <summary>Gets the logical content width after margins.</summary>
    public double ContentWidthDip => PageSettings.ContentWidthDip;

    /// <summary>Gets the logical content height after margins.</summary>
    public double ContentHeightDip => PageSettings.ContentHeightDip;

    /// <summary>Releases the in-memory fixed-layout package after snapshot consumers release it.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        try
        {
            ((IDisposable)_xpsDocument).Dispose();
        }
        finally
        {
            PackageStore.RemovePackage(_packageUri);
            _package.Close();
            _backingStream.Dispose();
        }
    }
}
