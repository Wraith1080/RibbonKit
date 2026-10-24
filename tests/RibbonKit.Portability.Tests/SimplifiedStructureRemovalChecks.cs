using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Localization;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// A public-control consumer, with no Showcase resources or application handlers.
internal static class SimplifiedStructureRemovalChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try
        {
            foreach (var (theme, dark, density, direction) in new[]
            {
                (RibbonTheme.Office2024, false, RibbonDensity.Compact, FlowDirection.LeftToRight),
                (RibbonTheme.Office2024Flat, true, RibbonDensity.Touch, FlowDirection.LeftToRight),
                (RibbonTheme.CrystalLight, true, RibbonDensity.Touch, FlowDirection.RightToLeft),
                (RibbonTheme.Office2007, false, RibbonDensity.Compact, FlowDirection.RightToLeft),
            })
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                var ribbon = new Ribbon { LayoutMode = RibbonLayoutMode.Simplified, Density = density };
                var home = new RibbonTab { Header = "Home" }; Ribbon.SetCommandId(home, "home");
                var authored = new RibbonGroup { Header = "Authored" }; Ribbon.SetCommandId(authored, "authored");
                var source = new RibbonToggleButton { Header = "Bold" }; Ribbon.SetCommandId(source, "bold");
                authored.Items.Add(source); home.Groups.Add(authored); ribbon.Tabs.Add(home); ribbon.AddToQuickAccess(source);
                var page = new RibbonSimplifiedCustomizePage { Ribbon = ribbon };
                var classic = new RibbonCustomizePage { Ribbon = ribbon };
                var simpleOptions = new RibbonOptionsPage { Header = "Simplified", Content = page };
                var normalOptions = new RibbonOptionsPage { Header = "Classic", Content = classic };
                var dialog = new RibbonOptionsDialog { Width = 760, Height = 520, FlowDirection = direction,
                    ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000, Top = -10000 };
                Ribbon.SetDensity(dialog, density);
                dialog.Pages.Add(simpleOptions); dialog.Pages.Add(normalOptions); dialog.SelectedPage = simpleOptions;
                try
                {
                    dialog.Show(); Layout(dialog);
                    if (density == RibbonDensity.Touch) { VisualTreeHelper.SetRootDpi(dialog, new DpiScale(1.5, 1.5)); Layout(dialog); }
                    var originalQat = ribbon.QuickAccessItems[0];
                    MenuAction(page, "PART_NewButton", RibbonString.NewTab); Layout(dialog);
                    Part<ListBox>(page, "PART_AvailableList").SelectedIndex = 0;
                    Click(page, "PART_AddButton"); page.ApplyChanges(); Layout(dialog);
                    var tab = ribbon.Tabs.Single(Ribbon.GetIsCustom); var group = Assert.Single(tab.Groups);
                    string committed = RibbonCustomizationSerializer.Serialize(ribbon);
                    var current = Part<ListBox>(page, "PART_CurrentList");
                    current.SelectedItem = current.Items.OfType<DependencyObject>().First();
                    Assert.True(Part<Button>(page, "PART_RemoveButton").IsEnabled);
                    Click(page, "PART_RemoveButton"); Layout(dialog);
                    Assert.Equal(committed, RibbonCustomizationSerializer.Serialize(ribbon));
                    page.CancelChanges(); Layout(dialog);
                    current.SelectedItem = current.Items.OfType<DependencyObject>().First();
                    Click(page, "PART_RemoveButton"); page.ApplyChanges(); Layout(dialog);
                    Assert.Empty(tab.Groups); Assert.Same(source, authored.Items[0]); Assert.Same(originalQat, ribbon.QuickAccessItems[0]);
                    ribbon.LayoutMode = RibbonLayoutMode.Classic; Layout(dialog);
                    var tree = Part<TreeView>(classic, "PART_Tree");
                    Assert.Empty(tree.Items.OfType<RibbonCustomizeNode>().Single(node => node.IsCustom).Children);
                    ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout(dialog);
                    var menu = OpenMenu(page, "PART_EditButton"); Layout(dialog);
                    var remove = menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, RibbonLocalization.GetString(RibbonString.RemoveTab)));
                    Assert.True(remove.IsEnabled); Assert.Equal(direction, menu.FlowDirection);
                    Assert.Equal(density, Ribbon.GetDensity(menu));
                    if (density == RibbonDensity.Touch) Assert.True(remove.ActualHeight >= 44);
                    Capture(menu, $"{theme}-{dark}-{density}-{direction}");
                    remove.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); menu.IsOpen = false; Layout(dialog);
                    Assert.Contains(tab, ribbon.Tabs); page.ApplyChanges(); Layout(dialog); Assert.Single(ribbon.Tabs);
                    ribbon.LayoutMode = RibbonLayoutMode.Classic; Layout(dialog);
                    Assert.DoesNotContain(tree.Items.OfType<RibbonCustomizeNode>(), node => node.IsCustom);
                    string deleted = RibbonCustomizationSerializer.Serialize(ribbon);
                    RibbonCustomizationSerializer.Apply(ribbon, deleted); Layout(dialog);
                    Assert.Single(ribbon.Tabs); Assert.True(ribbon.IsInQuickAccess(source));
                    ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout(dialog);
                    menu = OpenMenu(page, "PART_EditButton");
                    Assert.False(menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, RibbonLocalization.GetString(RibbonString.RemoveTab))).IsEnabled);
                    menu.IsOpen = false;
                }
                finally { dialog.Close(); Layout(dialog); }
            }
            Console.WriteLine("Simplified structure removal: 4 independent theme/density/direction cases passed; Touch uses simulated 150% root DPI.");
        }
        finally { RibbonAnimation.GlobalLevel = motion; ThemeManager.SetDarkMode(application, false); }
    }

    private static T Part<T>(Control owner, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
    private static void Click(Control owner, string name) => Part<ButtonBase>(owner, name).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
    private static ContextMenu OpenMenu(Control page, string name) { Click(page, name); return Part<ButtonBase>(page, name).ContextMenu; }
    private static void MenuAction(Control page, string name, RibbonString key)
    {
        var menu = OpenMenu(page, name);
        menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, RibbonLocalization.GetString(key))).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        menu.IsOpen = false;
    }
    private static void Layout(Window window) { window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout(); window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); }
    private static void Capture(ContextMenu menu, string name)
    {
        string? path = Environment.GetEnvironmentVariable("RIBBONKIT_REMOVAL_CAPTURE");
        if (string.IsNullOrEmpty(path)) return;
        // Render the menu itself so native popup fade timing cannot capture an empty owner root.
        var surface = menu;
        var dpi = VisualTreeHelper.GetDpi(surface);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(surface.ActualWidth * dpi.DpiScaleX), (int)Math.Ceiling(surface.ActualHeight * dpi.DpiScaleY),
            96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY, PixelFormats.Pbgra32);
        bitmap.Render(surface); Directory.CreateDirectory(path);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(Path.Combine(path, name + ".png")); encoder.Save(output);
    }
}
