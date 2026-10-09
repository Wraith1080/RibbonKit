using Xunit;

namespace RibbonKit.VisualTests;

public sealed class SnapshotSelectionTests
{
    [Fact]
    public void Unset_selection_keeps_the_full_matrix()
    {
        foreach (string? filter in new string?[] { null, "", " " })
        {
            var selection = new SnapshotSelection(filter);
            Assert.False(selection.IsFocused);
            Assert.True(selection.Includes("office2024-default-100"));
            Assert.True(selection.Includes("crystal-dark-qat-below-200"));
            selection.EnsureAllPatternsMatched();
            Assert.Equal(2, selection.SelectedScenes.Count);
        }
    }

    [Fact]
    public void Exact_and_wildcard_patterns_select_only_matching_scenes_once()
    {
        var selection = new SnapshotSelection(
            " OFFICE2024-*-1??, office2024-default-100, crystal-light-qat-below-200 ");
        Assert.True(selection.IsFocused);
        Assert.True(selection.Includes("office2024-default-100"));
        Assert.True(selection.Includes("office2024-dark-125"));
        Assert.False(selection.Includes("office2024-default-200"));
        Assert.True(selection.Includes("crystal-light-qat-below-200"));
        Assert.False(selection.Includes("crystal-dark-qat-below-200"));
        selection.EnsureAllPatternsMatched();
        Assert.Equal(new[]
        {
            "office2024-default-100", "office2024-dark-125", "crystal-light-qat-below-200",
        }, selection.SelectedScenes);
    }

    [Fact]
    public void Every_requested_pattern_must_match_even_when_other_patterns_succeed()
    {
        var selection = new SnapshotSelection("office2024-*,misspelled-scene");
        Assert.True(selection.Includes("office2024-default-100"));
        var failure = Assert.Throws<InvalidOperationException>(selection.EnsureAllPatternsMatched);
        Assert.Contains("misspelled-scene", failure.Message);

        var empty = new SnapshotSelection("missing-*");
        Assert.False(empty.Includes("office2024-default-100"));
        Assert.Throws<InvalidOperationException>(empty.EnsureAllPatternsMatched);
    }

    [Fact]
    public void Empty_patterns_cannot_accidentally_select_the_full_matrix()
    {
        foreach (string filter in new[] { ",", "office2024-*,", ",office2024-*" })
            Assert.Throws<ArgumentException>(() => new SnapshotSelection(filter));
    }
}
