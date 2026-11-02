using System.ComponentModel;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// A normal designer-editable UserControl: no gallery layout or commands here.
public partial class GalleryStyleTileControl : UserControl
{
    private Color _borderColor = Color.LightGray;
    private int _borderWidth = 1;

    public GalleryStyleTileControl()
    {
        InitializeComponent();
        DoubleBuffered = true;
    }

    [Category("Appearance"), DefaultValue("Heading 1")]
    public string Caption
    {
        get => titleLabel.Text;
        set => titleLabel.Text = value;
    }

    internal void ShowItem(RibbonGalleryTile item, RibbonGalleryPresenterContext context, bool hot)
    {
        bool selected = ReferenceEquals(context.Gallery.SelectedItem, item);
        bool focused = ReferenceEquals(context.FocusedItem, item);
        Caption = item.Text;
        AccessibleName = item.Text;
        Enabled = item.Enabled;
        BackColor = selected ? context.Theme.Checked.Start : hot ? context.Theme.Hover.Start : context.Theme.Surface;
        ForeColor = item.Enabled ? context.Theme.Foreground : SystemColors.GrayText;

        var borderColor = selected || focused ? context.Theme.Accent : context.Theme.Border;
        int borderWidth = selected ? 2 : 1;
        if (_borderColor == borderColor && _borderWidth == borderWidth) return;
        _borderColor = borderColor;
        _borderWidth = borderWidth;
        Invalidate();
    }

    // Only the selection outline needs painting. All card content is editable
    // as ordinary panels/labels in GalleryStyleTileControl.Designer.cs.
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var border = new Pen(_borderColor, _borderWidth);
        e.Graphics.DrawRectangle(border, 1, 1, Math.Max(0, Width - 3), Math.Max(0, Height - 3));
    }
}
