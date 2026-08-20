using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace RibbonKit.Writer.Editing;

/// <summary>Describes whether one selection-sensitive value is uniform, mixed, unavailable, or unsupported.</summary>
public enum WriterSelectionValueKind
{
    /// <summary>No text or paragraph context exposes this value.</summary>
    Unset,

    /// <summary>Every inspected position exposes the same value.</summary>
    Uniform,

    /// <summary>The inspected selection contains more than one value.</summary>
    Mixed,

    /// <summary>The source contains a value that this state projection cannot represent.</summary>
    /// <remarks>
    /// Writer exposes solid colours through this adapter. An imported gradient or other unsupported
    /// brush remains in the document and is reported as unsupported rather than being misrepresented
    /// as one uniform solid colour.
    /// </remarks>
    Unsupported
}

/// <summary>A value together with its selection-state meaning.</summary>
/// <typeparam name="T">The type of the uniform value.</typeparam>
public readonly record struct WriterSelectionValue<T>
{
    private WriterSelectionValue(WriterSelectionValueKind kind, T value)
    {
        Kind = kind;
        Value = value;
    }

    /// <summary>Gets whether the value is unavailable, uniform, mixed, or unsupported.</summary>
    public WriterSelectionValueKind Kind { get; }

    /// <summary>
    /// Gets the uniform value. For <see cref="WriterSelectionValueKind.Unset"/>,
    /// <see cref="WriterSelectionValueKind.Mixed"/>, or
    /// <see cref="WriterSelectionValueKind.Unsupported"/>, this is the default value for
    /// <typeparamref name="T"/>.
    /// </summary>
    public T Value { get; }

    /// <summary>Gets whether the value is available and uniform.</summary>
    public bool IsUniform => Kind == WriterSelectionValueKind.Uniform;

    /// <summary>Gets whether the value is mixed.</summary>
    public bool IsMixed => Kind == WriterSelectionValueKind.Mixed;

    /// <summary>Gets whether the source value is outside this state projection's supported domain.</summary>
    public bool IsUnsupported => Kind == WriterSelectionValueKind.Unsupported;

    /// <summary>Gets whether the value is unavailable.</summary>
    public bool IsUnset => Kind == WriterSelectionValueKind.Unset;

    /// <summary>Creates an unavailable value.</summary>
    public static WriterSelectionValue<T> Unset() => new(WriterSelectionValueKind.Unset, default!);

    /// <summary>Creates a uniform value, including a deliberate <see langword="null"/> value.</summary>
    public static WriterSelectionValue<T> Uniform(T value) => new(WriterSelectionValueKind.Uniform, value);

    /// <summary>Creates a mixed value without coercing it to one of its members.</summary>
    public static WriterSelectionValue<T> Mixed() => new(WriterSelectionValueKind.Mixed, default!);

    /// <summary>Creates a value that is present but cannot be represented by the projected type.</summary>
    public static WriterSelectionValue<T> Unsupported() => new(WriterSelectionValueKind.Unsupported, default!);

    /// <summary>Copies the value only when it is uniform.</summary>
    public bool TryGetValue(out T value)
    {
        value = Value;
        return IsUniform;
    }
}

/// <summary>List state observed from the selected paragraphs.</summary>
public enum WriterListKind
{
    /// <summary>The paragraphs are not in a list.</summary>
    None,

    /// <summary>The paragraphs use a bullet list.</summary>
    Bulleted,

    /// <summary>The paragraphs use a numbered list.</summary>
    Numbered
}

/// <summary>Immutable selection-sensitive state exposed by <see cref="WriterEditingAdapter"/>.</summary>
public sealed class WriterEditingState
{
    internal WriterEditingState(
        bool hasSelection,
        bool hasTextContext,
        bool isEnabled,
        bool isReadOnly,
        bool canFormat,
        bool canCopy,
        bool canCut,
        bool canPaste,
        bool canSelectAll,
        bool canUndo,
        bool canRedo,
        WriterSelectionValue<FontFamily> fontFamily,
        WriterSelectionValue<double> fontSize,
        WriterSelectionValue<bool> bold,
        WriterSelectionValue<bool> italic,
        WriterSelectionValue<bool> underline,
        WriterSelectionValue<Color?> foreground,
        WriterSelectionValue<Color?> highlight,
        WriterSelectionValue<TextAlignment> alignment,
        WriterSelectionValue<double> indentation,
        WriterSelectionValue<double> spacingBefore,
        WriterSelectionValue<double> spacingAfter,
        WriterSelectionValue<WriterListKind> listKind)
    {
        HasSelection = hasSelection;
        HasTextContext = hasTextContext;
        IsEnabled = isEnabled;
        IsReadOnly = isReadOnly;
        CanFormat = canFormat;
        CanCopy = canCopy;
        CanCut = canCut;
        CanPaste = canPaste;
        CanSelectAll = canSelectAll;
        CanUndo = canUndo;
        CanRedo = canRedo;
        FontFamily = fontFamily;
        FontSize = fontSize;
        Bold = bold;
        Italic = italic;
        Underline = underline;
        Foreground = foreground;
        Highlight = highlight;
        Alignment = alignment;
        Indentation = indentation;
        SpacingBefore = spacingBefore;
        SpacingAfter = spacingAfter;
        ListKind = listKind;
    }

    /// <summary>Gets whether the user selected one or more document positions.</summary>
    public bool HasSelection { get; }

    /// <summary>Gets whether a non-empty text or paragraph context was inspected.</summary>
    public bool HasTextContext { get; }

    /// <summary>Gets whether the native editor is enabled.</summary>
    public bool IsEnabled { get; }

    /// <summary>Gets whether the native editor is read-only.</summary>
    public bool IsReadOnly { get; }

    /// <summary>Gets whether formatting or other document mutations are currently available.</summary>
    public bool CanFormat { get; }

    /// <summary>Gets whether Copy is currently available.</summary>
    public bool CanCopy { get; }

    /// <summary>Gets whether Cut is currently available.</summary>
    public bool CanCut { get; }

    /// <summary>Gets whether Paste is currently available from the system clipboard.</summary>
    public bool CanPaste { get; }

    /// <summary>Gets whether Select All is currently available.</summary>
    public bool CanSelectAll { get; }

    /// <summary>Gets whether the native editor can undo.</summary>
    public bool CanUndo { get; }

    /// <summary>Gets whether the native editor can redo.</summary>
    public bool CanRedo { get; }

    /// <summary>Gets the selected font family state.</summary>
    public WriterSelectionValue<FontFamily> FontFamily { get; }

    /// <summary>Gets the selected font-size state in device-independent units.</summary>
    public WriterSelectionValue<double> FontSize { get; }

    /// <summary>Gets the selected bold state.</summary>
    public WriterSelectionValue<bool> Bold { get; }

    /// <summary>Gets the selected italic state.</summary>
    public WriterSelectionValue<bool> Italic { get; }

    /// <summary>Gets the selected underline state.</summary>
    public WriterSelectionValue<bool> Underline { get; }

    /// <summary>Gets the selected foreground colour, or a non-uniform state for mixed/unsupported brushes.</summary>
    public WriterSelectionValue<Color?> Foreground { get; }

    /// <summary>Gets the selected highlight colour, or a non-uniform state for mixed/unsupported brushes.</summary>
    public WriterSelectionValue<Color?> Highlight { get; }

    /// <summary>Gets the selected paragraph-alignment state.</summary>
    public WriterSelectionValue<TextAlignment> Alignment { get; }

    /// <summary>Gets the selected left-indentation state in device-independent units.</summary>
    public WriterSelectionValue<double> Indentation { get; }

    /// <summary>Gets the selected paragraph-spacing-before state in device-independent units.</summary>
    public WriterSelectionValue<double> SpacingBefore { get; }

    /// <summary>Gets the selected paragraph-spacing-after state in device-independent units.</summary>
    public WriterSelectionValue<double> SpacingAfter { get; }

    /// <summary>Gets the selected list state.</summary>
    public WriterSelectionValue<WriterListKind> ListKind { get; }

    internal static WriterEditingState Empty() => new(
        hasSelection: false,
        hasTextContext: false,
        isEnabled: false,
        isReadOnly: false,
        canFormat: false,
        canCopy: false,
        canCut: false,
        canPaste: false,
        canSelectAll: false,
        canUndo: false,
        canRedo: false,
        WriterSelectionValue<FontFamily>.Unset(),
        WriterSelectionValue<double>.Unset(),
        WriterSelectionValue<bool>.Unset(),
        WriterSelectionValue<bool>.Unset(),
        WriterSelectionValue<bool>.Unset(),
        WriterSelectionValue<Color?>.Unset(),
        WriterSelectionValue<Color?>.Unset(),
        WriterSelectionValue<TextAlignment>.Unset(),
        WriterSelectionValue<double>.Unset(),
        WriterSelectionValue<double>.Unset(),
        WriterSelectionValue<double>.Unset(),
        WriterSelectionValue<WriterListKind>.Unset());
}
