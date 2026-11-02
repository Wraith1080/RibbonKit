using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>A decorative separator: vertical in a group, horizontal in dropdown Items or Backstage Items/FooterItems. It cannot receive command focus, KeyTips or activation.</summary>
[DefaultProperty(nameof(Visible))]
public sealed class RibbonMenuSeparatorItem : RibbonItem
{
    /// <summary>Initializes a decorative separator.</summary>
    public RibbonMenuSeparatorItem() { }
    /// <summary>Does nothing; decorative separators have no command action.</summary>
    public override void PerformClick() => Ribbon?.VerifyAccess();
}
