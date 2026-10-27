using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

internal sealed class RibbonInputFilter(RibbonControl ribbon) : IMessageFilter, IDisposable
{
    private bool _attached, _altArmed;
    internal void Attach()
    {
        if (_attached || ribbon.IsInDesignMode)
            return;
        Application.AddMessageFilter(this);
        _attached = true;
    }
    internal void Detach()
    {
        if (!_attached)
            return;
        Application.RemoveMessageFilter(this);
        _attached = false;
        _altArmed = false;
    }
    public bool PreFilterMessage(ref Message m)
    {
        ribbon.ScreenTips.Observe(ref m);
        if (ribbon.FindForm() is not { } form || (!form.ContainsFocus && !ribbon.Popups.ContainsFocus && !ribbon.Editors.HasOpenList))
            return false;
        if (m.Msg is 0x201 or 0x204 or 0x207)
        {
            ribbon.SetKeyboardFocusCues(false);
            ribbon.ExitKeyTips(false);
            uint position = GetMessagePos();
            var point = new Point(unchecked((short)position), unchecked((short)(position >> 16)));
            if (!ribbon.Editors.ContainsListPoint(point))
            {
                ribbon.Editors.OutsidePress(point);
                ribbon.Popups.OutsidePress(point);
            }
            return false;
        }
        if (m.Msg == 0x102 && ribbon.Editors.ProcessListCharacter((char)(int)m.WParam)) return true;
        if (m.Msg is 0x104 or 0x100)
        {
            var key = (Keys)(int)m.WParam;
            if (key == Keys.F1 && Control.ModifierKeys == Keys.Control && ribbon.Enabled && ribbon.Visible
                && (form.ContainsFocus || ribbon.Popups.ContainsFocus))
            {
                _altArmed = false;
                // Holding the chord must not repeatedly flip the mode.
                if (((long)m.LParam & (1L << 30)) == 0) ribbon.ToggleMinimizedByUser();
                return true;
            }
            if (ribbon.Editors.ProcessListKey(key | Control.ModifierKeys))
            {
                _altArmed = false;
                return true;
            }
            if (key == Keys.Menu && !Control.ModifierKeys.HasFlag(Keys.Control))
            {
                _altArmed = true;
                return false;
            }
            if (key != Keys.Menu)
                _altArmed = false;
            // Observe Tab before normal WinForms traversal transfers focus into
            // the ribbon; do not consume the host's navigation message.
            if (key == Keys.Tab && (Control.ModifierKeys & (Keys.Control | Keys.Alt)) == 0)
                ribbon.SetKeyboardFocusCues(true);
            // ToolStrip's menu loop can consume Escape before the hosted control
            // sees ProcessCmdKey. Dismiss exactly one library-owned level here.
            if (key == Keys.Escape && ribbon.Popups.HasOpen && !ribbon.Editors.ContainsFocus && Control.ModifierKeys == Keys.None)
            {
                ribbon.SetKeyboardFocusCues(true);
                ribbon.Popups.CloseTop(true);
                return true;
            }
            if (key == Keys.F10 && Control.ModifierKeys == Keys.None)
            {
                if (!ribbon.Editors.CommitFocused()) return true;
                ribbon.ToggleKeyTips();
                return true;
            }
            if (key == Keys.F6 && (Control.ModifierKeys & (Keys.Control | Keys.Alt)) == 0)
            {
                if (!ribbon.Editors.CommitFocused()) return true;
                ribbon.SetKeyboardFocusCues(true);
                if (ribbon.ContainsFocus || ribbon.Popups.ContainsFocus)
                {
                    ribbon.Popups.CloseAll(false);
                    ribbon.ExitKeyTips(false);
                    form.SelectNextControl(ribbon, true, true, true, true);
                }
                else
                    ribbon.Focus();
                return true;
            }
            if (ribbon.KeyTipsActive && (Control.ModifierKeys & (Keys.Control | Keys.Alt)) == 0)
                return ribbon.ProcessRibbonKey(key | Control.ModifierKeys);
        }
        if (m.Msg == 0x105 && (Keys)(int)m.WParam == Keys.Menu && _altArmed)
        {
            _altArmed = false;
            if (!ribbon.Editors.CommitFocused()) return true;
            ribbon.ToggleKeyTips();
            return true;
        }
        return false;
    }
    public void Dispose() => Detach();
    [DllImport("user32.dll")] private static extern uint GetMessagePos();
}
