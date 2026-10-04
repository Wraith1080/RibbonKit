using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class InRibbonGallerySelectionTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2024, false, 1.25)]
    [InlineData(RibbonTheme.Office2024, true, 2)]
    [InlineData(RibbonTheme.CrystalLight, false, 2)]
    [InlineData(RibbonTheme.CrystalLight, true, 1.25)]
    public void Selected_row_survives_visiting_other_tabs_and_galleries(
        RibbonTheme theme, bool dark, double scale) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        ThemeManager.Apply(application, theme);
        ThemeManager.SetDarkMode(application, dark);
        var oldMotion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var gallery = CreateGallery(158, 112);
        var otherGallery = CreateGallery(258, 64);
        otherGallery.SelectedIndex = 0;
        var ribbon = new Ribbon();
        var view = AddTab("View", gallery);
        var home = AddTab("Home", otherGallery);
        var lab = AddTab("Lab", new RibbonButton { Header = "Other command" });
        var samples = AddTab("Samples", new RibbonButton { Header = "Sample" });
        ribbon.SelectedTab = view;
        var window = new Window { Content = ribbon, Width = 1000, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
            Layout();
            gallery.IsDropDownOpen = true;
            Layout();
            gallery.SelectedIndex = 4;
            var selected = Assert.IsType<RibbonGalleryItem>(gallery.SelectedItem);
            gallery.IsDropDownOpen = false;
            Layout();
            AssertVisible();
            int selectionChanges = 0;
            gallery.SelectionChanged += (_, _) => selectionChanges++;
            foreach (var tab in new[] { samples, view, lab, samples, view, home, lab, view })
            {
                ribbon.SelectedTab = tab;
                Layout();
                if (ReferenceEquals(tab, view)) AssertVisible();
            }
            Assert.Equal(0, selectionChanges);

            // Replacing a lookless template creates a new strip without unloading the gallery.
            var template = gallery.Template;
            gallery.Template = null;
            Layout();
            gallery.Template = template;
            Layout();
            AssertVisible();

            // Browsing away from the selected row must still work while the gallery stays loaded.
            var strip = Assert.IsType<ScrollViewer>(gallery.Template.FindName("PART_ScrollViewer", gallery));
            strip.ScrollToVerticalOffset(0);
            Layout();
            window.Width -= 20;
            Layout();
            Assert.Equal(0, strip.VerticalOffset);
            Assert.Same(selected, gallery.SelectedItem);
            Assert.Equal(0, selectionChanges);

            void AssertVisible()
            {
                Assert.Same(selected, gallery.SelectedItem);
                var viewer = Assert.IsType<ScrollViewer>(gallery.Template.FindName("PART_ScrollViewer", gallery));
                var chrome = Assert.IsType<Border>(selected.Template.FindName("Chrome", selected));
                var bounds = chrome.TransformToVisual(viewer).TransformBounds(new Rect(chrome.RenderSize));
                Assert.True(bounds.Top >= -0.01 && bounds.Bottom <= viewer.ActualHeight + 0.01,
                    $"{theme}, dark={dark}, scale={scale}: selected border {bounds}, viewport {viewer.RenderSize}, offset {viewer.VerticalOffset}.");
            }
        }
        finally
        {
            window.Close();
            RibbonAnimation.GlobalLevel = oldMotion;
            Sta.ResetApplication();
        }
        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
        RibbonTab AddTab(string header, FrameworkElement content)
        {
            var tab = new RibbonTab { Header = header };
            var group = new RibbonGroup { Header = header, CanResize = false };
            group.Items.Add(content);
            tab.Groups.Add(group);
            ribbon.Tabs.Add(tab);
            return tab;
        }
    });

    private static InRibbonGallery CreateGallery(double width, double tileWidth)
    {
        var gallery = new InRibbonGallery { Width = width, VerticalAlignment = VerticalAlignment.Center };
        for (int i = 0; i < 8; i++) gallery.Items.Add(new RibbonGalleryItem
        {
            Content = new TextBlock { Text = $"Item {i}", Width = tileWidth, Height = 40,
                VerticalAlignment = VerticalAlignment.Center },
        });
        return gallery;
    }
}
