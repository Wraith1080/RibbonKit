using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using RibbonKit.Controls;

namespace RibbonKit.Automation;

/// <summary>UIA peer for <see cref="Ribbon"/>.</summary>
public class RibbonAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public RibbonAutomationPeer(Ribbon owner) : base(owner)
    {
    }

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    /// <inheritdoc />
    protected override string GetClassNameCore() => "Ribbon";
}

/// <summary>UIA peer for <see cref="RibbonGroup"/>: reports the group name.</summary>
public class RibbonGroupAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public RibbonGroupAutomationPeer(RibbonGroup owner) : base(owner)
    {
    }

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    /// <inheritdoc />
    protected override string GetClassNameCore() => "RibbonGroup";

    /// <inheritdoc />
    protected override string GetNameCore()
    {
        string name = base.GetNameCore();
        return string.IsNullOrEmpty(name)
            ? ((RibbonGroup)Owner).Header?.ToString() ?? string.Empty
            : name;
    }
}

/// <summary>UIA peer for <see cref="RibbonButton"/>: names the button from its Header.</summary>
public class RibbonButtonAutomationPeer : ButtonAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public RibbonButtonAutomationPeer(RibbonButton owner) : base(owner)
    {
    }

    /// <inheritdoc />
    protected override string GetNameCore()
    {
        string name = base.GetNameCore();
        return string.IsNullOrEmpty(name)
            ? ((RibbonButton)Owner).Header ?? string.Empty
            : name;
    }
}

/// <summary>UIA peer for <see cref="RibbonToggleButton"/>: names the button from its Header.</summary>
public class RibbonToggleButtonAutomationPeer : ToggleButtonAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public RibbonToggleButtonAutomationPeer(RibbonToggleButton owner) : base(owner)
    {
    }

    /// <inheritdoc />
    protected override string GetNameCore()
    {
        string name = base.GetNameCore();
        return string.IsNullOrEmpty(name)
            ? ((RibbonToggleButton)Owner).Header ?? string.Empty
            : name;
    }
}

/// <summary>UIA peer for <see cref="RibbonMenuItem"/>: a menu item named from its Header.</summary>
public class RibbonMenuItemAutomationPeer : ButtonAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public RibbonMenuItemAutomationPeer(RibbonMenuItem owner) : base(owner)
    {
    }

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.MenuItem;

    /// <inheritdoc />
    protected override string GetClassNameCore() => "RibbonMenuItem";

    /// <inheritdoc />
    protected override string GetNameCore()
    {
        string name = base.GetNameCore();
        return string.IsNullOrEmpty(name)
            ? ((RibbonMenuItem)Owner).Header ?? string.Empty
            : name;
    }
}

/// <summary>
/// UIA peer for <see cref="RibbonDropDownButton"/>: a button exposing the
/// ExpandCollapse pattern for its flyout.
/// </summary>
public class RibbonDropDownButtonAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
    /// <summary>Creates the peer.</summary>
    public RibbonDropDownButtonAutomationPeer(RibbonDropDownButton owner) : base(owner)
    {
    }

    /// <inheritdoc />
    public ExpandCollapseState ExpandCollapseState =>
        ((RibbonDropDownButton)Owner).IsDropDownOpen
            ? ExpandCollapseState.Expanded
            : ExpandCollapseState.Collapsed;

    /// <inheritdoc />
    public void Expand() =>
        ((RibbonDropDownButton)Owner).SetCurrentValue(RibbonDropDownButton.IsDropDownOpenProperty, true);

    /// <inheritdoc />
    public void Collapse() =>
        ((RibbonDropDownButton)Owner).SetCurrentValue(RibbonDropDownButton.IsDropDownOpenProperty, false);

    /// <inheritdoc />
    public override object? GetPattern(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.ExpandCollapse ? this : base.GetPattern(patternInterface);

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    /// <inheritdoc />
    protected override string GetClassNameCore() => "RibbonDropDownButton";

    /// <inheritdoc />
    protected override string GetNameCore()
    {
        string name = base.GetNameCore();
        return string.IsNullOrEmpty(name)
            ? ((RibbonDropDownButton)Owner).Header ?? string.Empty
            : name;
    }
}

/// <summary>
/// UIA peer for <see cref="RibbonSplitButton"/>: adds the Invoke pattern (primary
/// action) on top of the dropdown's ExpandCollapse pattern.
/// </summary>
public class RibbonSplitButtonAutomationPeer : RibbonDropDownButtonAutomationPeer, IInvokeProvider
{
    /// <summary>Creates the peer.</summary>
    public RibbonSplitButtonAutomationPeer(RibbonSplitButton owner) : base(owner)
    {
    }

    /// <inheritdoc />
    public void Invoke() => ((RibbonSplitButton)Owner).AutomationInvokePrimary();

    /// <inheritdoc />
    public override object? GetPattern(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.Invoke ? this : base.GetPattern(patternInterface);

    /// <inheritdoc />
    protected override string GetClassNameCore() => "RibbonSplitButton";
}

/// <summary>UIA peer for <see cref="RibbonComboBox"/>: names the combo from its Header label.</summary>
public class RibbonComboBoxAutomationPeer : ComboBoxAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public RibbonComboBoxAutomationPeer(RibbonComboBox owner) : base(owner)
    {
    }

    /// <inheritdoc />
    protected override string GetNameCore()
    {
        string name = base.GetNameCore();
        return string.IsNullOrEmpty(name)
            ? ((RibbonComboBox)Owner).Header ?? string.Empty
            : name;
    }
}
