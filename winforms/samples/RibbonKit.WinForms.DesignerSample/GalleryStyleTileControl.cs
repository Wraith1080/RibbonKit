using System.ComponentModel;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// A normal designer-editable UserControl: no gallery layout or commands here.
public partial class GalleryStyleTileControl : UserControl
{
    private Color _borderColor = Color.LightGray;
    private int _borderWidth = 1;
    private RibbonGalleryPresenterContext? _context;
    private RibbonGalleryTile? _item;
    private bool _hot, _pressed;

    public GalleryStyleTileControl()
    {
        InitializeComponent();
        titleLabel.BackColor = Color.Transparent;
        DoubleBuffered = true;
    }

    [Category("Appearance"), DefaultValue("Heading 1")]
    public string Caption
    {
        get => titleLabel.Text;
        set => titleLabel.Text = value;
    }

    internal void ShowItem(RibbonGalleryTile item, RibbonGalleryPresenterContext context, bool hot, bool pressed = false)
    {
        _context = context; _item = item; _hot = hot; _pressed = pressed;
        Caption = item.Text;
        AccessibleName = item.Text;
        Enabled = item.Enabled;
        BackColor = context.Theme.Surface;
        ForeColor = item.Enabled ? context.Theme.Foreground : SystemColors.GrayText;

        _borderColor = context.Theme.Border;
        Invalidate(true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
        if (_context is not { } c || _item is not { } item) return;
        c.Renderer.DrawGalleryTileState(new(e.Graphics, ClientRectangle, c.Theme, Font, DeviceDpi, Hot: _hot, Pressed: _pressed) { Density = c.Density }, item, ReferenceEquals(c.Gallery.SelectedItem, item));
    }

    // Theme-owned state paint stays separate from the ordinary designer-editable
    // paper artwork and labels in GalleryStyleTileControl.Designer.cs.
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        bool selected = _context is { } c && ReferenceEquals(c.Gallery.SelectedItem, _item);
        if (!selected && !_hot && !_pressed)
        {
            using var border = new Pen(_borderColor, _borderWidth);
            e.Graphics.DrawRectangle(border, 1, 1, Math.Max(0, Width - 3), Math.Max(0, Height - 3));
        }
        if (_context is { } context && ReferenceEquals(context.FocusedItem, _item))
            context.Renderer.DrawFocus(new(e.Graphics, ClientRectangle, context.Theme, Font, DeviceDpi) { Density = context.Density });
    }
}
