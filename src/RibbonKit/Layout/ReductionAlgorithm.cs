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
    public static int[] ComputeStates(
        double availableWidth,
        IReadOnlyList<double[]> stateWidths,
        IReadOnlyList<int> reductionOrder)
    {
        ArgumentNullException.ThrowIfNull(stateWidths);
        ArgumentNullException.ThrowIfNull(reductionOrder);

        int count = stateWidths.Count;
        var states = new int[count];
        if (count == 0 || double.IsPositiveInfinity(availableWidth))
        {
            return states;
        }

        double total = 0;
        for (int i = 0; i < count; i++)
        {
            if (stateWidths[i] is null || stateWidths[i].Length == 0)
            {
                throw new ArgumentException(
                    $"Group {i} must provide at least one state width.", nameof(stateWidths));
            }

            total += stateWidths[i][0];
        }

        foreach (int index in reductionOrder)
        {
            if (index < 0 || index >= count)
            {
                throw new ArgumentException(
                    $"Reduction order contains invalid group index {index}.", nameof(reductionOrder));
            }

            while (total > availableWidth)
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

            if (total <= availableWidth)
            {
                break;
            }
        }

        return states;
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
            if (widths[t] < widths[current])
            {
                return t;
            }
        }

        return -1;
    }
}
