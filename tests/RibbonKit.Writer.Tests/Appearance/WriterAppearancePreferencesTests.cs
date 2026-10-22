using System.IO;
using RibbonKit;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Interop;
using RibbonKit.Theming;
using RibbonKit.Writer.Appearance;
using Xunit;

namespace RibbonKit.Writer.Tests.Appearance;

public sealed class WriterAppearancePreferencesTests
{
    [Theory]
    [InlineData(RibbonQuickAccessIconAppearance.WhiteOverlay)]
    [InlineData(RibbonQuickAccessIconAppearance.ColorWithGlow)]
    [InlineData(RibbonQuickAccessIconAppearance.AdaptiveColor)]
    public void FlatTouchAndQatChoicesRoundTripWithoutChangingOtherPreferences(RibbonQuickAccessIconAppearance icons)
    {
        var expected = new WriterAppearancePreferences
        {
            Theme = RibbonTheme.Office2024Flat, Density = RibbonDensity.Touch,
            QuickAccessIconAppearance = icons, DarkPalette = true, Accent = "#FF7030A0",
            Backdrop = RibbonBackdrop.Acrylic, GlassSurfaces = true, BackstageTranslucent = true,
            CapturedPopupBackdrop = true, ShowRuler = false, ShowMarginGuides = false,
        };
        Assert.True(WriterAppearancePreferencesSerializer.TryDeserialize(
            WriterAppearancePreferencesSerializer.Serialize(expected), out var actual));
        Assert.Equal(expected, actual);
        Assert.Equal(RibbonBackdrop.Acrylic, WriterAppearanceCompatibility.ResolveBackdrop(actual, true));
        Assert.True(MainWindow.ShouldUseTransparentRulerSurface(actual.Theme, true, false));
        Assert.False(MainWindow.ShouldUseTransparentRulerSurface(actual.Theme, false, false));
        Assert.False(MainWindow.ShouldUseTransparentRulerSurface(actual.Theme, true, true));
    }

    [Fact]
    public void OldVersionOneSettingsPreserveAllFieldsAndGainCompatibleDefaults()
    {
        const string json = """
            {"SchemaVersion":1,"Theme":"CrystalLight","DarkPalette":true,"Accent":"#FF7030A0",
             "AccentedTitleBar":true,"BackstageDesign":"CrystalFloating","Backdrop":"Acrylic",
             "BackstageTranslucent":true,"GlassSurfaces":true,"CapturedPopupBackdrop":true,
             "AnimationLevel":"Expressive","RespectSystemReducedMotion":false,
             "ShowRuler":false,"ShowMarginGuides":false}
            """;
        Assert.True(WriterAppearancePreferencesSerializer.TryDeserialize(json, out var restored));
        Assert.Equal(new WriterAppearancePreferences
        {
            Theme = RibbonTheme.CrystalLight, DarkPalette = true, Accent = "#FF7030A0",
            AccentedTitleBar = true, BackstageDesign = RibbonBackstageDesign.CrystalFloating,
            Backdrop = RibbonBackdrop.Acrylic, BackstageTranslucent = true, GlassSurfaces = true,
            CapturedPopupBackdrop = true, AnimationLevel = RibbonAnimationLevel.Expressive,
            RespectSystemReducedMotion = false, ShowRuler = false, ShowMarginGuides = false,
        }, restored);
        Assert.Equal(RibbonDensity.Compact, restored.Density);
        Assert.Equal(RibbonQuickAccessIconAppearance.WhiteOverlay, restored.QuickAccessIconAppearance);
    }

    [Theory]
    [InlineData("Density", "FutureMode")]
    [InlineData("Density", "17")]
    [InlineData("QuickAccessIconAppearance", "FutureIcons")]
    [InlineData("QuickAccessIconAppearance", "17")]
    public void InvalidNewPreferencesAreRejected(string name, string value)
    {
        Assert.False(WriterAppearancePreferencesSerializer.TryDeserialize(
            $"{{\"SchemaVersion\":1,\"{name}\":\"{value}\"}}", out var actual));
        Assert.Equal(new WriterAppearancePreferences(), actual);
    }

    [Theory]
    [InlineData(false, RibbonBackstageDesign.CrystalSidebar)]
    [InlineData(true, RibbonBackstageDesign.CrystalFloating)]
    public void CrystalSettingsRoundTripAndRestartWithCompatibleDefaults(bool dark, RibbonBackstageDesign design)
    {
        var expected = new WriterAppearancePreferences
        {
            Theme = RibbonTheme.CrystalLight, DarkPalette = dark, BackstageDesign = design,
            Accent = "#FF7030A0", GlassSurfaces = true, CapturedPopupBackdrop = true,
        };
        Assert.True(WriterAppearancePreferencesSerializer.TryDeserialize(
            WriterAppearancePreferencesSerializer.Serialize(expected), out var actual));
        Assert.Equal(expected, actual);
        Assert.Equal(RibbonBackstageDesign.CrystalSidebar, WriterAppearanceCompatibility.Normalize(
            expected with { BackstageDesign = RibbonBackstageDesign.Modern }).BackstageDesign);
        Assert.Equal(RibbonBackstageDesign.Modern, WriterAppearanceCompatibility.Normalize(
            expected with { Theme = RibbonTheme.Office2024 }).BackstageDesign);
        Assert.Equal(RibbonWindowFrameAppearance.Default, WriterAppearanceCompatibility.Normalize(
            expected with { FrameAppearance = RibbonWindowFrameAppearance.Office2007Aero }).FrameAppearance);
        Assert.False(WriterAppearanceCompatibility.CanUseBackstageTranslucency(expected, false));
        Assert.Equal(RibbonApplicationButtonShape.Tab, actual.ApplicationButtonShape);
        Assert.False(new WriterAppearancePreferences().GlassSurfaces);
        Assert.False(new WriterAppearancePreferences().CapturedPopupBackdrop);
    }

    [Fact]
    public void ExistingVersionOneSettingsKeepOptionalEffectsDisabled()
    {
        Assert.True(WriterAppearancePreferencesSerializer.TryDeserialize(
            "{\"SchemaVersion\":1,\"Theme\":\"Office2010\",\"BackstageDesign\":\"Classic2010\"}", out var restored));
        Assert.Equal(RibbonTheme.Office2010, restored.Theme);
        Assert.False(restored.GlassSurfaces);
        Assert.False(restored.CapturedPopupBackdrop);
    }

    [Fact]
    public void RulerRevealsMaterialOnlyForActiveOffice2024Backdrop()
    {
        Assert.True(MainWindow.ShouldUseTransparentRulerSurface(
            RibbonTheme.Office2024,
            isBackdropActive: true,
            highContrast: false));
        Assert.False(MainWindow.ShouldUseTransparentRulerSurface(
            RibbonTheme.Office2024,
            isBackdropActive: false,
            highContrast: false));
        Assert.False(MainWindow.ShouldUseTransparentRulerSurface(
            RibbonTheme.Office2019,
            isBackdropActive: true,
            highContrast: false));
        Assert.False(MainWindow.ShouldUseTransparentRulerSurface(
            RibbonTheme.Office2024,
            isBackdropActive: true,
            highContrast: true));
        Assert.True(MainWindow.ShouldUseTransparentRulerSurface(
            RibbonTheme.CrystalLight, isBackdropActive: true, highContrast: false));
        Assert.False(MainWindow.ShouldUseTransparentRulerSurface(
            RibbonTheme.CrystalLight, isBackdropActive: false, highContrast: false));
        Assert.False(MainWindow.ShouldUseTransparentRulerSurface(
            RibbonTheme.CrystalLight, isBackdropActive: true, highContrast: true));
    }

    [Fact]
    public void VersionedSettingsRoundTripEveryWriterAppearanceChoice()
    {
        var expected = new WriterAppearancePreferences
        {
            Theme = RibbonTheme.Office2007,
            DarkPalette = true,
            Accent = "#123456",
            AccentedTitleBar = false,
            BackstageDesign = RibbonBackstageDesign.Glass2007,
            BackstageTranslucent = true,
            Backdrop = RibbonBackdrop.Acrylic,
            FrameAppearance = RibbonWindowFrameAppearance.Office2007Aero,
            ApplicationButtonShape = RibbonApplicationButtonShape.Orb,
            AnimationLevel = RibbonAnimationLevel.Expressive,
            RespectSystemReducedMotion = false,
            ShowRuler = false,
            ShowMarginGuides = false,
        };

        string json = WriterAppearancePreferencesSerializer.Serialize(expected);

        Assert.True(WriterAppearancePreferencesSerializer.TryDeserialize(json, out var actual));
        Assert.Equal(expected with { Accent = "#FF123456" }, actual);
    }

    [Fact]
    public void InvalidSchemaEnumAndAccentAreRejectedWithoutPartialState()
    {
        Assert.False(WriterAppearancePreferencesSerializer.TryDeserialize(
            "{\"SchemaVersion\":99}", out var future));
        Assert.Equal(new WriterAppearancePreferences(), future);

        Assert.False(WriterAppearancePreferencesSerializer.TryDeserialize(
            "{\"SchemaVersion\":1,\"Theme\":\"FutureOffice\"}", out var unknown));
        Assert.Equal(new WriterAppearancePreferences(), unknown);

        Assert.False(WriterAppearancePreferencesSerializer.TryDeserialize(
            "{\"SchemaVersion\":1,\"Accent\":\"not-a-color\"}", out var badAccent));
        Assert.Equal(new WriterAppearancePreferences(), badAccent);
    }

    [Fact]
    public void ThemeCompatibilityUsesDiscoverableChoicesWithValidFallbacks()
    {
        var incompatible = new WriterAppearancePreferences
        {
            Theme = RibbonTheme.Office2024,
            BackstageDesign = RibbonBackstageDesign.Classic2007,
            FrameAppearance = RibbonWindowFrameAppearance.Office2007Aero,
            ApplicationButtonShape = RibbonApplicationButtonShape.Orb,
            Backdrop = RibbonBackdrop.Mica,
            AccentedTitleBar = true,
        };

        WriterAppearancePreferences normalized =
            WriterAppearanceCompatibility.Normalize(incompatible);

        Assert.Equal(RibbonBackstageDesign.Modern, normalized.BackstageDesign);
        Assert.Equal(RibbonWindowFrameAppearance.Default, normalized.FrameAppearance);
        Assert.Equal(RibbonApplicationButtonShape.Tab, normalized.ApplicationButtonShape);
        Assert.Equal(RibbonBackdrop.Mica, normalized.Backdrop);
        Assert.True(normalized.AccentedTitleBar);

        var aero = incompatible with
        {
            Theme = RibbonTheme.Office2007,
            BackstageDesign = RibbonBackstageDesign.Glass2007,
            FrameAppearance = RibbonWindowFrameAppearance.Office2007Aero,
            ApplicationButtonShape = RibbonApplicationButtonShape.Orb,
            Backdrop = RibbonBackdrop.Mica,
            AccentedTitleBar = true,
        };
        normalized = WriterAppearanceCompatibility.Normalize(aero);

        Assert.Equal(RibbonBackstageDesign.Glass2007, normalized.BackstageDesign);
        Assert.Equal(RibbonWindowFrameAppearance.Office2007Aero, normalized.FrameAppearance);
        Assert.Equal(RibbonApplicationButtonShape.Orb, normalized.ApplicationButtonShape);
        Assert.Equal(RibbonBackdrop.None, normalized.Backdrop);
        Assert.False(normalized.AccentedTitleBar);

        normalized = WriterAppearanceCompatibility.Normalize(aero with
        {
            ApplicationButtonShape = RibbonApplicationButtonShape.Tab,
        });
        Assert.Equal(RibbonApplicationButtonShape.Orb, normalized.ApplicationButtonShape);
    }

    [Fact]
    public void AppearanceDefaultsDoNotChangeRibbonLayoutAndCorruptionFallsBackSafely()
    {
        string directory = Path.Combine(Path.GetTempPath(), "RibbonKit.Writer.W4A", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string appearancePath = Path.Combine(directory, "appearance.json");
            string ribbonPath = Path.Combine(directory, "ribbon.json");
            var store = new WriterSettingsStore(new WriterSettingsPaths(appearancePath, ribbonPath));
            const string ribbonLayout = "{\"schemaVersion\":1,\"tabs\":[]}";
            var custom = new WriterAppearancePreferences
            {
                Theme = RibbonTheme.Office2013,
                DarkPalette = true,
                ShowRuler = false,
            };

            Assert.True(store.SaveRibbonLayout(ribbonLayout));
            Assert.True(store.SaveAppearance(custom));
            Assert.Equal(custom, store.LoadAppearance());
            Assert.Equal(ribbonLayout, store.LoadRibbonLayout());

            Assert.True(store.SaveAppearance(new WriterAppearancePreferences()));
            Assert.Equal(ribbonLayout, store.LoadRibbonLayout());

            File.WriteAllText(appearancePath, "{ broken");
            Assert.Equal(new WriterAppearancePreferences(), store.LoadAppearance());
            Assert.Equal(ribbonLayout, store.LoadRibbonLayout());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void UnsupportedPlatformBackdropFallsBackWithoutErasingThePreference()
    {
        var preferences = new WriterAppearancePreferences
        {
            Backdrop = RibbonBackdrop.Tabbed,
            BackstageTranslucent = true,
        };

        Assert.Equal(
            RibbonBackdrop.None,
            WriterAppearanceCompatibility.ResolveBackdrop(preferences, systemBackdropSupported: false));
        Assert.False(WriterAppearanceCompatibility.CanUseBackstageTranslucency(
            preferences,
            systemBackdropSupported: false));
        Assert.Equal(RibbonBackdrop.Tabbed, preferences.Backdrop);
    }
}
