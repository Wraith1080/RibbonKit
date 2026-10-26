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
        if (ribbon.FindForm() is not { } form || (!form.ContainsFocus && !ribbon.Popups.ContainsFocus))
            return false;
        if (m.Msg is 0x201 or 0x204 or 0x207)
        {
            ribbon.ExitKeyTips(false);
            uint position = GetMessagePos();
            var point = new Point(unchecked((short)position), unchecked((short)(position >> 16)));
            ribbon.Popups.OutsidePress(point);
            return false;
        }
        if (m.Msg is 0x104 or 0x100)
        {
            var key = (Keys)(int)m.WParam;
            if (key == Keys.Menu && !Control.ModifierKeys.HasFlag(Keys.Control))
            {
                _altArmed = true;
                return false;
            }
            if (key != Keys.Menu)
                _altArmed = false;
            // ToolStrip's menu loop can consume Escape before the hosted control
            // sees ProcessCmdKey. Dismiss exactly one library-owned level here.
            if (key == Keys.Escape && ribbon.Popups.HasOpen && Control.ModifierKeys == Keys.None)
            {
                ribbon.Popups.CloseTop(true);
                return true;
            }
            if (key == Keys.F10 && Control.ModifierKeys == Keys.None)
            {
                ribbon.ToggleKeyTips();
                return true;
            }
            if (key == Keys.F6 && (Control.ModifierKeys & (Keys.Control | Keys.Alt)) == 0)
            {
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
            ribbon.ToggleKeyTips();
            return true;
        }
        return false;
    }
    public void Dispose() => Detach();
    [DllImport("user32.dll")] private static extern uint GetMessagePos();
}
