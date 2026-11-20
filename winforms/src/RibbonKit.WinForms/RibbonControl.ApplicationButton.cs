using System.ComponentModel;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonControl
{
    private RibbonApplicationButtonShape? _applicationButtonShape;
    private IRibbonIcon? _applicationButtonIcon;
    /// <summary>Gets or overrides the theme's File shape. Null/Reset follows Theme.ApplicationButtonShape. Shape never changes FileMenuMode, its command, KeyTip or accessible identity.</summary>
    [Category("Appearance"), DefaultValue(null)]
    public RibbonApplicationButtonShape? ApplicationButtonShape
    {
        get => _applicationButtonShape;
        set
        {
            VerifyAccess();
            if (value is not null && !Enum.IsDefined(value.Value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_applicationButtonShape == value) return;
            CancelApplicationButtonPress(); _applicationButtonShape = value; ModelChanged();
            ApplicationButtonShapeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>Occurs after changing or clearing the explicit application-button shape.</summary>
    public event EventHandler? ApplicationButtonShapeChanged;
    /// <summary>Gets the explicit shape or the current theme's default.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonApplicationButtonShape EffectiveApplicationButtonShape => ApplicationButtonShape ?? Theme.ApplicationButtonShape;
    /// <summary>Gets the measured File command bounds in ribbon client device pixels, including caption overlap. Empty means there is no visible File target.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Rectangle ApplicationButtonBounds => Snapshot.Targets.FirstOrDefault(t => t.Kind == TargetKind.FileMenu)?.Bounds ?? Rectangle.Empty;
    /// <summary>Gets or sets a borrowed Orb glyph. Null uses the shared four-square vector; the Form icon is not substituted. The caller retains ownership. Rectangular File text is independent.</summary>
    [Browsable(false), DefaultValue(null), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IRibbonIcon? ApplicationButtonIcon
    {
        get => _applicationButtonIcon;
        set { VerifyAccess(); if (ReferenceEquals(_applicationButtonIcon, value)) return; _applicationButtonIcon = value; Invalidate(); }
    }
    internal void CancelApplicationButtonPress()
    {
        if (!ReferenceEquals(_pressed, FileMenu)) return;
        _pressed = null; _pressedScroll = null; Capture = false;
    }
    internal bool ApplicationButtonContains(Point point) => EffectiveApplicationButtonShape == RibbonApplicationButtonShape.Orb
        && HasFileMenu && RibbonApplicationButtonMetrics.Contains(ApplicationButtonBounds, point);
}
