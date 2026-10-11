using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Localization;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

internal static class QuickAccessVisibilityChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try
        {
            foreach (var theme in new[] { RibbonTheme.Office2024, RibbonTheme.Office2010, RibbonTheme.CrystalLight })
            foreach (var flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
                VerifyWindow(application, theme, flow);
        }
        finally { RibbonAnimation.GlobalLevel = motion; ThemeManager.Apply(application, RibbonTheme.Office2024); }
    }

    private static void VerifyWindow(Application application, RibbonTheme theme, FlowDirection flow)
    {
        ThemeManager.Apply(application, theme);
        var icon = new DrawingImage(new GeometryDrawing(Brushes.SteelBlue, null, Geometry.Parse("M3,2 L13,2 L13,14 L3,14 Z")));
        var ribbon = new Ribbon { QuickAccessMaxWidth = 120, Density = RibbonDensity.Touch };
        var source = new RibbonDropDownButton { Header = "Actions", Icon = icon };
        var menuItem = new RibbonMenuItem { Header = "Action" }; source.Items.Add(menuItem);
        var group = new RibbonGroup { Header = "Editing" }; group.Items.Add(source);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
        ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        for (int i = 0; i < 7; i++) ribbon.QuickAccessItems.Add(new RibbonButton { Header = $"Command {i}", Icon = icon, Size = RibbonControlSize.Small });
        ribbon.AddToQuickAccess(source);
        var copy = Assert.IsType<RibbonDropDownButton>(ribbon.QuickAccessItems.Last());
        var originalCaption = new TextBlock { Text = "Host caption content" };
        var window = new RibbonWindow { Content = ribbon, TitleBarContent = originalCaption,
            Width = 950, Height = 400, Left = -10000, Top = -10000, FlowDirection = flow,
            ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout(window);
            var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
            var tabQat = Part<RibbonQuickAccessToolBar>(tabs, "QatTabRowHost");
            var below = Part<Border>(ribbon, "QatBelowHost");
            var belowItems = Part<ItemsControl>(ribbon, "QatBelowItems");
            var body = Part<Border>(tabs, "ContentHost");
            foreach (bool minimized in new[] { false, true })
            foreach (var position in Enum.GetValues<RibbonQuickAccessPosition>())
            {
                ribbon.IsMinimized = minimized; ribbon.QuickAccessPosition = position; Layout(window);
                var order = ribbon.QuickAccessItems.ToArray();
                double shownHeight = ribbon.ActualHeight;
                AssertVisible();
                var active = position == RibbonQuickAccessPosition.TitleBar
                    ? Assert.IsType<RibbonQuickAccessToolBar>(window.TitleBarContent)
                    : position == RibbonQuickAccessPosition.TabRow ? tabQat : null;
                if (active is not null)
                {
                    Assert.True(active.HasOverflow);
                    Part<ToggleButton>(active, "PART_OverflowButton").IsChecked = true; Layout(window);
                    Assert.True(Part<Popup>(active, "PART_OverflowPopup").IsOpen);
                }
                else { copy.IsDropDownOpen = true; Layout(window); Assert.Contains(menuItem, copy.Items.Cast<object>()); }

                var qatMenu = (ContextMenu)typeof(Ribbon).GetMethod("EnsureQatContextMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(ribbon, null)!;
                qatMenu.RaiseEvent(new RoutedEventArgs(ContextMenu.OpenedEvent));
                Click(qatMenu, RibbonString.HideQuickAccessToolbar); Layout(window);
                Assert.False(ribbon.IsQuickAccessVisible);
                Assert.Equal(position, ribbon.QuickAccessPosition); Assert.Equal(order, ribbon.QuickAccessItems.ToArray());
                Assert.Same(tab, ribbon.SelectedTab);
                Assert.Same(originalCaption, window.TitleBarContent);
                Assert.Equal(Visibility.Collapsed, tabQat.Visibility); Assert.Null(tabQat.ItemsSource);
                Assert.Equal(Visibility.Collapsed, below.Visibility); Assert.Null(belowItems.ItemsSource);
                if (active is not null) Assert.False(Part<Popup>(active, "PART_OverflowPopup").IsOpen);
                Assert.False(copy.IsDropDownOpen); Assert.Contains(menuItem, source.Items.Cast<object>());
                Assert.Equal((CornerRadius)ribbon.FindResource("RibbonKit.Metrics.ContentCornerRadius"), body.CornerRadius);
                if (position == RibbonQuickAccessPosition.BelowRibbon) Assert.True(ribbon.ActualHeight < shownHeight);
                AssertNoHiddenKeyTips(ribbon);

                // Raise the actual context-menu routed event on a tab header, which is still present.
                var restoredMenu = OpenBackgroundMenu(tab, window);
                Assert.Equal(flow, restoredMenu.FlowDirection);
                Click(restoredMenu, RibbonString.ShowQuickAccessToolbar); restoredMenu.IsOpen = false; Layout(window);
                Assert.True(ribbon.IsQuickAccessVisible); AssertVisible();
                Assert.Equal(order, ribbon.QuickAccessItems.ToArray());
                Assert.Equal(position, ribbon.QuickAccessPosition);
            }
            ribbon.IsQuickAccessVisible = false;
            var added = new RibbonButton { Header = "New command", Icon = icon }; group.Items.Add(added);
            Assert.True(ribbon.AddToQuickAccess(added)); Layout(window); Assert.True(ribbon.IsQuickAccessVisible);
            ribbon.IsQuickAccessVisible = false;
            var messages = new RibbonMessageBar(); messages.Items.Add(new RibbonMessage { Message = "Message", IsOpen = true });
            ribbon.MessageBar = messages; Layout(window);
            Assert.Equal((CornerRadius)ribbon.FindResource("RibbonKit.Metrics.ContentCornerRadiusTop"), body.CornerRadius);

            void AssertVisible()
            {
                var position = ribbon.QuickAccessPosition;
                Assert.Equal(position == RibbonQuickAccessPosition.TabRow ? Visibility.Visible : Visibility.Collapsed, tabQat.Visibility);
                Assert.Equal(position == RibbonQuickAccessPosition.BelowRibbon ? Visibility.Visible : Visibility.Collapsed, below.Visibility);
                if (position == RibbonQuickAccessPosition.TitleBar)
                    Assert.IsType<RibbonQuickAccessToolBar>(window.TitleBarContent);
            }
        }
        finally { window.Close(); Layout(window); }
    }

    private static ContextMenu OpenBackgroundMenu(UIElement source, Window window)
    {
        var constructor = typeof(ContextMenuEventArgs).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(ctor => ctor.GetParameters().Length == 2);
        var args = (ContextMenuEventArgs)constructor.Invoke(new object[] { source, true });
        source.RaiseEvent(args); Layout(window); Assert.True(args.Handled);
        return PresentationSource.CurrentSources.Cast<PresentationSource>().OfType<HwndSource>()
            .SelectMany(host => Descendants(host.RootVisual)).OfType<ContextMenu>().Single(menu => menu.IsOpen);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject? root)
    {
        if (root is null) yield break;
        yield return root;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Descendants(VisualTreeHelper.GetChild(root, i))) yield return child;
    }

    private static void AssertNoHiddenKeyTips(Ribbon ribbon)
    {
        var service = typeof(Ribbon).GetField("_keyTipService", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(ribbon)!;
        var level = service.GetType().GetMethod("BuildRootLevel", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, null)!;
        foreach (var item in (IEnumerable)level.GetType().GetProperty("Items")!.GetValue(level)!)
            Assert.DoesNotContain(item.GetType().GetProperty("Target")!.GetValue(item), ribbon.QuickAccessItems);
    }

    private static void Click(ContextMenu menu, RibbonString caption) => menu.Items.OfType<MenuItem>()
        .Single(item => Equals(item.Header, RibbonLocalization.GetString(caption))).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }
}
