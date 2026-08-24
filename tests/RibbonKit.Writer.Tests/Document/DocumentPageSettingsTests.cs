using RibbonKit.Writer.Models;
using Xunit;

namespace RibbonKit.Writer.Tests.Document;

public sealed class DocumentPageSettingsTests
{
    [Fact]
    public void NamedPresetsUseNinetySixDipPhysicalDimensions()
    {
        var a4 = DocumentPageSettings.A4();
        Assert.Equal(DocumentPaperSize.A4, a4.PaperSize);
        AssertClose(DocumentLength.MillimetersToDips(210), a4.WidthDip);
        AssertClose(DocumentLength.MillimetersToDips(297), a4.HeightDip);

        var letter = DocumentPageSettings.Letter();
        Assert.Equal(DocumentPaperSize.Letter, letter.PaperSize);
        Assert.Equal(816, letter.WidthDip);
        Assert.Equal(1056, letter.HeightDip);

        var legal = DocumentPageSettings.Legal();
        Assert.Equal(DocumentPaperSize.Legal, legal.PaperSize);
        Assert.Equal(816, legal.WidthDip);
        Assert.Equal(1344, legal.HeightDip);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.125)]
    [InlineData(1)]
    [InlineData(8.5)]
    [InlineData(14)]
    public void InchConversionsRoundTrip(double inches)
    {
        AssertClose(inches, DocumentLength.DipsToInches(DocumentLength.InchesToDips(inches)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(25.4)]
    [InlineData(210)]
    [InlineData(297)]
    public void MillimeterConversionsRoundTrip(double millimeters)
    {
        AssertClose(millimeters,
            DocumentLength.DipsToMillimeters(DocumentLength.MillimetersToDips(millimeters)));
    }

    [Fact]
    public void OrientationUsesCanonicalDimensionsWithoutCumulativeDrift()
    {
        var original = DocumentPageSettings.A4();
        var settings = original;

        for (var index = 0; index < 1000; index++)
            settings = settings.ToggleOrientation();

        Assert.Equal(original, settings);
        var landscape = original.WithOrientation(DocumentPageOrientation.Landscape);
        Assert.Equal(original.PortraitHeightDip, landscape.WidthDip);
        Assert.Equal(original.PortraitWidthDip, landscape.HeightDip);
        Assert.Same(landscape, landscape.WithOrientation(DocumentPageOrientation.Landscape));
    }

    [Fact]
    public void CustomDimensionsRemainPortraitBasisAcrossOrientationChanges()
    {
        var margins = new DocumentPageMargins(30, 40, 50, 60);
        var custom = DocumentPageSettings.CreateCustom(700, 1000,
            DocumentPageOrientation.Landscape, margins);

        Assert.Equal(DocumentPaperSize.Custom, custom.PaperSize);
        Assert.Equal(1000, custom.WidthDip);
        Assert.Equal(700, custom.HeightDip);
        Assert.Equal(920, custom.ContentWidthDip);
        Assert.Equal(600, custom.ContentHeightDip);

        var portrait = custom.WithOrientation(DocumentPageOrientation.Portrait);
        Assert.Equal(700, portrait.WidthDip);
        Assert.Equal(1000, portrait.HeightDip);
        Assert.Equal(margins, portrait.Margins);
    }

    [Fact]
    public void PresetAndMarginUpdatesAreImmutable()
    {
        var original = DocumentPageSettings.Letter();
        var margins = new DocumentPageMargins(48, 72, 48, 72);
        var updated = original.WithMargins(margins).WithPreset(DocumentPaperSize.Legal);

        Assert.NotSame(original, updated);
        Assert.Equal(DocumentPaperSize.Letter, original.PaperSize);
        Assert.Equal(DocumentPageMargins.Normal, original.Margins);
        Assert.Equal(DocumentPaperSize.Legal, updated.PaperSize);
        Assert.Equal(margins, updated.Margins);
        Assert.Same(updated, updated.WithMargins(margins));
        Assert.Same(updated, updated.WithPreset(DocumentPaperSize.Legal));
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(-1, 100)]
    [InlineData(100, 0)]
    [InlineData(100, -1)]
    [InlineData(double.NaN, 100)]
    [InlineData(100, double.PositiveInfinity)]
    [InlineData(100, 99)]
    public void InvalidCustomDimensionsAreRejected(double widthDip, double heightDip)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            DocumentPageSettings.CreateCustom(widthDip, heightDip, margins: DocumentPageMargins.None));
    }

    [Fact]
    public void InvalidMarginsAreRejectedWithoutMutatingOriginal()
    {
        var original = DocumentPageSettings.CreateCustom(600, 900, margins: DocumentPageMargins.None);
        var invalidMargins = new[]
        {
            new DocumentPageMargins(-1, 0, 0, 0),
            new DocumentPageMargins(0, double.NaN, 0, 0),
            new DocumentPageMargins(300, 0, 300, 0),
            new DocumentPageMargins(0, 450, 0, 450),
            new DocumentPageMargins(double.PositiveInfinity, 0, 0, 0)
        };

        foreach (var margins in invalidMargins)
            Assert.ThrowsAny<ArgumentException>(() => original.WithMargins(margins));

        Assert.Equal(DocumentPageMargins.None, original.Margins);
        Assert.Equal(600, original.ContentWidthDip);
        Assert.Equal(900, original.ContentHeightDip);
    }

    [Fact]
    public void OrientationChangeRejectsMarginsThatWouldConsumeRotatedPage()
    {
        var portrait = DocumentPageSettings.CreateCustom(600, 1000,
            margins: new DocumentPageMargins(0, 350, 0, 350));

        Assert.Throws<ArgumentException>(() =>
            portrait.WithOrientation(DocumentPageOrientation.Landscape));
        Assert.Equal(DocumentPageOrientation.Portrait, portrait.Orientation);
        Assert.Equal(1000, portrait.HeightDip);
    }

    [Fact]
    public void InvalidEnumValuesAndCustomPresetRequestsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DocumentPageSettings.CreatePreset((DocumentPaperSize)99));
        Assert.Throws<ArgumentException>(() =>
            DocumentPageSettings.CreatePreset(DocumentPaperSize.Custom));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DocumentPageSettings.Letter((DocumentPageOrientation)99));
        Assert.Throws<ArgumentException>(() =>
            DocumentPageSettings.Letter().WithPreset(DocumentPaperSize.Custom));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-1)]
    public void ConversionHelpersRejectInvalidLengths(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DocumentLength.InchesToDips(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => DocumentLength.DipsToInches(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => DocumentLength.MillimetersToDips(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => DocumentLength.DipsToMillimeters(value));
    }

    [Fact]
    public void ConversionsRejectFiniteInputsThatWouldOverflow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DocumentLength.InchesToDips(double.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => DocumentLength.MillimetersToDips(double.MaxValue));
    }

    private static void AssertClose(double expected, double actual) =>
        Assert.InRange(actual, expected - 0.000000001, expected + 0.000000001);
}
