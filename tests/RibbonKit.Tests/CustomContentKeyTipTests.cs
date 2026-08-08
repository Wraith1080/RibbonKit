using System.Windows;
using System.Windows.Controls;
using RibbonKit.Controls;
using Xunit;
using RibbonKeyTipService = RibbonKit.Controls.KeyTipService;

namespace RibbonKit.Tests;

public class CustomContentKeyTipTests
{
    [Fact]
    public void Backstage_collects_only_visible_explicit_page_content_targets() => Sta.Run(() =>
    {
        var root = new Grid();
        var content = new StackPanel();
        root.Children.Add(content);

        var export = new Button { Content = "Export" };
        KeyTip.SetKeys(export, "EX");
        content.Children.Add(export);

        content.Children.Add(new Button { Content = "Untagged" });

        var hiddenBranch = new StackPanel { Visibility = Visibility.Collapsed };
        var hidden = new Button { Content = "Hidden" };
        KeyTip.SetKeys(hidden, "HI");
        hiddenBranch.Children.Add(hidden);
        content.Children.Add(hiddenBranch);

        var navigationItem = new BackstageTabItem { Header = "Info" };
        KeyTip.SetKeys(navigationItem, "I");
        root.Children.Add(navigationItem);

        UIElement target = Assert.Single(RibbonKeyTipService.GetBackstageContentKeyTipTargets(root));
        Assert.Same(export, target);
    });

    [Fact]
    public void Application_menu_collects_built_in_and_explicit_custom_content_targets() => Sta.Run(() =>
    {
        var root = new Grid();
        var content = new StackPanel();
        root.Children.Add(content);

        var paneItem = new RibbonApplicationMenuPaneItem { Content = "Recent document" };
        var footerButton = new RibbonApplicationMenuButton { Content = "Options" };
        var customButton = new Button { Content = "Manage locations" };
        KeyTip.SetKeys(customButton, "ML");

        content.Children.Add(paneItem);
        content.Children.Add(footerButton);
        content.Children.Add(customButton);
        content.Children.Add(new Button { Content = "Untagged" });

        var navigationItem = new RibbonApplicationMenuItem { Header = "Open" };
        KeyTip.SetKeys(navigationItem, "O");
        root.Children.Add(navigationItem);

        var hiddenBranch = new StackPanel { Visibility = Visibility.Collapsed };
        var hidden = new Button { Content = "Hidden" };
        KeyTip.SetKeys(hidden, "HI");
        hiddenBranch.Children.Add(hidden);
        root.Children.Add(hiddenBranch);

        IReadOnlyList<UIElement> targets =
            RibbonKeyTipService.GetApplicationMenuContentKeyTipTargets(root);

        Assert.Equal(3, targets.Count);
        Assert.Contains(paneItem, targets);
        Assert.Contains(footerButton, targets);
        Assert.Contains(customButton, targets);
        Assert.DoesNotContain(navigationItem, targets);
        Assert.DoesNotContain(hidden, targets);
    });

    [Fact]
    public void Explicit_standard_button_uses_the_existing_invoke_path() => Sta.Run(() =>
    {
        var button = new Button { Content = "Export" };
        bool invoked = false;
        button.Click += (_, _) => invoked = true;

        RibbonKeyTipService.InvokeControl(button);
        Sta.Drain();

        Assert.True(invoked);
    });

    [Fact]
    public void Ribbon_toggle_KeyTip_runs_checked_state_and_Click_side_effects_together() => Sta.Run(() =>
    {
        var toggle = new RibbonToggleButton();
        bool? checkedStateSeenByClick = null;
        int clicks = 0;
        toggle.Click += (_, _) =>
        {
            clicks++;
            checkedStateSeenByClick = toggle.IsChecked;
        };

        RibbonKeyTipService.InvokeControl(toggle);

        Assert.True(toggle.IsChecked);
        Assert.True(checkedStateSeenByClick);
        Assert.Equal(1, clicks);

        RibbonKeyTipService.InvokeControl(toggle);

        Assert.False(toggle.IsChecked);
        Assert.False(checkedStateSeenByClick);
        Assert.Equal(2, clicks);
    });

    [Fact]
    public void Backstage_action_item_invokes_instead_of_becoming_selected() => Sta.Run(() =>
    {
        var action = new BackstageTabItem { Header = "Options", IsButton = true };
        bool invoked = false;
        action.Click += (_, _) => invoked = true;

        RibbonKeyTipService.InvokeControl(action);

        Assert.True(invoked);
        Assert.False(action.IsSelected);
    });

    [Fact]
    public void Disabled_target_is_not_invoked_even_when_an_ancestor_disables_it() => Sta.Run(() =>
    {
        var parent = new StackPanel { IsEnabled = false };
        var button = new Button { Content = "Export" };
        bool invoked = false;
        button.Click += (_, _) => invoked = true;
        parent.Children.Add(button);

        Assert.False(button.IsEnabled);
        Assert.False(RibbonKeyTipService.CanInvoke(button));

        RibbonKeyTipService.InvokeControl(button);
        Sta.Drain();

        Assert.False(invoked);
    });
}
