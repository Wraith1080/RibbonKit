using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RibbonKit.Showcase;

/// <summary>Places a blurred window snapshot behind a shared ribbon popup's sharp content.</summary>
internal sealed class CrystalPopupBackdrop
{
    private const double Padding = 24;
    private readonly Window _owner;
    private readonly Control _control;
    private readonly string _hostPart;
    private readonly ScrollChangedEventHandler _scrollChanged;
    private Popup? _popup;
    private Border? _host;
    private DispatcherOperation? _pending;
    private Rect _lastBounds;
    private DpiScale _lastDpi;
    private bool _enabled;
    private bool _capturing;
    private bool _removed;

    public CrystalPopupBackdrop(Window owner, Control control, string hostPart)
    {
        _owner = owner;
        _control = control;
        _hostPart = hostPart;
        _control.Loaded += OnControlLoaded;
        _owner.SizeChanged += OnOwnerSizeChanged;
        _owner.Closed += OnOwnerClosed;
        _scrollChanged = (_, _) => Refresh();
        _owner.AddHandler(ScrollViewer.ScrollChangedEvent, _scrollChanged, true);
    }

    public void Apply(bool enabled)
    {
        _enabled = enabled;
        Attach();
        if (enabled) Refresh();
        else RestoreBackground();
    }

    private void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        Attach();
        Refresh();
    }

    private void OnOwnerSizeChanged(object sender, SizeChangedEventArgs e) => Refresh();
    private void OnOwnerClosed(object? sender, EventArgs e) => Remove();

    private void Attach()
    {
        if (_removed) return;
        _control.ApplyTemplate();
        var popup = _control.Template?.FindName("PART_Popup", _control) as Popup;
        var host = _control.Template?.FindName(_hostPart, _control) as Border;
        if (ReferenceEquals(_popup, popup) && ReferenceEquals(_host, host)) return;

        DetachPopup();
        _popup = popup;
        _host = host;
        if (_popup == null) return;
        _popup.Opened += OnOpened;
        _popup.Closed += OnClosed;
        if (_popup.IsOpen) OnOpened(_popup, EventArgs.Empty);
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;
        CompositionTarget.Rendering += OnRendering;
        Refresh();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;
        _pending?.Abort();
        _lastBounds = Rect.Empty;
        RestoreBackground();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!_enabled || _capturing || !CanCapture()) return;
        var bounds = Bounds();
        if (bounds != _lastBounds || !VisualTreeHelper.GetDpi(_owner).Equals(_lastDpi))
            Refresh();
    }

    private bool CanCapture() => _enabled && !_removed && _popup?.IsOpen == true &&
        _host is { ActualWidth: > 0, ActualHeight: > 0 } &&
        PresentationSource.FromVisual(_host) != null && _owner.IsLoaded;

    private Rect Bounds()
    {
        var first = _owner.PointFromScreen(_host!.PointToScreen(new Point(0, 0)));
        var last = _owner.PointFromScreen(_host.PointToScreen(
            new Point(_host.ActualWidth, _host.ActualHeight)));
        return new Rect(first, last);
    }

    public void Refresh()
    {
        if (!CanCapture() || _capturing || _pending?.Status == DispatcherOperationStatus.Pending) return;
        _pending = _owner.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(Capture));
    }

    private void Capture()
    {
        if (!CanCapture()) return;
        var host = _host!;
        _capturing = true;
        try
        {
            var bounds = Bounds();
            var dpi = VisualTreeHelper.GetDpi(_owner);
            var crop = CaptureBlurred(_owner, bounds, dpi);
            if (crop == null) return;

            var surface = new Rect(0, 0, host.ActualWidth, host.ActualHeight);
            var tint = (Brush)_owner.FindResource("Crystal.Brushes.FrostedFrame");
            var material = new DrawingGroup();
            using (var context = material.Open())
            {
                // A popup can extend beyond the owner window. Keep those pixels
                // opaque where the owner has no content to sample.
                context.DrawRectangle((Brush)_owner.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"),
                    null, surface);
                context.DrawImage(crop, surface);
                context.DrawRectangle(tint, null, surface);
            }
            host.Background = new DrawingBrush(material)
            {
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = surface,
                Stretch = Stretch.Fill,
            };
            _lastBounds = bounds;
            _lastDpi = dpi;
        }
        finally { _capturing = false; }
    }

    internal static BitmapSource? CaptureBlurred(FrameworkElement owner, Rect bounds, DpiScale dpi)
    {
        var region = bounds;
        region.Inflate(Padding, Padding);
        int width = (int)Math.Ceiling(region.Width * dpi.DpiScaleX);
        int height = (int)Math.Ceiling(region.Height * dpi.DpiScaleY);
        if (width <= 0 || height <= 0) return null;

        // Popup content lives in another HWND, so rendering the owner excludes
        // its foreground and shadow without changing visibility or keyboard focus.
        var snapshot = new RenderTargetBitmap(width, height,
            dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        var drawing = new DrawingVisual();
        using (var context = drawing.RenderOpen())
            context.DrawRectangle(new VisualBrush(owner)
            {
                AutoLayoutContent = false,
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = region,
                Stretch = Stretch.Fill,
            }, null, new Rect(0, 0, region.Width, region.Height));
        snapshot.Render(drawing);
        snapshot.Freeze();

        var image = new Image
        {
            Source = snapshot,
            Width = region.Width,
            Height = region.Height,
            Stretch = Stretch.Fill,
            Effect = new BlurEffect { Radius = 6, KernelType = KernelType.Gaussian,
                RenderingBias = RenderingBias.Performance },
        };
        image.Measure(new Size(region.Width, region.Height));
        image.Arrange(new Rect(0, 0, region.Width, region.Height));
        var blurred = new RenderTargetBitmap(width, height,
            dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        blurred.Render(image);
        blurred.Freeze();

        int left = (int)Math.Round(Padding * dpi.DpiScaleX);
        int top = (int)Math.Round(Padding * dpi.DpiScaleY);
        int cropWidth = Math.Min(width - left, (int)Math.Ceiling(bounds.Width * dpi.DpiScaleX));
        int cropHeight = Math.Min(height - top, (int)Math.Ceiling(bounds.Height * dpi.DpiScaleY));
        if (cropWidth <= 0 || cropHeight <= 0) return null;
        var crop = new CroppedBitmap(blurred, new Int32Rect(left, top, cropWidth, cropHeight));
        crop.Freeze();
        return crop;
    }

    private void RestoreBackground()
    {
        _host?.SetResourceReference(Border.BackgroundProperty,
            "RibbonKit.Brushes.Ribbon.ContentBackground");
    }

    private void DetachPopup()
    {
        CompositionTarget.Rendering -= OnRendering;
        _pending?.Abort();
        if (_popup != null)
        {
            _popup.Opened -= OnOpened;
            _popup.Closed -= OnClosed;
        }
        RestoreBackground();
        _popup = null;
        _host = null;
        _lastBounds = Rect.Empty;
    }

    public void Remove()
    {
        if (_removed) return;
        _removed = true;
        DetachPopup();
        _control.Loaded -= OnControlLoaded;
        _owner.SizeChanged -= OnOwnerSizeChanged;
        _owner.Closed -= OnOwnerClosed;
        _owner.RemoveHandler(ScrollViewer.ScrollChangedEvent, _scrollChanged);
    }
}
