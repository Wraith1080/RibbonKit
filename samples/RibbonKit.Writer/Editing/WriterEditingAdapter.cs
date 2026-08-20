using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.ComponentModel;

namespace RibbonKit.Writer.Editing;

/// <summary>
/// Coordinates Writer-owned formatting commands over a native <see cref="RichTextBox"/>.
/// </summary>
/// <remarks>
/// The adapter deliberately leaves document ownership, selection ownership, IME handling, clipboard
/// storage and the native undo stack with WPF. It only installs command bindings, reads a snapshot of
/// the current selection, and applies a requested operation when the caller executes a command. A
/// mixed selection is reported as mixed; observing <see cref="State"/> never applies a formatting
/// value to the document.
/// </remarks>
public sealed class WriterEditingAdapter : IDisposable
{
    /// <summary>The indentation step used by Increase/Decrease Indentation.</summary>
    public const double IndentationStep = 18d;

    private readonly List<CommandBinding> _commandBindings = new();
    private readonly DependencyPropertyDescriptor? _isEnabledDescriptor;
    private readonly DependencyPropertyDescriptor? _isReadOnlyDescriptor;
    private bool _disposed;
    private bool _refreshingState;

    /// <summary>Creates an adapter over an existing native editor.</summary>
    /// <param name="editor">The editor whose document and selection remain application-owned.</param>
    public WriterEditingAdapter(RichTextBox editor)
    {
        Editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _isEnabledDescriptor = DependencyPropertyDescriptor.FromProperty(
            UIElement.IsEnabledProperty, typeof(UIElement));
        _isReadOnlyDescriptor = DependencyPropertyDescriptor.FromProperty(
            TextBoxBase.IsReadOnlyProperty, typeof(TextBoxBase));
        AddCommandBindings();
        Editor.SelectionChanged += EditorStateChanged;
        Editor.TextChanged += EditorStateChanged;
        _isEnabledDescriptor?.AddValueChanged(Editor, EditorAvailabilityChanged);
        _isReadOnlyDescriptor?.AddValueChanged(Editor, EditorAvailabilityChanged);
        State = BuildState();
        CommandManager.RequerySuggested += OnCommandManagerRequerySuggested;
    }

    /// <summary>Gets the native editor controlled by this adapter.</summary>
    public RichTextBox Editor { get; }

    /// <summary>Gets the latest immutable selection and command-state snapshot.</summary>
    public WriterEditingState State { get; private set; }

    /// <summary>Raised after selection, document, or command-state inputs change.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Re-reads selection, availability, and clipboard state without changing the document.</summary>
    /// <remarks>
    /// This is the deterministic explicit refresh seam for clipboard state. WPF does not expose a
    /// document-level clipboard-changed event; command requery also refreshes the snapshot when it is
    /// raised, while direct <see cref="CanExecute"/> queries always read the live editor state.
    /// </remarks>
    public void RefreshState()
    {
        ThrowIfDisposed();
        RefreshStateCore(invalidateRequery: true);
    }

    /// <summary>
    /// Queries a command against the adapter's editor. This is useful for ribbon view models that do
    /// not want to depend on a focused-window command target.
    /// </summary>
    public bool CanExecute(ICommand command, object? parameter = null)
    {
        ArgumentNullException.ThrowIfNull(command);
        ThrowIfDisposed();
        return CanExecuteCore(command, parameter);
    }

    /// <summary>Executes a routed command when its current native-editor state allows it.</summary>
    /// <returns><see langword="true"/> when the command was accepted for execution.</returns>
    public bool TryExecute(ICommand command, object? parameter = null)
    {
        ArgumentNullException.ThrowIfNull(command);
        ThrowIfDisposed();
        if (!CanExecuteCore(command, parameter))
            return false;
        ExecuteCore(command, parameter);
        return true;
    }

    /// <summary>Applies a font family to the current selection or insertion point.</summary>
    public void ApplyFontFamily(FontFamily family)
    {
        ArgumentNullException.ThrowIfNull(family);
        ApplyInlineProperty(TextElement.FontFamilyProperty, family);
    }

    /// <summary>Applies a font size in device-independent units.</summary>
    public void ApplyFontSize(double size)
    {
        if (!IsValidFontSize(size, out _))
            throw new ArgumentOutOfRangeException(nameof(size), size, "Font size is outside WPF's valid range.");
        ApplyInlineProperty(TextElement.FontSizeProperty, size);
    }

    /// <summary>Toggles bold without choosing a value merely because the selection is mixed.</summary>
    public void ToggleBold() => ApplyInlineProperty(TextElement.FontWeightProperty,
        State.Bold.IsUniform && State.Bold.Value ? FontWeights.Normal : FontWeights.Bold);

    /// <summary>Toggles italic without choosing a value merely because the selection is mixed.</summary>
    public void ToggleItalic() => ApplyInlineProperty(TextElement.FontStyleProperty,
        State.Italic.IsUniform && State.Italic.Value ? FontStyles.Normal : FontStyles.Italic);

    /// <summary>Toggles underline without choosing a value merely because the selection is mixed.</summary>
    public void ToggleUnderline() => ApplyInlineProperty(Inline.TextDecorationsProperty,
        State.Underline.IsUniform && State.Underline.Value ? null : TextDecorations.Underline);

    /// <summary>Applies a solid foreground colour or clears it with a <see langword="null"/> value.</summary>
    public void ApplyForeground(object? brushOrColor) =>
        ApplyInlineProperty(TextElement.ForegroundProperty, ToBrush(brushOrColor, allowNull: true));

    /// <summary>Applies a solid highlight colour or clears it with a <see langword="null"/> value.</summary>
    public void ApplyHighlight(object? brushOrColor) =>
        ApplyInlineProperty(TextElement.BackgroundProperty, ToBrush(brushOrColor, allowNull: true));

    /// <summary>Sets alignment on all paragraphs touched by the current selection.</summary>
    public void SetAlignment(TextAlignment alignment)
    {
        if (!Enum.IsDefined(alignment))
            throw new ArgumentOutOfRangeException(nameof(alignment), alignment, "Unknown text alignment.");
        ApplyParagraphProperty(Paragraph.TextAlignmentProperty, alignment);
    }

    /// <summary>Sets the left indentation on all paragraphs touched by the current selection.</summary>
    public void SetIndentation(double indentation)
    {
        ValidateDimension(indentation, nameof(indentation));
        ApplyParagraphMargins(margin => margin with { Left = indentation });
    }

    /// <summary>Increases the left indentation of each touched paragraph.</summary>
    public void IncreaseIndentation() => AdjustIndentation(IndentationStep);

    /// <summary>Decreases the left indentation of each touched paragraph without going below zero.</summary>
    public void DecreaseIndentation() => AdjustIndentation(-IndentationStep);

    /// <summary>Sets paragraph spacing before all paragraphs touched by the current selection.</summary>
    public void SetParagraphSpacingBefore(double spacing)
    {
        ValidateDimension(spacing, nameof(spacing));
        ApplyParagraphMargins(margin => margin with { Top = spacing });
    }

    /// <summary>Sets paragraph spacing after all paragraphs touched by the current selection.</summary>
    public void SetParagraphSpacingAfter(double spacing)
    {
        ValidateDimension(spacing, nameof(spacing));
        ApplyParagraphMargins(margin => margin with { Bottom = spacing });
    }

    /// <summary>Copies the current selection through the native RichTextBox clipboard path.</summary>
    public void Copy()
    {
        if (!CanCopy)
            return;
        Editor.Copy();
        RefreshState();
    }

    /// <summary>Cuts the current selection through the native RichTextBox clipboard path.</summary>
    public void Cut()
    {
        if (!CanCut)
            return;
        Editor.Cut();
        RefreshState();
    }

    /// <summary>Pastes the current system clipboard through the native RichTextBox clipboard path.</summary>
    public void Paste()
    {
        if (!CanPaste)
            return;
        Editor.Paste();
        RefreshState();
    }

    /// <summary>Undoes the latest native editor undo unit.</summary>
    public void Undo()
    {
        if (!CanUndo)
            return;
        Editor.Undo();
        RefreshState();
    }

    /// <summary>Redoes the latest native editor undo unit.</summary>
    public void Redo()
    {
        if (!CanRedo)
            return;
        Editor.Redo();
        RefreshState();
    }

    /// <summary>Removes the adapter's event handlers and command bindings.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        CommandManager.RequerySuggested -= OnCommandManagerRequerySuggested;
        _isEnabledDescriptor?.RemoveValueChanged(Editor, EditorAvailabilityChanged);
        _isReadOnlyDescriptor?.RemoveValueChanged(Editor, EditorAvailabilityChanged);
        Editor.SelectionChanged -= EditorStateChanged;
        Editor.TextChanged -= EditorStateChanged;
        foreach (var binding in _commandBindings)
            Editor.CommandBindings.Remove(binding);
        _commandBindings.Clear();
    }

    private void AddCommandBindings()
    {
        AddBinding(ApplicationCommands.Copy, (_, e) => Copy(), (_, e) => e.CanExecute = CanCopy);
        AddBinding(ApplicationCommands.Cut, (_, e) => Cut(), (_, e) => e.CanExecute = CanCut);
        AddBinding(ApplicationCommands.Paste, (_, e) => Paste(), (_, e) => e.CanExecute = CanPaste);
        AddBinding(ApplicationCommands.Undo, (_, e) => Undo(), (_, e) => e.CanExecute = CanUndo);
        AddBinding(ApplicationCommands.Redo, (_, e) => Redo(), (_, e) => e.CanExecute = CanRedo);
        AddBinding(ApplicationCommands.SelectAll, (_, e) =>
        {
            if (CanSelectAll)
                Editor.SelectAll();
        }, (_, e) => e.CanExecute = CanSelectAll);

        AddBinding(EditingCommands.ToggleBold, (_, e) => ToggleBold(), (_, e) => e.CanExecute = CanFormat);
        AddBinding(EditingCommands.ToggleItalic, (_, e) => ToggleItalic(), (_, e) => e.CanExecute = CanFormat);
        AddBinding(EditingCommands.ToggleUnderline, (_, e) => ToggleUnderline(), (_, e) => e.CanExecute = CanFormat);
        AddBinding(WriterEditingCommands.ApplyFontFamily, (_, e) => ApplyFontFamily(ParseFontFamily(e.Parameter)),
            (_, e) => e.CanExecute = CanFormat && CanParseFontFamily(e.Parameter));
        AddBinding(WriterEditingCommands.ApplyFontSize, (_, e) => ApplyFontSize(ParseFontSize(e.Parameter)),
            (_, e) => e.CanExecute = CanFormat && CanParseFontSize(e.Parameter));
        AddBinding(WriterEditingCommands.ApplyForeground, (_, e) => ApplyForeground(e.Parameter),
            (_, e) => e.CanExecute = CanFormat && CanParseBrush(e.Parameter));
        AddBinding(WriterEditingCommands.ApplyHighlight, (_, e) => ApplyHighlight(e.Parameter),
            (_, e) => e.CanExecute = CanFormat && CanParseBrush(e.Parameter));
        AddBinding(WriterEditingCommands.SetAlignment, (_, e) => SetAlignment(ParseAlignment(e.Parameter)),
            (_, e) => e.CanExecute = CanFormat && CanParseAlignment(e.Parameter));
        AddBinding(WriterEditingCommands.SetIndentation, (_, e) => SetIndentation(ParseDimension(e.Parameter, nameof(e.Parameter))),
            (_, e) => e.CanExecute = CanFormat && CanParseDimension(e.Parameter));
        AddBinding(WriterEditingCommands.IncreaseIndentation, (_, e) => IncreaseIndentation(),
            (_, e) => e.CanExecute = CanFormat);
        AddBinding(WriterEditingCommands.DecreaseIndentation, (_, e) => DecreaseIndentation(),
            (_, e) => e.CanExecute = CanFormat);
        AddBinding(WriterEditingCommands.SetParagraphSpacingBefore,
            (_, e) => SetParagraphSpacingBefore(ParseDimension(e.Parameter, nameof(e.Parameter))),
            (_, e) => e.CanExecute = CanFormat && CanParseDimension(e.Parameter));
        AddBinding(WriterEditingCommands.SetParagraphSpacingAfter,
            (_, e) => SetParagraphSpacingAfter(ParseDimension(e.Parameter, nameof(e.Parameter))),
            (_, e) => e.CanExecute = CanFormat && CanParseDimension(e.Parameter));
        AddBinding(WriterEditingCommands.ToggleBullets, (_, e) => ToggleNativeList(EditingCommands.ToggleBullets),
            (_, e) => e.CanExecute = CanFormat);
        AddBinding(WriterEditingCommands.ToggleNumbering, (_, e) => ToggleNativeList(EditingCommands.ToggleNumbering),
            (_, e) => e.CanExecute = CanFormat);
    }

    private bool CanExecuteCore(ICommand command, object? parameter)
    {
        if (command == ApplicationCommands.Copy)
            return CanCopy;
        if (command == ApplicationCommands.Cut)
            return CanCut;
        if (command == ApplicationCommands.Paste)
            return CanPaste;
        if (command == ApplicationCommands.Undo)
            return CanUndo;
        if (command == ApplicationCommands.Redo)
            return CanRedo;
        if (command == ApplicationCommands.SelectAll)
            return CanSelectAll;
        if (command == EditingCommands.ToggleBold || command == EditingCommands.ToggleItalic ||
            command == EditingCommands.ToggleUnderline || command == WriterEditingCommands.IncreaseIndentation ||
            command == WriterEditingCommands.DecreaseIndentation || command == WriterEditingCommands.ToggleBullets ||
            command == WriterEditingCommands.ToggleNumbering)
            return CanFormat;
        if (command == WriterEditingCommands.ApplyFontFamily)
            return CanFormat && CanParseFontFamily(parameter);
        if (command == WriterEditingCommands.ApplyFontSize)
            return CanFormat && CanParseFontSize(parameter);
        if (command == WriterEditingCommands.ApplyForeground || command == WriterEditingCommands.ApplyHighlight)
            return CanFormat && CanParseBrush(parameter);
        if (command == WriterEditingCommands.SetAlignment)
            return CanFormat && CanParseAlignment(parameter);
        if (command == WriterEditingCommands.SetIndentation ||
            command == WriterEditingCommands.SetParagraphSpacingBefore ||
            command == WriterEditingCommands.SetParagraphSpacingAfter)
            return CanFormat && CanParseDimension(parameter);
        return false;
    }

    private void ExecuteCore(ICommand command, object? parameter)
    {
        if (command == ApplicationCommands.Copy) Copy();
        else if (command == ApplicationCommands.Cut) Cut();
        else if (command == ApplicationCommands.Paste) Paste();
        else if (command == ApplicationCommands.Undo) Undo();
        else if (command == ApplicationCommands.Redo) Redo();
        else if (command == ApplicationCommands.SelectAll && CanSelectAll)
            Editor.SelectAll();
        else if (command == EditingCommands.ToggleBold) ToggleBold();
        else if (command == EditingCommands.ToggleItalic) ToggleItalic();
        else if (command == EditingCommands.ToggleUnderline) ToggleUnderline();
        else if (command == WriterEditingCommands.ApplyFontFamily) ApplyFontFamily(ParseFontFamily(parameter));
        else if (command == WriterEditingCommands.ApplyFontSize) ApplyFontSize(ParseFontSize(parameter));
        else if (command == WriterEditingCommands.ApplyForeground) ApplyForeground(parameter);
        else if (command == WriterEditingCommands.ApplyHighlight) ApplyHighlight(parameter);
        else if (command == WriterEditingCommands.SetAlignment) SetAlignment(ParseAlignment(parameter));
        else if (command == WriterEditingCommands.SetIndentation) SetIndentation(ParseDimension(parameter, nameof(parameter)));
        else if (command == WriterEditingCommands.IncreaseIndentation) IncreaseIndentation();
        else if (command == WriterEditingCommands.DecreaseIndentation) DecreaseIndentation();
        else if (command == WriterEditingCommands.SetParagraphSpacingBefore)
            SetParagraphSpacingBefore(ParseDimension(parameter, nameof(parameter)));
        else if (command == WriterEditingCommands.SetParagraphSpacingAfter)
            SetParagraphSpacingAfter(ParseDimension(parameter, nameof(parameter)));
        else if (command == WriterEditingCommands.ToggleBullets) ToggleNativeList(EditingCommands.ToggleBullets);
        else if (command == WriterEditingCommands.ToggleNumbering) ToggleNativeList(EditingCommands.ToggleNumbering);
    }

    private void AddBinding(ICommand command, ExecutedRoutedEventHandler execute,
        CanExecuteRoutedEventHandler canExecute)
    {
        ExecutedRoutedEventHandler handledExecute = (sender, args) =>
        {
            execute(sender, args);
            args.Handled = true;
        };
        CanExecuteRoutedEventHandler handledCanExecute = (sender, args) =>
        {
            canExecute(sender, args);
            args.Handled = true;
        };
        var binding = new CommandBinding(command, handledExecute, handledCanExecute);
        _commandBindings.Add(binding);
        Editor.CommandBindings.Add(binding);
    }

    private void ToggleNativeList(RoutedCommand command)
    {
        if (!CanFormat)
            return;
        using (BeginChange())
            command.Execute(null, Editor);
        RefreshState();
    }

    private void ApplyInlineProperty(DependencyProperty property, object? value)
    {
        ThrowIfDisposed();
        if (!CanFormat)
            return;
        using (BeginChange())
            new TextRange(Editor.Selection.Start, Editor.Selection.End).ApplyPropertyValue(property, value);
        RefreshState();
    }

    private void ApplyParagraphProperty(DependencyProperty property, object value)
    {
        ThrowIfDisposed();
        if (!CanFormat)
            return;
        using (BeginChange())
        {
            var paragraphs = GetSelectedParagraphs();
            if (paragraphs.Count == 0)
                new TextRange(Editor.Selection.Start, Editor.Selection.End).ApplyPropertyValue(property, value);
            else
                foreach (var paragraph in paragraphs)
                    new TextRange(paragraph.ContentStart, paragraph.ContentEnd).ApplyPropertyValue(property, value);
        }
        RefreshState();
    }

    private void ApplyParagraphMargins(Func<Thickness, Thickness> update)
    {
        ThrowIfDisposed();
        if (!CanFormat)
            return;
        ArgumentNullException.ThrowIfNull(update);
        using (BeginChange())
        {
            var paragraphs = GetSelectedParagraphs();
            if (paragraphs.Count == 0)
            {
                var range = new TextRange(Editor.Selection.Start, Editor.Selection.End);
                var current = ReadThickness(range.GetPropertyValue(Paragraph.MarginProperty));
                range.ApplyPropertyValue(Paragraph.MarginProperty, update(current));
            }
            else
            {
                foreach (var paragraph in paragraphs)
                {
                    var range = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
                    range.ApplyPropertyValue(Paragraph.MarginProperty, update(NormalizeThickness(paragraph.Margin)));
                }
            }
        }
        RefreshState();
    }

    private void AdjustIndentation(double delta)
    {
        ApplyParagraphMargins(margin => margin with { Left = Math.Max(0, margin.Left + delta) });
    }

    private IDisposable BeginChange()
    {
        Editor.BeginChange();
        return new ChangeScope(Editor);
    }

    private WriterEditingState BuildState()
    {
        var hasSelection = HasSelection;
        var hasTextContext = HasDocumentText();
        var contextRange = TryGetInspectionRange();
        if (contextRange is not null)
            hasTextContext = HasTextCharacters(contextRange.Text);

        var paragraphs = hasTextContext ? GetSelectedParagraphs() : new List<Paragraph>();
        var inline = contextRange is null || !hasTextContext;
        var fontFamily = inline ? WriterSelectionValue<FontFamily>.Unset() : ReadInline<FontFamily>(contextRange!, TextElement.FontFamilyProperty, TryFontFamily);
        var fontSize = inline ? WriterSelectionValue<double>.Unset() : ReadInline<double>(contextRange!, TextElement.FontSizeProperty, TryDouble);
        var bold = inline ? WriterSelectionValue<bool>.Unset() : ReadInline<bool>(contextRange!, TextElement.FontWeightProperty, TryBold);
        var italic = inline ? WriterSelectionValue<bool>.Unset() : ReadInline<bool>(contextRange!, TextElement.FontStyleProperty, TryItalic);
        var underline = inline ? WriterSelectionValue<bool>.Unset() : ReadInline<bool>(contextRange!, Inline.TextDecorationsProperty, TryUnderline);
        var foreground = inline ? WriterSelectionValue<Color?>.Unset() :
            ReadInline<Color?>(contextRange!, TextElement.ForegroundProperty, TryColor, unsupportedValue: true);
        var highlight = inline ? WriterSelectionValue<Color?>.Unset() :
            ReadInline<Color?>(contextRange!, TextElement.BackgroundProperty, TryColor, unsupportedValue: true);
        var alignment = ReadParagraph(paragraphs, p => p.TextAlignment);
        var indentation = ReadParagraph(paragraphs, p => NormalizeLength(p.Margin.Left));
        var spacingBefore = ReadParagraph(paragraphs, p => NormalizeLength(p.Margin.Top));
        var spacingAfter = ReadParagraph(paragraphs, p => NormalizeLength(p.Margin.Bottom));
        var listKind = ReadListKind(paragraphs);

        return new WriterEditingState(
            hasSelection,
            hasTextContext,
            isEnabled: Editor.IsEnabled,
            isReadOnly: Editor.IsReadOnly,
            canFormat: CanFormat,
            canCopy: CanCopy,
            canCut: CanCut,
            canPaste: CanPaste,
            canSelectAll: CanSelectAll,
            canUndo: CanUndo,
            canRedo: CanRedo,
            fontFamily,
            fontSize,
            bold,
            italic,
            underline,
            foreground,
            highlight,
            alignment,
            indentation,
            spacingBefore,
            spacingAfter,
            listKind);
    }

    private WriterSelectionValue<T> ReadInline<T>(TextRange range, DependencyProperty property,
        TryValue<T> convert, bool unsupportedValue = false)
    {
        object value;
        try { value = range.GetPropertyValue(property); }
        catch (ArgumentException) { return HasSelection ? WriterSelectionValue<T>.Mixed() : WriterSelectionValue<T>.Unset(); }
        if (ReferenceEquals(value, DependencyProperty.UnsetValue))
            return HasSelection ? WriterSelectionValue<T>.Mixed() : WriterSelectionValue<T>.Unset();
        if (convert(value, out var converted))
            return WriterSelectionValue<T>.Uniform(converted);
        return unsupportedValue ? WriterSelectionValue<T>.Unsupported() : WriterSelectionValue<T>.Mixed();
    }

    private static WriterSelectionValue<T> ReadParagraph<T>(IReadOnlyList<Paragraph> paragraphs,
        Func<Paragraph, T> read)
    {
        if (paragraphs.Count == 0)
            return WriterSelectionValue<T>.Unset();
        var first = read(paragraphs[0]);
        for (var index = 1; index < paragraphs.Count; index++)
        {
            if (!EqualityComparer<T>.Default.Equals(first, read(paragraphs[index])))
                return WriterSelectionValue<T>.Mixed();
        }
        return WriterSelectionValue<T>.Uniform(first);
    }

    private static WriterSelectionValue<WriterListKind> ReadListKind(IReadOnlyList<Paragraph> paragraphs)
    {
        if (paragraphs.Count == 0)
            return WriterSelectionValue<WriterListKind>.Unset();
        var first = GetListKind(paragraphs[0]);
        for (var index = 1; index < paragraphs.Count; index++)
        {
            if (first != GetListKind(paragraphs[index]))
                return WriterSelectionValue<WriterListKind>.Mixed();
        }
        return WriterSelectionValue<WriterListKind>.Uniform(first);
    }

    private static WriterListKind GetListKind(Paragraph paragraph)
    {
        for (DependencyObject? current = paragraph.Parent; current is not null; current =
             (current as FrameworkContentElement)?.Parent)
        {
            if (current is not List list)
                continue;
            return list.MarkerStyle is TextMarkerStyle.Decimal or TextMarkerStyle.UpperRoman or
                TextMarkerStyle.LowerRoman or TextMarkerStyle.UpperLatin or TextMarkerStyle.LowerLatin
                ? WriterListKind.Numbered
                : WriterListKind.Bulleted;
        }
        return WriterListKind.None;
    }

    private List<Paragraph> GetSelectedParagraphs()
    {
        var result = new List<Paragraph>();
        var start = Editor.Selection.Start;
        var end = Editor.Selection.End;
        if (start.CompareTo(end) > 0)
        {
            (start, end) = (end, start);
        }

        var seen = new HashSet<Paragraph>();
        void AddParagraph(Paragraph? paragraph)
        {
            if (paragraph is not null && seen.Add(paragraph))
                result.Add(paragraph);
        }

        if (start.CompareTo(end) == 0)
        {
            AddParagraph(start.Paragraph);
            return result;
        }

        var cursor = start;
        while (cursor.CompareTo(end) < 0)
        {
            AddParagraph(cursor.Paragraph);
            var next = cursor.GetNextContextPosition(LogicalDirection.Forward);
            if (next is null || next.CompareTo(cursor) <= 0)
                break;
            cursor = next;
        }
        return result;
    }

    private TextRange? TryGetInspectionRange()
    {
        if (HasSelection)
            return new TextRange(Editor.Selection.Start, Editor.Selection.End);
        var position = Editor.Selection.Start;
        var before = position.GetNextInsertionPosition(LogicalDirection.Backward);
        if (before is not null && before.CompareTo(position) < 0)
        {
            var range = new TextRange(before, position);
            if (HasTextCharacters(range.Text))
                return range;
        }
        var after = position.GetNextInsertionPosition(LogicalDirection.Forward);
        if (after is not null && position.CompareTo(after) < 0)
        {
            var range = new TextRange(position, after);
            if (HasTextCharacters(range.Text))
                return range;
        }
        return null;
    }

    private bool CanFormat => Editor.IsEnabled && !Editor.IsReadOnly;
    private bool CanCopy => Editor.IsEnabled && HasSelection;
    private bool CanCut => CanFormat && HasSelection;
    private bool CanPaste => CanFormat && CanReadClipboard();
    private bool CanSelectAll => Editor.IsEnabled && HasDocumentText();
    private bool CanUndo => CanFormat && Editor.CanUndo;
    private bool CanRedo => CanFormat && Editor.CanRedo;
    private bool HasSelection => Editor.Selection.Start.CompareTo(Editor.Selection.End) != 0;

    private bool HasDocumentText()
    {
        var cursor = Editor.Document.ContentStart;
        var end = Editor.Document.ContentEnd;
        while (cursor.CompareTo(end) < 0)
        {
            if (cursor.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                return true;
            var next = cursor.GetNextContextPosition(LogicalDirection.Forward);
            if (next is null || next.CompareTo(cursor) <= 0)
                break;
            cursor = next;
        }
        return false;
    }

    private static bool HasTextCharacters(string? text) => !string.IsNullOrEmpty(text) &&
        text.Any(character => character is not '\r' and not '\n');

    private bool CanReadClipboard()
    {
        try
        {
            return Clipboard.ContainsData(DataFormats.Rtf) || Clipboard.ContainsData(DataFormats.Xaml) ||
                Clipboard.ContainsData(DataFormats.XamlPackage) || Clipboard.ContainsText();
        }
        catch (ExternalException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void EditorStateChanged(object? sender, RoutedEventArgs e) => RefreshState();

    private void EditorStateChanged(object? sender, TextChangedEventArgs e) => RefreshState();

    private void EditorAvailabilityChanged(object? sender, EventArgs e) => RefreshState();

    private void OnCommandManagerRequerySuggested(object? sender, EventArgs e)
    {
        if (_disposed || _refreshingState)
            return;
        if (!Editor.Dispatcher.CheckAccess())
        {
            _ = Editor.Dispatcher.BeginInvoke(
                DispatcherPriority.DataBind,
                new Action(() => RefreshStateCore(invalidateRequery: false)));
            return;
        }
        RefreshStateCore(invalidateRequery: false);
    }

    private void RefreshStateCore(bool invalidateRequery)
    {
        if (_disposed || _refreshingState)
            return;
        _refreshingState = true;
        try
        {
            State = BuildState();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _refreshingState = false;
        }
        if (invalidateRequery && !_disposed)
            CommandManager.InvalidateRequerySuggested();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WriterEditingAdapter));
    }

    private sealed class ChangeScope(RichTextBox editor) : IDisposable
    {
        public void Dispose() => editor.EndChange();
    }

    private delegate bool TryValue<T>(object? input, out T value);

    private static bool TryFontFamily(object? input, out FontFamily value)
    {
        if (input is FontFamily family)
        {
            value = family;
            return true;
        }
        value = null!;
        return false;
    }

    private static bool TryDouble(object? input, out double value)
    {
        if (input is double number && !double.IsNaN(number) && !double.IsInfinity(number))
        {
            value = number;
            return true;
        }
        value = default;
        return false;
    }

    private static bool TryBold(object? input, out bool value)
    {
        if (input is FontWeight weight)
        {
            value = weight.ToOpenTypeWeight() >= FontWeights.SemiBold.ToOpenTypeWeight();
            return true;
        }
        value = default;
        return false;
    }

    private static bool TryItalic(object? input, out bool value)
    {
        if (input is FontStyle style)
        {
            value = style == FontStyles.Italic;
            return true;
        }
        value = default;
        return false;
    }

    private static bool TryUnderline(object? input, out bool value)
    {
        if (input is null)
        {
            value = false;
            return true;
        }
        if (input is TextDecorationCollection decorations)
        {
            value = decorations.Any(decoration => decoration.Location == TextDecorationLocation.Underline);
            return true;
        }
        value = default;
        return false;
    }

    private static bool TryColor(object? input, out Color? value)
    {
        if (input is null)
        {
            value = null;
            return true;
        }
        if (input is SolidColorBrush solid)
        {
            value = solid.Color;
            return true;
        }
        value = null;
        return false;
    }

    private static bool TryParseFontFamily(object? input, out FontFamily value)
    {
        if (input is FontFamily family)
        {
            value = family;
            return true;
        }
        if (input is string name && !string.IsNullOrWhiteSpace(name))
        {
            try
            {
                value = new FontFamily(name);
                return true;
            }
            catch (ArgumentException)
            {
                // An invalid family is rejected by CanExecute rather than by a command callback.
            }
        }
        value = null!;
        return false;
    }

    private static bool CanParseFontFamily(object? input) => TryParseFontFamily(input, out FontFamily _);

    private static FontFamily ParseFontFamily(object? input) =>
        TryParseFontFamily(input, out var family)
            ? family
            : throw new ArgumentException("A valid font family or family name is required.", nameof(input));

    private static bool TryParseFontSize(object? input, out double value)
    {
        if (input is double number)
            return IsValidFontSize(number, out value);
        if (input is float single)
            return IsValidFontSize(single, out value);
        if (input is string text && double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out number))
            return IsValidFontSize(number, out value);
        value = default;
        return false;
    }

    private static double ParseFontSize(object? input) =>
        TryParseFontSize(input, out var size)
            ? size
            : throw new ArgumentException("A font size within WPF's valid range is required.", nameof(input));

    private static bool CanParseFontSize(object? input) => TryParseFontSize(input, out double _);

    private static bool TryParseBrush(object? input, bool allowNull, out Brush? brush)
    {
        if (input is null && allowNull)
        {
            brush = null;
            return true;
        }
        if (input is SolidColorBrush value)
        {
            brush = value;
            return true;
        }
        if (input is Brush)
        {
            brush = null;
            return false;
        }
        if (input is Color color)
        {
            brush = new SolidColorBrush(color);
            return true;
        }
        if (input is string text && !string.IsNullOrWhiteSpace(text))
        {
            try
            {
                brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(text)!);
                return true;
            }
            catch (Exception exception) when (exception is FormatException or InvalidOperationException or ArgumentException or InvalidCastException)
            {
                // Invalid colour strings are reported through CanExecute.
            }
        }
        brush = null;
        return false;
    }

    private static Brush? ToBrush(object? input, bool allowNull) =>
            TryParseBrush(input, allowNull, out var brush)
            ? brush
            : throw new ArgumentException("A Color, SolidColorBrush, supported colour string, or null is required.", nameof(input));

    private static bool CanParseBrush(object? input) => TryParseBrush(input, allowNull: true, out Brush? _);

    private static bool TryParseAlignment(object? input, out TextAlignment alignment)
    {
        if (input is TextAlignment value && Enum.IsDefined(value))
        {
            alignment = value;
            return true;
        }
        if (input is string text && Enum.TryParse(text, ignoreCase: true, out value) && Enum.IsDefined(value))
        {
            alignment = value;
            return true;
        }
        alignment = default;
        return false;
    }

    private static TextAlignment ParseAlignment(object? input) =>
        TryParseAlignment(input, out var alignment)
            ? alignment
            : throw new ArgumentException("A valid TextAlignment is required.", nameof(input));

    private static bool CanParseAlignment(object? input) => TryParseAlignment(input, out TextAlignment _);

    private static bool TryParseDimension(object? input, out double value)
    {
        if (input is double number)
            return IsValidDimension(number, out value);
        if (input is float single)
            return IsValidDimension(single, out value);
        if (input is string text && double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out number))
            return IsValidDimension(number, out value);
        value = default;
        return false;
    }

    private static double ParseDimension(object? input, string parameterName) =>
        TryParseDimension(input, out var value)
            ? value
            : throw new ArgumentOutOfRangeException(parameterName, input, "A finite non-negative dimension is required.");

    private static bool CanParseDimension(object? input) => TryParseDimension(input, out double _);

    private static void ValidateDimension(double value, string parameterName)
    {
        if (!IsValidDimension(value, out _))
            throw new ArgumentOutOfRangeException(parameterName, value, "The dimension must be finite and non-negative.");
    }

    private static bool IsValidDimension(double value, out double normalized)
    {
        normalized = value;
        return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 &&
            Paragraph.MarginProperty.IsValidValue(new Thickness(value, 0, 0, 0));
    }

    private static bool IsValidFontSize(double value, out double normalized)
    {
        normalized = value;
        return TextElement.FontSizeProperty.IsValidValue(value);
    }

    private static Thickness ReadThickness(object value) =>
        value is Thickness thickness ? NormalizeThickness(thickness) : new Thickness();

    private static Thickness NormalizeThickness(Thickness thickness) => new(
        NormalizeLength(thickness.Left),
        NormalizeLength(thickness.Top),
        NormalizeLength(thickness.Right),
        NormalizeLength(thickness.Bottom));

    private static double NormalizeLength(double value) => double.IsNaN(value) ? 0 : value;
}
