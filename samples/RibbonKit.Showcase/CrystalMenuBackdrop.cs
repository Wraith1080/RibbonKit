using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RibbonKit.Showcase;

/// <summary>Refreshes a blurred snapshot behind the menu without blurring its foreground.</summary>
internal sealed class CrystalMenuBackdrop
{
    private const double Padding = 24;
    private readonly FrameworkElement _owner;
    private readonly Border _host;
    private readonly Border _frame;
    private readonly Grid _wrapper;
    private readonly Image _image = new() { Margin = new Thickness(-Padding), Stretch = Stretch.Fill };
    private readonly ScrollChangedEventHandler _scrollChanged;
    private DispatcherOperation? _pending;
    private Rect _lastBounds;
    private DpiScale _lastDpi;
    private bool _capturing;
    private bool _removed;

    internal Grid Layer { get; } = new() { Name = "CrystalMenuBackdrop", IsHitTestVisible = false };

    public CrystalMenuBackdrop(FrameworkElement owner, Border host, Border frame, Grid wrapper)
    {
        _owner = owner;
        _host = host;
        _frame = frame;
        _wrapper = wrapper;
        _image.Effect = new BlurEffect { Radius = 6, KernelType = KernelType.Gaussian,
            RenderingBias = RenderingBias.Performance };
        Layer.Children.Add(_image);
        wrapper.Children.Insert(1, Layer);
        frame.Loaded += OnLoaded;
        frame.IsVisibleChanged += OnVisibleChanged;
        frame.LayoutUpdated += OnLayoutUpdated;
        owner.SizeChanged += OnOwnerSizeChanged;
        _scrollChanged = (_, _) => Refresh();
        owner.AddHandler(ScrollViewer.ScrollChangedEvent, _scrollChanged, true);
        Refresh();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Refresh();
    private void OnOwnerSizeChanged(object sender, SizeChangedEventArgs e) => Refresh();
    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_capturing) return;
        if (_frame.IsVisible) Refresh();
        else _image.Source = null;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (!_capturing && _frame.IsVisible && _owner.IsAncestorOf(_host) &&
            (Bounds() != _lastBounds || !VisualTreeHelper.GetDpi(_owner).Equals(_lastDpi)))
            Refresh();
    }

    // The host is stationary; its child wrapper may still be running the opening slide.
    private Rect Bounds() => _host.TransformToAncestor(_owner).TransformBounds(new Rect(_frame.RenderSize));

    public void Refresh()
    {
        if (_removed || _capturing || _pending?.Status == DispatcherOperationStatus.Pending) return;
        _pending = _owner.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(Capture));
    }

    private void Capture()
    {
        if (_removed || !_frame.IsVisible || !_owner.IsAncestorOf(_host) ||
            _frame.ActualWidth <= 0 || _frame.ActualHeight <= 0) return;
        _lastBounds = Bounds();
        var region = _lastBounds;
        region.Inflate(Padding, Padding);
        var dpi = VisualTreeHelper.GetDpi(_owner);
        _lastDpi = dpi;
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(region.Width * dpi.DpiScaleX),
            (int)Math.Ceiling(region.Height * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY,
            PixelFormats.Pbgra32);
        var opacity = _wrapper.Opacity;
        _capturing = true;
        try
        {
            // Exclude the menu synchronously, without toggling visibility (which can disturb
            // keyboard focus). Opening motion fades the host, not this wrapper. Restore before
            // returning to the dispatcher so no transparent frame is presented onscreen.
            _wrapper.SetCurrentValue(UIElement.OpacityProperty, 0d);
            var drawing = new DrawingVisual();
            using (var context = drawing.RenderOpen())
                context.DrawRectangle(new VisualBrush(_owner)
                {
                    AutoLayoutContent = false, ViewboxUnits = BrushMappingMode.Absolute,
                    Viewbox = region, Stretch = Stretch.Fill,
                }, null, new Rect(0, 0, region.Width, region.Height));
            bitmap.Render(drawing);
            bitmap.Freeze();
        }
        finally
        {
            _wrapper.SetCurrentValue(UIElement.OpacityProperty, opacity);
            _capturing = false;
        }
        _image.Source = bitmap;
        // Clip after the blur so the expanded sampling area cannot spill over the glass rim.
        Layer.Clip = new RectangleGeometry(new Rect(_frame.RenderSize),
            _frame.CornerRadius.TopLeft, _frame.CornerRadius.TopLeft);
    }

    public void Remove()
    {
        _removed = true;
        _pending?.Abort();
        _frame.Loaded -= OnLoaded;
        _frame.IsVisibleChanged -= OnVisibleChanged;
        _frame.LayoutUpdated -= OnLayoutUpdated;
        _owner.SizeChanged -= OnOwnerSizeChanged;
        _owner.RemoveHandler(ScrollViewer.ScrollChangedEvent, _scrollChanged);
        _image.Source = null;
        _wrapper.Children.Remove(Layer);
    }
}
