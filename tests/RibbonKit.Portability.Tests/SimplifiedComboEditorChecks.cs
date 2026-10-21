using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
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

internal static class SimplifiedComboEditorChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var choices = new ObservableCollection<FontChoice> { new("Calibri", "Common fonts"), new("Segoe UI", "Common fonts"), new("Arial", "Other fonts") };
        var view = new ListCollectionView(choices);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(FontChoice.Section)));
        var source = new RibbonComboBox { Header = "Font", ItemsSource = view, DisplayMemberPath = nameof(FontChoice.Name),
            IsEditable = true, InputWidth = 150, SelectedIndex = 0 };
        var size = new RibbonComboBox { Header = "Size", InputWidth = 48, SelectedIndex = 0 };
        var authored = new ComboBoxItem { Content = "12" };
        size.Items.Add(new ComboBoxItem { Content = "11" }); size.Items.Add(authored); size.Items.Add(new ComboBoxItem { Content = "12" });
        Ribbon.SetCommandId(source, "font.family"); Ribbon.SetCommandId(size, "font.size");
        Ribbon.SetSimplifiedPlacement(source, RibbonSimplifiedPlacement.Inline); Ribbon.SetSimplifiedPlacement(size, RibbonSimplifiedPlacement.Inline);
        var group = new RibbonGroup { Header = "Font", CanResize = false }; group.Items.Add(source); group.Items.Add(size);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TabRow }; ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ribbon.AddToQuickAccess(source);
        var qat = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
        var customGroup = new RibbonGroup { Header = "Custom", CanResize = false }; Ribbon.SetIsCustom(customGroup, true); tab.Groups.Add(customGroup);
        var page = new RibbonCustomizePage { Ribbon = ribbon };
        var setup = new StackPanel { Children = { ribbon, page } };
        var window = new RibbonWindow { Content = setup, Width = 1100, Height = 450,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024); window.Show(); Layout();
            var available = Part<ListBox>(page, "PART_AvailableList");
            available.SelectedItem = available.Items.Cast<RibbonCommandEntry>().Single(item => ReferenceEquals(item.Control, source));
            var home = Assert.IsType<RibbonCustomizeNode>(Part<TreeView>(page, "PART_Tree").Items[0]);
            home.Children.Single(node => node.IsCustom).IsSelected = true; Layout();
            Part<ButtonBase>(page, "PART_AddButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout();
            var custom = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(customGroup.Items));
            Ribbon.SetSimplifiedPlacement(custom, RibbonSimplifiedPlacement.Inline);
            setup.Children.Remove(ribbon); window.Content = ribbon; Layout();

            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            foreach (FlowDirection direction in Enum.GetValues<FlowDirection>())
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                ribbon.Density = density; window.FlowDirection = direction;
                VisualTreeHelper.SetRootDpi(window, new DpiScale(density == RibbonDensity.Touch ? 1.5 : 1, density == RibbonDensity.Touch ? 1.5 : 1));
                ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout();
                var row = Descendants<RibbonQuickAccessToolBar>((DependencyObject)tab.Content).Single();
                var editor = row.Items.OfType<RibbonComboBox>().Single(item => item.Header == source.Header);
                var sizeEditor = row.Items.OfType<RibbonComboBox>().Single(item => item.Header == size.Header);
                Assert.Same(source, Ribbon.GetQuickAccessSource(editor)); Assert.True(editor.IsEditable); Assert.False(sizeEditor.IsEditable);
                Assert.Equal(150, editor.InputWidth); Assert.Equal(48, sizeEditor.InputWidth);
                Assert.Equal(source.Text, editor.Text); Assert.Same(size, authored.Parent);
                Assert.Contains(row.Items.OfType<RibbonDropDownButton>(), item => item.Header == source.Header && ReferenceEquals(Ribbon.GetQuickAccessSource(item), source));
                var input = Part<TextBox>(editor, "PART_EditableTextBox");
                Assert.True(input.IsVisible); Assert.True(input.ActualHeight > 0); Assert.True(editor.ActualHeight <= row.ActualHeight + 1);
                Assert.Same(editor.FindResource("RibbonKit.Brushes.Input.SurfaceBackground"), Part<Border>(editor, "Chrome").Background);
                editor.IsDropDownOpen = true; Layout();
                Assert.Equal(2, Descendants<GroupItem>(Part<Popup>(editor, "PART_Popup").Child).Count());
                editor.SelectedIndex = 2; editor.IsDropDownOpen = false; Layout(); Assert.Same(choices[2], source.SelectedItem);
                input.Text = "Custom font"; Layout(); Assert.Equal("Custom font", source.Text);
                source.SelectedIndex = 0; Layout(); Assert.Equal("Calibri", editor.Text);
                sizeEditor.SelectedIndex = 2; Layout(); Assert.Equal(2, size.SelectedIndex); Assert.Equal("12", size.Text);
                qat.IsDropDownOpen = true; Layout(); Assert.Equal(3, qat.Items.Count); qat.IsDropDownOpen = false;
                if (direction == FlowDirection.LeftToRight) Save(ribbon, $"{theme}-{(dark ? "dark" : "light")}-{density}");
                editor.IsDropDownOpen = true; Layout(); row.Width = 60; Layout();
                Assert.False(editor.IsDropDownOpen); Assert.False(editor.Focusable);
                Part<ToggleButton>(row, "PART_OverflowButton").IsChecked = true; Layout();
                var overflow = Part<ItemsControl>(row, "PART_OverflowHost");
                Assert.Contains(overflow.Items.OfType<RibbonDropDownButton>(), item => item.Header == source.Header);
                Part<ToggleButton>(row, "PART_OverflowButton").IsChecked = false; row.ClearValue(FrameworkElement.WidthProperty); Layout();
                ribbon.LayoutMode = RibbonLayoutMode.Classic; Layout();
                Assert.Same(view, source.ItemsSource); Assert.Equal("Calibri", source.Text); Assert.Same(size, authored.Parent);
                Assert.Equal(3, size.Items.Count);
            }
            void Layout()
            {
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
            }
        }
        finally { window.Close(); RibbonAnimation.GlobalLevel = motion; ThemeManager.SetDarkMode(application, false); }
    }
    private static T Part<T>(Control owner, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T item) yield return item;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Save(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_SIMPLIFIED_COMBO_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var dpi = VisualTreeHelper.GetDpi(element);
        var image = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX), (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        image.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
    private sealed record FontChoice(string Name, string Section);
}
