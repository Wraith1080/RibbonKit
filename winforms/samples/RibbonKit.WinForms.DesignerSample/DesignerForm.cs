using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm : RibbonForm
{
    public DesignerForm()
    {
        InitializeComponent();
        _sampleAppearance = new(ribbon, this);
        SynchronizeInputMode();
        SynchronizeThemeGallery();
        ConfigurePopup(selectDropdown, "Select options");
        ConfigurePopup(pasteSplitButton, "Paste options");
        ConfigurePopup(applicationOpen, "Open options");
        pasteSplitButton.Click += (_, _) => statusLabel.Text = "Paste primary action";
    }
    private void ConfigurePopup(RibbonDropDownItem item, string caption)
    {
        if (item.Content is not PopupOptionsControl body) return;
        body.Heading.Text = caption;
        body.Applied += (_, _) => { statusLabel.Text = caption + ": " + body.ValueEditor.Text + " / " + body.ModeEditor.Text; item.CloseDropDown(); };
        body.Dismissed += (_, _) => item.CloseDropDown();
    }
    private void documentStylesGallery_ItemPreview(object? sender, RibbonGalleryPreviewEventArgs e) => statusLabel.Text = "Preview: " + e.Item.Text;
    private void documentStylesGallery_ItemPreviewCancelled(object? sender, EventArgs e) => statusLabel.Text = "Selected: " + documentStylesGallery.SelectedItem?.Text;
    private void documentStylesGallery_SelectionCommitted(object? sender, EventArgs e) => statusLabel.Text = "Selected: " + documentStylesGallery.SelectedItem?.Text;

    private void ApplicationExit_Click(object? sender, EventArgs e) => Close();

    private void ApplicationCommand_Click(object? sender, EventArgs e) => statusLabel.Text = "Application Menu: " + (sender as RibbonItem)?.Text;

    private void ApplyButton_Click(object? sender, EventArgs e) => statusLabel.Text = "Applied: " + nameField.Value;
    private void BackstageSaveCommand_Click(object? sender, EventArgs e) => statusLabel.Text = "Backstage Save: " + ((BackstageInfoControl?)infoPage.Content)?.NotesEditor.Text;
    private void BackstageOptionsCommand_Click(object? sender, EventArgs e) => statusLabel.Text = "Backstage Options";

    private void saveButton_Click(object sender, EventArgs e)
    {
        MessageBox.Show("Save QAT command clicked");
    }
}
