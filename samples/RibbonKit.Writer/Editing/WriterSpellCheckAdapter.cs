using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RibbonKit.Writer.Editing;

/// <summary>Provides the optional native WPF spell-check capability for a Writer editor.</summary>
/// <remarks>
/// WPF owns the spelling engine, dictionaries, adorners, selection, caret and IME behavior. This
/// adapter only toggles <see cref="SpellCheck.IsEnabledProperty"/> and reports whether that native
/// attached property is available. It does not install a custom dictionary or rewrite document text.
/// Enabling is refused for disabled or read-only editors; disabling remains allowed in those states.
/// </remarks>
public sealed class WriterSpellCheckAdapter : IDisposable, INotifyPropertyChanged
{
    private readonly DependencyPropertyDescriptor? _isEnabledDescriptor;
    private readonly DependencyPropertyDescriptor? _isReadOnlyDescriptor;
    private readonly DependencyPropertyDescriptor? _spellEnabledDescriptor;
    private readonly bool _originalNativeValue;
    private bool _disposed;

    /// <summary>Creates an adapter over an existing native editor.</summary>
    /// <param name="editor">The editor whose native spelling property is controlled.</param>
    public WriterSpellCheckAdapter(RichTextBox editor)
    {
        Editor = editor ?? throw new ArgumentNullException(nameof(editor));
        IsSupported = DetectNativeSupport(editor);
        _originalNativeValue = IsSupported && SpellCheck.GetIsEnabled(editor);
        _isEnabledDescriptor = DependencyPropertyDescriptor.FromProperty(
            UIElement.IsEnabledProperty, typeof(UIElement));
        _isReadOnlyDescriptor = DependencyPropertyDescriptor.FromProperty(
            TextBoxBase.IsReadOnlyProperty, typeof(TextBoxBase));
        _spellEnabledDescriptor = DependencyPropertyDescriptor.FromProperty(
            SpellCheck.IsEnabledProperty, typeof(RichTextBox));
        _isEnabledDescriptor?.AddValueChanged(Editor, OnAvailabilityChanged);
        _isReadOnlyDescriptor?.AddValueChanged(Editor, OnAvailabilityChanged);
        _spellEnabledDescriptor?.AddValueChanged(Editor, OnSpellEnabledChanged);
    }

    /// <summary>Gets the native editor.</summary>
    public RichTextBox Editor { get; }

    /// <summary>Gets whether WPF exposed its native spell-check attached property.</summary>
    public bool IsSupported { get; }

    /// <summary>Gets whether native spelling is currently enabled on the editor.</summary>
    public bool IsEnabled => IsSupported && SpellCheck.GetIsEnabled(Editor);

    /// <summary>Gets whether the editor is currently able to enable native spelling.</summary>
    public bool CanEnable => !_disposed && IsSupported && Editor.IsEnabled && !Editor.IsReadOnly;

    /// <summary>Gets whether the adapter has been disposed.</summary>
    public bool IsDisposed => _disposed;

    /// <summary>Raised when the native enabled state or editor availability changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Enables native WPF spelling when the editor supports and allows it.</summary>
    /// <returns><see langword="true"/> when the attached property changed or was already enabled.</returns>
    public bool Enable() => SetEnabled(true);

    /// <summary>Disables native WPF spelling. Disabling is allowed for a read-only editor.</summary>
    /// <returns><see langword="true"/> when the attached property changed or was already disabled.</returns>
    public bool Disable() => SetEnabled(false);

    /// <summary>Sets native spelling according to the editor's availability contract.</summary>
    /// <param name="enabled">Whether native spelling should be enabled.</param>
    /// <returns><see langword="true"/> when the requested state is available and applied.</returns>
    public bool SetEnabled(bool enabled)
    {
        ThrowIfDisposed();
        if (!IsSupported)
            return false;
        if (enabled && !CanEnable)
            return false;
        if (IsEnabled == enabled)
            return true;

        SpellCheck.SetIsEnabled(Editor, enabled);
        return true;
    }

    /// <summary>Restores the state observed by this adapter and unsubscribes from editor changes.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        var nativeStateChanged = IsSupported && SpellCheck.GetIsEnabled(Editor) != _originalNativeValue;
        _disposed = true;
        _isEnabledDescriptor?.RemoveValueChanged(Editor, OnAvailabilityChanged);
        _isReadOnlyDescriptor?.RemoveValueChanged(Editor, OnAvailabilityChanged);
        _spellEnabledDescriptor?.RemoveValueChanged(Editor, OnSpellEnabledChanged);
        if (nativeStateChanged)
            SpellCheck.SetIsEnabled(Editor, _originalNativeValue);
        OnPropertyChanged(nameof(IsDisposed));
        if (nativeStateChanged)
            OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(CanEnable));
    }

    private static bool DetectNativeSupport(RichTextBox editor)
    {
        try
        {
            _ = SpellCheck.GetIsEnabled(editor);
            return SpellCheck.IsEnabledProperty != null;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private void OnAvailabilityChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CanEnable));
        OnPropertyChanged(nameof(IsEnabled));
    }

    private void OnSpellEnabledChanged(object? sender, EventArgs e) =>
        OnPropertyChanged(nameof(IsEnabled));

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
