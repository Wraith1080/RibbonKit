using System;
using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
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
                Assert.Equal("Optimize spacing between commands", window.TouchModeSelector.DropDownHeader);
                Assert.Equal(2, window.TouchModeSelector.Items.Count);

                // Exercise the real selection handlers without writing the user's
                // persisted Showcase appearance from this headless test.
                typeof(MainWindow).GetField("_restoringAppearance",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
                window.TouchModeChoice.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(RibbonDensity.Touch, window.MainRibbon.Density);
                window.MouseModeChoice.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(RibbonDensity.Compact, window.MainRibbon.Density);
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

    [Theory]
    [InlineData(RibbonTheme.Office2007)]
    [InlineData(RibbonTheme.Office2010)]
    [InlineData(RibbonTheme.Office2013)]
    [InlineData(RibbonTheme.Office2019)]
    [InlineData(RibbonTheme.Office2024)]
    [InlineData(RibbonTheme.CrystalLight)]
    public void Density_picker_closes_on_selection_and_preserves_saved_qat_identity(RibbonTheme theme) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        MainWindow? window = null;
        try
        {
            ThemeManager.Apply(application, theme);
            window = new MainWindow { Width = 1540, Height = 900, Left = -10000, Top = -10000,
                ShowActivated = false, ShowInTaskbar = false };
            var type = typeof(MainWindow);
            window.Loaded -= (RoutedEventHandler)type.GetMethod("OnWindowLoaded",
                BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate(typeof(RoutedEventHandler), window);
            type.GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
            var ribbon = window.MainRibbon;
            ribbon.Density = RibbonDensity.Touch; // Same assignment used to restore the saved flag.
            ribbon.SelectedTab = ribbon.Tabs.Single(tab => Equals(tab.Header, "View"));
            window.Show(); Layout(window);
            var selector = window.TouchModeSelector;
            Assert.False(selector.IsDropDownOpen);
            Assert.Equal("auto:tab.view/group.view.backstage/Touch mode#2", Ribbon.GetCommandId(selector));
            selector.IsDropDownOpen = true; Layout(window);
            Assert.Same(selector.FindResource("RibbonKit.Brushes.Control.CheckedBackground"), window.TouchModeChoice.Background);
            var selected = window.TouchModeChoice;
            Brush selectedPaint = selected.IsPressed
                ? (Brush)selected.FindResource("RibbonKit.Brushes.Control.PressedBackground")
                : selected.IsMouseOver ? (Brush)selected.FindResource("RibbonKit.Brushes.Control.HoverBackground")
                : selected.Background;
            Assert.Same(selectedPaint, Part<Border>(selected, "Chrome").Background);
            Assert.Same(selector.FindResource("Icon.Touch"), window.TouchModeChoice.LargeIcon);
            SavePreview(Part<Border>(selector, "PART_MenuHost"), $"{theme}-Touch");
            window.MouseModeChoice.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window);
            Assert.Equal(RibbonDensity.Compact, ribbon.Density);
            Assert.False(selector.IsDropDownOpen);

            ribbon.AddToQuickAccess(selector); Layout(window);
            var proxy = ribbon.QuickAccessItems.OfType<RibbonDropDownButton>().Single(item =>
                ReferenceEquals(Ribbon.GetQuickAccessSource(item), selector));
            proxy.IsDropDownOpen = true; Layout(window);
            Assert.Equal(selector.DropDownHeader, Part<TextBlock>(proxy, "DropDownHeaderText").Text);
            Assert.Same(proxy.FindResource("RibbonKit.Brushes.Control.CheckedBackground"), window.MouseModeChoice.Background);
            SavePreview(Part<Border>(proxy, "PART_MenuHost"), $"{theme}-Mouse-QAT");
            window.TouchModeChoice.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window);
            Assert.Equal(RibbonDensity.Touch, ribbon.Density);
            Assert.False(proxy.IsDropDownOpen);
            Assert.Equal(2, selector.Items.Count); Assert.Empty(proxy.Items);
            string saved = RibbonCustomizationSerializer.Serialize(ribbon);
            Assert.Contains("auto:tab.view/group.view.backstage/Touch mode#2", saved, StringComparison.Ordinal);
            ribbon.QuickAccessItems.Clear();
            RibbonCustomizationSerializer.Apply(ribbon, saved); Layout(window);
            Assert.Equal(RibbonDensity.Touch, ribbon.Density);
            Assert.Contains(ribbon.QuickAccessItems.OfType<RibbonDropDownButton>(), item =>
                ReferenceEquals(Ribbon.GetQuickAccessSource(item), selector));
        }
        finally
        {
            window?.Close(); RibbonAnimation.GlobalLevel = motion; Sta.ResetApplication();
        }
    });

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }

    private static void SavePreview(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_DENSITY_SELECTOR_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        // Include the popup root's margin/shadow space, rather than cropping the child visual offset.
        if (PresentationSource.FromVisual(element)?.RootVisual is FrameworkElement root && root is not Window)
            element = root;
        Directory.CreateDirectory(directory);
        DpiScale dpi = VisualTreeHelper.GetDpi(element);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY,
            PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(file);
    }
}
