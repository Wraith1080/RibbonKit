namespace RibbonKitDesignerConsumer;

partial class GalleryStylesControl
{
    private GalleryStyleTileControl headingCard = null!;
    private GalleryStyleTileControl bodyCard = null!;
    private GalleryStyleTileControl quoteCard = null!;
    private GalleryStyleTileControl captionCard = null!;
    private GalleryStyleTileControl headingFiveCard = null!;
    private GalleryStyleTileControl bodySixCard = null!;

    private void InitializeComponent()
    {
        headingCard = new GalleryStyleTileControl();
        bodyCard = new GalleryStyleTileControl();
        quoteCard = new GalleryStyleTileControl();
        captionCard = new GalleryStyleTileControl();
        headingFiveCard = new GalleryStyleTileControl();
        bodySixCard = new GalleryStyleTileControl();
        SuspendLayout();

        headingCard.Caption = "Heading 1";
        headingCard.Location = new Point(6, 6);
        headingCard.Name = "headingCard";
        headingCard.Size = new Size(171, 70);
        bodyCard.Caption = "Body 2";
        bodyCard.Location = new Point(183, 6);
        bodyCard.Name = "bodyCard";
        bodyCard.Size = new Size(171, 70);
        quoteCard.Caption = "Quote 3";
        quoteCard.Location = new Point(6, 82);
        quoteCard.Name = "quoteCard";
        quoteCard.Size = new Size(171, 70);
        captionCard.Caption = "Caption 4";
        captionCard.Location = new Point(183, 82);
        captionCard.Name = "captionCard";
        captionCard.Size = new Size(171, 70);
        headingFiveCard.Caption = "Heading 5";
        headingFiveCard.Location = new Point(6, 158);
        headingFiveCard.Name = "headingFiveCard";
        headingFiveCard.Size = new Size(171, 70);
        bodySixCard.Caption = "Body 6";
        bodySixCard.Location = new Point(183, 158);
        bodySixCard.Name = "bodySixCard";
        bodySixCard.Size = new Size(171, 70);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.AddRange(new Control[] { headingCard, bodyCard, quoteCard, captionCard, headingFiveCard, bodySixCard });
        Name = "GalleryStylesControl";
        Size = new Size(360, 234);
        ResumeLayout(false);
    }
}
