using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;

namespace RibbonKit.Writer.Editing;

/// <summary>Stable selection and spelling state captured before a Writer context menu opens.</summary>
public sealed class WriterEditorContextMenuTarget
{
    private readonly FlowDocument? _document;

    /// <summary>Creates a target from two pointers in the same live FlowDocument.</summary>
    public WriterEditorContextMenuTarget(
        TextPointer start,
        TextPointer end,
        SpellingError? spellingError = null)
        : this(start, end, document: null, spellingError)
    {
    }

    internal WriterEditorContextMenuTarget(
        TextPointer start,
        TextPointer end,
        FlowDocument? document,
        SpellingError? spellingError = null)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        if (start.CompareTo(end) <= 0)
        {
            Start = start;
            End = end;
        }
        else
        {
            Start = end;
            End = start;
        }

        SpellingError = spellingError;
        _document = document;
    }

    /// <summary>Gets the normalized selection start.</summary>
    public TextPointer Start { get; }

    /// <summary>Gets the normalized selection end.</summary>
    public TextPointer End { get; }

    /// <summary>Gets the spelling error under the captured caret or selection, when any.</summary>
    public SpellingError? SpellingError { get; }

    /// <summary>Gets whether the target contains selected document content.</summary>
    public bool HasSelection
    {
        get
        {
            try
            {
                return Start.CompareTo(End) != 0;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    /// <summary>Gets whether both pointers can still be compared in the live document.</summary>
    public bool IsValid
    {
        get
        {
            try
            {
                _ = Start.CompareTo(End);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    /// <summary>Restores this target as the editor's selection.</summary>
    public bool TryRestore(RichTextBox editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        if (!IsValidFor(editor))
            return false;

        try
        {
            editor.Selection.Select(Start, End);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>Gets whether this target still belongs to the editor document that captured it.</summary>
    public bool IsValidFor(RichTextBox editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        return IsValid && (_document is null || ReferenceEquals(editor.Document, _document));
    }
}

/// <summary>
/// Allows W3-E to append structured-object rows after the W1-E text menu without replacing its
/// target snapshot or duplicating the base menu.
/// </summary>
public sealed class WriterEditorContextMenuExtensionContext : EventArgs
{
    private readonly Func<string, ICommand, object?, MenuItem> _commandFactory;
    private readonly Func<string, Action<WriterEditorContextMenuTarget>, MenuItem> _callbackFactory;
    private readonly Func<string, Func<WriterEditorContextMenuTarget, bool>,
        Action<WriterEditorContextMenuTarget>, MenuItem> _guardedCallbackFactory;
    private readonly List<object> _items = new();

    internal WriterEditorContextMenuExtensionContext(
        ContextMenu menu,
        RichTextBox editor,
        WriterEditorContextMenuTarget target,
        Func<string, ICommand, object?, MenuItem> commandFactory,
        Func<string, Action<WriterEditorContextMenuTarget>, MenuItem> callbackFactory,
        Func<string, Func<WriterEditorContextMenuTarget, bool>,
            Action<WriterEditorContextMenuTarget>, MenuItem> guardedCallbackFactory)
    {
        Menu = menu;
        Editor = editor;
        Target = target;
        _commandFactory = commandFactory;
        _callbackFactory = callbackFactory;
        _guardedCallbackFactory = guardedCallbackFactory;
    }

    /// <summary>Gets the menu being populated.</summary>
    public ContextMenu Menu { get; }

    /// <summary>Gets the command target RichTextBox.</summary>
    public RichTextBox Editor { get; }

    /// <summary>Gets the stable target captured before popup focus moved.</summary>
    public WriterEditorContextMenuTarget Target { get; }

    /// <summary>Adds a structured-object menu item after the base text actions.</summary>
    public void AddItem(MenuItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.CommandTarget = Editor;
        _items.Add(item);
    }

    /// <summary>Adds a separator after the base text actions.</summary>
    public void AddSeparator() => _items.Add(new Separator());

    /// <summary>
    /// Creates a command item whose execution restores <see cref="Target"/> first and whose
    /// command target remains <see cref="Editor"/>.
    /// </summary>
    public MenuItem CreateCommandItem(string header, ICommand command, object? parameter = null) =>
        _commandFactory(header, command, parameter);

    /// <summary>Creates a callback item that restores <see cref="Target"/> before invocation.</summary>
    public MenuItem CreateCallbackItem(
        string header,
        Action<WriterEditorContextMenuTarget> callback) =>
        _callbackFactory(header, callback);

    /// <summary>
    /// Creates a callback item that revalidates the captured target immediately before execution.
    /// </summary>
    public MenuItem CreateCallbackItem(
        string header,
        Func<WriterEditorContextMenuTarget, bool> canExecute,
        Action<WriterEditorContextMenuTarget> callback)
    {
        ArgumentNullException.ThrowIfNull(canExecute);
        ArgumentNullException.ThrowIfNull(callback);
        return _guardedCallbackFactory(header, canExecute, callback);
    }

    internal IReadOnlyList<object> Items => _items;
}

/// <summary>Builds Writer's modern, target-stable text context menu for a RichTextBox.</summary>
/// <remarks>
/// The menu remains app-owned: the controller loads RibbonKit's keyed menu resource dictionary when
/// available, but it never changes RibbonKit runtime code. Base rows are text-only. Later W3-E code
/// can append table, picture or hyperlink rows through <see cref="ExtensionsRequested"/>.
/// </remarks>
public sealed class WriterEditorContextMenuController : IDisposable
{
    private static readonly Uri MenuResourceUri = new(
        "pack://application:,,,/RibbonKit;component/Themes/Menus.xaml",
        UriKind.Absolute);
    private readonly RichTextBox _editor;
    private readonly ContextMenu _menu;
    private ContextMenu? _originalContextMenu;
    private WriterEditorContextMenuTarget? _currentTarget;
    private bool _attached;
    private bool _disposed;

    /// <summary>Creates and attaches a Writer-owned context menu.</summary>
    /// <param name="editor">The native editor that remains the command target.</param>
    public WriterEditorContextMenuController(RichTextBox editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _menu = new ContextMenu
        {
            PlacementTarget = editor,
            FlowDirection = editor.FlowDirection
        };
        TryApplyModernMenuStyles(_menu);
        Attach();
    }

    /// <summary>Gets the native editor used as every command target.</summary>
    public RichTextBox Editor => _editor;

    /// <summary>Gets the Writer-owned menu assigned to the editor.</summary>
    public ContextMenu Menu => _menu;

    /// <summary>Gets the target captured for the currently open menu.</summary>
    public WriterEditorContextMenuTarget? CurrentTarget => _currentTarget;

    /// <summary>Gets or sets the optional Clear Formatting callback override.</summary>
    /// <remarks>
    /// When unset, the existing WriterEditingCommands.ClearFormatting command is projected.
    /// </remarks>
    public Action<WriterEditorContextMenuTarget>? ClearFormattingRequested { get; set; }

    /// <summary>Gets or sets the callback used to open the app-owned Font dialog.</summary>
    public Action<WriterEditorContextMenuTarget>? FontDialogRequested { get; set; }

    /// <summary>Gets or sets the callback used to open the app-owned Paragraph dialog.</summary>
    public Action<WriterEditorContextMenuTarget>? ParagraphDialogRequested { get; set; }

    /// <summary>
    /// Gets or sets an optional structured-selection hit test used to preserve native selection
    /// when a right click lands inside a selected object region such as table cells.
    /// </summary>
    public Func<TextPointer, TextPointer, TextPointer, bool>? StructuredSelectionHitTest { get; set; }

    /// <summary>
    /// Raised while the base text menu is complete so later packets can append context-aware
    /// structured-object actions using the same stable target.
    /// </summary>
    public event EventHandler<WriterEditorContextMenuExtensionContext>? ExtensionsRequested;

    /// <summary>Attaches the menu and its mouse/opening handlers idempotently.</summary>
    public void Attach()
    {
        ThrowIfDisposed();
        if (_attached)
            return;

        _originalContextMenu = _editor.ContextMenu;
        _editor.ContextMenu = _menu;
        _editor.PreviewMouseRightButtonDown += OnPreviewMouseRightButtonDown;
        _editor.ContextMenuOpening += OnContextMenuOpening;
        _menu.Closed += OnMenuClosed;
        _attached = true;
    }

    /// <summary>Detaches handlers and restores the context menu present before attachment.</summary>
    public void Detach()
    {
        if (!_attached)
            return;

        _editor.PreviewMouseRightButtonDown -= OnPreviewMouseRightButtonDown;
        _editor.ContextMenuOpening -= OnContextMenuOpening;
        _menu.Closed -= OnMenuClosed;
        if (ReferenceEquals(_editor.ContextMenu, _menu))
            _editor.ContextMenu = _originalContextMenu;
        _currentTarget = null;
        _attached = false;
    }

    /// <summary>Captures the editor's current selection and spelling state without opening a menu.</summary>
    public WriterEditorContextMenuTarget CaptureCurrentTarget()
    {
        ThrowIfDisposed();
        return CaptureTarget();
    }

    /// <summary>Rebuilds the menu from the current editor selection without opening the popup.</summary>
    public void Refresh()
    {
        ThrowIfDisposed();
        _menu.FlowDirection = _editor.FlowDirection;
        _currentTarget = CaptureTarget();
        RebuildMenu(_currentTarget);
    }

    /// <summary>Restores a previously captured target before an extension command executes.</summary>
    public bool TryRestoreTarget(WriterEditorContextMenuTarget target)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(target);
        return target.TryRestore(_editor);
    }

    /// <summary>Applies RibbonKit's keyed menu styles, returning false on resource failure.</summary>
    public static bool TryApplyModernMenuStyles(ContextMenu menu)
    {
        ArgumentNullException.ThrowIfNull(menu);
        try
        {
            // ResourceDictionary and Style instances are dispatcher-affine. Loading a fresh
            // dictionary per menu also keeps independent STA test hosts and secondary UI threads
            // from sharing a Style created by another dispatcher.
            return TryApplyModernMenuStyles(menu,
                new ResourceDictionary { Source = MenuResourceUri });
        }
        catch (Exception exception) when (exception is IOException or KeyNotFoundException or
            UriFormatException or InvalidOperationException or XamlParseException)
        {
            // Writer remains usable with the stock WPF menu if the optional RibbonKit pack
            // dictionary cannot be resolved in a test host or a differently packaged deployment.
            return false;
        }
    }

    /// <summary>
    /// Applies styles from a supplied resource dictionary. A null or keyless dictionary is a
    /// supported fallback path for hosts and tests that do not package RibbonKit's menu resources.
    /// </summary>
    /// <param name="menu">The menu to style.</param>
    /// <param name="resources">The optional keyed RibbonKit menu resources.</param>
    /// <returns><see langword="true"/> when at least one keyed style was applied.</returns>
    public static bool TryApplyModernMenuStyles(
        ContextMenu menu,
        ResourceDictionary? resources)
    {
        ArgumentNullException.ThrowIfNull(menu);
        if (resources is null)
            return false;

        try
        {
            var applied = false;
            if (resources["RibbonKit.ContextMenu"] is Style menuStyle)
            {
                menu.Style = menuStyle;
                applied = true;
            }

            if (resources["RibbonKit.MenuItem"] is Style itemStyle)
            {
                menu.Resources[typeof(MenuItem)] = itemStyle;
                applied = true;
            }

            if (resources["RibbonKit.MenuSeparator"] is Style separatorStyle)
            {
                menu.Resources[MenuItem.SeparatorStyleKey] = separatorStyle;
                applied = true;
            }

            return applied;
        }
        catch (Exception)
        {
            // A malformed or partially available optional dictionary must not prevent the stock
            // ContextMenu from being used by Writer.
            return false;
        }
    }

    /// <summary>Removes handlers, restores the prior menu and releases popup-owned state.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        Detach();
        _menu.Items.Clear();
        _disposed = true;
    }

    private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_disposed || e.ChangedButton != MouseButton.Right)
            return;

        TextPointer? pointer;
        try
        {
            pointer = _editor.GetPositionFromPoint(e.GetPosition(_editor), snapToText: true);
        }
        catch (InvalidOperationException)
        {
            pointer = null;
        }

        if (pointer is null)
            return;

        var selectionStart = _editor.Selection.Start;
        var selectionEnd = _editor.Selection.End;
        if (ShouldPreserveSelection(pointer, selectionStart, selectionEnd))
            return;

        var insertion = pointer.GetInsertionPosition(LogicalDirection.Forward) ?? pointer;
        _editor.Focus();
        _editor.Selection.Select(insertion, insertion);
    }

    private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (_disposed)
            return;

        Refresh();
    }

    private void OnMenuClosed(object? sender, RoutedEventArgs e) => _currentTarget = null;

    private WriterEditorContextMenuTarget CaptureTarget()
    {
        var start = _editor.Selection.Start;
        var end = _editor.Selection.End;
        if (start.CompareTo(end) > 0)
            (start, end) = (end, start);

        return new WriterEditorContextMenuTarget(start, end, _editor.Document,
            FindSpellingError(start, end));
    }

    private void RebuildMenu(WriterEditorContextMenuTarget target)
    {
        _menu.Items.Clear();
        var spellingError = target.SpellingError;
        if (spellingError is not null)
        {
            foreach (var suggestion in spellingError.Suggestions
                         .Where(static suggestion => !string.IsNullOrWhiteSpace(suggestion))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                _menu.Items.Add(CreateCommandItem(suggestion,
                    EditingCommands.CorrectSpellingError, suggestion, target));
            }

            _menu.Items.Add(CreateCommandItem("Ignore", EditingCommands.IgnoreSpellingError,
                null, target));
            var ignoreAll = CreateCallbackItem("Ignore All", target, current =>
            {
                if (current.SpellingError is not null)
                    current.SpellingError.IgnoreAll();
            });
            _menu.Items.Add(ignoreAll);
            _menu.Items.Add(new Separator());
        }

        AddCommandItem("Undo", ApplicationCommands.Undo, target, inputGestureText: "Ctrl+Z");
        AddCommandItem("Redo", ApplicationCommands.Redo, target, inputGestureText: "Ctrl+Y");
        _menu.Items.Add(new Separator());
        AddCommandItem("Cut", ApplicationCommands.Cut, target, inputGestureText: "Ctrl+X");
        AddCommandItem("Copy", ApplicationCommands.Copy, target, inputGestureText: "Ctrl+C");
        AddCommandItem("Paste", ApplicationCommands.Paste, target, inputGestureText: "Ctrl+V");
        AddCommandItem("Paste Text Only", WriterEditingCommands.PasteTextOnly, target);
        AddCommandItem("Select All", ApplicationCommands.SelectAll, target, inputGestureText: "Ctrl+A");
        _menu.Items.Add(new Separator());
        AddCommandItem("Bold", EditingCommands.ToggleBold, target, inputGestureText: "Ctrl+B");
        AddCommandItem("Italic", EditingCommands.ToggleItalic, target, inputGestureText: "Ctrl+I");
        AddCommandItem("Underline", EditingCommands.ToggleUnderline, target, inputGestureText: "Ctrl+U");

        if (ClearFormattingRequested is not null)
            _menu.Items.Add(CreateCallbackItem("Clear Formatting", target, ClearFormattingRequested));
        else
            AddCommandItem("Clear Formatting", WriterEditingCommands.ClearFormatting, target);

        _menu.Items.Add(CreateCallbackItem("Font...", target, FontDialogRequested));
        _menu.Items.Add(CreateCallbackItem("Paragraph...", target, ParagraphDialogRequested));

        var extensions = new WriterEditorContextMenuExtensionContext(
            _menu,
            _editor,
            target,
            (header, command, parameter) => CreateCommandItem(header, command, parameter, target),
            (header, callback) => CreateCallbackItem(header, target, callback),
            (header, canExecute, callback) =>
                CreateCallbackItem(header, target, callback, canExecute));
        ExtensionsRequested?.Invoke(this, extensions);
        foreach (var item in extensions.Items)
            _menu.Items.Add(item);
    }

    private void AddCommandItem(
        string header,
        ICommand command,
        WriterEditorContextMenuTarget target,
        string? inputGestureText = null) =>
        _menu.Items.Add(CreateCommandItem(header, command, null, target, inputGestureText));

    private MenuItem CreateCommandItem(
        string header,
        ICommand command,
        object? parameter,
        WriterEditorContextMenuTarget target,
        string? inputGestureText = null)
    {
        ArgumentNullException.ThrowIfNull(command);
        return new MenuItem
        {
            Header = header,
            Command = new TargetedCommand(this, target, command),
            CommandParameter = parameter,
            CommandTarget = _editor,
            InputGestureText = inputGestureText
        };
    }

    private MenuItem CreateCallbackItem(
        string header,
        WriterEditorContextMenuTarget target,
        Action<WriterEditorContextMenuTarget>? callback,
        Func<WriterEditorContextMenuTarget, bool>? canExecute = null)
    {
        var item = new MenuItem
        {
            Header = header,
            CommandTarget = _editor
        };
        if (callback is null)
            item.IsEnabled = false;
        else
        {
            item.Command = new TargetedCallbackGuardCommand(this, target, canExecute);
            item.Click += (_, _) =>
            {
                if (!TargetedCallbackGuardCommand.CanInvoke(
                        this, target, canExecute, out var canInvoke)
                    || !canInvoke || !TryRestoreTarget(target)
                    || !TargetedCallbackGuardCommand.CanInvoke(
                        this, target, canExecute, out canInvoke) || !canInvoke)
                    return;
                callback(target);
            };
        }

        return item;
    }

    /// <summary>Returns whether a pointer lies inside a non-empty selection.</summary>
    public static bool IsPointerInsideSelection(
        TextPointer pointer,
        TextPointer selectionStart,
        TextPointer selectionEnd)
    {
        if (selectionStart.CompareTo(selectionEnd) > 0)
            (selectionStart, selectionEnd) = (selectionEnd, selectionStart);
        if (selectionStart.CompareTo(selectionEnd) == 0)
            return false;
        return pointer.CompareTo(selectionStart) >= 0 && pointer.CompareTo(selectionEnd) < 0;
    }

    internal bool ShouldPreserveSelection(TextPointer pointer, TextPointer selectionStart,
        TextPointer selectionEnd) =>
        IsPointerInsideSelection(pointer, selectionStart, selectionEnd)
        || StructuredSelectionHitTest?.Invoke(pointer, selectionStart, selectionEnd) == true;

    private SpellingError? FindSpellingError(TextPointer start, TextPointer end)
    {
        var candidates = new[]
        {
            start,
            start.GetInsertionPosition(LogicalDirection.Backward),
            start.GetInsertionPosition(LogicalDirection.Forward),
            end,
            end.GetInsertionPosition(LogicalDirection.Backward),
            end.GetInsertionPosition(LogicalDirection.Forward)
        };
        foreach (var candidate in candidates)
        {
            if (candidate is null)
                continue;
            try
            {
                var error = _editor.GetSpellingError(candidate);
                if (error is not null)
                    return error;
            }
            catch (ArgumentException)
            {
                // A structural boundary is not a valid spelling query location.
            }
            catch (InvalidOperationException)
            {
                // Native spelling may be unavailable in an off-screen or unloaded test control.
            }
        }

        return null;
    }

    private sealed class TargetedCommand(
        WriterEditorContextMenuController owner,
        WriterEditorContextMenuTarget target,
        ICommand command) : ICommand
    {
        public bool CanExecute(object? parameter) =>
            target.IsValidFor(owner._editor)
            && CanExecuteTargeted(command, parameter, owner._editor);

        public void Execute(object? parameter)
        {
            if (!owner.TryRestoreTarget(target))
                return;
            ExecuteTargeted(command, parameter, owner._editor);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        private static bool CanExecuteTargeted(ICommand command, object? parameter,
            RichTextBox editor) => command is RoutedCommand routed
                ? routed.CanExecute(parameter, editor)
                : command.CanExecute(parameter);

        private static void ExecuteTargeted(ICommand command, object? parameter,
            RichTextBox editor)
        {
            if (command is RoutedCommand routed)
                routed.Execute(parameter, editor);
            else
                command.Execute(parameter);
        }
    }

    private sealed class TargetedCallbackGuardCommand(
        WriterEditorContextMenuController owner,
        WriterEditorContextMenuTarget target,
        Func<WriterEditorContextMenuTarget, bool>? canExecute) : ICommand
    {
        public bool CanExecute(object? parameter) =>
            CanInvoke(owner, target, canExecute, out var canInvoke) && canInvoke;

        internal static bool CanInvoke(
            WriterEditorContextMenuController owner,
            WriterEditorContextMenuTarget target,
            Func<WriterEditorContextMenuTarget, bool>? canExecute,
            out bool canInvoke)
        {
            canInvoke = false;
            if (!target.IsValidFor(owner._editor))
                return false;
            try
            {
                canInvoke = canExecute?.Invoke(target) ?? true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Execute(object? parameter) { }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
