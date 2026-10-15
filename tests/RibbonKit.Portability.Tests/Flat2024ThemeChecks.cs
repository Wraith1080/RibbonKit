using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Runs on the existing consumer's STA/Application, without Showcase resources or helpers.
internal static class Flat2024ThemeChecks
{
    internal static void Verify(Application application)
    {
        var previousMotion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        ThemeManager.SetAccentedTitleBar(application, false);
        ThemeManager.SetTitleBarBackdrop(application, false);
        ThemeManager.ClearAccent(application);
        var button = new RibbonButton { Header = "Normal", Size = RibbonControlSize.Large };
        var toggle = new RibbonToggleButton { Header = "Toggle", Size = RibbonControlSize.Large };
        var drop = new RibbonDropDownButton { Header = "Dropdown", Size = RibbonControlSize.Large };
        var split = new RibbonSplitButton { Header = "Split", Size = RibbonControlSize.Large };
        var combo = new RibbonComboBox { InputWidth = 100 };
        combo.Items.Add("One");
        combo.SelectedIndex = 0;
        var group = new RibbonGroup { Header = "Commands", CanResize = false, ShowDialogLauncher = true };
        foreach (var control in new Control[] { button, toggle, drop, split, combo }) group.Items.Add(control);
        var home = new RibbonTab { Header = "Home" };
        home.Groups.Add(group);
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon };
        ribbon.Tabs.Add(home);
        ribbon.SelectedTab = home;
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save" });
        var messageBar = new RibbonMessageBar();
        messageBar.Items.Add(new RibbonMessage { Title = "Notice", Message = "Connected surface" });
        ribbon.MessageBar = messageBar;
        var menu = new RibbonApplicationMenu { DefaultContent = new TextBlock { Text = "Recent documents" } };
        var menuCommand = new RibbonApplicationMenuItem { Header = "New" };
        menu.Items.Add(menuCommand);
        ribbon.ApplicationMenu = menu;
        var backstage = new Backstage { Design = RibbonBackstageDesign.Modern, Height = 240 };
        var backstageHome = new BackstageTabItem { Header = "Home", Content = new TextBlock { Text = "Page content" } };
        var backstageInfo = new BackstageTabItem { Header = "Info", Content = new TextBlock { Text = "Info content" } };
        backstage.Items.Add(backstageHome);
        backstage.Items.Add(backstageInfo);
        backstage.SelectedIndex = 0;
        // A detached group exercises the same collapsed template without adaptive layout resetting it.
        var collapsedGroup = new RibbonGroup { Header = "Collapsed", Width = 100 };
        collapsedGroup.Items.Add(new RibbonButton { Header = "Action" });
        SetState(collapsedGroup, typeof(RibbonGroup), "SizeStatePropertyKey", RibbonGroupSizeState.Collapsed);
        var panel = new StackPanel();
        panel.Children.Add(ribbon);
        panel.Children.Add(collapsedGroup);
        panel.Children.Add(backstage);
        var utilityStyles = new ResourceDictionary { Source = new Uri("/RibbonKit;component/Themes/Controls.Shared.xaml", UriKind.Relative) };
        var utility = new RepeatButton { Style = (Style)utilityStyles["RibbonKit.ScrollLeftButton"], Height = 30 };
        panel.Children.Add(utility);
        var window = new RibbonWindow { Content = panel, Width = 900, Height = 680,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024Flat);
            window.Show();
            foreach (bool dark in new[] { false, true })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            {
                ThemeManager.SetDarkMode(application, dark);
                ribbon.Density = density;
                Ribbon.SetDensity(collapsedGroup, density);
                window.FlowDirection = direction;
                Layout(window);
                var original = ThemeManager.CreatePalette(RibbonTheme.Office2024, dark: dark);
                var flat = ThemeManager.CreatePalette(RibbonTheme.Office2024Flat, dark: dark);
                var utilityRim = Part<Border>(utility, "CrystalUtilityRim");
                SetState(utility, typeof(ButtonBase), "IsPressedPropertyKey", true); Layout(window);
                Assert.Equal(1, utilityRim.Opacity); Assert.Equal(new Thickness(1), utilityRim.BorderThickness);
                Assert.Equal(ColorOf(flat["RibbonKit.Brushes.Control.PressedBorder"]), ColorOf(utilityRim.BorderBrush));
                SetState(utility, typeof(ButtonBase), "IsPressedPropertyKey", false); Layout(window); Assert.Equal(0, utilityRim.Opacity);
                var collapse = Part<ToggleButton>(Part<RibbonTabControl>(ribbon, "TabControlHost"), "MinimizeToggle");
                collapse.IsChecked = true; Layout(window);
                var checkedRim = Part<Border>(collapse, "CrystalUtilityRim");
                Assert.Equal(1, checkedRim.Opacity);
                Assert.Equal(ColorOf(flat["RibbonKit.Brushes.Utility.CheckedBorder"]), ColorOf(checkedRim.BorderBrush));
                collapse.IsChecked = false; Layout(window);
                Assert.True(ThemeManager.SupportsDarkMode(RibbonTheme.Office2024Flat));
                Assert.Equal(original["RibbonKit.Metrics.ContentBorderThickness"], flat["RibbonKit.Metrics.ContentBorderThickness"]);
                Assert.Equal(original["RibbonKit.Metrics.ContentCornerRadius"], flat["RibbonKit.Metrics.ContentCornerRadius"]);
                foreach (string effect in new[] { "PopupShadow", "ApplicationMenuShadow",
                    "ApplicationButtonMenuOpenShadow", "ApplicationOrbShadow" })
                    Assert.Equal(0, Assert.IsType<DropShadowEffect>(flat["RibbonKit.Effects." + effect]).Opacity);
                double originalContrast = Contrast(ColorOf(original["RibbonKit.Brushes.Ribbon.Border"]), ColorOf(original["RibbonKit.Brushes.Ribbon.BodyBackground"]));
                double flatContrast = Contrast(ColorOf(flat["RibbonKit.Brushes.Ribbon.Border"]), ColorOf(flat["RibbonKit.Brushes.Ribbon.BodyBackground"]));
                Assert.True(flatContrast > originalContrast);
                if (dark) Assert.True(flatContrast >= 3);
                if (!dark)
                {
                    foreach (string border in new[] { "Popup.Border", "ScreenTip.OuterBorder",
                        "ApplicationMenu.FrameBorder", "ComboBox.PopupBorder", "InRibbonGallery.PopupBorder" })
                        Assert.True(Contrast(ColorOf(flat["RibbonKit.Brushes." + border]),
                            ColorOf(flat["RibbonKit.Brushes.Ribbon.BodyBackground"])) > flatContrast);
                }
                var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
                Assert.Equal(new Thickness(1), Part<Border>(tabs, "ContentHost").BorderThickness);
                foreach (var pair in new (string Key, Effect Actual)[] {
                    ("ContentShadow", Part<Border>(tabs, "ContentHost").Effect),
                    ("QatExtenderShadow", Part<Border>(ribbon, "QatBelowHost").Effect),
                    ("ContentShadow", messageBar.Effect) })
                {
                    var shadow = Assert.IsType<DropShadowEffect>(pair.Actual);
                    var originalShadow = Assert.IsType<DropShadowEffect>(original["RibbonKit.Effects." + pair.Key]);
                    Assert.True(shadow.Opacity > 0 && shadow.Opacity < originalShadow.Opacity);
                    Assert.True(shadow.ShadowDepth > 0 && shadow.ShadowDepth < originalShadow.ShadowDepth);
                    Assert.True(shadow.BlurRadius > shadow.ShadowDepth);
                    Assert.Equal(270, shadow.Direction);
                }

                SetState(button, typeof(UIElement), "IsMouseOverPropertyKey", true);
                var hoverWash = Part<Border>(button, "HoverWash");
                Assert.Equal(ColorOf(hoverWash.Background), ColorOf(hoverWash.BorderBrush));
                Assert.Equal(new Thickness(1), hoverWash.BorderThickness);
                SetState(button, typeof(ButtonBase), "IsPressedPropertyKey", true);
                Assert.Equal(1, Part<Border>(button, "PressWash").Opacity);
                Assert.True(ColorOf(Part<Border>(button, "PressWash").BorderBrush).A > 0);
                SetState(button, typeof(ButtonBase), "IsPressedPropertyKey", false);
                SetState(button, typeof(UIElement), "IsMouseOverPropertyKey", false);
                toggle.IsChecked = true;
                Assert.Equal(1, Part<Border>(toggle, "CheckWash").Opacity);
                Assert.True(ColorOf(Part<Border>(toggle, "CheckWash").BorderBrush).A > 0);
                SetState(toggle, typeof(UIElement), "IsMouseOverPropertyKey", true);
                var checkedHoverRim = Part<Border>(toggle, "CheckedHoverRim");
                Assert.Equal(1, checkedHoverRim.Opacity);
                Assert.Equal(new Thickness(1), checkedHoverRim.BorderThickness);
                Assert.Equal(ColorOf(Part<Border>(toggle, "CheckWash").BorderBrush), ColorOf(checkedHoverRim.BorderBrush));
                SetState(toggle, typeof(UIElement), "IsMouseOverPropertyKey", false);
                toggle.IsChecked = false;

                CheckNativeButton(Part<ToggleButton>(drop, "PART_Toggle"), window, outlineHover: true);
                CheckNativeButton(Part<Button>(split, "PART_Primary"), window, outlineHover: true,
                    expectedBorder: new Thickness(1, 1, 0, 1));
                CheckNativeButton(Part<ToggleButton>(split, "PART_Toggle"), window, outlineHover: true);
                CheckSplitCompanion(split, window);
                CheckNativeButton(Part<Button>(group, "PART_DialogLauncher"), window, outlineHover: false);
                CheckNativeButton(Part<ToggleButton>(collapsedGroup, "PART_CollapsedButton"), window, outlineHover: false);

                var comboChrome = Part<Border>(combo, "Chrome");
                Assert.Equal(ColorOf(original["RibbonKit.Brushes.Input.Border"]), ColorOf(comboChrome.BorderBrush));
                SetState(combo, typeof(UIElement), "IsMouseOverPropertyKey", true);
                Assert.Equal(ColorOf(original["RibbonKit.Brushes.Input.HoverBorder"]), ColorOf(comboChrome.BorderBrush));
                SetState(combo, typeof(UIElement), "IsMouseOverPropertyKey", false);
                drop.IsDropDownOpen = true;
                Layout(window);
                Assert.Equal(0, Assert.IsType<DropShadowEffect>(Part<Border>(drop, "PART_MenuHost").Effect).Opacity);
                Assert.Equal(new Thickness(1), Part<Border>(drop, "PART_MenuHost").BorderThickness);
                if (!dark)
                    Assert.True(Contrast(ColorOf(Part<Border>(drop, "PART_MenuHost").BorderBrush),
                        ColorOf(Part<Border>(drop, "PART_MenuHost").Background)) > flatContrast);
                drop.IsDropDownOpen = false;
                Layout(window);
                var frame = Part<Border>(backstage, "ModernPageFrame");
                var root = Part<Grid>(backstage, "RootGrid");
                var page = Part<Border>(backstage, "ContentArea");
                var pageHost = Part<Grid>(backstage, "ContentAreaHost");
                Assert.Equal(0, Assert.IsType<DropShadowEffect>(pageHost.Effect).Opacity);
                Assert.Equal(new Thickness(1, 1, 0, 0), frame.BorderThickness);
                Assert.Equal(new CornerRadius(4, 0, 0, 0), frame.CornerRadius);
                Assert.False(frame.IsHitTestVisible);
                Assert.Equal(page.RenderSize, frame.RenderSize);
                Assert.True(frame.ActualWidth < backstage.ActualWidth);
                Assert.Equal(page.TransformToAncestor(backstage).TransformBounds(new Rect(page.RenderSize)),
                    frame.TransformToAncestor(backstage).TransformBounds(new Rect(frame.RenderSize)));
                Assert.Null(root.Clip);
                Assert.Null(pageHost.Clip);
                Assert.NotNull(page.Clip);
                Assert.False(page.Clip.FillContains(new Point(0.1, 0.1)));
                Assert.True(page.Clip.FillContains(new Point(0.1, page.ActualHeight - 0.1)));
                Assert.Same(Part<Border>(backstage, "NavColumn").Background,
                    Part<Border>(backstage, "ModernPageBackdrop").Background);
                Size framedPageSize = page.RenderSize;
                foreach (bool backdrop in new[] { false, true })
                {
                    backstage.Translucent = backdrop;
                    ThemeManager.SetTitleBarBackdrop(application, backdrop);
                    ThemeManager.SetAccentedTitleBar(application, true);
                    Layout(window);
                    Assert.Equal(new Thickness(0), frame.BorderThickness);
                    Assert.Equal(new CornerRadius(0), frame.CornerRadius);
                    Assert.Null(page.Clip);
                    var pageShadow = Assert.IsType<DropShadowEffect>(pageHost.Effect);
                    var originalPageShadow = Assert.IsType<DropShadowEffect>(original["RibbonKit.Effects.Backstage.ContentShadow"]);
                    Assert.True(pageShadow.Opacity > 0);
                    Assert.Equal(originalPageShadow.Opacity, pageShadow.Opacity);
                    Assert.Equal(originalPageShadow.BlurRadius, pageShadow.BlurRadius);
                    Assert.Equal(originalPageShadow.ShadowDepth, pageShadow.ShadowDepth);
                    Assert.Equal(originalPageShadow.Direction, pageShadow.Direction);
                    Assert.Equal(framedPageSize, page.RenderSize);
                    Assert.Same(Part<Border>(backstage, "NavColumn").Background,
                        Part<Border>(backstage, "ModernPageBackdrop").Background);
                    ThemeManager.SetAccentedTitleBar(application, false);
                    Layout(window);
                    Assert.Equal(new Thickness(1, 1, 0, 0), frame.BorderThickness);
                    Assert.Equal(new CornerRadius(4, 0, 0, 0), frame.CornerRadius);
                    Assert.NotNull(page.Clip);
                    Assert.False(page.Clip.FillContains(new Point(0.1, 0.1)));
                    Assert.Equal(0, Assert.IsType<DropShadowEffect>(pageHost.Effect).Opacity);
                    Assert.Equal(framedPageSize, page.RenderSize);
                }
                backstage.Translucent = false;
                ThemeManager.SetTitleBarBackdrop(application, false);
                Layout(window);
                Assert.Equal(new Thickness(0), Part<Border>(backstage, "NavColumn").BorderThickness);
                Assert.Equal(new Thickness(0), page.BorderThickness);
                Assert.Equal(0, Assert.IsType<DropShadowEffect>(pageHost.Effect).Opacity);
                var selectedChrome = Part<Border>(backstageHome, "Chrome");
                var idleChrome = Part<Border>(backstageInfo, "Chrome");
                Assert.Equal(new Thickness(1), selectedChrome.BorderThickness);
                Assert.Equal(ColorOf(flat["RibbonKit.Brushes.Control.CheckedBorder"]), ColorOf(selectedChrome.BorderBrush));
                Assert.Equal(new CornerRadius(4), selectedChrome.CornerRadius);
                Assert.True(idleChrome.BorderBrush is null || ColorOf(idleChrome.BorderBrush).A == 0);
                Size homeSize = backstageHome.RenderSize;
                Size infoSize = backstageInfo.RenderSize;
                backstage.SelectedIndex = 1;
                Layout(window);
                Assert.True(selectedChrome.BorderBrush is null || ColorOf(selectedChrome.BorderBrush).A == 0);
                Assert.Equal(ColorOf(flat["RibbonKit.Brushes.Control.CheckedBorder"]), ColorOf(idleChrome.BorderBrush));
                Assert.Equal(homeSize, backstageHome.RenderSize);
                Assert.Equal(infoSize, backstageInfo.RenderSize);
                backstage.SelectedIndex = 0;
                Layout(window);
                ribbon.IsBackstageOpen = true;
                Layout(window);
                Assert.Same(menu.FindResource("RibbonKit.Brushes.ApplicationMenu.InnerOutline"),
                    Part<Border>(menu, "ContentOutline").BorderBrush);
                Assert.Equal(new Thickness(1), Part<Border>(menu, "ContentOutline").BorderThickness);
                var menuPrimary = Part<Button>(menuCommand, "PART_Primary");
                SetState(menuPrimary, typeof(ButtonBase), "IsPressedPropertyKey", true);
                var menuOutline = Part<Border>(menuCommand, "Outline");
                Assert.Equal(1, menuOutline.Opacity);
                Assert.Equal(Part<Border>(menuCommand, "RowShape").CornerRadius, menuOutline.CornerRadius);
                Assert.Equal(new CornerRadius(0), menuOutline.CornerRadius);
                Assert.Equal(new CornerRadius(4), Part<Border>(button, "PressWash").CornerRadius);
                SetState(menuPrimary, typeof(ButtonBase), "IsPressedPropertyKey", false);
                ribbon.IsBackstageOpen = false;
                Layout(window);
            }
            // Scoped and application palettes share the same accent behavior and preserve local precedence.
            ThemeManager.SetAccent(application, Colors.SeaGreen);
            Assert.Equal(Colors.SeaGreen, ColorOf(application.Resources["RibbonKit.Brushes.Tab.SelectedUnderline"]));
            var scoped = ThemeManager.CreatePalette(RibbonTheme.Office2024Flat, Colors.Purple, dark: true);
            drop.Resources.MergedDictionaries.Add(scoped);
            var nativeDrop = Part<ToggleButton>(drop, "PART_Toggle");
            SetState(nativeDrop, typeof(UIElement), "IsMouseOverPropertyKey", true);
            drop.Resources["RibbonKit.Brushes.Control.DropDownHoverBorder"] = Brushes.Magenta;
            Assert.Same(Brushes.Magenta, Part<Border>(nativeDrop, "Chrome").BorderBrush);
            drop.Resources.Remove("RibbonKit.Brushes.Control.DropDownHoverBorder");
            drop.Resources.MergedDictionaries.Remove(scoped);
            SetState(nativeDrop, typeof(UIElement), "IsMouseOverPropertyKey", false);
            // Colored QAT chrome keeps normal hover quiet and acknowledges pressed/checked states.
            Ribbon.SetQatOnColoredSurface(button, true);
            SetState(button, typeof(UIElement), "IsMouseOverPropertyKey", true);
            Assert.Equal(0, Part<Border>(button, "ColoredStateRim").Opacity);
            SetState(button, typeof(ButtonBase), "IsPressedPropertyKey", true);
            Assert.Equal(1, Part<Border>(button, "ColoredStateRim").Opacity);
            SetState(button, typeof(ButtonBase), "IsPressedPropertyKey", false);
            SetState(button, typeof(UIElement), "IsMouseOverPropertyKey", false);
            Ribbon.SetQatOnColoredSurface(button, false);
            Ribbon.SetQatOnColoredSurface(drop, true);
            CheckNativeButton(nativeDrop, window, outlineHover: true);
            Ribbon.SetQatOnColoredSurface(drop, false);
            Ribbon.SetQatOnColoredSurface(split, true);
            CheckSplitCompanion(split, window);
            Ribbon.SetQatOnColoredSurface(split, false);
            backstage.Design = RibbonBackstageDesign.Classic;
            Layout(window);
            Assert.Null(Part<Grid>(backstage, "RootGrid").Clip);
            Assert.Null(Part<Border>(backstage, "ContentArea").Clip);
            Assert.Equal(Visibility.Collapsed, Part<Border>(backstage, "ModernPageFrame").Visibility);
            Assert.Null(Part<Grid>(backstage, "ContentAreaHost").Effect);
            backstage.Design = RibbonBackstageDesign.Modern;
            Layout(window);
            Size pageSize = Part<Border>(backstage, "ContentArea").RenderSize;
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            Layout(window);
            Assert.Equal(0, ColorOf(Part<Border>(backstageHome, "Chrome").BorderBrush).A);
            Assert.Equal(new Thickness(0), Part<Border>(backstage, "ModernPageFrame").BorderThickness);
            Assert.Null(Part<Grid>(backstage, "RootGrid").Clip);
            Assert.Null(Part<Border>(backstage, "ContentArea").Clip);
            Assert.Equal(pageSize, Part<Border>(backstage, "ContentArea").RenderSize);
            Assert.True(Assert.IsType<DropShadowEffect>(Part<Grid>(backstage, "ContentAreaHost").Effect).Opacity > 0);
            ribbon.IsBackstageOpen = true;
            Layout(window);
            Assert.Equal(new CornerRadius(4), Part<Border>(menuCommand, "Outline").CornerRadius);
            ribbon.IsBackstageOpen = false;
            Assert.True(Assert.IsType<DropShadowEffect>(Part<Border>(Part<RibbonTabControl>(ribbon, "TabControlHost"), "ContentHost").Effect).Opacity > 0);
            Assert.Equal(new Thickness(0), Part<Border>(nativeDrop, "Chrome").BorderThickness);
            Assert.Equal(0.25, Assert.IsType<DropShadowEffect>(application.FindResource("RibbonKit.Effects.PopupShadow")).Opacity);
        }
        finally
        {
            drop.IsDropDownOpen = false;
            ribbon.IsBackstageOpen = false;
            window.Close();
            ThemeManager.ClearAccent(application);
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.SetTitleBarBackdrop(application, false);
            ThemeManager.SetAccentedTitleBar(application, false);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            RibbonAnimation.GlobalLevel = previousMotion;
        }
    }

    private static void CheckNativeButton(ButtonBase button, Window window, bool outlineHover,
        Thickness? expectedBorder = null)
    {
        var chrome = Part<Border>(button, "Chrome");
        var size = button.RenderSize;
        Assert.True(chrome.BorderBrush is null || ColorOf(chrome.BorderBrush).A == 0);
        SetState(button, typeof(UIElement), "IsMouseOverPropertyKey", true);
        Assert.True(ColorOf(chrome.BorderBrush).A > 0);
        if (outlineHover)
            Assert.NotEqual(ColorOf(chrome.Background), ColorOf(chrome.BorderBrush));
        else
            Assert.Equal(ColorOf(chrome.Background), ColorOf(chrome.BorderBrush));
        SetState(button, typeof(ButtonBase), "IsPressedPropertyKey", true);
        Assert.True(ColorOf(chrome.BorderBrush).A > 0);
        Assert.Equal(expectedBorder ?? new Thickness(1), chrome.BorderThickness);
        Layout(window);
        Assert.Equal(size, button.RenderSize);
        SetState(button, typeof(ButtonBase), "IsPressedPropertyKey", false);
        SetState(button, typeof(UIElement), "IsMouseOverPropertyKey", false);
    }

    private static void CheckSplitCompanion(RibbonSplitButton split, Window window)
    {
        foreach (var layout in new[] { RibbonSplitButtonLayout.Horizontal, RibbonSplitButtonLayout.Vertical })
        {
            split.Layout = layout;
            Layout(window);
            var primary = Part<Button>(split, "PART_Primary");
            var arrow = Part<ToggleButton>(split, "PART_Toggle");
            Size size = split.RenderSize;
            foreach (var pair in new (ButtonBase Hovered, ButtonBase Companion)[] { (primary, arrow), (arrow, primary) })
            {
                SetState(pair.Hovered, typeof(UIElement), "IsMouseOverPropertyKey", true);
                Layout(window);
                var activeChrome = Part<Border>(pair.Hovered, "Chrome");
                var companionChrome = Part<Border>(pair.Companion, "Chrome");
                Assert.Equal(ColorOf(activeChrome.BorderBrush), ColorOf(companionChrome.BorderBrush));
                var primaryChrome = Part<Border>(primary, "Chrome");
                var arrowChrome = Part<Border>(arrow, "Chrome");
                Assert.Equal(1, layout == RibbonSplitButtonLayout.Vertical
                    ? primaryChrome.BorderThickness.Bottom + arrowChrome.BorderThickness.Top
                    : primaryChrome.BorderThickness.Right + arrowChrome.BorderThickness.Left);
                Assert.Equal(1, companionChrome.BorderThickness.Left);
                Assert.Equal(1, companionChrome.BorderThickness.Top);
                Assert.Equal(0, ColorOf(companionChrome.Background).A);
                Assert.Equal(size, split.RenderSize);
                SetState(pair.Hovered, typeof(UIElement), "IsMouseOverPropertyKey", false);
                Layout(window);
                Assert.True(companionChrome.BorderBrush is null || ColorOf(companionChrome.BorderBrush).A == 0);
            }
        }
        split.Layout = RibbonSplitButtonLayout.Horizontal;
        Layout(window);
    }

    private static double Contrast(Color a, Color b)
    {
        static double Luminance(Color c)
        {
            static double Linear(byte value) { double n = value / 255d; return n <= 0.04045 ? n / 12.92 : Math.Pow((n + 0.055) / 1.055, 2.4); }
            return 0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);
        }
        double x = Luminance(a), y = Luminance(b);
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }
    private static Color ColorOf(object? brush) => Assert.IsType<SolidColorBrush>(brush).Color;
    private static T Part<T>(Control owner, string name) where T : FrameworkElement
    {
        owner.ApplyTemplate();
        return Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
    }
    private static void SetState(DependencyObject control, Type owner, string key, object value) =>
        control.SetValue(Assert.IsType<DependencyPropertyKey>(owner.GetField(key, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)), value);
    private static void Layout(Window window)
    {
        window.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }
}
