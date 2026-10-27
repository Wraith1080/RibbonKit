using RibbonKit.WinForms.Layout;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

// Logical constants are local to this component until the planned theming inventory.
internal sealed record RibbonScreenTipLayout(Size Size, Rectangle Title, Rectangle Description)
{
    internal const TextFormatFlags TextFlags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.TextBoxControl;
    internal static string TitleFor(RibbonItem item) => string.IsNullOrEmpty(item.ScreenTipTitle) ? item.Text : item.ScreenTipTitle;

    internal static RibbonScreenTipLayout Measure(string title, string description, Font font, int dpi, Size available)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        int padding = Math.Min(S(12), Math.Max(0, (Math.Min(available.Width, available.Height) - 2) / 2));
        int width = Math.Max(1, Math.Min(S(320), available.Width) - 2 * padding);
        using var titleFont = new Font(font, font.Style | FontStyle.Bold);
        var titleSize = TextRenderer.MeasureText(title, titleFont, new(width, int.MaxValue), TextFlags);
        var descriptionSize = string.IsNullOrEmpty(description) ? Size.Empty : TextRenderer.MeasureText(description, font, new(width, int.MaxValue), TextFlags);
        int gap = descriptionSize.IsEmpty || title.Length == 0 ? 0 : S(6);
        int height = Math.Max(1, available.Height - 2 * padding);
        int titleHeight = title.Length == 0 ? 0 : Math.Min(height, titleSize.Height);
        int descriptionHeight = Math.Min(Math.Max(0, height - titleHeight - gap), descriptionSize.Height);
        int contentWidth = Math.Min(width, Math.Max(titleSize.Width, descriptionSize.Width));
        var size = new Size(Math.Max(1, contentWidth + 2 * padding), Math.Min(available.Height, titleHeight + gap + descriptionHeight + 2 * padding));
        return new(size, new(padding, padding, contentWidth, titleHeight), descriptionHeight == 0 ? Rectangle.Empty : new(padding, padding + titleHeight + gap, contentWidth, descriptionHeight));
    }

    internal static Rectangle Place(Rectangle anchor, Point pointer, Size size, Rectangle work, int dpi)
    {
        int gap = RibbonLayoutEngine.Scale(8, dpi);
        int y = Math.Max(anchor.Bottom + gap, pointer.Y + gap);
        if (y + size.Height > work.Bottom) y = Math.Min(anchor.Top - gap - size.Height, pointer.Y - gap - size.Height);
        return new(Math.Clamp(anchor.Left, work.Left, Math.Max(work.Left, work.Right - size.Width)),
            Math.Clamp(y, work.Top, Math.Max(work.Top, work.Bottom - size.Height)), size.Width, size.Height);
    }
}

// A passive owned HWND, deliberately outside ToolStrip's active menu/focus stack.
internal sealed class RibbonScreenTipWindow : Form
{
    private readonly RibbonControl _ribbon;
    private readonly RibbonScreenTipLayout _layout;
    private readonly string _title, _description;
    private readonly PopupChromeWindow _chrome = new();
    private readonly int _radius;
    internal RibbonScreenTipWindow(RibbonControl ribbon, RibbonItem item, Rectangle anchor, Point pointer)
    {
        _ribbon = ribbon;
        _title = RibbonScreenTipLayout.TitleFor(item); _description = item.ScreenTipDescription;
        var work = Screen.FromRectangle(anchor).WorkingArea;
        int inset = RibbonLayoutEngine.Scale(8, ribbon.DeviceDpi);
        if (work.Width > 2 * inset && work.Height > 2 * inset) work.Inflate(-inset, -inset);
        _layout = RibbonScreenTipLayout.Measure(_title, _description, ribbon.Font, ribbon.DeviceDpi, work.Size);
        FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None; CausesValidation = false; TabStop = false;
        SetStyle(ControlStyles.Selectable, false);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Bounds = RibbonScreenTipLayout.Place(anchor, pointer, _layout.Size, work, ribbon.DeviceDpi);
        _radius = Math.Clamp(ribbon.Painter.GetPopupCornerRadius(ribbon.Theme, ribbon.DeviceDpi), 0, Math.Min(Width, Height) / 2);
        Region = _radius > 0 ? RoundedPainter.CreateOpaqueRegion(Size, _radius) : null;
    }
    protected override bool ShowWithoutActivation => true;
    internal void RaiseAbovePopups()
    {
        // ToolStrip menus are topmost. Raise only after the passive Show path:
        // Form.TopMost during creation can activate the HWND despite that path.
        SetWindowPos(Handle, -1, 0, 0, 0, 0, 0x0013); // HWND_TOPMOST, NOMOVE | NOSIZE | NOACTIVATE
        _chrome.Update(this, _ribbon, _radius);
    }
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x080000A0; // WS_EX_NOACTIVATE | TOOLWINDOW | TRANSPARENT
            return cp;
        }
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x84) { m.Result = -1; return; } // HTTRANSPARENT
        if (m.Msg == 0x21) { m.Result = 3; return; } // MA_NOACTIVATE
        base.WndProc(ref m);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var context = new RibbonPaintContext(e.Graphics, ClientRectangle, _ribbon.Theme, _ribbon.Font, _ribbon.DeviceDpi);
        _ribbon.Painter.DrawPopupFrame(context);
        _ribbon.Painter.DrawScreenTip(context, _title, _description, _layout.Title, _layout.Description);
        _chrome.Update(this, _ribbon, _radius);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) _chrome.Dispose();
        base.Dispose(disposing);
    }
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags);
}
