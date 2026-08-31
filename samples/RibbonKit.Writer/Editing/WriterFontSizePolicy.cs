using System.Collections.ObjectModel;
using System.Globalization;

namespace RibbonKit.Writer.Editing;

/// <summary>Defines the validated point-size and grow/shrink policy used by Writer.</summary>
public static class WriterFontSizePolicy
{
    /// <summary>The smallest supported font size in points.</summary>
    public const double MinimumPointSize = 1d;

    /// <summary>The largest supported font size in points.</summary>
    public const double MaximumPointSize = 1638d;

    /// <summary>WPF's device-independent-unit conversion factor for one typographic point.</summary>
    public const double DipPerPoint = 96d / 72d;

    /// <summary>Conventional Office-style sizes, in ascending order.</summary>
    private static readonly IReadOnlyList<double> ConventionalSizes =
        new ReadOnlyCollection<double>(
        [8d, 9d, 10d, 11d, 12d, 14d, 16d, 18d, 20d, 22d, 24d, 26d, 28d, 36d, 48d, 72d]);

    /// <summary>Gets the immutable conventional size list used by the editable size picker.</summary>
    public static IReadOnlyList<double> ConventionalPointSizes => ConventionalSizes;

    /// <summary>Returns whether a point size is finite and within Writer's supported range.</summary>
    public static bool IsValidPointSize(double points)
    {
        return double.IsFinite(points) &&
            points >= MinimumPointSize &&
            points <= MaximumPointSize;
    }

    /// <summary>Parses a point-size entry using the current culture and invariant fallback.</summary>
    /// <param name="text">Editable combo-box text.</param>
    /// <param name="points">The validated point size when parsing succeeds.</param>
    public static bool TryParsePoints(string? text, out double points)
    {
        return TryParsePoints(text, CultureInfo.CurrentCulture, out points);
    }

    /// <summary>
    /// Parses a point-size entry using the supplied culture and then invariant syntax. Both decimal
    /// separators are therefore available without making an invalid value valid.
    /// </summary>
    /// <param name="text">Editable combo-box text.</param>
    /// <param name="formatProvider">Preferred culture; null uses the current culture.</param>
    /// <param name="points">The validated point size when parsing succeeds.</param>
    public static bool TryParsePoints(
        string? text,
        IFormatProvider? formatProvider,
        out double points)
    {
        points = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var provider = formatProvider ?? CultureInfo.CurrentCulture;
        if (TryParseCore(text, provider, out points) && IsValidPointSize(points))
            return true;

        if (!ReferenceEquals(provider, CultureInfo.InvariantCulture) &&
            TryParseCore(text, CultureInfo.InvariantCulture, out points) &&
            IsValidPointSize(points))
            return true;

        points = default;
        return false;
    }

    /// <summary>Converts a validated typographic point value to WPF device-independent units.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The point value is outside Writer's range.</exception>
    public static double PointsToDip(double points)
    {
        EnsureValidPointSize(points, nameof(points));
        return points * DipPerPoint;
    }

    /// <summary>Tries to convert a point value to WPF device-independent units.</summary>
    public static bool TryPointsToDip(double points, out double dip)
    {
        if (!IsValidPointSize(points))
        {
            dip = default;
            return false;
        }

        dip = points * DipPerPoint;
        return true;
    }

    /// <summary>Converts a validated WPF device-independent value back to typographic points.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The converted point value is outside range.</exception>
    public static double DipToPoints(double dip)
    {
        if (!double.IsFinite(dip))
            throw new ArgumentOutOfRangeException(nameof(dip), "The DIP value must be finite.");

        var points = dip / DipPerPoint;
        EnsureValidPointSize(points, nameof(dip));
        return points;
    }

    /// <summary>Tries to convert WPF device-independent units to a validated point size.</summary>
    public static bool TryDipToPoints(double dip, out double points)
    {
        points = default;
        if (!double.IsFinite(dip))
            return false;

        var candidate = dip / DipPerPoint;
        if (!IsValidPointSize(candidate))
            return false;

        points = candidate;
        return true;
    }

    /// <summary>
    /// Grows a size to the next conventional value. A custom value between conventional sizes moves
    /// to the next larger value; a value above the conventional range remains unchanged.
    /// </summary>
    public static double Grow(double points)
    {
        EnsureValidPointSize(points, nameof(points));
        foreach (var conventional in ConventionalSizes)
        {
            if (conventional > points)
                return conventional;
        }

        return points;
    }

    /// <summary>
    /// Shrinks a size to the next conventional value. A custom value between conventional sizes moves
    /// to the next smaller value; a value below the conventional range remains unchanged.
    /// </summary>
    public static double Shrink(double points)
    {
        EnsureValidPointSize(points, nameof(points));
        for (var index = ConventionalSizes.Count - 1; index >= 0; index--)
        {
            var conventional = ConventionalSizes[index];
            if (conventional < points)
                return conventional;
        }

        return points;
    }

    /// <summary>Tries to grow a valid size and reports whether it changed.</summary>
    public static bool TryGrow(double points, out double grown)
    {
        if (!IsValidPointSize(points))
        {
            grown = default;
            return false;
        }

        grown = Grow(points);
        return grown != points;
    }

    /// <summary>Tries to shrink a valid size and reports whether it changed.</summary>
    public static bool TryShrink(double points, out double shrunk)
    {
        if (!IsValidPointSize(points))
        {
            shrunk = default;
            return false;
        }

        shrunk = Shrink(points);
        return shrunk != points;
    }

    private static bool TryParseCore(string text, IFormatProvider provider, out double points)
    {
        return double.TryParse(
            text,
            NumberStyles.Float,
            provider,
            out points);
    }

    private static void EnsureValidPointSize(double points, string parameterName)
    {
        if (!IsValidPointSize(points))
            throw new ArgumentOutOfRangeException(
                parameterName,
                points,
                $"The point size must be finite and between {MinimumPointSize} and {MaximumPointSize} points.");
    }
}
