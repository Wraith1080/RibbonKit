using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
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

// Public RibbonKit controls/resources only, including motion during real render frames.
internal static class LayoutMotionPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var level = RibbonAnimation.GlobalLevel;
        var reduce = RibbonAnimation.RespectSystemReduceMotion;
        var actions = new[] { RibbonAnimationAction.LayoutModeChange, RibbonAnimationAction.DensityChange, RibbonAnimationAction.TabSwitch };
        var overrides = actions.ToDictionary(action => action, RibbonAnimation.GetActionOverride);
        try
        {
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;
            RibbonAnimation.RespectSystemReduceMotion = false;
            foreach (var action in actions) RibbonAnimation.ClearActionLevel(action);
            string? scope = Environment.GetEnvironmentVariable("RIBBONKIT_PORTABILITY_SCOPE");
            if (scope == "ModePopupDismissal")
            {
                VerifyModePopupDismissal(application);
                return;
            }
            if (scope == "LayoutMotionPaint")
            {
                foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
                    VerifyLayout(application, RibbonTheme.Office2024Flat, FlowDirection.LeftToRight, RibbonDensity.Compact, mode);
                VerifyInterruptions(application);
                VerifyDensitySelector(application);
            }
            else if (scope != "LayoutMotionPopup")
            {
                foreach (var theme in new[] { RibbonTheme.Office2010, RibbonTheme.Office2024Flat, RibbonTheme.CrystalLight })
                foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
                foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
                foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
                    VerifyLayout(application, theme, direction, density, mode);
                VerifyInterruptions(application);
                VerifyDensitySelector(application);
            }
            DensityTransitionPortabilityChecks.VerifyPopupAnchor(application);
            VerifyModePopupDismissal(application);
        }
        finally
        {
            RibbonAnimation.GlobalLevel = level;
            RibbonAnimation.RespectSystemReduceMotion = reduce;
            foreach (var action in actions)
                if (overrides[action] is { } value) RibbonAnimation.SetActionLevel(action, value);
                else RibbonAnimation.ClearActionLevel(action);
        }
    }

    private static void VerifyModePopupDismissal(Application application)
    {
        foreach (var theme in new[] { RibbonTheme.Office2010, RibbonTheme.Office2024Flat, RibbonTheme.CrystalLight })
        foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
        {
            ThemeManager.Apply(application, theme);
            var (window, ribbon) = Create();
            ribbon.FlowDirection = direction;
            var group = ribbon.Tabs[0].Groups[0];
            var mode = new ConsumerToggle { Header = "Simplified" };
            var ordinary = new ConsumerToggle { Header = "Option" };
            var nested = new RibbonDropDownButton { Header = "Nested" };
            nested.Items.Add(new RibbonMenuItem { Header = "Native command" });
            group.Items.Add(mode); group.Items.Add(ordinary); group.Items.Add(nested);
            mode.Checked += (_, _) => ribbon.LayoutMode = RibbonLayoutMode.Simplified;
            mode.Unchecked += (_, _) => ribbon.LayoutMode = RibbonLayoutMode.Classic;
            bool clickedInPopup = false;
            mode.Click += (_, _) => clickedInPopup = Part<Popup>(group, "PART_Popup").IsOpen
                && Part<Border>(group, "PART_PopupHost").IsAncestorOf(mode);
            try
            {
                window.Show(); Layout(window); WaitForRender(window);
                ((FrameworkElement)ribbon.Tabs[0].Content).Width = 70; Layout(window);
                Part<ToggleButton>(group, "PART_CollapsedButton").IsChecked = true; Layout(window);
                Assert.True(Part<Popup>(group, "PART_Popup").IsOpen);
                ordinary.Invoke(); Layout(window);
                Assert.True(Part<Popup>(group, "PART_Popup").IsOpen);
                mode.Invoke(); Assert.True(clickedInPopup); Layout(window);
                Assert.False(Part<Popup>(group, "PART_Popup").IsOpen);
                Assert.True(ordinary.IsChecked);
                Assert.True(Part<Decorator>(group, "PART_NormalHost").IsAncestorOf(mode));
                Assert.Single(Scenes(Body(ribbon)));
                PumpUntil(() => Scenes(Body(ribbon)).Length == 0);

                var toolbar = Descendants((DependencyObject)ribbon.Tabs[0].Content)
                    .OfType<RibbonQuickAccessToolBar>().Single();
                Part<ToggleButton>(toolbar, "PART_OverflowButton").IsChecked = true; Layout(window);
                var overflow = Part<Popup>(toolbar, "PART_OverflowPopup");
                var opener = Descendants(overflow.Child).OfType<RibbonDropDownButton>()
                    .Single(control => ReferenceEquals(Ribbon.GetQuickAccessSource(control), group));
                opener.IsDropDownOpen = true; Layout(window);
                Assert.True(Part<Popup>(group, "PART_Popup").IsOpen);
                mode.Invoke(); Assert.True(clickedInPopup); Layout(window);
                Assert.False(Part<Popup>(group, "PART_Popup").IsOpen);
                Assert.False(opener.IsDropDownOpen); Assert.False(overflow.IsOpen);
                Assert.True(Part<Decorator>(group, "PART_NormalHost").IsAncestorOf(mode));
                PumpUntil(() => Scenes(Body(ribbon)).Length == 0);

                ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
                ribbon.AddToQuickAccess(group); Layout(window);
                var qat = ribbon.QuickAccessItems.OfType<RibbonDropDownButton>()
                    .Single(control => ReferenceEquals(Ribbon.GetQuickAccessSource(control), group));
                qat.IsDropDownOpen = true; Layout(window);
                nested.IsDropDownOpen = true; Layout(window);
                Assert.True(Part<Popup>(group, "PART_Popup").IsOpen);
                Assert.True(nested.IsDropDownOpen);
                mode.Invoke(); Assert.True(clickedInPopup); Layout(window);
                Assert.False(Part<Popup>(group, "PART_Popup").IsOpen);
                Assert.False(qat.IsDropDownOpen); Assert.False(nested.IsDropDownOpen);
                Assert.Single(nested.Items);
                Assert.True(Part<Decorator>(group, "PART_NormalHost").IsAncestorOf(mode));
                PumpUntil(() => Scenes(Body(ribbon)).Length == 0);
            }
            finally { window.Close(); }
        }
    }

    private sealed class ConsumerToggle : RibbonToggleButton
    {
        internal void Invoke() => OnClick();
    }

    private static T Part<T>(Control owner, string name) where T : class =>
        Assert.IsType<T>(owner.Template.FindName(name, owner));

    private static void VerifyLayout(Application application, RibbonTheme theme,
        FlowDirection direction, RibbonDensity density, RibbonLayoutMode mode)
    {
        ThemeManager.Apply(application, theme);
        var (window, ribbon) = Create();
        ribbon.FlowDirection = direction;
        // Restore both appearance preferences in Loaded, before motion readiness.
        window.Loaded += (_, _) => { ribbon.LayoutMode = mode; ribbon.Density = density; };
        try
        {
            window.Show(); Layout(window); WaitForRender(window);
            Border body = Body(ribbon);
            Assert.Empty(Scenes(body));
            Assert.False(body.HasAnimatedProperties);
            double previousHeight = body.ActualHeight;
            var transform = new TranslateTransform(1, 0);
            body.SetBinding(UIElement.RenderTransformProperty, new Binding(".") { Source = transform });
            body.SetBinding(UIElement.OpacityProperty, new Binding(".") { Source = 0.9d });
            ribbon.LayoutMode = mode == RibbonLayoutMode.Classic ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic;
            // Force the render boundary before deferred row construction runs.
            // The live destination must already be hidden by the previous vector cover.
            Assert.Equal(0, body.Opacity);
            Assert.Single(Holds(body));
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                window.UpdateLayout();
                Assert.Equal(0, body.Opacity);
                Assert.Single(Holds(body));
                if (density == RibbonDensity.Compact) Save(window, $"{theme}-{mode}-{direction}-pending");
            }, DispatcherPriority.Render);
            Layout(window);
            var scene = Assert.Single(Scenes(body));
            Assert.False(scene.IsHitTestVisible);
            Assert.False(scene.Focusable);
            Assert.Equal(0, body.Opacity);
            Assert.Same(transform, body.RenderTransform);
            Assert.True(mode == RibbonLayoutMode.Classic ? body.ActualHeight < previousHeight : body.ActualHeight > previousHeight);
            Assert.True(scene.DesiredSize.Height >= Math.Max(previousHeight, body.ActualHeight));
            double finalHeight = body.ActualHeight;
            if (density == RibbonDensity.Compact)
            {
                Save(window, $"{theme}-{mode}-{direction}-start");
                WaitMilliseconds(40);
                Assert.NotEmpty(Scenes(body));
                Save(window, $"{theme}-{mode}-{direction}-middle");
            }
            PumpUntil(() => Scenes(body).Length == 0);
            Assert.False(body.HasAnimatedProperties);
            Assert.Equal(0.9d, body.Opacity);
            Assert.Equal(finalHeight, body.ActualHeight);
            Assert.True(BindingOperations.IsDataBound(body, UIElement.OpacityProperty));
            Assert.True(BindingOperations.IsDataBound(body, UIElement.RenderTransformProperty));
            if (density == RibbonDensity.Compact)
                Save(window, $"{theme}-{mode}-{direction}-rest");

            ribbon.Density = density == RibbonDensity.Compact ? RibbonDensity.Touch : RibbonDensity.Compact;
            Layout(window);
            Assert.Single(Scenes(body));
            finalHeight = body.ActualHeight;
            PumpUntil(() => Scenes(body).Length == 0);
            Assert.Equal(finalHeight, body.ActualHeight);
            Assert.Equal(0.9d, body.Opacity);
            Assert.Same(transform, body.RenderTransform);

            RibbonAnimation.SetActionLevel(RibbonAnimationAction.LayoutModeChange, RibbonAnimationLevel.None);
            ribbon.LayoutMode = mode; Layout(window);
            Assert.Empty(Scenes(body));
            Assert.False(body.HasAnimatedProperties);
            Assert.Equal(0.9d, body.Opacity);
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.LayoutModeChange);
        }
        finally { window.Close(); }
    }

    private static void VerifyInterruptions(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.Office2024Flat);
        var (window, ribbon) = Create();
        try
        {
            window.Show(); Layout(window); WaitForRender(window);
            var body = Body(ribbon);
            ribbon.LayoutMode = RibbonLayoutMode.Simplified;
            ribbon.Density = RibbonDensity.Touch;
            ribbon.LayoutMode = RibbonLayoutMode.Classic;
            Layout(window); Assert.Single(Scenes(body));
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            Assert.Empty(Scenes(body)); Assert.Equal(1, body.Opacity);
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;
            ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout(window);
            Assert.Single(Scenes(body));
            // Subsequent keyboard input immediately exposes the real final controls.
            ribbon.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, Key.Tab)
            { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            Assert.Empty(Scenes(body)); Assert.Equal(1, body.Opacity);

            ribbon.LayoutMode = RibbonLayoutMode.Classic; Layout(window);
            Assert.Single(Scenes(body));
            ribbon.SelectedTab = ribbon.Tabs[1]; Layout(window);
            Assert.Empty(Scenes(body)); Assert.Equal(1, body.Opacity);
            ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout(window);
            PumpUntil(() => Scenes(body).Length == 0);
            ribbon.SelectedTab = ribbon.Tabs[0]; Layout(window);
            var content = Assert.IsAssignableFrom<FrameworkElement>(body.Child);
            Assert.True(content.HasAnimatedProperties);
            WaitMilliseconds(100);
            Assert.True(content.HasAnimatedProperties);
            RibbonAnimation.SetActionLevel(RibbonAnimationAction.TabSwitch, RibbonAnimationLevel.None);
            Assert.False(content.HasAnimatedProperties);
            Assert.True(content.RenderTransform.Value.IsIdentity);
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.TabSwitch);

            ribbon.Density = RibbonDensity.Compact; Layout(window);
            Assert.Single(Scenes(body));
            window.Width += 80; Layout(window);
            Assert.Empty(Scenes(body)); Assert.Equal(1, body.Opacity);
            ribbon.LayoutMode = RibbonLayoutMode.Classic; Layout(window);
            Assert.Single(Scenes(body));
            ribbon.Template = FreshTemplate(); ribbon.ApplyTemplate(); Layout(window);
            Assert.Empty(Scenes(body)); Assert.False(body.HasAnimatedProperties);
            body = Body(ribbon);
            ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout(window);
            Assert.Single(Scenes(body));
            window.Content = null; Layout(window);
            Assert.Empty(Scenes(body)); Assert.Equal(1, body.Opacity);
        }
        finally { window.Close(); }
    }

    private static ControlTemplate FreshTemplate()
    {
        var resources = new ResourceDictionary
        { Source = new Uri("pack://application:,,,/RibbonKit;component/Themes/Office2024.xaml") };
        return (ControlTemplate)((Style)resources[typeof(Ribbon)]).Setters.OfType<Setter>()
            .Single(setter => setter.Property == Control.TemplateProperty).Value;
    }

    private static void VerifyDensitySelector(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.Office2024Flat);
        foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
        {
            var (window, ribbon) = Create();
            ribbon.LayoutMode = mode;
            var selector = new RibbonDropDownButton { Header = "Touch/Mouse mode" };
            var touch = new RibbonMenuItem { Header = "Touch", Tag = RibbonDensity.Touch };
            var mouse = new RibbonMenuItem { Header = "Mouse", Tag = RibbonDensity.Compact };
            foreach (var item in new[] { touch, mouse })
            {
                item.Click += (sender, _) => ribbon.Density = (RibbonDensity)((RibbonMenuItem)sender).Tag;
                selector.Items.Add(item);
            }
            Ribbon.SetSimplifiedPlacement(selector, RibbonSimplifiedPlacement.Inline);
            ribbon.Tabs[0].Groups[0].Items.Add(selector);
            try
            {
                window.Show(); Layout(window); WaitForRender(window);
                var body = Body(ribbon);
                var opener = mode == RibbonLayoutMode.Classic ? selector : Descendants(ribbon)
                    .OfType<RibbonDropDownButton>().Single(control => control.IsVisible && control.ActualWidth > 0
                        && ReferenceEquals(Ribbon.GetQuickAccessSource(control), selector));
                foreach (var choice in new[] { touch, mouse })
                {
                    double before = body.ActualHeight;
                    opener.IsDropDownOpen = true; Layout(window);
                    Assert.True(opener.IsDropDownOpen);
                    choice.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, choice));
                    Assert.Equal(0, body.Opacity);
                    Layout(window);
                    Assert.Equal((RibbonDensity)choice.Tag, ribbon.Density);
                    Assert.False(opener.IsDropDownOpen);
                    Assert.Single(Scenes(body)); // The just-dismissed selector is not a persistent anchor.
                    Assert.NotEqual(before, body.ActualHeight);
                    PumpUntil(() => Scenes(body).Length == 0);
                    Assert.Empty(Holds(body)); Assert.Equal(1, body.Opacity);
                }
            }
            finally { window.Close(); }
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i); yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static (RibbonWindow, Ribbon) Create()
    {
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon };
        foreach (var caption in new[] { "Home", "Insert" })
        {
            var tab = new RibbonTab { Header = caption };
            foreach (var title in new[] { "Clipboard", "Font", "Styles" })
            {
                var group = new RibbonGroup { Header = title };
                for (int i = 0; i < 3; i++)
                {
                    var button = new RibbonButton { Header = $"{title} {i}",
                        Icon = new DrawingImage(new GeometryDrawing(Brushes.SteelBlue, null,
                            new RectangleGeometry(new Rect(0, 0, 16, 16)))),
                        Size = i == 0 ? RibbonControlSize.Large : RibbonControlSize.Small };
                    Ribbon.SetSimplifiedPlacement(button, RibbonSimplifiedPlacement.Inline);
                    group.Items.Add(button);
                }
                tab.Groups.Add(group);
            }
            ribbon.Tabs.Add(tab);
        }
        ribbon.SelectedTab = ribbon.Tabs[0];
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save" });
        var root = new DockPanel(); DockPanel.SetDock(ribbon, Dock.Top); root.Children.Add(ribbon);
        root.Children.Add(new Border { Background = Brushes.White, Child = new TextBlock { Text = "Consumer document", Margin = new Thickness(16) } });
        var window = new RibbonWindow { Content = root, Width = 900, Height = 420,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        return (window, ribbon);
    }

    private static Border Body(Ribbon ribbon)
    {
        var tabs = (RibbonTabControl)ribbon.Template.FindName("TabControlHost", ribbon);
        return (Border)tabs.Template.FindName("ContentHost", tabs);
    }
    private static Adorner[] Scenes(Border body) => AdornerLayer.GetAdornerLayer(body)?.GetAdorners(body)
        ?.Where(adorner => adorner.GetType().Name == "RibbonLayoutTransition").ToArray() ?? Array.Empty<Adorner>();
    private static Adorner[] Holds(Border body) => AdornerLayer.GetAdornerLayer(body)?.GetAdorners(body)
        ?.Where(adorner => adorner.GetType().Name == "RibbonLayoutHold").ToArray() ?? Array.Empty<Adorner>();
    private static void Layout(Window window)
    { Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout(); }
    private static void WaitMilliseconds(int milliseconds)
    { var watch = Stopwatch.StartNew(); PumpUntil(() => watch.ElapsedMilliseconds >= milliseconds); }
    private static void WaitForRender(Window window)
    {
        bool rendered = false; EventHandler handler = (_, _) => rendered = true;
        CompositionTarget.Rendering += handler;
        try { window.InvalidateVisual(); PumpUntil(() => rendered); }
        finally { CompositionTarget.Rendering -= handler; }
    }
    private static void PumpUntil(Func<bool> condition)
    {
        var frame = new DispatcherFrame(); var watch = Stopwatch.StartNew();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(5) };
        timer.Tick += (_, _) => { if (condition() || watch.Elapsed > TimeSpan.FromSeconds(2)) frame.Continue = false; };
        timer.Start();
        try { Dispatcher.PushFrame(frame); } finally { timer.Stop(); }
        Assert.True(condition(), "Layout motion did not settle within two seconds.");
    }
    private static void Save(Window window, string name)
    {
        string? folder = Environment.GetEnvironmentVariable("RIBBONKIT_LAYOUT_MOTION_DIAGNOSTICS");
        if (string.IsNullOrEmpty(folder)) return;
        Directory.CreateDirectory(folder);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(Path.Combine(folder, name + ".png")); png.Save(output);
    }
}
