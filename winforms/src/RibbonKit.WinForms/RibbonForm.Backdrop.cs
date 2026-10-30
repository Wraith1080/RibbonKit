using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonForm
{
    private RibbonWindowBackdrop _backdrop, _effectiveBackdrop;
    private int _backdropHeight;
    private bool _backdropOwned, _applyingBackdrop, _backdropPaintFailed, _disposingBackdrop;
    private RibbonBackdropBuffer? _captionBackdrop;
    /// <summary>Gets or sets optional native material behind the form and ribbon. Default is None. Host native GDI application controls inside RibbonBackdropPanel to retain opaque text/content. Windows 11 build 22621 or later is required; unsupported, high-contrast, layered or borderless windows retain the preference with a solid fallback. Document controls, Opacity, TransparencyKey and the authored ribbon theme are not modified.</summary>
    [Category("Appearance"), DefaultValue(RibbonWindowBackdrop.None)]
    public RibbonWindowBackdrop Backdrop
    {
        get => _backdrop;
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            _ribbon?.VerifyAccess();
            if (_backdrop == value) return;
            _backdrop = value; _backdropPaintFailed = false; RefreshBackdrop(); BackdropChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after the requested Backdrop preference changes. Applications can update their own content margins or presentation; the library does not reparent application controls.</summary>
    public event EventHandler? BackdropChanged;
    /// <summary>Gets the native backdrop setting successfully applied to the current handle, or None during fallback or before handle creation. Windows may render an applied material as a solid color according to its transparency, remote-session, power or activation policy.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonWindowBackdrop EffectiveBackdrop => _effectiveBackdrop;
    internal int BackdropHeaderHeight => _ribbon is { HasWindowCaption: true } ribbon
        ? CaptionHeight + RibbonLayoutEngine.Scale(ribbon.LayoutTheme.TabHeight, DeviceDpi) : CaptionHeight;
    internal bool HasWindowBackdrop => _effectiveBackdrop != RibbonWindowBackdrop.None;
    internal static bool CanUseBackdrop(bool supported, bool highContrast, bool customCaption, bool topLevel, double opacity, Color transparencyKey, bool paintFailed)
        => supported && !highContrast && customCaption && topLevel && opacity == 1 && transparencyKey == Color.Empty && !paintFailed;
    private void RefreshBackdrop(bool force = false)
    {
        if (_applyingBackdrop || _disposingBackdrop || !IsHandleCreated || IsDisposed || Disposing) return;
        _applyingBackdrop = true;
        try
        {
            var requested = CanUseBackdrop(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) && LicenseManager.UsageMode != LicenseUsageMode.Designtime && !DesignMode, SystemInformation.HighContrast,
                CustomCaption, TopLevel, Opacity, TransparencyKey, _backdropPaintFailed || RibbonDrawingHold.HasOpacity(Handle)) ? _backdrop : RibbonWindowBackdrop.None;
            int height = requested == RibbonWindowBackdrop.None ? 0 : -1;
            if (!force && requested == _effectiveBackdrop && height == _backdropHeight) return;
            _ribbon?.SettleMotion();
            if (requested != RibbonWindowBackdrop.None)
            {
                SetMaterialCaptionStyle(true);
                try { _ribbon?.PrepareBackdropEditors(true); }
                catch (Win32Exception error) { _backdropPaintFailed = true; ReleaseNativeBackdrop(); Trace.WriteLine("Native editor backdrop fallback: " + error.Message); return; }
                int native = requested == RibbonWindowBackdrop.Mica ? 2 : 3;
                var margins = new BackdropMargins { Left = -1 };
                if (DwmSetWindowAttribute(Handle, 38, ref native, sizeof(int)) >= 0 && DwmExtendFrameIntoClientArea(Handle, ref margins) >= 0)
                { _backdropOwned = true; _effectiveBackdrop = requested; _backdropHeight = height; }
                else { _backdropOwned = true; ReleaseNativeBackdrop(); }
            }
            else ReleaseNativeBackdrop();
            Invalidate(true); _ribbon?.Invalidate(true);
            if (!RecreatingHandle) RefreshOpaquePanels(this);
        }
        finally { _applyingBackdrop = false; }
    }
    private static void RefreshOpaquePanels(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is RibbonBackdropPanel panel && panel.IsHandleCreated && panel.Visible) { panel.EnsureOpaque(); panel.Refresh(); }
            else RefreshOpaquePanels(child);
        }
    }
    private void ReleaseNativeBackdrop(bool updateFrame = true)
    {
        bool release = _backdropOwned && IsHandleCreated;
        bool repaint = release && updateFrame && !IsDisposed && !Disposing && !_disposingBackdrop && !RecreatingHandle;
        // Stop glass clearing before native calls can reenter painting. Replace
        // the old zero-alpha/black backing while material still covers it, then
        // remove the material and finish the Solid frame/client synchronously.
        _backdropOwned = false; _effectiveBackdrop = RibbonWindowBackdrop.None; _backdropHeight = 0;
        if (repaint) { RibbonDrawingHold.Repaint(Handle); FlushBackdropDrawing(); }
        if (release)
        {
            int none = 1; var margins = new BackdropMargins();
            DwmSetWindowAttribute(Handle, 38, ref none, sizeof(int)); DwmExtendFrameIntoClientArea(Handle, ref margins);
        }
        SetMaterialCaptionStyle(false, updateFrame);
        _ribbon?.PrepareBackdropEditors(false);
        _captionBackdrop?.Dispose(); _captionBackdrop = null;
        if (repaint) { RibbonDrawingHold.Repaint(Handle); FlushBackdropDrawing(); }
    }
    internal void BackdropPaintFailed(Exception error)
    {
        Trace.WriteLine("Ribbon backdrop uses solid fallback: " + error.Message);
        _backdropPaintFailed = true; RefreshBackdrop();
    }
    /// <inheritdoc />
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (!HasWindowBackdrop) { base.OnPaintBackground(e); return; }
        // A zero-alpha black client background exposes the system material.
        nint dc = e.Graphics.GetHdc();
        try { PatBlt(dc, e.ClipRectangle.X, e.ClipRectangle.Y, e.ClipRectangle.Width, e.ClipRectangle.Height, 0x00000042); }
        finally { e.Graphics.ReleaseHdc(dc); }
    }
    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        ReleaseNativeBackdrop(updateFrame: false); _backdropPaintFailed = false; base.OnHandleDestroyed(e);
    }
    [StructLayout(LayoutKind.Sequential)] private struct BackdropMargins { internal int Left, Right, Top, Bottom; }
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);
    [DllImport("dwmapi.dll")] private static extern int DwmExtendFrameIntoClientArea(nint window, ref BackdropMargins margins);
    [DllImport("gdi32.dll")] private static extern bool PatBlt(nint dc, int x, int y, int width, int height, uint operation);
    [DllImport("gdi32.dll", EntryPoint = "GdiFlush")] private static extern bool FlushBackdropDrawing();
}
