using RibbonKit.WinForms.Layout;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

// A child of an ordinary Form, with one persistent native parent per borrowed page.
// No document Controls are moved, disabled or disposed to produce the overlay.
internal sealed class RibbonBackstageSurface : ContainerControl
{
    private readonly RibbonControl _ribbon;
    private readonly Panel _navigation;
    private readonly Panel _footer;
    private readonly Panel _pages;
    private readonly Panel _pageContent = new() { TabIndex = 0 };
    private readonly EntryButton _back;
    private readonly Dictionary<RibbonItem, Control> _entries = [];
    private bool _arranging;
    private readonly Dictionary<RibbonBackstagePageItem, PageSlot> _slots = [];
    private RibbonBackstagePageItem? _shownPage;
    private RibbonBackstagePageMotion? _pageMotion;
    internal RibbonBackstagePageMotion? PageMotion => _pageMotion;
    internal bool IsPageAnimating => _pageMotion?.IsRunning == true;
    internal void SettlePageMotion() { var motion = _pageMotion; _pageMotion = null; motion?.Dispose(); }
    internal void ObservePageMotionInput(ref Message message) => _pageMotion?.ObserveInput(ref message);
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
    // Material paint keeps its alpha-preserving rail and native page layer even
    // at full rail opacity; changing the tint must not change redirection owners.
    private bool MaterialPaint => _ribbon.HasWindowBackdrop;
    internal bool TransparentNavigation => _ribbon.HasWindowBackdrop && _ribbon.Backstage.NavigationBackgroundOpacity < 1;
    internal RibbonTheme PaintTheme => _ribbon.Theme with
    {
        BackstageNavigationUsesAccent = false,
        BackstageNavigationHover = _ribbon.Theme.BackstageNavigationUsesAccent ? _ribbon.Theme.BackstageHoverFill : _ribbon.Theme.BackstageNavigationHover,
        BackstageNavigationSelected = _ribbon.Theme.BackstageNavigationUsesAccent ? _ribbon.Theme.BackstageSelectedFill : _ribbon.Theme.BackstageNavigationSelected,
        BackstageNavigationBackground = Color.FromArgb(TransparentNavigation ? (int)Math.Round(255 * _ribbon.Backstage.NavigationBackgroundOpacity) : 255,
            _ribbon.Theme.BackstageNavigationColor)
    };
    private Control MotionTarget => MaterialPaint ? _pages : this;
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
    internal void RepaintBackdrop(bool retainPage = false, bool publishOpacity = true)
    {
        if (retainPage && MaterialPaint)
        {
            // Only the rail/frame changes its drawing during travel. Keep the
            // page's redirected pixels and finish any native exposed-area paint,
            // rather than erasing every application editor on each timer step.
            int rail = Math.Clamp(_pages.Left + _pageRegionRadius, 0, Width);
            int top = Math.Clamp(_pages.Top, 0, Height);
            if (rail > 0) Invalidate(new Rectangle(0, 0, rail, Height), true);
            if (top > 0 && rail < Width) Invalidate(new Rectangle(rail, 0, Width - rail, top), true);
            RibbonDrawingHold.UpdatePending(Handle);
            if (_motionTarget is { IsDisposed: false, IsHandleCreated: true } target) RibbonDrawingHold.UpdatePending(target.Handle);
        }
        else
        {
            RibbonDrawingHold.Repaint(Handle);
            if (_motionTarget is { IsDisposed: false, IsHandleCreated: true } target && !ReferenceEquals(target, this)) RibbonDrawingHold.Repaint(target.Handle);
        }
        if (publishOpacity) PublishMotionOpacity();
    }
    internal void PublishMotionOpacity() => _motionOpacity?.SetOpacity(MotionAlpha);
    // Publish native opacity after painting the corresponding moved frame.
    internal void SetMotionAlpha(byte alpha) => MotionAlpha = alpha;
    internal void RestoreMotionOpacity()
    {
        SetMotionAlpha(255);
        if (Visible && IsHandleCreated) RepaintBackdrop(retainPage: MaterialPaint);
        else ReleaseMotionOpacity();
    }
    internal void MoveForMotion(Point location)
    {
        // Do not let WinForms copy clipped pixels from the previous position of
        // a redirected native page: the newly exposed strip can otherwise be black.
        bool repaintExposed = Visible && MaterialPaint && _pages.IsHandleCreated;
        var exposedBefore = repaintExposed ? VisiblePageBounds() : Rectangle.Empty;
        _movingForMotion = true;
        try { Location = location; }
        finally { _movingForMotion = false; }
        if (repaintExposed)
        {
            var exposedAfter = VisiblePageBounds();
            if (exposedAfter.IsEmpty) return;
            if (exposedBefore.IsEmpty) _pages.Invalidate(exposedAfter, true);
            else if (exposedAfter.Left < exposedBefore.Left)
                _pages.Invalidate(Rectangle.FromLTRB(exposedAfter.Left, exposedAfter.Top, exposedBefore.Left, exposedAfter.Bottom), true);
            if (!exposedBefore.IsEmpty && exposedAfter.Right > exposedBefore.Right)
                _pages.Invalidate(Rectangle.FromLTRB(exposedBefore.Right, exposedAfter.Top, exposedAfter.Right, exposedAfter.Bottom), true);
        }
    }
    private Rectangle VisiblePageBounds()
    {
        if (Parent is not { IsDisposed: false } parent) return Rectangle.Empty;
        var clip = Rectangle.Intersect(parent.RectangleToScreen(parent.ClientRectangle), SystemInformation.VirtualScreen);
        var visible = Rectangle.Intersect(_pages.RectangleToScreen(_pages.ClientRectangle), clip);
        return visible.Width > 0 && visible.Height > 0 ? _pages.RectangleToClient(visible) : Rectangle.Empty;
    }
    protected override void WndProc(ref Message message)
    {
        if (IsCaptionHit(message)) { message.Result = -1; return; }
        if (_movingForMotion && message.Msg == 0x46) // WINDOWPOSCHANGING
        {
            var position = Marshal.PtrToStructure<MotionWindowPosition>(message.LParam);
            position.Flags |= 0x0100; // SWP_NOCOPYBITS; retain WinForms' native bounds transaction.
            Marshal.StructureToPtr(position, message.LParam, false);
        }
        base.WndProc(ref message);
    }
    private bool IsCaptionHit(Message message)
    {
        if (message.Msg != 0x84 || CaptionHeight <= 0) return false;
        var point = PointToClient(new(unchecked((short)(long)message.LParam), unchecked((short)((long)message.LParam >> 16))));
        return point.Y >= 0 && point.Y < CaptionHeight;
    }
    private sealed class PagePanel(RibbonBackstageSurface surface) : Panel
    {
        protected override void WndProc(ref Message message)
        {
            if (surface.IsCaptionHit(message)) { message.Result = -1; return; }
            base.WndProc(ref message);
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct MotionWindowPosition
    { internal nint Window, After; internal int X, Y, Width, Height; internal uint Flags; }
    private void ReleaseMotionOpacity() { _motionOpacity?.Dispose(); _motionOpacity = null; _motionTarget = null; }
    internal Rectangle NavigationBounds => new(0, 0, _pageFrameBounds.Left, ClientSize.Height);
    internal Control PageArea => _pages;
    internal int CaptionHeight => _ribbon.HasFullHeightBackstage ? _ribbon.WindowCaptionHeight : 0;
    internal RibbonBackstageSurface(RibbonControl ribbon)
    {
        _ribbon = ribbon;
        _pages = new PagePanel(this) { TabIndex = 3 };
        _pages.Controls.Add(_pageContent);
        _navigation = new NavigationPanel(this) { AutoScroll = true, TabIndex = 1 };
        _footer = new NavigationPanel(this) { TabIndex = 2, Visible = false };
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        AutoScaleMode = AutoScaleMode.None;
        AutoValidate = AutoValidate.EnablePreventFocusChange;
        TabStop = false;
        AccessibleName = "Backstage";
        _back = new(this, null) { Text = "Back", AccessibleName = "Back", TabIndex = 0 };
        Controls.AddRange([_back, _navigation, _footer, _pages]);
        _back.Click += (_, _) => _ribbon.IsBackstageOpen = false;
    }
    internal void Synchronize()
    {
        if (_syncing || IsDisposed || _disposing) return;
        _syncing = true;
        try
        {
            bool hadFocus = ContainsFocus, hadPageFocus = _pages.ContainsFocus;
            bool interrupted = IsPageAnimating || _ribbon.BackstageOpeningMotion?.IsRunning == true;
            bool changedPage = !ReferenceEquals(_shownPage, _ribbon.Backstage.SelectedPage);
            bool animatePage = changedPage && !interrupted && _shownPage is { Content: not null } previous && _ribbon.Backstage.Available(previous);
            SettlePageMotion();
            if (_ribbon.IsBackstageOpen && !ReferenceEquals(_shownPage, _ribbon.Backstage.SelectedPage)) _ribbon.BackstageOpeningMotion?.Dispose();
            Font = _ribbon.Font;
            UpdateNavigationFont();
            BackColor = _ribbon.Theme.BackstageNavigationColor;
            _navigation.BackColor = _ribbon.Theme.BackstageNavigationColor;
            _footer.BackColor = _navigation.BackColor;
            _pages.BackColor = _ribbon.Theme.Surface;
            _pageContent.BackColor = _ribbon.Theme.Surface;
            if (_motionOpacity is not null && !ReferenceEquals(_motionTarget, MotionTarget)) ReleaseMotionOpacity();
            if (Visible && _ribbon.HasWindowBackdrop && _motionOpacity is null) PrepareBackdrop();
            var items = _ribbon.Backstage.NavigationItems.Where(i => i.Visible && !i.IsDisposed).ToArray();
            foreach (var old in _entries.Keys.Except(items).ToArray()) { _entries[old].Dispose(); _entries.Remove(old); }
            int index = 0;
            foreach (var item in items)
            {
                if (!_entries.TryGetValue(item, out var button))
                {
                    button = item is RibbonMenuSeparatorItem separator ? new SeparatorControl(this, separator) : new EntryButton(this, item);
                    if (button is EntryButton) button.Click += (_, _) => _ribbon.ActivateBackstage(item);
                    _entries.Add(item, button);
                    _navigation.Controls.Add(button);
                }
                button.Text = item.Text;
                button.AccessibleName = string.IsNullOrEmpty(item.AccessibleName) ? item.Text : item.AccessibleName;
                button.AccessibleDescription = item.AccessibleDescription;
                button.Enabled = item is not RibbonMenuSeparatorItem && item.EffectiveEnabled;
                button.TabIndex = index++;
            }
            var pages = _ribbon.Backstage.NavigationItems.OfType<RibbonBackstagePageItem>().Where(p => !p.IsDisposed).ToArray();
            foreach (var old in _slots.Keys.Where(p => !pages.Contains(p) || !ReferenceEquals(p.Content, _slots[p].Content)).ToArray())
            { _slots[old].Dispose(); _slots.Remove(old); }
            foreach (var page in pages.Where(p => p.Content is { IsDisposed: false }))
            {
                if (!_slots.TryGetValue(page, out var slot))
                {
                    slot = new(page.Content!);
                    _slots.Add(page, slot);
                    _pageContent.Controls.Add(slot);
                }
            }
            PerformLayout();
            if (animatePage && _ribbon.Backstage.SelectedPage is { } selected && _slots.TryGetValue(selected, out var arriving))
                _pageMotion = RibbonBackstagePageMotion.Prepare(_ribbon, arriving, _pageContent);
            foreach (var pair in _slots) pair.Value.Visible = ReferenceEquals(pair.Key, _ribbon.Backstage.SelectedPage);
            bool movedFocus = hadFocus && (!ContainsFocus || hadPageFocus && !ReferenceEquals(_shownPage, _ribbon.Backstage.SelectedPage));
            _shownPage = _ribbon.Backstage.SelectedPage;
            PerformLayout();
            AssignTips();
            // Selection changes chrome, not authored page pixels. Invalidating
            // every descendant also invalidates native edit/RichEdit windows.
            Invalidate(); _back.Invalidate(); _navigation.Invalidate(true); _footer.Invalidate(true);
            if (movedFocus && _ribbon.IsBackstageOpen) FocusInitial(false);
            _pageMotion?.Start();
        }
        catch { SettlePageMotion(); throw; }
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
        _footer.Font = font;
        previous?.Dispose();
    }
    internal void DetachContent(Control content)
    {
        SettlePageMotion();
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
    private IEnumerable<EntryButton> NavigationButtons() => new[] { _back }.Concat(_ribbon.Backstage.NavigationItems.Where(i => _entries.ContainsKey(i)).Select(i => _entries[i]).OfType<EntryButton>()).Where(b => b.Visible && b.Enabled);
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
        if (keys is Keys.Up or Keys.Down or Keys.Home or Keys.End && (_navigation.ContainsFocus || _footer.ContainsFocus || _back.Focused))
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
        if (_back is null || _arranging) return;
        _arranging = true;
        try
        {
            var layout = RibbonBackstageLayout.Build(ClientSize, _navigation.Font, DeviceDpi, _ribbon.Backstage, captionHeight: CaptionHeight);
            _back.Bounds = layout.Back;
            _navigation.Bounds = layout.Navigation;
            _footer.Bounds = layout.Footer;
            _pageFrameBounds = layout.Page;
            int border = RibbonBackstageLayout.PageBorderWidth(_ribbon.Painter, _ribbon.Theme, DeviceDpi, layout.Page.Size);
            int radius = RibbonBackstageLayout.PageCornerRadius(_ribbon.Painter, _ribbon.Theme, DeviceDpi, layout.Page.Size);
            _pages.Bounds = layout.PageContentBounds(_ribbon.Theme, DeviceDpi, border);
            _pageContent.Bounds = new(0, Math.Min(CaptionHeight, _pages.Height), _pages.Width, Math.Max(0, _pages.Height - CaptionHeight));
            int padding = RibbonLayoutEngine.Scale(_ribbon.Theme.BackstagePagePadding ?? 0, DeviceDpi);
            UpdatePageRegion(Math.Max(0, radius - border - padding));
            _navigation.SuspendLayout(); _footer.SuspendLayout();
            try
            {
                _navigation.AutoScrollMinSize = new(0, layout.NavigationContentHeight);
                foreach (var slot in layout.Arrange(_ribbon.Backstage, -_navigation.AutoScrollPosition.Y))
                {
                    if (!_entries.TryGetValue(slot.Item, out var entry)) continue;
                    var parent = slot.Footer && !layout.FooterInline ? _footer : _navigation;
                    if (!ReferenceEquals(entry.Parent, parent)) parent.Controls.Add(entry);
                    var bounds = slot.Bounds; bounds.Offset(-parent.Left, -parent.Top);
                    bounds.Width = Math.Max(1, parent.ClientSize.Width - 2 * layout.Inset); entry.Bounds = bounds;
                }
                _footer.Visible = layout.Footer.Height > 0;
            }
            finally { _navigation.ResumeLayout(false); _footer.ResumeLayout(false); }
            foreach (var slot in _slots.Values)
                if (!IsPageAnimating || !ReferenceEquals(_pageMotion!.Target, slot)) slot.Bounds = new(Point.Empty, _pageContent.ClientSize);
        }
        finally { _arranging = false; }
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
        if (!Visible) { SettlePageMotion(); ReleaseMotionOpacity(); }
        else if (_ribbon.HasWindowBackdrop && _motionOpacity is null) PrepareBackdrop();
    }
    protected override void OnHandleDestroyed(EventArgs e) { SettlePageMotion(); ReleaseMotionOpacity(); base.OnHandleDestroyed(e); }
    protected override void OnPaintBackground(PaintEventArgs e) { if (!MaterialPaint) base.OnPaintBackground(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        void Paint(Graphics graphics) => _ribbon.Painter.DrawBackstageBackground(new(graphics, ClientRectangle, PaintTheme, Font, DeviceDpi) { Density = _ribbon.Density, CompositedText = MaterialPaint, WindowBackdrop = _ribbon.HasWindowBackdrop }, _pageFrameBounds.Left);
        if (MaterialPaint)
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
        protected override void OnPaintBackground(PaintEventArgs e) { if (!_surface.MaterialPaint) base.OnPaintBackground(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!_surface.MaterialPaint) { _buffer?.Dispose(); _buffer = null; base.OnPaint(e); return; }
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
            SettlePageMotion();
            ReleaseMotionOpacity();
            _paintBuffer?.Dispose(); _paintBuffer = null; _topFrameBuffer?.Dispose(); _topFrameBuffer = null;
            foreach (var slot in _slots.Values) slot.Detach();
            _ribbon.BackstageSurfaceDisposed(this);
        }
        base.Dispose(disposing);
        if (disposing) { _navigationFont?.Dispose(); _navigationFont = null; _navigationFontSource = null; }
    }
    internal sealed class PageSlot : Panel
    {
        internal Control Content { get; }
        private readonly DockStyle _dock;
        private readonly Rectangle _bounds;
        private readonly bool _visible;
        private bool _moving;
        private RibbonDrawingHold? _pageOpacity;
        internal byte PageOpacity { get; private set; } = 255;
        internal bool HasApplicationOpacity => _pageOpacity is null && RibbonDrawingHold.HasOpacity(Handle);
        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                // Compose this native page subtree atomically, including edit
                // HWNDs. WinForms' managed double buffering cannot cover them.
                parameters.ExStyle |= 0x02000000; // WS_EX_COMPOSITED, scoped to the library-owned slot.
                return parameters;
            }
        }
        internal void AcquirePageOpacity(byte alpha)
        {
            _pageOpacity ??= RibbonDrawingHold.Create(Handle, alpha, compositeChildren: true);
            SetPageOpacity(alpha);
        }
        internal void SetPageOpacity(byte alpha) { _pageOpacity?.SetOpacity(alpha); PageOpacity = _pageOpacity is null ? (byte)255 : alpha; }
        internal void RestorePageOpacity()
        {
            SetPageOpacity(255);
            // Keep the visible page's redirection stable after fading. Removing
            // WS_EX_LAYERED and forcing an erase here flashes native editors.
            if (IsHandleCreated) RibbonDrawingHold.UpdatePending(Handle);
        }
        private void ReleasePageOpacity() { _pageOpacity?.Dispose(); _pageOpacity = null; PageOpacity = 255; }
        protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); if (!Visible) ReleasePageOpacity(); }
        protected override void OnHandleDestroyed(EventArgs e) { ReleasePageOpacity(); base.OnHandleDestroyed(e); }
        internal void MoveForMotion(Point location)
        {
            bool repaintExposed = Visible && IsHandleCreated;
            using var before = repaintExposed ? VisibleClientRegion() : null;
            _moving = true;
            try { Location = location; }
            finally { _moving = false; }
            if (!repaintExposed || !Visible) return;
            using var after = VisibleClientRegion();
            // The redirected page initially paints only its unclipped rows.
            // Sliding down exposes rows above that cached drawing; update just
            // those pixels before publishing the next frame, including the
            // curved viewport edge and native children within the exposed area.
            if (before is not null) after.Exclude(before);
            Invalidate(after, true);
        }
        private Region VisibleClientRegion()
        {
            if (Parent is not { } viewport) return new Region(Rectangle.Empty);
            var region = viewport.Region?.Clone() ?? new Region(viewport.ClientRectangle);
            region.Translate(-Left, -Top); region.Intersect(ClientRectangle);
            return region;
        }
        protected override void WndProc(ref Message message)
        {
            if (_moving && message.Msg == 0x46)
            {
                var position = Marshal.PtrToStructure<MotionWindowPosition>(message.LParam);
                position.Flags |= 0x0100; // SWP_NOCOPYBITS; repaint exposed native strips.
                Marshal.StructureToPtr(position, message.LParam, false);
            }
            base.WndProc(ref message);
        }
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
        protected override void Dispose(bool disposing) { if (disposing) { Visible = false; ReleasePageOpacity(); Detach(); } base.Dispose(disposing); }
    }
    private sealed class SeparatorControl : Control
    {
        private readonly RibbonBackstageSurface _surface;
        private readonly RibbonMenuSeparatorItem _item;
        private RibbonBackdropBuffer? _buffer;
        internal SeparatorControl(RibbonBackstageSurface surface, RibbonMenuSeparatorItem item)
        {
            _surface = surface; _item = item; TabStop = false; Enabled = false; AccessibleRole = AccessibleRole.Separator;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.Selectable, false);
        }
        protected override void OnPaintBackground(PaintEventArgs e) { if (!_surface.MaterialPaint) base.OnPaintBackground(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            void Paint(Graphics graphics) => _surface._ribbon.Painter.DrawBackstageEntry(new(graphics, ClientRectangle, _surface.PaintTheme, Font, DeviceDpi)
                { Density = _surface._ribbon.Density, CompositedText = _surface.MaterialPaint, WindowBackdrop = _surface._ribbon.HasWindowBackdrop }, _item, false);
            if (_surface.MaterialPaint) (_buffer ??= new()).Paint(e.Graphics, Size, Height, Color.Transparent, Paint, opacity: _surface.MotionAlpha);
            else { _buffer?.Dispose(); _buffer = null; Paint(e.Graphics); }
        }
        protected override void Dispose(bool disposing) { if (disposing) _buffer?.Dispose(); base.Dispose(disposing); }
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
        protected override void OnPaintBackground(PaintEventArgs e) { if (!_surface.MaterialPaint) base.OnPaintBackground(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_surface.MaterialPaint) (_buffer ??= new()).Paint(e.Graphics, Size, Height, Color.Transparent, PaintEntry, opacity: _surface.MotionAlpha);
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
            var context = new RibbonPaintContext(graphics, bounds, _surface.PaintTheme, Font, DeviceDpi, _hot, _pressed, Focused && ribbon.KeyboardFocusCues) { Density = ribbon.Density, CompositedText = _surface.MaterialPaint, WindowBackdrop = ribbon.HasWindowBackdrop };
            ribbon.Painter.DrawBackstageEntry(context, Item, ReferenceEquals(Item, ribbon.Backstage.SelectedPage) && Item is not null);
            if (Tip.Length > 0)
            {
                var text = TextRenderer.MeasureText(graphics, Tip, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                int S(int n) => RibbonLayoutEngine.Scale(n, DeviceDpi);
                var desired = new RibbonTipMetrics(ribbon.Theme).BadgeSize(text, DeviceDpi, backstage: true);
                var size = new Size(Math.Min(Math.Max(0, Width - S(8)), desired.Width), Math.Min(Math.Max(0, Height - S(4)), desired.Height));
                ribbon.Painter.DrawKeyTip(context with { Bounds = new(bounds.Left + Math.Max(0, Width - S(4) - size.Width), bounds.Top + (Height - size.Height) / 2, size.Width, size.Height) }, Tip);
            }
        }
        protected override void Dispose(bool disposing) { if (disposing) _buffer?.Dispose(); base.Dispose(disposing); }
    }
}
