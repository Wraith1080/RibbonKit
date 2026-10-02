using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Theming;
using RibbonKit.Writer.Appearance;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Page;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Printing;
using RibbonKit.Writer.Tests.Document;
using Xunit;
using Xunit.Abstractions;

namespace RibbonKit.Writer.Tests.Appearance;

[Collection("Writer UI")]
public sealed class WriterThemeRenderingTests(ITestOutputHelper output)
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
    [Fact]
    public async Task CompleteDialogFamilyRethemesRendersAndReturnsAcrossAllPalettesAndDpiScales()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var snapshot = new WriterPreviewCloneService().CreateSnapshot(
                new FlowDocument(new Paragraph(new Run("Theme and print preview proof"))), DocumentPageSettings.A4());
            snapshot.Paginator.GetPage(0);
            var font = new WriterFontDialogResult(new FontFamily("Segoe UI"), 11, FontStyles.Normal,
                FontWeights.Normal, false, WriterStrikethroughStyle.None, WriterBaselineEffect.Normal, Colors.Black);
            Window[] dialogs = [new WriterFontDialog(font, new WriterFontCatalog(() => [new FontFamily("Segoe UI")])),
                new WriterColorDialog(Colors.Purple), new WriterParagraphDialog(), new WriterTableSizeDialog(),
                new WriterPictureInsertDialog(), new WriterHyperlinkDialog(), new WriterDateTimeDialog(),
                new WriterCustomMarginsDialog(DocumentPageSettings.A4()),
                new WriterPrintSetupDialog(snapshot, [new WriterPrinterChoice(null, "Test printer")], "Test printer")];
            var scopes = dialogs.Select(dialog => new WriterAppearanceScope(dialog)).ToArray();
            try
            {
                for (int i = 0; i < dialogs.Length; i++)
                {
                    scopes[i].Apply(new WriterAppearancePreferences());
                    dialogs[i].WindowStartupLocation = WindowStartupLocation.Manual;
                    dialogs[i].Left = -10000;
                    dialogs[i].Top = -10000;
                    dialogs[i].ShowInTaskbar = false;
                    dialogs[i].ShowActivated = false;
                    dialogs[i].Show();
                }
                await Task.Delay(100);
                foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
                foreach (bool dark in new[] { false, true })
                for (int i = 0; i < dialogs.Length; i++)
                {
                    var dialog = dialogs[i];
                    var preferences = new WriterAppearancePreferences { Theme = theme, DarkPalette = dark,
                        Accent = theme == RibbonTheme.CrystalLight ? "#FF7030A0" : null };
                    scopes[i].Apply(preferences);
                    foreach (double scale in new[] { 1, 1.25, 1.5, 1.75, 2, 1.25 })
                    {
                        VisualTreeHelper.SetRootDpi(dialog, new DpiScale(scale, scale));
                        dialog.FlowDirection = scale == 1.75 ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                        dialog.UpdateLayout();
                        dialog.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                        var root = Assert.IsAssignableFrom<FrameworkElement>(dialog.Content);
                        Assert.Equal(scale, VisualTreeHelper.GetDpi(root).DpiScaleX);
                        Assert.True(ReferenceEquals(dialog.FindResource("RibbonKit.Brushes.Control.SurfaceBackground"), dialog.Background),
                            $"{dialog.GetType().Name}: {theme}/{dark}/{scale}, dialog background does not follow shared surface");
                        Assert.True(root.ActualWidth > 0 && root.ActualHeight > 0);
                        Assert.True(root.ActualHeight + root.Margin.Top + root.Margin.Bottom + 1 >= root.DesiredSize.Height,
                            $"{dialog.GetType().Name}: {theme}/{dark}/{scale}, content height {root.ActualHeight}, desired {root.DesiredSize.Height}");
                        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth * scale),
                            (int)Math.Ceiling(root.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
                        var drawing = new DrawingVisual();
                        using (var context = drawing.RenderOpen())
                        {
                            context.DrawRectangle(dialog.Background, null, new Rect(root.RenderSize));
                            context.DrawRectangle(new VisualBrush(root), null, new Rect(root.RenderSize));
                        }
                        bitmap.Render(drawing);
                        Assert.True(bitmap.PixelWidth > 0);
                        if (theme == RibbonTheme.CrystalLight && scale is 1 or 2)
                        {
                            string output = Path.Combine(AppContext.BaseDirectory, "writer-theme-diagnostics");
                            Directory.CreateDirectory(output);
                            var encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmap));
                            using var file = File.Create(Path.Combine(output, $"{dialog.GetType().Name}-{(dark ? "dark" : "light")}-{scale * 100:0}.png"));
                            encoder.Save(file);
                        }
                    }
                    // Owner DPI changes above are synthetic. A native popup may retain the
                    // monitor DPI; record its effective visual scale separately, without asserting parity.
                    if (dialog is WriterDateTimeDialog)
                    {
                        var date = (DatePicker)dialog.FindName("DateBox");
                        date.IsDropDownOpen = true;
                        dialog.UpdateLayout();
                        var popup = (Popup)date.Template.FindName("PART_Popup", date);
                        Assert.True(VisualTreeHelper.GetDpi((Visual)popup.Child).DpiScaleX > 0);
                        var source = Assert.IsType<HwndSource>(PresentationSource.FromVisual((Visual)popup.Child));
                        output.WriteLine($"{theme}/{dark}: owner synthetic {VisualTreeHelper.GetDpi(dialog).DpiScaleX}, " +
                            $"calendar visual {VisualTreeHelper.GetDpi((Visual)popup.Child).DpiScaleX}, native HWND {GetDpiForWindow(source.Handle)} DPI");
                        date.IsDropDownOpen = false;
                    }
                }
                for (int i = 0; i < dialogs.Length; i++)
                {
                    scopes[i].Apply(new WriterAppearancePreferences());
                    Assert.Same(dialogs[i].FindResource("RibbonKit.Brushes.Control.SurfaceBackground"), dialogs[i].Background);
                }
            }
            finally
            {
                foreach (var scope in scopes) scope.Dispose();
                foreach (var dialog in dialogs) dialog.Close();
            }
        }, TimeSpan.FromMinutes(3));
    }
}
