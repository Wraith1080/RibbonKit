namespace RibbonKitDesignerConsumer;

partial class PopupOptionsControl
{
    private System.ComponentModel.IContainer components = null!;
    private Label heading = null!;
    private Label valueLabel = null!;
    private TextBox valueEditor = null!;
    private Label modeLabel = null!;
    private ComboBox modeEditor = null!;
    private CheckBox rememberCheck = null!;
    private Label validationLabel = null!;
    private Button applyButton = null!;
    private Button cancelButton = null!;
    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        heading = new Label(); valueLabel = new Label(); valueEditor = new TextBox();
        modeLabel = new Label(); modeEditor = new ComboBox(); rememberCheck = new CheckBox();
        validationLabel = new Label(); applyButton = new Button(); cancelButton = new Button();
        SuspendLayout();
        heading.AutoSize = true;
        heading.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        heading.Location = new Point(16, 12); heading.Text = "Popup options";
        valueLabel.AutoSize = true; valueLabel.Location = new Point(16, 49); valueLabel.Text = "Value";
        valueEditor.Location = new Point(16, 70); valueEditor.Size = new Size(288, 23); valueEditor.Text = "Document"; valueEditor.TabIndex = 0;
        valueEditor.Validating += ValueValidating;
        modeLabel.AutoSize = true; modeLabel.Location = new Point(16, 106); modeLabel.Text = "Mode";
        modeEditor.DropDownStyle = ComboBoxStyle.DropDownList;
        modeEditor.Items.AddRange(new object[] { "Current selection", "Whole document", "New items only" });
        modeEditor.Location = new Point(16, 127); modeEditor.Size = new Size(288, 23); modeEditor.SelectedIndex = 0; modeEditor.TabIndex = 1;
        rememberCheck.AutoSize = true; rememberCheck.Location = new Point(16, 164); rememberCheck.Text = "Remember these options"; rememberCheck.TabIndex = 2;
        validationLabel.AutoSize = true; validationLabel.ForeColor = Color.Firebrick; validationLabel.Location = new Point(16, 194);
        applyButton.Location = new Point(140, 224); applyButton.Size = new Size(78, 28); applyButton.Text = "Apply"; applyButton.TabIndex = 3;
        applyButton.CausesValidation = false; applyButton.Click += ApplyClicked;
        cancelButton.Location = new Point(226, 224); cancelButton.Size = new Size(78, 28); cancelButton.Text = "Cancel"; cancelButton.TabIndex = 4;
        cancelButton.CausesValidation = false; cancelButton.Click += CancelClicked;
        AutoScaleDimensions = new SizeF(96F, 96F); AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = SystemColors.Window; Font = new Font("Segoe UI", 9F);
        Controls.AddRange(new Control[] { heading, valueLabel, valueEditor, modeLabel, modeEditor, rememberCheck, validationLabel, applyButton, cancelButton });
        Name = "PopupOptionsControl"; Size = new Size(320, 268);
        ResumeLayout(false); PerformLayout();
    }
}
