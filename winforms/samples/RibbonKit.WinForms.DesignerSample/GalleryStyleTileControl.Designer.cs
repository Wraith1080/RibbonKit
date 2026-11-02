namespace RibbonKitDesignerConsumer;

partial class GalleryStyleTileControl
{
    private Panel paperPanel = null!;
    private Panel accentBar = null!;
    private Label previewLabel = null!;
    private Label titleLabel = null!;

    private void InitializeComponent()
    {
        paperPanel = new Panel();
        accentBar = new Panel();
        previewLabel = new Label();
        titleLabel = new Label();
        paperPanel.SuspendLayout();
        SuspendLayout();

        // Paper preview: ordinary designer-editable controls.
        paperPanel.BackColor = Color.White;
        paperPanel.Controls.Add(previewLabel);
        paperPanel.Controls.Add(accentBar);
        paperPanel.Location = new Point(10, 9);
        paperPanel.Name = "paperPanel";
        paperPanel.Size = new Size(34, 52);
        paperPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        accentBar.BackColor = Color.SteelBlue;
        accentBar.Location = new Point(5, 6);
        accentBar.Name = "accentBar";
        accentBar.Size = new Size(24, 4);
        previewLabel.ForeColor = Color.DimGray;
        previewLabel.Location = new Point(3, 15);
        previewLabel.Name = "previewLabel";
        previewLabel.Size = new Size(28, 30);
        previewLabel.Text = "Aa";
        previewLabel.TextAlign = ContentAlignment.MiddleCenter;

        titleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        titleLabel.Location = new Point(53, 3);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(115, 64);
        titleLabel.Text = "Heading 1";
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        titleLabel.UseMnemonic = false;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        Controls.Add(titleLabel);
        Controls.Add(paperPanel);
        Name = "GalleryStyleTileControl";
        Size = new Size(174, 70);
        TabStop = false;
        paperPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}
