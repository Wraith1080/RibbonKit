using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RibbonKit.Controls;

// Shared image plumbing: keep the command's original Icon/binding intact, and adapt only the
// small image on a colored QAT. Drawings share a bounded, weakly owned palette cache.
internal static class QatAdaptiveIcon
{
    public static readonly DependencyProperty ActiveProperty = DependencyProperty.RegisterAttached(
        "Active", typeof(bool), typeof(QatAdaptiveIcon),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnContextChanged));
    public static bool GetActive(DependencyObject target) => (bool)target.GetValue(ActiveProperty);
    public static void SetActive(DependencyObject target, bool value) => target.SetValue(ActiveProperty, value);

    public static readonly DependencyProperty SurfaceProperty = DependencyProperty.RegisterAttached(
        "Surface", typeof(Brush), typeof(QatAdaptiveIcon),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.Inherits, OnContextChanged));
    public static Brush GetSurface(DependencyObject target) => (Brush)target.GetValue(SurfaceProperty);
    public static void SetSurface(DependencyObject target, Brush value) => target.SetValue(SurfaceProperty, value);

    public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached(
        "Source", typeof(ImageSource), typeof(QatAdaptiveIcon), new FrameworkPropertyMetadata(null, OnSourceChanged));
    public static ImageSource? GetSource(DependencyObject target) => (ImageSource?)target.GetValue(SourceProperty);
    public static void SetSource(DependencyObject target, ImageSource? value) => target.SetValue(SourceProperty, value);

    private static readonly ConditionalWeakTable<Image, ImageState> Images = new();
    private static readonly ConditionalWeakTable<DrawingImage, PaletteCache> Palettes = new();

    private static void OnSourceChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not Image image) return;
        // WPF can report a Freezable subproperty change before its Changed subscribers run.
        // Invalidate here too, so a live Image never reads the previous mutable palette.
        Invalidate(GetSource(image));
        Images.GetValue(image, static item => new ImageState(item)).Refresh();
    }

    private static void OnContextChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is Image image && Images.TryGetValue(image, out var state)) state.Refresh();
    }

    internal static ImageSource? Adapt(ImageSource? source, Brush surface)
    {
        if (source is not DrawingImage drawing || drawing.Drawing is null) return source;
        Color background = SurfaceColor(surface);
        if (background.A == 0) return source;
        var cache = Palettes.GetValue(drawing, static item => new PaletteCache(item));
        lock (cache)
        {
            if (cache.Images.TryGetValue(background, out var existing)) return existing;
            var adapted = AdaptiveIconDrawing.Create(drawing, background);
            // Accent pickers can produce many colors. Retain only eight palettes per source.
            if (cache.Images.Count == 8) cache.Images.Remove(cache.Images.Keys.First());
            cache.Images.Add(background, adapted);
            return adapted;
        }
    }

    private static void Invalidate(ImageSource? source)
    {
        if (source is DrawingImage { IsFrozen: false } drawing && Palettes.TryGetValue(drawing, out var cache)) cache.Clear();
    }

    private static Color SurfaceColor(Brush surface)
    {
        if (surface is SolidColorBrush solid) return solid.Color;
        if (surface is GradientBrush gradient && gradient.GradientStops.Count > 0)
        {
            var stops = gradient.GradientStops.OrderBy(stop => stop.Offset).ToArray();
            var left = stops.LastOrDefault(stop => stop.Offset <= .5) ?? stops[0];
            var right = stops.FirstOrDefault(stop => stop.Offset >= .5) ?? stops[^1];
            double fraction = right.Offset > left.Offset ? (.5 - left.Offset) / (right.Offset - left.Offset) : 0;
            return AdaptiveIconDrawing.Mix(left.Color, right.Color, fraction);
        }
        return Colors.Transparent;
    }

    private sealed class PaletteCache
    {
        internal readonly Dictionary<Color, ImageSource> Images = new();
        internal PaletteCache(DrawingImage source)
        {
            if (!source.IsFrozen) source.Changed += OnChanged;
        }
        private void OnChanged(object? sender, EventArgs args) => Clear();
        internal void Clear() { lock (this) Images.Clear(); }
    }

    private sealed class ImageState
    {
        private readonly Image _image;
        private ImageSource? _observed;

        internal ImageState(Image image)
        {
            _image = image;
            image.Loaded += OnLoaded;
            image.Unloaded += OnUnloaded;
        }

        internal void Refresh()
        {
            var source = GetSource(_image);
            var observed = _image.IsLoaded && GetActive(_image) && source is { IsFrozen: false } ? source : null;
            if (!ReferenceEquals(_observed, observed))
            {
                // Freeze clears Freezable handlers; removing one afterward is itself a write.
                if (_observed is { IsFrozen: false }) _observed.Changed -= OnDrawingChanged;
                _observed = observed;
                if (_observed is not null) _observed.Changed += OnDrawingChanged;
            }
            _image.Source = GetActive(_image) ? Adapt(source, GetSurface(_image)) : source;
        }

        private void OnDrawingChanged(object? sender, EventArgs args)
        {
            Invalidate(GetSource(_image));
            Refresh();
        }
        private void OnLoaded(object sender, RoutedEventArgs args) => Refresh();
        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            if (_observed is { IsFrozen: false }) _observed.Changed -= OnDrawingChanged;
            _observed = null;
        }
    }
}
