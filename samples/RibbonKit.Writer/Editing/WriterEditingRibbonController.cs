using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Controls;

namespace RibbonKit.Writer.Editing;

/// <summary>Commands for Writer actions that are not supplied by WPF.</summary>
public static class WriterRibbonCommands
{
    /// <summary>Opens the Writer find dialog.</summary>
    public static RoutedUICommand Find { get; } = Create("Find", nameof(Find));

    /// <summary>Opens the Writer find-and-replace dialog.</summary>
    public static RoutedUICommand Replace { get; } = Create("Replace", nameof(Replace));

    private static RoutedUICommand Create(string text, string name) =>
        new(text, name, typeof(WriterRibbonCommands));
}

/// <summary>
/// Connects Writer's native editing surface and W1-A/W1-B services to ribbon controls.
/// </summary>
/// <remarks>
/// The controller never replaces the editor's document except when explicitly asked by the shell,
/// and it never owns selection, caret, clipboard, IME, or the native undo stack. Ribbon commands use
/// the editor as their routed-command target; combo-box commits call the same adapter directly and
/// return focus to the editor after applying a value. State projection is deliberately separate from
/// command execution so mixed and unset selection values are never coerced into a formatting value.
/// </remarks>
public sealed class WriterEditingRibbonController : IDisposable
{
    private readonly List<Action> _unbinders = new();
    private readonly List<Action> _stateUpdaters = new();
    private readonly List<(Window Window, CommandBinding Binding)> _windowCommandBindings = new();
    private bool _applyingState;
    private bool _disposed;

    /// <summary>Creates a controller over an existing native Writer editor.</summary>
    public WriterEditingRibbonController(RichTextBox editor)
    {
        Editor = editor ?? throw new ArgumentNullException(nameof(editor));
        Editing = new WriterEditingAdapter(editor);
        FontCatalog = new WriterFontCatalog(dispatcher: editor.Dispatcher);
        FindReplace = new WriterFindReplaceService(editor);
        SpellCheck = new WriterSpellCheckAdapter(editor);
        Statistics = new WriterDocumentStatistics(editor);
        Zoom = new WriterZoomModel();

        Editing.StateChanged += OnEditingStateChanged;
        SpellCheck.PropertyChanged += OnSpellCheckPropertyChanged;
        Statistics.StatisticsChanged += OnStatisticsChanged;
        Zoom.PropertyChanged += OnZoomPropertyChanged;
        ApplyZoom();
        ApplyState();
    }

    /// <summary>Gets the native editor controlled by this instance.</summary>
    public RichTextBox Editor { get; }

    /// <summary>Gets the W1-A selection and command-state adapter.</summary>
    public WriterEditingAdapter Editing { get; }

    /// <summary>Gets the cached installed-font source used by Writer's font picker.</summary>
    public WriterFontCatalog FontCatalog { get; }

    /// <summary>Gets the W1-B find and replace service.</summary>
    public WriterFindReplaceService FindReplace { get; }

    /// <summary>Gets the W1-B native spelling adapter.</summary>
    public WriterSpellCheckAdapter SpellCheck { get; }

    /// <summary>Gets the debounced word and character statistics observer.</summary>
    public WriterDocumentStatistics Statistics { get; }

    /// <summary>Gets the bounded zoom state.</summary>
    public WriterZoomModel Zoom { get; }

    /// <summary>Gets the latest native-editor selection state.</summary>
    public WriterEditingState State => Editing.State;

    /// <summary>Raised when selection-sensitive ribbon state should be refreshed.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Raised after a non-stale word/character snapshot is published.</summary>
    public event EventHandler? StatisticsChanged;

    /// <summary>Raised after the zoom transform has been updated.</summary>
    public event EventHandler? ZoomChanged;

    /// <summary>Replaces the editor document and explicitly refreshes the debounced statistics seam.</summary>
    /// <param name="document">The shell-owned document to display.</param>
    public void ReplaceDocument(FlowDocument document)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(document);
        if (!ReferenceEquals(Editor.Document, document))
            Editor.Document = document;

        // Document replacement is not guaranteed to raise TextChanged on every WPF path. W1-B
        // exposes Refresh specifically for this shell boundary.
        Statistics.Refresh();
        Editing.RefreshState();
    }

    /// <summary>Executes a W1-A command against the native editor when it is currently available.</summary>
    public bool TryExecute(ICommand command, object? parameter = null)
    {
        ThrowIfDisposed();
        return Editing.TryExecute(command, parameter);
    }

    /// <summary>Binds a routed WPF or Writer formatting command to a ribbon button.</summary>
    public void BindCommand(ButtonBase control, ICommand command, object? parameter = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(command);

        control.Command = command;
        control.CommandParameter = parameter;
        control.CommandTarget = Editor;
        RoutedEventHandler restoreFocus = (_, _) => RestoreEditorFocusDeferred();
        control.Click += restoreFocus;
        _unbinders.Add(() => control.Click -= restoreFocus);
        _stateUpdaters.Add(() =>
        {
            control.IsEnabled = Editing.CanExecute(command, parameter);
        });
        ApplyState();
    }

    /// <summary>
    /// Binds the primary half of a split button to the editor without relying on keyboard focus as
    /// the routed-command target.
    /// </summary>
    public void BindCommand(RibbonSplitButton control, ICommand command, object? parameter = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(command);

        RoutedEventHandler handler = (_, _) =>
        {
            if (!_disposed && Editing.TryExecute(command, parameter))
                RestoreEditorFocusDeferred();
        };
        control.Click += handler;
        _unbinders.Add(() => control.Click -= handler);
        _stateUpdaters.Add(() => control.IsEnabled = Editing.CanExecute(command, parameter));
        ApplyState();
    }

    /// <summary>Binds a command and a selection-state projection to a toggle button.</summary>
    public void BindToggle(RibbonToggleButton control, ICommand command,
        Func<WriterEditingState, bool?> checkedValue, object? parameter = null)
    {
        ArgumentNullException.ThrowIfNull(checkedValue);
        BindCommand(control, command, parameter);
        _stateUpdaters.Add(() =>
            control.SetCurrentValue(ToggleButton.IsCheckedProperty, checkedValue(State)));
        ApplyState();
    }

    /// <summary>Binds an action that is available while the native editor is enabled.</summary>
    public void BindAction(ButtonBase control, Action action, Func<WriterEditingState, bool>? canExecute = null,
        bool restoreEditorFocus = false)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(action);
        canExecute ??= static state => state.IsEnabled;

        RoutedEventHandler handler = (_, _) =>
        {
            if (!_disposed && canExecute(State))
            {
                action();
                if (restoreEditorFocus)
                    RestoreEditorFocusDeferred();
            }
        };
        control.Click += handler;
        _unbinders.Add(() => control.Click -= handler);
        _stateUpdaters.Add(() => control.IsEnabled = canExecute(State));
        ApplyState();
    }

    /// <summary>Binds an application action to the primary half of a split button.</summary>
    public void BindAction(RibbonSplitButton control, Action action,
        Func<WriterEditingState, bool>? canExecute = null, bool restoreEditorFocus = false)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(action);
        canExecute ??= static state => state.IsEnabled;

        RoutedEventHandler handler = (_, _) =>
        {
            if (!_disposed && canExecute(State))
            {
                action();
                if (restoreEditorFocus)
                    RestoreEditorFocusDeferred();
            }
        };
        control.Click += handler;
        _unbinders.Add(() => control.Click -= handler);
        _stateUpdaters.Add(() => control.IsEnabled = canExecute(State));
        ApplyState();
    }

    /// <summary>Binds an enabled-state projection to a ribbon dropdown.</summary>
    public void BindAvailability(RibbonDropDownButton control, Func<WriterEditingState, bool> canExecute)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(canExecute);
        _stateUpdaters.Add(() => control.IsEnabled = canExecute(State));
        ApplyState();
    }

    /// <summary>Binds one paragraph-spacing preset in typographic points.</summary>
    public void BindParagraphSpacingPreset(ButtonBase control, double beforePoints, double afterPoints)
    {
        if (!double.IsFinite(beforePoints) || beforePoints < 0)
            throw new ArgumentOutOfRangeException(nameof(beforePoints));
        if (!double.IsFinite(afterPoints) || afterPoints < 0)
            throw new ArgumentOutOfRangeException(nameof(afterPoints));

        BindAction(control, () =>
        {
            Editor.BeginChange();
            try
            {
                Editing.SetParagraphSpacingBefore(PointsToDips(beforePoints));
                Editing.SetParagraphSpacingAfter(PointsToDips(afterPoints));
            }
            finally
            {
                Editor.EndChange();
            }
        }, state => state.CanFormat, restoreEditorFocus: true);
    }

    /// <summary>Binds a font-family combo to the native formatting adapter.</summary>
    public void BindFontFamily(RibbonComboBox combo)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(combo);
        ApplyFontCatalog(combo);
        EventHandler dropDownClosed = (_, _) => CommitFontFamily(combo, SelectedText(combo));
        KeyEventHandler keyDown = (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                CommitFontFamily(combo, combo.Text);
                e.Handled = true;
            }
        };
        KeyboardFocusChangedEventHandler lostFocus = (_, _) =>
        {
            if (!combo.IsKeyboardFocusWithin)
                CommitFontFamily(combo, combo.Text);
        };
        combo.DropDownClosed += dropDownClosed;
        combo.KeyDown += keyDown;
        combo.LostKeyboardFocus += lostFocus;
        _unbinders.Add(() =>
        {
            combo.DropDownClosed -= dropDownClosed;
            combo.KeyDown -= keyDown;
            combo.LostKeyboardFocus -= lostFocus;
        });
        _stateUpdaters.Add(() =>
        {
            combo.IsEnabled = State.CanFormat;
            if (ShouldPreserveComboEdit(combo))
                return;
            if (State.FontFamily.IsUniform)
                combo.Text = State.FontFamily.Value.Source;
            else
            {
                combo.SelectedIndex = -1;
                combo.Text = string.Empty;
            }
        });
        ApplyState();
    }

    /// <summary>Binds a font-size combo to the native formatting adapter.</summary>
    public void BindFontSize(RibbonComboBox combo)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(combo);
        EventHandler dropDownClosed = (_, _) => CommitFontSize(SelectedText(combo));
        KeyEventHandler keyDown = (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                CommitFontSize(combo.Text);
                e.Handled = true;
            }
        };
        KeyboardFocusChangedEventHandler lostFocus = (_, _) =>
        {
            if (!combo.IsKeyboardFocusWithin)
                CommitFontSize(combo.Text);
        };
        combo.DropDownClosed += dropDownClosed;
        combo.KeyDown += keyDown;
        combo.LostKeyboardFocus += lostFocus;
        _unbinders.Add(() =>
        {
            combo.DropDownClosed -= dropDownClosed;
            combo.KeyDown -= keyDown;
            combo.LostKeyboardFocus -= lostFocus;
        });
        _stateUpdaters.Add(() =>
        {
            combo.IsEnabled = State.CanFormat;
            if (ShouldPreserveComboEdit(combo))
                return;
            if (State.FontSize.IsUniform)
                combo.Text = DipsToPoints(State.FontSize.Value).ToString("0.##", CultureInfo.CurrentCulture);
            else
            {
                combo.SelectedIndex = -1;
                combo.Text = string.Empty;
            }
        });
        ApplyState();
    }

    /// <summary>Binds a spell-check toggle to WPF's native spelling property.</summary>
    public void BindSpellCheck(RibbonToggleButton control)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(control);
        RoutedEventHandler handler = (_, _) =>
        {
            if (_disposed)
                return;
            if (SpellCheck.IsEnabled)
                SpellCheck.Disable();
            else
                SpellCheck.Enable();
            ApplyState();
            RestoreEditorFocusDeferred();
        };
        control.Click += handler;
        _unbinders.Add(() => control.Click -= handler);
        _stateUpdaters.Add(() =>
        {
            control.IsEnabled = SpellCheck.IsEnabled || SpellCheck.CanEnable;
            control.SetCurrentValue(ToggleButton.IsCheckedProperty,
                SpellCheck.IsSupported ? SpellCheck.IsEnabled : false);
        });
        ApplyState();
    }

    /// <summary>Binds zoom-in, zoom-out, and reset actions to the bounded zoom model.</summary>
    public void BindZoom(ButtonBase zoomIn, ButtonBase zoomOut, ButtonBase reset)
    {
        BindAction(zoomIn, () => Zoom.Increase(), restoreEditorFocus: true);
        BindAction(zoomOut, () => Zoom.Decrease(), restoreEditorFocus: true);
        BindAction(reset, () => Zoom.Reset(), restoreEditorFocus: true);
    }

    /// <summary>Converts typographic points to WPF device-independent pixels.</summary>
    public static double PointsToDips(double points) => WriterFontSizePolicy.PointsToDip(points);

    /// <summary>Converts WPF device-independent pixels to typographic points.</summary>
    public static double DipsToPoints(double dips) => WriterFontSizePolicy.DipToPoints(dips);

    /// <summary>Installs a window command binding for a Writer action.</summary>
    public void BindWindowCommand(Window window, ICommand command, ExecutedRoutedEventHandler execute,
        CanExecuteRoutedEventHandler canExecute)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentNullException.ThrowIfNull(canExecute);
        var binding = new CommandBinding(command, execute, canExecute);
        window.CommandBindings.Add(binding);
        _windowCommandBindings.Add((window, binding));
    }

    /// <summary>Returns a selection-sensitive toggle projection for one uniform value.</summary>
    public static bool? Checked<T>(WriterSelectionValue<T> value, T expected)
    {
        return value.IsUniform ? EqualityComparer<T>.Default.Equals(value.Value, expected) : null;
    }

    /// <summary>Raises a state refresh after an external editor availability change.</summary>
    public void RefreshState()
    {
        ThrowIfDisposed();
        Editing.RefreshState();
        ApplyState();
    }

    /// <summary>Disposes app-owned adapters while leaving the native editor and document intact.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Editing.StateChanged -= OnEditingStateChanged;
        SpellCheck.PropertyChanged -= OnSpellCheckPropertyChanged;
        Statistics.StatisticsChanged -= OnStatisticsChanged;
        Zoom.PropertyChanged -= OnZoomPropertyChanged;
        foreach (var unbind in _unbinders)
            unbind();
        _unbinders.Clear();
        foreach (var (window, binding) in _windowCommandBindings)
            window.CommandBindings.Remove(binding);
        _windowCommandBindings.Clear();
        FindReplace.Dispose();
        SpellCheck.Dispose();
        Statistics.Dispose();
        Editing.Dispose();
    }

    private void CommitFontFamily(RibbonComboBox combo, string text)
    {
        if (_applyingState || string.IsNullOrWhiteSpace(text) || !State.CanFormat)
            return;
        if (State.FontFamily.IsUniform && string.Equals(State.FontFamily.Value.Source, text.Trim(),
                StringComparison.OrdinalIgnoreCase))
            return;
        if (Editing.CanExecute(WriterEditingCommands.ApplyFontFamily, text.Trim()))
        {
            Editing.TryExecute(WriterEditingCommands.ApplyFontFamily, text.Trim());
            FontCatalog.RememberRecent(new FontFamily(text.Trim()));
            ApplyFontCatalog(combo);
            RestoreEditorFocusDeferred();
        }
        ApplyState();
    }

    private static string SelectedText(ComboBox combo) => combo.SelectedItem switch
    {
        ComboBoxItem { Content: not null } item => item.Content.ToString() ?? combo.Text,
        WriterFontChoice choice => choice.DisplayName,
        not null => combo.SelectedItem.ToString() ?? combo.Text,
        _ => combo.Text
    };

    internal static bool ShouldPreserveComboEdit(ComboBox combo)
    {
        ArgumentNullException.ThrowIfNull(combo);
        return combo.IsKeyboardFocusWithin || combo.IsDropDownOpen;
    }

    private void CommitFontSize(string text)
    {
        if (_applyingState || string.IsNullOrWhiteSpace(text) || !State.CanFormat)
            return;
        if (!WriterFontSizePolicy.TryParsePoints(text.Trim(), CultureInfo.CurrentCulture, out var size))
            return;
        var sizeInDips = PointsToDips(size);
        if (State.FontSize.IsUniform && Math.Abs(State.FontSize.Value - sizeInDips) < 0.001)
            return;
        if (Editing.CanExecute(WriterEditingCommands.ApplyFontSize, sizeInDips))
        {
            Editing.TryExecute(WriterEditingCommands.ApplyFontSize, sizeInDips);
            RestoreEditorFocusDeferred();
        }
        ApplyState();
    }

    private void ApplyFontCatalog(RibbonComboBox combo)
    {
        var current = State.FontFamily.IsUniform ? State.FontFamily.Value : null;
        combo.ItemsSource = FontCatalog.CreateProjection(current).Items;
    }

    private void OnEditingStateChanged(object? sender, EventArgs e)
    {
        ApplyState();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSpellCheckPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ApplyState();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnStatisticsChanged(object? sender, EventArgs e) =>
        StatisticsChanged?.Invoke(this, EventArgs.Empty);

    private void OnZoomPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ApplyZoom();
        ZoomChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyState()
    {
        if (_disposed || _applyingState)
            return;
        _applyingState = true;
        try
        {
            foreach (var update in _stateUpdaters)
                update();
        }
        finally
        {
            _applyingState = false;
        }
    }

    private void ApplyZoom()
    {
        var scale = Zoom.Value / 100d;
        Editor.LayoutTransform = Math.Abs(scale - 1d) < 0.0001
            ? Transform.Identity
            : new ScaleTransform(scale, scale);
    }

    private void RestoreEditorFocusDeferred()
    {
        if (_disposed)
            return;
        // KeyTip activation runs inside the current input event. Restoring at Input priority can
        // move focus before the terminating KeyUp is dispatched and leak the KeyTip letters into
        // the document. ContextIdle waits until that input sequence has fully unwound while still
        // returning the caret before the user can begin ordinary typing.
        _ = Editor.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            if (!_disposed && Editor.IsEnabled)
                Editor.Focus();
        }));
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
