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

internal static class SimplifiedCustomizationPortabilityChecks
{
    internal static void Verify(Application application)
    {
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        int cases = 0;
        try
        {
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            foreach (FlowDirection direction in Enum.GetValues<FlowDirection>())
            foreach (double scale in new[] { 1d, 1.5d })
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                var ribbon = new Ribbon { LayoutMode = RibbonLayoutMode.Simplified, Density = density };
                var tab = new RibbonTab { Header = "Home" }; Ribbon.SetCommandId(tab, "home");
                var group = new RibbonGroup { Header = "Editing" }; Ribbon.SetCommandId(group, "editing");
                var otherGroup = new RibbonGroup { Header = "Formatting" }; Ribbon.SetCommandId(otherGroup, "formatting");
                var first = new RibbonButton { Header = "Copy" }; Ribbon.SetCommandId(first, "copy");
                Ribbon.SetSimplifiedPlacement(first, RibbonSimplifiedPlacement.Inline); group.Items.Add(first);
                for (int i = 0; i < 24; i++)
                {
                    var button = new RibbonButton { Header = $"Command {i}" }; Ribbon.SetCommandId(button, $"command-{i}");
                    (i < 12 ? group : otherGroup).Items.Add(button);
                }
                tab.Groups.Add(group); tab.Groups.Add(otherGroup); ribbon.Tabs.Add(tab);
                var page = new RibbonSimplifiedCustomizePage { Ribbon = ribbon };
                var classic = new RibbonOptionsPage { Header = "Classic", Content = new RibbonCustomizePage { Ribbon = ribbon } };
                var simplified = new RibbonOptionsPage { Header = "Simplified", Content = page };
                var qat = new RibbonOptionsPage { Header = "QAT", Content = new RibbonQuickAccessPage { Ribbon = ribbon } };
                var dialog = new RibbonOptionsDialog { Width = 700, Height = 500, ShowActivated = false, ShowInTaskbar = false,
                    Left = -10000, Top = -10000, WindowStartupLocation = WindowStartupLocation.Manual, FlowDirection = direction };
                Ribbon.SetDensity(dialog, density);
                dialog.Pages.Add(classic); dialog.Pages.Add(simplified); dialog.Pages.Add(qat); dialog.SelectedPage = simplified;
                try
                {
                    dialog.Show(); Layout(dialog); VisualTreeHelper.SetRootDpi(dialog, new DpiScale(scale, scale)); Layout(dialog);
                    Assert.True(page.IsEnabled); Assert.False(classic.IsEnabled); Assert.True(qat.IsEnabled);
                    Assert.Equal(Visibility.Collapsed, classic.Visibility); Assert.Equal(Visibility.Visible, simplified.Visibility);
                    var viewport = Part<ScrollContentPresenter>(Part<ScrollViewer>(dialog, "PART_ContentScroll"), "PART_ScrollContentPresenter");
                    Assert.Equal(ScrollBarVisibility.Disabled, Part<ScrollViewer>(dialog, "PART_ContentScroll").VerticalScrollBarVisibility);
                    var parts = new[] { "PART_TabSelector", "PART_GroupSelector", "PART_NewTabButton", "PART_NewGroupButton", "PART_RenameTabButton", "PART_RenameGroupButton",
                        "PART_AvailableList", "PART_CurrentList", "PART_AddButton", "PART_RemoveButton",
                        "PART_UpButton", "PART_DownButton", "PART_ResetButton", "PART_ResetAllButton", "PART_ApplyButton" };
                    foreach (string name in parts)
                    {
                        var part = Part<FrameworkElement>(page, name);
                        Rect bounds = part.TransformToVisual(viewport).TransformBounds(new Rect(part.RenderSize));
                        Assert.True(bounds.Left >= -0.75 && bounds.Right <= viewport.ActualWidth + 0.75 && bounds.Top >= -0.75 && bounds.Bottom <= viewport.ActualHeight + 0.75,
                            $"{theme}/{dark}/{density}/{direction}/{scale}/{name}: {bounds}, viewport={viewport.RenderSize}");
                    }
                    var selector = Part<RibbonComboBox>(page, "PART_TabSelector");
                    var currentFrame = Part<ListBox>(page, "PART_CurrentList");
                    foreach (double width in new[] { 560d, 700d, 900d })
                    {
                        dialog.Width = width; Layout(dialog); Layout(dialog);
                        Assert.InRange(Math.Abs(selector.ActualWidth - currentFrame.ActualWidth), 0, 1);
                        Assert.InRange(Math.Abs(Part<FrameworkElement>(selector, "InputBox").ActualWidth - selector.ActualWidth), 0, 1);
                        var groupSelector = Part<RibbonComboBox>(page, "PART_GroupSelector");
                        Assert.InRange(Math.Abs(groupSelector.ActualWidth - currentFrame.ActualWidth), 0, 1);
                        Assert.InRange(Math.Abs(Part<FrameworkElement>(groupSelector, "InputBox").ActualWidth - groupSelector.ActualWidth), 0, 1);
                        foreach (string name in parts)
                        {
                            var part = Part<FrameworkElement>(page, name);
                            Rect bounds = part.TransformToVisual(viewport).TransformBounds(new Rect(part.RenderSize));
                            Assert.True(bounds.Left >= -0.75 && bounds.Right <= viewport.ActualWidth + 0.75 && bounds.Top >= -0.75 && bounds.Bottom <= viewport.ActualHeight + 0.75,
                                $"{theme}/{dark}/{density}/{direction}/{scale}/width={width}/{name}: {bounds}, viewport={viewport.RenderSize}");
                        }
                    }
                    var available = Part<ListBox>(page, "PART_AvailableList"); var current = Part<ListBox>(page, "PART_CurrentList");
                    Assert.Equal(25, available.Items.Count); Assert.Single(current.Items.Cast<object>());
                    var scroll = Part<ScrollViewer>(available, "PART_ScrollViewer");
                    Assert.False(scroll.CanContentScroll); Assert.False(Part<ScrollViewer>(current, "PART_ScrollViewer").CanContentScroll);
                    scroll.ScrollToVerticalOffset(7.5); Layout(dialog);
                    Assert.InRange(scroll.VerticalOffset, 7, 8);
                    available.SelectedIndex = available.Items.Count - 1; available.ScrollIntoView(available.SelectedItem); Layout(dialog);
                    var last = (ListBoxItem)available.ItemContainerGenerator.ContainerFromIndex(available.SelectedIndex);
                    Rect lastBounds = last.TransformToAncestor(scroll).TransformBounds(new Rect(last.RenderSize));
                    Assert.InRange(lastBounds.Top, -1, scroll.ViewportHeight);
                    Assert.InRange(lastBounds.Bottom, 0, scroll.ViewportHeight + 1);
                    scroll.ScrollToTop(); Layout(dialog);
                    available.SelectedIndex = 1; Click(page, "PART_AddButton"); Layout(dialog);
                    Assert.Equal(2, current.Items.Count);
                    string before = RibbonCustomizationSerializer.Serialize(ribbon);
                    page.CancelChanges(); Layout(dialog); Assert.Single(current.Items.Cast<object>());
                    Assert.Equal(before, RibbonCustomizationSerializer.Serialize(ribbon));
                    available.SelectedIndex = 1; Click(page, "PART_AddButton"); Click(page, "PART_UpButton");
                    page.ApplyChanges(); Layout(dialog);
                    Assert.Contains("command-0", RibbonCustomizationSerializer.Serialize(ribbon));
                    ribbon.LayoutMode = RibbonLayoutMode.Classic; Layout(dialog);
                    Assert.Equal(Visibility.Visible, classic.Visibility); Assert.Equal(Visibility.Collapsed, simplified.Visibility);
                    Assert.Same(classic, dialog.SelectedPage); Assert.False(page.IsEnabled); Assert.True(qat.IsEnabled);
                    ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout(dialog);
                    Assert.Equal(Visibility.Collapsed, classic.Visibility); Assert.Equal(Visibility.Visible, simplified.Visibility);
                    Assert.Same(simplified, dialog.SelectedPage); Assert.Equal(2, current.Items.Count);
                    string beforeStructure = RibbonCustomizationSerializer.Serialize(ribbon);
                    Click(page, "PART_NewTabButton"); available.SelectedIndex = 1; Click(page, "PART_AddButton"); Layout(dialog);
                    Assert.Single(ribbon.Tabs); Assert.Single(current.Items.Cast<object>());
                    Assert.Equal(beforeStructure, RibbonCustomizationSerializer.Serialize(ribbon));
                    page.CancelChanges(); Layout(dialog); Assert.Equal(beforeStructure, RibbonCustomizationSerializer.Serialize(ribbon));
                    Click(page, "PART_NewTabButton"); available.SelectedIndex = 1; Click(page, "PART_AddButton");
                    Click(page, "PART_NewGroupButton"); available.SelectedIndex = 0; Click(page, "PART_AddButton"); Layout(dialog);
                    if (theme == RibbonTheme.Office2024 && !dark && density == RibbonDensity.Compact && direction == FlowDirection.LeftToRight && scale == 1)
                    {
                        Rename(page, "PART_RenameTabButton", "My tools"); Rename(page, "PART_RenameGroupButton", "Favorites");
                        Layout(dialog); Layout(dialog); Save(dialog, "structure-editor-consumer");
                    }
                    page.ApplyChanges(); Layout(dialog); Assert.Equal(2, ribbon.Tabs.Count);
                    var customTab = ribbon.Tabs.Last(); string customId = Ribbon.GetCommandId(customTab)!;
                    Assert.True(Ribbon.GetIsCustom(customTab)); Assert.Equal(2, customTab.Groups.Count);
                    Assert.All(customTab.Groups, item => Assert.Single(item.Items.Cast<object>()));
                    Assert.Same(first, Ribbon.GetQuickAccessSource((FrameworkElement)customTab.Groups.Last().Items[0]));
                    string structured = RibbonCustomizationSerializer.Serialize(ribbon);
                    Assert.Contains("\"GroupId\"", structured);
                    ribbon.LayoutMode = RibbonLayoutMode.Classic; Layout(dialog); Assert.Equal(2, ribbon.Tabs.Count);
                    ribbon.LayoutMode = RibbonLayoutMode.Simplified; RibbonCustomizationSerializer.Apply(ribbon, structured); Layout(dialog);
                    var restored = ribbon.Tabs.Single(item => Ribbon.GetCommandId(item) == customId);
                    Assert.NotSame(customTab, restored); Assert.Equal(2, restored.Groups.Count);
                    Assert.All(restored.Groups, item => Assert.Single(item.Items.Cast<object>()));
                    Part<Selector>(page, "PART_TabSelector").SelectedIndex = 0; Layout(dialog);
                    Click(page, "PART_ResetAllButton"); page.ApplyChanges(); Layout(dialog); Assert.Single(current.Items.Cast<object>());
                    Assert.Equal(2, restored.Groups.Count); Assert.All(restored.Groups, item => Assert.Single(item.Items.Cast<object>()));
                    cases++;
                }
                finally { dialog.Close(); Layout(dialog); }
            }
            Console.WriteLine($"Simplified customization independent consumer: {cases} theme/dark/density/direction/root-DPI combinations passed.");
        }
        finally { RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle; ThemeManager.SetDarkMode(application, false); }
    }

    private static T Part<T>(Control control, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));
    private static void Click(Control control, string name) => Part<ButtonBase>(control, name).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
    private static void Layout(Window window) { Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout(); }

    private static void Rename(Control page, string part, string caption)
    {
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            var dialog = Application.Current.Windows.OfType<RibbonCustomizeEditDialog>().Single(); dialog.ApplyTemplate();
            Part<TextBox>(dialog, "PART_NameBox").Text = caption; Click(dialog, "PART_OkButton");
        }));
        Click(page, part);
    }

    private static void Save(Window window, string name)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "TestResults", "simplified-customization"); Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
}
