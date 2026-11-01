namespace RibbonKitDesignerConsumer;

partial class BackstageInfoControl
{
    private TableLayoutPanel pageLayout = null!;
    private Label headingLabel = null!, helpLabel = null!, titleLabel = null!, ownerLabel = null!, notesLabel = null!;
    private TextBox titleTextBox = null!, ownerTextBox = null!, notesTextBox = null!;
    private FlowLayoutPanel actionsPanel = null!;
    private Button clearNotesButton = null!, resetNotesButton = null!;
    private void InitializeComponent()
    {
        pageLayout = new TableLayoutPanel();
        headingLabel = new Label(); helpLabel = new Label(); titleLabel = new Label(); ownerLabel = new Label(); notesLabel = new Label();
        titleTextBox = new TextBox(); ownerTextBox = new TextBox(); notesTextBox = new TextBox();
        actionsPanel = new FlowLayoutPanel(); clearNotesButton = new Button(); resetNotesButton = new Button();
        SuspendLayout(); pageLayout.SuspendLayout(); actionsPanel.SuspendLayout();
        pageLayout.Name = "pageLayout"; pageLayout.Dock = DockStyle.Fill; pageLayout.Padding = new Padding(24); pageLayout.AutoScroll = true;
        pageLayout.ColumnCount = 2; pageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); pageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pageLayout.RowCount = 6;
        pageLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); pageLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); pageLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); pageLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headingLabel.Name = "headingLabel"; headingLabel.Text = "Document information"; headingLabel.AutoSize = true;
        headingLabel.Margin = new Padding(3, 0, 3, 10); pageLayout.Controls.Add(headingLabel, 0, 0); pageLayout.SetColumnSpan(headingLabel, 2);
        helpLabel.Name = "helpLabel"; helpLabel.Text = "This page is a normal UserControl. Edit its layout in BackstageInfoControl.cs → View Designer.";
        helpLabel.AutoSize = true; helpLabel.Dock = DockStyle.Fill; helpLabel.Margin = new Padding(3, 0, 3, 18);
        pageLayout.Controls.Add(helpLabel, 0, 1); pageLayout.SetColumnSpan(helpLabel, 2);
        titleLabel.Name = "titleLabel"; titleLabel.Text = "Document"; titleLabel.AutoSize = true; titleLabel.Anchor = AnchorStyles.Left;
        pageLayout.Controls.Add(titleLabel, 0, 2);
        titleTextBox.Name = "titleTextBox"; titleTextBox.Text = "Quarterly report"; titleTextBox.Dock = DockStyle.Fill; titleTextBox.TabIndex = 0;
        pageLayout.Controls.Add(titleTextBox, 1, 2);
        ownerLabel.Name = "ownerLabel"; ownerLabel.Text = "Owner"; ownerLabel.AutoSize = true; ownerLabel.Anchor = AnchorStyles.Left;
        pageLayout.Controls.Add(ownerLabel, 0, 3);
        ownerTextBox.Name = "ownerTextBox"; ownerTextBox.Text = "Example author"; ownerTextBox.Dock = DockStyle.Fill; ownerTextBox.TabIndex = 1;
        pageLayout.Controls.Add(ownerTextBox, 1, 3);
        notesLabel.Name = "notesLabel"; notesLabel.Text = "Notes"; notesLabel.AutoSize = true; notesLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        notesLabel.Margin = new Padding(3, 8, 12, 3); pageLayout.Controls.Add(notesLabel, 0, 4);
        notesTextBox.Name = "notesTextBox"; notesTextBox.Multiline = true; notesTextBox.ScrollBars = ScrollBars.Vertical;
        notesTextBox.Text = "Application page notes"; notesTextBox.Dock = DockStyle.Fill; notesTextBox.MinimumSize = new Size(120, 100);
        notesTextBox.TabIndex = 2; pageLayout.Controls.Add(notesTextBox, 1, 4);
        actionsPanel.Name = "actionsPanel"; actionsPanel.AutoSize = true; actionsPanel.Dock = DockStyle.Fill;
        actionsPanel.FlowDirection = FlowDirection.LeftToRight; actionsPanel.WrapContents = true; actionsPanel.Margin = new Padding(0, 12, 0, 0);
        clearNotesButton.Name = "clearNotesButton"; clearNotesButton.Text = "Clear notes"; clearNotesButton.AutoSize = true; clearNotesButton.TabIndex = 0;
        resetNotesButton.Name = "resetNotesButton"; resetNotesButton.Text = "Restore example"; resetNotesButton.AutoSize = true; resetNotesButton.TabIndex = 1;
        clearNotesButton.Click += ClearNotesButton_Click; resetNotesButton.Click += ResetNotesButton_Click;
        actionsPanel.Controls.Add(clearNotesButton); actionsPanel.Controls.Add(resetNotesButton);
        pageLayout.Controls.Add(actionsPanel, 1, 5);
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi; Size = new Size(640, 420);
        Controls.Add(pageLayout); Name = "BackstageInfoControl";
        actionsPanel.ResumeLayout(false); actionsPanel.PerformLayout(); pageLayout.ResumeLayout(false); pageLayout.PerformLayout(); ResumeLayout(false);
    }
}
