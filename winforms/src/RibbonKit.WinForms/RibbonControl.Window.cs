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
    private RibbonTheme? _headerThemeSource, _headerCaptionTheme, _headerTheme, _headerChromeTheme;
    private bool _headerComposited;
    internal RibbonTheme HeaderPaintTheme(bool composited, bool chrome = false)
    {
        var source = composited ? WindowPaintTheme : Theme;
        bool colored = Theme.ExtendTitleBarColorToHeader && HasWindowCaption &&
            (!WindowHost!.TitleBarColor.IsEmpty || !Theme.TitleBarBackground.IsEmpty || Theme.TitleBarBackgroundFill is not null);
        if (!colored && source.HeaderHover is null && source.HeaderPressed is null && source.HeaderForeground.IsEmpty) return source;
        var caption = colored ? WindowHost!.CaptionTheme(Theme, composited) : null;
        if (!ReferenceEquals(source, _headerThemeSource) || !ReferenceEquals(caption, _headerCaptionTheme) || composited != _headerComposited)
        {
            _headerThemeSource = source; _headerCaptionTheme = caption; _headerComposited = composited;
            _headerTheme = caption is null ? source : source with
            {
                Background = WindowHost!.CaptionColor(Theme, composited), BackgroundFill = WindowHost.CaptionFill(Theme, composited), HeaderForeground = caption.Foreground,
                HeaderHover = source.ColoredHeaderHover ?? caption.Hover, HeaderPressed = source.ColoredHeaderPressed ?? caption.Pressed, DisabledForeground = caption.DisabledForeground,
                BackdropHover = source.ColoredHeaderHover ?? caption.BackdropHover, BackdropPressed = source.ColoredHeaderPressed ?? caption.BackdropPressed
            };
            var header = _headerTheme;
            _headerChromeTheme = header with
            {
                Background = composited ? Color.Transparent : header.Background,
                Surface = composited ? Color.Transparent : header.Background,
                Foreground = header.HeaderTextColor, SecondaryText = caption?.SecondaryText ?? header.SecondaryText,
                Accent = caption?.Accent ?? header.Accent, Border = caption?.Border ?? header.Border,
                Hover = header.HeaderHover ?? header.Hover, Pressed = header.HeaderPressed ?? header.Pressed,
                Checked = caption?.Checked ?? header.Checked
            };
        }
        return chrome ? _headerChromeTheme! : _headerTheme!;
    }
    private RibbonBackstageCaptionFrame BackstageCaptionFrame(bool composited)
    {
        var theme = Theme;
        var policy = RibbonPresentationResolver.Backstage(theme).Caption.Resolve(theme);
        var host = WindowHost;
        var motion = BackstageOpeningMotion is { IsRunning: true } running ? running : null;
        var normal = host?.CaptionColor(theme, composited) ?? (composited ? Color.Transparent : theme.Background);
        var state = new RibbonBackstageCaptionState(IsBackstageOpen, HasWindowCaption,
            host is not null && !host.TitleBarColor.IsEmpty || !theme.TitleBarBackground.IsEmpty || theme.TitleBarBackgroundFill is not null, HasWindowBackdrop, normal,
            motion?.VisibleWidth ?? host?.ClientSize.Width ?? Width, WindowCaptionHeight, motion?.Opacity ?? (byte)255, Backstage?.NavigationBackgroundOpacity ?? 1);
        return policy.Resolve(state);
    }
    internal Color CaptionBackground(bool composited) => BackstageCaptionFrame(composited).CaptionColor;
    internal Rectangle BackstageCaptionTintBounds => BackstageCaptionFrame(false).TintBounds;
    internal Rectangle CaptionOverlayBounds => HasFullHeightBackstage && IsBackstageOpen
        ? new(0, 0, _backstageMotion is { IsRunning: true } motion ? motion.VisibleWidth : WindowHost?.ClientSize.Width ?? Width, WindowCaptionHeight) : Rectangle.Empty;
    internal Rectangle CaptionPageBounds
    {
        get
        {
            var overlay = CaptionOverlayBounds;
            if (overlay.IsEmpty || _backstageSurface is not { IsDisposed: false } surface) return Rectangle.Empty;
            // Use the live surface position: the caption split moves with the
            // navigation/page boundary throughout opening and closing.
            int left = Math.Clamp(surface.Left + surface.NavigationBounds.Right, overlay.Left, overlay.Right);
            return Rectangle.FromLTRB(left, overlay.Top, overlay.Right, overlay.Bottom);
        }
    }
    internal Rectangle CaptionNavigationBounds => Rectangle.FromLTRB(CaptionOverlayBounds.Left, CaptionOverlayBounds.Top,
        IsBackstageOpen ? CaptionPageBounds.Left : 0, CaptionOverlayBounds.Bottom);
    internal void PaintCaptionBackground(Graphics graphics, Rectangle bounds, bool composited)
    {
        var state = graphics.Save();
        try
        {
            graphics.SetClip(bounds, System.Drawing.Drawing2D.CombineMode.Intersect);
            var canvas = new Rectangle(0, 0, WindowHost?.ClientSize.Width ?? Width, WindowCaptionHeight);
            var fill = WindowHost?.CaptionFill(Theme, composited);
            BackstageCaptionFrame(composited).Paint(graphics, canvas, fill is { } authored
                ? () => Painter.PaintFill(new(graphics, canvas, Theme, Font, DeviceDpi), authored) : null);
        }
        finally { graphics.Restore(state); }
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
                _backdropTheme = Theme with { Background = Color.Transparent, BackgroundFill = Theme.BackgroundFill, QuickAccessBackgroundFill = Theme.QuickAccessBackgroundFill };
            }
            return _backdropTheme!;
        }
    }
    private bool _backdropBodyMasked;
    internal bool CapturingBackdropScene;
    internal bool BackdropBodyMasked => _backdropBodyMasked;
    internal bool KeepsBackdropFrame => _motion?.KeepsBodyFrame == true;
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
        _galleryStrips.SetBackdropOpacity(BackdropBodyMasked ? (byte)1 : (byte)255);
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
