namespace RibbonKitDesignerConsumer;

public partial class BackstageInfoControl : UserControl
{
    public BackstageInfoControl() => InitializeComponent();
    internal TextBox NotesEditor => notesTextBox;
    private void ClearNotesButton_Click(object? sender, EventArgs e) => notesTextBox.Clear();
    private void ResetNotesButton_Click(object? sender, EventArgs e) => notesTextBox.Text = "Application page notes";
}
