namespace RibbonKitDesignerConsumer;

// An ordinary designer-editable UserControl: no RibbonKit presenter interface.
public partial class PopupOptionsControl : UserControl
{
    public PopupOptionsControl() => InitializeComponent();
    internal TextBox ValueEditor => valueEditor;
    internal ComboBox ModeEditor => modeEditor;
    internal Button ApplyButton => applyButton;
    internal Label Heading => heading;
    internal event EventHandler? Applied;
    internal event EventHandler? Dismissed;
    private void ApplyClicked(object? sender, EventArgs e)
    {
        if (ValidateChildren()) Applied?.Invoke(this, EventArgs.Empty);
    }
    private void CancelClicked(object? sender, EventArgs e) => Dismissed?.Invoke(this, EventArgs.Empty);
    private void ValueValidating(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = string.IsNullOrWhiteSpace(valueEditor.Text);
        validationLabel.Text = e.Cancel ? "Enter a value before applying." : "";
    }
}
