namespace RibbonKit.Writer.Models;

/// <summary>Named paper sizes supported by RibbonKit Writer.</summary>
public enum DocumentPaperSize
{
    Custom = 0,
    A4 = 1,
    Letter = 2,
    Legal = 3
}

/// <summary>The logical orientation of a Writer document page.</summary>
public enum DocumentPageOrientation
{
    Portrait = 0,
    Landscape = 1
}

/// <summary>Logical page margins expressed in device-independent pixels.</summary>
public readonly record struct DocumentPageMargins(
    double LeftDip,
    double TopDip,
    double RightDip,
    double BottomDip)
{
    /// <summary>One-inch margins on every side.</summary>
    public static DocumentPageMargins Normal => new(
        DocumentLength.InchesToDips(1),
        DocumentLength.InchesToDips(1),
        DocumentLength.InchesToDips(1),
        DocumentLength.InchesToDips(1));

    /// <summary>No logical page margins.</summary>
    public static DocumentPageMargins None => default;
}

/// <summary>Converts physical document lengths to and from WPF device-independent pixels.</summary>
public static class DocumentLength
{
    public const double DipsPerInch = 96d;
    public const double MillimetersPerInch = 25.4d;

    public static double InchesToDips(double inches) =>
        Convert(inches, DipsPerInch, nameof(inches));

    public static double DipsToInches(double dips) =>
        Validate(dips, nameof(dips)) / DipsPerInch;

    public static double MillimetersToDips(double millimeters) =>
        Convert(millimeters, DipsPerInch / MillimetersPerInch, nameof(millimeters));

    public static double DipsToMillimeters(double dips) =>
        Validate(dips, nameof(dips)) * MillimetersPerInch / DipsPerInch;

    private static double Validate(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(parameterName, value,
                "Document lengths must be finite and non-negative.");
        return value;
    }

    private static double Convert(double value, double factor, string parameterName)
    {
        var result = Validate(value, parameterName) * factor;
        if (!double.IsFinite(result))
            throw new ArgumentOutOfRangeException(parameterName, value,
                "The converted document length must be finite.");
        return result;
    }
}

/// <summary>
/// Immutable logical paper dimensions, orientation and margins for a Writer document.
/// Dimensions are stored in their portrait basis so repeated orientation changes cannot accumulate drift.
/// </summary>
public sealed record DocumentPageSettings
{
    private DocumentPageSettings(
        DocumentPaperSize paperSize,
        double portraitWidthDip,
        double portraitHeightDip,
        DocumentPageOrientation orientation,
        DocumentPageMargins margins)
    {
        ValidatePaperSize(paperSize);
        ValidateOrientation(orientation);
        ValidateDimension(portraitWidthDip, nameof(portraitWidthDip));
        ValidateDimension(portraitHeightDip, nameof(portraitHeightDip));
        if (portraitWidthDip > portraitHeightDip)
            throw new ArgumentException("Portrait-basis width cannot exceed portrait-basis height.",
                nameof(portraitWidthDip));

        PaperSize = paperSize;
        PortraitWidthDip = portraitWidthDip;
        PortraitHeightDip = portraitHeightDip;
        Orientation = orientation;
        ValidateMargins(margins, WidthDip, HeightDip);
        Margins = margins;
    }

    public DocumentPaperSize PaperSize { get; }

    public DocumentPageOrientation Orientation { get; }

    public double PortraitWidthDip { get; }

    public double PortraitHeightDip { get; }

    public double WidthDip => Orientation == DocumentPageOrientation.Portrait
        ? PortraitWidthDip
        : PortraitHeightDip;

    public double HeightDip => Orientation == DocumentPageOrientation.Portrait
        ? PortraitHeightDip
        : PortraitWidthDip;

    public DocumentPageMargins Margins { get; }

    public double ContentWidthDip => WidthDip - Margins.LeftDip - Margins.RightDip;

    public double ContentHeightDip => HeightDip - Margins.TopDip - Margins.BottomDip;

    public static DocumentPageSettings A4(
        DocumentPageOrientation orientation = DocumentPageOrientation.Portrait,
        DocumentPageMargins? margins = null) =>
        CreatePreset(DocumentPaperSize.A4, orientation, margins);

    public static DocumentPageSettings Letter(
        DocumentPageOrientation orientation = DocumentPageOrientation.Portrait,
        DocumentPageMargins? margins = null) =>
        CreatePreset(DocumentPaperSize.Letter, orientation, margins);

    public static DocumentPageSettings Legal(
        DocumentPageOrientation orientation = DocumentPageOrientation.Portrait,
        DocumentPageMargins? margins = null) =>
        CreatePreset(DocumentPaperSize.Legal, orientation, margins);

    public static DocumentPageSettings CreatePreset(
        DocumentPaperSize paperSize,
        DocumentPageOrientation orientation = DocumentPageOrientation.Portrait,
        DocumentPageMargins? margins = null)
    {
        var (widthDip, heightDip) = paperSize switch
        {
            DocumentPaperSize.A4 => (
                DocumentLength.MillimetersToDips(210),
                DocumentLength.MillimetersToDips(297)),
            DocumentPaperSize.Letter => (
                DocumentLength.InchesToDips(8.5),
                DocumentLength.InchesToDips(11)),
            DocumentPaperSize.Legal => (
                DocumentLength.InchesToDips(8.5),
                DocumentLength.InchesToDips(14)),
            DocumentPaperSize.Custom => throw new ArgumentException(
                "Custom paper requires explicit dimensions.", nameof(paperSize)),
            _ => throw new ArgumentOutOfRangeException(nameof(paperSize), paperSize,
                "Unknown document paper size.")
        };

        return new DocumentPageSettings(paperSize, widthDip, heightDip, orientation,
            margins ?? DocumentPageMargins.Normal);
    }

    public static DocumentPageSettings CreateCustom(
        double portraitWidthDip,
        double portraitHeightDip,
        DocumentPageOrientation orientation = DocumentPageOrientation.Portrait,
        DocumentPageMargins? margins = null) =>
        new(DocumentPaperSize.Custom, portraitWidthDip, portraitHeightDip, orientation,
            margins ?? DocumentPageMargins.Normal);

    public DocumentPageSettings WithOrientation(DocumentPageOrientation orientation)
    {
        ValidateOrientation(orientation);
        return orientation == Orientation
            ? this
            : new DocumentPageSettings(PaperSize, PortraitWidthDip, PortraitHeightDip, orientation, Margins);
    }

    public DocumentPageSettings ToggleOrientation() => WithOrientation(
        Orientation == DocumentPageOrientation.Portrait
            ? DocumentPageOrientation.Landscape
            : DocumentPageOrientation.Portrait);

    public DocumentPageSettings WithMargins(DocumentPageMargins margins) =>
        margins == Margins
            ? this
            : new DocumentPageSettings(PaperSize, PortraitWidthDip, PortraitHeightDip, Orientation, margins);

    public DocumentPageSettings WithPreset(DocumentPaperSize paperSize) =>
        paperSize == PaperSize && paperSize != DocumentPaperSize.Custom
            ? this
            : CreatePreset(paperSize, Orientation, Margins);

    public DocumentPageSettings WithCustomDimensions(double portraitWidthDip, double portraitHeightDip) =>
        new(DocumentPaperSize.Custom, portraitWidthDip, portraitHeightDip, Orientation, Margins);

    private static void ValidateDimension(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(parameterName, value,
                "Page dimensions must be finite and greater than zero.");
    }

    private static void ValidateMargins(DocumentPageMargins margins, double widthDip, double heightDip)
    {
        ValidateMargin(margins.LeftDip, nameof(margins.LeftDip));
        ValidateMargin(margins.TopDip, nameof(margins.TopDip));
        ValidateMargin(margins.RightDip, nameof(margins.RightDip));
        ValidateMargin(margins.BottomDip, nameof(margins.BottomDip));

        if (margins.LeftDip + margins.RightDip >= widthDip)
            throw new ArgumentException("Horizontal margins must leave positive content width.", nameof(margins));
        if (margins.TopDip + margins.BottomDip >= heightDip)
            throw new ArgumentException("Vertical margins must leave positive content height.", nameof(margins));
    }

    private static void ValidateMargin(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(parameterName, value,
                "Page margins must be finite and non-negative.");
    }

    private static void ValidatePaperSize(DocumentPaperSize paperSize)
    {
        if (!Enum.IsDefined(paperSize))
            throw new ArgumentOutOfRangeException(nameof(paperSize), paperSize,
                "Unknown document paper size.");
    }

    private static void ValidateOrientation(DocumentPageOrientation orientation)
    {
        if (!Enum.IsDefined(orientation))
            throw new ArgumentOutOfRangeException(nameof(orientation), orientation,
                "Unknown document page orientation.");
    }
}
