using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// One reusable library presenter per source. Tiles, selection and application
// preview handlers stay on the source; popup/QAT surfaces only borrow the models.
internal sealed class RibbonGalleryStripManager(RibbonControl ribbon) : IDisposable
{
    private readonly Dictionary<RibbonGalleryItem, RibbonGalleryStripSurface> _strips = [];
    private bool _syncing, _disposed;
    internal RibbonGalleryStripSurface? SurfaceFor(RibbonGalleryItem source) => _strips.GetValueOrDefault(source);
    internal void Sync()
    {
        if (_syncing || _disposed || ribbon.IsInDesignMode) return;
        _syncing = true;
        try
        {
            var sources = ribbon.RetainedElements.OfType<RibbonGalleryItem>().Where(g => g.IsInRibbon && !g.IsDisposed).ToArray();
            foreach (var old in _strips.Keys.Except(sources).ToArray()) { _strips[old].Dispose(); _strips.Remove(old); }
            var slots = new Dictionary<RibbonGalleryItem, (Control Parent, RibbonTarget Target)>();
            void Add(Control parent, IEnumerable<RibbonTarget> targets)
            {
                foreach (var target in targets)
                    if (target.Element is RibbonGalleryItem { IsInRibbon: true } gallery && target.Size == RibbonItemSize.Large && !target.GalleryDropDown && !target.VisibleBounds.IsEmpty)
                        slots[gallery] = (parent, target);
            }
            Add(ribbon, ribbon.Snapshot.Targets);
            foreach (var popup in ribbon.Popups.Surfaces) Add(popup, popup.Targets);
            foreach (var source in sources)
            {
                bool shown = slots.TryGetValue(source, out var slot);
                if (!_strips.TryGetValue(source, out var strip))
                {
                    if (!shown) continue;
                    _strips[source] = strip = new(ribbon, source);
                }
                if (shown) strip.Present(slot.Parent, slot.Target); else strip.Park();
            }
        }
        finally { _syncing = false; }
    }
    internal bool Focus(RibbonGalleryItem source)
    {
        if (ribbon.IsBackstageOpen) return false;
        Sync();
        return _strips.TryGetValue(source, out var strip) && strip.Visible && strip.Focus();
    }
    internal void Release(Control parent)
    {
        foreach (var strip in _strips.Values.Where(s => ReferenceEquals(s.Parent, parent)).ToArray()) strip.Park();
    }
    internal void CancelPreviews() { foreach (var strip in _strips.Values.ToArray()) strip.Source.CancelPreview(strip); }
    public void Dispose() { _disposed = true; foreach (var strip in _strips.Values.ToArray()) strip.Dispose(); _strips.Clear(); }
}

internal sealed class RibbonGalleryStripSurface : Control
{
    private readonly RibbonControl _ribbon;
    internal RibbonGalleryItem Source { get; }
    private RibbonGalleryStripLayout _layout = null!;
    private RibbonGalleryTile? _focused, _hot, _pressed, _lastSelection;
    private RibbonGalleryStripButton? _hotButton, _pressedButton;
    private int _row;
    private int _lastSelectedIndex = int.MinValue, _lastColumns;
    private bool _presenting, _keyboardPreview;
    private Point? _lastPointer;
    private Rectangle _clip;
    internal RibbonGalleryStripLayout LayoutSnapshot => _layout;
    internal RibbonGalleryTile? FocusedTile => _focused;
    private bool Interactive => Source.EffectiveEnabled && Source.Visible && !_ribbon.IsBackstageOpen
        && (_ribbon.Popups.Top is null || ReferenceEquals(Parent, _ribbon.Popups.Top));
    internal RibbonGalleryStripSurface(RibbonControl ribbon, RibbonGalleryItem source)
    {
        _ribbon = ribbon; Source = source;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        TabStop = false; CausesValidation = false;
        Rebuild();
    }
    internal void Present(Control parent, RibbonTarget target)
    {
        if (IsDisposed || _presenting) return;
        _presenting = true;
        try
        {
            bool entering = !Visible || !ReferenceEquals(Parent, parent);
            if (!ReferenceEquals(Parent, parent)) { Park(); Parent = parent; }
            Font = _ribbon.Font; BackColor = _ribbon.Theme.Surface; Enabled = Source.EffectiveEnabled;
            AccessibleName = Source.Text + " gallery strip";
            Bounds = target.Bounds;
            var clip = target.VisibleBounds; clip.Offset(-Bounds.X, -Bounds.Y);
            if (_clip != clip) { _clip = clip; Region = new Region(clip); }
            int selectedIndex = Array.IndexOf(Tiles(), Source.SelectedItem);
            if (entering || !ReferenceEquals(_lastSelection, Source.SelectedItem) || selectedIndex != _lastSelectedIndex || _lastColumns != Source.InRibbonColumnCount) RevealSelection();
            _lastSelection = Source.SelectedItem;
            _lastSelectedIndex = selectedIndex; _lastColumns = Source.InRibbonColumnCount;
            Rebuild(); Visible = true; BringToFront(); Invalidate();
        }
        finally { _presenting = false; }
    }
    internal void Park()
    {
        Source.CancelPreview(this); _hot = _pressed = null; _hotButton = _pressedButton = null;
        Capture = false; Visible = false;
        if (Parent is not null) Parent = null;
    }
    private RibbonGalleryTile[] Tiles() => Source.Items.OfType<RibbonGalleryTile>().Where(t => t.Visible && !t.IsDisposed).ToArray();
    private void RevealSelection()
    {
        var tiles = Tiles(); int index = Source.SelectedItem is { } selected ? Array.IndexOf(tiles, selected) : -1;
        if (index >= 0) { _row = index / Source.InRibbonColumnCount; _focused = tiles[index]; }
        else if (_focused is null || !tiles.Contains(_focused) || !_focused.EffectiveEnabled) _focused = tiles.FirstOrDefault(t => t.EffectiveEnabled);
    }
    private void Rebuild()
    {
        _layout = RibbonGalleryStripLayout.Build(Source, ClientSize, _ribbon.DeviceDpi, _row); _row = _layout.Row;
        if (_focused is not null && (!Source.Items.Contains(_focused) || !_focused.Visible || !_focused.EffectiveEnabled)) _focused = null;
    }
    protected override void OnResize(EventArgs e) { base.OnResize(e); if (Source is not null) Rebuild(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var context = new RibbonPaintContext(e.Graphics, ClientRectangle, _ribbon.Theme, Font, _ribbon.DeviceDpi) { Density = _ribbon.Density };
        _ribbon.Painter.DrawGalleryStripFrame(context);
        foreach (var item in _layout.Tiles)
            _ribbon.Painter.DrawGalleryTile(context with { Bounds = item.Bounds, Hot = Interactive && ReferenceEquals(_hot, item.Tile),
                Pressed = Interactive && ReferenceEquals(_pressed, item.Tile), Focused = Focused && _ribbon.KeyboardFocusCues && ReferenceEquals(_focused, item.Tile) },
                item.Tile, ReferenceEquals(Source.SelectedItem, item.Tile));
        foreach (var (kind, bounds, available) in Buttons())
            _ribbon.Painter.DrawGalleryStripButton(context with { Bounds = bounds, Hot = _hotButton == kind, Pressed = _pressedButton == kind
                || kind == RibbonGalleryStripButton.Expand && _ribbon.Popups.IsMenuExpanded(Source) }, kind, available);
    }
    private IEnumerable<(RibbonGalleryStripButton Kind, Rectangle Bounds, bool Available)> Buttons()
    {
        yield return (RibbonGalleryStripButton.Up, _layout.Up, Interactive && _row > 0);
        yield return (RibbonGalleryStripButton.Down, _layout.Down, Interactive && _row < _layout.LastRow);
        yield return (RibbonGalleryStripButton.Expand, _layout.Expand, Source.EffectiveEnabled && !_ribbon.IsBackstageOpen);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e); _hotButton = Buttons().Where(b => b.Available && b.Bounds.Contains(e.Location)).Select(b => (RibbonGalleryStripButton?)b.Kind).FirstOrDefault();
        _hot = Interactive ? _layout.Tiles.FirstOrDefault(t => t.Tile.EffectiveEnabled && t.Bounds.Contains(e.Location))?.Tile : null;
        var pointer = MousePosition;
        if (Interactive && _lastPointer != pointer) { _lastPointer = pointer; _keyboardPreview = false; Source.Preview(_hot, this); }
        Invalidate();
    }
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e); _hot = null; _hotButton = null;
        if (!_keyboardPreview || _lastPointer != MousePosition) { _keyboardPreview = false; Source.CancelPreview(this); }
        Invalidate();
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || !_ribbon.Editors.CommitFocused()) return;
        _ribbon.SetKeyboardFocusCues(false); _ribbon.ExitKeyTips(false);
        _pressedButton = Buttons().Where(b => b.Available && b.Bounds.Contains(e.Location)).Select(b => (RibbonGalleryStripButton?)b.Kind).FirstOrDefault();
        _pressed = Interactive ? _layout.Tiles.FirstOrDefault(t => t.Tile.EffectiveEnabled && t.Bounds.Contains(e.Location))?.Tile : null;
        if (_pressed is not null) { _focused = _pressed; Focus(); }
        Capture = _pressed is not null || _pressedButton is not null; Invalidate();
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e); var tile = _pressed; var button = _pressedButton; _pressed = null; _pressedButton = null; Capture = false;
        if (e.Button != MouseButtons.Left) return;
        if (tile is not null && Interactive && _layout.Tiles.Any(t => ReferenceEquals(t.Tile, tile) && t.Bounds.Contains(e.Location))) CommitTile(tile);
        else if (button is { } kind && Buttons().Any(b => b.Kind == kind && b.Available && b.Bounds.Contains(e.Location)))
        { if (kind == RibbonGalleryStripButton.Expand) Source.ShowDropDown(); else ScrollRow(kind == RibbonGalleryStripButton.Up ? -1 : 1); }
        Invalidate();
    }
    protected override void OnMouseCaptureChanged(EventArgs e) { _pressed = null; _pressedButton = null; base.OnMouseCaptureChanged(e); Invalidate(); }
    private void CommitTile(RibbonGalleryTile tile)
    {
        Source.Commit(tile);
        if (ReferenceEquals(Source.SelectedItem, tile) && Parent is RibbonPopupSurface) _ribbon.Popups.CloseAll(true);
    }
    private void ScrollRow(int delta, bool keyboard = false)
    {
        int next = Math.Clamp(_row + delta, 0, _layout.LastRow);
        if (next == _row) return;
        Source.CancelPreview(this); _keyboardPreview = false; _hot = null; _lastPointer = null;
        _row = next; Rebuild();
        if (Focused) _focused = _layout.Tiles.FirstOrDefault(t => t.Tile.EffectiveEnabled)?.Tile;
        if (keyboard && _focused is not null) { _keyboardPreview = true; _lastPointer = MousePosition; Source.Preview(_focused, this); }
        Invalidate();
    }
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (Interactive && e.Delta != 0) { ScrollRow(e.Delta > 0 ? -1 : 1); if (e is HandledMouseEventArgs handled) handled.Handled = true; }
        else base.OnMouseWheel(e);
    }
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e); _ribbon.EditorFocused(Source);
        if (!_layout.Tiles.Any(t => ReferenceEquals(t.Tile, _focused))) _focused = _layout.Tiles.FirstOrDefault(t => t.Tile.EffectiveEnabled)?.Tile;
        Invalidate();
    }
    protected override void OnLostFocus(EventArgs e) { Source.CancelPreview(this); base.OnLostFocus(e); Invalidate(); }
    protected override bool ProcessCmdKey(ref Message msg, Keys keys)
    {
        if (!Interactive) return base.ProcessCmdKey(ref msg, keys);
        _ribbon.SetKeyboardFocusCues(true);
        if (keys is Keys.F4 or (Keys.Alt | Keys.Down)) { Source.CancelPreview(this); Source.ShowDropDown(); return true; }
        if (keys == Keys.Escape) { Source.CancelPreview(this); _ribbon.FocusElement(Source, enterEditor: false); return true; }
        if (keys is Keys.Tab or (Keys.Tab | Keys.Shift))
        {
            Source.CancelPreview(this);
            if (Parent is RibbonPopupSurface popup) popup.NavigateFromElement(Source, keys);
            else { _ribbon.FocusElement(Source, enterEditor: false); _ribbon.ProcessRibbonKey(keys); }
            return true;
        }
        if (keys is Keys.Enter or Keys.Space) { if (_focused is { } tile) CommitTile(tile); return true; }
        if (keys is Keys.PageUp or Keys.PageDown) { ScrollRow(keys == Keys.PageUp ? -1 : 1, true); return true; }
        if (keys is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End)
        {
            var tiles = Tiles(); int current = Array.IndexOf(tiles, _focused); int delta = keys == Keys.Left ? -1 : keys == Keys.Right ? 1 : keys == Keys.Up ? -Source.InRibbonColumnCount : Source.InRibbonColumnCount;
            int index = keys == Keys.Home ? 0 : keys == Keys.End ? tiles.Length - 1 : Math.Clamp(current + delta, 0, Math.Max(0, tiles.Length - 1));
            int direction = keys is Keys.Left or Keys.Up or Keys.Home ? -1 : 1;
            if (keys == Keys.Home) direction = 1;
            while (index >= 0 && index < tiles.Length && !tiles[index].EffectiveEnabled) index += direction;
            if (index >= 0 && index < tiles.Length)
            {
                _focused = tiles[index]; _row = index / Source.InRibbonColumnCount; Rebuild(); _hot = null;
                _keyboardPreview = true; _lastPointer = MousePosition; Source.Preview(_focused, this); Invalidate();
            }
            return true;
        }
        return base.ProcessCmdKey(ref msg, keys);
    }
    protected override void Dispose(bool disposing) { if (disposing) Source.CancelPreview(this); base.Dispose(disposing); }
}
