using System.Windows.Automation.Peers;
using RibbonKit.Controls;

namespace RibbonKit.Automation;

// The stock TabControl peer returns its tab items only. Keep that implementation
// (including selected content and SelectionItem) and bridge the adjacent QAT.
internal sealed class RibbonTabControlAutomationPeer(RibbonTabControl owner) : TabControlAutomationPeer(owner)
{
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var children = base.GetChildrenCore();
        if (owner.Template?.FindName("QatTabRowHost", owner) is RibbonQuickAccessToolBar toolbar
            && UIElementAutomationPeer.CreatePeerForElement(toolbar) is { } peer)
        {
            // Retain the part while hidden too: WPF's visibility metadata filters it
            // from Control View and can reconnect it after a QAT placement change.
            children ??= [];
            if (!children.Contains(peer)) children.Add(peer);
        }
        return children;
    }
}
