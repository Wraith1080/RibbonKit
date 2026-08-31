using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace RibbonKit.Writer.Editing;

/// <summary>Describes the direction requested by the editor's keyboard focus-exit seam.</summary>
public enum WriterEditorFocusDirection
{
    /// <summary>Move focus to the next focusable surface.</summary>
    Forward,

    /// <summary>Move focus to the previous focusable surface.</summary>
    Backward
}

/// <summary>Describes the deterministic handling decision for an editor Tab key.</summary>
public enum WriterParagraphTabAction
{
    /// <summary>Leave the key to WPF's normal focus or text-editing behavior.</summary>
    Unhandled,

    /// <summary>Insert one literal tab character into the current paragraph.</summary>
    InsertLiteralTab,

    /// <summary>Increase paragraph indentation or nest a list item.</summary>
    IncreaseIndentation,

    /// <summary>Decrease paragraph indentation or outdent a list item.</summary>
    DecreaseIndentation
}

/// <summary>Pure decision value used by <see cref="WriterParagraphKeyboardController"/>.</summary>
public readonly record struct WriterParagraphTabDecision(
    WriterParagraphTabAction Action,
    bool IsHandled)
{
    /// <summary>Returns whether the decision leaves the key unhandled.</summary>
    public bool IsUnhandled => !IsHandled || Action == WriterParagraphTabAction.Unhandled;

    /// <summary>Computes the Tab policy before any live WPF document mutation.</summary>
    /// <param name="inTableCell">Whether the current range belongs to a table cell.</param>
    /// <param name="atParagraphBoundary">Whether the caret or selection covers a paragraph boundary.</param>
    /// <param name="reverse">Whether Shift+Tab was pressed.</param>
    /// <param name="control">Whether Ctrl is the only active modifier.</param>
    public static WriterParagraphTabDecision Decide(
        bool inTableCell,
        bool atParagraphBoundary,
        bool reverse,
        bool control)
    {
        if (inTableCell)
            return new(WriterParagraphTabAction.Unhandled, false);

        if (control)
            return new(WriterParagraphTabAction.InsertLiteralTab, true);

        if (!atParagraphBoundary)
        {
            // RichTextBox.AcceptsTab is deliberately false in Writer. A plain mid-paragraph Tab
            // therefore needs an explicit literal-text path; reverse traversal remains available
            // to the host's normal focus contract until a paragraph boundary is reached.
            return reverse
                ? new(WriterParagraphTabAction.Unhandled, false)
                : new(WriterParagraphTabAction.InsertLiteralTab, true);
        }

        return reverse
            ? new(WriterParagraphTabAction.DecreaseIndentation, true)
            : new(WriterParagraphTabAction.IncreaseIndentation, true);
    }
}

/// <summary>
/// Projects paragraph-boundary Tab behavior onto a native Writer <see cref="RichTextBox"/>.
/// </summary>
/// <remarks>
/// The table controller is intentionally independent and remains first owner of table-cell Tab
/// routing. This controller only acts on ordinary paragraphs. Plain mid-paragraph Tab and Ctrl+Tab
/// are explicit literal-tab paths because Writer's editor keeps AcceptsTab false; mid-paragraph
/// Shift+Tab remains unhandled. The host can provide <see cref="FocusNavigationRequested"/> for a
/// deterministic F6 focus-exit path.
/// </remarks>
public sealed class WriterParagraphKeyboardController : IDisposable
{
    private readonly RichTextBox _editor;
    private readonly WriterTableInteractionController? _tableInteraction;
    private bool _attached;
    private bool _disposed;

    /// <summary>Creates and attaches a paragraph keyboard controller.</summary>
    /// <param name="editor">The native editor whose keyboard surface is controlled.</param>
    /// <param name="tableInteraction">
    /// The existing table controller, when present. It remains the sole table-cell Tab owner.
    /// </param>
    /// <param name="focusNavigationRequested">
    /// Optional callback invoked for F6 or Shift+F6. Return <see langword="true"/> when the host
    /// moved focus and the key should be marked handled.
    /// </param>
    public WriterParagraphKeyboardController(
        RichTextBox editor,
        WriterTableInteractionController? tableInteraction = null,
        Func<WriterEditorFocusDirection, bool>? focusNavigationRequested = null)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _tableInteraction = tableInteraction;
        FocusNavigationRequested = focusNavigationRequested;
        Attach();
    }

    /// <summary>Gets the native editor controlled by this instance.</summary>
    public RichTextBox Editor => _editor;

    /// <summary>Gets whether the preview-key handler is currently attached.</summary>
    public bool IsAttached => _attached;

    /// <summary>
    /// Gets or sets the host callback used to move focus away from the editor for F6.
    /// </summary>
    public Func<WriterEditorFocusDirection, bool>? FocusNavigationRequested { get; set; }

    /// <summary>Attaches the idempotent preview-key handler.</summary>
    public void Attach()
    {
        ThrowIfDisposed();
        if (_attached)
            return;

        _editor.PreviewKeyDown += OnPreviewKeyDown;
        _attached = true;
    }

    /// <summary>Detaches the preview-key handler without disposing the editor.</summary>
    public void Detach()
    {
        if (!_attached)
            return;

        _editor.PreviewKeyDown -= OnPreviewKeyDown;
        _attached = false;
    }

    /// <summary>
    /// Handles a live Tab request. This method is useful to hosts and tests that already own a
    /// keyboard event and want to preserve the same table and boundary policy.
    /// </summary>
    /// <param name="reverse">Whether the request is Shift+Tab.</param>
    /// <returns>
    /// <see langword="true"/> when a literal tab was inserted or a native paragraph mutation was
    /// performed.
    /// </returns>
    public bool TryHandleTab(bool reverse)
    {
        ThrowIfDisposed();
        var decision = WriterParagraphTabDecision.Decide(
            IsInTableCell(),
            IsAtParagraphBoundary(),
            reverse,
            control: false);
        if (!decision.IsHandled)
            return false;

        if (decision.Action == WriterParagraphTabAction.InsertLiteralTab)
            return TryInsertLiteralTab();

        var command = reverse
            ? EditingCommands.DecreaseIndentation
            : EditingCommands.IncreaseIndentation;
        if (!_editor.IsEnabled || _editor.IsReadOnly)
            return false;

        _editor.BeginChange();
        try
        {
            if (command.CanExecute(null, _editor))
            {
                command.Execute(null, _editor);
                return true;
            }

            return AdjustParagraphIndentation(reverse);
        }
        finally
        {
            _editor.EndChange();
        }
    }

    /// <summary>
    /// Inserts one literal tab into an ordinary paragraph, replacing the current selection when
    /// necessary. Table cells are rejected so the table controller retains that path.
    /// </summary>
    /// <returns><see langword="true"/> when the native insertion succeeded.</returns>
    public bool TryInsertLiteralTab()
    {
        ThrowIfDisposed();
        if (IsInTableCell() || !_editor.IsEnabled || _editor.IsReadOnly)
            return false;

        return WriterInlineInsertion.TryReplaceSelection(_editor, new Run("\t"));
    }

    /// <summary>Returns whether the current selection or caret is inside a WPF table cell.</summary>
    public bool IsInTableCell()
    {
        ThrowIfDisposed();
        if (_tableInteraction is not null &&
            (_tableInteraction.Tables.TryGetCell(_editor.Selection.Start, out _) ||
             _tableInteraction.Tables.TryGetCell(_editor.Selection.End, out _)))
            return true;

        return IsPointerInTableCell(_editor.Selection.Start) ||
            IsPointerInTableCell(_editor.Selection.End);
    }

    /// <summary>Checks a text pointer's logical ancestry without changing the live selection.</summary>
    public static bool IsPointerInTableCell(TextPointer? pointer)
    {
        for (DependencyObject? current = pointer?.Parent; current is not null;
             current = GetParent(current))
        {
            if (current is TableCell)
                return true;
        }

        return false;
    }

    /// <summary>Removes handlers and releases this controller's ownership of the editor event.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        Detach();
        _disposed = true;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_disposed || e.Handled)
            return;

        var modifiers = e.KeyboardDevice.Modifiers;
        if (e.Key == Key.F6 && (modifiers == ModifierKeys.None || modifiers == ModifierKeys.Shift))
        {
            var direction = modifiers == ModifierKeys.Shift
                ? WriterEditorFocusDirection.Backward
                : WriterEditorFocusDirection.Forward;
            if (FocusNavigationRequested?.Invoke(direction) == true)
                e.Handled = true;
            return;
        }

        if (e.Key != Key.Tab)
            return;

        // The table controller is attached before this controller in the Writer host. If its
        // first-cell reverse path deliberately leaves the event unhandled, this explicit check
        // still prevents paragraph routing from taking ownership of the table cell.
        if (IsInTableCell())
            return;

        if (modifiers == ModifierKeys.Control)
        {
            if (TryInsertLiteralTab())
                e.Handled = true;
            return;
        }

        if (modifiers != ModifierKeys.None && modifiers != ModifierKeys.Shift)
            return;

        if (TryHandleTab(modifiers == ModifierKeys.Shift))
            e.Handled = true;
    }

    private bool IsAtParagraphBoundary()
    {
        var start = _editor.Selection.Start;
        var end = _editor.Selection.End;
        if (start.CompareTo(end) > 0)
            (start, end) = (end, start);

        if (start.CompareTo(end) == 0)
        {
            var paragraph = FindParagraphAtOrNear(start, LogicalDirection.Forward) ??
                FindParagraphAtOrNear(start, LogicalDirection.Backward);
            return paragraph is not null &&
                (IsAtParagraphStart(start, paragraph) || IsAtParagraphEnd(start, paragraph));
        }

        var first = FindParagraphAtOrNear(start, LogicalDirection.Forward);
        var last = FindParagraphAtOrNear(end, LogicalDirection.Backward) ??
            FindParagraphAtOrNear(end, LogicalDirection.Forward);
        return first is not null && last is not null &&
            IsAtParagraphStart(start, first) && IsAtParagraphEnd(end, last);
    }

    private bool AdjustParagraphIndentation(bool reverse)
    {
        var paragraphs = GetTouchedParagraphs();
        if (paragraphs.Count == 0)
            return false;

        foreach (var paragraph in paragraphs)
        {
            var margin = paragraph.Margin;
            var left = double.IsNaN(margin.Left) ? 0 : margin.Left;
            var adjusted = reverse
                ? Math.Max(0, left - WriterEditingAdapter.IndentationStep)
                : left + WriterEditingAdapter.IndentationStep;
            new TextRange(paragraph.ContentStart, paragraph.ContentEnd).ApplyPropertyValue(
                Paragraph.MarginProperty,
                new Thickness(adjusted, margin.Top, margin.Right, margin.Bottom));
        }

        return true;
    }

    private IReadOnlyList<Paragraph> GetTouchedParagraphs()
    {
        var result = new List<Paragraph>();
        var seen = new HashSet<Paragraph>();
        var start = _editor.Selection.Start;
        var end = _editor.Selection.End;
        if (start.CompareTo(end) > 0)
            (start, end) = (end, start);

        var cursor = start;
        while (cursor.CompareTo(end) <= 0)
        {
            var paragraph = FindParagraphAtOrNear(cursor, LogicalDirection.Forward) ??
                FindParagraphAtOrNear(cursor, LogicalDirection.Backward);
            if (paragraph is not null && seen.Add(paragraph))
                result.Add(paragraph);
            if (cursor.CompareTo(end) == 0)
                break;
            var next = cursor.GetNextContextPosition(LogicalDirection.Forward);
            if (next is null || next.CompareTo(cursor) <= 0)
                break;
            cursor = next.CompareTo(end) > 0 ? end : next;
        }

        return result;
    }

    private static bool IsAtParagraphStart(TextPointer pointer, Paragraph paragraph)
    {
        var start = paragraph.ContentStart;
        var insertionStart = start.GetInsertionPosition(LogicalDirection.Forward);
        return pointer.CompareTo(start) == 0 || pointer.CompareTo(insertionStart) == 0;
    }

    private static bool IsAtParagraphEnd(TextPointer pointer, Paragraph paragraph)
    {
        var end = paragraph.ContentEnd;
        var insertionEnd = end.GetInsertionPosition(LogicalDirection.Backward);
        return pointer.CompareTo(end) == 0 || pointer.CompareTo(insertionEnd) == 0;
    }

    private static Paragraph? FindParagraphAtOrNear(TextPointer pointer, LogicalDirection direction)
    {
        var paragraph = WriterInlineInsertion.FindParagraph(pointer);
        if (paragraph is not null)
            return paragraph;

        var insertion = pointer.GetInsertionPosition(direction);
        return insertion is null ? null : WriterInlineInsertion.FindParagraph(insertion);
    }

    private static DependencyObject? GetParent(DependencyObject current) => current switch
    {
        FrameworkContentElement contentElement => contentElement.Parent,
        FrameworkElement frameworkElement => frameworkElement.Parent,
        _ => null
    };

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
