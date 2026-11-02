using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Built-in strips remain movable. Custom views belong to fixed ribbon/session
// slots; tiles, selection and application preview handlers stay on the source.
internal sealed class RibbonGalleryStripManager(RibbonControl ribbon) : IDisposable
{
    private readonly Dictionary<RibbonGalleryItem, RibbonGalleryStripSurface> _strips = [];
    private readonly Dictionary<RibbonGalleryItem, RibbonGalleryInlineSurface> _inline = [];
    private readonly Dictionary<(RibbonGalleryItem Source, RibbonPopupSurface Host), RibbonGalleryInlineSurface> _temporary = [];
    private bool _syncing, _disposed;
    private IEnumerable<RibbonGalleryInlineSurface> CustomViews => _temporary.Values.Concat(_inline.Values);
    internal RibbonGalleryStripSurface? SurfaceFor(RibbonGalleryItem source) => _strips.GetValueOrDefault(source);
    internal RibbonGalleryInlineSurface? InlineSurfaceFor(RibbonGalleryItem source) => _inline.GetValueOrDefault(source);
    internal RibbonGalleryInlineSurface? InlineSurfaceFor(RibbonGalleryItem source, RibbonPopupSurface host) => _temporary.GetValueOrDefault((source, host));
    internal RibbonGalleryInlineSurface? CustomInputOwner(Control? origin) => CustomViews.FirstOrDefault(view => view.OwnsInput(origin));
    private RibbonTarget? CurrentSlot(RibbonGalleryItem source, Control parent)
    {
        if (parent.IsDisposed || parent is RibbonPopupSurface popup && !ribbon.Popups.Surfaces.Contains(popup)) return null;
        var targets = parent is RibbonPopupSurface surface ? surface.Targets : ribbon.Snapshot.Targets;
        return targets.FirstOrDefault(target => ReferenceEquals(target.Element, source) && target.Size == RibbonItemSize.Large && !target.GalleryDropDown && !target.VisibleBounds.IsEmpty);
    }
    internal void Sync()
    {
        if (_syncing || _disposed || ribbon.IsInDesignMode) return;
        _syncing = true;
        try
        {
            var sources = ribbon.RetainedElements.OfType<RibbonGalleryItem>().Where(g => g.IsInRibbon && !g.IsDisposed).ToHashSet();
            foreach (var old in _strips.Keys.Except(sources).ToArray()) { _strips[old].Dispose(); _strips.Remove(old); }
            foreach (var old in _inline.Keys.Where(source => !sources.Contains(source) || !source.HasCustomInRibbonPresenter || !_inline[source].MatchesSelectors).ToArray())
            { var view = _inline[old]; _inline.Remove(old); view.Dispose(); }
            foreach (var key in _temporary.Keys.Where(key => !sources.Contains(key.Source) || !key.Source.HasCustomInRibbonPresenter || !_temporary[key].MatchesSelectors || !ribbon.Popups.Surfaces.Contains(key.Host)).ToArray())
            { var view = _temporary[key]; _temporary.Remove(key); view.Dispose(); }
            foreach (var source in sources)
            {
                if (source.HasCustomInRibbonPresenter)
                {
                    var target = CurrentSlot(source, ribbon);
                    bool shown = target is not null && ribbon.Visible && !ribbon.IsBackstageOpen;
                    if (shown && !_inline.ContainsKey(source))
                    {
                        var created = new RibbonGalleryInlineSurface(ribbon, source);
                        target = CurrentSlot(source, ribbon);
                        if (!Valid(source, created) || target is null) { created.Dispose(); continue; }
                        _inline[source] = created;
                    }
                    if (_inline.TryGetValue(source, out var permanent))
                    {
                        try { if (shown) permanent.Present(target!); else permanent.Park(); }
                        catch { _inline.Remove(source); permanent.Dispose(); throw; }
                        if (!Retained(source, permanent)) { _inline.Remove(source); permanent.Dispose(); }
                    }
                    foreach (var host in ribbon.Popups.Surfaces.ToArray())
                    {
                        var key = (source, host); target = CurrentSlot(source, host);
                        if (target is not null && !_temporary.ContainsKey(key))
                        {
                            var created = new RibbonGalleryInlineSurface(ribbon, source, host);
                            target = CurrentSlot(source, host);
                            if (!Valid(source, created) || target is null) { created.Dispose(); continue; }
                            _temporary[key] = created;
                        }
                        if (_temporary.TryGetValue(key, out var temporary))
                        {
                            try { if (target is not null) temporary.Present(target); else temporary.Park(); }
                            catch { _temporary.Remove(key); temporary.Dispose(); throw; }
                            if (!Retained(source, temporary) || !ribbon.Popups.Surfaces.Contains(host)) { _temporary.Remove(key); temporary.Dispose(); }
                        }
                    }
                    if (_strips.TryGetValue(source, out var oldStrip)) oldStrip.Park();
                }
                else
                {
                    // Preserve the built-in path's one movable strip per source.
                    Control parent = ribbon; var target = CurrentSlot(source, ribbon);
                    foreach (var host in ribbon.Popups.Surfaces)
                        if (CurrentSlot(source, host) is { } slot) { parent = host; target = slot; }
                    if (!_strips.TryGetValue(source, out var strip))
                    {
                        if (target is null) continue;
                        _strips[source] = strip = new(ribbon, source);
                    }
                    if (target is not null) strip.Present(parent, target); else strip.Park();
                }
            }
        }
        finally { _syncing = false; }
    }
    private bool Retained(RibbonGalleryItem source, RibbonGalleryInlineSurface view) => !source.IsDisposed && source.IsInRibbon && ReferenceEquals(source.Ribbon, ribbon) && view.MatchesSelectors;
    private bool Valid(RibbonGalleryItem source, RibbonGalleryInlineSurface view) => source.Visible && Retained(source, view);
    internal bool Focus(RibbonGalleryItem source)
    {
        if (ribbon.IsBackstageOpen) return false;
        Sync();
        if (ribbon.Popups.Top is { } host && _temporary.TryGetValue((source, host), out var temporary) && temporary.Visible) return temporary.FocusView();
        if (_inline.TryGetValue(source, out var custom) && custom.Visible) return custom.FocusView();
        return _strips.TryGetValue(source, out var strip) && strip.Visible && strip.Focus();
    }
    internal void Release(Control parent)
    {
        foreach (var key in _temporary.Keys.Where(key => ReferenceEquals(key.Host, parent)).ToArray())
        { var view = _temporary[key]; _temporary.Remove(key); view.Dispose(); }
        foreach (var strip in _strips.Values.Where(s => ReferenceEquals(s.Parent, parent)).ToArray()) strip.Park();
        foreach (var view in _inline.Values.Where(s => ReferenceEquals(s.Parent, parent)).ToArray()) view.Park();
    }
    internal void CancelPreviews()
    {
        foreach (var strip in _strips.Values.ToArray()) strip.Source.CancelPreview(strip);
        foreach (var view in CustomViews.ToArray()) view.CancelPreview();
    }
    internal void SetBackdropOpacity(byte alpha) { foreach (var view in _inline.Values.ToArray()) view.SetBackdropOpacity(alpha); }
    public void Dispose()
    {
        _disposed = true;
        foreach (var strip in _strips.Values.ToArray()) strip.Dispose(); _strips.Clear();
        foreach (var view in CustomViews.ToArray()) view.Dispose(); _temporary.Clear(); _inline.Clear();
    }
}

internal sealed class RibbonGalleryStripSurface : Control
{
    private readonly RibbonControl _ribbon;
    private RibbonBackdropBuffer? _backdropBuffer;
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
        base.OnPaint(e);
        if (_ribbon.HasWindowBackdrop)
        {
            try { (_backdropBuffer ??= new()).Paint(e.Graphics, ClientSize, 0, _ribbon.Theme.Surface, PaintContent, opaque: true); return; }
            catch (Exception error) when (error is System.ComponentModel.Win32Exception or System.Runtime.InteropServices.ExternalException)
            { _ribbon.WindowHost!.BackdropPaintFailed(error); }
        }
        _backdropBuffer?.Dispose(); _backdropBuffer = null;
        PaintContent(e.Graphics);
    }
    private void PaintContent(Graphics graphics)
    {
        var context = new RibbonPaintContext(graphics, ClientRectangle, _ribbon.Theme, Font, _ribbon.DeviceDpi) { Density = _ribbon.Density };
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
    protected override void Dispose(bool disposing) { if (disposing) { _backdropBuffer?.Dispose(); _backdropBuffer = null; Source.CancelPreview(this); } base.Dispose(disposing); }
}
