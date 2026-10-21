using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// A consumer with only RibbonKit references; grouping is ordinary WPF data/view configuration.
internal static class GroupedComboBoxChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var choices = new ObservableCollection<FontChoice>
        {
            new("Calibri", "Common fonts"), new("Segoe UI", "Common fonts"),
            new("Arial", "Other fonts"), new("Times New Roman", "Other fonts"),
            new("Consolas", "Other fonts"), new("Georgia", "Other fonts"),
        };
        var view = new ListCollectionView(choices);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(FontChoice.Section)));
        var combo = new RibbonComboBox { Header = "Font", IsEditable = true, ItemsSource = view,
            DisplayMemberPath = nameof(FontChoice.Name), MaxDropDownHeight = 150, SelectedIndex = 0 };
        var group = new RibbonGroup { Header = "Font", CanResize = false }; group.Items.Add(combo);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TabRow };
        ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ribbon.AddToQuickAccess(combo);
        var qat = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
        var customGroup = new RibbonGroup { Header = "Custom", CanResize = false };
        Ribbon.SetIsCustom(customGroup, true); tab.Groups.Add(customGroup);
        var page = new RibbonCustomizePage { Ribbon = ribbon };
        var setup = new StackPanel { Children = { ribbon, page } };
        var window = new RibbonWindow { Content = setup, Width = 950, Height = 500,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024); window.Show(); Layout();
            var available = Part<ListBox>(page, "PART_AvailableList");
            available.SelectedItem = available.Items.Cast<RibbonCommandEntry>().Single(item => ReferenceEquals(item.Control, combo));
            var home = Assert.IsType<RibbonCustomizeNode>(Part<TreeView>(page, "PART_Tree").Items[0]);
            home.Children.Single(node => node.IsCustom).IsSelected = true; Layout();
            Part<ButtonBase>(page, "PART_AddButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout();
            var custom = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(customGroup.Items));
            setup.Children.Remove(ribbon); window.Content = ribbon; Layout();
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            foreach (FlowDirection direction in Enum.GetValues<FlowDirection>())
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                ribbon.Density = density; window.FlowDirection = direction;
                double scale = density == RibbonDensity.Touch ? 1.5 : 1;
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale)); Layout();
                foreach (Control control in new Control[] { combo, qat, custom })
                {
                    Open(control, true); Layout();
                    var popup = Part<Popup>(control, "PART_Popup");
                    var root = Assert.IsAssignableFrom<FrameworkElement>(popup.Child);
                    var groups = Descendants<GroupItem>(root).ToArray();
                    Assert.Equal(new[] { "Common fonts", "Other fonts" },
                        groups.Select(item => ((CollectionViewGroup)item.Content).Name));
                    Assert.Equal(new Thickness(0), groups[0].Margin);
                    Assert.Equal(new Thickness(0), groups[1].Margin);
                    var header = Part<Border>(groups[0], "SectionHeader");
                    Assert.Equal(new Thickness(0), header.Margin);
                    Assert.False(header.IsHitTestVisible); Assert.False(groups[0].Focusable);
                    Assert.Same(control.FindResource("RibbonKit.Brushes.ApplicationMenu.HeaderBackground"), header.Background);
                    var text = Assert.Single(Descendants<TextBlock>(header));
                    Assert.Equal(direction, text.FlowDirection);
                    Assert.Same(control.FindResource("RibbonKit.Brushes.ApplicationMenu.Foreground"), text.Foreground);
                    var rows = Descendants<FrameworkElement>(root).Where(row =>
                        row is ComboBoxItem or Button || row is Border { Name: "SectionHeader" }).ToArray();
                    var panel = Descendants<Panel>(root).First(item => item.IsItemsHost);
                    double tolerance = 1 / scale + 0.01;
                    Assert.InRange(Math.Abs(rows[0].TranslatePoint(new Point(), panel).Y), 0, tolerance);
                    for (int index = 1; index < rows.Length; index++)
                    {
                        double gap = rows[index].TranslatePoint(new Point(), root).Y -
                            rows[index - 1].TranslatePoint(new Point(0, rows[index - 1].ActualHeight), root).Y;
                        if (Math.Abs(gap - 2) > tolerance)
                        {
                            Save(root, "spacing-failure");
                            Assert.Fail($"{theme}/{dark}/{density}/{direction}/{control.GetType().Name}: gap {gap} between {rows[index - 1].GetType().Name} ({rows[index - 1].ActualHeight}/{rows[index - 1].DesiredSize.Height}, dpi {VisualTreeHelper.GetDpi(rows[index - 1]).DpiScaleY}) and {rows[index].GetType().Name} ({rows[index].ActualHeight}/{rows[index].DesiredSize.Height}, dpi {VisualTreeHelper.GetDpi(rows[index]).DpiScaleY}); popup dpi {VisualTreeHelper.GetDpi(root).DpiScaleY}");
                        }
                    }
                    Assert.InRange(Math.Abs(panel.ActualHeight - rows[^1].TranslatePoint(new Point(0, rows[^1].ActualHeight), panel).Y), 0, tolerance);
                    var scroll = Assert.Single(Descendants<ScrollViewer>(root));
                    Assert.True(scroll.ScrollableHeight > 0);
                    Assert.Single(Descendants<ScrollBar>(root), bar => bar.IsVisible && bar.Orientation == Orientation.Vertical);
                    scroll.ScrollToTop(); Layout();
                    double before = header.TranslatePoint(new Point(), root).Y;
                    scroll.ScrollToEnd(); Layout();
                    Assert.True(header.TranslatePoint(new Point(), root).Y < before);
                    if (direction == FlowDirection.LeftToRight && ReferenceEquals(control, qat))
                    {
                        double height = combo.MaxDropDownHeight;
                        combo.MaxDropDownHeight = 1000;
                        scroll.ScrollToTop(); Layout();
                        Save(root, $"{theme}-{(dark ? "dark" : "light")}-{density}");
                        combo.MaxDropDownHeight = height; Layout();
                    }
                    if (control is RibbonDropDownButton copy)
                    {
                        Assert.Null(copy.DropDownHeader);
                        var last = copy.Items.OfType<Button>().Single(item => ReferenceEquals(item.Content, choices[^1]));
                        ((IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(last)!.GetPattern(PatternInterface.Invoke)!).Invoke();
                        Layout(); Assert.Same(choices[^1], combo.SelectedItem); Assert.False(copy.IsDropDownOpen);
                        Assert.Same(view, combo.ItemsSource);
                    }
                    else Open(control, false);
                    Layout();
                }
            }
            // Open-copy updates from a native collection view do not need host refresh code.
            qat.IsDropDownOpen = true; Layout(); choices.Add(new("Aptos", "Other fonts")); Layout();
            Assert.Equal(7, qat.Items.Count);
            view.GroupDescriptions.Clear(); Layout(); Assert.Equal("Font", qat.DropDownHeader);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(FontChoice.Section))); Layout();
            Assert.Null(qat.DropDownHeader);

            void Layout()
            {
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            }
        }
        finally { window.Close(); RibbonAnimation.GlobalLevel = motion; ThemeManager.SetDarkMode(application, false); }
    }

    private static void Open(Control control, bool open)
    {
        if (control is ComboBox combo) combo.IsDropDownOpen = open;
        else ((RibbonDropDownButton)control).IsDropDownOpen = open;
    }
    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T item) yield return item;
            foreach (var item2 in Descendants<T>(child)) yield return item2;
        }
    }
    private static void Save(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_GROUPED_COMBO_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var dpi = VisualTreeHelper.GetDpi(element);
        // Popup children retain their outer margin as a visual offset. Include
        // that allocation so the rounded frame and shadow are not cropped.
        Thickness margin = element.Margin;
        var image = new RenderTargetBitmap((int)Math.Ceiling((element.ActualWidth + margin.Left + margin.Right) * dpi.DpiScaleX),
            (int)Math.Ceiling((element.ActualHeight + margin.Top + margin.Bottom) * dpi.DpiScaleY),
            dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        image.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
    private sealed record FontChoice(string Name, string Section);
}
