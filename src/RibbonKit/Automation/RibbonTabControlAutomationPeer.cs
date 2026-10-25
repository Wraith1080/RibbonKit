using System.Windows.Automation.Peers;
using RibbonKit.Controls;

namespace RibbonKit.Automation;

// The stock TabControl peer returns its tab items only. Keep that implementation
// (including selected content and SelectionItem) and bridge adjacent native controls.
internal sealed class RibbonTabControlAutomationPeer(RibbonTabControl owner) : TabControlAutomationPeer(owner)
{
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var children = base.GetChildrenCore();
        foreach (string name in new[] { "QatTabRowHost", "MinimizeToggle" })
        {
            if (owner.Template?.FindName(name, owner) is System.Windows.UIElement element
                && UIElementAutomationPeer.CreatePeerForElement(element) is { } peer)
            {
                // Retain hidden parts too: WPF filters visibility and can reconnect
                // them after QAT placement or minimization-policy changes.
                children ??= [];
                if (!children.Contains(peer)) children.Add(peer);
            }
        }
        return children;
    }
}
