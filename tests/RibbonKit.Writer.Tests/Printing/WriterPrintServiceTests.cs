using System.Windows.Documents;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Printing;
using RibbonKit.Writer.Tests.Document;
using RibbonKit.Writer.Tests.Preview;
using Xunit;

namespace RibbonKit.Writer.Tests.Printing;

[Collection(WriterPreviewTestCollection.Name)]
public sealed class WriterPrintServiceTests
{
    [Fact]
    public void AnalysisReportsEveryConflictingEdge()
    {
        StaTestHelper.Run(() =>
        {
            var settings = DocumentPageSettings.Letter(DocumentPageOrientation.Portrait,
                new DocumentPageMargins(20, 30, 40, 50));
            var snapshot = new WriterPreviewCloneService().CreateSnapshot(
                new FlowDocument(new Paragraph(new Run("print"))), settings);
            var capabilities = WriterPrintDeviceCapabilities.Create(
                settings.WidthDip, settings.HeightDip,
                32, 42, settings.WidthDip - 74, settings.HeightDip - 94);

            var analysis = new WriterPrintService().Analyze(snapshot, capabilities);

            Assert.True(analysis.HasConflicts);
            Assert.Equal(WriterPrintConflictEdges.Left | WriterPrintConflictEdges.Top |
                WriterPrintConflictEdges.Right | WriterPrintConflictEdges.Bottom,
                analysis.ConflictingEdges);
            Assert.Equal(4, analysis.Conflicts.Count);
            Assert.Equal(new[] { WriterPrintConflictEdges.Left, WriterPrintConflictEdges.Top,
                WriterPrintConflictEdges.Right, WriterPrintConflictEdges.Bottom },
                analysis.Conflicts.Select(conflict => conflict.Edge));
            Assert.Equal(12, analysis.Conflicts.Single(conflict => conflict.Edge == WriterPrintConflictEdges.Left).ClippingDip);
            Assert.Equal(2, analysis.Conflicts.Single(conflict => conflict.Edge == WriterPrintConflictEdges.Right).ClippingDip);
        });
    }

    [Fact]
    public void ReportOnlySubmitsExactPaginatorWithoutChangingLogicalMargins()
    {
        StaTestHelper.Run(() =>
        {
            var settings = DocumentPageSettings.A4();
            var liveDocument = new FlowDocument(new Paragraph(new Run("print")));
            var writerDocument = new WriterDocument(liveDocument, pageSettings: settings);
            writerDocument.MarkClean();
            var snapshot = new WriterPreviewCloneService().CreateSnapshot(
                liveDocument, settings);
            var device = new RecordingPrintDevice(WriterPrintDeviceCapabilities.Create(
                settings.WidthDip, settings.HeightDip, 72, 72,
                settings.WidthDip - 144, settings.HeightDip - 144));

            var result = new WriterPrintService().Print(snapshot, device);

            Assert.True(result.Submitted);
            Assert.Same(snapshot.Paginator, device.Paginator);
            Assert.Same(snapshot.Paginator, result.Paginator);
            Assert.Equal(settings, snapshot.PageSettings);
            Assert.Equal(settings.Margins, snapshot.PageSettings.Margins);
            Assert.False(writerDocument.IsDirty);
            Assert.Equal(settings, writerDocument.PageSettings);
            Assert.Same(liveDocument, writerDocument.Content);
        });
    }

    [Fact]
    public void RejectPolicyReportsConflictsAndDoesNotSubmit()
    {
        StaTestHelper.Run(() =>
        {
            var settings = DocumentPageSettings.Letter();
            var snapshot = new WriterPreviewCloneService().CreateSnapshot(
                new FlowDocument(new Paragraph(new Run("print"))), settings);
            var device = new RecordingPrintDevice(WriterPrintDeviceCapabilities.Create(
                settings.WidthDip, settings.HeightDip, 120, 120,
                settings.WidthDip - 240, settings.HeightDip - 240));

            var result = new WriterPrintService().Print(snapshot, device,
                new WriterPrintOptions { ConflictBehavior = WriterPrintConflictBehavior.Reject });

            Assert.False(result.Submitted);
            Assert.Null(device.Paginator);
            Assert.True(result.Analysis.HasConflicts);
        });
    }

    [Fact]
    public void PageSizeMismatchIsExplicitAndRejectPolicyDoesNotSubmit()
    {
        StaTestHelper.Run(() =>
        {
            var settings = DocumentPageSettings.A4();
            var snapshot = new WriterPreviewCloneService().CreateSnapshot(
                new FlowDocument(new Paragraph(new Run("A4 preview"))), settings);
            var letter = DocumentPageSettings.Letter();
            var device = new RecordingPrintDevice(new WriterPrintDeviceCapabilities(
                letter.WidthDip, letter.HeightDip, 0, 0, letter.WidthDip, letter.HeightDip));

            var result = new WriterPrintService().Print(snapshot, device,
                new WriterPrintOptions { ConflictBehavior = WriterPrintConflictBehavior.Reject });

            Assert.False(result.Submitted);
            Assert.NotNull(result.Analysis.PageSizeMismatch);
            Assert.True(result.Analysis.ConflictingEdges.HasFlag(WriterPrintConflictEdges.PageSize));
            Assert.Null(device.Paginator);
        });
    }

    [Fact]
    public void CapabilitiesRejectAnImageableRectangleOutsideThePage()
    {
        Assert.Throws<ArgumentException>(() => new WriterPrintDeviceCapabilities(
            100, 100, 10, 10, 91, 80));
    }

    [Fact]
    public void DefaultCapabilitiesCannotBypassValidation()
    {
        StaTestHelper.Run(() =>
        {
            var snapshot = new WriterPreviewCloneService().CreateSnapshot(
                new FlowDocument(new Paragraph(new Run("print"))), DocumentPageSettings.Letter());
            WriterPrintDeviceCapabilities? invalidCapabilities =
                new WriterPrintDeviceCapabilities();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new WriterPrintService().Analyze(snapshot, invalidCapabilities));
        });
    }

    [Fact]
    public void MissingCapabilitiesAreReportedAndRejectPolicyDoesNotSubmit()
    {
        StaTestHelper.Run(() =>
        {
            var snapshot = new WriterPreviewCloneService().CreateSnapshot(
                new FlowDocument(new Paragraph(new Run("print"))), DocumentPageSettings.Letter());
            var device = new RecordingPrintDevice(null);

            var result = new WriterPrintService().Print(snapshot, device,
                new WriterPrintOptions { ConflictBehavior = WriterPrintConflictBehavior.Reject });

            Assert.False(result.Submitted);
            Assert.False(result.Analysis.AreCapabilitiesAvailable);
            Assert.True(result.Analysis.ConflictingEdges.HasFlag(
                WriterPrintConflictEdges.CapabilitiesUnavailable));
            Assert.Null(device.Paginator);
        });
    }

    private sealed class RecordingPrintDevice(WriterPrintDeviceCapabilities? capabilities) : IWriterPrintDevice
    {
        public WriterPrintDeviceCapabilities? Capabilities { get; } = capabilities;
        public DocumentPaginator? Paginator { get; private set; }
        public string? DocumentName { get; private set; }

        public void Submit(DocumentPaginator paginator, string documentName)
        {
            Paginator = paginator;
            DocumentName = documentName;
        }
    }
}
