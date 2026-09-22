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
            ("ScrollBar.Glyph", "Text.Secondary"),
            ("ScrollBar.ButtonBackground", "Control.SurfaceBackground"),
            ("ScrollBar.ButtonBorder", "Control.HoverBorder"),
            ("Dialog.ActionBackground", "Tab.SelectedBackground"),
        })
            dialog.Resources["RibbonKit.Brushes." + target] = palette["RibbonKit.Brushes." + source];
        dialog.Resources["RibbonKit.Brushes.ScrollBar.ThumbBorder"] = palette["RibbonKit.Brushes.Control.HoverBorder"];
        dialog.Resources["RibbonKit.Brushes.ScrollBar.Track"] = Brushes.Transparent;
        var arrowSurface = (Brush)dialog.Resources["RibbonKit.Brushes.ScrollBar.ButtonBackground"];
        var selectedAccent = ((SolidColorBrush)dialog.FindResource("RibbonKit.Brushes.Accent")).Color;
        dialog.Resources["RibbonKit.Brushes.ScrollBar.Thumb"] = WithWash(arrowSurface, selectedAccent, 0.06);
        dialog.Resources["RibbonKit.Brushes.ScrollBar.ThumbHover"] = WithWash(arrowSurface, selectedAccent, 0.08);
        dialog.Resources["RibbonKit.Brushes.ScrollBar.ThumbPressed"] = WithWash(arrowSurface, selectedAccent, 0.11);
        dialog.Resources["RibbonKit.Brushes.Dialog.ActionBorder"] = palette["Crystal.Brushes.ScreenTipBorder"];
        dialog.Resources["RibbonKit.Metrics.ScrollBar.Thickness"] = 14d;
        dialog.Resources["RibbonKit.Metrics.ScrollBar.ThumbBorderThickness"] = new Thickness(1);
        foreach (var name in new[] { "ButtonCornerRadius", "ThumbCornerRadius", "RailCornerRadius" })
            dialog.Resources["RibbonKit.Metrics.ScrollBar." + name] = new CornerRadius(4);

        dialog.Loaded += (_, _) =>
        {
            if (dialog.Template.FindName("PART_OkButton", dialog) is Button ok)
            {
                var accent = ((SolidColorBrush)dialog.FindResource("RibbonKit.Brushes.Accent")).Color;
                foreach (var key in new[] { "Control.CheckedBackground", "Tab.SelectedBackground", "Control.PressedBackground" })
                    ok.Resources["RibbonKit.Brushes." + key] = WithWash((Brush)palette["RibbonKit.Brushes." + key], accent, 0.12);
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

    private static DrawingBrush WithWash(Brush source, Color color, double amount)
    {
        // Preserve the accepted surface and reflection geometry; only its tint changes.
        var face = new RectangleGeometry(new Rect(0, 0, 1, 1));
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(source.CloneCurrentValue(), null, face));
        var wash = new SolidColorBrush(Color.FromArgb((byte)Math.Round(amount * 255), color.R, color.G, color.B));
        drawing.Children.Add(new GeometryDrawing(wash, null, face));
        var brush = new DrawingBrush(drawing) { Viewbox = new Rect(0, 0, 1, 1), ViewboxUnits = BrushMappingMode.Absolute, Stretch = Stretch.Fill };
        brush.Freeze();
        return brush;
    }
}
