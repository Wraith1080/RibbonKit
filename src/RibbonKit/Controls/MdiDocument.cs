using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace RibbonKit.Controls;

/// <summary>
/// The model for one document hosted in an <see cref="MdiContainer"/>: its content,
/// caption data, and placement. Bindable (implements <see cref="INotifyPropertyChanged"/>)
/// so it works equally as an MVVM item (bound via <c>ItemsSource</c>) or as the object
/// created by <see cref="MdiContainer.AddDocument(FrameworkElement, string)"/>.
/// Placement properties round-trip: the container writes back position/size/state as the
/// user drags, resizes, and maximizes, so the model always reflects the live layout
/// (which is what the later layout-persistence feature serializes).
/// </summary>
public class MdiDocument : INotifyPropertyChanged
{
    private object? _content;
    private string _title = string.Empty;
    private object? _icon;
    private double _left = double.NaN;
    private double _top = double.NaN;
    private double _width = double.NaN;
    private double _height = double.NaN;
    private WindowState _windowState = WindowState.Normal;
    private bool _canClose = true;
    private bool _isModified;
    private bool _isActive;

    /// <summary>Initializes an empty document.</summary>
    public MdiDocument()
    {
    }

    /// <summary>Initializes a document with content and a caption title.</summary>
    public MdiDocument(object? content, string title)
    {
        _content = content;
        _title = title;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// The document body: a <see cref="FrameworkElement"/> (e.g. a UserControl), or a
    /// view-model rendered through the container's <c>ItemTemplate</c>.
    /// </summary>
    public object? Content
    {
        get => _content;
        set => Set(ref _content, value);
    }

    /// <summary>The caption title.</summary>
    public string Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    /// <summary>Optional small icon shown at the left of the caption.</summary>
    public object? Icon
    {
        get => _icon;
        set => Set(ref _icon, value);
    }

    /// <summary>
    /// Left edge in DIPs relative to the container. <see cref="double.NaN"/> means
    /// "unplaced": the container assigns a cascade position when the child loads.
    /// </summary>
    public double Left
    {
        get => _left;
        set => Set(ref _left, value);
    }

    /// <summary>Top edge in DIPs relative to the container. NaN means "unplaced".</summary>
    public double Top
    {
        get => _top;
        set => Set(ref _top, value);
    }

    /// <summary>Width in DIPs. NaN means "use the container's default document size".</summary>
    public double Width
    {
        get => _width;
        set => Set(ref _width, value);
    }

    /// <summary>Height in DIPs. NaN means "use the container's default document size".</summary>
    public double Height
    {
        get => _height;
        set => Set(ref _height, value);
    }

    /// <summary>Normal, Minimized (caption strip at the bottom), or Maximized (fills the client area).</summary>
    public WindowState WindowState
    {
        get => _windowState;
        set => Set(ref _windowState, value);
    }

    /// <summary>Whether the caption shows a close button. Default <see langword="true"/>.</summary>
    public bool CanClose
    {
        get => _canClose;
        set => Set(ref _canClose, value);
    }

    /// <summary>Shows the dirty marker (•) next to the title, like an unsaved Office document.</summary>
    public bool IsModified
    {
        get => _isModified;
        set => Set(ref _isModified, value);
    }

    /// <summary>
    /// Whether this is the container's active document. Maintained by the container;
    /// setting it directly does not change activation — call
    /// <see cref="MdiContainer.ActivateDocument(object)"/> for that.
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => Set(ref _isActive, value);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
