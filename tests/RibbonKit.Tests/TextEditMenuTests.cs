using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class TextEditMenuTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Native_edit_menu_uses_shared_theme_without_replacing_editor_commands(bool comboBox) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        Control control = comboBox
            ? new RibbonComboBox { IsEditable = true, Text = "Calibri" }
            : new RibbonTextBox { Text = "Calibri" };
        var window = new Window { Content = control, Width = 350, Height = 180,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        ContextMenu? menu = null;
        try
        {
            window.Show(); Layout(window);
            var editor = comboBox
                ? Assert.IsType<TextBox>(control.Template.FindName("PART_EditableTextBox", control))
                : Assert.IsType<RibbonTextBox>(control);
            editor.Select(1, 3);
            menu = OpenNativeMenu(editor);
            Layout(window);

            Assert.NotNull(menu.Style);
            Assert.Null(editor.ContextMenu);
            Assert.Equal(DependencyProperty.UnsetValue, editor.ReadLocalValue(FrameworkElement.ContextMenuProperty));
            Assert.Equal(1, editor.SelectionStart);
            Assert.Equal(3, editor.SelectionLength);
            Assert.Same(editor, menu.PlacementTarget);
            Assert.Equal(typeof(TextBox).Assembly, menu.GetType().Assembly);
            foreach (var command in new[] { ApplicationCommands.Cut, ApplicationCommands.Copy, ApplicationCommands.Paste })
            {
                var item = Assert.Single(menu.Items.OfType<MenuItem>(), item => ReferenceEquals(item.Command, command));
                Assert.Same(editor, item.CommandTarget);
                Assert.NotNull(item.Style);
                Assert.NotEmpty(item.InputGestureText);
                Assert.True(item.ActualHeight > 0);
                var gesture = Assert.IsType<TextBlock>(item.Template.FindName("Gesture", item));
                Assert.Equal(item.InputGestureText, gesture.Text);
                Assert.Equal(Visibility.Visible, gesture.Visibility);
            }

            menu.IsOpen = false; Layout(window);
            editor.IsReadOnly = true;
            menu = OpenNativeMenu(editor); Layout(window);
            Assert.False(Command(ApplicationCommands.Cut).IsEnabled);
            Assert.True(Command(ApplicationCommands.Copy).IsEnabled);
            Assert.False(Command(ApplicationCommands.Paste).IsEnabled);
            Assert.Equal("ali", editor.SelectedText);

            MenuItem Command(RoutedCommand command) =>
                Assert.Single(menu.Items.OfType<MenuItem>(), item => ReferenceEquals(item.Command, command));
        }
        finally
        {
            if (menu is not null) menu.IsOpen = false;
            window.Close(); Sta.Drain(DispatcherPriority.ApplicationIdle); Sta.ResetApplication();
        }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Authored_menus_and_explicit_null_keep_their_precedence(bool comboBox) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        Control control = comboBox ? new RibbonComboBox { IsEditable = true } : new RibbonTextBox();
        var window = new Window { Content = control, Width = 350, Height = 180,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        var custom = new ContextMenu { Items = { new MenuItem { Header = "Custom action" } } };
        try
        {
            window.Show(); Layout(window);
            var editor = comboBox ? (TextBox)control.Template.FindName("PART_EditableTextBox", control) : (TextBox)control;
            editor.ContextMenu = custom;
            custom.PlacementTarget = editor; custom.IsOpen = true; Layout(window);
            Assert.Null(custom.Style);
            Assert.Empty(custom.Resources);
            Assert.Null(((MenuItem)custom.Items[0]).Style);
            custom.IsOpen = false; Layout(window);

            // A host can also open a menu directly without storing it on the editor.
            editor.ClearValue(FrameworkElement.ContextMenuProperty);
            custom.IsOpen = true; Layout(window);
            Assert.Null(custom.Style);
            custom.IsOpen = false; Layout(window);

            editor.ContextMenu = null;
            var args = OpeningArgs(editor);
            editor.RaiseEvent(args); Layout(window);
            Assert.False(args.Handled);
            Assert.Empty(OpenMenus());
            Assert.Null(editor.ReadLocalValue(FrameworkElement.ContextMenuProperty));
        }
        finally
        {
            custom.IsOpen = false; window.Close();
            Sta.Drain(DispatcherPriority.ApplicationIdle); Sta.ResetApplication();
        }
    });

    [Fact]
    public void Ordinary_text_box_native_menu_is_not_restyled() => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var editor = new TextBox { Text = "Plain text" };
        var window = new Window { Content = editor, Width = 350, Height = 180,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        ContextMenu? menu = null;
        try
        {
            window.Show(); Layout(window);
            menu = OpenNativeMenu(editor);
            Assert.Null(menu.Style);
        }
        finally
        {
            if (menu is not null) menu.IsOpen = false;
            window.Close(); Sta.Drain(DispatcherPriority.ApplicationIdle); Sta.ResetApplication();
        }
    });

    private static ContextMenu OpenNativeMenu(TextBox editor)
    {
        // WPF's event-args constructor is internal. Raising the routed keyboard-open
        // event exercises its real editor class handler without physical OS input.
        var args = OpeningArgs(editor);
        editor.RaiseEvent(args);
        Sta.Drain(DispatcherPriority.ApplicationIdle);
        Assert.True(args.Handled);
        return Assert.Single(OpenMenus());
    }

    private static ContextMenuEventArgs OpeningArgs(TextBox editor) =>
        (ContextMenuEventArgs)typeof(ContextMenuEventArgs).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(ctor => ctor.GetParameters().Length == 2).Invoke(new object[] { editor, true });

    private static IEnumerable<ContextMenu> OpenMenus() =>
        PresentationSource.CurrentSources.Cast<PresentationSource>().OfType<HwndSource>()
            .SelectMany(source => Descendants(source.RootVisual)).OfType<ContextMenu>().Where(menu => menu.IsOpen);

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        yield return root;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var child in Descendants(VisualTreeHelper.GetChild(root, index))) yield return child;
    }

    private static void Layout(Window window)
    {
        Sta.Drain(DispatcherPriority.ApplicationIdle); window.UpdateLayout();
        Sta.Drain(DispatcherPriority.ApplicationIdle);
    }
}
