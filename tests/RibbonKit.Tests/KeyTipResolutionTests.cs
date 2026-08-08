using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public class KeyTipResolutionTests
{
    [Fact]
    public void Explicit_keys_are_normalized_and_reserved_before_automatic_keys()
    {
        string[] keys = KeyTipService.ResolveKeys(new[]
        {
            new KeyTipCandidate("Home", null),
            new KeyTipCandidate("Format", "h"),
        });

        Assert.Equal(new[] { "O", "H" }, keys);
    }

    [Fact]
    public void Automatic_keys_walk_each_label_until_the_level_is_unique()
    {
        string[] keys = KeyTipService.ResolveKeys(new[]
        {
            new KeyTipCandidate("Home", null),
            new KeyTipCandidate("Help", null),
            new KeyTipCandidate("History", null),
        });

        Assert.Equal(new[] { "H", "E", "I" }, keys);
    }

    [Fact]
    public void Later_duplicate_explicit_key_falls_back_to_label_derivation()
    {
        string[] keys = KeyTipService.ResolveKeys(new[]
        {
            new KeyTipCandidate("Home", "H"),
            new KeyTipCandidate("Help", "h"),
        });

        Assert.Equal(new[] { "H", "E" }, keys);
    }

    [Theory]
    [InlineData("F", "FN", "F", "I")]
    [InlineData("FN", "F", "FN", "I")]
    public void Later_explicit_prefix_collision_falls_back_to_label_derivation(
        string firstExplicit,
        string secondExplicit,
        string expectedFirst,
        string expectedSecond)
    {
        string[] keys = KeyTipService.ResolveKeys(new[]
        {
            new KeyTipCandidate("File", firstExplicit),
            new KeyTipCandidate("Find", secondExplicit),
        });

        Assert.Equal(new[] { expectedFirst, expectedSecond }, keys);
    }

    [Fact]
    public void Automatic_keys_avoid_prefixes_reserved_by_explicit_keys()
    {
        string[] keys = KeyTipService.ResolveKeys(new[]
        {
            new KeyTipCandidate("Find", "FN"),
            new KeyTipCandidate("File", null),
            new KeyTipCandidate(null, null),
        });

        Assert.Equal(new[] { "FN", "I", "A" }, keys);
        AssertPrefixFree(keys);
    }

    [Fact]
    public void Non_latin_labels_use_a_typeable_ascii_fallback()
    {
        string[] keys = KeyTipService.ResolveKeys(new[]
        {
            new KeyTipCandidate("ملف", null),
            new KeyTipCandidate("خيارات", null),
        });

        Assert.Equal(new[] { "A", "B" }, keys);
    }

    [Theory]
    [InlineData("?")]
    [InlineData("é")]
    [InlineData("   ")]
    public void Untypeable_explicit_keys_fall_back_to_label_derivation(string explicitKeys)
    {
        string[] keys = KeyTipService.ResolveKeys(new[]
        {
            new KeyTipCandidate("Home", explicitKeys),
        });

        Assert.Equal(new[] { "H" }, keys);
    }

    private static void AssertPrefixFree(IEnumerable<string> keys)
    {
        string[] nonEmpty = keys.Where(key => key.Length > 0).ToArray();

        for (int left = 0; left < nonEmpty.Length; left++)
        {
            for (int right = left + 1; right < nonEmpty.Length; right++)
            {
                Assert.False(
                    nonEmpty[left].StartsWith(nonEmpty[right], StringComparison.OrdinalIgnoreCase) ||
                    nonEmpty[right].StartsWith(nonEmpty[left], StringComparison.OrdinalIgnoreCase),
                    $"'{nonEmpty[left]}' and '{nonEmpty[right]}' are not independently typeable.");
            }
        }
    }
}
