using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Edit the real card controls in this UserControl's designer. The popup example
// stays separate; both views bind to the same source tiles at runtime.
public partial class GalleryStylesStripControl : UserControl, IRibbonGalleryPresenter
{
    private readonly Dictionary<RibbonGalleryTile, GalleryStyleTileControl> _views = [];
    private RibbonGalleryTile[] _items = [];
    private RibbonGalleryPresenterContext? _context;
    private RibbonGalleryTile? _pressed, _hot;
    private Point _lastPointer = Cursor.Position;
    private int _row;
    internal RibbonGalleryPresenterContext Context => _context!;
    internal int ViewedRow => _row;
    private int Columns => _context?.Gallery.InRibbonColumnCount ?? 3;
    private int LastRow => Math.Max(0, (_items.Length - 1) / Columns);
    public GalleryStylesStripControl() { InitializeComponent(); DoubleBuffered = true; TabStop = true; }
    public override Size GetPreferredSize(Size proposedSize) => new(600, 80);
    public void Synchronize(RibbonGalleryPresenterContext context)
    {
        if (!ReferenceEquals(_context, context)) { _pressed = _hot = null; _lastPointer = Cursor.Position; }
        _context = context; _items = context.Gallery.Items.OfType<RibbonGalleryTile>().Where(item => item.Visible).ToArray();
        BackColor = context.Theme.Surface;
        SuspendLayout();
        try
        {
            foreach (var old in _views.Keys.Except(_items).ToArray()) { _views[old].Dispose(); _views.Remove(old); }
            foreach (var item in _items)
            {
                if (_views.ContainsKey(item)) continue;
                var card = Controls.OfType<GalleryStyleTileControl>().FirstOrDefault(view => view.Tag is null) ?? new GalleryStyleTileControl();
                card.Tag = item; card.TabStop = false; _views.Add(item, card); ConnectPointer(card, item);
                if (card.Parent is null) Controls.Add(card);
            }
            foreach (var unused in Controls.OfType<GalleryStyleTileControl>().Where(card => card.Tag is null)) unused.Visible = false;
            LayoutCards();
        }
        finally { ResumeLayout(false); }
    }
    private void LayoutCards()
    {
        if (_context is null) return;
        _row = Math.Clamp(_row, 0, LastRow);
        int gap = Math.Max(1, (int)Math.Round(3 * Context.Dpi / 96d));
        for (int i = 0; i < _items.Length; i++)
        {
            var card = _views[_items[i]];
            int left = Context.Viewport.Width * (i % Columns) / Columns;
            int right = Context.Viewport.Width * (i % Columns + 1) / Columns;
            card.Bounds = new(left + gap, (i / Columns - _row) * Context.Viewport.Height + gap, Math.Max(1, right - left - 2 * gap), Math.Max(1, Context.Viewport.Height - 2 * gap));
            card.Visible = true; card.ShowItem(_items[i], Context, ReferenceEquals(_hot, _items[i]));
        }
        Invalidate();
    }
    public Rectangle GetItemBounds(RibbonGalleryTile item) => _views.TryGetValue(item, out var card) ? card.Bounds : Rectangle.Empty;
    public RibbonGalleryTile? HitTest(Point point) => _context is not null && new Rectangle(Point.Empty, Context.Viewport).Contains(point)
        ? _items.FirstOrDefault(item => GetItemBounds(item).Contains(point)) : null;
    public RibbonGalleryTile? Navigate(RibbonGalleryTile? current, Keys key)
    {
        int index = Array.IndexOf(_items, current); if (_items.Length == 0) return null;
        int direction = key is Keys.Left or Keys.Up or Keys.PageUp or Keys.End ? -1 : 1;
        int step = key is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown ? Columns : 1;
        int next = key == Keys.Home ? 0 : key == Keys.End ? _items.Length - 1 : Math.Clamp(index + direction * step, 0, _items.Length - 1);
        while (next >= 0 && next < _items.Length)
        { if (_items[next].Enabled) return _items[next]; next += direction; }
        return current;
    }
    public void Reveal(RibbonGalleryTile item) { int index = Array.IndexOf(_items, item); if (index >= 0) { _row = index / Columns; LayoutCards(); } }
    public bool CanScrollBackward => _row > 0;
    public bool CanScrollForward => _row < LastRow;
    public void ScrollView(RibbonGalleryScroll amount)
    {
        _row = Math.Clamp(_row + (amount is RibbonGalleryScroll.RowBackward or RibbonGalleryScroll.PageBackward ? -1 : 1), 0, LastRow);
        LayoutCards(); _context?.NotifyViewChanged();
    }
    private void ConnectPointer(Control control, RibbonGalleryTile item)
    {
        control.TabStop = false;
        control.MouseMove += (_, _) =>
        {
            if (_lastPointer == Cursor.Position) return;
            _lastPointer = Cursor.Position;
            if (_context?.Preview(item) == true) { _hot = item; LayoutCards(); }
        };
        control.MouseLeave += (_, _) =>
        {
            if (!ReferenceEquals(HitTest(PointToClient(Cursor.Position)), item)) { _hot = null; _context?.Preview(null); LayoutCards(); }
        };
        control.MouseDown += (_, e) =>
        {
            _pressed = e.Button == MouseButtons.Left && (!control.CanSelect || control.ContainsFocus) ? item : null;
            if (_pressed is not null && _context?.FocusItem(item) != true) _pressed = null;
        };
        control.MouseCaptureChanged += (_, _) => { if (!control.Capture) _pressed = null; };
        control.MouseUp += (_, e) =>
        {
            var pressed = _pressed; _pressed = null; var point = PointToClient(control.PointToScreen(e.Location));
            if (e.Button == MouseButtons.Left && ReferenceEquals(pressed, item) && ReferenceEquals(HitTest(point), item)) _context?.Commit(item);
        };
        foreach (Control child in control.Controls) ConnectPointer(child, item);
    }
}
