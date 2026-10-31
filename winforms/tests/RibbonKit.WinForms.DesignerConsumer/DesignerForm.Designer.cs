namespace RibbonKitDesignerConsumer;

partial class DesignerForm
{
    private System.ComponentModel.IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DesignerForm));
        ribbon = new RibbonKit.WinForms.RibbonControl();
        homeTab = new RibbonKit.WinForms.RibbonTab();
        editTab = new RibbonKit.WinForms.RibbonTab();
        commandsGroup = new RibbonKit.WinForms.RibbonGroup();
        fieldsGroup = new RibbonKit.WinForms.RibbonGroup();
        applyButton = new RibbonKit.WinForms.RibbonButtonItem();
        boldToggle = new RibbonKit.WinForms.RibbonToggleItem();
        nameField = new RibbonKit.WinForms.RibbonTextBoxItem();
        fontField = new RibbonKit.WinForms.RibbonComboBoxItem();
        bindingTextBox = new TextBox();
        statusLabel = new Label();
        components.Add(homeTab, "homeTab");
        components.Add(editTab, "editTab");
        components.Add(commandsGroup, "commandsGroup");
        components.Add(fieldsGroup, "fieldsGroup");
        components.Add(applyButton, "applyButton");
        components.Add(boldToggle, "boldToggle");
        components.Add(nameField, "nameField");
        components.Add(fontField, "fontField");
        SuspendLayout();
        ribbon.Dock = DockStyle.Top;
        ribbon.Name = "ribbon";
        ribbon.TabIndex = 0;
        ribbon.Tabs.AddRange(new RibbonKit.WinForms.RibbonTab[] { homeTab, editTab });
        homeTab.Id = "home";
        homeTab.Text = "Home";
        homeTab.KeyTip = "H";
        homeTab.Groups.Add(commandsGroup);
        editTab.Id = "edit";
        editTab.Text = "Edit";
        editTab.KeyTip = "E";
        editTab.Groups.Add(fieldsGroup);
        commandsGroup.Id = "commands";
        commandsGroup.Text = "Commands";
        commandsGroup.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:copy", true);
        commandsGroup.Items.AddRange(new RibbonKit.WinForms.RibbonItem[] { applyButton, boldToggle });
        fieldsGroup.Id = "fields";
        fieldsGroup.Text = "Fields";
        fieldsGroup.Items.AddRange(new RibbonKit.WinForms.RibbonItem[] { nameField, fontField });
        applyButton.Id = "apply";
        applyButton.Text = "Apply";
        applyButton.KeyTip = "A";
        applyButton.Image = (Image)resources.GetObject("applyButton.Image");
        applyButton.Click += ApplyButton_Click;
        boldToggle.Id = "bold";
        boldToggle.Text = "Bold";
        boldToggle.Checked = true;
        boldToggle.Icon = (RibbonKit.WinForms.IRibbonIcon)resources.GetObject("boldToggle.Icon");
        nameField.Id = "name";
        nameField.Text = "Name";
        nameField.Value = "Initial value";
        nameField.DataBindings.Add(new Binding("Value", bindingTextBox, "Text", false, DataSourceUpdateMode.OnPropertyChanged));
        fontField.Id = "font";
        fontField.Text = "Font";
        fontField.Items.AddRange(new string[] { "Segoe UI", "Arial", "Consolas" });
        fontField.SelectedIndex = 0;
        bindingTextBox.Location = new Point(24, 240);
        bindingTextBox.Name = "bindingTextBox";
        bindingTextBox.Size = new Size(300, 27);
        bindingTextBox.TabIndex = 1;
        bindingTextBox.Text = "Initial value";
        statusLabel.AutoSize = true;
        statusLabel.Location = new Point(24, 290);
        statusLabel.Name = "statusLabel";
        statusLabel.Text = "Designer consumer";
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(900, 500);
        Controls.Add(statusLabel);
        Controls.Add(bindingTextBox);
        Controls.Add(ribbon);
        Name = "DesignerForm";
        Text = "RibbonKit independent designer consumer";
        ResumeLayout(false);
        PerformLayout();
    }

    private RibbonKit.WinForms.RibbonControl ribbon = null!;
    private RibbonKit.WinForms.RibbonTab homeTab = null!;
    private RibbonKit.WinForms.RibbonTab editTab = null!;
    private RibbonKit.WinForms.RibbonGroup commandsGroup = null!;
    private RibbonKit.WinForms.RibbonGroup fieldsGroup = null!;
    private RibbonKit.WinForms.RibbonButtonItem applyButton = null!;
    private RibbonKit.WinForms.RibbonToggleItem boldToggle = null!;
    private RibbonKit.WinForms.RibbonTextBoxItem nameField = null!;
    private RibbonKit.WinForms.RibbonComboBoxItem fontField = null!;
    private TextBox bindingTextBox = null!;
    private Label statusLabel = null!;
}
