using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Runs in the existing library-only STA/Application; no Showcase assets or helpers.
internal static class CrystalTintPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var animation = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var context = new RibbonTab { Header = "Picture أدوات", IsContextual = true, ContextualColor = Brushes.Teal };
        var home = new RibbonTab { Header = "Home" };
        context.Groups.Add(new RibbonGroup { Header = "Picture commands" });
        home.Groups.Add(new RibbonGroup { Header = "Home commands" });
        var ribbon = new Ribbon();
        ribbon.Tabs.Add(home);
        ribbon.Tabs.Add(context);
        ribbon.SelectedTab = context;
        var secondRibbon = new Ribbon();
        var secondContext = new RibbonTab { Header = "Table", IsContextual = true, ContextualColor = Brushes.Purple };
        secondContext.Groups.Add(new RibbonGroup { Header = "Table commands" });
        secondRibbon.Tabs.Add(secondContext);
        var window = Window(ribbon);
        var secondWindow = Window(secondRibbon);
        const string surfaceKey = "RibbonKit.Brushes.Ribbon.ContentBackground";
        try
        {
            ThemeManager.ClearAccent(application);
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            int changed = 0;
            EventHandler handler = (_, _) => changed++;
            ThemeManager.Changed += handler;
            var palette = ThemeManager.CreatePalette(RibbonTheme.CrystalLight, Colors.Purple);
            var secondPalette = ThemeManager.CreatePalette(RibbonTheme.CrystalLight, Colors.SeaGreen, true);
            ThemeManager.Changed -= handler;
            Assert.Equal(0, changed);
            Assert.Equal(RibbonTheme.Office2024, ThemeManager.CurrentTheme);
            Assert.False(ThemeManager.IsDarkMode);
            window.Resources.MergedDictionaries.Add(palette);
            secondWindow.Resources.MergedDictionaries.Add(secondPalette);
            window.Show();
            secondWindow.Show();
            Layout(window);
            Layout(secondWindow);
            var chrome = Part<Border>(context, "HeaderChrome");
            var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
            var marker = Part<Rectangle>(tabs, "PART_TabMarker");
            var secondChrome = Part<Border>(secondContext, "HeaderChrome");
            var secondMaterial = XamlWriter.Save(secondWindow.FindResource(surfaceKey));
            var secondContextMaterial = XamlWriter.Save(secondChrome.Background);
            Assert.Null(context.ContextualSelectionBrush);
            Assert.Empty(context.Resources.Keys.Cast<object>());
            Assert.IsType<DrawingBrush>(marker.Fill);
            Assert.Equal(Mix(Colors.Teal, Colors.White, 0.72),
                Assert.IsType<RadialGradientBrush>(chrome.Background).GradientStops[2].Color);
            Assert.Equal(Mix(Colors.Purple, Colors.Black, 0.72),
                Assert.IsType<RadialGradientBrush>(secondChrome.Background).GradientStops[2].Color);
            Assert.NotEqual(XamlWriter.Save(window.FindResource(surfaceKey)), secondMaterial);
            Assert.IsType<DrawingBrush>(chrome.BorderBrush);

            var oldMarker = marker.Fill;
            var tint = new SolidColorBrush(Colors.Teal);
            context.ContextualColor = tint;
            Layout(window);
            tint.Color = Colors.Goldenrod;
            Layout(window);
            Assert.Equal(Mix(Colors.Goldenrod, Colors.White, 0.72),
                Assert.IsType<RadialGradientBrush>(chrome.Background).GradientStops[2].Color);
            Assert.NotSame(oldMarker, marker.Fill);
            context.ContextualSelectionBrush = Brushes.Orange;
            Layout(window);
            Assert.Same(Brushes.Orange, marker.Fill);
            context.ContextualColor = Brushes.Coral;
            Layout(window);
            Assert.Same(Brushes.Orange, marker.Fill);
            context.ClearValue(RibbonTab.ContextualSelectionBrushProperty);
            Layout(window);
            Assert.IsType<DrawingBrush>(marker.Fill);
            context.Foreground = Brushes.Lime;
            Layout(window);
            Assert.Same(Brushes.Lime, Part<ContentPresenter>(context, "ContextualHeaderText").GetValue(TextElement.ForegroundProperty));
            context.ClearValue(Control.ForegroundProperty);

            context.Resources["RibbonKit.Brushes.Tab.SelectedBackground"] = Brushes.Orange;
            context.Resources["RibbonKit.Brushes.Tab.SelectedUnderline"] = Brushes.Red;
            context.Resources["RibbonKit.Brushes.Tab.ContextualForeground"] = Brushes.Brown;
            Layout(window);
            Assert.Same(Brushes.Orange, chrome.Background);
            Assert.Same(Brushes.Red, marker.Fill);
            Assert.Same(Brushes.Brown, Part<ContentPresenter>(context, "ContextualHeaderText").GetValue(TextElement.ForegroundProperty));
            context.Resources.Clear();
            Layout(window);

            var gradient = new LinearGradientBrush(Colors.Green, Colors.Purple, 45);
            using (var errors = new StringWriter())
            using (var listener = new System.Diagnostics.TextWriterTraceListener(errors))
            {
                var source = System.Diagnostics.PresentationTraceSources.DataBindingSource;
                var previousLevel = source.Switch.Level;
                source.Switch.Level = System.Diagnostics.SourceLevels.Error;
                source.Listeners.Add(listener);
                try
                {
                    context.ContextualColor = gradient;
                    Layout(window);
                    Assert.Same(gradient, marker.Fill);
                    Assert.Same(context.FindResource("RibbonKit.Brushes.Tab.SelectedBackground"), chrome.Background);
                    listener.Flush();
                    Assert.Equal(string.Empty, errors.ToString());
                }
                finally
                {
                    source.Listeners.Remove(listener);
                    source.Switch.Level = previousLevel;
                }
            }
            context.ContextualColor = Brushes.Teal;
            ribbon.SelectedTab = home;
            Layout(window);
            var mouseKey = (DependencyPropertyKey)typeof(UIElement).GetField("IsMouseOverPropertyKey",
                BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            chrome.SetValue(mouseKey, true);
            Layout(window);
            Assert.Equal(Color.FromArgb(28, 0, 128, 128), Assert.IsType<SolidColorBrush>(chrome.Background).Color);
            Assert.Equal(0.6, chrome.BorderBrush.Opacity);
            chrome.SetValue(mouseKey, false);
            ribbon.SelectedTab = context;
            Layout(window);

            window.FlowDirection = FlowDirection.RightToLeft;
            Layout(window);
            Assert.Equal(FlowDirection.RightToLeft, context.FlowDirection);
            context.ContextualColor = null;
            Layout(window);
            var accentBefore = Assert.IsType<SolidColorBrush>(context.ContextualBrush).Color;
            var replacement = ThemeManager.CreatePalette(RibbonTheme.CrystalLight, Colors.Coral, true);
            window.Resources.MergedDictionaries[0] = replacement;
            Layout(window);
            Assert.NotEqual(accentBefore, Assert.IsType<SolidColorBrush>(context.ContextualBrush).Color);
            Assert.Equal(Mix(Assert.IsType<SolidColorBrush>(context.ContextualBrush).Color, Colors.Black, 0.72),
                Assert.IsType<RadialGradientBrush>(chrome.Background).GradientStops[2].Color);
            Assert.Equal(secondMaterial, XamlWriter.Save(secondWindow.FindResource(surfaceKey)));
            Assert.Equal(secondContextMaterial, XamlWriter.Save(secondChrome.Background));
            window.Resources.MergedDictionaries.Clear();
            Layout(window);
            Assert.IsType<SolidColorBrush>(marker.Fill);
            Assert.Same(context.FindResource("RibbonKit.Brushes.Tab.SelectedBackground"), chrome.Background);
            Assert.Equal(secondMaterial, XamlWriter.Save(secondWindow.FindResource(surfaceKey)));

            // Generic factory parity: Office uses exactly the existing accent policy.
            foreach (var theme in new[] { RibbonTheme.Office2007, RibbonTheme.Office2010,
                RibbonTheme.Office2013, RibbonTheme.Office2019, RibbonTheme.Office2024 })
            foreach (bool dark in new[] { false, true })
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                ThemeManager.SetAccent(application, Colors.Purple);
                var officePalette = ThemeManager.CreatePalette(theme, Colors.Purple, dark);
                foreach (string key in new[] { "Accent", "Tab.SelectedForeground", "Tab.SelectedUnderline",
                    "Control.CheckedBackground", "ApplicationButton.Background", "Dialog.PrimaryBackground",
                    "MdiChild.ActiveCaptionBackground" })
                {
                    string resourceKey = "RibbonKit.Brushes." + key;
                    Assert.Equal(XamlWriter.Save(application.Resources[resourceKey]), XamlWriter.Save(officePalette[resourceKey]));
                }
                window.Resources.MergedDictionaries.Add(officePalette);
                Layout(window);
                Assert.Same(officePalette["RibbonKit.Brushes.Tab.SelectedBackground"], chrome.Background);
                window.Resources.MergedDictionaries.Clear();
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => ThemeManager.CreatePalette((RibbonTheme)999));
        }
        finally
        {
            window.Close();
            secondWindow.Close();
            ThemeManager.ClearAccent(application);
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            RibbonAnimation.GlobalLevel = animation;
        }
    }

    private static Window Window(Ribbon ribbon) => new RibbonWindow { Content = ribbon, Width = 720, Height = 330,
        Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
    private static T Part<T>(Control control, string name) where T : DependencyObject =>
        Assert.IsType<T>(control.Template.FindName(name, control));
    private static Color Mix(Color color, Color target, double amount) => Color.FromRgb(
        (byte)(color.R + (target.R - color.R) * amount), (byte)(color.G + (target.G - color.G) * amount),
        (byte)(color.B + (target.B - color.B) * amount));
    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }
}
