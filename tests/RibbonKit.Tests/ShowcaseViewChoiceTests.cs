using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class ShowcaseViewChoiceTests
{
    [Fact]
    public void Main_window_constructs_with_theme_gallery_and_backstage_dropdown() => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            var window = new MainWindow();
            try
            {
                Assert.Equal(6, window.ThemeGallery.Items.Count);
                Assert.Equal("Office2024",
                    ((FrameworkElement)window.ThemeGallery.SelectedItem).Tag);
                Assert.Equal(7, window.BackstageLayoutSelector.Items.Count);
                Assert.Equal("2024 Rail", window.BackstageLayoutSelector.Header);
                Assert.False(window.GlassTreatmentToggle.IsChecked);
                Assert.Equal(RibbonControlSize.Large, window.GlassTreatmentToggle.Size);
                Assert.Equal("Glass look", window.GlassTreatmentToggle.Header);
                Assert.NotNull(window.GlassTreatmentToggle.LargeIcon);

                // Exercise the real selection handlers without writing the user's
                // persisted Showcase appearance from this headless test.
                typeof(MainWindow).GetField("_restoringAppearance",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
                window.ThemeGallery.SelectedItem = window.ThemeGallery.Items[2];
                Assert.Equal(RibbonTheme.Office2019, ThemeManager.CurrentTheme);
                var floating = (RibbonMenuItem)window.BackstageLayoutSelector.Items[6];
                floating.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(RibbonBackstageDesign.CrystalFloating, window.ShowcaseBackstage.Design);
                Assert.Equal("Crystal Floating", window.BackstageLayoutSelector.Header);
                window.ThemeGallery.SelectedItem = window.ThemeGallery.Items[1];
                Assert.Equal(RibbonTheme.Office2024, ThemeManager.CurrentTheme);
                window.ThemeGallery.SelectedItem = window.ThemeGallery.Items[0];
                Assert.Equal(RibbonTheme.CrystalLight, ThemeManager.CurrentTheme);
                Assert.True(window.GlassTreatmentToggle.IsChecked);
                window.ThemeGallery.SelectedItem = window.ThemeGallery.Items[1];
                Assert.False(window.GlassTreatmentToggle.IsChecked);
                window.GlassTreatmentToggle.IsChecked = true;
                window.ThemeGallery.SelectedItem = window.ThemeGallery.Items[2];
                Assert.True(window.GlassTreatmentToggle.IsChecked);
            }
            finally { window.Close(); }
        }
        finally { Sta.ResetApplication(); }
    });
}
