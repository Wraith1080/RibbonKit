using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class ComboBoxQuickAccessTests
{
    [Fact]
    public void Authored_items_stay_parented_and_selection_commits_once_through_the_source() => Sta.Run(() =>
    {
        var combo = new RibbonComboBox { Header = "Size", IsEditable = true, SelectedIndex = 0 };
        var first = new ComboBoxItem { Content = "11" };
        var disabled = new ComboBoxItem { Content = "12", IsEnabled = false };
        var last = new ComboBoxItem { Content = new TextBlock { Text = "14" } };
        combo.Items.Add(first); combo.Items.Add(disabled); combo.Items.Add(last);
        var ribbon = RibbonFor(combo);
        Assert.True(ribbon.AddToQuickAccess(combo));
        Assert.False(ribbon.AddToQuickAccess(combo));
        var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
        using var host = Host(proxy);
        int changes = 0; combo.SelectionChanged += (_, _) => changes++;
        proxy.IsDropDownOpen = true; host.Layout();
        Assert.Equal(3, proxy.Items.Count);
        Assert.Same(combo, first.Parent); Assert.Same(combo, last.Parent);
        Assert.False(Assert.IsAssignableFrom<Button>(proxy.Items[1]).IsEnabled);
        Assert.Equal("14", Assert.IsAssignableFrom<Button>(proxy.Items[2]).Content);
        Invoke(Assert.IsAssignableFrom<Button>(proxy.Items[2])); host.Layout();
        Assert.Equal(1, changes); Assert.Same(last, combo.SelectedItem);
        Assert.Equal("14", combo.Text);
        Assert.False(proxy.IsDropDownOpen);
        Assert.Equal(3, combo.Items.Count);
        combo.SelectedIndex = 0;
        proxy.IsDropDownOpen = true; host.Layout();
        Assert.True(Assert.IsType<bool>(Assert.IsAssignableFrom<Button>(proxy.Items[0]).GetValue(RibbonComboBoxQuickAccessItem.IsSelectedProperty)));
        combo.IsEnabled = false; host.Layout(); Assert.False(proxy.IsEnabled);
    });

    [Fact]
    public void Bound_data_live_items_and_duplicate_values_keep_native_selection_bindings() => Sta.Run(() =>
    {
        var model = new SelectionModel();
        var combo = new RibbonComboBox { Header = "Mode", DisplayMemberPath = nameof(Choice.Name), DataContext = model };
        combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(model.Choices)));
        combo.SetBinding(Selector.SelectedItemProperty, new Binding(nameof(model.Selected)) { Mode = BindingMode.TwoWay });
        var ribbon = RibbonFor(combo); ribbon.AddToQuickAccess(combo);
        var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
        using var host = Host(proxy);
        proxy.IsDropDownOpen = true; host.Layout();
        Invoke(Assert.IsAssignableFrom<Button>(proxy.Items[1])); host.Layout();
        Assert.Same(model.Choices[1], model.Selected);
        Assert.True(BindingOperations.IsDataBound(combo, Selector.SelectedItemProperty));
        model.Selected = model.Choices[0];
        proxy.IsDropDownOpen = true; host.Layout();
        model.Choices.Add(new Choice("Third")); host.Layout(); Assert.Equal(3, proxy.Items.Count);
        model.Choices.RemoveAt(0); host.Layout(); Assert.Equal(2, proxy.Items.Count);
        Invoke(Assert.IsAssignableFrom<Button>(proxy.Items[1])); host.Layout();
        Assert.Same(model.Choices[1], model.Selected);

        var numbers = new RibbonComboBox { ItemsSource = new[] { 11, 12, 12 }, SelectedIndex = 0 };
        var numbersProxy = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.CreateCommandProxy(numbers, RibbonControlSize.Small));
        host.Window.Content = numbersProxy; host.Layout();
        numbersProxy.IsDropDownOpen = true; host.Layout();
        Invoke(Assert.IsAssignableFrom<Button>(numbersProxy.Items[2])); host.Layout();
        Assert.Equal(2, numbers.SelectedIndex);
    });

    [Fact]
    public void Combo_catalog_custom_group_and_saved_qat_resolve_the_fresh_source() => Sta.Run(() =>
    {
        var original = new RibbonComboBox { Header = "Size", Icon = Icon() };
        original.Items.Add("11"); original.Items.Add("12");
        Ribbon.SetCommandId(original, "command.size");
        var ribbon = RibbonFor(original);
        var entry = Assert.Single(RibbonCommandCatalog.CollectAvailable(ribbon));
        Assert.Same(original, entry.Control); Assert.Same(original.Icon, entry.Icon);
        ribbon.AddToQuickAccess(original);
        var custom = new RibbonGroup { Header = "Custom" }; Ribbon.SetIsCustom(custom, true);
        Ribbon.SetCommandId(custom, "custom:group"); ribbon.Tabs[0].Groups.Add(custom);
        var copy = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.CreateCommandProxy(original, RibbonControlSize.Medium));
        copy.Header = "My size"; custom.Items.Add(copy);
        Assert.Equal("My size", RibbonCommandCatalog.Describe(copy).DisplayName);
        string json = RibbonCustomizationSerializer.Serialize(ribbon);
        var fresh = new RibbonComboBox { Header = "Size", Icon = Icon() }; fresh.Items.Add("10"); fresh.Items.Add("14");
        Ribbon.SetCommandId(fresh, "command.size"); var restored = RibbonFor(fresh);
        RibbonCustomizationSerializer.Apply(restored, json);
        var qat = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(restored.QuickAccessItems));
        Assert.Same(fresh, Ribbon.GetQuickAccessSource(qat));
        var restoredCopy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(restored.Tabs[0].Groups[1].Items));
        Assert.Equal("My size", restoredCopy.Header); Assert.Same(fresh, Ribbon.GetQuickAccessSource(restoredCopy));
        using var host = Host(qat);
        qat.IsDropDownOpen = true; host.Layout(); Invoke(Assert.IsAssignableFrom<Button>(qat.Items[1])); host.Layout();
        Assert.Equal(1, fresh.SelectedIndex); Assert.Equal(-1, original.SelectedIndex);
        var replacement = Icon(); fresh.Icon = replacement; host.Layout(); Assert.Same(replacement, qat.Icon);
    });

    private static Ribbon RibbonFor(RibbonComboBox combo)
    {
        var ribbon = new Ribbon(); var tab = new RibbonTab { Header = "Home" }; var group = new RibbonGroup { Header = "Font" };
        Ribbon.SetCommandId(tab, "tab.home"); Ribbon.SetCommandId(group, "group.font");
        group.Items.Add(combo); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        return ribbon;
    }

    [Fact]
    public void Directly_declared_qat_combos_use_dropdowns_and_survive_remove_and_reset() => Sta.Run(() =>
    {
        var combo = new RibbonComboBox { Header = "Mode" }; combo.Items.Add("First"); combo.Items.Add("Second");
        Ribbon.SetCommandId(combo, "command.mode");
        var ribbon = new Ribbon(); ribbon.QuickAccessItems.Add(combo); Sta.Drain();
        var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
        Assert.Same(combo, Ribbon.GetQuickAccessSource(proxy)); Assert.Same(ribbon, combo.Parent);
        string defaults = RibbonCustomizationSerializer.Serialize(ribbon);
        ribbon.QuickAccessItems.Clear();
        Assert.Same(combo, Assert.Single(RibbonCommandCatalog.CollectAvailable(ribbon)).Control);
        Assert.True(ribbon.AddToQuickAccess(combo));
        Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
        RibbonCustomizationSerializer.Apply(ribbon, defaults); Sta.Drain();
        Assert.Same(combo, Ribbon.GetQuickAccessSource(Assert.IsAssignableFrom<FrameworkElement>(Assert.Single(ribbon.QuickAccessItems))));
    });

    [Fact]
    public void Choice_templates_selectors_and_authored_container_overrides_render_independently() => Sta.Run(() =>
    {
        var template = Template(nameof(Choice.Name), "{0} choice");
        var combo = new RibbonComboBox { ItemsSource = new[] { new Choice("First") }, ItemTemplate = template };
        var ribbon = RibbonFor(combo);
        var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.CreateCommandProxy(combo, RibbonControlSize.Small));
        using var host = Host(proxy);
        proxy.IsDropDownOpen = true; host.Layout();
        Assert.Contains(Descendants<TextBlock>(Assert.IsAssignableFrom<Button>(proxy.Items[0])), text => text.Text == "First choice");
        combo.ItemTemplate = Template(nameof(Choice.Name), "Updated {0}"); host.Layout();
        Assert.Contains(Descendants<TextBlock>(Assert.IsAssignableFrom<Button>(proxy.Items[0])), text => text.Text == "Updated First");
        proxy.IsDropDownOpen = false;
        combo.ItemTemplate = null; combo.ItemTemplateSelector = new ChoiceTemplateSelector(template);
        proxy.IsDropDownOpen = true; host.Layout();
        Assert.Contains(Descendants<TextBlock>(Assert.IsAssignableFrom<Button>(proxy.Items[0])), text => text.Text == "First choice");

        var authored = new RibbonComboBox { ItemTemplate = Template(".", "Default {0}") };
        var container = new ComboBoxItem { Content = "Alpha", ContentTemplate = Template(".", "Authored {0}") };
        authored.Items.Add(container); authored.Items.Add(new ComboBoxItem { Content = "Bravo" });
        authored.Items.Add(new ComboBoxItem { Content = "Charlie", ContentTemplateSelector =
            new ChoiceTemplateSelector(Template(".", "Selected {0}")) });
        var authoredProxy = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.CreateCommandProxy(authored, RibbonControlSize.Small));
        host.Window.Content = authoredProxy; host.Layout(); authoredProxy.IsDropDownOpen = true; host.Layout();
        Assert.Contains(Descendants<TextBlock>(Assert.IsAssignableFrom<Button>(authoredProxy.Items[0])), text => text.Text == "Authored Alpha");
        Assert.Contains(Descendants<TextBlock>(Assert.IsAssignableFrom<Button>(authoredProxy.Items[1])), text => text.Text == "Default Bravo");
        Assert.Contains(Descendants<TextBlock>(Assert.IsAssignableFrom<Button>(authoredProxy.Items[2])), text => text.Text == "Selected Charlie");
        Assert.Same(authored, container.Parent);
    });

    [Fact]
    public void Editable_text_keeps_its_editing_menu_while_the_arrow_resolves_the_combo_command() => Sta.Run(() =>
    {
        var combo = new RibbonComboBox { Header = "Font", IsEditable = true };
        var ribbon = RibbonFor(combo);
        using var host = Host(combo);
        var resolve = typeof(Ribbon).GetMethod("ResolveCommandControl", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var editor = Assert.IsAssignableFrom<TextBox>(combo.Template.FindName("PART_EditableTextBox", combo));
        var arrow = Assert.IsAssignableFrom<ToggleButton>(combo.Template.FindName("DropToggle", combo));
        Assert.Null(resolve.Invoke(ribbon, new object[] { editor }));
        Assert.Same(combo, resolve.Invoke(ribbon, new object[] { arrow }));
    });

    [Fact]
    public void Popup_starts_at_current_choice_and_mouse_hover_moves_focus_without_committing() => Sta.Run(() =>
    {
        var combo = new RibbonComboBox { Header = "Font" };
        combo.Items.Add(new ComboBoxItem { Content = "Unavailable", IsEnabled = false });
        combo.Items.Add("Calibri"); combo.Items.Add("Times New Roman"); combo.Items.Add("Georgia");
        var ribbon = RibbonFor(combo); ribbon.AddToQuickAccess(combo);
        var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
        using var host = Host(proxy);
        host.Window.Activate();
        foreach (var theme in new[] { RibbonTheme.Office2007, RibbonTheme.CrystalLight })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ThemeManager.Apply(Application.Current, theme); Ribbon.SetDensity(proxy, density); host.Layout();
            combo.SelectedIndex = 2;
            var opener = Assert.IsAssignableFrom<ToggleButton>(proxy.Template.FindName("PART_Toggle", proxy));
            Assert.True(opener.Focus()); proxy.IsDropDownOpen = true; host.Layout();
            var rows = proxy.Items.OfType<Button>().ToArray();
            Assert.Same(rows[2], Keyboard.FocusedElement);
            int changes = 0;
            SelectionChangedEventHandler changed = (_, _) => changes++;
            combo.SelectionChanged += changed;
            rows[3].RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
            host.Layout();
            Assert.Same(rows[3], Keyboard.FocusedElement); Assert.False(rows[1].IsKeyboardFocused);
            Assert.Equal(2, combo.SelectedIndex); Assert.Equal(0, changes); Assert.True(proxy.IsDropDownOpen);
            rows[0].RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
            Assert.Same(rows[3], Keyboard.FocusedElement);
            Invoke(rows[3]); host.Layout();
            Assert.Equal(3, combo.SelectedIndex); Assert.Equal(1, changes); Assert.False(proxy.IsDropDownOpen);
            combo.SelectionChanged -= changed;
        }
    });

    private static DataTemplate Template(string path, string format)
    {
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new Binding(path) { StringFormat = format });
        return new DataTemplate { VisualTree = text };
    }
    private sealed class ChoiceTemplateSelector(DataTemplate template) : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container) => template;
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T item) yield return item;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static DrawingImage Icon() => new(new GeometryDrawing(Brushes.Blue, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
    private static void Invoke(Button button)
    {
        var peer = UIElementAutomationPeer.CreatePeerForElement(button)!;
        ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)!).Invoke(); Sta.Drain();
    }
    private static TestHost Host(FrameworkElement content) => new(content);
    private sealed class TestHost : IDisposable
    {
        internal Window Window { get; }
        internal TestHost(FrameworkElement content)
        {
            Sta.UseApplication();
            Window = new Window { Content = content, Width = 650, Height = 300,
                Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            Window.Show(); Layout();
        }
        internal void Layout() { Sta.Drain(); Window.UpdateLayout(); Sta.Drain(); }
        public void Dispose() { Window.Close(); Sta.Drain(); Sta.ResetApplication(); }
    }
    private sealed record Choice(string Name);
    private sealed class SelectionModel : INotifyPropertyChanged
    {
        public ObservableCollection<Choice> Choices { get; } = new() { new("First"), new("Second") };
        private Choice? _selected;
        internal SelectionModel() => _selected = Choices[0];
        public Choice? Selected { get => _selected; set { _selected = value; PropertyChanged?.Invoke(this, new(nameof(Selected))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
