using System.Windows;
using System.Windows.Documents;

namespace RibbonKit.Writer.Editing;

/// <summary>Strikethrough effect supported by Writer's Font dialog.</summary>
public enum WriterStrikethroughStyle
{
    /// <summary>No strikethrough line.</summary>
    None,

    /// <summary>One line through the text.</summary>
    Single,

    /// <summary>Two lines through the text.</summary>
    Double
}

/// <summary>Vertical character position supported by Writer's Font dialog.</summary>
public enum WriterBaselineEffect
{
    /// <summary>Normal baseline positioning.</summary>
    Normal,

    /// <summary>Raised superscript positioning.</summary>
    Superscript,

    /// <summary>Lowered subscript positioning.</summary>
    Subscript
}

/// <summary>Creates and recognizes Writer's supported text-decoration combinations.</summary>
internal static class WriterFontEffects
{
    public static TextDecorationCollection? CreateDecorations(
        bool underline,
        WriterStrikethroughStyle strikethrough)
    {
        if (!Enum.IsDefined(strikethrough))
            throw new ArgumentOutOfRangeException(nameof(strikethrough));

        var decorations = new TextDecorationCollection();
        if (underline)
            decorations.Add(TextDecorations.Underline[0]);
        if (strikethrough is WriterStrikethroughStyle.Single or WriterStrikethroughStyle.Double)
            decorations.Add(TextDecorations.Strikethrough[0]);
        if (strikethrough == WriterStrikethroughStyle.Double)
        {
            // WPF's predefined Baseline decoration gives the second persisted line. Using only
            // predefined decorations keeps XamlPackage/RTF conversion deterministic and safe.
            decorations.Add(TextDecorations.Baseline[0]);
        }

        return decorations.Count == 0 ? null : decorations;
    }

    public static WriterStrikethroughStyle ReadStrikethrough(TextDecorationCollection? decorations)
    {
        if (decorations is null)
            return WriterStrikethroughStyle.None;
        var hasStrike = decorations.Any(decoration =>
            decoration.Location == TextDecorationLocation.Strikethrough);
        if (!hasStrike)
            return WriterStrikethroughStyle.None;
        var hasSecondLine = decorations.Count(decoration =>
                                decoration.Location == TextDecorationLocation.Strikethrough) > 1 ||
                            decorations.Any(decoration =>
                                decoration.Location == TextDecorationLocation.Baseline);
        return hasSecondLine ? WriterStrikethroughStyle.Double : WriterStrikethroughStyle.Single;
    }

    public static BaselineAlignment ToBaselineAlignment(WriterBaselineEffect effect) => effect switch
    {
        WriterBaselineEffect.Normal => BaselineAlignment.Baseline,
        WriterBaselineEffect.Superscript => BaselineAlignment.Superscript,
        WriterBaselineEffect.Subscript => BaselineAlignment.Subscript,
        _ => throw new ArgumentOutOfRangeException(nameof(effect))
    };

    public static bool TryReadBaselineEffect(object? value, out WriterBaselineEffect effect)
    {
        effect = value switch
        {
            BaselineAlignment.Baseline => WriterBaselineEffect.Normal,
            BaselineAlignment.Superscript => WriterBaselineEffect.Superscript,
            BaselineAlignment.Subscript => WriterBaselineEffect.Subscript,
            _ => default
        };
        return value is BaselineAlignment.Baseline or
            BaselineAlignment.Superscript or BaselineAlignment.Subscript;
    }
}
