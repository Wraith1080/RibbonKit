using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace RibbonKit.Writer.Editing;

/// <summary>Accessible, app-owned path picker for portable Writer images.</summary>
public partial class WriterPictureInsertDialog : Window
{
    /// <summary>Creates a picture picker with an optional opening path.</summary>
    public WriterPictureInsertDialog(string? openingPath = null)
    {
        InitializeComponent();
        PathBox.Text = openingPath ?? string.Empty;
        ValidatePath();
    }

    /// <summary>Gets the path accepted by the user, or null after cancellation.</summary>
    public string? SelectedPath { get; private set; }

    private void OnPathChanged(object sender, TextChangedEventArgs e) => ValidatePath();

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Pictures (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) == true)
            PathBox.Text = dialog.FileName;
    }

    private void OnInsert(object sender, RoutedEventArgs e)
    {
        ValidatePath();
        if (!InsertButton.IsEnabled)
            return;
        SelectedPath = PathBox.Text.Trim();
        DialogResult = true;
    }

    private void ValidatePath()
    {
        var path = PathBox.Text.Trim();
        var extension = Path.GetExtension(path);
        var validExtension = extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
        var valid = path.Length > 0 && validExtension && File.Exists(path);
        InsertButton.IsEnabled = valid;
        ValidationText.Text = valid
            ? "The image will be embedded in the document."
            : path.Length == 0
                ? "Enter a picture path or use Browse."
                : !validExtension
                    ? "Choose a PNG, JPEG, GIF or BMP file."
                    : "The selected picture file cannot be read.";
    }
}
