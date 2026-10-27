using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// A child of an ordinary Form, with one persistent native parent per borrowed page.
// No document Controls are moved, disabled or disposed to produce the overlay.
internal sealed class RibbonBackstageSurface : ContainerControl
{
    private readonly RibbonControl _ribbon;
    private readonly Panel _navigation = new() { AutoScroll = true, TabIndex = 1 };
    private readonly Panel _pages = new() { TabIndex = 2 };
    private readonly EntryButton _back;
    private readonly Dictionary<RibbonItem, EntryButton> _entries = [];
    private readonly Dictionary<RibbonBackstagePageItem, PageSlot> _slots = [];
    private RibbonBackstagePageItem? _shownPage;
    private bool _syncing, _tips, _disposing;
    private string _prefix = "";
    internal RibbonBackstageSurface(RibbonControl ribbon)
    {
        _ribbon = ribbon;
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
            Font = _ribbon.Font;
            BackColor = _ribbon.Theme.BackstageNavigationBackground;
            _navigation.BackColor = _ribbon.Theme.BackstageNavigationBackground;
            _pages.BackColor = _ribbon.Theme.Surface;
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
        var layout = RibbonBackstageLayout.Build(ClientSize, Font, DeviceDpi, _ribbon.Backstage);
        _back.Bounds = layout.Back;
        _navigation.Bounds = layout.Navigation;
        _pages.Bounds = layout.Page;
        int y = _navigation.AutoScrollPosition.Y;
        foreach (var item in _ribbon.Backstage.Items.Where(i => _entries.ContainsKey(i)))
        {
            _entries[item].Bounds = new(layout.Inset, y, Math.Max(1, _navigation.ClientSize.Width - 2 * layout.Inset), layout.RowHeight);
            y += layout.RowHeight;
        }
        _navigation.AutoScrollMinSize = new(0, _entries.Count * layout.RowHeight);
        foreach (var slot in _slots.Values) slot.Bounds = new(Point.Empty, _pages.ClientSize);
    }
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); Synchronize(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        _ribbon.Painter.DrawBackstageBackground(new(e.Graphics, ClientRectangle, _ribbon.Theme, Font, DeviceDpi), _pages.Left);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposing)
        {
            _disposing = true;
            foreach (var slot in _slots.Values) slot.Detach();
            _ribbon.BackstageSurfaceDisposed(this);
        }
        base.Dispose(disposing);
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
        protected override void OnPaint(PaintEventArgs e)
        {
            var ribbon = _surface._ribbon;
            var context = new RibbonPaintContext(e.Graphics, ClientRectangle, ribbon.Theme, Font, DeviceDpi, _hot, _pressed, Focused && ribbon.KeyboardFocusCues);
            ribbon.Painter.DrawBackstageEntry(context, Item, ReferenceEquals(Item, ribbon.Backstage.SelectedPage) && Item is not null);
            if (Tip.Length > 0) ribbon.Painter.DrawKeyTip(context with { Bounds = new(Width - Math.Min(Width, 34), Height / 2 - 9, Math.Min(Width, 30), 18) }, Tip);
        }
    }
}
