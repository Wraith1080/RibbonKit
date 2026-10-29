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

/// <summary>Chooses the presentation for tab content, newly appearing tab headers and group collapse/restore.</summary>
public enum RibbonTabContentAnimation
{
    /// <summary>Crossfades natural-size tab/group drawings and fades in newly appearing headers.</summary>
    Fade,
    /// <summary>Slides arriving natural-size tab/header drawings down, and groups left on collapse or right on restore, without a content fade.</summary>
    SlideDown
}

public partial class RibbonControl
{
    private RibbonAnimationLevel _animationLevel;
    private RibbonTabContentAnimation _tabContentAnimation;
    private RibbonGeometryMotion? _motion;
    private RibbonGroupMotion? _groupMotion;
    internal RibbonGroupMotion GroupMotion => _groupMotion ??= new(this);
    internal bool CanAnimateGroups => AnimationLevel != RibbonAnimationLevel.None && _motion?.Changing != true && _motion?.IsRunning != true && _updateDepth == 0 && !_disposing && IsHandleCreated && Visible && Enabled && !IsInDesignMode && !IsBackstageOpen;
    internal void AnimateGroupChange(Action mutation)
    {
        if (!CanAnimateGroups || _groupMotion?.Changing == true) { mutation(); return; }
        if (IsMinimized && Popups.Surfaces.FirstOrDefault(s => s.OwnerElement is RibbonTab) is { } body)
            GroupMotion.Change(body, () => body.BodyLayout!, mutation);
        else GroupMotion.Change(this, () => Snapshot, mutation);
    }
    internal bool CapturingMotionIndicators;
    internal RibbonGeometryMotion Motion => _motion ??= new(this);
    internal bool CanAnimateHeaderArrival => AnimationLevel != RibbonAnimationLevel.None && _motion?.Changing != true && _updateDepth == 0 && !_disposing && IsHandleCreated && Visible && SelectedTab is not null;
    internal bool CanAnimatePopupOpening => _updateDepth == 0 && !_disposing && IsHandleCreated && Enabled && !IsInDesignMode;
    internal bool CanAnimateMessageChange => AnimationLevel != RibbonAnimationLevel.None && _motion?.Changing != true && _updateDepth == 0 && !_disposing && IsHandleCreated && Visible;
    /// <summary>Gets or sets opt-in geometry, group, tab/header, message-panel, popup and Backstage/page motion. None preserves immediate presentation. Applications own this appearance preference.</summary>
    [Category("Appearance"), DefaultValue(RibbonAnimationLevel.None)]
    public RibbonAnimationLevel AnimationLevel
    {
        get => _animationLevel;
        set { VerifyAccess(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_animationLevel == value) return; _groupMotion?.Settle(); _motion?.Settle(); _popups?.SettleMotion(); _backstageMotion?.Dispose(); _animationLevel = value; }
    }
    /// <summary>Gets or sets Fade or SlideDown for expanded tab content, arriving tab headers and group collapse/restore. Groups slide horizontally in the collapse/restore direction. Changing this application-owned preference settles active content motion; AnimationLevel.None and the system policy still produce immediate changes.</summary>
    [Category("Appearance"), DefaultValue(RibbonTabContentAnimation.Fade)]
    public RibbonTabContentAnimation TabContentAnimation
    {
        get => _tabContentAnimation;
        set { VerifyAccess(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_tabContentAnimation == value) return; _groupMotion?.Settle(); _motion?.Settle(); _tabContentAnimation = value; }
    }
    /// <summary>Gets whether geometry, group, message-panel, popup or Backstage/page motion is active. Animated Backstage closing retains visibility until completion; geometry and message drawings follow committed layout.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsAnimating => _groupMotion?.IsRunning == true || _motion?.IsRunning == true || _popups.IsAnimating || _backstageMotion?.IsRunning == true;
    internal void SettleMotion() { if (_groupMotion is { Changing: false }) _groupMotion.Settle(); if (_motion is { Changing: false }) _motion.Settle(); _popups?.SettleMotion(); _backstageMotion?.Dispose(); }
    internal bool ObserveMotionInput(ref Message message) { _groupMotion?.ObserveInput(ref message); _motion?.ObserveInput(ref message); _popups.ObserveMotionInput(ref message); return _backstageMotion?.ObserveInput(ref message) ?? false; }
    internal bool MotionBottomConnected => MessageBar.HasOpenMessages && MessageTopConnected;
}
