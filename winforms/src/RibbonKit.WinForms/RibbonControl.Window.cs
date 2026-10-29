using System.ComponentModel;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonControl
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal RibbonForm? WindowHost { get; set; }
    internal bool HasWindowCaption => WindowHost is { } host && host.SupportsRibbon(this);
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
