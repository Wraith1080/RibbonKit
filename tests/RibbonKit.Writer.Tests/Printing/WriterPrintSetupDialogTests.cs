using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RibbonKit.Controls;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Printing;
using RibbonKit.Writer.Tests.Document;
using RibbonKit.Writer.Tests.Preview;
using Xunit;

namespace RibbonKit.Writer.Tests.Printing;

[Collection(WriterPreviewTestCollection.Name)]
public sealed class WriterPrintSetupDialogTests
{
    [Fact]
    public void DialogUsesWriterThemeAndHostsTheExactPreviewWithoutUnsupportedClaim()
    {
        StaTestHelper.Run(() =>
        {
            using var snapshot = new WriterPreviewCloneService().CreateSnapshot(
                new FlowDocument(new Paragraph(new Run("Writer-owned print preview"))),
                DocumentPageSettings.A4());
            var dialog = new WriterPrintSetupDialog(snapshot,
                new[] { new WriterPrinterChoice(null, "Test printer") }, "Test printer")
            {
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                Opacity = 0.01
            };

            dialog.Show();
            try
            {
                var preview = Assert.IsType<WriterDocumentPreviewView>(dialog.FindName("Preview"));
                Assert.Same(snapshot, preview.Snapshot);
                Assert.Same(snapshot.Paginator, preview.PrimaryPageView.DocumentPaginator);
                Assert.Contains("A4", Assert.IsType<TextBlock>(
                    dialog.FindName("PageSummaryText")).Text);
                Assert.Same(dialog.TryFindResource("RibbonKit.Brushes.Control.SurfaceBackground"),
                    dialog.Background);
                var printer = Assert.IsType<RibbonComboBox>(dialog.FindName("PrinterBox"));
                var printerHost = Assert.IsType<Grid>(dialog.FindName("PrinterFieldHost"));
                var printerBounds = printer.TransformToAncestor(printerHost)
                    .TransformBounds(new Rect(printer.RenderSize));
                Assert.True(printerBounds.Right <= printerHost.ActualWidth + 0.5,
                    $"Printer field right {printerBounds.Right:0.##}, host right {printerHost.ActualWidth:0.##}.");
                var settingsLayout = Assert.IsType<Grid>(dialog.FindName("PrintSettingsLayout"));
                var actionButtons = Assert.IsType<StackPanel>(dialog.FindName("PrintActionButtons"));
                var actionBounds = actionButtons.TransformToAncestor(settingsLayout)
                    .TransformBounds(new Rect(actionButtons.RenderSize));
                Assert.InRange(Math.Abs(
                    actionBounds.Left + actionBounds.Width / 2 - settingsLayout.ActualWidth / 2), 0, 0.5);
                Assert.Same(dialog.TryFindResource("OptionsDialogActionButtonStyle"),
                    Assert.IsType<Button>(dialog.FindName("CancelButton")).Style);
                Assert.Same(dialog.TryFindResource("OptionsDialogPrimaryButtonStyle"),
                    Assert.IsType<Button>(dialog.FindName("PrintButton")).Style);
                Assert.Same(dialog.TryFindResource("OptionsDialogActionButtonStyle"),
                    Assert.IsType<Button>(dialog.FindName("PreviousButton")).Style);
                Assert.Same(dialog.TryFindResource("OptionsDialogActionButtonStyle"),
                    Assert.IsType<Button>(dialog.FindName("NextButton")).Style);
                var text = string.Join(" ", FindText(dialog));
                Assert.DoesNotContain("doesn't support print preview", text,
                    StringComparison.OrdinalIgnoreCase);
                Assert.False(Assert.IsType<Button>(dialog.FindName("PrintButton")).IsEnabled);
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    private static IEnumerable<string> FindText(DependencyObject root)
    {
        if (root is TextBlock textBlock)
            yield return textBlock.Text;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            foreach (var text in FindText(VisualTreeHelper.GetChild(root, index)))
                yield return text;
        }
    }
}
