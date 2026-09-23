using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RibbonKit.Showcase;

/// <summary>Shares the accepted customization scrollbar material with preview scroll viewers.</summary>
internal sealed class CrystalScrollBars
{
    private readonly ScrollViewer _viewer;
    private readonly ResourceDictionary _scope = new();

    public CrystalScrollBars(ScrollViewer viewer)
    {
        _viewer = viewer;
        var templates = new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Controls.ScrollBars.xaml", UriKind.Relative) };
        _scope.MergedDictionaries.Add(templates);
        _scope[typeof(ScrollBar)] = new Style(typeof(ScrollBar), (Style)templates["RibbonKit.ScrollBarStyle"]);
    }

    public void Apply(ResourceDictionary? palette, Color accent)
    {
        if (palette == null)
        {
            _viewer.Resources.MergedDictionaries.Remove(_scope);
            return;
        }
        Configure(_scope, palette, accent);
        if (!_viewer.Resources.MergedDictionaries.Contains(_scope))
            _viewer.Resources.MergedDictionaries.Add(_scope);
    }

    internal static void Configure(ResourceDictionary resources, ResourceDictionary palette, Color accent)
    {
        foreach (var (target, source) in new[]
        {
            ("Glyph", "Text.Secondary"), ("ButtonBackground", "Control.SurfaceBackground"),
            ("ButtonBorder", "Control.HoverBorder"), ("ThumbBorder", "Control.HoverBorder"),
        }) resources["RibbonKit.Brushes.ScrollBar." + target] = palette["RibbonKit.Brushes." + source];
        resources["RibbonKit.Brushes.ScrollBar.Track"] = Brushes.Transparent;
        var face = (Brush)palette["RibbonKit.Brushes.Control.SurfaceBackground"];
        resources["RibbonKit.Brushes.ScrollBar.Thumb"] = WithWash(face, accent, 0.06);
        resources["RibbonKit.Brushes.ScrollBar.ThumbHover"] = WithWash(face, accent, 0.08);
        resources["RibbonKit.Brushes.ScrollBar.ThumbPressed"] = WithWash(face, accent, 0.11);
        resources["RibbonKit.Metrics.ScrollBar.Thickness"] = 14d;
        resources["RibbonKit.Metrics.ScrollBar.ThumbBorderThickness"] = new Thickness(1);
        foreach (var name in new[] { "ButtonCornerRadius", "ThumbCornerRadius", "RailCornerRadius" })
            resources["RibbonKit.Metrics.ScrollBar." + name] = new CornerRadius(4);
    }

    internal static DrawingBrush WithWash(Brush source, Color color, double amount)
    {
        var face = new RectangleGeometry(new Rect(0, 0, 1, 1));
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(source.CloneCurrentValue(), null, face));
        var wash = new SolidColorBrush(Color.FromArgb((byte)Math.Round(amount * 255), color.R, color.G, color.B));
        drawing.Children.Add(new GeometryDrawing(wash, null, face));
        var brush = new DrawingBrush(drawing) { Viewbox = new Rect(0, 0, 1, 1), ViewboxUnits = BrushMappingMode.Absolute, Stretch = Stretch.Fill };
        brush.Freeze();
        return brush;
    }
}
