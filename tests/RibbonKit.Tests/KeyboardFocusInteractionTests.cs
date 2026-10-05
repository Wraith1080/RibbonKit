using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Ellipse = System.Windows.Shapes.Ellipse;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public class KeyboardFocusInteractionTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2013, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.Office2013, FlowDirection.RightToLeft)]
    [InlineData(RibbonTheme.Office2019, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.Office2019, FlowDirection.RightToLeft)]
    public void Native_focus_remains_visible_on_accent_rails_and_colored_headers(RibbonTheme theme, FlowDirection flow) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        var animationLevel = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        ThemeManager.Apply(application, theme);
        var home = new BackstageTabItem { Header = "Home", Content = new Button { Content = "Page action" } };
        var info = new BackstageTabItem { Header = "Info", Content = new Button { Content = "Info action" } };
        var backstage = new Backstage { Design = RibbonBackstageDesign.Classic };
        backstage.Items.Add(home);
        backstage.Items.Add(info);
        var ribbon = new Ribbon { Backstage = backstage, ApplicationButtonShape = RibbonApplicationButtonShape.Tab };
        var tab = new RibbonTab { Header = "Home" };
        ribbon.Tabs.Add(tab);
        ribbon.Tabs.Add(new RibbonTab { Header = "Other" });
        var document = new TextBox();
        var content = new DockPanel();
        DockPanel.SetDock(ribbon, Dock.Top);
        content.Children.Add(ribbon);
        content.Children.Add(document);
        var window = CreateWindow(new AdornerDecorator { Child = content }, flow);
        window.Height = 600;
        try
        {
            window.Show();
            foreach (bool dark in new[] { false, true })
            foreach (Color accent in new[] { Color.FromRgb(43, 87, 154), Color.FromRgb(138, 62, 129), Color.FromRgb(255, 224, 138) })
            {
                ThemeManager.SetDarkMode(application, dark);
                ThemeManager.SetAccent(application, accent);
                ThemeManager.SetAccentedTitleBar(application, true);
                Sta.Drain();
                window.UpdateLayout();
                var tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
                var file = Assert.IsType<ToggleButton>(tabs.Template.FindName("PART_ApplicationButton", tabs));
                var collapse = Assert.IsType<ToggleButton>(tabs.Template.FindName("MinimizeToggle", tabs));
                Color band = ColorOf(ribbon.FindResource("RibbonKit.Brushes.Ribbon.Background"));
                AssertVisible(file, EffectiveBackground(file, band), "file");
                AssertVisible(collapse, EffectiveBackground(collapse, band), "collapse");
                var header = Assert.IsType<Border>(tab.Template.FindName("HeaderChrome", tab));
                AssertVisible(tab, band, "selected-tab");
                // Exercise the same header on the colored band, independent of WPF's
                // automatic selection when a tab header gains keyboard focus.
                header.Background = Brushes.Transparent;
                AssertVisible(tab, band, "band-tab");
                header.ClearValue(Border.BackgroundProperty);
                Assert.True(collapse.Focus());
                ShowFocusVisual();
                ThemeManager.SetAccentedTitleBar(application, false);
                Sta.Drain();
                window.UpdateLayout();
                AssertVisible(collapse, EffectiveBackground(collapse,
                    ColorOf(ribbon.FindResource("RibbonKit.Brushes.Ribbon.Background"))), "uncolored-collapse", refocus: false);

                ribbon.IsBackstageOpen = true;
                Sta.Drain();
                window.UpdateLayout();
                Color rail = ColorOf(((Border)backstage.Template.FindName("NavColumn", backstage)).Background);
                foreach (var item in new[] { home, info })
                {
                    AssertVisible(item, EffectiveBackground(item, rail), "nav-" + item.Header);
                    var chrome = Assert.IsType<Border>(item.Template.FindName("Chrome", item));
                    chrome.Background = (Brush)item.FindResource("RibbonKit.Brushes.Backstage.ItemHoverBackground");
                    AssertVisible(item, ColorOf(chrome.Background), "hover-" + item.Header, refocus: false);
                    chrome.ClearValue(Border.BackgroundProperty);
                }
                var back = Assert.IsType<Button>(backstage.Template.FindName("PART_BackButton", backstage));
                AssertVisible(back, rail, "back");
                ribbon.IsBackstageOpen = false;
                Sta.Drain();
            }
        }
        finally { window.Close(); Sta.ResetApplication(); RibbonAnimation.GlobalLevel = animationLevel; }

        void AssertVisible(Control target, Color background, string name, bool refocus = true)
        {
            if (refocus) Assert.True(target.Focus());
            ShowFocusVisual();
            window.UpdateLayout();
            PumpFrames();
            var adorner = Assert.Single(AdornerLayer.GetAdornerLayer(target)!.GetAdorners(target)!);
            Brush stroke = FindNamed<Ellipse>(adorner, "FocusRing")?.Stroke ?? FindBorder(adorner)!.BorderBrush;
            background = EffectiveBackground(target, background);
            double contrast = Contrast(ColorOf(stroke), background);
            SaveReview(window, $"contrast-{theme}-{flow}-{ThemeManager.IsDarkMode}-{ColorOf(target.FindResource("RibbonKit.Brushes.Accent"))}-{name}");
            Assert.True(contrast >= 3, $"{name}: outline={ColorOf(stroke)}, background={background}, contrast={contrast}");
        }
    });

    private static Color EffectiveBackground(Control target, Color band) =>
        (target.Template.FindName("Chrome", target) ?? target.Template.FindName("HeaderChrome", target))
            is Border { Background: SolidColorBrush brush } && brush.Color.A == 255
            ? brush.Color : band;

    private static Color ColorOf(object brush) => Assert.IsType<SolidColorBrush>(brush).Color;

    private static double Contrast(Color a, Color b)
    {
        double x = Luminance(a), y = Luminance(b);
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
        static double Luminance(Color color) => 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
        static double Linear(byte value)
        {
            double channel = value / 255d;
            return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }
    }

    [Theory]
    [InlineData(RibbonTheme.Office2024, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.Office2024, FlowDirection.RightToLeft)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.RightToLeft)]
    public void Native_focus_uses_the_visible_chrome_bounds_and_corners(RibbonTheme theme, FlowDirection flow) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        ThemeManager.Apply(application, theme);
        var toggle = new RibbonToggleButton { Header = "Colored title bar", Width = 130, Height = 90, IsChecked = true };
        var tile = new RibbonGalleryItem { Content = "Auto", Width = 100, Height = 65, IsSelected = true };
        var crystalTemplates = new ResourceDictionary
            { Source = new Uri("/RibbonKit;component/Themes/Controls.CrystalBackstage.xaml", UriKind.Relative) };
        var row = new BackstageTabItem { Header = "Home", Width = 220,
            Style = (Style)crystalTemplates["Crystal.Backstage.Tab"], IsSelected = true };
        var content = new StackPanel { Margin = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Left };
        content.Children.Add(toggle);
        content.Children.Add(tile);
        content.Children.Add(row);
        var window = CreateWindow(new AdornerDecorator { Child = content }, flow);
        window.Height = 400;
        try
        {
            window.Show();
            Sta.Drain();
            window.UpdateLayout();
            foreach (var target in new Control[] { toggle, tile, row })
            {
                Assert.True(target.Focus());
                ShowFocusVisual();
                window.UpdateLayout();
                PumpFrames();
                var chrome = Assert.IsType<Border>(target.Template.FindName("Chrome", target));
                AssertChrome(chrome, target.GetType().Name);
                // Geometry must refresh after a template surface changes while it retains focus.
                chrome.Margin = new Thickness(13, 4, 7, 9);
                chrome.CornerRadius = new CornerRadius(18, 12, 6, 3);
                chrome.RenderTransform = new TransformGroup { Children =
                    { new ScaleTransform(0.8, 0.9), new RotateTransform(7), new TranslateTransform(5, 3) } };
                PumpFrames();
                AssertChrome(chrome, "changed-" + target.GetType().Name);
            }
        }
        finally { window.Close(); Sta.ResetApplication(); }

        void AssertChrome(Border chrome, string name)
        {
            var adorner = Assert.Single(AdornerLayer.GetAdornerLayer(chrome)!.GetAdorners((UIElement)chrome.TemplatedParent)!);
            var ring = FindBorder(adorner);
            Assert.NotNull(ring);
            Rect actual = ring.TransformToVisual(window).TransformBounds(new Rect(ring.RenderSize));
            Rect expected = chrome.TransformToVisual(window).TransformBounds(
                new Rect(2, 2, chrome.ActualWidth - 4, chrome.ActualHeight - 4));
            SaveReview(window, $"chrome-{theme}-{flow}-{name}");
            Assert.True(Math.Abs(actual.Left - expected.Left) < 0.1 && Math.Abs(actual.Top - expected.Top) < 0.1
                && Math.Abs(actual.Width - expected.Width) < 0.1 && Math.Abs(actual.Height - expected.Height) < 0.1,
                $"{name}: ring={actual}, chrome inset={expected}");
            Assert.Equal(new CornerRadius(Math.Max(0, chrome.CornerRadius.TopLeft - 2),
                Math.Max(0, chrome.CornerRadius.TopRight - 2), Math.Max(0, chrome.CornerRadius.BottomRight - 2),
                Math.Max(0, chrome.CornerRadius.BottomLeft - 2)), ring.CornerRadius);
        }
    });

    private static void SaveReview(Window window, string name)
    {
        var directory = Environment.GetEnvironmentVariable("RIBBONKIT_FOCUS_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),
            (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var output = File.Create(Path.Combine(directory, name + ".png"));
        encoder.Save(output);
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Circular_focus_follows_back_discs_real_orbs_and_classic_proxies(FlowDirection flow) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        var animationLevel = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        ThemeManager.Apply(application, RibbonTheme.Office2007);
        var page = new BackstageTabItem { Header = "Home", Content = new Button { Content = "Page action" } };
        var backstage = new Backstage();
        backstage.Items.Add(page);
        Backstage.SetDesign(backstage, RibbonBackstageDesign.Classic2007);
        var ribbon = new Ribbon { Backstage = backstage };
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        var document = new TextBox();
        var content = new DockPanel { Margin = new Thickness(0, 35, 0, 0) };
        DockPanel.SetDock(ribbon, Dock.Top);
        content.Children.Add(ribbon);
        content.Children.Add(document);
        var window = CreateWindow(new AdornerDecorator { Child = content }, flow);
        window.Height = 600;
        try
        {
            window.Show();
            Sta.Drain();
            window.UpdateLayout();
            var tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
            var file = Assert.IsType<ToggleButton>(tabs.Template.FindName("PART_ApplicationButton", tabs));
            var sphere = FindNamed<Ellipse>(file, "OrbFill");
            Assert.NotNull(sphere);
            Assert.True(file.Focus());
            AssertCircle(file, sphere, "orb");
            ribbon.IsBackstageOpen = true;
            Sta.Drain();
            window.UpdateLayout();
            var proxy = Assert.IsType<Button>(typeof(Ribbon).GetField("_classicBackstageOrbProxy",
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(ribbon));
            var proxySphere = FindNamed<Ellipse>(proxy, "OrbFill");
            Assert.NotNull(proxySphere);
            Assert.True(proxy.Focus());
            AssertCircle(proxy, proxySphere, "proxy");
            ribbon.IsBackstageOpen = false;
            Sta.Drain();
            PumpFrames();

            foreach (var theme in new[] { RibbonTheme.Office2024, RibbonTheme.Office2010 })
            {
                ThemeManager.Apply(application, theme);
                ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Tab;
                Backstage.SetDesign(backstage, theme == RibbonTheme.Office2024
                    ? RibbonBackstageDesign.Modern : RibbonBackstageDesign.Glass2007);
                Sta.Drain();
                ribbon.IsBackstageOpen = true;
                Sta.Drain();
                window.UpdateLayout();
                var back = Assert.IsType<Button>(backstage.Template.FindName("PART_BackButton", backstage));
                Assert.True(back.Focus(), $"{theme}: visible={back.IsVisible}, design={backstage.Design}, open={ribbon.IsBackstageOpen}");
                AssertCircle(back, back, $"back-{theme}");
                ribbon.IsBackstageOpen = false;
                Sta.Drain();
                PumpFrames();
            }
            // Returning to a File tab restores its ordinary rectangular focus style.
            Assert.True(document.Focus());
            window.UpdateLayout();
            tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
            file = Assert.IsType<ToggleButton>(tabs.Template.FindName("PART_ApplicationButton", tabs));
            Assert.True(file.Focus());
            ShowFocusVisual();
            window.UpdateLayout();
            Assert.NotNull(FindBorder(Assert.Single(AdornerLayer.GetAdornerLayer(file)!.GetAdorners(file)!)));
        }
        finally { window.Close(); Sta.ResetApplication(); RibbonAnimation.GlobalLevel = animationLevel; }

        void AssertCircle(Control target, FrameworkElement face, string name)
        {
            ShowFocusVisual();
            window.UpdateLayout();
            PumpFrames();
            var adorner = Assert.Single(AdornerLayer.GetAdornerLayer(target)!.GetAdorners(target)!);
            var ring = FindNamed<Ellipse>(adorner, "FocusRing");
            Assert.NotNull(ring);
            Assert.Same(target.FindResource("RibbonKit.Brushes.Input.FocusBorder"), ring.Stroke);
            Rect actual = ring.TransformToVisual(window).TransformBounds(new Rect(ring.RenderSize));
            Rect expected = face.TransformToVisual(window).TransformBounds(
                new Rect(2, 2, face.ActualWidth - 4, face.ActualHeight - 4));
            var directory = Environment.GetEnvironmentVariable("RIBBONKIT_FOCUS_DIAGNOSTICS");
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),
                    (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(window);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using var output = File.Create(Path.Combine(directory, $"circular-{name}-{flow}.png"));
                encoder.Save(output);
            }
            // Placement uses the realized disc, so native-focus layout rounding does
            // not introduce a separate approximation at fractional DPI.
            Assert.True(Math.Abs(actual.Left - expected.Left) < 0.1,
                $"{name}: ring={actual}, face inset={expected}, DPI={VisualTreeHelper.GetDpi(window).DpiScaleX}");
            Assert.InRange(Math.Abs(actual.Top - expected.Top), 0, 0.1);
            Assert.InRange(Math.Abs(actual.Width - expected.Width), 0, 0.1);
            Assert.InRange(Math.Abs(actual.Height - expected.Height), 0, 0.1);
            Assert.Equal(ring.ActualWidth, ring.ActualHeight, 5);
        }
    });

    [Theory]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.LeftToRight, -30d)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.RightToLeft, 30d)]
    [InlineData(RibbonTheme.Office2024, FlowDirection.LeftToRight, -30d)]
    [InlineData(RibbonTheme.Office2024, FlowDirection.RightToLeft, 30d)]
    public void Native_focus_outline_follows_backstage_motion_without_layout_input(
        RibbonTheme theme, FlowDirection flow, double offset) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        ThemeManager.Apply(application, theme);
        var target = new Button
        {
            Width = 120, Height = 40, Content = "Active page",
            FocusVisualStyle = (Style)application.FindResource("RibbonKit.KeyboardFocusVisual"),
        };
        var slide = new TranslateTransform(offset, 0);
        var scale = new ScaleTransform(1, 1);
        var surface = new Grid { RenderTransform = new TransformGroup { Children = { scale, slide } } };
        surface.Children.Add(target);
        var content = new Border();
        var decorator = new AdornerDecorator { Child = content };
        var window = CreateWindow(decorator, flow);
        var backstage = new BackstageAdorner(content, surface, new Ribbon { FlowDirection = flow });
        try
        {
            window.Show();
            decorator.AdornerLayer.Add(backstage);
            window.UpdateLayout();
            Assert.True(target.Focus());
            ShowFocusVisual();
            window.UpdateLayout();
            PumpFrames();
            AssertAligned();

            var clock = new DoubleAnimation(offset, 0, TimeSpan.FromSeconds(1)).CreateClock();
            slide.ApplyAnimationClock(TranslateTransform.XProperty, clock);
            clock.Controller!.Pause();
            clock.Controller.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(500), TimeSeekOrigin.BeginTime);
            PumpFrames();
            AssertAligned();

            slide.BeginAnimation(TranslateTransform.XProperty, null);
            slide.X = 0;
            scale.ScaleX = 0.8;
            scale.ScaleY = 0.7;
            PumpFrames();
            AssertAligned();

            // A new keyboard focus visual must track again after the previous one unloads.
            Keyboard.ClearFocus();
            PumpFrames();
            scale.ScaleX = scale.ScaleY = 1;
            slide.X = offset;
            Assert.True(target.Focus());
            ShowFocusVisual();
            PumpFrames();
            slide.X = 0;
            PumpFrames();
            AssertAligned();
        }
        finally
        {
            decorator.AdornerLayer.Remove(backstage);
            backstage.Detach();
            window.Close();
            Sta.ResetApplication();
        }

        void AssertAligned()
        {
            var adorner = Assert.Single(AdornerLayer.GetAdornerLayer(target)!.GetAdorners(target)!);
            var ring = FindBorder(adorner);
            Assert.NotNull(ring);
            Rect actual = ring.TransformToVisual(decorator).TransformBounds(new Rect(ring.RenderSize));
            Rect expected = target.TransformToVisual(decorator).TransformBounds(
                new Rect(2, 2, target.ActualWidth - 4, target.ActualHeight - 4));
            Assert.InRange(Math.Abs(actual.Left - expected.Left), 0, 0.1);
            Assert.InRange(Math.Abs(actual.Top - expected.Top), 0, 0.1);
            Assert.InRange(Math.Abs(actual.Width - expected.Width), 0, 0.1);
            Assert.InRange(Math.Abs(actual.Height - expected.Height), 0, 0.1);
        }
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Collapsed_group_enters_cycles_and_returns_keyboard_focus(FlowDirection flow) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        ThemeManager.Apply(application, RibbonTheme.CrystalLight);
        var first = new RibbonButton { Header = "First" };
        var last = new RibbonButton { Header = "Last" };
        var editor = new TextBox { Text = "Editable", Width = 100 };
        var dropdown = new RibbonDropDownButton { Header = "Nested", Size = RibbonControlSize.Medium };
        var nested = new RibbonMenuItem { Header = "Nested command" };
        dropdown.Items.Add(nested);
        var commands = new StackPanel();
        commands.Children.Add(new RibbonButton { Header = "Disabled", IsEnabled = false });
        commands.Children.Add(first);
        commands.Children.Add(dropdown);
        commands.Children.Add(editor);
        commands.Children.Add(last);
        var group = new RibbonGroup { Header = "Collapsed", ShowDialogLauncher = true };
        group.Items.Add(commands);
        group.SetSizeState(RibbonGroupSizeState.Collapsed);
        var window = CreateWindow(new AdornerDecorator { Child = group }, flow);
        try
        {
            window.Show();
            Sta.Drain();
            window.UpdateLayout();
            var opener = Assert.IsType<ToggleButton>(group.CollapsedButton);
            var popup = Assert.IsType<Popup>(group.Template.FindName("PART_Popup", group));
            var launcher = Assert.IsAssignableFrom<ButtonBase>(group.DialogLauncher);
            Assert.True(opener.Focus());
            Press(Key.Enter);
            Assert.True(popup.IsOpen);
            Assert.Same(first, Keyboard.FocusedElement);
            var nestedOpener = Assert.IsType<ToggleButton>(dropdown.Template.FindName("PART_Toggle", dropdown));
            Move(FocusNavigationDirection.Next, nestedOpener);
            Move(FocusNavigationDirection.Next, editor);
            Move(FocusNavigationDirection.Next, last);
            Move(FocusNavigationDirection.Next, launcher);
            Move(FocusNavigationDirection.Next, first);
            Move(FocusNavigationDirection.Previous, launcher);
            Move(FocusNavigationDirection.Previous, last);
            Press(Key.Home);
            Assert.Same(first, Keyboard.FocusedElement);
            Press(Key.Down);
            Assert.Same(nestedOpener, Keyboard.FocusedElement);
            Press(Key.Enter);
            Assert.True(dropdown.IsDropDownOpen);
            Assert.Same(nested, Keyboard.FocusedElement);
            Press(Key.Escape);
            Assert.False(dropdown.IsDropDownOpen);
            Assert.True(popup.IsOpen);
            Assert.Same(nestedOpener, Keyboard.FocusedElement);
            Press(Key.Escape);
            Assert.False(popup.IsOpen);
            Assert.Same(opener, Keyboard.FocusedElement);

            Press(Key.Up);
            Assert.True(popup.IsOpen);
            Assert.Same(launcher, Keyboard.FocusedElement);
            Press(Key.Escape);
            Assert.Same(opener, Keyboard.FocusedElement);
            Press(Key.Down);
            Assert.Same(first, Keyboard.FocusedElement);
            int clicks = 0;
            first.Click += (_, _) => clicks++;
            Press(Key.Enter);
            Assert.Equal(1, clicks);
            Assert.False(popup.IsOpen);
            Assert.Same(opener, Keyboard.FocusedElement);
        }
        finally { window.Close(); Sta.ResetApplication(); }
    });

    private static Window CreateWindow(UIElement content, FlowDirection flow) => new()
    {
        Content = content, FlowDirection = flow, Width = 400, Height = 300,
        Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false,
    };

    private static void Press(Key key)
    {
        var target = Assert.IsAssignableFrom<UIElement>(Keyboard.FocusedElement);
        var source = PresentationSource.FromVisual(target)!;
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key)
        { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key)
        { RoutedEvent = Keyboard.PreviewKeyUpEvent });
        Sta.Drain();
    }

    private static void Move(FocusNavigationDirection direction, IInputElement expected)
    {
        Assert.True(((UIElement)Keyboard.FocusedElement).MoveFocus(new TraversalRequest(direction)));
        Sta.Drain();
        Assert.Same(expected, Keyboard.FocusedElement);
    }

    private static void ShowFocusVisual()
    {
        var force = typeof(KeyboardNavigation).GetProperty("AlwaysShowFocusVisual", BindingFlags.Static | BindingFlags.NonPublic)!;
        object? previous = force.GetValue(null);
        try
        {
            force.SetValue(null, true);
            typeof(KeyboardNavigation).GetMethod("ShowFocusVisual", BindingFlags.Static | BindingFlags.NonPublic,
                Type.EmptyTypes)!.Invoke(null, null);
        }
        finally { force.SetValue(null, previous); }
    }

    private static Border? FindBorder(DependencyObject node)
    {
        if (node is Border border) return border;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++)
            if (FindBorder(VisualTreeHelper.GetChild(node, index)) is { } found) return found;
        return null;
    }

    private static T? FindNamed<T>(DependencyObject node, string name) where T : FrameworkElement
    {
        if (node is T element && element.Name == name) return element;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++)
            if (FindNamed<T>(VisualTreeHelper.GetChild(node, index), name) is { } found) return found;
        return null;
    }

    private static void PumpFrames()
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(120) };
        timer.Tick += (_, _) => frame.Continue = false;
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); }
    }
}
