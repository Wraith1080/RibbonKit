using System.Reflection;
using System.Windows;
using System.Windows.Controls;
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
    public async Task CopiedBoldFormatsSelectedTextLikeTheOriginal(bool customGroup)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture();
            try
            {
                fixture.Show();
                fixture.Window.Width = 1800;
                var original = Assert.IsType<RibbonToggleButton>(fixture.Window.FindName("BoldButton"));
                RibbonToggleButton copy;
                if (customGroup)
                {
                    var group = new RibbonGroup { Header = "Favorites", CanResize = false };
                    Ribbon.SetIsCustom(group, true);
                    // The same factory used by Customize Ribbon; avoid changing the user's profile.
                    copy = Assert.IsType<RibbonToggleButton>(typeof(Ribbon).GetMethod("CreateCommandProxy",
                        BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fixture.Ribbon,
                            new object[] { original, RibbonControlSize.Medium, false }));
                    group.Items.Add(copy);
                    ((RibbonTab)fixture.Window.FindName("HomeTab")).Groups.Add(group);
                }
                else
                {
                    Assert.True(fixture.Ribbon.AddToQuickAccess(original));
                    copy = Assert.Single(fixture.Ribbon.QuickAccessItems.OfType<RibbonToggleButton>(),
                        item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), original));
                }
                fixture.Editor.Document.Blocks.Clear();
                fixture.Editor.Document.Blocks.Add(new Paragraph(new Run("random selected text")));
                fixture.Editor.SelectAll();
                fixture.Window.EditingController.RefreshState();
                await PumpAsync();

                Toggle(original); await PumpAsync();
                Assert.Equal(FontWeights.Bold, fixture.Editor.Selection.GetPropertyValue(TextElement.FontWeightProperty));
                Toggle(original); await PumpAsync();
                Assert.Equal(FontWeights.Normal, fixture.Editor.Selection.GetPropertyValue(TextElement.FontWeightProperty));

                Assert.True(copy.Focus());
                Toggle(copy); await PumpAsync();
                Assert.Equal(FontWeights.Bold, fixture.Editor.Selection.GetPropertyValue(TextElement.FontWeightProperty));
                Toggle(copy); await PumpAsync();
                Assert.Equal(FontWeights.Normal, fixture.Editor.Selection.GetPropertyValue(TextElement.FontWeightProperty));
                Assert.Equal("random selected text", fixture.Editor.Selection.Text.TrimEnd('\r', '\n'));
            }
            finally { fixture.Shell.CurrentDocument.MarkClean(); }
        }, TimeSpan.FromSeconds(30));
    }
}
