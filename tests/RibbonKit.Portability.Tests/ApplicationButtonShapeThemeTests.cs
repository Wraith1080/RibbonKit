using System.ComponentModel;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

/// <summary>A library-only consumer: this project has no Showcase reference or resources.</summary>
public sealed class ApplicationButtonShapeThemeTests
{
    private static void VerifyNoApplicationHeaderInset(Application application)
    {
        var backstage = new Backstage();
        var ribbon = new Ribbon { Backstage = backstage,
            QuickAccessPosition = RibbonQuickAccessPosition.TabRow };
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save" });
        var home = new RibbonTab { Header = "Home" };
        home.Groups.Add(new RibbonGroup { Header = "Clipboard" });
        ribbon.Tabs.Add(home);
        ribbon.SelectedTab = home;
        var window = new RibbonWindow
        {
            Content = ribbon,
            Width = 700,
            Height = 350,
            Left = -10000,
            Top = -10000,
            ShowActivated = false,
            ShowInTaskbar = false,
        };
        try
        {
            window.Show();
            foreach ((RibbonTheme theme, double inset) in new[]
            {
                (RibbonTheme.Office2007, 4d),
                (RibbonTheme.Office2010, 0d),
                (RibbonTheme.Office2013, 0d),
                (RibbonTheme.Office2019, 0d),
                (RibbonTheme.Office2024, 0d),
                (RibbonTheme.CrystalLight, 22d),
            })
            {
                foreach (bool dark in new[] { false, true })
                {
                    ThemeManager.Apply(application, theme);
                    ThemeManager.SetDarkMode(application, dark);
                    ribbon.Backstage = backstage;
                    ribbon.FlowDirection = FlowDirection.LeftToRight;
                    ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
                    Drain();
                    window.UpdateLayout();

                    var tabs = Assert.IsType<RibbonTabControl>(
                        ribbon.Template.FindName("TabControlHost", ribbon));
                    var panel = Assert.IsType<TabPanel>(
                        tabs.Template.FindName("PART_TabItemsPanel", tabs));
                    var qat = Assert.IsType<RibbonQuickAccessToolBar>(
                        tabs.Template.FindName("QatTabRowHost", tabs));
                    var file = Assert.IsType<ToggleButton>(
                        tabs.Template.FindName("PART_ApplicationButton", tabs));
                    var body = Assert.IsType<Border>(
                        tabs.Template.FindName("ContentHost", tabs));
                    var normalTabMargin = Assert.IsType<Thickness>(
                        ribbon.FindResource("RibbonKit.Metrics.TabStripMargin"));
                    var normalQatMargin = new Thickness(2, 4, 4, 0);
                    Assert.Equal(Visibility.Visible, file.Visibility);
                    Assert.Equal(normalTabMargin, panel.Margin);
                    Assert.Equal(normalQatMargin, qat.Margin);

                    ribbon.Backstage = null;
                    Drain();
                    window.UpdateLayout();
                    Assert.Equal(Visibility.Collapsed, file.Visibility);
                    Assert.Equal(normalTabMargin, panel.Margin);
                    Assert.Equal(LeadingInset(normalQatMargin, inset, false), qat.Margin);
                    if (inset > 0)
                        AssertPastCorner(qat, body, tabs, false);

                    ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
                    Drain();
                    window.UpdateLayout();
                    Assert.Equal(Visibility.Collapsed, qat.Visibility);
                    Assert.Equal(LeadingInset(normalTabMargin, inset, false), panel.Margin);
                    if (inset > 0)
                        AssertPastCorner(home, body, tabs, false);

                    ribbon.FlowDirection = FlowDirection.RightToLeft;
                    Drain();
                    window.UpdateLayout();
                    Assert.Equal(LeadingInset(normalTabMargin, inset, true), panel.Margin);
                    if (inset > 0)
                        AssertPastCorner(home, body, tabs, true);

                    ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
                    Drain();
                    window.UpdateLayout();
                    Assert.Equal(normalTabMargin, panel.Margin);
                    Assert.Equal(LeadingInset(normalQatMargin, inset, true), qat.Margin);
                    if (inset > 0)
                        AssertPastCorner(qat, body, tabs, true);
                }
            }

            ThemeManager.Apply(application, RibbonTheme.Office2024);
            ThemeManager.SetDarkMode(application, false);
            ribbon.Backstage = null;
            ribbon.FlowDirection = FlowDirection.LeftToRight;
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
            Drain();
            var liveTabs = Assert.IsType<RibbonTabControl>(
                ribbon.Template.FindName("TabControlHost", ribbon));
            var liveQat = Assert.IsType<RibbonQuickAccessToolBar>(
                liveTabs.Template.FindName("QatTabRowHost", liveTabs));
            Assert.Equal(new Thickness(2, 4, 4, 0), liveQat.Margin);
            var office2007Tokens = Tokens("Office2007");
            ribbon.Resources.MergedDictionaries.Add(office2007Tokens);
            Drain();
            Assert.Equal(new Thickness(6, 4, 4, 0), liveQat.Margin);
            ribbon.Resources.MergedDictionaries.Remove(office2007Tokens);
            Drain();
            Assert.Equal(new Thickness(2, 4, 4, 0), liveQat.Margin);
        }
        finally
        {
            window.Close();
            ThemeManager.SetDarkMode(application, false);
        }
    }

    [Fact]
    public void Theme_default_and_local_value_follow_wpf_precedence() => RunSta(() =>
    {
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        if (Environment.GetEnvironmentVariable("RIBBONKIT_PORTABILITY_SCOPE") == "MinimizedDivider")
        {
            TouchDensityPortabilityChecks.VerifyMinimizedDivider(application);
            return;
        }
        if (Environment.GetEnvironmentVariable("RIBBONKIT_PORTABILITY_SCOPE") == "TouchChrome")
        {
            TouchDensityPortabilityChecks.VerifyChrome(application);
            return;
        }
        if (Environment.GetEnvironmentVariable("RIBBONKIT_PORTABILITY_SCOPE") == "GroupLauncher")
        {
            TouchDensityPortabilityChecks.VerifyGroupLauncher(application);
            return;
        }
        if (Environment.GetEnvironmentVariable("RIBBONKIT_PORTABILITY_SCOPE") == "DensityTransition")
        {
            DensityTransitionPortabilityChecks.Verify(application);
            return;
        }
        TouchDensityPortabilityChecks.Verify(application);
        // WPF allows only one Application per process. The explicit local touch scope
        // reuses this entry point while leaving the default full consumer run intact.
        if (Environment.GetEnvironmentVariable("RIBBONKIT_PORTABILITY_SCOPE") == "Touch")
            return;
        DensityTransitionPortabilityChecks.Verify(application);
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        KeyboardFocusPortabilityChecks.Verify(application);
        KeyboardNavigationPortabilityChecks.Verify(application);
        CustomizationScrollSpacingChecks.Verify(application);
        CrystalUtilityPortabilityChecks.Verify(application);
        CrystalTintPortabilityChecks.Verify(application);
        PopupMarginPortabilityChecks.Verify(application);
        ApplicationMenuViewportChecks.Verify(application);
        CrystalApplicationMenuPortabilityChecks.Verify(application);
        CrystalQuickAccessPortabilityChecks.Verify(application);
        CrystalMessagePortabilityChecks.Verify(application);
        Assert.Equal(new CornerRadius(4), application.Resources["RibbonKit.Metrics.MenuItemCornerRadius"]);
        Assert.Equal(new CornerRadius(4), application.Resources["RibbonKit.Metrics.InputCornerRadius"]);
        var ribbon = new Ribbon
        {
            Backstage = new Backstage { Design = RibbonBackstageDesign.Classic2007 },
        };
        var menuItem = new RibbonMenuItem { Header = "Open" };
        var textInput = new RibbonTextBox { Header = "Find", Text = "Keep text" };
        var comboInput = new RibbonComboBox { Header = "Font", ItemsSource = new[] { "Aptos", "Georgia" }, SelectedIndex = 1 };
        var check = new RibbonCheckBox { Header = "Guides", IsChecked = true };
        var radio = new RibbonRadioButton { Header = "Comfortable", IsChecked = true };
        var gallery = new InRibbonGallery { Width = 282, SelectedIndex = 0 };
        var tile = new RibbonGalleryItem { Content = "Style" };
        gallery.Items.Add(tile);
        var tip = new RibbonScreenTip { Title = "Find", Description = "Search the document", PlacementTarget = textInput };
        textInput.ToolTip = tip;
        var group = new RibbonGroup { Header = "File actions" };
        group.Items.Add(menuItem);
        var home = new RibbonTab { Header = "Home" };
        home.Groups.Add(group);
        ribbon.Tabs.Add(home);
        ribbon.SelectedTab = home;
        var window = new RibbonWindow
        {
            Content = ribbon,
            Width = 600,
            Height = 300,
            Left = -10000,
            Top = -10000,
            ShowActivated = false,
            ShowInTaskbar = false,
        };
        var inputHost = new StackPanel();
        inputHost.Children.Add(textInput);
        inputHost.Children.Add(comboInput);
        inputHost.Children.Add(check);
        inputHost.Children.Add(radio);
        inputHost.Children.Add(gallery);
        var inputWindow = new Window
        {
            Content = inputHost,
            Width = 500,
            Height = 350,
            Left = -10000,
            Top = -10000,
            ShowActivated = false,
            ShowInTaskbar = false,
        };
        try
        {
            window.Show();
            inputWindow.Show();
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Tab);
            AssertMenuRadius(menuItem, 4);
            AssertInputMaterials(textInput, comboInput, crystal: false, radius: 4);
            AssertOptionAndGallery(check, radio, gallery, tile, crystal: false, radius: 4);

            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            {
                ThemeManager.Apply(application, theme);
                Drain();
                AssertShape(ribbon, theme == RibbonTheme.Office2007
                    ? RibbonApplicationButtonShape.Orb
                    : RibbonApplicationButtonShape.Tab);
                double radius = theme switch
                {
                    RibbonTheme.Office2013 or RibbonTheme.Office2019 => 0,
                    RibbonTheme.CrystalLight or RibbonTheme.Office2024 => 4,
                    _ => 3,
                };
                AssertMenuRadius(menuItem, radius);
                AssertInputMaterials(textInput, comboInput, theme == RibbonTheme.CrystalLight, radius);
                AssertOptionAndGallery(check, radio, gallery, tile, theme == RibbonTheme.CrystalLight, radius);
            }

            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            Drain();
            AssertDetachedTip(tip, crystal: true);
            AssertGalleryPopup(gallery, crystal: true);

            gallery.IsDropDownOpen = true;
            Drain();
            var galleryPopupHost = Assert.IsType<Border>(
                gallery.Template.FindName("PART_PopupHost", gallery));
            ThemeManager.Apply(application, RibbonTheme.Office2019);
            Drain();
            Assert.Same(gallery.FindResource("RibbonKit.Brushes.InRibbonGallery.PopupBackground"),
                galleryPopupHost.Background);
            gallery.IsDropDownOpen = false;
            Drain();
            var scopedGalleryBackground = new SolidColorBrush(Color.FromRgb(0x23, 0x45, 0x67));
            inputWindow.Resources["RibbonKit.Brushes.Ribbon.ContentBackground"] = scopedGalleryBackground;
            gallery.IsDropDownOpen = true;
            Drain();
            Assert.Same(scopedGalleryBackground, galleryPopupHost.Background);
            gallery.IsDropDownOpen = false;
            inputWindow.Resources.Remove("RibbonKit.Brushes.Ribbon.ContentBackground");
            Drain();

            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            ThemeManager.SetDarkMode(application, true);
            Drain();
            AssertMenuRadius(menuItem, 4);
            AssertInputMaterials(textInput, comboInput, crystal: true, radius: 4);
            AssertOptionAndGallery(check, radio, gallery, tile, crystal: true, radius: 4);
            AssertDetachedTip(tip, crystal: true);
            AssertGalleryPopup(gallery, crystal: true);
            ThemeManager.Apply(application, RibbonTheme.Office2019);
            Drain();
            AssertMenuRadius(menuItem, 0);
            AssertInputMaterials(textInput, comboInput, crystal: false, radius: 0);
            AssertOptionAndGallery(check, radio, gallery, tile, crystal: false, radius: 0);
            AssertDetachedTip(tip, crystal: false);
            AssertGalleryPopup(gallery, crystal: false);

            ThemeManager.SetDarkMode(application, false);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            var manualCrystal = Tokens("Crystal.Light");
            application.Resources.MergedDictionaries.Add(manualCrystal);
            Drain();
            AssertInputMaterials(textInput, comboInput, crystal: true, radius: 4);
            AssertOptionAndGallery(check, radio, gallery, tile, crystal: true, radius: 4);
            AssertDetachedTip(tip, crystal: true);
            AssertGalleryPopup(gallery, crystal: true);
            var manualDark = Tokens("Crystal.Dark");
            application.Resources.MergedDictionaries.Add(manualDark);
            Drain();
            AssertInputMaterials(textInput, comboInput, crystal: true, radius: 4);
            AssertOptionAndGallery(check, radio, gallery, tile, crystal: true, radius: 4);
            AssertDetachedTip(tip, crystal: true);
            application.Resources.MergedDictionaries.Remove(manualDark);
            application.Resources.MergedDictionaries.Remove(manualCrystal);
            Drain();
            AssertInputMaterials(textInput, comboInput, crystal: false, radius: 4);
            AssertOptionAndGallery(check, radio, gallery, tile, crystal: false, radius: 4);
            AssertDetachedTip(tip, crystal: false);

            inputHost.FlowDirection = FlowDirection.RightToLeft;
            check.Header = "إرشادات";
            Drain();
            Assert.Equal(FlowDirection.RightToLeft, check.FlowDirection);
            Assert.Equal("إرشادات", check.Header);
            AssertOptionAndGallery(check, radio, gallery, tile, crystal: false, radius: 4);
            check.Header = "Guides";
            inputHost.FlowDirection = FlowDirection.LeftToRight;

            ThemeManager.Apply(application, RibbonTheme.Office2007);
            ThemeManager.SetDarkMode(application, true);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Orb);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Tab);

            ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Tab;
            ThemeManager.Apply(application, RibbonTheme.Office2007);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Tab);
            ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Orb;
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Orb);
            ribbon.ClearValue(Ribbon.ApplicationButtonShapeProperty);
            Drain();
            Assert.Same(DependencyProperty.UnsetValue,
                ribbon.ReadLocalValue(Ribbon.ApplicationButtonShapeProperty));
            AssertShape(ribbon, RibbonApplicationButtonShape.Tab);
            ThemeManager.Apply(application, RibbonTheme.Office2007);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Orb);

            ThemeManager.Apply(application, RibbonTheme.Office2024);
            Drain();
            var tokens = Tokens("Office2007");
            ribbon.Resources.MergedDictionaries.Add(tokens);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Orb);
            var black = Tokens("Office2007.Dark");
            ribbon.Resources.MergedDictionaries.Add(black);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Orb);
            ribbon.Resources.MergedDictionaries.Remove(black);
            ribbon.Resources.MergedDictionaries.Remove(tokens);
            tokens = Tokens("Office2019");
            ribbon.Resources.MergedDictionaries.Add(tokens);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Tab);
            ribbon.Resources.MergedDictionaries.Remove(tokens);

            ThemeManager.Apply(application, RibbonTheme.Office2007);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Orb);
            ribbon.IsBackstageOpen = true;
            Drain();
            var field = typeof(Ribbon).GetField("_classicBackstageOrbProxy",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var proxy = Assert.IsType<Button>(field.GetValue(ribbon));
            Assert.NotNull(VisualTreeHelper.GetParent(proxy));
            ribbon.IsBackstageOpen = false;
            Drain();
            ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Tab;
            ribbon.IsBackstageOpen = true;
            Drain();
            Assert.Null(VisualTreeHelper.GetParent(proxy));

            VerifyNoApplicationHeaderInset(application);

            ribbon.IsBackstageOpen = false;
            ribbon.ClearValue(Ribbon.ApplicationButtonShapeProperty);
            ThemeManager.Apply(application, RibbonTheme.Office2007);
            Drain();
            VerifyApplicationOrbGlyphTemplate(ribbon, window);
            VerifyCustomizationPages(application);
            CapturedBackdropPortabilityChecks.Verify(application);
            GallerySelectionPortabilityChecks.Verify(application);
            QuickAccessScopePortabilityChecks.Verify(application);
        }
        finally
        {
            tip.IsOpen = false;
            gallery.IsDropDownOpen = false;
            inputWindow.Close();
            window.Close();
            ThemeManager.SetDarkMode(application, false);
            application.Shutdown();
        }
    });

    private static ResourceDictionary Tokens(string name) => new()
    {
        Source = new Uri($"/RibbonKit;component/Themes/Tokens.{name}.xaml", UriKind.Relative),
    };

    private static void VerifyCustomizationPages(Application application)
    {
        var source = new Ribbon();
        var tab = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Commands" };
        for (int i = 0; i < 30; i++)
            group.Items.Add(new RibbonButton { Header = $"Action {i + 1}" });
        tab.Groups.Add(group);
        source.Tabs.Add(tab);
        source.SelectedTab = tab;
        source.QuickAccessItems.Add(new RibbonButton { Header = "Save" });

        var ribbonPage = new RibbonOptionsPage
        {
            Header = "Customize Ribbon",
            Content = new RibbonCustomizePage { Ribbon = source },
        };
        var quickAccessPage = new RibbonOptionsPage
        {
            Header = "Quick Access Toolbar",
            Content = new RibbonQuickAccessPage { Ribbon = source },
        };
        var dialog = new RibbonOptionsDialog
        {
            Title = "Options",
            Width = 760,
            Height = 430,
            Left = -10000,
            Top = -10000,
            ShowActivated = false,
            ShowInTaskbar = false,
        };
        dialog.Pages.Add(ribbonPage);
        dialog.Pages.Add(quickAccessPage);
        dialog.SelectedPage = ribbonPage;
        try
        {
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            dialog.Show();
            Drain();
            dialog.UpdateLayout();
            AssertCustomizationTheme(dialog, ribbonPage, quickAccessPage, crystal: true);
            dialog.SelectedPage = ribbonPage;
            Drain();
            var customize = Assert.IsType<RibbonCustomizePage>(ribbonPage.Content);
            var available = Assert.IsType<ListBox>(customize.Template.FindName("PART_AvailableList", customize));
            var scroll = Assert.IsType<ScrollViewer>(available.Template.FindName("PART_ScrollViewer", available));
            Assert.True(scroll.ScrollableHeight > 0);
            scroll.ScrollToBottom();
            Drain();
            Assert.True(scroll.VerticalOffset > 0);
            scroll.ScrollToTop();
            VerifyCustomizationFocus(dialog, ribbonPage);
            ThemeManager.SetAccent(application, Color.FromRgb(138, 62, 129));
            Drain();
            Assert.IsType<DrawingBrush>(((Button)dialog.Template.FindName("PART_OkButton", dialog)).Background);
            var lightNavigation = dialog.FindResource(
                "RibbonKit.Brushes.OptionsDialog.NavigationSelectedBackground");

            ThemeManager.SetDarkMode(application, true);
            Drain();
            AssertCustomizationTheme(dialog, ribbonPage, quickAccessPage, crystal: true);
            Assert.NotSame(lightNavigation, dialog.FindResource(
                "RibbonKit.Brushes.OptionsDialog.NavigationSelectedBackground"));
            VerifyCustomizationFocus(dialog, ribbonPage);
            ThemeManager.ClearAccent(application);

            dialog.FlowDirection = FlowDirection.RightToLeft;
            ribbonPage.Header = "تخصيص الشريط";
            Drain();
            Assert.Equal(FlowDirection.RightToLeft, ribbonPage.FlowDirection);
            Assert.Equal("تخصيص الشريط", ribbonPage.Header);

            foreach (RibbonTheme theme in new[]
            {
                RibbonTheme.Office2007, RibbonTheme.Office2010,
                RibbonTheme.Office2013, RibbonTheme.Office2019, RibbonTheme.Office2024,
            })
            {
                foreach (bool dark in new[] { false, true })
                {
                    ThemeManager.Apply(application, theme);
                    ThemeManager.SetDarkMode(application, dark);
                    Drain();
                    AssertCustomizationTheme(dialog, ribbonPage, quickAccessPage, crystal: false);
                }
            }

            ThemeManager.SetDarkMode(application, false);
            var manualCrystal = Tokens("Crystal.Light");
            dialog.Resources.MergedDictionaries.Add(manualCrystal);
            Drain();
            AssertCustomizationTheme(dialog, ribbonPage, quickAccessPage, crystal: true);
            var manualDark = Tokens("Crystal.Dark");
            dialog.Resources.MergedDictionaries.Add(manualDark);
            Drain();
            AssertCustomizationTheme(dialog, ribbonPage, quickAccessPage, crystal: true);
            Assert.Equal(Color.FromRgb(234, 244, 252),
                ((SolidColorBrush)dialog.FindResource("RibbonKit.Brushes.OptionsDialog.PrimaryForeground")).Color);
            dialog.Resources.MergedDictionaries.Remove(manualDark);
            dialog.Resources.MergedDictionaries.Remove(manualCrystal);
            Drain();
            AssertCustomizationTheme(dialog, ribbonPage, quickAccessPage, crystal: false);
        }
        finally
        {
            dialog.Close();
            ThemeManager.ClearAccent(application);
            ThemeManager.SetDarkMode(application, false);
        }
    }

    private static void AssertCustomizationTheme(RibbonOptionsDialog dialog,
        RibbonOptionsPage ribbonPage, RibbonOptionsPage quickAccessPage, bool crystal)
    {
        dialog.SelectedPage = ribbonPage;
        Drain();
        dialog.UpdateLayout();
        var nav = Assert.IsType<ListBox>(dialog.Template.FindName("PART_PageList", dialog));
        Assert.Equal(2, nav.Items.Count);
        var selectedMarker = Assert.IsType<System.Windows.Shapes.Rectangle>(
            ribbonPage.Template.FindName("BottomMarker", ribbonPage));
        Assert.Equal(crystal ? 1d : 0d, selectedMarker.Opacity);
        Assert.Equal(Visibility.Visible, selectedMarker.Visibility);
        var action = Assert.IsType<Button>(dialog.Template.FindName("PART_OkButton", dialog));
        var actionChrome = Assert.IsType<Border>(action.Template.FindName("Chrome", action));
        Assert.Equal(crystal ? new CornerRadius(12) : Assert.IsType<CornerRadius>(dialog.FindResource(
            "RibbonKit.Metrics.ControlCornerRadius")), actionChrome.CornerRadius);
        Assert.Equal(crystal ? FontWeights.SemiBold : FontWeights.Normal, action.FontWeight);
        var cancel = Assert.IsType<Button>(dialog.Template.FindName("PART_CancelButton", dialog));
        Assert.Equal(crystal ? new CornerRadius(12) : Assert.IsType<CornerRadius>(dialog.FindResource(
            "RibbonKit.Metrics.ScrollBar.ButtonCornerRadius")),
            ((Border)cancel.Template.FindName("Chrome", cancel)).CornerRadius);

        var customize = Assert.IsType<RibbonCustomizePage>(ribbonPage.Content);
        var available = Assert.IsType<ListBox>(customize.Template.FindName("PART_AvailableList", customize));
        var tree = Assert.IsType<TreeView>(customize.Template.FindName("PART_Tree", customize));
        var root = Assert.IsType<TreeViewItem>(tree.ItemContainerGenerator.ContainerFromIndex(0));
        root.IsExpanded = true;
        Drain();
        Assert.NotNull(root.Template.FindName("PART_Header", root));

        var originalDirection = dialog.FlowDirection;
        foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
        {
            dialog.FlowDirection = direction;
            dialog.SelectedPage = ribbonPage;
            Drain();
            dialog.UpdateLayout();
            AssertFrame(available, crystal);
            AssertFrame(tree, crystal);

            // Local padding and a scoped metric must still contribute independently.
            available.Padding = new Thickness(4);
            available.Resources["RibbonKit.Metrics.Customize.FrameInset"] = new Thickness(5);
            dialog.UpdateLayout();
            AssertFrameBounds(available, 10);
            available.Resources.Remove("RibbonKit.Metrics.Customize.FrameInset");
            available.ClearValue(Control.PaddingProperty);
            dialog.UpdateLayout();
            AssertFrame(available, crystal);

            // A host style's padding must also retain its WPF precedence.
            foreach (var control in new Control[] { available, tree })
            {
                control.Style = new Style(control.GetType())
                {
                    Setters = { new Setter(Control.PaddingProperty, new Thickness(4)) },
                };
                dialog.UpdateLayout();
                AssertFrameBounds(control, (crystal ? 3 : 2) + 4);
                control.ClearValue(FrameworkElement.StyleProperty);
                dialog.UpdateLayout();
                AssertFrame(control, crystal);
            }

            dialog.SelectedPage = quickAccessPage;
            Drain();
            dialog.UpdateLayout();
            var qat = Assert.IsType<RibbonQuickAccessPage>(quickAccessPage.Content);
            AssertFrame(Assert.IsType<ListBox>(qat.Template.FindName("PART_AvailableList", qat)), crystal);
            AssertFrame(Assert.IsType<ListBox>(qat.Template.FindName("PART_CurrentList", qat)), crystal);
        }
        dialog.FlowDirection = originalDirection;
        Drain();
    }

    private static void VerifyCustomizationFocus(RibbonOptionsDialog dialog, RibbonOptionsPage page)
    {
        dialog.SelectedPage = page;
        Drain();
        dialog.UpdateLayout();
        dialog.Activate();
        var navigation = (ListBox)dialog.Template.FindName("PART_PageList", dialog);
        var navigationItem = (ListBoxItem)navigation.ItemContainerGenerator.ContainerFromItem(page);
        Assert.True(navigationItem.Focus());
        Drain();
        Assert.Same(dialog.FindResource("RibbonKit.Brushes.Accent"),
            ((Border)page.Template.FindName("Chrome", page)).BorderBrush);

        var customize = (RibbonCustomizePage)page.Content;
        var tree = (TreeView)customize.Template.FindName("PART_Tree", customize);
        var root = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(0);
        root.IsSelected = true;
        Assert.True(root.Focus());
        Drain();
        Assert.Same(dialog.FindResource("RibbonKit.Brushes.Accent"),
            ((Border)root.Template.FindName("Row", root)).BorderBrush);

        var available = (ListBox)customize.Template.FindName("PART_AvailableList", customize);
        available.SelectedIndex = 0;
        var item = (ListBoxItem)available.ItemContainerGenerator.ContainerFromIndex(0);
        Assert.True(item.Focus());
        Drain();
        var row = (Border)item.Template.FindName("Row", item);
        Assert.Same(dialog.FindResource("RibbonKit.Brushes.Accent"), row.BorderBrush);
        Assert.Same(dialog.FindResource("RibbonKit.Brushes.Control.CheckedBackground"), row.Background);
        Assert.Same(dialog.FindResource("RibbonKit.Brushes.Customize.SelectedBorder"),
            ((Border)root.Template.FindName("Row", root)).BorderBrush);

        var primary = (Button)dialog.Template.FindName("PART_OkButton", dialog);
        Assert.True(primary.Focus());
        Drain();
        Assert.Same(dialog.FindResource("RibbonKit.Brushes.OptionsDialog.PrimaryFocusBorder"),
            ((Border)primary.Template.FindName("Chrome", primary)).BorderBrush);
        Assert.Same(dialog.FindResource("RibbonKit.Brushes.Customize.SelectedBorder"), row.BorderBrush);
    }

    private static void AssertFrame(Control control, bool crystal)
    {
        control.ApplyTemplate();
        var frame = Assert.IsType<Border>(control.Template.FindName("Frame", control));
        Assert.Equal(new CornerRadius(crystal ? 8 : 0), frame.CornerRadius);
        Assert.Equal(new Thickness(0), control.Padding);
        // Measure realized native viewport and bar bounds rather than token values:
        // this catches the former extra TreeView padding and flush Crystal ListBox.
        AssertFrameBounds(control, crystal ? 3 : 2);
        if (crystal)
        {
            var viewer = Assert.IsType<ScrollViewer>(control.Template.FindName("PART_ScrollViewer", control));
            viewer.ApplyTemplate();
            var bar = Assert.IsType<ScrollBar>(viewer.Template.FindName("PART_VerticalScrollBar", viewer));
            Assert.Equal(14d, bar.Width);
            Assert.Equal(new CornerRadius(4), RibbonScrollBar.GetThumbCornerRadius(bar));
            bar.ApplyTemplate();
            Assert.Equal("RibbonKit.Controls.RibbonScrollBarTrack", Assert.IsAssignableFrom<Track>(bar.Template.FindName("PART_Track", bar)).GetType().FullName);
        }
    }

    private static void AssertFrameBounds(Control control, double inset)
    {
        var viewer = Assert.IsType<ScrollViewer>(control.Template.FindName("PART_ScrollViewer", control));
        var dpi = VisualTreeHelper.GetDpi(viewer);
        // Frame border, host padding and the inset border round independently.
        // Use a four-DIP host padding fixture to avoid native Padding's own odd
        // pixel split; default frame/bar gaps are checked without any tolerance.
        double horizontalInset = (Math.Round(dpi.DpiScaleX) + Math.Round(control.Padding.Left * dpi.DpiScaleX)
            + Math.Round((inset - 1 - control.Padding.Left) * dpi.DpiScaleX)) / dpi.DpiScaleX;
        double verticalInset = (Math.Round(dpi.DpiScaleY) + Math.Round(control.Padding.Top * dpi.DpiScaleY)
            + Math.Round((inset - 1 - control.Padding.Top) * dpi.DpiScaleY)) / dpi.DpiScaleY;
        var bounds = viewer.TransformToAncestor(control).TransformBounds(new Rect(viewer.RenderSize));
        Assert.Equal(horizontalInset, bounds.Left, 3);
        Assert.Equal(verticalInset, bounds.Top, 3);
        Assert.Equal(horizontalInset, control.ActualWidth - bounds.Right, 3);
        Assert.Equal(verticalInset, control.ActualHeight - bounds.Bottom, 3);

        viewer.ApplyTemplate();
        var bar = Assert.IsType<ScrollBar>(viewer.Template.FindName("PART_VerticalScrollBar", viewer));
        if (bar.Visibility == Visibility.Visible)
        {
            var barBounds = bar.TransformToAncestor(control).TransformBounds(new Rect(bar.RenderSize));
            Assert.Equal(verticalInset, barBounds.Top, 3);
            Assert.Equal(verticalInset, control.ActualHeight - barBounds.Bottom, 3);
            // These ancestor coordinates can remain logical in an RTL tree;
            // native ScrollViewer owns the mirroring boundary and side placement.
            double edge = Math.Min(barBounds.Left, control.ActualWidth - barBounds.Right);
            Assert.Equal(horizontalInset, edge, 3);
        }
    }

    private static void AssertMenuRadius(RibbonMenuItem item, double radius)
    {
        item.ApplyTemplate();
        var chrome = Assert.IsType<Border>(item.Template.FindName("Chrome", item));
        Assert.Equal(new CornerRadius(radius), chrome.CornerRadius);
    }

    private static void AssertInputMaterials(RibbonTextBox text, RibbonComboBox combo, bool crystal, double radius)
    {
        Assert.Equal(new CornerRadius(radius), Assert.IsType<CornerRadius>(
            text.FindResource("RibbonKit.Metrics.InputCornerRadius")));
        text.ApplyTemplate();
        combo.ApplyTemplate();
        var textChrome = Assert.IsType<Border>(text.Template.FindName("Chrome", text));
        var comboChrome = Assert.IsType<Border>(combo.Template.FindName("Chrome", combo));
        Assert.Same(text.FindResource("RibbonKit.Brushes.Input.SurfaceBackground"), textChrome.Background);
        Assert.Same(combo.FindResource("RibbonKit.Brushes.Input.SurfaceBackground"), comboChrome.Background);
        Assert.Same(text.FindResource("RibbonKit.Brushes.Input.Border"), textChrome.BorderBrush);
        Assert.Same(combo.FindResource("RibbonKit.Brushes.Input.Border"), comboChrome.BorderBrush);
        Assert.Equal(textChrome.CornerRadius, comboChrome.CornerRadius);
        Assert.Equal(new CornerRadius(radius), textChrome.CornerRadius);
        Assert.Equal(crystal, textChrome.Background is DrawingBrush);
        Assert.Equal(crystal, comboChrome.Background is DrawingBrush);
        Assert.Equal("Keep text", text.Text);
        Assert.Equal("Georgia", combo.SelectedItem);
    }

    private static void AssertOptionAndGallery(RibbonCheckBox check, RibbonRadioButton radio,
        InRibbonGallery gallery, RibbonGalleryItem tile, bool crystal, double radius)
    {
        check.ApplyTemplate();
        radio.ApplyTemplate();
        gallery.ApplyTemplate();
        tile.ApplyTemplate();
        var checkIndicator = Assert.IsType<Border>(check.Template.FindName("Indicator", check));
        var radioIndicator = Assert.IsType<Ellipse>(radio.Template.FindName("Indicator", radio));
        var tileChrome = Assert.IsType<Border>(tile.Template.FindName("Chrome", tile));
        Assert.Equal(new CornerRadius(crystal ? 3 : radius), checkIndicator.CornerRadius);
        Assert.Equal(new CornerRadius(crystal ? 5 : radius), tileChrome.CornerRadius);
        Assert.Equal(crystal, checkIndicator.Background is DrawingBrush);
        Assert.Equal(crystal, radioIndicator.Fill is DrawingBrush);
        Assert.Equal(crystal, tile.FindResource("RibbonKit.Brushes.GalleryItem.HoverBorder") is DrawingBrush);
        Assert.Equal(crystal, gallery.FindResource("RibbonKit.Brushes.InRibbonGallery.SurfaceBackground") is DrawingBrush);
        var focus = Assert.IsType<Border>(check.Template.FindName("FocusRing", check));
        Assert.Equal(!crystal, Assert.IsType<SolidColorBrush>(focus.BorderBrush).Color == Colors.Transparent);
        Assert.True(check.IsChecked);
        Assert.True(radio.IsChecked);
        Assert.Same(tile, gallery.SelectedItem);
    }

    private static void AssertGalleryPopup(InRibbonGallery gallery, bool crystal)
    {
        gallery.IsDropDownOpen = true;
        Drain();
        var popup = Assert.IsType<Popup>(gallery.Template.FindName("PART_Popup", gallery));
        var host = Assert.IsType<Border>(gallery.Template.FindName("PART_PopupHost", gallery));
        Assert.True(popup.IsOpen);
        Assert.Equal(crystal, host.Background is DrawingBrush);
        gallery.IsDropDownOpen = false;
        Drain();
    }

    private static void AssertDetachedTip(RibbonScreenTip tip, bool crystal)
    {
        tip.IsOpen = true;
        Drain();
        var border = Assert.IsType<Border>(VisualTreeHelper.GetChild(tip, 0));
        Assert.Equal(crystal, border.Background is DrawingBrush);
        Assert.Equal(Assert.IsType<CornerRadius>(tip.FindResource(
            "RibbonKit.Metrics.ScreenTip.OuterCornerRadius")), border.CornerRadius);
        if (crystal) Assert.Equal(new CornerRadius(8), border.CornerRadius);
        Assert.NotNull(tip.Template.FindName("InnerReflection", tip));
        tip.IsOpen = false;
        Drain();
    }

    private static void VerifyApplicationOrbGlyphTemplate(Ribbon ribbon, RibbonWindow window)
    {
        Assert.Null(ribbon.ApplicationOrbGlyphTemplate);
        Assert.Equal(typeof(DataTemplate), TypeDescriptor.GetProperties(typeof(Ribbon))[
            nameof(Ribbon.ApplicationOrbGlyphTemplate)]?.PropertyType);
        var tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
        var button = Assert.IsType<ToggleButton>(tabs.Template.FindName("PART_ApplicationButton", tabs));
        var realGlyph = NamedDescendant<Viewbox>(button, "OrbGlyph");
        var realSphere = NamedDescendant<Ellipse>(button, "OrbFill");
        Assert.Equal(Visibility.Visible, NamedDescendant<Grid>(button, "DefaultOrbGlyph").Visibility);
        Assert.Equal(Visibility.Collapsed,
            NamedDescendant<ContentPresenter>(button, "CustomOrbGlyph").Visibility);

        var first = GlyphTemplate(Brushes.Gold);
        ribbon.ApplicationOrbGlyphTemplate = first;
        Drain();
        window.UpdateLayout();
        var realCustom = NamedDescendant<ContentPresenter>(button, "CustomOrbGlyph");
        Assert.Same(first, realCustom.ContentTemplate);
        Assert.Equal(Visibility.Collapsed, NamedDescendant<Grid>(button, "DefaultOrbGlyph").Visibility);
        realCustom.ApplyTemplate();
        Drain();
        window.UpdateLayout();
        Assert.True(VisualTreeHelper.GetChildrenCount(realCustom) > 0,
            "The custom orb glyph template did not produce a visual.");
        var realMark = Descendant<Path>(realCustom);
        Assert.Same(Brushes.Gold, realMark.Fill);
        Assert.Same(realSphere, NamedDescendant<Ellipse>(button, "OrbFill"));

        ribbon.IsBackstageOpen = true;
        Drain();
        window.UpdateLayout();
        var field = typeof(Ribbon).GetField("_classicBackstageOrbProxy",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var proxy = Assert.IsType<Button>(field.GetValue(ribbon));
        Assert.NotNull(VisualTreeHelper.GetParent(proxy));
        Assert.Equal(string.Empty, proxy.Content);
        Assert.Same(
            Assert.IsType<ContentPresenter>(button.Template.FindName("Orb", button)).ContentTemplate,
            proxy.ContentTemplate);
        var proxyGlyph = NamedDescendant<Viewbox>(proxy, "OrbGlyph");
        var proxySphere = NamedDescendant<Ellipse>(proxy, "OrbFill");
        var proxyCustom = NamedDescendant<ContentPresenter>(proxy, "CustomOrbGlyph");
        var proxyMark = Descendant<Path>(proxyCustom);
        Assert.Same(Brushes.Gold, proxyMark.Fill);
        Assert.NotSame(realMark, proxyMark);
        Assert.Contains(Assert.IsType<TransformGroup>(proxyGlyph.RenderTransform).Children,
            transform => transform is RotateTransform);
        Assert.True(proxySphere.RenderTransform.Value.IsIdentity);
        Assert.Same(VisualTreeHelper.GetParent(proxyGlyph), VisualTreeHelper.GetParent(proxySphere));
        Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(proxy)));
        Assert.Equal(proxy.ToolTip, AutomationProperties.GetName(proxy));

        var second = GlyphTemplate(Brushes.DeepSkyBlue);
        ribbon.ApplicationOrbGlyphTemplate = second;
        Drain();
        window.UpdateLayout();
        Assert.Same(second,
            NamedDescendant<ContentPresenter>(button, "CustomOrbGlyph").ContentTemplate);
        Assert.Same(second,
            NamedDescendant<ContentPresenter>(proxy, "CustomOrbGlyph").ContentTemplate);
        Assert.NotSame(Descendant<Path>(realCustom), Descendant<Path>(proxyCustom));
        Assert.Same(Brushes.DeepSkyBlue, Descendant<Path>(realCustom).Fill);
        Assert.Same(Brushes.DeepSkyBlue, Descendant<Path>(proxyCustom).Fill);
        Assert.Same(realGlyph, NamedDescendant<Viewbox>(button, "OrbGlyph"));
        Assert.Same(proxySphere, NamedDescendant<Ellipse>(proxy, "OrbFill"));

        ribbon.ApplicationOrbGlyphTemplate = null;
        Drain();
        window.UpdateLayout();
        Assert.Equal(Visibility.Visible, NamedDescendant<Grid>(button, "DefaultOrbGlyph").Visibility);
        Assert.Equal(Visibility.Visible, NamedDescendant<Grid>(proxy, "DefaultOrbGlyph").Visibility);
        Assert.Equal(Visibility.Collapsed,
            NamedDescendant<ContentPresenter>(button, "CustomOrbGlyph").Visibility);
        Assert.Equal(Visibility.Collapsed,
            NamedDescendant<ContentPresenter>(proxy, "CustomOrbGlyph").Visibility);
        ribbon.IsBackstageOpen = false;
        Drain();

        ribbon.ApplicationOrbGlyphTemplate = first;
        ribbon.ApplicationMenu = new RibbonApplicationMenu();
        ribbon.IsBackstageOpen = true;
        Drain();
        window.UpdateLayout();
        Assert.True(ribbon.IsApplicationMenuOpen);
        Assert.Same(first, NamedDescendant<ContentPresenter>(button, "CustomOrbGlyph").ContentTemplate);
        Assert.Same(Brushes.Gold,
            Descendant<Path>(NamedDescendant<ContentPresenter>(button, "CustomOrbGlyph")).Fill);
        ribbon.IsBackstageOpen = false;
        ribbon.ApplicationMenu = null;
        Drain();

        var backstage = Assert.IsType<Backstage>(ribbon.Backstage);
        backstage.Design = RibbonBackstageDesign.Glass2007;
        ribbon.IsBackstageOpen = true;
        Drain();
        ribbon.IsBackstageOpen = false;
        Drain();
        Assert.Same(first, NamedDescendant<ContentPresenter>(button, "CustomOrbGlyph").ContentTemplate);
    }

    private static DataTemplate GlyphTemplate(Brush fill)
    {
        var mark = new FrameworkElementFactory(typeof(Path));
        mark.SetValue(Path.DataProperty, Geometry.Parse("M0,0 L16,0 L16,16 Z"));
        mark.SetValue(Shape.FillProperty, fill);
        mark.SetValue(FrameworkElement.WidthProperty, 16d);
        mark.SetValue(FrameworkElement.HeightProperty, 16d);
        return new DataTemplate { VisualTree = mark };
    }

    private static T NamedDescendant<T>(DependencyObject root, string name)
        where T : FrameworkElement
    {
        FrameworkElement? found = FindNamedDescendant(root, name);
        return Assert.IsType<T>(found);
    }

    private static T Descendant<T>(DependencyObject root) where T : FrameworkElement
    {
        T? found = FindDescendant<T>(root);
        return Assert.IsType<T>(found);
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : FrameworkElement
    {
        if (root is T matching)
            return matching;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            T? found = FindDescendant<T>(VisualTreeHelper.GetChild(root, i));
            if (found is not null)
                return found;
        }
        return null;
    }

    private static FrameworkElement? FindNamedDescendant(DependencyObject root, string name)
    {
        if (root is FrameworkElement element && element.Name == name)
            return element;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            FrameworkElement? found = FindNamedDescendant(VisualTreeHelper.GetChild(root, i), name);
            if (found is not null)
                return found;
        }
        return null;
    }

    private static Thickness LeadingInset(Thickness margin, double inset, bool rtl) => rtl
        ? new Thickness(margin.Left, margin.Top, margin.Right + inset, margin.Bottom)
        : new Thickness(margin.Left + inset, margin.Top, margin.Right, margin.Bottom);

    private static void AssertPastCorner(FrameworkElement element, Border body,
        RibbonTabControl tabs, bool rtl)
    {
        Rect bounds = element.TransformToVisual(tabs).TransformBounds(
            new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        Rect bodyBounds = body.TransformToVisual(tabs).TransformBounds(
            new Rect(0, 0, body.ActualWidth, body.ActualHeight));
        if (rtl)
            Assert.True(bounds.Right <= bodyBounds.Right - body.CornerRadius.TopRight + 0.5);
        else
            Assert.True(bounds.Left >= bodyBounds.Left + body.CornerRadius.TopLeft - 0.5);
    }

    private static void AssertShape(Ribbon ribbon, RibbonApplicationButtonShape expected)
    {
        Assert.Equal(expected, ribbon.ApplicationButtonShape);
        var tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
        var button = Assert.IsType<ToggleButton>(tabs.Template.FindName("PART_ApplicationButton", tabs));
        button.ApplyTemplate();
        var orb = Assert.IsType<ContentPresenter>(button.Template.FindName("Orb", button));
        Assert.Equal(expected == RibbonApplicationButtonShape.Orb
            ? Visibility.Visible : Visibility.Collapsed, orb.Visibility);
    }

    private static void Drain() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

    private static void RunSta(Action body)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception exception) { failure = ExceptionDispatchInfo.Capture(exception); }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        // This aggregate realizes consumer windows across themes, directions and Backstage designs.
        if (!thread.Join(TimeSpan.FromSeconds(300)))
            throw new TimeoutException("The ribbon portability test did not finish within 300 seconds.");
        failure?.Throw();
    }
}
