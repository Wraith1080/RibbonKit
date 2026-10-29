using System.ComponentModel;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

/// <summary>An optional native caption host for a direct, top-docked ribbon. Document controls, DPI startup and application lifetime remain application-owned.</summary>
public class RibbonForm : Form
{
    private RibbonControl? _ribbon;
    private int _hotCaption;
    private int _pressedCaption;
    private bool _updating;
    private int _lastCaptionHeight;
    /// <summary>Creates a resizable native ribbon window with standard system-menu and caption semantics.</summary>
    public RibbonForm()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
    }

    /// <summary>Gets or sets a borrowed ribbon already added directly to Controls with Dock=Top. Caption integration requires empty Form padding and no other visible top-docked sibling. Association does not reparent or dispose it. Null releases the association and restores its ordinary toolbar fallback.</summary>
    [Category("Behavior"), DefaultValue(null)]
    public RibbonControl? Ribbon
    {
        get => _ribbon;
        set
        {
            if (ReferenceEquals(_ribbon, value)) return;
            if (value is not null)
            {
                value.VerifyAccess();
                if (value.IsDisposed || !ReferenceEquals(value.Parent, this) || value.Dock != DockStyle.Top || value.WindowHost is not null)
                    throw new ArgumentException("Associate a live, direct, top-docked ribbon without another window host.", nameof(value));
            }
            var old = _ribbon;
            old?.VerifyAccess();
            if (old is not null)
            {
                old.ParentChanged -= RibbonChanged; old.DockChanged -= RibbonChanged; old.VisibleChanged -= RibbonChanged; old.Disposed -= RibbonDisposed;
                old.WindowHost = null;
            }
            _ribbon = value;
            if (value is not null)
            {
                value.WindowHost = this;
                value.ParentChanged += RibbonChanged; value.DockChanged += RibbonChanged; value.VisibleChanged += RibbonChanged; value.Disposed += RibbonDisposed;
            }
            if (old is { IsDisposed: false, Disposing: false }) old.ModelChanged();
            RefreshRibbon();
        }
    }
    internal bool SupportsRibbon(RibbonControl ribbon) => !IsDisposed && !Disposing && ReferenceEquals(Ribbon, ribbon)
        && ReferenceEquals(ribbon.Parent, this) && ribbon.Dock == DockStyle.Top && ribbon.Visible
        && Padding == Padding.Empty && !Controls.Cast<Control>().Any(c => !ReferenceEquals(c, ribbon) && c.Visible && c.Dock == DockStyle.Top)
        && FormBorderStyle is FormBorderStyle.Sizable or FormBorderStyle.FixedSingle or FormBorderStyle.Fixed3D;
    internal int CaptionHeight => RibbonLayoutEngine.Scale(Math.Max(36, Ribbon is { Density: RibbonDensity.Touch } ? 48 : 36), DeviceDpi);
    internal bool CustomCaption => FormBorderStyle is FormBorderStyle.Sizable or FormBorderStyle.FixedSingle or FormBorderStyle.Fixed3D;
    internal RibbonCaptionGeometry Caption => RibbonCaptionGeometry.Measure(ClientSize.Width, CaptionHeight, DeviceDpi, ControlBox, MinimizeBox, MaximizeBox, ShowIcon);
    internal Rectangle QuickAccessSlot(RibbonControl ribbon)
    {
        var geometry = Caption;
        int available = Math.Max(0, geometry.Buttons.Left - geometry.ContentLeft);
        int cap = RibbonLayoutEngine.Scale(ribbon.QuickAccessMaxWidth, DeviceDpi);
        int minimum = RibbonLayoutEngine.Scale(RibbonDensityMetrics.For(ribbon.Density).QuickAccessButton + 4, DeviceDpi);
        // At narrow widths preserve the overflow target before sacrificing the title.
        int width = Math.Min(available, Math.Min(cap, Math.Max(minimum, available - RibbonLayoutEngine.Scale(100, DeviceDpi))));
        return new(geometry.ContentLeft, 0, Math.Max(0, width), CaptionHeight);
    }
    private void RibbonDisposed(object? sender, EventArgs e) => Ribbon = null;
    private void RibbonChanged(object? sender, EventArgs e)
    {
        if (_ribbon is { } ribbon && (!ReferenceEquals(ribbon.Parent, this) || ribbon.Dock != DockStyle.Top)) Ribbon = null;
        else RefreshRibbon();
    }
    private void RefreshRibbon()
    {
        if (_updating || IsDisposed || Disposing) return;
        _updating = true;
        try { _ribbon?.ModelChanged(); PerformLayout(); Invalidate(); }
        finally { _updating = false; }
    }
    /// <inheritdoc />
    public override Rectangle DisplayRectangle
    {
        get
        {
            var rectangle = base.DisplayRectangle;
            if (CustomCaption && (_ribbon is null || !SupportsRibbon(_ribbon)))
            { int height = Math.Min(CaptionHeight, rectangle.Height); rectangle.Y += height; rectangle.Height -= height; }
            return rectangle;
        }
    }
    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        FrameChanged();
    }
    private void FrameChanged()
    {
        if (!IsHandleCreated) return;
        SetWindowPos(Handle, 0, 0, 0, 0, 0, 0x37); // FRAMECHANGED | NOACTIVATE | NOMOVE | NOSIZE | NOZORDER
        RefreshRibbon();
    }
    /// <inheritdoc />
    protected override void OnDpiChanged(DpiChangedEventArgs e) { base.OnDpiChanged(e); FrameChanged(); }
    /// <inheritdoc />
    protected override void OnResize(EventArgs e) { base.OnResize(e); RefreshRibbon(); }
    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int height = _ribbon?.WindowCaptionHeight ?? 0;
        if (_lastCaptionHeight == height) return;
        _lastCaptionHeight = height;
        RefreshRibbon();
    }
    /// <inheritdoc />
    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); _ribbon?.Invalidate(); }
    /// <inheritdoc />
    protected override void OnActivated(EventArgs e) { base.OnActivated(e); Invalidate(); _ribbon?.Invalidate(); }
    /// <inheritdoc />
    protected override void OnDeactivate(EventArgs e) { base.OnDeactivate(e); _hotCaption = 0; Invalidate(); _ribbon?.Invalidate(); }
    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (CustomCaption && (_ribbon is null || !_ribbon.HasWindowCaption)) PaintCaption(e.Graphics, null);
    }
    internal void PaintCaption(Graphics graphics, RibbonControl? ribbon)
    {
        var geometry = Caption; var theme = ribbon?.Theme ?? RibbonThemes.Office2024Light;
        using var background = new SolidBrush(theme.Background);
        graphics.FillRectangle(background, new Rectangle(0, 0, ClientSize.Width, CaptionHeight));
        if (ShowIcon && Icon is { } icon && !geometry.Icon.IsEmpty) graphics.DrawIcon(icon, geometry.Icon);
        int left = geometry.ContentLeft;
        if (ribbon?.EffectiveQuickAccessPosition == RibbonQuickAccessPosition.TitleBar && ribbon.HasQuickAccess) left = ribbon.Snapshot.QuickAccessBounds.Right + RibbonLayoutEngine.Scale(10, DeviceDpi);
        var text = Rectangle.FromLTRB(Math.Min(left, geometry.Buttons.Left), 0, geometry.Buttons.Left, CaptionHeight);
        TextRenderer.DrawText(graphics, Text, Font, text, ContainsFocus ? theme.Foreground : theme.SecondaryText,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        foreach (var (bounds, hit) in geometry.Targets)
        {
            if (bounds.IsEmpty) continue;
            if (_hotCaption == hit) { using var hover = new SolidBrush(hit == 20 ? Color.FromArgb(232, 17, 35) : _pressedCaption == hit ? theme.Pressed.Start : theme.Hover.Start); graphics.FillRectangle(hover, bounds); }
            using var pen = new Pen(_hotCaption == 20 && hit == 20 ? Color.White : theme.Foreground, Math.Max(1, DeviceDpi / 96f));
            int size = RibbonLayoutEngine.Scale(10, DeviceDpi), x = bounds.Left + (bounds.Width - size) / 2, y = bounds.Top + (bounds.Height - size) / 2;
            if (hit == 8) graphics.DrawLine(pen, x, y + size / 2, x + size, y + size / 2);
            else if (hit == 9)
            {
                if (WindowState == FormWindowState.Maximized) { graphics.DrawRectangle(pen, x + 3, y, size - 3, size - 3); graphics.FillRectangle(background, x, y + 3, size - 3, size - 3); graphics.DrawRectangle(pen, x, y + 3, size - 3, size - 3); }
                else graphics.DrawRectangle(pen, x, y, size, size);
            }
            else { graphics.DrawLine(pen, x, y, x + size, y + size); graphics.DrawLine(pen, x + size, y, x, y + size); }
        }
    }
    internal int CaptionHitTest(Point point)
    {
        if (!CustomCaption) return 1;
        if (WindowState != FormWindowState.Maximized && FormBorderStyle == FormBorderStyle.Sizable)
        {
            int edge = Math.Max(1, GetSystemMetricsForDpi(33, (uint)DeviceDpi) + GetSystemMetricsForDpi(92, (uint)DeviceDpi));
            bool left = point.X < edge, right = point.X >= ClientSize.Width - edge;
            bool top = point.Y < edge, bottom = point.Y >= ClientSize.Height - edge;
            if (top) return left ? 13 : right ? 14 : 12;
            if (bottom) return left ? 16 : right ? 17 : 15;
            if (left) return 10;
            if (right) return 11;
        }
        if (point.Y < 0 || point.Y >= CaptionHeight) return 1;
        var geometry = Caption;
        foreach (var (bounds, hit) in geometry.Targets) if (bounds.Contains(point)) return hit;
        if (geometry.Icon.Contains(point) && ControlBox) return 3;
        if (_ribbon is { HasWindowCaption: true } ribbon && ribbon.Snapshot.QuickAccessBounds.Contains(point)) return 1;
        return 2;
    }
    /// <inheritdoc />
    protected override void WndProc(ref Message m)
    {
        if (CustomCaption)
        {
            if (m.Msg == 0x83 && m.WParam != 0) // NCCALCSIZE: retain the resize frame, reclaim only the caption.
            {
                var original = Marshal.PtrToStructure<NativeRect>(m.LParam);
                base.WndProc(ref m);
                var client = Marshal.PtrToStructure<NativeRect>(m.LParam);
                client.Top = original.Top + (IsZoomed(Handle) ? client.Left - original.Left : 0);
                Marshal.StructureToPtr(client, m.LParam, false); m.Result = 0; return;
            }
            if (m.Msg == 0x84)
            {
                var point = PointToClient(new(unchecked((short)(long)m.LParam), unchecked((short)((long)m.LParam >> 16))));
                // DWM receives the message first; our measured client caption supplies custom targets.
                bool handled = DwmDefWindowProc(Handle, m.Msg, m.WParam, m.LParam, out var dwmResult);
                int hit = CaptionHitTest(point);
                if (hit != 1) { m.Result = hit; return; }
                if (point.Y >= CaptionHeight && handled) { m.Result = dwmResult; return; }
            }
            if (m.Msg == 0xA1 && (int)m.WParam is 8 or 9 or 20)
            {
                _pressedCaption = _hotCaption = (int)m.WParam;
                Capture = true; Invalidate(); _ribbon?.Invalidate();
                m.Result = 0; return;
            }
            if (m.Msg is 0x202 or 0xA2 && _pressedCaption != 0)
            {
                int pressed = _pressedCaption;
                bool invoke = pressed == CaptionHitTest(PointToClient(Cursor.Position));
                _pressedCaption = 0; Capture = false; Invalidate(); _ribbon?.Invalidate();
                if (invoke) PostMessage(Handle, 0x112, pressed == 20 ? 0xF060 : pressed == 8 ? 0xF020 : WindowState == FormWindowState.Maximized ? 0xF120 : 0xF030, 0);
                m.Result = 0; return;
            }
            if (m.Msg is 0x1F or 0x215) { _pressedCaption = 0; _hotCaption = 0; Invalidate(); _ribbon?.Invalidate(); }
            if (m.Msg == 0xA0) { _hotCaption = (int)m.WParam; Invalidate(); _ribbon?.Invalidate(); var track = new TrackMouse { Size = Marshal.SizeOf<TrackMouse>(), Flags = 0x12, Window = Handle }; TrackMouseEvent(ref track); }
            if (m.Msg == 0x2A2) { _hotCaption = 0; Invalidate(); _ribbon?.Invalidate(); }
            if (m.Msg == 0xA4 && (int)m.WParam is 2 or 3)
            {
                var menu = GetSystemMenu(Handle, false);
                int command = TrackPopupMenu(menu, 0x100, unchecked((short)(long)m.LParam), unchecked((short)((long)m.LParam >> 16)), 0, Handle, 0);
                if (command != 0) PostMessage(Handle, 0x112, command, 0);
                m.Result = 0; return;
            }
        }
        base.WndProc(ref m);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) Ribbon = null; base.Dispose(disposing); }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct TrackMouse { public int Size, Flags; public nint Window; public uint HoverTime; }
    [DllImport("user32.dll")] private static extern bool IsZoomed(nint window);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint window, nint after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] private static extern int GetSystemMetricsForDpi(int index, uint dpi);
    [DllImport("dwmapi.dll")] private static extern bool DwmDefWindowProc(nint window, int message, nint wparam, nint lparam, out nint result);
    [DllImport("user32.dll")] private static extern bool TrackMouseEvent(ref TrackMouse track);
    [DllImport("user32.dll")] private static extern nint GetSystemMenu(nint window, bool revert);
    [DllImport("user32.dll")] private static extern int TrackPopupMenu(nint menu, uint flags, int x, int y, int reserved, nint window, nint rectangle);
    [DllImport("user32.dll")] private static extern bool PostMessage(nint window, int message, nint wparam, nint lparam);
}

internal sealed record RibbonCaptionGeometry(Rectangle Icon, int ContentLeft, Rectangle Buttons, (Rectangle Bounds, int Hit)[] Targets)
{
    internal static RibbonCaptionGeometry Measure(int width, int height, int dpi, bool controlBox, bool minimize, bool maximize, bool icon)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        int button = Math.Min(S(46), Math.Max(0, width / 3));
        int right = width;
        List<(Rectangle, int)> targets = [];
        if (controlBox)
            foreach (int hit in new[] { 20, 9, 8 })
            {
                if (hit == 9 && !maximize || hit == 8 && !minimize) continue;
                right -= button; targets.Add((new(right, 0, button, height), hit));
            }
        int size = Math.Min(S(16), height);
        var iconBounds = icon && right >= S(32) ? new Rectangle(S(10), (height - size) / 2, size, size) : Rectangle.Empty;
        return new(iconBounds, Math.Min(right, iconBounds.IsEmpty ? S(8) : iconBounds.Right + S(10)), new(right, 0, width - right, height), targets.ToArray());
    }
}
