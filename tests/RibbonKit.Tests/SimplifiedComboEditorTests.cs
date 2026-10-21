using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;
using KeyTipService = RibbonKit.Controls.KeyTipService;

namespace RibbonKit.Tests;

public sealed class SimplifiedComboEditorTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Original_slot_keeps_native_combo_and_authored_items_while_copies_remain_menus(bool editable) => Sta.Run(() =>
    {
        using var host = new Host(editable);
        var first = new ComboBoxItem { Content = "Calibri" };
        var last = new ComboBoxItem { Content = new TextBlock { Text = "Arial" } };
        host.Source.Items.Add(first); host.Source.Items.Add(last); host.Source.SelectedIndex = 0;
        host.Ribbon.AddToQuickAccess(host.Source);
        var custom = new RibbonGroup { Header = "Custom" }; Ribbon.SetIsCustom(custom, true);
        var declaration = (RibbonDropDownButton)host.Ribbon.CreateCommandProxy(host.Source, RibbonControlSize.Medium);
        Ribbon.SetSimplifiedPlacement(declaration, RibbonSimplifiedPlacement.Inline);
        custom.Items.Add(declaration); host.Tab.Groups.Add(custom);
        host.Simplify();
        var editor = Assert.IsType<RibbonComboBoxSimplifiedProxy>(host.Copy(host.Source));
        Assert.Equal(editable, editor.IsEditable); Assert.Equal(host.Source.InputWidth, editor.InputWidth);
        Assert.Equal("Calibri", editor.Text);
        Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(host.Ribbon.QuickAccessItems));
        Assert.IsAssignableFrom<RibbonDropDownButton>(host.Row.Items.OfType<FrameworkElement>().Single(item =>
            ReferenceEquals(item.GetValue(Ribbon.SimplifiedCommandGroupProperty), custom) && item is not RibbonGroupSeparator && item is not RibbonGroupQuickAccessProxy));
        Assert.Same(host.Source, first.Parent); Assert.Same(host.Source, last.Parent);
        int changes = 0; host.Source.SelectionChanged += (_, _) => changes++;
        editor.SelectedIndex = 1; host.Layout();
        Assert.Same(last, host.Source.SelectedItem); Assert.Equal("Arial", host.Source.Text); Assert.Equal(1, changes);
        host.Source.SelectedIndex = 0; host.Layout(); Assert.Equal("Calibri", editor.Text);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        Assert.Same(host.Source, first.Parent); Assert.Equal(2, host.Source.Items.Count);
        host.Simplify(); Assert.Equal("Calibri", Assert.IsAssignableFrom<RibbonComboBox>(host.Copy(host.Source)).Text);
    });

    [Fact]
    public void Editable_text_and_bound_selection_remain_owned_by_source_through_refresh_and_mode_changes() => Sta.Run(() =>
    {
        using var host = new Host(true);
        var model = new Model();
        host.Source.DataContext = model;
        host.Source.DisplayMemberPath = nameof(FontChoice.Name);
        host.Source.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(Model.Choices)));
        host.Source.SetBinding(Selector.SelectedItemProperty, new Binding(nameof(Model.Selected)) { Mode = BindingMode.TwoWay });
        host.Source.SetBinding(ComboBox.TextProperty, new Binding(nameof(Model.Text)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        host.Simplify();
        var editor = Assert.IsAssignableFrom<RibbonComboBox>(host.Copy(host.Source));
        Assert.Equal("Calibri", editor.Text);
        var input = (TextBox)editor.Template.FindName("PART_EditableTextBox", editor);
        input.Text = "Aptos custom"; host.Layout();
        Assert.Equal("Aptos custom", host.Source.Text); Assert.Equal("Aptos custom", model.Text);
        Assert.True(BindingOperations.IsDataBound(host.Source, Selector.SelectedItemProperty));
        Assert.True(BindingOperations.IsDataBound(host.Source, ComboBox.TextProperty));
        model.Choices.Add(new("Aptos", "Other")); host.Layout();
        Assert.Equal(3, editor.Items.Count); Assert.Equal("Aptos custom", editor.Text);
        editor.SelectedIndex = 2; host.Layout(); Assert.Same(model.Choices[2], model.Selected); Assert.Equal("Aptos", model.Text);
        model.Choices.RemoveAt(0); host.Layout(); Assert.Same(model.Choices[1], model.Selected); Assert.Equal(1, editor.SelectedIndex);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout(); Assert.Equal("Aptos", host.Source.Text);
        host.Simplify(); Assert.Equal("Aptos", Assert.IsAssignableFrom<RibbonComboBox>(host.Copy(host.Source)).Text);
    });

    [Fact]
    public void Grouped_view_and_native_group_flyout_stay_synchronized_and_overflow_uses_menu() => Sta.Run(() =>
    {
        using var host = new Host(true);
        var choices = new ObservableCollection<FontChoice> { new("Calibri", "Common"), new("Arial", "Other") };
        var view = new ListCollectionView(choices);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(FontChoice.Section)));
        host.Source.ItemsSource = view; host.Source.DisplayMemberPath = nameof(FontChoice.Name); host.Source.SelectedIndex = 0;
        host.Simplify();
        var editor = Assert.IsAssignableFrom<RibbonComboBox>(host.Copy(host.Source));
        Assert.Equal(new[] { "Common", "Other" }, editor.Items.Groups!.Cast<CollectionViewGroup>().Select(group => group.Name));
        editor.IsDropDownOpen = true; host.Layout();
        choices.Add(new("Consolas", "Other")); host.Layout(); Assert.Equal(3, editor.Items.Count);
        editor.SelectedIndex = 2; editor.IsDropDownOpen = false; host.Layout(); Assert.Same(choices[2], host.Source.SelectedItem);
        view.GroupDescriptions.Clear(); host.Layout(); Assert.False(editor.IsGrouping); Assert.Equal("Consolas", editor.Text);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(FontChoice.Section))); host.Layout(); Assert.True(editor.IsGrouping);
        host.Row.Width = 60; host.Layout(); Assert.True(host.Row.IsOverflowed(editor)); Assert.False(editor.Focusable);
        host.Row.OpenOverflow(); host.Layout();
        var menu = Assert.Single(host.Row.OverflowEntries.OfType<RibbonComboBoxQuickAccessProxy>());
        menu.IsDropDownOpen = true; host.Layout(); Assert.Equal(3, menu.Items.Count);
        menu.IsDropDownOpen = false; host.Row.CloseOverflow(); host.Row.ClearValue(FrameworkElement.WidthProperty); host.Layout();
        var groupMenu = host.Row.Items.OfType<RibbonGroupQuickAccessProxy>().Single();
        groupMenu.IsDropDownOpen = true; host.Layout(); Assert.True(host.Source.IsVisible);
        host.Source.SelectedIndex = 1; host.Layout(); Assert.Equal(1, editor.SelectedIndex); Assert.Equal("Arial", editor.Text);
        groupMenu.IsDropDownOpen = false; host.Layout();
        Assert.Same(view, host.Source.ItemsSource);
    });

    [Fact]
    public void Keyboard_focus_and_open_picker_return_through_resize_and_classic_mode() => Sta.Run(() =>
    {
        using var host = new Host(true);
        host.Source.Items.Add("Calibri"); host.Source.Items.Add("Arial"); host.Source.SelectedIndex = 0;
        host.Simplify(); host.Window.Activate(); host.Layout();
        var editor = Assert.IsAssignableFrom<RibbonComboBox>(host.Copy(host.Source));
        editor.Focus(); host.Layout(); Assert.True(editor.IsKeyboardFocusWithin);
        KeyTipService.InvokeControl(editor); host.Layout(); Assert.True(editor.IsDropDownOpen);
        host.Row.Width = 60; host.Layout();
        Assert.False(editor.IsDropDownOpen); Assert.True(host.Row.OverflowButton!.IsKeyboardFocusWithin);
        host.Row.ClearValue(FrameworkElement.WidthProperty); host.Layout();
        editor.Focus(); host.Layout(); Assert.True(editor.IsKeyboardFocusWithin);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        Assert.True(host.Source.IsKeyboardFocusWithin,
            $"Focus remained on {Keyboard.FocusedElement?.GetType().Name}; source visible={host.Source.IsVisible}, editor visible={editor.IsVisible}.");
    });

    [Fact]
    public void Returning_to_classic_does_not_override_a_later_focus_choice() => Sta.Run(() =>
    {
        using var host = new Host(true);
        host.Source.Items.Add("Calibri"); host.Source.SelectedIndex = 0;
        var other = new Button { Content = "Document action" };
        host.Window.Content = null;
        host.Window.Content = new StackPanel { Children = { host.Ribbon, other } };
        host.Simplify(); host.Window.Activate(); host.Layout();
        var editor = Assert.IsAssignableFrom<RibbonComboBox>(host.Copy(host.Source));
        editor.Focus(); host.Layout(); Assert.True(editor.IsKeyboardFocusWithin);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; other.Focus(); host.Layout();
        Assert.True(other.IsKeyboardFocusWithin);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Controls_declared_in_custom_tabs_or_groups_keep_dropdown_presentation(bool customGroup) => Sta.Run(() =>
    {
        using var host = new Host(true);
        host.Source.Items.Add("Calibri"); host.Source.SelectedIndex = 0;
        Ribbon.SetIsCustom(customGroup ? (DependencyObject)host.Group : host.Tab, true);
        host.Simplify();
        Assert.IsAssignableFrom<RibbonDropDownButton>(host.Copy(host.Source));
    });

    [Fact]
    public void Initial_simplified_mode_and_reload_realize_current_native_editor_state() => Sta.Run(() =>
    {
        using var host = new Host(true, initialSimplified: true);
        var editor = Assert.IsAssignableFrom<RibbonComboBox>(host.Copy(host.Source));
        var input = Assert.IsType<TextBox>(editor.Template.FindName("PART_EditableTextBox", editor));
        Assert.True(input.IsVisible && input.ActualWidth > 0 && input.ActualHeight > 0);
        Assert.Equal("Arial", editor.Text); Assert.Equal(1, editor.SelectedIndex);
        host.Window.Content = null; host.Layout();
        host.Source.Items.Add("Georgia"); host.Source.SelectedIndex = 2;
        host.Window.Content = host.Ribbon; host.Layout();
        var reloaded = Assert.IsAssignableFrom<RibbonComboBox>(host.Copy(host.Source));
        Assert.NotSame(editor, reloaded); Assert.Equal("Georgia", reloaded.Text); Assert.Equal(3, reloaded.Items.Count);
    });

    private sealed record FontChoice(string Name, string Section);
    private sealed class Model : INotifyPropertyChanged
    {
        public ObservableCollection<FontChoice> Choices { get; } = new() { new("Calibri", "Common"), new("Arial", "Other") };
        private FontChoice? _selected;
        private string _text = "Calibri";
        public Model() => _selected = Choices[0];
        public FontChoice? Selected { get => _selected; set { _selected = value; PropertyChanged?.Invoke(this, new(nameof(Selected))); } }
        public string Text { get => _text; set { _text = value; PropertyChanged?.Invoke(this, new(nameof(Text))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Host : IDisposable
    {
        internal readonly Ribbon Ribbon = new();
        internal readonly RibbonTab Tab = new() { Header = "Home" };
        internal readonly RibbonGroup Group = new() { Header = "Font" };
        internal readonly RibbonComboBox Source;
        internal readonly Window Window;
        internal RibbonQuickAccessToolBar Row => ((RibbonGroupsHost)Tab.Content).SimplifiedToolBar!;
        internal FrameworkElement Copy(FrameworkElement source) => Row.Items.OfType<FrameworkElement>().Single(item => Ribbon.GetQuickAccessSource(item) == source
            && ReferenceEquals(item.GetValue(Ribbon.SimplifiedCommandGroupProperty), Group));
        internal Host(bool editable, bool initialSimplified = false)
        {
            ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024); RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            Source = new RibbonComboBox { Header = "Font", IsEditable = editable, InputWidth = 150 };
            Ribbon.SetSimplifiedPlacement(Source, RibbonSimplifiedPlacement.Inline); Group.Items.Add(Source);
            Tab.Groups.Add(Group); Ribbon.Tabs.Add(Tab); Ribbon.SelectedTab = Tab;
            if (initialSimplified)
            {
                Source.Items.Add("Calibri"); Source.Items.Add("Arial"); Source.SelectedIndex = 1;
                Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
            }
            Window = new Window { Content = Ribbon, Width = 1000, Height = 350, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            Window.Show(); Layout();
        }
        internal void Simplify() { Ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout(); }
        internal void Layout() { Sta.Drain(); Window.UpdateLayout(); Sta.Drain(); Window.UpdateLayout(); }
        public void Dispose() { Window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle; Sta.ResetApplication(); }
    }
}
