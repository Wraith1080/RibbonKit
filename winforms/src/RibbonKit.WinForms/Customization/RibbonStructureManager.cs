namespace RibbonKit.WinForms;

// Structural preferences keep authored definitions and command owners alive.
// Only user-created presentations are library-owned copies of display state.
internal sealed partial class RibbonStructureManager(RibbonCommandCatalog catalog)
{
    private RibbonControl Host => catalog.Host;
    private readonly HashSet<RibbonTab> _tabs = [];
    private readonly HashSet<RibbonGroup> _groups = [];
    private readonly HashSet<RibbonElement> _owned = [];
    private readonly Dictionary<RibbonElement, string> _labels = [];
    private readonly HashSet<RibbonTab> _hidden = [];
    private RibbonCustomizationLayout? _defaults;
    internal bool Applying, Synchronizing;
    internal bool IsCustomized { get; private set; }
    internal IEnumerable<RibbonElement> Parked => _tabs.Cast<RibbonElement>().Concat(_groups).Concat(_owned).Where(e => !e.IsDisposed && ReferenceEquals(e.Parent, catalog)).Distinct();
    internal IEnumerable<RibbonElement> Retained => _owned.Where(e => !e.IsDisposed && e.Parent is null).SelectMany(e => e.Tree());
    internal string? Label(RibbonElement element) => _labels.GetValueOrDefault(element);
    internal bool Hidden(RibbonTab tab) => _hidden.Contains(tab);
    internal bool Custom(RibbonElement element) => _owned.Contains(element) && element is RibbonTab or RibbonGroup;
    internal RibbonElement? Definition(string id) => _tabs.Cast<RibbonElement>().Concat(_groups).FirstOrDefault(e => !e.IsDisposed && e.Id == id);
    private void Definitions()
    {
        if (!Applying)
        {
            _tabs.RemoveWhere(t => t.IsDisposed || !ReferenceEquals(t.Parent, Host) || t.IsModal || t.IsContextual);
            _groups.RemoveWhere(g => g.IsDisposed || g.Parent is not RibbonTab tab || !_tabs.Contains(tab));
        }
        foreach (var tab in Host.Tabs.Where(t => !t.IsDisposed && !t.IsModal && !t.IsContextual && !Custom(t))) _tabs.Add(tab);
        foreach (var tab in _tabs.Where(t => !t.IsDisposed)) foreach (var group in tab.Groups.Where(g => !g.IsDisposed && !Custom(g))) _groups.Add(group);
    }
    internal void CaptureDefaults() { Definitions(); _defaults = Capture(); }
    internal RibbonCustomizationLayout Defaults => RibbonCustomizationSerializer.CopyLayout(_defaults ??= Capture())!;
    internal RibbonCustomizationLayout Capture()
    {
        Definitions();
        return new() { Tabs = Host.Tabs.Where(Managed).Select(tab => new RibbonCustomizationTab
        {
            Id = tab.Id, IsCustom = Custom(tab), Label = Label(tab), Visible = !_hidden.Contains(tab),
            Groups = tab.Groups.Select(group => new RibbonCustomizationGroup
            {
                Id = group.Id, IsCustom = Custom(group), Label = Label(group),
                Commands = Custom(group) ? group.Items.Select(item =>
                {
                    if (!_proxies.TryGetValue(item, out var proxy)) throw new InvalidOperationException("Use a customization session to edit custom group membership.");
                    return new RibbonCustomizationCommand { Id = item.Id, CommandId = proxy.Source.Id, Label = proxy.Entry.Label, Size = proxy.Entry.Size };
                }).ToList() : []
            }).ToList()
        }).ToList() };
    }
    private bool Managed(RibbonTab tab) => !tab.IsDisposed && !tab.IsModal && !tab.IsContextual && (_tabs.Contains(tab) || Custom(tab));
    internal sealed class Plan
    {
        internal readonly List<(RibbonTab Model, RibbonCustomizationTab Data, List<(RibbonGroup Model, RibbonCustomizationGroup Data, List<RibbonItem> Items)> Groups)> Tabs = [];
        internal readonly List<RibbonElement> Created = [];
        internal readonly List<(Proxy Proxy, RibbonCustomizationCommand Entry)> Entries = [];
    }
    internal Plan Prepare(RibbonCustomizationLayout layout, bool persistent)
    {
        Definitions();
        RibbonCustomizationSerializer.Serialize(new RibbonCustomizationState { ApplicationId = "preflight", Ribbon = layout });
        var sources = catalog.GetAvailable(RibbonCustomizationTarget.Ribbon).ToDictionary(e => e.CommandId, StringComparer.Ordinal);
        var plan = new Plan(); var occupied = Host.AllElements.Where(e => !_owned.Contains(e) && !IsProxy(e)).Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        var tabIds = layout.Tabs.Where(t => !t.IsCustom).Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var groupIds = layout.Tabs.SelectMany(t => t.Groups).Where(g => !g.IsCustom).Select(g => g.Id).ToHashSet(StringComparer.Ordinal);
        if (!_tabs.Where(t => !t.IsDisposed && !t.IsModal && !t.IsContextual).Select(t => t.Id).ToHashSet(StringComparer.Ordinal).SetEquals(tabIds)
            || !_groups.Where(g => !g.IsDisposed).Select(g => g.Id).ToHashSet(StringComparer.Ordinal).SetEquals(groupIds))
            throw new InvalidOperationException("The profile must retain all current authored tabs and groups. Reload or migrate the profile for changed application definitions.");
        T Model<T>(string id, bool custom) where T : RibbonElement, new()
        {
            if (!custom)
            {
                if (Definition(id) is not T existing || persistent && !existing.HasExplicitId) throw new InvalidOperationException("Unknown or nonpersistent Ribbon definition: " + id);
                return existing;
            }
            if (!occupied.Add(id)) throw new InvalidOperationException("Custom identity conflicts with an authored component: " + id);
            if (_owned.FirstOrDefault(e => !e.IsDisposed && e.Id == id) is T retained) return retained;
            var created = new T { Id = id }; plan.Created.Add(created); return created;
        }
        try
        {
            foreach (var tab in layout.Tabs)
            {
                if (tab.IsCustom && string.IsNullOrWhiteSpace(tab.Label)) throw new InvalidOperationException("Custom tabs need a name.");
                var model = Model<RibbonTab>(tab.Id, tab.IsCustom);
                var groups = new List<(RibbonGroup, RibbonCustomizationGroup, List<RibbonItem>)>();
                foreach (var group in tab.Groups)
                {
                    if (group.IsCustom && string.IsNullOrWhiteSpace(group.Label)) throw new InvalidOperationException("Custom groups need a name.");
                    var groupModel = Model<RibbonGroup>(group.Id, group.IsCustom); var items = new List<RibbonItem>();
                    if (!group.IsCustom && !ReferenceEquals(groupModel.Parent, model))
                        throw new InvalidOperationException("Authored groups can be reordered within their original tab; use custom groups on other tabs.");
                    foreach (var row in group.Commands)
                    {
                        if (!sources.TryGetValue(row.CommandId, out var source) || source.Source is not RibbonItem command || persistent && !source.HasStableIdentity)
                            throw new InvalidOperationException("Unknown, transient or nonpersistent Ribbon command: " + row.CommandId);
                        if (!occupied.Add(row.Id)) throw new InvalidOperationException("Command presentation identity conflicts with another component: " + row.Id);
                        if (command is RibbonGalleryItem gallery)
                            foreach (var tile in gallery.Items) if (!occupied.Add(row.Id + ":tile:" + tile.Id)) throw new InvalidOperationException("Gallery tile presentation identity conflicts with another component.");
                        var proxy = _proxies.Values.FirstOrDefault(p => !p.Model.IsDisposed && p.Model.Id == row.Id && ReferenceEquals(p.Source, command));
                        if (proxy is null) { proxy = CreateProxy(command, row); plan.Created.Add(proxy.Model); }
                        plan.Entries.Add((proxy, row)); items.Add(proxy.Model);
                    }
                    groups.Add((groupModel, group, items));
                }
                plan.Tabs.Add((model, tab, groups));
            }
            return plan;
        }
        catch { Release(plan); throw; }
    }
    internal void Release(Plan plan)
    {
        foreach (var model in plan.Created.Where(e => e.Parent is null && !_owned.Contains(e)).Distinct())
        { RemoveProxy(model); model.Dispose(); }
    }
    internal void Apply(Plan plan)
    {
        var desired = plan.Tabs.Select(t => t.Model).ToArray(); var queue = new Queue<RibbonTab>(desired); var combined = new List<RibbonTab>();
        foreach (var tab in Host.Tabs) { if (!Managed(tab)) combined.Add(tab); else if (queue.TryDequeue(out var next)) combined.Add(next); }
        combined.AddRange(queue);
        foreach (var element in plan.Created) _owned.Add(element);
        foreach (var entry in plan.Entries) entry.Proxy.Entry = entry.Entry;
        // Detach movable groups before attaching their new parents. Authored
        // command collections are never rewritten or replaced.
        foreach (var tab in Host.Tabs.Where(Managed).Concat(desired).Distinct().ToArray()) tab.Groups.Clear();
        foreach (var group in _groups.Where(g => !g.IsDisposed && g.Parent is null)) group.Parent = catalog;
        foreach (var element in _owned.Where(e => !e.IsDisposed && e is RibbonGroup && e.Parent is null)) element.Parent = catalog;
        foreach (var group in _owned.OfType<RibbonGroup>().Where(g => !g.IsDisposed)) group.Items.Clear();
        Host.Tabs.Clear();
        foreach (var tab in _tabs.Where(t => !t.IsDisposed && t.Parent is null)) tab.Parent = catalog;
        foreach (var tab in _owned.OfType<RibbonTab>().Where(t => !t.IsDisposed && t.Parent is null)) tab.Parent = catalog;
        _labels.Clear(); _hidden.Clear();
        foreach (var tab in plan.Tabs)
        {
            if (tab.Data.Label is { } label) _labels[tab.Model] = label;
            if (!tab.Data.Visible) _hidden.Add(tab.Model);
            if (tab.Data.IsCustom) tab.Model.Text = tab.Data.Label!;
            foreach (var group in tab.Groups)
            {
                if (group.Data.Label is { } groupLabel) _labels[group.Model] = groupLabel;
                if (group.Data.IsCustom) { group.Model.Text = group.Data.Label!; foreach (var item in group.Items) group.Model.Items.Add(item); }
                group.Model.Parent = null; tab.Model.Groups.Add(group.Model);
            }
        }
        foreach (var tab in combined) { tab.Parent = null; Host.Tabs.Add(tab); }
        IsCustomized = true; Synchronize();
    }
    internal void Dispose()
    {
        foreach (var element in _owned.Where(e => !e.IsDisposed && e.Parent is null).ToArray()) element.Dispose();
        _owned.Clear(); _proxies.Clear(); _tiles.Clear(); _labels.Clear(); _hidden.Clear();
    }
    internal sealed record Snapshot(RibbonTab[] Tabs, Dictionary<RibbonTab, RibbonGroup[]> Groups,
        Dictionary<RibbonGroup, RibbonItem[]> Items, Dictionary<RibbonElement, string> Labels, RibbonTab[] Hidden,
        Dictionary<Proxy, RibbonCustomizationCommand> Entries, bool Customized);
    internal Snapshot Save()
    {
        var tabs = Host.Tabs.Concat(_tabs).Concat(_owned.OfType<RibbonTab>()).Where(t => !t.IsDisposed).Distinct().ToArray();
        return new(Host.Tabs.ToArray(), tabs.ToDictionary(t => t, t => t.Groups.ToArray()),
            _owned.OfType<RibbonGroup>().Where(g => !g.IsDisposed).ToDictionary(g => g, g => g.Items.ToArray()),
            new(_labels), _hidden.ToArray(), _proxies.Values.Where(p => _owned.Contains(p.Model)).ToDictionary(p => p, p => p.Entry), IsCustomized);
    }
    internal void Restore(Snapshot saved)
    {
        foreach (var tab in Host.Tabs.Concat(saved.Groups.Keys).Concat(_owned.OfType<RibbonTab>()).Where(t => !t.IsDisposed).Distinct().ToArray())
            if (Managed(tab) || saved.Groups.ContainsKey(tab)) tab.Groups.Clear();
        foreach (var group in _groups.Cast<RibbonElement>().Concat(_owned.Where(e => e is RibbonGroup)).Where(e => !e.IsDisposed && e.Parent is null)) group.Parent = catalog;
        foreach (var group in _owned.OfType<RibbonGroup>().Where(g => !g.IsDisposed)) group.Items.Clear();
        Host.Tabs.Clear();
        foreach (var tab in _tabs.Concat(_owned.OfType<RibbonTab>()).Where(t => !t.IsDisposed && t.Parent is null)) tab.Parent = catalog;
        foreach (var pair in saved.Groups)
            foreach (var group in pair.Value) { group.Parent = null; pair.Key.Groups.Add(group); }
        foreach (var pair in saved.Items) foreach (var item in pair.Value) pair.Key.Items.Add(item);
        foreach (var tab in saved.Tabs) { tab.Parent = null; Host.Tabs.Add(tab); }
        _labels.Clear(); foreach (var pair in saved.Labels) _labels.Add(pair.Key, pair.Value);
        _hidden.Clear(); foreach (var tab in saved.Hidden) _hidden.Add(tab);
        foreach (var pair in saved.Entries) pair.Key.Entry = pair.Value;
        IsCustomized = saved.Customized; Synchronize();
    }
    internal void Discard(Plan plan)
    {
        foreach (var model in plan.Created)
        {
            _owned.Remove(model);
            if (ReferenceEquals(model.Parent, catalog)) model.Parent = null;
            if (model.Parent is null) { RemoveProxy(model); model.Dispose(); }
        }
    }
}
