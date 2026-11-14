using System.Collections;

namespace RibbonKit.WinForms;

/// <summary>A color at a normalized intermediate position in a ribbon fill. A stop collection validates positions.</summary>
public readonly record struct RibbonGradientStop(double Offset, Color Color);

/// <summary>Chooses a fill's device-bounds gradient axis.</summary>
public enum RibbonGradientDirection
{
    /// <summary>Runs from the top to the bottom.</summary>
    Vertical,
    /// <summary>Runs from the left to the right.</summary>
    Horizontal
}

/// <summary>An immutable, structurally equal snapshot of intermediate gradient stops, excluding the fill's Start/End endpoints.</summary>
public sealed class RibbonGradientStops : IReadOnlyList<RibbonGradientStop>, IEquatable<RibbonGradientStops>
{
    private readonly RibbonGradientStop[] _stops;
    /// <summary>Copies stops whose finite offsets must increase strictly between zero and one. An empty sequence retains a two-stop fill.</summary>
    public RibbonGradientStops(IEnumerable<RibbonGradientStop> stops)
    {
        ArgumentNullException.ThrowIfNull(stops);
        _stops = stops.ToArray(); double previous = 0;
        foreach (var stop in _stops)
        {
            if (!double.IsFinite(stop.Offset) || stop.Offset <= previous || stop.Offset >= 1)
                throw new ArgumentOutOfRangeException(nameof(stops), "Intermediate offsets must be finite, strictly increasing and between zero and one.");
            previous = stop.Offset;
        }
    }
    /// <summary>Gets the number of intermediate stops.</summary>
    public int Count => _stops.Length;
    /// <summary>Gets an immutable stop by index.</summary>
    public RibbonGradientStop this[int index] => _stops[index];
    /// <summary>Enumerates the copied stops without exposing the backing array.</summary>
    public IEnumerator<RibbonGradientStop> GetEnumerator() => ((IEnumerable<RibbonGradientStop>)_stops).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    /// <summary>Compares stop positions and colors by value.</summary>
    public bool Equals(RibbonGradientStops? other) => other is not null && _stops.AsSpan().SequenceEqual(other._stops);
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is RibbonGradientStops other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() { var hash = new HashCode(); foreach (var stop in _stops) hash.Add(stop); return hash.ToHashCode(); }
}

internal static class RibbonFillColors
{
    internal static bool IsOpaque(RibbonFill fill) => fill.Start.A == 255 && fill.End.A == 255 && (fill.IntermediateStops is null || fill.IntermediateStops.All(s => s.Color.A == 255));
    internal static bool IsSolid(RibbonFill fill) => fill.Start.ToArgb() == fill.End.ToArgb() && (fill.IntermediateStops is null || fill.IntermediateStops.All(s => s.Color.ToArgb() == fill.Start.ToArgb()));
    internal static RibbonFill Map(RibbonFill fill, Func<Color, Color> map) => fill with
    {
        Start = map(fill.Start), End = map(fill.End),
        IntermediateStops = fill.IntermediateStops is { } stops ? new(stops.Select(s => s with { Color = map(s.Color) })) : null
    };
    internal static Color At(RibbonFill fill, double position)
    {
        double left = 0; var start = fill.Start;
        if (fill.IntermediateStops is { } stops)
            foreach (var stop in stops)
            {
                if (position <= stop.Offset) return Mix(start, stop.Color, (position - left) / (stop.Offset - left));
                left = stop.Offset; start = stop.Color;
            }
        return Mix(start, fill.End, (position - left) / (1 - left));
    }
    private static Color Mix(Color start, Color end, double t)
    {
        t = Math.Clamp(t, 0, 1);
        int Part(int from, int to) => (int)Math.Round(from + (to - from) * t);
        return Color.FromArgb(Part(start.A, end.A), Part(start.R, end.R), Part(start.G, end.G), Part(start.B, end.B));
    }
}
