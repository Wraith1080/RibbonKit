namespace RibbonKitDesignerConsumer;

partial class BackstagePrintControl
{
    private FlowLayoutPanel pageLayout = null!;
    private Label headingLabel = null!, helpLabel = null!;
    private ComboBox orientationComboBox = null!;
    private CheckBox colorCheckBox = null!;
    private void InitializeComponent()
    {
        pageLayout = new FlowLayoutPanel(); headingLabel = new Label(); helpLabel = new Label();
        orientationComboBox = new ComboBox(); colorCheckBox = new CheckBox();
        SuspendLayout(); pageLayout.SuspendLayout();
        pageLayout.Name = "pageLayout"; pageLayout.Dock = DockStyle.Fill; pageLayout.FlowDirection = FlowDirection.TopDown;
        pageLayout.WrapContents = false; pageLayout.AutoScroll = true; pageLayout.Padding = new Padding(24);
        headingLabel.Name = "headingLabel"; headingLabel.AutoSize = true; headingLabel.Text = "Print settings";
        helpLabel.Name = "helpLabel"; helpLabel.AutoSize = true; helpLabel.Text = "A second UserControl page. These example settings do not send a print job.";
        helpLabel.Margin = new Padding(3, 10, 3, 18);
        orientationComboBox.Name = "orientationComboBox"; orientationComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        orientationComboBox.Items.AddRange(new object[] { "Portrait", "Landscape" }); orientationComboBox.SelectedIndex = 0;
        orientationComboBox.Size = new Size(220, 25); orientationComboBox.TabIndex = 0; orientationComboBox.AccessibleName = "Orientation";
        colorCheckBox.Name = "colorCheckBox"; colorCheckBox.AutoSize = true; colorCheckBox.Text = "Print in color"; colorCheckBox.Checked = true; colorCheckBox.TabIndex = 1;
        pageLayout.Controls.Add(headingLabel); pageLayout.Controls.Add(helpLabel); pageLayout.Controls.Add(orientationComboBox); pageLayout.Controls.Add(colorCheckBox);
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi; Size = new Size(640, 420);
        Controls.Add(pageLayout); Name = "BackstagePrintControl";
        pageLayout.ResumeLayout(false); pageLayout.PerformLayout(); ResumeLayout(false);
    }
}
