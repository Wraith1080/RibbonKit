using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;
using Ellipse = System.Windows.Shapes.Ellipse;

namespace RibbonKit.Portability.Tests;

internal static class KeyboardFocusPortabilityChecks
{
    private static string? _lastInput;

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
        var toggle = new RibbonToggleButton { Header = "Colored title bar", IsChecked = true };
        var tile = new RibbonGalleryItem { Content = "Auto", IsSelected = true };
        var gallery = new InRibbonGallery();
        gallery.Items.Add(tile);
        var group = new RibbonGroup { Header = "Focus surfaces" };
        group.Items.Add(toggle);
        group.Items.Add(gallery);
        var home = new RibbonTab { Header = "Home" };
        home.Groups.Add(group);
        ribbon.Tabs.Add(home);
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

                var tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
                var file = Assert.IsType<ToggleButton>(tabs.Template.FindName("PART_ApplicationButton", tabs));
                foreach (var shape in new[] { RibbonApplicationButtonShape.Orb, RibbonApplicationButtonShape.Tab })
                {
                    ribbon.ApplicationButtonShape = shape;
                    Drain();
                    window.UpdateLayout();
                    Assert.True(document.Focus());
                    Assert.True(file.Focus());
                    VerifyFocusVisual(file);
                }
                if (theme is RibbonTheme.Office2013 or RibbonTheme.Office2019)
                {
                    // All color and geometry come from RibbonKit in this consumer:
                    // no Showcase resources, templates or focus helper participate.
                    var collapse = Assert.IsType<ToggleButton>(tabs.Template.FindName("MinimizeToggle", tabs));
                    foreach (bool colored in new[] { true, false })
                    {
                        ThemeManager.SetAccentedTitleBar(application, colored);
                        Drain();
                        window.UpdateLayout();
                        foreach (var target in new Control[] { file, home, collapse })
                        {
                            Assert.True(target.Focus());
                            VerifyFocusVisual(target, $"contrast-{theme}-{dark}-{direction}-{colored}-{target.GetType().Name}", window);
                        }
                    }
                    var backstage = new Backstage { Design = RibbonBackstageDesign.Classic };
                    var row = new BackstageTabItem { Header = "Home", Content = new Button { Content = "Page action" } };
                    backstage.Items.Add(row);
                    ribbon.ApplicationMenu = null;
                    ribbon.Backstage = backstage;
                    ribbon.IsBackstageOpen = true;
                    Drain();
                    window.UpdateLayout();
                    Assert.True(row.Focus());
                    VerifyFocusVisual(row, $"contrast-{theme}-{dark}-{direction}-nav", window);
                    var back = Assert.IsType<Button>(backstage.Template.FindName("PART_BackButton", backstage));
                    Assert.True(back.Focus());
                    VerifyFocusVisual(back, $"contrast-{theme}-{dark}-{direction}-back", window);
                    ribbon.IsBackstageOpen = false;
                    ribbon.Backstage = null;
                    Drain();
                    ribbon.ApplicationMenu = menu;
                }
                ribbon.ClearValue(Ribbon.ApplicationButtonShapeProperty);
                foreach (var target in new Control[] { toggle, tile })
                {
                    Assert.True(target.Focus());
                    VerifyFocusVisual(target, reviewName is null ? null : $"{reviewName}-{target.GetType().Name}", window);
                }
            }
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"{ThemeManager.CurrentTheme}, dark={ThemeManager.IsDarkMode}, "
                + $"direction={ribbon.FlowDirection}: {_lastInput}", exception);
        }
        finally
        {
            window.Close();
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.SetAccentedTitleBar(application, false);
        }
    }

    private static Button Part(RibbonApplicationMenuItem item, string name) =>
        Assert.IsType<Button>(item.Template.FindName(name, item));

    internal static void VerifyFocusVisual(Control target, string? reviewName = null, Window? owner = null)
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
        var window = owner ?? Window.GetWindow(target);
        Assert.NotNull(window);
        window.UpdateLayout();
        // The real focus template attaches its geometry tracker on Loaded. Process
        // that lifecycle event before inspecting the newly created native adorner.
        Drain();
        window.UpdateLayout();
        var layer = AdornerLayer.GetAdornerLayer(target);
        Assert.NotNull(layer);
        var adorners = layer.GetAdorners(target);
        Assert.NotNull(adorners);
        Adorner adorner = Assert.Single(adorners);
        var sphere = FindNamed<Ellipse>(target, "OrbFill");
        if (target.Template.FindName("Outline", target) is Ellipse || sphere is { IsVisible: true })
        {
            var ring = FindNamed<Ellipse>(adorner, "FocusRing");
            Assert.NotNull(ring);
            VerifyBrush(ring.Stroke);
            FrameworkElement face = sphere is { IsVisible: true } ? sphere : target;
            Rect bounds = ring.TransformToVisual(adorner).TransformBounds(new Rect(ring.RenderSize));
            Rect expected = face.TransformToVisual(adorner).TransformBounds(
                new Rect(2, 2, face.ActualWidth - 4, face.ActualHeight - 4));
            Assert.True(Math.Abs(bounds.Left - expected.Left) < 0.1 && Math.Abs(bounds.Top - expected.Top) < 0.1
                && Math.Abs(bounds.Width - expected.Width) < 0.1 && Math.Abs(bounds.Height - expected.Height) < 0.1,
                $"{reviewName}/{target.GetType().Name}: ring={bounds}, disc inset={expected}");
            Assert.Equal(ring.ActualWidth, ring.ActualHeight, 5);
        }
        else
        {
            var ring = FindBorder(adorner);
            Assert.NotNull(ring);
            VerifyBrush(ring.BorderBrush);
            var chrome = (target.Template.FindName("Chrome", target)
                ?? target.Template.FindName("HeaderChrome", target)) as Border;
            FrameworkElement surface = chrome is { IsVisible: true } ? chrome : target;
            // Popup commands have a separate presentation root from their owning window.
            Rect bounds = ring.TransformToVisual(adorner).TransformBounds(new Rect(ring.RenderSize));
            Rect expected = surface.TransformToVisual(adorner).TransformBounds(
                new Rect(2, 2, surface.ActualWidth - 4, surface.ActualHeight - 4));
            Assert.True(Math.Abs(bounds.Left - expected.Left) < 0.1 && Math.Abs(bounds.Top - expected.Top) < 0.1
                && Math.Abs(bounds.Width - expected.Width) < 0.1 && Math.Abs(bounds.Height - expected.Height) < 0.1,
                $"{reviewName}/{target.GetType().Name}: ring={bounds}, surface inset={expected}, loaded={ring.IsLoaded}");
            if (surface is Border border)
                Assert.Equal(new CornerRadius(Math.Max(0, border.CornerRadius.TopLeft - 2),
                    Math.Max(0, border.CornerRadius.TopRight - 2), Math.Max(0, border.CornerRadius.BottomRight - 2),
                    Math.Max(0, border.CornerRadius.BottomLeft - 2)), ring.CornerRadius);
        }
        string? reviewDirectory = Environment.GetEnvironmentVariable("RIBBONKIT_FOCUS_DIAGNOSTICS");
        if (reviewName is not null && !string.IsNullOrEmpty(reviewDirectory))
        {
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),
                (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            Directory.CreateDirectory(reviewDirectory);
            using var file = File.Create(Path.Combine(reviewDirectory, $"{reviewName}.png"));
            encoder.Save(file);
        }

        void VerifyBrush(Brush actual)
        {
            Brush? backdrop = null;
            if (target is RibbonTab || target.Name == "MinimizeToggle"
                || target.Name == "PART_ApplicationButton" && sphere is not { IsVisible: true })
                backdrop = (Brush)target.FindResource("RibbonKit.Brushes.Ribbon.Background");
            else if (target is BackstageTabItem && Backstage.GetDesign(target) == RibbonBackstageDesign.Classic)
                backdrop = (Brush)target.FindResource("RibbonKit.Brushes.Backstage.Classic.NavBackground");
            else if (target.Name == "PART_BackButton"
                && target.TemplatedParent is Backstage ownerBackstage
                && ownerBackstage.Template.FindName("NavColumn", ownerBackstage) is Border rail)
                backdrop = rail.Background;

            if (backdrop is not SolidColorBrush { Color.A: 255, Opacity: 1 } band)
            {
                Assert.Same(target.FindResource("RibbonKit.Brushes.Input.FocusBorder"), actual);
                return;
            }
            var chrome = (target.Template.FindName("Chrome", target)
                ?? target.Template.FindName("HeaderChrome", target)) as Border;
            Color background = chrome?.Background is SolidColorBrush { Color.A: 255, Opacity: 1 } fill
                ? fill.Color : band.Color;
            Color stroke = Assert.IsType<SolidColorBrush>(actual).Color;
            double light = Luminance(stroke), dark = Luminance(background);
            double contrast = (Math.Max(light, dark) + 0.05) / (Math.Min(light, dark) + 0.05);
            Assert.True(contrast >= 3, $"{target.GetType().Name}/{target.Name}: outline={stroke}, surface={background}, contrast={contrast}");
        }
    }

    private static double Luminance(Color color) =>
        0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);

    private static double Linear(byte channel)
    {
        double value = channel / 255d;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
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

    private static T? FindNamed<T>(DependencyObject node, string name) where T : FrameworkElement
    {
        if (node is T element && element.Name == name) return element;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++)
            if (FindNamed<T>(VisualTreeHelper.GetChild(node, index), name) is { } found) return found;
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
        _lastInput = $"key={key}, modifiers={Keyboard.Modifiers}, target={target.GetType().Name}, "
            + $"focused={target.IsKeyboardFocused}, window active={Window.GetWindow(target)?.IsActive}";
        var source = PresentationSource.FromVisual(target)!;
        var preview = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
        { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        target.RaiseEvent(preview);
        if (!preview.Handled)
            target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
            { RoutedEvent = Keyboard.KeyDownEvent });
        _lastInput += $", down pressed={(target as ButtonBase)?.IsPressed}, down focused={target.IsKeyboardFocused}";
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
        { RoutedEvent = Keyboard.KeyUpEvent });
        Drain();
        _lastInput += $", up modifiers={Keyboard.Modifiers}, up focused={target.IsKeyboardFocused}";
    }

    private static void Drain() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
}
