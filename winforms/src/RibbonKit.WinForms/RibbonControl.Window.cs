using System.ComponentModel;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonControl
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal RibbonForm? WindowHost { get; set; }
    internal bool HasWindowCaption => WindowHost is { } host && host.SupportsRibbon(this);
    internal bool HasWindowBackdrop => HasWindowCaption && WindowHost!.HasWindowBackdrop;
    internal int BackdropHeaderHeight => WindowCaptionHeight + RibbonLayoutEngine.Scale(LayoutTheme.TabHeight, DeviceDpi);
    private RibbonBackdropBuffer? _backdropBuffer;
    private RibbonTheme? _backdropThemeSource, _backdropTheme;
    internal Color CaptionBackground(bool composited) => IsBackstageOpen
        ? Color.FromArgb((int)Math.Round((HasWindowBackdrop ? Backstage.NavigationBackgroundOpacity : 1) *
            (_backstageMotion is { IsRunning: true } motion ? motion.Opacity : 255)), Theme.BackstageNavigationBackground)
        : composited ? Color.Transparent : Theme.Background;
    internal void PaintCaptionBackground(Graphics graphics, Rectangle bounds, bool composited)
    {
        using var normal = new SolidBrush(composited ? Color.Transparent : Theme.Background);
        graphics.FillRectangle(normal, bounds);
        if (!IsBackstageOpen) return;
        int width = _backstageMotion is { IsRunning: true } motion ? motion.VisibleWidth : Width;
        var tint = Rectangle.Intersect(bounds, new Rectangle(0, 0, width, WindowCaptionHeight));
        using var background = new SolidBrush(CaptionBackground(composited));
        graphics.FillRectangle(background, tint);
    }
    internal void InvalidateBackstageCaption()
    {
        if (!IsDisposed && HasWindowCaption)
        {
            WindowHost!.InvalidateCaption();
            // Paint in the same timer turn as the native surface move.
            Update(); WindowHost.Update();
        }
    }
    internal RibbonTheme WindowPaintTheme
    {
        get
        {
            if (!HasWindowBackdrop) return Theme;
            if (!ReferenceEquals(Theme, _backdropThemeSource))
            {
                _backdropThemeSource = Theme;
                _backdropTheme = Theme with { Background = Color.Transparent };
            }
            return _backdropTheme!;
        }
    }
    private bool _backdropBodyMasked;
    internal bool CapturingBackdropScene;
    internal bool BackdropBodyMasked => _backdropBodyMasked;
    internal void PrepareBackdropEditors(bool enabled)
    {
        _editors.SetBackdrop(enabled);
        if (enabled) _backstageSurface?.PrepareBackdrop();
    }
    internal void CopyBackdropSnapshot(Bitmap destination)
    {
        using var graphics = Graphics.FromImage(destination);
        graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
        graphics.DrawImageUnscaled(_backdropBuffer!.Image, Point.Empty);
    }
    internal void SetBackdropBodyMasked(bool masked)
    {
        if (_backdropBodyMasked == masked) return;
        _backdropBodyMasked = masked; _editors.SetBackdropOpacity(BackdropBodyMasked ? (byte)1 : (byte)255);
        if (!IsDisposed && IsHandleCreated) { Invalidate(); Update(); }
    }
    internal int WindowCaptionHeight => HasWindowCaption ? WindowHost!.CaptionHeight : 0;
    /// <summary>Gets the displayed toolbar position. A retained TitleBar preference falls back to TabRow without a supporting RibbonForm association or room beside its caption buttons.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonQuickAccessPosition EffectiveQuickAccessPosition => QuickAccessPosition == RibbonQuickAccessPosition.TitleBar
        && (!HasWindowCaption || WindowHost!.QuickAccessSlot(this).Width <= 0)
        ? RibbonQuickAccessPosition.TabRow : QuickAccessPosition;

    internal bool HandleWindowHitTest(ref Message message)
    {
        if (message.Msg != 0x84 || !HasWindowCaption) return false;
        var local = PointToClient(new(unchecked((short)(long)message.LParam), unchecked((short)((long)message.LParam >> 16))));
        if (local.Y >= WindowCaptionHeight) return false;
        // Keep disabled targets client-side too: they must never drag the window.
        message.Result = Snapshot.QuickAccessBounds.Contains(local) ? 1 : -1; // HTCLIENT / HTTRANSPARENT to the parent Form.
        return true;
    }
}
