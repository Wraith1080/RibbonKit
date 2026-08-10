using System.IO;
using System.Linq;
using System.Xml.Linq;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public class RibbonWindowResizeTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void Live_resize_state_is_read_only_idempotent_and_restored() => Sta.Run(() =>
    {
        var window = new RibbonWindow();

        window.BeginLiveResize();
        window.BeginLiveResize();

        Assert.True(window.IsLiveResizing);
        Assert.True((bool)window.GetValue(RibbonWindow.IsLiveResizingProperty));
        Assert.True(RibbonWindow.IsLiveResizingProperty.ReadOnly);

        window.EndLiveResize();
        window.EndLiveResize();

        Assert.False(window.IsLiveResizing);
    });

    [Fact]
    public void Live_resize_triggers_suppress_both_wide_ribbon_shadows()
    {
        XDocument document = XDocument.Load(RibbonChromePath());
        XElement[] triggers = document
            .Descendants(Presentation + "DataTrigger")
            .Where(trigger =>
                ((string?)trigger.Attribute("Binding"))?.Contains("IsLiveResizing") == true
                && (string?)trigger.Attribute("Value") == "True")
            .ToArray();

        Assert.Equal(2, triggers.Length);
        Assert.Contains(triggers, trigger => SuppressesEffect(trigger, "QatBelowHost"));
        Assert.Contains(triggers, trigger => SuppressesEffect(trigger, "ContentHost"));
    }

    private static bool SuppressesEffect(XElement trigger, string targetName) =>
        trigger.Elements(Presentation + "Setter").Any(setter =>
            (string?)setter.Attribute("TargetName") == targetName
            && (string?)setter.Attribute("Property") == "Effect"
            && (string?)setter.Attribute("Value") == "{x:Null}");

    private static string RibbonChromePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(
            Assert.IsType<DirectoryInfo>(directory).FullName,
            "src",
            "RibbonKit",
            "Themes",
            "Controls.RibbonChrome.xaml");
    }
}
