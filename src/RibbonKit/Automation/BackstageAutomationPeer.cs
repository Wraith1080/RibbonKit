using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Localization;

namespace RibbonKit.Automation;

// Keep stock tab selection/navigation and publish the existing page host without
// naming WPF's selected presenter, which would also propagate nav-item alignment.
internal sealed class BackstageAutomationPeer(Backstage owner) : TabControlAutomationPeer(owner)
{
    private NavigationAutomationPeer? _navigation;
    private RegionAutomationPeer? _page;
    private object[] _navigationItems = [];
    private DispatcherOperation? _refresh;

    protected override List<AutomationPeer>? GetChildrenCore()
    {
        if (!EnsureRegions()) return base.GetChildrenCore();
        if (_navigation is null)
        {
            var children = base.GetChildrenCore() ?? [];
            children.Add(_page!);
            return children;
        }
        var items = owner.Items.Cast<object>().ToArray();
        bool navigationChanged = !_navigationItems.SequenceEqual(items);
        _navigationItems = items;
        if (navigationChanged) QueueRegionRefresh();
        return [_navigation, _page!];
    }

    public override object? GetPattern(PatternInterface patternInterface)
    {
        if (EnsureRegions() && _navigation is not null && patternInterface is
            PatternInterface.Selection or PatternInterface.ItemContainer or PatternInterface.Scroll)
            return _navigation.GetPattern(patternInterface);
        return base.GetPattern(patternInterface);
    }

    protected override ItemAutomationPeer CreateItemAutomationPeer(object item) =>
        EnsureRegions() && _navigation is not null
            ? _navigation.PeerForItem(item) : base.CreateItemAutomationPeer(item);

    private bool EnsureRegions()
    {
        // Custom templates using the standard TabControl part already expose the page.
        if (owner.Template?.FindName("PART_SelectedContentHost", owner) is ContentPresenter)
        {
            _navigation = null; _page = null; _navigationItems = [];
            return false;
        }
        if (owner.Template?.FindName("ContentArea", owner) is not FrameworkElement pageHost)
        {
            _navigation = null; _page = null; _navigationItems = [];
            return false;
        }
        if (_page is null || !ReferenceEquals(_page.Owner, pageHost))
            _page = new RegionAutomationPeer(pageHost, AutomationControlType.Pane,
                "BackstagePage", SelectedPageName, () => PageChildren(pageHost));
        var navigationHost = owner.Template.FindName("NavColumn", owner) as FrameworkElement
            ?? owner.Template.FindName("NavSurface", owner) as FrameworkElement;
        if (navigationHost is null)
        {
            // An authored template may expose only the established page-host part.
            // Keep its actions reachable even without a known navigation region.
            _navigation = null; _navigationItems = [];
            return true;
        }

        if (_navigation is null || !ReferenceEquals(_navigation.Host, navigationHost))
            _navigation = new NavigationAutomationPeer(owner, navigationHost, this);
        return true;
    }

    internal void InvalidateRegions()
    {
        _navigation?.InvalidatePeer();
        _page?.InvalidatePeer();
        InvalidatePeer();
        QueueRegionRefresh();
    }

    private void QueueRegionRefresh()
    {
        if (_refresh is { Status: DispatcherOperationStatus.Pending or DispatcherOperationStatus.Executing }) return;
        // Selection/template callbacks precede presenter realization. Region peers
        // are not registered as native peers of their template elements, so rebuild
        // their caches once layout has produced the current page's visual children.
        _refresh = Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            // ResetChildrenCache can publish structure events and reconnect item
            // providers through the parent. Never run it from GetChildrenCore.
            try
            {
                ResetChildrenCache();
                _navigation?.ResetChildrenCache();
                _page?.ResetChildrenCache();
                _navigation?.InvalidatePeer();
                _page?.InvalidatePeer();
                InvalidatePeer();
            }
            finally { _refresh = null; }
        }));
    }

    private string SelectedPageName() =>
        owner.SelectedItem is { } selected && owner.ItemContainerGenerator.ContainerFromItem(selected) is UIElement item
            ? UIElementAutomationPeer.CreatePeerForElement(item)?.GetName() ?? string.Empty
            : string.Empty;

    private static List<AutomationPeer>? PageChildren(FrameworkElement host) =>
        UIElementAutomationPeer.CreatePeerForElement(host) is { } peer
            ? [peer] : new FrameworkElementAutomationPeer(host).GetChildren();

    // WPF item peers must be direct children of their owning items peer. Reusing
    // the root's item peers under a plain Group breaks provider reconnection and
    // can make a reader realize (select) tabs merely by inspecting their metadata.
    private sealed class NavigationAutomationPeer : TabControlAutomationPeer
    {
        private readonly BackstageAutomationPeer _root;
        internal FrameworkElement Host { get; }

        internal NavigationAutomationPeer(Backstage owner, FrameworkElement host, BackstageAutomationPeer root) : base(owner)
        {
            Host = host; _root = root;
            PropertyChangedEventManager.AddHandler(RibbonLocalizationBindingSource.Instance,
                (_, _) => InvalidatePeer(), "Item[]");
        }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetClassNameCore() => "BackstageNavigation";
        protected override string GetAutomationIdCore() => "BackstageNavigation";
        protected override string GetNameCore() =>
            System.Windows.Automation.AutomationProperties.GetName(Host) is { Length: > 0 } name
                ? name : RibbonLocalization.GetString(RibbonString.BackstageNavigation);
        protected override Rect GetBoundingRectangleCore() => new FrameworkElementAutomationPeer(Host).GetBoundingRectangle();
        protected override bool IsOffscreenCore() => new FrameworkElementAutomationPeer(Host).IsOffscreen();
        protected override bool IsKeyboardFocusableCore() => false;
        protected override ItemAutomationPeer CreateItemAutomationPeer(object item) => new NavigationItemPeer(item, this, _root);

        internal ItemAutomationPeer PeerForItem(object item) =>
            GetChildren()?.OfType<ItemAutomationPeer>().FirstOrDefault(peer => Equals(peer.Item, item))
            ?? CreateItemAutomationPeer(item);
    }

    private sealed class NavigationItemPeer(object item, NavigationAutomationPeer parent, BackstageAutomationPeer root)
        : TabItemAutomationPeer(item, parent)
    {
        public override object? GetPattern(PatternInterface patternInterface)
        {
            var pattern = base.GetPattern(patternInterface);
            return patternInterface == PatternInterface.SelectionItem && pattern is ISelectionItemProvider selection
                ? new SelectionItemProvider(selection, () => ProviderFromPeer(root)) : pattern;
        }
    }

    private sealed class SelectionItemProvider(ISelectionItemProvider native, Func<IRawElementProviderSimple> container)
        : ISelectionItemProvider
    {
        public bool IsSelected => native.IsSelected;
        public IRawElementProviderSimple SelectionContainer => container();
        public void AddToSelection() => native.AddToSelection();
        public void RemoveFromSelection() => native.RemoveFromSelection();
        public void Select() => native.Select();
    }

    // These peers describe existing semantic regions; their owners stay in the
    // original template and do not gain focus stops, bindings or extra visuals.
    private sealed class RegionAutomationPeer : FrameworkElementAutomationPeer
    {
        private readonly AutomationControlType _type;
        private readonly string _id;
        private readonly Func<string> _name;
        private readonly Func<List<AutomationPeer>?> _children;

        internal RegionAutomationPeer(FrameworkElement owner, AutomationControlType type,
            string id, Func<string> name, Func<List<AutomationPeer>?> children) : base(owner)
        {
            _type = type; _id = id; _name = name; _children = children;
            PropertyChangedEventManager.AddHandler(RibbonLocalizationBindingSource.Instance,
                (_, _) => InvalidatePeer(), "Item[]");
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => _type;
        protected override string GetClassNameCore() => _id;
        protected override string GetAutomationIdCore() => _id;
        protected override string GetNameCore() =>
            base.GetNameCore() is { Length: > 0 } name ? name : _name();
        protected override bool IsControlElementCore() => true;
        protected override bool IsContentElementCore() => true;
        protected override bool IsKeyboardFocusableCore() => false;
        protected override List<AutomationPeer>? GetChildrenCore() => _children();
    }
}
