using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class SimplifiedSelectionTests
{
    [Fact]
    public void Exact_fit_reserves_more_commands_even_without_displaced_items()
    {
        SimplifiedCommandMeasurement[] commands = [new("copy", "clipboard", 40), new("paste", "clipboard", 40)];
        var result = RibbonSimplifiedSelection.Compute(commands, ["clipboard", "font"], 114, 30, 4, 6);
        Assert.Equal(new[] { 0, 1 }, result.Inline); Assert.Empty(result.Overflow); Assert.Equal(114, result.Extent);
        Assert.Equal(new[] { "clipboard", "font" }, result.FullGroups); Assert.False(result.RequiresScrolling);
        var smaller = RibbonSimplifiedSelection.Compute(commands, ["clipboard", "font"], 113, 30, 4, 6);
        Assert.Equal(new[] { 0 }, smaller.Inline); Assert.Equal(new[] { 1 }, smaller.Overflow);
    }
    [Fact]
    public void Priority_decides_retention_but_neither_inline_nor_overflow_order_changes()
    {
        SimplifiedCommandMeasurement[] commands = [new("a", "g", 40, -100), new("b", "g", 60, 100), new("c", "g", 40), new("d", "g", 40, -100)];
        var result = RibbonSimplifiedSelection.Compute(commands, ["g"], 134, 30, 4, 6);
        Assert.Equal(new[] { 1, 2 }, result.Inline); Assert.Equal(new[] { 0, 3 }, result.Overflow); Assert.Equal(134, result.Extent);
    }
    [Fact]
    public void Removing_a_group_run_rejoins_neighbours_without_leaving_a_divider_or_double_gap()
    {
        SimplifiedCommandMeasurement[] commands = [new("a", "A", 20, 10), new("b", "B", 20), new("c", "A", 20, 10)];
        var full = RibbonSimplifiedSelection.Compute(commands, ["A", "B"], 80, 10, 2, 5);
        Assert.Equal(new[] { 1, 2 }, full.DividerBefore);
        var narrow = RibbonSimplifiedSelection.Compute(commands, ["A", "B"], 55, 10, 2, 5);
        Assert.Equal(new[] { 0, 2 }, narrow.Inline); Assert.Empty(narrow.DividerBefore); Assert.Equal(52, narrow.Extent);
        Assert.Equal(new[] { "A", "B" }, narrow.FullGroups);
    }
    [Fact]
    public void Duplicate_sources_use_first_eligible_declaration_and_return_original_input_indices()
    {
        SimplifiedCommandMeasurement[] commands = [new("copy", "A", 30), new("copy", "B", 100, 100), new("bold", "B", 20)];
        var original = commands.ToArray(); var result = RibbonSimplifiedSelection.Compute(commands, ["A", "B", "A"], 100, 20, 2, 5);
        Assert.Equal(new[] { 0, 2 }, result.Inline); Assert.Equal(new[] { 2 }, result.DividerBefore); Assert.Equal(original, commands);
        Assert.Equal(new[] { "A", "B" }, result.FullGroups);
    }
    [Fact]
    public void Very_narrow_rows_preserve_full_group_access_and_report_the_unshrinkable_more_target()
    {
        SimplifiedCommandMeasurement[] commands = [new("a", "A", 40), new("b", "B", 40)];
        var result = RibbonSimplifiedSelection.Compute(commands, ["A", "B"], 0, 44, 4, 6);
        Assert.Empty(result.Inline); Assert.Equal(new[] { 0, 1 }, result.Overflow); Assert.Equal(new[] { "A", "B" }, result.FullGroups);
        Assert.Equal(44, result.Extent); Assert.True(result.RequiresScrolling);
        var empty = RibbonSimplifiedSelection.Compute([], [], 0, 44, 4, 6); Assert.Equal(0, empty.Extent); Assert.False(empty.RequiresScrolling);
    }
    [Theory]
    [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Device_pixel_measurements_keep_source_order_and_budget_at_representative_dpi(int dpi)
    {
        int S(int value) => value * dpi / 96;
        SimplifiedCommandMeasurement[] commands = [new("copy", "A", S(48)), new("bold", "B", S(32)), new("italic", "B", S(32))];
        var result = RibbonSimplifiedSelection.Compute(commands, ["A", "B"], S(120), S(32), S(4), S(8));
        Assert.Equal(new[] { 0, 1 }, result.Inline); Assert.Equal(new[] { 2 }, result.Overflow); Assert.Equal(S(120), result.Extent);
    }
    [Fact]
    public void Many_priorities_and_repeated_groups_partition_sources_without_losing_access()
    {
        var random = new Random(17);
        for (int sample = 0; sample < 200; sample++)
        {
            var commands = Enumerable.Range(0, random.Next(1, 80)).Select(i => new SimplifiedCommandMeasurement("cmd" + i, "g" + random.Next(3), random.Next(24, 120), random.Next(-2, 3))).ToArray();
            int width = random.Next(32, 900); var result = RibbonSimplifiedSelection.Compute(commands, ["g0", "g1", "g2"], width, 32, 3, 5);
            Assert.Equal(Enumerable.Range(0, commands.Length), result.Inline.Concat(result.Overflow).Order());
            Assert.Equal(result.Inline.Order(), result.Inline); Assert.Equal(result.Overflow.Order(), result.Overflow);
            long expected = 32;
            for (int i = 0; i < result.Inline.Length; i++) { expected += commands[result.Inline[i]].Width; if (i > 0) expected += commands[result.Inline[i - 1]].GroupId == commands[result.Inline[i]].GroupId ? 3 : 5; }
            Assert.Equal(expected, result.Extent); Assert.True(result.Extent <= width);
            if (result.Inline.Length > 0 && result.Overflow.Length > 0) Assert.True(result.Overflow.Max(i => commands[i].Priority) <= result.Inline.Min(i => commands[i].Priority));
        }
        var large = Enumerable.Range(0, 10000).Select(i => new SimplifiedCommandMeasurement("c" + i, "g", 32, i % 7)).ToArray();
        Assert.InRange(RibbonSimplifiedSelection.Compute(large, ["g"], 400, 32, 3, 5).Inline.Length, 1, 11);
    }
    [Fact]
    public void Invalid_measurements_and_unknown_groups_fail_before_selection()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RibbonSimplifiedSelection.Compute([], [], -1, 32, 3, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => RibbonSimplifiedSelection.Compute([], [], 100, 0, 3, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => RibbonSimplifiedSelection.Compute([new("a", "g", 0)], ["g"], 100, 32, 3, 5));
        Assert.Throws<ArgumentException>(() => RibbonSimplifiedSelection.Compute([new("a", "missing", 30)], ["g"], 100, 32, 3, 5));
        Assert.Throws<ArgumentException>(() => RibbonSimplifiedSelection.Compute([new("", "g", 30)], ["g"], 100, 32, 3, 5));
    }
}
