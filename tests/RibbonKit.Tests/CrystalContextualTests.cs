using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using Xunit;

namespace RibbonKit.Tests;

public class CrystalContextualTests
{
    [Fact]
    public void Contextual_marker_tracks_override_replacement_clear_and_color_changes() => Sta.Run(() =>
    {
        var context = new RibbonTab { Header = "Picture", IsContextual = true, ContextualColor = Brushes.Teal };
        var ordinary = new RibbonTab { Header = "Home" };
        var host = Host(context, ordinary);
        using var shown = new ShownHost(host);
        Layout(host);
        var marker = (Rectangle)host.Template.FindName("PART_TabMarker", host);
        Assert.Same(context.ContextualBrush, marker.Fill);

        var glass = new LinearGradientBrush(Colors.White, Colors.Teal, 90);
        context.ContextualSelectionBrush = glass;
        Sta.Drain();
        Assert.Same(glass, marker.Fill);
        Assert.Same(Brushes.Teal, context.ContextualBrush);
        context.ContextualSelectionBrush = Brushes.Violet;
        Sta.Drain();
        Assert.Same(Brushes.Violet, marker.Fill);
        context.ContextualSelectionBrush = null;
        context.ContextualColor = Brushes.Purple;
        Sta.Drain();
        Assert.Same(Brushes.Purple, marker.Fill);

        host.SelectedItem = ordinary;
        Layout(host);
        Assert.Same(Brushes.Blue, marker.Fill);
    });

    [Fact]
    public void Crystal_palette_follows_context_color_and_restores_standard_mode() => Sta.Run(() =>
    {
        var context = new CrystalContextualTab { Header = "Picture", IsContextual = true, ContextualColor = Brushes.Teal };
        var ordinary = new RibbonTab { Header = "Home" };
        var host = Host(context, ordinary);
        host.Resources.Remove("RibbonKit.Brushes.Tab.SelectedUnderline");
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit.Showcase;component/Themes/Crystal.Light.xaml", UriKind.Relative) });
        using var shown = new ShownHost(host);
        Layout(host);
        var marker = (Rectangle)host.Template.FindName("PART_TabMarker", host);
        var teal = Assert.IsType<RadialGradientBrush>(context.Resources["RibbonKit.Brushes.Tab.SelectedBackground"]);
        Assert.IsType<DrawingBrush>(marker.Fill);
        Assert.Same(context.ContextualSelectionBrush, marker.Fill);
        Assert.False(ordinary.Resources.Contains("RibbonKit.Brushes.Tab.SelectedBackground"));

        context.ContextualColor = Brushes.Purple;
        Sta.Drain();
        var purple = Assert.IsType<RadialGradientBrush>(context.Resources["RibbonKit.Brushes.Tab.SelectedBackground"]);
        Assert.NotEqual(teal.GradientStops[2].Color, purple.GradientStops[2].Color);
        Assert.Same(context.ContextualSelectionBrush, marker.Fill);
        var mutableTint = new SolidColorBrush(Colors.Teal);
        context.ContextualColor = mutableTint;
        Sta.Drain();
        var beforeMutation = context.ContextualSelectionBrush;
        mutableTint.Color = Colors.Goldenrod;
        Sta.Drain();
        Assert.NotSame(beforeMutation, context.ContextualSelectionBrush);
        Assert.Same(context.ContextualSelectionBrush, marker.Fill);
        context.ContextualColor = Brushes.Purple;
        context.CrystalEnabled = false;
        Sta.Drain();
        Assert.False(context.Resources.Contains("RibbonKit.Brushes.Tab.SelectedBackground"));
        Assert.Null(context.ContextualSelectionBrush);
        Assert.Same(Brushes.Purple, marker.Fill);
        context.CrystalEnabled = true;
        Sta.Drain();
        Assert.IsType<DrawingBrush>(marker.Fill);
    });

    [Fact]
    public void Crystal_input_styles_are_scoped_and_comparison_preserves_values() => Sta.Run(() =>
    {
        var root = new StackPanel();
        root.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        var crystal = new ResourceDictionary
        { Source = new Uri("/RibbonKit.Showcase;component/Themes/Crystal.Light.xaml", UriKind.Relative) };
        root.Resources.MergedDictionaries.Add(crystal);
        var text = new RibbonTextBox { Text = "Keep my title" };
        var combo = new RibbonComboBox { ItemsSource = new[] { "Segoe UI", "Georgia" }, SelectedIndex = 1 };
        var button = new RibbonButton { Header = "Ordinary button" };
        root.Children.Add(text);
        root.Children.Add(combo);
        root.Children.Add(button);
        root.Measure(new Size(500, 200));
        root.Arrange(new Rect(0, 0, 500, 200));
        root.UpdateLayout();

        var textChrome = (Border)text.Template.FindName("Chrome", text);
        var comboChrome = (Border)combo.Template.FindName("Chrome", combo);
        var buttonChrome = (Border)button.Template.FindName("Chrome", button);
        Assert.IsType<DrawingBrush>(textChrome.Background);
        Assert.IsType<DrawingBrush>(comboChrome.Background);
        Assert.Equal(new CornerRadius(4), textChrome.CornerRadius);
        Assert.Equal(new CornerRadius(4), comboChrome.CornerRadius);
        Assert.Equal(new CornerRadius(8), buttonChrome.CornerRadius);
        Assert.IsType<SolidColorBrush>(root.FindResource("RibbonKit.Brushes.Control.SurfaceBackground"));

        root.Resources.MergedDictionaries.Remove(crystal);
        root.UpdateLayout();
        Sta.Drain();
        Assert.Equal("Keep my title", text.Text);
        Assert.Equal("Georgia", combo.SelectedItem);
        Assert.IsType<SolidColorBrush>(((Border)text.Template.FindName("Chrome", text)).Background);
        Assert.IsType<SolidColorBrush>(((Border)combo.Template.FindName("Chrome", combo)).Background);
    });

    [Fact]
    public void Crystal_accent_switches_live_inputs_without_losing_values_or_original_palette() => Sta.Run(() =>
    {
        const string surface = "RibbonKit.Brushes.Ribbon.ContentBackground";
        var root = new StackPanel();
        root.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        var blue = CrystalPalette.Create(CrystalPalette.Blue);
        root.Resources.MergedDictionaries.Add(blue);
        var text = new RibbonTextBox { Text = "Keep this title" };
        var combo = new RibbonComboBox { ItemsSource = new[] { "Arial", "Georgia" }, SelectedIndex = 1 };
        root.Children.Add(text);
        root.Children.Add(combo);
        void LayoutInputs()
        {
            root.Measure(new Size(500, 200));
            root.Arrange(new Rect(0, 0, 500, 200));
            root.UpdateLayout();
            Sta.Drain();
        }
        LayoutInputs();
        var original = ((LinearGradientBrush)blue[surface]).GradientStops[2].Color;
        static Color InputEdge(Brush brush) => ((RadialGradientBrush)((GeometryDrawing)
            ((DrawingGroup)((DrawingBrush)brush).Drawing).Children[0]).Brush).GradientStops[2].Color;
        Color originalInput = InputEdge(((Border)text.Template.FindName("Chrome", text)).Background);
        foreach (string hex in new[] { "#287E78", "#8659A5", "#AC731C" })
        {
            var tinted = CrystalPalette.Create((Color)ColorConverter.ConvertFromString(hex));
            root.Resources.MergedDictionaries[1] = tinted;
            LayoutInputs();
            Assert.NotEqual(original, ((LinearGradientBrush)tinted[surface]).GradientStops[2].Color);
            Assert.Equal(original, ((LinearGradientBrush)blue[surface]).GradientStops[2].Color);
            var inputBackground = (DrawingBrush)text.FindResource("RibbonKit.Brushes.Control.SurfaceBackground");
            Assert.NotEqual(originalInput, InputEdge(inputBackground));
            Assert.Same(inputBackground, ((Border)text.Template.FindName("Chrome", text)).Background);
            Assert.IsType<DrawingBrush>(((Border)combo.Template.FindName("Chrome", combo)).Background);
            Assert.Equal("Keep this title", text.Text);
            Assert.Equal("Georgia", combo.SelectedItem);
            Assert.Equal(new CornerRadius(4), ((Border)text.Template.FindName("Chrome", text)).CornerRadius);
            // Pure-white reflection stays white in every hue variant.
            var rim = (DrawingGroup)((DrawingBrush)tinted["RibbonKit.Brushes.Control.HoverBorder"]).Drawing;
            var glint = (RadialGradientBrush)((GeometryDrawing)rim.Children[1]).Brush;
            Assert.Equal(Colors.White, glint.GradientStops[0].Color);
            Color foreground = ((SolidColorBrush)tinted["RibbonKit.Brushes.Tab.SelectedForeground"]).Color;
            foreach (var stop in ((RadialGradientBrush)tinted["RibbonKit.Brushes.Tab.SelectedBackground"]).GradientStops)
                Assert.True((Luminance(stop.Color) + 0.05) / (Luminance(foreground) + 0.05) >= 4.5);
        }
        root.Resources.MergedDictionaries.RemoveAt(1);
        LayoutInputs();
        Assert.IsType<SolidColorBrush>(((Border)text.Template.FindName("Chrome", text)).Background);
        root.Resources.MergedDictionaries.Add(CrystalPalette.Create(CrystalPalette.Blue));
        LayoutInputs();
        Assert.Equal(original, ((LinearGradientBrush)root.FindResource(surface)).GradientStops[2].Color);
        Assert.Equal("Keep this title", text.Text);
    });

    [Fact]
    public void Crystal_global_accent_preserves_contextual_tab_tint() => Sta.Run(() =>
    {
        var context = new CrystalContextualTab { Header = "Picture", IsContextual = true, ContextualColor = Brushes.Teal };
        var host = Host(context);
        host.Resources.Remove("RibbonKit.Brushes.Tab.SelectedUnderline");
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        host.Resources.MergedDictionaries.Add(CrystalPalette.Create(CrystalPalette.Blue));
        using var shown = new ShownHost(host);
        Layout(host);
        Color before = ((RadialGradientBrush)context.Resources["RibbonKit.Brushes.Tab.SelectedBackground"]).GradientStops[2].Color;
        host.Resources.MergedDictionaries[1] = CrystalPalette.Create(Colors.Purple);
        context.CrystalEnabled = true;
        Layout(host);
        Assert.Same(Brushes.Teal, context.ContextualBrush);
        Assert.Equal(before, ((RadialGradientBrush)context.Resources["RibbonKit.Brushes.Tab.SelectedBackground"]).GradientStops[2].Color);
        var marker = (Rectangle)host.Template.FindName("PART_TabMarker", host);
        Assert.Same(context.ContextualSelectionBrush, marker.Fill);
    });

    [Fact]
    public void Crystal_backstage_uses_tinted_materials_and_restores_baseline_without_losing_selection() => Sta.Run(() =>
    {
        var stage = new Backstage { Design = RibbonBackstageDesign.Modern };
        stage.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        stage.Resources.MergedDictionaries.Add(CrystalPalette.Create(CrystalPalette.Blue));
        var home = new BackstageTabItem { Header = "Home", Content = new TextBlock { Text = "Document" } };
        var appearance = new BackstageTabItem { Header = "Appearance", Content = new TextBlock { Text = "Tint" } };
        stage.Items.Add(home);
        stage.Items.Add(appearance);
        stage.SelectedItem = appearance;
        void LayoutStage()
        {
            stage.Measure(new Size(800, 500));
            stage.Arrange(new Rect(0, 0, 800, 500));
            stage.UpdateLayout();
            Sta.Drain();
        }
        LayoutStage();
        var nav = (Border)stage.Template.FindName("NavColumn", stage);
        var chrome = (Border)appearance.Template.FindName("Chrome", appearance);
        var original = Assert.IsType<LinearGradientBrush>(nav.Background).GradientStops[2].Color;
        Assert.Same(appearance, stage.SelectedItem);
        Assert.True(appearance.IsSelected);
        Assert.Equal(RibbonBackstageDesign.Modern, Backstage.GetDesign(appearance));
        Assert.IsType<DrawingBrush>(stage.FindResource("RibbonKit.Brushes.Backstage.Modern.ItemSelected"));
        var selection = Assert.IsType<DrawingBrush>(chrome.Background);
        Assert.IsType<DrawingBrush>(Assert.IsType<GeometryDrawing>(selection.Drawing).Brush);
        Assert.Equal(new CornerRadius(4), chrome.CornerRadius);
        stage.Resources.MergedDictionaries[1] = CrystalPalette.Create(Colors.Purple);
        LayoutStage();
        Assert.NotEqual(original, Assert.IsType<LinearGradientBrush>(nav.Background).GradientStops[2].Color);
        Assert.Same(appearance, stage.SelectedItem);
        Assert.Same(stage.FindResource("RibbonKit.Brushes.Backstage.Modern.ItemSelected"), chrome.Background);
        Assert.Same(stage.FindResource("RibbonKit.Brushes.Backstage.ContentBackground"), stage.Background);
        stage.Resources.MergedDictionaries.RemoveAt(1);
        LayoutStage();
        Assert.IsType<SolidColorBrush>(nav.Background);
        Assert.IsType<SolidColorBrush>(chrome.Background);
        Assert.Same(appearance, stage.SelectedItem);
    });

    [Fact]
    public void Crystal_floating_backstage_keeps_document_binding_navigation_and_comparison_when_detached() => Sta.Run(() =>
    {
        var stage = new Backstage { Design = RibbonBackstageDesign.Modern };
        var titleInput = new RibbonTextBox { Text = "A calmer workspace" };
        var title = new TextBlock();
        var home = new BackstageTabItem { Header = "Overview", Content = title };
        var appearance = new BackstageTabItem { Header = "Appearance", Content = new TextBlock { Text = "Tint" } };
        var about = new BackstageTabItem { Header = "About" };
        stage.Items.Add(home);
        stage.Items.Add(appearance);
        stage.Items.Add(about);
        stage.SelectedItem = home;
        var presentation = new CrystalBackstagePresentation(stage, new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) }, title, titleInput);
        var blue = CrystalPalette.Create(CrystalPalette.Blue);
        presentation.Apply(blue);
        void LayoutStage()
        {
            stage.Measure(new Size(680, 440));
            stage.Arrange(new Rect(0, 0, 680, 440));
            stage.UpdateLayout();
            Sta.Drain();
        }
        LayoutStage();
        Assert.Equal("A calmer workspace", title.Text);
        titleInput.Text = "Changed while File is open";
        Sta.Drain();
        Assert.Equal(titleInput.Text, title.Text);
        var first = home.TransformToAncestor(stage).Transform(new Point());
        var second = appearance.TransformToAncestor(stage).Transform(new Point());
        var third = about.TransformToAncestor(stage).Transform(new Point());
        Assert.Equal(first.Y, second.Y);
        Assert.Equal(first.Y, third.Y);
        Assert.True(first.X < second.X && second.X < third.X);
        Assert.True(third.X + about.ActualWidth <= stage.ActualWidth);
        Assert.Null(stage.Template.FindName("NavColumn", stage));
        int backRequests = 0;
        stage.BackRequested += (_, _) => backRequests++;
        ((Button)stage.Template.FindName("PART_BackButton", stage)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(1, backRequests);
        stage.SelectedItem = appearance;
        var purple = CrystalPalette.Create(Colors.Purple);
        presentation.Apply(purple);
        LayoutStage();
        Assert.Same(appearance, stage.SelectedItem);
        Assert.Same(purple["RibbonKit.Brushes.Window.Background"], stage.FindResource("RibbonKit.Brushes.Window.Background"));
        presentation.Apply(null);
        LayoutStage();
        Assert.NotNull(stage.Template.FindName("NavColumn", stage));
        Assert.Same(appearance, stage.SelectedItem);
        presentation.Apply(blue);
        LayoutStage();
        Assert.Null(stage.Template.FindName("NavColumn", stage));
        stage.SelectedItem = home;
        LayoutStage();
        Assert.Equal(titleInput.Text, title.Text);
    });

    private static double Luminance(Color color)
    {
        static double Linear(byte b)
        {
            double c = b / 255d;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
    }

    private static RibbonTabControl Host(params RibbonTab[] tabs)
    {
        var host = new RibbonTabControl
        {
            Width = 760, Height = 150, ItemsSource = tabs, SelectedIndex = 0,
            Template = (ControlTemplate)XamlReader.Parse("""
                <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:rk="clr-namespace:RibbonKit.Controls;assembly=RibbonKit" TargetType="{x:Type rk:RibbonTabControl}">
                    <Grid>
                        <TabPanel IsItemsHost="True" />
                        <Rectangle x:Name="PART_TabMarker" Height="3" HorizontalAlignment="Left" VerticalAlignment="Top">
                            <Rectangle.RenderTransform><TranslateTransform x:Name="PART_TabMarkerTranslate" /></Rectangle.RenderTransform>
                        </Rectangle>
                    </Grid>
                </ControlTemplate>
                """),
        };
        host.Resources["RibbonKit.Brushes.Tab.SelectedUnderline"] = Brushes.Blue;
        return host;
    }

    private static void Layout(RibbonTabControl host)
    {
        host.ApplyTemplate();
        host.Measure(new Size(760, 150));
        host.Arrange(new Rect(0, 0, 760, 150));
        host.UpdateLayout();
        Sta.Drain(DispatcherPriority.Loaded);
    }

    private sealed class ShownHost : IDisposable
    {
        private readonly Window _window;
        public ShownHost(RibbonTabControl host)
        {
            _window = new Window { Content = host, Width = 800, Height = 200,
                ShowInTaskbar = false, ShowActivated = false, Left = -10000, Top = -10000 };
            _window.Show();
        }
        public void Dispose() => _window.Close();
    }
}
