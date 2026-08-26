using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

[Collection("Writer UI")]
public sealed class WriterStructuredContentDialogTests
{
    [Fact]
    public void StructuredContentDialogsUseCompactNonResizableLayouts()
    {
        StaTestHelper.Run(() =>
        {
            var dialogs = new Window[]
            {
                new WriterPictureInsertDialog(),
                new WriterHyperlinkDialog(),
                new WriterDateTimeDialog(new DateTimeOffset(2026, 8, 26, 10, 30, 0,
                    TimeSpan.FromHours(7))),
                new WriterTableSizeDialog()
            };
            try
            {
                foreach (var dialog in dialogs)
                {
                    Assert.Equal(ResizeMode.NoResize, dialog.ResizeMode);
                    Assert.Equal(SizeToContent.Height, dialog.SizeToContent);
                }
                Assert.Equal(VerticalAlignment.Center,
                    Assert.IsType<ComboBox>(dialogs[2].FindName("FormatBox")).VerticalAlignment);
                Assert.False(Assert.IsType<TextBox>(dialogs[1].FindName("DisplayTextBox"))
                    .AcceptsReturn);
                Assert.Equal(VerticalAlignment.Center,
                    Assert.IsType<Button>(dialogs[3].FindName("InsertButton")).VerticalAlignment);
            }
            finally
            {
                foreach (var dialog in dialogs)
                    dialog.Close();
            }
        });
    }

    [Fact]
    public void TableSizeDialogValidatesTheSupportedManualRange()
    {
        StaTestHelper.Run(() =>
        {
            var dialog = new WriterTableSizeDialog();
            try
            {
                var rows = Assert.IsType<TextBox>(dialog.FindName("RowsBox"));
                var columns = Assert.IsType<TextBox>(dialog.FindName("ColumnsBox"));
                var insert = Assert.IsType<Button>(dialog.FindName("InsertButton"));
                Assert.Equal("InsertTableSizeDialog", AutomationProperties.GetAutomationId(dialog));
                Assert.True(insert.IsEnabled);

                rows.Text = "9";
                Assert.False(insert.IsEnabled);
                rows.Text = "3";
                columns.Text = "0";
                Assert.False(insert.IsEnabled);
                columns.Text = "8";
                Assert.True(insert.IsEnabled);
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    [Fact]
    public void PictureDialogValidatesSupportedExistingPathsAndExposesStableAutomation()
    {
        StaTestHelper.Run(() =>
        {
            var path = Path.Combine(Path.GetTempPath(), $"writer-picture-{Guid.NewGuid():N}.png");
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            var dialog = new WriterPictureInsertDialog();
            try
            {
                var pathBox = Assert.IsType<TextBox>(dialog.FindName("PathBox"));
                var insert = Assert.IsType<Button>(dialog.FindName("InsertButton"));
                Assert.Equal("InsertPictureDialog", AutomationProperties.GetAutomationId(dialog));
                Assert.Equal("Insert Picture", AutomationProperties.GetName(dialog));
                Assert.False(insert.IsEnabled);

                pathBox.Text = path;
                Assert.True(insert.IsEnabled);
                pathBox.Text = Path.ChangeExtension(path, ".txt");
                Assert.False(insert.IsEnabled);
            }
            finally
            {
                dialog.Close();
                File.Delete(path);
            }
        });
    }

    [Fact]
    public void HyperlinkDialogRejectsUnsafeUrisAndProjectsEditModeAccessibly()
    {
        StaTestHelper.Run(() =>
        {
            var dialog = new WriterHyperlinkDialog("https://example.com", "Example");
            try
            {
                var address = Assert.IsType<TextBox>(dialog.FindName("AddressBox"));
                var insert = Assert.IsType<Button>(dialog.FindName("InsertButton"));
                Assert.Equal("Edit Hyperlink", dialog.Title);
                Assert.Equal("Edit Hyperlink", AutomationProperties.GetName(dialog));
                Assert.Equal("Apply", insert.Content);
                Assert.Equal("Apply hyperlink changes", AutomationProperties.GetName(insert));
                Assert.True(insert.IsEnabled);

                address.Text = "file:///C:/unsafe.exe";
                Assert.False(insert.IsEnabled);
                address.Text = "mailto:writer@example.com";
                Assert.True(insert.IsEnabled);
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    [Fact]
    public void DateTimeDialogRejectsOutOfDayTimesAndKeepsStableAutomation()
    {
        StaTestHelper.Run(() =>
        {
            var dialog = new WriterDateTimeDialog(new DateTimeOffset(2026, 8, 26, 10, 30, 0,
                TimeSpan.FromHours(7)));
            try
            {
                var time = Assert.IsType<TextBox>(dialog.FindName("TimeBox"));
                var insert = Assert.IsType<Button>(dialog.FindName("InsertButton"));
                Assert.Equal("InsertDateTimeDialog", AutomationProperties.GetAutomationId(dialog));
                Assert.Equal("Insert Date and Time", AutomationProperties.GetName(dialog));
                Assert.True(insert.IsEnabled);

                time.Text = "25:00";
                Assert.False(insert.IsEnabled);
                time.Text = "23:59";
                Assert.True(insert.IsEnabled);
            }
            finally
            {
                dialog.Close();
            }
        });
    }
}
