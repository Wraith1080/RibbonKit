using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// A permanent ribbon slot owns this surface and its application control. The
// expanded popup owns another view; this control is never moved into a flyout.
internal sealed class RibbonGalleryInlineSurface : Control, IRibbonGalleryPresenterHost
{
    private readonly RibbonControl _ribbon;
    internal RibbonGalleryItem Source { get; }
    private readonly Type? _type;
    private readonly Func<Control>? _factory;
    private readonly GalleryViewport _viewport = new() { TabStop = false };
    private readonly UserControl _control;
    private readonly IRibbonGalleryPresenter _presenter;
    private RibbonGalleryPresenterContext? _context;
    private RibbonGalleryStripLayout _layout = null!;
    private RibbonBackdropBuffer? _backdrop;
    private RibbonGalleryTile? _focused, _selection, _pressed;
    private int _selectionIndex = -1;
    private bool _selectionAvailable;
    private RibbonGalleryStripButton? _hotButton, _pressedButton;
    private bool _presenting, _synchronizing, _focusing, _committing, _shown, _inputEnabled, _selectionKnown, _keyboardPreview;
    private Point? _lastPointer;
    private Rectangle _clip;
    internal UserControl PresenterControl => _control;
    internal bool MatchesSelectors => _type == Source.InRibbonPresenterType && _factory == Source.InRibbonPresenterFactory;
    private bool Interactive => _shown && Visible && _ribbon.Visible && _ribbon.Enabled && Source.Visible && Source.EffectiveEnabled
        && !Source.IsDisposed && ReferenceEquals(Source.Ribbon, _ribbon) && !_ribbon.IsBackstageOpen && !_ribbon.Popups.HasOpen;

    internal RibbonGalleryInlineSurface(RibbonControl ribbon, RibbonGalleryItem source)
    {
        _ribbon = ribbon; Source = source; _type = source.InRibbonPresenterType; _factory = source.InRibbonPresenterFactory;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        TabStop = false; CausesValidation = false; AccessibleName = source.Text + " gallery"; AccessibleRole = AccessibleRole.List;
        _control = RibbonGalleryPresenterOwnership.Accept(source.CreateInRibbonPresenter());
        _presenter = (IRibbonGalleryPresenter)_control;
        try
        {
            Controls.Add(_viewport); _control.Dock = DockStyle.Fill; _viewport.Controls.Add(_control);
            _control.MouseMove += PointerMove; _control.MouseLeave += PointerLeave;
            _control.MouseDown += PointerDown; _control.MouseUp += PointerUp;
            _control.MouseCaptureChanged += PointerCaptureChanged; _control.MouseWheel += PointerWheel;
            _control.GotFocus += ViewFocused; _control.Leave += ViewLeft;
        }
        catch { _control.Dispose(); _viewport.Dispose(); throw; }
    }

    internal void Present(RibbonTarget target)
    {
        if (IsDisposed || _presenting) return;
        _presenting = true;
        try
        {
            _shown = true;
            if (Parent is null) _ribbon.Controls.Add(this);
            Font = _ribbon.Font; BackColor = _ribbon.Theme.Surface; Enabled = Source.EffectiveEnabled;
            Bounds = target.Bounds;
            var clip = target.VisibleBounds; clip.Offset(-Left, -Top);
            if (_clip != clip) { _clip = clip; var old = Region; Region = new Region(clip); old?.Dispose(); }
            _layout = RibbonGalleryStripLayout.Build(Source, ClientSize, _ribbon.DeviceDpi, 0);
            _viewport.Bounds = _layout.Viewport; _viewport.BackColor = _ribbon.Theme.Surface;
            _control.Font = Font; _control.BackColor = BackColor;
            Visible = true;
            bool interactive = Interactive;
            if (_context is null || _inputEnabled != interactive || !ReferenceEquals(_context.Theme, _ribbon.Theme)
                || !ReferenceEquals(_context.Renderer, _ribbon.Painter) || !ReferenceEquals(_context.Font, Font)
                || _context.Density != _ribbon.Density || _context.Dpi != _ribbon.DeviceDpi)
            {
                DetachContext();
                if (IsDisposed || Source.IsDisposed || !MatchesSelectors) return;
                _inputEnabled = interactive;
                _context = new(this, Source, _ribbon, RibbonGalleryPresentation.InRibbon);
            }
            _context.Viewport = _viewport.ClientSize;
            SynchronizePresenter();
            if (_inputEnabled && !Interactive) DetachContext();
            SetBackdropOpacity(_ribbon.BackdropBodyMasked ? (byte)1 : (byte)255);
            BringToFront(); Invalidate();
        }
        finally { _presenting = false; }
    }

    private void SynchronizePresenter()
    {
        if (_synchronizing || IsDisposed || _context is not { } context) return;
        _synchronizing = true;
        try
        {
            if (_focused is not null && !Source.Available(_focused)) _focused = null;
            _focused ??= Source.SelectedItem is { } selected && Source.Available(selected) ? selected : Source.Items.OfType<RibbonGalleryTile>().FirstOrDefault(Source.Available);
            context.FocusedItem = _focused;
            _presenter.Synchronize(context);
            if (IsDisposed || !ReferenceEquals(context, _context)) return;
            int selectedIndex = Source.Items.OfType<RibbonGalleryTile>().Where(tile => tile.Visible).ToList().IndexOf(Source.SelectedItem!);
            bool selectedAvailable = Source.SelectedItem is { } chosen && Source.Available(chosen);
            if (!_selectionKnown || !ReferenceEquals(_selection, Source.SelectedItem) || _selectionIndex != selectedIndex || _selectionAvailable != selectedAvailable)
            {
                _selectionKnown = true; _selection = Source.SelectedItem; _selectionIndex = selectedIndex; _selectionAvailable = selectedAvailable;
                if (_selection is { } tile && Source.Available(tile)) _presenter.Reveal(tile);
            }
        }
        finally { _synchronizing = false; }
    }

    internal void Park()
    {
        _shown = false; DetachContext();
        if (!IsDisposed) Visible = false;
    }
    private void DetachContext()
    {
        var old = _context; _context = null; old?.Detach(); _inputEnabled = false;
        _pressed = null; _pressedButton = _hotButton = null; _lastPointer = null; _keyboardPreview = false; Capture = false;
        if (old is not null) Source.CancelPreview(old);
    }
    internal void CancelPreview() { if (_context is { } context) Source.CancelPreview(context); }
    internal void SetBackdropOpacity(byte alpha) => _viewport.SetOpacity(alpha);
    private bool IsCurrent(RibbonGalleryPresenterContext context) => ReferenceEquals(_context, context) && !IsDisposed
        && !_control.IsDisposed && ReferenceEquals(_control.Parent, _viewport) && _inputEnabled && MatchesSelectors && Interactive;
    bool IRibbonGalleryPresenterHost.IsCurrentPresenter(RibbonGalleryPresenterContext context) => IsCurrent(context);
    bool IRibbonGalleryPresenterHost.PresenterPreview(RibbonGalleryPresenterContext context, RibbonGalleryTile? tile)
    {
        _ribbon.VerifyAccess();
        if (_synchronizing || !IsCurrent(context) || tile is not null && !Source.Available(tile)) return false;
        Source.Preview(tile, context); return IsCurrent(context);
    }
    bool IRibbonGalleryPresenterHost.PresenterFocus(RibbonGalleryPresenterContext context, RibbonGalleryTile tile)
    {
        _ribbon.VerifyAccess();
        if (_synchronizing || _focusing || !IsCurrent(context) || !Source.Available(tile)) return false;
        _focusing = true;
        try
        {
            bool libraryEditor = _ribbon.Editors.ContainsFocus;
            if (libraryEditor && !_ribbon.Editors.CommitFocused() || !IsCurrent(context) || !Source.Available(tile)) return false;
            var previous = _focused; _focused = tile; context.FocusedItem = tile;
            bool causesValidation = _control.CausesValidation, focused;
            try
            {
                // The library just validated its editor. Suppress only that
                // duplicate native transition; application editors otherwise
                // keep ordinary WinForms validation.
                if (libraryEditor) _control.CausesValidation = false;
                focused = _control.Focus() && _control.ContainsFocus;
            }
            finally { if (!_control.IsDisposed) _control.CausesValidation = causesValidation; }
            if (!IsCurrent(context)) return false;
            if (!focused) { _focused = previous; context.FocusedItem = previous; SynchronizePresenter(); return false; }
            _presenter.Reveal(tile);
            if (!IsCurrent(context)) return false;
            SynchronizePresenter(); _ribbon.AccessibilityFocusChanged(Source);
            return IsCurrent(context);
        }
        finally { _focusing = false; }
    }
    bool IRibbonGalleryPresenterHost.PresenterCommit(RibbonGalleryPresenterContext context, RibbonGalleryTile tile)
    {
        _ribbon.VerifyAccess();
        if (_synchronizing || _focusing || _committing || !IsCurrent(context) || !Source.Available(tile)) return false;
        _committing = true;
        try
        {
            if (!_ribbon.Editors.CommitFocused() || !IsCurrent(context) || !Source.Available(tile)) return false;
            return _ribbon.ActivateGalleryTile(tile, this, editorValidated: true);
        }
        finally { _committing = false; }
    }
    void IRibbonGalleryPresenterHost.PresenterViewChanged(RibbonGalleryPresenterContext context)
    {
        _ribbon.VerifyAccess();
        if (_synchronizing || !IsCurrent(context)) return;
        Invalidate(); _ribbon.AccessibilityChanged();
    }
    internal bool OwnsInput(Control? origin) => origin is not null && (ReferenceEquals(origin, this) || Contains(origin));
    internal bool FocusView()
    {
        if (_context is not { IsActive: true }) return false;
        // Returning from a cancelled expansion must not Reveal an older focus
        // and overwrite the independently browsed row.
        return _control.Focus();
    }
    internal bool ProcessKey(Keys key)
    {
        if (_context is not { IsActive: true } context) return false;
        _ribbon.SetKeyboardFocusCues(true);
        if (key is Keys.F4 or (Keys.Alt | Keys.Down)) { Expand(); return true; }
        if (key is Keys.Escape or Keys.Tab or (Keys.Tab | Keys.Shift))
        {
            context.Preview(null); _ribbon.FocusElement(Source, enterEditor: false);
            if (key != Keys.Escape) _ribbon.ProcessRibbonKey(key);
            return true;
        }
        if (key is Keys.Enter or Keys.Space) { if (_focused is { } tile) context.Commit(tile); return true; }
        if (key is not (Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown)) return false;
        var next = _presenter.Navigate(_focused, key);
        if (next is not null && context.FocusItem(next))
        { _keyboardPreview = true; _lastPointer = MousePosition; context.Preview(next); }
        return true;
    }
    internal void Scroll(RibbonGalleryScroll amount)
    {
        if (_context is not { IsActive: true } context) return;
        context.Preview(null); _keyboardPreview = false; _lastPointer = null;
        _presenter.ScrollView(amount);
        if (IsCurrent(context) && _control.ContainsFocus)
            _focused = Source.Items.OfType<RibbonGalleryTile>().FirstOrDefault(tile => Source.Available(tile)
                && _presenter.GetItemBounds(tile).IntersectsWith(new Rectangle(Point.Empty, context.Viewport)));
        SynchronizePresenter(); Invalidate(); _ribbon.AccessibilityChanged();
    }
    private void Expand()
    {
        if (_context is not { IsActive: true } context) return;
        context.Preview(null); Source.ShowDropDown();
    }
    private RibbonGalleryTile? Hit(Point point) => _context is { IsActive: true } && new Rectangle(Point.Empty, _viewport.ClientSize).Contains(point)
        && _presenter.HitTest(point) is { } tile && Source.Available(tile) ? tile : null;
    private void PointerMove(object? sender, MouseEventArgs e)
    {
        if (_lastPointer == MousePosition || _context is not { IsActive: true } context) return;
        _lastPointer = MousePosition; _keyboardPreview = false; _ribbon.SetKeyboardFocusCues(false); context.Preview(Hit(e.Location));
    }
    private void PointerLeave(object? sender, EventArgs e)
    { if (!_keyboardPreview || _lastPointer != MousePosition) _context?.Preview(null); }
    private void PointerDown(object? sender, MouseEventArgs e)
    {
        // UserControl attempts native focus before raising MouseDown. A failed
        // attempt has already run validation; do not request it a second time.
        _pressed = e.Button == MouseButtons.Left && _control.ContainsFocus ? Hit(e.Location) : null;
        if (_pressed is { } tile && _context?.FocusItem(tile) != true) _pressed = null;
    }
    private void PointerUp(object? sender, MouseEventArgs e)
    {
        var tile = _pressed; _pressed = null;
        if (e.Button == MouseButtons.Left && tile is not null && ReferenceEquals(tile, Hit(e.Location))) _context?.Commit(tile);
    }
    private void PointerCaptureChanged(object? sender, EventArgs e) { if (sender is Control { Capture: false }) _pressed = null; }
    private void PointerWheel(object? sender, MouseEventArgs e)
    {
        if (_context is not { IsActive: true } || e.Delta == 0) return;
        Scroll(e.Delta < 0 ? RibbonGalleryScroll.RowForward : RibbonGalleryScroll.RowBackward);
        if (e is HandledMouseEventArgs handled) handled.Handled = true;
    }
    private void ViewFocused(object? sender, EventArgs e) { _ribbon.EditorFocused(Source); SynchronizePresenter(); }
    private void ViewLeft(object? sender, EventArgs e) => CancelPreview();
    protected override bool ProcessCmdKey(ref Message msg, Keys key) => ProcessKey(key) || base.ProcessCmdKey(ref msg, key);
    private IEnumerable<(RibbonGalleryStripButton Kind, Rectangle Bounds, bool Available)> Buttons()
    {
        if (_layout is null) yield break;
        yield return (RibbonGalleryStripButton.Up, _layout.Up, Interactive && _presenter.CanScrollBackward);
        yield return (RibbonGalleryStripButton.Down, _layout.Down, Interactive && _presenter.CanScrollForward);
        yield return (RibbonGalleryStripButton.Expand, _layout.Expand, Interactive);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e); _hotButton = Buttons().Where(b => b.Available && b.Bounds.Contains(e.Location)).Select(b => (RibbonGalleryStripButton?)b.Kind).FirstOrDefault(); Invalidate();
    }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hotButton = null; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _pressedButton = e.Button == MouseButtons.Left ? Buttons().Where(b => b.Available && b.Bounds.Contains(e.Location)).Select(b => (RibbonGalleryStripButton?)b.Kind).FirstOrDefault() : null;
        Capture = _pressedButton is not null; Invalidate();
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e); var pressed = _pressedButton; _pressedButton = null; Capture = false;
        if (e.Button == MouseButtons.Left && pressed is { } kind && Buttons().Any(b => b.Kind == kind && b.Available && b.Bounds.Contains(e.Location)))
        { if (kind == RibbonGalleryStripButton.Expand) Expand(); else Scroll(kind == RibbonGalleryStripButton.Up ? RibbonGalleryScroll.RowBackward : RibbonGalleryScroll.RowForward); }
        Invalidate();
    }
    protected override void OnMouseCaptureChanged(EventArgs e) { _pressedButton = null; base.OnMouseCaptureChanged(e); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_ribbon.HasWindowBackdrop)
        {
            try { (_backdrop ??= new()).Paint(e.Graphics, ClientSize, 0, _ribbon.Theme.Surface, PaintContent, opaque: true); return; }
            catch (Exception error) when (error is System.ComponentModel.Win32Exception or System.Runtime.InteropServices.ExternalException)
            { _ribbon.WindowHost!.BackdropPaintFailed(error); }
        }
        _backdrop?.Dispose(); _backdrop = null; PaintContent(e.Graphics);
    }
    private void PaintContent(Graphics graphics)
    {
        var context = new RibbonPaintContext(graphics, ClientRectangle, _ribbon.Theme, Font, _ribbon.DeviceDpi) { Density = _ribbon.Density };
        _ribbon.Painter.DrawGalleryStripFrame(context);
        foreach (var (kind, bounds, available) in Buttons())
            _ribbon.Painter.DrawGalleryStripButton(context with { Bounds = bounds, Hot = _hotButton == kind, Pressed = _pressedButton == kind
                || kind == RibbonGalleryStripButton.Expand && _ribbon.Popups.IsMenuExpanded(Source) }, kind, available);
    }
    protected override AccessibleObject CreateAccessibilityInstance() => new GalleryAccessibleObject(this);
    private sealed class GalleryAccessibleObject(RibbonGalleryInlineSurface owner) : ControlAccessibleObject(owner)
    {
        private readonly Dictionary<RibbonGalleryTile, AccessibleObject> _tiles = [];
        private RibbonGalleryTile[] Tiles
        {
            get
            {
                var tiles = owner.Source.Items.OfType<RibbonGalleryTile>().Where(tile => tile.Visible && !tile.IsDisposed).ToArray();
                foreach (var old in _tiles.Keys.Except(tiles).ToArray()) _tiles.Remove(old);
                return tiles;
            }
        }
        public override AccessibleRole Role => AccessibleRole.List;
        public override string? Name { get => owner.Source.Text; set => base.Name = value; }
        public override int GetChildCount() => owner.IsDisposed ? 0 : Tiles.Length + 3;
        public override AccessibleObject? GetChild(int index)
        {
            if (owner.IsDisposed || index < 0) return null;
            var tiles = Tiles;
            if (index < tiles.Length)
            {
                var tile = tiles[index];
                if (!_tiles.TryGetValue(tile, out var accessible)) _tiles[tile] = accessible = new TileAccessibleObject(owner, this, tile);
                return accessible;
            }
            return index < tiles.Length + 3 ? new UtilityAccessibleObject(owner, this, (RibbonGalleryStripButton)(index - tiles.Length)) : null;
        }
        public override AccessibleObject? GetFocused() => owner._control.ContainsFocus && owner._focused is { } tile ? GetChild(Array.IndexOf(Tiles, tile)) : null;
        public override AccessibleObject? GetSelected() => owner.Source.SelectedItem is { } tile ? GetChild(Array.IndexOf(Tiles, tile)) : null;
    }
    private sealed class TileAccessibleObject(RibbonGalleryInlineSurface owner, AccessibleObject parent, RibbonGalleryTile tile) : AccessibleObject
    {
        public override AccessibleObject Parent => parent;
        public override string? Name { get => tile.AccessibleName.Length > 0 ? tile.AccessibleName : tile.Text; set { } }
        public override string? Description => tile.AccessibleDescription;
        public override AccessibleRole Role => AccessibleRole.ListItem;
        public override string DefaultAction => "Select";
        public override Rectangle Bounds
        {
            get
            {
                if (owner.IsDisposed || !owner.Visible || tile.IsDisposed || !tile.Visible || !owner.Source.Items.Contains(tile) || owner._context is null) return Rectangle.Empty;
                var bounds = owner._presenter.GetItemBounds(tile);
                if (bounds.IsEmpty) return Rectangle.Empty;
                return Rectangle.Intersect(owner._control.RectangleToScreen(bounds), Rectangle.Intersect(owner._viewport.RectangleToScreen(owner._viewport.ClientRectangle), owner.RectangleToScreen(owner._clip)));
            }
        }
        public override AccessibleStates State => AccessibleStates.Selectable | AccessibleStates.Focusable
            | (Bounds.IsEmpty ? AccessibleStates.Offscreen : 0) | (!owner.Interactive || !owner.Source.Available(tile) ? AccessibleStates.Unavailable : 0)
            | (ReferenceEquals(owner.Source.SelectedItem, tile) ? AccessibleStates.Selected : 0)
            | (owner._control.ContainsFocus && ReferenceEquals(owner._focused, tile) ? AccessibleStates.Focused : 0);
        public override void DoDefaultAction() => owner._context?.Commit(tile);
        public override void Select(AccessibleSelection flags) { if ((flags & AccessibleSelection.TakeFocus) != 0) owner._context?.FocusItem(tile); }
    }
    private sealed class UtilityAccessibleObject(RibbonGalleryInlineSurface owner, AccessibleObject parent, RibbonGalleryStripButton kind) : AccessibleObject
    {
        public override AccessibleObject Parent => parent;
        public override AccessibleRole Role => AccessibleRole.PushButton;
        public override string? Name { get => kind == RibbonGalleryStripButton.Up ? "Previous gallery row" : kind == RibbonGalleryStripButton.Down ? "Next gallery row" : "Expand gallery"; set { } }
        public override Rectangle Bounds => owner.IsDisposed || !owner.Visible ? Rectangle.Empty
            : Rectangle.Intersect(owner.RectangleToScreen(owner.Buttons().First(b => b.Kind == kind).Bounds), owner.RectangleToScreen(owner._clip));
        public override AccessibleStates State => owner.IsDisposed || !owner.Visible ? AccessibleStates.Offscreen | AccessibleStates.Unavailable
            : owner.Buttons().First(b => b.Kind == kind).Available ? AccessibleStates.None : AccessibleStates.Unavailable;
        public override void DoDefaultAction()
        { if (kind == RibbonGalleryStripButton.Expand) owner.Expand(); else owner.Scroll(kind == RibbonGalleryStripButton.Up ? RibbonGalleryScroll.RowBackward : RibbonGalleryScroll.RowForward); }
    }
    private sealed class GalleryViewport : RibbonBackdropPanel
    {
        private int _alpha = -1;
        protected override CreateParams CreateParams
        {
            get { var value = base.CreateParams; value.ExStyle |= 0x02000000; return value; } // Composite native children as one opaque subtree.
        }
        internal void SetOpacity(byte alpha)
        {
            if (!IsHandleCreated || IsDisposed) return;
            EnsureOpaque();
            if (FindForm() is not RibbonForm { HasWindowBackdrop: true } host) { _alpha = -1; return; }
            if (_alpha == alpha) return;
            if (!SetLayeredWindowAttributes(Handle, 0, alpha, 2)) host.BackdropPaintFailed(new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error()));
            else _alpha = alpha;
        }
        protected override void OnHandleDestroyed(EventArgs e) { _alpha = -1; base.OnHandleDestroyed(e); }
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(nint window, uint color, byte alpha, uint flags);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _shown = false; DetachContext(); _backdrop?.Dispose(); _backdrop = null;
            _control.MouseMove -= PointerMove; _control.MouseLeave -= PointerLeave; _control.MouseDown -= PointerDown; _control.MouseUp -= PointerUp;
            _control.MouseCaptureChanged -= PointerCaptureChanged; _control.MouseWheel -= PointerWheel; _control.GotFocus -= ViewFocused; _control.Leave -= ViewLeft;
        }
        base.Dispose(disposing);
    }
}
