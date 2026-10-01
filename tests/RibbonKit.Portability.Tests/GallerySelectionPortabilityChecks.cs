using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

internal static class GallerySelectionPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var selection = new Selection { Index = 4 };
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new Binding());
        text.SetValue(FrameworkElement.WidthProperty, 112d);
        text.SetValue(FrameworkElement.HeightProperty, 40d);
        var gallery = new InRibbonGallery { Width = 158,
            ItemsSource = Enumerable.Range(0, 8).Select(i => $"Theme {i}").ToArray(),
            ItemTemplate = new DataTemplate { VisualTree = text },
            VerticalAlignment = VerticalAlignment.Center };
        gallery.SetBinding(Selector.SelectedIndexProperty,
            new Binding(nameof(Selection.Index)) { Source = selection, Mode = BindingMode.TwoWay });
        var otherGallery = new InRibbonGallery { Width = 258, SelectedIndex = 0 };
        for (int i = 0; i < 8; i++) otherGallery.Items.Add(new RibbonGalleryItem
        { Content = new TextBlock { Text = $"Style {i}", Width = 64, Height = 40 } });
        var ribbon = new Ribbon();
        var view = AddTab("View", gallery);
        var home = AddTab("Home", otherGallery);
        ribbon.SelectedTab = view;
        var window = new Window { Content = ribbon, Width = 1000, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (FlowDirection flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            foreach (double scale in new[] { 1.25, 2 })
            {
                ribbon.SelectedTab = home;
                Layout();
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                window.FlowDirection = flow;
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                ribbon.SelectedTab = view;
                Layout();
                AssertVisible();
                gallery.IsDropDownOpen = true;
                Layout();
                var popup = Assert.IsType<ScrollViewer>(gallery.Template.FindName("PART_PopupScrollViewer", gallery));
                Assert.Equal(0, popup.VerticalOffset);
                Assert.True(popup.ViewportHeight > gallery.ActualHeight);
                gallery.IsDropDownOpen = false;
                Layout();
                AssertVisible();
                Assert.True(BindingOperations.IsDataBound(gallery, Selector.SelectedIndexProperty));
                Assert.Equal(4, selection.Index);

                void AssertVisible()
                {
                    Assert.Equal(4, gallery.SelectedIndex);
                    var tile = Assert.IsType<RibbonGalleryItem>(gallery.ItemContainerGenerator.ContainerFromIndex(4));
                    var viewer = Assert.IsType<ScrollViewer>(gallery.Template.FindName("PART_ScrollViewer", gallery));
                    var chrome = Assert.IsType<Border>(tile.Template.FindName("Chrome", tile));
                    var bounds = chrome.TransformToVisual(viewer).TransformBounds(new Rect(chrome.RenderSize));
                    Assert.True(bounds.Top >= -0.01 && bounds.Bottom <= viewer.ActualHeight + 0.01,
                        $"{theme}, dark={dark}, {flow}, scale={scale}: selected border {bounds}, viewport {viewer.RenderSize}.");
                }
            }
        }
        finally
        {
            gallery.IsDropDownOpen = false;
            window.Close();
            RibbonAnimation.GlobalLevel = motion;
            ThemeManager.SetDarkMode(application, false);
        }
        void Layout() { Drain(); window.UpdateLayout(); Drain(); }
        void Drain() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        RibbonTab AddTab(string header, FrameworkElement content)
        {
            var tab = new RibbonTab { Header = header };
            var group = new RibbonGroup { Header = header, CanResize = false };
            group.Items.Add(content);
            tab.Groups.Add(group);
            ribbon.Tabs.Add(tab);
            return tab;
        }
    }

    private sealed class Selection { public int Index { get; set; } }
}
