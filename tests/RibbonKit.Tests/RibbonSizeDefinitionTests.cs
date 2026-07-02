using RibbonKit.Layout;
using Xunit;

namespace RibbonKit.Tests;

public class RibbonSizeDefinitionTests
{
    [Fact]
    public void Parses_standard_definition()
    {
        var sizes = RibbonSizeDefinition.Parse("Large, Medium, Small");

        Assert.Equal(
            new[] { RibbonControlSize.Large, RibbonControlSize.Medium, RibbonControlSize.Small },
            sizes);
    }

    [Fact]
    public void Parsing_is_case_insensitive_and_accepts_middle_alias()
    {
        var sizes = RibbonSizeDefinition.Parse("large,MIDDLE,small");

        Assert.Equal(
            new[] { RibbonControlSize.Large, RibbonControlSize.Medium, RibbonControlSize.Small },
            sizes);
    }

    [Theory]
    [InlineData("Huge, Medium, Small")]
    [InlineData("")]
    [InlineData("   ")]
    public void Invalid_definitions_throw(string definition)
    {
        Assert.ThrowsAny<ArgumentException>(() => RibbonSizeDefinition.Parse(definition));
    }

    [Fact]
    public void SizeFor_maps_each_group_state_to_the_declared_size()
    {
        const string def = "Large, Medium, Small";

        Assert.Equal(RibbonControlSize.Large, RibbonSizeDefinition.SizeFor(def, RibbonGroupSizeState.Large));
        Assert.Equal(RibbonControlSize.Medium, RibbonSizeDefinition.SizeFor(def, RibbonGroupSizeState.Medium));
        Assert.Equal(RibbonControlSize.Small, RibbonSizeDefinition.SizeFor(def, RibbonGroupSizeState.Small));
    }

    [Fact]
    public void SizeFor_clamps_when_definition_is_shorter_than_state_range()
    {
        const string def = "Large, Medium";

        // Small group state clamps to the last declared size.
        Assert.Equal(RibbonControlSize.Medium, RibbonSizeDefinition.SizeFor(def, RibbonGroupSizeState.Small));
    }
}
