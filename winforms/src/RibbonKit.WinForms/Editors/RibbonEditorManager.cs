using System.ComponentModel;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// One native editor per model. Popup windows borrow its presentation, never own it.
internal sealed class RibbonEditorManager(RibbonControl ribbon) : IDisposable
{
    private readonly Dictionary<RibbonItem, Presenter> _editors = [];
    private bool _syncing, _disposed;
    internal bool HasOpenList => _editors.Values.Any(p => p.ListOpen);
    internal bool ContainsFocus => _editors.Values.Any(p => p.Editor.ContainsFocus || p.ListOpen);
    internal TextBox? EditorFor(RibbonTextBoxItem item) => _editors.GetValueOrDefault(item)?.Editor as TextBox;
    internal ComboBox? EditorFor(RibbonComboBoxItem item) => _editors.GetValueOrDefault(item)?.Editor as ComboBox;
    internal bool CommitFocused() => _editors.Values.FirstOrDefault(p => p.Editor.ContainsFocus || p.ListOpen)?.CommitAndCloseList() ?? true;
    internal bool IsDropDownOpen(RibbonComboBoxItem item) => _editors.GetValueOrDefault(item) is { ListOpen: true, QuickReference: null };
    internal bool IsQuickAccessOpen(RibbonQuickAccessComboBoxReference reference) => _editors.Values.Any(p => p.ListOpen && ReferenceEquals(p.QuickReference, reference));
    internal void CloseQuickAccess(RibbonQuickAccessComboBoxReference reference)
    {
        foreach (var presenter in _editors.Values.Where(p => ReferenceEquals(p.QuickReference, reference)).ToArray()) presenter.SetDropDown(false);
    }
    internal void OpenQuickAccess(RibbonQuickAccessComboBoxReference reference, Control opener, Rectangle anchor)
    {
        if (_disposed || ribbon.IsInDesignMode || ribbon.IsBackstageOpen || !RibbonQuickAccess.Available(reference) || reference.Source is not { } source) return;
        CloseLists();
        ribbon.Popups.PrepareChoice(opener);
        if (!RibbonQuickAccess.Available(reference) || !ReferenceEquals(reference.Source, source) || opener.IsDisposed || !opener.Visible) return;
        var targets = opener is RibbonPopupSurface popup ? popup.Targets : ribbon.Snapshot.Targets;
        if (targets.FirstOrDefault(t => ReferenceEquals(t.Element, reference)) is not { } target || target.VisibleBounds.IsEmpty) return;
        anchor = target.VisibleBounds;
        if (!_editors.TryGetValue(source, out var presenter)) _editors[source] = presenter = new(ribbon, source);
        presenter.OpenQuickAccess(reference, opener, anchor);
    }
    internal void SetDropDown(RibbonComboBoxItem item, bool open)
    {
        if (open && ribbon.IsBackstageOpen) return;
        if (_editors.TryGetValue(item, out var presenter)) presenter.SetDropDown(open);
    }
    internal bool ProcessListKey(Keys keys) => _editors.Values.FirstOrDefault(p => p.ListOpen)?.ProcessListKey(keys) ?? false;
    internal bool ContainsListPoint(Point point) => _editors.Values.Any(p => p.Choice?.ContainsPoint(point) == true);
    internal bool OwnsWindow(nint handle) => _editors.Values.Any(p => p.Choice?.OwnsWindow(handle) == true);
    internal void CloseLists(RibbonItem? except = null)
    {
        foreach (var entry in _editors.Where(p => !ReferenceEquals(p.Key, except)).ToArray()) entry.Value.SetDropDown(false);
    }
    internal void OutsidePress(Point point)
    {
        foreach (var presenter in _editors.Values.ToArray())
            if (presenter.ListOpen && !presenter.ScreenField.Contains(point)) presenter.SetDropDown(false);
    }
    internal bool ProcessListCharacter(char value)
    {
        var presenter = _editors.Values.FirstOrDefault(p => p.ListOpen);
        return presenter?.ProcessCharacter(value) ?? false;
    }
    internal void Sync()
    {
        if (_syncing || _disposed || ribbon.IsInDesignMode) return;
        _syncing = true;
        try
        {
            var owned = ribbon.RetainedElements.OfType<RibbonItem>().Where(i => !i.IsDisposed && i is IRibbonEditorItem).ToHashSet();
            foreach (var item in _editors.Keys.Where(i => !owned.Contains(i)).ToArray())
            {
                _editors[item].Dispose();
                _editors.Remove(item);
            }
            var slots = new Dictionary<RibbonItem, (Control Surface, RibbonTarget Target)>();
            void Add(Control surface, IEnumerable<RibbonTarget> targets)
            {
                foreach (var target in targets)
                    if (target.Element is RibbonItem item && item is IRibbonEditorItem && !target.VisibleBounds.IsEmpty)
                        slots[item] = (surface, target);
            }
            Add(ribbon, ribbon.Snapshot.Targets);
            foreach (var popup in ribbon.Popups.Surfaces) Add(popup, popup.Targets);
            foreach (var item in owned)
            {
                bool shown = slots.TryGetValue(item, out var slot);
                if (!_editors.TryGetValue(item, out var presenter))
                {
                    if (!shown) continue;
                    _editors[item] = presenter = new(ribbon, item);
                }
                if (shown) presenter.Present(slot.Surface, slot.Target);
                else presenter.Park();
            }
        }
        finally { _syncing = false; }
    }
    internal bool Focus(RibbonItem item)
    {
        if (ribbon.IsBackstageOpen) return false;
        Sync();
        return _editors.TryGetValue(item, out var presenter) && presenter.FocusEditor();
    }
    internal void Release(Control surface)
    {
        foreach (var presenter in _editors.Values.ToArray()) presenter.Release(surface);
    }
    public void Dispose()
    {
        _disposed = true;
        foreach (var presenter in _editors.Values.ToArray()) presenter.Dispose();
        _editors.Clear();
    }

    private sealed class Presenter : Control
    {
        private readonly RibbonControl _ribbon;
        private readonly RibbonItem _item;
        private readonly IRibbonEditorItem _value;
        private readonly Control _editor;
        private object _accepted, _lastModel;
        private object? _listStart;
        private bool _applying, _ending, _editing, _dirty, _changingValue;
        private int _labelWidth, _choicesVersion = -1;
        private Rectangle _clip, _field;
        private bool _comboPressed;
        internal RibbonQuickAccessComboBoxReference? QuickReference { get; private set; }
        private Control? _quickOpener;
        private Rectangle _quickAnchor;
        private int _listChoicesVersion;
        private Region? _ownedRegion;
        internal Control Editor => _editor;
        internal RibbonChoicePopup? Choice { get; private set; }
        internal bool ListOpen => Choice?.Visible ?? false;
        internal Rectangle ScreenField => QuickReference is not null && _quickOpener is { IsDisposed: false } opener ? opener.RectangleToScreen(_quickAnchor) : RectangleToScreen(_field);
        internal bool ProcessCharacter(char value)
        {
            if ((_item is not RibbonComboBoxItem { IsEditable: false } && QuickReference is null) || !ListOpen) return false;
            Choice!.Character(value); return true;
        }
        internal Presenter(RibbonControl ribbon, RibbonItem item)
        {
            _ribbon = ribbon; _item = item; _value = (IRibbonEditorItem)ribbon.CommandCatalog.Structure.Source(item); _accepted = _lastModel = _value.CaptureEdit();
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            TabStop = false;
            if (item is RibbonComboBoxItem)
            {
                var combo = new NativeComboBox(this) { TabStop = false, Name = item.Id, FlatStyle = FlatStyle.Flat, IntegralHeight = false, DrawMode = DrawMode.OwnerDrawVariable };
                _editor = combo;
                combo.SelectedIndexChanged += (_, _) => ReadNativeValue(true);
                combo.SelectionChangeCommitted += (_, _) =>
                {
                    if (_applying || IsDisposed) return;
                    ReadNativeValue(true);
                    if (IsDisposed) return;
                    if (!TryCommit()) RestoreList();
                };
            }
            else _editor = new NativeTextBox(this) { BorderStyle = BorderStyle.None, AutoSize = false, TabStop = false, Name = item.Id };
            Controls.Add(_editor);
            _editor.TextChanged += (_, _) => ReadNativeValue();
        }
        private (int Start, int Length) Selection => _editor switch
        {
            TextBox text => (text.SelectionStart, text.SelectionLength),
            ComboBox combo when ((RibbonComboBoxItem)_item).IsEditable => (combo.SelectionStart, combo.SelectionLength),
            _ => (0, 0)
        };
        private void SelectText(int start, int length)
        {
            start = Math.Min(start, _editor.Text.Length);
            length = Math.Min(length, _editor.Text.Length - start);
            if (_editor is TextBox text) text.Select(start, length);
            else if (_editor is ComboBox combo && ((RibbonComboBoxItem)_item).IsEditable) combo.Select(start, length);
        }
        private void ReadNativeValue(bool selection = false)
        {
            if (_applying || _item.IsDisposed) return;
            _changingValue = true;
            try
            {
                object before = _value.CaptureEdit();
                if (_editor is ComboBox combo && _item is RibbonComboBoxItem model)
                {
                    if (model.IsEditable && !selection) model.Value = combo.Text;
                    else if (combo.SelectedIndex >= 0) model.SelectedIndex = combo.SelectedIndex;
                    else if (model.IsEditable) model.Value = combo.Text;
                    else model.SelectedIndex = -1;
                }
                else _value.Value = _editor.Text;
                _dirty |= _value.IsEditChanged(before);
            }
            finally { _changingValue = false; }
        }
        internal void Present(Control surface, RibbonTarget target)
        {
            SyncQuickAccess();
            if (!_item.EffectiveEnabled && ListOpen) CloseChoice(true);
            if (!_item.EffectiveEnabled && _dirty && !TryCommit()) CancelEdit();
            if (IsDisposed) return;
            var (start, length) = Selection;
            _applying = true;
            try
            {
                bool external = _value.IsEditChanged(_lastModel);
                if (external && !_changingValue) CloseChoice(false);
                if (_editor is ComboBox native && _item is RibbonComboBoxItem choice)
                {
                    external |= _choicesVersion != choice.ChoicesVersion;
                    var style = choice.IsEditable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList;
                    if (_choicesVersion != choice.ChoicesVersion || native.DropDownStyle != style)
                    {
                        CloseChoice(false);
                        native.DropDownStyle = style;
                        if (_choicesVersion != choice.ChoicesVersion)
                        {
                            native.BeginUpdate();
                            try { native.Items.Clear(); native.Items.AddRange(choice.Items.Cast<object>().ToArray()); }
                            finally { native.EndUpdate(); }
                            _choicesVersion = choice.ChoicesVersion;
                        }
                    }
                }
                if (!ReferenceEquals(Parent, surface))
                {
                    CloseChoice(true);
                    Visible = false;
                    Parent = surface;
                }
                Font = _ribbon.Font;
                BackColor = _ribbon.Theme.Surface;
                Bounds = target.Bounds;
                _labelWidth = target.LabelWidth;
                var clip = Rectangle.Intersect(target.Bounds, target.Clip);
                clip.Offset(-target.Bounds.X, -target.Bounds.Y);
                if (_clip != clip)
                {
                    _clip = clip;
                    var previous = _ownedRegion;
                    Region = _ownedRegion = new Region(clip);
                    previous?.Dispose();
                }
                var field = _field = RibbonEditorLayout.Field(ClientRectangle, _item, _labelWidth, Font, _ribbon.DeviceDpi);
                if (_editor is NativeComboBox nativeCombo)
                {
                    nativeCombo.ApplyMetrics();
                    // Owner-drawn selection height can exceed PreferredHeight,
                    // especially in Touch. Center the actual native control in
                    // its painted field without requesting another native resize.
                    nativeCombo.SetBounds(field.Left, field.Top + (field.Height - nativeCombo.Height) / 2,
                        field.Width, nativeCombo.Height, BoundsSpecified.Location | BoundsSpecified.Width);
                    nativeCombo.ClipToText();
                }
                else
                {
                    int inset = RibbonLayoutEngine.Scale(4, _ribbon.DeviceDpi);
                    _editor.Bounds = new(field.Left + inset, field.Top + (field.Height - Font.Height) / 2,
                        Math.Max(1, field.Width - 2 * inset), Font.Height);
                }
                if (Choice is { } currentChoice && !currentChoice.Matches(QuickReference is null ? RectangleToScreen(field) : ScreenField)) CloseChoice(true);
                if (IsDisposed) return;
                if (_editor is ComboBox combo && _item is RibbonComboBoxItem model)
                {
                    if (combo.SelectedIndex != model.SelectedIndex) combo.SelectedIndex = model.SelectedIndex;
                    if (model.IsEditable && combo.Text != model.Value)
                    {
                        // ComboBox.Text also selects case-insensitive matches.
                        // Preserve exact free text and its explicit -1 selection.
                        if (model.SelectedIndex < 0 && combo.IsHandleCreated) ComboBoxNative.SetEditableText(combo, model.Value);
                        else combo.Text = model.Value;
                    }
                    combo.MaxLength = model.MaxLength;
                    Choice?.Select(model.SelectedIndex);
                }
                else if (_editor is TextBox text && _item is RibbonTextBoxItem textItem)
                {
                    if (text.Text != textItem.Value) text.Text = textItem.Value;
                    text.ReadOnly = textItem.ReadOnly;
                    text.MaxLength = textItem.MaxLength;
                }
                if (Selection != (start, length)) SelectText(start, length);
                if (external && !_changingValue)
                {
                    _accepted = _value.CaptureEdit();
                    _dirty = false;
                }
                _lastModel = _value.CaptureEdit();
                _editor.ForeColor = _item.EffectiveEnabled ? _ribbon.Theme.Foreground : SystemColors.GrayText;
                _editor.BackColor = BackColor;
                Enabled = _item.EffectiveEnabled;
            }
            finally { _applying = false; }
            Visible = true;
            Invalidate();
        }
        internal void Park()
        {
            SyncQuickAccess();
            if (QuickReference is null)
            {
                CloseChoice(true);
                if (_dirty && !TryCommit()) CancelEdit();
            }
            if (IsDisposed) return;
            var (start, length) = Selection;
            bool focused = _editor.ContainsFocus;
            Visible = false;
            if (!ReferenceEquals(Parent, _ribbon)) Parent = _ribbon;
            SelectText(start, length);
            _editing = false;
            if (focused && _ribbon.CanFocus) _ribbon.Focus();
        }
        internal void Release(Control surface)
        {
            if (ReferenceEquals(_quickOpener, surface)) CloseChoice(true);
            if (ReferenceEquals(Parent, surface)) Park();
        }
        private void SyncQuickAccess()
        {
            if (QuickReference is not { } reference || _changingValue || _ending) return;
            if (_value.IsEditChanged(_lastModel) || _item is RibbonComboBoxItem combo && combo.ChoicesVersion != _listChoicesVersion)
            {
                CloseChoice(false); _accepted = _lastModel = _value.CaptureEdit(); _dirty = false;
                return;
            }
            var targets = _quickOpener is RibbonPopupSurface popup ? popup.Targets : _ribbon.Snapshot.Targets;
            var target = targets.FirstOrDefault(t => ReferenceEquals(t.Element, reference));
            if (!RibbonQuickAccess.Available(reference) || !ReferenceEquals(reference.Source, _item) || _quickOpener is not { IsDisposed: false, Visible: true }
                || target is null || target.VisibleBounds != _quickAnchor || Choice is { } choice && !choice.Matches(ScreenField)) CloseChoice(true);
        }
        internal void OpenQuickAccess(RibbonQuickAccessComboBoxReference reference, Control opener, Rectangle anchor)
        {
            if (_item is not RibbonComboBoxItem model || IsDisposed) return;
            QuickReference = reference; _quickOpener = opener; _quickAnchor = anchor;
            _accepted = _lastModel = _value.CaptureEdit(); _listStart = _accepted; _dirty = false; _listChoicesVersion = model.ChoicesVersion;
            Choice = new(_ribbon, model, opener.RectangleToScreen(anchor), index =>
            {
                if (IsDisposed || !RibbonQuickAccess.Available(reference) || !ReferenceEquals(reference.Source, model)) return;
                var before = _value.CaptureEdit(); _changingValue = true;
                try { model.SelectedIndex = index; _dirty |= _value.IsEditChanged(before); _lastModel = _value.CaptureEdit(); }
                finally { _changingValue = false; }
            }, () =>
            {
                if (!CommitAndCloseList()) CloseChoice(true);
                FocusChoiceOpener(reference, opener);
            }, () => CloseChoice(true), opener is RibbonPopupSurface);
            _ribbon.Invalidate(); opener.Invalidate();
        }
        private void FocusChoiceOpener(RibbonQuickAccessComboBoxReference? reference, Control? opener)
        {
            if (IsDisposed || _ribbon.IsDisposed) return;
            if (reference is null) { FocusEditor(); return; }
            if (opener is RibbonPopupSurface { IsDisposed: false } popup && RibbonQuickAccess.Available(reference)) popup.FocusElement(reference);
            else if (opener is { IsDisposed: false } && RibbonQuickAccess.Available(reference)) _ribbon.FocusElement(reference, enterEditor: false);
        }
        internal bool FocusEditor()
        {
            if (!Visible || !Enabled) return false;
            var (start, length) = Selection;
            bool focused = _editor.Focus();
            SelectText(start, length);
            return focused;
        }
        private void BeginEdit()
        {
            if (!_editing && !_dirty) _accepted = _value.CaptureEdit();
            _editing = true;
            _ribbon.ExitKeyTips(false);
            if (Parent is RibbonPopupSurface popup) popup.EditorFocused(_item);
            else _ribbon.EditorFocused(_item);
            Invalidate();
        }
        private void ValidateEdit(CancelEventArgs args)
        {
            if (_ending || _item.IsDisposed) return;
            _ending = true;
            try { _value.ValidateEdit(args); }
            finally { _ending = false; }
        }
        private void AcceptEdit()
        {
            if (_ending || _item.IsDisposed) return;
            _ending = true;
            try
            {
                AcceptCore();
            }
            finally { _ending = false; }
        }
        private void AcceptCore()
        {
            bool changed = _value.IsEditChanged(_accepted);
            _accepted = _value.CaptureEdit();
            _dirty = false;
            _value.AcceptEdit(changed);
        }
        internal bool TryCommit()
        {
            if (_ending || _item.IsDisposed) return true;
            _ending = true;
            try
            {
                var reference = QuickReference;
                var args = new CancelEventArgs();
                _value.ValidateEdit(args);
                if (args.Cancel || IsDisposed || _item.IsDisposed) return false;
                if (reference is not null && (!ReferenceEquals(reference, QuickReference) || !ReferenceEquals(reference.Source, _item) || !RibbonQuickAccess.Available(reference)))
                { CloseChoice(true); return false; }
                if (reference is not null && _item is RibbonComboBoxItem model && model.ChoicesVersion != _listChoicesVersion)
                { CloseChoice(false); return false; }
                // Accepted callbacks may close/reparent this presentation. They
                // must not roll a successfully committed list preview back.
                _listStart = null;
                AcceptCore();
                return true;
            }
            finally { _ending = false; }
        }
        private void CancelEdit()
        {
            _ending = true;
            try { _dirty = false; _changingValue = true; _value.CancelEdit(_accepted); }
            finally { _ending = false; _changingValue = false; }
        }
        private void RestoreList()
        {
            if (_listStart is not { } start || IsDisposed || _item.IsDisposed) return;
            _listStart = null;
            _changingValue = true;
            try { _value.CancelEdit(start); _dirty = _value.IsEditChanged(_accepted); }
            finally { _changingValue = false; }
        }
        private void CloseChoice(bool cancel)
        {
            var popup = Choice;
            Choice = null;
            var opener = _quickOpener;
            QuickReference = null; _quickOpener = null;
            if (cancel) RestoreList();
            _listStart = null;
            popup?.Dispose();
            if (!IsDisposed) Invalidate();
            if (opener is { IsDisposed: false }) opener.Invalidate();
            _ribbon.Invalidate();
        }
        internal void SetDropDown(bool open)
        {
            if (_editor is not ComboBox combo || IsDisposed) return;
            if (!open) { CloseChoice(true); return; }
            if (QuickReference is not null) CloseChoice(true);
            if (ListOpen || !Visible || !Enabled || !combo.CanFocus) return;
            _ribbon.Editors.CloseLists(_item);
            _ribbon.Popups.PrepareChoice(Parent!);
            if (IsDisposed || !Visible || !_item.EffectiveEnabled) return;
            _listStart = _value.CaptureEdit();
            Choice = new(_ribbon, (RibbonComboBoxItem)_item, RectangleToScreen(_field),
                index => { if (!IsDisposed) combo.SelectedIndex = index; },
                () =>
                {
                    if (!CommitAndCloseList()) CloseChoice(true);
                    if (!IsDisposed) FocusEditor();
                }, () => CloseChoice(true));
            Invalidate();
        }
        internal bool CommitAndCloseList()
        {
            if (!TryCommit()) return false;
            if (!IsDisposed) CloseChoice(false);
            return true;
        }
        internal bool ProcessListKey(Keys keys)
        {
            if (!ListOpen) return false;
            var reference = QuickReference; var opener = _quickOpener;
            if (keys is Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown)
            { Choice!.Move(keys); return true; }
            if (keys == Keys.Escape) { CloseChoice(true); FocusChoiceOpener(reference, opener); return true; }
            if (keys is Keys.Enter or Keys.F4 or (Keys.Alt | Keys.Down) or (Keys.Alt | Keys.Up))
            { CommitAndCloseList(); return true; }
            if (keys is Keys.Tab or (Keys.Tab | Keys.Shift))
            {
                if (CommitAndCloseList() && !IsDisposed)
                {
                    if (reference is null) NavigateEditor(keys);
                    else if (opener is RibbonPopupSurface { IsDisposed: false } popup) popup.NavigateFromElement(reference, keys);
                    else { _ribbon.FocusElement(reference, enterEditor: false); _ribbon.ProcessRibbonKey(keys); }
                }
                return true;
            }
            return false;
        }
        private void NavigateEditor(Keys keys)
        {
            _ribbon.SetKeyboardFocusCues(true);
            if (Parent is RibbonPopupSurface popup) popup.NavigateFromEditor(_item, keys);
            else
            {
                _ribbon.FocusElement(_item, enterEditor: false);
                _ribbon.ProcessRibbonKey(keys);
            }
        }
        private bool ProcessEditorKey(Keys keys)
        {
            if (ProcessListKey(keys)) return true;
            if (_editor is ComboBox && keys is Keys.F4 or (Keys.Alt | Keys.Down))
            {
                SetDropDown(true);
                return true;
            }
            if (keys == Keys.Enter) { TryCommit(); return true; }
            if (keys == Keys.Escape)
            {
                CancelEdit();
                if (IsDisposed) return true;
                if (Parent is RibbonPopupSurface) _ribbon.Popups.CloseTop(true);
                else _ribbon.FocusElement(_item, enterEditor: false);
                return true;
            }
            if (keys is Keys.Tab or (Keys.Tab | Keys.Shift))
            {
                if (!CommitAndCloseList() || IsDisposed) return true;
                NavigateEditor(keys);
                return true;
            }
            return false;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || !FocusEditor()) return;
            if (_editor is ComboBox && _field.Contains(e.Location))
            {
                Capture = true;
                _comboPressed = true;
                Invalidate();
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            bool open = _comboPressed && e.Button == MouseButtons.Left && _field.Contains(e.Location);
            _comboPressed = false; Capture = false; Invalidate();
            if (open && _editor is ComboBox) SetDropDown(!ListOpen);
        }
        protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); _comboPressed = false; Invalidate(); }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (_editor is ComboBox) Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); if (_editor is ComboBox) Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var context = new RibbonPaintContext(e.Graphics, ClientRectangle, _ribbon.Theme, Font, _ribbon.DeviceDpi,
                Hot: _item.EffectiveEnabled && _field.Contains(PointToClient(MousePosition)),
                Pressed: _item.EffectiveEnabled && _comboPressed, Focused: _editor.ContainsFocus) { Density = _ribbon.Density };
            var field = RibbonEditorLayout.Field(ClientRectangle, _item, _labelWidth, Font, _ribbon.DeviceDpi);
            if (_item is RibbonTextBoxItem text) _ribbon.Painter.DrawTextBox(context, text, field);
            else _ribbon.Painter.DrawComboBox(context, (RibbonComboBoxItem)_item, field);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { CloseChoice(false); Region = null; _ownedRegion?.Dispose(); _ownedRegion = null; }
            base.Dispose(disposing);
        }
        private sealed class NativeTextBox(Presenter presenter) : TextBox
        {
            protected override bool ProcessCmdKey(ref Message msg, Keys keyData) =>
                presenter.ProcessEditorKey(keyData) || base.ProcessCmdKey(ref msg, keyData);
            protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); presenter.BeginEdit(); }
            protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); presenter._editing = false; presenter.Invalidate(); }
            protected override void OnValidating(CancelEventArgs e) { base.OnValidating(e); presenter.ValidateEdit(e); }
            protected override void OnValidated(EventArgs e) { base.OnValidated(e); presenter.AcceptEdit(); }
        }
        private sealed class NativeComboBox(Presenter presenter) : ComboBox
        {
            private Rectangle _textClip;
            private Region? _textRegion;
            private int _metricFontHeight = -1;
            private RibbonDensity? _metricDensity;
            internal void ApplyMetrics()
            {
                // Variable drawing keeps padded list rows independent of the
                // compact selection field, which aligns with the TextBox frame.
                bool applying = presenter._applying;
                presenter._applying = true;
                try
                {
                    if (_metricFontHeight == Font.Height && _metricDensity == presenter._ribbon.Density) return;
                    _metricFontHeight = Font.Height; _metricDensity = presenter._ribbon.Density;
                    ItemHeight = Math.Max(Font.Height, RibbonLayoutEngine.Scale(presenter._ribbon.Density == RibbonDensity.Touch ? 28 : 0, presenter._ribbon.DeviceDpi));
                }
                finally { presenter._applying = applying; }
            }
            internal void ClipToText()
            {
                var text = RibbonEditorLayout.ComboText(ClientRectangle, presenter._ribbon.DeviceDpi, presenter._ribbon.Density);
                text.Y = (Height - Font.Height) / 2; text.Height = Font.Height;
                if (_textClip != text)
                {
                    _textClip = text;
                    var previous = _textRegion;
                    Region = _textRegion = new Region(text);
                    previous?.Dispose();
                }
                ComboBoxNative.PlaceEdit(this, text);
            }
            protected override void OnMeasureItem(MeasureItemEventArgs e)
            {
                base.OnMeasureItem(e);
                e.ItemHeight = RibbonEditorLayout.ComboRowHeight(Font, presenter._ribbon.DeviceDpi, presenter._ribbon.Density);
            }
            protected override void OnDropDown(EventArgs e)
            {
                DropDownHeight = RibbonEditorLayout.ComboRowHeight(Font, presenter._ribbon.DeviceDpi, presenter._ribbon.Density) * Math.Max(1, Math.Min(Items.Count, MaxDropDownItems)) + 2;
                base.OnDropDown(e);
            }
            protected override void OnDrawItem(DrawItemEventArgs e)
            {
                var ribbon = presenter._ribbon;
                var item = (RibbonComboBoxItem)presenter._item;
                var context = new RibbonPaintContext(e.Graphics, e.Bounds, ribbon.Theme, Font, ribbon.DeviceDpi) { Density = ribbon.Density };
                if (e.State.HasFlag(DrawItemState.ComboBoxEdit))
                {
                    using var fill = new SolidBrush(ribbon.Theme.Surface);
                    e.Graphics.FillRectangle(fill, e.Bounds);
                    ribbon.Painter.DrawText(context with { Bounds = RibbonEditorLayout.ComboText(ClientRectangle, ribbon.DeviceDpi, ribbon.Density) },
                        item.Value, item.EffectiveEnabled ? ribbon.Theme.Foreground : SystemColors.GrayText,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
                }
                else if (e.Index >= 0 && e.Index < item.Items.Count)
                    ribbon.Painter.DrawComboBoxItem(context, item, e.Index, e.State.HasFlag(DrawItemState.Selected));
            }
            protected override void OnHandleCreated(EventArgs e)
            {
                bool applying = presenter._applying;
                presenter._applying = true;
                try
                {
                    base.OnHandleCreated(e);
                    var model = (RibbonComboBoxItem)presenter._item;
                    // Handle creation can precede population of the native Items.
                    // Present applies positive indices after copying the choices.
                    if (model.IsEditable && model.SelectedIndex < 0)
                    {
                        SelectedIndex = -1;
                        ComboBoxNative.SetEditableText(this, model.Value);
                    }
                }
                finally { presenter._applying = applying; }
            }
            protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); ApplyMetrics(); }
            protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); presenter.Invalidate(); }
            protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); presenter.Invalidate(); }
            protected override void OnMouseDown(MouseEventArgs e) { presenter._comboPressed = true; presenter.Invalidate(); base.OnMouseDown(e); }
            protected override void OnMouseUp(MouseEventArgs e) { presenter._comboPressed = false; presenter.Invalidate(); base.OnMouseUp(e); }
            protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); presenter._comboPressed = false; presenter.Invalidate(); }
            protected override void Dispose(bool disposing)
            {
                if (disposing) { Region = null; _textRegion?.Dispose(); _textRegion = null; }
                base.Dispose(disposing);
            }
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == 0x14F) // CB_SHOWDROPDOWN
                { presenter.SetDropDown(m.WParam != 0); m.Result = 0; return; }
                if (DropDownStyle == ComboBoxStyle.DropDownList && m.Msg is 0x201 or 0x202 or 0x203)
                {
                    var local = new Point(unchecked((short)m.LParam), unchecked((short)(m.LParam.ToInt64() >> 16)));
                    var point = presenter.PointToClient(PointToScreen(local));
                    var args = new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0);
                    if (m.Msg is 0x201 or 0x203) presenter.OnMouseDown(args); else presenter.OnMouseUp(args);
                    m.Result = 0; return;
                }
                base.WndProc(ref m);
            }
            protected override bool ProcessCmdKey(ref Message msg, Keys keyData) =>
                presenter.ProcessEditorKey(keyData) || base.ProcessCmdKey(ref msg, keyData);
            protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); presenter.BeginEdit(); }
            protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); presenter._editing = false; presenter.Invalidate(); }
            protected override void OnValidating(CancelEventArgs e) { base.OnValidating(e); presenter.ValidateEdit(e); }
            protected override void OnValidated(EventArgs e) { base.OnValidated(e); presenter.AcceptEdit(); }
        }
    }
}
