using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Edit a card's appearance in GalleryStyleTileControl's normal UserControl designer.
// This control connects those cards to the source gallery and arranges them.
public partial class GalleryStylesControl : UserControl, IRibbonGalleryPresenter
{
    private const int ColumnCount = 2;
    private readonly Dictionary<RibbonGalleryTile, GalleryStyleTileControl> _views = [];
    private readonly Dictionary<RibbonGalleryTile, int> _indices = [];
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
    // The initial synchronization receives the monitor budget, before preferred
    // size is measured. Keep realization within this presenter's natural size.
    private Size ViewSize => new(Math.Min(Context.Viewport.Width, Scale(360)), Math.Min(Context.Viewport.Height, Scale(234)));
    private int MaximumOffset => Math.Max(0, Gap + RowCount * RowHeight - ViewSize.Height);

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
        _indices.Clear();
        for (int i = 0; i < _items.Length; i++) _indices.Add(_items[i], i);
        if (_hotItem is { Enabled: false }) _hotItem = null;
        BackColor = context.Theme.Surface;

        _updating = true;
        SuspendLayout();
        try
        {
            RemoveOldCards();
            // Keep the six designer-authored cards, but do not create HWNDs or
            // managed card trees for the unvisited rows of a large gallery.
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
        foreach (var item in _views.Keys.Where(item => !_indices.ContainsKey(item)).ToArray())
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
        SuspendLayout();
        try
        {
            var viewport = new Rectangle(Point.Empty, ViewSize);
            foreach (var pair in _views)
                if (!GetItemBounds(pair.Key).IntersectsWith(viewport)) pair.Value.Visible = false;
            int firstRow = Math.Max(0, (_scrollOffset - Gap) / RowHeight);
            int lastRow = Math.Min(RowCount - 1, (_scrollOffset + ViewSize.Height - 1 - Gap) / RowHeight);
            for (int index = firstRow * ColumnCount; index < Math.Min(_items.Length, (lastRow + 1) * ColumnCount); index++)
            {
                var item = _items[index];
                if (!_views.TryGetValue(item, out var card))
                {
                    card = Controls.OfType<GalleryStyleTileControl>().FirstOrDefault(view => view.Tag is null)
                        ?? new GalleryStyleTileControl();
                    card.Visible = false;
                    card.Tag = item;
                    ConnectPointerEvents(card, item);
                    _views.Add(item, card);
                    if (card.Parent is null) Controls.Add(card);
                }
                card.ShowItem(item, Context, ReferenceEquals(_hotItem, item));
                // Complete first-show native autoscaling before applying bounds
                // measured in the context's physical pixels.
                card.Visible = true;
                card.Bounds = GetItemBounds(item);
            }
        }
        finally { ResumeLayout(false); }
    }

    private void RefreshCardStates()
    {
        foreach (var pair in _views)
            if (pair.Value.Visible) pair.Value.ShowItem(pair.Key, Context, ReferenceEquals(_hotItem, pair.Key));
    }

    public Rectangle GetItemBounds(RibbonGalleryTile item)
    {
        if (!_indices.TryGetValue(item, out int index)) return Rectangle.Empty;
        int cardWidth = Math.Max(1, (ViewSize.Width - (ColumnCount + 1) * Gap) / ColumnCount);
        return new(Gap + index % ColumnCount * (cardWidth + Gap), Gap + index / ColumnCount * RowHeight - _scrollOffset, cardWidth, RowHeight - Gap);
    }

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
        int targetTop = origin.Top + (page ? ViewSize.Height * (backwards ? -1 : 1) : 0);
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
        else if (bounds.Bottom > ViewSize.Height - Gap) _scrollOffset += bounds.Bottom - ViewSize.Height + Gap;
        LayoutCards();
    }

    public bool CanScrollBackward => _scrollOffset > 0;
    public bool CanScrollForward => _scrollOffset < MaximumOffset;

    public void ScrollView(RibbonGalleryScroll amount)
    {
        bool backwards = amount is RibbonGalleryScroll.RowBackward or RibbonGalleryScroll.PageBackward;
        bool page = amount is RibbonGalleryScroll.PageBackward or RibbonGalleryScroll.PageForward;
        int step = page ? ViewSize.Height : RowHeight;
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
