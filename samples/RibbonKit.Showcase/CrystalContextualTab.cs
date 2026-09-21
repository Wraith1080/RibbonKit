using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Showcase-only tint adapter over the shared RibbonTab template.</summary>
public class CrystalContextualTab : RibbonTab
{
    private bool _crystalEnabled = true;
    private const string TextKey = "Crystal.ContextualText";
    private static readonly string[] ScopedKeys =
    {
        "RibbonKit.Brushes.Tab.SelectedBackground", "RibbonKit.Brushes.Tab.HoverBackground",
        "RibbonKit.Brushes.Tab.SelectedBorderBrush", "RibbonKit.Brushes.Tab.HoverBorder",
        "RibbonKit.Metrics.ContextualUnselectedOpacity", TextKey,
    };

    /// <summary>Enables the experimental palette without changing the requested context color.</summary>
    public bool CrystalEnabled
    {
        get => _crystalEnabled;
        set { _crystalEnabled = value; RefreshPalette(); }
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        RefreshPalette();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == ContextualBrushProperty || e.Property == IsContextualProperty)
            RefreshPalette();
    }

    private void RefreshPalette()
    {
        // Gradient/custom contextual brushes keep the standard renderer until a sampling
        // contract is defined. The prototype derives material colors from solid tints only.
        if (!_crystalEnabled || !IsContextual || ContextualBrush is not SolidColorBrush tint)
        {
            foreach (string key in ScopedKeys) Resources.Remove(key);
            ClearValue(ContextualSelectionBrushProperty);
            if (GetTemplateChild("ContextualHeaderText") is FrameworkElement originalText)
                originalText.ClearValue(TextElement.ForegroundProperty);
            return;
        }

        Color color = tint.Color;
        var surface = new RadialGradientBrush
        {
            Center = new Point(0.5, 0.25), GradientOrigin = new Point(0.5, 0.25),
            RadiusX = 0.85, RadiusY = 0.95,
        };
        surface.GradientStops.Add(new GradientStop(Mix(color, Colors.White, 0.95), 0));
        surface.GradientStops.Add(new GradientStop(Mix(color, Colors.White, 0.86), 0.55));
        surface.GradientStops.Add(new GradientStop(Mix(color, Colors.White, 0.72), 1));
        surface.Freeze();
        Resources[ScopedKeys[0]] = surface;
        Resources[ScopedKeys[1]] = Frozen(Color.FromArgb(28, color.R, color.G, color.B));

        if (TryFindResource("Crystal.Drawing.GlassRim") is DrawingGroup rim)
        {
            DrawingGroup drawing = rim.Clone();
            ((GeometryDrawing)drawing.Children[0]).Brush = Frozen(Mix(color, Colors.White, 0.5));
            var border = new DrawingBrush(drawing) { Viewbox = new Rect(0, 0, 1, 1), ViewboxUnits = BrushMappingMode.Absolute };
            border.Freeze();
            Resources[ScopedKeys[2]] = border;
            DrawingBrush hover = border.Clone();
            hover.Opacity = 0.6;
            hover.Freeze();
            Resources[ScopedKeys[3]] = hover;
        }

        if (TryFindResource("RibbonKit.Brushes.Tab.SelectedUnderline") is DrawingBrush bubble)
        {
            DrawingBrush marker = bubble.Clone();
            var drawing = (DrawingGroup)marker.Drawing;
            var gradient = (LinearGradientBrush)((GeometryDrawing)drawing.Children[0]).Brush;
            double[] light = { 0.35, 0.66, 0.94, 0.80, 0.45 };
            for (int i = 0; i < gradient.GradientStops.Count; i++)
                gradient.GradientStops[i].Color = Mix(color, Colors.White, light[i]);
            marker.Freeze();
            ContextualSelectionBrush = marker;
        }
        Resources[TextKey] = Frozen(Mix(color, Colors.Black, 0.45));
        Resources[ScopedKeys[4]] = 0.85;
        if (GetTemplateChild("ContextualHeaderText") is FrameworkElement text)
            text.SetResourceReference(TextElement.ForegroundProperty, TextKey);
    }

    private static Color Mix(Color color, Color target, double amount) => Color.FromRgb(
        (byte)(color.R + (target.R - color.R) * amount),
        (byte)(color.G + (target.G - color.G) * amount),
        (byte)(color.B + (target.B - color.B) * amount));

    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
