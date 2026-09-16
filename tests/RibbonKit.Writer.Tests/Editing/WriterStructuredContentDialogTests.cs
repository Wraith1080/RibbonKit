using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

[Collection("Writer UI")]
public sealed class WriterStructuredContentDialogTests
{
    [Fact]
    public void InsertDialogsResolveThemeChangesIncludingCalendarPopupAndFormatItems()
    {
        StaTestHelper.Run(() =>
        {
            var dialogs = new Window[]
            {
                new WriterTableSizeDialog(), new WriterPictureInsertDialog(),
                new WriterHyperlinkDialog(), new WriterDateTimeDialog()
            };
            try
            {
                foreach (var dialog in dialogs)
                {
                    var originalResources = dialog.Resources;
                    dialog.Resources = new ResourceDictionary();
                    dialog.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative)
                    });
                    dialog.Resources.MergedDictionaries.Add(originalResources);
                    dialog.WindowStartupLocation = WindowStartupLocation.Manual;
                    dialog.Left = -10000;
                    dialog.Top = -10000;
                    dialog.Show();
                }
                foreach (var theme in new[] { "2007", "2010", "2013", "2019", "2024" })
                foreach (var palette in new[] { "", ".Dark" })
                foreach (var dialog in dialogs)
                {
                    var tokens = new ResourceDictionary();
                    tokens.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri($"/RibbonKit;component/Themes/Tokens.Office{theme}.xaml", UriKind.Relative)
                    });
                    if (palette.Length > 0) tokens.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri($"/RibbonKit;component/Themes/Tokens.Office{theme}{palette}.xaml",
                            UriKind.Relative)
                    });
                    dialog.Resources.MergedDictionaries[0] = tokens;
                    dialog.UpdateLayout();
                    dialog.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
                    Assert.Same(dialog.FindResource("RibbonKit.Brushes.Control.SurfaceBackground"), dialog.Background);
                    Assert.Same(dialog.FindResource("RibbonKit.Brushes.Text.Primary"), dialog.Foreground);
                    var insert = Assert.IsType<Button>(dialog.FindName("InsertButton"));
                    Assert.NotNull(insert.Template);
                    Assert.Same(dialog.FindResource("RibbonKit.Brushes.Dialog.PrimaryBackground"), insert.Background);
                    foreach (var field in new[] { "RowsBox", "ColumnsBox", "PathBox", "AddressBox", "DisplayTextBox", "TimeBox" })
                    {
                        if (dialog.FindName(field) is not TextBox textBox) continue;
                        Assert.IsType<RibbonTextBox>(textBox);
                        Assert.NotNull(textBox.Template);
                        Assert.Same(dialog.FindResource("RibbonKit.Brushes.Text.Primary"), textBox.Foreground);
                    }
                    if (dialog is not WriterDateTimeDialog) continue;
                    var format = Assert.IsType<RibbonComboBox>(dialog.FindName("FormatBox"));
                    format.IsDropDownOpen = true;
                    dialog.UpdateLayout();
                    Assert.NotNull(format.Template);
                    var formatItem = Assert.IsAssignableFrom<ComboBoxItem>(format.ItemContainerGenerator.ContainerFromIndex(0));
                    Assert.Same(dialog.FindResource("RibbonKit.Brushes.Text.Primary"), formatItem.Foreground);
                    format.IsDropDownOpen = false;
                    var date = Assert.IsType<DatePicker>(dialog.FindName("DateBox"));
                    var dateText = Assert.IsType<DatePickerTextBox>(date.Template.FindName("PART_TextBox", date));
                    Assert.Same(dialog.FindResource("RibbonKit.Brushes.Text.Primary"), dateText.CaretBrush);
                    date.IsDropDownOpen = true;
                    dialog.UpdateLayout();
                    var popup = Assert.IsType<Popup>(date.Template.FindName("PART_Popup", date));
                    var calendar = Assert.IsType<Calendar>(popup.Child);
                    calendar.UpdateLayout();
                    Assert.NotNull(calendar.CalendarDayButtonStyle);
                    var item = Assert.IsType<CalendarItem>(calendar.Template.FindName("PART_CalendarItem", calendar));
                    Assert.Same(dialog.FindResource("RibbonKit.Brushes.Control.SurfaceBackground"),
                        Assert.IsType<Border>(VisualTreeHelper.GetChild(item, 0)).Background);
                    var month = Assert.IsType<Grid>(item.Template.FindName("PART_MonthView", item));
                    Assert.Equal(42, month.Children.OfType<CalendarDayButton>().Count());
                    var dayTitles = month.Children.OfType<FrameworkElement>()
                        .Where(child => child is not CalendarDayButton).ToArray();
                    Assert.Equal(7, dayTitles.Length);
                    Assert.All(dayTitles, title => Assert.True(title.ActualHeight > 0));
                    var header = Assert.IsType<Button>(item.Template.FindName("PART_HeaderButton", item));
                    header.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    calendar.UpdateLayout();
                    Assert.Equal(CalendarMode.Year, calendar.DisplayMode);
                    var year = Assert.IsType<Grid>(item.Template.FindName("PART_YearView", item));
                    Assert.Equal(Visibility.Visible, year.Visibility);
                    Assert.Equal(12, year.Children.OfType<CalendarButton>().Count());
                    calendar.DisplayMode = CalendarMode.Month;
                    var next = Assert.IsType<Button>(item.Template.FindName("PART_NextButton", item));
                    var openingMonth = calendar.DisplayDate;
                    next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.Equal(openingMonth.AddMonths(1).Month, calendar.DisplayDate.Month);
                    calendar.SelectedDate = new DateTime(2026, 9, 16);
                    Assert.Equal(calendar.SelectedDate, date.SelectedDate);
                    date.IsDropDownOpen = false;
                }
            }
            finally
            {
                foreach (var dialog in dialogs) dialog.Close();
            }
        });
    }

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
                    Assert.IsAssignableFrom<ComboBox>(dialogs[2].FindName("FormatBox")).VerticalAlignment);
                Assert.False(Assert.IsAssignableFrom<TextBox>(dialogs[1].FindName("DisplayTextBox"))
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
                var rows = Assert.IsAssignableFrom<TextBox>(dialog.FindName("RowsBox"));
                var columns = Assert.IsAssignableFrom<TextBox>(dialog.FindName("ColumnsBox"));
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
                var pathBox = Assert.IsAssignableFrom<TextBox>(dialog.FindName("PathBox"));
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
                var address = Assert.IsAssignableFrom<TextBox>(dialog.FindName("AddressBox"));
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
                var time = Assert.IsAssignableFrom<TextBox>(dialog.FindName("TimeBox"));
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
