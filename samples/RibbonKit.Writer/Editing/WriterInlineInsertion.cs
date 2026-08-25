using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace RibbonKit.Writer.Editing;

/// <summary>Small, editor-only primitives shared by Writer structured-content services.</summary>
/// <remarks>
/// WPF does not expose an insertion method for an arbitrary <see cref="Inline"/>. This helper keeps
/// insertion inside the current paragraph, splits a run when necessary, and leaves the native
/// editor's selection/undo ownership intact. It deliberately rejects a selection crossing paragraph
/// boundaries rather than silently changing document structure.
/// </remarks>
internal static class WriterInlineInsertion
{
    internal static bool TryReplaceSelection(RichTextBox editor, Inline inline)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(inline);
        if (!editor.IsEnabled || editor.IsReadOnly)
            return false;
        if (editor.Document.Blocks.Count == 0)
            return TryInsertIntoEmptyDocument(editor, inline);

        var selection = editor.Selection;
        var start = selection.Start;
        var end = selection.End;
        var paragraph = FindParagraph(start);
        var endParagraph = FindParagraph(end);
        if (endParagraph is null)
        {
            var previous = end.GetPositionAtOffset(-1, LogicalDirection.Backward);
            endParagraph = previous is null ? null : FindParagraph(previous);
        }
        if (endParagraph is null && paragraph is not null
            && end.CompareTo(paragraph.ContentStart) >= 0
            && end.CompareTo(paragraph.ContentEnd) <= 0)
            endParagraph = paragraph;
        if (paragraph is null || !ReferenceEquals(paragraph, endParagraph))
            return false;

        var clippedEnd = end.CompareTo(paragraph.ContentEnd) > 0
            ? paragraph.ContentEnd
            : end;
        if (!CanInsert(paragraph, start, inline))
            return false;

        editor.BeginChange();
        try
        {
            // Insert first so a failed preflight or native collection operation never deletes the
            // user's selection. If deletion fails after insertion, remove only the new inline while
            // the same native change scope is still open.
            if (!TryInsert(paragraph, start, inline))
                return false;

            try
            {
                if (start.CompareTo(end) != 0)
                    new TextRange(inline.ElementEnd, clippedEnd).Text = string.Empty;
            }
            catch
            {
                RemoveInlineCore(inline);
                throw;
            }

            var caret = inline.ElementEnd.GetInsertionPosition(LogicalDirection.Forward);
            editor.Selection.Select(caret, caret);
            return true;
        }
        catch
        {
            RemoveInlineCore(inline);
            return false;
        }
        finally
        {
            editor.EndChange();
        }
    }

    internal static IDisposable BeginChange(RichTextBox editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        editor.BeginChange();
        return new ChangeScope(editor);
    }

    private static bool TryInsertIntoEmptyDocument(RichTextBox editor, Inline inline)
    {
        var document = editor.Document;
        var paragraph = new Paragraph();
        using (BeginChange(editor))
        {
            try
            {
                document.Blocks.Add(paragraph);
                if (!TryInsert(paragraph, paragraph.ContentStart, inline))
                {
                    document.Blocks.Remove(paragraph);
                    return false;
                }
                var caret = inline.ElementEnd.GetInsertionPosition(LogicalDirection.Forward);
                editor.Selection.Select(caret, caret);
                return true;
            }
            catch
            {
                if (inline.Parent is not null)
                    RemoveInlineCore(inline);
                if (paragraph.Parent is not null)
                    document.Blocks.Remove(paragraph);
                return false;
            }
        }
    }

    internal static Paragraph? FindParagraph(TextPointer pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        for (DependencyObject? current = pointer.Parent; current is not null;
             current = GetParent(current))
        {
            if (current is Paragraph paragraph)
                return paragraph;
        }
        return null;
    }

    internal static Hyperlink? FindHyperlink(RichTextBox editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        var selection = editor.Selection;
        for (DependencyObject? current = selection.Start.Parent; current is not null;
             current = GetParent(current))
        {
            if (current is Hyperlink hyperlink)
                return hyperlink;
        }

        // A selection can start on a structural boundary whose Parent is the Paragraph. Look for a
        // link whose content range contains that boundary before falling back to the end pointer.
        foreach (var hyperlink in EnumerateHyperlinks(editor.Document))
        {
            if (ContainsPointer(hyperlink, selection.Start)
                || ContainsPointer(hyperlink, selection.End))
                return hyperlink;
        }
        return null;
    }

    internal static InlineUIContainer? FindImage(RichTextBox editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        for (DependencyObject? current = editor.Selection.Start.Parent; current is not null;
             current = GetParent(current))
        {
            if (current is InlineUIContainer container && container.Child is Image)
                return container;
        }

        foreach (var container in EnumerateImages(editor.Document))
        {
            if (ContainsPointer(container, editor.Selection.Start)
                || ContainsPointer(container, editor.Selection.End))
                return container;
        }
        return null;
    }

    private static bool ContainsPointer(Inline inline, TextPointer pointer) =>
        pointer.CompareTo(inline.ElementStart) >= 0
        && pointer.CompareTo(inline.ElementEnd) <= 0;

    internal static bool TryRemoveInline(RichTextBox editor, Inline inline)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(inline);
        if (!editor.IsEnabled || editor.IsReadOnly)
            return false;

        using (BeginChange(editor))
            return RemoveInlineCore(inline, editor);
    }

    internal static InlineCollection? GetOwnerCollection(Inline inline) => inline.Parent switch
    {
        Paragraph paragraph => paragraph.Inlines,
        Span span => span.Inlines,
        _ => null
    };

    internal static IEnumerable<Hyperlink> EnumerateHyperlinks(FlowDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        foreach (var block in EnumerateBlocks(document.Blocks))
        {
            if (block is not Paragraph paragraph)
                continue;
            foreach (var inline in EnumerateInlines(paragraph.Inlines))
            {
                if (inline is Hyperlink hyperlink)
                    yield return hyperlink;
            }
        }
    }

    internal static IEnumerable<InlineUIContainer> EnumerateImages(FlowDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        foreach (var block in EnumerateBlocks(document.Blocks))
        {
            if (block is not Paragraph paragraph)
                continue;
            foreach (var inline in EnumerateInlines(paragraph.Inlines))
            {
                if (inline is InlineUIContainer container && container.Child is Image)
                    yield return container;
            }
        }
    }

    private static bool TryInsert(Paragraph paragraph, TextPointer pointer, Inline inline)
    {
        var containingInline = FindContainingInline(pointer);
        if (containingInline is Run run)
            return InsertInsideRun(run, pointer, inline);

        var collection = containingInline is not null
            ? GetOwnerCollection(containingInline)
            : FindOwnerCollection(paragraph, pointer);
        if (collection is null)
            return false;

        for (var current = collection.FirstInline; current is not null; current = current.NextInline)
        {
            if (pointer.CompareTo(current.ContentStart) <= 0)
            {
                collection.InsertBefore(current, inline);
                return true;
            }
            if (pointer.CompareTo(current.ContentEnd) < 0)
                return false;
        }

        collection.Add(inline);
        return true;
    }

    private static bool CanInsert(Paragraph paragraph, TextPointer pointer, Inline inline)
    {
        var containingInline = FindContainingInline(pointer);
        if (containingInline is Run run)
        {
            var collection = GetOwnerCollection(run);
            if (collection is null)
                return false;
            var before = new TextRange(run.ContentStart, pointer).Text;
            var after = new TextRange(pointer, run.ContentEnd).Text;
            return before.Length + after.Length == (run.Text ?? string.Empty).Length;
        }

        var owner = containingInline is not null
            ? GetOwnerCollection(containingInline)
            : FindOwnerCollection(paragraph, pointer);
        return owner is not null;
    }

    private static bool InsertInsideRun(Run run, TextPointer pointer, Inline inline)
    {
        var collection = GetOwnerCollection(run);
        if (collection is null)
            return false;

        var before = new TextRange(run.ContentStart, pointer).Text;
        var after = new TextRange(pointer, run.ContentEnd).Text;
        var originalText = run.Text ?? string.Empty;
        if (before.Length + after.Length != originalText.Length)
            return false;

        Inline? trailingRun = null;
        try
        {
            if (before.Length == 0)
            {
                collection.InsertBefore(run, inline);
                return true;
            }

            if (after.Length > 0)
                trailingRun = CloneRun(run, after);
            collection.InsertAfter(run, inline);
            if (trailingRun is not null)
                collection.InsertAfter(inline, trailingRun);
            run.Text = before;
            return true;
        }
        catch
        {
            if (trailingRun?.Parent is not null)
                GetOwnerCollection(trailingRun)?.Remove(trailingRun);
            if (inline.Parent is not null)
                GetOwnerCollection(inline)?.Remove(inline);
            run.Text = originalText;
            return false;
        }
    }

    private static bool RemoveInlineCore(Inline inline, RichTextBox? editor = null)
    {
        var collection = GetOwnerCollection(inline);
        if (collection is null)
            return false;
        var caret = inline.ElementStart.GetInsertionPosition(LogicalDirection.Backward);
        collection.Remove(inline);
        if (editor is not null)
            editor.Selection.Select(caret, caret);
        return true;
    }

    private sealed class ChangeScope(RichTextBox editor) : IDisposable
    {
        public void Dispose() => editor.EndChange();
    }

    private static Run CloneRun(Run source, string text)
    {
        var clone = new Run(text);
        CopyLocalValue(source, clone, TextElement.FontFamilyProperty);
        CopyLocalValue(source, clone, TextElement.FontSizeProperty);
        CopyLocalValue(source, clone, TextElement.FontStretchProperty);
        CopyLocalValue(source, clone, TextElement.FontStyleProperty);
        CopyLocalValue(source, clone, TextElement.FontWeightProperty);
        CopyLocalValue(source, clone, TextElement.ForegroundProperty);
        CopyLocalValue(source, clone, TextElement.BackgroundProperty);
        CopyLocalValue(source, clone, FrameworkElement.FlowDirectionProperty);
        CopyLocalValue(source, clone, Inline.BaselineAlignmentProperty);
        CopyLocalValue(source, clone, Inline.TextDecorationsProperty);
        return clone;
    }

    private static void CopyLocalValue(DependencyObject source, DependencyObject target,
        DependencyProperty property)
    {
        var value = source.ReadLocalValue(property);
        if (value != DependencyProperty.UnsetValue)
            target.SetValue(property, value);
    }

    private static Inline? FindContainingInline(TextPointer pointer)
    {
        for (DependencyObject? current = pointer.Parent; current is not null;
             current = GetParent(current))
        {
            if (current is Inline inline)
                return inline;
            if (current is Paragraph)
                break;
        }
        return null;
    }

    private static InlineCollection? FindOwnerCollection(Paragraph paragraph, TextPointer pointer)
    {
        for (var current = paragraph.Inlines.FirstInline; current is not null; current = current.NextInline)
        {
            if (pointer.CompareTo(current.ContentStart) < 0)
                return paragraph.Inlines;
            if (pointer.CompareTo(current.ContentEnd) <= 0)
                return current is Span span ? span.Inlines : paragraph.Inlines;
        }
        return paragraph.Inlines;
    }

    private static DependencyObject? GetParent(DependencyObject current) => current switch
    {
        FrameworkContentElement contentElement => contentElement.Parent,
        FrameworkElement frameworkElement => frameworkElement.Parent,
        _ => null
    };

    private static IEnumerable<Block> EnumerateBlocks(BlockCollection blocks)
    {
        foreach (var block in blocks)
        {
            yield return block;
            if (block is Section section)
            {
                foreach (var nested in EnumerateBlocks(section.Blocks))
                    yield return nested;
            }
            else if (block is List list)
            {
                foreach (var item in list.ListItems)
                foreach (var nested in EnumerateBlocks(item.Blocks))
                    yield return nested;
            }
        }
    }

    private static IEnumerable<Inline> EnumerateInlines(InlineCollection inlines)
    {
        foreach (var inline in inlines)
        {
            yield return inline;
            if (inline is Span span)
            {
                foreach (var nested in EnumerateInlines(span.Inlines))
                    yield return nested;
            }
        }
    }
}
