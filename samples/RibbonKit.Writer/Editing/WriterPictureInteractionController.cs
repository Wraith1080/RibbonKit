using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace RibbonKit.Writer.Editing;

/// <summary>
/// Owns Writer's explicit document-bound picture selection and its non-document adorner.
/// </summary>
public sealed class WriterPictureInteractionController : IDisposable
{
    private readonly RichTextBox _editor;
    private readonly WriterImageService _images;
    private InlineUIContainer? _selectedContainer;
    private Image? _selectedImage;
    private WriterPictureResizeAdorner? _adorner;
    private AdornerLayer? _adornerLayer;
    private Window? _hostWindow;
    private bool _changingSelection;
    private bool _committingResize;
    private bool _disposed;

    /// <summary>Creates a controller over the one live Writer editor.</summary>
    public WriterPictureInteractionController(RichTextBox editor, WriterImageService images)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _images = images ?? throw new ArgumentNullException(nameof(images));
        _editor.PreviewMouseLeftButtonDown += OnEditorPreviewMouseLeftButtonDown;
        _editor.SelectionChanged += OnEditorSelectionChanged;
        _editor.TextChanged += OnEditorTextChanged;
        _editor.LayoutUpdated += OnEditorLayoutUpdated;
    }

    /// <summary>Gets whether one live picture is explicitly selected.</summary>
    public bool HasSelection => _committingResize || IsSelectionCurrent();

    /// <summary>Gets the selected image element while it remains live.</summary>
    public Image? SelectedImage => IsSelectionCurrent() ? _selectedImage : null;

    /// <summary>Gets the selected inline container while it remains live.</summary>
    internal InlineUIContainer? SelectedContainer => IsSelectionCurrent() ? _selectedContainer : null;

    internal Size MaximumSize => GetMaximumSize();

    /// <summary>Raised when the durable picture target changes or its committed geometry changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Selects an exact picture only while it belongs to the editor's current document.</summary>
    public bool SelectPicture(InlineUIContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        ThrowIfDisposed();
        if (!WriterInlineInsertion.IsInlineInDocument(_editor.Document, container)
            || !WriterInlineInsertion.TryGetImage(container, out var image))
            return false;

        CancelActiveResize();
        var changed = !ReferenceEquals(_selectedContainer, container)
            || !ReferenceEquals(_selectedImage, image);
        _selectedContainer = container;
        _selectedImage = image;
        _changingSelection = true;
        try
        {
            _editor.Selection.Select(container.ElementStart, container.ElementEnd);
        }
        finally
        {
            _changingSelection = false;
        }
        AttachAdorner();
        if (changed)
            StateChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Hit-tests live picture rectangles in editor coordinates and selects the topmost match.</summary>
    internal bool TrySelectAtPoint(Point point)
    {
        ThrowIfDisposed();
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
            return false;
        foreach (var container in WriterInlineInsertion.EnumerateImages(_editor.Document).Reverse())
        {
            if (!WriterInlineInsertion.TryGetImage(container, out var image)
                || !TryGetImageRect(image, out var rect) || !rect.Contains(point))
                continue;
            return SelectPicture(container);
        }
        return false;
    }

    /// <summary>Clears picture selection and removes all interaction chrome.</summary>
    public void ClearSelection()
    {
        ThrowIfDisposed();
        ClearSelectionCore(raiseChanged: true);
    }

    /// <summary>Clears stale state at a document lifetime boundary.</summary>
    public void ReplaceDocument(FlowDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ThrowIfDisposed();
        CancelActiveResize();
        ClearSelectionCore(raiseChanged: true);
    }

    /// <summary>Cancels a pointer resize without changing the durable picture selection.</summary>
    public void CancelActiveResize() => _adorner?.CancelDrag();

    /// <summary>Revalidates selection after Undo/Redo or an app-owned structural mutation.</summary>
    public void Refresh()
    {
        ThrowIfDisposed();
        if (!IsSelectionCurrent())
        {
            ClearSelectionCore(raiseChanged: true);
            return;
        }
        AttachAdorner();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Commits a bounded width/height edit through the same one-unit resize transaction.</summary>
    public bool TrySetSize(double width, double height)
    {
        ThrowIfDisposed();
        if (!double.IsFinite(width) || !double.IsFinite(height)
            || width < WriterPictureResizeGeometry.MinimumDimension
            || height < WriterPictureResizeGeometry.MinimumDimension
            || _selectedContainer is null || _selectedImage is null
            || !IsSelectionCurrent())
            return false;

        var maximum = GetMaximumSize();
        var bounded = new Size(Math.Min(width, maximum.Width), Math.Min(height, maximum.Height));
        var opening = WriterImageService.CloneImageElement(_selectedImage);
        return CommitResize(opening, bounded);
    }

    internal bool BeginExternalResize(WriterPictureResizeHandle handle)
    {
        ThrowIfDisposed();
        if (!Enum.IsDefined(handle) || !IsSelectionCurrent())
            return false;
        AttachAdorner();
        if (_adorner is null)
            return false;
        _adorner.BeginDragForTesting(handle, new Point());
        return _adorner.IsDragging;
    }

    internal void UpdateExternalResize(Vector delta)
    {
        ThrowIfDisposed();
        if (_adorner?.IsDragging == true && double.IsFinite(delta.X) && double.IsFinite(delta.Y))
            _adorner.UpdateDragForTesting(new Point(delta.X, delta.Y));
    }

    internal bool CompleteExternalResize()
    {
        ThrowIfDisposed();
        if (_adorner?.IsDragging != true)
            return false;
        _adorner.CompleteDragForTesting();
        return HasSelection;
    }

    internal void CancelExternalResize() => _adorner?.CancelDrag();

    /// <summary>Removes the explicitly selected picture through the established image undo bridge.</summary>
    public bool TryRemoveSelectedPicture()
    {
        ThrowIfDisposed();
        if (_selectedContainer is null || !IsSelectionCurrent()
            || !_images.TryRemoveImage(_editor, _selectedContainer))
            return false;
        ClearSelectionCore(raiseChanged: true);
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _editor.PreviewMouseLeftButtonDown -= OnEditorPreviewMouseLeftButtonDown;
        _editor.SelectionChanged -= OnEditorSelectionChanged;
        _editor.TextChanged -= OnEditorTextChanged;
        _editor.LayoutUpdated -= OnEditorLayoutUpdated;
        if (_hostWindow is not null)
            _hostWindow.PreviewKeyDown -= OnHostPreviewKeyDown;
        ClearSelectionCore(raiseChanged: false);
    }

    private void OnEditorPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Handled || e.ChangedButton != MouseButton.Left)
            return;
        if (TrySelectAtPoint(e.GetPosition(_editor)))
        {
            _editor.Focus();
            e.Handled = true;
        }
    }

    private void OnEditorSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_changingSelection || _committingResize)
            return;
        if (_selectedContainer is null)
        {
            if (!_editor.Selection.IsEmpty
                && WriterInlineInsertion.FindImageForKeyboardRemoval(
                    _editor, backward: false) is { } selected)
                SelectPicture(selected);
            return;
        }
        if (!SelectionMatches(_selectedContainer))
            ClearSelectionCore(raiseChanged: true);
    }

    private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_committingResize)
            return;
        if (_selectedContainer is not null && !IsSelectionCurrent())
            ClearSelectionCore(raiseChanged: true);
    }

    private void OnEditorLayoutUpdated(object? sender, EventArgs e)
    {
        var host = Window.GetWindow(_editor);
        if (!ReferenceEquals(host, _hostWindow))
        {
            if (_hostWindow is not null)
                _hostWindow.PreviewKeyDown -= OnHostPreviewKeyDown;
            _hostWindow = host;
            if (_hostWindow is not null)
                _hostWindow.PreviewKeyDown += OnHostPreviewKeyDown;
        }
        if (_selectedContainer is null || _committingResize)
            return;
        if (!IsSelectionCurrent())
            ClearSelectionCore(raiseChanged: true);
        else
            AttachAdorner();
    }

    private void OnHostPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || _adorner?.IsDragging != true)
            return;
        _adorner.CancelDrag();
        e.Handled = true;
    }

    private bool SelectionMatches(InlineUIContainer container)
    {
        var start = _editor.Selection.Start;
        var end = _editor.Selection.End;
        return start.CompareTo(container.ElementStart) == 0
            && end.CompareTo(container.ElementEnd) == 0;
    }

    private bool IsSelectionCurrent() => _selectedContainer is not null
        && _selectedImage is not null
        && WriterInlineInsertion.IsInlineInDocument(_editor.Document, _selectedContainer)
        && WriterInlineInsertion.TryGetImage(_selectedContainer, out var current)
        && ReferenceEquals(current, _selectedImage);

    private void AttachAdorner()
    {
        if (_selectedImage is null || !_selectedImage.IsLoaded || !IsSelectionCurrent())
            return;
        var layer = AdornerLayer.GetAdornerLayer(_selectedImage);
        if (layer is null)
            return;
        if (_adorner is not null && ReferenceEquals(_adorner.AdornedElement, _selectedImage)
            && ReferenceEquals(_adornerLayer, layer))
            return;

        RemoveAdorner();
        _adorner = new WriterPictureResizeAdorner(_selectedImage, _editor,
            GetMaximumSize, CommitResize);
        _adornerLayer = layer;
        layer.Add(_adorner);
    }

    private bool CommitResize(Image openingSnapshot, Size committedSize)
    {
        if (_selectedContainer is null || !IsSelectionCurrent())
            return false;
        _committingResize = true;
        try
        {
            if (!_images.TryResizeImage(_editor, _selectedContainer, openingSnapshot,
                    committedSize.Width, committedSize.Height, out var replacement))
                return false;
            _selectedContainer = null;
            _selectedImage = null;
            RemoveAdorner();
            return SelectPicture(replacement);
        }
        finally
        {
            _committingResize = false;
        }
    }

    private Size GetMaximumSize()
    {
        var pageWidth = _editor.Document.PageWidth;
        var pageHeight = _editor.Document.PageHeight;
        var padding = _editor.Document.PagePadding;
        var maximumWidth = double.IsFinite(pageWidth)
            ? pageWidth - padding.Left - padding.Right
            : _editor.ActualWidth - _editor.Padding.Left - _editor.Padding.Right;
        var maximumHeight = double.IsFinite(pageHeight)
            ? pageHeight - padding.Top - padding.Bottom
            : Math.Max(_editor.ActualHeight, WriterPictureResizeGeometry.MinimumDimension);
        return new Size(
            Math.Max(WriterPictureResizeGeometry.MinimumDimension, maximumWidth),
            Math.Max(WriterPictureResizeGeometry.MinimumDimension, maximumHeight));
    }

    private bool TryGetImageRect(Image image, out Rect rect)
    {
        rect = Rect.Empty;
        if (!image.IsLoaded || image.ActualWidth <= 0 || image.ActualHeight <= 0)
            return false;
        try
        {
            var origin = image.TranslatePoint(new Point(0, 0), _editor);
            rect = new Rect(origin, new Size(image.ActualWidth, image.ActualHeight));
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void ClearSelectionCore(bool raiseChanged)
    {
        var hadSelection = _selectedContainer is not null || _adorner is not null;
        CancelActiveResize();
        RemoveAdorner();
        _selectedContainer = null;
        _selectedImage = null;
        if (hadSelection && raiseChanged)
            StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RemoveAdorner()
    {
        if (_adorner is not null && _adornerLayer is not null)
            _adornerLayer.Remove(_adorner);
        _adorner = null;
        _adornerLayer = null;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

internal sealed class WriterPictureResizeAdorner : Adorner
{
    internal const double VisibleHandleSize = 8d;
    internal const double HandleHitTargetSize = 16d;

    private static readonly DependencyProperty SelectionBrushProperty =
        DependencyProperty.Register(nameof(SelectionBrush), typeof(Brush),
            typeof(WriterPictureResizeAdorner), new FrameworkPropertyMetadata(Brushes.DodgerBlue,
                FrameworkPropertyMetadataOptions.AffectsRender));
    private static readonly DependencyProperty HandleFillProperty =
        DependencyProperty.Register(nameof(HandleFill), typeof(Brush),
            typeof(WriterPictureResizeAdorner), new FrameworkPropertyMetadata(Brushes.White,
                FrameworkPropertyMetadataOptions.AffectsRender));

    private readonly Image _image;
    private readonly RichTextBox _coordinateRoot;
    private readonly Func<Size> _getMaximum;
    private readonly Func<Image, Size, bool> _commit;
    private WriterPictureResizeHandle? _activeHandle;
    private Point _openingPointer;
    private Size _openingSize;
    private Image? _openingSnapshot;
    private bool _endingCapture;

    internal WriterPictureResizeAdorner(Image image, RichTextBox coordinateRoot,
        Func<Size> getMaximum, Func<Image, Size, bool> commit) : base(image)
    {
        _image = image;
        _coordinateRoot = coordinateRoot;
        _getMaximum = getMaximum;
        _commit = commit;
        Focusable = false;
        SetResourceReference(SelectionBrushProperty, "RibbonKit.Brushes.Accent");
        SetResourceReference(HandleFillProperty, "RibbonKit.Brushes.Control.Background");
    }

    private Brush SelectionBrush => (Brush)GetValue(SelectionBrushProperty);
    private Brush HandleFill => (Brush)GetValue(HandleFillProperty);

    internal bool IsDragging => _activeHandle is not null;

    internal void BeginDragForTesting(WriterPictureResizeHandle handle, Point pointer) =>
        BeginDrag(handle, pointer, captureMouse: false);

    internal void UpdateDragForTesting(Point pointer) => UpdateDrag(pointer);

    internal void CompleteDragForTesting() => CompleteDrag();

    internal void SimulateCaptureLossForTesting() => CancelDrag();

    internal void CancelDrag()
    {
        if (_activeHandle is null)
            return;
        RestoreOpeningGeometry();
        EndCapture();
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var dpi = VisualTreeHelper.GetDpi(this);
        var pen = new Pen(SelectionBrush, Math.Max(1d / dpi.DpiScaleX, 1d));
        drawingContext.DrawRectangle(null, pen, new Rect(0, 0, ActualWidth, ActualHeight));
        var renderedSize = new Size(Math.Max(ActualWidth, 0.001), Math.Max(ActualHeight, 0.001));
        foreach (var rect in WriterPictureResizeGeometry.GetHandleRects(
                     renderedSize, dpi, HandleHitTargetSize).Values)
            drawingContext.DrawRectangle(Brushes.Transparent, null, rect);
        foreach (var rect in WriterPictureResizeGeometry.GetHandleRects(
                     renderedSize, dpi, VisibleHandleSize).Values)
            drawingContext.DrawRectangle(HandleFill, pen, rect);
    }

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters) =>
        TryGetHandle(hitTestParameters.HitPoint, out _)
            ? new PointHitTestResult(this, hitTestParameters.HitPoint)
            : null;

    protected override void OnQueryCursor(QueryCursorEventArgs e)
    {
        base.OnQueryCursor(e);
        if (!TryGetHandle(Mouse.GetPosition(this), out var handle))
            return;
        e.Cursor = GetCursor(handle);
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (!TryGetHandle(e.GetPosition(this), out var handle))
            return;
        BeginDrag(handle, e.GetPosition(_coordinateRoot), captureMouse: true);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_activeHandle is null || e.LeftButton != MouseButtonState.Pressed)
            return;
        UpdateDrag(e.GetPosition(_coordinateRoot));
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_activeHandle is null || _openingSnapshot is null)
            return;
        CompleteDrag();
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (!_endingCapture && _activeHandle is not null)
            CancelDrag();
    }

    protected override AutomationPeer? OnCreateAutomationPeer() => null;

    internal static Cursor GetCursor(WriterPictureResizeHandle handle) => handle switch
    {
        WriterPictureResizeHandle.TopLeft or WriterPictureResizeHandle.BottomRight =>
            Cursors.SizeNWSE,
        WriterPictureResizeHandle.TopRight or WriterPictureResizeHandle.BottomLeft =>
            Cursors.SizeNESW,
        WriterPictureResizeHandle.Left or WriterPictureResizeHandle.Right => Cursors.SizeWE,
        WriterPictureResizeHandle.Top or WriterPictureResizeHandle.Bottom => Cursors.SizeNS,
        _ => throw new ArgumentOutOfRangeException(nameof(handle), handle,
            "Unknown picture handle.")
    };

    private bool TryGetHandle(Point point, out WriterPictureResizeHandle handle)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            handle = default;
            return false;
        }
        return TryGetHandle(point, new Size(ActualWidth, ActualHeight),
            VisualTreeHelper.GetDpi(this), out handle);
    }

    internal static bool TryGetHandle(Point point, Size renderedSize, DpiScale dpi,
        out WriterPictureResizeHandle handle)
    {
        var nearestDistance = double.PositiveInfinity;
        var found = false;
        handle = default;
        foreach (var pair in WriterPictureResizeGeometry.GetHandleRects(
                     renderedSize, dpi, HandleHitTargetSize))
        {
            if (!pair.Value.Contains(point))
                continue;
            var center = new Point(pair.Value.X + pair.Value.Width / 2d,
                pair.Value.Y + pair.Value.Height / 2d);
            var distance = (point - center).LengthSquared;
            if (distance >= nearestDistance)
                continue;
            nearestDistance = distance;
            handle = pair.Key;
            found = true;
        }
        return found;
    }

    private void BeginDrag(WriterPictureResizeHandle handle, Point pointer, bool captureMouse)
    {
        if (!Enum.IsDefined(handle) || !double.IsFinite(pointer.X) || !double.IsFinite(pointer.Y))
            throw new ArgumentOutOfRangeException(nameof(handle), handle, "Invalid picture drag start.");
        _activeHandle = handle;
        _openingPointer = pointer;
        _openingSize = ResolveDisplayedSize(_image);
        _openingSnapshot = WriterImageService.CloneImageElement(_image);
        if (captureMouse)
            Mouse.Capture(this);
    }

    private void UpdateDrag(Point pointer)
    {
        if (_activeHandle is not { } handle)
            return;
        var delta = pointer - _openingPointer;
        var size = WriterPictureResizeGeometry.Resize(_openingSize, delta, handle, _getMaximum());
        _image.Width = size.Width;
        _image.Height = size.Height;
        InvalidateVisual();
    }

    private void CompleteDrag()
    {
        if (_activeHandle is null || _openingSnapshot is null)
            return;
        var committed = new Size(_image.Width, _image.Height);
        var openingSnapshot = _openingSnapshot;
        EndCapture();
        if (!_commit(openingSnapshot, committed))
            RestoreImageFromSnapshot(_image, openingSnapshot);
    }

    private void RestoreOpeningGeometry()
    {
        if (_openingSnapshot is not null)
            RestoreImageFromSnapshot(_image, _openingSnapshot);
    }

    private void EndCapture()
    {
        _endingCapture = true;
        try
        {
            if (IsMouseCaptured)
                ReleaseMouseCapture();
        }
        finally
        {
            _endingCapture = false;
            _activeHandle = null;
            _openingSnapshot = null;
        }
    }

    private static Size ResolveDisplayedSize(Image image)
    {
        var width = double.IsFinite(image.Width) ? image.Width
            : image.ActualWidth > 0 ? image.ActualWidth : image.Source?.Width ?? 0;
        var height = double.IsFinite(image.Height) ? image.Height
            : image.ActualHeight > 0 ? image.ActualHeight : image.Source?.Height ?? 0;
        return new Size(Math.Max(WriterPictureResizeGeometry.MinimumDimension, width),
            Math.Max(WriterPictureResizeGeometry.MinimumDimension, height));
    }

    private static void RestoreImageFromSnapshot(Image target, Image snapshot)
    {
        Restore(FrameworkElement.WidthProperty);
        Restore(FrameworkElement.HeightProperty);

        void Restore(DependencyProperty property)
        {
            var value = snapshot.ReadLocalValue(property);
            if (value == DependencyProperty.UnsetValue)
                target.ClearValue(property);
            else
                target.SetValue(property, value);
        }
    }
}
