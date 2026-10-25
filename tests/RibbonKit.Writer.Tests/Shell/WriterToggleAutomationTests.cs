using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using RibbonKit.Controls;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Shell;

public sealed partial class MainWindowIntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AutomationBoldFormatsSelectedTextThroughOriginalAndQat(bool qat)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture();
            try
            {
                fixture.Window.ShowActivated = false;
                fixture.Show();
                fixture.Window.Width = 1800;
                var original = Assert.IsType<RibbonToggleButton>(fixture.Window.FindName("BoldButton"));
                Assert.True(fixture.Ribbon.AddToQuickAccess(original));
                var copy = Assert.Single(fixture.Ribbon.QuickAccessItems.OfType<RibbonToggleButton>(),
                    item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), original));
                AutomationProperties.SetAutomationId(copy, "WriterAutomationBoldCopy");
                fixture.Editor.Document.Blocks.Clear();
                fixture.Editor.Document.Blocks.Add(new Paragraph(new Run("automation selected text")));
                fixture.Editor.SelectAll();
                fixture.Window.EditingController.RefreshState();
                await PumpAsync();

                AutomationPeer peer;
                if (qat)
                {
                    var toolbar = FindVisualDescendants<RibbonQuickAccessToolBar>(fixture.Window)
                        .Single(item => item.IsVisible && item.Items.Contains(copy));
                    peer = Assert.Single(UIElementAutomationPeer.CreatePeerForElement(toolbar)!.GetChildren()!,
                        item => item.GetAutomationId() == "WriterAutomationBoldCopy");
                }
                else peer = UIElementAutomationPeer.CreatePeerForElement(original)!;
                var provider = Assert.IsAssignableFrom<IToggleProvider>(peer.GetPattern(PatternInterface.Toggle));
                int clicks = 0;
                original.Click += (_, _) => clicks++;
                for (int i = 0; i < 2; i++)
                {
                    provider.Toggle();
                    await PumpAsync();
                    bool expected = i == 0;
                    Assert.Equal(expected ? FontWeights.Bold : FontWeights.Normal,
                        fixture.Editor.Selection.GetPropertyValue(TextElement.FontWeightProperty));
                    Assert.Equal(expected, original.IsChecked);
                    Assert.Equal(expected, copy.IsChecked);
                    Assert.Equal(expected ? ToggleState.On : ToggleState.Off, provider.ToggleState);
                    Assert.Equal(i + 1, clicks);
                }
                Assert.Equal("automation selected text", fixture.Editor.Selection.Text.TrimEnd('\r', '\n'));
            }
            finally { fixture.Shell.CurrentDocument.MarkClean(); }
        }, TimeSpan.FromSeconds(30));
    }
}
