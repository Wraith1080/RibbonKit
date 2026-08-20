using System.Windows.Input;

namespace RibbonKit.Writer.Editing;

/// <summary>Commands owned by the Writer formatting adapter.</summary>
/// <remarks>
/// Standard clipboard and history commands remain the WPF <see cref="ApplicationCommands"/>
/// commands. These commands cover Writer-specific values and paragraph operations so the later
/// ribbon integration can bind to stable, application-owned command objects without changing the
/// RibbonKit runtime.
/// </remarks>
public static class WriterEditingCommands
{
    /// <summary>Applies a <see cref="System.Windows.Media.FontFamily"/> or font-family name.</summary>
    public static RoutedUICommand ApplyFontFamily { get; } = Create("Apply Font Family", nameof(ApplyFontFamily));

    /// <summary>Applies a numeric font size in device-independent units.</summary>
    public static RoutedUICommand ApplyFontSize { get; } = Create("Apply Font Size", nameof(ApplyFontSize));

    /// <summary>Applies a foreground Color, SolidColorBrush, supported colour string, or null.</summary>
    public static RoutedUICommand ApplyForeground { get; } = Create("Apply Foreground", nameof(ApplyForeground));

    /// <summary>Applies a highlight Color, SolidColorBrush, supported colour string, or null.</summary>
    public static RoutedUICommand ApplyHighlight { get; } = Create("Apply Highlight", nameof(ApplyHighlight));

    /// <summary>Sets paragraph alignment from a <see cref="System.Windows.TextAlignment"/> value.</summary>
    public static RoutedUICommand SetAlignment { get; } = Create("Set Alignment", nameof(SetAlignment));

    /// <summary>Sets the left paragraph indentation in device-independent units.</summary>
    public static RoutedUICommand SetIndentation { get; } = Create("Set Indentation", nameof(SetIndentation));

    /// <summary>Increases the left paragraph indentation by the adapter's standard step.</summary>
    public static RoutedUICommand IncreaseIndentation { get; } = Create("Increase Indentation", nameof(IncreaseIndentation));

    /// <summary>Decreases the left paragraph indentation by the adapter's standard step.</summary>
    public static RoutedUICommand DecreaseIndentation { get; } = Create("Decrease Indentation", nameof(DecreaseIndentation));

    /// <summary>Sets paragraph spacing before the selected paragraphs in device-independent units.</summary>
    public static RoutedUICommand SetParagraphSpacingBefore { get; } = Create("Set Paragraph Spacing Before", nameof(SetParagraphSpacingBefore));

    /// <summary>Sets paragraph spacing after the selected paragraphs in device-independent units.</summary>
    public static RoutedUICommand SetParagraphSpacingAfter { get; } = Create("Set Paragraph Spacing After", nameof(SetParagraphSpacingAfter));

    /// <summary>Toggles a bulleted list for the selected paragraphs.</summary>
    public static RoutedUICommand ToggleBullets { get; } = Create("Toggle Bullets", nameof(ToggleBullets));

    /// <summary>Toggles a numbered list for the selected paragraphs.</summary>
    public static RoutedUICommand ToggleNumbering { get; } = Create("Toggle Numbering", nameof(ToggleNumbering));

    private static RoutedUICommand Create(string text, string name) =>
        new(text, name, typeof(WriterEditingCommands));
}
