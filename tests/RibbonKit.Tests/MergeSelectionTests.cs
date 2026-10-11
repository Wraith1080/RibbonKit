using System.Windows;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class MergeSelectionTests
{
    [Theory]
    [InlineData("Home")]
    [InlineData("View")]
    [InlineData("Ribbon Lab")]
    public void Showcase_deactivation_keeps_the_selected_host_tab(string header) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(showcaseResources: true), RibbonTheme.Office2013);
        var window = new RibbonKit.Showcase.MainWindow();
        try
        {
            typeof(RibbonKit.Showcase.MainWindow).GetField("_restoringAppearance",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(window, true);
            window.Left = -10000; window.Top = -10000;
            window.ShowActivated = false; window.ShowInTaskbar = false;
            window.Show();
            Sta.Drain();
            window.MergeToggle.IsChecked = true;
            Sta.Drain();
            Assert.Same(window.ChartToolsSource.Tabs[0], window.MainRibbon.SelectedTab);
            Assert.True(window.MainRibbon.IsMerged(window.ChartToolsSource));
            RibbonTab view = window.MainRibbon.Tabs.Single(tab => tab.Header?.ToString() == header);
            window.MainRibbon.SelectedTab = view;
            Sta.Drain();
            window.MergeToggle.IsChecked = false;
            Sta.Drain();
            Assert.Same(view, window.MainRibbon.SelectedTab);
            Assert.False(window.MainRibbon.IsMerged(window.ChartToolsSource));

            // Only a removed tool tab needs the ordinary visible-tab fallback.
            window.MergeToggle.IsChecked = true;
            Sta.Drain();
            Assert.Same(window.ChartToolsSource.Tabs[0], window.MainRibbon.SelectedTab);
            window.MergeToggle.IsChecked = false;
            Sta.Drain();
            Assert.Same(window.MainRibbon.Tabs[0], window.MainRibbon.SelectedTab);

            // A cancelled activation must not run the queued tool-tab jump later.
            window.MainRibbon.SelectedTab = view;
            window.MergeToggle.IsChecked = true;
            window.MergeToggle.IsChecked = false;
            Sta.Drain();
            Assert.Same(view, window.MainRibbon.SelectedTab);
        }
        finally { window.Close(); Sta.ResetApplication(); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unmerging_an_unselected_source_preserves_the_realized_host_tab(bool contributeGroup) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2013);
        var ribbon = new Ribbon();
        var home = new RibbonTab { Header = "Home" };
        Ribbon.SetCommandId(home, "HOME");
        var view = new RibbonTab { Header = "View" };
        ribbon.Tabs.Add(home);
        ribbon.Tabs.Add(view);
        var source = new RibbonMergeSource();
        source.Tabs.Add(new RibbonTab { Header = "Chart Design" });
        source.Tabs.Add(new RibbonTab { Header = "Chart Format" });
        if (contributeGroup)
        {
            source.Groups.Add(new RibbonGroupContribution
            {
                TargetTabId = "HOME",
                Group = new RibbonGroup { Header = "Chart Data" },
            });
        }

        var window = new RibbonWindow
        {
            Content = ribbon, Width = 800, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false,
        };
        try
        {
            window.Show();
            Sta.Drain();
            ribbon.Merge(source);
            ribbon.SelectedTab = view;
            Sta.Drain();
            ribbon.Unmerge(source);
            Sta.Drain();
            Assert.Same(view, ribbon.SelectedTab);
            Assert.Equal(1, ribbon.SelectedIndex);
        }
        finally { window.Close(); }
    });
}
