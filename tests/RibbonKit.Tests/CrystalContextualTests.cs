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
