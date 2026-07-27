using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RibbonKit.Controls;

/// <summary>
/// An in-window Multiple Document Interface host: children float as themed
/// <see cref="MdiChild"/> windows inside the client area (WPF has no native MDI).
/// Supports both injection styles:
/// <list type="bullet">
/// <item><description>Imperative — <see cref="AddDocument(FrameworkElement, string)"/>
/// wraps any element (typically a UserControl) in a themed child window.</description></item>
/// <item><description>MVVM — bind <c>ItemsSource</c> to a collection of
/// <see cref="MdiDocument"/> (or view-models rendered via <c>ItemTemplate</c>).</description></item>
/// </list>
/// <code language="xaml">
/// &lt;rk:MdiContainer x:Name="Mdi" /&gt;
/// // Mdi.AddDocument(new MyEditorControl(), "Document1");
/// </code>
/// </summary>
public class MdiContainer : ItemsControl
{
    private const double DefaultDocumentWidth = 520;
    private const double DefaultDocumentHeight = 360;
    private const double CascadeStep = 28;
    private const int CascadeWrap = 10;

    private static readonly DependencyPropertyKey ActiveDocumentPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ActiveDocument),
            typeof(object),
            typeof(MdiContainer),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="ActiveDocument"/> dependency property.</summary>
    public static readonly DependencyProperty ActiveDocumentProperty =
        ActiveDocumentPropertyKey.DependencyProperty;

    private int _zCounter;
    private int _cascadeIndex;

    static MdiContainer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MdiContainer),
            new FrameworkPropertyMetadata(typeof(MdiContainer)));
    }

    /// <summary>Initializes the container and wires the child activation/close plumbing.</summary>
    public MdiContainer()
    {
        AddHandler(MdiChild.ActivationRequestedEvent, new RoutedEventHandler(OnChildActivationRequested));
        AddHandler(MdiChild.CloseRequestedEvent, new RoutedEventHandler(OnChildCloseRequested));
    }

    /// <summary>
    /// Raised before a document closes (close button, or <see cref="CloseDocument"/>).
    /// Set <see cref="CancelEventArgs.Cancel"/> to keep it open — e.g. after an
    /// unsaved-changes prompt.
    /// </summary>
    public event EventHandler<MdiDocumentClosingEventArgs>? DocumentClosing;

    /// <summary>Raised after a document has closed and been removed.</summary>
    public event EventHandler<MdiDocumentEventArgs>? DocumentClosed;

    /// <summary>Raised when <see cref="ActiveDocument"/> changes.</summary>
    public event EventHandler? ActiveDocumentChanged;

    /// <summary>
    /// The item (an <see cref="MdiDocument"/> or your bound view-model) whose child
    /// window is currently active, or <see langword="null"/> when there are none.
    /// </summary>
    public object? ActiveDocument => GetValue(ActiveDocumentProperty);

    /// <summary>
    /// Wraps <paramref name="content"/> in a themed child window and shows it. Only for
    /// the imperative style (throws if <c>ItemsSource</c> is bound — add to your source
    /// collection instead). Returns the created document so the caller can keep
    /// adjusting it (icon, size, dirty flag, ...).
    /// </summary>
    public MdiDocument AddDocument(FrameworkElement content, string title)
    {
        var document = new MdiDocument(content, title);
        AddDocument(document);
        return document;
    }

    /// <summary>Adds a prepared document. Only for the imperative style (see <see cref="AddDocument(FrameworkElement, string)"/>).</summary>
    public void AddDocument(MdiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Items.Add(document);
    }

    /// <summary>Brings the given item's child window to front and makes it active.</summary>
    public void ActivateDocument(object item)
    {
        if (ItemContainerGenerator.ContainerFromItem(item) is MdiChild child)
        {
            ActivateChild(child);
        }
    }

    /// <summary>
    /// Closes the given item's document: raises the cancelable <see cref="DocumentClosing"/>,
    /// then removes it from <c>Items</c> (imperative style) or from a modifiable bound
    /// source list. With a read-only source, handle <see cref="DocumentClosed"/> and
    /// remove the item from your collection yourself.
    /// </summary>
    public void CloseDocument(object item)
    {
        var closing = new MdiDocumentClosingEventArgs(item);
        DocumentClosing?.Invoke(this, closing);
        if (closing.Cancel)
        {
            return;
        }

        if (ItemsSource is null)
        {
            Items.Remove(item);
        }
        else if (ItemsSource is IList { IsReadOnly: false, IsFixedSize: false } list && list.Contains(item))
        {
            list.Remove(item);
        }

        DocumentClosed?.Invoke(this, new MdiDocumentEventArgs(item));
    }

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new MdiChild();

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => item is MdiChild;

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);

        if (element is not MdiChild child)
        {
            return;
        }

        if (item is MdiDocument document)
        {
            // Two-way plumbing between the model and the chrome: the child reflects the
            // model, and drag/resize/state changes write back so the model always holds
            // the live layout.
            child.DataContext = document;
            Bind(child, MdiChild.TitleProperty, nameof(MdiDocument.Title), BindingMode.OneWay);
            Bind(child, MdiChild.IconProperty, nameof(MdiDocument.Icon), BindingMode.OneWay);
            Bind(child, ContentControl.ContentProperty, nameof(MdiDocument.Content), BindingMode.OneWay);
            Bind(child, MdiChild.CanCloseProperty, nameof(MdiDocument.CanClose), BindingMode.OneWay);
            Bind(child, MdiChild.IsModifiedProperty, nameof(MdiDocument.IsModified), BindingMode.OneWay);
            Bind(child, MdiChild.IsActiveProperty, nameof(MdiDocument.IsActive), BindingMode.TwoWay);
            Bind(child, MdiChild.WindowStateProperty, nameof(MdiDocument.WindowState), BindingMode.TwoWay);
            Bind(child, MdiChild.LeftProperty, nameof(MdiDocument.Left), BindingMode.TwoWay);
            Bind(child, MdiChild.TopProperty, nameof(MdiDocument.Top), BindingMode.TwoWay);
            Bind(child, WidthProperty, nameof(MdiDocument.Width), BindingMode.TwoWay);
            Bind(child, HeightProperty, nameof(MdiDocument.Height), BindingMode.TwoWay);
        }
        else if (!ReferenceEquals(child, item))
        {
            // Raw element or view-model bound directly: the child hosts it as content,
            // rendered through ItemTemplate when one is set.
            child.Content = item;
            if (child.Title.Length == 0)
            {
                child.Title = item is FrameworkElement { Name.Length: > 0 } fe
                    ? fe.Name
                    : $"Document {Items.Count}";
            }
        }

        if (ItemTemplate is not null)
        {
            child.ContentTemplate = ItemTemplate;
        }

        if (ItemTemplateSelector is not null)
        {
            child.ContentTemplateSelector = ItemTemplateSelector;
        }

        // Default size and cascade placement for unplaced documents. SetCurrentValue
        // keeps the two-way bindings alive and pushes the assigned values back into the
        // model, so it learns its real placement immediately.
        if (double.IsNaN(child.Width))
        {
            child.SetCurrentValue(WidthProperty, DefaultDocumentWidth);
        }

        if (double.IsNaN(child.Height))
        {
            child.SetCurrentValue(HeightProperty, DefaultDocumentHeight);
        }

        if (double.IsNaN(child.Left) || double.IsNaN(child.Top))
        {
            double offset = 16 + (CascadeStep * (_cascadeIndex++ % CascadeWrap));
            child.SetCurrentValue(MdiChild.LeftProperty, offset);
            child.SetCurrentValue(MdiChild.TopProperty, offset);
        }

        // Pass the item explicitly: the generator may not resolve ItemFromContainer
        // while the container is still being prepared.
        ActivateChild(child, item);
    }

    /// <inheritdoc />
    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        if (element is MdiChild child)
        {
            BindingOperations.ClearAllBindings(child);
        }

        base.ClearContainerForItemOverride(element, item);
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

        // If the active document went away, promote the topmost remaining child.
        if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace
            or NotifyCollectionChangedAction.Reset)
        {
            object? active = ActiveDocument;
            if (active is null || ItemContainerGenerator.ContainerFromItem(active) is not MdiChild)
            {
                ActivateTopmost();
            }
        }
    }

    private static void Bind(FrameworkElement target, DependencyProperty property, string path, BindingMode mode) =>
        target.SetBinding(property, new Binding(path) { Mode = mode });

    private void OnChildActivationRequested(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is MdiChild child)
        {
            ActivateChild(child);
        }
    }

    private void OnChildCloseRequested(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is MdiChild { CanClose: true } child)
        {
            object item = ItemContainerGenerator.ItemFromContainer(child);
            CloseDocument(item == DependencyProperty.UnsetValue ? child : item);
            e.Handled = true;
        }
    }

    private void ActivateChild(MdiChild child, object? knownItem = null)
    {
        object item = knownItem ?? ItemContainerGenerator.ItemFromContainer(child);
        object newActive = item == DependencyProperty.UnsetValue ? child : item;
        if (ReferenceEquals(ActiveDocument, newActive) && child.IsActive)
        {
            return;
        }

        foreach (object other in Items)
        {
            if (ItemContainerGenerator.ContainerFromItem(other) is MdiChild otherChild
                && !ReferenceEquals(otherChild, child))
            {
                otherChild.SetCurrentValue(MdiChild.IsActiveProperty, false);
            }
        }

        Panel.SetZIndex(child, ++_zCounter);
        child.SetCurrentValue(MdiChild.IsActiveProperty, true);
        SetValue(ActiveDocumentPropertyKey, newActive);
        ActiveDocumentChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ActivateTopmost()
    {
        MdiChild? topmost = null;
        foreach (object item in Items)
        {
            if (ItemContainerGenerator.ContainerFromItem(item) is MdiChild child
                && (topmost is null || Panel.GetZIndex(child) > Panel.GetZIndex(topmost)))
            {
                topmost = child;
            }
        }

        if (topmost is not null)
        {
            ActivateChild(topmost);
        }
        else
        {
            SetValue(ActiveDocumentPropertyKey, null);
            ActiveDocumentChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

/// <summary>Event data carrying the document item involved.</summary>
public class MdiDocumentEventArgs : EventArgs
{
    /// <summary>Initializes the event data.</summary>
    public MdiDocumentEventArgs(object document) => Document = document;

    /// <summary>The item: an <see cref="MdiDocument"/>, or your bound view-model.</summary>
    public object Document { get; }
}

/// <summary>Cancelable event data for <see cref="MdiContainer.DocumentClosing"/>.</summary>
public class MdiDocumentClosingEventArgs : CancelEventArgs
{
    /// <summary>Initializes the event data.</summary>
    public MdiDocumentClosingEventArgs(object document) => Document = document;

    /// <summary>The item about to close.</summary>
    public object Document { get; }
}
