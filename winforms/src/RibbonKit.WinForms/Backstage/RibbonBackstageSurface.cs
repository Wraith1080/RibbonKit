using RibbonKit.WinForms.Layout;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

// A child of an ordinary Form, with one persistent native parent per borrowed page.
// No document Controls are moved, disabled or disposed to produce the overlay.
internal sealed class RibbonBackstageSurface : ContainerControl
{
    private readonly RibbonControl _ribbon;
    private readonly Panel _navigation;
    private readonly Panel _pages = new() { TabIndex = 2 };
    private readonly EntryButton _back;
    private readonly Dictionary<RibbonItem, EntryButton> _entries = [];
    private readonly Dictionary<RibbonBackstagePageItem, PageSlot> _slots = [];
    private RibbonBackstagePageItem? _shownPage;
    private Font? _navigationFont, _navigationFontSource;
    private float _navigationFontMinimum;
    private int _navigationFontDpi;
    private bool _syncing, _tips, _disposing;
    private string _prefix = "";
    private RibbonDrawingHold? _motionOpacity;
    private Control? _motionTarget;
    private RibbonBackdropBuffer? _paintBuffer;
    private RibbonBackdropBuffer? _topFrameBuffer;
    private Rectangle _pageFrameBounds;
    private Size _pageRegionSize;
    private int _pageRegionRadius;
    private bool _movingForMotion;
    internal byte MotionAlpha { get; private set; } = 255;
    internal bool TransparentNavigation => _ribbon.HasWindowBackdrop && _ribbon.Backstage.NavigationBackgroundOpacity < 1;
    internal RibbonTheme PaintTheme => _ribbon.Theme with { BackstageNavigationBackground = Color.FromArgb(
        TransparentNavigation ? (int)Math.Round(255 * _ribbon.Backstage.NavigationBackgroundOpacity) : 255, _ribbon.Theme.BackstageNavigationBackground) };
    private Control MotionTarget => TransparentNavigation ? _pages : this;
    internal bool HasApplicationOpacity => _motionOpacity is null && RibbonDrawingHold.HasOpacity(MotionTarget.Handle);
    internal RibbonDrawingHold AcquireMotionOpacity(byte alpha)
    {
        var target = MotionTarget;
        if (!ReferenceEquals(_motionTarget, target)) ReleaseMotionOpacity();
        _motionTarget = target;
        _motionOpacity ??= RibbonDrawingHold.Create(target.Handle, alpha, compositeChildren: _ribbon.HasWindowBackdrop);
        if (_ribbon.HasWindowBackdrop) _motionOpacity.CompositeChildren();
        _motionOpacity.SetOpacity(alpha);
        return _motionOpacity;
    }
    internal void PrepareBackdrop()
    {
        if (Visible && IsHandleCreated && !HasApplicationOpacity) AcquireMotionOpacity(255).CompositeChildren();
    }
    internal void RepaintBackdrop()
    {
        RibbonDrawingHold.Repaint(Handle);
        // A transparent rail redirects only the page panel. Redraw that native
        // layer explicitly after its ancestor moves, including its edit HWNDs.
        if (_motionTarget is { IsDisposed: false, IsHandleCreated: true } target && !ReferenceEquals(target, this)) RibbonDrawingHold.Repaint(target.Handle);
        _motionOpacity?.SetOpacity(MotionAlpha);
    }
    internal void SetMotionAlpha(byte alpha) { MotionAlpha = alpha; _motionOpacity?.SetOpacity(alpha); }
    internal void RestoreMotionOpacity()
    {
        SetMotionAlpha(255);
        if (Visible && IsHandleCreated) RepaintBackdrop();
        else ReleaseMotionOpacity();
    }
    internal void MoveForMotion(Point location)
    {
        // Do not let WinForms copy clipped pixels from the previous position of
        // a redirected native page: the newly exposed strip can otherwise be black.
        _movingForMotion = true;
        try { Location = location; }
        finally { _movingForMotion = false; }
    }
    protected override void WndProc(ref Message message)
    {
        if (_movingForMotion && message.Msg == 0x46) // WINDOWPOSCHANGING
        {
            var position = Marshal.PtrToStructure<MotionWindowPosition>(message.LParam);
            position.Flags |= 0x0100; // SWP_NOCOPYBITS; retain WinForms' native bounds transaction.
            Marshal.StructureToPtr(position, message.LParam, false);
        }
        base.WndProc(ref message);
    }
    [StructLayout(LayoutKind.Sequential)] private struct MotionWindowPosition
    { internal nint Window, After; internal int X, Y, Width, Height; internal uint Flags; }
    private void ReleaseMotionOpacity() { _motionOpacity?.Dispose(); _motionOpacity = null; _motionTarget = null; }
    internal Rectangle NavigationBounds => new(0, 0, _pageFrameBounds.Left, ClientSize.Height);
    internal Control PageArea => _pages;
    internal RibbonBackstageSurface(RibbonControl ribbon)
    {
        _ribbon = ribbon;
        _navigation = new NavigationPanel(this) { AutoScroll = true, TabIndex = 1 };
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        AutoScaleMode = AutoScaleMode.None;
        AutoValidate = AutoValidate.EnablePreventFocusChange;
        TabStop = false;
        AccessibleName = "Backstage";
        _back = new(this, null) { Text = "Back", AccessibleName = "Back", TabIndex = 0 };
        Controls.AddRange([_back, _navigation, _pages]);
        _back.Click += (_, _) => _ribbon.IsBackstageOpen = false;
    }
    internal void Synchronize()
    {
        if (_syncing || IsDisposed || _disposing) return;
        _syncing = true;
        try
        {
            bool hadFocus = ContainsFocus, hadPageFocus = _pages.ContainsFocus;
            if (_ribbon.IsBackstageOpen && !ReferenceEquals(_shownPage, _ribbon.Backstage.SelectedPage)) _ribbon.BackstageOpeningMotion?.Dispose();
            Font = _ribbon.Font;
            UpdateNavigationFont();
            BackColor = _ribbon.Theme.BackstageNavigationBackground;
            _navigation.BackColor = _ribbon.Theme.BackstageNavigationBackground;
            _pages.BackColor = _ribbon.Theme.Surface;
            if (_motionOpacity is not null && !ReferenceEquals(_motionTarget, MotionTarget)) ReleaseMotionOpacity();
            if (Visible && _ribbon.HasWindowBackdrop && _motionOpacity is null) PrepareBackdrop();
            var items = _ribbon.Backstage.Items.Where(i => i.Visible && !i.IsDisposed).ToArray();
            foreach (var old in _entries.Keys.Except(items).ToArray()) { _entries[old].Dispose(); _entries.Remove(old); }
            int index = 0;
            foreach (var item in items)
            {
                if (!_entries.TryGetValue(item, out var button))
                {
                    button = new(this, item);
                    button.Click += (_, _) => _ribbon.ActivateBackstage(item);
                    _entries.Add(item, button);
                    _navigation.Controls.Add(button);
                }
                button.Text = item.Text;
                button.AccessibleName = string.IsNullOrEmpty(item.AccessibleName) ? item.Text : item.AccessibleName;
                button.AccessibleDescription = item.AccessibleDescription;
                button.Enabled = item.EffectiveEnabled;
                button.TabIndex = index++;
            }
            var pages = _ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>().Where(p => !p.IsDisposed).ToArray();
            foreach (var old in _slots.Keys.Where(p => !pages.Contains(p) || !ReferenceEquals(p.Content, _slots[p].Content)).ToArray())
            { _slots[old].Dispose(); _slots.Remove(old); }
            foreach (var page in pages.Where(p => p.Content is { IsDisposed: false }))
            {
                if (!_slots.TryGetValue(page, out var slot))
                {
                    slot = new(page.Content!);
                    _slots.Add(page, slot);
                    _pages.Controls.Add(slot);
                }
                slot.Visible = ReferenceEquals(page, _ribbon.Backstage.SelectedPage);
            }
            bool movedFocus = hadFocus && (!ContainsFocus || hadPageFocus && !ReferenceEquals(_shownPage, _ribbon.Backstage.SelectedPage));
            _shownPage = _ribbon.Backstage.SelectedPage;
            PerformLayout();
            AssignTips();
            Invalidate(true);
            if (movedFocus && _ribbon.IsBackstageOpen) FocusInitial(false);
        }
        finally { _syncing = false; }
    }
    private void UpdateNavigationFont()
    {
        var source = _ribbon.Font;
        float minimum = _ribbon.Theme.BackstageNavigationFontSize;
        if (ReferenceEquals(_navigationFontSource, source) && _navigationFontMinimum == minimum && _navigationFontDpi == DeviceDpi) return;
        var font = new Font(source.FontFamily, Math.Max(source.SizeInPoints, minimum * 72 / 96), source.Style,
            GraphicsUnit.Point, source.GdiCharSet, source.GdiVerticalFont);
        var previous = _navigationFont;
        _navigationFont = font; _navigationFontSource = source; _navigationFontMinimum = minimum; _navigationFontDpi = DeviceDpi;
        _navigation.Font = font;
        previous?.Dispose();
    }
    internal void DetachContent(Control content)
    {
        foreach (var page in _slots.Keys.Where(p => ReferenceEquals(_slots[p].Content, content)).ToArray())
        { _slots[page].Dispose(); _slots.Remove(page); }
    }
    internal bool ValidatePage() => ValidateChildren(ValidationConstraints.Enabled | ValidationConstraints.Visible);
    internal void FocusInitial(bool tips)
    {
        _tips = tips;
        _prefix = "";
        if (_ribbon.Backstage.SelectedPage is { } selected && _entries.TryGetValue(selected, out var entry) && entry.CanFocus) entry.Focus();
        else if (_entries.Values.FirstOrDefault(b => b.CanFocus) is { } first) first.Focus();
        else _back.Focus();
        AssignTips();
    }
    internal void ToggleKeyTips() { _tips = !_tips; _prefix = ""; AssignTips(); }
    internal void HideKeyTips() { _tips = false; _prefix = ""; AssignTips(); }
    private void AssignTips()
    {
        var buttons = NavigationButtons().ToArray();
        var tips = KeyTipResolver.Resolve(buttons.Select(b => (b.Text, b.Item?.KeyTip ?? "B")));
        for (int i = 0; i < buttons.Length; i++) { buttons[i].Tip = _tips && tips[i].StartsWith(_prefix, StringComparison.Ordinal) ? tips[i] : ""; buttons[i].Invalidate(); }
    }
    private IEnumerable<EntryButton> NavigationButtons() => new[] { _back }.Concat(_ribbon.Backstage.Items.Where(i => _entries.ContainsKey(i)).Select(i => _entries[i])).Where(b => b.Visible && b.Enabled);
    internal bool ProcessSurfaceKey(Keys keys)
    {
        if (!_ribbon.IsBackstageOpen) return false;
        if (keys == (Keys.Control | Keys.F1)) return true;
        if (keys == Keys.Escape)
        {
            if (FocusedControl(_ribbon.FindForm()!) is ComboBox { DroppedDown: true } combo) combo.DroppedDown = false;
            else _ribbon.IsBackstageOpen = false;
            return true;
        }
        if (keys is Keys.Tab or (Keys.Tab | Keys.Shift))
        {
            HideKeyTips();
            SelectNextControl(RibbonControl.FocusedControl(this), !keys.HasFlag(Keys.Shift), true, true, true);
            return true;
        }
        if (keys is Keys.F6 or (Keys.F6 | Keys.Shift))
        {
            HideKeyTips();
            if (_pages.ContainsFocus || !_pages.SelectNextControl(null, true, true, true, true)) FocusInitial(false);
            return true;
        }
        if (_tips && (keys & (Keys.Control | Keys.Alt)) == 0 && (keys & Keys.KeyCode) is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9)
        {
            var key = keys & Keys.KeyCode;
            char c = key is >= Keys.A and <= Keys.Z ? (char)('A' + key - Keys.A) : (char)('0' + key - Keys.D0);
            string prefix = _prefix + c;
            var buttons = NavigationButtons().ToArray();
            var tips = KeyTipResolver.Resolve(buttons.Select(b => (b.Text, b.Item?.KeyTip ?? "B")));
            int match = Array.FindIndex(tips, t => t == prefix);
            if (match >= 0) { HideKeyTips(); buttons[match].PerformClick(); }
            else { _prefix = tips.Any(t => t.StartsWith(prefix, StringComparison.Ordinal)) ? prefix : ""; AssignTips(); }
            return true;
        }
        if (keys is Keys.Up or Keys.Down or Keys.Home or Keys.End && (_navigation.ContainsFocus || _back.Focused))
        {
            HideKeyTips();
            var buttons = NavigationButtons().ToArray();
            int index = Array.FindIndex(buttons, b => b.Focused);
            index = keys == Keys.Home ? 0 : keys == Keys.End ? buttons.Length - 1 : (index + (keys == Keys.Up ? -1 : 1) + buttons.Length) % buttons.Length;
            buttons[index].Focus();
            return true;
        }
        return false;
    }
    private static Control? FocusedControl(ContainerControl container) => RibbonControl.FocusedControl(container);
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) => ProcessSurfaceKey(keyData) || base.ProcessCmdKey(ref msg, keyData);
    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData == Keys.Enter)
        {
            // Keep the Form's document AcceptButton outside this active surface.
            // Native multiline editors handle their input before dialog-key routing.
            if (RibbonControl.FocusedControl(this) is IButtonControl button) button.PerformClick();
            return true;
        }
        return base.ProcessDialogKey(keyData);
    }
    protected override bool ProcessMnemonic(char charCode) => base.ProcessMnemonic(charCode) || _ribbon.IsBackstageOpen;
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (_back is null) return;
        var layout = RibbonBackstageLayout.Build(ClientSize, _navigation.Font, DeviceDpi, _ribbon.Backstage);
        _back.Bounds = layout.Back;
        _navigation.Bounds = layout.Navigation;
        _pageFrameBounds = layout.Page;
        int border = RibbonBackstageLayout.PageBorderWidth(_ribbon.Painter, _ribbon.Theme, DeviceDpi, layout.Page.Size);
        int radius = RibbonBackstageLayout.PageCornerRadius(_ribbon.Painter, _ribbon.Theme, DeviceDpi, layout.Page.Size);
        _pages.Bounds = new(layout.Page.Left + border, layout.Page.Top + border,
            Math.Max(0, layout.Page.Width - border), Math.Max(0, layout.Page.Height - border));
        UpdatePageRegion(Math.Max(0, radius - border));
        int y = _navigation.AutoScrollPosition.Y;
        foreach (var item in _ribbon.Backstage.Items.Where(i => _entries.ContainsKey(i)))
        {
            _entries[item].Bounds = new(layout.Inset, y, Math.Max(1, _navigation.ClientSize.Width - 2 * layout.Inset), layout.RowHeight);
            y += layout.RowHeight;
        }
        _navigation.AutoScrollMinSize = new(0, _entries.Count * layout.RowHeight);
        foreach (var slot in _slots.Values) slot.Bounds = new(Point.Empty, _pages.ClientSize);
    }
    private void UpdatePageRegion(int radius)
    {
        var size = _pages.ClientSize;
        radius = Math.Clamp(radius, 0, Math.Max(0, Math.Min(size.Width, size.Height) / 2));
        if (size == _pageRegionSize && radius == _pageRegionRadius) return;
        Region? next = null;
        if (radius > 0)
        {
            next = RoundedPainter.CreateOpaqueRegion(new(size.Width + radius, size.Height + radius), radius);
            next.Intersect(new Rectangle(Point.Empty, size)); // Only the top-left corner is inside the native clip.
        }
        var previous = _pages.Region; _pages.Region = next; previous?.Dispose();
        _pageRegionSize = size; _pageRegionRadius = radius;
    }
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); Synchronize(); }
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) ReleaseMotionOpacity();
        else if (_ribbon.HasWindowBackdrop && _motionOpacity is null) PrepareBackdrop();
    }
    protected override void OnHandleDestroyed(EventArgs e) { ReleaseMotionOpacity(); base.OnHandleDestroyed(e); }
    protected override void OnPaintBackground(PaintEventArgs e) { if (!TransparentNavigation) base.OnPaintBackground(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        void Paint(Graphics graphics) => _ribbon.Painter.DrawBackstageBackground(new(graphics, ClientRectangle, PaintTheme, Font, DeviceDpi) { Density = _ribbon.Density, CompositedText = TransparentNavigation, WindowBackdrop = _ribbon.HasWindowBackdrop }, _pageFrameBounds.Left);
        if (TransparentNavigation)
        {
            // The native page owns its pixels and opacity. Only redirect the
            // uncovered rail/corner and top frame, not a window-sized white page.
            int rail = Math.Clamp(_pages.Left + _pageRegionRadius, 0, Width);
            int top = Math.Clamp(_pages.Top, 0, Height);
            if (rail > 0) (_paintBuffer ??= new()).Paint(e.Graphics, new(rail, Height), Height, Color.Transparent, Paint, opacity: MotionAlpha);
            if (top > 0 && rail < Width)
                (_topFrameBuffer ??= new()).Paint(e.Graphics, new(Width - rail, top), top, Color.Transparent, Paint, opacity: MotionAlpha, origin: new(rail, 0));
        }
        else
        {
            _paintBuffer?.Dispose(); _paintBuffer = null; _topFrameBuffer?.Dispose(); _topFrameBuffer = null;
            Paint(e.Graphics);
        }
    }
    private sealed class NavigationPanel : Panel
    {
        private readonly RibbonBackstageSurface _surface;
        internal NavigationPanel(RibbonBackstageSurface surface)
        {
            _surface = surface;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        private RibbonBackdropBuffer? _buffer;
        protected override void OnPaintBackground(PaintEventArgs e) { if (!_surface.TransparentNavigation) base.OnPaintBackground(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!_surface.TransparentNavigation) { _buffer?.Dispose(); _buffer = null; base.OnPaint(e); return; }
            (_buffer ??= new()).Paint(e.Graphics, Size, Height, Color.Transparent, graphics =>
            { using var fill = new SolidBrush(_surface.PaintTheme.BackstageNavigationBackground); graphics.FillRectangle(fill, ClientRectangle); }, opacity: _surface.MotionAlpha);
        }
        protected override void Dispose(bool disposing) { if (disposing) _buffer?.Dispose(); base.Dispose(disposing); }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposing)
        {
            _disposing = true;
            ReleaseMotionOpacity();
            _paintBuffer?.Dispose(); _paintBuffer = null; _topFrameBuffer?.Dispose(); _topFrameBuffer = null;
            foreach (var slot in _slots.Values) slot.Detach();
            _ribbon.BackstageSurfaceDisposed(this);
        }
        base.Dispose(disposing);
        if (disposing) { _navigationFont?.Dispose(); _navigationFont = null; _navigationFontSource = null; }
    }
    private sealed class PageSlot : Panel
    {
        internal Control Content { get; }
        private readonly DockStyle _dock;
        private readonly Rectangle _bounds;
        private readonly bool _visible;
        internal PageSlot(Control content)
        {
            Visible = false;
            Content = content;
            _dock = content.Dock; _bounds = content.Bounds; _visible = content.Visible;
            // Preserve an already established binding context when taking a native parent.
            if (content.BindingContext is { } context) content.BindingContext = context;
            Controls.Add(content);
            content.Dock = DockStyle.Fill;
            content.Visible = true;
            TabStop = false;
        }
        internal void Detach()
        {
            if (!ReferenceEquals(Content.Parent, this)) return;
            Controls.Remove(Content);
            if (!Content.IsDisposed) { Content.Dock = _dock; Content.Bounds = _bounds; Content.Visible = _visible; }
        }
        protected override void Dispose(bool disposing) { if (disposing) Detach(); base.Dispose(disposing); }
    }
    private sealed class EntryButton : Button
    {
        private readonly RibbonBackstageSurface _surface;
        internal RibbonItem? Item { get; }
        internal string Tip = "";
        private bool _hot, _pressed;
        private RibbonBackdropBuffer? _buffer;
        internal EntryButton(RibbonBackstageSurface surface, RibbonItem? item)
        {
            _surface = surface; Item = item;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            CausesValidation = false;
            FlatStyle = FlatStyle.Flat;
        }
        protected override void OnMouseEnter(EventArgs e) { _hot = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hot = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _surface.HideKeyTips(); _pressed = e.Button == MouseButtons.Left; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnMouseCaptureChanged(EventArgs e) { _pressed = false; Invalidate(); base.OnMouseCaptureChanged(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { if (!_surface.TransparentNavigation) base.OnPaintBackground(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_surface.TransparentNavigation) (_buffer ??= new()).Paint(e.Graphics, Size, Height, Color.Transparent, PaintEntry, opacity: _surface.MotionAlpha);
            else { _buffer?.Dispose(); _buffer = null; PaintEntry(e.Graphics); }
        }
        internal void PaintAt(Graphics graphics, Point origin)
        {
            // Native theme text draws in HDC coordinates, so provide absolute
            // bounds instead of relying on a GDI+ translation reaching GDI.
            PaintEntry(graphics, new Rectangle(origin, Size));
        }
        private void PaintEntry(Graphics graphics) => PaintEntry(graphics, ClientRectangle);
        private void PaintEntry(Graphics graphics, Rectangle bounds)
        {
            var ribbon = _surface._ribbon;
            var context = new RibbonPaintContext(graphics, bounds, _surface.PaintTheme, Font, DeviceDpi, _hot, _pressed, Focused && ribbon.KeyboardFocusCues) { Density = ribbon.Density, CompositedText = _surface.TransparentNavigation, WindowBackdrop = ribbon.HasWindowBackdrop };
            ribbon.Painter.DrawBackstageEntry(context, Item, ReferenceEquals(Item, ribbon.Backstage.SelectedPage) && Item is not null);
            if (Tip.Length > 0)
            {
                var text = TextRenderer.MeasureText(graphics, Tip, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                int S(int n) => RibbonLayoutEngine.Scale(n, DeviceDpi);
                var size = new Size(Math.Min(Math.Max(0, Width - S(8)), text.Width + S(8)), Math.Min(Math.Max(0, Height - S(4)), text.Height + S(4)));
                ribbon.Painter.DrawKeyTip(context with { Bounds = new(bounds.Left + Math.Max(0, Width - S(4) - size.Width), bounds.Top + (Height - size.Height) / 2, size.Width, size.Height) }, Tip);
            }
        }
        protected override void Dispose(bool disposing) { if (disposing) _buffer?.Dispose(); base.Dispose(disposing); }
    }
}
