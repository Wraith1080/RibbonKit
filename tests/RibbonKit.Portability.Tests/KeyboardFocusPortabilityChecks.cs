using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

internal static class KeyboardFocusPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var plain = new RibbonApplicationMenuItem { Header = "Save" };
        var disabled = new RibbonApplicationMenuItem { Header = "Disabled", IsEnabled = false };
        var paneAction = new RibbonApplicationMenuPaneItem { Content = "Save a copy" };
        var split = new RibbonApplicationMenuItem { Header = "Save As", Content = paneAction };
        var dropdown = new RibbonApplicationMenuItem
        {
            Header = "Print", IsSplit = false,
            Content = new RibbonApplicationMenuPaneItem { Content = "Print preview" },
        };
        var footer = new RibbonApplicationMenuButton { Content = "Options" };
        var menu = new RibbonApplicationMenu { FooterContent = footer };
        menu.Items.Add(plain);
        menu.Items.Add(disabled);
        menu.Items.Add(new RibbonApplicationMenuSeparator());
        menu.Items.Add(split);
        menu.Items.Add(dropdown);
        var ribbon = new Ribbon { ApplicationMenu = menu };
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        var document = new TextBox();
        var root = new DockPanel();
        DockPanel.SetDock(ribbon, Dock.Top);
        root.Children.Add(ribbon);
        root.Children.Add(document);
        var window = new Window
        {
            Content = root, Width = 720, Height = 650,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false,
        };
        int plainClicks = 0;
        int splitClicks = 0;
        int commandExecutions = 0;
        var saveCommand = new RoutedCommand();
        plain.Command = saveCommand;
        plain.CommandParameter = "save-parameter";
        window.CommandBindings.Add(new CommandBinding(saveCommand, (_, e) =>
        {
            Assert.Equal("save-parameter", e.Parameter);
            commandExecutions++;
        }, (_, e) => { e.CanExecute = true; e.Handled = true; }));
        plain.Click += (_, _) => plainClicks++;
        split.Click += (_, _) => splitClicks++;
        try
        {
            window.Show();
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (FlowDirection direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                ribbon.FlowDirection = direction;
                ribbon.IsBackstageOpen = true;
                Drain();
                window.UpdateLayout();

                Assert.False(ribbon.Focusable);
                Assert.False(ribbon.IsTabStop);
                Assert.False(menu.IsTabStop);
                Assert.False(plain.Focusable);
                Button primary = Part(plain, "PART_Primary");
                Button splitPrimary = Part(split, "PART_Primary");
                Button arrow = Part(split, "PART_Arrow");
                Button dropdownPrimary = Part(dropdown, "PART_Primary");
                Assert.Same(primary, Keyboard.FocusedElement);
                Assert.NotNull(primary.FocusVisualStyle);
                Assert.Same(primary.FocusVisualStyle, arrow.FocusVisualStyle);
                Assert.Same(primary.FocusVisualStyle, footer.FocusVisualStyle);
                Assert.Same(primary.FocusVisualStyle, paneAction.FocusVisualStyle);
                string? reviewName = theme == RibbonTheme.CrystalLight
                    && direction == FlowDirection.LeftToRight
                    ? $"crystal-{(dark ? "dark" : "light")}" : null;
                VerifyFocusVisual(primary, reviewName is null ? null : $"{reviewName}-command");

                // Native WPF traversal must skip containers, separators and disabled rows.
                Next(primary, splitPrimary);
                Assert.Same(split, menu.ActiveItem);
                Next(splitPrimary, arrow);
                VerifyFocusVisual(arrow, reviewName is null ? null : $"{reviewName}-arrow");
                Previous(arrow, splitPrimary);
                Next(splitPrimary, arrow);
                Next(arrow, dropdownPrimary);
                VerifyFocusVisual(dropdownPrimary, reviewName is null ? null : $"{reviewName}-dropdown");
                Assert.Same(dropdown, menu.ActiveItem);
                Assert.False(Part(dropdown, "PART_Arrow").IsTabStop);
                Assert.False(Part(dropdown, "PART_Arrow").Focusable);
                var printAction = Assert.IsType<RibbonApplicationMenuPaneItem>(dropdown.Content);
                Next(dropdownPrimary, printAction);
                Next(printAction, footer);
                Next(footer, primary);
                Previous(primary, footer);
                Assert.False(document.IsKeyboardFocusWithin);

                Assert.True(primary.Focus());
                int before = plainClicks;
                Press(primary, Key.Enter);
                Assert.Equal(before + 1, plainClicks);
                Assert.Equal(plainClicks, commandExecutions);
                Assert.False(ribbon.IsApplicationMenuOpen);

                ribbon.IsBackstageOpen = true;
                Drain();
                Assert.True(primary.Focus());
                before = plainClicks;
                Press(primary, Key.Space);
                Assert.Equal(before + 1, plainClicks);
                Assert.Equal(plainClicks, commandExecutions);
                Assert.False(ribbon.IsApplicationMenuOpen);

                ribbon.IsBackstageOpen = true;
                Drain();
                Assert.True(splitPrimary.Focus());
                before = splitClicks;
                Press(splitPrimary, Key.Enter);
                Assert.Equal(before + 1, splitClicks);
                Assert.False(ribbon.IsApplicationMenuOpen);

                ribbon.IsBackstageOpen = true;
                Drain();
                Assert.True(arrow.Focus());
                int arrowClicks = 0;
                RoutedEventHandler recordArrow = (_, _) => arrowClicks++;
                arrow.AddHandler(Button.ClickEvent, recordArrow, handledEventsToo: true);
                before = splitClicks;
                Press(arrow, Key.Enter);
                Assert.Equal(1, arrowClicks);
                Assert.Equal(before, splitClicks);
                Assert.Same(split, menu.ActiveItem);
                Assert.True(ribbon.IsApplicationMenuOpen);
                Press(arrow, Key.Space);
                Assert.Equal(2, arrowClicks);
                arrow.RemoveHandler(Button.ClickEvent, recordArrow);
                Assert.Equal(before, splitClicks);
                Assert.True(ribbon.IsApplicationMenuOpen);
                Assert.True(dropdownPrimary.Focus());
                Press(dropdownPrimary, Key.Enter);
                Assert.Same(dropdown, menu.ActiveItem);
                Assert.True(ribbon.IsApplicationMenuOpen);
                Press(dropdownPrimary, Key.Escape);
                Assert.False(ribbon.IsApplicationMenuOpen);
            }
        }
        finally
        {
            window.Close();
            ThemeManager.SetDarkMode(application, false);
        }
    }

    private static Button Part(RibbonApplicationMenuItem item, string name) =>
        Assert.IsType<Button>(item.Template.FindName(name, item));

    private static void VerifyFocusVisual(Control target, string? reviewName = null)
    {
        // Exercise WPF's real focus adorner in the library-only window. Forcing its display
        // substitutes for the physical keyboard input mode that an offscreen fixture lacks.
        var force = typeof(KeyboardNavigation).GetProperty("AlwaysShowFocusVisual",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        object? previous = force.GetValue(null);
        try
        {
            force.SetValue(null, true);
            typeof(KeyboardNavigation).GetMethod("ShowFocusVisual",
                BindingFlags.Static | BindingFlags.NonPublic, Type.EmptyTypes)!.Invoke(null, null);
        }
        finally { force.SetValue(null, previous); }
        Window.GetWindow(target)!.UpdateLayout();
        var layer = AdornerLayer.GetAdornerLayer(target)!;
        Adorner adorner = Assert.Single(layer.GetAdorners(target)!);
        var ring = FindBorder(adorner);
        Assert.NotNull(ring);
        Assert.Same(target.FindResource("RibbonKit.Brushes.Input.FocusBorder"), ring.BorderBrush);
        Rect bounds = ring.TransformToAncestor(adorner).TransformBounds(new Rect(ring.RenderSize));
        Assert.True(bounds.Left >= 0 && bounds.Top >= 0
            && bounds.Right <= target.ActualWidth && bounds.Bottom <= target.ActualHeight);
        string? reviewDirectory = Environment.GetEnvironmentVariable("RIBBONKIT_FOCUS_DIAGNOSTICS");
        if (reviewName is not null && !string.IsNullOrEmpty(reviewDirectory))
        {
            var window = Window.GetWindow(target)!;
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),
                (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            Directory.CreateDirectory(reviewDirectory);
            using var file = File.Create(Path.Combine(reviewDirectory, $"{reviewName}.png"));
            encoder.Save(file);
        }
    }

    private static Border? FindBorder(DependencyObject node)
    {
        if (node is Border border) return border;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++)
        {
            Border? found = FindBorder(VisualTreeHelper.GetChild(node, index));
            if (found is not null) return found;
        }
        return null;
    }

    private static void Next(UIElement from, UIElement to)
    {
        Drain();
        Window.GetWindow(from)?.UpdateLayout();
        Assert.True(from.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
        Assert.Same(to, Keyboard.FocusedElement);
    }

    private static void Previous(UIElement from, UIElement to)
    {
        Drain();
        Window.GetWindow(from)?.UpdateLayout();
        Assert.True(from.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous)));
        Assert.Same(to, Keyboard.FocusedElement);
    }

    private static void Press(UIElement target, Key key)
    {
        var source = PresentationSource.FromVisual(target)!;
        var preview = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
        { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        target.RaiseEvent(preview);
        if (!preview.Handled)
            target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
            { RoutedEvent = Keyboard.KeyDownEvent });
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
        { RoutedEvent = Keyboard.KeyUpEvent });
        Drain();
    }

    private static void Drain() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
}
