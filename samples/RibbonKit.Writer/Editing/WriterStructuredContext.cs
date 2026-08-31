using System.Windows.Controls;
using System.Windows.Documents;

namespace RibbonKit.Writer.Editing;

internal enum WriterStructuredContextKind
{
    Text,
    Table,
    Picture,
    Hyperlink
}

/// <summary>
/// One document-bound structured target captured before context-menu focus leaves the editor.
/// </summary>
internal sealed class WriterStructuredContextSnapshot
{
    internal WriterStructuredContextSnapshot(
        FlowDocument document,
        WriterEditorContextMenuTarget target,
        WriterStructuredContextKind kind,
        Table? table = null,
        TableCell? tableCell = null,
        InlineUIContainer? picture = null,
        Hyperlink? hyperlink = null)
    {
        Document = document;
        Target = target;
        Kind = kind;
        Table = table;
        TableCell = tableCell;
        Picture = picture;
        Hyperlink = hyperlink;
    }

    internal FlowDocument Document { get; }
    internal WriterEditorContextMenuTarget Target { get; }
    internal WriterStructuredContextKind Kind { get; }
    internal Table? Table { get; }
    internal TableCell? TableCell { get; }
    internal InlineUIContainer? Picture { get; }
    internal Hyperlink? Hyperlink { get; }
}

/// <summary>
/// Resolves and revalidates Writer structured targets without changing the native selection.
/// </summary>
internal sealed class WriterStructuredContextResolver
{
    private readonly RichTextBox _editor;
    private readonly WriterTableService _tables;

    internal WriterStructuredContextResolver(RichTextBox editor, WriterTableService tables)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _tables = tables ?? throw new ArgumentNullException(nameof(tables));
    }

    internal WriterStructuredContextSnapshot Capture(WriterEditorContextMenuTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var document = _editor.Document;
        if (!target.IsValidFor(_editor))
            return new WriterStructuredContextSnapshot(document, target,
                WriterStructuredContextKind.Text);

        var picture = WriterInlineInsertion.FindImage(document, target.Start, target.End);
        if (picture is not null)
        {
            return new WriterStructuredContextSnapshot(document, target,
                WriterStructuredContextKind.Picture, picture: picture);
        }

        var hyperlink = WriterInlineInsertion.FindHyperlink(document, target.Start, target.End);
        if (hyperlink is not null)
        {
            return new WriterStructuredContextSnapshot(document, target,
                WriterStructuredContextKind.Hyperlink, hyperlink: hyperlink);
        }

        if (_tables.TryGetCell(target.Start, out var first)
            && _tables.TryGetSelectionRange(target.Start, target.End, out var range)
            && ReferenceEquals(first.Table, range.Table))
        {
            return new WriterStructuredContextSnapshot(document, target,
                WriterStructuredContextKind.Table, first.Table, first.Cell);
        }

        return new WriterStructuredContextSnapshot(document, target,
            WriterStructuredContextKind.Text);
    }

    internal bool IsCurrent(WriterStructuredContextSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!ReferenceEquals(_editor.Document, snapshot.Document)
            || !snapshot.Target.IsValidFor(_editor))
            return false;

        return snapshot.Kind switch
        {
            WriterStructuredContextKind.Text => true,
            WriterStructuredContextKind.Table => TryGetTableCell(snapshot, out _),
            WriterStructuredContextKind.Picture => ReferenceEquals(snapshot.Picture,
                WriterInlineInsertion.FindImage(snapshot.Document,
                    snapshot.Target.Start, snapshot.Target.End)),
            WriterStructuredContextKind.Hyperlink => ReferenceEquals(snapshot.Hyperlink,
                WriterInlineInsertion.FindHyperlink(snapshot.Document,
                    snapshot.Target.Start, snapshot.Target.End)),
            _ => false
        };
    }

    internal bool TryGetTableCell(
        WriterStructuredContextSnapshot snapshot,
        out WriterTableCellReference reference)
    {
        reference = default;
        if (snapshot.Kind != WriterStructuredContextKind.Table
            || snapshot.Table is null || snapshot.TableCell is null
            || !ReferenceEquals(_editor.Document, snapshot.Document)
            || !snapshot.Target.IsValidFor(_editor)
            || !_tables.TryGetCell(snapshot.Target.Start, out var current))
            return false;

        if (!ReferenceEquals(current.Table, snapshot.Table)
            || !ReferenceEquals(current.Cell, snapshot.TableCell))
            return false;
        reference = current;
        return true;
    }

    internal bool TryGetTableRange(
        WriterStructuredContextSnapshot snapshot,
        out WriterTableRange range)
    {
        range = default;
        return TryGetTableCell(snapshot, out _)
            && _tables.TryGetSelectionRange(snapshot.Target.Start, snapshot.Target.End, out range)
            && ReferenceEquals(range.Table, snapshot.Table);
    }
}
