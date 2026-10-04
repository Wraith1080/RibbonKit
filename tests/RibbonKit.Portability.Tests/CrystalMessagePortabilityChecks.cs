using System.Windows;
using System.Reflection;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Reuses the consumer fixture's single STA/Application; never loads Showcase.
internal static class CrystalMessagePortabilityChecks
{
    internal static void Verify(Application application)
    {
        var animation = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var command = new ActionCommand();
        var first = new RibbonMessage { Title = "PROTECTED VIEW", Message = "Review this document before editing.",
            ActionContent = "Enable _Editing", ActionCommand = command, ActionCommandParameter = "edit" };
        var second = new RibbonMessage { Title = "SECURITY NOTICE", Message = new string('W', 200),
            IsOpen = false, IsDismissible = false };
        var bar = new RibbonMessageBar { ItemsSource = new[] { first, second } };
        var ribbon = new Ribbon { MessageBar = bar, Backstage = new Backstage() };
        var home = new RibbonTab { Header = "Home" };
        home.Groups.Add(new RibbonGroup { Header = "Commands" });
        ribbon.Tabs.Add(home);
        ribbon.SelectedTab = home;
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save" });
        var window = new RibbonWindow { Content = ribbon, Width = 800, Height = 480,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            ThemeManager.SetDarkMode(application, false);
            window.Show();
            Layout();
            var action = Part<Button>(first, "PART_ActionButton");
            var chrome = Part<Border>(action, "Chrome");
            Assert.Equal(new CornerRadius(12), chrome.CornerRadius);
            var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
            var body = Part<Border>(tabs, "ContentHost");
            var root = Part<Border>(first, "PART_Root");
            second.IsOpen = true;
            Layout();
            var secondRoot = Part<Border>(second, "PART_Root");
            var lightPaint = chrome.Background;

            foreach (bool dark in new[] { false, true, false })
            {
                ThemeManager.SetDarkMode(application, dark);
                foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
                foreach (var position in Enum.GetValues<RibbonQuickAccessPosition>())
                foreach (bool minimized in new[] { false, true })
                {
                    ribbon.FlowDirection = direction;
                    ribbon.QuickAccessPosition = position;
                    ribbon.IsMinimized = minimized;
                    first.IsOpen = true;
                    second.IsOpen = true;
                    Layout();
                    Assert.True(ribbon.HasOpenMessages);
                    Assert.Equal(new CornerRadius(14), body.CornerRadius);
                    Assert.Equal(new CornerRadius(10), root.CornerRadius);
                    Assert.Equal(new CornerRadius(10), secondRoot.CornerRadius);
                    Assert.Equal(new Thickness(7, 3, 7, 7), bar.Margin);
                    Assert.Equal(new Thickness(0, 2, 0, 2), first.Margin);
                    Assert.Same(first.FindResource("RibbonKit.Brushes.MessageBar.Background"), root.Background);
                    Assert.Same(first.FindResource("RibbonKit.Brushes.Control.CheckedBackground"), chrome.Background);
                    Assert.Same(first.FindResource("RibbonKit.Brushes.Control.HoverBorder"), chrome.BorderBrush);
                    Assert.Equal(new CornerRadius(12), chrome.CornerRadius);
                    Assert.Equal(Cursors.Hand, action.Cursor);
                    Assert.Equal(Visibility.Collapsed, Part<Button>(second, "PART_ActionButton").Visibility);
                    Assert.Equal(Visibility.Collapsed, Part<Button>(second, "PART_CloseButton").Visibility);
                    Assert.True(second.ActualHeight > first.ActualHeight); // Wrapped body grows without clipping.
                    var firstBounds = Bounds(root, ribbon);
                    Assert.True(Bounds(secondRoot, ribbon).Top > firstBounds.Bottom);
                    if (position == RibbonQuickAccessPosition.BelowRibbon)
                        Assert.True(firstBounds.Top > Bounds(Part<Border>(ribbon, "QatBelowHost"), ribbon).Bottom);
                    else if (!minimized)
                        Assert.True(firstBounds.Top > Bounds(body, ribbon).Bottom);
                    Assert.Equal(minimized ? Visibility.Collapsed : Visibility.Visible, body.Visibility);
                    Assert.True(direction == FlowDirection.LeftToRight
                        ? Bounds(action, window).Left > Bounds(Part<TextBlock>(first, "TitleText"), window).Left
                        : Bounds(action, window).Left < Bounds(Part<TextBlock>(first, "TitleText"), window).Left);
                    first.Dismiss();
                    Layout();
                    Assert.True(bar.HasOpenMessages);
                    Assert.False(first.IsPresented);
                    Assert.True(second.IsLastOpenMessage);
                    second.IsOpen = false;
                    Layout();
                    Assert.False(ribbon.HasOpenMessages);
                    Assert.Equal(new Thickness(), bar.Margin);
                    Assert.Null(bar.Effect);
                }
                if (dark) Assert.NotSame(lightPaint, chrome.Background);
            }

            first.IsOpen = true;
            Layout();
            int clicks = 0;
            first.ActionClick += (_, _) => clicks++;
            ((IInvokeProvider)new ButtonAutomationPeer(action)).Invoke();
            Layout();
            Assert.Equal(1, clicks);
            Assert.True(first.IsOpen); // Action closure remains a host policy.
            Assert.Equal("edit", command.ExecutedParameter);
            SetState(action, typeof(UIElement), "IsMouseOverPropertyKey", true);
            Assert.Same(first.FindResource("RibbonKit.Brushes.Tab.SelectedBackground"), chrome.Background);
            SetState(action, typeof(System.Windows.Controls.Primitives.ButtonBase), "IsPressedPropertyKey", true);
            Assert.Same(first.FindResource("RibbonKit.Brushes.Control.PressedBackground"), chrome.Background);
            SetState(action, typeof(UIElement), "IsKeyboardFocusedPropertyKey", true);
            Assert.Same(first.FindResource("RibbonKit.Brushes.Accent"), chrome.BorderBrush);
            SetState(action, typeof(UIElement), "IsKeyboardFocusedPropertyKey", false);
            SetState(action, typeof(System.Windows.Controls.Primitives.ButtonBase), "IsPressedPropertyKey", false);
            SetState(action, typeof(UIElement), "IsMouseOverPropertyKey", false);
            command.Enabled = false;
            command.Refresh();
            Layout();
            Assert.False(action.IsEnabled);
            Assert.Equal(0.4, action.Opacity);
            command.Enabled = true;
            command.Refresh();
            Layout();
            int dismissals = 0;
            first.Dismissed += (_, _) => dismissals++;
            Part<Button>(first, "PART_CloseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Layout();
            Assert.Equal(1, dismissals);
            Assert.False(first.IsOpen);

            // Existing realized parts track scoped resources and explicit host values.
            first.IsOpen = true;
            Layout();
            const string corners = "RibbonKit.Metrics.MessageBar.ActionCornerRadius";
            first.Resources[corners] = new CornerRadius(5);
            Layout();
            Assert.Equal(new CornerRadius(5), chrome.CornerRadius);
            first.Resources[corners] = new CornerRadius(7);
            Layout();
            Assert.Equal(new CornerRadius(7), chrome.CornerRadius);
            chrome.CornerRadius = new CornerRadius(3);
            action.Background = Brushes.Magenta;
            first.Background = Brushes.Lime;
            foreach (bool dark in new[] { true, false })
            {
                ThemeManager.SetDarkMode(application, dark);
                Layout();
                Assert.Same(chrome, Part<Border>(action, "Chrome"));
                Assert.Equal(new CornerRadius(3), chrome.CornerRadius);
                Assert.Same(Brushes.Magenta, chrome.Background);
                Assert.Same(Brushes.Lime, root.Background);
            }
            chrome.ClearValue(Border.CornerRadiusProperty);
            action.ClearValue(Control.BackgroundProperty);
            first.ClearValue(Control.BackgroundProperty);
            first.Resources.Remove(corners);
            Layout();
            Assert.Equal(new CornerRadius(12), chrome.CornerRadius);
            Assert.Same(first.FindResource("RibbonKit.Brushes.Control.CheckedBackground"), chrome.Background);

            var hostStyle = new Style(typeof(Button), action.Style);
            hostStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Magenta));
            action.Style = hostStyle;
            ThemeManager.SetDarkMode(application, true);
            Layout();
            chrome = Part<Border>(action, "Chrome");
            Assert.Same(hostStyle, action.Style);
            Assert.Same(Brushes.Magenta, chrome.Background);
            action.ClearValue(FrameworkElement.StyleProperty);
            Layout();
            Assert.Same(first.FindResource("RibbonKit.Styles.MessageBar.ActionButton"), action.Style);

            // The existing template key remains an independently scoped extension point.
            const string templateKey = "RibbonKit.Templates.MessageBar.ActionButton";
            var hostTemplate = new ControlTemplate(typeof(Button))
            { VisualTree = new FrameworkElementFactory(typeof(Border), "HostActionChrome") };
            first.Resources[templateKey] = hostTemplate;
            Layout();
            Assert.Same(hostTemplate, action.Template);
            Assert.NotNull(hostTemplate.FindName("HostActionChrome", action));
            first.Resources.Remove(templateKey);
            Layout();
            chrome = Part<Border>(action, "Chrome");

            var semanticBackground = root.Background;
            ThemeManager.SetAccent(application, Colors.Purple);
            Layout();
            Assert.Same(semanticBackground, root.Background);
            ThemeManager.ClearAccent(application);
            foreach (var theme in new[] { RibbonTheme.Office2007, RibbonTheme.Office2010,
                RibbonTheme.Office2013, RibbonTheme.Office2019, RibbonTheme.Office2024 })
            foreach (bool dark in new[] { false, true })
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
                ribbon.IsMinimized = false;
                Layout();
                chrome = Part<Border>(action, "Chrome");
                Assert.Equal(first.FindResource("RibbonKit.Metrics.SmallControlCornerRadius"), chrome.CornerRadius);
                Assert.Same(first.FindResource("RibbonKit.Brushes.Control.SurfaceBackground"), chrome.Background);
                Assert.Same(first.FindResource("RibbonKit.Brushes.ScreenTip.Border"), chrome.BorderBrush);
                action.IsEnabled = false;
                Layout();
                Assert.Equal(0.45, action.Opacity);
                action.ClearValue(UIElement.IsEnabledProperty);
                Layout();
                Assert.Equal(1, action.Opacity);
                Assert.Equal(first.FindResource("RibbonKit.Metrics.ContentCornerRadiusTop"), body.CornerRadius);
                Assert.Equal(first.FindResource("RibbonKit.Metrics.MessageBar.LastCornerRadius"), root.CornerRadius);
            }

            // Packaged dictionaries also work when merged by the host rather than ThemeManager.
            foreach (bool dark in new[] { false, true })
            {
                var scope = new ResourceDictionary();
                scope.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(
                    "/RibbonKit;component/Themes/Tokens.Crystal.Light.xaml", UriKind.Relative) });
                if (dark) scope.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(
                    "/RibbonKit;component/Themes/Tokens.Crystal.Dark.xaml", UriKind.Relative) });
                window.Resources.MergedDictionaries.Add(scope);
                Layout();
                chrome = Part<Border>(action, "Chrome");
                Assert.Equal(new CornerRadius(14), body.CornerRadius);
                Assert.Equal(new CornerRadius(12), chrome.CornerRadius);
                Assert.Same(first.FindResource("RibbonKit.Brushes.Control.CheckedBackground"), chrome.Background);
                window.Resources.MergedDictionaries.Remove(scope);
                Layout();
                chrome = Part<Border>(action, "Chrome");
                Assert.Equal(new CornerRadius(2), chrome.CornerRadius);
            }
        }
        finally
        {
            window.Close();
            ThemeManager.ClearAccent(application);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            ThemeManager.SetDarkMode(application, false);
            RibbonAnimation.GlobalLevel = animation;
        }
        void Layout()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
        }
    }

    private static T Part<T>(Control owner, string name) where T : FrameworkElement =>
        Assert.IsType<T>(owner.Template.FindName(name, owner));
    private static Rect Bounds(FrameworkElement element, Visual relativeTo) =>
        element.TransformToVisual(relativeTo).TransformBounds(new Rect(element.RenderSize));

    private static void SetState(DependencyObject control, Type declaringType, string keyName, bool value)
    {
        var key = Assert.IsType<DependencyPropertyKey>(declaringType.GetField(keyName,
            BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null));
        control.SetValue(key, value);
    }

    private sealed class ActionCommand : ICommand
    {
        internal bool Enabled { get; set; } = true;
        internal object? ExecutedParameter { get; private set; }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => Enabled;
        public void Execute(object? parameter) => ExecutedParameter = parameter;
        internal void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
