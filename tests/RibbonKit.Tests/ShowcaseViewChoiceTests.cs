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
using RibbonKit.Interop;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class ShowcaseViewChoiceTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2024, false, RibbonBackdrop.Mica)]
    [InlineData(RibbonTheme.Office2024, true, RibbonBackdrop.Mica)]
    [InlineData(RibbonTheme.Office2024, false, RibbonBackdrop.Acrylic)]
    [InlineData(RibbonTheme.Office2024, true, RibbonBackdrop.Acrylic)]
    [InlineData(RibbonTheme.Office2024Flat, false, RibbonBackdrop.Mica)]
    [InlineData(RibbonTheme.Office2024Flat, true, RibbonBackdrop.Mica)]
    [InlineData(RibbonTheme.Office2024Flat, false, RibbonBackdrop.Acrylic)]
    [InlineData(RibbonTheme.Office2024Flat, true, RibbonBackdrop.Acrylic)]
    public void Colored_2024_caption_keeps_the_backdrop_visible_behind_the_ribbon(
        RibbonTheme theme, bool dark, RibbonBackdrop backdrop) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var window = new MainWindow();
        try
        {
            var type = typeof(MainWindow);
            type.GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
            window.ThemeGallery.SelectedItem = window.ThemeGallery.Items.Cast<RibbonGalleryItem>()
                .Single(item => Equals(item.Tag, theme.ToString()));
            window.DarkModeToggle.IsChecked = dark;
            var backdropKey = (DependencyPropertyKey)typeof(RibbonWindow).GetField("ActiveBackdropPropertyKey",
                BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            // Exercise the host's post-activation path without requesting native DWM state.
            window.SetValue(backdropKey, backdrop);
            ThemeManager.SetTitleBarBackdrop(application, true);
            type.GetMethod("UpdateBackdropSurfaceTransparency", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
            window.MainRibbon.ApplyTemplate();
            foreach (bool glass in new[] { false, true })
            {
                window.GlassTreatmentToggle.IsChecked = glass;
                type.GetMethod("RefreshGlassTreatment", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
                foreach (bool colored in new[] { false, true, false })
                {
                    window.AccentedTitleBarToggle.IsChecked = colored;
                    Assert.Equal(0, Alpha(window.MainRibbon.Background));
                    Assert.Equal(0, Alpha((Brush)window.FindResource("RibbonKit.Brushes.Ribbon.TabStripBackground")));
                    Assert.Equal(0, Alpha(window.Background));
                    Assert.Equal(0, Alpha(window.MainContentArea.Background));
                    if (colored)
                    {
                        var caption = Assert.IsType<SolidColorBrush>(window.FindResource("RibbonKit.Brushes.TitleBar.Background"));
                        var accent = Assert.IsType<SolidColorBrush>(window.FindResource("RibbonKit.Brushes.Accent"));
                        Assert.Equal(accent.Color, caption.Color);
                    }
                }
            }
        }
        finally { window.Close(); Sta.ResetApplication(); }
        static double Alpha(Brush brush) => Assert.IsType<SolidColorBrush>(brush).Color.A * brush.Opacity;
    });

    [Theory]
    [InlineData(RibbonQuickAccessIconAppearance.WhiteOverlay, "White Overlay")]
    [InlineData(RibbonQuickAccessIconAppearance.ColorWithGlow, "Color with Glow")]
    [InlineData(RibbonQuickAccessIconAppearance.AdaptiveColor, "Adaptive Color")]
    public void Qat_icon_choice_survives_theme_and_colored_title_bar_changes(
        RibbonQuickAccessIconAppearance appearance, string label) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var window = new MainWindow();
        try
        {
            typeof(MainWindow).GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(window, true);
            window.QatIconAppearanceSelector.Items.Cast<RibbonMenuItem>()
                .Single(item => Equals(item.Tag, appearance))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            foreach (var theme in Enum.GetValues<RibbonTheme>())
            {
                window.ThemeGallery.SelectedItem = window.ThemeGallery.Items.Cast<RibbonGalleryItem>()
                    .Single(item => Equals(item.Tag, theme.ToString()));
                window.AccentedTitleBarToggle.IsChecked = true;
                window.AccentedTitleBarToggle.IsChecked = false;
                Assert.Equal(appearance, window.MainRibbon.QuickAccessIconAppearance);
                Assert.Equal(label, window.QatIconAppearanceSelector.Header);
            }
        }
        finally { window.Close(); Sta.ResetApplication(); }
    });

    [Theory]
    [InlineData(RibbonTheme.CrystalLight, RibbonBackstageDesign.CrystalSidebar, "Crystal Sidebar")]
    [InlineData(RibbonTheme.Office2024, RibbonBackstageDesign.Modern, "2024 Rail")]
    [InlineData(RibbonTheme.Office2024Flat, RibbonBackstageDesign.Modern, "2024 Rail")]
    [InlineData(RibbonTheme.Office2019, RibbonBackstageDesign.Classic, "2013 Rail")]
    [InlineData(RibbonTheme.Office2013, RibbonBackstageDesign.Classic, "2013 Rail")]
    [InlineData(RibbonTheme.Office2010, RibbonBackstageDesign.Classic2010, "2010 Glass")]
    [InlineData(RibbonTheme.Office2007, RibbonBackstageDesign.Classic2007, "2007 Classic")]
    public void Selecting_a_theme_applies_its_preferred_backstage_layout(
        RibbonTheme theme, RibbonBackstageDesign design, string label) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var window = new MainWindow();
        var demo = new LocalizationRtlDemo();
        try
        {
            typeof(MainWindow).GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(window, true);
            demo.AttachApplicationSurfaceSource(window);
            window.ApplicationMenuToggle.IsChecked = true;
            if (theme == RibbonTheme.Office2024) SelectTheme(RibbonTheme.Office2019);
            SelectLayout(RibbonBackstageDesign.CrystalFloating);
            SelectTheme(theme);
            Assert.Equal(design, window.ShowcaseBackstage.Design);
            Assert.Equal(label, window.BackstageLayoutSelector.Header);
            Assert.Equal(design, demo.DemoBackstage.Design);
            Assert.True(window.ApplicationMenuToggle.IsChecked);
            Assert.NotNull(window.MainRibbon.ApplicationMenu);

            // A manual choice still takes effect and survives a dark-mode change.
            SelectLayout(RibbonBackstageDesign.CrystalFloating);
            window.DarkModeToggle.IsChecked = true;
            Assert.Equal(RibbonBackstageDesign.CrystalFloating, window.ShowcaseBackstage.Design);
            Assert.Equal("Crystal Floating", window.BackstageLayoutSelector.Header);
            Assert.Equal(RibbonBackstageDesign.CrystalFloating, demo.DemoBackstage.Design);

            void SelectTheme(RibbonTheme value) => window.ThemeGallery.SelectedItem = window.ThemeGallery.Items
                .Cast<RibbonGalleryItem>().Single(item => Equals(item.Tag, value.ToString()));
            void SelectLayout(RibbonBackstageDesign value) => window.BackstageLayoutSelector.Items
                .Cast<RibbonMenuItem>().Single(item => Equals(item.Tag, value.ToString()))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        finally { demo.Close(); window.Close(); Sta.ResetApplication(); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Application_menu_toggle_survives_theme_and_dark_mode_changes(bool useMenu) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var window = new MainWindow();
        var demo = new LocalizationRtlDemo();
        try
        {
            // Exercise the real selector handlers without writing user preferences.
            typeof(MainWindow).GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(window, true);
            window.ApplicationMenuToggle.IsChecked = useMenu;
            demo.AttachApplicationSurfaceSource(window);
            var menu = (RibbonApplicationMenu)window.FindName("ShowcaseApplicationMenu");
            foreach (var theme in Enum.GetValues<RibbonTheme>())
            {
                window.ThemeGallery.SelectedItem = window.ThemeGallery.Items.Cast<RibbonGalleryItem>()
                    .Single(item => Equals(item.Tag, theme.ToString()));
                foreach (bool dark in new[] { false, true })
                {
                    window.DarkModeToggle.IsChecked = dark;
                    Assert.Equal(useMenu, window.ApplicationMenuToggle.IsChecked);
                    Assert.Same(useMenu ? menu : null, window.MainRibbon.ApplicationMenu);
                    Assert.Equal(useMenu, window.ApplicationSurfaceState.UsesApplicationMenu);
                    Assert.Equal(useMenu, demo.DemoRibbon.ApplicationMenu is not null);
                }
            }
            // The existing toggle still changes both windows explicitly.
            window.ApplicationMenuToggle.IsChecked = !useMenu;
            Assert.Equal(!useMenu, window.MainRibbon.ApplicationMenu is not null);
            Assert.Equal(!useMenu, demo.DemoRibbon.ApplicationMenu is not null);
        }
        finally { demo.Close(); window.Close(); Sta.ResetApplication(); }
    });

    [Theory]
    [InlineData(RibbonTheme.Office2007, RibbonBackdrop.Acrylic)]
    [InlineData(RibbonTheme.Office2010, RibbonBackdrop.Acrylic)]
    [InlineData(RibbonTheme.Office2007, RibbonBackdrop.Mica)]
    [InlineData(RibbonTheme.Office2010, RibbonBackdrop.Mica)]
    public void Legacy_aero_themes_suspend_glass_without_losing_the_modern_choice(RibbonTheme legacy, RibbonBackdrop backdrop) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var window = new MainWindow();
        try
        {
            typeof(MainWindow).GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(window, true);
            // Inject derived runtime state to exercise the real selector handlers
            // without depending on native DWM availability in this regression.
            var backdropKey = (DependencyPropertyKey)typeof(RibbonWindow).GetField("ActiveBackdropPropertyKey",
                BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            window.SetValue(backdropKey, backdrop);
            window.GlassTreatmentToggle.IsChecked = true;
            Assert.Equal(0.44, ((Brush)window.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground")).Opacity, 5);

            void Select(RibbonTheme theme) => window.ThemeGallery.SelectedItem = window.ThemeGallery.Items
                .Cast<RibbonGalleryItem>().Single(item => item.Tag!.ToString() == theme.ToString());

            Select(legacy);
            Assert.False(window.GlassTreatmentToggle.IsEnabled);
            Assert.False(window.GlassTreatmentToggle.IsChecked);
            Assert.True(window.AeroFrameGroup.IsEnabled);
            Assert.Equal(1, ((Brush)window.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground")).Opacity);
            Assert.Equal(true, typeof(MainWindow).GetField("_glassTreatmentOverride",
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window));
            Select(RibbonTheme.Office2013);
            Assert.True(window.GlassTreatmentToggle.IsEnabled);
            Assert.True(window.GlassTreatmentToggle.IsChecked);
            Assert.Equal(0.44, ((Brush)window.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground")).Opacity, 5);

            window.GlassTreatmentToggle.IsChecked = false;
            Select(legacy);
            Select(RibbonTheme.Office2019);
            Assert.True(window.GlassTreatmentToggle.IsEnabled);
            Assert.False(window.GlassTreatmentToggle.IsChecked);
            Assert.Equal(1, ((Brush)window.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground")).Opacity);
        }
        finally { window.Close(); Sta.ResetApplication(); }
    });

    [Theory]
    [InlineData(RibbonTheme.Office2013, false)]
    [InlineData(RibbonTheme.Office2013, true)]
    [InlineData(RibbonTheme.Office2019, false)]
    [InlineData(RibbonTheme.Office2019, true)]
    [InlineData(RibbonTheme.Office2024, false)]
    [InlineData(RibbonTheme.Office2024, true)]
    [InlineData(RibbonTheme.CrystalLight, false)]
    public void Glass_treatment_tracks_mica_acrylic_and_disabled_backdrops(RibbonTheme theme, bool dark) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var window = new MainWindow();
        try
        {
            var type = typeof(MainWindow);
            type.GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
            window.ThemeGallery.SelectedItem = window.ThemeGallery.Items.Cast<RibbonGalleryItem>()
                .Single(item => item.Tag!.ToString() == theme.ToString());
            window.DarkModeToggle.IsChecked = dark;
            // Crystal's default choice and the Office opt-in use the same host refresh path.
            if (theme == RibbonTheme.CrystalLight) Assert.True(window.GlassTreatmentToggle.IsChecked);
            else window.GlassTreatmentToggle.IsChecked = true;
            int baselineDictionaries = window.Resources.MergedDictionaries.Count;
            var backdropKey = (DependencyPropertyKey)typeof(RibbonWindow).GetField("ActiveBackdropPropertyKey",
                BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var refresh = type.GetMethod("RefreshCrystalTint", BindingFlags.Instance | BindingFlags.NonPublic)!;

            foreach (var backdrop in new[] { RibbonBackdrop.Mica, RibbonBackdrop.Acrylic, RibbonBackdrop.Mica })
            {
                // Native activation remains a host/target-machine check. Exercise its real
                // post-activation refresh with truthful derived state, without invoking DWM.
                window.SetValue(backdropKey, backdrop);
                refresh.Invoke(window, null);
                Assert.Equal(baselineDictionaries + 1, window.Resources.MergedDictionaries.Count);
                Assert.Equal(theme == RibbonTheme.Office2019 && dark ? 0.64 : 0.44,
                    ((Brush)window.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground")).Opacity, 5);
                Assert.Equal(dark ? (byte)0x50 : (byte)0x98,
                    Assert.IsType<SolidColorBrush>(window.FindResource("RibbonKit.Brushes.Control.HoverBackground")).Color.A);

                window.GlassTreatmentToggle.IsChecked = false;
                Assert.Equal(baselineDictionaries, window.Resources.MergedDictionaries.Count);
                Assert.Equal(1, ((Brush)window.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground")).Opacity);
                window.GlassTreatmentToggle.IsChecked = true;
            }
            window.SetValue(backdropKey, RibbonBackdrop.None);
            refresh.Invoke(window, null);
            Assert.True(window.GlassTreatmentToggle.IsChecked);
            Assert.Equal(baselineDictionaries, window.Resources.MergedDictionaries.Count);
            Assert.Equal(1, ((Brush)window.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground")).Opacity);
        }
        finally { window.Close(); Sta.ResetApplication(); }
    });

    [Fact]
    public void Theme_gallery_qat_preserves_authored_visuals_and_the_real_selection_handler() => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var main = new MainWindow();
        typeof(MainWindow).GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(main, true);
        Assert.True(main.MainRibbon.AddToQuickAccess(main.ThemeGallery));
        var proxy = main.MainRibbon.QuickAccessItems.OfType<RibbonDropDownButton>().Single(item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), main.ThemeGallery));
        var host = new Window { Content = proxy, Width = 600, Height = 400, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            host.Show(); Sta.Drain(); host.UpdateLayout(); Sta.Drain();
            foreach (var tile in main.ThemeGallery.Items.Cast<RibbonGalleryItem>().ToArray())
            {
                proxy.IsDropDownOpen = true; Sta.Drain(); host.UpdateLayout(); Sta.Drain();
                Assert.True(tile.ActualWidth > 0); Assert.Same(tile, main.ThemeGallery.ItemContainerGenerator.ContainerFromItem(tile));
                tile.IsSelected = true; Sta.Drain(); host.UpdateLayout(); Sta.Drain();
                Assert.Equal(Enum.Parse<RibbonTheme>(tile.Tag!.ToString()!), ThemeManager.CurrentTheme);
                Assert.Same(tile, main.ThemeGallery.SelectedItem);
                proxy.IsDropDownOpen = false; Sta.Drain(); Assert.Equal(7, main.ThemeGallery.Items.Count);
            }
        }
        finally { host.Close(); main.Close(); RibbonAnimation.GlobalLevel = motion; Sta.ResetApplication(); }
    });

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
                Assert.Equal(7, window.ThemeGallery.Items.Count);
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
                var combos = RibbonCommandCatalog.CollectAvailable(window.MainRibbon)
                    .Select(entry => entry.Control).OfType<RibbonComboBox>().ToArray();
                Assert.Equal(2, combos.Length);
                Assert.All(combos, combo => Assert.NotNull(combo.Icon));
                Assert.Contains(combos, combo => Ribbon.GetCommandId(combo) == "command.home.font.family");
                Assert.Contains(combos, combo => Ribbon.GetCommandId(combo) == "command.home.font.size");
                var galleries = RibbonCommandCatalog.CollectAvailable(window.MainRibbon).Select(entry => entry.Control).OfType<RibbonGallery>().ToArray();
                Assert.Equal(3, galleries.Length); Assert.All(galleries, gallery => Assert.NotNull(gallery.Icon));

                // Exercise the real selection handlers without writing the user's
                // persisted Showcase appearance from this headless test.
                typeof(MainWindow).GetField("_restoringAppearance",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
                window.TouchModeChoice.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(RibbonDensity.Touch, window.MainRibbon.Density);
                window.MouseModeChoice.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(RibbonDensity.Compact, window.MainRibbon.Density);
                void SelectTheme(RibbonTheme theme) => window.ThemeGallery.SelectedItem =
                    window.ThemeGallery.Items.Cast<RibbonGalleryItem>().Single(item => Equals(item.Tag, theme.ToString()));
                SelectTheme(RibbonTheme.Office2019);
                Assert.Equal(RibbonTheme.Office2019, ThemeManager.CurrentTheme);
                var floating = (RibbonMenuItem)window.BackstageLayoutSelector.Items[6];
                floating.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(RibbonBackstageDesign.CrystalFloating, window.ShowcaseBackstage.Design);
                Assert.Equal("Crystal Floating", window.BackstageLayoutSelector.Header);
                SelectTheme(RibbonTheme.Office2024);
                Assert.Equal(RibbonTheme.Office2024, ThemeManager.CurrentTheme);
                SelectTheme(RibbonTheme.CrystalLight);
                Assert.Equal(RibbonTheme.CrystalLight, ThemeManager.CurrentTheme);
                Assert.True(window.GlassTreatmentToggle.IsChecked);
                SelectTheme(RibbonTheme.Office2024);
                Assert.False(window.GlassTreatmentToggle.IsChecked);
                window.GlassTreatmentToggle.IsChecked = true;
                SelectTheme(RibbonTheme.Office2019);
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
