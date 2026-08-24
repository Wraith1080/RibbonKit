using System.IO;
using System.IO.Packaging;
using System.Printing;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps.Packaging;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Preview;

/// <summary>Creates isolated, page-aware preview snapshots from trusted live Writer content.</summary>
public sealed class WriterPreviewCloneService
{
    /// <summary>
    /// Clones a trusted live <see cref="FlowDocument"/> through WPF's XamlPackage format and
    /// applies the supplied logical page settings to the clone.
    /// </summary>
    /// <param name="source">The live editor document. It is never modified.</param>
    /// <param name="pageSettings">The logical page settings to apply to the clone.</param>
    /// <returns>A snapshot whose document and paginator are independent of <paramref name="source"/>.</returns>
    public WriterPreviewSnapshot CreateSnapshot(FlowDocument source, DocumentPageSettings pageSettings)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(pageSettings);

        var clone = new FlowDocument();
        using (var xamlPackage = new MemoryStream())
        {
            var sourceRange = new TextRange(source.ContentStart, source.ContentEnd);
            sourceRange.Save(xamlPackage, DataFormats.XamlPackage);
            xamlPackage.Position = 0;
            var cloneRange = new TextRange(clone.ContentStart, clone.ContentEnd);
            cloneRange.Load(xamlPackage, DataFormats.XamlPackage);
        }

        CopyFlowDocumentFormatting(source, clone);
        ApplyPageSettings(clone, pageSettings);

        var flowPaginator = ((IDocumentPaginatorSource)clone).DocumentPaginator;
        flowPaginator.PageSize = new Size(pageSettings.WidthDip, pageSettings.HeightDip);
        flowPaginator.ComputePageCount();

        var backingStream = new MemoryStream();
        var package = Package.Open(backingStream, FileMode.Create, FileAccess.ReadWrite);
        var packageUri = new Uri($"memorystream://ribbonkit.writer/{Guid.NewGuid():N}.xps");
        PackageStore.AddPackage(packageUri, package);
        XpsDocument? xpsDocument = null;
        try
        {
            xpsDocument = new XpsDocument(package, CompressionOption.SuperFast,
                packageUri.AbsoluteUri);
            var serializationTicket = new PrintTicket
            {
                PageMediaSize = new PageMediaSize(pageSettings.PortraitWidthDip,
                    pageSettings.PortraitHeightDip),
                PageOrientation = pageSettings.Orientation == DocumentPageOrientation.Landscape
                    ? PageOrientation.Landscape
                    : PageOrientation.Portrait
            };
            XpsDocument.CreateXpsDocumentWriter(xpsDocument).Write(flowPaginator,
                serializationTicket);
            var fixedDocument = xpsDocument.GetFixedDocumentSequence();
            var paginator = fixedDocument.DocumentPaginator;
            paginator.ComputePageCount();
            return new WriterPreviewSnapshot(clone, paginator, pageSettings,
                xpsDocument, package, backingStream, packageUri, fixedDocument);
        }
        catch
        {
            (xpsDocument as IDisposable)?.Dispose();
            PackageStore.RemovePackage(packageUri);
            package.Close();
            backingStream.Dispose();
            throw;
        }
    }

    private static void CopyFlowDocumentFormatting(FlowDocument source, FlowDocument clone)
    {
        clone.Background = CloneFreezable(source.Background);
        clone.Foreground = CloneFreezable(source.Foreground);
        clone.FontFamily = source.FontFamily;
        clone.FontSize = source.FontSize;
        clone.FontStretch = source.FontStretch;
        clone.FontStyle = source.FontStyle;
        clone.FontWeight = source.FontWeight;
        clone.Language = source.Language;
        clone.FlowDirection = source.FlowDirection;
        clone.TextAlignment = source.TextAlignment;
        clone.LineHeight = source.LineHeight;
        clone.LineStackingStrategy = source.LineStackingStrategy;
        clone.IsHyphenationEnabled = source.IsHyphenationEnabled;
        clone.IsOptimalParagraphEnabled = source.IsOptimalParagraphEnabled;
        clone.TextEffects = CloneFreezable(source.TextEffects);
        clone.ColumnRuleBrush = CloneFreezable(source.ColumnRuleBrush);
        clone.ColumnRuleWidth = source.ColumnRuleWidth;
    }

    private static void ApplyPageSettings(FlowDocument clone, DocumentPageSettings pageSettings)
    {
        var margins = pageSettings.Margins;
        clone.PageWidth = pageSettings.WidthDip;
        clone.PageHeight = pageSettings.HeightDip;
        clone.PagePadding = new Thickness(margins.LeftDip, margins.TopDip,
            margins.RightDip, margins.BottomDip);

        // Match the column to the complete logical content rectangle. WPF's NaN/Auto value can
        // create multiple newspaper-style columns on a sufficiently wide page.
        clone.ColumnWidth = pageSettings.ContentWidthDip;
        clone.ColumnGap = 0;
        clone.IsColumnWidthFlexible = false;
        clone.ColumnRuleWidth = 0;
    }

    private static T? CloneFreezable<T>(T? value) where T : Freezable
    {
        if (value is null)
            return null;
        return (T)value.CloneCurrentValue();
    }
}
