using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Real animation clocks in a consumer that references only RibbonKit.
internal static class MessageTransitionPortabilityChecks
{
    internal static void Verify(Application application)
    {
        RibbonAnimationLevel level = RibbonAnimation.GlobalLevel;
        bool reduce = RibbonAnimation.RespectSystemReduceMotion;
        RibbonAnimationLevel? action = RibbonAnimation.GetActionOverride(RibbonAnimationAction.MessageBar);
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;
        RibbonAnimation.RespectSystemReduceMotion = false;
        RibbonAnimation.ClearActionLevel(RibbonAnimationAction.MessageBar);
        try
        {
            foreach (RibbonTheme theme in new[] { RibbonTheme.Office2007, RibbonTheme.Office2024 })
            foreach (bool dark in new[] { false, true })
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            foreach (RibbonQuickAccessPosition placement in new[]
                { RibbonQuickAccessPosition.TitleBar, RibbonQuickAccessPosition.TabRow })
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                VerifyTransitions(theme, dark, density, placement);
            }
            VerifyLifecycle(application);
        }
        finally
        {
            RibbonAnimation.GlobalLevel = level;
            RibbonAnimation.RespectSystemReduceMotion = reduce;
            if (action is { } value) RibbonAnimation.SetActionLevel(RibbonAnimationAction.MessageBar, value);
            else RibbonAnimation.ClearActionLevel(RibbonAnimationAction.MessageBar);
        }
    }

    private static void VerifyTransitions(RibbonTheme theme, bool dark, RibbonDensity density,
        RibbonQuickAccessPosition placement)
    {
        var first = new RibbonMessage { Title = "FIRST", Message = "First notification", IsOpen = false };
        var second = new RibbonMessage { Title = "SECOND", Message = "Second notification", IsOpen = false };
        var messages = new RibbonMessageBar();
        messages.Items.Add(first);
        messages.Items.Add(second);
        var (window, ribbon) = Create(messages, density, placement);
        try
        {
            window.Show(); Layout(window);
            var rim = Part<Border>(messages, "ExposedTopBorder");
            Assert.False(BindingOperations.IsDataBound(rim, UIElement.OpacityProperty));
            AssertRest(rim);
            first.IsOpen = true;
            var firstRoot = Part<FrameworkElement>(first, "PART_Root");
            Assert.Equal(0d, rim.Opacity); // No opaque top-edge flash on the first frame.
            SampleTransition(firstRoot, rim, opening: true);
            SavePreview(window, $"{theme}-{(dark ? "dark" : "light")}-{density}-{placement}-opening");
            Finish(firstRoot); AssertRest(rim);

            // Adding a lower row must not replay the stack's exposed upper edge.
            second.IsOpen = true;
            var secondRoot = Part<FrameworkElement>(second, "PART_Root");
            PumpUntil(() => secondRoot.Opacity > .1 && secondRoot.Opacity < .9);
            Assert.Equal(1d, rim.Opacity);
            Assert.Equal(Matrix.Identity, rim.RenderTransform.Value);
            Finish(secondRoot);

            first.Dismiss();
            Assert.True(messages.HasOpenMessages);
            SampleTransition(firstRoot, rim, opening: false);
            SavePreview(window, $"{theme}-{(dark ? "dark" : "light")}-{density}-{placement}-closing");
            PumpUntil(() => !first.IsPresented); Layout(window);
            AssertRest(rim);
            Assert.Same(secondRoot, BindingOperations.GetBinding(rim, UIElement.OpacityProperty)!.Source);

            // Interrupt an exit with a reopen: the old completion cannot hide the live row/rim.
            second.IsOpen = false;
            SampleTransition(secondRoot, rim, opening: false);
            second.IsOpen = true;
            SampleTransition(secondRoot, rim, opening: true);
            Finish(secondRoot); Assert.True(second.IsPresented); AssertRest(rim);
            second.Dismiss(); SampleTransition(secondRoot, rim, opening: false);
            PumpUntil(() => !messages.HasOpenMessages); Layout(window);
            AssertRest(rim);
            Assert.Equal(new Thickness(0), rim.Tag);
            Assert.False(BindingOperations.IsDataBound(rim, UIElement.OpacityProperty));

            // Disabled motion, including an action override, uses the same immediate row state.
            RibbonAnimation.SetActionLevel(RibbonAnimationAction.MessageBar, RibbonAnimationLevel.None);
            first.IsOpen = true; Layout(window); AssertRest(firstRoot); AssertRest(rim);
            first.Dismiss(); Layout(window); Assert.False(messages.HasOpenMessages); AssertRest(rim);
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.MessageBar);
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            first.IsOpen = true; Layout(window); AssertRest(firstRoot); AssertRest(rim);
            first.Dismiss(); Layout(window); Assert.False(messages.HasOpenMessages); AssertRest(rim);
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;

            // Connected placement suppresses this extra rim while keeping the row transition.
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
            Layout(window);
            first.IsOpen = true;
            Assert.Equal(new Thickness(0), rim.Tag);
            SampleTransition(firstRoot, rim, opening: true);
            Finish(firstRoot);
        }
        finally { window.Close(); Layout(window); }
    }

    private static void VerifyLifecycle(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        ThemeManager.SetDarkMode(application, false);
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Expressive;
        var firstState = new MessageState();
        var secondState = new MessageState();
        var containerStyle = new Style(typeof(RibbonMessage));
        containerStyle.Setters.Add(new Setter(RibbonMessage.IsOpenProperty,
            new Binding(nameof(MessageState.IsOpen)) { Mode = BindingMode.TwoWay }));
        var messages = new RibbonMessageBar
        {
            ItemsSource = new[] { firstState, secondState }, ItemContainerStyle = containerStyle,
        };
        var (window, ribbon) = Create(messages, RibbonDensity.Touch, RibbonQuickAccessPosition.TabRow);
        try
        {
            window.Show(); Layout(window);
            var first = Assert.IsType<RibbonMessage>(messages.ItemContainerGenerator.ContainerFromIndex(0));
            var second = Assert.IsType<RibbonMessage>(messages.ItemContainerGenerator.ContainerFromIndex(1));
            var firstRoot = Part<FrameworkElement>(first, "PART_Root");
            var secondRoot = Part<FrameworkElement>(second, "PART_Root");
            var rim = Part<Border>(messages, "ExposedTopBorder");
            Finish(firstRoot); Finish(secondRoot);
            Assert.Same(firstRoot, BindingOperations.GetBinding(rim, UIElement.OpacityProperty)!.Source);

            firstState.IsOpen = false; SampleTransition(firstRoot, rim, opening: false);
            ControlTemplate rowTemplate = first.Template;
            first.Template = null; first.ApplyTemplate(); Layout(window);
            first.Template = rowTemplate; first.ApplyTemplate(); Layout(window);
            firstState.IsOpen = true;
            firstRoot = Part<FrameworkElement>(first, "PART_Root");
            Assert.Same(firstRoot, BindingOperations.GetBinding(rim, UIElement.OpacityProperty)!.Source);
            SampleTransition(firstRoot, rim, opening: true); Finish(firstRoot);

            firstState.IsOpen = false; SampleTransition(firstRoot, rim, opening: false);
            ControlTemplate barTemplate = messages.Template;
            messages.Template = null; messages.ApplyTemplate(); Layout(window);
            Assert.False(BindingOperations.IsDataBound(rim, UIElement.OpacityProperty));
            AssertRest(rim);
            messages.Template = barTemplate; messages.ApplyTemplate(); Layout(window);
            rim = Part<Border>(messages, "ExposedTopBorder");
            second = Assert.IsType<RibbonMessage>(messages.ItemContainerGenerator.ContainerFromIndex(1));
            secondRoot = Part<FrameworkElement>(second, "PART_Root");
            Assert.Same(secondRoot, BindingOperations.GetBinding(rim, UIElement.OpacityProperty)!.Source);
            Finish(secondRoot);
            AssertRest(rim);

            secondState.IsOpen = false; SampleTransition(secondRoot, rim, opening: false);
            ribbon.MessageBar = null; Layout(window);
            Assert.False(BindingOperations.IsDataBound(rim, UIElement.OpacityProperty));
            AssertRest(rim);
            secondState.IsOpen = true;
            ribbon.MessageBar = messages; Layout(window);
            rim = Part<Border>(messages, "ExposedTopBorder");
            second = Assert.IsType<RibbonMessage>(messages.ItemContainerGenerator.ContainerFromIndex(1));
            secondRoot = Part<FrameworkElement>(second, "PART_Root");
            SampleTransition(secondRoot, rim, opening: true); Finish(secondRoot); AssertRest(rim);

            RibbonAnimation.RespectSystemReduceMotion = true;
            secondState.IsOpen = false;
            if (RibbonAnimation.SystemReduceMotion)
            {
                Assert.False(second.IsPresented); AssertRest(rim);
                secondState.IsOpen = true; AssertRest(secondRoot); AssertRest(rim);
            }
            else
            {
                SampleTransition(secondRoot, rim, opening: false);
                PumpUntil(() => !second.IsPresented);
            }
        }
        finally { window.Close(); Layout(window); }
    }

    private sealed class MessageState : INotifyPropertyChanged
    {
        private bool _isOpen = true;
        public bool IsOpen
        {
            get => _isOpen;
            set { _isOpen = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOpen))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private static (RibbonWindow, Ribbon) Create(RibbonMessageBar messages,
        RibbonDensity density, RibbonQuickAccessPosition placement)
    {
        var ribbon = new Ribbon { IsMinimized = true, Density = density,
            QuickAccessPosition = placement, MessageBar = messages };
        var tab = new RibbonTab { Header = "Home" };
        ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save" });
        var content = new DockPanel(); DockPanel.SetDock(ribbon, Dock.Top); content.Children.Add(ribbon);
        content.Children.Add(new Border { Background = Brushes.White });
        return (new RibbonWindow { Content = content, Width = 760, Height = 350,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false }, ribbon);
    }

    private static void SampleTransition(FrameworkElement root, Border rim, bool opening)
    {
        Assert.True(root.HasAnimatedProperties);
        PumpUntil(() => root.Opacity > .1 && root.Opacity < .9);
        Assert.Equal(root.Opacity, rim.Opacity, 6);
        Assert.Equal(Matrix.Identity, rim.RenderTransform.Value);
        Assert.True(root.RenderTransform.Value.OffsetY < 0, opening ? "Entrance glide" : "Exit glide");
        Assert.Equal(Transform.Identity, rim.LayoutTransform);
    }

    private static void Finish(FrameworkElement root) => PumpUntil(() => !root.HasAnimatedProperties);

    private static void AssertRest(FrameworkElement element)
    {
        Assert.Equal(1d, element.Opacity);
        Assert.Equal(Matrix.Identity, element.RenderTransform.Value);
        Assert.False(element.HasAnimatedProperties);
    }

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }

    private static void SavePreview(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_MESSAGE_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        DpiScale dpi = VisualTreeHelper.GetDpi(element);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY,
            PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(file);
    }

    private static void PumpUntil(Func<bool> condition)
    {
        if (condition()) return;
        var frame = new DispatcherFrame();
        var watch = Stopwatch.StartNew();
        // Observe actual render frames; background polling can miss a short fade while WPF
        // is processing template/theme work, especially when capturing transition renders.
        EventHandler render = (_, _) => { if (condition()) frame.Continue = false; };
        CompositionTarget.Rendering += render;
        var timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) => { if (condition() || watch.Elapsed > TimeSpan.FromSeconds(2)) frame.Continue = false; };
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); CompositionTarget.Rendering -= render; }
        Assert.True(condition(), "The message transition did not reach its expected state within two seconds.");
    }
}
