using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using RibbonKit.Controls;
using RibbonKit.Theming;
using RibbonKit.Writer.Appearance;
using RibbonKit.Writer.Services.Documents;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Shell;

public sealed partial class MainWindowIntegrationTests
{
    [Theory]
    [InlineData(RibbonDensity.Compact, false)]
    [InlineData(RibbonDensity.Compact, true)]
    [InlineData(RibbonDensity.Touch, false)]
    [InlineData(RibbonDensity.Touch, true)]
    public async Task WriterHeightResizeKeepsRibbonAndPaperOriginStable(RibbonDensity density, bool ruler)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var store = new WriterSettingsStore(new WriterSettingsPaths(directory.File("appearance.json"), directory.File("ribbon.json")));
            store.SaveAppearance(new WriterAppearancePreferences { Theme = RibbonTheme.CrystalLight,
                Density = density, ShowRuler = ruler, CapturedPopupBackdrop = true });
            using var fixture = new WindowFixture(settings: store);
            fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
            fixture.Ribbon.SelectedTab = (RibbonTab)fixture.Window.FindName("ViewTab");
            fixture.Show();
            await PumpAsync();
            fixture.Editor.AppendText("Height resizing retains Writer's selected text and document.");
            fixture.Editor.SelectAll();
            ClickHostButton((RibbonButton)fixture.Window.FindName("ZoomInButton"));
            ClickHostButton((RibbonButton)fixture.Window.FindName("ZoomInButton"));
            await PumpAsync();
            var document = fixture.Editor.Document;
            string selected = fixture.Editor.Selection.Text;
            bool undo = fixture.Editor.CanUndo;
            bool dirty = fixture.Shell.CurrentDocument.IsDirty;
            double startHeight = fixture.Window.Height;
            double scale = VisualTreeHelper.GetDpi(fixture.Window).DpiScaleY;
            var paper = (Panel)fixture.Window.FindName("DocumentPresentationHost");
            Point ribbonOrigin = fixture.Ribbon.TranslatePoint(new Point(), fixture.Window);
            Point paperOrigin = paper.TranslatePoint(new Point(), fixture.Window);
            int transitions = 0;
            fixture.Ribbon.LayoutTransitionPreparing += (_, _) => transitions++;
            var handle = new WindowInteropHelper(fixture.Window).Handle;
            ResizeWriterNativeMessage(handle, 0x0231, IntPtr.Zero, IntPtr.Zero);
            Assert.IsType<BitmapCache>(fixture.Ribbon.CacheMode);
            try
            {
                for (int step = 1; step <= 64; step++)
                {
                    int pixel = step <= 32 ? step : 64 - step;
                    fixture.Window.Height = startHeight + pixel / scale;
                    fixture.Window.UpdateLayout();
                    await Dispatcher.Yield(DispatcherPriority.Loaded);
                    Assert.Equal(ribbonOrigin, fixture.Ribbon.TranslatePoint(new Point(), fixture.Window));
                    Assert.Equal(paperOrigin, paper.TranslatePoint(new Point(), fixture.Window));
                    Assert.False(DocumentMotion(fixture).IsPresenting);
                }
                Assert.Equal(0, transitions);
                Assert.Same(document, fixture.Editor.Document);
                Assert.Equal(selected, fixture.Editor.Selection.Text);
                Assert.Equal(undo, fixture.Editor.CanUndo);
                Assert.Equal(dirty, fixture.Shell.CurrentDocument.IsDirty);
                Assert.Equal("120%", ((TextBlock)fixture.Window.FindName("ZoomText")).Text);
            }
            finally
            {
                ResizeWriterNativeMessage(handle, 0x0232, IntPtr.Zero, IntPtr.Zero);
                fixture.Dialogs.Decisions.Enqueue(UnsavedChangesDecision.Discard);
                Assert.Null(fixture.Ribbon.CacheMode);
            }
        }, TimeSpan.FromSeconds(30));
    }

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern IntPtr ResizeWriterNativeMessage(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam);
}
