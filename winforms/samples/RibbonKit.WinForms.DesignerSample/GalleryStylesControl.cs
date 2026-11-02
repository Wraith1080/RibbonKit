using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Edit a card's appearance in GalleryStyleTileControl's normal UserControl designer.
// This control connects those cards to the source gallery and arranges them.
public partial class GalleryStylesControl : UserControl, IRibbonGalleryPresenter
{
    private const int ColumnCount = 2;
    private readonly Dictionary<RibbonGalleryTile, GalleryStyleTileControl> _views = [];
    private RibbonGalleryPresenterContext? _context;
    private RibbonGalleryTile[] _items = [];
    private RibbonGalleryTile? _hotItem;
    private RibbonGalleryTile? _pressedItem;
    private Point _lastPointer = Cursor.Position;
    private int _scrollOffset;
    private bool _updating;

    internal RibbonGalleryPresenterContext Context => _context!;
    private int Scale(int pixels) => (int)Math.Round(pixels * (_context?.Dpi ?? DeviceDpi) / 96d);
    private int RowHeight => Scale(_context?.Density == RibbonDensity.Touch ? 88 : 76);
    private int Gap => Scale(6);
    private int RowCount => (_items.Length + ColumnCount - 1) / ColumnCount;
    private int MaximumOffset => Math.Max(0, Gap + RowCount * RowHeight - Context.Viewport.Height);

    public GalleryStylesControl()
    {
        InitializeComponent();
        DoubleBuffered = true;
        TabStop = true;
    }

    public override Size GetPreferredSize(Size proposedSize) => new(Scale(360), Scale(234));

    public void Synchronize(RibbonGalleryPresenterContext context)
    {
        _context = context;
        _items = context.Gallery.Items.OfType<RibbonGalleryTile>().Where(item => item.Visible).ToArray();
        if (_hotItem is { Enabled: false }) _hotItem = null;
        BackColor = context.Theme.Surface;

        _updating = true;
        SuspendLayout();
        try
        {
            RemoveOldCards();
            foreach (var item in _items)
            {
                if (_views.ContainsKey(item)) continue;

                // Reuse the six cards visible in this control's designer first.
                var card = Controls.OfType<GalleryStyleTileControl>().FirstOrDefault(view => view.Tag is null)
                    ?? new GalleryStyleTileControl();
                card.Tag = item;
                ConnectPointerEvents(card, item);
                _views.Add(item, card);
                if (card.Parent is null) Controls.Add(card);
            }
            foreach (var unused in Controls.OfType<GalleryStyleTileControl>().Where(card => card.Tag is null))
                unused.Visible = false;

            LayoutCards();
            RefreshCardStates();
        }
        finally
        {
            ResumeLayout(false);
            _updating = false;
        }
    }

    private void RemoveOldCards()
    {
        foreach (var item in _views.Keys.Except(_items).ToArray())
        {
            _views[item].Dispose();
            _views.Remove(item);
            if (ReferenceEquals(_hotItem, item)) _hotItem = null;
            if (ReferenceEquals(_pressedItem, item)) _pressedItem = null;
        }
    }

    private void LayoutCards()
    {
        if (_context is null) return;
        _scrollOffset = Math.Clamp(_scrollOffset, 0, MaximumOffset);
        int cardWidth = Math.Max(1, (Context.Viewport.Width - (ColumnCount + 1) * Gap) / ColumnCount);
        for (int index = 0; index < _items.Length; index++)
        {
            int column = index % ColumnCount;
            int row = index / ColumnCount;
            var card = _views[_items[index]];
            card.Bounds = new(Gap + column * (cardWidth + Gap), Gap + row * RowHeight - _scrollOffset, cardWidth, RowHeight - Gap);
            card.Visible = true;
        }
    }

    private void RefreshCardStates()
    {
        foreach (var item in _items)
            _views[item].ShowItem(item, Context, ReferenceEquals(_hotItem, item));
    }

    public Rectangle GetItemBounds(RibbonGalleryTile item) => _views.TryGetValue(item, out var card) ? card.Bounds : Rectangle.Empty;

    public RibbonGalleryTile? HitTest(Point location) => ClientRectangle.Contains(location)
        ? _items.FirstOrDefault(item => GetItemBounds(item).Contains(location)) : null;

    public RibbonGalleryTile? Navigate(RibbonGalleryTile? current, Keys key)
    {
        var available = _items.Where(item => item.Enabled).ToArray();
        if (available.Length == 0) return null;

        int index = Array.IndexOf(available, current);
        if (key == Keys.Home || index < 0) return available[0];
        if (key == Keys.End) return available[^1];
        if (key is Keys.Left or Keys.Right or Keys.Tab or (Keys.Tab | Keys.Shift))
        {
            int direction = key is Keys.Left or (Keys.Tab | Keys.Shift) ? -1 : 1;
            return available[Math.Clamp(index + direction, 0, available.Length - 1)];
        }

        bool backwards = key is Keys.Up or Keys.PageUp;
        var origin = GetItemBounds(available[index]);
        bool page = key is Keys.PageUp or Keys.PageDown;
        int targetTop = origin.Top + (page ? Context.Viewport.Height * (backwards ? -1 : 1) : 0);
        return available.Where(item => backwards ? GetItemBounds(item).Top < origin.Top : GetItemBounds(item).Top > origin.Top)
            .OrderBy(item => Math.Abs(GetItemBounds(item).Top - targetTop))
            .ThenBy(item => Math.Abs(GetItemBounds(item).Left - origin.Left))
            .FirstOrDefault() ?? current;
    }

    public void Reveal(RibbonGalleryTile item)
    {
        var bounds = GetItemBounds(item);
        if (bounds.IsEmpty) return;
        if (bounds.Top < Gap) _scrollOffset += bounds.Top - Gap;
        else if (bounds.Bottom > Context.Viewport.Height - Gap) _scrollOffset += bounds.Bottom - Context.Viewport.Height + Gap;
        LayoutCards();
    }

    public bool CanScrollBackward => _scrollOffset > 0;
    public bool CanScrollForward => _scrollOffset < MaximumOffset;

    public void ScrollView(RibbonGalleryScroll amount)
    {
        bool backwards = amount is RibbonGalleryScroll.RowBackward or RibbonGalleryScroll.PageBackward;
        bool page = amount is RibbonGalleryScroll.PageBackward or RibbonGalleryScroll.PageForward;
        int step = page ? Context.Viewport.Height : RowHeight;
        _scrollOffset += backwards ? -step : step;
        LayoutCards();
        Context.NotifyViewChanged();
    }

    // Child controls receive their own mouse events. Use the context so
    // selection, validation, commands and closing still happen exactly once.
    private void ConnectPointerEvents(Control control, RibbonGalleryTile item)
    {
        control.MouseMove += (_, _) => PreviewItem(item);
        control.MouseLeave += (_, _) =>
        {
            if (ReferenceEquals(_hotItem, item) && !ReferenceEquals(HitTest(PointToClient(Cursor.Position)), item))
            {
                _hotItem = null;
                _context?.Preview(null);
                RefreshCardStates();
            }
        };
        control.MouseDown += (_, e) =>
        {
            _pressedItem = e.Button == MouseButtons.Left ? item : null;
            if (_pressedItem is not null) _context?.FocusItem(item);
        };
        control.MouseUp += (_, e) =>
        {
            var pressed = _pressedItem;
            _pressedItem = null;
            var location = PointToClient(control.PointToScreen(e.Location));
            if (e.Button == MouseButtons.Left && ReferenceEquals(pressed, item) && ReferenceEquals(HitTest(location), item))
                _context?.Commit(item);
        };
        foreach (Control child in control.Controls) ConnectPointerEvents(child, item);
    }

    private void PreviewItem(RibbonGalleryTile item)
    {
        var pointer = Cursor.Position;
        // Showing a popup underneath a stationary pointer is not a hover.
        if (pointer == _lastPointer && _hotItem is null) return;
        _lastPointer = pointer;
        if (_context?.Preview(item) != true) return;
        _hotItem = item;
        RefreshCardStates();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (!_updating) LayoutCards();
    }
}
