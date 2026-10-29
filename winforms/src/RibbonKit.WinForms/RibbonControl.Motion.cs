using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>Chooses native ribbon geometry motion. System-disabled client animations always use immediate changes.</summary>
public enum RibbonAnimationLevel
{
    /// <summary>Commits final geometry immediately; the native preview default.</summary>
    None,
    /// <summary>Short motion with natural-size command drawings.</summary>
    Subtle,
    /// <summary>Longer motion with a gentle spring for the active tab indicator.</summary>
    Expressive
}

/// <summary>Chooses the presentation for arriving tab content and newly appearing tab headers.</summary>
public enum RibbonTabContentAnimation
{
    /// <summary>Crossfades natural-size tab body drawings and fades in newly appearing headers.</summary>
    Fade,
    /// <summary>Slides arriving natural-size body or header drawings down into their viewport, without a content fade.</summary>
    SlideDown
}

public partial class RibbonControl
{
    private RibbonAnimationLevel _animationLevel;
    private RibbonTabContentAnimation _tabContentAnimation;
    private RibbonGeometryMotion? _motion;
    internal bool CapturingMotionIndicators;
    internal RibbonGeometryMotion Motion => _motion ??= new(this);
    internal bool CanAnimateHeaderArrival => AnimationLevel != RibbonAnimationLevel.None && _motion?.Changing != true && _updateDepth == 0 && !_disposing && IsHandleCreated && Visible && SelectedTab is not null;
    /// <summary>Gets or sets opt-in Classic/Simplified, density, QAT, minimize/restore, expanded tab-selection and appearing-header motion. None preserves immediate presentation. Applications own this appearance preference.</summary>
    [Category("Appearance"), DefaultValue(RibbonAnimationLevel.None)]
    public RibbonAnimationLevel AnimationLevel
    {
        get => _animationLevel;
        set { VerifyAccess(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_animationLevel == value) return; _motion?.Settle(); _animationLevel = value; }
    }
    /// <summary>Gets or sets Fade or SlideDown for expanded tab content and newly shown, added or merged tab headers. Other geometry transitions keep their existing presentation. Changing this application-owned preference settles active motion; AnimationLevel.None and the system policy still produce immediate changes.</summary>
    [Category("Appearance"), DefaultValue(RibbonTabContentAnimation.Fade)]
    public RibbonTabContentAnimation TabContentAnimation
    {
        get => _tabContentAnimation;
        set { VerifyAccess(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_tabContentAnimation == value) return; _motion?.Settle(); _tabContentAnimation = value; }
    }
    /// <summary>Gets whether this ribbon currently displays inert geometry-transition drawings. Real layout and source ownership are already committed.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsAnimating => _motion?.IsRunning == true;
    internal void SettleMotion() { if (_motion is { Changing: false }) _motion.Settle(); }
    internal void ObserveMotionInput(ref Message message) => _motion?.ObserveInput(ref message);
    internal bool MotionBottomConnected => MessageBar.HasOpenMessages && MessageTopConnected;
}
