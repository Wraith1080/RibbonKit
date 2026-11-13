using RibbonKit.WinForms.Samples;
using RibbonKit.WinForms.Showcase.Persistence;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class SampleAppearanceTests
{
    [Theory] [InlineData(RibbonBuiltInTheme.Office2013Light)] [InlineData(RibbonBuiltInTheme.Office2019Light)] [InlineData(RibbonBuiltInTheme.Office2024Light)]
    public void Accent_and_colored_title_reset_independently_and_recover_authored_application_roles(RibbonBuiltInTheme family) => FoundationTests.Sta(() =>
    {
        var authored = RibbonThemes.Office2024Light with { Accent = Color.DarkRed, TitleBarForeground = Color.Navy, Hover = new(Color.Pink) };
        using var form = new RibbonForm { TitleBarColor = Color.Beige }; using var ribbon = new RibbonControl { Theme = authored };
        form.Controls.Add(ribbon); form.Ribbon = ribbon; var selection = new SampleAppearance(ribbon, form);
        selection.SelectTheme(family); var familyTheme = ribbon.Theme;
        selection.SelectAccent(Color.Teal); selection.SetColorTitleBar(true);
        Assert.Equal(family, selection.SelectedTheme); Assert.Equal(Color.Teal, ribbon.Theme.Accent); Assert.Equal(Color.Teal, form.TitleBarColor);
        Assert.Equal(Color.White, ribbon.Theme.TitleBarForeground); Assert.Equal(Color.White, form.CaptionTheme(ribbon.Theme, false).Foreground);
        selection.SelectAccent(Color.Purple); Assert.Equal(Color.Purple, form.TitleBarColor);
        selection.SetColorTitleBar(false);
        Assert.Equal(Color.Beige, form.TitleBarColor); Assert.Equal(familyTheme.TitleBarForeground, ribbon.Theme.TitleBarForeground); Assert.Equal(Color.Purple, ribbon.Theme.Accent);
        selection.SelectAccent(null); Assert.Equal(family, ribbon.BuiltInTheme); Assert.Same(familyTheme, ribbon.Theme);
        selection.SelectTheme(null); Assert.Same(authored, ribbon.Theme); Assert.Equal(Color.Beige, form.TitleBarColor);
        selection.SetColorTitleBar(true); selection.SetColorTitleBar(false);
        Assert.Equal(Color.Navy, ribbon.Theme.TitleBarForeground); Assert.Equal(Color.Pink, ribbon.Theme.Hover.Start);
        Assert.False(form.IsHandleCreated);
    });

    [Fact]
    public void Appearance_preferences_roundtrip_and_old_preferences_keep_default_choices()
    {
        var preferences = new ShowcasePreferences { BuiltInTheme = RibbonBuiltInTheme.Office2013Light, AccentArgb = Color.Teal.ToArgb(), ColorTitleBar = true };
        var read = ShowcasePreferences.FromJson(preferences.ToJson());
        Assert.Equal(preferences.BuiltInTheme, read.BuiltInTheme); Assert.Equal(preferences.AccentArgb, read.AccentArgb); Assert.True(read.ColorTitleBar);
        var old = ShowcasePreferences.FromJson("{\"format\":\"RibbonKit.WinForms.Showcase.Preferences\",\"version\":1}");
        Assert.Null(old.AccentArgb); Assert.False(old.ColorTitleBar);
        Assert.ThrowsAny<Exception>(() => ShowcasePreferences.FromJson("{\"format\":\"RibbonKit.WinForms.Showcase.Preferences\",\"version\":1,\"accentArgb\":0}"));
    }
}
