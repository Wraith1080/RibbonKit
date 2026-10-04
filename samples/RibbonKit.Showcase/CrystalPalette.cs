using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using RibbonKit.Theming;

namespace RibbonKit.Showcase;

/// <summary>Adds Showcase document paint to an isolated library Crystal palette.</summary>
internal static class CrystalPalette
{
    /// <summary>The original study tint; selecting it restores the exact source palette.</summary>
    public static Color Blue => Color.FromRgb(57, 124, 169);

    /// <summary>Creates a fresh window palette without changing application resources.</summary>
    public static ResourceDictionary Create(Color accent, bool dark = false)
    {
        var palette = Load(dark ? "Crystal.Dark.xaml" : "Crystal.Light.xaml");
        palette.MergedDictionaries.Clear();
        palette.MergedDictionaries.Add(ThemeManager.CreatePalette(RibbonTheme.CrystalLight, accent, dark));
        // Only Showcase document paint remains here; reusable control tint is in RibbonKit.
        if (accent != Blue || dark) TintResources(palette, Hue(accent) - Hue(Blue), keepText: true);
        return palette;
    }

    private static ResourceDictionary Load(string file) => new()
    {
        Source = new Uri($"/RibbonKit.Showcase;component/Themes/{file}", UriKind.Relative),
    };

    private static void TintResources(ResourceDictionary resources, double rotation, bool keepText)
    {
        foreach (object key in resources.Keys.Cast<object>().ToArray())
        {
            if (key is string semanticKey && semanticKey.StartsWith("RibbonKit.Brushes.MessageBar.", StringComparison.Ordinal))
                continue;
            // These shared composites already resolve the tinted face and wash in
            // their own scope; rotating the same nested brushes would tint twice.
            if (key is "RibbonKit.Brushes.ScrollBar.Thumb" or "RibbonKit.Brushes.ScrollBar.ThumbHover" or "RibbonKit.Brushes.ScrollBar.ThumbPressed")
                continue;
            if (keepText && key is string name &&
                (name.Contains(".Text.") || name.EndsWith("Foreground", StringComparison.Ordinal)
                    || name == "RibbonKit.Brushes.ScrollBar.Glyph"))
                continue;
            if (resources[key] is not Brush original) continue;
            Brush brush = original.Clone();
            TintBrush(brush, rotation);
            if (brush.CanFreeze) brush.Freeze();
            resources[key] = brush;
        }
    }

    private static void TintBrush(Brush brush, double rotation)
    {
        switch (brush)
        {
            case SolidColorBrush solid:
                solid.Color = Rotate(solid.Color, rotation);
                break;
            case GradientBrush gradient:
                foreach (var stop in gradient.GradientStops) stop.Color = Rotate(stop.Color, rotation);
                break;
            case DrawingBrush drawing:
                TintDrawing(drawing.Drawing, rotation);
                break;
        }
    }

    private static void TintDrawing(Drawing drawing, double rotation)
    {
        if (drawing is DrawingGroup group)
            foreach (var child in group.Children) TintDrawing(child, rotation);
        else if (drawing is GeometryDrawing geometry)
        {
            if (geometry.Brush != null) TintBrush(geometry.Brush, rotation);
            if (geometry.Pen != null) TintBrush(geometry.Pen.Brush, rotation);
        }
    }

    // Rotate hue only: retain source saturation, lightness, alpha and highlight geometry.
    // Every variant starts from XAML, avoiding accumulated rounding after repeated changes.
    private static Color Rotate(Color color, double rotation)
    {
        double max = Math.Max(color.R, Math.Max(color.G, color.B));
        double min = Math.Min(color.R, Math.Min(color.G, color.B));
        double chroma = max - min;
        if (chroma == 0) return color;
        double h = ((Hue(color) + rotation) % 360 + 360) % 360 / 60;
        double x = chroma * (1 - Math.Abs(h % 2 - 1));
        (double r, double g, double b) = h switch
        {
            < 1 => (chroma, x, 0d), < 2 => (x, chroma, 0d),
            < 3 => (0d, chroma, x), < 4 => (0d, x, chroma),
            < 5 => (x, 0d, chroma), _ => (chroma, 0d, x),
        };
        return Color.FromArgb(color.A, (byte)Math.Round(r + min),
            (byte)Math.Round(g + min), (byte)Math.Round(b + min));
    }

    private static double Hue(Color color)
    {
        double max = Math.Max(color.R, Math.Max(color.G, color.B));
        double min = Math.Min(color.R, Math.Min(color.G, color.B));
        double delta = max - min;
        if (delta == 0) return 0;
        double hue = max == color.R ? (color.G - color.B) / delta
            : max == color.G ? (color.B - color.R) / delta + 2
            : (color.R - color.G) / delta + 4;
        return (hue * 60 + 360) % 360;
    }

}
