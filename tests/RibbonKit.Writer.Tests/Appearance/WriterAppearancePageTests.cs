using System.Windows.Automation;
using System.Windows.Controls;
using RibbonKit;
using RibbonKit.Controls;
using RibbonKit.Interop;
using RibbonKit.Theming;
using RibbonKit.Writer.Appearance;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Appearance;

[Collection("Writer UI")]
public sealed class WriterAppearancePageTests
{
    [Fact]
    public void PagePublishesLivePreviewAndAppearanceOnlyDefaultsWithStableAutomationNames()
    {
        StaTestHelper.Run(() =>
        {
            var page = new WriterAppearancePage();
            var custom = new WriterAppearancePreferences
            {
                Theme = RibbonTheme.Office2007,
                DarkPalette = true,
                BackstageDesign = RibbonBackstageDesign.Glass2007,
                Backdrop = RibbonBackdrop.Acrylic,
                FrameAppearance = RibbonWindowFrameAppearance.Office2007Aero,
                ApplicationButtonShape = RibbonApplicationButtonShape.Orb,
                ShowRuler = false,
            };
            page.SetPreferences(custom);

            Assert.Equal(custom, page.Preferences);
            Assert.Equal("Appearance settings", AutomationProperties.GetName(page));
            Assert.Equal("WriterAppearancePage", AutomationProperties.GetAutomationId(page));
            var content = Assert.IsType<StackPanel>(page.Content);
            var introduction = Assert.IsType<TextBlock>(content.Children[0]);
            Assert.StartsWith("Preview Writer's theme", introduction.Text, StringComparison.Ordinal);
            Assert.DoesNotContain(content.Children.OfType<TextBlock>(), text => text.Text == "Appearance");

            WriterAppearancePreferences? preview = null;
            page.PreferencesChanged += (_, preferences) => preview = preferences;
            var defaults = Assert.IsType<Button>(page.FindName("AppearanceDefaultsButton"));
            defaults.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));

            Assert.Equal(new WriterAppearancePreferences(), preview);
            Assert.Equal(new WriterAppearancePreferences(), page.Preferences);
            Assert.Equal("Restore appearance defaults", AutomationProperties.GetName(defaults));
            Assert.Same(page.TryFindResource("OptionsDialogActionButtonStyle"), defaults.Style);
            var accent = Assert.IsType<Button>(page.FindName("AccentButton"));
            Assert.Same(page.TryFindResource("OptionsDialogActionButtonStyle"), accent.Style);
            var apply = Assert.IsType<Button>(page.FindName("AppearanceApplyButton"));
            Assert.Same(page.TryFindResource("OptionsDialogPrimaryButtonStyle"), apply.Style);
            Assert.Equal(
                "Apply appearance settings",
                AutomationProperties.GetName(apply));
        });
    }
}
