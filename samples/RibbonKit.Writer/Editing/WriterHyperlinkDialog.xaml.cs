using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace RibbonKit.Writer.Editing;

/// <summary>Accessible, transactional editor for a Writer hyperlink address and label.</summary>
public partial class WriterHyperlinkDialog : Window
{
    /// <summary>Creates a hyperlink dialog with optional existing values.</summary>
    public WriterHyperlinkDialog(string? address = null, string? displayText = null)
    {
        InitializeComponent();
        AddressBox.Text = address ?? string.Empty;
        DisplayTextBox.Text = displayText ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(address))
        {
            Title = "Edit Hyperlink";
            InsertButton.Content = "Apply";
            AutomationProperties.SetName(this, "Edit Hyperlink");
            AutomationProperties.SetName(InsertButton, "Apply hyperlink changes");
        }
        ValidateInput();
    }

    /// <summary>Gets the validated address after Insert.</summary>
    public string? Address { get; private set; }

    /// <summary>Gets the optional label after Insert.</summary>
    public string? DisplayText { get; private set; }

    private void OnInputChanged(object sender, TextChangedEventArgs e) => ValidateInput();

    private void OnInsert(object sender, RoutedEventArgs e)
    {
        ValidateInput();
        if (!InsertButton.IsEnabled)
            return;
        Address = AddressBox.Text.Trim();
        DisplayText = string.IsNullOrWhiteSpace(DisplayTextBox.Text)
            ? null
            : DisplayTextBox.Text;
        DialogResult = true;
    }

    private void ValidateInput()
    {
        var address = AddressBox.Text.Trim();
        var display = DisplayTextBox.Text;
        var validDisplay = display.Length <= 2048 && !display.Any(char.IsControl);
        var valid = WriterHyperlinkService.TryParseUri(address, out _)
            && validDisplay;
        InsertButton.IsEnabled = valid;
        ValidationText.Text = valid
            ? "The link will remain inert until the user explicitly activates it."
            : !WriterHyperlinkService.TryParseUri(address, out _)
                ? "Enter a safe absolute HTTP, HTTPS or mailto address."
                : "Display text must be 2,048 characters or fewer and contain no controls.";
    }
}
