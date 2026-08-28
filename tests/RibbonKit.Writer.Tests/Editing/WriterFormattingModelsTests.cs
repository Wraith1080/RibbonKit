using System.Globalization;
using System.Linq;
using System.Windows.Media;
using RibbonKit.Writer.Editing;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

public sealed class WriterFormattingModelsTests
{
    [Fact]
    public void FontCatalogCachesAndSortsInjectedFamiliesWithoutCaseDuplicates()
    {
        var sourceCalls = 0;
        var catalog = new WriterFontCatalog(
            () =>
            {
                sourceCalls++;
                return new[]
                {
                    new FontFamily("Zoo"),
                    new FontFamily("alpha"),
                    new FontFamily("Alpha"),
                    new FontFamily("Beta")
                };
            });

        var first = catalog.InstalledFonts;
        var second = catalog.InstalledFonts;

        Assert.Same(first, second);
        Assert.Equal(1, sourceCalls);
        Assert.Equal(new[] { "Alpha", "Beta", "Zoo" },
            first.Select(choice => choice.DisplayName).ToArray());
    }

    [Fact]
    public void FontCatalogSearchIsCaseInsensitiveAndFontChoiceKeepsOwnDisplayFace()
    {
        var custom = new WriterFontChoice(new FontFamily("Display Face"), "My Display Face");
        var catalog = new WriterFontCatalog(
            () => new[] { custom.FontFamily, new FontFamily("Segoe UI"), new FontFamily("Arial") });

        var result = catalog.Search("display FACE");

        var choice = Assert.Single(result);
        Assert.Equal("My Display Face", custom.DisplayName);
        Assert.Equal("Display Face", choice.DisplayName);
        Assert.Equal("Display Face", choice.FontFamily.Source);
    }

    [Fact]
    public void FontCatalogFallsBackWhenEnumerationFailsAndRefreshesCache()
    {
        var catalog = new WriterFontCatalog(
            () => throw new InvalidOperationException("font provider unavailable"));

        var fallback = catalog.InstalledFonts;

        Assert.Equal(
            WriterFontCatalog.DefaultFallbackFamilyNames,
            fallback.Select(choice => choice.DisplayName).ToArray());
        Assert.NotEmpty(catalog.RefreshInstalledFonts());
    }

    [Fact]
    public void FontCatalogRecentAndProjectionSectionsAreBoundedAndDuplicateFree()
    {
        var catalog = new WriterFontCatalog(
            () => new[]
            {
                new FontFamily("Alpha"),
                new FontFamily("Beta"),
                new FontFamily("Gamma"),
                new FontFamily("Delta")
            },
            recentLimit: 2,
            recommendedFamilyNames: new[] { "Beta", "Alpha", "Beta" });

        catalog.RememberRecent(new FontFamily("Gamma"));
        catalog.RememberRecent(new FontFamily("Beta"));
        catalog.RememberRecent(new FontFamily("Gamma"));

        var projection = catalog.CreateProjection(new FontFamily("GAMMA"));
        var identities = projection.Items.Select(choice => choice.SourceName).ToArray();

        Assert.Equal(new[] { "Gamma", "Beta" },
            catalog.RecentFonts.Select(choice => choice.SourceName).ToArray());
        Assert.Equal("Gamma", projection.Current!.SourceName);
        Assert.Equal(new[] { "Beta", "Alpha" },
            projection.Recommended.Select(choice => choice.SourceName).ToArray());
        Assert.Equal(new[] { "Delta" },
            projection.RemainingInstalled.Select(choice => choice.SourceName).ToArray());
        Assert.Equal(identities.Length, identities.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(new[] { "Gamma", "Beta", "Alpha", "Delta" }, identities);
    }

    [Fact]
    public void FontCatalogRequiresItsExplicitDispatcher()
    {
        var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        var catalog = new WriterFontCatalog(
            () => new[] { new FontFamily("Segoe UI") },
            dispatcher: dispatcher);

        Assert.Same(dispatcher, catalog.Dispatcher);
        Assert.True(catalog.IsOnRequiredDispatcher);
        Assert.Single(catalog.InstalledFonts);
    }

    [Fact]
    public void FontSizePolicyExposesConventionalSizesAndFiniteBounds()
    {
        Assert.Equal(
            new[] { 8d, 9d, 10d, 11d, 12d, 14d, 16d, 18d, 20d, 22d, 24d, 26d, 28d, 36d, 48d, 72d },
            WriterFontSizePolicy.ConventionalPointSizes);

        Assert.True(WriterFontSizePolicy.IsValidPointSize(1));
        Assert.True(WriterFontSizePolicy.IsValidPointSize(1638));
        Assert.False(WriterFontSizePolicy.IsValidPointSize(0));
        Assert.False(WriterFontSizePolicy.IsValidPointSize(1638.01));
        Assert.False(WriterFontSizePolicy.IsValidPointSize(double.NaN));
        Assert.False(WriterFontSizePolicy.IsValidPointSize(double.PositiveInfinity));
    }

    [Fact]
    public void FontSizePolicyParsesCultureAwareEditableValuesAndRejectsInvalidText()
    {
        Assert.True(WriterFontSizePolicy.TryParsePoints(
            "12.5", CultureInfo.InvariantCulture, out var invariant));
        Assert.Equal(12.5, invariant);

        Assert.True(WriterFontSizePolicy.TryParsePoints(
            "12,5", CultureInfo.GetCultureInfo("de-DE"), out var localized));
        Assert.Equal(12.5, localized);

        Assert.False(WriterFontSizePolicy.TryParsePoints("0", out _));
        Assert.False(WriterFontSizePolicy.TryParsePoints("1639", out _));
        Assert.False(WriterFontSizePolicy.TryParsePoints("NaN", out _));
        Assert.False(WriterFontSizePolicy.TryParsePoints("not a size", out _));
    }

    [Fact]
    public void FontSizePolicyConvertsBetweenPointsAndWpfDip()
    {
        var dip = WriterFontSizePolicy.PointsToDip(18);

        Assert.Equal(24, dip, 10);
        Assert.Equal(18, WriterFontSizePolicy.DipToPoints(dip), 10);
        Assert.True(WriterFontSizePolicy.TryPointsToDip(12.5, out var customDip));
        Assert.Equal(12.5, WriterFontSizePolicy.DipToPoints(customDip), 10);
        Assert.False(WriterFontSizePolicy.TryDipToPoints(0, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => WriterFontSizePolicy.PointsToDip(0));
    }

    [Fact]
    public void FontSizePolicyGrowShrinkStepThroughConventionalValuesAndStayMonotonicAtExtremes()
    {
        Assert.Equal(11, WriterFontSizePolicy.Grow(10));
        Assert.Equal(14, WriterFontSizePolicy.Grow(13));
        Assert.Equal(12, WriterFontSizePolicy.Shrink(13));
        Assert.Equal(8, WriterFontSizePolicy.Grow(5));
        Assert.Equal(5, WriterFontSizePolicy.Shrink(5));
        Assert.Equal(100, WriterFontSizePolicy.Grow(100));
        Assert.Equal(72, WriterFontSizePolicy.Shrink(100));
        Assert.False(WriterFontSizePolicy.TryGrow(72, out var unchangedGrow));
        Assert.Equal(72, unchangedGrow);
        Assert.False(WriterFontSizePolicy.TryShrink(8, out var unchangedShrink));
        Assert.Equal(8, unchangedShrink);
    }

    [Fact]
    public void ColorEntriesAreImmutableValuesAndPaletteProvidesTargetDefaults()
    {
        var automatic = WriterColorEntry.Automatic;
        var noColor = WriterColorEntry.NoColor;

        Assert.True(automatic.IsAutomatic);
        Assert.Null(automatic.Color);
        Assert.True(noColor.IsNoColor);
        Assert.Null(noColor.Color);

        var palette = new WriterColorPalette(recentLimit: 2);
        Assert.Equal(automatic, palette.ForegroundPrimaryAction);
        Assert.Equal(noColor, palette.HighlightPrimaryAction);
        Assert.Equal(automatic, palette.GetEntries(WriterColorTarget.Foreground).First());
        Assert.Equal(noColor, palette.GetEntries(WriterColorTarget.Highlight).First());
    }

    [Fact]
    public void ColorPaletteKeepsBoundedUniqueRecentsAndLastUsedPrimaryActions()
    {
        var first = Color.FromArgb(0xFF, 0x10, 0x20, 0x30);
        var second = Color.FromArgb(0xFF, 0x40, 0x50, 0x60);
        var third = Color.FromArgb(0xFF, 0x70, 0x80, 0x90);
        var palette = new WriterColorPalette(recentLimit: 2);

        palette.RememberRecent(first);
        palette.RememberRecent(second);
        palette.RememberRecent(first);
        palette.RememberRecent(third);
        palette.SetLastUsed(WriterColorTarget.Foreground, second);
        palette.SetLastUsed(WriterColorTarget.Highlight, third);

        Assert.Equal(new[] { third, second }, palette.RecentColors);
        Assert.Equal(second, palette.ForegroundPrimaryAction.Color);
        Assert.Equal(third, palette.HighlightPrimaryAction.Color);
        Assert.Equal(second, palette.GetPrimaryColor(WriterColorTarget.Foreground));
        Assert.Equal(third, palette.GetPrimaryColor(WriterColorTarget.Highlight));

        palette.SetLastUsed(WriterColorTarget.Foreground, null);
        palette.SetLastUsed(WriterColorTarget.Highlight, null);
        Assert.Equal(WriterColorEntry.Automatic, palette.ForegroundPrimaryAction);
        Assert.Equal(WriterColorEntry.NoColor, palette.HighlightPrimaryAction);
    }

    [Fact]
    public void ColorPaletteGalleryHasNoDuplicateSolidColorsAcrossSections()
    {
        var custom = Color.FromArgb(0xFF, 0x12, 0x34, 0x56);
        var palette = new WriterColorPalette(recentLimit: 4);
        palette.RememberRecent(Colors.Black);
        palette.RememberRecent(custom);

        foreach (var target in Enum.GetValues<WriterColorTarget>())
        {
            var entries = palette.GetEntries(target);
            var solidColors = entries
                .Where(entry => entry.Color.HasValue)
                .Select(entry => entry.Color!.Value)
                .ToArray();

            Assert.Equal(solidColors.Length, solidColors.Distinct().Count());
        }
    }

    [Fact]
    public void ColorPaletteRejectsInvalidBaseEntryKindsAndRecentLimits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WriterColorPalette(0));

        var invalid = new WriterColorEntry(
            "custom", "Custom", Colors.Red, WriterColorEntryKind.Custom);
        Assert.Throws<ArgumentException>(() => new WriterColorPalette(
            new[] { invalid }, Array.Empty<WriterColorEntry>()));
    }
}
