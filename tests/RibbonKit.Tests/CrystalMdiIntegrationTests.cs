using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class CrystalMdiIntegrationTests
{
    private static readonly string[] MdiKeys =
    {
        "RibbonKit.Brushes.MdiChild.ActiveCaptionBackground",
        "RibbonKit.Brushes.MdiChild.ActiveCaptionForeground",
        "RibbonKit.Brushes.MdiChild.InactiveCaptionBackground",
        "RibbonKit.Brushes.MdiChild.InactiveCaptionForeground",
        "RibbonKit.Brushes.MdiChild.ActiveBorder",
        "RibbonKit.Brushes.MdiChild.InactiveBorder",
        "RibbonKit.Brushes.MdiClient.Background",
        "RibbonKit.Brushes.MdiChild.CaptionButtonHoverBackground",
        "RibbonKit.Brushes.MdiChild.CaptionButtonPressedBackground",
        "RibbonKit.Metrics.MdiChild.CornerRadius",
        "RibbonKit.Metrics.MdiChild.CaptionCornerRadius",
        "RibbonKit.Metrics.MdiChild.CloseButtonCornerRadius",
    };

    [Fact]
    public void Crystal_mdi_tokens_reuse_keys_present_in_every_office_variant() => Sta.Run(() =>
    {
        foreach (string generation in new[] { "2007", "2010", "2013", "2019", "2024" })
        {
            var resources = new ResourceDictionary();
            resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri($"/RibbonKit;component/Themes/Tokens.Office{generation}.xaml", UriKind.Relative) });
            foreach (bool dark in new[] { false, true })
            {
                if (dark)
                    resources.MergedDictionaries.Add(new ResourceDictionary
                    { Source = new Uri($"/RibbonKit;component/Themes/Tokens.Office{generation}.Dark.xaml", UriKind.Relative) });
                foreach (string key in MdiKeys)
                    Assert.NotNull(resources[key]);
            }
        }
    });

    [Fact]
    public void Crystal_mdi_demo_styles_realized_children_and_restores_office() => Sta.Run(() =>
    {
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit.Showcase;component/Icons.xaml", UriKind.Relative) });
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var demo = new MdiDemo
        {
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false,
        };
        try
        {
            demo.Show();
            Sta.Drain();
            MdiDocument document = Assert.IsType<MdiDocument>(demo.Mdi.Items[0]);
            var editor = Assert.IsType<TextBox>(document.Content);
            var tab = Assert.IsType<CrystalContextualTab>(Assert.Single(document.MergeSource!.Tabs));
            var child = Assert.IsType<MdiChild>(demo.Mdi.ItemContainerGenerator.ContainerFromItem(document));
            demo.Mdi.ActivateDocument(document);
            Sta.Drain();
            demo.UpdateLayout();
            var caption = Assert.IsType<Border>(child.Template.FindName("Caption", child));
            Assert.False(tab.CrystalEnabled);
            Assert.IsType<SolidColorBrush>(caption.Background);

            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            demo.ApplyCrystal(true, Colors.Purple);
            Sta.Drain();
            demo.UpdateLayout();
            Assert.True(tab.CrystalEnabled);
            Assert.IsType<DrawingBrush>(tab.ContextualSelectionBrush);
            Assert.IsType<RadialGradientBrush>(caption.Background);
            Assert.Same(child.FindResource("RibbonKit.Brushes.MdiChild.ActiveCaptionBackground"), caption.Background);
            Assert.Same(demo.Mdi.FindResource("RibbonKit.Brushes.MdiClient.Background"), demo.Mdi.Background);
            Assert.Same(editor.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), editor.Background);
            Assert.Same(editor.FindResource("RibbonKit.Brushes.Text.Primary"), editor.Foreground);
            var purpleBorder = Assert.IsType<SolidColorBrush>(child.FindResource(
                "RibbonKit.Brushes.MdiChild.ActiveBorder")).Color;

            demo.ApplyCrystal(true, Colors.SeaGreen);
            Sta.Drain();
            var greenBorder = Assert.IsType<SolidColorBrush>(child.FindResource(
                "RibbonKit.Brushes.MdiChild.ActiveBorder")).Color;
            Assert.NotEqual(purpleBorder, greenBorder);

            ThemeManager.Apply(application, RibbonTheme.Office2024);
            demo.ApplyCrystal(false);
            Sta.Drain();
            Assert.False(tab.CrystalEnabled);
            Assert.Null(tab.ContextualSelectionBrush);
            Assert.IsType<SolidColorBrush>(caption.Background);
            Assert.Equal(Colors.White, Assert.IsType<SolidColorBrush>(demo.Background).Color);
            Assert.Null(demo.TryFindResource("Crystal.Brushes.FrostedFrame"));
            Assert.Equal(DependencyProperty.UnsetValue,
                editor.ReadLocalValue(Control.BackgroundProperty));
            Assert.Equal(DependencyProperty.UnsetValue,
                editor.ReadLocalValue(Control.ForegroundProperty));

            RibbonNoApplicationTabInsetChecks.Verify(application);
        }
        finally
        {
            demo.Close();
            application.Shutdown();
        }
    });
}
