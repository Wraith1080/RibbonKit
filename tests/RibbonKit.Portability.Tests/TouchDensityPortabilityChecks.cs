using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// A realized consumer with only RibbonKit resources; shares the suite's one STA/Application.
internal static class TouchDensityPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var animation = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var button = new RibbonButton { Header = "Copy", Size = RibbonControlSize.Small, SizeDefinition = "Small,Small,Small", HorizontalAlignment = HorizontalAlignment.Left };
        var toggle = new RibbonToggleButton { Header = "Bold", Size = RibbonControlSize.Small, SizeDefinition = "Small,Small,Small", IsChecked = true };
        var check = new RibbonCheckBox { Header = "Guides", IsChecked = true };
        var menuItem = new RibbonMenuItem { Header = "Open" };
        var dropdown = new RibbonDropDownButton { Header = "More", Size = RibbonControlSize.Medium, SizeDefinition = "Medium,Medium,Medium" };
        dropdown.Items.Add(menuItem);
        for (int i = 0; i < 20; i++) dropdown.Items.Add(new RibbonMenuItem { Header = $"Command {i + 1}" });
        var split = new RibbonSplitButton { Header = "Paste", Size = RibbonControlSize.Medium, SizeDefinition = "Medium,Medium,Medium" };
        split.Items.Add(new RibbonMenuItem { Header = "Special" });
        var verticalSplit = new RibbonSplitButton { Header = "Paste", Layout = RibbonSplitButtonLayout.Vertical, Size = RibbonControlSize.Large };
        var combo = new RibbonComboBox { Header = "Font", IsEditable = true, ItemsSource = new[] { "Aptos", "Georgia" }, SelectedIndex = 1 };
        var text = new RibbonTextBox { Header = "Find", Text = "Keep text" };
        var radio = new RibbonRadioButton { Header = "Portrait", IsChecked = true };
        var icon = new DrawingImage(new GeometryDrawing(Brushes.SteelBlue, new Pen(Brushes.White, 1),
            Geometry.Parse("M2,2 L14,2 L14,14 L2,14 Z M5,6 L11,6 M5,9 L11,9")));
        button.Icon = toggle.Icon = dropdown.Icon = split.Icon = verticalSplit.Icon = icon;
        verticalSplit.LargeIcon = icon;
        var gallery = new InRibbonGallery { Width = 240, SelectedIndex = 0 };
        for (int i = 0; i < 12; i++)
            gallery.Items.Add(new RibbonGalleryItem { Content = new Border { Width = 60, Height = 26,
                Background = Brushes.SteelBlue, Child = new TextBlock { Text = $"Style {i + 1}", Foreground = Brushes.White } } });

        var commands = new RibbonGroup { Header = "Commands", CanResize = false, ShowDialogLauncher = true };
        commands.Items.Add(Column(button, toggle, check));
        commands.Items.Add(Column(dropdown, split));
        commands.Items.Add(verticalSplit);
        var inputs = new RibbonGroup { Header = "Inputs", CanResize = false };
        inputs.Items.Add(Column(combo, text, radio));
        var styles = new RibbonGroup { Header = "Gallery", CanResize = false };
        styles.Items.Add(gallery);
        var home = new RibbonTab { Header = "Home" };
        home.Groups.Add(commands); home.Groups.Add(inputs); home.Groups.Add(styles);
        var backstage = new Backstage();
        var backstageTab = new BackstageTabItem { Header = "Information", Content = new TextBlock { Text = "Consumer content" } };
        backstage.Items.Add(backstageTab);
        var ribbon = new Ribbon { Backstage = backstage, QuickAccessMaxWidth = 120 };
        var message = new RibbonMessage { Title = "NOTICE", Message = "Consumer message", ActionContent = "Continue" };
        var unavailableMessage = new RibbonMessage { Message = "Unavailable action", ActionContent = "Unavailable",
            Icon = icon, ActionCommand = new UnavailableCommand() };
        var messages = new RibbonMessageBar();
        messages.Items.Add(message); messages.Items.Add(unavailableMessage); ribbon.MessageBar = messages;
        ribbon.Tabs.Add(home); ribbon.SelectedTab = home;
        var qat = new RibbonButton { Header = "Save", Size = RibbonControlSize.Small };
        qat.Icon = icon;
        ribbon.QuickAccessItems.Add(qat);
        for (int i = 0; i < 6; i++) ribbon.QuickAccessItems.Add(new RibbonButton { Header = $"QAT {i + 1}", Size = RibbonControlSize.Small });
        ribbon.QuickAccessItems.Add(new RibbonDropDownButton { Header = "Overflow dropdown", Icon = icon, Size = RibbonControlSize.Small });
        ribbon.QuickAccessItems.Add(new RibbonSplitButton { Header = "Overflow split", Icon = icon, Size = RibbonControlSize.Small });
        var unrelated = new RibbonButton { Header = "Separate consumer", Size = RibbonControlSize.Small };
        var content = new DockPanel();
        DockPanel.SetDock(ribbon, Dock.Top); content.Children.Add(ribbon);
        content.Children.Add(unrelated);
        var window = Window(content, 1120, 650);
        try
        {
            window.Show();
            Assert.Equal(RibbonDensity.Compact, ribbon.Density);
            Assert.Throws<ArgumentException>(() => ribbon.Density = (RibbonDensity)123);
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                foreach (FlowDirection flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
                {
                    ribbon.FlowDirection = flow;
                    ribbon.Density = RibbonDensity.Compact;
                    Layout(window);
                    double compactHeight = ribbon.ActualHeight;
                    double messageFont = Part<TextBlock>(message, "MessageText").FontSize;
                    double actionFont = Part<Button>(message, "PART_ActionButton").FontSize;
                    Assert.True(Part<Button>(message, "PART_ActionButton").ActualHeight < 44);
                    Assert.True(Part<Button>(message, "PART_CloseButton").ActualHeight < 44);
                    Assert.True(button.ActualWidth < 44, $"Compact button width: {button.ActualWidth}");
                    Assert.Equal(24, Part<Grid>(combo, "InputBox").ActualHeight);
                    AssertGalleryHeight(gallery, 54);
                    ribbon.Density = RibbonDensity.Touch;
                    Layout(window);
                    Assert.True(ribbon.ActualHeight > compactHeight);
                    foreach (var control in new Control[] { button, toggle, check, dropdown, split, combo, text, radio })
                    {
                        Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(control));
                        Assert.True(control.ActualHeight >= 44, $"{control.GetType().Name} height: {control.ActualHeight}");
                        Assert.True(control.ActualWidth >= 44);
                    }
                    Assert.Equal(RibbonDensity.Compact, Ribbon.GetDensity(unrelated));
                    foreach (var row in new[] { message, unavailableMessage })
                    {
                        Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(row));
                        Assert.True(row.ActualHeight >= 52);
                        var action = Part<Button>(row, "PART_ActionButton");
                        var close = Part<Button>(row, "PART_CloseButton");
                        Assert.True(action.ActualWidth >= 44 && action.ActualHeight >= 44);
                        Assert.True(close.ActualWidth >= 44 && close.ActualHeight >= 44);
                        Assert.Equal(messageFont, Part<TextBlock>(row, "MessageText").FontSize);
                        Assert.Equal(actionFont, action.FontSize);
                        Assert.Equal(20, Part<Grid>(row, "MessageIconHost").ActualWidth);
                    }
                    Assert.False(Part<Button>(unavailableMessage, "PART_ActionButton").IsEnabled);
                    Part<Button>(message, "PART_CloseButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Layout(window); Assert.False(message.IsOpen);
                    Assert.True(messages.HasOpenMessages);
                    message.IsOpen = true; Layout(window);
                    Assert.True(home.ActualHeight >= 44);
                    Assert.True(Part<Button>(commands, "PART_DialogLauncher").ActualWidth >= 44);
                    Assert.True(Part<Button>(commands, "PART_DialogLauncher").ActualHeight >= 44);
                    VerifySplit(split); VerifySplit(verticalSplit);
                    Assert.Equal(44, Part<Grid>(combo, "InputBox").ActualHeight);
                    Assert.Equal(44, Part<Grid>(text, "InputBox").ActualHeight);
                    AssertGalleryHeight(gallery, 60);
                    var viewport = Part<ScrollViewer>(gallery, "PART_ScrollViewer");
                    var side = Part<Grid>(gallery, "SideButtonHost");
                    Assert.InRange(side.TranslatePoint(new Point(), gallery).Y, 0, 1);
                    Assert.False(Part<FrameworkElement>(gallery, "PART_LineUp").IsVisible);
                    Assert.False(Part<FrameworkElement>(gallery, "PART_LineDown").IsVisible);
                    var opener = Part<FrameworkElement>(gallery, "PART_ExpandToggle");
                    Assert.True(opener.ActualWidth >= 44 && opener.ActualHeight >= 44);
                    Assert.Equal(PanningMode.VerticalOnly, viewport.PanningMode);
                    var tile = (RibbonGalleryItem)gallery.Items[0];
                    Assert.InRange(tile.ActualHeight, viewport.ViewportHeight - 1, viewport.ViewportHeight + 1);
                    Assert.InRange(viewport.ViewportWidth / tile.ActualWidth,
                        Math.Round(viewport.ViewportWidth / tile.ActualWidth) - 0.02,
                        Math.Round(viewport.ViewportWidth / tile.ActualWidth) + 0.02);
                    Assert.True(dropdown.ActualWidth >= 56);
                    Assert.True(Part<System.Windows.Shapes.Path>(dropdown, "MediumChevron").Margin.Left >= 8);
                    Assert.Equal("Keep text", text.Text); Assert.Equal(1, combo.SelectedIndex);
                    Assert.True(toggle.IsChecked); Assert.True(check.IsChecked); Assert.True(radio.IsChecked);

                    // A local override wins; clearing it restores the owning scope immediately.
                    Ribbon.SetDensity(button, RibbonDensity.Compact); Layout(window);
                    Assert.True(button.ActualWidth < 44);
                    button.ClearValue(Ribbon.DensityProperty); Layout(window);
                    Assert.True(button.ActualWidth >= 44);
                    button.MinWidth = 70; Layout(window);
                    Assert.Equal(70, button.MinWidth);
                    Assert.InRange(button.ActualWidth, 70, 71);
                    button.ClearValue(FrameworkElement.MinWidthProperty);

                    if (flow == FlowDirection.LeftToRight)
                    {
                        VerifyPopups();
                        if (theme is RibbonTheme.Office2024 or RibbonTheme.CrystalLight)
                            SavePreview(ribbon, $"{theme}-{(dark ? "dark" : "light")}-touch");
                    }
                    ribbon.Density = RibbonDensity.Compact; Layout(window);
                    Assert.Equal(compactHeight, ribbon.ActualHeight);
                    AssertGalleryHeight(gallery, 54);
                    if (flow == FlowDirection.LeftToRight && theme is RibbonTheme.Office2024 or RibbonTheme.CrystalLight)
                        SavePreview(ribbon, $"{theme}-{(dark ? "dark" : "light")}-compact");
                }
            }

            ribbon.Density = RibbonDensity.Touch;
            foreach (RibbonQuickAccessPosition position in Enum.GetValues<RibbonQuickAccessPosition>())
            {
                ribbon.QuickAccessPosition = position; Layout(window);
                Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(qat));
                Assert.True(qat.ActualWidth >= 44 && qat.ActualHeight >= 44);
                FrameworkElement host = position switch
                {
                    RibbonQuickAccessPosition.TitleBar => Assert.IsType<RibbonQuickAccessToolBar>(window.TitleBarContent),
                    RibbonQuickAccessPosition.TabRow => Part<RibbonQuickAccessToolBar>(Part<RibbonTabControl>(ribbon, "TabControlHost"), "QatTabRowHost"),
                    _ => Part<Border>(ribbon, "QatBelowHost"),
                };
                if (position == RibbonQuickAccessPosition.TitleBar)
                {
                    Assert.True(qat.TranslatePoint(new Point(0, qat.ActualHeight), window).Y <= ribbon.TranslatePoint(new Point(), window).Y);
                    double titleHeight = Part<Grid>(window, "TitleBarBand").ActualHeight;
                    Assert.True(titleHeight >= 46);
                    Assert.Equal(46, WindowChrome.GetWindowChrome(window).CaptionHeight);
                    ribbon.IsBackstageOpen = true; Layout(window);
                    Assert.False(window.IsTitleBarContentVisible);
                    Assert.Equal(titleHeight, Part<Grid>(window, "TitleBarBand").ActualHeight);
                    ribbon.IsBackstageOpen = false; Layout(window);
                    Assert.Equal(titleHeight, Part<Grid>(window, "TitleBarBand").ActualHeight);
                }
                if (host is RibbonQuickAccessToolBar toolbar)
                {
                    var opener = Part<ToggleButton>(toolbar, "PART_OverflowButton");
                    Assert.True(opener.ActualWidth >= 44 && opener.ActualHeight >= 44);
                    opener.IsChecked = true; Layout(window);
                    var popup = Part<Popup>(toolbar, "PART_OverflowPopup");
                    Assert.True(popup.IsOpen);
                    Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(popup.Child));
                    var overflowButtons = Descendants(popup.Child).OfType<RibbonButton>().ToArray();
                    Assert.NotEmpty(overflowButtons);
                    Assert.All(overflowButtons, b => Assert.True(b.ActualHeight >= 44));
                    var overflowDropdown = Descendants(popup.Child).OfType<RibbonDropDownButton>()
                        .Single(control => Equals(control.Header, "Overflow dropdown"));
                    var overflowSplit = Descendants(popup.Child).OfType<RibbonSplitButton>().Single();
                    foreach (var control in new Control[] { overflowDropdown, overflowSplit })
                    {
                        Assert.Equal(HorizontalAlignment.Left, control.HorizontalContentAlignment);
                        var primary = control is RibbonSplitButton ? Part<ButtonBase>(control, "PART_Primary")
                            : Part<ButtonBase>(control, "PART_Toggle");
                        var presenter = Part<ContentPresenter>(primary, "NativeContent");
                        Assert.Equal(HorizontalAlignment.Left, presenter.HorizontalAlignment);
                        Assert.InRange(presenter.TranslatePoint(new Point(), primary).X, 0, 2);
                    }
                    SavePreview(Assert.IsAssignableFrom<FrameworkElement>(popup.Child), $"qat-{position}-touch-overflow");
                    opener.IsChecked = false; Layout(window);
                }
                var context = Assert.IsType<ContextMenu>(host.ContextMenu);
                context.PlacementTarget = host; context.IsOpen = true; Layout(window);
                Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(context));
                var visibleItems = context.Items.OfType<MenuItem>().Where(item => item.IsVisible).ToArray();
                Assert.True(visibleItems.Length >= 3);
                Assert.All(visibleItems, item => Assert.True(item.ActualHeight >= 44));
                context.IsOpen = false;
                ribbon.Density = RibbonDensity.Compact; Layout(window);
                Assert.Equal(RibbonDensity.Compact, Ribbon.GetDensity(qat));
                if (position == RibbonQuickAccessPosition.TitleBar)
                {
                    Assert.True(Part<Grid>(window, "TitleBarBand").ActualHeight < 46);
                    Assert.True(WindowChrome.GetWindowChrome(window).CaptionHeight < 46);
                }
                ribbon.Density = RibbonDensity.Touch;
            }

            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
            ribbon.IsMinimized = true; Layout(window);
            Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(qat));
            ribbon.Density = RibbonDensity.Compact; ribbon.IsMinimized = false; Layout(window);
            Assert.True(button.ActualWidth < 44);
            ribbon.Density = RibbonDensity.Touch;
            ribbon.IsBackstageOpen = true; Layout(window);
            Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(backstage));
            Assert.True(backstageTab.ActualHeight >= 44);
            ribbon.IsBackstageOpen = false; Layout(window);

            var menu = new RibbonApplicationMenu { DefaultContent = new TextBlock { Text = "Recent documents" } };
            var menuSplit = new RibbonApplicationMenuItem { Header = "Save as", IsSplit = true,
                Command = ApplicationCommands.SaveAs, Content = new TextBlock { Text = "Formats" } };
            menu.Items.Add(menuSplit);
            var footer = new RibbonApplicationMenuButton { Content = "Options" };
            menu.FooterContent = footer; ribbon.ApplicationMenu = menu;
            ribbon.IsBackstageOpen = true; Layout(window);
            Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(menu));
            Assert.True(Part<Button>(menuSplit, "PART_Arrow").ActualWidth >= 44);
            Assert.True(footer.ActualHeight >= 44);
            TouchDown(unrelated); Layout(window);
            Assert.False(ribbon.IsBackstageOpen);

            // A scoped token override updates geometry without changing the density selection.
            ribbon.Resources["RibbonKit.Metrics.Touch.TargetSize"] = 48d; Layout(window);
            Assert.True(button.ActualWidth >= 48);
            ribbon.Resources.Remove("RibbonKit.Metrics.Touch.TargetSize"); Layout(window);
            VerifyAdaptiveRefresh();
            VerifyCollapsedFlyoutDensity(application);
            VerifyReviewSurfaces(application, icon);
        }
        finally
        {
            window.Close();
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            RibbonAnimation.GlobalLevel = animation;
        }

        void VerifyPopups()
        {
            dropdown.IsDropDownOpen = true; Layout(window);
            Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(Part<Popup>(dropdown, "PART_Popup").Child));
            Assert.True(menuItem.ActualHeight >= 44);
            var menuViewport = Part<ScrollViewer>(dropdown, "PART_PopupScrollViewer");
            Assert.Equal(PanningMode.VerticalOnly, menuViewport.PanningMode);
            Assert.Equal(340, menuViewport.MaxHeight);
            Assert.True(menuViewport.ScrollableHeight > 0);
            ribbon.Density = RibbonDensity.Compact; Layout(window);
            Assert.Equal(RibbonDensity.Compact, Ribbon.GetDensity(menuItem));
            ribbon.Density = RibbonDensity.Touch; Layout(window);
            Assert.True(menuItem.ActualHeight >= 44);
            menuViewport.ScrollToEnd(); Layout(window);
            var last = (RibbonMenuItem)dropdown.Items[^1];
            Rect lastBounds = last.TransformToAncestor(menuViewport).TransformBounds(new Rect(last.RenderSize));
            Assert.True(lastBounds.Bottom <= menuViewport.ActualHeight + 1);
            dropdown.IsDropDownOpen = false;
            combo.IsDropDownOpen = true; Layout(window);
            Assert.True(((ComboBoxItem)combo.ItemContainerGenerator.ContainerFromIndex(0)).ActualHeight >= 44);
            combo.IsDropDownOpen = false;
            gallery.IsDropDownOpen = true; Layout(window);
            Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(Part<Popup>(gallery, "PART_Popup").Child));
            Assert.Equal(PanningMode.VerticalOnly, Part<ScrollViewer>(gallery, "PART_PopupScrollViewer").PanningMode);
            Assert.InRange(((RibbonGalleryItem)gallery.Items[0]).ActualHeight, 44, 54);
            gallery.IsDropDownOpen = false; Layout(window);
            Assert.Equal(0, gallery.SelectedIndex);
            var strip = Part<ScrollViewer>(gallery, "PART_ScrollViewer");
            Assert.InRange(((RibbonGalleryItem)gallery.Items[0]).ActualHeight,
                strip.ViewportHeight - 1, strip.ViewportHeight + 1);
            gallery.SelectedIndex = 8; Layout(window);
            gallery.IsDropDownOpen = true; Layout(window);
            gallery.IsDropDownOpen = false; Layout(window);
            Assert.Equal(8, gallery.SelectedIndex);
            var chosen = (RibbonGalleryItem)gallery.Items[8];
            var chosenBounds = chosen.TransformToAncestor(strip).TransformBounds(new Rect(chosen.RenderSize));
            Assert.InRange(chosenBounds.Top, -1, strip.ViewportHeight);
            Assert.InRange(chosenBounds.Bottom, 0, strip.ViewportHeight + 1);
            gallery.Width = 294; Layout(window);
            var rowItems = gallery.Items.Cast<RibbonGalleryItem>()
                .Where(item => Math.Abs(item.TranslatePoint(new Point(), strip).Y
                    - chosen.TranslatePoint(new Point(), strip).Y) < 1).ToArray();
            Assert.InRange(rowItems.Sum(item => item.ActualWidth), strip.ViewportWidth - 1, strip.ViewportWidth + 1);
            gallery.Width = 240; gallery.SelectedIndex = 0;
            gallery.IsDropDownOpen = true; Layout(window);
            gallery.IsDropDownOpen = false; Layout(window);
            dropdown.IsDropDownOpen = true; Layout(window);
            TouchDown(dropdown); Layout(window);
            Assert.True(dropdown.IsDropDownOpen);
            TouchDown(unrelated); Layout(window);
            Assert.False(dropdown.IsDropDownOpen);
        }
    }

    private static void VerifyAdaptiveRefresh()
    {
        var home = new RibbonTab { Header = "Home" };
        for (int g = 0; g < 2; g++)
        {
            var group = new RibbonGroup { Header = $"Group {g + 1}" };
            for (int i = 0; i < 4; i++) group.Items.Add(new RibbonButton
                { Size = RibbonControlSize.Small, SizeDefinition = "Small,Small,Small" });
            home.Groups.Add(group);
        }
        var ribbon = new Ribbon(); ribbon.Tabs.Add(home); ribbon.SelectedTab = home;
        var window = Window(ribbon, 300, 480);
        try
        {
            window.Show(); Layout(window);
            Assert.All(home.Groups, g => Assert.NotEqual(RibbonGroupSizeState.Collapsed, g.SizeState));
            foreach (var item in home.Groups[0].Items.OfType<RibbonButton>()) Ribbon.SetDensity(item, RibbonDensity.Touch);
            Layout(window);
            Assert.True(home.Groups.Any(g => g.SizeState == RibbonGroupSizeState.Collapsed),
                $"Local touch widths did not reduce: ribbon={ribbon.ActualWidth}, groups={string.Join(",", home.Groups.Select(g => g.DesiredSize.Width))}");
            foreach (var item in home.Groups[0].Items.OfType<RibbonButton>()) item.ClearValue(Ribbon.DensityProperty);
            Layout(window);
            Assert.All(home.Groups, g => Assert.NotEqual(RibbonGroupSizeState.Collapsed, g.SizeState));
            ribbon.Density = RibbonDensity.Touch; Layout(window);
            Assert.Contains(home.Groups, g => g.SizeState == RibbonGroupSizeState.Collapsed);
            var collapsed = home.Groups.First(g => g.SizeState == RibbonGroupSizeState.Collapsed);
            Part<ToggleButton>(collapsed, "PART_CollapsedButton").IsChecked = true; Layout(window);
            Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(Part<Border>(collapsed, "PART_PopupHost")));
            Assert.All(collapsed.Items.OfType<RibbonButton>(), b => Assert.True(b.ActualHeight >= 44));
            Part<ToggleButton>(collapsed, "PART_CollapsedButton").IsChecked = false;
            ribbon.Width = 120; Layout(window);
            var content = Part<Border>(Part<RibbonTabControl>(ribbon, "TabControlHost"), "ContentHost");
            var scrollArrows = Descendants(content).OfType<RepeatButton>()
                .Where(b => Equals(b.Tag, "BodyScroll") && b.IsVisible).ToArray();
            Assert.NotEmpty(scrollArrows);
            Assert.All(scrollArrows, b => Assert.True(b.ActualWidth >= 44));
            ribbon.ClearValue(FrameworkElement.WidthProperty);
            ribbon.Density = RibbonDensity.Compact; Layout(window);
            Assert.All(home.Groups, g => Assert.NotEqual(RibbonGroupSizeState.Collapsed, g.SizeState));
        }
        finally { window.Close(); }
    }

    private static void VerifyCollapsedFlyoutDensity(Application application)
    {
        foreach (var theme in Enum.GetValues<RibbonTheme>())
        foreach (bool startTouch in new[] { false, true })
        {
            ThemeManager.Apply(application, theme);
            var tab = new RibbonTab { Header = "View" };
            var fixedGroup = new RibbonGroup { Header = "Fixed", CanResize = false, Width = 340 };
            fixedGroup.Items.Add(new RibbonButton { Header = "Fixed", Size = RibbonControlSize.Large });
            tab.Groups.Add(fixedGroup);
            var group = new RibbonGroup { Header = "Backstage" };
            for (int i = 0; i < 7; i++) group.Items.Add(new RibbonToggleButton
                { Header = $"Command {i}", Size = RibbonControlSize.Large });
            tab.Groups.Add(group);
            var ribbon = new Ribbon { Density = startTouch ? RibbonDensity.Touch : RibbonDensity.Compact };
            ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
            var window = Window(ribbon, 560, 400);
            try
            {
                window.Show(); Layout(window);
                Assert.Equal(RibbonGroupSizeState.Collapsed, group.SizeState);
                for (int iteration = 0; iteration < 4; iteration++)
                {
                    Part<ToggleButton>(group, "PART_CollapsedButton").IsChecked = true;
                    Layout(window);
                    Assert.NotNull(Part<Border>(group, "PART_PopupHost").Child);
                    ribbon.Density = ribbon.Density == RibbonDensity.Compact ? RibbonDensity.Touch : RibbonDensity.Compact;
                    Layout(window);
                    Assert.Equal(RibbonGroupSizeState.Collapsed, group.SizeState);
                }
                group.CanResize = false; Layout(window);
                var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
                var scroll = Part<RibbonKit.Layout.RibbonScrollContentHost>(tabs, "PART_ContentScroll");
                Assert.True(scroll.ExtentWidth > scroll.ViewportWidth);
                Assert.True(scroll.CanScrollRight);
                Assert.Contains(Descendants(Part<Border>(tabs, "ContentHost")).OfType<RepeatButton>(),
                    b => Equals(b.Tag, "BodyScroll") && b.IsVisible);
                SavePreview(ribbon, $"{theme}-adaptive-overflow-{startTouch}");
                scroll.InvalidateMeasure(); Layout(window);
                Assert.True(scroll.CanScrollRight);
            }
            finally { window.Close(); }
        }
    }

    private static void VerifySplit(RibbonSplitButton split)
    {
        var primary = Part<Button>(split, "PART_Primary");
        var arrow = Part<ToggleButton>(split, "PART_Toggle");
        Assert.True(primary.ActualHeight >= 44 && primary.ActualWidth >= 44);
        Assert.True(arrow.ActualHeight >= 44 && arrow.ActualWidth >= 44);
        var primaryBounds = primary.TransformToAncestor(split).TransformBounds(new Rect(primary.RenderSize));
        var arrowBounds = arrow.TransformToAncestor(split).TransformBounds(new Rect(arrow.RenderSize));
        Rect overlap = Rect.Intersect(primaryBounds, arrowBounds);
        Assert.True(overlap.IsEmpty || overlap.Width < 0.01 || overlap.Height < 0.01);
        var content = Part<ContentPresenter>(primary, "NativeContent");
        Point center = content.TranslatePoint(new Point(content.ActualWidth / 2, content.ActualHeight / 2), primary);
        Assert.InRange(center.X, primary.ActualWidth / 2 - 1, primary.ActualWidth / 2 + 1);
        Assert.InRange(center.Y, primary.ActualHeight / 2 - 1, primary.ActualHeight / 2 + 1);
    }

    private static void VerifyReviewSurfaces(Application application, ImageSource icon)
    {
        var large = new RibbonButton { Header = "Message", Size = RibbonControlSize.Large };
        var shortToggle = new RibbonToggleButton { Header = "Touch mode", Size = RibbonControlSize.Large };
        var longToggle = new RibbonToggleButton { Header = "Colored Title Bar", Size = RibbonControlSize.Large };
        large.LargeIcon = shortToggle.LargeIcon = longToggle.LargeIcon = icon;
        var group = new RibbonGroup { Header = "Commands", CanResize = false };
        var adjacentGallery = new InRibbonGallery { Width = 184 };
        adjacentGallery.Items.Add(new RibbonGalleryItem { Content = "Tile" });
        group.Items.Add(large); group.Items.Add(adjacentGallery); group.Items.Add(shortToggle); group.Items.Add(longToggle);
        var smallStack = Column(new RibbonButton { Size = RibbonControlSize.Small, Icon = icon },
            new RibbonButton { Size = RibbonControlSize.Small, Icon = icon },
            new RibbonButton { Size = RibbonControlSize.Small, Icon = icon });
        group.Items.Add(smallStack);
        var separator = new RibbonGroupSeparator(); group.Items.Add(separator);
        var selected = new RibbonTab { Header = "View" }; selected.Groups.Add(group);
        var unselected = new RibbonTab { Header = "Home" };
        var backstage = new Backstage();
        var row = new BackstageTabItem { Header = "Information", Content = new TextBlock { Text = "Consumer page" } };
        backstage.Items.Add(row);
        var ribbon = new Ribbon { Backstage = backstage, Density = RibbonDensity.Touch,
            QuickAccessPosition = RibbonQuickAccessPosition.TitleBar };
        ribbon.Tabs.Add(unselected); ribbon.Tabs.Add(selected); ribbon.SelectedTab = selected;
        var qat = new RibbonButton { Size = RibbonControlSize.Small };
        qat.Icon = icon;
        ribbon.QuickAccessItems.Add(qat);
        var qatDropDown = new RibbonDropDownButton { Size = RibbonControlSize.Small, Icon = icon };
        ribbon.QuickAccessItems.Add(qatDropDown);
        var content = new DockPanel();
        DockPanel.SetDock(ribbon, Dock.Top); content.Children.Add(ribbon);
        content.Children.Add(new Border());
        var window = Window(content, 1000, 620);
        try
        {
            window.Show();
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (FlowDirection flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                TraceReview($"{theme}/{dark}/{flow}: ribbon layout");
                ribbon.FlowDirection = flow; Layout(window);
                double titleHeight = Part<Grid>(window, "TitleBarBand").ActualHeight;
                Assert.True(titleHeight >= 46);
                Assert.Equal(46, WindowChrome.GetWindowChrome(window).CaptionHeight);
                Assert.Equal(136, large.ActualHeight); Assert.Equal(large.ActualHeight, shortToggle.ActualHeight);
                Assert.InRange(large.ActualHeight + large.Margin.Top + large.Margin.Bottom,
                    smallStack.ActualHeight - 1, smallStack.ActualHeight + 1);
                Assert.Equal(large.ActualHeight, longToggle.ActualHeight);
                foreach (var neighbor in new FrameworkElement[] { large, shortToggle })
                {
                    var neighborBounds = neighbor.TransformToAncestor(window).TransformBounds(new Rect(neighbor.RenderSize));
                    var galleryBounds = adjacentGallery.TransformToAncestor(window).TransformBounds(new Rect(adjacentGallery.RenderSize));
                    double gap = Math.Max(galleryBounds.Left - neighborBounds.Right, neighborBounds.Left - galleryBounds.Right);
                    Assert.True(gap >= 4, $"{theme}/{flow}: gallery spacing {gap}");
                }
                if (theme == RibbonTheme.Office2010)
                    Assert.IsType<SolidColorBrush>(Part<Border>(ribbon, "QatBelowHost").Background);
                Assert.Equal(36, Part<Image>(large, "LargeImage").ActualWidth);
                Assert.Equal(20, Part<Image>(qat, "SmallImage").ActualWidth);
                Assert.Equal(20, Part<Grid>(qatDropDown, "SmallIconHost").ActualWidth);
                Assert.True(qatDropDown.ActualWidth >= 56);
                var smallIcon = Part<Grid>(qatDropDown, "SmallIconHost");
                var smallArrow = Part<System.Windows.Shapes.Path>(qatDropDown, "SmallChevron");
                var iconBounds = smallIcon.TransformToAncestor(qatDropDown).TransformBounds(new Rect(smallIcon.RenderSize));
                var arrowBounds = smallArrow.TransformToAncestor(qatDropDown).TransformBounds(new Rect(smallArrow.RenderSize));
                // Both bounds are expressed in the dropdown's own coordinates;
                // its parent's RTL mirror does not change their local ordering.
                double iconArrowGap = Math.Max(arrowBounds.Left - iconBounds.Right,
                    iconBounds.Left - arrowBounds.Right);
                Assert.True(iconArrowGap >= 8, $"{theme}/{flow}: dropdown gap {iconArrowGap}");
                Assert.True(separator.ActualHeight >= large.ActualHeight);
                var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
                var body = Part<Border>(tabs, "ContentHost");
                Assert.InRange(body.ActualHeight, large.ActualHeight, large.ActualHeight + 48);
                var header = Part<Border>(selected, "HeaderChrome");
                if (ribbon.ApplicationButtonShape == RibbonApplicationButtonShape.Tab)
                {
                    var file = Part<ToggleButton>(tabs, "PART_ApplicationButton");
                    var caption = Part<ContentPresenter>(file, "ApplicationCaption");
                    var tabCaption = Part<ContentPresenter>(selected, "HeaderText");
                    Assert.InRange(file.ActualHeight, header.ActualHeight - 1, header.ActualHeight + 1);
                    Assert.True(file.ActualWidth >= 64);
                    double center = caption.TranslatePoint(new Point(0, caption.ActualHeight / 2), tabs).Y;
                    double tabCenter = tabCaption.TranslatePoint(new Point(0, tabCaption.ActualHeight / 2), tabs).Y;
                    Assert.InRange(center, tabCenter - 1, tabCenter + 1);
                }
                var hoverHeader = Part<Border>(unselected, "HeaderChrome");
                var notch = Part<Border>(tabs, "PART_ConnectNotch");
                if (notch.ActualWidth > 0)
                {
                    double gap = body.TranslatePoint(new Point(), window).Y
                        - header.TranslatePoint(new Point(0, header.ActualHeight), window).Y;
                    Assert.InRange(gap, -1.1, 1.1);
                }
                SetHover(hoverHeader, true); Layout(window);
                var hoverIndicator = Part<System.Windows.Shapes.Rectangle>(unselected, "HoverIndicator");
                Assert.True(VisibleBrush(hoverHeader.Background)
                    || hoverIndicator.IsVisible && hoverIndicator.Opacity > 0 && VisibleBrush(hoverIndicator.Fill));
                SetHover(hoverHeader, false);
                if (ribbon.ApplicationButtonShape == RibbonApplicationButtonShape.Orb)
                {
                    var app = Part<ToggleButton>(tabs, "PART_ApplicationButton");
                    var orb = Part<ContentPresenter>(app, "Orb");
                    var surface = Assert.IsType<Grid>(orb.ContentTemplate.FindName("OrbSurface", orb));
                    Assert.Equal(52, surface.ActualWidth); Assert.Equal(52, surface.ActualHeight);
                    Assert.True(surface.TranslatePoint(new Point(0, surface.ActualHeight), window).Y
                        <= body.TranslatePoint(new Point(), window).Y + 1);
                }
                if (!dark && flow == FlowDirection.LeftToRight && theme == RibbonTheme.Office2007)
                    SavePreview(window, "Office2007-touch-window");
                foreach (var design in Enum.GetValues<RibbonBackstageDesign>())
                {
                    TraceReview($"{theme}/{dark}/{flow}: backstage {design}");
                    backstage.Design = design;
                    if (design is RibbonBackstageDesign.CrystalFloating or RibbonBackstageDesign.CrystalSidebar)
                        backstage.SetResourceReference(FrameworkElement.StyleProperty,
                            design == RibbonBackstageDesign.CrystalFloating ? "Crystal.Backstage.Style" : "Crystal.Backstage.Sidebar");
                    else backstage.ClearValue(FrameworkElement.StyleProperty);
                    ribbon.IsBackstageOpen = true; Layout(window);
                    Assert.Equal(titleHeight, Part<Grid>(window, "TitleBarBand").ActualHeight);
                    Assert.True(row.ActualHeight >= 52, $"{theme}/{design}: nav row {row.ActualHeight}");
                    double touchFont = row.FontSize;
                    ribbon.Density = RibbonDensity.Compact; Layout(window);
                    Assert.Equal(touchFont, row.FontSize);
                    ribbon.Density = RibbonDensity.Touch; Layout(window);
                    if (design == RibbonBackstageDesign.Classic2007
                        && ribbon.ApplicationButtonShape == RibbonApplicationButtonShape.Orb)
                    {
                        var proxy = Descendants(window).OfType<Button>()
                            .Single(button => button.Name == "PART_Classic2007OrbProxy");
                        Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(proxy));
                        var surface = Descendants(proxy).OfType<Grid>().Single(grid => grid.Name == "OrbSurface");
                        Assert.Equal(52, surface.ActualHeight); Assert.Equal(52, surface.ActualWidth);
                        var nav = Part<Border>(backstage, "NavColumn");
                        var page = Part<Border>(backstage, "ContentArea");
                        Assert.Equal(page.Margin.Top, nav.Margin.Top);
                        Assert.Same(page.Background, nav.Background);
                        Assert.Equal(page.TranslatePoint(new Point(), window).Y,
                            nav.TranslatePoint(new Point(), window).Y);
                        Assert.InRange(nav.Padding.Top, 16, 16);
                        var rowChrome = Part<Border>(row, "Chrome");
                        Assert.True(rowChrome.TranslatePoint(new Point(), window).Y
                            >= surface.TranslatePoint(new Point(0, surface.ActualHeight), window).Y + 4);
                    }
                    Assert.True(row.TranslatePoint(new Point(0, row.ActualHeight), window).Y
                        <= window.ActualHeight, $"{theme}/{design}: nav row clipped by its surface");
                    if (!dark && flow == FlowDirection.LeftToRight
                        && (theme == RibbonTheme.Office2007 && design == RibbonBackstageDesign.Classic2007
                            || theme == RibbonTheme.CrystalLight && design == RibbonBackstageDesign.CrystalSidebar))
                        SavePreview(window, $"{theme}-{design}-touch-backstage");
                    ribbon.IsBackstageOpen = false; Layout(window);
                    Assert.Equal(titleHeight, Part<Grid>(window, "TitleBarBand").ActualHeight);
                }
                ribbon.Backstage = null;
                var menu = new RibbonApplicationMenu();
                var split = new RibbonApplicationMenuItem { Header = "Save As", Icon = icon, IsSplit = true,
                    Command = ApplicationCommands.SaveAs, Content = new TextBlock { Text = "Formats" } };
                var dropdown = new RibbonApplicationMenuItem { Header = "Prepare", Icon = icon, IsSplit = false,
                    Content = new TextBlock { Text = "Details" } };
                menu.Items.Add(split); menu.Items.Add(dropdown); ribbon.ApplicationMenu = menu;
                ribbon.IsBackstageOpen = true; Layout(window);
                double menuFont = split.FontSize;
                ribbon.Density = RibbonDensity.Compact; Layout(window);
                Assert.Equal(menuFont, split.FontSize);
                Assert.Equal(menuFont, dropdown.FontSize);
                ribbon.Density = RibbonDensity.Touch; Layout(window);
                if (theme == RibbonTheme.Office2007)
                {
                    var app = Part<ToggleButton>(tabs, "PART_ApplicationButton");
                    var orb = Part<ContentPresenter>(app, "Orb");
                    var surface = Assert.IsType<Grid>(orb.ContentTemplate.FindName("OrbSurface", orb));
                    Assert.True(split.TranslatePoint(new Point(), window).Y
                        >= surface.TranslatePoint(new Point(0, surface.ActualHeight), window).Y + 2);
                    if (!dark && flow == FlowDirection.LeftToRight)
                        SavePreview(window, "Office2007-orb-menu-clearance");
                }
                TraceReview($"{theme}/{dark}/{flow}: application menu open");
                foreach (var item in new[] { split, dropdown })
                {
                    var root = Part<Grid>(item, "Root");
                    Assert.Equal(56, root.ActualHeight);
                    Assert.Equal(44, Part<Border>(item, "ArrowFill").ActualWidth);
                    Assert.Equal(44, Part<Button>(item, "PART_Arrow").ActualWidth);
                    var main = Part<Border>(item, "MainFill");
                    var arrow = Part<Border>(item, "ArrowFill");
                    Assert.InRange(main.ActualWidth + arrow.ActualWidth, root.ActualWidth - 1, root.ActualWidth + 1);
                    var primary = Part<Button>(item, "PART_Primary");
                    SetHover(primary, true); Layout(window);
                    Assert.Equal(1, main.Opacity); Assert.Equal(1, arrow.Opacity);
                    Assert.Equal(main.ActualHeight, arrow.ActualHeight);
                    if (!dark && flow == FlowDirection.LeftToRight
                        && theme is RibbonTheme.Office2007 or RibbonTheme.CrystalLight)
                        SavePreview(menu, $"{theme}-{(item.IsSplit ? "split" : "dropdown")}-touch-menu");
                    SetHover(primary, false); Layout(window);
                }
                if (theme == RibbonTheme.Office2007)
                {
                    ribbon.Density = RibbonDensity.Compact; Layout(window);
                    Assert.Equal(52, Part<Grid>(split, "Root").ActualHeight);
                    ribbon.Density = RibbonDensity.Touch; Layout(window);
                    Assert.Equal(56, Part<Grid>(split, "Root").ActualHeight);
                }
                ribbon.IsBackstageOpen = false; ribbon.ApplicationMenu = null; ribbon.Backstage = backstage;
                Layout(window);
                if (flow == FlowDirection.LeftToRight) SavePreview(ribbon, $"{theme}-{(dark ? "dark" : "light")}-review");
            }
        }
        finally { window.Close(); }
    }

    private static bool VisibleBrush(Brush? brush) => brush is not null && brush.Opacity > 0
        && (brush is not SolidColorBrush solid || solid.Color.A > 0);

    private sealed class UnavailableCommand : ICommand
    {
        public bool CanExecute(object? parameter) => false;
        public void Execute(object? parameter) => throw new InvalidOperationException("Disabled action invoked");
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }

    private static void TraceReview(string stage)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_TOUCH_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        File.AppendAllText(Path.Combine(directory, "review-layout.txt"), stage + Environment.NewLine);
    }

    private static void SetHover(UIElement element, bool value) => element.SetValue(
        (DependencyPropertyKey)typeof(UIElement).GetField("IsMouseOverPropertyKey",
            BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!, value);

    private static void AssertGalleryHeight(InRibbonGallery gallery, double expected)
    {
        Assert.Equal(expected, gallery.Height);
        double pixel = 1 / VisualTreeHelper.GetDpi(gallery).DpiScaleY;
        Assert.InRange(gallery.ActualHeight, expected - pixel, expected + pixel);
    }

    private static StackPanel Column(params UIElement[] controls)
    {
        var panel = new StackPanel();
        foreach (var control in controls) panel.Children.Add(control);
        return panel;
    }

    private static RibbonWindow Window(object content, double width, double height) => new()
    {
        Content = content, Width = width, Height = height, Left = -10000, Top = -10000,
        ShowActivated = false, ShowInTaskbar = false,
    };

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }

    private static void TouchDown(UIElement target) => target.RaiseEvent(
        new TouchEventArgs(new ConsumerTouchDevice(), Environment.TickCount)
        { RoutedEvent = UIElement.PreviewTouchDownEvent });

    // Exercises routing/dismissal only; physical touch and mouse promotion remain live gates.
    private sealed class ConsumerTouchDevice() : TouchDevice(1)
    {
        public override TouchPoint GetTouchPoint(IInputElement? relativeTo) =>
            new(this, new Point(), new Rect(0, 0, 1, 1), TouchAction.Down);
        public override TouchPointCollection GetIntermediateTouchPoints(IInputElement? relativeTo) =>
            new() { GetTouchPoint(relativeTo) };
    }

    private static void SavePreview(FrameworkElement ribbon, string name)
    {
        // Optional artifacts for visual inspection; never create or replace approvals.
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_TOUCH_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(ribbon.ActualWidth),
            (int)Math.Ceiling(ribbon.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        var background = new DrawingVisual();
        using (DrawingContext drawing = background.RenderOpen())
            drawing.DrawRectangle((Brush)ribbon.FindResource("RibbonKit.Brushes.Window.Background"),
                null, new Rect(0, 0, ribbon.ActualWidth, ribbon.ActualHeight));
        bitmap.Render(background);
        bitmap.Render(ribbon);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(file);
    }
}
