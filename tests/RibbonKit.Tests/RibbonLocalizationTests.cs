using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using RibbonKit.Controls;
using RibbonKit.Localization;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>First Phase 6 localization/RTL slice: RibbonKit-owned context menus.</summary>
public class RibbonLocalizationTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    [Fact]
    public void Embedded_resources_cover_every_declared_ribbon_string()
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            foreach (RibbonString key in Enum.GetValues<RibbonString>())
            {
                string value = RibbonLocalization.GetString(key);
                Assert.False(string.IsNullOrWhiteSpace(value));
                Assert.NotEqual(key.ToString(), value);
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Provider_can_override_one_string_and_fall_back_for_the_rest()
    {
        IRibbonLocalizationProvider? previous = RibbonLocalization.Provider;
        try
        {
            RibbonLocalization.Provider = new SingleStringProvider(
                RibbonString.CustomizeRibbon,
                "Personalize Ribbon…");

            Assert.Equal(
                "Personalize Ribbon…",
                RibbonLocalization.GetString(RibbonString.CustomizeRibbon));
            Assert.Equal(
                "Collapse the Ribbon",
                RibbonLocalization.GetString(RibbonString.CollapseRibbon));
        }
        finally
        {
            RibbonLocalization.Provider = previous;
        }
    }

    [Fact]
    public void Cached_qat_menu_refreshes_localized_headers_each_time_it_opens() => Sta.Run(() =>
    {
        IRibbonLocalizationProvider? previous = RibbonLocalization.Provider;
        try
        {
            RibbonLocalization.Provider = new PrefixProvider("first");
            var ribbon = new Ribbon();
            ContextMenu menu = Invoke<ContextMenu>(ribbon, "EnsureQatContextMenu");

            Assert.Equal("first:RemoveFromQuickAccessToolbar", Header(menu, 0));
            Assert.Equal("first:ShowQuickAccessToolbarInTitleBar", Header(menu, 2));
            Assert.Equal("first:CustomizeQuickAccessToolbar", Header(menu, 6));

            RibbonLocalization.Provider = new PrefixProvider("second");
            menu.RaiseEvent(new RoutedEventArgs(ContextMenu.OpenedEvent, menu));

            Assert.Equal("second:RemoveFromQuickAccessToolbar", Header(menu, 0));
            Assert.Equal("second:ShowQuickAccessToolbarAboveRibbon", Header(menu, 3));
            Assert.Equal("second:ShowQuickAccessToolbarBelowRibbon", Header(menu, 4));
            Assert.Equal("second:CustomizeQuickAccessToolbar", Header(menu, 6));
        }
        finally
        {
            RibbonLocalization.Provider = previous;
        }
    });

    [Fact]
    public void Qat_menu_copies_rtl_flow_from_its_disconnected_popup_host() => Sta.Run(() =>
    {
        var host = new Border { FlowDirection = FlowDirection.RightToLeft };
        var menu = new ContextMenu { FlowDirection = FlowDirection.LeftToRight };

        MethodInfo prepare = Assert.IsAssignableFrom<MethodInfo>(typeof(Ribbon).GetMethod(
            "PrepareQatContextMenu",
            BindingFlags.Static | BindingFlags.NonPublic));
        prepare.Invoke(null, [host, menu]);

        Assert.Equal(FlowDirection.RightToLeft, menu.FlowDirection);
    });

    [Fact]
    public void Rtl_submenus_open_to_the_left()
    {
        var document = XDocument.Load(Path.Combine(
            RepositoryRoot(),
            "src",
            "RibbonKit",
            "Themes",
            "Menus.xaml"));

        XElement trigger = Assert.Single(
            document.Descendants(Presentation + "Trigger"),
            element =>
                (string?)element.Attribute("Property") == "FlowDirection"
                && (string?)element.Attribute("Value") == "RightToLeft");

        XElement[] setters = trigger.Elements(Presentation + "Setter").ToArray();
        Assert.Contains(setters, setter =>
            (string?)setter.Attribute("TargetName") == "PART_Popup"
            && (string?)setter.Attribute("Property") == "Placement"
            && (string?)setter.Attribute("Value") == "Left");
    }

    [Fact]
    public void Ribbon_context_menus_no_longer_embed_their_english_headers()
    {
        string source = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "RibbonKit",
            "Controls",
            "Ribbon.cs"));

        Assert.DoesNotContain("Header = \"Add to Quick Access Toolbar\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Header = \"Customize Quick Access Toolbar…\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Header = \"Show Quick Access Toolbar Above the Ribbon\"", source, StringComparison.Ordinal);
    }

    private static string? Header(ContextMenu menu, int index) =>
        Assert.IsType<MenuItem>(menu.Items[index]).Header?.ToString();

    private static T Invoke<T>(object target, string methodName)
    {
        MethodInfo method = Assert.IsAssignableFrom<MethodInfo>(target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic));
        return Assert.IsType<T>(method.Invoke(target, null));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        return Assert.IsType<DirectoryInfo>(directory).FullName;
    }

    private sealed class SingleStringProvider(RibbonString key, string value) : IRibbonLocalizationProvider
    {
        public string? GetString(RibbonString requested, CultureInfo culture) =>
            requested == key ? value : null;
    }

    private sealed class PrefixProvider(string prefix) : IRibbonLocalizationProvider
    {
        public string GetString(RibbonString key, CultureInfo culture) => $"{prefix}:{key}";
    }
}
