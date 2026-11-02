namespace RibbonKitDesignerConsumer;

partial class GalleryStylesStripControl
{
    private GalleryStyleTileControl headingCard = null!;
    private GalleryStyleTileControl bodyCard = null!;
    private GalleryStyleTileControl quoteCard = null!;
    private void InitializeComponent()
    {
        headingCard = new GalleryStyleTileControl();
        bodyCard = new GalleryStyleTileControl();
        quoteCard = new GalleryStyleTileControl();
        SuspendLayout();
        headingCard.Caption = "Heading 1";
        headingCard.Location = new Point(3, 3);
        headingCard.Name = "headingCard";
        headingCard.Size = new Size(194, 74);
        bodyCard.Caption = "Body 2";
        bodyCard.Location = new Point(203, 3);
        bodyCard.Name = "bodyCard";
        bodyCard.Size = new Size(194, 74);
        quoteCard.Caption = "Quote 3";
        quoteCard.Location = new Point(403, 3);
        quoteCard.Name = "quoteCard";
        quoteCard.Size = new Size(194, 74);
        // Runtime layout uses the context's physical viewport and DPI. Do not
        // scale those bounds again when this root is parented into the ribbon.
        AutoScaleMode = AutoScaleMode.None;
        Controls.AddRange(new Control[] { headingCard, bodyCard, quoteCard });
        Name = "GalleryStylesStripControl";
        Size = new Size(600, 80);
        ResumeLayout(false);
    }
}
