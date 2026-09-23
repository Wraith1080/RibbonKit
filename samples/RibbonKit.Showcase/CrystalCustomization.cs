using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

internal static class CrystalCustomization
{
    public static void Apply(RibbonOptionsDialog dialog, ResourceDictionary palette)
    {
        dialog.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit.Showcase;component/Themes/Crystal.Customize.xaml", UriKind.Relative) });
        foreach (var (target, source) in new[]
        {
            ("OptionsDialog.RailBackground", "Ribbon.ContentBackground"),
            ("Dialog.ActionBackground", "Tab.SelectedBackground"),
        })
            dialog.Resources["RibbonKit.Brushes." + target] = palette["RibbonKit.Brushes." + source];
        var selectedAccent = ((SolidColorBrush)dialog.FindResource("RibbonKit.Brushes.Accent")).Color;
        CrystalScrollBars.Configure(dialog.Resources, palette, selectedAccent);
        dialog.Resources["RibbonKit.Brushes.Dialog.ActionBorder"] = palette["Crystal.Brushes.ScreenTipBorder"];

        dialog.Loaded += (_, _) =>
        {
            if (dialog.Template.FindName("PART_OkButton", dialog) is Button ok)
            {
                var accent = ((SolidColorBrush)dialog.FindResource("RibbonKit.Brushes.Accent")).Color;
                foreach (var key in new[] { "Control.CheckedBackground", "Tab.SelectedBackground", "Control.PressedBackground" })
                    ok.Resources["RibbonKit.Brushes." + key] = CrystalScrollBars.WithWash((Brush)palette["RibbonKit.Brushes." + key], accent, 0.12);
                ok.Style = (Style)dialog.FindResource("Crystal.Customize.PrimaryAction");
            }
            if (dialog.Template.FindName("PART_CancelButton", dialog) is Button cancel)
                cancel.Style = (Style)dialog.FindResource("Crystal.Customize.Action");
            if (dialog.Template.FindName("PART_PageList", dialog) is ListBox navigation)
            {
                var containers = new Style(typeof(ListBoxItem), navigation.ItemContainerStyle);
                containers.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
                navigation.ItemContainerStyle = containers;
            }
        };

        foreach (var entry in dialog.Pages)
        {
            entry.SetResourceReference(Control.TemplateProperty, "Crystal.Customize.Navigation");
            if (entry.Content is not Control page) continue;
            TreeView? styledTree = null;
            page.Loaded += (_, _) =>
            {
                foreach (var name in new[] { "Add", "Remove", "Reset", "NewTab", "NewGroup", "Edit", "Up", "Down", "Import", "Export" })
                    if (page.Template?.FindName("PART_" + name + "Button", page) is Button button)
                        button.Style = (Style)dialog.FindResource(name is "Up" or "Down" or "Import" or "Export"
                            ? "Crystal.Customize.CompactAction" : "Crystal.Customize.Action");
                foreach (var name in new[] { "PART_AvailableList", "PART_CurrentList" })
                    if (page.Template?.FindName(name, page) is ListBox list)
                        list.ItemContainerStyle = (Style)dialog.FindResource("Crystal.Customize.ListItem");
                if (page.Template?.FindName("PART_Tree", page) is TreeView tree && tree != styledTree)
                {
                    var style = new Style(typeof(TreeViewItem), tree.ItemContainerStyle);
                    style.Setters.Add(new Setter(Control.TemplateProperty, dialog.FindResource("Crystal.Customize.TreeItem")));
                    tree.ItemContainerStyle = style;
                    styledTree = tree;
                }
            };
        }
    }

}
