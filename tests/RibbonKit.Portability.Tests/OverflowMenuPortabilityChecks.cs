using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Public controls/resources only, including both actual overflow hosts.
internal static class OverflowMenuPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var action = new RibbonButton { Header = "Save", Icon = new DrawingImage() };
        var toggle = new RibbonToggleButton { Header = "Bold", Icon = new DrawingImage() };
        var drop = new RibbonDropDownButton { Header = "Select", Icon = new DrawingImage() };
        var split = new RibbonSplitButton { Header = "Paste", Icon = new DrawingImage() };
        drop.Items.Add(new RibbonMenuItem { Header = "Select all" }); split.Items.Add(new RibbonMenuItem { Header = "Paste special" });
        var combo = new RibbonComboBox { Header = "Size", ItemsSource = new[] { "11", "12" }, SelectedIndex = 0 };
        var gallery = new RibbonGallery { Header = "Shapes", Icon = new DrawingImage(), Width = 120 };
        var strip = new InRibbonGallery { Header = "Styles", Icon = new DrawingImage(), Width = 120 };
        foreach (var source in new RibbonGallery[] { gallery, strip })
            source.Items.Add(new RibbonGalleryItem { Content = new Border { Width = 48, Height = 24, Background = Brushes.Blue } });
        var group = new RibbonGroup { Header = "Editing" };
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TabRow, QuickAccessMaxWidth = 36 };
        ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        foreach (var source in new FrameworkElement[] { action, toggle, drop, split, combo, gallery, strip })
        { group.Items.Add(source); Ribbon.SetSimplifiedPlacement(source, RibbonSimplifiedPlacement.Inline); ribbon.AddToQuickAccess(source); }
        ribbon.AddToQuickAccess(group);
        var nativeCombo = new RibbonComboBox { ItemsSource = new[] { "11", "12" }, SelectedIndex = 0 };
        var window = new RibbonWindow { Content = new StackPanel { Children = { ribbon, nativeCombo } }, Width = 1000, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        string context = "startup";
        var events = new Queue<string>();
        void Trace(string reason)
        {
            if (events.Count == 12) events.Dequeue();
            events.Enqueue($"{context}: {reason}, windowActive={window.IsActive}");
        }
        window.Deactivated += (_, _) => Trace("window deactivated");
        window.LocationChanged += (_, _) => Trace("window moved");
        window.SizeChanged += (_, e) => Trace($"window resized {e.PreviousSize}->{e.NewSize}");
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024); window.Show(); window.Activate(); Layout(window);
            foreach (var theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (var density in Enum.GetValues<RibbonDensity>())
            foreach (var direction in Enum.GetValues<FlowDirection>())
            {
                VisualTreeHelper.SetRootDpi(window, new DpiScale(dark ? 1.5 : 1, dark ? 1.5 : 1));
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                ribbon.Density = density; Ribbon.SetDensity(nativeCombo, density); window.FlowDirection = direction; Layout(window);
                double minimum = density == RibbonDensity.Touch ? 44 : 32;
                nativeCombo.IsDropDownOpen = true; Layout(window);
                CheckRow((FrameworkElement)nativeCombo.ItemContainerGenerator.ContainerFromIndex(0), minimum);
                nativeCombo.IsDropDownOpen = false; Layout(window);
                foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
                {
                    ribbon.LayoutMode = mode; Layout(window);
                    var host = (RibbonGroupsHost)tab.Content;
                    var toolbar = mode == RibbonLayoutMode.Simplified
                        ? Descendants(host).OfType<RibbonQuickAccessToolBar>().Single()
                        : Descendants(ribbon).OfType<RibbonQuickAccessToolBar>().Single(item => item.IsVisible && !host.IsAncestorOf(item));
                    if (mode == RibbonLayoutMode.Simplified)
                    {
                        toolbar.Width = 36; Layout(window);
                        Assert.Equal(new Thickness(0, 3, 0, 3), toolbar.Margin);
                        double expected = host.ActualHeight - 6;
                        double pixel = 1 / VisualTreeHelper.GetDpi(toolbar).DpiScaleY;
                        Assert.InRange(toolbar.ActualHeight, expected - pixel, expected + pixel);
                    }
                    var more = (ToggleButton)toolbar.Template.FindName("PART_OverflowButton", toolbar);
                    Assert.True(more.Focus());
                    more.IsChecked = true; Layout(window);
                    var root = (Popup)toolbar.Template.FindName("PART_OverflowPopup", toolbar);
                    Assert.True(root.IsOpen);
                    var entries = (ItemsControl)toolbar.Template.FindName("PART_OverflowHost", toolbar);
                    var initial = entries.Items.OfType<FrameworkElement>().First(entry => entry.IsEnabled);
                    Assert.True(initial.IsKeyboardFocusWithin, $"{theme}/{dark}/{density}/{direction}/{mode}: initial={initial}, visible={initial.IsVisible}, focusable={initial.Focusable}, focused={Keyboard.FocusedElement}");
                    foreach (var row in entries.Items.OfType<FrameworkElement>()) CheckRow(row, minimum);
                    foreach (string header in new[] { "Select", "Paste", "Size", "Shapes", "Styles", "Editing" })
                    {
                        context = $"{theme}/{dark}/{density}/{direction}/{mode}/{header}";
                        var entry = entries.Items.OfType<RibbonDropDownButton>().Single(item => item.Header == header);
                        entry.IsDropDownOpen = true; Layout(window);
                        var popup = (Popup)(header == "Editing" ? group.Template.FindName("PART_Popup", group) : entry.Template.FindName("PART_Popup", entry));
                        Assert.True(popup.IsOpen, $"{theme}, dark={dark}, {density}, {direction}, {mode}, {header}");
                        Assert.Equal(PlacementMode.Custom, popup.Placement); Assert.Same(entry, popup.PlacementTarget);
                        var parentChrome = (FrameworkElement)((AdornerDecorator)root.Child).Child;
                        Rect parentRect = Bounds(parentChrome), childRect = Bounds(popup.Child is AdornerDecorator decorator
                            ? (FrameworkElement)decorator.Child : (FrameworkElement)popup.Child);
                        Assert.True(Math.Abs(childRect.Left - parentRect.Right) <= 1 || Math.Abs(parentRect.Left - childRect.Right) <= 1,
                            $"Detached {header}: {theme}, {direction}, {mode}, parent={parentRect}, child={childRect}");
                        var arrow = header == "Paste"
                            ? (System.Windows.Shapes.Path)((ToggleButton)entry.Template.FindName("PART_Toggle", entry)).Template.FindName("ToggleChevron", (ToggleButton)entry.Template.FindName("PART_Toggle", entry))
                            : (System.Windows.Shapes.Path)entry.Template.FindName("OverflowChevron", entry);
                        Assert.True(arrow.IsVisible); Assert.True(arrow.Data.Bounds.Height > arrow.Data.Bounds.Width);
                        Assert.Same(ribbon.FindResource("RibbonKit.Brushes.Text.Secondary"), arrow.Stroke);
                        if (header == "Size") CheckRow((FrameworkElement)entry.Items[0], minimum);
                        if (header is "Shapes" or "Styles")
                            Assert.IsType<ItemsPresenter>(Assert.IsAssignableFrom<ScrollViewer>(Assert.Single(entry.Items)).Content);
                        if (header is "Select" or "Paste") CheckRow((FrameworkElement)entry.Items[0], minimum);
                        entry.IsDropDownOpen = false; Layout(window);
                        Assert.True(root.IsOpen, $"{context}: parent overflow closed after child close; currentPart={ReferenceEquals(root, toolbar.Template.FindName("PART_OverflowPopup", toolbar))}, checked={more.IsChecked}, hasOverflow={toolbar.HasOverflow}, focused={Keyboard.FocusedElement}. Events: {string.Join(" | ", events)}");
                    }
                    Assert.Single(drop.Items); Assert.Single(split.Items);
                    Assert.Equal(0, combo.SelectedIndex); Assert.Single(gallery.Items); Assert.Single(strip.Items);
                    var native = (Popup)group.Template.FindName("PART_Popup", group);
                    Assert.Equal(PlacementMode.Bottom, native.Placement); Assert.Null(native.CustomPopupPlacementCallback);
                    var focused = (UIElement)Keyboard.FocusedElement;
                    InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(focused)!, Environment.TickCount, Key.Escape)
                        { RoutedEvent = Keyboard.PreviewKeyDownEvent });
                    Layout(window); Assert.False(root.IsOpen); Assert.True(more.IsKeyboardFocused);
                }
            }
        }
        finally { window.Close(); Layout(window); RibbonAnimation.GlobalLevel = motion; ThemeManager.SetDarkMode(application, false); }
    }

    private static void CheckRow(FrameworkElement row, double minimum)
    {
        double pixel = 1 / VisualTreeHelper.GetDpi(row).DpiScaleY;
        Assert.InRange(row.ActualHeight, minimum - pixel, double.PositiveInfinity);
        Assert.InRange(row.ActualWidth, 200 - pixel, double.PositiveInfinity);
    }
    private static Rect Bounds(FrameworkElement element) => new(element.PointToScreen(new Point()),
        element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight)));
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        { var child = VisualTreeHelper.GetChild(parent, i); yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
    private static void Layout(Window window)
    { window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout(); window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); }
}
