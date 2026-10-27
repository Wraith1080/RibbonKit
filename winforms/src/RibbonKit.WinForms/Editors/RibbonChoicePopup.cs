using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// The ComboBox remains the editor. Its independently owned native ListBox supplies
// scrolling/search, inside the same frame and shadow as command/group popups.
internal sealed class RibbonChoicePopup : IDisposable
{
    private readonly RibbonControl _ribbon;
    private readonly RibbonComboBoxItem _item;
    private readonly Action<int> _preview;
    private readonly Action _accept, _cancel;
    private readonly RibbonPopupWindow _window;
    private readonly ChoiceList _list;
    private readonly Form? _form;
    private readonly RibbonTheme _theme;
    private readonly Rectangle _anchor;
    private readonly int _dpi;
    private bool _silent, _disposed, _activationPending;
    internal bool Visible => !_disposed && _window.Visible;
    internal RibbonChoicePopup(RibbonControl ribbon, RibbonComboBoxItem item, Rectangle anchor,
        Action<int> preview, Action accept, Action cancel, bool submenu = false)
    {
        ribbon.ScreenTips.Cancel();
        _ribbon = ribbon; _item = item; _anchor = anchor; _theme = ribbon.Theme;
        _dpi = ribbon.DeviceDpi;
        _preview = preview; _accept = accept; _cancel = cancel;
        _list = new(this)
        {
            Name = item.Id + ".choices", Font = ribbon.Font, BackColor = ribbon.Theme.Surface,
            ForeColor = ribbon.Theme.Foreground, BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed, IntegralHeight = false, TabStop = false,
            CausesValidation = false, ItemHeight = RibbonEditorLayout.ComboRowHeight(ribbon.Font, ribbon.DeviceDpi)
        };
        _list.Items.AddRange(item.Items.Cast<object>().ToArray());
        _list.SelectedIndex = item.SelectedIndex;
        _list.SelectedIndexChanged += (_, _) => { if (!_silent && !_disposed && _list.SelectedIndex >= 0) _preview(_list.SelectedIndex); };
        // A QAT anchor may be narrower than every choice. Measure on the
        // owning surface's DPI-aware DC, including both text insets and frame.
        using var measure = ribbon.CreateGraphics();
        int width = Math.Max(anchor.Width, item.Items.Select(text => TextRenderer.MeasureText(measure, text, ribbon.Font,
            new(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width
            + 2 * RibbonLayoutEngine.Scale(6, ribbon.DeviceDpi) + 2).DefaultIfEmpty(anchor.Width).Max());
        if (item.Items.Count > 8) width += SystemInformation.GetVerticalScrollBarWidthForDpi(ribbon.DeviceDpi);
        var bounds = CalculateBounds(anchor, Screen.FromRectangle(anchor).WorkingArea, width, _list.ItemHeight, item.Items.Count, ribbon.DeviceDpi, submenu);
        _list.Size = new(bounds.Width - 2, bounds.Height - 2);
        var host = new ToolStripControlHost(_list) { AutoSize = false, Size = _list.Size, Margin = Padding.Empty, Padding = Padding.Empty };
        _window = new(ribbon, QueueActivationCheck) { Size = bounds.Size, Name = item.Id + ".popup" };
        _window.Items.Add(host);
        _window.Closed += (_, _) => { if (!_disposed) _cancel(); };
        _form = ribbon.FindForm();
        if (_form is not null)
        {
            _form.Deactivate += OnDeactivate;
            _form.LocationChanged += OnMove;
            _form.ResizeBegin += OnMove;
        }
        _window.Show(bounds.Location);
    }
    internal static Rectangle CalculateBounds(Rectangle anchor, Rectangle work, int width, int rowHeight, int count, int dpi, bool submenu = false)
    {
        if (submenu) return RibbonPopupManager.CalculateBounds(anchor, work, new(width, Math.Max(1, Math.Min(count, 8)) * rowHeight + 2), true, dpi);
        int inset = RibbonLayoutEngine.Scale(8, dpi);
        if (work.Width > 2 * inset + 2 && work.Height > 2 * inset + rowHeight + 2) work.Inflate(-inset, -inset);
        int below = Math.Max(0, work.Bottom - anchor.Bottom), above = Math.Max(0, anchor.Top - work.Top);
        int wanted = Math.Max(1, Math.Min(count, 8)) * rowHeight + 2;
        bool up = wanted > below && above > below;
        int available = Math.Max(0, up ? above : below);
        int rows = Math.Max(1, (available - 2) / Math.Max(1, rowHeight));
        int height = Math.Min(work.Height, Math.Min(wanted, rows * rowHeight + 2));
        width = Math.Clamp(width, Math.Min(3, work.Width), work.Width);
        return new(Math.Clamp(anchor.Left, work.Left, work.Right - width),
            Math.Clamp(up ? anchor.Top - height : anchor.Bottom, work.Top, work.Bottom - height), width, height);
    }
    internal bool Matches(Rectangle anchor) => _dpi == _ribbon.DeviceDpi && _theme == _ribbon.Theme && _anchor == anchor && _list.Font.Equals(_ribbon.Font);
    internal bool ContainsPoint(Point point) => Visible && _window.Bounds.Contains(point)
        && (_window.Region is null || _window.Region.IsVisible(_window.PointToClient(point)));
    internal bool OwnsWindow(nint handle) => Visible && (handle == _window.Handle || handle == _list.Handle || IsChild(_window.Handle, handle));
    internal void Select(int index)
    {
        if (_disposed || _list.SelectedIndex == index) return;
        _silent = true;
        try { _list.SelectedIndex = index; }
        finally { _silent = false; }
    }
    internal void Move(Keys key)
    {
        if (_disposed || _list.Items.Count == 0) return;
        _list.ClearHot();
        int page = Math.Max(1, _list.ClientSize.Height / _list.ItemHeight);
        int index = _list.SelectedIndex;
        index = key switch { Keys.Home => 0, Keys.End => _list.Items.Count - 1,
            Keys.Up => index - 1, Keys.PageUp => index - page, Keys.PageDown => index + page, _ => index + 1 };
        _list.SelectedIndex = Math.Clamp(index, 0, _list.Items.Count - 1);
    }
    internal void Character(char value)
    {
        if (!_disposed) { _list.ClearHot(); SendMessage(_list.Handle, 0x102, value, 1); } // native ListBox incremental search
    }
    private void OnMove(object? sender, EventArgs e) => _cancel();
    private void OnDeactivate(object? sender, EventArgs e) => QueueActivationCheck();
    private void QueueActivationCheck()
    {
        if (_disposed || _activationPending || !_ribbon.IsHandleCreated) return;
        _activationPending = true;
        _ribbon.BeginInvoke(() =>
        {
            _activationPending = false;
            if (!Visible) return;
            nint foreground = GetForegroundWindow();
            if (foreground == _form?.Handle || OwnsWindow(foreground) || _ribbon.Popups.OwnsWindow(foreground)) return;
            _cancel();
        });
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_form is not null)
        {
            _form.Deactivate -= OnDeactivate;
            _form.LocationChanged -= OnMove;
            _form.ResizeBegin -= OnMove;
        }
        _window.Close(); _window.Dispose();
    }
    private sealed class ChoiceList(RibbonChoicePopup owner) : ListBox
    {
        private int _pressed = -1;
        private int _hot = -1;
        private Point _pointer = MousePosition;
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (owner._disposed || e.Index < 0 || e.Index >= owner._item.Items.Count) return;
            var ribbon = owner._ribbon;
            // DRAWITEM carries the row's current native state even during a
            // selection transition, when querying SelectedIndex can still
            // return the previous row and leave its selected paint behind.
            ribbon.Painter.DrawComboBoxItem(new(e.Graphics, e.Bounds, ribbon.Theme, Font, ribbon.DeviceDpi,
                Hot: e.Index == _hot, Pressed: e.Index == _pressed && Capture),
                owner._item, e.Index, (e.State & DrawItemState.Selected) != 0);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            // Opening, scrolling and repainting can synthesize WM_MOUSEMOVE.
            // Only real motion resumes pointer highlighting after keyboard use.
            var pointer = MousePosition;
            if (_pointer == pointer) return;
            _pointer = pointer;
            int index = IndexFromPoint(e.Location);
            SetHot(ClientRectangle.Contains(e.Location) ? index : -1);
        }
        internal void ClearHot() => SetHot(-1);
        private void SetHot(int index)
        {
            if (_hot == index) return;
            int previous = _hot; _hot = index;
            InvalidateRow(previous); InvalidateRow(index);
        }
        private void InvalidateRow(int index)
        {
            if (index >= 0 && index < Items.Count) Invalidate(GetItemRectangle(index));
        }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); ClearHot(); }
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e); int previous = _pressed; _pressed = -1; InvalidateRow(previous);
        }
        protected override void WndProc(ref Message m)
        {
            // A row press must not transfer focus out of the native text editor.
            // Nonclient scrollbar messages keep the ListBox's native handling.
            if (m.Msg is 0x201 or 0x202)
            {
                var point = new Point(unchecked((short)m.LParam), unchecked((short)(m.LParam.ToInt64() >> 16)));
                int index = ClientRectangle.Contains(point) ? IndexFromPoint(point) : -1;
                if (m.Msg == 0x201)
                {
                    _pressed = index; Capture = index >= 0;
                    SetHot(index); InvalidateRow(index);
                }
                else
                {
                    bool accept = index >= 0 && index == _pressed;
                    int pressed = _pressed;
                    _pressed = -1; Capture = false;
                    InvalidateRow(pressed);
                    if (accept)
                    {
                        SelectedIndex = index;
                        if (!owner._disposed) owner._accept();
                    }
                }
                m.Result = 0; return;
            }
            base.WndProc(ref m);
        }
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool IsChild(nint parent, nint child);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
}
