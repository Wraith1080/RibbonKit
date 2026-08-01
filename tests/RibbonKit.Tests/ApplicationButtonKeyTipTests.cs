using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Xml.Linq;
using RibbonKit.Controls;
using Xunit;
using RibbonKeyTipService = RibbonKit.Controls.KeyTipService;

namespace RibbonKit.Tests;

/// <summary>Guards the application button's root KeyTip registration and surface precedence.</summary>
public class ApplicationButtonKeyTipTests
{
    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void KeyTip_lookup_and_template_share_the_application_button_part_name() => Sta.Run(() =>
    {
        var document = XDocument.Load(RibbonChromePath());
        var templateButton = Assert.Single(
            document.Descendants(),
            element => (string?)element.Attribute(Xaml + "Name") == Ribbon.ApplicationButtonPartName);

        Assert.Equal("ToggleButton", templateButton.Name.LocalName);

        var root = new Grid();
        var legacyName = new ToggleButton { Name = "ApplicationButton" };
        var currentName = new ToggleButton { Name = Ribbon.ApplicationButtonPartName };
        root.Children.Add(legacyName);
        root.Children.Add(currentName);

        Assert.Same(currentName, RibbonKeyTipService.FindApplicationButton(root));
    });

    [Fact]
    public void Application_menu_wins_over_backstage_for_File_KeyTip_activation() => Sta.Run(() =>
    {
        var ribbon = new Ribbon { Backstage = new Backstage() };
        Assert.True(RibbonKeyTipService.ApplicationButtonOpensBackstage(ribbon));

        ribbon.ApplicationMenu = new RibbonApplicationMenu();

        Assert.False(RibbonKeyTipService.ApplicationButtonOpensBackstage(ribbon));
        Assert.True(RibbonKeyTipService.ApplicationButtonOpensApplicationMenu(ribbon));
    });

    [Fact]
    public void Application_menu_nav_KeyTip_targets_the_primary_button() => Sta.Run(() =>
    {
        var primaryFactory = new FrameworkElementFactory(typeof(Button), "PART_Primary");
        var item = new RibbonApplicationMenuItem
        {
            Header = "New",
            Template = new ControlTemplate(typeof(RibbonApplicationMenuItem))
            {
                VisualTree = primaryFactory,
            },
        };
        bool clicked = false;
        item.Click += (_, _) => clicked = true;

        item.ApplyTemplate();
        var primary = Assert.IsType<Button>(item.PrimaryPart);
        RibbonKeyTipService.InvokeControl(primary);
        Sta.Drain();

        Assert.True(clicked);
    });

    [Fact]
    public void Application_menu_split_nav_exposes_command_and_arrow_KeyTip_targets() => Sta.Run(() =>
    {
        RibbonApplicationMenuItem item = NavItem(hasPane: true, isSplit: true);

        var targets = RibbonKeyTipService.GetApplicationMenuNavTargets(item);

        Assert.Collection(
            targets,
            target =>
            {
                Assert.Same(item.PrimaryPart, target.Target);
                Assert.False(target.OpensPane);
            },
            target =>
            {
                Assert.Same(item.ArrowPart, target.Target);
                Assert.True(target.OpensPane);
            });
    });

    [Fact]
    public void Application_menu_split_arrow_claims_pane_without_running_primary_command() => Sta.Run(() =>
    {
        var menu = new RibbonApplicationMenu();
        RibbonApplicationMenuItem item = NavItem(hasPane: true, isSplit: true);
        bool primaryInvoked = false;
        item.Click += (_, _) => primaryInvoked = true;
        menu.Items.Add(item);

        item.ApplyTemplate();
        Assert.NotNull(item.ArrowPart);
        item.ArrowPart!.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

        Assert.Same(item, menu.ActiveItem);
        Assert.True(item.IsActive);
        Assert.False(primaryInvoked);
    });

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    public void Application_menu_non_split_nav_exposes_one_KeyTip_target(
        bool hasPane,
        bool isSplit,
        bool opensPane) => Sta.Run(() =>
    {
        RibbonApplicationMenuItem item = NavItem(hasPane, isSplit);

        var target = Assert.Single(RibbonKeyTipService.GetApplicationMenuNavTargets(item));

        Assert.Same(item.PrimaryPart, target.Target);
        Assert.Equal(opensPane, target.OpensPane);
    });

    private static RibbonApplicationMenuItem NavItem(bool hasPane, bool isSplit)
    {
        var root = new FrameworkElementFactory(typeof(Grid));
        root.AppendChild(new FrameworkElementFactory(typeof(Button), "PART_Primary"));
        root.AppendChild(new FrameworkElementFactory(typeof(Button), "PART_Arrow"));

        return new RibbonApplicationMenuItem
        {
            Header = "Print",
            Content = hasPane ? new Border() : null,
            IsSplit = isSplit,
            Template = new ControlTemplate(typeof(RibbonApplicationMenuItem))
            {
                VisualTree = root,
            },
        };
    }

    private static string RibbonChromePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, $"Could not find RibbonKit.sln above {AppContext.BaseDirectory}.");

        return Path.Combine(
            directory!.FullName,
            "src",
            "RibbonKit",
            "Themes",
            "Controls.RibbonChrome.xaml");
    }
}
