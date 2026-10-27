using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms.Accessibility;

[ComVisible(true), ClassInterface(ClassInterfaceType.None)]
internal class RibbonAutomationNode : IRawProvider, IFragmentProvider, IInvokeProvider, IToggleProvider, ISelectionProvider, ISelectionItemProvider, IExpandProvider
{
    private static int s_nextId;
    private readonly int _runtimeId = Interlocked.Increment(ref s_nextId);
    protected RibbonAutomationRoot Root;
    internal readonly RibbonElement? Element;
    internal readonly bool TabList;
    private readonly Dictionary<int, object> _lastProperties = [];
    internal RibbonAutomationNode(RibbonAutomationRoot root, RibbonElement? element, bool tabList = false) => (Root, Element, TabList) = (root, element, tabList);
    public int ProviderOptions => 2;
    internal virtual RibbonAutomationNode? Parent => TabList ? Root : Element switch { RibbonTab => Root.TabsNode, RibbonGroup { Parent: RibbonTab tab } => Root.Node(tab), RibbonItem item when Root.Surface is null && item.Parent is RibbonGroup group => Root.Node(group), _ => Root };
    internal virtual IReadOnlyList<RibbonAutomationNode> Children => Root.Read(() =>
    {
        if (TabList)
            return (IReadOnlyList<RibbonAutomationNode>)Root.Ribbon.Tabs.Where(t => t.Visible).Select(Root.Node).ToArray();
        if (Element is RibbonTab tab && ReferenceEquals(tab, Root.Ribbon.SelectedTab))
            return tab.Groups.Where(g => g.Visible).Select(Root.Node).ToArray();
        if (Element is RibbonGroup group && !Root.Ribbon.Snapshot.Groups.Any(g => ReferenceEquals(g.Group, group) && g.Collapsed))
            return Root.Targets.Where(t => t.Element is RibbonItem item && ReferenceEquals(item.Parent, group)).Select(t => t.Element!).Distinct().Select(e => Root.Node(e)).ToArray();
        return [];
    });
    internal virtual Rectangle ScreenBounds => Root.Read(() =>
    {
        var bounds = TabList ? Root.Ribbon.Snapshot.TabViewport : Element is RibbonGroup group ? Rectangle.Intersect(Root.Ribbon.Snapshot.Groups.FirstOrDefault(s => ReferenceEquals(s.Group, group))?.Bounds ?? Rectangle.Empty, Root.Ribbon.Snapshot.BodyViewport) : Root.Targets.FirstOrDefault(t => ReferenceEquals(t.Element, Element))?.VisibleBounds ?? Rectangle.Empty;
        return bounds.IsEmpty || !Root.Control.Visible ? Rectangle.Empty : Root.Control.RectangleToScreen(bounds);
    });
    private bool Expandable => Element is RibbonDropDownItem || Element is RibbonGroup group && Root.Ribbon.Snapshot.Groups.Any(s => ReferenceEquals(s.Group, group) && s.Collapsed);
    public object? GetPatternProvider(int patternId) => Root.Read<object?>(() => patternId switch
    {
        10000 when Element is RibbonButtonItem and not RibbonToggleItem => this,
        10015 when Element is RibbonToggleItem => this,
        10001 when TabList => this,
        10010 when Element is RibbonTab => this,
        10005 when Expandable => this,
        _ => null
    });
    public virtual object? GetPropertyValue(int propertyId) => Root.Read<object?>(() => propertyId switch
    {
        30003 => TabList ? 50018 : Element switch { RibbonTab => 50019, RibbonGroup when Expandable => 50000, RibbonGroup => 50026, _ => 50000 },
        30005 => TabList ? "Ribbon tabs" : Element is RibbonItem { AccessibleName.Length: > 0 } item ? item.AccessibleName : Element?.Text ?? "Ribbon",
        30006 => Element?.KeyTip is { Length: > 0 } keys ? "Alt, " + keys : "",
        30008 => Element is not null && ReferenceEquals(Root.Ribbon.FocusedElement, Element) && (Root.Control.Focused || Root.Ribbon.Popups.ContainsFocus),
        30009 => TabList || Element is not RibbonGroup || Expandable,
        30010 => Element?.EffectiveEnabled ?? Root.Control.Enabled,
        30011 => TabList ? "ribbon.tabs" : Element?.Id ?? "ribbon",
        30012 => Element?.GetType().Name ?? "RibbonControl",
        30013 => (Element as RibbonItem)?.AccessibleDescription ?? "",
        30016 => true,
        30017 => true,
        30022 => ScreenBounds.IsEmpty,
        30024 => "WinForm",
        30028 => Element is RibbonDropDownItem || Expandable,
        30031 => Element is RibbonButtonItem and not RibbonToggleItem,
        30036 => Element is RibbonTab,
        30037 => TabList,
        30041 => Element is RibbonToggleItem,
        30070 => Expandable ? ExpandCollapseState : null,
        30079 => Element is RibbonTab && IsSelected,
        30086 => Element is RibbonToggleItem toggle ? toggle.Checked ? 1 : 0 : null,
        _ => null
    });
    public virtual IRawProvider? HostRawElementProvider => null;
    public IFragmentProvider? Navigate(int direction) => Root.Read<IFragmentProvider?>(() =>
    {
        if (direction == 0)
            return Parent;
        if (direction == 3)
            return Children.FirstOrDefault();
        if (direction == 4)
            return Children.LastOrDefault();
        var siblings = Parent?.Children ?? [];
        int i = siblings.ToList().IndexOf(this) + (direction == 1 ? 1 : -1);
        return i >= 0 && i < siblings.Count ? siblings[i] : null;
    });
    public int[] GetRuntimeId() => [3, _runtimeId];
    public UiaRectangle BoundingRectangle => new(ScreenBounds);
    public virtual IFragmentRootProvider[]? GetEmbeddedFragmentRoots() => null;
    public void SetFocus() => Root.Run(() => { CheckEnabled(); Root.Ribbon.SetKeyboardFocusCues(true); if (Root.Surface is { } surface && Element is { } element) surface.FocusElement(element); else if (Element is { } node) Root.Ribbon.FocusElement(node); else Root.Control.Focus(); });
    public IFragmentRootProvider FragmentRoot => Root;
    private void CheckEnabled()
    {
        if (Element?.IsDisposed == true || Element is not null && !Root.Ribbon.AllElements.Contains(Element))
            throw new COMException("Element is no longer available.", unchecked((int)0x80040201));
        if (Element is { EffectiveEnabled: false } || !Root.Control.Enabled)
            throw new COMException("Element is disabled.", unchecked((int)0x80040200));
    }
    public void Invoke() => Root.Run(() => { CheckEnabled(); if (Element is RibbonButtonItem item) { item.PerformClick(); Root.Ribbon.Popups.CloseAll(true); } });
    public void Toggle() => Invoke();
    public int ToggleState => Root.Read(() => Element is RibbonToggleItem { Checked: true } ? 1 : 0);
    public IRawProvider[] GetSelection() => Root.Read(() => Root.Ribbon.SelectedTab is { } tab ? new IRawProvider[] { Root.Node(tab) } : []);
    public bool CanSelectMultiple => false;
    public bool IsSelectionRequired => true;
    public void Select() => Root.Run(() => { CheckEnabled(); if (Element is RibbonTab tab) Root.Ribbon.SelectedTab = tab; });
    public void AddToSelection() => Select();
    public void RemoveFromSelection() => throw new InvalidOperationException("A ribbon requires a selected tab while tabs are available.");
    public bool IsSelected => Root.Read(() => ReferenceEquals(Element, Root.Ribbon.SelectedTab));
    public IRawProvider SelectionContainer => Root.TabsNode;
    public void Expand() => Root.Run(() =>
    {
        CheckEnabled();
        if (Root.Ribbon.Popups.IsExpanded(Element!))
            return;
        if (Element is RibbonDropDownItem drop)
            drop.ShowDropDown();
        else if (Element is RibbonGroup && Root.Targets.FirstOrDefault(t => ReferenceEquals(t.Element, Element)) is { } target)
            Root.Ribbon.Activate(target);
    });
    public void Collapse() => Root.Run(() => { if (Element is { } element) Root.Ribbon.Popups.Close(element, true); });
    public int ExpandCollapseState => Root.Read(() => Element is not null && Root.Ribbon.Popups.IsExpanded(Element) ? 1 : 0);
    internal void RaiseChanges()
    {
        foreach (int property in new[] { 30005, 30010, 30070, 30079, 30086 })
        {
            var value = GetPropertyValue(property);
            if (value is null)
                continue;
            if (_lastProperties.TryGetValue(property, out var old) && !Equals(old, value))
                _ = UiaNative.UiaRaiseAutomationPropertyChangedEvent(this, property, old, value);
            _lastProperties[property] = value;
        }
    }
}

[ComVisible(true), ClassInterface(ClassInterfaceType.None)]
internal sealed class RibbonAutomationRoot : RibbonAutomationNode, IFragmentRootProvider
{
    private readonly Dictionary<RibbonElement, RibbonAutomationNode> _nodes = [];
    private bool _disconnecting;
    internal RibbonControl Ribbon
    {
        get;
    }
    internal RibbonPopupSurface? Surface
    {
        get;
    }
    internal Control Control => (Control?)Surface ?? Ribbon;
    internal RibbonAutomationNode TabsNode
    {
        get;
    }
    internal IReadOnlyList<RibbonTarget> Targets => Surface?.Targets ?? Ribbon.Snapshot.Targets;
    internal bool Disconnected
    {
        get; private set;
    }
    // The base needs a root reference; assign it through a separate constructor overload below.
    internal RibbonAutomationRoot(RibbonControl ribbon, RibbonPopupSurface? surface = null) : base(null!, null)
    {
        Root = this;
        Ribbon = ribbon;
        Surface = surface;
        TabsNode = new(this, null, true);
    }
    internal override RibbonAutomationNode? Parent => null;
    internal override IReadOnlyList<RibbonAutomationNode> Children => Read(() => Surface is null ? new[] { TabsNode } : Targets.Where(t => t.Element is not null).Select(t => t.Element!).Distinct().Select(e => Node(e)).ToArray());
    internal override Rectangle ScreenBounds => Read(() => Control.Visible ? Control.RectangleToScreen(Control.ClientRectangle) : Rectangle.Empty);
    public override IRawProvider? HostRawElementProvider => Read(() => { UiaNative.UiaHostProviderFromHwnd(Control.Handle, out var host); return host; });
    public override object? GetPropertyValue(int propertyId) => propertyId switch { 30003 => Surface is null ? 50033 : 50009, 30005 => Surface?.OwnerElement.Text ?? Ribbon.AccessibleName ?? "Ribbon", 30011 => Surface is null ? Ribbon.Name is { Length: > 0 } name ? name : "ribbon" : "popup:" + Surface.OwnerElement.Id, 30012 => "RibbonControl", _ => base.GetPropertyValue(propertyId) };
    internal RibbonAutomationNode Node(RibbonElement element)
    {
        if (!_nodes.TryGetValue(element, out var node))
        {
            node = new(this, element);
            _nodes.Add(element, node);
            node.RaiseChanges();
        }
        return node;
    }
    internal T Read<T>(Func<T> action)
    {
        if (Disconnected || Control.IsDisposed || !Control.IsHandleCreated)
            throw new COMException("Element is no longer available.", unchecked((int)0x80040201));
        return Control.InvokeRequired ? (T)Control.Invoke(action)! : action();
    }
    internal void Run(Action action) => Read(() => { action(); return 0; });
    public IFragmentProvider? ElementProviderFromPoint(double x, double y) => Read<IFragmentProvider?>(() =>
    {
        var point = Control.PointToClient(new((int)x, (int)y));
        var target = Targets.LastOrDefault(t => t.VisibleBounds.Contains(point));
        return target?.Element is { } element ? Node(element) : this;
    });
    public IFragmentProvider? GetFocus() => Read<IFragmentProvider?>(() => Control.ContainsFocus && Ribbon.FocusedElement is { } item ? Node(item) : null);
    internal void Changed()
    {
        if (Disconnected || _disconnecting || !Control.IsHandleCreated)
            return;
        foreach (var entry in _nodes.ToArray())
        {
            if (!Ribbon.AllElements.Contains(entry.Key))
            {
                _ = UiaNative.UiaDisconnectProvider(entry.Value);
                _nodes.Remove(entry.Key);
            }
            else
                entry.Value.RaiseChanges();
        }
        _ = UiaNative.UiaRaiseStructureChangedEvent(this, 2, null, 0);
    }
    internal void Disconnect()
    {
        if (Disconnected || _disconnecting)
            return;
        _disconnecting = true;
        try
        {
            // UIAutomationCore reenters property/navigation/host callbacks while
            // disconnecting. Keep the live HWND and cached tree available until
            // those callbacks finish; subsequent stale client calls still fail.
            foreach (var node in _nodes.Values.ToArray())
                _ = UiaNative.UiaDisconnectProvider(node);
            _ = UiaNative.UiaDisconnectProvider(this);
        }
        finally
        {
            Disconnected = true;
            _nodes.Clear();
            _disconnecting = false;
        }
    }
}
