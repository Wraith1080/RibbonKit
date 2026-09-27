using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

internal static class RibbonNoApplicationTabInsetChecks
{
    private static readonly string[] InsetKeys =
    {
        "RibbonKit.Metrics.TabStripMarginNoApplication",
        "RibbonKit.Metrics.TabStripMarginNoApplicationRtl",
        "RibbonKit.Metrics.QatTabRowMarginNoApplication",
        "RibbonKit.Metrics.QatTabRowMarginNoApplicationRtl",
    };

    internal static void Verify(Application application)
    {
        AssertOfficeTokenParity();
        ThemeManager.Apply(application, RibbonTheme.CrystalLight);
        var backstage = new Backstage();
        var ribbon = new Ribbon { Backstage = backstage };
        var home = new RibbonTab { Header = "Window" };
        home.Groups.Add(new RibbonGroup { Header = "Documents" });
        var preview = new RibbonTab { Header = "Print Preview", IsModal = true,
            Visibility = Visibility.Collapsed };
        preview.Groups.Add(new RibbonGroup { Header = "Print" });
        ribbon.Tabs.Add(home);
        ribbon.Tabs.Add(preview);
        ribbon.SelectedTab = home;
        var window = new RibbonWindow { Content = ribbon, Width = 700, Height = 350,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            Sta.Drain();
            var tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
            var body = Assert.IsType<Border>(tabs.Template.FindName("ContentHost", tabs));
            var panel = Assert.IsType<TabPanel>(tabs.Template.FindName("PART_TabItemsPanel", tabs));
            var qat = Assert.IsType<RibbonQuickAccessToolBar>(tabs.Template.FindName("QatTabRowHost", tabs));
            var file = Assert.IsType<ToggleButton>(tabs.Template.FindName("PART_ApplicationButton", tabs));
            var mergedIcon = Assert.IsType<Image>(tabs.Template.FindName("PART_MergedCaptionIcon", tabs));
            Assert.Equal(Visibility.Visible, file.Visibility);
            Assert.Equal(new Thickness(2, 4, 4, 0), panel.Margin);
            Assert.Equal(new Thickness(2, 4, 4, 0), qat.Margin);
            Assert.Equal(new CornerRadius(14), body.CornerRadius);
            double normalGap = HeaderGap(home, qat, tabs, rtl: false);

            ribbon.Backstage = null;
            Sta.Drain();
            window.UpdateLayout();
            Assert.Equal(Visibility.Collapsed, file.Visibility);
            Assert.Equal(new Thickness(2, 4, 4, 0), panel.Margin);
            Assert.Equal(new Thickness(24, 4, 4, 0), qat.Margin);
            Assert.Equal(new CornerRadius(14), body.CornerRadius);
            AssertQatPastLeadingCorner(qat, body, tabs, rtl: false);
            AssertPastLeadingCorner(home, body, tabs, rtl: false);
            Assert.InRange(Math.Abs(HeaderGap(home, qat, tabs, rtl: false) - normalGap), 0, 0.5);

            ribbon.ShowMergedCaption(null, "Document");
            Sta.Drain();
            window.UpdateLayout();
            Assert.Equal(Visibility.Visible, mergedIcon.Visibility);
            Assert.Equal(new Thickness(2, 4, 4, 0), panel.Margin);
            Assert.Equal(new Thickness(2, 4, 4, 0), qat.Margin);
            AssertPastLeadingCorner(home, body, tabs, rtl: false);
            ribbon.ClearMergedCaption();
            Sta.Drain();
            Assert.Equal(new Thickness(24, 4, 4, 0), qat.Margin);

            ribbon.FlowDirection = FlowDirection.RightToLeft;
            Sta.Drain();
            window.UpdateLayout();
            Assert.Equal(new Thickness(2, 4, 4, 0), panel.Margin);
            Assert.Equal(new Thickness(2, 4, 26, 0), qat.Margin);
            AssertQatPastLeadingCorner(qat, body, tabs, rtl: true);
            AssertPastLeadingCorner(home, body, tabs, rtl: true);

            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
            Sta.Drain();
            Assert.Equal(new CornerRadius(14, 14, 0, 0), body.CornerRadius);
            Assert.Equal(new Thickness(2, 4, 26, 0), panel.Margin);
            Assert.Equal(Visibility.Collapsed, qat.Visibility);
            CrystalQuickAccess.Apply(ribbon, true);
            Sta.Drain();
            Assert.Equal(new CornerRadius(14), body.CornerRadius);
            CrystalQuickAccess.Apply(ribbon, false);

            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
            ribbon.Backstage = backstage;
            ribbon.FlowDirection = FlowDirection.LeftToRight;
            Sta.Drain();
            Assert.Equal(new Thickness(2, 4, 4, 0), panel.Margin);
            Assert.Equal(new Thickness(2, 4, 4, 0), qat.Margin);
            Assert.True(ribbon.EnterModal(preview));
            Sta.Drain();
            window.UpdateLayout();
            Assert.Equal(Visibility.Collapsed, file.Visibility);
            Assert.Equal(new Thickness(2, 4, 4, 0), panel.Margin);
            Assert.Equal(new Thickness(24, 4, 4, 0), qat.Margin);
            Assert.Equal(new CornerRadius(14), body.CornerRadius);
            AssertQatPastLeadingCorner(qat, body, tabs, rtl: false);
            AssertPastLeadingCorner(preview, body, tabs, rtl: false);
            Assert.True(ribbon.ExitModal());
            Sta.Drain();
            Assert.Equal(new Thickness(2, 4, 4, 0), panel.Margin);
            Assert.Equal(new Thickness(2, 4, 4, 0), qat.Margin);

            ribbon.Backstage = null;
            ThemeManager.Apply(application, RibbonTheme.Office2007);
            Sta.Drain();
            window.UpdateLayout();
            Assert.Equal(new Thickness(2, 2, 4, 0), panel.Margin);
            Assert.Equal(new Thickness(6, 4, 4, 0), qat.Margin);
            Assert.Equal(new CornerRadius(3), body.CornerRadius);
            AssertQatPastLeadingCorner(qat, body, tabs, rtl: false);
            AssertPastLeadingCorner(home, body, tabs, rtl: false);

            ThemeManager.Apply(application, RibbonTheme.Office2024);
            Sta.Drain();
            Assert.Equal(new Thickness(2, 4, 4, 0), panel.Margin);
            Assert.Equal(new Thickness(2, 4, 4, 0), qat.Margin);
            Assert.Equal(new CornerRadius(8), body.CornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    private static void AssertPastLeadingCorner(RibbonTab tab, Border body,
        RibbonTabControl tabs, bool rtl)
    {
        Rect tabBounds = tab.TransformToVisual(tabs).TransformBounds(
            new Rect(0, 0, tab.ActualWidth, tab.ActualHeight));
        Rect bodyBounds = body.TransformToVisual(tabs).TransformBounds(
            new Rect(0, 0, body.ActualWidth, body.ActualHeight));
        if (rtl)
            Assert.True(tabBounds.Right <= bodyBounds.Right - body.CornerRadius.TopRight + 0.5,
                $"Tab right {tabBounds.Right} overlaps body corner at {bodyBounds.Right - body.CornerRadius.TopRight}.");
        else
            Assert.True(tabBounds.Left >= bodyBounds.Left + body.CornerRadius.TopLeft - 0.5,
                $"Tab left {tabBounds.Left} overlaps body corner at {bodyBounds.Left + body.CornerRadius.TopLeft}.");
    }

    private static void AssertQatPastLeadingCorner(RibbonQuickAccessToolBar qat,
        Border body, RibbonTabControl tabs, bool rtl)
    {
        Rect qatBounds = qat.TransformToVisual(tabs).TransformBounds(
            new Rect(0, 0, qat.ActualWidth, qat.ActualHeight));
        Rect bodyBounds = body.TransformToVisual(tabs).TransformBounds(
            new Rect(0, 0, body.ActualWidth, body.ActualHeight));
        if (rtl)
            Assert.True(qatBounds.Right <= bodyBounds.Right - body.CornerRadius.TopRight + 0.5);
        else
            Assert.True(qatBounds.Left >= bodyBounds.Left + body.CornerRadius.TopLeft - 0.5);
    }

    private static double HeaderGap(RibbonTab tab, RibbonQuickAccessToolBar qat,
        RibbonTabControl tabs, bool rtl)
    {
        Rect tabBounds = tab.TransformToVisual(tabs).TransformBounds(
            new Rect(0, 0, tab.ActualWidth, tab.ActualHeight));
        Rect qatBounds = qat.TransformToVisual(tabs).TransformBounds(
            new Rect(0, 0, qat.ActualWidth, qat.ActualHeight));
        return rtl ? qatBounds.Left - tabBounds.Right : tabBounds.Left - qatBounds.Right;
    }

    private static void AssertOfficeTokenParity()
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
                foreach (string key in InsetKeys)
                    Assert.IsType<Thickness>(resources[key]);
            }
        }
    }
}
