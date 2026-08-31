using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace RibbonKit.Writer.Editing;

/// <summary>Accessible, fixed-size entry dialog for table dimensions outside the quick gallery.</summary>
public partial class WriterTableSizeDialog : Window
{
    private bool _initialized;

    /// <summary>Creates a table-size dialog with an optional opening size.</summary>
    public WriterTableSizeDialog(int rows = 4, int columns = 4)
    {
        InitializeComponent();
        RowsBox.Text = rows.ToString(CultureInfo.CurrentCulture);
        ColumnsBox.Text = columns.ToString(CultureInfo.CurrentCulture);
        _initialized = true;
        ValidateInput();
    }

    /// <summary>Gets the validated row count after Insert.</summary>
    public int Rows { get; private set; }

    /// <summary>Gets the validated column count after Insert.</summary>
    public int Columns { get; private set; }

    private void OnInputChanged(object sender, TextChangedEventArgs e)
    {
        if (_initialized)
            ValidateInput();
    }

    private void OnInsert(object sender, RoutedEventArgs e)
    {
        if (!TryReadDimensions(out var rows, out var columns))
            return;
        Rows = rows;
        Columns = columns;
        DialogResult = true;
    }

    private void ValidateInput()
    {
        var valid = TryReadDimensions(out _, out _);
        InsertButton.IsEnabled = valid;
        ValidationText.Text = valid
            ? "The table will be inserted at the current paragraph boundary."
            : "Enter whole numbers from 1 through 8.";
    }

    private bool TryReadDimensions(out int rows, out int columns)
    {
        rows = 0;
        columns = 0;
        return int.TryParse(RowsBox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture,
                out rows)
            && rows is >= 1 and <= WriterTableService.MaximumStructuralCount
            && int.TryParse(ColumnsBox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture,
                out columns)
            && columns is >= 1 and <= WriterTableService.MaximumStructuralCount;
    }
}
