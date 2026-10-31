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
    internal event EventHandler? Changed;
    internal RibbonDesignSelection(RibbonControl ribbon, ISelectionService? selection)
    {
        _ribbon = ribbon; _selection = selection;
        if (_selection is not null) _selection.SelectionChanged += SelectionChanged;
    }
    internal bool Active => ReferenceEquals(_selection?.PrimarySelection, _ribbon) || TabFor(_selection?.PrimarySelection) is not null;
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
        return snapshot.Targets.LastOrDefault(target => target.Kind is TargetKind.Tab or TargetKind.Group or TargetKind.Item
                && target.Element is not null && target.VisibleBounds.Contains(point))?.Element
            ?? snapshot.Groups.LastOrDefault(group => Rectangle.Intersect(group.Frame, snapshot.BodyViewport).Contains(point))?.Group;
    }
    private RibbonTarget? ScrollTarget(Point point) => _ribbon.Snapshot.Targets.LastOrDefault(target =>
        target.Kind is TargetKind.TabBack or TargetKind.TabNext or TargetKind.BodyBack or TargetKind.BodyNext && target.VisibleBounds.Contains(point));
    internal bool CanSelectAt(Point point) => HitTest(point) is not null || ScrollTarget(point) is not null;
    internal bool SelectAt(Point point)
    {
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
        if (!ReferenceEquals(component, _ribbon) && TabFor(component) is null) return;
        if (TabFor(component) is { } tab) _ribbon.SetDesignPreviewTab(tab);
        _selection?.SetSelectedComponents(new[] { component }, SelectionTypes.Replace);
        Changed?.Invoke(this, EventArgs.Empty);
    }
    internal void SelectDraft(RibbonDesignSnapshot draft)
    {
        var name = draft.Nodes.FirstOrDefault(node => node.Key == draft.SelectionKey)?.ComponentName;
        var selected = _ribbon.Tabs.SelectMany(tab => tab.Tree()).FirstOrDefault(element => name is not null && element.Site?.Name == name);
        Select((object?)selected ?? _ribbon);
    }
    internal Rectangle SelectionBounds(object? component)
    {
        if (TabFor(component) is null) return Rectangle.Empty;
        var snapshot = _ribbon.Snapshot;
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
        if (TabFor(Selected) is { } tab) _ribbon.SetDesignPreviewTab(tab);
        Changed?.Invoke(this, EventArgs.Empty);
    }
    public void Dispose()
    {
        if (_selection is not null) _selection.SelectionChanged -= SelectionChanged;
        if (!_ribbon.IsDisposed && _ribbon.IsInDesignMode) _ribbon.SetDesignPreviewTab(null);
    }
}
