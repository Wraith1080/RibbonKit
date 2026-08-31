using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace RibbonKit.Writer.Editing;

/// <summary>Controls where a find operation begins relative to the native editor selection.</summary>
public enum WriterFindStartBehavior
{
    /// <summary>Begin at the end of the current selection, or at the caret when it is collapsed.</summary>
    AfterCurrentSelection,

    /// <summary>Begin at the start of the current selection, or at the caret when it is collapsed.</summary>
    CurrentSelection,

    /// <summary>Begin at the start of the document.</summary>
    DocumentStart
}

/// <summary>Options for a deterministic native-editor find operation.</summary>
public sealed record WriterFindOptions
{
    /// <summary>Gets the text to locate. An empty value is rejected and never matches.</summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>Gets whether matching uses ordinal case-sensitive comparison.</summary>
    public bool MatchCase { get; init; }

    /// <summary>Gets whether a search that reaches the end continues from the document start.</summary>
    public bool Wrap { get; init; } = true;

    /// <summary>Gets the deterministic starting point for the search.</summary>
    public WriterFindStartBehavior StartBehavior { get; init; } = WriterFindStartBehavior.AfterCurrentSelection;

    /// <summary>Gets or sets the common case-sensitive alias for <see cref="MatchCase"/>.</summary>
    public bool CaseSensitive
    {
        get => MatchCase;
        init => MatchCase = value;
    }
}

/// <summary>Describes the outcome and selected span of a find operation.</summary>
public readonly record struct WriterFindResult
{
    private WriterFindResult(
        bool found,
        bool emptyQuery,
        bool wrapped,
        int index,
        int length,
        bool crossesParagraphBoundary,
        string query)
    {
        Found = found;
        EmptyQuery = emptyQuery;
        Wrapped = wrapped;
        Index = index;
        Length = length;
        CrossesParagraphBoundary = crossesParagraphBoundary;
        Query = query;
    }

    /// <summary>Gets whether a non-empty query was selected in the editor.</summary>
    public bool Found { get; }

    /// <summary>Gets whether the query was empty and therefore rejected without scanning.</summary>
    public bool EmptyQuery { get; }

    /// <summary>Gets whether the match was found after wrapping at the document end.</summary>
    public bool Wrapped { get; }

    /// <summary>
    /// Gets the UTF-16 index in the canonical document text, where a non-user-text barrier occupies
    /// one internal position, or -1 when not found.
    /// </summary>
    public int Index { get; }

    /// <summary>Gets the UTF-16 length of the match, or zero when not found.</summary>
    public int Length { get; }

    /// <summary>Gets whether the match includes a FlowDocument paragraph boundary.</summary>
    /// <remarks>
    /// Paragraph boundaries are represented by one canonical <c>\n</c>. They can be found, but a
    /// replacement that includes one is rejected so replacing text cannot merge or flatten blocks.
    /// </remarks>
    public bool CrossesParagraphBoundary { get; }

    /// <summary>Gets the normalized query used by the operation.</summary>
    public string Query { get; }

    /// <summary>Gets a not-found result.</summary>
    public static WriterFindResult NotFound(string query = "") =>
        new(false, string.IsNullOrEmpty(query), false, -1, 0, false, query);

    internal static WriterFindResult Empty(string query) => new(false, true, false, -1, 0, false, query);

    internal static WriterFindResult Match(
        int index, int length, bool wrapped, bool crossesParagraphBoundary, string query) =>
        new(true, false, wrapped, index, length, crossesParagraphBoundary, query);
}

/// <summary>Describes one replace operation without rescanning replacement text.</summary>
public readonly record struct WriterReplaceResult
{
    private WriterReplaceResult(bool replaced, bool emptyQuery, bool readOnly, bool structuralBoundary,
        bool found, bool wrapped)
    {
        Replaced = replaced;
        EmptyQuery = emptyQuery;
        ReadOnly = readOnly;
        StructuralBoundary = structuralBoundary;
        Found = found;
        Wrapped = wrapped;
    }

    /// <summary>Gets whether the selected native range was replaced.</summary>
    public bool Replaced { get; }

    /// <summary>Gets whether an empty query was rejected.</summary>
    public bool EmptyQuery { get; }

    /// <summary>Gets whether the editor was disabled or read-only.</summary>
    public bool ReadOnly { get; }

    /// <summary>Gets whether the match included a structural paragraph boundary.</summary>
    public bool StructuralBoundary { get; }

    /// <summary>Gets whether a match was found before replacement was considered.</summary>
    public bool Found { get; }

    /// <summary>Gets whether the find operation wrapped.</summary>
    public bool Wrapped { get; }

    internal static WriterReplaceResult From(WriterFindResult find, bool replaced, bool readOnly,
        bool structuralBoundary) => new(replaced, find.EmptyQuery, readOnly, structuralBoundary,
        find.Found, find.Wrapped);
}

/// <summary>
/// Provides deterministic find, replace-one and replace-all operations over a native
/// <see cref="RichTextBox"/>.
/// </summary>
/// <remarks>
/// The service snapshots native text only for the current operation and applies replacements through
/// <see cref="TextRange.Text"/>. Paragraph boundaries are exposed as one canonical <c>\n</c> between
/// paragraphs; a match may cross that boundary for find purposes, but replacements containing it are
/// skipped. This explicit rule preserves the existing FlowDocument block structure and formatting
/// outside each replaced span. Inline/block UI elements and anchored Figure/Floater content are
/// represented by non-user-text barriers, so ordinary text cannot bridge or remove those elements.
/// Searches are ordinal and never use the current UI culture. The default start is after the current
/// selection (or the caret), and wrapping is enabled; replacement text is never searched again during
/// ReplaceAll.
/// </remarks>
public class WriterFindReplaceService : IDisposable
{
    private bool _disposed;

    /// <summary>Creates a service over an existing native editor.</summary>
    /// <param name="editor">The editor whose document and selection remain application-owned.</param>
    public WriterFindReplaceService(RichTextBox editor)
    {
        Editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    /// <summary>Gets the native editor used by this service.</summary>
    public RichTextBox Editor { get; }

    /// <summary>Finds and selects the next match using explicit options.</summary>
    /// <param name="options">The query, case option, wrapping option and starting behavior.</param>
    /// <returns>The selected match, or a not-found result.</returns>
    public WriterFindResult FindNext(WriterFindOptions options)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(options);

        var query = NormalizeLineEndings(options.Query ?? string.Empty);
        if (query.Length == 0)
            return WriterFindResult.Empty(query);

        var snapshot = WriterDocumentTextSnapshot.Create(Editor.Document);
        var start = GetStartIndex(snapshot, options.StartBehavior);
        var comparison = options.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var index = snapshot.Text.IndexOf(query, start, comparison);
        var wrapped = false;

        if (index < 0 && options.Wrap && start > 0)
        {
            index = snapshot.Text.IndexOf(query, 0, start, comparison);
            wrapped = index >= 0;
        }

        if (index < 0)
            return WriterFindResult.NotFound(query);

        var result = WriterFindResult.Match(index, query.Length, wrapped,
            snapshot.ContainsStructuralBoundary(index, query.Length), query);
        Select(snapshot, result);
        return result;
    }

    /// <summary>Finds and selects the next match using the common overload options.</summary>
    public WriterFindResult FindNext(string query, bool matchCase = false, bool wrap = true)
    {
        return FindNext(new WriterFindOptions
        {
            Query = query,
            MatchCase = matchCase,
            Wrap = wrap,
            StartBehavior = WriterFindStartBehavior.AfterCurrentSelection
        });
    }

    /// <summary>Finds and selects the next match with a case-sensitive alias.</summary>
    public WriterFindResult FindNext(string query, bool caseSensitive, bool wrap, bool useCurrentSelection)
    {
        return FindNext(new WriterFindOptions
        {
            Query = query,
            MatchCase = caseSensitive,
            Wrap = wrap,
            StartBehavior = useCurrentSelection
                ? WriterFindStartBehavior.CurrentSelection
                : WriterFindStartBehavior.AfterCurrentSelection
        });
    }

    /// <summary>Tries to find and select the next match.</summary>
    public bool TryFindNext(string query, out WriterFindResult result, bool matchCase = false, bool wrap = true)
    {
        result = FindNext(query, matchCase, wrap);
        return result.Found;
    }

    /// <summary>Replaces the current selection when it is an exact query match.</summary>
    /// <remarks>
    /// This operation never searches for another span. It is useful after a caller deliberately
    /// selected text or after <see cref="FindNext(WriterFindOptions)"/>. A selection containing a
    /// paragraph boundary is left unchanged.
    /// </remarks>
    public WriterReplaceResult ReplaceCurrent(string query, string replacement, bool matchCase = false)
    {
        ThrowIfDisposed();
        query = NormalizeLineEndings(query ?? string.Empty);
        replacement ??= string.Empty;
        if (query.Length == 0)
            return WriterReplaceResult.From(WriterFindResult.Empty(query), false, false, false);

        var snapshot = WriterDocumentTextSnapshot.Create(Editor.Document);
        var start = GetLogicalOffset(snapshot, Editor.Selection.Start);
        var end = GetLogicalOffset(snapshot, Editor.Selection.End);
        if (end <= start || end - start != query.Length ||
            !string.Equals(snapshot.Text.Substring(start, end - start), query,
                matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
        {
            return WriterReplaceResult.From(WriterFindResult.NotFound(query), false, false, false);
        }

        var find = WriterFindResult.Match(start, query.Length, false,
            snapshot.ContainsStructuralBoundary(start, query.Length), query);
        if (find.CrossesParagraphBoundary)
            return WriterReplaceResult.From(find, false, false, true);
        if (!CanMutate)
            return WriterReplaceResult.From(find, false, true, false);

        ReplaceSpan(snapshot, start, query.Length, replacement);
        return WriterReplaceResult.From(find, true, false, false);
    }

    /// <summary>Finds and replaces one next match.</summary>
    public WriterReplaceResult ReplaceNext(string query, string replacement, bool matchCase = false, bool wrap = true)
    {
        ThrowIfDisposed();
        replacement ??= string.Empty;
        var normalizedQuery = NormalizeLineEndings(query ?? string.Empty);
        if (normalizedQuery.Length > 0 && TryGetCurrentMatch(normalizedQuery, matchCase, out var current))
        {
            if (current.CrossesParagraphBoundary)
                return WriterReplaceResult.From(current, false, false, true);
            if (!CanMutate)
                return WriterReplaceResult.From(current, false, true, false);

            var currentSnapshot = WriterDocumentTextSnapshot.Create(Editor.Document);
            ReplaceSpan(currentSnapshot, current.Index, current.Length, replacement);
            return WriterReplaceResult.From(current, true, false, false);
        }

        var find = FindNext(normalizedQuery, matchCase, wrap);
        if (!find.Found || find.EmptyQuery || find.CrossesParagraphBoundary)
            return WriterReplaceResult.From(find, false, false, find.CrossesParagraphBoundary);
        if (!CanMutate)
            return WriterReplaceResult.From(find, false, true, false);

        var snapshot = WriterDocumentTextSnapshot.Create(Editor.Document);
        ReplaceSpan(snapshot, find.Index, find.Length, replacement);
        return WriterReplaceResult.From(find, true, false, false);
    }

    /// <summary>
    /// Replaces all matches in the document snapshot and returns the number of replacements applied.
    /// </summary>
    /// <remarks>
    /// Matches are collected once, then applied from the end of the original document toward the
    /// beginning inside one native change block. Consequently a replacement containing the query
    /// cannot cause a loop or be counted again. Matches crossing paragraph boundaries are skipped.
    /// </remarks>
    public int ReplaceAll(string query, string replacement, bool matchCase = false)
    {
        ThrowIfDisposed();
        query = NormalizeLineEndings(query ?? string.Empty);
        replacement ??= string.Empty;
        if (query.Length == 0 || !CanMutate)
            return 0;

        var snapshot = WriterDocumentTextSnapshot.Create(Editor.Document);
        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var matches = new List<int>();
        for (var index = snapshot.Text.IndexOf(query, 0, comparison);
             index >= 0;
             index = snapshot.Text.IndexOf(query, index + query.Length, comparison))
        {
            if (!snapshot.ContainsStructuralBoundary(index, query.Length))
                matches.Add(index);
        }

        if (matches.Count == 0)
            return 0;

        Editor.BeginChange();
        try
        {
            for (var i = matches.Count - 1; i >= 0; i--)
                ReplaceSpan(snapshot, matches[i], query.Length, replacement);
        }
        finally
        {
            Editor.EndChange();
        }

        return matches.Count;
    }

    /// <summary>Gets whether the editor accepts a document mutation.</summary>
    public bool CanMutate => Editor.IsEnabled && !Editor.IsReadOnly;

    /// <summary>Removes the service's ownership marker; the native editor remains usable.</summary>
    public void Dispose() => _disposed = true;

    private static string NormalizeLineEndings(string value) => value.Replace("\r\n", "\n").Replace('\r', '\n');

    private int GetStartIndex(WriterDocumentTextSnapshot snapshot, WriterFindStartBehavior behavior)
    {
        return behavior switch
        {
            WriterFindStartBehavior.DocumentStart => 0,
            WriterFindStartBehavior.CurrentSelection => GetLogicalOffset(snapshot, Editor.Selection.Start),
            WriterFindStartBehavior.AfterCurrentSelection => GetLogicalOffset(snapshot, Editor.Selection.End),
            _ => throw new ArgumentOutOfRangeException(nameof(behavior), behavior, "Unknown find start behavior.")
        };
    }

    private static int GetLogicalOffset(WriterDocumentTextSnapshot snapshot, TextPointer pointer)
    {
        for (var i = 0; i < snapshot.Units.Count; i++)
        {
            var unit = snapshot.Units[i];
            if (pointer.CompareTo(unit.Start) <= 0)
                return i;
            if (pointer.CompareTo(unit.End) <= 0)
                return i + 1;
        }
        return snapshot.Units.Count;
    }

    private bool TryGetCurrentMatch(string query, bool matchCase, out WriterFindResult result)
    {
        var snapshot = WriterDocumentTextSnapshot.Create(Editor.Document);
        var start = GetLogicalOffset(snapshot, Editor.Selection.Start);
        var end = GetLogicalOffset(snapshot, Editor.Selection.End);
        if (end > start && end - start == query.Length &&
            string.Equals(snapshot.Text.Substring(start, end - start), query,
                matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
        {
            result = WriterFindResult.Match(start, query.Length, false,
                snapshot.ContainsStructuralBoundary(start, query.Length), query);
            return true;
        }

        result = WriterFindResult.NotFound(query);
        return false;
    }

    private void Select(WriterDocumentTextSnapshot snapshot, WriterFindResult result)
    {
        var start = snapshot.Units[result.Index].Start;
        var end = snapshot.Units[result.Index + result.Length - 1].End;
        Editor.Selection.Select(start, end);
    }

    private void ReplaceSpan(WriterDocumentTextSnapshot snapshot, int index, int length, string replacement)
    {
        var start = snapshot.Units[index].Start;
        var end = snapshot.Units[index + length - 1].End;
        var range = new TextRange(start, end);
        Editor.BeginChange();
        try
        {
            range.Text = replacement;
            var caret = range.End.GetInsertionPosition(LogicalDirection.Backward);
            Editor.Selection.Select(caret, caret);
        }
        finally
        {
            Editor.EndChange();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

internal readonly record struct WriterDocumentTextUnit(
    char Character,
    TextPointer Start,
    TextPointer End,
    bool IsStructuralBoundary,
    bool IsNonTextBarrier);

internal sealed class WriterDocumentTextSnapshot
{
    private WriterDocumentTextSnapshot(string text, IReadOnlyList<WriterDocumentTextUnit> units)
    {
        Text = text;
        Units = units;
    }

    public string Text { get; }

    public IReadOnlyList<WriterDocumentTextUnit> Units { get; }

    public static WriterDocumentTextSnapshot Create(FlowDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var text = new StringBuilder();
        var units = new List<WriterDocumentTextUnit>();
        var pointer = document.ContentStart;
        var end = document.ContentEnd;

        while (pointer.CompareTo(end) < 0)
        {
            var context = pointer.GetPointerContext(LogicalDirection.Forward);
            if (context == TextPointerContext.Text)
            {
                var run = pointer.GetTextInRun(LogicalDirection.Forward);
                for (var i = 0; i < run.Length; i++)
                {
                    var start = pointer.GetPositionAtOffset(i, LogicalDirection.Forward)!;
                    var unitEnd = pointer.GetPositionAtOffset(i + 1, LogicalDirection.Forward)!;
                    text.Append(run[i]);
                    units.Add(new WriterDocumentTextUnit(run[i], start, unitEnd, false, false));
                }

                pointer = pointer.GetPositionAtOffset(run.Length, LogicalDirection.Forward)!;
                continue;
            }

            var adjacent = context == TextPointerContext.ElementEnd
                ? pointer.GetAdjacentElement(LogicalDirection.Backward)
                : pointer.GetAdjacentElement(LogicalDirection.Forward);
            var next = pointer.GetPositionAtOffset(1, LogicalDirection.Forward)!;
            if (context == TextPointerContext.EmbeddedElement)
            {
                AddUnit(text, units, '\uFFFC', pointer, next, false, true);
            }
            else if (context == TextPointerContext.ElementStart && IsNonTextElement(adjacent))
            {
                AddUnit(text, units, '\uFFFC', pointer, next, false, true);
            }
            else if (context == TextPointerContext.ElementEnd &&
                pointer.Parent is Paragraph && next.Parent is not Paragraph)
            {
                AddUnit(text, units, '\n', pointer, next, true, false);
            }
            else if (context == TextPointerContext.ElementStart && adjacent is LineBreak)
            {
                AddUnit(text, units, '\n', pointer, next, false, false);
            }
            else if (context == TextPointerContext.ElementEnd && IsNonTextElement(adjacent))
            {
                AddUnit(text, units, '\uFFFC', pointer, next, false, true);
            }

            pointer = next;
        }

        while (units.Count > 0 && units[^1].IsStructuralBoundary)
        {
            units.RemoveAt(units.Count - 1);
            text.Length--;
        }

        return new WriterDocumentTextSnapshot(text.ToString(), units);
    }

    public bool ContainsStructuralBoundary(int index, int length)
    {
        var end = Math.Min(index + length, Units.Count);
        for (var i = Math.Max(0, index); i < end; i++)
        {
            if (Units[i].IsStructuralBoundary || Units[i].IsNonTextBarrier)
                return true;
        }
        return false;
    }

    private static bool IsNonTextElement(object? element) => element is
        InlineUIContainer or BlockUIContainer or Figure or Floater or UIElement;

    private static void AddUnit(
        StringBuilder text,
        List<WriterDocumentTextUnit> units,
        char character,
        TextPointer start,
        TextPointer end,
        bool isStructuralBoundary,
        bool isNonTextBarrier)
    {
        if (isNonTextBarrier && units.Count > 0 && units[^1].IsNonTextBarrier)
        {
            units[^1] = units[^1] with { End = end };
            return;
        }

        text.Append(character);
        units.Add(new WriterDocumentTextUnit(character, start, end,
            isStructuralBoundary, isNonTextBarrier));
    }
}
