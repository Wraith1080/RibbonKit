namespace RibbonKit.Layout;

/// <summary>
/// Pure, WPF-free core of the adaptive sizing engine. Given the width each group
/// occupies at each of its size states, decides which state every group should use
/// so the row fits the available width.
/// </summary>
/// <remarks>
/// <para>
/// Reduction follows a caller-supplied order (see
/// <see cref="ComputeStates(double, IReadOnlyList{double[]}, IReadOnlyList{int})"/>):
/// each group in the order is stepped down as far as needed — fully exhausting its
/// states — before the next group is touched.
/// </para>
/// <para>
/// A step is only taken when it actually makes the group narrower. Layouts where a
/// "smaller" state is wider than the previous one (e.g. three large buttons whose
/// icon+label medium row is wider than the large stack) are skipped over, jumping
/// straight to the next genuinely narrower state. Without this, one threshold could
/// cascade every group to its smallest state at once.
/// </para>
/// <para>Kept free of any WPF dependency so it can be unit-tested directly.</para>
/// </remarks>
public static class ReductionAlgorithm
{
    // Same scale-aware tolerance pattern WPF uses internally for layout-double comparisons.
    // It absorbs binary representation noise (for example 0.1 + 0.2 vs 0.3) without hiding a
    // meaningful DIP difference at ordinary ribbon dimensions.
    private const double DoubleEpsilon = 2.2204460492503131e-016;

    /// <summary>
    /// Computes the size-state index for each group using the default order:
    /// rightmost group first.
    /// </summary>
    /// <param name="availableWidth">Width available to the whole row of groups.</param>
    /// <param name="stateWidths">
    /// One array per group: the group's desired width at each state, index 0 being the
    /// largest. A single-element array marks a non-adaptive child.
    /// </param>
    /// <returns>The chosen state index for each group (0 = largest).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stateWidths"/> is null.</exception>
    /// <exception cref="ArgumentException">A group has no state widths.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="availableWidth"/> or a state width is negative, NaN, or an unsupported
    /// infinity.
    /// </exception>
    public static int[] ComputeStates(double availableWidth, IReadOnlyList<double[]> stateWidths)
    {
        ArgumentNullException.ThrowIfNull(stateWidths);

        var rightmostFirst = new int[stateWidths.Count];
        for (int i = 0; i < rightmostFirst.Length; i++)
        {
            rightmostFirst[i] = rightmostFirst.Length - 1 - i;
        }

        return ComputeStates(availableWidth, stateWidths, rightmostFirst);
    }

    /// <summary>
    /// Computes the size-state index for each group, reducing groups in the given order.
    /// </summary>
    /// <param name="availableWidth">Width available to the whole row of groups.</param>
    /// <param name="stateWidths">
    /// One array per group: the group's desired width at each state, index 0 being the
    /// largest. A single-element array marks a child that cannot shrink.
    /// </param>
    /// <param name="reductionOrder">
    /// Group indices in the order they should be reduced. Each group is fully exhausted
    /// before the next is touched. Indices omitted from the order are never reduced.
    /// </param>
    /// <returns>The chosen state index for each group (0 = largest).</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="stateWidths"/> or <paramref name="reductionOrder"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A group has no state widths, or <paramref name="reductionOrder"/> contains an invalid index.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="availableWidth"/> or a state width is negative, NaN, or an unsupported
    /// infinity.
    /// </exception>
    public static int[] ComputeStates(
        double availableWidth,
        IReadOnlyList<double[]> stateWidths,
        IReadOnlyList<int> reductionOrder)
    {
        ArgumentNullException.ThrowIfNull(stateWidths);
        ArgumentNullException.ThrowIfNull(reductionOrder);

        int count = stateWidths.Count;
        var states = new int[count];
        ValidateInputs(availableWidth, stateWidths, reductionOrder);
        if (count == 0 || double.IsPositiveInfinity(availableWidth))
        {
            return states;
        }

        double total = 0;
        for (int i = 0; i < count; i++)
        {
            total += stateWidths[i][0];
        }

        foreach (int index in reductionOrder)
        {
            while (GreaterThan(total, availableWidth))
            {
                int next = NextNarrowerState(stateWidths[index], states[index]);
                if (next < 0)
                {
                    break; // This group is exhausted; move to the next one in the order.
                }

                total -= stateWidths[index][states[index]];
                states[index] = next;
                total += stateWidths[index][next];
            }

            if (!GreaterThan(total, availableWidth))
            {
                break;
            }
        }

        return states;
    }

    private static void ValidateInputs(
        double availableWidth,
        IReadOnlyList<double[]> stateWidths,
        IReadOnlyList<int> reductionOrder)
    {
        if (double.IsNaN(availableWidth)
            || double.IsNegativeInfinity(availableWidth)
            || availableWidth < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(availableWidth),
                availableWidth,
                "Available width must be non-negative or positive infinity.");
        }

        double largeWidthTotal = 0;
        for (int i = 0; i < stateWidths.Count; i++)
        {
            double[]? widths = stateWidths[i];
            if (widths is null || widths.Length == 0)
            {
                throw new ArgumentException(
                    $"Group {i} must provide at least one state width.", nameof(stateWidths));
            }

            for (int state = 0; state < widths.Length; state++)
            {
                double width = widths[state];
                if (!double.IsFinite(width) || width < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(stateWidths),
                        width,
                        $"Group {i}, state {state} must have a finite, non-negative width.");
                }
            }

            largeWidthTotal += widths[0];
            if (!double.IsFinite(largeWidthTotal))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stateWidths),
                    "The combined large-state width must be finite.");
            }
        }

        foreach (int index in reductionOrder)
        {
            if (index < 0 || index >= stateWidths.Count)
            {
                throw new ArgumentException(
                    $"Reduction order contains invalid group index {index}.", nameof(reductionOrder));
            }
        }
    }

    /// <summary>
    /// Finds the next state after <paramref name="current"/> that is strictly narrower,
    /// skipping states that would not reduce (or would increase) the group's width.
    /// Returns -1 when no narrower state exists.
    /// </summary>
    private static int NextNarrowerState(double[] widths, int current)
    {
        for (int t = current + 1; t < widths.Length; t++)
        {
            if (LessThan(widths[t], widths[current]))
            {
                return t;
            }
        }

        return -1;
    }

    private static bool GreaterThan(double left, double right) =>
        left > right && !AreClose(left, right);

    private static bool LessThan(double left, double right) =>
        left < right && !AreClose(left, right);

    private static bool AreClose(double left, double right)
    {
        if (left == right)
        {
            return true;
        }

        double tolerance = (Math.Abs(left) + Math.Abs(right) + 10.0) * DoubleEpsilon;
        return Math.Abs(left - right) < tolerance;
    }
}
