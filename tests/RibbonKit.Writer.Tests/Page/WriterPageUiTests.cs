using System.Globalization;
using System.Windows.Media;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Page;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Page;

public sealed class WriterPageUiTests
{
    [Theory]
    [InlineData(WriterMarginPreset.Normal, 1, 1, 1, 1)]
    [InlineData(WriterMarginPreset.Narrow, 0.5, 0.5, 0.5, 0.5)]
    [InlineData(WriterMarginPreset.Moderate, 0.75, 1, 0.75, 1)]
    [InlineData(WriterMarginPreset.Wide, 2, 1, 2, 1)]
    public void MarginPresetsHaveStablePhysicalEdges(WriterMarginPreset preset,
        double left, double top, double right, double bottom)
    {
        var margins = WriterPageUi.CreateMargins(preset);
        Assert.Equal(left, DocumentLength.DipsToInches(margins.LeftDip), 6);
        Assert.Equal(top, DocumentLength.DipsToInches(margins.TopDip), 6);
        Assert.Equal(right, DocumentLength.DipsToInches(margins.RightDip), 6);
        Assert.Equal(bottom, DocumentLength.DipsToInches(margins.BottomDip), 6);
    }

    [Fact]
    public void CustomMarginsParseAllFourEdgesIntoOneValidatedReplacement()
    {
        var opening = DocumentPageSettings.Letter();

        var valid = WriterPageUi.TryCreateCustomSettings(opening,
            "0.75", "1.25", "0.5", "1.5", CultureInfo.InvariantCulture,
            out var replacement, out var error);

        Assert.True(valid, error);
        Assert.NotNull(replacement);
        Assert.NotSame(opening, replacement);
        Assert.Equal(opening.PaperSize, replacement.PaperSize);
        Assert.Equal(opening.Orientation, replacement.Orientation);
        Assert.Equal(0.75, DocumentLength.DipsToInches(replacement.Margins.TopDip), 6);
        Assert.Equal(1.25, DocumentLength.DipsToInches(replacement.Margins.BottomDip), 6);
        Assert.Equal(0.5, DocumentLength.DipsToInches(replacement.Margins.LeftDip), 6);
        Assert.Equal(1.5, DocumentLength.DipsToInches(replacement.Margins.RightDip), 6);
        Assert.Equal(DocumentPageMargins.Normal, opening.Margins);
    }

    [Theory]
    [InlineData("", "1", "1", "1")]
    [InlineData("-1", "1", "1", "1")]
    [InlineData("NaN", "1", "1", "1")]
    [InlineData("1", "1", "5", "5")]
    public void InvalidCustomMarginsCannotProduceAReplacement(
        string top, string bottom, string left, string right)
    {
        var opening = DocumentPageSettings.Letter();

        Assert.False(WriterPageUi.TryCreateCustomSettings(opening,
            top, bottom, left, right, CultureInfo.InvariantCulture,
            out var replacement, out var error));
        Assert.Null(replacement);
        Assert.False(string.IsNullOrWhiteSpace(error));
        Assert.Equal(DocumentPageMargins.Normal, opening.Margins);
    }

    [Fact]
    public void DialogConstructionAndCancelDoNotMutateOpeningSettings()
    {
        StaTestHelper.Run(() =>
        {
            var opening = DocumentPageSettings.A4(DocumentPageOrientation.Landscape,
                new DocumentPageMargins(30, 40, 50, 60));
            var dialog = new WriterCustomMarginsDialog(opening);

            Assert.Null(dialog.ResultSettings);
            Assert.Equal(new DocumentPageMargins(30, 40, 50, 60), opening.Margins);
            var pagePreview = Assert.IsType<System.Windows.Controls.Border>(
                dialog.FindName("PagePreview"));
            Assert.True(pagePreview.Width > pagePreview.Height);
            dialog.Close();
        });
    }

    [Fact]
    public void PageSummaryIncludesLogicalSettingsPagesAndColour()
    {
        var summary = WriterPageUi.FormatSummary(DocumentPageSettings.A4(
            DocumentPageOrientation.Landscape), 2,
            new SolidColorBrush(Color.FromRgb(255, 253, 240)));

        Assert.Contains("A4 · Landscape · 2 pages", summary);
        Assert.Contains("Margins (in):", summary);
        Assert.Contains("Page colour: Ivory", summary);
    }
}
