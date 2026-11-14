using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

/// <summary>Opt-in state outlines and inner rims, independent of fills, content and hit geometry. Null/Empty members preserve existing recipes.</summary>
public sealed record RibbonStateChromeSettings
{
    /// <summary>Initializes overrides without adding any state decoration.</summary>
    public RibbonStateChromeSettings() { }
    /// <summary>Gets hover outline ink. Empty retains the existing outline; Transparent hides it.</summary>
    public Color HoverBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets pressed/open outline ink. Empty retains the existing outline.</summary>
    public Color PressedBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets checked/selected outline ink. Empty retains the existing outline.</summary>
    public Color CheckedBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets checked-hover outline ink. Empty follows CheckedBorderColor, then the existing outline.</summary>
    public Color CheckedHoverBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets the inner rim ink, including alpha. Empty/Transparent draws no rim.</summary>
    public Color InnerBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets inner rim width in logical pixels, 0..32. Null/zero draws no rim.</summary>
    public int? InnerBorderWidth { get; init; }
    /// <summary>Gets inner rim inset in logical pixels, 0..1024. Null follows the painted outer stroke; available bounds clamp it.</summary>
    public int? InnerBorderInset { get; init; }
    internal bool HasOverrides => !HoverBorderColor.IsEmpty || !PressedBorderColor.IsEmpty || !CheckedBorderColor.IsEmpty || !CheckedHoverBorderColor.IsEmpty || InnerBorderColor.A != 0 && InnerBorderWidth is > 0;
    internal Color Border(RibbonPaintContext c, Color fallback)
    {
        bool hot = c.Hot || c.StateHot;
        var color = c.Pressed || c.StatePressed ? PressedBorderColor : c.StateChecked ? hot && !CheckedHoverBorderColor.IsEmpty ? CheckedHoverBorderColor : CheckedBorderColor : hot ? HoverBorderColor : Color.Empty;
        return color.IsEmpty ? fallback : color;
    }
    internal static void Validate(RibbonStateChromeSettings? value)
    {
        if (value?.InnerBorderWidth is < 0 or > 32 || value?.InnerBorderInset is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(value), "Inner rim width must be 0..32 and inset 0..1024 logical pixels, or null.");
    }
}

internal static class RibbonStateChromeRenderer
{
    internal static void Paint(RoundedPainter painter, RibbonPaintContext c, int radius, RibbonFill fill, Color border, int stroke, RibbonStateChromeSettings? settings)
    {
        painter.Draw(c.Graphics, c.Bounds, radius, fill, border, stroke, c.IsTopEdgeConnected, c.IsBottomEdgeConnected);
        if (settings?.InnerBorderWidth is not > 0 || settings.InnerBorderColor.A == 0) return;
        int inset = settings.InnerBorderInset is { } logical ? RibbonLayoutEngine.Scale(logical, c.DeviceDpi) : stroke;
        inset = Math.Clamp(inset, 0, Math.Max(0, (Math.Min(c.Bounds.Width, c.Bounds.Height) - 1) / 2));
        var bounds = Rectangle.Inflate(c.Bounds, -inset, -inset);
        int width = RibbonLayoutEngine.Scale(settings.InnerBorderWidth.Value, c.DeviceDpi);
        var mode = c.Graphics.CompositingMode;
        try
        {
            c.Graphics.CompositingMode = CompositingMode.SourceOver;
            painter.Draw(c.Graphics, bounds, Math.Max(0, radius - inset), new(Color.Transparent), settings.InnerBorderColor, width, c.IsTopEdgeConnected, c.IsBottomEdgeConnected);
        }
        finally { c.Graphics.CompositingMode = mode; }
    }
}

public partial class RibbonRenderer
{
    internal void PaintStateChrome(RibbonPaintContext c, int radius, RibbonFill fill, Color border, int stroke, RibbonStateChromeSettings? settings) =>
        RibbonStateChromeRenderer.Paint(_rounded, c, radius, fill, border, stroke, settings);
}
