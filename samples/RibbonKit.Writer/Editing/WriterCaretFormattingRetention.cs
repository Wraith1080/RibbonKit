using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace RibbonKit.Writer.Editing;

// WPF clears pending typing properties on mouse-down, even at the same caret.
// Preserve only explicit Writer formatting across a completed, stationary click.
internal sealed class WriterCaretFormattingRetention : IDisposable
{
    private static readonly DependencyProperty[] Properties =
    [
        TextElement.FontFamilyProperty, TextElement.FontSizeProperty,
        TextElement.FontWeightProperty, TextElement.FontStyleProperty, TextElement.FontStretchProperty,
        Inline.TextDecorationsProperty, Inline.BaselineAlignmentProperty,
        TextElement.ForegroundProperty, TextElement.BackgroundProperty
    ];
    private readonly RichTextBox _editor;
    private readonly Action _refresh;
    private TextPointer? _formattedCaret;
    private FlowDocument? _document;
    private object?[]? _clickValues;

    internal WriterCaretFormattingRetention(RichTextBox editor, Action refresh)
    {
        _editor = editor;
        _refresh = refresh;
        editor.PreviewMouseDown += OnMouseDown;
        editor.AddHandler(Mouse.MouseUpEvent, new MouseButtonEventHandler(OnMouseUp), handledEventsToo: true);
        editor.SelectionChanged += OnSelectionChanged;
        editor.TextChanged += OnTextChanged;
    }

    internal void Remember()
    {
        Clear();
        if (!_editor.Selection.IsEmpty) return;
        _document = _editor.Document;
        _formattedCaret = _editor.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
    }

    internal void Clear()
    {
        _formattedCaret = null;
        _document = null;
        _clickValues = null;
    }

    private bool IsSameCaret => _formattedCaret is not null && ReferenceEquals(_document, _editor.Document)
        && _editor.Selection.IsEmpty && _formattedCaret.CompareTo(
            _editor.CaretPosition.GetInsertionPosition(LogicalDirection.Forward)) == 0;

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _clickValues = null;
        if (e.ChangedButton != MouseButton.Left || Keyboard.Modifiers != ModifierKeys.None
            || !_editor.IsEnabled || _editor.IsReadOnly || !IsSameCaret) return;
        _clickValues = Properties.Select(_editor.Selection.GetPropertyValue).ToArray();
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        var values = _clickValues;
        _clickValues = null;
        if (values is null || !_editor.IsEnabled || _editor.IsReadOnly || !IsSameCaret) return;
        _editor.BeginChange();
        try
        {
            for (int i = 0; i < Properties.Length; i++)
                if (!ReferenceEquals(values[i], DependencyProperty.UnsetValue)
                    && !Equals(values[i], _editor.Selection.GetPropertyValue(Properties[i])))
                    _editor.Selection.ApplyPropertyValue(Properties[i], values[i]);
        }
        finally { _editor.EndChange(); }
        _refresh();
    }

    private void OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (!IsSameCaret) Clear();
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e) => Clear();

    public void Dispose()
    {
        _editor.PreviewMouseDown -= OnMouseDown;
        _editor.RemoveHandler(Mouse.MouseUpEvent, new MouseButtonEventHandler(OnMouseUp));
        _editor.SelectionChanged -= OnSelectionChanged;
        _editor.TextChanged -= OnTextChanged;
        Clear();
    }
}
