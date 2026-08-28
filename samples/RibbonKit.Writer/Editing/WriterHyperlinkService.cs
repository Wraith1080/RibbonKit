using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace RibbonKit.Writer.Editing;

/// <summary>Creates, edits and removes safe Writer hyperlinks in a native editor.</summary>
/// <remarks>
/// Writer hyperlinks are inert navigation metadata. Only absolute HTTP(S) and mailto URIs are
/// accepted; file, pack, javascript, data and other activation schemes are rejected before the
/// document changes.
/// </remarks>
public sealed class WriterHyperlinkService
{
    /// <summary>Maximum URI length accepted by hyperlink operations.</summary>
    public const int MaximumUriLength = 2048;

    /// <summary>Tries to create a hyperlink over the current selection.</summary>
    /// <param name="editor">The live native Writer editor.</param>
    /// <param name="uri">The absolute, safe navigation URI.</param>
    /// <param name="displayText">Optional replacement text; otherwise selected text or the URI is used.</param>
    /// <returns><see langword="true"/> when a hyperlink was created.</returns>
    public bool TryCreate(RichTextBox editor, Uri uri, string? displayText = null)
    {
        ArgumentNullException.ThrowIfNull(editor);
        if (!editor.IsEnabled || editor.IsReadOnly
            || !IsSafeUri(uri) || !TryValidateDisplayText(displayText))
            return false;

        var selection = editor.Selection;
        var selectedText = selection.Start.CompareTo(selection.End) == 0
            ? null
            : TrimSelectionTerminator(new TextRange(selection.Start, selection.End).Text);
        var text = displayText ?? selectedText;
        if (string.IsNullOrEmpty(text))
            text = uri.ToString();
        if (!TryValidateDisplayText(text))
            return false;
        if (WriterInlineInsertion.FindHyperlink(editor) is not null)
            return false;

        var link = new Hyperlink(new Run(text)) { NavigateUri = uri };
        return WriterInlineInsertion.TryReplaceSelection(editor, link);
    }

    /// <summary>Tries to create a hyperlink after validating a user-provided URI string.</summary>
    public bool TryCreate(RichTextBox editor, string uri, string? displayText = null)
    {
        ArgumentNullException.ThrowIfNull(editor);
        return TryParseUri(uri, out var parsed) && TryCreate(editor, parsed, displayText);
    }

    /// <summary>Tries to change the URI and optionally the visible text of the current hyperlink.</summary>
    public bool TryEdit(RichTextBox editor, Uri uri, string? displayText = null)
    {
        ArgumentNullException.ThrowIfNull(editor);
        if (!editor.IsEnabled || editor.IsReadOnly
            || !IsSafeUri(uri) || !TryValidateDisplayText(displayText))
            return false;
        var hyperlink = WriterInlineInsertion.FindHyperlink(editor);
        if (hyperlink is null)
            return false;

        var owner = WriterInlineInsertion.GetOwnerCollection(hyperlink);
        if (owner is null)
            return false;

        var selection = editor.Selection;
        var startAtStart = selection.Start.CompareTo(hyperlink.ElementStart) == 0;
        var startAtEnd = selection.Start.CompareTo(hyperlink.ElementEnd) == 0;
        var endAtStart = selection.End.CompareTo(hyperlink.ElementStart) == 0;
        var endAtEnd = selection.End.CompareTo(hyperlink.ElementEnd) == 0;
        var replacement = new Hyperlink { NavigateUri = uri };
        CopyHyperlinkProperties(hyperlink, replacement);
        if (displayText is not null)
        {
            var run = new Run(displayText);
            if (hyperlink.Inlines.FirstInline is Run sourceRun)
                CopyRunProperties(sourceRun, run);
            replacement.Inlines.Add(run);
        }

        using (WriterInlineInsertion.BeginChange(editor))
        {
            owner.InsertBefore(hyperlink, replacement);
            if (displayText is null)
            {
                foreach (var child in hyperlink.Inlines.ToArray())
                {
                    hyperlink.Inlines.Remove(child);
                    replacement.Inlines.Add(child);
                }
            }
            owner.Remove(hyperlink);
            var mappedStart = startAtStart ? replacement.ElementStart
                : startAtEnd ? replacement.ElementEnd : replacement.ContentStart;
            var mappedEnd = endAtStart ? replacement.ElementStart
                : endAtEnd ? replacement.ElementEnd : replacement.ContentEnd;
            editor.Selection.Select(mappedStart, mappedEnd);
        }
        return true;
    }

    /// <summary>Tries to change the URI from a user-provided string.</summary>
    public bool TryEdit(RichTextBox editor, string uri, string? displayText = null)
    {
        ArgumentNullException.ThrowIfNull(editor);
        return TryParseUri(uri, out var parsed) && TryEdit(editor, parsed, displayText);
    }

    /// <summary>Removes the current hyperlink while retaining its visible inline content.</summary>
    public bool TryRemove(RichTextBox editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        var hyperlink = WriterInlineInsertion.FindHyperlink(editor);
        return hyperlink is not null && TryRemove(editor, hyperlink);
    }

    /// <summary>Removes one captured live hyperlink while retaining its visible inline content.</summary>
    public bool TryRemove(RichTextBox editor, Hyperlink hyperlink)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(hyperlink);
        if (!WriterInlineInsertion.IsInlineInDocument(editor.Document, hyperlink))
            return false;
        var collection = WriterInlineInsertion.GetOwnerCollection(hyperlink);
        if (collection is null || !editor.IsEnabled || editor.IsReadOnly)
            return false;

        using (WriterInlineInsertion.BeginChange(editor))
        {
            var next = hyperlink.NextInline;
            var children = hyperlink.Inlines.ToArray();
            foreach (var child in children)
            {
                hyperlink.Inlines.Remove(child);
                if (next is null)
                    collection.Add(child);
                else
                    collection.InsertBefore(next, child);
            }

            var caret = hyperlink.ElementStart.GetInsertionPosition(LogicalDirection.Backward);
            collection.Remove(hyperlink);
            editor.Selection.Select(caret, caret);
        }
        return true;
    }

    /// <summary>Returns whether a URI is safe Writer navigation metadata.</summary>
    public static bool IsSafeUri(Uri? uri)
    {
        if (uri is null || !uri.IsAbsoluteUri || uri.OriginalString.Length is 0 or > MaximumUriLength
            || uri.OriginalString.Any(char.IsControl)
            || uri.OriginalString.Any(char.IsWhiteSpace))
            return false;

        var isHttp = uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        var isMailto = uri.Scheme.Equals(Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase);
        if (HasUnsafePercentEscapes(uri.OriginalString, isMailto))
            return false;
        return (isHttp && uri.UserInfo.Length == 0)
            || (isMailto && !uri.OriginalString.StartsWith("mailto://", StringComparison.OrdinalIgnoreCase)
                && !uri.UserInfo.Contains(':', StringComparison.Ordinal));
    }

    /// <summary>Parses and validates a user-provided absolute URI.</summary>
    public static bool TryParseUri(string? value, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumUriLength
            || value.Any(char.IsControl) || value.Any(char.IsWhiteSpace))
            return false;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) || !IsSafeUri(parsed))
            return false;
        uri = parsed;
        return true;
    }

    private static bool TryValidateDisplayText(string? text) => text is null
        || (text.Length is > 0 and <= 4096
            && !text.Any(static character => character is '\r' or '\n' || char.IsControl(character)));

    private static bool HasUnsafePercentEscapes(string value, bool mailto)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '%')
                continue;
            if (index + 2 >= value.Length
                || !Uri.IsHexDigit(value[index + 1]) || !Uri.IsHexDigit(value[index + 2]))
                return true;
            var decoded = Convert.ToByte(value.Substring(index + 1, 2), 16);
            if (decoded < 0x20 || decoded == 0x7F || mailto && decoded == ':')
                return true;
            index += 2;
        }
        return false;
    }

    private static string TrimSelectionTerminator(string text) => text.EndsWith("\r\n", StringComparison.Ordinal)
        ? text[..^2]
        : text.EndsWith('\n') ? text[..^1] : text;

    private static void CopyHyperlinkProperties(Hyperlink source, Hyperlink target)
    {
        CopyLocalValue(source, target, TextElement.FontFamilyProperty);
        CopyLocalValue(source, target, TextElement.FontSizeProperty);
        CopyLocalValue(source, target, TextElement.FontStretchProperty);
        CopyLocalValue(source, target, TextElement.FontStyleProperty);
        CopyLocalValue(source, target, TextElement.FontWeightProperty);
        CopyLocalValue(source, target, TextElement.ForegroundProperty);
        CopyLocalValue(source, target, TextElement.BackgroundProperty);
        CopyLocalValue(source, target, FrameworkElement.FlowDirectionProperty);
        CopyLocalValue(source, target, Inline.BaselineAlignmentProperty);
        CopyLocalValue(source, target, Inline.TextDecorationsProperty);
    }

    private static void CopyRunProperties(Run source, Run target)
    {
        CopyLocalValue(source, target, TextElement.FontFamilyProperty);
        CopyLocalValue(source, target, TextElement.FontSizeProperty);
        CopyLocalValue(source, target, TextElement.FontStretchProperty);
        CopyLocalValue(source, target, TextElement.FontStyleProperty);
        CopyLocalValue(source, target, TextElement.FontWeightProperty);
        CopyLocalValue(source, target, TextElement.ForegroundProperty);
        CopyLocalValue(source, target, TextElement.BackgroundProperty);
        CopyLocalValue(source, target, FrameworkElement.FlowDirectionProperty);
        CopyLocalValue(source, target, Inline.BaselineAlignmentProperty);
        CopyLocalValue(source, target, Inline.TextDecorationsProperty);
    }

    private static void CopyLocalValue(DependencyObject source, DependencyObject target,
        DependencyProperty property)
    {
        var value = source.ReadLocalValue(property);
        if (value != DependencyProperty.UnsetValue)
            target.SetValue(property, value);
    }
}

/// <summary>Inserts a deterministic, culture-aware date or time value as native text.</summary>
public sealed class WriterDateTimeService
{
    /// <summary>Inserts a formatted date/time at the caret, replacing one same-paragraph selection.</summary>
    /// <param name="editor">The live native Writer editor.</param>
    /// <param name="value">The value to format.</param>
    /// <param name="format">A standard or custom .NET date/time format.</param>
    /// <param name="culture">The culture used for formatting, or the current culture when omitted.</param>
    /// <returns><see langword="true"/> when text was inserted.</returns>
    public bool TryInsert(RichTextBox editor, DateTimeOffset value,
        string format = "g", IFormatProvider? culture = null)
    {
        ArgumentNullException.ThrowIfNull(editor);
        if (format is null || format.Length is 0 or > 128
            || format.Any(static character => character is '\r' or '\n' || char.IsControl(character)))
            return false;

        string text;
        try
        {
            text = value.ToString(format, culture ?? System.Globalization.CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            return false;
        }
        if (text.Length is 0 or > 256 || text.Any(char.IsControl))
            return false;
        return WriterInlineInsertion.TryReplaceSelection(editor, new Run(text));
    }

    /// <summary>Inserts a local <see cref="DateTime"/> value using the supplied culture.</summary>
    public bool TryInsert(RichTextBox editor, DateTime value,
        string format = "g", IFormatProvider? culture = null) =>
        TryInsert(editor, new DateTimeOffset(value), format, culture);
}
