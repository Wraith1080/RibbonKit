using System.Windows.Automation.Peers;
using RibbonKit.Controls;

namespace RibbonKit.Automation;

// Simplified commands live in a sibling toolbar, while the source groups remain
// hidden for native flyouts. Publish the existing toolbar peer, not hidden sources.
internal sealed class RibbonGroupsHostAutomationPeer(RibbonGroupsHost owner) : ItemsControlAutomationPeer(owner)
{
    protected override string GetClassNameCore() => "ItemsControl";
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
    protected override ItemAutomationPeer CreateItemAutomationPeer(object item) => new GroupItemPeer(item, this);

    protected override List<AutomationPeer>? GetChildrenCore()
    {
        if (Ribbon.GetLayoutMode(owner) == RibbonLayoutMode.Simplified
            && owner.SimplifiedToolBar is { } toolbar
            && UIElementAutomationPeer.CreatePeerForElement(toolbar) is { } peer)
            return [peer];
        return base.GetChildrenCore();
    }

    // Match WPF's internal generic ItemsControl item peer in Classic mode.
    private sealed class GroupItemPeer(object item, ItemsControlAutomationPeer parent) : ItemAutomationPeer(item, parent)
    {
        protected override string GetClassNameCore() => "ItemsControlItem";
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;
    }
}
