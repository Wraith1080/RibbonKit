using System.ComponentModel;
using System.ComponentModel.Design;
using RibbonKit.WinForms.Layout;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design;

// SDK-free selection policy. All rectangles come from the runtime's measured,
// clipped layout; no duplicate designer geometry or command activation path.
internal sealed class RibbonDesignSelection : IDisposable
{
    private readonly RibbonControl _ribbon;
    private readonly ISelectionService? _selection;
    internal RibbonBackstageDesignPreview Backstage { get; }
    internal event EventHandler? Changed;
    internal RibbonDesignSelection(RibbonControl ribbon, ISelectionService? selection, Control? previewSurface = null)
    {
        _ribbon = ribbon; _selection = selection;
        Backstage = new(ribbon, selection, previewSurface ?? ribbon); Backstage.Changed += PreviewChanged;
        if (_selection is not null) _selection.SelectionChanged += SelectionChanged;
    }
    internal bool Active => Backstage.IsOpen || ReferenceEquals(_selection?.PrimarySelection, _ribbon)
        || TabFor(_selection?.PrimarySelection) is not null || QuickAccessComponent(_selection?.PrimarySelection);
    private void PreviewChanged(object? sender, EventArgs e) => Changed?.Invoke(this, EventArgs.Empty);
    internal object? Selected => _selection?.PrimarySelection;
    internal RibbonTab? TabFor(object? component)
    {
        for (var element = component as RibbonElement; element is not null; element = element.Parent as RibbonElement)
            if (element is RibbonTab tab) return _ribbon.Tabs.Contains(tab) ? tab : null;
        return null;
    }
    internal string[] TabChoices => new[] { "(Startup tab)" }.Concat(_ribbon.Tabs.Select(TabLabel)).ToArray();
    private static string TabLabel(RibbonTab tab) => tab.Text + " (" + (tab.Site?.Name ?? tab.Id) + ")"
        + (!tab.Visible ? " — hidden" : "") + (!tab.Enabled ? " — disabled" : "");
    internal string PreviewTab
    {
        get => _ribbon.DesignPreviewTab is { } tab ? TabLabel(tab) : "(Startup tab)";
        set
        {
            if (value == "(Startup tab)") { Select(_ribbon); _ribbon.SetDesignPreviewTab(null); Changed?.Invoke(this, EventArgs.Empty); return; }
            var tab = _ribbon.Tabs.SingleOrDefault(tab => TabLabel(tab) == value) ?? throw new ArgumentException("Select a tab from this ribbon.", nameof(value));
            Select(tab);
        }
    }
    internal RibbonElement? HitTest(Point point)
    {
        if (!_ribbon.ClientRectangle.Contains(point)) return null;
        var snapshot = _ribbon.Snapshot;
        return snapshot.Targets.LastOrDefault(target => target.Kind is TargetKind.Tab or TargetKind.Group or TargetKind.Item or TargetKind.QuickAccess
                && target.Element is not null && target.VisibleBounds.Contains(point))?.Element
            ?? snapshot.Groups.LastOrDefault(group => Rectangle.Intersect(group.Frame, snapshot.BodyViewport).Contains(point))?.Group;
    }
    private RibbonTarget? ScrollTarget(Point point) => _ribbon.Snapshot.Targets.LastOrDefault(target =>
        target.Kind is TargetKind.TabBack or TargetKind.TabNext or TargetKind.BodyBack or TargetKind.BodyNext && target.VisibleBounds.Contains(point));
    internal bool CanSelectAt(Point point) => Backstage.CanOpenFromFile(point) || HitTest(point) is not null || ScrollTarget(point) is not null;
    internal bool SelectAt(Point point)
    {
        if (Backstage.CanOpenFromFile(point)) { Backstage.Open(); return true; }
        if (ScrollTarget(point) is { } scroll)
        {
            _ribbon.Scroll(scroll.Kind is TargetKind.TabBack or TargetKind.TabNext, scroll.Kind is TargetKind.TabBack or TargetKind.BodyBack ? -120 : 120);
            return true;
        }
        if (HitTest(point) is not { } element) return false;
        Select(element); return true;
    }
    internal void Select(object component)
    {
        if (!ReferenceEquals(component, _ribbon) && TabFor(component) is null && !QuickAccessComponent(component)) return;
        Backstage.Close(false);
        if (TabFor(component) is { } tab) _ribbon.SetDesignPreviewTab(tab);
        _selection?.SetSelectedComponents(new[] { component }, SelectionTypes.Replace);
        Changed?.Invoke(this, EventArgs.Empty);
    }
    internal void SelectDraft(RibbonDesignSnapshot draft)
    {
        var key = draft.QuickAccessItems.FirstOrDefault(node => node.Key == draft.SelectionKey && node.Kind == "OwnedQat")?.SourceKey ?? draft.SelectionKey;
        var name = draft.AllNodes.FirstOrDefault(node => node.Key == key)?.ComponentName;
        var selected = _ribbon.Tabs.SelectMany(tab => tab.Tree()).Concat(_ribbon.CommandCatalog.Commands.SelectMany(command => command.Tree())).Concat(_ribbon.QuickAccessItems)
            .FirstOrDefault(element => name is not null && element.Site?.Name == name);
        Select((object?)selected ?? _ribbon);
    }
    internal RibbonElement? QuickAccessSource => Selected is RibbonElement entry && _ribbon.QuickAccessItems.Contains(entry)
        && RibbonQuickAccess.Source(entry) is { } source && !ReferenceEquals(entry, source) && ReferenceEquals(source.Ribbon, _ribbon) ? source : null;
    internal void SelectQuickAccessSource() { if (QuickAccessSource is { } source) Select(source); }
    internal RibbonDropDownItem? PopupSource
    {
        get
        {
            for (var element = QuickAccessSource ?? Selected as RibbonElement; element is not null; element = element.Parent as RibbonElement)
                if (element is RibbonDropDownItem and not RibbonGalleryItem && RibbonDesignCommitter.EditableElements(_ribbon).Contains(element))
                    return (RibbonDropDownItem)element;
            return null;
        }
    }
    private bool QuickAccessComponent(object? component)
    {
        if (component is not RibbonElement element || !ReferenceEquals(element.Ribbon, _ribbon)) return false;
        for (RibbonElement? current = element; current != null; current = current.Parent as RibbonElement)
            if (ReferenceEquals(current, _ribbon.CommandCatalog) || _ribbon.QuickAccessItems.Contains(current)) return true;
        return false;
    }
    internal Rectangle SelectionBounds(object? component)
    {
        if (component is not RibbonElement) return Rectangle.Empty;
        var snapshot = _ribbon.Snapshot;
        if (TabFor(component) is null)
        {
            // Catalog sources can have several visible QAT presentations. A
            // selected reference outlines only itself; a selected source reveals
            // its visible presentations without activating the command.
            return snapshot.Targets.Where(target => target.Kind == TargetKind.QuickAccess && target.Element is { } entry
                    && (ReferenceEquals(entry, component) || ReferenceEquals(RibbonQuickAccess.Source(entry), component)))
                .Select(target => target.VisibleBounds).Where(bounds => !bounds.IsEmpty).Aggregate(Rectangle.Empty,
                    (bounds, next) => bounds.IsEmpty ? next : Rectangle.Union(bounds, next));
        }
        for (var element = component as RibbonElement; element is not null; element = element.Parent as RibbonElement)
        {
            var bounds = snapshot.Targets.Where(target => ReferenceEquals(target.Element, element) && target.Kind is TargetKind.Tab or TargetKind.Group or TargetKind.Item)
                .Select(target => target.VisibleBounds).Where(rectangle => !rectangle.IsEmpty).ToArray();
            if (bounds.Length != 0) return bounds.Aggregate(Rectangle.Union);
            if (element is RibbonGroup group && snapshot.Groups.FirstOrDefault(slot => ReferenceEquals(slot.Group, group)) is { } slot)
                return Rectangle.Intersect(slot.Frame, snapshot.BodyViewport);
        }
        return Rectangle.Empty;
    }
    private void SelectionChanged(object? sender, EventArgs e)
    {
        bool wasOpen = Backstage.IsOpen;
        Backstage.ObserveSelection(Selected);
        if (TabFor(Selected) is { } tab) _ribbon.SetDesignPreviewTab(tab);
        // Preview gestures publish after SetSelectedComponents returns. An
        // external selection that closes the preview already published Close.
        if (Backstage.ChangingSelection || wasOpen && !Backstage.IsOpen) return;
        Changed?.Invoke(this, EventArgs.Empty);
    }
    public void Dispose()
    {
        if (_selection is not null) _selection.SelectionChanged -= SelectionChanged;
        Backstage.Changed -= PreviewChanged; Backstage.Close(false);
        if (!_ribbon.IsDisposed && _ribbon.IsInDesignMode) _ribbon.SetDesignPreviewTab(null);
    }
}
