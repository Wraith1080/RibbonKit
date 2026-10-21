using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class DropDownGroupingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Native_combo_and_copies_show_groups_without_changing_selection_identity(bool editable) => Sta.Run(() =>
    {
        var choices = Choices();
        var view = View(choices);
        var combo = new RibbonComboBox { Header = "Font", ItemsSource = view,
            DisplayMemberPath = nameof(Choice.Name), IsEditable = editable, SelectedIndex = 2 };
        var owner = new Ribbon();
        var proxy = (RibbonDropDownButton)owner.CreateCommandProxy(combo, RibbonControlSize.Small);
        using var host = new Host(new StackPanel { Children = { combo, proxy } });
        combo.IsDropDownOpen = true; host.Layout();
        CheckGroups((FrameworkElement)((Popup)combo.Template.FindName("PART_Popup", combo)).Child);
        Assert.Same(choices[2], combo.SelectedItem);
        combo.IsDropDownOpen = false; host.Layout();
        proxy.IsDropDownOpen = true; host.Layout();
        CheckGroups((FrameworkElement)((Popup)proxy.Template.FindName("PART_Popup", proxy)).Child);
        Assert.Null(proxy.DropDownHeader);
        Assert.Equal(choices.Count, proxy.Items.Count);
        Assert.Same(choices[2], proxy.Items.OfType<RibbonComboBoxQuickAccessItem>().Single(row => row.IsSelected).SourceItem);
        Assert.All(Descendants<GroupItem>((DependencyObject)((Popup)proxy.Template.FindName("PART_Popup", proxy)).Child),
            group => Assert.False(group.Focusable));
        int changes = 0;
        combo.SelectionChanged += (_, _) => changes++;
        var pick = proxy.Items.OfType<RibbonComboBoxQuickAccessItem>().Single(row => ReferenceEquals(row.SourceItem, choices[3]));
        ((IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(pick)!.GetPattern(PatternInterface.Invoke)!).Invoke();
        host.Layout();
        Assert.Same(choices[3], combo.SelectedItem);
        Assert.Equal(1, changes);
        Assert.False(proxy.IsDropDownOpen);
        Assert.Same(view, combo.ItemsSource);
    });

    [Fact]
    public void Open_copy_tracks_grouping_item_changes_and_return_to_flat_mode() => Sta.Run(() =>
    {
        var choices = Choices();
        var view = View(choices);
        var combo = new RibbonComboBox { Header = "Font", ItemsSource = view, DisplayMemberPath = nameof(Choice.Name) };
        var proxy = (RibbonDropDownButton)new Ribbon().CreateCommandProxy(combo, RibbonControlSize.Small);
        using var host = new Host(proxy);
        proxy.IsDropDownOpen = true; host.Layout();
        var root = (FrameworkElement)((Popup)proxy.Template.FindName("PART_Popup", proxy)).Child;
        CheckGroups(root);
        choices.Add(new Choice("Georgia", "Other fonts")); host.Layout();
        Assert.Equal(5, proxy.Items.Count);
        choices.RemoveAt(0); host.Layout(); Assert.Equal(4, proxy.Items.Count);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Choice.Name))); host.Layout();
        Assert.Equal(6, Descendants<GroupItem>(root).Count());
        CheckRowSpacing(root);
        view.GroupDescriptions.Clear(); host.Layout();
        Assert.Empty(Descendants<GroupItem>(root));
        Assert.Equal("Font", proxy.DropDownHeader);
        var heading = (Border)proxy.Template.FindName("DropDownHeaderHost", proxy);
        Assert.Equal(new Thickness(0, 0, 0, 2), heading.Margin);
        CheckRowSpacing(root);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Choice.Section))); host.Layout();
        CheckGroups(root);
        Assert.Null(proxy.DropDownHeader);
    });

    [Fact]
    public void Group_sort_order_and_multiple_membership_keep_distinct_source_indices() => Sta.Run(() =>
    {
        var choices = Choices();
        var view = new ListCollectionView(choices);
        var description = new PropertyGroupDescription(nameof(Choice.Section), new MultipleGroups());
        description.SortDescriptions.Add(new SortDescription(nameof(CollectionViewGroup.Name), ListSortDirection.Descending));
        view.GroupDescriptions.Add(description);
        var combo = new RibbonComboBox { Header = "Font", ItemsSource = view, DisplayMemberPath = nameof(Choice.Name) };
        var proxy = (RibbonDropDownButton)new Ribbon().CreateCommandProxy(combo, RibbonControlSize.Small);
        using var host = new Host(proxy);
        proxy.IsDropDownOpen = true; host.Layout();
        Assert.Equal(combo.Items.Count, proxy.Items.Count);
        Assert.Equal(new[] { "Shared", "Other fonts", "Common fonts" },
            proxy.Items.Groups!.Cast<CollectionViewGroup>().Select(group => group.Name));
        var rows = proxy.Items.OfType<RibbonComboBoxQuickAccessItem>().ToArray();
        Assert.Equal(8, rows.Length);
        Assert.Equal(combo.Items.Cast<object>(), rows.Select(row => row.SourceItem));
        ((IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(rows[^1])!.GetPattern(PatternInterface.Invoke)!).Invoke();
        host.Layout();
        Assert.Equal(rows[^1].SourceIndex, combo.SelectedIndex);
        Assert.Same(rows[^1].SourceItem, combo.SelectedItem);
    });

    private sealed class MultipleGroups : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            new[] { (string)value, "Shared" };
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    }

    [Fact]
    public void Showcase_font_sample_supplies_grouped_choices_without_loading_user_preferences() => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(showcaseResources: true), RibbonTheme.Office2024);
        var window = new RibbonKit.Showcase.MainWindow();
        try
        {
            var combo = Assert.IsType<RibbonComboBox>(window.FindName("FontFamilyCombo"));
            Assert.True(combo.IsEditable);
            Assert.Equal("Calibri", combo.Text);
            Assert.Equal(new[] { "Common fonts", "Other fonts" },
                combo.Items.Groups!.Cast<CollectionViewGroup>().Select(group => group.Name));
            Assert.Equal(6, combo.Items.Count);
            Assert.Equal("Name", combo.DisplayMemberPath);
        }
        finally { window.Close(); Sta.ResetApplication(); }
    });

    [Fact]
    public void Authored_group_style_keeps_precedence_in_native_combo_and_copy() => Sta.Run(() =>
    {
        var combo = new RibbonComboBox { Header = "Font", ItemsSource = View(Choices()) };
        var style = new GroupStyle { ContainerStyle = new Style(typeof(GroupItem)) };
        style.ContainerStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(12)));
        combo.GroupStyle.Add(style);
        var proxy = (RibbonDropDownButton)new Ribbon().CreateCommandProxy(combo, RibbonControlSize.Small);
        using var host = new Host(new StackPanel { Children = { combo, proxy } });
        Assert.Same(style, Assert.Single(combo.GroupStyle));
        proxy.IsDropDownOpen = true; host.Layout();
        Assert.Same(style, Assert.Single(proxy.GroupStyle));
    });

    [Fact]
    public void Ordinary_dropdown_supports_native_grouped_item_templates() => Sta.Run(() =>
    {
        var row = new FrameworkElementFactory(typeof(RibbonMenuItem));
        row.SetBinding(RibbonMenuItem.HeaderProperty, new Binding(nameof(Choice.Name)));
        var dropdown = new RibbonDropDownButton { Header = "Fonts", ItemsSource = View(Choices()),
            ItemTemplate = new DataTemplate { VisualTree = row } };
        using var host = new Host(dropdown);
        dropdown.IsDropDownOpen = true; host.Layout();
        CheckGroups((FrameworkElement)((Popup)dropdown.Template.FindName("PART_Popup", dropdown)).Child);
    });

    [Theory]
    [InlineData(false, RibbonDensity.Compact)]
    [InlineData(true, RibbonDensity.Compact)]
    [InlineData(false, RibbonDensity.Touch)]
    [InlineData(true, RibbonDensity.Touch)]
    public void Flat_menus_space_visible_rows_and_caption_without_outer_gaps(bool split, RibbonDensity density) => Sta.Run(() =>
    {
        RibbonDropDownButton menu = split ? new RibbonSplitButton() : new RibbonDropDownButton();
        menu.DropDownHeader = "Fonts";
        Ribbon.SetDensity(menu, density);
        var first = new RibbonMenuItem { Header = "Calibri" };
        var middle = new RibbonMenuItem { Header = "Segoe UI" };
        var last = new RibbonMenuItem { Header = "Arial" };
        menu.Items.Add(first); menu.Items.Add(middle); menu.Items.Add(last);
        using var host = new Host(menu);
        menu.IsDropDownOpen = true; host.Layout();
        var root = (FrameworkElement)((Popup)menu.Template.FindName("PART_Popup", menu)).Child;
        var heading = (Border)menu.Template.FindName("DropDownHeaderHost", menu);
        CheckRowSpacing(root);
        Assert.Equal(2, first.TranslatePoint(new Point(), root).Y - heading.TranslatePoint(new Point(0, heading.ActualHeight), root).Y, 6);
        middle.Visibility = Visibility.Collapsed; last.Visibility = Visibility.Collapsed; host.Layout();
        CheckRowSpacing(root);
        menu.Items.Remove(first); host.Layout();
        Assert.Equal(0, Descendants<Panel>(root).First(panel => panel.IsItemsHost).ActualHeight);
    });

    [Fact]
    public void Empty_last_group_has_no_trailing_spacing() => Sta.Run(() =>
    {
        var view = View(Choices());
        view.GroupDescriptions[0].GroupNames.Add("Common fonts");
        view.GroupDescriptions[0].GroupNames.Add("Other fonts");
        view.GroupDescriptions[0].GroupNames.Add("No fonts");
        var combo = new RibbonComboBox { ItemsSource = view, DisplayMemberPath = nameof(Choice.Name) };
        using var host = new Host(combo);
        combo.IsDropDownOpen = true; host.Layout();
        var root = (FrameworkElement)((Popup)combo.Template.FindName("PART_Popup", combo)).Child;
        CheckRowSpacing(root);
    });

    private static ObservableCollection<Choice> Choices() => new()
    {
        new("Calibri", "Common fonts"), new("Segoe UI", "Common fonts"),
        new("Arial", "Other fonts"), new("Consolas", "Other fonts"),
    };
    private static ListCollectionView View(ObservableCollection<Choice> choices)
    {
        var view = new ListCollectionView(choices);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Choice.Section)));
        return view;
    }
    private static void CheckGroups(FrameworkElement root)
    {
        var groups = Descendants<GroupItem>(root).ToArray();
        Assert.Equal(new[] { "Common fonts", "Other fonts" }, groups.Select(group => ((CollectionViewGroup)group.Content).Name));
        Assert.Equal(new Thickness(0), groups[0].Margin);
        Assert.Equal(new Thickness(0), groups[1].Margin);
        foreach (var group in groups)
        {
            var header = (Border)group.Template.FindName("SectionHeader", group);
            Assert.Equal(new Thickness(0), header.Margin);
            Assert.False(header.IsHitTestVisible);
        }
        CheckRowSpacing(root);
    }
    private static void CheckRowSpacing(FrameworkElement root)
    {
        var rows = Descendants<FrameworkElement>(root).Where(row => row.Visibility != Visibility.Collapsed &&
            (row is ComboBoxItem or RibbonComboBoxQuickAccessItem or RibbonMenuItem || row is Border { Name: "SectionHeader" })).ToArray();
        var panel = Descendants<Panel>(root).First(item => item.IsItemsHost);
        Assert.Equal(0, rows[0].TranslatePoint(new Point(), panel).Y, 6);
        for (int index = 1; index < rows.Length; index++)
            Assert.Equal(2, rows[index].TranslatePoint(new Point(), root).Y -
                rows[index - 1].TranslatePoint(new Point(0, rows[index - 1].ActualHeight), root).Y, 6);
        Assert.Equal(panel.ActualHeight, rows[^1].TranslatePoint(new Point(0, rows[^1].ActualHeight), panel).Y, 6);
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T item) yield return item;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private sealed record Choice(string Name, string Section);
    private sealed class Host : IDisposable
    {
        private readonly RibbonAnimationLevel _motion = RibbonAnimation.GlobalLevel;
        private readonly Window _window;
        internal Host(FrameworkElement content)
        {
            ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024);
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            _window = new Window { Content = content, Width = 650, Height = 400,
                Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            _window.Show(); Layout();
        }
        internal void Layout() { Sta.Drain(DispatcherPriority.ApplicationIdle); _window.UpdateLayout(); Sta.Drain(DispatcherPriority.ApplicationIdle); }
        public void Dispose() { _window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = _motion; Sta.ResetApplication(); }
    }
}
