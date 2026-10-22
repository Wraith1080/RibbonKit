using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Native editor popups, with no Showcase resources, helpers or subclasses.
internal static class TextEditMenuChecks
{
    internal static void Verify(Application application)
    {
        var textBox = new RibbonTextBox { Text = "Quarterly report" };
        var comboBox = new RibbonComboBox { IsEditable = true, Text = "Calibri" };
        var window = new Window { Width = 440, Height = 250, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false,
            Content = new StackPanel { Children = { textBox, comboBox } } };
        ContextMenu? menu = null;
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            window.Show(); Layout();
            var comboEditor = Assert.IsType<TextBox>(comboBox.Template.FindName("PART_EditableTextBox", comboBox));
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                var direction = density == RibbonDensity.Touch ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                textBox.FlowDirection = comboBox.FlowDirection = direction;
                Ribbon.SetDensity(textBox, density); Ribbon.SetDensity(comboBox, density);
                Layout();
                // Refresh after a theme template change; never patch template parts.
                comboEditor = Assert.IsType<TextBox>(comboBox.Template.FindName("PART_EditableTextBox", comboBox));
                foreach (TextBox editor in new TextBox[] { textBox, comboEditor })
                {
                    editor.Select(1, 3);
                    string selection = editor.SelectedText;
                    menu = Open(editor); Layout();
                    Assert.NotNull(menu.Style);
                    Assert.Equal(direction, menu.FlowDirection);
                    Assert.Equal(density, Ribbon.GetDensity(menu));
                    Assert.Equal(selection, editor.SelectedText);
                    Assert.Equal(1, editor.SelectionStart); Assert.Equal(3, editor.SelectionLength);
                    var surface = Descendants(menu).OfType<Border>().First();
                    Assert.Same(editor.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), surface.Background);
                    Assert.Same(editor.FindResource("RibbonKit.Brushes.Popup.Border"), surface.BorderBrush);
                    Assert.Equal(editor.FindResource("RibbonKit.Metrics.PopupCornerRadius"), surface.CornerRadius);
                    Assert.Same(editor.FindResource("RibbonKit.Brushes.Text.Primary"), menu.Foreground);
                    foreach (var command in new[] { ApplicationCommands.Cut, ApplicationCommands.Copy, ApplicationCommands.Paste })
                    {
                        var item = Assert.Single(menu.Items.OfType<MenuItem>(), item => ReferenceEquals(item.Command, command));
                        Assert.Same(editor, item.CommandTarget);
                        Assert.NotNull(item.Style);
                        var chrome = Assert.IsType<Border>(item.Template.FindName("Chrome", item));
                        var gesture = Assert.IsType<TextBlock>(item.Template.FindName("Gesture", item));
                        Assert.Equal(item.InputGestureText, gesture.Text);
                        Assert.True(gesture.ActualWidth > 0);
                        Rect bounds = gesture.TransformToAncestor(chrome).TransformBounds(new Rect(gesture.RenderSize));
                        Assert.True(bounds.Left >= 0 && bounds.Right <= chrome.ActualWidth,
                            $"{theme}/{dark}/{density}: shortcut clipped ({bounds}, width {chrome.ActualWidth}).");
                        if (density == RibbonDensity.Touch)
                            Assert.True(item.ActualHeight >= (double)editor.FindResource("RibbonKit.Metrics.Touch.TargetSize"));
                    }
                    // Save compact LTR combo and touch RTL text-box appearances for inspection.
                    if ((density == RibbonDensity.Compact) == (editor == comboEditor))
                        Save(menu, $"{theme}-{(dark ? "dark" : "light")}-{density}");
                    menu.IsOpen = false; Layout(); menu = null;
                }
            }

            // Dynamic resources keep an already open native popup in sync.
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            ThemeManager.SetDarkMode(application, false); Layout();
            menu = Open(textBox); Layout();
            var liveSurface = Descendants(menu).OfType<Border>().First();
            ThemeManager.SetDarkMode(application, true); Layout();
            Assert.Same(textBox.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), liveSurface.Background);
            ThemeManager.Apply(application, RibbonTheme.CrystalLight); Layout();
            Assert.Same(textBox.FindResource("RibbonKit.Brushes.Popup.Border"), liveSurface.BorderBrush);
            Assert.Equal(textBox.FindResource("RibbonKit.Metrics.PopupCornerRadius"), liveSurface.CornerRadius);
            var scopedSurface = new SolidColorBrush(Colors.DarkSlateBlue);
            textBox.Resources["RibbonKit.Brushes.Ribbon.ContentBackground"] = scopedSurface; Layout();
            Assert.Same(scopedSurface, liveSurface.Background);

            void Layout()
            {
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                window.UpdateLayout();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            }
        }
        finally
        {
            if (menu is not null) menu.IsOpen = false;
            window.Close(); ThemeManager.SetDarkMode(application, false);
        }
    }

    private static ContextMenu Open(TextBox editor)
    {
        var args = (ContextMenuEventArgs)typeof(ContextMenuEventArgs)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(ctor => ctor.GetParameters().Length == 2).Invoke(new object[] { editor, true });
        editor.RaiseEvent(args);
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        Assert.True(args.Handled);
        return Assert.Single(PresentationSource.CurrentSources.Cast<PresentationSource>().OfType<HwndSource>()
            .SelectMany(source => Descendants(source.RootVisual)).OfType<ContextMenu>(), menu => menu.IsOpen);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        yield return root;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var child in Descendants(VisualTreeHelper.GetChild(root, index))) yield return child;
    }

    private static void Save(FrameworkElement menu, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_TEXT_EDIT_MENU_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var dpi = VisualTreeHelper.GetDpi(menu);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(menu.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(menu.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(menu);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
}
