using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed partial class RibbonPopupSurface
{
    // WinForms' HWND provider bridges these measured menu rows and the real native
    // content/scrollbar providers. Application controls retain their own peers.
    private sealed class ApplicationMenuAccessibleObject(RibbonPopupSurface owner) : ControlAccessibleObject(owner)
    {
        private readonly Dictionary<RibbonElement, AccessibleObject> _rows = [];
        public override AccessibleRole Role => AccessibleRole.MenuPopup;
        public override string? Name { get => owner.AccessibleName ?? "Application Menu"; set => owner.AccessibleName = value; }
        private IEnumerable<AccessibleObject> Children => owner.Targets.Where(t => t.Element is not null and not RibbonMenuSeparatorItem).Select(t => t.Element!).Distinct()
            .Select(e => { if (!_rows.TryGetValue(e, out var row)) _rows[e] = row = new ApplicationMenuRow(owner, this, e); return row; })
            .Concat(owner.Controls.Cast<Control>().Where(c => c.Visible).Select(c => c.AccessibilityObject));
        public override int GetChildCount() => Children.Count();
        public override AccessibleObject? GetChild(int index) => Children.ElementAtOrDefault(index);
        public override AccessibleObject? GetFocused() => Children.FirstOrDefault(c => (c.State & AccessibleStates.Focused) != 0) ?? base.GetFocused();
        public override AccessibleObject? HitTest(int x, int y) => Children.FirstOrDefault(c => c.Bounds.Contains(x, y)) ?? base.HitTest(x, y);
    }
    private sealed class ApplicationMenuRow(RibbonPopupSurface owner, AccessibleObject parent, RibbonElement item) : AccessibleObject
    {
        private RibbonTarget? Target => owner.IsDisposed ? null : owner.Targets.FirstOrDefault(t => ReferenceEquals(t.Element, item) && !t.DropDownPart);
        public override AccessibleObject Parent => parent;
        public override string? Name { get => item.Text; set { } }
        public override AccessibleRole Role => item is RibbonCheckBoxItem or RibbonToggleItem ? AccessibleRole.CheckButton : item is RibbonRadioButtonItem ? AccessibleRole.RadioButton : AccessibleRole.MenuItem;
        public override string? KeyboardShortcut => item.KeyTip;
        public override string? DefaultAction => item is RibbonDropDownItem and not RibbonSplitButtonItem ? "Open pane" : "Invoke";
        public override Rectangle Bounds => Target is { } target && !target.VisibleBounds.IsEmpty ? owner.RectangleToScreen(target.VisibleBounds) : Rectangle.Empty;
        public override AccessibleStates State
        {
            get
            {
                var state = AccessibleStates.Focusable | AccessibleStates.Selectable;
                if (!item.EffectiveEnabled) state |= AccessibleStates.Unavailable;
                if (Bounds.IsEmpty) state |= AccessibleStates.Offscreen;
                if (owner.Focused && ReferenceEquals(owner._focused, item)) state |= AccessibleStates.Focused;
                if (ReferenceEquals(owner._applicationPane, item)) state |= AccessibleStates.Selected | AccessibleStates.Expanded;
                else if (item is RibbonDropDownItem) state |= AccessibleStates.Collapsed;
                if (item is RibbonToggleItem { Checked: true } or RibbonCheckBoxItem { Checked: true } or RibbonRadioButtonItem { Checked: true } or RibbonSplitButtonItem { Checked: true }) state |= AccessibleStates.Checked;
                return state;
            }
        }
        public override void DoDefaultAction() { if (Target is { Enabled: true } target) owner._ribbon.Activate(target, owner); }
        public override void Select(AccessibleSelection flags) { if ((flags & (AccessibleSelection.TakeFocus | AccessibleSelection.TakeSelection)) != 0 && !owner.IsDisposed) owner.FocusElement(item); }
        public override int GetChildCount() => item is RibbonSplitButtonItem ? 1 : 0;
        public override AccessibleObject? GetChild(int index) => index == 0 && item is RibbonSplitButtonItem ? new ApplicationMenuArrow(owner, this, item) : null;
    }
    private sealed class ApplicationMenuArrow(RibbonPopupSurface owner, AccessibleObject parent, RibbonElement item) : AccessibleObject
    {
        private RibbonTarget? Target => owner.IsDisposed ? null : owner.Targets.FirstOrDefault(t => ReferenceEquals(t.Element, item) && t.DropDownPart);
        public override AccessibleObject Parent => parent;
        public override string? Name { get => item.Text + " menu"; set { } }
        public override AccessibleRole Role => AccessibleRole.MenuItem;
        public override string? DefaultAction => "Open pane";
        public override Rectangle Bounds => Target is { } target && !target.VisibleBounds.IsEmpty ? owner.RectangleToScreen(target.VisibleBounds) : Rectangle.Empty;
        public override AccessibleStates State => parent.State;
        public override void DoDefaultAction() { if (Target is { Enabled: true } target) owner._ribbon.Activate(target, owner); }
        public override void Select(AccessibleSelection flags) { if ((flags & AccessibleSelection.TakeFocus) != 0 && !owner.IsDisposed) owner.FocusElement(item, true); }
    }
}
