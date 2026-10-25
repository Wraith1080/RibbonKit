using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using RibbonKit.Controls;
using RibbonKit.Theming;
using RibbonKit.Writer.Appearance;
using RibbonKit.Writer.Tests.Document;
using RibbonKit.Writer.Services.RecentFiles;
using Xunit;

namespace RibbonKit.Writer.Tests.Shell;

public sealed partial class MainWindowIntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BackstageRecentDocumentIsReachableAndInvokableThroughItsParentPeer(bool crystal)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture(withRecentFile: true);
            try
            {
                fixture.Window.ShowActivated = false;
                fixture.Show();
                var stage = Assert.IsType<Backstage>(fixture.Ribbon.Backstage);
                fixture.Window.ApplyAppearance(new WriterAppearancePreferences
                    { Theme = crystal ? RibbonTheme.CrystalLight : RibbonTheme.Office2024 });
                stage.Design = crystal ? RibbonBackstageDesign.CrystalSidebar : RibbonBackstageDesign.Modern;
                stage.Items.OfType<BackstageTabItem>().Single(item => Equals(item.Header, "Home")).IsSelected = true;
                fixture.Ribbon.IsBackstageOpen = true;
                await PumpAsync();
                var list = FindVisualDescendants<ItemsControl>(stage).Single(item =>
                    AutomationProperties.GetAutomationId(item) == "RecentList");
                var button = FindVisualDescendants<Button>(list).First(item => item.CommandParameter is RecentFileEntry);
                var entry = Assert.IsType<RecentFileEntry>(button.CommandParameter);
                var root = UIElementAutomationPeer.CreatePeerForElement(stage)!;
                var regions = root.GetChildren()!;
                Assert.Equal(new[] { AutomationControlType.Group, AutomationControlType.Pane },
                    regions.Select(item => item.GetAutomationControlType()));
                var page = regions[1]; Assert.Equal("Home", page.GetName());
                Assert.DoesNotContain(BackstageAutomationTree(regions[0]), item => item.GetAutomationId() == entry.Path);
                var peer = Assert.Single(BackstageAutomationTree(page),
                    item => item.GetAutomationId() == entry.Path);
                Assert.Equal(entry.FileName, peer.GetName());
                Assert.True(peer.IsEnabled());
                Assert.IsAssignableFrom<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke)).Invoke();
                await WaitForShellIdleAsync(fixture.Shell);
                await PumpAsync();
                Assert.Equal(entry.Path, fixture.Shell.CurrentDocument.Path);
                Assert.False(fixture.Ribbon.IsBackstageOpen);
            }
            finally { fixture.Shell.CurrentDocument.MarkClean(); }
        }, TimeSpan.FromSeconds(30));
    }

    private static IEnumerable<AutomationPeer> BackstageAutomationTree(AutomationPeer peer)
    {
        yield return peer;
        foreach (var child in peer.GetChildren() ?? [])
            foreach (var descendant in BackstageAutomationTree(child)) yield return descendant;
    }
}
