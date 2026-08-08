using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Guards sticky pane ownership in the application-menu navigation column.</summary>
public class ApplicationMenuHoverTests
{
    [Fact]
    public void Leaving_a_pane_row_across_the_separator_gap_keeps_its_pane_active() => Sta.Run(() =>
    {
        var menu = new RibbonApplicationMenu();
        var saveAs = PaneItem("Save As", "Save a copy");
        menu.Items.Add(saveAs);

        menu.NotifyItemHoverChanged(saveAs, isOver: true);
        menu.NotifyItemHoverChanged(saveAs, isOver: false);

        // The previous implementation scheduled a Background-priority reset here. Pumping the
        // dispatcher models a deliberately slow pointer crossing the narrow nav-to-pane gap.
        Sta.Drain();

        Assert.Same(saveAs, menu.ActiveItem);
        Assert.Equal("Save a copy", menu.ActivePaneContent);
        Assert.True(saveAs.IsActive);
        Assert.True(menu.HasActivePane);
    });

    [Fact]
    public void Only_entering_another_main_nav_row_changes_the_active_pane() => Sta.Run(() =>
    {
        var menu = new RibbonApplicationMenu();
        var saveAs = PaneItem("Save As", "Save a copy");
        var print = PaneItem("Print", "Print choices");
        var save = new RibbonApplicationMenuItem { Header = "Save" };
        menu.Items.Add(saveAs);
        menu.Items.Add(print);
        menu.Items.Add(save);

        menu.NotifyItemHoverChanged(saveAs, isOver: true);
        menu.NotifyItemHoverChanged(print, isOver: true);

        Assert.Same(print, menu.ActiveItem);
        Assert.False(saveAs.IsActive);
        Assert.True(print.IsActive);

        // A pane-less main command intentionally restores the default page.
        menu.NotifyItemHoverChanged(save, isOver: true);

        Assert.Null(menu.ActiveItem);
        Assert.Null(menu.ActivePaneContent);
        Assert.False(print.IsActive);
        Assert.False(menu.HasActivePane);
    });

    [Fact]
    public void Design_preview_index_is_inert_at_runtime() => Sta.Run(() =>
    {
        var menu = new RibbonApplicationMenu();
        menu.Items.Add(PaneItem("Save As", "Save a copy"));

        menu.DesignPreviewActiveIndex = 0;

        Assert.Null(menu.ActiveItem);
        Assert.False(menu.HasActivePane);
    });

    [Fact]
    public void Design_file_surface_is_inert_at_runtime() => Sta.Run(() =>
    {
        var ribbon = new Ribbon
        {
            ApplicationMenu = new RibbonApplicationMenu(),
            DesignPreviewFileSurface = 2,
        };

        Assert.False(ribbon.IsBackstageOpen);
        Assert.False(ribbon.IsApplicationMenuOpen);
    });

    private static RibbonApplicationMenuItem PaneItem(string header, object content) =>
        new() { Header = header, Content = content };
}
