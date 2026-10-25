using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RibbonKit.Controls;

namespace RibbonKit.Automation;

// Keep WPF's item-peer lifetime, collection notifications and event redirection,
// but expose each realized command's semantics instead of a generic DataItem.
internal sealed class RibbonCommandItemsAutomationPeer(ItemsControl owner, AutomationControlType role)
    : ItemsControlAutomationPeer(owner)
{
    protected override string GetClassNameCore() => Owner.GetType().Name;
    protected override AutomationControlType GetAutomationControlTypeCore() => role;
    protected override ItemAutomationPeer CreateItemAutomationPeer(object item) => new CommandItemPeer(item, this);

    private sealed class CommandItemPeer(object item, ItemsControlAutomationPeer parent) : ItemAutomationPeer(item, parent)
    {
        private UIElement? Element => Item as UIElement
            ?? ((ItemsControl)ItemsControlAutomationPeer.Owner).ItemContainerGenerator.ContainerFromItem(Item) as UIElement;

        private AutomationPeer? ControlPeer => Element is { } element
            ? UIElementAutomationPeer.CreatePeerForElement(element) : null;

        protected override string GetClassNameCore() => ControlPeer?.GetClassName() ?? "RibbonCommandItem";
        protected override AutomationControlType GetAutomationControlTypeCore() =>
            ControlPeer?.GetAutomationControlType() ?? AutomationControlType.Custom;

        public override object? GetPattern(PatternInterface patternInterface)
        {
            object? provider = ControlPeer?.GetPattern(patternInterface);
            if (patternInterface == PatternInterface.Toggle && provider is IToggleProvider toggle && Element is ToggleButton button)
                return new ToggleActivation(button, toggle);
            return provider ?? base.GetPattern(patternInterface);
        }
    }

    // Native Toggle changes checked state only. A command copy also needs its
    // established Click/Command stages; the existing activation path runs them once
    // and uses SetCurrentValue so source/copy bindings remain attached.
    private sealed class ToggleActivation(ToggleButton owner, IToggleProvider state) : IToggleProvider
    {
        public ToggleState ToggleState => state.ToggleState;
        public void Toggle()
        {
            if (!owner.IsEnabled) throw new ElementNotEnabledException();
            RibbonKit.Controls.KeyTipService.InvokeControl(owner);
        }
    }
}
