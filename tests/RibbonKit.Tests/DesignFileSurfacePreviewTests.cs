using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public sealed class DesignFileSurfacePreviewTests
{
    [Fact]
    public void File_preview_cycles_preserve_both_authored_surfaces_and_their_pages() => Sta.Run(() =>
    {
        var menu = new RibbonApplicationMenu { DefaultContent = new TextBlock { Text = "Recent" } };
        var pane = new RibbonApplicationMenuItem { Header = "Print", Content = new TextBlock { Text = "Print choices" } };
        menu.Items.Add(pane);
        var backstage = new Backstage();
        backstage.Items.Add(new BackstageTabItem { Header = "Info", Content = new TextBlock { Text = "Info page" } });
        backstage.Items.Add(new BackstageTabItem { Header = "Open", Content = new TextBlock { Text = "Open page" } });
        var ribbon = new Ribbon { ApplicationMenu = menu, Backstage = backstage };
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        foreach (var control in new DependencyObject[] { ribbon, menu, backstage })
            DesignerProperties.SetIsInDesignMode(control, true);
        var host = new Grid { Children = { ribbon } };
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new System.Uri("/RibbonKit;component/Themes/Office2024.xaml", System.UriKind.Relative) });

        // Match the context path (page before surface) and editor path (surface
        // before page), including a return to the closed/default state.
        for (int cycle = 0; cycle < 2; cycle++)
        {
            foreach (int surface in new[] { 0, 1, 2, 1, 0, 2, 0 })
            {
                if (cycle == 0) SelectPage(surface);
                ribbon.DesignPreviewFileSurface = surface;
                Layout();
                if (cycle == 1) SelectPage(surface);
                Layout();
                Assert.Equal(surface != 0, ribbon.IsBackstageOpen);
                Assert.Equal(surface == 2, ribbon.IsApplicationMenuOpen);
                var stageHost = Assert.IsType<Border>(ribbon.Template.FindName("PART_DesignBackstageHost", ribbon));
                Assert.Equal(surface == 1, ReferenceEquals(stageHost.Child, backstage));
                Assert.Same(menu, ribbon.ApplicationMenu);
                Assert.Same(backstage, ribbon.Backstage);
                if (surface == 1) Assert.Equal(1, backstage.SelectedIndex);
                if (surface == 2) Assert.Same(pane, menu.ActiveItem);
            }
        }

        void SelectPage(int surface)
        {
            if (surface == 1) backstage.SelectedIndex = 1;
            if (surface == 2) menu.DesignPreviewActiveIndex = 0;
        }

        void Layout()
        {
            host.Measure(new Size(900, 600));
            host.Arrange(new Rect(0, 0, 900, 600));
            host.UpdateLayout();
            Sta.Drain();
        }
    });
}
