// Native implementation of the ordering/priority contract documented by WPF's
// Simplified Ribbon plan. No WPF types or source presentation ownership is used.
namespace RibbonKit.WinForms.Layout;

internal readonly record struct SimplifiedCommandMeasurement(string CommandId, string GroupId, int Width, int Priority = 0);
internal sealed record SimplifiedSelection(int[] Inline, int[] Overflow, int[] DividerBefore, string[] FullGroups,
    long Extent, bool RequiresScrolling);

// Inputs are already measured device pixels and eligible original sources in
// draft order. Return original input indices, never reordered/copied commands.
internal static class RibbonSimplifiedSelection
{
    internal static SimplifiedSelection Compute(IReadOnlyList<SimplifiedCommandMeasurement> commands,
        IReadOnlyList<string> fullGroups, int availableWidth, int moreWidth, int commandGap, int dividerWidth)
    {
        ArgumentNullException.ThrowIfNull(commands); ArgumentNullException.ThrowIfNull(fullGroups);
        ArgumentOutOfRangeException.ThrowIfNegative(availableWidth); ArgumentOutOfRangeException.ThrowIfNegativeOrZero(moreWidth);
        ArgumentOutOfRangeException.ThrowIfNegative(commandGap); ArgumentOutOfRangeException.ThrowIfNegative(dividerWidth);
        if (fullGroups.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Group identities must be nonempty.", nameof(fullGroups));
        var groups = fullGroups.Distinct(StringComparer.Ordinal).ToArray(); var groupIds = groups.ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal); var sourceIndices = new List<int>();
        for (int i = 0; i < commands.Count; i++)
        {
            var command = commands[i];
            if (string.IsNullOrWhiteSpace(command.CommandId) || string.IsNullOrWhiteSpace(command.GroupId) || !groupIds.Contains(command.GroupId))
                throw new ArgumentException("Commands need source identities and an available original group.", nameof(commands));
            if (command.Width <= 0) throw new ArgumentOutOfRangeException(nameof(commands), "Measured command widths must be positive.");
            if (seen.Add(command.CommandId)) sourceIndices.Add(i);
        }
        int count = sourceIndices.Count;
        var previous = Enumerable.Range(-1, count).ToArray(); var next = Enumerable.Range(1, count).ToArray();
        if (count > 0) next[^1] = -1;
        var inline = Enumerable.Repeat(true, count).ToArray();
        int Between(int a, int b) => StringComparer.Ordinal.Equals(commands[sourceIndices[a]].GroupId, commands[sourceIndices[b]].GroupId) ? commandGap : dividerWidth;
        long extent = groups.Length > 0 ? moreWidth : 0;
        for (int i = 0; i < count; i++) { extent += commands[sourceIndices[i]].Width; if (i > 0) extent += Between(i - 1, i); }
        // Lowest priority leaves first. Equal priorities leave from the end.
        // Linked neighbours update divider/run costs in O(1) per removal.
        foreach (int index in Enumerable.Range(0, count).OrderBy(i => commands[sourceIndices[i]].Priority).ThenByDescending(i => i))
        {
            if (extent <= availableWidth) break;
            int left = previous[index], right = next[index];
            extent -= commands[sourceIndices[index]].Width;
            if (left >= 0) extent -= Between(left, index);
            if (right >= 0) extent -= Between(index, right);
            if (left >= 0 && right >= 0) extent += Between(left, right);
            if (left >= 0) next[left] = right;
            if (right >= 0) previous[right] = left;
            inline[index] = false;
        }
        var retained = Enumerable.Range(0, count).Where(i => inline[i]).Select(i => sourceIndices[i]).ToArray();
        var displaced = Enumerable.Range(0, count).Where(i => !inline[i]).Select(i => sourceIndices[i]).ToArray();
        var dividers = retained.Where((index, position) => position > 0 && !StringComparer.Ordinal.Equals(commands[retained[position - 1]].GroupId, commands[index].GroupId)).ToArray();
        return new(retained, displaced, dividers, groups, extent, extent > availableWidth);
    }
}
