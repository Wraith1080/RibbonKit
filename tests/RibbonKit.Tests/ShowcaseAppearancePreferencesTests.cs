using RibbonKit.Controls;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public class ShowcaseAppearancePreferencesTests
{
    [Fact]
    public void Round_trip_preserves_every_appearance_preference()
    {
        var source = new ShowcaseAppearancePreferences
        {
            Theme = RibbonTheme.Office2010,
            Accent = "#107c41",
            DarkMode = true,
            AccentedTitleBar = true,
            FrameAppearance = RibbonWindowFrameAppearance.Office2007Aero,
            BackstageDesign = RibbonBackstageDesign.Classic2010,
            BackstageTranslucent = true,
            FileSurface = ShowcaseFileSurface.ApplicationMenu,
            Backdrop = ShowcaseBackdropPreference.Acrylic,
        };

        string json = ShowcaseAppearancePreferencesSerializer.Serialize(source);

        Assert.True(ShowcaseAppearancePreferencesSerializer.TryDeserialize(json, out var restored));
        Assert.Equal(ShowcaseAppearancePreferences.CurrentSchemaVersion, restored.SchemaVersion);
        Assert.Equal(RibbonTheme.Office2010, restored.Theme);
        Assert.Equal("#FF107C41", restored.Accent);
        Assert.True(restored.DarkMode);
        Assert.True(restored.AccentedTitleBar);
        Assert.Equal(RibbonWindowFrameAppearance.Office2007Aero, restored.FrameAppearance);
        Assert.Equal(RibbonBackstageDesign.Classic2010, restored.BackstageDesign);
        Assert.True(restored.BackstageTranslucent);
        Assert.Equal(ShowcaseFileSurface.ApplicationMenu, restored.FileSurface);
        Assert.Equal(ShowcaseBackdropPreference.Acrylic, restored.Backdrop);

        Assert.Contains("\"theme\": \"Office2010\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"backdrop\": \"Acrylic\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"frameAppearance\": \"Office2007Aero\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Defaults_match_the_factory_showcase_appearance()
    {
        var defaults = new ShowcaseAppearancePreferences();

        Assert.Equal(RibbonTheme.Office2024, defaults.Theme);
        Assert.Null(defaults.Accent);
        Assert.False(defaults.DarkMode);
        Assert.False(defaults.AccentedTitleBar);
        Assert.Equal(RibbonWindowFrameAppearance.Default, defaults.FrameAppearance);
        Assert.Equal(RibbonBackstageDesign.Modern, defaults.BackstageDesign);
        Assert.False(defaults.BackstageTranslucent);
        Assert.Equal(ShowcaseFileSurface.Backstage, defaults.FileSurface);
        Assert.Equal(ShowcaseBackdropPreference.None, defaults.Backdrop);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"schemaVersion\":3}")]
    [InlineData("{\"schemaVersion\":1,\"theme\":\"FutureTheme\"}")]
    [InlineData("{\"schemaVersion\":2,\"frameAppearance\":\"FutureFrame\"}")]
    [InlineData("{\"schemaVersion\":1,\"accent\":\"blue-ish\"}")]
    public void Invalid_or_unsupported_documents_are_rejected_without_partial_state(string? json)
    {
        Assert.False(ShowcaseAppearancePreferencesSerializer.TryDeserialize(json, out var restored));
        Assert.Equal(new ShowcaseAppearancePreferences(), restored);
    }

    [Fact]
    public void Version_one_preferences_migrate_to_the_default_frame_appearance()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "theme": "Office2007",
              "backdrop": "Acrylic"
            }
            """;

        Assert.True(ShowcaseAppearancePreferencesSerializer.TryDeserialize(json, out var restored));
        Assert.Equal(ShowcaseAppearancePreferences.CurrentSchemaVersion, restored.SchemaVersion);
        Assert.Equal(RibbonTheme.Office2007, restored.Theme);
        Assert.Equal(RibbonWindowFrameAppearance.Default, restored.FrameAppearance);
        Assert.Equal(ShowcaseBackdropPreference.Acrylic, restored.Backdrop);
    }

    [Theory]
    [InlineData("#107c41", "#FF107C41")]
    [InlineData("#80107c41", "#80107C41")]
    [InlineData(" #D13438 ", "#FFD13438")]
    [InlineData("red", null)]
    [InlineData("#12345", null)]
    public void Accent_normalization_is_stable(string input, string? expected)
    {
        Assert.Equal(expected, ShowcaseAppearancePreferencesSerializer.NormalizeAccent(input));
    }
}
