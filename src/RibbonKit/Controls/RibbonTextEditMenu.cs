using System.Windows;
using System.Windows.Controls;
using RibbonKit.Animation;

namespace RibbonKit.Controls;

/// <summary>Applies shared menu chrome to WPF's own ribbon text-editor menus.</summary>
internal static class RibbonTextEditMenu
{
    static RibbonTextEditMenu()
    {
        // The editor creates a temporary menu, without assigning TextBox.ContextMenu.
        // Its public Opened event exposes that actual instance without replacing it.
        EventManager.RegisterClassHandler(typeof(ContextMenu), ContextMenu.OpenedEvent,
            new RoutedEventHandler(OnMenuOpened));
    }

    internal static void EnsureRegistered() { }

    private static void OnMenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu
            || menu.PlacementTarget is not TextBox editor
            || !IsRibbonEditor(editor)
            || editor.ContextMenu is not null
            || editor.ReadLocalValue(FrameworkElement.ContextMenuProperty) is null
            || menu.GetType() == typeof(ContextMenu)
            || menu.GetType().Assembly != typeof(TextBox).Assembly)
        {
            return;
        }

        Ribbon.ApplyModernMenuStyle(menu);
        menu.FlowDirection = editor.FlowDirection;
        Ribbon.SetDensity(menu, Ribbon.GetDensity(editor));
        // WPF has already started its native entrance. Do not add a second animation.
        RibbonPopupMotion.SetAnimateOpen(menu, false);
        StyleItems(menu);
    }

    private static bool IsRibbonEditor(TextBox editor) =>
        editor is RibbonTextBox
        || editor.TemplatedParent is RibbonComboBox { IsEditable: true } comboBox
            && ReferenceEquals(comboBox.Template?.FindName("PART_EditableTextBox", comboBox), editor);

    private static void StyleItems(ItemsControl parent)
    {
        foreach (object child in parent.Items)
        {
            if (child is MenuItem item)
            {
                // Native editor rows use a WPF-private MenuItem subclass. An implicit
                // style keyed by the base type does not reach those derived instances.
                if (item.ReadLocalValue(FrameworkElement.StyleProperty) == DependencyProperty.UnsetValue)
                    item.SetResourceReference(FrameworkElement.StyleProperty, typeof(MenuItem));
                StyleItems(item);
            }
        }
    }
}
