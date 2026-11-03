using RibbonKit.WinForms.Accessibility;

namespace RibbonKit.WinForms;

/// <summary>Provides RibbonControl for native ribbon presentation.</summary>
public partial class RibbonControl
{
    private RibbonAutomationRoot? _automation;
    internal RibbonAutomationRoot AutomationRoot => _automation ??= new(this);
    /// <inheritdoc />
    protected override void WndProc(ref Message m)
    {
        if (HandleWindowHitTest(ref m) || HandleCustomizationContext(ref m)) return;
        if (m.Msg == 0x3D && m.LParam.ToInt64() == -25 && !IsInDesignMode)
        {
            m.Result = UiaNative.UiaReturnRawElementProvider(Handle, m.WParam, m.LParam, AutomationRoot.NativeProvider);
            return;
        }
        base.WndProc(ref m);
    }
    internal void AccessibilityChanged()
    {
        _automation?.Changed();
        foreach (var surface in _popups.Surfaces)
            surface.AutomationChanged();
    }
    internal void AccessibilityFocusChanged(RibbonElement element)
    {
        if (_popups.Top is { IsApplicationMenu: true } application) { application.NotifyApplicationFocus(element); return; }
        var root = _popups.Top is { } surface ? surface.AutomationRoot : AutomationRoot;
        if (IsHandleCreated)
            _ = UiaNative.UiaRaiseAutomationEvent(root.Node(element).NativeProvider, 20005);
    }
    private void DisposeAutomation()
    {
        _automation?.Disconnect();
        _automation = null;
    }
    /// <inheritdoc />
    protected override AccessibleObject CreateAccessibilityInstance() => new RibbonMsaaObject(this);
    private sealed class RibbonMsaaObject(RibbonControl owner) : ControlAccessibleObject(owner)
    {
        public override AccessibleRole Role => AccessibleRole.Pane;
        public override string? Name
        {
            get => owner.AccessibleName ?? "Ribbon"; set => owner.AccessibleName = value;
        }
    }
}

internal sealed partial class RibbonPopupSurface
{
    private RibbonAutomationRoot? _automation;
    internal RibbonAutomationRoot AutomationRoot => _automation ??= new(_ribbon, this);
    internal void AutomationChanged()
    {
        if (IsApplicationMenu) AccessibilityNotifyClients(AccessibleEvents.Reorder, -1);
        else _automation?.Changed();
    }
    internal void NotifyApplicationFocus(RibbonElement element)
    {
        var elements = Targets.Where(t => t.Element is not null and not RibbonMenuSeparatorItem).Select(t => t.Element!).Distinct().ToList();
        int index = elements.IndexOf(element);
        if (index >= 0) AccessibilityNotifyClients(AccessibleEvents.Focus, index);
    }
    protected override void WndProc(ref Message m)
    {
        if (HandleCustomizationContext(ref m)) return;
        if (m.Msg == 0x3D && m.LParam.ToInt64() == -25 && !HasCustomContent && !IsApplicationMenu)
        {
            m.Result = UiaNative.UiaReturnRawElementProvider(Handle, m.WParam, m.LParam, AutomationRoot.NativeProvider);
            return;
        }
        base.WndProc(ref m);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DetachContent();
            DisposeApplicationMenu();
            DisposeOverflowScrolling();
            _automation?.Disconnect();
            _automation = null;
            DetachPresenter();
        }
        base.Dispose(disposing);
    }
}
