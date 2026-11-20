using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonForm
{
    private RibbonWindowFrameAppearance _frameAppearance;
    private Color _aeroFrameTint = Color.Empty;
    private float _aeroFrameTintIntensity = .16f;
    private int? _aeroBorder;
    private int _priorAeroBorder;
    private RibbonBackdropBuffer? _aeroSideBuffer, _aeroBottomBuffer;
    private int AeroFrameInset => WindowState == FormWindowState.Normal && AeroFrameSettings is not null
        ? RibbonLayoutEngine.Scale(6, DeviceDpi) : 0;
    internal Rectangle AeroContentBounds => InsetAeroFrame(ClientRectangle);
    internal Rectangle CaptionHostBounds => new(AeroContentBounds.Left, 0, AeroContentBounds.Width, CaptionHeight);

    private Rectangle InsetAeroFrame(Rectangle bounds)
    {
        int inset = AeroFrameInset;
        int side = Math.Clamp(inset, 0, Math.Max(0, (bounds.Width - 1) / 2));
        int bottom = Math.Clamp(inset, 0, Math.Max(0, bounds.Height - 1));
        return new(bounds.Left + side, bounds.Top, bounds.Width - 2 * side, bounds.Height - bottom);
    }

    /// <summary>Gets or sets the requested frame appearance. Standard is the default and Reset value. Aero is applied only when the associated theme supplies an Aero palette. The native Backdrop preference is independent.</summary>
    [Category("Appearance"), DefaultValue(RibbonWindowFrameAppearance.Standard)]
    public RibbonWindowFrameAppearance FrameAppearance
    {
        get => _frameAppearance;
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            _ribbon?.VerifyAccess();
            if (_frameAppearance == value) return;
            _ribbon?.SettleMotion();
            _frameAppearance = value;
            PerformLayout();
            Invalidate();
            InvalidateFrameAppearance();
            FrameAppearanceChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Occurs after the requested frame appearance changes.</summary>
    public event EventHandler? FrameAppearanceChanged;

    /// <summary>Gets the current frame presentation. Windows may use a solid backdrop or system border independently of this value.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonWindowFrameAppearance EffectiveFrameAppearance => AeroFrameSettings is not null
        ? RibbonWindowFrameAppearance.Office2010Aero : RibbonWindowFrameAppearance.Standard;

    /// <summary>Gets or sets an opaque tint for the Aero caption, tab row and restored window edges. Empty follows the explicit TitleBarColor or the palette tint.</summary>
    [Category("Appearance"), DefaultValue(typeof(Color), "")]
    public Color AeroFrameTint
    {
        get => _aeroFrameTint;
        set
        {
            if (!value.IsEmpty && value.A != 255) throw new ArgumentException("Choose an opaque frame tint or reset to Empty.", nameof(value));
            _ribbon?.VerifyAccess();
            if (_aeroFrameTint == value) return;
            bool active = AeroFrameSettings is not null;
            if (active) _ribbon!.SettleMotion();
            _aeroFrameTint = value;
            if (active) InvalidateFrameAppearance();
        }
    }

    /// <summary>Gets or sets tint coverage in the active Aero frame, from zero to one. Defaults to .16. The inactive frame uses its neutral palette.</summary>
    [Category("Appearance"), DefaultValue(.16f)]
    public float AeroFrameTintIntensity
    {
        get => _aeroFrameTintIntensity;
        set
        {
            if (!float.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value));
            _ribbon?.VerifyAccess();
            if (_aeroFrameTintIntensity == value) return;
            bool active = AeroFrameSettings is not null;
            if (active) _ribbon!.SettleMotion();
            _aeroFrameTintIntensity = value;
            if (active) InvalidateFrameAppearance();
        }
    }

    internal RibbonAeroFrameAppearanceSettings? AeroFrameSettings =>
        FrameAppearance == RibbonWindowFrameAppearance.Office2010Aero && _ribbon is { HasWindowCaption: true } ribbon &&
        TopLevel && !DesignMode && LicenseManager.UsageMode != LicenseUsageMode.Designtime && !SystemInformation.HighContrast
        ? ribbon.Theme.AeroFrameAppearance : null;

    private Color AeroTint(RibbonAeroFrameAppearanceSettings frame) => !AeroFrameTint.IsEmpty ? AeroFrameTint
        : !TitleBarColor.IsEmpty ? TitleBarColor : frame.TintColor;

    internal RibbonFill AeroFrameFill(RibbonAeroFrameAppearanceSettings frame, bool composited)
    {
        if (!_captionActive) return frame.InactiveBackground;
        var tint = AeroTint(frame);
        return composited ? new(Color.FromArgb((int)Math.Round(255 * AeroFrameTintIntensity), tint))
            : RibbonFillColors.Map(frame.ActiveBackground, color => Blend(tint, color, AeroFrameTintIntensity));
    }

    internal void PaintAeroFrameBand(RibbonPaintContext context, Rectangle band, RibbonAeroFrameAppearanceSettings frame)
    {
        var mode = context.Graphics.CompositingMode;
        try
        {
            // Caption and tabs replace their respective part of the same fill.
            // The translucent material tint is applied once, including restores.
            if (context.CompositedText) context.Graphics.CompositingMode = CompositingMode.SourceCopy;
            band.Inflate(AeroContentBounds.Left, 0);
            _ribbon!.Painter.PaintFill(context with { BackgroundBounds = band }, AeroFrameFill(frame, context.CompositedText));
        }
        finally { context.Graphics.CompositingMode = mode; }
    }

    private void InvalidateFrameAppearance()
    {
        InvalidateCaption();
        if (_ribbon is { HasWindowCaption: true } ribbon)
            ribbon.Invalidate(new Rectangle(0, 0, ribbon.Width, ribbon.BackdropHeaderHeight));
    }

    internal void RefreshAeroFrameLayout()
    {
        if (FrameAppearance != RibbonWindowFrameAppearance.Office2010Aero) return;
        PerformLayout();
        Invalidate();
    }

    private void InvalidateAeroEdges()
    {
        var client = ClientRectangle;
        var content = AeroContentBounds;
        if (client == content) return;
        using var edges = new Region(client);
        edges.Exclude(content);
        Invalidate(edges);
    }

    private void PaintAeroEdges(PaintEventArgs e, bool material)
    {
        if (AeroFrameSettings is not { } frame || _ribbon is not { } ribbon) return;
        var client = ClientRectangle;
        var content = AeroContentBounds;
        if (content == client) return;
        var band = new Rectangle(content.Left, 0, content.Width, ribbon.BackdropHeaderHeight);
        var left = Rectangle.FromLTRB(client.Left, client.Top, content.Left, content.Bottom);
        var right = Rectangle.FromLTRB(content.Right, client.Top, client.Right, content.Bottom);
        var bottom = Rectangle.FromLTRB(client.Left, content.Bottom, client.Right, client.Bottom);
        PaintEdge(left, ref _aeroSideBuffer);
        PaintEdge(right, ref _aeroSideBuffer);
        PaintEdge(bottom, ref _aeroBottomBuffer);

        void PaintEdge(Rectangle edge, ref RibbonBackdropBuffer? buffer)
        {
            if (edge.Width <= 0 || edge.Height <= 0 || !edge.IntersectsWith(e.ClipRectangle)) return;
            void Paint(Graphics graphics) => PaintAeroFrameBand(new(graphics, edge, ribbon.Theme, ribbon.Font, DeviceDpi)
                { CompositedText = material }, band, frame);
            if (material)
                (buffer ??= new()).Paint(e.Graphics, edge.Size, edge.Height, Color.Transparent, Paint, origin: edge.Location);
            else Paint(e.Graphics);
        }
    }

    private void ReleaseAeroEdgeBuffers()
    {
        _aeroSideBuffer?.Dispose(); _aeroSideBuffer = null;
        _aeroBottomBuffer?.Dispose(); _aeroBottomBuffer = null;
    }

    internal void RefreshAeroBorder(bool force = false)
    {
        const int borderColorAttribute = 34, systemDefault = -1;
        if (!IsHandleCreated || IsDisposed || Disposing || _disposingBackdrop || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        var frame = AeroFrameSettings;
        if (frame is null)
        {
            if (_aeroBorder is not null && DwmSetWindowAttribute(Handle, borderColorAttribute, ref _priorAeroBorder, sizeof(int)) >= 0)
                _aeroBorder = null;
            return;
        }

        var color = _captionActive ? Blend(AeroTint(frame), frame.ActiveBorderColor, AeroFrameTintIntensity) : frame.InactiveBorderColor;
        int rgb = color.R | color.G << 8 | color.B << 16;
        if (!force && _aeroBorder == rgb) return;
        if (_aeroBorder is null && GetFrameBorderColor(Handle, borderColorAttribute, out _priorAeroBorder, sizeof(int)) < 0)
            _priorAeroBorder = systemDefault;
        if (DwmSetWindowAttribute(Handle, borderColorAttribute, ref rgb, sizeof(int)) >= 0) _aeroBorder = rgb;
    }

    [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
    private static extern int GetFrameBorderColor(nint window, int attribute, out int value, int size);
}
