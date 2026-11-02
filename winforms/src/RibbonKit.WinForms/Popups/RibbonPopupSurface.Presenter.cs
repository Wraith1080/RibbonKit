using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed partial class RibbonPopupSurface : IRibbonGalleryPresenterHost
{
    bool IRibbonGalleryPresenterHost.IsCurrentPresenter(RibbonGalleryPresenterContext context) => IsCurrentPresenter(context);
    bool IRibbonGalleryPresenterHost.PresenterPreview(RibbonGalleryPresenterContext context, RibbonGalleryTile? item) => PresenterPreview(context, item);
    bool IRibbonGalleryPresenterHost.PresenterFocus(RibbonGalleryPresenterContext context, RibbonGalleryTile item) => PresenterFocus(context, item);
    bool IRibbonGalleryPresenterHost.PresenterCommit(RibbonGalleryPresenterContext context, RibbonGalleryTile item) => PresenterCommit(context, item);
    void IRibbonGalleryPresenterHost.PresenterViewChanged(RibbonGalleryPresenterContext context) => PresenterViewChanged(context);
    private IRibbonGalleryPresenter? _galleryPresenter;
    private Control? _galleryPresenterControl;
    private Panel? _galleryPresenterViewport;
    private PresenterScrollButton? _presenterBack, _presenterNext;
    private RibbonGalleryPresenterContext? _galleryContext;
    private RibbonGalleryTile? _presentedSelection;
    private RibbonGalleryTile? _presenterPressed;
    private bool _presenterSynchronizing;
    internal bool HasGalleryPresenter => _galleryPresenter is not null;

    private Size InitializeGalleryPresenter(RibbonGalleryItem gallery, Size maximum)
    {
        var control = RibbonGalleryPresenterOwnership.Accept(gallery.CreateDropDownPresenter());
        var presenter = (IRibbonGalleryPresenter)control;
        _galleryPresenterControl = control; _galleryPresenter = presenter;
        try
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, _ribbon.DeviceDpi);
            int footer = S(24), padding = 2 * _contentPadding;
            var limit = new Size(Math.Max(1, maximum.Width - S(24) - padding), Math.Max(1, maximum.Height - S(32) - footer - padding));
            _galleryContext = new(this, gallery, _ribbon) { Viewport = limit };
            control.Font = Font; control.BackColor = _ribbon.Theme.Surface;
            presenter.Synchronize(_galleryContext);
            var preferred = control.GetPreferredSize(limit);
            var viewport = new Size(Math.Clamp(preferred.Width, 1, limit.Width), Math.Clamp(preferred.Height, 1, limit.Height));
            _galleryContext.Viewport = viewport;
            _galleryPresenterViewport = new Panel { Bounds = new(_contentPadding, _contentPadding, viewport.Width, viewport.Height), BackColor = _ribbon.Theme.Surface, TabStop = false };
            Controls.Add(_galleryPresenterViewport);
            control.Dock = DockStyle.Fill; _galleryPresenterViewport.Controls.Add(control);
            control.MouseMove += PresenterMouseMove; control.MouseLeave += PresenterMouseLeave; control.MouseDown += PresenterMouseDown; control.MouseUp += PresenterMouseUp;
            int buttonWidth = Math.Max(1, Math.Min(S(24), viewport.Width / 2));
            _presenterBack = new(this, false) { Bounds = new(_contentPadding + Math.Max(0, viewport.Width - 2 * buttonWidth), _contentPadding + viewport.Height, buttonWidth, footer) };
            _presenterNext = new(this, true) { Bounds = new(_presenterBack.Right, _presenterBack.Top, buttonWidth, footer) };
            Controls.Add(_presenterBack); Controls.Add(_presenterNext);
            SynchronizePresenter();
            AutoScroll = false;
            return new(viewport.Width + padding, viewport.Height + footer + padding);
        }
        catch
        {
            DetachPresenter(); control.Dispose(); _galleryPresenterViewport?.Dispose();
            _presenterBack?.Dispose(); _presenterNext?.Dispose(); throw;
        }
    }
    internal bool IsCurrentPresenter(RibbonGalleryPresenterContext context) => ReferenceEquals(_galleryContext, context)
        && !IsDisposed && _galleryPresenterControl is { IsDisposed: false }
        && GallerySource is { IsDisposed: false, Visible: true, EffectiveEnabled: true } gallery
        && ReferenceEquals(gallery.Ribbon, _ribbon) && _ribbon.Popups.Surfaces.Contains(this);
    internal bool PresenterPreview(RibbonGalleryPresenterContext context, RibbonGalleryTile? item)
    {
        _ribbon.VerifyAccess();
        if (_presenterSynchronizing || !IsCurrentPresenter(context) || item is not null && !context.Gallery.Available(item)) return false;
        context.Gallery.Preview(item, this); return true;
    }
    internal bool PresenterCommit(RibbonGalleryPresenterContext context, RibbonGalleryTile item)
    {
        _ribbon.VerifyAccess();
        if (_presenterSynchronizing || !IsCurrentPresenter(context) || !context.Gallery.Available(item) || !_ribbon.Editors.CommitFocused()
            || !IsCurrentPresenter(context) || !context.Gallery.Available(item)) return false;
        // Use the same accepted activation and popup-dismissal route as built-in
        // tiles. In particular, committing the selected tile still commits once.
        return _ribbon.ActivateGalleryTile(item, this, editorValidated: true);
    }
    internal bool PresenterFocus(RibbonGalleryPresenterContext context, RibbonGalleryTile item)
    {
        _ribbon.VerifyAccess();
        if (_presenterSynchronizing || !IsCurrentPresenter(context) || !context.Gallery.Available(item)) return false;
        FocusPresenterItem(item); return IsCurrentPresenter(context);
    }
    internal void PresenterViewChanged(RibbonGalleryPresenterContext context)
    {
        _ribbon.VerifyAccess();
        if (_presenterSynchronizing || !IsCurrentPresenter(context)) return;
        UpdatePresenterScrollButtons(); Invalidate(); _ribbon.AccessibilityChanged();
    }
    private IReadOnlyList<RibbonTarget> PresenterTargets()
    {
        if (_galleryPresenter is not { } presenter || _galleryContext is not { } context || _galleryPresenterViewport is not { } viewport || _galleryPresenterControl is not { } control) return [];
        var origin = new Point(viewport.Left + control.Left, viewport.Top + control.Top);
        return context.Gallery.Items.OfType<RibbonGalleryTile>().Where(tile => tile.Visible).Select(tile =>
        {
            var bounds = presenter.GetItemBounds(tile);
            if (!bounds.IsEmpty) bounds.Offset(origin);
            return new RibbonTarget(TargetKind.Item, tile, bounds, Rectangle.Intersect(viewport.Bounds, ClientRectangle));
        }).ToArray();
    }
    internal void SynchronizePresenter()
    {
        if (_presenterSynchronizing || _galleryPresenter is not { } presenter || _galleryContext is not { } context) return;
        _presenterSynchronizing = true;
        try
        {
            if (context.FocusedItem is { } focus && !context.Gallery.Available(focus)) { context.FocusedItem = null; _focused = null; }
            presenter.Synchronize(context);
            if (!ReferenceEquals(_presentedSelection, context.Gallery.SelectedItem))
            {
                _presentedSelection = context.Gallery.SelectedItem;
                if (_presentedSelection is { } selected && context.Gallery.Available(selected)) presenter.Reveal(selected);
            }
            UpdatePresenterScrollButtons();
        }
        finally { _presenterSynchronizing = false; }
    }
    private void UpdatePresenterScrollButtons()
    {
        if (_galleryPresenter is not { } presenter) return;
        if (_presenterBack is { } back) { back.Enabled = presenter.CanScrollBackward; back.Invalidate(); }
        if (_presenterNext is { } next) { next.Enabled = presenter.CanScrollForward; next.Invalidate(); }
    }
    private void FocusPresenterItem(RibbonGalleryTile? tile)
    {
        if (_galleryPresenter is not { } presenter || _galleryContext is not { } context || _galleryPresenterControl is not { } control) return;
        if (tile is not null && !context.Gallery.Available(tile)) return;
        context.FocusedItem = tile; _focused = tile;
        if (tile is not null) presenter.Reveal(tile);
        control.Focus(); SynchronizePresenter();
        if (tile is not null) _ribbon.AccessibilityFocusChanged(tile);
    }
    internal bool OwnsPresenterInput(Control? origin) => _galleryPresenterControl is { } control && origin is not null
        && (ReferenceEquals(origin, control) || control.Contains(origin) || ReferenceEquals(origin, _presenterBack) || ReferenceEquals(origin, _presenterNext));
    internal bool ProcessPresenterKey(Keys keys)
    {
        if (_galleryPresenter is not { } presenter || _galleryContext is not { IsActive: true } context) return false;
        if (keys is Keys.Enter or Keys.Space)
        { if (context.FocusedItem is { } tile) context.Commit(tile); return true; }
        if (keys is not (Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown or Keys.Tab or (Keys.Tab | Keys.Shift))) return false;
        _ribbon.SetKeyboardFocusCues(true);
        _galleryKeyboardPreview = true; _galleryLastPointer = MousePosition;
        var next = presenter.Navigate(context.FocusedItem, keys);
        if (next is not null && context.Gallery.Available(next))
        { FocusPresenterItem(next); context.Preview(next); }
        return true;
    }
    internal void ScrollPresenter(RibbonGalleryScroll amount)
    {
        if (_galleryPresenter is not { } presenter || _galleryContext is not { IsActive: true } context) return;
        context.Preview(null); presenter.ScrollView(amount); UpdatePresenterScrollButtons();
        _ribbon.AccessibilityChanged();
    }
    private void PresenterMouseMove(object? sender, MouseEventArgs e)
    {
        if (_galleryPresenter is not { } presenter || _galleryContext is not { IsActive: true } || _ribbon.Popups.IsOpening(this)
            || _galleryLastPointer == MousePosition) return;
        _ribbon.SetKeyboardFocusCues(false); PreviewGalleryPointer(presenter.HitTest(e.Location));
    }
    private void PresenterMouseLeave(object? sender, EventArgs e)
    { if (!_galleryKeyboardPreview || _galleryLastPointer != MousePosition) _galleryContext?.Preview(null); }
    private void PresenterMouseDown(object? sender, MouseEventArgs e)
    {
        _presenterPressed = e.Button == MouseButtons.Left ? _galleryPresenter?.HitTest(e.Location) : null;
        if (_presenterPressed is { } tile) FocusPresenterItem(tile);
    }
    private void PresenterMouseUp(object? sender, MouseEventArgs e)
    {
        var pressed = _presenterPressed; _presenterPressed = null;
        if (e.Button == MouseButtons.Left && pressed is not null && ReferenceEquals(pressed, _galleryPresenter?.HitTest(e.Location))) _galleryContext?.Commit(pressed);
    }
    private void DetachPresenter()
    {
        var context = _galleryContext; _galleryContext = null; context?.Detach();
        if (_galleryPresenterControl is { } control)
        { control.MouseMove -= PresenterMouseMove; control.MouseLeave -= PresenterMouseLeave; control.MouseDown -= PresenterMouseDown; control.MouseUp -= PresenterMouseUp; }
        _galleryPresenter = null; _galleryPresenterControl = null;
        GallerySource?.CancelPreview(this);
    }
    private sealed class PresenterScrollButton : Button
    {
        private readonly RibbonPopupSurface _surface; private readonly bool _forward;
        private bool _hot, _pressed;
        internal PresenterScrollButton(RibbonPopupSurface surface, bool forward)
        {
            _surface = surface; _forward = forward; TabStop = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; UseVisualStyleBackColor = false;
            AccessibleName = forward ? "Next gallery row" : "Previous gallery row";
            Click += (_, _) => { surface.ScrollPresenter(forward ? RibbonGalleryScroll.RowForward : RibbonGalleryScroll.RowBackward); surface._galleryPresenterControl?.Focus(); };
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            using var background = new SolidBrush(_surface._ribbon.Theme.Surface); e.Graphics.FillRectangle(background, ClientRectangle);
            _surface._ribbon.Painter.DrawGalleryStripButton(
                new(e.Graphics, ClientRectangle, _surface._ribbon.Theme, Font, _surface._ribbon.DeviceDpi, Hot: _hot, Pressed: _pressed) { Density = _surface._ribbon.Density },
                _forward ? RibbonGalleryStripButton.Down : RibbonGalleryStripButton.Up, Enabled);
        }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); _pressed = true; Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _pressed = false; Invalidate(); }
        protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); _pressed = false; Invalidate(); }
    }
}
