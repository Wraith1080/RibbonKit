using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Layout;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Exercises live motion through an independent consumer; no Showcase resources/helpers.
internal static class DensityTransitionPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var level = RibbonAnimation.GlobalLevel;
        var reduce = RibbonAnimation.RespectSystemReduceMotion;
        var action = RibbonAnimation.GetActionOverride(RibbonAnimationAction.DensityChange);
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;
        RibbonAnimation.RespectSystemReduceMotion = false;
        RibbonAnimation.ClearActionLevel(RibbonAnimationAction.DensityChange);
        try
        {
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (RibbonQuickAccessPosition placement in Enum.GetValues<RibbonQuickAccessPosition>())
            foreach (bool restoreInLoaded in new[] { false, true })
                VerifyPlacement(application, theme, placement, restoreInLoaded);
            VerifyInterruption(application);
            VerifyPopupAnchor(application);
        }
        finally
        {
            RibbonAnimation.GlobalLevel = level;
            RibbonAnimation.RespectSystemReduceMotion = reduce;
            if (action is { } value) RibbonAnimation.SetActionLevel(RibbonAnimationAction.DensityChange, value);
            else RibbonAnimation.ClearActionLevel(RibbonAnimationAction.DensityChange);
        }
    }

    private static void VerifyPlacement(Application application, RibbonTheme theme,
        RibbonQuickAccessPosition placement, bool restoreInLoaded)
    {
        RibbonAnimation.GlobalLevel = restoreInLoaded ? RibbonAnimationLevel.Expressive : RibbonAnimationLevel.Subtle;
        ThemeManager.Apply(application, theme);
        var (window, ribbon, adaptive, gallery, message) = Create(placement);
        ribbon.FlowDirection = restoreInLoaded ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        if (restoreInLoaded) window.Loaded += (_, _) => ribbon.Density = RibbonDensity.Touch;
        else ribbon.Density = RibbonDensity.Touch;
        try
        {
            window.Show(); Layout(window);
            var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
            Assert.False(tabs.HasAnimatedProperties); // Persisted startup never fades.
            WaitForRender(window);
            Assert.False(tabs.HasAnimatedProperties);
            double touchHeight = ribbon.ActualHeight;
            AssertCollapsed(adaptive);
            ribbon.Density = RibbonDensity.Compact;
            Layout(window);
            double compactHeight = ribbon.ActualHeight;
            Assert.True(compactHeight < touchHeight);
            Assert.True(tabs.HasAnimatedProperties);
            FrameworkElement? qat = placement switch
            {
                RibbonQuickAccessPosition.BelowRibbon => Part<Border>(ribbon, "QatBelowHost"),
                RibbonQuickAccessPosition.TitleBar => Assert.IsType<RibbonQuickAccessToolBar>(window.TitleBarContent),
                _ => null,
            };
            if (qat is not null) Assert.True(qat.HasAnimatedProperties);
            if (placement == RibbonQuickAccessPosition.BelowRibbon)
            {
                var transform = Assert.IsType<TransformGroup>(qat!.RenderTransform);
                var glide = Assert.IsType<TranslateTransform>(transform.Children[^1]);
                Assert.InRange(glide.Y, 0, RibbonAnimation.GetSlideOffset(RibbonAnimationAction.DensityChange));
                Assert.Equal(0, glide.X);
            }
            else if (qat is not null) Assert.Equal(Matrix.Identity, qat.RenderTransform.Value);
            Assert.Equal(Matrix.Identity, tabs.RenderTransform.Value); // Text/icons are never scaled.
            Assert.Equal(Transform.Identity, tabs.LayoutTransform);
            double titleHeight = Part<Grid>(window, "TitleBarBand").ActualHeight;
            PumpUntil(() => !tabs.HasAnimatedProperties);
            Assert.Equal(compactHeight, ribbon.ActualHeight);
            Assert.Equal(titleHeight, Part<Grid>(window, "TitleBarBand").ActualHeight);
            AssertRest(qat);
            if (theme == RibbonTheme.Office2024 && placement == RibbonQuickAccessPosition.BelowRibbon && !restoreInLoaded)
                SavePreview(window, "density-compact-rest");

            // Change density from an open collapsed flyout with motion actually enabled.
            AssertCollapsed(adaptive);
            Part<ToggleButton>(adaptive, "PART_CollapsedButton").IsChecked = true;
            Layout(window);
            Assert.NotNull(Part<Border>(adaptive, "PART_PopupHost").Child);
            ribbon.Density = RibbonDensity.Touch;
            Layout(window);
            AssertCollapsed(adaptive);
            Assert.Null(Part<Border>(adaptive, "PART_PopupHost").Child);
            Assert.Equal(touchHeight, ribbon.ActualHeight);
            Assert.Equal(7, gallery.SelectedIndex);
            Assert.True(message.ActualHeight >= 52);
            Assert.True(Part<Button>(message, "PART_ActionButton").ActualHeight >= 44);
            var viewer = Part<ScrollViewer>(gallery, "PART_ScrollViewer");
            var selected = Assert.IsType<RibbonGalleryItem>(gallery.Items[7]);
            var selectedBounds = selected.TransformToAncestor(viewer).TransformBounds(new Rect(selected.RenderSize));
            Assert.True(selectedBounds.Bottom > 0 && selectedBounds.Top < viewer.ActualHeight);
            if (theme == RibbonTheme.Office2024 && placement == RibbonQuickAccessPosition.BelowRibbon && !restoreInLoaded)
            {
                PumpUntil(() => tabs.Opacity < 1d);
                SavePreview(window, "density-touch-settling");
            }
            if (placement == RibbonQuickAccessPosition.TitleBar)
            {
                Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(window.TitleBarContent as DependencyObject ?? window));
                Assert.Equal(46, WindowChrome.GetWindowChrome(window).CaptionHeight);
                Assert.True(Part<Grid>(window, "TitleBarBand").ActualHeight >= 46);
            }
            var scroll = Part<RibbonScrollContentHost>(tabs, "PART_ContentScroll");
            adaptive.CanResize = false; Layout(window);
            Assert.True(scroll.CanScrollRight);
            Assert.True(scroll.ExtentWidth > scroll.ViewportWidth);
            Assert.True(Descendants<RepeatButton>(tabs).Single(button =>
                ReferenceEquals(button.Command, scroll.ScrollRightCommand)).IsVisible);
            double extent = scroll.ExtentWidth;
            PumpUntil(() => !tabs.HasAnimatedProperties);
            Assert.Equal(touchHeight, ribbon.ActualHeight);
            Assert.Equal(extent, scroll.ExtentWidth);
            AssertRest(qat);
            if (theme == RibbonTheme.Office2024 && placement == RibbonQuickAccessPosition.BelowRibbon && !restoreInLoaded)
                SavePreview(window, "density-touch-rest");
        }
        finally { window.Close(); }
    }

    private static void VerifyInterruption(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var (window, ribbon, _, _, _) = Create(RibbonQuickAccessPosition.BelowRibbon);
        try
        {
            window.Show(); Layout(window); WaitForRender(window);
            var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
            var qat = Part<Border>(ribbon, "QatBelowHost");
            var original = new TranslateTransform(2, 3);
            qat.SetBinding(UIElement.RenderTransformProperty, new Binding(".") { Source = original });
            tabs.SetBinding(UIElement.OpacityProperty, new Binding(".") { Source = 0.8d });
            ribbon.Density = RibbonDensity.Touch;
            ribbon.Density = RibbonDensity.Compact;
            ribbon.Density = RibbonDensity.Touch;
            Layout(window);
            Assert.True(tabs.HasAnimatedProperties);
            Assert.Same(original, Assert.IsType<TransformGroup>(qat.RenderTransform).Children[0]);
            PumpUntil(() => tabs.Opacity < 0.74d);
            Assert.InRange(tabs.Opacity, 0.68d, 0.74d);
            ribbon.Density = RibbonDensity.Compact; Layout(window);
            PumpUntil(() => !tabs.HasAnimatedProperties);
            Assert.Equal(0.8d, tabs.Opacity);
            Assert.Same(original, qat.RenderTransform);
            Assert.True(BindingOperations.IsDataBound(tabs, UIElement.OpacityProperty));
            Assert.True(BindingOperations.IsDataBound(qat, UIElement.RenderTransformProperty));

            // A pending toggle and an active transition both respect disabling motion.
            ribbon.Density = RibbonDensity.Touch;
            RibbonAnimation.SetActionLevel(RibbonAnimationAction.DensityChange, RibbonAnimationLevel.None);
            Layout(window); Assert.False(tabs.HasAnimatedProperties); Assert.Same(original, qat.RenderTransform);
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.DensityChange);
            ribbon.Density = RibbonDensity.Compact; Layout(window);
            Assert.True(tabs.HasAnimatedProperties);
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            Assert.False(tabs.HasAnimatedProperties); Assert.Same(original, qat.RenderTransform);
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;

            // Policy respects the real OS preference without changing system settings.
            ribbon.Density = RibbonDensity.Touch; Layout(window);
            RibbonAnimation.RespectSystemReduceMotion = true;
            if (RibbonAnimation.SystemReduceMotion) Assert.False(tabs.HasAnimatedProperties);
            RibbonAnimation.RespectSystemReduceMotion = false;

            ribbon.Density = RibbonDensity.Compact; Layout(window);
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
            Assert.False(tabs.HasAnimatedProperties); Assert.Same(original, qat.RenderTransform);
            Layout(window);
            ribbon.Density = RibbonDensity.Touch; Layout(window);
            ribbon.Template = FreshSharedTemplate();
            ribbon.ApplyTemplate(); Layout(window);
            Assert.False(tabs.HasAnimatedProperties);
            Assert.Same(original, qat.RenderTransform);
            var replacement = Part<RibbonTabControl>(ribbon, "TabControlHost");
            Assert.NotSame(tabs, replacement);
            ribbon.Density = RibbonDensity.Compact; Layout(window);
            Assert.True(replacement.HasAnimatedProperties);
            object content = window.Content;
            window.Content = null; Layout(window);
            Assert.False(replacement.HasAnimatedProperties);
            window.Content = content; Layout(window);
            Assert.False(Part<RibbonTabControl>(ribbon, "TabControlHost").HasAnimatedProperties);
            WaitForRender(window);
            // Cancel a queued transition on unload as well as an already-running one.
            ribbon.Density = RibbonDensity.Touch;
            window.Content = null; Layout(window);
            Assert.False(replacement.HasAnimatedProperties);
        }
        finally { window.Close(); }
    }

    private static void VerifyPopupAnchor(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var (window, ribbon, _, _, _) = Create(RibbonQuickAccessPosition.BelowRibbon);
        ribbon.QuickAccessItems.Clear();
        var dropdown = new RibbonDropDownButton { Header = "Save options", Size = RibbonControlSize.Small };
        dropdown.Items.Add(new RibbonMenuItem { Header = "Save as" });
        ribbon.QuickAccessItems.Add(dropdown);
        try
        {
            window.Show(); Layout(window); WaitForRender(window);
            dropdown.IsDropDownOpen = true; Layout(window);
            var popup = Part<Popup>(dropdown, "PART_Popup");
            var anchor = popup.PlacementTarget;
            var placement = popup.Placement;
            var qat = Part<Border>(ribbon, "QatBelowHost");
            ribbon.Density = RibbonDensity.Touch; Layout(window);
            Assert.True(popup.IsOpen);
            Assert.Same(anchor, popup.PlacementTarget);
            Assert.Equal(placement, popup.Placement);
            Assert.Equal(Matrix.Identity, qat.RenderTransform.Value); // No glide of a live anchor.
            Assert.True(qat.HasAnimatedProperties); // Its opacity still settles.
            dropdown.IsDropDownOpen = false; Layout(window);
            ribbon.Density = RibbonDensity.Compact; Layout(window);
            Assert.IsType<TransformGroup>(qat.RenderTransform);
            // Minimize owns its existing QAT glide; density must release its layer first.
            ribbon.IsMinimized = true;
            Assert.False(Part<RibbonTabControl>(ribbon, "TabControlHost").HasAnimatedProperties);
            Assert.IsType<TranslateTransform>(qat.RenderTransform);
            PumpUntil(() => Part<Border>(Part<RibbonTabControl>(ribbon, "TabControlHost"), "ContentHost").Visibility == Visibility.Collapsed);
            AssertRest(qat);
        }
        finally { window.Close(); }
    }

    private static ControlTemplate FreshSharedTemplate()
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/RibbonKit;component/Themes/Office2024.xaml"),
        };
        return (ControlTemplate)((Style)dictionary[typeof(Ribbon)]).Setters.OfType<Setter>()
            .Single(setter => setter.Property == Control.TemplateProperty).Value;
    }

    private static (RibbonWindow, Ribbon, RibbonGroup, InRibbonGallery, RibbonMessage) Create(RibbonQuickAccessPosition placement)
    {
        var ribbon = new Ribbon { QuickAccessPosition = placement, QuickAccessMaxWidth = 120,
            Backstage = new Backstage() };
        var tab = new RibbonTab { Header = "Home" };
        var fixedGroup = new RibbonGroup { Header = "Fixed", CanResize = false, Width = 340 };
        fixedGroup.Items.Add(new RibbonButton { Header = "Command", Size = RibbonControlSize.Large });
        var adaptive = new RibbonGroup { Header = "Adaptive" };
        for (int i = 0; i < 7; i++) adaptive.Items.Add(new RibbonButton { Header = $"Command {i}" });
        var gallery = new InRibbonGallery { Width = 180, SelectedIndex = 7 };
        for (int i = 0; i < 10; i++) gallery.Items.Add(new RibbonGalleryItem { Content = $"Style {i}" });
        var galleryGroup = new RibbonGroup { Header = "Gallery", CanResize = false };
        galleryGroup.Items.Add(gallery);
        tab.Groups.Add(fixedGroup); tab.Groups.Add(adaptive); tab.Groups.Add(galleryGroup);
        ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        for (int i = 0; i < 5; i++) ribbon.QuickAccessItems.Add(new RibbonButton { Header = $"Save {i}" });
        var message = new RibbonMessage { Message = "Notice", ActionContent = "Continue" };
        var messages = new RibbonMessageBar(); messages.Items.Add(message); ribbon.MessageBar = messages;
        var content = new DockPanel(); DockPanel.SetDock(ribbon, Dock.Top); content.Children.Add(ribbon);
        var window = new RibbonWindow { Content = content, Width = 680, Height = 500,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        return (window, ribbon, adaptive, gallery, message);
    }

    private static void AssertCollapsed(RibbonGroup group) => Assert.Equal(RibbonGroupSizeState.Collapsed, group.SizeState);
    private static void AssertRest(FrameworkElement? element)
    {
        if (element is null) return;
        Assert.False(element.HasAnimatedProperties);
        Assert.Equal(1d, element.Opacity);
        Assert.Equal(Matrix.Identity, element.RenderTransform.Value);
    }

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }

    private static void WaitForRender(Window window)
    {
        bool rendered = false;
        EventHandler handler = (_, _) => rendered = true;
        CompositionTarget.Rendering += handler;
        try { window.InvalidateVisual(); PumpUntil(() => rendered); }
        finally { CompositionTarget.Rendering -= handler; }
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var frame = new DispatcherFrame();
        var watch = Stopwatch.StartNew();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) => { if (condition() || watch.Elapsed > TimeSpan.FromSeconds(2)) frame.Continue = false; };
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); }
        Assert.True(condition(), "The density transition/render did not reach its expected state within two seconds.");
    }

    private static void SavePreview(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_TOUCH_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth),
            (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(file);
    }
}
