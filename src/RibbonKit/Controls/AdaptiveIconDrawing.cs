using System.Windows;
using System.Windows.Media;

namespace RibbonKit.Controls;

// Color heuristics, not semantic icon recognition. Sample earlier vector paint so an inset
// on a recolored body can remain distinct. Would-be black parts reveal the surface through
// a partial cutout; source geometry and authored icon objects remain untouched.
internal sealed class AdaptiveIconDrawing
{
    private readonly Color _surface;
    private readonly List<Paint> _paint = new();
    private readonly List<Layer> _layers = new();

    private AdaptiveIconDrawing(Color surface) => _surface = surface;

    internal static ImageSource Create(DrawingImage source, Color surface)
    {
        int count = 0;
        if (!Supported(source.Drawing, ref count)) return source;
        var drawing = source.CloneCurrentValue();
        if (drawing.Drawing is GeometryDrawing { Brush: not null, Pen: not null } root)
        {
            var group = new DrawingGroup();
            group.Children.Add(root);
            drawing.Drawing = group;
        }
        new AdaptiveIconDrawing(surface).Visit(drawing.Drawing, Matrix.Identity, null, 1);
        drawing.Freeze();
        return drawing;
    }

    private static bool Supported(Drawing drawing, ref int count)
    {
        // Bound the one-time work for unusually complex host artwork. No raster fallback.
        if (++count > 128) return false;
        if (drawing is DrawingGroup group)
        {
            foreach (var child in group.Children)
                if (!Supported(child, ref count)) return false;
            return true;
        }
        return drawing switch
        {
            GeometryDrawing geometry => (geometry.Brush is null or SolidColorBrush)
                && (geometry.Pen?.Brush is null or SolidColorBrush),
            _ => false,
        };
    }

    private void Visit(Drawing drawing, Matrix matrix, Geometry? clip, double opacity,
        DrawingCollection? parent = null, int index = -1)
    {
        if (drawing is DrawingGroup group)
        {
            Matrix local = group.Transform?.Value ?? Matrix.Identity;
            local.Append(matrix);
            if (group.ClipGeometry is { } ownClip) clip = Clip(World(ownClip, local), clip);
            // Earlier children may acquire a cutout wrapper while visiting a later child.
            for (int child = 0; child < group.Children.Count; child++)
                Visit(group.Children[child], local, clip, opacity * group.Opacity, group.Children, child);
        }
        else if (drawing is GeometryDrawing { Geometry: { } geometry } shape)
        {
            // Fill and stroke need independent coverage: a partial stroke must also cut
            // through its own filled body rather than merely fading over an opaque fill.
            if (shape.Brush is not null && shape.Pen is not null && parent is not null)
            {
                var strokeDrawing = new GeometryDrawing(null, shape.Pen, geometry);
                shape.Pen = null;
                var split = new DrawingGroup();
                split.Children.Add(shape);
                split.Children.Add(strokeDrawing);
                parent[index] = split;
                Visit(split, matrix, clip, opacity);
                return;
            }
            int paintsBefore = _paint.Count;
            if (shape.Brush is SolidColorBrush fill && fill.Color.A > 0 && fill.Opacity > 0)
            {
                Geometry region = Clip(World(geometry, matrix), clip);
                shape.Brush = Recolor(fill, region, opacity);
            }
            if (shape.Pen is { Brush: SolidColorBrush stroke } pen && stroke.Color.A > 0 && stroke.Opacity > 0)
            {
                Geometry region = Clip(World(geometry.GetWidenedPathGeometry(pen), matrix), clip);
                pen.Brush = Recolor(stroke, region, opacity);
            }
            if (_paint.Count > paintsBefore && parent is not null)
                _layers.Add(new Layer(shape, parent, index, matrix));
        }
    }

    private SolidColorBrush Recolor(SolidColorBrush source, Geometry region, double opacity)
    {
        Color backdrop = Backdrop(region);
        double alpha = source.Color.A / 255d * source.Opacity * opacity;
        Color color = ChooseColor(source.Color, backdrop, alpha);
        if (color.R == 0 && color.G == 0 && color.B == 0
            && (source.Color.R != 0 || source.Color.G != 0 || source.Color.B != 0))
        {
            // Remove preceding paint first. Merely fading this fill would leave an opaque
            // body underneath. Later accents still paint normally over the opening.
            foreach (var layer in _layers) layer.Erase(region);
            foreach (var paint in _paint)
                if (paint.Region.Bounds.IntersectsWith(region.Bounds))
                    paint.Region = Geometry.Combine(paint.Region, region, GeometryCombineMode.Exclude, null);
            color = Color.FromArgb((byte)Math.Round(source.Color.A * .25), source.Color.R, source.Color.G, source.Color.B);
        }
        var brush = source.CloneCurrentValue();
        brush.Color = color;
        _paint.Add(new Paint(region, color, source.Opacity * opacity));
        return brush;
    }

    private Color Backdrop(Geometry region)
    {
        Rect bounds = region.Bounds;
        if (bounds.IsEmpty) return _surface;
        var samples = new Dictionary<Color, int>();
        // A small, fixed grid is independent of DPI; it runs only when building a palette.
        for (int y = 0; y < 9; y++)
        for (int x = 0; x < 9; x++)
        {
            var point = new Point(bounds.X + bounds.Width * (x + .5) / 9,
                bounds.Y + bounds.Height * (y + .5) / 9);
            if (!region.FillContains(point)) continue;
            Color color = _surface;
            foreach (var paint in _paint)
                if (paint.Region.FillContains(point)) color = Mix(color, paint.Color, paint.Color.A / 255d * paint.Opacity);
            samples[color] = samples.GetValueOrDefault(color) + 1;
        }
        return samples.Count == 0 ? _surface : samples.MaxBy(item => item.Value).Key;
    }

    private static Color ChooseColor(Color source, Color backdrop, double alpha)
    {
        const double desiredContrast = 3;
        double Score(Color color) => Contrast(Mix(backdrop, color, alpha), backdrop);
        if (Score(source) >= desiredContrast) return source;

        // Neutral shapes become white/dark. Colored parts take the smallest brightness
        // adjustment that reaches the target, retaining hue as far as this permits.
        int spread = Math.Max(source.R, Math.Max(source.G, source.B)) - Math.Min(source.R, Math.Min(source.G, source.B));
        var white = Color.FromArgb(source.A, 255, 255, 255);
        var black = Color.FromArgb(source.A, 0, 0, 0);
        Color best = Score(white) >= Score(black) ? white : black;
        if (spread <= 24) return best;
        for (int step = 1; step <= 24; step++)
        {
            Color light = Mix(source, white, step / 24d);
            Color dark = Mix(source, black, step / 24d);
            if (Score(light) >= desiredContrast || Score(dark) >= desiredContrast)
                return Score(light) >= Score(dark) ? light : dark;
        }
        return best;
    }

    private static double Contrast(Color first, Color second)
    {
        double a = Luminance(first), b = Luminance(second);
        return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
    }

    private static double Luminance(Color color)
    {
        static double Linear(byte value)
        {
            double channel = value / 255d;
            return channel <= .04045 ? channel / 12.92 : Math.Pow((channel + .055) / 1.055, 2.4);
        }
        return .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
    }

    internal static Color Mix(Color first, Color second, double amount) => Color.FromArgb(first.A,
        (byte)Math.Round(first.R + (second.R - first.R) * amount),
        (byte)Math.Round(first.G + (second.G - first.G) * amount),
        (byte)Math.Round(first.B + (second.B - first.B) * amount));

    private static Geometry World(Geometry geometry, Matrix matrix)
    {
        var world = new GeometryGroup { Transform = new MatrixTransform(matrix) };
        world.Children.Add(geometry);
        return world;
    }

    private static Geometry Clip(Geometry region, Geometry? clip) => clip is null ? region
        : Geometry.Combine(region, clip, GeometryCombineMode.Intersect, null);

    private sealed class Paint(Geometry region, Color color, double opacity)
    {
        internal Geometry Region = region;
        internal readonly Color Color = color;
        internal readonly double Opacity = opacity;
    }

    private sealed class Layer(GeometryDrawing drawing, DrawingCollection parent, int index, Matrix matrix)
    {
        private DrawingGroup? _wrapper;

        internal void Erase(Geometry region)
        {
            if (!matrix.HasInverse || !World(new RectangleGeometry(drawing.Bounds), matrix).Bounds.IntersectsWith(region.Bounds)) return;
            Matrix inverse = matrix;
            inverse.Invert();
            Geometry localHole = World(region, inverse);
            if (_wrapper is null)
            {
                Rect bounds = drawing.Bounds;
                bounds.Inflate(1, 1); // Keep the outer antialiasing fringe outside the cutout.
                _wrapper = new DrawingGroup { ClipGeometry = new RectangleGeometry(bounds) };
                _wrapper.Children.Add(drawing);
                parent[index] = _wrapper;
            }
            _wrapper.ClipGeometry = Geometry.Combine(_wrapper.ClipGeometry, localHole, GeometryCombineMode.Exclude, null);
        }
    }
}
