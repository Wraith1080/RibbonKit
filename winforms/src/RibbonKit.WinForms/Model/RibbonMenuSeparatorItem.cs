using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>A decorative horizontal separator owned by dropdown Items or Backstage Items/FooterItems. It cannot receive command focus, KeyTips or activation.</summary>
[DefaultProperty(nameof(Visible))]
public sealed class RibbonMenuSeparatorItem : RibbonItem
{
    /// <summary>Initializes a decorative menu separator.</summary>
    public RibbonMenuSeparatorItem() { }
    /// <summary>Does nothing; decorative separators have no command action.</summary>
    public override void PerformClick() => Ribbon?.VerifyAccess();
}
