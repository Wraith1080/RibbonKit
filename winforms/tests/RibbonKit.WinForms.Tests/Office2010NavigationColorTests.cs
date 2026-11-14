using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2010NavigationColorTests
{
    [Theory]
    [InlineData(30, 90, 156)] [InlineData(180, 40, 64)] [InlineData(33, 115, 70)]
    [InlineData(128, 57, 123)] [InlineData(0, 112, 112)] [InlineData(168, 75, 0)]
    public void Navigation_stays_in_the_chosen_hue_and_lightens_without_a_lower_highlight_peak(int r, int g, int b)
    {
        var accent = Color.FromArgb(r, g, b); var theme = RibbonThemes.Office2010Blue with { Accent = accent };
        var fill = theme.BackstageSelectedFill; float previous = -1;
        foreach (int index in Enumerable.Range(0, 101))
        {
            var color = RibbonFillColors.At(fill, index / 100d);
            Assert.InRange(HueDistance(accent, color), 0, 4);
            Assert.True(color.GetBrightness() >= previous - 1f / 255, $"Lower highlight reverses at {index}% for {accent}");
            previous = color.GetBrightness();
        }
        if (r == 180) Assert.All(new[] { fill.Start, fill.End, fill.IntermediateStops![0].Color }, c => Assert.True(c.R > c.B * 1.4 && c.R > c.G * 1.4));
    }

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Actual_selected_and_pressed_paint_has_no_bright_band_below_the_label(int dpi) => FoundationTests.Sta(() =>
    {
        foreach (var accent in new[] { Color.FromArgb(180, 40, 64), Color.FromArgb(33, 115, 70), Color.FromArgb(128, 57, 123), Color.FromArgb(30, 90, 156) })
        foreach (bool office in new[] { false, true })
        foreach (bool pressed in new[] { false, true })
        {
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            var theme = RibbonThemes.Office2010Blue with { Accent = accent };
            using var item = new RibbonButtonItem { Text = "Selected" }; using var image = new Bitmap(dpi * 3, dpi / 2); using var graphics = Graphics.FromImage(image);
            renderer.DrawBackstageEntry(new(graphics, new(Point.Empty, image.Size), theme, Control.DefaultFont, dpi, Pressed: pressed), item, !pressed);
            float previous = -1;
            for (int y = 2; y < image.Height - 2; y++)
            {
                var color = image.GetPixel(image.Width * 4 / 5, y);
                Assert.InRange(HueDistance(accent, color), 0, 4);
                Assert.True(color.GetBrightness() >= previous - 1f / 255, $"Painted highlight falls at y={y}"); previous = color.GetBrightness();
            }
        }
    });

    [Theory] [InlineData(0)] [InlineData(128)] [InlineData(255)]
    public void Neutral_accents_stay_neutral_including_navigation_borders(int channel)
    {
        var recipe = RibbonThemes.Office2010Blue.IdentityAccent!; var accent = Color.FromArgb(channel, channel, channel);
        foreach (var color in new[] { recipe.AdaptNavigationFill(recipe.NavigationSelectedBackground, accent).Start,
            recipe.AdaptNavigationFill(recipe.NavigationSelectedBackground, accent).End, recipe.AdaptNavigationFill(new(recipe.NavigationBorderColor), accent).Start })
        { Assert.Equal(color.R, color.G); Assert.Equal(color.G, color.B); }
    }

    [Fact]
    public void Navigation_strength_and_explicit_fills_retain_independent_ownership_alpha_and_direction()
    {
        var theme = RibbonThemes.Office2010Blue; var recipe = theme.IdentityAccent!; var accent = Color.FromArgb(180, 40, 64);
        var reference = recipe.NavigationSelectedBackground with { Direction = RibbonGradientDirection.Horizontal, End = Color.FromArgb(91, Color.LightBlue) };
        Assert.Equal(reference, recipe.AdaptNavigationFill(reference, recipe.ReferenceAccent));
        Assert.Equal(reference, (recipe with { NavigationStrength = 0 }).AdaptNavigationFill(reference, accent));
        var full = recipe.AdaptNavigationFill(reference, accent); var half = (recipe with { NavigationStrength = .5 }).AdaptNavigationFill(reference, accent);
        Assert.Equal(reference.Direction, full.Direction); Assert.Equal(91, full.End.A); Assert.Equal(reference.IntermediateStops![0].Offset, full.IntermediateStops![0].Offset);
        Assert.InRange(half.Start.R, Math.Min(reference.Start.R, full.Start.R), Math.Max(reference.Start.R, full.Start.R)); Assert.NotEqual(reference, full); Assert.NotEqual(full, half);
        var authored = new RibbonFill(Color.Lime, Color.Olive);
        Assert.Equal(authored, (theme with { Accent = accent, BackstageNavigationSelected = authored }).BackstageSelectedFill);
        Assert.Equal(recipe.FileBackground, (recipe with { NavigationStrength = 0 }).FileBackground);
        Assert.Equal(recipe.AdaptFill(recipe.FileOpenBackground, accent), (recipe with { NavigationStrength = 0 }).AdaptFill(recipe.FileOpenBackground, accent));
    }

    private static double HueDistance(Color left, Color right) { double distance = Math.Abs(left.GetHue() - right.GetHue()); return Math.Min(distance, 360 - distance); }
}
