using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace RibbonKit.Writer.Editing;

/// <summary>Accessible, transactional editor for deterministic date/time insertion.</summary>
public partial class WriterDateTimeDialog : Window
{
    private bool _initialized;

    /// <summary>Creates a date/time dialog initialized to the current local value.</summary>
    public WriterDateTimeDialog(DateTimeOffset? openingValue = null)
    {
        InitializeComponent();
        var value = openingValue ?? DateTimeOffset.Now;
        DateBox.SelectedDate = value.Date;
        TimeBox.Text = value.ToString("t", CultureInfo.CurrentCulture);
        FormatBox.SelectedIndex = 0;
        _initialized = true;
        ValidateInput();
    }

    /// <summary>Gets the selected value after Insert.</summary>
    public DateTimeOffset? ResultValue { get; private set; }

    /// <summary>Gets the selected .NET format after Insert.</summary>
    public string? ResultFormat { get; private set; }

    private void OnDateChanged(object sender, SelectionChangedEventArgs e) => ValidateAfterInitialization();

    private void OnTimeChanged(object sender, TextChangedEventArgs e) => ValidateAfterInitialization();

    private void OnFormatChanged(object sender, SelectionChangedEventArgs e) => ValidateAfterInitialization();

    private void OnInsert(object sender, RoutedEventArgs e)
    {
        ValidateInput();
        if (!InsertButton.IsEnabled || DateBox.SelectedDate is not { } date ||
            !TryGetTime(out var time))
            return;
        ResultValue = new DateTimeOffset(date.Date + time);
        ResultFormat = GetFormat();
        DialogResult = true;
    }

    private void ValidateInput()
    {
        var valid = DateBox.SelectedDate is not null
            && TryGetTime(out _)
            && !string.IsNullOrWhiteSpace(GetFormat());
        InsertButton.IsEnabled = valid;
        ValidationText.Text = valid
            ? "The formatted value will be inserted as editable text."
            : "Enter a valid date, time and format.";
    }

    private void ValidateAfterInitialization()
    {
        if (_initialized)
            ValidateInput();
    }

    private string GetFormat() => FormatBox.SelectedItem is ComboBoxItem { Tag: string format }
        ? format
        : "g";

    private bool TryGetTime(out TimeSpan time)
    {
        var text = TimeBox.Text.Trim();
        if (DateTime.TryParse(text, CultureInfo.CurrentCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault, out var parsed)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault, out parsed))
        {
            time = parsed.TimeOfDay;
            return true;
        }

        return TimeSpan.TryParse(text, CultureInfo.CurrentCulture, out time)
            && time >= TimeSpan.Zero
            && time < TimeSpan.FromDays(1);
    }
}
