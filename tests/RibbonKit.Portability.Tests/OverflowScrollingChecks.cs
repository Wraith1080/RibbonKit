using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Only public RibbonKit controls and their shared templates; no Showcase resources.
internal static class OverflowScrollingChecks
{
    internal static void Verify(Application application)
    {
        var level = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try
        {
            foreach (var sample in new[]
            {
                (RibbonTheme.Office2024, false, RibbonDensity.Compact, FlowDirection.LeftToRight),
                (RibbonTheme.Office2024Flat, true, RibbonDensity.Touch, FlowDirection.RightToLeft),
                (RibbonTheme.CrystalLight, false, RibbonDensity.Compact, FlowDirection.RightToLeft),
                (RibbonTheme.Office2007, true, RibbonDensity.Touch, FlowDirection.LeftToRight),
            })
            {
                ThemeManager.Apply(application, sample.Item1);
                ThemeManager.SetDarkMode(application, sample.Item2);
                var ribbon = new Ribbon { Density = sample.Item3, FlowDirection = sample.Item4,
                    QuickAccessPosition = RibbonQuickAccessPosition.TabRow, QuickAccessMaxWidth = 36 };
                var group = new RibbonGroup { Header = "Commands" };
                var tab = new RibbonTab { Header = "Home" };
                tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
                var menu = new RibbonDropDownButton { Header = "Options", Icon = new DrawingImage() };
                menu.Items.Add(new RibbonMenuItem { Header = "First option" });
                menu.Items.Add(new RibbonMenuItem { Header = "Second option" });
                var split = new RibbonSplitButton { Header = "Paste", Icon = new DrawingImage() };
                split.Items.Add(new RibbonMenuItem { Header = "Paste special" });
                var combo = new RibbonComboBox { Header = "Size", ItemsSource = new[] { "11", "12" }, SelectedIndex = 0 };
                var gallery = new InRibbonGallery { Header = "Styles", Width = 120 };
                gallery.Items.Add(new RibbonGalleryItem { Content = "Style A" });
                foreach (var source in new FrameworkElement[] { menu, split, combo, gallery })
                {
                    Ribbon.SetSimplifiedPlacement(source, RibbonSimplifiedPlacement.Inline);
                    group.Items.Add(source); ribbon.AddToQuickAccess(source);
                }
                int invoked = 0;
                for (int i = 0; i < 30; i++)
                {
                    var command = new RibbonButton { Header = $"Command {i}", Icon = new DrawingImage() };
                    Ribbon.SetSimplifiedPlacement(command, RibbonSimplifiedPlacement.Inline);
                    command.Click += (_, _) => invoked++;
                    group.Items.Add(command); ribbon.AddToQuickAccess(command);
                }
                var window = new RibbonWindow { Content = ribbon, Width = 850, Height = 350,
                    FlowDirection = sample.Item4, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
                try
                {
                    window.Show(); window.Activate(); Layout(window);
                    foreach (var (mode, position) in new[]
                    {
                        (RibbonLayoutMode.Classic, RibbonQuickAccessPosition.TabRow),
                        (RibbonLayoutMode.Simplified, RibbonQuickAccessPosition.TabRow),
                        (RibbonLayoutMode.Classic, RibbonQuickAccessPosition.BelowRibbon),
                    })
                    {
                        ribbon.LayoutMode = mode; ribbon.QuickAccessPosition = position;
                        bool below = position == RibbonQuickAccessPosition.BelowRibbon;
                        if (below)
                        {
                            // Exercise the actual full-row constraint with nested commands at the end.
                            foreach (var copy in ribbon.QuickAccessItems.Take(4).ToArray())
                            { ribbon.QuickAccessItems.Remove(copy); ribbon.QuickAccessItems.Add(copy); }
                            window.Width = 520;
                        }
                        Layout(window);
                        var body = (RibbonGroupsHost)tab.Content;
                        var toolbar = mode == RibbonLayoutMode.Simplified
                            ? Descendants(body).OfType<RibbonQuickAccessToolBar>().Single()
                            : Descendants(ribbon).OfType<RibbonQuickAccessToolBar>().Single(bar => bar.IsVisible && !body.IsAncestorOf(bar));
                        if (!below) toolbar.Width = 36;
                        else
                        {
                            Assert.True(double.IsNaN(toolbar.Width) && double.IsPositiveInfinity(toolbar.MaxWidth));
                            Assert.True(toolbar.ActualWidth > ribbon.QuickAccessMaxWidth + 100);
                        }
                        Layout(window);
                        var toggle = (ToggleButton)toolbar.Template.FindName("PART_OverflowButton", toolbar);
                        toggle.IsChecked = true; Layout(window);
                        var popup = (Popup)toolbar.Template.FindName("PART_OverflowPopup", toolbar);
                        var viewer = Assert.IsType<ScrollViewer>(toolbar.Template.FindName("PART_OverflowScrollViewer", toolbar));
                        Assert.True(double.IsFinite(viewer.MaxHeight));
                        var nativeRoot = (FrameworkElement)PresentationSource.FromVisual(viewer)!.RootVisual;
                        Assert.True(viewer.TranslatePoint(new Point(0, viewer.ActualHeight), nativeRoot).Y <= nativeRoot.ActualHeight + .5,
                            "The default screen-height budget must keep the bottom band inside the native popup.");
                        viewer.MaxHeight = 240; Layout(window);
                        var up = (RepeatButton)viewer.Template.FindName("PART_ScrollUp", viewer);
                        var down = (RepeatButton)viewer.Template.FindName("PART_ScrollDown", viewer);
                        var viewport = (ScrollContentPresenter)viewer.Template.FindName("PART_ScrollContentPresenter", viewer);
                        Assert.True(up.IsVisible && down.IsVisible);
                        Assert.False(up.IsEnabled); Assert.True(down.IsEnabled);
                        Assert.True(viewer.ScrollableHeight > 0);
                        Assert.InRange(viewer.ActualHeight, 1, 240.5);
                        Assert.True(up.TranslatePoint(new Point(0, up.ActualHeight), viewer).Y <= viewport.TranslatePoint(new Point(), viewer).Y + .5);
                        Assert.True(viewport.TranslatePoint(new Point(0, viewport.ActualHeight), viewer).Y <= down.TranslatePoint(new Point(), viewer).Y + .5);
                        if (sample.Item3 == RibbonDensity.Touch) Assert.True(down.ActualHeight >= 44);
                        Assert.DoesNotContain(Descendants(viewer).OfType<ScrollBar>(), bar => bar.IsVisible);
                        down.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window);
                        Assert.True(viewer.VerticalOffset > 0); Assert.True(popup.IsOpen); Assert.Equal(0, invoked);
                        Assert.Same(viewer.FindResource("RibbonKit.Brushes.Text.Secondary"), down.Foreground);
                        VerifyKeyboard(window, toolbar, popup, viewer);
                        Capture(popup, $"{sample.Item1}-{sample.Item2}-{sample.Item3}-{sample.Item4}-{mode}-{position}");
                        viewer.ScrollToBottom(); Layout(window);
                        Assert.True(up.IsEnabled); Assert.False(down.IsEnabled);
                        var entries = (ItemsControl)toolbar.Template.FindName("PART_OverflowHost", toolbar);
                        // Simplified also appends its full-group access entry.
                        entries.Items.OfType<RibbonButton>().Last().RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window);
                        Assert.Equal(1, invoked); invoked = 0;
                        Assert.False(popup.IsOpen);
                    }
                }
                finally { window.Close(); Layout(window); }
            }
        }
        finally { ThemeManager.SetDarkMode(application, false); RibbonAnimation.GlobalLevel = level; }
    }

    private static void VerifyKeyboard(Window window, RibbonQuickAccessToolBar toolbar, Popup popup, ScrollViewer viewer)
    {
        var items = (ItemsControl)toolbar.Template.FindName("PART_OverflowHost", toolbar);
        var rows = items.Items.OfType<FrameworkElement>().Where(row => row.IsEnabled && row.IsVisible).ToArray();
        viewer.ScrollToTop(); rows[0].BringIntoView(); Layout(window);
        Assert.True(rows[0].Focus() || rows[0].MoveFocus(new TraversalRequest(FocusNavigationDirection.First)),
            $"Keyboard setup failed: active={window.IsActive}, focus={Keyboard.FocusedElement}");
        Layout(window);
        foreach (var next in rows.Skip(1))
        {
            Press((UIElement)Keyboard.FocusedElement, Key.Down); Layout(window);
            Assert.True(next.IsKeyboardFocusWithin);
            Assert.All(rows.OfType<RibbonDropDownButton>(), entry => Assert.False(entry.IsDropDownOpen));
            var viewport = (ScrollContentPresenter)viewer.Template.FindName("PART_ScrollContentPresenter", viewer);
            Assert.InRange(next.TranslatePoint(new Point(), viewport).Y, -.5, viewport.ActualHeight - next.ActualHeight + .5);
        }
        Assert.True(viewer.VerticalOffset > 0);
        foreach (var previous in rows.AsEnumerable().Reverse().Skip(1))
        {
            Press((UIElement)Keyboard.FocusedElement, Key.Up); Layout(window);
            Assert.True(previous.IsKeyboardFocusWithin);
        }
        var menu = rows.OfType<RibbonDropDownButton>().Single(entry => entry.Header?.ToString() == "Options");
        menu.BringIntoView(); Layout(window);
        Assert.True(menu.Focus() || menu.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)));
        Press((UIElement)Keyboard.FocusedElement, menu.FlowDirection == FlowDirection.LeftToRight ? Key.Right : Key.Left); Layout(window);
        Assert.True(menu.IsDropDownOpen);
        Press((UIElement)Keyboard.FocusedElement, Key.Down); Layout(window);
        Assert.True(((UIElement)menu.Items[1]).IsKeyboardFocusWithin);
        Press((UIElement)Keyboard.FocusedElement, Key.Escape); Layout(window);
        Assert.False(menu.IsDropDownOpen); Assert.True(popup.IsOpen);
    }

    private static void Press(UIElement target, Key key)
    {
        var args = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target)!, Environment.TickCount, key)
            { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        target.RaiseEvent(args);
        if (!args.Handled) { args.RoutedEvent = Keyboard.KeyDownEvent; target.RaiseEvent(args); }
    }

    private static void Capture(Popup popup, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_OVERFLOW_SCROLL_CAPTURE");
        if (string.IsNullOrEmpty(directory)) return;
        var surface = (FrameworkElement)(PresentationSource.FromVisual(popup.Child)?.RootVisual ?? popup.Child);
        var dpi = VisualTreeHelper.GetDpi(surface);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(surface.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(surface.ActualHeight * dpi.DpiScaleY), 96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY, PixelFormats.Pbgra32);
        bitmap.Render(surface);
        Directory.CreateDirectory(directory);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(output);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        { var child = VisualTreeHelper.GetChild(parent, i); yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }

    private static void Layout(Window window)
    { window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout(); window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); }
}
