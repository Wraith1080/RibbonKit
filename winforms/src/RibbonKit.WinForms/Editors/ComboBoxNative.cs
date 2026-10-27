using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

internal static class ComboBoxNative
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Info
    {
        internal int Size;
        internal Rect Item, Button;
        internal int ButtonState;
        internal nint Combo, Edit, List;
    }
    internal static void SetEditableText(ComboBox combo, string value) => SetWindowText(combo.Handle, value);
    internal static void PlaceEdit(ComboBox combo, Rectangle bounds)
    {
        if (!combo.IsHandleCreated || combo.DropDownStyle != ComboBoxStyle.DropDown) return;
        var info = new Info { Size = Marshal.SizeOf<Info>() };
        if (GetComboBoxInfo(combo.Handle, ref info) && info.Edit != 0)
        {
            if (GetWindowRect(info.Edit, out var current))
            {
                MapWindowPoints(0, combo.Handle, ref current, 2);
                if (current.Left == bounds.Left && current.Top == bounds.Top && current.Right == bounds.Right && current.Bottom == bounds.Bottom) return;
            }
            SetWindowPos(info.Edit, 0, bounds.X, bounds.Y, bounds.Width, bounds.Height, 0x14); // no z-order or activation change
        }
    }
    [DllImport("user32.dll")] private static extern bool GetComboBoxInfo(nint hwnd, ref Info info);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint hwnd, out Rect rect);
    [DllImport("user32.dll")] private static extern int MapWindowPoints(nint from, nint to, ref Rect rect, uint points);
    [DllImport("user32.dll", EntryPoint = "SetWindowTextW", CharSet = CharSet.Unicode)] private static extern bool SetWindowText(nint hwnd, string text);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);
}
