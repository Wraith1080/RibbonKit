using System.Windows.Automation.Peers;
using System.Windows.Controls;
using RibbonKit.Automation;

namespace RibbonKit.Controls;

// The overflow templates need the same actionable item peers as the QAT strip.
// All ordinary ItemsControl layout, items and binding behavior remains inherited.
internal sealed class RibbonOverflowItemsHost : ItemsControl
{
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new RibbonCommandItemsAutomationPeer(this, AutomationControlType.Menu);
}
